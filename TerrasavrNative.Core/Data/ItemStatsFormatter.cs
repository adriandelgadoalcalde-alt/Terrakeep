using System.Text;
using TerrasavrNative.Core.Calamity;

namespace TerrasavrNative.Core.Data;

// Texto de tooltip con las estadisticas reales de un objeto (daño/defensa/etc.) - pedido
// explicito 1-sep-2026: "ninguna de las armas armaduras o accesorios... te muestran las
// estadisticas como en terrasav". Vanilla usa VanillaItemStatsCatalog (extraido de Item.cs
// real, ver scripts/extraer-estadisticas-vanilla.py); Calamity usa CalamityItemStats, ya
// presente en catalog.json desde antes pero sin explotar en la UI. Devuelve null si el objeto
// no tiene ninguna estadistica real conocida (no se inventa un "0" para objetos sin combate).
public static class ItemStatsFormatter
{
    public static string? Format(bool isCalamity, int id, VanillaItemStatsCatalog vanillaStats, CalamityCatalog calamityCatalog)
    {
        if (isCalamity)
        {
            var entry = calamityCatalog.BySyntheticId(id);
            var s = entry?.Stats;
            if (s == null) return null;
            return FormatLines(damage: s.Damage, defense: null, crit: s.Crit, knockBack: s.KnockBack,
                useTime: s.UseTime, mana: s.Mana, healLife: null, healMana: null, rare: null, damageType: s.DamageType);
        }

        var v = vanillaStats.Get(id);
        if (v == null) return null;
        return FormatLines(damage: v.Damage, defense: v.Defense, crit: v.Crit, knockBack: v.KnockBack,
            useTime: v.UseTime, mana: v.Mana, healLife: v.HealLife, healMana: v.HealMana, rare: v.Rare, damageType: null);
    }

    private static string? FormatLines(int? damage, int? defense, int? crit, double? knockBack, int? useTime,
        int? mana, int? healLife, int? healMana, int? rare, string? damageType)
    {
        var sb = new StringBuilder();
        void Line(string text)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(text);
        }

        if (damage is int d) Line(damageType != null ? $"Daño: {d} ({damageType})" : $"Daño: {d}");
        if (defense is int def) Line($"Defensa: {def}");
        if (crit is int c) Line($"Prob. de crítico: +{c}%");
        if (knockBack is double kb) Line($"Nudillo: {kb:0.##}");
        if (useTime is int ut) Line($"Velocidad de uso: {ut}");
        if (mana is int m) Line($"Coste de maná: {m}");
        if (healLife is int hl) Line($"Restaura vida: {hl}");
        if (healMana is int hm) Line($"Restaura maná: {hm}");
        if (rare is int r) Line($"Rareza: {r}");

        return sb.Length > 0 ? sb.ToString() : null;
    }
}
