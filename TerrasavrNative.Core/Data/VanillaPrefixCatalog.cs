using System.Text.Json;
using System.Text.Json.Serialization;

namespace TerrasavrNative.Core.Data;

public sealed class VanillaPrefixEntryData
{
    [JsonPropertyName("id")] public required int Id { get; init; }
    [JsonPropertyName("internal")] public required string Internal { get; init; }
    [JsonPropertyName("es")] public string? Es { get; init; }
    [JsonPropertyName("en")] public string? En { get; init; }
}

// Los 97 prefijos vanilla reales (PrefixID), calamity/prefixes.json.
public sealed class VanillaPrefixCatalog
{
    private readonly Dictionary<int, VanillaPrefixEntryData> _byId;

    private VanillaPrefixCatalog(Dictionary<int, VanillaPrefixEntryData> byId) => _byId = byId;

    public VanillaPrefixEntryData? ById(int id) => _byId.TryGetValue(id, out var e) ? e : null;

    public static VanillaPrefixCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static VanillaPrefixCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<List<VanillaPrefixEntryData>>(stream)
            ?? throw new InvalidDataException("prefixes.json invalido.");
        return new VanillaPrefixCatalog(raw.ToDictionary(e => e.Id));
    }
}
