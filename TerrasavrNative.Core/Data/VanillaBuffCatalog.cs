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
    private readonly Dictionary<string, string> _descriptionsByInternal;

    private VanillaBuffCatalog(Dictionary<int, string> namesById, Dictionary<string, string> descriptionsByInternal)
    {
        _namesById = namesById;
        _descriptionsByInternal = descriptionsByInternal;
    }

    public string GetName(int buffId) =>
        _namesById.TryGetValue(buffId, out var name) ? name : $"Buff #{buffId}";

    // Descripcion real (scripts/extraer-descripciones-buffs.py, 353 reales de
    // Terraria.Localization.Content.es-ES.Game.json, clave "BuffDescription") - null si de
    // verdad no hay (ej. los buffs internos de minecart, que el propio juego no traduce
    // porque nunca se le muestran al jugador). vanilla_buff_names.json guarda el nombre YA
    // "humanizado" con espacios (ej. "Obsidian Skin") pero BuffDescription usa el nombre
    // interno PascalCase real sin espacios ("ObsidianSkin") - quitar los espacios reconstruye
    // la clave real (verificado: 290/354 aciertos, el resto son minecarts sin descripcion
    // real que mostrar, no un fallo de esta transformacion).
    public string? GetDescription(int buffId)
    {
        if (!_namesById.TryGetValue(buffId, out var name)) return null;
        string internalName = name.Replace(" ", "");
        return _descriptionsByInternal.TryGetValue(internalName, out var desc) ? desc : null;
    }

    public IEnumerable<(int Id, string Name)> AllEntries() => _namesById.Select(kv => (kv.Key, kv.Value));

    public static VanillaBuffCatalog LoadFromFile(string namesPath, string descriptionsPath)
    {
        using var namesStream = File.OpenRead(namesPath);
        using var descStream = File.OpenRead(descriptionsPath);
        return LoadFromStream(namesStream, descStream);
    }

    public static VanillaBuffCatalog LoadFromStream(Stream namesStream, Stream descriptionsStream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(namesStream)
            ?? throw new InvalidDataException("vanilla_buff_names.json invalido.");
        var byId = new Dictionary<int, string>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;

        var descriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionsStream)
            ?? throw new InvalidDataException("vanilla_buff_descriptions.json invalido.");

        return new VanillaBuffCatalog(byId, descriptions);
    }
}
