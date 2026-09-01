using System.Collections.ObjectModel;

namespace TerrasavrNative.App.ViewModels;

// Un grupo de slots con nombre para mostrar (una pestaña/seccion: Inventario, Banco...).
public sealed class ContainerViewModel(string key, string displayName, ObservableCollection<ItemSlotViewModel> slots)
{
    public string Key { get; } = key;
    public string DisplayName { get; } = displayName;
    public ObservableCollection<ItemSlotViewModel> Slots { get; } = slots;
}
