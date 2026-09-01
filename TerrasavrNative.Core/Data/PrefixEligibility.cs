using TerrasavrNative.Core.Model;

namespace TerrasavrNative.Core.Data;

// Punto unico que decide que categorias de prefijo aplican a un GameItem, vanilla o de
// Calamity - usado por el panel "Editar" para filtrar el picker de prefijos por tipo real
// de objeto (arregla "me esta permitiendo poner prefijo a objetos que no deberia").
public static class PrefixEligibility
{
    public static PrefixCategory For(GameItem item, PrefixRulesCatalog rules, CalamityCatalog calamityCatalog)
    {
        if (!item.IsCalamity)
            return rules.VanillaCategories(item.Id);

        var entry = calamityCatalog.BySyntheticId(item.Id);
        if (entry == null) return PrefixCategory.None;

        var category = entry.Category ?? "";
        var damageType = entry.Stats?.DamageType ?? "";

        if (category.StartsWith("Accessories", StringComparison.OrdinalIgnoreCase))
            return PrefixCategory.Accessory;

        if (category.StartsWith("Weapons/", StringComparison.OrdinalIgnoreCase) || category.StartsWith("Tools", StringComparison.OrdinalIgnoreCase))
        {
            var cat = PrefixCategory.AnyWeapon;
            if (damageType.Contains("Summon", StringComparison.OrdinalIgnoreCase)) cat |= PrefixCategory.Summon;
            else if (damageType.Contains("Melee", StringComparison.OrdinalIgnoreCase) || category.Contains("Melee", StringComparison.OrdinalIgnoreCase)) cat |= PrefixCategory.Melee;
            else if (damageType.Contains("Ranged", StringComparison.OrdinalIgnoreCase) || category.Contains("Ranged", StringComparison.OrdinalIgnoreCase)) cat |= PrefixCategory.Ranged;
            else if (damageType.Contains("Magic", StringComparison.OrdinalIgnoreCase) || category.Contains("Magic", StringComparison.OrdinalIgnoreCase)) cat |= PrefixCategory.Magic;
            // Rogue (DraedonsArsenal incluido) y cualquier otro tipo de daño real de Calamity
            // sin equivalente vanilla se quedan solo en AnyWeapon - documentado, no inventado.
            return cat;
        }

        return PrefixCategory.None;
    }
}
