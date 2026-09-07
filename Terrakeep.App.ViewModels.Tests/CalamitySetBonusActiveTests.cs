using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// A9-04-SETCALAMITY (informe de pulido final, C-10b, cierra L3-b): "3 piezas de un set real de
// Calamity -> ActiveSetBonusText != null". Antes ActiveCalamitySetBonusText comparaba tres
// SetBonus por igualdad de texto - cuerpo/piernas de Calamity NUNCA tienen SetBonus propio (solo
// el casco), asi que esta condicion era matematicamente imposible de cumplir con las 3 puestas:
// "una funcion anunciada, probada y muerta" (informe §3). Mismos ids reales que
// CalamityArmorSlotTests/CalamityArmorSetCatalogTests.
public sealed class CalamitySetBonusActiveTests
{
    private const int AerospecBreastplateId = 20000243;
    private const int AerospecHeadMeleeId = 20000245;
    private const int AerospecLeggingsId = 20000249;
    private const int EmpyreanMaskId = 20000285; // Head de OTRO set real, sin bono propio de Aerospec

    private static MainViewModel NewLoadedViewModel()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"calamity-set-bonus-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void LasTresPiezasRealesDeAerospecPuestas_ActivaElBonoDeSetDeVerdad()
    {
        var vm = NewLoadedViewModel();
        var items = vm.EquipmentGroup!.EquippedItems.Slots;

        items[0].PlaceItem(AerospecHeadMeleeId);
        items[1].PlaceItem(AerospecBreastplateId);
        items[2].PlaceItem(AerospecLeggingsId);

        Assert.False(string.IsNullOrEmpty(vm.EquipmentGroup.ActiveSetBonusText));
    }

    [Fact]
    public void DosPiezasDeSetsDistintos_NuncaActivaNingunBono()
    {
        var vm = NewLoadedViewModel();
        var items = vm.EquipmentGroup!.EquippedItems.Slots;

        items[0].PlaceItem(EmpyreanMaskId); // casco de OTRO set
        items[1].PlaceItem(AerospecBreastplateId);
        items[2].PlaceItem(AerospecLeggingsId);

        Assert.Null(vm.EquipmentGroup.ActiveSetBonusText);
    }
}
