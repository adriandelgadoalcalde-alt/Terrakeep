using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terrakeep.Core.Data;

// Estadisticas reales por id de objeto vanilla - extraidas directamente de los bloques
// SetDefaults de Item.cs (decompilado real de tModLoader, ver
// scripts/extraer-estadisticas-vanilla.py en la raiz del repo), nunca inventadas. Solo los
// campos que el juego asigna de verdad para ese id concreto estan presentes (null = ese
// objeto no tiene esa estadistica, no "vale 0").
public sealed class VanillaItemStats
{
    [JsonPropertyName("damage")] public int? Damage { get; init; }
    [JsonPropertyName("defense")] public int? Defense { get; init; }
    [JsonPropertyName("knockBack")] public double? KnockBack { get; init; }
    [JsonPropertyName("useTime")] public int? UseTime { get; init; }
    [JsonPropertyName("crit")] public int? Crit { get; init; }
    [JsonPropertyName("mana")] public int? Mana { get; init; }
    [JsonPropertyName("healLife")] public int? HealLife { get; init; }
    [JsonPropertyName("healMana")] public int? HealMana { get; init; }
    [JsonPropertyName("rare")] public int? Rare { get; init; }
}

public sealed class VanillaItemStatsCatalog
{
    private readonly Dictionary<int, VanillaItemStats> _byId;

    private VanillaItemStatsCatalog(Dictionary<int, VanillaItemStats> byId) => _byId = byId;

    public VanillaItemStats? Get(int itemId) => _byId.TryGetValue(itemId, out var stats) ? stats : null;

    public static VanillaItemStatsCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static VanillaItemStatsCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, VanillaItemStats>>(stream)
            ?? throw new InvalidDataException("vanilla_stats.json invalido.");
        var byId = new Dictionary<int, VanillaItemStats>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;
        return new VanillaItemStatsCatalog(byId);
    }
}
