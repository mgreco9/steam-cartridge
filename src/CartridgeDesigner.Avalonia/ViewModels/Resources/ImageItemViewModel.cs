using System;
using System.IO;
using Avalonia.Media.Imaging;
using SteamCartridge.Core.Workspace;

public class ImageItemViewModel
{
    public ImageItem Image { get; }

    public Bitmap ThumbnailBitmap { get; }

    public string Name => Image.Name;

    public ImageItemViewModel(ImageItem image)
    {
        Image = image;
        using MemoryStream stream = new MemoryStream(image.Thumbnail);

        #pragma warning disable CA1416 // Validate platform compatibility
        ThumbnailBitmap = new Bitmap(stream);
        #pragma warning restore CA1416 // Validate platform compatibilidty
    }
}