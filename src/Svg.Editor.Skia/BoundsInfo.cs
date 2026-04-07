using SK = SkiaSharp;

namespace Svg.Editor.Skia;

public struct BoundsInfo
{
    public SK.SKPoint TL { get; }
    public SK.SKPoint TR { get; }
    public SK.SKPoint BR { get; }
    public SK.SKPoint BL { get; }
    public SK.SKPoint TopMid { get; }
    public SK.SKPoint RightMid { get; }
    public SK.SKPoint BottomMid { get; }
    public SK.SKPoint LeftMid { get; }
    public SK.SKPoint Center { get; }
    public SK.SKPoint RotHandle { get; }

    public BoundsInfo(
        SK.SKPoint tl,
        SK.SKPoint tr,
        SK.SKPoint br,
        SK.SKPoint bl,
        SK.SKPoint topMid,
        SK.SKPoint rightMid,
        SK.SKPoint bottomMid,
        SK.SKPoint leftMid,
        SK.SKPoint center,
        SK.SKPoint rotHandle)
    {
        TL = tl;
        TR = tr;
        BR = br;
        BL = bl;
        TopMid = topMid;
        RightMid = rightMid;
        BottomMid = bottomMid;
        LeftMid = leftMid;
        Center = center;
        RotHandle = rotHandle;
    }
}
