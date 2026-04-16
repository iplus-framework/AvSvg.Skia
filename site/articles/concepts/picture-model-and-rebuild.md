---
title: "Picture Model and Rebuild"
---

# Picture Model and Rebuild

One of the more useful parts of Svg.Skia is that it does not stop at "load and draw". The intermediate command model remains available.

## Accessing the model

`SKSvg.Model` exposes the `ShimSkiaSharp.SKPicture` command tree. This is the structure that `SkiaModel` later turns into `SkiaSharp.SKPicture`.

## Editing commands

Because the model is a plain command graph, you can inspect and mutate it before rebuilding the final picture:

```csharp
using System.Linq;
using ShimSkiaSharp;
using Svg.Skia;

var svg = new SKSvg();
svg.FromSvg("<svg width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\" fill=\"red\" /></svg>");

foreach (var cmd in svg.Model?.Commands?.OfType<DrawPathCanvasCommand>() ?? Enumerable.Empty<DrawPathCanvasCommand>())
{
    if (cmd.Paint?.Color is { } color)
    {
        cmd.Paint.Color = new SKColor(color.Red, color.Red, color.Red, color.Alpha);
    }
}

svg.RebuildFromModel();
```

## Avalonia rebuild flow

The same idea exists in the Avalonia wrappers:

- `Avalonia.Svg.Skia.SvgSource.RebuildFromModel()`
- `Avalonia.Svg.Skia.SvgSource.ReLoad(...)`
- `Avalonia.Svg.SvgSource.RebuildFromModel()`

This makes it possible to keep an SVG-backed image source in XAML while still mutating the underlying commands from code.

## Cloning

The Avalonia image and source types provide cloning helpers so you can keep one original asset and derive modified variants without mutating shared state:

- `SvgSource.Clone()`
- `SvgImage.Clone()`

## When to use model editing

Model editing is a good fit when:

- CSS alone is not expressive enough,
- the asset must stay embedded but the commands need a runtime tweak,
- you want to derive monochrome or recolored variants from one source,
- you need testing hooks around specific command types.
