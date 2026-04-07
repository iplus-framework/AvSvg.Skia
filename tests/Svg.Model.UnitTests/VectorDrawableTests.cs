using System;
using System.IO;
using System.Numerics;
using System.Text;
using Svg;
using Svg.Model.Services;
using Svg.Transforms;
using Xunit;

namespace Svg.Model.UnitTests;

public class VectorDrawableTests
{
    [Fact]
    public void FromVectorDrawable_MapsRootSizeOpacityAndPaintAlpha()
    {
        const string xml = """
            <vector xmlns:android="http://schemas.android.com/apk/res/android"
                android:width="24dp"
                android:height="16sp"
                android:viewportWidth="24"
                android:viewportHeight="16"
                android:alpha="0.5">
                <path
                    android:name="shape"
                    android:fillColor="#804488cc"
                    android:fillAlpha="0.5"
                    android:strokeColor="#40221100"
                    android:strokeAlpha="0.25"
                    android:strokeWidth="2"
                    android:pathData="M0,0 L24,0 L24,16 L0,16 Z" />
            </vector>
            """;

        var document = SvgService.FromVectorDrawable(xml);

        Assert.NotNull(document);
        Assert.Equal(SvgUnitType.Pixel, document!.Width.Type);
        Assert.Equal(24f, document.Width.Value);
        Assert.Equal(SvgUnitType.Pixel, document.Height.Type);
        Assert.Equal(16f, document.Height.Value);
        Assert.Equal(24f, document.ViewBox.Width);
        Assert.Equal(16f, document.ViewBox.Height);
        Assert.Equal(0.5f, document.Opacity);

        var path = Assert.IsType<SvgPath>(document.GetElementById("shape"));
        var fill = Assert.IsType<SvgColourServer>(path.Fill);
        var stroke = Assert.IsType<SvgColourServer>(path.Stroke);

        Assert.Equal(255, fill.Colour.A);
        Assert.Equal(255, stroke.Colour.A);
        Assert.Equal(0.5f * (128f / 255f), path.FillOpacity, 3);
        Assert.Equal(0.25f * (64f / 255f), path.StrokeOpacity, 3);
        Assert.Equal(2f, path.StrokeWidth.Value);
    }

    [Fact]
    public void OpenVectorDrawable_Stream_MapsNamesAndClipPath()
    {
        const string xml = """
            <vector xmlns:android="http://schemas.android.com/apk/res/android"
                android:width="24dp"
                android:height="24dp"
                android:viewportWidth="24"
                android:viewportHeight="24">
                <group android:name="outer">
                    <clip-path
                        android:name="clipper"
                        android:pathData="M2,2 L22,2 L22,22 L2,22 Z" />
                    <path
                        android:name="fill"
                        android:fillColor="#ff0000"
                        android:pathData="M0,0 L24,0 L24,24 L0,24 Z" />
                </group>
            </vector>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var document = SvgService.OpenVectorDrawable(stream);

        Assert.NotNull(document);
        var group = Assert.IsType<SvgGroup>(document!.GetElementById("outer"));
        var clipPath = Assert.IsType<SvgClipPath>(document.GetElementById("clipper"));
        var path = Assert.IsType<SvgPath>(document.GetElementById("fill"));

        Assert.NotNull(clipPath);
        Assert.NotNull(path);

        var clipWrapper = Assert.IsType<SvgGroup>(Assert.Single(group.Children));
        Assert.Equal("#clipper", clipWrapper.ClipPath?.ToString());
        Assert.Same(path, Assert.Single(clipWrapper.Children));
    }

    [Fact]
    public void OpenVectorDrawable_Stream_MapsClipPathFillType()
    {
        const string xml = """
            <vector xmlns:android="http://schemas.android.com/apk/res/android"
                android:width="24dp"
                android:height="24dp"
                android:viewportWidth="24"
                android:viewportHeight="24">
                <group>
                    <clip-path
                        android:name="clipper"
                        android:fillType="evenOdd"
                        android:pathData="M1,1 L23,1 L23,23 L1,23 Z M7,7 L17,7 L17,17 L7,17 Z" />
                    <path
                        android:fillColor="#ff0000"
                        android:pathData="M0,0 L24,0 L24,24 L0,24 Z" />
                </group>
            </vector>
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var document = SvgService.OpenVectorDrawable(stream);

        Assert.NotNull(document);
        var clipPath = Assert.IsType<SvgClipPath>(document!.GetElementById("clipper"));
        var clipPathElement = Assert.IsType<SvgPath>(Assert.Single(clipPath.Children));

        Assert.Equal(SvgClipRule.EvenOdd, clipPathElement.ClipRule);
    }

    [Fact]
    public void FromVectorDrawable_ComputesGroupTransformMatrix()
    {
        const string xml = """
            <vector xmlns:android="http://schemas.android.com/apk/res/android"
                android:width="24dp"
                android:height="24dp"
                android:viewportWidth="24"
                android:viewportHeight="24">
                <group
                    android:name="group"
                    android:rotation="45"
                    android:pivotX="12"
                    android:pivotY="10"
                    android:scaleX="2"
                    android:scaleY="3"
                    android:translateX="4"
                    android:translateY="5">
                    <path
                        android:fillColor="#ff0000"
                        android:pathData="M0,0 L1,0 L1,1 Z" />
                </group>
            </vector>
            """;

        var document = SvgService.FromVectorDrawable(xml);

        Assert.NotNull(document);
        var group = Assert.IsType<SvgGroup>(document!.GetElementById("group"));
        var matrix = Assert.IsType<SvgMatrix>(Assert.Single(group.Transforms));

        var expected =
            Matrix3x2.CreateTranslation(-12f, -10f) *
            Matrix3x2.CreateScale(2f, 3f) *
            Matrix3x2.CreateRotation((float)(Math.PI / 4.0)) *
            Matrix3x2.CreateTranslation(16f, 15f);

        Assert.Equal(expected.M11, matrix.Points[0], 4);
        Assert.Equal(expected.M12, matrix.Points[1], 4);
        Assert.Equal(expected.M21, matrix.Points[2], 4);
        Assert.Equal(expected.M22, matrix.Points[3], 4);
        Assert.Equal(expected.M31, matrix.Points[4], 4);
        Assert.Equal(expected.M32, matrix.Points[5], 4);
    }

    [Theory]
    [InlineData("width", "0dp")]
    [InlineData("height", "-1dp")]
    [InlineData("viewportWidth", "0")]
    [InlineData("viewportHeight", "-1")]
    public void FromVectorDrawable_RejectsNonPositiveVectorMetrics(string attributeName, string value)
    {
        var width = attributeName == "width" ? value : "24dp";
        var height = attributeName == "height" ? value : "24dp";
        var viewportWidth = attributeName == "viewportWidth" ? value : "24";
        var viewportHeight = attributeName == "viewportHeight" ? value : "24";

        var xml = $"""
            <vector xmlns:android="http://schemas.android.com/apk/res/android"
                android:width="{width}"
                android:height="{height}"
                android:viewportWidth="{viewportWidth}"
                android:viewportHeight="{viewportHeight}">
                <path android:fillColor="#ff0000" android:pathData="M0,0 L1,0 L1,1 Z" />
            </vector>
            """;

        var exception = Assert.Throws<FormatException>(() => SvgService.FromVectorDrawable(xml));
        Assert.Contains($"{attributeName} > 0", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""<vector xmlns:android="http://schemas.android.com/apk/res/android" android:width="24dp" android:height="24dp" android:viewportWidth="24" android:viewportHeight="24" android:tint="#ffffff"><path android:fillColor="#ff0000" android:pathData="M0,0 L1,0 L1,1 Z" /></vector>""")]
    [InlineData("""<vector xmlns:android="http://schemas.android.com/apk/res/android" android:width="24dp" android:height="24dp" android:viewportWidth="24" android:viewportHeight="24"><path android:fillColor="#ff0000" android:trimPathStart="0.1" android:pathData="M0,0 L1,0 L1,1 Z" /></vector>""")]
    [InlineData("""<vector xmlns:android="http://schemas.android.com/apk/res/android" android:width="24dp" android:height="24dp" android:viewportWidth="24" android:viewportHeight="24"><path android:fillColor="@color/accent" android:pathData="M0,0 L1,0 L1,1 Z" /></vector>""")]
    public void FromVectorDrawable_RejectsUnsupportedFeatures(string xml)
    {
        Assert.Throws<NotSupportedException>(() => SvgService.FromVectorDrawable(xml));
    }
}
