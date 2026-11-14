using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using Avalonia.Controls;
using Avalonia.Media.Imaging;

using CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;
using CartridgeDesigner.Avalonia.Views.DesignView;

using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using SteamCartridge.Core;
using SteamCartridge.Core.Workspace;
using SteamCartridgeLauncher;

namespace CartridgeDesigner.Avalonia.ViewModels.LibraryViewModel;

public class LibraryCartridgeViewModel : ReactiveObject
{
    private WorkspaceManager _ws;
    private CartridgeReader _reader;

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public LibraryCartridgeViewModel(WorkspaceManager ws, CartridgeReader reader)
    {
        _ws = ws;
        _reader = reader;
        AssignEnabled = false;
        EditEnabled = false;

        _reader.CardInserted += OnCartridgeInserted;
        _reader.CardRemoved += OnCartridgeRemoved;
    }

    /////////////////////////
    //// DATA MANAGEMENT ////
    /////////////////////////
    private GameData? _gameSelected;
    private GameManager? _gameManager;

    public void SelectGame(GameData game)
    {
        // 1 - If same game, nothing to do
        if(game == _gameSelected)
            return;

        // 2 - Load the game
        _gameSelected = game;
        _gameManager = _ws.LaunchGameManager(game);

        // 3 - Retrieve title 
        Title = game.Name;

        // 4 - Retrieve cover image if any
        ImageItem? image = _gameManager.LaunchRepositoryManager(Repository.CARTRIDGE).LoadImage("cartridge");
        if(image!=null)
            CoverImage = new Bitmap(new MemoryStream(image.Thumbnail));
        else
            CoverImage = null;

        // 5 - Activate edit cover
        EditEnabled = true;
    }

    //////////////////////////
    //// TITLE MANAGEMENT ////
    //////////////////////////
    public required string Title
    {
        get;
        set =>this.RaiseAndSetIfChanged(ref field, value);
    }

    //////////////////////////
    //// IMAGE MANAGEMENT ////
    //////////////////////////
    public Bitmap? CoverImage
    {
        get;
        set =>this.RaiseAndSetIfChanged(ref field, value);
    }

    ////////////////////////////////////
    //// ASSIGN CARTIDGE MANAGEMENT ////
    ////////////////////////////////////
    public bool AssignEnabled
    {
        get;
        private set =>this.RaiseAndSetIfChanged(ref field, value);
    }

    public void AssignCartridge_Click()
    {
        if (_gameSelected == null)
            return;

        _reader.WriteCard($"steam:{_gameSelected.GameID}");

        _gameSelected.PhysicalState = CartridgePhysicalState.ASSIGNED;
        _gameManager?.UpdateGame();
    }

    public void OnCartridgeInserted()
    {
        AssignEnabled = true;
    }

    public void OnCartridgeRemoved()
    {
        AssignEnabled = false;
    }

    ///////////////////////////////
    //// EDIT COVER MANAGEMENT ////
    ///////////////////////////////
    public bool EditEnabled
    {
        get;
        private set =>this.RaiseAndSetIfChanged(ref field, value);
    }

    public void EditCover_Click()
    {
        if (_gameSelected == null)
            return;

        DesignPreviewWindowModel previewWindowModel = new DesignPreviewWindowModel();
        DesignPreviewWindow previewWindow = new DesignPreviewWindow()
        {
            DataContext = previewWindowModel
        };

        DesignMainView window = new DesignMainView()
        {
            PreviewWindow = previewWindow,
            DataContext = DesignMainViewModel.CreateDesignView(_ws, _gameSelected, previewWindowModel)
        };

        window.Show();
        previewWindow.Show();
    }


    ////////////////////////////
    //// CLOSING MANAGEMENT ////
    ////////////////////////////
    public void Dispose()
    {
        _reader.CardInserted -= OnCartridgeInserted;
        _reader.CardRemoved -= OnCartridgeRemoved;
    }
}