using System.Text.Json;
using System.Text.Json.Serialization;

namespace TerrasavrNative.Core.Data;

public sealed class WhatsNewChange
{
    [JsonPropertyName("es")] public string? Es { get; init; }
    [JsonPropertyName("en")] public string? En { get; init; }

    public string DisplayText => Es ?? En ?? string.Empty;
}

// Objeto nuevo listado en una version (whats_new.json, campo "items") - "key" es el nombre
// interno, no un id numerico.
public sealed class WhatsNewItem
{
    [JsonPropertyName("key")] public string? Key { get; init; }
    [JsonPropertyName("es")] public string? Es { get; init; }
    [JsonPropertyName("en")] public string? En { get; init; }

    public string DisplayName => Es ?? En ?? Key ?? "?";
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
