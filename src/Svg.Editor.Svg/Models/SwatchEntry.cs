using System.Linq;
using Svg;

namespace Svg.Editor.Svg.Models;

public class SwatchEntry
{
    public SvgLinearGradientServer Swatch { get; }
    public string Name { get; }

    public SwatchEntry(SvgLinearGradientServer swatch, string name)
    {
        Swatch = swatch;
        Name = name;
    }

    public string Color
    {
        get => ColorText.ToArgbHex(GetColor());
        set => SetColor(ColorText.Parse(value));
    }

    private System.Drawing.Color GetColor()
    {
        var stop = Swatch.Stops.FirstOrDefault();
        return stop?.GetColor(Swatch) ?? System.Drawing.Color.Black;
    }

    private void SetColor(System.Drawing.Color color)
    {
        Swatch.Children.Clear();
        Swatch.Children.Add(new SvgGradientStop
        {
            Offset = new SvgUnit(0f),
            StopColor = new SvgColourServer(color),
            StopOpacity = 1f
        });
        Swatch.Children.Add(new SvgGradientStop
        {
            Offset = new SvgUnit(1f),
            StopColor = new SvgColourServer(color),
            StopOpacity = 1f
        });
    }

    public override string ToString() => Name;
}
