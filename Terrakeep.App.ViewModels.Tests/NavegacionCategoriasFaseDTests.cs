using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Terrakeep.App.Controls;

namespace Terrakeep.App.ViewModels.Tests;

// FASE D del responsive global (28-sep-2026): las dos piezas de logica que sustituyen al arbol lateral de
// categorias con ScrollViewer propio y a las columnas fijas de los resultados, en la familia "catalogo con
// categorias" (Libreria de objetos, Libreria de buffs, Investigacion).
public sealed class NavegacionCategoriasFaseDTests
{
    // --- Ruta de la carpeta elegida (CatalogBrowserViewModel.RutaCategoria/MigasCategoria/NodoSubcategorias),
    //     una sola implementacion para las 3 superficies. Arbol REAL de la Libreria (MainViewModel sin personaje).

    [Fact]
    public void ElegirUnaHojaMarcaSuRutaYElDesplegableOfreceSusHermanas()
    {
        var lib = new MainViewModel().Library;
        var raiz = lib.RootCategories.First(r => r.Children.Any(h => h.Children.Count > 0));
        var nivel2 = raiz.Children.First(h => h.Children.Count > 0);
        var hoja = nivel2.Children.First(h => h.Children.Count == 0);

        lib.SelectCategoryCommand.Execute(hoja);

        Assert.Same(hoja, lib.SelectedCategory);
        Assert.Equal([raiz, nivel2, hoja], lib.RutaCategoria.ToArray());
        Assert.Equal([nivel2, hoja], lib.MigasCategoria.ToArray()); // la raiz ya se ve resaltada en la fila principal
        Assert.Same(nivel2, lib.NodoSubcategorias);               // hoja sin hijos -> hermanas (hijos del padre)
        Assert.True(lib.HaySubnavegacion);
        Assert.True(raiz.IsInSelectedPath);
        Assert.True(nivel2.IsInSelectedPath);
        Assert.False(hoja.IsInSelectedPath);                       // la elegida se marca con IsSelected, no como ruta
        Assert.True(hoja.IsSelected);
    }

    [Fact]
    public void ElegirUnaRaizConHijosOfreceSusHijosSinMigas()
    {
        var lib = new MainViewModel().Library;
        var raiz = lib.RootCategories.First(r => r.Children.Count > 0);

        lib.SelectCategoryCommand.Execute(raiz);

        Assert.Equal([raiz], lib.RutaCategoria.ToArray());
        Assert.Empty(lib.MigasCategoria);
        Assert.Same(raiz, lib.NodoSubcategorias);
        Assert.True(lib.HaySubnavegacion);
    }

    [Fact]
    public void VerTodoLimpiaLaRutaYLasMarcas()
    {
        var lib = new MainViewModel().Library;
        var raiz = lib.RootCategories.First(r => r.Children.Count > 0);
        var hija = raiz.Children[0];
        lib.SelectCategoryCommand.Execute(hija);

        lib.ClearCategoryCommand.Execute(null);

        Assert.Null(lib.SelectedCategory);
        Assert.Empty(lib.RutaCategoria);
        Assert.Empty(lib.MigasCategoria);
        Assert.Null(lib.NodoSubcategorias);
        Assert.False(lib.HaySubnavegacion);
        Assert.False(raiz.IsInSelectedPath);
        Assert.True(lib.ShowRootCategoryCards);
    }

    [Fact]
    public void LaRutaFuncionaIgualEnLaLibreriaDeBuffsYEnInvestigacion()
    {
        var vm = new MainViewModel();
        foreach (var (raices, seleccionar, ruta) in new (System.Collections.Generic.IList<CategoryNodeViewModel>, System.Windows.Input.ICommand, System.Collections.Generic.IList<CategoryNodeViewModel>)[]
                 {
                     (vm.BuffLibrary.RootCategories, vm.BuffLibrary.SelectCategoryCommand, vm.BuffLibrary.RutaCategoria),
                     (vm.Research.RootCategories, vm.Research.SelectCategoryCommand, vm.Research.RutaCategoria),
                 })
        {
            var raiz = raices.FirstOrDefault(r => r.Children.Count > 0) ?? raices[0];
            var objetivo = raiz.Children.FirstOrDefault() ?? raiz;
            seleccionar.Execute(objetivo);
            Assert.Same(raiz, ruta[0]);
            Assert.Same(objetivo, ruta[^1]);
        }
    }

    // --- SlotGridPanel.PreferirCeldaGrande (con AdaptiveColumns). Headless, mismo patron STA que
    //     SlotGridPanelAdaptiveColumnsTests.

    private static T EnHiloSta<T>(Func<T> accion)
    {
        T resultado = default!;
        Exception? error = null;
        var hilo = new Thread(() => { try { resultado = accion(); } catch (Exception ex) { error = ex; } });
        hilo.SetApartmentState(ApartmentState.STA);
        hilo.Start();
        hilo.Join();
        if (error != null) throw error;
        return resultado;
    }

    private static (int cols, double celda) Medir(int n, double ancho, double alto, bool preferirGrande)
        => EnHiloSta(() =>
        {
            var g = new SlotGridPanel { Columns = 60, AdaptiveColumns = true, PreferirCeldaGrande = preferirGrande, MinCell = 40, MaxCell = 90, Gap = 4, ReferenceColumns = 10, AvailableHeight = alto };
            for (int i = 0; i < n; i++) g.Children.Add(new Border());
            g.Measure(new Size(ancho, double.PositiveInfinity));
            g.Arrange(new Rect(0, 0, ancho, g.DesiredSize.Height));
            var xs = g.Children.OfType<Border>().Select(b => Math.Round(b.TranslatePoint(new Point(0, 0), g).X)).Distinct().Count();
            return (xs, g.Children.OfType<Border>().First().RenderSize.Width);
        });

    // Caso medido en la Libreria a 2560x1440 (100 resultados, ~2139x584): AdaptiveColumns a secas da 48
    // columnas de ~40,6px en 3 filas; con PreferirCeldaGrande, todo sigue cabiendo en el alto pero con la
    // celda maxima (90px).
    [Fact]
    public void SiTodoCabeEligeLasColumnasDeLaCeldaMasGrande()
    {
        var (colsA, celdaA) = Medir(100, 2139, 584, preferirGrande: false);
        var (colsB, celdaB) = Medir(100, 2139, 584, preferirGrande: true);
        Assert.True(colsA >= 40); // 48 columnas a MinCell en 2139px
        Assert.True(celdaA < 50);
        Assert.Equal(90, celdaB, 1);
        Assert.True(colsB < colsA);
        int filas = (int)Math.Ceiling(100 / (double)colsB);
        Assert.True(filas * celdaB + (filas - 1) * 4 <= 584 + 0.5); // cabe sin scroll
    }

    // Si ni con el maximo de columnas cabe a MinCell (muchos resultados en un viewport bajo), se queda en el
    // maximo de columnas a MinCell: llenar el ancho y dejar el resto al scroll owner (s14 paso 8).
    [Fact]
    public void SiNoCabeNiAMinCellSeQuedaEnElMaximoDeColumnas()
    {
        var (colsA, celdaA) = Medir(100, 635, 58, preferirGrande: false);
        var (colsB, celdaB) = Medir(100, 635, 58, preferirGrande: true);
        Assert.Equal(colsA, colsB);
        Assert.Equal(14, colsB);
        Assert.Equal(40, celdaB, 1);
    }
}
