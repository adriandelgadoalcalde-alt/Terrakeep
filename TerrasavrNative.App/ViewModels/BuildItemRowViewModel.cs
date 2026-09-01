namespace TerrasavrNative.App.ViewModels;

// Una entrada de equipo dentro de una build (armadura/arma/accesorio), ya resuelta a nombre +
// icono real para mostrar - BuildItemRef (Core) solo trae el pid/nombre/prefijo en crudo.
// StatsTooltip cierra el bug real reportado 1-sep-2026 ("no salen [tooltips de estadisticas]
// en Builds... si lo hace en Terrasavr") - antes esta clase ni siquiera guardaba el id
// numerico del objeto, imposible calcular nada.
public sealed class BuildItemRowViewModel(string displayName, string? prefixText, string? iconPath, bool isCalamity, string? statsTooltip)
{
    public string DisplayName { get; } = displayName;
    public string? PrefixText { get; } = prefixText;
    public string? IconPath { get; } = iconPath;
    public bool IsCalamity { get; } = isCalamity;
    public string? StatsTooltip { get; } = statsTooltip;
}
