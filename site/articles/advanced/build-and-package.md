---
title: "Build and Package"
---

# Build and Package

## Local repository workflow

The expected local sequence is:

```bash
git submodule update --init --recursive
dotnet format --no-restore
dotnet build Svg.Skia.slnx -c Release
dotnet test Svg.Skia.slnx -c Release
```

## CI workflows

The repository now has dedicated workflows for:

- build and test,
- release packaging,
- docs publishing.

The docs workflow builds the Lunet site and publishes `site/.lunet/build/www` to GitHub Pages on pushes to `main` or `master`.

## Packaging notes

The repository ships more than one NuGet package. The main runtime packages are:

- `Svg.Skia`
- `Svg.Model`
- `Svg.Controls.Avalonia`
- `Svg.Controls.Skia.Avalonia`
- `Skia.Controls.Avalonia`
- `Svg.SourceGenerator.Skia`
- `Svg.CodeGen.Skia`
- `Svg.Custom`
- `ShimSkiaSharp`

There are also sample or tool packages such as `Svg.Skia.Converter` and `svgc`.

## Release path

The `release.yml` workflow:

- builds and tests a release,
- packs the NuGet artifacts,
- pushes packages to NuGet,
- creates a GitHub release with the packaged artifacts attached.
