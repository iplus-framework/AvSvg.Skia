using System;
using System.Collections.ObjectModel;
using System.Linq;
using Svg;
using Svg.Editor.Svg.Models;
using Svg.Model.Drawables;

namespace Svg.Editor.Svg;

public class LayerService
{
    public ObservableCollection<LayerEntry> Layers { get; } = new();

    public void Load(SvgDocument? document, DrawableBase? root = null)
    {
        Layers.Clear();
        if (document is null)
            return;
        int index = 1;
        foreach (var g in document.Children.OfType<SvgGroup>())
        {
            if (IsLayerGroup(g))
                Layers.Add(CreateEntry(g, root, ref index));
        }
    }

    public LayerEntry AddLayer(SvgDocument document, string? name = null)
    {
        var group = new SvgGroup();
        group.CustomAttributes["data-layer"] = "true";
        group.CustomAttributes["data-name"] = name ?? $"Layer {Layers.Count + 1}";
        group.CustomAttributes["data-visible"] = "true";
        group.CustomAttributes["data-lock"] = "false";
        document.Children.Add(group);
        var entry = new LayerEntry(group, group.CustomAttributes["data-name"]);
        Layers.Add(entry);
        return entry;
    }

    public void RemoveLayer(LayerEntry layer, SvgDocument document)
    {
        document.Children.Remove(layer.Group);
        Layers.Remove(layer);
    }

    public void MoveUp(LayerEntry layer, SvgDocument document)
    {
        var idx = document.Children.IndexOf(layer.Group);
        if (idx > 0)
        {
            document.Children.RemoveAt(idx);
            document.Children.Insert(idx - 1, layer.Group);
            var lidx = Layers.IndexOf(layer);
            Layers.Move(lidx, lidx - 1);
        }
    }

    public void MoveDown(LayerEntry layer, SvgDocument document)
    {
        var idx = document.Children.IndexOf(layer.Group);
        if (idx >= 0 && idx < document.Children.Count - 1)
        {
            document.Children.RemoveAt(idx);
            document.Children.Insert(idx + 1, layer.Group);
            var lidx = Layers.IndexOf(layer);
            Layers.Move(lidx, lidx + 1);
        }
    }

    private static bool IsLayerGroup(SvgGroup group)
        => group.CustomAttributes.TryGetValue("data-layer", out var flag) &&
           string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);

    private static LayerEntry CreateEntry(SvgGroup group, DrawableBase? root, ref int index)
    {
        group.CustomAttributes.TryGetValue("data-name", out var name);
        var entry = new LayerEntry(group, string.IsNullOrEmpty(name) ? $"Layer {index++}" : name);
        if (root is not null)
            entry.Drawable = FindDrawable(root, group);
        foreach (var child in group.Children.OfType<SvgGroup>())
            if (IsLayerGroup(child))
                entry.Sublayers.Add(CreateEntry(child, root, ref index));
        return entry;
    }

    private static DrawableBase? FindDrawable(DrawableBase drawable, SvgElement element)
    {
        if (drawable.Element == element)
            return drawable;
        if (drawable is DrawableContainer container)
        {
            foreach (var child in container.ChildrenDrawables)
            {
                var found = FindDrawable(child, element);
                if (found is not null)
                    return found;
            }
        }
        return null;
    }
}
