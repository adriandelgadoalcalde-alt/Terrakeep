using TerrasavrNative.Core.WldFormat;
using Xunit;
using Xunit.Abstractions;

namespace TerrasavrNative.Core.Tests.WldFormat;

// Prueba de humo contra mundos .wld REALES de este PC. Si el RLE de tiles esta mal
// implementado, esto se manifiesta de forma inconfundible: la lectura no termina donde debe
// (EndOfStreamException/datos basura) o el numero de tiles activos es absurdo - no hace falta
// verificar pixel a pixel para tener una señal de alarma fiable.
public class WldReaderRealFileTests(ITestOutputHelper output)
{
    private const string WorldsDir = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds";

    public static IEnumerable<object[]> RealWldFiles()
    {
        if (!Directory.Exists(WorldsDir)) yield break;
        yield return [Path.Combine(WorldsDir, "El_Musgo_de_Accidentes.wld")];
        yield return [Path.Combine(WorldsDir, "adriandres.wld")];
    }

    [Theory]
    [MemberData(nameof(RealWldFiles))]
    public void Read_RealWorld_ParsesHeaderAndFillsEntireGrid(string path)
    {
        if (!File.Exists(path)) return;

        var world = WldReader.Read(File.ReadAllBytes(path));

        output.WriteLine($"{Path.GetFileName(path)}: '{world.Header.Title}', {world.Header.TilesWide}x{world.Header.TilesHigh}, version={world.Header.Version}, NPCs={world.Npcs.Count}");

        Assert.False(string.IsNullOrWhiteSpace(world.Header.Title));
        // Un mundo real de Terraria (Pequeño/Mediano/Grande) siempre cae en este rango.
        Assert.InRange(world.Header.TilesWide, 1000, 20000);
        Assert.InRange(world.Header.TilesHigh, 500, 10000);
        Assert.Equal(world.Header.TilesWide, world.Tiles.GetLength(0));
        Assert.Equal(world.Header.TilesHigh, world.Tiles.GetLength(1));

        int activeCount = 0, wallCount = 0, liquidCount = 0;
        for (int x = 0; x < world.Tiles.GetLength(0); x++)
        {
            for (int y = 0; y < world.Tiles.GetLength(1); y++)
            {
                var t = world.Tiles[x, y];
                if (t.IsActive) activeCount++;
                if (t.Wall != 0) wallCount++;
                if (t.LiquidAmount > 0) liquidCount++;
            }
        }
        long totalTiles = (long)world.Header.TilesWide * world.Header.TilesHigh;
        output.WriteLine($"  activos={activeCount} ({100.0 * activeCount / totalTiles:F1}%), con pared={wallCount}, con liquido={liquidCount}");

        // Un mundo real de Terraria SIEMPRE tiene una fraccion sustancial de tiles activos
        // (tierra/piedra maciza en gran parte del subsuelo) - si el RLE estuviera mal, esto
        // saldria en 0%, 100% o un numero sin sentido.
        double activeRatio = (double)activeCount / totalTiles;
        Assert.InRange(activeRatio, 0.15, 0.95);
    }

    [Theory]
    [MemberData(nameof(RealWldFiles))]
    public void Read_RealWorld_NpcsHaveSaneCoordinates(string path)
    {
        if (!File.Exists(path)) return;

        var world = WldReader.Read(File.ReadAllBytes(path));

        foreach (var npc in world.Npcs)
        {
            Assert.InRange(npc.TileX, 0, world.Header.TilesWide);
            Assert.InRange(npc.TileY, 0, world.Header.TilesHigh);
            Assert.False(string.IsNullOrEmpty(npc.GivenName) && npc.Id == 0);
        }
    }
}
