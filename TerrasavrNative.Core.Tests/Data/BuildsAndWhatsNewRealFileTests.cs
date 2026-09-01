using TerrasavrNative.Core.Data;
using Xunit;

namespace TerrasavrNative.Core.Tests.Data;

// Prueba de humo contra los JSON reales copiados en TerrasavrNative.App/Assets - habria
// pillado en seco el desajuste de esquema real (whats_new.json: "items" es una lista de
// objetos {key,es,en}, no de strings) antes de que la app llegara a crashear en runtime.
public class BuildsAndWhatsNewRealFileTests
{
    private const string AppAssetsDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets";

    [Fact]
    public void RealBuildsJson_LoadsAllStagesAndClasses()
    {
        string path = Path.Combine(AppAssetsDir, "builds.json");
        if (!File.Exists(path)) return;

        var catalog = BuildsCatalog.LoadFromFile(path);

        Assert.NotEmpty(catalog.Stages);
        foreach (var stage in catalog.Stages)
        {
            Assert.False(string.IsNullOrWhiteSpace(stage.Label));
            Assert.NotEmpty(stage.Classes);
        }
    }

    [Fact]
    public void RealBuildsCalamityJson_LoadsAllStagesIncludingRogue()
    {
        string path = Path.Combine(AppAssetsDir, "builds_calamity.json");
        if (!File.Exists(path)) return;

        var catalog = BuildsCatalog.LoadFromFile(path);

        Assert.NotEmpty(catalog.Stages);
        Assert.Contains(catalog.Stages, s => s.Classes.ContainsKey("rogue"));
    }

    [Fact]
    public void RealWhatsNewJson_LoadsEntriesWithItemsAndChanges()
    {
        string path = Path.Combine(AppAssetsDir, "whats_new.json");
        if (!File.Exists(path)) return;

        var catalog = WhatsNewCatalog.LoadFromFile(path);

        Assert.NotEmpty(catalog.Entries);
        // La version 1.4.5.7 trajo 40 objetos nuevos (sin cambios de comportamiento propios);
        // la 1.4.5.8 al reves (solo correcciones, cero objetos nuevos) - cada version puede
        // tener cualquier combinacion de items/changes, no ambos a la vez necesariamente.
        var v1457 = catalog.Entries.FirstOrDefault(e => e.Version == "1.4.5.7");
        Assert.NotNull(v1457);
        Assert.Equal(40, v1457!.Items.Count);
        Assert.All(v1457.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.DisplayName)));

        var v1458 = catalog.Entries.FirstOrDefault(e => e.Version == "1.4.5.8");
        Assert.NotNull(v1458);
        Assert.NotEmpty(v1458!.Changes);
    }
}
