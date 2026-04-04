using System;
using ShimSkiaSharp;
using Svg.Model;
using Svg.Model.Drawables;

namespace Svg.Skia;

internal sealed class SvgSceneDrawableProxy : DrawableBase
{
    private readonly SvgSceneDocument _sceneDocument;
    private readonly SvgSceneNode _node;

    public SvgSceneDrawableProxy(SvgSceneDocument sceneDocument, SvgSceneNode node)
        : base(sceneDocument.AssetLoader, references: null)
    {
        _sceneDocument = sceneDocument ?? throw new ArgumentNullException(nameof(sceneDocument));
        _node = node ?? throw new ArgumentNullException(nameof(node));
        Element = node.HitTestTargetElement ?? node.Element;
        IsDrawable = node.IsDrawable;
        IgnoreAttributes = sceneDocument.IgnoreAttributes;
        IsAntialias = node.IsAntialias;
        GeometryBounds = node.GeometryBounds;
        TransformedBounds = node.TransformedBounds;
        Transform = node.Transform;
        TotalTransform = node.TotalTransform;
        Overflow = node.Overflow;
        Clip = node.Clip;
        ClipPath = node.ClipPath?.DeepClone();
        Mask = node.MaskPaint?.DeepClone();
        MaskDstIn = node.MaskDstIn?.DeepClone();
        Opacity = node.Opacity?.DeepClone();
        Filter = node.Filter?.DeepClone();
        FilterClip = node.FilterClip;
        Fill = node.Fill?.DeepClone();
        Stroke = node.Stroke?.DeepClone();
    }

    public override void OnDraw(SKCanvas canvas, DrawAttributes ignoreAttributes, DrawableBase? until)
    {
        SvgSceneRenderer.RenderNodeToCanvas(_sceneDocument, _node, canvas, ignoreAttributes);
    }

    public override void Draw(SKCanvas canvas, DrawAttributes ignoreAttributes, DrawableBase? until, bool enableTransform)
    {
        if (!IsDrawable)
        {
            return;
        }

        SvgSceneRenderer.RenderNodeToCanvas(_sceneDocument, _node, canvas, ignoreAttributes, until: null, enableTransform: enableTransform);
    }

    public override SKDrawable Clone()
    {
        return new SvgSceneDrawableProxy(_sceneDocument, _node);
    }
}
