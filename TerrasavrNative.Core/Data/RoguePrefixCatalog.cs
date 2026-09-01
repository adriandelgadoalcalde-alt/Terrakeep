using System.Text.Json;
using System.Text.Json.Serialization;

namespace TerrasavrNative.Core.Data;

// Los 21 ModPrefix reales que registra CalamityMod (17 de arma incluyendo Horrible + 4 de
// accesorio) - calamity/rogue_prefixes.json ya trae el id sintetico precalculado
// (CalamityIds.PrefixIdBase + indice, 10000-10020), no hace falta recalcularlo aqui.
public sealed class RoguePrefixEntryData
{
    [JsonPropertyName("id")] public required int Id { get; init; }
    [JsonPropertyName("internal")] public required string Internal { get; init; }
    [JsonPropertyName("es")] public string? Es { get; init; }
    [JsonPropertyName("en")] public string? En { get; init; }
    // Solo presentes en prefijos de arma:
    [JsonPropertyName("damageMult")] public double? DamageMult { get; init; }
    [JsonPropertyName("useTimeMult")] public double? UseTimeMult { get; init; }
    [JsonPropertyName("shootSpeedMult")] public double? ShootSpeedMult { get; init; }
    // Solo presente en prefijos de accesorio:
    [JsonPropertyName("effect")] public string? Effect { get; init; }
}

file sealed class RoguePrefixesFile
{
    [JsonPropertyName("idBase")] public int IdBase { get; init; }
    [JsonPropertyName("weapon")] public required List<RoguePrefixEntryData> Weapon { get; init; }
    [JsonPropertyName("accessory")] public required List<RoguePrefixEntryData> Accessory { get; init; }
    [JsonPropertyName("best")] public required RoguePrefixBest Best { get; init; }
}

public sealed class RoguePrefixBest
{
    [JsonPropertyName("weapon")] public required string Weapon { get; init; }
    [JsonPropertyName("accessory")] public required string Accessory { get; init; }
}

public sealed class RoguePrefixCatalog
{
    private readonly Dictionary<int, RoguePrefixEntryData> _byId;
    private readonly Dictionary<string, RoguePrefixEntryData> _byInternal;

    public IReadOnlyList<RoguePrefixEntryData> Weapon { get; }
    public IReadOnlyList<RoguePrefixEntryData> Accessory { get; }
    public RoguePrefixBest Best { get; }

    private RoguePrefixCatalog(List<RoguePrefixEntryData> weapon, List<RoguePrefixEntryData> accessory, RoguePrefixBest best)
    {
        Weapon = weapon;
        Accessory = accessory;
        Best = best;
        var all = weapon.Concat(accessory).ToList();
        _byId = all.ToDictionary(e => e.Id);
        _byInternal = all.ToDictionary(e => e.Internal);
    }

    public RoguePrefixEntryData? ById(int id) => _byId.TryGetValue(id, out var e) ? e : null;
    public RoguePrefixEntryData? ByInternal(string internalName) => _byInternal.TryGetValue(internalName, out var e) ? e : null;

    public static RoguePrefixCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static RoguePrefixCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<RoguePrefixesFile>(stream)
            ?? throw new InvalidDataException("rogue_prefixes.json invalido.");
        return new RoguePrefixCatalog(raw.Weapon, raw.Accessory, raw.Best);
    }
}
