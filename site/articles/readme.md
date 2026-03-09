---
title: "Svg.Skia Documentation"
---

# Documentation

Svg.Skia spans a few related areas:

- `Svg.Skia` and `Svg.Model` handle SVG parsing, picture-model generation, SkiaSharp output, and model mutation.
- `Svg.Controls.Avalonia` and `Svg.Controls.Skia.Avalonia` expose Avalonia controls, images, and brush helpers.
- `Skia.Controls.Avalonia` hosts general-purpose Skia controls for Avalonia.
- `Svg.CodeGen.Skia`, `Svg.SourceGenerator.Skia`, `svgc`, and `Svg.Skia.Converter` cover generated code and CLI workflows.
- [Packages](packages) gives dedicated coverage for every shippable library NuGet in the repo.

## Suggested reading order

1. [Getting Started](getting-started) for package selection and the first render.
2. [Packages](packages) for library-by-library installation, responsibilities, and usage patterns.
3. [Concepts](concepts) to understand how files, models, pictures, and Avalonia resources relate.
4. [Guides](guides) for scenario-focused tasks such as exporting images, hit testing, or generating code.
5. [XAML Usage](xaml) when the primary integration point is Avalonia.
6. [Reference](reference) for package maps, samples, licensing, and the docs pipeline.

## Generated API

- Use [API Reference](../api) for the generated surface area across the documented assemblies.
- Use [API Coverage Index](reference/api-coverage-index) to see which projects feed the API site.
