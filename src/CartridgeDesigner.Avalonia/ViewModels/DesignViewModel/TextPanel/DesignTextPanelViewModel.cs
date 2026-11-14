
using System;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Media;

using ReactiveUI;

using SkiaSharp;

using SteamCartridge.Core.Workspace;

namespace CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;

public class DesignTextPanelViewModel : TabViewModel
{
    private readonly GameManager _ws;
    private readonly string _id;

    /////////////////////
    //// CONSTRUCTOR ////
    /////////////////////
    public DesignTextPanelViewModel(GameManager ws, string id)
    {
        _ws = ws;
        _id = id;
        Text = "Undefined";
        TextColor = Brushes.White;
        _renderColor = SKColors.White;
    }

    public static DesignTextPanelViewModel CreateTextDesignView(GameManager ws, JsonElement element)
    {
        // 1 - Parse json data
        string id = element.GetProperty("id").GetString() ?? throw new Exception("id is not defined");
        string label = element.GetProperty("label").GetString() ?? throw new Exception("label is not defined");
        string font = element.GetProperty("font").GetString() ?? throw new Exception("font is not defined");
        double height = element.GetProperty("height").GetDouble();
        double width = element.GetProperty("width").GetDouble();
        int fontsize = element.GetProperty("fontsize").GetInt32();
        string source = element.GetProperty("source").GetString() ?? throw new Exception("source is not defined");

        string text = source switch
        {
            "Name" => ws.Game.Name,
            "Classification" => ws.Game.Classification,
            "Numerotation" => ws.Game.Numerotation,
            _ => throw new Exception("undefined source")
        };

        // 2 - Build panel view model
        return new DesignTextPanelViewModel(ws, id)
        {
            Title = label,
            Text = text,
            Height = (int)(height * 300),
            Width = (int)(width * 300),
            SelectedFont = FontFamily.Parse(font),
            FontSize = fontsize
        };
    }

    /////////////////////////
    //// TEXT MANAGEMENT ////
    /////////////////////////
    public string Text
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            _ = DebounceRenderAsync();
        }
    }

    /////////////////////////
    //// FONT MANAGEMENT ////
    /////////////////////////
    public int FontSize { get; set; }
    public ObservableCollection<FontFamily> Fonts { get; } =
        new(FontManager.Current.SystemFonts);

    public required FontFamily SelectedFont
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            _ = DebounceRenderAsync();
        }
    }

    //////////////////////////
    //// COLOR MANAGEMENT ////
    //////////////////////////
    private SKColor _renderColor;
    public IBrush TextColor
    {
        get;
        private set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            _ = DebounceRenderAsync();
        }
    }

    public void SetWhiteCommand()
    {
        _renderColor = SKColors.White;
        TextColor = Brushes.White;
    }

    public void SetBlackCommand()
    {
        _renderColor = SKColors.Black;
        TextColor = Brushes.Black;
    }

    ///////////////////////
    //// TEXT RENDERER ////
    ///////////////////////
    public required int Width { get; init; }
    public required int Height { get; init; }
    private CancellationTokenSource? _cropDebounceCts;

    private async Task DebounceRenderAsync()
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
            Render();
        }
        catch (TaskCanceledException)
        {
            // If cancelled, do nothing
        }
    }

    public void Render()
    {
        // 1 - Initialize canvas
        SKBitmap bitmap = new SKBitmap(Width, Height);
        using SKCanvas canvas = new SKCanvas(bitmap);

        // 2 - Set background transparent
        canvas.Clear(SKColors.Transparent);

        // 3 - Define text font
        int fontSize = FontSize;
        float textWidth;
        SKFont font;
        do
        {
            font = new SKFont()
            {
                Size = fontSize--,
                Typeface = SKTypeface.FromFamilyName(SelectedFont.Name, SKFontStyle.Bold)
            };

            textWidth = font.MeasureText(Text);
        } while (textWidth > Width * 0.95f);

        SKPaint paint = new SKPaint()
        {
            Color = _renderColor,
            IsAntialias = true,
        };

        // 4 - Center text
        SKFontMetrics metrics = new SKFontMetrics();
        font.GetFontMetrics(out metrics);
        float textHeight = metrics.Descent - metrics.Ascent;

        float x = (Width - textWidth) / 2;
        float y = (Height - textHeight) / 2 - metrics.Ascent;

        canvas.DrawText(Text, x, y, font, paint);

        // 5 - Save text as image
        ImageItem imageItem = new ImageItem
        {
            Name = _id,
            Thumbnail = bitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray()
        };
        _ws.PushCoverImage(imageItem);
    }
}