using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terrakeep.Core.Data;

// Indice real de sprite de armadura por id de objeto vanilla - pedido explicito del usuario
// (3-sep-2026): "los personajes de inicio no se visualizan como realmente son en el
// juego... que muestre el personaje con la vanidad que tiene cada uno pero fiel al
// guardado". Fuente real: scripts/extraer-slots-armadura-vanilla.py, valores LITERALES de
// headSlot/bodySlot/legSlot de Item.cs (1.4.5.8 decompilado) - el indice que Terraria usa
// para cargar Armor_Head_N.xnb/Armor_Legs_N.xnb/Images/Armor/Armor_N.xnb (el compuesto
// torso+brazo ya renderizado). Los sprites en si ya estan extraidos y recortados al frame
// de reposo en Assets/player/armor_{head,body,legs}/{N}.png (scripts/
// extraer-sprites-armadura-vanilla.js) - este catalogo solo resuelve QUE numero le
// corresponde a cada objeto, nunca el propio sprite.
public sealed class VanillaArmorSlotEntry
{
    [JsonPropertyName("h")] public int? Head { get; init; }
    [JsonPropertyName("b")] public int? Body { get; init; }
    [JsonPropertyName("l")] public int? Legs { get; init; }
}

public sealed class VanillaArmorSlotCatalog
{
    private readonly Dictionary<int, VanillaArmorSlotEntry> _byItemId;

    private VanillaArmorSlotCatalog(Dictionary<int, VanillaArmorSlotEntry> byItemId) => _byItemId = byItemId;

    public VanillaArmorSlotEntry? ById(int itemId) => _byItemId.TryGetValue(itemId, out var entry) ? entry : null;

    public static VanillaArmorSlotCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static VanillaArmorSlotCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, VanillaArmorSlotEntry>>(stream)
            ?? throw new InvalidDataException("vanilla_armor_slots.json invalido.");
        var byId = new Dictionary<int, VanillaArmorSlotEntry>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;
        return new VanillaArmorSlotCatalog(byId);
    }
}
