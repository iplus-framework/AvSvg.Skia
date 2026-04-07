using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Svg.Editor.Avalonia.Models;

namespace Svg.Editor.Avalonia;

public partial class SymbolNameEditorView : SvgEditorDialogViewBase<SymbolNameResult>
{
    private readonly TextBox _nameBox;

    public SymbolNameEditorView()
    {
        InitializeComponent();
        _nameBox = this.FindControl<TextBox>("NameBox")!;
    }

    public string? SymbolName
    {
        get => _nameBox.Text;
        set => _nameBox.Text = value;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(SymbolName))
            Accept(new SymbolNameResult(SymbolName!));
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Cancel();
    }
}
