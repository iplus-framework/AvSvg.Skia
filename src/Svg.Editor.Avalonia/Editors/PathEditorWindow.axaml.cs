using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Svg;
using Svg.Editor.Avalonia.Models;
using Svg.Pathing;

namespace Svg.Editor.Avalonia;

public partial class PathEditorWindow : Window
{
    public PathEditorWindow()
        : this(new SvgPath { PathData = new SvgPathSegmentList() })
    {
    }

    public PathEditorWindow(SvgPath path)
    {
        InitializeComponent();
        EditorView.SetPath(path);
        EditorView.Accepted += EditorView_OnAccepted;
        EditorView.Cancelled += EditorView_OnCancelled;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void EditorView_OnAccepted(object? sender, PathEditorResult result)
    {
        Close(result);
    }

    private void EditorView_OnCancelled(object? sender, System.EventArgs e)
    {
        Close(null);
    }
}
