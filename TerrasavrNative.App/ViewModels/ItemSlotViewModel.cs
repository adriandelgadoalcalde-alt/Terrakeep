using CommunityToolkit.Mvvm.ComponentModel;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App.ViewModels;

// Un slot individual de un contenedor - de momento solo lectura (Fase 2, nucleo basico); la
// edicion (cambiar/quitar objeto, prefijo, cantidad) llega en una fase posterior.
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
            return;
        }

        DisplayName = item.IsCalamity
            ? _service.CalamityCatalog.BySyntheticId(item.Id)?.DisplayName ?? $"Calamity #{item.Id}"
            : _service.VanillaCatalog.GetName(item.Id);

        if (item.Prefix.IsCalamity)
        {
            var prefixEntry = _service.RoguePrefixCatalog.ById(item.Prefix.SyntheticId);
            PrefixDisplay = prefixEntry?.Es ?? prefixEntry?.En ?? string.Empty;
        }
        else if (!item.Prefix.IsNone)
        {
            var prefixEntry = _service.VanillaPrefixCatalog.ById(item.Prefix.VanillaId);
            PrefixDisplay = prefixEntry?.Es ?? prefixEntry?.En ?? $"Prefijo #{item.Prefix.VanillaId}";
        }
        else
        {
            PrefixDisplay = string.Empty;
        }
    }
}
