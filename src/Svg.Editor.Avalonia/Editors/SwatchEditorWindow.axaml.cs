using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Svg.Editor.Avalonia.Models;

namespace Svg.Editor.Avalonia;

public partial class SwatchEditorWindow : Window
{
    public SwatchEditorWindow()
        : this("#000000")
    {
    }

    public SwatchEditorWindow(string color)
    {
        InitializeComponent();
        EditorView.SelectedColor = color;
        EditorView.Accepted += EditorView_OnAccepted;
        EditorView.Cancelled += EditorView_OnCancelled;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void EditorView_OnAccepted(object? sender, SwatchEditorResult result)
    {
        Close(result);
    }

    private void EditorView_OnCancelled(object? sender, System.EventArgs e)
    {
        Close(null);
    }
}
