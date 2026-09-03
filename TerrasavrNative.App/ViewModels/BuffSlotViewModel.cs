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
    }

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
            return;
        }

        IsCalamity = Buff.Id >= CalamityIds.BuffIdBase;
        if (IsCalamity)
        {
            var entry = _calamityCatalog.BySyntheticId(Buff.Id);
            DisplayName = entry?.DisplayName ?? $"Calamity #{Buff.Id}";
            IconPath = entry?.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/buff_icons/" + entry.Icon : null;
            Description = entry?.Description;
        }
        else
        {
            DisplayName = _vanillaCatalog.GetDisplayName(Buff.Id);
            IconPath = VanillaBuffIconResolver.GetIconPath(Buff.Id);
            Description = _vanillaCatalog.GetDescription(Buff.Id);
        }
    }

    // Coloca un buff nuevo - duracion inicial al minimo real de ese buff (BuffDurationPresets,
    // el mismo criterio "mejor prefijo automatico" que ya usa ItemSlotViewModel.PlaceItem con
    // objetos: un valor real y razonable de entrada, editable despues a mano o con los 3
    // botones Minima/Media/Maxima del panel Editar).
    //
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
            RejectionMessage = $"'{nombre}' ya esta puesto en otro slot - Terraria no permite dos instancias del mismo buff.";
            return false;
        }
        RejectionMessage = null;
        Buff.Id = buffId;
        Buff.Time = buffId < CalamityIds.BuffIdBase
            ? BuffDurationPresets.GetPresets(buffId, _characterVersion, _durations).MinTicks
            : 600 * 60; // Calamity: sin tabla de duraciones real todavia, 10 min razonable
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

    [RelayCommand]
    private void Clear()
    {
        Buff.Id = 0;
        Buff.Time = 0;
        Refresh();
    }

    [RelayCommand]
    private void ChooseFromLibrary() => _requestPick?.Invoke(this);

    partial void OnDurationSecondsChanged(int value)
    {
        if (_suppressDurationWriteback || IsEmpty) return;
        int clamped = Math.Max(0, value);
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
