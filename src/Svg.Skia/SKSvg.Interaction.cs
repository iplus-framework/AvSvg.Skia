using System;
using System.Collections.Generic;
using System.ComponentModel;
using ShimSkiaSharp;
using Svg.Model.Drawables;

namespace Svg.Skia;

public partial class SKSvg
{
    public IEnumerable<SvgSceneNode> HitTestSceneNodes(SKPoint point)
    {
        if (TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null)
        {
            foreach (var node in sceneDocument.HitTest(point))
            {
                yield return node;
            }
        }
    }

    public IEnumerable<SvgSceneNode> HitTestSceneNodes(SKRect rect)
    {
        if (TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null)
        {
            foreach (var node in sceneDocument.HitTest(rect))
            {
                yield return node;
            }
        }
    }

    public SvgSceneNode? HitTestTopmostSceneNode(SKPoint point)
    {
        if (!TryEnsureRetainedSceneGraph(out var sceneDocument) || sceneDocument is null)
        {
            return null;
        }

        return sceneDocument.HitTestTopmostNode(point);
    }

    public SvgSceneNode? HitTestTopmostSceneNode(SKPoint point, SKMatrix canvasMatrix)
    {
        return TryGetPicturePoint(point, canvasMatrix, out var picturePoint)
            ? HitTestTopmostSceneNode(picturePoint)
            : null;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use HitTestTopmostSceneNode or HitTestTopmostElement to work directly with retained scene state.")]
    public DrawableBase? HitTestTopmostDrawable(SKPoint point)
    {
        if (!TryEnsureRetainedSceneGraph(out var sceneDocument) || sceneDocument is null)
        {
            return null;
        }

        var node = sceneDocument.HitTestTopmostNode(point);
        return node is null || node.HitTestTargetElement is null
            ? null
            : new SvgSceneDrawableProxy(sceneDocument, node);
    }

    public SvgElement? HitTestTopmostElement(SKPoint point)
    {
        return HitTestTopmostSceneNode(point)?.HitTestTargetElement;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use HitTestTopmostSceneNode or HitTestTopmostElement to work directly with retained scene state.")]
    public DrawableBase? HitTestTopmostDrawable(SKPoint point, SKMatrix canvasMatrix)
    {
        return TryGetPicturePoint(point, canvasMatrix, out var picturePoint)
            ? HitTestTopmostDrawable(picturePoint)
            : null;
    }

    public SvgElement? HitTestTopmostElement(SKPoint point, SKMatrix canvasMatrix)
    {
        return TryGetPicturePoint(point, canvasMatrix, out var picturePoint)
            ? HitTestTopmostElement(picturePoint)
            : null;
    }
}
