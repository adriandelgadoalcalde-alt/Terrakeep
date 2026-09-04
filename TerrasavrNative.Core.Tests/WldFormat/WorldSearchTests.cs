using System.Text;
using System.Text.Json;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.WldFormat;
using Xunit;

namespace TerrasavrNative.Core.Tests.WldFormat;

// Punto 4 (advisor Opus, buscador de objetos del mundo, Fase 1 de
// ESPEC-buscador-mundo-tedit.md). Mundo sintetico pequeño (5x5) en vez de un .wld real -
// WorldSearch.Run es una funcion pura sobre WldWorld, no hace falta decodificar ningun
// fichero real para probar el barrido/limite/cancelacion.
public class WorldSearchTests
{
    private static WldWorld MakeWorld(WldTile[,] tiles, IReadOnlyList<WldNpc>? npcs = null) => new()
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
        var npcNames = MakeNpcNames();

        var result = WorldSearch.Run(world, new WorldSearchQuery { TileTypes = new HashSet<int> { 2 } }, tileNames, npcNames);

        var hit = Assert.Single(result.Hits);
        Assert.Equal(1, hit.X);
        Assert.Equal(2, hit.Y);
        Assert.Equal("Piedra", hit.Name);
        Assert.Equal(WorldSearchKind.Tile, hit.Kind);
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
        var result = WorldSearch.Run(world, new WorldSearchQuery { TileTypes = new HashSet<int> { -1 } }, MakeTileNames(), MakeNpcNames());

        Assert.Empty(result.Hits);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public void Run_ParedYLiquido_SeBuscanIndependientementeDelTile()
    {
        var tiles = new WldTile[1, 1];
        tiles[0, 0] = new WldTile(-1, 5, 2, 200, 0, 0); // sin tile activo, pared 5, lava (2) al 200

        var world = MakeWorld(tiles);
        var tileNames = MakeTileNames();
        // AllWalls no se usa aqui directamente, pero WallName necesita el nombre para el hit.
        var conParedNombrada = TileNameCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { tiles = new { }, walls = new Dictionary<string, object> { ["5"] = new { name = "Piedra (pared)" } } }))));

        var resultPared = WorldSearch.Run(world, new WorldSearchQuery { WallIds = new HashSet<int> { 5 } }, conParedNombrada, MakeNpcNames());
        var hitPared = Assert.Single(resultPared.Hits);
        Assert.Equal(WorldSearchKind.Wall, hitPared.Kind);
        Assert.Equal("Piedra (pared)", hitPared.Name);

        var resultLiquido = WorldSearch.Run(world, new WorldSearchQuery { LiquidTypes = new HashSet<byte> { 2 } }, tileNames, MakeNpcNames());
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

        var result = WorldSearch.Run(world, new WorldSearchQuery { NpcIds = new HashSet<int> { 17 } }, MakeTileNames(), npcNames);

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
        var result = WorldSearch.Run(world, new WorldSearchQuery { TileTypes = new HashSet<int> { 1 }, DisplayLimit = 5 }, MakeTileNames((1, "Tierra")), MakeNpcNames());

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
        var result = WorldSearch.Run(world, new WorldSearchQuery(), MakeTileNames(), MakeNpcNames());
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
            WorldSearch.Run(world, new WorldSearchQuery { TileTypes = new HashSet<int> { 1 } }, MakeTileNames((1, "Tierra")), MakeNpcNames(), cts.Token));
    }
}
