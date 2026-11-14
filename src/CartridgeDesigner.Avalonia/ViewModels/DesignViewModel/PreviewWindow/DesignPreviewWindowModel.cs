
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Avalonia.Media.Imaging;
using ReactiveUI;
using SkiaSharp;
using SteamCartridge.Core.Workspace;

namespace CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;

public class DesignPreviewWindowModel : ReactiveObject
{
    private GameManager? _ws;
    private FolderManager? _repositoryManager;
    private readonly Dictionary<string, ImageLayer> _previewList;
    private double _width;
    private double _height;

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public DesignPreviewWindowModel()
    {
        _previewList = [];
    }

    public void InitializePreview(GameManager ws, JsonElement layoutArray)
    {
        // 1 - Set workspace
        _ws = ws;
        _repositoryManager = ws.LaunchRepositoryManager(Repository.CARTRIDGE);

        // 2 - Define layout
        foreach (JsonElement layout in layoutArray.EnumerateArray())
        {
            string id = layout.GetProperty("imageId").GetString() ?? throw new Exception("imageId is not defined");
            double x = layout.GetProperty("x").GetDouble();
            double y = layout.GetProperty("y").GetDouble();
            double width = layout.GetProperty("width").GetDouble();
            double height = layout.GetProperty("height").GetDouble();
            int rotation = layout.TryGetProperty("rotation", out JsonElement value) ? value.GetInt32() : 0;

            _width = Math.Max(_width, width+x);
            _height = Math.Max(_height, height+y);

            ImageItem? image = _repositoryManager.LoadImage(id);

            SKBitmap? bitmap = null;
            if(image != null)
                bitmap = SKBitmap.Decode(image.Thumbnail);
            
            ImageLayer layer = new ImageLayer
            {
                Bitmap = bitmap,
                Id = id,
                X = (int)(x * 300),
                Y = (int)(y * 300),
                Width = (int)(width * 300),
                Height = (int)(height * 300),
                Rotation = rotation
            };

            _previewList.Add(id, layer);
        }
        
        // 3 - Render first preview
        RenderPreview();

        // 4 - Subscribe to each cover modification
        foreach(string id in _previewList.Keys)
        {
            _ws.SubscribeCoverImageModifiedEvent(id, (image) => {
                _previewList[id].Bitmap = SKBitmap.Decode(image.Thumbnail);
                RenderPreview();
            });
        }
    }

    //////////////////////////////////
    //// PREVIEW IMAGE MANAGEMENT ////
    //////////////////////////////////
    public Bitmap? PreviewImage
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public void RenderPreview()
    {
        // 1 - Initialize preview canvas
        SKBitmap preview = new SKBitmap((int)(_width * 300), (int)(_height * 300));
        using var canvas = new SKCanvas(preview);

        canvas.Clear(SKColors.Red);

        // 2 - Add each layer to preview
        foreach(ImageLayer layer in _previewList.Values)
        {
            if(layer.Bitmap == null)
                continue;

            float cx = layer.X + layer.Width / 2f;
            float cy = layer.Y + layer.Height / 2f;

            canvas.Save();
            canvas.Translate(cx, cy);
            canvas.RotateDegrees(layer.Rotation);

            SKRect position = new SKRect(-layer.Width / 2f, -layer.Height / 2f, layer.Width / 2f, layer.Height / 2f);
            if(layer.Rotation%180!=0 && layer.Rotation%90==0)
                position = new SKRect(-layer.Height / 2f, -layer.Width / 2f, layer.Height / 2f, layer.Width / 2f);
            canvas.DrawBitmap(layer.Bitmap, position);

            canvas.Restore();
        }

        // 3 - Save preview
        ImageItem imageItem = new ImageItem
        {
            Name = "cartridge",
            Thumbnail = preview.Encode(SKEncodedImageFormat.Png, 100).ToArray()
        };
        _ws!.PushCoverImage(imageItem);

        // 4 - Display preview
        PreviewImage = new Bitmap(new MemoryStream(imageItem.Thumbnail));
    }

    private class ImageLayer
    {
        public SKBitmap? Bitmap;
        public required string Id;
        public required int X;
        public required int Y;
        public required int Width;
        public required int Height;
        public required int Rotation;
    }
}