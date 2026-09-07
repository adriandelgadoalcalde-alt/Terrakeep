using System.IO;
using System.Linq;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H5-13 (quinta auditoria de Opus): "el estado vacio de la Libreria... es un rectangulo en
// blanco" - ShowRootCategoryCards subio de ResearchViewModel (H4-07 punto 3) a la base
// compartida (CatalogBrowserViewModel, H5-15), y LibraryViewModel la sobrescribe con una
// condicion extra real (restriccion de slot). Mismo patron de test real que
// ResearchRootCategoryCardsTests.cs.
public sealed class LibraryRootCategoryCardsTests
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
        string path = Path.Combine(Path.GetTempPath(), $"library-cards-h513-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void SinCarpetaNiBusquedaNiRestriccion_MuestraLasTarjetasDeCarpetaRaiz()
    {
        var vm = NewLoadedViewModel();

        Assert.True(vm.Library.ShowRootCategoryCards);
        Assert.NotEmpty(vm.Library.RootCategories);
    }

    [Fact]
    public void ElegirUnaCarpeta_OcultaLasTarjetas()
    {
        var vm = NewLoadedViewModel();
        var carpeta = vm.Library.RootCategories.First();

        vm.Library.SelectCategoryCommand.Execute(carpeta);

        Assert.False(vm.Library.ShowRootCategoryCards);
    }

    [Fact]
    public void EscribirUnaBusqueda_OcultaLasTarjetasDeInmediato()
    {
        var vm = NewLoadedViewModel();

        // H5-13: a diferencia de Results (que sigue debounceando 180ms), ShowRootCategoryCards
        // se notifica de forma instantanea en OnSearchTextChanged - no hace falta esperar nada.
        vm.Library.SearchText = "espada";

        Assert.False(vm.Library.ShowRootCategoryCards);
    }

    [Fact]
    public void QuitarLaCarpetaSinBusquedaActiva_VuelveAMostrarLasTarjetas()
    {
        var vm = NewLoadedViewModel();
        var carpeta = vm.Library.RootCategories.First();
        vm.Library.SelectCategoryCommand.Execute(carpeta);
        Assert.False(vm.Library.ShowRootCategoryCards);

        vm.Library.ClearCategoryCommand.Execute(null);

        Assert.True(vm.Library.ShowRootCategoryCards);
    }

    // H5-13: unica de las 3 superficies con esta condicion extra real - con un slot restringido
    // como destino (PickTarget con AcceptedKind real, no SlotKind.None), el catalogo YA se
    // reduce a un conjunto pequeño y util (ver hasSlotRestriction en LibraryViewModel.
    // ApplyFilter) - mostrar las carpetas raiz genericas ahi seria un paso atras.
    [Fact]
    public void SlotDestinoConRestriccionReal_NoMuestraLasTarjetas()
    {
        var vm = NewLoadedViewModel();
        var slotRestringido = vm.EquipmentGroup!.AllContainers
            .SelectMany(c => c.Slots)
            .First(s => s.AcceptedKind != SlotKind.None);

        vm.Library.PickTarget = slotRestringido;

        Assert.False(vm.Library.ShowRootCategoryCards);
    }

    // Todo slot real de EquipmentGroup (Items/Social/Dyes) SIEMPRE tiene una restriccion real
    // (ArmorHead/ArmorBody/ArmorLegs/Accessory/Dye - ver EquipmentGroupViewModel.AddSlotSet,
    // "los 10 slots reales" nunca caen al SlotKind.None de reserva) - el caso real de destino
    // SIN restriccion es un slot de Inventario corriente (cualquier objeto encaja).
    [Fact]
    public void SlotDestinoSinRestriccion_SigueMostrandoLasTarjetas()
    {
        var vm = NewLoadedViewModel();
        var slotSinRestriccion = vm.InventoryContainer!.Slots.First(s => s.AcceptedKind == SlotKind.None);

        vm.Library.PickTarget = slotSinRestriccion;

        Assert.True(vm.Library.ShowRootCategoryCards);
    }
}
