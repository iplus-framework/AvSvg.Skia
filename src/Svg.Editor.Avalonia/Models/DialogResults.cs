using System.Collections.Generic;
using Svg;
using Svg.Editor.Svg.Models;
using Svg.Model;

namespace Svg.Editor.Avalonia.Models;

public sealed class InsertElementResult
{
    public InsertElementResult(string elementName)
    {
        ElementName = elementName;
    }

    public string ElementName { get; }
}

public sealed class PatternEditorResult
{
    public PatternEditorResult(SvgPatternServer pattern)
    {
        Pattern = pattern;
    }

    public SvgPatternServer Pattern { get; }
}

public sealed class GradientStopsEditorResult
{
    public GradientStopsEditorResult(IReadOnlyList<GradientStopInfo> stops)
    {
        Stops = stops;
    }

    public IReadOnlyList<GradientStopInfo> Stops { get; }
}

public sealed class GradientMeshEditorResult
{
    public GradientMeshEditorResult(GradientMesh mesh)
    {
        Mesh = mesh;
    }

    public GradientMesh Mesh { get; }
}

public sealed class StrokeProfileEditorResult
{
    public StrokeProfileEditorResult(IReadOnlyList<StrokePointInfo> points)
    {
        Points = points;
    }

    public IReadOnlyList<StrokePointInfo> Points { get; }
}

public sealed class TextEditorResult
{
    public TextEditorResult(string text, string fontFamily, SvgFontWeight fontWeight, float letterSpacing, float wordSpacing)
    {
        Text = text;
        FontFamily = fontFamily;
        FontWeight = fontWeight;
        LetterSpacing = letterSpacing;
        WordSpacing = wordSpacing;
    }

    public string Text { get; }
    public string FontFamily { get; }
    public SvgFontWeight FontWeight { get; }
    public float LetterSpacing { get; }
    public float WordSpacing { get; }
}

public sealed class SwatchEditorResult
{
    public SwatchEditorResult(string color)
    {
        Color = color;
    }

    public string Color { get; }
}

public sealed class SymbolNameResult
{
    public SymbolNameResult(string symbolId)
    {
        SymbolId = symbolId;
    }

    public string SymbolId { get; }
}

public sealed class SymbolSelectionResult
{
    public SymbolSelectionResult(string symbolId)
    {
        SymbolId = symbolId;
    }

    public string SymbolId { get; }
}

public sealed class SettingsEditorResult
{
    public SettingsEditorResult(bool snapToGrid, double gridSize, bool showGrid, bool includeHidden)
    {
        SnapToGrid = snapToGrid;
        GridSize = gridSize;
        ShowGrid = showGrid;
        IncludeHidden = includeHidden;
    }

    public bool SnapToGrid { get; }
    public double GridSize { get; }
    public bool ShowGrid { get; }
    public bool IncludeHidden { get; }
}

public sealed class PathEditorResult
{
    public PathEditorResult(string pathData)
    {
        PathData = pathData;
    }

    public string PathData { get; }
}
