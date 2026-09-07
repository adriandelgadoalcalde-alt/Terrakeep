using System.Text.Json;
using Terrakeep.Core.Model;

namespace Terrakeep.Core.Data;

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

    // Codigos reales de WldReader.cs (1=Agua, 2=Lava, 3=Miel, 4=Shimmer sintetico) - unica
    // fuente de verdad para el color de un liquido, usada tanto por WorldRenderer (mapa
    // pintado) como por ExplorationViewModel (swatch de respaldo en la pestaña Liquidos del
    // buscador, que no tiene sprite recortable: ver el comentario real en ese fichero).
    //
    // Movido aqui desde WorldRenderer.cs (2-sep-2026 lo tenia duplicado y con dos bugs reales ya
    // corregidos, que no hay que reintroducir si se toca esto): (1) los codigos 1/2/3 estaban
    // asignados al REVES (Agua salia pintada del color de Lava) - confirmado contra la fuente
    // real de TEdit, World.FileV2.cs: Agua -> header1 |= 0b0000_1000 (codigo 1), Lava ->
    // 0b0001_0000 (codigo 2), Miel -> 0b0001_1000 (codigo 3); (2) Miel y Shimmer compartian el
    // MISMO codigo 3 en disco, asi que la Miel salia pintada de lila (color de Shimmer) -
    // WldReader.cs separa Shimmer a un codigo sintetico propio (4, nunca en disco) para poder
    // distinguirlos aqui.
    public RgbaColor LiquidColor(byte liquidType) => Global(liquidType switch
    {
        2 => "Lava",
        3 => "Honey",
        4 => "Shimmer",
        _ => "Water", // codigo 1, y cualquier valor de reserva
    });

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
