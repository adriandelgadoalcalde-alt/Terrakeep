using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Terrakeep.App.Controls;

// ADR-TERRAKEEP-016/031 (27-sep-2026): extraido de MainWindow.xaml.cs durante la extraccion final
// de Objetos/ObjetosView (cierra el plan ADR-TERRAKEEP-016). StartCardDrag (arrastre real con
// adorno visual que sigue al cursor) NO se pudo mover entero ni duplicar sin riesgo: lo usan por
// igual los handlers de Objetos (OnLibraryCardMouseMove/OnItemSlotMouseMove, movidos a
// Terrakeep.App/Views/ObjetosView.xaml.cs) Y los gemelos reales de Buffs
// (OnBuffLibraryCardMouseMove/OnBuffSlotMouseMove, que se QUEDAN en MainWindow.xaml.cs porque
// Buffs no se extrae esta ronda) - ni exclusivo de un lado ni trivial de duplicar (30+ lineas, una
// clase Adorner anidada, un P/Invoke). Sin ninguna dependencia real de "this"/_viewModel (helper
// puro de UI, ya lo era en su ubicacion original), se extrae aqui como internal static - visible
// desde MainWindow.xaml.cs Y desde Terrakeep.App.Views.ObjetosView.xaml.cs (mismo ensamblado,
// "internal" no distingue namespace) sin duplicar una sola linea de logica de arrastre.
//
// C-11 (informe de pulido final, cierra L4): "WPF arrastra sin ninguna vista previa, asi
// que el usuario no ve que este llevando nada" - adorno real (VisualBrush de la propia
// tarjeta, semitransparente) que sigue al cursor durante el arrastre, en vez de solo confiar
// en el cursor del sistema (que ya anuncia aceptado/rechazado via OnItemSlotDragOver, pero
// nunca QUE se esta arrastrando).
//
// Bug real confirmado por el usuario (14-sep-2026): el comentario original de este metodo
// decia "compartido por las dos tarjetas reales que inician arrastre - los slots ya
// muestran su propio contenido real en pantalla, la confusion original era especifica de
// las tarjetas". Esa suposicion era FALSA en la practica: OnItemSlotMouseMove/
// OnBuffSlotMouseMove nunca llegaron a llamar a este metodo (llamaban a
// DragDrop.DoDragDrop directamente, dos lineas mas abajo de donde estaba este comentario) -
// asi que arrastrar un objeto o un buff desde su slot solo mostraba el cursor por defecto
// de Windows ("un cuadrado vacio", palabras del usuario), en TODO el programa. El hueco
// vacio SI se nota mientras se arrastra (el slot de origen no se oscurece ni desaparece), y
// el ghost hace tanta falta ahi como en las tarjetas de la Libreria. Se parametriza el
// efecto permitido (Copy|Move para las tarjetas, que SIEMPRE colocan una copia nueva del
// catalogo; solo Move para slot-a-slot, que intercambian el contenido de los dos) en vez de
// duplicar el metodo entero.
// Bug real "drag ghost blanco/vacio en la Libreria" (25-sep-2026, investigador-bug, TASK
// CONTEXT e5eaea9e-c261-4199-8e7d-060b6054f58d): Mouse.GetPosition(element) deja de reflejar
// la posicion real del cursor en cuanto DoDragDrop entra en su bucle modal OLE - WPF cachea
// la ultima posicion conocida del PresentationSource y ese bucle nativo no la actualiza,
// asi que el ghost se congela fuera de la ventana casi de inmediato (confirmado con
// instrumentacion real: 477 de 479 posiciones identicas bit a bit). GetCursorPos (Win32, la
// API real que SI sigue actualizandose durante el bucle OLE) + PointFromScreen es el patron
// estandar para este problema concreto de WPF.
//
// Bug real "el ghost se pierde al arrastrar fuera de la Libreria" (28-sep-2026, reportado por
// el usuario: "según lo vas moviendo para llevarlo al inventario... se lo comen las capas de
// fuera de la libreria"). Causa real confirmada de forma estructural (dump de VisualTreeHelper.
// GetParent desde el AdornerLayer encontrado, sin depender de ninguna captura de pantalla):
// `AdornerLayer.GetAdornerLayer(element)`, cuando `element` es una tarjeta de la Libreria (o
// cualquier slot dentro de un ScrollViewer, que es el caso real de las 3 librerias y de varios
// paneles de slots), NO devuelve la capa de toda la ventana - devuelve una capa LOCAL propia del
// ScrollViewer que contiene al elemento (WPF le da a los descendientes de un ScrollViewer su
// propio AdornerLayer, anidado dentro de ScrollContentPresenter, para que adornos "normales"
// -selección de texto, indicadores de resize- se recorten y se desplacen CON el scroll). Medido
// en vivo: esa capa local medía 815x160px (el viewport visible de la lista de la Libreria) frente
// a los 1164x821px reales de la capa de la ventana completa (la del AdornerDecorator implícito de
// Window, dos instancias DISTINTAS confirmadas con ReferenceEquals). Como ScrollContentPresenter
// SIEMPRE recorta su contenido al viewport (es lo que hace que el scroll funcione), cualquier cosa
// que el DragAdorner dibuje fuera de esos ~160px de alto queda cortada en cuanto el cursor sale de
// la zona visible de la Libreria - exactamente "se lo comen las capas de fuera de la libreria".
// Arreglo: coger la capa desde la RAIZ de la ventana (`Window.GetWindow(element).Content`) en vez
// de desde el propio elemento arrastrado - así el ghost usa SIEMPRE la capa de ventana completa,
// tenga o no el elemento un ScrollViewer por encima, con el mismo fallback de antes (al propio
// elemento) si no hay ninguna Window real todavía.
internal static class DragDropSupport
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Win32Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Win32Point point);

    internal static void StartCardDrag(FrameworkElement element, DataObject data, DragDropEffects allowedEffects = DragDropEffects.Copy | DragDropEffects.Move)
    {
        // Capa de la VENTANA COMPLETA, nunca la capa local (mas pequeña) de un ScrollViewer
        // intermedio - ver el hallazgo del 28-sep-2026 arriba. AdornedElement sigue siendo el
        // propio `element` (mantiene el mismo espacio de coordenadas que UpdatePosition, que sigue
        // usando `element.PointFromScreen`), solo cambia DE QUE CAPA cuelga el adorno.
        var rootVisual = (Visual?)Window.GetWindow(element)?.Content;
        var layer = (rootVisual != null ? AdornerLayer.GetAdornerLayer(rootVisual) : null)
            ?? AdornerLayer.GetAdornerLayer(element);
        if (layer == null) { DragDrop.DoDragDrop(element, data, allowedEffects); return; }

        var adorner = DragGhost.CrearAdorner(element);
        GetCursorPos(out var inicio);
        adorner.UpdatePosition(element.PointFromScreen(new Point(inicio.X, inicio.Y)));
        layer.Add(adorner);
        void OnFeedback(object? s, GiveFeedbackEventArgs e)
        {
            GetCursorPos(out var screenPt);
            // Punta del cursor (hotspot) en coordenadas del elemento - misma conversion
            // GetCursorPos+PointFromScreen de siempre (DPI incluido); el desplazamiento relativo
            // (sprite ENCIMA de la punta) lo decide DragGhost.CalcularRectGhost.
            adorner.UpdatePosition(element.PointFromScreen(new Point(screenPt.X, screenPt.Y)));
        }
        element.GiveFeedback += OnFeedback;
        try
        {
            DragDrop.DoDragDrop(element, data, allowedEffects);
        }
        finally
        {
            element.GiveFeedback -= OnFeedback;
            layer.Remove(adorner);
        }
    }

}

// Pedido del usuario (28-sep-2026): "ahora puedes quitar el recuadro blanco o sea ocultarlo y que
// solo se vea el sprite, además hay que ponerlo que esté por encima de la punta del mouse y hay que
// hacer que los sprites arrastrados se vean algo más grandes y ya quedará perfecto".
// Antes: el adorno pintaba un VisualBrush de la tarjeta/slot ENTERO (fondo BgElevated, borde,
// boton "Colocar" si estaba visible...) a +12,+12 del cursor (abajo a la derecha, tapado en parte
// por la propia flecha) y al mismo tamaño que la tarjeta. Ahora:
//  1. Solo el sprite: se busca el Image real visible con Source dentro del elemento arrastrado
//     (tarjeta de Libreria de objetos/buffs o slot de inventario/buffs - los 4 usan un Image con
//     IconPath) y se dibuja SOLO su ImageSource con DrawImage, sin rectangulo, fondo ni borde.
//     Si no hubiera ningun Image (no pasa hoy en ningun origen real), se cae al VisualBrush de
//     antes para no quedarse sin ghost.
//  2. Encima de la punta: centrado en horizontal sobre el hotspot y con su borde inferior
//     SeparacionSobreCursorPx por encima - la flecha nunca lo tapa.
//  3. Mas grande: EscalaSprite x el tamaño con que se ve el sprite en la tarjeta, con un minimo de
//     LadoMinimoPx en el lado mayor, escalado por vecino mas cercano (pixel art nitido).
// Publico (no internal) SOLO para que el arnes Terrakeep.App.Tests (sin InternalsVisibleTo,
// criterio ya establecido en el proyecto) pueda ejercitar la MISMA geometria/adorno de produccion.
public static class DragGhost
{
    /// <summary>Factor de escala del sprite arrastrado respecto a como se ve en la tarjeta/slot.</summary>
    public const double EscalaSprite = 1.5;
    /// <summary>Tamaño minimo (px logicos) del lado mayor del sprite arrastrado.</summary>
    public const double LadoMinimoPx = 48;
    /// <summary>Separacion (px logicos) entre el borde inferior del sprite y la punta del cursor.</summary>
    public const double SeparacionSobreCursorPx = 6;
    /// <summary>Opacidad del sprite arrastrado (ligera transparencia para ver el destino debajo).</summary>
    public const double OpacidadSprite = 0.85;

    /// <summary>Tamaño final del sprite arrastrado a partir del tamaño con que se ve en pantalla.</summary>
    public static Size CalcularTamañoSprite(Size mostrado)
    {
        if (mostrado.Width <= 0 || mostrado.Height <= 0) return new Size(LadoMinimoPx, LadoMinimoPx);
        double escala = EscalaSprite;
        double ladoMayor = Math.Max(mostrado.Width, mostrado.Height);
        if (ladoMayor * escala < LadoMinimoPx) escala = LadoMinimoPx / ladoMayor;
        return new Size(mostrado.Width * escala, mostrado.Height * escala);
    }

    /// <summary>Rectangulo del sprite: centrado en X sobre la punta del cursor y con su borde
    /// inferior SeparacionSobreCursorPx por encima de ella.</summary>
    public static Rect CalcularRectGhost(Point puntaCursor, Size sprite) =>
        new(puntaCursor.X - sprite.Width / 2, puntaCursor.Y - SeparacionSobreCursorPx - sprite.Height, sprite.Width, sprite.Height);

    /// <summary>Image real del sprite dentro del elemento arrastrado: visible, con Source, y la de
    /// mayor area renderizada (en un slot de objetos, el icono real frente al "fantasma" de hueco
    /// vacio, que ademas esta colapsado cuando el slot tiene objeto).</summary>
    public static System.Windows.Controls.Image? BuscarSprite(DependencyObject raiz)
    {
        System.Windows.Controls.Image? mejor = null;
        double mejorArea = 0;
        var pendientes = new System.Collections.Generic.Stack<DependencyObject>();
        pendientes.Push(raiz);
        while (pendientes.Count > 0)
        {
            var actual = pendientes.Pop();
            if (actual is System.Windows.Controls.Image img && img.Source != null && img.IsVisible)
            {
                double area = img.ActualWidth * img.ActualHeight;
                if (area > mejorArea) { mejor = img; mejorArea = area; }
            }
            int n = VisualTreeHelper.GetChildrenCount(actual);
            for (int i = 0; i < n; i++) pendientes.Push(VisualTreeHelper.GetChild(actual, i));
        }
        return mejor;
    }

    /// <summary>Crea el adorno de arrastre para `arrastrado` (el mismo que usa StartCardDrag).</summary>
    public static SpriteAdorner CrearAdorner(FrameworkElement arrastrado)
    {
        var sprite = BuscarSprite(arrastrado);
        return sprite != null
            ? new SpriteAdorner(arrastrado, sprite.Source, CalcularTamañoSprite(new Size(sprite.ActualWidth, sprite.ActualHeight)))
            : new SpriteAdorner(arrastrado, new VisualBrush(arrastrado) { Stretch = Stretch.Uniform }, new Size(arrastrado.ActualWidth, arrastrado.ActualHeight));
    }

    /// <summary>Adorno que dibuja SOLO el sprite (sin caja) encima de la punta del cursor.
    /// IsHitTestVisible en False para no interferir con el propio Drop.</summary>
    public sealed class SpriteAdorner : Adorner
    {
        private readonly ImageSource? _sprite;
        private readonly Brush? _fallback;

        /// <summary>Tamaño final (ya escalado) con que se dibuja el sprite.</summary>
        public Size TamañoSprite { get; }
        /// <summary>Rectangulo actual (coordenadas del elemento adornado) donde se dibuja.</summary>
        public Rect RectActual { get; private set; }
        /// <summary>True si se encontro un Image real y se dibuja solo su ImageSource.</summary>
        public bool DibujaSoloSprite => _sprite != null;

        public SpriteAdorner(UIElement adornado, ImageSource sprite, Size tamaño) : base(adornado)
        {
            _sprite = sprite;
            TamañoSprite = tamaño;
            Iniciar();
        }

        internal SpriteAdorner(UIElement adornado, Brush fallback, Size tamaño) : base(adornado)
        {
            _fallback = fallback;
            TamañoSprite = tamaño;
            Iniciar();
        }

        private void Iniciar()
        {
            IsHitTestVisible = false;
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
            RectActual = DragGhost.CalcularRectGhost(new Point(0, 0), TamañoSprite);
        }

        /// <summary>Recoloca el sprite encima de la punta del cursor (coordenadas del elemento adornado).</summary>
        public void UpdatePosition(Point puntaCursor)
        {
            RectActual = DragGhost.CalcularRectGhost(puntaCursor, TamañoSprite);
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.PushOpacity(OpacidadSprite);
            if (_sprite != null) drawingContext.DrawImage(_sprite, RectActual);
            else if (_fallback != null) drawingContext.DrawRectangle(_fallback, null, RectActual);
            drawingContext.Pop();
        }
    }
}
