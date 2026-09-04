using System.Text.Json;

namespace TerrasavrNative.Core.Data;

// Nombres reales de tile/pared (tile_names.json, generado en Terrasavr-Calamity-Beta cruzando
// Data/tiles.json y Data/walls.json de TEdit contra la traduccion real del juego). Cada
// entrada de tile puede traer variantes por sprite exacto (objeto "frames", clave "u,v" en
// PIXELES reales de la hoja de sprites - ej. distinguir un cofre de oro de uno de la jungla,
// mismo id de tile 21) - TileVariantName las resuelve usando el u/v que WldTile ya guarda.
public sealed class TileNameCatalog
{
    private readonly Dictionary<int, string> _tiles;
    private readonly Dictionary<int, Dictionary<(short U, short V), string>> _tileFrames;
    private readonly Dictionary<int, string> _walls;

    private TileNameCatalog(Dictionary<int, string> tiles, Dictionary<int, Dictionary<(short, short), string>> tileFrames, Dictionary<int, string> walls)
    {
        _tiles = tiles;
        _tileFrames = tileFrames;
        _walls = walls;
    }

    public string TileName(int type) => _tiles.TryGetValue(type, out var n) ? n : $"Tile #{type}";
    public string WallName(int wallId) => wallId == 0 ? string.Empty : _walls.TryGetValue(wallId, out var n) ? n : $"Pared #{wallId}";

    // Punto 4 (advisor Opus, buscador de objetos del mundo, ver ESPEC-buscador-mundo-tedit.md):
    // hace falta enumerar TODOS los tiles/paredes con nombre conocido para poder resolver un
    // texto de busqueda libre a un conjunto de ids reales - antes solo habia lookup por id
    // suelto (TileName/WallName), pensado para el tooltip del mapa, no para un buscador.
    public IEnumerable<(int Id, string Name)> AllTiles => _tiles.Select(kv => (kv.Key, kv.Value));
    public IEnumerable<(int Id, string Name)> AllWalls => _walls.Select(kv => (kv.Key, kv.Value));

    // Nombre de variante exacta si el tile tiene frames registrados y el u/v coincide con
    // alguno; si no hay frames para este tile, o el u/v no coincide con ninguno conocido, cae
    // al nombre base (TileName) - nunca se inventa una variante.
    public string TileVariantName(int type, short u, short v)
    {
        if (_tileFrames.TryGetValue(type, out var frames) && frames.TryGetValue((u, v), out var variantName))
            return variantName;
        return TileName(type);
    }

    public static TileNameCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static TileNameCatalog LoadFromStream(Stream stream)
    {
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;
        var (tiles, tileFrames) = ReadTileEntries(root.GetProperty("tiles"));
        return new TileNameCatalog(tiles, tileFrames, ReadNames(root.GetProperty("walls")));
    }

    private static Dictionary<int, string> ReadNames(JsonElement obj)
    {
        var result = new Dictionary<int, string>();
        foreach (var prop in obj.EnumerateObject())
        {
            if (!int.TryParse(prop.Name, out int id)) continue;
            string? name = ReadEntryName(prop.Value);
            if (name != null) result[id] = name;
        }
        return result;
    }

    private static (Dictionary<int, string> Tiles, Dictionary<int, Dictionary<(short, short), string>> Frames) ReadTileEntries(JsonElement obj)
    {
        var tiles = new Dictionary<int, string>();
        var allFrames = new Dictionary<int, Dictionary<(short, short), string>>();
        foreach (var prop in obj.EnumerateObject())
        {
            if (!int.TryParse(prop.Name, out int id)) continue;
            string? name = ReadEntryName(prop.Value);
            if (name != null) tiles[id] = name;

            if (!prop.Value.TryGetProperty("frames", out var framesEl)) continue;
            var frames = new Dictionary<(short, short), string>();
            foreach (var frameProp in framesEl.EnumerateObject())
            {
                string? frameName = ReadEntryName(frameProp.Value);
                if (frameName == null) continue;
                int comma = frameProp.Name.IndexOf(',');
                if (comma < 0) continue;
                if (!short.TryParse(frameProp.Name[..comma], out short u)) continue;
                if (!short.TryParse(frameProp.Name[(comma + 1)..], out short v)) continue;
                frames[(u, v)] = frameName;
            }
            if (frames.Count > 0) allFrames[id] = frames;
        }
        return (tiles, allFrames);
    }

    private static string? ReadEntryName(JsonElement entry) =>
        entry.TryGetProperty("name_es", out var esEl) ? esEl.GetString()
        : entry.TryGetProperty("name", out var nameEl) ? nameEl.GetString()
        : null;
}
