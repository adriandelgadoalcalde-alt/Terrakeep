using System.IO;
using System.Linq;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// H6-12 (sexta auditoria de Opus, "los buffs de Calamity no distinguen buff de debuff"):
// BuffSlotViewModel.IsDebuff refleja de verdad CalamityBuffEntry.IsDebuff (Main.debuff[] real
// del ModBuff, ver scripts/extraer-debuffs-calamity.js) tras colocar un buff real.
public sealed class BuffSlotDebuffTests
{
    private static readonly CharacterFileService Service = new();

    private static MainViewModel NewLoadedViewModel()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"buff-debuff-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void ColocarUnDebuffRealDeCalamity_MarcaIsDebuffTrue()
    {
        var vm = NewLoadedViewModel();
        // Cualquier debuff real de Calamity conocido (ver CalamityBuffCatalogTests.cs para el
        // spot-check contra el codigo fuente real: "Malnourished" es el primero encontrado en
        // la practica) - no se hardcodea el id, se pide al catalogo real cual es, mas
        // resistente a que el orden del catalogo cambie.
        var entry = Service.CalamityBuffCatalog.Entries.First(e => e.IsDebuff);
        var slot = vm.Buffs.Container!.Slots[0];

        slot.PlaceBuff(entry.SyntheticId);

        Assert.True(slot.IsDebuff);
        Assert.True(slot.IsCalamity);
    }

    [Fact]
    public void ColocarUnBuffPositivoDeCalamity_NoMarcaIsDebuff()
    {
        var vm = NewLoadedViewModel();
        var entry = Service.CalamityBuffCatalog.Entries.First(e => !e.IsDebuff);
        var slot = vm.Buffs.Container!.Slots[0];

        slot.PlaceBuff(entry.SyntheticId);

        Assert.False(slot.IsDebuff);
        Assert.True(slot.IsCalamity);
    }

    [Fact]
    public void SlotVacio_NoEsDebuff()
    {
        var vm = NewLoadedViewModel();
        Assert.False(vm.Buffs.Container!.Slots[0].IsDebuff);
    }
}
