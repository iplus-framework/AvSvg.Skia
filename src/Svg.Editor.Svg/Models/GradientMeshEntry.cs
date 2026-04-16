using System;
using Svg.Model;

namespace Svg.Editor.Svg.Models;

public class GradientMeshEntry : PropertyEntry
{
    public GradientMesh Mesh { get; }

    public GradientMeshEntry(GradientMesh mesh)
        : base("Mesh", string.Empty, (_, __) => { })
    {
        Mesh = mesh;
    }

    public override void Apply(object target)
    {
        // Placeholder for applying the mesh to an object.
    }
}
