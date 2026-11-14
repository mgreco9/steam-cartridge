using Avalonia.Controls;
using Avalonia.Input;
using CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;

namespace CartridgeDesigner.Avalonia.Views.DesignView;

public partial class DesignFolderView : UserControl
{
    private DesignFolderViewModel? _vm => DataContext as DesignFolderViewModel;

    public DesignFolderView()
    {
        InitializeComponent();
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.DataTransfer.TryGetFiles() is { } files)
        {
            foreach (var file in files)
            {
                string path = file.Path.LocalPath;
                _vm?.DropFile(path);
            }
        }
    }
}
