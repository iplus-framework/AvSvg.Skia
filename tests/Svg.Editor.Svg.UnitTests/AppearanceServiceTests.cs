using System.Linq;
using Svg;
using Svg.Editor.Svg;
using Svg.Editor.Svg.Models;
using Xunit;

namespace Svg.Editor.Svg.UnitTests;

public class AppearanceServiceTests
{
    [Fact]
    public void AddLoadAndRemoveStyle_RoundTripsCustomAttributes()
    {
        var document = new SvgDocument();
        var service = new AppearanceService();
        var style = new StyleEntry
        {
            Name = "button",
            Fill = "#ff0000",
            Stroke = "#000000",
            Opacity = 0.5f
        };

        service.AddOrUpdateStyle(document, style);
        service.Load(document);

        var loaded = Assert.Single(service.Styles);
        Assert.Equal("button", loaded.Name);
        Assert.Equal("#ff0000", loaded.Fill);
        Assert.Equal("#000000", loaded.Stroke);
        Assert.Equal(0.5f, loaded.Opacity);

        service.RemoveStyle(document, loaded);

        Assert.Empty(service.Styles);
        Assert.DoesNotContain(document.CustomAttributes, pair => pair.Key.Contains("button"));
    }
}
