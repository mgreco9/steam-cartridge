using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Media.Imaging;
using ReactiveUI;
using SteamCartridge.Core.Events;
using SteamCartridge.Core.Workspace;
using SteamCartridgeLauncher;

namespace CartridgeDesigner.Avalonia.ViewModels.CartridgeViewModel;

public class CartridgeMainViewModel : TabViewModel
{
    private readonly WorkspaceManager _ws;
    private readonly CartridgeReader _reader;
    private GameData? _currentInsertedGame;

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public CartridgeMainViewModel(WorkspaceManager ws, CartridgeReader reader)
    {
        _ws = ws;
        _reader = reader;
        GameTitle = "";
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "CartridgeWaiting.png");
        CoverImage = new Bitmap(path);

        _reader.CardInserted += OnCartridgeInserted;
        _reader.CardRemoved += OnCartridgeRemoved;
        _ws.GameUpdated += OnGameUpdated;
        _ws.GameCached += OnGameCached;
    }
    
    //////////////////////////
    //// IMAGE MANAGEMENT ////
    //////////////////////////
    public string GameTitle
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public Bitmap? CoverImage
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /////////////////////////////
    //// GAME LAUNCH MANAGER ////
    /////////////////////////////
    public bool LaunchEnabled
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public void LaunchGame_Click()
    {
        string? text = _reader.ReadCard();

        if (text == null || !text.StartsWith("steam:"))
            return;

        string gameId = text[6..];
        Process.Start(new ProcessStartInfo
        {
            FileName = $"steam://run/{gameId}",
            UseShellExecute = true
        });
    }

    public void OnCartridgeInserted()
    {
        UiThread.Run(() =>
        {
            // 1 - Read card
            string? text = _reader.ReadCard();
            if (text == null || !text.StartsWith("steam:"))
            {
                _currentInsertedGame = null;
                LaunchEnabled = false;
                GameTitle = "Unrecognized";
                CoverImage = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Assets", "CartridgeUnrecognized.png"));
                return;
            }

            string gameId = text[6..];

            // 2 - Retrieve game
            if (!_ws.GameExists(gameId))
            {
                _currentInsertedGame = null;
                LaunchEnabled = false;
                GameTitle = "Unrecognized";
                CoverImage = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Assets", "CartridgeUnrecognized.png"));
                return;
            }

            GameManager manager = _ws.LaunchGameManager(gameId);
            _currentInsertedGame = manager.Game;

            // 3 - Load data
            LaunchEnabled = true;
            GameTitle = manager.Game.Name;
            RefreshCover(manager);
        });
    }

    public void OnCartridgeRemoved()
    {
        UiThread.Run(() =>
        {
            _currentInsertedGame = null;
            LaunchEnabled = false;
            GameTitle = "";
            CoverImage = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Assets", "CartridgeWaiting.png"));
        });
    }

    private void RefreshCover(GameManager? manager = null)
    {
        if (_currentInsertedGame == null)
            return;

        manager ??= _ws.LaunchGameManager(_currentInsertedGame);
        ImageItem? image = manager.LaunchRepositoryManager(Repository.CARTRIDGE).LoadImage("cartridge");
        CoverImage = image != null ? new Bitmap(new MemoryStream(image.Thumbnail)) : null;
    }

    private void OnGameUpdated(object? sender, GameCacheChangeEventArgs e)
    {
        if (_currentInsertedGame == null || e.Game.GameID != _currentInsertedGame.GameID)
            return;

        UiThread.Run(() =>
        {
            if (e.PropertyName is nameof(GameData.Name) or null)
                GameTitle = e.Game.Name;

            if (e.PropertyName is nameof(GameData.DesignState) or null)
                RefreshCover();
        });
    }

    private void OnGameCached(object? sender, GameData game)
    {
        if (_currentInsertedGame == null || game.GameID != _currentInsertedGame.GameID)
            return;

        UiThread.Run(() =>
        {
            _currentInsertedGame = game;
            GameTitle = game.Name;
            RefreshCover();
        });
    }

    ////////////////////////////
    //// CLOSING MANAGEMENT ////
    ////////////////////////////
    public override void Dispose()
    {
        _reader.CardInserted -= OnCartridgeInserted;
        _reader.CardRemoved -= OnCartridgeRemoved;
        _ws.GameUpdated -= OnGameUpdated;
        _ws.GameCached -= OnGameCached;
    }
}
