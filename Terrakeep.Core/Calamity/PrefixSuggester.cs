using Terrakeep.Core.Data;
using Terrakeep.Core.Model;

namespace Terrakeep.Core.Calamity;

// Boton "mejor prefijo" (la estrella en la app JS): sugiere el prefijo real mas fuerte para
// cualquier objeto, vanilla o de Calamity. Fuente de datos:
// - vanilla: BestPrefixCatalog.vanilla (por id numerico).
// - Calamity, armas Picaro (RogueDamageClass real, detectado por damageType): el prefijo
//   REAL mas fuerte de los 21 propios de Calamity (RoguePrefixCatalog.Best.Weapon -
//   "Flawless"/"Impecable"), no la tabla generica.
// - resto de Calamity: BestPrefixCatalog.calamity (por nombre interno) - ya cubre el caso de
//   "accesorios siempre sugieren Amenazante" al ser una tabla generada contra la wiki real.
public static class PrefixSuggester
{
    public static ItemPrefix? Suggest(GameItem item, CalamityCatalog calamityCatalog, BestPrefixCatalog bestPrefixCatalog, RoguePrefixCatalog roguePrefixCatalog)
    {
        if (!item.IsCalamity)
        {
            var vanillaBest = bestPrefixCatalog.BestVanillaPrefix(item.Id);
            return vanillaBest.HasValue ? ItemPrefix.Vanilla(vanillaBest.Value) : null;
        }

        var entry = calamityCatalog.BySyntheticId(item.Id);
        if (entry == null) return null;

        bool isRogueWeapon = entry.Stats?.DamageType?.Contains("Rogue", StringComparison.OrdinalIgnoreCase) == true;
        if (isRogueWeapon)
        {
            var rogueBest = roguePrefixCatalog.ByInternal(roguePrefixCatalog.Best.Weapon);
            if (rogueBest != null) return ItemPrefix.CalamitySynthetic(rogueBest.Id);
        }

        var calBest = bestPrefixCatalog.BestCalamityPrefix(entry.Internal);
        return calBest.HasValue ? ItemPrefix.Vanilla(calBest.Value) : null;
    }
}
