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
- `../src/Svg.Controls.Skia.Uno/Svg.Controls.Skia.Uno.csproj`
- `../src/Skia.Controls.Avalonia/Skia.Controls.Avalonia.csproj`
- `../src/Svg.Editor.Core/Svg.Editor.Core.csproj`
- `../src/Svg.Editor.Svg/Svg.Editor.Svg.csproj`
- `../src/Svg.Editor.Skia/Svg.Editor.Skia.csproj`
- `../src/Svg.Editor.Avalonia/Svg.Editor.Avalonia.csproj`
- `../src/Svg.Editor.Skia.Avalonia/Svg.Editor.Skia.Avalonia.csproj`
- `../src/ShimSkiaSharp/ShimSkiaSharp.csproj`
- `../src/Svg.SourceGenerator.Skia/Svg.SourceGenerator.Skia.csproj`

## Build settings

Current API settings:

- configuration: `Release`
- target framework override: `netstandard2.0`
- output path: `/api`

The Uno control project uses a per-project override of `TargetFramework=net10.0` because it does not target `netstandard2.0`.

## Why `netstandard2.0`

This repository mixes:

- multi-target runtime packages,
- multi-target editor packages,
- `netstandard2.0`-only generator packages.

Using `netstandard2.0` as the documentation build target keeps the API site aligned across the documented assemblies without having to split the API generation into multiple passes.

## `Svg.CodeGen.Skia`

`Svg.CodeGen.Skia` is described in the authored docs but is not added as a separate `api.dotnet` project because `Svg.SourceGenerator.Skia` links the same codegen types into its assembly, which produces duplicate API UIDs during Lunet generation.

## When a new package is added

To keep the authored docs and generated API aligned:

1. Add the project to `site/config.scriban` under `api.dotnet.projects`.
2. Add any new Avalonia, Uno, or external assembly xrefs under `api.dotnet.external_apis` if the public API links out to assemblies that are not already covered.
3. Update [Packages and Namespaces](packages-and-namespaces) and the package article under `site/articles/packages/`.
4. Rebuild the site with `./build-docs.sh`.
