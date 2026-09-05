using System.Windows.Media;

namespace TerrasavrNative.App.ViewModels;

// Una entrada del catalogo completo (vanilla o Calamity) para la Libreria/Buscador. IconPath
// es una URI pack://siteoforigin real para ambos (vanilla via VanillaIconResolver, extraidos
// de items.png; Calamity via su propio icono ya copiado) - null solo para el puñado de casos
// sin icono real (ver VanillaIconResolver), donde la UI cae a un "?" de texto.
public sealed class LibraryItemViewModel(string displayName, bool isCalamity, string? iconPath, int id, string category, string? statsTooltip = null, (byte R, byte G, byte B)? rarityColor = null)
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    public string DisplayName { get; } = displayName;
    public bool IsCalamity { get; } = isCalamity;
    public string? IconPath { get; } = iconPath;
    public int Id { get; } = id;
    public string Category { get; } = category;

    // C-09 (informe de pulido final, cierra L2): plegado (minusculas + sin diacriticos) UNA
    // sola vez aqui, no en cada pulsacion - LibraryViewModel llamaba a ToLowerInvariant() sobre
    // las 8821 entradas del catalogo en CADA tecla; con esto cachea lo que ya era mas barato de
    // calcular una vez. Solo para COMPARAR - DisplayName/StatsTooltip (lo que se ve) no cambian.
    public string NameFolded { get; } = LibrarySearchGrammar.Fold(displayName);
    public string? TooltipFolded { get; } = statsTooltip is null ? null : LibrarySearchGrammar.Fold(statsTooltip);

    // Daño/defensa/etc. reales (ItemStatsFormatter) - null si el objeto no tiene ninguna
    // estadistica de combate conocida (WPF no muestra ToolTip si el valor enlazado es null).
    public string? StatsTooltip { get; } = statsTooltip;

    // Auditoria de Opus, D-3: color REAL de rareza de Terraria (VanillaRarityColorCatalog,
    // verificado contra el decompilado) - null para lo que de verdad no tiene una rareza
    // coloreada real (Calamity, o rareza 0/sin dato), cae al color de texto normal.
    public Brush? RarityBrush { get; } = rarityColor is { } c ? new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B)) : null;
}
