namespace TerrasavrNative.App.ViewModels;

// Una entrada de equipo dentro de una build (armadura/arma/accesorio), ya resuelta a nombre +
// icono real para mostrar - BuildItemRef (Core) solo trae el pid/nombre/prefijo en crudo.
public sealed class BuildItemRowViewModel(string displayName, string? prefixText, string? iconPath, bool isCalamity)
{
    public string DisplayName { get; } = displayName;
    public string? PrefixText { get; } = prefixText;
    public string? IconPath { get; } = iconPath;
    public bool IsCalamity { get; } = isCalamity;
}
