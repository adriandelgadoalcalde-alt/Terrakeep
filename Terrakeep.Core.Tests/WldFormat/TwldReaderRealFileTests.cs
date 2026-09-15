using Terrakeep.Core.WldFormat;
using Xunit;
using Xunit.Abstractions;

namespace Terrakeep.Core.Tests.WldFormat;

// T3 del documento I+D real ("Terrakeep, editor de cofres/letreros del .wld", recomendacion 4,
// 15-sep-2026): prueba contra el `.twld` REAL de "Afueras de Larvas de gusano" - el mismo mundo
// que la bitacora (6-sep-2026) y el comentario real de ExplorationViewModel.ChestKindName ya
// documentan con 51 cofres reales de Calamity saliendo como "Tile #-1" en Exploracion, porque
// tModLoader escribe un tile de mod como aire en el .wld normal (WorldFile.cs:1425 real) y guarda
// su tipo de verdad aparte, en el .twld. Esta prueba demuestra que TwldReader resuelve esos
// mismos cofres con un nombre real (mod + tile), no solo el ID crudo.
public class TwldReaderRealFileTests(ITestOutputHelper output)
{
    private const string WorldsDir = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds";
    private const string WorldName = "Afueras_de_Larvas_de_gusano";

    [Fact]
    public void Read_MundoRealConCalamity_ResuelveLosCofresQueSalianComoTileMenosUno()
    {
        string wldPath = Path.Combine(WorldsDir, WorldName + ".wld");
        string twldPath = Path.Combine(WorldsDir, WorldName + ".twld");
        if (!File.Exists(wldPath) || !File.Exists(twldPath)) return; // LIMITE REAL: mundo no disponible en esta maquina

        var world = WldReader.Read(File.ReadAllBytes(wldPath));

        // Mismos cofres "Tile #-1" que documenta ExplorationViewModel.ChestKindName: la casilla
        // real del cofre esta INACTIVA en el .wld (Type<0) porque es un tile de mod.
        var chestsSinResolver = world.Chests
            .Where(c => c.X >= 0 && c.X < world.Header.TilesWide && c.Y >= 0 && c.Y < world.Header.TilesHigh)
            .Where(c => !world.Tiles[c.X, c.Y].IsActive)
            .ToList();
        output.WriteLine($"Cofres con tile inactivo en el .wld (\"Tile #-1\" hoy): {chestsSinResolver.Count} de {world.Chests.Count}");
        Assert.True(chestsSinResolver.Count > 0, "Este mundo real deberia reproducir el bug documentado (51 cofres de Calamity) - si esto falla, puede que el mundo de referencia haya cambiado.");

        var positions = chestsSinResolver.Select(c => (c.X, c.Y)).ToHashSet();
        var content = TwldReader.Read(File.ReadAllBytes(twldPath), world.Header.TilesWide, world.Header.TilesHigh, positions);

        output.WriteLine($"Entradas reales de tileMap en el .twld: {content.TileEntries.Count}");
        foreach (var e in content.TileEntries.Values.Take(10))
            output.WriteLine($"  tileMap: value={e.Type} {e.DisplayName} framed={e.FrameImportant}");

        Assert.True(content.TileEntries.Count > 0, "El .twld real de un mundo con Calamity cargado tiene que traer al menos una entrada de tileMap.");
        // Nombres reales ya documentados en la bitacora/comentario de ChestKindName para este
        // mundo concreto (AbyssTreasureChest, RustyChestTile, AstralChestLocked, SecurityChestTile,
        // AshenChest, VoidChest) - no se exige el conjunto exacto (puede variar si el mod se
        // actualiza), solo que el mod real de Calamity aparezca de verdad.
        Assert.Contains(content.TileEntries.Values, e => e.Mod.Contains("Calamity", StringComparison.OrdinalIgnoreCase));

        int resueltos = 0;
        foreach (var chest in chestsSinResolver)
        {
            string? nombre = content.DescribeTileAt(chest.X, chest.Y);
            if (nombre != null)
            {
                resueltos++;
                Assert.Contains(":", nombre); // formato real "Mod: NombreInterno"
            }
        }
        output.WriteLine($"Cofres resueltos con nombre real de mod: {resueltos} de {chestsSinResolver.Count}");
        Assert.True(resueltos > 0, "Al menos un cofre de los que hoy salen como \"Tile #-1\" tiene que resolverse con un nombre real via .twld.");
    }

    [Fact]
    public void Read_SinPosicionesDeInteres_NoDecodificaLaRejillaPesada()
    {
        string twldPath = Path.Combine(WorldsDir, WorldName + ".twld");
        if (!File.Exists(twldPath)) return;

        // Sin `positionsOfInterest`, solo se leen tileMap/wallMap (barato) - las rejillas densas
        // (potencialmente decenas de MB en un mundo real) se quedan vacias, no hace falta
        // decodificarlas para saber que ENTRADAS trae el archivo.
        var content = TwldReader.Read(File.ReadAllBytes(twldPath), tilesWide: 8400, tilesHigh: 2400);
        Assert.True(content.TileEntries.Count > 0);
        Assert.Empty(content.ModTileTypeAt);
        Assert.Empty(content.ModWallTypeAt);
    }
}
