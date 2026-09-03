using System.IO;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), T-B: "elegir OTRO personaje en Inicio con cambios sin
// guardar los tira sin avisar". MainViewModel.ConfirmDiscardChanges es el gancho real que
// MainWindow rellena con el dialogo Si/No/Cancelar - aqui se prueba SOLO la logica real de la
// decision (con o sin cambios sin guardar), sin ninguna Window ni MessageBox real de por medio.
public sealed class ConfirmDiscardChangesTests
{
    private static readonly EquipmentAppearanceResolver EquipAppearance = new CharacterFileService().EquipmentAppearance;


    private static (MainViewModel Vm, string Path) WriteAndLoad(string name)
    {
        var character = new PlrCharacter
        {
            Name = name,
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"confirm-discard-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        return (new MainViewModel(), path);
    }

    private static CharacterListEntryViewModel EntryFor(string path)
    {
        var character = PlrFile.Read(File.ReadAllBytes(path));
        return new CharacterListEntryViewModel(path, character, isCalamity: false, DateTime.UtcNow, EquipAppearance);
    }

    [Fact]
    public void ElegirOtroPersonajeConCambiosSinGuardar_SiElHookCancela_NoCargaNiPierdeElActual()
    {
        var (vm, path1) = WriteAndLoad("Uno");
        vm.LoadFromPath(path1);
        vm.Appearance.Swatches[0].R = vm.Appearance.Swatches[0].R == 0 ? 1 : 0; // deja el personaje sucio de verdad
        Assert.True(vm.IsDirty);

        vm.ConfirmDiscardChanges = () => false; // el usuario pulsa "Cancelar" en el dialogo real
        string path2 = Path.Combine(Path.GetTempPath(), $"confirm-discard-otro-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path2, PlrFile.Write(new PlrCharacter
        {
            Name = "Dos",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        }));

        vm.Home.OpenCommand.Execute(EntryFor(path2));

        Assert.Equal("Uno", vm.CharacterName); // NO se ha cargado el segundo - se respeta la cancelacion
        Assert.True(vm.IsDirty); // los cambios sin guardar del primero siguen ahi

        File.Delete(path1);
        File.Delete(path2);
    }

    [Fact]
    public void ElegirOtroPersonajeConCambiosSinGuardar_SiElHookConfirma_CargaElNuevo()
    {
        var (vm, path1) = WriteAndLoad("Uno");
        vm.LoadFromPath(path1);
        vm.Appearance.Swatches[0].R = vm.Appearance.Swatches[0].R == 0 ? 1 : 0;
        Assert.True(vm.IsDirty);

        vm.ConfirmDiscardChanges = () => true; // el usuario elige "No" (descartar) o guarda con exito
        string path2 = Path.Combine(Path.GetTempPath(), $"confirm-discard-otro-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path2, PlrFile.Write(new PlrCharacter
        {
            Name = "Dos",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        }));

        vm.Home.OpenCommand.Execute(EntryFor(path2));

        Assert.Equal("Dos", vm.CharacterName); // esta vez si se carga el segundo
        Assert.False(vm.IsDirty); // una carga fresca nunca deja sucio

        File.Delete(path1);
        File.Delete(path2);
    }

    [Fact]
    public void ElegirOtroPersonajeSinCambiosSinGuardar_NoConsultaElHook()
    {
        var (vm, path1) = WriteAndLoad("Uno");
        vm.LoadFromPath(path1);
        Assert.False(vm.IsDirty); // recien cargado, nada que perder todavia

        bool hookLlamado = false;
        vm.ConfirmDiscardChanges = () => { hookLlamado = true; return false; };
        string path2 = Path.Combine(Path.GetTempPath(), $"confirm-discard-otro-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path2, PlrFile.Write(new PlrCharacter
        {
            Name = "Dos",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        }));

        vm.Home.OpenCommand.Execute(EntryFor(path2));

        Assert.False(hookLlamado); // sin nada que perder, no hace falta ni preguntar
        Assert.Equal("Dos", vm.CharacterName); // se carga directo

        File.Delete(path1);
        File.Delete(path2);
    }
}
