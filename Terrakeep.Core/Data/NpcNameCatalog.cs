using System.Text.Json.Serialization;

namespace Terrakeep.Core.Data;

public sealed class NpcNameEntryData
{
    [JsonPropertyName("id")] public required int Id { get; init; }
    [JsonPropertyName("key")] public required string Key { get; init; }
    [JsonPropertyName("en")] public string? En { get; init; }
    [JsonPropertyName("es")] public string? Es { get; init; }

    // Ronda de traduccion del CONTENIDO del juego (6-sep-2026): npc_names.json YA traia los dos
    // idiomas reales (691/691 con `en` y `es`) - lo que faltaba era usarlos. Antes esto devolvia
    // el español siempre, tambien con la interfaz en ingles.
    public string DisplayName => DisplayNameFor(LocalizedContent.CurrentLanguage);

    public string DisplayNameFor(string language)
    {
        string? picked = LocalizedContent.Pick(Es, En, language);
        return string.IsNullOrWhiteSpace(picked) ? Key : picked;
    }
}

public sealed class NpcNameCatalog
{
    private readonly Dictionary<int, NpcNameEntryData> _byId;
    // Editor de mundos v1 (14-sep-2026): el bestiario del .wld guarda sus claves como texto
    // (el "bestiary credit id" real del juego, NPC.GetBestiaryCreditId -> NPCID.Search.GetName
    // para NPCs vanilla) - EXACTAMENTE el mismo texto que la columna "key" de npc_names.json,
    // confirmado a mano (id 3 = "Zombie" en los dos). Indice por clave, aparte del de por id,
    // para poder traducir esas entradas sin tener que ir a buscar primero el id.
    private readonly Dictionary<string, NpcNameEntryData> _byKey;

    private NpcNameCatalog(Dictionary<int, NpcNameEntryData> byId)
    {
        _byId = byId;
        _byKey = byId.Values.GroupBy(e => e.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    public string GetName(int npcId) => GetName(npcId, LocalizedContent.CurrentLanguage);

    public string GetName(int npcId, string language) =>
        _byId.TryGetValue(npcId, out var e) ? e.DisplayNameFor(language) : $"NPC #{npcId}";

    // NPCs modded (Calamity) no estan en npc_names.json (catalogo solo vanilla, ver el
    // comentario real de WldBestiary) - devuelve null en vez de un "#0" inventado, para que el
    // llamador pueda decidir mostrar la clave cruda tal cual (honestidad real: nunca fingir una
    // traduccion que no existe).
    public string? TryGetNameByKey(string bestiaryKey) => TryGetNameByKey(bestiaryKey, LocalizedContent.CurrentLanguage);

    public string? TryGetNameByKey(string bestiaryKey, string language) =>
        _byKey.TryGetValue(bestiaryKey, out var e) ? e.DisplayNameFor(language) : null;

    // Punto 4 (advisor Opus, buscador de objetos del mundo): enumeracion real para resolver un
    // texto de busqueda libre contra el nombre de tipo de NPC (no el nombre propio que el
    // jugador le puso, ese vive en WldNpc.GivenName) - mismo motivo que AllTiles/AllWalls de
    // TileNameCatalog.
    public IEnumerable<(int Id, string Name)> All => _byId.Select(kv => (kv.Key, kv.Value.DisplayName));

    // El buscador del mundo resuelve texto libre contra estos nombres: si la enumeracion va en
    // un idioma y el usuario escribe en el otro, no encuentra nada. Por eso hay version por
    // idioma explicito ademas de la del idioma activo.
    public IEnumerable<(int Id, string Name)> AllFor(string language) =>
        _byId.Select(kv => (kv.Key, kv.Value.DisplayNameFor(language)));

    public static NpcNameCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static NpcNameCatalog LoadFromStream(Stream stream)
    {
        var raw = System.Text.Json.JsonSerializer.Deserialize<List<NpcNameEntryData>>(stream)
            ?? throw new InvalidDataException("npc_names.json invalido.");
        return new NpcNameCatalog(raw.ToDictionary(e => e.Id));
    }
}
