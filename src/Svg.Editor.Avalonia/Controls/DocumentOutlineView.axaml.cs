using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Svg.Editor.Core;

namespace Svg.Editor.Avalonia.Controls;

public partial class DocumentOutlineView : UserControl
{
    public static readonly DirectProperty<DocumentOutlineView, ObservableCollection<SvgNode>?> NodesProperty =
        AvaloniaProperty.RegisterDirect<DocumentOutlineView, ObservableCollection<SvgNode>?>(
            nameof(Nodes),
            view => view.Nodes,
            (view, value) => view.Nodes = value);

    public static readonly DirectProperty<DocumentOutlineView, ObservableCollection<ArtboardInfo>?> ArtboardsProperty =
        AvaloniaProperty.RegisterDirect<DocumentOutlineView, ObservableCollection<ArtboardInfo>?>(
            nameof(Artboards),
            view => view.Artboards,
            (view, value) => view.Artboards = value);

    private ObservableCollection<SvgNode>? _nodes;
    private ObservableCollection<ArtboardInfo>? _artboards;

    public ObservableCollection<SvgNode>? Nodes
    {
        get => _nodes;
        set
        {
            SetAndRaise(NodesProperty, ref _nodes, value);
            if (DocumentTreeControl is { })
                DocumentTreeControl.ItemsSource = value;
        }
    }

    public ObservableCollection<ArtboardInfo>? Artboards
    {
        get => _artboards;
        set
        {
            SetAndRaise(ArtboardsProperty, ref _artboards, value);
            if (ArtboardListControl is { })
                ArtboardListControl.ItemsSource = value;
        }
    }

    public TextBox FilterTextBox => FilterTextBoxControl;
    public TreeView OutlineTree => DocumentTreeControl;
    public ListBox ArtboardList => ArtboardListControl;
    public Border DropIndicator => DropIndicatorControl;

    public DocumentOutlineView()
    {
        InitializeComponent();
        DocumentTreeControl.ItemsSource = _nodes;
        ArtboardListControl.ItemsSource = _artboards;
    }
}
