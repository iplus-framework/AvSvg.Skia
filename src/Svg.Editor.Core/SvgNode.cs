using System.Collections.ObjectModel;
using System.ComponentModel;
using Svg;

namespace Svg.Editor.Core;

public class SvgNode : INotifyPropertyChanged
{
    public SvgElement Element { get; }
    public ObservableCollection<SvgNode> Children { get; } = new();
    public SvgNode? Parent { get; }
    public string Label { get; }
    private bool _isVisible;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
                return;

            _isVisible = value;
            Element.Visibility = value ? "visible" : "hidden";
            Element.Display = value ? "inline" : "none";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
        }
    }

    public SvgNode(SvgElement element, SvgNode? parent)
    {
        Element = element;
        Parent = parent;
        var name = SvgElementInfo.GetElementName(element.GetType());
        Label = string.IsNullOrEmpty(element.ID)
            ? name
            : $"{name} ({element.ID})";
        _isVisible = SvgElementInfo.IsVisible(element);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
