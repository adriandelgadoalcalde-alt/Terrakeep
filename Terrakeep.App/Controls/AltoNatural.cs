using System.Windows;
using System.Windows.Controls;

namespace Terrakeep.App.Controls;

// FASE D del responsive global, correccion D-01 del revisor visual (28-sep-2026): decorador que mide a su
// hijo con alto ILIMITADO y expone ese alto natural (Alto), sin cambiar nada del layout (devuelve el mismo
// tamaño que devolveria un Decorator normal, recortado al disponible). Lo usa ObjetosView para que, en
// Compacto (panel Editar solo en la fila de contenido), el reparto Objetos/Libreria nunca deje a Editar mas
// bajo que su contenido - sin medir a mano dentro de LayoutUpdated (eso provocaria otra pasada de layout).
public sealed class AltoNatural : Decorator
{
    public double Alto { get; private set; }

    protected override Size MeasureOverride(Size constraint)
    {
        if (Child is not UIElement hijo) { Alto = 0; return default; }
        hijo.Measure(new Size(constraint.Width, double.PositiveInfinity));
        Alto = hijo.DesiredSize.Height;
        return new Size(hijo.DesiredSize.Width, Math.Min(Alto, constraint.Height));
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        Child?.Arrange(new Rect(arrangeSize));
        return arrangeSize;
    }
}
