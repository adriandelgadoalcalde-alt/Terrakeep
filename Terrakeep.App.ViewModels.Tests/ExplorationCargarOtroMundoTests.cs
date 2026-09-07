using System.IO;
using System.Threading.Tasks;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

// AR-EX1b (oleada de pruebas del area "Exploracion del mundo", 6-sep-2026): bug real encontrado
// leyendo el camino de carga y confirmado despues con el arnes sobre dos mundos REALES.
//
// LoadFromPathAsync limpia con cuidado casi todo el estado del mundo saliente (resultados de
// busqueda, resumen, indice actual, filtros de NPC, resaltado del mapa...) y termina llamando a
// RebuildInventory(). Pero RebuildInventory DESPACHA por SelectedCategory - y dos lineas antes
// se acaba de dejar en "Todo", que es justo la unica categoria que no reconstruye ningun
// inventario. Resultado real: "Cofre a cofre" (ChestRows, la unica vista con coleccion propia)
// conservaba ENTERAS las filas del mundo ANTERIOR, y si habia un cofre seleccionado su marcador
// (HasCurrentChest/CurrentChestX/Y) seguia pintandose sobre el mapa NUEVO - en una casilla que en
// el mundo entrante puede no existir siquiera si es mas pequeño.
//
// Aqui se fija la regla sin depender de ningun .wld: la ruta inexistente entra por el catch real
// de LoadFromPathAsync, que es el otro camino donde tampoco debia quedar nada del mundo anterior.
// El camino de exito (dos mundos reales de esta maquina) lo mide el arnes, que si tiene ficheros.
public sealed class ExplorationCargarOtroMundoTests
{
    private static ChestRowViewModel Cofre(int x, int y) =>
        new("Cofre de oro", null, x, y, null, [], false);

    [Fact]
    public async Task CargarOtroMundo_NoDejaNiUnaFilaDeCofresDelAnterior()
    {
        var vm = new MainViewModel().Exploration;
        vm.ChestRows.Add(Cofre(100, 200));
        vm.ChestRows.Add(Cofre(300, 400));

        await vm.LoadFromPathAsync(Path.Combine(Path.GetTempPath(), "terrakeep-mundo-que-no-existe.wld"));

        Assert.Empty(vm.ChestRows);
    }

    [Fact]
    public async Task CargarOtroMundo_ApagaElMarcadorDelCofreSeleccionado()
    {
        var vm = new MainViewModel().Exploration;
        var cofre = Cofre(4114, 645);
        vm.ChestRows.Add(cofre);
        vm.GoToChestCommand.Execute(cofre);
        Assert.True(vm.HasCurrentChest); // el gesto real del usuario deja el marcador puesto

        await vm.LoadFromPathAsync(Path.Combine(Path.GetTempPath(), "terrakeep-mundo-que-no-existe.wld"));

        Assert.False(vm.HasCurrentChest);
    }

    [Fact]
    public async Task CargarOtroMundo_TampocoDejaResultadosDeBusquedaDelAnterior()
    {
        // Regla ya existente (Punto 4) que este arreglo no debe romper - se fija junto a las dos
        // de arriba porque comparten exactamente el mismo motivo: nada del mundo saliente puede
        // sobrevivir a la carga del entrante.
        var vm = new MainViewModel().Exploration;
        vm.ChestRows.Add(Cofre(1, 1));

        await vm.LoadFromPathAsync(Path.Combine(Path.GetTempPath(), "terrakeep-mundo-que-no-existe.wld"));

        Assert.Empty(vm.WorldSearchResults);
        Assert.False(vm.IsWorldLoaded);
    }
}
