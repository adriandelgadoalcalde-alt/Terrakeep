using System.Text.Json;

namespace Terrakeep.Core.Data;

// Tabla de "mejor prefijo" real por objeto (calamity/best_prefix.json - vanilla indexado por id
// numerico, Calamity por nombre interno). Cubre armas/accesorios reales (objetos sin prefijo
// util, como bloques, simplemente no aparecen aqui). No incluye los 21 prefijos reales de
// Calamity (esos viven en RoguePrefixCatalog.Best, con su propia logica por clase Picaro) -
// esta tabla es la "generica" que ya funcionaba desde antes de esa ampliacion.
public sealed class BestPrefixCatalog
{
    private readonly Dictionary<int, byte> _vanillaById;
    private readonly Dictionary<string, byte> _calamityByInternal;

    private BestPrefixCatalog(Dictionary<int, byte> vanillaById, Dictionary<string, byte> calamityByInternal)
    {
        _vanillaById = vanillaById;
        _calamityByInternal = calamityByInternal;
    }

    public byte? BestVanillaPrefix(int itemId) => _vanillaById.TryGetValue(itemId, out var p) ? p : null;
    public byte? BestCalamityPrefix(string internalName) => _calamityByInternal.TryGetValue(internalName, out var p) ? p : null;

    public static BestPrefixCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static BestPrefixCatalog LoadFromStream(Stream stream)
    {
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;

        var vanilla = new Dictionary<int, byte>();
        foreach (var prop in root.GetProperty("vanilla").EnumerateObject())
            if (int.TryParse(prop.Name, out int id))
                vanilla[id] = (byte)prop.Value.GetInt32();

        var calamity = new Dictionary<string, byte>();
        foreach (var prop in root.GetProperty("calamity").EnumerateObject())
            calamity[prop.Name] = (byte)prop.Value.GetInt32();

        return new BestPrefixCatalog(vanilla, calamity);
    }
}
