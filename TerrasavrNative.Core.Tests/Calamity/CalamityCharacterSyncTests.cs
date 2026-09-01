using System.Text;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.Nbt;
using TerrasavrNative.Core.PlrFormat;
using Xunit;

namespace TerrasavrNative.Core.Tests.Calamity;

public class CalamityCharacterSyncTests
{
    private const string CatalogJson = """
    [
      { "internal": "Abaddon", "mod": "CalamityMod", "category": "Accessories", "displayName_es": "Abaddon", "displayName_fallback": "Abaddon", "icon": "x.png", "stats": {} },
      { "internal": "Calamity", "mod": "CalamityMod", "category": "Accessories", "displayName_es": "Calamity", "displayName_fallback": "Calamity", "icon": "x.png", "stats": {} }
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

    private static CalamityCharacterSync MakeSync(out CalamityCatalog catalog)
    {
        catalog = CalamityCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(CatalogJson)));
        var prefixes = RoguePrefixCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(RoguePrefixesJson)));
        var translator = new CalamityPrefixTranslator(prefixes);
        var codec = new CalamityItemCodec(catalog, translator);
        return new CalamityCharacterSync(codec);
    }

    private static PlrCharacter MakeBlankCharacter()
    {
        var character = new PlrCharacter { Version = 315, Name = "Test", PrimaryLoadout = PlrLoadout.CreateEmpty(true) };
        Array.Fill(character.Inventory, PlrItemSlot.Empty);
        Array.Fill(character.BankItems, PlrItemSlot.Empty);
        Array.Fill(character.SafeItems, PlrItemSlot.Empty);
        Array.Fill(character.ForgeItems, PlrItemSlot.Empty);
        Array.Fill(character.VoidItems, PlrItemSlot.Empty);
        Array.Fill(character.EquipmentItems, PlrItemSlot.Empty);
        Array.Fill(character.EquipmentDyes, PlrItemSlot.Empty);
        return character;
    }

    [Fact]
    public void MergeAll_NoTplr_ReturnsVanillaOnlyView()
    {
        var sync = MakeSync(out _);
        var character = MakeBlankCharacter();
        character.Inventory[0] = new PlrItemSlot(Id: 42, Count: 5, Prefix: 3, Favorited: false);

        var merged = sync.MergeAll(character, tplrRoot: null);

        Assert.Equal(42, merged["inventory"][0].Id);
        Assert.Equal(5, merged["inventory"][0].Count);
        Assert.False(merged["inventory"][0].IsCalamity);
    }

    [Fact]
    public void MergeAll_WithTplr_MergesCalamityItem_IntoCorrectSlot()
    {
        var sync = MakeSync(out var catalog);
        var character = MakeBlankCharacter();

        var tplrRoot = NbtCompound.Of(
            ("inventory", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(
                    ("mod", new NbtString("CalamityMod")),
                    ("name", new NbtString("Abaddon")),
                    ("slot", new NbtShort(7)),
                    ("stack", new NbtInt(1))
                )
            ]))
        );

        var merged = sync.MergeAll(character, tplrRoot);

        var expectedId = catalog.ByModAndInternal("CalamityMod", "Abaddon")!.SyntheticId;
        Assert.Equal(expectedId, merged["inventory"][7].Id);
        Assert.True(merged["inventory"][7].IsCalamity);
        // Los demas slots del contenedor siguen vacios/vanilla.
        Assert.True(merged["inventory"][0].IsEmpty);
    }

    [Fact]
    public void MaskAndSyncAll_CalamityItem_ClearsPlrSlot_AndWritesTplrEntry()
    {
        var sync = MakeSync(out var catalog);
        var character = MakeBlankCharacter();

        var calamityId = catalog.ByModAndInternal("CalamityMod", "Calamity")!.SyntheticId;
        var merged = new Dictionary<string, GameItem[]>
        {
            ["inventory"] = character.Inventory.ToGameItems(),
            ["bank"] = character.BankItems.ToGameItems(),
            ["bank2"] = character.SafeItems.ToGameItems(),
            ["bank3"] = character.ForgeItems.ToGameItems(),
            ["bank4"] = character.VoidItems.ToGameItems(),
            ["miscEquips"] = character.EquipmentItems.ToGameItems(),
            ["miscDyes"] = character.EquipmentDyes.ToGameItems(),
        };
        merged["inventory"][3] = new GameItem { Id = calamityId, Count = 1, Prefix = ItemPrefix.CalamitySynthetic(10000) };

        var tplrRoot = sync.MaskAndSyncAll(character, merged, existingTplrRoot: null);

        // El .plr enmascarado no debe conocer el objeto de Calamity en absoluto.
        Assert.True(character.Inventory[3].IsEmpty);
        Assert.Equal(0, character.Inventory[3].Id);

        // El .tplr si lo tiene, con su prefijo real de Calamity.
        var invList = (NbtList)tplrRoot.Get("inventory")!;
        Assert.Single(invList.Items);
        var entry = (NbtCompound)invList.Items[0];
        Assert.Equal("CalamityMod", ((NbtString)entry.Get("mod")!).Value);
        Assert.Equal("Calamity", ((NbtString)entry.Get("name")!).Value);
        Assert.Equal((short)3, ((NbtShort)entry.Get("slot")!).Value);
        Assert.Equal("Vicious", ((NbtString)entry.Get("modPrefixName")!).Value);
    }

    [Fact]
    public void RoundTrip_MergeThenMaskAndSync_PreservesCalamityItem()
    {
        var sync = MakeSync(out var catalog);
        var character = MakeBlankCharacter();

        var tplrRoot = NbtCompound.Of(
            ("inventory", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(
                    ("mod", new NbtString("CalamityMod")),
                    ("name", new NbtString("Abaddon")),
                    ("slot", new NbtShort(10)),
                    ("stack", new NbtInt(1))
                )
            ]))
        );

        var merged = sync.MergeAll(character, tplrRoot);
        var newTplrRoot = sync.MaskAndSyncAll(character, merged, tplrRoot);

        // Releer desde cero el resultado (como si se hubiera guardado y vuelto a cargar).
        var reMerged = sync.MergeAll(character, newTplrRoot);
        var expectedId = catalog.ByModAndInternal("CalamityMod", "Abaddon")!.SyntheticId;
        Assert.Equal(expectedId, reMerged["inventory"][10].Id);
    }

    [Fact]
    public void MaskAndSyncAll_VanillaItem_NeverAppearsInTplr()
    {
        var sync = MakeSync(out _);
        var character = MakeBlankCharacter();
        var merged = new Dictionary<string, GameItem[]>
        {
            ["inventory"] = character.Inventory.ToGameItems(),
            ["bank"] = character.BankItems.ToGameItems(),
            ["bank2"] = character.SafeItems.ToGameItems(),
            ["bank3"] = character.ForgeItems.ToGameItems(),
            ["bank4"] = character.VoidItems.ToGameItems(),
            ["miscEquips"] = character.EquipmentItems.ToGameItems(),
            ["miscDyes"] = character.EquipmentDyes.ToGameItems(),
        };
        merged["inventory"][0] = new GameItem { Id = 1, Count = 1, Prefix = ItemPrefix.Vanilla(5) };

        var tplrRoot = sync.MaskAndSyncAll(character, merged, existingTplrRoot: null);

        Assert.Equal(1, character.Inventory[0].Id); // vanilla intacto en el .plr
        var invList = (NbtList)tplrRoot.Get("inventory")!;
        Assert.Empty(invList.Items); // nada de Calamity que escribir
    }
}
