using System;
using System.Linq;
using Svg;

namespace Svg.Editor.Core;

public static class SvgElementInfo
{
    public static string GetElementName(Type type)
    {
        var attr = type.GetCustomAttributes(typeof(SvgElementAttribute), true)
            .OfType<SvgElementAttribute>()
            .FirstOrDefault(a => !string.IsNullOrEmpty(a.ElementName));
        return attr?.ElementName ?? type.Name;
    }

    public static bool IsVisible(SvgElement element)
    {
        var vis = !string.Equals(element.Visibility, "hidden", StringComparison.OrdinalIgnoreCase) &&
                  !string.Equals(element.Visibility, "collapse", StringComparison.OrdinalIgnoreCase);
        var disp = !string.Equals(element.Display, "none", StringComparison.OrdinalIgnoreCase);
        return vis && disp;
    }
}
