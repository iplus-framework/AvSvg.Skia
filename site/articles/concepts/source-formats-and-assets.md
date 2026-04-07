---
title: "Source Formats and Assets"
---

# Source Formats and Assets

Svg.Skia accepts several input forms:

- file paths,
- streams,
- XML readers,
- raw SVG strings,
- Android VectorDrawable XML,
- Avalonia resource URIs.

## SVG files and strings

`SKSvg` supports:

- `Load(string path)`
- `Load(Stream stream, SvgParameters? parameters = null, Uri? baseUri = null)`
- `Load(XmlReader reader)`
- `FromSvg(string svg)`

The Avalonia `SvgSource` wrappers expose the same idea but add `baseUri`-aware resource resolution for XAML.

## Android VectorDrawable

There are two supported entry points:

- `Load(path)` with auto-detection for VectorDrawable XML.
- `LoadVectorDrawable(...)` and `FromVectorDrawable(...)` for explicit VectorDrawable handling.

The repository includes tests that validate accepted and rejected Android attributes, element nesting, and SVG equivalence for representative cases.

## CSS injection

Both Avalonia stacks support `Css` and `CurrentCss` overlays. This is useful for:

- recoloring icons without changing the source asset,
- styling based on control state,
- mutating the rendered output through resource dictionaries or styles.

`SvgParameters` carries entity substitutions and CSS fragments when loading.

## Resource and URI loading

The Avalonia packages resolve:

- relative asset paths,
- `avares://` URIs,
- file URIs,
- HTTP or HTTPS URLs.

The Skia-backed `Avalonia.Svg.Skia.SvgSource` also exposes `EnableThrowOnMissingResource` for stricter missing-resource behavior during development or testing.
