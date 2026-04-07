---
title: "SvgResource and Brushes"
---

# SvgResource and Brushes

`SvgResourceExtension` replaces the older `SvgBrush` naming and lets you use SVG content anywhere an Avalonia brush is accepted.

## Direct XAML usage

```xml
<Border Background="{SvgResource /Assets/__tiger.svg}" />
```

## Resource dictionary usage

```xml
<UserControl
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:svg="clr-namespace:Avalonia.Svg.Skia;assembly=Svg.Controls.Skia.Avalonia">
  <UserControl.Resources>
    <svg:SvgResource x:Key="TigerBrush"
                     Stretch="UniformToFill"
                     AlignmentX="Center"
                     AlignmentY="Center"
                     TileMode="Tile"
                     DestinationRect="0,0,1,1"
                     Opacity="0.85">/Assets/__tiger.svg</svg:SvgResource>
  </UserControl.Resources>

  <Border Background="{DynamicResource TigerBrush}" />
</UserControl>
```

## Code-behind usage

The extension can also create a brush directly:

```csharp
using Avalonia.Media;
using Avalonia.Svg.Skia;

IBrush background = new SvgResourceExtension("avares://MyAssembly/Assets/Icon.svg")
{
    BaseUri = new Uri("avares://MyAssembly/")
}.ToBrush();
```

Or via the static helpers:

```csharp
var brush = SvgResourceExtension.CreateBrush(
    "avares://MyAssembly/Assets/Icon.svg",
    stretch: Stretch.Fill,
    alignmentX: AlignmentX.Center,
    alignmentY: AlignmentY.Center);
```

## Why this is useful

This pattern is a good fit when:

- the same icon must be reused as a background or fill,
- the source SVG should remain centralized in resources,
- layout, tiling, or transforms must be configured at the brush level instead of the control level.
