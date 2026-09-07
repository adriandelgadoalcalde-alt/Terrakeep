using Terrakeep.Core.Data;
using Terrakeep.Core.WldFormat;
using Xunit;
using Xunit.Abstractions;

namespace Terrakeep.Core.Tests.WldFormat;

// Confirma de extremo a extremo que WldReader ya guarda u/v (antes se descartaban, ver el
// historial de WldTile.cs) y que TileNameCatalog.TileVariantName los resuelve al nombre EXACTO
// del sprite - usando un mundo y un tile_names.json REALES, no una copia de prueba. Los cofres
// (tile id 21) son el caso de uso citado en la documentacion del proyecto (mismo id, distinto
// sprite segun bioma) y casi cualquier mundo real tiene al menos uno.
public class TileVariantRealFileTests(ITestOutputHelper output)
{
    private const string WorldsDir = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds";
    private const string AssetsDir = @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets";
    private const int ChestTileId = 21;

    [Fact]
    public void RealWorld_ChestTiles_ResolveToARealVariantName()
    {
        string tileNamesPath = Path.Combine(AssetsDir, "tile_names.json");
        string worldPath = Path.Combine(WorldsDir, "El_Musgo_de_Accidentes.wld");
        if (!File.Exists(tileNamesPath) || !File.Exists(worldPath)) return;

        var tileNames = TileNameCatalog.LoadFromFile(tileNamesPath);
        var world = WldReader.Read(File.ReadAllBytes(worldPath));

        int chestsFound = 0, chestsWithKnownVariant = 0;
        for (int x = 0; x < world.Tiles.GetLength(0) && chestsFound < 50; x++)
        {
            for (int y = 0; y < world.Tiles.GetLength(1) && chestsFound < 50; y++)
            {
                var tile = world.Tiles[x, y];
                if (!tile.IsActive || tile.Type != ChestTileId) continue;
                chestsFound++;

                string variantName = tileNames.TileVariantName(tile.Type, tile.U, tile.V);
                output.WriteLine($"  cofre en ({x},{y}) u={tile.U} v={tile.V} -> '{variantName}'");
                if (variantName != tileNames.TileName(ChestTileId)) chestsWithKnownVariant++;
            }
        }

        output.WriteLine($"Cofres encontrados: {chestsFound}, con variante exacta resuelta: {chestsWithKnownVariant}");
        if (chestsFound == 0) return; // este mundo en concreto no tiene cofres - no es un fallo

        // Al menos uno debe resolver a una variante mas especifica que el nombre base
        // ("Chests") - si TileVariantName siempre cayera al generico, u/v no se estaria
        // leyendo/comparando bien.
        Assert.True(chestsWithKnownVariant > 0, "Ningun cofre real resolvio una variante especifica - u/v podria no estar leyendose bien.");
    }
}
