using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Terrakeep.Core.PlrFormat;

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
// T-C para el .tplr) - viven en %LOCALAPPDATA%\Terrakeep\Backups\{personaje}-{huella}\, la MISMA
// carpeta base ya real de WindowPlacementService.
//
// ===========================================================================================
// BK (13-sep-2026, encargo "historial de copias de seguridad con versiones reales"): revision a
// fondo de esta pieza. Lo que ya habia era correcto pero se quedaba corto en cuatro puntos
// REALES, cada uno comprobado contra el codigo/los ficheros de esta maquina antes de tocar nada:
//
//   BK-1. La copia se hacia DESPUES de escribir (MainViewModel.Save llamaba a SaveBackup justo
//         detras de _service.Save). O sea que el historial guardaba "estados ya guardados", pero
//         el estado ANTERIOR a la primera escritura de la sesion no quedaba en ningun sitio
//         salvo el .bak de un solo nivel: dos guardados seguidos y el fichero con el que
//         arrancaste esa tarde ya no existe en ninguna parte. Ahora el snapshot se toma ANTES de
//         escribir (BackupReason.BeforeSave). Es estrictamente mejor, no una preferencia: el
//         estado previo al guardado N-esimo ES el resultado del guardado N-1, asi que
//         fotografiar "antes" conserva TODO lo que conservaba "despues" y ademas el original.
//         Lo unico que "antes" no tiene es el ultimo guardado... que es exactamente el fichero
//         real que hay en disco ahora mismo, no hace falta una copia de el.
//
//   BK-2. El sello era de SEGUNDO (yyyyMMdd-HHmmss) con File.Copy(overwrite:true): dos guardados
//         dentro del mismo segundo se pisaban el uno al otro en silencio, y el historial mostraba
//         una sola version donde hubo dos. Ahora el sello lleva milisegundos y ademas se
//         desambigua con un sufijo si hiciera falta - nunca se pierde un punto por velocidad de
//         tecleo.
//
//   BK-3. Purge() ordenaba por File.GetLastWriteTimeUtc, y File.Copy CONSERVA la fecha de
//         modificacion del ORIGEN - o sea que la "fecha" por la que se ordenaba no era la de la
//         copia sino la del .plr copiado. Basta con restaurar una version antigua (lo que deja
//         el .plr real con fecha antigua) para que la siguiente copia naciera "vieja" y fuera la
//         primera candidata a desaparecer, aunque fuera la mas reciente de todas. Ahora se ordena
//         por el sello REAL del propio nombre del fichero, que es un dato nuestro y no depende de
//         ninguna fecha heredada.
//
//   BK-4. No habia ningun limite GLOBAL: el cupo era por personaje, pero cada ruta distinta
//         estrena su propia carpeta y nadie las retira nunca. Medido en esta maquina antes de
//         tocar nada: 2.765 carpetas, 3.690 ficheros, 28 MB - casi todo de rutas temporales de
//         los propios arneses de prueba que ya no existen. Ver FindOrphanHistories/
//         PurgeOrphanHistories mas abajo.
//
// Formato del snapshot (decision tomada CON NUMEROS REALES, no por costumbre): un unico fichero
// contenedor ".tkbak" por version, que por dentro es un ZIP normal y corriente (System.IO.
// Compression, sin ninguna dependencia nueva) con "player.plr", "player.tplr" si lo hay, y
// "meta.json". Un fichero por version en vez de dos o tres sueltos = imposible que exista media
// version, y la carpeta se lee de un vistazo en el Explorador.
//
// Y se guarda SIN COMPRIMIR a proposito (CompressionLevel.NoCompression para los dos binarios,
// Optimal solo para el meta.json). Medido con gzip -9 sobre los ficheros REALES de esta maquina:
//     adrian.plr      102.912 -> 102.961 bytes  (+0,05%)
//     adrian.tplr      23.550 ->  23.585 bytes  (+0,15%)
//     Eldelgas.plr      3.744 ->   3.780 bytes  (+1,0%)
//     Eldelgas.tplr     1.875 ->   1.912 bytes  (+2,0%)
// Comprimir los AGRANDA, en los cuatro casos. No es casualidad ni una mala racha: el .plr esta
// cifrado entero con AES-128-CBC (ver PlrCrypto - no hay ni una cabecera en claro) y el .tplr ya
// es un NBT gzipeado por el propio tModLoader. Datos cifrados o ya comprimidos no tienen
// redundancia que exprimir, asi que lo unico que añade otra pasada de deflate es su propia
// cabecera. El coste real en disco sale de la cuenta de arriba: el personaje mas gordo de esta
// maquina ocupa 126 KB por version, o sea ~2,5 MB por los 20 puntos de fabrica - barato de
// sobra para no complicar nada con diffs binarios.
public enum BackupReason
{
    // El estado que habia en disco justo ANTES de un guardado real (el caso normal, BK-1).
    BeforeSave,
    // El usuario pidio la copia a mano ("Crear copia ahora" en el panel de historial).
    Manual,
    // Red de seguridad de la propia restauracion: antes de sobrescribir el fichero real con una
    // version antigua se fotografia lo que habia, para que restaurar tambien se pueda deshacer.
    BeforeRestore,
    // Copias del formato ANTIGUO (ficheros .plr/.tplr sueltos con sello de segundo, sin meta) que
    // este usuario ya tuviera en disco. No se sabe -ni se inventa- por que se crearon.
    Unknown,
}

// Lo que se escribe tal cual en el meta.json de dentro del contenedor. Nombres cortos y estables:
// esto es formato en disco, no una clase de paso.
public sealed class BackupSnapshotInfo
{
    public int Schema { get; set; } = 1;
    public string Reason { get; set; } = nameof(BackupReason.Unknown);
    public string CreatedLocal { get; set; } = "";   // ISO round-trip ("o"), hora local real
    public string SourcePlrPath { get; set; } = "";  // ruta completa del .plr del que salio
    public string AppVersion { get; set; } = "";

    // Resumen legible, sacado del propio .plr fotografiado (nunca del estado en memoria, que en
    // el momento de un BeforeSave ya lleva las ediciones sin guardar) - si el fichero no se
    // pudiera leer, estos campos se quedan a cero y el snapshot se guarda igual: una copia sin
    // resumen sigue siendo una copia buena, y negarse a copiar por no poder describirla seria
    // exactamente el fallo que este sistema existe para evitar.
    public bool SummaryAvailable { get; set; }
    public string CharacterName { get; set; } = "";
    public int SaveVersion { get; set; }
    public int Difficulty { get; set; }
    public int HealthNow { get; set; }
    public int HealthMax { get; set; }
    public int ManaNow { get; set; }
    public int ManaMax { get; set; }
    public long PlayTimeTicks { get; set; }
    public int PveDeaths { get; set; }
    public int ItemCount { get; set; }        // slots con objeto real en inventario+monedas+municion
    public bool HasTplr { get; set; }

    public long PlrBytes { get; set; }
    public long TplrBytes { get; set; }
}

// Una version del historial, ya localizada en disco. `ContainerPath` es el .tkbak del formato
// nuevo o, para las copias del formato antiguo, el .plr suelto (ver IsLegacy).
public sealed record BackupEntry(
    string ContainerPath,
    DateTime TimestampLocal,
    long SizeBytes,
    BackupReason Reason,
    BackupSnapshotInfo? Info,
    bool IsLegacy,
    string? LegacyTplrPath);

// Una carpeta de historial cuyo .plr de origen ya no aparece en ninguna de las carpetas reales
// que la app escanea - ver FindOrphanHistories.
public sealed record OrphanHistory(string Directory, string DisplayName, int Snapshots, long SizeBytes, DateTime NewestLocal);

public sealed class BackupHistoryService
{
    // H5-07 (quinta auditoria de Opus): "N configurable de verdad (pantalla de Ajustes)" - ya
    // no es un techo fijo, MainViewModel lo fija desde SettingsService.Load() al arrancar y de
    // nuevo cada vez que el usuario lo cambia en Ajustes. 20 se queda como valor de fabrica real.
    //
    // BK: por que el limite es POR NUMERO y no por antiguedad. Un editor de partidas no se usa
    // todos los dias - un personaje que no se toca desde hace tres meses sigue mereciendo sus
    // puntos de retorno intactos, asi que "borrar lo que tenga mas de X dias" castigaria justo al
    // que menos culpa tiene. Un tope por numero, en cambio, da una cota de disco predecible y
    // facil de explicar: 20 versiones x 126 KB (el personaje mas gordo medido en esta maquina) =
    // ~2,5 MB por personaje, y siempre los 20 ULTIMOS puntos de trabajo, tengan la edad que
    // tengan.
    public int MaxBackupsPerCharacter { get; set; } = 20;

    // Raiz real del historial. Propiedad de instancia (antes era un static readonly) por un
    // motivo real medido: las pruebas y los arneses trabajan sobre .plr temporales, y cada ruta
    // temporal distinta estrenaba una carpeta en el %LOCALAPPDATA% REAL de esta maquina que no
    // retiraba nadie - de ahi buena parte de las 2.765 carpetas de BK-4. Ahora una prueba puede
    // apuntar a su propio directorio y no tocar nada del usuario.
    public string BackupsRoot { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "Backups");

    // Sello con MILISEGUNDOS (BK-2) - el formato antiguo, de segundo, se sigue leyendo tal cual
    // (ver TimestampFormatLegacy) pero ya no se escribe.
    private const string TimestampFormat = "yyyyMMdd-HHmmss-fff";
    private const string TimestampFormatLegacy = "yyyyMMdd-HHmmss";
    public const string SnapshotExtension = ".tkbak";

    private const string EntryPlr = "player.plr";
    private const string EntryTplr = "player.tplr";
    private const string EntryMeta = "meta.json";

    private static readonly JsonSerializerOptions MetaJson = new() { WriteIndented = true };

    // ---------------------------------------------------------------------------------------
    // Crear una version
    // ---------------------------------------------------------------------------------------

    // Fotografia el .plr (y su .tplr hermano si lo hay) TAL Y COMO ESTAN EN DISCO ahora mismo.
    // Devuelve la version creada, o null si no habia nada que fotografiar (un personaje nuevo que
    // todavia no se ha guardado nunca no tiene estado anterior que perder - no es un error).
    public BackupEntry? SaveBackup(string plrPath, string? tplrPath, BackupReason reason)
    {
        if (!File.Exists(plrPath)) return null;

        byte[] plrBytes = File.ReadAllBytes(plrPath);
        string? tplrReal = tplrPath ?? Path.ChangeExtension(plrPath, ".tplr");
        byte[]? tplrBytes = File.Exists(tplrReal) ? File.ReadAllBytes(tplrReal) : null;

        string charDir = DirectoryFor(plrPath);
        Directory.CreateDirectory(charDir);

        var now = DateTime.Now;
        var info = DescribeSnapshot(plrPath, plrBytes, tplrBytes, reason, now);
        string destino = RutaLibre(charDir, now, reason);

        // Mismo criterio real que CharacterFileService.WriteAtomic: se escribe a un temporal y
        // solo al final aparece con su nombre definitivo, de un paso. Un corte a mitad deja un
        // .tmp suelto que ListBackups ignora por extension, nunca media version que parezca buena.
        string tmp = destino + ".tmp";
        try
        {
            using (var fs = File.Create(tmp))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                Escribir(zip, EntryPlr, plrBytes, CompressionLevel.NoCompression);
                if (tplrBytes != null) Escribir(zip, EntryTplr, tplrBytes, CompressionLevel.NoCompression);
                Escribir(zip, EntryMeta, JsonSerializer.SerializeToUtf8Bytes(info, MetaJson), CompressionLevel.Optimal);
            }
            File.Move(tmp, destino);
        }
        catch (Exception)
        {
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch (Exception) { /* best-effort */ }
            throw;
        }

        Purge(charDir);
        return new BackupEntry(destino, now, plrBytes.LongLength + (tplrBytes?.LongLength ?? 0), reason, info, IsLegacy: false, LegacyTplrPath: null);
    }

    public BackupEntry? SaveBackup(LoadedCharacter loaded, BackupReason reason) =>
        SaveBackup(loaded.PlrPath, loaded.TplrPath, reason);

    private static void Escribir(ZipArchive zip, string nombre, byte[] datos, CompressionLevel nivel)
    {
        var entry = zip.CreateEntry(nombre, nivel);
        using var s = entry.Open();
        s.Write(datos, 0, datos.Length);
    }

    // Sufijo de una letra con el motivo: hace que la carpeta se lea sola en el Explorador y, sobre
    // todo, que Purge pueda distinguir una copia manual de una automatica SIN abrir los 20 zips.
    private static char LetraDe(BackupReason reason) => reason switch
    {
        BackupReason.Manual => 'M',
        BackupReason.BeforeRestore => 'R',
        BackupReason.BeforeSave => 'A',
        _ => 'X',
    };

    private static BackupReason MotivoDe(char letra) => letra switch
    {
        'M' => BackupReason.Manual,
        'R' => BackupReason.BeforeRestore,
        'A' => BackupReason.BeforeSave,
        _ => BackupReason.Unknown,
    };

    // BK-2: con milisegundos ya es practicamente imposible chocar, pero "practicamente" no es
    // "nunca" (dos guardados en el mismo milisegundo son posibles en un arnes automatico, y ahi
    // es justo donde mas duele perder un punto sin enterarse). Si el nombre existe, se desplaza
    // al siguiente milisegundo libre en vez de sobrescribir.
    private static string RutaLibre(string charDir, DateTime now, BackupReason reason)
    {
        for (int i = 0; i < 1000; i++)
        {
            string ruta = Path.Combine(charDir, $"{now.AddMilliseconds(i):yyyyMMdd-HHmmss-fff}-{LetraDe(reason)}{SnapshotExtension}");
            if (!File.Exists(ruta)) return ruta;
        }
        return Path.Combine(charDir, $"{now:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}-{LetraDe(reason)}{SnapshotExtension}");
    }

    private static BackupSnapshotInfo DescribeSnapshot(string plrPath, byte[] plrBytes, byte[]? tplrBytes, BackupReason reason, DateTime now)
    {
        var info = new BackupSnapshotInfo
        {
            Reason = reason.ToString(),
            CreatedLocal = now.ToString("o"),
            SourcePlrPath = plrPath,
            AppVersion = typeof(BackupHistoryService).Assembly.GetName().Version?.ToString() ?? "",
            PlrBytes = plrBytes.LongLength,
            TplrBytes = tplrBytes?.LongLength ?? 0,
            HasTplr = tplrBytes != null,
        };
        try
        {
            var c = PlrFile.Read(plrBytes);
            info.SummaryAvailable = true;
            info.CharacterName = c.Name;
            info.SaveVersion = c.Version;
            info.Difficulty = c.Difficulty;
            info.HealthNow = c.HealthNow;
            info.HealthMax = c.HealthMax;
            info.ManaNow = c.ManaNow;
            info.ManaMax = c.ManaMax;
            info.PlayTimeTicks = (long)(((ulong)c.PlayTimeHigh << 32) | c.PlayTimeLow);
            info.PveDeaths = c.PveDeaths;
            info.ItemCount =
                c.Inventory.Count(s => s.Id != 0) + c.Coins.Count(s => s.Id != 0) + c.Ammo.Count(s => s.Id != 0);
        }
        catch (Exception)
        {
            // Ver el comentario de SummaryAvailable: la copia vale igual, solo se queda sin
            // resumen. Nunca se inventa un dato que el fichero no haya dado de verdad.
        }
        return info;
    }

    // ---------------------------------------------------------------------------------------
    // Listar / leer / restaurar
    // ---------------------------------------------------------------------------------------

    public IReadOnlyList<BackupEntry> ListBackups(string plrPath)
    {
        string charDir = DirectoryFor(plrPath);
        if (!Directory.Exists(charDir)) return [];

        var result = new List<BackupEntry>();

        foreach (string file in Directory.GetFiles(charDir, "*" + SnapshotExtension))
        {
            string nombre = Path.GetFileNameWithoutExtension(file);
            if (!TryParseNombre(nombre, out var ts, out var reason)) continue;
            var info = TryReadInfo(file);
            long size = info != null ? info.PlrBytes + info.TplrBytes : SafeLength(file);
            result.Add(new BackupEntry(file, ts, size, info != null ? ParseReason(info.Reason, reason) : reason, info, IsLegacy: false, LegacyTplrPath: null));
        }

        // Formato ANTIGUO (ficheros sueltos con sello de segundo). Se sigue leyendo -y
        // restaurando- tal cual: quien ya venia usando Terrakeep tiene ahi puntos de retorno
        // suyos de verdad, y hacerlos desaparecer por cambiar de formato seria exactamente el
        // tipo de perdida de datos que esta clase existe para evitar. No se convierten al vuelo
        // (reescribir ficheros del usuario sin que lo pida) - simplemente conviven hasta que el
        // cupo los va retirando por antiguedad, como cualquier otro punto.
        foreach (string file in Directory.GetFiles(charDir, "*.plr"))
        {
            string stamp = Path.GetFileNameWithoutExtension(file);
            if (!DateTime.TryParseExact(stamp, TimestampFormatLegacy, null, System.Globalization.DateTimeStyles.None, out var ts)) continue;
            string tplr = Path.ChangeExtension(file, ".tplr");
            bool tieneTplr = File.Exists(tplr);
            result.Add(new BackupEntry(file, ts, SafeLength(file) + (tieneTplr ? SafeLength(tplr) : 0),
                BackupReason.Unknown, Info: null, IsLegacy: true, LegacyTplrPath: tieneTplr ? tplr : null));
        }

        return result.OrderByDescending(b => b.TimestampLocal).ToList();
    }

    private static bool TryParseNombre(string nombreSinExtension, out DateTime ts, out BackupReason reason)
    {
        ts = default;
        reason = BackupReason.Unknown;
        // "yyyyMMdd-HHmmss-fff-L" -> los 18 primeros caracteres son el sello, el ultimo la letra.
        if (nombreSinExtension.Length < TimestampFormat.Length) return false;
        string sello = nombreSinExtension[..TimestampFormat.Length];
        if (!DateTime.TryParseExact(sello, TimestampFormat, null, System.Globalization.DateTimeStyles.None, out ts)) return false;
        reason = MotivoDe(nombreSinExtension[^1]);
        return true;
    }

    private static BackupReason ParseReason(string texto, BackupReason porDefecto) =>
        Enum.TryParse<BackupReason>(texto, out var r) ? r : porDefecto;

    private static long SafeLength(string path)
    {
        try { return new FileInfo(path).Length; } catch (Exception) { return 0; }
    }

    private static BackupSnapshotInfo? TryReadInfo(string container)
    {
        try
        {
            using var zip = ZipFile.OpenRead(container);
            var entry = zip.GetEntry(EntryMeta);
            if (entry == null) return null;
            using var s = entry.Open();
            return JsonSerializer.Deserialize<BackupSnapshotInfo>(s);
        }
        catch (Exception)
        {
            return null; // un contenedor ilegible se lista igual, con su fecha del nombre - nunca revienta la lista entera
        }
    }

    // Los bytes reales del .plr guardado en esta version. Lo usa la UI para COMPROBAR que el
    // punto se puede leer de verdad ANTES de tocar el fichero bueno (INI-08: un .bak truncado
    // restaurado a ciegas destruye el personaje sano, ya paso una vez en este proyecto).
    public byte[] ReadPlrBytes(BackupEntry entry)
    {
        if (entry.IsLegacy) return File.ReadAllBytes(entry.ContainerPath);
        using var zip = ZipFile.OpenRead(entry.ContainerPath);
        var e = zip.GetEntry(EntryPlr) ?? throw new InvalidDataException($"El contenedor '{Path.GetFileName(entry.ContainerPath)}' no lleva {EntryPlr}");
        using var s = e.Open();
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    // Mismo criterio real ya establecido en HomeViewModel.RestoreBackup/H3-02: sin .tplr en ESE
    // punto concreto del historial, no resucitar uno que naciera despues.
    //
    // BK: la restauracion se escribe con el mismo patron atomico del resto del proyecto (a un
    // .tmp y luego File.Replace/Move) - a mitad de una restauracion es justo cuando peor viene
    // quedarse sin fichero.
    public void Restore(string plrPath, string? tplrPathReal, BackupEntry entry)
    {
        byte[] plr = ReadPlrBytes(entry);
        byte[]? tplr = ReadTplrBytes(entry);

        EscribirAtomico(plrPath, plr);
        string tplrTarget = tplrPathReal ?? Path.ChangeExtension(plrPath, ".tplr");
        if (tplr != null) EscribirAtomico(tplrTarget, tplr);
        else if (File.Exists(tplrTarget)) File.Delete(tplrTarget);
    }

    public byte[]? ReadTplrBytes(BackupEntry entry)
    {
        if (entry.IsLegacy)
            return entry.LegacyTplrPath != null && File.Exists(entry.LegacyTplrPath) ? File.ReadAllBytes(entry.LegacyTplrPath) : null;
        using var zip = ZipFile.OpenRead(entry.ContainerPath);
        var e = zip.GetEntry(EntryTplr);
        if (e == null) return null;
        using var s = e.Open();
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    private static void EscribirAtomico(string path, byte[] bytes)
    {
        string tmp = path + ".tkrestore.tmp";
        File.WriteAllBytes(tmp, bytes);
        if (!File.Exists(path)) { File.Move(tmp, path); return; }
        try { File.Replace(tmp, path, null, ignoreMetadataErrors: true); }
        catch (IOException) { File.Copy(tmp, path, overwrite: true); File.Delete(tmp); }
    }

    // ---------------------------------------------------------------------------------------
    // Cupo por personaje
    // ---------------------------------------------------------------------------------------

    private void Purge(string charDir)
    {
        // BK-3: por el sello REAL del nombre, nunca por la fecha de modificacion del fichero (que
        // File.Copy heredaba del .plr de origen y podia ser mucho mas antigua que la copia).
        var todas = ListarDe(charDir);
        int sobran = todas.Count - Math.Max(1, MaxBackupsPerCharacter);
        if (sobran <= 0) return;

        // A quien le toca irse: primero las AUTOMATICAS mas antiguas; las copias que el usuario
        // pidio a mano solo se retiran si ya no queda ninguna automatica que sacrificar. Una copia
        // manual es un punto que alguien marco a proposito ("aqui estaba bien"), no ruido de fondo.
        var candidatas = todas
            .OrderBy(b => b.Reason == BackupReason.Manual ? 1 : 0)
            .ThenBy(b => b.TimestampLocal)
            .Take(sobran);

        foreach (var vieja in candidatas) Borrar(vieja);
    }

    private List<BackupEntry> ListarDe(string charDir)
    {
        var result = new List<BackupEntry>();
        if (!Directory.Exists(charDir)) return result;
        foreach (string file in Directory.GetFiles(charDir, "*" + SnapshotExtension))
        {
            if (!TryParseNombre(Path.GetFileNameWithoutExtension(file), out var ts, out var reason)) continue;
            result.Add(new BackupEntry(file, ts, SafeLength(file), reason, Info: null, IsLegacy: false, LegacyTplrPath: null));
        }
        foreach (string file in Directory.GetFiles(charDir, "*.plr"))
        {
            string stamp = Path.GetFileNameWithoutExtension(file);
            if (!DateTime.TryParseExact(stamp, TimestampFormatLegacy, null, System.Globalization.DateTimeStyles.None, out var ts)) continue;
            string tplr = Path.ChangeExtension(file, ".tplr");
            result.Add(new BackupEntry(file, ts, SafeLength(file), BackupReason.Unknown, Info: null, IsLegacy: true,
                LegacyTplrPath: File.Exists(tplr) ? tplr : null));
        }
        return result;
    }

    private static void Borrar(BackupEntry entry)
    {
        try
        {
            File.Delete(entry.ContainerPath);
            if (entry.IsLegacy && entry.LegacyTplrPath != null && File.Exists(entry.LegacyTplrPath)) File.Delete(entry.LegacyTplrPath);
        }
        catch (Exception)
        {
            // un punto que no se deja borrar (antivirus, fichero abierto) no debe tumbar el guardado
        }
    }

    public void DeleteBackup(BackupEntry entry) => Borrar(entry);

    public long MeasureHistorySize(string plrPath)
    {
        string charDir = DirectoryFor(plrPath);
        if (!Directory.Exists(charDir)) return 0;
        long total = 0;
        foreach (string f in Directory.GetFiles(charDir)) total += SafeLength(f);
        return total;
    }

    public string HistoryDirectoryFor(string plrPath) => DirectoryFor(plrPath);

    // ---------------------------------------------------------------------------------------
    // BK-4: limite GLOBAL - historiales huerfanos
    // ---------------------------------------------------------------------------------------

    // Una carpeta de historial se identifica por la huella de la ruta completa del .plr (ver
    // HuellaDeRuta), asi que el camino de vuelta (carpeta -> .plr) no existe por si solo. Lo que
    // SI se puede hacer es el camino de ida: calcular la huella de cada .plr realmente visible
    // ahora mismo (las mismas carpetas que escanea Inicio, incluidas las adicionales de Ajustes)
    // y considerar huerfana toda carpeta que no le corresponda a ninguno. Funciona igual para las
    // carpetas del formato antiguo, que no llevan ningun meta.json con la ruta de origen.
    public IReadOnlyList<OrphanHistory> FindOrphanHistories()
    {
        if (!Directory.Exists(BackupsRoot)) return [];

        var vivas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string dir in CharacterFileService.GetAllPlayersDirectories())
        {
            string[] ficheros;
            try { ficheros = Directory.GetFiles(dir, "*.plr"); }
            catch (Exception) { continue; }
            foreach (string plr in ficheros) vivas.Add(Path.GetFileName(DirectoryFor(plr)));
        }

        var result = new List<OrphanHistory>();
        string[] carpetas;
        try { carpetas = Directory.GetDirectories(BackupsRoot); }
        catch (Exception) { return []; }

        foreach (string carpeta in carpetas)
        {
            string nombre = Path.GetFileName(carpeta);
            if (vivas.Contains(nombre)) continue;
            var puntos = ListarDe(carpeta);
            if (puntos.Count == 0 && Directory.GetFiles(carpeta).Length > 0) continue; // contenido ajeno: no es nuestro, no se toca
            long bytes = 0;
            try { foreach (string f in Directory.GetFiles(carpeta)) bytes += SafeLength(f); } catch (Exception) { }
            var masNueva = puntos.Count > 0 ? puntos.Max(p => p.TimestampLocal) : DirectoryTime(carpeta);
            result.Add(new OrphanHistory(carpeta, NombreLegible(nombre), puntos.Count, bytes, masNueva));
        }
        return result.OrderByDescending(o => o.SizeBytes).ToList();
    }

    private static DateTime DirectoryTime(string dir)
    {
        try { return Directory.GetLastWriteTime(dir); } catch (Exception) { return DateTime.MinValue; }
    }

    // "Eldelgas-006a13ba" -> "Eldelgas". La huella es un detalle interno, no algo que enseñar.
    private static string NombreLegible(string nombreDeCarpeta)
    {
        int guion = nombreDeCarpeta.LastIndexOf('-');
        return guion > 0 && nombreDeCarpeta.Length - guion == 9 ? nombreDeCarpeta[..guion] : nombreDeCarpeta;
    }

    public (int Carpetas, long Bytes) DeleteOrphanHistories(IEnumerable<OrphanHistory> huerfanas)
    {
        int n = 0; long bytes = 0;
        foreach (var h in huerfanas)
        {
            try
            {
                Directory.Delete(h.Directory, recursive: true);
                n++; bytes += h.SizeBytes;
            }
            catch (Exception) { /* best-effort: una carpeta bloqueada no debe abortar la limpieza */ }
        }
        return (n, bytes);
    }

    // Limpieza AUTOMATICA, deliberadamente conservadora: solo historiales huerfanos y ademas con
    // mas de `gracia` sin una sola copia nueva. El periodo de gracia no es decoracion - el caso
    // que de verdad da miedo es "borre el .plr sin querer y el historial era lo unico que
    // quedaba", asi que un historial recien quedado huerfano NO se toca por su cuenta jamas. Tres
    // meses es tiempo de sobra para darse cuenta, y mientras tanto el panel de historial ofrece
    // la limpieza a mano (con su confirmacion) para quien quiera el espacio ya.
    public static readonly TimeSpan OrphanGracePeriod = TimeSpan.FromDays(90);

    public (int Carpetas, long Bytes) PurgeOrphanHistories(TimeSpan? gracia = null)
    {
        var limite = DateTime.Now - (gracia ?? OrphanGracePeriod);
        return DeleteOrphanHistories(FindOrphanHistories().Where(o => o.NewestLocal < limite).ToList());
    }

    // ---------------------------------------------------------------------------------------
    // Carpeta por personaje
    // ---------------------------------------------------------------------------------------

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
    private string DirectoryFor(string plrPath)
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
