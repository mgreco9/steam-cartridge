using System.ComponentModel;
using System.IO;

using Avalonia.Media.Imaging;

using CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;
using CartridgeDesigner.Avalonia.Views.DesignView;

using ReactiveUI;

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

        _ws.GameCached += OnGameCached;
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
        if (game == _gameSelected)
            return;

        // 2 - Load the game. Follow later cache edits (the title is a copy of Game.Name).
        _gameSelected?.PropertyChanged -= OnSelectedGameChanged;

        _gameSelected = game;
        _gameSelected.PropertyChanged += OnSelectedGameChanged;
        _gameManager = _ws.LaunchGameManager(game);

        // 3 - Retrieve title 
        Title = game.Name;

        // 4 - Retrieve cover image if any
        RefreshCover();

        // 5 - Activate edit cover
        EditEnabled = true;
    }

    private void RefreshCover()
    {
        if (_gameSelected == null || _gameManager == null)
        {
            CoverImage = null;
            return;
        }

        ImageItem? image = _gameManager.LaunchRepositoryManager(Repository.CARTRIDGE).LoadImage("cartridge");
        CoverImage = image != null ? new Bitmap(new MemoryStream(image.Thumbnail)) : null;
    }

    private void OnSelectedGameChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not GameData game || !ReferenceEquals(game, _gameSelected))
            return;

        if (e.PropertyName is nameof(GameData.Name) or null)
            UiThread.Run(() => Title = game.Name);

        if (e.PropertyName is nameof(GameData.DesignState) or null)
            UiThread.Run(RefreshCover);
    }

    private void OnGameCached(object? sender, GameData game)
    {
        if (_gameSelected != null && game.GameID == _gameSelected.GameID && !ReferenceEquals(game, _gameSelected))
        {
            UiThread.Run(() =>
            {
                if (_gameSelected != null)
                    _gameSelected.PropertyChanged -= OnSelectedGameChanged;

                _gameSelected = game;
                _gameSelected.PropertyChanged += OnSelectedGameChanged;
                _gameManager = _ws.LaunchGameManager(game);
                Title = game.Name;
                RefreshCover();
            });
        }
    }

    //////////////////////////
    //// TITLE MANAGEMENT ////
    //////////////////////////
    public required string Title
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    //////////////////////////
    //// IMAGE MANAGEMENT ////
    //////////////////////////
    public Bitmap? CoverImage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    ////////////////////////////////////
    //// ASSIGN CARTIDGE MANAGEMENT ////
    ////////////////////////////////////
    public bool AssignEnabled
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public void AssignCartridge_Click()
    {
        if (_gameSelected == null)
            return;

        _reader.WriteCard($"steam:{_gameSelected.GameID}");

        // Library list and ignore button follow this through GameUpdated.
        _gameSelected.PhysicalState = CartridgePhysicalState.ASSIGNED;
        _gameManager?.UpdateGame();
    }

    public void OnCartridgeInserted()
    {
        UiThread.Run(() => AssignEnabled = true);
    }

    public void OnCartridgeRemoved()
    {
        UiThread.Run(() => AssignEnabled = false);
    }

    ///////////////////////////////
    //// EDIT COVER MANAGEMENT ////
    ///////////////////////////////
    public bool EditEnabled
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
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
        if (_gameSelected != null)
            _gameSelected.PropertyChanged -= OnSelectedGameChanged;

        _ws.GameCached -= OnGameCached;
        _reader.CardInserted -= OnCartridgeInserted;
        _reader.CardRemoved -= OnCartridgeRemoved;
    }
}