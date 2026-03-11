# Svg.Editor package layout

`samples/AvalonDraw` is now the demo host for the extracted editor stack.

Reference `Svg.Editor.Skia.Avalonia` when you want the ready-made editor workspace and interactive canvas.
It now exposes `SvgEditorWorkspace` for the full composed editor and `SvgEditorSurface` for the canvas only.

Reference `Svg.Editor.Avalonia` when you only need the editor dialogs and other Avalonia-specific UI pieces without the Skia editing surface.
It now contains the reusable side-panel controls: `DocumentOutlineView`, `ResourceBrowserView`, `PropertyInspectorView`, `ToolPaletteView`, and `StatusBarView`.
It also exposes the standalone editor views used by the default dialog host, including `InsertElementPickerView`, `PatternEditorView`, `GradientStopsEditorView`, `GradientMeshEditorView`, `StrokeProfileEditorView`, `TextEditorView`, `PathSegmentsEditorView`, `SwatchEditorView`, `SymbolPickerView`, `SymbolNameEditorView`, and `SettingsEditorView`.
Use `ISvgEditorDialogService` and `ISvgEditorFileDialogService` to replace the default window/file-picker flow when embedding the editor into a host application.

Reference `Svg.Editor.Skia` when you need rendering/editing helpers and overlay logic but are composing your own UI.
It exposes the public editor-side helpers `SvgEditorOverlayRenderer`, `SvgEditorInteractionController`, `BoundsInfo`, and `PathPoint`.

Reference `Svg.Editor.Svg` for SVG document mutation services, resource management, and property editing support.

Reference `Svg.Editor.Core` for shared editor state types such as `SvgEditorSession`, `SvgEditorSettings`, `SvgNode`, and `ArtboardInfo`.

`SvgEditorWorkspace` now keeps host-owned seams public:
- assign `DialogService` to customize modal presentation and inline editor hosting
- assign `FileDialogService` to customize open/save/export/image picking
- call `OpenDocumentAsync`, `SaveDocumentAsync`, `ExportSelectedElementAsync`, `ExportPdfAsync`, `ExportXpsAsync`, `PlaceImageAsync`, and `ShowSettingsAsync` from host menus or commands instead of relying on the built-in menu bar
