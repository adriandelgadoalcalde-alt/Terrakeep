using Terrakeep.Core.Data;
using Terrakeep.Core.Guia;
using Terrakeep.Core.WldFormat;
using Xunit;

namespace Terrakeep.Core.Tests.Guia;

// Guia Encargo A "GuiaCalamity" (25-sep-2026): hueco de cobertura real que cierra este archivo -
// antes de este cambio GuideFlags.Existe/Valor no reconocian NINGUNA bandera de Calamity
// (GuideContext no tenia ni el campo CalamityDownedFlags), asi que los 25 tramos de
// guia_progresion.json con "ambito":"calamity" NUNCA podian marcarse como completados aunque el
// jugador ya hubiera derrotado ese jefe en su .wld/.twld real - ver TwldReaderCalamityFlagsTests
// para el parseo del .twld en si, este archivo prueba el WIRING completo (GuideContext ->
// GuideFlags -> GuideEvaluator), mismo patron ya usado en GuideEvaluationEngineTests para las
// banderas de evento tardio vanilla (downedGoblins/downedFrost/downedPirates).
public class GuideFlagsCalamityTests
{
    private static RequisitoGuia Req(string bandera) => new() { Tipo = TipoRequisitoGuia.Bandera, Bandera = bandera };

    private static WldHeader MakeHeader() => new()
    {
        Version = 279, Pointers = new int[10], TileFrameImportant = [], Title = "Mundo de prueba",
        WorldId = 1, TilesHigh = 1, TilesWide = 1, SpawnX = 0, SpawnY = 0, GroundLevel = 100, RockLevel = 200,
        Seed = "semilla-sintetica", GameMode = 0, DungeonX = 0, DungeonY = 0, Time = 0, DayTime = true,
        MoonPhase = 0, BloodMoon = false, IsEclipse = false, IsCrimson = false,
        DownedBoss1EyeOfCthulhu = false, DownedBoss2EaterOfWorldsOrBrainOfCthulhu = false, DownedBoss3Skeletron = false,
        DownedQueenBee = false, DownedMechBoss1TheDestroyer = false, DownedMechBoss2TheTwins = false,
        DownedMechBoss3SkeletronPrime = false, DownedPlantBoss = false, DownedGolemBoss = false, HardMode = false,
        DownedGoblinArmy = false, DownedFrostLegion = false, DownedPirates = false,
    };

    private static WldWorld MakeWorld() => new()
    {
        Header = MakeHeader(), Tiles = new WldTile[1, 1], Npcs = [], Chests = [], Signs = [],
        TileEntities = [], ShimmeredNpcTypes = new HashSet<int>(),
    };

    [Theory]
    [InlineData("downedDesertScourge")]
    [InlineData("downedProvidence")]
    [InlineData("downedCalamitas")]
    [InlineData("downedAstrumDeus")]
    [InlineData("downedDoG")]
    [InlineData("downedPlaguebringer")]
    public void BanderasDeCalamity_SonConocidasPorGuideFlags(string bandera)
    {
        Assert.True(GuideFlags.Existe(bandera));
    }

    [Fact]
    public void BanderaDeCalamityDesconocida_NoEsConocidaPorGuideFlags()
    {
        // "downedHorribleHog" es una propiedad REAL de DownedBossSystem pero ningun tramo de la
        // Guia la usa - no forma parte de CalamityCanonicalFlagNames, tiene que seguir "no
        // reconocida" (mismo criterio honesto que el resto del vocabulario cerrado).
        Assert.False(GuideFlags.Existe("downedHorribleHog"));
    }

    [Fact]
    public void ConMundoCargadoYBanderaCalamityPuesta_SaleCumplidaSinPersonaje()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null);
        var contexto = new GuideContext
        {
            Character = null,
            MergedContainers = null,
            World = MakeWorld(),
            HasCalamity = true,
            CalamityDownedFlags = new HashSet<string> { "downedDesertScourge", "downedCrabulon" },
        };

        var desertScourge = evaluador.Evaluar(Req("downedDesertScourge"), contexto);
        var providence = evaluador.Evaluar(Req("downedProvidence"), contexto);

        Assert.False(desertScourge.NoEvaluable);
        Assert.True(desertScourge.Cumplido);
        // Otra bandera de Calamity NO puesta en el set: no cumplida, pero SI evaluable (mundo
        // cargado de verdad) - nunca "no evaluable" solo porque el set no la traiga.
        Assert.False(providence.NoEvaluable);
        Assert.False(providence.Cumplido);
    }

    [Fact]
    public void SinMundoCargado_BanderaDeCalamityQuedaNoEvaluableConMotivoDeMundo()
    {
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null);
        // Mismo contrato que GuideContext.CalamityDownedFlags documenta: null cuando no hay mundo
        // cargado (GuideViewModel.Refresh() nunca llama a ResolveCalamityDownedFlags en ese caso).
        var contexto = new GuideContext { Character = null, MergedContainers = null, World = null, CalamityDownedFlags = null };

        var resultado = evaluador.Evaluar(Req("downedProvidence"), contexto);

        Assert.True(resultado.NoEvaluable);
        // Guia Fase A REABIERTA (26-sep-2026, arquitecto-keep a8c40689): antes de esta ronda el
        // motivo era el generico "guide_motive_load_data" ("carga un personaje/mundo"), pese a
        // que el nombre de esta prueba YA decia "ConMotivoDeMundo" - una bandera (de Calamity o
        // vanilla) SIEMPRE se lee del .wld/.twld, nunca del .plr, asi que el motivo real y
        // especifico es "carga el mundo", nunca "falta el personaje".
        Assert.Equal("guide_motive_load_world", resultado.MotivoClave);
    }

    [Fact]
    public void MundoCargadoSinTwldValido_TodasLasBanderasDeCalamitySalenNoCumplidas_NuncaNoEvaluables()
    {
        // Mundo cargado pero sin `.twld` real (o sin seccion DownedBossSystem) - GuideViewModel.
        // ResolveCalamityDownedFlags devuelve un HashSet VACIO real, nunca null (ver su
        // comentario) - la bandera es evaluable y sale "no cumplida" de verdad, igual que
        // pasaria con NPC.downedXxx=false en una partida sin ese jefe derrotado.
        var evaluador = new GuideEvaluator(MakeItemNames(), MakeNpcNames(), null);
        var contexto = new GuideContext
        {
            Character = null, MergedContainers = null, World = MakeWorld(), HasCalamity = true,
            CalamityDownedFlags = new HashSet<string>(),
        };

        var resultado = evaluador.Evaluar(Req("downedYharon"), contexto);

        Assert.False(resultado.NoEvaluable);
        Assert.False(resultado.Cumplido);
    }

    // Las 31 banderas reales que guia_progresion.json referencia en sus tramos de Calamity tienen
    // que ser TODAS conocidas por GuideFlags.Existe - si una sola falta, ese tramo entero se queda
    // "no evaluable" sin motivo real (regresion silenciosa del wiring).
    [Fact]
    public void Las31BanderasDeCalamityQueLaGuiaReferencia_SonTodasConocidas()
    {
        foreach (string bandera in TwldReader.CalamityCanonicalFlagNames)
            Assert.True(GuideFlags.Existe(bandera), $"GuideFlags no reconoce '{bandera}'");

        Assert.Equal(31, TwldReader.CalamityCanonicalFlagNames.Count);
    }

    private static VanillaItemCatalog MakeItemNames() =>
        VanillaItemCatalog.LoadFromStreams(
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}")),
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}")),
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}")));

    private static NpcNameCatalog MakeNpcNames() =>
        NpcNameCatalog.LoadFromStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("[]")));
}
