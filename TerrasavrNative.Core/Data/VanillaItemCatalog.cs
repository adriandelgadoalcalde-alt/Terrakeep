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

    private VanillaItemCatalog(Dictionary<int, string> namesById) => _namesById = namesById;

    public string GetName(int itemId) =>
        _namesById.TryGetValue(itemId, out var name) ? name : $"Item #{itemId}";

    public static VanillaItemCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static VanillaItemCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidDataException("vanilla_item_names.json invalido.");
        var byId = new Dictionary<int, string>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;
        return new VanillaItemCatalog(byId);
    }
}
