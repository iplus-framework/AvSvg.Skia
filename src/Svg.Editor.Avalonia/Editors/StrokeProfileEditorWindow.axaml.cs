using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Svg.Editor.Avalonia.Models;
using Svg.Editor.Svg.Models;

namespace Svg.Editor.Avalonia;

public partial class StrokeProfileEditorWindow : Window
{
    public StrokeProfileEditorWindow()
        : this(new ObservableCollection<StrokePointInfo>())
    {
    }

    public StrokeProfileEditorWindow(ObservableCollection<StrokePointInfo> points)
    {
        InitializeComponent();
        EditorView.SetPoints(points);
        EditorView.Accepted += EditorView_OnAccepted;
        EditorView.Cancelled += EditorView_OnCancelled;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void EditorView_OnAccepted(object? sender, StrokeProfileEditorResult result)
    {
        Close(result);
    }

    private void EditorView_OnCancelled(object? sender, System.EventArgs e)
    {
        Close(null);
    }
}
