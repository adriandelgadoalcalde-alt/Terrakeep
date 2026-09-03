using System.IO;
using System.Linq;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// H3-05 (tercera auditoria de Opus, Fable): antes de este arreglo, los 21 ModPrefix reales de
// Calamity (RoguePrefixCatalog) solo se podian conseguir via "mejor prefijo" automatico - el
// picker manual del panel Editar nunca los enseñaba (armas Picaro solo veian "Universal +/-",
// ningun accesorio veia los 4 de accesorio real). Pruebas deterministas contra datos reales del
// catalogo de Calamity (Aerial Tracker, indice 1905 -> id sintetico 20001905, categoria real
// "Weapons/DraedonsArsenal", damageType real "RogueDamageClass.Instance").
public sealed class RoguePrefixPickerTests
{
    private const int AerialTrackerSyntheticId = CalamityIds.ItemIdBase + 1905;
    // Escudo de obsidiana - accesorio VANILLA real (id 397), usado para probar que los 4
    // ModPrefix reales de accesorio de Calamity (Category=Accessory, CanRoll universal en el
    // propio juego decompilado) se unen de verdad al pool vanilla, no solo a Calamity.
    private const int ObsidianShieldId = 397;

    private static MainViewModel NewLoadedViewModel()
    {
        var character = new PlrCharacter
        {
            Name = "Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        string path = Path.Combine(Path.GetTempPath(), $"rogue-prefix-test-{Guid.NewGuid():N}.plr");
        File.WriteAllBytes(path, PlrFile.Write(character));
        var vm = new MainViewModel();
        vm.LoadFromPath(path);
        File.Delete(path);
        return vm;
    }

    private static PrefixGroupButtonViewModel SelectPositivosPicaro(MainViewModel vm)
    {
        var positivos = vm.ItemEdit.Metas.Single(m => m.Meta.NameEs == "Positivos");
        vm.ItemEdit.SelectMetaCommand.Execute(positivos);
        var picaro = vm.ItemEdit.Groups.Single(g => g.Group.NameEs == "Pícaro");
        vm.ItemEdit.SelectGroupCommand.Execute(picaro);
        return picaro;
    }

    [Fact]
    public void UnArmaPicaroDeCalamity_MuestraElGrupoPicaroConLos17PrefijosReales()
    {
        var vm = NewLoadedViewModel();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(AerialTrackerSyntheticId);
        vm.SelectSlot(slot);

        SelectPositivosPicaro(vm);

        Assert.Equal(17, vm.ItemEdit.Prefixes.Count);
        Assert.All(vm.ItemEdit.Prefixes, p => Assert.True(p.Prefix.IsCalamity && p.Prefix.SyntheticId >= CalamityIds.PrefixIdBase));
    }

    [Fact]
    public void AplicarUnPrefijoPicaroReal_LoColocaYLoMarcaComoActual()
    {
        var vm = NewLoadedViewModel();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(AerialTrackerSyntheticId);
        vm.SelectSlot(slot);
        SelectPositivosPicaro(vm);

        var vicioso = vm.ItemEdit.Prefixes.Single(p => p.DisplayName == "Vicioso");
        Assert.Equal("+10% de daño, -5% de tiempo de uso, +15% de velocidad de disparo", vicioso.EffectDescription);

        vm.ItemEdit.ApplyPrefixCommand.Execute(vicioso);

        Assert.True(slot.Item.Prefix.IsCalamity);
        Assert.Equal(CalamityIds.PrefixIdBase, slot.Item.Prefix.SyntheticId); // Vicioso = id 10000
        var recargado = vm.ItemEdit.Prefixes.Single(p => p.DisplayName == "Vicioso");
        Assert.True(recargado.IsCurrent);
    }

    [Fact]
    public void UnAccesorioVanilla_TambienVeLos4PrefijosRealesDeAccesorioDeCalamity()
    {
        var vm = NewLoadedViewModel();
        var slot = vm.InventoryContainer!.Slots[0];
        slot.PlaceItem(ObsidianShieldId);
        vm.SelectSlot(slot);

        var positivos = vm.ItemEdit.Metas.Single(m => m.Meta.NameEs == "Positivos");
        vm.ItemEdit.SelectMetaCommand.Execute(positivos);
        var accesorio = vm.ItemEdit.Groups.Single(g => g.Group.NameEs == "Accesorio");
        vm.ItemEdit.SelectGroupCommand.Execute(accesorio);

        var silencioso = vm.ItemEdit.Prefixes.SingleOrDefault(p => p.DisplayName == "Silencioso");
        Assert.NotNull(silencioso);
        Assert.Contains("sigilo", silencioso!.EffectDescription);

        vm.ItemEdit.ApplyPrefixCommand.Execute(silencioso);
        Assert.True(slot.Item.Prefix.IsCalamity);
        Assert.Equal(CalamityIds.PrefixIdBase + 20, slot.Item.Prefix.SyntheticId); // Silent = id 10020
    }
}
