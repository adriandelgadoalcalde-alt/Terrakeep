using Terrakeep.Core.WldFormat;
using Xunit;

namespace Terrakeep.Core.Tests.WldFormat;

// Editor de mundos v1 (14-sep-2026, guia real de bitacora.md 13-sep-2026): PatchSpawnPoint/
// PatchTimeAndMoon/PatchBossFlags, los tres nuevos escritores reales de .wld (junto a
// PatchGameMode, unico que existia antes). Mismo riesgo real que documenta la cabecera de
// WldWriterTests - un fallo aqui puede corromper el archivo de mundo de un usuario, no solo un
// valor en memoria -, mismo criterio de prueba mas estricta (byte a byte contra el original).
public class WldWriterProgressPatchTests
{
    // Cabecera sintetica COMPLETA (hasta HardMode inclusive) - mismo orden de campos exacto que
    // WldReader.ReadHeader, version real reciente (todos los campos opcionales por version
    // presentes). Parametros con valor por defecto "todo en cero/false" para que cada prueba
    // solo tenga que nombrar lo que le importa.
    private static byte[] BuildHeaderBytes(
        uint version = 279, int spawnX = 100, int spawnY = 200, double time = 0, bool dayTime = true,
        int moonPhase = 0, bool bloodMoon = false, bool isEclipse = false,
        bool isCrimson = false, bool downedBoss1 = false, bool downedBoss2 = false, bool downedBoss3 = false,
        bool downedQueenBee = false, bool downedMech1 = false, bool downedMech2 = false, bool downedMech3 = false,
        bool downedPlant = false, bool downedGolem = false, bool downedSlimeKing = false, bool hardMode = false)
    {
        var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        w.Write(version);
        w.Write("relogic".ToCharArray());
        w.Write((byte)2);
        w.Write((uint)1);
        w.Write((long)0);

        var pointers = new int[] { 0, 0, 0, 0, 0 };
        w.Write((short)pointers.Length);
        foreach (int p in pointers) w.Write(p);
        w.Write((short)0); // tileFrameImportant vacio

        w.Write("Mi Mundo de Verdad");
        w.Write("-987654321"); // seed (version != 179)
        w.Write(new byte[8]); // WorldGenVersion
        if (version >= 181) w.Write(new byte[16]); // WorldGUID
        w.Write(1); // worldId
        w.Write(new byte[16]); // Left/Right/Top/BottomWorld
        w.Write(1000); // tilesHigh
        w.Write(2000); // tilesWide

        if (version >= 209)
        {
            w.Write(0); // GameMode (Int32)
            if (version >= 222) w.Write(false);
            if (version >= 227) w.Write(false);
            if (version >= 238) w.Write(false);
            if (version >= 239) w.Write(false);
            if (version >= 241) w.Write(false);
            if (version >= 249) w.Write(false);
            if (version >= 266) w.Write(false);
            if (version >= 267) w.Write(false);
            if (version >= 302) w.Write(false);
        }
        else if (version >= 112)
        {
            w.Write(false); // GameMode (bool)
        }

        if (version >= 141) w.Write(new byte[8]); // CreationTime
        if (version >= 284) w.Write(new byte[8]); // LastPlayed
        w.Write((byte)0); // MoonType
        w.Write(new byte[4 * 3]); // TreeX
        w.Write(new byte[4 * 4]); // TreeStyle
        w.Write(new byte[4 * 3]); // CaveBackX
        w.Write(new byte[4 * 4]); // CaveBackStyle
        w.Write(new byte[4 * 3]); // Ice/Jungle/HellBackStyle

        w.Write(spawnX);
        w.Write(spawnY);
        w.Write(300.0); // groundLevel
        w.Write(600.0); // rockLevel
        w.Write(time);
        w.Write(dayTime);
        w.Write(moonPhase);
        w.Write(bloodMoon);
        w.Write(isEclipse);
        w.Write(20); w.Write(30); // dungeon

        w.Write(isCrimson);
        w.Write(downedBoss1);
        w.Write(downedBoss2);
        w.Write(downedBoss3);
        w.Write(downedQueenBee);
        w.Write(downedMech1);
        w.Write(downedMech2);
        w.Write(downedMech3);
        w.Write(false); // DownedMechBossAny
        w.Write(downedPlant);
        w.Write(downedGolem);
        if (version >= 118) w.Write(downedSlimeKing);
        w.Write(false); w.Write(false); w.Write(false); // SavedGoblin/Wizard/Mech
        w.Write(false); w.Write(false); w.Write(false); w.Write(false); // DownedGoblins/Clown/Frost/Pirates
        w.Write(false); w.Write(false); // ShadowOrbSmashed/SpawnMeteor
        w.Write((byte)0); // ShadowOrbCount
        w.Write(0); // AltarCount
        w.Write(hardMode);

        w.Flush();
        return ms.ToArray();
    }

    private static int CountDiffBytes(byte[] a, byte[] b) =>
        Enumerable.Range(0, a.Length).Count(i => a[i] != b[i]);

    [Fact]
    public void PatchSpawnPoint_CambiaSoloLosDosInt32DeSpawn()
    {
        byte[] original = BuildHeaderBytes(spawnX: 100, spawnY: 200);
        byte[] patched = WldWriter.PatchSpawnPoint(original, 555, 777);

        var header = WldReader.ReadHeader(patched);
        Assert.Equal(555, header.SpawnX);
        Assert.Equal(777, header.SpawnY);

        Assert.Equal(original.Length, patched.Length);
        int diffs = CountDiffBytes(original, patched);
        Assert.True(diffs <= 8, $"se esperaban como mucho 8 bytes distintos (2 Int32), se encontraron {diffs}");

        // Nada mas del header cambio - control cruzado real (no solo "no lanzo").
        var before = WldReader.ReadHeader(original);
        Assert.Equal(before.Title, header.Title);
        Assert.Equal(before.DungeonX, header.DungeonX);
        Assert.Equal(before.HardMode, header.HardMode);
    }

    [Fact]
    public void PatchTimeAndMoon_EscribeLosCincoCamposJuntos()
    {
        byte[] original = BuildHeaderBytes(time: 0, dayTime: true, moonPhase: 0, bloodMoon: false, isEclipse: false);
        byte[] patched = WldWriter.PatchTimeAndMoon(original, 13500.5, false, 4, true, true);

        var header = WldReader.ReadHeader(patched);
        Assert.Equal(13500.5, header.Time);
        Assert.False(header.DayTime);
        Assert.Equal(4, header.MoonPhase);
        Assert.True(header.BloodMoon);
        Assert.True(header.IsEclipse);

        Assert.Equal(original.Length, patched.Length);
        // Time(8) + DayTime(1) + MoonPhase(4) + BloodMoon(1) + IsEclipse(1) = 15 bytes maximo.
        Assert.True(CountDiffBytes(original, patched) <= 15);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    public void PatchTimeAndMoon_FaseLunarFueraDeRango_Lanza(int faseInvalida)
    {
        byte[] original = BuildHeaderBytes();
        Assert.Throws<ArgumentOutOfRangeException>(() => WldWriter.PatchTimeAndMoon(original, 0, true, faseInvalida, false, false));
    }

    [Fact]
    public void PatchTimeAndMoon_TiempoNegativo_Lanza()
    {
        byte[] original = BuildHeaderBytes();
        Assert.Throws<ArgumentOutOfRangeException>(() => WldWriter.PatchTimeAndMoon(original, -1, true, 0, false, false));
    }

    [Fact]
    public void PatchBossFlags_SoloCambiaLosCamposPedidos_ElRestoQuedaIgual()
    {
        byte[] original = BuildHeaderBytes(downedBoss1: false, downedBoss2: false, hardMode: false, downedGolem: true);

        var patch = new WldWriter.WorldFlagsPatch(DownedBoss1EyeOfCthulhu: true, HardMode: true);
        byte[] patched = WldWriter.PatchBossFlags(original, patch);

        var header = WldReader.ReadHeader(patched);
        Assert.True(header.DownedBoss1EyeOfCthulhu); // el que se pidio cambiar
        Assert.True(header.HardMode);                // el otro que se pidio cambiar
        Assert.False(header.DownedBoss2EaterOfWorldsOrBrainOfCthulhu); // no tocado, sigue false
        Assert.True(header.DownedGolemBoss); // no tocado, sigue true (el valor ORIGINAL, no un false por omision)

        Assert.Equal(original.Length, patched.Length);
        Assert.True(CountDiffBytes(original, patched) <= 2, "solo 2 bytes deberian cambiar - los dos campos pedidos, ninguno mas");
    }

    [Fact]
    public void PatchBossFlags_TodosLosJefesPrincipalesRedondoDeVerdad()
    {
        byte[] original = BuildHeaderBytes(version: 279);
        var patch = new WldWriter.WorldFlagsPatch(
            DownedBoss1EyeOfCthulhu: true, DownedBoss2EaterOfWorldsOrBrainOfCthulhu: true, DownedBoss3Skeletron: true,
            DownedQueenBee: true, DownedMechBoss1TheDestroyer: true, DownedMechBoss2TheTwins: true,
            DownedMechBoss3SkeletronPrime: true, DownedPlantBoss: true, DownedGolemBoss: true,
            DownedSlimeKingBoss: true, HardMode: true);

        byte[] patched = WldWriter.PatchBossFlags(original, patch);
        var header = WldReader.ReadHeader(patched);

        Assert.True(header.DownedBoss1EyeOfCthulhu);
        Assert.True(header.DownedBoss2EaterOfWorldsOrBrainOfCthulhu);
        Assert.True(header.DownedBoss3Skeletron);
        Assert.True(header.DownedQueenBee);
        Assert.True(header.DownedMechBoss1TheDestroyer);
        Assert.True(header.DownedMechBoss2TheTwins);
        Assert.True(header.DownedMechBoss3SkeletronPrime);
        Assert.True(header.DownedPlantBoss);
        Assert.True(header.DownedGolemBoss);
        Assert.True(header.DownedSlimeKingBoss);
        Assert.True(header.HardMode);
    }

    [Fact]
    public void PatchBossFlags_SlimeKingEnMundoAnteriorA118_Lanza()
    {
        byte[] original = BuildHeaderBytes(version: 100); // < 118, el campo ni existe en el archivo
        var patch = new WldWriter.WorldFlagsPatch(DownedSlimeKingBoss: true);
        Assert.Throws<NotSupportedException>(() => WldWriter.PatchBossFlags(original, patch));
    }

    [Fact]
    public void PatchBossFlags_SinSlimeKingEnMundoAnteriorA118_NoLanzaYCambiaElResto()
    {
        byte[] original = BuildHeaderBytes(version: 100);
        var patch = new WldWriter.WorldFlagsPatch(HardMode: true);
        byte[] patched = WldWriter.PatchBossFlags(original, patch);
        Assert.True(WldReader.ReadHeader(patched).HardMode);
        Assert.Null(WldReader.ReadHeader(patched).DownedSlimeKingBoss);
    }
}
