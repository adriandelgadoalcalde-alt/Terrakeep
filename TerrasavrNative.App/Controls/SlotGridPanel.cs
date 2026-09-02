using System.Windows;
using System.Windows.Controls;

namespace TerrasavrNative.App.Controls;

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

    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    public double MinCell { get => (double)GetValue(MinCellProperty); set => SetValue(MinCellProperty, value); }
    public double MaxCell { get => (double)GetValue(MaxCellProperty); set => SetValue(MaxCellProperty, value); }
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }
    public double AvailableHeight { get => (double)GetValue(AvailableHeightProperty); set => SetValue(AvailableHeightProperty, value); }

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
        double cell = Math.Clamp(Math.Min(cellFromWidth, cellFromHeight), MinCell, MaxCell);

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
        for (int i = 0; i < n; i++)
        {
            int r = i / _cols, c = i % _cols;
            double x = c * (_cell + Gap), y = r * (_cell + Gap);
            InternalChildren[i].Arrange(new Rect(x, y, _cell, _cell));
        }
        return finalSize;
    }
}
