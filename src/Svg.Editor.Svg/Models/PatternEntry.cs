using Svg;

namespace Svg.Editor.Svg.Models;

public class PatternEntry
{
    public SvgPatternServer Pattern { get; }
    public string Name { get; }

    public PatternEntry(SvgPatternServer pattern, string name)
    {
        Pattern = pattern;
        Name = name;
    }

    public override string ToString() => Name;
}
