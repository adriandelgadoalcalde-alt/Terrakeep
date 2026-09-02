using System.Collections.ObjectModel;

namespace TerrasavrNative.App.ViewModels;

// Un grupo de slots con nombre para mostrar (una pestaña/seccion: Inventario, Banco...).
public sealed class ContainerViewModel(string key, string displayName, ObservableCollection<ItemSlotViewModel> slots)
{
    public string Key { get; } = key;
    public string DisplayName { get; } = displayName;
    public ObservableCollection<ItemSlotViewModel> Slots { get; } = slots;

    // Nº de columnas reales de la cuadricula (pregunta a Opus sobre el diseño, 2-sep-2026 -
    // segunda consulta): 10 por defecto (la propia rejilla de Terraria para Inventario/Banco/
    // Caja fuerte/Fragua/Boveda), pero Equipamiento necesita 5 (PlrLoadout.Items/Social/Dyes
    // son siempre 10 slots reales en forma 5x2, no 10x1) - sin esto GridWidth daria 1600 para
    // un contenedor de 10, y el Viewbox lo escalaria a una tira larga y fina en vez de un
    // bloque 5x2 compacto.
    public int Columns { get; init; } = 10;

    // Bug real encontrado 2-sep-2026 (pregunta a Opus sobre el diseño): el WrapPanel de
    // ContainerTabTemplate tenia un Width FIJO de 1600 (10 columnas) para TODOS los
    // contenedores, incluidos los pequeños (Monedas/Municion=4, Mascota/Montura/Gancho/
    // Tintes=5) - un WrapPanel con Width explicito reporta ESE ancho al medir aunque tenga
    // pocos hijos, asi que el Viewbox los escalaba igual de pequeño que si tuvieran 50 items
    // reales, dejando el 60% del ancho vacio. Ancho real = nº de columnas x huella real de
    // ItemSlotCard (148 + 6+6 de margen = 160px, ver Theme.xaml).
    public int GridWidth => Math.Min(Columns, Math.Max(1, Slots.Count)) * 160;
}
