using TerrasavrNative.App.ViewModels;

namespace TerrasavrNative.App.ViewModels.Tests;

// Dos bugs reales reportados por el usuario probando la app (6-sep-2026), en el modo "Cofre a
// cofre" de Exploracion > Cofres:
//   - "el cuadradito de resaltado que marca 'aqui esta seleccionado esto' no aparece" - las demas
//     categorias comparten WorldSearchHitRowViewModel.IsCurrent, y ChestRowViewModel no tenia esa
//     propiedad: no era un binding roto, faltaba el estado entero.
//   - "que la opcion de acercar al seleccionar sea su PROPIA casilla, independiente" - habia una
//     sola AutoZoomOnNavigate global que leia el code-behind para TODA navegacion.
// El arnes de UI (AR-12) comprueba ademas el borde REAL pintado y el zoom real del mapa; esto
// fija la regla en la ViewModel, sin ventana ni mundo de por medio.
public sealed class ChestByChestSelectionTests
{
    private static ChestRowViewModel Chest(string nombre, int x, int y) =>
        new(nombre, null, x, y, null, []);

    private static ExplorationViewModel ConTresCofres(out ChestRowViewModel a, out ChestRowViewModel b, out ChestRowViewModel c)
    {
        var vm = new MainViewModel().Exploration;
        a = Chest("Cofre de oro", 100, 200);
        b = Chest("Cofre de la jungla", 300, 400);
        c = Chest("Cofre de hielo", 500, 600);
        vm.ChestRows.Add(a);
        vm.ChestRows.Add(b);
        vm.ChestRows.Add(c);
        return vm;
    }

    [Fact]
    public void PulsarUnCofre_LoMarcaComoActual_YSoloAEl()
    {
        var vm = ConTresCofres(out var a, out var b, out _);

        vm.GoToChestCommand.Execute(b);

        Assert.True(b.IsCurrent);
        Assert.False(a.IsCurrent);
        Assert.Equal(1, vm.ChestRows.Count(r => r.IsCurrent));
    }

    [Fact]
    public void PulsarOtroCofre_ApagaElResaltadoDelAnterior()
    {
        var vm = ConTresCofres(out var a, out var b, out _);
        vm.GoToChestCommand.Execute(a);

        vm.GoToChestCommand.Execute(b);

        Assert.False(a.IsCurrent);
        Assert.True(b.IsCurrent);
    }

    // El resaltado sobrevive a replegar: pulsar dos veces el mismo cofre lo cierra, pero sigue
    // siendo el ultimo al que se navego (mismo criterio que un resultado de busqueda repulsado).
    [Fact]
    public void ReplegarElMismoCofre_NoPierdeElResaltado()
    {
        var vm = ConTresCofres(out var a, out _, out _);

        vm.GoToChestCommand.Execute(a);
        vm.GoToChestCommand.Execute(a);

        Assert.False(a.IsExpanded);
        Assert.True(a.IsCurrent);
    }

    [Fact]
    public void LaCasillaDeCofres_NoTocaLaGlobal_NiAlReves()
    {
        var vm = ConTresCofres(out _, out _, out _);

        vm.AutoZoomOnChestNavigate = true;
        Assert.False(vm.AutoZoomOnNavigate);

        vm.AutoZoomOnChestNavigate = false;
        vm.AutoZoomOnNavigate = true;
        Assert.False(vm.AutoZoomOnChestNavigate);
    }

    // Lo que de verdad rompia el pedido del usuario: quien decide el zoom. El code-behind lee
    // NavigationWantsAutoZoom, y tiene que valer lo que diga la casilla de LA SECCION de la que
    // viene la navegacion.
    [Fact]
    public void ConLaGlobalEncendidaYLaDeCofresApagada_PulsarUnCofreNoPideZoom()
    {
        var vm = ConTresCofres(out var a, out _, out _);
        vm.AutoZoomOnNavigate = true;
        vm.AutoZoomOnChestNavigate = false;

        vm.GoToChestCommand.Execute(a);

        Assert.False(vm.NavigationWantsAutoZoom);
    }

    [Fact]
    public void ConLaDeCofresEncendidaYLaGlobalApagada_PulsarUnCofreSiPideZoom()
    {
        var vm = ConTresCofres(out var a, out _, out _);
        vm.AutoZoomOnNavigate = false;
        vm.AutoZoomOnChestNavigate = true;

        vm.GoToChestCommand.Execute(a);

        Assert.True(vm.NavigationWantsAutoZoom);
    }

    // Las demas secciones siguen compartiendo la global, que es lo que ya funcionaba y no debe
    // cambiar: cualquier navegacion que no venga de "Cofre a cofre" la sigue leyendo a ella.
    [Fact]
    public void UnaNavegacionNormal_SiguePreguntandoALaCasillaGlobal()
    {
        var vm = ConTresCofres(out _, out _, out _);
        vm.AutoZoomOnNavigate = true;
        vm.AutoZoomOnChestNavigate = false;

        vm.NavigateToTile(1234, 567);

        Assert.True(vm.NavigationWantsAutoZoom);
    }
}
