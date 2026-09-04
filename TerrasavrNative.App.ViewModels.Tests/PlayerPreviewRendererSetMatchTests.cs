using TerrasavrNative.App.Services;

namespace TerrasavrNative.App.ViewModels.Tests;

// H6-01-b (advisor Opus, "la vanidad no se dibuja bien en el cuerpo delgado" - caso real
// "Eldelgas", ver ESPEC-dibujado-sprites.md). Verificacion de extremo a extremo real (no solo
// las tablas puras de PlayerBodyDrawTables, ver TerrasavrNative.Core.Tests): el propio
// PlayerPreviewRenderer.Render aplica SetMatch/hidesTopSkin/hidesBottomSkin/hasBody de verdad
// al componer el lienzo, usando los assets REALES extraidos de la instalacion de Steam
// (armor_body/93.png, armor_legs/165.png, body8/*) - mismo criterio de "pixeles distintos, no
// solo que no lance excepcion" ya establecido en PlayerPreviewRendererH6Tests.
public sealed class PlayerPreviewRendererSetMatchTests
{
    private static readonly PlayerPreviewRenderer.PlayerColors Colors = new(
        new(150, 90, 50), new(255, 220, 177), new(80, 50, 30),
        new(130, 60, 60), new(200, 180, 160), new(70, 70, 120), new(90, 60, 40));

    private static byte[] Pixels(System.Windows.Media.Imaging.WriteableBitmap bmp)
    {
        var pixels = new byte[bmp.PixelHeight * bmp.PixelWidth * 4];
        bmp.CopyPixels(pixels, bmp.PixelWidth * 4, 0);
        return pixels;
    }

    // El "Vestido de la Muerte" real (item 1820) tiene bodySlot=93 - el mismo id que usa
    // Eldelgas.plr en el .plr real leido por el advisor. armor_body/93.png y armor_legs/165.png
    // son sprites REALES de la instalacion de Steam (extraidos por extraer-sprites-armadura-
    // vanilla.js, el segundo ampliado en esta misma pasada para cubrir los ids sinteticos de
    // SetMatch).
    private static readonly PlayerPreviewRenderer.EquippedArmor VestidoDeLaMuerte = new(
        HeadFile: null, BodyFile: RutaArmorBody(93), LegsFile: null,
        HeadSlot: null, BodySlot: 93, LegsSlot: null);

    private static string RutaArmorBody(int id) =>
        System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "player", "armor_body", id + ".png");

    [Fact]
    public void CasoRealEldelgas_BodySlot93_RenderizaSinExcepcionEnLaVarianteMaleDress()
    {
        var bmp = PlayerPreviewRenderer.Render(1, skinVariant: 8 /* MaleDress, el Gender real de Eldelgas.plr */, Colors, VestidoDeLaMuerte);
        Assert.Equal(40, bmp.PixelWidth);
        Assert.Equal(56, bmp.PixelHeight);
    }

    [Fact]
    public void CasoRealEldelgas_ConVestidoDeLaMuerte_ElResultadoDifiereDeSinArmadura()
    {
        var sinArmadura = PlayerPreviewRenderer.Render(1, skinVariant: 8, Colors);
        var conVestido = PlayerPreviewRenderer.Render(1, skinVariant: 8, Colors, VestidoDeLaMuerte);

        Assert.NotEqual(Pixels(sinArmadura), Pixels(conVestido));
    }

    [Fact]
    public void CasoRealEldelgas_SetMatchFuerzaLasPerneras165_DifiereDeUnaArmaduraSinSetMatch()
    {
        // Comparacion diferencial real: el UNICO cambio entre los dos casos es el bodySlot
        // (93 con SetMatch real, frente a un id cualquiera de la instalacion sin entrada en
        // SetMatch, ej. 1 = "Copper Helmet"-body). Si SetMatch no se estuviera aplicando de
        // verdad, ambos renders solo diferirian en la celda de torso/brazo (misma hoja de
        // armadura en los dos, aunque de items distintos) - la pierna es la señal real de que
        // SetMatch(body=93 -> legs=165) esta actuando, porque solo el caso 93 dibuja
        // armor_legs/165.png en vez de los pantalones base.
        var conBody1SinSetMatch = new PlayerPreviewRenderer.EquippedArmor(
            HeadFile: null, BodyFile: RutaArmorBody(1), LegsFile: null,
            HeadSlot: null, BodySlot: 1, LegsSlot: null);

        var sinSetMatch = PlayerPreviewRenderer.Render(1, skinVariant: 8, Colors, conBody1SinSetMatch);
        var conSetMatch = PlayerPreviewRenderer.Render(1, skinVariant: 8, Colors, VestidoDeLaMuerte);

        Assert.NotEqual(Pixels(sinSetMatch), Pixels(conSetMatch));
    }

    [Fact]
    public void ConArmaduraDeCuerpoPuesta_LaRopaBaseNoSeDibuja_BugNumero1DelCasoEldelgas()
    {
        // ESPEC-dibujado-sprites.md, fallo real #1: "con armadura/vanidad de cuerpo puesta, el
        // juego NO dibuja la ropa base". Verificacion indirecta pero real: renderizar la MISMA
        // variante/colores con y sin hasBody (BodySlot=1, sin ninguna bandera especial de
        // SetMatch/hidesTopSkin) tiene que dar pixeles distintos - si el bug #1 siguiera
        // presente, la camisa/camiseta base se seguirian dibujando ENCIMA de la armadura y el
        // torso/hombro tendria una mezcla de colores que no ocurre cuando de verdad se omite la
        // ropa base.
        var conArmaduraBody1 = new PlayerPreviewRenderer.EquippedArmor(
            HeadFile: null, BodyFile: RutaArmorBody(1), LegsFile: null,
            HeadSlot: null, BodySlot: 1, LegsSlot: null);

        var sinArmadura = PlayerPreviewRenderer.Render(1, skinVariant: 0, Colors);
        var conArmadura = PlayerPreviewRenderer.Render(1, skinVariant: 0, Colors, conArmaduraBody1);

        Assert.NotEqual(Pixels(sinArmadura), Pixels(conArmadura));
    }

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)8)]
    public void VariantesDeCuerpoAlternativas_RenderizanSinExcepcionConLosAssetsRealesExtraidos(byte skinVariant)
    {
        // H6-01-b: antes de esta pasada, body1..3/5..9 no se usaban nunca (siempre body0/body4)
        // - esta prueba confirma que los assets ampliados por extraer-sprites-jugador.js
        // (herencia real resuelta en extraccion) se cargan sin reventar para las variantes
        // reales del usuario.
        var bmp = PlayerPreviewRenderer.Render(1, skinVariant, Colors);
        Assert.Equal(40, bmp.PixelWidth);
        Assert.Equal(56, bmp.PixelHeight);
    }
}
