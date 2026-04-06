using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ShimSkiaSharp;
using Svg.Model;
using Svg.Model.Services;

namespace Svg.Skia;

internal static class SvgSceneTextCompiler
{
    private static readonly Regex s_multipleSpaces = new(@" {2,}", RegexOptions.Compiled);

    public static bool TryCompile(
        SvgTextBase svgTextBase,
        SKRect viewport,
        SKMatrix parentTotalTransform,
        ISvgAssetLoader assetLoader,
        HashSet<Uri>? references,
        DrawAttributes ignoreAttributes,
        string? elementAddressKey,
        string? compilationRootKey,
        bool isCompilationRootBoundary,
        out SvgSceneNode? node)
    {
        node = new SvgSceneNode(
            SvgSceneNodeKindExtensions.FromElement(svgTextBase),
            svgTextBase,
            elementAddressKey,
            svgTextBase.GetType().Name,
            compilationRootKey,
            isCompilationRootBoundary)
        {
            CompilationStrategy = SvgSceneCompilationStrategy.DirectRetained,
            IsAntialias = PaintingService.IsAntialias(svgTextBase),
            Transform = TransformsService.ToMatrix(svgTextBase.Transforms)
        };

        node.TotalTransform = parentTotalTransform.PreConcat(node.Transform);
        node.IsRenderable = HasFeatures(svgTextBase, ignoreAttributes) && MaskingService.CanDraw(svgTextBase, ignoreAttributes);
        node.HitTestTargetElement = svgTextBase;
        SvgSceneCompiler.AssignRetainedVisualState(node, svgTextBase);
        SvgSceneCompiler.AssignRetainedResourceKeys(node, svgTextBase);
        node.OpacityValue = SvgScenePaintingService.AdjustSvgOpacity(svgTextBase.Opacity);
        node.Fill = SvgScenePaintingService.IsValidFill(svgTextBase)
            ? SvgScenePaintingService.GetFillPaint(svgTextBase, SKRect.Empty, assetLoader, ignoreAttributes)
            : null;
        node.Stroke = SvgScenePaintingService.IsValidStroke(svgTextBase, SKRect.Empty)
            ? SvgScenePaintingService.GetStrokePaint(svgTextBase, SKRect.Empty, assetLoader, ignoreAttributes)
            : null;
        node.SupportsFillHitTest = SvgScenePaintingService.IsValidFill(svgTextBase);
        node.SupportsStrokeHitTest = SvgScenePaintingService.IsValidStroke(svgTextBase, SKRect.Empty);
        node.StrokeWidth = node.Stroke?.StrokeWidth ?? 0f;

        var geometryBounds = EstimateGeometryBounds(svgTextBase, viewport, assetLoader);
        node.GeometryBounds = geometryBounds;
        node.TransformedBounds = node.TotalTransform.MapRect(geometryBounds);

        if (!node.IsRenderable)
        {
            return true;
        }

        var cullRect = CreateLocalCullRect(geometryBounds);
        if (cullRect.IsEmpty)
        {
            node.IsRenderable = false;
            return true;
        }

        var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(cullRect);
        DrawText(svgTextBase, viewport, ignoreAttributes | DrawAttributes.ClipPath | DrawAttributes.Mask | DrawAttributes.Opacity | DrawAttributes.Filter, canvas, assetLoader, references);
        var localModel = recorder.EndRecording();
        node.LocalModel = localModel.Commands is { Count: > 0 } ? localModel : null;

        if (node.LocalModel is null)
        {
            node.IsRenderable = false;
        }

        return true;
    }

    private static SKRect EstimateGeometryBounds(SvgTextBase svgTextBase, SKRect viewport, ISvgAssetLoader assetLoader)
    {
        var x = svgTextBase.X.Count >= 1 ? svgTextBase.X[0].ToDeviceValue(UnitRenderingType.HorizontalOffset, svgTextBase, viewport) : 0f;
        var y = svgTextBase.Y.Count >= 1 ? svgTextBase.Y[0].ToDeviceValue(UnitRenderingType.VerticalOffset, svgTextBase, viewport) : 0f;
        var dx = svgTextBase.Dx.Count >= 1 ? svgTextBase.Dx[0].ToDeviceValue(UnitRenderingType.HorizontalOffset, svgTextBase, viewport) : 0f;
        var dy = svgTextBase.Dy.Count >= 1 ? svgTextBase.Dy[0].ToDeviceValue(UnitRenderingType.VerticalOffset, svgTextBase, viewport) : 0f;

        var currentX = x + dx;
        var currentY = y + dy;
        var bounds = SKRect.Empty;
        MeasureTextBase(svgTextBase, ref currentX, ref currentY, viewport, assetLoader, ref bounds);
        return bounds;
    }

    private static void DrawText(
        SvgTextBase svgTextBase,
        SKRect viewport,
        DrawAttributes ignoreAttributes,
        SKCanvas canvas,
        ISvgAssetLoader assetLoader,
        HashSet<Uri>? references)
    {
        var xs = new List<float>();
        var ys = new List<float>();
        var dxs = new List<float>();
        var dys = new List<float>();
        GetPositionsX(svgTextBase, viewport, xs);
        GetPositionsY(svgTextBase, viewport, ys);
        GetPositionsDX(svgTextBase, viewport, dxs);
        GetPositionsDY(svgTextBase, viewport, dys);

        var x = xs.Count >= 1 ? xs[0] : 0f;
        var y = ys.Count >= 1 ? ys[0] : 0f;
        var dx = dxs.Count >= 1 ? dxs[0] : 0f;
        var dy = dys.Count >= 1 ? dys[0] : 0f;
        var currentX = x + dx;
        var currentY = y + dy;

        DrawTextBase(svgTextBase, ref currentX, ref currentY, viewport, ignoreAttributes, canvas, assetLoader, references, EstimateGeometryBounds(svgTextBase, viewport, assetLoader));
    }

    private static void DrawTextBase(
        SvgTextBase svgTextBase,
        ref float currentX,
        ref float currentY,
        SKRect viewport,
        DrawAttributes ignoreAttributes,
        SKCanvas canvas,
        ISvgAssetLoader assetLoader,
        HashSet<Uri>? references,
        SKRect rootGeometryBounds)
    {
        foreach (var node in GetContentNodes(svgTextBase))
        {
            switch (node)
            {
                case not SvgTextBase:
                    if (string.IsNullOrEmpty(node.Content))
                    {
                        break;
                    }

                    var text = PrepareText(svgTextBase, node.Content);
                    var isValidFill = SvgScenePaintingService.IsValidFill(svgTextBase);
                    var isValidStroke = SvgScenePaintingService.IsValidStroke(svgTextBase, rootGeometryBounds);

                    if ((!isValidFill && !isValidStroke) || string.IsNullOrEmpty(text))
                    {
                        break;
                    }

                    var xs = new List<float>();
                    var ys = new List<float>();
                    var dxs = new List<float>();
                    var dys = new List<float>();
                    GetPositionsX(svgTextBase, viewport, xs);
                    GetPositionsY(svgTextBase, viewport, ys);
                    GetPositionsDX(svgTextBase, viewport, dxs);
                    GetPositionsDY(svgTextBase, viewport, dys);

                    if (TryCreatePositionedCodepointPoints(text!, xs, ys, dxs, dys, out var positionedPoints))
                    {
                        var fillAdvance = 0f;
                        if (SvgScenePaintingService.IsValidFill(svgTextBase))
                        {
                            var fillPaint = SvgScenePaintingService.GetFillPaint(svgTextBase, rootGeometryBounds, assetLoader, ignoreAttributes);
                            if (fillPaint is not null)
                            {
                                fillAdvance = DrawPositionedTextRuns(svgTextBase, text!, positionedPoints, rootGeometryBounds, fillPaint, canvas, assetLoader);
                            }
                        }

                        var strokeAdvance = 0f;
                        if (SvgScenePaintingService.IsValidStroke(svgTextBase, rootGeometryBounds))
                        {
                            var strokePaint = SvgScenePaintingService.GetStrokePaint(svgTextBase, rootGeometryBounds, assetLoader, ignoreAttributes);
                            if (strokePaint is not null)
                            {
                                strokeAdvance = DrawPositionedTextRuns(svgTextBase, text!, positionedPoints, rootGeometryBounds, strokePaint, canvas, assetLoader);
                            }
                        }

                        currentX = positionedPoints[positionedPoints.Length - 1].X + Math.Max(fillAdvance, strokeAdvance);
                        currentY = positionedPoints[positionedPoints.Length - 1].Y;
                        break;
                    }

                    var x = xs.Count >= 1 ? xs[0] : currentX;
                    var y = ys.Count >= 1 ? ys[0] : currentY;
                    var dx = dxs.Count >= 1 ? dxs[0] : 0f;
                    var dy = dys.Count >= 1 ? dys[0] : 0f;
                    currentX = x + dx;
                    currentY = y + dy;
                    DrawTextString(svgTextBase, text!, ref currentX, ref currentY, rootGeometryBounds, ignoreAttributes, canvas, assetLoader, references);
                    break;

                case SvgTextPath svgTextPath:
                    DrawTextPath(svgTextPath, ref currentX, ref currentY, viewport, ignoreAttributes, canvas, assetLoader, references);
                    break;

                case SvgTextRef svgTextRef:
                    DrawTextRef(svgTextRef, ref currentX, ref currentY, viewport, ignoreAttributes, canvas, assetLoader, references, rootGeometryBounds);
                    break;

                case SvgTextSpan svgTextSpan:
                    DrawTextBase(svgTextSpan, ref currentX, ref currentY, viewport, ignoreAttributes, canvas, assetLoader, references, rootGeometryBounds);
                    break;
            }
        }
    }

    private static void DrawTextString(
        SvgTextBase svgTextBase,
        string text,
        ref float x,
        ref float y,
        SKRect geometryBounds,
        DrawAttributes ignoreAttributes,
        SKCanvas canvas,
        ISvgAssetLoader assetLoader,
        HashSet<Uri>? references)
    {
        var fillAdvance = 0f;
        if (SvgScenePaintingService.IsValidFill(svgTextBase))
        {
            var fillPaint = SvgScenePaintingService.GetFillPaint(svgTextBase, geometryBounds, assetLoader, ignoreAttributes);
            if (fillPaint is not null)
            {
                fillAdvance = DrawTextRuns(svgTextBase, text, x, y, geometryBounds, fillPaint, canvas, assetLoader);
            }
        }

        var strokeAdvance = 0f;
        if (SvgScenePaintingService.IsValidStroke(svgTextBase, geometryBounds))
        {
            var strokePaint = SvgScenePaintingService.GetStrokePaint(svgTextBase, geometryBounds, assetLoader, ignoreAttributes);
            if (strokePaint is not null)
            {
                strokeAdvance = DrawTextRuns(svgTextBase, text, x, y, geometryBounds, strokePaint, canvas, assetLoader);
            }
        }

        x += Math.Max(strokeAdvance, fillAdvance);
    }

    private static float DrawTextRuns(
        SvgTextBase svgTextBase,
        string text,
        float anchorX,
        float anchorY,
        SKRect geometryBounds,
        SKPaint paint,
        SKCanvas canvas,
        ISvgAssetLoader assetLoader)
    {
        PaintingService.SetPaintText(svgTextBase, geometryBounds, paint);

        var textAlign = paint.TextAlign;
        var typefaceSpans = assetLoader.FindTypefaces(text, paint);
        if (typefaceSpans.Count == 0)
        {
            return 0f;
        }

        var totalAdvance = 0f;
        foreach (var span in typefaceSpans)
        {
            totalAdvance += span.Advance;
        }

        var currentX = anchorX;
        if (textAlign == SKTextAlign.Center)
        {
            currentX -= totalAdvance * 0.5f;
        }
        else if (textAlign == SKTextAlign.Right)
        {
            currentX -= totalAdvance;
        }

        paint.TextAlign = SKTextAlign.Left;

        foreach (var typefaceSpan in typefaceSpans)
        {
            paint.Typeface = typefaceSpan.Typeface;
            canvas.DrawText(typefaceSpan.Text, currentX, anchorY, paint);
            currentX += typefaceSpan.Advance;
            paint = paint.Clone();
        }

        return totalAdvance;
    }

    private static float DrawPositionedTextRuns(
        SvgTextBase svgTextBase,
        string text,
        SKPoint[] points,
        SKRect geometryBounds,
        SKPaint paint,
        SKCanvas canvas,
        ISvgAssetLoader assetLoader)
    {
        PaintingService.SetPaintText(svgTextBase, geometryBounds, paint);

        var lastCodepointStart = GetLastCodepointStart(text);
        var leadingText = text.Substring(0, lastCodepointStart);
        if (!string.IsNullOrEmpty(leadingText))
        {
            var offset = 0;
            foreach (var typefaceSpan in assetLoader.FindTypefaces(leadingText, paint))
            {
                var localPaint = paint.Clone();
                localPaint.Typeface = typefaceSpan.Typeface;

                var codepointCount = CountCodepoints(typefaceSpan.Text);
                var spanPoints = new SKPoint[codepointCount];
                Array.Copy(points, offset, spanPoints, 0, codepointCount);

                var textBlob = SKTextBlob.CreatePositioned(typefaceSpan.Text, spanPoints);
                canvas.DrawText(textBlob, 0, 0, localPaint);
                offset += codepointCount;
            }
        }

        var trailingText = text.Substring(lastCodepointStart);
        foreach (var typefaceSpan in assetLoader.FindTypefaces(trailingText, paint))
        {
            var localPaint = paint.Clone();
            localPaint.Typeface = typefaceSpan.Typeface;
            canvas.DrawText(typefaceSpan.Text, points[points.Length - 1].X, points[points.Length - 1].Y, localPaint);
            return typefaceSpan.Advance;
        }

        var fallbackPaint = paint.Clone();
        canvas.DrawText(trailingText, points[points.Length - 1].X, points[points.Length - 1].Y, fallbackPaint);
        var fallbackBounds = new SKRect();
        return assetLoader.MeasureText(trailingText, fallbackPaint, ref fallbackBounds);
    }

    private static void DrawTextPath(
        SvgTextPath svgTextPath,
        ref float currentX,
        ref float currentY,
        SKRect viewport,
        DrawAttributes ignoreAttributes,
        SKCanvas canvas,
        ISvgAssetLoader assetLoader,
        HashSet<Uri>? references)
    {
        if (!HasFeatures(svgTextPath, ignoreAttributes) ||
            !MaskingService.CanDraw(svgTextPath, ignoreAttributes) ||
            SvgService.HasRecursiveReference(svgTextPath, static e => e.ReferencedPath, new HashSet<Uri>()))
        {
            return;
        }

        var svgPath = SvgService.GetReference<SvgPath>(svgTextPath, svgTextPath.ReferencedPath);
        var skPath = svgPath?.PathData?.ToPath(svgPath.FillRule);
        if (skPath is null || skPath.IsEmpty)
        {
            return;
        }

        var geometryBounds = skPath.Bounds;
        var startOffset = svgTextPath.StartOffset.ToDeviceValue(UnitRenderingType.Other, svgTextPath, viewport);
        var hOffset = currentX + startOffset;
        var vOffset = currentY;
        var text = PrepareText(svgTextPath, svgTextPath.Text);
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (SvgScenePaintingService.IsValidFill(svgTextPath))
        {
            var fillPaint = SvgScenePaintingService.GetFillPaint(svgTextPath, geometryBounds, assetLoader, ignoreAttributes);
            if (fillPaint is not null)
            {
                PaintingService.SetPaintText(svgTextPath, geometryBounds, fillPaint);
                canvas.DrawTextOnPath(text!, skPath, hOffset, vOffset, fillPaint);
            }
        }

        if (SvgScenePaintingService.IsValidStroke(svgTextPath, geometryBounds))
        {
            var strokePaint = SvgScenePaintingService.GetStrokePaint(svgTextPath, geometryBounds, assetLoader, ignoreAttributes);
            if (strokePaint is not null)
            {
                PaintingService.SetPaintText(svgTextPath, geometryBounds, strokePaint);
                canvas.DrawTextOnPath(text!, skPath, hOffset, vOffset, strokePaint);
            }
        }
    }

    private static void DrawTextRef(
        SvgTextRef svgTextRef,
        ref float currentX,
        ref float currentY,
        SKRect viewport,
        DrawAttributes ignoreAttributes,
        SKCanvas canvas,
        ISvgAssetLoader assetLoader,
        HashSet<Uri>? references,
        SKRect rootGeometryBounds)
    {
        if (!HasFeatures(svgTextRef, ignoreAttributes) ||
            !MaskingService.CanDraw(svgTextRef, ignoreAttributes) ||
            SvgService.HasRecursiveReference(svgTextRef, static e => e.ReferencedElement, new HashSet<Uri>()))
        {
            return;
        }

        var svgReferencedText = SvgService.GetReference<SvgText>(svgTextRef, svgTextRef.ReferencedElement);
        if (svgReferencedText is null)
        {
            return;
        }

        DrawTextBase(svgReferencedText, ref currentX, ref currentY, viewport, ignoreAttributes, canvas, assetLoader, references, rootGeometryBounds);
    }

    private static void MeasureTextBase(
        SvgTextBase svgTextBase,
        ref float currentX,
        ref float currentY,
        SKRect viewport,
        ISvgAssetLoader assetLoader,
        ref SKRect bounds)
    {
        foreach (var node in GetContentNodes(svgTextBase))
        {
            switch (node)
            {
                case not SvgTextBase:
                    if (string.IsNullOrEmpty(node.Content))
                    {
                        break;
                    }

                    var text = PrepareText(svgTextBase, node.Content);
                    if (string.IsNullOrEmpty(text))
                    {
                        break;
                    }

                    var xs = new List<float>();
                    var ys = new List<float>();
                    var dxs = new List<float>();
                    var dys = new List<float>();
                    GetPositionsX(svgTextBase, viewport, xs);
                    GetPositionsY(svgTextBase, viewport, ys);
                    GetPositionsDX(svgTextBase, viewport, dxs);
                    GetPositionsDY(svgTextBase, viewport, dys);

                    if (TryCreatePositionedCodepointPoints(text!, xs, ys, dxs, dys, out var positionedPoints))
                    {
                        var positionedTextBounds = MeasurePositionedTextStringBounds(svgTextBase, text!, positionedPoints, viewport, assetLoader, out var positionedAdvance);
                        UnionBounds(ref bounds, positionedTextBounds);
                        currentX = positionedPoints[positionedPoints.Length - 1].X + positionedAdvance;
                        currentY = positionedPoints[positionedPoints.Length - 1].Y;
                        break;
                    }

                    var x = xs.Count >= 1 ? xs[0] : currentX;
                    var y = ys.Count >= 1 ? ys[0] : currentY;
                    var dx = dxs.Count >= 1 ? dxs[0] : 0f;
                    var dy = dys.Count >= 1 ? dys[0] : 0f;
                    currentX = x + dx;
                    currentY = y + dy;

                    var textBounds = MeasureTextStringBounds(svgTextBase, text!, currentX, currentY, viewport, assetLoader, out var advance);
                    UnionBounds(ref bounds, textBounds);
                    currentX += advance;
                    break;

                case SvgTextPath svgTextPath:
                    MeasureTextPath(svgTextPath, ref currentX, ref currentY, viewport, assetLoader, ref bounds);
                    break;

                case SvgTextRef svgTextRef:
                    MeasureTextRef(svgTextRef, ref currentX, ref currentY, viewport, assetLoader, ref bounds);
                    break;

                case SvgTextSpan svgTextSpan:
                    MeasureTextBase(svgTextSpan, ref currentX, ref currentY, viewport, assetLoader, ref bounds);
                    break;
            }
        }
    }

    private static void MeasureTextPath(
        SvgTextPath svgTextPath,
        ref float currentX,
        ref float currentY,
        SKRect viewport,
        ISvgAssetLoader assetLoader,
        ref SKRect bounds)
    {
        if (SvgService.HasRecursiveReference(svgTextPath, static e => e.ReferencedPath, new HashSet<Uri>()))
        {
            return;
        }

        var svgPath = SvgService.GetReference<SvgPath>(svgTextPath, svgTextPath.ReferencedPath);
        var skPath = svgPath?.PathData?.ToPath(svgPath.FillRule);
        if (skPath is null || skPath.IsEmpty)
        {
            return;
        }

        var paint = new SKPaint();
        PaintingService.SetPaintText(svgTextPath, skPath.Bounds, paint);
        var metrics = assetLoader.GetFontMetrics(paint);
        var inflate = Math.Max(Math.Abs(metrics.Ascent), Math.Abs(metrics.Descent));
        var pathBounds = skPath.Bounds;
        var measuredBounds = SKRect.Create(
            pathBounds.Left,
            pathBounds.Top - inflate,
            pathBounds.Width,
            pathBounds.Height + (inflate * 2f));
        UnionBounds(ref bounds, measuredBounds);
    }

    private static void MeasureTextRef(
        SvgTextRef svgTextRef,
        ref float currentX,
        ref float currentY,
        SKRect viewport,
        ISvgAssetLoader assetLoader,
        ref SKRect bounds)
    {
        if (SvgService.HasRecursiveReference(svgTextRef, static e => e.ReferencedElement, new HashSet<Uri>()))
        {
            return;
        }

        var svgReferencedText = SvgService.GetReference<SvgText>(svgTextRef, svgTextRef.ReferencedElement);
        if (svgReferencedText is null)
        {
            return;
        }

        MeasureTextBase(svgReferencedText, ref currentX, ref currentY, viewport, assetLoader, ref bounds);
    }

    private static SKRect MeasureTextStringBounds(
        SvgTextBase svgTextBase,
        string text,
        float anchorX,
        float anchorY,
        SKRect viewport,
        ISvgAssetLoader assetLoader,
        out float advance)
    {
        var paint = new SKPaint();
        PaintingService.SetPaintText(svgTextBase, viewport, paint);

        var totalAdvance = 0f;
        var typefaceSpans = assetLoader.FindTypefaces(text, paint);
        if (typefaceSpans.Count > 0)
        {
            foreach (var span in typefaceSpans)
            {
                totalAdvance += span.Advance;
            }
        }
        else
        {
            var scratchBounds = new SKRect();
            totalAdvance = assetLoader.MeasureText(text, paint, ref scratchBounds);
        }

        var startX = anchorX;
        if (paint.TextAlign == SKTextAlign.Center)
        {
            startX -= totalAdvance * 0.5f;
        }
        else if (paint.TextAlign == SKTextAlign.Right)
        {
            startX -= totalAdvance;
        }

        var metrics = assetLoader.GetFontMetrics(paint);
        advance = totalAdvance;
        return new SKRect(startX, anchorY + metrics.Ascent, startX + totalAdvance, anchorY + metrics.Descent);
    }

    private static SKRect MeasurePositionedTextStringBounds(
        SvgTextBase svgTextBase,
        string text,
        SKPoint[] points,
        SKRect viewport,
        ISvgAssetLoader assetLoader,
        out float advance)
    {
        var paint = new SKPaint();
        PaintingService.SetPaintText(svgTextBase, viewport, paint);

        var bounds = SKRect.Empty;
        advance = 0f;

        var pointIndex = 0;
        var spans = assetLoader.FindTypefaces(text, paint);
        if (spans.Count == 0)
        {
            MeasurePositionedCodepoints(text, points, paint, assetLoader, ref bounds, ref pointIndex, ref advance);
            return bounds;
        }

        foreach (var span in spans)
        {
            var localPaint = paint.Clone();
            localPaint.Typeface = span.Typeface;
            MeasurePositionedCodepoints(span.Text, points, localPaint, assetLoader, ref bounds, ref pointIndex, ref advance);
        }

        return bounds;
    }

    private static void UnionBounds(ref SKRect bounds, SKRect candidate)
    {
        if (candidate.IsEmpty)
        {
            return;
        }

        bounds = bounds.IsEmpty
            ? candidate
            : SKRect.Union(bounds, candidate);
    }

    private static void GetPositionsX(SvgTextBase svgTextBase, SKRect viewport, List<float> xs)
    {
        for (var i = 0; i < svgTextBase.X.Count; i++)
        {
            xs.Add(svgTextBase.X[i].ToDeviceValue(UnitRenderingType.HorizontalOffset, svgTextBase, viewport));
        }
    }

    private static void GetPositionsY(SvgTextBase svgTextBase, SKRect viewport, List<float> ys)
    {
        for (var i = 0; i < svgTextBase.Y.Count; i++)
        {
            ys.Add(svgTextBase.Y[i].ToDeviceValue(UnitRenderingType.VerticalOffset, svgTextBase, viewport));
        }
    }

    private static void GetPositionsDX(SvgTextBase svgTextBase, SKRect viewport, List<float> dxs)
    {
        for (var i = 0; i < svgTextBase.Dx.Count; i++)
        {
            dxs.Add(svgTextBase.Dx[i].ToDeviceValue(UnitRenderingType.HorizontalOffset, svgTextBase, viewport));
        }
    }

    private static void GetPositionsDY(SvgTextBase svgTextBase, SKRect viewport, List<float> dys)
    {
        for (var i = 0; i < svgTextBase.Dy.Count; i++)
        {
            dys.Add(svgTextBase.Dy[i].ToDeviceValue(UnitRenderingType.VerticalOffset, svgTextBase, viewport));
        }
    }

    private static bool TryCreatePositionedCodepointPoints(
        string text,
        IReadOnlyList<float> xs,
        IReadOnlyList<float> ys,
        IReadOnlyList<float> dxs,
        IReadOnlyList<float> dys,
        out SKPoint[] points)
    {
        var codepointCount = CountCodepoints(text);
        if (xs.Count < 1 || ys.Count < 1 || xs.Count != ys.Count || xs.Count != codepointCount)
        {
            points = Array.Empty<SKPoint>();
            return false;
        }

        points = new SKPoint[codepointCount];
        for (var i = 0; i < codepointCount; i++)
        {
            var dx = dxs.Count >= 1 && i < dxs.Count ? dxs[i] : 0f;
            var dy = dys.Count >= 1 && i < dys.Count ? dys[i] : 0f;
            points[i] = new SKPoint(xs[i] + dx, ys[i] + dy);
        }

        return true;
    }

    private static void MeasurePositionedCodepoints(
        string text,
        SKPoint[] points,
        SKPaint paint,
        ISvgAssetLoader assetLoader,
        ref SKRect bounds,
        ref int pointIndex,
        ref float advance)
    {
        var charIndex = 0;
        while (TryReadNextCodepoint(text, ref charIndex, out var codepoint))
        {
            var glyphBounds = new SKRect();
            var glyphAdvance = assetLoader.MeasureText(codepoint, paint, ref glyphBounds);
            var metrics = assetLoader.GetFontMetrics(paint);
            var point = points[pointIndex++];
            var candidate = glyphBounds.IsEmpty
                ? new SKRect(point.X, point.Y + metrics.Ascent, point.X + glyphAdvance, point.Y + metrics.Descent)
                : new SKRect(point.X + glyphBounds.Left, point.Y + glyphBounds.Top, point.X + glyphBounds.Right, point.Y + glyphBounds.Bottom);
            UnionBounds(ref bounds, candidate);
            advance = glyphAdvance;
        }
    }

    private static int CountCodepoints(string text)
    {
        return text.Length - CountLowSurrogates(text);
    }

    private static int GetLastCodepointStart(string text)
    {
        return text.Length - (char.IsLowSurrogate(text[text.Length - 1]) ? 2 : 1);
    }

    private static bool TryReadNextCodepoint(string text, ref int charIndex, out string codepoint)
    {
        if (charIndex >= text.Length)
        {
            codepoint = string.Empty;
            return false;
        }

        var start = charIndex++;
        if (charIndex < text.Length && char.IsHighSurrogate(text[start]) && char.IsLowSurrogate(text[charIndex]))
        {
            charIndex++;
        }

        codepoint = text.Substring(start, charIndex - start);
        return true;
    }

    private static int CountLowSurrogates(string text)
    {
        var count = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsLowSurrogate(text[i]))
            {
                count++;
            }
        }

        return count;
    }

    private static IEnumerable<ISvgNode> GetContentNodes(SvgTextBase svgTextBase)
    {
        if (svgTextBase.Nodes is null || svgTextBase.Nodes.Count < 1)
        {
            foreach (var child in svgTextBase.Children)
            {
                if (child is ISvgNode svgNode &&
                    child is not ISvgDescriptiveElement &&
                    child is not NonSvgElement)
                {
                    yield return svgNode;
                }
            }
        }
        else
        {
            foreach (var node in svgTextBase.Nodes)
            {
                if (node is NonSvgElement)
                {
                    continue;
                }

                yield return node;
            }
        }
    }

    private static string? PrepareText(SvgTextBase svgTextBase, string? value)
    {
        value = ApplyTransformation(svgTextBase, value);
        if (value is null)
        {
            return null;
        }

        value = new StringBuilder(value)
            .Replace("\r\n", " ")
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('\t', ' ')
            .ToString();

        return svgTextBase.SpaceHandling == XmlSpaceHandling.Preserve
            ? value
            : s_multipleSpaces.Replace(value.TrimStart(), " ");
    }

    private static string? ApplyTransformation(SvgTextBase svgTextBase, string? value)
    {
        if (value is null)
        {
            return null;
        }

        return svgTextBase.TextTransformation switch
        {
            SvgTextTransformation.Capitalize => value.ToUpper(CultureInfo.CurrentCulture),
            SvgTextTransformation.Uppercase => value.ToUpper(CultureInfo.CurrentCulture),
            SvgTextTransformation.Lowercase => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value),
            _ => value
        };
    }

    private static bool HasFeatures(SvgElement element, DrawAttributes ignoreAttributes)
    {
        var hasRequiredFeatures = ignoreAttributes.HasFlag(DrawAttributes.RequiredFeatures) || element.HasRequiredFeatures();
        var hasRequiredExtensions = ignoreAttributes.HasFlag(DrawAttributes.RequiredExtensions) || element.HasRequiredExtensions();
        var hasSystemLanguage = ignoreAttributes.HasFlag(DrawAttributes.SystemLanguage) || element.HasSystemLanguage();
        return hasRequiredFeatures && hasRequiredExtensions && hasSystemLanguage;
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
