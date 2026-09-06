using System.Text.Json;
using System.Text.Json.Serialization;
using TerrasavrNative.Core.Calamity;

namespace TerrasavrNative.Core.Data;

public sealed class CalamityItemStats
{
    [JsonPropertyName("damage")] public int? Damage { get; init; }
    [JsonPropertyName("useTime")] public int? UseTime { get; init; }
    [JsonPropertyName("crit")] public int? Crit { get; init; }
    [JsonPropertyName("knockBack")] public double? KnockBack { get; init; }
    [JsonPropertyName("mana")] public int? Mana { get; init; }
    [JsonPropertyName("damageType")] public string? DamageType { get; init; }
    // Defensa real (armaduras y algunos accesorios) - antes ausente del todo en catalog.json
    // (pedido explicito del usuario: "las armaduras de calamity no dicen especificaciones
    // cuando pasas el raton"). Extraida de verdad de Item.defense en el SetDefaults() real de
    // cada objeto, ver scripts/extraer-defensa-calamity.js - null para lo que de verdad no
    // tiene (armaduras vanity, la mayoria de accesorios), nunca inventado ni puesto a 0.
    [JsonPropertyName("defense")] public int? Defense { get; init; }
}

public sealed class CalamityCatalogEntryData
{
    [JsonPropertyName("internal")] public required string Internal { get; init; }
    [JsonPropertyName("mod")] public required string Mod { get; init; }
    [JsonPropertyName("category")] public required string Category { get; init; }
    [JsonPropertyName("displayName_es")] public string? DisplayNameEs { get; init; }
    [JsonPropertyName("displayName_fallback")] public string? DisplayNameFallback { get; init; }
    [JsonPropertyName("icon")] public string? Icon { get; init; }
    [JsonPropertyName("stats")] public CalamityItemStats? Stats { get; init; }
    // H3-11 (tercera auditoria de Opus, Fable): "Head"/"Body"/"Legs" real - la parte del cuerpo
    // que ocupa esta pieza de armadura, extraida del atributo real `[AutoloadEquip(new
    // EquipType[] { EquipType.X })]` de cada clase (tModLoader moderno, ver
    // scripts/extraer-slot-armadura-calamity.js) - null para lo que no es una pieza de
    // armadura de cuerpo real (accesorios, armas, el resto de categorias).
    [JsonPropertyName("equipSlot")] public string? EquipSlot { get; init; }
    // Bono de set completo real (pedido explicito del usuario tras el arreglo de defensa: "la
    // bonificacion por el set no [aparece]") - texto real resuelto del sistema de plantillas de
    // localizacion de tModLoader/Calamity (referencias {$Clave@N} anidadas, ver
    // scripts/extraer-bonos-set-calamity.js) y traducido a mano donde Calamity solo trae texto
    // en ingles (las partes que coinciden con CommonItemTooltip.* vienen YA en español oficial
    // de tModLoader). Mismo texto en las 2-3 piezas de un mismo set, igual que el juego real.
    [JsonPropertyName("setBonus")] public string? SetBonus { get; init; }
    // Ronda de traduccion del CONTENIDO del juego (6-sep-2026): el MISMO bono, resuelto por el
    // mismo script contra la localizacion `en_US` de tModLoader en vez de la `es_ES` -> texto
    // 100% ingles real del mod, sin nada traducido a mano (esta instalacion de Calamity solo
    // trae en-US, o sea que en ingles la fuente es literal). 69 de 69 sets reales, ver
    // `node scripts/extraer-bonos-set-calamity.js en` + aplicar-bonos-set-calamity.js.
    [JsonPropertyName("setBonus_en")] public string? SetBonusEn { get; init; }
}

// Una entrada del catalogo con su id sintetico ya resuelto (ItemIdBase + indice en el array
// de catalog.json - el mismo esquema secuencial que usa la capa JS, "se calculan en caliente
// por posicion en el array, nunca se persisten en ningun archivo").
public sealed class CalamityCatalogEntry(CalamityCatalogEntryData data, int syntheticId)
{
    public int SyntheticId { get; } = syntheticId;
    public string Internal => data.Internal;
    public string Mod => data.Mod;
    public string Category => data.Category;
    // `displayName_fallback` es el nombre real INGLES del mod (del hjson en-US del .tmod, 2709
    // de 2709 entradas) - antes solo se usaba como ultimo recurso cuando faltaba el español;
    // ahora es la cara inglesa de verdad de este catalogo.
    public string DisplayName => DisplayNameFor(LocalizedContent.CurrentLanguage);

    public string DisplayNameFor(string language)
    {
        string picked = LocalizedContent.Pick(data.DisplayNameEs, data.DisplayNameFallback, language);
        return string.IsNullOrWhiteSpace(picked) ? data.Internal : picked;
    }

    public string? Icon => data.Icon;
    public CalamityItemStats? Stats => data.Stats;
    public string? SetBonus => SetBonusFor(LocalizedContent.CurrentLanguage);

    public string? SetBonusFor(string language)
    {
        if (data.SetBonus is null && data.SetBonusEn is null) return null;
        string picked = LocalizedContent.Pick(data.SetBonus, data.SetBonusEn, language);
        return string.IsNullOrWhiteSpace(picked) ? null : picked;
    }
    public string? EquipSlot => data.EquipSlot;
}

public sealed class CalamityCatalog
{
    private readonly List<CalamityCatalogEntry> _entries;
    private readonly Dictionary<int, CalamityCatalogEntry> _bySyntheticId;
    private readonly Dictionary<(string Mod, string Internal), CalamityCatalogEntry> _byModInternal;

    private CalamityCatalog(List<CalamityCatalogEntry> entries)
    {
        _entries = entries;
        _bySyntheticId = entries.ToDictionary(e => e.SyntheticId);
        _byModInternal = entries.ToDictionary(e => (e.Mod, e.Internal));
    }

    public IReadOnlyList<CalamityCatalogEntry> Entries => _entries;

    public CalamityCatalogEntry? BySyntheticId(int id) =>
        _bySyntheticId.TryGetValue(id, out var e) ? e : null;

    public CalamityCatalogEntry? ByModAndInternal(string mod, string internalName) =>
        _byModInternal.TryGetValue((mod, internalName), out var e) ? e : null;

    public static CalamityCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static CalamityCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<List<CalamityCatalogEntryData>>(stream)
            ?? throw new InvalidDataException("catalog.json no contiene un array valido.");

        // El orden del array IMPORTA - determina el id sintetico de cada objeto. No reordenar.
        var entries = new List<CalamityCatalogEntry>(raw.Count);
        for (int i = 0; i < raw.Count; i++)
            entries.Add(new CalamityCatalogEntry(raw[i], CalamityIds.ItemIdBase + i));

        return new CalamityCatalog(entries);
    }
}
