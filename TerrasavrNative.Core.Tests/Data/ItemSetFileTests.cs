using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.Model;
using Xunit;

namespace TerrasavrNative.Core.Tests.Data;

// H5-03 (quinta auditoria de Opus): "guardar/cargar conjuntos de objetos, funcion de primera
// clase en el Terrasavr original" - mismo resourceType/forma real por slot (ob.procItem,
// reference/terrasavr-real/script.beautified.js:5864) que el JSON real que ya escribia el
// motor original, extendido para Calamity (mod+nombre interno, nunca el id sintetico de este
// puerto).
public sealed class ItemSetFileTests
{
    private const string AppAssetsDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets";

    private static readonly CalamityCatalog Calamity = CalamityCatalog.LoadFromFile(Path.Combine(AppAssetsDir, "calamity", "catalog.json"));
    private static readonly RoguePrefixCatalog RoguePrefixes = RoguePrefixCatalog.LoadFromFile(Path.Combine(AppAssetsDir, "calamity", "rogue_prefixes.json"));

    [Fact]
    public void RoundTrip_ObjetoVanillaConPrefijoYFavorito_SeConservaEntero()
    {
        var item = new GameItem { Id = 4, Count = 1, Prefix = TerrasavrNative.Core.Model.ItemPrefix.Vanilla(1), Favorited = true }; // Iron Broadsword, prefijo real

        var file = ItemSetFile.FromItems([item], Calamity, RoguePrefixes);
        byte[] bytes = file.Write();
        var read = ItemSetFile.Read(bytes);
        var items = read.ToItems(Calamity, RoguePrefixes);

        Assert.Equal(ItemSetFile.ResourceType, read.ResourceType_);
        Assert.Single(items);
        Assert.Equal(4, items[0].Id);
        Assert.Equal(1, items[0].Count);
        Assert.False(items[0].Prefix.IsCalamity);
        Assert.Equal((byte)1, items[0].Prefix.VanillaId);
        Assert.True(items[0].Favorited);
    }

    [Fact]
    public void RoundTrip_ObjetoDeCalamity_SeIdentificaPorModEInternalNoPorIdSintetico()
    {
        var entry = Calamity.Entries[0];
        var item = new GameItem { Id = entry.SyntheticId, Count = 1 };

        var file = ItemSetFile.FromItems([item], Calamity, RoguePrefixes);
        var json = System.Text.Encoding.UTF8.GetString(file.Write());

        Assert.Contains(entry.Internal, json);
        Assert.DoesNotContain(entry.SyntheticId.ToString(), json); // nunca el id sintetico crudo

        var items = ItemSetFile.Read(file.Write()).ToItems(Calamity, RoguePrefixes);
        Assert.Equal(entry.SyntheticId, items[0].Id);
    }

    [Fact]
    public void SlotVacio_SeGuardaComoNullNuncaComoIdCero()
    {
        var file = ItemSetFile.FromItems([GameItem.Empty], Calamity, RoguePrefixes);
        Assert.Null(file.Slots[0]);

        var items = file.ToItems(Calamity, RoguePrefixes);
        Assert.True(items[0].IsEmpty);
    }

    [Fact]
    public void Read_ResourceTypeAjeno_Rechaza()
    {
        string json = """{"resourceType":"OtraCosa","resourceVersion":"1.0","slots":[]}""";
        Assert.Throws<InvalidDataException>(() => ItemSetFile.Read(System.Text.Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void ObjetoCalamityYaNoInstalado_NoSeInventa_SlotVacio()
    {
        string json = """{"resourceType":"TerrakeepItems","resourceVersion":"1.0","slots":[{"mod":"ModQueYaNoExiste","internal":"AlgoInventado","count":1,"prefix":0,"favorited":false}]}""";
        var file = ItemSetFile.Read(System.Text.Encoding.UTF8.GetBytes(json));
        var items = file.ToItems(Calamity, RoguePrefixes);
        Assert.True(items[0].IsEmpty);
    }

    [Fact]
    public void RoundTrip_PrefijoRogueDeCalamity_SeIdentificaPorNombreInterno()
    {
        var prefixEntry = RoguePrefixes.Weapon[0];
        var item = new GameItem { Id = 4, Count = 1, Prefix = TerrasavrNative.Core.Model.ItemPrefix.CalamitySynthetic(prefixEntry.Id) };

        var file = ItemSetFile.FromItems([item], Calamity, RoguePrefixes);
        var json = System.Text.Encoding.UTF8.GetString(file.Write());
        Assert.Contains(prefixEntry.Internal, json);

        var items = ItemSetFile.Read(file.Write()).ToItems(Calamity, RoguePrefixes);
        Assert.True(items[0].Prefix.IsCalamity);
        Assert.Equal(prefixEntry.Id, items[0].Prefix.SyntheticId);
    }
}
