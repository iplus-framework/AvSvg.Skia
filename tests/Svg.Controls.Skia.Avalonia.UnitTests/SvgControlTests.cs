using Avalonia.Headless.XUnit;
using Avalonia.Svg.Skia;
using Xunit;

namespace Avalonia.Svg.Skia.UnitTests;

public class SvgControlTests
{
    [AvaloniaFact]
    public void AnimationPlaybackRate_NormalizesNonFiniteValues()
    {
        var svg = new Svg(new System.Uri("avares://Svg.Controls.Skia.Avalonia.UnitTests/"));

        svg.AnimationPlaybackRate = double.NaN;
        Assert.Equal(0d, svg.AnimationPlaybackRate);

        svg.AnimationPlaybackRate = double.PositiveInfinity;
        Assert.Equal(0d, svg.AnimationPlaybackRate);

        svg.AnimationPlaybackRate = -1d;
        Assert.Equal(0d, svg.AnimationPlaybackRate);

        svg.AnimationPlaybackRate = 1.5d;
        Assert.Equal(1.5d, svg.AnimationPlaybackRate);
    }
}
