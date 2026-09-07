using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), A-c: "los contadores de A-1 solo estan en Almacenes -
// 'Inventario (47/50)' seria igual de util y no existe en ningun sitio". ContainerViewModel.
// DisplayName ahora incluye el recuento real y en vivo, en CUALQUIER contenedor.
public sealed class ContainerCountTests
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
        string path = Path.Combine(Path.GetTempPath(), $"container-count-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void InventarioMuestraElRecuentoRealDeEntrada()
    {
        var vm = NewLoadedViewModel();

        Assert.Equal($"Inventario (0/{vm.InventoryContainer!.Slots.Count})", vm.InventoryContainer.DisplayName);
    }

    [Fact]
    public void ElRecuentoSeActualizaEnVivoAlColocarYVaciar()
    {
        var vm = NewLoadedViewModel();
        var slot = vm.InventoryContainer!.Slots[0];

        slot.PlaceItem(2); // Dirt Block, id real

        Assert.StartsWith("Inventario (1/", vm.InventoryContainer.DisplayName);

        slot.ClearCommand.Execute(null);

        Assert.StartsWith("Inventario (0/", vm.InventoryContainer.DisplayName);
    }
}
