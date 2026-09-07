using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

// Prueba de humo contra los JSON reales copiados en Terrakeep.App/Assets - habria
// pillado en seco el desajuste de esquema real (whats_new.json: "items" es una lista de
// objetos {key,es,en}, no de strings) antes de que la app llegara a crashear en runtime.
public class BuildsAndWhatsNewRealFileTests
{
    private const string AppAssetsDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets";

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
        // Pedido explicito del usuario (2-sep-2026): "dos pestañas, Terraria vanilla y
        // tModLoader/Calamity Mod" - whats_new.json (mezclaba las dos cosas) se retiro, esta
        // version vanilla real conserva el mismo contenido, solo cambia el fichero.
        string path = Path.Combine(AppAssetsDir, "whats_new_vanilla.json");
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
        Assert.NotEmpty(v1458.Bugfixes); // pedido explicito del usuario: bugfixes ya no se ignora
    }

    // Pedido explicito del usuario (2-sep-2026): "dos pestañas, Terraria vanilla y tModLoader/
    // Calamity Mod" - gemelo real del test de arriba, contra el registro real de Calamity Mod
    // (fuente: calamitymod.wiki.gg, versiones 2.2.0-2.2.4).
    [Fact]
    public void RealWhatsNewCalamityJson_LoadsEntriesWithBugfixes()
    {
        string path = Path.Combine(AppAssetsDir, "whats_new_calamity.json");
        if (!File.Exists(path)) return;

        var catalog = WhatsNewCatalog.LoadFromFile(path);

        Assert.Equal(5, catalog.Entries.Count); // 2.2.0 a 2.2.4
        var v224 = catalog.Entries.FirstOrDefault(e => e.Version.StartsWith("2.2.4"));
        Assert.NotNull(v224);
        Assert.NotEmpty(v224!.Bugfixes);
        var v220 = catalog.Entries.FirstOrDefault(e => e.Version.StartsWith("2.2.0"));
        Assert.NotNull(v220);
        Assert.NotEmpty(v220!.Items); // "Hog Wild" trajo objetos nuevos reales
    }
}
