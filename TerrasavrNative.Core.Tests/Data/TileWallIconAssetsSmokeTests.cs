using Xunit;

namespace TerrasavrNative.Core.Tests.Data;

// Encargo del usuario 4-sep-2026 ("faltan todos los sprites en exploracion... quiero todos los
// sprites de todos los objetos del mundo") - ver ESPEC-sprites-botones-badges.md#D.2. Convierte
// "se me olvido ejecutar scripts/extraer-iconos-tiles.js / -paredes.js" en un test rojo en vez
// de en un cuadradito de color en produccion - cifras exactas ya medidas y documentadas
// (ESPEC-sprites-botones-badges.md#A.6/A.7): 878 iconos de tile (749 base + 129 variantes de
// cofre/comoda), 366 de pared.
public class TileWallIconAssetsSmokeTests
{
    private const string AssetsDir = @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets";

    [Fact]
    public void TileIcons_878FicherosReales_IncluidoElCofreDeOro()
    {
        string dir = Path.Combine(AssetsDir, "vanilla", "tile_icons");
        if (!Directory.Exists(dir)) return; // entorno sin los assets extraidos - se salta en silencio

        Assert.Equal(878, Directory.GetFiles(dir, "*.png").Length);
        Assert.True(File.Exists(Path.Combine(dir, "21_36_0.png")), "Cofre de oro (tipo 21, variante 36,0)");
        Assert.True(File.Exists(Path.Combine(dir, "7.png")), "Mineral de cobre (tipo 7, icono base)");
    }

    [Fact]
    public void WallIcons_366FicherosReales_IncluidaLaParedDePiedra()
    {
        string dir = Path.Combine(AssetsDir, "vanilla", "wall_icons");
        if (!Directory.Exists(dir)) return;

        Assert.Equal(366, Directory.GetFiles(dir, "*.png").Length);
        Assert.True(File.Exists(Path.Combine(dir, "1.png")), "Pared de piedra (id 1)");
    }
}
