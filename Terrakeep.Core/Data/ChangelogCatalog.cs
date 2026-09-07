using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terrakeep.Core.Data;

// Una version del changelog del EDITOR (Terrakeep) - distinto de WhatsNewCatalog, que es el
// changelog del JUEGO/Calamity (whats_new.json). Contenido real derivado de bitacora.md y del
// historial de commits del propio proyecto, nada inventado - pedido explicito 1-sep-2026 tras
// comprobar que la tarjeta "Novedades" de Inicio decia falsamente ser del editor.
//
// Ronda de idioma del 6-sep-2026 (queja real del usuario: "mas de la mitad del contenido sigue
// en español al cambiar a ingles", citando "Acerca de" y "las versiones"): la auditoria anterior
// dejo esto fuera a proposito por considerarlo "contenido, no interfaz" - el usuario deja claro
// que SI le importa. Los campos "_en" son paralelos y OPCIONALES: mismo criterio real ya
// establecido por LocalizationService, español es el idioma de REFERENCIA y una entrada sin
// traducir cae a español, nunca a texto inventado ni a una cadena vacia.
public sealed class ChangelogEntry
{
    [JsonPropertyName("version")] public required string Version { get; init; }
    [JsonPropertyName("date")] public string? Date { get; init; }
    [JsonPropertyName("date_en")] public string? DateEn { get; init; }
    [JsonPropertyName("summary")] public string? Summary { get; init; }
    [JsonPropertyName("summary_en")] public string? SummaryEn { get; init; }
    [JsonPropertyName("added")] public List<string> Added { get; init; } = [];
    [JsonPropertyName("added_en")] public List<string> AddedEn { get; init; } = [];
    [JsonPropertyName("fixed")] public List<string> Fixed { get; init; } = [];
    [JsonPropertyName("fixed_en")] public List<string> FixedEn { get; init; } = [];

    public string DateFor(string language) => LocalizedContent.Pick(Date, DateEn, language);
    public string SummaryFor(string language) => LocalizedContent.Pick(Summary, SummaryEn, language);
    public IReadOnlyList<string> AddedFor(string language) => LocalizedContent.PickList(Added, AddedEn, language);
    public IReadOnlyList<string> FixedFor(string language) => LocalizedContent.PickList(Fixed, FixedEn, language);
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
