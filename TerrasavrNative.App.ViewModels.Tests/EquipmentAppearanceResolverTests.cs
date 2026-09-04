using System.IO;
using System.Linq;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels.Tests;

// Pedido explicito del usuario (3-sep-2026): "los personajes de inicio no se visualizan como
// realmente son en el juego... que muestre el personaje con la vanidad que tiene cada uno
// pero fiel al guardado igual que lo que lleva puesto de vanidad". Verifica de extremo a
// extremo (no solo "deberia funcionar"): ids REALES de objetos vanilla/Calamity, contra el
// catalogo real cargado por CharacterFileService, con los sprites reales ya extraidos en
// disco (no solo que la ruta se calcule bien - que el fichero exista de verdad).
public sealed class EquipmentAppearanceResolverTests
{
    private static readonly CharacterFileService Service = new();

    // Casco de cobre, id 89 real (spot-check ya hecho en scripts/extraer-slots-armadura-
    // vanilla.js: headSlot=1) - mismo objeto ya usado como spot-check real en
    // vanilla_armor_sets.json ("MetalTier1", pieces=[89,80,76]).
    private const int CascoCobre = 89;
    private const int CascoCobreScale = 90; // hermano real del mismo set, headSlot=2

    private static PlrLoadout LoadoutConCabeza(int itemsHeadId, int socialHeadId = 0)
    {
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[0] = new PlrItemSlot(itemsHeadId, 1, 0, false);
        if (socialHeadId != 0) loadout.Social[0] = new PlrItemSlot(socialHeadId, 1, 0, false);
        return loadout;
    }

    [Fact]
    public void ObjetoVanillaFuncionalEnCabeza_ResuelveUnSpriteRealQueExisteEnDisco()
    {
        var armor = Service.EquipmentAppearance.Resolve(LoadoutConCabeza(CascoCobre));

        Assert.NotNull(armor.HeadFile);
        Assert.True(File.Exists(armor.HeadFile));
        Assert.EndsWith("armor_head" + Path.DirectorySeparatorChar + "1.png", armor.HeadFile); // headSlot real del Casco de cobre
    }

    [Fact]
    public void VanidadPuesta_TapaAlObjetoFuncional_FielAlGuardado()
    {
        // Casco de cobre puesto de verdad, pero con OTRO casco (headSlot=2) en el slot de
        // vanidad - el juego real muestra el de VANIDAD, no el funcional.
        var armor = Service.EquipmentAppearance.Resolve(LoadoutConCabeza(CascoCobre, CascoCobreScale));

        Assert.NotNull(armor.HeadFile);
        Assert.EndsWith("armor_head" + Path.DirectorySeparatorChar + "2.png", armor.HeadFile);
    }

    [Fact]
    public void SlotVacio_NoResuelveNingunSprite()
    {
        var armor = Service.EquipmentAppearance.Resolve(PlrLoadout.CreateEmpty(isPrimary: true));

        Assert.Null(armor.HeadFile);
        Assert.Null(armor.BodyFile);
        Assert.Null(armor.LegsFile);
    }

    [Fact]
    public void ObjetoCalamityRealConEquipSlot_ResuelveElSpriteRealYaExtraidoDelTmod()
    {
        // Cualquier pieza real de Calamity con EquipSlot=="Body" ya conocido (185/185 tienen
        // su sprite real extraido, ver bitacora.md) - no se hardcodea un id concreto, se pide
        // al catalogo real cual es el primero, igual de valido y mas resistente a que el
        // catalogo cambie de orden.
        var entry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "Body");
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[1] = new PlrItemSlot(entry.SyntheticId, 1, 0, false);

        var armor = Service.EquipmentAppearance.Resolve(loadout);

        Assert.NotNull(armor.BodyFile);
        Assert.True(File.Exists(armor.BodyFile));
        Assert.EndsWith(entry.Internal + "_Body.png", armor.BodyFile);
    }

    [Fact]
    public void ObjetoCalamityDeUnSlotDistinto_NoSeCuelaEnOtroHueco()
    {
        // Una pieza de CUERPO puesta en el slot de CABEZA (dato incoherente, no deberia darse
        // en un .plr real, pero el resolver no debe inventarse un sprite igualmente) - defensa
        // ya explicita en EquipmentAppearanceResolver.Resolve (comprobacion EquipSlot).
        var entry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "Body");
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[0] = new PlrItemSlot(entry.SyntheticId, 1, 0, false);

        var armor = Service.EquipmentAppearance.Resolve(loadout);

        Assert.Null(armor.HeadFile);
    }

    [Fact]
    public void RenderConArmaduraRealDaUnaImagenDistintaASinArmadura()
    {
        // Verificacion de extremo a extremo real (no solo que la ruta se calcule bien): el
        // propio PlayerPreviewRenderer.Render produce pixeles distintos cuando se le pasa la
        // capa de armadura real resuelta - si algun dia el compositor deja de leer 'armor' por
        // un refactor descuidado, esta prueba lo pilla en seco.
        var colors = new PlayerPreviewRenderer.PlayerColors(
            new(150, 90, 50), new(255, 220, 177), new(80, 50, 30),
            new(130, 60, 60), new(200, 180, 160), new(70, 70, 120), new(90, 60, 40));

        var sinArmadura = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, colors);
        var armor = Service.EquipmentAppearance.Resolve(LoadoutConCabeza(CascoCobre));
        var conArmadura = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, colors, armor);

        var pixelesSin = new byte[sinArmadura.PixelHeight * sinArmadura.PixelWidth * 4];
        sinArmadura.CopyPixels(pixelesSin, sinArmadura.PixelWidth * 4, 0);
        var pixelesCon = new byte[conArmadura.PixelHeight * conArmadura.PixelWidth * 4];
        conArmadura.CopyPixels(pixelesCon, conArmadura.PixelWidth * 4, 0);

        Assert.NotEqual(pixelesSin, pixelesCon);
    }
}
