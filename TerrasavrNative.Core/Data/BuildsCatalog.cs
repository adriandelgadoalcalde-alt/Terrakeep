using System.Text.Json;
using System.Text.Json.Serialization;

namespace TerrasavrNative.Core.Data;

public sealed class BuildItemRef
{
    [JsonPropertyName("pid")] public string? Pid { get; init; }
    [JsonPropertyName("en")] public string? En { get; init; }
    [JsonPropertyName("es")] public string? Es { get; init; }
    [JsonPropertyName("prefix")] public string? Prefix { get; init; }

    public string DisplayName => Es ?? En ?? Pid ?? "?";
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
    // Clave = nombre de clase (melee/ranged/mage.../rogue en Calamity) tal cual viene en el JSON.
    public Dictionary<string, BuildClassGear> Classes { get; init; } = [];
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
                if (classProp.Name == "label") continue;
                if (classProp.Value.ValueKind != JsonValueKind.Object) continue;

                var gear = new BuildClassGear
                {
                    Armor = ReadItemList(classProp.Value, "armor"),
                    Weapons = ReadItemList(classProp.Value, "weapons"),
                    Accessories = ReadItemList(classProp.Value, "accessories"),
                };
                classes[classProp.Name] = gear;
            }

            stages.Add(new BuildStage { Key = stageProp.Name, Label = labelEl.GetString() ?? stageProp.Name, Classes = classes });
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
