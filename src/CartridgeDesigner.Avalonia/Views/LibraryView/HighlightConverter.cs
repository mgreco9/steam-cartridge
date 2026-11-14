using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CartridgeDesigner.Avalonia.Views.LibraryView;

public class HighlightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            // Selected item background
            return new SolidColorBrush(Color.Parse("#2A4DFF"));
        }

        // Default background
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}