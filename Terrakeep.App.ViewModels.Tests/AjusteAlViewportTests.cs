using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Terrakeep.App.Controls;

namespace Terrakeep.App.ViewModels.Tests;

// FASE C del responsive global (PDF "Arreglo familia keep", bloque 2 - s8/s13/s14/s28): el nuevo
// mecanismo que sustituye al ScrollViewer propio de ContainerCompactTemplate en Inventario/
// Almacenes - SlotGridPanel.DeficitAlto + Controls/AjusteAlViewport. Headless, mismo patron STA que
// SlotGridPanelAdaptiveColumnsTests (FASE A): Measure() funciona sin ventana real.
//
// Geometria de prueba = la de una pagina real reducida: una cabecera fija de 50px (DockPanel.Top) y
// una rejilla 10x5 (50 celdas, MinCell 40, MaxCell 90, Gap 4) en 600px de ancho; el decorador se
// mide con alto INFINITO, igual que dentro del ScrollViewer de la pagina, y el alto real del
// viewport le llega por AltoViewport.
public sealed class AjusteAlViewportTests
{
    private static T EnHiloSta<T>(Func<T> accion)
    {
        T resultado = default!;
        Exception? error = null;
        var hilo = new Thread(() =>
        {
            try { resultado = accion(); }
            catch (Exception ex) { error = ex; }
        });
        hilo.SetApartmentState(ApartmentState.STA);
        hilo.Start();
        hilo.Join();
        if (error != null) throw error;
        return resultado;
    }

    private static SlotGridPanel Rejilla()
    {
        var g = new SlotGridPanel { Columns = 10, MinCell = 40, MaxCell = 90, Gap = 4 };
        for (int i = 0; i < 50; i++) g.Children.Add(new Border());
        return g;
    }

    private static (Size deseado, double celda, double deficitFinal) MedirPagina(double altoViewport)
        => EnHiloSta(() =>
        {
            var rejilla = Rejilla();
            var dock = new DockPanel { LastChildFill = true };
            var cabecera = new Border { Height = 50 };
            DockPanel.SetDock(cabecera, Dock.Top);
            dock.Children.Add(cabecera);
            dock.Children.Add(rejilla);
            var ajuste = new AjusteAlViewport { AltoViewport = altoViewport, Child = dock };
            ajuste.Measure(new Size(600, double.PositiveInfinity));
            // Celda real = (alto deseado de la rejilla - 4 huecos de 4px) / 5 filas.
            return (ajuste.DesiredSize, (rejilla.DesiredSize.Height - 16) / 5, rejilla.DeficitAlto);
        });

    // Cabe: la pagina ocupa exactamente el viewport (300px) y la celda sale del ALTO restante
    // ((300-50-4*4)/5 = 46,8px), no del ancho ni congelada - sin scroll.
    [Fact]
    public void SiCabeLaRejillaSeAjustaAlAltoRealRestanteSinScroll()
    {
        var (deseado, celda, deficit) = MedirPagina(300);
        Assert.Equal(300, deseado.Height, precision: 1);
        Assert.Equal(46.8, celda, precision: 1);
        Assert.Equal(0, deficit, precision: 3);
    }

    // No cabe ni a MinCell: la pagina crece EXACTAMENTE lo que falta (50 + 5*40 + 4*4 = 266 > 200)
    // para que el scroll owner de la pagina lo desplace, en vez de recortar la ultima fila.
    [Fact]
    public void SiNoCabeNiAMinCellLaPaginaCreceLoJustoParaElScrollOwner()
    {
        var (deseado, celda, deficit) = MedirPagina(200);
        Assert.Equal(266, deseado.Height, precision: 1);
        Assert.Equal(40, celda, precision: 3);
        Assert.Equal(0, deficit, precision: 3); // tras la segunda pasada ya no falta nada
    }

    // Viewport enorme: la celda topa en MaxCell y la pagina mide solo su contenido (sin estirarse).
    [Fact]
    public void ConViewportGrandeLaCeldaTopaEnMaxCell()
    {
        var (deseado, celda, _) = MedirPagina(2000);
        // fromW = (600-36)/10 = 56,4 limita antes que MaxCell=90 en este ancho.
        Assert.Equal(56.4, celda, precision: 1);
        Assert.Equal(50 + 5 * 56.4 + 16, deseado.Height, precision: 1);
    }

    // SlotGridPanel informa del deficit solo con alto finito; con alto infinito (Equipamiento, dentro
    // del ScrollViewer de su pagina sin decorador) sigue siendo 0, comportamiento de siempre.
    [Fact]
    public void DeficitAltoSoloConAltoFinito()
    {
        var (finito, infinito) = EnHiloSta(() =>
        {
            var a = Rejilla(); a.Measure(new Size(600, 100));
            var b = Rejilla(); b.Measure(new Size(600, double.PositiveInfinity));
            return (a.DeficitAlto, b.DeficitAlto);
        });
        Assert.Equal(5 * 40 + 16 - 100, finito, precision: 3);
        Assert.Equal(0, infinito, precision: 3);
    }

    // H-C1 (revisor de la FASE C): primera medida con el MinCell POR DEFECTO de SlotGridPanel (44, el
    // MultiBinding real aun no resuelto) -> no cabe y el decorador infla la restriccion de la rejilla;
    // cuando MinCell pasa a 40, WPF re-mide SOLO la rejilla con esa restriccion inflada. Sin el aviso
    // (evento MedidaConAltoFinito) la pagina se quedaba en 50+5*44+16=286 para un viewport de 266.
    // Con el aviso el decorador reparte de nuevo desde el alto real: 266, celda 40, sin deficit.
    [Fact]
    public void SiLaRejillaSeReMidePorSuCuentaElAjusteVuelveAlAltoReal()
    {
        var (antes, despues, celda) = EnHiloSta(() =>
        {
            var rejilla = new SlotGridPanel { Columns = 10, MinCell = 44, MaxCell = 90, Gap = 4 };
            for (int i = 0; i < 50; i++) rejilla.Children.Add(new Border());
            var dock = new DockPanel { LastChildFill = true };
            var cabecera = new Border { Height = 50 };
            DockPanel.SetDock(cabecera, Dock.Top);
            dock.Children.Add(cabecera);
            dock.Children.Add(rejilla);
            var ajuste = new AjusteAlViewport { AltoViewport = 266, Child = dock };
            ajuste.Measure(new Size(600, double.PositiveInfinity));
            double a = ajuste.DesiredSize.Height;               // 50 + 5*44 + 16 = 286 (no cabe a 44)
            rejilla.MinCell = 40;                                // "el binding resuelve"
            ajuste.UpdateLayout();                               // LayoutManager real: re-mide la rejilla sola
            ajuste.Measure(new Size(600, double.PositiveInfinity));
            return (a, ajuste.DesiredSize.Height, (rejilla.DesiredSize.Height - 16) / 5);
        });
        Assert.Equal(286, antes, precision: 1);
        Assert.Equal(266, despues, precision: 1);
        Assert.Equal(40, celda, precision: 1);
    }

    // Sin viewport medido todavia (AltoViewport=0, primer layout): medida natural, sin romper nada.
    [Fact]
    public void SinViewportTodaviaMideNatural()
    {
        var (deseado, _, _) = MedirPagina(0);
        Assert.True(deseado.Height > 0);
    }
}
