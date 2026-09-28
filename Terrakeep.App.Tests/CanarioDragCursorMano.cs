// CANARIO (28-sep-2026, segunda ronda del ghost de arrastre). Pedido del usuario con foto real:
// "cuando coges algo, que cambie de ratón a una mano, hacer que esa mano haga que lo agarra por el
// centro el objeto... Y hay que ocultar ese pequeño cuadrado que hay al lado del ratón". Ampliado por
// el coordinador el mismo dia: tiene que valer en TODOS los arrastres de la pagina (Librerias,
// Equipamiento, Inventario, Almacenes) y en todos los destinos (slot que acepta, slot que rechaza,
// zonas sin destino, fuera de la ventana).
//
// Arrastres REALES (SetCursorPos + mouse_event desde un hilo aparte, mismo mecanismo que
// EjecutarDragGhostLibreriaSolo) desde elementos reales de la ventana; en cada uno se mide, con
// fuentes INDEPENDIENTES del codigo de produccion:
//  1. En cada GiveFeedback (suscriptor en la ventana con handledEventsToo, corre DESPUES del de
//     produccion en el mismo evento burbujeante): UseDefaultCursors debe ser False (si fuera True,
//     OLE pintaria la flecha con el recuadro) y GetCursor() (Win32, cursor real del hilo de UI) debe
//     ser el HCURSOR de la mano si el efecto acepta algo, o el de "no se puede" si es None.
//  2. Centro del SpriteAdorner real (el que StartCardDrag cuelga de la capa de la ventana) frente a
//     GetCursorPos+PointFromScreen: desviacion <= 1 px.
//  3. Desde el hilo de inyeccion, GetCursorInfo (cursor GLOBAL que Windows esta mostrando de verdad)
//     sostenido sobre el destino, y otra vez tras soltar/Esc: al final NO debe ser ninguno de los dos
//     cursores propios (restaurado).
//  4. Captura real de pantalla (BitBlt del escritorio + DrawIconEx del cursor que dice GetCursorInfo,
//     igual que hace cualquier grabador de pantalla) sostenida sobre el destino - evidencia visual.
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Terrakeep.App;
using Terrakeep.App.Controls;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private sealed class ResultadoArrastreMano
    {
        public string Etiqueta = "";
        public int Feedbacks, ConDefaultCursors, CursorIncorrecto, ConMano, ConNo;
        public int DesvioMedidas, DesvioFuera;
        public double DesvioMax;
        public string? UltimoEfecto, UltimoBajoCursor;
        public Point? SeguidoPrimero, SeguidoUltimo;
        public IntPtr CursorGlobalSostenido, CursorGlobalFuera, CursorGlobalFinal;
        public string? Captura;
        public List<string> Incidencias = new();
    }

    private static void EjecutarCursorManoArrastres(MainWindow window, MainViewModel vm)
    {
        var fallos = new List<string>();
        var resultados = new List<ResultadoArrastreMano>();
        try
        {
            string dir = CarpetaEvidenciaKeepQa();
            GuardarCursoresEvidencia(window, dir);

            // --- A) Libreria -> slot de Inventario (sostener: mano) -> Esc ---
            vm.RequestObjetosSection(1); DoEvents(); DoEvents(); DoEvents();
            var tarjeta = BuscarTarjetaLibreria(window, vm);
            var slotsInv = SlotsVisibles(window, vm.InventoryContainer);
            if (tarjeta == null || slotsInv.Count < 2) fallos.Add($"A) sin tarjeta de Libreria ({tarjeta != null}) o sin slots de Inventario visibles ({slotsInv.Count})");
            else
            {
                var destino = slotsInv.FirstOrDefault(b => ((ItemSlotViewModel)b.DataContext).IsEmpty) ?? slotsInv[^1];
                var r = ArrastreRealMano(window, tarjeta, Centro(destino), null, true, "libreria->inventario(Esc)", "drag-cursor-vivo-libreria-sobre-slot.png");
                resultados.Add(r);
                if (r.ConMano == 0) fallos.Add("A) sobre un slot de Inventario que acepta no se vio NUNCA la mano");
                if (r.CursorGlobalSostenido != DragCursors.HandleAgarrarActual) fallos.Add($"A) cursor global sostenido sobre el slot = 0x{r.CursorGlobalSostenido:X} != mano 0x{DragCursors.HandleAgarrarActual:X}");
            }

            // --- B) Inventario -> otro slot de Inventario, soltar de verdad (intercambio) ---
            // Pulsar un slot lo SELECCIONA (panel Editar) y eso recoloca la rejilla: el origen se
            // preselecciona antes y el destino se calcula ya con el diseño asentado.
            (var origenInv, slotsInv) = PreseleccionarOrigen(window, vm, vm.InventoryContainer);
            if (origenInv == null) fallos.Add("B) no hay ningun slot de Inventario con objeto visible");
            else
            {
                var destino = DestinoQueAcepta(slotsInv, origenInv, preferirVacio: true);
                var so = (ItemSlotViewModel)origenInv.DataContext; var sd = (ItemSlotViewModel)destino.DataContext;
                Console.WriteLine($"DRAG_CURSOR_MANO[B]: origen slot {so.SlotIndex} (id {so.Item.Id}) -> destino slot {sd.SlotIndex} (id {sd.Item.Id}, vacio={sd.IsEmpty}) ; acepta destino={sd.AcceptsItem(so.Item.Id)} acepta origen={so.AcceptsItem(sd.Item.Id)} ; destino alcanzable={CentroAlcanzable(window, destino)}");
                var antes = Centro(destino);
                var r = ArrastreRealMano(window, origenInv, antes, null, false, "inventario->inventario(soltar)", "drag-cursor-vivo-inventario-sobre-slot.png", destino);
                resultados.Add(r);
                Console.WriteLine($"DRAG_CURSOR_MANO[B]: centro del destino en pantalla antes=({antes.X:0},{antes.Y:0}) primer GiveFeedback=({r.SeguidoPrimero?.X:0},{r.SeguidoPrimero?.Y:0}) ultimo GiveFeedback=({r.SeguidoUltimo?.X:0},{r.SeguidoUltimo?.Y:0}) despues=({Centro(destino).X:0},{Centro(destino).Y:0}) ; foco ahora={Keyboard.FocusedElement?.GetType().Name}");
                if (r.CursorGlobalSostenido != DragCursors.HandleAgarrarActual) fallos.Add($"B) cursor global sostenido sobre el slot = 0x{r.CursorGlobalSostenido:X} != mano");
            }

            // --- C) Inventario -> zona sin destino (fondo de la cabecera) -> soltar: "no se puede" ---
            (origenInv, slotsInv) = PreseleccionarOrigen(window, vm, vm.InventoryContainer);
            if (origenInv != null)
            {
                var fondo = ZonaSinDestino(window);
                if (fondo == null) fallos.Add("C) no se encontro ninguna zona sin destino dentro de la ventana");
                else
                {
                    var r = ArrastreRealMano(window, origenInv, fondo.Value, null, false, "inventario->fondo(soltar)", "drag-cursor-vivo-sobre-fondo.png");
                    resultados.Add(r);
                    if (r.ConNo == 0) fallos.Add("C) sobre una zona sin destino no se vio NUNCA el cursor de 'no se puede'");
                    if (r.CursorGlobalSostenido != DragCursors.HandleNoPermitidoActual) fallos.Add($"C) cursor global sostenido sobre el fondo = 0x{r.CursorGlobalSostenido:X} != 'no se puede' 0x{DragCursors.HandleNoPermitidoActual:X}");
                }
            }

            // --- D) Equipamiento -> otro slot de Equipamiento -> FUERA de la ventana -> soltar ---
            vm.RequestObjetosSection(0); DoEvents(); DoEvents(); DoEvents();
            var (origenEq, slotsEq) = PreseleccionarOrigen(window, vm, vm.EquipmentGroup?.CurrentItems, vm.EquipmentGroup?.CurrentSocial, vm.EquipmentGroup?.CurrentDyes);
            if (origenEq == null) fallos.Add($"D) no hay ningun slot de Equipamiento con objeto visible ({slotsEq.Count} slots visibles)");
            else
            {
                var intermedio = DestinoQueAcepta(slotsEq, origenEq, preferirVacio: false);
                bool intermedioAcepta = Acepta(origenEq, intermedio);
                var fuera = PuntoFueraDeVentana(window);
                var r = ArrastreRealMano(window, origenEq, Centro(intermedio), fuera, false, "equipamiento->equipamiento->fuera(soltar)", "drag-cursor-vivo-equipamiento-sobre-slot.png");
                resultados.Add(r);
                var esperadoIntermedio = intermedioAcepta ? DragCursors.HandleAgarrarActual : DragCursors.HandleNoPermitidoActual;
                if (r.CursorGlobalSostenido != esperadoIntermedio) fallos.Add($"D) sobre el slot de Equipamiento intermedio (acepta={intermedioAcepta}) el cursor global = 0x{r.CursorGlobalSostenido:X} != 0x{esperadoIntermedio:X}");
                if (fuera == null) r.Incidencias.Add("sin sitio en pantalla para salir de la ventana; solo se prueba el slot intermedio");
                else if (r.CursorGlobalFuera != DragCursors.HandleNoPermitidoActual) fallos.Add($"D) fuera de la ventana el cursor global = 0x{r.CursorGlobalFuera:X} != 'no se puede' 0x{DragCursors.HandleNoPermitidoActual:X}");
            }

            // --- E) Almacenes -> otro slot del mismo almacen -> Esc ---
            vm.RequestObjetosSection(2); DoEvents(); DoEvents(); DoEvents();
            var almacen = vm.StorageGroup?.Current;
            var slotsAlm = SlotsVisibles(window, almacen);
            if (slotsAlm.Count >= 2 && !slotsAlm.Any(b => ((ItemSlotViewModel)b.DataContext).IsNotEmpty) && vm.Library.Results.Count > 0)
            {
                // Almacen vacio en este personaje: se coloca un objeto real del catalogo (solo en
                // memoria, el arnes nunca guarda) para tener algo que arrastrar.
                ((ItemSlotViewModel)slotsAlm[0].DataContext).PlaceItem(vm.Library.Results[0].Id);
                DoEvents(); DoEvents();
                slotsAlm = SlotsVisibles(window, almacen);
            }
            (var origenAlm, slotsAlm) = PreseleccionarOrigen(window, vm, almacen);
            if (origenAlm == null) fallos.Add($"E) no hay ningun slot de Almacen con objeto visible ({slotsAlm.Count} slots visibles)");
            else
            {
                var destino = DestinoQueAcepta(slotsAlm, origenAlm, preferirVacio: true);
                var r = ArrastreRealMano(window, origenAlm, Centro(destino), null, true, "almacen->almacen(Esc)", "drag-cursor-vivo-almacen-sobre-slot.png");
                resultados.Add(r);
                if (r.CursorGlobalSostenido != DragCursors.HandleAgarrarActual) fallos.Add($"E) cursor global sostenido sobre el slot = 0x{r.CursorGlobalSostenido:X} != mano");
            }

            vm.RequestObjetosSection(1); DoEvents();
        }
        catch (Exception ex) { fallos.Add("EXCEPCION: " + ex); }

        // Criterios comunes a todos los arrastres reales.
        foreach (var r in resultados)
        {
            Console.WriteLine($"DRAG_CURSOR_MANO[{r.Etiqueta}]: GiveFeedback={r.Feedbacks} ; UseDefaultCursors=True en {r.ConDefaultCursors} ; cursor del hilo distinto del esperado en {r.CursorIncorrecto} ; con mano={r.ConMano} ; con 'no se puede'={r.ConNo} ; ultimo efecto={r.UltimoEfecto} bajo el cursor={r.UltimoBajoCursor} ; centro sprite-hotspot: max {r.DesvioMax:0.##}px, {r.DesvioFuera}/{r.DesvioMedidas} medidas >1px ; cursor global sostenido=0x{r.CursorGlobalSostenido:X} fuera=0x{r.CursorGlobalFuera:X} final=0x{r.CursorGlobalFinal:X}" + (r.Captura != null ? $" ; captura={r.Captura}" : "") + (r.Incidencias.Count > 0 ? " ; " + string.Join(" ; ", r.Incidencias) : ""));
            if (r.Feedbacks == 0) { fallos.Add($"[{r.Etiqueta}] GiveFeedback no se disparo nunca - el arrastre real no arranco"); continue; }
            if (r.ConDefaultCursors > 0) fallos.Add($"[{r.Etiqueta}] {r.ConDefaultCursors} GiveFeedback con UseDefaultCursors=True - reaparece la flecha con el recuadro");
            if (r.CursorIncorrecto > 0) fallos.Add($"[{r.Etiqueta}] {r.CursorIncorrecto} GiveFeedback con un cursor que no es el propio esperado para su efecto");
            if (r.DesvioMedidas == 0) fallos.Add($"[{r.Etiqueta}] no se encontro el SpriteAdorner en la capa de la ventana durante el arrastre");
            else if (r.DesvioFuera > Math.Max(2, r.DesvioMedidas / 20)) fallos.Add($"[{r.Etiqueta}] el centro del sprite se separo >1px del hotspot en {r.DesvioFuera}/{r.DesvioMedidas} medidas (max {r.DesvioMax:0.##}px)");
            if (r.CursorGlobalFinal == DragCursors.HandleAgarrarActual || r.CursorGlobalFinal == DragCursors.HandleNoPermitidoActual)
                fallos.Add($"[{r.Etiqueta}] al terminar el arrastre el cursor global sigue siendo el propio (0x{r.CursorGlobalFinal:X}) - no se restauro");
        }
        if (resultados.Sum(r => r.ConMano) == 0 || resultados.Sum(r => r.ConNo) == 0)
            fallos.Add($"no se observaron las dos variantes del cursor (mano={resultados.Sum(r => r.ConMano)}, no={resultados.Sum(r => r.ConNo)})");

        if (fallos.Count == 0) Console.WriteLine($"DRAG_CURSOR_MANO: OK - {resultados.Count} arrastres reales (Libreria, Inventario, fondo, Equipamiento+fuera de ventana, Almacenes): nunca el cursor OLE por defecto, mano sobre destinos que aceptan, 'no se puede' donde no, sprite centrado en el hotspot y cursor restaurado al terminar (soltar, Esc y fuera de la ventana)");
        else foreach (var f in fallos) Console.WriteLine("FALLO: DRAG_CURSOR_MANO - " + f);
    }

    private static (FrameworkElement? origen, List<FrameworkElement> slots) PreseleccionarOrigen(Window window, MainViewModel vm, params ContainerViewModel?[] contenedores)
    {
        List<FrameworkElement> Todos() => contenedores.SelectMany(c => SlotsVisibles(window, c)).ToList();
        var slot = Todos().Select(b => (ItemSlotViewModel)b.DataContext).FirstOrDefault(s => s.IsNotEmpty);
        if (slot == null) return (null, Todos());
        // Seleccionar el origen SI cambia de verdad el diseño (el panel Editar enseña ese objeto), asi que se
        // hace antes de calcular el destino. Lo que YA NO se hace (pasada completa con raton real, 28-sep-2026):
        // el Focus() y el clic real previo sobre el origen "para dejar el scroll asentado" - enmascaraban justo
        // el salto que tenia que ver este canario (la pagina se desplazaba 9,8px al bajar el boton sobre un slot
        // medio tapado, ObjetosView.OnItemSlotMouseLeftButtonDown). Arreglado en produccion, el mouse-down del
        // propio arrastre no puede mover nada; si vuelve, el destino calculado aqui dejara de estar bajo el
        // cursor y el canario lo vera (cursor/efecto sobre el destino).
        vm.SelectSlot(slot);
        DoEvents(); DoEvents(); DoEvents();
        var todos = Todos();
        return (todos.FirstOrDefault(b => ReferenceEquals(b.DataContext, slot)), todos);
    }

    /// <summary>Misma regla que OnItemSlotDragOver para un intercambio slot-a-slot.</summary>
    private static bool Acepta(FrameworkElement origen, FrameworkElement destino)
    {
        var o = (ItemSlotViewModel)origen.DataContext; var d = (ItemSlotViewModel)destino.DataContext;
        return d.AcceptsItem(o.Item.Id) && o.AcceptsItem(d.Item.Id);
    }

    private static FrameworkElement DestinoQueAcepta(List<FrameworkElement> slots, FrameworkElement origen, bool preferirVacio)
    {
        var otros = slots.Where(b => !ReferenceEquals(b, origen)).ToList();
        return (preferirVacio ? otros.LastOrDefault(b => ((ItemSlotViewModel)b.DataContext).IsEmpty && Acepta(origen, b)) : null)
            ?? otros.LastOrDefault(b => Acepta(origen, b))
            ?? otros.Last();
    }

    private static Point Centro(FrameworkElement e) => e.PointToScreen(new Point(e.ActualWidth / 2, e.ActualHeight / 2));

    private static FrameworkElement? BuscarTarjetaLibreria(Window window, MainViewModel vm)
    {
        if (vm.Library.Results.Count == 0)
        {
            var cat = vm.Library.RootCategories.FirstOrDefault();
            cat?.SelectCommand?.Execute(cat);
            DoEvents(); DoEvents(); DoEvents();
        }
        if (vm.Library.Results.Count == 0) return null;
        var item = vm.Library.Results[0];
        return Descendientes<Border>(window).Where(b => ReferenceEquals(b.DataContext, item) && b.IsVisible && b.ActualWidth > 0 && CentroAlcanzable(window, b))
            .OrderByDescending(b => b.ActualWidth * b.ActualHeight).FirstOrDefault();
    }

    /// <summary>Border raiz real (SlotCompactTemplate) de cada slot visible del contenedor.</summary>
    private static List<FrameworkElement> SlotsVisibles(Window window, ContainerViewModel? contenedor)
    {
        if (contenedor == null) return new();
        var slots = new HashSet<object>(contenedor.Slots);
        return Descendientes<Border>(window)
            .Where(b => b.DataContext is ItemSlotViewModel s && slots.Contains(s) && b.IsVisible && b.ActualWidth > 0 && b.AllowDrop)
            .GroupBy(b => b.DataContext)
            .Select(g => (FrameworkElement)g.OrderByDescending(b => b.ActualWidth * b.ActualHeight).First())
            .Where(b => CentroAlcanzable(window, b))
            .ToList();
    }

    /// <summary>Hit-test real en el centro del elemento: True solo si lo que hay debajo ES el propio
    /// elemento o un descendiente suyo (descarta slots recortados por el viewport de un ScrollViewer
    /// o tapados por otra cosa).</summary>
    private static bool CentroAlcanzable(Window window, FrameworkElement b)
    {
        var raiz = (UIElement)window.Content;
        var p = b.TranslatePoint(new Point(b.ActualWidth / 2, b.ActualHeight / 2), raiz);
        if (p.X <= 0 || p.Y <= 0 || p.X >= window.ActualWidth || p.Y >= window.ActualHeight) return false;
        for (var x = raiz.InputHitTest(p) as DependencyObject; x != null; x = VisualTreeHelper.GetParent(x))
            if (ReferenceEquals(x, b)) return true;
        return false;
    }

    /// <summary>Punto de pantalla dentro de la ventana donde ningun elemento acepta el soltar
    /// (hit-test real: ni slot de objeto ni de buff debajo).</summary>
    private static Point? ZonaSinDestino(Window window)
    {
        var raiz = (UIElement)window.Content;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        // Interior de la ventana primero (la franja de cabecera y=~17 puede ser cromo transparente:
        // la captura de pantalla enseña lo que hay DETRAS aunque el punto sea de la ventana).
        var candidatos = new List<(double, double)>();
        for (double fy = 0.2; fy < 0.95; fy += 0.08) for (double fx = 0.05; fx < 0.96; fx += 0.1) candidatos.Add((fx, fy));
        candidatos.AddRange(new[] { (0.5, 0.02), (0.3, 0.02), (0.7, 0.03), (0.5, 0.97), (0.98, 0.5) });
        foreach (var (fx, fy) in candidatos)
        {
            var p = new Point(window.ActualWidth * fx, window.ActualHeight * fy);
            if (!EsDeLaVentana(hwnd, window.PointToScreen(p))) continue; // tapado por otra ventana (p. ej. siempre visible)
            var local = window.TranslatePoint(p, raiz);
            if (raiz.InputHitTest(local) is not DependencyObject d) continue;
            bool esSlot = false;
            for (var x = d; x != null; x = VisualTreeHelper.GetParent(x))
                if (x is FrameworkElement { DataContext: ItemSlotViewModel or BuffSlotViewModel } fe && fe is Border { AllowDrop: true }) { esSlot = true; break; }
            if (!esSlot) return window.PointToScreen(p);
        }
        return null;
    }

    private static Point? PuntoFueraDeVentana(Window window)
    {
        var tl = window.PointToScreen(new Point(0, 0));
        var br = window.PointToScreen(new Point(window.ActualWidth, window.ActualHeight));
        double pantallaAncho = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
        double y = (tl.Y + br.Y) / 2;
        if (br.X + 90 < pantallaAncho) return new Point(br.X + 70, y);
        if (tl.X - 90 > SystemParameters.VirtualScreenLeft) return new Point(tl.X - 70, y);
        return null;
    }

    private static ResultadoArrastreMano ArrastreRealMano(Window window, FrameworkElement origen, Point destino, Point? luegoFuera, bool cancelarConEsc, string etiqueta, string? captura, FrameworkElement? seguir = null)
    {
        var r = new ResultadoArrastreMano { Etiqueta = etiqueta };
        var capa = AdornerLayer.GetAdornerLayer((Visual)window.Content);

        void OnFeedbackMedidor(object s, GiveFeedbackEventArgs e)
        {
            r.Feedbacks++;
            if (seguir != null) { var c0 = Centro(seguir); r.SeguidoPrimero ??= c0; r.SeguidoUltimo = c0; }
            if (e.UseDefaultCursors) r.ConDefaultCursors++;
            bool acepta = (e.Effects & (DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link)) != 0;
            r.UltimoEfecto = e.Effects.ToString();
            var esperado = acepta ? DragCursors.HandleAgarrarActual : DragCursors.HandleNoPermitidoActual;
            if (acepta) r.ConMano++; else r.ConNo++;
            if (esperado == IntPtr.Zero || CursorManoNative.GetCursor() != esperado) r.CursorIncorrecto++;

            {
                GetCursorPos(out var pc);
                var raiz = (UIElement)window.Content;
                var local = raiz.PointFromScreen(new Point(pc.X, pc.Y));
                string bajo = "nada";
                for (var x = raiz.InputHitTest(local) as DependencyObject; x != null; x = VisualTreeHelper.GetParent(x))
                    if (x is FrameworkElement { DataContext: ItemSlotViewModel sv } fe && fe is Border { AllowDrop: true }) { bajo = $"slot {sv.SlotIndex} ({fe.GetType().Name} {fe.ActualWidth:0}x{fe.ActualHeight:0}, local={local.X:0},{local.Y:0})"; break; }
                if (bajo == "nada" && raiz.InputHitTest(local) is FrameworkElement h) bajo = $"{h.GetType().Name} (sin slot, local={local.X:0},{local.Y:0})";
                r.UltimoBajoCursor = bajo;
            }
            DragGhost.SpriteAdorner? ghost = null;
            if (capa != null)
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(capa) && ghost == null; i++)
                    ghost = VisualTreeHelper.GetChild(capa, i) as DragGhost.SpriteAdorner;
            if (ghost != null)
            {
                GetCursorPos(out var pt);
                var hotspot = ghost.AdornedElement.PointFromScreen(new Point(pt.X, pt.Y));
                var c = new Point((ghost.RectActual.Left + ghost.RectActual.Right) / 2, (ghost.RectActual.Top + ghost.RectActual.Bottom) / 2);
                double d = Math.Max(Math.Abs(c.X - hotspot.X), Math.Abs(c.Y - hotspot.Y));
                r.DesvioMedidas++;
                if (d > 1) r.DesvioFuera++;
                r.DesvioMax = Math.Max(r.DesvioMax, d);
            }
        }
        var handler = new GiveFeedbackEventHandler(OnFeedbackMedidor);
        window.AddHandler(DragDrop.GiveFeedbackEvent, handler, true);

        var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        ForzarPrimerPlano(hwnd);
        var inicio = Centro(origen);
        if (!SeguroParaInyectar(hwnd) || !EsDeLaVentana(hwnd, inicio) || !EsDeLaVentana(hwnd, destino))
        {
            window.RemoveHandler(DragDrop.GiveFeedbackEvent, handler);
            r.Incidencias.Add("ABORTADO sin pulsar nada: ventana de prueba no en primer plano, con un dialogo modal abierto, o el origen/destino tapado por otra ventana");
            Console.WriteLine($"FALLO: DRAG_CURSOR_MANO[{etiqueta}] - " + r.Incidencias[^1]);
            return r;
        }
        string dir = CarpetaEvidenciaKeepQa();

        void Recorrer(Point a, Point b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            int pasos = Math.Max(1, (int)(Math.Sqrt(dx * dx + dy * dy) / 3.0));
            for (int i = 1; i <= pasos; i++)
            {
                SetCursorPos((int)Math.Round(a.X + dx * i / pasos), (int)Math.Round(a.Y + dy * i / pasos));
                System.Threading.Thread.Sleep(5);
            }
        }

        bool abortado = false;
        // Suelta el boton SIEMPRE dentro de la ventana de prueba (en el origen) si algo va mal.
        void Abortar(string motivo)
        {
            abortado = true;
            r.Incidencias.Add("ABORTADO: " + motivo);
            SetCursorPos((int)inicio.X, (int)inicio.Y);
            System.Threading.Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }
        var hilo = new System.Threading.Thread(() =>
        {
            SetCursorPos((int)inicio.X, (int)inicio.Y);
            System.Threading.Thread.Sleep(120);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            System.Threading.Thread.Sleep(60);
            // primeros pasos finos para cruzar el umbral de arrastre dentro del propio elemento
            for (int i = 1; i <= 6; i++) { SetCursorPos((int)inicio.X + i * 2, (int)inicio.Y + i); System.Threading.Thread.Sleep(15); }
            Recorrer(new Point(inicio.X + 12, inicio.Y + 6), destino);
            System.Threading.Thread.Sleep(350);
            if (!SeguroParaInyectar(hwnd)) { Abortar("dialogo modal o foco perdido a mitad del arrastre"); return; }
            r.CursorGlobalSostenido = CursorGlobal();
            if (captura != null) r.Captura = CapturaPantallaConCursor(destino, Path.Combine(dir, captura));
            if (luegoFuera is { } f)
            {
                Recorrer(destino, f);
                System.Threading.Thread.Sleep(350);
                r.CursorGlobalFuera = CursorGlobal();
                // Soltar fuera SOLO sobre el escritorio (nunca sobre la ventana de otro programa):
                // si ahi hay otra cosa, se vuelve al destino dentro de la ventana y se suelta alli.
                // Fuera de la ventana hay otro programa: NO se suelta encima. Se cancela con Esc
                // (solo teclado, a la ventana de prueba que sigue en primer plano), se mide ahi
                // mismo la restauracion del cursor, y se vuelve dentro para soltar el boton.
                if (!EsEscritorio(f))
                {
                    if (!SeguroParaInyectar(hwnd)) { Abortar("dialogo modal o foco perdido fuera de la ventana"); return; }
                    r.Incidencias.Add("fuera de la ventana hay otro programa: cancelado con Esc ahi (sin soltar encima) y soltado de vuelta dentro");
                    keybd_event(0x1B, 0, 0, UIntPtr.Zero);
                    System.Threading.Thread.Sleep(30);
                    keybd_event(0x1B, 0, 0x0002, UIntPtr.Zero);
                    System.Threading.Thread.Sleep(400); // SIN mover el raton
                    r.CursorGlobalFinal = CursorGlobal();
                    Recorrer(f, destino);
                    System.Threading.Thread.Sleep(100);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    return;
                }
            }
            if (!SeguroParaInyectar(hwnd)) { Abortar("dialogo modal o foco perdido antes de soltar"); return; }
            GetCursorPos(out var ahora);
            if (!EsDeLaVentana(hwnd, new Point(ahora.X, ahora.Y))) { Abortar("el punto de soltar no pertenece a la ventana de prueba (otra ventana la tapa)"); return; }
            if (cancelarConEsc)
            {
                keybd_event(0x1B, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(30);
                keybd_event(0x1B, 0, 0x0002, UIntPtr.Zero);
                System.Threading.Thread.Sleep(200);
            }
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            System.Threading.Thread.Sleep(400); // SIN mover el raton: la restauracion no puede depender de un WM_SETCURSOR nuevo
            r.CursorGlobalFinal = CursorGlobal();
        })
        { IsBackground = true };
        hilo.SetApartmentState(System.Threading.ApartmentState.STA);
        hilo.Start();
        var limite = DateTime.Now.AddSeconds(15);
        while (hilo.IsAlive && DateTime.Now < limite) { DoEvents(); System.Threading.Thread.Sleep(10); }
        hilo.Join(1000);
        if (abortado) Console.WriteLine($"FALLO: DRAG_CURSOR_MANO[{etiqueta}] - prueba ABORTADA por seguridad: " + string.Join(" ; ", r.Incidencias));
        window.RemoveHandler(DragDrop.GiveFeedbackEvent, handler);
        // Nunca dejar el boton "pulsado" - y soltarlo SIEMPRE dentro de la ventana de prueba.
        if (!abortado) { SetCursorPos((int)destino.X, (int)destino.Y); mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero); }
        DoEvents(); DoEvents();
        return r;
    }

    /// <summary>La ventana de prueba de ESTE proceso esta en primer plano y habilitada (con un dialogo
    /// modal abierto, Windows deshabilita a su dueña): solo entonces se inyecta raton/teclado.</summary>
    private static bool SeguroParaInyectar(IntPtr hwnd) =>
        GetForegroundWindow() == hwnd && CursorManoNative.IsWindowEnabled(hwnd);

    /// <summary>Lo que Windows tiene de verdad en ese punto de pantalla es la ventana de prueba
    /// (WindowFromPoint), no otra que la tape.</summary>
    private static bool EsDeLaVentana(IntPtr hwnd, Point p) =>
        CursorManoNative.GetAncestor(CursorManoNative.WindowFromPoint(new CursorManoNative.POINT { X = (int)p.X, Y = (int)p.Y }), 2) == hwnd;

    private static bool EsEscritorio(Point p)
    {
        var h = CursorManoNative.GetAncestor(CursorManoNative.WindowFromPoint(new CursorManoNative.POINT { X = (int)p.X, Y = (int)p.Y }), 2);
        var sb = new System.Text.StringBuilder(64);
        CursorManoNative.GetClassName(h, sb, sb.Capacity);
        string c = sb.ToString();
        return c == "Progman" || c == "WorkerW";
    }

    private static IntPtr CursorGlobal()
    {
        var ci = new CursorManoNative.CURSORINFO { cbSize = Marshal.SizeOf<CursorManoNative.CURSORINFO>() };
        return CursorManoNative.GetCursorInfo(ref ci) ? ci.hCursor : IntPtr.Zero;
    }

    /// <summary>Captura real del escritorio alrededor de `centro` (px fisicos) + el cursor que Windows
    /// muestra en ese momento dibujado en su posicion (BitBlt no incluye el cursor).</summary>
    private static string? CapturaPantallaConCursor(Point centro, string ruta)
    {
        const int L = 220;
        int x0 = (int)centro.X - L / 2, y0 = (int)centro.Y - L / 2;
        IntPtr pantalla = CursorManoNative.GetDC(IntPtr.Zero);
        IntPtr mem = CursorManoNative.CreateCompatibleDC(pantalla);
        IntPtr bmp = CursorManoNative.CreateCompatibleBitmap(pantalla, L, L);
        IntPtr viejo = CursorManoNative.SelectObject(mem, bmp);
        try
        {
            CursorManoNative.BitBlt(mem, 0, 0, L, L, pantalla, x0, y0, 0x00CC0020 | 0x40000000);
            var ci = new CursorManoNative.CURSORINFO { cbSize = Marshal.SizeOf<CursorManoNative.CURSORINFO>() };
            if (CursorManoNative.GetCursorInfo(ref ci) && ci.hCursor != IntPtr.Zero && CursorManoNative.GetIconInfo(ci.hCursor, out var ii))
            {
                CursorManoNative.DrawIconEx(mem, ci.ptScreenPos.X - ii.xHotspot - x0, ci.ptScreenPos.Y - ii.yHotspot - y0, ci.hCursor, 0, 0, 0, IntPtr.Zero, 0x0003);
                if (ii.hbmMask != IntPtr.Zero) CursorManoNative.DeleteObject(ii.hbmMask);
                if (ii.hbmColor != IntPtr.Zero) CursorManoNative.DeleteObject(ii.hbmColor);
            }
            CursorManoNative.SelectObject(mem, viejo);
            var src = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmp, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            var grande = new TransformedBitmap(src, new ScaleTransform(2, 2));
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(grande));
            using var fs = File.Create(ruta);
            enc.Save(fs);
            return ruta;
        }
        catch (Exception ex) { return "error: " + ex.Message; }
        finally
        {
            CursorManoNative.DeleteObject(bmp);
            CursorManoNative.DeleteDC(mem);
            CursorManoNative.ReleaseDC(IntPtr.Zero, pantalla);
        }
    }

    private static BitmapSource? BitmapDeCursor(IntPtr h) =>
        h == IntPtr.Zero ? null : System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(h, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

    /// <summary>PNG del cursor REAL cargado por produccion (desde su HCURSOR): tamaño real y x8.</summary>
    private static void GuardarCursoresEvidencia(Window window, string dir)
    {
        DragCursors.Agarrar(window); DragCursors.NoPermitido(window);
        Console.WriteLine($"DRAG_CURSOR_MANO: cursores cargados de los recursos incrustados -> mano=0x{DragCursors.HandleAgarrarActual:X} no=0x{DragCursors.HandleNoPermitidoActual:X} ; imagen elegida {DragCursors.LadoActual}x{DragCursors.LadoActual} px, hotspot=({DragCursors.HotspotActual.X},{DragCursors.HotspotActual.Y})");
        foreach (var (h, nombre) in new[] { (DragCursors.HandleAgarrarActual, "cursor-mano-agarrar"), (DragCursors.HandleNoPermitidoActual, "cursor-mano-agarrar-no") })
        {
            var bmp = BitmapDeCursor(h);
            if (bmp == null) { Console.WriteLine($"FALLO: DRAG_CURSOR_MANO - no se pudo cargar {nombre} (HCURSOR nulo)"); continue; }
            GuardarPngEvidencia(bmp, Path.Combine(dir, nombre + "-real.png"));
            int n = bmp.PixelWidth, z = 8;
            var dv = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(dv, BitmapScalingMode.NearestNeighbor);
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x1B, 0x1D, 0x23)), null, new Rect(0, 0, n * z, n * z));
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)), null, new Rect(n * z, 0, n * z, n * z));
                dc.DrawImage(bmp, new Rect(0, 0, n * z, n * z));
                dc.DrawImage(bmp, new Rect(n * z, 0, n * z, n * z));
                var rojo = new Pen(Brushes.Red, 2);
                foreach (double ox in new double[] { 0, n * z })
                {
                    double hx = ox + (DragCursors.HotspotActual.X + 0.5) * z, hy = (DragCursors.HotspotActual.Y + 0.5) * z;
                    dc.DrawLine(rojo, new Point(hx - 10, hy), new Point(hx + 10, hy));
                    dc.DrawLine(rojo, new Point(hx, hy - 10), new Point(hx, hy + 10));
                }
            }
            var rtb = new RenderTargetBitmap(n * z * 2, n * z, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            GuardarPngEvidencia(rtb, Path.Combine(dir, nombre + "-x8.png"));
        }
    }

    private static void GuardarPngEvidencia(BitmapSource bmp, string ruta)
    {
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        using var fs = File.Create(ruta);
        enc.Save(fs);
    }

    /// <summary>Composicion: el render real del adorno (sprite) con el cursor REAL de mano encima, su
    /// hotspot sobre el punto donde produccion coloca el hotspot - a tamaño real y a x3.</summary>
    private static void GuardarComposicionMano(BitmapSource renderAdorno, Point hotspot, string etiqueta, FrameworkElement contexto, string dir)
    {
        DragCursors.Agarrar(contexto);
        var cursor = BitmapDeCursor(DragCursors.HandleAgarrarActual);
        if (cursor == null) { Console.WriteLine($"FALLO: DRAG_GHOST_ASPECTO[{etiqueta}] - no se pudo cargar el cursor de mano para la composicion"); return; }
        double escalaDpi = VisualTreeHelper.GetDpi(contexto).DpiScaleX;
        double ladoCursor = cursor.PixelWidth / escalaDpi;
        var hs = new Point(DragCursors.HotspotActual.X / escalaDpi, DragCursors.HotspotActual.Y / escalaDpi);
        int W = renderAdorno.PixelWidth, H = renderAdorno.PixelHeight;
        foreach (int z in new[] { 1, 3 })
        {
            var dv = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(dv, BitmapScalingMode.NearestNeighbor);
            using (var dc = dv.RenderOpen())
            {
                dc.PushTransform(new ScaleTransform(z, z));
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x1B, 0x1D, 0x23)), null, new Rect(0, 0, W, H));
                dc.DrawImage(renderAdorno, new Rect(0, 0, W, H));
                dc.DrawImage(cursor, new Rect(hotspot.X - hs.X, hotspot.Y - hs.Y, ladoCursor, ladoCursor));
                dc.Pop();
            }
            var rtb = new RenderTargetBitmap(W * z, H * z, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            GuardarPngEvidencia(rtb, Path.Combine(dir, $"drag-ghost-{etiqueta}-mano{(z == 1 ? "" : "-x3")}.png"));
        }
        Console.WriteLine($"DRAG_GHOST_ASPECTO[{etiqueta}]: composicion sprite+mano en {dir}\\drag-ghost-{etiqueta}-mano*.png");
    }

    private static class CursorManoNative
    {
        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X; public int Y; }
        [StructLayout(LayoutKind.Sequential)] public struct CURSORINFO { public int cbSize; public int flags; public IntPtr hCursor; public POINT ptScreenPos; }
        [StructLayout(LayoutKind.Sequential)] public struct ICONINFO { public bool fIcon; public int xHotspot; public int yHotspot; public IntPtr hbmMask; public IntPtr hbmColor; }
        [DllImport("user32.dll")] public static extern IntPtr GetCursor();
        [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
        [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr hwnd, System.Text.StringBuilder sb, int max);
        [DllImport("user32.dll")] public static extern bool GetCursorInfo(ref CURSORINFO pci);
        [DllImport("user32.dll")] public static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);
        [DllImport("user32.dll")] public static extern bool DrawIconEx(IntPtr hdc, int x, int y, IntPtr hIcon, int cx, int cy, int istep, IntPtr hbr, int flags);
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
        [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("gdi32.dll")] public static extern bool BitBlt(IntPtr hdc, int x, int y, int w, int h, IntPtr src, int sx, int sy, int rop);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr hdc);
    }
}
