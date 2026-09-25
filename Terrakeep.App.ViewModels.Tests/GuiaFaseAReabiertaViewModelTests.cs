using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Guia;
using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;
using Terrakeep.Core.WldFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Guia Fase A REABIERTA (26-sep-2026, arquitecto-keep a8c40689, I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md):
// preparacion Core+ViewModel+localizacion para la siguiente fase XAML (que consumira
// EsLimiteEstructural/MostrarAvisoSinMundo desde el arbol) - SIN tocar MainWindow.xaml/.xaml.cs en
// esta ronda (ocupado por otro encargo en paralelo). Estas pruebas cubren el hueco real de
// cobertura de la capa ViewModel que la Fase A abre: hasta ahora GuideViewModel/
// GuideRequisitoViewModel no tenian NINGUNA prueba propia en este arnes (solo se ejercitaban
// indirectamente via GUIA_SOLO/PruebasGuiaYServidor).
public sealed class GuiaFaseAReabiertaViewModelTests
{
    // ---------------------------------------------------------------------------------------
    // GuideRequisitoViewModel.EsLimiteEstructural - mapeo 1:1 contra ResultadoRequisitoGuia.
    // GuideRequisitoViewModel.ParaPruebas es SOLO para pruebas (sin InternalsVisibleTo hacia este
    // arnes, ver su propio comentario en GuideViewModel.cs) - deja probar el mapeo directamente,
    // sin depender de que el catalogo real de la Guia (guia_progresion.json) tenga HOY algun
    // requisito NpcActivo (no lo tiene: sigue siendo el UNICO limite estructural real, pero
    // ninguna pantalla del arbol de progresion vanilla/Calamity actual lo usa todavia).
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void GuideRequisitoViewModel_EsLimiteEstructural_ReflejaElResultadoReal1a1()
    {
        var textos = GuideTextCatalog.LoadFromFiles("", ""); // rutas vacias -> catalogo vacio (Text() cae a "[clave]"), no hace falta para esta prueba

        var requisitoLimite = new RequisitoGuia { Tipo = TipoRequisitoGuia.NpcActivo, Id = 4 };
        var resultadoLimite = new ResultadoRequisitoGuia
        {
            Requisito = requisitoLimite,
            NoEvaluable = true,
            EsLimiteEstructural = true,
            MotivoClave = "guide_motive_active_npc",
        };

        var requisitoNormal = new RequisitoGuia { Tipo = TipoRequisitoGuia.Objeto, Id = 5 };
        var resultadoNormal = new ResultadoRequisitoGuia { Requisito = requisitoNormal, Cumplido = true };

        var vmLimite = GuideRequisitoViewModel.ParaPruebas(resultadoLimite, textos);
        var vmNormal = GuideRequisitoViewModel.ParaPruebas(resultadoNormal, textos);

        Assert.True(vmLimite.EsLimiteEstructural);
        Assert.False(vmNormal.EsLimiteEstructural);
    }

    // ---------------------------------------------------------------------------------------
    // GuideViewModel.MostrarAvisoSinMundo/MostrarAvisoSinPersonaje - via el constructor PUBLICO
    // real (mismo camino que MainViewModel.cs:1132), con un personaje sintetico minimo (mismo
    // criterio que MakeCharacter en Terrakeep.Core.Tests) y un mundo sintetico minimo (mismo
    // criterio que WorldCompareCardExportTests.MundoSintetico de este mismo arnes) - nunca un
    // .plr/.wld real en disco, solo los campos que la Guia de verdad necesita.
    // ---------------------------------------------------------------------------------------

    private static LoadedCharacter MakeLoadedCharacter()
    {
        var personaje = new PlrCharacter
        {
            Version = 279,
            Name = "Personaje de prueba",
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            HealthMax = 100,
        };
        return new LoadedCharacter("prueba.plr", null, "", personaje, null, new Dictionary<string, GameItem[]>());
    }

    private static WldWorld MakeWorld() => new()
    {
        Header = new WldHeader
        {
            Version = 279,
            Pointers = new int[10],
            TileFrameImportant = [],
            Title = "Mundo de prueba",
            WorldId = 1,
            TilesHigh = 1,
            TilesWide = 1,
            SpawnX = 0,
            SpawnY = 0,
            GroundLevel = 100,
            RockLevel = 200,
            Seed = "semilla-sintetica",
            GameMode = 0,
            DungeonX = 0,
            DungeonY = 0,
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
            DownedSlimeKingBoss = false,
            HardMode = false,
            DownedGoblinArmy = false,
            DownedFrostLegion = false,
            DownedPirates = false,
        },
        Tiles = new WldTile[1, 1],
        Npcs = [],
        Chests = [],
        Signs = [],
        TileEntities = [],
        ShimmeredNpcTypes = new HashSet<int>(),
    };

    [Fact]
    public void MostrarAvisoSinMundo_EsTrue_SoloConPersonajeCargadoYSinMundo()
    {
        var servicio = new CharacterFileService();
        var loaded = MakeLoadedCharacter();
        var gvm = new GuideViewModel(servicio, () => loaded, () => null, () => false, () => null);

        Assert.True(gvm.MostrarAvisoSinMundo);
        Assert.False(gvm.MostrarAvisoSinPersonaje);
    }

    [Fact]
    public void MostrarAvisoSinMundoYSinPersonaje_SonMutuamenteExcluyentes_NuncaLosDosTrueALaVez()
    {
        var servicio = new CharacterFileService();
        var loaded = MakeLoadedCharacter();
        var mundo = MakeWorld();

        // Los 4 estados reales posibles de "que hay cargado ahora mismo en Terrakeep".
        var conLosDos = new GuideViewModel(servicio, () => loaded, () => mundo, () => false, () => null);
        var soloMundo = new GuideViewModel(servicio, () => null, () => mundo, () => false, () => null);
        var soloPersonaje = new GuideViewModel(servicio, () => loaded, () => null, () => false, () => null);
        var ninguno = new GuideViewModel(servicio, () => null, () => null, () => false, () => null);

        Assert.False(conLosDos.MostrarAvisoSinMundo);
        Assert.False(conLosDos.MostrarAvisoSinPersonaje);

        Assert.False(soloMundo.MostrarAvisoSinMundo);
        Assert.True(soloMundo.MostrarAvisoSinPersonaje);

        Assert.True(soloPersonaje.MostrarAvisoSinMundo);
        Assert.False(soloPersonaje.MostrarAvisoSinPersonaje);

        Assert.False(ninguno.MostrarAvisoSinMundo);
        Assert.False(ninguno.MostrarAvisoSinPersonaje);
    }
}
