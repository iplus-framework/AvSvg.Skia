---
title: "Styling and Previewer"
---

# Styling and Previewer

## CSS overlays in XAML

Both Avalonia SVG stacks support attached CSS properties for quick icon restyling:

```xml
<Style Selector="Svg">
  <Setter Property="(Svg.Css)" Value=".Black { fill: #FF0000; }" />
</Style>
```

You can also apply style changes based on control state:

```xml
<Panel.Styles>
  <Style Selector="Panel:pointerover Svg">
    <Setter Property="(Svg.Css)" Value=".Black { fill: #FF0000; }" />
  </Style>
</Panel.Styles>
```

The sample project `AvaloniaSvgSkiaStylingSample` demonstrates this with pointer-over state.

## `Css` versus `CurrentCss`

- `Css` is the base override for the source.
- `CurrentCss` is useful when the current visual state needs to add or replace fragments at runtime.

The Skia-backed `SvgImage` reloads the underlying `SvgSource` when those properties change.

## Previewer setup

Avalonia previewer sometimes fails to load custom assemblies referenced only from XAML. Keep the SVG assemblies alive in `BuildAvaloniaApp()`:

```csharp
GC.KeepAlive(typeof(SvgImageExtension).Assembly);
GC.KeepAlive(typeof(Svg.Controls.Skia.Avalonia.Svg).Assembly);
```

Use the matching namespace type if the app uses the non-Skia package instead.

## When previewer issues appear

Check:

- the XAML namespace and assembly names,
- that the package assembly is referenced by the startup project,
- that `BuildAvaloniaApp()` keeps the needed assemblies alive when previewer resolution is incomplete.
