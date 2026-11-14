using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;
using DynamicData.Binding;

namespace CartridgeDesigner.Avalonia.Views.DesignView;

public partial class DesignBlurredPanelView : UserControl
{
    private DesignBlurredPanelViewModel? _vm => DataContext as DesignBlurredPanelViewModel;

    public DesignBlurredPanelView()
    {
        InitializeComponent();
    }
}
