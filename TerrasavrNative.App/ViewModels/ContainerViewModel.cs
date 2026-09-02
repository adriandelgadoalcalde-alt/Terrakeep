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

    // MinCell real de SlotGridPanel (44 por defecto) - pedido explicito 2-sep-2026: el lateral
    // de Mascota/Montura+Tinte de la fusion de Equipamiento (5 filas x 2 grupos apiladas en
    // una columna estrecha) necesitaba scroll a 44px minimo en la resolucion real del
    // usuario ("los cuadrados... ya que ahora sale scroll y no lo queremos" - celdas mas
    // pequeñas, no aceptar el scroll). Solo estos dos contenedores lo bajan; el resto se
    // queda en 44 (el mismo suelo de legibilidad que Inventario/Almacenes).
    public double MinCell { get; init; } = 44;
}
