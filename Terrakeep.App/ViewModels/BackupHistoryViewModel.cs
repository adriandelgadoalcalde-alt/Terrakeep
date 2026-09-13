using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Terrakeep.App.Services;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels;

// BK (13-sep-2026, encargo "historial de copias de seguridad con versiones reales"): el panel
// real de historial. Antes esto era un SUBMENU del menu contextual de la tarjeta de Inicio
// (H5-04) con una linea "fecha · tamaño" por punto y un clic que restauraba AL INSTANTE, sin una
// sola pregunta. Tres cosas mal ahi, todas reales:
//   - Sobrescribir el personaje actual es una accion IRREVERSIBLE y ahi se disparaba con un
//     clic de menu, que es el gesto mas facil de dar por error de toda la interfaz (se navega
//     un menu con el raton apretado y el boton se suelta encima de lo que sea).
//   - "20260908-124213 · 3,7 KB" no dice nada sobre QUE hay dentro de esa version - es
//     imposible elegir un punto de retorno si todos se leen igual.
//   - Un menu contextual no cabe: 20 puntos + acciones no caben en una tira vertical de popup.
// Ahora es un panel real dentro de la ventana, con el resumen legible de cada version (nombre,
// vida, mana, horas jugadas, objetos, dificultad, si llevaba .tplr), confirmacion EN LA PROPIA
// FILA antes de sobrescribir nada, y copia de seguridad automatica del estado actual antes de
// restaurar (asi restaurar tambien se puede deshacer).
public sealed partial class BackupHistoryViewModel : ObservableObject
{
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    private readonly BackupHistoryService _service;

    public BackupHistoryViewModel(BackupHistoryService service)
    {
        _service = service;
        System.ComponentModel.PropertyChangedEventManager.AddHandler(LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    // Mismo criterio real (y mismo motivo) que HomeViewModel.OnIdiomaCambiado: evento DEBIL y
    // metodo de instancia real, nunca una lambda.
    private void OnIdiomaCambiado(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        foreach (var p in Points) p.RefreshTexts();
        OnPropertyChanged(nameof(FooterText));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(OrphanCleanupText));
    }

    public ObservableCollection<BackupPointViewModel> Points { get; } = [];

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private string? _characterName;

    // Ruta real del .plr cuyo historial se esta enseñando. Puede ser el personaje cargado ahora
    // mismo o cualquier otro de la lista de Inicio - el panel es el mismo.
    public string? PlrPath { get; private set; }

    // El personaje que se esta enseñando ES el que el editor tiene cargado ahora mismo: hace
    // falta saberlo para recargarlo tras restaurar (si no, el editor seguiria con el estado
    // viejo en memoria y el siguiente Guardar lo machacaria - mismo agujero real que cerro
    // H3-04 para "Restaurar copia de seguridad").
    public bool IsCurrentCharacter { get; private set; }

    // Quien recarga de verdad. MainViewModel lo enchufa; en pruebas headless puede quedarse null.
    public Action<string>? ReloadRequested { get; set; }

    private string? _statusKey;
    private object?[] _statusArgs = [];
    // Mismo bug real ya corregido en HomeViewModel (INI-04): guardar el TEXTO ya resuelto lo
    // congela en el idioma de ese instante. Se guarda la clave y se compone al leerlo.
    public string? StatusText => _statusKey == null ? null : LocalizationService.Instance.Format(_statusKey, _statusArgs);
    [ObservableProperty] private bool _statusIsError;

    private void SetStatus(string clave, bool esError, params object?[] args)
    {
        _statusKey = clave;
        _statusArgs = args;
        StatusIsError = esError;
        OnPropertyChanged(nameof(StatusText));
    }

    private void ClearStatus()
    {
        if (_statusKey == null) return;
        _statusKey = null;
        _statusArgs = [];
        StatusIsError = false;
        OnPropertyChanged(nameof(StatusText));
    }

    public string FooterText
    {
        get
        {
            if (PlrPath == null) return "";
            long bytes = _service.MeasureHistorySize(PlrPath);
            return LocalizationService.Instance.Format("backup_footer",
                Points.Count, _service.MaxBackupsPerCharacter, FormatSize(bytes));
        }
    }

    private IReadOnlyList<OrphanHistory> _orphans = [];
    public bool HasOrphans => _orphans.Count > 0;
    public string OrphanCleanupText => _orphans.Count == 0 ? "" :
        LocalizationService.Instance.Format("backup_clean_orphans", _orphans.Count, FormatSize(_orphans.Sum(o => o.SizeBytes)));

    public static string FormatSize(long bytes) =>
        bytes >= 1024 * 1024 ? $"{bytes / (1024.0 * 1024.0):0.0} MB" : $"{bytes / 1024.0:0.0} KB";

    // -------------------------------------------------------------------------------------

    public void Open(string plrPath, string? characterName, bool isCurrentCharacter)
    {
        PlrPath = plrPath;
        CharacterName = characterName ?? Path.GetFileNameWithoutExtension(plrPath);
        IsCurrentCharacter = isCurrentCharacter;
        ClearStatus();
        Reload();
        IsOpen = true;
    }

    // El velo de fondo del panel cierra al hacer clic; un clic DENTRO de la tarjeta no debe
    // cerrarla. WPF no tiene "traga este evento y no hagas nada" declarativo, asi que el
    // MouseBinding interior apunta a este comando vacio a proposito (lo mismo que se hace en el
    // resto del proyecto cuando hace falta parar la propagacion sin code-behind).
    [RelayCommand]
    private void Noop() { }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        foreach (var p in Points) p.IsConfirmingRestore = false;
    }

    public void Reload()
    {
        Points.Clear();
        if (PlrPath == null) return;
        foreach (var entry in _service.ListBackups(PlrPath))
            Points.Add(new BackupPointViewModel(entry));
        _orphans = _service.FindOrphanHistories();
        OnPropertyChanged(nameof(FooterText));
        OnPropertyChanged(nameof(HasOrphans));
        OnPropertyChanged(nameof(OrphanCleanupText));
        OnPropertyChanged(nameof(IsEmpty));
    }

    public bool IsEmpty => Points.Count == 0;

    // "Crear copia ahora" - snapshot manual bajo demanda, del fichero TAL Y COMO ESTA EN DISCO.
    // Pedido explicito del encargo ("decide si tambien conviene un snapshot manual"): si, y por
    // un motivo concreto - el snapshot automatico se dispara antes de GUARDAR, asi que marcar un
    // punto bueno obligaria si no a hacer un guardado de mentira. Ademas una copia manual es la
    // ultima en irse cuando el cupo aprieta (ver BackupHistoryService.Purge).
    [RelayCommand]
    private void CreateManualBackup()
    {
        if (PlrPath == null) return;
        try
        {
            var creada = _service.SaveBackup(PlrPath, null, BackupReason.Manual);
            Reload();
            if (creada == null) SetStatus("backup_manual_nothing", esError: true);
            else SetStatus("backup_manual_done", esError: false, creada.TimestampLocal.ToString("dd/MM/yyyy HH:mm:ss"));
        }
        catch (Exception ex)
        {
            SetStatus("backup_manual_failed", esError: true, ex.Message);
        }
    }

    [RelayCommand]
    private void AskRestore(BackupPointViewModel point)
    {
        // Una sola fila en modo "¿seguro?" a la vez - dos confirmaciones abiertas a la vez son
        // justo como se acaba pulsando la que no era.
        foreach (var p in Points) p.IsConfirmingRestore = ReferenceEquals(p, point);
        ClearStatus();
    }

    [RelayCommand]
    private void CancelRestore(BackupPointViewModel point) => point.IsConfirmingRestore = false;

    [RelayCommand]
    private void ConfirmRestore(BackupPointViewModel point)
    {
        if (PlrPath == null) return;
        point.IsConfirmingRestore = false;
        try
        {
            // INI-08 (bug real de perdida de datos ya vivido en este proyecto): NUNCA sobrescribir
            // el fichero bueno con una copia que ni siquiera se puede leer. Se lee ANTES de tocar
            // nada, con el mismo lector real que usa la app para cargar.
            byte[] bytes = _service.ReadPlrBytes(point.Entry);
            PlrFile.Read(bytes);

            // La propia restauracion se puede deshacer: antes de pisar el fichero actual se
            // fotografia lo que hay. Sin esto, elegir el punto equivocado (a dos clics de
            // distancia) destruiria el estado actual para siempre - exactamente el problema que
            // este panel existe para resolver, reintroducido por la puerta de atras.
            _service.SaveBackup(PlrPath, null, BackupReason.BeforeRestore);

            _service.Restore(PlrPath, null, point.Entry);
            Reload();
            SetStatus("backup_restored", esError: false, point.Entry.TimestampLocal.ToString("dd/MM/yyyy HH:mm:ss"));
            if (IsCurrentCharacter) ReloadRequested?.Invoke(PlrPath);
        }
        catch (Exception ex)
        {
            SetStatus("backup_restore_failed", esError: true, ex.Message);
        }
    }

    [RelayCommand]
    private void CleanOrphans()
    {
        try
        {
            var (carpetas, bytes) = _service.DeleteOrphanHistories(_orphans);
            Reload();
            SetStatus("backup_orphans_cleaned", esError: false, carpetas, FormatSize(bytes));
        }
        catch (Exception ex)
        {
            SetStatus("backup_manual_failed", esError: true, ex.Message);
        }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (PlrPath == null) return;
        try
        {
            string dir = _service.HistoryDirectoryFor(PlrPath);
            Directory.CreateDirectory(dir);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dir) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            SetStatus("backup_manual_failed", esError: true, ex.Message);
        }
    }
}

// Una fila del panel. Todo su texto se compone al leerlo (nunca se congela en el idioma del
// momento) - ver el bug real INI-04/INI-02 de HomeViewModel.
public sealed partial class BackupPointViewModel : ObservableObject
{
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    public BackupEntry Entry { get; }

    public BackupPointViewModel(BackupEntry entry) => Entry = entry;

    [ObservableProperty] private bool _isConfirmingRestore;

    public string WhenText => Entry.TimestampLocal.ToString("dd/MM/yyyy HH:mm:ss");

    public string ReasonText => LocalizationService.Instance[Entry.Reason switch
    {
        BackupReason.BeforeSave => "backup_reason_before_save",
        BackupReason.Manual => "backup_reason_manual",
        BackupReason.BeforeRestore => "backup_reason_before_restore",
        _ => "backup_reason_unknown",
    }];

    public string SizeText => BackupHistoryViewModel.FormatSize(Entry.SizeBytes);

    // El resumen legible que hace util la lista. Se compone SOLO con lo que el propio .plr
    // fotografiado dijo de verdad (BackupSnapshotInfo.SummaryAvailable) - una copia del formato
    // antiguo, o una cuyo .plr no se pudo leer, lo dice claro en vez de inventarse numeros.
    public string SummaryText
    {
        get
        {
            var loc = LocalizationService.Instance;
            var i = Entry.Info;
            if (i is not { SummaryAvailable: true })
                return Entry.IsLegacy ? loc["backup_summary_legacy"] : loc["backup_summary_unavailable"];

            var partes = new List<string>
            {
                i.CharacterName,
                AppearanceViewModel.DifficultyLabelFor(i.Difficulty),
                loc.Format("backup_summary_health", i.HealthNow, i.HealthMax),
                loc.Format("backup_summary_mana", i.ManaNow, i.ManaMax),
                loc.Format("backup_summary_playtime", HorasJugadas(i.PlayTimeTicks)),
                loc.Format("backup_summary_items", i.ItemCount),
            };
            if (i.HasTplr) partes.Add(loc["backup_summary_tplr"]);
            return string.Join(" · ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }

    // Mismo tick count de 64 bits que ya usa AppearanceViewModel (10 millones de ticks/segundo,
    // exactamente TimeSpan.Ticks - confirmado contra el script.js real de Terrasavr).
    private static string HorasJugadas(long ticks)
    {
        var t = TimeSpan.FromTicks(Math.Max(0, ticks));
        return t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}min" : $"{t.Minutes}min";
    }

    public void RefreshTexts()
    {
        OnPropertyChanged(nameof(ReasonText));
        OnPropertyChanged(nameof(SummaryText));
    }
}
