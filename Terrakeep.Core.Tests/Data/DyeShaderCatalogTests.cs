using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

// GapAnalysis Encargo I (25-sep-2026): spot-checks reales contra
// Terraria.Initializers.DyeInitializer.LoadArmorDyes() decompilado (ver el comentario completo
// de cabecera de DyeShaderCatalog.cs para la cita real) - no una fixture inventada.
public class DyeShaderCatalogTests
{
    [Fact]
    public void RedDye_ColorBaseExacto_1_0_0()
    {
        var c = DyeShaderCatalog.PlainColor(1007 /* RedDye */);
        Assert.NotNull(c);
        Assert.Equal(255, c!.Value.R);
        Assert.Equal(0, c.Value.G);
        Assert.Equal(0, c.Value.B);
    }

    [Fact]
    public void BrightRedDye_FormulaRealR05Mas05()
    {
        // DyeInitializer.cs:18: UseColor(r*0.5f+0.5f, ...) - RedDye (1,0,0) -> (1, 0.5, 0.5).
        var c = DyeShaderCatalog.PlainColor(1038 /* BrightRedDye = RedDye+31 */);
        Assert.NotNull(c);
        Assert.Equal(255, c!.Value.R);
        Assert.Equal(128, c.Value.G);
        Assert.Equal(128, c.Value.B);
    }

    [Fact]
    public void RedAndBlackDye_MismoColorBase_SoloCambiaElShaderFx()
    {
        var baseColor = DyeShaderCatalog.PlainColor(1007);
        var blackVariant = DyeShaderCatalog.PlainColor(1019 /* RedandBlackDye = RedDye+12 */);
        Assert.Equal(baseColor, blackVariant);
    }

    [Fact]
    public void RedAndSilverDye_MismoColorBase_SoloCambiaElRibete()
    {
        var baseColor = DyeShaderCatalog.PlainColor(1007);
        var silverVariant = DyeShaderCatalog.PlainColor(1051 /* RedandSilverDye = RedDye+44 */);
        Assert.Equal(baseColor, silverVariant);
    }

    [Fact]
    public void BrownDye_CasoEspecialDeIdsExplicitos_ColorRealTranscrito()
    {
        var c = DyeShaderCatalog.PlainColor(2874 /* BrownDye */);
        Assert.NotNull(c);
        Assert.Equal((byte)102, c!.Value.R); // 0.4 * 255
        Assert.Equal((byte)51, c.Value.G);   // 0.2 * 255
        Assert.Equal((byte)0, c.Value.B);
    }

    [Fact]
    public void BlackDye_ArmorBrightnessColored_ColorRealTranscrito()
    {
        var c = DyeShaderCatalog.PlainColor(1050 /* BlackDye */);
        Assert.NotNull(c);
        Assert.Equal((byte)153, c!.Value.R); // 0.6 * 255
        Assert.Equal((byte)153, c.Value.G);
        Assert.Equal((byte)153, c.Value.B);
    }

    [Theory]
    [InlineData(1066)] // RainbowDye - ArmorColoredRainbow, animado
    [InlineData(1031)] // FlameDye - ArmorColoredGradient, animado
    [InlineData(1969)] // TeamDye - color dinamico segun el equipo
    [InlineData(3190)] // ReflectiveDye - reflejo de escena en tiempo real
    [InlineData(3978)] // ColorOnly - sin UseColor conocido, desconocido por seguridad
    [InlineData(2872)] // InvertDye - inversion de color, no un color fijo
    public void DyesAnimadosReales_NuncaDevuelvenUnColorPlano(int animatedDyeItemId)
    {
        Assert.False(DyeShaderCatalog.IsKnownPlainDye(animatedDyeItemId));
        Assert.Null(DyeShaderCatalog.PlainColor(animatedDyeItemId));
    }

    [Fact]
    public void ObjetoQueNoEsDye_DevuelveNull()
    {
        // Wooden Sword (id 1) - nunca fue registrado como dye.
        Assert.Null(DyeShaderCatalog.PlainColor(1));
    }

    [Fact]
    public void ConteoRealDeDyesPlanos_57()
    {
        // 12 colores base x4 variantes (48) + BrownDye x4 (4) + 5 sueltos = 57 - ver el
        // comentario completo de cabecera de DyeShaderCatalog.cs.
        Assert.Equal(57, DyeShaderCatalog.PlainDyeCount);
    }
}
