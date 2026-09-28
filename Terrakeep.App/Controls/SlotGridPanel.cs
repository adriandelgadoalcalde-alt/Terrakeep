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

    // AdaptiveColumns (FASE A del responsive global, PDF "Arreglo familia keep", segundo bloque -
    // "TERRAKEEP: RESPONSIVE GLOBAL, PAGINACION Y SCROLL COMO ULTIMO RECURSO"). Generalizacion
    // DENTRO del propio panel del mismo patron que hasta ahora vivia SOLO fuera, en
    // ChestInspectorColumnsConverter (DensityConverters.cs): en vez de que cada pantalla nueva
    // reinvente su propio converter que le dice a SlotGridPanel cuantas columnas usar, el panel
    // puede calcularlo el mismo con el ancho REAL disponible.
    //
    // Semantica exacta pedida por el encargo: `Columns` pasa a significar "maximumColumns" (el
    // techo de siempre, NUNCA se ignora - preserva fixed-columns donde ya tiene significado
    // semantico real, ej. Equipamiento a 5). `actualColumns` es el maximo numero de columnas que
    // caben de verdad con una celda de al menos MinCell legible, sin superar ese techo - con
    // menos columnas la rejilla se hace mas ALTA (mas filas), nunca depende de un converter
    // externo y nunca crea su propio scroll interno (eso sigue siendo responsabilidad del scroll
    // owner EXTERIOR que hospede el panel, exactamente igual que hoy).
    //
    // Opt-in, por defecto false: con AdaptiveColumns=false el calculo de `cols` es LITERAL el
    // mismo `Math.Max(1, Math.Min(Columns, n))` de siempre - comportamiento identico byte a byte
    // al que tenia el panel antes de esta pasada para el resto de pantallas.
    //
    // FASE F del responsive global (29-sep-2026): ChestInspectorColumnsConverter ya esta migrado
    // (retirado) - Views/ChestInspectorView.xaml usa AdaptiveColumns="True" directamente, misma
    // formula, calculada dentro del panel contra el availableSize.Width real del Measure.
    public static readonly DependencyProperty AdaptiveColumnsProperty = DependencyProperty.Register(
        nameof(AdaptiveColumns), typeof(bool), typeof(SlotGridPanel),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

    // PreferirCeldaGrande (FASE D del responsive global, 28-sep-2026) - solo tiene efecto junto con
    // AdaptiveColumns y con un alto finito conocido (AvailableHeight del scroll owner de los resultados).
    // Para rejillas SIN columnas semanticas (resultados de la Libreria y de la Libreria de buffs, s8/s17).
    // AdaptiveColumns a secas elige SIEMPRE el maximo de columnas que caben a MinCell: con muchos
    // resultados es lo correcto (llenar el ancho, mas resultados a la vista), pero cuando TODOS caben en
    // el viewport deja celdas minimas y media pantalla vacia (medido: categoria de 100 objetos a
    // 2560x1440, 40 columnas de 47px en 3 filas sobre un viewport de 584px). Con esta opcion, si hay un
    // numero de columnas con el que todo cabe en el alto disponible, se elige el que da la celda MAS
    // GRANDE (empate: mas columnas); si ni con el maximo de columnas cabe a MinCell, se queda en el
    // maximo de columnas a MinCell (el scroll owner exterior absorbe el resto, s14 paso 8). Nunca supera
    // Columns (maximumColumns) ni MaxCell/ReferenceColumns. Por defecto false: nada cambia en Fase A.
    public static readonly DependencyProperty PreferirCeldaGrandeProperty = DependencyProperty.Register(
        nameof(PreferirCeldaGrande), typeof(bool), typeof(SlotGridPanel),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public bool PreferirCeldaGrande { get => (bool)GetValue(PreferirCeldaGrandeProperty); set => SetValue(PreferirCeldaGrandeProperty, value); }

    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    public double MinCell { get => (double)GetValue(MinCellProperty); set => SetValue(MinCellProperty, value); }
    public double MaxCell { get => (double)GetValue(MaxCellProperty); set => SetValue(MaxCellProperty, value); }
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }
    public double AvailableHeight { get => (double)GetValue(AvailableHeightProperty); set => SetValue(AvailableHeightProperty, value); }
    public int ReferenceColumns { get => (int)GetValue(ReferenceColumnsProperty); set => SetValue(ReferenceColumnsProperty, value); }
    public double ReferenceWidth { get => (double)GetValue(ReferenceWidthProperty); set => SetValue(ReferenceWidthProperty, value); }
    public bool AdaptiveColumns { get => (bool)GetValue(AdaptiveColumnsProperty); set => SetValue(AdaptiveColumnsProperty, value); }

    private double _cell = 44;
    private int _cols = 1;

    // FASE C del responsive global (28-sep-2026): cuanto alto le FALTA a la rejilla en la ultima
    // medida con alto finito - 0 si cabe (la celda se encoge hasta caber) y > 0 solo cuando ni
    // siquiera a MinCell cabe en el alto ofrecido. WPF recorta el DesiredSize de cada elemento a
    // su availableSize (ver CLAUDE.md del repo), asi que ese exceso no llega a los padres por el
    // layout normal: lo lee AjusteAlViewport para crecer justo lo que falta y dejar que el scroll
    // owner de la pagina lo absorba (s14 paso 8), en vez de recortar las ultimas filas.
    public double DeficitAlto { get; private set; }

    // D-08 (correccion de la FASE D, 28-sep-2026): alto que ocuparia la rejilla con la celda en MinCell (con
    // las columnas que caben a MinCell en este ancho). Lo usa AjusteAlViewport.AltoMinimo.
    public double AltoEnMinCell { get; private set; }

    // Correccion H-C1 del revisor de la FASE C: aviso a AjusteAlViewport en CADA medida con alto
    // finito (evento enrutado que burbujea, sin recorrer el arbol a mano). Antes solo se avisaba si
    // cambiaba DeficitAlto, y eso no bastaba: en la primera entrada a Inventario el MinCell enlazado
    // (MultiBinding con AncestorType=Window) aun valia el 44 por defecto, la rejilla no cabia y el
    // decorador la volvia a medir con alto+20; cuando el binding resolvia MinCell=40, WPF re-media SOLO
    // la rejilla con esa restriccion INFLADA, a 44px cabia justo, el deficit seguia en 0 y la pagina se
    // quedaba en 316px para un viewport de 295,9 (20px de scroll, 5a fila cortada). Cualquier medida
    // propia de la rejilla (MinCell/MaxCell/Columns/hijos que cambian) debe dar al decorador la ocasion
    // de volver a repartir desde el alto REAL; las que provoca el propio decorador las ignora el (sin bucle).
    public static readonly RoutedEvent MedidaConAltoFinitoEvent = EventManager.RegisterRoutedEvent(
        "MedidaConAltoFinito", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SlotGridPanel));

    protected override Size MeasureOverride(Size availableSize)
    {
        int n = InternalChildren.Count;
        DeficitAlto = 0;
        if (n == 0)
        {
            AltoEnMinCell = 0;
            if (!double.IsInfinity(availableSize.Height)) RaiseEvent(new RoutedEventArgs(MedidaConAltoFinitoEvent, this));
            return new Size(0, 0);
        }

        int maximumColumns = Math.Max(1, Math.Min(Columns, n));

        // AdaptiveColumns: actualColumns = maximo numero de columnas que caben de verdad con
        // MinCell legible en el ancho REAL disponible, sin superar maximumColumns - misma formula
        // (inversa de cellFromWidth) que ya usaba ChestInspectorColumnsConverter desde fuera. Con
        // ancho infinito (medida sin restriccion real, p.ej. antes del primer layout) o con
        // MinCell+Gap<=0 (configuracion degenerada) no hay ancho real contra el que adaptar - se
        // queda en maximumColumns, igual que el modo fijo.
        int cols = maximumColumns;
        if (AdaptiveColumns && !double.IsInfinity(availableSize.Width))
        {
            double celdaConHueco = MinCell + Gap;
            if (celdaConHueco > 0)
            {
                int columnasQueCaben = (int)Math.Floor((availableSize.Width + Gap) / celdaConHueco);
                cols = Math.Clamp(columnasQueCaben, 1, maximumColumns);
            }
        }

        double availH = double.IsInfinity(availableSize.Height) ? AvailableHeight : availableSize.Height;
        int filasEnMinCell = (int)Math.Ceiling(n / (double)cols);
        AltoEnMinCell = filasEnMinCell * MinCell + Gap * (filasEnMinCell - 1);

        if (AdaptiveColumns && PreferirCeldaGrande && !double.IsInfinity(availableSize.Width) && availH > 0)
        {
            double w = availableSize.Width;
            double refW0 = ReferenceWidth > 0 ? ReferenceWidth : w;
            double techoRef = ReferenceColumns > 0 ? (refW0 - Gap * (ReferenceColumns - 1)) / ReferenceColumns : double.PositiveInfinity;
            int mejorCols = -1; double mejorCelda = 0;
            for (int c = cols; c >= 1; c--)
            {
                int r = (int)Math.Ceiling(n / (double)c);
                double celdaC = Math.Min(Math.Min((w - Gap * (c - 1)) / c, (availH - Gap * (r - 1)) / r), Math.Min(techoRef, MaxCell));
                if (celdaC >= MinCell - 0.01 && celdaC > mejorCelda + 0.01) { mejorCelda = celdaC; mejorCols = c; }
            }
            if (mejorCols > 0) cols = mejorCols;
        }

        int rows = (int)Math.Ceiling(n / (double)cols);

        double availW = double.IsInfinity(availableSize.Width) ? MaxCell * cols + Gap * (cols - 1) : availableSize.Width;
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
        if (!double.IsInfinity(availableSize.Height))
        {
            DeficitAlto = Math.Max(0, totalH - availableSize.Height);
            RaiseEvent(new RoutedEventArgs(MedidaConAltoFinitoEvent, this));
        }
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
