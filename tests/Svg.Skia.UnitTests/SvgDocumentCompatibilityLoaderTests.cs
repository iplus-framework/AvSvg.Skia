using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;
using Xunit;

namespace Svg.Skia.UnitTests;

public class SvgDocumentCompatibilityLoaderTests
{
    [Fact]
    public void OpenXmlReader_MatchesStringOverload_WhenDtdProcessingIsEnabled()
    {
        const string svg = """
            <!DOCTYPE svg [
              <!ENTITY greet "HELLO">
            ]>
            <svg xmlns="http://www.w3.org/2000/svg">
              <text id="target">&greet;</text>
            </svg>
            """;

        var originalDisableDtdProcessing = SvgDocument.DisableDtdProcessing;
        try
        {
            SvgDocument.DisableDtdProcessing = false;

            var expected = CaptureLoad(() => SvgDocumentCompatibilityLoader.FromSvg<SvgDocument>(svg));
            var actual = CaptureLoad(() =>
            {
                using var stringReader = new StringReader(svg);
                using var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Parse
                });
                return SvgDocumentCompatibilityLoader.Open<SvgDocument>(xmlReader);
            });

            Assert.Equal(expected.Succeeded, actual.Succeeded);
            Assert.Equal(expected.ExceptionType, actual.ExceptionType);
            Assert.Equal(expected.Text, actual.Text);
        }
        finally
        {
            SvgDocument.DisableDtdProcessing = originalDisableDtdProcessing;
        }
    }

    [Fact]
    public void OpenXmlReader_RejectsParseEnabledReaders_WhenDtdProcessingIsDisabled()
    {
        const string svg = """
            <!DOCTYPE svg [
              <!ENTITY greet "HELLO">
            ]>
            <svg xmlns="http://www.w3.org/2000/svg">
              <text id="target">&greet;</text>
            </svg>
            """;

        var originalDisableDtdProcessing = SvgDocument.DisableDtdProcessing;
        try
        {
            SvgDocument.DisableDtdProcessing = true;

            var stringLoad = CaptureLoad(() => SvgDocumentCompatibilityLoader.FromSvg<SvgDocument>(svg));
            var xmlReaderLoad = CaptureLoad(() =>
            {
                using var stringReader = new StringReader(svg);
                using var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Parse
                });
                return SvgDocumentCompatibilityLoader.Open<SvgDocument>(xmlReader);
            });

            Assert.False(stringLoad.Succeeded);
            Assert.False(xmlReaderLoad.Succeeded);
            Assert.Equal(typeof(InvalidOperationException).FullName, xmlReaderLoad.ExceptionType);
        }
        finally
        {
            SvgDocument.DisableDtdProcessing = originalDisableDtdProcessing;
        }
    }

    [Fact]
    public void OpenPath_AppliesImportedStylesheets()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var cssPath = Path.Combine(tempDirectory, "styles.css");
            var svgPath = Path.Combine(tempDirectory, "test.svg");

            File.WriteAllText(cssPath, "#target { fill: green; }");
            File.WriteAllText(svgPath, """
                <svg xmlns="http://www.w3.org/2000/svg">
                  <style type="text/css"><![CDATA[
                    @import url("styles.css");
                  ]]></style>
                  <circle id="target" cx="10" cy="10" r="5" fill="red" />
                </svg>
                """);

            var document = SvgDocumentCompatibilityLoader.Open<SvgDocument>(svgPath, new SvgOptions());
            var circle = document.Descendants().OfType<SvgCircle>().Single(static element => element.ID == "target");
            var fill = Assert.IsType<SvgColourServer>(circle.Fill);

            Assert.Equal(Color.Green.ToArgb(), fill.Colour.ToArgb());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void OpenPath_ReappliesImportedStylesheetsAcrossSeparateStyleBlocksInSourceOrder()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var cssPath = Path.Combine(tempDirectory, "styles.css");
            var svgPath = Path.Combine(tempDirectory, "test.svg");

            File.WriteAllText(cssPath, "#target { fill: green; }");
            File.WriteAllText(svgPath, """
                <svg xmlns="http://www.w3.org/2000/svg">
                  <style type="text/css"><![CDATA[
                    @import url("styles.css");
                    #target { fill: blue; }
                  ]]></style>
                  <style type="text/css"><![CDATA[
                    @import url("styles.css");
                  ]]></style>
                  <circle id="target" cx="10" cy="10" r="5" fill="red" />
                </svg>
                """);

            var document = SvgDocumentCompatibilityLoader.Open<SvgDocument>(svgPath, new SvgOptions());
            var circle = document.Descendants().OfType<SvgCircle>().Single(static element => element.ID == "target");
            var fill = Assert.IsType<SvgColourServer>(circle.Fill);

            Assert.Equal(Color.Green.ToArgb(), fill.Colour.ToArgb());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void OpenXmlReader_AppliesImportedStylesheetsUsingReaderBaseUri()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var cssPath = Path.Combine(tempDirectory, "styles.css");
            var svgPath = Path.Combine(tempDirectory, "test.svg");

            File.WriteAllText(cssPath, "#target { fill: green; }");
            File.WriteAllText(svgPath, """
                <svg xmlns="http://www.w3.org/2000/svg">
                  <style type="text/css"><![CDATA[
                    @import url("styles.css");
                  ]]></style>
                  <circle id="target" cx="10" cy="10" r="5" fill="red" />
                </svg>
                """);

            using var xmlReader = XmlReader.Create(svgPath, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit
            });

            var document = SvgDocumentCompatibilityLoader.Open<SvgDocument>(xmlReader);
            var circle = document.Descendants().OfType<SvgCircle>().Single(static element => element.ID == "target");
            var fill = Assert.IsType<SvgColourServer>(circle.Fill);

            Assert.Equal(Color.Green.ToArgb(), fill.Colour.ToArgb());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void OpenPath_IgnoresMalformedImportedStylesheetsWithoutSemicolon()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var cssPath = Path.Combine(tempDirectory, "styles.css");
            var svgPath = Path.Combine(tempDirectory, "test.svg");

            File.WriteAllText(cssPath, "#target { fill: green; }");
            File.WriteAllText(svgPath, """
                <svg xmlns="http://www.w3.org/2000/svg">
                  <style><![CDATA[
                    @import "styles.css"
                  ]]></style>
                  <circle id="target" cx="10" cy="10" r="5" fill="red" />
                </svg>
                """);

            var document = SvgDocumentCompatibilityLoader.Open<SvgDocument>(svgPath, new SvgOptions());
            var circle = document.Descendants().OfType<SvgCircle>().Single(static element => element.ID == "target");
            var fill = Assert.IsType<SvgColourServer>(circle.Fill);

            Assert.Equal(Color.Red.ToArgb(), fill.Colour.ToArgb());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void FromSvg_IgnoresUnsupportedStyleElementTypes()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg">
              <rect id="default-style" width="10" height="10" fill="red" />
              <rect id="unsupported-style" y="20" width="10" height="10" fill="green" />
              <style>#default-style { fill: green; }</style>
              <style type="text/some-unknown-styling-language">#unsupported-style { fill: red; }</style>
            </svg>
            """;

        var document = SvgDocumentCompatibilityLoader.FromSvg<SvgDocument>(svg);
        var defaultRect = document.Descendants().OfType<SvgRectangle>().Single(static element => element.ID == "default-style");
        var unsupportedRect = document.Descendants().OfType<SvgRectangle>().Single(static element => element.ID == "unsupported-style");

        var defaultFill = Assert.IsType<SvgColourServer>(defaultRect.Fill);
        var unsupportedFill = Assert.IsType<SvgColourServer>(unsupportedRect.Fill);

        Assert.Equal(Color.Green.ToArgb(), defaultFill.Colour.ToArgb());
        Assert.Equal(Color.Green.ToArgb(), unsupportedFill.Colour.ToArgb());
    }

    [Fact]
    public void FromSvg_AppliesCssStyleTypeWithParameters()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg">
              <rect id="target" width="10" height="10" fill="red" />
              <style type="text/css; charset=utf-8">#target { fill: green; }</style>
            </svg>
            """;

        var document = SvgDocumentCompatibilityLoader.FromSvg<SvgDocument>(svg);
        var rect = document.Descendants().OfType<SvgRectangle>().Single(static element => element.ID == "target");
        var fill = Assert.IsType<SvgColourServer>(rect.Fill);

        Assert.Equal(Color.Green.ToArgb(), fill.Colour.ToArgb());
    }

    private static LoadResult CaptureLoad(Func<SvgDocument> load)
    {
        try
        {
            var document = load();
            var text = document
                .Descendants()
                .OfType<SvgText>()
                .Single(static element => element.ID == "target")
                .Text;
            return new LoadResult(true, text, null);
        }
        catch (Exception ex)
        {
            return new LoadResult(false, null, ex.GetType().FullName);
        }
    }

    private readonly record struct LoadResult(bool Succeeded, string? Text, string? ExceptionType);
}
