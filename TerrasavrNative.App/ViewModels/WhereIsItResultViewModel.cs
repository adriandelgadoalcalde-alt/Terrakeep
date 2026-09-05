namespace TerrasavrNative.App.ViewModels;

// H5-05 (quinta auditoria de Opus): "¿Dónde lo tengo?" - una fila real del resultado, icono +
// nombre + contenedor + slot (los mismos datos que ya usa el panel Editar compartido,
// ItemSlotViewModel.ContainerName/SlotIndex - nada nuevo que calcular). ContainerKey (la clave
// REAL del ContainerViewModel dueño, "bank"/"loadout1Social"/...) es solo para la navegacion
// real (MainViewModel.NavigateToWhereIsItResult) - nunca se muestra, ContainerName ya es el
// texto legible real.
public sealed class WhereIsItResultViewModel(ItemSlotViewModel slot, string containerKey)
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    public ItemSlotViewModel Slot { get; } = slot;
    public string ContainerKey { get; } = containerKey;
    public string DisplayName => Slot.DisplayName;
    public string? IconPath => Slot.IconPath;
    public string ContainerName => Slot.ContainerName;
    // 1-based para mostrar ("slot 5", no "slot 4") - mismo criterio real que cualquier otro
    // numero de cara al usuario en la app (ej. "Accesorio 1..7").
    public int SlotNumber => Slot.SlotIndex + 1;
}
