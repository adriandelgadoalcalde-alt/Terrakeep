using TerrasavrNative.Core.Data;

namespace TerrasavrNative.Core.WldFormat;

// Punto 4 del feedback del usuario ("el mundo... podria tener un buscador de todo tipo de
// objetos, no es un editor pero si un buscador") - ESPEC-buscador-mundo-tedit.md (advisor
// Opus, ingenieria inversa del buscador real de TEdit).
//
// Fase 1: busca sobre lo que ExplorationViewModel ya leia en memoria (tiles, paredes,
// liquidos, NPCs).
// Fase 2 (esta pasada): + cofres (por objeto real dentro, WldChest/WldChestItem) y letreros
// (por texto libre, WldSign) - ahora que WldReader lee esas dos secciones. Tile entities
// (maniquies/marcos de item/percheros) siguen fuera a proposito - el advisor no leyo
// TileEntity.Load campo a campo (formato polimorfico por tipo, variantes reales entre
// versiones), y WldReader.Read tampoco intenta leerlas (salta directo por puntero a NPCs).
//
// Alcance deliberado, documentado y no un descuido: NO deduplica sprites multi-tile (un cofre
// 2x2 sale como 4 coincidencias, una por tile) - deduplicar de verdad necesita el
// frameSize/textureGrid real de cada tile (Data/tiles.json de TEdit), que este proyecto no
// importa todavia (ESPEC-buscador-mundo-tedit.md#5.4, opcion 3). Una fusion aproximada por
// proximidad (opcion 2 del mismo documento) se descarta a proposito: fusionaria tambien
// coincidencias REALES y distintas de un mismo tipo de bloque comun - preferible mostrar de
// mas y ser exacto que fusionar con un umbral inventado.
public readonly record struct WorldSearchHit(int X, int Y, string Name, WorldSearchKind Kind);

// OreVein va AL FINAL a proposito (el orden se refleja en KindLabel de la App - ver
// ESPEC-ui-exploracion.md#14.2) - Fase 3/Minerales (advisor Opus): las vetas encontradas por
// OreVeinFinder se vuelcan como WorldSearchHit normales para heredar gratis la navegacion
// circular/distancia al spawn/marcador que ya tiene cualquier resultado.
public enum WorldSearchKind { Tile, Wall, Liquid, Npc, ChestItem, Sign, OreVein }

public sealed class WorldSearchQuery
{
    public IReadOnlySet<int> TileTypes { get; init; } = new HashSet<int>();
    public IReadOnlySet<int> WallIds { get; init; } = new HashSet<int>();
    public IReadOnlySet<byte> LiquidTypes { get; init; } = new HashSet<byte>();
    public IReadOnlySet<int> NpcIds { get; init; } = new HashSet<int>();
    // Fase 2: NetId real del objeto (mismo id que VanillaItemCatalog/ItemID.cs - los NetId de
    // Calamity que un .wld real pueda guardar no se conocen de antemano, tModLoader los asigna
    // en tiempo de carga del mod; ver el comentario de ItemNames en Run).
    public IReadOnlySet<int> ChestItemIds { get; init; } = new HashSet<int>();
    // Fase 3 (ESPEC-ui-exploracion.md#14.2): variante EXACTA de un tile enmarcado (Type,U,V) -
    // permite buscar "Cofre de oro" (21,36,0) y no "cualquier cofre" (TileTypes={21}). Conjunto
    // aparte de TileTypes (no un Dictionary<..,bool>, un Set alcanza: la presencia ya es la
    // señal) - un tile puede casar por TileTypes O por SpriteVariants, nunca produce dos filas
    // por la misma casilla (ver Run).
    public IReadOnlySet<(int Type, short U, short V)> SpriteVariants { get; init; } = new HashSet<(int, short, short)>();
    // Fase 2: los letreros son texto libre, no un catalogo de ids - en vez de acoplar Core a la
    // gramatica de busqueda de la App (LibrarySearchGrammar vive en TerrasavrNative.App, Core
    // no puede depender de App), quien construye la query decide COMO casa el texto (un
    // Contains simple, la gramatica real de comas/espacios/#id, lo que haga falta) y aqui solo
    // se invoca el predicado por cada letrero real.
    public Func<string, bool>? SignTextPredicate { get; init; }
    public int DisplayLimit { get; init; } = 1000;

    public bool IsEmpty => TileTypes.Count == 0 && WallIds.Count == 0 && LiquidTypes.Count == 0
        && NpcIds.Count == 0 && ChestItemIds.Count == 0 && SpriteVariants.Count == 0 && SignTextPredicate == null;
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

    // Un letrero real puede ser largo/multilinea - recorta para la fila de resultado, no para
    // el texto real (eso lo sigue teniendo WldSign.Text si algun dia hace falta mostrarlo
    // entero, ej. en un tooltip).
    private static string TruncateSignText(string text)
    {
        string oneLine = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return oneLine.Length > 60 ? oneLine[..60] + "…" : oneLine;
    }

    // Bucle x->y (mismo orden que el RLE del .wld, ver WldReader) sobre TODA la rejilla -
    // mismo patron real que TEdit (FindSidebarViewModel.SearchMap), pensado para correr fuera
    // del hilo de UI (Task.Run, ver ExplorationViewModel) y ser cancelable a media pasada.
    // itemNames resuelve el nombre real de un objeto encontrado en un cofre (VanillaItemCatalog
    // - un NetId de Calamity real no reconocido cae en su propio "Item #N" de fallback, nunca
    // se inventa un nombre).
    public static WorldSearchResult Run(WldWorld world, WorldSearchQuery query, TileNameCatalog tileNames, NpcNameCatalog npcNames, VanillaItemCatalog itemNames, CancellationToken ct = default)
    {
        var hits = new List<WorldSearchHit>();
        int total = 0;

        bool wantsTileScan = query.TileTypes.Count > 0 || query.WallIds.Count > 0 || query.LiquidTypes.Count > 0 || query.SpriteVariants.Count > 0;
        if (wantsTileScan)
        {
            int w = world.Header.TilesWide, h = world.Header.TilesHigh;
            for (int x = 0; x < w; x++)
            {
                ct.ThrowIfCancellationRequested();
                for (int y = 0; y < h; y++)
                {
                    var tile = world.Tiles[x, y];
                    // Un tile casa por TileTypes (cualquier variante) O por SpriteVariants (una
                    // variante exacta) - una unica fila por casilla aunque las dos condiciones
                    // sean ciertas a la vez.
                    if (tile.IsActive && (query.TileTypes.Contains(tile.Type) || query.SpriteVariants.Contains((tile.Type, tile.U, tile.V))))
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

        // Fase 2: la coordenada del resultado es la del CONTENEDOR, no la del objeto dentro -
        // mismo criterio real que TEdit (SearchContainers, ESPEC-buscador-mundo-tedit.md#1.2).
        // Un cofre con mas de un objeto que casa da mas de una fila, misma posicion las dos.
        if (query.ChestItemIds.Count > 0)
        {
            foreach (var chest in world.Chests)
            {
                ct.ThrowIfCancellationRequested();
                foreach (var item in chest.Items)
                    if (query.ChestItemIds.Contains(item.NetId))
                        Add(ref total, hits, query.DisplayLimit, new WorldSearchHit(chest.X, chest.Y, itemNames.GetName(item.NetId), WorldSearchKind.ChestItem));
            }
        }

        if (query.SignTextPredicate != null)
        {
            foreach (var sign in world.Signs)
            {
                ct.ThrowIfCancellationRequested();
                if (query.SignTextPredicate(sign.Text))
                    Add(ref total, hits, query.DisplayLimit, new WorldSearchHit(sign.X, sign.Y, TruncateSignText(sign.Text), WorldSearchKind.Sign));
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
