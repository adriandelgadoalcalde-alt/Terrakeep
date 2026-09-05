using TerrasavrNative.Core.Data;
using Xunit;

namespace TerrasavrNative.Core.Tests.Data;

// WS2 de TerrakeepMod (6-sep-2026): el arbol de carpetas de la Libreria (objetos y buffs) vivia
// en TerrasavrNative.App/Services y por tanto no lo cubria NINGUN test de Core. Al moverlo aqui
// (para que el mod de tModLoader pueda reutilizarlo sin WPF de por medio) se cubre de verdad
// contra los mismos JSON reales que carga la app, con el mismo patron que el resto de pruebas
// de humo de esta carpeta (si el asset real no esta, el test se salta en vez de fallar en falso).
public class LibraryTreeBuilderTests
{
    private const string AppAssetsDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets";

    // Resolucion de icono de mentira - lo unico que se comprueba es que el arbol la USA (que la
    // dependencia de disco/WPF quedo de verdad fuera de Core, inyectada).
    private static string? FakeIcon(int id) => $"icono:{id}";

    private static (VanillaLibraryTreeCatalog Tree, LibraryLabelCatalog Labels, CalamityCatalog Calamity)? LoadItemCatalogs()
    {
        string tree = Path.Combine(AppAssetsDir, "vanilla_library_tree.json");
        string labels = Path.Combine(AppAssetsDir, "vanilla_library_labels_es.json");
        string calamity = Path.Combine(AppAssetsDir, "calamity", "catalog.json");
        if (!File.Exists(tree) || !File.Exists(labels) || !File.Exists(calamity)) return null;
        return (VanillaLibraryTreeCatalog.LoadFromFile(tree), LibraryLabelCatalog.LoadFromFile(labels), CalamityCatalog.LoadFromFile(calamity));
    }

    private static IEnumerable<CategoryTreeNodeData> Walk(IEnumerable<CategoryTreeNodeData> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in Walk(node.Children)) yield return child;
        }
    }

    [Fact]
    public void ArbolDeObjetosReal_TraeElArbolVanillaMasLaCarpetaDeCalamity()
    {
        if (LoadItemCatalogs() is not { } c) return;

        var roots = LibraryTreeBuilder.BuildItemTree(c.Tree, c.Labels, c.Calamity, FakeIcon);

        // Una raiz por nodo real del arbol de Terrasavr + exactamente una de Calamity, la ultima.
        Assert.Equal(c.Tree.RootNodes.Count + 1, roots.Count);
        var calamityRoot = roots[^1];
        Assert.Equal("Calamity (mod)", calamityRoot.Name);
        Assert.Equal("Calamity", calamityRoot.FullPath);
        Assert.NotEmpty(calamityRoot.Children);
        Assert.NotEmpty(calamityRoot.ItemIdsOrdered);
    }

    [Fact]
    public void ArbolDeObjetosReal_SinCatalogoDeCalamity_NoInventaEsaCarpeta()
    {
        if (LoadItemCatalogs() is not { } c) return;

        var roots = LibraryTreeBuilder.BuildItemTree(c.Tree, c.Labels, calamity: null, FakeIcon);

        Assert.Equal(c.Tree.RootNodes.Count, roots.Count);
        Assert.DoesNotContain(roots, n => n.FullPath == "Calamity");
    }

    [Fact]
    public void ArbolDeObjetosReal_UsaLaResolucionDeIconoInyectada()
    {
        if (LoadItemCatalogs() is not { } c) return;

        var roots = LibraryTreeBuilder.BuildItemTree(c.Tree, c.Labels, c.Calamity, FakeIcon);

        // Todo icono real que salga tiene que venir del Func inyectado, nunca de una ruta de
        // disco cocinada dentro de Core.
        var conIcono = Walk(roots).Where(n => n.IconPath != null).ToList();
        Assert.NotEmpty(conIcono);
        Assert.All(conIcono, n => Assert.StartsWith("icono:", n.IconPath!));
    }

    [Fact]
    public void ArbolDeObjetosReal_UnaCarpetaIntermedia_EsLaUnionOrdenadaYSinDuplicadosDeSusHijos()
    {
        if (LoadItemCatalogs() is not { } c) return;

        var roots = LibraryTreeBuilder.BuildItemTree(c.Tree, c.Labels, c.Calamity, FakeIcon);

        foreach (var node in Walk(roots).Where(n => n.Children.Count > 0))
        {
            // Sin duplicados, y el conjunto coincide exactamente con la lista ordenada.
            Assert.Equal(node.ItemIdsOrdered.Count, node.ItemIdsOrdered.Distinct().Count());
            Assert.Equal(node.ItemIdsOrdered.Count, node.ItemIdSet.Count);
            // Orden real: los ids del primer hijo aparecen antes que cualquier id exclusivo del
            // segundo (concatenacion real, no un reordenado por id).
            var esperado = new List<int>();
            var vistos = new HashSet<int>();
            foreach (var child in node.Children)
                foreach (int id in child.ItemIdsOrdered)
                    if (vistos.Add(id)) esperado.Add(id);
            Assert.Equal(esperado, node.ItemIdsOrdered);
        }
    }

    [Fact]
    public void ArbolDeObjetosReal_NingunaHojaPasaDeLas40EntradasPorPagina()
    {
        if (LoadItemCatalogs() is not { } c) return;

        var roots = LibraryTreeBuilder.BuildItemTree(c.Tree, c.Labels, c.Calamity, FakeIcon);

        // Solo dentro de "Calamity (mod)": las hojas vanilla vienen YA paginadas del propio
        // Terrasavr real (y alguna hoja curada suya pasa de 40 a proposito), las de Calamity las
        // pagina este algoritmo.
        var calamityRoot = roots[^1];
        foreach (var hoja in Walk([calamityRoot]).Where(n => n.Children.Count == 0))
            Assert.True(hoja.ItemIdsOrdered.Count <= 40, $"{hoja.FullPath} tiene {hoja.ItemIdsOrdered.Count} entradas");
    }

    [Fact]
    public void EtiquetasDeCalamity_TraducenLaCategoriaRealYCaenAlInglesSinInventarNada()
    {
        Assert.Equal("Armas - Cuerpo a cuerpo", LibraryTreeBuilder.CalamityCategoryLabel("Weapons/Melee"));
        // Sin entrada directa: se traduce solo el segmento raiz, el nombre propio del mod se
        // deja tal cual (criterio ya establecido, no se inventa una traduccion).
        Assert.Equal("Armadura - Aerospec", LibraryTreeBuilder.CalamityCategoryLabel("Armor/Aerospec"));
        // Sin ninguna entrada: tal cual, nunca inventado.
        Assert.Equal("NoExisteEstaCategoria", LibraryTreeBuilder.CalamityCategoryLabel("NoExisteEstaCategoria"));
    }
}
