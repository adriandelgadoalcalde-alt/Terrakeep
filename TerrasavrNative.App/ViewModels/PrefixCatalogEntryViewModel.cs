using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App.ViewModels;

// Una entrada del catalogo completo de prefijos (vanilla o Calamity/Picaro) para el picker de
// "Elegir prefijo" - cierra el hueco #2 de la auditoria Terrasavr JS vs puerto (antes solo se
// podia usar el boton de "mejor prefijo" auto-sugerido).
public sealed class PrefixCatalogEntryViewModel(string displayName, ItemPrefix prefix, bool isCalamity)
{
    public string DisplayName { get; } = displayName;
    public ItemPrefix Prefix { get; } = prefix;
    public bool IsCalamity { get; } = isCalamity;
}
