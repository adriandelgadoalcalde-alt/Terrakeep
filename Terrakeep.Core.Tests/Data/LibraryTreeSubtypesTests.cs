using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

// 8-sep-2026: la rama raiz "Categories" del arbol curado ya no se parte en "Page 1", "Page 2"...
// de 40 en 40 por orden de id, sino por el SUBTIPO REAL de cada objeto (espadas, lanzas, arcos,
// armas de fuego, picos, bloques, estandartes...), derivado del codigo decompilado del juego por
// scripts/extraer-subtipos-libreria-vanilla.py y aplicado por
// scripts/extraer-arbol-libreria-vanilla.js.
//
// Estas pruebas van contra el JSON REAL que carga la app (mismo patron que
// LibraryTreeBuilderTests: si el asset no esta, se saltan en vez de fallar en falso). Lo que
// comprueban no es "que haya N paginas" -eso cambiaria con cada version del juego- sino las dos
// cosas que de verdad tienen que cumplirse: que ninguna carpeta reorganizada siga partida a
// ciegas, y que objetos CONOCIDOS caigan en su subtipo real.
public class LibraryTreeSubtypesTests
{
    private const string AppAssetsDir =
        @"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets";

    private static (VanillaLibraryTreeCatalog Tree, LibraryLabelCatalog Labels)? Cargar()
    {
        string tree = Path.Combine(AppAssetsDir, "vanilla_library_tree.json");
        string labels = Path.Combine(AppAssetsDir, "vanilla_library_labels_es.json");
        if (!File.Exists(tree) || !File.Exists(labels)) return null;
        return (VanillaLibraryTreeCatalog.LoadFromFile(tree), LibraryLabelCatalog.LoadFromFile(labels));
    }

    private static List<CategoryTreeNodeData>? Raices()
    {
        if (Cargar() is not { } c) return null;
        return LibraryTreeBuilder.BuildItemTree(c.Tree, c.Labels, calamity: null, id => $"icono:{id}");
    }

    // Busca por el nombre INGLES (NameEn), que es la clave estable del arbol, ignorando el "(N)".
    private static CategoryTreeNodeData Buscar(IEnumerable<CategoryTreeNodeData> nodos, params string[] ruta)
    {
        var actuales = nodos;
        CategoryTreeNodeData? nodo = null;
        foreach (string paso in ruta)
        {
            nodo = actuales.FirstOrDefault(n => SinRecuento(n.NameEn ?? n.Name) == paso);
            Assert.True(nodo != null, $"No existe la carpeta real '{paso}' en '{string.Join(" > ", ruta)}'");
            actuales = nodo!.Children;
        }
        return nodo!;
    }

    private static string SinRecuento(string nombre)
    {
        int abre = nombre.LastIndexOf(" (", StringComparison.Ordinal);
        return abre > 0 && nombre.EndsWith(')') ? nombre[..abre] : nombre;
    }

    private static IEnumerable<CategoryTreeNodeData> Recorrer(IEnumerable<CategoryTreeNodeData> nodos)
    {
        foreach (var n in nodos)
        {
            yield return n;
            foreach (var h in Recorrer(n.Children)) yield return h;
        }
    }

    [Theory]
    // (carpeta reorganizada, subcarpeta esperada, objeto conocido que tiene que estar dentro)
    [InlineData("Weapons/Melee damage", "Swords", 368)]        // Excalibur
    [InlineData("Weapons/Melee damage", "Swords", 4956)]       // La Cenit
    [InlineData("Weapons/Melee damage", "Yoyos", 3389)]        // Terrarian
    [InlineData("Weapons/Melee damage", "Spears", 274)]        // Lanza de la oscuridad
    [InlineData("Weapons/Melee damage", "Flails", 162)]        // Flagelo con bola
    [InlineData("Weapons/Melee damage", "Boomerangs", 55)]     // Bumerán encantado
    [InlineData("Weapons/Melee damage", "Pickaxes", 3509)]     // Pico de cobre
    [InlineData("Weapons/Melee damage", "Drills", 385)]        // Taladro de cobalto
    [InlineData("Weapons/Melee damage", "Chainsaws", 383)]     // Motosierra de cobalto
    [InlineData("Weapons/Ranged damage", "Bows", 39)]          // Arco de madera
    [InlineData("Weapons/Ranged damage", "Guns", 98)]          // Minishark
    [InlineData("Weapons/Ranged damage", "Guns", 1553)]        // S.D.M.G.
    [InlineData("Weapons/Ranged damage", "Launchers", 759)]    // Lanzacohetes
    [InlineData("Weapons/Ranged damage", "Arrows", 40)]        // Flecha de madera
    [InlineData("Weapons/Ranged damage", "Bullets", 97)]       // Bala de mosquete
    [InlineData("Weapons/Ranged damage", "Thrown weapons", 168)] // Granada
    [InlineData("Equipable/Armor", "Head", 2763)]              // Yelmo de bengala solar
    [InlineData("Equipable/Accessories", "Wings", 823)]        // Alas de polluelo
    [InlineData("Equipable/Dyes", "Hair dyes", 1977)]          // Tinte de pelo real
    [InlineData("Tools/Pickaxes", "Drills", 385)]
    [InlineData("Placeable", "Blocks", 2)]                     // Bloque de tierra
    [InlineData("Placeable", "Banners", 3390)]                 // un estandarte real
    public void ObjetoConocido_CaeEnSuSubtipoReal(string carpeta, string subcarpeta, int id)
    {
        if (Raices() is not { } raices) return;

        var ruta = new List<string> { "Categories" };
        ruta.AddRange(carpeta.Split('/'));
        var padre = Buscar(raices, [.. ruta]);
        var hijo = padre.Children.FirstOrDefault(h => SinRecuento(h.NameEn ?? h.Name) == subcarpeta);
        Assert.True(hijo != null, $"'{carpeta}' no tiene la subcarpeta '{subcarpeta}'");
        Assert.Contains(id, hijo!.ItemIdSet);
    }

    [Fact]
    public void CarpetasReorganizadas_YaNoTienenPaginasSueltasColgandoDirectamente()
    {
        if (Raices() is not { } raices) return;

        string[][] rutas =
        [
            ["Categories", "Weapons", "Melee damage"],
            ["Categories", "Weapons", "Ranged damage"],
            ["Categories", "Equipable", "Armor"],
            ["Categories", "Equipable", "Accessories"],
            ["Categories", "Equipable", "Vanity"],
            ["Categories", "Equipable", "Head slot"],
            ["Categories", "Equipable", "Dyes"],
            ["Categories", "Tools", "Pickaxes"],
            ["Categories", "Placeable"],
        ];
        foreach (var ruta in rutas)
        {
            var nodo = Buscar(raices, ruta);
            Assert.NotEmpty(nodo.Children);
            foreach (var hijo in nodo.Children)
            {
                string en = hijo.NameEn ?? hijo.Name;
                Assert.False(en.StartsWith("Page ", StringComparison.Ordinal),
                    $"{string.Join(" > ", ruta)} sigue colgando '{en}' directamente");
                // Toda subcarpeta nueva lleva su recuento, como el resto del arbol curado.
                Assert.EndsWith(")", en);
            }
        }
    }

    [Fact]
    public void CarpetasReorganizadas_NoPierdenNiDuplicanNingunObjeto()
    {
        if (Raices() is not { } raices) return;

        var categorias = Buscar(raices, "Categories");
        foreach (var nodo in Recorrer([categorias]).Where(n => n.Children.Count > 0))
        {
            var esperado = new List<int>();
            var vistos = new HashSet<int>();
            foreach (var hijo in nodo.Children)
                foreach (int id in hijo.ItemIdsOrdered)
                    if (vistos.Add(id)) esperado.Add(id);
            Assert.Equal(esperado, nodo.ItemIdsOrdered);
        }

        // Y el recuento que se ve en el rotulo de las dos carpetas que reporto el usuario sigue
        // siendo el real (ni se pierde ni se duplica nada al reagrupar).
        Assert.Equal(316, Buscar(raices, "Categories", "Weapons", "Melee damage").ItemIdsOrdered.Count);
        Assert.Equal(180, Buscar(raices, "Categories", "Weapons", "Ranged damage").ItemIdsOrdered.Count);
        Assert.Equal(3219, Buscar(raices, "Categories", "Placeable").ItemIdsOrdered.Count);
    }

    [Fact]
    public void LasCarpetasNuevas_TienenSuTraduccionRealAlEspanol()
    {
        if (Cargar() is not { } c) return;

        // La traduccion va por PLANTILLA ("Swords ($1)"), igual que el resto del arbol curado.
        Assert.Equal("Espadas (111)", c.Labels.Translate("Swords (111)"));
        Assert.Equal("Arcos (41)", c.Labels.Translate("Bows (41)"));
        Assert.Equal("Bloques (282)", c.Labels.Translate("Blocks (282)"));
        Assert.Equal("Otros colocables (7)", c.Labels.Translate("Other placeables (7)"));
        // Y ninguna subcarpeta NUEVA se queda en ingles por olvido. Solo se miran las hijas de
        // las carpetas reorganizadas: el resto del arbol es el de Terrasavr tal cual, y ahi hay
        // nombres que el propio Terrasavr nunca tradujo ("Equipable"), que no es cosa de aqui.
        if (Raices() is not { } raices) return;
        string[][] rutas =
        [
            ["Categories", "Weapons", "Melee damage"],
            ["Categories", "Weapons", "Ranged damage"],
            ["Categories", "Equipable", "Armor"],
            ["Categories", "Equipable", "Accessories"],
            ["Categories", "Equipable", "Vanity"],
            ["Categories", "Equipable", "Head slot"],
            ["Categories", "Equipable", "Body slot"],
            ["Categories", "Equipable", "Leg slot"],
            ["Categories", "Equipable", "Dyes"],
            ["Categories", "Tools", "Pickaxes"],
            ["Categories", "Tools", "Axes"],
            ["Categories", "Tools", "Hammers"],
            ["Categories", "Placeable"],
        ];
        foreach (var ruta in rutas)
            foreach (var hijo in Buscar(raices, ruta).Children)
            {
                string en = hijo.NameEn ?? hijo.Name;
                // "Pianos" se escribe igual en los dos idiomas; es la unica excepcion real.
                Assert.True(hijo.Name != en || SinRecuento(en) == "Pianos",
                    $"La carpeta '{en}' no tiene traduccion al español");
            }
    }
}
