namespace Svg.Editor.Skia.Avalonia;

public class SvgEditorSurface : global::Avalonia.Svg.Skia.Svg
{
    public global::Svg.Editor.Skia.SvgEditorOverlayRenderer OverlayRenderer { get; set; }

    public global::Svg.Editor.Skia.SvgEditorInteractionController InteractionController { get; set; }

    public SvgEditorSurface()
        : base(new global::System.Uri("avares://Svg.Editor.Skia.Avalonia/"))
    {
        OverlayRenderer = new global::Svg.Editor.Skia.SvgEditorOverlayRenderer();
        InteractionController = new global::Svg.Editor.Skia.SvgEditorInteractionController();
    }
}
