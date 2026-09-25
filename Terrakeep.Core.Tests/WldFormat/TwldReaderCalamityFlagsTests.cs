using Terrakeep.Core.Nbt;
using Terrakeep.Core.WldFormat;
using Xunit;

namespace Terrakeep.Core.Tests.WldFormat;

// Guia Encargo A "GuiaCalamity" (25-sep-2026): canario que cierra el hueco de cobertura real
// (arquitecto-keep, TASK CONTEXT e5eaea9e-c261-4199-8e7d-060b6054f58d) - antes de este cambio
// TwldReader.Read solo miraba la clave "tiles" del .twld, nunca "modData", asi que la Guia jamas
// podia marcar un jefe de Calamity como derrotado aunque el mundo real lo tuviera. Ninguno de los
// `.twld` reales disponibles en esta maquina trae puesta a true ninguna de las 31 banderas que la
// Guia necesita (ver TwldReaderRealFileTests para el fichero real usado, con progreso real pero
// fuera de esa tabla) - por eso esta fixture es SINTETICA a proposito (mismo formato NBT real,
// construida con el mismo NbtSerializer/TplrFile que usa el resto del proyecto para escribir
// `.tplr`, nunca un blob opaco inventado).
public class TwldReaderCalamityFlagsTests
{
    // Construye un `.twld` minimo pero real en su formato: root NBT con "modData" (lista de
    // TagCompound {mod,name,data}, exactamente como escribe WorldIO.SaveModData real) - incluye
    // una entrada de OTRO ModSystem de antes de la que interesa (DownedBossSystem no es siempre
    // la primera en la lista real) para probar que el filtro por mod+name funciona de verdad, no
    // solo "la primera entrada que aparezca".
    private static byte[] BuildTwld(params string[] downedFlagsGuardadas)
    {
        var flagsList = new NbtList(NbtTagType.String,
            [.. downedFlagsGuardadas.Select(f => (NbtTag)new NbtString(f))]);

        var downedBossSystemEntry = NbtCompound.Of(
            ("mod", new NbtString("CalamityMod")),
            ("name", new NbtString("DownedBossSystem")),
            ("data", NbtCompound.Of(("downedFlags", flagsList))));

        var otroSistemaEntry = NbtCompound.Of(
            ("mod", new NbtString("CalamityMod")),
            ("name", new NbtString("MiscWorldStateSystem")),
            ("data", NbtCompound.Of(("abyssSide", new NbtByte(1)))));

        var modData = new NbtList(NbtTagType.Compound, [otroSistemaEntry, downedBossSystemEntry]);
        var root = NbtCompound.Of(("modData", modData));
        return TplrFile.Write("", root);
    }

    [Fact]
    public void ReadCalamityDownedFlags_TraduceLasClavesGuardadasATextoCanonico()
    {
        byte[] twld = BuildTwld("desertScourge", "astrageldon", "calamitas", "supremeCalamitas");

        var flags = TwldReader.ReadCalamityDownedFlags(twld);

        Assert.Equal(4, flags.Count);
        Assert.Contains("downedDesertScourge", flags);
        Assert.Contains("downedAstrumAureus", flags); // "astrageldon" -> downedAstrumAureus
        Assert.Contains("downedCalamitasClone", flags); // "calamitas" -> downedCalamitasClone (NO downedCalamitas)
        Assert.Contains("downedCalamitas", flags); // "supremeCalamitas" -> downedCalamitas
    }

    // Los 3 mapeos que el arquitecto-keep transcribio mal de memoria (corregidos contra
    // DownedBossSystem.cs real antes de aplicar el arreglo, ver el comentario de
    // TwldReader.CalamityDownedFlagMap) - canario especifico para que no se cuelen otra vez.
    [Fact]
    public void ReadCalamityDownedFlags_CorrigeLosTresMapeosQueElArquitectoTranscribioMal()
    {
        byte[] twld = BuildTwld("plaguebringerGoliath", "starGod", "devourerOfGods");

        var flags = TwldReader.ReadCalamityDownedFlags(twld);

        Assert.Equal(3, flags.Count);
        Assert.Contains("downedPlaguebringer", flags); // NO "plaguebringer" (esa clave no existe en el mod real)
        Assert.Contains("downedAstrumDeus", flags); // NO "astrumDeus"
        Assert.Contains("downedDoG", flags); // NO "dog"
    }

    [Fact]
    public void ReadCalamityDownedFlags_IgnoraClavesQueNingunTramoDeLaGuiaReferencia()
    {
        // "horribleHog" es una bandera REAL de DownedBossSystem (downedHorribleHog) pero ningun
        // tramo de guia_progresion.json la usa como "bandera" - queda fuera de la tabla a
        // proposito (ver el comentario de CalamityDownedFlagMap), se ignora sin romper nada.
        byte[] twld = BuildTwld("horribleHog", "desertScourge");

        var flags = TwldReader.ReadCalamityDownedFlags(twld);

        Assert.Single(flags);
        Assert.Contains("downedDesertScourge", flags);
    }

    [Fact]
    public void ReadCalamityDownedFlags_SinModData_DevuelveConjuntoVacioSinLanzar()
    {
        var root = NbtCompound.Of(("tiles", NbtCompound.Of()));
        byte[] twld = TplrFile.Write("", root);

        var flags = TwldReader.ReadCalamityDownedFlags(twld);

        Assert.Empty(flags);
    }

    [Fact]
    public void ReadCalamityDownedFlags_ModDataSinDownedBossSystem_DevuelveConjuntoVacio()
    {
        var otroSistemaEntry = NbtCompound.Of(
            ("mod", new NbtString("CalamityMod")),
            ("name", new NbtString("MiscWorldStateSystem")),
            ("data", NbtCompound.Of(("abyssSide", new NbtByte(1)))));
        var root = NbtCompound.Of(("modData", new NbtList(NbtTagType.Compound, [otroSistemaEntry])));
        byte[] twld = TplrFile.Write("", root);

        var flags = TwldReader.ReadCalamityDownedFlags(twld);

        Assert.Empty(flags);
    }

    [Fact]
    public void ReadCalamityDownedFlags_ListaVacia_DevuelveConjuntoVacio()
    {
        byte[] twld = BuildTwld();

        var flags = TwldReader.ReadCalamityDownedFlags(twld);

        Assert.Empty(flags);
    }

    // Las 31 claves guardadas por CalamityMod.DownedBossSystem.SaveWorldData real que
    // guia_progresion.json referencia (25 tramos de ambito "calamity") tienen que traducirse TODAS
    // - si esto falla, algun tramo de la Guia se quedaria mudo aunque el jugador ya hubiera
    // derrotado ese jefe.
    [Fact]
    public void ReadCalamityDownedFlags_Las31ClavesQueLaGuiaNecesita_SeTraducenTodas()
    {
        string[] clavesGuardadas =
        [
            "desertScourge", "clam", "crabulon", "hiveMind", "perforator", "slimeGod", "dreadnautilus",
            "cryogen", "aquaticScourge", "brimstoneElemental", "cragmawMire", "astrageldon", "calamitas",
            "greatSandShark", "scavenger", "plaguebringerGoliath", "bumblebirb", "adultEidolonWyrm",
            "polterghast", "mauler", "starGod", "guardians", "providence", "ceaselessVoid", "stormWeaver",
            "signus", "oldDuke", "devourerOfGods", "yharon", "exoMechs", "supremeCalamitas",
        ];
        Assert.Equal(31, clavesGuardadas.Length);

        var flags = TwldReader.ReadCalamityDownedFlags(BuildTwld(clavesGuardadas));

        Assert.Equal(31, flags.Count);
        Assert.Equal(TwldReader.CalamityCanonicalFlagNames.OrderBy(f => f), flags.OrderBy(f => f));
    }
}
