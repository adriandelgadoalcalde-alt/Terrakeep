using System.Windows;
using System.Windows.Controls;

namespace Terrakeep.App.Controls;

// Pedido explicito del usuario (29-sep-2026, captura de "Acerca de"): las tarjetas del historial de
// versiones "tiene[n] que tener una caja que queden todas iguales pero que sea justo al acabar el
// texto, no esta locura". Causa real: el ItemsPanel era un UniformGrid, que da a TODAS las celdas el
// alto de la tarjeta MAS ALTA de toda la coleccion - con un historial largo, cualquier version corta
// se estiraba hasta el alto de la version mas larga (cientos de px de fondo vacio bajo el texto).
//
// Este panel reparte el ancho EXACTAMENTE igual que UniformGrid (Columnas columnas del mismo ancho,
// mismo orden de lectura fila a fila), pero cada FILA mide lo que mide su tarjeta mas alta: dentro de
// una fila todas las tarjetas quedan iguales (se estiran a esa altura), y la fila termina justo donde
// acaba su texto mas largo. Sin estado entre pasadas y sin tocar nada fuera de si mismo (el alto de
// una fila depende solo de sus hijos medidos con un ancho fijo), asi que no puede entrar en bucle de
// layout.
public sealed class TarjetasEnFilasPanel : Panel
{
    public static readonly DependencyProperty ColumnasProperty = DependencyProperty.Register(
        nameof(Columnas), typeof(int), typeof(TarjetasEnFilasPanel),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsMeasure),
        v => v is int n && n >= 1);

    public int Columnas
    {
        get => (int)GetValue(ColumnasProperty);
        set => SetValue(ColumnasProperty, value);
    }

    // Solo lectura para diagnostico (canario RESTO_RESPONSIVE_SOLO): alto real de cada fila.
    public IReadOnlyList<double> AltosDeFila => _altosFila;

    private readonly List<double> _altosFila = [];

    private List<UIElement> Visibles()
    {
        var lista = new List<UIElement>(InternalChildren.Count);
        foreach (UIElement h in InternalChildren)
            if (h != null && h.Visibility != Visibility.Collapsed) lista.Add(h);
        return lista;
    }

    protected override Size MeasureOverride(Size disponible)
    {
        int columnas = Math.Max(1, Columnas);
        var hijos = Visibles();
        _altosFila.Clear();

        // Mismo criterio que UniformGrid: con ancho finito, cada celda = ancho / columnas; con ancho
        // infinito (sin padre que acote), la celda es tan ancha como el hijo mas ancho.
        double anchoCelda;
        if (double.IsInfinity(disponible.Width))
        {
            anchoCelda = 0;
            foreach (var h in hijos)
            {
                h.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                anchoCelda = Math.Max(anchoCelda, h.DesiredSize.Width);
            }
        }
        else anchoCelda = disponible.Width / columnas;

        var medida = new Size(anchoCelda, double.PositiveInfinity);
        double altoTotal = 0, altoFila = 0, anchoMaximo = 0;
        for (int i = 0; i < hijos.Count; i++)
        {
            hijos[i].Measure(medida);
            anchoMaximo = Math.Max(anchoMaximo, hijos[i].DesiredSize.Width);
            altoFila = Math.Max(altoFila, hijos[i].DesiredSize.Height);
            if (i % columnas == columnas - 1 || i == hijos.Count - 1)
            {
                _altosFila.Add(altoFila);
                altoTotal += altoFila;
                altoFila = 0;
            }
        }
        // Ancho deseado igual que UniformGrid: la celda mas ancha x columnas.
        return new Size(anchoMaximo * columnas, altoTotal);
    }

    protected override Size ArrangeOverride(Size final)
    {
        int columnas = Math.Max(1, Columnas);
        var hijos = Visibles();
        double anchoCelda = final.Width / columnas;
        double y = 0;
        int fila = 0;
        for (int i = 0; i < hijos.Count; i++)
        {
            int columna = i % columnas;
            double alto = fila < _altosFila.Count ? _altosFila[fila] : hijos[i].DesiredSize.Height;
            hijos[i].Arrange(new Rect(columna * anchoCelda, y, anchoCelda, alto));
            if (columna == columnas - 1 || i == hijos.Count - 1) { y += alto; fila++; }
        }
        foreach (UIElement h in InternalChildren)
            if (h != null && h.Visibility == Visibility.Collapsed) h.Arrange(new Rect(0, 0, 0, 0));
        return final;
    }
}
