using Svg;
using Svg.Model.Drawables;
using Svg.Skia;

namespace Svg.Editor.Skia;

public readonly struct SelectionVisualInfo
{
    public SelectionVisualInfo(SvgVisualElement element, SvgSceneNode? sceneNode, DrawableBase? drawable)
    {
        Element = element;
        SceneNode = sceneNode;
        Drawable = drawable;
    }

    public SvgVisualElement Element { get; }

    public SvgSceneNode? SceneNode { get; }

    public DrawableBase? Drawable { get; }
}
