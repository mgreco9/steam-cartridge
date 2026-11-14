using Avalonia;
using Avalonia.Controls;

namespace CartridgeDesigner.Avalonia.Views.Controls;

public class AspectRatio : Decorator
{
    static AspectRatio()
    {
        RatioProperty.Changed.AddClassHandler<AspectRatio>((x, e) =>
        {
            x.InvalidateMeasure();
            x.InvalidateArrange();
        });
    }

    public static readonly StyledProperty<double> RatioProperty =
        AvaloniaProperty.Register<AspectRatio, double>(
            "Ratio", 1.0);

    public double Ratio
    {
        get => GetValue(RatioProperty);
        set => SetValue(RatioProperty, value);
    }


    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child == null)
            return new Size();

        var ratio = Ratio;

        double width = availableSize.Width;
        double height = availableSize.Height;

        if (width / height > ratio)
            width = height * ratio;
        else
            height = width / ratio;

        Child.Measure(new Size(width, height));
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child == null)
            return finalSize;

        var ratio = Ratio;

        double width = finalSize.Width;
        double height = finalSize.Height;

        if (width / height > ratio)
            width = height * ratio;
        else
            height = width / ratio;

        var rect = new Rect(
            (finalSize.Width - width) / 2,
            (finalSize.Height - height) / 2,
            width,
            height);

        Child.Arrange(rect);
        return finalSize;
    }
}