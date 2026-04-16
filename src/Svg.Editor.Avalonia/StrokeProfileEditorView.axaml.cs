using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Svg.Editor.Avalonia.Models;
using Svg.Editor.Svg.Models;

namespace Svg.Editor.Avalonia;

public partial class StrokeProfileEditorView : SvgEditorDialogViewBase<StrokeProfileEditorResult>
{
    private readonly DataGrid _grid;
    private readonly ObservableCollection<StrokePointInfo> _points = new();

    public StrokeProfileEditorView()
    {
        InitializeComponent();
        _grid = this.FindControl<DataGrid>("PointsGrid")!;
        _grid.ItemsSource = _points;
    }

    public void SetPoints(System.Collections.Generic.IEnumerable<StrokePointInfo> points)
    {
        _points.Clear();
        foreach (var point in points.Select(p => new StrokePointInfo { Offset = p.Offset, Width = p.Width }))
            _points.Add(point);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _points.Add(new StrokePointInfo { Offset = 0.0, Width = 1.0 });
    }

    private void RemoveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_grid.SelectedItem is StrokePointInfo info)
            _points.Remove(info);
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Accept(new StrokeProfileEditorResult(_points
            .Select(point => new StrokePointInfo { Offset = point.Offset, Width = point.Width })
            .ToList()));
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Cancel();
    }
}
