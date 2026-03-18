using System.Runtime.CompilerServices;
using Epsilon.Core.Database;
using Epsilon.Core.Documents;
using Epsilon.Core.LLM;
using Epsilon.Core.Models;
using Epsilon.Core.WebSearch;

namespace Epsilon.Core.Research;

public class ResearchService
{
    private readonly DatabaseService _db;
    private readonly ProviderRegistry _registry;
    private readonly DocumentRetriever _retriever;
    private readonly WebSearchService _webSearch;

    public ResearchService(DatabaseService db, ProviderRegistry registry,
        DocumentRetriever retriever, WebSearchService webSearch)
    {
        _db = db;
        _registry = registry;
        _retriever = retriever;
        _webSearch = webSearch;
    }

    public ResearchProject CreateProject(string toolType, string providerId, string modelId)
    {
        var tool = ToolRegistry.Get(toolType)
            ?? throw new ArgumentException($"Unknown tool type: {toolType}");

        var project = new ResearchProject
        {
            ToolType = toolType,
            Title = $"New {tool.DisplayName}",
            ProviderId = providerId,
            ModelId = modelId,
        };

        _db.CreateResearchProject(project);

        // Pre-create all step rows
        foreach (var stepDef in tool.Steps)
        {
            var step = new ResearchStep
            {
                ProjectId = project.Id,
                StepIndex = stepDef.Index,
            };
            _db.UpsertResearchStep(step);
        }

        return project;
    }

    public List<ResearchProject> ListProjects() => _db.ListResearchProjects();

    public ResearchProject? GetProject(string id) => _db.GetResearchProject(id);

    public void UpdateProject(ResearchProject project) => _db.UpdateResearchProject(project);

    public void DeleteProject(string id) => _db.DeleteResearchProject(id);

    public List<ResearchStep> GetSteps(string projectId) => _db.GetResearchSteps(projectId);

    public void SaveStep(ResearchStep step) => _db.UpsertResearchStep(step);

    public async IAsyncEnumerable<StreamChunk> StreamStepAsync(
        ResearchProject project,
        ResearchStep step,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var tool = ToolRegistry.Get(project.ToolType)
            ?? throw new InvalidOperationException($"Unknown tool type: {project.ToolType}");

        var stepDef = tool.Steps[step.StepIndex];

        // Get provider
        var provider = _registry.Get(project.ProviderId ?? "openai")
            ?? throw new InvalidOperationException("No LLM provider configured.");

        var apiKey = _db.GetSetting($"{project.ProviderId}_api_key");

        // Build the user prompt with context
        var userPrompt = BuildUserPrompt(project, step, stepDef);

        // Run RAG and optional web search in parallel
        var ragTask = Task.Run(() => _retriever.Search(step.UserInput.Length > 0 ? step.UserInput : project.Title), ct);
        var webTask = project.WebSearchEnabled
            ? _webSearch.SearchAsync(step.UserInput.Length > 0 ? step.UserInput : project.Title, ct)
            : Task.FromResult(new List<WebSearchResult>());

        await Task.WhenAll(ragTask, webTask);

        // Build system prompt with context
        var systemPrompt = stepDef.SystemPrompt;

        var ragChunks = ragTask.Result;
        if (ragChunks.Count > 0)
            systemPrompt += _retriever.BuildContext(ragChunks);

        var webResults = webTask.Result;
        if (webResults.Count > 0)
            systemPrompt += _webSearch.BuildContext(webResults);

        var request = new ChatRequest
        {
            Model = project.ModelId ?? "gpt-4o",
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = userPrompt },
            },
            SystemPrompt = systemPrompt,
            Temperature = 0.7f,
            MaxTokens = 4096,
        };

        // Update step status
        step.Status = "generating";
        _db.UpsertResearchStep(step);

        var fullResponse = new System.Text.StringBuilder();

        await foreach (var chunk in provider.StreamAsync(request, apiKey, ct))
        {
            fullResponse.Append(chunk.Delta);
            yield return chunk;
            if (chunk.Done) break;
        }

        // Save generated content
        step.GeneratedContent = fullResponse.ToString();
        step.Status = "done";
        step.GeneratedAt = DateTime.UtcNow;
        _db.UpsertResearchStep(step);

        // Update project title from first step if it's still the default
        if (step.StepIndex == 0 && project.Title.StartsWith("New "))
        {
            var input = step.UserInput.Trim();
            project.Title = input.Length > 60 ? input[..57] + "..." : input;
            if (string.IsNullOrWhiteSpace(project.Title))
                project.Title = $"{tool.DisplayName} Project";
            _db.UpdateResearchProject(project);
        }
    }

    private string BuildUserPrompt(ResearchProject project, ResearchStep currentStep, StepDefinition stepDef)
    {
        var prompt = stepDef.UserPromptTemplate;

        // Replace {input}
        prompt = prompt.Replace("{input}", currentStep.UserInput);

        // Replace {project_title}
        prompt = prompt.Replace("{project_title}", project.Title);

        // Replace {previous_steps} with formatted context from prior steps
        var previousContext = BuildPreviousStepsContext(project, currentStep.StepIndex);
        prompt = prompt.Replace("{previous_steps}", previousContext);

        return prompt;
    }

    private string BuildPreviousStepsContext(ResearchProject project, int currentIndex)
    {
        if (currentIndex == 0) return "(No previous steps)";

        var tool = ToolRegistry.Get(project.ToolType);
        if (tool == null) return "";

        var steps = _db.GetResearchSteps(project.Id);
        var parts = new List<string>();

        foreach (var step in steps.Where(s => s.StepIndex < currentIndex && s.Status == "done"))
        {
            var label = tool.Steps[step.StepIndex].Label;
            parts.Add($"### {label}\n\n**Student Input:** {step.UserInput}\n\n**Generated Content:**\n{step.GeneratedContent}");
        }

        return parts.Count > 0
            ? string.Join("\n\n---\n\n", parts)
            : "(No completed previous steps)";
    }
}
