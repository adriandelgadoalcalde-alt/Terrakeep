using TerrasavrNative.Core.Data;
using Xunit;

namespace TerrasavrNative.Core.Tests.Data;

// WS2 de TerrakeepMod (6-sep-2026) - ver LibraryTreeBuilderTests para el porque. Aqui lo que se
// protege sobre todo son las 6 listas literales de ids portadas de initLibs() real de Terrasavr
// (el valor irreemplazable de este builder): que sigan saliendo tal cual, en su orden real, tras
// el movimiento a Core.
public class BuffTreeBuilderTests
{
    private const string AppAssetsDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets";

    private static string? FakeBuffIcon(int id) => $"buff:{id}";
    private static string? FakeItemIcon(int id) => $"objeto:{id}";

    private static (VanillaBuffCatalog Vanilla, CalamityBuffCatalog CalamityBuffs, CalamityCatalog CalamityItems)? LoadBuffCatalogs()
    {
        string names = Path.Combine(AppAssetsDir, "vanilla_buff_names.json");
        string descs = Path.Combine(AppAssetsDir, "vanilla_buff_descriptions.json");
        string namesEs = Path.Combine(AppAssetsDir, "vanilla_buff_names_es.json");
        string calBuffs = Path.Combine(AppAssetsDir, "calamity", "buffs.json");
        string calDescs = Path.Combine(AppAssetsDir, "calamity_buff_descriptions.json");
        string calDebuffs = Path.Combine(AppAssetsDir, "calamity", "buff_debuffs.json");
        string calItems = Path.Combine(AppAssetsDir, "calamity", "catalog.json");
        if (!File.Exists(names) || !File.Exists(descs) || !File.Exists(namesEs) ||
            !File.Exists(calBuffs) || !File.Exists(calDescs) || !File.Exists(calDebuffs) || !File.Exists(calItems))
            return null;
        return (
            VanillaBuffCatalog.LoadFromFile(names, descs, namesEs),
            CalamityBuffCatalog.LoadFromFile(calBuffs, calDescs, calDebuffs),
            CalamityCatalog.LoadFromFile(calItems));
    }

    [Fact]
    public void ArbolDeBuffsReal_TieneLasSeisCarpetasCuradasMasIndiceMasCalamity_EnEseOrden()
    {
        if (LoadBuffCatalogs() is not { } c) return;

        var roots = BuffTreeBuilder.BuildBuffTree(c.Vanilla, c.CalamityBuffs, c.CalamityItems, FakeBuffIcon, FakeItemIcon);

        Assert.Equal(8, roots.Count);
        Assert.Equal(
            ["Utilidad", "Offensivo", "Defensivo", "Special", "Mascota", "Negativo", "Indice", "Calamity"],
            roots.Select(n => n.FullPath).ToArray());
        // Los nombres visibles llevan el recuento real, y "Offensivo"/"Special" son typos REALES
        // de Terrasavr que no se corrigen.
        Assert.Equal("Utilidad (17)", roots[0].Name);
        Assert.Equal("Offensivo (18)", roots[1].Name);
        Assert.Equal("Special (10)", roots[3].Name);
        Assert.Equal("Calamity (mod)", roots[7].Name);
    }

    [Fact]
    public void ArbolDeBuffsReal_LasListasLiteralesDeInitLibsSalenTalCual_EnSuOrdenReal()
    {
        if (LoadBuffCatalogs() is not { } c) return;

        var roots = BuffTreeBuilder.BuildBuffTree(c.Vanilla, c.CalamityBuffs, c.CalamityItems, FakeBuffIcon, FakeItemIcon);

        // Puerto literal real de initLibs() (script.beautified.js) - el orden NO es ascendente a
        // proposito (ej. Utilidad acaba en 57, 3, 63... y Negativo empieza en 21, 20, 22...).
        Assert.Equal([1, 4, 8, 9, 10, 11, 12, 15, 18, 19, 27, 34, 57, 3, 63, 101, 102], roots[0].ItemIdsOrdered);
        Assert.Equal([7, 13, 86, 16, 25, 17, 71, 73, 74, 75, 76, 77, 78, 79, 93, 98, 99, 100], roots[1].ItemIdsOrdered);
        Assert.Equal([5, 14, 26, 43, 48, 58, 59, 62, 87, 89, 95, 96, 97], roots[2].ItemIdsOrdered);
        Assert.Equal([3, 6, 26, 28, 29, 60, 64, 49, 83, 90], roots[3].ItemIdsOrdered);
        Assert.Equal([40, 41, 42, 45, 50, 51, 52, 53, 54, 55, 56, 61, 65, 66, 81, 82, 84, 85, 91, 92], roots[4].ItemIdsOrdered);
        Assert.Equal([21, 20, 22, 23, 24, 30, 31, 32, 33, 35, 36, 37, 38, 44, 46, 47, 67, 68, 69, 70, 72, 80, 86, 88, 94, 103], roots[5].ItemIdsOrdered);
        // Pertenencia multiple real (86 esta en Offensivo Y en Negativo; 3 en Utilidad Y Special;
        // 26 en Defensivo Y Special) - igual que el arbol real de Terrasavr.
        Assert.Contains(86, roots[1].ItemIdSet);
        Assert.Contains(86, roots[5].ItemIdSet);
        Assert.Contains(26, roots[2].ItemIdSet);
        Assert.Contains(26, roots[3].ItemIdSet);
    }

    [Fact]
    public void ArbolDeBuffsReal_ElIndicePaginaDe33En33YCubreTodoslosBuffsVanilla()
    {
        if (LoadBuffCatalogs() is not { } c) return;

        var indice = BuffTreeBuilder.BuildBuffTree(c.Vanilla, c.CalamityBuffs, c.CalamityItems, FakeBuffIcon, FakeItemIcon)[6];

        var todos = c.Vanilla.AllEntries().Select(e => e.Id).OrderBy(id => id).ToList();
        Assert.Equal($"Índice ({todos.Count})", indice.Name);
        Assert.Equal(todos, indice.ItemIdsOrdered);
        Assert.NotEmpty(indice.Children);
        Assert.All(indice.Children, p => Assert.True(p.ItemIdsOrdered.Count <= 33));
        Assert.Equal("Indice/1-33", indice.Children[0].FullPath);
    }

    [Fact]
    public void ArbolDeBuffsReal_LaCarpetaDeCalamityUsaElIconoDeUnOBJETO_NoDeUnBuff()
    {
        if (LoadBuffCatalogs() is not { } c) return;

        var calamity = BuffTreeBuilder.BuildBuffTree(c.Vanilla, c.CalamityBuffs, c.CalamityItems, FakeBuffIcon, FakeItemIcon)[7];

        // Criterio ya establecido: ningun buff puede representar "el mod entero", se usa el
        // objeto real CalamityMod/Calamity - por eso el resolutor de OBJETOS, no el de buffs.
        Assert.StartsWith("objeto:", calamity.IconPath!);
        Assert.All(calamity.Children, n => Assert.StartsWith("buff:", n.IconPath!));
        // Ninguna categoria real de buffs de Calamity lleva barra, asi que cuelgan todas
        // directas de la raiz (sin carpeta intermedia), y las hojas se paginan de 40 en 40.
        foreach (var hoja in calamity.Children.SelectMany(n => n.Children.Count == 0 ? [n] : n.Children))
            Assert.True(hoja.ItemIdsOrdered.Count <= 40);
    }

    [Fact]
    public void ArbolDeBuffs_SinCatalogosDeCalamity_SeQuedaEnLasSeisCarpetasMasElIndice()
    {
        if (LoadBuffCatalogs() is not { } c) return;

        var roots = BuffTreeBuilder.BuildBuffTree(c.Vanilla, calamityBuffs: null, calamityItems: null, FakeBuffIcon, FakeItemIcon);

        Assert.Equal(7, roots.Count);
        Assert.DoesNotContain(roots, n => n.FullPath == "Calamity");
    }

    [Fact]
    public void EtiquetasDeCategoriaDeBuffsDeCalamity_TraducenLoRealYDejanIntactoLoDesconocido()
    {
        Assert.Equal("Invocación", BuffTreeBuilder.CalamityBuffCategoryLabel("Summon"));
        Assert.Equal("Daño continuo", BuffTreeBuilder.CalamityBuffCategoryLabel("DamageOverTime"));
        Assert.Equal("CategoriaNueva", BuffTreeBuilder.CalamityBuffCategoryLabel("CategoriaNueva"));
    }
}
