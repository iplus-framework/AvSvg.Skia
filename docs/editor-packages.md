# Svg.Editor package layout

The canonical end-user documentation for the extracted editor stack now lives in the Lunet site:

- `site/articles/editor/` for architecture and integration guides
- `site/articles/packages/svg-editor-*.md` for package-by-package coverage
- `site/articles/reference/api-coverage-index.md` for generated API inputs

Use this file as a short maintainer note only.

## Package summary

- `Svg.Editor.Core`: shared session, settings, outline, artboard, clipboard, and history state
- `Svg.Editor.Svg`: document mutation services and editor-side SVG/resource models
- `Svg.Editor.Skia`: selection math, path editing, align/distribute, and overlay rendering
- `Svg.Editor.Avalonia`: reusable Avalonia panels, editor views, and dialog abstractions
- `Svg.Editor.Skia.Avalonia`: `SvgEditorSurface` and `SvgEditorWorkspace`

## Host-owned seams on `SvgEditorWorkspace`

- `DialogService`
- `FileDialogService`
- `PreviewRequested`
- `ResourceAssembly`
- `WorkspaceTitlePrefix`

Public host command entry points remain:

- `LoadDocument`
- `OpenDocumentAsync`
- `SaveDocumentAsync`
- `ExportSelectedElementAsync`
- `ExportPdfAsync`
- `ExportXpsAsync`
- `PlaceImageAsync`
- `ShowSettingsAsync`
