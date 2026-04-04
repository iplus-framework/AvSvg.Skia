// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using ShimSkiaSharp;
using Svg.Model.Drawables;

namespace Svg.Skia;

public partial class SKSvg
{
    /// <summary>
    /// Returns drawables that hit-test against a point in picture coordinates.
    /// </summary>
    /// <param name="point">Point in picture coordinate space.</param>
    /// <returns>Enumerable of drawables containing the point.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use HitTestSceneNodes or HitTestElements to work directly with retained scene state.")]
    public IEnumerable<DrawableBase> HitTestDrawables(SKPoint point)
    {
        foreach (var node in HitTestSceneNodes(point))
        {
            if (TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null)
            {
                yield return new SvgSceneDrawableProxy(sceneDocument, node);
            }
        }
    }

    /// <summary>
    /// Returns drawables that intersect with a rectangle in picture coordinates.
    /// </summary>
    /// <param name="rect">Rectangle in picture coordinate space.</param>
    /// <returns>Enumerable of drawables intersecting the rectangle.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use HitTestSceneNodes or HitTestElements to work directly with retained scene state.")]
    public IEnumerable<DrawableBase> HitTestDrawables(SKRect rect)
    {
        foreach (var node in HitTestSceneNodes(rect))
        {
            if (TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null)
            {
                yield return new SvgSceneDrawableProxy(sceneDocument, node);
            }
        }
    }

    /// <summary>
    /// Returns SVG elements that hit-test against a point in picture coordinates.
    /// </summary>
    /// <param name="point">Point in picture coordinate space.</param>
    /// <returns>Enumerable of elements containing the point.</returns>
    public IEnumerable<SvgElement> HitTestElements(SKPoint point)
    {
        if (TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null)
        {
            foreach (var node in SvgSceneHitTestService.HitTest(sceneDocument, point))
            {
                if (node.HitTestTargetElement is { } element)
                {
                    yield return element;
                }
            }
        }
    }

    /// <summary>
    /// Returns SVG elements that intersect with a rectangle in picture coordinates.
    /// </summary>
    /// <param name="rect">Rectangle in picture coordinate space.</param>
    /// <returns>Enumerable of elements intersecting the rectangle.</returns>
    public IEnumerable<SvgElement> HitTestElements(SKRect rect)
    {
        if (TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null)
        {
            foreach (var node in SvgSceneHitTestService.HitTest(sceneDocument, rect))
            {
                if (node.HitTestTargetElement is { } element)
                {
                    yield return element;
                }
            }
        }
    }

    /// <summary>
    /// Returns retained scene nodes that hit-test against a point in canvas coordinates.
    /// </summary>
    /// <param name="point">Point in canvas coordinate space.</param>
    /// <param name="canvasMatrix">Current canvas transform.</param>
    /// <returns>Enumerable of retained scene nodes containing the point.</returns>
    public IEnumerable<SvgSceneNode> HitTestSceneNodes(SKPoint point, SKMatrix canvasMatrix)
    {
        if (TryGetPicturePoint(point, canvasMatrix, out var picturePoint))
        {
            foreach (var node in HitTestSceneNodes(picturePoint))
            {
                yield return node;
            }
        }
    }

    /// <summary>
    /// Returns retained scene nodes that intersect with a rectangle in canvas coordinates.
    /// </summary>
    /// <param name="rect">Rectangle in canvas coordinate space.</param>
    /// <param name="canvasMatrix">Current canvas transform.</param>
    /// <returns>Enumerable of retained scene nodes intersecting the rectangle.</returns>
    public IEnumerable<SvgSceneNode> HitTestSceneNodes(SKRect rect, SKMatrix canvasMatrix)
    {
        if (TryGetPictureRect(rect, canvasMatrix, out var pictureRect))
        {
            foreach (var node in HitTestSceneNodes(pictureRect))
            {
                yield return node;
            }
        }
    }

    /// <summary>
    /// Converts a point from canvas coordinates to picture coordinates.
    /// </summary>
    /// <param name="point">The point in canvas coordinate space.</param>
    /// <param name="canvasMatrix">Current canvas transform.</param>
    /// <param name="picturePoint">Resulting point in picture coordinates.</param>
    /// <returns><c>true</c> if conversion succeeded.</returns>
    public bool TryGetPicturePoint(SKPoint point, SKMatrix canvasMatrix, out SKPoint picturePoint)
    {
        if (!canvasMatrix.TryInvert(out var inverse))
        {
            picturePoint = default;
            return false;
        }

        picturePoint = inverse.MapPoint(point);
        return true;
    }

    /// <summary>
    /// Converts a rectangle from canvas coordinates to picture coordinates.
    /// </summary>
    /// <param name="rect">The rectangle in canvas coordinate space.</param>
    /// <param name="canvasMatrix">Current canvas transform.</param>
    /// <param name="pictureRect">Resulting rectangle in picture coordinates.</param>
    /// <returns><c>true</c> if conversion succeeded.</returns>
    public bool TryGetPictureRect(SKRect rect, SKMatrix canvasMatrix, out SKRect pictureRect)
    {
        if (!canvasMatrix.TryInvert(out var inverse))
        {
            pictureRect = default;
            return false;
        }

        pictureRect = rect;
        inverse.MapRect(ref pictureRect);
        return true;
    }

    /// <summary>
    /// Returns drawables that hit-test against a point in canvas coordinates.
    /// </summary>
    /// <param name="point">Point in canvas coordinate space.</param>
    /// <param name="canvasMatrix">Current canvas transform.</param>
    /// <returns>Enumerable of drawables containing the point.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use HitTestSceneNodes or HitTestElements to work directly with retained scene state.")]
    public IEnumerable<DrawableBase> HitTestDrawables(SKPoint point, SKMatrix canvasMatrix)
    {
        if (TryGetPicturePoint(point, canvasMatrix, out var pp))
        {
            foreach (var d in HitTestDrawables(pp))
            {
                yield return d;
            }
        }
    }

    /// <summary>
    /// Returns drawables that intersect with a rectangle in canvas coordinates.
    /// </summary>
    /// <param name="rect">Rectangle in canvas coordinate space.</param>
    /// <param name="canvasMatrix">Current canvas transform.</param>
    /// <returns>Enumerable of drawables intersecting the rectangle.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use HitTestSceneNodes or HitTestElements to work directly with retained scene state.")]
    public IEnumerable<DrawableBase> HitTestDrawables(SKRect rect, SKMatrix canvasMatrix)
    {
        if (TryGetPictureRect(rect, canvasMatrix, out var pr))
        {
            foreach (var d in HitTestDrawables(pr))
            {
                yield return d;
            }
        }
    }

    /// <summary>
    /// Returns SVG elements that hit-test against a point in canvas coordinates.
    /// </summary>
    /// <param name="point">Point in canvas coordinate space.</param>
    /// <param name="canvasMatrix">Current canvas transform.</param>
    /// <returns>Enumerable of elements containing the point.</returns>
    public IEnumerable<SvgElement> HitTestElements(SKPoint point, SKMatrix canvasMatrix)
    {
        if (TryGetPicturePoint(point, canvasMatrix, out var pp))
        {
            foreach (var e in HitTestElements(pp))
            {
                yield return e;
            }
        }
    }

    /// <summary>
    /// Returns SVG elements that intersect with a rectangle in canvas coordinates.
    /// </summary>
    /// <param name="rect">Rectangle in canvas coordinate space.</param>
    /// <param name="canvasMatrix">Current canvas transform.</param>
    /// <returns>Enumerable of elements intersecting the rectangle.</returns>
    public IEnumerable<SvgElement> HitTestElements(SKRect rect, SKMatrix canvasMatrix)
    {
        if (TryGetPictureRect(rect, canvasMatrix, out var pr))
        {
            foreach (var e in HitTestElements(pr))
            {
                yield return e;
            }
        }
    }
}
