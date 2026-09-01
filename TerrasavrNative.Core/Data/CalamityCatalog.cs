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
    public string DisplayName => data.DisplayNameEs ?? data.DisplayNameFallback ?? data.Internal;
    public string? Icon => data.Icon;
    public CalamityItemStats? Stats => data.Stats;
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
