using System.Text.Json;

namespace TerrasavrNative.Core.Data;

// Categoria real por id de objeto vanilla - extraida directamente de los bloques SetDefaults
// de Item.cs (decompilado real de tModLoader, ver scripts/extraer-categorias-vanilla.py en la
// raiz del repo), nunca inventada: cada categoria viene de mirar que campos reales asigna el
// juego para ese id concreto (accessory/melee/ranged/headSlot/ammo/createTile...). Mismo
// esquema de jerarquia "Padre/Hijo" que ya usa Category en CalamityCatalogEntry (catalog.json
// real), para poder construir un unico arbol de carpetas con ambas fuentes.
public sealed class VanillaCategoryCatalog
{
    private readonly Dictionary<int, string> _byId;

    private VanillaCategoryCatalog(Dictionary<int, string> byId) => _byId = byId;

    // "Materiales" es el cajon de sastre real del propio extractor (todo lo que no encaja en
    // ninguna categoria mas especifica cae ahi) - se usa igual aqui como valor por defecto
    // para cualquier id que ni siquiera este en el propio archivo (no deberia pasar salvo
    // algun id nuevo de una version de Terraria mas reciente que el codigo decompilado usado).
    public string GetCategory(int itemId) => _byId.TryGetValue(itemId, out var cat) ? cat : "Materiales";

    public static VanillaCategoryCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static VanillaCategoryCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidDataException("vanilla_categories.json invalido.");
        var byId = new Dictionary<int, string>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;
        return new VanillaCategoryCatalog(byId);
    }
}
