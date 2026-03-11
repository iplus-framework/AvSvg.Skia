using System.Linq;
using Svg;
using Svg.Editor.Svg;
using Svg.Editor.Svg.Models;
using Xunit;

namespace Svg.Editor.Svg.UnitTests;

public class LayerServiceTests
{
    [Fact]
    public void AddMoveAndRemoveLayer_UpdatesDocumentAndEntries()
    {
        var document = new SvgDocument();
        var service = new LayerService();

        var first = service.AddLayer(document, "Layer A");
        var second = service.AddLayer(document, "Layer B");

        Assert.Equal(2, document.Children.Count);
        Assert.Equal(new[] { "Layer A", "Layer B" }, service.Layers.Select(layer => layer.Name).ToArray());

        service.MoveUp(second, document);
        Assert.Equal(new[] { "Layer B", "Layer A" }, service.Layers.Select(layer => layer.Name).ToArray());

        second.Locked = true;
        first.Visible = false;

        Assert.True(second.Locked);
        Assert.False(first.Visible);
        Assert.Equal("hidden", first.Group.Visibility);

        service.RemoveLayer(second, document);

        Assert.Single(service.Layers);
        Assert.Single(document.Children.OfType<SvgGroup>());
        Assert.Equal("Layer A", service.Layers[0].Name);
    }
}
