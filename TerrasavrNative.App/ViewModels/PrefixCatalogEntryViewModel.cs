using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App.ViewModels;

// Una entrada de prefijo elegible para el objeto seleccionado, dentro de un grupo del panel
// "Editar" (btPrefix reales) - cierra el hueco de la auditoria Terrasavr JS vs puerto (antes
// solo se podia usar el boton de "mejor prefijo" auto-sugerido, o una lista plana sin filtrar
// por tipo de objeto).
public sealed class PrefixCatalogEntryViewModel(string displayName, ItemPrefix prefix, bool isCalamity, bool isCurrent = false, string? effectDescription = null)
{
    public string DisplayName { get; } = displayName;
    public ItemPrefix Prefix { get; } = prefix;
    public bool IsCalamity { get; } = isCalamity;
    // Si es el prefijo YA puesto en el objeto seleccionado - se recalcula reconstruyendo la
    // coleccion entera cada vez (son <=19 entradas por grupo, gratis), no con binding en vivo.
    public bool IsCurrent { get; } = isCurrent;

    // Auditoria de Opus (octava pasada, D-6): "una lista de 20 nombres opacos" - el efecto real
    // ya se sabe calcular (PrefixEffectCatalog, numeros reales extraidos de Item.cs decompilado,
    // ya usado en el tooltip del propio objeto) pero antes no se mostraba aqui, en el propio
    // boton donde se elige. Null para los prefijos de Calamity (esos numeros no se investigaron
    // esta pasada, ver ItemStatsFormatter) - WPF no muestra ToolTip si el valor enlazado es null.
    public string? EffectDescription { get; } = effectDescription;
}
