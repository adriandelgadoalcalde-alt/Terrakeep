using System.Globalization;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.Core.Data;

// Los 6 catalogos reales que ItemStatsFormatter.Describe necesita, agrupados en un unico record
// - pregunta a Opus sobre el diseño 2-sep-2026, cuarta pasada. Antes eran 5 parametros sueltos
// en la firma (ya dificil de no equivocarse al llamar) y con los 2 catalogos nuevos de esa
// pasada (tooltips descriptivos, sets de armadura) habria llegado a 7 - un solo record que
// CharacterFileService construye una vez y expone (TooltipCatalogs), y los call sites reales
// (LibraryViewModel, BuildsViewModel, ItemSlotViewModel, WhatsNewItemViewModel) lo pasan tal cual.
public sealed record ItemTooltipCatalogs(
    VanillaItemStatsCatalog Stats,
    CalamityCatalog Calamity,
    VanillaCategoryCatalog Categories,
    VanillaItemTooltipCatalog Tooltips,
    VanillaArmorSetCatalog ArmorSets,
    // C-10b/C-10c (auditoria de pulido final): catalogo de SET completo de Calamity - permite
    // mostrar el bono en CUALQUIER pieza del set (antes solo el casco, unico con SetBonus real).
    CalamityArmorSetCatalog CalamityArmorSets,
    PrefixEffectCatalog PrefixEffects);

// Estadisticas reales de un objeto (daño/defensa/etc.) - pedido explicito 1-sep-2026: "ninguna
// de las armas armaduras o accesorios... te muestran las estadisticas como en terrasav".
// Formulas y tramos REALES, extraidos de script.js (clase de formato de tooltip real, no
// adivinadas) y de local-site/lang/lang.zip -> Terrasavr.es-ES.json (namespace "meta.item") -
// ver bitacora.md para el detalle de la verificacion.
//
// Compone por SECCIONES independientes (efecto de prefijo / stats numericos / tooltip
// descriptivo / bonus de set), cada una opcional; devuelve null solo si TODAS estan vacias -
// bug real de raiz de la cuarta pasada: un accesorio sin daño/defensa no generaba ninguna linea
// aunque tuviera un efecto real descrito en el juego.
//
// Ronda de idioma del 6-sep-2026 - CAMBIO DE CONTRATO: antes esto devolvia un `string` ya
// redactado en español a fuego ("Daño:", "Con el set completo:", "Muy Rapido"...), asi que con
// la app en ingles los tooltips seguian en español y no habia forma de arreglarlo sin meter
// `LocalizationService` (WPF, App) dentro de Core - imposible, Core se compila TAMBIEN para
// net8.0 para el mod de tModLoader. Ahora devuelve DATOS (`ItemStatsInfo`: numeros + enums) y es
// `TerrasavrNative.App/Services/ItemStatsTextBuilder` quien redacta la frase final con las
// claves de idioma. Ver el comentario largo de ItemStatsInfo.cs.
public static class ItemStatsFormatter
{
    public static ItemStatsInfo? Describe(bool isCalamity, int id, ItemTooltipCatalogs catalogs, ItemPrefix? prefix = null)
    {
        var info = new ItemStatsInfo();

        // Seccion 1 - efecto real del prefijo (numeros reales extraidos de
        // Item.TryGetPrefixStatMultipliersForItem/Player.GrantPrefixBenefits, nunca texto
        // inventado - ver PrefixEffectCatalog). Solo prefijos vanilla por ahora (Calamity no
        // se investigo esta pasada). El NOMBRE del prefijo ya se muestra aparte en el tooltip
        // compuesto de MainWindow.xaml - esto es solo el efecto numerico.
        if (prefix is { IsNone: false, IsCalamity: false } p)
            info = info with { PrefixEffects = catalogs.PrefixEffects.Effects(p.VanillaId) };

        if (isCalamity)
        {
            var entry = catalogs.Calamity.BySyntheticId(id);
            var s = entry?.Stats;
            if (s != null)
            {
                // Defensa real (pedido explicito del usuario: "las armaduras de calamity no
                // dicen especificaciones cuando pasas el raton") - antes hardcodeada a null
                // aqui, pero el hueco real era mas profundo: catalog.json no tenia defense
                // para NINGUN objeto de Calamity (0 de 186 armaduras reales, comprobado antes
                // de tocar nada) - ver scripts/extraer-defensa-calamity.js, que la extrae de
                // verdad de Item.defense en el SetDefaults() real de cada objeto.
                info = WithNumericStats(info, damage: s.Damage, damageKind: DamageKindForCalamity(s.DamageType),
                    defense: s.Defense, crit: s.Crit, knockBack: s.KnockBack, useTime: s.UseTime,
                    mana: s.Mana, healLife: null, healMana: null, rare: null);
            }
            // Bono de set completo real (pedido explicito del usuario: "la bonificacion por el
            // set no [aparece]") - texto real ya resuelto y traducido, ver
            // scripts/extraer-bonos-set-calamity.js y CalamityCatalogEntryData.SetBonus. Solo
            // los cascos tienen su PROPIO SetBonus real (el modelo de datos original de Calamity
            // - 0/131 cuerpos/piernas lo tienen nunca).
            if (entry?.SetBonus != null)
            {
                info = info with { SetBonusText = entry.SetBonus };
            }
            else
            {
                // C-10c (auditoria de pulido final, cierra L3-b): body/legs de un set real de
                // Calamity se quedaban completamente mudos - "codigo verificado una vez y nunca
                // funciono" (ActiveCalamitySetBonusText comparaba tres SetBonus que nunca podian
                // coincidir). El catalogo POR SET (CalamityArmorSetCatalog, C-10b) enumera aqui
                // el bono de CADA variante de casco real del mismo set, mirando cualquier pieza.
                var setInfo = catalogs.CalamityArmorSets.FindSetContaining(id);
                if (setInfo != null && setInfo.Heads.Count > 0)
                {
                    var bodyEntry = catalogs.Calamity.BySyntheticId(setInfo.BodySyntheticId);
                    var legsEntry = setInfo.LegsSyntheticId is int legsId ? catalogs.Calamity.BySyntheticId(legsId) : null;
                    var heads = new List<CalamitySetBonusHead>(setInfo.Heads.Count);
                    foreach (var (headId, bonusText) in setInfo.Heads)
                    {
                        string headName = catalogs.Calamity.BySyntheticId(headId)?.DisplayName ?? $"#{headId}";
                        heads.Add(new CalamitySetBonusHead(headName, bonusText));
                    }
                    info = info with
                    {
                        CalamitySetBonus = new CalamitySetBonusInfo(
                            bodyEntry?.DisplayName ?? string.Empty, legsEntry?.DisplayName, heads),
                    };
                }
            }
        }
        else
        {
            var v = catalogs.Stats.Get(id);
            if (v != null)
            {
                var damageKind = v.Damage is int ? DamageKindForVanilla(catalogs.Categories.GetCategory(id)) : ItemDamageKind.Unknown;
                info = WithNumericStats(info, damage: v.Damage, damageKind: damageKind, defense: v.Defense,
                    crit: v.Crit, knockBack: v.KnockBack, useTime: v.UseTime, mana: v.Mana,
                    healLife: v.HealLife, healMana: v.HealMana, rare: v.Rare);
            }

            // Seccion 3 - tooltip descriptivo real (texto ya con los porcentajes rellenados
            // por el propio juego, ej. "Aumenta un 15% el daño cuerpo a cuerpo") - antes un
            // accesorio sin stats de combate no mostraba NADA.
            info = info with { DescriptiveTooltip = catalogs.Tooltips.Get(id) };

            // Seccion 4 - bonus de set completo. Estatico por id (no comprueba el equipo
            // puesto de verdad - lo consume tambien la Libreria sobre objetos sueltos, sin
            // personaje cargado), etiquetado explicitamente por la App para no fingir que ya
            // esta activo.
            var setInfo = catalogs.ArmorSets.Get(id);
            if (setInfo != null) info = info with { SetBonusText = setInfo.Text };
        }

        return info.IsEmpty ? null : info;
    }

    // Tipo de daño real - vanilla lo decide la categoria real ya extraida
    // (VanillaCategoryCatalog, de los mismos bloques SetDefaults# de Item.cs), Calamity lo
    // decide el DamageType real (ej. "DamageClass.MeleeNoSpeed") por subcadena en vez de
    // mostrarlo en crudo como antes.
    private static ItemDamageKind DamageKindForVanilla(string category) => category switch
    {
        "Armas/Cuerpo a cuerpo" => ItemDamageKind.Melee,
        "Armas/A distancia" => ItemDamageKind.Ranged,
        "Armas/Magia" => ItemDamageKind.Magic,
        "Armas/Invocacion" => ItemDamageKind.Summon,
        _ => ItemDamageKind.Unknown,
    };

    private static ItemDamageKind DamageKindForCalamity(string? damageType)
    {
        if (string.IsNullOrEmpty(damageType)) return ItemDamageKind.Unknown;
        if (damageType.Contains("Rogue", StringComparison.OrdinalIgnoreCase)) return ItemDamageKind.Rogue;
        if (damageType.Contains("Melee", StringComparison.OrdinalIgnoreCase)) return ItemDamageKind.Melee;
        if (damageType.Contains("Ranged", StringComparison.OrdinalIgnoreCase)) return ItemDamageKind.Ranged;
        if (damageType.Contains("Magic", StringComparison.OrdinalIgnoreCase)) return ItemDamageKind.Magic;
        if (damageType.Contains("Summon", StringComparison.OrdinalIgnoreCase)) return ItemDamageKind.Summon;
        return ItemDamageKind.Unknown;
    }

    private static ItemStatsInfo WithNumericStats(ItemStatsInfo info, int? damage, ItemDamageKind damageKind,
        int? defense, int? crit, double? knockBack, int? useTime, int? mana, int? healLife, int? healMana, int? rare)
    {
        info = info with
        {
            Damage = damage,
            DamageKind = damage is int ? damageKind : ItemDamageKind.Unknown,
            Dps = damage is int d && useTime is int dpsUseTime && dpsUseTime > 0
                ? (int)Math.Round(60.0 * d / dpsUseTime)
                : null,
            Defense = defense,
            CritChance = crit,
            ManaCost = mana,
            HealLife = healLife,
            HealMana = healMana,
            Rarity = rare,
        };

        // Velocidad de uso y retroceso solo se muestran junto al daño (objetos de combate
        // real) - todo objeto colocable/consumible tiene tambien un useTime real en
        // Item.cs (velocidad de animacion al colocar/beber), pero mostrarlo ahi como si
        // fuera relevante para combate confundia mas que ayudaba (bug real reportado:
        // "Tierra"/pociones mostraban "Use time" sin venir a cuento).
        if (damage.HasValue)
        {
            if (useTime is int ut)
            {
                info = info with
                {
                    UseTime = ut,
                    UseTimePerSecond = FormatNumber(Math.Floor(6000.0 / ut) / 100.0),
                    UseTimeTier = TierForUseTime(ut),
                };
            }
            if (knockBack is double kb)
            {
                info = info with
                {
                    Knockback = FormatNumber(kb),
                    KnockbackTier = TierForKnockback(kb),
                };
            }
        }

        return info;
    }

    // Los 8 tramos reales de velocidad de uso (el ritmo por segundo = floor(6000/useTime)/100 es
    // real y esta confirmado: useTime=12 -> floor(500)/100 = 5).
    private static ItemUseTimeTier TierForUseTime(int useTime) => useTime switch
    {
        <= 8 => ItemUseTimeTier.InsanelyFast,
        <= 20 => ItemUseTimeTier.VeryFast,
        <= 25 => ItemUseTimeTier.Fast,
        <= 30 => ItemUseTimeTier.Normal,
        <= 35 => ItemUseTimeTier.Slow,
        <= 45 => ItemUseTimeTier.VerySlow,
        <= 55 => ItemUseTimeTier.ExtremelySlow,
        _ => ItemUseTimeTier.InsanelySlow,
    };

    // Los 8 tramos reales de retroceso.
    private static ItemKnockbackTier TierForKnockback(double knockBack) => knockBack switch
    {
        <= 1.5 => ItemKnockbackTier.ExtremelyWeak,
        <= 3 => ItemKnockbackTier.VeryWeak,
        <= 4 => ItemKnockbackTier.Weak,
        <= 6 => ItemKnockbackTier.Normal,
        <= 7 => ItemKnockbackTier.Strong,
        <= 9 => ItemKnockbackTier.VeryStrong,
        <= 11 => ItemKnockbackTier.ExtremelyStrong,
        _ => ItemKnockbackTier.Insane,
    };

    // InvariantCulture a proposito, en los dos idiomas: punto decimal siempre (es lo que ya hacia
    // el codigo anterior y lo que esperan las pruebas reales - "Retroceso 5.5", nunca "5,5").
    private static string FormatNumber(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
