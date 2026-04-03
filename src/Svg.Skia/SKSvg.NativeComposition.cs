using System;
using System.Collections.Generic;
using ShimSkiaSharp;
using Svg;
using Svg.Model;
using Svg.Model.Drawables;
using Svg.Model.Drawables.Factories;
using Svg.Transforms;

namespace Svg.Skia;

public partial class SKSvg
{
    public bool SupportsNativeComposition
    {
        get
        {
            return TryGetRenderableNativeCompositionAnimatedChildIndexes(out _, out _, out _);
        }
    }

    public bool TryCreateNativeCompositionScene(out SvgNativeCompositionScene? scene)
    {
        scene = null;

        if (!TryGetNativeCompositionState(out var sourceDocument, out var currentDocument, out var animatedChildIndexes, out var sourceBounds))
        {
            return false;
        }

        var animatedChildIndexSet = new HashSet<int>(animatedChildIndexes);
        var layers = new List<SvgNativeCompositionLayer>(sourceDocument.Children.Count);

        for (var i = 0; i < sourceDocument.Children.Count; i++)
        {
            var element = animatedChildIndexSet.Contains(i) ? currentDocument.Children[i] : sourceDocument.Children[i];
            layers.Add(CreateNativeCompositionLayer(element, i, animatedChildIndexSet.Contains(i), sourceBounds));
        }

        scene = new SvgNativeCompositionScene(sourceBounds, layers);
        return true;
    }

    public bool TryCreateNativeCompositionFrame(out SvgNativeCompositionFrame? frame)
    {
        frame = null;

        if (!TryGetNativeCompositionState(out _, out var currentDocument, out var animatedChildIndexes, out var sourceBounds))
        {
            return false;
        }

        var layers = new List<SvgNativeCompositionLayer>(animatedChildIndexes.Count);
        foreach (var animatedChildIndex in animatedChildIndexes)
        {
            if (animatedChildIndex < 0 || animatedChildIndex >= currentDocument.Children.Count)
            {
                return false;
            }

            layers.Add(CreateNativeCompositionLayer(currentDocument.Children[animatedChildIndex], animatedChildIndex, isAnimated: true, sourceBounds));
        }

        frame = new SvgNativeCompositionFrame(sourceBounds, layers);
        return true;
    }

    private bool TryGetNativeCompositionState(
        out SvgDocument sourceDocument,
        out SvgDocument currentDocument,
        out IReadOnlyList<int> animatedChildIndexes,
        out SKRect sourceBounds)
    {
        sourceDocument = null!;
        currentDocument = null!;
        animatedChildIndexes = Array.Empty<int>();
        sourceBounds = SKRect.Empty;

        if (!TryGetRenderableNativeCompositionAnimatedChildIndexes(out sourceDocument, out var renderableAnimatedChildIndexes, out sourceBounds) ||
            AnimationController is not { } animationController)
        {
            return false;
        }

        animatedChildIndexes = renderableAnimatedChildIndexes;
        currentDocument = GetNativeCompositionDocument(animationController);
        return true;
    }

    private bool TryGetRenderableNativeCompositionAnimatedChildIndexes(
        out SvgDocument sourceDocument,
        out int[] animatedChildIndexes,
        out SKRect sourceBounds)
    {
        sourceDocument = null!;
        animatedChildIndexes = Array.Empty<int>();
        sourceBounds = SKRect.Empty;

        if (SourceDocument is not { } currentSourceDocument ||
            AnimationController is not { } animationController ||
            !animationController.TryGetAnimatedTopLevelChildIndexes(out var candidateAnimatedChildIndexes) ||
            candidateAnimatedChildIndexes.Count == 0 ||
            !TryGetNativeCompositionSourceBounds(out sourceBounds))
        {
            return false;
        }

        var renderableIndexes = new int[candidateAnimatedChildIndexes.Count];
        for (var i = 0; i < candidateAnimatedChildIndexes.Count; i++)
        {
            var animatedChildIndex = candidateAnimatedChildIndexes[i];
            if (animatedChildIndex < 0 ||
                animatedChildIndex >= currentSourceDocument.Children.Count ||
                !CanRenderNativeCompositionRoot(currentSourceDocument.Children[animatedChildIndex], sourceBounds))
            {
                return false;
            }

            renderableIndexes[i] = animatedChildIndex;
        }

        sourceDocument = currentSourceDocument;
        animatedChildIndexes = renderableIndexes;
        return true;
    }

    private SvgDocument GetNativeCompositionDocument(SvgAnimationController animationController)
    {
        if (_animatedDocument is { } animatedDocument)
        {
            return animatedDocument;
        }

        if (_lastRenderedAnimationFrameState is { } renderedFrameState)
        {
            return animationController.CreateAnimatedDocument(renderedFrameState);
        }

        return AnimationTime > TimeSpan.Zero
            ? animationController.CreateAnimatedDocument(AnimationTime)
            : SourceDocument!;
    }

    private bool TryGetNativeCompositionSourceBounds(out SKRect sourceBounds)
    {
        if (Model is { } model)
        {
            sourceBounds = model.CullRect;
            return true;
        }

        if (Picture is { } picture)
        {
            sourceBounds = new SKRect(
                picture.CullRect.Left,
                picture.CullRect.Top,
                picture.CullRect.Right,
                picture.CullRect.Bottom);
            return true;
        }

        sourceBounds = SKRect.Empty;
        return false;
    }

    private SvgNativeCompositionLayer CreateNativeCompositionLayer(
        SvgElement element,
        int documentChildIndex,
        bool isAnimated,
        SKRect sourceBounds)
    {
        var references = new HashSet<Uri> { element.OwnerDocument.BaseUri };
        var drawable = DrawableFactory.Create(element, sourceBounds, null, AssetLoader, references, _ignoreAttributes);
        if (drawable is null)
        {
            return CreateHiddenNativeCompositionLayer(documentChildIndex, isAnimated);
        }

        drawable.PostProcess(sourceBounds, SKMatrix.Identity);

        var canExtractTranslation = TryGetNativeCompositionTranslation(element, out var nativeTranslation);
        var opacity = TryGetNativeCompositionOpacity(element);
        var canExtractOpacity = opacity < 1f;
        var drawBounds = GetNativeCompositionRenderBounds(drawable);
        if (canExtractTranslation)
        {
            drawBounds = OffsetRect(drawBounds, -nativeTranslation.X, -nativeTranslation.Y);
        }

        if (drawBounds.IsEmpty || drawBounds.Width <= 0 || drawBounds.Height <= 0)
        {
            return CreateHiddenNativeCompositionLayer(documentChildIndex, isAnimated);
        }

        var picture = RecordNativeCompositionPicture(
            drawable,
            drawBounds,
            extractOpacity: canExtractOpacity,
            extractTranslation: canExtractTranslation);

        var offset = new SKPoint(
            drawBounds.Left + nativeTranslation.X,
            drawBounds.Top + nativeTranslation.Y);

        return new SvgNativeCompositionLayer(
            documentChildIndex,
            isAnimated,
            picture,
            offset,
            new SKSize(drawBounds.Width, drawBounds.Height),
            opacity,
            isVisible: true);
    }

    private bool CanRenderNativeCompositionRoot(SvgElement element, SKRect sourceBounds)
    {
        var references = new HashSet<Uri> { element.OwnerDocument.BaseUri };
        var drawable = DrawableFactory.Create(element, sourceBounds, null, AssetLoader, references, _ignoreAttributes);
        if (drawable is null)
        {
            return false;
        }

        drawable.PostProcess(sourceBounds, SKMatrix.Identity);

        var drawBounds = GetNativeCompositionRenderBounds(drawable);
        if (TryGetNativeCompositionTranslation(element, out var nativeTranslation))
        {
            drawBounds = OffsetRect(drawBounds, -nativeTranslation.X, -nativeTranslation.Y);
        }

        return !drawBounds.IsEmpty && drawBounds.Width > 0 && drawBounds.Height > 0;
    }

    private static SKRect GetNativeCompositionRenderBounds(DrawableBase drawable)
    {
        var bounds = drawable.TransformedBounds;

        if (drawable is DrawableContainer container)
        {
            foreach (var child in container.ChildrenDrawables)
            {
                var childBounds = GetNativeCompositionRenderBounds(child);
                if (childBounds.IsEmpty)
                {
                    continue;
                }

                bounds = bounds.IsEmpty
                    ? childBounds
                    : SKRect.Union(bounds, childBounds);
            }
        }

        return bounds;
    }

    private static SKRect OffsetRect(SKRect rect, float dx, float dy)
    {
        if (rect.IsEmpty || (dx == 0f && dy == 0f))
        {
            return rect;
        }

        return new SKRect(rect.Left + dx, rect.Top + dy, rect.Right + dx, rect.Bottom + dy);
    }

    private static SvgNativeCompositionLayer CreateHiddenNativeCompositionLayer(int documentChildIndex, bool isAnimated)
    {
        return new SvgNativeCompositionLayer(
            documentChildIndex,
            isAnimated,
            picture: null,
            offset: SKPoint.Empty,
            size: SKSize.Empty,
            opacity: 0f,
            isVisible: false);
    }

    private SKPicture RecordNativeCompositionPicture(
        DrawableBase drawable,
        SKRect drawBounds,
        bool extractOpacity,
        bool extractTranslation)
    {
        var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(SKRect.Create(0f, 0f, drawBounds.Width, drawBounds.Height));

        if (drawBounds.Left != 0f || drawBounds.Top != 0f)
        {
            canvas.SetMatrix(SKMatrix.CreateTranslation(-drawBounds.Left, -drawBounds.Top));
        }

        var ignoreAttributes = _ignoreAttributes;
        if (extractOpacity)
        {
            ignoreAttributes |= DrawAttributes.Opacity;
        }

        drawable.Draw(canvas, ignoreAttributes, until: null, enableTransform: !extractTranslation);
        return recorder.EndRecording();
    }

    private static bool TryGetNativeCompositionTranslation(SvgElement element, out SKPoint translation)
    {
        translation = SKPoint.Empty;

        if (element.Transforms is not { Count: > 0 } transforms)
        {
            return false;
        }

        float x = 0f;
        float y = 0f;
        foreach (var transform in transforms)
        {
            if (transform is not SvgTranslate translate)
            {
                return false;
            }

            x += translate.X;
            y += translate.Y;
        }

        if (x == 0f && y == 0f)
        {
            return false;
        }

        translation = new SKPoint(x, y);
        return true;
    }

    private static float TryGetNativeCompositionOpacity(SvgElement element)
    {
        if (element is not ISvgStylable stylable)
        {
            return 1f;
        }

        var opacity = stylable.Opacity;
        if (opacity <= 0f)
        {
            return 0f;
        }

        return opacity >= 1f ? 1f : opacity;
    }
}
