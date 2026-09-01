using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.Nbt;

namespace TerrasavrNative.Core.Calamity;

// Traduce el campo de prefijo entre su forma NBT real (byte "prefix" vanilla, o par de
// strings "modPrefixMod"/"modPrefixName" para un prefijo real de mod) y el ItemPrefix
// sintetico que usa esta app - mismo mecanismo que calamityReadPrefixField/
// calamityPushPrefixField en overrides.js (confirmado byte a byte).
public sealed class CalamityPrefixTranslator(RoguePrefixCatalog catalog)
{
    public ItemPrefix ReadFromEntry(NbtCompound entry)
    {
        string? modPrefixMod = (entry.Get("modPrefixMod") as NbtString)?.Value;
        string? modPrefixName = (entry.Get("modPrefixName") as NbtString)?.Value;

        if (modPrefixMod != null && modPrefixName != null)
        {
            if (modPrefixMod == "CalamityMod")
            {
                var found = catalog.ByInternal(modPrefixName);
                if (found != null) return ItemPrefix.CalamitySynthetic(found.Id);
            }
            // Prefijo modded de otro mod, o de Calamity pero no reconocido: se deja sin
            // prefijo, no se inventa un mapeo (mismo criterio que la version JS).
            return ItemPrefix.None;
        }

        var plain = entry.Get("prefix") as NbtByte;
        byte plainValue = plain != null ? unchecked((byte)plain.Value) : (byte)0;
        return plainValue != 0 ? ItemPrefix.Vanilla(plainValue) : ItemPrefix.None;
    }

    // Anade los campos NBT correspondientes al prefijo (0, 1 o 2 campos segun el caso) a una
    // lista de campos que se esta construyendo para un item.
    public void WriteToFields(List<(string Name, NbtTag Tag)> fields, ItemPrefix prefix)
    {
        if (prefix.IsCalamity)
        {
            var entry = catalog.ById(prefix.SyntheticId);
            if (entry == null) return; // id sintetico invalido/obsoleto: no se escribe nada, no se inventa
            fields.Add(("modPrefixMod", new NbtString("CalamityMod")));
            fields.Add(("modPrefixName", new NbtString(entry.Internal)));
            return;
        }
        if (prefix.VanillaId != 0)
            fields.Add(("prefix", new NbtByte(unchecked((sbyte)prefix.VanillaId))));
    }
}
