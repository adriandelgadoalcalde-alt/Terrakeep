using System.Globalization;
using System.Text;
using TerrasavrNative.Core.Calamity;

namespace TerrasavrNative.Core.Data;

// Texto de tooltip con las estadisticas reales de un objeto (daño/defensa/etc.) - pedido
// explicito 1-sep-2026: "ninguna de las armas armaduras o accesorios... te muestran las
// estadisticas como en terrasav". Reescrito 2-sep-2026 tras feedback real comparando con
// capturas de Terrasavr: el formato anterior era demasiado plano ("Daño: 8") frente al real
// ("8 daño de cuerpo a cuerpo (~40 DPS)"). Formulas y texto en español REALES, extraidas de
// script.js (clase de formato de tooltip real, no adivinadas) y de
// local-site/lang/lang.zip -> Terrasavr.es-ES.json (namespace "meta.item") - ver
// bitacora.md para el detalle de la verificacion.
public static class ItemStatsFormatter
{
    public static string? Format(bool isCalamity, int id, VanillaItemStatsCatalog vanillaStats, CalamityCatalog calamityCatalog, VanillaCategoryCatalog vanillaCategories)
    {
        if (isCalamity)
        {
            var entry = calamityCatalog.BySyntheticId(id);
            var s = entry?.Stats;
            if (s == null) return null;
            return FormatLines(damage: s.Damage, damageLabel: DamageLabelForCalamity(s.DamageType), defense: null,
                crit: s.Crit, knockBack: s.KnockBack, useTime: s.UseTime, mana: s.Mana, healLife: null, healMana: null, rare: null);
        }

        var v = vanillaStats.Get(id);
        if (v == null) return null;
        string? damageLabel = v.Damage is int ? DamageLabelForVanilla(vanillaCategories.GetCategory(id)) : null;
        return FormatLines(damage: v.Damage, damageLabel: damageLabel, defense: v.Defense, crit: v.Crit,
            knockBack: v.KnockBack, useTime: v.UseTime, mana: v.Mana, healLife: v.HealLife, healMana: v.HealMana, rare: v.Rare);
    }

    // Etiqueta de tipo de daño real - vanilla la decide la categoria real ya extraida
    // (VanillaCategoryCatalog, de los mismos bloques SetDefaults# de Item.cs), Calamity la
    // decide el DamageType real (ej. "DamageClass.MeleeNoSpeed") por subcadena en vez de
    // mostrarlo en crudo como antes.
    private static string? DamageLabelForVanilla(string category) => category switch
    {
        "Armas/Cuerpo a cuerpo" => "daño de cuerpo a cuerpo",
        "Armas/A distancia" => "daño por rango",
        "Armas/Magia" => "daño magico",
        "Armas/Invocacion" => "daño de invocacion",
        _ => null,
    };

    private static string? DamageLabelForCalamity(string? damageType)
    {
        if (string.IsNullOrEmpty(damageType)) return null;
        if (damageType.Contains("Rogue", StringComparison.OrdinalIgnoreCase)) return "daño Picaro";
        if (damageType.Contains("Melee", StringComparison.OrdinalIgnoreCase)) return "daño de cuerpo a cuerpo";
        if (damageType.Contains("Ranged", StringComparison.OrdinalIgnoreCase)) return "daño por rango";
        if (damageType.Contains("Magic", StringComparison.OrdinalIgnoreCase)) return "daño magico";
        if (damageType.Contains("Summon", StringComparison.OrdinalIgnoreCase)) return "daño de invocacion";
        return null;
    }

    private static string? FormatLines(int? damage, string? damageLabel, int? defense, int? crit, double? knockBack,
        int? useTime, int? mana, int? healLife, int? healMana, int? rare)
    {
        var sb = new StringBuilder();
        void Line(string text)
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(text);
        }

        if (damage is int d)
        {
            string line = damageLabel != null ? $"{d} {damageLabel}" : $"{d} de daño";
            if (useTime is int dpsUseTime && dpsUseTime > 0)
                line += $" (~{Math.Round(60.0 * d / dpsUseTime)} DPS)";
            Line(line);
        }
        if (defense is int def) Line($"{def} defensa");
        if (crit is int c) Line($"{c}% chance de golpe critico");
        // Velocidad de uso y retroceso solo se muestran junto al daño (objetos de combate
        // real) - todo objeto colocable/consumible tiene tambien un useTime real en
        // Item.cs (velocidad de animacion al colocar/beber), pero mostrarlo ahi como si
        // fuera relevante para combate confundia mas que ayudaba (bug real reportado:
        // "Tierra"/pociones mostraban "Use time" sin venir a cuento).
        if (damage.HasValue)
        {
            if (useTime is int ut) Line(FormatUseTime(ut));
            if (knockBack is double kb) Line(FormatKnockback(kb));
        }
        if (mana is int m) Line($"Costo {m} mana");
        if (healLife is int hl) Line($"Restaura {hl} de Vida");
        if (healMana is int hm) Line($"Restaura {hm} de Mana");
        if (rare is int r) Line($"Rareza {r}");

        return sb.Length > 0 ? sb.ToString() : null;
    }

    // "Use time $1 ($2/s, $3)" real - ritmo por segundo = floor(6000/useTime)/100 (real,
    // confirmado: useTime=12 -> floor(500)/100 = 5), descriptor de 8 tramos reales (texto
    // real de Terrasavr.es-ES.json, namespace meta.item.useTime.*).
    private static string FormatUseTime(int useTime)
    {
        double perSecond = Math.Floor(6000.0 / useTime) / 100.0;
        string descriptor = useTime switch
        {
            <= 8 => "Locamente Rapido",
            <= 20 => "Muy Rapido",
            <= 25 => "Rapido",
            <= 30 => "Normal",
            <= 35 => "Lento",
            <= 45 => "Muy Lento",
            <= 55 => "Extremadamente Lento",
            _ => "Locamente Lento",
        };
        return $"Use time {useTime} ({perSecond.ToString("0.##", CultureInfo.InvariantCulture)}/s, {descriptor})";
    }

    // "Retroceso $1 ($2)" real, 8 tramos reales (texto real de Terrasavr.es-ES.json,
    // namespace meta.item.knockback.* - incluido el typo real "Extremandamente" en el tramo
    // mas debil, no es nuestro, no se corrige).
    private static string FormatKnockback(double knockBack)
    {
        string descriptor = knockBack switch
        {
            <= 1.5 => "Extremandamente Debil",
            <= 3 => "Muy Debil",
            <= 4 => "Debil",
            <= 6 => "Normal",
            <= 7 => "Fuerte",
            <= 9 => "Muy fuerte",
            <= 11 => "Extremadamente Fuerte",
            _ => "Demente",
        };
        return $"Retroceso {knockBack.ToString("0.##", CultureInfo.InvariantCulture)} ({descriptor})";
    }
}
