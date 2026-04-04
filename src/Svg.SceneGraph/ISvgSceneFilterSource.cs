using ShimSkiaSharp;

namespace Svg.Skia;

internal interface ISvgSceneFilterSource
{
    SKPicture? SourceGraphic(SKRect? clip);
    SKPicture? BackgroundImage(SKRect? clip);
    SKPaint? FillPaint();
    SKPaint? StrokePaint();
}
