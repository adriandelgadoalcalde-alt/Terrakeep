using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

// H6-08/H6-09/H6-10 (sexta auditoria de Opus): NpcHeadProfile es una tabla real, portada de
// Terraria.GameContent.TownNPCProfiles.cs decompilado - estos casos son spot-checks reales
// contra esa misma tabla, no valores inventados.
public class NpcHeadProfileTests
{
    [Fact]
    public void Merchant_HeadNormalYShimmerReales()
    {
        // Merchant (17): LegacyWithSimpleShimmer("Merchant", 2, 63) real.
        Assert.Equal(2, NpcHeadProfile.GetHeadIndex(17, variationIndex: 0, isShimmered: false));
        Assert.Equal(63, NpcHeadProfile.GetHeadIndex(17, variationIndex: 0, isShimmered: true));
    }

    [Fact]
    public void OldManYSkeletonMerchant_SinIconoRealEnElJuego()
    {
        // OldMan (37) y SkeletonMerchant (453): -1/-1 reales en TownNPCProfiles.cs - ninguno
        // de los dos tiene icono de cabeza en el mapa del juego real.
        Assert.Null(NpcHeadProfile.GetHeadIndex(37, 0, false));
        Assert.Null(NpcHeadProfile.GetHeadIndex(453, 0, false));
    }

    [Theory]
    [InlineData(0, 27)]
    [InlineData(1, 28)]
    [InlineData(5, 32)]
    public void TownCat_UsaLaVariacionRealDelWld(int variationIndex, int esperado)
    {
        // TownCat (637): CatHeadIDs reales = {27,28,29,30,31,32} - el shimmer no aplica a las
        // mascotas de pueblo (perfil VariantNPCProfile real, sin version shimmerizada propia).
        Assert.Equal(esperado, NpcHeadProfile.GetHeadIndex(637, variationIndex, isShimmered: false));
        Assert.Equal(esperado, NpcHeadProfile.GetHeadIndex(637, variationIndex, isShimmered: true));
    }

    [Fact]
    public void TownCat_VariacionFueraDeRango_NoRompe()
    {
        // Defensivo: un .wld real nunca deberia traer una variationIndex fuera de 0-5 para un
        // Gato de pueblo, pero si pasara (fichero corrupto/version futura), envolver es mejor
        // que lanzar una excepcion real que tumbe el escaneo de Exploracion entero.
        Assert.Equal(27, NpcHeadProfile.GetHeadIndex(637, variationIndex: 6, isShimmered: false)); // envuelve a 0
        Assert.Equal(32, NpcHeadProfile.GetHeadIndex(637, variationIndex: -1, isShimmered: false)); // envuelve al ultimo
    }

    [Fact]
    public void TownSlimeBlue_IndiceFijoSinVariacionNiShimmer()
    {
        // TownSlimeBlue (670): LegacyNPCProfile fijo, headId=46, sin distincion shimmer real.
        Assert.Equal(46, NpcHeadProfile.GetHeadIndex(670, 0, false));
        Assert.Equal(46, NpcHeadProfile.GetHeadIndex(670, 0, true));
    }

    [Fact]
    public void TaxCollector_ElHallazgoExplicitoDeOpus()
    {
        // TaxCollector (441): LegacyWithSimpleShimmer("TaxCollector", 23, 70) real - el hallazgo
        // explicito del informe ("falta el NPC 441").
        Assert.Equal(23, NpcHeadProfile.GetHeadIndex(441, 0, false));
        Assert.Equal(70, NpcHeadProfile.GetHeadIndex(441, 0, true));
    }

    [Fact]
    public void NpcSinPerfilReal_DevuelveNull()
    {
        // Un enemigo/critter cualquiera (no un NPC de pueblo real) no tiene entrada en la
        // tabla - null, no una excepcion ni un indice inventado.
        Assert.Null(NpcHeadProfile.GetHeadIndex(npcType: 1, 0, false)); // Zombie generico
    }
}
