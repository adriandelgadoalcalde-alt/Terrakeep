namespace Terrakeep.Core.Data;

// Datos estructurados de las estadisticas reales de un objeto (daño/defensa/velocidad/bono de
// set/efecto del prefijo), SIN una sola palabra de idioma dentro - ronda de idioma del
// 6-sep-2026, cierra el hallazgo que la oleada de QA de Personaje->Objetos dejo anotado sin
// tocar: "ItemStatsFormatter compone TODO el texto en español a fuego, asi que con la app en
// ingles los tooltips de estadisticas siguen en español".
//
// El reparto de responsabilidades queda asi, y es el motivo entero de que este fichero exista:
//   - `Terrakeep.Core` (este proyecto) CALCULA: numeros reales, tramo al que pertenecen
//     (los 8 de velocidad de uso y los 8 de retroceso), tipo de daño, DPS, que piezas forman el
//     set... Devuelve enums y numeros, nunca frases.
//   - `Terrakeep.App` REDACTA: `ItemStatsTextBuilder` traduce cada enum/numero a la frase
//     final con `LocalizationService` (`Loc[...]`/`Loc.Format(...)`), igual que el resto de la
//     app.
// Esto es obligatorio, no una preferencia de estilo: Core se compila TAMBIEN para net8.0 porque
// lo reutiliza el mod de tModLoader (ver Terrakeep.Core.csproj), asi que no puede conocer
// `LocalizationService` (que vive en la App, WPF) ni ninguna otra dependencia de la interfaz.
//
// Los unicos `string` que viajan aqui dentro son (a) numeros ya formateados en InvariantCulture
// ("+15%", "5.5", "3.33" - simbolos, no palabras) y (b) texto que YA viene traducido de los
// propios catalogos de datos (el tooltip descriptivo de vanilla y el bono de set de Calamity,
// que se extrajeron ya en español de los assets reales del juego/mod - no son literales
// escritos a mano en el codigo, y traducirlos es un trabajo de datos aparte).

// Tipo real de daño del objeto - vanilla lo decide su categoria real ya extraida
// (VanillaCategoryCatalog), Calamity su DamageType real ("DamageClass.MeleeNoSpeed"...).
// `Unknown` = el objeto hace daño pero no se sabe de que clase: la frase generica.
public enum ItemDamageKind
{
    Unknown,
    Melee,
    Ranged,
    Magic,
    Summon,
    Rogue,
}

// Los 8 tramos reales de velocidad de uso (texto real de Terrasavr.es-ES.json, namespace
// meta.item.useTime.*) - aqui solo el tramo, la palabra la pone la App.
public enum ItemUseTimeTier
{
    InsanelyFast,
    VeryFast,
    Fast,
    Normal,
    Slow,
    VerySlow,
    ExtremelySlow,
    InsanelySlow,
}

// Los 8 tramos reales de retroceso (namespace meta.item.knockback.*).
public enum ItemKnockbackTier
{
    ExtremelyWeak,
    VeryWeak,
    Weak,
    Normal,
    Strong,
    VeryStrong,
    ExtremelyStrong,
    Insane,
}

// Que estadistica toca un efecto de prefijo. Los numeros salen de
// Item.TryGetPrefixStatMultipliersForItem (armas) y Player.GrantPrefixBenefits (accesorios) del
// Terraria decompilado - ver PrefixEffectCatalog.
public enum ItemPrefixStat
{
    Damage,
    CritChance,
    Knockback,
    UseTime,
    Size,
    ShootSpeed,
    ManaCost,
    Defense,
    MaxMana,
    MoveSpeed,
    MeleeSpeed,
}

// Un efecto real de un prefijo. `Amount` es el numero YA formateado con signo y, si procede, con
// el simbolo de porcentaje ("+15%", "-10%", "+1") - InvariantCulture, sin ninguna palabra: la
// App lo mete en la plantilla de idioma que corresponda a `Stat`.
public sealed record ItemPrefixStatEffect(ItemPrefixStat Stat, string Amount);

// El bono de set completo de Calamity visto desde una pieza que NO es el casco (C-10c): el
// modelo de datos real de Calamity solo pone SetBonus en los cascos, asi que desde el peto o las
// perneras hay que enumerar el bono de CADA variante de casco real del mismo set.
public sealed record CalamitySetBonusInfo(
    string BodyName,
    string? LegsName,
    IReadOnlyList<CalamitySetBonusHead> Heads);

public sealed record CalamitySetBonusHead(string HeadName, string BonusText);

// El resultado completo. Todo opcional: un objeto puede traer solo defensa, solo un tooltip
// descriptivo, solo el bono de set... `ItemStatsFormatter.Describe` devuelve null cuando no hay
// absolutamente nada real que contar (mismo criterio de siempre: lo que no se sabe no se
// inventa, y WPF no muestra un ToolTip cuyo valor enlazado es null).
public sealed record ItemStatsInfo
{
    // Seccion 1 - efecto real del prefijo puesto en el objeto. Lista vacia si no tiene prefijo,
    // si es un prefijo de Calamity (esos numeros no estan investigados) o si el prefijo no tiene
    // ningun efecto conocido.
    public IReadOnlyList<ItemPrefixStatEffect> PrefixEffects { get; init; } = [];

    // Seccion 2 - estadisticas numericas reales.
    public int? Damage { get; init; }
    public ItemDamageKind DamageKind { get; init; } = ItemDamageKind.Unknown;
    public int? Dps { get; init; }
    public int? Defense { get; init; }
    public int? CritChance { get; init; }
    public int? UseTime { get; init; }
    // Ritmo por segundo ya formateado en InvariantCulture ("5", "3.33") - la formula real es
    // floor(6000/useTime)/100, ver ItemStatsFormatter.
    public string? UseTimePerSecond { get; init; }
    public ItemUseTimeTier? UseTimeTier { get; init; }
    // Retroceso ya formateado en InvariantCulture ("5.5") - punto decimal siempre, en los dos
    // idiomas, igual que hacia el codigo anterior.
    public string? Knockback { get; init; }
    public ItemKnockbackTier? KnockbackTier { get; init; }
    public int? ManaCost { get; init; }
    public int? HealLife { get; init; }
    public int? HealMana { get; init; }
    public int? Rarity { get; init; }

    // Seccion 3 - tooltip descriptivo real del juego (ya traducido en el propio asset, ver la
    // nota de arriba). Solo vanilla.
    public string? DescriptiveTooltip { get; init; }

    // Seccion 4 - bono de set completo. `SetBonusText` es el caso simple (vanilla, o un casco de
    // Calamity con su propio SetBonus); `CalamitySetBonus` el compuesto de arriba. Nunca los dos
    // a la vez.
    public string? SetBonusText { get; init; }
    public CalamitySetBonusInfo? CalamitySetBonus { get; init; }

    public bool IsEmpty =>
        PrefixEffects.Count == 0
        && Damage is null && Defense is null && CritChance is null && UseTime is null
        && Knockback is null && ManaCost is null && HealLife is null && HealMana is null
        && Rarity is null && DescriptiveTooltip is null && SetBonusText is null
        && CalamitySetBonus is null;
}
