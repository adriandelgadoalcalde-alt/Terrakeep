using System.IO;
using System.Linq;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H5-13 (quinta auditoria de Opus): gemelo real de LibraryRootCategoryCardsTests.cs -
// BuffLibraryViewModel no tiene ningun concepto de "restriccion de slot" (a diferencia de
// LibraryViewModel), asi que usa directamente la condicion generica de CatalogBrowserViewModel,
// sin override.
public sealed class BuffLibraryRootCategoryCardsTests
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
        string path = Path.Combine(Path.GetTempPath(), $"bufflibrary-cards-h513-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void SinCarpetaNiBusqueda_MuestraLasTarjetasDeCarpetaRaiz()
    {
        var vm = NewLoadedViewModel();

        Assert.True(vm.BuffLibrary.ShowRootCategoryCards);
        Assert.NotEmpty(vm.BuffLibrary.RootCategories);
    }

    [Fact]
    public void ElegirUnaCarpeta_OcultaLasTarjetas()
    {
        var vm = NewLoadedViewModel();
        var carpeta = vm.BuffLibrary.RootCategories.First();

        vm.BuffLibrary.SelectCategoryCommand.Execute(carpeta);

        Assert.False(vm.BuffLibrary.ShowRootCategoryCards);
    }

    [Fact]
    public void EscribirUnaBusqueda_OcultaLasTarjetasDeInmediato()
    {
        var vm = NewLoadedViewModel();

        vm.BuffLibrary.SearchText = "piel";

        Assert.False(vm.BuffLibrary.ShowRootCategoryCards);
    }

    [Fact]
    public void QuitarLaCarpetaSinBusquedaActiva_VuelveAMostrarLasTarjetas()
    {
        var vm = NewLoadedViewModel();
        var carpeta = vm.BuffLibrary.RootCategories.First();
        vm.BuffLibrary.SelectCategoryCommand.Execute(carpeta);
        Assert.False(vm.BuffLibrary.ShowRootCategoryCards);

        vm.BuffLibrary.ClearCategoryCommand.Execute(null);

        Assert.True(vm.BuffLibrary.ShowRootCategoryCards);
    }

    // H5-13: ItemCount (poblado por BuffLibraryTreeBuilder para todo nodo, ver su comentario
    // real) es lo que las tarjetas nuevas muestran - confirma que no es un campo muerto.
    [Fact]
    public void ItemCountDeLasCarpetasRaiz_EstaPobladoDeVerdad()
    {
        var vm = NewLoadedViewModel();

        Assert.All(vm.BuffLibrary.RootCategories, carpeta => Assert.True(carpeta.ItemCount > 0));
    }
}
