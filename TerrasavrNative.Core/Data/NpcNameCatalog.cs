using System.Text.Json.Serialization;

namespace TerrasavrNative.Core.Data;

public sealed class NpcNameEntryData
{
    [JsonPropertyName("id")] public required int Id { get; init; }
    [JsonPropertyName("key")] public required string Key { get; init; }
    [JsonPropertyName("en")] public string? En { get; init; }
    [JsonPropertyName("es")] public string? Es { get; init; }

    public string DisplayName => Es ?? En ?? Key;
}

public sealed class NpcNameCatalog
{
    private readonly Dictionary<int, NpcNameEntryData> _byId;

    private NpcNameCatalog(Dictionary<int, NpcNameEntryData> byId) => _byId = byId;

    public string GetName(int npcId) => _byId.TryGetValue(npcId, out var e) ? e.DisplayName : $"NPC #{npcId}";

    public static NpcNameCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static NpcNameCatalog LoadFromStream(Stream stream)
    {
        var raw = System.Text.Json.JsonSerializer.Deserialize<List<NpcNameEntryData>>(stream)
            ?? throw new InvalidDataException("npc_names.json invalido.");
        return new NpcNameCatalog(raw.ToDictionary(e => e.Id));
    }
}
