using Avalonia.Controls;
using Avalonia.Interactivity;
using CartridgeDesigner.Avalonia.ViewModels.LibraryViewModel;

namespace CartridgeDesigner.Avalonia.Views.LibraryView;

public partial class LibraryMainView : UserControl
{
    private LibraryMainViewModel? _vm => DataContext as LibraryMainViewModel;

    public LibraryMainView()
    {
        InitializeComponent();
    }

    public void SyncLibrary_Click(object sender, RoutedEventArgs e)
    {
        _vm?.SyncLibrary_Click();
    }

    public void ExportCover_Click(object sender, RoutedEventArgs e)
    {
        _vm?.ExportCover_Click();
    }
    
}