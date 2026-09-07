using System.Text;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.Data;
using Terrakeep.Core.Model;
using Terrakeep.Core.Nbt;
using Xunit;

namespace Terrakeep.Core.Tests.Calamity;

public class CalamityPrefixTranslatorTests
{
    private const string RoguePrefixesJson = """
    {
      "note": "test fixture",
      "idBase": 10000,
      "weapon": [
        { "id": 10000, "internal": "Vicious", "es": "Vicioso", "en": "Vicious", "damageMult": 1.1, "useTimeMult": 0.95, "shootSpeedMult": 1.15 }
      ],
      "accessory": [
        { "id": 10017, "internal": "Dauntless", "es": "Intrepido", "en": "Dauntless", "effect": "+20 vida maxima" }
      ],
      "best": { "weapon": "Vicious", "accessory": "Dauntless" }
    }
    """;

    private static CalamityPrefixTranslator MakeTranslator() =>
        new(RoguePrefixCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(RoguePrefixesJson))));

    [Fact]
    public void ReadFromEntry_PlainVanillaPrefix()
    {
        var translator = MakeTranslator();
        var entry = NbtCompound.Of(("prefix", new NbtByte(12)));

        var prefix = translator.ReadFromEntry(entry);

        Assert.False(prefix.IsCalamity);
        Assert.Equal((byte)12, prefix.VanillaId);
    }

    [Fact]
    public void ReadFromEntry_NoPrefixField_ReturnsNone()
    {
        var translator = MakeTranslator();
        var prefix = translator.ReadFromEntry(new NbtCompound());
        Assert.True(prefix.IsNone);
    }

    [Fact]
    public void ReadFromEntry_RealCalamityPrefix_ResolvesSyntheticId()
    {
        var translator = MakeTranslator();
        var entry = NbtCompound.Of(
            ("modPrefixMod", new NbtString("CalamityMod")),
            ("modPrefixName", new NbtString("Vicious"))
        );

        var prefix = translator.ReadFromEntry(entry);

        Assert.True(prefix.IsCalamity);
        Assert.Equal(10000, prefix.SyntheticId);
    }

    [Fact]
    public void ReadFromEntry_UnknownModdedPrefix_ReturnsNone_DoesNotInventMapping()
    {
        var translator = MakeTranslator();
        var entry = NbtCompound.Of(
            ("modPrefixMod", new NbtString("SomeOtherMod")),
            ("modPrefixName", new NbtString("Whatever"))
        );

        var prefix = translator.ReadFromEntry(entry);

        Assert.True(prefix.IsNone);
    }

    [Fact]
    public void WriteToFields_VanillaPrefix_WritesSingleByteField()
    {
        var translator = MakeTranslator();
        var fields = new List<(string, NbtTag)>();

        translator.WriteToFields(fields, ItemPrefix.Vanilla(12));

        Assert.Single(fields);
        Assert.Equal("prefix", fields[0].Item1);
        Assert.Equal((sbyte)12, ((NbtByte)fields[0].Item2).Value);
    }

    [Fact]
    public void WriteToFields_CalamityPrefix_WritesModPrefixModAndName()
    {
        var translator = MakeTranslator();
        var fields = new List<(string, NbtTag)>();

        translator.WriteToFields(fields, ItemPrefix.CalamitySynthetic(10017));

        Assert.Equal(2, fields.Count);
        Assert.Equal("modPrefixMod", fields[0].Item1);
        Assert.Equal("CalamityMod", ((NbtString)fields[0].Item2).Value);
        Assert.Equal("modPrefixName", fields[1].Item1);
        Assert.Equal("Dauntless", ((NbtString)fields[1].Item2).Value);
    }

    [Fact]
    public void WriteToFields_None_WritesNothing()
    {
        var translator = MakeTranslator();
        var fields = new List<(string, NbtTag)>();

        translator.WriteToFields(fields, ItemPrefix.None);

        Assert.Empty(fields);
    }

    [Fact]
    public void RoundTrip_CalamityPrefix_ReadThenWriteThenReadAgain()
    {
        var translator = MakeTranslator();
        var original = ItemPrefix.CalamitySynthetic(10000);

        var fields = new List<(string, NbtTag)>();
        translator.WriteToFields(fields, original);
        var entry = new NbtCompound(fields);
        var readBack = translator.ReadFromEntry(entry);

        Assert.Equal(original.SyntheticId, readBack.SyntheticId);
    }
}
