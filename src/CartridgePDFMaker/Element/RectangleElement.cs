using System.Text.Json;
using SkiaSharp;
using SteamCartridge.Core.Workspace;

namespace CartridgePDFMaker;

public class RectangleElement : TemplateElement
{
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Width { get; init; }
    public required float Height { get; init; }
    public required SKColor Color { get; init; }
    public required float Thickness { get; init; }

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public RectangleElement(UnitConverter converter) : base(converter)
    {

    }
    
    public static RectangleElement InitializeElement(JsonElement layout, UnitConverter converter)
    {
        // 1 - Retrieve attribute value
        double x = layout.GetProperty("x").GetDouble();
        double y = layout.GetProperty("y").GetDouble();
        double width = layout.GetProperty("width").GetDouble();
        double height = layout.GetProperty("height").GetDouble();
        string color = layout.TryGetProperty("color", out JsonElement value) ? value.GetString()! : "#000000";
        double thickness = layout.TryGetProperty("thickness", out value) ? value.GetDouble() : 1;

        // 2 - Build element
        return new RectangleElement(converter)
        {
            X = converter.CmToPt(x),
            Y = converter.CmToPt(y),
            Width = converter.CmToPt(width),
            Height = converter.CmToPt(height),
            Color = SKColor.Parse(color),
            Thickness = (float)thickness
        };
    }

    /////////////////////////
    //// DRAW MANAGEMENT ////
    /////////////////////////
    public override void Draw(SKCanvas canvas, ImageItem cover)
    {
        // 1 -Define paint
        SKPaint paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = Color,
            StrokeWidth = Thickness
        };

        // 2 - Draw
        canvas.DrawRect(X, Y, Width, Height, paint);
    }
}