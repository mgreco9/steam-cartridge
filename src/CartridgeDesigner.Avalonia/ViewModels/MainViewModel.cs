using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Controls;
using CartridgeDesigner.Avalonia.ViewModels.CartridgeViewModel;
using CartridgeDesigner.Avalonia.ViewModels.ClassificationViewModel;
using CartridgeDesigner.Avalonia.ViewModels.LibraryViewModel;
using SteamCartridge.Core.Workspace;
using SteamCartridgeLauncher;

namespace CartridgeDesigner.Avalonia.ViewModels;

public class MainViewModel
{
    private readonly WorkspaceManager _ws;

    public MainViewModel(WorkspaceManager ws)
    {
        _ws = ws;
    }

    public static MainViewModel Initialize(WorkspaceManager ws, CartridgeReader reader)
    {
        // 1 - Initialize tabs
        List<TabViewModel> tabs =
        [
            new CartridgeMainViewModel(ws, reader)
            {
                Title = "Cartridge"
            },
            new LibraryMainViewModel(ws, reader)
            {
                Title = "Library"
            },
            new ClassificationMainViewModel(ws)
            {
                Title = "Classification"
            }
        ];

        // 2 - Build Main view
        return new MainViewModel(ws)
        {
            Tabs = tabs
        };
    }


    ////////////////////////
    //// TAB MANAGEMENT ////
    ////////////////////////
    public required List<TabViewModel> Tabs { get; init; }

    ////////////////////////////
    //// CLOSING MANAGEMENT ////
    ////////////////////////////
    public void Dispose()
    {
        foreach(TabViewModel tab in Tabs)
        {
            tab.Dispose();
        }
    }
}