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

    public MainViewModel()
    {
        Chat = App.Services.GetRequiredService<ChatViewModel>();
        Settings = App.Services.GetRequiredService<SettingsViewModel>();
        Documents = App.Services.GetRequiredService<DocumentsViewModel>();
        Toolkit = App.Services.GetRequiredService<ResearchToolkitViewModel>();
        Solver = App.Services.GetRequiredService<SolverViewModel>();
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
}
