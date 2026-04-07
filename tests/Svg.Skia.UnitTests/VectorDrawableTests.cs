using System;
using System.IO;
using System.Text;
using SkiaSharp;
using Xunit;

namespace Svg.Skia.UnitTests;

public class VectorDrawableTests
{
    [Fact]
    public void LoadVectorDrawable_Stream_LoadsPicture()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(FilledVectorDrawable));
        using var svg = new SKSvg();

        var picture = svg.LoadVectorDrawable(stream);

        Assert.NotNull(picture);
        Assert.NotNull(svg.Picture);
    }

    [Fact]
    public void Load_FilePath_AutoDetectsVectorDrawableXml()
    {
        var path = Path.Combine(Path.GetTempPath(), $"SvgSkia_VectorDrawable_{Guid.NewGuid():N}.xml");
        File.WriteAllText(path, FilledVectorDrawable);

        try
        {
            using var svg = new SKSvg();
            var picture = svg.Load(path);

            Assert.NotNull(picture);
            Assert.NotNull(svg.Picture);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Render_VectorDrawableMatchesEquivalentSvg_FilledPath()
    {
        AssertRenderedEqual(FilledVectorDrawable, FilledSvg);
    }

    [Fact]
    public void Render_VectorDrawableMatchesEquivalentSvg_StrokePath()
    {
        AssertRenderedEqual(StrokedVectorDrawable, StrokedSvg);
    }

    [Fact]
    public void Render_VectorDrawableMatchesEquivalentSvg_ClippedGroup()
    {
        AssertRenderedEqual(ClippedVectorDrawable, ClippedSvg);
    }

    [Fact]
    public void Render_VectorDrawableMatchesEquivalentSvg_ClipPathEvenOdd()
    {
        AssertRenderedEqual(EvenOddClipVectorDrawable, EvenOddClipSvg);
    }

    private static void AssertRenderedEqual(string vectorDrawableXml, string svgXml)
    {
        using var vector = new SKSvg();
        using var svg = new SKSvg();

        Assert.NotNull(vector.FromVectorDrawable(vectorDrawableXml));
        Assert.NotNull(svg.FromSvg(svgXml));
        Assert.NotNull(vector.Picture);
        Assert.NotNull(svg.Picture);

        using var vectorBitmap = vector.Picture!.ToBitmap(
            SKColors.Transparent,
            1f,
            1f,
            SKColorType.Rgba8888,
            SKAlphaType.Unpremul,
            vector.Settings.Srgb);
        using var svgBitmap = svg.Picture!.ToBitmap(
            SKColors.Transparent,
            1f,
            1f,
            SKColorType.Rgba8888,
            SKAlphaType.Unpremul,
            svg.Settings.Srgb);

        Assert.NotNull(vectorBitmap);
        Assert.NotNull(svgBitmap);
        Assert.Equal(svgBitmap!.Width, vectorBitmap!.Width);
        Assert.Equal(svgBitmap.Height, vectorBitmap.Height);

        for (var x = 0; x < vectorBitmap.Width; x++)
        {
            for (var y = 0; y < vectorBitmap.Height; y++)
            {
                Assert.Equal(svgBitmap.GetPixel(x, y), vectorBitmap.GetPixel(x, y));
            }
        }
    }

    private const string FilledVectorDrawable = """
        <vector xmlns:android="http://schemas.android.com/apk/res/android"
            android:width="24dp"
            android:height="24dp"
            android:viewportWidth="24"
            android:viewportHeight="24">
            <path
                android:fillColor="#ff336699"
                android:pathData="M2,2 L22,2 L22,22 L2,22 Z" />
        </vector>
        """;

    private const string FilledSvg = """
        <svg width="24" height="24" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path fill="#336699" d="M2,2 L22,2 L22,22 L2,22 Z" />
        </svg>
        """;

    private const string StrokedVectorDrawable = """
        <vector xmlns:android="http://schemas.android.com/apk/res/android"
            android:width="24dp"
            android:height="24dp"
            android:viewportWidth="24"
            android:viewportHeight="24">
            <path
                android:fillColor="#00000000"
                android:strokeColor="#ffcc3300"
                android:strokeWidth="3"
                android:strokeLineCap="round"
                android:strokeLineJoin="bevel"
                android:pathData="M4,4 L20,12 L4,20 Z" />
        </vector>
        """;

    private const string StrokedSvg = """
        <svg width="24" height="24" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path fill="none" stroke="#cc3300" stroke-width="3" stroke-linecap="round" stroke-linejoin="bevel" d="M4,4 L20,12 L4,20 Z" />
        </svg>
        """;

    private const string ClippedVectorDrawable = """
        <vector xmlns:android="http://schemas.android.com/apk/res/android"
            android:width="24dp"
            android:height="24dp"
            android:viewportWidth="24"
            android:viewportHeight="24">
            <group android:name="clipped">
                <clip-path
                    android:name="clip"
                    android:pathData="M2,2 L22,2 L22,22 L2,22 Z" />
                <path
                    android:fillColor="#ff00aa44"
                    android:pathData="M0,0 L24,0 L24,24 L0,24 Z" />
            </group>
        </vector>
        """;

    private const string ClippedSvg = """
        <svg width="24" height="24" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <clipPath id="clip">
              <path d="M2,2 L22,2 L22,22 L2,22 Z" />
            </clipPath>
          </defs>
          <g clip-path="url(#clip)">
            <path fill="#00aa44" d="M0,0 L24,0 L24,24 L0,24 Z" />
          </g>
        </svg>
        """;

    private const string EvenOddClipVectorDrawable = """
        <vector xmlns:android="http://schemas.android.com/apk/res/android"
            android:width="24dp"
            android:height="24dp"
            android:viewportWidth="24"
            android:viewportHeight="24">
            <group>
                <clip-path
                    android:name="clip"
                    android:fillType="evenOdd"
                    android:pathData="M1,1 L23,1 L23,23 L1,23 Z M7,7 L17,7 L17,17 L7,17 Z" />
                <path
                    android:fillColor="#ff00aa44"
                    android:pathData="M0,0 L24,0 L24,24 L0,24 Z" />
            </group>
        </vector>
        """;

    private const string EvenOddClipSvg = """
        <svg width="24" height="24" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <defs>
            <clipPath id="clip">
              <path clip-rule="evenodd" d="M1,1 L23,1 L23,23 L1,23 Z M7,7 L17,7 L17,17 L7,17 Z" />
            </clipPath>
          </defs>
          <g clip-path="url(#clip)">
            <path fill="#00aa44" d="M0,0 L24,0 L24,24 L0,24 Z" />
          </g>
        </svg>
        """;
}
