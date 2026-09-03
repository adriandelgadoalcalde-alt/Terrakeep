using System.Text.Json;
using System.Text.Json.Serialization;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.Core.Data;

// H5-03 (quinta auditoria de Opus): "no existe guardar/cargar conjuntos de objetos, que en el
// Terrasavr original SI es una funcion de primera clase" - app.io.IoSave/IoLoad reales
// (reference/terrasavr-real/script.beautified.js:5804-5880, ob.procItem:5864) escriben
// exactamente resourceType/id/count/prefix/isFavorited por slot, resourceType real
// "TerrasavrItems" (verificado en el propio JS, no adivinado). Aqui: "TerrakeepItems" a
// proposito (formato NUEVO, no el mismo bytes-a-bytes) - el motor original nunca tuvo
// Calamity, y un objeto de Calamity aqui se identifica por mod+nombre interno (nunca por el id
// sintetico de este puerto, que no sobreviviria a una regeneracion del catalogo real).
public sealed class ItemSetSlotData
{
    // Vanilla: Id real (>0), Mod/Internal ambos null. Calamity: Id null, Mod/Internal rellenos.
    [JsonPropertyName("id")] public int? Id { get; init; }
    [JsonPropertyName("mod")] public string? Mod { get; init; }
    [JsonPropertyName("internal")] public string? Internal { get; init; }
    [JsonPropertyName("count")] public int Count { get; init; }
    // Prefijo vanilla real (byte, 0 = ninguno) - igual que ob.procItem real.
    [JsonPropertyName("prefix")] public byte Prefix { get; init; }
    // Prefijo REAL de Calamity (Rogue) por nombre interno - el motor original no tenia esto,
    // pero es la misma idea real que "mod/internal" para el objeto: portable entre catalogos.
    [JsonPropertyName("prefixInternal")] public string? PrefixInternal { get; init; }
    [JsonPropertyName("favorited")] public bool Favorited { get; init; }
}

public sealed class ItemSetFile
{
    public const string ResourceType = "TerrakeepItems";

    [JsonPropertyName("resourceType")] public string ResourceType_ { get; init; } = ResourceType;
    [JsonPropertyName("resourceVersion")] public string ResourceVersion { get; init; } = "1.0";
    // Un slot vacio real se guarda como null - mismo criterio real que ob.procItem
    // (a.isEmpty() -> return null), nunca un id=0 inventado.
    [JsonPropertyName("slots")] public required List<ItemSetSlotData?> Slots { get; init; }

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static ItemSetFile FromItems(IEnumerable<GameItem> items, CalamityCatalog calamity, RoguePrefixCatalog roguePrefixes) => new()
    {
        Slots = items.Select(item => ToSlotData(item, calamity, roguePrefixes)).ToList(),
    };

    public IReadOnlyList<GameItem> ToItems(CalamityCatalog calamity, RoguePrefixCatalog roguePrefixes) =>
        Slots.Select(slot => FromSlotData(slot, calamity, roguePrefixes)).ToList();

    private static ItemSetSlotData? ToSlotData(GameItem item, CalamityCatalog calamity, RoguePrefixCatalog roguePrefixes)
    {
        if (item.IsEmpty) return null;

        string? mod = null, internalName = null;
        int? id = null;
        if (item.IsCalamity)
        {
            var entry = calamity.BySyntheticId(item.Id);
            if (entry == null) return null; // id sintetico sin entrada real - nada real que guardar
            mod = entry.Mod;
            internalName = entry.Internal;
        }
        else
        {
            id = item.Id;
        }

        string? prefixInternal = null;
        byte prefixByte = 0;
        if (item.Prefix.IsCalamity)
        {
            prefixInternal = roguePrefixes.ById(item.Prefix.SyntheticId)?.Internal;
        }
        else if (!item.Prefix.IsNone)
        {
            prefixByte = item.Prefix.VanillaId;
        }

        return new ItemSetSlotData
        {
            Id = id,
            Mod = mod,
            Internal = internalName,
            Count = item.Count,
            Prefix = prefixByte,
            PrefixInternal = prefixInternal,
            Favorited = item.Favorited,
        };
    }

    private static GameItem FromSlotData(ItemSetSlotData? data, CalamityCatalog calamity, RoguePrefixCatalog roguePrefixes)
    {
        if (data == null) return GameItem.Empty;

        int? resolvedId = data.Id;
        if (resolvedId == null && data.Mod != null && data.Internal != null)
            resolvedId = calamity.ByModAndInternal(data.Mod, data.Internal)?.SyntheticId;
        // Ni id vanilla ni Calamity resoluble (mod desinstalado, catalogo cambiado) - slot
        // vacio en vez de inventar un objeto que no es el real ("lo que no se encuentra no se
        // inventa", mismo criterio ya establecido en todo el proyecto).
        if (resolvedId is not { } id || id <= 0) return GameItem.Empty;

        var prefix = ItemPrefix.None;
        if (data.PrefixInternal != null)
        {
            var prefixEntry = roguePrefixes.ByInternal(data.PrefixInternal);
            if (prefixEntry != null) prefix = ItemPrefix.CalamitySynthetic(prefixEntry.Id);
        }
        else if (data.Prefix != 0)
        {
            prefix = ItemPrefix.Vanilla(data.Prefix);
        }

        return new GameItem { Id = id, Count = Math.Max(data.Count, 1), Prefix = prefix, Favorited = data.Favorited };
    }

    public byte[] Write() => JsonSerializer.SerializeToUtf8Bytes(this, WriteOptions);

    public static ItemSetFile Read(byte[] bytes)
    {
        var raw = JsonSerializer.Deserialize<ItemSetFile>(bytes)
            ?? throw new InvalidDataException("Fichero de conjunto de objetos invalido.");
        if (raw.ResourceType_ != ResourceType)
            throw new InvalidDataException($"No es un fichero de conjunto de objetos de Terrakeep real (resourceType='{raw.ResourceType_}').");
        return raw;
    }
}
