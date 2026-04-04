using System;
using System.Collections.Generic;
using System.Globalization;
using ShimSkiaSharp;
using Svg;
using Svg.Model;
using Svg.Model.Drawables;
using Svg.Model.Drawables.Elements;
using Svg.Model.Services;

namespace Svg.Skia;

public partial class SKSvg
{
    private static readonly HashSet<string> s_inheritedAnimationAttributes = new(StringComparer.Ordinal)
    {
        "alphabetic",
        "ascent",
        "ascent-height",
        "clip",
        "clip-rule",
        "color",
        "color-interpolation",
        "color-interpolation-filters",
        "descent",
        "dominant-baseline",
        "fill",
        "fill-opacity",
        "fill-rule",
        "flood-color",
        "flood-opacity",
        "font",
        "font-family",
        "font-size",
        "font-stretch",
        "font-style",
        "font-variant",
        "font-weight",
        "glyph-name",
        "horiz-adv-x",
        "horiz-origin-x",
        "horiz-origin-y",
        "k",
        "lengthAdjust",
        "letter-spacing",
        "shape-rendering",
        "space",
        "stop-color",
        "stop-opacity",
        "stroke",
        "stroke-dasharray",
        "stroke-dashoffset",
        "stroke-linecap",
        "stroke-linejoin",
        "stroke-miterlimit",
        "stroke-opacity",
        "stroke-width",
        "text-anchor",
        "text-decoration",
        "text-transform",
        "textLength",
        "units-per-em",
        "vert-adv-y",
        "vert-origin-x",
        "vert-origin-y",
        "visibility",
        "word-spacing",
        "x-height"
    };

    private sealed class CachedPictureDrawable : DrawableBase
    {
        private readonly SKPicture _picture;

        public CachedPictureDrawable(DrawableBase source, SKPicture picture)
            : base(source.AssetLoader, CloneReferences(source.References))
        {
            _picture = picture;
            Element = source.Element;
            Parent = source.Parent;
            OwnerViewport = source.OwnerViewport;
            IsDrawable = true;
            IgnoreAttributes = source.IgnoreAttributes;
            IsAntialias = source.IsAntialias;
            GeometryBounds = source.GeometryBounds;
            TransformedBounds = source.TransformedBounds;
            Transform = SKMatrix.Identity;
            TotalTransform = SKMatrix.Identity;
        }

        private CachedPictureDrawable(CachedPictureDrawable source)
            : this(source, source._picture)
        {
        }

        public override void OnDraw(SKCanvas canvas, DrawAttributes ignoreAttributes, DrawableBase? until)
        {
            if (until is { } && this == until)
            {
                return;
            }

            canvas.DrawPicture(_picture);
        }

        public override void PostProcess(SKRect? viewport, SKMatrix totalMatrix)
        {
            TotalTransform = totalMatrix;
            TransformedBounds = GeometryBounds;
        }

        public override SKDrawable Clone()
        {
            return new CachedPictureDrawable(this);
        }
    }

    private sealed class SkippedDrawable : DrawableBase
    {
        public SkippedDrawable(DrawableBase source)
            : base(source.AssetLoader, CloneReferences(source.References))
        {
            Element = source.Element;
            Parent = source.Parent;
            OwnerViewport = source.OwnerViewport;
            IgnoreAttributes = source.IgnoreAttributes;
            IsAntialias = source.IsAntialias;
            GeometryBounds = SKRect.Empty;
            TransformedBounds = SKRect.Empty;
            Transform = SKMatrix.Identity;
            TotalTransform = SKMatrix.Identity;
            IsDrawable = false;
        }

        private SkippedDrawable(SkippedDrawable source)
            : this((DrawableBase)source)
        {
        }

        public override void OnDraw(SKCanvas canvas, DrawAttributes ignoreAttributes, DrawableBase? until)
        {
        }

        public override SKDrawable Clone()
        {
            return new SkippedDrawable(this);
        }
    }

    private sealed class AnimationSubtreeNode
    {
        private readonly List<AnimationSubtreeNode> _children = new();

        private AnimationSubtreeNode(AnimationSubtreeNode? parent, DrawableBase drawable)
        {
            Parent = parent;
            Drawable = drawable;
            ElementKey = TryGetElementAddressKey(drawable.Element);
        }

        public AnimationSubtreeNode? Parent { get; }

        public DrawableBase Drawable { get; private set; }

        public string? ElementKey { get; private set; }

        public IReadOnlyList<AnimationSubtreeNode> Children => _children;

        public int Order { get; private set; }

        public SKPicture? Model { get; private set; }

        public SkiaSharp.SKPicture? Picture { get; private set; }

        public bool IsDirty { get; private set; }

        public static AnimationSubtreeNode Create(AnimationSubtreeNode? parent, DrawableBase drawable)
        {
            var node = new AnimationSubtreeNode(parent, drawable);
            var childSlots = GetChildSlots(drawable);
            if (childSlots is { Count: > 0 })
            {
                for (var i = 0; i < childSlots.Count; i++)
                {
                    node._children.Add(Create(node, childSlots[i].Child));
                }
            }

            return node;
        }

        public void Sync(SKSvg owner, DrawableBase drawable, List<SkiaSharp.SKPicture> deferredDisposals)
        {
            Drawable = drawable;
            ElementKey = TryGetElementAddressKey(drawable.Element);

            var childSlots = GetChildSlots(drawable);
            if (childSlots is not { Count: > 0 })
            {
                if (_children.Count > 0)
                {
                    for (var i = 0; i < _children.Count; i++)
                    {
                        _children[i].Dispose(owner, deferredDisposals);
                    }

                    _children.Clear();
                    MarkDirty();
                }

                return;
            }

            for (var i = 0; i < childSlots.Count; i++)
            {
                var childDrawable = childSlots[i].Child;
                var childElementKey = TryGetElementAddressKey(childDrawable.Element);

                if (i < _children.Count && CanReuseNode(_children[i], childDrawable, childElementKey))
                {
                    _children[i].Sync(owner, childDrawable, deferredDisposals);
                    continue;
                }

                if (i < _children.Count)
                {
                    _children[i].Dispose(owner, deferredDisposals);
                    _children[i] = Create(this, childDrawable);
                    _children[i].MarkDirty();
                }
                else
                {
                    var newNode = Create(this, childDrawable);
                    newNode.MarkDirty();
                    _children.Add(newNode);
                }
            }

            if (_children.Count > childSlots.Count)
            {
                for (var i = childSlots.Count; i < _children.Count; i++)
                {
                    _children[i].Dispose(owner, deferredDisposals);
                }

                _children.RemoveRange(childSlots.Count, _children.Count - childSlots.Count);
                MarkDirty();
            }
        }

        public void MarkDirty()
        {
            if (IsDirty)
            {
                return;
            }

            IsDirty = true;
            Parent?.MarkDirty();
        }

        public void MarkSubtreeDirty()
        {
            MarkDirty();

            for (var i = 0; i < _children.Count; i++)
            {
                _children[i].MarkSubtreeDirtyCore();
            }
        }

        public bool Contains(AnimationSubtreeNode? candidate)
        {
            var current = candidate;
            while (current is not null)
            {
                if (ReferenceEquals(current, this))
                {
                    return true;
                }

                current = current.Parent;
            }

            return false;
        }

        public void PopulateLookup(Dictionary<string, AnimationSubtreeNode> lookup, ref int order)
        {
            Order = order++;

            var elementKey = ElementKey;
            if (!string.IsNullOrWhiteSpace(elementKey) && !lookup.ContainsKey(elementKey!))
            {
                lookup.Add(elementKey!, this);
            }

            for (var i = 0; i < _children.Count; i++)
            {
                _children[i].PopulateLookup(lookup, ref order);
            }
        }

        public void Rebuild(SKSvg owner, List<SkiaSharp.SKPicture> deferredDisposals)
        {
            for (var i = 0; i < _children.Count; i++)
            {
                _children[i].Rebuild(owner, deferredDisposals);
            }

            if (!IsDirty && Model is { } && Picture is { })
            {
                return;
            }

            var newModel = RecordNodeModel(this);
            var newPicture = owner.SkiaModel.ToSKPicture(newModel);

            owner.ReplaceRegisteredPicture(Model, Picture, newModel, newPicture, deferredDisposals);

            Model = newModel;
            Picture = newPicture;
            IsDirty = false;
        }

        public void Dispose(SKSvg owner, List<SkiaSharp.SKPicture>? deferredDisposals = null)
        {
            for (var i = 0; i < _children.Count; i++)
            {
                _children[i].Dispose(owner, deferredDisposals);
            }

            _children.Clear();

            owner.DisposeRegisteredPicture(Model, Picture, deferredDisposals);
            Model = null;
            Picture = null;
            IsDirty = false;
        }

        private void MarkSubtreeDirtyCore()
        {
            IsDirty = true;

            for (var i = 0; i < _children.Count; i++)
            {
                _children[i].MarkSubtreeDirtyCore();
            }
        }
    }

    private sealed class AnimationCacheEntry
    {
        public AnimationCacheEntry(AnimationSubtreeNode rootNode)
        {
            RootNode = rootNode;
        }

        public AnimationSubtreeNode RootNode { get; }
    }

    private sealed class AnimationLayerEntry
    {
        private readonly HashSet<string> _animatedTargetKeys = new(StringComparer.Ordinal);
        private readonly Dictionary<AnimationSubtreeNode, AnimationCacheEntry> _cacheEntriesByRoot = new();
        private AnimationCacheEntry[] _cacheEntries = Array.Empty<AnimationCacheEntry>();

        public AnimationLayerEntry(int documentChildIndex, int drawableChildIndex, AnimationSubtreeNode rootNode)
        {
            DocumentChildIndex = documentChildIndex;
            DrawableChildIndex = drawableChildIndex;
            RootNode = rootNode;
            NodeLookup = new Dictionary<string, AnimationSubtreeNode>(StringComparer.Ordinal);
            RefreshNodeLookup();
            RefreshCacheEntries();
        }

        public int DocumentChildIndex { get; }

        public int DrawableChildIndex { get; }

        public AnimationSubtreeNode RootNode { get; private set; }

        public Dictionary<string, AnimationSubtreeNode> NodeLookup { get; }

        public IReadOnlyList<AnimationCacheEntry> CacheEntries => _cacheEntries;

        public IEnumerable<string> AnimatedTargetKeys => _animatedTargetKeys;

        public SKPicture? Model { get; private set; }

        public bool HasAnimatedTargets => _animatedTargetKeys.Count > 0;

        public void AddAnimatedTargetKey(string addressKey)
        {
            if (!string.IsNullOrWhiteSpace(addressKey))
            {
                _ = _animatedTargetKeys.Add(addressKey);
            }
        }

        public bool TryFindNode(string addressKey, out AnimationSubtreeNode node)
        {
            return NodeLookup.TryGetValue(addressKey, out node!);
        }

        public void SyncRoot(SKSvg owner, DrawableBase drawable, List<SkiaSharp.SKPicture> deferredDisposals)
        {
            var elementKey = TryGetElementAddressKey(drawable.Element);
            if (!CanReuseNode(RootNode, drawable, elementKey))
            {
                RootNode.Dispose(owner, deferredDisposals);
                RootNode = AnimationSubtreeNode.Create(parent: null, drawable);
                RootNode.MarkDirty();
            }
            else
            {
                RootNode.Sync(owner, drawable, deferredDisposals);
            }

            RefreshNodeLookup();
        }

        public void RefreshAfterPostProcess()
        {
            RefreshCacheEntries();
        }

        public bool TryResolveNode(string addressKey, out AnimationSubtreeNode node)
        {
            var current = addressKey;
            while (!string.IsNullOrWhiteSpace(current))
            {
                if (NodeLookup.TryGetValue(current, out node!))
                {
                    return true;
                }

                var separatorIndex = current.LastIndexOf('/');
                if (separatorIndex < 0)
                {
                    break;
                }

                current = current.Substring(0, separatorIndex);
            }

            node = RootNode;
            return true;
        }

        public bool TryMarkDirty(string addressKey, bool includeDescendantCacheRoots = false)
        {
            if (!TryResolveNode(addressKey, out var node))
            {
                return false;
            }

            if (ResolveCacheEntry(node) is null)
            {
                return false;
            }

            if (includeDescendantCacheRoots)
            {
                node.MarkSubtreeDirty();
            }
            else
            {
                node.MarkDirty();
            }

            return true;
        }

        public void MarkAllDirty()
        {
            if (_cacheEntries.Length == 0)
            {
                RootNode.MarkDirty();
                return;
            }

            for (var i = 0; i < _cacheEntries.Length; i++)
            {
                _cacheEntries[i].RootNode.MarkDirty();
            }
        }

        public void RebuildDirtyEntries(SKSvg owner, List<SkiaSharp.SKPicture> deferredDisposals)
        {
            if (_cacheEntries.Length == 0)
            {
                RootNode.Rebuild(owner, deferredDisposals);
                return;
            }

            for (var i = 0; i < _cacheEntries.Length; i++)
            {
                _cacheEntries[i].RootNode.Rebuild(owner, deferredDisposals);
            }
        }

        public void RebuildCompositeModel()
        {
            Model = RecordScopeCompositeModel(this);
        }

        public void Dispose(SKSvg owner)
        {
            RootNode.Dispose(owner);
            NodeLookup.Clear();
            _cacheEntries = Array.Empty<AnimationCacheEntry>();
            _cacheEntriesByRoot.Clear();
            Model = null;
        }

        private AnimationCacheEntry? ResolveCacheEntry(AnimationSubtreeNode? node)
        {
            var current = node;
            while (current is not null)
            {
                if (_cacheEntriesByRoot.TryGetValue(current, out var cacheEntry))
                {
                    return cacheEntry;
                }

                current = current.Parent;
            }

            return null;
        }

        private void RefreshNodeLookup()
        {
            NodeLookup.Clear();
            var order = 0;
            RootNode.PopulateLookup(NodeLookup, ref order);
        }

        private void RefreshCacheEntries()
        {
            var candidates = new List<AnimationSubtreeNode>(_animatedTargetKeys.Count);

            foreach (var addressKey in _animatedTargetKeys)
            {
                if (!TryResolveNode(addressKey, out var node))
                {
                    continue;
                }

                candidates.Add(SelectCacheRoot(node));
            }

            if (candidates.Count == 0)
            {
                candidates.Add(RootNode);
            }

            candidates.Sort(static (left, right) =>
            {
                var depthCompare = GetDepth(left).CompareTo(GetDepth(right));
                return depthCompare != 0 ? depthCompare : left.Order.CompareTo(right.Order);
            });

            var selectedRoots = new List<AnimationSubtreeNode>(candidates.Count);
            foreach (var candidate in candidates)
            {
                var skipCandidate = false;
                for (var i = selectedRoots.Count - 1; i >= 0; i--)
                {
                    var existing = selectedRoots[i];
                    if (ReferenceEquals(existing, candidate) || existing.Contains(candidate))
                    {
                        skipCandidate = true;
                        break;
                    }

                    if (candidate.Contains(existing))
                    {
                        selectedRoots.RemoveAt(i);
                    }
                }

                if (!skipCandidate)
                {
                    selectedRoots.Add(candidate);
                }
            }

            selectedRoots.Sort(static (left, right) => left.Order.CompareTo(right.Order));

            _cacheEntries = new AnimationCacheEntry[selectedRoots.Count];
            _cacheEntriesByRoot.Clear();

            for (var i = 0; i < selectedRoots.Count; i++)
            {
                var cacheEntry = new AnimationCacheEntry(selectedRoots[i]);
                _cacheEntries[i] = cacheEntry;
                _cacheEntriesByRoot[selectedRoots[i]] = cacheEntry;
            }
        }

        private static AnimationSubtreeNode SelectCacheRoot(AnimationSubtreeNode node)
        {
            var selected = node;
            var current = node.Parent;
            while (current is not null)
            {
                if (!IsOverlaySafeAncestor(current.Drawable))
                {
                    selected = current;
                }

                current = current.Parent;
            }

            return selected;
        }
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

        var animatedTargetKeys = AnimationController.GetAnimatedTargetAddressKeys();
        if (animatedTargetKeys.Count == 0)
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

        var candidateEntries = new List<AnimationLayerEntry>(rootDrawable.ChildrenDrawables.Count);
        for (var i = 0; i < rootDrawable.ChildrenDrawables.Count; i++)
        {
            if (!TryGetTopLevelDocumentChildIndex(SourceDocument, rootDrawable.ChildrenDrawables[i], out var documentChildIndex))
            {
                continue;
            }

            candidateEntries.Add(new AnimationLayerEntry(
                documentChildIndex,
                i,
                AnimationSubtreeNode.Create(parent: null, rootDrawable.ChildrenDrawables[i])));
        }

        if (candidateEntries.Count == 0)
        {
            DisableAnimationLayerCaching();
            return false;
        }

        for (var i = 0; i < animatedTargetKeys.Count; i++)
        {
            var addressKey = animatedTargetKeys[i];
            var matched = false;
            for (var j = 0; j < candidateEntries.Count; j++)
            {
                if (!candidateEntries[j].TryFindNode(addressKey, out _))
                {
                    continue;
                }

                candidateEntries[j].AddAnimatedTargetKey(addressKey);
                matched = true;
            }

            if (!matched)
            {
                DisableAnimationLayerCaching();
                return false;
            }
        }

        var entries = new List<AnimationLayerEntry>(candidateEntries.Count);
        var animatedDrawableIndexes = new HashSet<int>();
        for (var i = 0; i < candidateEntries.Count; i++)
        {
            var entry = candidateEntries[i];
            if (!entry.HasAnimatedTargets)
            {
                entry.Dispose(this);
                continue;
            }

            entry.RefreshAfterPostProcess();
            entries.Add(entry);
            _ = animatedDrawableIndexes.Add(entry.DrawableChildIndex);
        }

        if (entries.Count == 0)
        {
            DisableAnimationLayerCaching();
            return false;
        }

        entries.Sort(static (left, right) => left.DrawableChildIndex.CompareTo(right.DrawableChildIndex));

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

            if (_animationLayerEntries is { Length: > 0 } entries)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    entries[i].Dispose(this);
                }
            }

            SkiaModel.ClearCachedPictures();

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

    private bool TryRenderAnimationLayerFrame(
        SvgDocument animatedDocument,
        SvgAnimationFrameState frameState,
        SvgAnimationFrameState? previousState)
    {
        if (!UsesAnimationLayerCaching ||
            _animationLayerEntries is not { Length: > 0 } layerEntries ||
            _animationLayerBounds is not { } cullRect ||
            Drawable is not DrawableContainer rootDrawable)
        {
            return false;
        }

        var deferredDisposals = new List<SkiaSharp.SKPicture>();
        var references = new HashSet<Uri> { animatedDocument.BaseUri };
        var completed = false;

        try
        {
            for (var i = 0; i < layerEntries.Length; i++)
            {
                var entry = layerEntries[i];
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
                entry.SyncRoot(this, replacement, deferredDisposals);
            }

            rootDrawable.Element = animatedDocument;
            rootDrawable.PostProcess(cullRect, SKMatrix.Identity);

            var entriesByTargetKey = new Dictionary<string, List<AnimationLayerEntry>>(StringComparer.Ordinal);
            for (var i = 0; i < layerEntries.Length; i++)
            {
                var entry = layerEntries[i];
                entry.RefreshAfterPostProcess();

                foreach (var addressKey in entry.AnimatedTargetKeys)
                {
                    if (!entriesByTargetKey.TryGetValue(addressKey, out var scopedEntries))
                    {
                        scopedEntries = new List<AnimationLayerEntry>();
                        entriesByTargetKey.Add(addressKey, scopedEntries);
                    }

                    scopedEntries.Add(entry);
                }
            }

            if (previousState is null)
            {
                for (var i = 0; i < layerEntries.Length; i++)
                {
                    layerEntries[i].MarkAllDirty();
                }
            }
            else
            {
                foreach (var attribute in frameState.EnumerateDirtyAttributes(previousState))
                {
                    if (!entriesByTargetKey.TryGetValue(attribute.TargetAddress.Key, out var scopedEntries))
                    {
                        return false;
                    }

                    var includeDescendantCacheRoots = IsInheritedAnimationAttribute(attribute.AttributeName);
                    for (var i = 0; i < scopedEntries.Count; i++)
                    {
                        if (!scopedEntries[i].TryMarkDirty(attribute.TargetAddress.Key, includeDescendantCacheRoots))
                        {
                            return false;
                        }
                    }
                }

                foreach (var removedAttribute in frameState.EnumerateRemovedAttributes(previousState))
                {
                    if (!TryGetAddressKey(removedAttribute.Key, out var addressKey) ||
                        !entriesByTargetKey.TryGetValue(addressKey, out var scopedEntries))
                    {
                        return false;
                    }

                    var includeDescendantCacheRoots = IsInheritedAnimationAttribute(removedAttribute.AttributeName);
                    for (var i = 0; i < scopedEntries.Count; i++)
                    {
                        if (!scopedEntries[i].TryMarkDirty(addressKey, includeDescendantCacheRoots))
                        {
                            return false;
                        }
                    }
                }
            }

            for (var i = 0; i < layerEntries.Length; i++)
            {
                layerEntries[i].RebuildDirtyEntries(this, deferredDisposals);
                layerEntries[i].RebuildCompositeModel();
            }

            var dynamicLayerModel = RecordAnimationLayerModel(layerEntries, cullRect);
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

                DisposeDeferredPictures(deferredDisposals);
            }

            completed = true;
            return true;
        }
        finally
        {
            if (!completed)
            {
                DisposeDeferredPictures(deferredDisposals);
            }
        }
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

    private void ReplaceRegisteredPicture(
        SKPicture? oldModel,
        SkiaSharp.SKPicture? oldPicture,
        SKPicture newModel,
        SkiaSharp.SKPicture? newPicture,
        List<SkiaSharp.SKPicture> deferredDisposals)
    {
        DisposeRegisteredPicture(oldModel, oldPicture, deferredDisposals);

        if (newPicture is { })
        {
            SkiaModel.RegisterCachedPicture(newModel, newPicture);
        }
    }

    private void DisposeRegisteredPicture(
        SKPicture? model,
        SkiaSharp.SKPicture? picture,
        List<SkiaSharp.SKPicture>? deferredDisposals = null)
    {
        SkiaModel.UnregisterCachedPicture(model);

        if (picture is null)
        {
            return;
        }

        if (deferredDisposals is null)
        {
            picture.Dispose();
        }
        else
        {
            deferredDisposals.Add(picture);
        }
    }

    private static void DisposeDeferredPictures(List<SkiaSharp.SKPicture> deferredDisposals)
    {
        for (var i = 0; i < deferredDisposals.Count; i++)
        {
            deferredDisposals[i].Dispose();
        }

        deferredDisposals.Clear();
    }

    private static bool CanReuseNode(AnimationSubtreeNode node, DrawableBase drawable, string? elementKey)
    {
        return node.Drawable.GetType() == drawable.GetType() &&
               string.Equals(node.ElementKey, elementKey, StringComparison.Ordinal);
    }

    private static bool TryGetElementAddressKey(SvgElement? element, out string? key)
    {
        key = null;
        if (element is null || element is SvgDocument)
        {
            return false;
        }

        var address = SvgAnimationController.SvgElementAddress.Create(element);
        if (address.ChildIndexes.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < address.ChildIndexes.Length; i++)
        {
            if (address.ChildIndexes[i] < 0)
            {
                return false;
            }
        }

        key = address.Key;
        return true;
    }

    private static string? TryGetElementAddressKey(SvgElement? element)
    {
        _ = TryGetElementAddressKey(element, out var key);
        return key;
    }

    private static bool TryGetAddressKey(string targetAttributeKey, out string addressKey)
    {
        var separatorIndex = targetAttributeKey.IndexOf('|');
        if (separatorIndex < 0)
        {
            addressKey = targetAttributeKey;
            return !string.IsNullOrWhiteSpace(addressKey);
        }

        addressKey = targetAttributeKey.Substring(0, separatorIndex);
        return !string.IsNullOrWhiteSpace(addressKey);
    }

    private static bool TryGetTopLevelDocumentChildIndex(SvgDocument document, DrawableBase drawable, out int childIndex)
    {
        childIndex = -1;

        if (drawable.Element is not { } element || element.Parent is not SvgDocument)
        {
            return false;
        }

        childIndex = document.Children.IndexOf(element);
        return childIndex >= 0;
    }

    private static int GetDepth(AnimationSubtreeNode node)
    {
        var depth = 0;
        var current = node.Parent;
        while (current is not null)
        {
            depth++;
            current = current.Parent;
        }

        return depth;
    }

    private static bool IsInheritedAnimationAttribute(string attributeName)
    {
        return !string.IsNullOrWhiteSpace(attributeName) &&
               s_inheritedAnimationAttributes.Contains(attributeName);
    }

    private sealed class DrawableChildSlot
    {
        private readonly Action<DrawableBase> _setChild;

        public DrawableChildSlot(DrawableBase child, Action<DrawableBase> setChild)
        {
            Child = child;
            _setChild = setChild;
        }

        public DrawableBase Child { get; }

        public void Replace(DrawableBase drawable)
        {
            _setChild(drawable);
        }
    }

    private static List<DrawableChildSlot>? GetChildSlots(DrawableBase drawable)
    {
        List<DrawableChildSlot>? slots = null;

        if (drawable is DrawableContainer container)
        {
            slots = new List<DrawableChildSlot>(container.ChildrenDrawables.Count);
            for (var i = 0; i < container.ChildrenDrawables.Count; i++)
            {
                var childIndex = i;
                slots.Add(new DrawableChildSlot(
                    container.ChildrenDrawables[childIndex],
                    replacement => container.ChildrenDrawables[childIndex] = replacement));
            }
        }

        if (drawable is UseDrawable use && use.ReferencedDrawable is { } referencedDrawable)
        {
            slots ??= new List<DrawableChildSlot>(1);
            slots.Add(new DrawableChildSlot(referencedDrawable, replacement => use.ReferencedDrawable = replacement));
        }

        if (drawable is SwitchDrawable svgSwitch && svgSwitch.FirstChild is { } firstChild)
        {
            slots ??= new List<DrawableChildSlot>(1);
            slots.Add(new DrawableChildSlot(firstChild, replacement => svgSwitch.FirstChild = replacement));
        }

        if (drawable is MarkerDrawable marker && marker.MarkerElementDrawable is { } markerElementDrawable)
        {
            slots ??= new List<DrawableChildSlot>(1);
            slots.Add(new DrawableChildSlot(markerElementDrawable, replacement => marker.MarkerElementDrawable = replacement));
        }

        if (drawable is DrawablePath path && path.MarkerDrawables is { Count: > 0 } markerDrawables)
        {
            slots ??= new List<DrawableChildSlot>(markerDrawables.Count);
            for (var i = 0; i < markerDrawables.Count; i++)
            {
                var childIndex = i;
                slots.Add(new DrawableChildSlot(
                    markerDrawables[childIndex],
                    replacement => markerDrawables[childIndex] = replacement));
            }
        }

        return slots;
    }

    private static bool ContainsCutRoot(AnimationSubtreeNode node, HashSet<AnimationSubtreeNode> cutRoots)
    {
        if (cutRoots.Contains(node))
        {
            return true;
        }

        for (var i = 0; i < node.Children.Count; i++)
        {
            if (ContainsCutRoot(node.Children[i], cutRoots))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOverlaySafeAncestor(DrawableBase drawable)
    {
        return drawable.Overflow is null &&
               drawable.Clip is null &&
               drawable.ClipPath is null &&
               drawable.MaskDrawable is null &&
               drawable.Mask is null &&
               drawable.MaskDstIn is null &&
               drawable.Filter is null &&
               drawable.FilterClip is null &&
               drawable.Fill is null &&
               drawable.Stroke is null;
    }

    private static SKPicture RecordTopLevelDrawableLayer(
        DrawableContainer rootDrawable,
        SKRect cullRect,
        HashSet<int> markedIndexes,
        bool includeMarked)
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

    private static SKPicture RecordAnimationLayerModel(AnimationLayerEntry[] layerEntries, SKRect cullRect)
    {
        var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(cullRect);

        for (var i = 0; i < layerEntries.Length; i++)
        {
            if (layerEntries[i].Model is { } picture)
            {
                canvas.DrawPicture(picture);
            }
        }

        return recorder.EndRecording();
    }

    private static SKPicture RecordScopeCompositeModel(AnimationLayerEntry entry)
    {
        if (entry.CacheEntries.Count == 1 &&
            ReferenceEquals(entry.CacheEntries[0].RootNode, entry.RootNode) &&
            entry.RootNode.Model is { } rootModel)
        {
            return rootModel;
        }

        var bounds = GetDrawableRecordBounds(entry.RootNode.Drawable);
        var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(bounds);

        var cutRoots = new HashSet<AnimationSubtreeNode>();
        for (var i = 0; i < entry.CacheEntries.Count; i++)
        {
            cutRoots.Add(entry.CacheEntries[i].RootNode);
        }

        if (RecordStaticNodeModel(entry.RootNode, cutRoots) is { } staticModel)
        {
            canvas.DrawPicture(staticModel);
        }

        for (var i = 0; i < entry.CacheEntries.Count; i++)
        {
            if (RecordOverlayNodeModel(entry.RootNode, entry.CacheEntries[i].RootNode) is { } overlayModel)
            {
                canvas.DrawPicture(overlayModel);
            }
        }

        return recorder.EndRecording();
    }

    private static SKPicture? RecordStaticNodeModel(AnimationSubtreeNode node, HashSet<AnimationSubtreeNode> cutRoots)
    {
        if (cutRoots.Contains(node))
        {
            return null;
        }

        if (!ContainsCutRoot(node, cutRoots))
        {
            return node.Model;
        }

        var bounds = GetDrawableRecordBounds(node.Drawable);
        var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(bounds);

        var childSlots = GetChildSlots(node.Drawable);
        if (childSlots is { Count: > 0 } && node.Children.Count == childSlots.Count)
        {
            var originalChildren = new DrawableBase[childSlots.Count];
            for (var i = 0; i < childSlots.Count; i++)
            {
                originalChildren[i] = childSlots[i].Child;

                var childModel = RecordStaticNodeModel(node.Children[i], cutRoots);
                childSlots[i].Replace(childModel is { }
                    ? new CachedPictureDrawable(originalChildren[i], childModel)
                    : new SkippedDrawable(originalChildren[i]));
            }

            try
            {
                node.Drawable.Draw(canvas, node.Drawable.IgnoreAttributes, null, true);
            }
            finally
            {
                for (var i = 0; i < originalChildren.Length; i++)
                {
                    childSlots[i].Replace(originalChildren[i]);
                }
            }
        }
        else
        {
            node.Drawable.Draw(canvas, node.Drawable.IgnoreAttributes, null, true);
        }

        return recorder.EndRecording();
    }

    private static SKPicture? RecordOverlayNodeModel(AnimationSubtreeNode current, AnimationSubtreeNode target)
    {
        if (ReferenceEquals(current, target))
        {
            return current.Model;
        }

        if (!current.Contains(target))
        {
            return null;
        }

        var childSlots = GetChildSlots(current.Drawable);
        if (childSlots is not { Count: > 0 } || current.Children.Count != childSlots.Count)
        {
            return current.Model;
        }

        var pathChildIndex = -1;
        for (var i = 0; i < current.Children.Count; i++)
        {
            if (current.Children[i].Contains(target))
            {
                pathChildIndex = i;
                break;
            }
        }

        if (pathChildIndex < 0)
        {
            return null;
        }

        var childModel = RecordOverlayNodeModel(current.Children[pathChildIndex], target);
        if (childModel is null)
        {
            return null;
        }

        var bounds = GetDrawableRecordBounds(current.Drawable);
        var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(bounds);

        var originalChildren = new DrawableBase[childSlots.Count];
        for (var i = 0; i < childSlots.Count; i++)
        {
            originalChildren[i] = childSlots[i].Child;
            childSlots[i].Replace(i == pathChildIndex
                ? new CachedPictureDrawable(originalChildren[i], childModel)
                : new SkippedDrawable(originalChildren[i]));
        }

        try
        {
            current.Drawable.Draw(canvas, current.Drawable.IgnoreAttributes, null, true);
        }
        finally
        {
            for (var i = 0; i < originalChildren.Length; i++)
            {
                childSlots[i].Replace(originalChildren[i]);
            }
        }

        return recorder.EndRecording();
    }

    private static SKPicture ComposeAnimationLayerModel(SKPicture? staticLayerModel, SKPicture? dynamicLayerModel, SKRect cullRect)
    {
        var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(cullRect);

        if (staticLayerModel is { })
        {
            canvas.DrawPicture(staticLayerModel);
        }

        if (dynamicLayerModel is { })
        {
            canvas.DrawPicture(dynamicLayerModel);
        }

        return recorder.EndRecording();
    }

    private static SKPicture RecordNodeModel(AnimationSubtreeNode node)
    {
        var bounds = GetDrawableRecordBounds(node.Drawable);
        var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(bounds);

        var childSlots = GetChildSlots(node.Drawable);
        if (childSlots is { Count: > 0 } && node.Children.Count == childSlots.Count)
        {
            var originalChildren = new DrawableBase[childSlots.Count];
            for (var i = 0; i < childSlots.Count; i++)
            {
                originalChildren[i] = childSlots[i].Child;
                if (node.Children[i].Model is { } childModel)
                {
                    childSlots[i].Replace(new CachedPictureDrawable(originalChildren[i], childModel));
                }
            }

            try
            {
                node.Drawable.Draw(canvas, node.Drawable.IgnoreAttributes, null, true);
            }
            finally
            {
                for (var i = 0; i < originalChildren.Length; i++)
                {
                    childSlots[i].Replace(originalChildren[i]);
                }
            }
        }
        else
        {
            node.Drawable.Draw(canvas, node.Drawable.IgnoreAttributes, null, true);
        }

        return recorder.EndRecording();
    }

    private static SKRect GetDrawableRecordBounds(DrawableBase drawable)
    {
        var worldBounds = GetDrawableWorldBounds(drawable);
        var parentTransform = drawable.Parent?.TotalTransform ?? SKMatrix.Identity;
        if (!worldBounds.IsEmpty && !parentTransform.IsIdentity && parentTransform.TryInvert(out var inverse))
        {
            worldBounds = inverse.MapRect(worldBounds);
        }

        if (!worldBounds.IsEmpty)
        {
            return worldBounds;
        }

        if (!drawable.GeometryBounds.IsEmpty)
        {
            return drawable.GeometryBounds;
        }

        return SKRect.Create(0f, 0f, 1f, 1f);
    }

    private static SKRect GetDrawableWorldBounds(DrawableBase drawable)
    {
        var bounds = drawable.TransformedBounds;

        if (drawable.FilterClip is { } filterClip)
        {
            bounds = bounds.IsEmpty ? filterClip : SKRect.Union(bounds, filterClip);
        }

        var childSlots = GetChildSlots(drawable);
        if (childSlots is { Count: > 0 })
        {
            for (var i = 0; i < childSlots.Count; i++)
            {
                var childBounds = GetDrawableWorldBounds(childSlots[i].Child);
                if (childBounds.IsEmpty)
                {
                    continue;
                }

                bounds = bounds.IsEmpty ? childBounds : SKRect.Union(bounds, childBounds);
            }
        }

        return bounds;
    }
}
