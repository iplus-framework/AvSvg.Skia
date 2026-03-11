using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Svg.Editor.Avalonia.Models;

namespace Svg.Editor.Avalonia;

public partial class SwatchEditorView : SvgEditorDialogViewBase<SwatchEditorResult>
{
    private readonly ColorPicker _picker;

    public SwatchEditorView()
    {
        InitializeComponent();
        _picker = this.FindControl<ColorPicker>("Picker")!;
    }

    public string SelectedColor
    {
        get
        {
            var color = _picker.Color;
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        set => _picker.Color = Color.Parse(value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Accept(new SwatchEditorResult(SelectedColor));
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Cancel();
    }
}
