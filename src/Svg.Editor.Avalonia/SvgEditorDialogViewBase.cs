using System;
using Avalonia.Controls;

namespace Svg.Editor.Avalonia;

public abstract class SvgEditorDialogViewBase<TResult> : UserControl
{
    public event EventHandler<TResult>? Accepted;
    public event EventHandler? Cancelled;

    protected void Accept(TResult result)
        => Accepted?.Invoke(this, result);

    protected void Cancel()
        => Cancelled?.Invoke(this, EventArgs.Empty);
}
