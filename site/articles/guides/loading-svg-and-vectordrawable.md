---
title: "Loading Svg and VectorDrawable"
---

# Loading Svg and VectorDrawable

## Files, streams, and raw strings

Use `SKSvg` when you need direct control over the runtime renderer:

```csharp
using System.IO;
using Svg.Skia;

using var svgFromFile = new SKSvg();
svgFromFile.Load("image.svg");

using var stream = File.OpenRead("image.svg");
using var svgFromStream = new SKSvg();
svgFromStream.Load(stream);

using var svgFromText = new SKSvg();
svgFromText.FromSvg("<svg width=\"16\" height=\"16\"><circle cx=\"8\" cy=\"8\" r=\"8\" fill=\"red\" /></svg>");
```

## Reusable factory helpers

If you prefer one-shot helpers, `SKSvg` exposes factory methods:

- `CreateFromFile(...)`
- `CreateFromStream(...)`
- `CreateFromSvg(...)`
- `CreateFromVectorDrawable(...)`

## Avalonia resource loading

The Skia-backed Avalonia package resolves resource paths through `SvgSource.Load(...)`:

```csharp
using Avalonia.Svg.Skia;

var source = SvgSource.Load("avares://MyAssembly/Assets/Icon.svg", baseUri: null);
```

The non-Skia package exposes the same pattern through `Avalonia.Svg.SvgSource.Load(...)`.

## VectorDrawable handling

Use these APIs when the source is Android XML:

```csharp
using Svg.Skia;

using var svg = new SKSvg();
svg.LoadVectorDrawable("icon.xml");
```

Or convert raw XML directly:

```csharp
using Svg.Skia;

using var svg = new SKSvg();
svg.FromVectorDrawable(vectorDrawableXml);
```

The repository tests cover:

- width and viewport validation,
- clip-path behavior,
- group transforms,
- supported and unsupported Android attributes,
- equivalence between representative VectorDrawable and SVG examples.
