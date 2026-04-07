---
title: "Android VectorDrawable Support"
---

# Android VectorDrawable Support

Svg.Skia can import Android VectorDrawable XML and render it through the same pipeline as regular SVG.

## Entry points

- `SKSvg.Load(path)` auto-detects VectorDrawable XML.
- `SKSvg.LoadVectorDrawable(...)` loads it explicitly.
- `SKSvg.FromVectorDrawable(string xml)` parses raw XML content.
- `SvgService.OpenVectorDrawable(...)` and `SvgService.FromVectorDrawable(...)` are available at the document-model layer.

## What is covered

The repository includes tests for:

- root size and viewport handling,
- group transforms,
- clip-path mapping,
- path fill and stroke mapping,
- even-odd clip-path behavior,
- direct pixel equality between representative VectorDrawable and SVG equivalents.

## Contract validation

`Svg.Model.UnitTests` includes a pinned Android attribute contract that checks:

- which attributes are supported,
- which are unsupported,
- which are supported only with constraints,
- which implementation extensions are explicit.

That means unsupported features fail intentionally instead of silently producing partial output.

## Examples of rejected features

The tests explicitly reject unsupported cases such as:

- root `android:tint`,
- path trimming attributes,
- Android resource references like `@color/accent`.

## Practical guidance

Use VectorDrawable support when you want one rendering stack for:

- Android icon assets,
- desktop conversion,
- Avalonia reuse,
- generated-code workflows.

If the asset relies heavily on Android-only features outside the supported subset, treat it as a candidate for conversion to standard SVG first.
