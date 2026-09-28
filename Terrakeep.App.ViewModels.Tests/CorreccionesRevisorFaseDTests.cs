using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Terrakeep.App.Controls;

namespace Terrakeep.App.ViewModels.Tests;

// Correcciones del revisor visual de la FASE D del responsive global (28-sep-2026): la logica sin ventana de
// D-01 (composicion de Editar y linea de ruta que nunca envuelve) y D-03 (subcategorias sin el prefijo
// repetido de su madre). La geometria real la mide LIBRARY_RESPONSIVE_SOLO en el arnes.
public sealed class CorreccionesRevisorFaseDTests
{
    // --- D-01/D-05: la regla de composicion de Editar por SizeClass (MainViewModel.IsEditarBarraCompleta) se RETIRO en
    //     la segunda revision visual (L-02): Editar vive siempre en la fila de contenido y lo que cambia es el selector
    //     de prefijo (en linea / desplegable) segun el alto real de la fila - decision de la vista, medida por
    //     LIBRARY_RESPONSIVE_SOLO (EDITAR-EN-LINEA, PREFIJO-DESPLEGABLE, LIBRERIA-ANCHO). Se comprueba aqui que la
    //     propiedad vieja no vuelve (negative acceptance). ---
    [Fact]
    public void LaBarraLateralDeEditarPorSizeClassYaNoExiste()
        => Assert.Null(typeof(MainViewModel).GetProperty("IsEditarBarraCompleta"));

    // --- D-03: nombre de una subcarpeta debajo de su madre. ---
    [Theory]
    [InlineData("Colocables - Abismo (9)", "Colocables (1047)", "Abismo (9)")]
    [InlineData("Colocables - Paredes - Estructuras de Draedon (8)", "Colocables (1047)", "Paredes - Estructuras de Draedon (8)")]
    [InlineData("Placeables - Sunken Sea (24)", "Placeables (1047)", "Sunken Sea (24)")]
    [InlineData("Colocables (71)", "Colocables (1047)", "Colocables (71)")]        // se llama igual que su madre: tal cual
    [InlineData("Espadas (111)", "Daño de Cuerpo a Cuerpo (316)", "Espadas (111)")] // sin prefijo: tal cual
    [InlineData("Materiales", null, "Materiales")]                                    // raiz
    public void QuitarPrefijoDeMadre(string nombre, string? madre, string esperado)
        => Assert.Equal(esperado, CategoryNodeViewModel.QuitarPrefijoDeMadre(nombre, madre));

    [Fact]
    public void LasSubcarpetasRealesDeColocablesDeCalamitySeVenSinElPrefijoDeSuMadre()
    {
        var lib = new MainViewModel().Library;
        var calamity = lib.RootCategories.FirstOrDefault(r => r.FullPath == "Calamity");
        if (calamity == null) return; // sin catalogo de Calamity en esta maquina
        var colocables = calamity.Children.FirstOrDefault(c => c.FullPath == "Calamity/Placeables");
        Assert.NotNull(colocables);
        Assert.True(colocables!.Children.Count > 10);
        string baseMadre = colocables.Name[..colocables.Name.LastIndexOf(" (", StringComparison.Ordinal)];
        Assert.All(colocables.Children, h =>
        {
            Assert.Same(colocables, h.Padre);
            Assert.False(h.NombreEnRuta.StartsWith(baseMadre + " - ", StringComparison.Ordinal), h.NombreEnRuta);
        });
        Assert.Contains(colocables.Children, h => h.NombreEnRuta != h.Name); // de verdad quita algo
    }

    // --- D-01: LineaRutaPanel nunca envuelve: pliega migas intermedias y luego recorta el resumen. ---
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

    private static (int plegadas, double alto, double anchoResumen, double derechaMax) MedirLinea(double ancho, int migas, double anchoMiga, double anchoResumen)
        => EnHiloSta(() =>
        {
            var p = new LineaRutaPanel();
            var elipsis = new Border { Width = 20, Height = 24 };
            LineaRutaPanel.SetRol(elipsis, RolLineaRuta.Elipsis);
            p.Children.Add(elipsis);
            for (int i = 0; i < migas; i++)
            {
                var m = new Border { Width = anchoMiga, Height = 24 };
                LineaRutaPanel.SetRol(m, RolLineaRuta.Miga);
                p.Children.Add(m);
            }
            var fijo = new Border { Width = 120, Height = 24 };
            p.Children.Add(fijo);
            var resumen = new TextBlock { Text = new string('x', (int)(anchoResumen / 6)), TextTrimming = TextTrimming.CharacterEllipsis };
            LineaRutaPanel.SetRol(resumen, RolLineaRuta.Resumen);
            p.Children.Add(resumen);
            p.Measure(new Size(ancho, double.PositiveInfinity));
            p.Arrange(new Rect(0, 0, ancho, p.DesiredSize.Height));
            double derecha = p.Children.OfType<FrameworkElement>().Where(c => c.RenderSize.Width > 0)
                .Max(c => c.TranslatePoint(new Point(c.RenderSize.Width, 0), p).X);
            return (p.MigasPlegadas, p.DesiredSize.Height, resumen.RenderSize.Width, derecha);
        });

    [Fact]
    public void SiTodoCabeNoPliegaNada()
    {
        var r = MedirLinea(1000, 3, 100, 120);
        Assert.Equal(0, r.plegadas);
        Assert.True(r.alto <= 24.5);
    }

    [Fact]
    public void SiNoCabePliegaLasMigasIntermediasYNuncaLaUltima()
    {
        // 4 migas de 150 + fijo 120 + resumen ~120 = 840 en 500px: se pliegan migas hasta caber.
        var r = MedirLinea(500, 4, 150, 120);
        Assert.InRange(r.plegadas, 1, 3);
        Assert.True(r.alto <= 24.5);      // UNA linea
        Assert.True(r.derechaMax <= 500.5); // nada se sale
    }

    [Fact]
    public void ConLaUltimaMigaSolaSinSitioElResumenCedeElAncho()
    {
        var r = MedirLinea(360, 3, 150, 400);
        Assert.Equal(2, r.plegadas);          // solo queda la ultima
        Assert.True(r.alto <= 24.5);
        Assert.True(r.derechaMax <= 360.5);
        Assert.True(r.anchoResumen < 400);    // recortado (con tooltip completo en la app)
    }
}
