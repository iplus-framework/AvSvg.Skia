---
title: "API Coverage Index"
---

# API Coverage Index

The generated API reference under `/api` is built from these projects:

- `../src/Svg.Skia/Svg.Skia.csproj`
- `../src/Svg.Model/Svg.Model.csproj`
- `../src/Svg.Custom/Svg.Custom.csproj`
- `../src/Svg.Controls.Avalonia/Svg.Controls.Avalonia.csproj`
- `../src/Svg.Controls.Skia.Avalonia/Svg.Controls.Skia.Avalonia.csproj`
- `../src/Skia.Controls.Avalonia/Skia.Controls.Avalonia.csproj`
- `../src/ShimSkiaSharp/ShimSkiaSharp.csproj`
- `../src/Svg.SourceGenerator.Skia/Svg.SourceGenerator.Skia.csproj`

## Build settings

Current API settings:

- configuration: `Release`
- target framework override: `netstandard2.0`
- output path: `/api`

## Why `netstandard2.0`

This repository mixes:

- multi-target runtime packages,
- `netstandard2.0`-only generator packages.

Using `netstandard2.0` as the documentation build target keeps the API site aligned across the documented assemblies without having to split the API generation into multiple passes.

## `Svg.CodeGen.Skia`

`Svg.CodeGen.Skia` is described in the authored docs but is not added as a separate `api.dotnet` project because `Svg.SourceGenerator.Skia` links the same codegen types into its assembly, which produces duplicate API UIDs during Lunet generation.
