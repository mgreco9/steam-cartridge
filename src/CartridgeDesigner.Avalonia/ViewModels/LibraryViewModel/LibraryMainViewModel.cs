using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CartridgePDFMaker;
using ReactiveUI;
using SteamCartridge.Core;
using SteamCartridge.Core.Workspace;
using SteamCartridgeLauncher;
using SteamGameLoader;

namespace CartridgeDesigner.Avalonia.ViewModels.LibraryViewModel;

public class LibraryMainViewModel : TabViewModel
{
    private WorkspaceManager _ws;
    private CartridgeReader _reader;
    public LibraryFolderViewModel FolderModel { get; }
    public LibraryCartridgeViewModel CartridgeModel { get; }

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public LibraryMainViewModel(WorkspaceManager ws, CartridgeReader reader)
    {
        _ws = ws;
        _reader = reader;
        FolderModel = new LibraryFolderViewModel(ws);
        CartridgeModel = new LibraryCartridgeViewModel(ws, reader)
        {
            Title = "No game selected",
        };

        FolderModel.SelectedItemChanged += OnItemSelected;
        LibraryLoaded = true;
        LoadLibrary();
    }

    private void OnItemSelected(GameData? data)
    {
        if (data == null)
            return;

        ExportEnabled = true;

        CartridgeModel.SelectGame(data);
    }

    /////////////////////////////////
    //// LIBRARY SYNC MANAGEMENT ////
    /////////////////////////////////
    public bool LibraryLoaded 
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public void SyncLibrary_Click()
    {
        LoadLibrary();
    }

    public async void LoadLibrary()
    {
        // 1 - Do not load if already loading
        if(!LibraryLoaded)
            return;

        // 2 - Initialize loader
        SteamLibraryLoader loader = new SteamLibraryLoader(_ws);

        // 3 - Load library
        try
        {
            LibraryLoaded = false;

            await loader.LoadSteamLibrary();
        }
        finally
        {
            FolderModel.Refresh();
            LibraryLoaded = true;
        }
    }

    ///////////////////////////
    //// EXPORT MANAGEMENT ////
    ///////////////////////////
    public bool ExportEnabled{ 
        get; 
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public void ExportCover_Click()
    {
        // 1 - Retrieve the selected games
        List<GameData> selectedGameList = FolderModel.SelectedItem
                .Select(g => g.Game)
                .Where(g => g.DesignState == CartridgeDesignState.DESIGNED)
                .ToList();
        
        // 2 - Retrieve the list of generated cover image
        List<ImageItem> coverList = [];
        foreach(GameData game in selectedGameList)
        {
            ImageItem? cover = _ws.LaunchGameManager(game).LaunchRepositoryManager(Repository.CARTRIDGE).LoadImage("cartridge");
            
            if(cover == null)
                continue;

            coverList.Add(cover);
        }

        // 3 - Initialize PDF maker
        CartridgePDFLib pdfMaker = CartridgePDFLib.InitializePDFMaker(_ws);

        // 4 - Generate PDF
        pdfMaker.GeneratePDF(coverList);
    }

    ////////////////////////////
    //// CLOSING MANAGEMENT ////
    ////////////////////////////
    public override void Dispose()
    {
        CartridgeModel.Dispose();
    }
}