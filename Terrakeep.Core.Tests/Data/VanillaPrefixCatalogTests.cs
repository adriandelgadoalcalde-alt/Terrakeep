using System.Text;
using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

public class VanillaPrefixCatalogTests
{
    private const string SampleJson = """
    [
      {"id":1,"internal":"Large","es":"Grande","en":"Large"},
      {"id":59,"internal":"Godly","es":"Divino","en":"Godly"}
    ]
    """;

    private static VanillaPrefixCatalog Load() =>
        VanillaPrefixCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(SampleJson)));

    [Fact]
    public void ById_And_ByInternal_ResolveSameEntry()
    {
        var catalog = Load();
        Assert.Equal("Divino", catalog.ById(59)!.Es);
        Assert.Equal("Divino", catalog.ByInternal("Godly")!.Es);
        Assert.Null(catalog.ById(999));
    }

    [Fact]
    public void AllEntries_ReturnsEveryEntry()
    {
        var catalog = Load();
        var entries = catalog.AllEntries().ToList();
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Id == 1 && e.Internal == "Large");
        Assert.Contains(entries, e => e.Id == 59 && e.Internal == "Godly");
    }
}
