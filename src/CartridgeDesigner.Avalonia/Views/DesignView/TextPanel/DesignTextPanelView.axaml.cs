using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;
using DynamicData.Binding;

namespace CartridgeDesigner.Avalonia.Views.DesignView;

public partial class DesignTextPanelView : UserControl
{
    private DesignTextPanelViewModel? _vm => DataContext as DesignTextPanelViewModel;

    public DesignTextPanelView()
    {
        InitializeComponent();
    }

    private void White_Click(object? sender, RoutedEventArgs e)
    {
        if(_vm == null)
            return;

        _vm.SetWhiteCommand();
    }

    private void Black_Click(object? sender, RoutedEventArgs e)
    {
        if(_vm == null)
            return;

        _vm.SetBlackCommand();
    }
}
