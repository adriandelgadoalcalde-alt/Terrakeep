using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Catalogo de ideas Keep, idea 6 ("Laboratorio de personajes" - "investigacion coherente"),
// segunda pasada (20-sep-2026, reconsiderada a peticion explicita del coordinador). Verifica
// ResearchViewModel.MarkResearchedByPid directamente contra el catalogo REAL (mismo criterio ya
// establecido por ResearchEditableTests: sin Dispatcher real, sin depender del debounce de
// busqueda) - "MoltenHelmet" es un pid REAL de builds.json (Pre-Hardmode/melee/armor).
public sealed class GenerateCharacterResearchTests
{
    private static readonly CharacterFileService Service = new();

    private static PlrCharacter NuevoPersonaje() => new()
    {
        Name = "Test",
        Version = 279,
        Difficulty = 3, // Modo Viaje real
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    private static ResearchViewModel NuevoConPersonaje(out PlrCharacter character)
    {
        character = NuevoPersonaje();
        var vm = new ResearchViewModel(Service);
        vm.LoadFrom(character);
        return vm;
    }

    [Fact]
    public void MarkResearchedByPid_ConUnPidVanillaRealDeBuildsJson_LoMarcaComoInvestigadoAlMaximo()
    {
        var vm = NuevoConPersonaje(out var character);
        Assert.Equal(0, vm.ResearchedCount);

        vm.MarkResearchedByPid(["MoltenHelmet"]); // pid real, builds.json Pre-Hardmode/melee/armor

        Assert.Equal(1, vm.ResearchedCount);

        vm.SyncBackTo(character);
        var entrada = Assert.Single(character.Research);
        Assert.Equal("MoltenHelmet", entrada.Pid);
        // Investigado AL MAXIMO real (FullResearchCount), no un conteo parcial cualquiera - un
        // personaje "listo para X" debe poder fabricar el objeto libremente en Modo Viaje.
        int id = Service.VanillaCatalog.GetIdByKey("MoltenHelmet")!.Value;
        int completoReal = Service.VanillaResearchCounts.Get(id) ?? Terrakeep.App.Services.ResearchAllService.PlaceholderCount;
        Assert.Equal(completoReal, entrada.Count);
    }

    [Fact]
    public void MarkResearchedByPid_VariasVeces_NuncaBajaUnConteoYaCompleto()
    {
        var vm = NuevoConPersonaje(out _);
        vm.MarkResearchedByPid(["MoltenHelmet"]);
        int primerConteo = vm.ResearchedCount;

        vm.MarkResearchedByPid(["MoltenHelmet"]); // el mismo objeto otra vez

        Assert.Equal(primerConteo, vm.ResearchedCount); // sigue siendo 1, no duplica ni revierte
    }

    [Fact]
    public void MarkResearchedByPid_ConUnPidQueNoExisteEnNingunCatalogo_SeIgnoraEnSilencio()
    {
        var vm = NuevoConPersonaje(out var character);

        vm.MarkResearchedByPid(["ObjetoQueNoExisteDeVerdad12345"]);

        Assert.Equal(0, vm.ResearchedCount);
        vm.SyncBackTo(character);
        Assert.Empty(character.Research);
    }

    [Fact]
    public void MarkResearchedByPid_ConVariosPidsDeUnBuildReal_LosMarcaTodos()
    {
        var vm = NuevoConPersonaje(out var character);

        // Mismo equipo real (armadura+arma+accesorio) que builds.json trae de verdad para
        // Pre-Hardmode/melee - nunca ids inventados a mano.
        vm.MarkResearchedByPid(["MoltenHelmet", "MoltenBreastplate", "MoltenGreaves", "NightsEdge", "FeralClaws"]);

        Assert.Equal(5, vm.ResearchedCount);
        vm.SyncBackTo(character);
        Assert.Equal(5, character.Research.Count);
    }
}
