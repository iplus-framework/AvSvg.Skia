using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Svg.Skia.UnitTests.Common;

public static class ImageHelper
{
    private static double CompareImages(Image<Rgba32> actual, Image<Rgba32> expected, IReadOnlyCollection<Rectangle>? ignoredRegions = null)
    {
        if (actual.Width != expected.Width || actual.Height != expected.Height)
        {
            throw new ArgumentException("Images have different resolutions");
        }

        var quantity = 0;
        double squaresError = 0;

        const double scale = 1 / 255d;

        for (var x = 0; x < actual.Width; x++)
        {
            double localError = 0;

            for (var y = 0; y < actual.Height; y++)
            {
                if (IsIgnored(x, y, ignoredRegions))
                {
                    continue;
                }

                var expectedAlpha = expected[x, y].A * scale;
                var actualAlpha = actual[x, y].A * scale;

                var r = scale * (expectedAlpha * expected[x, y].R - actualAlpha * actual[x, y].R);
                var g = scale * (expectedAlpha * expected[x, y].G - actualAlpha * actual[x, y].G);
                var b = scale * (expectedAlpha * expected[x, y].B - actualAlpha * actual[x, y].B);
                var a = expectedAlpha - actualAlpha;

                var error = r * r + g * g + b * b + a * a;

                localError += error;
                quantity++;
            }

            squaresError += localError;
        }

        if (quantity == 0)
        {
            return 0d;
        }

        var meanSquaresError = squaresError / quantity;

        const int channelCount = 4;

        meanSquaresError = meanSquaresError / channelCount;

        return Math.Sqrt(meanSquaresError);
    }

    public static void CompareImages(string name, string actualPath, string expectedPath, double errorThreshold, IReadOnlyCollection<Rectangle>? ignoredRegions = null)
    {
        using var expected = Image.Load<Rgba32>(expectedPath);
        using var actual = Image.Load<Rgba32>(actualPath);
        var immediateError = CompareImages(actual, expected, ignoredRegions);

        if (immediateError > errorThreshold)
        {
            Assert.Fail(name + ": Error = " + immediateError);
        }
    }

    private static bool IsIgnored(int x, int y, IReadOnlyCollection<Rectangle>? ignoredRegions)
    {
        if (ignoredRegions is null || ignoredRegions.Count == 0)
        {
            return false;
        }

        foreach (var region in ignoredRegions)
        {
            if (x >= region.Left && x < region.Right && y >= region.Top && y < region.Bottom)
            {
                return true;
            }
        }

        return false;
    }
}
