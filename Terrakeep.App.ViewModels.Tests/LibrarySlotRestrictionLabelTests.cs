using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), L-b: "el aviso de 'solo validos para el slot
// seleccionado' es un texto mas dentro de ResultsSummary, facil de pasar por alto - y ni
// siquiera dice CUAL slot". LibraryViewModel.SlotRestrictionLabel ahora lleva el rol real del
// slot (SlotRoleLabel, "Cabeza"/"Accesorio 3"/"Tinte"...), null cuando no hay restriccion.
public sealed class LibrarySlotRestrictionLabelTests
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
        string path = Path.Combine(Path.GetTempPath(), $"library-slot-restriction-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void SinPickTarget_NoHayEtiquetaDeRestriccion()
    {
        var vm = NewLoadedViewModel();

        Assert.Null(vm.Library.SlotRestrictionLabel);
    }

    [Fact]
    public void PickTargetSinRestriccion_InventarioNormal_NoHayEtiqueta()
    {
        var vm = NewLoadedViewModel();

        vm.Library.PickTarget = vm.InventoryContainer!.Slots[0];

        Assert.Null(vm.Library.SlotRestrictionLabel);
    }

    [Fact]
    public void PickTargetConRestriccion_MuestraElRolRealDelSlot()
    {
        var vm = NewLoadedViewModel();
        // El slot de Casco (ArmorHead) real, primer slot del equipo puesto.
        var cascoSlot = vm.EquipmentGroup!.CurrentItems.Slots[0];

        vm.Library.PickTarget = cascoSlot;

        Assert.Equal("Cabeza", vm.Library.SlotRestrictionLabel);
    }

    [Fact]
    public void CancelarLaEleccionLimpiaLaEtiqueta()
    {
        var vm = NewLoadedViewModel();
        var cascoSlot = vm.EquipmentGroup!.CurrentItems.Slots[0];
        vm.Library.PickTarget = cascoSlot;
        Assert.NotNull(vm.Library.SlotRestrictionLabel);

        vm.Library.CancelPickCommand.Execute(null);

        Assert.Null(vm.Library.SlotRestrictionLabel);
    }
}
