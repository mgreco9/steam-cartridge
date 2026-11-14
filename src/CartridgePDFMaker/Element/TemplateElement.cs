using SkiaSharp;
using SteamCartridge.Core.Workspace;

namespace CartridgePDFMaker;

public abstract class TemplateElement
{
    protected readonly UnitConverter _converter;
    public TemplateElement(UnitConverter converter)
    {
        _converter = converter;
    }

    public abstract void Draw(SKCanvas canvas, ImageItem cover);
}