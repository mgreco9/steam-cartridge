using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;
using DynamicData.Binding;

namespace CartridgeDesigner.Avalonia.Views.DesignView;

public partial class DesignPreviewWindow : Window
{
    private bool _allowClose = false;

    public DesignPreviewWindow()
    {
        InitializeComponent();
    }

    public void AllowClose()
    {
        _allowClose = true;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }
}
