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
        Assert.Null(acc.BackpackFile);
        Assert.Null(acc.TailFile);
        Assert.Null(acc.ShoesFile); // GapAnalysis Encargo D (25-sep-2026)
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

    // GapAnalysis Encargo A (25-sep-2026): backSlot se clasifica en 3 canales reales posibles
    // (Player.cs:37169-37184, UpdateVisibleAccessory - ver BackAccessoryLayerTable, tabla real
    // de ArmorIDs.cs:1717/1719). Ids reales confirmados en vanilla_accessory_slots.json contra
    // vanilla_item_names_en.json de este PC (25-sep-2026): Magic Quiver (id 1321, backSlot=7,
    // DrawInBackpackLayer), Dog Tail (id 4769, backSlot=25, DrawInTailLayer), Bee Cloak (id 1247,
    // backSlot=1, ninguna de las 2 tablas - Back normal), los 3 con sprite ya extraido en
    // Assets/player/acc_back/.
    private const int MagicQuiver = 1321; // backSlot=7 -> Backpack
    private const int DogTail = 4769;     // backSlot=25 -> Tail
    private const int BeeCloak = 1247;    // backSlot=1 -> Back normal (ninguna tabla)

    [Fact]
    public void BackSlotEnTablaDeBackpack_SeReclasificaComoBackpackNoComoBack()
    {
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(3, MagicQuiver));

        Assert.NotNull(acc.BackpackFile);
        Assert.True(File.Exists(acc.BackpackFile));
        Assert.EndsWith("acc_back" + Path.DirectorySeparatorChar + "7.png", acc.BackpackFile);
        Assert.Equal(7, acc.BackpackSlot);
        Assert.Null(acc.BackFile);
        Assert.Null(acc.BackSlot);
        Assert.Null(acc.TailFile);
    }

    [Fact]
    public void BackSlotEnTablaDeTail_SeReclasificaComoTailNoComoBack()
    {
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(3, DogTail));

        Assert.NotNull(acc.TailFile);
        Assert.True(File.Exists(acc.TailFile));
        Assert.EndsWith("acc_back" + Path.DirectorySeparatorChar + "25.png", acc.TailFile);
        Assert.Equal(25, acc.TailSlot);
        Assert.Null(acc.BackFile);
        Assert.Null(acc.BackSlot);
        Assert.Null(acc.BackpackFile);
    }

    [Fact]
    public void BackSlotFueraDeAmbasTablas_SigueSiendoBackNormal()
    {
        // Tercer canal - el ya portado por PortSeleccion Encargo1, no debe romperse.
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(3, BeeCloak));

        Assert.NotNull(acc.BackFile);
        Assert.EndsWith("acc_back" + Path.DirectorySeparatorChar + "1.png", acc.BackFile);
        Assert.Equal(1, acc.BackSlot);
        Assert.Null(acc.BackpackFile);
        Assert.Null(acc.TailFile);
    }

    [Fact]
    public void VanidadDeBackpackTapaAlFuncionalDeBack_FielAlGuardado()
    {
        // Funcional: Bee Cloak (Back normal) en el hueco 3. Vanidad: Magic Quiver (Backpack) en
        // el hueco 7 - el juego real muestra el de VANIDAD, aunque caiga en un canal distinto
        // (Backpack en vez de Back), mismo criterio "vanidad tapa a funcional" ya verificado
        // para el resto de tipos.
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[3] = new PlrItemSlot(BeeCloak, 1, 0, false);
        loadout.Social[7] = new PlrItemSlot(MagicQuiver, 1, 0, false);

        var acc = Service.EquipmentAppearance.ResolveAccessories(loadout);

        Assert.NotNull(acc.BackpackFile);
        Assert.EndsWith("acc_back" + Path.DirectorySeparatorChar + "7.png", acc.BackpackFile);
        Assert.Null(acc.BackFile);
        Assert.Null(acc.TailFile);
    }

    [Fact]
    public void VanidadDeTailTapaAlFuncionalDeBackpack_FielAlGuardado()
    {
        // Funcional: Magic Quiver (Backpack) en el hueco 3. Vanidad: Dog Tail (Tail) en el hueco
        // 8 - el juego real muestra el de VANIDAD, aunque caiga en OTRO canal (Tail en vez de
        // Backpack).
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[3] = new PlrItemSlot(MagicQuiver, 1, 0, false);
        loadout.Social[8] = new PlrItemSlot(DogTail, 1, 0, false);

        var acc = Service.EquipmentAppearance.ResolveAccessories(loadout);

        Assert.NotNull(acc.TailFile);
        Assert.EndsWith("acc_back" + Path.DirectorySeparatorChar + "25.png", acc.TailFile);
        Assert.Null(acc.BackFile);
        Assert.Null(acc.BackpackFile);
    }

    [Fact]
    public void SlotVacio_NoResuelveNingunBackpackNiTail()
    {
        var acc = Service.EquipmentAppearance.ResolveAccessories(PlrLoadout.CreateEmpty(isPrimary: true));

        Assert.Null(acc.BackpackFile);
        Assert.Null(acc.TailFile);
        Assert.Null(acc.BackpackSlot);
        Assert.Null(acc.TailSlot);
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

    // GapAnalysis Encargo B (25-sep-2026): HeadBack no es un item/canal independiente - se
    // DERIVA del headSlot YA resuelto via ArmorIDs.Head.Sets.FrontToBackID (ver
    // ResolveHeadBack/PlayerBodyDrawTables.HeadFrontToBackID para la cita real completa). Ids
    // vanilla reales confirmados contra vanilla_armor_slots.json de este PC (25-sep-2026): Dog
    // Ears (id 4768, headSlot=242), Cat Ears (id 1824, headSlot=133), Bunny Ears (id 4560,
    // headSlot=224) - los 3 con entrada real en FrontToBackID y sprite "de espaldas" ya
    // extraido en Assets/player/armor_head/ (246/252/253.png, ampliado en esta misma pasada por
    // extraer-sprites-armadura-vanilla.js/HEAD_SINTETICOS_FRONT_TO_BACK).
    private const int OrejasDePerro = 4768; // headSlot=242 -> FrontToBackID=246
    private const int OrejasDeGato = 1824;  // headSlot=133 -> FrontToBackID=252
    private const int OrejasDeConejo = 4560; // headSlot=224 -> FrontToBackID=253

    [Theory]
    [InlineData(OrejasDePerro, 246)]
    [InlineData(OrejasDeGato, 252)]
    [InlineData(OrejasDeConejo, 253)]
    public void CascoConEntradaRealEnFrontToBackID_ResuelveElSpriteDeEspaldas(int itemId, int backIdEsperado)
    {
        var armor = Service.EquipmentAppearance.Resolve(LoadoutConCabeza(itemId));

        Assert.NotNull(armor.HeadBackFile);
        Assert.True(File.Exists(armor.HeadBackFile));
        Assert.EndsWith("armor_head" + Path.DirectorySeparatorChar + backIdEsperado + ".png", armor.HeadBackFile);
    }

    [Fact]
    public void CascoSinEntradaEnFrontToBackID_HeadBackEsNull_LaInmensaMayoriaDeCascosReales()
    {
        // El Casco de cobre (headSlot=1) NO tiene sprite "de espaldas" real - confirma que el
        // camino "sin entrada" no revienta ni inventa un fichero, mismo criterio "lo que no se
        // encuentra no se inventa" del resto del resolver.
        var armor = Service.EquipmentAppearance.Resolve(LoadoutConCabeza(CascoCobre));

        Assert.Null(armor.HeadBackFile);
    }

    [Fact]
    public void SlotVacio_HeadBackFileTambienEsNull()
    {
        var armor = Service.EquipmentAppearance.Resolve(PlrLoadout.CreateEmpty(isPrimary: true));

        Assert.Null(armor.HeadBackFile);
    }

    [Fact]
    public void VanidadDeCascoConOrejasTapaAlFuncionalSinEntrada_HeadBackSigueALaVanidad()
    {
        // Funcional: Casco de cobre (sin FrontToBackID) en el slot 0. Vanidad: Orejas de perro
        // (headSlot=242, CON FrontToBackID) en el slot de vanidad - el juego real muestra la
        // vanidad, y HeadBack tiene que derivarse del headSlot VISIBLE (el de vanidad), no del
        // funcional tapado - mismo criterio "vanidad tapa a funcional" ya verificado para
        // HeadFile/BodyFile/LegsFile.
        var armor = Service.EquipmentAppearance.Resolve(LoadoutConCabeza(CascoCobre, OrejasDePerro));

        Assert.NotNull(armor.HeadBackFile);
        Assert.EndsWith("armor_head" + Path.DirectorySeparatorChar + "246.png", armor.HeadBackFile);
    }

    [Fact]
    public void RenderConHeadBackFileDaUnaImagenDistintaASinEl_AislandoSoloEsaCapa()
    {
        // Verificacion de extremo a extremo real y AISLADA (no solo que la ruta se calcule
        // bien): las dos EquippedArmor de aqui son IDENTICAS salvo HeadBackFile - si algun dia
        // PlayerPreviewRenderer.Render deja de leer ese campo por un refactor descuidado, esta
        // prueba lo pilla en seco (a diferencia de comparar dos cascos DISTINTOS, que tambien
        // diferirian por HeadFile aunque HeadBackFile no se dibujara nunca).
        //
        // HeadSlot=1 (Casco de cobre) SIN HeadFile es una combinacion sintetica a proposito
        // (no se da nunca en un .plr real, donde HeadSlot y HeadFile siempre se resuelven
        // juntos): fuerza hideHair=true (1 no esta en FullHairHeadSlots/HatHairHeadSlots, ver
        // HairDrawProfile) sin dibujar ningun casco encima - los 28 pixels reales del sprite
        // "de espaldas" (armor_head/246.png, coordenadas x=24-29/y=10-15, comprobado con Pillow
        // contra este PC) caen en una zona que body0/head.png deja transparente, asi que solo
        // se ven si NINGUNA otra capa opaca los tapa. Los 6 headSlot reales que SI tienen
        // entrada en FrontToBackID (ver CascoConEntradaRealEnFrontToBackID_...) estan TODOS en
        // FullHairHeadSlots (fullHair=true) - el pelo real se dibuja encima igualmente, por eso
        // esta prueba aislada no reutiliza esos ids, solo necesita comprobar que Render lee el
        // campo HeadBackFile de verdad.
        string headBackPath = Path.Combine(AppContext.BaseDirectory, "Assets", "player", "armor_head", "246.png");
        Assert.True(File.Exists(headBackPath));

        var colors = new PlayerPreviewRenderer.PlayerColors(
            new(150, 90, 50), new(255, 220, 177), new(80, 50, 30),
            new(130, 60, 60), new(200, 180, 160), new(70, 70, 120), new(90, 60, 40));

        var sinHeadBack = new PlayerPreviewRenderer.EquippedArmor(HeadFile: null, BodyFile: null, LegsFile: null, HeadSlot: 1);
        var conHeadBack = new PlayerPreviewRenderer.EquippedArmor(HeadFile: null, BodyFile: null, LegsFile: null, HeadSlot: 1, HeadBackFile: headBackPath);

        var renderSinHeadBack = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, colors, sinHeadBack);
        var renderConHeadBack = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, colors, conHeadBack);

        var pixelesSinHeadBack = new byte[renderSinHeadBack.PixelHeight * renderSinHeadBack.PixelWidth * 4];
        renderSinHeadBack.CopyPixels(pixelesSinHeadBack, renderSinHeadBack.PixelWidth * 4, 0);
        var pixelesConHeadBack = new byte[renderConHeadBack.PixelHeight * renderConHeadBack.PixelWidth * 4];
        renderConHeadBack.CopyPixels(pixelesConHeadBack, renderConHeadBack.PixelWidth * 4, 0);

        Assert.NotEqual(pixelesSinHeadBack, pixelesConHeadBack);
    }

    // GapAnalysis Encargo D (25-sep-2026): shoeSlot - el accesorio REAL de zapatos
    // (Player.cs:37193-37200), canal COMPLETAMENTE DISTINTO de los zapatos BASE de la piel/
    // pantalon del personaje. Ids reales confirmados en vanilla_accessory_slots.json contra
    // Item.cs de este PC (25-sep-2026): Hermes Boots (id 54, shoeSlot=6, SIN entrada en
    // MaleToFemaleID) y Glass Slipper (id 5077, shoeSlot=25, UNICA entrada real:
    // MaleToFemaleID[25]=26 -> GlassSlipperFemale).
    private const int HermesBoots = 54;  // shoeSlot=6
    private const int GlassSlipper = 5077; // shoeSlot=25 -> 26 en femenino

    [Fact]
    public void AccesorioDeZapatosVanilla_ResuelveUnSpriteRealQueExisteEnDisco()
    {
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(3, HermesBoots));

        Assert.NotNull(acc.ShoesFile);
        Assert.True(File.Exists(acc.ShoesFile));
        Assert.EndsWith("acc_shoes" + Path.DirectorySeparatorChar + "6.png", acc.ShoesFile);
        Assert.Equal(6, acc.ShoesSlot);
    }

    [Fact]
    public void ResolveAccessories_NuncaAplicaLaRegulaDeSexo_GuardaElIdMasculinoNeutroTalCual()
    {
        // ResolveAccessories no conoce el sexo del personaje (no tiene ese parametro) - la
        // sustitucion MaleToFemaleID se aplica DESPUES, en PlayerPreviewRenderer.Render (ver
        // PlayerBodyDrawTablesTests.ShoeMaleToFemaleID_* para la tabla). Glass Slipper siempre
        // declara shoeSlot=25 en el .plr real, sin importar el sexo del personaje.
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(3, GlassSlipper));

        Assert.NotNull(acc.ShoesFile);
        Assert.EndsWith("acc_shoes" + Path.DirectorySeparatorChar + "25.png", acc.ShoesFile);
        Assert.Equal(25, acc.ShoesSlot);
    }

    [Fact]
    public void SlotVacio_NoResuelveNingunAccesorioDeZapatos()
    {
        var acc = Service.EquipmentAppearance.ResolveAccessories(PlrLoadout.CreateEmpty(isPrimary: true));

        Assert.Null(acc.ShoesFile);
        Assert.Null(acc.ShoesSlot);
    }

    [Fact]
    public void PersonajeFemenino_ConGlassSlipper_RenderSustituyeAlSpriteFemenino25A26_MaleToFemaleID()
    {
        // Verificacion de extremo a extremo AISLADA (mismo criterio que
        // RenderConHeadBackFileDaUnaImagenDistintaASinEl_AislandoSoloEsaCapa): el objeto real
        // SIEMPRE resuelve a acc_shoes/25.png (confirmado arriba) - si Render aplica de verdad
        // la regla de sexo, el resultado FEMENINO con el objeto original (25.png) tiene que ser
        // PIXEL A PIXEL IDENTICO al de pasar directamente el sprite femenino (26.png) a mano, y
        // el MASCULINO con el mismo objeto NO debe coincidir con forzar el sprite femenino (el
        // "!Male" del codigo real tiene que participar de verdad, no solo "cualquier cambio").
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(3, GlassSlipper));
        Assert.Equal(25, acc.ShoesSlot);

        string femaleSpritePath = Path.Combine(AppContext.BaseDirectory, "Assets", "player", "acc_shoes", "26.png");
        Assert.True(File.Exists(femaleSpritePath));
        var accConSpriteFemeninoForzado = acc with { ShoesFile = femaleSpritePath, ShoesSlot = null };

        var colors = new PlayerPreviewRenderer.PlayerColors(
            new(150, 90, 50), new(255, 220, 177), new(80, 50, 30),
            new(130, 60, 60), new(200, 180, 160), new(70, 70, 120), new(90, 60, 40));

        var femConObjetoOriginal = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.FemaleStarter, colors, accessories: acc);
        var femConSpriteFemeninoDirecto = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.FemaleStarter, colors, accessories: accConSpriteFemeninoForzado);
        var masConObjetoOriginal = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, colors, accessories: acc);
        var masConSpriteFemeninoForzado = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, colors, accessories: accConSpriteFemeninoForzado);

        byte[] Pix(System.Windows.Media.Imaging.WriteableBitmap b)
        {
            var p = new byte[b.PixelHeight * b.PixelWidth * 4];
            b.CopyPixels(p, b.PixelWidth * 4, 0);
            return p;
        }

        // Femenino: el objeto original (25.png, sin sustituir a mano) da EXACTAMENTE el mismo
        // resultado que forzar 26.png - la sustitucion real ocurrio dentro de Render.
        Assert.Equal(Pix(femConSpriteFemeninoDirecto), Pix(femConObjetoOriginal));
        // Masculino: el mismo objeto (shoeSlot=25) NO se sustituye - forzar el sprite femenino
        // SI cambia el resultado, confirmando que la condicion real "!Male" participa.
        Assert.NotEqual(Pix(masConSpriteFemeninoForzado), Pix(masConObjetoOriginal));
    }

    // El orden real con wearsRobe=true (Shoes ANTES que Leggings, LegacyPlayerRenderer.cs:
    // 195-204) se verifica en PlayerPreviewRendererAccessoriesTests.Orden_ConWearsRobeReal_
    // PernerasCubrenAZapatos (requiere PNGs sinteticos para aislar el orden con certeza -
    // hallazgo real de esta misma investigacion: con un legSlot REAL tipo robe/vestido
    // -armor_legs/88.png, body=15- el propio sprite de la falda es opaco donde caen los pies,
    // asi que "sigue cambiando el pixel" NO es una aserción valida ahi - lo fiel es que la
    // pernera se compone DESPUES y por tanto LA TAPA, exactamente igual que en el juego real).

    // GapAnalysis Encargo C (25-sep-2026): balloonSlot se clasifica en 2 canales reales posibles
    // (Player.cs:37232-37241, UpdateVisibleAccessory - ver BalloonAccessoryLayerTable, tabla real
    // de ArmorIDs.cs:2252). Ids reales confirmados en vanilla_accessory_slots.json contra Item.cs
    // de este PC (25-sep-2026): Shiny Red Balloon (id 159, balloonSlot=8, SIN entrada en
    // DrawInFrontOfBackArmLayer - canal Balloon normal), Royal Scepter (id 5076, balloonSlot=18,
    // el UNICO balloonSlot real con DrawInFrontOfBackArmLayer=true - canal BalloonFront), los 2
    // con sprite ya extraido en Assets/player/acc_balloon/.
    private const int GloboRojoBrillante = 159; // balloonSlot=8 -> Balloon normal
    private const int CetroReal = 5076;         // balloonSlot=18 -> BalloonFront (unico real)

    [Fact]
    public void BalloonSlotFueraDeLaTablaDeFrente_SigueSiendoBalloonNormal()
    {
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(3, GloboRojoBrillante));

        Assert.NotNull(acc.BalloonFile);
        Assert.True(File.Exists(acc.BalloonFile));
        Assert.EndsWith("acc_balloon" + Path.DirectorySeparatorChar + "8.png", acc.BalloonFile);
        Assert.Equal(8, acc.BalloonSlot);
        Assert.Null(acc.BalloonFrontFile);
        Assert.Null(acc.BalloonFrontSlot);
    }

    [Fact]
    public void BalloonSlotEnLaTablaDeFrente_SeReclasificaComoBalloonFrontNoComoBalloon()
    {
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConAccesorio(3, CetroReal));

        Assert.NotNull(acc.BalloonFrontFile);
        Assert.True(File.Exists(acc.BalloonFrontFile));
        Assert.EndsWith("acc_balloon" + Path.DirectorySeparatorChar + "18.png", acc.BalloonFrontFile);
        Assert.Equal(18, acc.BalloonFrontSlot);
        Assert.Null(acc.BalloonFile);
        Assert.Null(acc.BalloonSlot);
    }

    [Fact]
    public void SlotVacio_NoResuelveNingunGlobo()
    {
        var acc = Service.EquipmentAppearance.ResolveAccessories(PlrLoadout.CreateEmpty(isPrimary: true));

        Assert.Null(acc.BalloonFile);
        Assert.Null(acc.BalloonFrontFile);
        Assert.Null(acc.BalloonSlot);
        Assert.Null(acc.BalloonFrontSlot);
    }

    [Fact]
    public void VanidadDeBalloonFrontTapaAlFuncionalDeBalloonNormal_FielAlGuardado()
    {
        // Funcional: Shiny Red Balloon (Balloon normal) en el hueco 3. Vanidad: Royal Scepter
        // (BalloonFront, vanity=true real - Item.cs:45307) en el hueco 7 - el juego real muestra
        // la vanidad, aunque caiga en OTRO canal (BalloonFront en vez de Balloon), mismo criterio
        // "vanidad tapa a funcional" ya verificado para Back/Backpack/Tail (Encargo A).
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[3] = new PlrItemSlot(GloboRojoBrillante, 1, 0, false);
        loadout.Social[7] = new PlrItemSlot(CetroReal, 1, 0, false);

        var acc = Service.EquipmentAppearance.ResolveAccessories(loadout);

        Assert.NotNull(acc.BalloonFrontFile);
        Assert.EndsWith("acc_balloon" + Path.DirectorySeparatorChar + "18.png", acc.BalloonFrontFile);
        Assert.Null(acc.BalloonFile);
    }

    [Fact]
    public void RenderConBalloonFileDaUnaImagenDistintaASinEl_AislandoSoloEsaCapa()
    {
        // Verificacion de extremo a extremo AISLADA (mismo criterio que
        // RenderConHeadBackFileDaUnaImagenDistintaASinEl_AislandoSoloEsaCapa): dos
        // EquippedAccessories IDENTICOS salvo BalloonFile - si algun dia Render deja de leer ese
        // campo por un refactor descuidado, esta prueba lo pilla en seco.
        string balloonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "player", "acc_balloon", "8.png");
        Assert.True(File.Exists(balloonPath));

        var colors = new PlayerPreviewRenderer.PlayerColors(
            new(150, 90, 50), new(255, 220, 177), new(80, 50, 30),
            new(130, 60, 60), new(200, 180, 160), new(70, 70, 120), new(90, 60, 40));

        var sinAcc = new EquippedAccessories(null, null, null, null, null, null, null);
        var conBalloon = sinAcc with { BalloonFile = balloonPath };

        var renderSinBalloon = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, colors, accessories: sinAcc);
        var renderConBalloon = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, colors, accessories: conBalloon);

        var pixelesSin = new byte[renderSinBalloon.PixelHeight * renderSinBalloon.PixelWidth * 4];
        renderSinBalloon.CopyPixels(pixelesSin, renderSinBalloon.PixelWidth * 4, 0);
        var pixelesCon = new byte[renderConBalloon.PixelHeight * renderConBalloon.PixelWidth * 4];
        renderConBalloon.CopyPixels(pixelesCon, renderConBalloon.PixelWidth * 4, 0);

        Assert.NotEqual(pixelesSin, pixelesCon);
    }

    [Fact]
    public void RenderConBalloonFrontFileDaUnaImagenDistintaASinEl_AislandoSoloEsaCapa()
    {
        // Gemela real de la prueba de arriba, pero para el canal BalloonFront (AccBalloon_18,
        // RoyalScepter - el UNICO real, tira 40x1120 "alineada al lienzo", ver el comentario real
        // de PlayerPreviewRenderer.Render/Paso 4). Sin BodyFile (rama "sin cuerpo" de Paso 4, mismo
        // criterio que ConWearsRobeReal_LaCapaDeZapatosSigueDibujandoseSinRevantar de arriba).
        string balloonFrontPath = Path.Combine(AppContext.BaseDirectory, "Assets", "player", "acc_balloon", "18.png");
        Assert.True(File.Exists(balloonFrontPath));

        var colors = new PlayerPreviewRenderer.PlayerColors(
            new(150, 90, 50), new(255, 220, 177), new(80, 50, 30),
            new(130, 60, 60), new(200, 180, 160), new(70, 70, 120), new(90, 60, 40));

        var sinAcc = new EquippedAccessories(null, null, null, null, null, null, null);
        var conBalloonFront = sinAcc with { BalloonFrontFile = balloonFrontPath };

        var renderSin = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, colors, accessories: sinAcc);
        var renderCon = PlayerPreviewRenderer.Render(1, skinVariant: PlayerVariantSets.MaleStarter, colors, accessories: conBalloonFront);

        var pixelesSin = new byte[renderSin.PixelHeight * renderSin.PixelWidth * 4];
        renderSin.CopyPixels(pixelesSin, renderSin.PixelWidth * 4, 0);
        var pixelesCon = new byte[renderCon.PixelHeight * renderCon.PixelWidth * 4];
        renderCon.CopyPixels(pixelesCon, renderCon.PixelWidth * 4, 0);

        Assert.NotEqual(pixelesSin, pixelesCon);
    }

    // CalamityAccesorios (25-sep-2026, gap analysis paridad UICharacter): los 9 canales de
    // accesorio real de Calamity (Waist/Neck/HandsOn/HandsOff/Back/Shield/Face/Balloon/Shoes) -
    // IsAccessoryType/ResolveAccessorySprite ya eran genericos por tipo, lo que faltaba era
    // CalamityCatalogEntry.EquipSlot poblado para estos 9 tipos (ver scripts/extraer-slot-
    // armadura-calamity.js). Un test por canal, pidiendo el item real al catalogo
    // dinamicamente (nunca un id hardcodeado sin verificar que existe) - mismo criterio ya
    // establecido en ObjetoCalamityRealConEquipSlot_ResuelveElSpriteRealYaExtraidoDelTmod.
    private static PlrLoadout LoadoutConCalamityEnAccesorio(int syntheticId)
    {
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[3] = new PlrItemSlot(syntheticId, 1, 0, false);
        return loadout;
    }

    [Fact]
    public void CalamityWaist_ResuelveElIconoRealYaExtraidoDelTmod()
    {
        var entry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "Waist");
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConCalamityEnAccesorio(entry.SyntheticId));

        Assert.NotNull(acc.WaistFile);
        Assert.True(File.Exists(acc.WaistFile));
        Assert.EndsWith(entry.Internal + "_Waist.png", acc.WaistFile);
        Assert.Null(acc.WaistSlot); // numeracion propia de Calamity, no compartida con vanilla
    }

    [Fact]
    public void CalamityNeck_ResuelveElIconoRealYaExtraidoDelTmod()
    {
        var entry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "Neck");
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConCalamityEnAccesorio(entry.SyntheticId));

        Assert.NotNull(acc.NeckFile);
        Assert.True(File.Exists(acc.NeckFile));
        Assert.EndsWith(entry.Internal + "_Neck.png", acc.NeckFile);
    }

    [Fact]
    public void CalamityBack_ResuelveElIconoRealYaExtraidoDelTmod()
    {
        var entry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "Back");
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConCalamityEnAccesorio(entry.SyntheticId));

        Assert.NotNull(acc.BackFile);
        Assert.True(File.Exists(acc.BackFile));
        Assert.EndsWith(entry.Internal + "_Back.png", acc.BackFile);
        // backSlotId es null para Calamity (numeracion propia) - BackAccessoryLayerTable nunca
        // reclasifica a Backpack/Tail para estos objetos, se quedan en el canal Back normal.
        Assert.Null(acc.BackpackFile);
        Assert.Null(acc.TailFile);
    }

    [Fact]
    public void CalamityShield_ResuelveElIconoRealYaExtraidoDelTmod_IncluidoStygianShieldFueraDeArmor()
    {
        // StygianShield es el spot-check real del arreglo del filtro de categoria (categoria
        // real "Weapons/Melee", no "Armor/..." ni "Accessories" - el atributo AutoloadEquip real
        // es la unica fuente de verdad, ver la cabecera del script).
        var entry = Service.CalamityCatalog.Entries.First(e => e.Internal == "StygianShield");
        Assert.Equal("Shield", entry.EquipSlot);
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConCalamityEnAccesorio(entry.SyntheticId));

        Assert.NotNull(acc.ShieldFile);
        Assert.True(File.Exists(acc.ShieldFile));
        Assert.EndsWith("StygianShield_Shield.png", acc.ShieldFile);
    }

    [Fact]
    public void CalamityFace_ResuelveElIconoRealYaExtraidoDelTmod()
    {
        var entry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "Face");
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConCalamityEnAccesorio(entry.SyntheticId));

        Assert.NotNull(acc.FaceFile);
        Assert.True(File.Exists(acc.FaceFile));
        Assert.EndsWith(entry.Internal + "_Face.png", acc.FaceFile);
    }

    [Fact]
    public void CalamityBalloon_ResuelveElIconoRealYaExtraidoDelTmod_SiempreCanalNormalNuncaFront()
    {
        var entry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "Balloon");
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConCalamityEnAccesorio(entry.SyntheticId));

        Assert.NotNull(acc.BalloonFile);
        Assert.True(File.Exists(acc.BalloonFile));
        Assert.EndsWith(entry.Internal + "_Balloon.png", acc.BalloonFile);
        // balloonSlotId es null para Calamity - BalloonAccessoryLayerTable.IsFrontLayer nunca se
        // consulta, fiel-por-defecto al canal Balloon normal (ver el comentario real de
        // EquipmentAppearanceResolver.ResolveAccessories).
        Assert.Null(acc.BalloonFrontFile);
    }

    [Fact]
    public void CalamityShoes_ResuelveElIconoRealYaExtraidoDelTmod_SufijoRealPluralNoSingular()
    {
        // Sufijo real de fichero = nombre CRUDO del enum EquipType.Shoes ("_Shoes.png"), no el
        // alias corto "Shoe" que usan los campos/directorios vanilla (e.Shoe, acc_shoes/) - ver
        // la cabecera real de scripts/extraer-slot-armadura-calamity.js.
        var entry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "Shoes");
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConCalamityEnAccesorio(entry.SyntheticId));

        Assert.NotNull(acc.ShoesFile);
        Assert.True(File.Exists(acc.ShoesFile));
        Assert.EndsWith(entry.Internal + "_Shoes.png", acc.ShoesFile);
        Assert.Null(acc.ShoesSlot); // numeracion propia de Calamity - MaleToFemaleID nunca se aplica
    }

    [Fact]
    public void CalamityHandsOnYHandsOff_UnMismoGuanteResuelveLosDosCanalesALaVez()
    {
        // 6 items reales (guantes) declaran EquipType.HandsOn Y EquipType.HandsOff a la vez -
        // decision de diseno: CalamityCatalogEntry.EquipSlotSecondary (ver su comentario real en
        // CalamityCatalog.cs). UN UNICO item en UN UNICO hueco (indice 3) debe resolver AMBOS
        // canales, cada uno con su propio sprite real (BloodstainedGlove_HandsOn.png Y
        // BloodstainedGlove_HandsOff.png son ficheros REALES distintos, ya extraidos).
        var entry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "HandsOn" && e.EquipSlotSecondary == "HandsOff");
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConCalamityEnAccesorio(entry.SyntheticId));

        Assert.NotNull(acc.HandOnFile);
        Assert.True(File.Exists(acc.HandOnFile));
        Assert.EndsWith(entry.Internal + "_HandsOn.png", acc.HandOnFile);

        Assert.NotNull(acc.HandOffFile);
        Assert.True(File.Exists(acc.HandOffFile));
        Assert.EndsWith(entry.Internal + "_HandsOff.png", acc.HandOffFile);
    }

    [Fact]
    public void CalamityDeUnSoloTipo_NoResuelveNingunCanalDeGuanteQueNoDeclara()
    {
        // Defensa real contra un falso positivo del EquipSlotSecondary nuevo: los 6 items reales
        // de guante son el UNICO caso real con EquipSlotSecondary relleno (confirmado con el
        // volcado real del script) - cualquier otro objeto de un solo tipo (EquipSlotSecondary
        // null) no debe colarse en HandOn/HandOff solo porque el operador "||" del IsAccessoryType
        // nuevo compara tambien el campo secundario.
        var entry = Service.CalamityCatalog.Entries.First(e => e.EquipSlot == "Waist");
        Assert.Null(entry.EquipSlotSecondary);
        var acc = Service.EquipmentAppearance.ResolveAccessories(LoadoutConCalamityEnAccesorio(entry.SyntheticId));

        Assert.NotNull(acc.WaistFile);
        Assert.Null(acc.HandOnFile);
        Assert.Null(acc.HandOffFile);
    }
}
