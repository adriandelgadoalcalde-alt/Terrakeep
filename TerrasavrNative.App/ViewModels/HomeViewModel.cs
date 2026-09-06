using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Auditoria de Opus, I-1 ("Inicio deberia ser un lanzador real, no una pagina de bienvenida
// estatica que obliga a abrir el Explorador de archivos incluso para el caso normal"): escanea
// UNA VEZ al arrancar (y bajo demanda con "Actualizar", por si se ha guardado algo nuevo desde
// fuera) la carpeta real de personajes de tModLoader - la misma que ya usaba el dialogo de
// "Cargar personaje..." como carpeta inicial (CharacterFileService.GetDefaultPlayersDirectory).
// Solo lee lo minimo real de cada .plr (PlrFile.Read completo, son ficheros pequeños - sin
// tocar el .tplr, que solo hace falta para editar de verdad) - un error real leyendo un fichero
// concreto (corrupto, formato ajeno) no debe tumbar el listado entero de los demas.
//
// T-G (segunda auditoria de Opus, Fable): "arranque sincrono - HomeViewModel.Refresh() async,
// con IsScanning (ya existia como propiedad, pero SIN NINGUN binding real en el XAML - un
// interruptor que nunca encendia nada)". Medido de verdad antes de tocar nada (mismo criterio
// que X-7): ~97ms reales en esta maquina con solo 3 personajes (Debug, primera pasada -
// mayoria coste de JIT en frio de PlrFile.Read/AES/NBT, no I/O puro) - constructor de
// HomeViewModel corre COMO PARTE del constructor de MainViewModel, que a su vez corre ANTES de
// que MainWindow.InitializeComponent() pueda arrancar (field initializers de C#, orden real) -
// asi que esto retrasaba la ventana entera, no solo el listado de Inicio. Async de verdad
// (Task.Run para el escaneo real de disco - mismo patron ya usado y probado en
// ExplorationViewModel.LoadFromPathAsync) deja que la ventana aparezca sin esperar, y con
// muchos mas personajes reales (o una carpeta de Documentos sincronizada por OneDrive, I/O
// real mucho mas lento que esta maquina) la diferencia seria mucho mayor todavia.
public partial class HomeViewModel : ObservableObject
{
    public ObservableCollection<CharacterListEntryViewModel> Characters { get; } = [];

    [ObservableProperty] private bool _isScanning;

    // Oleada de pruebas del 6-sep-2026 (bloque INI-04 del arnes) - BUG REAL: esto era
    // `[ObservableProperty] private string? _scanMessage` y todos los sitios que lo rellenan le
    // metian el texto YA RESUELTO (`Loc.Format(...)`). Un texto resuelto una sola vez se queda
    // congelado en el idioma que hubiera en ese instante: con la app en español, "Fulano no
    // tiene ninguna copia de seguridad que restaurar" seguia en español despues de cambiar a
    // ingles en vivo, y lo mismo el "Ningun personaje encontrado en..." que ve de entrada quien
    // no tenga Terraria en la ruta habitual. El barrido de idioma (A10-IDIOMA-BARRIDO) no lo
    // detectaba porque compara lo que hay EN PANTALLA en ese momento, y este mensaje casi nunca
    // lo esta cuando se hace el barrido.
    //
    // Se guarda la CLAVE del diccionario y sus argumentos reales; el texto se compone al leerlo,
    // en el idioma activo, y un cambio de idioma dispara PropertyChanged (ver OnIdiomaCambiado)
    // para que lo que ya este en pantalla se reescriba solo.
    private string? _scanMessageKey;
    private object?[] _scanMessageArgs = [];
    // Caso aparte real: la lista de carpetas escaneadas se une con un separador que TAMBIEN es
    // texto traducido (" ni en " / " nor in "). Unirla al guardar dejaria ese separador
    // congelado dentro del argumento, asi que se guardan las carpetas y se unen al leer.
    private IReadOnlyList<string>? _scanMessageFolders;

    public string? ScanMessage
    {
        get
        {
            if (_scanMessageKey is null) return null;
            var loc = LocalizationService.Instance;
            return _scanMessageFolders is null
                ? loc.Format(_scanMessageKey, _scanMessageArgs)
                : loc.Format(_scanMessageKey, string.Join(loc["scan_folder_joiner"], _scanMessageFolders));
        }
    }

    private void SetScanMessage(string clave, params object?[] args)
    {
        _scanMessageKey = clave;
        _scanMessageArgs = args;
        _scanMessageFolders = null;
        OnPropertyChanged(nameof(ScanMessage));
    }

    private void SetScanMessageCarpetas(string clave, IReadOnlyList<string> carpetas)
    {
        _scanMessageKey = clave;
        _scanMessageArgs = [];
        _scanMessageFolders = carpetas;
        OnPropertyChanged(nameof(ScanMessage));
    }

    // Unica via real de borrarlo desde fuera - no hay setter publico a proposito: aceptar un
    // string ya resuelto es exactamente lo que reintroduciria el bug de arriba.
    public void ClearScanMessage()
    {
        if (_scanMessageKey is null) return;
        _scanMessageKey = null;
        _scanMessageArgs = [];
        _scanMessageFolders = null;
        OnPropertyChanged(nameof(ScanMessage));
    }

    // I-a: ruta real del personaje cargado ahora mismo en MainViewModel (null si ninguno) -
    // MainViewModel.LoadFromPath la actualiza en su finally, tanto en exito como en fallo.
    private string? _currentPath;

    public event Action<string>? CharacterChosen;

    // Doll fiel al guardado (pedido explicito, 3-sep-2026) - ver EquipmentAppearanceResolver.
    private readonly EquipmentAppearanceResolver _equipmentAppearance;
    // H5-04 (quinta auditoria de Opus): copias de seguridad rotativas, ver BackupHistoryService.
    private readonly BackupHistoryService _backupHistory;

    // H5-07 (quinta auditoria de Opus): "session.json recuerda el ULTIMO personaje real - Inicio
    // ofrece 'Continuar con Nombre' como accion destacada... nunca carga automatica silenciosa".
    private string? _lastSessionPath;
    [ObservableProperty] private string? _lastSessionCharacterName;
    // null = sin aviso real; non-null = el fichero cambio por fuera desde la ultima sesion real
    // (LastCharacterModifiedUtc guardado no coincide con la fecha real de ahora mismo) - se
    // sigue pudiendo continuar, pero avisado, nunca en silencio.
    //
    // Oleada del 6-sep-2026 (bloque INI-02 del arnes) - mismo BUG REAL que ScanMessage de
    // arriba: el texto se resolvia UNA vez aqui y se quedaba en el idioma de ese instante. Se
    // guarda el HECHO (¿cambio por fuera?) y el texto se compone al leerlo. Ademas, el camino
    // de "el fichero ya no existe" salia por `return` sin apagar un aviso anterior, que asi se
    // quedaba colgado apuntando a un personaje que ya no se ofrece.
    private bool _lastSessionIsStale;

    public string? LastSessionStalenessWarning => _lastSessionIsStale ? LocalizationService.Instance["home_stale_warning"] : null;

    public void SetLastSession(TerrakeepSession session)
    {
        _lastSessionPath = session.LastCharacterPath;
        if (_lastSessionPath == null || !File.Exists(_lastSessionPath))
        {
            LastSessionCharacterName = null;
            MarcarSesionCambiadaPorFuera(false);
            return;
        }
        LastSessionCharacterName = session.LastCharacterName ?? Path.GetFileNameWithoutExtension(_lastSessionPath);
        var modificadoReal = File.GetLastWriteTimeUtc(_lastSessionPath);
        MarcarSesionCambiadaPorFuera(session.LastCharacterModifiedUtc.HasValue && modificadoReal != session.LastCharacterModifiedUtc.Value);
    }

    private void MarcarSesionCambiadaPorFuera(bool valor)
    {
        if (_lastSessionIsStale == valor) return;
        _lastSessionIsStale = valor;
        OnPropertyChanged(nameof(LastSessionStalenessWarning));
    }

    // El texto de estas dos propiedades vive en el diccionario de idioma, no en un campo ya
    // resuelto - hay que volver a preguntarlo cuando el usuario cambia de idioma en vivo. Evento
    // DEBIL a proposito (mismo motivo real que LocalizedContentViewModel: LocalizationService es
    // un singleton que vive lo que la aplicacion, y `dotnet test` construye cientos de
    // MainViewModel); el handler es un metodo de instancia real, NUNCA una lambda - con una
    // lambda el objetivo del delegate es el cierre generado, que no referencia nadie mas y el
    // recolector puede llevarse en cualquier momento, dejando la suscripcion muerta en silencio.
    private void OnIdiomaCambiado(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(ScanMessage));
        OnPropertyChanged(nameof(LastSessionStalenessWarning));
    }

    [RelayCommand]
    private void Continue()
    {
        if (_lastSessionPath != null) CharacterChosen?.Invoke(_lastSessionPath);
    }

    public HomeViewModel(EquipmentAppearanceResolver equipmentAppearance, BackupHistoryService backupHistory)
    {
        _equipmentAppearance = equipmentAppearance;
        _backupHistory = backupHistory;
        System.ComponentModel.PropertyChangedEventManager.AddHandler(LocalizationService.Instance, OnIdiomaCambiado, "Item[]");

        // Fire-and-forget deliberado: el constructor no puede ser async, y no hay nada
        // real que esperar aqui todavia (el arranque de MainWindow sigue su curso normal -
        // Characters simplemente se rellena un instante despues, IsScanning ahora SI tiene
        // un spinner real que lo refleja mientras tanto, ver MainWindow.xaml).
        _ = RefreshAsync();
    }

    // I-a (segunda auditoria de Opus, Fable): "No se distingue que personaje esta cargado - las
    // tarjetas se ven identicas al volver a Inicio". Se llama tanto al cargar/cambiar de
    // personaje como tras Refresh() (la lista se reconstruye entera, IsCurrent no sobrevive).
    public void UpdateCurrentPath(string? path)
    {
        _currentPath = path;
        foreach (var entry in Characters)
            entry.IsCurrent = string.Equals(entry.FilePath, path, StringComparison.OrdinalIgnoreCase);
    }

    // H5-07 (quinta auditoria de Opus): bug real encontrado y arreglado verificando esta misma
    // pasada (visto en el propio arnes UIA: "10 personaje(s) encontrado(s)", cada uno duplicado)
    // - MainWindow.xaml.cs ahora relanza este mismo escaneo tras aplicar las carpetas
    // adicionales de Ajustes (LoadFromDisk), justo encima del escaneo AUTOMATICO que este mismo
    // constructor ya dispara (fire-and-forget) - dos vueltas reales de RefreshAsync en marcha a
    // la vez, cada una AÑADIENDO a Characters en vez de que la segunda sustituya a la primera.
    // Contador de generacion real: solo la vuelta MAS RECIENTE aplica su resultado - una vuelta
    // vieja que termina tarde se descarta en silencio en vez de pisar (o duplicar sobre) lo que
    // ya haya puesto una vuelta mas nueva.
    private int _scanGeneration;

    // El nombre real real de la carpeta escaneada solo hace falta para el mensaje "Ningun
    // personaje encontrado en..." - se calcula en el hilo de UI (barato, una sola llamada a
    // Environment.GetFolderPath) para poder mostrarlo aunque el escaneo en si falle.
    [RelayCommand]
    private async Task RefreshAsync()
    {
        int myGeneration = ++_scanGeneration;
        Characters.Clear();
        IsScanning = true;
        try
        {
            // Pedido explicito del usuario: personajes VANILLA (sin ningun mod, carpeta real
            // "Documents\My Games\Terraria\Players" sin el segmento "tModLoader") tambien
            // cuentan, no solo los de tModLoader - GetAllPlayersDirectories ya filtra a las que
            // existen de verdad (0, 1 o las 2), nunca cae a "Documentos entero".
            var dirs = CharacterFileService.GetAllPlayersDirectories();
            var scanned = await Task.Run(() => ScanCharacters(dirs, _equipmentAppearance));
            if (myGeneration != _scanGeneration) return; // una vuelta MAS NUEVA ya esta en marcha - esta es obsoleta
            foreach (var entry in scanned) Characters.Add(entry);
            if (Characters.Count > 0) ClearScanMessage();
            else if (dirs.Count == 0) SetScanMessage("scan_no_players_folder");
            else SetScanMessageCarpetas("scan_no_players_in", dirs);
            UpdateCurrentPath(_currentPath); // la lista es nueva de cero, IsCurrent hay que recalcularlo
        }
        finally
        {
            // Solo la vuelta MAS RECIENTE apaga el indicador - si una vuelta vieja termina
            // tarde (ej. I/O lento) mientras una mas nueva sigue en marcha, no debe fingir que
            // el escaneo real ya acabo.
            if (myGeneration == _scanGeneration) IsScanning = false;
        }
    }

    // Todo el trabajo real de disco (enumerar + leer + descifrar cada .plr) - se ejecuta en un
    // hilo de fondo via Task.Run (RefreshAsync de arriba), nunca toca ninguna ObservableCollection
    // directamente (serian modificaciones desde fuera del hilo de UI).
    private static List<CharacterListEntryViewModel> ScanCharacters(IEnumerable<string> dirs, EquipmentAppearanceResolver equipmentAppearance)
    {
        var result = new List<CharacterListEntryViewModel>();
        // Orden real GLOBAL por fecha (no por carpeta primero) - un personaje vanilla reciente
        // debe aparecer antes que uno de tModLoader mas antiguo, no al reves solo por venir de
        // una carpeta distinta.
        var plrFiles = dirs.SelectMany(dir => Directory.GetFiles(dir, "*.plr"));
        foreach (string path in plrFiles.OrderByDescending(File.GetLastWriteTimeUtc))
        {
            try
            {
                var character = PlrFile.Read(File.ReadAllBytes(path));
                // Encargo del usuario 4-sep-2026: el hecho PRINCIPAL es "este personaje es de
                // tModLoader" (tiene un .tplr hermano), no "es de Calamity" - la variable de
                // antes se llamaba isCalamity pero comprobaba exactamente esto, asi que
                // CUALQUIER mod (o un .tplr huerfano de una partida vieja) encendia una insignia
                // roja que decia "Calamity". Ver ESPEC-sprites-botones-badges.md#C.1.
                string tplrPath = Path.ChangeExtension(path, ".tplr");
                bool esTModLoader = File.Exists(tplrPath);
                // Coste real medido en la carpeta real de este usuario: 2,53 ms para los 6
                // personajes juntos, incluido un .tplr de 183 KB crudos con 2709 entradas de
                // research - y corre dentro del Task.Run que este escaneo ya usa, no en el hilo
                // de UI. No hay nada que optimizar aqui.
                var tplr = esTModLoader ? TplrProbe.TryRead(tplrPath) : null;
                result.Add(new CharacterListEntryViewModel(path, character, esTModLoader, tplr, File.GetLastWriteTimeUtc(path), equipmentAppearance));
            }
            catch (Exception)
            {
                // Un .plr ajeno/corrupto no debe tumbar el listado de los demas - se omite
                // en silencio, igual que ya hace la Libreria con ids sin catalogar.
            }
        }
        return result;
    }

    [RelayCommand]
    private void Open(CharacterListEntryViewModel entry) => CharacterChosen?.Invoke(entry.FilePath);

    // I-b (segunda auditoria de Opus, Fable): "Sin ninguna accion secundaria en la tarjeta -
    // faltan las 3 obvias y baratas: abrir carpeta, duplicar personaje, restaurar copia de
    // seguridad". Menu contextual real en la tarjeta (ver MainWindow.xaml).
    [RelayCommand]
    private void OpenFolder(CharacterListEntryViewModel entry)
    {
        try
        {
            // /select, resalta el fichero real en el Explorador en vez de solo abrir la carpeta
            // a ciegas - mismo gesto real que "Mostrar en carpeta" de cualquier otra app.
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{entry.FilePath}\"");
        }
        catch (Exception ex)
        {
            SetScanMessage("error_open_folder", ex.Message);
        }
    }

    [RelayCommand]
    private void Duplicate(CharacterListEntryViewModel entry)
    {
        try
        {
            // "la red de seguridad real para experimentar" - copia de fichero pura (mismo
            // nombre interno del personaje, solo cambia el archivo) para poder tocar la copia
            // sin arriesgar el original. Numerado si "(copia)" ya existe, nunca sobrescribe.
            string dir = Path.GetDirectoryName(entry.FilePath)!;
            string baseName = Path.GetFileNameWithoutExtension(entry.FilePath);
            // Ronda de idioma del 6-sep-2026: el sufijo iba interpolado a pelo aqui aunque la
            // clave "home_copy_suffix" YA existia en los dos diccionarios desde la ronda anterior
            // (creada y nunca enchufada) - duplicar un personaje con la app en ingles creaba un
            // "Fulano (copia).plr". "home_copy_suffix_first" es nueva: el primer duplicado no
            // lleva numero, y ese caso no tenia clave ninguna.
            string newPath = Path.Combine(dir, LocalizationService.Instance.Format("home_copy_suffix_first", baseName));
            for (int n = 2; File.Exists(newPath); n++)
                newPath = Path.Combine(dir, LocalizationService.Instance.Format("home_copy_suffix", baseName, n));

            File.Copy(entry.FilePath, newPath);
            string tplrSrc = Path.ChangeExtension(entry.FilePath, ".tplr");
            if (File.Exists(tplrSrc)) File.Copy(tplrSrc, Path.ChangeExtension(newPath, ".tplr"));
            _ = RefreshAsync(); // T-G: mismo criterio real que el constructor, fire-and-forget
        }
        catch (Exception ex)
        {
            SetScanMessage("error_duplicating", ex.Message);
        }
    }

    [RelayCommand]
    private void RestoreBackup(CharacterListEntryViewModel entry)
    {
        try
        {
            // Mismo .bak real que T-C ya deja (CharacterFileService.WriteAtomic) - esto es el
            // mismo mecanismo de "Deshacer ultimo guardado" de la cabecera, pero operando sobre
            // CUALQUIER personaje de la lista, este cargado ahora mismo o no.
            string plrBak = entry.FilePath + ".bak";
            if (!File.Exists(plrBak))
            {
                SetScanMessage("error_no_backup_to_restore", entry.Name);
                return;
            }
            File.Copy(plrBak, entry.FilePath, overwrite: true);
            string tplrPath = Path.ChangeExtension(entry.FilePath, ".tplr");
            string tplrBak = tplrPath + ".bak";
            if (File.Exists(tplrBak))
            {
                File.Copy(tplrBak, tplrPath, overwrite: true);
            }
            else if (File.Exists(tplrPath))
            {
                // H3-02 (tercera auditoria, Fable): mismo motivo real que MainViewModel.
                // UndoLastSave - sin .tplr.bak, este .tplr nacio en el mismo guardado que se
                // esta restaurando por encima, no existia antes. Dejarlo resucitaria su
                // contenido de Calamity al recargar.
                File.Delete(tplrPath);
            }
            _ = RefreshAsync(); // T-G: mismo criterio real que el constructor, fire-and-forget

            // H3-04 (tercera auditoria, Fable): "Restaurar copia de seguridad" restauraba los
            // ficheros en disco pero no avisaba a MainViewModel - si el personaje restaurado
            // era el que estaba cargado, el editor seguia mostrando el estado antiguo en
            // memoria, y un Guardar posterior lo machacaba en silencio. Mismo evento real ya
            // usado por "Cargar" (CharacterChosen) - MainViewModel ya lo conecta con su propio
            // aviso real de "cambios sin guardar" (ConfirmDiscardChanges) antes de recargar, asi
            // que unas ediciones en memoria sin guardar tambien avisan aqui, no solo se pisan.
            if (_currentPath != null && string.Equals(_currentPath, entry.FilePath, StringComparison.OrdinalIgnoreCase))
                CharacterChosen?.Invoke(entry.FilePath);
        }
        catch (Exception ex)
        {
            SetScanMessage("error_restoring_backup", ex.Message);
        }
    }

    // H5-04 (quinta auditoria de Opus): "un panel 'Historial de guardados'... con fecha, tamaño
    // y un boton por punto - para restaurar cualquiera, no solo el ultimo". Se consulta bajo
    // demanda (al abrir el submenu real, ver MainWindow.xaml.cs) - no al escanear Inicio, seria
    // I/O de sobra para personajes que el usuario nunca llega a abrir el menu contextual.
    public IReadOnlyList<BackupEntry> ListBackupPoints(CharacterListEntryViewModel entry) =>
        _backupHistory.ListBackups(entry.FilePath);

    [RelayCommand]
    private void RestoreBackupPoint((CharacterListEntryViewModel Entry, BackupEntry Backup) args)
    {
        var (entry, backup) = args;
        try
        {
            _backupHistory.Restore(entry.FilePath, null, backup);
            _ = RefreshAsync(); // T-G: mismo criterio real que el constructor, fire-and-forget

            // H3-04: mismo aviso real a MainViewModel que RestoreBackup de arriba - si el
            // personaje restaurado es el que esta cargado ahora mismo, el editor no debe seguir
            // mostrando el estado antiguo en memoria.
            if (_currentPath != null && string.Equals(_currentPath, entry.FilePath, StringComparison.OrdinalIgnoreCase))
                CharacterChosen?.Invoke(entry.FilePath);
        }
        catch (Exception ex)
        {
            SetScanMessage("error_restoring_backup_dated", backup.TimestampLocal.ToString("dd/MM/yyyy HH:mm:ss"), ex.Message);
        }
    }
}
