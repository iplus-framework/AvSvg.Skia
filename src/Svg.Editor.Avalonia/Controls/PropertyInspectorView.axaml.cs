using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Svg.Editor.Svg.Models;

namespace Svg.Editor.Avalonia.Controls;

public partial class PropertyInspectorView : UserControl
{
    public static readonly DirectProperty<PropertyInspectorView, ObservableCollection<PropertyEntry>?> FilteredPropertiesProperty =
        AvaloniaProperty.RegisterDirect<PropertyInspectorView, ObservableCollection<PropertyEntry>?>(
            nameof(FilteredProperties),
            view => view.FilteredProperties,
            (view, value) => view.FilteredProperties = value);

    private ObservableCollection<PropertyEntry>? _filteredProperties;

    public ObservableCollection<PropertyEntry>? FilteredProperties
    {
        get => _filteredProperties;
        set
        {
            SetAndRaise(FilteredPropertiesProperty, ref _filteredProperties, value);
            if (PropertiesGridControl is { })
                PropertiesGridControl.ItemsSource = value;
        }
    }

    public Func<IEnumerable<string>>? PatternReferenceProvider { get; set; }
    public Func<Task<string?>>? CreatePatternReferenceAsync { get; set; }
    public Func<GradientStopsEntry, Task<IReadOnlyList<GradientStopInfo>?>>? EditGradientStopsAsync { get; set; }
    public Func<GradientMeshEntry, Task>? EditGradientMeshAsync { get; set; }
    public Func<StrokeProfileEntry, Task<IReadOnlyList<StrokePointInfo>?>>? EditStrokeProfileAsync { get; set; }

    public TextBox FilterTextBox => FilterTextBoxControl;
    public Button ApplyButton => ApplyButtonControl;
    public DataGrid PropertiesGrid => PropertiesGridControl;

    public PropertyInspectorView()
    {
        Resources["PropertyEditorTemplate"] = PropertyEditorTemplateFactory.Create(this);
        InitializeComponent();
        PropertiesGridControl.ItemsSource = _filteredProperties;
    }

    internal IEnumerable<string> GetPatternReferences()
        => PatternReferenceProvider?.Invoke() ?? Array.Empty<string>();
}
