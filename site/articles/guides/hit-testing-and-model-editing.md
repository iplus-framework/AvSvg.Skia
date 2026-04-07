---
title: "Hit Testing and Model Editing"
---

# Hit Testing and Model Editing

## Hit testing in picture coordinates

`SKSvg` exposes hit testing for both drawables and SVG elements:

```csharp
using System.Linq;
using ShimSkiaSharp;
using Svg.Skia;

using var svg = new SKSvg();
svg.Load("image.svg");

var element = svg.HitTestElements(new SKPoint(10, 10)).FirstOrDefault();
var drawable = svg.HitTestDrawables(new SKRect(0, 0, 40, 40)).FirstOrDefault();
```

## Hit testing on transformed canvases

When the picture is drawn through a transformed canvas, convert the input point or rectangle back into picture space first:

```csharp
using ShimSkiaSharp;
using Svg.Skia;

using var svg = new SKSvg();
svg.Load("image.svg");

var canvasMatrix = SKMatrix.CreateScale(2f, 2f);
if (svg.TryGetPicturePoint(new SKPoint(50, 50), canvasMatrix, out var picturePoint))
{
    var hits = svg.HitTestElements(picturePoint);
}
```

There are overloads that take the canvas matrix directly:

- `HitTestElements(SKPoint point, SKMatrix canvasMatrix)`
- `HitTestElements(SKRect rect, SKMatrix canvasMatrix)`
- `HitTestDrawables(SKPoint point, SKMatrix canvasMatrix)`
- `HitTestDrawables(SKRect rect, SKMatrix canvasMatrix)`

## Avalonia control hit testing

The Skia-backed `Avalonia.Svg.Skia.Svg` control exposes `HitTestElements(Point point)` in control coordinates, so the control handles the coordinate transform for you.

## Model editing

Once you identify the commands you care about, update the model and rebuild:

```csharp
using System.Linq;
using ShimSkiaSharp;
using Svg.Skia;

using var svg = new SKSvg();
svg.Load("image.svg");

foreach (var cmd in svg.Model?.Commands?.OfType<DrawPathCanvasCommand>() ?? Enumerable.Empty<DrawPathCanvasCommand>())
{
    if (cmd.Paint?.Color is { } color)
    {
        cmd.Paint.Color = new SKColor(color.Red, 0, 0, color.Alpha);
    }
}

svg.RebuildFromModel();
```

In Avalonia, the same pattern applies through `SvgSource.RebuildFromModel()`.
