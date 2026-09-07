using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), L-c: "el tope de 300 resultados no tiene ninguna medicion
// real detras". Medido de verdad con el arnes UIA (busqueda amplia real sobre ~8469 objetos,
// Debug primera pasada): 100->158ms, 150->271ms, 300->802ms - NO escala lineal, 300 era un
// freeze real y perceptible tecleando. Bajado a 100, el punto real donde el reflow deja de
// notarse manteniendo un numero util de resultados. Elegir categoria (sincrono, sin el
// debounce real de la busqueda por texto - ver LibraryViewModel._searchDebounceTimer, que
// necesita un Dispatcher real y se verifica en el arnes UIA) es la via mas simple para probar
// el tope en si de forma determinista.
public sealed class LibraryMaxResultsTests
{
    [Fact]
    public void SeleccionarUnaCategoriaGrandeTopaLosResultadosReales()
    {
        var vm = new MainViewModel();
        // "Items by ID" (o el nodo final equivalente, cajon de sastre real de Terrasavr) es la
        // hoja mas grande real conocida - si no aparece por nombre exacto, se busca la hoja con
        // mas ids reales de todo el arbol (mismo criterio: probar contra la MAS grande real,
        // no una inventada).
        CategoryNodeViewModel? mayor = null;
        void Buscar(IEnumerable<CategoryNodeViewModel> nodes)
        {
            foreach (var n in nodes)
            {
                if (n.ItemIdsOrdered.Count > 0 && (mayor == null || n.ItemIdsOrdered.Count > mayor.ItemIdsOrdered.Count))
                    mayor = n;
                Buscar(n.Children);
            }
        }
        Buscar(vm.Library.RootCategories);
        Assert.NotNull(mayor);
        Assert.True(mayor!.ItemIdsOrdered.Count > 100, "hace falta una categoria real con mas de 100 objetos para probar el tope de verdad");

        vm.Library.SelectCategoryCommand.Execute(mayor);

        Assert.Equal(100, vm.Library.Results.Count);
    }
}
