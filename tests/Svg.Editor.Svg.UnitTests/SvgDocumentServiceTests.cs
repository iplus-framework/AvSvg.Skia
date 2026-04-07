using System;
using System.IO;
using Svg;
using Svg.Editor.Svg;
using Xunit;

namespace Svg.Editor.Svg.UnitTests;

public class SvgDocumentServiceTests
{
    [Fact]
    public void FromSvg_GetXml_Save_RoundTripsDocument()
    {
        const string svg = "<svg width=\"10\" height=\"20\"><rect id=\"r1\" x=\"1\" y=\"2\" width=\"3\" height=\"4\" fill=\"red\" /></svg>";
        var service = new SvgDocumentService();

        var document = service.FromSvg(svg);

        Assert.NotNull(document);
        Assert.Equal(10, document!.Width.Value);
        Assert.Equal(20, document.Height.Value);

        var xml = service.GetXml(document);
        Assert.Contains("rect", xml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("r1", xml, StringComparison.Ordinal);

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.svg");
        try
        {
            service.Save(document, path);
            var reloaded = service.Open(path);

            Assert.NotNull(reloaded);
            Assert.Equal(document.Width.Value, reloaded!.Width.Value);
            Assert.Equal(document.Height.Value, reloaded.Height.Value);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
