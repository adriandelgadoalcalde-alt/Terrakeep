using System.IO;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// H5-01 (quinta auditoria de Opus): "casi toda edicion del personaje es irreversible". Verifica
// de extremo a extremo (personaje real cargado en un MainViewModel real, no un UndoStack
// aislado) que Deshacer/Rehacer cubren de verdad los caminos de edicion reales.
public sealed class UndoStackTests
{
    private static PlrCharacter NuevoPersonaje(string nombre) => new()
    {
        Name = nombre,
        Version = 279,
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    private static (MainViewModel Vm, string Path) NuevoCargado(string nombre = "Test")
    {
        string path = Path.Combine(Path.GetTempPath(), $"undo-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje(nombre)));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        return (vm, path);
    }

    [Fact]
    public void PlaceItem_ColocarYDeshacer_VuelveAlSlotVacio()
    {
        var (vm, path) = NuevoCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        Assert.False(vm.UndoStack.CanUndo);

        slot.PlaceItem(2); // Dirt Block

        Assert.True(vm.UndoStack.CanUndo);
        Assert.Equal(2, slot.ItemId);

        vm.UndoEditCommand.Execute(null);

        Assert.True(slot.IsEmpty);
        Assert.False(vm.UndoStack.CanUndo);
        Assert.True(vm.UndoStack.CanRedo);

        File.Delete(path);
    }

    [Fact]
    public void PlaceItem_DeshacerYRehacer_VuelveAPonerElMismoObjeto()
    {
        var (vm, path) = NuevoCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(2);

        vm.UndoEditCommand.Execute(null);
        Assert.True(slot.IsEmpty);

        vm.RedoEditCommand.Execute(null);
        Assert.Equal(2, slot.ItemId);
        Assert.False(vm.UndoStack.CanRedo);

        File.Delete(path);
    }

    [Fact]
    public void UnaEdicionNueva_TrasDeshacer_TruncaLaColaDeRehacer()
    {
        var (vm, path) = NuevoCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(2);
        vm.UndoEditCommand.Execute(null);
        Assert.True(vm.UndoStack.CanRedo);

        slot.PlaceItem(3); // edicion nueva - invalida el "rehacer" que se habia deshecho

        Assert.False(vm.UndoStack.CanRedo);
        Assert.Equal(3, slot.ItemId);

        File.Delete(path);
    }

    [Fact]
    public void MultiplesEdicionesEnVariosSlots_SeDeshacenEnOrdenInverso()
    {
        var (vm, path) = NuevoCargado();
        var slotA = vm.InventoryContainer!.Slots[0];
        var slotB = vm.InventoryContainer!.Slots[1];
        slotA.PlaceItem(2);
        slotB.PlaceItem(3);

        vm.UndoEditCommand.Execute(null);
        Assert.True(slotB.IsEmpty);
        Assert.Equal(2, slotA.ItemId); // el primero (A) todavia no se toco

        vm.UndoEditCommand.Execute(null);
        Assert.True(slotA.IsEmpty);
        Assert.False(vm.UndoStack.CanUndo);

        File.Delete(path);
    }

    [Fact]
    public void Deshacer_ConservaCantidadYPrefijoYFavorito()
    {
        var (vm, path) = NuevoCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(2);
        slot.Count = 25;
        slot.ToggleFavoriteCommand.Execute(null);
        Assert.Equal(25, slot.Item.Count);
        Assert.True(slot.IsFavorited);

        vm.UndoEditCommand.Execute(null); // deshace el favorito
        Assert.False(slot.IsFavorited);
        Assert.Equal(25, slot.Item.Count); // la cantidad no se toca (es una entrada distinta)

        vm.UndoEditCommand.Execute(null); // deshace la cantidad
        Assert.Equal(1, slot.Item.Count);

        File.Delete(path);
    }

    [Fact]
    public void Clear_SobreUnSlotConObjeto_SeDeshaceRestaurandoloEntero()
    {
        var (vm, path) = NuevoCargado();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(2);
        slot.Count = 10;

        slot.ClearCommand.Execute(null);
        Assert.True(slot.IsEmpty);

        vm.UndoEditCommand.Execute(null);
        Assert.Equal(2, slot.ItemId);
        Assert.Equal(10, slot.Item.Count);

        File.Delete(path);
    }

    [Fact]
    public void CargarOtroPersonaje_LimpiaElHistorialAnterior()
    {
        var (vm, path) = NuevoCargado();
        vm.InventoryContainer!.Slots[0].PlaceItem(2);
        Assert.True(vm.UndoStack.CanUndo);

        string path2 = Path.Combine(Path.GetTempPath(), $"undo-otro-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path2, PlrFile.Write(NuevoPersonaje("Otro")));
        vm.LoadFromPath(path2);

        Assert.False(vm.UndoStack.CanUndo);
        Assert.False(vm.UndoStack.CanRedo);

        File.Delete(path);
        File.Delete(path2);
    }

    [Fact]
    public void MoverTodoAlAlmacen_EsUnaSolaEntradaDeDeshacer()
    {
        var (vm, path) = NuevoCargado();
        vm.InventoryContainer!.Slots[10].PlaceItem(2);
        vm.InventoryContainer!.Slots[11].PlaceItem(3);
        int entradasAntes = vm.UndoStack.Entries.Count;

        vm.MoveInventoryToStorageCommand.Execute(null);

        // Una sola entrada nueva para los 2 objetos movidos a la vez (no 2 sueltas).
        Assert.Equal(entradasAntes + 1, vm.UndoStack.Entries.Count);
        Assert.True(vm.InventoryContainer.Slots[10].IsEmpty);
        Assert.True(vm.InventoryContainer.Slots[11].IsEmpty);

        vm.UndoEditCommand.Execute(null);

        // Deshacer el traslado en bloque restaura AMBOS objetos de una vez.
        Assert.Equal(2, vm.InventoryContainer.Slots[10].ItemId);
        Assert.Equal(3, vm.InventoryContainer.Slots[11].ItemId);

        File.Delete(path);
    }

    [Fact]
    public void SwapWith_ArrastrarEntreDosSlots_CadaUnoSeDeshaceConSuPropiaEntrada()
    {
        var (vm, path) = NuevoCargado();
        var slotA = vm.InventoryContainer!.Slots[0];
        var slotB = vm.InventoryContainer!.Slots[1];
        slotA.PlaceItem(2);
        slotB.PlaceItem(3);
        int entradasAntes = vm.UndoStack.Entries.Count;

        slotA.SwapWith(slotB);
        Assert.Equal(3, slotA.ItemId);
        Assert.Equal(2, slotB.ItemId);
        Assert.Equal(entradasAntes + 2, vm.UndoStack.Entries.Count); // 2 slots cambiaron, 2 entradas reales

        vm.UndoEditCommand.Execute(null);
        vm.UndoEditCommand.Execute(null);

        Assert.Equal(2, slotA.ItemId);
        Assert.Equal(3, slotB.ItemId);

        File.Delete(path);
    }
}
