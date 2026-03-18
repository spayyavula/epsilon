using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Epsilon.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _activeTab = "Chat";

    public ChatViewModel Chat { get; }
    public SettingsViewModel Settings { get; }
    public DocumentsViewModel Documents { get; }
    public ResearchToolkitViewModel Toolkit { get; }
    public SolverViewModel Solver { get; }
    public FlashcardViewModel Flashcards { get; }

    public MainViewModel()
    {
        Chat = App.Services.GetRequiredService<ChatViewModel>();
        Settings = App.Services.GetRequiredService<SettingsViewModel>();
        Documents = App.Services.GetRequiredService<DocumentsViewModel>();
        Toolkit = App.Services.GetRequiredService<ResearchToolkitViewModel>();
        Solver = App.Services.GetRequiredService<SolverViewModel>();
        Flashcards = App.Services.GetRequiredService<FlashcardViewModel>();
        CurrentView = Chat;
    }

    [RelayCommand]
    private void NavigateToChat()
    {
        ActiveTab = "Chat";
        CurrentView = Chat;
    }

    [RelayCommand]
    private void NavigateToDocuments()
    {
        ActiveTab = "Documents";
        CurrentView = Documents;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        ActiveTab = "Settings";
        CurrentView = Settings;
    }

    [RelayCommand]
    private void NavigateToToolkit()
    {
        ActiveTab = "Toolkit";
        CurrentView = Toolkit;
    }

    [RelayCommand]
    private void NavigateToSolver()
    {
        ActiveTab = "Solver";
        CurrentView = Solver;
    }

    [RelayCommand]
    private void NavigateToFlashcards()
    {
        ActiveTab = "Flashcards";
        CurrentView = Flashcards;
    }

    /// <summary>
    /// param format: "ToolType|equation text"
    /// Navigates to the Research Toolkit and opens a new project pre-filled with the equation.
    /// </summary>
    [RelayCommand]
    private void NavigateToToolWithContext(string param)
    {
        var parts = param.Split('|', 2);
        var toolType = parts.Length > 0 ? parts[0] : "ConceptExplorer";
        var prefill = parts.Length > 1 ? parts[1] : "";

        ActiveTab = "Toolkit";
        CurrentView = Toolkit;
        Toolkit.NewProjectWithContext(toolType, prefill);
    }
}
