using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H4-04 (cuarta auditoria de Opus, Fable): arrastrar un buff duplicado sobre un slot se
// rechazaba en silencio total al soltar, sin ningun cursor de prohibido mientras se arrastraba
// (a diferencia de los objetos). WouldRejectPlacingBuff es la comprobacion SIN EFECTO que
// OnBuffSlotDragOver usa para decidir el cursor - misma logica real que PlaceBuff, sin tocar
// el slot.
public sealed class BuffDragOverRejectionTests
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
        string path = Path.Combine(Path.GetTempPath(), $"buff-dragover-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void UnBuffYaColocadoEnOtroSlot_SeRechazaSinTocarNingunSlot()
    {
        var vm = NewLoadedViewModel();
        var slots = vm.Buffs.Container!.Slots;
        slots[0].PlaceBuff(1); // Obsidian Skin, id real vanilla

        bool rechazado = slots[1].WouldRejectPlacingBuff(1);

        Assert.True(rechazado);
        Assert.True(slots[1].IsEmpty); // solo es un vistazo - no coloca nada de verdad
    }

    [Fact]
    public void UnBuffLibre_NoSeRechaza()
    {
        var vm = NewLoadedViewModel();
        var slots = vm.Buffs.Container!.Slots;

        Assert.False(slots[0].WouldRejectPlacingBuff(1));
    }

    [Fact]
    public void ElMismoSlotConSuPropioBuff_NoSeRechazaASiMismo()
    {
        var vm = NewLoadedViewModel();
        var slot = vm.Buffs.Container!.Slots[0];
        slot.PlaceBuff(1);

        Assert.False(slot.WouldRejectPlacingBuff(1));
    }
}
