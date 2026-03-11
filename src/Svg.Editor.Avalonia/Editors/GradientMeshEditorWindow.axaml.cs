using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Svg.Editor.Avalonia.Models;
using Svg.Model;

namespace Svg.Editor.Avalonia;

public partial class GradientMeshEditorWindow : Window
{
    public GradientMeshEditorWindow()
        : this(new GradientMesh())
    {
    }

    public GradientMeshEditorWindow(GradientMesh mesh)
    {
        InitializeComponent();
        EditorView.SetMesh(mesh);
        EditorView.Accepted += EditorView_OnAccepted;
        EditorView.Cancelled += EditorView_OnCancelled;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void EditorView_OnAccepted(object? sender, GradientMeshEditorResult result)
    {
        Close(result);
    }

    private void EditorView_OnCancelled(object? sender, System.EventArgs e)
    {
        Close(null);
    }
}
