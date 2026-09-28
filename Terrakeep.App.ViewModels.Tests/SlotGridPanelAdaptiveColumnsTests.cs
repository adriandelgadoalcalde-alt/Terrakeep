using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Terrakeep.App.Controls;

namespace Terrakeep.App.ViewModels.Tests;

// FASE A del responsive global (PDF "Arreglo familia keep", segundo bloque - "TERRAKEEP:
// RESPONSIVE GLOBAL, PAGINACION Y SCROLL COMO ULTIMO RECURSO"): infraestructura generica de
// columnas adaptativas dentro del propio SlotGridPanel (Controls/SlotGridPanel.cs),
// generalizando el patron que hasta ahora vivia SOLO fuera, en ChestInspectorColumnsConverter
// (Converters/DensityConverters.cs). NINGUNA pantalla real usa todavia AdaptiveColumns - eso es
// trabajo de fases posteriores, coordinado aparte (ver bitacora.md).
//
// Mismo patron que el arnes real T24-SLOTGRID (Terrakeep.App.Tests/Program.cs, auditoria de
// Opus Bloque 6): Panel.Measure() funciona standalone (sin arbol visual real, sin Window) porque
// MeasureOverride es matematica pura sobre InternalChildren/las DependencyProperty del propio
// panel - por eso este test SI puede vivir en xunit headless (Terrakeep.App.Tests es un arnes de
// UI Automation real con ventana, capturas y ~2min13s por pasada; esto es instantaneo). Unica
// diferencia real de entorno: el Main() de ese arnes lleva [STAThread] (necesario para
// FrameworkElement/Panel: dispara System.Windows.Input.InputManager en su constructor), y el
// hilo de trabajo por defecto de xunit es MTA - de ahi EnHiloSta, para construir y medir el
// panel en un hilo STA propio sin necesitar ningun paquete nuevo (autonomia tecnica, pero la
// solucion mas simple gana).
public sealed class SlotGridPanelAdaptiveColumnsTests
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

    private static Size Medir(int childCount, Size availableSize, Action<SlotGridPanel> configurar) =>
        EnHiloSta(() =>
        {
            var grid = new SlotGridPanel();
            configurar(grid);
            for (int i = 0; i < childCount; i++) grid.Children.Add(new Border());
            grid.Measure(availableSize);
            return grid.DesiredSize;
        });

    // Caso real: Columns=6 (maximumColumns, el techo de siempre), 12 hijos, ancho estrecho
    // (200px). En modo FIJO (comportamiento de siempre) las 6 columnas se mantienen a la fuerza:
    // cellFromWidth natural es (200-4*5)/6=30, por debajo de MinCell=40 - la celda se CONGELA en
    // MinCell (40), pidiendo mas ancho del disponible (260 reales, recortado por WPF a los 200 de
    // entrada - mismo gotcha ya documentado en T24-SLOTGRID caso1). Eso es exactamente lo que
    // dispara scroll horizontal cuando esto vive dentro de un ScrollViewer real.
    [Fact]
    public void ModoFijoDeSiempreCongelaEnMinCellYPideMasAnchoDelDisponible()
    {
        var size = Medir(12, new Size(200, double.PositiveInfinity),
            g => { g.Columns = 6; g.MinCell = 40; g.MaxCell = 90; g.Gap = 4; });

        // Ancho devuelto recortado por WPF al availableSize de entrada (200), no a los 260
        // reales que pediria 6*40+4*5.
        Assert.Equal(200, size.Width, precision: 3);
        // rows=ceil(12/6)=2; cell=MinCell=40 (congelada); alto real = 2*40+4*1=84.
        Assert.Equal(84, size.Height, precision: 3);
    }

    // Mismo escenario exacto (Columns=6, 12 hijos, MinCell=40, MaxCell=90, Gap=4, ancho 200) pero
    // con AdaptiveColumns=true: actualColumns baja de 6 a 4 (floor((200+4)/(40+4))=4, dentro del
    // techo maximumColumns=6) - la celda natural con 4 columnas (47) YA es legible (por encima de
    // MinCell) sin necesitar scroll, y la rejilla se hace mas ALTA (3 filas en vez de 2) para
    // absorber el mismo contenido - exactamente el comportamiento pedido por el encargo ("la
    // rejilla se hace mas ALTA cuando hay menos columnas").
    [Fact]
    public void AdaptiveColumnsReduceColumnasDentroDelTechoYHaceLaRejillaMasAlta()
    {
        var size = Medir(12, new Size(200, double.PositiveInfinity),
            g => { g.Columns = 6; g.MinCell = 40; g.MaxCell = 90; g.Gap = 4; g.AdaptiveColumns = true; });

        // actualColumns=4, cols<n asi que rows=ceil(12/4)=3; cellFromWidth=(200-4*3)/4=47;
        // totalW=4*47+4*3=200 (cabe exacto, sin recorte de WPF); totalH=3*47+4*2=149.
        Assert.Equal(200, size.Width, precision: 3);
        Assert.Equal(149, size.Height, precision: 3);

        // Confirma el propio requisito: menos columnas -> mas alto que el modo fijo (84) con el
        // MISMO contenido y el MISMO ancho disponible.
        Assert.True(size.Height > 84);
    }

    // actualColumns nunca puede superar maximumColumns (Columns) - el techo de siempre se
    // mantiene, nunca se ignora. Con un ancho ENORME (2000px) que dejaria caber muchas mas
    // columnas de las que Columns=6 permite, el panel se queda en 6, igual que el modo fijo.
    [Fact]
    public void AdaptiveColumnsNuncaSuperaElTechoDeMaximumColumns()
    {
        var fijo = Medir(12, new Size(2000, double.PositiveInfinity),
            g => { g.Columns = 6; g.MinCell = 40; g.MaxCell = 90; g.Gap = 4; });
        var adaptativo = Medir(12, new Size(2000, double.PositiveInfinity),
            g => { g.Columns = 6; g.MinCell = 40; g.MaxCell = 90; g.Gap = 4; g.AdaptiveColumns = true; });

        // Con ancho tan grande, ambos modos calzan las mismas 6 columnas (techo real) y el mismo
        // MaxCell=90 - resultado identico.
        Assert.Equal(fijo.Width, adaptativo.Width, precision: 3);
        Assert.Equal(fijo.Height, adaptativo.Height, precision: 3);
    }

    // Con MinCell+Gap<=0 (configuracion degenerada, nunca usada por ninguna pantalla real) no hay
    // ancho real contra el que dividir - AdaptiveColumns no debe reventar, se queda en
    // maximumColumns como el modo fijo.
    [Fact]
    public void AdaptiveColumnsConMinCellYGapCeroNoRevientaYUsaElTecho()
    {
        var size = Medir(4, new Size(200, double.PositiveInfinity),
            g => { g.Columns = 4; g.MinCell = 0; g.MaxCell = 90; g.Gap = 0; g.AdaptiveColumns = true; });

        Assert.False(double.IsNaN(size.Width));
        Assert.False(double.IsNaN(size.Height));
    }

    // Con ancho de entrada infinito (medida sin restriccion real, p.ej. antes del primer layout)
    // AdaptiveColumns no tiene ancho real contra el que adaptar - debe comportarse exactamente
    // como el modo fijo (maximumColumns), nunca reventar ni degenerar a 1 columna.
    [Fact]
    public void AdaptiveColumnsConAnchoInfinitoSeComportaComoElModoFijo()
    {
        var fijo = Medir(10, new Size(double.PositiveInfinity, double.PositiveInfinity),
            g => { g.Columns = 10; g.MinCell = 40; g.MaxCell = 90; g.Gap = 4; });
        var adaptativo = Medir(10, new Size(double.PositiveInfinity, double.PositiveInfinity),
            g => { g.Columns = 10; g.MinCell = 40; g.MaxCell = 90; g.Gap = 4; g.AdaptiveColumns = true; });

        Assert.Equal(fijo.Width, adaptativo.Width, precision: 3);
        Assert.Equal(fijo.Height, adaptativo.Height, precision: 3);
    }

    // Regresion directa de los 3 casos reales ya verificados por el arnes visual T24-SLOTGRID
    // (Terrakeep.App.Tests/Program.cs, sin tocar ese archivo) con AdaptiveColumns=false (valor
    // por defecto): confirma que la formula NO cambio para el modo de siempre, tambien de forma
    // headless e instantanea.
    [Fact]
    public void ModoFijoPorDefectoReproduceElCaso1DeT24SlotgridSueloMinCell()
    {
        var size = Medir(10, new Size(300, double.PositiveInfinity),
            g => { g.Columns = 10; g.MinCell = 40; g.MaxCell = 90; g.Gap = 4; });

        Assert.Equal(300, size.Width, precision: 3); // recortado por WPF, no los 436 reales
        Assert.Equal(40, size.Height, precision: 3); // 1 fila * MinCell=40
    }

    [Fact]
    public void ModoFijoPorDefectoReproduceElCaso2DeT24SlotgridTechoMaxCell()
    {
        var size = Medir(10, new Size(2000, double.PositiveInfinity),
            g => { g.Columns = 10; g.MinCell = 40; g.MaxCell = 90; g.Gap = 4; });

        Assert.Equal(936, size.Width, precision: 3); // 10*90+4*9
        Assert.Equal(90, size.Height, precision: 3);
    }

    [Fact]
    public void ModoFijoPorDefectoReproduceElCaso3DeT24SlotgridReferenceWidthCruzado()
    {
        var size = Medir(5, new Size(2000, double.PositiveInfinity),
            g => { g.Columns = 5; g.MinCell = 30; g.MaxCell = 90; g.Gap = 4; g.ReferenceColumns = 10; g.ReferenceWidth = 400; });

        Assert.Equal(198, size.Width, precision: 3); // 5*36.4+4*4
        Assert.Equal(36.4, size.Height, precision: 3);
    }
}
