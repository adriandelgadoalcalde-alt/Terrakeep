using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// H4-07 (cuarta auditoria de Opus, Fable): "a pantalla completa sobra muchisimo espacio y la
// Libreria sigue plegada por defecto" - el motivo real de plegarla ("devolver espacio a la
// cuadricula", medido a 1080x700) desaparece en Amplio. Mismo patron real de B-2
// ("preferencia + revelado temporal") - IsLibraryCollapsed sigue siendo SOLO la preferencia,
// nunca se pisa.
public sealed class LibraryRevealInAmplioTests
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
        string path = Path.Combine(Path.GetTempPath(), $"library-reveal-h407-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void CrecerAAmplioConLaLibreriaPlegadaPorPreferencia_LaRevelaSolaSinTocarElBoton()
    {
        var vm = NewLoadedViewModel();
        vm.UpdateSizeClass(1300); // Normal - plegada por defecto
        Assert.True(vm.IsLibraryCollapsed);
        Assert.False(vm.IsLibraryVisible);

        vm.UpdateSizeClass(1600); // Amplio real

        Assert.True(vm.IsLibraryVisible); // revelada de verdad
        Assert.True(vm.IsLibraryCollapsed); // pero la preferencia del boton NO se toco
    }

    [Fact]
    public void EncogerDesdeAmplio_VuelveSolaALaPreferenciaReal()
    {
        var vm = NewLoadedViewModel();
        vm.UpdateSizeClass(1600); // Amplio - visible por el tamaño, no por preferencia
        Assert.True(vm.IsLibraryVisible);

        vm.UpdateSizeClass(1300); // Normal otra vez

        Assert.False(vm.IsLibraryVisible); // vuelve a plegada, tal cual el boton la dejo
    }

    [Fact]
    public void SiElUsuarioLaDespliegaAMano_SigueVisibleAlEncogerDeAmplio()
    {
        var vm = NewLoadedViewModel();
        vm.UpdateSizeClass(1600); // Amplio - plegada por defecto pero visible por el tamaño
        vm.ToggleLibraryCollapsedCommand.Execute(null); // el usuario la despliega A MANO (preferencia real)
        Assert.False(vm.IsLibraryCollapsed);

        vm.UpdateSizeClass(1300); // Normal - por debajo de Amplio

        Assert.True(vm.IsLibraryVisible); // sigue visible: fue el USUARIO quien la desplego
    }

    [Fact]
    public void MismoComportamientoParaLaLibreriaDeBuffs()
    {
        var vm = NewLoadedViewModel();
        vm.UpdateSizeClass(1300);
        Assert.True(vm.IsBuffLibraryCollapsed);
        Assert.False(vm.IsBuffLibraryVisible);

        vm.UpdateSizeClass(1600);

        Assert.True(vm.IsBuffLibraryVisible);
        Assert.True(vm.IsBuffLibraryCollapsed);
    }
}
