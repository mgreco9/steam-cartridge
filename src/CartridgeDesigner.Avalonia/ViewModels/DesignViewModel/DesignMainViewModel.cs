using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;

using SteamCartridge.Core.Workspace;

namespace CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;

public class DesignMainViewModel
{
    private GameManager _ws;
    private DesignPreviewWindowModel _preview;
    public ObservableCollection<TabViewModel> Tabs { get; }

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public DesignMainViewModel(GameManager ws, ICollection<TabViewModel> tabList, DesignPreviewWindowModel preview)
    {
        _ws = ws;
        _preview = preview;

        Tabs = [];
        foreach (TabViewModel tab in tabList)
        {
            Tabs.Add(tab);
        }
    }

    public static DesignMainViewModel CreateDesignView(WorkspaceManager ws, GameData game, DesignPreviewWindowModel previewModel)
    {
        // 1 - Retrieve the json layout
        JsonDocument jsonDocument = ws.GetLayoutJson();
        GameManager gameManager = ws.LaunchGameManagerLocally(game.GameID);

        // 2 - Iterate over each element of the json and add it to a list
        List<TabViewModel> tabList = [];
        JsonElement elementArray = jsonDocument.RootElement.GetProperty("elements");
        foreach (JsonElement element in elementArray.EnumerateArray())
        {
            string type = element.GetProperty("type").GetString() ?? throw new Exception("type is not defined");

            TabViewModel tabView = type switch
            {
                "image" => DesignImagePanelViewModel.CreateImageDesignView(gameManager, element),
                "text" => DesignTextPanelViewModel.CreateTextDesignView(gameManager, element),
                "blurred" => DesignBlurredPanelViewModel.CreateBlurredDesignView(gameManager, element),
                _ => throw new NotImplementedException($"No valid panel for {type} type")
            };

            tabList.Add(tabView);
        }

        // 4 - Initialize preview
        JsonElement layoutArray = jsonDocument.RootElement.GetProperty("layout");
        previewModel.InitializePreview(gameManager, layoutArray);

        // 5 - Build model
        DesignMainViewModel mainModel = new DesignMainViewModel(gameManager, tabList, previewModel);

        // 6 - Return model
        return mainModel;
    }

    ////////////////////////////
    //// COMMAND MANAGEMENT ////
    ////////////////////////////
    public void ValidateCommand()
    {
        // 1 - Persist data and copy images to the library folder
        _ws.UpdateGame();
        _ws.SaveChange();
        _ws.Dispose();

        // 2 - Notify UI listeners AFTER files are written on disk
        _ws.Game.DesignState = CartridgeDesignState.DESIGNED;
    }

    public void CancelCommand()
    {
        _ws.Dispose();
    }
}
