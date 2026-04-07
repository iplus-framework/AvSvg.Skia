using System;
using System.Globalization;

namespace Svg.Editor.Svg.Models;

internal static class ColorText
{
    public static string ToArgbHex(System.Drawing.Color color)
        => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    public static System.Drawing.Color Parse(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return System.Drawing.Color.Black;

        var text = color.Trim();
        if (text[0] == '#')
            text = text.Substring(1);

        return text.Length switch
        {
            6 => System.Drawing.Color.FromArgb(
                255,
                byte.Parse(text.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(text.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(text.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)),
            8 => System.Drawing.Color.FromArgb(
                byte.Parse(text.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(text.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(text.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(text.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)),
            _ => throw new FormatException($"Unsupported color value '{color}'.")
        };
    }
}
