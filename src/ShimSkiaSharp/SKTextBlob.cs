// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
using System;

namespace ShimSkiaSharp;

public sealed class SKTextBlob : ICloneable, IDeepCloneable<SKTextBlob>
{
    public string? Text { get; private set; }
    public ushort[]? Glyphs { get; private set; }
    public SKPoint[]? Points { get; private set; }

    private SKTextBlob()
    {
    }

    public static SKTextBlob CreatePositioned(string? text, SKPoint[]? points)
        => new() { Text = text, Points = points };

    public static SKTextBlob CreatePositionedGlyphs(ushort[]? glyphs, SKPoint[]? points)
        => new() { Glyphs = glyphs, Points = points };

    public SKTextBlob Clone() => DeepClone(new CloneContext());

    public SKTextBlob DeepClone() => Clone();

    object ICloneable.Clone() => Clone();

    internal SKTextBlob DeepClone(CloneContext context)
    {
        if (context.TryGet(this, out SKTextBlob existing))
        {
            return existing;
        }

        var clone = new SKTextBlob();
        context.Add(this, clone);

        clone.Text = Text;
        clone.Glyphs = CloneHelpers.CloneArray(Glyphs, context);
        clone.Points = CloneHelpers.CloneArray(Points, context);

        return clone;
    }
}
