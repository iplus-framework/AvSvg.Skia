using Svg.Model.Services;
using Xunit;

namespace Svg.Model.UnitTests;

public class PaintingServiceTests
{
    [Theory]
    [InlineData(SvgFontWeight.W100, SvgFontWeight.Bolder, SvgFontWeight.Normal)]
    [InlineData(SvgFontWeight.W500, SvgFontWeight.Bolder, SvgFontWeight.Bold)]
    [InlineData(SvgFontWeight.W800, SvgFontWeight.Bolder, SvgFontWeight.W900)]
    [InlineData(SvgFontWeight.W900, SvgFontWeight.Bolder, SvgFontWeight.W900)]
    [InlineData(SvgFontWeight.W100, SvgFontWeight.Lighter, SvgFontWeight.W100)]
    [InlineData(SvgFontWeight.W300, SvgFontWeight.Lighter, SvgFontWeight.W100)]
    [InlineData(SvgFontWeight.W400, SvgFontWeight.Lighter, SvgFontWeight.W100)]
    [InlineData(SvgFontWeight.W600, SvgFontWeight.Lighter, SvgFontWeight.Normal)]
    [InlineData(SvgFontWeight.W800, SvgFontWeight.Lighter, SvgFontWeight.Bold)]
    [InlineData(SvgFontWeight.W900, SvgFontWeight.Lighter, SvgFontWeight.Bold)]
    public void ResolveFontWeight_UsesBrowserRelativeWeightTable(
        SvgFontWeight parentWeight,
        SvgFontWeight requestedWeight,
        SvgFontWeight expectedWeight)
    {
        var document = SvgDocument.FromSvg<SvgDocument>($$"""
            <svg xmlns="http://www.w3.org/2000/svg">
              <text id="parent" font-weight="{{parentWeight}}">
                <tspan id="child" font-weight="{{requestedWeight}}">Text</tspan>
              </text>
            </svg>
            """);

        var child = Assert.IsType<SvgTextSpan>(document.GetElementById("child"));

        Assert.Equal(expectedWeight, PaintingService.ResolveFontWeight(child, child.FontWeight));
    }

    [Fact]
    public void ResolveFontWeight_Inherit_UsesComputedParentWeight()
    {
        var parent = new SvgTextSpan
        {
            FontWeight = SvgFontWeight.Bold
        };
        var child = new SvgTextSpan
        {
            FontWeight = SvgFontWeight.Inherit
        };

        parent.Children.Add(child);

        Assert.Equal(SvgFontWeight.W700, PaintingService.ResolveFontWeight(child, SvgFontWeight.Inherit));
    }
}
