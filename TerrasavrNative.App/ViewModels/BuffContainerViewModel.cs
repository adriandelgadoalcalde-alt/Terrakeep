using System.Collections.ObjectModel;

namespace TerrasavrNative.App.ViewModels;

// Contenedor de buffs con nombre para mostrar - gemelo de ContainerViewModel (objetos) pero
// para BuffSlotViewModel. No se generaliza ContainerViewModel en si (esta tipado a
// ItemSlotViewModel y lo consumen AutoEquip/SyncEditsBackToMerged/StorageGroupViewModel/
// EquipmentGroupViewModel - hacerlo generico seria riesgo real por cero beneficio, pregunta a
// Opus sobre el diseño 2-sep-2026, cuarta pasada).
public sealed class BuffContainerViewModel(string displayName, ObservableCollection<BuffSlotViewModel> slots, int columns)
{
    public string DisplayName { get; } = displayName;
    public ObservableCollection<BuffSlotViewModel> Slots { get; } = slots;
    public int Columns { get; } = columns;
}
