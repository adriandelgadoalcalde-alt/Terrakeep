using Terrakeep.Core.Data;
using Terrakeep.Core.Model;
using Terrakeep.Core.Nbt;

namespace Terrakeep.Core.Calamity;

// Traduce entre una entrada NBT de item de Calamity (tal y como vive en una lista de
// contenedor del .tplr) y un GameItem con su id sintetico resuelto. Campos y orden
// confirmados contra overrides.js/ItemIO real: mod, name, slot, prefix/modPrefixMod+
// modPrefixName, stack (solo si >1), globalData.
//
// LIMITACION HEREDADA A PROPOSITO (igual que la version JS): al codificar, la entrada se
// reconstruye desde cero con solo estos campos - cualquier campo "data" (distinto de
// globalData) que un mod usara se pierde. No se corrige aqui por fidelidad con el
// comportamiento ya verificado de la app actual.
public sealed class CalamityItemCodec(CalamityCatalog catalog, CalamityPrefixTranslator prefixTranslator)
{
    // Devuelve null si el mod+name no esta en el catalogo (item de un mod/version desconocida
    // - se descarta en vez de inventar un id, mismo criterio que el resto de esta capa).
    public (short Slot, GameItem Item)? Decode(NbtCompound entry)
    {
        string? mod = (entry.Get("mod") as NbtString)?.Value;
        string? name = (entry.Get("name") as NbtString)?.Value;
        short? slot = (entry.Get("slot") as NbtShort)?.Value;
        if (mod == null || name == null || slot == null) return null;

        var catalogEntry = catalog.ByModAndInternal(mod, name);
        if (catalogEntry == null) return null;

        int stack = (entry.Get("stack") as NbtInt)?.Value ?? 1;
        var globalData = entry.Get("globalData") as NbtList;
        var prefix = prefixTranslator.ReadFromEntry(entry);

        var item = new GameItem
        {
            Id = catalogEntry.SyntheticId,
            Count = stack,
            Prefix = prefix,
            GlobalData = globalData,
        };
        return (slot.Value, item);
    }

    public NbtCompound Encode(short slot, GameItem item)
    {
        if (!item.IsCalamity)
            throw new ArgumentException($"Item.Id={item.Id} no es un id sintetico de Calamity.", nameof(item));

        var catalogEntry = catalog.BySyntheticId(item.Id)
            ?? throw new InvalidOperationException($"Id sintetico de Calamity desconocido: {item.Id}");

        var fields = new List<(string, NbtTag)>
        {
            ("mod", new NbtString(catalogEntry.Mod)),
            ("name", new NbtString(catalogEntry.Internal)),
            ("slot", new NbtShort(slot)),
        };
        prefixTranslator.WriteToFields(fields, item.Prefix);
        if (item.Count > 1) fields.Add(("stack", new NbtInt(item.Count)));
        fields.Add(("globalData", item.GlobalData ?? new NbtList(NbtTagType.Compound)));

        return new NbtCompound(fields);
    }
}
