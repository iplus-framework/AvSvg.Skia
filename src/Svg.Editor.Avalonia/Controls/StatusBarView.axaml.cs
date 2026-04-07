using Avalonia;
using Avalonia.Controls;

namespace Svg.Editor.Avalonia.Controls;

public partial class StatusBarView : UserControl
{
    public static readonly DirectProperty<StatusBarView, string?> StatusTextProperty =
        AvaloniaProperty.RegisterDirect<StatusBarView, string?>(
            nameof(StatusText),
            view => view.StatusText,
            (view, value) => view.StatusText = value);

    private string? _statusText;

    public string? StatusText
    {
        get => _statusText;
        set
        {
            SetAndRaise(StatusTextProperty, ref _statusText, value);
            if (StatusTextBlockControl is { })
                StatusTextBlockControl.Text = value;
        }
    }

    public TextBlock StatusTextBlock => StatusTextBlockControl;
    public Button ResetButton => ResetButtonControl;

    public StatusBarView()
    {
        InitializeComponent();
        StatusTextBlockControl.Text = _statusText;
    }
}
