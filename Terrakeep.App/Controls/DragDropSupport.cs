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

        var adorner = new DragAdorner(element, element);
        layer.Add(adorner);
        void OnFeedback(object? s, GiveFeedbackEventArgs e)
        {
            GetCursorPos(out var screenPt);
            var pos = element.PointFromScreen(new Point(screenPt.X, screenPt.Y));
            adorner.UpdatePosition(pos.X + 12, pos.Y + 12);
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

    // VisualBrush de la tarjeta original, dibujado en la posicion real del cursor - IsHitTestVisible
    // en False para no interferir con el propio Drop (el adorno vive en una capa aparte, por
    // encima de todo el arbol visual de la ventana, pero nunca debe recibir eventos de raton).
    private sealed class DragAdorner : Adorner
    {
        private readonly VisualBrush _brush;
        private readonly double _width, _height;
        private double _left, _top;

        public DragAdorner(UIElement adornedElement, FrameworkElement dragged) : base(adornedElement)
        {
            _brush = new VisualBrush(dragged) { Opacity = 0.75, Stretch = Stretch.Uniform };
            _width = dragged.ActualWidth;
            _height = dragged.ActualHeight;
            IsHitTestVisible = false;
        }

        public void UpdatePosition(double left, double top)
        {
            _left = left;
            _top = top;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext) =>
            drawingContext.DrawRectangle(_brush, null, new Rect(_left, _top, _width, _height));
    }
}
