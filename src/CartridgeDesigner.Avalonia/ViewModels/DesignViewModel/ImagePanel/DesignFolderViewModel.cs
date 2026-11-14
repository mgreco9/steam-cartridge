using System;
using System.Collections.ObjectModel;
using System.IO;
using ReactiveUI;
using SteamCartridge.Core.Workspace;

namespace CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;

public class DesignFolderViewModel : ReactiveObject
{
    private readonly FolderManager _ws;

    private readonly ObservableCollection<ImageItemViewModel> _imageCollection;

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public DesignFolderViewModel(FolderManager ws, ImageItem[] imageList)
    {
        _ws = ws;
        _imageCollection = new ObservableCollection<ImageItemViewModel>();

        foreach(ImageItem image in imageList)
        {
            _imageCollection.Add(new ImageItemViewModel(image));
        }
    }

    ////////////////////////////////
    //// FOLDER VIEW MANAGEMENT ////
    ////////////////////////////////
    public ObservableCollection<ImageItemViewModel> ImageCollection
    {
        get => _imageCollection;
    }

    public ImageItemViewModel? SelectedImage
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            SelectedImageChanged?.Invoke(field);
        }
    }
    public void DropFile(string path)
    {
        // 1 - If path does not exists, ignore
        if(!Path.Exists(path))
            return;

        // 2 - Only handle png and jpg
        string extension = Path.GetExtension(path);
        if(extension != ".png" && extension != ".jpg")
            return;

        // 3 - Create new image item
        ImageItem image = new ImageItem
        {
            Name = Path.GetFileNameWithoutExtension(path),
            Thumbnail = File.ReadAllBytes(path)
        };

        // 4 - Save image item
        _ws.SaveImage(image);

        // 5 - Add image to cache
        _imageCollection.Add(new ImageItemViewModel(image));
    }

    public event Action<ImageItemViewModel?>? SelectedImageChanged;

}