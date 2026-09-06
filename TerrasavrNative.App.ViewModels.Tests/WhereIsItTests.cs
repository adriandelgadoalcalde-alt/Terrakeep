using System.IO;
using System.Linq;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// H5-05 (quinta auditoria de Opus): "no se puede buscar entre los ~350 slots que el personaje
// ya tiene - ¿Donde tengo el Ala de murcielago? solo se responde a ojo". ApplyWhereIsItFilter
// es publica a proposito (mismo motivo real que CatalogBrowserViewModel.SelectCategoryCommand
// en ResearchEditableTests.cs) - sortea el debounce real de 180ms sin depender de un Dispatcher
// corriendo en un test xunit plano.
public sealed class WhereIsItTests
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
        string path = Path.Combine(Path.GetTempPath(), $"whereisit-h505-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void BuscarPorId_EncuentraElObjetoEnElInventario()
    {
        var vm = NewLoadedViewModel();
        var slot = vm.InventoryContainer!.Slots[5];
        slot.PlaceItem(2); // Bloque de tierra, id real vanilla

        vm.WhereIsItSearchText = "#2";
        vm.ApplyWhereIsItFilter();

        Assert.Single(vm.WhereIsItResults);
        var resultado = vm.WhereIsItResults[0];
        Assert.Same(slot, resultado.Slot);
        Assert.Equal("Inventario", resultado.ContainerName);
        Assert.Equal(6, resultado.SlotNumber); // 1-based real, slot 5 -> "slot 6"
    }

    [Fact]
    public void BuscarPorId_EncuentraElObjetoEnUnAlmacen()
    {
        var vm = NewLoadedViewModel();
        vm.StorageGroup!.SelectCommand.Execute(vm.StorageGroup.Options[1]); // Caja fuerte (bank2)
        var slot = vm.StorageGroup.Current.Slots[3];
        slot.PlaceItem(3);

        vm.WhereIsItSearchText = "#3";
        vm.ApplyWhereIsItFilter();

        Assert.Single(vm.WhereIsItResults);
        Assert.Same(slot, vm.WhereIsItResults[0].Slot);
        Assert.Equal("Caja fuerte", vm.WhereIsItResults[0].ContainerName);
    }

    [Fact]
    public void BuscarUnObjetoPuesto_LoEncuentraEnElLoadoutRealQueLoTiene()
    {
        var vm = NewLoadedViewModel();
        // Pildora "2" (la segunda): con CurrentLoadout=0 el conjunto 1 vive en el contenedor 0
        // (PrimaryLoadout) y el conjunto 2 en el contenedor 2 - ya no hay ninguna pildora que
        // apunte al contenedor 1 (es el hueco vacio del swap del loadout activo). Ver
        // EquipmentGroupViewModel.ContainerForLoadout.
        vm.EquipmentGroup!.SelectLoadoutCommand.Execute(vm.EquipmentGroup.LoadoutOptions[1]);
        var slot = vm.EquipmentGroup.CurrentSocial.Slots[0]; // Vanidad (cabeza) del conjunto 2
        // UpdateFrom en vez de PlaceItem a proposito: este slot exige un cascoreal de verdad
        // (AcceptedKind=ArmorHead), y lo unico que importa aqui es que WhereIsIt sepa buscar
        // en TODOS los slots de EquipmentGroup, no volver a probar la restriccion en si (ya
        // cubierta por SlotKeyboardActionsTests.cs).
        slot.UpdateFrom(new GameItem { Id = 4, Count = 1 });

        vm.WhereIsItSearchText = "#4";
        vm.ApplyWhereIsItFilter();

        Assert.Single(vm.WhereIsItResults);
        Assert.Same(slot, vm.WhereIsItResults[0].Slot);
    }

    [Fact]
    public void SinBusqueda_TodosLosSlotsConObjetoQuedanIsSearchMatchTrue()
    {
        var vm = NewLoadedViewModel();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(2);
        vm.WhereIsItSearchText = "espada";
        vm.ApplyWhereIsItFilter();
        Assert.False(slot.IsSearchMatch); // "Bloque de tierra" no coincide con "espada"

        vm.WhereIsItSearchText = string.Empty;
        vm.ApplyWhereIsItFilter();

        Assert.True(slot.IsSearchMatch);
        Assert.Empty(vm.WhereIsItSummary);
    }

    [Fact]
    public void ConBusquedaActiva_LosSlotsQueNoCoincidenQuedanAtenuados()
    {
        var vm = NewLoadedViewModel();
        var coincide = vm.InventoryContainer!.Slots[0];
        var noCoincide = vm.InventoryContainer.Slots[1];
        coincide.PlaceItem(2);
        noCoincide.PlaceItem(3);

        vm.WhereIsItSearchText = "#2";
        vm.ApplyWhereIsItFilter();

        Assert.True(coincide.IsSearchMatch);
        Assert.False(noCoincide.IsSearchMatch);
    }

    [Fact]
    public void ObjetosDuplicados_SeReflejanEnElResumenReal()
    {
        var vm = NewLoadedViewModel();
        vm.InventoryContainer!.Slots[0].PlaceItem(2);
        vm.InventoryContainer.Slots[1].PlaceItem(2); // mismo id, otro slot - duplicado real

        vm.WhereIsItSearchText = "#2";
        vm.ApplyWhereIsItFilter();

        Assert.Equal(2, vm.WhereIsItResults.Count);
        Assert.Contains("duplicado", vm.WhereIsItSummary);
    }

    [Fact]
    public void SinResultados_DiceloExplicitamente()
    {
        var vm = NewLoadedViewModel();
        vm.InventoryContainer!.Slots[0].PlaceItem(2);

        vm.WhereIsItSearchText = "objeto que no existe en absoluto";
        vm.ApplyWhereIsItFilter();

        Assert.Empty(vm.WhereIsItResults);
        Assert.Equal("Sin resultados en tu personaje.", vm.WhereIsItSummary);
    }

    [Fact]
    public void NavigateToResult_ParaUnSlotDeAlmacen_SeleccionaElAlmacenRealYElSlot()
    {
        var vm = NewLoadedViewModel();
        vm.StorageGroup!.SelectCommand.Execute(vm.StorageGroup.Options[0]); // deja el selector en Banco a proposito
        vm.StorageGroup.SelectCommand.Execute(vm.StorageGroup.Options[2]); // Fragua del Defensor (bank3) - el objeto real esta ahi
        var slot = vm.StorageGroup.Current.Slots[7];
        slot.PlaceItem(2);
        vm.StorageGroup.SelectCommand.Execute(vm.StorageGroup.Options[0]); // vuelve a Banco - la navegacion real debe corregirlo solo
        vm.WhereIsItSearchText = "#2";
        vm.ApplyWhereIsItFilter();
        var resultado = vm.WhereIsItResults.First(r => ReferenceEquals(r.Slot, slot));
        vm.IsWhereIsItOpen = true;

        vm.NavigateToWhereIsItResultCommand.Execute(resultado);

        Assert.Equal(2, vm.StorageGroup.SelectedIndex); // Fragua del Defensor, no Banco
        Assert.Same(slot, vm.ItemEdit.Slot);
        Assert.True(slot.IsSelected);
        Assert.False(vm.IsWhereIsItOpen);
    }

    [Fact]
    public void NavigateToResult_ParaUnSlotDeEquipamiento_SeleccionaElLoadoutYLaVistaReales()
    {
        var vm = NewLoadedViewModel();
        vm.EquipmentGroup!.SelectLoadoutCommand.Execute(vm.EquipmentGroup.LoadoutOptions.First(o => o.Value == 2));
        var slot = vm.EquipmentGroup.CurrentDyes.Slots[1]; // Tintes del Loadout 2
        slot.UpdateFrom(new GameItem { Id = 2, Count = 1 }); // UpdateFrom, no PlaceItem - ver comentario real de arriba
        vm.EquipmentGroup.SelectLoadoutCommand.Execute(vm.EquipmentGroup.LoadoutOptions.First(o => o.Value == 0)); // vuelve a Puesto
        vm.WhereIsItSearchText = "#2";
        vm.ApplyWhereIsItFilter();
        var resultado = vm.WhereIsItResults.First(r => ReferenceEquals(r.Slot, slot));

        vm.NavigateToWhereIsItResultCommand.Execute(resultado);

        Assert.Equal(2, vm.EquipmentGroup.SelectedLoadout);
        Assert.Equal(EquipmentKind.Dyes, vm.EquipmentGroup.SelectedKind);
        Assert.Same(slot, vm.ItemEdit.Slot);
    }

    [Fact]
    public void CargarOtroPersonaje_LimpiaLaBusquedaAnterior()
    {
        var vm = NewLoadedViewModel();
        vm.InventoryContainer!.Slots[0].PlaceItem(2);
        vm.WhereIsItSearchText = "#2";
        vm.ApplyWhereIsItFilter();
        Assert.NotEmpty(vm.WhereIsItResults);
        vm.IsWhereIsItOpen = true;

        // Cargar OTRO personaje reconstruye Containers/EquipmentGroup de cero - un resultado
        // apuntando a un ItemSlotViewModel ya descartado no tiene ningun sitio real al que ir.
        var character2 = new PlrCharacter
        {
            Name = "Test2", Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path2 = Path.Combine(Path.GetTempPath(), $"whereisit-h505-otro-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path2, PlrFile.Write(character2));
        vm.LoadFromPath(path2);
        File.Delete(path2);

        Assert.Empty(vm.WhereIsItResults);
        Assert.Equal(string.Empty, vm.WhereIsItSearchText);
        Assert.False(vm.IsWhereIsItOpen);
    }
}
