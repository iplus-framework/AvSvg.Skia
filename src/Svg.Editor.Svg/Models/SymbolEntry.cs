using System;
using Svg;

namespace Svg.Editor.Svg.Models;

public class SymbolEntry
{
    public SvgSymbol Symbol { get; }
    public string Name { get; }

    public SymbolEntry(SvgSymbol symbol, string name)
    {
        Symbol = symbol;
        Name = name;
    }

    public bool Apply(SvgVisualElement element)
    {
        if (string.IsNullOrEmpty(Symbol.ID))
            return false;

        if (element is SvgUse use)
        {
            use.ReferencedElement = new Uri($"#{Symbol.ID}", UriKind.Relative);
            return true;
        }

        return false;
    }

    public override string ToString() => Name;
}
