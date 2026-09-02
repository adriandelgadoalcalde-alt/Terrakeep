using System.Text.Json;
using System.Text.Json.Serialization;

namespace TerrasavrNative.Core.Data;

// Bonificacion real de set completo de armadura (pregunta a Opus sobre el diseño 2-sep-2026,
// cuarta pasada: "si tienes el set completo siempre hay una bonificacion... no se muestra al
// pasar el raton"). Fuente real: scripts/extraer-sets-armadura.py, que resuelve las ~66
// condiciones reales de Player.UpdateArmorSets (decompilado) contra el head/bodySlot/legSlot
// real de cada pieza (Item.cs) - nunca inventado.
//
// El tooltip de UNA pieza no puede saber si el jugador lleva las otras 2 puestas de verdad
// (StatsTooltip es estatico por id, lo consume tambien la Libreria sobre objetos sueltos) -
// por eso el texto se muestra siempre que se mira una pieza de un set real, con la etiqueta
// "(si llevas el set completo)" explicita en vez de fingir que ya esta activo. El chequeo en
// vivo (equipo puesto de verdad) es un problema aparte, ver ActiveSetBonus en
// EquipmentGroupViewModel si se implementa.
public sealed class ArmorSetInfo
{
    [JsonPropertyName("key")] public required string Key { get; init; }
    [JsonPropertyName("text")] public required string Text { get; init; }
    [JsonPropertyName("pieces")] public required int[] Pieces { get; init; }
}

public sealed class VanillaArmorSetCatalog
{
    private readonly Dictionary<int, ArmorSetInfo> _byItemId;

    private VanillaArmorSetCatalog(Dictionary<int, ArmorSetInfo> byItemId) => _byItemId = byItemId;

    public ArmorSetInfo? Get(int itemId) => _byItemId.TryGetValue(itemId, out var info) ? info : null;

    // Chequeo en vivo real: las 3 piezas puestas coinciden EXACTAMENTE con las de un set
    // conocido - para un futuro "bonificacion activa" reactivo (EquipmentGroupViewModel), sin
    // necesidad de guardar la tabla de slots por separado en la app.
    public ArmorSetInfo? BonusForEquipped(int headItemId, int bodyItemId, int legsItemId)
    {
        if (!_byItemId.TryGetValue(headItemId, out var info)) return null;
        return info.Pieces.Length == 3 && info.Pieces[0] == headItemId && info.Pieces[1] == bodyItemId && info.Pieces[2] == legsItemId
            ? info
            : null;
    }

    public static VanillaArmorSetCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static VanillaArmorSetCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, ArmorSetInfo>>(stream)
            ?? throw new InvalidDataException("vanilla_armor_sets.json invalido.");
        var byId = new Dictionary<int, ArmorSetInfo>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = value;
        return new VanillaArmorSetCatalog(byId);
    }
}
