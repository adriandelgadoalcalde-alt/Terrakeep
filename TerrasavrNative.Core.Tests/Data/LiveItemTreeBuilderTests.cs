using TerrasavrNative.Core.Data;
using Xunit;

namespace TerrasavrNative.Core.Tests.Data;

// WS2 de TerrakeepMod (6-sep-2026): base de datos pura para la Libreria DEL MOD, que descubrira
// el contenido en vivo desde ContentSamples.ItemsByType (esa extraccion vive en el mod, no aqui
// - ver el comentario de LiveItemTreeBuilder). Estos tests usan objetos de mentira a proposito:
// lo que se comprueba es el agrupado/paginado/orden, no ningun catalogo real.
public class LiveItemTreeBuilderTests
{
    private static string? FakeIcon(int id) => $"icono:{id}";

    private static LiveItemInfo Item(int id, string mod, string cat) => new(id, $"Objeto {id}", mod, cat);

    [Fact]
    public void UnaCarpetaRaizPorMod_ConElJuegoBaseSiemprePrimero()
    {
        var arbol = LiveItemTreeBuilder.BuildTree(
        [
            Item(1, "ZetaMod", "Weapons"),
            Item(2, "AlphaMod", "Weapons"),
            Item(3, "Terraria", "Weapons"),
        ], FakeIcon);

        Assert.Equal(["Terraria", "AlphaMod", "ZetaMod"], arbol.Select(n => n.FullPath).ToArray());
        Assert.Equal("Terraria (1)", arbol[0].Name);
    }

    [Fact]
    public void LasCategoriasConBarraSeAgrupanPorSuSegmentoRaiz_IgualQueEnLaAppDeEscritorio()
    {
        var arbol = LiveItemTreeBuilder.BuildTree(
        [
            Item(10, "Terraria", "Weapons/Melee"),
            Item(11, "Terraria", "Weapons/Ranged"),
            Item(12, "Terraria", "Materials"),
        ], FakeIcon);

        var raiz = Assert.Single(arbol);
        // "Weapons" agrupa dos categorias -> carpeta intermedia; "Materials" es unica -> cuelga
        // directa (mismo criterio real que "Calamity (mod)" en la app).
        Assert.Equal(["Terraria/Materials", "Terraria/Weapons"], raiz.Children.Select(n => n.FullPath).ToArray());
        Assert.Equal(["Terraria/Weapons/Melee", "Terraria/Weapons/Ranged"], raiz.Children[1].Children.Select(n => n.FullPath).ToArray());
        Assert.Equal("Weapons (2)", raiz.Children[1].Name);
    }

    [Fact]
    public void UnaHojaDeMasDe40ObjetosSeParteEnPaginas()
    {
        var items = Enumerable.Range(1, 95).Select(i => Item(i, "Terraria", "Materials")).ToList();

        var raiz = Assert.Single(LiveItemTreeBuilder.BuildTree(items, FakeIcon));

        var materiales = Assert.Single(raiz.Children);
        Assert.Equal([40, 40, 15], materiales.Children.Select(p => p.ItemIdsOrdered.Count).ToArray());
        Assert.Equal("Página 1", materiales.Children[0].Name);
        Assert.Equal("Terraria/Materials/Page3", materiales.Children[2].FullPath);
        // La carpeta sigue conociendo sus 95 ids, en el orden real de descubrimiento.
        Assert.Equal(Enumerable.Range(1, 95), materiales.ItemIdsOrdered);
    }

    [Fact]
    public void ElOrdenDeDescubrimientoSeRespetaYUnIdRepetidoSoloEntraUnaVez()
    {
        var arbol = LiveItemTreeBuilder.BuildTree(
        [
            Item(7, "Terraria", "Materials"),
            Item(3, "Terraria", "Materials"),
            Item(7, "Terraria", "Weapons"), // repetido: se queda la primera aparicion real
        ], FakeIcon);

        var raiz = Assert.Single(arbol);
        var materiales = Assert.Single(raiz.Children);
        Assert.Equal([7, 3], materiales.ItemIdsOrdered);
        Assert.Equal("Terraria/Materials", materiales.FullPath);
    }

    [Fact]
    public void SinCategoriaReal_CaeEnOtros_YNuncaSeInventaUnNombre()
    {
        var arbol = LiveItemTreeBuilder.BuildTree([Item(5, "Terraria", "")], FakeIcon);

        var otros = Assert.Single(Assert.Single(arbol).Children);
        Assert.Equal("Terraria/Otros", otros.FullPath);
        Assert.Equal("Otros (1)", otros.Name);
    }

    [Fact]
    public void LaTraduccionDeCategoriaYDePaginaSonInyectables()
    {
        var items = Enumerable.Range(1, 41).Select(i => Item(i, "Terraria", "Weapons")).ToList();

        var raiz = Assert.Single(LiveItemTreeBuilder.BuildTree(
            items, FakeIcon, categoryLabel: c => $"<{c}>", pageLabel: n => $"Page {n}"));

        var armas = Assert.Single(raiz.Children);
        Assert.Equal("<Weapons> (41)", armas.Name);
        Assert.Equal("Page 1", armas.Children[0].Name);
    }

    [Fact]
    public void ElIconoDeCadaCarpetaSaleDelResolutorInyectado_ConElPrimerObjetoRealDeLaCategoria()
    {
        var arbol = LiveItemTreeBuilder.BuildTree(
        [
            Item(21, "Terraria", "Weapons"),
            Item(22, "Terraria", "Ammo"),
        ], FakeIcon);

        var raiz = Assert.Single(arbol);
        // Primera categoria en orden ordinal ("Ammo") -> su primer objeto real (22).
        Assert.Equal("icono:22", raiz.IconPath);
        Assert.Equal("icono:22", raiz.Children[0].IconPath);
        Assert.Equal("icono:21", raiz.Children[1].IconPath);
    }

    [Fact]
    public void ElArbolEnVivoProduceExactamenteElMismoTipoDeNodoQueLaAppDeEscritorio()
    {
        var raiz = Assert.Single(LiveItemTreeBuilder.BuildTree([Item(1, "Terraria", "Weapons")], FakeIcon));

        // Mismo record puro que consume la app WPF (CategoryNodeViewModel) - asi WS3 puede
        // reutilizar cualquier cosa ya escrita sobre CategoryTreeNodeData.
        Assert.IsType<CategoryTreeNodeData>(raiz);
        Assert.Equal(raiz.ItemIdsOrdered.Count, raiz.ItemIdSet.Count);
    }
}
