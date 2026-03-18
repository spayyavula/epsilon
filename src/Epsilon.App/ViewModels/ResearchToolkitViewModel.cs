using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Epsilon.Core.Database;
using Epsilon.Core.LLM;
using Epsilon.Core.Models;
using Epsilon.Core.Research;

namespace Epsilon.App.ViewModels;

public partial class ResearchToolkitViewModel : ObservableObject
{
    private readonly ResearchService _service;
    private readonly DatabaseService _db;
    private readonly ProviderRegistry _registry;
    private readonly string _docsDir;
    private CancellationTokenSource? _generationCts;

    // Tool list (static)
    public IReadOnlyList<ToolDefinition> Tools { get; } = ToolRegistry.GetAll();

    // Project list
    public ObservableCollection<ResearchProject> Projects { get; } = new();

    // Active project state
    [ObservableProperty]
    private ResearchProject? _activeProject;

    [ObservableProperty]
    private ToolDefinition? _activeTool;

    [ObservableProperty]
    private bool _showProjectList = true;

    [ObservableProperty]
    private int _currentStepIndex;

    [ObservableProperty]
    private string _currentStepInput = "";

    [ObservableProperty]
    private string _currentStepLabel = "";

    [ObservableProperty]
    private string _currentInputLabel = "";

    [ObservableProperty]
    private string? _currentInputPlaceholder;

    [ObservableProperty]
    private bool _isAutoGenerateStep;

    [ObservableProperty]
    private bool _currentStepHasContent;

    [ObservableProperty]
    private string _currentStepStatus = "empty";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateStepCommand))]
    private bool _isGenerating;

    [ObservableProperty]
    private string _streamingContent = "";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private string _selectedProviderId = "openai";

    [ObservableProperty]
    private string _selectedModelId = "gpt-4o";

    [ObservableProperty]
    private bool _webSearchEnabled;

    [ObservableProperty]
    private bool _webSearchAvailable;

    [ObservableProperty]
    private bool _isLastStep;

    [ObservableProperty]
    private bool _isFirstStep = true;

    [ObservableProperty]
    private int _totalSteps;

    public ObservableCollection<ProviderInfo> Providers { get; } = new();
    public ObservableCollection<ModelInfo> Models { get; } = new();
    public ObservableCollection<ResearchStepInfo> StepList { get; } = new();

    // Events for WebView2 bridge
    public event Action<string>? StepContentStreaming;
    public event Action? StepContentFinished;
    public event Action<string>? StepContentReady;
    public event Action? ViewCleared;

    private List<ResearchStep> _steps = new();

    public ResearchToolkitViewModel(ResearchService service, DatabaseService db,
        ProviderRegistry registry, AppConfig config)
    {
        _service = service;
        _db = db;
        _registry = registry;
        _docsDir = config.DocsDirectory;

        WebSearchAvailable = !string.IsNullOrEmpty(_db.GetSetting("exa_api_key"));
        LoadProviders();
        LoadProjects();
    }

    private void LoadProjects()
    {
        Projects.Clear();
        foreach (var p in _service.ListProjects())
            Projects.Add(p);
    }

    private void LoadProviders()
    {
        Providers.Clear();
        foreach (var p in _registry.ListProviders())
            Providers.Add(p);
    }

    private async void LoadModels()
    {
        Models.Clear();
        var provider = _registry.Get(SelectedProviderId);
        if (provider == null) return;

        var apiKey = _db.GetSetting($"{SelectedProviderId}_api_key");
        try
        {
            var models = await provider.ListModelsAsync(apiKey);
            foreach (var m in models)
                Models.Add(m);
            if (Models.Count > 0 && !Models.Any(m => m.Id == SelectedModelId))
                SelectedModelId = Models[0].Id;
        }
        catch { }
    }

    partial void OnSelectedProviderIdChanged(string value) => LoadModels();

    // --- Commands ---

    [RelayCommand]
    private void NewProject(string toolType)
    {
        var project = _service.CreateProject(toolType, SelectedProviderId, SelectedModelId);
        Projects.Insert(0, project);
        OpenProjectInternal(project);
    }

    [RelayCommand]
    private void OpenProject(ResearchProject project) => OpenProjectInternal(project);

    private void OpenProjectInternal(ResearchProject project)
    {
        ActiveProject = project;
        ActiveTool = ToolRegistry.Get(project.ToolType);
        if (ActiveTool == null) return;

        SelectedProviderId = project.ProviderId ?? "openai";
        SelectedModelId = project.ModelId ?? "gpt-4o";
        WebSearchEnabled = project.WebSearchEnabled;
        TotalSteps = ActiveTool.Steps.Count;

        _steps = _service.GetSteps(project.Id);

        // Build step list for sidebar
        StepList.Clear();
        foreach (var stepDef in ActiveTool.Steps)
        {
            var step = _steps.FirstOrDefault(s => s.StepIndex == stepDef.Index);
            StepList.Add(new ResearchStepInfo
            {
                Index = stepDef.Index,
                Label = stepDef.Label,
                Status = step?.Status ?? "empty",
                IsCompleted = step?.Status == "done",
            });
        }

        LoadModels();
        ShowProjectList = false;
        NavigateToStep(project.CurrentStep);
    }

    [RelayCommand]
    private void DeleteProject(ResearchProject project)
    {
        _service.DeleteProject(project.Id);
        Projects.Remove(project);
        if (ActiveProject?.Id == project.Id)
            BackToList();
    }

    [RelayCommand]
    private void BackToList()
    {
        SaveCurrentInput();
        ActiveProject = null;
        ActiveTool = null;
        ShowProjectList = true;
        ViewCleared?.Invoke();
        LoadProjects();
    }

    [RelayCommand]
    private void NavigateToStep(int index)
    {
        if (ActiveTool == null || ActiveProject == null) return;
        if (index < 0 || index >= ActiveTool.Steps.Count) return;

        SaveCurrentInput();

        CurrentStepIndex = index;
        var stepDef = ActiveTool.Steps[index];
        var step = _steps.FirstOrDefault(s => s.StepIndex == index);

        CurrentStepLabel = stepDef.Label;
        CurrentInputLabel = stepDef.InputLabel;
        CurrentInputPlaceholder = stepDef.InputPlaceholder;
        IsAutoGenerateStep = stepDef.IsAutoGenerate;
        CurrentStepInput = step?.UserInput ?? "";
        CurrentStepStatus = step?.Status ?? "empty";
        CurrentStepHasContent = !string.IsNullOrEmpty(step?.GeneratedContent);
        IsFirstStep = index == 0;
        IsLastStep = index == ActiveTool.Steps.Count - 1;

        // Update sidebar
        foreach (var s in StepList)
            s.IsActive = s.Index == index;

        // Update project
        ActiveProject.CurrentStep = index;
        _service.UpdateProject(ActiveProject);

        // Show content in WebView2 if step is done
        if (CurrentStepHasContent && step != null)
            StepContentReady?.Invoke(step.GeneratedContent);
        else
            ViewCleared?.Invoke();
    }

    [RelayCommand]
    private void NextStep()
    {
        if (ActiveTool == null) return;
        if (CurrentStepIndex < ActiveTool.Steps.Count - 1)
            NavigateToStep(CurrentStepIndex + 1);
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStepIndex > 0)
            NavigateToStep(CurrentStepIndex - 1);
    }

    [RelayCommand(CanExecute = nameof(CanGenerate))]
    private async Task GenerateStep()
    {
        if (ActiveProject == null || ActiveTool == null) return;

        SaveCurrentInput();

        var step = _steps.FirstOrDefault(s => s.StepIndex == CurrentStepIndex);
        if (step == null) return;

        // Update project provider settings
        ActiveProject.ProviderId = SelectedProviderId;
        ActiveProject.ModelId = SelectedModelId;
        ActiveProject.WebSearchEnabled = WebSearchEnabled;
        _service.UpdateProject(ActiveProject);

        IsGenerating = true;
        StreamingContent = "";
        CurrentStepStatus = "generating";
        _generationCts = new CancellationTokenSource();

        try
        {
            await foreach (var chunk in _service.StreamStepAsync(
                ActiveProject, step, _generationCts.Token))
            {
                if (chunk.Done) break;
                StreamingContent += chunk.Delta;
                StepContentStreaming?.Invoke(StreamingContent);
            }

            // Update local state
            CurrentStepStatus = "done";
            CurrentStepHasContent = true;
            StepContentReady?.Invoke(step.GeneratedContent);

            // Update sidebar
            var listItem = StepList.FirstOrDefault(s => s.Index == CurrentStepIndex);
            if (listItem != null)
            {
                listItem.Status = "done";
                listItem.IsCompleted = true;
            }

            // Update project title
            if (CurrentStepIndex == 0)
                LoadProjects();

            StatusMessage = $"{CurrentStepLabel} generated successfully.";
        }
        catch (OperationCanceledException)
        {
            step.Status = string.IsNullOrEmpty(step.GeneratedContent) ? "empty" : "done";
            _service.SaveStep(step);
            CurrentStepStatus = step.Status;
            StatusMessage = "Generation cancelled.";
            StepContentFinished?.Invoke();
        }
        catch (Exception ex)
        {
            step.Status = "error";
            _service.SaveStep(step);
            CurrentStepStatus = "error";
            StatusMessage = $"Error: {ex.Message}";
            StepContentFinished?.Invoke();
        }
        finally
        {
            IsGenerating = false;
            StreamingContent = "";
            _generationCts?.Dispose();
            _generationCts = null;
        }
    }

    private bool CanGenerate()
    {
        if (IsGenerating) return false;
        if (ActiveTool == null) return false;

        var stepDef = ActiveTool.Steps[CurrentStepIndex];
        // Auto-generate steps don't need input
        if (stepDef.IsAutoGenerate) return true;

        return !string.IsNullOrWhiteSpace(CurrentStepInput);
    }

    partial void OnCurrentStepInputChanged(string value)
    {
        GenerateStepCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void CancelGeneration()
    {
        _generationCts?.Cancel();
    }

    [RelayCommand]
    private void ExportProject()
    {
        if (ActiveProject == null || ActiveTool == null) return;

        try
        {
            SaveCurrentInput();
            var steps = _service.GetSteps(ActiveProject.Id);
            var filePath = ResearchPdfExporter.Export(ActiveProject, steps, ActiveTool, _docsDir);

            ActiveProject.Status = "exported";
            _service.UpdateProject(ActiveProject);

            StatusMessage = $"Exported to {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportLatex()
    {
        if (ActiveProject == null || ActiveTool == null) return;

        try
        {
            SaveCurrentInput();
            var steps = _service.GetSteps(ActiveProject.Id);
            var filePath = LatexExporter.Export(ActiveProject, steps, ActiveTool, _docsDir);

            StatusMessage = $"LaTeX exported to {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"LaTeX export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportLean()
    {
        if (ActiveProject == null || ActiveTool == null) return;

        try
        {
            SaveCurrentInput();
            var steps = _service.GetSteps(ActiveProject.Id);

            // Find LeanBridge step content (step index 1 has the Lean code)
            var leanStep = steps.FirstOrDefault(s => s.StepIndex == 1 && !string.IsNullOrEmpty(s.GeneratedContent))
                        ?? steps.FirstOrDefault(s => !string.IsNullOrEmpty(s.GeneratedContent));

            if (leanStep == null)
            {
                StatusMessage = "No generated content to export.";
                return;
            }

            var filePath = LeanExporter.ExportLeanProject(leanStep.GeneratedContent, ActiveProject.Title, _docsDir);
            StatusMessage = $"Lean file exported to {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lean export failed: {ex.Message}";
        }
    }

    public void NewProjectWithContext(string toolType, string prefill)
    {
        var project = _service.CreateProject(toolType, SelectedProviderId, SelectedModelId);
        Projects.Insert(0, project);
        OpenProjectInternal(project);

        // Pre-fill step 0
        if (!string.IsNullOrWhiteSpace(prefill))
        {
            CurrentStepInput = prefill;
            var step = _steps.FirstOrDefault(s => s.StepIndex == 0);
            if (step != null)
            {
                step.UserInput = prefill;
                _service.SaveStep(step);
            }
        }
    }

    private void SaveCurrentInput()
    {
        if (ActiveProject == null) return;

        var step = _steps.FirstOrDefault(s => s.StepIndex == CurrentStepIndex);
        if (step != null && step.UserInput != CurrentStepInput)
        {
            step.UserInput = CurrentStepInput;
            _service.SaveStep(step);
        }
    }
}

// Lightweight step info for the sidebar
public partial class ResearchStepInfo : ObservableObject
{
    public int Index { get; set; }
    public string Label { get; set; } = "";

    [ObservableProperty]
    private string _status = "empty";

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private bool _isActive;
}
