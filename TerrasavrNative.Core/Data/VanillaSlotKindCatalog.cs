using System.Text.Json;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.Core.Data;

// Que SlotKind real tiene cada objeto vanilla (municion/moneda/tinte/gancho/montura/vagoneta/
// mascota/mascota de luz) - extraido de scripts/extraer-slot-kind-vanilla.py (bitmask real,
// fuentes citadas archivo:linea en la cabecera de ese script: Item.cs, Projectile.cs, Main.cs,
// Initializers/DyeInitializer.cs, todos decompilados de tModLoader 1.4.5.8 real).
//
// Regla obligatoria (consulta a Opus, sexta pasada): un objeto de Calamity SIEMPRE pasa la
// validacion de slot restringido, sea cual sea - CalamityCatalogEntryData no tiene ninguno de
// estos campos (ammo/mountType/buffType/dye/shoot), asi que validarlo de forma estricta
// bloquearia TODAS las monturas/mascotas/tintes reales de Calamity. Por eso GetKind solo se
// llama para ids vanilla; el llamador (ItemSlotViewModel.AcceptsItem) ya trata cualquier
// GameItem.IsCalamity==true como aceptado sin consultar este catalogo.
//
// Cobertura real conocida y documentada (no es un catalogo completo al 100%): el escaner de
// Item.cs solo encuentra el objeto dueño de un ~70-85% de las asignaciones reales (mismo techo
// que vanilla_categories.json, ver cabecera del script) - un id ausente de este catalogo NO
// esta restringido (GetKind devuelve None), nunca al reves.
public sealed class VanillaSlotKindCatalog
{
    private readonly Dictionary<int, SlotKind> _byId;

    private VanillaSlotKindCatalog(Dictionary<int, SlotKind> byId) => _byId = byId;

    public SlotKind GetKind(int itemId) => _byId.TryGetValue(itemId, out var kind) ? kind : SlotKind.None;

    public static VanillaSlotKindCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static VanillaSlotKindCatalog LoadFromStream(Stream stream)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, int>>(stream)
            ?? throw new InvalidDataException("vanilla_slot_kind.json invalido.");
        var byId = new Dictionary<int, SlotKind>(raw.Count);
        foreach (var (key, value) in raw)
            byId[int.Parse(key)] = (SlotKind)value;
        return new VanillaSlotKindCatalog(byId);
    }
}
