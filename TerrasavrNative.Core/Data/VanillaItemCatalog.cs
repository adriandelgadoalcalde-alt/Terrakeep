using System.Text.Json;

namespace TerrasavrNative.Core.Data;

// Nombres reales de los objetos vanilla de Terraria, generados cruzando ItemID.cs decompilado
// (id numerico -> nombre interno) con las traducciones reales del juego (lang.zip ->
// Terraria.Localization.Content.es-ES.Items.json, clave "ItemName") - ver Assets/
// vanilla_item_names.json. Los ~13 objetos sin traduccion real se quedan con su nombre interno
// en ingles (mismo criterio que tile_names.json/npc_names.json de Terrasavr-Calamity-Beta: lo
// que no encuentra coincidencia se queda en ingles a proposito, nunca se inventa).
public sealed class VanillaItemCatalog
{
    private readonly Dictionary<int, string> _namesById;
    private readonly Dictionary<string, string> _namesByKey;

    private VanillaItemCatalog(Dictionary<int, string> namesById, Dictionary<string, string> namesByKey)
    {
        _namesById = namesById;
        _namesByKey = namesByKey;
    }

    public string GetName(int itemId) =>
        _namesById.TryGetValue(itemId, out var name) ? name : $"Item #{itemId}";

    // Por nombre interno (ej. "MoltenHelmet") - el mismo formato que usan los PID de
    // investigacion vanilla (sin "/", a diferencia de los PID de mods) y los "pid" de
    // builds.json. Ver Assets/vanilla_item_names_by_key.json.
    public string GetNameByKey(string internalName) =>
        _namesByKey.TryGetValue(internalName, out var name) ? name : internalName;

    public IReadOnlyCollection<string> AllInternalNames() => _namesByKey.Keys;

    public IEnumerable<(int Id, string Name)> AllEntries() => _namesById.Select(kv => (kv.Key, kv.Value));

    public static VanillaItemCatalog LoadFromFile(string path, string byKeyPath)
    {
        using var stream = File.OpenRead(path);
        using var keyStream = File.OpenRead(byKeyPath);
        return LoadFromStreams(stream, keyStream);
    }

    public static VanillaItemCatalog LoadFromStreams(Stream stream, Stream byKeyStream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidDataException("vanilla_item_names.json invalido.");
        var byId = new Dictionary<int, string>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;

        var byKey = JsonSerializer.Deserialize<Dictionary<string, string>>(byKeyStream)
            ?? throw new InvalidDataException("vanilla_item_names_by_key.json invalido.");

        return new VanillaItemCatalog(byId, byKey);
    }
}
