using CommunityToolkit.Mvvm.ComponentModel;

namespace TerrasavrNative.App.ViewModels;

// Una entrada de equipo dentro de una build (armadura/arma/accesorio), ya resuelta a nombre +
// icono real para mostrar - BuildItemRef (Core) solo trae el pid/nombre/prefijo en crudo.
// StatsTooltip cierra el bug real reportado 1-sep-2026 ("no salen [tooltips de estadisticas]
// en Builds... si lo hace en Terrasavr") - antes esta clase ni siquiera guardaba el id
// numerico del objeto, imposible calcular nada.
public sealed partial class BuildItemRowViewModel(string displayName, string? prefixText, string? iconPath, bool isCalamity, string? statsTooltip, int itemId) : ObservableObject
{
    public string DisplayName { get; } = displayName;
    public string? PrefixText { get; } = prefixText;
    public string? IconPath { get; } = iconPath;
    public bool IsCalamity { get; } = isCalamity;
    public string? StatsTooltip { get; } = statsTooltip;
    // Id real vanilla o sintetico de Calamity - 0 si el pid del build no se resolvio (mismo
    // caso real que AutoEquipService cuenta como "sin resolver", ver Bd-c). Usado solo para
    // calcular IsOwned, ver BuildsViewModel.RefreshOwnership.
    public int ItemId { get; } = itemId;

    // Bd-d (segunda auditoria de Opus, Fable): "marcar lo que ya se posee" - true si este
    // objeto (por id real) aparece en algun contenedor real del personaje cargado ahora mismo
    // (Inventario/Almacenes/Equipamiento de los 4 loadouts), no solo "puesto". Se recalcula al
    // cargar personaje y al entrar en la pestaña Builds (ver BuildsViewModel.RefreshOwnership) -
    // no en cada tecla de una edicion en Objetos, seria recalcular cientos de filas por cada
    // pulsacion sin necesidad real (nadie mira Builds mientras edita Objetos a la vez).
    [ObservableProperty] private bool _isOwned;
}
