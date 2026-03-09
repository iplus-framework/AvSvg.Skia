---
title: "Quickstart: Loading and Rendering"
---

# Quickstart: Loading and Rendering

The shortest path through the core library is:

1. Create `SKSvg`.
2. Load SVG content from a file, stream, XML reader, or raw string.
3. Use the generated `SKPicture` directly or export it.

## Render an SVG file

```csharp
using SkiaSharp;
using Svg.Skia;

var svg = new SKSvg();

svg.Load("image.svg");

SKCanvas canvas = ...;
canvas.DrawPicture(svg.Picture);
```

## Save to png

```csharp
using SkiaSharp;
using Svg.Skia;

using var svg = new SKSvg();

if (svg.Load("image.svg") is { })
{
    svg.Save("image.png", SKEncodedImageFormat.Png, 100, 1f, 1f);
}
```

You can also export the picture directly:

```csharp
using System.IO;
using SkiaSharp;
using Svg.Skia;

using var svg = new SKSvg();

if (svg.Load("image.svg") is { })
{
    using var stream = File.OpenWrite("image.png");
    svg.Picture!.ToImage(
        stream,
        SKColors.Transparent,
        SKEncodedImageFormat.Png,
        100,
        1f,
        1f,
        SKImageInfo.PlatformColorType,
        SKAlphaType.Unpremul,
        svg.Settings.Srgb);
}
```

## Save to pdf or xps

```csharp
using SkiaSharp;
using Svg.Skia;

using var svg = new SKSvg();

if (svg.Load("image.svg") is { })
{
    svg.Picture!.ToPdf("image.pdf", SKColors.Empty, 1f, 1f);
    svg.Picture!.ToXps("image.xps", SKColors.Empty, 1f, 1f);
}
```

## Load raw markup

```csharp
using Svg.Skia;

const string markup = "<svg width=\"16\" height=\"16\"><rect width=\"16\" height=\"16\" fill=\"red\" /></svg>";

using var svg = new SKSvg();
svg.FromSvg(markup);
```

## Load Android VectorDrawable

`SKSvg.Load(path)` auto-detects Android VectorDrawable XML when the file content matches that format. You can also opt into the dedicated API explicitly:

```csharp
using Svg.Skia;

using var svg = new SKSvg();

if (svg.LoadVectorDrawable("icon.xml") is { })
{
    svg.Save("icon.png", SkiaSharp.SKColors.Transparent);
}
```

## Next steps

- Use [Source Formats and Assets](../concepts/source-formats-and-assets) for file-path, URI, stream, and CSS-loading details.
- Use [Exporting Images, Pdf, and Xps](../guides/exporting-images-pdf-and-xps) for output behavior and supported formats.
- Use [Picture Model and Rebuild](../concepts/picture-model-and-rebuild) if you want to mutate the generated commands.
