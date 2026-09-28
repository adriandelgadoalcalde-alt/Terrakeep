using Terrakeep.Core.Data;

namespace Terrakeep.Core.Tests.Data;

// Revisor visual r2 de la FASE D (28-sep-2026, L-01): las 10 armaduras de Calamity sin traduccion propia salian en
// CamelCase solo en español ("Armadura - DesertProwler"); el ingles ya las separaba.
public class EtiquetasCalamityEsTests
{
    [Theory]
    [InlineData("Armor/DesertProwler", "Armadura - Desert Prowler", "Armor - Desert Prowler")]
    [InlineData("Armor/GodSlayer", "Armadura - God Slayer", "Armor - God Slayer")]
    [InlineData("Armor/TitanHeart", "Armadura - Titan Heart", "Armor - Titan Heart")]
    [InlineData("Armor/FathomSwarmer", "Armadura - Fathom Swarmer", "Armor - Fathom Swarmer")]
    [InlineData("Armor/OmegaBlue", "Armadura - Omega Blue", "Armor - Omega Blue")]
    [InlineData("Armor/Aerospec", "Armadura - Aerospec", "Armor - Aerospec")]
    public void LasArmadurasSinTraduccionSeSeparanIgualQueEnIngles(string categoria, string es, string en)
    {
        Assert.Equal(es, LibraryTreeBuilder.CalamityCategoryLabel(categoria));
        Assert.Equal(en, LibraryTreeBuilder.CalamityCategoryLabelEn(categoria));
    }

    [Fact]
    public void UnaCategoriaDesconocidaDeUnSoloSegmentoSeDevuelveTalCual()
        => Assert.Equal("NoExisteEstaCategoria", LibraryTreeBuilder.CalamityCategoryLabel("NoExisteEstaCategoria"));
}
