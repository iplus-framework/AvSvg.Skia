---
title: "Trimming, AOT, and NativeAOT"
---

# Trimming, AOT, and NativeAOT

Several packages in this repository declare trimming and AOT-related metadata.

## Runtime packages

`Svg.Skia` and `Svg.Custom` set:

- `IsTrimmable=true` for frameworks compatible with `net6.0`,
- `IsAotCompatible=true` for frameworks compatible with `net8.0`.

That does not remove the need for your own application-level validation, but it does mean the libraries are already annotated with these deployment modes in mind.

## Source generator path

The generated-code route is often the simplest way to avoid runtime parsing in highly constrained environments:

- `Svg.SourceGenerator.Skia` compiles assets at build time.
- `svgc` can generate checked-in code ahead of publishing.

## Converter NativeAOT note

The repository includes `samples/Svg.Skia.Converter/NativeAOT.md` with publish commands for:

- `win-x64`
- `linux-x64`
- `osx-x64`

Those commands are a good starting point when the converter should be distributed as a self-contained tool.

## Recommended approach

- Prefer the normal runtime renderer first.
- Move to generated code when startup time, deployment size, or restricted environments matter.
- Validate the exact asset set under trimming or AOT because SVG handling still depends on the shape of the content being loaded.
