using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Svg;
using Svg.Editor.Avalonia.Models;
using Svg.Editor.Svg.Models;
using Svg.Model;

namespace Svg.Editor.Avalonia;

public class SvgEditorDialogService : ISvgEditorDialogService
{
    public async Task<InsertElementResult?> ShowInsertElementAsync(Window owner, IEnumerable<string> names)
    {
        var dialog = new InsertElementWindow(names);
        return await dialog.ShowDialog<InsertElementResult?>(owner);
    }

    public async Task<PatternEditorResult?> ShowPatternEditorAsync(Window owner)
    {
        var dialog = new PatternEditorWindow();
        return await dialog.ShowDialog<PatternEditorResult?>(owner);
    }

    public async Task<GradientStopsEditorResult?> ShowGradientEditorAsync(Window owner, IEnumerable<GradientStopInfo> stops)
    {
        var dialog = new GradientEditorWindow(new System.Collections.ObjectModel.ObservableCollection<GradientStopInfo>(stops));
        return await dialog.ShowDialog<GradientStopsEditorResult?>(owner);
    }

    public async Task<GradientMeshEditorResult?> ShowGradientMeshEditorAsync(Window owner, GradientMesh mesh)
    {
        var dialog = new GradientMeshEditorWindow(mesh);
        return await dialog.ShowDialog<GradientMeshEditorResult?>(owner);
    }

    public async Task<StrokeProfileEditorResult?> ShowStrokeProfileEditorAsync(Window owner, IEnumerable<StrokePointInfo> points)
    {
        var dialog = new StrokeProfileEditorWindow(new System.Collections.ObjectModel.ObservableCollection<StrokePointInfo>(points));
        return await dialog.ShowDialog<StrokeProfileEditorResult?>(owner);
    }

    public async Task<TextEditorResult?> ShowTextEditorAsync(
        Window owner,
        string text,
        string fontFamily,
        SvgFontWeight fontWeight,
        float letterSpacing,
        float wordSpacing)
    {
        var dialog = new TextEditorWindow(text, fontFamily, fontWeight, letterSpacing, wordSpacing);
        return await dialog.ShowDialog<TextEditorResult?>(owner);
    }

    public async Task<SymbolNameResult?> ShowSymbolNameAsync(Window owner)
    {
        var dialog = new SymbolNameWindow();
        return await dialog.ShowDialog<SymbolNameResult?>(owner);
    }

    public async Task<SymbolSelectionResult?> ShowSymbolSelectionAsync(Window owner, IEnumerable<string> ids)
    {
        var dialog = new SymbolSelectWindow(ids);
        return await dialog.ShowDialog<SymbolSelectionResult?>(owner);
    }

    public async Task<SwatchEditorResult?> ShowSwatchEditorAsync(Window owner, string color)
    {
        var dialog = new SwatchEditorWindow(color);
        return await dialog.ShowDialog<SwatchEditorResult?>(owner);
    }

    public async Task<SettingsEditorResult?> ShowSettingsAsync(
        Window owner,
        bool snapToGrid,
        double gridSize,
        bool showGrid,
        bool includeHidden)
    {
        var dialog = new SettingsWindow(snapToGrid, gridSize, showGrid, includeHidden);
        return await dialog.ShowDialog<SettingsEditorResult?>(owner);
    }

    public async Task<PathEditorResult?> ShowPathEditorAsync(Window owner, SvgPath path)
    {
        var dialog = new PathEditorWindow(path);
        return await dialog.ShowDialog<PathEditorResult?>(owner);
    }
}
