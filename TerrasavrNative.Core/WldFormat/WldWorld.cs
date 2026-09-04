namespace TerrasavrNative.Core.WldFormat;

// Un mundo ya leido: cabecera + rejilla de tiles (indexada [x, y], igual que Main.tile[i,j] en
// el juego real) + NPCs. Solo lectura - nunca se escribe un .wld desde esta app.
public sealed class WldWorld
{
    public required WldHeader Header { get; init; }
    public required WldTile[,] Tiles { get; init; }
    public required IReadOnlyList<WldNpc> Npcs { get; init; }
    // H6-08/H6-09 (sexta auditoria de Opus): tipos de NPC "shimmerizados" REALES de este mundo
    // concreto (WorldFile.LoadNPCs real: NPC.ShimmeredTownNPCs[tipo]=true - un estado GLOBAL
    // por tipo en ese mundo, no por instancia) - usado por NpcHeadProfile para elegir la cabeza
    // normal o la version "shimmer" real de cada NPC.
    public required IReadOnlySet<int> ShimmeredNpcTypes { get; init; }
}
