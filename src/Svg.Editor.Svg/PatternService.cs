using System;
using System.Collections.ObjectModel;
using System.Linq;
using Svg;
using Svg.Editor.Svg.Models;

namespace Svg.Editor.Svg;

public class PatternService
{
    public ObservableCollection<PatternEntry> Patterns { get; } = new();

    public void Load(SvgDocument? document)
    {
        Patterns.Clear();
        if (document is null)
            return;
        int index = 1;
        foreach (var p in document.Descendants().OfType<SvgPatternServer>())
        {
            var name = string.IsNullOrEmpty(p.ID) ? $"Pattern {index++}" : p.ID!;
            Patterns.Add(new PatternEntry(p, name));
        }
    }

    public void AddPattern(SvgDocument document, SvgPatternServer pattern)
    {
        document.Children.Add(pattern);
        var name = string.IsNullOrEmpty(pattern.ID) ? $"Pattern {Patterns.Count + 1}" : pattern.ID!;
        Patterns.Add(new PatternEntry(pattern, name));
    }
}
