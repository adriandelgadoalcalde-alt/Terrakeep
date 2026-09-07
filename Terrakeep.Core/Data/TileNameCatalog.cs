using System.Text.Json;

namespace Terrakeep.Core.Data;

// Nombres reales de tile/pared (tile_names.json, generado en Terrasavr-Calamity-Beta cruzando
// Data/tiles.json y Data/walls.json de TEdit contra la traduccion real del juego). Cada
// entrada de tile puede traer variantes por sprite exacto (objeto "frames", clave "u,v" en
// PIXELES reales de la hoja de sprites - ej. distinguir un cofre de oro de uno de la jungla,
// mismo id de tile 21) - TileVariantName las resuelve usando el u/v que WldTile ya guarda.
//
// Ronda de traduccion del CONTENIDO del juego (6-sep-2026): el fichero YA era bilingue - `name`
// es el nombre real INGLES (de TEdit, el idioma base de esa fuente) y `name_es` la traduccion
// real cuando existe. Lo que faltaba era usarlo: antes se leia `name_es` y si no `name`, o sea
// SIEMPRE el mismo idioma pasara lo que pasara con la interfaz. Ahora se guardan los dos y se
// elige con el idioma activo, cayendo al que haya (nunca vacio, nunca inventado). Cobertura
// real: 754 tiles + 9347 variantes por sprite + 367 paredes con nombre ingles (100%), de los
// que 571 tiles + 5975 variantes + 357 paredes tienen ademas traduccion al español.
public sealed class TileNameCatalog
{
    // (Es, En) por id - Es puede ser null (sin traduccion real al español), En nunca lo es en
    // la practica porque es el nombre base de la fuente, pero se trata igual por seguridad.
    private readonly Dictionary<int, (string? Es, string? En)> _tiles;
    private readonly Dictionary<int, Dictionary<(short U, short V), (string? Es, string? En)>> _tileFrames;
    private readonly Dictionary<int, (string? Es, string? En)> _walls;

    private TileNameCatalog(
        Dictionary<int, (string?, string?)> tiles,
        Dictionary<int, Dictionary<(short, short), (string?, string?)>> tileFrames,
        Dictionary<int, (string?, string?)> walls)
    {
        _tiles = tiles;
        _tileFrames = tileFrames;
        _walls = walls;
    }

    private static string Pick((string? Es, string? En) entry, string language) =>
        LocalizedContent.Pick(entry.Es, entry.En, language);

    public string TileName(int type) => TileName(type, LocalizedContent.CurrentLanguage);

    public string TileName(int type, string language) =>
        _tiles.TryGetValue(type, out var n) ? Pick(n, language) : $"Tile #{type}";

    public string WallName(int wallId) => WallName(wallId, LocalizedContent.CurrentLanguage);

    public string WallName(int wallId, string language) =>
        wallId == 0 ? string.Empty
        : _walls.TryGetValue(wallId, out var n) ? Pick(n, language)
        : $"Pared #{wallId}";

    // Punto 4 (advisor Opus, buscador de objetos del mundo, ver ESPEC-buscador-mundo-tedit.md):
    // hace falta enumerar TODOS los tiles/paredes con nombre conocido para poder resolver un
    // texto de busqueda libre a un conjunto de ids reales - antes solo habia lookup por id
    // suelto (TileName/WallName), pensado para el tooltip del mapa, no para un buscador.
    public IEnumerable<(int Id, string Name)> AllTiles => AllTilesFor(LocalizedContent.CurrentLanguage);
    public IEnumerable<(int Id, string Name)> AllWalls => AllWallsFor(LocalizedContent.CurrentLanguage);

    public IEnumerable<(int Id, string Name)> AllTilesFor(string language) =>
        _tiles.Select(kv => (kv.Key, Pick(kv.Value, language)));

    public IEnumerable<(int Id, string Name)> AllWallsFor(string language) =>
        _walls.Select(kv => (kv.Key, Pick(kv.Value, language)));

    // Nombre de variante exacta si el tile tiene frames registrados y el u/v coincide con
    // alguno; si no hay frames para este tile, o el u/v no coincide con ninguno conocido, cae
    // al nombre base (TileName) - nunca se inventa una variante.
    public string TileVariantName(int type, short u, short v) =>
        TileVariantName(type, u, v, LocalizedContent.CurrentLanguage);

    public string TileVariantName(int type, short u, short v, string language)
    {
        if (_tileFrames.TryGetValue(type, out var frames) && frames.TryGetValue((u, v), out var variantName))
            return Pick(variantName, language);
        return TileName(type, language);
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

    private static Dictionary<int, (string?, string?)> ReadNames(JsonElement obj)
    {
        var result = new Dictionary<int, (string?, string?)>();
        foreach (var prop in obj.EnumerateObject())
        {
            if (!int.TryParse(prop.Name, out int id)) continue;
            var names = ReadEntryNames(prop.Value);
            if (names is { } n) result[id] = n;
        }
        return result;
    }

    private static (Dictionary<int, (string?, string?)> Tiles, Dictionary<int, Dictionary<(short, short), (string?, string?)>> Frames) ReadTileEntries(JsonElement obj)
    {
        var tiles = new Dictionary<int, (string?, string?)>();
        var allFrames = new Dictionary<int, Dictionary<(short, short), (string?, string?)>>();
        foreach (var prop in obj.EnumerateObject())
        {
            if (!int.TryParse(prop.Name, out int id)) continue;
            var names = ReadEntryNames(prop.Value);
            if (names is { } n) tiles[id] = n;

            if (!prop.Value.TryGetProperty("frames", out var framesEl)) continue;
            var frames = new Dictionary<(short, short), (string?, string?)>();
            foreach (var frameProp in framesEl.EnumerateObject())
            {
                var frameNames = ReadEntryNames(frameProp.Value);
                if (frameNames is not { } fn) continue;
                int comma = frameProp.Name.IndexOf(',');
                if (comma < 0) continue;
                if (!short.TryParse(frameProp.Name[..comma], out short u)) continue;
                if (!short.TryParse(frameProp.Name[(comma + 1)..], out short v)) continue;
                frames[(u, v)] = fn;
            }
            if (frames.Count > 0) allFrames[id] = frames;
        }
        return (tiles, allFrames);
    }

    // null solo si la entrada no tiene NINGUNO de los dos nombres (no deberia pasar en el
    // fichero real) - se descarta la entrada entera, igual que antes.
    private static (string? Es, string? En)? ReadEntryNames(JsonElement entry)
    {
        string? es = entry.TryGetProperty("name_es", out var esEl) ? esEl.GetString() : null;
        string? en = entry.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
        return es is null && en is null ? null : (es, en);
    }
}
