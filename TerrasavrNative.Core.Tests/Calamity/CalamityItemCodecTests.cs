using System.Text;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.Nbt;
using Xunit;

namespace TerrasavrNative.Core.Tests.Calamity;

public class CalamityItemCodecTests
{
    private const string CatalogJson = """
    [
      { "internal": "Abaddon", "mod": "CalamityMod", "category": "Accessories", "displayName_es": "Abaddon", "displayName_fallback": "Abaddon", "icon": "Abaddon.png", "stats": {} },
      { "internal": "CalamityTitleMusicBox", "mod": "CalamityModMusic", "category": "Music", "displayName_es": "Caja", "displayName_fallback": "Box", "icon": "x.png", "stats": {} }
    ]
    """;

    private const string RoguePrefixesJson = """
    {
      "note": "test fixture", "idBase": 10000,
      "weapon": [{ "id": 10000, "internal": "Vicious", "es": "Vicioso", "en": "Vicious", "damageMult": 1.1, "useTimeMult": 0.95, "shootSpeedMult": 1.15 }],
      "accessory": [],
      "best": { "weapon": "Vicious", "accessory": "Vicious" }
    }
    """;

    private static CalamityItemCodec MakeCodec(out CalamityCatalog catalog)
    {
        catalog = CalamityCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(CatalogJson)));
        var prefixes = RoguePrefixCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(RoguePrefixesJson)));
        var translator = new CalamityPrefixTranslator(prefixes);
        return new CalamityItemCodec(catalog, translator);
    }

    [Fact]
    public void Decode_ResolvesKnownItem_WithStackAndPrefix()
    {
        var codec = MakeCodec(out var catalog);
        var entry = NbtCompound.Of(
            ("mod", new NbtString("CalamityMod")),
            ("name", new NbtString("Abaddon")),
            ("slot", new NbtShort(5)),
            ("modPrefixMod", new NbtString("CalamityMod")),
            ("modPrefixName", new NbtString("Vicious")),
            ("stack", new NbtInt(3))
        );

        var result = codec.Decode(entry);

        Assert.NotNull(result);
        Assert.Equal((short)5, result!.Value.Slot);
        Assert.Equal(catalog.ByModAndInternal("CalamityMod", "Abaddon")!.SyntheticId, result.Value.Item.Id);
        Assert.Equal(3, result.Value.Item.Count);
        Assert.True(result.Value.Item.Prefix.IsCalamity);
    }

    [Fact]
    public void Decode_StackDefaultsToOne_WhenFieldAbsent()
    {
        var codec = MakeCodec(out _);
        var entry = NbtCompound.Of(
            ("mod", new NbtString("CalamityMod")),
            ("name", new NbtString("Abaddon")),
            ("slot", new NbtShort(0))
        );

        var result = codec.Decode(entry);

        Assert.Equal(1, result!.Value.Item.Count);
    }

    [Fact]
    public void Decode_UnknownItem_ReturnsNull()
    {
        var codec = MakeCodec(out _);
        var entry = NbtCompound.Of(
            ("mod", new NbtString("CalamityMod")),
            ("name", new NbtString("NoExiste")),
            ("slot", new NbtShort(0))
        );

        Assert.Null(codec.Decode(entry));
    }

    [Fact]
    public void Encode_OmitsStackField_WhenCountIsOne()
    {
        var codec = MakeCodec(out var catalog);
        var item = new GameItem { Id = catalog.ByModAndInternal("CalamityMod", "Abaddon")!.SyntheticId, Count = 1 };

        var encoded = codec.Encode(3, item);

        Assert.Null(encoded.Get("stack"));
        Assert.Equal((short)3, ((NbtShort)encoded.Get("slot")!).Value);
        Assert.NotNull(encoded.Get("globalData"));
    }

    [Fact]
    public void Encode_IncludesStackField_WhenCountGreaterThanOne()
    {
        var codec = MakeCodec(out var catalog);
        var item = new GameItem { Id = catalog.ByModAndInternal("CalamityMod", "Abaddon")!.SyntheticId, Count = 99 };

        var encoded = codec.Encode(0, item);

        Assert.Equal(99, ((NbtInt)encoded.Get("stack")!).Value);
    }

    [Fact]
    public void RoundTrip_DecodeThenEncode_PreservesEssentialFields()
    {
        var codec = MakeCodec(out _);
        var originalEntry = NbtCompound.Of(
            ("mod", new NbtString("CalamityModMusic")),
            ("name", new NbtString("CalamityTitleMusicBox")),
            ("slot", new NbtShort(7)),
            ("prefix", new NbtByte(0)),
            ("stack", new NbtInt(2))
        );

        var decoded = codec.Decode(originalEntry)!.Value;
        var reEncoded = codec.Encode(decoded.Slot, decoded.Item);
        var reDecoded = codec.Decode(reEncoded)!.Value;

        Assert.Equal(decoded.Slot, reDecoded.Slot);
        Assert.Equal(decoded.Item.Id, reDecoded.Item.Id);
        Assert.Equal(decoded.Item.Count, reDecoded.Item.Count);
    }

    [Fact]
    public void Encode_NonCalamityItem_Throws()
    {
        var codec = MakeCodec(out _);
        var vanillaItem = new GameItem { Id = 42 };
        Assert.Throws<ArgumentException>(() => codec.Encode(0, vanillaItem));
    }
}
