using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Data;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H3-12 (tercera auditoria de Opus, Fable): antes del arreglo, escribir a mano un numero de
// segundos lo bastante grande (Buff.Time = segundos*60) desbordaba el int y lo volvia NEGATIVO
// en silencio - Buff.Time es lo que se escribe de verdad en el .plr al guardar. Prueba
// determinista real: version >= 269 -> techo real 1999999980 ticks (S.getMaxTime(),
// BuffDurationPresets.MaxTicksForVersion) = 33333333 segundos.
public sealed class BuffDurationOverflowTests
{
    private static MainViewModel NewLoadedViewModel(int version = 279)
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = version,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"buff-duration-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void EscribirUnNumeroDeSegundosEnorme_NoDesbordaBuffTimeANegativo()
    {
        var vm = NewLoadedViewModel();
        var slot = vm.Buffs.Container!.Slots[0];
        slot.PlaceBuff(1); // Obsidian Skin

        slot.DurationSeconds = 40_000_000; // *60 = 2 400 000 000, > int.MaxValue sin acotar

        Assert.True(slot.Buff.Time > 0); // nunca negativo
        Assert.Equal(BuffDurationPresets.MaxTicksForVersion(279), slot.Buff.Time);
        Assert.Equal(BuffDurationPresets.MaxTicksForVersion(279) / 60, slot.DurationSeconds);
    }

    [Fact]
    public void EscribirUnNumeroDeSegundosDentroDelTecho_NoSeToca()
    {
        var vm = NewLoadedViewModel();
        var slot = vm.Buffs.Container!.Slots[0];
        slot.PlaceBuff(1);

        slot.DurationSeconds = 120;

        Assert.Equal(120, slot.DurationSeconds);
        Assert.Equal(120 * 60, slot.Buff.Time);
    }

    [Fact]
    public void PersonajeLegacy_UsaElTechoLegacyMasBajo()
    {
        var vm = NewLoadedViewModel(version: 100); // < 269
        var slot = vm.Buffs.Container!.Slots[0];
        slot.PlaceBuff(1);

        slot.DurationSeconds = 999_999;

        Assert.Equal(BuffDurationPresets.MaxTicksForVersion(100), slot.Buff.Time);
        Assert.True(slot.Buff.Time < BuffDurationPresets.MaxTicksForVersion(279));
    }
}
