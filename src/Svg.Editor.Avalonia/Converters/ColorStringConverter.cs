using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Svg.Editor.Avalonia.Converters;

public class ColorStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && Color.TryParse(s, out var color))
            return color;

        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Color color ? color.ToString() : null;
}
