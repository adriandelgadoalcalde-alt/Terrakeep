using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Terrakeep.App.Controls;

// FASE C del responsive global (PDF "Arreglo familia keep", bloque 2 - s8/s13/s14/s22/s28, 28-sep-2026).
//
// Contenido de una pagina FINITA (Inventario 10x5, Almacenes 10x4) que se AJUSTA al viewport de su
// UNICO scroll owner (el ScrollViewer de la pagina) y solo crece - dejando que ese owner desplace -
// cuando ni siquiera a MinCell cabe.
//
// Por que hace falta: un ScrollViewer mide a su contenido con alto INFINITO, asi que una rejilla dentro
// de el nunca sabe cuanto sitio real le queda debajo de la cabecera. El modelo viejo lo "resolvia" con un
// segundo ScrollViewer dentro (ContainerCompactTemplate) cuyo alto enlazaba SlotGridPanel.AvailableHeight -
// pero anidado en el de la pagina ese ScrollViewer tambien recibia alto infinito: se quedaba del alto de
// su propio contenido (nunca desplazaba) y la celda quedaba CONGELADA en el primer tamaño que tuvo
// (medido: 57,1px de 1080 a 2576px de ancho, 29% del ancho usado maximizado), ademas de robar el foco al
// pulsar un slot y arrastrar la pagina ~75-84px (FrameworkElement.OnGotFocus -> BringIntoView).
//
// Que hace: mide al hijo con el alto REAL del viewport (AltoViewport, enlazado en XAML al ViewportHeight
// del ScrollViewer de la pagina). La cabecera toma lo suyo y la rejilla (SlotGridPanel) encoge o crece la
// celda hasta caber, entre MinCell y MaxCell. Si aun a MinCell no cabe, SlotGridPanel.DeficitAlto dice
// cuanto falta y se vuelve a medir con ese alto de mas: la pagina queda justo ese tanto mas alta que el
// viewport y la desplaza su owner (s14: el scroll como ultimo recurso, nunca uno local).
public sealed class AjusteAlViewport : Decorator
{
    public static readonly DependencyProperty AltoViewportProperty = DependencyProperty.Register(
        nameof(AltoViewport), typeof(double), typeof(AjusteAlViewport),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double AltoViewport { get => (double)GetValue(AltoViewportProperty); set => SetValue(AltoViewportProperty, value); }

    // true mientras este decorador mide a su hijo: los cambios de SlotGridPanel.DeficitAlto que
    // provoca esa misma medida no necesitan avisarle (ver SlotGridPanel.MeasureOverride).
    internal bool Midiendo { get; private set; }

    protected override Size MeasureOverride(Size constraint)
    {
        var hijo = Child;
        if (hijo == null) return new Size(0, 0);

        double alto = double.IsInfinity(constraint.Height) ? AltoViewport : constraint.Height;
        Midiendo = true;
        try
        {
            // Antes del primer layout real (viewport aun sin medir) no hay alto contra el que ajustar:
            // medida natural, y en cuanto el ScrollViewer publique su ViewportHeight se vuelve a medir.
            if (double.IsNaN(alto) || double.IsInfinity(alto) || alto <= 0)
            {
                hijo.Measure(constraint);
                return hijo.DesiredSize;
            }

            hijo.Measure(new Size(constraint.Width, alto));
            double deficit = DeficitDeRejillas(hijo);
            if (deficit > 0.5) hijo.Measure(new Size(constraint.Width, alto + deficit));
            return hijo.DesiredSize;
        }
        finally { Midiendo = false; }
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        Child?.Arrange(new Rect(arrangeSize));
        return arrangeSize;
    }

    // Suma del alto que les falta a las rejillas visibles del contenido (en estas paginas hay una sola
    // a la vez; sumar es lo conservador si algun dia se apilan varias). No entra dentro de cada
    // SlotGridPanel: sus hijos son celdas, no pueden contener otra rejilla.
    private static double DeficitDeRejillas(DependencyObject raiz)
    {
        double total = 0;
        int n = VisualTreeHelper.GetChildrenCount(raiz);
        for (int i = 0; i < n; i++)
        {
            var hijo = VisualTreeHelper.GetChild(raiz, i);
            if (hijo is UIElement { Visibility: not Visibility.Visible }) continue;
            if (hijo is SlotGridPanel rejilla) { total += rejilla.DeficitAlto; continue; }
            total += DeficitDeRejillas(hijo);
        }
        return total;
    }
}
