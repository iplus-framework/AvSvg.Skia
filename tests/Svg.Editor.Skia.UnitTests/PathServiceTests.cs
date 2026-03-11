using System.Collections.Generic;
using System.Linq;
using Svg;
using Svg.Editor.Skia;
using Svg.Pathing;
using Xunit;
using Shim = ShimSkiaSharp;

namespace Svg.Editor.Skia.UnitTests;

public class PathServiceTests
{
    [Fact]
    public void MakeSmooth_CreatesMoveAndLineSegments()
    {
        var segments = PathService.MakeSmooth(new List<Shim.SKPoint>
        {
            new(0, 0),
            new(5, 5),
            new(10, 0)
        });

        Assert.NotNull(segments);
        Assert.IsType<SvgMoveToSegment>(segments[0]);
        Assert.True(segments.Count >= 2);
    }

    [Fact]
    public void ElementToPath_ConvertsRectangleGeometry()
    {
        var rectangle = new SvgRectangle
        {
            X = 1,
            Y = 2,
            Width = 10,
            Height = 20
        };

        var path = PathService.ElementToPath(rectangle);

        Assert.NotNull(path);
        var svgPath = PathService.ToSvgPathData(path!);
        Assert.Contains("M", svgPath);
        Assert.Contains("L", svgPath);
    }
}
