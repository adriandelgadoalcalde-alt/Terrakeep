using System.ComponentModel;
using System.Windows.Media;
using Terrakeep.App.Services;
using Terrakeep.Core.Data;

namespace Terrakeep.App.ViewModels;

// Una entrada del catalogo completo (vanilla o Calamity) para la Libreria/Buscador. IconPath
// es una URI pack://siteoforigin real para ambos (vanilla via VanillaIconResolver, extraidos
// de items.png; Calamity via su propio icono ya copiado) - null solo para el puñado de casos
// sin icono real (ver VanillaIconResolver), donde la UI cae a un "?" de texto.
//
// Ronda de idioma del 6-sep-2026: recibia el tooltip de estadisticas como un `string` YA
// redactado (en español a fuego, ver ItemStatsFormatter) y se quedaba congelado en ese idioma.
// Ahora recibe los DATOS (ItemStatsInfo, sin idioma) y redacta al leer. Implementa
// INotifyPropertyChanged solo para eso: la suscripcion al servicio de idioma NO se hace aqui
// (son 8821 entradas reales, una suscripcion por entrada seria un derroche), la hace
// LibraryViewModel una sola vez y reparte - ver RefrescarIdioma.
//
// Ronda de traduccion del CONTENIDO del juego (6-sep-2026): `DisplayName` era un valor fijo del
// constructor - el nombre del OBJETO se quedaba en español aunque la interfaz estuviera en
// ingles. Ahora entra tambien `displayNameEn` (nombre real del juego en ingles) y se elige al
// leer, igual que ya se hacia con el tooltip.
public sealed class LibraryItemViewModel(string displayName, bool isCalamity, string? iconPath, int id, string category, ItemStatsInfo? stats = null, (byte R, byte G, byte B)? rarityColor = null, string? displayNameEn = null)
    : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public LocalizationService Loc => LocalizationService.Instance;

    public string DisplayName => LocalizedContent.Pick(displayName, displayNameEn);
    public bool IsCalamity { get; } = isCalamity;
    public string? IconPath { get; } = iconPath;
    public int Id { get; } = id;
    public string Category { get; } = category;

    // C-09 (informe de pulido final, cierra L2): plegado (minusculas + sin diacriticos) UNA
    // sola vez aqui, no en cada pulsacion - LibraryViewModel llamaba a ToLowerInvariant() sobre
    // las 8821 entradas del catalogo en CADA tecla; con esto cachea lo que ya era mas barato de
    // calcular una vez. Solo para COMPARAR - DisplayName/StatsTooltip (lo que se ve) no cambian.
    // Ronda de traduccion del CONTENIDO del juego: ahora el nombre cambia con el idioma, asi
    // que el plegado se recuerda POR IDIOMA (misma tecnica que TooltipFolded justo debajo) -
    // sigue sin plegarse nada en cada pulsacion, pero buscar "iron pickaxe" con la app en
    // ingles encuentra de verdad lo que se ve en pantalla.
    private string? _nameFolded;
    private string? _nameFoldedLang;
    public string NameFolded
    {
        get
        {
            string idioma = LocalizationService.Instance.Language;
            if (_nameFoldedLang != idioma)
            {
                _nameFolded = LibrarySearchGrammar.Fold(DisplayName);
                _nameFoldedLang = idioma;
            }
            return _nameFolded!;
        }
    }

    // Misma idea que NameFolded, pero el texto del tooltip SI cambia con el idioma (busqueda
    // ".texto", ver LibrarySearchGrammar) - se pliega una vez POR IDIOMA y se recuerda cual
    // era, en vez de una sola vez para siempre: sigue sin plegarse nada en cada pulsacion.
    private string? _tooltipFolded;
    private string? _tooltipFoldedLang;
    public string? TooltipFolded
    {
        get
        {
            string idioma = LocalizationService.Instance.Language;
            if (_tooltipFoldedLang != idioma)
            {
                string? texto = StatsTooltip;
                _tooltipFolded = texto is null ? null : LibrarySearchGrammar.Fold(texto);
                _tooltipFoldedLang = idioma;
            }
            return _tooltipFolded;
        }
    }

    // Daño/defensa/etc. reales, ya redactados en el idioma activo (ItemStatsFormatter calcula,
    // ItemStatsTextBuilder redacta) - null si el objeto no tiene ninguna estadistica de combate
    // conocida (WPF no muestra ToolTip si el valor enlazado es null).
    public string? StatsTooltip => ItemStatsTextBuilder.Build(stats);

    // Auditoria de Opus, D-3: color REAL de rareza de Terraria (VanillaRarityColorCatalog,
    // verificado contra el decompilado) - null para lo que de verdad no tiene una rareza
    // coloreada real (Calamity, o rareza 0/sin dato), cae al color de texto normal.
    public Brush? RarityBrush { get; } = rarityColor is { } c ? new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B)) : null;

    // Lo llama LibraryViewModel al cambiar el idioma (una unica suscripcion para las 8821
    // entradas). No recalcula nada aqui: solo avisa de que el texto hay que volver a leerlo.
    public void RefrescarIdioma()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatsTooltip)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
    }
}
