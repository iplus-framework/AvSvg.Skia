using Svg.Pathing;
using Shim = ShimSkiaSharp;

namespace Svg.Editor.Skia;

public struct PathPoint
{
    public SvgPathSegment Segment;
    public int Type;
    public Shim.SKPoint Point;
}
