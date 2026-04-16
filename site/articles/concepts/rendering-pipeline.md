---
title: "Rendering Pipeline"
---

# Rendering Pipeline

At a high level, the render path is:

1. Parse markup into SVG document objects.
2. Convert the document into the intermediate picture model.
3. Materialize that model as a `SkiaSharp.SKPicture`.
4. Draw the picture, export it, or expose it through Avalonia.

## Parsing

`Svg.Custom` supplies the SVG document object model. `SvgService` is the main entry point used across the repository to open or parse documents.

## Model generation

`Svg.Model` converts document nodes into a `ShimSkiaSharp.SKPicture` model. That model is a command tree rather than a GPU-backed picture, so it can be inspected or mutated safely.

## Picture materialization

`Svg.Skia.SKSvg` owns:

- the original source information,
- the generated drawable tree,
- the `ShimSkiaSharp` model, and
- the lazily created `SkiaSharp.SKPicture`.

The `Picture` property is created on demand from the current model. When the model changes, `RebuildFromModel()` creates a fresh `SKPicture`.

## Avalonia integration

The Avalonia packages sit on top of the same conceptual steps:

- `SvgSource` loads and retains the source data.
- `SvgImage` exposes it as an `IImage`.
- `Svg` wraps it as a control.
- `SvgResourceExtension` turns it into a reusable brush.

## Output paths

Once you have an `SKPicture`, the repository exposes helpers for:

- drawing to `SKCanvas`,
- raster output through `ToBitmap()` and `ToImage()`,
- vector output through `ToSvg()`,
- document output through `ToPdf()` and `ToXps()`.
