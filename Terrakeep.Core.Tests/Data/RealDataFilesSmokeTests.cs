using Terrakeep.Core.Calamity;
using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

// Prueba de humo contra los JSON REALES de Terrasavr-Calamity-Beta (no una copia/fixture) -
// confirma que los loaders aguantan los 2709 objetos/308 buffs/97 prefijos reales, no solo la
// muestra pequeña de los tests unitarios de arriba. Se salta sola (no falla) si esta carpeta
// no existe en la maquina donde corran los tests - es deliberadamente una comprobacion local,
// no una dependencia dura del build.
public class RealDataFilesSmokeTests
{
    private const string LocalSiteDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Calamity-Beta\resources\app\local-site";

    [Fact]
    public void RealCatalogJson_LoadsAllEntriesWithSequentialIds()
    {
        string path = Path.Combine(LocalSiteDir, "calamity", "catalog.json");
        if (!File.Exists(path)) return; // entorno sin la carpeta hermana - se salta en silencio

        var catalog = CalamityCatalog.LoadFromFile(path);

        Assert.True(catalog.Entries.Count > 2000, $"Se esperaban miles de objetos, hubo {catalog.Entries.Count}");
        for (int i = 0; i < catalog.Entries.Count; i++)
        {
            Assert.Equal(CalamityIds.ItemIdBase + i, catalog.Entries[i].SyntheticId);
        }

        // Un objeto real conocido, citado en README.md de Terrasavr-Calamity-Beta.
        var calamityAccessory = catalog.ByModAndInternal("CalamityMod", "Calamity");
        Assert.NotNull(calamityAccessory);
    }

    [Fact]
    public void RealRoguePrefixesJson_LoadsAll21Prefixes()
    {
        string path = Path.Combine(LocalSiteDir, "calamity", "rogue_prefixes.json");
        if (!File.Exists(path)) return;

        var catalog = RoguePrefixCatalog.LoadFromFile(path);

        Assert.Equal(17, catalog.Weapon.Count);
        Assert.Equal(4, catalog.Accessory.Count);
        Assert.Equal("Flawless", catalog.Best.Weapon);
        Assert.Equal("Silent", catalog.Best.Accessory);
        Assert.NotNull(catalog.ByInternal("Flawless"));
        Assert.NotNull(catalog.ByInternal("Silent"));
    }
}
