using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Svg;
using Svg.Editor.Avalonia.Controls;
using Svg.Editor.Core;
using Svg.Editor.Svg.Models;
using Xunit;

namespace Svg.Editor.Skia.Avalonia.UnitTests;

public class PanelControlsTests
{
    [AvaloniaFact]
    public void DocumentOutlineView_BindsNodesAndArtboards()
    {
        var root = new SvgGroup { ID = "root" };
        var nodes = new ObservableCollection<SvgNode> { new(root, null) };
        var artboards = new ObservableCollection<ArtboardInfo> { new(new SvgGroup(), "Board", 100, 200) };
        var view = new DocumentOutlineView
        {
            Nodes = nodes,
            Artboards = artboards
        };

        Assert.Same(nodes, view.OutlineTree.ItemsSource);
        Assert.Same(artboards, view.ArtboardList.ItemsSource);
    }

    [AvaloniaFact]
    public void ResourceBrowserView_BindsCollections()
    {
        var view = new ResourceBrowserView
        {
            Patterns = new ObservableCollection<PatternEntry> { new(new SvgPatternServer(), "Pattern 1") },
            Swatches = new ObservableCollection<SwatchEntry> { new(new SvgLinearGradientServer(), "Swatch 1") },
            BrushStyles = new ObservableCollection<BrushEntry> { new("Brush 1", new StrokeProfile()) },
            Symbols = new ObservableCollection<SymbolEntry> { new(new SvgSymbol(), "Symbol 1") },
            StyleEntries = new ObservableCollection<StyleEntry> { new() { Name = "Style 1" } },
            Layers = new ObservableCollection<LayerEntry> { new(new SvgGroup(), "Layer 1") }
        };

        Assert.NotNull(view.PatternList.ItemsSource);
        Assert.NotNull(view.SwatchList.ItemsSource);
        Assert.NotNull(view.BrushList.ItemsSource);
        Assert.NotNull(view.SymbolList.ItemsSource);
        Assert.NotNull(view.StyleList.ItemsSource);
        Assert.NotNull(view.LayerTree.ItemsSource);
    }

    [AvaloniaFact]
    public void PropertyInspectorView_BindsEntries()
    {
        var entries = new ObservableCollection<PropertyEntry>
        {
            new(nameof(SvgRectangle.Width), typeof(SvgRectangle).GetProperty(nameof(SvgRectangle.Width))!, "10")
        };
        var view = new PropertyInspectorView
        {
            FilteredProperties = entries
        };

        Assert.Same(entries, view.PropertiesGrid.ItemsSource);
        Assert.NotNull(view.Resources["PropertyEditorTemplate"]);
    }
}
