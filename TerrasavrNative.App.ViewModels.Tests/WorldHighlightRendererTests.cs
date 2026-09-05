using System.Windows.Media;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.WldFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// A9-10-MIELTODA (informe de pulido final, cierra E6/E7): "una cantidad absurda de mieles" - el
// bug real era que la LISTA (topada a 1000) y el MAPA compartian el mismo tope en la practica.
// WorldHighlightRenderer pinta el nucleo (opacidad completa, sin contar el halo de alrededor) en
// CADA tile que casa, sin tope alguno - el numero de pixeles a opacidad completa tiene que ser
// EXACTAMENTE el recuento real de tiles, marque uno o marque decenas de miles.
public sealed class WorldHighlightRendererTests
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
            Seed = "semilla-sintetica",
            GameMode = 0,
            DungeonX = 0,
            DungeonY = 0,
        },
        Tiles = tiles,
        Npcs = [],
        Chests = [],
        Signs = [],
        TileEntities = [],
        ShimmeredNpcTypes = new HashSet<int>(),
    };

    private static int CountNucleoPixels(System.Windows.Media.Imaging.BitmapSource bitmap, byte expectedAlpha)
    {
        int w = bitmap.PixelWidth, h = bitmap.PixelHeight;
        var pixels = new byte[h * w * 4];
        bitmap.CopyPixels(pixels, w * 4, 0);
        int count = 0;
        for (int i = 3; i < pixels.Length; i += 4)
            if (pixels[i] == expectedAlpha) count++;
        return count;
    }

    [Fact]
    public void RenderLiquids_MarcaTODOSLosTilesDeMiel_SinTope()
    {
        int w = 40, h = 40;
        var tiles = new WldTile[w, h];
        for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) tiles[x, y] = WldTile.Empty;

        var rnd = new Random(20260904);
        int mielEsperada = 0;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (rnd.NextDouble() < 0.3)
                {
                    tiles[x, y] = new WldTile(-1, 0, 3, 255, 0, 0); // miel
                    mielEsperada++;
                }

        var world = MakeWorld(tiles);
        var color = Colors.Orange;
        var bitmap = WorldHighlightRenderer.RenderLiquids(world, new HashSet<byte> { 3 }, color);

        int nucleoCount = CountNucleoPixels(bitmap, color.A);

        Assert.True(mielEsperada > 100, "la muestra sintetica debe generar bastante mas de 1000 tiles potenciales para que el hallazgo real (tope compartido lista/mapa) sea representativo");
        Assert.Equal(mielEsperada, nucleoCount);
    }

    [Fact]
    public void RenderWalls_MarcaTODOSLosTilesDeLaPared_SinTope()
    {
        int w = 30, h = 30;
        var tiles = new WldTile[w, h];
        for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) tiles[x, y] = WldTile.Empty;

        int muroEsperado = 0;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if ((x + y) % 3 == 0)
                {
                    tiles[x, y] = new WldTile(-1, 7, 0, 0, 0, 0);
                    muroEsperado++;
                }

        var world = MakeWorld(tiles);
        var color = Colors.Orange;
        var bitmap = WorldHighlightRenderer.RenderWalls(world, new HashSet<int> { 7 }, color);

        Assert.Equal(muroEsperado, CountNucleoPixels(bitmap, color.A));
    }

    [Fact]
    public void Render_TilesSinSeleccion_NoPintaNadaAOpacidadCompleta()
    {
        var tiles = new WldTile[5, 5];
        for (int x = 0; x < 5; x++) for (int y = 0; y < 5; y++) tiles[x, y] = new WldTile(1, 0, 0, 0, 0, 0);

        var bitmap = WorldHighlightRenderer.Render(MakeWorld(tiles), new HashSet<int> { 999 }, Colors.Orange);

        Assert.Equal(0, CountNucleoPixels(bitmap, Colors.Orange.A));
    }
}
