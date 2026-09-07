using System.Text.Json;

namespace Terrakeep.Core.Data;

// Nombres de los 354 buffs vanilla reales (BuffID.cs decompilado, id -> nombre interno).
// GetName da la version "humanizada" del nombre interno en ingles (ej. "ObsidianSkin" ->
// "Obsidian Skin"); GetDisplayName da el nombre real en español cuando existe (ver mas abajo -
// encontrado 2-sep-2026, cuarta pasada, Game.json clave BuffName).
public sealed class VanillaBuffCatalog
{
    private readonly Dictionary<int, string> _namesById;
    private readonly Dictionary<string, string> _descriptionsByInternal;
    private readonly Dictionary<string, string> _namesEsByInternal;
    // Ronda de traduccion del CONTENIDO del juego (6-sep-2026): nombre y descripcion REALES del
    // juego en ingles (en-US.Game.json, claves BuffName 352 y BuffDescription 353 - cobertura
    // 100%, ver scripts/extraer-nombres-buffs-en.py). Ojo: NO son lo mismo que
    // vanilla_buff_names.json, que es el nombre INTERNO de BuffID.cs humanizado con espacios
    // (ej. "Beetle Might3", "Minecart Left") y sigue haciendo de clave y de ultimo recurso.
    private readonly Dictionary<string, string> _namesEnByInternal = [];
    private readonly Dictionary<string, string> _descriptionsEnByInternal = [];

    private VanillaBuffCatalog(Dictionary<int, string> namesById, Dictionary<string, string> descriptionsByInternal, Dictionary<string, string> namesEsByInternal)
    {
        _namesById = namesById;
        _descriptionsByInternal = descriptionsByInternal;
        _namesEsByInternal = namesEsByInternal;
    }

    public string GetName(int buffId) =>
        _namesById.TryGetValue(buffId, out var name) ? name : $"Buff #{buffId}";

    // Nombre real en español (pregunta a Opus sobre el diseño 2-sep-2026, cuarta pasada -
    // cierra el TODO de arriba): Terraria.Localization.Content.es-ES.Game.json, clave
    // BuffName (352 entradas reales), mismo nombre interno PascalCase que BuffDescription.
    // Cae al nombre "humanizado" en ingles (GetName) si de verdad no hay traduccion real.
    public string GetDisplayName(int buffId) => GetDisplayName(buffId, LocalizedContent.CurrentLanguage);

    public string GetDisplayName(int buffId, string language)
    {
        if (!_namesById.TryGetValue(buffId, out var name)) return $"Buff #{buffId}";
        string internalName = name.Replace(" ", "");
        if (language == LocalizedContent.English && _namesEnByInternal.TryGetValue(internalName, out var en)) return en;
        if (language == LocalizedContent.English) return name; // el interno humanizado YA es ingles
        return _namesEsByInternal.TryGetValue(internalName, out var es) ? es : name;
    }

    // Descripcion real (scripts/extraer-descripciones-buffs.py, 353 reales de
    // Terraria.Localization.Content.es-ES.Game.json, clave "BuffDescription") - null si de
    // verdad no hay (ej. los buffs internos de minecart, que el propio juego no traduce
    // porque nunca se le muestran al jugador). vanilla_buff_names.json guarda el nombre YA
    // "humanizado" con espacios (ej. "Obsidian Skin") pero BuffDescription usa el nombre
    // interno PascalCase real sin espacios ("ObsidianSkin") - quitar los espacios reconstruye
    // la clave real (verificado: 290/354 aciertos, el resto son minecarts sin descripcion
    // real que mostrar, no un fallo de esta transformacion).
    public string? GetDescription(int buffId) => GetDescription(buffId, LocalizedContent.CurrentLanguage);

    public string? GetDescription(int buffId, string language)
    {
        if (!_namesById.TryGetValue(buffId, out var name)) return null;
        string internalName = name.Replace(" ", "");
        if (language == LocalizedContent.English && _descriptionsEnByInternal.TryGetValue(internalName, out var en)) return en;
        return _descriptionsByInternal.TryGetValue(internalName, out var desc) ? desc : null;
    }

    public IEnumerable<(int Id, string Name)> AllEntries() => _namesById.Select(kv => (kv.Key, kv.Value));

    public static VanillaBuffCatalog LoadFromFile(string namesPath, string descriptionsPath, string namesEsPath,
        string? namesEnPath = null, string? descriptionsEnPath = null)
    {
        using var namesStream = File.OpenRead(namesPath);
        using var descStream = File.OpenRead(descriptionsPath);
        using var namesEsStream = File.OpenRead(namesEsPath);
        var catalog = LoadFromStream(namesStream, descStream, namesEsStream);
        Merge(catalog._namesEnByInternal, namesEnPath);
        Merge(catalog._descriptionsEnByInternal, descriptionsEnPath);
        return catalog;
    }

    // Ruta null o fichero ausente = catalogo solo español (varios tests de Core cargan asi a
    // proposito) - nunca revienta el arranque por no encontrar la parte inglesa.
    private static void Merge(Dictionary<string, string> target, string? path)
    {
        if (path is null || !File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [];
        foreach (var (key, value) in raw) target[key] = value;
    }

    public static VanillaBuffCatalog LoadFromStream(Stream namesStream, Stream descriptionsStream, Stream namesEsStream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(namesStream)
            ?? throw new InvalidDataException("vanilla_buff_names.json invalido.");
        var byId = new Dictionary<int, string>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;

        var descriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(descriptionsStream)
            ?? throw new InvalidDataException("vanilla_buff_descriptions.json invalido.");
        var namesEs = JsonSerializer.Deserialize<Dictionary<string, string>>(namesEsStream)
            ?? throw new InvalidDataException("vanilla_buff_names_es.json invalido.");

        return new VanillaBuffCatalog(byId, descriptions, namesEs);
    }
}
