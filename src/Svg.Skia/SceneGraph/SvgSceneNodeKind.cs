using Svg;
using Svg.Model.Drawables;
using Svg.Model.Drawables.Elements;

namespace Svg.Skia;

public enum SvgSceneNodeKind
{
    Unknown,
    Fragment,
    Group,
    Anchor,
    Use,
    Switch,
    Image,
    Text,
    Marker,
    Path,
    Shape,
    Mask,
    Container
}

internal static class SvgSceneNodeKindExtensions
{
    public static SvgSceneNodeKind FromDrawable(DrawableBase drawable)
    {
        return drawable switch
        {
            FragmentDrawable => SvgSceneNodeKind.Fragment,
            GroupDrawable => SvgSceneNodeKind.Group,
            AnchorDrawable => SvgSceneNodeKind.Anchor,
            UseDrawable => SvgSceneNodeKind.Use,
            SwitchDrawable => SvgSceneNodeKind.Switch,
            ImageDrawable => SvgSceneNodeKind.Image,
            TextDrawable => SvgSceneNodeKind.Text,
            MarkerDrawable => SvgSceneNodeKind.Marker,
            DrawablePath path => path.Path is { } ? SvgSceneNodeKind.Path : SvgSceneNodeKind.Shape,
            DrawableContainer => SvgSceneNodeKind.Container,
            _ => SvgSceneNodeKind.Unknown
        };
    }

    public static SvgSceneNodeKind FromElement(SvgElement element)
    {
        return element switch
        {
            SvgFragment => SvgSceneNodeKind.Fragment,
            SvgGroup => SvgSceneNodeKind.Group,
            SvgAnchor => SvgSceneNodeKind.Anchor,
            SvgUse => SvgSceneNodeKind.Use,
            SvgSwitch => SvgSceneNodeKind.Switch,
            SvgImage => SvgSceneNodeKind.Image,
            SvgTextBase => SvgSceneNodeKind.Text,
            SvgMarker => SvgSceneNodeKind.Marker,
            SvgPath => SvgSceneNodeKind.Path,
            SvgCircle or SvgEllipse or SvgRectangle or SvgLine or SvgPolyline or SvgPolygon => SvgSceneNodeKind.Shape,
            SvgMask => SvgSceneNodeKind.Mask,
            _ => SvgSceneNodeKind.Unknown
        };
    }
}
