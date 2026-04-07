using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Svg.Editor.Svg.Models;

namespace Svg.Editor.Avalonia.Controls;

public partial class ResourceBrowserView : UserControl
{
    public static readonly DirectProperty<ResourceBrowserView, ObservableCollection<PatternEntry>?> PatternsProperty =
        AvaloniaProperty.RegisterDirect<ResourceBrowserView, ObservableCollection<PatternEntry>?>(
            nameof(Patterns),
            view => view.Patterns,
            (view, value) => view.Patterns = value);

    public static readonly DirectProperty<ResourceBrowserView, ObservableCollection<SwatchEntry>?> SwatchesProperty =
        AvaloniaProperty.RegisterDirect<ResourceBrowserView, ObservableCollection<SwatchEntry>?>(
            nameof(Swatches),
            view => view.Swatches,
            (view, value) => view.Swatches = value);

    public static readonly DirectProperty<ResourceBrowserView, ObservableCollection<BrushEntry>?> BrushStylesProperty =
        AvaloniaProperty.RegisterDirect<ResourceBrowserView, ObservableCollection<BrushEntry>?>(
            nameof(BrushStyles),
            view => view.BrushStyles,
            (view, value) => view.BrushStyles = value);

    public static readonly DirectProperty<ResourceBrowserView, ObservableCollection<SymbolEntry>?> SymbolsProperty =
        AvaloniaProperty.RegisterDirect<ResourceBrowserView, ObservableCollection<SymbolEntry>?>(
            nameof(Symbols),
            view => view.Symbols,
            (view, value) => view.Symbols = value);

    public static readonly DirectProperty<ResourceBrowserView, ObservableCollection<StyleEntry>?> StyleEntriesProperty =
        AvaloniaProperty.RegisterDirect<ResourceBrowserView, ObservableCollection<StyleEntry>?>(
            nameof(StyleEntries),
            view => view.StyleEntries,
            (view, value) => view.StyleEntries = value);

    public static readonly DirectProperty<ResourceBrowserView, ObservableCollection<LayerEntry>?> LayersProperty =
        AvaloniaProperty.RegisterDirect<ResourceBrowserView, ObservableCollection<LayerEntry>?>(
            nameof(Layers),
            view => view.Layers,
            (view, value) => view.Layers = value);

    private ObservableCollection<PatternEntry>? _patterns;
    private ObservableCollection<SwatchEntry>? _swatches;
    private ObservableCollection<BrushEntry>? _brushStyles;
    private ObservableCollection<SymbolEntry>? _symbols;
    private ObservableCollection<StyleEntry>? _styleEntries;
    private ObservableCollection<LayerEntry>? _layers;

    public ObservableCollection<PatternEntry>? Patterns
    {
        get => _patterns;
        set
        {
            SetAndRaise(PatternsProperty, ref _patterns, value);
            if (PatternListControl is { })
                PatternListControl.ItemsSource = value;
        }
    }

    public ObservableCollection<SwatchEntry>? Swatches
    {
        get => _swatches;
        set
        {
            SetAndRaise(SwatchesProperty, ref _swatches, value);
            if (SwatchListControl is { })
                SwatchListControl.ItemsSource = value;
        }
    }

    public ObservableCollection<BrushEntry>? BrushStyles
    {
        get => _brushStyles;
        set
        {
            SetAndRaise(BrushStylesProperty, ref _brushStyles, value);
            if (BrushListControl is { })
                BrushListControl.ItemsSource = value;
        }
    }

    public ObservableCollection<SymbolEntry>? Symbols
    {
        get => _symbols;
        set
        {
            SetAndRaise(SymbolsProperty, ref _symbols, value);
            if (SymbolListControl is { })
                SymbolListControl.ItemsSource = value;
        }
    }

    public ObservableCollection<StyleEntry>? StyleEntries
    {
        get => _styleEntries;
        set
        {
            SetAndRaise(StyleEntriesProperty, ref _styleEntries, value);
            if (StyleListControl is { })
                StyleListControl.ItemsSource = value;
        }
    }

    public ObservableCollection<LayerEntry>? Layers
    {
        get => _layers;
        set
        {
            SetAndRaise(LayersProperty, ref _layers, value);
            if (LayerTreeControl is { })
                LayerTreeControl.ItemsSource = value;
        }
    }

    public ListBox PatternList => PatternListControl;
    public ListBox SwatchList => SwatchListControl;
    public ListBox BrushList => BrushListControl;
    public ListBox SymbolList => SymbolListControl;
    public ListBox StyleList => StyleListControl;
    public TreeView LayerTree => LayerTreeControl;
    public Button AddLayerButton => AddLayerButtonControl;
    public Button DeleteLayerButton => DeleteLayerButtonControl;
    public Button MoveLayerUpButton => MoveLayerUpButtonControl;
    public Button MoveLayerDownButton => MoveLayerDownButtonControl;
    public Button LockLayerButton => LockLayerButtonControl;
    public Button UnlockLayerButton => UnlockLayerButtonControl;

    public ResourceBrowserView()
    {
        InitializeComponent();
        PatternListControl.ItemsSource = _patterns;
        SwatchListControl.ItemsSource = _swatches;
        BrushListControl.ItemsSource = _brushStyles;
        SymbolListControl.ItemsSource = _symbols;
        StyleListControl.ItemsSource = _styleEntries;
        LayerTreeControl.ItemsSource = _layers;
    }
}
