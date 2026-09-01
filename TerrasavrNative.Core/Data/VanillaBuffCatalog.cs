using System.Text.Json;

namespace TerrasavrNative.Core.Data;

// Nombres de los 354 buffs vanilla reales (BuffID.cs decompilado, id -> nombre interno). SIN
// traduccion real al español todavia - lang.zip (ver VanillaItemCatalog) solo trae la
// categoria "Items" (que solo tiene BuffDescription, no BuffName) - los buffs de Calamity SI
// tienen nombre real en español via calamity/buffs.json, pero los vanilla se quedan con una
// version "humanizada" del nombre interno en ingles (ej. "ObsidianSkin" -> "Obsidian Skin")
// hasta que se investigue de donde saca el juego real el nombre de buff traducido.
public sealed class VanillaBuffCatalog
{
    private readonly Dictionary<int, string> _namesById;

    private VanillaBuffCatalog(Dictionary<int, string> namesById) => _namesById = namesById;

    public string GetName(int buffId) =>
        _namesById.TryGetValue(buffId, out var name) ? name : $"Buff #{buffId}";

    public IEnumerable<(int Id, string Name)> AllEntries() => _namesById.Select(kv => (kv.Key, kv.Value));

    public static VanillaBuffCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static VanillaBuffCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidDataException("vanilla_buff_names.json invalido.");
        var byId = new Dictionary<int, string>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;
        return new VanillaBuffCatalog(byId);
    }
}
