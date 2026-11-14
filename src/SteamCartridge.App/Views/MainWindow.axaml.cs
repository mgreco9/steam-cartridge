using Avalonia.Controls;
using SteamCartridge.App.ViewModels;

namespace SteamCartridge.App.Views;

public partial class MainWindow : Window
{
    private MainWindowModel? _vm => DataContext as MainWindowModel;

    public MainWindow()
    {
        InitializeComponent();
    }
}