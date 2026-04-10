using System;
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
