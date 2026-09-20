using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.WldFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Catalogo de ideas Keep, idea 8 ("Informe y comparador de mundos" - "salida como tarjeta
// compartible (PNG/HTML)"), reconsiderada a peticion explicita del coordinador (20-sep-2026):
// "añade la exportacion como tarjeta PNG/HTML a WorldCompareViewModel". Dos mundos sinteticos
// reales (WldWriter.WriteWorld, mismo mecanismo real ya probado por WldWriterWriteWorldTests -
// nunca un .wld inventado a mano byte a byte) con una diferencia real y conocida (tamaño y
// dificil/no dificil) para verificar que las dos tarjetas de verdad reflejan esos datos.
public sealed class WorldCompareCardExportTests
{
    private static WldWorld MundoSintetico(int wide, int high, string titulo, bool hardMode)
    {
        var tiles = new WldTile[wide, high];
        for (int x = 0; x < wide; x++)
            for (int y = 0; y < high; y++)
                tiles[x, y] = y < 5 ? WldTile.Empty : new WldTile(type: 0, wall: 1, liquidType: 0, liquidAmount: 0, u: 0, v: 0);

        return new WldWorld
        {
            Header = new WldHeader
            {
                Version = 279,
                Pointers = [],
                TileFrameImportant = [],
                Title = titulo,
                WorldId = 1,
                TilesHigh = high,
                TilesWide = wide,
                SpawnX = 5,
                SpawnY = 5,
                GroundLevel = 5,
                RockLevel = high / 2,
                Seed = "keepqa-idea8",
                GameMode = 0,
                DungeonX = wide / 2,
                DungeonY = high / 2,
                Time = 0,
                DayTime = true,
                MoonPhase = 0,
                BloodMoon = false,
                IsEclipse = false,
                IsCrimson = false,
                DownedBoss1EyeOfCthulhu = false,
                DownedBoss2EaterOfWorldsOrBrainOfCthulhu = false,
                DownedBoss3Skeletron = false,
                DownedQueenBee = false,
                DownedMechBoss1TheDestroyer = false,
                DownedMechBoss2TheTwins = false,
                DownedMechBoss3SkeletronPrime = false,
                DownedPlantBoss = false,
                DownedGolemBoss = false,
                DownedGoblinArmy = false,
                DownedFrostLegion = false,
                DownedPirates = false,
                HardMode = hardMode,
            },
            Tiles = tiles,
            Npcs = [new WldNpc { Id = 17, GivenName = "Guide", TileX = 10, TileY = 4, Homeless = false, VariationIndex = 0 }],
            Chests = [],
            Signs = [],
            TileEntities = [],
            ShimmeredNpcTypes = new HashSet<int>(),
            Bestiary = new WldBestiary { Kills = new Dictionary<string, int>(), Sighted = new HashSet<string>(), Chatted = new HashSet<string>() },
        };
    }

    private static async Task<(WorldCompareViewModel Vm, string PathA, string PathB)> NuevoComparadorCargado()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"worldcompare-card-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string pathA = Path.Combine(dir, "MundoPequeno.wld");
        string pathB = Path.Combine(dir, "MundoGrande.wld");
        File.WriteAllBytes(pathA, WldWriter.WriteWorld(MundoSintetico(20, 15, "MundoPequeno", hardMode: false)));
        File.WriteAllBytes(pathB, WldWriter.WriteWorld(MundoSintetico(40, 25, "MundoGrande", hardMode: true)));

        var vm = new WorldCompareViewModel();
        await vm.LoadAAsync(pathA);
        await vm.LoadBAsync(pathB);
        return (vm, pathA, pathB);
    }

    [Fact]
    public async Task ExportarTarjetaHtml_ContieneLosNombresYLosValoresRealesDeLosDosMundos()
    {
        var (vm, pathA, _) = await NuevoComparadorCargado();
        string dir = Path.GetDirectoryName(pathA)!;
        string htmlPath = Path.Combine(dir, "tarjeta.html");
        try
        {
            vm.ExportCardToHtml(htmlPath);

            Assert.True(File.Exists(htmlPath));
            string html = File.ReadAllText(htmlPath);
            Assert.Contains("MundoPequeno", html);
            Assert.Contains("MundoGrande", html);
            // El tamaño real (20x15 vs 40x25) es una fila DIFERENTE conocida - tiene que verse
            // reflejada de verdad, no solo la cabecera con los nombres. Los digitos (ASCII puro)
            // sobreviven intactos al HtmlEncode real de "×" (WebUtility.HtmlEncode SI convierte
            // ese caracter a una referencia numerica, comportamiento real y correcto - no hace
            // falta reproducirlo aqui para probar que el DATO llego).
            Assert.Contains("20", html);
            Assert.Contains("15", html);
            Assert.Contains("40", html);
            Assert.Contains("25", html);
            // Fila realmente distinta (dificil) marcada con la clase real "diff".
            Assert.Contains("class=\"diff\"", html);
        }
        finally { if (File.Exists(htmlPath)) File.Delete(htmlPath); Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task ExportarTarjetaPng_ProduceUnFicheroRealConElAltoEsperadoSegunElNumeroDeFilas()
    {
        var (vm, pathA, _) = await NuevoComparadorCargado();
        string dir = Path.GetDirectoryName(pathA)!;
        string pngPath = Path.Combine(dir, "tarjeta.png");
        try
        {
            vm.ExportCardToPng(pngPath);

            Assert.True(File.Exists(pngPath));
            using var stream = File.OpenRead(pngPath);
            var decoder = new System.Windows.Media.Imaging.PngBitmapDecoder(stream,
                System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            Assert.Equal(640, frame.PixelWidth);
            // Alto real = cabecera fija + una fila por StatRows - nunca un numero fijo inventado,
            // se recalcula aqui con el MISMO StatRows.Count real que ya uso ExportCardToPng.
            int altoEsperado = 60 + vm.StatRows.Count * 26 + 20;
            Assert.Equal(altoEsperado, frame.PixelHeight);

            // Al menos un pixel no transparente de verdad (la tarjeta no salio en blanco).
            var pixeles = new byte[frame.PixelWidth * frame.PixelHeight * 4];
            frame.CopyPixels(pixeles, frame.PixelWidth * 4, 0);
            Assert.Contains(pixeles, b => b != 0);
        }
        finally { if (File.Exists(pngPath)) File.Delete(pngPath); Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task SinLosDosMundosCargados_NoExportaNada()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"worldcompare-card-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string pathA = Path.Combine(dir, "Solo.wld");
        File.WriteAllBytes(pathA, WldWriter.WriteWorld(MundoSintetico(20, 15, "Solo", hardMode: false)));
        string htmlPath = Path.Combine(dir, "no-deberia-existir.html");
        try
        {
            var vm = new WorldCompareViewModel();
            await vm.LoadAAsync(pathA); // solo A, nunca B

            vm.ExportCardToHtml(htmlPath);

            Assert.False(File.Exists(htmlPath));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
