---
title: "Package Architecture"
---

# Package Architecture

Svg.Skia is composed of several layers that can be used independently or together.

## Core rendering path

- `Svg.Custom` vendors and packages the upstream SVG object model used to parse documents.
- `Svg.Model` converts parsed SVG content into a picture-recorder model based on `ShimSkiaSharp`.
- `Svg.Skia` turns that model into `SkiaSharp.SKPicture`, exposes helpers for loading and exporting, and adds hit-testing support.

## Avalonia integration

- `Svg.Controls.Skia.Avalonia` builds Avalonia controls around `Svg.Skia`.
- `Svg.Controls.Avalonia` exposes a similar API surface but renders through the Avalonia drawing model.
- `Skia.Controls.Avalonia` provides reusable general-purpose Skia controls such as `SKCanvasControl`, `SKPictureControl`, and `SKPictureImage`.

## Generated-code path

- `Svg.CodeGen.Skia` generates C# from the picture model.
- `Svg.SourceGenerator.Skia` hosts the incremental Roslyn generator package.
- `samples/svgc` demonstrates manual file-to-C# generation.

## Typical namespace map

| Namespace | Role |
| --- | --- |
| `Svg.Skia` | runtime loading, rendering, export, hit testing |
| `Svg.Model` | model types, parameters, services, drawable conversion |
| `Avalonia.Svg.Skia` | Skia-backed Avalonia controls and resources |
| `Avalonia.Svg` | Avalonia drawing-stack controls and resources |
| `Avalonia.Controls.Skia` | general-purpose Skia controls for Avalonia |
| `ShimSkiaSharp` | picture-recorder model used for command inspection and rebuild |
| `Svg.SourceGenerator.Skia` | incremental generator surface |
| `Svg.CodeGen.Skia` | direct code-generation helpers |

## Repository shape

- `src/` contains the shipping libraries.
- `samples/` contains demo apps and CLI utilities.
- `tests/` covers renderer behavior, Avalonia integration, source generation, and VectorDrawable support.
- `externals/` holds the vendored SVG source and external reference suites.

For installation details, responsibilities, and usage patterns per library package, see [Packages](../packages).
