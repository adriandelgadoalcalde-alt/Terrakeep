using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Nbt;
using Xunit;

namespace TerrasavrNative.Core.Tests.Calamity;

// Encargo del usuario 4-sep-2026 ("en el inicio los personajes que tienen mod solo marcan
// calamity... que sean personajes verdaderamente de tmodloader") - ver
// ESPEC-sprites-botones-badges.md#C. El caso mas importante de todos (el 1) es el que NO existe
// en ningun .tplr real de esta maquina (los 4 tienen contenido real de Calamity) - sin esta
// prueba sintetica, el bug ("cualquier .tplr enciende la insignia de Calamity") podria volver a
// colarse sin que ningun test real lo pillara.
public class TplrProbeTests
{
    [Fact]
    public void TieneContenidoDeCalamity_SoloEntradasVanillaOModAjeno_EsFalse()
    {
        var root = NbtCompound.Of(
            ("inventory", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(("mod", new NbtString("Terraria")), ("name", new NbtString("IronBroadsword")), ("slot", new NbtShort(0)))
            ])),
            ("usedMods", new NbtList(NbtTagType.String, [new NbtString("HEROsMod")]))
        );

        var summary = TplrProbe.From(root);

        Assert.False(summary.HasCalamityContent);
        Assert.Equal(["HEROsMod"], summary.UsedMods);
    }

    [Fact]
    public void TieneContenidoDeCalamity_ObjetoDeCalamityEnInventario_EsTrue()
    {
        var root = NbtCompound.Of(
            ("inventory", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("Abaddon")), ("slot", new NbtShort(7)))
            ]))
        );

        Assert.True(TplrProbe.From(root).HasCalamityContent);
    }

    [Fact]
    public void TieneContenidoDeCalamity_ObjetoDentroDeUnLoadout_EsTrue()
    {
        var root = NbtCompound.Of(
            ("loadouts", NbtCompound.Of(
                ("loadout0Armor", new NbtList(NbtTagType.Compound, [
                    NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("Calamity")), ("slot", new NbtShort(12)))
                ]))
            ))
        );

        Assert.True(TplrProbe.From(root).HasCalamityContent);
    }

    [Fact]
    public void TieneContenidoDeCalamity_BuffDeCalamity_EsTrue()
    {
        var root = NbtCompound.Of(
            ("modBuffs", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(("mod", new NbtString("CalamityMod")), ("name", new NbtString("AbandonedSlimeBuff")), ("time", new NbtInt(300)))
            ]))
        );

        Assert.True(TplrProbe.From(root).HasCalamityContent);
    }

    [Fact]
    public void UsedMods_ConTresCadenas_ConservaElOrdenReal()
    {
        var root = NbtCompound.Of(
            ("usedMods", new NbtList(NbtTagType.String, [
                new NbtString("CalamityModMusic"), new NbtString("CalamityMod"), new NbtString("HEROsMod"),
            ]))
        );

        Assert.Equal(["CalamityModMusic", "CalamityMod", "HEROsMod"], TplrProbe.From(root).UsedMods);
    }

    [Fact]
    public void UsedMods_Ausente_EsListaVaciaNuncaNull()
    {
        // Caso real de esta maquina: prueba.tplr (94 bytes, escrito por el propio Terrakeep,
        // MaskAndSyncAll no escribe usedMods) - no tiene la clave en absoluto.
        var root = NbtCompound.Of(
            ("inventory", new NbtList(NbtTagType.Compound, []))
        );

        var mods = TplrProbe.From(root).UsedMods;

        Assert.NotNull(mods);
        Assert.Empty(mods);
    }

    [Fact]
    public void TryRead_FicheroDeBasura_DevuelveNullSinLanzar()
    {
        string path = Path.Combine(Path.GetTempPath(), $"tplr-basura-{Guid.NewGuid():N}.tplr");
        File.WriteAllBytes(path, [1, 2, 3, 4, 5]); // no es gzip valido

        var summary = TplrProbe.TryRead(path);

        Assert.Null(summary);
        File.Delete(path);
    }

    // Round-trip real contra TplrFile.Write/Read (no solo NbtCompound en memoria) - confirma que
    // el .tplr sintetico que fabrica el arnes de UI Automation (D.3, caso "tModLoader sin
    // Calamity") se lee de verdad igual que uno real de tModLoader.
    [Fact]
    public void TryRead_TplrEscritoAMano_SoloModVanilla_DetectaAusenciaDeCalamity()
    {
        var root = NbtCompound.Of(
            ("inventory", new NbtList(NbtTagType.Compound, [
                NbtCompound.Of(("mod", new NbtString("Terraria")), ("name", new NbtString("IronBroadsword")), ("slot", new NbtShort(0)))
            ])),
            ("usedMods", new NbtList(NbtTagType.String, [new NbtString("HEROsMod")]))
        );
        string path = Path.Combine(Path.GetTempPath(), $"tplr-vanilla-{Guid.NewGuid():N}.tplr");
        File.WriteAllBytes(path, TplrFile.Write("Player", root));

        var summary = TplrProbe.TryRead(path);

        Assert.NotNull(summary);
        Assert.False(summary!.HasCalamityContent);
        Assert.Equal(["HEROsMod"], summary.UsedMods);
        File.Delete(path);
    }
}
