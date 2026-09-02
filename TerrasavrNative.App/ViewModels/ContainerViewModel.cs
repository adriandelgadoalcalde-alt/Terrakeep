using System.Collections.ObjectModel;

namespace TerrasavrNative.App.ViewModels;

// Un grupo de slots con nombre para mostrar (una pestaña/seccion: Inventario, Banco...).
public sealed class ContainerViewModel(string key, string displayName, ObservableCollection<ItemSlotViewModel> slots)
{
    public string Key { get; } = key;
    public string DisplayName { get; } = displayName;
    public ObservableCollection<ItemSlotViewModel> Slots { get; } = slots;

    // Nº de columnas reales de la cuadricula compacta (SlotGridPanel, ver
    // TerrasavrNative.App/Controls/SlotGridPanel.cs) - 10 por defecto (la propia rejilla de
    // Terraria para Inventario/Banco/Caja fuerte/Fragua/Boveda), pero Equipamiento necesita 5
    // (PlrLoadout.Items/Social/Dyes son siempre 10 slots reales en forma 5x2, no 10x1 - ver
    // EquipmentGroupViewModel.AddSlotSet).
    public int Columns { get; init; } = 10;
}
