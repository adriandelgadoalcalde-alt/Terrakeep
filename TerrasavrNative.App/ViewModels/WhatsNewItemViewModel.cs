using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

// N-d (segunda auditoria de Opus, Fable): "los 'Objetos nuevos' son texto, no sprites - toda la
// app enseña sprites, aqui no, y el resolvedor de iconos ya esta a mano". WhatsNewItem.Key es
// el nombre interno real (no un id numerico, ver el comentario real de la clase) - se resuelve
// aqui contra VanillaItemCatalog (mismo mecanismo real ya usado para el Pid de Investigacion).
// IconPath queda null (fallback real "?" del icono fantasma, ya existente) si el nombre interno
// no se reconoce - "lo que no se encuentra no se inventa", mismo criterio de siempre.
public sealed class WhatsNewItemViewModel
{
    public string DisplayName { get; }
    public string? IconPath { get; }

    private WhatsNewItemViewModel(string displayName, string? iconPath)
    {
        DisplayName = displayName;
        IconPath = iconPath;
    }

    public static WhatsNewItemViewModel ForVanilla(WhatsNewItem item, VanillaItemCatalog vanillaCatalog) =>
        new(item.DisplayName, item.Key != null && vanillaCatalog.GetIdByKey(item.Key) is int id
            ? VanillaIconResolver.GetIconPath(id)
            : null);

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
    public static WhatsNewItemViewModel ForCalamity(WhatsNewItem item, CalamityCatalog calamityCatalog) =>
        new(item.DisplayName, item.Key != null && calamityCatalog.ByModAndInternal("CalamityMod", item.Key) is { } entry
            ? entry.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + entry.Icon : null
            : null);
}
