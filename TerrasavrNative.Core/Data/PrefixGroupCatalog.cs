using TerrasavrNative.Core.Calamity;

namespace TerrasavrNative.Core.Data;

// Un grupo de prefijos dentro de una meta (ej. "Cuerpo a cuerpo +" dentro de "Positivos") -
// Requires es la categoria real que debe tener el objeto para que el grupo tenga sentido
// (PrefixCategory.None = siempre aplica, usado por los grupos de "mejor ajuste").
public sealed record PrefixGroup(string NameEs, string NameEn, PrefixCategory Requires, int[] PrefixIds);

// Una "meta" (pestaña) del selector de prefijo real: Biblioteca (mejor ajuste)/Positivos/
// Negativos - igual que los 3 botones "Library"/"Positive"/"Negative" reales de Terrasavr.
public sealed record PrefixMeta(string NameEs, string NameEn, PrefixGroup[] Groups);

// Tabla LITERAL de "pfxMeta" del Terrasavr real (script.js, clase app.TabEdit, beautificado
// en esta sesion - lineas 2600-2657), traducida a C#. Los ids de prefijo de invocacion de
// Calamity (85-97: Fabled/Loyal/Worthy/Focused/Patient/Rabid/IllTempered/Petty/Feeble/
// Skittish/Eager/Ballistic/Scraggling) vienen de las constantes reales M.PXxx del propio
// script.js (lineas ~10453-10466: M.PLegendary2=84, M.PFabled=85, ... M.PScraggling=97) -
// Terraria vanilla real solo define hasta PrefixID.Count==85, asi que 85-97 NO son vanilla
// aunque se escriban como el mismo byte de prefijo (ver PrefixEligibility.cs).
//
// El Terrasavr JS real NO filtra estos grupos por tipo de objeto (siempre muestra los 8) -
// el "Requires" de cada grupo aqui es una mejora real sobre el original, basada en datos
// reales del propio juego (PrefixRulesCatalog, extraido de Item.cs/PrefixLegacy.cs) en vez
// de dejar que el usuario intente ponerle "Legendario" a un arco.
public static class PrefixGroupCatalog
{
    public static IReadOnlyList<PrefixMeta> Metas { get; } =
    [
        new("Biblioteca", "Library",
        [
            new("Mejor", "Best", PrefixCategory.None, [60, 59, 82, 83, 81, 84, 85]),
            new("Daño", "Damage", PrefixCategory.None, [20, 57, 59, 5, 81, 25, 82, 35, 83, 84, 85, 87]),
            new("Crítico", "Critical", PrefixCategory.None, [61, 60, 59, 44, 46, 3, 81, 16, 82, 83, 84]),
        ]),
        new("Positivos", "Positive",
        [
            // H3-05 (tercera auditoria de Opus, Fable): +10017..10020 - los 4 ModPrefix reales
            // de accesorio de Calamity (Dauntless/Friendly/Invigorating/Silent, decompilados:
            // Category=PrefixCategory.Accessory, CanRoll universal - ninguno restringido a
            // objetos de Calamity) se unen de verdad al MISMO pool vanilla de "Accesorio" por
            // categoria (mismo mecanismo real de ModPrefix de tModLoader) - aplican a
            // CUALQUIER accesorio, vanilla o de Calamity. "Friendly" en concreto tiene
            // RollChance=0 en el propio juego (nunca sale al azar) - este picker manual es la
            // UNICA forma real de ponerlo, antes ni eso.
            new("Accesorio", "Accessory", PrefixCategory.Accessory, [62, 63, 67, 69, 70, 73, 74, 77, 78, 66, 10017, 10018, 10019, 10020]),
            new("Accesorio +", "Accessory+", PrefixCategory.Accessory, [64, 65, 68, 71, 72, 75, 76, 79, 80]),
            new("Universal +", "Universal+", PrefixCategory.AnyWeapon, [36, 37, 38, 53, 54, 55, 57, 59, 61]),
            new("Común +", "Common+", PrefixCategory.Melee | PrefixCategory.Ranged | PrefixCategory.Magic, [42, 43, 44, 45, 46, 51]),
            new("Cuerpo a cuerpo +", "Melee+", PrefixCategory.Melee, [1, 2, 3, 4, 5, 6, 12, 14, 15, 81]),
            new("A distancia +", "Ranged+", PrefixCategory.Ranged, [16, 17, 18, 19, 20, 21, 25, 58, 82]),
            new("Magia +", "Magic+", PrefixCategory.Magic, [26, 27, 28, 32, 33, 34, 35, 82, 83]),
            new("Invocación +", "Summon+", PrefixCategory.Summon, [85, 86, 87, 88, 95, 96, 97, 89, 90, 91]),
            // H3-05: los 17 ModPrefix reales de arma Picaro (RoguePrefixCatalog.Weapon,
            // RogueWeaponPrefix/HorribleWeaponPrefix decompilados) - antes SIN NINGUN grupo
            // real que los mostrara (solo AnyWeapon generico), pese a ser el equivalente
            // directo y propio de Picaro a "Cuerpo a cuerpo +"/"A distancia +"/"Magia +". No se
            // separan en +/- (a diferencia de esos 3) porque Calamity no los diseña como pares
            // simetricos por estadistica - son 17 variantes propias, mezcla real de pros/
            // contras cada una (ver RoguePrefixCatalog.DescribeWeaponEffect).
            new("Pícaro", "Rogue", PrefixCategory.Rogue, [10000, 10001, 10002, 10003, 10004, 10005, 10006, 10007, 10008, 10009, 10010, 10011, 10012, 10013, 10014, 10015, 10016]),
        ]),
        new("Negativos", "Negative",
        [
            new("Universal -", "Universal-", PrefixCategory.AnyWeapon, [39, 40, 41, 56]),
            new("Común -", "Common-", PrefixCategory.Melee | PrefixCategory.Ranged | PrefixCategory.Magic, [47, 48, 49, 50]),
            new("Cuerpo a cuerpo -", "Melee-", PrefixCategory.Melee, [7, 8, 9, 10, 11, 13]),
            new("A distancia -", "Ranged-", PrefixCategory.Ranged, [22, 23, 24]),
            new("Magia -", "Magic-", PrefixCategory.Magic, [29, 30, 31]),
            new("Invocación -", "Summon-", PrefixCategory.Summon, [92, 93, 94]),
        ]),
    ];

    // Grupos de una meta que aplican de verdad al objeto (Requires=None siempre aplica) y
    // que tienen al menos un prefijo legal tras filtrar. Solo se filtra por legalidad real
    // para objetos vanilla (PrefixRulesCatalog es especifico de ids vanilla) - para
    // Calamity se confia en el Requires ya calculado por PrefixEligibility, sin tabla de
    // legalidad por-objeto propia disponible en este proyecto.
    public static IEnumerable<PrefixGroup> GroupsFor(PrefixMeta meta, PrefixCategory itemCategories, bool isCalamityItem, int itemId, PrefixRulesCatalog rules)
    {
        foreach (var g in meta.Groups)
        {
            if (g.Requires != PrefixCategory.None && (itemCategories & g.Requires) == 0) continue;
            if (PrefixIdsFor(g, isCalamityItem, itemId, rules).Any()) yield return g;
        }
    }

    public static IEnumerable<int> PrefixIdsFor(PrefixGroup group, bool isCalamityItem, int itemId, PrefixRulesCatalog rules)
    {
        if (isCalamityItem) return group.PrefixIds;
        var legal = rules.LegalPrefixes(itemId);
        // H3-05: los ids sinteticos de Calamity (>= PrefixIdBase, ej. los 4 de accesorio real
        // dentro de "Accesorio") nunca estan en PrefixRulesCatalog (tabla solo vanilla,
        // Item.GetRollablePrefixes real) - no filtrarlos por legalidad vanilla, son un pool
        // real APARTE que tModLoader une al vanilla por categoria (ver el comentario real en
        // el grupo "Accesorio" de arriba), no un objeto vanilla desconocido.
        return group.PrefixIds.Where(id => id >= CalamityIds.PrefixIdBase || legal.Contains(id));
    }
}
