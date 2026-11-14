using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SkiaSharp;
using SteamCartridge.Core.Workspace;

namespace CartridgePDFMaker;

public class CartridgePDFLib
{
    private readonly WorkspaceManager _ws;
    private readonly UnitConverter _converter;
    public required TemplateElement[] ElementList { get; init; }
    public required float RowHeight { get; init; }
    public required float Padding { get; init; }

    public CartridgePDFLib(WorkspaceManager ws, UnitConverter converter)
    {
        _ws = ws;
        _converter = converter;
    }

    public static CartridgePDFLib InitializePDFMaker(WorkspaceManager ws)
    {
        // 1 - Retrieve the json layout
        JsonDocument jsonDocument = ws.GetLayoutJson();
        JsonElement pdfTemplate = jsonDocument.RootElement.GetProperty("pdfTemplate");

        // 2 - Retrieve attributes
        double rowHeight = pdfTemplate.GetProperty("rowHeight").GetDouble();
        double padding = pdfTemplate.GetProperty("padding").GetDouble();
        double calibration = pdfTemplate.GetProperty("calibration").GetDouble();
        JsonElement layoutArray = pdfTemplate.GetProperty("items");

        // 2 - Define layout
        UnitConverter converter = new UnitConverter((float)calibration);
        TemplateElement[] elementList = new TemplateElement[layoutArray.GetArrayLength()];
        int idx = 0;
        foreach (JsonElement layout in layoutArray.EnumerateArray())
        {
            string type = layout.GetProperty("type").GetString() ?? throw new Exception("type is not defined");

            elementList[idx++] = type switch
            {
                "image" => ImageElement.InitializeElement(layout, converter),
                "line" => LineElement.InitializeElement(layout, converter),
                "rectangle" => RectangleElement.InitializeElement(layout, converter),
                _ => throw new Exception("unrecognized type")
            };
        }

        // 3 - Build PDF Lib
        return new CartridgePDFLib(ws, converter)
        {
            ElementList = elementList,
            RowHeight = converter.CmToPt(rowHeight),
            Padding = converter.CmToPt(padding)
        };
    }

    public void GeneratePDF(ICollection<ImageItem> coverList)
    {
        // 1 - Open PDF for write
        if(File.Exists(_ws.PDFOutputPath))
        {
            try
            {
                File.Delete(_ws.PDFOutputPath);
            }
            catch (IOException)
            {
                return;
            }
        }
            
        using var stream = File.OpenWrite(_ws.PDFOutputPath);
        using var document = SKDocument.CreatePdf(stream);

        float pageWidth = 28.3465f * 21;
        float pageHeight = 28.3465f * 29.7f;

        // 2 - For each item, create a row
        int nbRowPerPage = (int)Math.Floor(pageHeight / RowHeight);
        int rowIdx = 0;
        SKCanvas canvas = document.BeginPage(pageWidth, pageHeight);
        foreach(ImageItem cover in coverList)
        {
            // 2.1 - Create new page if needed
            if(rowIdx!=0 && rowIdx % nbRowPerPage == 0)
            {
                document.EndPage();
                canvas = document.BeginPage(pageWidth, pageHeight);
            }

            // 2.2 - Transpose padding
            canvas.Save();
            canvas.Translate(Padding, Padding);

            // 2.3 - Draw each element of the row
            foreach(TemplateElement element in ElementList)
            {
                element.Draw(canvas, cover);
            }

            // 2.4 - Transpose to next row
            rowIdx++;
            if(rowIdx % nbRowPerPage != 0)
            {
                canvas.Restore();
                canvas.Translate(0, RowHeight);
            }
        }
    }
}

public class UnitConverter
{
    private readonly float _calibration;

    public UnitConverter(float calibration)
    {
        _calibration = calibration;
    }

    // PDF points
    public float CmToPt(double cm) => CmToPt((float)cm);
    public float CmToPt(float cm)
    {
        return cm * 28.3465f * _calibration;
    }
}