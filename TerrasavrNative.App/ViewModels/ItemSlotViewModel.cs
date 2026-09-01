using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App.ViewModels;

// Un slot individual de un contenedor. Edicion real, de momento solo el prefijo (boton "mejor
// prefijo") - cambiar/quitar el objeto en si o su cantidad sigue pendiente de una fase
// posterior.
public partial class ItemSlotViewModel : ObservableObject
{
    private readonly CharacterFileService _service;

    public int SlotIndex { get; }
    public GameItem Item { get; private set; } = GameItem.Empty;

    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private int _count;
    [ObservableProperty] private bool _isCalamity;
    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private string _prefixDisplay = string.Empty;
    [ObservableProperty] private bool _hasBestPrefixSuggestion;

    public ItemSlotViewModel(CharacterFileService service, int slotIndex, GameItem item)
    {
        _service = service;
        SlotIndex = slotIndex;
        UpdateFrom(item);
    }

    public void UpdateFrom(GameItem item)
    {
        Item = item;
        IsEmpty = item.IsEmpty;
        IsCalamity = item.IsCalamity;
        Count = item.Count;

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
