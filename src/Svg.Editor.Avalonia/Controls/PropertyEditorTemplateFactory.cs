using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Svg;
using Svg.Editor.Avalonia.Converters;
using Svg.Editor.Svg.Models;
using AColor = Avalonia.Media.Color;
using SK = SkiaSharp;

namespace Svg.Editor.Avalonia.Controls;

internal static class PropertyEditorTemplateFactory
{
    public static FuncDataTemplate<PropertyEntry> Create(PropertyInspectorView owner)
    {
        return new FuncDataTemplate<PropertyEntry>((entry, _) =>
        {
            if (entry.Property?.Name == nameof(SvgVisualElement.Fill))
            {
                var items = new ObservableCollection<string>(owner.GetPatternReferences());
                if (owner.CreatePatternReferenceAsync is not null)
                    items.Insert(0, "New Pattern");

                var box = new AutoCompleteBox
                {
                    ItemsSource = items,
                    MinimumPrefixLength = 0,
                    VerticalAlignment = VerticalAlignment.Center
                };
                box[!AutoCompleteBox.TextProperty] = new Binding(nameof(PropertyEntry.Value)) { Mode = BindingMode.TwoWay };
                box.GotFocus += (_, _) => box.IsDropDownOpen = true;
                box.SelectionChanged += async (_, _) =>
                {
                    if (box.SelectedItem as string != "New Pattern" || owner.CreatePatternReferenceAsync is null)
                        return;

                    var reference = await owner.CreatePatternReferenceAsync();
                    if (string.IsNullOrWhiteSpace(reference))
                        return;

                    if (!items.Contains(reference))
                        items.Add(reference);

                    box.SelectedItem = reference;
                    box.Text = reference;
                };
                return box;
            }

            if (entry.Options is { } options)
            {
                return CreateAutoCompleteBox(options);
            }

            if (entry.Suggestions is { } suggestions)
            {
                return CreateAutoCompleteBox(suggestions);
            }

            if (entry.Property?.PropertyType == typeof(bool))
            {
                var checkBox = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
                checkBox[!ToggleButton.IsCheckedProperty] = new Binding(nameof(PropertyEntry.Value))
                {
                    Mode = BindingMode.TwoWay,
                    Converter = new BooleanStringConverter()
                };
                return checkBox;
            }

            if (entry.Property?.PropertyType == typeof(SK.SKColor) || entry.Property?.PropertyType == typeof(AColor))
            {
                var picker = new ColorPicker { Width = 120, VerticalAlignment = VerticalAlignment.Center };
                picker[!ColorPicker.ColorProperty] = new Binding(nameof(PropertyEntry.Value))
                {
                    Mode = BindingMode.TwoWay,
                    Converter = new ColorStringConverter()
                };
                return picker;
            }

            if (entry is GradientMeshEntry gradientMeshEntry)
            {
                var button = new Button { Content = "Edit Mesh", VerticalAlignment = VerticalAlignment.Center };
                button.Click += async (_, _) =>
                {
                    if (owner.EditGradientMeshAsync is not null)
                        await owner.EditGradientMeshAsync(gradientMeshEntry);
                };
                return button;
            }

            if (entry is GradientStopsEntry gradientStopsEntry)
            {
                var button = new Button { Content = entry.Value ?? "Edit", VerticalAlignment = VerticalAlignment.Center };
                button.Click += async (_, _) =>
                {
                    if (owner.EditGradientStopsAsync is null)
                        return;

                    var result = await owner.EditGradientStopsAsync(gradientStopsEntry);
                    if (result is null)
                        return;

                    gradientStopsEntry.Stops.Clear();
                    foreach (var stop in result)
                        gradientStopsEntry.Stops.Add(new GradientStopInfo { Offset = stop.Offset, Color = stop.Color });
                    gradientStopsEntry.UpdateValue();
                    gradientStopsEntry.NotifyChanged();
                };
                return button;
            }

            if (entry is StrokeProfileEntry strokeProfileEntry)
            {
                var button = new Button { Content = entry.Value ?? "Edit", VerticalAlignment = VerticalAlignment.Center };
                button.Click += async (_, _) =>
                {
                    if (owner.EditStrokeProfileAsync is null)
                        return;

                    var result = await owner.EditStrokeProfileAsync(strokeProfileEntry);
                    if (result is null)
                        return;

                    strokeProfileEntry.Points.Clear();
                    foreach (var point in result)
                        strokeProfileEntry.Points.Add(new StrokePointInfo { Offset = point.Offset, Width = point.Width });
                    strokeProfileEntry.UpdateValue();
                    strokeProfileEntry.NotifyChanged();
                };
                return button;
            }

            var textBox = new TextBox { VerticalContentAlignment = VerticalAlignment.Center };
            textBox[!TextBox.TextProperty] = new Binding(nameof(PropertyEntry.Value)) { Mode = BindingMode.TwoWay };
            return textBox;
        }, true);
    }

    private static AutoCompleteBox CreateAutoCompleteBox(IEnumerable<string> itemsSource)
    {
        var box = new AutoCompleteBox
        {
            ItemsSource = itemsSource.ToList(),
            MinimumPrefixLength = 0,
            VerticalAlignment = VerticalAlignment.Center
        };
        box[!AutoCompleteBox.TextProperty] = new Binding(nameof(PropertyEntry.Value)) { Mode = BindingMode.TwoWay };
        box.GotFocus += (_, _) => box.IsDropDownOpen = true;
        return box;
    }
}
