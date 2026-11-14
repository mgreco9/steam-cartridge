using Avalonia.Controls;
using Avalonia.Interactivity;
using CartridgeDesigner.Avalonia.ViewModels.LibraryViewModel;

namespace CartridgeDesigner.Avalonia.Views.LibraryView;

public partial class LibraryCartridgeView : UserControl
{
    private LibraryCartridgeViewModel? _vm => DataContext as LibraryCartridgeViewModel;

    public LibraryCartridgeView()
    {
        InitializeComponent();
    }

    public void AssignCartridge_Click(object sender, RoutedEventArgs e)
    {
        _vm?.AssignCartridge_Click();
    }
    
    public void EditCover_Click(object sender, RoutedEventArgs e)
    {
        _vm?.EditCover_Click();
    }
}