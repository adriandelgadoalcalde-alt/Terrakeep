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
    private readonly Dictionary<string, string> _namesByKey;
    private readonly Dictionary<string, int> _idsByKey;
    private readonly Dictionary<int, string> _keysById;
    // Ronda de traduccion del CONTENIDO del juego (6-sep-2026): mismo catalogo real contra
    // Terraria.Localization.Content.en-US.Items.json (6180 nombres reales, ver
    // scripts/extraer-nombres-objetos-en.py). Vacio = catalogo cargado sin la parte inglesa
    // (varios tests de Core lo hacen a proposito): entonces se responde siempre en español,
    // que es el idioma de referencia y esta completo.
    private readonly Dictionary<int, string> _namesByIdEn;
    private readonly Dictionary<string, string> _namesByKeyEn;

    private VanillaItemCatalog(
        Dictionary<int, string> namesById, Dictionary<string, string> namesByKey, Dictionary<string, int> idsByKey,
        Dictionary<int, string>? namesByIdEn = null, Dictionary<string, string>? namesByKeyEn = null)
    {
        _namesById = namesById;
        _namesByKey = namesByKey;
        _idsByKey = idsByKey;
        _namesByIdEn = namesByIdEn ?? [];
        _namesByKeyEn = namesByKeyEn ?? [];
        // H5-02 (quinta auditoria de Opus): reverso real de GetIdByKey - hace falta para poder
        // ESCRIBIR una entrada de investigacion nueva (PlrResearchEntry.Pid) a partir de un id
        // ya resuelto, no solo leerla. El primero que gane en caso de alias reales (mismo id,
        // mas de una clave) es una resolucion valida igual - el Pid solo tiene que apuntar de
        // vuelta al MISMO objeto real, no a una clave concreta entre varias.
        _keysById = new Dictionary<int, string>();
        foreach (var (key, id) in idsByKey) _keysById.TryAdd(id, key);
    }

    // Nombre interno real por id (ej. 4 -> "IronBroadsword") - reverso de GetIdByKey, ver el
    // comentario del campo _keysById.
    public string? GetKeyById(int itemId) => _keysById.TryGetValue(itemId, out var key) ? key : null;

    public string GetName(int itemId) => GetName(itemId, LocalizedContent.CurrentLanguage);

    public string GetName(int itemId, string language)
    {
        if (language == LocalizedContent.English && _namesByIdEn.TryGetValue(itemId, out var en)) return en;
        return _namesById.TryGetValue(itemId, out var name) ? name : $"Item #{itemId}";
    }

    // Por nombre interno (ej. "MoltenHelmet") - el mismo formato que usan los PID de
    // investigacion vanilla (sin "/", a diferencia de los PID de mods) y los "pid" de
    // builds.json. Ver Assets/vanilla_item_names_by_key.json.
    public string GetNameByKey(string internalName) => GetNameByKey(internalName, LocalizedContent.CurrentLanguage);

    public string GetNameByKey(string internalName, string language)
    {
        if (language == LocalizedContent.English && _namesByKeyEn.TryGetValue(internalName, out var en)) return en;
        return _namesByKey.TryGetValue(internalName, out var name) ? name : internalName;
    }

    // Id real por nombre interno - usado para resolver "pid" de builds.json a un GameItem.Id
    // real (auto-equipar). Ver Assets/vanilla_item_ids_by_key.json (generado desde
    // ItemID.cs decompilado, mismo criterio que vanilla_item_names.json).
    public int? GetIdByKey(string internalName) =>
        _idsByKey.TryGetValue(internalName, out var id) ? id : null;

    public IReadOnlyCollection<string> AllInternalNames() => _namesByKey.Keys;

    // Enumeracion en el idioma activo - la usan el buscador de objetos y los arboles, donde un
    // nombre en el idioma equivocado no solo se ve mal: hace que la busqueda no encuentre nada.
    public IEnumerable<(int Id, string Name)> AllEntries() => AllEntries(LocalizedContent.CurrentLanguage);

    public IEnumerable<(int Id, string Name)> AllEntries(string language) =>
        _namesById.Select(kv => (kv.Key, GetName(kv.Key, language)));

    // Las rutas inglesas son opcionales: null/fichero ausente = catalogo solo español (varios
    // tests de Core cargan asi a proposito), nunca una excepcion de arranque.
    public static VanillaItemCatalog LoadFromFile(string path, string byKeyPath, string idsByKeyPath,
        string? enPath = null, string? enByKeyPath = null)
    {
        using var stream = File.OpenRead(path);
        using var keyStream = File.OpenRead(byKeyPath);
        using var idsStream = File.OpenRead(idsByKeyPath);
        var catalog = LoadFromStreams(stream, keyStream, idsStream);
        if (enPath is not null && File.Exists(enPath))
            using (var enStream = File.OpenRead(enPath))
                Fill(catalog._namesByIdEn, ReadStringMap(enStream), int.Parse);
        if (enByKeyPath is not null && File.Exists(enByKeyPath))
            using (var enKeyStream = File.OpenRead(enByKeyPath))
                foreach (var (k, v) in ReadStringMap(enKeyStream)) catalog._namesByKeyEn[k] = v;
        return catalog;
    }

    private static Dictionary<string, string> ReadStringMap(Stream stream) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [];

    private static void Fill(Dictionary<int, string> target, Dictionary<string, string> raw, Func<string, int> parse)
    {
        foreach (var (key, value) in raw) target[parse(key)] = value;
    }

    public static VanillaItemCatalog LoadFromStreams(Stream stream, Stream byKeyStream, Stream idsByKeyStream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidDataException("vanilla_item_names.json invalido.");
        var byId = new Dictionary<int, string>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;

        var byKey = JsonSerializer.Deserialize<Dictionary<string, string>>(byKeyStream)
            ?? throw new InvalidDataException("vanilla_item_names_by_key.json invalido.");

        var idsByKey = JsonSerializer.Deserialize<Dictionary<string, int>>(idsByKeyStream)
            ?? throw new InvalidDataException("vanilla_item_ids_by_key.json invalido.");

        return new VanillaItemCatalog(byId, byKey, idsByKey);
    }
}
