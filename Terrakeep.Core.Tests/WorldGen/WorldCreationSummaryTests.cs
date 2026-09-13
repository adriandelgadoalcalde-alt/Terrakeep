using Terrakeep.Core.WorldGen;
using Xunit;

namespace Terrakeep.Core.Tests.WorldGen;

// Vista previa de generacion de mundo (14-sep-2026, punto 9 de bitacora.md 13-sep-2026) - el
// unico dato realmente determinista de toda la generacion es el tamaño en tiles (constante real
// del juego) y las semillas secretas (efectos reales, documentados, disparados por texto exacto
// - ver el comentario real de SpecialSeedCatalog, portado 1:1 desde UIWorldCreation.cs
// decompilado de tModLoader).
public class WorldCreationSummaryTests
{
    [Theory]
    [InlineData(WorldSizeOption.Small, 4200, 1200)]
    [InlineData(WorldSizeOption.Medium, 6400, 1800)]
    [InlineData(WorldSizeOption.Large, 8400, 2400)]
    public void WorldSizeCatalog_DevuelveLasDimensionesRealesDelJuego(WorldSizeOption size, int anchoEsperado, int altoEsperado)
    {
        var dims = WorldSizeCatalog.Get(size);
        Assert.Equal(anchoEsperado, dims.TilesWide);
        Assert.Equal(altoEsperado, dims.TilesHigh);
    }

    [Theory]
    [InlineData("no traps", SpecialSeedEffect.NoTraps)]
    [InlineData("NoTraps", SpecialSeedEffect.NoTraps)] // sin espacio, mayusculas - real, ToLower en el juego
    [InlineData("not the bees", SpecialSeedEffect.NotTheBees)]
    [InlineData("not the bees!", SpecialSeedEffect.NotTheBees)]
    [InlineData("NotTheBees", SpecialSeedEffect.NotTheBees)]
    [InlineData("for the worthy", SpecialSeedEffect.ForTheWorthy)]
    [InlineData("fortheworthy", SpecialSeedEffect.ForTheWorthy)]
    [InlineData("don't dig up", SpecialSeedEffect.DontDigUp)]
    [InlineData("dont dig up", SpecialSeedEffect.DontDigUp)]
    [InlineData("dontdigup", SpecialSeedEffect.DontDigUp)]
    [InlineData("celebrationmk10", SpecialSeedEffect.CelebrationMk10)]
    [InlineData("the constant", SpecialSeedEffect.TheConstant)]
    [InlineData("theconstant", SpecialSeedEffect.TheConstant)]
    [InlineData("constant", SpecialSeedEffect.TheConstant)]
    [InlineData("eye4aneye", SpecialSeedEffect.TheConstant)]
    [InlineData("eyeforaneye", SpecialSeedEffect.TheConstant)]
    [InlineData("5162020", SpecialSeedEffect.DrunkWorld)]
    public void Detect_SemillaSecretaConocida_ReconoceExactamenteUnEfecto(string seedText, SpecialSeedEffect esperado)
    {
        var efectos = SpecialSeedCatalog.Detect(seedText);
        Assert.Single(efectos);
        Assert.Equal(esperado, efectos[0]);
    }

    // "get fixed boi" (Zenith, real): activa TODAS las demas banderas a la vez, incluida
    // DrunkWorld (WorldGen.GenerateWorld real: `if (seed == 5162020 || everythingWorldGen)`) -
    // confirmado byte a byte contra el codigo fuente decompilado, no una suposicion.
    [Theory]
    [InlineData("get fixed boi")]
    [InlineData("getfixedboi")]
    public void Detect_GetFixedBoi_ActivaTodosLosEfectosALaVez(string seedText)
    {
        var efectos = SpecialSeedCatalog.Detect(seedText);
        Assert.Equal(8, efectos.Count);
        Assert.Contains(SpecialSeedEffect.NoTraps, efectos);
        Assert.Contains(SpecialSeedEffect.NotTheBees, efectos);
        Assert.Contains(SpecialSeedEffect.ForTheWorthy, efectos);
        Assert.Contains(SpecialSeedEffect.DontDigUp, efectos);
        Assert.Contains(SpecialSeedEffect.CelebrationMk10, efectos);
        Assert.Contains(SpecialSeedEffect.TheConstant, efectos);
        Assert.Contains(SpecialSeedEffect.DrunkWorld, efectos);
        Assert.Contains(SpecialSeedEffect.Zenith, efectos);
    }

    [Theory]
    [InlineData("")]
    [InlineData("mi mundo normal")]
    [InlineData("1234567")] // numerica real pero NO es 5162020
    public void Detect_SemillaNormal_NoActivaNingunEfecto(string seedText) =>
        Assert.Empty(SpecialSeedCatalog.Detect(seedText));

    [Fact]
    public void Build_ComponeElResumenCompletoConLasDimensionesYLaSemillaReales()
    {
        var resumen = WorldCreationSummaryBuilder.Build(WorldSizeOption.Large, difficulty: 1, WorldEvilOption.Crimson, "for the worthy");

        Assert.Equal(8400, resumen.TilesWide);
        Assert.Equal(2400, resumen.TilesHigh);
        Assert.Equal(1, resumen.Difficulty);
        Assert.Equal(WorldEvilOption.Crimson, resumen.Evil);
        Assert.Single(resumen.SpecialSeedEffects);
        Assert.Equal(SpecialSeedEffect.ForTheWorthy, resumen.SpecialSeedEffects[0]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Build_DificultadFueraDeRango_Lanza(int dificultadInvalida) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => WorldCreationSummaryBuilder.Build(WorldSizeOption.Small, dificultadInvalida, WorldEvilOption.Random, null));

    [Fact]
    public void Build_SemillaNula_NoLanzaYQuedaVacia()
    {
        var resumen = WorldCreationSummaryBuilder.Build(WorldSizeOption.Medium, 0, WorldEvilOption.Random, null);
        Assert.Equal(string.Empty, resumen.SeedText);
        Assert.Empty(resumen.SpecialSeedEffects);
    }
}
