using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// H4-02 (cuarta auditoria de Opus, Fable): "Almacenes seleccionada y luego oculta en Amplio deja
// un contenido huerfano sin pestaña activa" - la pestaña interna (Equipamiento=0/Inventario=1/
// Almacenes=2) no tenia SelectedIndex enlazado; al crecer a Amplio (IsStorageExpanded=true,
// oculta "Almacenes") con esa pestaña activa, WPF se quedaba apuntando a un TabItem ya
// Collapsed.
public sealed class StorageTabOrphanTests
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
        string path = Path.Combine(Path.GetTempPath(), $"storage-tab-h402-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    [Fact]
    public void CrecerAAmplioConAlmacenesActiva_MueveLaSeleccionAInventario()
    {
        var vm = NewLoadedViewModel();
        vm.UpdateSizeClass(1300); // Normal - "Almacenes" (2) sigue visible aqui
        vm.ObjetosSubTabIndex = 2; // Almacenes

        vm.UpdateSizeClass(1600); // Amplio real - "Almacenes" pasa a oculta

        Assert.True(vm.IsStorageExpanded);
        Assert.Equal(1, vm.ObjetosSubTabIndex); // Inventario, nunca huerfana en Almacenes
    }

    [Fact]
    public void CrecerAAmplioConOtraPestañaActiva_NoTocaLaSeleccion()
    {
        var vm = NewLoadedViewModel();
        vm.UpdateSizeClass(1300);
        vm.ObjetosSubTabIndex = 0; // Equipamiento

        vm.UpdateSizeClass(1600);

        Assert.Equal(0, vm.ObjetosSubTabIndex); // no se mueve sin motivo real
    }

    [Fact]
    public void EncogerDesdeAmplio_NoTocaLaSeleccion()
    {
        var vm = NewLoadedViewModel();
        vm.UpdateSizeClass(1600);
        vm.ObjetosSubTabIndex = 1; // Inventario

        vm.UpdateSizeClass(1300); // Normal - "Almacenes" vuelve a estar disponible

        Assert.Equal(1, vm.ObjetosSubTabIndex);
    }
}
