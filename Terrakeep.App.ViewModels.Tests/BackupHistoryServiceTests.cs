using System.IO;
using System.Threading;
using Terrakeep.App.Services;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H5-04 (quinta auditoria de Opus): "la copia de seguridad es de un solo nivel - el segundo
// Guardar destruye la unica red". Copias rotativas reales con fecha, en su propia carpeta de
// %LOCALAPPDATA%\Terrakeep\Backups - nunca en la carpeta real de Documentos del personaje
// (mismo criterio ya establecido para el .tplr, T-C).
public sealed class BackupHistoryServiceTests
{
    private static PlrCharacter NuevoPersonaje(string nombre) => new()
    {
        Name = nombre,
        Version = 279,
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    private static string NuevaRutaTemporal() => Path.Combine(Path.GetTempPath(), $"backup-hist-{Guid.NewGuid():N}.plr");

    [Fact]
    public void SaveBackup_DejaUnaCopiaRealRecuperablePorListBackups()
    {
        var service = new BackupHistoryService();
        string path = NuevaRutaTemporal();
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Test")));
        var loaded = new LoadedCharacter(path, null, "Player", NuevoPersonaje("Test"), null, new Dictionary<string, Terrakeep.Core.Model.GameItem[]>());

        service.SaveBackup(loaded);
        var backups = service.ListBackups(path);

        Assert.Single(backups);
        Assert.True(File.Exists(backups[0].PlrPath));
        Assert.True(backups[0].SizeBytes > 0);

        File.Delete(path);
    }

    [Fact]
    public void ListBackups_SinNingunaCopiaTodavia_DevuelveVacio()
    {
        var service = new BackupHistoryService();
        string path = Path.Combine(Path.GetTempPath(), $"backup-hist-nunca-{Guid.NewGuid():N}.plr");
        Assert.Empty(service.ListBackups(path));
    }

    [Fact]
    public void Restore_CopiaLaVersionElegidaEncimaDelFicheroReal()
    {
        var service = new BackupHistoryService();
        string path = NuevaRutaTemporal();
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Original")));
        var loadedOriginal = new LoadedCharacter(path, null, "Player", NuevoPersonaje("Original"), null, new Dictionary<string, Terrakeep.Core.Model.GameItem[]>());
        service.SaveBackup(loadedOriginal);
        var puntoOriginal = service.ListBackups(path)[0];

        // Sobrescribe el fichero real con OTRO contenido (simula ediciones posteriores).
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Editado")));

        service.Restore(path, null, puntoOriginal);

        var restaurado = PlrFile.Read(File.ReadAllBytes(path));
        Assert.Equal("Original", restaurado.Name);

        File.Delete(path);
    }

    [Fact]
    public void ListBackups_OrdenaDeMasRecienteAMasAntigua()
    {
        var service = new BackupHistoryService();
        string path = NuevaRutaTemporal();
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Test")));
        var loaded = new LoadedCharacter(path, null, "Player", NuevoPersonaje("Test"), null, new Dictionary<string, Terrakeep.Core.Model.GameItem[]>());

        service.SaveBackup(loaded);
        Thread.Sleep(1100); // el sello real es a nivel de segundo (yyyyMMdd-HHmmss) - hace falta que cambie de verdad
        service.SaveBackup(loaded);

        var backups = service.ListBackups(path);
        Assert.Equal(2, backups.Count);
        Assert.True(backups[0].TimestampLocal >= backups[1].TimestampLocal);

        File.Delete(path);
    }

    // H5-07 (quinta auditoria de Opus): "N configurable de verdad" - MaxBackupsPerCharacter
    // paso de const fijo a propiedad real; confirma que Purge() SI respeta un valor bajo real
    // (no solo que compila con el tipo cambiado).
    [Fact]
    public void MaxBackupsPerCharacter_ConfigurableAUnValorBajo_PurgaLasMasAntiguasDeVerdad()
    {
        var service = new BackupHistoryService { MaxBackupsPerCharacter = 2 };
        string path = NuevaRutaTemporal();
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Test")));
        var loaded = new LoadedCharacter(path, null, "Player", NuevoPersonaje("Test"), null, new Dictionary<string, Terrakeep.Core.Model.GameItem[]>());

        service.SaveBackup(loaded);
        Thread.Sleep(1100);
        service.SaveBackup(loaded);
        Thread.Sleep(1100);
        service.SaveBackup(loaded); // 3ª copia real, con el cupo en 2 - la mas antigua debe desaparecer

        var backups = service.ListBackups(path);
        Assert.Equal(2, backups.Count);

        File.Delete(path);
    }
}
