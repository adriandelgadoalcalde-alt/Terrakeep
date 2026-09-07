using System.Text;
using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

public class PrefixGroupCatalogTests
{
    // El pool "swords" incluye aqui, ademas de un par de ids propios de Melee (1,2,81), tres
    // ids reales que en el juego SI son compartidos con el grupo "Universal+" (36,37,59) -
    // igual que en PrefixesForSwords real (ver scripts/extraer-prefijos-vanilla.py) - para
    // poder probar de verdad que un objeto puede aparecer en mas de un grupo a la vez.
    private const string Fixture = """
    {
        "prefixesByCategory": {
            "swords": [1, 2, 36, 37, 59, 81],
            "gunsBows": [16, 82]
        },
        "itemCategories": {
            "1": ["melee", "anyWeapon"]
        },
        "itemPool": {
            "1": "swords"
        }
    }
    """;

    private static PrefixRulesCatalog LoadRules()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Fixture));
        return PrefixRulesCatalog.LoadFromStream(stream);
    }

    [Fact]
    public void MeleeWeapon_ShowsOnlyGroupsThatIntersectItsLegalPool()
    {
        var rules = LoadRules();
        var itemCats = rules.VanillaCategories(1); // Melee|AnyWeapon

        var positiveMeta = PrefixGroupCatalog.Metas.Single(m => m.NameEn == "Positive");
        var groups = PrefixGroupCatalog.GroupsFor(positiveMeta, itemCats, isCalamityItem: false, itemId: 1, rules).ToList();

        Assert.Contains(groups, g => g.NameEn == "Melee+");
        Assert.Contains(groups, g => g.NameEn == "Universal+"); // Requires=AnyWeapon, tambien aplica

        // No admite ningun prefijo de tipo Ranged/Magic/Accessory/Summon - ni la categoria
        // aplica (Requires no coincide) ni tendria prefijos legales aunque aplicara.
        Assert.DoesNotContain(groups, g => g.NameEn is "Ranged+" or "Magic+" or "Accessory" or "Summon+");

        // Dentro de Melee+, solo los ids realmente legales para ESTE objeto (interseccion
        // con su pool real, no el grupo completo del Terrasavr original que no filtra).
        var meleeGroup = groups.Single(g => g.NameEn == "Melee+");
        var ids = PrefixGroupCatalog.PrefixIdsFor(meleeGroup, isCalamityItem: false, itemId: 1, rules).ToList();
        Assert.Equal([1, 2, 81], ids);
    }

    [Fact]
    public void ItemWithNoLegalPrefixes_ShowsNoGroupsInAnyMeta()
    {
        var rules = LoadRules();
        var itemCats = rules.VanillaCategories(999); // no existe en la fixture -> None

        foreach (var meta in PrefixGroupCatalog.Metas)
        {
            var groups = PrefixGroupCatalog.GroupsFor(meta, itemCats, isCalamityItem: false, itemId: 999, rules);
            Assert.Empty(groups); // incluye "Biblioteca" (Requires=None) - sin pool legal, sin grupos
        }
    }

    [Fact]
    public void CalamityItem_UsesGroupIdsWithoutVanillaPoolFiltering()
    {
        var rules = LoadRules();
        // Un arma de invocacion de Calamity: PrefixEligibility.For ya habria calculado
        // Summon|AnyWeapon - aqui se simula pasando esa categoria directamente.
        var positiveMeta = PrefixGroupCatalog.Metas.Single(m => m.NameEn == "Positive");
        var groups = PrefixGroupCatalog.GroupsFor(positiveMeta, PrefixCategory.Summon | PrefixCategory.AnyWeapon, isCalamityItem: true, itemId: 20000000, rules).ToList();

        var summonGroup = Assert.Single(groups, g => g.NameEn == "Summon+");
        var ids = PrefixGroupCatalog.PrefixIdsFor(summonGroup, isCalamityItem: true, itemId: 20000000, rules).ToList();
        // Sin filtrar por PrefixRulesCatalog (no tiene datos de objetos de Calamity) - se
        // devuelve el grupo completo tal cual.
        Assert.Equal(summonGroup.PrefixIds, ids);
    }
}
