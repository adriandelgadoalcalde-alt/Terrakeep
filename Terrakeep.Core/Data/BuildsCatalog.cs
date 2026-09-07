using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terrakeep.Core.Data;

public sealed class BuildItemRef
{
    [JsonPropertyName("pid")] public string? Pid { get; init; }
    [JsonPropertyName("en")] public string? En { get; init; }
    [JsonPropertyName("es")] public string? Es { get; init; }
    [JsonPropertyName("prefix")] public string? Prefix { get; init; }
    // Solo Calamity/Picaro: prefijo real de Calamity (modPrefixMod/modPrefixName), identificado
    // por su id sintetico (>= CalamityIds.PrefixIdBase) en vez de por nombre interno vanilla -
    // ver la nota de builds_calamity.json.
    [JsonPropertyName("prefixId")] public int? PrefixId { get; init; }

    public string DisplayName => Es ?? En ?? Pid ?? "?";

    // Ronda de idioma del 6-sep-2026 - mismo bug real que en WhatsNewItem: el nombre INGLES de
    // cada pieza ya venia en el propio builds.json (campo "en", desde el primer dia), pero
    // DisplayName devuelve siempre el español y era lo unico que la pestaña Builds mostraba.
    public string NameFor(string language)
    {
        string elegido = LocalizedContent.Pick(Es, En, language);
        return string.IsNullOrWhiteSpace(elegido) ? Pid ?? "?" : elegido;
    }
}

public sealed class BuildClassGear
{
    public List<BuildItemRef> Armor { get; init; } = [];
    public List<BuildItemRef> Weapons { get; init; } = [];
    public List<BuildItemRef> Accessories { get; init; } = [];
}

public sealed class BuildStage
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    // Ronda de idioma del 6-sep-2026: la etiqueta de etapa ("Pre-Hardmode (listo para el Muro de
    // Carne)") vivia SOLO en español dentro del propio builds.json y se veia tal cual con la app
    // en ingles - a diferencia de las piezas, que si traian su "en" desde el principio.
    public string? LabelEn { get; init; }
    // Clave = nombre de clase (melee/ranged/mage.../rogue en Calamity) tal cual viene en el JSON.
    public Dictionary<string, BuildClassGear> Classes { get; init; } = [];

    public string LabelFor(string language) => LocalizedContent.Pick(Label, LabelEn, language);
}

// Panel "Builds" - equipo de referencia por etapa/clase (builds.json vanilla,
// builds_calamity.json). Solo lectura por ahora (sin auto-equipar todavia) - las entradas ya
// traen su nombre real en español, no hace falta resolver ids para mostrarlas.
public sealed class BuildsCatalog
{
    public IReadOnlyList<BuildStage> Stages { get; }

    private BuildsCatalog(List<BuildStage> stages) => Stages = stages;

    public static BuildsCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static BuildsCatalog LoadFromStream(Stream stream)
    {
        using var doc = JsonDocument.Parse(stream);
        var stages = new List<BuildStage>();

        foreach (var stageProp in doc.RootElement.EnumerateObject())
        {
            if (stageProp.Value.ValueKind != JsonValueKind.Object) continue; // ej. "_note"
            if (!stageProp.Value.TryGetProperty("label", out var labelEl)) continue;

            var classes = new Dictionary<string, BuildClassGear>();
            foreach (var classProp in stageProp.Value.EnumerateObject())
            {
                if (classProp.Name is "label" or "label_en") continue;
                if (classProp.Value.ValueKind != JsonValueKind.Object) continue;

                var gear = new BuildClassGear
                {
                    Armor = ReadItemList(classProp.Value, "armor"),
                    Weapons = ReadItemList(classProp.Value, "weapons"),
                    Accessories = ReadItemList(classProp.Value, "accessories"),
                };
                classes[classProp.Name] = gear;
            }

            string? labelEn = stageProp.Value.TryGetProperty("label_en", out var labelEnEl)
                ? labelEnEl.GetString() : null;
            stages.Add(new BuildStage
            {
                Key = stageProp.Name,
                Label = labelEl.GetString() ?? stageProp.Name,
                LabelEn = labelEn,
                Classes = classes,
            });
        }

        return new BuildsCatalog(stages);
    }

    private static List<BuildItemRef> ReadItemList(JsonElement classObj, string propertyName)
    {
        if (!classObj.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return [];
        var list = new List<BuildItemRef>();
        foreach (var itemEl in arr.EnumerateArray())
        {
            var item = itemEl.Deserialize<BuildItemRef>();
            if (item != null) list.Add(item);
        }
        return list;
    }
}
