using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terrakeep.Core.Data;

// Indice real de sprite de ACCESORIO por id de objeto vanilla - PortSeleccion Encargo1
// (25-sep-2026), hermano directo de VanillaArmorSlotCatalog (mismo criterio, extendido a los
// 7 tipos de accesorio funcional/vanidad: waist/neck/handOn/handOff/back/shield/face).
// Fuente real: scripts/extraer-slots-accesorios-vanilla.py, valores LITERALES de
// waistSlot/neckSlot/handOnSlot/handOffSlot/backSlot/shieldSlot/faceSlot de Item.cs
// (decompilado real) - el indice que Terraria usa para cargar Acc_Waist_N.xnb/Acc_Neck_N.xnb/
// Acc_HandsOn_N.xnb/Acc_HandsOff_N.xnb/Acc_Back_N.xnb/Acc_Shield_N.xnb/Acc_Face_N.xnb. Los
// sprites en si ya estan extraidos (sin recortar, hoja XNB->PNG real) en
// Assets/player/acc_{waist,neck,handon,handoff,back,shield,face}/{N}.png (scripts/
// extraer-sprites-accesorios-vanilla.js) - este catalogo solo resuelve QUE numero le
// corresponde a cada objeto y de QUE TIPO es, nunca el propio sprite.
//
// A diferencia de VanillaArmorSlotCatalog (donde el TIPO ya lo decide la posicion 0/1/2 del
// loadout), aqui el TIPO lo decide el propio objeto (confirmado en Player.cs real,
// UpdateVisibleAccessory: "if (item.waistSlot > 0) waist = item.waistSlot;" y analogo para
// los otros 6 campos, sin relacion con en cual de los 7 huecos genericos del loadout este
// puesto) - por eso esta entrada puede tener MAS de un campo relleno en teoria (un objeto que
// fuera a la vez, por ejemplo, accesorio de cuello Y de espalda), aunque en la practica cada
// accesorio real de Terraria solo asigna uno.
public sealed class VanillaAccessorySlotEntry
{
    [JsonPropertyName("w")] public int? Waist { get; init; }
    [JsonPropertyName("n")] public int? Neck { get; init; }
    [JsonPropertyName("ho")] public int? HandOn { get; init; }
    [JsonPropertyName("hf")] public int? HandOff { get; init; }
    [JsonPropertyName("bk")] public int? Back { get; init; }
    [JsonPropertyName("s")] public int? Shield { get; init; }
    [JsonPropertyName("fc")] public int? Face { get; init; }
}

public sealed class VanillaAccessorySlotCatalog
{
    private readonly Dictionary<int, VanillaAccessorySlotEntry> _byItemId;

    private VanillaAccessorySlotCatalog(Dictionary<int, VanillaAccessorySlotEntry> byItemId) => _byItemId = byItemId;

    public VanillaAccessorySlotEntry? ById(int itemId) => _byItemId.TryGetValue(itemId, out var entry) ? entry : null;

    public static VanillaAccessorySlotCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static VanillaAccessorySlotCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, VanillaAccessorySlotEntry>>(stream)
            ?? throw new InvalidDataException("vanilla_accessory_slots.json invalido.");
        var byId = new Dictionary<int, VanillaAccessorySlotEntry>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;
        return new VanillaAccessorySlotCatalog(byId);
    }
}
