using System.IO;

namespace TerrasavrNative.App.Services;

// H5-04 (quinta auditoria de Opus): "la copia de seguridad es de un solo nivel - el segundo
// Guardar destruye la unica red". CharacterFileService.WriteAtomic (T-C) ya deja un .bak junto
// al fichero real (correcto, atomico, no se toca) pero SE SOBRESCRIBE en cada guardado - el
// unico punto de retorno real es el guardado inmediatamente anterior. Escenario real que mas
// duele en un editor de partidas: guardas, sigues tocando, guardas otra vez, y el error estaba
// DOS guardados atras - en ese momento no queda nada.
//
// Copias rotativas con fecha, aparte del .bak (que se queda tal cual, es lo que hace atomico el
// propio guardado). No ensucian Documents\My Games\Terraria (mismo criterio ya establecido en
// T-C para el .tplr) - viven en %LOCALAPPDATA%\Terrakeep\Backups\{personaje}\, la MISMA carpeta
// base ya real de WindowPlacementService.
public sealed record BackupEntry(string PlrPath, string? TplrPath, DateTime TimestampLocal, long SizeBytes);

public sealed class BackupHistoryService
{
    // H5-07 (quinta auditoria de Opus): "N configurable de verdad (pantalla de Ajustes)" - ya
    // no es un techo fijo, MainViewModel lo fija desde SettingsService.Load() al arrancar y de
    // nuevo cada vez que el usuario lo cambia en Ajustes. 20 se queda como valor de fabrica real
    // (el mismo que H5-04 ya midio como "razonable" antes de que esto fuera configurable).
    public int MaxBackupsPerCharacter { get; set; } = 20;

    private static readonly string BackupsRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "Backups");

    private const string TimestampFormat = "yyyyMMdd-HHmmss";

    public void SaveBackup(LoadedCharacter loaded)
    {
        string charDir = DirectoryFor(loaded.PlrPath);
        Directory.CreateDirectory(charDir);
        string stamp = DateTime.Now.ToString(TimestampFormat);
        File.Copy(loaded.PlrPath, Path.Combine(charDir, stamp + ".plr"), overwrite: true);
        if (loaded.TplrPath != null && File.Exists(loaded.TplrPath))
            File.Copy(loaded.TplrPath, Path.Combine(charDir, stamp + ".tplr"), overwrite: true);
        Purge(charDir);
    }

    public IReadOnlyList<BackupEntry> ListBackups(string plrPath)
    {
        string charDir = DirectoryFor(plrPath);
        if (!Directory.Exists(charDir)) return [];

        var result = new List<BackupEntry>();
        foreach (string file in Directory.GetFiles(charDir, "*.plr"))
        {
            string stamp = Path.GetFileNameWithoutExtension(file);
            if (!DateTime.TryParseExact(stamp, TimestampFormat, null, System.Globalization.DateTimeStyles.None, out var ts)) continue;
            string tplr = Path.ChangeExtension(file, ".tplr");
            result.Add(new BackupEntry(file, File.Exists(tplr) ? tplr : null, ts, new FileInfo(file).Length));
        }
        return result.OrderByDescending(b => b.TimestampLocal).ToList();
    }

    // Mismo criterio real ya establecido en HomeViewModel.RestoreBackup/H3-02: sin .tplr en ESE
    // punto concreto del historial, no resucitar uno que naciera despues.
    public void Restore(string plrPath, string? tplrPathReal, BackupEntry entry)
    {
        File.Copy(entry.PlrPath, plrPath, overwrite: true);
        string tplrTarget = tplrPathReal ?? Path.ChangeExtension(plrPath, ".tplr");
        if (entry.TplrPath != null) File.Copy(entry.TplrPath, tplrTarget, overwrite: true);
        else if (File.Exists(tplrTarget)) File.Delete(tplrTarget);
    }

    private void Purge(string charDir)
    {
        var files = Directory.GetFiles(charDir, "*.plr").OrderByDescending(File.GetLastWriteTimeUtc).ToList();
        foreach (string old in files.Skip(MaxBackupsPerCharacter))
        {
            File.Delete(old);
            string tplr = Path.ChangeExtension(old, ".tplr");
            if (File.Exists(tplr)) File.Delete(tplr);
        }
    }

    private static string DirectoryFor(string plrPath) =>
        Path.Combine(BackupsRoot, SanitizeFileName(Path.GetFileNameWithoutExtension(plrPath)));

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }
}
