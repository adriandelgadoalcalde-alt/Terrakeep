// CANARIO REAL - bucle de layout en reposo (29-sep-2026, regresion reportada por el usuario: su
// Terrakeep instalado se quedo "congelado" - 1,25 GB, ~57 % de un nucleo sin parar, entrada sin
// atender, hilo de UI SIEMPRE dentro de ContextLayoutManager.UpdateLayout en 3 muestras).
//
// CAUSA REAL (demostrada con el volcado de memoria del proceso congelado, dotnet-dump, ver
// bitacora.md): MainWindow.OnExplorationSidebarScrollChanged recibia TAMBIEN los ScrollChanged
// BURBUJEADOS del ScrollViewer interno de las listas virtualizadas de Browse y decidia la
// visibilidad del indicador "desliza para ver mas" (fila Auto BAJO el ScrollViewer de la barra
// lateral) con los valores de la lista interna. Barra lateral en un offset intermedio (37,21 de
// 85,2) + lista interna casi al fondo = el indicador aparece/desaparece en cada pasada (el
// ScrollViewer de la barra alterna 910,82 <-> 941,45 px de alto, secuencia real de 44
// ScrollChangedEventArgs sacada del volcado) - sin fin. DoEvents (prioridad Background) no vuelve
// nunca: el mismo "no se puede usar" que vio el usuario.
//
// LAYOUT_REPOSO_SOLO=1 mide, en REPOSO (sin entrada, sin tocar el ViewModel), las pasadas de layout
// reales (Window.LayoutUpdated) durante 1 s en: el estado EXACTO del volcado, todas las pestañas de
// primer nivel, las sub-pestañas de Personaje y las categorias reales de Exploracion con un mundo
// grande cargado (copia aislada), en varios tamaños incluidas las fronteras de SizeClass
// (1519/1520 y 1919/1920) y el tamaño real de la ventana congelada (1651x1204). Umbral:
// UMBRAL_PASADAS_REPOSO/s (una UI sana hace 0). Un VIGILANTE en otro hilo convierte un cuelgue del
// hilo de UI (> SEGUNDOS_BLOQUEO sin volver de DoEvents) en "FALLO: LAYOUT_REPOSO_SOLO-BLOQUEO" con
// la secuencia de ScrollChanged registrada, y sale con codigo 1 en vez de quedarse colgado.
//
// Con un cliente UIA EXTERNO (proceso hijo) activo, como en la maquina del usuario (el volcado tenia
// el hilo de UI en ContextLayoutManager.fireAutomationEvents): cada pasada de layout recorre ademas
// todo el arbol de peers, lo que convierte cualquier bucle en un consumo de CPU mucho mayor.
//
// LAYOUT_REPOSO_RAPIDO=1: solo el estado del volcado + su matriz de offsets, para iterar.
// LAYOUT_REPOSO_SIN_UIA=1: sin el cliente UIA externo.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Terrakeep.App;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private const int UMBRAL_PASADAS_REPOSO = 2;
    private const int SEGUNDOS_BLOQUEO = 20;

    private static long _latidoReposoMs;
    private static volatile string _pasoReposo = "";
    private static readonly ConcurrentQueue<string> _registroScrollReposo = new();

    // Cliente UIA EXTERNO (proceso hijo = este mismo exe con LAYOUT_REPOSO_CLIENTE_UIA_HWND). WPF solo
    // recorre el arbol de peers tras CADA pasada de layout (GetNameCore/IsOffscreenCore/GetChildrenCore
    // de todo el subarbol) cuando algun cliente tiene registrados PropertyChanged/StructureChanged - un
    // UIA "en proceso" (AutomationElement desde el propio arnes) NO lo reproduce.
    private static int EjecutarClienteUiaExterno(IntPtr hwnd)
    {
        try
        {
            var raiz = System.Windows.Automation.AutomationElement.FromHandle(hwnd);
            int eventos = 0;
            System.Windows.Automation.Automation.AddAutomationPropertyChangedEventHandler(raiz, System.Windows.Automation.TreeScope.Subtree,
                (_, _) => System.Threading.Interlocked.Increment(ref eventos),
                System.Windows.Automation.AutomationElement.NameProperty,
                System.Windows.Automation.AutomationElement.IsOffscreenProperty,
                System.Windows.Automation.AutomationElement.BoundingRectangleProperty);
            System.Windows.Automation.Automation.AddStructureChangedEventHandler(raiz, System.Windows.Automation.TreeScope.Subtree,
                (_, _) => System.Threading.Interlocked.Increment(ref eventos));
            System.Windows.Automation.Automation.AddAutomationFocusChangedEventHandler((_, _) => System.Threading.Interlocked.Increment(ref eventos));
            string? listo = Environment.GetEnvironmentVariable("LAYOUT_REPOSO_CLIENTE_UIA_LISTO");
            if (listo != null) File.WriteAllText(listo, "listo");
            int pidPadre = int.Parse(Environment.GetEnvironmentVariable("LAYOUT_REPOSO_CLIENTE_UIA_PADRE") ?? "0");
            var fin = DateTime.UtcNow.AddMinutes(30); // tope duro por si acaso
            while (DateTime.UtcNow < fin)
            {
                System.Threading.Thread.Sleep(500);
                try { if (pidPadre == 0 || System.Diagnostics.Process.GetProcessById(pidPadre).HasExited) break; }
                catch (ArgumentException) { break; }
            }
            System.Windows.Automation.Automation.RemoveAllEventHandlers();
            return 0;
        }
        catch (Exception ex) { Console.WriteLine("CLIENTE-UIA-EXCEPTION: " + ex.Message); return 1; }
    }

    private static System.Diagnostics.Process? LanzarClienteUiaExterno(Window window)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        string listo = Path.Combine(Path.GetTempPath(), $"terrakeep-cliente-uia-{Environment.ProcessId}.listo");
        if (File.Exists(listo)) File.Delete(listo);
        var psi = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = false,
            WorkingDirectory = AppContext.BaseDirectory,
        };
        psi.Environment["LAYOUT_REPOSO_CLIENTE_UIA_HWND"] = hwnd.ToInt64().ToString();
        psi.Environment["LAYOUT_REPOSO_CLIENTE_UIA_LISTO"] = listo;
        psi.Environment["LAYOUT_REPOSO_CLIENTE_UIA_PADRE"] = Environment.ProcessId.ToString();
        var p = System.Diagnostics.Process.Start(psi);
        // La suscripcion es cross-process: el hilo de UI del arnes tiene que atenderla - bombear.
        long hasta = Environment.TickCount64 + 20000;
        while (!File.Exists(listo) && Environment.TickCount64 < hasta) EsperarConLatido(50);
        bool ok = File.Exists(listo);
        Console.WriteLine($"LAYOUT-REPOSO: cliente UIA externo pid={p?.Id} suscrito={ok}");
        try { File.Delete(listo); } catch (IOException) { }
        return p;
    }

    /// <summary>Diagnostico: DispatcherTimer activos del hilo de UI (campo privado Dispatcher._timers) y
    /// el metodo real de su Tick - para saber QUE produce pasadas de layout periodicas.</summary>
    private static string DescribirTemporizadoresActivos()
    {
        try
        {
            var d = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            var campo = typeof(System.Windows.Threading.Dispatcher).GetField("_timers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (campo?.GetValue(d) is not System.Collections.IList lista) return "(sin acceso)";
            var partes = new List<string>();
            foreach (var t in lista.OfType<System.Windows.Threading.DispatcherTimer>().ToList())
            {
                var tick = typeof(System.Windows.Threading.DispatcherTimer).GetField("Tick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(t) as Delegate;
                string metodos = tick == null ? "?" : string.Join("+", tick.GetInvocationList().Select(m => $"{m.Method.DeclaringType?.FullName}.{m.Method.Name}"));
                partes.Add($"{t.Interval.TotalMilliseconds:0}ms->{metodos}");
            }
            return partes.Count == 0 ? "(ninguno)" : string.Join(" | ", partes);
        }
        catch (Exception ex) { return "(error: " + ex.Message + ")"; }
    }

    /// <summary>Igual que WaitForDispatcher, pero dejando latido para el vigilante de bloqueo.</summary>
    private static void EsperarConLatido(int ms)
    {
        long hasta = Environment.TickCount64 + ms;
        do
        {
            DoEvents();
            System.Threading.Interlocked.Exchange(ref _latidoReposoMs, Environment.TickCount64);
            System.Threading.Thread.Sleep(1);
        } while (Environment.TickCount64 < hasta);
    }

    private static void IniciarVigilanteBloqueo()
    {
        System.Threading.Interlocked.Exchange(ref _latidoReposoMs, Environment.TickCount64);
        var hilo = new System.Threading.Thread(() =>
        {
            while (true)
            {
                System.Threading.Thread.Sleep(1000);
                long sinLatido = Environment.TickCount64 - System.Threading.Interlocked.Read(ref _latidoReposoMs);
                if (sinLatido < SEGUNDOS_BLOQUEO * 1000) continue;
                Console.WriteLine($"FALLO: LAYOUT_REPOSO_SOLO-BLOQUEO - el hilo de UI lleva {sinLatido / 1000} s sin volver de DoEvents " +
                                  $"(prioridad Background nunca atendida = la app no responde a la entrada) en el paso '{_pasoReposo}'. " +
                                  "Ultimos ScrollChanged vistos por la barra lateral de Exploracion (origen: off/ext/vp):");
                foreach (var linea in _registroScrollReposo.ToArray()) Console.WriteLine("  " + linea);
                Console.WriteLine("LAYOUT_REPOSO_SOLO: BLOQUEO - 1 fallo(s)");
                Console.Out.Flush();
                Environment.Exit(1);
            }
        }) { IsBackground = true, Name = "VigilanteBloqueoLayoutReposo" };
        hilo.Start();
    }

    private static int EjecutarLayoutReposoSolo(MainWindow window, MainViewModel vm)
    {
        int fallos = 0, medidas = 0;
        System.Diagnostics.Process? clienteUia = null;
        void Fallo(string codigo, string msg) { fallos++; Console.WriteLine($"FALLO: LAYOUT_REPOSO_SOLO-{codigo} - {msg}"); }

        IniciarVigilanteBloqueo();

        int pasadas = 0;
        EventHandler contador = (_, _) => pasadas++;
        window.LayoutUpdated += contador;

        var sidebarScroll = window.FindName("ExplorationSidebarScroll") as ScrollViewer;
        int scrollChanged = 0;
        // Registro circular de TODO lo que llega por ScrollChanged a la barra lateral (propio y
        // burbujeado desde ScrollViewer internos) - lo que el vigilante vuelca si hay bloqueo.
        ScrollChangedEventHandler contadorScroll = (emisor, e) =>
        {
            scrollChanged++;
            string origen = ReferenceEquals(e.OriginalSource, sidebarScroll) ? "BARRA" : $"INTERNO({(e.OriginalSource as FrameworkElement)?.TemplatedParent?.GetType().Name ?? e.OriginalSource?.GetType().Name})";
            _registroScrollReposo.Enqueue($"{origen} off={e.VerticalOffset:0.##} ext={e.ExtentHeight:0.##} vp={e.ViewportHeight:0.##}");
            while (_registroScrollReposo.Count > 24) _registroScrollReposo.TryDequeue(out _);
        };
        if (sidebarScroll != null) sidebarScroll.ScrollChanged += contadorScroll;

        void Paso(string paso) { _pasoReposo = paso; }

        void MedirReposo(string cab)
        {
            Paso(cab);
            // Reposo = sin entrada. El raton REAL del usuario puede quedar por casualidad encima de una
            // tarjeta de personaje de Inicio de la ventana del arnes: eso arranca la animacion de andar
            // (DispatcherTimer de 70 ms, CharacterListEntryViewModel.SetHovering) = ~14 pasadas/s
            // legitimas mientras dura el hover, no un bucle. Se apaga aqui para medir reposo de verdad.
            foreach (var c in vm.Home.Characters) c.SetHovering(false);
            EsperarConLatido(450); // asentar la maquetacion tras el cambio de estado
            pasadas = 0; scrollChanged = 0;
            var cpu0 = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
            EsperarConLatido(1000);
            double cpuMs = (System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime - cpu0).TotalMilliseconds;
            medidas++;
            string extra = sidebarScroll != null && sidebarScroll.IsVisible
                ? $" barraLateral: ScrollChanged={scrollChanged} vp={sidebarScroll.ViewportHeight:0.##} ext={sidebarScroll.ExtentHeight:0.##} off={sidebarScroll.VerticalOffset:0.##}"
                : "";
            Console.WriteLine($"LAYOUT-REPOSO {cab}: pasadas/s={pasadas} cpu={cpuMs:0}ms/s{extra}");
            if (pasadas <= UMBRAL_PASADAS_REPOSO) return;
            string temporizadores = DescribirTemporizadoresActivos();
            Console.WriteLine($"LAYOUT-REPOSO   temporizadores activos: {temporizadores}");
            // Unica fuente periodica = animacion de hover de una tarjeta de personaje (70 ms): el raton
            // FISICO del usuario esta encima de la ventana del arnes (no se puede mover sin SendInput,
            // prohibido con el usuario delante). Es entrada real, no reposo: se apaga y se repite la
            // medida; si el raton vuelve a disparar el hover, la medida queda INCONCLUSIVE, no FALLO.
            if (temporizadores.Contains("EnsureHoverWalkTimer"))
            {
                for (int intento = 1; intento <= 3; intento++)
                {
                    foreach (var c in vm.Home.Characters) c.SetHovering(false);
                    EsperarConLatido(300);
                    pasadas = 0;
                    EsperarConLatido(1000);
                    string t2 = DescribirTemporizadoresActivos();
                    Console.WriteLine($"LAYOUT-REPOSO {cab} (reintento {intento} sin hover): pasadas/s={pasadas}");
                    if (pasadas <= UMBRAL_PASADAS_REPOSO) return;
                    if (!t2.Contains("EnsureHoverWalkTimer")) { temporizadores = t2; break; }
                    if (intento == 3)
                    {
                        Console.WriteLine($"LAYOUT-REPOSO {cab}: INCONCLUSIVE - el raton real del usuario vuelve a poner en hover una tarjeta de personaje (animacion de andar, 70 ms) en cada reintento; no es reposo");
                        return;
                    }
                }
            }
            if (pasadas > UMBRAL_PASADAS_REPOSO)
                Fallo("BUCLE", $"{cab}: {pasadas} pasadas de layout en 1 s de reposo (umbral {UMBRAL_PASADAS_REPOSO}) - la maquetacion no se estabiliza");
        }

        (ListBox lista, ScrollViewer sv, IScrollInfo? rueda)? ListaInternaVisible()
        {
            if (window.FindName("BrowseView") is not FrameworkElement bv) return null;
            var lista = Descendientes<ListBox>(bv).FirstOrDefault(l => l.IsVisible && l.Items.Count > 0);
            var sv = lista == null ? null : Descendientes<ScrollViewer>(lista).FirstOrDefault();
            if (lista == null || sv == null) return null;
            return (lista, sv, Descendientes<VirtualizingStackPanel>(lista).FirstOrDefault() as IScrollInfo);
        }

        // Rueda del raton (el comando real que ejecuta el ScrollViewer al recibirla, sin entrada
        // real) hasta el final de la lista interna - como la tenia el usuario.
        void RuedaHastaElFinal((ListBox lista, ScrollViewer sv, IScrollInfo? rueda) li)
        {
            li.sv.ScrollToVerticalOffset(0); EsperarConLatido(30);
            for (int paso = 0; paso < 120 && li.sv.VerticalOffset < li.sv.ScrollableHeight - 0.5; paso++)
            {
                li.rueda?.MouseWheelDown();
                EsperarConLatido(5);
            }
            // Listas muy largas (cientos de filas): el resto del camino de golpe y los ultimos
            // pasos otra vez con la rueda, que es lo que reajusta el offset en modo pixel.
            if (li.sv.VerticalOffset < li.sv.ScrollableHeight - 0.5)
            {
                li.sv.ScrollToEnd(); EsperarConLatido(30);
                for (int paso = 0; paso < 6; paso++) { li.rueda?.MouseWheelDown(); EsperarConLatido(5); }
            }
        }

        try
        {
            // Personaje: la COPIA aislada del primero que haya (Inicio escanea las copias).
            Paso("carga de personaje");
            int espera = 0;
            while (vm.Home.IsScanning && espera < 200) { EsperarConLatido(50); espera++; }
            var copia = vm.Home.Characters.FirstOrDefault();
            if (copia != null)
            {
                vm.LoadFromPath(copia.FilePath);
                EsperarConLatido(20);
                ComprobarPersonajeAislado(vm, "LAYOUT_REPOSO_SOLO");
            }
            else Console.WriteLine("LAYOUT-REPOSO: AVISO - sin copia de personaje, Personaje se mide vacio");

            // Mundo grande (copia aislada, mismo tamaño 8400x2400 que el del volcado).
            Paso("carga de mundo");
            string worldsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria", "tModLoader", "Worlds");
            string? mundoReal = new[] { "El_Gruta_(Legendario).wld", "roca_negra.wld" }.Select(n => Path.Combine(worldsDir, n)).FirstOrDefault(File.Exists);
            if (mundoReal != null)
            {
                vm.SelectedTabIndex = 4; EsperarConLatido(20);
                var carga = vm.Exploration.LoadFromPathAsync(MundoAislado(mundoReal));
                while (!carga.IsCompleted) EsperarConLatido(20);
                EsperarConLatido(50);
                Console.WriteLine($"LAYOUT-REPOSO: mundo cargado (copia de {Path.GetFileName(mundoReal)}) IsWorldLoaded={vm.Exploration.IsWorldLoaded}");
            }
            else Console.WriteLine("LAYOUT-REPOSO: AVISO - sin mundo real que copiar, Exploracion se mide vacia");

            double anchoSidebarOriginal = vm.Settings.ExplorationSidebarWidth;
            string idiomaOriginal = vm.Settings.Language;
            vm.Settings.Language = "es"; EsperarConLatido(20);

            var categorias = new (WorldSearchCategory cat, int modo, string nombre)[]
            {
                (WorldSearchCategory.Chests, 0, "Cofres-PorTipo"), (WorldSearchCategory.Chests, 1, "Cofres-PorContenido"),
                (WorldSearchCategory.Chests, 2, "Cofres-CofreACofre"), (WorldSearchCategory.Ores, 0, "Minerales"),
                (WorldSearchCategory.Objects, 0, "Objetos"), (WorldSearchCategory.Npcs, 0, "NPCs"), (WorldSearchCategory.All, 0, "Todo"),
            };

            void EntrarCategoria(WorldSearchCategory cat, int modo)
            {
                vm.SelectedTabIndex = 4;
                vm.Exploration.ShowSidebarBrowseCommand.Execute(null);
                vm.Exploration.SelectedCategory = cat;
                if (cat == WorldSearchCategory.Chests) vm.Exploration.ChestViewMode = modo;
                sidebarScroll?.ScrollToVerticalOffset(0);
                EsperarConLatido(60);
            }

            // Matriz "barra lateral en offset X" x "lista interna al fondo" - la combinacion real del
            // volcado. Los offsets se piden DESPUES de dejar la lista al fondo (el orden importa: al
            // mover la barra cambia el alto de la lista).
            void MedirMatrizBarraYLista(string tam, string nombre)
            {
                if (sidebarScroll == null) return;
                var li = ListaInternaVisible();
                if (li == null || li.Value.sv.ScrollableHeight < 0.5) return;
                RuedaHastaElFinal(li.Value);
                double desplazable = sidebarScroll.ScrollableHeight;
                foreach (double off in new[] { 0.0, 37.21, desplazable * 0.25, desplazable * 0.5, desplazable * 0.75, desplazable - 1, desplazable })
                {
                    if (desplazable < 1 && off > 0) break;
                    sidebarScroll.ScrollToVerticalOffset(off);
                    MedirReposo($"Exploracion/{nombre} {tam} barra={vm.Settings.ExplorationSidebarWidth:0} (barra lateral en {off:0.##} + lista interna al fondo, filas={li.Value.lista.Items.Count} listaOff={li.Value.sv.VerticalOffset:0.##}/{li.Value.sv.ScrollableHeight:0.##})");
                }
                sidebarScroll.ScrollToVerticalOffset(0);
                li.Value.sv.ScrollToVerticalOffset(0);
            }

            void MedirExploracion(string tam, bool completo)
            {
                foreach (var (cat, modo, nombre) in categorias)
                {
                    EntrarCategoria(cat, modo);
                    MedirReposo($"Exploracion/{nombre} {tam} barra={vm.Settings.ExplorationSidebarWidth:0}");
                    if (completo && sidebarScroll != null)
                    {
                        sidebarScroll.ScrollToEnd();
                        MedirReposo($"Exploracion/{nombre} {tam} barra={vm.Settings.ExplorationSidebarWidth:0} (barra lateral al fondo)");
                        sidebarScroll.ScrollToVerticalOffset(0);
                    }
                    if (completo || cat == WorldSearchCategory.Chests && modo == 0 || cat == WorldSearchCategory.Objects)
                        MedirMatrizBarraYLista(tam, nombre);
                }
                vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                vm.Exploration.ChestViewMode = 0;
                vm.Exploration.ShowSidebarWorldToolsCommand.Execute(null);
                MedirReposo($"Exploracion/Mundo {tam}");
                vm.Exploration.ShowSidebarBrowseCommand.Execute(null);
                sidebarScroll?.ScrollToVerticalOffset(0);
            }

            // ---------------------------------------------------------------------------------
            // 0) ESTADO EXACTO DEL VOLCADO (determinista): 1651x1204, barra lateral 520, Cofres/Por
            //    tipo, lista interna con la rueda hasta el final y barra lateral en 37,21.
            // ---------------------------------------------------------------------------------
            FijarTamaño(window, 1651, 1204);
            vm.SelectedTabIndex = 0;
            MedirReposo("Inicio 1651x1204 (antes de Exploracion)");
            vm.Settings.ExplorationSidebarWidth = 520;
            EntrarCategoria(WorldSearchCategory.Chests, 0);
            {
                var li = ListaInternaVisible();
                if (li != null && sidebarScroll != null)
                {
                    Paso("estado del volcado: rueda hasta el final de la lista interna");
                    RuedaHastaElFinal(li.Value);
                    Paso("estado del volcado: barra lateral a 37,21");
                    sidebarScroll.ScrollToVerticalOffset(37.21);
                    MedirReposo($"ESTADO-VOLCADO Exploracion/Cofres-PorTipo 1651x1204 barra=520 (barra lateral 37,21 + lista interna al fondo, listaOff={li.Value.sv.VerticalOffset:0.##}/{li.Value.sv.ScrollableHeight:0.##} listaExt={li.Value.sv.ExtentHeight:0.##})");
                    sidebarScroll.ScrollToVerticalOffset(0);
                    li.Value.sv.ScrollToVerticalOffset(0);
                }
                else Fallo("ESTADO-VOLCADO", "no se encontro la lista interna de Cofres/Por tipo o la barra lateral - ¿cambio la estructura?");
            }

            // 1) Matriz completa en el tamaño del volcado, sin y con cliente UIA externo.
            MedirExploracion("1651x1204 sinUIA", completo: true);
            vm.SelectedTabIndex = 0;
            MedirReposo("Inicio 1651x1204 (despues de Exploracion)");
            if (Environment.GetEnvironmentVariable("LAYOUT_REPOSO_SIN_UIA") != "1")
            {
                clienteUia = LanzarClienteUiaExterno(window);
                MedirExploracion("1651x1204 conUIA", completo: true);
                vm.SelectedTabIndex = 0;
                MedirReposo("Inicio 1651x1204 conUIA");
            }

            if (Environment.GetEnvironmentVariable("LAYOUT_REPOSO_RAPIDO") != "1")
            {
                var tamaños = new (double w, double h)[]
                {
                    (1080, 700), (1366, 768), (1519, 900), (1520, 900), (1651, 1204),
                    (1919, 1080), (1920, 1080), (2560, 1440),
                };
                var tabs = new (int idx, string nombre)[]
                { (0, "Inicio"), (2, "Builds"), (3, "Guia"), (5, "Hosting"), (6, "Novedades"), (7, "AcercaDe") };
                string[] subPersonaje = { "Objetos", "Buffs", "Investigacion", "Apariencia", "Aparicion", "Desbloqueos", "Version", "Comparar" };
                foreach (var (w, h) in tamaños)
                {
                    FijarTamaño(window, w, h);
                    string tam = $"{w:0}x{h:0}";
                    foreach (var (idx, nombre) in tabs)
                    {
                        vm.SelectedTabIndex = idx;
                        MedirReposo($"{nombre} {tam}");
                        var sv = Descendientes<ScrollViewer>(window).FirstOrDefault(s => s.IsVisible && s.ScrollableHeight > 0.5 && s.Name != "ExplorationSidebarScroll");
                        if (sv != null) { sv.ScrollToEnd(); MedirReposo($"{nombre} {tam} (scroll al fondo)"); sv.ScrollToVerticalOffset(0); }
                    }
                    vm.SelectedTabIndex = 1;
                    for (int i = 0; i < subPersonaje.Length; i++)
                    {
                        vm.PersonajeInnerTabIndex = i;
                        MedirReposo($"Personaje/{subPersonaje[i]} {tam}");
                    }
                    vm.PersonajeInnerTabIndex = 0;
                    // Barra lateral en su maximo real (520, el del volcado) en todos los tamaños; el de
                    // fabrica (320) tambien en las fronteras de SizeClass.
                    vm.Settings.ExplorationSidebarWidth = 520;
                    MedirExploracion(tam, completo: false);
                    if (w is 1519 or 1520 or 1919 or 1920)
                    {
                        vm.Settings.ExplorationSidebarWidth = 320;
                        MedirExploracion(tam, completo: false);
                    }
                }
                // EN en el tamaño del volcado.
                vm.Settings.Language = "en"; EsperarConLatido(20);
                FijarTamaño(window, 1651, 1204);
                vm.Settings.ExplorationSidebarWidth = 520;
                MedirExploracion("1651x1204 en", completo: true);
                vm.SelectedTabIndex = 7; MedirReposo("AcercaDe 1651x1204 en");
            }

            vm.Settings.ExplorationSidebarWidth = anchoSidebarOriginal;
            vm.Settings.Language = idiomaOriginal;
            vm.IsDirty = false;
            EsperarConLatido(20);
        }
        catch (Exception ex) { fallos++; Console.WriteLine("LAYOUT-REPOSO-EXCEPTION: " + ex); }
        finally
        {
            try { if (clienteUia != null && !clienteUia.HasExited) clienteUia.Kill(); } catch (Exception) { }
            window.LayoutUpdated -= contador;
            if (sidebarScroll != null) sidebarScroll.ScrollChanged -= contadorScroll;
        }
        Console.WriteLine($"LAYOUT_REPOSO_SOLO: {medidas} medida(s), {fallos} fallo(s)");
        return fallos;
    }
}
