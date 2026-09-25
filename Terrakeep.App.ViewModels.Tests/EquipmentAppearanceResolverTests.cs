using System.IO;
using System.Linq;
using Terrakeep.App.Services;
using Terrakeep.Core.Model;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

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

    // PortSeleccion Encargo1 (25-sep-2026): ResolveAccessories, los 7 slots de accesorio
    // funcional/vanidad (indices 3..9, genericos - ver el comentario real de la clase). Ids
    // reales, spot-check ya hecho a mano en Item.cs antes de escribir extraer-slots-
    // accesorios-vanilla.py: Copper Watch (id 15, waistSlot=2), Silver Watch (id 16,
    // waistSlot=7), Cobalt Shield (id 156, shieldSlot=1), Cross Necklace (id 554, neckSlot=2).
    private const int RelojCobre = 15;   // waistSlot=2
    private const int RelojPlata = 16;   // waistSlot=7 (mismo TIPO que RelojCobre, otro slot)
    private const int EscudoCobalto = 156; // shieldSlot=1
    private const int ColgantePlata = 554; // neckSlot=2

    private static PlrLoadout LoadoutConAccesorio(int itemsIndex, int itemsId, int? socialIndex = null, int socialId = 0)
    {
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[itemsIndex] = new PlrItemSlot(itemsId, 1, 0, false);
        if (socialIndex is int si) loadout.Social[si] = new PlrItemSlot(socialId, 1, 0, false);
        return loadout;
    }

    [Fact]
    public void AccesorioVanillaFuncional_ResuelveUnSpriteRealQueExisteEnDisco()
    {
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(3, RelojCobre));

        Assert.NotNull(acc.WaistFile);
        Assert.True(File.Exists(acc.WaistFile));
        Assert.EndsWith("acc_waist" + Path.DirectorySeparatorChar + "2.png", acc.WaistFile);
        Assert.Equal(2, acc.WaistSlot);
    }

    [Fact]
    public void VanidadEnOtroHuecoGenerico_TapaAlFuncionalDelMismoTipo_FielAlGuardado()
    {
        // Funcional: Reloj de cobre (waistSlot=2) en el hueco generico 3. Vanidad: Reloj de
        // plata (waistSlot=7, MISMO TIPO, distinto valor) en el hueco generico 7 - un indice
        // totalmente distinto del funcional, porque los 7 huecos de accesorio son genericos
        // (a diferencia de cabeza/cuerpo/piernas, NO hay correspondencia 1:1 de indice). El
        // juego real muestra el de VANIDAD por TIPO, no por indice compartido.
        var loadout = LoadoutConAccesorio(3, RelojCobre, socialIndex: 7, socialId: RelojPlata);

        var acc = Service.EquipmentAppearance.ResolveAccessories(loadout);

        Assert.NotNull(acc.WaistFile);
        Assert.EndsWith("acc_waist" + Path.DirectorySeparatorChar + "7.png", acc.WaistFile);
        Assert.Equal(7, acc.WaistSlot);
    }

    [Fact]
    public void SlotVacio_NoResuelveNingunAccesorio()
    {
        var acc = Service.EquipmentAppearance.ResolveAccessories(PlrLoadout.CreateEmpty(isPrimary: true));

        Assert.Null(acc.WaistFile);
        Assert.Null(acc.NeckFile);
        Assert.Null(acc.HandOnFile);
        Assert.Null(acc.HandOffFile);
        Assert.Null(acc.BackFile);
        Assert.Null(acc.ShieldFile);
        Assert.Null(acc.FaceFile);
    }

    [Fact]
    public void TresTiposDistintosEnHuecosDistintos_ResuelvenIndependientemente()
    {
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[3] = new PlrItemSlot(RelojCobre, 1, 0, false);      // waist
        loadout.Items[4] = new PlrItemSlot(EscudoCobalto, 1, 0, false);   // shield
        loadout.Items[5] = new PlrItemSlot(ColgantePlata, 1, 0, false);   // neck

        var acc = Service.EquipmentAppearance.ResolveAccessories(loadout);

        Assert.NotNull(acc.WaistFile);
        Assert.EndsWith("acc_waist" + Path.DirectorySeparatorChar + "2.png", acc.WaistFile);
        Assert.NotNull(acc.ShieldFile);
        Assert.EndsWith("acc_shield" + Path.DirectorySeparatorChar + "1.png", acc.ShieldFile);
        Assert.NotNull(acc.NeckFile);
        Assert.EndsWith("acc_neck" + Path.DirectorySeparatorChar + "2.png", acc.NeckFile);
        Assert.Null(acc.HandOnFile);
        Assert.Null(acc.HandOffFile);
        Assert.Null(acc.BackFile);
        Assert.Null(acc.FaceFile);
    }

    [Fact]
    public void ObjetoDeArmaduraCalamityEnHuecoDeAccesorio_NoSeCuelaComoAccesorio()
    {
        // Una pieza de armadura de CUERPO de Calamity (EquipSlot=="Body") puesta en un hueco
        // GENERICO de accesorio (dato incoherente, no deberia darse en un .plr real, pero el
        // resolver no debe inventarse un sprite igualmente - mismo criterio defensivo que
        // ObjetoCalamityDeUnSlotDistinto_NoSeCuelaEnOtroHueco para armadura).
        var entry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "Body");
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[3] = new PlrItemSlot(entry.SyntheticId, 1, 0, false);

        var acc = Service.EquipmentAppearance.ResolveAccessories(loadout);

        Assert.Null(acc.WaistFile);
        Assert.Null(acc.NeckFile);
        Assert.Null(acc.HandOnFile);
        Assert.Null(acc.HandOffFile);
        Assert.Null(acc.BackFile);
        Assert.Null(acc.ShieldFile);
        Assert.Null(acc.FaceFile);
    }
}
