using System.Text.Json;
using System.Text.Json.Serialization;

namespace TerrasavrNative.Core.Data;

public sealed class HairDyeEntryData
{
    [JsonPropertyName("index")] public required int Index { get; init; }
    [JsonPropertyName("itemId")] public required int ItemId { get; init; }
}

// Los 12 tintes de pelo reales (PlrCharacter.HairDye, byte 0-255, 0 = ninguno) - indice y
// objeto real extraidos de DyeInitializer.cs/HairShaderDataSet.cs (tModLoader decompilado
// real, ver scripts/extraer-tintes-pelo.py). Antes de esto el campo era un TextBox numerico
// puro, sin sprite ni nombre visibles ("Apariencia sigue siendo por ID", pedido explicito
// 1-sep-2026).
public sealed class HairDyeCatalog
{
    public IReadOnlyList<HairDyeEntryData> Entries { get; }

    private HairDyeCatalog(List<HairDyeEntryData> entries) => Entries = entries;

    public static HairDyeCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static HairDyeCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<List<HairDyeEntryData>>(stream)
            ?? throw new InvalidDataException("hair_dyes.json invalido.");
        return new HairDyeCatalog(raw.OrderBy(e => e.Index).ToList());
    }
}
