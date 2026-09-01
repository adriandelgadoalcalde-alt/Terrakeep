namespace TerrasavrNative.Core.WldFormat;

public sealed class WldNpc
{
    public required int Id { get; init; }
    public required string GivenName { get; init; }
    public required int TileX { get; init; }
    public required int TileY { get; init; }
    public required bool Homeless { get; init; }
}
