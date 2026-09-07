using System.IO;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), Ola 3 - H-1, H-2, H-3: la cabecera global gana un nombre
// de personaje REALMENTE editable, una linea con archivo+version, un aviso discreto si el
// nombre no coincide con el fichero, y un canal de error visible en cualquier pestaña.
public sealed class CabeceraGlobalTests
{
    private static PlrCharacter NuevoPersonaje(string nombre, int version = 269) => new()
    {
        Name = nombre,
        Version = version,
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    private static (MainViewModel Vm, string Path) NewLoadedViewModel(string nombre = "Test", int version = 269)
    {
        string path = Path.Combine(Path.GetTempPath(), $"cabecera-test-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje(nombre, version)));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        return (vm, path);
    }

    [Fact]
    public void EditarElNombre_EscribeDeVerdadAlPersonaje_YMarcaSucio_PeroNoAlCargar()
    {
        var (vm, path) = NewLoadedViewModel();
        Assert.False(vm.IsDirty); // cargar no debe marcar sucio por si solo

        vm.CharacterName = "NuevoNombre";
        Assert.True(vm.IsDirty);

        vm.SaveCommand.Execute(null);
        var vm2 = new MainViewModel();
        vm2.LoadFromPath(path); // round-trip real: si escribio de verdad, se recarga con el nombre nuevo
        Assert.Equal("NuevoNombre", vm2.CharacterName);

        File.Delete(path);
        File.Delete(path + ".bak");
    }

    [Fact]
    public void FileVersionLine_MuestraElArchivoYLaVersionResueltaAsuLabelReal()
    {
        var (vm, path) = NewLoadedViewModel(version: 269);
        Assert.Contains(Path.GetFileName(path), vm.FileVersionLine);
        Assert.Contains("1.4.4.0", vm.FileVersionLine); // etiqueta real para la version 269

        vm.VersionEditor.SetVersionCommand.Execute(39);
        Assert.Contains("1.1.2", vm.FileVersionLine); // etiqueta real para la version 39, se actualiza en vivo

        File.Delete(path);
    }

    [Fact]
    public void NameFileMismatch_TrueSiElNombreDelArchivoNoCoincideConElPersonaje()
    {
        var (vm, path) = NewLoadedViewModel("Test"); // ruta con GUID, nombre real "Test" - nunca coinciden
        Assert.True(vm.NameFileMismatch);
        File.Delete(path);
    }

    [Fact]
    public void NameFileMismatch_FalseSiElNombreDelArchivoSiCoincide()
    {
        string path = Path.Combine(Path.GetTempPath(), $"CoincideDeVerdad-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje(Path.GetFileNameWithoutExtension(path))));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);

        Assert.False(vm.NameFileMismatch);
        File.Delete(path);
    }

    [Fact]
    public void CargarConError_SubeElMensajeAlCanalGlobal_YSeLimpiaEnElSiguienteExito()
    {
        var vm = new MainViewModel();

        vm.LoadFromPath(Path.Combine(Path.GetTempPath(), $"no-existe-{Guid.NewGuid():N}.plr"));
        Assert.NotNull(vm.GlobalErrorMessage);
        Assert.Contains("Error al cargar", vm.GlobalErrorMessage);

        string realPath = Path.Combine(Path.GetTempPath(), $"cabecera-test-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(realPath, PlrFile.Write(NuevoPersonaje("Test")));
        vm.LoadFromPath(realPath);

        Assert.Null(vm.GlobalErrorMessage); // una carga con exito limpia el aviso anterior
        File.Delete(realPath);
    }

    [Fact]
    public void GuardarConError_SubeElMensajeAlCanalGlobal_YSeLimpiaEnElSiguienteExito()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"h3-guardar-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "personaje.plr");
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Test")));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        Assert.Null(vm.GlobalErrorMessage);

        Directory.Delete(dir, recursive: true); // fuerza que el siguiente Save() falle de verdad (ruta ya no existe)
        vm.SaveCommand.Execute(null);
        Assert.NotNull(vm.GlobalErrorMessage);
        Assert.Contains("Error al guardar", vm.GlobalErrorMessage);

        Directory.CreateDirectory(dir);
        vm.SaveCommand.Execute(null);
        Assert.Null(vm.GlobalErrorMessage); // un guardado con exito limpia el aviso anterior

        Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public void DismissGlobalError_LoLimpiaAMano()
    {
        var vm = new MainViewModel();
        vm.LoadFromPath(Path.Combine(Path.GetTempPath(), $"no-existe-{Guid.NewGuid():N}.plr"));
        Assert.NotNull(vm.GlobalErrorMessage);

        vm.DismissGlobalErrorCommand.Execute(null);

        Assert.Null(vm.GlobalErrorMessage);
    }
}
