namespace TerrasavrNative.Core.WldFormat;

// Un mundo ya leido: cabecera + rejilla de tiles (indexada [x, y], igual que Main.tile[i,j] en
// el juego real) + NPCs + cofres + letreros. Solo lectura - nunca se escribe un .wld desde esta
// app.
public sealed class WldWorld
{
    public required WldHeader Header { get; init; }
    public required WldTile[,] Tiles { get; init; }
    public required IReadOnlyList<WldNpc> Npcs { get; init; }
    // Punto 4 (advisor Opus, buscador de objetos del mundo), Fase 2 de
    // ESPEC-buscador-mundo-tedit.md.
    public required IReadOnlyList<WldChest> Chests { get; init; }
    public required IReadOnlyList<WldSign> Signs { get; init; }
    // Fase 2b (diferida de la Fase 2 original - ver el comentario de WldReader.Read): maniquies/
    // marcos de objeto/percheros/bandejas/frascos/anclas, con el formato real verificado campo a
    // campo contra TileEntity.cs de TEdit.
    public required IReadOnlyList<WldTileEntity> TileEntities { get; init; }
    // H6-08/H6-09 (sexta auditoria de Opus): tipos de NPC "shimmerizados" REALES de este mundo
    // concreto (WorldFile.LoadNPCs real: NPC.ShimmeredTownNPCs[tipo]=true - un estado GLOBAL
    // por tipo en ese mundo, no por instancia) - usado por NpcHeadProfile para elegir la cabeza
    // normal o la version "shimmer" real de cada NPC.
    public required IReadOnlySet<int> ShimmeredNpcTypes { get; init; }
}
