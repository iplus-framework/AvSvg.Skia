using System;
using System.Collections.Generic;
using System.Globalization;
using ShimSkiaSharp;
using Svg.Model;
using Svg.Model.Services;

namespace Svg.Skia;

internal sealed class SvgSceneFilterSource : ISvgSceneFilterSource
{
    private const DrawAttributes FilterBackgroundInput =
        DrawAttributes.ClipPath
        | DrawAttributes.Mask
        | DrawAttributes.Opacity
        | DrawAttributes.Filter;

    private const DrawAttributes FilterSourceInput =
        DrawAttributes.Filter;

    private readonly SvgSceneDocument _sceneDocument;
    private readonly SvgSceneNode _node;

    public SvgSceneFilterSource(SvgSceneDocument sceneDocument, SvgSceneNode node)
    {
        _sceneDocument = sceneDocument ?? throw new ArgumentNullException(nameof(sceneDocument));
        _node = node ?? throw new ArgumentNullException(nameof(node));
    }

    public SKPicture? SourceGraphic(SKRect? clip)
    {
        return SvgSceneRenderer.RenderNodePicture(_sceneDocument, _node, clip, FilterSourceInput, until: null, enableRootTransform: false);
    }

    public SKPicture? BackgroundImage(SKRect? clip)
    {
        if (FindContainerParentBackground(_node, out var clipRect) is not { } containerNode)
        {
            return null;
        }

        var cullRect = clip ?? CreateLocalCullRect(_node.GeometryBounds);
        if (cullRect.IsEmpty)
        {
            return null;
        }

        var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(cullRect);
        if (!clipRect.IsEmpty)
        {
            canvas.ClipRect(clipRect, SKClipOperation.Intersect);
        }

        SvgSceneRenderer.RenderNodeToCanvas(_sceneDocument, containerNode, canvas, FilterBackgroundInput, _node, enableTransform: false);
        return recorder.EndRecording();
    }

    public SKPaint? FillPaint()
    {
        return _node.Fill?.DeepClone();
    }

    public SKPaint? StrokePaint()
    {
        return _node.Stroke?.DeepClone();
    }

    private static SvgSceneNode? FindContainerParentBackground(SvgSceneNode node, out SKRect clipRect)
    {
        clipRect = SKRect.Empty;

        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (!current.CreatesBackgroundLayer)
            {
                continue;
            }

            if (current.BackgroundClip is { } backgroundClip)
            {
                clipRect = backgroundClip;
            }

            return current;
        }

        return null;
    }

    private static SKRect CreateLocalCullRect(SKRect bounds)
    {
        if (bounds.IsEmpty)
        {
            return SKRect.Empty;
        }

        return SKRect.Create(
            0f,
            0f,
            Math.Abs(bounds.Left) + bounds.Width,
            Math.Abs(bounds.Top) + bounds.Height);
    }
}
