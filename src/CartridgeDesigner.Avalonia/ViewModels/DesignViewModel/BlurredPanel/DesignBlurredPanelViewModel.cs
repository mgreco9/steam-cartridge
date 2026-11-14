
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReactiveUI;
using SkiaSharp;
using SteamCartridge.Core.Workspace;

namespace CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;

public class DesignBlurredPanelViewModel : TabViewModel
{
    private readonly GameManager _ws;
    private readonly string _id;

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public DesignBlurredPanelViewModel(GameManager ws, string id, string source)
    {
        _ws = ws;
        _id = id;
        _ws.SubscribeCoverImageModifiedEvent(source, (imageItem) =>
        {
            _source = imageItem;
            UpdateBlurredImage();
        });
    }

    public static DesignBlurredPanelViewModel CreateBlurredDesignView(GameManager ws, JsonElement element)
    {
        // 1 - Parse json data
        string id = element.GetProperty("id").GetString() ?? throw new Exception("id is not defined");
        string label = element.GetProperty("label").GetString() ?? throw new Exception("label is not defined");
        string source = element.GetProperty("source").GetString() ?? throw new Exception("source is not defined");
        double height = element.GetProperty("height").GetDouble();
        double width = element.GetProperty("width").GetDouble();
        int blurValue = element.GetProperty("blur").GetInt32();

        // 2 - Build panel view model
        ImageItem? image = ws.LaunchRepositoryManager(Repository.CARTRIDGE).LoadImage(id);
        Bitmap? blurredImage = null;
        if(image!=null)
            blurredImage = new Bitmap(new MemoryStream(image.Thumbnail));
        return new DesignBlurredPanelViewModel(ws, id, source)
        {
            Title = label,
            Height = (int)(height*300),
            Width = (int)(width*300),
            Blur = blurValue,
            BlurredImage = blurredImage
        };

    }
    
    /////////////////////////
    //// BLUR MANAGEMENT ////
    /////////////////////////
    public int Width { get; init; }
    public int Height { get; init; }
    public int Blur 
    {
        get; 
        set 
        {
            if(field == value)
                return;

            field = value;
            _ = DebounceBlurAsync();
        }
    }
    private ImageItem? _source;
    private CancellationTokenSource? _cropDebounceCts;
    public Bitmap? BlurredImage
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    private async Task DebounceBlurAsync()
    {
        try
        {
            // 1 - Cancel previous pending crop
            _cropDebounceCts?.Cancel();

            // 2 - Initialize new cancellation token
            CancellationTokenSource cts = new CancellationTokenSource();
            _cropDebounceCts = cts;

            // 3 - Wait 1000 ms
            await Task.Delay(1000, cts.Token);

            // 4 - If not cancelled, execute render
            UpdateBlurredImage();
        }
        catch (TaskCanceledException)
        {
            // If cancelled, do nothing
        }
    }

    public void UpdateBlurredImage()
    {
        if(_source==null)
            return;

        // 1 - Retrieve the updated image source
        SKBitmap bitmapSource = SKBitmap.Decode(_source.Thumbnail);

        // 2 - Select source slice
        SKRectI slice = new SKRectI(0, 0, 1, bitmapSource.Height);

        // 3 - Draw stretch blurred image
        SKBitmap result = new SKBitmap(Width, Height);
        using SKCanvas canvas = new SKCanvas(result);

        SKPaint paint = new SKPaint
        {
            ImageFilter = SKImageFilter.CreateBlur(Blur, Blur, SKShaderTileMode.Clamp)
        };

        SKRect destRect = new SKRect(0, 0, Width, Height);
        canvas.DrawBitmap(bitmapSource, slice, destRect, paint);

        // 4 - Save Blur
        ImageItem imageItem = new ImageItem
        {
            Name = _id,
            Thumbnail = result.Encode(SKEncodedImageFormat.Png, 100).ToArray()
        };
        _ws.PushCoverImage(imageItem);

        // 5 - Display blur
        BlurredImage = new Bitmap(new MemoryStream(imageItem.Thumbnail));
    }
}