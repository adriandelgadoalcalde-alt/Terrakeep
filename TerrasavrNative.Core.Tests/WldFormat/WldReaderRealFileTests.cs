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

    [Theory]
    [MemberData(nameof(RealWldFiles))]
    public void Read_RealWorld_GroundRockLevelsAndSpawnAreSane(string path)
    {
        if (!File.Exists(path)) return;

        var world = WldReader.Read(File.ReadAllBytes(path));
        var h = world.Header;
        output.WriteLine($"{Path.GetFileName(path)}: spawn=({h.SpawnX},{h.SpawnY}) groundLevel={h.GroundLevel} rockLevel={h.RockLevel}");

        // Orden real de las capas por profundidad: superficie (GroundLevel) esta siempre por
        // encima (Y menor) de la roca (RockLevel), y ambas caen dentro del alto del mundo.
        Assert.InRange(h.GroundLevel, 0, h.TilesHigh);
        Assert.InRange(h.RockLevel, 0, h.TilesHigh);
        Assert.True(h.GroundLevel < h.RockLevel, $"GroundLevel ({h.GroundLevel}) deberia estar por encima de RockLevel ({h.RockLevel})");

        // El punto de aparicion siempre esta dentro del mundo, y por encima o cerca de la
        // superficie (nunca en las profundidades) - Terraria coloca el spawn en la superficie.
        Assert.InRange(h.SpawnX, 0, h.TilesWide);
        Assert.InRange(h.SpawnY, 0, h.TilesHigh);
        Assert.True(h.SpawnY < h.RockLevel, $"El spawn ({h.SpawnY}) deberia estar por encima de la roca ({h.RockLevel})");
    }

    // H3-10 (tercera auditoria de Opus, Fable): miel (codigo real en disco 3) y Shimmer
    // (codigo sintetico propio 4, solo en memoria - ver el comentario real en WldReader.cs)
    // compartian antes el mismo codigo 3 - esta prueba confirma contra mundos REALES que 4
    // nunca aparece si la version del mundo es anterior a 269 (Shimmer ni existia) y que,
    // cuando aparece, es un codigo real DISTINTO de 3 (nunca los mezcla).
    [Theory]
    [MemberData(nameof(RealWldFiles))]
    public void Read_RealWorld_ShimmerYMielSonCodigosDistintos(string path)
    {
        if (!File.Exists(path)) return;

        var world = WldReader.Read(File.ReadAllBytes(path));
        int honeyCount = 0, shimmerCount = 0;
        for (int x = 0; x < world.Tiles.GetLength(0); x++)
        {
            for (int y = 0; y < world.Tiles.GetLength(1); y++)
            {
                byte lt = world.Tiles[x, y].LiquidType;
                Assert.InRange(lt, (byte)0, (byte)4); // nunca un codigo desconocido
                if (lt == 3) honeyCount++;
                if (lt == 4) shimmerCount++;
            }
        }
        output.WriteLine($"{Path.GetFileName(path)}: version={world.Header.Version}, miel(3)={honeyCount}, Shimmer(4)={shimmerCount}");

        if (world.Header.Version < 269)
            Assert.Equal(0, shimmerCount); // Shimmer no existia todavia en esa version
    }

    // H4-08 (cuarta auditoria de Opus, Fable): la lectura barata para el lanzador de mundos
    // debe devolver EXACTAMENTE el mismo titulo/dimensiones que la lectura completa - nunca una
    // aproximacion, y sin pagar el coste real de decodificar tiles/NPCs para conseguirlo.
    [Theory]
    [MemberData(nameof(RealWldFiles))]
    public void ReadHeader_RealWorld_CoincideConElHeaderDeLaLecturaCompleta(string path)
    {
        if (!File.Exists(path)) return;

        var bytes = File.ReadAllBytes(path);
        var fullHeader = WldReader.Read(bytes).Header;
        var cheapHeader = WldReader.ReadHeader(bytes);

        Assert.Equal(fullHeader.Title, cheapHeader.Title);
        Assert.Equal(fullHeader.TilesWide, cheapHeader.TilesWide);
        Assert.Equal(fullHeader.TilesHigh, cheapHeader.TilesHigh);
        Assert.Equal(fullHeader.Version, cheapHeader.Version);
    }

    // H6-08/H6-09 (sexta auditoria de Opus): townNpcVariationIndex real (Gato/Perro/Conejo de
    // pueblo, 0-5) y el set global de tipos "shimmerizados" (WorldFile.LoadNPCs real) ahora se
    // leen de verdad en vez de descartarse - contra un mundo real, sin asumir ningun valor
    // concreto (puede que este mundo en particular no tenga ninguna mascota de pueblo todavia),
    // solo que la lectura no rompe y los valores caen en un rango sano.
    [Theory]
    [MemberData(nameof(RealWldFiles))]
    public void Read_RealWorld_VariationIndexYShimmerSonSanos(string path)
    {
        if (!File.Exists(path)) return;

        var world = WldReader.Read(File.ReadAllBytes(path));
        output.WriteLine($"{Path.GetFileName(path)}: ShimmeredNpcTypes=[{string.Join(",", world.ShimmeredNpcTypes)}]");

        foreach (var npc in world.Npcs)
        {
            // Rango generoso a proposito (el campo real es 0-5 para Gato/Perro/Conejo de
            // pueblo y 0 para el resto, pero esta prueba solo quiere pillar una lectura
            // realmente rota - offset mal puesto, leyendo basura - no imponer la regla exacta
            // del juego).
            Assert.InRange(npc.VariationIndex, 0, 1000);
            output.WriteLine($"  {npc.GivenName} (tipo {npc.Id}): variationIndex={npc.VariationIndex}");
        }
    }

    // Punto 4 (advisor Opus, buscador de objetos del mundo), Fase 2 de
    // ESPEC-buscador-mundo-tedit.md: prueba de humo real de ReadChests/ReadSigns contra un .wld
    // REAL de este PC - el complemento real de haber verificado el formato byte a byte contra
    // World.FileV2.cs de TEdit (comentario de WldChest.cs/WldSign.cs). Si el offset de
    // ChestsSectionOffset/SignsSectionOffset o el `maxItems` por version estuvieran mal, esto se
    // manifiesta de forma inconfundible (EndOfStreamException/datos basura), igual que ya
    // documenta la cabecera de este fichero para los tiles.
    [Theory]
    [MemberData(nameof(RealWldFiles))]
    public void Read_RealWorld_CofresYLetrerosSonSanos(string path)
    {
        if (!File.Exists(path)) return;

        var world = WldReader.Read(File.ReadAllBytes(path));
        output.WriteLine($"{Path.GetFileName(path)}: cofres={world.Chests.Count}, letreros={world.Signs.Count}");

        foreach (var chest in world.Chests)
        {
            Assert.InRange(chest.X, 0, world.Header.TilesWide);
            Assert.InRange(chest.Y, 0, world.Header.TilesHigh);
            foreach (var item in chest.Items)
            {
                Assert.True(item.Stack > 0, "un slot vacio (stack<=0) nunca deberia haberse guardado como WldChestItem");
                Assert.True(item.NetId != 0, "un objeto real siempre tiene NetId != 0");
            }
        }

        // TileType.cs real de TEdit: Sign=55, GraveMarker=85, AnnouncementBox=425, TatteredSign=573.
        var signTileTypes = new HashSet<int> { 55, 85, 425, 573 };
        foreach (var sign in world.Signs)
        {
            Assert.InRange(sign.X, 0, world.Header.TilesWide - 1);
            Assert.InRange(sign.Y, 0, world.Header.TilesHigh - 1);
            var tile = world.Tiles[sign.X, sign.Y];
            Assert.True(tile.IsActive, $"letrero en ({sign.X},{sign.Y}) sobre un tile inactivo - el filtro real de TEdit (IsActive && IsSign()) no deberia haberlo dejado pasar");
            Assert.Contains(tile.Type, signTileTypes);
        }
    }

    // Un mundo real jugado tiene cofres de verdad (los del propio spawn, mínimo) - si esta
    // prueba diera 0 en TODOS los mundos reales de este PC seria señal de que el offset esta
    // mal (aterriza en una seccion equivocada y "totalChests" sale 0 por casualidad de los
    // bytes que encuentra ahi), no de que el mundo no tenga ninguno.
    [Fact]
    public void Read_AlMenosUnMundoReal_TieneCofresDeVerdad()
    {
        var encontrados = RealWldFiles().Select(a => (string)a[0]).Where(File.Exists)
            .Select(p => WldReader.Read(File.ReadAllBytes(p)).Chests.Count).ToList();
        if (encontrados.Count == 0) return; // ninguno de los dos mundos reales esta en este PC - nada que comprobar
        Assert.Contains(encontrados, c => c > 0);
    }
}
