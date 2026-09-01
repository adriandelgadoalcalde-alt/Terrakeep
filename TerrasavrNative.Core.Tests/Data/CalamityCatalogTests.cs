using System.Text;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using Xunit;

namespace TerrasavrNative.Core.Tests.Data;

public class CalamityCatalogTests
{
    private const string SampleJson = """
    [
      {
        "internal": "Abaddon",
        "mod": "CalamityMod",
        "category": "Accessories",
        "displayName_es": "Abaddon",
        "displayName_fallback": "Abaddon",
        "icon": "Abaddon.png",
        "stats": { "damage": null, "useTime": null, "crit": null, "knockBack": null, "mana": null, "damageType": null }
      },
      {
        "internal": "Calamity",
        "mod": "CalamityMod",
        "category": "Accessories",
        "displayName_es": "Calamity",
        "displayName_fallback": "Calamity",
        "icon": "Calamity.png",
        "stats": { "damage": 10, "useTime": 20, "crit": 4, "knockBack": 2.5, "mana": null, "damageType": "melee" }
      }
    ]
    """;

    private static CalamityCatalog Load() =>
        CalamityCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(SampleJson)));

    [Fact]
    public void AssignsSequentialSyntheticIds_StartingAtItemIdBase()
    {
        var catalog = Load();
        Assert.Equal(2, catalog.Entries.Count);
        Assert.Equal(CalamityIds.ItemIdBase, catalog.Entries[0].SyntheticId);
        Assert.Equal(CalamityIds.ItemIdBase + 1, catalog.Entries[1].SyntheticId);
    }

    [Fact]
    public void ByModAndInternal_ResolvesEntry()
    {
        var catalog = Load();
        var entry = catalog.ByModAndInternal("CalamityMod", "Calamity");
        Assert.NotNull(entry);
        Assert.Equal(CalamityIds.ItemIdBase + 1, entry!.SyntheticId);
        Assert.Equal(10, entry.Stats!.Damage);
    }

    [Fact]
    public void BySyntheticId_RoundTrips()
    {
        var catalog = Load();
        var entry = catalog.BySyntheticId(CalamityIds.ItemIdBase);
        Assert.NotNull(entry);
        Assert.Equal("Abaddon", entry!.Internal);
    }

    [Fact]
    public void UnknownLookup_ReturnsNull()
    {
        var catalog = Load();
        Assert.Null(catalog.ByModAndInternal("CalamityMod", "NoExiste"));
        Assert.Null(catalog.BySyntheticId(999));
    }
}
