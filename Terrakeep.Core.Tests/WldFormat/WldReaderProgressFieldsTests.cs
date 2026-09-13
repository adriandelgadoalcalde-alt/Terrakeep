using Terrakeep.Core.WldFormat;
using Xunit;
using Xunit.Abstractions;

namespace Terrakeep.Core.Tests.WldFormat;

// Editor de mundos v1 (14-sep-2026, guia real de bitacora.md 13-sep-2026): prueba de humo contra
// mundos .wld REALES de este PC para Time/DayTime/MoonPhase/BloodMoon/IsEclipse, el bloque de
// banderas de progreso (IsCrimson..HardMode) y el bestiario (Pointers[8], version>=210) - mismo
// criterio que WldReaderRealFileTests: si el offset de cualquiera de estos campos estuviera mal,
// se manifiesta de forma inconfundible (EndOfStreamException/valores absurdos en el resto de la
// lectura que viene DESPUES, como los NPCs), no hace falta conocer el valor exacto de antemano.
public class WldReaderProgressFieldsTests(ITestOutputHelper output)
{
    private const string WorldsDir = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds";

    public static IEnumerable<object[]> RealWldFiles()
    {
        if (!Directory.Exists(WorldsDir)) yield break;
        yield return [Path.Combine(WorldsDir, "El_Musgo_de_Accidentes.wld")];
        yield return [Path.Combine(WorldsDir, "adriandres.wld")];
        yield return [Path.Combine(WorldsDir, "roca_negra.wld")];
        yield return [Path.Combine(WorldsDir, "Afueras_de_Larvas_de_gusano.wld")];
    }

    [Theory]
    [MemberData(nameof(RealWldFiles))]
    public void ReadHeader_RealWorld_TiempoYLunaSonSanos(string path)
    {
        if (!File.Exists(path)) return;

        var h = WldReader.ReadHeader(File.ReadAllBytes(path));
        output.WriteLine($"{Path.GetFileName(path)}: Time={h.Time} DayTime={h.DayTime} MoonPhase={h.MoonPhase} BloodMoon={h.BloodMoon} IsEclipse={h.IsEclipse}");

        // Rango real del juego (Main.dayLength=54000, Main.nightLength=32400 - confirmado en
        // Terraria/Main.cs decompilado): el reloj guardado nunca supera el tramo mas largo de
        // los dos, sea cual sea DayTime.
        Assert.InRange(h.Time, 0, 54000);
        Assert.InRange(h.MoonPhase, 0, 7); // 8 fases reales
    }

    [Theory]
    [MemberData(nameof(RealWldFiles))]
    public void ReadHeader_RealWorld_BanderasDeProgresoSonCoherentes(string path)
    {
        if (!File.Exists(path)) return;

        var h = WldReader.ReadHeader(File.ReadAllBytes(path));
        output.WriteLine($"{Path.GetFileName(path)}: version={h.Version} HardMode={h.HardMode} " +
            $"EoC={h.DownedBoss1EyeOfCthulhu} EoW/BoC={h.DownedBoss2EaterOfWorldsOrBrainOfCthulhu} Skeletron={h.DownedBoss3Skeletron} " +
            $"QueenBee={h.DownedQueenBee} Destroyer={h.DownedMechBoss1TheDestroyer} Twins={h.DownedMechBoss2TheTwins} " +
            $"Prime={h.DownedMechBoss3SkeletronPrime} Plantera={h.DownedPlantBoss} Golem={h.DownedGolemBoss} SlimeKing={h.DownedSlimeKingBoss}");

        // Regla real del juego (WorldGen.hardMode solo se activa cuando cae algun mecanico, o el
        // propio HardMode ya viene marcado desde la creacion en mundos especiales) - la
        // comprobacion real y estable es la inversa: NINGUN jefe de hardmode puede estar caido en
        // un mundo que todavia NO es hardmode (eso si seria una lectura rota, un mecanico solo
        // aparece tras entrar en hardmode).
        if (!h.HardMode)
        {
            Assert.False(h.DownedMechBoss1TheDestroyer, "El Destructor no puede estar caido en un mundo que no es Hardmode todavia");
            Assert.False(h.DownedMechBoss2TheTwins, "Los Gemelos no pueden estar caidos en un mundo que no es Hardmode todavia");
            Assert.False(h.DownedMechBoss3SkeletronPrime, "Skeletron Prime no puede estar caido en un mundo que no es Hardmode todavia");
            Assert.False(h.DownedPlantBoss, "Plantera no puede estar caida en un mundo que no es Hardmode todavia");
            Assert.False(h.DownedGolemBoss, "Golem no puede estar caido en un mundo que no es Hardmode todavia");
        }
        if (h.Version < 118) Assert.Null(h.DownedSlimeKingBoss);
    }

    [Theory]
    [MemberData(nameof(RealWldFiles))]
    public void Read_RealWorld_TrasElBloqueDeBanderasLosNpcsSiguenSanos(string path)
    {
        if (!File.Exists(path)) return;

        // Control cruzado real: si el nuevo bloque de banderas desincronizara el stream, la
        // seccion de NPCs (alcanzada por su PROPIO puntero absoluto, Pointers[4]) seguiria
        // llegando bien - pero si en cambio WldHeader.TilesSectionOffset/etc. estuvieran mal
        // calculados por algun efecto indirecto, esto lo pillaria igual que ya hacian las
        // pruebas de humo existentes de WldReaderRealFileTests.
        var world = WldReader.Read(File.ReadAllBytes(path));
        foreach (var npc in world.Npcs)
        {
            Assert.InRange(npc.TileX, 0, world.Header.TilesWide);
            Assert.InRange(npc.TileY, 0, world.Header.TilesHigh);
        }
        output.WriteLine($"{Path.GetFileName(path)}: NPCs={world.Npcs.Count} (sanos tras leer el bloque de banderas)");
    }

    // Bestiario (Journey's End, version>=210) - punto 6 de bitacora.md (paneles de progreso):
    // confirma que Pointers[8] es de verdad el inicio real de la seccion (no Pointers[9], ver el
    // comentario de WldHeader.BestiarySectionOffset) contra mundos reales de este PC.
    [Theory]
    [MemberData(nameof(RealWldFiles))]
    public void Read_RealWorld_BestiarioSeLeeSinRomperNadaMas(string path)
    {
        if (!File.Exists(path)) return;

        var world = WldReader.Read(File.ReadAllBytes(path));
        output.WriteLine($"{Path.GetFileName(path)}: version={world.Header.Version} Bestiary={(world.Bestiary is null ? "null (version<210)" : $"Kills={world.Bestiary.Kills.Count} Sighted={world.Bestiary.Sighted.Count} Chatted={world.Bestiary.Chatted.Count}")}");

        if (world.Header.Version < 210)
        {
            Assert.Null(world.Bestiary);
            return;
        }

        Assert.NotNull(world.Bestiary);
        // Todo kill count real es > 0 (nunca se guardaria una entrada con 0 muertes) y toda
        // clave es un texto no vacio.
        foreach (var (key, kills) in world.Bestiary.Kills)
        {
            Assert.False(string.IsNullOrEmpty(key));
            Assert.True(kills > 0, $"'{key}' tiene {kills} muertes guardadas - un contador real nunca se persiste en 0");
        }
    }

    // Un mundo real y jugado de verdad (no uno recien creado) tiene NPCs muertos de sobra - si
    // esta prueba diera 0 en TODOS los mundos reales de este PC seria señal de que
    // BestiarySectionOffset apunta al sitio equivocado, igual que el resto de pruebas
    // "AlMenosUnMundoReal" de este proyecto (ver WldReaderRealFileTests).
    [Fact]
    public void Read_AlMenosUnMundoReal_TieneAlgunaMuerteRegistradaEnElBestiario()
    {
        var encontrados = RealWldFiles().Select(a => (string)a[0]).Where(File.Exists)
            .Select(p => WldReader.Read(File.ReadAllBytes(p)))
            .Where(w => w.Header.Version >= 210)
            .Select(w => w.Bestiary!.Kills.Count).ToList();
        if (encontrados.Count == 0) return;
        Assert.Contains(encontrados, c => c > 0);
    }
}
