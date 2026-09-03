using System.IO;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Segunda auditoria de Opus (Fable), I-a e I-b. Las pruebas de Duplicate/RestoreBackup usan
// SIEMPRE una carpeta temporal propia (nunca la carpeta real de Players de tModLoader) para
// que ningun fichero de prueba llegue a escribirse cerca de un personaje real - HomeViewModel
// solo actua sobre entry.FilePath, no exige que la entrada este en Characters ni que viva en la
// carpeta real escaneada.
public sealed class HomeCardTests
{
    private static PlrCharacter NuevoPersonaje(string nombre) => new()
    {
        Name = nombre,
        Version = 279,
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    [Fact]
    public void UpdateCurrentPath_MarcaIsCurrentSoloEnLaTarjetaQueCoincide()
    {
        var home = new HomeViewModel();
        var a = new CharacterListEntryViewModel(@"C:\a.plr", NuevoPersonaje("A"), false, DateTime.UtcNow);
        var b = new CharacterListEntryViewModel(@"C:\b.plr", NuevoPersonaje("B"), false, DateTime.UtcNow);
        home.Characters.Add(a);
        home.Characters.Add(b);

        home.UpdateCurrentPath(@"C:\b.plr");

        Assert.False(a.IsCurrent);
        Assert.True(b.IsCurrent);
    }

    [Fact]
    public void CargarUnPersonaje_MarcaSuTarjetaDeInicioComoActual()
    {
        var vm = new MainViewModel();
        string path = Path.Combine(Path.GetTempPath(), $"home-current-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Test")));
        var entry = new CharacterListEntryViewModel(path, PlrFile.Read(File.ReadAllBytes(path)), false, DateTime.UtcNow);
        vm.Home.Characters.Add(entry);

        vm.LoadFromPath(path);

        Assert.True(entry.IsCurrent);
        File.Delete(path);
    }

    [Fact]
    public void Duplicate_CreaUnaCopiaRealEnLaMismaCarpeta_SinTocarElOriginal()
    {
        var home = new HomeViewModel();
        string dir = Path.Combine(Path.GetTempPath(), $"home-dup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "MiPersonaje.plr");
        var character = NuevoPersonaje("MiPersonaje");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var entry = new CharacterListEntryViewModel(path, character, isCalamity: false, DateTime.UtcNow);

        home.DuplicateCommand.Execute(entry);

        string copia = Path.Combine(dir, "MiPersonaje (copia).plr");
        Assert.True(File.Exists(copia));
        Assert.True(File.Exists(path)); // el original sigue intacto
        Assert.Equal("MiPersonaje", PlrFile.Read(File.ReadAllBytes(copia)).Name); // mismo nombre interno, solo cambia el fichero

        Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public void RestoreBackup_RestauraDesdeElBakReal()
    {
        var home = new HomeViewModel();
        string dir = Path.Combine(Path.GetTempPath(), $"home-restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "X.plr");
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Nuevo")));
        File.WriteAllBytes(path + ".bak", PlrFile.Write(NuevoPersonaje("Viejo")));
        var entry = new CharacterListEntryViewModel(path, PlrFile.Read(File.ReadAllBytes(path)), false, DateTime.UtcNow);

        home.RestoreBackupCommand.Execute(entry);

        Assert.Equal("Viejo", PlrFile.Read(File.ReadAllBytes(path)).Name);
        Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public void RestoreBackup_SinBakReal_DejaUnMensajeSinTocarNada()
    {
        var home = new HomeViewModel();
        string dir = Path.Combine(Path.GetTempPath(), $"home-nobak-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "Y.plr");
        var character = NuevoPersonaje("Solo");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var entry = new CharacterListEntryViewModel(path, character, false, DateTime.UtcNow);

        home.RestoreBackupCommand.Execute(entry);

        Assert.Contains("no tiene ninguna copia", home.ScanMessage);
        Assert.Equal("Solo", PlrFile.Read(File.ReadAllBytes(path)).Name); // intacto

        Directory.Delete(dir, recursive: true);
    }
}
