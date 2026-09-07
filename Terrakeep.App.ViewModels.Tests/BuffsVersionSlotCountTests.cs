using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Data;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Oleada del 6-sep-2026 (Personaje > Buffs/Version): la rejilla de Buffs se construia UNA vez,
// en LoadFromPath, con los slots que traia el .plr - y no volvia a mirar la version nunca mas.
// PlrBodySerializer decide cuantos buffs escribe con la version REAL en el momento de guardar
// (44 si >=269, 22 si >=77, 10 si no - PlrBodySerializer.cs:351), asi que cambiar la version en
// la pestaña Version dejaba la rejilla mintiendo en los dos sentidos:
//   - al BAJAR: se seguian viendo (y editando) 44 slots, y al guardar se escribian 22 -> los
//     buffs de los slots 22..43 desaparecian sin ningun aviso.
//   - al SUBIR: un personaje viejo se quedaba con 22 slots para siempre, sin forma de usar los
//     22 nuevos que su version ya permite guardar.
// Ademas el techo real de duracion (S.getMaxTime(), BuffDurationPresets.MaxTicksForVersion)
// tambien depende del mismo umbral 269 y se congelaba en el de la carga.
public sealed class BuffsVersionSlotCountTests
{
    private static MainViewModel Cargar(int version)
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = version,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"buffs-version-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Theory]
    [InlineData(279, 44)]
    [InlineData(248, 22)]
    [InlineData(39, 10)]
    public void AlCargar_LaRejillaTieneLosSlotsRealesDeSuVersion(int version, int esperado)
    {
        var vm = Cargar(version);
        Assert.Equal(esperado, vm.Buffs.Container!.Slots.Count);
    }

    [Fact]
    public void BajarDe269_LaRejillaSeEncogeALos22SlotsQueDeVerdadSeEscriben()
    {
        var vm = Cargar(279);
        Assert.Equal(44, vm.Buffs.Container!.Slots.Count);

        vm.VersionEditor.SetVersionCommand.Execute(248);

        Assert.Equal(22, vm.Buffs.Container!.Slots.Count);
    }

    [Fact]
    public void SubirA269_LaRejillaCreceALos44SlotsQueSuNuevaVersionYaPermite()
    {
        var vm = Cargar(248);
        Assert.Equal(22, vm.Buffs.Container!.Slots.Count);

        vm.VersionEditor.SetVersionCommand.Execute(269);

        Assert.Equal(44, vm.Buffs.Container!.Slots.Count);
    }

    [Fact]
    public void BajarDeVersion_ConservaLosBuffsQueSiguenCabiendo()
    {
        var vm = Cargar(279);
        Assert.True(vm.Buffs.Container!.Slots[0].PlaceBuff(1));   // Obsidian Skin, slot que sobrevive
        Assert.True(vm.Buffs.Container!.Slots[30].PlaceBuff(2));  // slot 30, se pierde al bajar a 22

        vm.VersionEditor.SetVersionCommand.Execute(248);

        Assert.Equal(22, vm.Buffs.Container!.Slots.Count);
        Assert.Equal(1, vm.Buffs.Container!.Slots[0].Buff.Id);
    }

    // El techo global real de duracion es 1999999980 ticks con version>=269 y 1080000 por
    // debajo (BuffDurationPresets.MaxTicksForVersion, calco de S.getMaxTime() real) - escribir
    // segundos a mano se acota contra ese techo, y tras bajar la version tiene que acotar
    // contra el NUEVO, no contra el de la carga.
    [Fact]
    public void TrasBajarDeVersion_ElTechoDeDuracionEsElDeLaVersionNueva()
    {
        var vm = Cargar(279);
        vm.VersionEditor.SetVersionCommand.Execute(248);

        var slot = vm.Buffs.Container!.Slots[0];
        Assert.True(slot.PlaceBuff(1));
        slot.DurationSeconds = int.MaxValue / 60;

        Assert.Equal(BuffDurationPresets.MaxTicksForVersion(248) / 60, slot.DurationSeconds);
    }
}
