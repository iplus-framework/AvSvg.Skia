using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Svg.Editor.Avalonia.Converters;

public class BooleanStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => bool.TryParse(value as string, out var result) ? result : (object?)false;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool result ? result.ToString() : null;
}
