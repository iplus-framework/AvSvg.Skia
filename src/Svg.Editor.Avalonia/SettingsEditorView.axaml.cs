using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Svg.Editor.Avalonia.Models;

namespace Svg.Editor.Avalonia;

public partial class SettingsEditorView : SvgEditorDialogViewBase<SettingsEditorResult>
{
    private readonly CheckBox _snapCheckBox;
    private readonly CheckBox _gridCheckBox;
    private readonly CheckBox _hiddenSelectCheckBox;
    private readonly TextBox _gridSizeBox;

    public SettingsEditorView()
    {
        InitializeComponent();
        _snapCheckBox = this.FindControl<CheckBox>("SnapCheckBox")!;
        _gridCheckBox = this.FindControl<CheckBox>("GridCheckBox")!;
        _hiddenSelectCheckBox = this.FindControl<CheckBox>("HiddenSelectCheckBox")!;
        _gridSizeBox = this.FindControl<TextBox>("GridSizeBox")!;
    }

    public void Initialize(bool snapToGrid, double gridSize, bool showGrid, bool includeHidden)
    {
        _snapCheckBox.IsChecked = snapToGrid;
        _gridCheckBox.IsChecked = showGrid;
        _hiddenSelectCheckBox.IsChecked = includeHidden;
        _gridSizeBox.Text = gridSize.ToString(CultureInfo.InvariantCulture);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!double.TryParse(_gridSizeBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var gridSize))
            gridSize = 1.0;

        Accept(new SettingsEditorResult(
            _snapCheckBox.IsChecked ?? false,
            gridSize,
            _gridCheckBox.IsChecked ?? false,
            _hiddenSelectCheckBox.IsChecked ?? false));
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Cancel();
    }
}
