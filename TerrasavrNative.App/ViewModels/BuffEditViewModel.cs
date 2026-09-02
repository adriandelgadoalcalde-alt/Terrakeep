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
    private readonly CharacterFileService _service;
    private int _characterVersion = 279;
    private BuffDurationPreset _preset;

    [ObservableProperty] private BuffSlotViewModel? _slot;
    [ObservableProperty] private bool _hasSelection;
    [ObservableProperty] private string _noSelectionMessage = "Selecciona un slot para editarlo.";
    [ObservableProperty] private string _minLabel = "Mínima";
    [ObservableProperty] private string _mediaLabel = "Media";
    [ObservableProperty] private string _maxLabel = "Máxima";
    // Tooltip honesto (pregunta a Opus, cuarta pasada): "Mínima" es un dato REAL extraido del
    // buffTime de la pocion base salvo cuando no existe ese dato, en cuyo caso se usa la moda
    // real de los buffTime conocidos como aproximacion - la UI no debe fingir que ambos casos
    // son igual de reales.
    [ObservableProperty] private string _minTooltip = string.Empty;

    public BuffEditViewModel(CharacterFileService service)
    {
        _service = service;
        Refresh();
    }

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

    private void OnSlotPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Solo recalcular los presets si cambio DE OBJETO (Id) - recalcular en cada tecla de
        // DurationSeconds seria trabajo de sobra y ademas machacaria el propio valor que el
        // usuario esta escribiendo.
        if (e.PropertyName is nameof(BuffSlotViewModel.IsEmpty)) Refresh();
    }

    private void Refresh()
    {
        HasSelection = Slot != null && !Slot.IsEmpty;
        NoSelectionMessage = Slot == null ? "Selecciona un slot para editarlo." : "Este slot está vacío.";
        if (!HasSelection)
        {
            MinLabel = "Mínima";
            MediaLabel = "Media";
            MaxLabel = "Máxima";
            MinTooltip = string.Empty;
            return;
        }

        _preset = BuffDurationPresets.GetPresets(Slot!.Buff.Id, _characterVersion, _service.VanillaBuffDurations);
        MinLabel = $"Mínima ({FormatDuration(_preset.MinTicks)})";
        MediaLabel = $"Media ({FormatDuration(_preset.MediaTicks)})";
        MaxLabel = $"Máxima ({FormatDuration(_preset.MaxTicks)})";
        MinTooltip = _preset.IsRealMin
            ? "Duración real de la poción/objeto que da este buff en el juego"
            : "Sin dato real en el juego para este buff - valor más común de las pociones vanilla";
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
