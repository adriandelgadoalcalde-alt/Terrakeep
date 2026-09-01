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
public sealed class CalamityBuffEntry(CalamityBuffEntryData data, int syntheticId)
{
    public int SyntheticId { get; } = syntheticId;
    public string Internal => data.Internal;
    public string Mod => data.Mod;
    public string DisplayName => data.DisplayNameEs ?? data.DisplayNameFallback ?? data.Internal;
    public string? Icon => data.Icon;
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

    public static CalamityBuffCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static CalamityBuffCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<List<CalamityBuffEntryData>>(stream)
            ?? throw new InvalidDataException("buffs.json no contiene un array valido.");

        // El orden del array IMPORTA - determina el id sintetico de cada buff. No reordenar.
        var entries = new List<CalamityBuffEntry>(raw.Count);
        for (int i = 0; i < raw.Count; i++)
            entries.Add(new CalamityBuffEntry(raw[i], CalamityIds.BuffIdBase + i));

        return new CalamityBuffCatalog(entries);
    }
}
