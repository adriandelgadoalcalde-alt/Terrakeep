using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

// Prueba de humo contra el JSON real generado por scripts/extraer-tintes-pelo.py (no una
// fixture) - se salta sola si la carpeta no existe, mismo patron que el resto de *RealFile
// Tests del proyecto.
public class HairDyeCatalogTests
{
    private const string AssetsDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets";

    [Fact]
    public void RealFile_Has12EntriesInRealCallOrder()
    {
        string path = Path.Combine(AssetsDir, "hair_dyes.json");
        if (!File.Exists(path)) return;

        var catalog = HairDyeCatalog.LoadFromFile(path);

        Assert.Equal(12, catalog.Entries.Count);
        for (int i = 0; i < catalog.Entries.Count; i++)
            Assert.Equal(i + 1, catalog.Entries[i].Index);

        // Orden de llamada REAL (LoadHairDyes llama primero a LoadLegacyHairdyes, no el orden
        // por numero de linea del fichero fuente - ver el comentario del script).
        Assert.Equal(1977, catalog.Entries[0].ItemId);
        Assert.Equal(2863, catalog.Entries[10].ItemId);
        Assert.Equal(3259, catalog.Entries[11].ItemId);
    }
}
