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
        var window = Window.GetWindow(element);
        var rootVisual = (Visual?)window?.Content;
        var layer = (rootVisual != null ? AdornerLayer.GetAdornerLayer(rootVisual) : null)
            ?? AdornerLayer.GetAdornerLayer(element);

        DragGhost.SpriteAdorner? adorner = null;
        if (layer != null)
        {
            adorner = DragGhost.CrearAdorner(element);
            GetCursorPos(out var inicio);
            adorner.UpdatePosition(element.PointFromScreen(new Point(inicio.X, inicio.Y)));
            layer.Add(adorner);
        }

        // Pedido del usuario (28-sep-2026, con foto real): el cursor OLE por defecto (flecha con el
        // recuadro punteado de "mover/copiar") desaparece en TODO el arrastre - se sustituye por la
        // mano que agarra (o su variante "no se puede soltar aqui" cuando el destino devuelve None).
        var cursorMano = DragCursors.Agarrar(element);
        var cursorNo = DragCursors.NoPermitido(element);
        void OnFeedback(object? s, GiveFeedbackEventArgs e)
        {
            if (adorner != null)
            {
                GetCursorPos(out var screenPt);
                // Hotspot del cursor (centro de la palma) en coordenadas del elemento - misma
                // conversion GetCursorPos+PointFromScreen de siempre (DPI incluido); la colocacion
                // relativa (sprite CENTRADO en el hotspot) la decide DragGhost.CalcularRectGhost.
                adorner.UpdatePosition(element.PointFromScreen(new Point(screenPt.X, screenPt.Y)));
            }
            DragCursors.AplicarFeedback(e, cursorMano, cursorNo);
        }
        // Zonas sin destino real (fondo, cabeceras, otras pestañas, la propia Libreria): la ventana
        // tiene AllowDrop=True (solo para soltar .plr/.wld, OnWindowDrop) y sin un DragOver propio
        // WPF devolvia el efecto PERMITIDO por el origen - el cursor anunciaba "se puede soltar"
        // donde soltar no hace nada. Mientras dura ESTE arrastre, lo que ningun slot haya marcado
        // como Handled (OnItemSlotDragOver/OnBuffSlotDragOver si lo hacen) pasa a None. Nunca toca
        // un arrastre de ficheros desde el Explorador (FileDrop).
        void OnDragOverSinDestino(object s, DragEventArgs e)
        {
            if (e.Handled || e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
        element.GiveFeedback += OnFeedback;
        if (window != null) window.DragOver += OnDragOverSinDestino;
        try
        {
            DragDrop.DoDragDrop(element, data, allowedEffects);
        }
        finally
        {
            element.GiveFeedback -= OnFeedback;
            if (window != null) window.DragOver -= OnDragOverSinDestino;
            if (adorner != null) layer!.Remove(adorner);
            // Termine como termine (soltar, Esc, soltar fuera de la ventana): fuera la mano.
            DragCursors.Restaurar();
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
//  2. (Sustituido el 28-sep-2026, segunda ronda) Antes: encima de la punta de la flecha. Ahora el
//     cursor es una mano que agarra (DragCursors) y el sprite va CENTRADO en su hotspot (centro de
//     la palma, OffsetSobreHotspot = 0,0) - "que la mano haga que lo agarra por el centro".
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
    /// <summary>Desplazamiento (px logicos) del CENTRO del sprite respecto al hotspot del cursor de
    /// mano (centro de la palma). 0,0 = la mano "agarra" el objeto por su centro (pedido del usuario
    /// 28-sep-2026: que la mano tape parte del objeto es aceptable). Ajustable aqui sin tocar nada
    /// mas.</summary>
    public static readonly Vector OffsetSobreHotspot = new(0, 0);
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

    /// <summary>Rectangulo del sprite: su CENTRO en el hotspot del cursor (+ OffsetSobreHotspot).</summary>
    public static Rect CalcularRectGhost(Point hotspot, Size sprite) =>
        new(hotspot.X + OffsetSobreHotspot.X - sprite.Width / 2, hotspot.Y + OffsetSobreHotspot.Y - sprite.Height / 2, sprite.Width, sprite.Height);

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

    /// <summary>Adorno que dibuja SOLO el sprite (sin caja) centrado en el hotspot del cursor de mano.
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

        /// <summary>Recoloca el sprite centrado en el hotspot del cursor (coordenadas del elemento adornado).</summary>
        public void UpdatePosition(Point hotspot)
        {
            RectActual = DragGhost.CalcularRectGhost(hotspot, TamañoSprite);
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

// Pedido del usuario (28-sep-2026, con foto real del arrastre): "cuando coges algo, que cambie de
// ratón a una mano, hacer que esa mano haga que lo agarra por el centro el objeto... Y hay que
// ocultar ese pequeño cuadrado que hay al lado del ratón". El "pequeño cuadrado" es el cursor OLE
// por defecto de DragDrop.DoDragDrop (flecha + recuadro punteado de mover/copiar). WPF no trae un
// cursor de mano cerrada (Cursors.Hand es la mano que SEÑALA), asi que se incrustan dos .cur
// propios (Assets/cursors/, generados con scripts/generar-cursor-mano/): mano que agarra y la misma
// mano con el disco rojo de "no se puede soltar aqui". Cada .cur trae 32/40/48/64/96 px con el
// hotspot en el centro de la palma (16,18 en 32 px); se elige la imagen del tamaño de cursor real
// del sistema para el DPI del monitor (GetSystemMetricsForDpi(SM_CXCURSOR)), sin reescalar.
// Se carga con CreateIconFromResourceEx en vez de `new Cursor(Stream)` para CONSERVAR el HCURSOR:
// el arnes (CanarioDragCursorMano) lo compara con GetCursor()/GetCursorInfo durante un arrastre
// real. Publico (no internal) por el mismo motivo que DragGhost: el arnes no tiene InternalsVisibleTo.
public static class DragCursors
{
    public const string RecursoAgarrar = "Terrakeep.Cursores.mano-agarrar.cur";
    public const string RecursoNoPermitido = "Terrakeep.Cursores.mano-agarrar-no.cur";

    private static readonly System.Collections.Generic.Dictionary<(string, int), (Cursor cursor, IntPtr handle, int lado, Point hotspot)> _cache = new();

    /// <summary>HCURSOR de la mano que agarra usada por el ultimo arrastre (0 si aun no se cargo).</summary>
    public static IntPtr HandleAgarrarActual { get; private set; }
    /// <summary>HCURSOR de la variante "no se puede soltar aqui" usada por el ultimo arrastre.</summary>
    public static IntPtr HandleNoPermitidoActual { get; private set; }
    /// <summary>Lado (px fisicos) de la imagen del .cur elegida en la ultima carga.</summary>
    public static int LadoActual { get; private set; }
    /// <summary>Hotspot (px fisicos, dentro de la imagen elegida) de la ultima carga.</summary>
    public static Point HotspotActual { get; private set; }

    public static Cursor Agarrar(Visual? contexto)
    {
        var (c, h) = Cargar(RecursoAgarrar, contexto);
        HandleAgarrarActual = h;
        return c;
    }

    public static Cursor NoPermitido(Visual? contexto)
    {
        var (c, h) = Cargar(RecursoNoPermitido, contexto);
        HandleNoPermitidoActual = h;
        return c;
    }

    /// <summary>Lo que hace el GiveFeedback de StartCardDrag: nunca el cursor OLE por defecto;
    /// mano si el destino acepta algo, "no se puede" si devuelve None.</summary>
    public static void AplicarFeedback(GiveFeedbackEventArgs e, Cursor mano, Cursor noPermitido)
    {
        bool acepta = (e.Effects & (DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link)) != 0;
        e.UseDefaultCursors = false;
        Mouse.SetCursor(acepta ? mano : noPermitido);
        e.Handled = true;
    }

    /// <summary>Fin del arrastre (soltar, Esc o soltar fuera de la ventana): flecha normal y que WPF
    /// vuelva a aplicar el cursor real del elemento que haya debajo, si es de esta ventana.</summary>
    public static void Restaurar()
    {
        SetCursor(LoadCursor(IntPtr.Zero, IDC_ARROW));
        Mouse.UpdateCursor();
    }

    private static (Cursor, IntPtr) Cargar(string recurso, Visual? contexto)
    {
        int objetivo = LadoObjetivo(contexto);
        lock (_cache)
        {
            if (_cache.TryGetValue((recurso, objetivo), out var enCache))
            {
                LadoActual = enCache.lado; HotspotActual = enCache.hotspot;
                return (enCache.cursor, enCache.handle);
            }
            byte[] cur;
            using (var s = typeof(DragCursors).Assembly.GetManifestResourceStream(recurso))
            {
                if (s == null) return (Cursors.Hand, IntPtr.Zero);
                using var ms = new System.IO.MemoryStream();
                s.CopyTo(ms);
                cur = ms.ToArray();
            }
            var (lado, hx, hy, datos) = ElegirImagen(cur, objetivo);
            if (datos == null) return (Cursors.Hand, IntPtr.Zero);
            // Formato de recurso RT_CURSOR: WORD hotspotX + WORD hotspotY + DIB.
            var buf = new byte[4 + datos.Length];
            BitConverter.GetBytes((ushort)hx).CopyTo(buf, 0);
            BitConverter.GetBytes((ushort)hy).CopyTo(buf, 2);
            datos.CopyTo(buf, 4);
            IntPtr h = CreateIconFromResourceEx(buf, (uint)buf.Length, false, 0x00030000, lado, lado, 0);
            if (h == IntPtr.Zero) return (Cursors.Hand, IntPtr.Zero);
            var cursor = System.Windows.Interop.CursorInteropHelper.Create(new HandleCursor(h));
            _cache[(recurso, objetivo)] = (cursor, h, lado, new Point(hx, hy));
            LadoActual = lado;
            HotspotActual = new Point(hx, hy);
            return (cursor, h);
        }
    }

    private static int LadoObjetivo(Visual? contexto)
    {
        double escala = 1;
        try { if (contexto != null) escala = VisualTreeHelper.GetDpi(contexto).DpiScaleX; } catch (InvalidOperationException) { }
        int lado = 0;
        try { lado = GetSystemMetricsForDpi(SM_CXCURSOR, (uint)Math.Round(96 * escala)); } catch (EntryPointNotFoundException) { }
        return lado > 0 ? lado : (int)Math.Round(32 * escala);
    }

    /// <summary>Elige del .cur la imagen mas pequeña con lado &gt;= objetivo (o la mayor si ninguna llega).</summary>
    private static (int lado, int hx, int hy, byte[]? datos) ElegirImagen(byte[] cur, int objetivo)
    {
        if (cur.Length < 6 || BitConverter.ToUInt16(cur, 2) != 2) return (0, 0, 0, null);
        int n = BitConverter.ToUInt16(cur, 4);
        int mejor = -1, mejorLado = 0;
        for (int i = 0; i < n; i++)
        {
            int lado = cur[6 + 16 * i]; if (lado == 0) lado = 256;
            bool llega = lado >= objetivo, mejorLlega = mejorLado >= objetivo;
            if (mejor < 0 || (llega && (!mejorLlega || lado < mejorLado)) || (!llega && !mejorLlega && lado > mejorLado)) { mejor = i; mejorLado = lado; }
        }
        if (mejor < 0) return (0, 0, 0, null);
        int e = 6 + 16 * mejor;
        int hx = BitConverter.ToUInt16(cur, e + 4), hy = BitConverter.ToUInt16(cur, e + 6);
        int tam = BitConverter.ToInt32(cur, e + 8), off = BitConverter.ToInt32(cur, e + 12);
        if (off < 0 || tam <= 0 || off + tam > cur.Length) return (0, 0, 0, null);
        var datos = new byte[tam];
        Array.Copy(cur, off, datos, 0, tam);
        return (mejorLado, hx, hy, datos);
    }

    private sealed class HandleCursor : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public HandleCursor(IntPtr h) : base(true) { SetHandle(h); }
        protected override bool ReleaseHandle() => DestroyCursor(handle);
    }

    private const int SM_CXCURSOR = 13;
    private static readonly IntPtr IDC_ARROW = new(32512);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateIconFromResourceEx(byte[] presbits, uint dwResSize, bool fIcon, uint dwVer, int cxDesired, int cyDesired, uint flags);
    [DllImport("user32.dll")] private static extern bool DestroyCursor(IntPtr hCursor);
    [DllImport("user32.dll")] private static extern int GetSystemMetricsForDpi(int nIndex, uint dpi);
    [DllImport("user32.dll")] private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);
    [DllImport("user32.dll")] private static extern IntPtr SetCursor(IntPtr hCursor);
}
