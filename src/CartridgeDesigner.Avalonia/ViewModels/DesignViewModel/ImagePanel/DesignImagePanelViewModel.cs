using System;
using System.IO;
using System.Text.Json;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using SteamCartridge.Core.Workspace;

namespace CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;

public class DesignImagePanelViewModel : TabViewModel
{
    public DesignFolderViewModel FolderModel { get; }
    public DesignEditorViewModel EditorModel { get; }

    public required string ImageID { get; set; }

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public DesignImagePanelViewModel(DesignFolderViewModel folderModel, DesignEditorViewModel editorModel)
    {
        FolderModel = folderModel;
        EditorModel = editorModel;

        FolderModel.SelectedImageChanged += OnImageSelected;
    }

    public static DesignImagePanelViewModel CreateImageDesignView(GameManager ws, JsonElement element)
    {
        // 1 - Parse json data
        string id = element.GetProperty("id").GetString() ?? throw new Exception("id is not defined");
        string label = element.GetProperty("label").GetString() ?? throw new Exception("label is not defined");
        string sourceFolder = element.GetProperty("folder").GetString() ?? throw new Exception("folder is not defined");
        double width = element.GetProperty("width").GetDouble();
        double height = element.GetProperty("height").GetDouble();

        // 2 - Build folder sub view
        Repository repository = RepositoryFromString(sourceFolder);
        FolderManager folderWs = ws.LaunchRepositoryManager(repository);
        ImageItem[] imageList = folderWs.LoadAllImages();

        DesignFolderViewModel folderModel = new DesignFolderViewModel(folderWs, imageList);

        // 3 - Build editor sub view
        DesignEditorViewModel editorModel = new DesignEditorViewModel(ws, id)
        {
            Ratio = width / height
        };

        // 4 - Build main panel view
        DesignImagePanelViewModel imageView = new DesignImagePanelViewModel(folderModel, editorModel)
        {
            Title = label,
            ImageID = id
        };

        // 6 - Return panel view
        return imageView;
    }

    private static Repository RepositoryFromString(string value)
    {
        return value switch
        {
            "Front_Vertical" => Repository.FRONT_VERTICAL,
            "Front_Horizontal" => Repository.FRONT_HOTIZONTAL,
            "Back" => Repository.BACK,
            "Icon" => Repository.ICON,
            _ => throw new NotImplementedException()
        };
    }

    ///////////////////////////////////
    //// SELECTED IMAGE MANAGEMENT ////
    ///////////////////////////////////
    public void OnImageSelected(ImageItemViewModel? image)
    {
        if (image == null || EditorModel.SelectedImage?.Image == image.Image)
            return;

        EditorModel.SelectImage(image.Image);
        EditorModel.CenterImage();
    }

}
