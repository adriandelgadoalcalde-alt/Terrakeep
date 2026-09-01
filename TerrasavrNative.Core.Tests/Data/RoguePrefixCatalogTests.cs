using System.Text;
using TerrasavrNative.Core.Data;
using Xunit;

namespace TerrasavrNative.Core.Tests.Data;

public class RoguePrefixCatalogTests
{
    private const string SampleJson = """
    {
      "note": "test fixture",
      "idBase": 10000,
      "weapon": [
        { "id": 10000, "internal": "Vicious", "es": "Vicioso", "en": "Vicious", "damageMult": 1.1, "useTimeMult": 0.95, "shootSpeedMult": 1.15 },
        { "id": 10016, "internal": "Flawless", "es": "Impecable", "en": "Flawless", "damageMult": 1.15, "useTimeMult": 0.9, "shootSpeedMult": 1.1 }
      ],
      "accessory": [
        { "id": 10017, "internal": "Dauntless", "es": "Intrepido", "en": "Dauntless", "effect": "+20 vida maxima" },
        { "id": 10020, "internal": "Silent", "es": "Silencioso", "en": "Silent", "effect": "efecto" }
      ],
      "best": { "weapon": "Flawless", "accessory": "Silent" }
    }
    """;

    private static RoguePrefixCatalog Load() =>
        RoguePrefixCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(SampleJson)));

    [Fact]
    public void ById_ResolvesWeaponAndAccessory()
    {
        var catalog = Load();
        Assert.Equal("Vicious", catalog.ById(10000)!.Internal);
        Assert.Equal("Silent", catalog.ById(10020)!.Internal);
    }

    [Fact]
    public void ByInternal_ResolvesEntry()
    {
        var catalog = Load();
        Assert.Equal(10016, catalog.ByInternal("Flawless")!.Id);
    }

    [Fact]
    public void Best_ExposesWeaponAndAccessoryDefaults()
    {
        var catalog = Load();
        Assert.Equal("Flawless", catalog.Best.Weapon);
        Assert.Equal("Silent", catalog.Best.Accessory);
    }

    [Fact]
    public void UnknownInternal_ReturnsNull()
    {
        var catalog = Load();
        Assert.Null(catalog.ByInternal("NoExiste"));
    }
}
