using System.Windows;
using System.Windows.Controls;

namespace Terrakeep.App.Controls;

// Rejilla propia para contenedores grandes (Inventario=50, Banco/Caja/Fragua/Boveda=40) -
// pregunta a Opus sobre el diseño, 2-sep-2026 ("50 tarjetas ricas no caben legibles en
// ~678x165 con ninguna estrategia de escalado... cambiar la tarjeta, no la estrategia de
// escalado"). En vez de un Viewbox que escala TODO el bloque sin suelo (haciendolo ilegible
// con muchos objetos, y con un ScaleTransform que embarra el pixel art de los sprites), esta
// rejilla calcula un tamaño de celda de verdad: crece o encoge con la ventana ("de un
// plumazo", pedido explicito 2-sep-2026) mientras se mantenga entre MinCell y MaxCell: por
// debajo de MinCell se congela ahi y aparece scroll (nunca se sacrifica la legibilidad), por
// encima de MaxCell se congela tambien (para que Monedas/Municion, aunque vivieran aqui, no
// se inflen a tarjetas gigantes).
//
// cols = min(Columns, n); rows = ceil(n/cols); cell = clamp(min(anchoDisponible/cols,
// altoDisponible/rows), MinCell, MaxCell).
//
// Auditoria de Opus, Bloque 6 (T-24): resumen real de los 3 modos, a golpe de vista (el
// detalle de CADA UNO, con su motivacion real y el bug que arreglo, ya vive en el comentario de
// su propia DependencyProperty mas abajo - esto es solo el mapa):
//  1. BASICO (ReferenceColumns=0, el caso de siempre) - la celda se calcula SOLO contra el
//     propio ancho/alto disponibles de este panel, sin mirar a nadie mas.
//  2. ReferenceColumns>0, ReferenceWidth=0 - la celda nunca puede superar la que tendria un
//     panel de ReferenceColumns columnas usando el ANCHO PROPIO de este panel (para hermanos
//     que ya comparten la misma columna de Grid - incluso con menos columnas reales, no se
//     inflan mas que "10 columnas cabrian aqui").
//  3. ReferenceColumns>0 Y ReferenceWidth>0 - igual que el 2, pero contra un ancho de
//     referencia EXTERNO explicito (para hermanos en columnas de Grid DISTINTAS - ej. una fila
//     fusionada con 3 SlotGridPanel en 3 columnas mas estrechas cada uno, que deben verse todos
//     al mismo tamaño de icono que si compartieran la fila entera).
// Verificado con 3 casos reales deterministas (matematica pura, sin necesitar una ventana real)
// en Terrakeep.App.Tests - ver "T24-SLOTGRID" ahi.
public sealed class SlotGridPanel : Panel
{
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
        nameof(Columns), typeof(int), typeof(SlotGridPanel),
        new FrameworkPropertyMetadata(10, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty MinCellProperty = DependencyProperty.Register(
        nameof(MinCell), typeof(double), typeof(SlotGridPanel),
        new FrameworkPropertyMetadata(44.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty MaxCellProperty = DependencyProperty.Register(
        nameof(MaxCell), typeof(double), typeof(SlotGridPanel),
        new FrameworkPropertyMetadata(96.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
        nameof(Gap), typeof(double), typeof(SlotGridPanel),
        new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    // Un ScrollViewer mide a su hijo con Height = infinito, asi que sin esto el panel siempre
    // elegiria MaxCell y scrollearia siempre aunque sobrara espacio real. Se enlaza desde XAML
    // al ActualHeight del propio ScrollViewer que lo contiene via RelativeSource AncestorType
    // (un recorrido real del arbol visual, no un ElementName - ese si es fragil cruzando el
    // limite de un ItemsPanelTemplate, ver nota de diseño real de Opus).
    public static readonly DependencyProperty AvailableHeightProperty = DependencyProperty.Register(
        nameof(AvailableHeight), typeof(double), typeof(SlotGridPanel),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    // ReferenceColumns (0 = desactivado) - pregunta a Opus sobre el diseño, 2-sep-2026, cuarta
    // pasada ("al hacer la rejilla nueva para equipamiento y en la pestaña de mascotas y
    // municion y monedas se ven los iconos enormes... me gustaria que se vieran igual que la
    // rejilla de inventario de tamaño"). Bug real diagnosticado por Opus con la geometria real
    // del layout: Equipamiento (5 columnas) e Inventario (10 columnas) comparten el MISMO
    // ancho disponible (misma columna del mismo Grid), pero cada SlotGridPanel maximizaba SU
    // PROPIA celda de forma independiente - con menos columnas, cellFromWidth es mucho mayor
    // (672/5=134 vs 672/10=64), asi que Equipamiento se pegaba al MaxCell=96 mientras
    // Inventario se quedaba en ~64. Con ReferenceColumns>0, la celda nunca puede superar el
    // tamaño que tendria un contenedor de ReferenceColumns columnas en ESE MISMO ancho -
    // todos los contenedores que comparten columna (los 9 reales, ver ContainerCompactTemplate
    // en MainWindow.xaml, ReferenceColumns="10" = Inventario) salen con el mismo tamaño de
    // icono, sin perder el "crece con la ventana" (en una ventana grande, todos crecen igual
    // hasta MaxCell).
    public static readonly DependencyProperty ReferenceColumnsProperty = DependencyProperty.Register(
        nameof(ReferenceColumns), typeof(int), typeof(SlotGridPanel),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    // ReferenceWidth (0 = desactivado) - pregunta a Opus sobre el diseño, quinta pasada
    // (fusion de Equipamiento con Monturas/Monedas como laterales). ReferenceColumns por si
    // solo calcula cellFromReference contra el ANCHO PROPIO del panel (availW) - funciona hoy
    // porque los 9 contenedores comparten literalmente la misma columna del mismo Grid, pero
    // en un layout fusionado (Equipamiento + 2 laterales, 3 SlotGridPanel en 3 columnas
    // DISTINTAS mas estrechas) cada uno calcularia su propio techo contra SU PROPIO ancho
    // reducido y los tres colapsarian al MinCell, no al tamaño real que les corresponde.
    // ReferenceWidth deja fijar el ancho de referencia real (el de TODA la fila fusionada) en
    // vez del ancho propio - sin poner (0), comportamiento identico a solo-ReferenceColumns.
    public static readonly DependencyProperty ReferenceWidthProperty = DependencyProperty.Register(
        nameof(ReferenceWidth), typeof(double), typeof(SlotGridPanel),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    public double MinCell { get => (double)GetValue(MinCellProperty); set => SetValue(MinCellProperty, value); }
    public double MaxCell { get => (double)GetValue(MaxCellProperty); set => SetValue(MaxCellProperty, value); }
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }
    public double AvailableHeight { get => (double)GetValue(AvailableHeightProperty); set => SetValue(AvailableHeightProperty, value); }
    public int ReferenceColumns { get => (int)GetValue(ReferenceColumnsProperty); set => SetValue(ReferenceColumnsProperty, value); }
    public double ReferenceWidth { get => (double)GetValue(ReferenceWidthProperty); set => SetValue(ReferenceWidthProperty, value); }

    private double _cell = 44;
    private int _cols = 1;

    protected override Size MeasureOverride(Size availableSize)
    {
        int n = InternalChildren.Count;
        if (n == 0) return new Size(0, 0);

        int cols = Math.Max(1, Math.Min(Columns, n));
        int rows = (int)Math.Ceiling(n / (double)cols);

        double availW = double.IsInfinity(availableSize.Width) ? MaxCell * cols + Gap * (cols - 1) : availableSize.Width;
        double availH = double.IsInfinity(availableSize.Height) ? AvailableHeight : availableSize.Height;
        // Antes del primer paso de layout real (AvailableHeight todavia en 0) - no colapsar a
        // una rejilla ilegible, partir de MaxCell hasta que el remedido real llegue.
        if (availH <= 0) availH = MaxCell * rows + Gap * (rows - 1);

        double cellFromWidth = (availW - Gap * (cols - 1)) / cols;
        double cellFromHeight = (availH - Gap * (rows - 1)) / rows;
        double refW = ReferenceWidth > 0 ? ReferenceWidth : availW;
        double cellFromReference = ReferenceColumns > 0
            ? (refW - Gap * (ReferenceColumns - 1)) / ReferenceColumns
            : double.PositiveInfinity;
        double cell = Math.Clamp(Math.Min(Math.Min(cellFromWidth, cellFromHeight), cellFromReference), MinCell, MaxCell);

        _cell = cell;
        _cols = cols;

        var childSize = new Size(cell, cell);
        foreach (UIElement child in InternalChildren) child.Measure(childSize);

        double totalW = cols * cell + Gap * (cols - 1);
        double totalH = rows * cell + Gap * (rows - 1);
        return new Size(totalW, totalH);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int n = InternalChildren.Count;
        if (n == 0) return finalSize;

        double contentW = _cols * _cell + Gap * (_cols - 1);
        // ItemsPresenter ignora el HorizontalAlignment/VerticalAlignment que se le ponga al
        // panel del ItemsPanelTemplate en XAML (arregla siempre al panel el finalSize
        // completo, sea cual sea su tamaño natural real - gotcha real de WPF, confirmado tras
        // que un HorizontalAlignment="Center" puesto directamente en el XAML no tuviera ningun
        // efecto visible pese a compilar y ejecutarse sin error). El centrado real solo puede
        // hacerse aqui, calculando el hueco sobrante entre finalSize (siempre el disponible
        // completo) y el tamaño de contenido real, y desplazando el origen de cada celda -
        // pedido explicito del usuario (quinta pasada, "el panel me queda a un lado izquierdo,
        // no me gusta"). SOLO en horizontal - la queja original era de lado izquierdo/derecho,
        // nunca de arriba/abajo. Un primer intento centró tambien en vertical y desplazó la
        // rejilla de Equipamiento (fila "*" con hueco vertical real de sobra) hacia el medio,
        // separandola de la fila del selector Loadout/Vista - correccion real del usuario:
        // "los slots tambien de armadura y accesorio vuelvan a la parte superior no al
        // centro". offsetY se queda siempre en 0 (arriba), como estaba antes de esta pasada.
        double offsetX = Math.Max(0, (finalSize.Width - contentW) / 2);
        const double offsetY = 0;

        for (int i = 0; i < n; i++)
        {
            int r = i / _cols, c = i % _cols;
            double x = offsetX + c * (_cell + Gap), y = offsetY + r * (_cell + Gap);
            InternalChildren[i].Arrange(new Rect(x, y, _cell, _cell));
        }
        return finalSize;
    }
}
