using Avalonia;
using Avalonia.Controls;

namespace Svg.Editor.Avalonia.Controls;

public partial class ToolPaletteView : UserControl
{
    public static readonly DirectProperty<ToolPaletteView, string?> StrokeWidthTextProperty =
        AvaloniaProperty.RegisterDirect<ToolPaletteView, string?>(
            nameof(StrokeWidthText),
            view => view.StrokeWidthText,
            (view, value) => view.StrokeWidthText = value);

    private string? _strokeWidthText;

    public string? StrokeWidthText
    {
        get => _strokeWidthText;
        set
        {
            SetAndRaise(StrokeWidthTextProperty, ref _strokeWidthText, value);
            if (StrokeWidthBoxControl is { })
                StrokeWidthBoxControl.Text = value;
        }
    }

    public RadioButton SelectToolButton => SelectToolButtonControl;
    public RadioButton MultiSelectToolButton => MultiSelectToolButtonControl;
    public RadioButton PathToolButton => PathToolButtonControl;
    public RadioButton PolygonSelectToolButton => PolygonSelectToolButtonControl;
    public RadioButton PolylineSelectToolButton => PolylineSelectToolButtonControl;
    public RadioButton LineToolButton => LineToolButtonControl;
    public RadioButton RectToolButton => RectToolButtonControl;
    public RadioButton CircleToolButton => CircleToolButtonControl;
    public RadioButton EllipseToolButton => EllipseToolButtonControl;
    public RadioButton PolygonToolButton => PolygonToolButtonControl;
    public RadioButton PolylineToolButton => PolylineToolButtonControl;
    public RadioButton TextToolButton => TextToolButtonControl;
    public RadioButton TextPathToolButton => TextPathToolButtonControl;
    public RadioButton TextAreaToolButton => TextAreaToolButtonControl;
    public RadioButton SymbolToolButton => SymbolToolButtonControl;
    public RadioButton FreehandToolButton => FreehandToolButtonControl;
    public RadioButton PathLineToolButton => PathLineToolButtonControl;
    public RadioButton PathCubicToolButton => PathCubicToolButtonControl;
    public RadioButton PathQuadraticToolButton => PathQuadraticToolButtonControl;
    public RadioButton PathArcToolButton => PathArcToolButtonControl;
    public RadioButton PathMoveToolButton => PathMoveToolButtonControl;
    public TextBox StrokeWidthBox => StrokeWidthBoxControl;

    public ToolPaletteView()
    {
        InitializeComponent();
        StrokeWidthBoxControl.Text = _strokeWidthText;
    }
}
