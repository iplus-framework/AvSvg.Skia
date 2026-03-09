---
title: "Installation"
---

# Installation

## NuGet packages

Core renderer:

```bash
dotnet add package Svg.Skia
```

Avalonia controls backed by Skia:

```bash
dotnet add package Svg.Controls.Skia.Avalonia
```

Avalonia controls backed by the Avalonia drawing stack:

```bash
dotnet add package Svg.Controls.Avalonia
```

General-purpose Skia controls for Avalonia:

```bash
dotnet add package Skia.Controls.Avalonia
```

Source generator:

```bash
dotnet add package Svg.SourceGenerator.Skia
```

Converter as a global tool:

```bash
dotnet tool install -g Svg.Skia.Converter
```

## Target frameworks

Most runtime packages in this repository multi-target:

- `netstandard2.0`
- `net461`
- `net6.0`
- `net8.0`
- `net10.0`

The generator and codegen packages target `netstandard2.0`.

## Repository prerequisites

The repository uses git submodules for upstream SVG sources and external test data. After cloning:

```bash
git submodule update --init --recursive
```

The local docs pipeline also uses a .NET tool manifest for Lunet:

```bash
dotnet tool restore
```

## Project workflow commands

Formatting:

```bash
dotnet format --no-restore
```

Build:

```bash
dotnet build Svg.Skia.slnx -c Release
```

Tests:

```bash
dotnet test Svg.Skia.slnx -c Release
```

Docs:

```bash
./build-docs.sh
./serve-docs.sh
```
