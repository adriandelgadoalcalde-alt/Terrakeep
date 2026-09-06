using System.Text.Json;
using System.Text.Json.Serialization;
using TerrasavrNative.Core.Calamity;

namespace TerrasavrNative.Core.Data;

public sealed class CalamityBuffEntryData
{
    [JsonPropertyName("internal")] public required string Internal { get; init; }
    [JsonPropertyName("mod")] public required string Mod { get; init; }
    [JsonPropertyName("category")] public string? Category { get; init; }
    [JsonPropertyName("displayName_es")] public string? DisplayNameEs { get; init; }
    [JsonPropertyName("displayName_fallback")] public string? DisplayNameFallback { get; init; }
    [JsonPropertyName("icon")] public string? Icon { get; init; }
}

// Una entrada del catalogo de buffs con su id sintetico ya resuelto (BuffIdBase + indice en
// el array de buffs.json) - mismo esquema secuencial que CalamityCatalog para objetos.
public sealed class CalamityBuffEntry(CalamityBuffEntryData data, int syntheticId, string? description, bool isDebuff)
{
    public int SyntheticId { get; } = syntheticId;
    public string Internal => data.Internal;
    public string Mod => data.Mod;
    // `displayName_fallback` es el nombre real INGLES del mod (del hjson en-US del .tmod real,
    // 305 de 305) - ronda de traduccion del CONTENIDO del juego (6-sep-2026): antes solo se
    // usaba como ultimo recurso, ahora es la cara inglesa de verdad de este catalogo.
    public string DisplayName => DisplayNameFor(LocalizedContent.CurrentLanguage);

    public string DisplayNameFor(string language)
    {
        string picked = LocalizedContent.Pick(data.DisplayNameEs, data.DisplayNameFallback, language);
        return string.IsNullOrWhiteSpace(picked) ? data.Internal : picked;
    }
    public string? Icon => data.Icon;
    // Categoria real (calamity/buffs.json, campo "category" - Summon/StatBuffs/
    // DamageOverTime/Pets/Alcohol/StatDebuffs/Potions/Mounts/Placeables) - usada por
    // BuffLibraryTreeBuilder para agrupar la carpeta "Calamity (mod)" de la Libreria de buffs,
    // mismo patron que CalamityCatalogEntry.Category para objetos.
    public string? Category => data.Category;
    // Descripcion real EN INGLES (scripts/extraer-descripciones-buffs-calamity.js, del
    // .tmod real instalado) - esta instalacion de Calamity no trae es-ES, no se inventa una
    // traduccion. Null si de verdad no hay ninguna.
    public string? Description { get; } = description;
    // H6-12 (sexta auditoria de Opus, "no hay forma de distinguir buff de debuff"): real, de
    // `Main.debuff[base.Type] = true` en el propio SetStaticDefaults() del ModBuff real (ver
    // scripts/extraer-debuffs-calamity.js) - no una lista a mano ni una heuristica sobre el
    // nombre/categoria (DamageOverTime NO es lo mismo que "es debuff": todas sus 39 entradas
    // SI lo son, pero StatDebuffs/StatBuffs mezclan positivos y negativos de verdad).
    public bool IsDebuff { get; } = isDebuff;
}

// Los buffs reales de Calamity (calamity/buffs.json) - usado para fusionar/sincronizar
// modBuffs del .tplr (ver CalamityCharacterSync). Mismo patron que CalamityCatalog.
public sealed class CalamityBuffCatalog
{
    private readonly List<CalamityBuffEntry> _entries;
    private readonly Dictionary<int, CalamityBuffEntry> _bySyntheticId;
    private readonly Dictionary<(string Mod, string Internal), CalamityBuffEntry> _byModInternal;

    private CalamityBuffCatalog(List<CalamityBuffEntry> entries)
    {
        _entries = entries;
        _bySyntheticId = entries.ToDictionary(e => e.SyntheticId);
        _byModInternal = entries.ToDictionary(e => (e.Mod, e.Internal));
    }

    public IReadOnlyList<CalamityBuffEntry> Entries => _entries;

    public CalamityBuffEntry? BySyntheticId(int id) =>
        _bySyntheticId.TryGetValue(id, out var e) ? e : null;

    public CalamityBuffEntry? ByModAndInternal(string mod, string internalName) =>
        _byModInternal.TryGetValue((mod, internalName), out var e) ? e : null;

    public static CalamityBuffCatalog LoadFromFile(string path, string descriptionsPath, string debuffsPath)
    {
        using var stream = File.OpenRead(path);
        using var descStream = File.OpenRead(descriptionsPath);
        using var debuffsStream = File.OpenRead(debuffsPath);
        return LoadFromStream(stream, descStream, debuffsStream);
    }

    public static CalamityBuffCatalog LoadFromStream(Stream stream, Stream descriptionsStream, Stream debuffsStream)
    {
        var raw = JsonSerializer.Deserialize<List<CalamityBuffEntryData>>(stream)
            ?? throw new InvalidDataException("buffs.json no contiene un array valido.");
        var descriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionsStream)
            ?? throw new InvalidDataException("calamity_buff_descriptions.json invalido.");
        // H6-12: real (scripts/extraer-debuffs-calamity.js) - solo trae los internal name que
        // SI son debuffs (Main.debuff[base.Type]=true en su propio ModBuff real); ausencia =
        // buff normal, mismo valor por defecto que Terraria real (Main.debuff[] entero a false).
        var debuffs = JsonSerializer.Deserialize<Dictionary<string, bool>>(debuffsStream)
            ?? throw new InvalidDataException("buff_debuffs.json invalido.");

        // El orden del array IMPORTA - determina el id sintetico de cada buff. No reordenar.
        var entries = new List<CalamityBuffEntry>(raw.Count);
        for (int i = 0; i < raw.Count; i++)
        {
            descriptions.TryGetValue(raw[i].Internal, out var desc);
            bool isDebuff = debuffs.TryGetValue(raw[i].Internal, out var d) && d;
            entries.Add(new CalamityBuffEntry(raw[i], CalamityIds.BuffIdBase + i, desc, isDebuff));
        }

        return new CalamityBuffCatalog(entries);
    }
}
