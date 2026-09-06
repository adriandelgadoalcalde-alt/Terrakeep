using System.Collections.Generic;
using System.Text;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.Services;

// La otra mitad del reparto Core/App para el tooltip de estadisticas - ronda de idioma del
// 6-sep-2026. `TerrasavrNative.Core/Data/ItemStatsFormatter` CALCULA (numeros reales, tramos,
// tipo de daño, piezas del set) y devuelve `ItemStatsInfo`, puros datos sin una sola palabra;
// esta clase, que ya vive del lado de la App y por tanto SI puede ver `LocalizationService`,
// REDACTA la frase final. Motivo de que la frontera este exactamente aqui: Core se compila
// tambien para net8.0 porque lo reutiliza el mod de tModLoader, asi que no puede depender de
// WPF ni de LocalizationService (ver TerrasavrNative.Core.csproj y el comentario largo de
// ItemStatsInfo.cs).
//
// Antes de esto, `ItemStatsFormatter.Format` devolvia el texto ya escrito en español a fuego, y
// con la app en ingles TODOS los tooltips de estadisticas (Objetos, Libreria, Builds, Novedades)
// se quedaban en español. El barrido de idioma no podia cazarlo porque son popups que el barrido
// nunca llega a abrir - lo dejo anotado la oleada de QA de Personaje->Objetos.
public static class ItemStatsTextBuilder
{
    private static LocalizationService Loc => LocalizationService.Instance;

    // Texto completo del tooltip, en el idioma activo AHORA MISMO. Null si no hay nada que
    // contar (WPF no muestra un ToolTip cuyo valor enlazado es null - mismo criterio de antes).
    // Se llama en cada lectura de la propiedad, no se cachea: asi cambiar de idioma en vivo se
    // refleja sin que nadie tenga que acordarse de invalidar nada.
    public static string? Build(ItemStatsInfo? info)
    {
        if (info == null) return null;

        var sections = new List<string>();

        // Seccion 1 - efecto del prefijo puesto.
        string? prefijo = BuildPrefixEffects(info.PrefixEffects);
        if (prefijo != null) sections.Add(prefijo);

        // Seccion 2 - estadisticas numericas, en el mismo orden real de siempre.
        var numeric = new StringBuilder();
        void Line(string text)
        {
            if (numeric.Length > 0) numeric.Append('\n');
            numeric.Append(text);
        }

        if (info.Damage is int d)
        {
            string linea = Loc.Format(DamageKey(info.DamageKind), d);
            if (info.Dps is int dps) linea = Loc.Format("stats_damage_dps", linea, dps);
            Line(linea);
        }
        if (info.Defense is int def) Line(Loc.Format("stats_defense", def));
        if (info.CritChance is int crit) Line(Loc.Format("stats_crit_chance", crit));
        if (info.UseTime is int ut && info.UseTimeTier is ItemUseTimeTier utTier)
            Line(Loc.Format("stats_use_time", ut, info.UseTimePerSecond, Loc[UseTimeTierKey(utTier)]));
        if (info.Knockback is string kb && info.KnockbackTier is ItemKnockbackTier kbTier)
            Line(Loc.Format("stats_knockback", kb, Loc[KnockbackTierKey(kbTier)]));
        if (info.ManaCost is int mana) Line(Loc.Format("stats_mana_cost", mana));
        if (info.HealLife is int hl) Line(Loc.Format("stats_heal_life", hl));
        if (info.HealMana is int hm) Line(Loc.Format("stats_heal_mana", hm));
        if (info.Rarity is int rare) Line(Loc.Format("stats_rarity", rare));
        if (numeric.Length > 0) sections.Add(numeric.ToString());

        // Seccion 3 - tooltip descriptivo real del juego (ya viene traducido del propio asset).
        if (info.DescriptiveTooltip != null) sections.Add(info.DescriptiveTooltip);

        // Seccion 4 - bono de set completo.
        if (info.SetBonusText != null)
        {
            sections.Add(Loc.Format("stats_set_bonus", info.SetBonusText));
        }
        else if (info.CalamitySetBonus is CalamitySetBonusInfo set)
        {
            string piezas = set.LegsName != null
                ? Loc.Format("stats_set_pieces_pair", set.BodyName, set.LegsName)
                : set.BodyName;
            var sb = new StringBuilder();
            sb.Append(Loc.Format("stats_set_bonus_with_pieces", piezas));
            foreach (var head in set.Heads)
            {
                sb.Append('\n');
                sb.Append(Loc.Format("stats_set_bonus_head", head.HeadName, head.BonusText));
            }
            sections.Add(sb.ToString());
        }

        return sections.Count > 0 ? string.Join("\n", sections) : null;
    }

    // Efectos de un prefijo, en una sola linea separada por comas (la coma es puntuacion, no
    // idioma). Se expone aparte porque el panel "Editar prefijo" lo usa suelto, sin el resto del
    // tooltip: cada boton de prefijo dice lo que hace (D-6). Null si no hay ninguno.
    public static string? BuildPrefixEffects(IReadOnlyList<ItemPrefixStatEffect> effects)
    {
        if (effects.Count == 0) return null;
        var parts = new List<string>(effects.Count);
        foreach (var e in effects) parts.Add(Loc.Format(PrefixStatKey(e.Stat), e.Amount));
        return string.Join(", ", parts);
    }

    private static string DamageKey(ItemDamageKind kind) => kind switch
    {
        ItemDamageKind.Melee => "stats_damage_melee",
        ItemDamageKind.Ranged => "stats_damage_ranged",
        ItemDamageKind.Magic => "stats_damage_magic",
        ItemDamageKind.Summon => "stats_damage_summon",
        ItemDamageKind.Rogue => "stats_damage_rogue",
        _ => "stats_damage_generic",
    };

    private static string UseTimeTierKey(ItemUseTimeTier tier) => tier switch
    {
        ItemUseTimeTier.InsanelyFast => "stats_use_time_insanely_fast",
        ItemUseTimeTier.VeryFast => "stats_use_time_very_fast",
        ItemUseTimeTier.Fast => "stats_use_time_fast",
        ItemUseTimeTier.Normal => "stats_use_time_normal",
        ItemUseTimeTier.Slow => "stats_use_time_slow",
        ItemUseTimeTier.VerySlow => "stats_use_time_very_slow",
        ItemUseTimeTier.ExtremelySlow => "stats_use_time_extremely_slow",
        _ => "stats_use_time_insanely_slow",
    };

    private static string KnockbackTierKey(ItemKnockbackTier tier) => tier switch
    {
        ItemKnockbackTier.ExtremelyWeak => "stats_knockback_extremely_weak",
        ItemKnockbackTier.VeryWeak => "stats_knockback_very_weak",
        ItemKnockbackTier.Weak => "stats_knockback_weak",
        ItemKnockbackTier.Normal => "stats_knockback_normal",
        ItemKnockbackTier.Strong => "stats_knockback_strong",
        ItemKnockbackTier.VeryStrong => "stats_knockback_very_strong",
        ItemKnockbackTier.ExtremelyStrong => "stats_knockback_extremely_strong",
        _ => "stats_knockback_insane",
    };

    private static string PrefixStatKey(ItemPrefixStat stat) => stat switch
    {
        ItemPrefixStat.Damage => "prefix_effect_damage",
        ItemPrefixStat.CritChance => "prefix_effect_crit_chance",
        ItemPrefixStat.Knockback => "prefix_effect_knockback",
        ItemPrefixStat.UseTime => "prefix_effect_use_time",
        ItemPrefixStat.Size => "prefix_effect_size",
        ItemPrefixStat.ShootSpeed => "prefix_effect_shoot_speed",
        ItemPrefixStat.ManaCost => "prefix_effect_mana_cost",
        ItemPrefixStat.Defense => "prefix_effect_defense",
        ItemPrefixStat.MaxMana => "prefix_effect_max_mana",
        ItemPrefixStat.MoveSpeed => "prefix_effect_move_speed",
        _ => "prefix_effect_melee_speed",
    };
}
