using System.Linq;
using System.Text;
using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

// H6-12 (sexta auditoria de Opus, "los buffs de Calamity no distinguen buff de debuff"):
// IsDebuff viene de verdad de Main.debuff[base.Type]=true en el propio ModBuff de Calamity
// (ver scripts/extraer-debuffs-calamity.js), no de una heuristica sobre el nombre/categoria.
public class CalamityBuffCatalogTests
{
    private const string BuffsJson = """
    [
      { "internal": "Malnourished", "mod": "CalamityMod", "category": "StatDebuffs", "displayName_es": "Desnutrido", "displayName_fallback": "Malnourished", "icon": "x.png" },
      { "internal": "AbandonedSlimeBuff", "mod": "CalamityMod", "category": "Summon", "displayName_es": "Gelatina Astral", "displayName_fallback": "Abandoned Slime", "icon": "y.png" },
      { "internal": "SinMencion", "mod": "CalamityMod", "category": "StatBuffs", "displayName_es": "Sin Mencion", "displayName_fallback": "Sin Mencion", "icon": "z.png" }
    ]
    """;

    private const string DebuffsJson = """{ "Malnourished": true }""";

    [Fact]
    public void IsDebuff_SoloVerdaderoParaLosPresentesEnElFicheroReal()
    {
        var catalog = CalamityBuffCatalog.LoadFromStream(
            new MemoryStream(Encoding.UTF8.GetBytes(BuffsJson)),
            new MemoryStream(Encoding.UTF8.GetBytes("{}")),
            new MemoryStream(Encoding.UTF8.GetBytes(DebuffsJson)));

        Assert.True(catalog.Entries[0].IsDebuff); // Malnourished - Main.debuff[base.Type]=true real
        Assert.False(catalog.Entries[1].IsDebuff); // AbandonedSlimeBuff - buff de invocacion, nunca debuff
        Assert.False(catalog.Entries[2].IsDebuff); // sin mencion en buff_debuffs.json -> false por defecto
    }

    private const string AssetsDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets";

    [Fact]
    public void RealFile_DebuffsConocidosCoincidenConMainDebuffReal()
    {
        string buffsPath = Path.Combine(AssetsDir, "calamity", "buffs.json");
        string descPath = Path.Combine(AssetsDir, "calamity_buff_descriptions.json");
        string debuffsPath = Path.Combine(AssetsDir, "calamity", "buff_debuffs.json");
        if (!File.Exists(buffsPath) || !File.Exists(descPath) || !File.Exists(debuffsPath)) return;

        var catalog = CalamityBuffCatalog.LoadFromFile(buffsPath, descPath, debuffsPath);

        // Spot-check real contra el codigo fuente de Calamity Mod decompilado (ver bitacora.md):
        // CalamityMod/Buffs/StatDebuffs/Malnourished.cs pone Main.debuff[base.Type]=true;
        // CalamityMod/Buffs/Summon/AbandonedSlimeBuff.cs hereda de BaseSummonBuff, que NUNCA
        // toca Main.debuff (los buffs de invocacion no son debuffs).
        var malnourished = catalog.Entries.First(e => e.Internal == "Malnourished");
        Assert.True(malnourished.IsDebuff);
        var abandonedSlime = catalog.Entries.First(e => e.Internal == "AbandonedSlimeBuff");
        Assert.False(abandonedSlime.IsDebuff);

        // Recuento real medido con scripts/extraer-debuffs-calamity.js (105 de 305 entradas del
        // catalogo real son debuffs) - un umbral con margen real, no el numero exacto, para no
        // romper la prueba si Calamity añade/quita un debuff en una actualizacion futura del mod.
        int debuffCount = catalog.Entries.Count(e => e.IsDebuff);
        Assert.InRange(debuffCount, 90, 130);
    }
}
