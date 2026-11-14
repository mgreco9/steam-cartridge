using Avalonia.Controls;
using Avalonia.Input;
using CartridgeDesigner.Avalonia.ViewModels.LibraryViewModel;

namespace CartridgeDesigner.Avalonia.Views.LibraryView;

public partial class LibraryFolderView : UserControl
{
    private LibraryFolderViewModel? _vm => DataContext as LibraryFolderViewModel;

    public LibraryFolderView()
    {
        InitializeComponent();
    }

    private void OnItemPressed(object? sender, PointerPressedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not GameViewModel item)
            return;

        KeyModifiers key = KeyModifiers.None;
        if(e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            key = KeyModifiers.Shift;
        else if(e.KeyModifiers.HasFlag(KeyModifiers.Control))
            key = KeyModifiers.Control;

        _vm?.SelectItem(item, key);
    }
}
