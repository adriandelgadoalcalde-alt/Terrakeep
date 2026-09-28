using System.Windows;
using System.Windows.Controls;

namespace Terrakeep.App.Controls;

// Correccion D-01 del revisor visual de la FASE D del responsive global (28-sep-2026): la linea de ruta de
// NavegadorCategorias ("> Armas > Daño cuerpo a cuerpo (316) > Espadas (111) > Pagina 1  Subcategorias (3)
// 40 resultado(s)...") era un WrapPanel y envolvia a 2 lineas (40,6px) con una ruta profunda o con
// "Colocables (1047)" a 1080x700: el viewport de resultados bajaba a 37,5-39,1px, menos que una celda.
// Este panel la mantiene SIEMPRE en una linea:
//   1. Si todo cabe, todo se ve.
//   2. Si no, las migas intermedias se pliegan en "..." (Rol=Elipsis, con la ruta completa en su tooltip)
//      empezando por la mas alta; la ultima miga (la carpeta elegida) no se pliega nunca.
//   3. Si aun asi no cabe, el resumen (Rol=Resumen, con TextTrimming y tooltip completo) cede el ancho que
//      falte - la informacion completa sigue a un paso (s34).
// Rol por hijo (propiedad adjunta): Miga, Elipsis, Fijo (boton de subcategorias, acciones de carpeta) y
// Resumen. Orden visual = orden de los hijos. Las migas plegadas y la elipsis sin uso pasan a
// Visibility.Hidden (no Collapsed: siguen midiendose, asi el panel conoce su ancho natural para decidir
// en la pasada siguiente; y Hidden <-> Visible no invalida la medida del padre, no hay bucle de layout) y
// se colocan con tamaño 0: ni se dibujan, ni reciben clics, ni foco de teclado, ni cuentan como
// visibles (IsVisible=false) para la automatizacion/el arnes.
public enum RolLineaRuta { Fijo, Miga, Elipsis, Resumen }

public sealed class LineaRutaPanel : Panel
{
    public static readonly DependencyProperty RolProperty = DependencyProperty.RegisterAttached(
        "Rol", typeof(RolLineaRuta), typeof(LineaRutaPanel),
        new FrameworkPropertyMetadata(RolLineaRuta.Fijo, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

    public static RolLineaRuta GetRol(DependencyObject d) => (RolLineaRuta)d.GetValue(RolProperty);
    public static void SetRol(DependencyObject d, RolLineaRuta v) => d.SetValue(RolProperty, v);

    // Solo lectura para diagnostico (canario LIBRARY_RESPONSIVE_SOLO): cuantas migas quedaron plegadas.
    public int MigasPlegadas { get; private set; }

    private readonly HashSet<UIElement> _ocultos = [];

    protected override Size MeasureOverride(Size disponible)
    {
        var inf = new Size(double.PositiveInfinity, disponible.Height);
        UIElement? elipsis = null, resumen = null;
        var migas = new List<UIElement>();
        double anchoFijos = 0;
        foreach (UIElement h in InternalChildren)
        {
            h.Measure(inf); // Hidden tambien se mide (solo Collapsed da 0)
            if (h.Visibility == Visibility.Collapsed) continue;
            switch (GetRol(h))
            {
                case RolLineaRuta.Miga: migas.Add(h); break;
                case RolLineaRuta.Elipsis: elipsis ??= h; break;
                case RolLineaRuta.Resumen: resumen ??= h; break;
                default: anchoFijos += h.DesiredSize.Width; break;
            }
        }
        _ocultos.Clear();
        double anchoMigas = migas.Sum(m => m.DesiredSize.Width);
        double anchoElipsis = elipsis?.DesiredSize.Width ?? 0;
        double anchoResumen = resumen?.DesiredSize.Width ?? 0;
        int plegadas = 0;
        if (!double.IsInfinity(disponible.Width))
        {
            while (plegadas < migas.Count - 1 && anchoMigas + (plegadas > 0 ? anchoElipsis : 0) + anchoFijos + anchoResumen > disponible.Width)
            {
                anchoMigas -= migas[plegadas].DesiredSize.Width;
                _ocultos.Add(migas[plegadas]);
                plegadas++;
            }
        }
        if (plegadas == 0 && elipsis != null) _ocultos.Add(elipsis);
        MigasPlegadas = plegadas;
        foreach (var m in migas) m.Visibility = _ocultos.Contains(m) ? Visibility.Hidden : Visibility.Visible;
        if (elipsis != null) elipsis.Visibility = _ocultos.Contains(elipsis) ? Visibility.Hidden : Visibility.Visible;

        double usado = anchoMigas + (plegadas > 0 ? anchoElipsis : 0) + anchoFijos;
        if (resumen != null && !double.IsInfinity(disponible.Width) && usado + anchoResumen > disponible.Width)
        {
            resumen.Measure(new Size(Math.Max(0, disponible.Width - usado), disponible.Height));
            anchoResumen = resumen.DesiredSize.Width;
        }

        double alto = 0;
        foreach (UIElement h in InternalChildren)
            if (h.Visibility != Visibility.Collapsed && !_ocultos.Contains(h)) alto = Math.Max(alto, h.DesiredSize.Height);
        double ancho = usado + anchoResumen;
        if (!double.IsInfinity(disponible.Width)) ancho = Math.Min(ancho, disponible.Width);
        return new Size(ancho, alto);
    }

    protected override Size ArrangeOverride(Size final)
    {
        double x = 0;
        foreach (UIElement h in InternalChildren)
        {
            if (h.Visibility == Visibility.Collapsed || _ocultos.Contains(h)) { h.Arrange(new Rect(0, 0, 0, 0)); continue; }
            double w = GetRol(h) == RolLineaRuta.Resumen ? Math.Max(0, Math.Min(h.DesiredSize.Width, final.Width - x)) : h.DesiredSize.Width;
            double y = Math.Max(0, (final.Height - h.DesiredSize.Height) / 2);
            h.Arrange(new Rect(x, y, w, h.DesiredSize.Height));
            x += w;
        }
        return final;
    }
}
