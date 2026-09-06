using TerrasavrNative.App.ViewModels;

namespace TerrasavrNative.App.ViewModels.Tests;

// Bugs reales reportados por el usuario probando la app (6-sep-2026, con capturas), en
// Exploracion > Cofres:
//   - "al seleccionar un cofre el mapa no lo marca de forma que se distinga - he tenido que pasar
//     el raton a mano por encima para encontrarlo". El desplazamiento y el zoom SI funcionaban
//     (eso lo arreglo la ronda anterior, ChestByChestSelectionTests): lo que no existia era
//     ninguna marca sobre el mapa, porque la unica capa de marcadores se alimenta de
//     WorldSearchResults y "Cofre a cofre" tiene su propia lista (ChestRows).
//   - Medido de paso con el arnes (AR-13a): la plantilla de esa lista lleva desde C-06 un
//     TextBlock enlazado a "ItemCountLabel" y la fila NUNCA ha tenido esa propiedad - un binding
//     a un nombre inexistente no da error en WPF, deja el texto vacio, asi que "cuantos objetos
//     lleva dentro" no se ha visto jamas.
// El arnes comprueba el marcador REAL en el arbol visual y el desplazamiento real del mapa; esto
// fija la regla en la ViewModel, sin ventana ni mundo de por medio.
public sealed class CofresMarcadorYRecuentoTests
{
    private static ChestContentItemViewModel Objeto(int netId) => new(netId, $"Objeto {netId}", 1, null, null);

    private static ChestRowViewModel Chest(string nombre, int x, int y, int objetos = 0, bool deMod = false) =>
        new(nombre, null, x, y, null, Enumerable.Range(1, objetos).Select(Objeto).ToList(), deMod);

    private static ExplorationViewModel ConCofres(out ChestRowViewModel a, out ChestRowViewModel b)
    {
        var vm = new MainViewModel().Exploration;
        a = Chest("Cofre de oro", 100, 200, objetos: 3);
        b = Chest("Cofre de un mod", 4114, 645, objetos: 5, deMod: true);
        vm.ChestRows.Add(a);
        vm.ChestRows.Add(b);
        return vm;
    }

    [Fact]
    public void SinSeleccionarNada_NoHayMarcadorDeCofreEnElMapa()
    {
        var vm = ConCofres(out _, out _);

        Assert.False(vm.HasCurrentChest);
    }

    [Fact]
    public void PulsarUnCofre_DejaElMarcadorEnSuCasillaReal()
    {
        var vm = ConCofres(out _, out var b);

        vm.GoToChestCommand.Execute(b);

        Assert.True(vm.HasCurrentChest);
        Assert.Equal(4114, vm.CurrentChestX);
        Assert.Equal(645, vm.CurrentChestY);
    }

    [Fact]
    public void PulsarOtroCofre_MueveElMarcador_NoLoDuplica()
    {
        var vm = ConCofres(out var a, out var b);
        vm.GoToChestCommand.Execute(b);

        vm.GoToChestCommand.Execute(a);

        Assert.Equal(100, vm.CurrentChestX);
        Assert.Equal(200, vm.CurrentChestY);
        Assert.Equal(1, vm.ChestRows.Count(r => r.IsCurrent));
    }

    // "Cerrar" es el unico gesto real de "quita lo que hay marcado en el mapa" y lo comparten las
    // 5 categorias: tiene que llevarse tambien este marcador, o queda un marco suelto que nada
    // apaga.
    [Fact]
    public void CerrarLosResultados_ApagaElMarcadorYElResaltadoDeLaLista()
    {
        var vm = ConCofres(out var a, out _);
        vm.GoToChestCommand.Execute(a);

        vm.ClearOreMarksCommand.Execute(null);

        Assert.False(vm.HasCurrentChest);
        Assert.Equal(0, vm.ChestRows.Count(r => r.IsCurrent));
    }

    [Fact]
    public void CadaFilaDeCofre_DiceCuantosObjetosLleva()
    {
        var fila = Chest("Cofre de oro", 1, 2, objetos: 3);

        Assert.Equal(3, fila.ItemCount);
        Assert.False(string.IsNullOrWhiteSpace(fila.ItemCountLabel));
        Assert.Contains("3", fila.ItemCountLabel);
    }

    // El cofre cuya casilla el .wld guarda vacia (tile de un mod, ver
    // ExplorationViewModel.ChestKindName) se marca como tal para poder darle en la plantilla el
    // tooltip que lo explica, en vez del generico.
    [Fact]
    public void UnCofreDeMod_SeReconoceComoTal_YUnoNormalNo()
    {
        Assert.True(Chest("Cofre de un mod", 1, 2, deMod: true).IsModdedChest);
        Assert.False(Chest("Cofre de oro", 1, 2).IsModdedChest);
    }

    // Gemelo real en la otra lista de cofres ("Por tipo de cofre"): ahi el Id de la fila es el
    // TYPE del tile que respalda al cofre, y -1 significa "casilla vacia" (WldTile.Type). En el
    // resto de vistas el Id es un id de tile/pared/liquido o un NetId, siempre >= 0.
    [Fact]
    public void UnaFilaDeInventarioConIdNegativo_EsUnCofreDeMod()
    {
        var deMod = new WorldInventoryRowViewModel(-1, 0, 0, "Cofre de un mod", 51, null, null, default);
        var normal = new WorldInventoryRowViewModel(21, 36, 0, "Cofre de oro", 184, null, null, default);

        Assert.True(deMod.IsModdedChest);
        Assert.False(normal.IsModdedChest);
    }
}
