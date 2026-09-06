using System.Text.Json;

namespace TerrasavrNative.Core.Data;

// Tooltip descriptivo real de accesorios/armaduras/etc. (texto con los porcentajes YA
// rellenados por el propio juego, ej. "Aumenta un 15% el daño cuerpo a cuerpo") - pregunta a
// Opus sobre el diseño 2-sep-2026, cuarta pasada: antes ItemStatsFormatter solo componia
// campos NUMERICOS (VanillaItemStats), asi que un accesorio sin daño/defensa/etc. no generaba
// NINGUNA linea de tooltip. Fuente real: scripts/extraer-tooltips-vanilla.py, clave real
// ItemTooltip de Terraria.Localization.Content.es-ES.Items.json (con las referencias
// {$CommonItemTooltip.X} ya resueltas en el propio script - ver ese fichero para el detalle).
// No es parte de VanillaItemStats a proposito: ese catalogo documenta "null = el juego no
// asigna ese campo numerico", una invariante distinta de "no hay tooltip descriptivo real".
public sealed class VanillaItemTooltipCatalog
{
    private readonly Dictionary<int, string> _byId;
    // Ronda de traduccion del CONTENIDO del juego (6-sep-2026): los mismos 2789 tooltips reales
    // contra `en-US` (misma clave ItemTooltip, mismas referencias {$CommonItemTooltip.X} ya
    // resueltas por el mismo script). Vacio = cargado sin la parte inglesa -> siempre español.
    private readonly Dictionary<int, string> _byIdEn = [];

    private VanillaItemTooltipCatalog(Dictionary<int, string> byId) => _byId = byId;

    public string? Get(int itemId) => Get(itemId, LocalizedContent.CurrentLanguage);

    public string? Get(int itemId, string language)
    {
        if (language == LocalizedContent.English && _byIdEn.TryGetValue(itemId, out var en)) return en;
        return _byId.TryGetValue(itemId, out var text) ? text : null;
    }

    public static VanillaItemTooltipCatalog LoadFromFile(string path, string? enPath = null)
    {
        using var stream = File.OpenRead(path);
        var catalog = LoadFromStream(stream);
        if (enPath is not null && File.Exists(enPath))
        {
            using var enStream = File.OpenRead(enPath);
            var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(enStream) ?? [];
            foreach (var (key, value) in raw) catalog._byIdEn[int.Parse(key)] = value;
        }
        return catalog;
    }

    public static VanillaItemTooltipCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidDataException("vanilla_item_tooltips.json invalido.");
        var byId = new Dictionary<int, string>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;
        return new VanillaItemTooltipCatalog(byId);
    }
}
