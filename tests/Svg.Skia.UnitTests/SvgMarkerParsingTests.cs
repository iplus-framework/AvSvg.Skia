using System.IO;
using Svg;
using Xunit;

namespace Svg.Skia.UnitTests;

public class SvgMarkerParsingTests
{
    [Fact]
    public void PaintingMarker05_ShorthandMarkerStyleIsParsed()
    {
        var path = Path.Combine("..", "..", "..", "..", "..", "externals", "W3C_SVG_11_TestSuite", "W3C_SVG_11_TestSuite", "svg", "painting-marker-05-f.svg");
        var document = SvgDocument.Open<SvgDocument>(path);
        var markerPath = document.GetElementById<SvgPath>("p1");

        Assert.NotNull(markerPath);
        Assert.Equal("url(\"#marker1\")", markerPath!.Marker?.ToString());
        Assert.Equal("url(\"#marker1\")", markerPath.MarkerStart?.ToString());
        Assert.Equal("url(\"#marker1\")", markerPath.MarkerMid?.ToString());
        Assert.Equal("url(\"#marker1\")", markerPath.MarkerEnd?.ToString());
    }
}
