using System.Text;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.Data;
using Terrakeep.Core.Model;
using Xunit;

namespace Terrakeep.Core.Tests.Calamity;

public class PrefixSuggesterTests
{
    private const string CatalogJson = """
    [
      { "internal": "MoltenHelmet", "mod": "CalamityMod", "category": "Armor", "displayName_es": "x", "displayName_fallback": "x", "icon": "x.png", "stats": {} },
      { "internal": "ScourgeoftheDesert", "mod": "CalamityMod", "category": "Weapons", "displayName_es": "x", "displayName_fallback": "x", "icon": "x.png", "stats": { "damageType": "RogueDamageClass.Instance" } },
      { "internal": "SinRando", "mod": "CalamityMod", "category": "Weapons", "displayName_es": "x", "displayName_fallback": "x", "icon": "x.png", "stats": { "damageType": "DamageClass.Melee" } }
    ]
    """;

    private const string BestPrefixJson = """
    {
      "prefixNames": {},
      "vanilla": { "1": 81 },
      "calamity": { "MoltenHelmet": 81, "SinRando": 47 }
    }
    """;

    private const string RoguePrefixesJson = """
    {
      "note": "test fixture", "idBase": 10000,
      "weapon": [{ "id": 10016, "internal": "Flawless", "es": "Impecable", "en": "Flawless", "damageMult": 1.15, "useTimeMult": 0.9, "shootSpeedMult": 1.1 }],
      "accessory": [],
      "best": { "weapon": "Flawless", "accessory": "Flawless" }
    }
    """;

    private static (CalamityCatalog Catalog, BestPrefixCatalog BestPrefix, RoguePrefixCatalog Rogue) LoadFixtures()
    {
        var catalog = CalamityCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(CatalogJson)));
        var bestPrefix = BestPrefixCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(BestPrefixJson)));
        var rogue = RoguePrefixCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(RoguePrefixesJson)));
        return (catalog, bestPrefix, rogue);
    }

    [Fact]
    public void Suggest_VanillaItem_UsesVanillaTable()
    {
        var (catalog, bestPrefix, rogue) = LoadFixtures();
        var item = new GameItem { Id = 1 };

        var suggestion = PrefixSuggester.Suggest(item, catalog, bestPrefix, rogue);

        Assert.NotNull(suggestion);
        Assert.False(suggestion!.Value.IsCalamity);
        Assert.Equal((byte)81, suggestion.Value.VanillaId);
    }

    [Fact]
    public void Suggest_VanillaItem_NotInTable_ReturnsNull()
    {
        var (catalog, bestPrefix, rogue) = LoadFixtures();
        var item = new GameItem { Id = 999999 };

        Assert.Null(PrefixSuggester.Suggest(item, catalog, bestPrefix, rogue));
    }

    [Fact]
    public void Suggest_CalamityNonRogueItem_UsesGenericCalamityTable()
    {
        var (catalog, bestPrefix, rogue) = LoadFixtures();
        var helmetId = catalog.ByModAndInternal("CalamityMod", "MoltenHelmet")!.SyntheticId;
        var item = new GameItem { Id = helmetId };

        var suggestion = PrefixSuggester.Suggest(item, catalog, bestPrefix, rogue);

        Assert.NotNull(suggestion);
        Assert.False(suggestion!.Value.IsCalamity);
        Assert.Equal((byte)81, suggestion.Value.VanillaId);
    }

    [Fact]
    public void Suggest_RogueWeapon_UsesRealCalamityPrefix_NotGenericTable()
    {
        var (catalog, bestPrefix, rogue) = LoadFixtures();
        // "ScourgeoftheDesert" es Rogue y NO esta en la tabla generica calamity - confirma que
        // la deteccion de clase Picaro no depende de que la tabla generica lo cubra tambien.
        var id = catalog.ByModAndInternal("CalamityMod", "ScourgeoftheDesert")!.SyntheticId;
        var item = new GameItem { Id = id };

        var suggestion = PrefixSuggester.Suggest(item, catalog, bestPrefix, rogue);

        Assert.NotNull(suggestion);
        Assert.True(suggestion!.Value.IsCalamity);
        Assert.Equal(10016, suggestion.Value.SyntheticId); // "Flawless"
    }

    [Fact]
    public void Suggest_RogueClassButAlsoInGenericTable_PrefersRealCalamityPrefix()
    {
        var (catalog, bestPrefix, rogue) = LoadFixtures();
        // "SinRando" esta en la tabla generica (47) PERO su damageType real es Melee, no
        // Rogue - debe usar la tabla generica, no el prefijo real de Picaro.
        var id = catalog.ByModAndInternal("CalamityMod", "SinRando")!.SyntheticId;
        var item = new GameItem { Id = id };

        var suggestion = PrefixSuggester.Suggest(item, catalog, bestPrefix, rogue);

        Assert.NotNull(suggestion);
        Assert.False(suggestion!.Value.IsCalamity);
        Assert.Equal((byte)47, suggestion.Value.VanillaId);
    }

    [Fact]
    public void Suggest_UnknownCalamityId_ReturnsNull()
    {
        var (catalog, bestPrefix, rogue) = LoadFixtures();
        var item = new GameItem { Id = CalamityIds.ItemIdBase + 999 };

        Assert.Null(PrefixSuggester.Suggest(item, catalog, bestPrefix, rogue));
    }
}
