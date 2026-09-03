using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// H3-08 (tercera auditoria de Opus, Fable): prueba determinista real - antes del arreglo,
// "Defensa total" ignoraba por completo la defensa que aportan los prefijos de accesorio reales
// (Warding/Guarding/Menacing/Hardy/Armored, +1..+4 segun Player.GrantPrefixBenefits
// decompilado). Escudo de obsidiana (id vanilla real 397, defensa base real 2, verificado
// contra vanilla_stats.json) + prefijo Warding (id vanilla real 65, +4 defensa real, verificado
// contra vanilla_prefix_effects.json) deberia sumar 6, no 2.
public sealed class PrefixDefenseTests
{
    private const int ObsidianShieldId = 397;
    private const byte WardingPrefixId = 65;

    private static MainViewModel NewLoadedViewModel()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"prefix-defense-test-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void UnAccesorioConPrefijoWarding_SumaLaDefensaRealDelPrefijoALaTotal()
    {
        var vm = NewLoadedViewModel();
        var items = vm.EquipmentGroup!.EquippedItems.Slots;
        // Slots 0-2 son cabeza/cuerpo/piernas (armadura) - el primer accesorio real es el 3.
        var accessorySlot = items[3];

        accessorySlot.PlaceItem(ObsidianShieldId);
        Assert.Equal(2, vm.EquipmentGroup.TotalDefense);

        accessorySlot.SetPrefix(ItemPrefix.Vanilla(WardingPrefixId));
        Assert.Equal(6, vm.EquipmentGroup.TotalDefense);
    }

    [Fact]
    public void QuitarElPrefijoWarding_RestaLaDefensaDelPrefijoDeLaTotal()
    {
        var vm = NewLoadedViewModel();
        var items = vm.EquipmentGroup!.EquippedItems.Slots;
        var accessorySlot = items[3];

        accessorySlot.PlaceItem(ObsidianShieldId);
        accessorySlot.SetPrefix(ItemPrefix.Vanilla(WardingPrefixId));
        Assert.Equal(6, vm.EquipmentGroup.TotalDefense);

        accessorySlot.SetPrefix(ItemPrefix.Vanilla(0)); // "Ninguno"
        Assert.Equal(2, vm.EquipmentGroup.TotalDefense);
    }
}
