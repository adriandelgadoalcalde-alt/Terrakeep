namespace Terrakeep.Core.Model;

public readonly record struct RgbaColor(byte R, byte G, byte B, byte A)
{
    public static readonly RgbaColor Transparent = new(0, 0, 0, 0);
}
