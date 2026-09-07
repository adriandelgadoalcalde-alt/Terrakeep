using System.Text;
using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

public class VanillaBuffCatalogTests
{
    private const string SampleJson = """{"1":"Obsidian Skin","353":"Shimmer"}""";
    private const string SampleDescriptionsJson = """{"ObsidianSkin":"Inmune a fuego de lava"}""";
    private const string SampleNamesEsJson = """{"ObsidianSkin":"Piel de obsidiana"}""";

    private static VanillaBuffCatalog Load() =>
        VanillaBuffCatalog.LoadFromStream(
            new MemoryStream(Encoding.UTF8.GetBytes(SampleJson)),
            new MemoryStream(Encoding.UTF8.GetBytes(SampleDescriptionsJson)),
            new MemoryStream(Encoding.UTF8.GetBytes(SampleNamesEsJson)));

    [Fact]
    public void GetName_ResolvesKnownAndUnknown()
    {
        var catalog = Load();
        Assert.Equal("Obsidian Skin", catalog.GetName(1));
        Assert.Equal("Buff #999", catalog.GetName(999));
    }

    [Fact]
    public void GetDisplayName_PrefersSpanish_FallsBackToHumanizedName()
    {
        var catalog = Load();
        Assert.Equal("Piel de obsidiana", catalog.GetDisplayName(1)); // traduccion real
        Assert.Equal("Shimmer", catalog.GetDisplayName(353)); // sin traduccion real -> humanizado
    }

    [Fact]
    public void AllEntries_ReturnsEveryEntry()
    {
        var catalog = Load();
        var entries = catalog.AllEntries().ToList();
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Id == 1 && e.Name == "Obsidian Skin");
        Assert.Contains(entries, e => e.Id == 353 && e.Name == "Shimmer");
    }
}
