using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Svg.Editor.Avalonia.Converters;

public class EyeIconConverter : IValueConverter
{
    private static readonly Geometry EyeOpen = Geometry.Parse("M1,8 C3,4 13,4 15,8 C13,12 3,12 1,8 Z M8,8 A3,3 0 1 1 7.99,8 Z");
    private static readonly Geometry EyeClosed = Geometry.Parse("M1,8 C3,4 13,4 15,8 C13,12 3,12 1,8 Z M0,0 L16,16 M16,0 L0,16");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var visible = value as bool? ?? false;
        return visible ? EyeOpen : EyeClosed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => null;
}
