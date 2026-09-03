using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// H3-13 (tercera auditoria de Opus, Fable), gemelo de B-6 (ya cerrado en
// EquipmentGroupViewModel): sustituir el buff de un slot YA ocupado y YA seleccionado en el
// panel "Editar buff seleccionado" no refrescaba los presets Minima/Media/Maxima (el filtro
// vigilaba solo IsEmpty, que no cambia de valor cuando un buff sustituye a otro dentro del
// MISMO slot ya ocupado).
public sealed class BuffEditPresetRefreshTests
{
    // Ambos ids reales vanilla, con MinTicks reales distintos (vanilla_buff_durations.json):
    // 1 (Obsidian Skin) = 21600 ticks (6min), 2 (Regeneration) = 28800 ticks (8min).
    private const int ObsidianSkinId = 1;
    private const int RegenerationId = 2;

    private static MainViewModel NewLoadedViewModel()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"buff-edit-refresh-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void SustituirElBuffDeUnSlotYaOcupadoYSeleccionado_RefrescaLosPresetsReales()
    {
        var vm = NewLoadedViewModel();
        var slot = vm.Buffs.Container!.Slots[0];
        slot.PlaceBuff(ObsidianSkinId);
        vm.SelectBuffSlot(slot);

        Assert.Equal("Mínima (6min)", vm.BuffEdit.MinLabel);

        bool colocado = slot.PlaceBuff(RegenerationId); // sustituye un slot YA ocupado y YA seleccionado

        Assert.True(colocado);
        Assert.Equal("Mínima (8min)", vm.BuffEdit.MinLabel);
    }
}
