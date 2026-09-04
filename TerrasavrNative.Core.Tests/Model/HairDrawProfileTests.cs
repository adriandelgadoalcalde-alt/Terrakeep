using TerrasavrNative.Core.Model;
using Xunit;

namespace TerrasavrNative.Core.Tests.Model;

// H6-07 (sexta auditoria de Opus, Tanda D): HairDrawProfile es una tabla real, portada tal
// cual de Terraria.Player.GetHairSettings() decompilado (Terraria/Player.cs) - estos casos son
// spot-checks reales contra esa misma tabla y su formula real de backHairDraw, no valores
// inventados. Los ids de headSlot 10/13 tienen spot-check real cruzado en
// EquipmentAppearanceResolverHairTests.cs (Gafas de proteccion=10/fullHair, Cubo vacio=13/hatHair).
public class HairDrawProfileTests
{
    [Theory]
    [InlineData(10, true)]   // Gafas de proteccion (id real 37) - fullHair real
    [InlineData(139, true)]  // dentro de la lista real
    [InlineData(13, false)]  // Cubo vacio (id real 205) - hatHair, NO fullHair
    [InlineData(1, false)]   // headSlot cualquiera fuera de las dos listas - casco completo real
    public void IsFullHair_CoincideConLaListaRealDeGetHairSettings(int headSlot, bool esperado)
    {
        Assert.Equal(esperado, HairDrawProfile.IsFullHair(headSlot));
    }

    [Theory]
    [InlineData(13, true)]   // Cubo vacio (id real 205) - hatHair real
    [InlineData(292, true)]  // dentro de la lista real
    [InlineData(10, false)]  // Gafas de proteccion - fullHair, NO hatHair
    [InlineData(1, false)]
    public void IsHatHair_CoincideConLaListaRealDeGetHairSettings(int headSlot, bool esperado)
    {
        Assert.Equal(esperado, HairDrawProfile.IsHatHair(headSlot));
    }

    [Fact]
    public void NingunHeadSlot_EstaEnLasDosListasALaVez()
    {
        // Real: GetHairSettings es un unico switch (case) - un headSlot nunca puede caer en
        // fullHair Y hatHair a la vez. Verificacion cruzada real sobre el rango completo de
        // headSlot conocido (0..300, con margen).
        for (int slot = 0; slot <= 300; slot++)
            Assert.False(HairDrawProfile.IsFullHair(slot) && HairDrawProfile.IsHatHair(slot),
                $"headSlot={slot} esta en las dos listas a la vez - imposible en el juego real.");
    }

    // Formula real EXACTA (Player.cs, GetHairSettings): "num > 50 && (num < 56 || num > 63) &&
    // (num < 74 || num > 77) && (num < 88 || num > 89) && num != 94 && num != 100 &&
    // num != 104 && num != 112 && num < 116" + 6/133/134/146/162 sueltos.
    [Theory]
    [InlineData(50, false)]  // limite real: 50 NO cuenta ("num > 50", estricto)
    [InlineData(51, true)]   // primer id real dentro del rango
    [InlineData(55, true)]
    [InlineData(56, false)]  // entra en el hueco real 56-63
    [InlineData(63, false)]
    [InlineData(64, true)]   // sale del hueco real
    [InlineData(94, false)]  // exclusion real suelta
    [InlineData(115, true)]  // limite real: "num < 116"
    [InlineData(116, false)]
    [InlineData(6, true)]    // exclusion real suelta FUERA del rango >50
    [InlineData(133, true)]
    [InlineData(134, true)]
    [InlineData(146, true)]
    [InlineData(162, true)]
    [InlineData(163, false)]
    public void IsBackHairDraw_CoincideConLaFormulaRealExacta(int hairStyle, bool esperado)
    {
        Assert.Equal(esperado, HairDrawProfile.IsBackHairDraw(hairStyle));
    }
}
