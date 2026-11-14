using System;

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CartridgeDesigner.Avalonia.Views.DesignView;

public partial class DesignImagePanelView : UserControl
{
    public event Action? RequestClose;

    public DesignImagePanelView()
    {
        InitializeComponent();
    }

    private void Validate_Click(object sender, RoutedEventArgs e)
    {

        RequestClose?.Invoke();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        RequestClose?.Invoke();
    }
}
