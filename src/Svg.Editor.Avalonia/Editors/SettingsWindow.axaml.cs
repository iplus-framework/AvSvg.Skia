using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Svg.Editor.Avalonia.Models;

namespace Svg.Editor.Avalonia;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
        : this(false, 10.0, false, false)
    {
    }

    public SettingsWindow(bool snapToGrid, double gridSize, bool showGrid, bool includeHidden)
    {
        InitializeComponent();
        EditorView.Initialize(snapToGrid, gridSize, showGrid, includeHidden);
        EditorView.Accepted += EditorView_OnAccepted;
        EditorView.Cancelled += EditorView_OnCancelled;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void EditorView_OnAccepted(object? sender, SettingsEditorResult result)
    {
        Close(result);
    }

    private void EditorView_OnCancelled(object? sender, System.EventArgs e)
    {
        Close(null);
    }
}
