---
title: "Architecture"
---

# Architecture

The editor stack is layered so applications can pick the lowest level that still solves the problem.

## Dependency order

| Layer | Depends on | Main responsibility |
| --- | --- | --- |
| `Svg.Editor.Core` | `Svg.Model` | Host-agnostic state such as the active document, outline tree, selected ids, settings, clipboard, and undo/redo history |
| `Svg.Editor.Svg` | `Svg.Editor.Core`, `Svg.Model`, `Svg.Skia` | SVG document loading, save/export helpers, properties, layers, patterns, swatches, symbols, styles, and creation tools |
| `Svg.Editor.Skia` | `Svg.Editor.Core`, `Svg.Editor.Svg`, `Svg.Model`, `Svg.Skia` | Bounds math, hit handles, transforms, path editing, align/distribute, and overlay rendering |
| `Svg.Editor.Avalonia` | `Svg.Editor.Core`, `Svg.Editor.Svg` | Reusable Avalonia panels, property editors, standalone dialog views, and dialog/file-dialog service abstractions |
| `Svg.Editor.Skia.Avalonia` | `Svg.Editor.Avalonia`, `Svg.Editor.Core`, `Svg.Editor.Skia`, `Svg.Editor.Svg`, `Svg.Controls.Skia.Avalonia` | The interactive `SvgEditorSurface` and the default `SvgEditorWorkspace` composition |

## Package responsibilities

### `Svg.Editor.Core`

Use this layer when your host already has its own UI and needs a durable editor session object. `SvgEditorSession` stores the document reference, current file, title, filter text, selected ids, artboards, outline nodes, settings, clipboard contents, and undo/redo snapshots.

### `Svg.Editor.Svg`

This layer owns document mutation and resource-oriented models. It is the package behind the property inspector, layer browser, pattern list, swatches, symbols, and styles. It also carries `SvgDocumentService` for open, save, XML round-tripping, and export helpers.

### `Svg.Editor.Skia`

This layer owns the editing math. `SelectionService` and `PathService` handle transforms and editable path points, `AlignService` handles arrangement commands, and `RenderingService` draws the editor-only overlays. `SvgEditorInteractionController` and `SvgEditorOverlayRenderer` are the public adapter types meant for hosts.

### `Svg.Editor.Avalonia`

This layer exposes reusable controls and standalone views rather than a monolithic window. The main reusable panels are `DocumentOutlineView`, `ResourceBrowserView`, `PropertyInspectorView`, `ToolPaletteView`, and `StatusBarView`. Dialog workflows are abstracted through `ISvgEditorDialogService` and `ISvgEditorFileDialogService`.

### `Svg.Editor.Skia.Avalonia`

This is the highest-level package. `SvgEditorSurface` extends `Avalonia.Svg.Skia.Svg` with public overlay and interaction adapters, and `SvgEditorWorkspace` composes the menu, tool palette, left-side panels, canvas, and property inspector into the same layout used by AvalonDraw.

## Host-owned seams

The reusable workspace is intentionally not the final application shell. Hosts remain responsible for:

- window creation and app branding through `WorkspaceTitlePrefix`,
- default resource lookup via `ResourceAssembly`,
- preview window ownership via `PreviewRequested`,
- modal editors via `DialogService`,
- file picking and save locations via `FileDialogService`.

That design keeps the reusable packages independent from any single desktop app policy.

## Sample-to-package split

`samples/AvalonDraw` is now a thin host that:

- instantiates `SvgEditorWorkspace`,
- assigns the default dialog and file-dialog services,
- sets the title prefix to `AvalonDraw`,
- opens the preview window,
- loads the default `Assets/__tiger.svg` document.

All reusable editor code now lives under `src/Svg.Editor.*`.

## Choosing the smallest useful layer

- Choose `Svg.Editor.Skia.Avalonia` for the complete editor experience inside an Avalonia app.
- Choose `Svg.Editor.Avalonia` when you want the panels and dialogs but not the default canvas/workspace.
- Choose `Svg.Editor.Skia` when you are building your own interactive surface or canvas control.
- Choose `Svg.Editor.Svg` when the task is document/resource mutation without UI.
- Choose `Svg.Editor.Core` when you only need common session state and history primitives.
