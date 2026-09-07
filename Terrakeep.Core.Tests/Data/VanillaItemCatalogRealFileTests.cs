using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

// Prueba de humo contra los JSON reales copiados en Terrakeep.App/Assets (no una copia
// de prueba) - confirma en particular que vanilla_item_ids_by_key.json (generado desde
// ItemID.cs decompilado para resolver el "pid" de builds.json a un id real, ver
// MainViewModel.AutoEquip) da el id correcto para objetos conocidos. Se salta sola si la
// carpeta no existe en la maquina donde corran los tests.
public class VanillaItemCatalogRealFileTests
{
    private const string AssetsDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets";

    [Fact]
    public void RealFiles_GetIdByKey_ResolvesKnownItems()
    {
        string namesPath = Path.Combine(AssetsDir, "vanilla_item_names.json");
        string byKeyPath = Path.Combine(AssetsDir, "vanilla_item_names_by_key.json");
        string idsByKeyPath = Path.Combine(AssetsDir, "vanilla_item_ids_by_key.json");
        if (!File.Exists(namesPath) || !File.Exists(byKeyPath) || !File.Exists(idsByKeyPath)) return;

        var catalog = VanillaItemCatalog.LoadFromFile(namesPath, byKeyPath, idsByKeyPath);

        // id 1 = "Pico de hierro" (Iron Pickaxe), hecho ya verificado en la generacion original
        // de vanilla_item_names.json - confirma round-trip nombre-interno -> id -> nombre real.
        Assert.Equal(1, catalog.GetIdByKey("IronPickaxe"));
        Assert.Equal("Pico de hierro", catalog.GetName(catalog.GetIdByKey("IronPickaxe")!.Value));

        // Objeto citado en builds.json real (etapa Pre-Hardmode, clase Cuerpo a cuerpo).
        Assert.Equal(231, catalog.GetIdByKey("MoltenHelmet"));

        Assert.Null(catalog.GetIdByKey("EsteNombreNoExiste123"));
    }
}
