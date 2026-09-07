using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), S-c: "sin enlace al mapa de Exploracion desde Spawn
// Points" - "Ver en el mapa" real por fila, salta a Exploracion y centra el mapa en esas
// coordenadas exactas (mismo mecanismo real ya usado por los NPCs,
// ExplorationViewModel.NavigateToTile).
public sealed class ViewSpawnOnMapTests
{
    private static MainViewModel NewLoadedViewModelWithSpawn()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
            Servers = [new PlrServerEntry { Name = "Base", SpawnX = 4200, SpawnY = 300, WorldId = 0 }],
        };
        string path = Path.Combine(Path.GetTempPath(), $"view-spawn-map-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void SinMundoCargado_SaltaAExploracionYAvisaSinExcepcion()
    {
        var vm = NewLoadedViewModelWithSpawn();
        var row = vm.Servers.Entries[0];

        vm.ViewSpawnOnMapCommand.Execute(row);

        Assert.Equal(4, vm.SelectedTabIndex); // AppTab.Exploracion
        Assert.Contains("Carga un mundo", vm.Exploration.StatusMessage);
    }

    [Fact]
    public void ConMundoCargado_PideNavegarALasCoordenadasReales()
    {
        var vm = NewLoadedViewModelWithSpawn();
        var row = vm.Servers.Entries[0];
        (int X, int Y)? recibido = null;
        vm.Exploration.NavigateToTileRequested += (x, y) => recibido = (x, y);
        // Simula "mundo cargado" sin leer un .wld real - solo hace falta el flag que
        // ViewSpawnOnMap comprueba de verdad.
        vm.Exploration.IsWorldLoaded = true;

        vm.ViewSpawnOnMapCommand.Execute(row);

        Assert.Equal((4200, 300), recibido);
    }
}
