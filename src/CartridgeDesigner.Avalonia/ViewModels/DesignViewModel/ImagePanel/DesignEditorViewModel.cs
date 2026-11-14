
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using ReactiveUI;
using SkiaSharp;
using SteamCartridge.Core.Workspace;

namespace CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;

public class DesignEditorViewModel : ReactiveObject
{
    private GameManager _ws;
    private string _id;
    private Rect _borderView;

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public DesignEditorViewModel(GameManager ws, string id)
    {
        _ws = ws;
        _id = id;
        Zoom = 1;
        OffsetX = 0;
        OffsetY = 0;
    }

    ////////////////////////////////
    //// CROP BORDER MANAGEMENT ////
    ////////////////////////////////
    public required double Ratio
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public void ScaleBorder(Rect cropRect)
    {
        // 1 - Update crop border
        _borderView = cropRect;

        // 2 - Re scale image if necessary
        Scale(0);
    }

    ////////////////////////////
    //// IMAGE CHANGE EVENT ////
    ////////////////////////////
    private SKBitmap? _selectedImage;
    public ImageItemViewModel? SelectedImage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public double Zoom
    {
        get;
        private set =>this.RaiseAndSetIfChanged(ref field, value);
    }

    public double OffsetX
    {
        get;
        private set =>this.RaiseAndSetIfChanged(ref field, value);
    }

    public double OffsetY
    {
        get;
        private set =>this.RaiseAndSetIfChanged(ref field, value);
    }

    public void SelectImage(ImageItem image)
    {
        // 1 - Load SKBitMap
        _selectedImage = SKBitmap.Decode(image.Thumbnail);

        // 2 - Update displayed image
        SelectedImage = new ImageItemViewModel(image);
    }

    public void CenterImage()
    {
        // 1 - Do nothing if no image
        if (_selectedImage == null)
            return;

        // 2 - Retrieve image size
        double width = _selectedImage.Width;
        double height = _selectedImage.Height;

        // 3 - Make sure image is scaled so that one of its dimension is clamped to the border frame (the other must be greater or equal)
        double scaleX = _borderView.Width / width;
        double scaleY = _borderView.Height / height;
        double scale = Math.Max(scaleX, scaleY);

        // 4 - Define coordinate to frame center
        double originX = _borderView.Left + _borderView.Width / 2 - width * scale / 2;
        double originY = _borderView.Top + _borderView.Height / 2 - height * scale / 2;

        // 5 - Set transform value
        Zoom = scale;
        OffsetX = originX;
        OffsetY = originY;

        // 6 - Call cropping
        _ = DebounceCropAsync();
    }

    public void Scale(double delta)
    {
        // 1 - Do nothing if no image
        if (_selectedImage == null)
            return;

        // 2 - Compute new scale
        double scale = Math.Clamp(Zoom + delta, 0.1, 10);

        // 3 - Retrieve scaled image size
        double width = _selectedImage.Width * scale;
        double height = _selectedImage.Height * scale;

        // 4 - If one of the image dimension is smaller than crop frame, resize it to fit
        if (width < _borderView.Width || height < _borderView.Height)
            scale = Math.Max(_borderView.Width / _selectedImage.Width, _borderView.Height / _selectedImage.Height);

        // 5 - Push the new scale to view
        Zoom = scale;

        // 6 - Clamp coordinates to frame if necessary
        Drag(0, 0);
    }

    public void Drag(double deltaX, double deltaY)
    {
        // 1 - Do nothing if no image
        if (_selectedImage == null)
            return;

        // 2 - Retrieve scaled image size
        double width = _selectedImage.Width * Zoom;
        double height = _selectedImage.Height * Zoom;

        // 3 - Define min and max values of for each coordinate
        double minOffsetX = _borderView.Right - width;
        double maxOffsetX = _borderView.Left;

        double minOffsetY = _borderView.Bottom - height;
        double maxOffsetY = _borderView.Top;

        // 4 - Compute the new offset
        double newOffsetX = OffsetX + deltaX;
        double newOffsetY = OffsetY + deltaY;

        // 5 - Clamp image coordinates
        if (newOffsetX < minOffsetX)
            newOffsetX = minOffsetX;
        else if (newOffsetX > maxOffsetX)
            newOffsetX = maxOffsetX;

        if (newOffsetY < minOffsetY)
            newOffsetY = minOffsetY;
        else if (newOffsetY > maxOffsetY)
            newOffsetY = maxOffsetY;

        // 6 - Push the update
        OffsetX = newOffsetX;
        OffsetY = newOffsetY;

        // 7 - Call cropping
        _ = DebounceCropAsync();
    }

    ///////////////////////////////////
    //// IMAGE CROPPING MANAGEMENT ////
    ///////////////////////////////////
    private CancellationTokenSource? _cropDebounceCts;
    private async Task DebounceCropAsync()
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

            // 4 - If not cancelled, execute crop
            CropImage();
        }
        catch (TaskCanceledException)
        {
            // If cancelled, do nothing
        }
    }
    
    public void CropImage()
    {
        // 1 - Do nothing if no image
        if (_selectedImage == null)
            return;

        // 2 - Inverse border frame transform to image view
        int x = (int)Math.Floor((_borderView.X - OffsetX) / Zoom);
        int y = (int)Math.Floor((_borderView.Y - OffsetY) / Zoom);
        int w = (int)Math.Ceiling( _borderView.Width / Zoom);
        int h = (int)Math.Ceiling(_borderView.Height / Zoom);

        // 3 - Define render bitmap
        SKBitmap cropped = new SKBitmap(w, h);
        SKRectI rect = new SKRectI(x, y, x+w, y+h);
        _selectedImage.ExtractSubset(cropped, rect);

        // 4 - Save cropped image
        ImageItem imageItem = new ImageItem
        {
            Name = _id,
            Thumbnail = cropped.Encode(SKEncodedImageFormat.Png, 100).ToArray()
        };
        _ws.PushCoverImage(imageItem);
    }

}