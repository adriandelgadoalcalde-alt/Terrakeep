using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App.ViewModels;

// Una entrada de prefijo elegible para el objeto seleccionado, dentro de un grupo del panel
// "Editar" (btPrefix reales) - cierra el hueco de la auditoria Terrasavr JS vs puerto (antes
// solo se podia usar el boton de "mejor prefijo" auto-sugerido, o una lista plana sin filtrar
// por tipo de objeto).
public sealed class PrefixCatalogEntryViewModel(string displayName, ItemPrefix prefix, bool isCalamity, bool isCurrent = false)
{
    public string DisplayName { get; } = displayName;
    public ItemPrefix Prefix { get; } = prefix;
    public bool IsCalamity { get; } = isCalamity;
    // Si es el prefijo YA puesto en el objeto seleccionado - se recalcula reconstruyendo la
    // coleccion entera cada vez (son <=19 entradas por grupo, gratis), no con binding en vivo.
    public bool IsCurrent { get; } = isCurrent;
}
