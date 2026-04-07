---
title: "Svg.Skia"
---

# Svg.Skia

`Svg.Skia` is the main runtime rendering package in this repository. It loads SVG content into a `SkiaSharp.SKPicture`, preserves the intermediate model and drawable tree, and adds export and hit-testing helpers around that workflow.

## Install

```bash
dotnet add package Svg.Skia
```

## Choose this package when

- your application already uses `SkiaSharp`,
- you need direct access to `SkiaSharp.SKPicture`,
- you want runtime export to bitmap, pdf, or xps formats,
- you need hit testing or model rebuild after editing,
- you want Android `VectorDrawable` input support.

## Main types

| Type | Role |
| --- | --- |
| `SKSvg` | Main load, render, save, hit-test, and rebuild entry point |
| `SkiaModel` | Converts the intermediate `ShimSkiaSharp` model to real SkiaSharp objects |
| `SKSvgSettings` | Controls color-space and font-resolution behavior |
| `ITypefaceProvider` | Plug-in point for custom typeface lookup |
| `SkiaSvgAssetLoader` | `Svg.Model` asset-loader implementation for images and fonts |

## Typical workflow

1. Create `SKSvg`.
2. Load from file, stream, XML, string, or `SvgDocument`.
3. Read `Picture` for drawing.
4. Optionally inspect `Model` and `Drawable`.
5. Save or rebuild after edits.

## Load and draw

```csharp
using SkiaSharp;
using Svg.Skia;

using var svg = new SKSvg();

if (svg.Load("Assets/icon.svg") is { } picture)
{
    using var surface = SKSurface.Create(new SKImageInfo(256, 256));
    surface.Canvas.Clear(SKColors.Transparent);
    surface.Canvas.DrawPicture(picture);
}
```

`SKSvg.Load(...)` auto-detects `.svg`, `.svgz`, and Android `VectorDrawable` XML when loading from a file path.

## Exporting

`Svg.Skia` is the package to choose when rendering should end in a file or stream.

```csharp
using SkiaSharp;
using Svg.Skia;

using var svg = new SKSvg();

if (svg.Load("Assets/icon.svg") is not null)
{
    svg.Save("artifacts/icon.png", SKColors.Transparent, SKEncodedImageFormat.Png, 100, 2f, 2f);
    svg.Picture?.ToPdf("artifacts/icon.pdf", SKColors.White, 1f, 1f);
    svg.Picture?.ToXps("artifacts/icon.xps", SKColors.White, 1f, 1f);
}
```

Use this package instead of a UI package when the output target is not an Avalonia control.

## Hit testing and rebuild

`SKSvg` keeps both the intermediate model and the drawable tree, which makes it the best runtime package for inspection-oriented scenarios.

```csharp
using System.Linq;
using ShimSkiaSharp;
using Svg.Skia;

using var svg = new SKSvg();

if (svg.Load("Assets/icon.svg") is not null)
{
    var hit = svg.HitTestElements(new SKPoint(24, 24)).FirstOrDefault();
    if (hit is not null)
    {
        Console.WriteLine(hit.ID);
    }

    var rebuilt = svg.RebuildFromModel();
}
```

Use `TryGetPicturePoint` or `TryGetPictureRect` when the pointer coordinates come from a transformed canvas rather than picture space.

## Android VectorDrawable

`Svg.Skia` also handles Android drawable XML directly:

```csharp
using Svg.Skia;

using var svg = new SKSvg();

if (svg.LoadVectorDrawable("Assets/icon.xml") is not null)
{
    svg.Save("artifacts/icon.png", SkiaSharp.SKColors.Transparent);
}
```

If you only need the parsed SVG document produced from a VectorDrawable, see [Svg.Model](svg-model).

## Fonts and settings

`SKSvgSettings` controls the rendering conversion layer. The default configuration includes:

- platform image color type,
- sRGB and linear-sRGB color spaces,
- a font-manager provider,
- a generic fallback typeface provider.

Add custom `ITypefaceProvider` implementations when your application resolves fonts from embedded assets, custom directories, or a separate font registry.

## When not to choose `Svg.Skia`

- Choose [Svg.Controls.Skia.Avalonia](svg-controls-skia-avalonia) when the main target is Avalonia XAML and you want controls, images, and brushes.
- Choose [Svg.Model](svg-model) when the task is model inspection or mutation and you do not need the runtime `SkiaSharp.SKPicture` wrapper yet.
- Choose [Svg.SourceGenerator.Skia](svg-sourcegenerator-skia) when parsing should happen at build time instead of application startup.

## Related docs

- [Loading SVG and VectorDrawable](../guides/loading-svg-and-vectordrawable)
- [Exporting Images, PDF, and XPS](../guides/exporting-images-pdf-and-xps)
- [Hit Testing and Model Editing](../guides/hit-testing-and-model-editing)
- [Android VectorDrawable Support](../advanced/android-vectordrawable-support)
