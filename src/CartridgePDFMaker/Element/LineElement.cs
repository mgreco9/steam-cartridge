using System.Text.Json;
using SkiaSharp;
using SteamCartridge.Core.Workspace;

namespace CartridgePDFMaker;

public class LineElement : TemplateElement
{
    public required float X1 { get; init; }
    public required float Y1 { get; init; }
    public required float X2 { get; init; }
    public required float Y2 { get; init; }
    public required SKColor Color { get; init; }
    public required float Thickness { get; init; }

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public LineElement(UnitConverter converter) : base(converter)
    {

    }

    public static LineElement InitializeElement(JsonElement layout, UnitConverter converter)
    {
        // 1 - Retrieve attribute value
        double x1 = layout.GetProperty("x1").GetDouble();
        double y1 = layout.GetProperty("y1").GetDouble();
        double x2 = layout.GetProperty("x2").GetDouble();
        double y2 = layout.GetProperty("y2").GetDouble();
        string color = layout.TryGetProperty("color", out JsonElement value) ? value.GetString()! : "#000000";
        double thickness = layout.TryGetProperty("thickness", out value) ? value.GetDouble() : 1;

        // 2 - Build element
        return new LineElement(converter)
        {
            X1 = converter.CmToPt(x1),
            Y1 = converter.CmToPt(y1),
            X2 = converter.CmToPt(x2),
            Y2 = converter.CmToPt(y2),
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
            Color = Color,
            StrokeWidth = Thickness
        };

        // 2 - Draw
        canvas.DrawLine(X1, Y1, X2, Y2, paint);
    }
}