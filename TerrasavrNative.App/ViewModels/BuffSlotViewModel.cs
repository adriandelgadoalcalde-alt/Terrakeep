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

    public bool IsNotEmpty => !IsEmpty;
    partial void OnIsEmptyChanged(bool value) => OnPropertyChanged(nameof(IsNotEmpty));

    public BuffSlotViewModel(int slotIndex, PlrBuff buff, VanillaBuffCatalog vanillaCatalog,
        CalamityBuffCatalog calamityCatalog, VanillaBuffDurationCatalog durations, int characterVersion,
        Action<BuffSlotViewModel>? requestPick = null)
    {
        SlotIndex = slotIndex;
        Buff = buff;
        _vanillaCatalog = vanillaCatalog;
        _calamityCatalog = calamityCatalog;
        _durations = durations;
        _characterVersion = characterVersion;
        _requestPick = requestPick;
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
    public void PlaceBuff(int buffId)
    {
        Buff.Id = buffId;
        Buff.Time = buffId < CalamityIds.BuffIdBase
            ? BuffDurationPresets.GetPresets(buffId, _characterVersion, _durations).MinTicks
            : 600 * 60; // Calamity: sin tabla de duraciones real todavia, 10 min razonable
        Refresh();
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
