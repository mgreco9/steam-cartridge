using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SteamCartridge.App.ViewModels;

namespace SteamCartridge.App.Views;

public partial class ConfigurationWindow : Window
{
    private ConfigurationWindowModel? _vm => DataContext as ConfigurationWindowModel;

    public ConfigurationWindow()
    {
        InitializeComponent();
    }

    public async void Browse_Click(object sender, RoutedEventArgs e)
    {
        var result = await this.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select Workspace Folder",
                AllowMultiple = false
            });

        var folder = result?.FirstOrDefault();
        if (folder is null)
            return;

        // This is the user-visible path (not always guaranteed on sandboxed platforms)
        _vm?.SaveDirectory = folder.Path.LocalPath;
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        _vm?.Save_Click(this);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        _vm?.Cancel_Click(this);
    }
}