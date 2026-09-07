using System.IO;

namespace Terrakeep.App.Services;

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

    // Auditoria final de Opus (5-sep-2026, antes de publicar) - BUG REAL DE PERDIDA DE DATOS:
    // esto identificaba al personaje SOLO por el nombre del fichero, asi que dos personajes
    // DISTINTOS con el mismo nombre compartian carpeta de historial y se mezclaban. No es un
    // caso rebuscado: Terrakeep escanea a proposito las dos carpetas reales de Terraria (vanilla
    // y tModLoader, ver CharacterFileService.GetAllPlayersDirectories) y tener el mismo nombre
    // en las dos es lo normal - en esta misma maquina, "Eldelgas.plr" existe en las dos con
    // tamaños distintos (3952 vs 3744 bytes): son dos personajes diferentes. Consecuencia real:
    // el "Historial de guardados" de uno listaba tambien las copias del otro, y restaurar una de
    // ellas sobrescribia el personaje con OTRO personaje distinto, sin aviso ni vuelta atras.
    // WorldViewStateService ya usaba el criterio correcto para lo mismo ("Clave: la ruta ABSOLUTA
    // del .wld... dos mundos distintos con el mismo Title no colisionan") - esto era la excepcion
    // incoherente. Ahora la carpeta lleva ademas una huella corta de la ruta completa, asi que
    // dos rutas distintas nunca comparten historial.
    private static string DirectoryFor(string plrPath)
    {
        string nombre = SanitizeFileName(Path.GetFileNameWithoutExtension(plrPath));
        string destino = Path.Combine(BackupsRoot, nombre + "-" + HuellaDeRuta(plrPath));
        MigrarHistorialAntiguo(Path.Combine(BackupsRoot, nombre), destino);
        return destino;
    }

    // 8 hex de SHA-256 sobre la ruta completa normalizada (Windows real no distingue
    // mayusculas/minusculas en rutas - misma comparacion que ya hace AppendExtraFolders).
    private static string HuellaDeRuta(string plrPath)
    {
        string normalizada;
        try { normalizada = Path.GetFullPath(plrPath).ToLowerInvariant(); }
        catch (Exception) { normalizada = plrPath.ToLowerInvariant(); }
        byte[] hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(normalizada));
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }

    // Un usuario que ya venia usando Terrakeep tiene su historial real en la carpeta con el
    // formato viejo (solo el nombre) - se mueve entera al nombre nuevo la primera vez, para no
    // hacerle desaparecer copias que si son suyas. Solo si el destino no existe todavia: en el
    // caso de colision (dos personajes homonimos) el primero que pase por aqui se queda el
    // historial antiguo (que es exactamente lo que ya veia) y el segundo arranca limpio - nunca
    // se borra nada. Best-effort: si el movimiento falla (carpeta en uso, permisos), se sigue
    // con el destino nuevo y vacio, que es correcto, solo se pierde comodidad.
    private static void MigrarHistorialAntiguo(string carpetaAntigua, string carpetaNueva)
    {
        try
        {
            if (carpetaAntigua == carpetaNueva) return;
            if (!Directory.Exists(carpetaAntigua) || Directory.Exists(carpetaNueva)) return;
            Directory.Move(carpetaAntigua, carpetaNueva);
        }
        catch (Exception)
        {
            // sin historial migrado, pero nunca un fallo visible por esto
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }
}
