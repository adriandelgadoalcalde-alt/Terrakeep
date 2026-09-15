namespace Terrakeep.Core.WldFormat;

// Un mundo ya leido: cabecera + rejilla de tiles (indexada [x, y], igual que Main.tile[i,j] en
// el juego real) + NPCs + cofres + letreros. Solo lectura salvo por UNA excepcion deliberada y
// estrecha (WldWriter.PatchGameMode, pedido explicito del usuario 5-sep-2026: dificultad del
// mundo editable) - todo lo demas (tiles, NPCs, cofres...) se sigue sin poder escribir nunca.
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
    // Editor de mundos v1 (14-sep-2026): null en un mundo anterior a la version 210 (Journey's
    // End) - esa version de verdad no guarda esta seccion, no es un fallo de lectura.
    public WldBestiary? Bestiary { get; init; }

    // Ver WldHeader.WithGameMode/WithSpawn/WithTimeAndMoon/WithBossFlags - reconstruye el
    // WldWorld con la cabecera ya parcheada tras un guardado real, reutilizando el resto tal
    // cual (nada de eso cambia al editar solo la cabecera).
    public WldWorld WithHeader(WldHeader newHeader) => new()
    {
        Header = newHeader, Tiles = Tiles, Npcs = Npcs, Chests = Chests, Signs = Signs,
        TileEntities = TileEntities, ShimmeredNpcTypes = ShimmeredNpcTypes, Bestiary = Bestiary,
    };

    // Editor de cofres/letreros v1 (T1, 15-sep-2026): mismo patron exacto que WithHeader - tras
    // un guardado real via WldWriter.WriteChestItems/WriteSignText, reconstruye el WldWorld en
    // memoria con SOLO ese cofre/letrero actualizado, sin releer el mundo entero (releer un
    // mundo Grande cuesta ~1.4s reales, ver WldWriter.WithGameMode).
    public WldWorld WithChestItems(int chestIndex, IReadOnlyList<WldChestItem> newItems)
    {
        var chest = Chests[chestIndex];
        var newChests = Chests.ToList();
        newChests[chestIndex] = new WldChest { X = chest.X, Y = chest.Y, Name = chest.Name, Items = newItems, MaxItems = chest.MaxItems };
        return new WldWorld
        {
            Header = Header, Tiles = Tiles, Npcs = Npcs, Chests = newChests, Signs = Signs,
            TileEntities = TileEntities, ShimmeredNpcTypes = ShimmeredNpcTypes, Bestiary = Bestiary,
        };
    }

    public WldWorld WithSignText(int signIndex, string newText)
    {
        var sign = Signs[signIndex];
        var newSigns = Signs.ToList();
        newSigns[signIndex] = new WldSign { X = sign.X, Y = sign.Y, Text = newText };
        return new WldWorld
        {
            Header = Header, Tiles = Tiles, Npcs = Npcs, Chests = Chests, Signs = newSigns,
            TileEntities = TileEntities, ShimmeredNpcTypes = ShimmeredNpcTypes, Bestiary = Bestiary,
        };
    }
}
