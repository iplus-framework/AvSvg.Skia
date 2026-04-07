using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Svg;
using Svg.Editor.Avalonia.Models;

namespace Svg.Editor.Avalonia;

public partial class PathSegmentsEditorView : SvgEditorDialogViewBase<PathEditorResult>
{
    public class SegmentEntry
    {
        public int Index { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    private readonly DataGrid _grid;
    private readonly ObservableCollection<SegmentEntry> _entries = new();

    public PathSegmentsEditorView()
    {
        InitializeComponent();
        _grid = this.FindControl<DataGrid>("SegmentsGrid")!;
        _grid.ItemsSource = _entries;
    }

    public void SetPath(SvgPath path)
    {
        _entries.Clear();
        var index = 0;
        foreach (var segment in path.PathData)
            _entries.Add(new SegmentEntry { Index = index++, Text = segment.ToString() ?? string.Empty });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Accept(new PathEditorResult(string.Join(" ", _entries.Select(entry => entry.Text))));
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Cancel();
    }
}
