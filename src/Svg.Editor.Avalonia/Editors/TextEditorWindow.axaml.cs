using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Svg;
using Svg.Editor.Avalonia.Models;

namespace Svg.Editor.Avalonia;

public partial class TextEditorWindow : Window
{
    public TextEditorWindow()
        : this(string.Empty, string.Empty, SvgFontWeight.Normal, 0f, 0f)
    {
    }

    public TextEditorWindow(string text, string fontFamily, SvgFontWeight weight, float letter, float word)
    {
        InitializeComponent();
        EditorView.Initialize(text, fontFamily, weight, letter, word);
        EditorView.Accepted += EditorView_OnAccepted;
        EditorView.Cancelled += EditorView_OnCancelled;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void EditorView_OnAccepted(object? sender, TextEditorResult result)
    {
        Close(result);
    }

    private void EditorView_OnCancelled(object? sender, System.EventArgs e)
    {
        Close(null);
    }
}
