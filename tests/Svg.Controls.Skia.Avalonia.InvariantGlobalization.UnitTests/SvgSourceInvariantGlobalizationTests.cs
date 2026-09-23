using System.Globalization;
using System.IO;
using Avalonia.Svg.Skia;
using SkiaSharp;
using Xunit;

namespace Avalonia.Svg.Skia.InvariantGlobalization.UnitTests;

public class SvgSourceInvariantGlobalizationTests
{
    private const string SampleSvg = """
        <svg xmlns="http://www.w3.org/2000/svg" width="10" height="10">
          <rect x="0" y="0" width="10" height="10" fill="red" />
        </svg>
        """;

    [Fact]
    public void LoadFromSvg_CreatesPicture_WhenGlobalizationInvariant()
    {
        AssertInvariantGlobalization();

        using var source = SvgSource.LoadFromSvg(SampleSvg);

        Assert.NotNull(source.Svg);
        Assert.NotNull(source.Picture);
    }

    [Fact]
    public void Load_FilePathCreatesPicture_WhenGlobalizationInvariant()
    {
        AssertInvariantGlobalization();

        var path = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}.svg");
        File.WriteAllText(path, SampleSvg);

        try
        {
            using var source = SvgSource.Load(path);

            Assert.NotNull(source.Svg);
            Assert.NotNull(source.Picture);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromSvg_UsesDeterministicDefaultSystemLanguage_WhenGlobalizationInvariant()
    {
        AssertInvariantGlobalization();

        const string svgMarkup = """
            <svg xmlns="http://www.w3.org/2000/svg" width="40" height="40">
              <switch>
                <rect width="40" height="40" fill="#00ff00" systemLanguage="en-US" />
                <rect width="40" height="40" fill="#ff0000" />
              </switch>
            </svg>
            """;

        using var source = SvgSource.LoadFromSvg(svgMarkup);
        using var bitmap = RenderBitmap(source.Svg!, 40, 40);

        AssertMostlyGreen(bitmap.GetPixel(20, 20));
    }

    private static void AssertInvariantGlobalization()
    {
        Assert.Throws<CultureNotFoundException>(() => CultureInfo.GetCultureInfo("en-US"));
    }

    private static SKBitmap RenderBitmap(global::Svg.Skia.SKSvg svg, int width, int height)
    {
        var picture = svg.Picture;
        Assert.NotNull(picture);

        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawPicture(picture);
        return bitmap;
    }

    private static void AssertMostlyGreen(SKColor color)
    {
        Assert.True(
            color.Green > 180 && color.Red < 80 && color.Blue < 80 && color.Alpha > 220,
            $"Expected mostly green, got {color}.");
    }
}
