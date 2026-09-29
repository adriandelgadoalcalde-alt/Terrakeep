using System.Linq;
using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

// PortSeleccion Encargo4 (25-sep-2026): fija numericamente OffsetX/OffsetY/SpriteDirection de las
// 63 mascotas ya catalogadas, extraidos del decompilado real
// (Terraria/ID/ProjectileID.cs 34-37, CharacterPreviewAnimations - ".WithOffset(x, y)"/
// ".WithSpriteDirection(d)" por TIPO de proyectil real) - sin este canario, una futura edicion de
// pet_animations.json podria perder estos tres campos en silencio (JSON a mano, sin tipado) sin
// que ningun otro test lo detectara. Prueba de humo contra el JSON REAL copiado en
// Terrakeep.App/Assets (no una copia de prueba), mismo patron que VanillaItemCatalogRealFileTests.
public class PetAnimationCatalogTests
{
    private static readonly string AssetsDir =
        RutasEntornoReal.Repo(@"Terrakeep.App\Assets");

    // Tabla completa real (itemId -> (offsetX, offsetY, spriteDirection)), extraida el 25-sep-2026
    // cruzando el "shoot" de cada entrada de pet_animations.json contra
    // ProjectileID.Sets.CharacterPreviewAnimations del decompilado real - los 63 items tienen
    // entrada EXPLICITA en la tabla real (ninguno cae al valor por defecto 0,0,1).
    private static readonly Dictionary<int, (double OffsetX, double OffsetY, int SpriteDirection)> ValoresReales = new()
    {
        { 603, (6.0, 0.0, -1) },
        { 669, (0.0, 0.0, -1) },
        { 753, (-6.0, 0.0, -1) },
        { 994, (-4.0, -6.0, 1) },
        { 1170, (-8.0, -20.0, -1) },
        { 1171, (-2.0, -18.0, 1) },
        { 1172, (-2.0, 0.0, -1) },
        { 1180, (-2.0, -12.0, -1) },
        { 1181, (6.0, 0.0, -1) },
        { 1182, (4.0, 0.0, -1) },
        { 1242, (-16.0, 0.0, -1) },
        { 1311, (4.0, 0.0, -1) },
        { 1312, (0.0, 0.0, -1) },
        { 1798, (-6.0, 0.0, -1) },
        { 1799, (-4.0, 0.0, -1) },
        { 1810, (-14.0, 0.0, -1) },
        { 1837, (-4.0, 0.0, -1) },
        { 1927, (-10.0, 0.0, -1) },
        { 1959, (4.0, 0.0, -1) },
        { 2420, (-10.0, -20.0, -1) },
        { 2587, (6.0, 0.0, 1) },
        { 3060, (-18.0, 0.0, -1) },
        { 3857, (4.0, -10.0, 1) },
        { 4366, (-4.0, 0.0, -1) },
        { 4425, (-14.0, -24.0, -1) },
        { 4550, (-6.0, 0.0, -1) },
        { 4551, (0.0, 0.0, -1) },
        { 4603, (-20.0, -30.0, -1) },
        { 4604, (-10.0, 0.0, -1) },
        { 4605, (-6.0, -12.0, -1) },
        { 4701, (6.0, 0.0, -1) },
        { 4735, (-6.0, 0.0, -1) },
        { 4736, (4.0, 0.0, -1) },
        { 4737, (2.0, 0.0, -1) },
        { 4777, (4.0, 0.0, -1) },
        { 4797, (0.0, 0.0, 1) },
        { 4798, (4.0, -16.0, 1) },
        { 4799, (2.0, -8.0, 1) },
        { 4800, (0.0, 0.0, 1) },
        { 4801, (4.0, -16.0, 1) },
        { 4802, (4.0, -16.0, -1) },
        { 4803, (2.0, -8.0, 1) },
        { 4804, (4.0, -16.0, 1) },
        { 4805, (4.0, -16.0, 1) },
        { 4806, (2.0, 0.0, 1) },
        { 4808, (-4.0, -12.0, -1) },
        { 4809, (0.0, -8.0, 1) },
        { 4810, (-2.0, -12.0, -1) },
        { 4813, (2.0, 0.0, 1) },
        { 4814, (-2.0, -16.0, 1) },
        { 4815, (2.0, 0.0, 1) },
        { 4816, (6.0, 0.0, 1) },
        { 4817, (-2.0, -12.0, -1) },
        { 4960, (0.0, 0.0, 1) },
        { 5088, (0.0, 0.0, 1) },
        { 5090, (-2.0, 0.0, 1) },
        { 5091, (4.0, 0.0, 1) },
        { 5098, (4.0, 0.0, 1) },
        { 5131, (0.0, 0.0, 1) },
        { 5276, (2.0, 0.0, 1) },
        { 5297, (6.0, 0.0, -1) },
        { 5332, (2.0, 0.0, -1) },
        { 5333, (-2.0, 0.0, -1) },
    };

    [Fact]
    public void RealFile_Tiene63EntradasConLosOffsetsRealesDeCadaMascota()
    {
        string path = Path.Combine(AssetsDir, "pet_animations.json");
        if (!File.Exists(path)) return;

        var catalog = PetAnimationCatalog.LoadFromFile(path);

        Assert.Equal(63, ValoresReales.Count);
        foreach (var (itemId, esperado) in ValoresReales)
        {
            var entry = catalog.ByItemId(itemId);
            Assert.True(entry is not null, $"item {itemId} deberia existir en pet_animations.json");
            Assert.Equal(esperado.OffsetX, entry!.OffsetX);
            Assert.Equal(esperado.OffsetY, entry.OffsetY);
            Assert.Equal(esperado.SpriteDirection, entry.SpriteDirection);
        }
    }

    // 5 mascotas reales con offset NO-trivial (distinto de 0,0) mas una con SpriteDirection=-1 (la
    // mayoria de fila ya lo cubre), citadas explicitamente en el encargo original:
    //   item 994  -> proyectil 175 "Mascota Hornet" real: WithOffset(-4f, -6f), sin
    //                WithSpriteDirection (se queda en el 1 por defecto de la clase).
    //   item 4603 -> proyectil 815 real: WithOffset(-20f, -30f).WithSpriteDirection(-1) - el
    //                offset mas grande de las 63 mascotas catalogadas.
    [Theory]
    [InlineData(994, -4.0, -6.0, 1)]
    [InlineData(4603, -20.0, -30.0, -1)]
    [InlineData(1170, -8.0, -20.0, -1)]
    [InlineData(4425, -14.0, -24.0, -1)]
    [InlineData(3857, 4.0, -10.0, 1)]
    public void RealFile_MascotasCitadasEnElEncargo_TienenElOffsetRealExacto(
        int itemId, double offsetX, double offsetY, int spriteDirection)
    {
        string path = Path.Combine(AssetsDir, "pet_animations.json");
        if (!File.Exists(path)) return;

        var catalog = PetAnimationCatalog.LoadFromFile(path);
        var entry = catalog.ByItemId(itemId);

        Assert.NotNull(entry);
        Assert.Equal(offsetX, entry!.OffsetX);
        Assert.Equal(offsetY, entry.OffsetY);
        Assert.Equal(spriteDirection, entry.SpriteDirection);
    }

    [Fact]
    public void RealFile_MascotaDesconocida_DevuelveNull()
    {
        string path = Path.Combine(AssetsDir, "pet_animations.json");
        if (!File.Exists(path)) return;

        var catalog = PetAnimationCatalog.LoadFromFile(path);
        Assert.Null(catalog.ByItemId(-1));
    }

    // PortSeleccion Encargo5 (26-sep-2026): las 24 entradas reales del catalogo (de las 63) cuyo
    // proyectil ("shoot") tiene un ".WithCode(...)" real en ProjectileID.Sets.
    // CharacterPreviewAnimations (decompilado, Downloads\Keep\tModLoader-Decompiled\tModLoader\
    // Terraria\ID\ProjectileID.cs:34-37) - sin este canario, una futura edicion a mano de
    // pet_animations.json podria perder el campo "code" en silencio (JSON sin tipado) sin que
    // ningun otro test lo notara, igual que ya protegia RealFile_Tiene63EntradasConLosOffsetsReales
    // para OffsetX/OffsetY/SpriteDirection. Las 39 entradas restantes (no listadas aqui) deben
    // seguir en Code=null - verificado abajo por diferencia de conjunto contra las 63 totales.
    private static readonly Dictionary<int, string> CodigosReales = new()
    {
        { 994, "Float" }, { 1170, "Float" }, { 1171, "Float" }, { 1180, "Float" },
        { 2420, "Float" }, { 3857, "Float" }, { 4425, "Float" }, { 4603, "Float" },
        { 4605, "Float" }, { 4798, "Float" }, { 4802, "Float" }, { 4804, "Float" },
        { 4808, "Float" }, { 4810, "Float" }, { 4817, "Float" },
        { 4801, "FloatAndSpinWhenWalking" }, { 4805, "FloatAndSpinWhenWalking" },
        { 4797, "SlimePet" }, { 4960, "SlimePet" }, { 5131, "SlimePet" },
        { 5088, "BerniePet" },
        { 4799, "WormPet" }, { 4803, "WormPet" }, { 4809, "WormPet" },
    };

    [Fact]
    public void RealFile_MascotasConDelegadoCustom_TienenElCodeRealDelDecompilado()
    {
        string path = Path.Combine(AssetsDir, "pet_animations.json");
        if (!File.Exists(path)) return;

        var catalog = PetAnimationCatalog.LoadFromFile(path);

        Assert.Equal(24, CodigosReales.Count);
        foreach (var (itemId, code) in CodigosReales)
        {
            var entry = catalog.ByItemId(itemId);
            Assert.True(entry is not null, $"item {itemId} deberia existir en pet_animations.json");
            Assert.Equal(code, entry!.Code);
        }

        foreach (var itemId in ValoresReales.Keys.Except(CodigosReales.Keys))
        {
            var entry = catalog.ByItemId(itemId);
            Assert.True(entry is not null);
            Assert.Null(entry!.Code);
        }
    }
}
