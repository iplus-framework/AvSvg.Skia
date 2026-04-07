---
title: "Svg Control and SvgImage"
---

# Svg Control and SvgImage

## `Svg` control

The `Svg` control is the simplest way to render an SVG in XAML:

```xml
<svg:Svg Path="/Assets/__tiger.svg" />
```

Common properties include:

- `Path`
- `Source`
- `Stretch`
- `StretchDirection`

The Skia-backed control also adds:

- `EnableCache`
- `Wireframe`
- `DisableFilters`
- `Zoom`
- `PanX`
- `PanY`
- `ZoomToPoint(...)`

That makes it a better fit when you need interaction or viewport behavior.

## `SvgImage`

Use `SvgImage` when the target property expects `IImage`, for example on an Avalonia `Image` control:

```xml
<Image Source="{SvgImage /Assets/__AJ_Digital_Camera.svg}" />
```

Or through explicit object syntax:

```xml
<Image>
  <Image.Source>
    <svg:SvgImage Source="/Assets/__AJ_Digital_Camera.svg" />
  </Image.Source>
</Image>
```

## `SvgSource`

`SvgSource` is the reusable source object behind `SvgImage`.

Use it when:

- the asset should be created once and reused,
- you need access to the loaded picture or model from code,
- you want to rebuild or reload the content explicitly.

## Control coordinate hit testing

The Skia-backed `Svg` control exposes:

```csharp
var hits = svgControl.HitTestElements(new Point(x, y));
```

That method accepts control coordinates, not picture coordinates.
