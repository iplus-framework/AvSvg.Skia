using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Svg.Editor.Avalonia.Converters;
using Svg.Editor.Avalonia.Models;
using Svg.Editor.Svg.Models;

namespace Svg.Editor.Avalonia;

public partial class GradientStopsEditorView : SvgEditorDialogViewBase<GradientStopsEditorResult>
{
    private readonly DataGrid _grid;
    private readonly ObservableCollection<GradientStopInfo> _stops = new();

    public GradientStopsEditorView()
    {
        InitializeComponent();
        Resources["ColorStringConverter"] = new ColorStringConverter();
        _grid = this.FindControl<DataGrid>("StopsGrid")!;
        _grid.ItemsSource = _stops;
    }

    public void SetStops(System.Collections.Generic.IEnumerable<GradientStopInfo> stops)
    {
        _stops.Clear();
        foreach (var stop in stops.Select(s => new GradientStopInfo { Offset = s.Offset, Color = s.Color }))
            _stops.Add(stop);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _stops.Add(new GradientStopInfo { Offset = 0.0, Color = "#000000" });
    }

    private void RemoveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_grid.SelectedItem is GradientStopInfo info)
            _stops.Remove(info);
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Accept(new GradientStopsEditorResult(_stops
            .Select(stop => new GradientStopInfo { Offset = stop.Offset, Color = stop.Color })
            .ToList()));
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Cancel();
    }
}
