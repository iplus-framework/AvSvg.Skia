---
title: "Samples and Tools"
---

# Samples and Tools

The `samples/` directory covers both end-user scenarios and repository-internal utilities.

## Avalonia samples

- `AvaloniaSvgSkiaSample`: end-to-end sample for `Avalonia.Svg.Skia`, including `Svg`, `SvgImage`, `SvgSource`, resource usage, and draw-control integration.
- `AvaloniaSvgSkiaStylingSample`: CSS-based restyling and pointer-over behavior.
- `AvaloniaSvgSample`: equivalent non-Skia Avalonia stack sample.
- `AvaloniaControlsSample`: `SKCanvasControl`, `SKBitmapControl`, `SKPathControl`, and `SKPictureControl`.
- `AvaloniaSKPictureImageSample`: `SKPictureImage` and animation examples.
- `AvalonDraw`: larger sample application for SVG editing-oriented scenarios.
- `TestApp`: extra Avalonia test host.

## CLI and generation samples

- `Svg.Skia.Converter`: packaged conversion tool and NativeAOT notes.
- `svgc`: manual SVG-to-C# generator.
- `Svg.SourceGenerator.Skia.Sample`: demonstrates generated `Picture` classes.
- `SvgToPng`: Windows sample focused on raster conversion.

## Generator validation helpers

The repository also includes `tests-sourcegenerators/` shell scripts that drive `svgc` across larger external icon suites.

## Demo assets

The screenshot used on the docs home page comes from the repository's existing `images/Demo.png` asset and reflects the generated-code/demo workflow in this repo.
