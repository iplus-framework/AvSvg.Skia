using System;
using System.ComponentModel;
using Svg;

namespace Svg.Editor.Svg.Models;

public class StyleEntry
{
    public string Name { get; set; } = string.Empty;
    public string? Fill { get; set; }
    public string? Stroke { get; set; }
    public float Opacity { get; set; } = 1f;

    public override string ToString() => Name;

    public static StyleEntry FromElement(string name, SvgVisualElement element)
    {
        var converter = TypeDescriptor.GetConverter(typeof(SvgPaintServer));
        var entry = new StyleEntry { Name = name, Opacity = element.Opacity };

        if (element.Fill is { })
        {
            try
            {
                entry.Fill = converter.ConvertToInvariantString(element.Fill);
            }
            catch
            {
                entry.Fill = element.Fill.ToString();
            }
        }

        if (element.Stroke is { })
        {
            try
            {
                entry.Stroke = converter.ConvertToInvariantString(element.Stroke);
            }
            catch
            {
                entry.Stroke = element.Stroke.ToString();
            }
        }

        return entry;
    }

    public void Apply(SvgVisualElement element)
    {
        var converter = TypeDescriptor.GetConverter(typeof(SvgPaintServer));

        if (Fill is { })
        {
            try
            {
                element.Fill = (SvgPaintServer?)converter.ConvertFromInvariantString(Fill);
            }
            catch
            {
            }
        }

        if (Stroke is { })
        {
            try
            {
                element.Stroke = (SvgPaintServer?)converter.ConvertFromInvariantString(Stroke);
            }
            catch
            {
            }
        }

        element.Opacity = Opacity;
    }
}
