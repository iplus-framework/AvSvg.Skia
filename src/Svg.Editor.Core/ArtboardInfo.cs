using Svg;

namespace Svg.Editor.Core;

public class ArtboardInfo
{
    public SvgGroup Group { get; }
    public string Name { get; }
    public float Width { get; }
    public float Height { get; }

    public ArtboardInfo(SvgGroup group, string name, float width, float height)
    {
        Group = group;
        Name = name;
        Width = width;
        Height = height;
    }

    public override string ToString() => Name;
}
