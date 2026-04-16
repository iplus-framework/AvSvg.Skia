---
title: "Exporting Images, Pdf, and Xps"
---

# Exporting Images, Pdf, and Xps

Once `SKSvg.Picture` is available, export paths are simple and explicit.

## Raster output

`SKPictureExtensions.ToBitmap()` and `ToImage()` are the main raster helpers.

```csharp
using System.IO;
using SkiaSharp;
using Svg.Skia;

using var svg = new SKSvg();
svg.Load("image.svg");

using var stream = File.OpenWrite("image.webp");
svg.Picture!.ToImage(
    stream,
    SKColors.Transparent,
    SKEncodedImageFormat.Webp,
    100,
    1f,
    1f,
    SKImageInfo.PlatformColorType,
    SKAlphaType.Unpremul,
    svg.Settings.Srgb);
```

## Svg, pdf, and xps output

`SKPictureExtensions` also includes document-style exports:

```csharp
using SkiaSharp;
using Svg.Skia;

using var svg = new SKSvg();
svg.Load("image.svg");

svg.Picture!.ToSvg("image-roundtrip.svg", SKColors.Transparent, 1f, 1f);
svg.Picture!.ToPdf("image.pdf", SKColors.Transparent, 1f, 1f);
svg.Picture!.ToXps("image.xps", SKColors.Transparent, 1f, 1f);
```

## Scale and background

The export helpers accept:

- a background color,
- independent `scaleX` and `scaleY`,
- encoding quality for raster formats,
- explicit color-space and alpha-mode settings for bitmap output.

That makes it possible to reuse the same `SKPicture` for:

- transparent icon output,
- white-backed document previews,
- upscaled raster assets.

## Converter CLI formats

The `Svg.Skia.Converter` tool supports:

- `png`
- `jpg`
- `jpeg`
- `webp`
- `pdf`
- `xps`

See [CLI Conversion](cli-conversion) for batch examples.
