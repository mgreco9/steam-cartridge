using System.Text.Json;
using SkiaSharp;
using SteamCartridge.Core.Workspace;

namespace CartridgePDFMaker;

public class ImageElement : TemplateElement
{
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Width { get; init; }
    public required float Height { get; init; }

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public ImageElement(UnitConverter converter) : base(converter)
    {

    }

    public static ImageElement InitializeElement(JsonElement layout, UnitConverter converter)
    {
        // 1 - Retrieve attribute value
        double x = layout.GetProperty("x").GetDouble();
        double y = layout.GetProperty("y").GetDouble();
        double width = layout.GetProperty("width").GetDouble();
        double height = layout.GetProperty("height").GetDouble();

        // 2 - Build element
        return new ImageElement(converter)
        {
            X = converter.CmToPt(x),
            Y = converter.CmToPt(y),
            Width = converter.CmToPt(width),
            Height = converter.CmToPt(height)
        };
    }

    /////////////////////////
    //// DRAW MANAGEMENT ////
    /////////////////////////
    public override void Draw(SKCanvas canvas, ImageItem cover)
    {
        // 1 -Initialize bitmap
        SKBitmap bitmap = SKBitmap.Decode(cover.Thumbnail);

        // 2 - Draw
        SKRect position = new SKRect(X, Y, X+Width, Y+Height);
        canvas.DrawBitmap(bitmap, position);
    }
}