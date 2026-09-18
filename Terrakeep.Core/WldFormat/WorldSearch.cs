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
// SubX/SubY (19-sep-2026, tercer reporte real de marcador desplazado): la parte FRACCIONARIA de
// tile que hay que sumar a X/Y para obtener el centro geometrico real de lo que representa el
// resultado. X/Y siguen siendo enteros y siguen significando exactamente lo de siempre (son la
// coordenada con la que se navega y con la que se busca el cofre/letrero real en el .wld - no se
// toca ni un consumidor), pero un entero NO puede expresar el centro de todos los casos:
//   - algo de 1x1 en (x,y) ocupa [x, x+1) -> su centro es x + 0.5   (SubX = 0.5, el valor por
//     defecto, que es el caso mayoritario: tiles, paredes, liquidos, NPCs, centroides de veta)
//   - algo de 2x2 en la esquina (X,Y) ocupa [X, X+2) -> su centro es X + 1 EXACTO, que ya es
//     entero y ya viene sumado en X/Y (cofres, letreros, marcos de objeto) -> SubX = 0
//   - footprint IMPAR en un eje (el alto 3 del maniqui, el ancho 1 del frasco): el codigo ya
//     guardaba footprint/2 con division ENTERA y el propio comentario de TileEntityCenterOffset
//     reconocia que eso deja el marcador "a 0.5 tiles del centro geometrico, inevitable sin
//     coordenadas fraccionarias". Esto es justo esa coordenada fraccionaria: ese medio tile deja
//     de ser inevitable.
// Un unico +0.5 global en la vista NO vale (se probo y se midio): arreglaria los 1x1 y romperia
// por medio tile justo los cofres, que es la categoria que mas usa el usuario.
public readonly record struct WorldSearchHit(int X, int Y, string Name, WorldSearchKind Kind, double SubX = 0.5, double SubY = 0.5);

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
                        Add(ref total, hits, query.DisplayLimit, new WorldSearchHit(chest.X + 1, chest.Y + 1, itemNames.GetName(item.NetId), WorldSearchKind.ChestItem, 0, 0));
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
                var (offsetX, offsetY, subX, subY) = TileEntityCenterOffset(entity.Kind);
                foreach (var item in entity.Items)
                    if (query.ChestItemIds.Contains(item.NetId))
                        Add(ref total, hits, query.DisplayLimit, new WorldSearchHit(entity.X + offsetX, entity.Y + offsetY, itemNames.GetName(item.NetId), WorldSearchKind.TileEntityItem, subX, subY));
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
                    Add(ref total, hits, query.DisplayLimit, new WorldSearchHit(sign.X + 1, sign.Y + 1, TruncateSignText(sign.Text), WorldSearchKind.Sign, 0, 0));
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
    // SOLO se compensan los Kind con evidencia real y verificada (17-sep-2026, bitacora.md de
    // Terrakeep y de KeepQA; ronda de cierre de deuda, misma noche):
    //   - ItemFrame: Style2x2 puro, SIN ningun override de Origin/Width/Height
    //     (TileObjectData.cs decompilado, addTile(395)) - el MISMO primitivo exacto que el cofre
    //     ya arreglado arriba (tambien Style2x2), maxima confianza aunque este .wld concreto no
    //     tuviera ningun marco de objeto real que medir.
    //   - DisplayDoll (maniqui/womanniqui): Style2xX con Height=3 explicito (footprint 2x3) -
    //     VERIFICADO CON DATOS REALES: 125 maniquies reales de
    //     LLUIS-ADRI-PAU-WORLD.wld (KeepQA, verificarAlineacionMarcador.js), offset CONSTANTE en
    //     los 125 (rango 0,0 tiles).
    //   - HatRack: la ambiguedad de la ronda anterior queda RESUELTA con datos reales de fuente:
    //     TileID.cs (tModLoader, decompilado) tiene DOS constantes de rack de armas -
    //     WeaponsRack=334 (la reja decorativa legada, pre-Journey's End, sin TileEntity) y
    //     WeaponsRack2=471 (la real con TEWeaponsRack) - el "desfase" que parecia haber con
    //     HatRack=475 no era tal: TileObjectData.cs decompilado registra addTile(475) con
    //     newTile.CopyFrom(Style3x4) (Width=3/Height=4, TileObjectData.cs:3398 addBaseTile(out
    //     Style3x4) confirma esos numeros) y
    //     HookPostPlaceMyPlayer=TEHatRack.Hook_AfterPlacement con processedCoordinates:false -
    //     coincide exactamente con que TEHatRack.Hook_AfterPlacement (linea 82) hace su propio
    //     "Place(x + -1, y + -3)" (el hook recibe coordenadas SIN procesar y se autocorrige a la
    //     esquina superior-izquierda real). Footprint 3x4 confirmado por fuente inequivoca -> 0
    //     instancias reales en los 4 mundos disponibles, arreglado por confianza de fuente (mismo
    //     criterio ya usado con ItemFrame).
    //   - WeaponRack (WeaponsRack2=471, la real con TEWeaponsRack): TileObjectData.cs decompilado
    //     registra addTile(471) con newTile.CopyFrom(Style3x3Wall) (Width=3/Height=3,
    //     TileObjectData.cs:4392-4400 confirma Width=3/Height=3) y
    //     HookPostPlaceMyPlayer=TEWeaponsRack.Hook_AfterPlacement con processedCoordinates:true -
    //     coincide con que TEWeaponsRack.Hook_AfterPlacement (linea 72) llama "Place(x, y)" SIN
    //     ningun ajuste propio (coordenadas ya normalizadas a la esquina por el propio motor de
    //     colocacion antes de invocar el hook, mismo mecanismo que el cofre). Footprint 3x3
    //     confirmado por fuente inequivoca -> 0 instancias reales en los 4 mundos disponibles,
    //     arreglado por confianza de fuente.
    //   - DeadCellsDisplayJar: NO existe en absoluto en el arbol decompilado de tModLoader
    //     (1.4.4.9, el que de verdad juegan los usuarios de Terrakeep/Calamity) - es contenido
    //     vanilla 1.4.5.8 que tModLoader todavia no ha portado (confirmado real: 0 resultados
    //     buscando el nombre en todo tModLoader-Decompiled\tModLoader). Solo existe en
    //     TerrariaVanilla-Decompiled (1.4.5.8): TileObjectData.cs decompilado (ese arbol) registra
    //     addTile(698) con newTile.CopyFrom(Style1x2Top) (Width=1/Height=2, confirmado por
    //     addBaseTile(out Style1x2Top) con Width=1/Height=2 explicitos) y
    //     HookPostPlaceMyPlayer=TEDeadCellsDisplayJar.Hook_AfterPlacement con
    //     processedCoordinates:true - coincide con que Hook_AfterPlacement llama "Place(x, y)" sin
    //     ajuste propio. Footprint 1x2 confirmado por fuente inequivoca, aunque solo alcanzable
    //     cargando un .wld vanilla 1.4.5.8 crudo (no generado por tModLoader) en Terrakeep -
    //     arreglado igualmente por confianza de fuente, documentado el origen exacto del dato.
    //   - KiteAnchor (723) y CritterAnchor (724): mismo origen que el anterior (SOLO en
    //     TerrariaVanilla-Decompiled 1.4.5.8, no existen en tModLoader) - TileObjectData.cs de ese
    //     arbol registra ambos con newTile.CopyFrom(Style1x1) (footprint 1x1 real, sin excepcion:
    //     el propio addTile(724) fija ademas Width=1/Height=1/Origin=(0,0) explicitos linea a
    //     linea) y processedCoordinates:true. Un objeto 1x1 NO tiene esquina distinta de su
    //     centro (offset/2 con division entera siempre da 0 en ambos ejes) - NO APLICA, mismo
    //     motivo exacto que minerales/gemas/NPCs de la tabla de arriba, no falta de datos.
    //
    // TrainingDummy, LogicSensor, TeleportationPylon se dejan en (0,0) por un motivo distinto
    // (no de footprint): WldReader.ReadTileEntities NUNCA les rellena Items (dummy solo guarda su
    // Npc, sensor solo guarda LogicCheck/On, pylon "sin datos propios") - jamas pueden casar por
    // ChestItemIds ni producir un WorldSearchHit aqui, el offset es indiferente en la practica.
    // SubX/SubY = el medio tile que la division ENTERA de arriba tenia que tirar a la basura, por
    // eje y por Kind (19-sep-2026 - ver el comentario de WorldSearchHit): 0 cuando el footprint de
    // ese eje es PAR (footprint/2 ya es el centro exacto) y 0.5 cuando es IMPAR (footprint/2
    // entero cae en la casilla del medio, cuyo centro esta medio tile mas alla).
    //   ItemFrame 2x2 -> par/par              DisplayDoll 2x3 -> par/IMPAR (alto 3)
    //   HatRack 2x4 -> par/par                WeaponRack 2x2 -> par/par
    //   DeadCellsDisplayJar 1x2 -> IMPAR/par  resto: se tratan como 1x1 -> impar/impar
    private static (int X, int Y, double SubX, double SubY) TileEntityCenterOffset(WldTileEntityKind kind) => kind switch
    {
        WldTileEntityKind.ItemFrame => (1, 1, 0, 0),
        WldTileEntityKind.DisplayDoll => (1, 1, 0, 0.5),
        WldTileEntityKind.HatRack => (1, 2, 0, 0),
        WldTileEntityKind.WeaponRack => (1, 1, 0, 0),
        WldTileEntityKind.DeadCellsDisplayJar => (0, 1, 0.5, 0),
        _ => (0, 0, 0.5, 0.5),
    };

    private static void Add(ref int total, List<WorldSearchHit> hits, int limit, WorldSearchHit hit)
    {
        total++;
        if (hits.Count < limit) hits.Add(hit);
    }
}
