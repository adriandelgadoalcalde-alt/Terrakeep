using System.Text.Json;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.Core.Data;

// Colores reales del mapa (map_colors.json, generado en Terrasavr-Calamity-Beta desde
// MapColors.xml + globalColors.json de TEdit - ver ese proyecto para el detalle de como se
// genero). "tiles"/"walls" indexados por id numerico; "global" por nombre de zona
// (Space/Sky/Earth/Rock/Hell...) para el fondo segun profundidad.
public sealed class MapColorCatalog
{
    private readonly Dictionary<int, RgbaColor> _tiles;
    private readonly Dictionary<int, RgbaColor> _walls;
    private readonly Dictionary<string, RgbaColor> _global;

    private MapColorCatalog(Dictionary<int, RgbaColor> tiles, Dictionary<int, RgbaColor> walls, Dictionary<string, RgbaColor> global)
    {
        _tiles = tiles;
        _walls = walls;
        _global = global;
    }

    public RgbaColor TileColor(int type) => _tiles.TryGetValue(type, out var c) ? c : RgbaColor.Transparent;
    public RgbaColor WallColor(int wallId) => _walls.TryGetValue(wallId, out var c) ? c : RgbaColor.Transparent;
    public RgbaColor Global(string zoneName) => _global.TryGetValue(zoneName, out var c) ? c : RgbaColor.Transparent;

    public static MapColorCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static MapColorCatalog LoadFromStream(Stream stream)
    {
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;

        var tiles = ReadIndexedColors(root.GetProperty("tiles"));
        var walls = ReadIndexedColors(root.GetProperty("walls"));

        var global = new Dictionary<string, RgbaColor>();
        foreach (var prop in root.GetProperty("global").EnumerateObject())
            global[prop.Name] = ReadColor(prop.Value);

        return new MapColorCatalog(tiles, walls, global);
    }

    private static Dictionary<int, RgbaColor> ReadIndexedColors(JsonElement obj)
    {
        var result = new Dictionary<int, RgbaColor>();
        foreach (var prop in obj.EnumerateObject())
        {
            if (int.TryParse(prop.Name, out int id))
                result[id] = ReadColor(prop.Value);
        }
        return result;
    }

    private static RgbaColor ReadColor(JsonElement el) => new(
        (byte)el.GetProperty("r").GetInt32(),
        (byte)el.GetProperty("g").GetInt32(),
        (byte)el.GetProperty("b").GetInt32(),
        (byte)el.GetProperty("a").GetInt32());
}
