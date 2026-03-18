using System.Windows;
using System.Windows.Controls;
using Epsilon.App.ViewModels;

namespace Epsilon.App.Views;

public partial class DocumentsView : UserControl
{
    public DocumentsView()
    {
        InitializeComponent();
        Loaded += DocumentsView_Loaded;
        Unloaded += DocumentsView_Unloaded;
    }

    private void DocumentsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DocumentsViewModel vm)
            vm.ShowOneDrivePicker += OnShowOneDrivePicker;
    }

    private void DocumentsView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DocumentsViewModel vm)
            vm.ShowOneDrivePicker -= OnShowOneDrivePicker;
    }

    private void OnShowOneDrivePicker()
    {
        if (DataContext is not DocumentsViewModel vm) return;

        var picker = new OneDrivePickerWindow(vm.OneDriveLabel, vm.OneDriveSubfolders)
        {
            Owner = Window.GetWindow(this),
        };
        picker.ShowDialog();

        if (picker.Confirmed)
        {
            _ = vm.LinkSelectedOneDriveFoldersCommand.ExecuteAsync(null);
        }
    }
}
