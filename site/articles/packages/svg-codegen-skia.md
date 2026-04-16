---
title: "Svg.CodeGen.Skia"
---

# Svg.CodeGen.Skia

`Svg.CodeGen.Skia` turns the intermediate `ShimSkiaSharp.SKPicture` model into C# source code. It is the package to choose when SVG parsing should happen ahead of time and the final application should ship compiled `SKPicture` builders instead of raw `.svg` files.

## Install

```bash
dotnet add package Svg.CodeGen.Skia
```

## Choose this package when

- you want a custom asset-preparation step instead of a Roslyn source generator,
- generated files should be checked into source control,
- a build pipeline needs explicit control over file names and output locations,
- you want to generate C# from an already-built `ShimSkiaSharp.SKPicture`.

## Main type

| Type | Role |
| --- | --- |
| `SkiaCSharpCodeGen` | Generates C# source from a `ShimSkiaSharp.SKPicture` |

The generated class includes:

- a static `Picture` property,
- a static `Draw(SKCanvas)` method,
- all SkiaSharp object creation and disposal logic required to reconstruct the picture.

## Typical workflow

1. Parse the SVG into the repository's intermediate model.
2. Call `SkiaCSharpCodeGen.Generate(...)`.
3. Write the returned string to a `.cs` file.
4. Compile that file into the consuming project.

## Example

```csharp
using System.IO;
using Svg.CodeGen.Skia;
using Svg.Skia;

using var svg = new SKSvg();

if (svg.Load("Assets/icon.svg") is not null && svg.Model is not null)
{
    var code = SkiaCSharpCodeGen.Generate(svg.Model, "MyApp.Generated", "Icon");
    File.WriteAllText("Generated/Icon.g.cs", code);
}
```

This example uses `Svg.Skia` to create the intermediate model, but `Svg.CodeGen.Skia` itself only needs a `ShimSkiaSharp.SKPicture`.

## Why use this package instead of the source generator

Choose `Svg.CodeGen.Skia` when generation is an explicit build step and you want control over:

- output file locations,
- naming conventions,
- checked-in generated artifacts,
- batch generation outside MSBuild or Roslyn.

Choose [Svg.SourceGenerator.Skia](svg-sourcegenerator-skia) when SVG files already live in the consuming project and implicit build-time generation is the better experience.

## Good scenarios

- design-system icon packs converted during CI,
- SDKs that ship compiled picture classes,
- NativeAOT or trimmed applications that want less runtime parsing work,
- custom CLI tools such as `svgc`.

## Related docs

- [Source Generator and svgc](../guides/source-generator-and-svgc)
- [Svg.SourceGenerator.Skia](svg-sourcegenerator-skia)
- [ShimSkiaSharp](shim-skiasharp)
