using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// Una entrada de equipo dentro de una build (armadura/arma/accesorio), ya resuelta a nombre +
// icono real para mostrar - BuildItemRef (Core) solo trae el pid/nombre/prefijo en crudo.
// StatsTooltip cierra el bug real reportado 1-sep-2026 ("no salen [tooltips de estadisticas]
// en Builds... si lo hace en Terrasavr") - antes esta clase ni siquiera guardaba el id
// numerico del objeto, imposible calcular nada.
// Ronda de idioma del 6-sep-2026: recibia el nombre YA resuelto como string, y quien lo resolvia
// llamaba a BuildItemRef.DisplayName, que devuelve siempre el español aunque el propio
// builds.json traiga tambien el ingles - toda la pestaña Builds se quedaba sin traducir.
public sealed partial class BuildItemRowViewModel : ObservableObject
{
    private readonly BuildItemRef _source;

    // Ronda de idioma del 6-sep-2026: `statsTooltip` era un string YA redactado (en español a
    // fuego) y se quedaba congelado en ese idioma - ahora entran los DATOS (ItemStatsInfo) y la
    // frase se redacta al leerla, ver ItemStatsInfo.cs.
    public BuildItemRowViewModel(BuildItemRef source, string? prefixText, string? iconPath, bool isCalamity, ItemStatsInfo? stats, int itemId)
    {
        _source = source;
        PrefixText = prefixText;
        IconPath = iconPath;
        IsCalamity = isCalamity;
        _stats = stats;
        ItemId = itemId;
        PropertyChangedEventManager.AddHandler(Services.LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
    }

    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    public string DisplayName => _source.NameFor(Services.LocalizationService.Instance.Language);
    public string? PrefixText { get; }
    public string? IconPath { get; }
    public bool IsCalamity { get; }

    private readonly ItemStatsInfo? _stats;
    public string? StatsTooltip => Services.ItemStatsTextBuilder.Build(_stats);

    private void OnIdiomaCambiado(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(StatsTooltip));
    }
    // Id real vanilla o sintetico de Calamity - 0 si el pid del build no se resolvio (mismo
    // caso real que AutoEquipService cuenta como "sin resolver", ver Bd-c). Usado solo para
    // calcular IsOwned, ver BuildsViewModel.RefreshOwnership.
    public int ItemId { get; }

    // Bd-d (segunda auditoria de Opus, Fable): "marcar lo que ya se posee" - true si este
    // objeto (por id real) aparece en algun contenedor real del personaje cargado ahora mismo
    // (Inventario/Almacenes/Equipamiento de los 4 loadouts), no solo "puesto". Se recalcula al
    // cargar personaje y al entrar en la pestaña Builds (ver BuildsViewModel.RefreshOwnership) -
    // no en cada tecla de una edicion en Objetos, seria recalcular cientos de filas por cada
    // pulsacion sin necesidad real (nadie mira Builds mientras edita Objetos a la vez).
    [ObservableProperty] private bool _isOwned;
}
