using System;
using System.Collections.ObjectModel;
using System.Linq;
using Svg;
using Svg.Editor.Svg.Models;

namespace Svg.Editor.Svg;

public class SymbolService
{
    public ObservableCollection<SymbolEntry> Symbols { get; } = new();

    public void Load(SvgDocument? document)
    {
        Symbols.Clear();
        if (document is null)
            return;
        var defs = document.Children.OfType<SvgDefinitionList>().FirstOrDefault();
        if (defs is null)
            return;
        int index = 1;
        foreach (var s in defs.Children.OfType<SvgSymbol>())
        {
            var name = string.IsNullOrEmpty(s.ID) ? $"Symbol {index++}" : s.ID!;
            Symbols.Add(new SymbolEntry(s, name));
        }
    }

    public void AddSymbol(SvgDocument document, SvgSymbol symbol)
    {
        var defs = document.Children.OfType<SvgDefinitionList>().FirstOrDefault();
        if (defs is null)
        {
            defs = new SvgDefinitionList();
            document.Children.Add(defs);
        }
        defs.Children.Add(symbol);
        var name = string.IsNullOrEmpty(symbol.ID) ? $"Symbol {Symbols.Count + 1}" : symbol.ID!;
        Symbols.Add(new SymbolEntry(symbol, name));
    }
}
