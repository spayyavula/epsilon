using System.Runtime.CompilerServices;
using Epsilon.Core.LLM;
using Epsilon.Core.Models;
using Epsilon.Core.Research;
using Epsilon.Web.Data;
using Epsilon.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Epsilon.Web.Services;

public class WebResearchService
{
    private readonly AppDbContext _db;
    private readonly ProviderRegistry _registry;
    private readonly WebDocumentRetriever _retriever;
    private readonly UserKeyStore _keyStore;

    public WebResearchService(AppDbContext db, ProviderRegistry registry,
        WebDocumentRetriever retriever, UserKeyStore keyStore)
    {
        _db = db;
        _registry = registry;
        _retriever = retriever;
        _keyStore = keyStore;
    }

    public async Task<ResearchProjectEntity> CreateProjectAsync(
        Guid userId, string toolType, string providerId, string modelId, bool webSearchEnabled)
    {
        var tool = ToolRegistry.Get(toolType)
            ?? throw new ArgumentException($"Unknown tool type: {toolType}");

        var project = new ResearchProjectEntity
        {
            UserId = userId,
            ToolType = toolType,
            Title = $"New {tool.DisplayName}",
            ProviderId = providerId,
            ModelId = modelId,
            WebSearchEnabled = webSearchEnabled,
        };
        _db.ResearchProjects.Add(project);

        foreach (var stepDef in tool.Steps)
        {
            _db.ResearchSteps.Add(new ResearchStepEntity
            {
                ProjectId = project.Id,
                StepIndex = stepDef.Index,
            });
        }

        await _db.SaveChangesAsync();
        return project;
    }

    public async Task<List<ResearchProjectEntity>> ListProjectsAsync(Guid userId)
    {
        return await _db.ResearchProjects
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();
    }

    public async Task<ResearchProjectEntity?> GetProjectAsync(Guid userId, Guid projectId)
    {
        return await _db.ResearchProjects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
    }

    public async Task<bool> UpdateProjectAsync(Guid userId, Guid projectId,
        string? title, int? currentStep, string? status, bool? webSearchEnabled)
    {
        var project = await _db.ResearchProjects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        if (project == null) return false;

        if (title != null) project.Title = title;
        if (currentStep.HasValue) project.CurrentStep = currentStep.Value;
        if (status != null) project.Status = status;
        if (webSearchEnabled.HasValue) project.WebSearchEnabled = webSearchEnabled.Value;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteProjectAsync(Guid userId, Guid projectId)
    {
        var project = await _db.ResearchProjects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        if (project == null) return false;
        _db.ResearchProjects.Remove(project);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ResearchStepEntity>> GetStepsAsync(Guid userId, Guid projectId)
    {
        var project = await _db.ResearchProjects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        if (project == null) return new();

        return await _db.ResearchSteps
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.StepIndex)
            .ToListAsync();
    }

    public async Task<bool> SaveStepAsync(Guid userId, Guid projectId, int stepIndex, string userInput)
    {
        var project = await _db.ResearchProjects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        if (project == null) return false;

        var step = await _db.ResearchSteps
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.StepIndex == stepIndex);
        if (step == null) return false;

        step.UserInput = userInput;
        await _db.SaveChangesAsync();
        return true;
    }

    public async IAsyncEnumerable<StreamChunk> StreamStepAsync(
        Guid userId, Guid projectId, int stepIndex,
        string? overrideProviderId, string? overrideModelId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var project = await _db.ResearchProjects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId, ct)
            ?? throw new InvalidOperationException("Project not found");

        var step = await _db.ResearchSteps
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.StepIndex == stepIndex, ct)
            ?? throw new InvalidOperationException("Step not found");

        var tool = ToolRegistry.Get(project.ToolType)
            ?? throw new InvalidOperationException($"Unknown tool type: {project.ToolType}");

        var stepDef = tool.Steps[stepIndex];
        var providerId = overrideProviderId ?? project.ProviderId ?? "openai";
        var modelId = overrideModelId ?? project.ModelId ?? "gpt-4o";

        var provider = _registry.Get(providerId)
            ?? throw new InvalidOperationException("No LLM provider configured.");

        var apiKey = await _keyStore.GetKeyAsync(userId, providerId);

        // Build user prompt
        var userPrompt = stepDef.UserPromptTemplate
            .Replace("{input}", step.UserInput)
            .Replace("{project_title}", project.Title);

        var previousContext = await BuildPreviousStepsContextAsync(project, stepIndex, tool);
        userPrompt = userPrompt.Replace("{previous_steps}", previousContext);

        // RAG
        var searchQuery = step.UserInput.Length > 0 ? step.UserInput : project.Title;
        var ragChunks = await _retriever.SearchAsync(userId, searchQuery);
        var systemPrompt = stepDef.SystemPrompt;
        if (ragChunks.Count > 0)
            systemPrompt += _retriever.BuildContext(ragChunks);

        var request = new ChatRequest
        {
            Model = modelId,
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = userPrompt },
            },
            SystemPrompt = systemPrompt,
            Temperature = 0.7f,
            MaxTokens = 4096,
        };

        step.Status = "generating";
        await _db.SaveChangesAsync(ct);

        var fullResponse = new System.Text.StringBuilder();
        await foreach (var chunk in provider.StreamAsync(request, apiKey, ct))
        {
            fullResponse.Append(chunk.Delta);
            yield return chunk;
            if (chunk.Done) break;
        }

        step.GeneratedContent = fullResponse.ToString();
        step.Status = "done";
        step.GeneratedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(CancellationToken.None);

        if (stepIndex == 0 && project.Title.StartsWith("New "))
        {
            var input = step.UserInput.Trim();
            project.Title = input.Length > 60 ? input[..57] + "..." : input;
            if (string.IsNullOrWhiteSpace(project.Title))
                project.Title = $"{tool.DisplayName} Project";
            project.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task<string> BuildPreviousStepsContextAsync(
        ResearchProjectEntity project, int currentIndex, ToolDefinition tool)
    {
        if (currentIndex == 0) return "(No previous steps)";

        var steps = await _db.ResearchSteps
            .Where(s => s.ProjectId == project.Id && s.StepIndex < currentIndex && s.Status == "done")
            .OrderBy(s => s.StepIndex)
            .ToListAsync();

        if (steps.Count == 0) return "(No completed previous steps)";

        var parts = steps.Select(s =>
        {
            var label = tool.Steps[s.StepIndex].Label;
            return $"### {label}\n\n**Student Input:** {s.UserInput}\n\n**Generated Content:**\n{s.GeneratedContent}";
        });

        return string.Join("\n\n---\n\n", parts);
    }
}
