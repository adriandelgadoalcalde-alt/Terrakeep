using System.Text;
using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

public class PrefixRulesCatalogTests
{
    private const string Fixture = """
    {
        "prefixesByCategory": {
            "swords": [1, 2, 81],
            "gunsBows": [16, 17, 82],
            "accessories": [62, 63]
        },
        "itemCategories": {
            "1": ["melee", "anyWeapon"],
            "39": ["ranged", "anyWeapon"],
            "62": ["accessory"]
        },
        "itemPool": {
            "1": "swords",
            "39": "gunsBows",
            "62": "accessories"
        }
    }
    """;

    private static PrefixRulesCatalog Load()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Fixture));
        return PrefixRulesCatalog.LoadFromStream(stream);
    }

    [Fact]
    public void ItemWithoutEntry_HasNoCategoriesAndNoLegalPrefixes()
    {
        var catalog = Load();

        // Un bloque de tierra (id inventado 2 en esta fixture, no listado en ningun sitio)
        // no admite ningun prefijo - este es el bug real reportado por el usuario.
        Assert.Equal(PrefixCategory.None, catalog.VanillaCategories(2));
        Assert.Empty(catalog.LegalPrefixes(2));
        Assert.False(catalog.IsLegal(2, 81));
    }

    [Fact]
    public void MeleeWeapon_OnlyAdmitsItsOwnPool()
    {
        var catalog = Load();

        Assert.Equal(PrefixCategory.Melee | PrefixCategory.AnyWeapon, catalog.VanillaCategories(1));
        Assert.Equal([1, 2, 81], catalog.LegalPrefixes(1));
        Assert.True(catalog.IsLegal(1, 81));
        Assert.False(catalog.IsLegal(1, 82)); // 82 es de GunsBows, no de Swords
    }

    [Fact]
    public void RangedWeapon_OnlyAdmitsGunsBowsPool()
    {
        var catalog = Load();

        Assert.Equal(PrefixCategory.Ranged | PrefixCategory.AnyWeapon, catalog.VanillaCategories(39));
        Assert.True(catalog.IsLegal(39, 82));
        Assert.False(catalog.IsLegal(39, 81)); // 81 es de Swords, no de GunsBows
    }

    [Fact]
    public void Accessory_OnlyAdmitsAccessoryPool()
    {
        var catalog = Load();

        Assert.Equal(PrefixCategory.Accessory, catalog.VanillaCategories(62));
        Assert.Equal([62, 63], catalog.LegalPrefixes(62));
        Assert.False(catalog.IsLegal(62, 81));
    }
}

// Prueba de humo contra el JSON real generado por scripts/extraer-prefijos-vanilla.py (no
// una fixture) - mismo criterio que el resto de *RealFileTests de este proyecto: se salta
// sola si la carpeta no existe en la maquina donde corran los tests.
public class PrefixRulesCatalogRealFileTests
{
    private const string AssetsDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets";

    [Fact]
    public void RealFile_KnownItems_HaveCorrectEligibility()
    {
        string path = Path.Combine(AssetsDir, "vanilla_prefix_rules.json");
        if (!File.Exists(path)) return;

        var catalog = PrefixRulesCatalog.LoadFromFile(path);

        // Espada corta de cobre (1) - melee real, admite 81 (Legendario) pero no 82 (el
        // equivalente de A distancia).
        Assert.True(catalog.VanillaCategories(1).HasFlag(PrefixCategory.Melee));
        Assert.True(catalog.IsLegal(1, 81));
        Assert.False(catalog.IsLegal(1, 82));

        // Excalibur (368) - misma familia real (SwordsHammersAxesPicks).
        Assert.True(catalog.VanillaCategories(368).HasFlag(PrefixCategory.Melee));

        // Madera (2) - material, sin ninguna categoria de prefijo real. Este es
        // exactamente el bug reportado por el usuario ("me esta permitiendo poner prefijo
        // a objetos que no deberia").
        Assert.Equal(PrefixCategory.None, catalog.VanillaCategories(2));
        Assert.Empty(catalog.LegalPrefixes(2));

        // Terrarian (3389) - unico objeto vanilla real que admite el prefijo 84
        // (Legendario2/"Legendary"), via ItemsThatCanHaveLegendary2.
        Assert.True(catalog.IsLegal(3389, 84));

        Assert.True(catalog.LegalPrefixes(1).Count > 0);
    }
}
