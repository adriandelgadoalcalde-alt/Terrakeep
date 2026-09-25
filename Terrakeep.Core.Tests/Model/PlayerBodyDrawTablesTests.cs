using Terrakeep.Core.Model;
using Xunit;

namespace Terrakeep.Core.Tests.Model;

// H6-01-b (advisor Opus, "la vanidad no se dibuja bien en el cuerpo delgado" - caso real
// "Eldelgas", bodySlot 93/"Vestido de la Muerte"). Cada valor de aqui se releyo del decompilado
// real (Terraria.Player.SetMatch, Player.cs:37458-37694; PlayerDrawSet.cs:373/378-385/
// 1795-1796; PlayerDrawLayers.cs:1850-1926/1840-1848) al escribir esta bateria, no copiado del
// espec de memoria - ver PlayerBodyDrawTables.cs para la cita exacta de cada tabla.
public class PlayerBodyDrawTablesTests
{
    // ---- SetMatchBodyToLegs (Player.cs:37474-37584, ArmorSlotRequested==1) ----

    [Fact]
    public void SetMatchBodyToLegs_Body93_ElCasoRealEldelgas_FuerzaLegs165YMarcaWearsRobe()
    {
        var r = PlayerBodyDrawTables.SetMatchBodyToLegs(93, male: true, currentLegs: 0);
        Assert.Equal(new PlayerBodyDrawTables.BodyToLegsMatch(165, true), r);
    }

    [Fact]
    public void SetMatchBodyToLegs_Body166_NoMarcaWearsRobe_FlagEsFalseEnElCodigoReal()
    {
        var male = PlayerBodyDrawTables.SetMatchBodyToLegs(166, male: true, currentLegs: 0);
        var female = PlayerBodyDrawTables.SetMatchBodyToLegs(166, male: false, currentLegs: 0);
        Assert.Equal(new PlayerBodyDrawTables.BodyToLegsMatch(119, false), male);
        Assert.Equal(new PlayerBodyDrawTables.BodyToLegsMatch(100, false), female);
    }

    [Theory]
    [InlineData(165, true, 118)]
    [InlineData(165, false, 99)]
    [InlineData(167, true, 101)]
    [InlineData(167, false, 102)]
    [InlineData(183, true, 136)]
    [InlineData(183, false, 123)]
    public void SetMatchBodyToLegs_CasosQueDependenDelGenero(int body, bool male, int legsEsperado)
    {
        var r = PlayerBodyDrawTables.SetMatchBodyToLegs(body, male, currentLegs: 0);
        Assert.Equal(legsEsperado, r!.Value.Legs);
        Assert.True(r.Value.SetsWearsRobe);
    }

    [Theory]
    [InlineData(15, 88)]
    [InlineData(36, 89)]
    [InlineData(41, 97)]
    [InlineData(60, 93)]
    [InlineData(90, 166)]
    [InlineData(88, 168)]
    [InlineData(256, 244)]
    public void SetMatchBodyToLegs_CasosIndependientesDelGenero(int body, int legsEsperado)
    {
        Assert.Equal(legsEsperado, PlayerBodyDrawTables.SetMatchBodyToLegs(body, male: true, currentLegs: 0)!.Value.Legs);
        Assert.Equal(legsEsperado, PlayerBodyDrawTables.SetMatchBodyToLegs(body, male: false, currentLegs: 0)!.Value.Legs);
    }

    [Fact]
    public void SetMatchBodyToLegs_Body81_SoloActuaSiLosLegsActualesEstanVacios()
    {
        // Player.cs: "if (request.Legs == -1 || request.Legs == 0) { num2 = 169; }"
        Assert.Equal(169, PlayerBodyDrawTables.SetMatchBodyToLegs(81, male: true, currentLegs: 0)!.Value.Legs);
        Assert.Equal(169, PlayerBodyDrawTables.SetMatchBodyToLegs(81, male: true, currentLegs: -1)!.Value.Legs);
        Assert.Null(PlayerBodyDrawTables.SetMatchBodyToLegs(81, male: true, currentLegs: 5));
    }

    [Fact]
    public void SetMatchBodyToLegs_BodySinEntradaReal_DevuelveNull()
    {
        Assert.Null(PlayerBodyDrawTables.SetMatchBodyToLegs(1, male: true, currentLegs: 0));
        Assert.Null(PlayerBodyDrawTables.SetMatchBodyToLegs(0, male: true, currentLegs: 0));
    }

    // ---- SetMatchLegsToLegs (Player.cs:37585-37692, ArmorSlotRequested==2) ----

    [Theory]
    [InlineData(83, true, 117)]
    [InlineData(83, false, null)]
    [InlineData(203, false, 202)]
    [InlineData(203, true, null)]
    [InlineData(146, true, 146)]
    [InlineData(146, false, 147)]
    [InlineData(154, true, 155)]
    [InlineData(154, false, 154)]
    public void SetMatchLegsToLegs_CasosReales(int legs, bool male, int? esperado)
    {
        Assert.Equal(esperado, PlayerBodyDrawTables.SetMatchLegsToLegs(legs, male));
    }

    // ---- SetMatchHead (Player.cs:37470-37473, ArmorSlotRequested==0) ----

    [Fact]
    public void SetMatchHead_201_SustituyePor202EnFemenino()
    {
        Assert.Equal(201, PlayerBodyDrawTables.SetMatchHead(201, male: true));
        Assert.Equal(202, PlayerBodyDrawTables.SetMatchHead(201, male: false));
        Assert.Null(PlayerBodyDrawTables.SetMatchHead(1, male: true));
    }

    // ---- hidesTopSkin/hidesBottomSkin (PlayerDrawSet.cs:1795-1796) ----

    [Theory]
    [InlineData(82, true)]
    [InlineData(83, true)]
    [InlineData(93, true)] // caso real Eldelgas
    [InlineData(21, true)]
    [InlineData(22, true)]
    [InlineData(1, false)]
    [InlineData(0, false)]
    public void HidesTopSkin_CoincideConElCodigoReal(int body, bool esperado)
    {
        Assert.Equal(esperado, PlayerBodyDrawTables.HidesTopSkin(body));
    }

    [Fact]
    public void HidesBottomSkin_Body93_OcultaSiempreAunqueLosLegsNoEsten()
    {
        Assert.True(PlayerBodyDrawTables.HidesBottomSkin(93, legs: 0));
    }

    [Theory]
    [InlineData(20, true)]
    [InlineData(21, true)]
    [InlineData(216, true)]
    [InlineData(214, true)]
    [InlineData(215, true)]
    [InlineData(1, false)]
    public void HidesBottomSkin_PorLegSlot(int legs, bool esperado)
    {
        Assert.Equal(esperado, PlayerBodyDrawTables.HidesBottomSkin(body: 0, legs));
    }

    // ---- missingArm/missingHand (PlayerDrawSet.cs:373/378-385) ----

    [Fact]
    public void MissingArm_SoloElBody83EsFalse()
    {
        Assert.False(PlayerBodyDrawTables.MissingArm(83));
        Assert.True(PlayerBodyDrawTables.MissingArm(0));
        Assert.True(PlayerBodyDrawTables.MissingArm(1));
        Assert.True(PlayerBodyDrawTables.MissingArm(93));
    }

    [Theory]
    [InlineData(15, true)]
    [InlineData(213, true)] // el ultimo id real de la cadena de || en PlayerDrawSet.cs:373
    [InlineData(93, false)] // 93 (Eldelgas) NO esta en la lista real de missingHand
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void MissingHand_CoincideConLaListaRealDePlayerDrawSet(int body, bool esperado)
    {
        Assert.Equal(esperado, PlayerBodyDrawTables.MissingHand(body));
    }

    // ---- GetMatchingBodyExtension (PlayerDrawLayers.cs:1850-1926) ----

    [Theory]
    [InlineData(200, 149)]
    [InlineData(81, 169)]
    [InlineData(251, 238)]
    public void GetMatchingBodyExtension_CasosIndependientesDelGenero(int body, int esperado)
    {
        Assert.Equal(esperado, PlayerBodyDrawTables.GetMatchingBodyExtension(body, male: true));
        Assert.Equal(esperado, PlayerBodyDrawTables.GetMatchingBodyExtension(body, male: false));
    }

    [Theory]
    [InlineData(52, true, 171)]
    [InlineData(52, false, 172)]
    [InlineData(222, true, 201)]
    [InlineData(222, false, 200)]
    public void GetMatchingBodyExtension_CasosQueDependenDelGenero(int body, bool male, int esperado)
    {
        Assert.Equal(esperado, PlayerBodyDrawTables.GetMatchingBodyExtension(body, male));
    }

    [Fact]
    public void GetMatchingBodyExtension_Body93_NoTieneExtensionReal()
    {
        // El faldon de la vanidad "Vestido de la Muerte" (93) viene de SetMatch(legs=165), NO
        // de GetMatchingBodyExtension - 93 no aparece en su tabla real.
        Assert.Null(PlayerBodyDrawTables.GetMatchingBodyExtension(93, male: true));
    }

    [Fact]
    public void GetMatchingBodyExtensionBack_SoloBody251()
    {
        Assert.Equal(239, PlayerBodyDrawTables.GetMatchingBodyExtensionBack(251));
        Assert.Null(PlayerBodyDrawTables.GetMatchingBodyExtensionBack(200));
    }

    // ---- HeadFrontToBackID (ArmorIDs.cs:14 - GapAnalysis Encargo B, 25-sep-2026) ----

    [Theory]
    [InlineData(242, 246)] // DogEars -> DogEarsBack
    [InlineData(243, 247)] // FoxEars -> FoxEarsBack
    [InlineData(244, 248)] // LizardEars -> LizardEarsBack
    [InlineData(245, 249)] // PandaEars -> PandaEarsBack
    [InlineData(133, 252)] // CatEars -> CatEarsBack
    [InlineData(224, 253)] // BunnyEars -> BunnyEarsBack
    public void HeadFrontToBackID_Los6CascosRealesConEntrada(int head, int esperado)
    {
        Assert.Equal(esperado, PlayerBodyDrawTables.HeadFrontToBackID(head));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)] // Casco de cobre, headSlot real usado en EquipmentAppearanceResolverTests
    [InlineData(89)]
    [InlineData(201)] // el unico headSlot con entrada real en SetMatchHead - tabla distinta, sin FrontToBackID
    public void HeadFrontToBackID_CascoSinEntradaReal_DevuelveNull(int head)
    {
        Assert.Null(PlayerBodyDrawTables.HeadFrontToBackID(head));
    }

    // ---- ShoeMaleToFemaleID (ArmorIDs.cs:1869 - GapAnalysis Encargo D, 25-sep-2026) ----
    // "Factory.CreateIntSet(-1, 25, 26)" - SetFactory.CreateIntSet(defaultState, pares...):
    // UN UNICO par real, shoeSlot 25 (GlassSlipperMale) -> 26 (GlassSlipperFemale).

    [Fact]
    public void ShoeMaleToFemaleID_GlassSlipper_UnicaEntradaRealDeLaTabla()
    {
        Assert.Equal(26, PlayerBodyDrawTables.ShoeMaleToFemaleID(25));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]  // Hermes Boots, id real 54 - sin entrada en MaleToFemaleID
    [InlineData(24)] // Terraspark Boots, sin entrada
    [InlineData(26)] // la propia variante femenina no tiene entrada (evita un bucle infinito real)
    [InlineData(30)] // RollerSkatesPink, ultimo shoeSlot real (Count=31), sin entrada
    public void ShoeMaleToFemaleID_RestoDeCalzado_DevuelveNull_LaInmensaMayoriaDeZapatosReales(int shoe)
    {
        Assert.Null(PlayerBodyDrawTables.ShoeMaleToFemaleID(shoe));
    }

    // ---- DrawFaceMaskUnderHeadLayer / PreventFaceFlowerDraw / PreventFaceMaskDraw
    // (GapAnalysis Encargo F, 25-sep-2026) - ArmorIDs.Head.Sets, ArmorIDs.cs:20/22/24, indexadas
    // por HEADSLOT (no por faceSlot - ver FaceAccessoryLayerTableTests para las tablas indexadas
    // por faceSlot). Los valores probados son ids LITERALES de esas 3 listas reales (no se
    // identifica el nombre de la pieza vanilla de cada headSlot - basta con el valor numerico
    // real transcrito de ArmorIDs.cs para verificar la clasificacion).

    [Theory]
    [InlineData(26)]
    [InlineData(92)]
    [InlineData(203)] // ultimo id real de la lista (ArmorIDs.cs:20)
    public void DrawFaceMaskUnderHeadLayer_IdsRealesDeLaTabla(int head)
    {
        Assert.True(PlayerBodyDrawTables.DrawFaceMaskUnderHeadLayer(head));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)] // Casco de cobre, sin entrada real
    [InlineData(89)]
    public void DrawFaceMaskUnderHeadLayer_CascoSinEntradaReal_DevuelveFalse(int head)
    {
        Assert.False(PlayerBodyDrawTables.DrawFaceMaskUnderHeadLayer(head));
    }

    [Theory]
    [InlineData(92)]
    [InlineData(275)]
    public void PreventFaceFlowerDraw_LosDosIdsRealesDeLaTabla(int head)
    {
        Assert.True(PlayerBodyDrawTables.PreventFaceFlowerDraw(head));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(26)] // esta EN DrawFaceMaskUnderHeadLayer pero NO en PreventFaceFlowerDraw - tablas independientes
    public void PreventFaceFlowerDraw_CascoSinEntradaReal_DevuelveFalse(int head)
    {
        Assert.False(PlayerBodyDrawTables.PreventFaceFlowerDraw(head));
    }

    [Theory]
    [InlineData(27)]  // primer id real de la lista (ArmorIDs.cs:24)
    [InlineData(22)]  // penultimo id real de la lista
    [InlineData(196)] // ultimo id real de la lista
    public void PreventFaceMaskDraw_IdsRealesDeLaTabla(int head)
    {
        Assert.True(PlayerBodyDrawTables.PreventFaceMaskDraw(head));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(26)] // esta EN DrawFaceMaskUnderHeadLayer pero NO en PreventFaceMaskDraw - tablas independientes
    public void PreventFaceMaskDraw_CascoSinEntradaReal_DevuelveFalse(int head)
    {
        Assert.False(PlayerBodyDrawTables.PreventFaceMaskDraw(head));
    }

    // ---- PreventBeardDraw / BeardUsesHairColor (GapAnalysis Encargo G, 25-sep-2026) -
    // ArmorIDs.Head.Sets.PreventBeardDraw (ArmorIDs.cs:26, indexada por HEADSLOT, 47 ids reales)
    // y ArmorIDs.Beard.Sets.UseHairColor (ArmorIDs.cs:2321, indexada por BEARD id, 1-4).

    [Theory]
    [InlineData(118)] // primer id real de la lista (ArmorIDs.cs:26)
    [InlineData(276)] // ultimo id real de la lista
    [InlineData(22)]
    public void PreventBeardDraw_IdsRealesDeLaTabla(int head)
    {
        Assert.True(PlayerBodyDrawTables.PreventBeardDraw(head));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)] // Casco de cobre, sin entrada real
    [InlineData(89)]
    [InlineData(26)] // esta EN DrawFaceMaskUnderHeadLayer pero NO en PreventBeardDraw - tablas independientes
    public void PreventBeardDraw_CascoSinEntradaReal_DevuelveFalse(int head)
    {
        Assert.False(PlayerBodyDrawTables.PreventBeardDraw(head));
    }

    [Theory]
    [InlineData(2)] // WilsonBeardShort
    [InlineData(3)] // WilsonBeardLong
    [InlineData(4)] // WilsonBeardMagnificent
    public void BeardUsesHairColor_Los3WilsonBeardsRealesQueUsanColorDePelo(int beard)
    {
        Assert.True(PlayerBodyDrawTables.BeardUsesHairColor(beard));
    }

    [Theory]
    [InlineData(0)] // sin barba
    [InlineData(1)] // GingerBeard - NO usa color de pelo, colorArmorHead (textura ya naranja)
    public void BeardUsesHairColor_GingerBeardYSinBarba_DevuelveFalse(int beard)
    {
        Assert.False(PlayerBodyDrawTables.BeardUsesHairColor(beard));
    }

    // ---- FrontDontDrawIfWearingScarfOrCape/NeckIsAScarf/BackIsACape (ArmorIDs.cs:1819/2140/1721
    // - GapAnalysis Encargo E, 25-sep-2026) ----
    // "Factory.CreateBoolSet(false, 13)": UN UNICO frontId real con la condicion activa
    // (DeadCellsBeheadedBody) - NINGUN accesorio real de item.frontSlot lo alcanza nunca (los 11
    // accesorios reales van de 1 a 17 salvo 13 - ver el comentario real de la tabla), asi que
    // esto se prueba con el valor SINTETICO 13 (el propio indice real de la tabla, nunca
    // alcanzado por un accesorio real) - deja constancia explicita de esa distincion.

    [Fact]
    public void FrontDontDrawIfWearingScarfOrCape_13_DeadCellsBeheadedBody_UnicaEntradaRealDeLaTabla_NoAlcanzableViaAccesorioReal()
    {
        Assert.True(PlayerBodyDrawTables.FrontDontDrawIfWearingScarfOrCape(13));
    }

    [Theory]
    [InlineData(1)]  // CrimsonCloak, accesorio real (id 2284)
    [InlineData(5)]  // ManaCloak, accesorio real (id 4001)
    [InlineData(8)]  // HunterCloak, accesorio real (id 4744)
    [InlineData(11)] // PrinceCape, accesorio real (id 5080)
    [InlineData(12)] // ShimmerCloak, accesorio real (id 5355)
    [InlineData(15)] // ChippysWings, accesorio real (id 5627)
    [InlineData(16)] // LunasCloak, accesorio real (id 6141)
    [InlineData(17)] // DruidicSerpentCloak, accesorio real (id 6186)
    [InlineData(0)]
    public void FrontDontDrawIfWearingScarfOrCape_LosOnceAccesoriosRealesDeFrontSonSiempreFalse(int front)
    {
        Assert.False(PlayerBodyDrawTables.FrontDontDrawIfWearingScarfOrCape(front));
    }

    [Theory]
    [InlineData(8)] // WormScarf
    [InlineData(9)] // ApprenticeScarf
    public void NeckIsAScarf_LosDosScarfRealesDeLaTabla(int neck)
    {
        Assert.True(PlayerBodyDrawTables.NeckIsAScarf(neck));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]  // JellyfishNecklace, no es scarf
    [InlineData(11)] // Magiluminescence, no es scarf
    public void NeckIsAScarf_RestoDeAccesoriosDeCuello_DevuelveFalse(int neck)
    {
        Assert.False(PlayerBodyDrawTables.NeckIsAScarf(neck));
    }

    [Theory]
    [InlineData(1)]  // BeeCloak
    [InlineData(3)]  // CrimsonCloak (back)
    [InlineData(14)]
    [InlineData(41)]
    public void BackIsACape_MuestraRealDeLaTablaDeDoceIndices(int back)
    {
        Assert.True(PlayerBodyDrawTables.BackIsACape(back));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]  // Hero, no es cape (backpack/tail real)
    [InlineData(18)] // DrawInTailLayer real, no es cape
    public void BackIsACape_RestoDeAccesoriosDeEspalda_DevuelveFalse(int back)
    {
        Assert.False(PlayerBodyDrawTables.BackIsACape(back));
    }
}
