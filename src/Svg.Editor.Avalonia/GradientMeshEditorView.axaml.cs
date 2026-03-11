using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Svg.Editor.Avalonia.Models;
using Svg.Model;

namespace Svg.Editor.Avalonia;

public partial class GradientMeshEditorView : SvgEditorDialogViewBase<GradientMeshEditorResult>
{
    private readonly Canvas _canvas;
    private readonly ObservableCollection<GradientMeshPoint> _points = new();
    private GradientMeshPoint? _dragging;

    public GradientMeshEditorView()
    {
        InitializeComponent();
        _canvas = this.FindControl<Canvas>("MeshCanvas")!;
        this.FindControl<ItemsControl>("PointsItems")!.ItemsSource = _points;
        _canvas.PointerPressed += OnPointerPressed;
        _canvas.PointerReleased += OnPointerReleased;
        _canvas.PointerMoved += OnPointerMoved;
    }

    public void SetMesh(GradientMesh mesh)
    {
        _points.Clear();
        foreach (var point in mesh.Points)
            _points.Add(point);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var position = e.GetPosition(_canvas);
        foreach (var point in _points)
        {
            var rect = new Rect(point.Position.X - 5, point.Position.Y - 5, 10, 10);
            if (!rect.Contains(position))
                continue;

            _dragging = point;
            break;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = null;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragging is null)
            return;

        var position = e.GetPosition(_canvas);
        var index = _points.IndexOf(_dragging);
        if (index < 0)
            return;

        _points[index] = _dragging with { Position = new ShimSkiaSharp.SKPoint((float)position.X, (float)position.Y) };
        _dragging = _points[index];
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var mesh = new GradientMesh();
        mesh.Points.AddRange(_points);
        Accept(new GradientMeshEditorResult(mesh));
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Cancel();
    }
}
