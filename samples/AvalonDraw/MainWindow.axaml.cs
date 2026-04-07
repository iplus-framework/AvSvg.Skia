using System.Threading.Tasks;
using Avalonia.Controls;
using Svg;
using Svg.Editor.Avalonia;
using Svg.Editor.Skia.Avalonia;

namespace AvalonDraw;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        EditorWorkspace.WorkspaceTitlePrefix = "AvalonDraw";
        EditorWorkspace.DialogService = new SvgEditorDialogService();
        EditorWorkspace.FileDialogService = new SvgEditorFileDialogService();
        EditorWorkspace.PreviewRequested = ShowPreviewAsync;
        EditorWorkspace.WorkspaceTitleChanged += (_, title) => Title = title;
        EditorWorkspace.LoadDocument("Assets/__tiger.svg");
        Title = EditorWorkspace.WorkspaceTitle;
    }

    private async Task ShowPreviewAsync(SvgDocument document)
    {
        var preview = new PreviewWindow(document);
        await preview.ShowDialog(this);
    }
}
