using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Epsilon.App.ViewModels;
using Epsilon.Core.Database;
using Wpf.Ui.Controls;

namespace Epsilon.App.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var db = App.Services.GetRequiredService<DatabaseService>();
        var tourSeen = db.GetSetting("welcome_tour_seen");

        if (tourSeen != "true")
        {
            ShowWelcomeTour();
        }
    }

    private void ShowWelcomeTour()
    {
        var tour = new WelcomeTourWindow { Owner = this };
        tour.ShowDialog();

        if (tour.DontShowAgain)
        {
            var db = App.Services.GetRequiredService<DatabaseService>();
            db.SetSetting("welcome_tour_seen", "true");
        }
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        ShowWelcomeTour();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
