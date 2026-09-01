using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App.ViewModels;

// Un slot individual de un contenedor. Edicion real: prefijo (boton "mejor prefijo"), cantidad
// (editable directamente), colocar un objeto nuevo (delega en el buscador de la Libreria via
// requestPick) y vaciar el slot.
public partial class ItemSlotViewModel : ObservableObject
{
    private readonly CharacterFileService _service;
    private readonly Action<ItemSlotViewModel>? _requestPick;
    private bool _suppressCountWriteback;

    public int SlotIndex { get; }
    public GameItem Item { get; private set; } = GameItem.Empty;

    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private int _count;
    [ObservableProperty] private bool _isCalamity;
    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private string _prefixDisplay = string.Empty;
    [ObservableProperty] private bool _hasBestPrefixSuggestion;

    public bool IsNotEmpty => !IsEmpty;
    partial void OnIsEmptyChanged(bool value) => OnPropertyChanged(nameof(IsNotEmpty));

    public ItemSlotViewModel(CharacterFileService service, int slotIndex, GameItem item, Action<ItemSlotViewModel>? requestPick = null)
    {
        _service = service;
        SlotIndex = slotIndex;
        _requestPick = requestPick;
        UpdateFrom(item);
    }

    public void UpdateFrom(GameItem item)
    {
        Item = item;
        IsEmpty = item.IsEmpty;
        IsCalamity = item.IsCalamity;

        _suppressCountWriteback = true;
        Count = item.Count;
        _suppressCountWriteback = false;

        if (item.IsEmpty)
        {
            DisplayName = string.Empty;
            PrefixDisplay = string.Empty;
            HasBestPrefixSuggestion = false;
            return;
        }

        DisplayName = item.IsCalamity
            ? _service.CalamityCatalog.BySyntheticId(item.Id)?.DisplayName ?? $"Calamity #{item.Id}"
            : _service.VanillaCatalog.GetName(item.Id);

        RefreshPrefixDisplay();

        var suggestion = PrefixSuggester.Suggest(item, _service.CalamityCatalog, _service.BestPrefixes, _service.RoguePrefixCatalog);
        HasBestPrefixSuggestion = suggestion.HasValue && !suggestion.Value.Equals(item.Prefix);
    }

    // Coloca un objeto nuevo del catalogo (id real vanilla, o sintetico de Calamity) en este
    // slot - cantidad 1, sin prefijo, sin datos de Calamity heredados (es un objeto nuevo).
    public void PlaceItem(int id)
    {
        UpdateFrom(new GameItem { Id = id, Count = 1 });
    }

    [RelayCommand]
    private void Clear() => UpdateFrom(GameItem.Empty);

    [RelayCommand]
    private void ChooseFromLibrary() => _requestPick?.Invoke(this);

    partial void OnCountChanged(int value)
    {
        if (_suppressCountWriteback || Item.IsEmpty) return;
        // Un objeto real siempre tiene al menos 1 unidad - 0 significaria vaciar el slot,
        // para eso ya esta el boton "Vaciar" explicito.
        int clamped = Math.Clamp(value, 1, 9999);
        Item.Count = clamped;
        if (clamped != value)
        {
            _suppressCountWriteback = true;
            Count = clamped;
            _suppressCountWriteback = false;
        }
    }

    private void RefreshPrefixDisplay()
    {
        var prefix = Item.Prefix;
        if (prefix.IsCalamity)
        {
            var prefixEntry = _service.RoguePrefixCatalog.ById(prefix.SyntheticId);
            PrefixDisplay = prefixEntry?.Es ?? prefixEntry?.En ?? string.Empty;
        }
        else if (!prefix.IsNone)
        {
            var prefixEntry = _service.VanillaPrefixCatalog.ById(prefix.VanillaId);
            PrefixDisplay = prefixEntry?.Es ?? prefixEntry?.En ?? $"Prefijo #{prefix.VanillaId}";
        }
        else
        {
            PrefixDisplay = string.Empty;
        }
    }

    [RelayCommand]
    private void ApplyBestPrefix()
    {
        var suggestion = PrefixSuggester.Suggest(Item, _service.CalamityCatalog, _service.BestPrefixes, _service.RoguePrefixCatalog);
        if (suggestion == null) return;

        Item.Prefix = suggestion.Value;
        RefreshPrefixDisplay();
        HasBestPrefixSuggestion = false;
    }
}
