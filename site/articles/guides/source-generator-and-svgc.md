---
title: "Source Generator and svgc"
---

# Source Generator and svgc

Svg.Skia supports two generated-code workflows.

## Roslyn source generator

`Svg.SourceGenerator.Skia` compiles `.svg` additional files into C# types during build.

Add the package:

```xml
<ItemGroup>
  <PackageReference Include="Svg.SourceGenerator.Skia" Version="*" />
</ItemGroup>
```

Add SVG assets:

```xml
<ItemGroup>
  <AdditionalFiles Include="Assets\**\*.svg" />
</ItemGroup>
```

Optional metadata can control the generated type names:

```xml
<ItemGroup>
  <AdditionalFiles Include="Assets\Camera.svg"
                   ClassName="Camera"
                   NamespaceName="Svg.Generated" />
</ItemGroup>
```

The sample project under `samples/Svg.SourceGenerator.Skia.Sample` demonstrates the generated classes being used as static `Picture` providers.

## Manual code generation with svgc

`samples/svgc` is a command-line tool that uses `Svg.CodeGen.Skia` directly.

Generate one file:

```bash
dotnet run -p samples/svgc/svgc.csproj -c Release -- \
  --inputFile ./Assets/icon.svg \
  --outputFile ./Generated/Icon.cs \
  --namespace Svg.Generated \
  --class Icon
```

Generate many files from JSON:

```bash
dotnet run -p samples/svgc/svgc.csproj -c Release -- --jsonFile ./items.json
```

## When to use which

Use the source generator when:

- the SVG assets already live in the consuming project,
- incremental builds matter,
- you want generated files to stay implicit.

Use `svgc` when:

- generation is part of a separate asset-preparation step,
- you want checked-in generated files,
- a custom pipeline needs direct control over names and destinations.
