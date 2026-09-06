using System.IO;
using TerrasavrNative.App.Services;
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
    private static readonly CharacterFileService Service = new();
    private static readonly EquipmentAppearanceResolver EquipAppearance = Service.EquipmentAppearance;

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
        var home = new HomeViewModel(EquipAppearance, Service.BackupHistory);
        var a = new CharacterListEntryViewModel(@"C:\a.plr", NuevoPersonaje("A"), false, null, DateTime.UtcNow, EquipAppearance);
        var b = new CharacterListEntryViewModel(@"C:\b.plr", NuevoPersonaje("B"), false, null, DateTime.UtcNow, EquipAppearance);
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
        var entry = new CharacterListEntryViewModel(path, PlrFile.Read(File.ReadAllBytes(path)), false, null, DateTime.UtcNow, EquipAppearance);
        vm.Home.Characters.Add(entry);

        vm.LoadFromPath(path);

        Assert.True(entry.IsCurrent);
        File.Delete(path);
    }

    [Fact]
    public void Duplicate_CreaUnaCopiaRealEnLaMismaCarpeta_SinTocarElOriginal()
    {
        var home = new HomeViewModel(EquipAppearance, Service.BackupHistory);
        string dir = Path.Combine(Path.GetTempPath(), $"home-dup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "MiPersonaje.plr");
        var character = NuevoPersonaje("MiPersonaje");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var entry = new CharacterListEntryViewModel(path, character, isTModLoader: false, tplr: null, DateTime.UtcNow, EquipAppearance);

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
        var home = new HomeViewModel(EquipAppearance, Service.BackupHistory);
        string dir = Path.Combine(Path.GetTempPath(), $"home-restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "X.plr");
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Nuevo")));
        File.WriteAllBytes(path + ".bak", PlrFile.Write(NuevoPersonaje("Viejo")));
        var entry = new CharacterListEntryViewModel(path, PlrFile.Read(File.ReadAllBytes(path)), false, null, DateTime.UtcNow, EquipAppearance);

        home.RestoreBackupCommand.Execute(entry);

        Assert.Equal("Viejo", PlrFile.Read(File.ReadAllBytes(path)).Name);
        Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public void RestoreBackup_SinBakReal_DejaUnMensajeSinTocarNada()
    {
        var home = new HomeViewModel(EquipAppearance, Service.BackupHistory);
        string dir = Path.Combine(Path.GetTempPath(), $"home-nobak-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "Y.plr");
        var character = NuevoPersonaje("Solo");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var entry = new CharacterListEntryViewModel(path, character, false, null, DateTime.UtcNow, EquipAppearance);

        home.RestoreBackupCommand.Execute(entry);

        // INI-07 (oleada del 6-sep-2026): el resultado fallido de una ACCION dejo de compartir
        // campo con el mensaje del escaneo - ScanMessage es "no encontre ningun personaje" (y el
        // XAML lo pinta como el reclamo "Empezar: cargar un personaje"), ActionErrorMessage es
        // "lo que acabas de pedir ha fallado". Esto es lo segundo.
        Assert.Contains("no tiene ninguna copia", home.ActionErrorMessage);
        Assert.Null(home.ScanMessage);
        Assert.Equal("Solo", PlrFile.Read(File.ReadAllBytes(path)).Name); // intacto

        Directory.Delete(dir, recursive: true);
    }

    // INI-08 (oleada del 6-sep-2026) - BUG REAL DE PERDIDA DE DATOS ya arreglado: "Restaurar
    // copia de seguridad" copiaba el .bak encima del .plr sin comprobar que el .bak se pudiera
    // leer siquiera. Un .bak truncado (guardado interrumpido, disco lleno, antivirus) destruia el
    // personaje BUENO, que ademas desaparecia de Inicio sin ningun aviso (el escaneo omite en
    // silencio lo que no puede leer, por diseño). Medido antes de arreglarlo: .plr de 3680 bytes
    // -> 3 bytes, sin vuelta atras.
    [Fact]
    public void RestoreBackup_ConBakIlegible_NoTocaElPlrBueno_YAvisa()
    {
        var home = new HomeViewModel(EquipAppearance, Service.BackupHistory);
        string dir = Path.Combine(Path.GetTempPath(), $"home-bakroto-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "Z.plr");
        var character = NuevoPersonaje("Intacto");
        File.WriteAllBytes(path, PlrFile.Write(character));
        long tamañoBueno = new FileInfo(path).Length;
        File.WriteAllBytes(path + ".bak", [0x01, 0x02, 0x03]); // ilegible a proposito
        var entry = new CharacterListEntryViewModel(path, character, false, null, DateTime.UtcNow, EquipAppearance);

        home.RestoreBackupCommand.Execute(entry);

        Assert.Equal("Intacto", PlrFile.Read(File.ReadAllBytes(path)).Name);
        Assert.Equal(tamañoBueno, new FileInfo(path).Length);
        Assert.NotNull(home.ActionErrorMessage);
        Assert.Contains("Intacto", home.ActionErrorMessage);

        Directory.Delete(dir, recursive: true);
    }
}
