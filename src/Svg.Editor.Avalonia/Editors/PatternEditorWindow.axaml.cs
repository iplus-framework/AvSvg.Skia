using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Svg.Editor.Avalonia.Models;

namespace Svg.Editor.Avalonia;

public partial class PatternEditorWindow : Window
{
    public PatternEditorWindow()
    {
        InitializeComponent();
        EditorView.Accepted += EditorView_OnAccepted;
        EditorView.Cancelled += EditorView_OnCancelled;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void EditorView_OnAccepted(object? sender, PatternEditorResult result)
    {
        Close(result);
    }

    private void EditorView_OnCancelled(object? sender, System.EventArgs e)
    {
        Close(null);
    }
}
