using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H4-02 (cuarta auditoria de Opus, Fable, HISTORICO): "Almacenes seleccionada y luego oculta en
// Amplio deja un contenido huerfano sin pestaña activa" - la pestaña interna (Equipamiento=0/
// Inventario=1/Almacenes=2) no tenia SelectedIndex enlazado; al crecer a Amplio (IsStorageExpanded
// =true, ocultaba "Almacenes") con esa pestaña activa, WPF se quedaba apuntando a un TabItem ya
// Collapsed.
//
// T3 (catalogo de rediseño visual, "Personaje: tablero con panel lateral" - bitacora.md
// 20-sep-2026, reabierto por instruccion explicita del coordinador/usuario): el TabControl de 3
// pestañas EXCLUYENTES se fusiono en un unico tablero de scroll continuo (MainWindow.xaml,
// ObjetosBoardScroll) - Almacenes YA NO SE OCULTA NUNCA, es su propia seccion permanente a
// cualquier SizeClass. El guardia real que este fichero probaba (mover la seleccion de Almacenes a
// Inventario al crecer a Amplio) se retiro del codigo de produccion PORQUE la condicion que lo
// disparaba (Almacenes oculta) ya no puede darse - el primer test de abajo se actualiza para
// probar precisamente ESO (que ObjetosSubTabIndex ya NO se mueve nunca, a ningun tamaño).
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
    public void CrecerAAmplioConAlmacenesActiva_YaNoMueveLaSeleccion()
    {
        var vm = NewLoadedViewModel();
        vm.UpdateSizeClass(1300); // Compacto tras R-04b (umbral subido a 1320)
        vm.ObjetosSubTabIndex = 2; // Almacenes

        vm.UpdateSizeClass(1600); // Amplio real - T3: Almacenes sigue siendo su propia seccion permanente, nunca se oculta

        Assert.True(vm.IsStorageExpanded); // la propiedad en si sigue existiendo/calculandose igual, solo dejo de gobernar Visibility de una pestaña
        Assert.Equal(2, vm.ObjetosSubTabIndex); // T3: ya no hay ningun TabItem que ocultar, nunca se mueve de Almacenes
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

        vm.UpdateSizeClass(1300); // Compacto tras R-04b - "Almacenes" vuelve a estar disponible igual

        Assert.Equal(1, vm.ObjetosSubTabIndex);
    }
}
