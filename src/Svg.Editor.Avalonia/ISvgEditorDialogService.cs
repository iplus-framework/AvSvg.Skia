using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Svg;
using Svg.Editor.Avalonia.Models;
using Svg.Editor.Svg.Models;
using Svg.Model;

namespace Svg.Editor.Avalonia;

public interface ISvgEditorDialogService
{
    Task<InsertElementResult?> ShowInsertElementAsync(Window owner, IEnumerable<string> names);
    Task<PatternEditorResult?> ShowPatternEditorAsync(Window owner);
    Task<GradientStopsEditorResult?> ShowGradientEditorAsync(Window owner, IEnumerable<GradientStopInfo> stops);
    Task<GradientMeshEditorResult?> ShowGradientMeshEditorAsync(Window owner, GradientMesh mesh);
    Task<StrokeProfileEditorResult?> ShowStrokeProfileEditorAsync(Window owner, IEnumerable<StrokePointInfo> points);
    Task<TextEditorResult?> ShowTextEditorAsync(
        Window owner,
        string text,
        string fontFamily,
        SvgFontWeight fontWeight,
        float letterSpacing,
        float wordSpacing);
    Task<SymbolNameResult?> ShowSymbolNameAsync(Window owner);
    Task<SymbolSelectionResult?> ShowSymbolSelectionAsync(Window owner, IEnumerable<string> ids);
    Task<SwatchEditorResult?> ShowSwatchEditorAsync(Window owner, string color);
    Task<SettingsEditorResult?> ShowSettingsAsync(
        Window owner,
        bool snapToGrid,
        double gridSize,
        bool showGrid,
        bool includeHidden);
    Task<PathEditorResult?> ShowPathEditorAsync(Window owner, SvgPath path);
}
