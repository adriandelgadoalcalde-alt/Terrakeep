using System.Text.Json;
using System.Text.Json.Serialization;
using Terrakeep.Core.Calamity;

namespace Terrakeep.Core.Data;

// Pedido explicito del usuario (4-sep-2026): "la pestaña de buff no tiene nada de guardar json
// ni tampoco cargar para guardar combinaciones de buff" - gemelo real de ItemSetFile.cs (H5-03,
// "guardar/cargar conjuntos de objetos"), mismo formato JSON, misma logica de portabilidad
// entre catalogos (vanilla por id real, Calamity por mod+nombre interno - el id sintetico de
// este puerto no sobreviviria a una regeneracion del catalogo real).
public sealed class BuffSetSlotData
{
    // Vanilla: Id real (>0), Mod/Internal ambos null. Calamity: Id null, Mod/Internal rellenos.
    [JsonPropertyName("id")] public int? Id { get; init; }
    [JsonPropertyName("mod")] public string? Mod { get; init; }
    [JsonPropertyName("internal")] public string? Internal { get; init; }
    // Duracion real en ticks (PlrBuff.Time, 60 ticks/segundo real).
    [JsonPropertyName("time")] public int Time { get; init; }
}

public sealed class BuffSetFile
{
    public const string ResourceType = "TerrakeepBuffs";

    [JsonPropertyName("resourceType")] public string ResourceType_ { get; init; } = ResourceType;
    [JsonPropertyName("resourceVersion")] public string ResourceVersion { get; init; } = "1.0";
    // Un slot vacio real se guarda como null - mismo criterio real que ItemSetFile.
    [JsonPropertyName("slots")] public required List<BuffSetSlotData?> Slots { get; init; }

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static BuffSetFile FromBuffs(IEnumerable<(int Id, int Time)> buffs, CalamityBuffCatalog calamityBuffs) => new()
    {
        Slots = buffs.Select(b => ToSlotData(b.Id, b.Time, calamityBuffs)).ToList(),
    };

    public IReadOnlyList<(int Id, int Time)> ToBuffs(CalamityBuffCatalog calamityBuffs) =>
        Slots.Select(slot => FromSlotData(slot, calamityBuffs)).ToList();

    private static BuffSetSlotData? ToSlotData(int id, int time, CalamityBuffCatalog calamityBuffs)
    {
        if (id == 0) return null;

        string? mod = null, internalName = null;
        int? realId = null;
        if (id >= CalamityIds.BuffIdBase)
        {
            var entry = calamityBuffs.BySyntheticId(id);
            if (entry == null) return null; // id sintetico sin entrada real - nada real que guardar
            mod = entry.Mod;
            internalName = entry.Internal;
        }
        else
        {
            realId = id;
        }

        return new BuffSetSlotData { Id = realId, Mod = mod, Internal = internalName, Time = time };
    }

    private static (int Id, int Time) FromSlotData(BuffSetSlotData? data, CalamityBuffCatalog calamityBuffs)
    {
        if (data == null) return (0, 0);

        int? resolvedId = data.Id;
        if (resolvedId == null && data.Mod != null && data.Internal != null)
            resolvedId = calamityBuffs.ByModAndInternal(data.Mod, data.Internal)?.SyntheticId;
        // Ni id vanilla ni Calamity resoluble (mod desinstalado, catalogo cambiado) - slot
        // vacio en vez de inventar un buff que no es el real.
        if (resolvedId is not { } id || id <= 0) return (0, 0);

        return (id, Math.Max(data.Time, 0));
    }

    public byte[] Write() => JsonSerializer.SerializeToUtf8Bytes(this, WriteOptions);

    public static BuffSetFile Read(byte[] bytes)
    {
        var raw = JsonSerializer.Deserialize<BuffSetFile>(bytes)
            ?? throw new InvalidDataException("Fichero de conjunto de buffs invalido.");
        if (raw.ResourceType_ != ResourceType)
            throw new InvalidDataException($"No es un fichero de conjunto de buffs de Terrakeep real (resourceType='{raw.ResourceType_}').");
        return raw;
    }
}
