using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), A-d: "operaciones en bloque - ordenar, vaciar contenedor,
// mover todo al banco" - ninguna de las 3 existia, solo "Vaciar slot" individual.
public sealed class ContainerBulkOpsTests
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
        string path = Path.Combine(Path.GetTempPath(), $"bulk-ops-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void OrdenarEmpaquetaPorIdAscendenteYDejaVaciosAlFinal()
    {
        var vm = NewLoadedViewModel();
        var inv = vm.InventoryContainer!;
        inv.Slots[0].PlaceItem(500);
        inv.Slots[3].PlaceItem(100);
        inv.Slots[7].PlaceItem(300);

        inv.SortCommand.Execute(null);

        Assert.Equal(100, inv.Slots[0].ItemId);
        Assert.Equal(300, inv.Slots[1].ItemId);
        Assert.Equal(500, inv.Slots[2].ItemId);
        Assert.True(inv.Slots[3].IsEmpty);
    }

    [Fact]
    public void VaciarContenedorDejaTodoElContenedorVacio()
    {
        var vm = NewLoadedViewModel();
        var inv = vm.InventoryContainer!;
        inv.Slots[0].PlaceItem(2);
        inv.Slots[5].PlaceItem(9);

        inv.ClearAllCommand.Execute(null);

        Assert.All(inv.Slots, s => Assert.True(s.IsEmpty));
    }

    [Fact]
    public void MoverTodoAlBancoTraslaLosObjetosYVaciaElOrigen()
    {
        var vm = NewLoadedViewModel();
        var inv = vm.InventoryContainer!;
        inv.Slots[0].PlaceItem(2);
        inv.Slots[1].PlaceItem(9);

        Assert.True(vm.MoveInventoryToStorageCommand.CanExecute(null));
        vm.MoveInventoryToStorageCommand.Execute(null);

        Assert.True(inv.Slots[0].IsEmpty);
        Assert.True(inv.Slots[1].IsEmpty);
        var bank = vm.StorageGroup!.Current;
        Assert.Equal(2, bank.Slots[0].ItemId);
        Assert.Equal(9, bank.Slots[1].ItemId);
        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void MoverTodoAlBancoNoPierdeLoQueNoCabe()
    {
        var vm = NewLoadedViewModel();
        var inv = vm.InventoryContainer!;
        var bank = vm.StorageGroup!.Current;
        // Llena el banco entero salvo un hueco.
        for (int i = 0; i < bank.Slots.Count - 1; i++)
            bank.Slots[i].PlaceItem(2);
        inv.Slots[0].PlaceItem(3);
        inv.Slots[1].PlaceItem(4); // este no debe caber

        int moved = inv.MoveAllTo(bank);

        Assert.Equal(1, moved);
        Assert.True(inv.Slots[0].IsEmpty);
        Assert.False(inv.Slots[1].IsEmpty); // se queda donde estaba, no se pierde
        Assert.Equal(4, inv.Slots[1].ItemId);
    }
}
