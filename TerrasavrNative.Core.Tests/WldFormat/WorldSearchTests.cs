using System.Text;
using System.Text.Json;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.WldFormat;
using Xunit;

namespace TerrasavrNative.Core.Tests.WldFormat;

// Punto 4 (advisor Opus, buscador de objetos del mundo, ver ESPEC-buscador-mundo-tedit.md,
// Fase 1 + Fase 2). Mundo sintetico pequeño (5x5) en vez de un .wld real - WorldSearch.Run es
// una funcion pura sobre WldWorld, no hace falta decodificar ningun fichero real para probar
// el barrido/limite/cancelacion.
public class WorldSearchTests
{
    private static WldWorld MakeWorld(WldTile[,] tiles, IReadOnlyList<WldNpc>? npcs = null,
        IReadOnlyList<WldChest>? chests = null, IReadOnlyList<WldSign>? signs = null,
        IReadOnlyList<WldTileEntity>? tileEntities = null) => new()
    {
        Header = new WldHeader
        {
            Version = 279,
            Pointers = new int[10],
            TileFrameImportant = [],
            Title = "Mundo de prueba",
            WorldId = 1,
            TilesHigh = tiles.GetLength(1),
            TilesWide = tiles.GetLength(0),
            SpawnX = 0,
            SpawnY = 0,
            GroundLevel = 100,
            RockLevel = 200,
        },
        Tiles = tiles,
        Npcs = npcs ?? [],
        Chests = chests ?? [],
        Signs = signs ?? [],
        TileEntities = tileEntities ?? [],
        ShimmeredNpcTypes = new HashSet<int>(),
    };

    private static TileNameCatalog MakeTileNames(params (int Id, string Name)[] tiles)
    {
        var tilesObj = new Dictionary<string, object>();
        foreach (var (id, name) in tiles) tilesObj[id.ToString()] = new { name };
        string json = JsonSerializer.Serialize(new { tiles = tilesObj, walls = new { }, });
        return TileNameCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
    }

    private static NpcNameCatalog MakeNpcNames(params (int Id, string Name)[] npcs)
    {
        string json = JsonSerializer.Serialize(npcs.Select(n => new { id = n.Id, key = "K" + n.Id, es = n.Name }));
        return NpcNameCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
    }

    private static VanillaItemCatalog MakeItemNames(params (int Id, string Name)[] items)
    {
        var byId = items.ToDictionary(i => i.Id.ToString(), i => i.Name);
        var byKey = items.ToDictionary(i => "K" + i.Id, i => i.Name);
        var idsByKey = items.ToDictionary(i => "K" + i.Id, i => i.Id);
        return VanillaItemCatalog.LoadFromStreams(
            new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(byId))),
            new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(byKey))),
            new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(idsByKey))));
    }

    private static WorldSearchResult Run(WldWorld world, WorldSearchQuery query, TileNameCatalog? tileNames = null,
        NpcNameCatalog? npcNames = null, VanillaItemCatalog? itemNames = null, CancellationToken ct = default) =>
        WorldSearch.Run(world, query, tileNames ?? MakeTileNames(), npcNames ?? MakeNpcNames(), itemNames ?? MakeItemNames(), ct);

    [Fact]
    public void Run_EncuentraTilesReales_ConSuPosicionYNombre()
    {
        var tiles = new WldTile[3, 3];
        for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                tiles[x, y] = WldTile.Empty;
        tiles[1, 2] = new WldTile(2, 0, 0, 0, 0, 0); // tipo 2 = "Piedra"

        var world = MakeWorld(tiles);
        var tileNames = MakeTileNames((2, "Piedra"));

        var result = Run(world, new WorldSearchQuery { TileTypes = new HashSet<int> { 2 } }, tileNames);

        var hit = Assert.Single(result.Hits);
        Assert.Equal(1, hit.X);
        Assert.Equal(2, hit.Y);
        Assert.Equal("Piedra", hit.Name);
        Assert.Equal(WorldSearchKind.Tile, hit.Kind);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public void Run_SpriteVariantExacta_SoloCasaEsaVarianteNoElTipoEntero()
    {
        var tiles = new WldTile[2, 1];
        tiles[0, 0] = new WldTile(21, 0, 0, 0, 36, 0); // "cofre de oro" real (u=36)
        tiles[1, 0] = new WldTile(21, 0, 0, 0, 0, 0);  // "cofre de madera" real (u=0), mismo tipo

        var world = MakeWorld(tiles);
        var tileNames = MakeTileNames((21, "Cofre"));

        var result = Run(world, new WorldSearchQuery { SpriteVariants = new HashSet<(int, short, short)> { (21, 36, 0) } }, tileNames);

        var hit = Assert.Single(result.Hits);
        Assert.Equal(0, hit.X); // solo el de oro, NO el de madera aunque comparta Type
        Assert.Equal(WorldSearchKind.Tile, hit.Kind);
    }

    [Fact]
    public void Run_TileTypesYSpriteVariants_NuncaDuplicanLaMismaCasilla()
    {
        var tiles = new WldTile[1, 1];
        tiles[0, 0] = new WldTile(21, 0, 0, 0, 36, 0);

        var world = MakeWorld(tiles);
        // El mismo tile casa por las DOS condiciones a la vez (tipo 21 Y la variante exacta) -
        // tiene que dar una unica fila, no dos.
        var result = Run(world, new WorldSearchQuery
        {
            TileTypes = new HashSet<int> { 21 },
            SpriteVariants = new HashSet<(int, short, short)> { (21, 36, 0) },
        }, MakeTileNames((21, "Cofre")));

        Assert.Single(result.Hits);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public void Run_TileInactivo_NuncaCuentaAunqueElTipoCoincidaPorCasualidad()
    {
        // WldTile.Empty tiene Type=-1 (IsActive=false) - un tile vacio no debe colarse nunca en
        // una busqueda, aunque -1 este (por error) en el conjunto pedido.
        var tiles = new WldTile[2, 2];
        for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) tiles[x, y] = WldTile.Empty;

        var world = MakeWorld(tiles);
        var result = Run(world, new WorldSearchQuery { TileTypes = new HashSet<int> { -1 } });

        Assert.Empty(result.Hits);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public void Run_ParedYLiquido_SeBuscanIndependientementeDelTile()
    {
        var tiles = new WldTile[1, 1];
        tiles[0, 0] = new WldTile(-1, 5, 2, 200, 0, 0); // sin tile activo, pared 5, lava (2) al 200

        var world = MakeWorld(tiles);
        var conParedNombrada = TileNameCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { tiles = new { }, walls = new Dictionary<string, object> { ["5"] = new { name = "Piedra (pared)" } } }))));

        var resultPared = Run(world, new WorldSearchQuery { WallIds = new HashSet<int> { 5 } }, conParedNombrada);
        var hitPared = Assert.Single(resultPared.Hits);
        Assert.Equal(WorldSearchKind.Wall, hitPared.Kind);
        Assert.Equal("Piedra (pared)", hitPared.Name);

        var resultLiquido = Run(world, new WorldSearchQuery { LiquidTypes = new HashSet<byte> { 2 } });
        var hitLiquido = Assert.Single(resultLiquido.Hits);
        Assert.Equal(WorldSearchKind.Liquid, hitLiquido.Kind);
        Assert.Equal("Lava", hitLiquido.Name);
    }

    [Fact]
    public void Run_Npc_BuscaPorTipoYDevuelveElNombreDeTipoNoElPropio()
    {
        var tiles = new WldTile[1, 1];
        tiles[0, 0] = WldTile.Empty;
        var npcs = new List<WldNpc> { new() { Id = 17, GivenName = "Bob", TileX = 40, TileY = 60, Homeless = false, VariationIndex = 0 } };
        var world = MakeWorld(tiles, npcs);
        var npcNames = MakeNpcNames((17, "El Guia"));

        var result = Run(world, new WorldSearchQuery { NpcIds = new HashSet<int> { 17 } }, npcNames: npcNames);

        var hit = Assert.Single(result.Hits);
        Assert.Equal(40, hit.X);
        Assert.Equal(60, hit.Y);
        Assert.Equal("El Guia", hit.Name); // el nombre de TIPO, no "Bob" (GivenName)
        Assert.Equal(WorldSearchKind.Npc, hit.Kind);
    }

    [Fact]
    public void Run_RespetaElLimiteDeVisualizacion_PeroCuentaElTotalReal()
    {
        var tiles = new WldTile[10, 10];
        for (int x = 0; x < 10; x++)
            for (int y = 0; y < 10; y++)
                tiles[x, y] = new WldTile(1, 0, 0, 0, 0, 0);

        var world = MakeWorld(tiles);
        var result = Run(world, new WorldSearchQuery { TileTypes = new HashSet<int> { 1 }, DisplayLimit = 5 }, MakeTileNames((1, "Tierra")));

        Assert.Equal(5, result.Hits.Count);
        Assert.Equal(100, result.TotalCount);
    }

    [Fact]
    public void Run_QueryVacia_NoRecorreNadaYNoDevuelveResultados()
    {
        var tiles = new WldTile[2000, 2000]; // si recorriera esto de verdad con una query vacia, tardaria - la prueba en si mide que NO lo hace
        for (int x = 0; x < 2000; x++) for (int y = 0; y < 2000; y++) tiles[x, y] = new WldTile(1, 0, 0, 0, 0, 0);
        var world = MakeWorld(tiles);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = Run(world, new WorldSearchQuery());
        sw.Stop();

        Assert.Empty(result.Hits);
        Assert.Equal(0, result.TotalCount);
        Assert.True(sw.ElapsedMilliseconds < 500, $"una query vacia no deberia recorrer la rejilla entera (tardo {sw.ElapsedMilliseconds}ms)");
    }

    [Fact]
    public void Run_Cancelacion_LanzaOperationCanceledException()
    {
        var tiles = new WldTile[500, 500];
        for (int x = 0; x < 500; x++) for (int y = 0; y < 500; y++) tiles[x, y] = new WldTile(1, 0, 0, 0, 0, 0);
        var world = MakeWorld(tiles);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            Run(world, new WorldSearchQuery { TileTypes = new HashSet<int> { 1 } }, MakeTileNames((1, "Tierra")), ct: cts.Token));
    }

    // ---- Fase 2: cofres y letreros ----

    [Fact]
    public void Run_ObjetoEnCofre_DevuelveLaPosicionDelCofreNoDelObjeto()
    {
        var tiles = new WldTile[1, 1];
        tiles[0, 0] = WldTile.Empty;
        var chests = new List<WldChest>
        {
            new() { X = 30, Y = 40, Name = "", Items = [new WldChestItem(NetId: 4, Stack: 1, Prefix: 0)] },
        };
        var world = MakeWorld(tiles, chests: chests);
        var itemNames = MakeItemNames((4, "Espada larga de hierro"));

        var result = Run(world, new WorldSearchQuery { ChestItemIds = new HashSet<int> { 4 } }, itemNames: itemNames);

        var hit = Assert.Single(result.Hits);
        Assert.Equal(30, hit.X);
        Assert.Equal(40, hit.Y);
        Assert.Equal("Espada larga de hierro", hit.Name);
        Assert.Equal(WorldSearchKind.ChestItem, hit.Kind);
    }

    [Fact]
    public void Run_CofreConDosObjetosQueCasan_DaUnaFilaPorObjeto()
    {
        var tiles = new WldTile[1, 1];
        tiles[0, 0] = WldTile.Empty;
        var chests = new List<WldChest>
        {
            new() { X = 5, Y = 5, Name = "", Items = [new WldChestItem(4, 1, 0), new WldChestItem(8, 5, 0)] },
        };
        var world = MakeWorld(tiles, chests: chests);

        var result = Run(world, new WorldSearchQuery { ChestItemIds = new HashSet<int> { 4, 8 } });

        Assert.Equal(2, result.Hits.Count);
        Assert.All(result.Hits, h => Assert.Equal((5, 5), (h.X, h.Y)));
    }

    [Fact]
    public void Run_ObjetoDeCofreDesconocido_CaeEnElFallbackRealDelCatalogo()
    {
        // Un NetId de Calamity (o cualquier mod) real que el .wld guarde no se puede resolver
        // via VanillaItemCatalog - tiene que caer en el "Item #N" de fallback, nunca en un
        // nombre inventado.
        var tiles = new WldTile[1, 1];
        tiles[0, 0] = WldTile.Empty;
        var chests = new List<WldChest> { new() { X = 1, Y = 1, Name = "", Items = [new WldChestItem(99999, 1, 0)] } };
        var world = MakeWorld(tiles, chests: chests);

        var result = Run(world, new WorldSearchQuery { ChestItemIds = new HashSet<int> { 99999 } });

        Assert.Equal("Item #99999", Assert.Single(result.Hits).Name);
    }

    [Fact]
    public void Run_Letrero_UsaElPredicadoRealYRecortaElTextoLargo()
    {
        var tiles = new WldTile[1, 1];
        tiles[0, 0] = WldTile.Empty;
        string textoLargo = new string('a', 80);
        var signs = new List<WldSign>
        {
            new() { X = 7, Y = 8, Text = "La contraseña es 1234" },
            new() { X = 9, Y = 9, Text = "Otro letrero sin relacion" },
            new() { X = 1, Y = 1, Text = textoLargo },
        };
        var world = MakeWorld(tiles, signs: signs);

        var result = Run(world, new WorldSearchQuery { SignTextPredicate = t => t.Contains("contraseña", StringComparison.OrdinalIgnoreCase) });
        var hit = Assert.Single(result.Hits);
        Assert.Equal(7, hit.X);
        Assert.Equal("La contraseña es 1234", hit.Name);
        Assert.Equal(WorldSearchKind.Sign, hit.Kind);

        var resultLargo = Run(world, new WorldSearchQuery { SignTextPredicate = t => t.Length > 50 });
        var hitLargo = Assert.Single(resultLargo.Hits);
        Assert.True(hitLargo.Name.Length <= 61); // 60 + el caracter de recorte real "…"
        Assert.EndsWith("…", hitLargo.Name);
    }

    [Fact]
    public void Run_QueryVaciaConSoloUnPredicadoDeLetreroNulo_NoEsVacia()
    {
        Assert.False(new WorldSearchQuery { SignTextPredicate = _ => true }.IsEmpty);
        Assert.True(new WorldSearchQuery().IsEmpty);
    }
}
