using System.Text;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using Xunit;

namespace TerrasavrNative.Core.Tests.Calamity;

public class BuildItemResolverTests
{
    private static VanillaItemCatalog LoadVanilla()
    {
        const string names = """{"1":"Pico de hierro","231":"Casco fundido"}""";
        const string byKey = """{"IronPickaxe":"Pico de hierro","MoltenHelmet":"Casco fundido"}""";
        const string idsByKey = """{"IronPickaxe":1,"MoltenHelmet":231}""";
        return VanillaItemCatalog.LoadFromStreams(
            new MemoryStream(Encoding.UTF8.GetBytes(names)),
            new MemoryStream(Encoding.UTF8.GetBytes(byKey)),
            new MemoryStream(Encoding.UTF8.GetBytes(idsByKey)));
    }

    private static VanillaPrefixCatalog LoadPrefixes()
    {
        const string json = """[{"id":51,"internal":"Legendary","es":"Legendario","en":"Legendary"}]""";
        return VanillaPrefixCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
    }

    private static CalamityCatalog LoadCalamity()
    {
        const string json = """
        [
          { "internal": "SmokingComet", "mod": "CalamityMod", "category": "Weapons",
            "displayName_es": "Cometa Humeante", "displayName_fallback": "Smoking Comet", "icon": null,
            "stats": { "damage": null, "useTime": null, "crit": null, "knockBack": null, "mana": null, "damageType": null } }
        ]
        """;
        return CalamityCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void ResolvesVanillaItem_WithVanillaPrefixByInternalName()
    {
        var itemRef = new BuildItemRef { Pid = "MoltenHelmet", Prefix = "Legendary" };
        var resolved = BuildItemResolver.Resolve(itemRef, LoadVanilla(), LoadCalamity(), LoadPrefixes());

        Assert.NotNull(resolved);
        Assert.Equal(231, resolved!.Id);
        Assert.False(resolved.Prefix.IsCalamity);
        Assert.Equal(51, resolved.Prefix.VanillaId);
    }

    [Fact]
    public void ResolvesVanillaItem_WithoutPrefix()
    {
        var itemRef = new BuildItemRef { Pid = "IronPickaxe" };
        var resolved = BuildItemResolver.Resolve(itemRef, LoadVanilla(), LoadCalamity(), LoadPrefixes());

        Assert.NotNull(resolved);
        Assert.Equal(1, resolved!.Id);
        Assert.True(resolved.Prefix.IsNone);
    }

    [Fact]
    public void ResolvesCalamityItem_WithSyntheticPrefixId()
    {
        var itemRef = new BuildItemRef { Pid = "CalamityMod/SmokingComet", PrefixId = 10002 };
        var resolved = BuildItemResolver.Resolve(itemRef, LoadVanilla(), LoadCalamity(), LoadPrefixes());

        Assert.NotNull(resolved);
        Assert.True(resolved!.Id >= CalamityIds.ItemIdBase);
        Assert.True(resolved.Prefix.IsCalamity);
        Assert.Equal(10002, resolved.Prefix.SyntheticId);
    }

    [Fact]
    public void UnknownPid_ReturnsNull()
    {
        var vanillaRef = new BuildItemRef { Pid = "NoExisteEsteObjeto" };
        var calamityRef = new BuildItemRef { Pid = "CalamityMod/NoExiste" };

        Assert.Null(BuildItemResolver.Resolve(vanillaRef, LoadVanilla(), LoadCalamity(), LoadPrefixes()));
        Assert.Null(BuildItemResolver.Resolve(calamityRef, LoadVanilla(), LoadCalamity(), LoadPrefixes()));
    }
}
