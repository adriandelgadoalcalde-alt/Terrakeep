namespace TerrasavrNative.Core.WldFormat;

// Que hay DE VERDAD en este mundo concreto - censo por tipo, calculado una sola vez al cargar.
//
// Peticion literal del usuario (4-sep-2026): "que entre todas las opciones solo puedan salir los
// objetos que tiene ese mundo, los que no ha habido suerte que en ese mundo se generen que no
// salgan en la busqueda". TEdit NO hace esto en ningun sitio (sus pickers salen siempre del
// catalogo completo del juego, WorldConfiguration.*, cargado en el constructor y nunca
// recargado - ver ESPEC-ui-exploracion.md#2): lo unico parecido que tiene es el censo en TEXTO
// de WorldAnalysis.cs:67-120, que se tira nada mas escribirlo. Esto es ese mismo censo, pero
// vivo y consultable.
//
// Medido en mundos reales de esta maquina (ESPEC-ui-exploracion.md#6): un mundo Grande de
// 8400x2400 contiene 260 tipos de tile de los 754 del catalogo, 125 paredes de 367 y 206 NetId
// distintos dentro de sus cofres - entre el 65% y el 96% de lo que hoy se ofrece como candidato
// no existe en el mundo cargado.
public sealed class WorldPresenceIndex
{
    // Recuento real de tiles por tipo (solo IsActive). La clave es el Type del tile.
    public required IReadOnlyDictionary<int, int> TileCounts { get; init; }
    // Recuento por id de pared (Wall != 0).
    public required IReadOnlyDictionary<int, int> WallCounts { get; init; }
    // Recuento por codigo de liquido (1=Agua, 2=Lava, 3=Miel, 4=Centelleo sintetico).
    public required IReadOnlyDictionary<byte, int> LiquidCounts { get; init; }
    // Variantes de sprite REALMENTE presentes, por (tipo, u, v) - solo para tiles enmarcados.
    // 4774 entradas en el mundo Grande medido: cabe de sobra en memoria y es lo que permite
    // ofrecer "Cofre de oro" en vez de "Cofres" a secas.
    public required IReadOnlyDictionary<(int Type, short U, short V), int> SpriteVariantCounts { get; init; }
    // Instancias reales por tipo de NPC.
    public required IReadOnlyDictionary<int, int> NpcCounts { get; init; }
    // NetId -> numero de slots ocupados por ese objeto en algun cofre real del mundo.
    public required IReadOnlyDictionary<int, int> ChestItemCounts { get; init; }
    // Cofres agrupados por la variante real de su casilla (Type,U,V del tile en chest.X/Y) -
    // lo que permite listar "Cofre de oro: 48" sin volver a recorrer nada.
    public required IReadOnlyDictionary<(int Type, short U, short V), int> ChestKindCounts { get; init; }
    public required int SignCount { get; init; }
    // Fase 2b: NetId -> numero de veces que ese objeto aparece expuesto/colgado en alguna tile
    // entity real del mundo (marco de objeto, perchero, maniqui, bandeja, frasco, ancla) - vive
    // aparte de ChestItemCounts (un marco de objeto no es un cofre) pero ambas se pueden unir en
    // la App para un unico "buscar este objeto en cualquier contenedor del mundo".
    public required IReadOnlyDictionary<int, int> TileEntityItemCounts { get; init; }

    public bool HasTile(int type) => TileCounts.ContainsKey(type);
    public bool HasWall(int id) => WallCounts.ContainsKey(id);
    public bool HasLiquid(byte code) => LiquidCounts.ContainsKey(code);
    public bool HasNpc(int id) => NpcCounts.ContainsKey(id);
    public bool HasChestItem(int netId) => ChestItemCounts.ContainsKey(netId);
    public bool HasTileEntityItem(int netId) => TileEntityItemCounts.ContainsKey(netId);

    // Una sola pasada x->y (mismo orden que el RLE del .wld, ver WldReader/WorldSearch.Run) mas
    // tres bucles cortos sobre Npcs/Chests. Pensado para correr dentro del mismo Task.Run que ya
    // lee+pinta el mundo (ExplorationViewModel.LoadFromPathAsync) - coste medido en un port a
    // Node sobre un mundo real de 8400x2400: decenas de milisegundos, frente a los ~1.4s que ya
    // cuesta ese paso completo (ver ESPEC-ui-exploracion.md#6, con la cautela real de que esa
    // medicion es de un port a JS, no del C# real).
    public static WorldPresenceIndex Build(WldWorld world, CancellationToken ct = default)
    {
        var tileCounts = new Dictionary<int, int>();
        var wallCounts = new Dictionary<int, int>();
        var liquidCounts = new Dictionary<byte, int>();
        var spriteVariantCounts = new Dictionary<(int, short, short), int>();

        int w = world.Header.TilesWide, h = world.Header.TilesHigh;
        bool[] frameImportant = world.Header.TileFrameImportant;
        for (int x = 0; x < w; x++)
        {
            ct.ThrowIfCancellationRequested();
            for (int y = 0; y < h; y++)
            {
                var tile = world.Tiles[x, y];
                if (tile.IsActive)
                {
                    tileCounts[tile.Type] = tileCounts.GetValueOrDefault(tile.Type) + 1;
                    // Mismo criterio real que usa el propio lector (WldReader.cs) para decidir
                    // si un tile trae U/V: header.TileFrameImportant[tipo].
                    bool isFramed = tile.Type < frameImportant.Length && frameImportant[tile.Type];
                    if (isFramed)
                    {
                        var key = (tile.Type, tile.U, tile.V);
                        spriteVariantCounts[key] = spriteVariantCounts.GetValueOrDefault(key) + 1;
                    }
                }
                if (tile.Wall != 0) wallCounts[tile.Wall] = wallCounts.GetValueOrDefault(tile.Wall) + 1;
                if (tile.LiquidAmount > 0) liquidCounts[tile.LiquidType] = liquidCounts.GetValueOrDefault(tile.LiquidType) + 1;
            }
        }

        var npcCounts = new Dictionary<int, int>();
        foreach (var npc in world.Npcs) npcCounts[npc.Id] = npcCounts.GetValueOrDefault(npc.Id) + 1;

        var chestItemCounts = new Dictionary<int, int>();
        var chestKindCounts = new Dictionary<(int, short, short), int>();
        foreach (var chest in world.Chests)
        {
            ct.ThrowIfCancellationRequested();
            foreach (var item in chest.Items)
                chestItemCounts[item.NetId] = chestItemCounts.GetValueOrDefault(item.NetId) + 1;

            if (chest.X >= 0 && chest.X < w && chest.Y >= 0 && chest.Y < h)
            {
                var tile = world.Tiles[chest.X, chest.Y];
                var key = (tile.Type, tile.U, tile.V);
                chestKindCounts[key] = chestKindCounts.GetValueOrDefault(key) + 1;
            }
        }

        var tileEntityItemCounts = new Dictionary<int, int>();
        foreach (var entity in world.TileEntities)
            foreach (var item in entity.Items)
                tileEntityItemCounts[item.NetId] = tileEntityItemCounts.GetValueOrDefault(item.NetId) + 1;

        return new WorldPresenceIndex
        {
            TileCounts = tileCounts,
            WallCounts = wallCounts,
            LiquidCounts = liquidCounts,
            SpriteVariantCounts = spriteVariantCounts,
            NpcCounts = npcCounts,
            ChestItemCounts = chestItemCounts,
            ChestKindCounts = chestKindCounts,
            SignCount = world.Signs.Count,
            TileEntityItemCounts = tileEntityItemCounts,
        };
    }
}
