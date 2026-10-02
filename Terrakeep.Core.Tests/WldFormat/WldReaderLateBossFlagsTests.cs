using Terrakeep.Core.Guia;
using Terrakeep.Core.WldFormat;
using Xunit;

namespace Terrakeep.Core.Tests.WldFormat;

// Guia Encargo5b (26-sep-2026, diseño de arquitecto-keep a84878b7, confirmado linea a linea
// contra World.FileV2.cs de TEdit real - xnb-lzx-tool-refs/World.FileV2.cs lineas 2120-2366):
// jefes tardios vanilla (Fishron/Marcianos/Culto Lunatico/Lunatico/Torres Celestiales/
// Emperatriz de la Luz/Reina Slime/Deerclops), detras de DOS secciones de ancho variable
// (Anglers, LoadBanners) que WldReader ahora atraviesa de verdad. Mismo criterio de prueba que
// WldWriterProgressPatchTests.BuildHeaderBytes (cabecera sintetica byte a byte, nunca un .wld
// real necesario para probar el offset) - aqui ampliada mas alla de HardMode para cubrir el
// tramo variable nuevo.
public class WldReaderLateBossFlagsTests
{
    // Cabecera sintetica COMPLETA (hasta el ultimo campo tardio que a esta version le
    // corresponda) - mismo orden EXACTO que WldReader.ReadHeader + ReadLateBossFlags. Las listas
    // variables (Anglers/KilledMobs/ClaimableBanners/PartyingNPCs/TreeTopVariations) se escriben
    // siempre con longitud 0: lo unico que importa aqui es el desplazamiento de los campos fijos
    // que las rodean, no su contenido.
    private static byte[] BuildHeaderBytes(
        uint version,
        bool downedFishron = false, bool downedMartians = false, bool downedLunaticCultist = false, bool downedMoonlord = false,
        bool downedCelestialSolar = false, bool downedCelestialVortex = false, bool downedCelestialNebula = false, bool downedCelestialStardust = false,
        bool downedEmpressOfLight = false, bool downedQueenSlime = false, bool downedDeerclops = false,
        IReadOnlyDictionary<string, bool>? extra = null)
    {
        bool X(string k) => extra != null && extra.TryGetValue(k, out var v) && v;
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

        w.Write(100); w.Write(200); // spawn
        w.Write(300.0); // groundLevel
        w.Write(600.0); // rockLevel
        w.Write(0.0); // time
        w.Write(true); // dayTime
        w.Write(0); // moonPhase
        w.Write(false); // bloodMoon
        w.Write(false); // isEclipse
        w.Write(20); w.Write(30); // dungeon

        w.Write(false); // isCrimson
        w.Write(false); w.Write(false); w.Write(false); w.Write(false); // downedBoss1/2/3/QueenBee
        w.Write(false); w.Write(false); w.Write(false); // downedMech1/2/3
        w.Write(false); // DownedMechBossAny
        w.Write(false); w.Write(false); // downedPlant/Golem
        if (version >= 118) w.Write(false); // downedSlimeKing
        w.Write(false); w.Write(false); w.Write(false); // SavedGoblin/Wizard/Mech
        w.Write(false); w.Write(false); w.Write(false); w.Write(false); // DownedGoblins/Clown/Frost/Pirates
        w.Write(X("shadowOrbSmashed")); w.Write(false); // ShadowOrbSmashed/SpawnMeteor
        w.Write((byte)0); // ShadowOrbCount
        w.Write(0); // AltarCount
        w.Write(false); // hardMode

        // --- a partir de aqui, el tramo nuevo (Guia Encargo5b) ---
        if (version >= 257) w.Write(false); // PartyOfDoom
        w.Write(0); w.Write(0); w.Write(0); w.Write(0.0); // InvasionDelay/Size/Type/X
        if (version >= 118) w.Write(0.0); // SlimeRainTime
        if (version >= 113) w.Write((byte)0); // SundialCooldown
        w.Write(false); // IsRaining
        w.Write(0); // TempRainTime
        w.Write(0f); // TempMaxRain
        w.Write(0); w.Write(0); w.Write(0); // SavedOreTiers Cobalt/Mythril/Adamantite
        w.Write(new byte[8]); // BgTree..BgOcean
        w.Write(0); // CloudBgActive
        w.Write((short)0); // NumClouds
        w.Write(0f); // WindSpeedSet

        if (version < 95) return Finish(w, ms);
        w.Write(0); // anglerCount=0

        if (version < 99) return Finish(w, ms);
        w.Write(false); // SavedAngler

        if (version < 101) return Finish(w, ms);
        w.Write(0); // AnglerQuest

        if (version < 104) return Finish(w, ms);
        w.Write(false); // SavedStylist
        if (version >= 140) w.Write(false); // SavedTaxCollector
        if (version >= 201) w.Write(false); // SavedGolfer
        if (version >= 107) w.Write(0); // InvasionSizeStart
        if (version >= 108) w.Write(0); // CultistDelay

        if (version < 109) return Finish(w, ms);
        w.Write((short)0); // KilledMobs count = 0
        if (version >= 289) w.Write((short)0); // ClaimableBanners count = 0

        if (version < 128) return Finish(w, ms);
        if (version >= 140) w.Write(false); // FastForwardTime

        if (version < 131) return Finish(w, ms);
        w.Write(downedFishron);

        if (version >= 140)
        {
            w.Write(downedMartians);
            w.Write(downedLunaticCultist);
            w.Write(downedMoonlord);
        }

        w.Write(X("downedHalloweenKing")); w.Write(X("downedHalloweenTree")); // Halloween/Navidad x5
        w.Write(X("downedChristmasIceQueen")); w.Write(X("downedChristmasSantank")); w.Write(X("downedChristmasTree"));

        if (version < 140) return Finish(w, ms);
        w.Write(downedCelestialSolar);
        w.Write(downedCelestialVortex);
        w.Write(downedCelestialNebula);
        w.Write(downedCelestialStardust);
        w.Write(false); w.Write(false); w.Write(false); w.Write(false); // Celestial*Active x4
        w.Write(false); // Apocalypse

        if (version >= 170)
        {
            w.Write(false); w.Write(false); w.Write(0); // PartyManual/Genuine/Cooldown
            w.Write(0); // numparty = 0
        }
        if (version >= 174)
        {
            w.Write(false); w.Write(0); w.Write(0f); w.Write(0f); // SandStorm*
        }
        if (version >= 178) { w.Write(false); w.Write(X("downedDD2InvasionT1")); w.Write(X("downedDD2InvasionT2")); w.Write(X("downedDD2InvasionT3")); } // SavedBartender + DD2 T1/T2/T3
        if (version > 194) w.Write((byte)0); // MushroomBg
        if (version >= 215) w.Write((byte)0); // UnderworldBg
        if (version >= 195) { w.Write((byte)0); w.Write((byte)0); w.Write((byte)0); } // BgTree2/3/4
        if (version >= 204) w.Write(X("combatBookWasUsed")); // CombatBookUsed
        if (version >= 207) { w.Write(0); w.Write(false); w.Write(false); w.Write(false); } // LanternNight*
        if (version >= 211) w.Write(0); // TreeTopVariations count = 0
        if (version >= 212) { w.Write(false); w.Write(false); } // ForceHalloween/XMasForToday
        if (version >= 216) { w.Write(0); w.Write(0); w.Write(0); w.Write(0); } // SavedOreTiers Copper/Iron/Silver/Gold
        if (version >= 217) { w.Write(false); w.Write(false); w.Write(false); } // BoughtCat/Dog/Bunny

        if (version >= 223)
        {
            w.Write(downedEmpressOfLight);
            w.Write(downedQueenSlime);
        }

        if (version >= 240) w.Write(downedDeerclops);
        if (version >= 250) w.Write(false); // unlockedSlimeBlueSpawn
        if (version >= 251) w.Write(new byte[8]); // unlocked*Spawn x8
        if (version >= 259) w.Write(X("combatBookVolumeTwoWasUsed"));
        if (version >= 260) w.Write(X("peddlersSatchelWasUsed"));

        return Finish(w, ms);
    }

    private static byte[] Finish(BinaryWriter w, MemoryStream ms)
    {
        w.Flush();
        return ms.ToArray();
    }

    // Rango 1: version < 95 (antes incluso de la lista de Anglers) - los 11 flags tardios,
    // TODOS null. Ninguno de los parametros downedXxx importa: el builder nunca llega a
    // escribirlos, asi que tampoco hay nada que leer.
    [Fact]
    public void ReadHeader_VersionAnteriorA95_TodosLosJefesTardiosSonNull()
    {
        var h = WldReader.ReadHeader(BuildHeaderBytes(version: 90));

        Assert.Null(h.DownedFishron);
        Assert.Null(h.DownedMartians);
        Assert.Null(h.DownedLunaticCultist);
        Assert.Null(h.DownedMoonlord);
        Assert.Null(h.DownedCelestialSolar);
        Assert.Null(h.DownedCelestialVortex);
        Assert.Null(h.DownedCelestialNebula);
        Assert.Null(h.DownedCelestialStardust);
        Assert.Null(h.DownedEmpressOfLight);
        Assert.Null(h.DownedQueenSlime);
        Assert.Null(h.DownedDeerclops);
    }

    // Rango 2: 131 <= version < 140 - SOLO DownedFishron tiene valor real (es el primero de la
    // familia, el resto exige v>=140 o mas). Confirma ademas que un valor TRUE se lee de verdad
    // (no solo que "no lanza").
    [Fact]
    public void ReadHeader_Version135_SoloFishronTieneValorElRestoNull()
    {
        var h = WldReader.ReadHeader(BuildHeaderBytes(version: 135, downedFishron: true));

        Assert.True(h.DownedFishron);
        Assert.Null(h.DownedMartians);
        Assert.Null(h.DownedLunaticCultist);
        Assert.Null(h.DownedMoonlord);
        Assert.Null(h.DownedCelestialSolar);
        Assert.Null(h.DownedCelestialVortex);
        Assert.Null(h.DownedCelestialNebula);
        Assert.Null(h.DownedCelestialStardust);
        Assert.Null(h.DownedEmpressOfLight);
        Assert.Null(h.DownedQueenSlime);
        Assert.Null(h.DownedDeerclops);
    }

    // Rango 3: 140 <= version < 223 - Fishron/Marcianos/Culto/Lunatico/Torres YA tienen valor
    // real; Emperatriz/ReinaSlime (v>=223) y Deerclops (v>=240) siguen null.
    [Fact]
    public void ReadHeader_Version200_JefesDe140TienenValorEmpressQueenSlimeYDeerclopsSonNull()
    {
        var h = WldReader.ReadHeader(BuildHeaderBytes(
            version: 200, downedFishron: true, downedMartians: true, downedLunaticCultist: true, downedMoonlord: true,
            downedCelestialSolar: true, downedCelestialVortex: true, downedCelestialNebula: false, downedCelestialStardust: true));

        Assert.True(h.DownedFishron);
        Assert.True(h.DownedMartians);
        Assert.True(h.DownedLunaticCultist);
        Assert.True(h.DownedMoonlord);
        Assert.True(h.DownedCelestialSolar);
        Assert.True(h.DownedCelestialVortex);
        Assert.False(h.DownedCelestialNebula); // valor real leido, no un true por defecto
        Assert.True(h.DownedCelestialStardust);
        Assert.Null(h.DownedEmpressOfLight);
        Assert.Null(h.DownedQueenSlime);
        Assert.Null(h.DownedDeerclops);
    }

    // Rango 4: version >= 240 - los 11 flags tienen valor real, incluido Deerclops (el ultimo,
    // v>=240).
    [Fact]
    public void ReadHeader_Version279_LosOnceJefesTardiosTienenValorReal()
    {
        var h = WldReader.ReadHeader(BuildHeaderBytes(
            version: 279,
            downedFishron: true, downedMartians: true, downedLunaticCultist: true, downedMoonlord: true,
            downedCelestialSolar: true, downedCelestialVortex: true, downedCelestialNebula: true, downedCelestialStardust: true,
            downedEmpressOfLight: true, downedQueenSlime: true, downedDeerclops: true));

        Assert.True(h.DownedFishron);
        Assert.True(h.DownedMartians);
        Assert.True(h.DownedLunaticCultist);
        Assert.True(h.DownedMoonlord);
        Assert.True(h.DownedCelestialSolar);
        Assert.True(h.DownedCelestialVortex);
        Assert.True(h.DownedCelestialNebula);
        Assert.True(h.DownedCelestialStardust);
        Assert.True(h.DownedEmpressOfLight);
        Assert.True(h.DownedQueenSlime);
        Assert.True(h.DownedDeerclops);
    }

    // Control cruzado real: tras leer el bloque tardio, el resto de la cabecera (SpawnX/Title/
    // HardMode) sigue sano - si el offset de cualquier campo tardio estuviera mal, el stream se
    // desincronizaria y esto lo pillaria igual que ya hacen las pruebas "TrasElBloqueDeBanderas"
    // de WldReaderProgressFieldsTests.
    [Fact]
    public void ReadHeader_Version279_ElRestoDeLaCabeceraSigueSano()
    {
        var h = WldReader.ReadHeader(BuildHeaderBytes(version: 279, downedFishron: true, downedDeerclops: true));
        Assert.Equal("Mi Mundo de Verdad", h.Title);
        Assert.Equal(100, h.SpawnX);
        Assert.Equal(200, h.SpawnY);
        Assert.False(h.HardMode);
    }

    // Regresion real del bug que CopyWith tenia antes de este encargo: los With* (WithSpawn/
    // WithTimeAndMoon/WithBossFlags/WithGameMode) construian el WldHeader nuevo con un
    // inicializador que NO mencionaba los 11 campos tardios - al ser propiedades no "required",
    // se habrian puesto a null por omision en cualquier copia, borrando silenciosamente un dato
    // ya leido del archivo.
    [Fact]
    public void WithSpawn_NoBorraLosJefesTardiosYaLeidos()
    {
        var original = WldReader.ReadHeader(BuildHeaderBytes(
            version: 279, downedFishron: true, downedMoonlord: true, downedDeerclops: true));

        var conNuevoSpawn = original.WithSpawn(999, 888);

        Assert.Equal(999, conNuevoSpawn.SpawnX);
        Assert.True(conNuevoSpawn.DownedFishron);
        Assert.True(conNuevoSpawn.DownedMoonlord);
        Assert.True(conNuevoSpawn.DownedDeerclops);
    }

    private static WldWorld ToMinimalWorld(WldHeader header) => new()
    {
        Header = header,
        Tiles = new WldTile[1, 1],
        Npcs = [],
        Chests = [],
        Signs = [],
        TileEntities = [],
        ShimmeredNpcTypes = new HashSet<int>(),
    };

    // Guia Encargo5b, verificacion final pedida explicitamente: con estos jefes YA derrotados en
    // un mundo cuya version SI los guarda, GuideFlags.Valor deja de devolver null ("no evaluable"
    // en GuideEvaluationEngine.EvaluarBandera - ver su comentario real) y devuelve el true real.
    [Fact]
    public void GuideFlags_ConJefesTardiosDerrotados_DejanDeSerNoEvaluables()
    {
        var header = WldReader.ReadHeader(BuildHeaderBytes(
            version: 279,
            downedFishron: true, downedMartians: true, downedLunaticCultist: true, downedMoonlord: true,
            downedCelestialSolar: true, downedCelestialVortex: true, downedCelestialNebula: true, downedCelestialStardust: true,
            downedEmpressOfLight: true, downedQueenSlime: true, downedDeerclops: true));
        var contexto = new GuideContext { World = ToMinimalWorld(header) };

        foreach (var clave in new[]
        {
            "downedFishron", "downedAncientCultist", "downedMoonlord", "downedMartians",
            "downedTowers", "downedQueenSlime", "downedDeerclops", "downedEmpressOfLight",
        })
        {
            Assert.True(GuideFlags.Existe(clave), $"'{clave}' deberia ser una bandera reconocida");
            Assert.True(GuideFlags.Valor(clave, contexto), $"'{clave}' deberia dar true (jefe ya derrotado), no null/false");
        }
    }

    // Mismo mundo pero de una version demasiado vieja para guardar estos campos (v90): la
    // bandera sigue siendo RECONOCIDA (Existe=true, el vocabulario no cambia con la version del
    // mundo) pero su Valor es null - "no evaluable por falta de datos", nunca un false inventado.
    [Fact]
    public void GuideFlags_ConMundoDemasiadoViejo_SigueReconocidaPeroValorEsNull()
    {
        var header = WldReader.ReadHeader(BuildHeaderBytes(version: 90));
        var contexto = new GuideContext { World = ToMinimalWorld(header) };

        Assert.True(GuideFlags.Existe("downedFishron"));
        Assert.Null(GuideFlags.Valor("downedFishron", contexto));
        Assert.True(GuideFlags.Existe("downedTowers"));
        Assert.Null(GuideFlags.Valor("downedTowers", contexto));
    }

    // downedTowers exige las 4 torres juntas (mismo patron que downedMechBossAll) - con solo 3
    // de 4 derrotadas debe dar false real, no true ni null.
    [Fact]
    public void GuideFlags_DownedTowers_ExigeLasCuatroTorresJuntas()
    {
        var header = WldReader.ReadHeader(BuildHeaderBytes(
            version: 279,
            downedCelestialSolar: true, downedCelestialVortex: true, downedCelestialNebula: true, downedCelestialStardust: false));
        var contexto = new GuideContext { World = ToMinimalWorld(header) };

        Assert.False(GuideFlags.Valor("downedTowers", contexto));
    }

    // Guia v2 (F1, 02-oct-2026): banderas que el lector ya atravesaba y ahora captura - orden
    // confirmado contra WorldFile.LoadHeaderFlags del tModLoader 1.4.4.9 decompilado.
    private static readonly string[] BanderasF1 =
    [
        "shadowOrbSmashed", "downedHalloweenKing", "downedHalloweenTree", "downedChristmasIceQueen", "downedChristmasSantank",
        "downedChristmasTree", "downedDD2InvasionT1", "downedDD2InvasionT2", "downedDD2InvasionT3", "combatBookWasUsed",
        "combatBookVolumeTwoWasUsed", "peddlersSatchelWasUsed",
    ];

    [Fact]
    public void GuiaV2F1_CadaBanderaNuevaSeLeeEnSuSitioSinArrastrarALasVecinas()
    {
        // Una a una a true: si un offset estuviera mal, saldria true otra bandera (o ninguna).
        foreach (var unica in BanderasF1)
        {
            var header = WldReader.ReadHeader(BuildHeaderBytes(version: 279, downedDeerclops: true,
                extra: new Dictionary<string, bool> { [unica] = true }));
            var contexto = new GuideContext { World = ToMinimalWorld(header) };
            foreach (var b in BanderasF1)
            {
                Assert.True(GuideFlags.Existe(b), b);
                Assert.Equal(b == unica, GuideFlags.Valor(b, contexto));
            }
            Assert.True(header.DownedDeerclops); // lo de antes sigue en su sitio
            Assert.Equal("Mi Mundo de Verdad", header.Title);
        }
    }

    [Fact]
    public void GuiaV2F1_VersionesViejasDanNullNoFalse()
    {
        // v200: tiene Halloween/Navidad (>=131) y DD2 (>=178), pero no el primer libro (>=204),
        // ni el segundo (>=259) ni la bolsa del buhonero (>=260).
        var h = WldReader.ReadHeader(BuildHeaderBytes(version: 200, extra: new Dictionary<string, bool>
        {
            ["downedHalloweenKing"] = true, ["downedDD2InvasionT3"] = true,
        }));
        Assert.True(h.DownedHalloweenKing);
        Assert.False(h.DownedChristmasTree);
        Assert.True(h.DownedDD2InvasionT3);
        Assert.Null(h.CombatBookWasUsed);
        Assert.Null(h.CombatBookVolumeTwoWasUsed);
        Assert.Null(h.PeddlersSatchelWasUsed);
        Assert.False(h.ShadowOrbSmashed);
    }

    [Fact]
    public void GuiaV2F1_WithSpawn_NoBorraLasBanderasNuevas()
    {
        var h = WldReader.ReadHeader(BuildHeaderBytes(version: 279, extra: new Dictionary<string, bool>
        {
            ["downedChristmasSantank"] = true, ["peddlersSatchelWasUsed"] = true, ["shadowOrbSmashed"] = true,
        })).WithSpawn(5, 6);
        Assert.True(h.DownedChristmasSantank);
        Assert.True(h.PeddlersSatchelWasUsed);
        Assert.True(h.ShadowOrbSmashed);
    }
}
