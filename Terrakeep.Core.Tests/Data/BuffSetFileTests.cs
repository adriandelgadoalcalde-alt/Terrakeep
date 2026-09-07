using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

// Pedido explicito del usuario (4-sep-2026): "la pestaña de buff no tiene nada de guardar json
// ni tampoco cargar para guardar combinaciones de buff" - gemelo real de ItemSetFileTests.cs
// (H5-03) para BuffSetFile.
public sealed class BuffSetFileTests
{
    private const string AppAssetsDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets";

    private static readonly CalamityBuffCatalog CalamityBuffs = CalamityBuffCatalog.LoadFromFile(
        Path.Combine(AppAssetsDir, "calamity", "buffs.json"),
        Path.Combine(AppAssetsDir, "calamity_buff_descriptions.json"),
        Path.Combine(AppAssetsDir, "calamity", "buff_debuffs.json"));

    [Fact]
    public void RoundTrip_BuffVanillaConDuracion_SeConservaEntero()
    {
        var file = BuffSetFile.FromBuffs([(1, 21600)], CalamityBuffs); // Obsidian Skin, id real, 360s reales
        byte[] bytes = file.Write();
        var read = BuffSetFile.Read(bytes);
        var buffs = read.ToBuffs(CalamityBuffs);

        Assert.Equal(BuffSetFile.ResourceType, read.ResourceType_);
        Assert.Single(buffs);
        Assert.Equal(1, buffs[0].Id);
        Assert.Equal(21600, buffs[0].Time);
    }

    [Fact]
    public void RoundTrip_BuffDeCalamity_SeIdentificaPorModEInternalNoPorIdSintetico()
    {
        var entry = CalamityBuffs.Entries[0];
        var file = BuffSetFile.FromBuffs([(entry.SyntheticId, 600)], CalamityBuffs);
        var json = System.Text.Encoding.UTF8.GetString(file.Write());

        Assert.Contains(entry.Internal, json);
        Assert.DoesNotContain(entry.SyntheticId.ToString(), json); // nunca el id sintetico crudo

        var buffs = BuffSetFile.Read(file.Write()).ToBuffs(CalamityBuffs);
        Assert.Equal(entry.SyntheticId, buffs[0].Id);
    }

    [Fact]
    public void SlotVacio_SeGuardaComoNullNuncaComoIdCero()
    {
        var file = BuffSetFile.FromBuffs([(0, 0)], CalamityBuffs);
        Assert.Null(file.Slots[0]);

        var buffs = file.ToBuffs(CalamityBuffs);
        Assert.Equal(0, buffs[0].Id);
    }

    [Fact]
    public void Read_ResourceTypeAjeno_Rechaza()
    {
        string json = """{"resourceType":"OtraCosa","resourceVersion":"1.0","slots":[]}""";
        Assert.Throws<InvalidDataException>(() => BuffSetFile.Read(System.Text.Encoding.UTF8.GetBytes(json)));
    }

    [Fact]
    public void BuffDeCalamityYaNoInstalado_NoSeInventa_SlotVacio()
    {
        string json = """{"resourceType":"TerrakeepBuffs","resourceVersion":"1.0","slots":[{"mod":"ModQueYaNoExiste","internal":"AlgoInventado","time":600}]}""";
        var file = BuffSetFile.Read(System.Text.Encoding.UTF8.GetBytes(json));
        var buffs = file.ToBuffs(CalamityBuffs);
        Assert.Equal(0, buffs[0].Id);
    }
}
