using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terrakeep.Core.Data;

// Ronda de idioma del 6-sep-2026 - BUG REAL de esta clase y de las tres de abajo: el JSON YA
// traia el texto ingles de cada linea (campo "en", desde el primer dia), pero DisplayText/
// DisplayName/DisplayDate/DisplayNote devolvian SIEMPRE el español y eran lo unico que el XAML
// bindeaba. Resultado: la pestaña Novedades entera se quedaba en español con la app en ingles,
// sin que faltara ni un solo dato - solo faltaba elegir. Los "...For(language)" son los que hay
// que usar; los "Display..." se quedan como el caso español de siempre.
public sealed class WhatsNewChange
{
    [JsonPropertyName("es")] public string? Es { get; init; }
    [JsonPropertyName("en")] public string? En { get; init; }

    public string DisplayText => Es ?? En ?? string.Empty;
    public string TextFor(string language) => LocalizedContent.Pick(Es, En, language);
}

// Objeto nuevo listado en una version (whats_new.json, campo "items") - "key" es el nombre
// interno, no un id numerico.
public sealed class WhatsNewItem
{
    [JsonPropertyName("key")] public string? Key { get; init; }
    [JsonPropertyName("es")] public string? Es { get; init; }
    [JsonPropertyName("en")] public string? En { get; init; }

    public string DisplayName => Es ?? En ?? Key ?? "?";

    // El nombre INTERNO (Key) es el ultimo recurso real cuando no hay ninguno de los dos
    // nombres - mismo criterio de siempre, "lo que no se encuentra no se inventa".
    public string NameFor(string language)
    {
        string elegido = LocalizedContent.Pick(Es, En, language);
        return string.IsNullOrWhiteSpace(elegido) ? Key ?? "?" : elegido;
    }
}

public sealed class WhatsNewEntry
{
    [JsonPropertyName("version")] public required string Version { get; init; }
    [JsonPropertyName("date_es")] public string? DateEs { get; init; }
    [JsonPropertyName("date_en")] public string? DateEn { get; init; }
    [JsonPropertyName("note_es")] public string? NoteEs { get; init; }
    [JsonPropertyName("note_en")] public string? NoteEn { get; init; }
    [JsonPropertyName("items")] public List<WhatsNewItem> Items { get; init; } = [];
    [JsonPropertyName("changes")] public List<WhatsNewChange> Changes { get; init; } = [];
    // Pedido explicito del usuario (2-sep-2026): "bugfixes" ya venia en el propio JSON pero
    // System.Text.Json lo ignoraba en silencio (nunca hubo esta propiedad aqui) - los arreglos
    // reales de cada version no se mostraban nunca.
    [JsonPropertyName("bugfixes")] public List<WhatsNewChange> Bugfixes { get; init; } = [];

    public string DisplayDate => DateEs ?? DateEn ?? string.Empty;
    public string DisplayNote => NoteEs ?? NoteEn ?? string.Empty;

    public string DateFor(string language) => LocalizedContent.Pick(DateEs, DateEn, language);
    public string NoteFor(string language) => LocalizedContent.Pick(NoteEs, NoteEn, language);
}

// Panel "Novedades" - registro de novedades por version de Terraria (whats_new.json, mas
// reciente primero, ya viene asi ordenado en el propio archivo).
public sealed class WhatsNewCatalog
{
    public IReadOnlyList<WhatsNewEntry> Entries { get; }

    private WhatsNewCatalog(List<WhatsNewEntry> entries) => Entries = entries;

    public static WhatsNewCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static WhatsNewCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<List<WhatsNewEntry>>(stream)
            ?? throw new InvalidDataException("whats_new.json invalido.");
        return new WhatsNewCatalog(raw);
    }
}
