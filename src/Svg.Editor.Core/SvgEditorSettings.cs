using System.ComponentModel;

namespace Svg.Editor.Core;

public class SvgEditorSettings : INotifyPropertyChanged
{
    private bool _wireframeEnabled;
    private bool _filtersDisabled;
    private bool _snapToGrid;
    private bool _showGrid;
    private double _gridSize = 10.0;
    private bool _includeHidden;

    public bool WireframeEnabled
    {
        get => _wireframeEnabled;
        set => SetField(ref _wireframeEnabled, value, nameof(WireframeEnabled));
    }

    public bool FiltersDisabled
    {
        get => _filtersDisabled;
        set => SetField(ref _filtersDisabled, value, nameof(FiltersDisabled));
    }

    public bool SnapToGrid
    {
        get => _snapToGrid;
        set => SetField(ref _snapToGrid, value, nameof(SnapToGrid));
    }

    public bool ShowGrid
    {
        get => _showGrid;
        set => SetField(ref _showGrid, value, nameof(ShowGrid));
    }

    public double GridSize
    {
        get => _gridSize;
        set => SetField(ref _gridSize, value, nameof(GridSize));
    }

    public bool IncludeHidden
    {
        get => _includeHidden;
        set => SetField(ref _includeHidden, value, nameof(IncludeHidden));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, string propertyName)
    {
        if (Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
