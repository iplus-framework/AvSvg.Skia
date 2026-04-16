using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Svg;
using Svg.Editor.Avalonia;
using Xunit;

namespace Svg.Editor.Skia.Avalonia.UnitTests;

public class EditorViewsTests
{
    [AvaloniaFact]
    public void InsertElementPickerView_AssignsItemsSource()
    {
        var view = new InsertElementPickerView
        {
            ElementNames = new[] { "rect", "circle", "path" }
        };

        var box = view.FindControl<AutoCompleteBox>("ElementList");

        Assert.NotNull(box);
        Assert.NotNull(box.ItemsSource);
    }

    [AvaloniaFact]
    public void SettingsEditorView_InitializesValues()
    {
        var view = new SettingsEditorView();

        view.Initialize(snapToGrid: true, gridSize: 24, showGrid: true, includeHidden: true);

        Assert.True(view.FindControl<CheckBox>("SnapCheckBox")!.IsChecked);
        Assert.True(view.FindControl<CheckBox>("GridCheckBox")!.IsChecked);
        Assert.True(view.FindControl<CheckBox>("HiddenSelectCheckBox")!.IsChecked);
        Assert.Equal("24", view.FindControl<TextBox>("GridSizeBox")!.Text);
    }

    [AvaloniaFact]
    public void TextEditorView_InitializesEditorFields()
    {
        var view = new TextEditorView();

        view.Initialize("Hello", "Arial", SvgFontWeight.Bold, 1.5f, 2.5f);

        Assert.Equal("Hello", view.FindControl<TextBox>("Editor")!.Text);
        Assert.Equal("Arial", view.FindControl<ComboBox>("FontFamilyBox")!.SelectedItem);
        Assert.NotNull(view.FindControl<ComboBox>("FontWeightBox")!.SelectedItem);
    }
}
