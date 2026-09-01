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

    private const string BuffsJson = """
    [
      { "internal": "AbandonedSlimeBuff", "mod": "CalamityMod", "displayName_es": "Gelatina Astral", "displayName_fallback": "Abandoned Slime" }
    ]
    """;

    private static CalamityCharacterSync MakeSync(out CalamityCatalog catalog) => MakeSync(out catalog, out _);

    private static CalamityCharacterSync MakeSync(out CalamityCatalog catalog, out CalamityBuffCatalog buffCatalog)
    {
        catalog = CalamityCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(CatalogJson)));
        buffCatalog = CalamityBuffCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(BuffsJson)));
        var prefixes = RoguePrefixCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(RoguePrefixesJson)));
        var translator = new CalamityPrefixTranslator(prefixes);
        var codec = new CalamityItemCodec(catalog, translator);
        return new CalamityCharacterSync(codec, buffCatalog);
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
        for (int i = 0; i < 44; i++) character.Buffs.Add(new PlrBuff { Id = 0, Time = 0 });
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

    [Fact]
    public void MergeAll_ModBuffs_FillsEmptySlots_VanillaAndCalamityMixed()
    {
        var sync = MakeSync(out _, out var buffCatalog);
        var character = MakeBlankCharacter();
        // Caso realista: el .plr de un personaje modeado normalmente trae su propio array de
        // buffs YA VACIO (tModLoader deja de escribirlo en cuanto hay mods instalados, ver el
        // comentario de MergeBuffs) - modBuffs es la fuente completa. Aqui se simula ademas el
        // caso raro de que el array nativo SI trajera algo que no aparece en modBuffs (id=99,
        // dato antiguo/ajeno): debe respetarse sin duplicar, y los buffs de modBuffs caen en
        // los siguientes huecos libres en orden.
        character.Buffs[0] = new PlrBuff { Id = 99, Time = 100 };

        var tplrRoot = NbtCompound.Of(
            ("modBuffs", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(("mod", new NbtString("Terraria")), ("id", new NbtInt(12)), ("time", new NbtInt(600))),
                NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("AbandonedSlimeBuff")), ("time", new NbtInt(300)))
            ]))
        );

        var merged = sync.MergeAll(character, tplrRoot);

        // El slot 0 ya estaba ocupado (dato ajeno a modBuffs) - se respeta, no se toca.
        Assert.Equal(99, character.Buffs[0].Id);
        // Los dos buffs de modBuffs (uno vanilla, uno de Calamity) caen en los siguientes
        // huecos libres, EN ORDEN.
        Assert.Equal(12, character.Buffs[1].Id);
        Assert.Equal(600, character.Buffs[1].Time);
        var expectedBuffId = buffCatalog.ByModAndInternal("CalamityMod", "AbandonedSlimeBuff")!.SyntheticId;
        Assert.Equal(expectedBuffId, character.Buffs[2].Id);
        Assert.Equal(300, character.Buffs[2].Time);
    }

    [Fact]
    public void MaskAndSyncAll_WritesModBuffs_VanillaAsIdCalamityAsModName()
    {
        var sync = MakeSync(out _, out var buffCatalog);
        var character = MakeBlankCharacter();
        var calamityBuffId = buffCatalog.ByModAndInternal("CalamityMod", "AbandonedSlimeBuff")!.SyntheticId;
        character.Buffs[0] = new PlrBuff { Id = 12, Time = 600 };
        character.Buffs[1] = new PlrBuff { Id = calamityBuffId, Time = 300 };

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

        var tplrRoot = sync.MaskAndSyncAll(character, merged, existingTplrRoot: null);

        // A diferencia de los items, el id sintetico de Calamity NO se enmascara en el .plr -
        // se escribe tal cual (ver el comentario de SyncBuffs: no corrompe la carga como si lo
        // hace un id de objeto fuera de rango, y tModLoader ignora este array igualmente en
        // cuanto hay mods instalados).
        Assert.Equal(calamityBuffId, character.Buffs[1].Id);

        var buffList = (NbtList)tplrRoot.Get("modBuffs")!;
        Assert.Equal(2, buffList.Items.Count);
        var vanillaEntry = (NbtCompound)buffList.Items[0];
        Assert.Equal("Terraria", ((NbtString)vanillaEntry.Get("mod")!).Value);
        Assert.Equal(12, ((NbtInt)vanillaEntry.Get("id")!).Value);
        var calamityEntry = (NbtCompound)buffList.Items[1];
        Assert.Equal("CalamityMod", ((NbtString)calamityEntry.Get("mod")!).Value);
        Assert.Equal("AbandonedSlimeBuff", ((NbtString)calamityEntry.Get("name")!).Value);
        Assert.Equal(300, ((NbtInt)calamityEntry.Get("time")!).Value);
    }

    [Fact]
    public void RoundTrip_MergeThenMaskAndSync_PreservesCalamityBuff()
    {
        var sync = MakeSync(out _, out var buffCatalog);
        var character = MakeBlankCharacter();

        var tplrRoot = NbtCompound.Of(
            ("modBuffs", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("AbandonedSlimeBuff")), ("time", new NbtInt(300)))
            ]))
        );

        var merged = sync.MergeAll(character, tplrRoot);
        var newTplrRoot = sync.MaskAndSyncAll(character, merged, tplrRoot);

        var reCharacter = MakeBlankCharacter();
        sync.MergeAll(reCharacter, newTplrRoot);

        var expectedBuffId = buffCatalog.ByModAndInternal("CalamityMod", "AbandonedSlimeBuff")!.SyntheticId;
        Assert.Equal(expectedBuffId, reCharacter.Buffs[0].Id);
        Assert.Equal(300, reCharacter.Buffs[0].Time);
    }

    [Fact]
    public void MergeAll_FlatArmorDye_WithCurrentLoadoutZero_MergesIntoPrimaryLoadout()
    {
        var sync = MakeSync(out var catalog);
        var character = MakeBlankCharacter(); // CurrentLoadout=0 por defecto

        var tplrRoot = NbtCompound.Of(
            ("armor", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("Abaddon")), ("slot", new NbtShort(3))), // accesorio (items[3])
                NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("Calamity")), ("slot", new NbtShort(12))) // vanidad (social[2])
            ])),
            ("dye", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("Abaddon")), ("slot", new NbtShort(1)))
            ]))
        );

        var merged = sync.MergeAll(character, tplrRoot);

        var abaddonId = catalog.ByModAndInternal("CalamityMod", "Abaddon")!.SyntheticId;
        var calamityId = catalog.ByModAndInternal("CalamityMod", "Calamity")!.SyntheticId;
        Assert.Equal(abaddonId, merged["loadout0Items"][3].Id);
        Assert.Equal(calamityId, merged["loadout0Social"][2].Id);
        Assert.Equal(abaddonId, merged["loadout0Dyes"][1].Id);
        // El resto de loadouts (si version>=269 los tuviera) no existen aqui - solo el mirror.
        Assert.False(merged.ContainsKey("loadout1Items"));
    }

    [Fact]
    public void MergeAll_FlatArmorDye_ActiveLoadoutQuirk_CurrentLoadoutOneTargetsLoadoutsZero_NotPrimary()
    {
        // calamityActiveLoadout(player) = player.loadouts[currentLoadout] SIN el +1 que si usa
        // el guardado vanilla nativo - con CurrentLoadout=1 esto cae en Loadouts[0] (indice 1
        // del array conceptual [PrimaryLoadout, ...Loadouts]), NUNCA en PrimaryLoadout. Ver el
        // comentario de MergeLoadoutArmorDye.
        var sync = MakeSync(out var catalog);
        var character = MakeBlankCharacter();
        character.Loadouts = [PlrLoadout.CreateEmpty(false), PlrLoadout.CreateEmpty(false), PlrLoadout.CreateEmpty(false)];
        character.CurrentLoadout = 1;

        var tplrRoot = NbtCompound.Of(
            ("armor", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("Abaddon")), ("slot", new NbtShort(0)))
            ]))
        );

        var merged = sync.MergeAll(character, tplrRoot);

        var abaddonId = catalog.ByModAndInternal("CalamityMod", "Abaddon")!.SyntheticId;
        Assert.Equal(abaddonId, merged["loadout1Items"][0].Id); // Loadouts[0], no el mirror
        Assert.True(merged["loadout0Items"][0].IsEmpty); // el mirror (PrimaryLoadout) NO se toca
    }

    [Fact]
    public void RoundTrip_MergeThenMaskAndSync_PreservesLoadoutArmorAndDye()
    {
        var sync = MakeSync(out var catalog);
        var character = MakeBlankCharacter();

        var tplrRoot = NbtCompound.Of(
            ("armor", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("Abaddon")), ("slot", new NbtShort(3)))
            ])),
            ("dye", new NbtList(NbtTagType.Compound, []))
        );

        var merged = sync.MergeAll(character, tplrRoot);
        var abaddonId = catalog.ByModAndInternal("CalamityMod", "Abaddon")!.SyntheticId;
        Assert.Equal(abaddonId, merged["loadout0Items"][3].Id);

        var newTplrRoot = sync.MaskAndSyncAll(character, merged, tplrRoot);

        // El .plr enmascarado no debe conocer el objeto de Calamity en el slot de armadura.
        Assert.True(character.PrimaryLoadout.Items[3].IsEmpty);

        // Releido desde cero, el mismo objeto debe reaparecer en el mismo slot.
        var reCharacter = MakeBlankCharacter();
        var reMerged = sync.MergeAll(reCharacter, newTplrRoot);
        Assert.Equal(abaddonId, reMerged["loadout0Items"][3].Id);
    }
}
