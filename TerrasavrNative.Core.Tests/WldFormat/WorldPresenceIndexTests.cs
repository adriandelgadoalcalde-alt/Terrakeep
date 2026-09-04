using TerrasavrNative.Core.WldFormat;
using Xunit;

namespace TerrasavrNative.Core.Tests.WldFormat;

// Punto 4 (advisor Opus, "que solo puedan salir los objetos que tiene ese mundo" - ver
// ESPEC-ui-exploracion.md#10). Mundo sintetico pequeño, mismo patron que WorldSearchTests.cs.
public class WorldPresenceIndexTests
{
    private static WldWorld MakeWorld(WldTile[,] tiles, bool[]? frameImportant = null,
        IReadOnlyList<WldNpc>? npcs = null, IReadOnlyList<WldChest>? chests = null, IReadOnlyList<WldSign>? signs = null,
        IReadOnlyList<WldTileEntity>? tileEntities = null) => new()
    {
        Header = new WldHeader
        {
            Version = 279,
            Pointers = new int[10],
            TileFrameImportant = frameImportant ?? [],
            Title = "Mundo de prueba",
            WorldId = 1,
            TilesHigh = tiles.GetLength(1),
            TilesWide = tiles.GetLength(0),
            SpawnX = 0,
            SpawnY = 0,
            GroundLevel = 100,
            RockLevel = 200,
            DungeonX = 0,
            DungeonY = 0,
        },
        Tiles = tiles,
        Npcs = npcs ?? [],
        Chests = chests ?? [],
        Signs = signs ?? [],
        TileEntities = tileEntities ?? [],
        ShimmeredNpcTypes = new HashSet<int>(),
    };

    [Fact]
    public void Build_CuentaTilesRealesPorTipo_YNuncaLosInactivos()
    {
        var tiles = new WldTile[3, 1];
        tiles[0, 0] = new WldTile(1, 0, 0, 0, 0, 0);
        tiles[1, 0] = new WldTile(1, 0, 0, 0, 0, 0);
        tiles[2, 0] = WldTile.Empty; // Type=-1, IsActive=false

        var idx = WorldPresenceIndex.Build(MakeWorld(tiles));

        Assert.Equal(2, idx.TileCounts[1]);
        Assert.True(idx.HasTile(1));
        Assert.False(idx.HasTile(-1));
        Assert.False(idx.HasTile(2)); // nunca inventado, tipo 2 no aparecio
    }

    [Fact]
    public void Build_ParedYLiquido_SeCuentanIndependientementeDelTile()
    {
        var tiles = new WldTile[1, 1];
        tiles[0, 0] = new WldTile(-1, 5, 2, 100, 0, 0); // sin tile activo, pared 5, lava (2)

        var idx = WorldPresenceIndex.Build(MakeWorld(tiles));

        Assert.True(idx.HasWall(5));
        Assert.Equal(1, idx.WallCounts[5]);
        Assert.True(idx.HasLiquid(2));
        Assert.False(idx.HasLiquid(1)); // agua no presente
    }

    [Fact]
    public void Build_SpriteVariantCounts_SoloParaTilesEnmarcados()
    {
        // TileFrameImportant[21] = true (tipo 21, cofre) - el tile 5 NO esta en la tabla, por
        // tanto no es "framed" segun el criterio real (mismo que usa WldReader).
        var frameImportant = new bool[22];
        frameImportant[21] = true;

        var tiles = new WldTile[2, 1];
        tiles[0, 0] = new WldTile(21, 0, 0, 0, 36, 0); // cofre de oro real (u=36)
        tiles[1, 0] = new WldTile(5, 0, 0, 0, 999, 999); // tipo no enmarcado, U/V no deberian contar

        var idx = WorldPresenceIndex.Build(MakeWorld(tiles, frameImportant));

        Assert.Equal(1, idx.SpriteVariantCounts[(21, 36, 0)]);
        Assert.DoesNotContain((5, (short)999, (short)999), idx.SpriteVariantCounts.Keys);
    }

    [Fact]
    public void Build_Npcs_CuentaInstanciasPorTipo()
    {
        var tiles = new WldTile[1, 1];
        tiles[0, 0] = WldTile.Empty;
        var npcs = new List<WldNpc>
        {
            new() { Id = 17, GivenName = "A", TileX = 1, TileY = 1, Homeless = false, VariationIndex = 0 },
            new() { Id = 17, GivenName = "B", TileX = 2, TileY = 2, Homeless = false, VariationIndex = 0 },
            new() { Id = 22, GivenName = "C", TileX = 3, TileY = 3, Homeless = true, VariationIndex = 0 },
        };

        var idx = WorldPresenceIndex.Build(MakeWorld(tiles, npcs: npcs));

        Assert.Equal(2, idx.NpcCounts[17]);
        Assert.Equal(1, idx.NpcCounts[22]);
        Assert.True(idx.HasNpc(17));
        Assert.False(idx.HasNpc(99)); // tipo real del roster que este mundo no genero
    }

    [Fact]
    public void Build_Cofres_CuentaObjetosPorNetIdYVariantePorLaCasillaDelAncla()
    {
        var frameImportant = new bool[22];
        frameImportant[21] = true;
        var tiles = new WldTile[2, 1];
        tiles[0, 0] = new WldTile(21, 0, 0, 0, 36, 0); // cofre de oro real
        tiles[1, 0] = new WldTile(21, 0, 0, 0, 0, 0);  // cofre de madera real

        var chests = new List<WldChest>
        {
            new() { X = 0, Y = 0, Name = "", Items = [new WldChestItem(4, 1, 0), new WldChestItem(8, 5, 0)] },
            new() { X = 1, Y = 0, Name = "", Items = [new WldChestItem(4, 1, 0)] },
        };

        var idx = WorldPresenceIndex.Build(MakeWorld(tiles, frameImportant, chests: chests));

        Assert.Equal(2, idx.ChestItemCounts[4]); // el NetId 4 aparece en 2 slots reales distintos
        Assert.Equal(1, idx.ChestItemCounts[8]);
        Assert.True(idx.HasChestItem(4));
        Assert.False(idx.HasChestItem(999)); // nunca inventado

        Assert.Equal(1, idx.ChestKindCounts[(21, 36, 0)]); // 1 cofre de oro real
        Assert.Equal(1, idx.ChestKindCounts[(21, 0, 0)]);  // 1 cofre de madera real
    }

    [Fact]
    public void Build_SignCount_EsElRealDeLaListaYaFiltrada()
    {
        var tiles = new WldTile[1, 1];
        tiles[0, 0] = WldTile.Empty;
        var signs = new List<WldSign> { new() { X = 1, Y = 1, Text = "hola" }, new() { X = 2, Y = 2, Text = "adios" } };

        var idx = WorldPresenceIndex.Build(MakeWorld(tiles, signs: signs));

        Assert.Equal(2, idx.SignCount);
    }

    [Fact]
    public void Build_MundoVacio_NoLanzaYDaIndicesVacios()
    {
        var tiles = new WldTile[1, 1];
        tiles[0, 0] = WldTile.Empty;

        var idx = WorldPresenceIndex.Build(MakeWorld(tiles));

        Assert.Empty(idx.TileCounts);
        Assert.Empty(idx.WallCounts);
        Assert.Empty(idx.NpcCounts);
        Assert.Empty(idx.ChestItemCounts);
        Assert.Equal(0, idx.SignCount);
    }

    [Fact]
    public void Build_Cancelacion_LanzaOperationCanceledException()
    {
        var tiles = new WldTile[500, 500];
        for (int x = 0; x < 500; x++) for (int y = 0; y < 500; y++) tiles[x, y] = new WldTile(1, 0, 0, 0, 0, 0);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => WorldPresenceIndex.Build(MakeWorld(tiles), cts.Token));
    }
}
