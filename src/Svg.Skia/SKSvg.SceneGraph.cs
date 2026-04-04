using System;
using System.Collections.Generic;
using System.ComponentModel;
using ShimSkiaSharp;
using Svg;
using Svg.Model.Drawables;
using Svg.Model.Services;

namespace Svg.Skia;

public partial class SKSvg
{
    private SvgSceneDocument? _retainedSceneGraph;
    private bool _retainedSceneGraphDirty = true;

    public SvgSceneDocument? RetainedSceneGraph
    {
        get
        {
            _ = TryEnsureRetainedSceneGraph(out var sceneDocument);
            return sceneDocument;
        }
    }

    public bool HasRetainedSceneGraph => RetainedSceneGraph is not null;

    public bool TryEnsureRetainedSceneGraph(out SvgSceneDocument? sceneDocument)
    {
        SvgDocument? sourceDocument;
        SKRect cullRect;

        lock (Sync)
        {
            if (!_retainedSceneGraphDirty)
            {
                sceneDocument = _retainedSceneGraph;
                return sceneDocument is not null;
            }

            sourceDocument = _animatedDocument ?? SourceDocument;
            cullRect = Model?.CullRect ?? SKRect.Empty;
            if (cullRect.IsEmpty && sourceDocument is { })
            {
                cullRect = SKRect.Create(SvgService.GetDimensions(sourceDocument));
            }
        }

        if (sourceDocument is null || cullRect.IsEmpty)
        {
            lock (Sync)
            {
                _retainedSceneGraph = null;
                _retainedSceneGraphDirty = false;
                sceneDocument = null;
            }

            return false;
        }

        if (!SvgSceneCompiler.TryCompile(sourceDocument, cullRect, AssetLoader, IgnoreAttributes, out var compiledSceneDocument))
        {
            lock (Sync)
            {
                _retainedSceneGraph = null;
                _retainedSceneGraphDirty = false;
                sceneDocument = null;
            }

            return false;
        }

        lock (Sync)
        {
            _retainedSceneGraph = compiledSceneDocument;
            _retainedSceneGraphDirty = false;
            sceneDocument = compiledSceneDocument;
        }

        return true;
    }

    public SKPicture? CreateRetainedSceneGraphModel()
    {
        return RetainedSceneGraph is { } sceneDocument
            ? sceneDocument.CreateModel()
            : null;
    }

    public SkiaSharp.SKPicture? CreateRetainedSceneGraphPicture()
    {
        var model = CreateRetainedSceneGraphModel();
        return model is null ? null : SkiaModel.ToSKPicture(model);
    }

    public bool TryGetRetainedSceneNode(string addressKey, out SvgSceneNode? node)
    {
        if (TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null)
        {
            return sceneDocument.TryGetNode(addressKey, out node);
        }

        node = null;
        return false;
    }

    public bool TryGetRetainedSceneNode(SvgElement element, out SvgSceneNode? node)
    {
        if (element is null)
        {
            throw new System.ArgumentNullException(nameof(element));
        }

        return TryGetRetainedSceneNode(SvgSceneCompiler.TryGetElementAddressKey(element) ?? string.Empty, out node);
    }

    public bool TryGetRetainedSceneNodes(string addressKey, out IReadOnlyList<SvgSceneNode> nodes)
    {
        if (TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null)
        {
            return sceneDocument.TryGetNodes(addressKey, out nodes);
        }

        nodes = System.Array.Empty<SvgSceneNode>();
        return false;
    }

    public bool TryGetRetainedSceneNodes(SvgElement element, out IReadOnlyList<SvgSceneNode> nodes)
    {
        if (element is null)
        {
            throw new System.ArgumentNullException(nameof(element));
        }

        return TryGetRetainedSceneNodes(SvgSceneCompiler.TryGetElementAddressKey(element) ?? string.Empty, out nodes);
    }

    public bool TryGetRetainedSceneNodeById(string id, out SvgSceneNode? node)
    {
        if (TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null)
        {
            return sceneDocument.TryGetNodeById(id, out node);
        }

        node = null;
        return false;
    }

    public bool TryGetRetainedSceneResource(string addressKey, out SvgSceneResource? resource)
    {
        if (TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null)
        {
            return sceneDocument.TryGetResource(addressKey, out resource);
        }

        resource = null;
        return false;
    }

    public bool TryGetRetainedSceneResourceById(string id, out SvgSceneResource? resource)
    {
        if (TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null)
        {
            return sceneDocument.TryGetResourceById(id, out resource);
        }

        resource = null;
        return false;
    }

    public SvgSceneMutationResult ApplyRetainedSceneMutation(SvgElement element, IReadOnlyCollection<string>? changedAttributes = null)
    {
        return TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null
            ? sceneDocument.ApplyMutation(element, changedAttributes)
            : new SvgSceneMutationResult(false, 0, 0);
    }

    public SvgSceneMutationResult ApplyRetainedSceneMutation(string addressKey, IReadOnlyCollection<string>? changedAttributes = null)
    {
        return TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null
            ? sceneDocument.ApplyMutation(addressKey, changedAttributes)
            : new SvgSceneMutationResult(false, 0, 0);
    }

    public SvgSceneMutationResult ApplyRetainedSceneMutationById(string id, IReadOnlyCollection<string>? changedAttributes = null)
    {
        return TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null
            ? sceneDocument.ApplyMutationById(id, changedAttributes)
            : new SvgSceneMutationResult(false, 0, 0);
    }

    public SKPicture? CreateRetainedSceneNodeModel(SvgSceneNode node, SKRect? clip = null)
    {
        if (!TryEnsureRetainedSceneGraph(out var sceneDocument) || sceneDocument is null)
        {
            return null;
        }

        return sceneDocument.CreateNodeModel(node, clip);
    }

    public SkiaSharp.SKPicture? CreateRetainedSceneNodePicture(SvgSceneNode node, SKRect? clip = null)
    {
        var model = CreateRetainedSceneNodeModel(node, clip);
        return model is null ? null : SkiaModel.ToSKPicture(model);
    }

    public SKPicture? CreateRetainedSceneModel(SvgElement element, SKRect? clip = null)
    {
        if (element is null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        return TryGetRetainedSceneNode(element, out var node) && node is not null
            ? CreateRetainedSceneNodeModel(node, clip)
            : null;
    }

    public SkiaSharp.SKPicture? CreateRetainedScenePicture(SvgElement element, SKRect? clip = null)
    {
        var model = CreateRetainedSceneModel(element, clip);
        return model is null ? null : SkiaModel.ToSKPicture(model);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use CreateRetainedSceneNodeModel or CreateRetainedSceneNodePicture to work directly with retained scene state.")]
    public DrawableBase? CreateRetainedSceneNodeDrawable(SvgSceneNode node)
    {
        if (node is null)
        {
            throw new System.ArgumentNullException(nameof(node));
        }

        return TryEnsureRetainedSceneGraph(out var sceneDocument) && sceneDocument is not null
            ? new SvgSceneDrawableProxy(sceneDocument, node)
            : null;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use CreateRetainedSceneModel or CreateRetainedScenePicture to work directly with retained scene state.")]
    public DrawableBase? CreateRetainedSceneDrawable(SvgElement element)
    {
        if (element is null)
        {
            throw new System.ArgumentNullException(nameof(element));
        }

        return TryGetRetainedSceneNode(element, out var node) && node is not null
            ? CreateRetainedSceneNodeDrawable(node)
            : null;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use HitTestSceneNodes instead.")]
    public IEnumerable<SvgSceneNode> HitTestRetainedSceneNodes(SKPoint point)
    {
        foreach (var node in HitTestSceneNodes(point))
        {
            yield return node;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use HitTestSceneNodes instead.")]
    public IEnumerable<SvgSceneNode> HitTestRetainedSceneNodes(SKRect rect)
    {
        foreach (var node in HitTestSceneNodes(rect))
        {
            yield return node;
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use HitTestTopmostSceneNode instead.")]
    public SvgSceneNode? HitTestTopmostRetainedSceneNode(SKPoint point)
    {
        return HitTestTopmostSceneNode(point);
    }

    private void InvalidateRetainedSceneGraph()
    {
        lock (Sync)
        {
            _retainedSceneGraphDirty = true;
            _retainedSceneGraph = null;
        }
    }

    private bool TryPrepareRetainedSceneGraphForAnimationFrame(SvgAnimationFrameState frameState, SvgAnimationFrameState? previousFrameState, out SvgSceneDocument? sceneDocument)
    {
        SvgDocument? currentDocument;

        lock (Sync)
        {
            currentDocument = _animatedDocument ?? SourceDocument;
        }

        sceneDocument = null;
        if (currentDocument is null)
        {
            return false;
        }

        if (!TryEnsureRetainedSceneGraph(out sceneDocument) || sceneDocument is null)
        {
            return false;
        }

        if (!ReferenceEquals(sceneDocument.SourceDocument, currentDocument))
        {
            return TryRebuildRetainedSceneGraphForCurrentDocument(currentDocument, out sceneDocument);
        }

        foreach (var dirtyAttribute in frameState.EnumerateDirtyAttributes(previousFrameState))
        {
            if (!sceneDocument.TryResolveElement(dirtyAttribute.TargetAddress.Key, out var targetElement) || targetElement is null)
            {
                return TryRebuildRetainedSceneGraphForCurrentDocument(currentDocument, out sceneDocument);
            }

            var result = sceneDocument.ApplyMutation(targetElement, new[] { dirtyAttribute.AttributeName });
            if (!result.Succeeded)
            {
                return TryRebuildRetainedSceneGraphForCurrentDocument(currentDocument, out sceneDocument);
            }
        }

        foreach (var removedAttribute in frameState.EnumerateRemovedAttributes(previousFrameState))
        {
            if (!sceneDocument.TryResolveElement(removedAttribute.TargetAddress.Key, out var targetElement) || targetElement is null)
            {
                return TryRebuildRetainedSceneGraphForCurrentDocument(currentDocument, out sceneDocument);
            }

            var result = sceneDocument.ApplyMutation(targetElement, new[] { removedAttribute.AttributeName });
            if (!result.Succeeded)
            {
                return TryRebuildRetainedSceneGraphForCurrentDocument(currentDocument, out sceneDocument);
            }
        }

        return true;
    }

    private bool TryRebuildRetainedSceneGraphForCurrentDocument(SvgDocument currentDocument, out SvgSceneDocument? sceneDocument)
    {
        InvalidateRetainedSceneGraph();
        if (!TryEnsureRetainedSceneGraph(out sceneDocument) || sceneDocument is null)
        {
            return false;
        }

        if (!ReferenceEquals(sceneDocument.SourceDocument, currentDocument))
        {
            InvalidateRetainedSceneGraph();
            sceneDocument = null;
            return false;
        }

        return true;
    }
}
