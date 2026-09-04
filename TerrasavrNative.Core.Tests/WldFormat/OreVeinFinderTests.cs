using TerrasavrNative.Core.WldFormat;
using Xunit;

namespace TerrasavrNative.Core.Tests.WldFormat;

// Punto 4 (advisor Opus, "buscador o marcador de minerales" - ver ESPEC-ui-exploracion.md#11.4).
public class OreVeinFinderTests
{
    private static WldWorld MakeWorld(WldTile[,] tiles) => new()
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
        Npcs = [],
        Chests = [],
        Signs = [],
        TileEntities = [],
        ShimmeredNpcTypes = new HashSet<int>(),
    };

    private static WldTile Ore(int type) => new((short)type, 0, 0, 0, 0, 0);

    [Fact]
    public void Find_DosVetasSeparadas_DevuelveDosGruposDistintos()
    {
        var tiles = new WldTile[10, 1];
        for (int x = 0; x < 10; x++) tiles[x, 0] = WldTile.Empty;
        tiles[0, 0] = Ore(7); tiles[1, 0] = Ore(7); tiles[2, 0] = Ore(7); // veta A: 3 tiles
        tiles[7, 0] = Ore(7); tiles[8, 0] = Ore(7); // veta B: 2 tiles, separada por hueco

        var vetas = OreVeinFinder.Find(MakeWorld(tiles), new HashSet<int> { 7 }, limit: 1000, out int total);

        Assert.Equal(2, total);
        Assert.Equal(2, vetas.Count);
        Assert.Equal(3, vetas[0].TileCount); // ordenadas por tamaño descendente, la mayor primero
        Assert.Equal(2, vetas[1].TileCount);
        Assert.Equal(1, vetas[0].CenterX); // centroide real de (0,1,2) = 1
    }

    [Fact]
    public void Find_ConectividadDiagonal_UneTilesQueSoloTocanEnEsquina()
    {
        // (0,0) y (1,1) solo se tocan en diagonal - los 8 vecinos SI los conecta, un flood-fill
        // de 4 vecinos NO lo haria. Esta prueba confirma que se usan los 8 vecinos reales.
        var tiles = new WldTile[3, 3];
        for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) tiles[x, y] = WldTile.Empty;
        tiles[0, 0] = Ore(7);
        tiles[1, 1] = Ore(7);
        tiles[2, 2] = Ore(7);

        var vetas = OreVeinFinder.Find(MakeWorld(tiles), new HashSet<int> { 7 }, limit: 1000, out int total);

        Assert.Equal(1, total); // las tres diagonales forman UNA sola veta conexa
        Assert.Equal(3, Assert.Single(vetas).TileCount);
    }

    [Fact]
    public void Find_TiposDistintosNuncaSeFusionanAunqueEstenPegados()
    {
        var tiles = new WldTile[2, 1];
        tiles[0, 0] = Ore(7);  // cobre
        tiles[1, 0] = Ore(6);  // hierro, pegado al cobre

        var vetas = OreVeinFinder.Find(MakeWorld(tiles), new HashSet<int> { 7, 6 }, limit: 1000, out int total);

        Assert.Equal(2, total); // dos vetas de 1 tile cada una, nunca una veta mixta de 2
        Assert.All(vetas, v => Assert.Equal(1, v.TileCount));
    }

    [Fact]
    public void Find_RespetaElLimite_PeroElTotalRealNoSeRecorta()
    {
        var tiles = new WldTile[20, 1];
        for (int x = 0; x < 20; x++) tiles[x, 0] = WldTile.Empty;
        for (int x = 0; x < 20; x += 2) tiles[x, 0] = Ore(7); // 10 vetas de 1 tile, separadas

        var vetas = OreVeinFinder.Find(MakeWorld(tiles), new HashSet<int> { 7 }, limit: 3, out int total);

        Assert.Equal(10, total);
        Assert.Equal(3, vetas.Count);
    }

    [Fact]
    public void Find_TileInactivoNuncaFormaParteDeUnaVeta()
    {
        var tiles = new WldTile[1, 1];
        tiles[0, 0] = WldTile.Empty;

        var vetas = OreVeinFinder.Find(MakeWorld(tiles), new HashSet<int> { -1 }, limit: 1000, out int total);

        Assert.Equal(0, total);
        Assert.Empty(vetas);
    }

    [Fact]
    public void CountVeinsByType_UnSoloBarrido_CuentaCadaTipoPorSeparado()
    {
        var tiles = new WldTile[3, 1];
        tiles[0, 0] = Ore(7); // cobre: 1 veta de 1 tile
        tiles[1, 0] = Ore(6); // hierro, pegado al cobre: 1 veta de 1 tile propia (no se fusiona)
        tiles[2, 0] = Ore(6); // no toca al hierro de (1,0)? si toca, (1,0)-(2,0) son vecinos - misma veta

        var conteo = OreVeinFinder.CountVeinsByType(MakeWorld(tiles), new HashSet<int> { 7, 6 });

        Assert.Equal(1, conteo[7]); // 1 veta de cobre
        Assert.Equal(1, conteo[6]); // hierro: (1,0) y (2,0) son vecinos reales, UNA sola veta de 2 tiles
    }

    [Fact]
    public void Find_Cancelacion_LanzaOperationCanceledException()
    {
        var tiles = new WldTile[500, 500];
        for (int x = 0; x < 500; x++) for (int y = 0; y < 500; y++) tiles[x, y] = Ore(7);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            OreVeinFinder.Find(MakeWorld(tiles), new HashSet<int> { 7 }, 1000, out _, cts.Token));
    }
}
