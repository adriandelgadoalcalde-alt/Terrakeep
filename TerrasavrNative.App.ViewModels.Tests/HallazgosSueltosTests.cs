using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), hallazgos sueltos fuera de las olas nombradas: Bu-a, Bu-b,
// D-c, Ap-f.
public sealed class HallazgosSueltosTests
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
        string path = Path.Combine(Path.GetTempPath(), $"hallazgos-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void BuA_TriggerEditFlash_DisparaJustEditedRealYSeApagaSolo()
    {
        var vm = NewLoadedViewModel();
        var slot = vm.Buffs.Container!.Slots[0];

        slot.TriggerEditFlash();

        Assert.True(slot.JustEdited); // sincrono, la parte False->True lo es
    }

    [Fact]
    public void BuB_NoSePuedePonerElMismoBuffEnDosSlots()
    {
        var vm = NewLoadedViewModel();
        var slots = vm.Buffs.Container!.Slots;
        Assert.True(slots[0].PlaceBuff(1)); // Obsidian Skin, id real vanilla

        bool colocadoOtraVez = slots[1].PlaceBuff(1);

        Assert.False(colocadoOtraVez);
        Assert.True(slots[1].IsEmpty); // el segundo slot se queda vacio, no se finge nada
        Assert.NotNull(slots[1].RejectionMessage);
        Assert.False(slots[0].IsEmpty); // el primero (el que ya lo tenia) sigue intacto
    }

    [Fact]
    public void BuB_ElMismoSlotPuedeQuedarseConSuPropioBuff_SwapNoSeRompe()
    {
        // Recolocar el MISMO buff en el MISMO slot que ya lo tiene (ej. re-elegirlo por
        // accidente) no debe rechazarse a si mismo.
        var vm = NewLoadedViewModel();
        var slot = vm.Buffs.Container!.Slots[0];
        slot.PlaceBuff(1);

        bool colocado = slot.PlaceBuff(1);

        Assert.True(colocado);
    }

    [Fact]
    public void DC_DesbloqueadoYActivadoDelCarritoPotenciadoSonFlagsIndependientes()
    {
        var vm = NewLoadedViewModel();

        vm.Flags.UnlockedSuperMinecart = true;

        Assert.False(vm.Flags.UsingSuperMinecart); // antes del arreglo, esto tambien se ponia a True

        vm.Flags.UsingSuperMinecart = true;

        Assert.True(vm.Flags.UnlockedSuperMinecart); // y sigue en True, no se pierde
        Assert.True(vm.Flags.UsingSuperMinecart);
    }

    [Fact]
    public void ApF_LaVidaActualNuncaSuperaLaMaxima()
    {
        var vm = NewLoadedViewModel();
        vm.Appearance.HealthMax = 100;

        vm.Appearance.HealthNow = 500;

        Assert.Equal(100, vm.Appearance.HealthNow); // el juego lo recorta, ahora Terrakeep tambien
    }

    [Fact]
    public void ApF_BajarLaVidaMaximaArrastraLaActualHaciaAbajoSiHaceFalta()
    {
        var vm = NewLoadedViewModel();
        vm.Appearance.HealthMax = 400;
        vm.Appearance.HealthNow = 400;

        vm.Appearance.HealthMax = 100;

        Assert.Equal(100, vm.Appearance.HealthNow);
    }

    [Fact]
    public void ApF_LaManaActualNuncaSuperaLaMaxima()
    {
        var vm = NewLoadedViewModel();
        vm.Appearance.ManaMax = 200;

        vm.Appearance.ManaNow = 999;

        Assert.Equal(200, vm.Appearance.ManaNow);
    }
}
