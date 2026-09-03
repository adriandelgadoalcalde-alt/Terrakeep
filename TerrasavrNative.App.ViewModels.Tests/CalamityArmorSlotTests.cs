using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// H3-11 (tercera auditoria de Opus, Fable): antes de este arreglo, cualquier pieza de armadura
// de Calamity se aceptaba en CUALQUIERA de los 3 slots (cabeza/cuerpo/piernas), sin comprobar
// de que parte del cuerpo era de verdad - solo miraba "es armadura". Ids sinteticos reales
// (catalog.json, extraidos con scripts/extraer-slot-armadura-calamity.js contra el atributo
// AutoloadEquip real de cada clase decompilada): AerospecBreastplate=20000243 (Body),
// EmpyreanMask=20000285 (Head), EmpyreanCuisses=20000284 (Legs).
public sealed class CalamityArmorSlotTests
{
    private const int AerospecBreastplateId = 20000243; // Body
    private const int EmpyreanMaskId = 20000285;         // Head
    private const int EmpyreanCuissesId = 20000284;      // Legs

    private static MainViewModel NewLoadedViewModel()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"calamity-armor-slot-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void UnPetoDeCalamity_SoloEncajaEnElSlotDeCuerpo()
    {
        var vm = NewLoadedViewModel();
        var items = vm.EquipmentGroup!.EquippedItems.Slots;
        var head = items[0];
        var body = items[1];
        var legs = items[2];

        Assert.False(head.AcceptsItem(AerospecBreastplateId));
        Assert.True(body.AcceptsItem(AerospecBreastplateId));
        Assert.False(legs.AcceptsItem(AerospecBreastplateId));
    }

    [Fact]
    public void UnCascoDeCalamity_SoloEncajaEnElSlotDeCabeza()
    {
        var vm = NewLoadedViewModel();
        var items = vm.EquipmentGroup!.EquippedItems.Slots;

        Assert.True(items[0].AcceptsItem(EmpyreanMaskId));
        Assert.False(items[1].AcceptsItem(EmpyreanMaskId));
        Assert.False(items[2].AcceptsItem(EmpyreanMaskId));
    }

    [Fact]
    public void UnasGrebasDeCalamity_SoloEncajanEnElSlotDePiernas()
    {
        var vm = NewLoadedViewModel();
        var items = vm.EquipmentGroup!.EquippedItems.Slots;

        Assert.False(items[0].AcceptsItem(EmpyreanCuissesId));
        Assert.False(items[1].AcceptsItem(EmpyreanCuissesId));
        Assert.True(items[2].AcceptsItem(EmpyreanCuissesId));
    }

    [Fact]
    public void ColocarUnCascoDeCalamityEnElSlotDeCuerpo_SeRechazaDeVerdad()
    {
        var vm = NewLoadedViewModel();
        var body = vm.EquipmentGroup!.EquippedItems.Slots[1];

        body.PlaceItem(EmpyreanMaskId);

        Assert.True(body.IsEmpty);
        Assert.NotNull(body.RejectionMessage);
    }
}
