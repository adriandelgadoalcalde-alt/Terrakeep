using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), Bd-d: "buscador/filtro por clase" (la pestaña Builds era
// una unica lista plana de TODAS las clases de TODAS las etapas) + "marcar lo que ya se posee"
// (ningun indicio de que un objeto del build ya estuviera en el personaje cargado).
public sealed class BuildsFilterOwnershipTests
{
    private static MainViewModel NewLoadedViewModel()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"builds-filter-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void FiltrarPorClaseInexistenteEnVanillaOcultaTodasSusEtapas()
    {
        var vm = NewLoadedViewModel();
        // "rogue" existe en Calamity pero NO en vanilla (builds.json real: mage/melee/ranged/
        // summoner) - filtrar a "rogue" debe dejar CUALQUIER etapa vanilla sin ninguna clase
        // visible, y por tanto la etapa entera oculta.
        var rogueOption = vm.Builds.ClassFilterOptions.Single(o => o.Key == "rogue");

        vm.Builds.SelectClassFilterCommand.Execute(rogueOption);

        Assert.All(vm.Builds.VanillaStages, s => Assert.False(s.IsVisible));
        Assert.Contains(vm.Builds.CalamityStages, s => s.IsVisible);
        Assert.All(vm.Builds.VanillaStages.SelectMany(s => s.Classes), c => Assert.False(c.IsVisible));
        Assert.Contains(vm.Builds.CalamityStages.SelectMany(s => s.Classes), c => c.ClassName == "rogue" && c.IsVisible);
    }

    [Fact]
    public void VolverAtodasRestauraTodoVisible()
    {
        var vm = NewLoadedViewModel();
        vm.Builds.SelectClassFilterCommand.Execute(vm.Builds.ClassFilterOptions.Single(o => o.Key == "rogue"));

        vm.Builds.SelectClassFilterCommand.Execute(vm.Builds.ClassFilterOptions.Single(o => o.Key == null));

        Assert.All(vm.Builds.VanillaStages, s => Assert.True(s.IsVisible));
        Assert.All(vm.Builds.CalamityStages, s => Assert.True(s.IsVisible));
        Assert.All(vm.Builds.VanillaStages.Concat(vm.Builds.CalamityStages).SelectMany(s => s.Classes), c => Assert.True(c.IsVisible));
    }

    [Fact]
    public void MarcaComoPoseidoUnObjetoRealDelInventarioYNoLosDemas()
    {
        var vm = NewLoadedViewModel();
        var row = vm.Builds.VanillaStages.SelectMany(s => s.Classes).SelectMany(c => c.AllRows).First(r => r.ItemId != 0);
        var otraFila = vm.Builds.VanillaStages.SelectMany(s => s.Classes).SelectMany(c => c.AllRows).First(r => r.ItemId != 0 && r.ItemId != row.ItemId);

        vm.InventoryContainer!.Slots[0].PlaceItem(row.ItemId);
        vm.SelectedTabIndex = 2; // AppTab.Builds - dispara OnSelectedTabIndexChanged -> RefreshOwnership

        Assert.True(row.IsOwned);
        Assert.False(otraFila.IsOwned);
    }
}
