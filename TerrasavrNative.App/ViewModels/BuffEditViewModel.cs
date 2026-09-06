using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Panel "Editar buff seleccionado" - gemelo reducido de ItemEditViewModel, sigue al slot de
// buff SELECCIONADO (pregunta a Opus sobre el diseño 2-sep-2026, cuarta pasada: "en el lado
// derecho igual que en edición de objetos... 'editar buff seleccionado'... 3 botones que
// pongan duración maxima media y mínima"). El campo de duracion en si se enlaza DIRECTO a
// Slot.DurationSeconds en el XAML (mismo criterio que Slot.Count en ItemEditTemplate) - esta
// clase solo añade los 3 botones de preset, que no tenian sitio en BuffSlotViewModel.
public partial class BuffEditViewModel : ObservableObject
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    private readonly CharacterFileService _service;
    private int _characterVersion = 279;
    private BuffDurationPreset _preset;

    [ObservableProperty] private BuffSlotViewModel? _slot;
    [ObservableProperty] private bool _hasSelection;
    [ObservableProperty] private string _noSelectionMessage = LocalizationService.Instance["select_slot_to_edit"];
    [ObservableProperty] private string _minLabel = LocalizationService.Instance["duration_min"];
    [ObservableProperty] private string _mediaLabel = LocalizationService.Instance["duration_media"];
    [ObservableProperty] private string _maxLabel = LocalizationService.Instance["duration_max"];
    // Tooltip honesto (pregunta a Opus, cuarta pasada): "Mínima" es un dato REAL extraido del
    // buffTime de la pocion base salvo cuando no existe ese dato, en cuyo caso se usa la moda
    // real de los buffTime conocidos como aproximacion - la UI no debe fingir que ambos casos
    // son igual de reales.
    [ObservableProperty] private string _minTooltip = string.Empty;

    public BuffEditViewModel(CharacterFileService service)
    {
        _service = service;
        // Ronda de idioma del 6-sep-2026: gemelo real del arreglo de ItemEditViewModel - los
        // textos de este panel se fijaban al construir y no se reevaluaban al cambiar de idioma.
        System.ComponentModel.PropertyChangedEventManager.AddHandler(
            LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
        Refresh();
    }

    private void OnIdiomaCambiado(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Refresh();

    // El maximo real (S.getMaxTime()) depende de si el personaje es version>=269 - se fija al
    // cargar el personaje (MainViewModel.LoadFromPath), igual que el resto de umbrales de
    // version ya usados en este puerto (PlrBodySerializer, EquipmentGroupViewModel).
    public void SetCharacterVersion(int version)
    {
        _characterVersion = version;
        Refresh();
    }

    partial void OnSlotChanged(BuffSlotViewModel? oldValue, BuffSlotViewModel? newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= OnSlotPropertyChanged;
        if (newValue != null) newValue.PropertyChanged += OnSlotPropertyChanged;
        Refresh();
    }

    // H3-13 (tercera auditoria de Opus, Fable): gemelo de B-6 (ya cerrado en
    // EquipmentGroupViewModel) - filtrar solo por IsEmpty no detecta un buff que SUSTITUYE a
    // otro en un slot YA ocupado y YA seleccionado aqui (SwapWith entre dos slots ocupados,
    // IsEmpty se queda en false en los dos extremos, nunca cambia de valor, nunca dispara
    // PropertyChanged) - los 3 presets Minima/Media/Maxima se quedaban calculados para el buff
    // VIEJO. DisplayName SI cambia siempre que el buff realmente cambia (incluido ocupado ->
    // otro buff distinto, Terraria no permite dos slots con el mismo id a la vez - Bu-b), y
    // NUNCA por escribir la duracion a mano (Refresh() de BuffSlotViewModel, quien fija
    // DisplayName, solo se llama desde PlaceBuff/SwapWith/Clear, no desde
    // OnDurationSecondsChanged) - no repite el trabajo de sobra que el comentario de abajo ya
    // evitaba.
    private void OnSlotPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Solo recalcular los presets si cambio DE OBJETO (Id) - recalcular en cada tecla de
        // DurationSeconds seria trabajo de sobra y ademas machacaria el propio valor que el
        // usuario esta escribiendo.
        if (e.PropertyName is nameof(BuffSlotViewModel.IsEmpty) or nameof(BuffSlotViewModel.DisplayName)) Refresh();
    }

    private void Refresh()
    {
        HasSelection = Slot != null && !Slot.IsEmpty;
        NoSelectionMessage = Slot == null ? LocalizationService.Instance["select_slot_to_edit"] : LocalizationService.Instance["slot_empty"];
        if (!HasSelection)
        {
            MinLabel = LocalizationService.Instance["duration_min"];
            MediaLabel = LocalizationService.Instance["duration_media"];
            MaxLabel = LocalizationService.Instance["duration_max"];
            MinTooltip = string.Empty;
            return;
        }

        _preset = BuffDurationPresets.GetPresets(Slot!.Buff.Id, _characterVersion, _service.VanillaBuffDurations);
        MinLabel = LocalizationService.Instance.Format("duration_min_with_value", FormatDuration(_preset.MinTicks));
        MediaLabel = LocalizationService.Instance.Format("duration_media_with_value", FormatDuration(_preset.MediaTicks));
        MaxLabel = LocalizationService.Instance.Format("duration_max_with_value", FormatDuration(_preset.MaxTicks));
        MinTooltip = _preset.IsRealMin
            ? LocalizationService.Instance["tt_duration_real"]
            : LocalizationService.Instance["tt_duration_estimated"];
    }

    [RelayCommand]
    private void ApplyMin() => Slot?.SetDurationTicks(_preset.MinTicks);

    [RelayCommand]
    private void ApplyMedia() => Slot?.SetDurationTicks(_preset.MediaTicks);

    [RelayCommand]
    private void ApplyMax() => Slot?.SetDurationTicks(_preset.MaxTicks);

    [RelayCommand]
    private void ClearSlot() => Slot?.ClearCommand.Execute(null);

    private static string FormatDuration(int ticks)
    {
        int totalSeconds = ticks / 60;
        if (totalSeconds < 60) return $"{totalSeconds}s";
        if (totalSeconds < 3600) return $"{totalSeconds / 60}min";
        if (totalSeconds < 86400) return $"{totalSeconds / 3600}h";
        return $"{totalSeconds / 86400}d";
    }
}
