using System.Windows.Controls;
using Epsilon.App.ViewModels;

namespace Epsilon.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void OpenAiKeyBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.OpenAiKey = ((PasswordBox)sender).Password;
    }

    private void AnthropicKeyBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.AnthropicKey = ((PasswordBox)sender).Password;
    }

    private void GeminiKeyBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.GeminiKey = ((PasswordBox)sender).Password;
    }

    private void ExaKeyBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.ExaKey = ((PasswordBox)sender).Password;
    }
}
