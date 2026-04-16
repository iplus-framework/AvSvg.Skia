using System.Collections.ObjectModel;
using System.Linq;
using Svg;
using Svg.Editor.Svg.Models;

namespace Svg.Editor.Svg;

public class BrushService
{
    public ObservableCollection<BrushEntry> Brushes { get; } = new();
    public BrushEntry? SelectedBrush { get; set; }

    public BrushService()
    {
        var def = new StrokeProfile();
        Brushes.Add(new BrushEntry("Default", def));
        SelectedBrush = Brushes[0];
    }

    public ObservableCollection<SwatchEntry> Swatches { get; } = new();

    public void LoadSwatches(SvgDocument? document)
    {
        Swatches.Clear();
        if (document is null)
            return;
        int index = 1;
        foreach (var grad in document.Descendants().OfType<SvgLinearGradientServer>())
        {
            if (grad.Stops.Count == 1 || (grad.Stops.Count == 2 && grad.Stops[0].GetColor(grad) == grad.Stops[1].GetColor(grad)))
            {
                var name = string.IsNullOrEmpty(grad.ID) ? $"Swatch {index++}" : grad.ID!;
                Swatches.Add(new SwatchEntry(grad, name));
            }
        }
    }
}
