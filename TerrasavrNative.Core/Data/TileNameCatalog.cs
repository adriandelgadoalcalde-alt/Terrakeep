using System.Text.Json;

namespace TerrasavrNative.Core.Data;

// Nombres reales de tile/pared (tile_names.json, generado en Terrasavr-Calamity-Beta cruzando
// Data/tiles.json y Data/walls.json de TEdit contra la traduccion real del juego). Cada
// entrada tiene variantes por sprite (clave "u,v") - de momento esta capa solo resuelve el
// nombre BASE (id de tile/pared), no las variantes por frame, porque WldTile todavia no
// guarda u/v (ver el comentario en WldTile.cs) - ampliar esto si el visor llega a necesitar
// tooltips por variante exacta.
public sealed class TileNameCatalog
{
    private readonly Dictionary<int, string> _tiles;
    private readonly Dictionary<int, string> _walls;

    private TileNameCatalog(Dictionary<int, string> tiles, Dictionary<int, string> walls)
    {
        _tiles = tiles;
        _walls = walls;
    }

    public string TileName(int type) => _tiles.TryGetValue(type, out var n) ? n : $"Tile #{type}";
    public string WallName(int wallId) => wallId == 0 ? string.Empty : _walls.TryGetValue(wallId, out var n) ? n : $"Pared #{wallId}";

    public static TileNameCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static TileNameCatalog LoadFromStream(Stream stream)
    {
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;
        return new TileNameCatalog(ReadNames(root.GetProperty("tiles")), ReadNames(root.GetProperty("walls")));
    }

    private static Dictionary<int, string> ReadNames(JsonElement obj)
    {
        var result = new Dictionary<int, string>();
        foreach (var prop in obj.EnumerateObject())
        {
            if (!int.TryParse(prop.Name, out int id)) continue;
            string? name = prop.Value.TryGetProperty("name_es", out var esEl) ? esEl.GetString()
                : prop.Value.TryGetProperty("name", out var nameEl) ? nameEl.GetString()
                : null;
            if (name != null) result[id] = name;
        }
        return result;
    }
}
