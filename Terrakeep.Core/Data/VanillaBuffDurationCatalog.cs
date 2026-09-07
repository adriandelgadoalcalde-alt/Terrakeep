using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terrakeep.Core.Data;

// Duracion minima real de un buff (el buffTime real de su pocion/objeto base en Item.cs) -
// pregunta a Opus sobre el diseño 2-sep-2026, cuarta pasada. Fuente real:
// scripts/extraer-duraciones-buffs.py (30 buffs reales). El unico caso real de varias
// duraciones para el mismo buff (Suerte, buff 257) trae ademas Tiers con las 3 reales
// (menor/normal/mayor, ratio exacto 1:2:3).
public sealed class VanillaBuffDurationInfo
{
    [JsonPropertyName("min")] public required int MinTicks { get; init; }
    [JsonPropertyName("src")] public required string Source { get; init; }
    [JsonPropertyName("srcItem")] public int? SourceItemId { get; init; }
    [JsonPropertyName("tiers")] public int[]? Tiers { get; init; }
}

public sealed class VanillaBuffDurationCatalog
{
    private readonly Dictionary<int, VanillaBuffDurationInfo> _byId;

    private VanillaBuffDurationCatalog(Dictionary<int, VanillaBuffDurationInfo> byId) => _byId = byId;

    public VanillaBuffDurationInfo? Get(int buffId) => _byId.TryGetValue(buffId, out var info) ? info : null;

    public static VanillaBuffDurationCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static VanillaBuffDurationCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, VanillaBuffDurationInfo>>(stream)
            ?? throw new InvalidDataException("vanilla_buff_durations.json invalido.");
        var byId = new Dictionary<int, VanillaBuffDurationInfo>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;
        return new VanillaBuffDurationCatalog(byId);
    }
}
