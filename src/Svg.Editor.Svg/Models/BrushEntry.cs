namespace Svg.Editor.Svg.Models;

public class BrushEntry
{
    public StrokeProfile Profile { get; }
    public string Name { get; }

    public BrushEntry(string name, StrokeProfile profile)
    {
        Name = name;
        Profile = profile;
    }

    public override string ToString() => Name;
}
