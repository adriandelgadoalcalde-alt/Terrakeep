using Terrakeep.Core.WldFormat;
using Xunit;

namespace Terrakeep.Core.Tests.WldFormat;

// Idea 1 del catalogo de funciones (segunda pieza, bitacora.md 20-sep-2026, quinta ronda):
// "editar que NPCs de pueblo han venido a vivir al mundo (hoy solo lectura)". El limite anterior
// era erroneo - ver el comentario real de WldWriter.WriteNpcs. WriteNpcs es el segundo escritor
// de seccion de ANCHO VARIABLE del proyecto (mismo riesgo real que ya documenta
// WldWriterChestSignTests - "el NPC quedo mal" es grave, pero "la tabla de punteros quedo
// desincronizada y el resto del mundo se volvio ilegible" es mucho peor). Fixture: en vez de
// construir el .wld sintetico byte a byte a mano (como hace WldWriterChestSignTests), aqui se usa
// WldWriter.WriteWorld - ya probado de forma independiente por WldWriterWriteWorldTests - para
// fabricar un mundo valido de verdad con NPCs/cofres/letreros reales dentro; eso deja estas
// pruebas centradas en lo que de verdad importa comprobar de WriteNpcs (que las DEMAS secciones
// sobreviven intactas a un cambio de longitud de la seccion de NPCs), sin repetir la construccion
// manual de cabecera/tiles ya cubierta en otro sitio.
public class WldWriterWriteNpcsTests : IDisposable
{
    private static readonly string WorldsDir = RutasEntornoReal.Documentos(@"tModLoader\Worlds");
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), $"terrakeep-npcs-test-{Guid.NewGuid():N}.wld");

    public void Dispose()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
    }

    private static WldWorld BuildFixtureWorld()
    {
        int wide = 10, high = 8;
        var tiles = new WldTile[wide, high];
        for (int x = 0; x < wide; x++)
            for (int y = 0; y < high; y++)
                tiles[x, y] = y < 3 ? WldTile.Empty : new WldTile(type: 0, wall: 1, liquidType: 0, liquidAmount: 0, u: 0, v: 0);
        // El letrero de abajo (X=2,Y=4) necesita un tile REAL de tipo Sign (55, ver
        // WldWriterChestSignTests) debajo - si no, WldReader.ReadSigns lo descarta como
        // "fantasma" (mismo criterio real del juego) y Signs se queda vacio incluso ANTES de
        // tocar nada de NPCs. Bug real encontrado en esta propia fixture al escribir la prueba
        // de abajo (antes.Signs ya salia vacio, nada que ver con WriteNpcs).
        tiles[2, 4] = new WldTile(type: 55, wall: 0, liquidType: 0, liquidAmount: 0, u: 0, v: 0);

        return new WldWorld
        {
            Header = new WldHeader
            {
                Version = 279, Pointers = [], TileFrameImportant = [], Title = "Mundo sintetico NPCs",
                WorldId = 1, TilesHigh = high, TilesWide = wide, SpawnX = 5, SpawnY = 5,
                GroundLevel = 20, RockLevel = 40, Seed = "npcs", GameMode = 0, DungeonX = 1, DungeonY = 1,
                Time = 0, DayTime = true, MoonPhase = 0, BloodMoon = false, IsEclipse = false, IsCrimson = false,
                DownedBoss1EyeOfCthulhu = false, DownedBoss2EaterOfWorldsOrBrainOfCthulhu = false, DownedBoss3Skeletron = false,
                DownedQueenBee = false, DownedMechBoss1TheDestroyer = false, DownedMechBoss2TheTwins = false,
                DownedMechBoss3SkeletronPrime = false, DownedPlantBoss = false, DownedGolemBoss = false,
                DownedSlimeKingBoss = false, HardMode = false,
                DownedGoblinArmy = false, DownedFrostLegion = false, DownedPirates = false,
            },
            Tiles = tiles,
            Npcs =
            [
                new WldNpc { Id = 17, GivenName = "Guia", TileX = 5, TileY = 4, Homeless = false, VariationIndex = 0 },
                new WldNpc { Id = 18, GivenName = "", TileX = 6, TileY = 4, Homeless = true, VariationIndex = 0 },
            ],
            Chests = [new WldChest { X = 1, Y = 4, Name = "", Items = [new WldChestItem(1, 1, 0)], MaxItems = 40 }],
            Signs = [new WldSign { X = 2, Y = 4, Text = "Letrero real" }],
            TileEntities = [],
            ShimmeredNpcTypes = new HashSet<int>(),
            Bestiary = new WldBestiary { Kills = new Dictionary<string, int>(), Sighted = new HashSet<string>(), Chatted = new HashSet<string>() },
        };
    }

    [Fact]
    public void WriteNpcs_AnadeUnoYQuitaOtro_ElRestoDelMundoSigueIntacto()
    {
        var world = BuildFixtureWorld();
        byte[] original = WldWriter.WriteWorld(world);
        var before = WldReader.Read(original);
        Assert.Equal(2, before.Npcs.Count);

        // Quita el Id 18 (sin casa) y anade el Id 22 (Enfermera, roster vanilla real) recien
        // llegada al punto de aparicion - mismo criterio que usa AddTownNpcCommand.
        var nuevaLista = before.Npcs.Where(n => n.Id != 18).ToList();
        nuevaLista.Add(new WldNpc { Id = 22, GivenName = "", TileX = before.Header.SpawnX, TileY = before.Header.SpawnY, Homeless = true, VariationIndex = 0 });

        byte[] patched = WldWriter.WriteNpcs(original, nuevaLista, before.ShimmeredNpcTypes);
        var after = WldReader.Read(patched);

        Assert.Equal(2, after.Npcs.Count);
        Assert.Contains(after.Npcs, n => n.Id == 17); // el que no se toco sigue
        Assert.DoesNotContain(after.Npcs, n => n.Id == 18); // el quitado ya no esta
        var nueva = Assert.Single(after.Npcs, n => n.Id == 22); // el anadido esta, en el spawn
        Assert.Equal(before.Header.SpawnX, nueva.TileX);
        Assert.Equal(before.Header.SpawnY, nueva.TileY);
        Assert.True(nueva.Homeless);

        // El resto del mundo (tiles, cofres, letreros, cabecera) sobrevive intacto.
        Assert.Equal(before.Header.TilesWide, after.Header.TilesWide);
        Assert.Equal(before.Header.TilesHigh, after.Header.TilesHigh);
        Assert.Equal(before.Header.Title, after.Header.Title);
        for (int x = 0; x < before.Header.TilesWide; x++)
            for (int y = 0; y < before.Header.TilesHigh; y++)
                Assert.Equal(before.Tiles[x, y], after.Tiles[x, y]);
        Assert.Equal(before.Chests.Count, after.Chests.Count);
        Assert.Equal(before.Chests[0].Items[0], after.Chests[0].Items[0]);
        Assert.Equal(before.Signs.Count, after.Signs.Count);
        Assert.Equal(before.Signs[0].Text, after.Signs[0].Text);
        Assert.NotNull(after.Bestiary);
    }

    [Fact]
    public void WriteNpcs_ListaVacia_QuitaTodosLosNpcsSinRomperNada()
    {
        var world = BuildFixtureWorld();
        byte[] original = WldWriter.WriteWorld(world);
        byte[] patched = WldWriter.WriteNpcs(original, [], new HashSet<int>());
        var after = WldReader.Read(patched);

        Assert.Empty(after.Npcs);
        Assert.Single(after.Chests);
        Assert.Single(after.Signs);
    }

    // ---------------------------------------------------------------------------------------
    // Mundo REAL de este PC - misma disciplina que WldWriterChestSignTests (copia a un
    // temporal, nunca el original; releido DE VERDAD desde disco tras escribir).
    // ---------------------------------------------------------------------------------------
    private static string? FindRealWorldWithNpcs()
    {
        if (!Directory.Exists(WorldsDir)) return null;
        foreach (var name in new[] { "roca_negra.wld", "Afueras_de_Larvas_de_gusano.wld", "adriandres.wld", "El_Musgo_de_Accidentes.wld", "ahora_si_que_si.wld", "kmmiu.wld" })
        {
            string path = Path.Combine(WorldsDir, name);
            if (!File.Exists(path)) continue;
            var world = WldReader.Read(File.ReadAllBytes(path));
            if (world.Npcs.Count > 0) return path;
        }
        return null;
    }

    [Fact]
    public void WriteNpcs_MundoReal_PersisteEnDiscoYElRestoSigueIntacto()
    {
        string? sourcePath = FindRealWorldWithNpcs();
        if (sourcePath == null) return; // LIMITE REAL: sin ningun mundo con NPCs en esta maquina, nada que verificar

        File.Copy(sourcePath, _tempPath, overwrite: true);
        byte[] original = File.ReadAllBytes(_tempPath);
        var before = WldReader.Read(original);
        Assert.NotEmpty(before.Npcs);

        int idQuitado = before.Npcs[0].Id;
        var nuevaLista = before.Npcs.Skip(1).ToList();
        nuevaLista.Add(new WldNpc { Id = 353, GivenName = "", TileX = before.Header.SpawnX, TileY = before.Header.SpawnY, Homeless = true, VariationIndex = 0 }); // 353 = Golfista, vanilla real

        byte[] patched = WldWriter.WriteNpcs(original, nuevaLista, before.ShimmeredNpcTypes);
        File.WriteAllBytes(_tempPath, patched);

        var reRead = WldReader.Read(File.ReadAllBytes(_tempPath));
        Assert.Equal(before.Npcs.Count, reRead.Npcs.Count);
        Assert.DoesNotContain(reRead.Npcs, n => n.Id == idQuitado);
        Assert.Contains(reRead.Npcs, n => n.Id == 353);

        // Integridad del resto del mundo real: mismo numero de cofres/letreros, mismas
        // dimensiones, mismo titulo.
        Assert.Equal(before.Chests.Count, reRead.Chests.Count);
        Assert.Equal(before.Signs.Count, reRead.Signs.Count);
        Assert.Equal(before.Header.TilesWide, reRead.Header.TilesWide);
        Assert.Equal(before.Header.TilesHigh, reRead.Header.TilesHigh);
        Assert.Equal(before.Header.Title, reRead.Header.Title);

        // La rejilla de tiles entera (lo mas caro de corromper con un offset mal calculado) sigue
        // exactamente igual - muestreo real, no cada tile, por coste.
        int stepX = Math.Max(1, before.Header.TilesWide / 200);
        int stepY = Math.Max(1, before.Header.TilesHigh / 200);
        for (int x = 0; x < before.Header.TilesWide; x += stepX)
            for (int y = 0; y < before.Header.TilesHigh; y += stepY)
                Assert.Equal(before.Tiles[x, y], reRead.Tiles[x, y]);
    }
}
