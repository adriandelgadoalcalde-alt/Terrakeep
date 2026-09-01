using System.Text.Json;
using System.Text.Json.Serialization;

namespace TerrasavrNative.Core.Data;

// Una version del changelog del EDITOR (Terrakeep) - distinto de WhatsNewCatalog, que es el
// changelog del JUEGO/Calamity (whats_new.json). Contenido real derivado de bitacora.md y del
// historial de commits del propio proyecto, nada inventado - pedido explicito 1-sep-2026 tras
// comprobar que la tarjeta "Novedades" de Inicio decia falsamente ser del editor.
public sealed class ChangelogEntry
{
    [JsonPropertyName("version")] public required string Version { get; init; }
    [JsonPropertyName("date")] public string? Date { get; init; }
    [JsonPropertyName("summary")] public string? Summary { get; init; }
    [JsonPropertyName("added")] public List<string> Added { get; init; } = [];
    [JsonPropertyName("fixed")] public List<string> Fixed { get; init; } = [];
}

public sealed class ChangelogCatalog
{
    public IReadOnlyList<ChangelogEntry> Entries { get; }

    private ChangelogCatalog(List<ChangelogEntry> entries) => Entries = entries;

    public static ChangelogCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static ChangelogCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<List<ChangelogEntry>>(stream)
            ?? throw new InvalidDataException("changelog.json invalido.");
        return new ChangelogCatalog(raw);
    }
}
