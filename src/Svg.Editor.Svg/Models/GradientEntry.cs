using System;
using System.Collections.ObjectModel;
using System.Linq;
using Svg;

namespace Svg.Editor.Svg.Models;

public class GradientStopInfo
{
    public double Offset { get; set; }
    public string Color { get; set; } = "#000000";
}

public class GradientStopsEntry : PropertyEntry
{
    public ObservableCollection<GradientStopInfo> Stops { get; }

    public GradientStopsEntry(SvgGradientServer gradient)
        : base("Stops", $"{gradient.Stops.Count} stops", (_, __) => { })
    {
        Stops = new ObservableCollection<GradientStopInfo>(
            gradient.Stops.Select(s => new GradientStopInfo
            {
                Offset = s.Offset.Value,
                Color = ColorToString(s.GetColor(gradient))
            }));
    }

    private static string ColorToString(System.Drawing.Color c)
        => ColorText.ToArgbHex(c);

    private static System.Drawing.Color ParseColor(string color)
        => ColorText.Parse(color);

    public override void Apply(object target)
    {
        if (target is not SvgGradientServer grad)
            return;
        grad.Children.Clear();
        foreach (var info in Stops)
        {
            var stop = new SvgGradientStop
            {
                Offset = new SvgUnit((float)info.Offset),
                StopColor = new SvgColourServer(ParseColor(info.Color)),
                StopOpacity = 1f
            };
            grad.Children.Add(stop);
        }
    }

    public void UpdateValue() => Value = $"{Stops.Count} stops";
}
