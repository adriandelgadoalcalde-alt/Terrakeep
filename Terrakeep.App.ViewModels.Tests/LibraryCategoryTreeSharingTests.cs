using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;

namespace Terrakeep.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), T-G: "arranque sincrono - compartir una unica instancia
// del arbol de Libreria entre Libreria e Investigacion" - antes cada uno llamaba a
// LibraryCategoryTreeBuilder.Build por separado, recorriendo/agrupando/paginando el catalogo
// completo (~8469 objetos) DOS VECES en cada arranque. Ahora la parte cara se calcula una sola
// vez (cacheada, compartida por referencia) - cada consumidor sigue recibiendo su PROPIO arbol
// de CategoryNodeViewModel (IsSelected/SelectCommand independientes de verdad).
public sealed class LibraryCategoryTreeSharingTests
{
    [Fact]
    public void LibreriaEInvestigacionComparten_LosMismosDatosDeIds_PeroNodosIndependientes()
    {
        var vm = new MainViewModel();

        Assert.NotEmpty(vm.Library.RootCategories);
        Assert.NotEmpty(vm.Research.RootCategories);
        Assert.Equal(vm.Library.RootCategories.Count, vm.Research.RootCategories.Count);

        var libRoot = vm.Library.RootCategories[0];
        var resRoot = vm.Research.RootCategories[0];

        // Mismos datos reales (mismo camino real, mismo orden curado) ...
        Assert.Equal(libRoot.FullPath, resRoot.FullPath);
        Assert.Equal(libRoot.ItemIdsOrdered, resRoot.ItemIdsOrdered);
        // ... pero NUNCA la misma instancia de nodo - seleccionar en Libreria no debe tocar Investigacion.
        Assert.NotSame(libRoot, resRoot);

        libRoot.IsSelected = true;
        Assert.False(resRoot.IsSelected);
    }

    [Fact]
    public void ConstruirElArbolDosVecesReutilizaLosMismosDatosDeIdsPorReferencia()
    {
        // Dos MainViewModel distintos -> dos CharacterFileService distintos -> LibraryViewModel/
        // ResearchViewModel de cada uno llaman a Build() por separado, pero la cache estatica de
        // LibraryCategoryTreeBuilder ya deberia estar calentada desde el primer MainViewModel
        // de cualquier test anterior - la propia lista de ids (ItemIdsOrdered) de la primera
        // carpeta real debe ser la MISMA referencia en ambos, no una copia recalculada.
        var vm1 = new MainViewModel();
        var vm2 = new MainViewModel();

        var ids1 = vm1.Library.RootCategories[0].ItemIdsOrdered;
        var ids2 = vm2.Library.RootCategories[0].ItemIdsOrdered;

        Assert.Same(ids1, ids2);
    }
}
