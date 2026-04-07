---
title: "ShimSkiaSharp"
---

# ShimSkiaSharp

`ShimSkiaSharp` is a cloneable, inspectable command-model mirror of the SkiaSharp drawing primitives used by the repository. It is not a renderer. It is the data model that sits between SVG parsing and either runtime conversion or generated-code output.

## Install

```bash
dotnet add package ShimSkiaSharp
```

## Choose this package when

- you need a serializable-style picture model rather than live native Skia objects,
- you want to inspect draw commands, paths, paints, and filters,
- you are writing tests, code generators, or transformation tooling,
- you need deep cloning and edit workflows over the command graph.

## Main concepts

| Type | Role |
| --- | --- |
| `SKPictureRecorder` | Records command-model drawing operations |
| `SKPicture` | Stores `CullRect` and a list of `CanvasCommand` items |
| `SKCanvas` | Records commands such as `DrawPath`, `DrawImage`, `Save`, and `Restore` |
| `SKPath` | Stores geometric path commands |
| `SKPaint` | Stores fill, stroke, shader, filter, and typography state |
| `ShimSkiaSharp.Editing.*` | Clone-on-write and traversal helpers for model editing |

## Recording and inspecting commands

```csharp
using System;
using ShimSkiaSharp;

var recorder = new SKPictureRecorder();
var canvas = recorder.BeginRecording(SKRect.Create(64, 64));

var path = new SKPath();
path.AddRect(SKRect.Create(8, 8, 48, 48));

var paint = new SKPaint
{
    Style = SKPaintStyle.Fill
};

canvas.DrawPath(path, paint);

var picture = recorder.EndRecording();

foreach (var command in picture.Commands ?? Array.Empty<CanvasCommand>())
{
    Console.WriteLine(command.GetType().Name);
}
```

This is the same family of types that `Svg.Model`, `Svg.CodeGen.Skia`, and parts of `Svg.Skia` exchange internally.

## Why it exists

Live `SkiaSharp` objects are excellent for rendering, but they are not ideal when you need:

- command inspection,
- deep cloning,
- deterministic transformations,
- code generation from a picture graph,
- tests that assert over structure instead of pixels.

`ShimSkiaSharp` fills that gap.

## Editing support

The editing helpers under `ShimSkiaSharp.Editing` let you update paints, paths, and command graphs either in place or with clone-on-write behavior. That makes the package useful outside SVG scenarios as well.

## Relationship to other packages

- [Svg.Model](svg-model) produces `ShimSkiaSharp` pictures and drawables from SVG input.
- [Svg.Skia](svg-skia) converts `ShimSkiaSharp` objects into live `SkiaSharp` objects for rendering.
- [Svg.CodeGen.Skia](svg-codegen-skia) emits C# source from `ShimSkiaSharp.SKPicture`.

## When not to choose `ShimSkiaSharp`

- Choose [Svg.Skia](svg-skia) when you want to draw or export immediately.
- Choose [Svg.Model](svg-model) when you need SVG-aware services in addition to the command model.
- Choose [Skia.Controls.Avalonia](skia-controls-avalonia) when the goal is UI presentation of live SkiaSharp content in Avalonia.

## Related docs

- [Picture Model and Rebuild](../concepts/picture-model-and-rebuild)
- [Svg.Model](svg-model)
- [Svg.CodeGen.Skia](svg-codegen-skia)
