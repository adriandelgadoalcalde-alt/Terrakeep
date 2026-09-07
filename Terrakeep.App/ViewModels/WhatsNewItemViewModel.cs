using Terrakeep.App.Services;
using Terrakeep.Core.Data;

namespace Terrakeep.App.ViewModels;

// N-d (segunda auditoria de Opus, Fable): "los 'Objetos nuevos' son texto, no sprites - toda la
// app enseña sprites, aqui no, y el resolvedor de iconos ya esta a mano". WhatsNewItem.Key es
// el nombre interno real (no un id numerico, ver el comentario real de la clase) - se resuelve
// aqui contra VanillaItemCatalog (mismo mecanismo real ya usado para el Pid de Investigacion).
// IconPath queda null (fallback real "?" del icono fantasma, ya existente) si el nombre interno
// no se reconoce - "lo que no se encuentra no se inventa", mismo criterio de siempre.
// Ronda de idioma del 6-sep-2026: DisplayName se fijaba con WhatsNewItem.DisplayName, que
// devuelve SIEMPRE el nombre español aunque el JSON traiga tambien el ingles - la rejilla de
// "Objetos nuevos" (y su tooltip) se quedaba en español con la app en ingles.
public sealed class WhatsNewItemViewModel : LocalizedContentViewModel
{
    private readonly WhatsNewItem _item;

    public string DisplayName => _item.NameFor(Idioma);
    public string? IconPath { get; }
    // C-17 (informe de pulido final, cierra la otra mitad de N1): misma llamada EXACTA que
    // LibraryViewModel (Format(isCalamity, id, catalogs)) - "como en calamity mod y tmodloader"
    // (sprite, nombre con color de rareza, daño/DPS, defensa, critico, tooltip real), sin
    // escribir ni un texto nuevo. Null si el nombre interno no se reconoce (mismo criterio que
    // IconPath) o si el objeto no tiene ninguna estadistica/tooltip real conocido.
    // Ronda de idioma del 6-sep-2026: guarda los DATOS (ItemStatsInfo) y redacta al leer, para
    // que cambiar de idioma en vivo tambien traduzca este tooltip - ver ItemStatsInfo.cs.
    public string? StatsTooltip => Services.ItemStatsTextBuilder.Build(_stats);

    private readonly ItemStatsInfo? _stats;

    private WhatsNewItemViewModel(WhatsNewItem item, string? iconPath, ItemStatsInfo? stats)
    {
        _item = item;
        IconPath = iconPath;
        _stats = stats;
    }

    protected override void RefrescarTextos()
    {
        Avisar(nameof(DisplayName));
        Avisar(nameof(StatsTooltip));
    }

    // C-16 (informe de pulido final, cierra media N1): whatsNewIds es el catalogo SEPARADO de
    // solo lectura para objetos 1.4.5+ (por encima del maximo real de vanillaCatalog) - se
    // consulta SOLO como respaldo, nunca en primer lugar (un objeto YA conocido por el catalogo
    // real sigue resolviendose igual que siempre). Ver WhatsNewItemIdCatalog: nunca se usa fuera
    // de aqui, en particular nunca para colocar nada en un slot real del personaje.
    public static WhatsNewItemViewModel ForVanilla(WhatsNewItem item, VanillaItemCatalog vanillaCatalog, WhatsNewItemIdCatalog whatsNewIds, ItemTooltipCatalogs catalogs)
    {
        int? id = item.Key != null ? vanillaCatalog.GetIdByKey(item.Key) ?? whatsNewIds.GetIdByKey(item.Key) : null;
        return new(item, id is int i ? VanillaIconResolver.GetIconPath(i) : null,
            id is int i2 ? ItemStatsFormatter.Describe(false, i2, catalogs) : null);
    }

    // Pedido explicito del usuario (2-sep-2026): gemelo real de arriba, para la pestaña de
    // tModLoader/Calamity Mod - WhatsNewItem.Key es el nombre interno REAL de la clase
    // decompilada (confirmado a mano contra calamitymod.wiki.gg antes de escribir
    // calamity_updates.json, ver ese fichero), resuelto igual contra CalamityCatalog
    // (ByModAndInternal, mismo mecanismo ya usado por PrefixSuggester/etc). "CalamityMod" es el
    // unico mod real que aparece en catalog.json (ver CalamityCatalogEntry.Mod), no hace falta
    // parametrizarlo. Muchos objetos NUEVOS reales de una version reciente (NPCs sobre todo,
    // ej. Horrible Hog/Divine Swine/Shady Salesman) todavia no estan en el catalogo local
    // (extraido de una version anterior del .tmod) - IconPath se queda null en ese caso, "lo
    // que no se encuentra no se inventa", mismo criterio real que la version vanilla de arriba.
    public static WhatsNewItemViewModel ForCalamity(WhatsNewItem item, CalamityCatalog calamityCatalog, ItemTooltipCatalogs catalogs)
    {
        var entry = item.Key != null ? calamityCatalog.ByModAndInternal("CalamityMod", item.Key) : null;
        string? iconPath = entry?.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + entry.Icon : null;
        var stats = entry != null ? ItemStatsFormatter.Describe(true, entry.SyntheticId, catalogs) : null;
        return new(item, iconPath, stats);
    }
}
