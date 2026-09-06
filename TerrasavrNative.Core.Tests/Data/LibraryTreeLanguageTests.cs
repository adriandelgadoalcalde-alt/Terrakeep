using TerrasavrNative.Core.Data;
using Xunit;

namespace TerrasavrNative.Core.Tests.Data;

// Ronda de idioma del 6-sep-2026. Las carpetas del arbol de la Libreria (objetos, Investigacion y
// buffs) solo existian en español - se veian igual con la app en ingles, uno de los puntos que
// reporto el usuario por su nombre. Estas pruebas fijan que el arbol trae AHORA los dos nombres.
public class LibraryTreeLanguageTests
{
    private const string AppAssetsDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets";

    private static string? FakeIcon(int id) => $"icono:{id}";

    private static IEnumerable<CategoryTreeNodeData> Recorrer(IEnumerable<CategoryTreeNodeData> nodos)
    {
        foreach (var n in nodos)
        {
            yield return n;
            foreach (var hijo in Recorrer(n.Children)) yield return hijo;
        }
    }

    // Mismo patron que el resto de pruebas de humo de esta carpeta: si el asset real no esta, se
    // salta en vez de fallar en falso.
    private static List<CategoryTreeNodeData>? ArbolObjetos()
    {
        string tree = Path.Combine(AppAssetsDir, "vanilla_library_tree.json");
        string labels = Path.Combine(AppAssetsDir, "vanilla_library_labels_es.json");
        string calamity = Path.Combine(AppAssetsDir, "calamity", "catalog.json");
        if (!File.Exists(tree) || !File.Exists(labels) || !File.Exists(calamity)) return null;
        return LibraryTreeBuilder.BuildItemTree(
            VanillaLibraryTreeCatalog.LoadFromFile(tree),
            LibraryLabelCatalog.LoadFromFile(labels),
            CalamityCatalog.LoadFromFile(calamity),
            FakeIcon);
    }

    [Fact]
    public void ArbolDeObjetos_TodaCarpetaTieneNombreIngles()
    {
        var arbol = ArbolObjetos();
        if (arbol == null) return;
        var sinIngles = Recorrer(arbol).Where(n => string.IsNullOrWhiteSpace(n.NameEn)).Select(n => n.FullPath).ToList();
        Assert.True(sinIngles.Count == 0, "carpetas sin nombre ingles: " + string.Join(", ", sinIngles.Take(10)));
    }

    // El caso concreto que salio en el barrido real con la app en ingles: la carpeta raiz
    // "Materials" se veia "Materiales", y "Pre-Hardmode" se veia "Pre-Modo Dificil".
    [Fact]
    public void ArbolDeObjetos_LasRaicesUsanElNombreInglesReal()
    {
        var arbol = ArbolObjetos();
        if (arbol == null) return;
        var materiales = arbol.FirstOrDefault(n => n.FullPath == "Materials");
        Assert.NotNull(materiales);
        Assert.Equal("Materiales", materiales!.Name);
        Assert.Equal("Materials", materiales.NameEn);
    }

    // "Calamity (mod)" es un nombre propio: identico en los dos idiomas, pero tiene que ESTAR
    // (si NameEn quedara null sus subcarpetas caerian al español).
    [Fact]
    public void ArbolDeObjetos_LaRaizDeCalamityTieneSubcarpetasEnIngles()
    {
        var arbol = ArbolObjetos();
        if (arbol == null) return;
        var calamity = arbol.FirstOrDefault(n => n.FullPath == "Calamity");
        Assert.NotNull(calamity);
        Assert.Equal("Calamity (mod)", calamity!.NameEn);
        var armas = calamity.Children.FirstOrDefault(n => n.FullPath.StartsWith("Calamity/Weapons"));
        Assert.NotNull(armas);
        Assert.StartsWith("Armas", armas!.Name);
        Assert.StartsWith("Weapons", armas.NameEn);
    }

    [Fact]
    public void CalamityCategoryLabelEn_SeparaElCamelCaseRealDelMod()
    {
        Assert.Equal("Weapons - Draedons Arsenal", LibraryTreeBuilder.CalamityCategoryLabelEn("Weapons/DraedonsArsenal"));
        Assert.Equal("Placeables - Sunken Sea", LibraryTreeBuilder.CalamityCategoryLabelEn("Placeables/SunkenSea"));
        Assert.Equal("Armor", LibraryTreeBuilder.CalamityCategoryLabelEn("Armor"));
    }

    [Fact]
    public void SepararCamelCase_NoRompeSiglasNiTextoYaSeparado()
    {
        Assert.Equal("Sunken Sea", LibraryTreeBuilder.SepararCamelCase("SunkenSea"));
        Assert.Equal("Ya separado", LibraryTreeBuilder.SepararCamelCase("Ya separado"));
        Assert.Equal("Armor", LibraryTreeBuilder.SepararCamelCase("Armor"));
    }

    [Fact]
    public void ArbolDeBuffs_TodaCarpetaTieneNombreIngles()
    {
        string buffs = Path.Combine(AppAssetsDir, "vanilla_buff_names.json");
        string desc = Path.Combine(AppAssetsDir, "vanilla_buff_descriptions.json");
        string buffsEs = Path.Combine(AppAssetsDir, "vanilla_buff_names_es.json");
        if (!File.Exists(buffs) || !File.Exists(desc) || !File.Exists(buffsEs)) return;
        var arbol = BuffTreeBuilder.BuildBuffTree(
            VanillaBuffCatalog.LoadFromFile(buffs, desc, buffsEs), null, null, FakeIcon, FakeIcon);
        var sinIngles = Recorrer(arbol).Where(n => string.IsNullOrWhiteSpace(n.NameEn)).Select(n => n.FullPath).ToList();
        Assert.True(sinIngles.Count == 0, "carpetas de buffs sin nombre ingles: " + string.Join(", ", sinIngles.Take(10)));
        var utilidad = arbol.First(n => n.FullPath == "Utilidad");
        Assert.StartsWith("Utilidad (", utilidad.Name);
        Assert.StartsWith("Utility (", utilidad.NameEn);
    }
}
