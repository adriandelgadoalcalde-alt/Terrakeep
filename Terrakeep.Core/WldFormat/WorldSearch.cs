using Terrakeep.Core.Data;

namespace Terrakeep.Core.WldFormat;

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
// TileEntityItem va justo despues de ChestItem a proposito (mismo criterio de origen: un
// objeto real encontrado DENTRO de un contenedor del mundo, solo que este no es un cofre -
// marco de objeto/perchero/maniqui/bandeja/frasco/ancla, Fase 2b).
public enum WorldSearchKind { Tile, Wall, Liquid, Npc, ChestItem, TileEntityItem, Sign, OreVein }

public sealed class WorldSearchQuery
{
    public IReadOnlySet<int> TileTypes { get; init; } = new HashSet<int>();
    public IReadOnlySet<int> WallIds { get; init; } = new HashSet<int>();
    public IReadOnlySet<byte> LiquidTypes { get; init; } = new HashSet<byte>();
    public IReadOnlySet<int> NpcIds { get; init; } = new HashSet<int>();
    // Fase 2: NetId real del objeto (mismo id que VanillaItemCatalog/ItemID.cs - los NetId de
    // Calamity que un .wld real pueda guardar no se conocen de antemano, tModLoader los asigna
    // en tiempo de carga del mod; ver el comentario de ItemNames en Run). Fase 2b: el mismo
    // conjunto de ids casa TANTO contra el contenido real de los cofres COMO contra el de las
    // tile entities (marcos/percheros/maniquies/...) - un unico "busca este objeto en cualquier
    // contenedor del mundo", cada coincidencia sale con el Kind real que le corresponde
    // (ChestItem o TileEntityItem) para poder distinguirlas en la lista de resultados.
    public IReadOnlySet<int> ChestItemIds { get; init; } = new HashSet<int>();
    // Fase 3 (ESPEC-ui-exploracion.md#14.2): variante EXACTA de un tile enmarcado (Type,U,V) -
    // permite buscar "Cofre de oro" (21,36,0) y no "cualquier cofre" (TileTypes={21}). Conjunto
    // aparte de TileTypes (no un Dictionary<..,bool>, un Set alcanza: la presencia ya es la
    // señal) - un tile puede casar por TileTypes O por SpriteVariants, nunca produce dos filas
    // por la misma casilla (ver Run).
    public IReadOnlySet<(int Type, short U, short V)> SpriteVariants { get; init; } = new HashSet<(int, short, short)>();
    // Fase 2: los letreros son texto libre, no un catalogo de ids - en vez de acoplar Core a la
    // gramatica de busqueda de la App (LibrarySearchGrammar vive en Terrakeep.App, Core
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
        //
        // Bug real reportado por el usuario jugando (17-sep-2026, confirmado con datos reales de
        // 358 cofres de un .wld real, offset CONSTANTE en los 358 - ver bitacora.md): WldChest.X/Y
        // es la esquina SUPERIOR-IZQUIERDA del bloque 2x2 que ocupa un cofre (TileObjectData:
        // Style2x2 + Origin=(0,1), confirmado contra TileObjectData.cs decompilado Y contra la
        // rejilla real del mundo), nunca su centro. Sumar la mitad del footprint (+1 tile en cada
        // eje) para que el marcador/la navegacion caigan en el CENTRO real del cofre, no en su
        // esquina - antes de este arreglo el marcador quedaba sistematicamente una casilla antes
        // en X e Y en TODOS los cofres.
        if (query.ChestItemIds.Count > 0)
        {
            foreach (var chest in world.Chests)
            {
                ct.ThrowIfCancellationRequested();
                foreach (var item in chest.Items)
                    if (query.ChestItemIds.Contains(item.NetId))
                        Add(ref total, hits, query.DisplayLimit, new WorldSearchHit(chest.X + 1, chest.Y + 1, itemNames.GetName(item.NetId), WorldSearchKind.ChestItem));
            }
        }

        // Fase 2b: mismo conjunto de ids que arriba, pero contra el contenido real de las tile
        // entities (marco de objeto/perchero/maniqui/bandeja/frasco/ancla) - la coordenada del
        // resultado es la de la propia tile entity.
        //
        // Bug real reportado por el usuario jugando (17-sep-2026, misma sesion que el arreglo de
        // cofres de arriba): el usuario pregunto si el mismo problema afecta a otras categorias
        // del buscador aparte de cofres. Investigado con datos reales (KeepQA,
        // verificarAlineacionMarcador.js + Terrakeep.Core.WldFormat.WldReader sobre 4 .wld reales:
        // Blando_Río.wld y LLUIS-ADRI-PAU-WORLD.wld, adriandres.wld,
        // 825aa9c2-47f8-425b-ab77-aedba421d2b9.wld) - SI: exactamente el mismo patron (esquina
        // cruda en vez de centro real), confirmado para los Kind con datos reales o fuente
        // decompilada inequivoca disponibles esta sesion. TileEntityCenterOffset compensa SOLO
        // esos Kind - ver su propio comentario para el detalle de cada uno y de por que el resto
        // se deja sin tocar (0,0) a proposito.
        if (query.ChestItemIds.Count > 0)
        {
            foreach (var entity in world.TileEntities)
            {
                ct.ThrowIfCancellationRequested();
                var (offsetX, offsetY) = TileEntityCenterOffset(entity.Kind);
                foreach (var item in entity.Items)
                    if (query.ChestItemIds.Contains(item.NetId))
                        Add(ref total, hits, query.DisplayLimit, new WorldSearchHit(entity.X + offsetX, entity.Y + offsetY, itemNames.GetName(item.NetId), WorldSearchKind.TileEntityItem));
            }
        }

        if (query.SignTextPredicate != null)
        {
            foreach (var sign in world.Signs)
            {
                ct.ThrowIfCancellationRequested();
                // Mismo bug/arreglo que arriba: un letrero real (Sign=55/GraveMarker "Tombstones"
                // =85/AnnouncementBox=425/TatteredSign=573, ver WldSign.cs) ocupa un bloque de
                // 2x2 tiles (TileObjectData.cs decompilado, Style2x2 sin ningun override de
                // Origin/Width/Height para estos 4 ids) - confirmado ademas con datos reales:
                // 226 letreros reales medidos tile a tile en 3 mundos jugados distintos (5 en
                // LLUIS-ADRI-PAU-WORLD.wld tipo Sign=55, 13 en Blando_Río.wld y 43 en
                // adriandres.wld tipo Tombstones=85, 179 en 825aa9c2-....wld tipo Sign=55/
                // AnnouncementBox=425), footprint 2x2 en TODOS salvo una unica fusion espuria de
                // dos lapidas contiguas del mismo estilo (artefacto de la propia medicion por
                // tiles pegados, no un letrero real de otro tamaño - ver bitacora.md 17-sep-2026).
                // sign.X/Y ya es la esquina superior-izquierda (igual criterio que WldChest.X/Y):
                // +1 en cada eje para centrar el marcador, mismo calculo exacto que los cofres.
                if (query.SignTextPredicate(sign.Text))
                    Add(ref total, hits, query.DisplayLimit, new WorldSearchHit(sign.X + 1, sign.Y + 1, TruncateSignText(sign.Text), WorldSearchKind.Sign));
            }
        }

        return new WorldSearchResult(hits, total);
    }

    // Compensacion esquina->centro por Kind de tile entity (mismo criterio que WldChest.X+1 de
    // arriba: offset = footprint/2 con division ENTERA - para un footprint par el resultado es el
    // centro real exacto -Style2x2 2/2=1, igual que el cofre-; para uno impar es la fila/columna
    // central real -Style2xX alto 3, 3/2=1 (division entera), la casilla de en medio de 0,1,2-,
    // la mejor aproximacion representable con coordenadas de tile enteras, a 0.5 tiles del centro
    // geometrico continuo, inevitable sin coordenadas fraccionarias).
    //
    // SOLO se compensan los Kind con evidencia real y verificada esta sesion (17-sep-2026,
    // bitacora.md de Terrakeep y de KeepQA):
    //   - ItemFrame: Style2x2 puro, SIN ningun override de Origin/Width/Height
    //     (TileObjectData.cs decompilado, addTile(395)) - el MISMO primitivo exacto que el cofre
    //     ya arreglado arriba (tambien Style2x2), maxima confianza aunque este .wld concreto no
    //     tuviera ningun marco de objeto real que medir.
    //   - DisplayDoll (maniqui/womanniqui): Style2xX con Height=3 explicito (footprint 2x3) -
    //     VERIFICADO CON DATOS REALES: 125 maniquies reales de
    //     LLUIS-ADRI-PAU-WORLD.wld (KeepQA, verificarAlineacionMarcador.js), offset CONSTANTE en
    //     los 125 (rango 0,0 tiles).
    //
    // Los demas Kind se dejan deliberadamente en (0,0), no por asumir que estan bien, sino porque
    // esta sesion no tuvo forma de verificarlos con el mismo rigor y el proyecto no fuerza un
    // numero inventado (ver bitacora.md, pendiente para una ronda aparte con datos reales de esos
    // objetos concretos):
    //   - TrainingDummy, LogicSensor, TeleportationPylon: WldReader.ReadTileEntities NUNCA les
    //     rellena Items (dummy solo guarda su Npc, sensor solo guarda LogicCheck/On, pylon "sin
    //     datos propios") - jamas pueden casar por ChestItemIds ni producir un WorldSearchHit
    //     aqui, el offset es indiferente en la practica.
    //   - HatRack: TileObjectData.cs decompilado lo liga (via TEHatRack.Hook_AfterPlacement) a un
    //     addTile con Style3x4, pero el ID numerico de ese addTile no cuadra de forma inequivoca
    //     con la constante HatRack de TileID.cs en este mismo build decompilado (posible
    //     desfase de version entre ambos archivos) - footprint real probable 3x4 pero NO
    //     confirmado con un dato real de esta sesion, se deja sin tocar a proposito.
    //   - WeaponRack, DeadCellsDisplayJar, KiteAnchor, CritterAnchor: SI pueden producir un hit
    //     real (leen items reales en WldReader.ReadTileEntities) pero ningun .wld real accesible
    //     en esta maquina tenia una instancia real que medir, y la fuente decompilada no dejo
    //     un addTile inequivoco para los 4 en el tiempo de esta sesion.
    private static (int X, int Y) TileEntityCenterOffset(WldTileEntityKind kind) => kind switch
    {
        WldTileEntityKind.ItemFrame => (1, 1),
        WldTileEntityKind.DisplayDoll => (1, 1),
        _ => (0, 0),
    };

    private static void Add(ref int total, List<WorldSearchHit> hits, int limit, WorldSearchHit hit)
    {
        total++;
        if (hits.Count < limit) hits.Add(hit);
    }
}
