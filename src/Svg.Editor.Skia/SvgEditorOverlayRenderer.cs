using System;
using System.Collections.Generic;
using Svg.Editor.Svg;
using Svg.Editor.Svg.Models;
using Svg.Model.Drawables;
using Shim = ShimSkiaSharp;
using SK = SkiaSharp;

namespace Svg.Editor.Skia;

public class SvgEditorOverlayRenderer
{
    private readonly RenderingService _renderingService;

    public SvgEditorOverlayRenderer()
        : this(new RenderingService(new PathService(), new ToolService()))
    {
    }

    public SvgEditorOverlayRenderer(RenderingService renderingService)
    {
        _renderingService = renderingService ?? throw new ArgumentNullException(nameof(renderingService));
    }

    public RenderingService Service => _renderingService;

    public void Draw(
        SK.SKCanvas canvas,
        SK.SKPicture? picture,
        SK.SKRect? rootBounds,
        SK.SKRect? artboardBounds,
        float scale,
        bool snapToGrid,
        bool showGrid,
        double gridSize,
        IEnumerable<LayerEntry> layers,
        LayerEntry? selectedLayer,
        IList<DrawableBase> selectedDrawables,
        Func<DrawableBase, BoundsInfo> getBounds,
        bool polyEditing,
        DrawableBase? editPolyDrawable,
        bool editPolyline,
        IList<Shim.SKPoint> polyPoints,
        Shim.SKMatrix polyMatrix)
    {
        _renderingService.Draw(
            canvas,
            picture,
            rootBounds,
            artboardBounds,
            scale,
            snapToGrid,
            showGrid,
            gridSize,
            layers,
            selectedLayer,
            selectedDrawables,
            getBounds,
            polyEditing,
            editPolyDrawable,
            editPolyline,
            polyPoints,
            polyMatrix);
    }
}
