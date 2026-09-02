using System.Globalization;
using System.Text;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.Core.Data;

// Los 6 catalogos reales que ItemStatsFormatter.Format necesita, agrupados en un unico record
// - pregunta a Opus sobre el diseño 2-sep-2026, cuarta pasada. Antes eran 5 parametros sueltos
// en la firma (ya dificil de no equivocarse al llamar) y con los 2 catalogos nuevos de esta
// pasada (tooltips descriptivos, sets de armadura) habria llegado a 7 - un solo record que
// CharacterFileService construye una vez y expone (TooltipCatalogs), y los 5 call sites reales
// (LibraryViewModel, BuildsViewModel, ItemSlotViewModel) lo pasan tal cual.
public sealed record ItemTooltipCatalogs(
    VanillaItemStatsCatalog Stats,
    CalamityCatalog Calamity,
    VanillaCategoryCatalog Categories,
    VanillaItemTooltipCatalog Tooltips,
    VanillaArmorSetCatalog ArmorSets,
    PrefixEffectCatalog PrefixEffects);

// Texto de tooltip con las estadisticas reales de un objeto (daño/defensa/etc.) - pedido
// explicito 1-sep-2026: "ninguna de las armas armaduras o accesorios... te muestran las
// estadisticas como en terrasav". Reescrito 2-sep-2026 tras feedback real comparando con
// capturas de Terrasavr: el formato anterior era demasiado plano ("Daño: 8") frente al real
// ("8 daño de cuerpo a cuerpo (~40 DPS)"). Formulas y texto en español REALES, extraidas de
// script.js (clase de formato de tooltip real, no adivinadas) y de
// local-site/lang/lang.zip -> Terrasavr.es-ES.json (namespace "meta.item") - ver
// bitacora.md para el detalle de la verificacion.
//
// Reescrito de nuevo 2-sep-2026 (cuarta pasada, pregunta a Opus sobre el diseño): pedido
// explicito del usuario ("las armaduras no te dicen toda la información... los accesorios
// tampoco... no te pone el porcentaje... y una pequeña descripción de lo que hace... si
// tienes el set completo siempre hay una bonificacion"). Bug real de raiz: FormatLines
// devolvia null si NINGUN campo NUMERICO estaba presente - un accesorio sin daño/defensa no
// generaba ninguna linea, aunque tuviera un efecto real descrito en el juego. Ahora compone
// por SECCIONES independientes (efecto de prefijo / stats numericos / tooltip descriptivo /
// bonus de set), cada una opcional, null solo si TODAS estan vacias.
public static class ItemStatsFormatter
{
    public static string? Format(bool isCalamity, int id, ItemTooltipCatalogs catalogs, ItemPrefix? prefix = null)
    {
        var sections = new List<string>();

        // Seccion 1 - efecto real del prefijo (numeros reales extraidos de
        // Item.TryGetPrefixStatMultipliersForItem/Player.GrantPrefixBenefits, nunca texto
        // inventado - ver PrefixEffectCatalog). Solo prefijos vanilla por ahora (Calamity no
        // se investigo esta pasada). El NOMBRE del prefijo ya se muestra aparte en el tooltip
        // compuesto de MainWindow.xaml - esto es solo el efecto numerico.
        if (prefix is { IsNone: false, IsCalamity: false } p)
        {
            string? effect = catalogs.PrefixEffects.Describe(p.VanillaId);
            if (effect != null) sections.Add(effect);
        }

        if (isCalamity)
        {
            var entry = catalogs.Calamity.BySyntheticId(id);
            var s = entry?.Stats;
            if (s != null)
            {
                string? numeric = FormatLines(damage: s.Damage, damageLabel: DamageLabelForCalamity(s.DamageType), defense: null,
                    crit: s.Crit, knockBack: s.KnockBack, useTime: s.UseTime, mana: s.Mana, healLife: null, healMana: null, rare: null);
                if (numeric != null) sections.Add(numeric);
            }
            // Descripcion textual/bonus de set de Calamity: no extraidos todavia (fuera de
            // alcance de esta pasada, ver bitacora.md - el .tmod real SI las trae, hjson de
            // Localization/en-US, solo en ingles en esta instalacion).
        }
        else
        {
            var v = catalogs.Stats.Get(id);
            if (v != null)
            {
                string? damageLabel = v.Damage is int ? DamageLabelForVanilla(catalogs.Categories.GetCategory(id)) : null;
                string? numeric = FormatLines(damage: v.Damage, damageLabel: damageLabel, defense: v.Defense, crit: v.Crit,
                    knockBack: v.KnockBack, useTime: v.UseTime, mana: v.Mana, healLife: v.HealLife, healMana: v.HealMana, rare: v.Rare);
                if (numeric != null) sections.Add(numeric);
            }

            // Seccion 3 - tooltip descriptivo real (texto ya con los porcentajes rellenados
            // por el propio juego, ej. "Aumenta un 15% el daño cuerpo a cuerpo") - antes un
            // accesorio sin stats de combate no mostraba NADA.
            string? tooltip = catalogs.Tooltips.Get(id);
            if (tooltip != null) sections.Add(tooltip);

            // Seccion 4 - bonus de set completo. Estatico por id (no comprueba el equipo
            // puesto de verdad - lo consume tambien la Libreria sobre objetos sueltos, sin
            // personaje cargado), etiquetado explicitamente para no fingir que ya esta activo.
            var setInfo = catalogs.ArmorSets.Get(id);
            if (setInfo != null)
                sections.Add($"Con el set completo: {setInfo.Text}");
        }

        return sections.Count > 0 ? string.Join("\n", sections) : null;
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
