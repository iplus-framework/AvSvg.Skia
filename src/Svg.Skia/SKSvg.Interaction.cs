using ShimSkiaSharp;
using Svg.Model.Drawables;
using Svg.Model.Services;

namespace Svg.Skia;

public partial class SKSvg
{
    public DrawableBase? HitTestTopmostDrawable(SKPoint point)
    {
        return Drawable is DrawableBase drawable
            ? SvgInteractionHitTest.HitTestTopmostDrawable(drawable, point)
            : null;
    }

    public SvgElement? HitTestTopmostElement(SKPoint point)
    {
        return HitTestTopmostDrawable(point)?.Element;
    }

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

internal static class SvgInteractionHitTest
{
    public static DrawableBase? HitTestTopmostDrawable(DrawableBase drawable, SKPoint point)
    {
        if (drawable is DrawableContainer container)
        {
            for (var index = container.ChildrenDrawables.Count - 1; index >= 0; index--)
            {
                var child = container.ChildrenDrawables[index];
                var childHit = HitTestTopmostDrawable(child, point);
                if (childHit is not null)
                {
                    return childHit;
                }
            }

            return null;
        }

        return HitTestService.HitTestPointer(drawable, point)
            ? drawable
            : null;
    }
}
