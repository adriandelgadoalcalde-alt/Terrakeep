using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Un slot individual de la rejilla de Buffs - calco reducido de ItemSlotViewModel (pregunta a
// Opus sobre el diseño 2-sep-2026, cuarta pasada: "la misma rejilla cantidad de slots y
// contorno y todo que inventario"). PlrCharacter.Buffs YA es un contenedor de tamaño FIJO real
// (44/22/10 segun version, ver PlrBodySerializer.cs) - antes la UI solo mostraba los slots CON
// buff, ahora se trata exactamente igual que un contenedor de items: 44 slots reales, algunos
// vacios. Un buff no tiene prefijo/cantidad/favorito, asi que esta clase es mas simple que
// ItemSlotViewModel, no una generalizacion de la misma.
public partial class BuffSlotViewModel : ObservableObject
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    private readonly VanillaBuffCatalog _vanillaCatalog;
    private readonly CalamityBuffCatalog _calamityCatalog;
    private readonly VanillaBuffDurationCatalog _durations;
    private readonly int _characterVersion;
    private readonly Action<BuffSlotViewModel>? _requestPick;
    // Bu-b (segunda auditoria de Opus, Fable): "se pueden poner buffs duplicados - PlaceBuff no
    // comprueba si el buff ya esta en otro slot, Terraria no tiene dos instancias del mismo
    // buff". No hay referencia directa a los slots hermanos (se construyen todos a la vez en
    // BuffsViewModel.LoadFrom) - se les pasa un delegado real en vez de la coleccion entera, la
    // misma coleccion que se sigue rellenando mientras este slot se construye.
    private readonly Func<int, BuffSlotViewModel, bool>? _isPlacedElsewhere;
    private bool _suppressDurationWriteback;

    public PlrBuff Buff { get; }
    public int SlotIndex { get; }

    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string? _iconPath;
    [ObservableProperty] private bool _isCalamity;
    // H6-12 (sexta auditoria de Opus, "no hay forma de distinguir buff de debuff"): real, de
    // CalamityBuffEntry.IsDebuff. Siempre false para buffs vanilla en esta pasada (fuera de
    // alcance, el usuario lo pidio especificamente para Calamity).
    [ObservableProperty] private bool _isDebuff;
    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private int _durationSeconds;
    [ObservableProperty] private bool _isSelected;
    // L-e (segunda auditoria de Opus, Fable): mismo arreglo real que ItemSlotViewModel - el
    // aviso de rechazo no debe sobrevivir a un cambio de seleccion.
    partial void OnIsSelectedChanged(bool value) => RejectionMessage = null;

    // Mismo patron real ya usado en ItemSlotViewModel.RejectionMessage - un aviso real, no
    // modal, que se limpia solo en la siguiente colocacion con exito.
    [ObservableProperty] private string? _rejectionMessage;

    // Bu-a (segunda auditoria de Opus, Fable): "T-14 se porto a objetos y no a buffs, aunque
    // los comentarios declaran a los dos paneles como gemelos". Calco literal real de
    // ItemSlotViewModel.JustEdited/TriggerEditFlash - ver ahi el porque del False->True
    // explicito (re-disparar el flash en la MISMA edicion en <450ms) y de Task.Delay en vez de
    // un DispatcherTimer por instancia.
    [ObservableProperty] private bool _justEdited;

    public void TriggerEditFlash()
    {
        JustEdited = false;
        JustEdited = true;
        _ = ResetEditFlashAsync();
    }

    private async System.Threading.Tasks.Task ResetEditFlashAsync()
    {
        await System.Threading.Tasks.Task.Delay(450);
        JustEdited = false;
    }

    public bool IsNotEmpty => !IsEmpty;
    partial void OnIsEmptyChanged(bool value) => OnPropertyChanged(nameof(IsNotEmpty));

    public BuffSlotViewModel(int slotIndex, PlrBuff buff, VanillaBuffCatalog vanillaCatalog,
        CalamityBuffCatalog calamityCatalog, VanillaBuffDurationCatalog durations, int characterVersion,
        Action<BuffSlotViewModel>? requestPick = null, Func<int, BuffSlotViewModel, bool>? isPlacedElsewhere = null)
    {
        SlotIndex = slotIndex;
        Buff = buff;
        _vanillaCatalog = vanillaCatalog;
        _calamityCatalog = calamityCatalog;
        _durations = durations;
        _characterVersion = characterVersion;
        _requestPick = requestPick;
        _isPlacedElsewhere = isPlacedElsewhere;
        Refresh();
        // Ronda de traduccion del CONTENIDO del juego (6-sep-2026): el nombre y la descripcion
        // del buff ya existen en los dos idiomas (vanilla_buff_names_en.json /
        // vanilla_buff_descriptions_en.json, y displayName_fallback para Calamity), pero aqui
        // son valores FIJADOS en Refresh, no propiedades calculadas - sin esto un cambio de
        // idioma en vivo dejaba los 44 slots de buff con el texto anterior. Evento DEBIL, mismo
        // motivo real que ItemSlotViewModel: el servicio de idioma es un singleton que vive lo
        // que la aplicacion y estos slots no.
        System.ComponentModel.PropertyChangedEventManager.AddHandler(
            Services.LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    // Refresh() es idempotente sobre el buff que ya hay puesto (no toca PlrBuff, solo recalcula
    // lo que se muestra), asi que rehacerlo entero al cambiar de idioma es seguro.
    private void OnIdiomaCambiado(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        IsEmpty = Buff.Id == 0;

        _suppressDurationWriteback = true;
        DurationSeconds = Buff.Time / 60;
        _suppressDurationWriteback = false;

        if (IsEmpty)
        {
            DisplayName = string.Empty;
            Description = null;
            IconPath = null;
            IsCalamity = false;
            IsDebuff = false;
            return;
        }

        IsCalamity = Buff.Id >= CalamityIds.BuffIdBase;
        if (IsCalamity)
        {
            var entry = _calamityCatalog.BySyntheticId(Buff.Id);
            DisplayName = entry?.DisplayName ?? $"Calamity #{Buff.Id}";
            IconPath = entry?.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/buff_icons/" + entry.Icon : null;
            Description = entry?.Description;
            IsDebuff = entry?.IsDebuff ?? false;
        }
        else
        {
            DisplayName = _vanillaCatalog.GetDisplayName(Buff.Id);
            IconPath = VanillaBuffIconResolver.GetIconPath(Buff.Id);
            Description = _vanillaCatalog.GetDescription(Buff.Id);
            IsDebuff = false; // vanilla fuera de alcance de H6-12, ver el comentario del campo
        }
    }

    // Coloca un buff nuevo - duracion inicial al minimo real de ese buff (BuffDurationPresets,
    // el mismo criterio "mejor prefijo automatico" que ya usa ItemSlotViewModel.PlaceItem con
    // objetos: un valor real y razonable de entrada, editable despues a mano o con los 3
    // botones Minima/Media/Maxima del panel Editar).
    //
    // H4-04 (cuarta auditoria de Opus, Fable): version SIN EFECTO de la comprobacion real que
    // hace PlaceBuff - usada por OnBuffSlotDragOver (mismo criterio real ya usado por
    // ItemSlotViewModel.AcceptsItem, un "vistazo" antes de soltar de verdad) para poner el
    // cursor de prohibido del sistema MIENTRAS se arrastra, no solo al soltar.
    public bool WouldRejectPlacingBuff(int buffId) => _isPlacedElsewhere?.Invoke(buffId, this) == true;

    // Bu-b (segunda auditoria de Opus, Fable): Terraria no tiene dos instancias del mismo buff
    // activas a la vez - rechaza la colocacion (sin tocar el slot) si ese buff YA esta en otro
    // slot, mismo criterio real de "avisar, no fingir" que RejectionMessage ya usa en
    // ItemSlotViewModel.
    public bool PlaceBuff(int buffId)
    {
        if (_isPlacedElsewhere?.Invoke(buffId, this) == true)
        {
            bool esDeCalamity = buffId >= CalamityIds.BuffIdBase;
            string nombre = esDeCalamity
                ? _calamityCatalog.BySyntheticId(buffId)?.DisplayName ?? $"Calamity #{buffId}"
                : _vanillaCatalog.GetDisplayName(buffId);
            RejectionMessage = Services.LocalizationService.Instance.Format("error_buff_duplicate", nombre);
            return false;
        }
        RejectionMessage = null;
        Buff.Id = buffId;
        // Oleada del 6-sep-2026: antes esto se bifurcaba y a un buff de Calamity le ponia
        // 600*60 ticks (10 min) "razonables" a ojo, mientras el panel Editar le ofrecia una
        // "Minima" de 28800 (8 min) - o sea que colocar un buff de Calamity y pulsar "Minima"
        // BAJABA la duracion, justo al reves de lo que ese boton promete, y el 10 no salia de
        // ninguna fuente real. GetPresets ya resuelve los dos casos por si solo: dato REAL para
        // vanilla y, sin tabla de duraciones de Calamity todavia, el fallback documentado
        // (28800, la moda real de los buffTime conocidos, con IsRealMin=false para que el
        // tooltip no finja que es un dato real). Colocar = "Minima" en los dos, sin numeros
        // magicos sueltos.
        Buff.Time = BuffDurationPresets.GetPresets(buffId, _characterVersion, _durations).MinTicks;
        Refresh();
        return true;
    }

    public void SwapWith(BuffSlotViewModel other)
    {
        (Buff.Id, other.Buff.Id) = (other.Buff.Id, Buff.Id);
        (Buff.Time, other.Buff.Time) = (other.Buff.Time, Buff.Time);
        Refresh();
        other.Refresh();
    }

    // H5-14 (quinta auditoria de Opus): gemelo real de ItemSlotViewModel.PasteItem - Ctrl+C/
    // Ctrl+V pega la duracion EXACTA copiada (a diferencia de PlaceBuff, que fija una duracion
    // razonable para una colocacion nueva). SI respeta la regla real de "sin dos instancias del
    // mismo buff a la vez" (a diferencia de RestoreExact, pensado solo para Deshacer, donde esa
    // comprobacion no aplica) - un copia/pega deliberado del usuario no debe saltarsela.
    public bool PasteBuff(int buffId, int time)
    {
        if (buffId > 0 && _isPlacedElsewhere?.Invoke(buffId, this) == true)
        {
            bool esDeCalamity = buffId >= CalamityIds.BuffIdBase;
            string nombre = esDeCalamity
                ? _calamityCatalog.BySyntheticId(buffId)?.DisplayName ?? $"Calamity #{buffId}"
                : _vanillaCatalog.GetDisplayName(buffId);
            RejectionMessage = Services.LocalizationService.Instance.Format("error_buff_duplicate", nombre);
            return false;
        }
        RejectionMessage = null;
        Buff.Id = buffId;
        Buff.Time = time;
        Refresh();
        return true;
    }

    // H4-06 (cuarta auditoria de Opus, Fable): usado por BuffContainerViewModel.UndoClear -
    // restaura un buff EXACTO (id+duracion) tal cual estaba antes de un "Vaciar todos" en
    // bloque. A diferencia de PlaceBuff (que fija una duracion RAZONABLE para una colocacion
    // nueva, nunca la exacta de origen), Deshacer necesita el dato real, no una aproximacion -
    // y no debe volver a comprobar duplicados (en el momento de deshacer, todos los slots ya
    // estan vacios de verdad, no hay nada con lo que colisionar).
    public void RestoreExact(int buffId, int time)
    {
        Buff.Id = buffId;
        Buff.Time = time;
        Refresh();
    }

    [RelayCommand]
    private void Clear()
    {
        Buff.Id = 0;
        Buff.Time = 0;
        Refresh();
    }

    [RelayCommand]
    private void ChooseFromLibrary() => _requestPick?.Invoke(this);

    // H3-12 (tercera auditoria de Opus, Fable): sin techo real, escribir un numero de segundos
    // lo bastante grande a mano desbordaba el int de Buff.Time al multiplicar por 60 (ej.
    // 40000000s * 60 = 2400000000, > int.MaxValue=2147483647, se volvia NEGATIVO en silencio -
    // ningun aviso, un guardado real habria escrito basura). Mismo techo GLOBAL real que ya usa
    // el boton "Maxima" (S.getMaxTime(), BuffDurationPresets.MaxTicksForVersion) - en segundos
    // para poder acotar ANTES de multiplicar, no despues.
    partial void OnDurationSecondsChanged(int value)
    {
        if (_suppressDurationWriteback || IsEmpty) return;
        int maxSeconds = BuffDurationPresets.MaxTicksForVersion(_characterVersion) / 60;
        int clamped = Math.Clamp(value, 0, maxSeconds);
        Buff.Time = clamped * 60;
        if (clamped != value)
        {
            _suppressDurationWriteback = true;
            DurationSeconds = clamped;
            _suppressDurationWriteback = false;
        }
    }

    // Aplica una duracion real en TICKS (usada por los 3 botones Minima/Media/Maxima del panel
    // Editar - BuffDurationPresets ya trabaja en ticks, no segundos).
    public void SetDurationTicks(int ticks)
    {
        Buff.Time = Math.Max(0, ticks);
        _suppressDurationWriteback = true;
        DurationSeconds = Buff.Time / 60;
        _suppressDurationWriteback = false;
    }
}
