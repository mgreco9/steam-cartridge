using System;

using Avalonia.Controls;
using Avalonia.Interactivity;
using CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;

namespace CartridgeDesigner.Avalonia.Views.DesignView;

public partial class DesignMainView : Window
{
    private DesignMainViewModel? _vm => DataContext as DesignMainViewModel;
    public required DesignPreviewWindow PreviewWindow {get; init;}

    public DesignMainView()
    {
        InitializeComponent();
    }

    private void Validate_Click(object sender, RoutedEventArgs e)
    {
        _vm?.ValidateCommand();
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _vm?.CancelCommand();
        Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if(!e.IsProgrammatic)
            _vm?.CancelCommand();

        PreviewWindow.AllowClose();
        PreviewWindow.Close();
    }
}
