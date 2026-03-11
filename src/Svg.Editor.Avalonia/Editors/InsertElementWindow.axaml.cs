using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Svg.Editor.Avalonia.Models;

namespace Svg.Editor.Avalonia;

public partial class InsertElementWindow : Window
{
    public InsertElementWindow()
        : this(Array.Empty<string>())
    {
    }

    public InsertElementWindow(IEnumerable<string> names)
    {
        InitializeComponent();
        EditorView.ElementNames = names;
        EditorView.Accepted += EditorView_OnAccepted;
        EditorView.Cancelled += EditorView_OnCancelled;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void EditorView_OnAccepted(object? sender, InsertElementResult result)
    {
        Close(result);
    }

    private void EditorView_OnCancelled(object? sender, EventArgs e)
    {
        Close(null);
    }
}
