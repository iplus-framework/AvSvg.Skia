---
title: "Uno Svg Control"
---

# Uno Svg Control

## Namespace

```xml
xmlns:svg="using:Uno.Svg.Skia"
```

## Core control properties

The Uno `Svg` control keeps the control-facing API close to the Avalonia Skia package:

- `SvgSource`
- `Path`
- `Source`
- `Stretch`
- `StretchDirection`
- `EnableCache`
- `Wireframe`
- `DisableFilters`
- `Zoom`
- `PanX`
- `PanY`
- `Css`
- `CurrentCss`

## Reusable resource example

```xml
<Page.Resources>
  <svg:SvgSource x:Key="LogoSource" Path="/Assets/logo.svg" />
</Page.Resources>

<svg:Svg SvgSource="{StaticResource LogoSource}"
         Height="160"
         Stretch="Uniform" />
```

## Inline source example

```xml
<svg:Svg Source="{x:Bind InlineSvg}"
         CurrentCss=".accent { fill: #2563eb; }" />
```

## Hit testing

```csharp
var point = e.GetCurrentPoint(MySvg).Position;
var hits = MySvg.HitTestElements(point);
```

`HitTestElements(...)` accepts Uno control coordinates and maps them back into picture coordinates using the current stretch, zoom, and pan state.

## Current v1 limits

- no `SvgImage`
- no brush/resource markup extension equivalent
- no native-renderer fallback

For those scenarios in Uno, keep the SVG in a reusable `SvgSource` and render it through one or more `Svg` controls.
