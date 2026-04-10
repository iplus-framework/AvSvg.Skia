using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public void FromSvg_IgnoresEmptyStyleElements()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg">
              <style />
              <rect id="target" width="10" height="10" fill="red" />
            </svg>
            """;

        var document = SvgDocumentCompatibilityLoader.FromSvg<SvgDocument>(svg);
        var rect = document.Descendants().OfType<SvgRectangle>().Single(static element => element.ID == "target");
        var fill = Assert.IsType<SvgColourServer>(rect.Fill);

        Assert.Equal(Color.Red.ToArgb(), fill.Colour.ToArgb());
    }

    [Fact]
    public void OpenPath_AppliesImportedStylesheetsWhenMediaMatchesScreen()
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
                    @import url("styles.css") screen;
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
    public void OpenPath_IgnoresImportedStylesheetsWhenMediaDoesNotMatch()
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
                    @import url("styles.css") print;
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
    public void OpenPath_IgnoresImportedStylesheetsWhenMediaListContainsEmptyEntry()
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
                    @import url("styles.css") print,;
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
    public void OpenPath_AppliesImportedStylesheetsWhenMediaFeatureMatchesStaticViewport()
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
                    @import url("styles.css") screen and (min-width: 100px) and (orientation: landscape);
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
    public void OpenPath_IgnoresImportedStylesheetsWhenMediaFeatureDoesNotMatchStaticViewport()
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
                    @import url("styles.css") screen and (max-width: 1px);
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
    public void OpenPath_IgnoresImportedStylesheetsAfterStyleRules()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var cssPath = Path.Combine(tempDirectory, "styles.css");
            var svgPath = Path.Combine(tempDirectory, "test.svg");

            File.WriteAllText(cssPath, "#target { fill: green !important; }");
            File.WriteAllText(svgPath, """
                <svg xmlns="http://www.w3.org/2000/svg">
                  <style type="text/css"><![CDATA[
                    #target { fill: blue; }
                    @import url("styles.css");
                  ]]></style>
                  <circle id="target" cx="10" cy="10" r="5" fill="red" />
                </svg>
                """);

            var document = SvgDocumentCompatibilityLoader.Open<SvgDocument>(svgPath, new SvgOptions());
            var circle = document.Descendants().OfType<SvgCircle>().Single(static element => element.ID == "target");
            var fill = Assert.IsType<SvgColourServer>(circle.Fill);

            Assert.Equal(Color.Blue.ToArgb(), fill.Colour.ToArgb());
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
    public void FromSvg_LinkPseudoClassDoesNotStyleNonLinkTextSiblings()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink">
              <text id="label" fill="black">
                prefix
                <a id="cta" xlink:href="#target">link</a>
                suffix
              </text>
              <style>a#cta:link { fill: red; }</style>
            </svg>
            """;

        var document = SvgDocumentCompatibilityLoader.FromSvg<SvgDocument>(svg);
        var text = document.Descendants().OfType<SvgText>().Single(static element => element.ID == "label");
        var anchor = document.Descendants().OfType<SvgAnchor>().Single(static element => element.ID == "cta");

        var textFill = Assert.IsType<SvgColourServer>(text.Fill);
        var anchorFill = Assert.IsType<SvgColourServer>(anchor.Fill);

        Assert.Equal(Color.Black.ToArgb(), textFill.Colour.ToArgb());
        Assert.Equal(Color.Red.ToArgb(), anchorFill.Colour.ToArgb());
    }

    [Fact]
    public void FromSvg_LinkPseudoClassStylesTextContainerWhenLinkOwnsEntireTextRun()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink">
              <text id="label">
                <a id="cta" xlink:href="#target">link</a>
              </text>
              <style>:link { fill: red; }</style>
            </svg>
            """;

        var document = SvgDocumentCompatibilityLoader.FromSvg<SvgDocument>(svg);
        var text = document.Descendants().OfType<SvgText>().Single(static element => element.ID == "label");
        var anchor = document.Descendants().OfType<SvgAnchor>().Single(static element => element.ID == "cta");

        var textFill = Assert.IsType<SvgColourServer>(text.Fill);
        var anchorFill = Assert.IsType<SvgColourServer>(anchor.Fill);

        Assert.Equal(Color.Red.ToArgb(), textFill.Colour.ToArgb());
        Assert.Equal(Color.Red.ToArgb(), anchorFill.Colour.ToArgb());
    }

    [Fact]
    public void FromSvg_LinkPseudoClassDoesNotStylePreservedWhitespaceOutsideLink()
    {
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink">
              <text id="label" xml:space="preserve" fill="black"> <a id="cta" xlink:href="#target">link</a> </text>
              <style>:link { fill: red; }</style>
            </svg>
            """;

        var document = SvgDocumentCompatibilityLoader.FromSvg<SvgDocument>(svg);
        var text = document.Descendants().OfType<SvgText>().Single(static element => element.ID == "label");
        var anchor = document.Descendants().OfType<SvgAnchor>().Single(static element => element.ID == "cta");

        var textFill = Assert.IsType<SvgColourServer>(text.Fill);
        var anchorFill = Assert.IsType<SvgColourServer>(anchor.Fill);

        Assert.Equal(Color.Black.ToArgb(), textFill.Colour.ToArgb());
        Assert.Equal(Color.Red.ToArgb(), anchorFill.Colour.ToArgb());
    }

    [Fact]
    public void ImportChain_DistinguishesUrisThatDifferOnlyByCase()
    {
        var processorType = typeof(SvgDocumentCompatibilityLoader).Assembly.GetType("Svg.SvgCssCompatibilityProcessor");
        Assert.NotNull(processorType);

        var createImportChain = processorType!.GetMethod(
            "CreateImportChain",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(createImportChain);

        var importChain = Assert.IsType<HashSet<string>>(createImportChain!.Invoke(null, null));

        Assert.True(importChain.Add("file:///tmp/A.css"));
        Assert.True(importChain.Add("file:///tmp/a.css"));
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

    [Fact]
    public void OpenPath_IgnoresCommentedImportTokens()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var cssPath = Path.Combine(tempDirectory, "styles.css");
            var svgPath = Path.Combine(tempDirectory, "test.svg");

            File.WriteAllText(cssPath, "#target { fill: green !important; }");
            File.WriteAllText(svgPath, """
                <svg xmlns="http://www.w3.org/2000/svg">
                  <style type="text/css"><![CDATA[
                    /* @import url("styles.css"); */
                    #target { fill: red; }
                  ]]></style>
                  <circle id="target" cx="10" cy="10" r="5" fill="blue" />
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
    public void OpenPath_IgnoresImportTokensInsideStrings()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var cssPath = Path.Combine(tempDirectory, "styles.css");
            var svgPath = Path.Combine(tempDirectory, "test.svg");

            File.WriteAllText(cssPath, "#target { fill: green !important; }");
            File.WriteAllText(svgPath, """
                <svg xmlns="http://www.w3.org/2000/svg">
                  <text id="target" x="0" y="15" fill="blue">test</text>
                  <style type="text/css"><![CDATA[
                    #target {
                      font-family: "@import url('styles.css');";
                      fill: red;
                    }
                  ]]></style>
                </svg>
                """);

            var document = SvgDocumentCompatibilityLoader.Open<SvgDocument>(svgPath, new SvgOptions());
            var text = document.Descendants().OfType<SvgText>().Single(static element => element.ID == "target");
            var fill = Assert.IsType<SvgColourServer>(text.Fill);

            Assert.Equal(Color.Red.ToArgb(), fill.Colour.ToArgb());
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
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
