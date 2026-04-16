using Svg;

namespace Svg.Editor.Core;

public sealed class ClipboardSnapshot
{
    public SvgElement? Element { get; set; }
    public string? Xml { get; set; }

    public void Clear()
    {
        Element = null;
        Xml = null;
    }
}
