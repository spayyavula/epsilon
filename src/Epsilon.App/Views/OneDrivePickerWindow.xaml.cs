using System.Collections.ObjectModel;
using System.Windows;
using Epsilon.Core.Documents;

namespace Epsilon.App.Views;

public partial class OneDrivePickerWindow : Window
{
    public bool Confirmed { get; private set; }

    public string OneDrivePath { get; }
    public ObservableCollection<OneDriveFolder> Folders { get; }

    public OneDrivePickerWindow(string oneDrivePath, ObservableCollection<OneDriveFolder> folders)
    {
        OneDrivePath = oneDrivePath;
        Folders = folders;
        DataContext = this;
        InitializeComponent();
    }

    private void LinkButton_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
