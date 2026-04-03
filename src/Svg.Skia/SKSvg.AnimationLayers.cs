using System;
using System.Collections.Generic;
using ShimSkiaSharp;
using Svg;
using Svg.Model.Drawables;
using Svg.Model.Services;

namespace Svg.Skia;

public partial class SKSvg
{
    private sealed class AnimationLayerEntry
    {
        public AnimationLayerEntry(int documentChildIndex, int drawableChildIndex)
        {
            DocumentChildIndex = documentChildIndex;
            DrawableChildIndex = drawableChildIndex;
        }

        public int DocumentChildIndex { get; }

        public int DrawableChildIndex { get; }
    }

    private AnimationLayerEntry[]? _animationLayerEntries;
    private SKRect? _animationLayerBounds;
    private SKPicture? _staticAnimationLayerModel;
    private SKPicture? _dynamicAnimationLayerModel;
    private SkiaSharp.SKPicture? _staticAnimationLayerPicture;
    private SkiaSharp.SKPicture? _dynamicAnimationLayerPicture;

    public bool UsesAnimationLayerCaching { get; private set; }

    private bool TryInitializeAnimationLayerCaching()
    {
        if (SourceDocument is null || AnimationController is null)
        {
            return false;
        }

        if (!AnimationController.TryGetAnimatedTopLevelChildIndexes(out var animatedTopLevelChildIndexes) ||
            animatedTopLevelChildIndexes.Count == 0)
        {
            DisableAnimationLayerCaching();
            return false;
        }

        var references = new HashSet<Uri> { SourceDocument.BaseUri };
        if (SvgService.ToDrawable(SourceDocument, AssetLoader, references, out var bounds, _ignoreAttributes) is not DrawableContainer rootDrawable ||
            bounds is not { } cullRect)
        {
            DisableAnimationLayerCaching();
            return false;
        }

        var entries = new List<AnimationLayerEntry>(animatedTopLevelChildIndexes.Count);
        var animatedDrawableIndexes = new HashSet<int>();
        foreach (var documentChildIndex in animatedTopLevelChildIndexes)
        {
            if (documentChildIndex < 0 || documentChildIndex >= SourceDocument.Children.Count)
            {
                DisableAnimationLayerCaching();
                return false;
            }

            var documentChild = SourceDocument.Children[documentChildIndex];
            var drawableChildIndex = FindTopLevelDrawableIndex(rootDrawable, documentChild);
            if (drawableChildIndex < 0 || !animatedDrawableIndexes.Add(drawableChildIndex))
            {
                DisableAnimationLayerCaching();
                return false;
            }

            entries.Add(new AnimationLayerEntry(documentChildIndex, drawableChildIndex));
        }

        if (entries.Count == 0)
        {
            DisableAnimationLayerCaching();
            return false;
        }

        var staticLayerModel = RecordTopLevelDrawableLayer(rootDrawable, cullRect, animatedDrawableIndexes, includeMarked: false);
        var staticLayerPicture = SkiaModel.ToSKPicture(staticLayerModel);

        lock (Sync)
        {
            WaitForDrawsLocked();

            Drawable = rootDrawable;
            _animationLayerEntries = entries.ToArray();
            _animationLayerBounds = cullRect;
            _staticAnimationLayerModel = staticLayerModel;
            _dynamicAnimationLayerModel = null;

            _staticAnimationLayerPicture?.Dispose();
            _staticAnimationLayerPicture = staticLayerPicture;

            _dynamicAnimationLayerPicture?.Dispose();
            _dynamicAnimationLayerPicture = null;

            _picture?.Dispose();
            _picture = null;

            WireframePicture?.Dispose();
            WireframePicture = null;

            UsesAnimationLayerCaching = true;
        }

        return true;
    }

    private void DisableAnimationLayerCaching()
    {
        lock (Sync)
        {
            WaitForDrawsLocked();

            _animationLayerEntries = null;
            _animationLayerBounds = null;
            _staticAnimationLayerModel = null;
            _dynamicAnimationLayerModel = null;

            _staticAnimationLayerPicture?.Dispose();
            _staticAnimationLayerPicture = null;

            _dynamicAnimationLayerPicture?.Dispose();
            _dynamicAnimationLayerPicture = null;

            UsesAnimationLayerCaching = false;
        }
    }

    private bool TryRenderAnimationLayerFrame(SvgDocument animatedDocument)
    {
        if (!UsesAnimationLayerCaching ||
            _animationLayerEntries is not { Length: > 0 } layerEntries ||
            _animationLayerBounds is not { } cullRect ||
            Drawable is not DrawableContainer rootDrawable)
        {
            return false;
        }

        var animatedDrawableIndexes = new HashSet<int>();
        var references = new HashSet<Uri> { animatedDocument.BaseUri };
        foreach (var entry in layerEntries)
        {
            if (entry.DocumentChildIndex < 0 || entry.DocumentChildIndex >= animatedDocument.Children.Count)
            {
                return false;
            }

            if (entry.DrawableChildIndex < 0 || entry.DrawableChildIndex >= rootDrawable.ChildrenDrawables.Count)
            {
                return false;
            }

            var animatedChild = animatedDocument.Children[entry.DocumentChildIndex];
            var replacement = Svg.Model.Drawables.Factories.DrawableFactory.Create(
                animatedChild,
                rootDrawable.OwnerViewport,
                rootDrawable,
                AssetLoader,
                references,
                _ignoreAttributes);

            if (replacement is null)
            {
                return false;
            }

            rootDrawable.ChildrenDrawables[entry.DrawableChildIndex] = replacement;
            _ = animatedDrawableIndexes.Add(entry.DrawableChildIndex);
        }

        rootDrawable.Element = animatedDocument;
        rootDrawable.PostProcess(cullRect, SKMatrix.Identity);

        var dynamicLayerModel = RecordTopLevelDrawableLayer(rootDrawable, cullRect, animatedDrawableIndexes, includeMarked: true);
        var dynamicLayerPicture = SkiaModel.ToSKPicture(dynamicLayerModel);
        var compositeModel = ComposeAnimationLayerModel(_staticAnimationLayerModel, dynamicLayerModel, cullRect);

        lock (Sync)
        {
            WaitForDrawsLocked();

            Drawable = rootDrawable;
            Model = compositeModel;
            _dynamicAnimationLayerModel = dynamicLayerModel;

            _dynamicAnimationLayerPicture?.Dispose();
            _dynamicAnimationLayerPicture = dynamicLayerPicture;

            _picture?.Dispose();
            _picture = null;

            WireframePicture?.Dispose();
            WireframePicture = null;
        }

        return true;
    }

    private bool TryDrawAnimationLayers(SkiaSharp.SKCanvas canvas)
    {
        if (!UsesAnimationLayerCaching)
        {
            return false;
        }

        SkiaSharp.SKPicture? staticLayerPicture;
        SkiaSharp.SKPicture? dynamicLayerPicture;
        lock (Sync)
        {
            staticLayerPicture = _staticAnimationLayerPicture;
            dynamicLayerPicture = _dynamicAnimationLayerPicture;
        }

        if (staticLayerPicture is null && dynamicLayerPicture is null)
        {
            return false;
        }

        if (staticLayerPicture is { })
        {
            canvas.DrawPicture(staticLayerPicture);
        }

        if (dynamicLayerPicture is { })
        {
            canvas.DrawPicture(dynamicLayerPicture);
        }

        return true;
    }

    private static int FindTopLevelDrawableIndex(DrawableContainer rootDrawable, SvgElement element)
    {
        for (var i = 0; i < rootDrawable.ChildrenDrawables.Count; i++)
        {
            if (ReferenceEquals(rootDrawable.ChildrenDrawables[i].Element, element))
            {
                return i;
            }
        }

        return -1;
    }

    private static SKPicture RecordTopLevelDrawableLayer(DrawableContainer rootDrawable, SKRect cullRect, HashSet<int> markedIndexes, bool includeMarked)
    {
        var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(cullRect);

        for (var i = 0; i < rootDrawable.ChildrenDrawables.Count; i++)
        {
            var isMarked = markedIndexes.Contains(i);
            if ((includeMarked && !isMarked) || (!includeMarked && isMarked))
            {
                continue;
            }

            rootDrawable.ChildrenDrawables[i].Draw(canvas, rootDrawable.IgnoreAttributes, null, true);
        }

        return recorder.EndRecording();
    }

    private static SKPicture ComposeAnimationLayerModel(SKPicture? staticLayerModel, SKPicture? dynamicLayerModel, SKRect cullRect)
    {
        var commands = new List<CanvasCommand>();
        if (staticLayerModel?.Commands is { } staticCommands)
        {
            for (var i = 0; i < staticCommands.Count; i++)
            {
                commands.Add(staticCommands[i]);
            }
        }

        if (dynamicLayerModel?.Commands is { } dynamicCommands)
        {
            for (var i = 0; i < dynamicCommands.Count; i++)
            {
                commands.Add(dynamicCommands[i]);
            }
        }

        return new SKPicture(cullRect, commands);
    }
}
