using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), Bd-c: "sin resolver" (el pid no existe en el catalogo) y
// "sin hueco libre" (el inventario esta lleno) eran el mismo contador "Skipped" fundido en un
// unico mensaje - dos causas reales distintas (una sin arreglo posible por el usuario, la otra
// si) deben contarse y decirse aparte.
public sealed class AutoEquipCountersTests
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
        string path = Path.Combine(Path.GetTempPath(), $"autoequip-counters-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void ArmaConPidInexistente_CuentaComoSinResolver_NoComoSinHueco()
    {
        var vm = NewLoadedViewModel();
        var gear = new BuildClassGear { Weapons = [new BuildItemRef { Pid = "esto_no_existe_de_verdad_xyz" }] };

        vm.AutoEquipCommand.Execute(gear);

        Assert.Contains("1 sin resolver", vm.StatusMessage);
        Assert.DoesNotContain("sin hueco libre", vm.StatusMessage);
    }

    [Fact]
    public void InventarioLleno_CuentaComoSinHueco_NoComoSinResolver()
    {
        var vm = NewLoadedViewModel();
        foreach (var slot in vm.InventoryContainer!.Slots)
            slot.PlaceItem(2); // llena el inventario entero con Dirt Block, id real
        var gear = vm.Builds.VanillaStages[0].Classes[0].Source; // arma real, resoluble de sobra

        vm.AutoEquipCommand.Execute(gear);

        Assert.Contains("sin hueco libre", vm.StatusMessage);
        Assert.DoesNotContain("sin resolver", vm.StatusMessage);
    }
}
