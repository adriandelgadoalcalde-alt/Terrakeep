using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TerrasavrNative.App.Controls;

// Rejilla de CATALOGO (Librería de objetos/buffs) - hermana de SlotGridPanel, pero con la
// filosofia real contraria a proposito (consulta a Opus, octava pasada, investigacion real
// del Bestiario decompilado de Terraria: Terraria.GameContent.UI.Elements.
// UIBestiaryEntryGrid.cs - celda CASI FIJA, columnas/filas VARIABLES, desbordamiento resuelto
// con PAGINACION, nunca con scroll ni encogiendo la celda por debajo de MinCell).
//
// SlotGridPanel sirve a una rejilla SEMANTICA (Inventario=10 columnas reales de Terraria,
// Equipamiento=5, etc.) donde el numero de columnas SIGNIFICA algo y debe quedarse fijo -
// celda variable, columnas fijas. Esta clase sirve al caso contrario: una rejilla de
// CATALOGO (Libreria) donde el numero de columnas es arbitrario (a nadie le importa si una
// carpeta se ve en 12 o en 25 columnas) pero el TAMAÑO del icono si importa para que se
// reconozca de un vistazo - celda fija, columnas variables.
//
// cols = clamp(round((anchoDisponible+Gap)/(PreferredCell+Gap)), 1, MaxColumns)
// cell  = clamp((anchoDisponible-Gap*(cols-1))/cols, MinCell, MaxCell)
// rows  = max(1, floor((altoDisponible+Gap)/(cell+Gap)))
// PageCapacity = cols*rows - se publica al ViewModel real (OneWayToSource) para que pagine
// Results a exactamente ese numero de tarjetas, nunca mas de las que caben - por eso esta
// rejilla NUNCA necesita ScrollViewer ni scroll interno.
public sealed class LibraryGridPanel : Panel
{
    public static readonly DependencyProperty PreferredCellProperty = DependencyProperty.Register(
        nameof(PreferredCell), typeof(double), typeof(LibraryGridPanel),
        new FrameworkPropertyMetadata(64.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty MinCellProperty = DependencyProperty.Register(
        nameof(MinCell), typeof(double), typeof(LibraryGridPanel),
        new FrameworkPropertyMetadata(44.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty MaxCellProperty = DependencyProperty.Register(
        nameof(MaxCell), typeof(double), typeof(LibraryGridPanel),
        new FrameworkPropertyMetadata(76.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
        nameof(Gap), typeof(double), typeof(LibraryGridPanel),
        new FrameworkPropertyMetadata(4.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty MaxColumnsProperty = DependencyProperty.Register(
        nameof(MaxColumns), typeof(int), typeof(LibraryGridPanel),
        new FrameworkPropertyMetadata(30, FrameworkPropertyMetadataOptions.AffectsMeasure));

    // Salida real hacia el ViewModel (Mode=OneWayToSource en XAML) - cuantas tarjetas caben
    // de verdad en el espacio real disponible. Publicar DENTRO de MeasureOverride reentra en
    // layout (WPF lanza "Layout cycle detected") - se difiere con Dispatcher.BeginInvoke y
    // solo cuando el valor real cambia, para no disparar un ciclo de remedidos infinito.
    public static readonly DependencyProperty PageCapacityProperty = DependencyProperty.Register(
        nameof(PageCapacity), typeof(int), typeof(LibraryGridPanel),
        new FrameworkPropertyMetadata(0));

    public double PreferredCell { get => (double)GetValue(PreferredCellProperty); set => SetValue(PreferredCellProperty, value); }
    public double MinCell { get => (double)GetValue(MinCellProperty); set => SetValue(MinCellProperty, value); }
    public double MaxCell { get => (double)GetValue(MaxCellProperty); set => SetValue(MaxCellProperty, value); }
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }
    public int MaxColumns { get => (int)GetValue(MaxColumnsProperty); set => SetValue(MaxColumnsProperty, value); }
    public int PageCapacity { get => (int)GetValue(PageCapacityProperty); set => SetValue(PageCapacityProperty, value); }

    private double _cell = 64;
    private int _cols = 1;
    private int _pendingCapacity = -1;

    protected override Size MeasureOverride(Size availableSize)
    {
        double availW = double.IsInfinity(availableSize.Width) ? PreferredCell * 10 : availableSize.Width;
        double availH = double.IsInfinity(availableSize.Height) ? PreferredCell * 4 : availableSize.Height;

        int cols = Math.Clamp((int)Math.Round((availW + Gap) / (PreferredCell + Gap)), 1, Math.Max(1, MaxColumns));
        double cell = Math.Clamp((availW - Gap * (cols - 1)) / cols, MinCell, MaxCell);
        int rows = Math.Max(1, (int)Math.Floor((availH + Gap) / (cell + Gap)));

        _cell = cell;
        _cols = cols;

        int capacity = cols * rows;
        if (capacity != PageCapacity && capacity != _pendingCapacity)
        {
            _pendingCapacity = capacity;
            Dispatcher.BeginInvoke(DispatcherPriority.DataBind, () =>
            {
                PageCapacity = capacity;
                _pendingCapacity = -1;
            });
        }

        int n = InternalChildren.Count;
        var childSize = new Size(cell, cell);
        foreach (UIElement child in InternalChildren) child.Measure(childSize);

        if (n == 0) return new Size(0, 0);
        int realRows = (int)Math.Ceiling(n / (double)cols);
        double totalW = cols * cell + Gap * (cols - 1);
        double totalH = Math.Min(realRows, rows) * cell + Gap * (Math.Min(realRows, rows) - 1);
        return new Size(totalW, Math.Max(0, totalH));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int n = InternalChildren.Count;
        if (n == 0) return finalSize;

        double contentW = _cols * _cell + Gap * (_cols - 1);
        // Mismo centrado horizontal real ya establecido en SlotGridPanel.ArrangeOverride (el
        // usuario pidio explicitamente ambas cosas: que no quede pegado a un lado, y que el
        // hueco sobrante se lea como diseño, no como bug).
        double offsetX = Math.Max(0, (finalSize.Width - contentW) / 2);

        for (int i = 0; i < n; i++)
        {
            int r = i / _cols, c = i % _cols;
            double x = offsetX + c * (_cell + Gap), y = r * (_cell + Gap);
            InternalChildren[i].Arrange(new Rect(x, y, _cell, _cell));
        }
        return finalSize;
    }
}
