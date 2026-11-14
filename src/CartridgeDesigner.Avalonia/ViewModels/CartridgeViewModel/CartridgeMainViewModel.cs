
using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Media.Imaging;
using ReactiveUI;
using SteamCartridge.Core.Workspace;
using SteamCartridgeLauncher;

namespace CartridgeDesigner.Avalonia.ViewModels.CartridgeViewModel;

public class CartridgeMainViewModel : TabViewModel
{
    private WorkspaceManager _ws;
    private CartridgeReader _reader;

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
        reader.CardInserted += OnCartridgeInserted;
        reader.CardRemoved += OnCartridgeRemoved;
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
        private set =>this.RaiseAndSetIfChanged(ref field, value);
    }

    public void LaunchGame_Click()
    {
        string? text = _reader.ReadCard();

        if(text == null)
            return;

        if(!text.StartsWith("steam:"))
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
        // 1 - Read card
        string? text = _reader.ReadCard();
        if(text == null)
        {
            GameTitle = "Unrecognized";
            CoverImage = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Assets", "CartridgeUnrecognized.png"));
            return;
        }

        // 2 - Retrieve game id
        if(!text.StartsWith("steam:"))
        {
            GameTitle = "Unrecognized";
            CoverImage = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Assets", "CartridgeUnrecognized.png"));
            return;
        }
        string gameId = text[6..];

        // 3 - Retrieve game
        if(!_ws.GameExists(gameId))
        {
            GameTitle = "Unrecognized";
            CoverImage = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Assets", "CartridgeUnrecognized.png"));
            return;
        }
        GameManager manager = _ws.LaunchGameManager(gameId);

        // 4 - Load data
        LaunchEnabled = true;
        GameTitle = manager.Game.Name;
        ImageItem? image = manager.LaunchRepositoryManager(Repository.CARTRIDGE).LoadImage("cartridge");
        if(image == null)
            CoverImage = null;
        else
            CoverImage = new Bitmap(new MemoryStream(image.Thumbnail));
    }

    public void OnCartridgeRemoved()
    {
        LaunchEnabled = false;
        GameTitle = "";
        CoverImage = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Assets", "CartridgeWaiting.png"));
    }

    ////////////////////////////
    //// CLOSING MANAGEMENT ////
    ////////////////////////////
    public override void Dispose()
    {
        _reader.CardInserted -= OnCartridgeInserted;
        _reader.CardRemoved -= OnCartridgeRemoved;
    }
}