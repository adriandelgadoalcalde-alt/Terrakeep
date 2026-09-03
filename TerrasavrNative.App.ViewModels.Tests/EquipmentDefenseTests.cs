using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), B-6: prueba determinista real - antes del arreglo, esto
// habria fallado (TotalDefense se quedaba en el valor de la PRIMERA pieza puesta, IsEmpty no
// cambia al sustituir un slot ya ocupado por otro objeto).
public sealed class EquipmentDefenseTests
{
    // Iron Helmet (id=90, defensa real=2) y Molten Helmet (id=231, defensa real=8) - dos cascos
    // vanilla reales con defensa distinta, verificados aparte contra vanilla_stats.json.
    private const int IronHelmetId = 90;
    private const int MoltenHelmetId = 231;

    private static MainViewModel NewLoadedViewModel()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"defense-test-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void SustituirElCasco_RecalculaLaDefensaTotal()
    {
        var vm = NewLoadedViewModel();
        var headSlot = vm.EquipmentGroup!.EquippedItems.Slots[0];

        headSlot.PlaceItem(IronHelmetId);
        Assert.Equal(2, vm.EquipmentGroup.TotalDefense);

        headSlot.PlaceItem(MoltenHelmetId); // sustituye un slot YA ocupado - el caso real que fallaba

        Assert.Equal(8, vm.EquipmentGroup.TotalDefense);
    }

    [Fact]
    public void VaciarElCasco_RecalculaLaDefensaTotal()
    {
        var vm = NewLoadedViewModel();
        var headSlot = vm.EquipmentGroup!.EquippedItems.Slots[0];
        headSlot.PlaceItem(MoltenHelmetId);
        Assert.Equal(8, vm.EquipmentGroup.TotalDefense);

        headSlot.ClearCommand.Execute(null);

        Assert.Equal(0, vm.EquipmentGroup.TotalDefense);
    }
}
