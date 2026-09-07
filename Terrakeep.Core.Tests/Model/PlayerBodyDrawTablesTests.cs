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
}
