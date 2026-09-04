using TerrasavrNative.Core.Data;

namespace TerrasavrNative.Core.WldFormat;

// Punto 4 del feedback del usuario ("el mundo... podria tener un buscador de todo tipo de
// objetos, no es un editor pero si un buscador") - Fase 1 de ESPEC-buscador-mundo-tedit.md
// (advisor Opus, ingenieria inversa del buscador real de TEdit): busca sobre lo que
// ExplorationViewModel YA lee en memoria (tiles, paredes, liquidos, NPCs) - cero cambios en
// WldReader/WldWorld. Cofres/letreros/tile entities quedan para una Fase 2 (formato .wld no
// leido todavia).
//
// Alcance deliberado de esta Fase 1 (documentado, no un descuido): NO deduplica sprites
// multi-tile (un cofre 2x2 sale como 4 coincidencias, una por tile) - deduplicar de verdad
// necesita el frameSize/textureGrid real de cada tile (Data/tiles.json de TEdit), que este
// proyecto no importa todavia (ESPEC-buscador-mundo-tedit.md#5.4, opcion 3, para una Fase 2).
// Una fusion aproximada por proximidad es la opcion 2 del mismo documento, pero se descarta a
// proposito por ahora: fusionaria tambien coincidencias REALES y distintas de un mismo tipo de
// bloque comun (ej. dos "Bloque de tierra" sueltos a menos de 3 tiles) - preferible mostrar de
// mas y ser exacto que fusionar con un umbral inventado.
public readonly record struct WorldSearchHit(int X, int Y, string Name, WorldSearchKind Kind);

public enum WorldSearchKind { Tile, Wall, Liquid, Npc }

public sealed class WorldSearchQuery
{
    public IReadOnlySet<int> TileTypes { get; init; } = new HashSet<int>();
    public IReadOnlySet<int> WallIds { get; init; } = new HashSet<int>();
    public IReadOnlySet<byte> LiquidTypes { get; init; } = new HashSet<byte>();
    public IReadOnlySet<int> NpcIds { get; init; } = new HashSet<int>();
    public int DisplayLimit { get; init; } = 1000;

    public bool IsEmpty => TileTypes.Count == 0 && WallIds.Count == 0 && LiquidTypes.Count == 0 && NpcIds.Count == 0;
}

public readonly record struct WorldSearchResult(IReadOnlyList<WorldSearchHit> Hits, int TotalCount);

public static class WorldSearch
{
    // Nombres reales de los 4 liquidos del juego (WldReader.cs ya usa estos mismos codigos:
    // 1=Agua, 2=Lava, 3=Miel, 4=Centelleo -sintetico, ver el comentario real de WldReader).
    // Copia deliberada y pequeña de ExplorationViewModel.LiquidName (Core no puede depender de
    // App) - 4 valores fijos del juego real, riesgo de divergencia minimo.
    public static string LiquidName(byte liquidType) => liquidType switch
    {
        2 => "Lava",
        3 => "Miel",
        4 => "Centelleo",
        _ => "Agua",
    };

    // Bucle x->y (mismo orden que el RLE del .wld, ver WldReader) sobre TODA la rejilla -
    // mismo patron real que TEdit (FindSidebarViewModel.SearchMap), pensado para correr fuera
    // del hilo de UI (Task.Run, ver ExplorationViewModel) y ser cancelable a media pasada.
    public static WorldSearchResult Run(WldWorld world, WorldSearchQuery query, TileNameCatalog tileNames, NpcNameCatalog npcNames, CancellationToken ct = default)
    {
        var hits = new List<WorldSearchHit>();
        int total = 0;

        bool wantsTileScan = query.TileTypes.Count > 0 || query.WallIds.Count > 0 || query.LiquidTypes.Count > 0;
        if (wantsTileScan)
        {
            int w = world.Header.TilesWide, h = world.Header.TilesHigh;
            for (int x = 0; x < w; x++)
            {
                ct.ThrowIfCancellationRequested();
                for (int y = 0; y < h; y++)
                {
                    var tile = world.Tiles[x, y];
                    if (tile.IsActive && query.TileTypes.Contains(tile.Type))
                        Add(ref total, hits, query.DisplayLimit, new WorldSearchHit(x, y, tileNames.TileVariantName(tile.Type, tile.U, tile.V), WorldSearchKind.Tile));
                    if (tile.Wall != 0 && query.WallIds.Contains(tile.Wall))
                        Add(ref total, hits, query.DisplayLimit, new WorldSearchHit(x, y, tileNames.WallName(tile.Wall), WorldSearchKind.Wall));
                    if (tile.LiquidAmount > 0 && query.LiquidTypes.Contains(tile.LiquidType))
                        Add(ref total, hits, query.DisplayLimit, new WorldSearchHit(x, y, LiquidName(tile.LiquidType), WorldSearchKind.Liquid));
                }
            }
        }

        if (query.NpcIds.Count > 0)
        {
            foreach (var npc in world.Npcs)
            {
                ct.ThrowIfCancellationRequested();
                if (query.NpcIds.Contains(npc.Id))
                    Add(ref total, hits, query.DisplayLimit, new WorldSearchHit(npc.TileX, npc.TileY, npcNames.GetName(npc.Id), WorldSearchKind.Npc));
            }
        }

        return new WorldSearchResult(hits, total);
    }

    private static void Add(ref int total, List<WorldSearchHit> hits, int limit, WorldSearchHit hit)
    {
        total++;
        if (hits.Count < limit) hits.Add(hit);
    }
}
