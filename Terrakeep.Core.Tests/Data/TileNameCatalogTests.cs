using System.Text;
using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

public class TileNameCatalogTests
{
    private const string SampleJson = """
    {
      "tiles": {
        "2": { "name": "Grass Block", "name_es": "Bloque de hierba" },
        "21": {
          "name": "Chests",
          "frames": {
            "0,0": { "name": "Wooden Chest" },
            "36,0": { "name": "Gold Chest", "name_es": "Cofre de oro" },
            "648,0": { "name": "Jungle Chest", "name_es": "Cofre de la selva" }
          }
        }
      },
      "walls": {
        "1": { "name": "Stone Wall", "name_es": "Pared de piedra" }
      }
    }
    """;

    private static TileNameCatalog Load() =>
        TileNameCatalog.LoadFromStream(new MemoryStream(Encoding.UTF8.GetBytes(SampleJson)));

    [Fact]
    public void TileName_ResolvesBaseNames()
    {
        var catalog = Load();
        Assert.Equal("Bloque de hierba", catalog.TileName(2));
        Assert.Equal("Chests", catalog.TileName(21)); // sin traduccion es-ES, cae al ingles
        Assert.Equal("Tile #999", catalog.TileName(999));
    }

    [Fact]
    public void WallName_ResolvesAndTreatsZeroAsEmpty()
    {
        var catalog = Load();
        Assert.Equal("Pared de piedra", catalog.WallName(1));
        Assert.Equal(string.Empty, catalog.WallName(0));
    }

    [Fact]
    public void TileVariantName_ResolvesExactSpriteByUV()
    {
        var catalog = Load();
        Assert.Equal("Cofre de oro", catalog.TileVariantName(21, 36, 0));
        Assert.Equal("Cofre de la selva", catalog.TileVariantName(21, 648, 0));
        Assert.Equal("Wooden Chest", catalog.TileVariantName(21, 0, 0)); // sin name_es, cae al ingles
    }

    [Fact]
    public void TileVariantName_UnknownUV_FallsBackToBaseName()
    {
        var catalog = Load();
        Assert.Equal("Chests", catalog.TileVariantName(21, 9999, 9999));
    }

    [Fact]
    public void TileVariantName_TileWithoutFrames_FallsBackToBaseName()
    {
        var catalog = Load();
        Assert.Equal("Bloque de hierba", catalog.TileVariantName(2, 0, 0));
    }
}
