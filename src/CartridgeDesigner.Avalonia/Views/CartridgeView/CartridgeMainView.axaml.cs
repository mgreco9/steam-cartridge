using Avalonia.Controls;
using Avalonia.Interactivity;
using CartridgeDesigner.Avalonia.ViewModels.CartridgeViewModel;

namespace CartridgeDesigner.Avalonia.Views.CartridgeView;

public partial class CartridgeMainView : UserControl
{
    private CartridgeMainViewModel? _vm => DataContext as CartridgeMainViewModel;
    
    public CartridgeMainView()
    {
        InitializeComponent();
    }

    public void LaunchGame_Click(object? sender, RoutedEventArgs e)
    {
        _vm?.LaunchGame_Click();
    }
}