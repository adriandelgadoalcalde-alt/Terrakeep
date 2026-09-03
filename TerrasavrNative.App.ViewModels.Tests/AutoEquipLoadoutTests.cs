using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), Bd-b: "Auto-equipar siempre va al loadout 0, ignorando el
// seleccionado - sorpresa silenciosa si estas mirando el Loadout 2". Prueba real: seleccionar
// el Loadout 1 y comprobar que Auto-equipar coloca AHI (no en el 0, que debe quedarse vacio).
public sealed class AutoEquipLoadoutTests
{
    [Fact]
    public void AutoEquipar_ColocaEnElLoadoutSeleccionado_NoSiempreEnElLoadout0()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"autoequip-test-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);

        vm.EquipmentGroup!.SelectedLoadout = 1; // el usuario esta mirando el Loadout 1, no el principal
        var gear = vm.Builds.VanillaStages[0].Classes[0].Source; // gear real del catalogo, primera clase/etapa

        vm.AutoEquipCommand.Execute(gear);

        Assert.True(vm.EquipmentGroup!.EquippedItems.Slots[0].IsEmpty); // el loadout 0 (Puesto) NO se toco
        Assert.False(vm.EquipmentGroup!.CurrentItems.Slots[0].IsEmpty); // el Loadout 1 (el seleccionado de verdad) SI recibio el equipo
        Assert.True(vm.IsDirty);

        File.Delete(path);
    }
}
