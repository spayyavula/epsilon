using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Epsilon.Core.Database;
using Epsilon.Core.LLM;
using Epsilon.Core.Models;

namespace Epsilon.App.ViewModels;

public partial class SolverViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly ProviderRegistry _registry;
    private CancellationTokenSource? _solveCts;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SolveCommand))]
    private string _equationInput = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SolveCommand))]
    private bool _isSolving;

    [ObservableProperty]
    private string _streamingContent = "";

    [ObservableProperty]
    private string _selectedProviderId = "openai";

    [ObservableProperty]
    private string _selectedModelId = "gpt-4o";

    [ObservableProperty]
    private string _statusMessage = "";

    public ObservableCollection<ProviderInfo> Providers { get; } = new();
    public ObservableCollection<ModelInfo> Models { get; } = new();
    public ObservableCollection<string> History { get; } = new();

    public event Action<string>? SolutionStreaming;
    public event Action? SolutionFinished;
    public event Action? ViewCleared;

    public SolverViewModel(DatabaseService db, ProviderRegistry registry)
    {
        _db = db;
        _registry = registry;
        LoadProviders();
        LoadModels();
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
            foreach (var m in models) Models.Add(m);
            if (Models.Count > 0 && !Models.Any(m => m.Id == SelectedModelId))
                SelectedModelId = Models[0].Id;
        }
        catch { }
    }

    partial void OnSelectedProviderIdChanged(string value) => LoadModels();

    [RelayCommand(CanExecute = nameof(CanSolve))]
    private async Task Solve()
    {
        var equation = EquationInput.Trim();
        if (string.IsNullOrEmpty(equation)) return;

        var provider = _registry.Get(SelectedProviderId);
        if (provider == null) { StatusMessage = "No provider configured."; return; }

        var apiKey = _db.GetSetting($"{SelectedProviderId}_api_key");

        IsSolving = true;
        StreamingContent = "";
        StatusMessage = "Solving...";
        _solveCts = new CancellationTokenSource();

        // Add to history
        if (!History.Contains(equation))
        {
            History.Insert(0, equation);
            if (History.Count > 20) History.RemoveAt(History.Count - 1);
        }

        var request = new ChatRequest
        {
            Model = SelectedModelId,
            Messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = equation },
            },
            SystemPrompt = SolverSystemPrompt,
            Temperature = 0.3f,
            MaxTokens = 4096,
        };

        try
        {
            var fullResponse = new System.Text.StringBuilder();

            await foreach (var chunk in provider.StreamAsync(request, apiKey, _solveCts.Token))
            {
                if (chunk.Done) break;
                fullResponse.Append(chunk.Delta);
                StreamingContent = fullResponse.ToString();
                SolutionStreaming?.Invoke(StreamingContent);
            }

            SolutionFinished?.Invoke();
            StatusMessage = "Solution complete.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled.";
            SolutionFinished?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            SolutionFinished?.Invoke();
        }
        finally
        {
            IsSolving = false;
            _solveCts?.Dispose();
            _solveCts = null;
        }
    }

    private bool CanSolve() => !IsSolving && !string.IsNullOrWhiteSpace(EquationInput);

    partial void OnEquationInputChanged(string value) => SolveCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void Cancel() => _solveCts?.Cancel();

    [RelayCommand]
    private void Clear()
    {
        EquationInput = "";
        StreamingContent = "";
        StatusMessage = "";
        ViewCleared?.Invoke();
    }

    [RelayCommand]
    private void LoadFromHistory(string equation)
    {
        EquationInput = equation;
    }

    private const string SolverSystemPrompt = """
        You are Epsilon, an expert equation solver and mathematical computation engine.

        When solving equations or mathematical expressions:

        1. **IDENTIFY** the equation type and state it clearly
        2. **SHOW EVERY STEP** as a numbered step with a clear label
        3. **FORMAT** each step as:
           ### Step N: [Description]
           [Show the mathematical work with LaTeX]
           [Brief explanation of what was done]

        4. **USE LATEX** for all mathematics: inline $...$ and display $$...$$
        5. **BOX THE FINAL ANSWER**: $$\boxed{answer}$$
        6. **VERIFY** the answer by substituting back or checking

        You can solve:
        - Linear equations and systems of equations
        - Quadratic, cubic, and polynomial equations
        - Trigonometric equations
        - Exponential and logarithmic equations
        - Differential equations (ODE and PDE)
        - Integrals (definite and indefinite)
        - Limits
        - Series and sequences
        - Matrix equations
        - Inequalities
        - Optimization problems

        For each step, show the transformation clearly:
        $$\text{previous expression} \implies \text{next expression}$$

        If the problem has multiple solutions, find ALL of them.
        If the problem has no solution, explain why.
        If the input is an expression (not an equation), simplify it fully.

        Be precise with mathematical notation. Show your work clearly so a student
        can follow every step and learn the technique.
        """;
}
