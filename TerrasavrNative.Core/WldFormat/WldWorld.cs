namespace TerrasavrNative.Core.WldFormat;

// Un mundo ya leido: cabecera + rejilla de tiles (indexada [x, y], igual que Main.tile[i,j] en
// el juego real) + NPCs. Solo lectura - nunca se escribe un .wld desde esta app.
public sealed class WldWorld
{
    public required WldHeader Header { get; init; }
    public required WldTile[,] Tiles { get; init; }
    public required IReadOnlyList<WldNpc> Npcs { get; init; }
}
