using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Svg;
using Svg.Editor.Avalonia.Models;
using Svg.Pathing;

namespace Svg.Editor.Avalonia;

public partial class PatternEditorView : SvgEditorDialogViewBase<PatternEditorResult>
{
    private readonly TextBox _widthBox;
    private readonly TextBox _heightBox;
    private readonly TextBox _pathBox;

    public PatternEditorView()
    {
        InitializeComponent();
        _widthBox = this.FindControl<TextBox>("WidthBox")!;
        _heightBox = this.FindControl<TextBox>("HeightBox")!;
        _pathBox = this.FindControl<TextBox>("PathBox")!;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        float.TryParse(_widthBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var width);
        float.TryParse(_heightBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var height);
        if (width <= 0)
            width = 10f;
        if (height <= 0)
            height = 10f;

        var pattern = new SvgPatternServer
        {
            Width = new SvgUnit(width),
            Height = new SvgUnit(height)
        };

        if (!string.IsNullOrWhiteSpace(_pathBox.Text))
        {
            var path = new SvgPath
            {
                PathData = SvgPathBuilder.Parse(_pathBox.Text),
                Fill = new SvgColourServer(System.Drawing.Color.Black)
            };
            pattern.Children.Add(path);
        }

        Accept(new PatternEditorResult(pattern));
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Cancel();
    }
}
