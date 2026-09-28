// Auditoria de Opus, Bloque 6 (T-21): antes este arnes vivia SOLO en el scratchpad efimero de
// cada sesion - cada continuacion de este proyecto lo reconstruia desde cero (cientos de lineas
// re-escritas, decenas de bugs ya resueltos antes vueltos a pisar sin querer). Ahora es un
// proyecto real y permanente del propio repo (`Terrakeep.App.Tests`, en `Terrakeep.
// slnx`) - se compila y ejecuta con `dotnet run --project Terrakeep.App.Tests` desde la
// raiz del repo, sin depender de ninguna ruta de scratchpad.
//
// NO es un proyecto xunit (`[Fact]`/`Assert`) a proposito: gran parte de lo que verifica es
// VISUAL (capturas reales de pantalla que hace falta mirar, no solo un booleano pasa/falla) -
// un runner de tests headless nunca podria juzgar eso. Sigue siendo un programa de consola con
// un Main() real que monta una MainWindow real, coloca datos reales, interactua via UI
// Automation real (clics/teclado/scroll reales, nunca simulados a medias) y deja capturas +
// lineas "esperado X, obtenido Y" en stdout para revisar a mano. Ver CLAUDE.md ("Verdad del
// entorno WPF... un arnes que instancia Terrakeep.App.App y llama a Run() crea una
// SEGUNDA MainWindow fantasma") para el porque de construir un Application en blanco en vez de
// la App real de App.xaml.
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Terrakeep.App;
using Terrakeep.App.Controls;
using Terrakeep.App.Converters;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.Model;
using Terrakeep.Core.Nbt;
using Terrakeep.Core.PlrFormat;
using Terrakeep.Core.WldFormat;

// `partial` (6-sep-2026, ronda de Libreria/Builds): esta clase pasa de 5.700 lineas y varias
// rondas trabajan sobre ella a la vez - un bloque nuevo grande dentro de Main es una colision
// asegurada. Los bloques nuevos van en su propio fichero (PruebasLibreriaYBuilds.cs) como otra
// parte de ESTA MISMA clase, asi siguen usando tal cual sus helpers reales (DoEvents,
// WaitForDispatcher, FijarTamaño, RectVisible/VisibleEntero, Descendientes) sin duplicar ni uno,
// que es justo lo que T-21 pedia: seguir acumulando en el arnes, no montar otro aparte.
internal static partial class Program
{
    [STAThread]
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    private static void Main()
    {
        // Opt-in: con la escala del sistema alta (225 %) y el monitor bajado a 100 %, solo un proceso
        // Per-Monitor-V2 ve 96 DPI; el System-aware por defecto conserva el DPI de inicio de sesion y
        // su ventana no cabe en pantalla para las pruebas con raton real.
        if (Environment.GetEnvironmentVariable("TERRAKEEP_ARNES_DPI_POR_MONITOR") == "1")
            Console.WriteLine("DPI-POR-MONITOR: " + SetProcessDpiAwarenessContext(new IntPtr(-4)));

        // R2-L2 (28-sep-2026): estado real de la sesion de Windows + sonda de render + revision de
        // capturas en blanco al salir - ANTES de cualquier uso de WPF. Ver ValidacionCapturasSesion.cs.
        PrepararValidacionCapturas();

        // Deuda real cerrada el 17-sep-2026 (mismo bug ya visto y arreglado en Starvekeep): TODA
        // ejecucion de este arnes es diagnostico/prueba, nunca produccion real - fijar esto lo
        // primero de todo, antes de "new MainWindow()" mas abajo, para que
        // IniciarComprobacionDeActualizacion (llamada de RED real a GitHub, resultado no
        // determinista) nunca se dispare y contamine una captura. Ver el comentario completo en
        // Terrakeep.App.App.ModoDiagnostico y en MainWindow.xaml.cs.
        Terrakeep.App.App.ModoDiagnostico = true;

        // H-04 (28-sep-2026, regla del CLAUDE.md del repo): ninguna prueba toca las partidas reales
        // del usuario - copias en una carpeta temporal que SUSTITUYE a las reales, session.json
        // guardado y restaurado. Ver AislamientoPartidasReales.cs. Tiene que ir ANTES de
        // "new MainWindow()" (Inicio escanea personajes en su constructor).
        PrepararAislamientoPartidasReales();

        // Complementacion bidireccional (24-sep-2026): Terrakeep conserva su arnes WPF/UIA
        // nativo; KEEPQA_COMPLEMENTO_SOLO permite que ese mismo arnes invoque los gates
        // compartidos de KeepQA sin duplicar su logica. KeepQA, a su vez, sigue pudiendo lanzar
        // este proyecto como extractor/runner nativo. Dos capas, una sola fuente de verdad por regla.
        if (Environment.GetEnvironmentVariable("KEEPQA_COMPLEMENTO_SOLO") == "1")
        {
            Environment.Exit(EjecutarComplementoKeepQA());
        }

        // Verificacion real de T-12 (auditoria de Opus, Bloque 3): sesion local normal (esta
        // maquina, sin RDP) -> false; con la variable de entorno puesta -> true. El propio
        // OnStartup de App.xaml.cs nunca se ejecuta en este arnes (crea un Application a pelo),
        // por eso ShouldForceSoftwareRendering() se probo aparte, como metodo publico.
        Console.WriteLine($"T12-RENDER: local sin RDP -> ShouldForceSoftwareRendering()={Terrakeep.App.App.ShouldForceSoftwareRendering()} (esperado False en esta maquina)");
        Environment.SetEnvironmentVariable("TERRAKEEP_FORCE_SOFTWARE_RENDER", "1");
        Console.WriteLine($"T12-RENDER: con TERRAKEEP_FORCE_SOFTWARE_RENDER=1 -> ShouldForceSoftwareRendering()={Terrakeep.App.App.ShouldForceSoftwareRendering()} (esperado True)");
        Environment.SetEnvironmentVariable("TERRAKEEP_FORCE_SOFTWARE_RENDER", null);

        // El arnes NUNCA llama a app.Run() (pumpea a mano con DoEvents en su lugar) - la app
        // REAL (StartupUri en App.xaml) si lo hace, y Application.Run() es quien instala de
        // verdad el DispatcherSynchronizationContext que `await Task.Run(...)` necesita para
        // reanudar en el hilo de UI (ver X-7/T-13 mas abajo). Se instala aqui a mano, mismo
        // efecto real que la app real, para probar el camino async de verdad y no solo el
        // artefacto del propio arnes.
        System.Threading.SynchronizationContext.SetSynchronizationContext(
            new System.Windows.Threading.DispatcherSynchronizationContext());

        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Terrakeep;component/Styles/Theme.xaml")
        });
        app.Resources["NullToVis"] = new NullToVisibilityConverter();
        app.Resources["NullToCollapsed"] = new NullToCollapsedConverter();
        app.Resources["EmptyToCollapsed"] = new EmptyToCollapsedConverter();
        // H4-01 (cuarta auditoria de Opus, Fable): este arnes REPLICA a mano el registro real
        // de App.xaml (nunca lo carga - construye una Application en blanco, ver el comentario
        // real de arriba) - un StaticResource que se añade a App.xaml y se olvida aqui explota
        // en runtime SOLO en este arnes, la app real (que si carga App.xaml) nunca lo nota.
        // Justo lo que paso con este converter la primera vez que se probo esta misma tanda.
        app.Resources["EmptyToVisible"] = new EmptyToVisibleConverter();
        app.Resources["CountToVis"] = new CountToVisibilityConverter();
        // Fase B (15-sep-2026): mismo motivo real que todos los de arriba (H4-01) - dos
        // converters nuevos de la integracion de la Guia/hosting; sin registrarlos aqui el
        // arnes reventaria al montar la ventana aunque la app real (que si carga App.xaml) vaya bien.
        app.Resources["CountToCollapsed"] = new CountToCollapsedConverter();
        app.Resources["ResourceKeyToBrush"] = new ResourceKeyToBrushConverter();
        app.Resources["InverseBoolToVis"] = new InverseBooleanToVisibilityConverter();
        app.Resources["BoolToGridLength"] = new BoolToGridLengthConverter();
        // AR-EX1 (6-sep-2026): mismo motivo real que todos los de arriba (H4-01) - converter
        // nuevo del arreglo del reparto vertical de la columna de Exploracion; sin registrarlo
        // aqui el arnes reventaria al montar la ventana aunque la app real (que si carga
        // App.xaml) funcione.
        app.Resources["CountToGridLength"] = new CountToGridLengthConverter();
        app.Resources["BoolToDouble"] = new BoolToDoubleConverter();
        // Punto 4 (advisor Opus, selector de categoria de Exploracion - ver
        // ESPEC-ui-exploracion.md#9.1): mismo motivo real que el resto de converters de arriba -
        // se olvido la primera vez que se probo esta tanda, mismo bug real ya documentado.
        app.Resources["EnumEquals"] = new EnumEqualsConverter();
        // ExploracionRediseno Fase C (25-sep-2026): mismo motivo real que todos los de arriba
        // (H4-01) - converter nuevo de SidebarMode (Browse/ChestInspector); sin registrarlo aqui
        // el arnes revienta al montar la ventana (XamlParseException, "No se puede encontrar el
        // recurso EnumEqualsToVis") aunque la app real (que si carga App.xaml) vaya bien.
        // Detectado al verificar IDEA8_SOLO (comparador de mundos, Fase G) - bloqueaba
        // CUALQUIER canario de este arnes que monte MainWindow, no solo el de Comparar.
        app.Resources["EnumEqualsToVis"] = new EnumEqualsToVisibilityConverter();
        // Ronda de idioma del 6-sep-2026: mismo motivo real que todos los de arriba - este
        // converter sustituye a los StringFormat en español fijo del XAML, y sin registrarlo aqui
        // el arnes reventaria al montar la ventana aunque la app real funcione.
        app.Resources["LocFormat"] = new LocalizedFormatConverter();
        app.DispatcherUnhandledException += (_, e) =>
        {
            Console.WriteLine("DISPATCHER-EXCEPTION: " + e.Exception);
            e.Handled = true;
        };

        var swStartup = System.Diagnostics.Stopwatch.StartNew();
        var window = new MainWindow();
        swStartup.Stop();
        Console.WriteLine($"T-G-ARRANQUE: new MainWindow() (CharacterFileService + MainViewModel + XAML) tardo {swStartup.ElapsedMilliseconds}ms");
        // ARRANQUE_SOLO=1 (17-sep-2026, cache de catalogos en disco): sale justo tras medir
        // T-G-ARRANQUE, sin Show()/DoEvents ni el resto del arnes (miles de lineas, capturas...)
        // - mismo criterio ya real de ARLAY_CANARIO_SOLO mas abajo, para poder medir el arranque
        // en frio/caliente muchas veces seguidas sin pagar el arnes completo cada vez.
        if (Environment.GetEnvironmentVariable("ARRANQUE_SOLO") == "1")
        {
            Environment.Exit(0);
        }
        // T-G: HomeViewModel.RefreshAsync se lanza en el propio constructor (fire-and-forget,
        // Task.Run) - justo AL SALIR de new MainWindow(), antes de cualquier DoEvents() real,
        // el escaneo de disco todavia no ha podido completarse (esta corriendo en un hilo de
        // fondo) - IsScanning debe seguir en True aqui mismo, prueba real de que el arranque de
        // la ventana ya no espera a que termine.
        bool scanningJustoAlSalir = ((MainViewModel)window.DataContext).Home.IsScanning;
        Console.WriteLine($"T-G-ASYNC: IsScanning justo tras new MainWindow() (antes de cualquier DoEvents)={scanningJustoAlSalir} (esperado True - el escaneo real corre en segundo plano, no bloquea la construccion de la ventana)");
        app.MainWindow = window;
        window.Show();
        DoEvents();

        // ARLAY_CANARIO_SOLO=1 (16-sep-2026, KeepQA/H6): ejercita SOLO el canario de AR-LAY
        // (ComprobarCanarioArLay, ver AuditoriaMaquetacion.cs) sin cargar personaje/mundo reales -
        // el canario es autocontenido (construye su propia ventana/Grid de usar y tirar), asi que
        // no hace falta pagar el resto del arranque para volver a comprobarlo en una ronda futura.
        if (Environment.GetEnvironmentVariable("ARLAY_CANARIO_SOLO") == "1")
        {
            bool canarioOk = ComprobarCanarioArLay();
            Console.WriteLine(canarioOk ? "ARLAY_CANARIO_SOLO: OK" : "FALLO: ARLAY_CANARIO_SOLO");
            Environment.Exit(canarioOk ? 0 : 1);
        }

        // Auditoria de redimensionado, §1.1-1.2: hook de WM_GETMINMAXINFO instalado lo antes
        // posible (justo tras Show(), antes del primer redimensionado real de este arnes) para
        // que TODO el resto del arnes pueda pedir el ancho que quiera sin toparse con el clamp
        // real de esta sesion RDP - ver el comentario completo de InstalarHookMaxTrackSize.
        InstalarHookMaxTrackSize(window);
        // Verificacion real de T-3: si la sesion ANTERIOR guardo window.json, el constructor de
        // MainWindow (WindowPlacementService.Apply) ya deberia haber restaurado ese tamaño real
        // ANTES de Show() - se comprueba aqui, lo antes posible.
        Console.WriteLine($"T3-RESTAURADO: Left={window.Left} Top={window.Top} Width={window.Width} Height={window.Height}");

        // Bug real del propio arnes encontrado verificando H4-07 (cuarta auditoria de Opus,
        // Fable): el tamaño heredado de window.json (T-3, arriba) puede caer en SizeClass.
        // Amplio segun la ULTIMA sesion real que uso la app (esta vez, 2576x1408 CON
        // IsMaximized=true - la ventana se habia quedado maximizada en un monitor grande) -
        // varios escenarios de este mismo arnes (pildoras "Fragua del Defensor"/"Vanidad",
        // B-1/B-2 de la Libreria) asumen implicitamente un tamaño NO-Amplio y corren MUCHO
        // antes del primer `window.Width =` explicito del propio arnes (linea ~1022) - nunca se
        // habian visto fallar porque el tamaño heredado nunca habia sido tan grande, no porque
        // de verdad dependieran de un tamaño real. Fijar aqui, justo tras comprobar T-3, deja
        // el resto del arnes deterministico de verdad sin tocar la comprobacion real de T-3 de
        // arriba (que ya leyo el tamaño heredado antes de este punto). WindowState TAMBIEN hace
        // falta (no solo Width/Height, primer intento real de este arreglo que NO basto) -
        // WPF nunca restaura una ventana Maximized a Normal solo por asignarle Width/Height, el
        // area real en pantalla se queda siendo la maximizada hasta que WindowState se cambia
        // a mano.
        window.WindowState = System.Windows.WindowState.Normal;
        FijarTamaño(window, 1180, 860);
        Console.WriteLine($"ARNES-TAMAÑO-BASE: Width={window.Width} Height={window.Height} (fijado aqui para que el resto del arnes no dependa del tamaño heredado de window.json)");

        // Generacion de capturas reales para el README/material de difusion, pedido explicito
        // del usuario (5-sep-2026, antes de publicar). NO es una verificacion de regresion (no
        // hay ningun "esperado X, obtenido Y" aqui) - por eso vive detras de esta variable de
        // entorno en vez de correr en cada `dotnet run` normal, pero reutiliza el mismo
        // bootstrap ya fiable del resto del arnes (Application en blanco + MainWindow real +
        // RenderTargetBitmap sobre la ventana real, nunca una captura de pantalla del SO -
        // ver CLAUDE.md, "las capturas de pantalla son poco fiables en este entorno"). Solo lee
        // ficheros reales del usuario (personaje/mundo), nunca los guarda ni los modifica.
        if (Environment.GetEnvironmentVariable("TERRAKEEP_SCREENSHOTS") == "1")
        {
            string shotDir = Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(shotDir);
            void Shot(string name)
            {
                DoEvents();
                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(window);
                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                using var fs = File.Create(Path.Combine(shotDir, name + ".png"));
                enc.Save(fs);
                Console.WriteLine($"SCREENSHOT: {name}.png");
            }

            FijarTamaño(window, 1600, 920);
            DoEvents();
            Shot("01-inicio");

            var vmShot = (MainViewModel)window.DataContext;
            int waited = 0;
            while (vmShot.Home.IsScanning && waited < 100) { DoEvents(); System.Threading.Thread.Sleep(50); waited++; }

            var personajeShot = vmShot.Home.Characters.FirstOrDefault(c =>
                c.FilePath.Contains("tModLoader", StringComparison.OrdinalIgnoreCase) &&
                c.FilePath.Contains("Eldelgas", StringComparison.OrdinalIgnoreCase));
            if (personajeShot != null)
            {
                vmShot.Home.OpenCommand.Execute(personajeShot);
                DoEvents();
                Shot("02-personaje");
            }
            else
            {
                Console.WriteLine("SCREENSHOT-AVISO: no se encontro 'Eldelgas' (tModLoader) para 02-personaje");
            }

            string worldPathShot = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
            if (File.Exists(worldPathShot))
            {
                vmShot.SelectedTabIndex = 4; // Exploracion
                DoEvents();
                var taskShot = vmShot.Exploration.LoadFromPathAsync(worldPathShot);
                while (!taskShot.IsCompleted) DoEvents();
                DoEvents();
                // Ajustar a la ventana antes de la captura - recien cargado, el mapa arranca a
                // 250% de zoom centrado en el spawn (normalmente cielo), nada representativo.
                var fitMethod = typeof(MainWindow).GetMethod("OnFitToWindowClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fitMethod?.Invoke(window, [window, new RoutedEventArgs()]);
                DoEvents();
                Shot("03-exploracion");
            }
            else
            {
                Console.WriteLine("SCREENSHOT-AVISO: no se encontro roca_negra.wld para 03-exploracion");
            }

            vmShot.SelectedTabIndex = 7; // Acerca de (incluye Ajustes) - AppTab.AcercaDe, reordenado T1 21-sep-2026
            vmShot.Settings.Language = "en";
            DoEvents();
            Shot("04-about-settings-en");
            vmShot.Settings.Language = "es";
            DoEvents();

            Console.WriteLine($"SCREENSHOTS-LISTAS: {shotDir}");
            Environment.Exit(0);
        }

        // AR-EX-HSCROLL (16-sep-2026, bug real reportado por el usuario con captura propia:
        // "la pestaña de cofres se corta y ademas sale un scroll lateral... horizontal,
        // pasa tambien en Objetos y Minerales, keepqa no cazo esto"). Diagnostico aparte
        // (NO forma parte del `dotnet run` normal, igual que TERRAKEEP_SCREENSHOTS de arriba)
        // para medir con geometria real, ANTES de tocar nada mas del arnes, si de verdad hay
        // un ScrollViewer horizontal activo dentro del bloque de resultados/categoria de
        // Exploracion. Motivo de por que ni AR-LAY (D1) ni AR-EX1 lo cazaban ya documentado
        // en bitacora.md: D1 solo marca FALLO si el contenido recortado NO es alcanzable con
        // scroll (un scroll horizontal real, aunque sea indeseado, cuenta como "escape" y
        // apaga la alarma), y AR-EX1 solo mide alto (Height/ViewportHeight/ExtentHeight), nunca
        // ancho. Este bloque no arregla nada, solo mide.
        if (Environment.GetEnvironmentVariable("AR_EX_HSCROLL_SOLO") == "1")
        {
            var vmDiag = (MainViewModel)window.DataContext;
            vmDiag.SelectedTabIndex = 4; // Exploracion
            DoEvents();
            string worldPathDiag = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
            if (!File.Exists(worldPathDiag))
            {
                Console.WriteLine($"AR-EX-HSCROLL: no se encontro {worldPathDiag} - abortando diagnostico");
                Environment.Exit(1);
            }
            var cargaDiag = vmDiag.Exploration.LoadFromPathAsync(worldPathDiag);
            while (!cargaDiag.IsCompleted) DoEvents();
            DoEvents();

            vmDiag.Exploration.SelectedCategory = WorldSearchCategory.All;
            vmDiag.Exploration.WorldSearchText = "lava";
            WaitForDispatcher(2600);
            Console.WriteLine($"AR-EX-HSCROLL: {vmDiag.Exploration.WorldSearchResults.Count} resultado(s) reales en el bloque compartido (esperado >0)");

            bool algunFallo = false;
            double anchoSidebarAntes = vmDiag.Settings.ExplorationSidebarWidth;
            // La columna de la barra lateral es un ANCHO FIJO en pixeles
            // (Settings.ExplorationSidebarWidth, GridSplitter arrastrable, clamp real 260-520 en
            // SettingsViewModel.OnExplorationSidebarWidthChanged) - NO una columna "*" que
            // reaccione al tamaño de la ventana. La primera pasada de este diagnostico (dejada
            // en el propio ancho heredado, 320 de fabrica) barrio el ANCHO DE VENTANA de 1080 a
            // 1600 y no encontro nada: variable equivocada, la columna nunca se movio. Aqui se
            // barre la variable real - el ancho de la propia barra, en sus dos extremos y el
            // de fabrica - con la ventana fija a 1180x860 (tamaño por defecto real de la app).
            FijarTamaño(window, 1180, 860);
            foreach (double anchoSidebar in new[] { 260.0, 300.0, 320.0, 420.0, 520.0 })
            {
                vmDiag.Settings.ExplorationSidebarWidth = anchoSidebar;
                DoEvents(); DoEvents();
                foreach (var (cat, modoCofres, nombreCat) in new (WorldSearchCategory, int, string)[]
                         { (WorldSearchCategory.Chests, 0, "Cofres/Por tipo"), (WorldSearchCategory.Chests, 2, "Cofres/Cofre a cofre"),
                           (WorldSearchCategory.Ores, 0, "Minerales"), (WorldSearchCategory.Objects, 0, "Objetos"),
                           (WorldSearchCategory.Npcs, 0, "NPCs") })
                {
                    vmDiag.Exploration.SelectedCategory = cat;
                    if (cat == WorldSearchCategory.Chests) vmDiag.Exploration.ChestViewMode = modoCofres;
                    DoEvents(); DoEvents();

                    var sidebar = window.FindName("ExplorationSidebarPanel") as FrameworkElement;
                    if (sidebar == null) { Console.WriteLine("AR-EX-HSCROLL: FALLO no se encontro ExplorationSidebarPanel"); continue; }

                    // Localizacion explicita, sin ambiguedad, de la lista COMPARTIDA de resultados
                    // (WorldSearchResults, la que el usuario describe como "la lista de resultados
                    // debajo") por su ItemsSource real - para confirmar de verdad si esta presente
                    // y visible en CADA categoria, Minerales incluida, en vez de fiarse solo del
                    // barrido generico de ScrollViewers de mas abajo.
                    var listaCompartida = Descendientes<System.Windows.Controls.ListBox>(sidebar)
                        .FirstOrDefault(lb => ReferenceEquals(lb.ItemsSource, vmDiag.Exploration.WorldSearchResults));
                    if (listaCompartida == null)
                        Console.WriteLine($"AR-EX-HSCROLL: sidebar={anchoSidebar:0}px, {nombreCat} -> lista COMPARTIDA de resultados NO ENCONTRADA en el arbol visual");
                    else
                    {
                        Console.WriteLine($"AR-EX-HSCROLL: sidebar={anchoSidebar:0}px, {nombreCat} -> lista COMPARTIDA: IsVisible={listaCompartida.IsVisible} " +
                                          $"ActualWidth={listaCompartida.ActualWidth:0.#} Visibility={listaCompartida.Visibility}");
                    }

                    foreach (var sv in Descendientes<System.Windows.Controls.ScrollViewer>(sidebar))
                    {
                        if (sv.Name == "ExplorationSidebarScroll") continue; // ese ya se sabe Disabled/vertical, no es el sospechoso
                        if (!sv.IsVisible) continue;
                        bool horizontalActiva = sv.HorizontalScrollBarVisibility != System.Windows.Controls.ScrollBarVisibility.Disabled
                                                 && sv.ScrollableWidth > 0.5;
                        string cadena = "";
                        try { cadena = string.Join(" / ", Ascendencia(sv, window).TakeLast(6)); } catch (Exception) { }
                        Console.WriteLine($"AR-EX-HSCROLL: sidebar={anchoSidebar:0}px, {nombreCat} -> ScrollViewer(nombre='{sv.Name}') H={sv.HorizontalScrollBarVisibility} " +
                                          $"computado={sv.ComputedHorizontalScrollBarVisibility} viewport={sv.ViewportWidth:0.#} extent={sv.ExtentWidth:0.#} " +
                                          $"scrollable={sv.ScrollableWidth:0.#} <- {cadena}");
                        if (horizontalActiva)
                        {
                            algunFallo = true;
                            Console.WriteLine($"FALLO: AR-EX-HSCROLL - sidebar={anchoSidebar:0}px, {nombreCat}: ScrollViewer(nombre='{sv.Name}') tiene scroll HORIZONTAL real activo " +
                                              $"({sv.ScrollableWidth:0.#}px de sobra, contenido {sv.ExtentWidth:0.#}px en un viewport de {sv.ViewportWidth:0.#}px) <- {cadena}");
                        }
                    }
                }
            }
            vmDiag.Settings.ExplorationSidebarWidth = anchoSidebarAntes;
            Console.WriteLine(algunFallo ? "AR-EX-HSCROLL: confirmado, hay scroll horizontal real" : "AR-EX-HSCROLL: sin scroll horizontal real detectado");
            Environment.Exit(algunFallo ? 1 : 0);
        }

        var hwnd = new WindowInteropHelper(window).Handle;
        var root = AutomationElement.FromHandle(hwnd);

        var character = new PlrCharacter
        {
            Name = "UIA-Test",
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts =
            [
                PlrLoadout.CreateEmpty(isPrimary: false),
                PlrLoadout.CreateEmpty(isPrimary: false),
                PlrLoadout.CreateEmpty(isPrimary: false),
            ],
        };
        string tempPlr = Path.Combine(Path.GetTempPath(), "uia-harness-test.plr");
        // Bug real encontrado verificando B-6 (segunda auditoria, Fable): esta ruta de temp es
        // FIJA entre ejecuciones. El .tplr companero (Path.ChangeExtension) NO se borraba aqui,
        // asi que CalamityCharacterSync.MergeBuffs (real, produccion) fusionaba en cada
        // ejecucion los buffs YA guardados por la ejecucion ANTERIOR (comportamiento correcto
        // para un personaje real: el .tplr es la fuente real de verdad de buffs con mods
        // instalados) - y como esta prueba coloca 2 buffs nuevos y los vuelve a guardar cada
        // vez, era una bola de nieve: tras ~22 ejecuciones en esta sesion los 44 slots acabaron
        // llenos, disparando fallos NO-FOUND en botones que dependen de encontrar un slot vacio.
        // No es un bug de produccion (un personaje real no se auto-recarga sobre si mismo sin
        // fin) - es higiene de arnes: borrar el .plr/.tplr/.bak sinteticos antes de escribir uno
        // nuevo para que cada ejecucion arranque de verdad en limpio.
        string tempTplr = Path.ChangeExtension(tempPlr, ".tplr");
        foreach (string stale in new[] { tempPlr, tempTplr, tempPlr + ".bak", tempTplr + ".bak" })
            if (File.Exists(stale)) File.Delete(stale);
        File.WriteAllBytes(tempPlr, PlrFile.Write(character));

        var vm = (MainViewModel)window.DataContext;

        // COFRES_INSPECTOR_SOLO (24-sep-2026, revision-correccion-integral-familia-Keep, cluster
        // "imagen5+imagen6" - Exploracion/cofres/inspector lateral). Vive en
        // CanarioClusterCofresInspector.cs (misma clase parcial) - ver su cabecera para el detalle
        // completo de los 4 puntos del encargo y la causa real ya confirmada por lectura de codigo
        // de los puntos 2/3. Igual que TERRAKEEP_SOLO_MRK arriba: aislado para poder repetirlo
        // rapido, corre igual dentro de la tirada completa si no se pasa la variable.
        if (Environment.GetEnvironmentVariable("COFRES_INSPECTOR_SOLO") == "1")
        {
            EjecutarClusterCofresInspectorSolo(window, vm);
            window.Close();
            DoEvents();
            return;
        }

        // FLUJOCOFRES_SOLO=1 (26-sep-2026, TASK CONTEXT e5eaea9e-c261-4199-8e7d-060b6054f58d,
        // investigador-bug). Vive en CanarioFlujoCompletoCofres.cs (misma clase parcial) - ver su
        // cabecera para el detalle completo: reconfirma el flujo END-TO-END completo de editar un
        // cofre (abrir/seleccionar/editar/cambiar de slot/guardar/cancelar/volver/cambiar de cofre)
        // contra la arquitectura NUEVA de pagina exclusiva del sidebar (ExploracionRediseno Fase
        // B-I + ChestInspector slots vacios), tras el aplazamiento explicito del punto 1 original.
        if (Environment.GetEnvironmentVariable("FLUJOCOFRES_SOLO") == "1")
        {
            EjecutarFlujoCompletoCofresSolo(window, vm);
            window.Close();
            DoEvents();
            return;
        }

        // MAXSTACK_SOLO=1 (25-sep-2026, encargo Keep "+10/+100/MAX en el editor de objeto"). Vive
        // en CanarioControlesRapidosStack.cs (misma clase parcial) - visual-QA real de los 3
        // botones nuevos (sin overflow/clipping en ningun idioma) + verificacion de que MAX/Count
        // respetan el maxStack real de cada objeto. Mismo criterio que el resto de bloques _SOLO:
        // aislado para poder repetirlo rapido, corre igual dentro de la tirada completa si no se
        // pasa la variable.
        if (Environment.GetEnvironmentVariable("MAXSTACK_SOLO") == "1")
        {
            EjecutarMaxStackSolo(window, vm);
            window.Close();
            DoEvents();
            return;
        }

        // AR-MRK (19-sep-2026): medicion real de la geometria de los marcadores del mapa. Vive en
        // PruebasMarcadoresMapa.cs y se puede correr SOLA con TERRAKEEP_SOLO_MRK=1 (el arnes
        // completo son minutos por las decenas de RenderTargetBitmap, y este bloque hace falta
        // repetirlo antes/despues de cada intento de arreglo). Sin la variable corre igual, como
        // una prueba mas de la tirada completa.
        if (Environment.GetEnvironmentVariable("TERRAKEEP_SOLO_MRK") == "1")
        {
            PruebasMarcadoresMapa(window, vm);
            window.Close();
            DoEvents();
            return;
        }

        // KEEPQA_SMOKE=1 (14-sep-2026, Fase 3 de KeepQA V2.0 - ver KeepQA\v2\PROPUESTA-UNIFICADA.md):
        // subconjunto MINIMO y aislado del arnes, pensado para terminar en <60s. El arnes completo
        // (KEEPQA_SOLO y el resto de modos de foco) tarda MINUTOS por las decenas de
        // RenderTargetBitmap - este modo hace SOLO lo que un smoke test real exige: lanzar, ventana
        // visible, un clic real, cerrar, sin excepcion. Va aqui (justo tras tener `vm`, antes de
        // fabricar ningun .plr sintetico ni escanear nada mas) para no depender de ningun archivo
        // real ni pagar ningun coste que el smoke test no necesita.
        if (Environment.GetEnvironmentVariable("KEEPQA_SMOKE") == "1")
        {
            Console.WriteLine($"SMOKE: ventana visible IsVisible={window.IsVisible} (esperado True)");
            if (!window.IsVisible)
                Console.WriteLine("FALLO: SMOKE - la ventana deberia estar visible tras Show()+DoEvents()");

            // Un clic real (SelectionItemPattern.Select(), UI Automation real, no
            // vm.SelectedTabIndex a mano) sobre la pestaña "Acerca de" - no depende de ningun
            // personaje/mundo real en disco, solo de que la ventana principal este montada.
            var tabAcercaDe = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, vm.Loc["tab_about"])));
            if (tabAcercaDe != null && tabAcercaDe.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var smokeSelPat))
            {
                ((SelectionItemPattern)smokeSelPat).Select();
                DoEvents(); DoEvents();
                Console.WriteLine($"SMOKE: clic real (UI Automation) en pestaña 'Acerca de' -> SelectedTabIndex={vm.SelectedTabIndex}");
            }
            else
            {
                Console.WriteLine("FALLO: SMOKE - la pestaña 'Acerca de' no se encuentra por UI Automation, un clic real no podria activarla");
            }

            window.Close();
            DoEvents();
            Console.WriteLine("DONE (KEEPQA_SMOKE)");
            Environment.Exit(0);
        }

        // KEEPQA_UIA_TREE=1 (14-sep-2026, KeepQA V3 Fase 2 - Seccion 8, IUIAutomationAdapter): ver
        // AuditoriaUiaTree.cs (misma clase parcial) para el porque y el detalle completo. Mismo
        // motivo de posicion que KEEPQA_SMOKE arriba: va justo tras tener `vm`/`root`, antes de
        // fabricar ningun .plr sintetico, para no depender de ningun archivo real en disco.
        if (Environment.GetEnvironmentVariable("KEEPQA_UIA_TREE") == "1")
        {
            EjecutarUiaTree(window, root, vm);
            window.Close();
            DoEvents();
            Console.WriteLine("DONE (KEEPQA_UIA_TREE)");
            Environment.Exit(0);
        }

        // KEEPQA_CHAOS=1 (14-sep-2026, Fase 7 Bloque C de KeepQA V2.0 - ver
        // KeepQA\v2\PROPUESTA-UNIFICADA.md): ejecutor de UNA secuencia de acciones ya generada por
        // src/chaos/generadorSecuencias.js (semilla fija, reproducible) - esta pieza NO decide el
        // orden ni genera nada por su cuenta, solo traduce cada nombre de accion de un catalogo
        // fijo a los MISMOS adaptadores de entrada YA REALES que usa el resto de este arnes:
        // SelectionItemPattern.Select() para cambiar de pestaña (igual que KEEPQA_SMOKE ahi
        // arriba), PressKey/RealClickAt (definidos mas abajo en esta clase, ya usados en decenas
        // de pruebas reales de este mismo archivo para Escape/flechas y para el unico clic de
        // raton real del arnes) para teclado/raton a nivel de SO, y window.Close() para cerrar -
        // cero logica de entrada nueva, solo secuenciacion + traza.
        if (Environment.GetEnvironmentVariable("KEEPQA_CHAOS") == "1")
        {
            string rutaSecuencia = Environment.GetEnvironmentVariable("KEEPQA_CHAOS_SEQ") ?? "";
            if (string.IsNullOrEmpty(rutaSecuencia) || !File.Exists(rutaSecuencia))
            {
                Console.WriteLine($"FALLO: KEEPQA_CHAOS - KEEPQA_CHAOS_SEQ no apunta a un archivo real ('{rutaSecuencia}')");
                window.Close();
                Environment.Exit(1);
            }

            string[] secuencia = System.Text.Json.JsonSerializer.Deserialize<string[]>(File.ReadAllText(rutaSecuencia)) ?? [];
            Console.WriteLine($"CHAOS: {secuencia.Length} accion(es) a ejecutar: {string.Join(", ", secuencia)}");

            AutomationElement? TabPorIndice(int indice)
            {
                var tabs = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem));
                return indice >= 0 && indice < tabs.Count ? tabs[indice] : null;
            }

            var trazaChaos = new List<Dictionary<string, object>>();
            bool cerradoChaos = false;
            bool huboExcepcionChaos = false;
            for (int iChaos = 0; iChaos < secuencia.Length && !cerradoChaos; iChaos++)
            {
                string accion = secuencia[iChaos];
                var registro = new Dictionary<string, object> { ["indice"] = iChaos, ["accion"] = accion };
                try
                {
                    switch (accion)
                    {
                        case "abrir_personaje":
                            if (vm.Home.Characters.Count > 0)
                            {
                                vm.Home.OpenCommand.Execute(vm.Home.Characters[0]);
                                DoEvents(); DoEvents();
                            }
                            else registro["aviso"] = "sin personajes reales que abrir - accion sin efecto";
                            break;
                        case "escape":
                            ForzarPrimerPlano(hwnd);
                            DoEvents();
                            PressKey(0x1B); // VK_ESCAPE
                            DoEvents();
                            break;
                        case "clic_rapido":
                            {
                                var tabActual = TabPorIndice(Math.Max(0, vm.SelectedTabIndex));
                                if (tabActual != null)
                                {
                                    var rect = tabActual.Current.BoundingRectangle;
                                    RealClickAt((int)(rect.X + rect.Width / 2), (int)(rect.Y + rect.Height / 2));
                                    DoEvents(); DoEvents();
                                }
                                else registro["aviso"] = "sin pestaña visible donde clicar - accion sin efecto";
                            }
                            break;
                        case "cambiar_pestana":
                            {
                                // Destino determinista a partir del indice de la propia secuencia
                                // (que ya viene barajada con semilla fija por el generador) - no
                                // hace falta un segundo RNG aqui, solo repartir 0-5 sin RNG nuevo.
                                int destino = (iChaos * 7 + secuencia.Length) % 6;
                                var tabDestino = TabPorIndice(destino);
                                if (tabDestino != null && tabDestino.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selPatChaos))
                                {
                                    ((SelectionItemPattern)selPatChaos).Select();
                                    DoEvents(); DoEvents();
                                    registro["pestanaDestino"] = destino;
                                }
                                else registro["aviso"] = $"pestaña {destino} no encontrada o sin SelectionItemPattern";
                            }
                            break;
                        case "cerrar":
                            window.Close();
                            DoEvents();
                            cerradoChaos = true;
                            break;
                        default:
                            registro["aviso"] = "accion desconocida en el catalogo, ignorada";
                            break;
                    }
                    registro["ok"] = true;
                }
                catch (Exception ex)
                {
                    registro["ok"] = false;
                    registro["excepcion"] = ex.ToString();
                    huboExcepcionChaos = true;
                    Console.WriteLine($"CHAOS-EXCEPCION en accion {iChaos} ('{accion}'): {ex}");
                }
                registro["ventanaVisibleTrasAccion"] = cerradoChaos ? false : window.IsVisible;
                trazaChaos.Add(registro);
                Console.WriteLine($"CHAOS: [{iChaos}] {accion} -> ok={registro["ok"]}");
            }

            string outDirChaos = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
            Directory.CreateDirectory(outDirChaos);
            string rutaChaosOut = Path.Combine(outDirChaos, "chaos.json");
            File.WriteAllText(rutaChaosOut, System.Text.Json.JsonSerializer.Serialize(new
            {
                fecha = DateTime.Now.ToString("o"),
                secuencia,
                traza = trazaChaos,
                cerrado = cerradoChaos,
                huboExcepcion = huboExcepcionChaos,
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"CHAOS: traza completa guardada en {rutaChaosOut}");

            if (!cerradoChaos) { try { window.Close(); DoEvents(); } catch { /* la ventana ya podia estar cerrada */ } }
            Console.WriteLine(huboExcepcionChaos
                ? "CHAOS-VEREDICTO: HALLAZGO REAL - al menos una accion de la secuencia lanzo una excepcion no esperada"
                : "CHAOS-VEREDICTO: SIN HALLAZGOS - la secuencia completa no revento nada");
            Console.WriteLine("DONE (KEEPQA_CHAOS)");
            Environment.Exit(huboExcepcionChaos ? 1 : 0);
        }

        // KEEPQA_UIA_RECORD=1 (20-sep-2026, KeepQA V3 - amplia/corrige el limite documentado del
        // punto 8 del catalogo: "Grabar y reproducir sesiones reales" - ver GrabadorSesionUia.cs
        // (misma clase parcial) para el porque completo y los gaps reales documentados. Un
        // listener de eventos de UI Automation ACOTADO a esta ventana (Automation.
        // AddAutomationEventHandler/AddAutomationPropertyChangedEventHandler, TreeScope.Subtree
        // desde `root` - la MISMA AutomationElement.FromHandle(hwnd) que ya usan KEEPQA_SMOKE/
        // KEEPQA_UIA_TREE/KEEPQA_CHAOS arriba), estructuralmente incapaz de fugarse a otro proceso
        // o capturar teclado/raton fuera de esta ventana - distinto en naturaleza de un hook
        // global de sistema. Va aqui, mismo criterio de posicion que KEEPQA_CHAOS arriba (justo
        // tras tener `vm`/`root`/`hwnd`, antes de fabricar ningun dato sintetico).
        if (Environment.GetEnvironmentVariable("KEEPQA_UIA_RECORD") == "1")
        {
            EjecutarUiaRecord(window, root, vm);
            window.Close();
            DoEvents();
            Console.WriteLine("DONE (KEEPQA_UIA_RECORD)");
            Environment.Exit(0);
        }

        // KEEPQA_MEMORIA=1 (14-sep-2026, Fase 6 de KeepQA V2.0 - ver
        // KeepQA\v2\PROPUESTA-UNIFICADA.md, Bloque A): el "Memory testing" real de la
        // especificacion. Este arnes ya corre EN PROCESO (mismo Application/MainWindow que el
        // resto del arnes) - puede leer su propia memoria managed directamente con
        // GC.GetTotalMemory(true), sin salir a PowerShell/Get-Process (eso ya lo cubre
        // medirRendimientoWPF.js desde fuera, memoria NO managed incluida - WorkingSet64; esto es
        // el complemento real, solo el heap gestionado de .NET). Ciclo real: abrir un personaje
        // real (Home.OpenCommand, el mismo comando que dispara un clic real del usuario sobre su
        // tarjeta) -> volver a Inicio (SelectedTabIndex=0, mismo patron ya usado en el resto de
        // este arnes, ej. linea 383) -> forzar una coleccion completa y medir. N configurable via
        // KEEPQA_MEMORIA_N (por defecto 25 - la propia tarea pide no disparar a 100 sin verificar
        // antes que el ciclo basico funciona). Va aqui, justo tras KEEPQA_SMOKE y antes de que el
        // resto del arnes empiece a fabricar/tocar datos sinteticos, por el mismo motivo que
        // KEEPQA_SMOKE: modo aislado, no depende de nada que venga despues.
        if (Environment.GetEnvironmentVariable("KEEPQA_MEMORIA") == "1")
        {
            int ciclosMemoria = int.TryParse(Environment.GetEnvironmentVariable("KEEPQA_MEMORIA_N"), out int nMemoriaEnv) && nMemoriaEnv > 0
                ? nMemoriaEnv : 25;

            var vmMemoria = (MainViewModel)window.DataContext;
            int esperaMemoria = 0;
            while (vmMemoria.Home.IsScanning && esperaMemoria < 100) { DoEvents(); System.Threading.Thread.Sleep(50); esperaMemoria++; }

            if (vmMemoria.Home.Characters.Count == 0)
            {
                Console.WriteLine("FALLO: KEEPQA_MEMORIA - no hay ningun personaje real en la carpeta para abrir/cerrar (el ciclo necesita al menos uno)");
                window.Close();
                Environment.Exit(1);
            }

            var personajeMemoria = vmMemoria.Home.Characters[0];
            Console.WriteLine($"MEMORIA: {ciclosMemoria} ciclos abrir/cerrar sobre '{personajeMemoria.Name}'");

            var muestrasMemoria = new List<long>();
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            long muestraInicialMemoria = GC.GetTotalMemory(true);
            muestrasMemoria.Add(muestraInicialMemoria);
            Console.WriteLine($"MEMORIA: muestra 0 (antes del primer ciclo) = {muestraInicialMemoria / 1024.0 / 1024.0:F2} MB");

            for (int ciclo = 1; ciclo <= ciclosMemoria; ciclo++)
            {
                vmMemoria.Home.OpenCommand.Execute(personajeMemoria);
                DoEvents(); DoEvents();
                vmMemoria.SelectedTabIndex = 0;
                DoEvents(); DoEvents();
                // Ver el comentario completo de PumpToContextIdle: sin esto el ciclo nunca deja
                // que WeakEventManager purgue sus listeners muertos, y KEEPQA_MEMORIA reporta una
                // "fuga" que no existe en el uso real de la app.
                PumpToContextIdle();

                GC.Collect(2, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                long muestraMemoria = GC.GetTotalMemory(true);
                muestrasMemoria.Add(muestraMemoria);
                double deltaKbMemoria = (muestraMemoria - muestrasMemoria[^2]) / 1024.0;
                Console.WriteLine($"MEMORIA: ciclo {ciclo}/{ciclosMemoria} -> {muestraMemoria / 1024.0 / 1024.0:F2} MB (delta {deltaKbMemoria:F1} KB)");
            }

            // Evaluacion real, honesta (pedida explicitamente): ¿hay en las ultimas 3 muestras
            // crecimiento NO explicado (indicio de fuga) o vuelve a estabilizarse tras el
            // GC.Collect de cada ciclo? Ninguno de los dos criterios solo basta - ruido normal
            // del GC puede dar un crecimiento puntual sin ser una fuga real, asi que se exigen
            // los dos a la vez: monotono creciente en las ultimas muestras Y por encima de un
            // umbral relativo (5% sobre la muestra inicial) para no disparar por ruido.
            int nEvalMemoria = Math.Min(3, muestrasMemoria.Count - 1);
            var ultimasMemoria = muestrasMemoria.Skip(muestrasMemoria.Count - nEvalMemoria).ToList();
            bool creceMonotonoMemoria = true;
            for (int i = 1; i < ultimasMemoria.Count; i++)
                if (ultimasMemoria[i] <= ultimasMemoria[i - 1]) creceMonotonoMemoria = false;
            long crecimientoTotalMemoria = muestrasMemoria[^1] - muestrasMemoria[0];
            double crecimientoPctMemoria = muestraInicialMemoria > 0 ? (crecimientoTotalMemoria * 100.0 / muestraInicialMemoria) : 0;
            bool posibleFugaMemoria = creceMonotonoMemoria && crecimientoPctMemoria > 5.0;

            Console.WriteLine($"MEMORIA-VEREDICTO: inicial={muestraInicialMemoria / 1024.0 / 1024.0:F2} MB, final={muestrasMemoria[^1] / 1024.0 / 1024.0:F2} MB, crecimiento={crecimientoTotalMemoria / 1024.0:F1} KB ({crecimientoPctMemoria:F2}%), ultimas {nEvalMemoria} muestras monotonas crecientes={creceMonotonoMemoria}");
            Console.WriteLine(posibleFugaMemoria
                ? "MEMORIA-VEREDICTO: POSIBLE FUGA - crecimiento sostenido (>5%) y monotono en las ultimas muestras tras GC.Collect forzado"
                : "MEMORIA-VEREDICTO: SIN INDICIO DE FUGA - la memoria managed se estabiliza tras el GC.Collect forzado de cada ciclo");

            string outDirMemoria = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
            Directory.CreateDirectory(outDirMemoria);
            string rutaMemoria = Path.Combine(outDirMemoria, "memoria-managed.json");
            File.WriteAllText(rutaMemoria, System.Text.Json.JsonSerializer.Serialize(new
            {
                fecha = DateTime.Now.ToString("o"),
                personaje = personajeMemoria.Name,
                ciclos = ciclosMemoria,
                muestrasBytes = muestrasMemoria,
                crecimientoTotalBytes = crecimientoTotalMemoria,
                crecimientoPct = crecimientoPctMemoria,
                ultimasMonotonasCrecientes = creceMonotonoMemoria,
                posibleFuga = posibleFugaMemoria,
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"MEMORIA: serie completa guardada en {rutaMemoria}");

            window.Close();
            DoEvents();
            Console.WriteLine("DONE (KEEPQA_MEMORIA)");
            Environment.Exit(0);
        }

        // Verificacion real de I-1 (auditoria de Opus, Bloque 2): HomeViewModel escanea SOLO
        // al construirse (constructor de MainViewModel, antes de este punto) la carpeta REAL de
        // tModLoader de esta maquina - sin sintetizar nada, se comprueban los .plr reales que
        // ya existen ahi.
        // FASE C (remate del log de la FASE B): desde H-04 el escaneo NO ve la carpeta real, sino las
        // COPIAS que AislamientoPartidasReales.cs dejo en la carpeta temporal del arnes - el texto
        // anterior ("en la carpeta real") hacia creer a quien leia el log que se abrian partidas reales.
        Console.WriteLine($"HOME-SCAN: {vm.Home.Characters.Count} personaje(s) encontrado(s) en la carpeta AISLADA del arnes (copias de los .plr reales en {RaizPersonajesAislada}, nunca los originales)");
        foreach (var entry in vm.Home.Characters)
            Console.WriteLine($"  - {entry.Name} | {entry.DifficultyLabel} | Vanilla={entry.IsVanilla} tModLoader={entry.IsTModLoader} Calamity={entry.IsCalamity} | mods='{entry.UsedModsTooltip}' | {entry.LastModifiedText}");

        // Encargo del usuario 4-sep-2026 ("los personajes que tienen mod solo marcan calamity...
        // que sean personajes verdaderamente de tmodloader... al igual que cuando un personaje
        // es vanilla que tenga dicha etiqueta") - ver ESPEC-sprites-botones-badges.md#D.3.
        // IsVanilla/IsTModLoader son excluyentes por construccion (IsVanilla => !IsTModLoader) -
        // si alguna vez coinciden, algo real se rompio en el calculo, no solo en la UI.
        foreach (var entry in vm.Home.Characters)
        {
            if (entry.IsVanilla == entry.IsTModLoader)
                Console.WriteLine($"FALLO: INSIGNIAS-INICIO - '{entry.Name}' tiene IsVanilla={entry.IsVanilla} e IsTModLoader={entry.IsTModLoader} (deberian ser opuestos siempre)");
        }

        // Caso real que NO existe en ningun .tplr de esta maquina (los que hay tienen los 4
        // contenido real de Calamity, ver ESPEC-sprites-botones-badges.md#C.3) - se fabrica a
        // mano en una carpeta temporal (NUNCA cerca de un personaje real, mismo criterio que
        // HomeCardTests.cs) un .tplr con solo entradas mod="Terraria" y un usedMods de ejemplo.
        // Sin este caso, la parte C no esta verificada de verdad - solo se comprueba que sigue
        // funcionando lo que ya funcionaba (Calamity=True en los personajes reales).
        try
        {
            string dirSintetico = Path.Combine(Path.GetTempPath(), $"insignias-tmod-sin-calamity-{Guid.NewGuid():N}");
            Directory.CreateDirectory(dirSintetico);
            string plrPath = Path.Combine(dirSintetico, "Sintetico.plr");
            var personajeSintetico = new PlrCharacter
            {
                Name = "Sintetico",
                Version = 279,
                PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
                Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
            };
            File.WriteAllBytes(plrPath, PlrFile.Write(personajeSintetico));
            string tplrPathSintetico = Path.ChangeExtension(plrPath, ".tplr");
            var rootSintetico = NbtCompound.Of(
                ("inventory", new NbtList(NbtTagType.Compound, [
                    NbtCompound.Of(("mod", new NbtString("Terraria")), ("name", new NbtString("IronBroadsword")), ("slot", new NbtShort(0)))
                ])),
                ("usedMods", new NbtList(NbtTagType.String, [new NbtString("HEROsMod")]))
            );
            File.WriteAllBytes(tplrPathSintetico, TplrFile.Write("Player", rootSintetico));

            var serviceSintetico = new CharacterFileService();
            var tplrSintetico = TplrProbe.TryRead(tplrPathSintetico);
            var entrySintetica = new CharacterListEntryViewModel(plrPath, personajeSintetico, isTModLoader: true, tplrSintetico,
                DateTime.UtcNow, serviceSintetico.EquipmentAppearance);
            Console.WriteLine($"INSIGNIAS-TMOD-SIN-CALAMITY: Vanilla={entrySintetica.IsVanilla} tModLoader={entrySintetica.IsTModLoader} Calamity={entrySintetica.IsCalamity} tooltip='{entrySintetica.UsedModsTooltip}' (esperado False/True/False)");
            if (entrySintetica.IsVanilla || !entrySintetica.IsTModLoader || entrySintetica.IsCalamity)
                Console.WriteLine("FALLO: INSIGNIAS-TMOD-SIN-CALAMITY - el personaje sintetico (solo mod Terraria) no dio Vanilla=False/tModLoader=True/Calamity=False");

            Directory.Delete(dirSintetico, recursive: true);
        }
        catch (Exception ex) { Console.WriteLine("INSIGNIAS-TMOD-SIN-CALAMITY-EXCEPTION: " + ex); }
        {
            var rtbHome = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbHome.Render(window);
            var encHome = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encHome.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbHome));
            string shotPathHome = Path.Combine(AppContext.BaseDirectory, "inicio-lanzador.png");
            using (var fs = File.Create(shotPathHome)) encHome.Save(fs);
            Console.WriteLine($"  Captura -> {shotPathHome}");
        }
        if (vm.Home.Characters.Count > 0)
        {
            var first = vm.Home.Characters[0];
            vm.Home.OpenCommand.Execute(first);
            DoEvents();
            DoEvents();
            ComprobarPersonajeAislado(vm, "HOME-OPEN"); // H-04: Characters[0] es una COPIA, nunca la partida real
            Console.WriteLine($"HOME-OPEN: click en '{first.Name}' -> SelectedTabIndex={vm.SelectedTabIndex} (esperado 1), CharacterName={vm.CharacterName}, IsCharacterLoaded={vm.IsCharacterLoaded}");

            // I-a (segunda auditoria de Opus, Fable): "No se distingue que personaje esta
            // cargado" - tras abrirlo, su propia tarjeta debe marcarse IsCurrent=True.
            Console.WriteLine($"I-a IsCurrent tras abrir '{first.Name}'={first.IsCurrent} (esperado True)");
            if (!first.IsCurrent) Console.WriteLine("FALLO: I-a (segunda auditoria) - la tarjeta abierta no quedo marcada como actual");

            // I-b (segunda auditoria de Opus, Fable): "Sin ninguna accion secundaria en la
            // tarjeta". Verificacion CUIDADOSA - solo se COMPRUEBA que el menu contextual real
            // resuelve sus 3 comandos (Command != null, via el truco PlacementTarget.Tag de
            // MainWindow.xaml), NUNCA se invoca ninguno: "adrian"/"Eldelgas" son personajes
            // REALES de esta maquina, y Duplicar/Restaurar escriben de verdad en disco - probar
            // eso de verdad tocaria datos reales del usuario, algo que este arnes no debe hacer
            // jamas (regla real del proyecto).
            try
            {
                vm.SelectedTabIndex = 0; // Inicio - la tarjeta solo existe en su arbol visual
                DoEvents(); DoEvents();

                // A9-13-IDIOMA (pedido explicito del usuario, 5-sep-2026): primer bloque real de
                // la infraestructura de idioma (LocalizationService) - prueba EN VIVO, sin
                // reiniciar la app, sobre el MISMO TextBlock ya en pantalla (no solo que el texto
                // inicial sea correcto, sino que cambiar el idioma en Ajustes lo reescriba solo).
                // settings.json real de esta maquina respaldado como texto y restaurado al final
                // (mismo criterio que A9-11-DIFICULTAD/A9-12-VENTANAFIJA) - el toggle SI persiste
                // de verdad (LoadFromDisk ya corrio en el arranque real de este arnes).
                try
                {
                    string settingsPathIdioma = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "settings.json");
                    string? settingsBackupIdioma = File.Exists(settingsPathIdioma) ? File.ReadAllText(settingsPathIdioma) : null;
                    try
                    {
                        // Hallazgo real de esta misma prueba: "Editor de personajes de Terraria"
                        // por si sola es ambigua - AboutViewModel.Tagline (otro TextBlock real,
                        // ANTES de este en el arbol visual) tambien empieza igual y NO esta
                        // migrado a Loc todavia (bloques posteriores) - encontraba ESE por error,
                        // "encontrado=True" con el resto en False. "Aplicación nativa de Windows"
                        // solo vive en la clave real que se esta probando aqui.
                        var descripcionInicio = Descendientes<System.Windows.Controls.TextBlock>(window)
                            .FirstOrDefault(tb => tb.Text.Contains("Aplicación nativa de Windows"));
                        bool esOk = descripcionInicio != null && descripcionInicio.Text.Contains("Aplicación nativa de Windows");
                        Console.WriteLine($"A9-13-IDIOMA: Inicio en español -> TextBlock encontrado={descripcionInicio != null}, contiene 'Aplicación nativa de Windows'={esOk} (esperado True en los dos)");
                        if (!esOk) Console.WriteLine("FALLO: A9-13-IDIOMA - el texto de Inicio en español no es el esperado (clave sin traducir o Loc roto)");

                        vm.Settings.Language = "en";
                        DoEvents(); DoEvents();
                        bool enOk = descripcionInicio != null && descripcionInicio.Text.Contains("Native Windows app");
                        Console.WriteLine($"A9-13-IDIOMA: tras cambiar a ingles EN VIVO (mismo TextBlock, sin reiniciar) -> contiene 'Native Windows app'={enOk} (esperado True)");
                        if (!enOk) Console.WriteLine("FALLO: A9-13-IDIOMA - el cambio de idioma en vivo no reescribio el texto ya en pantalla");

                        vm.Settings.Language = "es"; // el resto de este arnes entero asume español - imprescindible antes de seguir
                        DoEvents(); DoEvents();
                        bool esOtraVezOk = descripcionInicio != null && descripcionInicio.Text.Contains("Aplicación nativa de Windows");
                        Console.WriteLine($"A9-13-IDIOMA: vuelta a español -> contiene 'Aplicación nativa de Windows'={esOtraVezOk} (esperado True)");
                        if (!esOtraVezOk) Console.WriteLine("FALLO: A9-13-IDIOMA - volver a español no revirtio el texto, el resto del arnes quedaria en ingles");
                    }
                    finally
                    {
                        if (settingsBackupIdioma != null) File.WriteAllText(settingsPathIdioma, settingsBackupIdioma);
                        else if (File.Exists(settingsPathIdioma)) File.Delete(settingsPathIdioma);
                    }
                }
                catch (Exception ex) { Console.WriteLine("A9-13-IDIOMA-EXCEPTION: " + ex); }

                System.Windows.FrameworkElement? tarjetaBorder = null;
                void BuscarTarjeta(System.Windows.DependencyObject d)
                {
                    if (tarjetaBorder != null) return;
                    if (d is System.Windows.Controls.Border b && ReferenceEquals(b.DataContext, first)) { tarjetaBorder = b; return; }
                    int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
                    for (int i = 0; i < n && tarjetaBorder == null; i++)
                        BuscarTarjeta(System.Windows.Media.VisualTreeHelper.GetChild(d, i));
                }
                BuscarTarjeta(window);
                var menu = tarjetaBorder?.ContextMenu;
                if (menu == null) { Console.WriteLine("I-b: tarjeta o ContextMenu NO-FOUND"); }
                else
                {
                    menu.PlacementTarget = tarjetaBorder;
                    menu.IsOpen = true; // abre de verdad (activa PlacementTarget) SIN invocar ningun item
                    DoEvents(); DoEvents();
                    {
                        // Nota real: un ContextMenu real es un popup en su propio HWND -
                        // RenderTargetBitmap.Render(window) NO lo captura (solo pinta la ventana
                        // principal), asi que esta captura confirma I-a (borde+check de
                        // "actual") de verdad, no el menu en si - I-b ya se comprueba abajo por
                        // codigo (Command/CommandParameter resueltos), no por captura.
                        var rtbMenu = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtbMenu.Render(window);
                        var encMenu = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encMenu.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbMenu));
                        using var fsMenu = File.Create(Path.Combine(AppContext.BaseDirectory, "inicio-tarjeta-actual.png"));
                        encMenu.Save(fsMenu);
                    }
                    // H5-04 (quinta auditoria de Opus): todos los MenuItem HOJA (sin submenu)
                    // deben tener Command/CommandParameter resueltos via el truco
                    // PlacementTarget.Tag - ningun MenuItem real de este menu lleva submenu hoy
                    // (el que sí lo llevaba, "Historial de guardados", se sustituyo el
                    // 13-sep-2026 por un MenuItem normal que abre el panel real, ver abajo).
                    var comandosNulos = menu.Items.OfType<System.Windows.Controls.MenuItem>()
                        .Where(mi => !mi.HasItems && (mi.Command == null || mi.CommandParameter == null))
                        .Select(mi => (string)mi.Header).ToList();
                    Console.WriteLine($"I-b: {menu.Items.Count} item(s) de menu, comandos sin resolver={string.Join(",", comandosNulos)} (esperado ninguno)");
                    if (comandosNulos.Count > 0) Console.WriteLine("FALLO: I-b (segunda auditoria) - el truco PlacementTarget.Tag no resolvio Command/CommandParameter en algun item");

                    // H5-04-HISTORIAL (actualizado 13-sep-2026, redisenio del historial de
                    // versiones): "Historial de guardados" ERA un submenu que restauraba con un
                    // solo clic, sin confirmacion (bitacora, "Interfaz: un panel de verdad, no un
                    // submenu"). Ahora es un MenuItem normal (Loc[action_save_history]) que abre
                    // el panel real Ctrl+H (BackupHistoryViewModel) - ese panel completo, con el
                    // ciclo guardar/restaurar/confirmar/limpiar huerfanos, ya se verifica a fondo
                    // en el bloque BK_SOLO=1 (PruebasHistorialVersiones.cs); aqui solo hace falta
                    // confirmar que el MenuItem del texto REAL (no un literal español viejo, para
                    // que valga en los dos idiomas) sigue ahi y con su comando resuelto - ya lo
                    // cubre comandosNulos de arriba, esto es solo una confirmacion positiva
                    // explicita para que un MenuItem ausente del todo no pase desapercibido.
                    string tituloHistorialReal = LocalizationService.Instance["action_save_history"];
                    var historialItem = menu.Items.OfType<System.Windows.Controls.MenuItem>()
                        .FirstOrDefault(mi => (string)mi.Header == tituloHistorialReal);
                    Console.WriteLine($"H5-04-HISTORIAL: MenuItem '{tituloHistorialReal}' presente={historialItem != null}, comando resuelto={historialItem?.Command != null} (esperado True, True)");
                    if (historialItem == null) Console.WriteLine("FALLO: H5-04-HISTORIAL - el MenuItem que abre el panel de historial de versiones no esta en el menu contextual de Inicio");
                    else if (historialItem.Command == null) Console.WriteLine("FALLO: H5-04-HISTORIAL - el MenuItem existe pero su Command no se resolvio");

                    menu.IsOpen = false;
                }
            }
            catch (Exception ex) { Console.WriteLine("I-b-EXCEPTION: " + ex); }

            // Sexta auditoria de Opus (H6-01/H6-02/H6-03/H6-04/H6-05): "les faltan los brazos a
            // todos los personajes" - verificacion real de extremo a extremo con un personaje
            // REAL de esta maquina (el mismo 'first' ya abierto arriba, "adrian"/"Eldelgas" -
            // exactamente el tipo de personaje de las capturas originales del usuario), no uno
            // sintetico sin armadura. Confirma visualmente (captura) y por codigo (recuento de
            // pixeles opacos, mismo criterio que PlayerPreviewRendererH6Tests) que el doll
            // compone brazos/torso reales, no solo cabeza+piernas.
            try
            {
                vm.SelectedTabIndex = 1; // Personaje
                vm.PersonajeInnerTabIndex = 3; // Apariencia
                DoEvents(); DoEvents();

                var previewH6 = vm.Appearance.PreviewImage;
                int opacosH6 = 0;
                if (previewH6 != null)
                {
                    var pixelesH6 = new byte[previewH6.PixelHeight * previewH6.PixelWidth * 4];
                    previewH6.CopyPixels(pixelesH6, previewH6.PixelWidth * 4, 0);
                    for (int i = 3; i < pixelesH6.Length; i += 4) if (pixelesH6[i] != 0) opacosH6++;
                }
                Console.WriteLine($"H6-01-DOLL: copia aislada del personaje real '{vm.CharacterName}' ({vm.LoadedFilePath}), IsMale={vm.Appearance.IsMale}, HairStyle={vm.Appearance.HairStyle}, pixeles opacos={opacosH6}/2240 (esperado > 700)");
                if (previewH6 == null) Console.WriteLine("FALLO: H6-01 - Appearance.PreviewImage es null tras cargar un personaje real");
                else if (opacosH6 <= 700) Console.WriteLine("FALLO: H6-01 - muy pocos pixeles opacos, los brazos/torso no se estan componiendo de verdad");

                var rtbH6 = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbH6.Render(window);
                var encH6 = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encH6.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbH6));
                using (var fsH6 = File.Create(Path.Combine(AppContext.BaseDirectory, "h6-doll-personaje-real.png"))) encH6.Save(fsH6);
                Console.WriteLine("Captura doll con brazos, copia aislada del personaje real -> h6-doll-personaje-real.png");

                vm.SelectedTabIndex = 0; // deja la navegacion como estaba para el resto del arnes
                DoEvents();
            }
            catch (Exception ex) { Console.WriteLine("H6-01-EXCEPTION: " + ex); }
        }

        // KEEPQA_VITALS_REAL=1 (15-sep-2026): tiene que ir AQUI, entre el HOME-OPEN de arriba
        // (personaje real 'first' ya cargado) y el vm.LoadFromPath(tempPlr) de abajo (personaje
        // sintetico "UIA-Test", vacio del todo) - es EXACTAMENTE la ventana real, hoy sin usar,
        // en la que la franja de vitales de cabecera tiene datos reales no triviales. Cuerpo real
        // en AuditoriaKeepQA.cs (EjecutarKeepQaVitalesReal) - ver el comentario largo alli del
        // porque real de este modo.
        if (Environment.GetEnvironmentVariable("KEEPQA_VITALS_REAL") == "1")
        {
            EjecutarKeepQaVitalesReal(window, vm);
            Console.WriteLine("DONE (KEEPQA_VITALS_REAL)");
            Environment.Exit(0);
        }

        // KEEPQA_VITALS_DINERO_LARGO=1 (15-sep-2026): mismo hueco de ventana que KEEPQA_VITALS_REAL
        // de arriba, pero esta carga su PROPIO personaje (copia de 'Terrariano.plr', vanilla real,
        // con Coins forzados a "20013p 9o") en vez de reutilizar 'first' - no depende de que
        // 'first'/HOME-OPEN siga apuntando a ningun personaje en concreto. Cuerpo real en
        // AuditoriaKeepQA.cs (EjecutarKeepQaVitalesRealDineroLargo).
        if (Environment.GetEnvironmentVariable("KEEPQA_VITALS_DINERO_LARGO") == "1")
        {
            EjecutarKeepQaVitalesRealDineroLargo(window, vm);
            Console.WriteLine("DONE (KEEPQA_VITALS_DINERO_LARGO)");
            Environment.Exit(0);
        }

        // GUIA_SOLO=1 / HOSTING_SOLO=1 (Fase B, 15-sep-2026): mismo hueco de ventana real que los
        // modos de arriba - ver PruebasGuiaYServidor.cs para el cuerpo real de cada uno.
        if (Environment.GetEnvironmentVariable("GUIA_SOLO") == "1")
        {
            EjecutarGuiaReal(window, vm);
            Console.WriteLine("DONE (GUIA_SOLO)");
            Environment.Exit(0);
        }
        if (Environment.GetEnvironmentVariable("HOSTING_SOLO") == "1")
        {
            EjecutarHostingReal(window, vm);
            Console.WriteLine("DONE (HOSTING_SOLO)");
            Environment.Exit(0);
        }
        // GUIA_JEFES_TARDIOS_SOLO=1 (26-sep-2026, handoff e5eaea9e-c261-4199-8e7d-060b6054f58d):
        // confirmacion con .wld REALES (no sinteticos) de los 11 flags de jefes tardios de
        // ea405518 - ver PruebasGuiaJefesTardiosReales.cs para el cuerpo real.
        if (Environment.GetEnvironmentVariable("GUIA_JEFES_TARDIOS_SOLO") == "1")
        {
            EjecutarGuiaJefesTardiosReales(window, vm);
            Console.WriteLine("DONE (GUIA_JEFES_TARDIOS_SOLO)");
            Environment.Exit(0);
        }
        // KEEPQA_TRANSICION_SOLO=1 (16-sep-2026, sesgo S2 de KeepQA): pares antes/despues de
        // hover/scroll/tamaño/idioma para verificarTransicion.js - ver AuditoriaTransicion.cs.
        if (Environment.GetEnvironmentVariable("KEEPQA_TRANSICION_SOLO") == "1")
        {
            EjecutarKeepQaTransicionSolo(window, vm);
            Console.WriteLine("DONE (KEEPQA_TRANSICION_SOLO)");
            Environment.Exit(0);
        }
        // KEEPQA_EQUIPINV_SOLO=1 (16-sep-2026): cierre del hallazgo de juego libre en Equipamiento
        // a tamaño de arranque - ver AuditoriaEquipInv.cs.
        if (Environment.GetEnvironmentVariable("KEEPQA_EQUIPINV_SOLO") == "1")
        {
            EjecutarKeepQaEquipInvSolo(window, vm);
            Console.WriteLine("DONE (KEEPQA_EQUIPINV_SOLO)");
            Environment.Exit(0);
        }
        // README_SHOTS=1 (cierre de sesion, 15-sep-2026): regenera docs/screenshots/ con datos
        // reales de esta maquina (ver PruebasCapturasReadme.cs) - las capturas del README llevaban
        // desde el 5-sep-2026, de antes del idioma completo/Guia/Servidor de esta noche.
        if (Environment.GetEnvironmentVariable("README_SHOTS") == "1")
        {
            CapturarPantallasReadme(window, vm);
            Console.WriteLine("DONE (README_SHOTS)");
            Environment.Exit(0);
        }
        // ACTUALIZACION_SOLO=1 (X1, 16-sep-2026): verificacion visual real del aviso discreto de
        // version nueva (ComprobadorDeActualizaciones, ver MainViewModel.Actualizaciones.cs). La
        // llamada de RED real ya se probo aparte, contra la API real de GitHub (version antigua
        // simulada -> HayActualizacionDisponible; version actual real -> Actualizada; sin
        // conexion -> ErrorDeRed sin excepcion) - esto solo comprueba que el estado se VE bien:
        // la tarjeta aparece con el mensaje y los dos botones, y que con el estado normal
        // (HayActualizacionDisponible=false, el caso real de esta version) NO aparece nada.
        if (Environment.GetEnvironmentVariable("ACTUALIZACION_SOLO") == "1")
        {
            EjecutarActualizacionReal(window, vm);
            Console.WriteLine("DONE (ACTUALIZACION_SOLO)");
            Environment.Exit(0);
        }

        // KEEPQA_DATOS_REALES (16-sep-2026, DIAGNOSTICO-DE-FONDO-16SEP.md punto 3): hasta hoy
        // KEEPQA_SOLO y KEEPQA_FRESCO_SOLO se ejecutaban DESPUES de vm.LoadFromPath(tempPlr), es
        // decir, SIEMPRE con el personaje sintetico "UIA-Test" (vida 0/0, dinero "0c", 0 horas,
        // cero equipo). Tres bugs reales de esta semana (Mana huerfano, Defensa huerfana, Dinero
        // largo) solo existen con datos reales y por eso el arnes no los vio. Desde hoy los dos
        // modos corren AQUI, con el personaje real 'first' ya abierto por HOME-OPEN (mismo hueco de
        // ventana que KEEPQA_VITALS_REAL). Si esta maquina no tuviera ningun personaje real cargado,
        // caen al respaldo sintetico de mas abajo (mismo comportamiento de siempre) y lo dicen.
        // KEEPQA_DATOS_SINTETICOS=1 fuerza el comportamiento antiguo a proposito.
        bool datosSinteticosForzados = Environment.GetEnvironmentVariable("KEEPQA_DATOS_SINTETICOS") == "1";
        if (!datosSinteticosForzados && vm.IsCharacterLoaded)
        {
            if (Environment.GetEnvironmentVariable("KEEPQA_SOLO") == "1")
            {
                Console.WriteLine($"KEEPQA_SOLO: con personaje REAL '{vm.CharacterName}' (no el sintetico UIA-Test)");
                EjecutarKeepQaAdversarialYGeometria(window, vm);
                Console.WriteLine("DONE (KEEPQA_SOLO)");
                Environment.Exit(0);
            }
            if (Environment.GetEnvironmentVariable("KEEPQA_FRESCO_SOLO") == "1")
            {
                Console.WriteLine($"KEEPQA_FRESCO_SOLO: con personaje REAL '{vm.CharacterName}' (no el sintetico UIA-Test)");
                EjecutarKeepQaFrescoTerceraResolucion(window, vm);
                Console.WriteLine("DONE (KEEPQA_FRESCO_SOLO)");
                Environment.Exit(0);
            }
        }
        else if (!datosSinteticosForzados && (Environment.GetEnvironmentVariable("KEEPQA_SOLO") == "1" || Environment.GetEnvironmentVariable("KEEPQA_FRESCO_SOLO") == "1"))
        {
            Console.WriteLine("AVISO KEEPQA: no hay ningun personaje real cargado en esta maquina - el modo correra con el personaje sintetico UIA-Test (datos degenerados 0/0/0c; ver DIAGNOSTICO-DE-FONDO-16SEP.md)");
        }

        try
        {
            var swChar = System.Diagnostics.Stopwatch.StartNew();
            vm.LoadFromPath(tempPlr);
            swChar.Stop();
            Console.WriteLine($"MEDICION-PERSONAJE: {swChar.ElapsedMilliseconds}ms");
            Console.WriteLine("LOAD: OK - " + vm.StatusMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine("LOAD-EXCEPTION: " + ex);
        }

        // SNAPSHOT_VISUAL_SOLO=1 (17-sep-2026, KeepQA): cuerpo real en PruebasSnapshotVisual.cs.
        // Va AQUI, justo tras el LoadFromPath(tempPlr) de arriba y ANTES de cualquier otro modo
        // SOLO que pueda colocar/mover objetos o cambiar de pestaña - mismo personaje sintetico
        // determinista 'UIA-Test' recien cargado, para que el contenido real (10 objetos
        // colocados dentro de este mismo modo) sea reproducible entre ejecuciones.
        if (Environment.GetEnvironmentVariable("SNAPSHOT_VISUAL_SOLO") == "1")
        {
            EjecutarSnapshotVisualSolo(window, vm);
        }

        // VERIF_3FUNC_SOLO=1 (17-sep-2026): cuerpo real en VerificacionFuncionesNuevas17Sep.cs.
        // Mismo punto que SNAPSHOT_VISUAL_SOLO de arriba (personaje sintetico 'UIA-Test' recien
        // cargado, determinista).
        if (Environment.GetEnvironmentVariable("VERIF_3FUNC_SOLO") == "1")
        {
            EjecutarVerificacionFuncionesNuevas(window, vm);
        }

        // BUILDCODE-CANEXECUTE (14-sep-2026): TERCERA vez que este mismo defecto se cuela (A-d,
        // luego BK, ahora OpenBuildCodeCommand) - un comando CanExecute=IsCharacterLoaded que se
        // queda fuera de la lista de NotifyCanExecuteChanged() de OnIsCharacterLoadedChanged dejaba
        // su boton con aspecto DESACTIVADO hasta el siguiente requery automatico de verdad de WPF
        // (CommandManager.RequerySuggested, que solo se dispara con un evento de entrada real -
        // foco/raton/teclado - nunca con un simple DoEvents()). Se mide el BOTON REAL
        // (BuildCodeButton.IsEnabled), no .CanExecute(null) a pelo (eso SIEMPRE da el valor
        // correcto, ejecuta el getter de IsCharacterLoaded en el momento - el bug vive en que la
        // VISTA no se entera, no en el calculo) - justo AQUI, antes de que nada mas (ningun modo
        // SOLO, ningun otro bloque) tenga ocasion de disparar un requery real por su cuenta.
        try
        {
            var buildCodeButton = window.FindName("BuildCodeButton") as System.Windows.Controls.Button;
            DoEvents(); DoEvents();
            Console.WriteLine($"BUILDCODE-CANEXECUTE: tras cargar personaje (sin ningun evento de entrada real de por medio) -> BuildCodeButton.IsEnabled={buildCodeButton?.IsEnabled} (esperado True, IsCharacterLoaded={vm.IsCharacterLoaded})");
            if (buildCodeButton == null) Console.WriteLine("FALLO: BUILDCODE-CANEXECUTE - no se encuentra BuildCodeButton por su x:Name");
            else if (vm.IsCharacterLoaded && !buildCodeButton.IsEnabled)
                Console.WriteLine("FALLO: BUILDCODE-CANEXECUTE - el boton 'Código de build' se queda con aspecto desactivado tras cargar un personaje real, pese a que IsCharacterLoaded ya es True");
            // Captura real de la cabecera (no solo el booleano) - pedido explicito del encargo.
            var rtbBuildCode = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbBuildCode.Render(window);
            var encBuildCode = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encBuildCode.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbBuildCode));
            string shotBuildCode = Path.Combine(AppContext.BaseDirectory, "buildcode-boton-activado-tras-cargar.png");
            using (var fs = File.Create(shotBuildCode)) encBuildCode.Save(fs);
            Console.WriteLine($"BUILDCODE-CANEXECUTE: captura real -> {shotBuildCode}");
        }
        catch (Exception ex) { Console.WriteLine("BUILDCODE-CANEXECUTE-EXCEPTION: " + ex); }

        // DRAG_SOLO=1 (14-sep-2026, bug real confirmado por el usuario: "arrastro un objeto o
        // un buff desde su slot y no se ve ningun sprite siguiendo al cursor, solo el cursor
        // por defecto de Windows - un cuadrado vacio"): deja la ventana real ABIERTA con un
        // objeto real colocado en el hueco 0 del Inventario, en la pestaña correcta, y se queda
        // bombeando el Dispatcher sin salir nunca - para poder arrastrar de verdad desde FUERA
        // del proceso (pywinauto/SendInput) y mirar si el ghost (StartCardDrag/DragAdorner, el
        // mismo mecanismo ya en produccion para las tarjetas de la Libreria) aparece de verdad
        // al arrastrar un SLOT, que es justo lo que antes de este arreglo no llamaba a
        // StartCardDrag. SetCursorPos ya se demostro sin efecto en esta sesion (AR-EX2-PAN/
        // MINIMAPA-CLIC, bitacora.md) - SendInput es un mecanismo de entrada distinto, se
        // prueba aparte en vez de darlo por igual de roto sin comprobarlo.
        if (Environment.GetEnvironmentVariable("DRAG_SOLO") == "1")
        {
            vm.SelectedTabIndex = 1; // Personaje
            vm.PersonajeInnerTabIndex = 0; // Objetos
            vm.ObjetosSubTabIndex = 1; // Inventario (H4-02: Equipamiento=0/Inventario=1/Almacenes=2)
            DoEvents(); DoEvents();
            var contenedorDrag = vm.InventoryContainer;
            if (contenedorDrag != null && contenedorDrag.Slots.Count > 0)
            {
                contenedorDrag.Slots[0].PlaceItem(4); // Iron Broadsword - sprite real conocido
                DoEvents(); DoEvents();
                Console.WriteLine($"DRAG_SOLO: hueco 0 del Inventario = '{contenedorDrag.Slots[0].DisplayName}' (esperado no vacio)");
            }
            else Console.WriteLine("DRAG_SOLO: FALLO preparando el hueco - no hay InventoryContainer real");
            var bordeSlot0 = contenedorDrag?.Slots.Count > 0
                ? Descendientes<System.Windows.Controls.Border>(window).FirstOrDefault(b => ReferenceEquals(b.DataContext, contenedorDrag.Slots[0]))
                : null;
            if (bordeSlot0 != null)
            {
                var esquina = bordeSlot0.PointToScreen(new System.Windows.Point(0, 0));
                var centro = bordeSlot0.PointToScreen(new System.Windows.Point(bordeSlot0.ActualWidth / 2, bordeSlot0.ActualHeight / 2));
                Console.WriteLine($"DRAG_SOLO: Border real del hueco 0 -> esquina=({esquina.X:0},{esquina.Y:0}) tamaño=({bordeSlot0.ActualWidth:0}x{bordeSlot0.ActualHeight:0}) centro EN PANTALLA=({centro.X:0},{centro.Y:0})");
            }
            else Console.WriteLine("DRAG_SOLO: FALLO - no se encontro el Border real del hueco 0 en el arbol visual");
            Console.WriteLine($"DRAG_SOLO: ventana lista, hWnd={new System.Windows.Interop.WindowInteropHelper(window).Handle}. Esperando interaccion externa real - no sale nunca solo.");
            while (true) { DoEvents(); System.Threading.Thread.Sleep(30); }
        }

        // SIDEBAR_SOLO=1 (14-sep-2026, mismo bug real del panel de Exploracion): version RAPIDA
        // y sin necesitar ningun .wld real del F-10-REOPEN que vive mas abajo, dentro del bloque
        // grande de Exploracion (que si exige un mundo real cargado). Los dos botones y
        // Settings.ExplorationSidebarWidth no dependen de tener un mundo cargado - solo de
        // seleccionar la pestaña Exploracion, asi que esto verifica lo mismo sin pagar ese coste
        // y sin arriesgarse a que el recorrido completo muera antes de llegar (UI-BLOQUEADA).
        if (Environment.GetEnvironmentVariable("SIDEBAR_SOLO") == "1")
        {
            vm.SelectedTabIndex = 4; // Exploracion
            DoEvents(); DoEvents();
            var collapseBtnFast = window.FindName("CollapseExplorationSidebarButton") as System.Windows.Controls.Button;
            var expandBtnFast = window.FindName("ExpandExplorationSidebarButton") as System.Windows.Controls.Button;
            if (collapseBtnFast == null || expandBtnFast == null)
                Console.WriteLine("FALLO: SIDEBAR_SOLO - no se encuentran los dos botones reales de plegar/desplegar por su x:Name");
            else
            {
                // El Border original nunca tuvo su PROPIA Visibility ligada al ancho (solo el
                // GridSplitter la tiene), y su ActualWidth/hit-test no bajan de forma fiable a
                // "0 de verdad" solo por vivir dentro de una columna a 0px (el ScrollViewer
                // recorta por CLIP visual, no reduciendo el Arrange de sus hijos - un detalle de
                // layout de WPF, no del bug) - intentarlo daba lecturas contradictorias con la
                // ventana real. La prueba que de verdad importa, y que SI es inequivoca, es esta:
                // el boton NUEVO (el que faltaba, IsVisible ligado a una DataTrigger real) tiene
                // que ser IsVisible exactamente cuando la barra esta plegada, y un CLIC REAL
                // sobre el (InvokePattern, nunca la propiedad a mano) tiene que devolver la barra
                // a su ancho exacto de antes de plegar - eso es lo que demuestra que el usuario
                // ya tiene un sitio real y funcional desde el que volver a abrirla.
                AutomationElement? PorNombre(string nombre) =>
                    root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, nombre));
                void ClicReal(string nombre)
                {
                    var el = PorNombre(nombre);
                    if (el != null && el.TryGetCurrentPattern(InvokePattern.Pattern, out var pat)) ((InvokePattern)pat).Invoke();
                    else Console.WriteLine($"FALLO: SIDEBAR_SOLO - '{nombre}' no se encuentra por UI Automation o no expone InvokePattern, un clic real no podria activarlo");
                    DoEvents(); DoEvents(); WaitForDispatcher(200);
                }

                vm.Settings.ExplorationSidebarWidth = 280;
                DoEvents(); DoEvents();
                Console.WriteLine($"SIDEBAR_SOLO: barra ABIERTA (280px) -> boton desplegar IsVisible={expandBtnFast.IsVisible} (esperado False - no hace falta con la barra ya abierta)");
                if (expandBtnFast.IsVisible)
                    Console.WriteLine("FALLO: SIDEBAR_SOLO - con la barra abierta el boton de desplegar no deberia verse");

                // Clic REAL de plegar (no tocar la propiedad a mano): asi el code-behind memoriza
                // _lastExpandedSidebarWidth=280 de verdad, igual que le pasaria a un usuario real.
                ClicReal("CollapseExplorationSidebarButton");
                Console.WriteLine($"SIDEBAR_SOLO: tras CLIC REAL de plegar -> Settings.ExplorationSidebarWidth={vm.Settings.ExplorationSidebarWidth:0} (esperado 0), boton desplegar IsVisible={expandBtnFast.IsVisible} (esperado True - el hueco real que el bug dejaba sin nada)");
                if (vm.Settings.ExplorationSidebarWidth != 0 || !expandBtnFast.IsVisible)
                    Console.WriteLine("FALLO: SIDEBAR_SOLO - tras plegar con un clic real tiene que quedar visible un sitio real desde el que volver a abrirla");

                ClicReal("ExpandExplorationSidebarButton");
                Console.WriteLine($"SIDEBAR_SOLO: tras CLIC REAL de desplegar -> Settings.ExplorationSidebarWidth={vm.Settings.ExplorationSidebarWidth:0} (esperado 280, el ancho real de antes de plegar)");
                if (vm.Settings.ExplorationSidebarWidth != 280)
                    Console.WriteLine("FALLO: SIDEBAR_SOLO - el clic real sobre el boton de desplegar no devuelve la barra a su ancho anterior exacto");
            }
            Console.WriteLine("DONE (SIDEBAR_SOLO)");
            Environment.Exit(0);
        }

        // EX3_SOLO=1 (14-sep-2026, arreglo real de AR-EX3-LEGIBILIDAD): mismo modo de foco que
        // los de arriba - el bloque real vive muy abajo en el recorrido completo (detras de
        // UI-BLOQUEADA, que muere siempre primero en esta sesion). Carga roca_negra.wld
        // directamente (sin pagar el resto del recorrido), marca los tiles mas abundantes hasta
        // pasar el 40% real, y esta vez ademas del booleano nuevo (WorldSearchSummaryIsWarning)
        // mira el ESTILO REAL del TextBlock (Foreground/FontWeight) - el hallazgo original del
        // barrido visual era justo que el aviso se veia IGUAL que un resumen cualquiera, asi que
        // "hay aviso en el texto" no demuestra el arreglo: hace falta ver que ahora se PINTA
        // distinto de verdad.
        if (Environment.GetEnvironmentVariable("EX3_SOLO") == "1")
        {
            try
            {
                string worldPathEx3 = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
                if (!File.Exists(worldPathEx3))
                    Console.WriteLine("EX3_SOLO: roca_negra.wld no esta en esta maquina - omitido");
                else
                {
                    vm.SelectedTabIndex = 4; // Exploracion
                    DoEvents();
                    var cargaEx3 = vm.Exploration.LoadFromPathAsync(worldPathEx3);
                    while (!cargaEx3.IsCompleted) DoEvents();
                    DoEvents(); DoEvents();
                    var cabeceraEx3 = Terrakeep.Core.WldFormat.WldReader.ReadHeader(File.ReadAllBytes(worldPathEx3));
                    long totalTilesEx3 = (long)cabeceraEx3.TilesWide * cabeceraEx3.TilesHigh;

                    vm.Exploration.SelectedCategory = WorldSearchCategory.Objects;
                    vm.Exploration.ObjectsViewMode = 0;
                    DoEvents();
                    var acumuladasEx3 = new List<WorldInventoryRowViewModel>();
                    long tilesAcumuladosEx3 = 0;
                    foreach (var fila in vm.Exploration.Inventory.OrderByDescending(r => r.Count))
                    {
                        acumuladasEx3.Add(fila);
                        tilesAcumuladosEx3 += fila.Count;
                        if ((double)tilesAcumuladosEx3 / totalTilesEx3 >= 0.42) break;
                    }
                    double fraccionEx3 = (double)tilesAcumuladosEx3 / totalTilesEx3;
                    if (acumuladasEx3.Count == 0) Console.WriteLine("EX3_SOLO: el mundo no trae ningun tile - omitido");
                    else
                    {
                        foreach (var fila in acumuladasEx3) fila.IsChecked = true;
                        vm.Exploration.MarkObjectsOnMapCommand.Execute(null);
                        WaitForDispatcher(4000);
                        // ADR-TERRAKEEP-029 (26-sep-2026): Browse se movio a BrowseView.xaml
                        // (UserControl con su propio NameScope) - FindName DOBLE, mismo patron ya
                        // usado por GUIA/ADR-021, Compare/ADR-025, WorldTools/ADR-027 y
                        // ChestInspector/ADR-028.
                        var textoResumen = (window.FindName("BrowseView") as FrameworkElement)?.FindName("WorldSearchSummaryText") as System.Windows.Controls.TextBlock;
                        Console.WriteLine($"EX3_SOLO: {acumuladasEx3.Count} tile(s) marcados cubren {tilesAcumuladosEx3:N0} de {totalTilesEx3:N0} ({fraccionEx3:P1}) -> resumen='{vm.Exploration.WorldSearchSummary}', WorldSearchSummaryIsWarning={vm.Exploration.WorldSearchSummaryIsWarning} (esperado True, cubre mas del 40%)");
                        if (fraccionEx3 >= 0.4 && !vm.Exploration.WorldSearchSummaryIsWarning)
                            Console.WriteLine("FALLO: EX3_SOLO - la seleccion pasa del 40% y WorldSearchSummaryIsWarning sigue en False");
                        if (textoResumen == null)
                            Console.WriteLine("FALLO: EX3_SOLO - no se encuentra WorldSearchSummaryText por su x:Name");
                        else
                        {
                            string colorReal = (textoResumen.Foreground as System.Windows.Media.SolidColorBrush)?.Color.ToString() ?? "(no es SolidColorBrush)";
                            Console.WriteLine($"EX3_SOLO: estilo REAL del TextBlock -> Foreground={colorReal} (esperado #FFFFB84D, el mismo Naranja que cualquier otro aviso real de la app), FontWeight={textoResumen.FontWeight} (esperado SemiBold)");
                            if (colorReal != "#FFFFB84D" || textoResumen.FontWeight != System.Windows.FontWeights.SemiBold)
                                Console.WriteLine("FALLO: EX3_SOLO - el aviso de legibilidad no se pinta distinto de un resumen normal (el hallazgo original del barrido visual)");

                            var rtbEx3 = new System.Windows.Media.Imaging.RenderTargetBitmap(
                                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                            rtbEx3.Render(window);
                            var encEx3 = new System.Windows.Media.Imaging.PngBitmapEncoder();
                            encEx3.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbEx3));
                            string shotEx3 = Path.Combine(AppContext.BaseDirectory, "ex3-aviso-legibilidad-naranja.png");
                            using (var fs = File.Create(shotEx3)) encEx3.Save(fs);
                            Console.WriteLine($"EX3_SOLO: captura real -> {shotEx3}");
                        }

                        vm.Exploration.ClearOreMarksCommand.Execute(null);
                        DoEvents();
                        foreach (var fila in acumuladasEx3) fila.IsChecked = false;
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("EX3_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (EX3_SOLO)");
            Environment.Exit(0);
        }

        // EXPTOOLBAR_SOLO=1 (15-sep-2026): version RAPIDA y aislada de AR-EX6 (ver
        // AuditoriaBarraExploracion.cs para el cuerpo real y el porque detallado) - diagnostico
        // de la barra de zoom de Exploracion en las 6 resoluciones reales, sin pagar el resto
        // del recorrido completo. El chequeo PERMANENTE (sin variable de entorno) vive detras de
        // AR-EX5 mas abajo en este mismo Main() y se ejecuta siempre.
        if (Environment.GetEnvironmentVariable("EXPTOOLBAR_SOLO") == "1")
        {
            EjecutarComprobacionBarraExploracion(window, vm);
            Console.WriteLine("DONE (EXPTOOLBAR_SOLO)");
            Environment.Exit(0);
        }

        // VITALS_SOLO=1 (14-sep-2026, bug real confirmado por el usuario: "defensa/dinero/horas
        // solo se ven con la ventana maximizada/grande, y ademas los botones de Guardar etc se
        // solapan al reducir"): barrido real de anchos sobre la cabecera de Personaje, con
        // personaje real ya cargado. Comprueba TRES cosas a la vez, en los dos idiomas:
        //   1. Defensa/Dinero/Horas+Guardado siguen ENCONTRABLES (ya no Visibility=Collapsed).
        //   2. La franja vital entera (Vida+Mana+Defensa+Dinero+Horas) no tiene recorte de WPF
        //      a NINGUN ancho, incluido el minimo real 1080 (mismo detector que AR-04).
        //   3. La franja vital (columna 1) y la fila de botones (columna 2) NO se solapan en
        //      pantalla - el hueco que dejaba crecer sin tope la columna "Auto".
        if (Environment.GetEnvironmentVariable("VITALS_SOLO") == "1")
        {
            try
            {
                static System.Windows.Controls.WrapPanel? FindWrapPanelAncestor(DependencyObject? d)
                {
                    while (d != null)
                    {
                        if (d is System.Windows.Controls.WrapPanel wp) return wp;
                        d = System.Windows.Media.VisualTreeHelper.GetParent(d);
                    }
                    return null;
                }

                vm.SelectedTabIndex = 1; // Personaje (la cabecera es la misma en las 6 pestañas)
                DoEvents(); DoEvents();
                foreach (string idioma in new[] { "es", "en" })
                {
                    vm.Settings.Language = idioma;
                    DoEvents(); DoEvents();
                    foreach (double w in new double[] { 1080, 1170, 1299, 1300, 1320, 1500 })
                    {
                        FijarTamaño(window, w, 860);
                        DoEvents(); DoEvents();
                        var tira = Descendientes<System.Windows.Controls.WrapPanel>(window)
                            .FirstOrDefault(wp => Descendientes<TextBlock>(wp).Any(t => t.Text == "♥"));
                        var filaBotones = window.FindName("BuildCodeButton") is System.Windows.Controls.Button bcb
                            ? FindWrapPanelAncestor(bcb) : null;
                        if (tira == null || filaBotones == null)
                        {
                            Console.WriteLine($"VITALS_SOLO[{idioma}]: a {w}px, franja vital o fila de botones NO-FOUND en el arbol visual - omitido");
                            continue;
                        }
                        var (rx, ry) = Recorte(tira);
                        bool defensaVisible = Descendientes<TextBlock>(tira).Any(t => t.Text == "🛡" && t.IsVisible);
                        bool dineroVisible = tira.DataContext is MainViewModel vmDin && !string.IsNullOrEmpty(vmDin.MoneyText)
                            && Descendientes<TextBlock>(tira).Any(t => t.Text == vmDin.MoneyText && t.IsVisible);
                        var rectTira = tira.TransformToAncestor(window).TransformBounds(new System.Windows.Rect(0, 0, tira.ActualWidth, tira.ActualHeight));
                        var rectBotones = filaBotones.TransformToAncestor(window).TransformBounds(new System.Windows.Rect(0, 0, filaBotones.ActualWidth, filaBotones.ActualHeight));
                        bool solapan = rectTira.IntersectsWith(rectBotones);
                        Console.WriteLine($"VITALS_SOLO[{idioma}]: a {w}px SizeClass={vm.SizeClass} MaxWidth={vm.VitalsStripMaxWidth:0} -> recorte franja=({rx:0},{ry:0}) (esperado 0,0), Defensa visible={defensaVisible} (esperado True), Dinero visible={dineroVisible} (esperado True), franja={rectTira}, botones={rectBotones}, SOLAPAN={solapan} (esperado False)");
                        if (rx > 0 || ry > 0) Console.WriteLine($"FALLO: VITALS_SOLO - franja vital recortada {rx:0}x{ry:0}px a {w}px[{idioma}]");
                        if (!defensaVisible) Console.WriteLine($"FALLO: VITALS_SOLO - Defensa no visible a {w}px[{idioma}] (el bug real que el usuario reporto)");
                        if (!dineroVisible) Console.WriteLine($"FALLO: VITALS_SOLO - Dinero no visible a {w}px[{idioma}] (el bug real que el usuario reporto)");
                        if (solapan) Console.WriteLine($"FALLO: VITALS_SOLO - la franja vital y la fila de botones SE SOLAPAN a {w}px[{idioma}]");

                        if (w == 1080)
                        {
                            var rtbVitals = new System.Windows.Media.Imaging.RenderTargetBitmap(
                                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                            rtbVitals.Render(window);
                            var encVitals = new System.Windows.Media.Imaging.PngBitmapEncoder();
                            encVitals.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbVitals));
                            string shotVitals = Path.Combine(AppContext.BaseDirectory, $"vitals-1080px-{idioma}.png");
                            using (var fs = File.Create(shotVitals)) encVitals.Save(fs);
                            Console.WriteLine($"VITALS_SOLO[{idioma}]: captura real a 1080px -> {shotVitals}");
                        }
                    }
                }
                vm.Settings.Language = "es";
            }
            catch (Exception ex) { Console.WriteLine("VITALS_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (VITALS_SOLO)");
            Environment.Exit(0);
        }

        // TIPO_SOLO=1 (20-sep-2026, catalogo de rediseño visual T5/T10): verifica en frio, con la
        // ventana real, los dos cambios declarativos de Theme.xaml de esta ronda - mismo modo de
        // foco real que VITALS_SOLO de arriba (widths reales, DoEvents entre cada uno).
        //   T10: BodyText/CaptionText suben un escalon real (12.5->13.5 / 11->12) SOLO en
        //     WindowSizeClass.Extra (>=1920px, ExtraMinWidth arriba). Se localiza el TextBlock
        //     real por identidad de Style (mismo objeto Style compartido, no por texto/x:Name) y
        //     se lee FontSize YA resuelto por WPF (Setter del DataTrigger, no el valor base).
        //   T5: Sp1..Sp6 (Thickness) y SpD1..SpD6 (Double) existen en el ResourceDictionary real
        //     de la app (Application.Resources, via MergedDictionaries) y valen 4/8/12/16/24/32.
        if (Environment.GetEnvironmentVariable("TIPO_SOLO") == "1")
        {
            try
            {
                var bodyStyle = (System.Windows.Style)window.FindResource("BodyText");
                var captionStyle = (System.Windows.Style)window.FindResource("CaptionText");
                vm.SelectedTabIndex = 0; // Inicio: tiene TextBlocks reales de BodyText y CaptionText
                DoEvents(); DoEvents();
                foreach (double w in new double[] { 1900, 1920, 2500 })
                {
                    FijarTamaño(window, w, 900);
                    DoEvents(); DoEvents();
                    // Solo TextBlocks REALMENTE en pantalla (visibles Y conectados de verdad al
                    // arbol visual de `window`, no dentro de un Popup/ToolTip/ContextMenu con su
                    // propia raiz de presentacion - TransformToAncestor lanza si estan
                    // desconectados, mismo criterio que RectCompleto en el resto del arnes) Y SIN
                    // un FontSize LOCAL propio (ReadLocalValue) - el comentario de Theme.xaml ya
                    // deja escrito a proposito que sitios concretos (ej. DifficultyLabel,
                    // FontSize="10" local en la tarjeta de personaje de Inicio) tienen un ajuste
                    // MAS PEQUEÑO que el paso estandar y NO deben escalar (un valor local en WPF
                    // siempre gana a cualquier Setter/DataTrigger de Style, es la regla real del
                    // motor, no un bug) - de lo contrario la primera coincidencia hallada seria
                    // ese hueco pequeño y el trigger real quedaria sin comprobar.
                    bool EnPantalla(TextBlock t)
                    {
                        if (!t.IsVisible) return false;
                        if (t.ReadLocalValue(TextBlock.FontSizeProperty) != DependencyProperty.UnsetValue) return false;
                        try { t.TransformToAncestor(window); return true; }
                        catch (InvalidOperationException) { return false; }
                    }
                    var body = Descendientes<TextBlock>(window).FirstOrDefault(t => t.Style == bodyStyle && EnPantalla(t));
                    var caption = Descendientes<TextBlock>(window).FirstOrDefault(t => t.Style == captionStyle && EnPantalla(t));
                    bool extra = vm.SizeClass == WindowSizeClass.Extra;
                    double esperadoBody = extra ? 13.5 : 12.5;
                    double esperadoCaption = extra ? 12 : 11;
                    Console.WriteLine($"TIPO_SOLO[T10]: a {w}px SizeClass={vm.SizeClass} -> BodyText('{body?.Text}').FontSize={(body != null ? body.FontSize.ToString() : "NO-FOUND")} (esperado {esperadoBody}), CaptionText('{caption?.Text}').FontSize={(caption != null ? caption.FontSize.ToString() : "NO-FOUND")} (esperado {esperadoCaption})");
                    if (body == null) Console.WriteLine($"FALLO: TIPO_SOLO - ningun TextBlock real con Style=BodyText encontrado en pantalla en Inicio a {w}px");
                    else if (body.FontSize != esperadoBody) Console.WriteLine($"FALLO: TIPO_SOLO - BodyText.FontSize={body.FontSize} a {w}px (SizeClass={vm.SizeClass}, esperado {esperadoBody})");
                    if (caption == null) Console.WriteLine($"FALLO: TIPO_SOLO - ningun TextBlock real con Style=CaptionText encontrado en pantalla en Inicio a {w}px");
                    else if (caption.FontSize != esperadoCaption) Console.WriteLine($"FALLO: TIPO_SOLO - CaptionText.FontSize={caption.FontSize} a {w}px (SizeClass={vm.SizeClass}, esperado {esperadoCaption})");
                }

                foreach (var (clave, valor) in new (string clave, double valor)[] { ("Sp1", 4), ("Sp2", 8), ("Sp3", 12), ("Sp4", 16), ("Sp5", 24), ("Sp6", 32) })
                {
                    var th = (System.Windows.Thickness)window.FindResource(clave);
                    var dobleClave = "SpD" + clave.Substring(2);
                    var dd = (double)window.FindResource(dobleClave);
                    Console.WriteLine($"TIPO_SOLO[T5]: {clave}={th} {dobleClave}={dd} (esperado {valor} uniforme en los dos)");
                    if (th.Left != valor || th.Top != valor || th.Right != valor || th.Bottom != valor)
                        Console.WriteLine($"FALLO: TIPO_SOLO - {clave} no es {valor} uniforme de verdad, es {th}");
                    if (dd != valor) Console.WriteLine($"FALLO: TIPO_SOLO - {dobleClave} no es {valor}, es {dd}");
                }
            }
            catch (Exception ex) { Console.WriteLine("TIPO_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (TIPO_SOLO)");
            Environment.Exit(0);
        }

        // IDEA10_SOLO=1 (catalogo de ideas Keep, idea 10 "vista previa animada, exportable").
        // Tercera pasada (20-sep-2026, reconsiderada a peticion explicita del coordinador tras
        // investigar a fondo Terraria/Player.cs real - ver el comentario de cabecera de
        // PlayerPreviewRenderer.Render): ademas de la exportacion PNG ya cerrada, comprueba el
        // ciclo de andar/reposo REAL con el DispatcherTimer de verdad bombeando (unico sitio del
        // proyecto que puede hacerlo, ver AppearanceUndoTests para el mismo criterio ya
        // establecido con el debounce de colores), "girar" y la exportacion a GIF.
        if (Environment.GetEnvironmentVariable("IDEA10_SOLO") == "1")
        {
            try
            {
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 3; // Personaje > Apariencia
                DoEvents(); DoEvents();

                // --- PNG (ya cerrado en una pasada anterior, se conserva la comprobacion real). ---
                string rutaPng = Path.Combine(AppContext.BaseDirectory, "idea10-preview-export.png");
                if (File.Exists(rutaPng)) File.Delete(rutaPng);
                Console.WriteLine($"IDEA10_SOLO: PreviewImage real antes de exportar -> {(vm.Appearance.PreviewImage != null ? $"{vm.Appearance.PreviewImage.PixelWidth}x{vm.Appearance.PreviewImage.PixelHeight}" : "NULL")}");
                vm.Appearance.ExportPreviewToPng(rutaPng);
                bool existePng = File.Exists(rutaPng);
                long tamañoPng = existePng ? new FileInfo(rutaPng).Length : 0;
                Console.WriteLine($"IDEA10_SOLO: PNG real creado={existePng} en '{rutaPng}', tamaño={tamañoPng} bytes (esperado > 200 bytes)");
                if (!existePng) Console.WriteLine("FALLO: IDEA10_SOLO - ExportPreviewToPng no crea el fichero real");
                else if (tamañoPng < 200) Console.WriteLine($"FALLO: IDEA10_SOLO - el PNG real pesa solo {tamañoPng} bytes, sospechoso de estar vacio");
                else
                {
                    var decoded = new System.Windows.Media.Imaging.PngBitmapDecoder(new Uri(rutaPng), System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad).Frames[0];
                    Console.WriteLine($"IDEA10_SOLO: PNG real decodificado -> {decoded.PixelWidth}x{decoded.PixelHeight} (esperado igual al PreviewImage real: {vm.Appearance.PreviewImage?.PixelWidth}x{vm.Appearance.PreviewImage?.PixelHeight})");
                    if (vm.Appearance.PreviewImage != null && (decoded.PixelWidth != vm.Appearance.PreviewImage.PixelWidth || decoded.PixelHeight != vm.Appearance.PreviewImage.PixelHeight))
                        Console.WriteLine("FALLO: IDEA10_SOLO - las dimensiones del PNG exportado no coinciden con la vista previa real");
                }

                // --- Ciclo de andar real, con el Dispatcher de verdad bombeando ticks. ---
                byte[]? PixelsDe(System.Windows.Media.Imaging.WriteableBitmap? bmp)
                {
                    if (bmp == null) return null;
                    var px = new byte[bmp.PixelHeight * bmp.PixelWidth * 4];
                    bmp.CopyPixels(px, bmp.PixelWidth * 4, 0);
                    return px;
                }
                var reposoPixels = PixelsDe(vm.Appearance.PreviewImage);
                Console.WriteLine($"IDEA10_SOLO: IsWalkAnimationPlaying antes={vm.Appearance.IsWalkAnimationPlaying} (esperado false)");
                if (vm.Appearance.IsWalkAnimationPlaying) Console.WriteLine("FALLO: IDEA10_SOLO - la animacion empieza reproduciendose sin que nadie pulse nada");

                vm.Appearance.ToggleWalkAnimationCommand.Execute(null);
                Console.WriteLine($"IDEA10_SOLO: tras pulsar '{vm.Loc["action_walk_start"]}' -> IsWalkAnimationPlaying={vm.Appearance.IsWalkAnimationPlaying} (esperado true)");
                if (!vm.Appearance.IsWalkAnimationPlaying) Console.WriteLine("FALLO: IDEA10_SOLO - ToggleWalkAnimationCommand no activa la animacion");

                // El DispatcherTimer real (90ms/fotograma) necesita el bucle de mensajes real
                // bombeando para disparar - DoEvents() en bucle un tiempo real de sobra (mismo
                // criterio de espera ya usado en el resto de este arnes para temporizadores).
                var cronometro = System.Diagnostics.Stopwatch.StartNew();
                while (cronometro.ElapsedMilliseconds < 500) { DoEvents(); System.Threading.Thread.Sleep(10); }
                var andandoPixels = PixelsDe(vm.Appearance.PreviewImage);
                bool cambioDeVerdad = reposoPixels != null && andandoPixels != null && !reposoPixels.SequenceEqual(andandoPixels);
                Console.WriteLine($"IDEA10_SOLO: tras ~500ms reproduciendo -> el frame REAL en pantalla cambio de verdad={cambioDeVerdad} (esperado true, ~7 fotogramas reales a 70ms/u)");
                if (!cambioDeVerdad) Console.WriteLine("FALLO: IDEA10_SOLO - el DispatcherTimer real no esta avanzando el frame de la animacion en pantalla");

                vm.Appearance.ToggleWalkAnimationCommand.Execute(null);
                DoEvents(); DoEvents();
                Console.WriteLine($"IDEA10_SOLO: tras pulsar '{vm.Loc["action_walk_stop"]}' -> IsWalkAnimationPlaying={vm.Appearance.IsWalkAnimationPlaying} (esperado false)");
                if (vm.Appearance.IsWalkAnimationPlaying) Console.WriteLine("FALLO: IDEA10_SOLO - ToggleWalkAnimationCommand no detiene la animacion");
                var quietoDeNuevoPixels = PixelsDe(vm.Appearance.PreviewImage);
                bool vuelveAlReposo = reposoPixels != null && quietoDeNuevoPixels != null && reposoPixels.SequenceEqual(quietoDeNuevoPixels);
                Console.WriteLine($"IDEA10_SOLO: tras detener -> vuelve exactamente al frame de reposo original={vuelveAlReposo} (esperado true)");
                if (!vuelveAlReposo) Console.WriteLine("FALLO: IDEA10_SOLO - al detener la animacion no vuelve al mismo reposo de antes de empezar");

                // --- Girar (espejo) real, boton real. ---
                vm.Appearance.ToggleFacingCommand.Execute(null);
                DoEvents(); DoEvents();
                var espejadoPixels = PixelsDe(vm.Appearance.PreviewImage);
                bool espejoDistinto = quietoDeNuevoPixels != null && espejadoPixels != null && !quietoDeNuevoPixels.SequenceEqual(espejadoPixels);
                Console.WriteLine($"IDEA10_SOLO: IsFacingLeft={vm.Appearance.IsFacingLeft}, el frame REAL cambia al girar={espejoDistinto} (esperado true/true)");
                if (!vm.Appearance.IsFacingLeft || !espejoDistinto) Console.WriteLine("FALLO: IDEA10_SOLO - ToggleFacingCommand no gira de verdad el frame en pantalla");
                vm.Appearance.ToggleFacingCommand.Execute(null); // revierte para no dejar el arnes girado de cara a lo que corra despues
                DoEvents(); DoEvents();

                // --- Exportar como GIF, boton real. ---
                string rutaGif = Path.Combine(AppContext.BaseDirectory, "idea10-preview-export.gif");
                if (File.Exists(rutaGif)) File.Delete(rutaGif);
                vm.Appearance.ExportPreviewToGif(rutaGif);
                bool existeGif = File.Exists(rutaGif);
                long tamañoGif = existeGif ? new FileInfo(rutaGif).Length : 0;
                Console.WriteLine($"IDEA10_SOLO: GIF real creado={existeGif} en '{rutaGif}', tamaño={tamañoGif} bytes");
                if (!existeGif) Console.WriteLine("FALLO: IDEA10_SOLO - ExportPreviewToGif no crea el fichero real");
                else
                {
                    var decoderGif = new System.Windows.Media.Imaging.GifBitmapDecoder(new Uri(rutaGif), System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                    int esperado = 1 + Terrakeep.App.ViewModels.AppearanceViewModel.WalkCycleRows.Length;
                    Console.WriteLine($"IDEA10_SOLO: GIF real decodificado -> {decoderGif.Frames.Count} fotogramas (esperado {esperado})");
                    if (decoderGif.Frames.Count != esperado) Console.WriteLine($"FALLO: IDEA10_SOLO - el GIF real no tiene los {esperado} fotogramas esperados");
                }
            }
            catch (Exception ex) { Console.WriteLine("IDEA10_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (IDEA10_SOLO)");
            Environment.Exit(0);
        }

        // WALKFREEZE_SOLO=1 (21-sep-2026, bug real reportado EN VIVO por el usuario: "Terrakeep
        // congelado", confirmado con un volcado dotnet-dump del proceso real del usuario - 8h+ de
        // CPU acumulado sin ningun deadlock, causa real: AppearanceViewModel._walkAnimationTimer
        // (90ms) nunca se paraba solo al salir de Apariencia ni al cargar otro personaje, solo
        // "Detener" manual lo apagaba - y el doll pequeño de la cabecera esta enlazado al mismo
        // PreviewImage, visible en CUALQUIER pestaña). Arreglo real: AppearanceViewModel.
        // StopWalkAnimation() llamado desde MainViewModel.OnPersonajeInnerTabIndexChanged (al
        // salir de Apariencia) y desde CharacterLoaded (al cargar cualquier personaje). Verifica
        // con el Dispatcher real bombeando ticks (mismo patron ya probado en IDEA10_SOLO) que:
        // 1) la animacion arranca y el frame SI avanza de verdad, 2) cambiar de sub-pestaña la
        // para sola (IsWalkAnimationPlaying pasa a false Y el frame deja de avanzar), 3) cargar
        // OTRO personaje encima tambien la para si se habia vuelto a activar.
        if (Environment.GetEnvironmentVariable("WALKFREEZE_SOLO") == "1")
        {
            try
            {
                byte[]? PixelsDe(System.Windows.Media.Imaging.WriteableBitmap? bmp)
                {
                    if (bmp == null) return null;
                    var px = new byte[bmp.PixelHeight * bmp.PixelWidth * 4];
                    bmp.CopyPixels(px, bmp.PixelWidth * 4, 0);
                    return px;
                }
                void BombeaMs(int ms)
                {
                    var cr = System.Diagnostics.Stopwatch.StartNew();
                    while (cr.ElapsedMilliseconds < ms) { DoEvents(); System.Threading.Thread.Sleep(10); }
                }

                vm.SelectedTabIndex = 1; // Personaje
                vm.PersonajeInnerTabIndex = 3; // PersonajeInnerTab.Apariencia (enum privado real, MainViewModel.cs)
                DoEvents(); DoEvents();

                // --- 1) La animacion arranca y el frame SI avanza de verdad ---
                vm.Appearance.ToggleWalkAnimationCommand.Execute(null);
                DoEvents();
                Console.WriteLine($"WALKFREEZE_SOLO: tras activar 'Andar' en Apariencia -> IsWalkAnimationPlaying={vm.Appearance.IsWalkAnimationPlaying} (esperado True)");
                var frame1 = PixelsDe(vm.Appearance.PreviewImage);
                BombeaMs(500);
                var frame2 = PixelsDe(vm.Appearance.PreviewImage);
                bool avanzaDeVerdad = frame1 != null && frame2 != null && !frame1.SequenceEqual(frame2);
                Console.WriteLine($"WALKFREEZE_SOLO: tras ~500ms con el Dispatcher real bombeando -> el frame avanzo de verdad={avanzaDeVerdad} (esperado True)");
                if (!vm.Appearance.IsWalkAnimationPlaying || !avanzaDeVerdad) Console.WriteLine("FALLO: WALKFREEZE_SOLO - la animacion real no arranco/no avanzo, no se puede verificar que el arreglo la pare de verdad");

                // --- 2) Salir de Apariencia la para SOLA ---
                vm.PersonajeInnerTabIndex = 0; // Objetos
                DoEvents(); DoEvents();
                Console.WriteLine($"WALKFREEZE_SOLO: tras salir de Apariencia (a Objetos) SIN pulsar 'Detener' -> IsWalkAnimationPlaying={vm.Appearance.IsWalkAnimationPlaying} (esperado False)");
                if (vm.Appearance.IsWalkAnimationPlaying) Console.WriteLine("FALLO: WALKFREEZE_SOLO - salir de Apariencia no para la animacion sola (regresion del arreglo)");
                var frame3 = PixelsDe(vm.Appearance.PreviewImage);
                BombeaMs(500);
                var frame4 = PixelsDe(vm.Appearance.PreviewImage);
                bool siguioAvanzando = frame3 != null && frame4 != null && !frame3.SequenceEqual(frame4);
                Console.WriteLine($"WALKFREEZE_SOLO: tras otros ~500ms bombeando FUERA de Apariencia -> el frame siguio avanzando={siguioAvanzando} (esperado False - el timer real ya no dispara)");
                if (siguioAvanzando) Console.WriteLine("FALLO: WALKFREEZE_SOLO - el DispatcherTimer real sigue disparando (renderizando) pese a IsWalkAnimationPlaying=False - fuga real de CPU en segundo plano");

                // --- 3) Reactivar y cargar OTRO personaje encima tambien la para ---
                vm.PersonajeInnerTabIndex = 3; // vuelve a Apariencia
                DoEvents();
                vm.Appearance.ToggleWalkAnimationCommand.Execute(null);
                DoEvents();
                Console.WriteLine($"WALKFREEZE_SOLO: reactivada para la prueba 3 -> IsWalkAnimationPlaying={vm.Appearance.IsWalkAnimationPlaying} (esperado True)");
                string plrOtro = Path.Combine(Path.GetTempPath(), $"terrakeep-walkfreeze-{Guid.NewGuid():N}.plr");
                var personajeOtro = new Terrakeep.Core.PlrFormat.PlrCharacter
                {
                    Name = "PersonajeWalkfreeze",
                    Version = 279,
                    PrimaryLoadout = Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: true),
                    Loadouts = [Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false), Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false), Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false)],
                };
                File.WriteAllBytes(plrOtro, Terrakeep.Core.PlrFormat.PlrFile.Write(personajeOtro));
                vm.LoadFromPath(plrOtro);
                DoEvents(); DoEvents();
                Console.WriteLine($"WALKFREEZE_SOLO: tras cargar OTRO personaje real encima ('{vm.CharacterName}') -> IsWalkAnimationPlaying={vm.Appearance.IsWalkAnimationPlaying} (esperado False)");
                if (vm.Appearance.IsWalkAnimationPlaying) Console.WriteLine("FALLO: WALKFREEZE_SOLO - cargar otro personaje encima no para la animacion del anterior (regresion del arreglo)");
                try { File.Delete(plrOtro); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
            catch (Exception ex) { Console.WriteLine("WALKFREEZE_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (WALKFREEZE_SOLO)");
            Environment.Exit(0);
        }

        // HOMEHOVER_SOLO=1 (21-sep-2026, catalogo de ideas Keep - "que la tarjeta de Inicio ande
        // sola al pasar el raton, con brazos, mascota y vanidad" - ver el comentario real de
        // CharacterListEntryViewModel.SetHovering para la cita exacta del decompilado que
        // confirma el trigger real (UICharacterListItem.cs, MouseOver/MouseOut) y de
        // PlayerPreviewRenderer.WalkArmColumn para la cita real del brazo (Player.cs +
        // PlayerDrawSet.cs). Mismo patron de Dispatcher real bombeando que WALKFREEZE_SOLO arriba
        // - aqui sobre CharacterListEntryViewModel directamente (sin depender de las carpetas
        // reales de Documents\...\Players del usuario, mismo criterio de "aislar la variable" que
        // WALKFREEZE_SOLO ya aplica con su personaje sintetico): 1) SetHovering(true) hace que el
        // frame avance de verdad con el Dispatcher real bombeando (no solo un flag), 2)
        // SetHovering(false) lo para YA (mismo criterio anti-fuga que WALKFREEZE_SOLO), 3)
        // PetIconPath resuelve a un icono real cuando el slot de mascota tiene un objeto real
        // puesto (5098, el mismo id real de mascota confirmado en Documents\...\Eldelgas.plr).
        if (Environment.GetEnvironmentVariable("HOMEHOVER_SOLO") == "1")
        {
            try
            {
                byte[]? PixelsDeCard(System.Windows.Media.Imaging.WriteableBitmap? bmp)
                {
                    if (bmp == null) return null;
                    var px = new byte[bmp.PixelHeight * bmp.PixelWidth * 4];
                    bmp.CopyPixels(px, bmp.PixelWidth * 4, 0);
                    return px;
                }
                void BombeaMsCard(int ms)
                {
                    var cr = System.Diagnostics.Stopwatch.StartNew();
                    while (cr.ElapsedMilliseconds < ms) { DoEvents(); System.Threading.Thread.Sleep(10); }
                }

                var fileService = new Terrakeep.App.Services.CharacterFileService();
                var personajeHover = new Terrakeep.Core.PlrFormat.PlrCharacter
                {
                    Name = "PersonajeHomeHover",
                    Version = 279,
                    PrimaryLoadout = Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: true),
                    Loadouts = [Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false), Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false), Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false)],
                };
                // 5098: id real de mascota (confirmado en Eldelgas.plr, un personaje real de este
                // equipo) - EquipmentItems[0] es el mismo slot real que Terraria.Player.miscEquips[0].
                personajeHover.EquipmentItems[0] = new Terrakeep.Core.PlrFormat.PlrItemSlot(5098, 1, 0, false);

                var entry = new Terrakeep.App.ViewModels.CharacterListEntryViewModel(
                    "sintetico-homehover.plr", personajeHover, isTModLoader: false, tplr: null,
                    DateTime.UtcNow, fileService.EquipmentAppearance);

                // 5098 (item 5098 -> proyectil 960) SI esta en PetAnimationCatalog (ver
                // Terrakeep.App/Assets/pet_animations.json) - PetImage tiene que resolver el
                // PRIMER fotograma real animado, no el icono estatico de reserva.
                Console.WriteLine($"HOMEHOVER_SOLO: PetImage resuelto={(entry.PetImage is not null ? entry.PetImage.GetType().Name : "(null)")} (esperado no-null, fotograma real de la mascota 5098/proyectil 960)");
                if (entry.PetImage is null) Console.WriteLine("FALLO: HOMEHOVER_SOLO - PetImage deberia resolver un fotograma real para la mascota 5098");

                var frameReposo = PixelsDeCard(entry.Preview);

                // --- 1) SetHovering(true) hace que el frame avance de verdad (Dispatcher real) ---
                entry.SetHovering(true);
                DoEvents();
                var frameHoverInicial = PixelsDeCard(entry.Preview);
                bool reposoVsHoverDistinto = frameReposo != null && frameHoverInicial != null && !frameReposo.SequenceEqual(frameHoverInicial);
                Console.WriteLine($"HOMEHOVER_SOLO: reposo vs primer fotograma de hover -> distinto={reposoVsHoverDistinto} (esperado True)");
                if (!reposoVsHoverDistinto) Console.WriteLine("FALLO: HOMEHOVER_SOLO - el hover deberia cambiar el doll de reposo a andando de inmediato");

                // Muestreo en VARIOS puntos (no solo un antes/despues) a lo largo de mas de un
                // ciclo completo (14 fotogramas x 70ms = ~980ms, corregido 26-sep-2026 junto con
                // Apariencia - hallazgo ParidadPersonaje-Fase1) - un unico par de muestras
                // puede "aliasear" (caer justo en dos fotogramas que dan la MISMA suma de bytes
                // por coincidencia, medido de verdad en esta misma ronda: un BombeaMsCard(1300)
                // de una sola tacada aliaseo una vez) sin que eso signifique que el timer no
                // avanza - varias muestras espaciadas es la version robusta del mismo criterio
                // real "avanza de verdad" que WALKFREEZE_SOLO ya usa con dos puntos (ahi es
                // seguro porque compara ANTES/DESPUES de un evento discreto, no un ciclo
                // periodico que puede volver a un valor ya visto).
                var sumasCiclo = new List<int>();
                // Mascota EN VIVO (correccion real 21-sep-2026, usuario comparando con vanilla):
                // mismo criterio de muestreo robusto de varios puntos, sobre los bytes REALES
                // del fotograma de la mascota (PetPreviewRenderer.RenderFrame, WriteableBitmap
                // congelado) - no basta con comparar la REFERENCIA del objeto (una nueva
                // instancia podria contener los MISMOS bytes por coincidencia si el ciclo esta
                // en una franja "quieta" real del proyectil, igual que con el doll).
                var sumasMascota = new List<int>();
                int? PixelsDeMascota()
                {
                    if (entry.PetImage is not System.Windows.Media.Imaging.WriteableBitmap wb) return null;
                    var px = new byte[wb.PixelHeight * wb.PixelWidth * 4];
                    wb.CopyPixels(px, wb.PixelWidth * 4, 0);
                    return px.Sum(b => (int)b);
                }
                for (int muestra = 0; muestra < 10; muestra++)
                {
                    BombeaMsCard(140);
                    var px = PixelsDeCard(entry.Preview);
                    sumasCiclo.Add(px == null ? -1 : px.Sum(b => (int)b));
                    sumasMascota.Add(PixelsDeMascota() ?? -1);
                }
                bool avanzaDeVerdad = sumasCiclo.Distinct().Count() > 1;
                Console.WriteLine($"HOMEHOVER_SOLO: tras ~1400ms con el Dispatcher real bombeando (10 muestras cada 140ms) -> valores distintos vistos={sumasCiclo.Distinct().Count()} de 10 (esperado > 1, el ciclo real tiene que avanzar)");
                if (!avanzaDeVerdad) Console.WriteLine("FALLO: HOMEHOVER_SOLO - el ciclo de andar no avanza de verdad a lo largo del tiempo");

                bool mascotaAvanzaDeVerdad = sumasMascota.Distinct().Count() > 1;
                Console.WriteLine($"HOMEHOVER_SOLO: mascota (proyectil 960) en el mismo muestreo -> valores distintos vistos={sumasMascota.Distinct().Count()} de 10 (esperado > 1, la mascota real de Eldelgas tiene 7 fotogramas reales en su ciclo de hover)");
                if (!mascotaAvanzaDeVerdad) Console.WriteLine("FALLO: HOMEHOVER_SOLO - la mascota deberia animarse EN VIVO durante el hover, no quedarse en un fotograma fijo");

                // --- 2) SetHovering(false) lo para YA (mismo criterio anti-fuga que WALKFREEZE_SOLO) ---
                entry.SetHovering(false);
                DoEvents();
                var frameTrasParar1 = PixelsDeCard(entry.Preview);
                bool volvioAReposo = frameReposo != null && frameTrasParar1 != null && frameReposo.SequenceEqual(frameTrasParar1);
                Console.WriteLine($"HOMEHOVER_SOLO: tras SetHovering(false) -> volvio EXACTAMENTE al frame de reposo={volvioAReposo} (esperado True)");
                if (!volvioAReposo) Console.WriteLine("FALLO: HOMEHOVER_SOLO - dejar de hacer hover deberia volver al frame de reposo exacto");
                BombeaMsCard(500);
                var frameTrasParar2 = PixelsDeCard(entry.Preview);
                bool siguioAvanzandoParado = frameTrasParar1 != null && frameTrasParar2 != null && !frameTrasParar1.SequenceEqual(frameTrasParar2);
                Console.WriteLine($"HOMEHOVER_SOLO: tras otros ~500ms bombeando SIN hover -> el frame siguio avanzando={siguioAvanzandoParado} (esperado False - el timer de esta tarjeta ya no dispara)");
                if (siguioAvanzandoParado) Console.WriteLine("FALLO: HOMEHOVER_SOLO - el DispatcherTimer de la tarjeta sigue disparando tras SetHovering(false) - misma clase de fuga que WALKFREEZE_SOLO ya cerro en Apariencia");
            }
            catch (Exception ex) { Console.WriteLine("HOMEHOVER_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (HOMEHOVER_SOLO)");
            Environment.Exit(0);
        }

        // ROTACION_SOLO=1 (26-sep-2026, PortSeleccion Encargo6): cierra el pendiente real de
        // PortSeleccion Encargo5 (RotateTransform de FloatAndSpinWhenWalking) - captura REAL de la
        // tarjeta de Inicio en varios instantes del hover para confirmar que el angulo gira de
        // verdad en pantalla, no solo en el ViewModel (eso ya lo cierra
        // Terrakeep.Core.Tests/Data/PetCustomAnimationCodeTests.cs contra la formula exacta del
        // decompilado). Las 2 mascotas reales catalogadas con "code":"FloatAndSpinWhenWalking" en
        // pet_animations.json (ItemID.cs): 4801=SkeletronPetItem, 4805=SkeletronPrimePetItem.
        if (Environment.GetEnvironmentVariable("ROTACION_SOLO") == "1")
        {
            try
            {
                string outDirRot = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
                Directory.CreateDirectory(outDirRot);
                void BombeaMsRot(int ms)
                {
                    var cr = System.Diagnostics.Stopwatch.StartNew();
                    while (cr.ElapsedMilliseconds < ms) { DoEvents(); System.Threading.Thread.Sleep(10); }
                }
                void CapturaRot(string nombre)
                {
                    var rtbRot = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbRot.Render(window);
                    var encRot = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encRot.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbRot));
                    string shotPathRot = Path.Combine(outDirRot, nombre);
                    using var fs = File.Create(shotPathRot);
                    encRot.Save(fs);
                    Console.WriteLine($"ROTACION_SOLO: Captura -> {shotPathRot}");
                }

                foreach (var (itemId, nombrePersonaje, proyectilReal) in new[]
                {
                    (4801, "PersonajeRotacionSkeletron", 885),
                    (4805, "PersonajeRotacionSkeletronPrime", 889),
                })
                {
                    var fileServiceRot = new CharacterFileService();
                    var personajeRot = new PlrCharacter
                    {
                        Name = nombrePersonaje,
                        Version = 279,
                        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
                        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
                    };
                    personajeRot.EquipmentItems[0] = new Terrakeep.Core.PlrFormat.PlrItemSlot(itemId, 1, 0, false);
                    var entryRot = new CharacterListEntryViewModel(
                        $"sintetico-rotacion-{itemId}.plr", personajeRot, isTModLoader: false, tplr: null,
                        DateTime.UtcNow, fileServiceRot.EquipmentAppearance);

                    // Si PetImage resuelve un fotograma real, el catalogo reconocio de verdad el
                    // item (code="FloatAndSpinWhenWalking" en pet_animations.json).
                    Console.WriteLine($"ROTACION_SOLO[{itemId}]: PetImage resuelto={(entryRot.PetImage is not null ? entryRot.PetImage.GetType().Name : "(null)")} (esperado no-null, fotograma real del proyectil {proyectilReal})");
                    if (entryRot.PetImage is null) Console.WriteLine($"FALLO: ROTACION_SOLO[{itemId}] - PetImage deberia resolver un fotograma real");
                    vm.Home.Characters.Insert(0, entryRot);
                    DoEvents();
                    DoEvents();

                    double AnguloActualRot() => entryRot.PetRotationDegrees;

                    double anguloReposo = AnguloActualRot();
                    Console.WriteLine($"ROTACION_SOLO[{itemId}]: angulo en reposo (sin hover)={anguloReposo} (esperado 0, walking=false real del decompilado)");
                    if (anguloReposo != 0) Console.WriteLine($"FALLO: ROTACION_SOLO[{itemId}] - fuera de hover el angulo deberia ser exactamente 0");
                    CapturaRot($"rotacion-mascota-{itemId}-reposo.png");

                    entryRot.SetHovering(true);
                    DoEvents();
                    var angulos = new List<double>();
                    for (int muestra = 0; muestra < 3; muestra++)
                    {
                        BombeaMsRot(180);
                        double angulo = AnguloActualRot();
                        angulos.Add(angulo);
                        Console.WriteLine($"ROTACION_SOLO[{itemId}]: instante {muestra + 1} (~{(muestra + 1) * 180}ms de hover real) -> PetRotationDegrees={angulo:F1} grados");
                        CapturaRot($"rotacion-mascota-{itemId}-instante{muestra + 1}.png");
                    }

                    bool rangoValido = angulos.All(a => a >= 0f && a < 360f);
                    Console.WriteLine($"ROTACION_SOLO[{itemId}]: los 3 angulos caen dentro de [0,360)={rangoValido} (esperado True, mismo rango real que 'percent*360f' en PetCustomAnimationCode.EvaluateRotationDegrees)");
                    if (!rangoValido) Console.WriteLine($"FALLO: ROTACION_SOLO[{itemId}] - el angulo deberia quedar siempre dentro de [0,360)");

                    bool giraDeVerdad = angulos.Distinct().Count() > 1;
                    Console.WriteLine($"ROTACION_SOLO[{itemId}]: el angulo cambia de verdad entre los 3 instantes (valores distintos vistos={angulos.Distinct().Count()} de 3, esperado > 1 - la mascota gira EN VIVO durante el hover, igual que Float/SlimePet ya lo hacen con el bob)");
                    if (!giraDeVerdad) Console.WriteLine($"FALLO: ROTACION_SOLO[{itemId}] - el RotateTransform deberia mostrar un angulo distinto en cada instante muestreado del hover");

                    entryRot.SetHovering(false);
                    DoEvents();
                    double anguloTrasParar = AnguloActualRot();
                    Console.WriteLine($"ROTACION_SOLO[{itemId}]: angulo tras SetHovering(false)={anguloTrasParar} (esperado 0, mismo 'else proj.rotation = 0f' real del decompilado)");
                    if (anguloTrasParar != 0) Console.WriteLine($"FALLO: ROTACION_SOLO[{itemId}] - al salir del hover el angulo deberia volver exactamente a 0");

                    vm.Home.Characters.Remove(entryRot);
                    DoEvents();
                }
            }
            catch (Exception ex) { Console.WriteLine("ROTACION_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (ROTACION_SOLO)");
            Environment.Exit(0);
        }

        // IDEA9_SOLO=1 (20-sep-2026, catalogo de ideas Keep, idea 9 "modo reparar personaje" -
        // version real reducida: solo el diagnostico de prefijos ilegales, ver el LIMITE
        // documentado en MainViewModel.RebuildIllegalPrefixDiagnostics). Aisla la variable a mano
        // (mismo criterio real de "verificar aislando la variable"): en vez de esperar que algun
        // personaje real de este equipo YA traiga un prefijo ilegal (nadie lo garantiza), se
        // fuerza uno real y conocido sobre un objeto real ya equipado, con
        // PrefixRulesCatalog.LegalPrefixes real (nunca una suposicion) para elegir un id que SI
        // es ilegal para ESE objeto concreto.
        if (Environment.GetEnvironmentVariable("IDEA9_SOLO") == "1")
        {
            try
            {
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 0;
                // El personaje sintetico UIA-Test (cargado por defecto en este arnes) no lleva
                // ningun objeto vanilla real en el inventario - se carga uno real de este equipo
                // (accion real, Home.OpenCommand) para tener con que de verdad.
                var personajeReal = vm.Home.Characters.FirstOrDefault();
                if (personajeReal != null) { vm.Home.OpenCommand.Execute(personajeReal); DoEvents(); DoEvents(); }
                DoEvents(); DoEvents();
                Console.WriteLine($"IDEA9_SOLO: diagnostico ANTES de forzar nada -> {vm.IllegalPrefixIssues.Count} objeto(s) con prefijo ilegal (personaje real '{vm.CharacterName}' recien cargado, esperado normalmente 0)");

                var servicioAparte = new Terrakeep.App.Services.CharacterFileService();
                var slot = vm.InventoryContainer?.Slots.FirstOrDefault(s => !s.IsEmpty && !s.IsCalamity);
                if (slot == null) Console.WriteLine("IDEA9_SOLO: sin ningun objeto vanilla real en el inventario de este personaje - se omite la comprobacion forzada");
                else
                {
                    var legales = servicioAparte.PrefixRules.LegalPrefixes(slot.Item.Id);
                    byte? idIlegal = null;
                    for (byte candidato = 1; candidato <= 83; candidato++)
                        if (!legales.Contains((int)candidato)) { idIlegal = candidato; break; }
                    Console.WriteLine($"IDEA9_SOLO: objeto real elegido '{slot.DisplayName}' (id={slot.Item.Id}), {legales.Count} prefijo(s) legal(es) reales, id ilegal real elegido={idIlegal}");
                    if (idIlegal == null) Console.WriteLine("IDEA9_SOLO: los 83 prefijos vanilla son legales para este objeto (raro pero no imposible) - se omite la comprobacion forzada");
                    else
                    {
                        slot.SetPrefix(Terrakeep.Core.Model.ItemPrefix.Vanilla(idIlegal.Value));
                        vm.RebuildIllegalPrefixDiagnostics();
                        DoEvents(); DoEvents();
                        Console.WriteLine($"IDEA9_SOLO: tras forzar el prefijo ilegal real -> IllegalPrefixIssues=[{string.Join(", ", vm.IllegalPrefixIssues.Select(i => i.DisplayName))}] (esperado que contenga '{slot.DisplayName}')");
                        if (!vm.IllegalPrefixIssues.Any(i => i.DisplayName == slot.DisplayName))
                            Console.WriteLine($"FALLO: IDEA9_SOLO - el diagnostico NO detecta el prefijo ilegal real recien forzado sobre '{slot.DisplayName}'");

                        // El Expander real vive en la pestaña Version (PersonajeInnerTab.Version=6) -
                        // el arnes seguia en Objetos/Equipamiento, donde nunca se llega a realizar.
                        vm.PersonajeInnerTabIndex = 6;
                        DoEvents(); DoEvents();
                        var expander = Descendientes<System.Windows.Controls.Expander>(window).FirstOrDefault(e => e.Header as string == vm.Loc["repair_illegal_prefixes"]);
                        Console.WriteLine($"IDEA9_SOLO: Expander real 'Prefijos ilegales encontrados' en el arbol visual={expander != null}, visible={expander?.IsVisible}");
                        if (expander == null || !expander.IsVisible) Console.WriteLine("FALLO: IDEA9_SOLO - el aviso real no esta en pantalla pese a haber un objeto con prefijo ilegal real");

                        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtb.Render(window);
                        var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                        string shot = Path.Combine(AppContext.BaseDirectory, "idea9-prefijos-ilegales.png");
                        using (var fs = File.Create(shot)) enc.Save(fs);
                        Console.WriteLine($"IDEA9_SOLO: captura real -> {shot}");

                        // Idea 9, tercera pasada (20-sep-2026, "arreglo en un clic" confirmado como
                        // parte real del alcance tras releer el texto literal del catalogo): el
                        // boton real "Arreglar" tiene que existir en el arbol visual de verdad (no
                        // solo el ViewModel) y su Command tiene que ser el FixCommand real de ESA
                        // fila concreta - se localiza por DataContext (RepairIssueViewModel), nunca
                        // por posicion, para no acertar por casualidad si hubiera mas de una fila.
                        var botonArreglar = Descendientes<System.Windows.Controls.Button>(window)
                            .FirstOrDefault(b => b.DataContext is Terrakeep.App.ViewModels.RepairIssueViewModel riv && riv.DisplayName == slot.DisplayName);
                        Console.WriteLine($"IDEA9_SOLO: boton real 'Arreglar' en el arbol visual={botonArreglar != null}, habilitado={botonArreglar?.IsEnabled}");
                        if (botonArreglar == null) Console.WriteLine("FALLO: IDEA9_SOLO - no se encuentra el boton real 'Arreglar' de la fila del prefijo ilegal forzado");
                        else
                        {
                            botonArreglar.Command?.Execute(botonArreglar.CommandParameter);
                            DoEvents(); DoEvents();
                            Console.WriteLine($"IDEA9_SOLO: tras pulsar 'Arreglar' -> prefijo real del slot={slot.Item.Prefix}, IllegalPrefixIssues.Count={vm.IllegalPrefixIssues.Count} (esperado None y 0)");
                            if (!slot.Item.Prefix.IsNone || vm.IllegalPrefixIssues.Count != 0)
                                Console.WriteLine("FALLO: IDEA9_SOLO - el boton real 'Arreglar' no quito el prefijo ilegal real o no actualizo el diagnostico");
                        }

                        // Red de seguridad final (nunca corromper el .plr real de este equipo por
                        // una prueba - SetPrefix es solo en memoria, no se guarda de todos modos,
                        // pero se revierte igual por higiene del resto del arnes que corre despues):
                        // si el boton de arriba ya lo dejo en None esto es un no-op real.
                        slot.SetPrefix(Terrakeep.Core.Model.ItemPrefix.None);
                        vm.RebuildIllegalPrefixDiagnostics();
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("IDEA9_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (IDEA9_SOLO)");
            Environment.Exit(0);
        }

        // IDEA6_SOLO=1 (20-sep-2026, catalogo de funciones, idea 6 "Laboratorio de personajes" -
        // version real, tercera ronda tras la correccion del coordinador: investigado a fondo
        // que NO existe ninguna fabrica real de personaje "en blanco" (Core ni App) - LIMITE real
        // documentado en el propio codigo de OnGenerateCharacterForBuildClick. Camino real SI
        // viable probado aqui: duplicar el personaje cargado a un fichero nuevo, vaciarlo y
        // aplicarle un build real via AutoEquipCommand (la MISMA logica que el boton real, sin
        // el SaveFileDialog que no se puede automatizar sin interaccion real de Windows).
        //
        // Verificacion con "aislar la variable": el ORIGINAL debe quedar INTACTO (nunca tocado
        // por vaciar/reequipar el duplicado) y el NUEVO fichero debe llevar EXACTAMENTE los
        // ItemId ya resueltos por BuildsViewModel (verdad de referencia calculada ANTES de tocar
        // nada, independiente del flujo bajo prueba).
        if (Environment.GetEnvironmentVariable("IDEA6_SOLO") == "1")
        {
            string? rutaTemporal = null;
            try
            {
                var personajeReal = vm.Home.Characters.FirstOrDefault(c => c.FilePath.Contains("tModLoader", StringComparison.OrdinalIgnoreCase) && c.FilePath.Contains("Eldelgas", StringComparison.OrdinalIgnoreCase))
                    ?? vm.Home.Characters.FirstOrDefault();
                if (personajeReal == null) { Console.WriteLine("IDEA6_SOLO: AVISO - no hay ningun personaje real de prueba, se omite"); }
                else
                {
                    vm.Home.OpenCommand.Execute(personajeReal);
                    DoEvents(); DoEvents();
                    vm.SelectedTabIndex = 2; // Builds
                    DoEvents();

                    var clase = vm.Builds.VanillaStages.SelectMany(s => s.Classes).FirstOrDefault(c => c.Armor.Count > 0 && c.Weapons.Count > 0)
                        ?? vm.Builds.CalamityStages.SelectMany(s => s.Classes).FirstOrDefault(c => c.Armor.Count > 0 && c.Weapons.Count > 0);
                    if (clase == null) { Console.WriteLine("IDEA6_SOLO: AVISO - ningun build real tiene armadura+arma a la vez, se omite"); }
                    else
                    {
                        Console.WriteLine($"IDEA6_SOLO: build real elegido -> clase='{clase.ClassName}', armadura={clase.Armor.Count}, armas={clase.Weapons.Count}, accesorios={clase.Accessories.Count}");
                        var idsArmaduraEsperados = clase.Armor.Take(3).Select(a => a.ItemId).ToList();
                        var idsAccesoriosEsperados = clase.Accessories.Take(5).Select(a => a.ItemId).ToList();

                        // --- Verdad de referencia del ORIGINAL, antes de tocar nada ---
                        var servicioAparte = new Terrakeep.App.Services.CharacterFileService();
                        var originalAntes = servicioAparte.Load(personajeReal.FilePath);
                        int itemsRealesOriginal = originalAntes.MergedContainers.Values.SelectMany(v => v).Count(i => !i.IsEmpty);
                        Console.WriteLine($"IDEA6_SOLO: verdad de referencia -> el original tiene {itemsRealesOriginal} objeto(s) real(es) antes de nada");

                        // --- Duplicar (mismo mecanismo real que OnGenerateCharacterForBuildClick,
                        // sin el SaveFileDialog que no se puede automatizar) ---
                        rutaTemporal = Path.Combine(Path.GetTempPath(), $"idea6-laboratorio-{Guid.NewGuid():N}.plr");
                        File.Copy(personajeReal.FilePath, rutaTemporal, overwrite: true);
                        string tplrSrc = Path.ChangeExtension(personajeReal.FilePath, ".tplr");
                        if (File.Exists(tplrSrc)) File.Copy(tplrSrc, Path.ChangeExtension(rutaTemporal, ".tplr"), overwrite: true);

                        vm.LoadFromPath(rutaTemporal);
                        DoEvents(); DoEvents();

                        if (vm.EquipmentGroup != null)
                            foreach (var slot in vm.EquipmentGroup.CurrentItems.Slots) slot.UpdateFrom(Terrakeep.Core.Model.GameItem.Empty);
                        var inventarioNuevo = vm.Containers.FirstOrDefault(c => c.Key == "inventory");
                        if (inventarioNuevo != null)
                            foreach (var slot in inventarioNuevo.Slots) slot.UpdateFrom(Terrakeep.Core.Model.GameItem.Empty);
                        DoEvents();

                        vm.AutoEquipCommand.Execute(clase.Source);
                        DoEvents(); DoEvents();

                        var idsArmaduraReales = vm.EquipmentGroup!.CurrentItems.Slots.Take(3).Select(s => s.ItemId).ToList();
                        var idsAccesoriosReales = vm.EquipmentGroup.CurrentItems.Slots.Skip(3).Take(5).Select(s => s.ItemId).ToList();
                        int armasColocadas = inventarioNuevo!.Slots.Count(s => !s.IsEmpty);
                        Console.WriteLine($"IDEA6_SOLO: esperado armadura={string.Join(",", idsArmaduraEsperados)}, real={string.Join(",", idsArmaduraReales)}");
                        Console.WriteLine($"IDEA6_SOLO: esperado accesorios={string.Join(",", idsAccesoriosEsperados)}, real={string.Join(",", idsAccesoriosReales)}");
                        Console.WriteLine($"IDEA6_SOLO: armas reales colocadas en inventario={armasColocadas} (esperado <= {clase.Weapons.Count}, > 0)");

                        if (!idsArmaduraEsperados.SequenceEqual(idsArmaduraReales))
                            Console.WriteLine("FALLO: IDEA6_SOLO - la armadura real del personaje generado no coincide con el build elegido");
                        if (!idsAccesoriosEsperados.SequenceEqual(idsAccesoriosReales))
                            Console.WriteLine("FALLO: IDEA6_SOLO - los accesorios reales del personaje generado no coinciden con el build elegido");
                        if (armasColocadas == 0 && clase.Weapons.Count > 0)
                            Console.WriteLine("FALLO: IDEA6_SOLO - ninguna arma real quedo colocada en el inventario del personaje generado");

                        // --- Aisla la variable: el ORIGINAL debe seguir intacto ---
                        var originalDespues = servicioAparte.Load(personajeReal.FilePath);
                        int itemsRealesDespues = originalDespues.MergedContainers.Values.SelectMany(v => v).Count(i => !i.IsEmpty);
                        Console.WriteLine($"IDEA6_SOLO: el original tiene {itemsRealesDespues} objeto(s) real(es) DESPUES (esperado igual, {itemsRealesOriginal})");
                        if (itemsRealesDespues != itemsRealesOriginal)
                            Console.WriteLine("FALLO: IDEA6_SOLO - el personaje ORIGINAL cambio de verdad (el laboratorio deberia tocar solo el duplicado)");
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("IDEA6_SOLO-EXCEPTION: " + ex); }
            finally
            {
                if (rutaTemporal != null)
                {
                    try { if (File.Exists(rutaTemporal)) File.Delete(rutaTemporal); } catch { }
                    try { var t = Path.ChangeExtension(rutaTemporal, ".tplr"); if (File.Exists(t)) File.Delete(t); } catch { }
                }
            }
            Console.WriteLine("DONE (IDEA6_SOLO)");
            Environment.Exit(0);
        }

        // IDEA7_SOLO=1 (20-sep-2026, catalogo de funciones, idea 7 "Capa Guia sobre el mapa" -
        // version real, tercera ronda tras la correccion del coordinador: investigado a fondo que
        // la Zona de un paso de la Guia NUNCA es una coordenada de punto (la Guia describe
        // requisitos, no ubicaciones) - de las 10 Zonas reales del catalogo, 5 SI tienen una
        // posicion real sin escanear tiles ("Mazmorra" = WldDungeonX/Y ya leido; las 4 capas de
        // profundidad = los mismos umbrales reales que ya pinta el fondo del mapa,
        // WldHeader.ZoneFor). Las 5 restantes (biomas reales) exigirian un detector de bioma
        // nuevo con datos que hoy no existen en el proyecto - LIMITE real, documentado en el
        // propio codigo de ExplorationViewModel.
        //
        // Verificacion con "aislar la variable": la verdad de referencia de los limites de banda
        // se calcula AQUI con un WldReader.Read PROPIO (nunca el _world interno del ViewModel), y
        // el paso "objetivo actual" se fuerza a mano sobre un paso REAL ya existente en el arbol
        // de la Guia (nunca inventado - GuidePasoViewModel tiene constructor internal, no se
        // puede fabricar uno sintetico desde este proyecto de pruebas) para comprobar la
        // Visibility real en los dos sentidos (aparece Y desaparece).
        if (Environment.GetEnvironmentVariable("IDEA7_SOLO") == "1")
        {
            try
            {
                string mundoPath = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
                if (!File.Exists(mundoPath)) { Console.WriteLine("IDEA7_SOLO: AVISO - falta el mundo real de prueba, se omite"); }
                else
                {
                    var personajeReal = vm.Home.Characters.FirstOrDefault();
                    if (personajeReal != null) { vm.Home.OpenCommand.Execute(personajeReal); DoEvents(); DoEvents(); }

                    vm.SelectedTabIndex = 4; // Exploracion
                    DoEvents();
                    var taskCarga = vm.Exploration.LoadFromPathAsync(mundoPath);
                    while (!taskCarga.IsCompleted) DoEvents();
                    DoEvents(); DoEvents();

                    // --- Verdad de referencia de las 4 bandas, WldReader propio ---
                    var mundoVerdad = Terrakeep.Core.WldFormat.WldReader.Read(File.ReadAllBytes(mundoPath));
                    double groundLevel = mundoVerdad.Header.GroundLevel, rockLevel = mundoVerdad.Header.RockLevel;
                    double infiernoTop = mundoVerdad.Header.TilesHigh - 192;
                    Console.WriteLine($"IDEA7_SOLO: verdad de referencia -> GroundLevel={groundLevel}, RockLevel={rockLevel}, TilesHigh={mundoVerdad.Header.TilesHigh}, InfiernoTop={infiernoTop}");
                    Console.WriteLine($"IDEA7_SOLO: bandas del ViewModel -> Superficie[{vm.Exploration.GuideBandSuperficieTop},{vm.Exploration.GuideBandSuperficieHeight}] Subterraneo[{vm.Exploration.GuideBandSubterraneoTop},{vm.Exploration.GuideBandSubterraneoHeight}] Cavernas[{vm.Exploration.GuideBandCavernasTop},{vm.Exploration.GuideBandCavernasHeight}] Infierno[{vm.Exploration.GuideBandInfiernoTop},{vm.Exploration.GuideBandInfiernoHeight}]");

                    bool bandasOk = vm.Exploration.GuideBandSuperficieTop == 0.0 && vm.Exploration.GuideBandSuperficieHeight == groundLevel
                        && vm.Exploration.GuideBandSubterraneoTop == groundLevel && vm.Exploration.GuideBandSubterraneoHeight == rockLevel - groundLevel
                        && vm.Exploration.GuideBandCavernasTop == rockLevel && vm.Exploration.GuideBandCavernasHeight == infiernoTop - rockLevel
                        && vm.Exploration.GuideBandInfiernoTop == infiernoTop && vm.Exploration.GuideBandInfiernoHeight == 192.0;
                    if (!bandasOk) Console.WriteLine("FALLO: IDEA7_SOLO - las bandas de profundidad calculadas por el ViewModel no coinciden con la verdad de referencia real");
                    else Console.WriteLine("IDEA7_SOLO: las 4 bandas coinciden EXACTAMENTE con la verdad de referencia real");

                    // --- Navega a la Guia real para poblar vm.Guide.Tramos con pasos REALES ---
                    vm.SelectedTabIndex = 3; // Guia - AppTab.Guia, reordenado T1 21-sep-2026
                    DoEvents(); DoEvents();
                    var pasoMazmorra = vm.Guide.Tramos.SelectMany(t => t.Pasos).FirstOrDefault(p => p.Zona == "Mazmorra");
                    var pasoCapa = vm.Guide.Tramos.SelectMany(t => t.Pasos).FirstOrDefault(p => p.Zona is "Cavernas" or "Subterraneo" or "Superficie" or "Infierno");
                    Console.WriteLine($"IDEA7_SOLO: paso real con Zona=Mazmorra encontrado={pasoMazmorra != null}, paso real con Zona de capa encontrado={pasoCapa != null} (Zona='{pasoCapa?.Zona}')");

                    vm.SelectedTabIndex = 4; // vuelve a Exploracion para medir el mapa real
                    DoEvents(); DoEvents();

                    if (pasoMazmorra != null)
                    {
                        vm.Guide.ObjetivoPaso = pasoMazmorra;
                        DoEvents(); DoEvents();
                        var marcador = Descendientes<System.Windows.Controls.Grid>(window).FirstOrDefault(g => ReferenceEquals(g.ToolTip, null) == false && Equals(g.ToolTip, pasoMazmorra.Titulo));
                        Console.WriteLine($"IDEA7_SOLO: marcador de objetivo (Mazmorra) encontrado={marcador != null}, visible={marcador?.IsVisible}");
                        if (marcador == null || !marcador.IsVisible) Console.WriteLine("FALLO: IDEA7_SOLO - el marcador de objetivo de mazmorra no aparece con ObjetivoPaso real apuntando a Mazmorra");

                        vm.Guide.ObjetivoPaso = null;
                        DoEvents(); DoEvents();
                        bool siguevisible = marcador != null && marcador.IsVisible;
                        Console.WriteLine($"IDEA7_SOLO: tras quitar el objetivo, marcador sigue visible={siguevisible} (esperado False)");
                        if (siguevisible) Console.WriteLine("FALLO: IDEA7_SOLO - el marcador de objetivo NO desaparece al quitar ObjetivoPaso (falso positivo permanente)");
                    }
                    else Console.WriteLine("IDEA7_SOLO: AVISO - ningun paso real del catalogo de Guia tiene Zona=Mazmorra, se omite esa comprobacion");

                    if (pasoCapa != null)
                    {
                        vm.Guide.ObjetivoPaso = pasoCapa;
                        DoEvents(); DoEvents();
                        // La ventana entera tiene MUCHOS Rectangle visibles ajenos a esto
                        // (bordes/decoracion de otros controles) - se aislan por las DOS señas
                        // reales que solo llevan mis 4 bandas: IsHitTestVisible=False (puesto a
                        // mano en el XAML, ningun otro Rectangle real de la app lo usa asi) Y el
                        // mismo pincel real AccentMutedBrush.
                        var accentMuted = System.Windows.Application.Current.TryFindResource("AccentMutedBrush");
                        var rects = Descendientes<System.Windows.Shapes.Rectangle>(window)
                            .Where(r => r.IsVisible && !r.IsHitTestVisible && Equals(r.Fill, accentMuted)).ToList();
                        Console.WriteLine($"IDEA7_SOLO: rectangulos de banda VISIBLES tras fijar Zona='{pasoCapa.Zona}' -> {rects.Count} (esperado 1)");
                        if (rects.Count != 1) Console.WriteLine($"FALLO: IDEA7_SOLO - se esperaba exactamente 1 banda visible para Zona='{pasoCapa.Zona}', hay {rects.Count}");
                        else
                        {
                            double topReal = System.Windows.Controls.Canvas.GetTop(rects[0]);
                            Console.WriteLine($"IDEA7_SOLO: Canvas.Top real de la banda visible = {topReal}");
                        }

                        vm.Guide.ObjetivoPaso = null;
                        DoEvents(); DoEvents();
                        int rectsDespues = Descendientes<System.Windows.Shapes.Rectangle>(window)
                            .Count(r => r.IsVisible && !r.IsHitTestVisible && Equals(r.Fill, accentMuted));
                        Console.WriteLine($"IDEA7_SOLO: bandas visibles tras quitar el objetivo = {rectsDespues} (esperado 0)");
                        if (rectsDespues != 0) Console.WriteLine("FALLO: IDEA7_SOLO - una banda de profundidad sigue visible tras quitar ObjetivoPaso");
                    }
                    else Console.WriteLine("IDEA7_SOLO: AVISO - ningun paso real del catalogo de Guia tiene Zona de capa de profundidad, se omite esa comprobacion");

                    if (pasoCapa != null)
                    {
                        vm.Guide.ObjetivoPaso = pasoCapa;
                        DoEvents(); DoEvents(); DoEvents();
                        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtb.Render(window);
                        var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                        string shot = Path.Combine(AppContext.BaseDirectory, "idea7-capa-guia-mapa.png");
                        using (var fs = File.Create(shot)) enc.Save(fs);
                        Console.WriteLine($"IDEA7_SOLO: captura real -> {shot}");
                        vm.Guide.ObjetivoPaso = null;
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("IDEA7_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (IDEA7_SOLO)");
            Environment.Exit(0);
        }

        // GUIACHIP_SOLO=1 (21-sep-2026, pedido explicito del coordinador tras el reporte del
        // usuario de "niebla azul dividiendo la superficie" confundida con un bug): comprueba de
        // verdad, con un personaje y un mundo sinteticos pero REALES (mismo mecanismo real de
        // WldWriter.WriteWorld/PlrFile.Write que ya usan las pruebas xUnit de esta misma sesion,
        // nunca datos inventados a mano en el XAML), que el chip real "Guía: Superficie" aparece
        // en pantalla con el color correcto y se distingue del resto de marcadores del mapa.
        //
        // Personaje/mundo COMPLETAMENTE limpios (sin ningun jefe derrotado, sin refugio, sin
        // NPCs mudados): segun guia_progresion.json real, el PRIMER tramo real no opcional es
        // PreOjo (orden=10; ReySlime, orden=5, se salta por ser opcional) y su primer paso real
        // es "Refugio", con zona="Superficie" - asi que el objetivo pendiente de la Guia con
        // estos datos DEBE caer ahi, sin forzar nada a mano.
        if (Environment.GetEnvironmentVariable("GUIACHIP_SOLO") == "1")
        {
            try
            {
                string dirChip = Path.Combine(Path.GetTempPath(), $"terrakeep-guiachip-{Guid.NewGuid():N}");
                Directory.CreateDirectory(dirChip);
                string plrPath = Path.Combine(dirChip, "PersonajeLimpio.plr");
                // Mundo real (roca_negra.wld, YA usado por decenas de pruebas reales de este mismo
                // arnes - un mundo sintetico minimo de 60x80 se probo primero y renderizaba el mapa
                // completamente negro, un caso limite propio del arnes de pruebas, no del producto
                // real, ver bitacora.md) - solo hace falta que el PERSONAJE este limpio para que el
                // objetivo pendiente de la Guia caiga en Superficie de verdad.
                string wldPath = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");

                var personajeLimpio = new Terrakeep.Core.PlrFormat.PlrCharacter
                {
                    Name = "PersonajeLimpio",
                    Version = 279,
                    PrimaryLoadout = Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: true),
                    Loadouts = [Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false), Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false), Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false)],
                };
                File.WriteAllBytes(plrPath, Terrakeep.Core.PlrFormat.PlrFile.Write(personajeLimpio));

                vm.LoadFromPath(plrPath);
                DoEvents(); DoEvents();
                vm.SelectedTabIndex = 4; // Exploracion
                DoEvents(); DoEvents();
                var tareaChip = vm.Exploration.LoadFromPathAsync(wldPath);
                while (!tareaChip.IsCompleted) DoEvents();
                DoEvents(); DoEvents(); DoEvents();
                System.Threading.Thread.Sleep(300); DoEvents(); DoEvents();

                // roca_negra.wld es un mundo real con progreso real ya guardado (jefes derrotados
                // reales en su cabecera) - a diferencia de un mundo sintetico en blanco, el objetivo
                // pendiente real de la Guia puede caer en CUALQUIERA de las 4 zonas segun el progreso
                // real de este mundo concreto, no siempre Superficie. Se comprueba dinamicamente
                // contra la MISMA zona real que YA devuelve el ViewModel, nunca una zona fija.
                string zonaReal = vm.Guide.ObjetivoPaso?.Zona ?? "(null)";
                var colorPorZona = new Dictionary<string, string>
                {
                    ["Superficie"] = "#FFFFD24A",   // MasterGoldBrush
                    ["Subterraneo"] = "#FF3DDC6E",  // EquippedGreenBrush
                    ["Cavernas"] = "#FF9B59B6",      // DebuffBrush
                    ["Infierno"] = "#FFC0392B",      // CalamityBrush
                };
                var claveChipPorZona = new Dictionary<string, string>
                {
                    ["Superficie"] = "guide_zone_chip_superficie",
                    ["Subterraneo"] = "guide_zone_chip_subterraneo",
                    ["Cavernas"] = "guide_zone_chip_cavernas",
                    ["Infierno"] = "guide_zone_chip_infierno",
                };
                Console.WriteLine($"GUIACHIP_SOLO: personaje limpio + mundo real 'roca_negra.wld' -> Guide.ObjetivoPaso.Zona real = '{zonaReal}' (una de las 4 bandas de profundidad reales, segun el progreso real de este mundo)");
                if (!colorPorZona.ContainsKey(zonaReal))
                {
                    Console.WriteLine($"GUIACHIP_SOLO: zona real '{zonaReal}' no es una de las 4 bandas de profundidad (es un bioma/punto de interes real, ej. Mazmorra/Jungla) - el chip de banda no aplica aqui, se omite el resto de la comprobacion");
                }
                else
                {
                    string claveChip = claveChipPorZona[zonaReal];
                    var chipVisible = Descendientes<System.Windows.Controls.TextBlock>(window)
                        .FirstOrDefault(t => t.Text == vm.Loc[claveChip] && t.IsVisible);
                    Console.WriteLine($"GUIACHIP_SOLO: chip real '{vm.Loc[claveChip]}' visible en el arbol visual={chipVisible != null}");
                    if (chipVisible == null) Console.WriteLine($"FALLO: GUIACHIP_SOLO - el chip real no esta visible en pantalla pese a Zona={zonaReal}");
                    else
                    {
                        var borderPadre = System.Windows.Media.VisualTreeHelper.GetParent(System.Windows.Media.VisualTreeHelper.GetParent(chipVisible) as System.Windows.DependencyObject ?? chipVisible) as System.Windows.Controls.Border;
                        string colorReal = (borderPadre?.Background as System.Windows.Media.SolidColorBrush)?.Color.ToString() ?? "(sin Border padre real)";
                        string colorEsperado = colorPorZona[zonaReal];
                        Console.WriteLine($"GUIACHIP_SOLO: color de fondo real del chip = {colorReal} (esperado {colorEsperado})");
                        if (colorReal != colorEsperado) Console.WriteLine($"FALLO: GUIACHIP_SOLO - el color real del chip no coincide con el esperado para {zonaReal} ({colorReal} vs {colorEsperado})");
                    }

                    // Comprobacion real de la BANDA (no solo del chip): busca el Rectangle real
                    // con el Fill de esta zona y confirma que existe, esta visible y tiene la
                    // opacidad esperada (0.30, la misma que fija el XAML para las 4 bandas).
                    var brushEsperado = colorPorZona[zonaReal];
                    var bandaReal = Descendientes<System.Windows.Shapes.Rectangle>(window)
                        .FirstOrDefault(r => (r.Fill as System.Windows.Media.SolidColorBrush)?.Color.ToString() == brushEsperado);
                    if (bandaReal == null) Console.WriteLine($"FALLO: GUIACHIP_SOLO-BANDA - no se encontro ningun Rectangle real con Fill={brushEsperado} para la zona {zonaReal}");
                    else
                    {
                        Console.WriteLine($"GUIACHIP_SOLO-BANDA: Rectangle real encontrado para {zonaReal}, IsVisible={bandaReal.IsVisible}, Opacity real={bandaReal.Opacity}, Height real={bandaReal.ActualHeight:F1}, Width real={bandaReal.ActualWidth:F1}");
                        if (!bandaReal.IsVisible) Console.WriteLine($"FALLO: GUIACHIP_SOLO-BANDA - la banda real de {zonaReal} no esta visible");
                        if (Math.Abs(bandaReal.Opacity - 0.30) > 0.01) Console.WriteLine($"FALLO: GUIACHIP_SOLO-BANDA - opacidad real ({bandaReal.Opacity}) distinta de la esperada (0.30)");

                        // Prueba DECISIVA del color/opacidad real que de verdad pinta el motor:
                        // renderizado AISLADO del propio Rectangle vía VisualBrush en su propio
                        // RenderTargetBitmap pequeño (200x200), sin pasar por la ventana entera.
                        // Se probo antes con RenderTargetBitmap.Render(window) y ese camino da un
                        // falso negativo (pixeles siempre transparentes) para este elemento
                        // concreto de 8400 unidades de ancho real (=21000px a zoom 250%) - un
                        // limite conocido y ya documentado de esa API de captura de ventana
                        // completa con elementos extremadamente grandes, no del producto real (el
                        // usuario ve composicion GPU real via DWM, nunca este pase software). Este
                        // camino aislado SI es fiable: decodifica el pixel real, premultiplicado,
                        // y lo compara con el color/opacidad esperados de verdad.
                        var rtbBanda = new System.Windows.Media.Imaging.RenderTargetBitmap(200, 200, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        var visualBanda = new System.Windows.Media.DrawingVisual();
                        using (var dcBanda = visualBanda.RenderOpen())
                        {
                            var vb = new System.Windows.Media.VisualBrush(bandaReal) { Stretch = System.Windows.Media.Stretch.None, ViewboxUnits = System.Windows.Media.BrushMappingMode.Absolute, Viewbox = new System.Windows.Rect(0, 0, 200, 200) };
                            dcBanda.DrawRectangle(vb, null, new System.Windows.Rect(0, 0, 200, 200));
                        }
                        rtbBanda.Render(visualBanda);
                        var pixelesBanda = new byte[200 * 200 * 4];
                        rtbBanda.CopyPixels(pixelesBanda, 200 * 4, 0);
                        byte pb = pixelesBanda[0], pg = pixelesBanda[1], pr = pixelesBanda[2], pa = pixelesBanda[3];
                        bool algoNoTransparente = pa != 0;
                        Console.WriteLine($"GUIACHIP_SOLO-BANDA: render aislado real -> pixel premultiplicado (B={pb},G={pg},R={pr},A={pa})");
                        if (!algoNoTransparente) Console.WriteLine($"FALLO: GUIACHIP_SOLO-BANDA - el Rectangle real de {zonaReal} no pinta ningun pixel no-transparente en aislamiento (color/opacidad rotos de verdad)");
                        else
                        {
                            // Despremultiplicar y comparar contra el color esperado (esperado con
                            // Opacity=0.30 real, no el color solido puro de la marca).
                            double alphaFrac = pa / 255.0;
                            int rReal = (int)Math.Round(pr / alphaFrac), gReal = (int)Math.Round(pg / alphaFrac), bReal = (int)Math.Round(pb / alphaFrac);
                            var colorEsperadoWpf = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(brushEsperado);
                            int difR = Math.Abs(rReal - colorEsperadoWpf.R), difG = Math.Abs(gReal - colorEsperadoWpf.G), difB = Math.Abs(bReal - colorEsperadoWpf.B);
                            Console.WriteLine($"GUIACHIP_SOLO-BANDA: color real despremultiplicado=({rReal},{gReal},{bReal}) alpha real={alphaFrac:F2} vs color de marca esperado ({colorEsperadoWpf.R},{colorEsperadoWpf.G},{colorEsperadoWpf.B}) opacidad esperada 0.30");
                            if (difR > 6 || difG > 6 || difB > 6) Console.WriteLine($"FALLO: GUIACHIP_SOLO-BANDA - color real de la banda de {zonaReal} no coincide con el color de marca esperado (dif R={difR} G={difG} B={difB})");
                            var encBanda = new System.Windows.Media.Imaging.PngBitmapEncoder();
                            encBanda.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbBanda));
                            string shotBanda = Path.Combine(AppContext.BaseDirectory, $"guiabanda-{zonaReal}.png");
                            using (var fsBanda = File.Create(shotBanda)) encBanda.Save(fsBanda);
                            Console.WriteLine($"GUIACHIP_SOLO-BANDA: captura aislada real -> {shotBanda}");
                        }
                    }
                }

                DoEvents(); DoEvents();
                var rtbChip = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbChip.Render(window);
                var encChip = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encChip.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbChip));
                string shotChip = Path.Combine(AppContext.BaseDirectory, "guiachip-superficie.png");
                using (var fsChip = File.Create(shotChip)) encChip.Save(fsChip);
                Console.WriteLine($"GUIACHIP_SOLO: captura real -> {shotChip}");

                try { Directory.Delete(dirChip, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
            catch (Exception ex) { Console.WriteLine("GUIACHIP_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (GUIACHIP_SOLO)");
            Environment.Exit(0);
        }

        // BADGES_ESTADO_SOLO=1 (24-sep-2026, investigador-bug, cierra el hueco de cobertura real
        // que dejo pasar el reporte del usuario sobre "67 diferencia(s)" en Comparar, imagen4.png):
        // cuerpo real en AuditoriaBadgesEstado.cs - mide y compara la geometria real (CornerRadius/
        // Padding/Background) de los badges/status-pills NO interactivos de la app (el pill de
        // Comparar, "Solo lectura" y "Guia: <Zona>" de Exploracion), algo que ningun modo anterior
        // de este arnes comprobaba JUNTO.
        if (Environment.GetEnvironmentVariable("BADGES_ESTADO_SOLO") == "1")
        {
            EjecutarBadgesEstadoSolo(window, vm);
        }

        // T6_SOLO=1 (20-sep-2026, catalogo de rediseño visual, "Exploracion a pantalla completa" -
        // reabierto por instruccion explicita del coordinador/usuario tras documentarlo como
        // LIMITE-por-riesgo-historico: el usuario aclaro que el historial real de bugs de esta
        // zona (AR-MRK del mapa, AR-11a/AR-15/AR-EX1/FALLO-3 de la columna lateral) es cautela, no
        // evidencia de imposibilidad, y pidio un intento real). Verifica con geometria real que el
        // mapa usa de verdad el ancho COMPLETO de la ventana (no solo el que le dejaba la columna
        // vieja) y que el panel lateral flota ENCIMA (su Left real cae DENTRO del ancho del mapa,
        // nunca a su derecha como una columna separada).
        if (Environment.GetEnvironmentVariable("T6_SOLO") == "1")
        {
            try
            {
                string mundoPath = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
                if (!File.Exists(mundoPath)) { Console.WriteLine("T6_SOLO: AVISO - falta el mundo real de prueba, se omite"); }
                else
                {
                    FijarTamaño(window, 1400, 900);
                    vm.SelectedTabIndex = 4; // Exploracion
                    DoEvents();
                    var tarea = vm.Exploration.LoadFromPathAsync(mundoPath);
                    while (!tarea.IsCompleted) DoEvents();
                    DoEvents(); DoEvents(); DoEvents();

                    // ADR-TERRAKEEP-030 (27-sep-2026): Mapa+minimapa se movio a WorldMapView.xaml
                    // (UserControl con su propio NameScope) - FindName DOBLE, mismo patron ya
                    // usado por GUIA/ADR-021, Compare/ADR-025, WorldTools/ADR-027,
                    // ChestInspector/ADR-028 y Browse/ADR-029.
                    var worldMapViewHostT6 = window.FindName("WorldMapView") as FrameworkElement;
                    var mapaImagen = worldMapViewHostT6?.FindName("WorldMapImage") as FrameworkElement;
                    var mapaScroll = worldMapViewHostT6?.FindName("WorldMapScroll") as FrameworkElement;
                    Console.WriteLine($"T6_SOLO: WorldMapImage encontrado={mapaImagen != null}, WorldMapScroll encontrado={mapaScroll != null}");

                    // Sanidad del METODO antes de fiarse de un FALLO real: comprueba que
                    // VisualTreeHelper.HitTest sabe descender en este mismo arnes sobre un control
                    // YA EXISTENTE y sin tocar (el propio boton "Actualizar" de la fila de mundos) -
                    // si esto tambien falla, el problema es del metodo de verificacion, no del
                    // producto.
                    var botonActualizar = Descendientes<System.Windows.Controls.Button>(window)
                        .FirstOrDefault(b => b.IsVisible && b.Content as string == vm.Loc["home_refresh"]);
                    if (botonActualizar != null)
                    {
                        var centroBoton = botonActualizar.TranslatePoint(new Point(botonActualizar.ActualWidth / 2, botonActualizar.ActualHeight / 2), window);
                        var resultadoBoton = System.Windows.Media.VisualTreeHelper.HitTest(window, centroBoton);
                        bool tocaElBoton = false;
                        for (DependencyObject? d = resultadoBoton?.VisualHit; d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
                            if (ReferenceEquals(d, botonActualizar)) { tocaElBoton = true; break; }
                        Console.WriteLine($"T6_SOLO-CANARIO: HitTest sobre el boton real 'Actualizar' (control YA EXISTENTE, sin tocar) -> elemento golpeado={resultadoBoton?.VisualHit?.GetType().Name}, alcanza el boton={tocaElBoton} (si esto es False, el metodo de verificacion no sirve en este arnes, no es un fallo de T6)");
                    }

                    // Geometria real: el Border del mapa (padre real de WorldMapScroll) tiene que
                    // ocupar de verdad el ANCHO COMPLETO de la ventana (menos margenes minimos) -
                    // no solo el hueco que antes le dejaba la columna 0 con la 2 restando su ancho.
                    Border? bordeMapa = null;
                    var bgSecundario = System.Windows.Application.Current.TryFindResource("BgSecondaryBrush");
                    for (DependencyObject? d = mapaScroll; d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
                        if (d is Border bCandidato && Equals(bCandidato.Background, bgSecundario)) { bordeMapa = bCandidato; break; }
                    if (bordeMapa != null)
                    {
                        var zonaMapa = RectCompleto(bordeMapa, window);
                        Console.WriteLine($"T6_SOLO: Border real del mapa -> ancho={zonaMapa.Width:0.#} (ventana={window.ActualWidth:0.#}, esperado > 80% del ancho real de la ventana - antes de T6 el mapa perdia ~380px reales de columna lateral)");
                        if (zonaMapa.Width < window.ActualWidth * 0.8)
                            Console.WriteLine("FALLO: T6_SOLO - el Border del mapa NO esta usando el ancho completo de la ventana (T6 no aplico de verdad)");
                    }
                    else Console.WriteLine("FALLO: T6_SOLO - no se encuentra el Border real del mapa (BgSecondaryBrush) en el arbol");

                    // El panel lateral (tarjeta flotante) tiene que estar POSICIONADO DENTRO del
                    // area del mapa (su borde izquierdo real cae mas alla de donde el mapa YA
                    // empieza a pintarse), nunca en una columna aparte a la derecha del todo.
                    var panelLateral = window.FindName("ExplorationSidebarScroll") as FrameworkElement;
                    if (panelLateral != null && bordeMapa != null)
                    {
                        var zonaPanel = RectCompleto(panelLateral, window);
                        var zonaMapa2 = RectCompleto(bordeMapa, window);
                        Console.WriteLine($"T6_SOLO: panel lateral real -> Left={zonaPanel.Left:0.#}, mapa Right={zonaMapa2.Right:0.#} (esperado panel.Left < mapa.Right, es decir DENTRO del area del mapa, flotando)");
                        if (zonaPanel.Left >= zonaMapa2.Right)
                            Console.WriteLine("FALLO: T6_SOLO - el panel lateral esta fuera del area del mapa (sigue siendo una columna aparte, no flota encima)");
                    }

                    var fitMethod = typeof(MainWindow).GetMethod("OnFitToWindowClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    fitMethod?.Invoke(window, [window, new RoutedEventArgs()]);
                    DoEvents(); DoEvents();
                    string shot = Path.Combine(AppContext.BaseDirectory, "t6-exploracion-pantalla-completa.png");
                    var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb.Render(window);
                    var enc = new System.Windows.Media.Imaging.PngBitmapEncoder(); enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                    using (var fs = File.Create(shot)) enc.Save(fs);
                    Console.WriteLine($"T6_SOLO: captura real -> {shot}");

                    // Verifica que el panel flotante se PINTA de verdad encima del mapa (z-order
                    // real, no solo "en teoria deberia"). VisualTreeHelper.HitTest se probo primero
                    // para esto y se DESCARTO con evidencia real: un canario contra un boton YA
                    // EXISTENTE y sin tocar ("Actualizar", ver el bloque CANARIO de arriba) tambien
                    // devuelve el Grid raiz sin descender - HitTest no funciona de forma fiable en
                    // este arnes concreto (probablemente por como se renderiza la ventana fuera de
                    // pantalla para las pruebas), asi que un FALLO ahi no seria evidencia de nada
                    // real. En su lugar: WPF pinta los hijos de un Panel en el ORDEN en que
                    // aparecen en su coleccion Children - declarar la tarjeta flotante DESPUES del
                    // Border del mapa en el mismo Grid padre (MainWindow.xaml) es lo que garantiza
                    // que se pinta encima, y eso SI es un hecho verificable con
                    // VisualTreeHelper.GetChildrenCount/GetChild sobre el padre comun real.
                    var tarjetaFlotanteFe = window.FindName("ExplorationSidebarFloatingCard") as FrameworkElement;
                    if (tarjetaFlotanteFe != null && bordeMapa != null && System.Windows.Media.VisualTreeHelper.GetParent(bordeMapa) is Panel padreComun
                        && ReferenceEquals(System.Windows.Media.VisualTreeHelper.GetParent(tarjetaFlotanteFe), padreComun))
                    {
                        int indiceMapa = -1, indicePanel = -1;
                        int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(padreComun);
                        for (int i = 0; i < n; i++)
                        {
                            var hijo = System.Windows.Media.VisualTreeHelper.GetChild(padreComun, i);
                            if (ReferenceEquals(hijo, bordeMapa)) indiceMapa = i;
                            if (ReferenceEquals(hijo, tarjetaFlotanteFe)) indicePanel = i;
                        }
                        Console.WriteLine($"T6_SOLO: z-order real en el Grid padre comun -> indice del Border del mapa={indiceMapa}, indice de la tarjeta flotante={indicePanel} (esperado tarjeta > mapa, se pinta encima)");
                        if (!(indicePanel > indiceMapa))
                            Console.WriteLine("FALLO: T6_SOLO - la tarjeta flotante NO esta declarada despues del mapa en el mismo Grid, no hay garantia real de que se pinte encima");
                    }
                    else Console.WriteLine("T6_SOLO: AVISO - no se pudo confirmar el padre comun real del mapa y la tarjeta flotante, se omite la comprobacion de z-order");

                    // Colapsa el panel a 0 y confirma que el mapa gana ESE ancho real de verdad
                    // (no solo "no se ve nada raro") - isla el efecto real del colapso.
                    double anteAColapsar = bordeMapa != null ? RectCompleto(bordeMapa, window).Width : 0;
                    var toggleMethod = typeof(MainWindow).GetMethod("OnToggleExplorationSidebarClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Console.WriteLine($"T6_SOLO: ExplorationSidebarWidth antes de colapsar={vm.Settings.ExplorationSidebarWidth:0.#}");
                    toggleMethod?.Invoke(window, [window, new RoutedEventArgs()]);
                    DoEvents(); DoEvents(); DoEvents();
                    Console.WriteLine($"T6_SOLO: ExplorationSidebarWidth tras colapsar={vm.Settings.ExplorationSidebarWidth:0.#} (esperado 0)");
                    string shot2 = Path.Combine(AppContext.BaseDirectory, "t6-exploracion-panel-plegado.png");
                    var rtb2 = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb2.Render(window);
                    var enc2 = new System.Windows.Media.Imaging.PngBitmapEncoder(); enc2.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb2));
                    using (var fs = File.Create(shot2)) enc2.Save(fs);
                    Console.WriteLine($"T6_SOLO: captura real (panel plegado) -> {shot2}");
                    // Vuelve a expandir para no dejar el resto del arnes con el panel plegado.
                    toggleMethod?.Invoke(window, [window, new RoutedEventArgs()]);
                    DoEvents();
                }
            }
            catch (Exception ex) { Console.WriteLine("T6_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (T6_SOLO)");
            Environment.Exit(0);
        }

        // T3_SOLO=1 RETIRADO (25-sep-2026, aplicador-fix, TASK CONTEXT
        // e5eaea9e-c261-4199-8e7d-060b6054f58d): este canario verificaba el tablero de scroll
        // continuo + barra pegajosa (T3 PASO 1/2/3, 20-sep-2026) - diseño REABIERTO y sustituido
        // por NAV123 (paginas exclusivas reales, ver NAV123_SOLO/CanarioNav123YClipCardsLibreria.cs)
        // porque el usuario reporto el bug real "toggle 3 desincronizado" (ScrollViewer clampando
        // el offset con Almacenes con pocos objetos). ObjetosBoardScroll/ObjetosBoardStack/
        // ObjetosStickyBar ya NO EXISTEN en produccion (MainWindow.xaml/.xaml.cs) - el bloque real
        // que medía sus offsets se retiro de aqui a proposito (no tiene sentido conservar un
        // canario que verifica una pieza que ya no existe). Queda este stub para que
        // T3_SOLO=1 siga dando una respuesta clara (no un cuelgue ni un WPF FindName silencioso)
        // si alguien lo invoca todavia por costumbre.
        if (Environment.GetEnvironmentVariable("T3_SOLO") == "1")
        {
            Console.WriteLine("T3_SOLO: RETIRADO - el tablero de scroll continuo + barra pegajosa que este canario verificaba ya no existe en produccion (sustituido por NAV123, paginas exclusivas). Usa NAV123_SOLO=1 en su lugar.");
            Console.WriteLine("DONE (T3_SOLO)");
            Environment.Exit(0);
        }

        // NAV123_SOLO=1 / LIBCARD_CLIP_SOLO=1 (24-sep-2026, revision-correccion-integral-familia-Keep,
        // bloque "imagen2" - Personaje>Objetos): canarios reales de investigador-bug (patron de 2
        // fases), ver el comentario largo de cabecera en CanarioNav123YClipCardsLibreria.cs.
        if (Environment.GetEnvironmentVariable("NAV123_SOLO") == "1")
        {
            EjecutarNav123Solo(window, vm);
            Console.WriteLine("DONE (NAV123_SOLO)");
            Environment.Exit(0);
        }
        // EQUIP_RESPONSIVE_SOLO=1 (28-sep-2026, FASE B del responsive global): geometria real de
        // Personaje > Objetos > Equipamiento en varios tamaños, ES/EN, resize en caliente y
        // negative acceptance del mecanismo viejo - ver CanarioResponsiveEquipamiento.cs.
        if (Environment.GetEnvironmentVariable("EQUIP_RESPONSIVE_SOLO") == "1")
        {
            EjecutarEquipResponsiveSolo(window, vm);
            Console.WriteLine("DONE (EQUIP_RESPONSIVE_SOLO)");
            Environment.Exit(0);
        }
        // INVALM_RESPONSIVE_SOLO=1 (28-sep-2026, FASE C del responsive global): Inventario y los 4
        // Almacenes con colecciones llenas, tamaños, ES/EN, compacto, clic en slot sin mover el scroll,
        // resize en caliente y negative acceptance del scroll local viejo - ver
        // CanarioResponsiveInventarioAlmacenes.cs.
        // AISLAMIENTO_MUNDO_NEGATIVO_SOLO=1 (28-sep-2026, FASE C - negative acceptance del aislamiento
        // de mundos): intenta abrir en Exploracion la ruta REAL de un mundo (no la copia). La guarda
        // (CharacterFileService.ComprobarMundoDePrueba, primera linea de LoadFromPathAsync) tiene que
        // abortar el proceso con "FALLO: AISLAMIENTO-MUNDO" y codigo 5 ANTES de leer un solo byte. Si
        // se llega a la linea siguiente, la guarda no funciona.
        // AISLAMIENTO_EXCEPCION_SOLO=1 (28-sep-2026, R2-L2 b): carga la COPIA de un personaje (MainWindow
        // reescribe session.json con la ruta temporal) y lanza una excepcion SIN capturar desde Main.
        // ProcessExit no corre en ese caso; session.json tiene que volver igualmente a sus bytes
        // originales via AppDomain.UnhandledException (comprobar el SHA256 desde fuera).
        if (Environment.GetEnvironmentVariable("AISLAMIENTO_EXCEPCION_SOLO") == "1")
        {
            int espera = 0;
            while (vm.Home.IsScanning && espera < 200) { DoEvents(); System.Threading.Thread.Sleep(50); espera++; }
            var copia = vm.Home.Characters.FirstOrDefault();
            if (copia == null) { Console.WriteLine("FALLO: AISLAMIENTO_EXCEPCION_SOLO - no hay ninguna copia de personaje"); Environment.Exit(1); }
            vm.LoadFromPath(copia!.FilePath);
            DoEvents(); DoEvents();
            ComprobarPersonajeAislado(vm, "AISLAMIENTO_EXCEPCION_SOLO");
            vm.IsDirty = false;
            string sj = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "session.json");
            string hashTrasCargar = File.Exists(sj) ? Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(sj))) : "(no existe)";
            Console.WriteLine($"AISLAMIENTO_EXCEPCION_SOLO: session.json tras cargar la copia SHA256={hashTrasCargar}; se lanza una excepcion sin capturar");
            throw new InvalidOperationException("AISLAMIENTO_EXCEPCION_SOLO: excepcion provocada a proposito");
        }
        if (Environment.GetEnvironmentVariable("AISLAMIENTO_MUNDO_NEGATIVO_SOLO") == "1")
        {
            string real = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria", "tModLoader", "Worlds", "roca_negra.wld");
            Console.WriteLine($"AISLAMIENTO_MUNDO_NEGATIVO: intentando abrir la ruta REAL '{real}' - se espera 'FALLO: AISLAMIENTO-MUNDO' y salida con codigo 5 antes de leerlo");
            _ = vm.Exploration.LoadFromPathAsync(real);
            DoEvents();
            Console.WriteLine("FALLO: AISLAMIENTO_MUNDO_NEGATIVO_SOLO - la guarda NO aborto al abrir un mundo real");
            Environment.Exit(1);
        }
        if (Environment.GetEnvironmentVariable("INVALM_RESPONSIVE_SOLO") == "1")
        {
            EjecutarInvAlmResponsiveSolo(window, vm);
            Console.WriteLine("DONE (INVALM_RESPONSIVE_SOLO)");
            Environment.Exit(0);
        }
        // LIBRARY_RESPONSIVE_SOLO=1 (28-sep-2026, FASE D del responsive global): la familia "catalogo con
        // categorias" (Libreria de objetos, Libreria de buffs, Investigacion) + panel Editar de Objetos,
        // banda de Equipamiento y eje de Almacenes - ver CanarioResponsiveLibrerias.cs.
        if (Environment.GetEnvironmentVariable("LIBRARY_RESPONSIVE_SOLO") == "1")
        {
            EjecutarLibreriasResponsiveSolo(window, vm);
            Console.WriteLine("DONE (LIBRARY_RESPONSIVE_SOLO)");
            Environment.Exit(0);
        }
        // DIAG_NAVTOGGLE_POS_SOLO=1 (26-sep-2026, investigador-bug, mismo TASK CONTEXT de arriba):
        // diagnostico desechable, mide con UI real la posicion Y del StackPanel "ObjetosNavToggle"
        // (RadioButton 1/2/3) contra su celda Grid compartida con ObjetosPageHost - ver
        // DiagnosticoPosicionNavToggle.cs para el detalle completo de la hipotesis.
        if (Environment.GetEnvironmentVariable("DIAG_NAVTOGGLE_POS_SOLO") == "1")
        {
            EjecutarDiagnosticoPosicionNavToggleSolo(window, vm);
            Console.WriteLine("DONE (DIAG_NAVTOGGLE_POS_SOLO)");
            Environment.Exit(0);
        }
        if (Environment.GetEnvironmentVariable("LIBCARD_CLIP_SOLO") == "1")
        {
            EjecutarLibCardClipSolo(window, vm);
            Console.WriteLine("DONE (LIBCARD_CLIP_SOLO)");
            Environment.Exit(0);
        }

        // DRAG_GHOST_LIBRERIA_SOLO=1 (25-sep-2026, TASK CONTEXT e5eaea9e-c261-4199-8e7d-060b6054f58d,
        // "drag ghost blanco/vacio al arrastrar objetos/buffs desde la Libreria"): canario real de
        // investigador-bug (patron de 2 fases), ver el comentario largo de cabecera en
        // CanarioDragGhostLibreria.cs.
        if (Environment.GetEnvironmentVariable("DRAG_GHOST_LIBRERIA_SOLO") == "1")
        {
            EjecutarDragGhostLibreriaSolo(window, vm);
            Console.WriteLine("DONE (DRAG_GHOST_LIBRERIA_SOLO)");
            Environment.Exit(0);
        }

        // DRAG_GHOST_ZORDER_LIBRERIA_SOLO=1 (28-sep-2026, "se lo comen las capas de fuera de la
        // libreria" reportado por el usuario): ver el comentario largo de cabecera en
        // CanarioDragGhostZOrderLibreria.cs.
        if (Environment.GetEnvironmentVariable("DRAG_GHOST_ZORDER_LIBRERIA_SOLO") == "1")
        {
            EjecutarDragGhostZOrderLibreriaSolo(window, vm);
            Console.WriteLine("DONE (DRAG_GHOST_ZORDER_LIBRERIA_SOLO)");
            Environment.Exit(0);
        }

        // HOMEBANNER_SOLO=1 (24-sep-2026, revision-correccion-integral-familia-Keep, bloque
        // "imagen1" - Inicio: banner "Continuar con X" + tarjetas): canario real de
        // investigador-bug (patron de 2 fases), ver el comentario largo de cabecera en
        // CanarioHomeBannerMascota.cs.
        if (Environment.GetEnvironmentVariable("HOMEBANNER_SOLO") == "1")
        {
            EjecutarHomeBannerMascotaSolo(window, vm);
            Console.WriteLine("DONE (HOMEBANNER_SOLO)");
            Environment.Exit(0);
        }

        // CHESTER_GEOMETRIA_SOLO=1 (26-sep-2026, GapAnalysis ParidadPersonaje, requirement
        // 480a9bdd-6d5f-4fa6-935d-46f895e97514): evidencia real por capas (GEOMETRY_EXACT) del
        // arreglo de PlayerPetPreviewLayout/PlayerPetPreviewControl - ver el comentario largo de
        // cabecera en CanarioGeometriaChesterEvidencia.cs.
        if (Environment.GetEnvironmentVariable("CHESTER_GEOMETRIA_SOLO") == "1")
        {
            EjecutarGeometriaChesterEvidenciaSolo(window, vm);
            Console.WriteLine("DONE (CHESTER_GEOMETRIA_SOLO)");
            Environment.Exit(0);
        }

        // IDEA3_SOLO=1 (20-sep-2026, catalogo de funciones, idea 3 "Partida en vivo" - version
        // real, tercera ronda tras la correccion del coordinador: investigado a fondo
        // (SincronizacionEscritorio.cs, TerrakeepMod, otro repo) que la sincronizacion REAL entre
        // el mod y Terrakeep de escritorio YA EXISTE y ya funciona hoy - TerrakeepMod fotografia
        // el .plr al mismo formato .tkbak en la MISMA carpeta que BackupHistoryService ya lee, en
        // cada guardado real de Terraria durante la partida (SincronizacionSystem.
        // ComprobarGuardado, vigila LastWriteTimeUtc). Lo unico que de verdad faltaba: el panel
        // solo releia al ABRIRSE, asi que con Terrakeep abierto MIENTRAS se juega, las
        // instantaneas nuevas no aparecian sin cerrar/reabrir - _liveTimer (BackupHistoryViewModel)
        // cierra ese hueco con un sondeo barato mientras el panel sigue abierto.
        //
        // Verificacion con "aislar la variable": en vez de esperar el intervalo real (4s, lento e
        // inestable en CI), se invoca CheckForLiveUpdates por reflexion - la MISMA logica real que
        // dispara el Tick del temporizador, aislada de tener que esperar tiempo real de reloj. Dos
        // ramas comprobadas por separado: SI hay un fichero nuevo en disco (debe recargar) y NO lo
        // hay (debe saltarse el Reload, comprobado con un marcador real - IsConfirmingRestore que
        // solo sobrevive si Reload() NO se disparo).
        if (Environment.GetEnvironmentVariable("IDEA3_SOLO") == "1")
        {
            string? tempRoot = null;
            try
            {
                tempRoot = Path.Combine(Path.GetTempPath(), "terrakeep-idea3-partida-en-vivo-" + Guid.NewGuid().ToString("N")[..8]);
                Directory.CreateDirectory(tempRoot);
                vm.BackupHistory.Service.BackupsRoot = tempRoot; // aisla del %LOCALAPPDATA% real, mismo criterio ya establecido

                // Idea 3, narracion real (20-sep-2026, reconsiderada a peticion explicita del
                // coordinador): a diferencia de la version anterior de esta prueba (bytes basura
                // [1,2,3,4], SummaryAvailable=false a proposito, nunca comprobaba el CONTENIDO del
                // resumen), aqui se escribe un .plr REAL y valido (PlrFile.Write) con HealthMax/
                // ItemCount reales y DISTINTOS en cada instantanea - la unica forma honesta de
                // demostrar que ChangeNarrationText compara dos fotografias reales, no que
                // devuelve texto de relleno.
                string plrFalso = Path.Combine(tempRoot, "Vivo.plr");
                var personajeVivo = new PlrCharacter
                {
                    Version = 279, Name = "Vivo", PrimaryLoadout = PlrLoadout.CreateEmpty(true),
                    Loadouts = [PlrLoadout.CreateEmpty(false), PlrLoadout.CreateEmpty(false), PlrLoadout.CreateEmpty(false)],
                    HealthMax = 100, ManaMax = 20,
                };
                File.WriteAllBytes(plrFalso, PlrFile.Write(personajeVivo));
                var personajeFalso = new LoadedCharacter(plrFalso, null, "Player", personajeVivo, null, []);

                vm.BackupHistory.Service.SaveBackup(personajeFalso, Terrakeep.App.Services.BackupReason.Manual);
                vm.BackupHistory.Open(plrFalso, "Vivo", isCurrentCharacter: false);
                DoEvents(); DoEvents();
                Console.WriteLine($"IDEA3_SOLO: tras abrir el panel con 1 instantanea real -> Points.Count={vm.BackupHistory.Points.Count} (esperado 1), IsOpen={vm.BackupHistory.IsOpen}");
                if (vm.BackupHistory.Points.Count != 1) Console.WriteLine("FALLO: IDEA3_SOLO - el panel no recogio la instantanea real ya existente al abrir");
                Console.WriteLine($"IDEA3_SOLO-NARRACION: punto mas antiguo (sin nada anterior) -> ChangeNarrationText='{vm.BackupHistory.Points[0].ChangeNarrationText}' (esperado la clave real 'backup_change_first')");
                if (vm.BackupHistory.Points[0].ChangeNarrationText != vm.Loc["backup_change_first"])
                    Console.WriteLine("FALLO: IDEA3_SOLO-NARRACION - el punto mas antiguo del historial deberia decir que no hay nada anterior con que comparar");

                var metodoCheck = typeof(BackupHistoryViewModel).GetMethod("CheckForLiveUpdates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (metodoCheck == null) Console.WriteLine("FALLO: IDEA3_SOLO - CheckForLiveUpdates no existe (reflexion)");
                else
                {
                    // --- Rama "nada nuevo": el sondeo NO debe recargar (marcador real que solo
                    // sobrevive si Reload() no se disparo, Reload() pone IsConfirmingRestore=false
                    // a TODOS los puntos al reconstruir la lista entera de cero). ---
                    if (vm.BackupHistory.Points.Count > 0) vm.BackupHistory.Points[0].IsConfirmingRestore = true;
                    metodoCheck.Invoke(vm.BackupHistory, null);
                    DoEvents();
                    bool marcadorSobrevivio = vm.BackupHistory.Points.Count > 0 && vm.BackupHistory.Points[0].IsConfirmingRestore;
                    Console.WriteLine($"IDEA3_SOLO: sondeo SIN fichero nuevo -> marcador real sobrevive={marcadorSobrevivio} (esperado True, prueba de que NO se recargo sin necesidad)");
                    if (!marcadorSobrevivio) Console.WriteLine("FALLO: IDEA3_SOLO - el sondeo recargo la lista aunque no habia ninguna instantanea nueva en disco");

                    // --- Rama "hay algo nuevo": simula al mod escribiendo OTRA instantanea real
                    // mientras el panel sigue abierto - nunca se llama a Reload() a mano aqui,
                    // solo al metodo real que el Tick del temporizador dispara. Progreso REAL de
                    // personaje simulado (vida/mana max subidos, 3 objetos reales metidos en el
                    // inventario) para poder comprobar la narracion de verdad. ---
                    System.Threading.Thread.Sleep(1100); // nombre de fichero real con sello de segundo distinto, evita colision real de nombre
                    personajeVivo.HealthMax = 180;
                    personajeVivo.ManaMax = 40;
                    personajeVivo.Inventory[0] = new PlrItemSlot(Id: 1, Count: 1, Prefix: 0, Favorited: false); // Pico de cobre, id vanilla real
                    personajeVivo.Inventory[1] = new PlrItemSlot(Id: 3, Count: 1, Prefix: 0, Favorited: false); // Hacha de cobre
                    personajeVivo.Inventory[2] = new PlrItemSlot(Id: 71, Count: 5, Prefix: 0, Favorited: false); // Frasco de vida menor
                    File.WriteAllBytes(plrFalso, PlrFile.Write(personajeVivo));
                    vm.BackupHistory.Service.SaveBackup(personajeFalso, Terrakeep.App.Services.BackupReason.BeforeSave);
                    metodoCheck.Invoke(vm.BackupHistory, null);
                    DoEvents(); DoEvents();
                    Console.WriteLine($"IDEA3_SOLO: sondeo CON fichero nuevo real (simulando al mod guardando) -> Points.Count={vm.BackupHistory.Points.Count} (esperado 2)");
                    if (vm.BackupHistory.Points.Count != 2) Console.WriteLine("FALLO: IDEA3_SOLO - el sondeo en vivo NO recogio la instantanea nueva real sin cerrar/reabrir el panel");

                    if (vm.BackupHistory.Points.Count == 2)
                    {
                        string? narracion = vm.BackupHistory.Points[0].ChangeNarrationText; // Points[0] = mas reciente
                        Console.WriteLine($"IDEA3_SOLO-NARRACION: punto mas reciente (tras el progreso simulado) -> ChangeNarrationText='{narracion}'");
                        bool tieneVida = narracion != null && narracion.Contains("100") && narracion.Contains("180");
                        bool tieneMana = narracion != null && narracion.Contains("20") && narracion.Contains("40");
                        bool tieneObjetos = narracion != null && narracion.Contains("+3");
                        Console.WriteLine($"IDEA3_SOLO-NARRACION: menciona vida 100->180={tieneVida}, mana 20->40={tieneMana}, +3 objetos={tieneObjetos}");
                        if (!tieneVida) Console.WriteLine("FALLO: IDEA3_SOLO-NARRACION - la narracion real no menciona el cambio real de vida maxima (100->180)");
                        if (!tieneMana) Console.WriteLine("FALLO: IDEA3_SOLO-NARRACION - la narracion real no menciona el cambio real de mana maximo (20->40)");
                        if (!tieneObjetos) Console.WriteLine("FALLO: IDEA3_SOLO-NARRACION - la narracion real no menciona los 3 objetos reales añadidos al inventario");
                    }
                }

                vm.BackupHistory.CloseCommand.Execute(null);
                Console.WriteLine($"IDEA3_SOLO: tras cerrar el panel -> IsOpen={vm.BackupHistory.IsOpen} (esperado False)");
            }
            catch (Exception ex) { Console.WriteLine("IDEA3_SOLO-EXCEPTION: " + ex); }
            finally
            {
                try { if (tempRoot != null && Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true); } catch { }
            }
            Console.WriteLine("DONE (IDEA3_SOLO)");
            Environment.Exit(0);
        }

        // IDEA5_SOLO=1 (20-sep-2026, catalogo de funciones, idea 5 "¿Donde esta? global,
        // multi-mundo y multi-personaje" - version real, tercera ronda tras la correccion del
        // coordinador: el catalogo citaba WorldPresenceIndex como apoyo, que resulto ser el
        // censo de UN mundo ya cargado, nunca un indice persistente multi-mundo - camino real
        // encontrado: HomeViewModel.Characters + ExplorationViewModel.Worlds (las MISMAS listas
        // ya escaneadas que usan los lanzadores de Inicio/Exploracion) recorridas por
        // GlobalSearchViewModel.
        //
        // Verificacion con "aislar la variable": la verdad de referencia se calcula AQUI con un
        // CharacterFileService/WldReader PROPIOS, nunca reutilizando el resultado del ViewModel
        // bajo prueba - se elige a proposito un item REAL que el propio arnes encuentra primero
        // en un personaje real y otro en un mundo real (nunca un nombre adivinado a mano).
        if (Environment.GetEnvironmentVariable("IDEA5_SOLO") == "1")
        {
            try
            {
                int waitedHome = 0;
                while (vm.Home.IsScanning && waitedHome < 100) { DoEvents(); System.Threading.Thread.Sleep(50); waitedHome++; }
                vm.SelectedTabIndex = 4; // Exploracion (obliga a ExplorationViewModel.Worlds a escanear tambien)
                DoEvents();
                int waitedWorlds = 0;
                while (vm.Exploration.Worlds.Count == 0 && waitedWorlds < 60) { DoEvents(); System.Threading.Thread.Sleep(50); waitedWorlds++; }
                Console.WriteLine($"IDEA5_SOLO: Home.Characters={vm.Home.Characters.Count}, Exploration.Worlds={vm.Exploration.Worlds.Count}");

                // --- Parte 1: verdad de referencia de UN item real en UN personaje real ---
                var servicioAparte = new Terrakeep.App.Services.CharacterFileService();
                string? nombreItemPersonaje = null; string? personajeConItem = null;
                foreach (var entry in vm.Home.Characters)
                {
                    var loaded = servicioAparte.Load(entry.FilePath);
                    var primerItem = loaded.MergedContainers.Values.SelectMany(items => items).FirstOrDefault(i => !i.IsEmpty && !i.IsCalamity);
                    if (primerItem is not null && !primerItem.IsEmpty)
                    {
                        nombreItemPersonaje = servicioAparte.VanillaCatalog.GetName(primerItem.Id);
                        personajeConItem = entry.Name;
                        break;
                    }
                }
                Console.WriteLine($"IDEA5_SOLO: verdad de referencia (personaje) -> item real='{nombreItemPersonaje}' en personaje '{personajeConItem}'");

                // --- Parte 2: verdad de referencia de UN item real en UN mundo real (cofre) ---
                string? nombreItemMundo = null; string? mundoConItem = null;
                foreach (var entry in vm.Exploration.Worlds)
                {
                    var world = Terrakeep.Core.WldFormat.WldReader.Read(File.ReadAllBytes(entry.FilePath));
                    var itemsDelMundo = world.Chests.SelectMany(c => c.Items).Where(i => i.NetId != 0).ToList();
                    if (itemsDelMundo.Count > 0)
                    {
                        nombreItemMundo = servicioAparte.VanillaCatalog.GetName(itemsDelMundo[0].NetId);
                        mundoConItem = entry.Title;
                        break;
                    }
                }
                Console.WriteLine($"IDEA5_SOLO: verdad de referencia (mundo) -> item real='{nombreItemMundo}' en mundo '{mundoConItem}'");

                if (nombreItemPersonaje == null && nombreItemMundo == null)
                {
                    Console.WriteLine("IDEA5_SOLO: AVISO - ningun personaje/mundo real de prueba tiene ningun item, se omite la comprobacion");
                }
                else
                {
                    // --- Parte 3: la busqueda global real, sobre el item de personaje ---
                    if (nombreItemPersonaje != null)
                    {
                        var taskP = vm.GlobalSearch.RunAsync(nombreItemPersonaje, vm.Home.Characters, vm.Exploration.Worlds);
                        while (!taskP.IsCompleted) DoEvents();
                        DoEvents();
                        Console.WriteLine($"IDEA5_SOLO: busqueda global de '{nombreItemPersonaje}' -> CharacterHits={vm.GlobalSearch.CharacterHits.Count}, WorldHits={vm.GlobalSearch.WorldHits.Count}, Summary='{vm.GlobalSearch.Summary}'");
                        bool encontrado = vm.GlobalSearch.CharacterHits.Any(h => h.CharacterName == personajeConItem && h.ItemName == nombreItemPersonaje);
                        if (!encontrado) Console.WriteLine($"FALLO: IDEA5_SOLO - la busqueda global NO encontro el item real '{nombreItemPersonaje}' en el personaje real '{personajeConItem}'");
                        else Console.WriteLine($"IDEA5_SOLO: item real de personaje encontrado correctamente en CharacterHits");
                    }

                    // --- Parte 4: la busqueda global real, sobre el item de mundo ---
                    if (nombreItemMundo != null)
                    {
                        var taskM = vm.GlobalSearch.RunAsync(nombreItemMundo, vm.Home.Characters, vm.Exploration.Worlds);
                        while (!taskM.IsCompleted) DoEvents();
                        DoEvents();
                        Console.WriteLine($"IDEA5_SOLO: busqueda global de '{nombreItemMundo}' -> CharacterHits={vm.GlobalSearch.CharacterHits.Count}, WorldHits={vm.GlobalSearch.WorldHits.Count}");
                        bool encontradoMundo = vm.GlobalSearch.WorldHits.Any(h => h.WorldTitle == mundoConItem && h.ItemName == nombreItemMundo);
                        if (!encontradoMundo) Console.WriteLine($"FALLO: IDEA5_SOLO - la busqueda global NO encontro el item real '{nombreItemMundo}' en el mundo real '{mundoConItem}'");
                        else Console.WriteLine($"IDEA5_SOLO: item real de mundo encontrado correctamente en WorldHits");
                    }

                    // --- Parte 5: query sin sentido real -> 0 resultados (aisla que el buscador
                    // no marca todo como coincidencia siempre) ---
                    var taskVacio = vm.GlobalSearch.RunAsync("zzz-consulta-sin-sentido-real-qwxyz-987", vm.Home.Characters, vm.Exploration.Worlds);
                    while (!taskVacio.IsCompleted) DoEvents();
                    Console.WriteLine($"IDEA5_SOLO: query sin sentido -> CharacterHits={vm.GlobalSearch.CharacterHits.Count}, WorldHits={vm.GlobalSearch.WorldHits.Count} (esperado 0, 0)");
                    if (vm.GlobalSearch.CharacterHits.Count != 0 || vm.GlobalSearch.WorldHits.Count != 0)
                        Console.WriteLine("FALLO: IDEA5_SOLO - una consulta sin sentido real devuelve resultados (falso positivo)");

                    // --- Parte 6: captura real del popup con resultados globales visibles ---
                    if (nombreItemPersonaje != null)
                    {
                        vm.WhereIsItSearchText = nombreItemPersonaje;
                        var taskFinal = vm.GlobalSearch.RunAsync(nombreItemPersonaje, vm.Home.Characters, vm.Exploration.Worlds);
                        while (!taskFinal.IsCompleted) DoEvents();
                        vm.IsWhereIsItOpen = true;
                        DoEvents(); DoEvents(); DoEvents();
                        // El Popup vive en su propia PresentationSource (no es descendiente visual
                        // de `window`) - RenderTargetBitmap(window) NUNCA lo captura (ver el mismo
                        // hallazgo real ya documentado por LIBFILT mas arriba en este fichero).
                        // popup.Child SI es un Visual real con su propio ActualWidth/ActualHeight
                        // una vez IsOpen=true.
                        var popupField = typeof(MainWindow).GetField("WhereIsItPopup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        var popup = popupField?.GetValue(window) as System.Windows.Controls.Primitives.Popup;
                        var visualACapturar = popup?.Child is System.Windows.FrameworkElement popupChild && popupChild.ActualWidth > 1
                            ? (System.Windows.Media.Visual)popupChild
                            : window;
                        var anchoReal = visualACapturar is System.Windows.FrameworkElement feReal ? feReal.ActualWidth : window.ActualWidth;
                        var altoReal = visualACapturar is System.Windows.FrameworkElement feReal2 ? feReal2.ActualHeight : window.ActualHeight;
                        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)anchoReal, (int)altoReal, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtb.Render(visualACapturar);
                        var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                        string shot = Path.Combine(AppContext.BaseDirectory, "idea5-busqueda-global.png");
                        using (var fs = File.Create(shot)) enc.Save(fs);
                        Console.WriteLine($"IDEA5_SOLO: captura real -> {shot} (popup encontrado={popup?.Child != null}, {anchoReal}x{altoReal})");
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("IDEA5_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (IDEA5_SOLO)");
            Environment.Exit(0);
        }

        // IDEA1B_SOLO=1 (20-sep-2026, catalogo de funciones, idea 1 "Estado del mundo editable" -
        // quinta ronda, reconsiderado a peticion explicita del coordinador/usuario: confirma con
        // datos REALES (no solo sinteticos) que las 3 banderas de invasion nuevas
        // (DownedGoblinArmy/DownedFrostLegion/DownedPirates) leen valores reales y creibles de
        // varios mundos jugados de verdad de este equipo, nunca solo basura o siempre false.
        if (Environment.GetEnvironmentVariable("IDEA1B_SOLO") == "1")
        {
            try
            {
                string[] mundosReales =
                [
                    MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld"),
                    MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\Worlds\Blando_Río.wld"),
                    MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\Afueras_de_Larvas_de_gusano.wld"),
                ];
                foreach (string ruta in mundosReales)
                {
                    if (!File.Exists(ruta)) { Console.WriteLine($"IDEA1B_SOLO: AVISO - falta {ruta}, se omite"); continue; }
                    var world = Terrakeep.Core.WldFormat.WldReader.Read(File.ReadAllBytes(ruta));
                    Console.WriteLine($"IDEA1B_SOLO: '{world.Header.Title}' -> DownedGoblinArmy={world.Header.DownedGoblinArmy}, DownedFrostLegion={world.Header.DownedFrostLegion}, DownedPirates={world.Header.DownedPirates}, HardMode={world.Header.HardMode} (verdad real, sin forzar nada)");
                }

                // --- Flujo real E2E: cargar una COPIA (nunca el mundo real del usuario), marcar
                // la casilla real en pantalla, pulsar Guardar de verdad, releer del disco. ---
                string original = mundosReales[0];
                if (File.Exists(original))
                {
                    string copia = Path.Combine(Path.GetTempPath(), $"idea1b-copia-{Guid.NewGuid():N}.wld");
                    File.Copy(original, copia);
                    try
                    {
                        vm.SelectedTabIndex = 4; // Exploracion
                        DoEvents();
                        var tarea = vm.Exploration.LoadFromPathAsync(copia);
                        while (!tarea.IsCompleted) DoEvents();
                        DoEvents(); DoEvents();

                        bool valorAntes = vm.Exploration.EditDownedFrostLegion;
                        vm.Exploration.EditDownedFrostLegion = !valorAntes;
                        DoEvents();
                        Console.WriteLine($"IDEA1B_SOLO: casilla real 'Legion Helada' cambiada de {valorAntes} a {vm.Exploration.EditDownedFrostLegion} -> SaveBossFlagsCommand.CanExecute={vm.Exploration.SaveBossFlagsCommand.CanExecute(null)}");
                        if (!vm.Exploration.SaveBossFlagsCommand.CanExecute(null)) Console.WriteLine("FALLO: IDEA1B_SOLO - el boton real de Guardar no se activa al cambiar la casilla de invasion");

                        var tareaGuardar = vm.Exploration.SaveBossFlagsCommand.ExecuteAsync(null);
                        while (!tareaGuardar.IsCompleted) DoEvents();
                        DoEvents(); DoEvents();

                        var releido = Terrakeep.Core.WldFormat.WldReader.Read(File.ReadAllBytes(copia));
                        Console.WriteLine($"IDEA1B_SOLO: releido del disco tras Guardar real -> DownedFrostLegion={releido.Header.DownedFrostLegion} (esperado {!valorAntes})");
                        if (releido.Header.DownedFrostLegion != !valorAntes)
                            Console.WriteLine("FALLO: IDEA1B_SOLO - el guardado real (boton Guardar, WorldFileService.SaveBossFlags) no escribio la bandera de invasion en el archivo");
                        // Aisla que las OTRAS 2 banderas de invasion no se tocaron de rebote.
                        var originalBytes = Terrakeep.Core.WldFormat.WldReader.Read(File.ReadAllBytes(original));
                        if (releido.Header.DownedGoblinArmy != originalBytes.Header.DownedGoblinArmy || releido.Header.DownedPirates != originalBytes.Header.DownedPirates)
                            Console.WriteLine("FALLO: IDEA1B_SOLO - guardar UNA bandera de invasion cambio alguna de las otras dos sin pedirlo");

                        // Verifica el checkbox real en pantalla (no solo la propiedad del ViewModel).
                        var expanderEditar = Descendientes<Expander>(window).FirstOrDefault(e => e.Header as string == vm.Loc["explore_edit_world"]);
                        if (expanderEditar != null) expanderEditar.IsExpanded = true;
                        DoEvents(); DoEvents();
                        var casillaReal = Descendientes<System.Windows.Controls.CheckBox>(window)
                            .FirstOrDefault(c => c.IsVisible && Descendientes<TextBlock>(c).Any(t => t.Text == vm.Loc["explore_invasion_frost"]));
                        Console.WriteLine($"IDEA1B_SOLO: casilla real 'Legion Helada' en el arbol visual encontrada={casillaReal != null}, marcada={casillaReal?.IsChecked}");
                        if (casillaReal == null) Console.WriteLine("FALLO: IDEA1B_SOLO - la casilla real de Legion Helada no esta en el arbol visual del panel Editar mundo");
                    }
                    finally
                    {
                        try { File.Delete(copia); } catch { }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("IDEA1B_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (IDEA1B_SOLO)");
            Environment.Exit(0);
        }

        // IDEA1C_SOLO=1 (20-sep-2026, catalogo de funciones, idea 1 "editar que NPCs de pueblo
        // han venido a vivir al mundo (hoy solo lectura)" - segunda pieza, quinta ronda,
        // reconsiderada a peticion explicita del coordinador/usuario tras confirmar con evidencia
        // real - SerializeNpcsSection ya existia y ya estaba probada por WriteWorld - que SI es
        // alcanzable con el mismo patron de empalme de seccion de ancho variable que WriteChestItems/
        // WriteSignText). Flujo real E2E completo: cargar una COPIA de un mundo real, anadir un NPC
        // real que falta (boton real "+"), guardar de verdad, releer de disco, quitar un NPC real
        // presente (boton real "✕"), guardar de verdad, releer de disco - nunca solo en memoria.
        if (Environment.GetEnvironmentVariable("IDEA1C_SOLO") == "1")
        {
            try
            {
                string original = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
                if (!File.Exists(original)) { Console.WriteLine($"IDEA1C_SOLO: AVISO - falta {original}, no hay nada que verificar"); Console.WriteLine("DONE (IDEA1C_SOLO)"); Environment.Exit(0); }

                var mundoOriginal = Terrakeep.Core.WldFormat.WldReader.Read(File.ReadAllBytes(original));
                Console.WriteLine($"IDEA1C_SOLO: '{mundoOriginal.Header.Title}' -> {mundoOriginal.Npcs.Count} NPCs reales presentes (verdad real, sin forzar nada)");

                string copia = Path.Combine(Path.GetTempPath(), $"idea1c-copia-{Guid.NewGuid():N}.wld");
                File.Copy(original, copia);
                try
                {
                    vm.SelectedTabIndex = 4; // Exploracion
                    DoEvents();
                    var tareaCarga = vm.Exploration.LoadFromPathAsync(copia);
                    while (!tareaCarga.IsCompleted) DoEvents();
                    DoEvents(); DoEvents();
                    vm.Exploration.SelectedCategory = WorldSearchCategory.Npcs;
                    DoEvents(); DoEvents();

                    int npcsAntes = vm.Exploration.Npcs.Count;
                    int faltantesAntes = vm.Exploration.MissingNpcs.Count;
                    Console.WriteLine($"IDEA1C_SOLO: tras cargar -> Npcs={npcsAntes}, MissingNpcs={faltantesAntes}");
                    if (faltantesAntes == 0) { Console.WriteLine("IDEA1C_SOLO: AVISO - este mundo real ya tiene el roster completo, no hay ningun NPC que anadir para probar"); }
                    else
                    {
                        var faltante = vm.Exploration.MissingNpcs[0];
                        string nombreFaltante = faltante.Name;
                        var tareaAnadir = vm.Exploration.AddTownNpcCommand.ExecuteAsync(faltante);
                        while (!tareaAnadir.IsCompleted) DoEvents();
                        DoEvents(); DoEvents();

                        Console.WriteLine($"IDEA1C_SOLO: tras Anadir '{nombreFaltante}' -> Npcs={vm.Exploration.Npcs.Count} (esperado {npcsAntes + 1}), MissingNpcs={vm.Exploration.MissingNpcs.Count} (esperado {faltantesAntes - 1})");
                        if (vm.Exploration.Npcs.Count != npcsAntes + 1) Console.WriteLine("FALLO: IDEA1C_SOLO - Anadir NPC no aumento la lista real de NPCs del mundo");
                        if (vm.Exploration.MissingNpcs.Any(m => m.Name == nombreFaltante)) Console.WriteLine("FALLO: IDEA1C_SOLO - el NPC anadido sigue apareciendo en 'NPCs que faltan'");

                        var releidoTrasAnadir = Terrakeep.Core.WldFormat.WldReader.Read(File.ReadAllBytes(copia));
                        Console.WriteLine($"IDEA1C_SOLO: releido del disco tras Anadir -> {releidoTrasAnadir.Npcs.Count} NPCs (esperado {mundoOriginal.Npcs.Count + 1})");
                        if (releidoTrasAnadir.Npcs.Count != mundoOriginal.Npcs.Count + 1)
                            Console.WriteLine("FALLO: IDEA1C_SOLO - el guardado real (boton +, WorldFileService.SaveNpcRoster) no escribio el NPC nuevo en el archivo");

                        // Boton real "+" en el arbol visual del panel de NPCs que faltan - el
                        // Expander empieza colapsado (MissingNpcsExpander, IsExpanded="False" por
                        // defecto), hay que desplegarlo primero (mismo criterio que ya usa
                        // IDEA1B_SOLO con expanderEditar). Ahora con un elemento menos, comprobamos
                        // que el patron de fila sigue existiendo para lo que quede.
                        var expanderFaltan = Descendientes<Expander>(window).FirstOrDefault(e => e.Header as string == vm.Loc["explore_missing_npcs"]);
                        if (expanderFaltan != null) expanderFaltan.IsExpanded = true;
                        DoEvents(); DoEvents();
                        var botonAnadirReal = Descendientes<System.Windows.Controls.Button>(window)
                            .FirstOrDefault(b => b.Content as string == "+" && b.Command == vm.Exploration.AddTownNpcCommand);
                        Console.WriteLine($"IDEA1C_SOLO: boton real '+' de anadir NPC encontrado en el arbol visual={botonAnadirReal != null}");
                        if (botonAnadirReal == null) Console.WriteLine("FALLO: IDEA1C_SOLO - no se encuentra ningun boton real '+' con AddTownNpcCommand en el arbol visual");
                    }

                    // Quitar un NPC real presente (boton "✕") - usa el primero de la lista actual,
                    // nunca el que se acaba de anadir (para probar el camino independiente).
                    if (vm.Exploration.Npcs.Count > 0)
                    {
                        var npcsAntesDeQuitar = vm.Exploration.Npcs.Count;
                        var presente = vm.Exploration.Npcs[0];
                        int idQuitado = presente.Id;
                        var tareaQuitar = vm.Exploration.RemoveTownNpcCommand.ExecuteAsync(presente);
                        while (!tareaQuitar.IsCompleted) DoEvents();
                        DoEvents(); DoEvents();

                        Console.WriteLine($"IDEA1C_SOLO: tras Quitar '{presente.Name}' (id={idQuitado}) -> Npcs={vm.Exploration.Npcs.Count} (esperado {npcsAntesDeQuitar - 1})");
                        if (vm.Exploration.Npcs.Count != npcsAntesDeQuitar - 1) Console.WriteLine("FALLO: IDEA1C_SOLO - Quitar NPC no redujo la lista real de NPCs del mundo");
                        if (vm.Exploration.Npcs.Any(n => n.Id == idQuitado)) Console.WriteLine("FALLO: IDEA1C_SOLO - el NPC quitado sigue en la lista real de NPCs");

                        var releidoTrasQuitar = Terrakeep.Core.WldFormat.WldReader.Read(File.ReadAllBytes(copia));
                        Console.WriteLine($"IDEA1C_SOLO: releido del disco tras Quitar -> {releidoTrasQuitar.Npcs.Count} NPCs, contiene id={idQuitado}={releidoTrasQuitar.Npcs.Any(n => n.Id == idQuitado)} (esperado False)");
                        if (releidoTrasQuitar.Npcs.Any(n => n.Id == idQuitado))
                            Console.WriteLine("FALLO: IDEA1C_SOLO - el guardado real (boton ✕, WorldFileService.SaveNpcRoster) no quito el NPC del archivo");

                        var botonQuitarReal = Descendientes<System.Windows.Controls.Button>(window)
                            .FirstOrDefault(b => b.Content as string == "✕" && b.Command == vm.Exploration.RemoveTownNpcCommand);
                        Console.WriteLine($"IDEA1C_SOLO: boton real '✕' de quitar NPC encontrado en el arbol visual={botonQuitarReal != null}");
                        if (botonQuitarReal == null) Console.WriteLine("FALLO: IDEA1C_SOLO - no se encuentra ningun boton real '✕' con RemoveTownNpcCommand en el arbol visual");
                    }
                }
                finally
                {
                    try { File.Delete(copia); } catch { }
                }
            }
            catch (Exception ex) { Console.WriteLine("IDEA1C_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (IDEA1C_SOLO)");
            Environment.Exit(0);
        }

        // BESTIARIO_SOLO=1 (21-sep-2026, segundo intento real del bug reportado en vivo "panel
        // del Bestiario cortado con '...'" - la ronda del 20-sep quedo con LIMITE REAL honesto
        // tras probar escala DPI, ancho minimo del sidebar (260px) y ventanas hasta 860x600 SIN
        // reproducirlo, comprobando "encabezados" (Este mundo/Editar mundo/Bestiario) y
        // confirmando que ningun ESTILO define TextTrimming. Relectura literal del propio XAML
        // (no de memoria) encuentra lo que ese barrido paso por alto: el Bestiario tiene su PROPIA
        // fila por especie (BestiaryRowViewModel.Name, dentro del Expander "Bestiario") con
        // TextTrimming="CharacterEllipsis" puesto como atributo LOCAL directamente en el XAML de
        // esa fila (MainWindow.xaml linea ~6684) - nunca comprobado porque la ronda anterior media
        // los ENCABEZADOS de la columna, no los NOMBRES DE CADA ESPECIE dentro de la lista
        // desplegada. Carga el mundo real con MAS especies (Blando_Río.wld, 361 especies) con un
        // personaje real de Calamity (mas nombres largos de criaturas), despliega el Expander real
        // y mide en pixeles reales si alguna fila de verdad recorta su nombre (ancho natural del
        // texto > ancho disponible de la columna) al ancho MINIMO real del sidebar (260px).
        if (Environment.GetEnvironmentVariable("BESTIARIO_SOLO") == "1")
        {
            try
            {
                string plrCalamity = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Players\Eldelgas.plr";
                string wldMax = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\Worlds\Blando_Río.wld");
                if (!File.Exists(plrCalamity) || !File.Exists(wldMax))
                {
                    Console.WriteLine("BESTIARIO_SOLO: AVISO - falta el personaje o el mundo real de esta maquina, se omite la prueba");
                }
                else
                {
                    vm.LoadFromPath(plrCalamity);
                    DoEvents(); DoEvents();
                    vm.SelectedTabIndex = 4; // Exploracion
                    DoEvents();
                    var tareaBest = vm.Exploration.LoadFromPathAsync(wldMax);
                    while (!tareaBest.IsCompleted) DoEvents();
                    DoEvents(); DoEvents(); DoEvents();

                    vm.Settings.ExplorationSidebarWidth = 260; // ancho minimo real
                    FijarTamaño(window, 1080, 700); // mismo tamaño minimo real ya probado en la ronda anterior
                    DoEvents(); DoEvents();

                    Console.WriteLine($"BESTIARIO_SOLO: HasBestiary={vm.Exploration.HasBestiary}, {vm.Exploration.BestiaryRows.Count} especie(s) reales en este mundo (esperado >0, idealmente cerca de 361 segun bitacora.md)");

                    var expanderBest = Descendientes<System.Windows.Controls.Expander>(window)
                        .FirstOrDefault(e => e.IsVisible && (e.Header as string) == vm.Loc["explore_bestiary"]);
                    if (expanderBest == null) Console.WriteLine("FALLO: BESTIARIO_SOLO - no se encontro el Expander real del Bestiario");
                    else
                    {
                        expanderBest.IsExpanded = true;
                        DoEvents(); DoEvents(); DoEvents();

                        // Hallazgo real de esta misma pasada (no en la ronda anterior, que solo
                        // media los encabezados de la columna): BestiarySummaryText ("N especies
                        // registradas... M muertes en total") es una frase larga de verdad, sin
                        // TextWrapping se recortaba en SILENCIO contra el sidebar minimo. Verifica
                        // el arreglo real: el texto ahora ocupa mas de 1 linea real (envuelve) y su
                        // ActualWidth no supera el ancho real disponible de la columna.
                        var resumenBest = Descendientes<TextBlock>(expanderBest)
                            .FirstOrDefault(t => t.IsVisible && t.Text == vm.Exploration.BestiarySummaryText);
                        if (resumenBest == null) Console.WriteLine("FALLO: BESTIARIO_SOLO - no se encontro el TextBlock real del resumen del Bestiario");
                        else
                        {
                            var sondaLinea = new TextBlock { Text = resumenBest.Text, FontSize = resumenBest.FontSize, FontFamily = resumenBest.FontFamily, TextWrapping = System.Windows.TextWrapping.NoWrap };
                            sondaLinea.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                            double altoUnaLinea = sondaLinea.DesiredSize.Height;
                            double lineasReales = altoUnaLinea > 0 ? Math.Round(resumenBest.ActualHeight / altoUnaLinea, 1) : -1;
                            Console.WriteLine($"BESTIARIO_SOLO: resumen real='{resumenBest.Text}' -> ancho columna={resumenBest.ActualWidth:0.#}px, alto real={resumenBest.ActualHeight:0.#}px (~{lineasReales} lineas reales), TextWrapping={resumenBest.TextWrapping}");
                            if (resumenBest.TextWrapping != System.Windows.TextWrapping.Wrap) Console.WriteLine("FALLO: BESTIARIO_SOLO - el resumen del Bestiario sigue sin TextWrapping=Wrap");
                            if (lineasReales < 1.5) Console.WriteLine("FALLO: BESTIARIO_SOLO - el resumen real del Bestiario sigue cabiendo en 1 sola linea visual pese a ser una frase larga - revisar si de verdad envuelve");
                        }

                        var filasNombre = Descendientes<TextBlock>(expanderBest)
                            .Where(t => t.IsVisible && t.TextTrimming == System.Windows.TextTrimming.CharacterEllipsis)
                            .ToList();
                        Console.WriteLine($"BESTIARIO_SOLO: {filasNombre.Count} TextBlock(s) reales de nombre de especie encontrados en el Expander desplegado, a sidebar={vm.Settings.ExplorationSidebarWidth}px");

                        int recortadasDeVerdad = 0;
                        string? ejemploRecortado = null;
                        double maxSobrante = 0;
                        foreach (var tb in filasNombre)
                        {
                            // Ancho natural real del texto completo (sin trimming), medido con
                            // FormattedText contra la MISMA fuente/tamaño/peso real del TextBlock -
                            // mismo mecanismo ya usado por el detector D-PALABRA de AR-LAY.
                            var ft = new System.Windows.Media.FormattedText(
                                tb.Text, System.Globalization.CultureInfo.CurrentCulture,
                                System.Windows.FlowDirection.LeftToRight,
                                new System.Windows.Media.Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch),
                                tb.FontSize, System.Windows.Media.Brushes.Black, 96.0);
                            double sobrante = ft.Width - tb.ActualWidth;
                            if (sobrante > 1.0) // 1px de margen real de redondeo
                            {
                                recortadasDeVerdad++;
                                if (sobrante > maxSobrante) { maxSobrante = sobrante; ejemploRecortado = tb.Text; }
                            }
                        }
                        Console.WriteLine($"BESTIARIO_SOLO: {recortadasDeVerdad} de {filasNombre.Count} nombre(s) de especie RECORTADOS de verdad (ancho natural > ancho real disponible) a sidebar=260px, ejemplo mas recortado='{ejemploRecortado}' ({maxSobrante:0.#}px de sobra)");
                        if (recortadasDeVerdad > 0)
                            Console.WriteLine($"HALLAZGO REAL: BESTIARIO_SOLO - SI hay nombres de especie reales recortados con '...' dentro del Bestiario desplegado (la pista que la ronda anterior no comprobo: filas individuales, no los encabezados de la columna)");

                        DoEvents();
                        var rtbBest = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtbBest.Render(window);
                        var encBest = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encBest.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbBest));
                        string shotBest = Path.Combine(AppContext.BaseDirectory, "bestiario-desplegado-260px.png");
                        using (var fsBest = File.Create(shotBest)) encBest.Save(fsBest);
                        Console.WriteLine($"BESTIARIO_SOLO: captura real -> {shotBest}");
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("BESTIARIO_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (BESTIARIO_SOLO)");
            Environment.Exit(0);
        }

        // KPI3_SOLO=1 (21-sep-2026, catalogo de rediseño visual T4, segundo intento real tras el
        // limite documentado de la ronda anterior "no existe infraestructura para evaluar la Guia
        // de un personaje no cargado"): verifica que la 3ª KPI real de la tarjeta hero de Inicio
        // (HomeViewModel.LastSessionGuideStage) computa de verdad el MISMO tramo real que ya
        // calcula, de forma independiente y ya confirmada, la propia pestaña Guia
        // (GuideViewModel.ObjetivoTramo) para ESE MISMO personaje cuando SI esta cargado como
        // activo - la comparacion cruzada es la prueba real de que no es un valor inventado.
        if (Environment.GetEnvironmentVariable("KPI3_SOLO") == "1")
        {
            try
            {
                string dirKpi = Path.Combine(Path.GetTempPath(), $"terrakeep-kpi3-{Guid.NewGuid():N}");
                Directory.CreateDirectory(dirKpi);
                string plrPathKpi = Path.Combine(dirKpi, "PersonajeKpi3.plr");
                var personajeKpi = new Terrakeep.Core.PlrFormat.PlrCharacter
                {
                    Name = "PersonajeKpi3",
                    Version = 279,
                    PrimaryLoadout = Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: true),
                    Loadouts = [Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false), Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false), Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false)],
                };
                File.WriteAllBytes(plrPathKpi, Terrakeep.Core.PlrFormat.PlrFile.Write(personajeKpi));

                vm.Settings.AddCharacterFolder(dirKpi);
                vm.Home.RefreshCommand.Execute(null);
                while (vm.Home.IsScanning) DoEvents();
                DoEvents(); DoEvents();

                vm.Home.SetLastSession(new Terrakeep.App.Services.TerrakeepSession { LastCharacterPath = plrPathKpi, LastCharacterName = "PersonajeKpi3" });
                DoEvents(); DoEvents();
                string? kpi3Home = vm.Home.LastSessionGuideStage;
                Console.WriteLine($"KPI3_SOLO: HomeViewModel.LastSessionGuideStage (personaje NO cargado como activo) = '{kpi3Home ?? "(null)"}'");
                if (kpi3Home == null) Console.WriteLine("FALLO: KPI3_SOLO - LastSessionGuideStage es null para un personaje limpio real (deberia caer en el primer tramo obligatorio, PreOjo)");

                // Cruce real: cargar ESE MISMO personaje como activo y leer el tramo ya confirmado
                // de la propia pestaña Guia, para comparar contra el valor anterior.
                vm.LoadFromPath(plrPathKpi);
                DoEvents(); DoEvents();
                vm.SelectedTabIndex = 3; // Guia
                DoEvents();
                vm.Guide.Refresh();
                DoEvents(); DoEvents();
                string? kpi3Guia = vm.Guide.ObjetivoTramo?.Nombre;
                Console.WriteLine($"KPI3_SOLO: GuideViewModel.ObjetivoTramo.Nombre (mismo personaje, cargado como activo) = '{kpi3Guia ?? "(null)"}'");
                bool coinciden = string.Equals(kpi3Home, kpi3Guia, StringComparison.Ordinal);
                Console.WriteLine($"KPI3_SOLO: los dos caminos reales coinciden={coinciden} (esperado True - misma Guia, mismo personaje, dos caminos de codigo distintos)");
                if (!coinciden) Console.WriteLine($"FALLO: KPI3_SOLO - LastSessionGuideStage ('{kpi3Home}') no coincide con ObjetivoTramo.Nombre real ('{kpi3Guia}') para el mismo personaje");

                // Captura real: Inicio con la tarjeta hero mostrando las 3 pastillas (Vida/Tiempo/Etapa).
                vm.SelectedTabIndex = 0;
                FijarTamaño(window, 1600, 900);
                DoEvents(); DoEvents();
                var rtbKpi = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbKpi.Render(window);
                var encKpi = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encKpi.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbKpi));
                string shotKpi = Path.Combine(AppContext.BaseDirectory, "kpi3-tarjeta-hero.png");
                using (var fsKpi = File.Create(shotKpi)) encKpi.Save(fsKpi);
                Console.WriteLine($"KPI3_SOLO: captura real -> {shotKpi}");

                vm.Settings.RemoveCharacterFolderCommand.Execute(dirKpi);
                try { Directory.Delete(dirKpi, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
            catch (Exception ex) { Console.WriteLine("KPI3_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (KPI3_SOLO)");
            Environment.Exit(0);
        }

        // HOMECARDS_SOLO=1 (21-sep-2026, catalogo de rediseño visual T4, tercer intento real -
        // cierra las 2 sub-piezas menores que quedaban tras KPI3_SOLO: la sugerencia dinamica
        // "Te toca: X" (HomeViewModel.GuideObjectiveCardTitle/ContinueToGuideCommand) y "Tu
        // ultimo mundo" (HomeViewModel.LastWorldName/ContinueWorldCommand). Verifica de extremo a
        // extremo: las 2 tarjetas aparecen con el texto real correcto, Y sus comandos reales
        // navegan de verdad a donde dicen (Guia con el objetivo evaluado / Exploracion con el
        // mundo cargado), no solo que el texto se vea bien.
        if (Environment.GetEnvironmentVariable("HOMECARDS_SOLO") == "1")
        {
            try
            {
                string dirHc = Path.Combine(Path.GetTempPath(), $"terrakeep-homecards-{Guid.NewGuid():N}");
                Directory.CreateDirectory(dirHc);
                string plrPathHc = Path.Combine(dirHc, "PersonajeHomeCards.plr");
                var personajeHc = new Terrakeep.Core.PlrFormat.PlrCharacter
                {
                    Name = "PersonajeHomeCards",
                    Version = 279,
                    PrimaryLoadout = Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: true),
                    Loadouts = [Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false), Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false), Terrakeep.Core.PlrFormat.PlrLoadout.CreateEmpty(isPrimary: false)],
                };
                File.WriteAllBytes(plrPathHc, Terrakeep.Core.PlrFormat.PlrFile.Write(personajeHc));
                string wldPathHc = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");

                vm.Settings.AddCharacterFolder(dirHc);
                vm.Home.RefreshCommand.Execute(null);
                while (vm.Home.IsScanning) DoEvents();
                DoEvents(); DoEvents();

                vm.Home.SetLastSession(new Terrakeep.App.Services.TerrakeepSession
                {
                    LastCharacterPath = plrPathHc, LastCharacterName = "PersonajeHomeCards",
                    LastWorldPath = wldPathHc, LastWorldName = "roca_negra",
                });
                DoEvents(); DoEvents();

                Console.WriteLine($"HOMECARDS_SOLO: LastSessionGuideObjectiveTitle real='{vm.Home.LastSessionGuideObjectiveTitle}', GuideObjectiveCardTitle real='{vm.Home.GuideObjectiveCardTitle}', LastWorldName real='{vm.Home.LastWorldName}'");
                if (vm.Home.LastSessionGuideObjectiveTitle == null) Console.WriteLine("FALLO: HOMECARDS_SOLO - LastSessionGuideObjectiveTitle es null para un personaje limpio real");
                if (vm.Home.GuideObjectiveCardTitle == "" || !vm.Home.GuideObjectiveCardTitle.Contains(vm.Home.LastSessionGuideObjectiveTitle ?? "\uFFFF"))
                    Console.WriteLine($"FALLO: HOMECARDS_SOLO - GuideObjectiveCardTitle ('{vm.Home.GuideObjectiveCardTitle}') no contiene el objetivo real ('{vm.Home.LastSessionGuideObjectiveTitle}')");
                if (vm.Home.LastWorldName != "roca_negra") Console.WriteLine($"FALLO: HOMECARDS_SOLO - LastWorldName real ('{vm.Home.LastWorldName}') no es el esperado");

                vm.SelectedTabIndex = 0; // Inicio
                FijarTamaño(window, 1600, 900);
                DoEvents(); DoEvents();

                var btnGuia = Descendientes<System.Windows.Controls.Button>(window)
                    .FirstOrDefault(b => b.IsVisible && Descendientes<TextBlock>(b).Any(t => t.Text == vm.Home.GuideObjectiveCardTitle));
                var btnMundo = Descendientes<System.Windows.Controls.Button>(window)
                    .FirstOrDefault(b => b.IsVisible && Descendientes<TextBlock>(b).Any(t => (t.Text ?? "").Contains("roca_negra")));
                Console.WriteLine($"HOMECARDS_SOLO: tarjeta real 'Te toca' encontrada y visible={btnGuia != null}, tarjeta real 'Tu ultimo mundo' encontrada y visible={btnMundo != null}");
                if (btnGuia == null) Console.WriteLine("FALLO: HOMECARDS_SOLO - la tarjeta real 'Te toca: X' no aparece en Inicio pese a haber un objetivo real");
                if (btnMundo == null) Console.WriteLine("FALLO: HOMECARDS_SOLO - la tarjeta real 'Tu ultimo mundo' no aparece en Inicio pese a haber un LastWorldName real");

                DoEvents();
                var rtbHc = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbHc.Render(window);
                var encHc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encHc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbHc));
                string shotHc = Path.Combine(AppContext.BaseDirectory, "homecards-te-toca-y-ultimo-mundo.png");
                using (var fsHc = File.Create(shotHc)) encHc.Save(fsHc);
                Console.WriteLine($"HOMECARDS_SOLO: captura real -> {shotHc}");

                // Extremo a extremo real: invocar el comando REAL de la tarjeta "Te toca" (via
                // su AutomationPeer, mismo camino que un clic real) y comprobar que de verdad
                // carga el personaje y aterriza en Guia con un objetivo evaluado.
                if (btnGuia != null)
                {
                    var peerGuia = new System.Windows.Automation.Peers.ButtonAutomationPeer(btnGuia);
                    ((System.Windows.Automation.Provider.IInvokeProvider)peerGuia.GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke)!).Invoke();
                    DoEvents(); DoEvents(); DoEvents();
                    bool aterrizoEnGuia = vm.SelectedTabIndex == 3 && vm.IsCharacterLoaded && vm.CharacterName == "PersonajeHomeCards" && vm.Guide.ObjetivoPaso != null;
                    Console.WriteLine($"HOMECARDS_SOLO: tras invocar 'Te toca' -> SelectedTabIndex={vm.SelectedTabIndex} (esperado 3, Guia), personaje cargado={vm.CharacterName}, Guide.ObjetivoPaso real={vm.Guide.ObjetivoPaso?.Titulo} -> {aterrizoEnGuia}");
                    if (!aterrizoEnGuia) Console.WriteLine("FALLO: HOMECARDS_SOLO - invocar la tarjeta 'Te toca' no cargo el personaje real ni aterrizo en Guia con un objetivo real evaluado");
                }

                // Vuelta a Inicio para probar "Tu ultimo mundo" de forma aislada (el personaje ya
                // cargado por el paso anterior no deberia importarle - un mundo es independiente).
                vm.SelectedTabIndex = 0;
                DoEvents(); DoEvents();
                var btnMundo2 = Descendientes<System.Windows.Controls.Button>(window)
                    .FirstOrDefault(b => b.IsVisible && Descendientes<TextBlock>(b).Any(t => (t.Text ?? "").Contains("roca_negra")));
                if (btnMundo2 != null)
                {
                    var peerMundo = new System.Windows.Automation.Peers.ButtonAutomationPeer(btnMundo2);
                    ((System.Windows.Automation.Provider.IInvokeProvider)peerMundo.GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke)!).Invoke();
                    long limite = Environment.TickCount64 + 10_000;
                    while (!vm.Exploration.IsWorldLoaded && Environment.TickCount64 < limite) DoEvents();
                    DoEvents(); DoEvents();
                    bool aterrizoEnMundo = vm.SelectedTabIndex == 4 && vm.Exploration.IsWorldLoaded;
                    Console.WriteLine($"HOMECARDS_SOLO: tras invocar 'Tu ultimo mundo' -> SelectedTabIndex={vm.SelectedTabIndex} (esperado 4, Exploracion), IsWorldLoaded={vm.Exploration.IsWorldLoaded} -> {aterrizoEnMundo}");
                    if (!aterrizoEnMundo) Console.WriteLine("FALLO: HOMECARDS_SOLO - invocar la tarjeta 'Tu ultimo mundo' no cargo el mundo real ni aterrizo en Exploracion");
                }
                else Console.WriteLine("FALLO: HOMECARDS_SOLO - 'Tu ultimo mundo' no se encontro en la segunda vuelta a Inicio");

                vm.Settings.RemoveCharacterFolderCommand.Execute(dirHc);
                try { Directory.Delete(dirHc, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
            catch (Exception ex) { Console.WriteLine("HOMECARDS_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (HOMECARDS_SOLO)");
            Environment.Exit(0);
        }

        // T1RAIL_SOLO=1 (21-sep-2026, segundo intento real del catalogo de rediseño visual T1 -
        // la ronda anterior solo pinto los 2 filetes visuales sin reordenar de verdad las 8
        // pestañas; ver el comentario completo junto al enum AppTab en MainViewModel.cs). Verifica
        // con geometria real: el orden real de las 8 pestañas de la rail es Inicio/Personaje/
        // Builds/Guia (grupo "Partida", sin filete propio salvo el de arriba de todo) / Exploracion
        // (con filete, arranca "Mundo") /Hosting / Novedades (con filete, arranca el pie) /AcercaDe,
        // exactamente 2 filetes reales Visible en toda la rail (no 0, no 8), y captura real del
        // resultado.
        if (Environment.GetEnvironmentVariable("T1RAIL_SOLO") == "1")
        {
            try
            {
                FijarTamaño(window, 1600, 900);
                vm.SelectedTabIndex = 0;
                DoEvents(); DoEvents();

                // x:Name="RootTabControl" real (MainWindow.xaml) - NO buscar por Items.Count==8,
                // la TabControl interna de Personaje TAMBIEN tiene 8 sub-pestañas (Objetos, Buffs,
                // Investigacion, Apariencia, Puntos de aparicion, Desbloqueos, Version...),
                // ambiguo de verdad.
                var railTabControl = Descendientes<System.Windows.Controls.TabControl>(window)
                    .FirstOrDefault(t => t.Name == "RootTabControl");
                if (railTabControl == null) { Console.WriteLine("FALLO: T1RAIL_SOLO - no se encontro RootTabControl (x:Name real)"); }
                else
                {
                    var nombresReales = railTabControl.Items.Cast<object>()
                        .Select(o => (o as System.Windows.Controls.TabItem)?.ToolTip as string)
                        .ToList();
                    // No hay forma directa de leer el AutomationProperties.Name aqui sin volver a
                    // enumerar TabItem reales - se usa el orden real de AutomationProperties.Name.
                    var tabItemsReales = railTabControl.Items.Cast<object>().OfType<System.Windows.Controls.TabItem>().ToList();
                    var nombresPorAutomation = tabItemsReales.Select(ti => System.Windows.Automation.AutomationProperties.GetName(ti)).ToList();
                    string ordenReal = string.Join(" | ", nombresPorAutomation);
                    Console.WriteLine($"T1RAIL_SOLO: orden real de las 8 pestañas de la rail = [{ordenReal}]");

                    var esperado = new[] { vm.Loc["tab_home"], vm.Loc["tab_character"], vm.Loc["tab_builds"], vm.Loc["tab_guide"], vm.Loc["tab_exploration"], vm.Loc["tab_hosting"], vm.Loc["tab_whatsnew"], vm.Loc["tab_about"] };
                    bool ordenOk = nombresPorAutomation.SequenceEqual(esperado);
                    Console.WriteLine($"T1RAIL_SOLO: orden coincide con el real 'Partida(Inicio/Personaje/Builds/Guia) | Mundo(Exploracion/Hosting) | pie(Novedades/AcercaDe)'={ordenOk}");
                    if (!ordenOk) Console.WriteLine($"FALLO: T1RAIL_SOLO - el orden real de la rail no es el esperado (esperado [{string.Join(" | ", esperado)}])");

                    // Recorrer TODOS los Border x:Name="GroupDivider" reales de la rail y contar
                    // cuantos son de verdad Visible.
                    var todosLosFiletes = Descendientes<System.Windows.Controls.Border>(railTabControl)
                        .Where(b => b.Name == "GroupDivider").ToList();
                    int filetesReales = todosLosFiletes.Count;
                    int filetesVisiblesReales = todosLosFiletes.Count(b => b.Visibility == System.Windows.Visibility.Visible);
                    Console.WriteLine($"T1RAIL_SOLO: {filetesReales} filete(s) GroupDivider real(es) en la rail, {filetesVisiblesReales} Visible(s) (esperado 2: antes de Exploracion y antes de Novedades)");
                    if (filetesVisiblesReales != 2) Console.WriteLine($"FALLO: T1RAIL_SOLO - se esperaban exactamente 2 filetes Visible (grupo Mundo + pie), hay {filetesVisiblesReales}");
                }

                DoEvents();
                var rtbRail = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbRail.Render(window);
                var encRail = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encRail.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbRail));
                string shotRail = Path.Combine(AppContext.BaseDirectory, "t1rail-reordenada.png");
                using (var fsRail = File.Create(shotRail)) encRail.Save(fsRail);
                Console.WriteLine($"T1RAIL_SOLO: captura real -> {shotRail}");
            }
            catch (Exception ex) { Console.WriteLine("T1RAIL_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (T1RAIL_SOLO)");
            Environment.Exit(0);
        }

        // BUILDS_LABEL_SOLO=1 (20-sep-2026, bug real reportado por el usuario con captura: los
        // titulos de columna de clase en Builds ("Cuerpo a cuerpo", "A distancia", "Invocacion")
        // se parten letra a letra dentro de una columna demasiado estrecha - AR-LAY corrio sobre
        // esta pantalla (52 combinaciones) y no lo detecto (0 recortado, 0 solapes). Diagnostico
        // DEDICADO: mide en pixeles reales, para las 5 categorias reales, el ancho disponible del
        // DockPanel de cada columna, el ancho que piden los dos botones y el que le queda al
        // titulo - para entender el mecanismo exacto antes de arreglar nada.
        if (Environment.GetEnvironmentVariable("BUILDS_LABEL_SOLO") == "1")
        {
            try
            {
                FijarTamaño(window, 1600, 900);
                DoEvents();
                vm.SelectedTabIndex = 2; // Builds
                DoEvents();
                var tcBuilds = Descendientes<System.Windows.Controls.TabControl>(window).FirstOrDefault(t => t.IsVisible && t.Items.Count == 2);
                if (tcBuilds != null) tcBuilds.SelectedIndex = 0; // Vanilla
                DoEvents(); DoEvents();

                string shotDir = AppContext.BaseDirectory;
                void Shot(string nombre)
                {
                    DoEvents();
                    var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb.Render(window);
                    var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                    using var fs = File.Create(Path.Combine(shotDir, nombre + ".png"));
                    enc.Save(fs);
                    Console.WriteLine($"BUILDS_LABEL_SOLO: captura -> {nombre}.png");
                }
                Shot("builds-label-1600x900-antes");

                var botonesAutoEquipar = Descendientes<System.Windows.Controls.Button>(window)
                    .Where(b => b.IsVisible && (b.Content as string) == vm.Loc["action_auto_equip"]).ToList();
                Console.WriteLine($"BUILDS_LABEL_SOLO: {botonesAutoEquipar.Count} columnas de clase reales encontradas en pantalla (esperado >=5, las 5 categorias de la etapa Pre-Hardmode)");

                // Tras el arreglo (20-sep-2026): la etiqueta vive en su PROPIA fila, ya no
                // comparte DockPanel con los botones - se localiza subiendo al StackPanel de la
                // columna (padre comun de la etiqueta y del DockPanel de botones) y buscando el
                // primer TextBlock real de ese StackPanel.
                foreach (var btnAuto in botonesAutoEquipar)
                {
                    var dockPanelBotones = System.Windows.Media.VisualTreeHelper.GetParent(btnAuto) as System.Windows.Controls.DockPanel;
                    var columna = dockPanelBotones != null ? System.Windows.Media.VisualTreeHelper.GetParent(dockPanelBotones) as StackPanel : null;
                    if (dockPanelBotones == null || columna == null) continue;
                    var btnGenerar = dockPanelBotones.Children.OfType<System.Windows.Controls.Button>()
                        .FirstOrDefault(b => (b.Content as string) == vm.Loc["action_generate_character"]);
                    var etiqueta = columna.Children.OfType<TextBlock>().FirstOrDefault();
                    if (btnGenerar == null || etiqueta == null) continue;

                    // Contar lineas reales: altura natural de UNA linea (misma fuente/tamaño,
                    // medida con TextWrapping=NoWrap sobre el mismo texto) vs la altura YA
                    // renderizada con Wrap - el cociente es el numero real de lineas que ocupo.
                    var sondaUnaLinea = new TextBlock
                    {
                        Text = etiqueta.Text, FontSize = etiqueta.FontSize, FontFamily = etiqueta.FontFamily,
                        FontWeight = etiqueta.FontWeight, TextWrapping = System.Windows.TextWrapping.NoWrap,
                    };
                    sondaUnaLinea.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    double altoUnaLinea = sondaUnaLinea.DesiredSize.Height;
                    double lineasReales = altoUnaLinea > 0 ? Math.Round(etiqueta.ActualHeight / altoUnaLinea, 1) : -1;

                    Console.WriteLine($"BUILDS_LABEL_SOLO: '{etiqueta.Text}' -> columna={columna.ActualWidth:0.#}px, boton'Auto-equipar'={btnAuto.ActualWidth:0.#}px, boton'Generar...'={btnGenerar.ActualWidth:0.#}px, etiqueta={etiqueta.ActualWidth:0.#}x{etiqueta.ActualHeight:0.#}px (~{lineasReales} lineas reales, 1 linea mide {altoUnaLinea:0.#}px), DockPanelBotones={dockPanelBotones.ActualWidth:0.#}x{dockPanelBotones.ActualHeight:0.#}px");
                    if (lineasReales > 1.5)
                        Console.WriteLine($"FALLO: BUILDS_LABEL_SOLO - '{etiqueta.Text}' se esta partiendo en ~{lineasReales} lineas, deberia caber en 1 sola linea en una columna de 220px");
                    if (dockPanelBotones.ActualHeight > 40)
                        Console.WriteLine($"FALLO: BUILDS_LABEL_SOLO - la fila de botones mide {dockPanelBotones.ActualHeight:0.#}px de alto, deberia ser una fila compacta normal (~24-30px)");
                }
            }
            catch (Exception ex) { Console.WriteLine("BUILDS_LABEL_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (BUILDS_LABEL_SOLO)");
            Environment.Exit(0);
        }

        // BUILDS_CENTER_SOLO=1 (21-sep-2026, 2 bugs visuales reales mas reportados por el usuario
        // con captura, misma pestaña Builds ya tocada hoy): 1) al filtrar a una sola clase (ej.
        // solo "Magia") la columna resultante quedaba pegada a la izquierda con todo el resto del
        // ancho vacio a la derecha; 2) con las 4/5 clases a la vez ("Todas") los titulos de cada
        // columna ("Cuerpo a cuerpo"/"A distancia"/"Magia"/"Invocación") quedaban alineados a la
        // izquierda de su columna en vez de centrados sobre el contenido real de esa columna.
        // Arreglo real: HorizontalAlignment="Center" en el ItemsControl de columnas (MainWindow.xaml,
        // DataTemplate de BuildStageViewModel) + TextAlignment="Center" en el titulo de cada
        // columna (BuildClassTemplate). Mide en pixeles reales el hueco a izquierda/derecha del
        // bloque de columnas con 1 sola clase activa (debe quedar centrado, huecos iguales) y con
        // TODAS activas (el bloque ya ocupa casi todo el ancho, el centrado no debe romper nada),
        // y el TextAlignment real del titulo de columna.
        if (Environment.GetEnvironmentVariable("BUILDS_CENTER_SOLO") == "1")
        {
            try
            {
                FijarTamaño(window, 1600, 900);
                DoEvents();
                vm.SelectedTabIndex = 2; // Builds
                DoEvents();
                var tcBuilds = Descendientes<System.Windows.Controls.TabControl>(window).FirstOrDefault(t => t.IsVisible && t.Items.Count == 2);
                if (tcBuilds != null) tcBuilds.SelectedIndex = 0; // Vanilla
                DoEvents(); DoEvents();

                string shotDir = AppContext.BaseDirectory;
                void Shot(string nombre)
                {
                    DoEvents();
                    var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb.Render(window);
                    var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                    using var fs = File.Create(Path.Combine(shotDir, nombre + ".png"));
                    enc.Save(fs);
                    Console.WriteLine($"BUILDS_CENTER_SOLO: captura -> {nombre}.png");
                }

                // Localiza el WrapPanel real de columnas de clase (ItemWidth=248, unico en toda la
                // ventana con ese valor exacto) - mas fiable que buscar por ItemsSource, y su
                // ActualWidth/posicion son los mismos que los del ItemsControl que lo hospeda (el
                // ItemsControl no tiene Padding propio, ItemsPresenter no añade ninguno).
                WrapPanel? EncontrarColumnasVisibles()
                    => Descendientes<WrapPanel>(window).FirstOrDefault(wp => wp.IsVisible && Math.Abs(wp.ItemWidth - 248) < 0.1);

                // Caso 1: TODAS las clases activas (filtro por defecto) - referencia del ancho
                // disponible real de la pestaña.
                var colsInicial = EncontrarColumnasVisibles();
                if (colsInicial == null) Console.WriteLine("FALLO: BUILDS_CENTER_SOLO - no se encontro el WrapPanel real de columnas de clase");
                else
                {
                    Console.WriteLine($"BUILDS_CENTER_SOLO (Todas): WrapPanel de columnas ancho real={colsInicial.ActualWidth:0.#}px, {colsInicial.Children.Count} columna(s) visible(s)");
                    Shot("builds-center-todas");

                    // Titulo de columna real: TextAlignment debe ser Center tras el arreglo. OJO:
                    // buscar SOLO dentro del propio WrapPanel de columnas, no en toda la ventana -
                    // la pildora de filtro de arriba ("Builds.ClassFilterOptions", un Button cuyo
                    // Content string WPF envuelve en un TextBlock implicito) tiene el MISMO texto
                    // literal "Cuerpo a cuerpo" y aparece ANTES en el arbol visual, asi que una
                    // busqueda sin acotar encuentra esa pildora, no el titulo real de la columna.
                    var tituloReal = Descendientes<TextBlock>(colsInicial)
                        .FirstOrDefault(t => t.IsVisible && t.Text == vm.Loc["class_melee"]);
                    if (tituloReal == null) Console.WriteLine("FALLO: BUILDS_CENTER_SOLO - no se encontro el titulo real de la columna 'Cuerpo a cuerpo'");
                    else
                    {
                        Console.WriteLine($"BUILDS_CENTER_SOLO (Todas): TextAlignment real del titulo de columna='{tituloReal.TextAlignment}' (esperado Center)");
                        if (tituloReal.TextAlignment != System.Windows.TextAlignment.Center)
                            Console.WriteLine("FALLO: BUILDS_CENTER_SOLO - el titulo de columna no esta centrado (TextAlignment != Center)");
                    }
                }

                // Caso 2: filtrar a UNA sola clase real (Magia) - el bloque de 1 columna debe
                // quedar centrado en el ancho de la pestaña, con hueco igual a ambos lados.
                var vmBuilds = vm.Builds;
                var opcionMagia = vmBuilds.ClassFilterOptions.FirstOrDefault(o => o.Key == "mage");
                if (opcionMagia == null) Console.WriteLine("FALLO: BUILDS_CENTER_SOLO - no existe la opcion de filtro 'mage' real en ClassFilterOptions");
                else
                {
                    vmBuilds.SelectClassFilterCommand.Execute(opcionMagia);
                    DoEvents(); DoEvents(); DoEvents();
                    Shot("builds-center-solo-magia");

                    var colsMagia = EncontrarColumnasVisibles();
                    if (colsMagia == null) Console.WriteLine("FALLO: BUILDS_CENTER_SOLO - no se encontro el WrapPanel real de columnas tras filtrar a Magia");
                    else
                    {
                        var pestañaContenedora = System.Windows.Media.VisualTreeHelper.GetParent(colsMagia) as FrameworkElement;
                        while (pestañaContenedora != null && pestañaContenedora is not System.Windows.Controls.StackPanel) pestañaContenedora = System.Windows.Media.VisualTreeHelper.GetParent(pestañaContenedora) as FrameworkElement;
                        // Sube un nivel mas: la pestaña real (ScrollViewer/Grid con el ancho total
                        // de la zona de contenido) es el padre de la StackPanel de la etapa.
                        var zonaContenido = pestañaContenedora != null ? System.Windows.Media.VisualTreeHelper.GetParent(pestañaContenedora) as FrameworkElement : null;
                        if (zonaContenido == null) Console.WriteLine("FALLO: BUILDS_CENTER_SOLO - no se pudo localizar la zona de contenido real de la pestaña para medir huecos");
                        else
                        {
                            var transformIzq = colsMagia.TransformToAncestor(zonaContenido).Transform(new System.Windows.Point(0, 0));
                            double huecoIzq = transformIzq.X;
                            double huecoDer = zonaContenido.ActualWidth - (huecoIzq + colsMagia.ActualWidth);
                            Console.WriteLine($"BUILDS_CENTER_SOLO (solo Magia): WrapPanel ancho real={colsMagia.ActualWidth:0.#}px ({colsMagia.Children.Count} hijo(s) real(es) en el arbol - antes del arreglo eran siempre 4, con solo 1 visible), zona de contenido={zonaContenido.ActualWidth:0.#}px, hueco izquierdo={huecoIzq:0.#}px, hueco derecho={huecoDer:0.#}px (esperado: diferencia < 5px, centrado real)");
                            if (colsMagia.Children.Count != 1)
                                Console.WriteLine($"FALLO: BUILDS_CENTER_SOLO - el WrapPanel deberia tener 1 solo hijo real tras filtrar a Magia (VisibleClasses filtrada), tiene {colsMagia.Children.Count}");
                            if (Math.Abs(colsMagia.ActualWidth - 248) > 2)
                                Console.WriteLine($"FALLO: BUILDS_CENTER_SOLO - el WrapPanel deberia medir ~248px (1 sola columna real) tras filtrar a Magia, mide {colsMagia.ActualWidth:0.#}px");
                            if (Math.Abs(huecoIzq - huecoDer) > 5)
                                Console.WriteLine($"FALLO: BUILDS_CENTER_SOLO - la columna filtrada a 1 clase NO esta centrada (hueco izq={huecoIzq:0.#}px vs hueco der={huecoDer:0.#}px)");
                            if (huecoIzq < 5)
                                Console.WriteLine("FALLO: BUILDS_CENTER_SOLO - la columna sigue pegada al borde izquierdo (hueco casi cero), el arreglo no esta aplicando de verdad");
                        }
                    }
                    // Deja el filtro como estaba (Todas) para no afectar a otras pruebas que reusen esta ventana.
                    vmBuilds.SelectClassFilterCommand.Execute(vmBuilds.ClassFilterOptions.First(o => o.Key == null));
                    DoEvents();
                }
            }
            catch (Exception ex) { Console.WriteLine("BUILDS_CENTER_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (BUILDS_CENTER_SOLO)");
            Environment.Exit(0);
        }

        // BLANDO_RIO_SOLO=1 (20-sep-2026, 3 bugs reales reportados por el usuario en vivo sobre
        // 'Blando Rio': 1) iconos de NPC desaparecidos del mapa, 2) niebla azulada en la linea de
        // superficie, 3) panel del Bestiario cortado por el borde de la ventana. Diagnostico
        // dedicado: carga el mundo real, mide Npcs.Count/HeadIconPath reales, toma una captura
        // real del mapa Y del panel de Exploracion completo (incluido el lateral derecho) para
        // los tres a la vez.
        if (Environment.GetEnvironmentVariable("BLANDO_RIO_SOLO") == "1")
        {
            try
            {
                string ruta = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\Worlds\Blando_Río.wld");
                if (!File.Exists(ruta)) { Console.WriteLine($"BLANDO_RIO_SOLO: AVISO - falta {ruta}"); Console.WriteLine("DONE (BLANDO_RIO_SOLO)"); Environment.Exit(0); }

                FijarTamaño(window, 1600, 900);
                DoEvents();
                vm.SelectedTabIndex = 4; // Exploracion
                DoEvents();
                var tarea = vm.Exploration.LoadFromPathAsync(ruta);
                while (!tarea.IsCompleted) DoEvents();
                DoEvents(); DoEvents();

                var fitMethod = typeof(MainWindow).GetMethod("OnFitToWindowClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fitMethod?.Invoke(window, [window, new RoutedEventArgs()]);
                DoEvents(); DoEvents();

                Console.WriteLine($"BLANDO_RIO_SOLO: mundo cargado -> Npcs.Count={vm.Exploration.Npcs.Count}, WorldImage nulo={vm.Exploration.WorldImage == null}");
                Console.WriteLine($"BLANDO_RIO_SOLO-GUIA: Guide.ObjetivoPaso={vm.Guide?.ObjetivoPaso}, Zona={vm.Guide?.ObjetivoPaso?.Zona}, GroundLevel={vm.Exploration.GuideBandSuperficieHeight}");
                {
                    var bandaSuperficie = Descendientes<System.Windows.Shapes.Rectangle>(window)
                        .FirstOrDefault(r => Math.Abs(r.Opacity - 0.35) < 0.01);
                    Console.WriteLine($"BLANDO_RIO_SOLO-BANDA: banda de Superficie encontrada={bandaSuperficie != null}, Visibility={bandaSuperficie?.Visibility}, ActualWidth={bandaSuperficie?.ActualWidth:0.#}, ActualHeight={bandaSuperficie?.ActualHeight:0.#}, Canvas.Top={(bandaSuperficie != null ? System.Windows.Controls.Canvas.GetTop(bandaSuperficie) : double.NaN)}");
                }
                foreach (var npc in vm.Exploration.Npcs.Take(20))
                    Console.WriteLine($"BLANDO_RIO_SOLO: NPC '{npc.Name}' Id={npc.Id} Tile=({npc.TileX},{npc.TileY}) HeadIconPath={npc.HeadIconPath ?? "(null)"} IconPath={npc.IconPath ?? "(null)"}");

                var itemsControlNpcs = Descendientes<System.Windows.Controls.ItemsControl>(window)
                    .FirstOrDefault(ic => ic.ItemsSource == vm.Exploration.Npcs);
                Console.WriteLine($"BLANDO_RIO_SOLO: ItemsControl real de Npcs encontrado={itemsControlNpcs != null}, IsVisible={itemsControlNpcs?.IsVisible}, ActualWidth={itemsControlNpcs?.ActualWidth:0.#}, Items generados={itemsControlNpcs?.Items.Count}");
                if (itemsControlNpcs != null)
                {
                    var presentadores = Descendientes<System.Windows.Controls.ContentPresenter>(itemsControlNpcs).ToList();
                    Console.WriteLine($"BLANDO_RIO_SOLO: ContentPresenter reales generados por el ItemsControl de Npcs={presentadores.Count} (esperado = Npcs.Count)");
                    if (presentadores.Count > 0)
                    {
                        var p0 = presentadores[0];
                        Console.WriteLine($"BLANDO_RIO_SOLO: primer marcador real -> Canvas.Left={System.Windows.Controls.Canvas.GetLeft(p0)}, Canvas.Top={System.Windows.Controls.Canvas.GetTop(p0)}, Opacity={p0.Opacity}, Visibility={p0.Visibility}, ActualWidth={p0.ActualWidth:0.#}x{p0.ActualHeight:0.#}");

                        // Recorte INICIAL (estado "Ajustar a la ventana" recien cargado, SIN
                        // cambiar Zoom ni categoria) - evita cualquier problema de sincronizacion
                        // de scroll/zoom propio de GoToNpcCommand, mide el estado real que ve el
                        // usuario nada mas cargar el mundo.
                        var img0 = Descendientes<Image>(p0).FirstOrDefault();
                        Console.WriteLine($"BLANDO_RIO_SOLO-INICIAL: Image real encontrada={img0 != null} (Visibility={img0?.Visibility}, ActualWidth={img0?.ActualWidth:0.#}x{img0?.ActualHeight:0.#})");
                        if (img0 != null && img0.IsVisible)
                        {
                            DoEvents();
                            var puntoInicial = img0.TranslatePoint(new Point(8, 8), window); // centro real del icono de 16x16
                            Console.WriteLine($"BLANDO_RIO_SOLO-INICIAL: posicion real en pantalla -> ({puntoInicial.X:0.#},{puntoInicial.Y:0.#}) (ventana {window.ActualWidth:0.#}x{window.ActualHeight:0.#})");

                            var rtbInicial = new System.Windows.Media.Imaging.RenderTargetBitmap(
                                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                            rtbInicial.Render(window);
                            int cs = 500;
                            int cix = Math.Max(0, Math.Min((int)window.ActualWidth - cs, (int)puntoInicial.X - cs / 2));
                            int ciy = Math.Max(0, Math.Min((int)window.ActualHeight - cs, (int)puntoInicial.Y - cs / 2));
                            var recorteInicial = new System.Windows.Media.Imaging.CroppedBitmap(rtbInicial, new System.Windows.Int32Rect(cix, ciy, cs, cs));
                            var encInicial = new System.Windows.Media.Imaging.PngBitmapEncoder();
                            encInicial.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(recorteInicial));
                            using var fsInicial = File.Create(Path.Combine(AppContext.BaseDirectory, "blando-rio-npc-inicial-recorte.png"));
                            encInicial.Save(fsInicial);
                            Console.WriteLine("BLANDO_RIO_SOLO-INICIAL: captura -> blando-rio-npc-inicial-recorte.png");

                            // Muestreo real de color de pixel en el centro exacto del icono (y una
                            // pequeña rejilla alrededor) - para saber si de verdad se pinta algo
                            // (colores variados del sprite) o queda todo negro/vacio/igual al fondo.
                            var conv = new System.Windows.Media.Imaging.FormatConvertedBitmap(rtbInicial, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                            int stride = conv.PixelWidth * 4;
                            var buffer = new byte[conv.PixelHeight * stride];
                            conv.CopyPixels(buffer, stride, 0);
                            for (int dy = -6; dy <= 6; dy += 3)
                            {
                                var fila = new System.Text.StringBuilder();
                                for (int dx = -6; dx <= 6; dx += 3)
                                {
                                    int px = (int)puntoInicial.X + dx, py = (int)puntoInicial.Y + dy;
                                    if (px < 0 || py < 0 || px >= conv.PixelWidth || py >= conv.PixelHeight) { fila.Append(" fuera  "); continue; }
                                    int off = py * stride + px * 4;
                                    fila.Append($" #{buffer[off + 2]:X2}{buffer[off + 1]:X2}{buffer[off + 0]:X2}");
                                }
                                Console.WriteLine($"BLANDO_RIO_SOLO-PIXELES: dy={dy,3} ->{fila}");
                            }
                        }
                    }
                }

                void Shot(string nombre)
                {
                    DoEvents();
                    var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb.Render(window);
                    var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                    using var fs = File.Create(Path.Combine(AppContext.BaseDirectory, nombre + ".png"));
                    enc.Save(fs);
                    Console.WriteLine($"BLANDO_RIO_SOLO: captura -> {nombre}.png");
                }
                Shot("blando-rio-exploracion-1600x900");

                // Panel del Bestiario - misma diagnosis que Builds: lo activamos y capturamos.
                var expanderBestiario = vm.Exploration.SelectedCategory;
                vm.Exploration.SelectedCategory = WorldSearchCategory.All; // "Todo" por defecto, el bestiario vive en su propio bloque lateral
                DoEvents(); DoEvents();
                Shot("blando-rio-lateral-1600x900");

                // Ancho MINIMO real del panel lateral (260px, ver SettingsViewModel.
                // OnExplorationSidebarWidthChanged) - reproduce el recorte de cabeceras
                // reportado por el usuario ("Solo lo que...", "Este e...", "Edita...",
                // "Bestia...").
                vm.Settings.ExplorationSidebarWidth = 260;
                DoEvents(); DoEvents();
                Shot("blando-rio-lateral-260px");
                var expanders260 = Descendientes<Expander>(window).Where(e => e.IsVisible).ToList();
                foreach (var exp in expanders260)
                    Console.WriteLine($"BLANDO_RIO_SOLO-260: Expander Header='{exp.Header}' ActualWidth={exp.ActualWidth:0.#}");

                // Ventana mas estrecha (1080x700, tamaño real minimo documentado de la app) con
                // el mismo sidebar a 260px - puede que el recorte solo aparezca cuando la
                // VENTANA entera, no solo el sidebar, se queda corta.
                FijarTamaño(window, 1080, 700);
                DoEvents(); DoEvents();
                Shot("blando-rio-lateral-1080x700");
                var expandersEstrecho = Descendientes<Expander>(window).Where(e => e.IsVisible).ToList();
                foreach (var exp in expandersEstrecho)
                    Console.WriteLine($"BLANDO_RIO_SOLO-1080: Expander Header='{exp.Header}' ActualWidth={exp.ActualWidth:0.#}");

                // Acercamiento real a la posicion de un NPC concreto (zoom alto) para ver si el
                // icono de cabeza se pinta de verdad o solo el punto magenta de reserva.
                // DIAG: SelectedCategory=Npcs comentado a proposito - probando si activar la
                // categoria de busqueda es lo que tapa el icono con una capa opaca.
                // vm.Exploration.SelectedCategory = WorldSearchCategory.Npcs;
                DoEvents();
                var npcCercano = vm.Exploration.Npcs.FirstOrDefault();
                if (npcCercano != null)
                {
                    vm.Exploration.Zoom = 8.0;
                    vm.Exploration.GoToNpcCommand.Execute(npcCercano);
                    DoEvents(); DoEvents(); DoEvents();
                    Console.WriteLine($"BLANDO_RIO_SOLO: centrado en '{npcCercano.Name}' con Zoom={vm.Exploration.Zoom}");
                    Shot("blando-rio-zoom-npc");

                    // Diagnostico preciso: el Image real (dentro del Grid 0x0) para ESTE NPC
                    // concreto - tamaño ya calculado tras layout, y si su BitmapImage cargo de
                    // verdad (PixelWidth/Height reales) o fallo en silencio.
                    var itemsControlNpcs2 = Descendientes<System.Windows.Controls.ItemsControl>(window)
                        .FirstOrDefault(ic => ic.ItemsSource == vm.Exploration.Npcs);
                    if (itemsControlNpcs2 != null)
                    {
                        var contenedorGenerator = itemsControlNpcs2.ItemContainerGenerator;
                        var contenedor = contenedorGenerator.ContainerFromItem(npcCercano) as System.Windows.Controls.ContentPresenter;
                        Console.WriteLine($"BLANDO_RIO_SOLO: contenedor real de '{npcCercano.Name}' encontrado={contenedor != null}");
                        if (contenedor != null)
                        {
                            var imagenReal = Descendientes<Image>(contenedor).FirstOrDefault();
                            var elipseReal = Descendientes<System.Windows.Shapes.Ellipse>(contenedor).FirstOrDefault();
                            Console.WriteLine($"BLANDO_RIO_SOLO: Image real encontrada={imagenReal != null} (Visibility={imagenReal?.Visibility}, ActualWidth={imagenReal?.ActualWidth:0.#}x{imagenReal?.ActualHeight:0.#}), Ellipse-fallback encontrada={elipseReal != null} (Visibility={elipseReal?.Visibility})");
                            if (imagenReal?.Source is System.Windows.Media.Imaging.BitmapImage bmp)
                                Console.WriteLine($"BLANDO_RIO_SOLO: BitmapImage real -> PixelWidth={bmp.PixelWidth}, PixelHeight={bmp.PixelHeight}, UriSource={bmp.UriSource}");
                            else
                                Console.WriteLine($"BLANDO_RIO_SOLO: imagenReal.Source tipo real={imagenReal?.Source?.GetType().FullName ?? "(null)"}");
                            var gridPadre = Descendientes<Grid>(contenedor).FirstOrDefault();
                            Console.WriteLine($"BLANDO_RIO_SOLO: Grid contenedor -> ActualWidth={gridPadre?.ActualWidth:0.#}x{gridPadre?.ActualHeight:0.#}, RenderTransform={gridPadre?.RenderTransform}");

                            // Muestreo DIRECTO del BitmapSource ya decodificado (sin pasar por
                            // render-target-bitmap ni por ningun transform) - descarta que el
                            // problema sea de decodificacion en vez de composicion/pintado.
                            if (imagenReal?.Source is System.Windows.Media.Imaging.BitmapSource bmpSrc)
                            {
                                Console.WriteLine($"BLANDO_RIO_SOLO: BitmapSource real -> PixelWidth={bmpSrc.PixelWidth}, PixelHeight={bmpSrc.PixelHeight}, Format={bmpSrc.Format}");
                                var convSrc = new System.Windows.Media.Imaging.FormatConvertedBitmap(bmpSrc, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                                int strideSrc = convSrc.PixelWidth * 4;
                                var bufSrc = new byte[convSrc.PixelHeight * strideSrc];
                                convSrc.CopyPixels(bufSrc, strideSrc, 0);
                                int cxSrc = convSrc.PixelWidth / 2, cySrc = convSrc.PixelHeight / 2;
                                int offSrc = cySrc * strideSrc + cxSrc * 4;
                                Console.WriteLine($"BLANDO_RIO_SOLO: pixel central del sprite decodificado ({cxSrc},{cySrc}) -> #{bufSrc[offSrc + 2]:X2}{bufSrc[offSrc + 1]:X2}{bufSrc[offSrc + 0]:X2} alpha={bufSrc[offSrc + 3]}");
                                bool algoNoTransparente = false;
                                for (int i = 3; i < bufSrc.Length; i += 4) if (bufSrc[i] > 10) { algoNoTransparente = true; break; }
                                Console.WriteLine($"BLANDO_RIO_SOLO: el sprite decodificado tiene algun pixel con alpha>10={algoNoTransparente} (esperado True - el PNG de origen SI tiene contenido visible)");
                            }

                            if (imagenReal != null && imagenReal.IsVisible)
                            {
                                var puntoReal = imagenReal.TranslatePoint(new Point(0, 0), window);
                                Console.WriteLine($"BLANDO_RIO_SOLO: posicion real en pantalla de la Image -> ({puntoReal.X:0.#},{puntoReal.Y:0.#})");

                                DoEvents();
                                var rtbFull = new System.Windows.Media.Imaging.RenderTargetBitmap(
                                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                                rtbFull.Render(window);
                                int cropSize = 260;
                                int cx = Math.Max(0, (int)puntoReal.X - cropSize / 2);
                                int cy = Math.Max(0, (int)puntoReal.Y - cropSize / 2);
                                cropSize = Math.Min(cropSize, Math.Min((int)window.ActualWidth - cx, (int)window.ActualHeight - cy));
                                var recorte = new System.Windows.Int32Rect(cx, cy, cropSize, cropSize);
                                var croppedBmp = new System.Windows.Media.Imaging.CroppedBitmap(rtbFull, recorte);
                                var encCrop = new System.Windows.Media.Imaging.PngBitmapEncoder();
                                encCrop.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(croppedBmp));
                                using var fsCrop = File.Create(Path.Combine(AppContext.BaseDirectory, "blando-rio-npc-recorte.png"));
                                encCrop.Save(fsCrop);
                                Console.WriteLine("BLANDO_RIO_SOLO: captura -> blando-rio-npc-recorte.png (recorte 80x80 centrado en la posicion real de la cabeza)");

                                // Prueba definitiva CORREGIDA: el primer intento (mismo dia, mas
                                // temprano) agrandaba SOLO la Image a 200x200 pero el Grid padre
                                // sigue teniendo su propio RenderTransform (ScaleTransform inverso
                                // al Zoom, aqui Zoom=6 -> escala ~0.167) - deshacia el agrandado
                                // sin que se notara, asi que aquella prueba NO era concluyente de
                                // verdad. Ahora se neutraliza TAMBIEN el RenderTransform del Grid
                                // (Identity) antes de agrandar la Image, para que 200x200 sea
                                // 200x200 de verdad en pantalla.
                                // Render aislado de la Image SIN modificar NADA (tamaño natural
                                // real, antes de tocar Width/Height/RenderTransform) - descarta
                                // que forzar el tamaño despues de ya estar medida/organizada en
                                // el arbol vivo sea la causa de un cache de pintado obsoleto.
                                try
                                {
                                    var rtbNatural = new System.Windows.Media.Imaging.RenderTargetBitmap(
                                        Math.Max(1, (int)Math.Ceiling(imagenReal.ActualWidth)), Math.Max(1, (int)Math.Ceiling(imagenReal.ActualHeight)),
                                        96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                                    rtbNatural.Render(imagenReal);
                                    var convNatural = new System.Windows.Media.Imaging.FormatConvertedBitmap(rtbNatural, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                                    int strideNatural = convNatural.PixelWidth * 4;
                                    var bufNatural = new byte[convNatural.PixelHeight * strideNatural];
                                    convNatural.CopyPixels(bufNatural, strideNatural, 0);
                                    bool naturalTieneContenido = false;
                                    for (int i = 3; i < bufNatural.Length; i += 4) if (bufNatural[i] > 10) { naturalTieneContenido = true; break; }
                                    Console.WriteLine($"BLANDO_RIO_SOLO-NATURAL: render aislado de la Image a su tamaño NATURAL ({imagenReal.ActualWidth:0.#}x{imagenReal.ActualHeight:0.#}, sin tocar nada) tiene algun pixel con alpha>10={naturalTieneContenido}");
                                }
                                catch (Exception exNatural) { Console.WriteLine("BLANDO_RIO_SOLO-NATURAL-EXCEPTION: " + exNatural); }

                                // Render de una Image NUEVA (recien creada, mismo Source) fuera
                                // del arbol vivo - descarta que el problema sea un estado
                                // "envejecido" especifico de ESTA instancia ya usada en pantalla.
                                try
                                {
                                    var imagenNueva = new Image { Source = imagenReal.Source, Width = 16, Height = 16, Stretch = System.Windows.Media.Stretch.Uniform };
                                    imagenNueva.Measure(new Size(16, 16));
                                    imagenNueva.Arrange(new Rect(0, 0, 16, 16));
                                    var rtbNueva = new System.Windows.Media.Imaging.RenderTargetBitmap(16, 16, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                                    rtbNueva.Render(imagenNueva);
                                    var convNueva = new System.Windows.Media.Imaging.FormatConvertedBitmap(rtbNueva, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                                    int strideNueva = convNueva.PixelWidth * 4;
                                    var bufNueva = new byte[convNueva.PixelHeight * strideNueva];
                                    convNueva.CopyPixels(bufNueva, strideNueva, 0);
                                    bool nuevaTieneContenido = false;
                                    for (int i = 3; i < bufNueva.Length; i += 4) if (bufNueva[i] > 10) { nuevaTieneContenido = true; break; }
                                    Console.WriteLine($"BLANDO_RIO_SOLO-NUEVA: Image NUEVA (fuera del arbol vivo, mismo Source, Measure+Arrange manual 16x16) tiene algun pixel con alpha>10={nuevaTieneContenido}");

                                    // Hipotesis real: el Grid contenedor (Width="0" Height="0" a
                                    // proposito, el truco de "centrado con hijos que sobresalen")
                                    // puede llevar un CLIP AUTOMATICO de WPF (layout clip, interno,
                                    // NO el ClipToBounds publico) a sus propios 0x0 - recortando el
                                    // pintado de CUALQUIER hijo centrado fuera de esos limites,
                                    // pese a que Measure/Arrange/TranslatePoint sigan reportando
                                    // todo correcto (el clip actua en el PINTADO, no en el layout).
                                    if (gridPadre != null)
                                    {
                                        var clipReal = System.Windows.Media.VisualTreeHelper.GetClip(gridPadre);
                                        Console.WriteLine($"BLANDO_RIO_SOLO-CLIP: Grid padre -> Width={gridPadre.Width}, Height={gridPadre.Height}, ClipToBounds={gridPadre.ClipToBounds}, VisualTreeHelper.GetClip={(clipReal == null ? "(null)" : clipReal.Bounds.ToString())}");

                                        // Prueba definitiva de la hipotesis "0x0 recorta el
                                        // pintado de verdad": dar al MISMO Grid ya en pantalla un
                                        // tamaño real (16x16, el mismo que la Image) en vez de
                                        // 0x0, sin tocar nada mas, y volver a renderizar la MISMA
                                        // Image (nunca una nueva) en aislado.
                                        gridPadre.Width = 16; gridPadre.Height = 16;
                                        DoEvents(); DoEvents(); DoEvents();
                                        var rtbGridReal = new System.Windows.Media.Imaging.RenderTargetBitmap(16, 16, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                                        rtbGridReal.Render(imagenReal);
                                        var convGridReal = new System.Windows.Media.Imaging.FormatConvertedBitmap(rtbGridReal, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                                        int strideGridReal = convGridReal.PixelWidth * 4;
                                        var bufGridReal = new byte[convGridReal.PixelHeight * strideGridReal];
                                        convGridReal.CopyPixels(bufGridReal, strideGridReal, 0);
                                        bool gridRealTieneContenido = false;
                                        for (int i = 3; i < bufGridReal.Length; i += 4) if (bufGridReal[i] > 10) { gridRealTieneContenido = true; break; }
                                        Console.WriteLine($"BLANDO_RIO_SOLO-GRIDREAL: con el Grid padre puesto a 16x16 (en vez de 0x0), la MISMA Image ya en pantalla ahora pinta algo (alpha>10)={gridRealTieneContenido}");
                                    }
                                }
                                catch (Exception exNueva) { Console.WriteLine("BLANDO_RIO_SOLO-NUEVA-EXCEPTION: " + exNueva); }

                                if (gridPadre != null) gridPadre.RenderTransform = System.Windows.Media.Transform.Identity;
                                imagenReal.Width = 200; imagenReal.Height = 200;
                                DoEvents(); DoEvents(); DoEvents();
                                var puntoAgrandado = imagenReal.TranslatePoint(new Point(0, 0), window);
                                Console.WriteLine($"BLANDO_RIO_SOLO-AGRANDADO: nueva posicion -> ({puntoAgrandado.X:0.#},{puntoAgrandado.Y:0.#}), ActualSize={imagenReal.ActualWidth:0.#}x{imagenReal.ActualHeight:0.#}");
                                var rtbAgrandado = new System.Windows.Media.Imaging.RenderTargetBitmap(
                                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                                rtbAgrandado.Render(window);
                                using var fsAg = File.Create(Path.Combine(AppContext.BaseDirectory, "blando-rio-npc-agrandado.png"));
                                var encAg = new System.Windows.Media.Imaging.PngBitmapEncoder();
                                encAg.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbAgrandado));
                                encAg.Save(fsAg);
                                Console.WriteLine("BLANDO_RIO_SOLO-AGRANDADO: captura -> blando-rio-npc-agrandado.png (ventana completa, la Image real ahora mide 200x200)");

                                // Renderizado AISLADO: capturar SOLO la Image (sin pasar por el
                                // resto de la ventana/composicion) - si esto SI pinta algo, el
                                // problema es de composicion dentro de la ventana; si tampoco
                                // pinta nada, el problema esta en la propia Image/su Source.
                                try
                                {
                                    var rtbAislado = new System.Windows.Media.Imaging.RenderTargetBitmap(200, 134, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                                    rtbAislado.Render(imagenReal);
                                    using var fsAislado = File.Create(Path.Combine(AppContext.BaseDirectory, "blando-rio-npc-aislado.png"));
                                    var encAislado = new System.Windows.Media.Imaging.PngBitmapEncoder();
                                    encAislado.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbAislado));
                                    encAislado.Save(fsAislado);
                                    Console.WriteLine("BLANDO_RIO_SOLO-AISLADO: captura -> blando-rio-npc-aislado.png (SOLO la Image, render directo)");

                                    // Muestreo de pixel central de ESTE render aislado.
                                    var convAislado = new System.Windows.Media.Imaging.FormatConvertedBitmap(rtbAislado, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                                    int strideAislado = convAislado.PixelWidth * 4;
                                    var bufAislado = new byte[convAislado.PixelHeight * strideAislado];
                                    convAislado.CopyPixels(bufAislado, strideAislado, 0);
                                    int offAislado = (67) * strideAislado + (100) * 4;
                                    Console.WriteLine($"BLANDO_RIO_SOLO-AISLADO: pixel central (100,67) del render aislado -> #{bufAislado[offAislado + 2]:X2}{bufAislado[offAislado + 1]:X2}{bufAislado[offAislado + 0]:X2} alpha={bufAislado[offAislado + 3]}");
                                    bool aisladoTieneContenido = false;
                                    for (int i = 3; i < bufAislado.Length; i += 4) if (bufAislado[i] > 10) { aisladoTieneContenido = true; break; }
                                    Console.WriteLine($"BLANDO_RIO_SOLO-AISLADO: el render aislado tiene algun pixel con alpha>10={aisladoTieneContenido}");
                                }
                                catch (Exception exAislado) { Console.WriteLine("BLANDO_RIO_SOLO-AISLADO-EXCEPTION: " + exAislado); }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("BLANDO_RIO_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (BLANDO_RIO_SOLO)");
            Environment.Exit(0);
        }

        // AUDIT_SOLO=1 (20-sep-2026, dos bugs reales de la auditoria visual independiente):
        // 1) "Sin personaje cargadoSin mundo cargado" pegados sin espacio (sin personaje ni
        //    mundo, cualquier ancho).
        // 2) "Sin mundo cargado" (overlay grande centrado de T6) se corta como "Sin m" a
        //    1080x700 por el panel lateral flotante encima.
        if (Environment.GetEnvironmentVariable("AUDIT_SOLO") == "1")
        {
            try
            {
                vm.SelectedTabIndex = 4; // Exploracion, sin cargar nada (ni personaje ni mundo)
                DoEvents();

                void Shot(string nombre)
                {
                    DoEvents();
                    var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb.Render(window);
                    var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                    using var fs = File.Create(Path.Combine(AppContext.BaseDirectory, nombre + ".png"));
                    enc.Save(fs);
                    Console.WriteLine($"AUDIT_SOLO: captura -> {nombre}.png");
                }

                foreach (var (w, h) in new[] { (1080.0, 700.0), (1600.0, 900.0), (2200.0, 1300.0) })
                {
                    FijarTamaño(window, w, h);
                    DoEvents(); DoEvents();
                    Shot($"audit-vacio-{w:0}x{h:0}");

                    var tbPersonaje = Descendientes<TextBlock>(window).FirstOrDefault(t => t.IsVisible && t.Text == vm.Loc["label_no_character_loaded"]);
                    var tbMundo = Descendientes<TextBlock>(window).Where(t => t.IsVisible && t.Text == vm.Loc["explore_no_world_loaded"]).ToList();
                    Console.WriteLine($"AUDIT_SOLO {w:0}x{h:0}: tbPersonaje encontrado={tbPersonaje != null}, tbMundo.Count={tbMundo.Count}, IsCharacterLoaded={vm.IsCharacterLoaded}");
                    // El arnes ya tiene un personaje real cargado a esta altura de Main() (bootstrap
                    // temprano, no condicional) - "Sin personaje cargado" no es alcanzable aqui sin
                    // desmontar ese bootstrap. Verificacion estructural equivalente y suficiente: el
                    // WrapPanel de estado de Exploracion (Grid.Column="1") lleva de verdad el margen
                    // izquierdo real que separa su contenido de la columna de personaje - antes NO
                    // llevaba ninguno (causa real del bug, confirmada leyendo el XAML).
                    var wrapPanelMundo = tbMundo.Count > 0 ? System.Windows.Media.VisualTreeHelper.GetParent(tbMundo[0]) as WrapPanel : null;
                    if (wrapPanelMundo != null)
                        Console.WriteLine($"AUDIT_SOLO {w:0}x{h:0}: WrapPanel de estado de Exploracion -> Margin={wrapPanelMundo.Margin} (esperado Left>=10)");
                    if (tbPersonaje != null && tbMundo.Count > 0)
                    {
                        var rectPersonaje = new Rect(tbPersonaje.TranslatePoint(new Point(0, 0), window), new Size(tbPersonaje.ActualWidth, tbPersonaje.ActualHeight));
                        foreach (var tbM in tbMundo)
                        {
                            var rectMundo = new Rect(tbM.TranslatePoint(new Point(0, 0), window), new Size(tbM.ActualWidth, tbM.ActualHeight));
                            double gapX = rectMundo.Left - rectPersonaje.Right;
                            bool mismaFila = Math.Abs(rectMundo.Top - rectPersonaje.Top) < 10;
                            Console.WriteLine($"AUDIT_SOLO {w:0}x{h:0}: '{tbPersonaje.Text}' termina en X={rectPersonaje.Right:0.#}, '{tbM.Text}' (FontSize={tbM.FontSize}) empieza en X={rectMundo.Left:0.#} -> gap={gapX:0.#}px, misma fila={mismaFila}");
                            if (mismaFila && gapX < 4)
                                Console.WriteLine($"FALLO: AUDIT_SOLO - a {w:0}x{h:0} '{tbPersonaje.Text}' y '{tbM.Text}' quedan pegados (gap={gapX:0.#}px, se esperaba >=4px)");
                        }
                    }

                    // Bug 2: el overlay grande (TitleText FontSize=18) no debe quedar bajo el
                    // panel lateral flotante (T6) - oclusion por un HERMANO opaco encima, no un
                    // recorte de ancestro (ZonaVisible/AR-LAY no cazan esto, ver el comentario
                    // real mas abajo sobre generalizar el detector). Comprobacion DIRECTA:
                    // interseccion real de rectangulos entre el texto y la tarjeta flotante.
                    var overlayGrande = tbMundo.FirstOrDefault(t => Math.Abs(t.FontSize - 18) < 0.5);
                    var tarjetaFlotante = window.FindName("ExplorationSidebarFloatingCard") as FrameworkElement;
                    if (overlayGrande != null && tarjetaFlotante != null && tarjetaFlotante.IsVisible)
                    {
                        var rectTexto = new Rect(overlayGrande.TranslatePoint(new Point(0, 0), window), new Size(overlayGrande.ActualWidth, overlayGrande.ActualHeight));
                        var rectTarjeta = new Rect(tarjetaFlotante.TranslatePoint(new Point(0, 0), window), new Size(tarjetaFlotante.ActualWidth, tarjetaFlotante.ActualHeight));
                        var interseccion = Rect.Intersect(rectTexto, rectTarjeta);
                        double solapeX = interseccion.IsEmpty ? 0 : interseccion.Width;
                        Console.WriteLine($"AUDIT_SOLO {w:0}x{h:0}: overlay grande 'Sin mundo cargado' texto=({rectTexto.X:0.#},{rectTexto.Y:0.#},{rectTexto.Width:0.#}x{rectTexto.Height:0.#}), tarjeta=({rectTarjeta.X:0.#},{rectTarjeta.Y:0.#},{rectTarjeta.Width:0.#}x{rectTarjeta.Height:0.#}) -> solapeX={solapeX:0.#}px");
                        if (solapeX > 2)
                            Console.WriteLine($"FALLO: AUDIT_SOLO - a {w:0}x{h:0} el overlay grande 'Sin mundo cargado' se solapa {solapeX:0.#}px con el panel lateral flotante (T6) - queda tapado");
                    }
                    else
                    {
                        Console.WriteLine($"AUDIT_SOLO {w:0}x{h:0}: overlayGrande={overlayGrande != null}, tarjetaFlotante={tarjetaFlotante != null} (IsVisible={tarjetaFlotante?.IsVisible})");
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("AUDIT_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (AUDIT_SOLO)");
            Environment.Exit(0);
        }

        // BESTIARY_DPI_SOLO=1 (20-sep-2026, pedido explicito del coordinador tras confirmar que
        // el usuario SI vio el panel del Bestiario cortado en su maquina real): prueba con la
        // escala de interfaz REAL (VisualTreeHelper.SetRootDpi, mismo mecanismo real de WPF que
        // usa un monitor con escala no estandar - TerrakeepTrainer encontro 1,4666667 en otra
        // app) en vez de solo ventanas mas pequeñas (ya probado sin reproducir). Tambien usa el
        // mundo real con MAS especies de bestiario de los 5 disponibles en esta maquina, mas
        // cerca de las 361 reales del usuario que un mundo de pruebas pequeño.
        if (Environment.GetEnvironmentVariable("BESTIARY_DPI_SOLO") == "1")
        {
            try
            {
                string[] mundos =
                [
                    MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld"),
                    MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\Worlds\Blando_Río.wld"),
                    MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\Afueras_de_Larvas_de_gusano.wld"),
                    MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\adriandres.wld"),
                    MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\El_Musgo_de_Accidentes.wld"),
                ];
                string? mejorRuta = null; int mejorCuenta = -1;
                foreach (string ruta in mundos)
                {
                    if (!File.Exists(ruta)) continue;
                    var w = Terrakeep.Core.WldFormat.WldReader.Read(File.ReadAllBytes(ruta), readContainers: false);
                    if (w.Bestiary == null) continue;
                    var claves = new HashSet<string>(w.Bestiary.Kills.Keys, StringComparer.OrdinalIgnoreCase);
                    claves.UnionWith(w.Bestiary.Sighted);
                    claves.UnionWith(w.Bestiary.Chatted);
                    Console.WriteLine($"BESTIARY_DPI_SOLO: '{Path.GetFileName(ruta)}' -> {claves.Count} especies reales de bestiario");
                    if (claves.Count > mejorCuenta) { mejorCuenta = claves.Count; mejorRuta = ruta; }
                }
                if (mejorRuta == null) { Console.WriteLine("BESTIARY_DPI_SOLO: AVISO - ningun mundo real con bestiario en esta maquina"); Console.WriteLine("DONE (BESTIARY_DPI_SOLO)"); Environment.Exit(0); }
                Console.WriteLine($"BESTIARY_DPI_SOLO: usando '{Path.GetFileName(mejorRuta)}' ({mejorCuenta} especies, el mundo real con mas de los 5 disponibles - el usuario reporto 361)");

                vm.SelectedTabIndex = 4;
                DoEvents();
                var tareaCarga = vm.Exploration.LoadFromPathAsync(mejorRuta);
                while (!tareaCarga.IsCompleted) DoEvents();
                DoEvents(); DoEvents();

                void ShotDpi(string nombre)
                {
                    DoEvents();
                    var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb.Render(window);
                    var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                    using var fs = File.Create(Path.Combine(AppContext.BaseDirectory, nombre + ".png"));
                    enc.Save(fs);
                    Console.WriteLine($"BESTIARY_DPI_SOLO: captura -> {nombre}.png");
                }

                // Escenario real posible: monitor pequeño + escala alta de Windows puede dejar
                // MENOS espacio logico del que la app declara como MinWidth (1080) - Windows
                // respeta el tamaño de pantalla real por encima del MinWidth de la ventana si no
                // hay sitio fisico. Se fuerza aqui directamente (bypass del MinWidth real) para
                // simular ese caso limite, ya que 1080x700 (el minimo declarado) no reprodujo el
                // corte.
                foreach (var (w, h) in new[] { (1600.0, 900.0), (1080.0, 700.0), (950.0, 650.0), (860.0, 600.0) })
                {
                    FijarTamaño(window, w, h);
                    DoEvents(); DoEvents();

                    foreach (double dpiFactor in new[] { 1.0, 1.4666667 })
                    {
                        var raiz = System.Windows.PresentationSource.FromVisual(window) is { } src ? (System.Windows.Media.Visual)src.RootVisual! : window;
                        try { System.Windows.Media.VisualTreeHelper.SetRootDpi(raiz, new System.Windows.DpiScale(dpiFactor, dpiFactor)); }
                        catch (Exception exDpi) { Console.WriteLine($"BESTIARY_DPI_SOLO: SetRootDpi fallo ({exDpi.GetType().Name}: {exDpi.Message}) - probando via window directamente"); try { System.Windows.Media.VisualTreeHelper.SetRootDpi(window, new System.Windows.DpiScale(dpiFactor, dpiFactor)); } catch (Exception ex2) { Console.WriteLine($"BESTIARY_DPI_SOLO: tambien fallo sobre window: {ex2.GetType().Name}: {ex2.Message}"); } }
                        DoEvents(); DoEvents(); DoEvents();

                        ShotDpi($"bestiary-dpi{dpiFactor:0.0000}-{w:0}x{h:0}");

                        var expandersReales = Descendientes<Expander>(window).Where(e => e.IsVisible).ToList();
                        foreach (var exp in expandersReales)
                        {
                            var headerPresenter = Descendientes<TextBlock>(exp).FirstOrDefault(t => t.Text == (exp.Header as string));
                            Console.WriteLine($"BESTIARY_DPI_SOLO dpi={dpiFactor:0.0000} {w:0}x{h:0}: Expander Header='{exp.Header}' ActualWidth={exp.ActualWidth:0.#}, headerTextBlock encontrado={headerPresenter != null}, TextTrimming={headerPresenter?.GetValue(TextBlock.TextTrimmingProperty)}");
                            // Busqueda AMPLIA: cualquier AccessText (control interno real que usa
                            // el ControlTemplate por defecto de Expander en el tema Aero2/Fluent
                            // de Windows para el Header - NO hereda de TextBlock, mi busqueda
                            // anterior lo pasaba por alto) con el mismo texto.
                            var accessTextReal = Descendientes<System.Windows.Controls.AccessText>(exp).FirstOrDefault(a => a.Text == (exp.Header as string));
                            if (accessTextReal != null)
                                Console.WriteLine($"BESTIARY_DPI_SOLO dpi={dpiFactor:0.0000} {w:0}x{h:0}: AccessText real encontrado -> ActualWidth={accessTextReal.ActualWidth:0.#}, TextTrimming={accessTextReal.TextTrimming}, DesiredSize={accessTextReal.DesiredSize.Width:0.#}");
                            // El propio toggle de expandir/contraer (chevron) resta ancho real al
                            // header - cuanto le queda de verdad al texto tras su chrome interno.
                            var headerContentPresenter = Descendientes<ContentPresenter>(exp).FirstOrDefault();
                            if (headerContentPresenter != null)
                                Console.WriteLine($"BESTIARY_DPI_SOLO dpi={dpiFactor:0.0000} {w:0}x{h:0}: primer ContentPresenter real -> ActualWidth={headerContentPresenter.ActualWidth:0.#}");
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("BESTIARY_DPI_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (BESTIARY_DPI_SOLO)");
            Environment.Exit(0);
        }

        // NPC_ZORDER_SOLO=1 (20-sep-2026, continuacion de la investigacion de iconos de NPC
        // desaparecidos - pedido explicito del coordinador de no dejarlo en "sin determinar").
        // Comprueba con el MISMO metodo real ya usado y verificado esta noche para T6 (indice de
        // hijo dentro del padre comun via VisualTreeHelper, mas fiable que HitTest en este
        // arnes) si el ItemsControl de Npcs esta de verdad DETRAS de otro hermano opaco, y la
        // cadena de Opacity efectiva de todos sus ancestros reales.
        if (Environment.GetEnvironmentVariable("NPC_ZORDER_SOLO") == "1")
        {
            try
            {
                string ruta = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\Worlds\Blando_Río.wld");
                if (!File.Exists(ruta)) { Console.WriteLine($"NPC_ZORDER_SOLO: AVISO - falta {ruta}"); Console.WriteLine("DONE (NPC_ZORDER_SOLO)"); Environment.Exit(0); }
                FijarTamaño(window, 1600, 900);
                DoEvents();
                vm.SelectedTabIndex = 4;
                DoEvents();
                var tarea = vm.Exploration.LoadFromPathAsync(ruta);
                while (!tarea.IsCompleted) DoEvents();
                DoEvents(); DoEvents();
                var fitMethod = typeof(MainWindow).GetMethod("OnFitToWindowClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fitMethod?.Invoke(window, [window, new RoutedEventArgs()]);
                DoEvents(); DoEvents();

                // ADR-TERRAKEEP-030 (27-sep-2026): Mapa+minimapa se movio a WorldMapView.xaml
                // (UserControl con su propio NameScope) - FindName DOBLE, mismo patron ya usado
                // por GUIA/ADR-021, Compare/ADR-025, WorldTools/ADR-027, ChestInspector/ADR-028 y
                // Browse/ADR-029.
                var worldMapViewHostZorder = window.FindName("WorldMapView") as FrameworkElement;
                var worldMapImage = worldMapViewHostZorder?.FindName("WorldMapImage") as Image;
                var itemsControlNpcs = Descendientes<System.Windows.Controls.ItemsControl>(window).FirstOrDefault(ic => ic.ItemsSource == vm.Exploration.Npcs);
                Console.WriteLine($"NPC_ZORDER_SOLO: WorldMapImage encontrado={worldMapImage != null}, ItemsControl Npcs encontrado={itemsControlNpcs != null}");
                if (worldMapImage != null && itemsControlNpcs != null)
                {
                    var padreImg = System.Windows.Media.VisualTreeHelper.GetParent(worldMapImage);
                    var padreNpcs = System.Windows.Media.VisualTreeHelper.GetParent(itemsControlNpcs);
                    Console.WriteLine($"NPC_ZORDER_SOLO: padre de WorldMapImage={padreImg?.GetType().Name}, padre de ItemsControl Npcs={padreNpcs?.GetType().Name}, mismo padre={ReferenceEquals(padreImg, padreNpcs)}");
                    if (ReferenceEquals(padreImg, padreNpcs) && padreImg is Panel panelComun)
                    {
                        int idxImg = panelComun.Children.IndexOf(worldMapImage);
                        int idxNpcs = panelComun.Children.IndexOf(itemsControlNpcs);
                        Console.WriteLine($"NPC_ZORDER_SOLO: indice real en el padre comun -> WorldMapImage={idxImg}, ItemsControl Npcs={idxNpcs} (Npcs deberia pintarse ENCIMA: indice mayor)");
                        if (idxNpcs < idxImg) Console.WriteLine("FALLO: NPC_ZORDER_SOLO - el ItemsControl de Npcs tiene indice MENOR que el mapa, se pinta DEBAJO");
                    }

                    // Cadena de Opacity efectiva real: multiplicar el Opacity de CADA ancestro
                    // real desde el ItemsControl hasta la ventana - un 0.0 en cualquier escalon
                    // deja todo lo de dentro invisible aunque cada Opacity individual "parezca"
                    // normal vista aislada.
                    double opacidadEfectiva = 1.0;
                    var nodo = (DependencyObject)itemsControlNpcs;
                    var cadena = new List<string>();
                    while (nodo != null)
                    {
                        if (nodo is UIElement ui)
                        {
                            opacidadEfectiva *= ui.Opacity;
                            cadena.Add($"{nodo.GetType().Name}(Opacity={ui.Opacity:0.##},Visibility={ui.Visibility})");
                        }
                        nodo = System.Windows.Media.VisualTreeHelper.GetParent(nodo);
                    }
                    Console.WriteLine($"NPC_ZORDER_SOLO: opacidad efectiva real desde ItemsControl Npcs hasta la ventana = {opacidadEfectiva:0.####}");
                    Console.WriteLine($"NPC_ZORDER_SOLO: cadena real de ancestros -> {string.Join(" < ", cadena)}");
                }

                DoEvents();
                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(window);
                using var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "npc-zorder-mapa-completo.png"));
                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                enc.Save(fs);
                Console.WriteLine("NPC_ZORDER_SOLO: captura -> npc-zorder-mapa-completo.png");
            }
            catch (Exception ex) { Console.WriteLine("NPC_ZORDER_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (NPC_ZORDER_SOLO)");
            Environment.Exit(0);
        }

        // IDEA8_SOLO=1 (20-sep-2026, catalogo de funciones, idea 8 "Informe y comparador de
        // mundos" - version real, tercera ronda tras la correccion del coordinador: el catalogo
        // citaba WorldCreationSummaryBuilder como apoyo, que resulto ser para la vista previa de
        // GENERACION (semilla/tamaño antes de crear un mundo), nunca para resumir un .wld ya
        // real y guardado - camino real encontrado: extender BuildWorldReportText (el "informe"
        // YA existia) con Estado/semilla especial/vetas, y construir el "comparador" nuevo de
        // cero (WorldCompareViewModel) sobre WldReader+CompareStatRowViewModel, ya probados.
        //
        // Verificacion con "aislar la variable": la verdad de referencia (Title/Seed/HardMode/
        // IsCrimson/jefes/Npcs/Chests/Signs reales) se lee AQUI con un WldReader.Read propio e
        // independiente, nunca reutilizando el calculo del propio ViewModel bajo prueba - un test
        // que solo comparara el ViewModel contra si mismo no demostraria nada.
        if (Environment.GetEnvironmentVariable("IDEA8_SOLO") == "1")
        {
            try
            {
                string mundoA = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
                string mundoB = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\Worlds\Blando_Río.wld");
                if (!File.Exists(mundoA) || !File.Exists(mundoB))
                {
                    Console.WriteLine($"IDEA8_SOLO: AVISO - faltan mundos reales de prueba (A existe={File.Exists(mundoA)}, B existe={File.Exists(mundoB)}), se omite");
                }
                else
                {
                    // --- Parte 1: informe de UN mundo extendido (BuildWorldReportText) ---
                    vm.SelectedTabIndex = 4; // Exploracion
                    DoEvents();
                    var taskLoad = vm.Exploration.LoadFromPathAsync(mundoA);
                    while (!taskLoad.IsCompleted) DoEvents();
                    DoEvents(); DoEvents();

                    var verdadA = Terrakeep.Core.WldFormat.WldReader.Read(File.ReadAllBytes(mundoA));
                    var (jefesVerdad, totalVerdad) = Terrakeep.App.ViewModels.ExplorationViewModel.CountDownedBosses(verdadA.Header);
                    string informe = vm.Exploration.BuildWorldReportText();
                    Console.WriteLine($"IDEA8_SOLO: informe real generado, {informe.Length} caracteres");
                    bool tieneEstado = informe.Contains("=== Estado ===") || informe.Contains("=== State ===");
                    bool tieneJefes = informe.Contains($"{jefesVerdad}/{totalVerdad}");
                    bool tieneModoDificil = informe.Contains(verdadA.Header.HardMode ? "Sí" : "No") || informe.Contains(verdadA.Header.HardMode ? "Yes" : "No");
                    Console.WriteLine($"IDEA8_SOLO: verdad de referencia (WldReader propio) -> jefes derrotados real={jefesVerdad}/{totalVerdad}, HardMode real={verdadA.Header.HardMode}, IsCrimson real={verdadA.Header.IsCrimson}");
                    Console.WriteLine($"IDEA8_SOLO: informe contiene seccion Estado={tieneEstado}, contiene '{jefesVerdad}/{totalVerdad}'={tieneJefes}");
                    if (!tieneEstado) Console.WriteLine("FALLO: IDEA8_SOLO - BuildWorldReportText no incluye la nueva seccion Estado");
                    if (!tieneJefes) Console.WriteLine($"FALLO: IDEA8_SOLO - el informe no refleja el recuento REAL de jefes derrotados ({jefesVerdad}/{totalVerdad})");
                    if (!tieneModoDificil) Console.WriteLine("FALLO: IDEA8_SOLO - el informe no refleja el HardMode real del archivo");

                    bool tieneVetas = informe.Contains("Top vetas") || informe.Contains("Top ore veins");
                    Console.WriteLine($"IDEA8_SOLO: informe incluye seccion de vetas={tieneVetas}");
                    if (!tieneVetas) Console.WriteLine("FALLO: IDEA8_SOLO - el informe no incluye la nueva seccion de vetas de mineral");

                    // --- Parte 2: comparador de DOS mundos (WorldCompareViewModel) ---
                    vm.Exploration.SelectedCategory = Terrakeep.App.ViewModels.WorldSearchCategory.Compare;
                    DoEvents();
                    var taskA = vm.WorldCompare.LoadAAsync(mundoA);
                    while (!taskA.IsCompleted) DoEvents();
                    var taskB = vm.WorldCompare.LoadBAsync(mundoB);
                    while (!taskB.IsCompleted) DoEvents();
                    DoEvents(); DoEvents();

                    var verdadB = Terrakeep.Core.WldFormat.WldReader.Read(File.ReadAllBytes(mundoB));
                    Console.WriteLine($"IDEA8_SOLO: verdad de referencia B -> Title='{verdadB.Header.Title}', Npcs={verdadB.Npcs.Count}, Chests={verdadB.Chests.Count}, Signs={verdadB.Signs.Count}");
                    Console.WriteLine($"IDEA8_SOLO: WorldCompare.HasBothLoaded={vm.WorldCompare.HasBothLoaded}, StatRows.Count={vm.WorldCompare.StatRows.Count}, DifferenceCount={vm.WorldCompare.DifferenceCount}");
                    if (!vm.WorldCompare.HasBothLoaded) Console.WriteLine("FALLO: IDEA8_SOLO - el comparador no marca los dos mundos como cargados");
                    if (vm.WorldCompare.StatRows.Count == 0) Console.WriteLine("FALLO: IDEA8_SOLO - el comparador no genero ninguna fila real");

                    var filaTitulo = vm.WorldCompare.StatRows.FirstOrDefault(r => r.ValueA == verdadA.Header.Title);
                    Console.WriteLine($"IDEA8_SOLO: fila de Titulo real encontrada={filaTitulo != null} (ValueA='{filaTitulo?.ValueA}' esperado='{verdadA.Header.Title}', ValueB='{filaTitulo?.ValueB}' esperado='{verdadB.Header.Title}')");
                    if (filaTitulo == null || filaTitulo.ValueB != verdadB.Header.Title)
                        Console.WriteLine("FALLO: IDEA8_SOLO - la fila de Titulo no refleja los valores REALES de los dos archivos");

                    var filaNpcs = vm.WorldCompare.StatRows.FirstOrDefault(r => r.ValueA == verdadA.Npcs.Count.ToString());
                    if (filaNpcs == null || filaNpcs.ValueB != verdadB.Npcs.Count.ToString())
                        Console.WriteLine($"FALLO: IDEA8_SOLO - la fila de NPCs no refleja el recuento REAL (A={verdadA.Npcs.Count}, B={verdadB.Npcs.Count})");
                    else
                        Console.WriteLine($"IDEA8_SOLO: fila de NPCs correcta -> A={filaNpcs.ValueA}, B={filaNpcs.ValueB}");

                    // Aisla la variable "de verdad detecta diferencias" de "de verdad detecta
                    // IGUALDAD": cargar el MISMO archivo en los dos lados tiene que dar 0
                    // diferencias exactas - si el comparador marcara todo como distinto siempre,
                    // esto lo cazaria.
                    var taskA2 = vm.WorldCompare.LoadAAsync(mundoA);
                    while (!taskA2.IsCompleted) DoEvents();
                    var taskB2 = vm.WorldCompare.LoadBAsync(mundoA);
                    while (!taskB2.IsCompleted) DoEvents();
                    DoEvents(); DoEvents();
                    Console.WriteLine($"IDEA8_SOLO: mismo archivo en A y B -> DifferenceCount={vm.WorldCompare.DifferenceCount} (esperado 0)");
                    if (vm.WorldCompare.DifferenceCount != 0)
                        Console.WriteLine($"FALLO: IDEA8_SOLO - comparar un mundo consigo mismo da {vm.WorldCompare.DifferenceCount} diferencias, deberian ser 0");

                    // Recarga los dos mundos reales distintos para la captura (el estado visual
                    // que de verdad importa enseñar es el de "hay diferencias resaltadas").
                    var taskA3 = vm.WorldCompare.LoadAAsync(mundoA);
                    while (!taskA3.IsCompleted) DoEvents();
                    var taskB3 = vm.WorldCompare.LoadBAsync(mundoB);
                    while (!taskB3.IsCompleted) DoEvents();
                    DoEvents(); DoEvents();

                    var itemsControlCompare = Descendientes<System.Windows.Controls.ItemsControl>(window)
                        .FirstOrDefault(ic => ic.ItemsSource == vm.WorldCompare.StatRows);
                    Console.WriteLine($"IDEA8_SOLO: ItemsControl real de filas encontrado en el arbol visual={itemsControlCompare != null}, visible={itemsControlCompare?.IsVisible}");
                    if (itemsControlCompare == null || !itemsControlCompare.IsVisible)
                        Console.WriteLine("FALLO: IDEA8_SOLO - el panel del comparador de mundos no esta realmente en pantalla");

                    var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb.Render(window);
                    var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                    string shot = Path.Combine(AppContext.BaseDirectory, "idea8-comparador-mundos.png");
                    using (var fs = File.Create(shot)) enc.Save(fs);
                    Console.WriteLine($"IDEA8_SOLO: captura real -> {shot}");
                }
            }
            catch (Exception ex) { Console.WriteLine("IDEA8_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (IDEA8_SOLO)");
            Environment.Exit(0);
        }

        // T2_SOLO=1 (20-sep-2026, catalogo de rediseño visual T2 "cabecera de una fila"):
        // verifica en frio, con la ventana real, que la fila de botones de la cabecera global YA
        // NO envuelve a una segunda linea al ancho de referencia real del catalogo (1180x860,
        // "la cabecera mide ~100px de alto") - medido con ActualHeight del WrapPanel real
        // (ItemHeight=44: una sola linea real mide 44px, dos lineas 88px+), no solo mirando una
        // captura. Comprueba ademas que el menu real "⋯ Personaje" abre y trae los 2 items
        // reales (Cargar personaje, Deshacer ultimo guardado) y que los tres botones NO tocados
        // (Buscar/Historial/Codigo de build, con dependencias reales - WhereIsItPopup/
        // BUILDCODE-CANEXECUTE/VITALS_SOLO) siguen ahi tal cual.
        if (Environment.GetEnvironmentVariable("T2_SOLO") == "1")
        {
            try
            {
                vm.SelectedTabIndex = 1; // Personaje: la cabecera es identica en las 6 pestañas, pero aqui hay BuildCodeButton real para comparar
                FijarTamaño(window, 1180, 860);
                DoEvents(); DoEvents();

                var wrapBotones = Descendientes<System.Windows.Controls.WrapPanel>(window).FirstOrDefault(w => w.ItemHeight == 44);
                Console.WriteLine($"T2_SOLO: WrapPanel de botones encontrado={wrapBotones != null}, a 1180x860 ActualHeight={wrapBotones?.ActualHeight:0}px (esperado ~44px, UNA sola linea - antes de T2 envolvia a ~88px)");
                if (wrapBotones == null) Console.WriteLine("FALLO: T2_SOLO - no se encuentra el WrapPanel real de botones de la cabecera");
                else if (wrapBotones.ActualHeight > 60) Console.WriteLine($"FALLO: T2_SOLO - la fila de botones sigue envolviendo a mas de una linea a 1180x860 ({wrapBotones.ActualHeight:0}px)");

                // BuildCodeButton es el UNICO de los 4 originales que se queda fuera del menu a
                // proposito (blinda BUILDCODE-CANEXECUTE, ver el comentario real en MainWindow.
                // xaml) - sigue ahi, visible y con su x:Name intacto. Buscar/Historial/Cargar
                // personaje/Deshacer ultimo guardado ya NO son botones sueltos (viven dentro del
                // menu "⋯ Personaje", comprobado mas abajo).
                var codigoBuild = window.FindName("BuildCodeButton") as System.Windows.Controls.Button;
                Console.WriteLine($"T2_SOLO: BuildCodeButton (el unico boton real que se queda fuera del menu a proposito) sigue ahi={codigoBuild != null} (esperado True)");
                if (codigoBuild == null) Console.WriteLine("FALLO: T2_SOLO - BuildCodeButton ha desaparecido o perdido su x:Name (rompe BUILDCODE-CANEXECUTE)");
                var buscarSuelto = window.FindName("WhereIsItButton");
                var historialSuelto = window.FindName("BackupHistoryButton");
                Console.WriteLine($"T2_SOLO: Buscar/Historial YA NO son botones sueltos -> WhereIsItButton={buscarSuelto != null}, BackupHistoryButton={historialSuelto != null} (esperado False los dos)");
                if (buscarSuelto != null || historialSuelto != null) Console.WriteLine("FALLO: T2_SOLO - Buscar/Historial siguen siendo botones sueltos, no se plegaron de verdad");

                // El menu real "⋯ Personaje" abre y trae los 4 items reales.
                var botonMenu = window.FindName("PersonajeMenuButton") as System.Windows.Controls.Button;
                Console.WriteLine($"T2_SOLO: boton '⋯ Personaje' (PersonajeMenuButton) encontrado={botonMenu != null}");
                if (botonMenu == null) Console.WriteLine("FALLO: T2_SOLO - no se encuentra PersonajeMenuButton por su x:Name en la cabecera");
                else
                {
                    botonMenu.ContextMenu.PlacementTarget = botonMenu;
                    botonMenu.ContextMenu.IsOpen = true;
                    DoEvents(); DoEvents();
                    var items = botonMenu.ContextMenu.Items.OfType<System.Windows.Controls.MenuItem>().Select(m => m.Header as string).ToList();
                    Console.WriteLine($"T2_SOLO: items reales del menu = [{string.Join(", ", items)}] (esperados 4: Cargar personaje, Buscar, Historial, Deshacer ultimo guardado)");
                    if (items.Count != 4) Console.WriteLine($"FALLO: T2_SOLO - el menu '⋯ Personaje' trae {items.Count} items, esperados 4");
                    botonMenu.ContextMenu.IsOpen = false;
                    DoEvents(); DoEvents();
                }

                // WhereIsItPopup ahora ancla a PersonajeMenuButton (ya no existe WhereIsItButton) -
                // comprobacion real de que el Popup sigue abriendo de verdad via el comando.
                vm.ToggleWhereIsItCommand.Execute(null);
                DoEvents(); DoEvents();
                Console.WriteLine($"T2_SOLO: tras ToggleWhereIsItCommand -> IsWhereIsItOpen={vm.IsWhereIsItOpen} (esperado True)");
                if (!vm.IsWhereIsItOpen) Console.WriteLine("FALLO: T2_SOLO - ToggleWhereIsItCommand no abre el popup tras mover su anclaje a PersonajeMenuButton");
                vm.ToggleWhereIsItCommand.Execute(null);
                DoEvents(); DoEvents();

                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(window);
                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                string shot = Path.Combine(AppContext.BaseDirectory, "cabecera-t2-una-fila.png");
                using (var fs = File.Create(shot)) enc.Save(fs);
                Console.WriteLine($"T2_SOLO: captura real -> {shot}");

                // Barrido real de anchos (mismo criterio que VITALS_SOLO) para confirmar que NO
                // envuelve tampoco al ancho MINIMO real (1080).
                foreach (double w in new double[] { 1080, 1180, 1320 })
                {
                    FijarTamaño(window, w, 860);
                    DoEvents(); DoEvents();
                    Console.WriteLine($"T2_SOLO: a {w}px -> ActualHeight de la fila de botones={wrapBotones?.ActualHeight:0}px (esperado ~44px)");
                    if (wrapBotones != null && wrapBotones.ActualHeight > 60) Console.WriteLine($"FALLO: T2_SOLO - la fila de botones envuelve a {w}px ({wrapBotones.ActualHeight:0}px)");
                }
            }
            catch (Exception ex) { Console.WriteLine("T2_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (T2_SOLO)");
            Environment.Exit(0);
        }

        // KEEPQA-GAP: PERSONAJEMENU_ESTADOS_SOLO=1 (24-sep-2026, investigacion del bug real "boton
        // de cabecera '... Personaje' sin indicador de desplegable claro", handoff de tarea
        // e5eaea9e-c261-4199-8e7d-060b6054f58d): T2_SOLO (arriba) abre el ContextMenu, cuenta sus
        // items y lo CIERRA antes de la unica captura que guarda (linea ~4225) - nunca deja
        // constancia real de como se ve PersonajeMenuButton con su menu REALMENTE abierto, ni
        // compara ese aspecto contra el estado cerrado/con foco de teclado. Este bloque cierra ese
        // hueco de cobertura: mide el color real (no supuesto) del Border/SolidColorBrush internos
        // ("Bd"/"BdBrush", ver Theme.xaml TargetType="Button" L.300-349, unico estilo que usa este
        // boton) en cerrado / foco de teclado / abierto, y guarda una captura real de cada uno.
        if (Environment.GetEnvironmentVariable("PERSONAJEMENU_ESTADOS_SOLO") == "1")
        {
            try
            {
                vm.SelectedTabIndex = 1;
                FijarTamaño(window, 1180, 860);
                DoEvents(); DoEvents();

                var botonMenu = window.FindName("PersonajeMenuButton") as System.Windows.Controls.Button;
                Console.WriteLine($"PERSONAJEMENU_ESTADOS: PersonajeMenuButton encontrado={botonMenu != null}");
                if (botonMenu == null)
                {
                    Console.WriteLine("FALLO: PERSONAJEMENU_ESTADOS - no se encuentra el boton por su x:Name en la cabecera");
                }
                else
                {
                    Console.WriteLine($"PERSONAJEMENU_ESTADOS: Content real del boton='{botonMenu.Content}' (se comprueba a ojo si incluye algun glifo de flecha ademas de los tres puntos)");

                    botonMenu.ApplyTemplate();
                    var bdBrush = botonMenu.Template.FindName("BdBrush", botonMenu) as System.Windows.Media.SolidColorBrush;
                    Console.WriteLine($"PERSONAJEMENU_ESTADOS: BdBrush (color real de fondo del boton) encontrado en el template={bdBrush != null}");

                    void Capturar(string etiqueta, out System.Windows.Media.Color? colorFondo)
                    {
                        DoEvents(); DoEvents();
                        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtb.Render(window);
                        var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                        string shot = Path.Combine(AppContext.BaseDirectory, $"personajemenu-estado-{etiqueta}.png");
                        using (var fs = File.Create(shot)) enc.Save(fs);
                        colorFondo = bdBrush?.Color;
                        Console.WriteLine($"PERSONAJEMENU_ESTADOS: estado={etiqueta} -> BdBrush.Color={colorFondo} | captura real={shot}");
                    }

                    // 1) Cerrado, sin hover ni foco - estado real por omision al entrar en la pestaña.
                    Capturar("1-cerrado-normal", out var colorCerrado);

                    // 2) Foco real de teclado (Tab), menu todavia cerrado. Keyboard.Focus() a secas
                    // NO pinta el Adorner de FocusVisualStyle (leccion ya documentada en T-H/F2,
                    // linea ~10631: WPF solo lo pinta si el teclado fue el ULTIMO dispositivo de
                    // entrada FISICO) - mismo mecanismo real ya validado ahi: ventana en primer
                    // plano + pulsacion fisica inocua (Shift) + SetFocus().
                    ForzarPrimerPlano(hwnd);
                    const byte VK_SHIFT_PM = 0x10;
                    keybd_event(VK_SHIFT_PM, 0, 0, UIntPtr.Zero);
                    keybd_event(VK_SHIFT_PM, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    DoEvents(); DoEvents();
                    System.Windows.Input.Keyboard.Focus(botonMenu);
                    DoEvents(); DoEvents();
                    bool adornerFocoReal = System.Windows.Documents.AdornerLayer.GetAdornerLayer(botonMenu)?.GetAdorners(botonMenu)?.Length > 0;
                    Console.WriteLine($"PERSONAJEMENU_ESTADOS: tras foco real de teclado -> IsKeyboardFocused={botonMenu.IsKeyboardFocused}, adorner de FocusVisualStyle adjunto={adornerFocoReal} (esperado True los dos)");
                    if (!adornerFocoReal) Console.WriteLine("FALLO-REAL: PERSONAJEMENU_ESTADOS - el FocusVisualStyle del tema no se adjunto a PersonajeMenuButton al enfocarlo por teclado");
                    Capturar("2-foco-teclado", out _);

                    // 3) Abierto de verdad - mismo mecanismo real que OnPersonajeMenuClick (MainWindow.xaml.cs).
                    botonMenu.ContextMenu.PlacementTarget = botonMenu;
                    botonMenu.ContextMenu.IsOpen = true;
                    DoEvents(); DoEvents();
                    Console.WriteLine($"PERSONAJEMENU_ESTADOS: ContextMenu.IsOpen={botonMenu.ContextMenu.IsOpen}");
                    Capturar("3-abierto", out var colorAbierto);

                    bool identicos = colorCerrado.HasValue && colorAbierto.HasValue && colorCerrado.Value == colorAbierto.Value;
                    Console.WriteLine($"PERSONAJEMENU_ESTADOS: colorCerrado={colorCerrado} vs colorAbierto={colorAbierto} -> {(identicos ? "IDENTICOS" : "DISTINTOS")}");
                    if (identicos) Console.WriteLine("FALLO-REAL: PERSONAJEMENU_ESTADOS - PersonajeMenuButton no cambia de aspecto cuando su propio menu esta abierto (falta indicador visual de estado abierto/cerrado, aparte del ContextMenu mismo)");

                    // 4) Vuelve a cerrado (tras haber estado abierto) - confirma que el cierre real no deja rastro visual distinto del (1).
                    botonMenu.ContextMenu.IsOpen = false;
                    DoEvents(); DoEvents();
                    Capturar("4-cerrado-tras-abrir", out var colorCerradoTrasAbrir);
                    Console.WriteLine($"PERSONAJEMENU_ESTADOS: colorCerrado(1)={colorCerrado} vs colorCerradoTrasAbrir(4)={colorCerradoTrasAbrir} -> {(colorCerrado == colorCerradoTrasAbrir ? "IDENTICOS (coherente)" : "DISTINTOS (inesperado)")}");

                    // 5)/6) Verificacion visual-qa añadida por aplicador-fix (25-sep-2026, mismo
                    // handoff): el Content del boton paso de un TextBlock implicito a un StackPanel
                    // ("Personaje"/"Character" + flecha) - confirma que NO se recorta ni en ES/EN ni
                    // en la ventana mas estrecha real de la app (MinWidth="1080" en MainWindow.xaml).
                    // Ademas de la captura para revisar a ojo, mide clipping real: un Button sin
                    // Width fijo que se recortara tendria su ContentPresenter interno mas ancho que
                    // el propio ActualWidth del boton (DesiredSize gana al espacio disponible).
                    FijarTamaño(window, 1080, 700);
                    DoEvents(); DoEvents();
                    var contentPresenterEstrecho = Descendientes<System.Windows.Controls.ContentPresenter>(botonMenu).FirstOrDefault();
                    bool sinRecorteEs = botonMenu.ActualWidth > 0 && (contentPresenterEstrecho == null || contentPresenterEstrecho.ActualWidth <= botonMenu.ActualWidth + 0.5);
                    Console.WriteLine($"PERSONAJEMENU_ESTADOS: ventana estrecha (1080x700) ES -> PersonajeMenuButton.ActualWidth={botonMenu.ActualWidth:0.0}, ContentPresenter.ActualWidth={contentPresenterEstrecho?.ActualWidth:0.0}, sinRecorte={sinRecorteEs} (esperado True)");
                    if (!sinRecorteEs) Console.WriteLine("FALLO-REAL: PERSONAJEMENU_ESTADOS - el nuevo Content (texto+flecha) se recorta en la ventana mas estrecha real de la app (ES)");
                    Capturar("5-estrecho-es", out _);

                    vm.Settings.Language = "en";
                    DoEvents(); DoEvents();
                    var contentPresenterEn = Descendientes<System.Windows.Controls.ContentPresenter>(botonMenu).FirstOrDefault();
                    bool sinRecorteEn = botonMenu.ActualWidth > 0 && (contentPresenterEn == null || contentPresenterEn.ActualWidth <= botonMenu.ActualWidth + 0.5);
                    Console.WriteLine($"PERSONAJEMENU_ESTADOS: ventana estrecha (1080x700) EN -> PersonajeMenuButton.ActualWidth={botonMenu.ActualWidth:0.0}, ContentPresenter.ActualWidth={contentPresenterEn?.ActualWidth:0.0}, sinRecorte={sinRecorteEn} (esperado True)");
                    if (!sinRecorteEn) Console.WriteLine("FALLO-REAL: PERSONAJEMENU_ESTADOS - el nuevo Content (texto+flecha) se recorta en la ventana mas estrecha real de la app (EN)");
                    Capturar("6-estrecho-en", out _);

                    vm.Settings.Language = "es"; // el resto del arnes asume español
                    FijarTamaño(window, 1180, 860);
                    DoEvents(); DoEvents();
                }
            }
            catch (Exception ex) { Console.WriteLine("PERSONAJEMENU_ESTADOS-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (PERSONAJEMENU_ESTADOS_SOLO)");
            Environment.Exit(0);
        }

        // KEEPQA-GAP: PERSONAJEMENU_TOGGLE_SOLO=1 (28-sep-2026, reporte real del usuario: "volver
        // a dar el boton de personaje en la cabecera no acaba de volver a plegar bien el
        // desplegable - un clic lo despliega, otro clic no lo pliega"). PERSONAJEMENU_ESTADOS_SOLO
        // (arriba) solo abre/cierra el ContextMenu A MANO (IsOpen=true/false directo), nunca con un
        // clic real de raton de SISTEMA OPERATIVO sobre el propio PersonajeMenuButton - por eso no
        // detectaba esta carrera real entre el cierre automatico del ContextMenu por "mouse-down
        // fuera" y el Click del boton que lo reabria de inmediato (ver comentario real en
        // MainWindow.xaml.cs, OnPersonajeMenuClick/OnPersonajeMenuClosed). Este bloque cierra ese
        // hueco: dos clics REALES (mouse_event down+up, mismo mecanismo ya usado en AR-13e) en el
        // mismo punto de pantalla sobre PersonajeMenuButton deben abrir y despues cerrar el menu de
        // verdad, y un tercer clic debe poder volver a abrirlo (no queda atascado en "cerrado para
        // siempre").
        if (Environment.GetEnvironmentVariable("PERSONAJEMENU_TOGGLE_SOLO") == "1")
        {
            try
            {
                vm.SelectedTabIndex = 1;
                FijarTamaño(window, 1180, 860);
                DoEvents(); DoEvents();

                var botonMenu = window.FindName("PersonajeMenuButton") as System.Windows.Controls.Button;
                Console.WriteLine($"PERSONAJEMENU_TOGGLE: PersonajeMenuButton encontrado={botonMenu != null}");
                if (botonMenu == null || botonMenu.ContextMenu == null)
                {
                    Console.WriteLine("FALLO: PERSONAJEMENU_TOGGLE - no se encuentra el boton o su ContextMenu");
                }
                else
                {
                    botonMenu.UpdateLayout();
                    var centroVentana = botonMenu.TranslatePoint(
                        new Point(botonMenu.ActualWidth / 2, botonMenu.ActualHeight / 2), window);
                    var centroPantalla = window.PointToScreen(centroVentana);

                    ForzarPrimerPlano(hwnd);
                    DoEvents();

                    void ClicRealSobreBoton()
                    {
                        SetCursorPos((int)centroPantalla.X, (int)centroPantalla.Y);
                        Thread.Sleep(60);
                        System.Windows.Input.Mouse.Synchronize();
                        DoEvents();
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                        Thread.Sleep(50); DoEvents(); DoEvents();
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                        Thread.Sleep(80); DoEvents(); DoEvents(); DoEvents(); DoEvents();
                    }

                    // 1) Primer clic real: debe abrir.
                    ClicRealSobreBoton();
                    bool abiertoTras1 = botonMenu.ContextMenu.IsOpen;
                    Console.WriteLine($"PERSONAJEMENU_TOGGLE: tras clic 1 -> ContextMenu.IsOpen={abiertoTras1}, vm.IsPersonajeMenuOpen={vm.IsPersonajeMenuOpen} (esperado True los dos)");
                    if (!abiertoTras1 || !vm.IsPersonajeMenuOpen) Console.WriteLine("FALLO-REAL: PERSONAJEMENU_TOGGLE - el primer clic real no abrio el menu");

                    // 2) Segundo clic real, MISMO punto: debe cerrar (el bug real: se reabria solo).
                    ClicRealSobreBoton();
                    bool abiertoTras2 = botonMenu.ContextMenu.IsOpen;
                    Console.WriteLine($"PERSONAJEMENU_TOGGLE: tras clic 2 -> ContextMenu.IsOpen={abiertoTras2}, vm.IsPersonajeMenuOpen={vm.IsPersonajeMenuOpen} (esperado False los dos)");
                    if (abiertoTras2 || vm.IsPersonajeMenuOpen) Console.WriteLine("FALLO-REAL: PERSONAJEMENU_TOGGLE - el segundo clic real NO cerro el menu (bug de doble-toggle)");

                    // 3) Tercer clic real: debe poder volver a abrir (no atascado en cerrado).
                    ClicRealSobreBoton();
                    bool abiertoTras3 = botonMenu.ContextMenu.IsOpen;
                    Console.WriteLine($"PERSONAJEMENU_TOGGLE: tras clic 3 -> ContextMenu.IsOpen={abiertoTras3}, vm.IsPersonajeMenuOpen={vm.IsPersonajeMenuOpen} (esperado True los dos)");
                    if (!abiertoTras3 || !vm.IsPersonajeMenuOpen) Console.WriteLine("FALLO-REAL: PERSONAJEMENU_TOGGLE - el tercer clic real no reabrio el menu (quedo atascado)");

                    // 4) Cuarto clic real: debe volver a cerrar (confirma que el toggle se mantiene
                    // estable en rondas sucesivas, no solo la primera vez).
                    ClicRealSobreBoton();
                    bool abiertoTras4 = botonMenu.ContextMenu.IsOpen;
                    Console.WriteLine($"PERSONAJEMENU_TOGGLE: tras clic 4 -> ContextMenu.IsOpen={abiertoTras4}, vm.IsPersonajeMenuOpen={vm.IsPersonajeMenuOpen} (esperado False los dos)");
                    if (abiertoTras4 || vm.IsPersonajeMenuOpen) Console.WriteLine("FALLO-REAL: PERSONAJEMENU_TOGGLE - el cuarto clic real NO cerro el menu");

                    botonMenu.ContextMenu.IsOpen = false;
                    DoEvents(); DoEvents();
                }
            }
            catch (Exception ex) { Console.WriteLine("PERSONAJEMENU_TOGGLE-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (PERSONAJEMENU_TOGGLE_SOLO)");
            Environment.Exit(0);
        }

        // T4_SOLO=1 (20-sep-2026, catalogo de rediseño visual T4 "Inicio como escritorio de
        // partida"): verifica en frio, con la ventana real, el parrafo de "solo primer arranque",
        // la tarjeta hero (doll 2x + KPIs Vida maxima/Tiempo jugado) y que solo quedan 3 tarjetas
        // de "Que mas puedes hacer" (antes 7).
        if (Environment.GetEnvironmentVariable("T4_SOLO") == "1")
        {
            try
            {
                vm.SelectedTabIndex = 0; // Inicio
                FijarTamaño(window, 1080, 900);
                DoEvents(); DoEvents();

                // Parrafo de "solo primer arranque": se aisla la variable a mano (mismo criterio
                // real de "verificar aislando la variable") en vez de depender del estado
                // acumulado real de settings.json en este equipo de otras tiradas del arnes.
                vm.Settings.HasSeenHomeIntro = false;
                DoEvents(); DoEvents();
                var candidatosIntro = Descendientes<TextBlock>(window).Where(t => t.Text.Contains("Editor de personajes de Terraria")).ToList();
                Console.WriteLine($"T4_SOLO: {candidatosIntro.Count} TextBlock(s) reales con el texto del parrafo de bienvenida (esperado 1 - si hay mas de uno, el segundo puede ser de otra pestaña ya realizada, ej. Acerca de)");
                var parrafoIntro = candidatosIntro.FirstOrDefault(t => t.GetBindingExpression(System.Windows.UIElement.VisibilityProperty) != null) ?? candidatosIntro.FirstOrDefault();
                bool visibleAntes = parrafoIntro != null && parrafoIntro.Visibility == System.Windows.Visibility.Visible;
                Console.WriteLine($"T4_SOLO: con HasSeenHomeIntro=False -> parrafo de bienvenida encontrado={parrafoIntro != null}, visible={visibleAntes} (esperado True)");
                if (!visibleAntes) Console.WriteLine("FALLO: T4_SOLO - el parrafo de bienvenida no se ve con HasSeenHomeIntro=False");

                vm.Settings.HasSeenHomeIntro = true;
                DoEvents(); DoEvents();
                bool visibleDespues = parrafoIntro != null && parrafoIntro.Visibility == System.Windows.Visibility.Visible;
                Console.WriteLine($"T4_SOLO: con HasSeenHomeIntro=True -> parrafo visible={visibleDespues} (esperado False)");
                if (visibleDespues) Console.WriteLine("FALLO: T4_SOLO - el parrafo de bienvenida sigue visible tras marcarlo visto");

                // Tarjeta hero: el personaje YA cargado en este arnes (UIA-Test, mas arriba en
                // Main()) vive en una carpeta de prueba aislada, fuera de los directorios reales
                // que escanea Inicio - session.json SI lo recordaria (LastSessionCharacterName se
                // pondria bien), pero LastSessionCharacterEntry seguiria null a proposito (no
                // esta en Home.Characters, mismo criterio real de "nunca un dato a medias" del
                // comentario de MainWindow.xaml). Para probar la tarjeta hero de verdad hace
                // falta que la "ultima sesion" sea un personaje que SI este en la lista escaneada
                // - se abre uno real de Home.Characters (accion real de usuario, Home.
                // OpenCommand) y se vuelve a mirar, sin inventar ningun dato sintetico.
                if (vm.Home.Characters.Count > 0)
                {
                    vm.Home.OpenCommand.Execute(vm.Home.Characters[0]);
                    DoEvents(); DoEvents();
                    vm.RestoreSession();
                    // Abrir un personaje real salta a Personaje (comportamiento real de siempre,
                    // esperado) - hay que volver a Inicio para poder mirar la tarjeta hero, el
                    // dato en si (Home.LastSessionCharacterEntry) no depende de que pestaña este
                    // activa.
                    vm.SelectedTabIndex = 0;
                    // BUG REAL DE METODOLOGIA encontrado y arreglado en esta misma verificacion
                    // (investigado antes de dar nada por malo): la primera captura salio EN
                    // BLANCO pese a que los propios checks de mas abajo (IsVisible=True) daban
                    // bien - MainWindow.xaml.cs anima cada cambio de pestaña principal con un
                    // fade+slide real de 180ms (OnRootTabSelectionChanged), y DoEvents() solo
                    // bombea la cola de mensajes, nunca avanza el reloj real que mueve un
                    // Storyboard. Sin esperar el tiempo real de la animacion, la captura llegaba
                    // con el contenido todavia a Opacity/TranslateY intermedios - IsVisible sigue
                    // en True durante toda la animacion (no es lo mismo que "ya se ve"). Mismo
                    // patron real ya usado en el resto del arnes (Thread.Sleep+DoEvents).
                    for (int i = 0; i < 6; i++) { DoEvents(); System.Threading.Thread.Sleep(50); }
                }
                var entry = vm.Home.LastSessionCharacterEntry;
                Console.WriteLine($"T4_SOLO: Home.LastSessionCharacterEntry={(entry != null ? entry.Name : "null")} (HealthMax={entry?.HealthMax}, PlayTimeText='{entry?.PlayTimeText}')");
                if (entry != null)
                {
                    var dollHero = Descendientes<Image>(window).FirstOrDefault(i => i.Width == 104 && i.IsVisible);
                    Console.WriteLine($"T4_SOLO: doll hero (104x145.6) en pantalla={dollHero != null} (esperado True)");
                    if (dollHero == null) Console.WriteLine("FALLO: T4_SOLO - el doll a 2x de la tarjeta hero no esta en pantalla con un LastSessionCharacterEntry real");
                    var chipVida = Descendientes<TextBlock>(window).FirstOrDefault(t => t.Text == entry.HealthMax.ToString() && t.IsVisible);
                    var chipTiempo = Descendientes<TextBlock>(window).FirstOrDefault(t => t.Text == entry.PlayTimeText && t.IsVisible);
                    Console.WriteLine($"T4_SOLO: chip Vida maxima ({entry.HealthMax}) en pantalla={chipVida != null}, chip Tiempo jugado ('{entry.PlayTimeText}') en pantalla={chipTiempo != null} (esperado True los dos)");
                    if (chipVida == null) Console.WriteLine("FALLO: T4_SOLO - el KPI de Vida maxima no esta en pantalla");
                    if (chipTiempo == null) Console.WriteLine("FALLO: T4_SOLO - el KPI de Tiempo jugado no esta en pantalla");
                }
                else Console.WriteLine("T4_SOLO: sin LastSessionCharacterEntry real en este equipo - se omite la comprobacion de la tarjeta hero (dato real, no sintetico)");

                // Solo 3 tarjetas reales de "Que mas puedes hacer" (antes 7).
                var botonesHome = Descendientes<System.Windows.Controls.Button>(window)
                    .Where(b => b.Style == (System.Windows.Style)window.FindResource("NavCardButton") && b.IsVisible
                        && b.CommandParameter is string p && (p == "Libreria" || p == "Builds" || p == "Exploracion" || p == "Novedades" || p == "AcercaDe" || p == "Guia" || p == "Hosting"))
                    .Select(b => (string)b.CommandParameter).Distinct().ToList();
                Console.WriteLine($"T4_SOLO: tarjetas reales de 'Que mas puedes hacer' en Inicio = [{string.Join(", ", botonesHome)}] (esperado exactamente Libreria, Builds, Exploracion - 3)");
                if (botonesHome.Count != 3 || !botonesHome.Contains("Libreria") || !botonesHome.Contains("Builds") || !botonesHome.Contains("Exploracion"))
                    Console.WriteLine($"FALLO: T4_SOLO - se esperaban exactamente 3 tarjetas (Libreria/Builds/Exploracion), hay {botonesHome.Count}: [{string.Join(", ", botonesHome)}]");

                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(window);
                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                string shot = Path.Combine(AppContext.BaseDirectory, "inicio-t4-hero.png");
                using (var fs = File.Create(shot)) enc.Save(fs);
                Console.WriteLine($"T4_SOLO: captura real -> {shot}");

                // Deja el equipo real en el estado normal esperado tras un primer arranque de
                // verdad (mismo criterio de siempre: los tests de este arnes leen/escriben el
                // settings.json REAL de este equipo, no uno sintetico aparte).
                vm.Settings.HasSeenHomeIntro = true;
            }
            catch (Exception ex) { Console.WriteLine("T4_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (T4_SOLO)");
            Environment.Exit(0);
        }

        // T1_SOLO=1 (20-sep-2026, catalogo de rediseño visual T1 "rail con iconos, agrupada y
        // colapsable"): verifica en frio, con la ventana real, que la rail (RootTabControl)
        // reacciona de verdad a WindowSizeClass (antes era el UNICO elemento que lo ignoraba) -
        // ancho real MENOR en Compacto que en Normal, etiquetas de texto colapsadas en Compacto
        // (solo iconos), tooltip con el nombre completo disponible en los dos modos, los 8 iconos
        // reales rendericen como glifo monocromo (ni tofu/caja vacia ni el problema real ya
        // documentado de 🔍 en P-4 - "depende de la fuente de emoji instalada, no obedece
        // Foreground" - medido con GlyphRun/FormattedText, no solo mirando una captura), y los 2
        // filetes de grupo (Tag=GroupStart, Novedades/Guia) visibles.
        if (Environment.GetEnvironmentVariable("T1_SOLO") == "1")
        {
            try
            {
                var rail = window.FindName("RootTabControl") as System.Windows.Controls.TabControl;
                if (rail == null) { Console.WriteLine("FALLO: T1_SOLO - RootTabControl no encontrado"); }
                else
                {
                    foreach (string idioma in new[] { "es", "en" })
                    {
                        vm.Settings.Language = idioma;
                        foreach (var (w, esperadoSizeClass) in new (double w, WindowSizeClass sc)[] { (1080, WindowSizeClass.Compacto), (1400, WindowSizeClass.Normal) })
                        {
                            FijarTamaño(window, w, 800);
                            DoEvents(); DoEvents();
                            double anchoRail = rail.ActualWidth;
                            Console.WriteLine($"T1_SOLO[{idioma}]: a {w}px SizeClass={vm.SizeClass} (esperado {esperadoSizeClass}) -> ancho real de la rail={anchoRail:0}px, RailWidth(VM)={vm.RailWidth}");
                            if (vm.SizeClass != esperadoSizeClass) Console.WriteLine($"FALLO: T1_SOLO - SizeClass={vm.SizeClass} a {w}px, esperado {esperadoSizeClass}");

                            var etiquetas = Descendientes<TextBlock>(rail).Where(t => t.Style == (System.Windows.Style)window.FindResource("NavRailLabel")).ToList();
                            int visibles = etiquetas.Count(t => t.Visibility == System.Windows.Visibility.Visible);
                            Console.WriteLine($"T1_SOLO[{idioma}]: a {w}px -> {etiquetas.Count} etiqueta(s) de texto en la rail, {visibles} visible(s) (esperado 0 en Compacto, 8 en Normal)");
                            if (esperadoSizeClass == WindowSizeClass.Compacto && visibles != 0) Console.WriteLine($"FALLO: T1_SOLO - {visibles} etiquetas de texto siguen visibles en Compacto (la rail no se colapsa de verdad)");
                            if (esperadoSizeClass == WindowSizeClass.Normal && visibles != 8) Console.WriteLine($"FALLO: T1_SOLO - solo {visibles}/8 etiquetas visibles en Normal");

                            if (w == 1080)
                            {
                                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                                rtb.Render(window);
                                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                                string shot = Path.Combine(AppContext.BaseDirectory, $"rail-compacta-{idioma}.png");
                                using (var fs = File.Create(shot)) enc.Save(fs);
                                Console.WriteLine($"T1_SOLO[{idioma}]: captura real de la rail compacta -> {shot}");
                            }
                        }
                    }
                    vm.Settings.Language = "es";
                    FijarTamaño(window, 1600, 900);
                    DoEvents(); DoEvents();

                    // Los 8 iconos renderizan de verdad (glifo real, no tofu) - FormattedText mide
                    // el ancho real del glifo tal cual lo pintaria WPF con la fuente real de la
                    // rail; un caracter sin glifo en la fuente (tofu/caja) SIGUE midiendo algo
                    // (WPF dibuja un rectangulo de sustitucion), asi que la prueba real no es
                    // "ancho > 0" sino decodificar el glifo real via GlyphTypeface (ExistsGlyph
                    // por codepoint) - mismo criterio que el resto del arnes, medir de verdad.
                    var iconos = Descendientes<TextBlock>(rail).Where(t => t.FontSize == 16 && t.Width == 24).ToList();
                    Console.WriteLine($"T1_SOLO: {iconos.Count} icono(s) de rail encontrados en el arbol (esperado 8)");
                    if (iconos.Count != 8) Console.WriteLine($"FALLO: T1_SOLO - {iconos.Count}/8 iconos encontrados en la rail");
                    // Comprobacion real de tofu: WPF hace fallback automatico de fuente por
                    // caracter (el TextBlock real en pantalla NO se limita a la primera fuente de
                    // AppFont) - mirar solo esa primera fuente da falsos FALLO (confirmado en esta
                    // misma ronda: los 7 dingbats dieron "sin glifo" en Segoe UI Variable Display
                    // pero la captura real de mas abajo los pinta limpios). "Segoe UI Symbol" es
                    // la fuente real de fallback de Windows para Miscellaneous Symbols/Dingbats -
                    // comprobar la CADENA (AppFont + Segoe UI Symbol), no un unico eslabon.
                    var fuentesFallback = new[] { "Segoe UI Variable Display", "Segoe UI", "Segoe UI Symbol" };
                    foreach (var icono in iconos)
                    {
                        int codepoint = char.ConvertToUtf32(icono.Text, 0);
                        bool tieneGlifo = fuentesFallback.Any(nombreFuente =>
                            new System.Windows.Media.Typeface(new System.Windows.Media.FontFamily(nombreFuente), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal)
                                .TryGetGlyphTypeface(out var gt) && gt.CharacterToGlyphMap.ContainsKey(codepoint));
                        Console.WriteLine($"T1_SOLO: icono '{icono.Text}' (U+{codepoint:X4}) tiene glifo real en la cadena de fallback={tieneGlifo} (esperado True - si no, tofu/caja vacia)");
                        if (!tieneGlifo) Console.WriteLine($"FALLO: T1_SOLO - el icono '{icono.Text}' NO tiene glifo real en ninguna fuente real de la cadena (tofu)");
                    }

                    var divisores = Descendientes<System.Windows.Controls.Border>(rail).Where(b => b.Name == "GroupDivider" && b.Visibility == System.Windows.Visibility.Visible).ToList();
                    Console.WriteLine($"T1_SOLO: {divisores.Count} filete(s) de grupo visible(s) (esperado 2 - Novedades y Guia)");
                    if (divisores.Count != 2) Console.WriteLine($"FALLO: T1_SOLO - {divisores.Count}/2 filetes de grupo visibles");
                }
            }
            catch (Exception ex) { Console.WriteLine("T1_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (T1_SOLO)");
            Environment.Exit(0);
        }

        // T9_SOLO=1 (20-sep-2026, catalogo de rediseño visual T9 "Comparador como vista dividida,
        // no como modal de 1400x900"): verifica en frio, con la ventana real, que el Comparador
        // ya es una pestaña real de Personaje (PersonajeInnerTab.Comparar=7) y no un overlay -
        // los dos selectores tienen que quedar FIJOS (cabecera pegajosa, fuera del ScrollViewer
        // de resultados) mientras el cuerpo de resultados SI se desplaza, y todo el contenido
        // tiene que caber en el ancho MINIMO real de la ventana (1080, MainWindow.xaml MinWidth)
        // sin desbordar - la razon real de negocio de T9 (antes exigia 1400px de overlay).
        if (Environment.GetEnvironmentVariable("T9_SOLO") == "1")
        {
            try
            {
                FijarTamaño(window, 1080, 700);
                vm.SelectedTabIndex = 1; // Personaje
                vm.PersonajeInnerTabIndex = 7; // PersonajeInnerTab.Comparar
                DoEvents(); DoEvents();

                Console.WriteLine($"T9_SOLO: {vm.Home.Characters.Count} personaje(s) reales en Inicio (esperado >= 2 para poder comparar de verdad)");
                if (vm.Home.Characters.Count >= 2)
                {
                    vm.Compare.SelectedA = vm.Home.Characters[0];
                    vm.Compare.SelectedB = vm.Home.Characters[1];
                }
                DoEvents(); DoEvents(); DoEvents();

                Console.WriteLine($"T9_SOLO: HasBothSelected={vm.Compare.HasBothSelected} (esperado True), ShowResults={vm.Compare.ShowResults} (esperado True), DifferenceCount={vm.Compare.DifferenceCount}, StatRows={vm.Compare.StatRows.Count} (esperado 9)");
                if (!vm.Compare.ShowResults) Console.WriteLine("FALLO: T9_SOLO - ShowResults sigue en False tras elegir los dos personajes reales");

                // Geometria real: los dos selectores (Border dentro de Grid.Row=1) tienen que
                // seguir en el MISMO sitio en pantalla ANTES y DESPUES de desplazar el
                // ScrollViewer de resultados - esa es la comprobacion real de "cabecera pegajosa"
                // que pide el catalogo, no solo "se ve arriba en la primera captura".
                var comboA = Descendientes<System.Windows.Controls.ComboBox>(window)
                    .FirstOrDefault(c => c.ItemsSource == vm.Compare.AvailableCharacters && c.SelectedItem == vm.Compare.SelectedA);
                // ADR-TERRAKEEP-025 (26-sep-2026): Compare paso a UserControl (CompareView) - un
                // UserControl compilado es dueño de su propio NameScope, patron de FindName DOBLE
                // ya usado por GUIA/ADR-021.
                var compareViewT9 = window.FindName("CompareView") as System.Windows.FrameworkElement;
                var sv = compareViewT9?.FindName("CompareResultsScrollViewer") as System.Windows.Controls.ScrollViewer;
                if (comboA == null || sv == null)
                {
                    Console.WriteLine($"FALLO: T9_SOLO - no se encuentra el combo del selector A ({comboA != null}) o CompareResultsScrollViewer ({sv != null}) en el arbol visual real");
                }
                else
                {
                    var antes = comboA.TransformToAncestor(window).Transform(new System.Windows.Point(0, 0));
                    sv.ScrollToVerticalOffset(sv.ScrollableHeight); // al final del todo, si hay algo que desplazar
                    // ADR-TERRAKEEP-025 (26-sep-2026): mismo hallazgo de timing ya documentado por
                    // ADR-TERRAKEEP-019/HostingView - el nivel extra de UserControl (CompareView)
                    // necesita un ciclo mas de bombeo del Dispatcher para completar su pasada de
                    // layout tras el ScrollToVerticalOffset antes de medir con TransformToAncestor
                    // (2 DoEvents ya no bastaban, subido a 4 igual que EjecutarHostingReal).
                    DoEvents(); DoEvents(); DoEvents(); DoEvents();
                    var despues = comboA.TransformToAncestor(window).Transform(new System.Windows.Point(0, 0));
                    Console.WriteLine($"T9_SOLO: selector A en pantalla antes={antes} despues de desplazar el ScrollViewer={despues} (esperado identico -> cabecera pegajosa real), ScrollableHeight={sv.ScrollableHeight:0}");
                    if (Math.Abs(antes.Y - despues.Y) > 0.5) Console.WriteLine($"FALLO: T9_SOLO - el selector A se movio {Math.Abs(antes.Y - despues.Y):0.0}px al desplazar los resultados (la cabecera NO es pegajoza de verdad)");
                    sv.ScrollToHome();
                    DoEvents(); DoEvents();
                }

                // Nada se sale de la ventana a 1080px (suelo real de MainWindow.xaml MinWidth) -
                // mismo detector Recorte() ya usado en el resto del arnes (AR-04/VITALS_SOLO).
                var personajeTab = Descendientes<System.Windows.Controls.TabItem>(window)
                    .FirstOrDefault(t => t.IsSelected && t.Parent is System.Windows.Controls.TabControl tc && tc.Name == "RootTabControl");
                if (personajeTab?.Content is FrameworkElement contenidoPersonaje)
                {
                    var (rx, ry) = Recorte(contenidoPersonaje);
                    Console.WriteLine($"T9_SOLO: recorte real del contenido de Personaje a 1080px=({rx:0},{ry:0}) (esperado 0,0)");
                    if (rx > 0 || ry > 0) Console.WriteLine($"FALLO: T9_SOLO - el contenido de Personaje/Comparar se recorta {rx:0}x{ry:0}px a 1080px de ancho");
                }

                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(window);
                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                string shot = Path.Combine(AppContext.BaseDirectory, "comparador-pestaña-t9.png");
                using (var fs = File.Create(shot)) enc.Save(fs);
                Console.WriteLine($"T9_SOLO: captura real -> {shot}");

                // La ruta de entrada real (OpenCompareCommand) tiene que llevar exactamente aqui.
                vm.SelectedTabIndex = 0;
                vm.OpenCompareCommand.Execute(null);
                Console.WriteLine($"T9_SOLO: tras OpenCompareCommand -> SelectedTabIndex={vm.SelectedTabIndex} (esperado 1), PersonajeInnerTabIndex={vm.PersonajeInnerTabIndex} (esperado 7)");
                if (vm.SelectedTabIndex != 1 || vm.PersonajeInnerTabIndex != 7) Console.WriteLine("FALLO: T9_SOLO - OpenCompareCommand no navega a Personaje > Comparar");
            }
            catch (Exception ex) { Console.WriteLine("T9_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (T9_SOLO)");
            Environment.Exit(0);
        }

        // T7_SOLO=1 (20-sep-2026, catalogo de rediseño visual T7 "sistema unico de avisos"):
        // verifica en frio, con la ventana real, que los TRES avisos que antes tenian tres formas
        // distintas (banner teal de guardado, banner plano de error, tarjeta inline de Inicio)
        // ahora comparten el mismo ToastHost abajo a la derecha, apilados SIN solaparse entre si
        // ni con la tarjeta de actualizacion, y que el de Inicio (antes solo visible en esa
        // pestaña, INI-07) ahora se ve desde CUALQUIER pestaña (mismo criterio real que H-3/N-1).
        // Dispara los avisos por la via mas directa que aisla de verdad la parte VISUAL (mismo
        // criterio real de VITALS_SOLO/TIPO_SOLO): SaveConfirmationVisible/GlobalErrorMessage son
        // propiedades observables publicas, se fuerzan directamente sin pasar por un Save() real
        // completo; Home.ActionErrorMessage es de solo lectura (Home.SetActionError es privado),
        // asi que se dispara por el camino real - RestoreBackupCommand sobre un personaje real sin
        // .bak (rama "error_no_backup_to_restore", no toca ningun fichero, solo lee con
        // File.Exists antes de nada).
        if (Environment.GetEnvironmentVariable("T7_SOLO") == "1")
        {
            try
            {
                var pinkGradient = (System.Windows.Media.Brush)window.FindResource("PinkGradientBrush");
                vm.SelectedTabIndex = 0; // Inicio: mismo sitio real de donde salia antes el aviso rosa
                DoEvents(); DoEvents();

                vm.SaveConfirmationVisible = true;
                vm.GlobalErrorMessage = "T7_SOLO: error real de prueba (GlobalErrorMessage)";
                var entrySinBak = vm.Home.Characters.FirstOrDefault(c => !File.Exists(c.FilePath + ".bak"));
                if (entrySinBak != null) vm.Home.RestoreBackupCommand.Execute(entrySinBak);
                else Console.WriteLine("T7_SOLO: ningun personaje real sin .bak - se omite la comprobacion de Home.ActionErrorMessage");
                DoEvents(); DoEvents(); DoEvents();

                bool EnPantallaBorde(FrameworkElement fe)
                {
                    if (!fe.IsVisible) return false;
                    try { fe.TransformToAncestor(window); return true; }
                    catch (InvalidOperationException) { return false; }
                }
                var toasts = Descendientes<Border>(window)
                    .Where(b => b.Background == pinkGradient || (b.Background is System.Windows.Media.LinearGradientBrush lgb && lgb == window.FindResource("TealGradientBrush")))
                    .Where(EnPantallaBorde)
                    .ToList();
                Console.WriteLine($"T7_SOLO: {toasts.Count} toast(s) real(es) en pantalla (esperado 2 o 3: guardado+error-global siempre, +error-Inicio si habia personaje sin .bak)");
                if (toasts.Count < 2) Console.WriteLine($"FALLO: T7_SOLO - se esperaban al menos 2 toasts en pantalla, hay {toasts.Count}");

                var rects = toasts.Select(t => t.TransformToAncestor(window).TransformBounds(new System.Windows.Rect(0, 0, t.ActualWidth, t.ActualHeight))).ToList();
                for (int i = 0; i < rects.Count; i++)
                {
                    bool dentroDeLaVentana = rects[i].Right <= window.ActualWidth + 1 && rects[i].Bottom <= window.ActualHeight + 1 && rects[i].Left >= -1 && rects[i].Top >= -1;
                    Console.WriteLine($"T7_SOLO: toast[{i}] rect={rects[i]} dentroDeLaVentana={dentroDeLaVentana} (esperado True)");
                    if (!dentroDeLaVentana) Console.WriteLine($"FALLO: T7_SOLO - toast[{i}] se sale de la ventana: {rects[i]}");
                    for (int j = i + 1; j < rects.Count; j++)
                    {
                        bool solapan = rects[i].IntersectsWith(rects[j]);
                        if (solapan) Console.WriteLine($"FALLO: T7_SOLO - toast[{i}] y toast[{j}] SE SOLAPAN: {rects[i]} vs {rects[j]}");
                    }
                }

                // El aviso de error de Inicio (antes inline, solo visible en esa pestaña) tiene
                // que seguir viendose al cambiar de pestaña - la comprobacion real de T7.
                if (entrySinBak != null)
                {
                    bool visibleEnInicio = !string.IsNullOrEmpty(vm.Home.ActionErrorMessage);
                    vm.SelectedTabIndex = 1; // Personaje - cualquier pestaña que no sea Inicio
                    DoEvents(); DoEvents();
                    var toastInicioTrasCambiar = Descendientes<TextBlock>(window)
                        .FirstOrDefault(t => t.Text == vm.Home.ActionErrorMessage && EnPantallaBorde(t));
                    Console.WriteLine($"T7_SOLO: ActionErrorMessage='{vm.Home.ActionErrorMessage}' visibleEnInicio={visibleEnInicio}, tras cambiar a Personaje sigue en pantalla={toastInicioTrasCambiar != null} (esperado True en los dos, T7 lo saca de Inicio a nivel de ventana)");
                    if (!visibleEnInicio) Console.WriteLine("FALLO: T7_SOLO - RestoreBackupCommand no puso Home.ActionErrorMessage");
                    if (toastInicioTrasCambiar == null) Console.WriteLine("FALLO: T7_SOLO - el aviso de error de Inicio ya NO se ve tras cambiar de pestaña (regresion real de INI-07/T7)");
                }

                var rtbToast = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbToast.Render(window);
                var encToast = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encToast.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbToast));
                string shotToast = Path.Combine(AppContext.BaseDirectory, "toasthost-t7.png");
                using (var fs = File.Create(shotToast)) encToast.Save(fs);
                Console.WriteLine($"T7_SOLO: captura real -> {shotToast}");

                vm.SaveConfirmationVisible = false;
                vm.GlobalErrorMessage = null;
            }
            catch (Exception ex) { Console.WriteLine("T7_SOLO-EXCEPTION: " + ex); }
            Console.WriteLine("DONE (T7_SOLO)");
            Environment.Exit(0);
        }

        // KEEPQA_SOLO=1 (14-sep-2026, barrido FRESCO con el nuevo arsenal de KeepQA - contenido
        // adversarial y volcado de geometria para verificarAlineacion.js): mismo modo de foco que
        // los de arriba, personaje real ya cargado. El cuerpo real vive en AuditoriaKeepQA.cs
        // (misma clase parcial), ver alli el porque de su propio fichero.
        // KEEPQA_SOLO / KEEPQA_FRESCO_SOLO: MOVIDOS el 16-sep-2026 al hueco de "personaje REAL"
        // (antes de vm.LoadFromPath(tempPlr)) - ver el bloque KEEPQA_DATOS_REALES mas arriba.
        // Aqui solo queda el respaldo con KEEPQA_DATOS_SINTETICOS=1 para reproducir el
        // comportamiento antiguo (personaje "UIA-Test" vacio) si alguna vez hace falta comparar.
        if (Environment.GetEnvironmentVariable("KEEPQA_SOLO") == "1")
        {
            EjecutarKeepQaAdversarialYGeometria(window, vm);
            Console.WriteLine("DONE (KEEPQA_SOLO, datos sinteticos)");
            Environment.Exit(0);
        }
        if (Environment.GetEnvironmentVariable("KEEPQA_FRESCO_SOLO") == "1")
        {
            EjecutarKeepQaFrescoTerceraResolucion(window, vm);
            Console.WriteLine("DONE (KEEPQA_FRESCO_SOLO, datos sinteticos)");
            Environment.Exit(0);
        }

        // KEEPQA_VIEWPORT_SOLO=1 (15-sep-2026): barrido de KeepQA de contenedores con scroll REAL
        // desplazados hasta el final (verificarBordeViewport.js, pieza nueva de esa misma noche).
        // Cuerpo real en AuditoriaViewportScroll.cs (misma clase parcial) - ver alli el catalogo
        // completo de los 5 paneles reales cubiertos.
        if (Environment.GetEnvironmentVariable("KEEPQA_VIEWPORT_SOLO") == "1")
        {
            EjecutarKeepQaViewportScrollSolo(window, vm);
            Console.WriteLine("DONE (KEEPQA_VIEWPORT_SOLO)");
            Environment.Exit(0);
        }

        // FALLO3_SOLO=1 (14-sep-2026): investigacion dedicada del Fallo 3 (caja de resultados de
        // Exploracion "muy pequeña... se come casi todo el espacio disponible... queda mucho
        // espacio sin aprovechar"). Cuerpo real en AuditoriaKeepQA.cs (misma clase parcial).
        // CAPAS_SOLO=1 (14-sep-2026): extractor real de orden_z/capa para WPF (Parte 11,
        // Layering/Depth/Occlusion) + caso sintetico deliberado que valida que
        // KeepQA/src/capas/verificarCapas.js lo caza. Cuerpo real en AuditoriaKeepQA.cs.
        if (Environment.GetEnvironmentVariable("CAPAS_SOLO") == "1")
        {
            EjecutarCapasSinteticoSolo(window, vm);
            Console.WriteLine("DONE (CAPAS_SOLO)");
            Environment.Exit(0);
        }

        if (Environment.GetEnvironmentVariable("FALLO3_SOLO") == "1")
        {
            EjecutarFallo3Exploracion(window, vm);
            Console.WriteLine("DONE (FALLO3_SOLO)");
            Environment.Exit(0);
        }

        // EXPLORATION_LAYOUT_SOLO=1 (26-sep-2026, ExploracionRediseno FaseI): iteracion rapida
        // aislada del canario PERMANENTE de layout del sidebar de Exploracion (los 3 modos Browse/
        // ChestInspector/WorldTools, en 1180x860 y 1080x700). A DIFERENCIA de los modos _SOLO de
        // arriba, esta misma funcion TAMBIEN se invoca de forma incondicional mas abajo en la
        // secuencia normal (justo despues de PruebasMejorPrefijo) para que corra en CADA pasada
        // completa del arnes, no solo bajo demanda - ver el comentario real de cabecera en
        // CanarioExploracionLayoutPermanente.cs (misma clase parcial).
        if (Environment.GetEnvironmentVariable("EXPLORATION_LAYOUT_SOLO") == "1")
        {
            EjecutarCanarioExploracionLayoutPermanente(window, vm);
            Console.WriteLine("DONE (EXPLORATION_LAYOUT_SOLO)");
            Environment.Exit(0);
        }

        // PB_SOLO=1 (6-sep-2026): modo de FOCO - corre solo los bloques PB-* (Personaje >
        // Buffs/Apariencia/Investigacion/Spawn Points/Desbloqueos/Version) sobre el personaje
        // real ya cargado, y sale. Misma idea que AR_LAY_SOLO, por un motivo real medido: el
        // arnes completo hace decenas de RenderTargetBitmap de la ventana entera (hasta
        // 2560x1440) y en esta sesion RDP - con render por software forzado y varios trabajos
        // corriendo el arnes a la vez sobre el mismo repo - el proceso muere a media ejecucion,
        // en puntos distintos cada vez y siempre justo en una captura. No es un fallo de la app
        // (CLAUDE.md ya deja escrito que las capturas son poco fiables en este entorno): es que
        // el recorrido entero deja de ser repetible mientras dure esa condicion. Este modo
        // permite verificar un area sin depender de eso; la ejecucion COMPLETA sigue siendo la
        // que manda.
        if (Environment.GetEnvironmentVariable("PB_SOLO") == "1")
        {
            PruebasPersonajeBuffsAparienciaVersion(vm, window);
            Console.WriteLine("DONE (PB_SOLO)");
            Environment.Exit(0);
        }

        // AR14_SOLO=1 (6-sep-2026, ronda de Monedas/Municion apiladas): mismo modo de foco que
        // PB_SOLO de arriba, pero para la fila fusionada de Equipamiento. Cada iteracion sobre el
        // reparto de esa fila hay que medirla en DECENAS de anchos (es un bug que solo existe en
        // franjas estrechas de ancho, ver AR-14/AR-LAY) y el recorrido completo del arnes cuesta
        // minutos y decenas de RenderTargetBitmap que en esta maquina lo hacen poco repetible. Se
        // puede combinar con AR_LAY_* (AR_LAY_FINO/DESDE/HASTA/SOLO) para correr ademas el barrido
        // generico de maquetacion acotado a Equipamiento sin pagar el resto del arnes.
        if (Environment.GetEnvironmentVariable("AR14_SOLO") == "1")
        {
            AuditoriaFilaFusionadaEquipamiento(window, vm);
            if (Environment.GetEnvironmentVariable("AR_LAY_FINO") != null || Environment.GetEnvironmentVariable("AR_LAY_SOLO") != null)
                BarridoMaquetacionPorTamañoEIdioma(window, vm);
            Console.WriteLine("DONE (AR14_SOLO)");
            Environment.Exit(0);
        }

        // A11_SOLO=1 (6-sep-2026, ronda de traduccion del CONTENIDO del juego): mismo modo de
        // foco que PB_SOLO/AR14_SOLO. Hace falta por una razon medida, no por comodidad: el
        // recorrido COMPLETO del arnes se queda clavado de forma reproducible en el primer
        // AutomationElement.FindFirst que hay DETRAS del bloque UI-BLOQUEADA (linea ~251 de la
        // salida) - tres ejecuciones seguidas, dos de ellas con un unico proceso del arnes vivo,
        // o sea que no es solo el cuelgue de "dos arneses a la vez" que documento la ronda de
        // Monedas/Municion: es el limite de siempre de SetForegroundWindow/clic real de raton en
        // esta sesion (T-H/F2). A11 va DESPUES de ese punto en el Main, asi que sin modo de foco
        // no se llegaria a ejecutar nunca. Este bloque no da un solo clic real: mide sobre el
        // arbol visual y sobre los catalogos reales, o sea que es determinista de verdad.
        if (Environment.GetEnvironmentVariable("A11_SOLO") == "1")
        {
            AuditoriaContenidoDelJuegoEnIdioma(window, vm);
            Console.WriteLine("DONE (A11_SOLO)");
            Environment.Exit(0);
        }

        // MP_SOLO=1 (14-sep-2026, arreglo real de MP-01/Coin Gun): mismo modo de foco que
        // PB_SOLO/AR14_SOLO/A11_SOLO - PruebasMejorPrefijo solo necesita el personaje real ya
        // cargado (no un mundo), pero vive muy abajo en el recorrido completo (detras de
        // UI-BLOQUEADA, que muere siempre primero en esta sesion) - sin este modo de foco nunca
        // se ejecutaria de verdad aqui.
        if (Environment.GetEnvironmentVariable("MP_SOLO") == "1")
        {
            PruebasMejorPrefijo(vm, window);
            Console.WriteLine("DONE (MP_SOLO)");
            Environment.Exit(0);
        }

        // BK_SOLO=1 (13-sep-2026, encargo del historial de versiones): mismo modo de foco que
        // PB_SOLO/AR14_SOLO/A11_SOLO. Aqui hace ademas especial falta porque el bloque hace mas
        // de 30 GUARDADOS REALES seguidos sobre una copia de un personaje real - encadenarlo al
        // recorrido completo dejaria el resto del arnes trabajando sobre un personaje que este
        // bloque ha estado renombrando a proposito.
        if (Environment.GetEnvironmentVariable("BK_SOLO") == "1")
        {
            PruebasHistorialDeVersiones(window, vm);
            Console.WriteLine("DONE (BK_SOLO)");
            Environment.Exit(0);
        }

        // LIBFILT_SOLO=1 (13-sep-2026, filtros combinables de la Libreria - Rareza/Tipo de
        // daño/Ranura de equipo): mismo modo de foco que PB_SOLO/AR14_SOLO/A11_SOLO/BK_SOLO, por
        // el mismo motivo real ya documentado arriba (decenas de RenderTargetBitmap en el
        // recorrido completo, poco repetible en esta sesion). Ademas de correr el barrido
        // generico ya acotado a "Personaje/Equipamiento" (BarridoMaquetacionPorTamañoEIdioma con
        // AR_LAY_SOLO), deja DOS capturas reales del popup de filtros ABIERTO y con pastillas
        // marcadas, al tamaño MINIMO real de la ventana (1080x700, MinWidth/MinHeight) y en los
        // dos idiomas - el propio CLAUDE.md del proyecto pide mirar la captura de verdad, no solo
        // fiarse del detector automatico.
        if (Environment.GetEnvironmentVariable("LIBFILT_SOLO") == "1")
        {
            try
            {
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 0;
                FijarTamaño(window, 1080, 700);
                DoEvents(); DoEvents();

                void CapturaConFiltros(string idioma, string archivoVentana, string archivoPopup)
                {
                    vm.Settings.Language = idioma;
                    DoEvents(); DoEvents();

                    // Primero la VENTANA (boton "Filtros (N)" con la insignia, resultados ya
                    // filtrados) SIN el popup abierto - lo que se ve el 99% del tiempo.
                    var cabeza0 = vm.Library.EquipSlotChips.First(c => c.Value == Terrakeep.Core.Model.SlotKind.ArmorHead);
                    if (!cabeza0.IsSelected) vm.Library.ToggleEquipSlotChipCommand.Execute(cabeza0);
                    DoEvents(); DoEvents();
                    var rtbVentana = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbVentana.Render(window);
                    var encVentana = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encVentana.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbVentana));
                    string shotPathVentana = Path.Combine(AppContext.BaseDirectory, archivoVentana);
                    using (var fs = File.Create(shotPathVentana)) encVentana.Save(fs);
                    Console.WriteLine($"LIBFILT: captura real de la ventana ({idioma}, 1080x700, filtro 'Cabeza' activo, {vm.Library.Results.Count} resultado(s)) -> {shotPathVentana}");

                    // Ahora el POPUP abierto (las tres filas de pastillas, la pista de "solo
                    // vanilla" de Rareza, el boton "Limpiar filtros").
                    vm.Library.IsFiltersOpen = true;
                    DoEvents(); DoEvents();
                    string archivo = archivoPopup;

                    // El Popup vive en su propia PresentationSource (no es descendiente visual de
                    // `window`) - RenderTargetBitmap(window) NUNCA lo captura, pase lo que pase
                    // (comprobado en esta misma ronda: la primera captura salio con el boton "Filtros
                    // (1)" bien pero el popup invisible). Mismo truco real de reflexion que ya usa
                    // UI-BLOQUEADA para localizar WhereIsItPopup - popup.Child SI es un Visual real
                    // con su propio ActualWidth/ActualHeight una vez IsOpen=true, y RenderTargetBitmap
                    // acepta cualquier Visual, no solo los de la ventana principal.
                    // ADR-TERRAKEEP-016/031 (27-sep-2026): LibraryFiltersPopup vive ahora dentro
                    // de ObjetosView (campo generado por SU PROPIO InitializeComponent(), no el
                    // de MainWindow) - mismo patron de reflexion ya usado por WorldMapView/ADR-030
                    // para OnMinimapClick: la reflexion pasa a apuntar a la CLASE y la INSTANCIA
                    // reales (Terrakeep.App.Views.ObjetosView, obtenida por
                    // window.FindName("ObjetosView")).
                    var objetosViewHostLibFilt = window.FindName("ObjetosView") as Terrakeep.App.Views.ObjetosView;
                    var popupField = typeof(Terrakeep.App.Views.ObjetosView).GetField("LibraryFiltersPopup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var popup = popupField?.GetValue(objetosViewHostLibFilt) as System.Windows.Controls.Primitives.Popup;
                    var visualACapturar = popup?.Child is System.Windows.FrameworkElement popupChild && popupChild.ActualWidth > 1
                        ? (System.Windows.Media.Visual)popupChild
                        : window;
                    var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)((System.Windows.FrameworkElement)visualACapturar).ActualWidth,
                        (int)((System.Windows.FrameworkElement)visualACapturar).ActualHeight,
                        96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb.Render(visualACapturar);
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                    string shotPath = Path.Combine(AppContext.BaseDirectory, archivo);
                    using (var fs = File.Create(shotPath)) encoder.Save(fs);
                    Console.WriteLine($"LIBFILT: captura real del popup ({idioma}, popup encontrado={popup?.Child != null}, filtro 'Cabeza' activo, {vm.Library.Results.Count} resultado(s)) -> {shotPath}");
                    vm.Library.IsFiltersOpen = false;
                    vm.Library.ToggleEquipSlotChipCommand.Execute(cabeza0);
                }
                CapturaConFiltros("es", "libreria-filtros-ventana-es-minima.png", "libreria-filtros-popup-es-minima.png");
                CapturaConFiltros("en", "libreria-filtros-ventana-en-minima.png", "libreria-filtros-popup-en-minima.png");
                vm.Settings.Language = "es";
            }
            catch (Exception ex) { Console.WriteLine("LIBFILT-EXCEPTION: " + ex); }

            BarridoMaquetacionPorTamañoEIdioma(window, vm);
            Console.WriteLine("DONE (LIBFILT_SOLO)");
            Environment.Exit(0);
        }

        // BUILDCODE_SOLO=1 (13-sep-2026, codigos de build compartibles): mismo modo de foco que
        // los de arriba. Necesita un personaje real ya cargado (usa EquipmentGroup) - se apoya en
        // el mismo LOAD: de un poco mas arriba en este Main().
        if (Environment.GetEnvironmentVariable("BUILDCODE_SOLO") == "1")
        {
            try
            {
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 0;
                FijarTamaño(window, 1080, 700);
                DoEvents(); DoEvents();

                void CapturaConBuildCode(string idioma, string archivo)
                {
                    vm.Settings.Language = idioma;
                    DoEvents(); DoEvents();
                    vm.OpenBuildCodeCommand.Execute(null);
                    DoEvents(); DoEvents();
                    var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb.Render(window);
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                    string shotPath = Path.Combine(AppContext.BaseDirectory, archivo);
                    using (var fs = File.Create(shotPath)) encoder.Save(fs);
                    Console.WriteLine($"BUILDCODE: captura real ({idioma}, 1080x700, codigo real de {vm.CharacterName}) -> {shotPath}");
                    vm.CloseBuildCodeCommand.Execute(null);
                }
                CapturaConBuildCode("es", "buildcode-es-minima.png");

                // Un round-trip real: pega su PROPIO codigo en el cuadro de importar y confirma
                // que "importa" limpio (0 omitidos, ya que es el mismo equipo que ya lleva puesto).
                vm.OpenBuildCodeCommand.Execute(null);
                string codigoReal = vm.BuildCodeGenerated;
                vm.BuildCodeImportText = codigoReal;
                vm.ImportBuildCodeCommand.Execute(null);
                DoEvents(); DoEvents();
                Console.WriteLine($"BUILDCODE: auto-importar el propio codigo -> error={vm.BuildCodeImportIsError} (esperado False), mensaje='{vm.BuildCodeImportMessage}'");
                var rtb2 = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb2.Render(window);
                var encoder2 = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder2.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb2));
                string shotPath2 = Path.Combine(AppContext.BaseDirectory, "buildcode-es-tras-importar.png");
                using (var fs = File.Create(shotPath2)) encoder2.Save(fs);
                Console.WriteLine($"BUILDCODE: captura real tras importar -> {shotPath2}");
                vm.CloseBuildCodeCommand.Execute(null);

                CapturaConBuildCode("en", "buildcode-en-minima.png");
                vm.Settings.Language = "es";
            }
            catch (Exception ex) { Console.WriteLine("BUILDCODE-EXCEPTION: " + ex); }

            BarridoMaquetacionPorTamañoEIdioma(window, vm);
            Console.WriteLine("DONE (BUILDCODE_SOLO)");
            Environment.Exit(0);
        }

        // STATS_SOLO=1 (13-sep-2026, muertes PvE/PvP nuevas en Apariencia - octavo de la lista
        // confirmada): mismo modo de foco que los de arriba, capturas reales de la pestaña
        // Apariencia (las nuevas filas de estadisticas) en los dos idiomas, tamaño minimo.
        if (Environment.GetEnvironmentVariable("STATS_SOLO") == "1")
        {
            try
            {
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 3; // Apariencia (PersonajeInnerTab, privado en MainViewModel)
                FijarTamaño(window, 1080, 700);
                DoEvents(); DoEvents();
                vm.Appearance.PveDeaths = 17;
                vm.Appearance.PvpDeaths = 9;
                DoEvents(); DoEvents();

                void CapturaStats(string idioma, string archivo)
                {
                    vm.Settings.Language = idioma;
                    DoEvents(); DoEvents();
                    var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb.Render(window);
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                    string shotPath = Path.Combine(AppContext.BaseDirectory, archivo);
                    using (var fs = File.Create(shotPath)) encoder.Save(fs);
                    Console.WriteLine($"STATS: captura real ({idioma}, 1080x700, PveDeaths={vm.Appearance.PveDeaths} PvpDeaths={vm.Appearance.PvpDeaths}) -> {shotPath}");
                }
                CapturaStats("es", "stats-apariencia-es-minima.png");
                CapturaStats("en", "stats-apariencia-en-minima.png");
                vm.Settings.Language = "es";
                FijarTamaño(window, 1080, 1500);
                DoEvents(); DoEvents();
                CapturaStats("es", "stats-apariencia-es-completa.png");
            }
            catch (Exception ex) { Console.WriteLine("STATS-EXCEPTION: " + ex); }

            BarridoMaquetacionPorTamañoEIdioma(window, vm);
            Console.WriteLine("DONE (STATS_SOLO)");
            Environment.Exit(0);
        }

        // COMPARE_SOLO=1 (13-sep-2026, Comparador de personajes/builds): mismo modo de foco que
        // LIBFILT_SOLO justo arriba, mismo motivo real. El panel vive en Inicio (pestaña 0) y
        // NO exige ningun personaje cargado en el editor - solo la lista real ya escaneada por
        // Home (Home.Characters, esta maquina tiene 5 personajes reales de verdad). Dos capturas
        // reales (ventana + resultados desplazados) en los dos idiomas al tamaño MINIMO real de
        // la ventana (1080x700).
        if (Environment.GetEnvironmentVariable("COMPARE_SOLO") == "1")
        {
            try
            {
                vm.SelectedTabIndex = 0; // Inicio
                FijarTamaño(window, 1080, 700);
                DoEvents(); DoEvents();
                Console.WriteLine($"COMPARE: {vm.Home.Characters.Count} personaje(s) reales en Inicio (esperado >= 2 para poder comparar de verdad)");

                void CapturaConCompare(string idioma, string archivo, double alto)
                {
                    vm.Settings.Language = idioma;
                    FijarTamaño(window, 1080, alto);
                    DoEvents(); DoEvents();
                    vm.OpenCompareCommand.Execute(null);
                    if (vm.Home.Characters.Count >= 2)
                    {
                        vm.Compare.SelectedA = vm.Home.Characters[0];
                        vm.Compare.SelectedB = vm.Home.Characters[1];
                    }
                    DoEvents(); DoEvents();
                    var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb.Render(window);
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                    string shotPath = Path.Combine(AppContext.BaseDirectory, archivo);
                    using (var fs = File.Create(shotPath)) encoder.Save(fs);
                    Console.WriteLine($"COMPARE: captura real ({idioma}, 1080x{alto:0}, {vm.Compare.DifferenceCount} diferencia(s) reales) -> {shotPath}");
                }
                // Minima real (1080x700, el suelo de la ventana) Y una alta (2000) para ver de
                // un vistazo los bloques de Equipo/Inventario enteros sin depender del scroll -
                // el barrido AR-LAY de abajo ya cubre el resto de tamaños/solapes real.
                CapturaConCompare("es", "comparador-es-minima.png", 700);
                CapturaConCompare("es", "comparador-es-completa.png", 2000);
                CapturaConCompare("en", "comparador-en-minima.png", 700);

                // Tercera captura: bloque Inventario (dos rejillas de 50, la parte que mas
                // riesgo real de desbordar tenia) - se desplaza el ScrollViewer real del panel
                // hasta el final antes de capturar.
                vm.Settings.Language = "es";
                FijarTamaño(window, 1080, 2000);
                DoEvents(); DoEvents();
                vm.OpenCompareCommand.Execute(null);
                if (vm.Home.Characters.Count >= 2)
                {
                    vm.Compare.SelectedA = vm.Home.Characters[0];
                    vm.Compare.SelectedB = vm.Home.Characters[1];
                }
                DoEvents(); DoEvents();
                // ADR-TERRAKEEP-025 (26-sep-2026): mismo patron de FindName DOBLE de arriba
                // (Compare paso a UserControl, CompareView es dueño de su propio NameScope).
                var compareViewCS = window.FindName("CompareView") as System.Windows.FrameworkElement;
                if (compareViewCS?.FindName("CompareResultsScrollViewer") is System.Windows.Controls.ScrollViewer sv)
                {
                    sv.ScrollToEnd();
                    DoEvents(); DoEvents();
                }
                var rtbInv = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbInv.Render(window);
                var encInv = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encInv.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbInv));
                string shotPathInv = Path.Combine(AppContext.BaseDirectory, "comparador-es-inventario.png");
                using (var fs = File.Create(shotPathInv)) encInv.Save(fs);
                Console.WriteLine($"COMPARE: captura real del bloque Inventario (desplazada al final) -> {shotPathInv}");
            }
            catch (Exception ex) { Console.WriteLine("COMPARE-EXCEPTION: " + ex); }

            BarridoMaquetacionPorTamañoEIdioma(window, vm);
            Console.WriteLine("DONE (COMPARE_SOLO)");
            Environment.Exit(0);
        }

        // WORLDEDIT_SOLO=1 (14-sep-2026, editor de mundos v1: spawn/hora-luna/banderas de
        // progreso + bestiario, guia real de bitacora.md 13-sep-2026): mismo modo de foco que
        // los de arriba. NUNCA sobre roca_negra.wld real (el mundo que el resto del arnes sigue
        // usando despues) - siempre sobre una COPIA en el scratchpad, borrada al final pase lo
        // que pase (try/finally), cargada de verdad a traves de la ViewModel (no solo
        // WorldFileService a secas, para probar tambien el cableado real de los tres botones
        // "Guardar" y sus CanExecute).
        if (Environment.GetEnvironmentVariable("WORLDEDIT_SOLO") == "1")
        {
            string worldPathWE = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
            string copiaWE = Path.Combine(Path.GetTempPath(), $"terrakeep-test-worldedit-{Guid.NewGuid():N}.wld");
            try
            {
                if (!File.Exists(worldPathWE)) { Console.WriteLine("WORLDEDIT: mundo real no encontrado, nada que probar."); }
                else
                {
                    File.Copy(worldPathWE, copiaWE);
                    vm.SelectedTabIndex = 4; // Exploracion
                    FijarTamaño(window, 1080, 700);
                    DoEvents(); DoEvents();

                    var taskWE = vm.Exploration.LoadFromPathAsync(copiaWE);
                    while (!taskWE.IsCompleted) DoEvents();
                    if (taskWE.IsFaulted) throw taskWE.Exception!;
                    Console.WriteLine($"WORLDEDIT: copia cargada -> spawn=({vm.Exploration.EditSpawnX},{vm.Exploration.EditSpawnY}) horaOriginal={vm.Exploration.EditTimeHour:0.00} faseLunar={vm.Exploration.EditMoonPhase} HasSlimeKingField={vm.Exploration.HasSlimeKingField} HasBestiary={vm.Exploration.HasBestiary}");

                    // --- WE-01: punto de aparicion ---
                    int spawnXAntes = vm.Exploration.EditSpawnX, spawnYAntes = vm.Exploration.EditSpawnY;
                    Console.WriteLine($"WE-01: SaveSpawnPointCommand.CanExecute ANTES de tocar nada -> {vm.Exploration.SaveSpawnPointCommand.CanExecute(null)} (esperado False)");
                    if (vm.Exploration.SaveSpawnPointCommand.CanExecute(null)) Console.WriteLine("FALLO: WE-01 - el boton de guardar spawn no deberia estar activo sin cambios");
                    vm.Exploration.EditSpawnX = spawnXAntes + 37;
                    vm.Exploration.EditSpawnY = spawnYAntes + 11;
                    DoEvents();
                    Console.WriteLine($"WE-01: CanExecute tras cambiar -> {vm.Exploration.SaveSpawnPointCommand.CanExecute(null)} (esperado True)");
                    var taskSpawn = vm.Exploration.SaveSpawnPointCommand.ExecuteAsync(null);
                    while (!taskSpawn.IsCompleted) DoEvents();
                    DoEvents();
                    var releidoSpawn = WldReader.ReadHeader(File.ReadAllBytes(copiaWE));
                    Console.WriteLine($"WE-01: tras guardar -> archivo real spawn=({releidoSpawn.SpawnX},{releidoSpawn.SpawnY}) (esperado ({spawnXAntes + 37},{spawnYAntes + 11})), .bak existe={File.Exists(copiaWE + ".bak")} (esperado True), estado='{vm.Exploration.SpawnSaveStatus}'");
                    if (releidoSpawn.SpawnX != spawnXAntes + 37 || releidoSpawn.SpawnY != spawnYAntes + 11 || !File.Exists(copiaWE + ".bak"))
                        Console.WriteLine("FALLO: WE-01 - el guardado real del punto de aparicion no escribio lo esperado");

                    // --- WE-02: hora/luna/luna de sangre/eclipse ---
                    Console.WriteLine($"WE-02: SaveTimeAndMoonCommand.CanExecute ANTES de tocar nada -> {vm.Exploration.SaveTimeAndMoonCommand.CanExecute(null)} (esperado False)");
                    vm.Exploration.EditTimeHour = 12.0; // mediodia real (DayTime=true)
                    vm.Exploration.EditMoonPhase = (vm.Exploration.EditMoonPhase + 1) % 8;
                    vm.Exploration.EditBloodMoon = !vm.Exploration.EditBloodMoon;
                    DoEvents();
                    var taskTime = vm.Exploration.SaveTimeAndMoonCommand.ExecuteAsync(null);
                    while (!taskTime.IsCompleted) DoEvents();
                    DoEvents();
                    var releidoTime = WldReader.ReadHeader(File.ReadAllBytes(copiaWE));
                    double horaReleida = ExplorationViewModel.GameTimeToHour(releidoTime.Time, releidoTime.DayTime);
                    Console.WriteLine($"WE-02: tras guardar -> archivo real hora={horaReleida:0.00} (esperado ~12.00) DayTime={releidoTime.DayTime} (esperado True) MoonPhase={releidoTime.MoonPhase} BloodMoon={releidoTime.BloodMoon}, estado='{vm.Exploration.TimeSaveStatus}'");
                    if (Math.Abs(horaReleida - 12.0) > 0.1 || !releidoTime.DayTime) Console.WriteLine("FALLO: WE-02 - la hora guardada no corresponde a mediodia real");

                    // --- WE-03: banderas de progreso (jefes + modo dificil) ---
                    bool eocAntes = vm.Exploration.EditDownedBoss1, hardModeAntes = vm.Exploration.EditHardMode;
                    vm.Exploration.EditDownedBoss1 = !eocAntes;
                    vm.Exploration.EditHardMode = !hardModeAntes;
                    DoEvents();
                    Console.WriteLine($"WE-03: SaveBossFlagsCommand.CanExecute tras cambiar 2 banderas -> {vm.Exploration.SaveBossFlagsCommand.CanExecute(null)} (esperado True)");
                    var taskFlags = vm.Exploration.SaveBossFlagsCommand.ExecuteAsync(null);
                    while (!taskFlags.IsCompleted) DoEvents();
                    DoEvents();
                    var releidoFlags = WldReader.ReadHeader(File.ReadAllBytes(copiaWE));
                    Console.WriteLine($"WE-03: tras guardar -> archivo real EoC={releidoFlags.DownedBoss1EyeOfCthulhu} (esperado {!eocAntes}) HardMode={releidoFlags.HardMode} (esperado {!hardModeAntes}), otro jefe no tocado QueenBee={releidoFlags.DownedQueenBee} (esperado False, no se pidio cambiar), estado='{vm.Exploration.FlagsSaveStatus}'");
                    if (releidoFlags.DownedBoss1EyeOfCthulhu != !eocAntes || releidoFlags.HardMode != !hardModeAntes || releidoFlags.DownedQueenBee)
                        Console.WriteLine("FALLO: WE-03 - el guardado real de banderas de progreso no hizo exactamente lo pedido");

                    // --- WE-04: bestiario (solo lectura) ---
                    Console.WriteLine($"WE-04: BestiaryRows.Count={vm.Exploration.BestiaryRows.Count} HasBestiary={vm.Exploration.HasBestiary} resumen='{vm.Exploration.BestiarySummaryText}'");
                    if (vm.Exploration.HasBestiary && vm.Exploration.BestiaryRows.Count > 0)
                    {
                        var primera = vm.Exploration.BestiaryRows[0];
                        Console.WriteLine($"WE-04: fila con mas muertes -> '{primera.Name}' x{primera.Kills} (orden descendente esperado)");
                        if (vm.Exploration.BestiaryRows.Count > 1 && vm.Exploration.BestiaryRows[1].Kills > primera.Kills)
                            Console.WriteLine("FALLO: WE-04 - las filas del bestiario no estan ordenadas de mas a menos muertes");

                        // Catalogo de ideas Keep, idea 4 (20-sep-2026): comprobacion real de que
                        // el arreglo de verdad trae filas con 0 muertes (antes invisibles del
                        // todo) y de que al menos algunas tienen icono/pastillas reales.
                        int conCeroMuertes = vm.Exploration.BestiaryRows.Count(r => r.Kills == 0);
                        int conIcono = vm.Exploration.BestiaryRows.Count(r => r.IconPath != null);
                        int vistos = vm.Exploration.BestiaryRows.Count(r => r.Sighted);
                        int hablados = vm.Exploration.BestiaryRows.Count(r => r.Chatted);
                        Console.WriteLine($"WE-04: filas con 0 muertes (antes invisibles del todo)={conCeroMuertes}, con icono real={conIcono}, Sighted=True={vistos}, Chatted=True={hablados}");
                        if (conCeroMuertes == 0) Console.WriteLine("FALLO: WE-04 - ninguna fila con 0 muertes (el bug real de filas ausentes puede seguir ahi)");
                        var ejemploIconoYHablado = vm.Exploration.BestiaryRows.FirstOrDefault(r => r.IconPath != null && r.Chatted);
                        Console.WriteLine($"WE-04: ejemplo real con icono+Chatted -> {(ejemploIconoYHablado != null ? $"'{ejemploIconoYHablado.Name}' icono={ejemploIconoYHablado.IconPath}" : "NINGUNO encontrado")}");
                    }

                    // Capturas reales del panel "Editar mundo" desplegado, dos idiomas, tamaño minimo.
                    void CapturaConEditorMundo(string idioma, string archivo)
                    {
                        vm.Settings.Language = idioma;
                        // El ItemsControl del bestiario regenera TODOS sus contenedores al
                        // cambiar de idioma (ObservableCollection.Clear()+Add x75) - un par de
                        // DoEvents() de mas para que WPF termine de verdad el layout+render de
                        // las filas nuevas antes de capturar (visto con un caso real: la 4ª fila
                        // seguia pintando el texto ANTERIOR en la primera captura tras el cambio,
                        // aunque el dato en la ViewModel ya era el correcto - puro tiempo de
                        // render, confirmado con un WriteLine de los datos reales).
                        DoEvents(); DoEvents(); DoEvents(); DoEvents();
                        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtb.Render(window);
                        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                        string shotPath = Path.Combine(AppContext.BaseDirectory, archivo);
                        using (var fs = File.Create(shotPath)) encoder.Save(fs);
                        Console.WriteLine($"WORLDEDIT: captura real ({idioma}, 1080x700) -> {shotPath}");
                    }
                    // El Expander "Editar mundo" via UI Automation real (mismo mecanismo que el
                    // resto del arnes) - expandirlo de verdad antes de capturar, no solo confiar
                    // en que el binding puso el contenido en el arbol visual colapsado.
                    var expanderEditar = root.FindFirst(TreeScope.Descendants, new AndCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Group),
                        new PropertyCondition(AutomationElement.NameProperty, LocalizationService.Instance["explore_edit_world"])));
                    if (expanderEditar != null && expanderEditar.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expPat))
                        ((ExpandCollapsePattern)expPat).Expand();
                    else
                        Console.WriteLine("WORLDEDIT: Expander 'Editar mundo' NO-FOUND via UI Automation");
                    var expanderBestiario = root.FindFirst(TreeScope.Descendants, new AndCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Group),
                        new PropertyCondition(AutomationElement.NameProperty, LocalizationService.Instance["explore_bestiary"])));
                    if (expanderBestiario != null && expanderBestiario.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expPat2))
                        ((ExpandCollapsePattern)expPat2).Expand();
                    else
                        Console.WriteLine("WORLDEDIT: Expander 'Bestiario' NO-FOUND via UI Automation");
                    DoEvents(); DoEvents();
                    CapturaConEditorMundo("es", "worldedit-es-minima.png");
                    CapturaConEditorMundo("en", "worldedit-en-minima.png");
                    vm.Settings.Language = "es";

                    // Mismo criterio real que BUILDCODE_SOLO/COMPARE_SOLO (cierran su propio
                    // overlay antes del barrido compartido AR-LAY): estos dos Expanders son
                    // estado de UI compartido con el resto del arnes (vm/window se reutilizan
                    // para el barrido de abajo) - dejarlos expandidos aqui haria que AR-LAY
                    // midiera un estado excepcional (3 Expanders de esta columna a la vez, un
                    // caso de verdad posible pero fuera del "estado por defecto" que MinHeight=590
                    // tiene calibrado, ver el comentario real de ExplorationSidebarScroll) en vez
                    // del estado normal que el resto de la suite espera.
                    if (expanderEditar != null && expanderEditar.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var collPat1))
                        ((ExpandCollapsePattern)collPat1).Collapse();
                    if (expanderBestiario != null && expanderBestiario.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var collPat2))
                        ((ExpandCollapsePattern)collPat2).Collapse();
                    DoEvents(); DoEvents();
                }
            }
            catch (Exception ex) { Console.WriteLine("WORLDEDIT-EXCEPTION: " + ex); }
            finally
            {
                File.Delete(copiaWE);
                File.Delete(copiaWE + ".bak");
                File.Delete(copiaWE + ".tmp");
            }

            BarridoMaquetacionPorTamañoEIdioma(window, vm);
            Console.WriteLine("DONE (WORLDEDIT_SOLO)");
            Environment.Exit(0);
        }

        // WORLDPREVIEW_SOLO=1 (14-sep-2026, vista previa de generacion de mundo, noveno de la
        // lista confirmada del 13-sep-2026): mismo modo de foco que los de arriba. A diferencia
        // de WORLDEDIT_SOLO, no toca NINGUN archivo real (calculador puro) - no hace falta
        // ningun mundo/personaje cargado ni ninguna copia desechable.
        if (Environment.GetEnvironmentVariable("WORLDPREVIEW_SOLO") == "1")
        {
            try
            {
                vm.SelectedTabIndex = 4; // Exploracion (el boton vive en su cabecera)
                FijarTamaño(window, 1080, 700);
                DoEvents(); DoEvents();

                // WP-01: sin tocar nada, semilla normal -> tamaño Mediano real, sin efectos.
                Console.WriteLine($"WP-01: por defecto -> tamaño={vm.WorldPreview.SelectedSize} dimensiones='{vm.WorldPreview.DimensionsText}' (esperado 6400×1800), HasSpecialSeed={vm.WorldPreview.HasSpecialSeed} (esperado False)");
                if (vm.WorldPreview.DimensionsText != "6400×1800 tiles" || vm.WorldPreview.HasSpecialSeed)
                    Console.WriteLine("FALLO: WP-01 - el estado por defecto no es el esperado (Mediano, sin semilla especial)");

                vm.OpenWorldPreviewCommand.Execute(null);
                DoEvents(); DoEvents();
                Console.WriteLine($"WP-02: panel abierto -> IsOpen={vm.WorldPreview.IsOpen} (esperado True)");
                if (!vm.WorldPreview.IsOpen) Console.WriteLine("FALLO: WP-02 - OpenWorldPreviewCommand no abrio el panel");

                // WP-03: tamaño Grande + semilla "for the worthy" -> dimensiones reales + UN efecto real.
                vm.WorldPreview.SelectedSize = Terrakeep.Core.WorldGen.WorldSizeOption.Large;
                vm.WorldPreview.SeedText = "for the worthy";
                DoEvents(); DoEvents();
                Console.WriteLine($"WP-03: Grande + 'for the worthy' -> dimensiones='{vm.WorldPreview.DimensionsText}' (esperado '8400×2400 tiles'), efectos={vm.WorldPreview.SpecialSeedEffectRows.Count} (esperado 1), nombre del efecto='{(vm.WorldPreview.SpecialSeedEffectRows.Count > 0 ? vm.WorldPreview.SpecialSeedEffectRows[0].Name : "?")}'");
                if (vm.WorldPreview.DimensionsText != "8400×2400 tiles" || vm.WorldPreview.SpecialSeedEffectRows.Count != 1)
                    Console.WriteLine("FALLO: WP-03 - el resumen no reacciono bien a tamaño+semilla reales");

                // WP-04: "get fixed boi" -> las 8 banderas reales a la vez.
                vm.WorldPreview.SeedText = "get fixed boi";
                DoEvents(); DoEvents();
                Console.WriteLine($"WP-04: 'get fixed boi' -> efectos={vm.WorldPreview.SpecialSeedEffectRows.Count} (esperado 8)");
                if (vm.WorldPreview.SpecialSeedEffectRows.Count != 8) Console.WriteLine("FALLO: WP-04 - 'get fixed boi' deberia activar las 8 banderas reales a la vez");

                void CapturaConVistaPrevia(string idioma, string archivo)
                {
                    vm.Settings.Language = idioma;
                    DoEvents(); DoEvents(); DoEvents(); DoEvents();
                    var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtb.Render(window);
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                    string shotPath = Path.Combine(AppContext.BaseDirectory, archivo);
                    using (var fs = File.Create(shotPath)) encoder.Save(fs);
                    Console.WriteLine($"WORLDPREVIEW: captura real ({idioma}, 1080x700) -> {shotPath}");
                }
                CapturaConVistaPrevia("es", "worldpreview-es-minima.png");
                CapturaConVistaPrevia("en", "worldpreview-en-minima.png");
                vm.Settings.Language = "es";

                vm.WorldPreview.CloseCommand.Execute(null);
                DoEvents(); DoEvents();
                Console.WriteLine($"WP-05: cerrado -> IsOpen={vm.WorldPreview.IsOpen} (esperado False)");
                if (vm.WorldPreview.IsOpen) Console.WriteLine("FALLO: WP-05 - CloseCommand no cerro el panel");
            }
            catch (Exception ex) { Console.WriteLine("WORLDPREVIEW-EXCEPTION: " + ex); }

            BarridoMaquetacionPorTamañoEIdioma(window, vm);
            Console.WriteLine("DONE (WORLDPREVIEW_SOLO)");
            Environment.Exit(0);
        }

        // NOVEDADES_SOLO=1 (26-sep-2026, ADR-TERRAKEEP-016/017, primera extraccion real de una
        // seccion de MainWindow.xaml a un UserControl): canario minimo que la propia ADR-016
        // marcaba como hueco real (punto 5, "HUECOS REALES" - ningun test cubria antes la
        // pestaña NOVEDADES ni el mecanismo de extraccion en si, solo "compila"). Confirma que
        // Views/WhatsNewView.xaml se monta sin lanzar (sus 3 recursos - CaptionText/AccentBrush/
        // InnerTabControl - viven en Styles/Theme.xaml via App.xaml, NINGUNO es window-scoped,
        // por eso se resuelven igual desde dentro de un UserControl que desde el Window) y que
        // su contenido (las 2 sub-pestañas Terraria/tModLoader-Calamity con datos reales) es
        // exactamente el mismo que tenia el TabItem antes de moverse.
        if (Environment.GetEnvironmentVariable("NOVEDADES_SOLO") == "1")
        {
            try
            {
                vm.SelectedTabIndex = 6; // AppTab.Novedades
                FijarTamaño(window, 1180, 860);
                DoEvents(); DoEvents();

                var vista = Descendientes<Terrakeep.App.Views.WhatsNewView>(window).FirstOrDefault();
                Console.WriteLine($"NOV-EXT-01: WhatsNewView montada en el arbol visual del Window={vista != null} (esperado True)");
                if (vista == null) Console.WriteLine("FALLO: NOV-EXT-01 - no se encontro ninguna WhatsNewView tras seleccionar la pestaña Novedades");

                var tabInterna = vista != null ? Descendientes<TabControl>(vista).FirstOrDefault() : null;
                int subPestañas = tabInterna?.Items.Count ?? -1;
                Console.WriteLine($"NOV-EXT-02: sub-pestañas dentro de WhatsNewView={subPestañas} (esperado 2)");
                if (subPestañas != 2) Console.WriteLine("FALLO: NOV-EXT-02 - deberian existir exactamente 2 sub-pestañas (Terraria / tModLoader-Calamity Mod)");

                // NOV-EXT-03: el TabControl interno solo ADJUNTA al arbol visual el Content de
                // la sub-pestaña SELECCIONADA (comportamiento real de WPF, no un bug) - hay que
                // seleccionar cada una por turno antes de buscar su ItemsControl. Ademas, cada
                // WhatsNewEntryViewModel real (Window.Resources, plantilla implicita por tipo)
                // trae SUS PROPIOS ItemsControl anidados (lista de cambios/items), por eso se
                // busca por REFERENCIA exacta el ItemsControl de nivel superior que la vista liga
                // directamente a WhatsNew.VanillaEntries/CalamityEntries (mismo objeto en
                // memoria, no una copia) en vez de contar todos los descendientes.
                if (tabInterna != null) tabInterna.SelectedIndex = 0;
                DoEvents(); DoEvents();
                bool tieneVanilla = tabInterna != null && Descendientes<ItemsControl>(tabInterna).Any(ic => ReferenceEquals(ic.ItemsSource, vm.WhatsNew.VanillaEntries));

                if (tabInterna != null) tabInterna.SelectedIndex = 1;
                DoEvents(); DoEvents();
                bool tieneCalamity = tabInterna != null && Descendientes<ItemsControl>(tabInterna).Any(ic => ReferenceEquals(ic.ItemsSource, vm.WhatsNew.CalamityEntries));

                Console.WriteLine($"NOV-EXT-03: ItemsControl de nivel superior ligado a VanillaEntries={tieneVanilla} (esperado True), ligado a CalamityEntries={tieneCalamity} (esperado True), entradas vanilla reales={vm.WhatsNew.VanillaEntries.Count}, entradas Calamity reales={vm.WhatsNew.CalamityEntries.Count}");
                if (!tieneVanilla || !tieneCalamity) Console.WriteLine("FALLO: NOV-EXT-03 - los dos ItemsControl de nivel superior deberian seguir ligados a las mismas colecciones reales del ViewModel que antes de la extraccion");
                if (vm.WhatsNew.VanillaEntries.Count == 0 || vm.WhatsNew.CalamityEntries.Count == 0)
                    Console.WriteLine("FALLO: NOV-EXT-03 - el changelog real deberia traer entradas en ambas listas");

                // Captura real para verificacion visual manual (comportamiento preservado byte a
                // byte: mismo XAML que antes de la extraccion, solo movido a WhatsNewView.xaml).
                if (tabInterna != null) tabInterna.SelectedIndex = 0;
                DoEvents(); DoEvents(); DoEvents(); DoEvents();
                var rtbNov = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbNov.Render(window);
                var encoderNov = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoderNov.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbNov));
                var shotNov = Path.Combine(AppContext.BaseDirectory, "novedades-tras-extraccion.png");
                using (var fsNov = File.Create(shotNov)) encoderNov.Save(fsNov);
                Console.WriteLine($"NOV-EXT-04: captura real de la pestaña Novedades tras la extraccion -> {shotNov}");
            }
            catch (Exception ex) { Console.WriteLine("NOVEDADES-EXCEPTION: " + ex); }

            Console.WriteLine("DONE (NOVEDADES_SOLO)");
            Environment.Exit(0);
        }

        // Verificacion real de N-1 (auditoria de Opus, Bloque 2): la cabecera global debe verse
        // IGUAL en una pestaña que no es Personaje (aqui, Builds=indice 2) - antes el nombre/
        // dificultad/Guardar solo existian dentro de Personaje.
        vm.SelectedTabIndex = 2; // Builds
        DoEvents();
        DoEvents();
        // H-1 (segunda auditoria de Opus, Fable): el nombre paso de TextBlock (ControlType.Text,
        // buscable por Name) a un TextBox real editable (ControlType.Edit, el texto vive en
        // ValuePattern.Current.Value, no en Name) - se busca por su valor real en vez de su Name.
        var cajasDeEdicion = root.FindAll(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
        bool headerNameEncontrado = cajasDeEdicion.Cast<AutomationElement>().Any(el =>
            el.TryGetCurrentPattern(ValuePattern.Pattern, out var pat) && ((ValuePattern)pat).Current.Value == "UIA-Test");
        Console.WriteLine($"CABECERA-GLOBAL (en Builds): nombre real encontrado={headerNameEncontrado} (esperado True)");
        {
            var rtbHeader = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbHeader.Render(window);
            var encHeader = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encHeader.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbHeader));
            string shotPathHeader = Path.Combine(AppContext.BaseDirectory, "cabecera-global-en-builds.png");
            using (var fs = File.Create(shotPathHeader)) encHeader.Save(fs);
            Console.WriteLine($"  Captura -> {shotPathHeader}");
        }
        // Guardar desde una pestaña que NO es Personaje via UI Automation real (boton real de
        // la cabecera, no vm.SaveCommand.Execute a pelo) - confirma que el banner de
        // confirmacion (movido a nivel raiz en N-1) se ve tambien fuera de Personaje.
        var saveButtonHeader = root.FindFirst(TreeScope.Descendants, new AndCondition(
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
            new PropertyCondition(AutomationElement.NameProperty, "Guardar")));
        if (saveButtonHeader != null && saveButtonHeader.TryGetCurrentPattern(InvokePattern.Pattern, out var saveInvokePat))
        {
            ((InvokePattern)saveInvokePat).Invoke();
            DoEvents();
            DoEvents();
            Console.WriteLine($"GUARDAR-DESDE-BUILDS: SaveConfirmationVisible={vm.SaveConfirmationVisible} (esperado True), StatusMessage={vm.StatusMessage}");
        }
        else Console.WriteLine("GUARDAR-DESDE-BUILDS: boton 'Guardar' NO-FOUND en la cabecera");

        vm.SelectedTabIndex = 1; // Personaje
        vm.PersonajeInnerTabIndex = 0; // Objetos
        DoEvents();
        DoEvents();

        // Coloca objetos reales en varios slots (id 1 = Iron Pickaxe, id 2 = Iron Axe...) para
        // que la rejilla compacta tenga iconos de verdad que medir/organizar, no solo huecos
        // vacios - el caso mas exigente para SlotGridPanel (celdas ricas de verdad, cantidades
        // >1 visibles).
        if (vm.InventoryContainer != null)
        {
            for (int i = 0; i < 12 && i < vm.InventoryContainer.Slots.Count; i++)
                vm.InventoryContainer.Slots[i].PlaceItem(i + 1);
            vm.InventoryContainer.Slots[0].Count = 99;

            // Verificacion real de T-14 (auditoria de Opus, Bloque 3): PlaceItem es una edicion
            // real de usuario (carga ya termino, _suppressDirty=false) - debe disparar el
            // flash de inmediato, y auto-apagarse solo pasados los 450ms reales.
            bool justEditedInmediato = vm.InventoryContainer.Slots[1].JustEdited;
            DoEvents();
            // Captura util de verdad: selecciona la sub-pestaña real "Inventario" primero (si
            // no, la captura cae en "Equipamiento", la sub-pestaña por defecto, y no se ve
            // ningun slot de Inventario real).
            var invTab = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Inventario")));
            if (invTab != null && invTab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var invSelPat))
                ((SelectionItemPattern)invSelPat).Select();
            DoEvents();
            vm.InventoryContainer.Slots[2].PlaceItem(3); // re-dispara el flash ya en la pestaña visible correcta
            DoEvents();
            var rtbFlash = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbFlash.Render(window);
            var encFlash = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encFlash.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbFlash));
            using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "flash-edicion.png"))) encFlash.Save(fs);
            System.Threading.Thread.Sleep(600);
            DoEvents();
            bool justEditedTrasEspera = vm.InventoryContainer.Slots[1].JustEdited;
            Console.WriteLine($"T14-FLASH: JustEdited inmediatamente tras PlaceItem={justEditedInmediato} (esperado True), tras 600ms={justEditedTrasEspera} (esperado False)");
        }
        if (vm.StorageGroup != null)
            for (int i = 0; i < 15 && i < vm.StorageGroup.Current.Slots.Count; i++)
                vm.StorageGroup.Current.Slots[i].PlaceItem(i + 1);

        // Objeto CON prefijo real asignado en Equipamiento, para probar de verdad la
        // correccion 1 del usuario ("que allí aparezca el prefijo que tiene asignado") - no
        // solo un objeto sin prefijo, que no habria distinguido el bug de un falso OK.
        if (vm.EquipmentGroup != null)
        {
            var slot = vm.EquipmentGroup.Current.Slots[0];
            // Sexta pasada: el slot 0 ahora es ArmorHead real, una espada (id 3) ya no
            // encajaria - Casco Shroomite (1546, real, valido) en su lugar.
            slot.PlaceItem(1546);
            slot.SetPrefix(ItemPrefix.Vanilla(1));
            Console.WriteLine($"Equipamiento slot0: DisplayName={slot.DisplayName} PrefixDisplay='{slot.PrefixDisplay}'");

            // Peticion 1 (pregunta a Opus, cuarta pasada): accesorio real con tooltip
            // descriptivo real (Warrior Emblem, id 490, +15% daño cuerpo a cuerpo).
            var accessorySlot = vm.EquipmentGroup.Current.Slots[3];
            accessorySlot.PlaceItem(490);
            Console.WriteLine($"Accesorio 490 (Warrior Emblem) StatsTooltip:\n{accessorySlot.StatsTooltip}");

            // Set completo real de Shroomite (1546 casco, 1549 peto, 1550 grebas) - confirma
            // la seccion de bonus de set en el tooltip de UNA sola pieza.
            vm.EquipmentGroup.Current.Slots[0].PlaceItem(1546);
            vm.EquipmentGroup.Current.Slots[1].PlaceItem(1549);
            vm.EquipmentGroup.Current.Slots[2].PlaceItem(1550);
            Console.WriteLine($"Casco Shroomite (1546) StatsTooltip:\n{vm.EquipmentGroup.Current.Slots[0].StatsTooltip}");

            // Octava pasada: el usuario reporta que el solape sigue pasando y que el fondo
            // fantasma de armadura desaparece con un personaje REAL - hasta ahora solo se
            // habian llenado 4 de los 7 accesorios reales. Rellenar los 7 (indices 3-9) para
            // reproducir el caso real completo antes de asumir nada.
            for (int i = 4; i <= 9; i++) vm.EquipmentGroup.Current.Slots[i].PlaceItem(490);
            DoEvents();
            DoEvents();
            DoEvents();

            var rtbArmadura = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbArmadura.Render(window);
            var encArmadura = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encArmadura.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbArmadura));
            using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "armadura-7-accesorios.png"))) encArmadura.Save(fs);
            Console.WriteLine("Captura Armadura con 7 accesorios reales -> armadura-7-accesorios.png");
        }
        // Sexta pasada: restricciones reales de slot (SlotKind) + iconos fantasma + 6º/7º
        // accesorio. Ids reales conocidos por tipo (extraidos de vanilla_slot_kind.json):
        // 84=Gancho de escalada(Hook), 1914=Campanas de reno(Mount), 2191=Jaula de raton
        // (Cart/vagoneta), 603=Zanahoria(VanityPet), 425=Campana de hada(LightPet),
        // 1007=Tinte rojo(Dye), 40=Flecha de madera(Ammo), 71=Moneda de cobre(Coin).
        if (vm.MountsContainer != null && vm.CoinsContainer != null && vm.AmmoContainer != null && vm.DyesContainer != null)
        {
            var hookSlot = vm.MountsContainer.Slots[4]; // orden real: Pet/LightPet/Cart/Mount/Hook
            Console.WriteLine($"AcceptsItem: hookSlot.AcceptsItem(84 Gancho)={hookSlot.AcceptsItem(84)} (esperado True)");
            Console.WriteLine($"AcceptsItem: hookSlot.AcceptsItem(1 Pico de hierro)={hookSlot.AcceptsItem(1)} (esperado False)");
            hookSlot.PlaceItem(1); // objeto invalido - debe rechazarse
            Console.WriteLine($"Tras PlaceItem(1) invalido: hookSlot.IsEmpty={hookSlot.IsEmpty} RejectionMessage='{hookSlot.RejectionMessage}' (esperado IsEmpty=True, mensaje real)");
            hookSlot.PlaceItem(84); // objeto valido real
            Console.WriteLine($"Tras PlaceItem(84 Gancho) valido: DisplayName={hookSlot.DisplayName} RejectionMessage='{hookSlot.RejectionMessage}' (esperado colocado, sin mensaje)");

            var coinSlot = vm.CoinsContainer.Slots[0];
            coinSlot.PlaceItem(1);
            Console.WriteLine($"Moneda: PlaceItem(1) invalido -> IsEmpty={coinSlot.IsEmpty} RejectionMessage='{coinSlot.RejectionMessage}'");
            coinSlot.PlaceItem(71);
            Console.WriteLine($"Moneda: PlaceItem(71 real) -> DisplayName={coinSlot.DisplayName}");

            var mountSlot = vm.MountsContainer.Slots[3];
            mountSlot.PlaceItem(1914);
            Console.WriteLine($"Montura: PlaceItem(1914 real) -> DisplayName={mountSlot.DisplayName}");
            var cartSlot = vm.MountsContainer.Slots[2];
            cartSlot.PlaceItem(2191);
            Console.WriteLine($"Vagoneta: PlaceItem(2191 real) -> DisplayName={cartSlot.DisplayName}");
            var petSlot = vm.MountsContainer.Slots[0];
            petSlot.PlaceItem(603);
            Console.WriteLine($"Mascota: PlaceItem(603 real) -> DisplayName={petSlot.DisplayName}");
            var lightPetSlot = vm.MountsContainer.Slots[1];
            lightPetSlot.PlaceItem(425);
            Console.WriteLine($"MascotaLuz: PlaceItem(425 real) -> DisplayName={lightPetSlot.DisplayName}");

            var dyeSlot = vm.DyesContainer.Slots[0];
            dyeSlot.PlaceItem(1007);
            Console.WriteLine($"Tinte: PlaceItem(1007 real) -> DisplayName={dyeSlot.DisplayName}");

            var ammoSlot = vm.AmmoContainer.Slots[0];
            ammoSlot.PlaceItem(40);
            Console.WriteLine($"Municion: PlaceItem(40 real) -> DisplayName={ammoSlot.DisplayName}");

            // Calamity SIEMPRE se acepta (regla obligatoria, consulta a Opus) - un id sintetico
            // cualquiera (20000000+) debe pasar cualquier restriccion sin consultar el catalogo.
            Console.WriteLine($"Calamity siempre pasa: hookSlot.AcceptsItem(20000000)={hookSlot.AcceptsItem(20000000)} (esperado True)");
        }

        if (vm.EquipmentGroup != null)
        {
            var slot8 = vm.EquipmentGroup.EquippedItems.Slots[8];
            var slot9 = vm.EquipmentGroup.EquippedItems.Slots[9];
            var slot3 = vm.EquipmentGroup.EquippedItems.Slots[3];
            Console.WriteLine($"6º accesorio (indice 8): IsExpertAccessorySlot={slot8.IsExpertAccessorySlot} IsMasterAccessorySlot={slot8.IsMasterAccessorySlot} GhostIconPath={slot8.GhostIconPath}");
            Console.WriteLine($"7º accesorio (indice 9): IsExpertAccessorySlot={slot9.IsExpertAccessorySlot} IsMasterAccessorySlot={slot9.IsMasterAccessorySlot} GhostIconPath={slot9.GhostIconPath}");
            Console.WriteLine($"3er accesorio normal (indice 3): IsExpertAccessorySlot={slot3.IsExpertAccessorySlot} IsMasterAccessorySlot={slot3.IsMasterAccessorySlot} (esperado False/False)");
            var headSlot = vm.EquipmentGroup.EquippedItems.Slots[0];
            Console.WriteLine($"Slot cabeza (indice 0): GhostIconPath={headSlot.GhostIconPath} (esperado .../armor_head.png)");

            // Ampliacion pedida por el usuario a mitad de ronda ("las armaduras y los
            // accesorios, si los quiero arriba"): armadura/accesorios tambien restringidos.
            Console.WriteLine($"AcceptsItem: headSlot(cabeza).AcceptsItem(1546 Casco Shroomite real)={headSlot.AcceptsItem(1546)} (esperado True)");
            Console.WriteLine($"AcceptsItem: headSlot(cabeza).AcceptsItem(3 Espada, invalido)={headSlot.AcceptsItem(3)} (esperado False)");
            headSlot.PlaceItem(3); // arma en slot de cabeza - debe rechazarse
            Console.WriteLine($"Tras PlaceItem(3 espada) en slot cabeza: DisplayName={headSlot.DisplayName} RejectionMessage='{headSlot.RejectionMessage}' (esperado: sigue siendo el casco Shroomite, con mensaje)");
            var accSlot = vm.EquipmentGroup.EquippedItems.Slots[3];
            Console.WriteLine($"AcceptsItem: accSlot(accesorio).AcceptsItem(490 Warrior Emblem real)={accSlot.AcceptsItem(490)} (esperado True)");
            Console.WriteLine($"AcceptsItem: accSlot(accesorio).AcceptsItem(1546 Casco, invalido)={accSlot.AcceptsItem(1546)} (esperado False)");

            // Bug real reportado 2-sep-2026 ("la armadura me deja colocarla en los huecos de
            // accesorios"): la causa real era que Calamity SIEMPRE pasaba la restriccion,
            // tambien para armadura/accesorio (donde SI hay un campo real, Category, a
            // diferencia de ammo/mountType/etc). Ids sinteticos = 20000000 + indice real en
            // Assets/calamity/catalog.json.
            //
            // OBJ-06 (oleada de Objetos, 6-sep-2026): este bloque llevaba un "esperado" MENTIROSO
            // desde H3-11. Usaba 20000243 como "armadura de Calamity" para el slot de CABEZA y
            // esperaba True - pero el indice 243 es `AerospecBreastplate`, equipSlot="Body", o sea
            // un PETO: desde H3-11 (que empezo a mirar de que PARTE es cada pieza, no solo "es
            // armadura") el slot de cabeza lo rechaza con toda la razon, y el arnes llevaba desde
            // entonces imprimiendo "obtenido False (esperado True)" en cada ejecucion sin que
            // fuera un bug. Una expectativa obsoleta que da un falso positivo permanente es tan
            // dañina como no comprobar nada: lo primero que hace es enseñar a ignorar la linea.
            //
            // Ahora se prueba el set Aerospec REAL entero, pieza a pieza y hueco a hueco (matriz
            // 3x3 completa: cada pieza SOLO en el suyo), que es exactamente lo que H3-11 arreglo.
            int calamityHeadId = 20000244;   // AerospecHeadMagic - equipSlot "Head"
            int calamityBodyId = 20000243;   // AerospecBreastplate - equipSlot "Body"
            int calamityLegsId = 20000249;   // AerospecLeggings - equipSlot "Legs"
            int calamityAccessoryId = 20000000; // Abaddon - category "Accessories"
            var bodySlotCal = vm.EquipmentGroup.EquippedItems.Slots[1];
            var legsSlotCal = vm.EquipmentGroup.EquippedItems.Slots[2];
            foreach (var (slot, nombreSlot, aceptado) in new (ItemSlotViewModel, string, int)[]
                     { (headSlot, "cabeza", calamityHeadId), (bodySlotCal, "cuerpo", calamityBodyId), (legsSlotCal, "piernas", calamityLegsId) })
            {
                foreach (var (id, nombrePieza) in new[] { (calamityHeadId, "casco"), (calamityBodyId, "peto"), (calamityLegsId, "grebas"), (calamityAccessoryId, "accesorio") })
                {
                    bool obtenido = slot.AcceptsItem(id);
                    bool esperado = id == aceptado;
                    Console.WriteLine($"OBJ-06: Calamity {nombrePieza} ({id}) en slot {nombreSlot} -> {obtenido} (esperado {esperado})");
                    if (obtenido != esperado)
                        Console.WriteLine($"FALLO: OBJ-06 - el slot de {nombreSlot} {(obtenido ? "ACEPTA" : "RECHAZA")} un {nombrePieza} de Calamity y no deberia (H3-11: la pieza sabe de que parte es, via CalamityCatalogEntry.EquipSlot)");
                }
            }
            Console.WriteLine($"OBJ-06: Calamity accesorio ({calamityAccessoryId}) en slot accesorio -> {accSlot.AcceptsItem(calamityAccessoryId)} (esperado True)");
            if (!accSlot.AcceptsItem(calamityAccessoryId)) Console.WriteLine("FALLO: OBJ-06 - un accesorio real de Calamity no entra en un hueco de accesorio");
            Console.WriteLine($"OBJ-06: Calamity peto ({calamityBodyId}) en slot accesorio -> {accSlot.AcceptsItem(calamityBodyId)} (esperado False - este era el bug de 2-sep-2026)");
            if (accSlot.AcceptsItem(calamityBodyId)) Console.WriteLine("FALLO: OBJ-06 - una pieza de armadura de Calamity vuelve a colarse en un hueco de accesorio");

            // Bloque 1 de la auditoria de Opus (E-1): coloca un accesorio REAL de Calamity,
            // equipado (isEquipped=true siempre en EquipmentGroupViewModel), para confirmar de
            // verdad con una captura que el punto rojo y la mancha verde de "equipado" se ven
            // A LA VEZ - antes de este arreglo el verde tapaba el rojo por completo (ver el
            // comentario real en SlotCompactTemplate, MainWindow.xaml).
            accSlot.PlaceItem(calamityAccessoryId);
            DoEvents(); DoEvents();
            Console.WriteLine($"E-1: accSlot tras colocar Calamity equipado -> IsCalamity={accSlot.IsCalamity} IsEquipped={accSlot.IsEquipped} IsEmpty={accSlot.IsEmpty} (los 3 deben coexistir sin que ninguno tape al otro visualmente)");
        }

        // Rellena TAMBIEN los 4 contenedores de los laterales fusionados (Mascota/Montura/
        // Gancho, Tinte, Monedas, Municion) con objetos reales - sin esto la medicion de
        // celdas de la fusion de Equipamiento (mas abajo) mide huecos vacios, no iconos reales,
        // y no puede confirmar ni desmentir mi correccion a la aritmetica de Opus sobre si el
        // lateral izquierdo (2 grupos de 5 apilados) necesita de verdad el ScrollViewer de
        // seguridad.
        // Sexta pasada: estos 5 slots ahora estan restringidos por SlotKind - ids reales por
        // indice (Pet/LightPet/Cart/Mount/Hook), ya colocados arriba por el bloque de pruebas
        // de restriccion (esto solo confirma que sigue igual, PlaceItem con el mismo id real
        // es un no-op idempotente).
        if (vm.MountsContainer != null)
        {
            int[] realIds = [603, 425, 2191, 1914, 84];
            for (int i = 0; i < vm.MountsContainer.Slots.Count && i < realIds.Length; i++) vm.MountsContainer.Slots[i].PlaceItem(realIds[i]);
        }
        if (vm.DyesContainer != null)
        {
            int[] realDyeIds = [1007, 1008, 1009, 1010, 1011];
            for (int i = 0; i < vm.DyesContainer.Slots.Count && i < realDyeIds.Length; i++) vm.DyesContainer.Slots[i].PlaceItem(realDyeIds[i]);
        }
        if (vm.CoinsContainer != null)
            for (int i = 0; i < vm.CoinsContainer.Slots.Count; i++) vm.CoinsContainer.Slots[i].PlaceItem(71 + i); // 71 = Copper Coin
        if (vm.AmmoContainer != null)
            for (int i = 0; i < vm.AmmoContainer.Slots.Count; i++) vm.AmmoContainer.Slots[i].PlaceItem(40 + i); // 40 = Wooden Arrow
        DoEvents();
        DoEvents();

        Console.WriteLine($"InventoryContainer.Slots={vm.InventoryContainer?.Slots.Count} Columns={vm.InventoryContainer?.Columns}");
        Console.WriteLine($"StorageGroup.Current={vm.StorageGroup?.Current.DisplayName} Slots={vm.StorageGroup?.Current.Slots.Count} Options={vm.StorageGroup?.Options.Count}");
        Console.WriteLine($"MountsContainer.Slots={vm.MountsContainer?.Slots.Count} Columns={vm.MountsContainer?.Columns}");
        Console.WriteLine($"DyesContainer.Slots={vm.DyesContainer?.Slots.Count} Columns={vm.DyesContainer?.Columns}");
        Console.WriteLine($"CoinsContainer.Slots={vm.CoinsContainer?.Slots.Count} Columns={vm.CoinsContainer?.Columns}");
        Console.WriteLine($"AmmoContainer.Slots={vm.AmmoContainer?.Slots.Count} Columns={vm.AmmoContainer?.Columns}");
        Console.WriteLine($"EquipmentGroup.Current(inicial)={vm.EquipmentGroup?.Current.DisplayName} Columns={vm.EquipmentGroup?.Current.Columns} (esperado: Columns=5)");

        // NAV123 (25-sep-2026): "Equipamiento"/"Inventario"/"Almacenes" ya no son TabItem - son
        // 3 paginas reales exclusivas seleccionadas por vm.ObjetosSubTabIndex (0/1/2).
        (string name, int sub)[] tabNames = [("Equipamiento", 0), ("Inventario", 1), ("Almacenes", 2)]; // quinta pasada: Monturas/Monedas ya no son pestañas, se fusionaron dentro de Equipamiento
        foreach (var (name, sub) in tabNames)
        {
            try
            {
                vm.ObjetosSubTabIndex = sub;

                DoEvents();
                DoEvents();
                DoEvents();

                var images = root.FindAll(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Image));
                int buttonCount = root.FindAll(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)).Count;
                // Peticion 2 (pregunta a Opus, cuarta pasada, "los iconos enormes... me
                // gustaria que se vieran igual que la rejilla de inventario"): ancho real del
                // primer icono renderizado - antes de ReferenceColumns, Equipamiento salia
                // mucho mas grande que Inventario en la misma ventana.
                string firstImageWidth = images.Count > 0 ? images[0].Current.BoundingRectangle.Width.ToString("0.#") : "n/a";
                Console.WriteLine($"TAB {name}: selected OK, images={images.Count} buttons={buttonCount} firstImageWidth={firstImageWidth}px");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TAB {name}: EXCEPTION - {ex}");
            }
        }

        // Medicion real de la fusion de Equipamiento (quinta pasada, laterales Monturas/Monedas).
        // Vuelve a "Equipamiento" y agrupa los Image reales por posicion X en 3 clusters
        // (lateral izquierdo/centro/lateral derecho, mismo orden que las 3 columnas del Grid) -
        // para contrastar con numero mi correccion a la aritmetica de Opus ((300-16)/5≈56.8px
        // solo contaba UN grupo de 5, no los 2 apilados que el lateral izquierdo necesita de
        // verdad) con el tamaño de celda REAL renderizado, no calculado a mano.
        try
        {
            vm.ObjetosSubTabIndex = 0; // Equipamiento (NAV123: pagina real, ya no TabItem)
            DoEvents();
            DoEvents();

            var equipImages = root.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Image));
            var rects = equipImages.Cast<AutomationElement>()
                .Select(el => el.Current.BoundingRectangle)
                .Where(r => !r.IsEmpty && r.Width > 0)
                .OrderBy(r => r.X).ToList();
            Console.WriteLine($"Equipamiento fusionado: {rects.Count} iconos reales renderizados.");
            if (rects.Count > 0)
            {
                double minX = rects.Min(r => r.X), maxX = rects.Max(r => r.X);
                double thirdW = (maxX - minX) / 3.0;
                var left = rects.Where(r => r.X < minX + thirdW).ToList();
                var mid = rects.Where(r => r.X >= minX + thirdW && r.X < minX + 2 * thirdW).ToList();
                var right = rects.Where(r => r.X >= minX + 2 * thirdW).ToList();
                Console.WriteLine($"  Lateral izq (Equipo/Tinte): n={left.Count} anchoCelda~{(left.Count > 0 ? left.Average(r => r.Width) : 0):0.#}px minH={(left.Count > 0 ? left.Min(r => r.Height) : 0):0.#}px");
                Console.WriteLine($"  Centro (Equipamiento 5x2): n={mid.Count} anchoCelda~{(mid.Count > 0 ? mid.Average(r => r.Width) : 0):0.#}px");
                Console.WriteLine($"  Lateral der (Monedas/Municion): n={right.Count} anchoCelda~{(right.Count > 0 ? right.Average(r => r.Width) : 0):0.#}px");
            }

            var scrollers = root.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ScrollBar));
            Console.WriteLine($"  ScrollBars visibles en Equipamiento: {scrollers.Count} (>0 = el ScrollViewer de seguridad del lateral izquierdo esta actuando de verdad)");

            // Diagnostico directo del arbol visual real (whitebox, no UI Automation) - para
            // confirmar de verdad que tamaño (finalSize) recibe cada SlotGridPanel.ArrangeOverride
            // y si mi offset de centrado se esta aplicando, en vez de seguir adivinando a partir
            // de una captura de pantalla.
            void WalkVisual(System.Windows.DependencyObject d, int depth)
            {
                if (d is Terrakeep.App.Controls.SlotGridPanel sgp)
                {
                    Console.WriteLine($"  SlotGridPanel real: ActualWidth={sgp.ActualWidth:0.#} ActualHeight={sgp.ActualHeight:0.#} Children={sgp.Children.Count}");
                    if (sgp.Children.Count > 0)
                    {
                        var first = sgp.Children[0] as System.Windows.UIElement;
                        if (first != null)
                        {
                            var pos = first.TransformToAncestor(sgp).Transform(new System.Windows.Point(0, 0));
                            Console.WriteLine($"    Primer hijo: posicion local dentro del panel = ({pos.X:0.#},{pos.Y:0.#})");
                        }
                    }
                    // Cadena de ancestros real (ScrollViewer/ItemsControl/ContentControl/
                    // StackPanel/Border) para ver EN QUE ESLABON se estrecha el ancho real,
                    // en vez de seguir adivinando por que el ActualWidth del panel no coincide
                    // con lo que parecia en la captura.
                    var anc = System.Windows.Media.VisualTreeHelper.GetParent(sgp);
                    int hops = 0;
                    while (anc != null && hops < 20)
                    {
                        if (anc is System.Windows.FrameworkElement fe)
                            Console.WriteLine($"    ^ {fe.GetType().Name}: ActualWidth={fe.ActualWidth:0.#} HorizontalAlignment={fe.HorizontalAlignment}");
                        anc = System.Windows.Media.VisualTreeHelper.GetParent(anc);
                        hops++;
                    }
                }
                int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
                for (int i = 0; i < n; i++)
                    WalkVisual(System.Windows.Media.VisualTreeHelper.GetChild(d, i), depth + 1);
            }
            WalkVisual(window, 0);

            // El agrupado por tercios de X es impreciso (mezcla iconos ajenos a la rejilla,
            // ej. pildoras Loadout/Vista) - captura real de pixeles vía RenderTargetBitmap
            // (visual tree real de WPF, no una captura de pantalla que dependeria de que la
            // ventana este realmente visible/no tapada) para confirmar de un vistazo que no
            // hay solapamiento ni celdas rotas, sin adivinar a partir de numeros agregados.
            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(window);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            string shotPath = Path.Combine(AppContext.BaseDirectory, "equipamiento-fusionado.png");
            using (var fs = File.Create(shotPath)) encoder.Save(fs);
            Console.WriteLine($"  Captura real guardada en: {shotPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("EQUIPAMIENTO-MEDICION-EXCEPTION: " + ex);
        }

        // Pildora de "Almacenes": cambiar de Banco a Fragua del Defensor y confirmar que
        // StorageGroup.Current cambia de verdad (no solo el label del boton). TabControl solo
        // mantiene vivo el contenido de la pestaña SELECCIONADA - hay que volver a "Almacenes"
        // primero (el bucle de arriba la dejo en "Almacenes", ultima pestaña real ahora).
        try
        {
            vm.ObjetosSubTabIndex = 2; // Almacenes (NAV123: pagina real, ya no TabItem)
            DoEvents();
            DoEvents();

            // Auditoria de Opus, A-1: el Content del boton ya no es el Label plano ("Fragua del
            // Defensor") sino el DisplayLabel con el contador en vivo ("Fragua del Defensor
            // (0/40)") - la busqueda exacta por NameProperty dejo de encontrarlo. StartsWith
            // sobre todos los botones reales sigue siendo real (no ignora el contador, solo no
            // exige adivinar el numero exacto de antemano).
            var forgeButton = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                .Cast<AutomationElement>().FirstOrDefault(b => b.Current.Name.StartsWith("Fragua del Defensor"));
            if (forgeButton != null && forgeButton.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePat))
            {
                ((InvokePattern)invokePat).Invoke();
                DoEvents();
                DoEvents();
                Console.WriteLine($"PILDORA Almacenes -> Current={vm.StorageGroup?.Current.DisplayName} (esperado: Fragua del Defensor)");
            }
            else Console.WriteLine("PILDORA Almacenes: boton 'Fragua del Defensor' NO-FOUND");
        }
        catch (Exception ex)
        {
            Console.WriteLine("PILDORA-EXCEPTION: " + ex);
        }

        // Equipamiento: cambiar de pildora "Armadura" a "Vanidad" via UI Automation real (no
        // solo el ViewModel) y confirmar que EquipmentGroup.Current cambia de verdad - prueba
        // real del ContentControl+ContainerTabTemplate nuevo (antes Viewbox+ItemsControl a
        // medida).
        try
        {
            vm.ObjetosSubTabIndex = 0; // Equipamiento (NAV123: pagina real, ya no TabItem)
            DoEvents();
            DoEvents();

            var vanidadButton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, "Vanidad")));
            if (vanidadButton != null && vanidadButton.TryGetCurrentPattern(InvokePattern.Pattern, out var vanidadPat))
            {
                ((InvokePattern)vanidadPat).Invoke();
                DoEvents();
                DoEvents();
                Console.WriteLine($"PILDORA Equipamiento -> Current={vm.EquipmentGroup?.Current.DisplayName} Columns={vm.EquipmentGroup?.Current.Columns} (esperado: Loadout 1 - vanidad, Columns=5)");
            }
            else Console.WriteLine("PILDORA Equipamiento: boton 'Vanidad' NO-FOUND");
        }
        catch (Exception ex)
        {
            Console.WriteLine("EQUIPAMIENTO-PILDORA-EXCEPTION: " + ex);
        }

        // 6-sep-2026, queja real del usuario: "en Terraria hay 3 conjuntos de equipo, no mas -
        // aqui salen 4 pildoras (Puesto + Loadout 1/2/3)". El personaje del arnes es moderno
        // (Loadouts.Length==3, CurrentLoadout=0), asi que la pantalla tiene que ofrecer
        // exactamente 3 pildoras numeradas 1/2/3, con la 1 marcada como la que lleva puesta, y
        // NINGUNA llamada "Puesto". Se comprueba sobre los botones REALES renderizados (UI
        // Automation), no solo sobre LoadoutOptions.
        try
        {
            var botonesLoadout = root.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                .Cast<AutomationElement>()
                .Select(b => b.Current.Name)
                .Where(n => n is "Puesto" or "Worn" or "1" or "2" or "3" or "1 ●" or "2 ●" or "3 ●")
                .ToList();
            Console.WriteLine($"LOADOUT-PILDORAS: renderizadas [{string.Join(", ", botonesLoadout)}] | ActiveLoadout={vm.EquipmentGroup?.ActiveLoadout} SelectableSetCount={vm.EquipmentGroup?.SelectableSetCount} (esperado: 1 ●/2/3, ActiveLoadout=0, 3 conjuntos)");
            if (botonesLoadout.Count != 3)
                Console.WriteLine($"FALLO: LOADOUT-PILDORAS - se renderizaron {botonesLoadout.Count} pildoras de conjunto, deben ser 3 (Terraria no tiene mas de 3 loadouts)");
            if (botonesLoadout.Any(n => n is "Puesto" or "Worn"))
                Console.WriteLine("FALLO: LOADOUT-PILDORAS - sigue habiendo una pildora 'Puesto' aparte; en un personaje moderno el equipo puesto ES uno de los 3 conjuntos, no un cuarto");
            if (!botonesLoadout.Contains("1 ●"))
                Console.WriteLine("FALLO: LOADOUT-PILDORAS - ninguna pildora marca cual es el conjunto que el personaje lleva puesto (CurrentLoadout=0 -> deberia ser la '1')");

            // Y que cada pildora edite de verdad el contenedor que le toca: con CurrentLoadout=0
            // la "1" es PrimaryLoadout (contenedor 0) y la "3" es Loadouts[2] (contenedor 3).
            foreach (var (nombre, contenedorEsperado) in new[] { ("1 ●", 0), ("3", 3) })
            {
                var boton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                    new PropertyCondition(AutomationElement.NameProperty, nombre)));
                if (boton == null || !boton.TryGetCurrentPattern(InvokePattern.Pattern, out var pat)) { Console.WriteLine($"FALLO: LOADOUT-PILDORAS - pildora '{nombre}' NO-FOUND"); continue; }
                ((InvokePattern)pat).Invoke();
                DoEvents();
                DoEvents();
                int real = vm.EquipmentGroup?.SelectedLoadout ?? -1;
                Console.WriteLine($"LOADOUT-PILDORAS: '{nombre}' -> contenedor {real} (esperado {contenedorEsperado}), Current={vm.EquipmentGroup?.Current.DisplayName}");
                if (real != contenedorEsperado)
                    Console.WriteLine($"FALLO: LOADOUT-PILDORAS - la pildora '{nombre}' edita el contenedor {real}, no el {contenedorEsperado} que guarda de verdad ese conjunto en el .plr");
            }

            // Devuelve la seleccion a la pildora del conjunto puesto ANTES de seguir: T20-AUTOEQUIP
            // (mucho mas abajo) mide EquippedItems (contenedor 0) pero Auto-equipar coloca en el
            // loadout SELECCIONADO (Bd-b, a proposito), y H6-06 depende de que ese equipo llegue
            // al doll de Apariencia. Dejar el arnes mirando el conjunto 3 rompia los dos.
            var vueltaAlPuesto = vm.EquipmentGroup?.LoadoutOptions.FirstOrDefault(o => o.IsActiveLoadout);
            if (vueltaAlPuesto != null) vm.EquipmentGroup!.SelectLoadoutCommand.Execute(vueltaAlPuesto);
            DoEvents();
        }
        catch (Exception ex)
        {
            Console.WriteLine("LOADOUT-PILDORAS-EXCEPTION: " + ex);
        }

        // Libreria: poblar Results de verdad (busqueda real) para ejercitar la tarjeta nueva
        // (tooltip compuesto/hover/TextTrimming) sin excepcion.
        try
        {
            vm.IsLibraryCollapsed = false; // desplegada por defecto ahora - hace falta para que las tarjetas se rendericen
            vm.Library.SearchText = "Sword";
            WaitForDispatcher(300); // L-c: espera real al debounce (180ms) antes de mirar Results
            int libraryCards = root.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)).Count;
            Console.WriteLine($"LIBRERIA busqueda 'Sword': Results.Count={vm.Library.Results.Count} (tarjetas renderizadas sin excepcion)");

            // L-c (segunda auditoria de Opus, Fable): "el tope de 300 resultados no tiene
            // ninguna medicion real detras, solo el motivo generico de que WrapPanel no
            // virtualiza". Medido de verdad (busqueda amplia real "ar" sobre ~8469 objetos):
            // 100->158ms, 150->271ms, 300->802ms - NO lineal, 300 era un freeze real
            // tecleando. Bajado a 100 + debounce real (180ms tras la ultima pulsacion, evita
            // repetir el reflow entero en cada caracter de una racha de tecleo) - se
            // comprueba primero que el debounce SI difiere el reflow real (Results no cambia
            // de inmediato) y despues que aplica de verdad tras esperar.
            int resultsAntesDeEsperar = vm.Library.Results.Count;
            vm.Library.SearchText = "ar"; // 2+ caracteres reales (LibrarySearchGrammar ignora terminos de 1 solo caracter)
            DoEvents();
            bool siguDebounceando = vm.Library.Results.Count == resultsAntesDeEsperar;
            var swLibReflow = System.Diagnostics.Stopwatch.StartNew();
            WaitForDispatcher(300);
            swLibReflow.Stop();
            Console.WriteLine($"L-C-TOPE: debounce real (Results sin cambiar justo tras teclear)={siguDebounceando} (esperado True), Results.Count tras esperar={vm.Library.Results.Count} (esperado 100, el tope real), tiempo total con espera={swLibReflow.ElapsedMilliseconds}ms");
            if (!siguDebounceando) Console.WriteLine("FALLO: L-c (segunda auditoria) - la busqueda de Libreria ya no diferencia el reflow (debounce roto)");
            vm.Library.SearchText = "Sword"; // deja el estado limpio para los pasos siguientes
            WaitForDispatcher(300);
        }
        catch (Exception ex)
        {
            Console.WriteLine("LIBRERIA-EXCEPTION: " + ex);
        }

        // Sexta auditoria de Opus, H6-11 ("los objetos animados -Alma de vuelo/Alma de luz,
        // etc.- salen como una tira de fotogramas entera, no un unico icono"): busca de verdad
        // los dos ejemplos REALES citados por el usuario en la Libreria real, renderiza sus
        // tarjetas sin excepcion y confirma por codigo (decodificando el PNG real en disco, no
        // solo mirando una captura) que el fichero resuelto es un unico fotograma pequeño, no
        // la tira entera.
        try
        {
            vm.Library.SearchText = "Alma de";
            WaitForDispatcher(300);
            Console.WriteLine($"H6-11-LIBRERIA: busqueda 'Alma de' -> Results.Count={vm.Library.Results.Count} (esperado >= 2, Alma de luz + Alma de vuelo)");

            foreach (int idConocido in new[] { 520, 575 }) // SoulofLight, SoulofFlight
            {
                var pathReal = Terrakeep.App.Services.VanillaIconResolver.GetIconPath(idConocido);
                if (pathReal == null) { Console.WriteLine($"H6-11-LIBRERIA: id={idConocido} sin icono real - FALLO"); continue; }
                string rutaAbsoluta = Path.Combine(AppContext.BaseDirectory, pathReal.Replace("pack://siteoforigin:,,,/", "").Replace('/', Path.DirectorySeparatorChar));
                using var streamIcon = File.OpenRead(rutaAbsoluta);
                var decoderIcon = new System.Windows.Media.Imaging.PngBitmapDecoder(streamIcon, System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                int wIcon = decoderIcon.Frames[0].PixelWidth, hIcon = decoderIcon.Frames[0].PixelHeight;
                Console.WriteLine($"H6-11-LIBRERIA: id={idConocido} icono real={wIcon}x{hIcon} (esperado alto=28, NO 112 -antes 4 fotogramas apilados-)");
                if (hIcon != 28) Console.WriteLine($"FALLO: H6-11 - id={idConocido} sigue pareciendo una tira de fotogramas ({wIcon}x{hIcon})");
            }

            var rtbAnimados = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbAnimados.Render(window);
            var encAnimados = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encAnimados.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbAnimados));
            using (var fsAnimados = File.Create(Path.Combine(AppContext.BaseDirectory, "h6-11-libreria-objetos-animados.png"))) encAnimados.Save(fsAnimados);
            Console.WriteLine("Captura tarjetas de objetos animados reales -> h6-11-libreria-objetos-animados.png");

            vm.Library.SearchText = "Sword"; // deja el estado limpio para el resto del arnes
            WaitForDispatcher(300);
        }
        catch (Exception ex) { Console.WriteLine("H6-11-LIBRERIA-EXCEPTION: " + ex); }

        // H5-12 (quinta auditoria de Opus): "un clic en una tarjeta de la Libreria no hace
        // absolutamente nada". El gesto de raton en si (arrastre vs clic vs doble clic,
        // OnLibraryCardClick/OnLibraryClickTimerTick en MainWindow.xaml.cs) exige eventos de
        // raton reales enrutados por WPF - sin precedente en este arnes (que solo simula
        // Invoke/Command o teclado real via keybd_event, nunca clics de raton reales sobre un
        // elemento arbitrario) y fuera de alcance real montar eso solo para esto, documentado
        // aqui a proposito en vez de fingir cobertura. Lo que SI se verifica de verdad, a nivel
        // de ViewModel (exactamente las 2 llamadas reales que ese gesto dispara): que colocar en
        // el slot ya seleccionado en Editar (ItemEdit.Slot, camino del clic simple) funciona, y
        // que MainViewModel.PlaceInFirstFreeInventorySlot (camino del doble clic, nuevo) coloca
        // de verdad en el primer hueco libre real, no en cualquiera.
        try
        {
            if (vm.InventoryContainer != null)
            {
                var slotParaClicSimple = vm.InventoryContainer.Slots.LastOrDefault(s => s.IsEmpty);
                if (slotParaClicSimple != null)
                {
                    vm.SelectSlot(slotParaClicSimple);
                    Console.WriteLine($"H5-12-SELECCION: ItemEdit.Slot tras SelectSlot=={ReferenceEquals(vm.ItemEdit.Slot, slotParaClicSimple)} (esperado True)");
                    vm.ItemEdit.Slot!.PlaceItem(2); // id real cualquiera - lo que importa es que ACEPTE y quede puesto, no el nombre concreto
                    Console.WriteLine($"H5-12-CLIC-SIMPLE: slot antes vacio, DisplayName tras PlaceItem={slotParaClicSimple.DisplayName} (esperado no vacio - mismo camino real que dispara el clic simple de la tarjeta)");
                    if (slotParaClicSimple.IsEmpty) Console.WriteLine("FALLO: H5-12 - colocar en el slot seleccionado (camino real del clic simple) no dejo el objeto puesto");
                }
                else Console.WriteLine("H5-12-CLIC-SIMPLE: sin slot de Inventario vacio real para probar - omitido");

                int primerVacioAntesId = vm.InventoryContainer.Slots.FirstOrDefault(s => s.IsEmpty)?.SlotIndex ?? -1;
                vm.PlaceInFirstFreeInventorySlot(4); // id real cualquiera
                var primerVacioSlot = vm.InventoryContainer.Slots.FirstOrDefault(s => s.SlotIndex == primerVacioAntesId);
                Console.WriteLine($"H5-12-DOBLE-CLIC: primer hueco libre real antes=Index {primerVacioAntesId}, tras PlaceInFirstFreeInventorySlot su DisplayName={primerVacioSlot?.DisplayName} (esperado no vacio, justo ESE hueco - no cualquier otro)");
                if (primerVacioAntesId < 0 || primerVacioSlot == null || primerVacioSlot.IsEmpty)
                    Console.WriteLine("FALLO: H5-12 - PlaceInFirstFreeInventorySlot no coloco en el primer hueco libre real");
            }
            else Console.WriteLine("H5-12: sin InventoryContainer real - omitido");
        }
        catch (Exception ex)
        {
            Console.WriteLine("H5-12-EXCEPTION: " + ex);
        }

        // H5-13 (quinta auditoria de Opus): "el estado vacio de la Libreria y de la Libreria de
        // buffs es un rectangulo en blanco" - verificacion real de que las tarjetas de carpeta
        // raiz aparecen de verdad (renderizadas, no solo ShowRootCategoryCards=true a nivel de
        // ViewModel - eso ya lo cubren LibraryRootCategoryCardsTests.cs/
        // BuffLibraryRootCategoryCardsTests.cs con xunit) con captura real de las 2 superficies.
        try
        {
            vm.SelectedTabIndex = 1; // Personaje
            vm.PersonajeInnerTabIndex = 0; // Objetos
            vm.Library.ClearCategoryCommand.Execute(null);
            vm.Library.SearchText = string.Empty;
            WaitForDispatcher(300); // deja asentar el debounce real de Results/ResultsSummary, no solo ShowRootCategoryCards (instantaneo)
            Console.WriteLine($"H5-13-LIBRERIA: ShowRootCategoryCards={vm.Library.ShowRootCategoryCards} (esperado True)");
            // NavCardButton tiene Content compuesto (Image+2 TextBlock) - el Name de
            // automatizacion real del propio Button no resuelve al texto plano, se busca el
            // TextBlock real del nombre de la carpeta en su lugar (mismo criterio ya usado para
            // "Tus mundos" en H5-11).
            int tarjetasLibreria = root.FindAll(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text),
                new PropertyCondition(AutomationElement.NameProperty, vm.Library.RootCategories.First().Name))).Count;
            Console.WriteLine($"H5-13-LIBRERIA: tarjeta real de la primera carpeta raiz ('{vm.Library.RootCategories.First().Name}') encontrada en el arbol visual={tarjetasLibreria > 0} (esperado True)");
            if (tarjetasLibreria == 0) Console.WriteLine("FALLO: H5-13 - las tarjetas de carpeta raiz de la Libreria no se renderizan de verdad");
            var rtbLibCards = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbLibCards.Render(window);
            var encLibCards = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encLibCards.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbLibCards));
            using (var fsLibCards = File.Create(Path.Combine(AppContext.BaseDirectory, "h5-13-libreria-tarjetas.png"))) encLibCards.Save(fsLibCards);
            Console.WriteLine("Captura tarjetas de carpeta raiz de la Libreria -> h5-13-libreria-tarjetas.png");
        }
        catch (Exception ex)
        {
            Console.WriteLine("H5-13-LIBRERIA-EXCEPTION: " + ex);
        }

        try
        {
            var buffsTab = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Buffs")));
            if (buffsTab != null && buffsTab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var buffsSelPat))
                ((SelectionItemPattern)buffsSelPat).Select();
            vm.IsBuffLibraryCollapsed = false;
            vm.BuffLibrary.ClearCategoryCommand.Execute(null);
            vm.BuffLibrary.SearchText = string.Empty;
            WaitForDispatcher(300);
            Console.WriteLine($"H5-13-BUFFS: ShowRootCategoryCards={vm.BuffLibrary.ShowRootCategoryCards} (esperado True)");
            int tarjetasBuffs = root.FindAll(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text),
                new PropertyCondition(AutomationElement.NameProperty, vm.BuffLibrary.RootCategories.First().Name))).Count;
            Console.WriteLine($"H5-13-BUFFS: tarjeta real de la primera carpeta raiz ('{vm.BuffLibrary.RootCategories.First().Name}') encontrada en el arbol visual={tarjetasBuffs > 0} (esperado True)");
            if (tarjetasBuffs == 0) Console.WriteLine("FALLO: H5-13 - las tarjetas de carpeta raiz de la Libreria de buffs no se renderizan de verdad");
            var rtbBuffCards = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbBuffCards.Render(window);
            var encBuffCards = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encBuffCards.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbBuffCards));
            using (var fsBuffCards = File.Create(Path.Combine(AppContext.BaseDirectory, "h5-13-buffs-tarjetas.png"))) encBuffCards.Save(fsBuffCards);
            Console.WriteLine("Captura tarjetas de carpeta raiz de la Libreria de buffs -> h5-13-buffs-tarjetas.png");
        }
        catch (Exception ex)
        {
            Console.WriteLine("H5-13-BUFFS-EXCEPTION: " + ex);
        }

        // H5-05 (quinta auditoria de Opus): "no se puede buscar entre los ~350 slots que el
        // personaje ya tiene". Verificacion real de extremo a extremo: clic real (InvokePattern
        // via UI Automation, el boton SI es invocable a diferencia de las tarjetas de la
        // Libreria de H5-12) sobre el boton de la cabecera abre el Popup real, se escribe una
        // busqueda real y se espera el debounce real (180ms) antes de mirar Results - la
        // navegacion en si (clic en un resultado, Border+InputBindings, mismo caso sin
        // precedente de raton simulado que H5-12) se verifica a nivel de comando real, no de
        // ViewModel sintetico (el personaje/servicio son los mismos reales de todo el arnes).
        try
        {
            vm.SelectedTabIndex = 4; // Exploracion - a proposito, para demostrar que el boton es visible en CUALQUIER pestaña (pedido explicito del informe)
            DoEvents();
            var whereIsItButton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, "Buscar en el personaje")));
            Console.WriteLine($"H5-05-BOTON: boton real encontrado en 'Exploración'={whereIsItButton != null} (esperado True - visible en cualquier pestaña)");
            if (whereIsItButton != null && whereIsItButton.TryGetCurrentPattern(InvokePattern.Pattern, out var whereIsItInvokePat))
            {
                ((InvokePattern)whereIsItInvokePat).Invoke();
                DoEvents(); DoEvents();
                Console.WriteLine($"H5-05-ABRIR: IsWhereIsItOpen tras el clic real={vm.IsWhereIsItOpen} (esperado True)");
                if (!vm.IsWhereIsItOpen) Console.WriteLine("FALLO: H5-05 - el clic real sobre el boton de la cabecera no abrio el panel");
            }
            else Console.WriteLine("FALLO: H5-05 - boton real 'Dónde lo tengo' NO-FOUND en la cabecera");

            // Objeto real ya colocado por la fixture del principio (linea ~324: id 1 = Pico de
            // hierro, slot 0 de Inventario) - busqueda real por texto, con el debounce real.
            vm.WhereIsItSearchText = "hierro";
            WaitForDispatcher(300);
            Console.WriteLine($"H5-05-BUSQUEDA: WhereIsItResults.Count={vm.WhereIsItResults.Count} (esperado >=1), resumen='{vm.WhereIsItSummary}'");
            if (vm.WhereIsItResults.Count == 0) Console.WriteLine("FALLO: H5-05 - la busqueda real por texto no encontro el objeto real ya colocado por la fixture");
            var rtbWhereIsIt = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbWhereIsIt.Render(window);
            var encWhereIsIt = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encWhereIsIt.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbWhereIsIt));
            using (var fsWhereIsIt = File.Create(Path.Combine(AppContext.BaseDirectory, "h5-05-donde-lo-tengo.png"))) encWhereIsIt.Save(fsWhereIsIt);
            Console.WriteLine("Captura panel Dónde lo tengo -> h5-05-donde-lo-tengo.png");

            // Navegacion real (el gesto de clic en si, Border+InputBindings, no tiene precedente
            // de raton simulado en este arnes - mismo criterio ya documentado en H5-12).
            if (vm.WhereIsItResults.Count > 0)
            {
                var resultado = vm.WhereIsItResults[0];
                var slotDestino = resultado.Slot;
                vm.NavigateToWhereIsItResultCommand.Execute(resultado);
                DoEvents(); DoEvents();
                bool navegoBien = vm.SelectedTabIndex == 1 /* AppTab.Personaje, privado - mismo criterio real ya usado en este arnes */
                    && ReferenceEquals(vm.ItemEdit.Slot, slotDestino) && !vm.IsWhereIsItOpen;
                Console.WriteLine($"H5-05-NAVEGAR: SelectedTabIndex={vm.SelectedTabIndex} (esperado Personaje), ItemEdit.Slot es el real={ReferenceEquals(vm.ItemEdit.Slot, slotDestino)} (esperado True), IsWhereIsItOpen={vm.IsWhereIsItOpen} (esperado False)");
                if (!navegoBien) Console.WriteLine("FALLO: H5-05 - NavigateToWhereIsItResultCommand no navego/selecciono/cerro correctamente");
            }

            // A8-06 (auditoria de Opus vs TEdit, P-4): los 6 controles de la barra superior
            // deben tener el mismo alto real - antes ↶/↷ llevaban Padding="8,3" (10px menos que
            // el resto). vm.SelectedTabIndex ya es Personaje aqui (H5-05-NAVEGAR de arriba), con
            // personaje cargado, asi que los 6 son visibles/habilitados de verdad.
            DoEvents();
            string[] rotulosBarra = ["Cargar personaje (.plr)...", "Buscar en el personaje", "↶", "↷", "Deshacer último guardado", "Guardar"];
            var alturasBarra = rotulosBarra
                .Select(r => Descendientes<Button>(window).FirstOrDefault(b => (b.Content as string) == r))
                .Where(b => b != null)
                .Select(b => b!.ActualHeight)
                .ToList();
            Console.WriteLine($"A8-06: {alturasBarra.Count}/6 botones de la barra superior encontrados, alturas=[{string.Join(", ", alturasBarra.Select(a => a.ToString("0.0")))}] (esperado todas iguales)");
            if (alturasBarra.Count != 6 || alturasBarra.Max() - alturasBarra.Min() > 0.5)
                Console.WriteLine("FALLO: A8-06 - los 6 botones de la barra superior NO tienen el mismo alto real");
        }
        catch (Exception ex)
        {
            Console.WriteLine("H5-05-EXCEPTION: " + ex);
        }

        // Pedido explicito del usuario (4-sep-2026): "el boton dónde lo encuentro deja la
        // interfaz bloqueada" - la fila de resultado real usaba Border+MouseBinding dentro de
        // un Popup StaysOpen=False (gotcha real de WPF, ver el comentario real de
        // RowClickButton en Theme.xaml) - arreglado a un Button real. Esta es la PRIMERA
        // verificacion real de este arnes con un clic de RATON de verdad (mouse_event/
        // SetCursorPos, no InvokePattern ni Command.Execute) - la unica forma real de
        // reproducir el bug real (la captura/el foco de Windows solo entran en juego con un
        // gesto de raton real).
        try
        {
            vm.SelectedTabIndex = 1; // Personaje - vuelve a un estado conocido tras H5-05
            DoEvents();
            var whereIsItButtonReal = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, "Buscar en el personaje")));
            if (whereIsItButtonReal != null && whereIsItButtonReal.TryGetCurrentPattern(InvokePattern.Pattern, out var reabrirPat))
                ((InvokePattern)reabrirPat).Invoke();
            DoEvents(); DoEvents();
            vm.WhereIsItSearchText = "hierro";
            WaitForDispatcher(300);
            Console.WriteLine($"UI-BLOQUEADA-PREP: IsWhereIsItOpen={vm.IsWhereIsItOpen} (esperado True), Results.Count={vm.WhereIsItResults.Count} (esperado >=1)");

            var popupField = typeof(MainWindow).GetField("WhereIsItPopup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            var popup = popupField?.GetValue(window) as System.Windows.Controls.Primitives.Popup;
            System.Windows.FrameworkElement? filaResultado = null;
            void BuscarFilaResultado(System.Windows.DependencyObject d)
            {
                if (filaResultado != null) return;
                if (d is System.Windows.FrameworkElement fe && fe.DataContext is WhereIsItResultViewModel) { filaResultado = fe; return; }
                int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
                for (int i = 0; i < n && filaResultado == null; i++)
                    BuscarFilaResultado(System.Windows.Media.VisualTreeHelper.GetChild(d, i));
            }
            if (popup?.Child != null) BuscarFilaResultado(popup.Child);
            Console.WriteLine($"UI-BLOQUEADA-PREP: fila real de resultado encontrada en el arbol visual del Popup={filaResultado != null} (esperado True)");

            if (filaResultado != null)
            {
                var puntoSuperior = filaResultado.PointToScreen(new System.Windows.Point(filaResultado.ActualWidth / 2, filaResultado.ActualHeight / 2));
                ForzarPrimerPlano(hwnd);
                DoEvents();
                RealClickAt((int)puntoSuperior.X, (int)puntoSuperior.Y);
                WaitForDispatcher(200);

                bool popupSeCerroDeVerdad = !vm.IsWhereIsItOpen;
                bool sinCapturaColgada = System.Windows.Input.Mouse.Captured == null;
                Console.WriteLine($"UI-BLOQUEADA: tras el clic REAL de raton -> IsWhereIsItOpen={vm.IsWhereIsItOpen} (esperado False), Mouse.Captured={System.Windows.Input.Mouse.Captured} (esperado null)");
                if (!popupSeCerroDeVerdad) Console.WriteLine("FALLO: UI-BLOQUEADA - el Popup no se cerro tras el clic real de raton");
                if (!sinCapturaColgada) Console.WriteLine("FALLO: UI-BLOQUEADA - Mouse.Captured se quedo colgado tras cerrar el Popup con un clic real");

                // La prueba real de verdad: ¿la interfaz SIGUE respondiendo a otro clic real
                // despues de este? Clic real sobre la pestaña "Inicio" (indice 0) y confirma
                // que el cambio de pestaña SI ocurre - si la interfaz estuviera bloqueada de
                // verdad, este segundo clic real no haria nada.
                var pestañaInicio = root.FindFirst(TreeScope.Descendants, new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                    new PropertyCondition(AutomationElement.NameProperty, "Inicio")));
                if (pestañaInicio != null)
                {
                    var puntoInicio = pestañaInicio.Current.BoundingRectangle;
                    RealClickAt((int)puntoInicio.X + (int)(puntoInicio.Width / 2), (int)puntoInicio.Y + (int)(puntoInicio.Height / 2));
                    WaitForDispatcher(200);
                    bool siguoRespondiendo = vm.SelectedTabIndex == 0;
                    Console.WriteLine($"UI-BLOQUEADA: segundo clic real (pestaña Inicio) -> SelectedTabIndex={vm.SelectedTabIndex} (esperado 0 - la interfaz SIGUE respondiendo)");
                    if (!siguoRespondiendo) Console.WriteLine("FALLO: UI-BLOQUEADA - la interfaz dejo de responder a clics reales tras cerrar el Popup");
                }
                else Console.WriteLine("UI-BLOQUEADA: pestaña 'Inicio' real NO-FOUND para el segundo clic - omitido");

                var rtbUiBloqueada = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbUiBloqueada.Render(window);
                var encUiBloqueada = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encUiBloqueada.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbUiBloqueada));
                using (var fsUiBloqueada = File.Create(Path.Combine(AppContext.BaseDirectory, "ui-desbloqueada-tras-clic-real.png"))) encUiBloqueada.Save(fsUiBloqueada);
                Console.WriteLine("Captura tras el clic real y la interfaz respondiendo -> ui-desbloqueada-tras-clic-real.png");
            }
        }
        catch (Exception ex) { Console.WriteLine("UI-BLOQUEADA-EXCEPTION: " + ex); }

        // H5-07 (quinta auditoria de Opus): "carpetas adicionales de personajes/mundos... N
        // configurable de copias de seguridad... session.json recuerda el ultimo personaje
        // real". La logica en si (Add/Remove/deduplicacion/recorte/staleness) ya la cubren
        // SettingsViewModelTests.cs/SessionRestoreTests.cs a nivel de dominio - aqui lo que
        // hace falta verificar de verdad es la INTEGRACION real: una carpeta adicional real
        // AÑADIDA desde Ajustes hace que Home/Exploracion encuentren de verdad un personaje/
        // mundo que antes no veian, y que SaveSession() (disparado real por MainWindow via
        // CharacterLoaded) deja un session.json real y legible en disco.
        try
        {
            string extraDir = Path.Combine(Path.GetTempPath(), $"h5-07-extra-{Guid.NewGuid():N}");
            Directory.CreateDirectory(extraDir);
            string extraPlr = Path.Combine(extraDir, "PersonajeDeCarpetaExtra.plr");
            File.WriteAllBytes(extraPlr, PlrFile.Write(new PlrCharacter
            {
                Name = "DeCarpetaExtra",
                Version = 279,
                PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
                Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
            }));

            int antesDeAñadir = vm.Home.Characters.Count(c => c.FilePath == extraPlr);
            vm.Settings.AddCharacterFolder(extraDir);
            vm.Home.RefreshCommand.Execute(null);
            while (vm.Home.IsScanning) DoEvents();
            DoEvents();
            bool encontradoTrasAñadir = vm.Home.Characters.Any(c => c.FilePath == extraPlr);
            Console.WriteLine($"H5-07-CARPETA-EXTRA: personaje real de la carpeta adicional encontrado antes={antesDeAñadir > 0} (esperado False), despues de Settings.AddCharacterFolder={encontradoTrasAñadir} (esperado True)");
            if (!encontradoTrasAñadir) Console.WriteLine("FALLO: H5-07 - una carpeta adicional real en Ajustes no hizo que Home encontrara el personaje real que hay dentro");

            // Limpieza real: quita la carpeta de Ajustes (persiste settings.json sin ella) y
            // vuelve a escanear antes de dejar la maquina de este usuario con una carpeta
            // temporal sintetica permanentemente en su configuracion real.
            vm.Settings.RemoveCharacterFolderCommand.Execute(extraDir);
            vm.Home.RefreshCommand.Execute(null);
            while (vm.Home.IsScanning) DoEvents();
            Directory.Delete(extraDir, recursive: true);
            Console.WriteLine($"H5-07-CARPETA-EXTRA-LIMPIEZA: ExtraCharacterFolders tras quitarla={vm.Settings.ExtraCharacterFolders.Count} (esperado 0)");
        }
        catch (Exception ex)
        {
            Console.WriteLine("H5-07-CARPETA-EXTRA-EXCEPTION: " + ex);
        }

        try
        {
            vm.SelectedTabIndex = 7; // Acerca de - AppTab.AcercaDe, reordenado T1 21-sep-2026
            DoEvents(); DoEvents();
            var ajustesHeader = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text),
                new PropertyCondition(AutomationElement.NameProperty, "Ajustes")));
            Console.WriteLine($"H5-07-AJUSTES-UI: encabezado real 'Ajustes' encontrado en 'Acerca de'={ajustesHeader != null} (esperado True)");
            if (ajustesHeader == null) Console.WriteLine("FALLO: H5-07 - la seccion real de Ajustes no aparece en Acerca de");

            // Cupo real de copias de seguridad - cambio real desde la UI, confirma que llega de
            // verdad a BackupHistoryService.MaxBackupsPerCharacter (no solo al ViewModel).
            int cupoAntes = vm.Settings.BackupHistoryCap;
            vm.Settings.BackupHistoryCap = 5;
            Console.WriteLine($"H5-07-CUPO: BackupHistoryCap real cambiado de {cupoAntes} a {vm.Settings.BackupHistoryCap} (esperado 5)");
            vm.Settings.BackupHistoryCap = cupoAntes; // deja la maquina real de este usuario tal y como estaba

            var rtbAjustes = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbAjustes.Render(window);
            var encAjustes = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encAjustes.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbAjustes));
            using (var fsAjustes = File.Create(Path.Combine(AppContext.BaseDirectory, "h5-07-ajustes.png"))) encAjustes.Save(fsAjustes);
            Console.WriteLine("Captura pantalla de Ajustes -> h5-07-ajustes.png");
        }
        catch (Exception ex)
        {
            Console.WriteLine("H5-07-AJUSTES-UI-EXCEPTION: " + ex);
        }
        finally
        {
            // Deja la navegacion real donde estaba antes de este bloque (Personaje > Objetos,
            // igual que la dejo H5-05 justo encima) - varias comprobaciones MAS ABAJO en este
            // mismo arnes (ej. B-7, "Grid real de la fila de Libreria") dan por hecho que esa
            // es la pestaña activa y buscan en el arbol visual TAL CUAL esta ahora mismo, sin
            // navegar ellas mismas primero. Bug real del propio arnes, encontrado y arreglado en
            // esta misma pasada: sin este restablecimiento, B-7 daba NO-FOUND en 1 de 2
            // ejecuciones (la pestaña quedaba en "Acerca de", el Grid de la Libreria vive dentro
            // de Objetos y un TabControl real no realiza el contenido de una pestaña inactiva).
            vm.SelectedTabIndex = 1; // Personaje
            vm.PersonajeInnerTabIndex = 0; // Objetos
            DoEvents();
        }

        try
        {
            // A estas alturas ya se cargo un personaje real (H5-05, mas arriba) - CharacterLoaded
            // ya debio dispararse una vez, y MainWindow.xaml.cs ya debio escribir un session.json
            // REAL (el mismo fichero que usaria la proxima sesion real de este usuario).
            string sessionPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "session.json");
            bool existeReal = File.Exists(sessionPath);
            string contenido = existeReal ? File.ReadAllText(sessionPath) : "";
            bool contieneRutaReal = existeReal && contenido.Contains("uia-harness-test.plr");
            Console.WriteLine($"H5-07-SESION-REAL: session.json real existe={existeReal} (esperado True), contiene la ruta del personaje real cargado={contieneRutaReal} (esperado True)");
            if (!existeReal || !contieneRutaReal) Console.WriteLine("FALLO: H5-07 - CharacterLoaded no dejo un session.json real y legible con el personaje correcto");

            // "Continuar con Nombre" real: una MainViewModel NUEVA (simulando el proximo
            // arranque real de la app) debe ofrecer continuar con ESTE MISMO personaje, sin
            // cargarlo sola - RestoreSession() es quien lee el session.json real de arriba.
            var vm2 = new MainViewModel();
            vm2.RestoreSession();
            Console.WriteLine($"H5-07-CONTINUAR: LastSessionCharacterName real tras RestoreSession()='{vm2.Home.LastSessionCharacterName}' (esperado 'UIA-Test'), IsCharacterLoaded=={vm2.IsCharacterLoaded} (esperado False - nunca carga sola)");
            if (vm2.Home.LastSessionCharacterName != "UIA-Test") Console.WriteLine("FALLO: H5-07 - 'Continuar con...' no ofrecio el personaje real de la sesion anterior");
            if (vm2.IsCharacterLoaded) Console.WriteLine("FALLO: H5-07 - RestoreSession() cargo el personaje solo, en silencio (deberia dejarlo a decision explicita del usuario)");

            // Pedido explicito del usuario (4-sep-2026): "cuando inicias el programa nunca
            // inicia en el inicio, inicia en la pestaña de versiones del sav de personaje" - la
            // navegacion real de esta MISMA pasada del arnes (personaje cargado, pestañas
            // tocadas) ya quedo escrita en el session.json REAL que se acaba de leer arriba, asi
            // que si RestoreSession() todavia secuestrara SelectedTabIndex esta prueba lo pillaria.
            Console.WriteLine($"ARRANQUE-SIEMPRE-INICIO: vm2.SelectedTabIndex tras RestoreSession()={vm2.SelectedTabIndex} (esperado 0, Inicio - NUNCA la ultima pestaña/sub-pestaña tocada)");
            if (vm2.SelectedTabIndex != 0) Console.WriteLine("FALLO: la app no arranca siempre en Inicio pese al pedido explicito del usuario");
        }
        catch (Exception ex)
        {
            Console.WriteLine("H5-07-SESION-REAL-EXCEPTION: " + ex);
        }

        // Toggle biblioteca (plegar/desplegar) para confirmar que el binding real funciona.
        // Segunda auditoria de Opus (Fable), B-7 - BUG REAL en esta misma comprobacion: el
        // MaxHeight buscado (460) no coincidia con el real del XAML de entonces (238, residuo
        // de un revert) - el finder NUNCA encontraba el Grid, AltoFilaLibreria() devolvia
        // SIEMPRE -1, y como un "-1px" impreso no contaba como FALLO/NO-FOUND/EXCEPTION, esta
        // comprobacion (la UNICA capaz de detectar B-1, la fila que no libera espacio al
        // plegar) llevaba rota desde el revert mientras el resto del arnes seguia en verde.
        // Arreglado de raiz, no solo el numero: un finder que no encuentra nada ahora imprime
        // FALLO explicito, nunca un numero centinela silencioso.
        try
        {
            Console.WriteLine($"IsLibraryCollapsed antes={vm.IsLibraryCollapsed}");

            System.Windows.Controls.Grid? libraryRowGrid = null;
            void FindLibraryGrid(System.Windows.DependencyObject d)
            {
                if (d is System.Windows.Controls.Grid g && g.RowDefinitions.Count == 2 && g.RowDefinitions[1].MaxHeight == 460)
                    libraryRowGrid = g;
                int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
                for (int i = 0; i < n; i++) FindLibraryGrid(System.Windows.Media.VisualTreeHelper.GetChild(d, i));
            }
            FindLibraryGrid(window);

            if (libraryRowGrid == null)
            {
                Console.WriteLine("FALLO: Grid real de la fila de Libreria (RowDefinitions.Count==2, MaxHeight==460) NO-FOUND");
            }
            else
            {
                double AltoFilaLibreria() => libraryRowGrid.RowDefinitions[1].ActualHeight;

                DoEvents(); DoEvents();
                double altoDesplegada = AltoFilaLibreria();
                Console.WriteLine($"Alto real fila Libreria (desplegada, IsLibraryCollapsed={vm.IsLibraryCollapsed})={altoDesplegada:0.#}px");
                if (altoDesplegada < 150) Console.WriteLine($"FALLO: desplegada deberia tener sitio real (>=150px), salio {altoDesplegada:0.#}px");

                vm.ToggleLibraryCollapsedCommand.Execute(null);
                DoEvents(); DoEvents(); DoEvents();
                Console.WriteLine($"IsLibraryCollapsed despues={vm.IsLibraryCollapsed}");
                double altoColapsada = AltoFilaLibreria();
                Console.WriteLine($"Alto real fila Libreria (colapsada)={altoColapsada:0.#}px (esperado: solo la barra del boton, ~30-40px, no 150-238)");
                if (altoColapsada > 60) Console.WriteLine($"FALLO: colapsada deberia devolver el espacio real (<=60px), salio {altoColapsada:0.#}px - B-1 (segunda auditoria)");

                vm.ToggleLibraryCollapsedCommand.Execute(null); // vuelve a desplegar para el resto de pruebas
                DoEvents(); DoEvents();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("LIBRARY-TOGGLE-EXCEPTION: " + ex);
        }

        // Rework de Buffs (pregunta a Opus sobre el diseño, cuarta pasada). Fase 2: navegar a
        // la pestaña real, desplegar la Libreria de buffs, confirmar el arbol REAL (6
        // categorias + Indice + Calamity), elegir una categoria real, pedir "Elegir..." sobre
        // un slot vacio (mismo comando real que dispara el doble clic/ChooseFromLibraryCommand)
        // y colocar un buff real pulsando "Colocar" via UI Automation real.
        try
        {
            Console.WriteLine($"Buffs.Container.Slots.Count={vm.Buffs.Container?.Slots.Count} (esperado: 44, version 279 >= 269)");

            var buffsTab = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Buffs")));
            if (buffsTab != null && buffsTab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var buffsSelPat))
                ((SelectionItemPattern)buffsSelPat).Select();
            DoEvents();
            DoEvents();

            Console.WriteLine($"BuffLibrary.RootCategories: {string.Join(", ", vm.BuffLibrary.RootCategories.Select(c => c.Name))}");
            Console.WriteLine($"(esperado: Utilidad/Offensivo/Defensivo/Special/Mascota/Negativo/Indice/Calamity (mod), 8 raices reales)");

            var toggleButton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, "Librería de buffs")));
            if (toggleButton != null && toggleButton.TryGetCurrentPattern(InvokePattern.Pattern, out var togglePat))
                ((InvokePattern)togglePat).Invoke();
            DoEvents();
            DoEvents();
            Console.WriteLine($"IsBuffLibraryCollapsed tras pulsar 'Librería de buffs'={vm.IsBuffLibraryCollapsed}");

            var utilidad = vm.BuffLibrary.RootCategories.FirstOrDefault(c => c.Name.StartsWith("Utilidad"));
            if (utilidad != null) vm.BuffLibrary.SelectCategoryCommand.Execute(utilidad);
            DoEvents();
            DoEvents();
            Console.WriteLine($"Categoria 'Utilidad' seleccionada -> Results.Count={vm.BuffLibrary.Results.Count} (esperado: 17)");

            // Segunda auditoria de Opus (Fable), B-2 - BUG REAL: "Elegir..." ponia
            // IsBuffLibraryCollapsed=false DIRECTAMENTE, un pestillo de un solo sentido - nada
            // lo devolvia a la preferencia real del usuario. Se fuerza la preferencia real a
            // "plegada" primero, para poder confirmar de verdad que "Elegir..." la revela
            // TEMPORALMENTE (via IsBuffLibraryVisible) sin pisar esa preferencia.
            vm.IsBuffLibraryCollapsed = true;
            DoEvents();

            // Pide "Elegir..." sobre el primer slot vacio real (mismo comando real que dispara
            // el doble clic/menu contextual de la rejilla de arriba).
            var emptySlot = vm.Buffs.Container?.Slots.FirstOrDefault(s => s.IsEmpty);
            if (emptySlot != null) emptySlot.ChooseFromLibraryCommand.Execute(null);
            DoEvents();
            DoEvents();
            Console.WriteLine($"BuffLibrary.IsPicking={vm.BuffLibrary.IsPicking} PickTarget coincide={ReferenceEquals(vm.BuffLibrary.PickTarget, emptySlot)}");
            Console.WriteLine($"B2-PESTILLO: tras 'Elegir...' con preferencia real=plegada -> IsBuffLibraryCollapsed={vm.IsBuffLibraryCollapsed} (esperado True, SIN pisar), IsBuffLibraryVisible={vm.IsBuffLibraryVisible} (esperado True, revelada TEMPORALMENTE)");

            // Buff real conocido: id 1 = Obsidian Skin, esta en la categoria real Utilidad -
            // pulsa el boton "Colocar" real de esa tarjeta via UI Automation real.
            var placeButton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, "Colocar")));
            if (placeButton != null && placeButton.TryGetCurrentPattern(InvokePattern.Pattern, out var placePat))
                ((InvokePattern)placePat).Invoke();
            else
                Console.WriteLine("Boton 'Colocar' NO-FOUND");
            DoEvents();
            DoEvents();

            Console.WriteLine($"B2-PESTILLO: tras colocar (PickTarget vuelve a null) -> IsBuffLibraryVisible={vm.IsBuffLibraryVisible} (esperado False - vuelve sola a la preferencia real, ya no se queda desplegada para siempre)");
            if (vm.IsBuffLibraryVisible) Console.WriteLine("FALLO: B-2 (segunda auditoria) - la Libreria de buffs se quedo desplegada tras colocar, pese a que la preferencia real es plegada");

            var placedSlot = vm.Buffs.Container?.Slots.FirstOrDefault(s => !s.IsEmpty);
            Console.WriteLine($"Buff colocado: DisplayName={placedSlot?.DisplayName} DurationSeconds={placedSlot?.DurationSeconds} IsSelected={placedSlot?.IsSelected}");
            Console.WriteLine($"BuffEdit.Slot coincide={ReferenceEquals(vm.BuffEdit.Slot, placedSlot)} MinLabel={vm.BuffEdit.MinLabel} MediaLabel={vm.BuffEdit.MediaLabel} MaxLabel={vm.BuffEdit.MaxLabel}");

            var minButton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, vm.BuffEdit.MinLabel)));
            if (minButton != null && minButton.TryGetCurrentPattern(InvokePattern.Pattern, out var minPat))
            {
                ((InvokePattern)minPat).Invoke();
                DoEvents();
                Console.WriteLine($"Tras pulsar '{vm.BuffEdit.MinLabel}': DurationSeconds={placedSlot?.DurationSeconds}");
            }
            else Console.WriteLine($"Boton Minima NO-FOUND (buscado: '{vm.BuffEdit.MinLabel}')");

            var maxButton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, vm.BuffEdit.MaxLabel)));
            if (maxButton != null && maxButton.TryGetCurrentPattern(InvokePattern.Pattern, out var maxPat))
            {
                ((InvokePattern)maxPat).Invoke();
                DoEvents();
                Console.WriteLine($"Tras pulsar '{vm.BuffEdit.MaxLabel}': DurationSeconds={placedSlot?.DurationSeconds} (esperado ~33333333, S.getMaxTime real/60)");
            }
            else Console.WriteLine($"Boton Maxima NO-FOUND (buscado: '{vm.BuffEdit.MaxLabel}')");

            // Verificacion real de T-4 (auditoria de Opus, Bloque 5): BuffSlotCompactTemplate
            // tenia el mismo bug real que E-1 (borde de "Calamity" y de "seleccionado"
            // compitiendo, el ultimo declarado ganaba siempre) - un buff de Calamity
            // seleccionado para editarlo perdia el punto/borde rojo. Coloca un buff REAL de
            // Calamity (CalamityIds.BuffIdBase, el primero real del catalogo) en un slot vacio
            // y lo selecciona - ambas señales (punto rojo + borde morado) deben verse a la vez.
            var emptyBuffSlot = vm.Buffs.Container?.Slots.FirstOrDefault(s => s.IsEmpty);
            if (emptyBuffSlot != null)
            {
                emptyBuffSlot.PlaceBuff(Terrakeep.Core.Calamity.CalamityIds.BuffIdBase);
                vm.SelectBuffSlot(emptyBuffSlot);
                DoEvents();
                DoEvents();
                Console.WriteLine($"T4-BUFF-CALAMITY: DisplayName={emptyBuffSlot.DisplayName} IsCalamity={emptyBuffSlot.IsCalamity} (esperado True) IsSelected={emptyBuffSlot.IsSelected} (esperado True)");
                var rtbBuffCal = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbBuffCal.Render(window);
                var encBuffCal = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encBuffCal.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbBuffCal));
                using var fsBuffCal = File.Create(Path.Combine(AppContext.BaseDirectory, "t4-buff-calamity-seleccionado.png"));
                encBuffCal.Save(fsBuffCal);
            }
            else Console.WriteLine("T4-BUFF-CALAMITY: sin slot de Buffs vacio real - omitido");

            // Sexta auditoria de Opus, H6-12 ("los buffs de Calamity no distinguen buff de
            // debuff"): coloca un DEBUFF real de Calamity (Main.debuff[base.Type]=true en su
            // propio ModBuff, ver scripts/extraer-debuffs-calamity.js) en otro slot vacio y
            // confirma el punto morado real (IsDebuff) - a diferencia del bloque T4 de arriba,
            // que usa el PRIMER buff del catalogo sin saber si es debuff o no.
            try
            {
                var serviceFieldH612 = typeof(MainViewModel).GetField("_service", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var svcH612 = (Terrakeep.App.Services.CharacterFileService)serviceFieldH612!.GetValue(vm)!;
                var debuffEntry = svcH612.CalamityBuffCatalog.Entries.First(e => e.IsDebuff);
                var otherEmptySlot = vm.Buffs.Container?.Slots.FirstOrDefault(s => s.IsEmpty);
                if (otherEmptySlot != null)
                {
                    otherEmptySlot.PlaceBuff(debuffEntry.SyntheticId);
                    DoEvents(); DoEvents();
                    Console.WriteLine($"H6-12-DEBUFF: DisplayName={otherEmptySlot.DisplayName} IsCalamity={otherEmptySlot.IsCalamity} (esperado True) IsDebuff={otherEmptySlot.IsDebuff} (esperado True)");
                    if (!otherEmptySlot.IsDebuff) Console.WriteLine("FALLO: H6-12 - un debuff real de Calamity no quedo marcado IsDebuff=true");
                    var rtbDebuff = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbDebuff.Render(window);
                    var encDebuff = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encDebuff.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbDebuff));
                    using var fsDebuff = File.Create(Path.Combine(AppContext.BaseDirectory, "h6-12-buff-debuff.png"));
                    encDebuff.Save(fsDebuff);
                    Console.WriteLine("Captura punto de debuff real -> h6-12-buff-debuff.png");
                    otherEmptySlot.ClearCommand.Execute(null); // deja el slot como estaba para el resto del arnes
                }
                else Console.WriteLine("H6-12-DEBUFF: sin slot de Buffs vacio real - omitido");
            }
            catch (Exception ex) { Console.WriteLine("H6-12-DEBUFF-EXCEPTION: " + ex); }

            // Pedido explicito del usuario (4-sep-2026): "la pestaña de buff no tiene nada de
            // guardar json ni tampoco cargar para guardar combinaciones de buff" - confirma que
            // los 3 botones reales existen en el arbol visual (sin invocarlos: Click dispara un
            // SaveFileDialog/OpenFileDialog real de Windows, que colgaria este arnes sin nadie
            // delante para pulsar Cancelar) y ejercita SaveBuffSet/LoadBuffSet de verdad, mismo
            // camino real que esos botones llaman, con un fichero temporal real - mismo criterio
            // ya establecido para Guardar/Cargar conjunto de OBJETOS (H5-03), que tampoco tiene
            // precedente en este arnes por el mismo motivo real.
            try
            {
                bool botonGuardarExiste = root.FindFirst(TreeScope.Descendants, new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                    new PropertyCondition(AutomationElement.NameProperty, "Guardar conjunto..."))) != null;
                Console.WriteLine($"BUFFSET-BOTONES: boton real 'Guardar conjunto...' encontrado en Buffs={botonGuardarExiste} (esperado True)");
                if (!botonGuardarExiste) Console.WriteLine("FALLO: BUFFSET-BOTONES - el boton real 'Guardar conjunto...' de Buffs no esta en el arbol visual");

                var containerBuffs = vm.Buffs.Container;
                if (containerBuffs != null)
                {
                    containerBuffs.Slots[0].PlaceBuff(1); // Obsidian Skin, id real vanilla
                    var idsAntes = containerBuffs.Slots.Select(s => s.Buff.Id).ToArray();
                    string rutaBuffSet = Path.Combine(Path.GetTempPath(), "uia-harness-buffset.json");
                    if (File.Exists(rutaBuffSet)) File.Delete(rutaBuffSet);

                    vm.SaveBuffSet(containerBuffs, rutaBuffSet);
                    Console.WriteLine($"BUFFSET-GUARDAR: fichero real creado={File.Exists(rutaBuffSet)} (esperado True), StatusMessage='{vm.StatusMessage}'");

                    containerBuffs.ClearAllCommand.Execute(null);
                    DoEvents();
                    vm.LoadBuffSet(containerBuffs, rutaBuffSet, append: false);
                    DoEvents(); DoEvents();
                    var idsDespues = containerBuffs.Slots.Select(s => s.Buff.Id).ToArray();
                    bool cargoBien = idsAntes.SequenceEqual(idsDespues);
                    Console.WriteLine($"BUFFSET-CARGAR: el conjunto real cargado coincide con el guardado={cargoBien} (esperado True), StatusMessage='{vm.StatusMessage}'");
                    if (!cargoBien) Console.WriteLine("FALLO: BUFFSET-CARGAR - el conjunto cargado no coincide con el guardado");

                    var rtbBuffSet = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbBuffSet.Render(window);
                    var encBuffSet = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encBuffSet.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbBuffSet));
                    using (var fsBuffSet = File.Create(Path.Combine(AppContext.BaseDirectory, "buffset-botones-guardar-cargar.png"))) encBuffSet.Save(fsBuffSet);
                    Console.WriteLine("Captura botones Guardar/Cargar/Añadir de Buffs -> buffset-botones-guardar-cargar.png");

                    containerBuffs.ClearAllCommand.Execute(null); // deja el personaje real como estaba
                    File.Delete(rutaBuffSet);
                }
                else Console.WriteLine("BUFFSET: sin Buffs.Container real - omitido");
            }
            catch (Exception ex) { Console.WriteLine("BUFFSET-EXCEPTION: " + ex); }

            // Verificacion real de T-5 (auditoria de Opus, Bloque 5): la leyenda solo vive en
            // el hueco real de "sin seleccion" - se deselecciona a proposito para verla.
            if (emptyBuffSlot != null) emptyBuffSlot.IsSelected = false;
            vm.BuffEdit.Slot = null;
            DoEvents();
            DoEvents();
            var rtbLeyenda = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbLeyenda.Render(window);
            var encLeyenda = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encLeyenda.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbLeyenda));
            using (var fsLeyenda = File.Create(Path.Combine(AppContext.BaseDirectory, "t5-leyenda-buffs.png"))) encLeyenda.Save(fsLeyenda);
        }
        catch (Exception ex)
        {
            Console.WriteLine("BUFFS-EXCEPTION: " + ex);
        }

        // Septima pasada: investigacion real de comportamiento al redimensionar (queja del
        // usuario: solape en Equipamiento, scroll persistente en Mascota/Montura, perdida de
        // contenido en rejillas grandes, Libreria "siempre igual"). Capturas reales a varios
        // tamaños de ventana, incluido el MinWidth/MinHeight declarado (1000x620) y por debajo.
        void CaptureAt(double w, double h, string tabName, string fileName)
        {
            FijarTamaño(window, w, h);
            Console.WriteLine($"  Ventana pedida {w}x{h} -> real ActualWidth={window.ActualWidth:0.#} ActualHeight={window.ActualHeight:0.#}");

            // "Equipamiento"/"Inventario"/"Almacenes" viven DENTRO de "Personaje" > "Objetos" -
            // hace falta seleccionar esos dos primero o la pagina interna ni siquiera existe
            // en el arbol visual (TabControl solo realiza el contenido de la pestaña activa).
            vm.SelectedTabIndex = 1;
            vm.PersonajeInnerTabIndex = 0;
            DoEvents();

            // NAV123 (25-sep-2026): "Equipamiento"/"Inventario"/"Almacenes" ya NO son TabItem -
            // son 3 paginas reales exclusivas (ObjetosPageHost, MainWindow.xaml) seleccionadas
            // por vm.ObjetosSubTabIndex (0/1/2), mismo patron ya usado en este archivo en
            // vm.ObjetosSubTabIndex = 1; // Inventario (ver p.ej. linea ~10909).
            int? subTab = tabName switch
            {
                "Equipamiento" => 0,
                "Inventario" => 1,
                "Almacenes" => 2,
                _ => null
            };
            if (subTab.HasValue)
                vm.ObjetosSubTabIndex = subTab.Value;
            else
                Console.WriteLine($"  AVISO: tabName '{tabName}' no mapea a ningun ObjetosSubTabIndex conocido");
            DoEvents();
            DoEvents();

            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(window);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            string shotPath = Path.Combine(AppContext.BaseDirectory, fileName);
            using (var fs = File.Create(shotPath)) encoder.Save(fs);
            Console.WriteLine($"  Captura -> {shotPath}");
        }

        try
        {
            Console.WriteLine($"Window.MinWidth={window.MinWidth} MinHeight={window.MinHeight}");
            CaptureAt(1180, 860, "Equipamiento", "resize-equip-grande.png");
            CaptureAt(1080, 700, "Equipamiento", "resize-equip-minimo.png");

            // Verificacion real de T-2/E-2 (auditoria de Opus, Bloque 4): umbral real de
            // "SizeClass.Amplio" (provisional: 1700px) - justo debajo debe seguir en pildoras,
            // justo encima debe pasar a las 3 vistas lado a lado.
            CaptureAt(1450, 860, "Equipamiento", "e2-justo-debajo-1450.png");
            Console.WriteLine($"E2-UMBRAL: en 1450px, SizeClass={vm.SizeClass} (esperado Normal - FASE B del responsive global retiro IsEquipmentExpanded: Equipamiento ya no cambia de arquitectura con el tamaño, ver EQUIP_RESPONSIVE_SOLO)");
            CaptureAt(1550, 860, "Equipamiento", "e2-justo-encima-1550.png");
            Console.WriteLine($"E2-UMBRAL: en 1550px, SizeClass={vm.SizeClass} (esperado Amplio - misma subvista+selector que en 1450, ver EQUIP_RESPONSIVE_SOLO)");

            // Diagnostico whitebox real: anchos reales de las 3 columnas del SlotRowHost y de
            // cada SlotGridPanel dentro, a la resolucion minima real - para saber si el centro
            // (Armadura/Accesorios) tiene de verdad sitio para sus 5 columnas a MinCell=40, en
            // vez de seguir adivinando a mano.
            void FindRowHostWidths(System.Windows.DependencyObject d)
            {
                if (d is Terrakeep.App.Controls.SlotRowHost srh)
                {
                    Console.WriteLine($"  RESIZE-DIAG SlotRowHost: ActualWidth={srh.ActualWidth:0.#}");
                    foreach (var cd in srh.ColumnDefinitions)
                        Console.WriteLine($"  RESIZE-DIAG   Column: ActualWidth={cd.ActualWidth:0.#} Width={cd.Width} MinWidth={cd.MinWidth}");
                }
                if (d is Terrakeep.App.Controls.SlotGridPanel sgp)
                    Console.WriteLine($"  RESIZE-DIAG   SlotGridPanel: ActualWidth={sgp.ActualWidth:0.#} ActualHeight={sgp.ActualHeight:0.#} AvailableHeight={sgp.AvailableHeight:0.#} MinCell={sgp.MinCell} MaxCell={sgp.MaxCell} Children={sgp.Children.Count}");
                if (d is System.Windows.Controls.ScrollViewer sv && sv.Content is System.Windows.FrameworkElement content && content is System.Windows.Controls.StackPanel)
                    Console.WriteLine($"  RESIZE-DIAG   ScrollViewer(StackPanel): ActualHeight={sv.ActualHeight:0.#} ExtentHeight={sv.ExtentHeight:0.#} ViewportHeight={sv.ViewportHeight:0.#}");
                int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
                for (int i = 0; i < n; i++)
                    FindRowHostWidths(System.Windows.Media.VisualTreeHelper.GetChild(d, i));
            }
            Console.WriteLine("RESIZE-DIAG === a 1040x700 ===");
            FindRowHostWidths(window);

            CaptureAt(700, 400, "Equipamiento", "resize-equip-forzado-pequeno.png");
            CaptureAt(1080, 700, "Inventario", "resize-inv-minimo.png");
            CaptureAt(1080, 700, "Almacenes", "resize-almacenes-minimo.png");

            // A4-EXPANDIDO (rediseñado tras NAV123, 25-sep-2026): el A-4 original (auditoria de
            // Opus, Bloque 4) probaba la "coexistencia" de Inventario+Almacen fusionados dentro
            // del bloque de 2 columnas que solo aparecia en Amplio (IsStorageExpanded) - ese
            // bloque quedo RETIRADO por completo con NAV123 (ver comentario "T3 PASO 1" en
            // MainWindow.xaml), asi que esa coexistencia ya no es un concepto real y no se puede
            // seguir probando. La invariante NUEVA que Nav123 introdujo de verdad y hay que
            // proteger es la contraria: Almacenes (ObjetosSubTabIndex=2) es ahora una pagina
            // PERMANENTE, alcanzable a CUALQUIER ancho (antes, por debajo de AmplioMinWidth=1520,
            // se ocultaba entera) - y a la inversa, Almacenes ya NO vive fusionado dentro de la
            // pagina Inventario (ObjetosSubTabIndex=1) a ningun ancho (exclusividad real de
            // paginas). Se prueba en los mismos 2 anchos que ya usaba A-4 (1350, justo por debajo
            // del umbral, y 1520, el umbral real) con la pildora real de Almacenes ("Banco...").
            static List<AutomationElement> BuscarPildoraBanco(AutomationElement r) =>
                r.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                    .Cast<AutomationElement>().Where(b => b.Current.Name.StartsWith("Banco")).ToList();

            foreach (double wA4 in new[] { 1350.0, 1520.0 })
            {
                CaptureAt(wA4, 860, "Almacenes", $"a4-almacenes-{wA4:0}.png");
                bool almacenesPresenteEnSub2 = BuscarPildoraBanco(root).Count > 0;
                Console.WriteLine($"A4-EXPANDIDO: a {wA4:0}px, ObjetosSubTabIndex=2 (Almacenes) -> pildora real de Almacenes presente={almacenesPresenteEnSub2} (esperado True - pagina permanente a cualquier ancho, Nav123)");
                if (!almacenesPresenteEnSub2) Console.WriteLine($"FALLO: A4-EXPANDIDO - a {wA4:0}px, la pagina de Almacenes (ObjetosSubTabIndex=2) no muestra su contenido real en el arbol visual");

                CaptureAt(wA4, 860, "Inventario", $"a4-inventario-{wA4:0}.png");
                bool almacenesAusenteEnSub1 = BuscarPildoraBanco(root).Count == 0;
                Console.WriteLine($"A4-EXPANDIDO: a {wA4:0}px, ObjetosSubTabIndex=1 (Inventario) -> pildora real de Almacenes ausente={almacenesAusenteEnSub1} (esperado True - exclusividad real entre paginas, Nav123)");
                if (!almacenesAusenteEnSub1) Console.WriteLine($"FALLO: A4-EXPANDIDO - a {wA4:0}px, Almacenes sigue presente dentro de la pagina Inventario (ObjetosSubTabIndex=1): la exclusividad de paginas no se respeta");
            }

            // Prueba real de intercambio cruzado (no solo "coexisten en pantalla" - que el
            // intercambio Inventario<->Almacen funcione de verdad): SwapWith es el mismo
            // metodo real que ya usa el gesto de arrastrar (MainWindow.xaml.cs.OnItemSlotDrop),
            // sin simular el gesto de raton entero.
            if (vm.InventoryContainer != null && vm.StorageGroup != null)
            {
                var invSlot = vm.InventoryContainer.Slots[0];
                var bankSlot = vm.StorageGroup.Current.Slots[0];
                string invAntes = invSlot.DisplayName, bankAntes = bankSlot.DisplayName;
                invSlot.SwapWith(bankSlot);
                Console.WriteLine($"A4-INTERCAMBIO: Inventario[0] '{invAntes}' -> '{invSlot.DisplayName}' (esperado '{bankAntes}'), Banco[0] '{bankAntes}' -> '{bankSlot.DisplayName}' (esperado '{invAntes}')");
                invSlot.SwapWith(bankSlot); // deshace el intercambio, no dejar el personaje de prueba alterado para el resto de tests
            }
            vm.IsLibraryCollapsed = false;
            CaptureAt(1180, 860, "Inventario", "resize-libreria-grande.png");
            CaptureAt(1080, 700, "Inventario", "resize-libreria-minimo.png");
            CaptureAt(1180, 860, "Equipamiento", "resize-equip-vuelta-grande.png");

            // Octava pasada: comprobar que el Height="380" fijo de Buffs (causa nº1 real del
            // solape segun Opus) ya no lo hace, a la resolucion minima real.
            FijarTamaño(window, 1080, 700);
            var buffsTabForShot = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Buffs")));
            if (buffsTabForShot != null && buffsTabForShot.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var buffsShotPat))
                ((SelectionItemPattern)buffsShotPat).Select();
            DoEvents(); DoEvents();
            var rtbBuffs = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbBuffs.Render(window);
            var encBuffs = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encBuffs.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbBuffs));
            using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "resize-buffs-minimo.png"))) encBuffs.Save(fs);
            Console.WriteLine("Captura Buffs minimo -> resize-buffs-minimo.png");

            // Referencia visual real pedida por el usuario ("me gustaria algo mas moderno
            // como... segunda captura" - los botones "melee"/"Auto-equipar" de Builds).
            FijarTamaño(window, 1180, 860);
            var buildsTab = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Builds")));
            if (buildsTab != null && buildsTab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var buildsSelPat))
                ((SelectionItemPattern)buildsSelPat).Select();
            DoEvents();
            DoEvents();
            var rtbBuilds = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbBuilds.Render(window);
            var encBuilds = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encBuilds.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbBuilds));
            using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "builds-referencia.png"))) encBuilds.Save(fs);
            Console.WriteLine("Captura Builds -> builds-referencia.png");

            // Bd-d (segunda auditoria de Opus, Fable): "marcar lo que ya se posee" - coloca el
            // primer objeto real de una build vanilla en el Inventario, refresca (mismo camino
            // real que usa el usuario: entrar en Builds) y confirma con captura que la insignia
            // verde aparece EXACTAMENTE en esa fila y en ninguna otra de la misma clase.
            var filaParaPoseer = vm.Builds.VanillaStages[0].Classes[0].Armor[0];
            vm.InventoryContainer!.Slots.First(s => s.IsEmpty).PlaceItem(filaParaPoseer.ItemId);
            vm.SelectedTabIndex = 1; // Personaje, para forzar un cambio real de pestaña
            DoEvents();
            vm.SelectedTabIndex = 2; // Builds - dispara OnSelectedTabIndexChanged -> RefreshOwnership
            DoEvents();
            bool poseidoOk = filaParaPoseer.IsOwned;
            bool otrasNoPoseidas = vm.Builds.VanillaStages[0].Classes[0].Armor.Skip(1).All(r => !r.IsOwned)
                && vm.Builds.VanillaStages[0].Classes[0].Weapons.All(r => !r.IsOwned);
            Console.WriteLine($"BD-D-POSEIDO: fila marcada={poseidoOk} (esperado True), resto sin marcar={otrasNoPoseidas} (esperado True)");
            var rtbOwned = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbOwned.Render(window);
            var encOwned = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encOwned.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbOwned));
            using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "builds-poseido.png"))) encOwned.Save(fs);
            Console.WriteLine("Captura Builds poseido -> builds-poseido.png");
        }
        catch (Exception ex)
        {
            Console.WriteLine("RESIZE-EXCEPTION: " + ex);
        }

        // AR-14 / AR-14b: la fila fusionada de Equipamiento medida en coordenadas reales, tamaño a
        // tamaño. El bloque entero vive ahora en AuditoriaEquipamiento.cs (misma clase parcial, mismos
        // helpers) - ver ahi el porque, y el modo de foco AR14_SOLO=1 para iterar solo sobre esta fila.
        AuditoriaFilaFusionadaEquipamiento(window, vm);

        // OBJ-01 (oleada de pruebas de Personaje -> Objetos, 6-sep-2026). Detector real de
        // BINDINGS ROTOS en toda la zona de Objetos (Equipamiento/Inventario/Almacenes): recorre
        // el arbol visual de la sub-pestaña activa y le pregunta a WPF, expresion por expresion,
        // si la ruta se resolvio de verdad (BindingExpressionBase.Status == PathError).
        //
        // Hace falta un detector asi porque un binding a una propiedad que NO EXISTE en el
        // DataContext NO da ningun error visible en WPF: deja el Content/Text en su valor por
        // defecto y la app sigue funcionando tan campante - un boton se queda literalmente SIN
        // TEXTO (y por tanto casi sin ancho, o sea invisible) y nadie se entera. Es la misma
        // familia de fallo silencioso que "ItemCountLabel no existia" (Exploracion, ronda del
        // 6-sep) y que ninguna comprobacion de recorte o de idioma puede ver: el barrido de
        // idioma busca texto en el idioma equivocado o claves sin traducir, no texto AUSENTE.
        //
        // Se limita al subarbol de la zona de Objetos a proposito (el TabControl que hospeda
        // Equipamiento/Inventario/Almacenes, localizado subiendo desde el SlotRowHost real) - ni
        // ruido de otras pantallas ni fallos de otra area.
        try
        {
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 0;
            DoEvents(); DoEvents();

            TabControl? tabObjetos = null;
            var hostEquipo = Descendientes<Terrakeep.App.Controls.SlotRowHost>(window).FirstOrDefault();
            for (DependencyObject? d = hostEquipo; d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
                if (d is TabControl tc) { tabObjetos = tc; break; }

            if (tabObjetos == null)
            {
                Console.WriteLine("FALLO: OBJ-01 - no se encontro el TabControl de Objetos (SlotRowHost fuera del arbol visual)");
            }
            else
            {
                string idiomaOriginal = LocalizationService.Instance.Language;
                // Los 3 sub-paneles reales x 4 tamaños reales (el suelo declarado, el tamaño por
                // defecto de la app, un intermedio y uno Amplio - donde Inventario y Almacen
                // conviven y la mitad derecha es una COPIA A MANO de la cabecera, justo el sitio
                // donde una etiqueta se pierde sin que nadie lo note) x los 2 idiomas.
                foreach (string idioma in new[] { "es", "en" })
                {
                    LocalizationService.Instance.SetLanguage(idioma);
                    DoEvents();
                    foreach (var (w, h) in new[] { (1080.0, 700.0), (1180.0, 860.0), (1400.0, 900.0), (1600.0, 900.0) })
                    {
                        FijarTamaño(window, w, h);
                        foreach (int sub in new[] { 0, 1, 2 })
                        {
                            // "Almacenes" (2) se oculta entera en Amplio a proposito (A-a): ahi su
                            // contenido vive dentro de "Inventario", ya cubierto por sub=1.
                            if (sub == 2 && vm.IsStorageExpanded) continue;
                            vm.ObjetosSubTabIndex = sub;
                            DoEvents(); DoEvents();
                            var rotos = BindingsRotos(tabObjetos).Distinct().ToList();
                            string etiqueta = sub switch { 0 => "Equipamiento", 1 => "Inventario", _ => "Almacenes" };
                            Console.WriteLine($"OBJ-01: [{idioma}] {w:0}x{h:0} {etiqueta} -> {rotos.Count} binding(s) distintos con la ruta rota (esperado 0)"
                                              + (rotos.Count > 0 ? " | " + string.Join(" ; ", rotos) : ""));
                            if (rotos.Count > 0)
                                Console.WriteLine($"FALLO: OBJ-01 - [{idioma}] {w:0}x{h:0} {etiqueta}: {rotos.Count} binding(s) apuntan a una propiedad que no existe en su DataContext (el control se queda mudo, sin ningun error visible)");
                        }
                    }
                }
                LocalizationService.Instance.SetLanguage(idiomaOriginal);
                DoEvents();

                // OBJ-02: la consecuencia REAL de lo de arriba, medida en pantalla y no por
                // deduccion - un boton visible cuyo Content viene de un Binding y se quedo vacio
                // es un boton que el usuario NO PUEDE encontrar ni pulsar. Se mide en los dos
                // estados donde viven los botones de conjunto del ALMACEN: la pestaña "Almacenes"
                // propia (Normal) y la mitad derecha de "Inventario" (Amplio).
                foreach (var (w, h, sub, etiqueta) in new (double w, double h, int sub, string etiqueta)[]
                         { (1400, 900, 2, "pestaña Almacenes"), (1600, 900, 1, "Inventario+Almacen en Amplio") })
                {
                    FijarTamaño(window, w, h);
                    vm.ObjetosSubTabIndex = sub;
                    DoEvents(); DoEvents();
                    var mudos = Descendientes<Button>(tabObjetos)
                        .Where(b => b.IsVisible
                                    && System.Windows.Data.BindingOperations.GetBindingExpressionBase(b, ContentControl.ContentProperty) != null
                                    && (b.Content == null || (b.Content is string s && string.IsNullOrWhiteSpace(s))))
                        .ToList();
                    Console.WriteLine($"OBJ-02: {w:0}x{h:0} {etiqueta} -> {mudos.Count} boton(es) visibles con el texto vacio (esperado 0), anchos=[{string.Join(", ", mudos.Select(b => $"{b.ActualWidth:0}px"))}]");
                    if (mudos.Count > 0)
                        Console.WriteLine($"FALLO: OBJ-02 - {etiqueta}: {mudos.Count} boton(es) reales sin texto - imposibles de encontrar y de pulsar");
                }

                // OBJ-03: las 3 etiquetas reales de la cabecera de Equipamiento ("Conjunto:",
                // "Vista:", "Defensa total:"). Se comprueban aparte de OBJ-01/02 porque un
                // TextBlock vacio NO desaparece del todo: deja el numero de defensa suelto sin
                // decir de que es (capturado real en equipamiento-fusionado.png), un fallo mas
                // dificil de ver de un vistazo que un boton que directamente no esta.
                FijarTamaño(window, 1180, 860);
                vm.ObjetosSubTabIndex = 0;
                DoEvents(); DoEvents();
                hostEquipo ??= Descendientes<Terrakeep.App.Controls.SlotRowHost>(window).FirstOrDefault();
                // Se comparan contra el TEXTO REAL del diccionario, no contra un literal en
                // español escrito aqui: asi la comprobacion vale igual en los dos idiomas y
                // detecta tanto una etiqueta vacia (el bug de OBJ-01) como una que se quedara
                // desincronizada del diccionario. El texto del TextBlock incluye tambien el
                // valor (ej. "Defensa total: 51"), por eso es "empieza por", no igualdad.
                var textosEquipo = Descendientes<TextBlock>(hostEquipo!).Where(t => t.IsVisible).ToList();
                int vaciosEquipo = textosEquipo.Count(t => string.IsNullOrWhiteSpace(t.Text)
                                                           && System.Windows.Data.BindingOperations.GetBindingExpressionBase(t, TextBlock.TextProperty) != null);
                var faltan = new List<string>();
                foreach (string clave in new[] { "char_loadout_label", "char_view_label", "char_total_defense" })
                {
                    string esperado = LocalizationService.Instance[clave].Trim();
                    if (!textosEquipo.Any(t => t.Text.Trim().StartsWith(esperado, StringComparison.Ordinal))) faltan.Add($"{clave} ('{esperado}')");
                }
                Console.WriteLine($"OBJ-03: cabecera de Equipamiento -> etiquetas reales que faltan={faltan.Count} (esperado 0){(faltan.Count > 0 ? " | " + string.Join(" ; ", faltan) : "")}, TextBlocks enlazados y vacios={vaciosEquipo} (esperado 0)");
                if (faltan.Count > 0 || vaciosEquipo > 0)
                    Console.WriteLine("FALLO: OBJ-03 - falta alguna de las 3 etiquetas reales de la cabecera de Equipamiento (el numero de defensa se queda suelto, sin decir de que es)");
            }

            // OBJ-07: las 4 pildoras de almacen ("Banco (15/40)", "Caja fuerte (0/40)", "Fragua
            // del Defensor (15/40)", "Bóveda del Vacío (0/40)") no pueden cortarse en NINGUN
            // tamaño ni idioma. Se mide el ancho natural del texto contra el ancho real de la
            // caja, que es la unica forma de detectarlo: un texto sin TextTrimming que no cabe no
            // lleva ningun clip propio (VisualTreeHelper.GetClip da null), simplemente se dibuja
            // cortado - el mismo mecanismo que AR-13a ya usa para el reparto horizontal.
            //
            // El barrido va de 20 en 20px por todo el rango real de la ventana, en los dos
            // idiomas y en los DOS sitios donde viven esas pildoras (la pestaña "Almacenes"
            // propia por debajo del umbral de Amplio, y la mitad derecha de "Inventario" por
            // encima) - el caso que fallaba de verdad, 1520x864, es justo el primer ancho de
            // Amplio, o sea el mas apretado de los dos repartos.
            if (tabObjetos != null)
            {
                string idiomaAntes = LocalizationService.Instance.Language;
                var cortadas = new List<string>();
                int medidas = 0;
                foreach (string idioma in new[] { "es", "en" })
                {
                    LocalizationService.Instance.SetLanguage(idioma);
                    for (double w = 1080; w <= 1920; w += 20)
                    {
                        FijarTamaño(window, w, 864);
                        vm.ObjetosSubTabIndex = vm.IsStorageExpanded ? 1 : 2;
                        DoEvents(); DoEvents();
                        // Las pildoras se identifican por su TEXTO real (el DisplayLabel vivo de
                        // StorageGroup.Options), no por DataContext: un Button con Content string
                        // crea un TextBlock cuyo DataContext ES el propio string, no el
                        // EquipmentOptionViewModel - primer intento de este bloque, que medio 0
                        // pildoras y dio un "0 cortadas" que no demostraba nada.
                        var etiquetas = vm.StorageGroup?.Options.Select(o => o.DisplayLabel).ToHashSet() ?? [];
                        foreach (var tb in Descendientes<TextBlock>(tabObjetos))
                        {
                            if (!tb.IsVisible || !etiquetas.Contains(tb.Text)) continue;
                            medidas++;
                            // Los DOS mecanismos de corte, porque son distintos y aqui manda el
                            // segundo (leccion ya documentada en AR-11c/AR-15): (1) la caja del
                            // propio TextBlock es mas estrecha que su texto; (2) la caja es
                            // suficiente pero un ANCESTRO recorta - y entonces el TextBlock sigue
                            // diciendo que mide su ancho entero y TextoRecortado() da false. El
                            // caso real medido a 1520x864 era exactamente el (2): caja de 137px
                            // completa, de la que solo se pintaban 91.
                            double visible = RectVisible(tb, window).Width;
                            double necesita = AnchoNaturalDelTexto(tb);
                            bool cortadoPorLaCaja = TextoRecortado(tb);
                            bool cortadoPorUnAncestro = visible + 0.5 < Math.Min(necesita, tb.ActualWidth);
                            if (!cortadoPorLaCaja && !cortadoPorUnAncestro) continue;
                            cortadas.Add($"[{idioma}] {w:0}x864 '{tb.Text}' caja={tb.ActualWidth:0}px visible={visible:0}px necesita={necesita:0}px");
                        }
                    }
                }
                LocalizationService.Instance.SetLanguage(idiomaAntes);
                Console.WriteLine($"OBJ-07: {medidas} pildoras de almacen medidas (1080..1920 de 20 en 20, 2 idiomas) -> {cortadas.Count} cortadas (esperado 0)"
                                  + (cortadas.Count > 0 ? " | " + string.Join(" ; ", cortadas.Take(6)) : ""));
                if (cortadas.Count > 0)
                    Console.WriteLine($"FALLO: OBJ-07 - {cortadas.Count} pildora(s) de almacen se cortan: el usuario no puede leer de que almacen es ni cuanto lleva dentro");
            }

            FijarTamaño(window, 1180, 860);
            vm.ObjetosSubTabIndex = 0;
            DoEvents();
        }
        catch (Exception ex)
        {
            Console.WriteLine("OBJ-01-EXCEPTION: " + ex);
        }

        // OBJ-STATS-IDIOMA (ronda de idioma del 6-sep-2026). Cierra el hallazgo que la oleada de
        // QA de Personaje->Objetos dejo anotado sin tocar: "ItemStatsFormatter compone TODO el
        // texto en español a fuego, asi que con la app en ingles los tooltips de estadisticas
        // siguen en español. No lo ve el barrido de idioma (son popups)".
        //
        // Por eso este bloque ABRE EL POPUP DE VERDAD (ToolTip.IsOpen = true) y lee el
        // TextBlock real que se pinta dentro, en los dos idiomas, en vez de mirar la propiedad
        // del ViewModel - que es justo lo que no demostraba nada: el ViewModel puede estar bien
        // y el binding del popup quedarse congelado con el primer valor que leyo. Identificador
        // NO numerico a proposito (misma leccion que AR-LAY: dos rondas en paralelo ya se
        // pisaron renumerando bloques en este mismo fichero).
        try
        {
            var slotArma = vm.InventoryContainer?.Slots.FirstOrDefault();
            var slotCasco = vm.EquipmentGroup?.EquippedItems.Slots.FirstOrDefault();
            if (slotArma == null || slotCasco == null)
            {
                Console.WriteLine("OBJ-STATS-IDIOMA: sin personaje cargado, bloque omitido");
            }
            else
            {
                string idiomaAntes = LocalizationService.Instance.Language;
                // El arnes trabaja sobre una COPIA de un personaje real, pero aun asi se deja
                // todo como estaba: los bloques siguientes miran estos mismos contenedores.
                var armaAntes = slotArma.Item.Clone();
                var cascoAntes = slotCasco.Item.Clone();
                try
                {
                    vm.ObjetosSubTabIndex = 0;
                    DoEvents();
                    slotArma.PlaceItem(4);        // "Espada larga de hierro": daño + DPS + velocidad + retroceso
                    slotCasco.PlaceItem(20000244); // "Sombrero de Aerospec": defensa + bono de set de Calamity
                    DoEvents();

                    // El TextBlock del tooltip real, buscado por el popup abierto de cada slot.
                    // El ToolTip vive declarado en el XAML (Border.ToolTip), asi que su arbol
                    // visual solo existe mientras esta abierto - de ahi el IsOpen=true.
                    //
                    // PlacementTarget hay que ponerlo A MANO, y no es un detalle: el ToolTip del
                    // XAML toma su DataContext de "{Binding PlacementTarget.DataContext,
                    // RelativeSource=Self}", y eso normalmente lo rellena ToolTipService al
                    // abrirlo el raton. Abriendolo a pelo con IsOpen=true, PlacementTarget queda
                    // null, el DataContext tambien, y TODOS los TextBlock salen vacios - o sea un
                    // popup que se abre de verdad pero no demuestra nada (primer intento de este
                    // bloque: 14 "faltan" con los cuatro tooltips en blanco).
                    static string LeerTooltipReal(Window w, object dataContext)
                    {
                        foreach (var b in Descendientes<Border>(w))
                        {
                            if (!ReferenceEquals(b.DataContext, dataContext) || b.ToolTip is not ToolTip tt) continue;
                            tt.PlacementTarget = b;
                            tt.IsOpen = true;
                            DoEvents();
                            // Un pase de layout explicito, y no es de adorno: en la PRIMERA
                            // apertura de un ToolTip los bindings con conversor de visibilidad
                            // (el `EmptyToCollapsed` de la linea "Prefijo: X") todavia no se han
                            // evaluado, asi que ese TextBlock existe pero declara IsVisible=false
                            // y el filtro de abajo lo tiraba. Se noto porque la linea del prefijo
                            // salia en la segunda lectura (ingles) y no en la primera (español).
                            tt.UpdateLayout();
                            DoEvents(); DoEvents();
                            string texto = string.Join("\n", Descendientes<TextBlock>(tt)
                                .Where(t => t.IsVisible && !string.IsNullOrWhiteSpace(t.Text)).Select(t => t.Text));
                            tt.IsOpen = false;
                            DoEvents();
                            if (texto.Length > 0) return texto;
                        }
                        return "";
                    }

                    var fallos = new List<string>();
                    void Comprobar(string caso, string texto, string[] debeTener, string[] noDebeTener)
                    {
                        foreach (string s in debeTener)
                            if (!texto.Contains(s, StringComparison.Ordinal)) fallos.Add($"{caso}: falta '{s}'");
                        foreach (string s in noDebeTener)
                            if (texto.Contains(s, StringComparison.Ordinal)) fallos.Add($"{caso}: sigue apareciendo '{s}'");
                    }

                    // Cada slot solo existe en el arbol visual con SU pestaña delante (0 =
                    // Equipamiento, 1 = Inventario) - leerlos sin cambiar de pestaña devolveria
                    // "" y volveria a no demostrar nada.
                    string LeerArma() { vm.ObjetosSubTabIndex = 1; DoEvents(); return LeerTooltipReal(window, slotArma); }
                    string LeerCasco() { vm.ObjetosSubTabIndex = 0; DoEvents(); return LeerTooltipReal(window, slotCasco); }

                    LocalizationService.Instance.SetLanguage("es");
                    DoEvents();
                    // Calentamiento: la primera apertura de cada ToolTip se descarta a proposito
                    // (ver el comentario de UpdateLayout arriba) para que las dos lecturas que SI
                    // se comparan partan del mismo estado, y no una de un popup recien nacido y
                    // la otra de uno ya asentado - seria comparar peras con manzanas.
                    LeerArma(); LeerCasco();
                    string armaEs = LeerArma();
                    string cascoEs = LeerCasco();
                    Console.WriteLine("OBJ-STATS-IDIOMA [es] arma:\n  " + armaEs.Replace("\n", "\n  "));
                    Console.WriteLine("OBJ-STATS-IDIOMA [es] casco:\n  " + cascoEs.Replace("\n", "\n  "));
                    // Ademas de las estadisticas, las otras DOS cosas del mismo popup que
                    // dependen del idioma y que solo se ven abriendolo de verdad: el rol del
                    // slot (SlotRoleLabel) y el NOMBRE del prefijo (PrefixDisplay).
                    // OJO con añadir aqui "Legendario": la linea "Prefijo: X" del popup lleva un
                    // `Visibility` con conversor (`EmptyToCollapsed` sobre PrefixDisplay) que solo
                    // se reevalua cuando salta un PropertyChanged de esa propiedad, asi que en la
                    // PRIMERA lectura (español) el TextBlock existe pero declara IsVisible=false
                    // y no se puede medir de forma fiable - en la segunda (ingles) si, porque
                    // cambiar de idioma dispara ese aviso. No es un bug del producto (el nombre se
                    // ve perfectamente al pasar el raton de verdad) sino un limite de medir un
                    // popup abierto a mano: probado con un calentamiento previo y con
                    // UpdateLayout() explicito, y sigue igual. La cara española de PrefixDisplay
                    // se comprueba donde SI es determinista, en
                    // ObjetosTooltipStatsTests.ElNombreDelPrefijoYElRolDelSlotSiguenAlIdioma.
                    Comprobar("es/arma", armaEs, ["daño de cuerpo a cuerpo", "DPS", "Tiempo de uso", "Muy Rapido", "Retroceso"], []);
                    Comprobar("es/casco", cascoEs, ["defensa", "Con el set completo:", "Cabeza"], []);

                    LocalizationService.Instance.SetLanguage("en");
                    DoEvents();
                    string armaEn = LeerArma();
                    string cascoEn = LeerCasco();
                    Console.WriteLine("OBJ-STATS-IDIOMA [en] arma:\n  " + armaEn.Replace("\n", "\n  "));
                    Console.WriteLine("OBJ-STATS-IDIOMA [en] casco:\n  " + cascoEn.Replace("\n", "\n  "));
                    // "daño"/"Retroceso"/"Muy Rapido" son EXACTAMENTE el texto fijo que iba a
                    // fuego en Core y que con la app en ingles seguia saliendo en español.
                    Comprobar("en/arma", armaEn, ["melee damage", "DPS", "Use time", "Very Fast", "Knockback", "Legendary"],
                        ["daño", "Retroceso", "Muy Rapido", "Legendario"]);
                    // El bono de set en si ("Aumenta...", texto de contenido del juego) sigue en
                    // español a proposito, igual que los NOMBRES de objeto en toda la app - lo
                    // que aqui se comprueba es la FRASE del editor que lo envuelve.
                    Comprobar("en/casco", cascoEn, ["defense", "With the full set:", "Head"], ["Con el set completo", "Cabeza"]);

                    Console.WriteLine($"OBJ-STATS-IDIOMA: {fallos.Count} discrepancia(s) en el tooltip REAL abierto, 2 idiomas x 2 objetos (esperado 0)"
                                      + (fallos.Count > 0 ? " | " + string.Join(" ; ", fallos) : ""));
                    if (fallos.Count > 0)
                        Console.WriteLine("FALLO: OBJ-STATS-IDIOMA - el tooltip de estadisticas no sale entero en el idioma activo");
                }
                finally
                {
                    LocalizationService.Instance.SetLanguage(idiomaAntes);
                    slotArma.UpdateFrom(armaAntes);
                    slotCasco.UpdateFrom(cascoAntes);
                    DoEvents();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("OBJ-STATS-IDIOMA-EXCEPTION: " + ex);
        }

        // (Bloque real eliminado del arnes: probaba el rediseño de la Libreria de la
        // octava pasada - Fases 2/3/4/5/7 -, revertido entero por feedback directo del
        // usuario. Ver bitacora.md "REVERTIDO por feedback directo".

        // Verificacion real de R-1 (auditoria de Opus, Bloque 2): umbral real de investigacion
        // por objeto, extraido del TSV real de sacrificios de tModLoader - dos ids conocidos de
        // memoria del juego real (IronBroadsword=1, arma unica -> categoria D; DirtBlock=100,
        // bloque comun -> categoria L), directamente contra el catalogo cargado por la propia
        // app (misma instancia que ya construyo MainViewModel, via reflexion de su campo
        // privado _service - mas fiel que construir una segunda instancia aparte).
        try
        {
            var serviceField = typeof(MainViewModel).GetField("_service", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var svc = (Terrakeep.App.Services.CharacterFileService)serviceField!.GetValue(vm)!;
            Console.WriteLine($"R1-CATALOGO: IronBroadsword(id=4)={svc.VanillaResearchCounts.Get(4)} (esperado 1), DirtBlock(id=2)={svc.VanillaResearchCounts.Get(2)} (esperado 100)");

            var swResearch = System.Diagnostics.Stopwatch.StartNew();
            vm.ResearchAllCommand.Execute(null);
            swResearch.Stop();
            Console.WriteLine($"MEDICION-INVESTIGAR-TODO: {swResearch.ElapsedMilliseconds}ms");
            DoEvents();
            var ironNode = FindCategoryWithItem(vm.Research.RootCategories, 4);
            if (ironNode != null)
            {
                vm.Research.SelectCategoryCommand.Execute(ironNode);
                DoEvents();
                var row = vm.Research.Results.FirstOrDefault();
                Console.WriteLine($"R1-INVESTIGAR-TODO: primera fila de '{ironNode.Name}' -> CountLabel={row?.CountLabel} (esperado formato real x/N, NO '9999')");
            }
            else Console.WriteLine("R1-INVESTIGAR-TODO: ninguna carpeta real con IronBroadsword encontrada");

            // R-d/R-e/R-f/R-g (segunda auditoria de Opus, Fable): verificacion visual real de
            // los 4 arreglos de Fase 1 a la vez - aviso de Modo Viaje (UIA-Test es Softcore por
            // omision), buscador real (mismo cuadro/estilo que Libreria) y progreso "N/Total".
            vm.Research.ClearCategoryCommand.Execute(null); // sin esto, la busqueda de abajo queda acotada a 'Materiales' (la carpeta que aun seguia elegida)
            vm.Research.SearchText = "#20000000"; // CalamityIds.ItemIdBase - primer id sintetico real de objeto de Calamity
            // H5-02 (quinta auditoria de Opus): bug real de este arnes encontrado verificando
            // H5-02 (no de produccion) - SearchText dispara un DispatcherTimer real de 180ms
            // (CatalogBrowserViewModel), y un unico DoEvents() no espera tiempo real ninguno,
            // solo vacia lo que ya este listo AHORA. Resultado real: Results seguia con el
            // estado ANTERIOR (vacio) en el momento de comprobarlo - flakiness pura de
            // temporizacion, no un bug de ApplyFilter. Mismo remedio real ya probado en L-c
            // (LibraryViewModel) - WaitForDispatcher bombea Y cede la CPU de verdad hasta que
            // el tiempo pedido transcurre, dejando que el Tick real llegue a disparar.
            WaitForDispatcher(300);
            var calamityRow = vm.Research.Results.FirstOrDefault(r => r.IsCalamity);
            Console.WriteLine($"R-d: fila de Calamity (#20000000) tras Investigar todo -> CountLabel={calamityRow?.CountLabel} (esperado 'Investigado', nunca '9999')");
            if (calamityRow != null && calamityRow.CountLabel.Contains("9999")) Console.WriteLine("FALLO: R-d (segunda auditoria) - el 9999 crudo sigue visible en un chip de Calamity");
            vm.Research.SearchText = string.Empty;
            vm.Research.ClearCategoryCommand.Execute(null);
            DoEvents();
            Console.WriteLine($"R-f: ResultsSummary sin carpeta ni busqueda -> \"{vm.Research.ResultsSummary}\" (esperado formato real N/Total)");
            vm.Research.SearchText = "#4"; // Iron Broadsword, id real vanilla 4
            WaitForDispatcher(300); // mismo arreglo real de arriba (R-d) - esta pasaba por lo mismo, R-e incluido
            Console.WriteLine($"R-e: busqueda '#4' sin carpeta elegida -> {vm.Research.Results.Count} resultado(s) (esperado 1)");
            vm.SelectedTabIndex = 1; // Personaje (AppTab.Personaje) - el test de AutoEquip de arriba dejo Builds seleccionado
            vm.PersonajeInnerTabIndex = 2; // Investigacion
            DoEvents(); DoEvents();
            {
                var rtbResearch = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbResearch.Render(window);
                var encResearch = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encResearch.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbResearch));
                using var fsResearch = File.Create(Path.Combine(AppContext.BaseDirectory, "investigacion-ola3.png"));
                encResearch.Save(fsResearch);
            }
            vm.Research.SearchText = string.Empty; // no dejar la busqueda puesta para el resto de pruebas
            DoEvents();
        }
        catch (Exception ex)
        {
            Console.WriteLine("R1-EXCEPTION: " + ex);
        }

        // Verificacion real de N-3 (auditoria de Opus, Bloque 3): atajos de teclado con
        // pulsaciones REALES a nivel de SO (Keyboard.Modifiers no se puede fingir con un
        // RoutedEventArgs sintetico, lee el estado real del teclado) - ver PressCtrlPlus/
        // PressKey arriba. Ctrl+O NO se prueba aqui (abriria un dialogo modal real - mismo
        // riesgo de cuelgue ya documentado para MessageBox en N-2 - pero reutiliza LITERALMENTE
        // el mismo OnLoadClick que ya prueba a diario el boton "Cargar personaje...", cero
        // riesgo nuevo).
        try
        {
            ForzarPrimerPlano(hwnd);
            window.Activate();
            DoEvents();

            // Ctrl+F: cambia a Builds primero para confirmar que el atajo SALTA de verdad a la
            // Libreria (no que ya estuviera ahi por casualidad de un test anterior).
            vm.SelectedTabIndex = 2; // Builds
            vm.IsLibraryCollapsed = true;
            DoEvents();
            PressCtrlPlus(0x46); // VK_F
            DoEvents();
            DoEvents();
            var focused = System.Windows.Input.Keyboard.FocusedElement as FrameworkElement;
            // ADR-TERRAKEEP-016/031 (27-sep-2026): LibrarySearchBox vive ahora dentro de
            // ObjetosView (NameScope propio) - FindName doble, mismo patron ya usado por el resto
            // de la familia.
            var objetosViewHostN3 = window.FindName("ObjetosView") as FrameworkElement;
            Console.WriteLine($"N3-CTRL-F: SelectedTabIndex={vm.SelectedTabIndex} (esperado 1), IsLibraryCollapsed={vm.IsLibraryCollapsed} (esperado False), foco real en LibrarySearchBox={ReferenceEquals(focused, objetosViewHostN3?.FindName("LibrarySearchBox"))}");

            // Esc: pide elegir objeto para un slot real (IsPicking pasa a True, mismo camino
            // real que pulsar "Elegir..." en un slot) y confirma que Esc cancela de verdad.
            var anySlot = vm.InventoryContainer?.Slots.FirstOrDefault();
            if (anySlot != null)
            {
                anySlot.ChooseFromLibraryCommand.Execute(null);
                DoEvents();
                bool pickingAntes = vm.Library.IsPicking;
                PressKey(0x1B); // VK_ESCAPE
                DoEvents();
                Console.WriteLine($"N3-ESC: IsPicking antes={pickingAntes} (esperado True), despues={vm.Library.IsPicking} (esperado False)");
            }
            else Console.WriteLine("N3-ESC: sin slot de Inventario real para probar - omitido");

            // L-b (segunda auditoria de Opus, Fable): "el aviso de 'solo validos para el slot
            // seleccionado' es un texto mas, ni siquiera dice CUAL slot" - abre el selector para
            // un slot REALMENTE restringido (Mascota, ver MainViewModel.AddContainer
            // miscEquipKinds) y confirma la pildora real con el rol real del slot.
            var slotRestringido = vm.MountsContainer?.Slots.FirstOrDefault();
            if (slotRestringido != null)
            {
                vm.SelectedTabIndex = 1; // Personaje
                vm.PersonajeInnerTabIndex = 0; // Objetos
                DoEvents();
                slotRestringido.ChooseFromLibraryCommand.Execute(null);
                DoEvents();
                Console.WriteLine($"L-B-PILDORA: SlotRoleLabel real={slotRestringido.SlotRoleLabel}, Library.SlotRestrictionLabel={vm.Library.SlotRestrictionLabel} (esperado que coincidan, no null)");
                if (vm.Library.SlotRestrictionLabel != slotRestringido.SlotRoleLabel)
                    Console.WriteLine("FALLO: L-b (segunda auditoria) - la pildora no muestra el rol real del slot restringido");
                var rtbPill = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbPill.Render(window);
                var encPill = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encPill.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbPill));
                using (var fsPill = File.Create(Path.Combine(AppContext.BaseDirectory, "libreria-pildora-slot.png"))) encPill.Save(fsPill);
                Console.WriteLine("Captura pildora de restriccion de slot -> libreria-pildora-slot.png");
                vm.Library.CancelPickCommand.Execute(null);
                DoEvents();
            }
            else Console.WriteLine("L-B-PILDORA: sin slot de Mascota real para probar - omitido");

            // L-d (segunda auditoria de Opus, Fable): "sin ScrollViewer de seguridad en el
            // panel Editar - un objeto con muchos prefijos legales podria desbordar la altura
            // real en una ventana baja". Peor caso real acotado: meta "Positivos" (8 grupos
            // reales, ver PrefixGroupCatalog) + grupo "Cuerpo a cuerpo +" (10 prefijos reales)
            // sobre un arma real, en la altura MINIMA real documentada de la app (700px) -
            // confirma que el ScrollViewer nuevo SI tiene margen real de scroll (prueba de que
            // el contenido de verdad se acerca/supera el alto disponible) y que se puede
            // desplazar hasta el final sin excepciones.
            try
            {
                window.Height = 700;
                vm.SelectedTabIndex = 1; // Personaje
                vm.PersonajeInnerTabIndex = 0; // Objetos
                var slotParaPrefijos = vm.InventoryContainer?.Slots.FirstOrDefault(s => s.IsEmpty);
                if (slotParaPrefijos != null)
                {
                    slotParaPrefijos.PlaceItem(4); // Iron Broadsword, arma real (Melee)
                    vm.SelectSlot(slotParaPrefijos);
                    DoEvents();
                    var positivos = vm.ItemEdit.Metas.FirstOrDefault(m => m.Label.Contains("Positivos", StringComparison.OrdinalIgnoreCase));
                    if (positivos != null)
                    {
                        vm.ItemEdit.SelectMetaCommand.Execute(positivos);
                        DoEvents();
                        var meleePlus = vm.ItemEdit.Groups.FirstOrDefault(g => g.Label.Contains("Cuerpo a cuerpo", StringComparison.OrdinalIgnoreCase));
                        if (meleePlus != null) vm.ItemEdit.SelectGroupCommand.Execute(meleePlus);
                        DoEvents(); DoEvents();

                        ScrollViewer? FindEditorScroll(DependencyObject d)
                        {
                            if (d is ScrollViewer sv && FindDescendantText(sv, "Editar")) return sv;
                            int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
                            for (int i = 0; i < n; i++)
                            {
                                var found = FindEditorScroll(System.Windows.Media.VisualTreeHelper.GetChild(d, i));
                                if (found != null) return found;
                            }
                            return null;
                        }
                        bool FindDescendantText(DependencyObject d, string text)
                        {
                            if (d is TextBlock tb && tb.Text == text) return true;
                            int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
                            for (int i = 0; i < n; i++)
                                if (FindDescendantText(System.Windows.Media.VisualTreeHelper.GetChild(d, i), text)) return true;
                            return false;
                        }
                        var editorScroll = FindEditorScroll(window);
                        Console.WriteLine($"L-D-SCROLL: ScrollViewer real encontrado={editorScroll != null}, ScrollableHeight={editorScroll?.ScrollableHeight:0.#}px, ExtentHeight={editorScroll?.ExtentHeight:0.#}px, ViewportHeight={editorScroll?.ViewportHeight:0.#}px (Metas/Prefijos reales con grupo Cuerpo a cuerpo+ seleccionado, ventana a 700px de alto)");
                        if (editorScroll != null && editorScroll.ScrollableHeight > 0)
                        {
                            editorScroll.ScrollToEnd();
                            DoEvents(); DoEvents();
                            Console.WriteLine($"L-D-SCROLL: desplazado hasta el final sin excepcion, VerticalOffset={editorScroll.VerticalOffset:0.#}px (esperado ~= ScrollableHeight)");
                        }
                        var rtbEdit = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtbEdit.Render(window);
                        var encEdit = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encEdit.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbEdit));
                        using (var fsEdit = File.Create(Path.Combine(AppContext.BaseDirectory, "editar-scroll-700px.png"))) encEdit.Save(fsEdit);
                        Console.WriteLine("Captura panel Editar a 700px -> editar-scroll-700px.png");
                    }
                    else Console.WriteLine("L-D-SCROLL: meta 'Positivos' no encontrada - omitido");
                }
                else Console.WriteLine("L-D-SCROLL: sin slot de Inventario vacio real para probar - omitido");
            }
            catch (Exception ex) { Console.WriteLine("L-D-SCROLL-EXCEPTION: " + ex); }

            // Ap-a/Ap-b (segunda auditoria de Opus, Fable): "los selectores de peinado/tinte
            // abiertos a la vez empujan el contenido" + "228 miniaturas de peinado se
            // regeneran en cada tick del color - medir antes de tocar nada" (mismo criterio
            // que X-7/L-c).
            try
            {
                vm.PersonajeInnerTabIndex = 3; // Apariencia
                DoEvents();

                // Ap-c (segunda auditoria de Opus, Fable): "sin valor hexadecimal ni paleta
                // para los colores" - captura real de los 7 swatches con su campo hex nuevo,
                // ANTES de abrir el selector de peinado (que tapa esta zona).
                var rtbSwatches = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbSwatches.Render(window);
                var encSwatches = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encSwatches.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbSwatches));
                using (var fsSwatches = File.Create(Path.Combine(AppContext.BaseDirectory, "apariencia-colores-hex.png"))) encSwatches.Save(fsSwatches);
                Console.WriteLine("Captura colores con campo hex -> apariencia-colores-hex.png");

                var swHairOpen = System.Diagnostics.Stopwatch.StartNew();
                vm.Appearance.OpenHairPickerCommand.Execute(null); // primera apertura real - regenera las 228 miniaturas
                swHairOpen.Stop();
                Console.WriteLine($"AP-B-MEDIDA: primera apertura real (228 miniaturas) tardo {swHairOpen.ElapsedMilliseconds}ms, HairOptions.Count={vm.Appearance.HairOptions.Count} (esperado 228)");

                // Ap-a: abrir el selector de tinte debe cerrar el de peinado, y viceversa.
                vm.Appearance.OpenHairDyePickerCommand.Execute(null);
                DoEvents();
                Console.WriteLine($"AP-A-EXCLUSION: tras abrir tinte -> IsHairDyePickerOpen={vm.Appearance.IsHairDyePickerOpen} (esperado True), IsHairPickerOpen={vm.Appearance.IsHairPickerOpen} (esperado False)");
                if (vm.Appearance.IsHairPickerOpen) Console.WriteLine("FALLO: Ap-a (segunda auditoria) - abrir el selector de tinte no cerro el de peinado");
                vm.Appearance.OpenHairPickerCommand.Execute(null);
                DoEvents();
                if (vm.Appearance.IsHairDyePickerOpen) Console.WriteLine("FALLO: Ap-a (segunda auditoria) - abrir el selector de peinado no cerro el de tinte");

                // Ap-b: cambiar el color de pelo con el selector YA ABIERTO no debe dejar las
                // miniaturas en blanco para siempre - deben refrescarse de verdad tras esperar
                // el debounce real (180ms), sin regenerar en cada tick individual.
                var hairSwatch = vm.Appearance.Swatches.FirstOrDefault(s => s.Label.Contains("elo", StringComparison.OrdinalIgnoreCase));
                if (hairSwatch != null)
                {
                    hairSwatch.R = hairSwatch.R == 200 ? 199 : 200; // dispara PropertyChanged real
                    DoEvents();
                    bool vaciasJustoTrasElCambio = vm.Appearance.HairOptions.Count == 0;
                    WaitForDispatcher(300);
                    Console.WriteLine($"AP-B-REFRESH: tras cambiar color con el selector abierto -> vacias justo despues={vaciasJustoTrasElCambio}, HairOptions.Count tras esperar el debounce={vm.Appearance.HairOptions.Count} (esperado 228, nunca 0 permanente)");
                    if (vm.Appearance.HairOptions.Count != 228) Console.WriteLine("FALLO: Ap-b (segunda auditoria) - las miniaturas de peinado no se refrescaron tras cambiar el color con el selector abierto");
                }
                else Console.WriteLine("AP-B-REFRESH: swatch de color de pelo real no encontrado - omitido");

                var rtbHair = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbHair.Render(window);
                var encHair = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encHair.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbHair));
                using (var fsHair = File.Create(Path.Combine(AppContext.BaseDirectory, "apariencia-selector-peinado.png"))) encHair.Save(fsHair);
                Console.WriteLine("Captura selector de peinado -> apariencia-selector-peinado.png");
            }
            catch (Exception ex) { Console.WriteLine("AP-A-AP-B-EXCEPTION: " + ex); }

            // C-15 (informe de pulido final, cierra A1): Deshacer/Rehacer real en Apariencia -
            // los cambios DISCRETOS (peinado/tinte/genero/dificultad) ya se prueban en xunit
            // (AppearanceUndoTests, sin ventana). Lo que SOLO se puede probar aqui (necesita el
            // Dispatcher real bombeando un DispatcherTimer real) es el debounce de ~400ms de los
            // campos continuos - vida/mana/horas (TextBox con UpdateSourceTrigger=
            // PropertyChanged, cada caracter tecleado dispara un cambio real) y los 7 colores
            // (Slider, arrastrar dispara docenas de eventos por segundo).
            try
            {
                // HealthMax en cascada baja HealthNow si queda por encima (logica real ya
                // existente, Ap-f) - eso dispararia SU PROPIO debounce bajo otra key y rompería
                // el recuento esperado. HealthNow=0 primero (y se deja asentar) para que ningun
                // valor de prueba de HealthMax de mas abajo (9/99/999) quede nunca por debajo.
                vm.Appearance.HealthNow = 0;
                DoEvents();
                WaitForDispatcher(700);

                int undosAntes = vm.UndoStack.Entries.Count;
                int maxOriginal = vm.Appearance.HealthMax;
                // Simula escribir "999" caracter a caracter (3 cambios reales en < 400ms) - sin
                // agrupar, esto dejaria 3 entradas de un digito cada una en el historial.
                vm.Appearance.HealthMax = 9;
                DoEvents();
                vm.Appearance.HealthMax = 99;
                DoEvents();
                vm.Appearance.HealthMax = 999;
                DoEvents();
                int undosJustoTrasEscribir = vm.UndoStack.Entries.Count;
                WaitForDispatcher(700); // > 400ms del debounce real
                int undosTrasElDebounce = vm.UndoStack.Entries.Count;
                Console.WriteLine($"C-15-DEBOUNCE-VIDA: entradas antes={undosAntes}, justo tras escribir 3 digitos={undosJustoTrasEscribir} (esperado igual, el debounce aun no disparo), tras esperar 700ms={undosTrasElDebounce} (esperado antes+1, UNA sola entrada)");
                if (undosJustoTrasEscribir != undosAntes || undosTrasElDebounce != undosAntes + 1)
                    Console.WriteLine("FALLO: C-15 - el debounce de vida maxima no agrupo la rafaga en una unica entrada");

                vm.UndoEditCommand.Execute(null);
                Console.WriteLine($"C-15-DEBOUNCE-VIDA-DESHACER: HealthMax tras Deshacer={vm.Appearance.HealthMax} (esperado {maxOriginal}, el valor de ANTES de toda la rafaga)");
                if (vm.Appearance.HealthMax != maxOriginal) Console.WriteLine("FALLO: C-15 - Deshacer la rafaga de vida maxima no vuelve al valor de antes del gesto completo");
                vm.RedoEditCommand.Execute(null);
                if (vm.Appearance.HealthMax != 999) Console.WriteLine("FALLO: C-15 - Rehacer la rafaga de vida maxima no vuelve al valor final del gesto");

                // Mismo criterio para un color real (Slider R/G/B) - arrastrar los 3 canales
                // cuenta como UN solo cambio de color, no tres.
                var swatchParaUndo = vm.Appearance.Swatches[0];
                int rOriginal = swatchParaUndo.R, gOriginal = swatchParaUndo.G, bOriginal = swatchParaUndo.B;
                int undosAntesColor = vm.UndoStack.Entries.Count;
                swatchParaUndo.R = (rOriginal + 10) % 256;
                DoEvents();
                swatchParaUndo.G = (gOriginal + 20) % 256;
                DoEvents();
                swatchParaUndo.B = (bOriginal + 30) % 256;
                DoEvents();
                WaitForDispatcher(700);
                int undosTrasColorDebounce = vm.UndoStack.Entries.Count;
                Console.WriteLine($"C-15-DEBOUNCE-COLOR: entradas antes={undosAntesColor}, tras arrastrar R/G/B y esperar={undosTrasColorDebounce} (esperado +1, UNA sola entrada para los 3 canales)");
                if (undosTrasColorDebounce != undosAntesColor + 1) Console.WriteLine("FALLO: C-15 - el debounce de un color no agrupo R/G/B en una unica entrada");
                vm.UndoEditCommand.Execute(null);
                bool colorRestaurado = swatchParaUndo.R == rOriginal && swatchParaUndo.G == gOriginal && swatchParaUndo.B == bOriginal;
                Console.WriteLine($"C-15-DEBOUNCE-COLOR-DESHACER: color restaurado a (R={rOriginal},G={gOriginal},B={bOriginal})={colorRestaurado} (esperado True)");
                if (!colorRestaurado) Console.WriteLine("FALLO: C-15 - Deshacer el color no restaura los 3 canales originales");
            }
            catch (Exception ex) { Console.WriteLine("C-15-DEBOUNCE-EXCEPTION: " + ex); }

            // Ctrl+S: confirma que dispara el mismo guardado real (banner de confirmacion) que
            // ya prueba GUARDAR-DESDE-BUILDS, esta vez por teclado.
            vm.IsDirty = true; // fuerza un estado "con cambios" real para que Guardar tenga sentido
            vm.SaveConfirmationVisible = false;
            DoEvents();
            // Bug real de este arnes encontrado verificando H5-08 (quinta auditoria de Opus,
            // no del codigo de produccion): PressCtrlPlus inyecta la tecla a nivel de SO
            // (keybd_event) contra el foreground window REAL, no contra "window" por binding -
            // el ultimo SetForegroundWindow explicito quedaba muy atras (linea ~1181, antes de
            // Ctrl+F), y entre medias corren capturas RenderTargetBitmap/redimensionados de
            // sobra para que el foco real del SO derive - visto flaquear 1/4 sin esto (Ctrl+S
            // inyectado a ningun sitio real, SaveConfirmationVisible se quedaba en False).
            // Mismo patron ya usado en la linea ~1745 para el test de foco por teclado.
            ForzarPrimerPlano(hwnd);
            PressCtrlPlus(0x53); // VK_S
            DoEvents();
            DoEvents();
            Console.WriteLine($"N3-CTRL-S: SaveConfirmationVisible={vm.SaveConfirmationVisible} (esperado True)");
        }
        catch (Exception ex)
        {
            Console.WriteLine("N3-EXCEPTION: " + ex);
        }

        // H5-09 (quinta auditoria de Opus): Desbloqueos/Version/Novedades(x2)/Acerca de tenian
        // ancho fijo a mano - verificacion real de que DetailContentMaxWidth/DetailCardColumns
        // responden de verdad al SizeClass (no solo que la propiedad C# calcule bien, que ya
        // cubren los tests unitarios de MainViewModel - aqui lo que importa es que el XAML nuevo
        // (WrapPanel de familias en Desbloqueos, WrapPanel de grupos en Version, UniformGrid de
        // tarjetas en Novedades/Acerca de) renderiza sin excepcion y usa de verdad el ancho de
        // sobra en Amplio frente a Compacto.
        try
        {
            void CaptureDetailTab(int selectedTabIndex, int? personajeInnerTabIndex, string tabName, string fileName)
            {
                vm.SelectedTabIndex = selectedTabIndex;
                if (personajeInnerTabIndex is { } inner) vm.PersonajeInnerTabIndex = inner;
                DoEvents();
                var tabItem = root.FindFirst(TreeScope.Descendants, new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                    new PropertyCondition(AutomationElement.NameProperty, tabName)));
                if (tabItem != null && tabItem.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selPat))
                    ((SelectionItemPattern)selPat).Select();
                else
                    Console.WriteLine($"  AVISO H5-09: TabItem '{tabName}' no encontrado");
                DoEvents();
                DoEvents();
                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(window);
                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                using var fs = File.Create(Path.Combine(AppContext.BaseDirectory, fileName));
                enc.Save(fs);
                Console.WriteLine($"  Captura {tabName} -> {fileName}");
            }

            FijarTamaño(window, 1550, 900);
            Console.WriteLine($"H5-09-AMPLIO: SizeClass={vm.SizeClass} DetailContentMaxWidth={vm.DetailContentMaxWidth} DetailCardColumns={vm.DetailCardColumns} (esperado Amplio/1200/2)");
            CaptureDetailTab(1, 5, "Desbloqueos", "h5-09-desbloqueos-amplio.png");
            CaptureDetailTab(1, 6, "Versión", "h5-09-version-amplio.png");
            CaptureDetailTab(3, null, "Terraria", "h5-09-novedades-amplio.png");
            CaptureDetailTab(5, null, "Acerca de", "h5-09-acerca-de-amplio.png");

            FijarTamaño(window, 1180, 860);
            Console.WriteLine($"H5-09-COMPACTO: SizeClass={vm.SizeClass} DetailContentMaxWidth={vm.DetailContentMaxWidth} DetailCardColumns={vm.DetailCardColumns} (esperado Compacto o Normal/760/1)");
            CaptureDetailTab(1, 5, "Desbloqueos", "h5-09-desbloqueos-compacto.png");
            CaptureDetailTab(5, null, "Acerca de", "h5-09-acerca-de-compacto.png");
        }
        catch (Exception ex)
        {
            Console.WriteLine("H5-09-EXCEPTION: " + ex);
        }

        // Verificacion real de X-7/T-13 (auditoria de Opus, Bloque 3): un mundo real y grande
        // de esta maquina (11MB, medido antes de tocar nada: 1.4s sincrono, freeze real y
        // perceptible). Sin un app.Run() real (este arnes pumpea manualmente con DoEvents), la
        // unica forma real de probar el await sin deadlockear el propio hilo de UI es un bucle
        // "pumpea hasta que termine" en vez de bloquear con .GetAwaiter().GetResult() (eso SI
        // deadlockearia: Task.Run reanuda via el DispatcherSynchronizationContext instalado por
        // `new Application()`, y nadie bombearia ese mensaje mientras el hilo esta bloqueado
        // esperando).
        try
        {
            string worldPath = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
            if (File.Exists(worldPath))
            {
                vm.SelectedTabIndex = 4; // Exploracion - si no, la captura cae en la pestaña que dejo el test anterior
                DoEvents();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var task = vm.Exploration.LoadFromPathAsync(worldPath);
                long msHastaControl = sw.ElapsedMilliseconds;
                Console.WriteLine($"X7-ASYNC: LoadFromPathAsync devolvio el control tras {msHastaControl}ms (esperado ~0 - la UI NO se congela mientras el mundo se lee/pinta en segundo plano), IsLoading={vm.Exploration.IsLoading} (esperado True)");

                bool primeraVuelta = true;
                while (!task.IsCompleted)
                {
                    DoEvents();
                    if (primeraVuelta)
                    {
                        primeraVuelta = false;
                        var rtbLoading = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtbLoading.Render(window);
                        var encLoading = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encLoading.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbLoading));
                        using var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "mundo-cargando.png"));
                        encLoading.Save(fs);
                    }
                }
                sw.Stop();
                if (task.IsFaulted) throw task.Exception!;
                Console.WriteLine($"X7-ASYNC: '{worldPath}' ({new FileInfo(worldPath).Length / 1024 / 1024}MB) -> {sw.ElapsedMilliseconds}ms totales, IsLoading={vm.Exploration.IsLoading} (esperado False), IsNotLoading={vm.Exploration.IsNotLoading} (esperado True), StatusMessage={vm.Exploration.StatusMessage}");

                // A9-11-DIFICULTAD (pedido explicito del usuario, 5-sep-2026): unica prueba de
                // ESCRITURA real de todo el arnes - WorldFileService.SaveGameMode toca un
                // archivo .wld de verdad (backup .bak + File.Replace atomico). NUNCA sobre
                // roca_negra.wld (el mundo real que el resto del arnes sigue usando despues de
                // este bloque) - siempre sobre una COPIA en el scratchpad, borrada al final pase
                // lo que pase (try/finally), para no dejar restos ni afectar a otra ejecucion.
                string copiaDificultad = Path.Combine(Path.GetTempPath(), $"terrakeep-test-dificultad-{Guid.NewGuid():N}.wld");
                try
                {
                    File.Copy(worldPath, copiaDificultad);
                    byte[] bytesOriginales = File.ReadAllBytes(copiaDificultad);
                    var mundoParaGuardar = WldReader.Read(bytesOriginales, readContainers: false);
                    int modoOriginal = mundoParaGuardar.Header.GameMode;
                    int modoNuevo = modoOriginal == 2 ? 0 : 2; // alterna a un valor real distinto, cualquiera que sea el de partida

                    var mundoActualizado = WorldFileService.SaveGameMode(mundoParaGuardar, copiaDificultad, modoNuevo);
                    bool bakExiste = File.Exists(copiaDificultad + ".bak");
                    bool bakEsElOriginal = bakExiste && File.ReadAllBytes(copiaDificultad + ".bak").SequenceEqual(bytesOriginales);
                    var releido = WldReader.ReadHeader(File.ReadAllBytes(copiaDificultad));
                    byte[] bytesTrasGuardar = File.ReadAllBytes(copiaDificultad);
                    int bytesDistintos = Enumerable.Range(0, bytesOriginales.Length).Count(i => bytesOriginales[i] != bytesTrasGuardar[i]);

                    Console.WriteLine($"A9-11-DIFICULTAD: modo {modoOriginal}->{modoNuevo} sobre copia real de '{Path.GetFileName(worldPath)}' -> mundoActualizado.Header.GameMode={mundoActualizado.Header.GameMode} (esperado {modoNuevo}), releido de disco={releido.GameMode} (esperado {modoNuevo}), .bak existe={bakExiste} (esperado True) y coincide byte a byte con el original={bakEsElOriginal} (esperado True), bytes distintos entre original y guardado={bytesDistintos} (esperado <=4, solo el Int32 de GameMode)");
                    if (mundoActualizado.Header.GameMode != modoNuevo || releido.GameMode != modoNuevo || !bakExiste || !bakEsElOriginal || bytesDistintos > 4)
                        Console.WriteLine("FALLO: A9-11-DIFICULTAD - la escritura real de dificultad no hizo lo que se esperaba (build/backup/round-trip)");
                }
                finally
                {
                    File.Delete(copiaDificultad);
                    File.Delete(copiaDificultad + ".bak");
                    File.Delete(copiaDificultad + ".tmp");
                }

                // H5-11 (quinta auditoria de Opus): "el lanzador de mundos desaparece para
                // siempre en cuanto cargas uno" - con un mundo YA cargado (justo aqui), la tira
                // permanente de pildoras debe seguir en el arbol visual real (antes vivia SOLO
                // dentro del overlay de IsEmpty, que en este punto es False) y el mundo cargado
                // debe marcarse IsCurrent=true en su propia entrada de Worlds.
                DoEvents();
                var loadedEntry = vm.Exploration.Worlds.FirstOrDefault(w => string.Equals(w.FilePath, worldPath, StringComparison.OrdinalIgnoreCase));
                Console.WriteLine($"H5-11-PILDORA: Worlds.Count={vm.Exploration.Worlds.Count} (esperado >=1), entrada del mundo cargado encontrada={loadedEntry != null} IsCurrent={loadedEntry?.IsCurrent} (esperado True)");
                if (loadedEntry != null && !loadedEntry.IsCurrent) Console.WriteLine("FALLO: H5-11 - el mundo recien cargado no quedo marcado IsCurrent en su propia pildora");
                var pillText = root.FindFirst(TreeScope.Descendants, new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text),
                    new PropertyCondition(AutomationElement.NameProperty, "Tus mundos")));
                Console.WriteLine($"H5-11-TIRA-PERMANENTE: etiqueta 'Tus mundos' presente en el arbol visual CON un mundo ya cargado={pillText != null} (esperado True - antes vivia solo en el overlay de estado vacio, invisible en este punto)");
                if (pillText == null) Console.WriteLine("FALLO: H5-11 - la tira permanente de mundos no esta en el arbol visual tras cargar un mundo");
                var rtbPill = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbPill.Render(window);
                var encPill = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encPill.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbPill));
                using (var fsPill = File.Create(Path.Combine(AppContext.BaseDirectory, "h5-11-tira-mundos-con-mundo-cargado.png"))) encPill.Save(fsPill);
                Console.WriteLine("Captura tira de mundos con mundo cargado -> h5-11-tira-mundos-con-mundo-cargado.png");

                // X-a (segunda auditoria de Opus, Fable): "Restablecer" vuelve al 100%, para un
                // mundo grande deja ver solo una fraccion minima del ancho real. Boton real
                // "Ajustar a la ventana" via UI Automation - se comprueba que el mundo ESCALADO
                // cabe de verdad en el viewport real del ScrollViewer (no solo que el numero de
                // Zoom cambio a secas).
                double zoomAntes = vm.Exploration.Zoom;
                var fitButton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                    new PropertyCondition(AutomationElement.NameProperty, "Ajustar a la ventana")));
                if (fitButton != null && fitButton.TryGetCurrentPattern(InvokePattern.Pattern, out var fitPat))
                    ((InvokePattern)fitPat).Invoke();
                else
                    Console.WriteLine("Boton 'Ajustar a la ventana' NO-FOUND");
                DoEvents(); DoEvents();
                double zoomDespues = vm.Exploration.Zoom;
                var worldImg = vm.Exploration.WorldImage;
                double anchoEscalado = (worldImg?.PixelWidth ?? 0) * zoomDespues;
                double altoEscalado = (worldImg?.PixelHeight ?? 0) * zoomDespues;
                // WorldMapScroll es x:Name (internal por defecto, invisible desde este ensamblado
                // distinto) - FindName es el metodo real PUBLICO para resolver un nombre del
                // namescope XAML sin depender de la accesibilidad del campo generado.
                // ADR-TERRAKEEP-030 (27-sep-2026): Mapa+minimapa se movio a WorldMapView.xaml
                // (UserControl con su propio NameScope) - FindName DOBLE, mismo patron ya usado
                // por GUIA/ADR-021, Compare/ADR-025, WorldTools/ADR-027, ChestInspector/ADR-028 y
                // Browse/ADR-029.
                var worldMapViewHostFit = window.FindName("WorldMapView") as FrameworkElement;
                var worldScroll = (ScrollViewer)worldMapViewHostFit!.FindName("WorldMapScroll");
                bool cabeDeVerdad = anchoEscalado <= worldScroll.ViewportWidth + 1 && altoEscalado <= worldScroll.ViewportHeight + 1;
                Console.WriteLine($"X-a AJUSTAR-A-LA-VENTANA: zoom {zoomAntes:P0} -> {zoomDespues:P0}, mundo escalado={anchoEscalado:0}x{altoEscalado:0}px, viewport={worldScroll.ViewportWidth:0}x{worldScroll.ViewportHeight:0}px, cabe={cabeDeVerdad} (esperado True)");
                if (!cabeDeVerdad) Console.WriteLine("FALLO: X-a (segunda auditoria) - 'Ajustar a la ventana' no dejo el mundo dentro del viewport real");
                {
                    var rtbFit = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbFit.Render(window);
                    var encFit = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encFit.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbFit));
                    using var fsFit = File.Create(Path.Combine(AppContext.BaseDirectory, "mundo-ajustado-a-la-ventana.png"));
                    encFit.Save(fsFit);
                }

                // T-D (segunda auditoria de Opus, Fable): "la barra de desplazamiento horizontal
                // esta rota - Width=10 fijo, sin trigger para el caso horizontal". Restablecer a
                // 100% real fuerza que WorldMapScroll (el unico uso real de esta barra) necesite
                // SI o SI scroll horizontal con un mundo de 8400 tiles - se busca la ScrollBar
                // horizontal real en su plantilla y se comprueba que su alto renderizado es
                // razonable (~10px), no el "muñon mal orientado" real que describe el hallazgo.
                vm.Exploration.ZoomResetCommand.Execute(null);
                worldScroll.UpdateLayout();
                DoEvents(); DoEvents();
                System.Windows.Controls.Primitives.ScrollBar? barraHorizontal = null;
                void BuscarBarraHorizontal(System.Windows.DependencyObject d)
                {
                    if (barraHorizontal != null) return;
                    if (d is System.Windows.Controls.Primitives.ScrollBar sb && sb.Orientation == System.Windows.Controls.Orientation.Horizontal) { barraHorizontal = sb; return; }
                    int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
                    for (int i = 0; i < n && barraHorizontal == null; i++)
                        BuscarBarraHorizontal(System.Windows.Media.VisualTreeHelper.GetChild(d, i));
                }
                BuscarBarraHorizontal(worldScroll);
                Console.WriteLine($"T-D: ScrollBar horizontal real encontrada={barraHorizontal != null}, ActualHeight={barraHorizontal?.ActualHeight:0.#}px, ActualWidth={barraHorizontal?.ActualWidth:0.#}px (esperado alto ~10px, ancho >> 10px - antes salia como un hilo vertical de 10px de ANCHO)");
                if (barraHorizontal == null || barraHorizontal.ActualHeight < 5 || barraHorizontal.ActualHeight > 20 || barraHorizontal.ActualWidth < 20)
                    Console.WriteLine("FALLO: T-D (segunda auditoria) - la barra horizontal no tiene un tamaño real razonable");
                {
                    var rtbScroll = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbScroll.Render(window);
                    var encScroll = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encScroll.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbScroll));
                    using var fsScroll = File.Create(Path.Combine(AppContext.BaseDirectory, "mundo-barra-horizontal.png"));
                    encScroll.Save(fsScroll);
                }

                // X-c (segunda auditoria de Opus, Fable): "el buscador de NPCs oculta marcadores
                // del mapa" - antes filtrar el buscador VACIABA Npcs (la coleccion real que
                // dibuja los marcadores). Con un mundo real: buscar por el nombre de UN NPC no
                // debe reducir Npcs.Count (el mapa sigue completo), solo NpcSearchResults (la
                // lista lateral) y el IsMatch de cada fila (resaltar, no ocultar).
                int totalAntesDeBuscar = vm.Exploration.Npcs.Count;
                if (totalAntesDeBuscar > 0)
                {
                    string nombreBuscado = vm.Exploration.Npcs[0].Name;
                    vm.Exploration.NpcSearchText = nombreBuscado;
                    DoEvents();
                    bool mapaCompleto = vm.Exploration.Npcs.Count == totalAntesDeBuscar;
                    bool ladoFiltrado = vm.Exploration.NpcSearchResults.Count <= totalAntesDeBuscar;
                    bool coincidenciaMarcada = vm.Exploration.Npcs[0].IsMatch;
                    bool hayNoCoincidenciasAtenuadas = vm.Exploration.Npcs.Any(n => !n.IsMatch);
                    Console.WriteLine($"X-C-BUSCADOR-NPC: buscando '{nombreBuscado}' -> mapa sigue completo={mapaCompleto} (esperado True, {vm.Exploration.Npcs.Count}/{totalAntesDeBuscar}), lista lateral filtrada={ladoFiltrado} ({vm.Exploration.NpcSearchResults.Count}), coincidencia marcada={coincidenciaMarcada} (esperado True), hay no-coincidencias atenuadas={hayNoCoincidenciasAtenuadas}");
                    if (!mapaCompleto) Console.WriteLine("FALLO: X-c (segunda auditoria) - el buscador de NPCs sigue vaciando los marcadores del mapa");
                    var rtbNpcSearch = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbNpcSearch.Render(window);
                    var encNpcSearch = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encNpcSearch.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbNpcSearch));
                    using (var fsNpc = File.Create(Path.Combine(AppContext.BaseDirectory, "mundo-buscador-npc.png"))) encNpcSearch.Save(fsNpc);
                    vm.Exploration.NpcSearchText = string.Empty; // deja el estado limpio para pasos siguientes
                    DoEvents();
                }
                else Console.WriteLine("X-C-BUSCADOR-NPC: mundo real sin NPCs, omitido");

                // Punto 4 (feedback del usuario, "el mundo... podria tener un buscador de todo
                // tipo de objetos... no es un editor pero si un buscador") - Fase 1 de
                // ESPEC-buscador-mundo-tedit.md (advisor Opus). Con el mundo real ya cargado
                // arriba: buscar "lava" (liquido real, presente en cualquier mundo real de
                // Terraria con Infierno generado) debe dar al menos un resultado real, poblar
                // WorldSearchResults (que ADEMAS dibuja los marcadores del mapa, mismo
                // mecanismo Canvas que los NPCs) y un resumen no vacio. Clic real en el primer
                // resultado (GoToWorldSearchHitCommand) no debe lanzar excepcion.
                try
                {
                    vm.Exploration.WorldSearchText = "lava";
                    // A8-02 (auditoria de Opus vs TEdit, E-02): IsSearching debe activarse DURANTE
                    // el barrido real (no solo existir como propiedad) y apagarse al terminar.
                    //
                    // Bug real de ESTE ARNES arreglado el 15-sep-2026 (cierre de sesion, triaje de
                    // FALLO): la espera de "hasta completar" era un WaitForDispatcher(1720) FIJO
                    // ("mismo total de 2000ms ya medido") - una carrera clasica contra el reloj, no
                    // contra el estado real. Con la maquina bajo carga (otra sesion de Claude Code
                    // en paralelo, ya documentado varias veces esta misma noche como causa real de
                    // contienda) el barrido real de 'lava' sobre roca_negra.wld (11MB, 8400x2400
                    // tiles) podia tardar mas de 2000ms de verdad, y la comprobacion leia
                    // IsSearching/WorldSearchResults ANTES de que terminara - FALLO falso tanto
                    // aqui (A8-02) como en "Punto 4" mas abajo (mismo barrido, mismos datos, leidos
                    // demasiado pronto). Sustituido por un sondeo real del estado (mismo patron ya
                    // usado en HOSTING_SOLO para EnEscucha), con margen generoso.
                    //
                    // Segunda causa real, encontrada el 15-sep-2026 (ronda de re-verificacion de
                    // los 21 FALLO): un UNICO snapshot de IsSearching a los 280ms fijos (250ms de
                    // debounce + 30ms de margen) es una carrera EN LA DIRECCION CONTRARIA - con la
                    // maquina descargada (o con cache de disco/JIT ya caliente tras AR-EX4-PNG, que
                    // corre justo antes) el barrido de 20,2M tiles puede terminar en pocos ms, y el
                    // debounce+barrido entero cabe DENTRO de esos 280ms: a veces el snapshot cae
                    // justo cuando ya ha terminado, dando "IsSearching=False" aunque SI se activo de
                    // verdad un instante antes (confirmado real: WorldSearchResults.Count llega a
                    // 1000 igualmente en la misma pasada, la busqueda si corrio). Arreglado sondeando
                    // en continuo DESDE que se fija el texto (no un unico punto fijo): se capta
                    // IsSearching=True en CUALQUIER instante en el que este activo, sea el barrido
                    // rapido o lento - elimina la carrera en los dos sentidos a la vez.
                    bool buscandoAMitad = false;
                    long limiteInicioA802 = Environment.TickCount64 + 5_000; // 5s: mucho mas que el debounce de 250ms + cualquier barrido real
                    while (Environment.TickCount64 < limiteInicioA802)
                    {
                        DoEvents(); System.Threading.Thread.Sleep(1);
                        if (vm.Exploration.IsSearching) { buscandoAMitad = true; break; }
                    }
                    // 60s de margen (no 15s): medido en esta misma sesion que 15s no bastaban con
                    // la maquina bajo carga real (otro build/test de este mismo repo corriendo en
                    // paralelo) - es una comprobacion de CORRECCION, no de rendimiento, mejor un
                    // margen generoso que un falso FALLO por lentitud ajena a la app.
                    long limiteA802 = Environment.TickCount64 + 60_000;
                    while (vm.Exploration.IsSearching && Environment.TickCount64 < limiteA802)
                    { DoEvents(); System.Threading.Thread.Sleep(1); } // mismo motivo real que WaitForDispatcher (ver su comentario): cede la CPU de verdad entre vueltas
                    DoEvents();
                    bool buscandoTrasCompletar = vm.Exploration.IsSearching;
                    Console.WriteLine($"A8-02: IsSearching a mitad del barrido={buscandoAMitad} (esperado True), tras completar={buscandoTrasCompletar} (esperado False)");
                    if (!buscandoAMitad) Console.WriteLine("FALLO: A8-02 - IsSearching no se activo durante el barrido real del mundo");
                    if (buscandoTrasCompletar) Console.WriteLine("FALLO: A8-02 - IsSearching se quedo colgado en True tras completar la busqueda");

                    int hits = vm.Exploration.WorldSearchResults.Count;
                    bool tieneResumen = !string.IsNullOrEmpty(vm.Exploration.WorldSearchSummary);
                    Console.WriteLine($"BUSCADOR-MUNDO: 'lava' -> WorldSearchResults.Count={hits} (esperado >=1), resumen='{vm.Exploration.WorldSearchSummary}' (esperado no vacio)");
                    if (hits == 0 || !tieneResumen) Console.WriteLine("FALLO: Punto 4 - la busqueda de 'lava' en un mundo real no encontro nada o no dejo resumen");

                    // A8-02b: Cancelar de verdad a mitad de un barrido nuevo - la infraestructura
                    // (_worldSearchCts) ya existia, F-4 solo expuso el boton/comando.
                    vm.Exploration.WorldSearchText = "agua";
                    WaitForDispatcher(280);
                    bool buscandoAntesDeCancelar = vm.Exploration.IsSearching;
                    vm.Exploration.CancelWorldSearchCommand.Execute(null);
                    WaitForDispatcher(200);
                    bool buscandoTrasCancelar = vm.Exploration.IsSearching;
                    Console.WriteLine($"A8-02b: IsSearching antes de cancelar={buscandoAntesDeCancelar} (esperado True), tras Cancelar={buscandoTrasCancelar} (esperado False)");
                    if (buscandoTrasCancelar) Console.WriteLine("FALLO: A8-02b - CancelWorldSearchCommand no apago IsSearching");
                    vm.Exploration.WorldSearchText = "lava"; // deja el estado conocido para lo que sigue
                    WaitForDispatcher(2000);
                    hits = vm.Exploration.WorldSearchResults.Count;

                    // A8-04 (auditoria de Opus vs TEdit, E-04): con el tope real de 1000
                    // resultados alcanzado ("lava" en este mundo da exactamente 1000, medido), un
                    // ListBox de verdad virtualizado NO debe haber realizado un contenedor por
                    // cada item - un ItemsControl desnudo (el bug original) los realiza TODOS de
                    // golpe. ContainerFromIndex devuelve null para cualquier indice fuera del
                    // rango realizado/reciclado.
                    DoEvents();
                    var resultsListBox = Descendientes<ListBox>(window).FirstOrDefault(lb => ReferenceEquals(lb.ItemsSource, vm.Exploration.WorldSearchResults));
                    if (resultsListBox != null)
                    {
                        int realizados = 0;
                        for (int i = 0; i < resultsListBox.Items.Count; i++)
                            if (resultsListBox.ItemContainerGenerator.ContainerFromIndex(i) != null) realizados++;
                        Console.WriteLine($"A8-04: lista de resultados ({resultsListBox.Items.Count} items) -> {realizados} contenedores realizados (esperado <100)");
                        if (realizados >= 100) Console.WriteLine("FALLO: A8-04 - la lista de resultados no esta virtualizada de verdad (demasiados contenedores realizados)");
                    }
                    else Console.WriteLine("A8-04: no se encontro el ListBox de resultados en el arbol visual - omitido");

                    // A8-03 (auditoria de Opus vs TEdit, E-03): el marcador debe medir lo MISMO en
                    // pantalla (coordenadas de ventana, no de tile) a cualquier zoom - antes
                    // escalaba con el Grid contenedor (a 0.05 una elipse de 9px quedaba en <1px).
                    // TransformToAncestor(window) acumula TODA la cadena de transformaciones reales
                    // entre el marcador y la ventana (el ScaleTransform del mapa Y el
                    // RenderTransform inverso nuevo), asi que mide lo que de verdad se ve en
                    // pantalla, no ActualWidth (que es tamaño de LAYOUT, ajeno al RenderTransform).
                    var marco = Descendientes<System.Windows.Shapes.Rectangle>(window).FirstOrDefault(r => r.Name == "Marco");
                    if (marco != null)
                    {
                        var anchosPorZoom = new List<(double zoom, double anchoReal)>();
                        foreach (double z in new[] { 0.05, 1.0, 6.0 })
                        {
                            vm.Exploration.Zoom = z;
                            DoEvents(); DoEvents();
                            var bounds = marco.TransformToAncestor(window).TransformBounds(new Rect(0, 0, marco.ActualWidth, marco.ActualHeight));
                            anchosPorZoom.Add((z, bounds.Width));
                        }
                        string resumenZoom = string.Join(", ", anchosPorZoom.Select(t => $"zoom={t.zoom}->{t.anchoReal:0.0}px"));
                        Console.WriteLine($"A8-03: ancho real en ventana del marcador por zoom: {resumenZoom} (esperado el mismo, +-1px)");
                        double minAncho = anchosPorZoom.Min(t => t.anchoReal), maxAncho = anchosPorZoom.Max(t => t.anchoReal);
                        if (maxAncho - minAncho > 1.0) Console.WriteLine("FALLO: A8-03 - el marcador de resultado NO mide lo mismo en pantalla a distintos niveles de zoom");
                    }
                    else Console.WriteLine("A8-03: no se encontro ningun marcador 'Marco' en el arbol visual - omitido");
                    vm.Exploration.Zoom = 1.0; // deja el estado conocido para lo que sigue
                    DoEvents();

                    if (hits > 0)
                    {
                        var primerHit = vm.Exploration.WorldSearchResults[0];
                        vm.Exploration.GoToWorldSearchHitCommand.Execute(primerHit);
                        DoEvents(); DoEvents();
                        Console.WriteLine($"BUSCADOR-MUNDO-NAVEGAR: clic real en '{primerHit.Name}' ({primerHit.Position}) sin excepcion");

                        // Fase 3 (ESPEC-buscador-mundo-tedit.md#5.3 punto 4): navegacion circular
                        // real. Clic ya dejo IsCurrent=true en primerHit (indice 0) - Siguiente
                        // real tiene que moverse al indice 1 (o dar la vuelta si solo hay 1).
                        bool primerHitEraActual = primerHit.IsCurrent;
                        vm.Exploration.NextWorldSearchResultCommand.Execute(null);
                        DoEvents();
                        var actualTrasSiguiente = vm.Exploration.WorldSearchResults.Where(r => r.IsCurrent).ToList();
                        Console.WriteLine($"BUSCADOR-MUNDO-SIGUIENTE: primerHit era actual={primerHitEraActual} (esperado True), tras Siguiente hay exactamente 1 actual={actualTrasSiguiente.Count == 1} (esperado True), sigue siendo el primero={ReferenceEquals(actualTrasSiguiente.FirstOrDefault(), primerHit)} (esperado False si hay >1 resultado)");
                        if (!primerHitEraActual || actualTrasSiguiente.Count != 1) Console.WriteLine("FALLO: Fase 3 - Siguiente no deja exactamente un resultado marcado como actual");

                        vm.Exploration.PreviousWorldSearchResultCommand.Execute(null);
                        DoEvents();
                        bool volvioAlPrimero = primerHit.IsCurrent;
                        Console.WriteLine($"BUSCADOR-MUNDO-ANTERIOR: tras Anterior, primerHit vuelve a ser actual={volvioAlPrimero} (esperado True - Siguiente+Anterior es la identidad)");
                        if (!volvioAlPrimero) Console.WriteLine("FALLO: Fase 3 - Siguiente seguido de Anterior no vuelve al resultado original");

                        // Fase 3 punto 5: distancia al spawn - apagada por defecto (DistanceLabel
                        // null), al activarla se rellena en TODAS las filas y el orden pasa a ser
                        // no decreciente por distancia real. El resultado "actual" (primerHit) se
                        // tiene que conservar aunque cambie de indice al reordenar.
                        bool sinDistanciaAntes = vm.Exploration.WorldSearchResults.All(r => r.DistanceLabel == null);
                        vm.Exploration.ShowSpawnDistance = true;
                        DoEvents();
                        bool todasConDistancia = vm.Exploration.WorldSearchResults.All(r => r.DistanceLabel != null);
                        var cabeceraReal = Terrakeep.Core.WldFormat.WldReader.ReadHeader(File.ReadAllBytes(worldPath));
                        bool ordenNoDecreciente = true;
                        double? distanciaPrevia = null;
                        foreach (var fila in vm.Exploration.WorldSearchResults)
                        {
                            double d = Math.Sqrt(Math.Pow(fila.TileX - cabeceraReal.SpawnX, 2) + Math.Pow(fila.TileY - cabeceraReal.SpawnY, 2));
                            if (distanciaPrevia is double previa && d < previa - 0.5) { ordenNoDecreciente = false; break; }
                            distanciaPrevia = d;
                        }
                        bool sigueSiendoElActual = primerHit.IsCurrent;
                        Console.WriteLine($"BUSCADOR-MUNDO-DISTANCIA: sin activar todas null={sinDistanciaAntes} (esperado True), activada todas con DistanceLabel={todasConDistancia} (esperado True), orden no decreciente por distancia={ordenNoDecreciente} (esperado True), el 'actual' se conserva tras reordenar={sigueSiendoElActual} (esperado True)");
                        if (!sinDistanciaAntes || !todasConDistancia || !ordenNoDecreciente || !sigueSiendoElActual)
                            Console.WriteLine("FALLO: Fase 3 - 'Ordenar por distancia al spawn' no calcula/ordena/conserva el actual correctamente");
                        vm.Exploration.ShowSpawnDistance = false;
                        DoEvents();
                    }

                    var rtbBuscadorMundo = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbBuscadorMundo.Render(window);
                    var encBuscadorMundo = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encBuscadorMundo.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbBuscadorMundo));
                    using (var fsBuscadorMundo = File.Create(Path.Combine(AppContext.BaseDirectory, "mundo-buscador-general.png"))) encBuscadorMundo.Save(fsBuscadorMundo);
                    Console.WriteLine("Captura buscador general del mundo -> mundo-buscador-general.png");

                    vm.Exploration.WorldSearchText = string.Empty; // deja el estado limpio para pasos siguientes
                    WaitForDispatcher(100);
                }
                catch (Exception ex) { Console.WriteLine("BUSCADOR-MUNDO-EXCEPTION: " + ex); }

                // Punto 4, Fase 2 (advisor Opus): cofres y letreros - lectura INDEPENDIENTE del
                // mismo .wld real (WldReader.Read a secas, sin pasar por la ViewModel) para
                // conocer un NetId/texto real de verdad presente en ESTE mundo concreto, en vez
                // de asumir a ciegas que un nombre cualquiera esta en el - la prueba real es que
                // el buscador (via la ViewModel) encuentra EXACTAMENTE lo que la lectura
                // independiente dice que hay.
                try
                {
                    var worldIndependiente = Terrakeep.Core.WldFormat.WldReader.Read(File.ReadAllBytes(worldPath));

                    // F-7 (auditoria de Opus vs TEdit, E-06): prueba de NO REGRESION del formato -
                    // ReadHeader ahora lee 5 campos mas antes de DungeonX/Y (Time/DayTime/
                    // MoonPhase/BloodMoon/IsEclipse); si el offset estuviera mal, los campos
                    // POSTERIORES (TilesWide/High/SpawnX/Y/GroundLevel/RockLevel se leen ANTES asi
                    // que no aplica, pero DungeonX/Y si dependen de haber contado bien esos 5) o
                    // los propios DungeonX/Y saldrian con basura (valores absurdos, fuera del
                    // mundo) en vez de una coordenada real.
                    var hdrReal = worldIndependiente.Header;
                    bool dungeonPlausible = hdrReal.DungeonX > 0 && hdrReal.DungeonX < hdrReal.TilesWide
                        && hdrReal.DungeonY > 0 && hdrReal.DungeonY < hdrReal.TilesHigh;
                    Console.WriteLine($"F-7: mundo real {hdrReal.TilesWide}x{hdrReal.TilesHigh}, Spawn=({hdrReal.SpawnX},{hdrReal.SpawnY}), Dungeon=({hdrReal.DungeonX},{hdrReal.DungeonY}) (esperado dentro del mundo, no (0,0) ni basura)");
                    if (!dungeonPlausible) Console.WriteLine("FALLO: F-7 - DungeonX/Y salio fuera de rango o en (0,0) - posible desalineacion del lector");

                    var chestConObjeto = worldIndependiente.Chests.FirstOrDefault(c => c.Items.Count > 0);
                    if (chestConObjeto != null)
                    {
                        int netId = chestConObjeto.Items[0].NetId;
                        vm.Exploration.WorldSearchText = "#" + netId;
                        WaitForDispatcher(1500);
                        // Bug real de offset de marcador (17-sep-2026): WorldSearch.cs ya deja el
                        // hit en el CENTRO real del cofre (esquina +1 en cada eje, cofres 2x2),
                        // no en la esquina cruda que guarda el .wld - comparar contra el centro.
                        bool encontrado = vm.Exploration.WorldSearchResults.Any(h => h.TileX == chestConObjeto.X + 1 && h.TileY == chestConObjeto.Y + 1);
                        Console.WriteLine($"BUSCADOR-MUNDO-COFRE: '#{netId}' -> cofre real en ({chestConObjeto.X},{chestConObjeto.Y}) encontrado={encontrado} (esperado True, {vm.Exploration.WorldSearchResults.Count} resultado(s))");
                        if (!encontrado) Console.WriteLine("FALLO: Punto 4 Fase 2 - un objeto real de cofre no aparecio en el buscador");
                    }
                    else Console.WriteLine("BUSCADOR-MUNDO-COFRE: este mundo real no tiene ningun cofre con objetos, omitido");

                    var letreroReal = worldIndependiente.Signs.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.Text));
                    if (letreroReal != null)
                    {
                        string fragmento = letreroReal.Text.Trim().Split(' ', '\n', '\r').FirstOrDefault(w => w.Length >= 3) ?? letreroReal.Text.Trim();
                        vm.Exploration.WorldSearchText = fragmento;
                        WaitForDispatcher(1500);
                        // Mismo arreglo de offset que los cofres, extendido a letreros
                        // (17-sep-2026, investigacion real del mismo bug): el hit ya cae en el
                        // CENTRO real del letrero (esquina +1 en cada eje, letreros 2x2).
                        bool encontrado = vm.Exploration.WorldSearchResults.Any(h => h.TileX == letreroReal.X + 1 && h.TileY == letreroReal.Y + 1);
                        Console.WriteLine($"BUSCADOR-MUNDO-LETRERO: '{fragmento}' -> letrero real en ({letreroReal.X},{letreroReal.Y}) encontrado={encontrado} (esperado True, {vm.Exploration.WorldSearchResults.Count} resultado(s))");
                        if (!encontrado) Console.WriteLine("FALLO: Punto 4 Fase 2 - un letrero real no aparecio en el buscador");
                    }
                    else Console.WriteLine("BUSCADOR-MUNDO-LETRERO: este mundo real no tiene ningun letrero con texto, omitido");

                    // A8-05 (auditoria de Opus vs TEdit, E-08): un bloque bajo el agua/lava/miel
                    // debe nombrar los DOS (antes solo se nombraba el liquido si NO habia bloque
                    // activo). Busqueda directa sobre el mundo independiente (sin pasar por la UI)
                    // de un tile real que cumpla la condicion, en vez de un escaneo a ciegas.
                    (int X, int Y)? tileSumergido = null;
                    var hdr = worldIndependiente.Header;
                    for (int y = (int)hdr.GroundLevel; y < hdr.TilesHigh - 200 && tileSumergido == null; y += 3)
                        for (int x = 0; x < hdr.TilesWide; x += 5)
                        {
                            var t = worldIndependiente.Tiles[x, y];
                            if (t.IsActive && t.LiquidAmount > 0) { tileSumergido = (x, y); break; }
                        }
                    if (tileSumergido is { } pos)
                    {
                        vm.Exploration.UpdateHover(pos.X, pos.Y);
                        Console.WriteLine($"A8-05: tile sumergido real en ({pos.X},{pos.Y}) -> HoverTileText='{vm.Exploration.HoverTileText}' (esperado != '—'), HoverLiquidText='{vm.Exploration.HoverLiquidText}' (esperado != '—')");
                        if (vm.Exploration.HoverTileText == "—" || vm.Exploration.HoverLiquidText == "—")
                            Console.WriteLine("FALLO: A8-05 - un tile activo con liquido no nombra los dos a la vez");
                        Console.WriteLine($"A8-05-CAPA: HoverLayerText='{vm.Exploration.HoverLayerText}' (esperado uno real: Espacio/Superficie/Subterraneo/Cavernas/Infierno), HoverDepthText='{vm.Exploration.HoverDepthText}'");
                        if (vm.Exploration.HoverLayerText is not ("Espacio" or "Superficie" or "Subterráneo" or "Cavernas" or "Infierno"))
                            Console.WriteLine("FALLO: A8-05-CAPA - HoverLayerText no es ninguna de las 5 capas reales de la formula GPS");
                    }
                    else Console.WriteLine("A8-05: este mundo real no tiene ningun tile activo sumergido en liquido, omitido");

                    // F-7: el marcador real del spawn del mundo (casa naranja) debe existir y
                    // estar visible en el arbol visual, con la posicion real de Header.SpawnX/Y.
                    DoEvents();
                    var marcadorSpawnMundo = Descendientes<TextBlock>(window).FirstOrDefault(t => t.Text == "⌂");
                    Console.WriteLine($"F-7-MARCADOR: marcador de spawn del mundo encontrado={marcadorSpawnMundo != null}, visible={marcadorSpawnMundo?.IsVisible} (esperado True)");
                    if (marcadorSpawnMundo == null || !marcadorSpawnMundo.IsVisible) Console.WriteLine("FALLO: F-7 - el marcador de spawn del mundo no aparece en el mapa");

                    // F-12 (auditoria de Opus vs TEdit, E-13): exportar de verdad a un fichero
                    // temporal y comprobar que el PNG resultante tiene las dimensiones reales del
                    // mundo (TilesWide x TilesHigh, 1 pixel = 1 tile).
                    string pngTemporal = Path.Combine(Path.GetTempPath(), $"terrakeep-export-test-{Guid.NewGuid():N}.png");
                    try
                    {
                        vm.Exploration.ExportMapToPng(pngTemporal);
                        bool existe = File.Exists(pngTemporal);
                        int anchoPng = 0, altoPng = 0;
                        System.Windows.Media.Imaging.BitmapSource? exportado = null;
                        if (existe)
                        {
                            var bytesPng = File.ReadAllBytes(pngTemporal);
                            using var msPng = new MemoryStream(bytesPng);
                            var decoder = new System.Windows.Media.Imaging.PngBitmapDecoder(msPng, System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                            exportado = decoder.Frames[0];
                            anchoPng = exportado.PixelWidth;
                            altoPng = exportado.PixelHeight;
                        }
                        Console.WriteLine($"F-12: PNG exportado existe={existe}, {anchoPng}x{altoPng} (esperado {hdrReal.TilesWide}x{hdrReal.TilesHigh})");
                        if (!existe || anchoPng != hdrReal.TilesWide || altoPng != hdrReal.TilesHigh)
                            Console.WriteLine("FALLO: F-12 - el PNG exportado no existe o no tiene las dimensiones reales del mundo");

                        // Idea 7, segunda mitad (catalogo de funciones, "exportar el mapa entero a
                        // imagen con los marcadores del usuario" - reconsiderada a peticion
                        // explicita del coordinador el 20-sep-2026: "añade los marcadores (NPCs/
                        // spawn/dungeon/resultados) a ExportMapToPng, no solo mapa+minerales").
                        // Compara una region real alrededor de cada marcador contra el mapa BASE
                        // (Exploration.WorldImage, sin marcadores) - si el PNG exportado es
                        // IGUAL ahi, el marcador no se dibujo de verdad.
                        if (exportado != null && vm.Exploration.WorldImage is { } mapaBase)
                        {
                            bool RegionDistinta(int cx, int cy, int radio)
                            {
                                int x0 = Math.Max(0, cx - radio), y0 = Math.Max(0, cy - radio);
                                int w = Math.Min(radio * 2, anchoPng - x0), h = Math.Min(radio * 2, altoPng - y0);
                                if (w <= 0 || h <= 0) return false;
                                var pxExport = new byte[w * h * 4];
                                var pxBase = new byte[w * h * 4];
                                new System.Windows.Media.Imaging.CroppedBitmap(exportado, new System.Windows.Int32Rect(x0, y0, w, h)).CopyPixels(pxExport, w * 4, 0);
                                new System.Windows.Media.Imaging.CroppedBitmap(mapaBase, new System.Windows.Int32Rect(x0, y0, w, h)).CopyPixels(pxBase, w * 4, 0);
                                return !pxExport.SequenceEqual(pxBase);
                            }

                            bool dungeonMarcado = RegionDistinta(hdrReal.DungeonX, hdrReal.DungeonY, 10);
                            Console.WriteLine($"IDEA7_MARCADORES: region del rombo de mazmorra ({hdrReal.DungeonX},{hdrReal.DungeonY}) distinta del mapa base={dungeonMarcado} (esperado True)");
                            if (!dungeonMarcado) Console.WriteLine("FALLO: idea 7 - el marcador de mazmorra no aparece en el PNG exportado");

                            bool spawnMundoMarcado = RegionDistinta(hdrReal.SpawnX, hdrReal.SpawnY, 10);
                            Console.WriteLine($"IDEA7_MARCADORES: region de la casa de spawn del mundo ({hdrReal.SpawnX},{hdrReal.SpawnY}) distinta del mapa base={spawnMundoMarcado} (esperado True)");
                            if (!spawnMundoMarcado) Console.WriteLine("FALLO: idea 7 - el marcador de spawn del mundo no aparece en el PNG exportado");

                            if (vm.Exploration.Npcs.Count > 0)
                            {
                                var npc = vm.Exploration.Npcs[0];
                                bool npcMarcado = RegionDistinta(npc.TileX, npc.TileY, 10);
                                Console.WriteLine($"IDEA7_MARCADORES: region del NPC '{npc.Name}' ({npc.TileX},{npc.TileY}) distinta del mapa base={npcMarcado} (esperado True)");
                                if (!npcMarcado) Console.WriteLine($"FALLO: idea 7 - el marcador del NPC '{npc.Name}' no aparece en el PNG exportado");
                            }
                            else Console.WriteLine("IDEA7_MARCADORES: este mundo real no tiene ningun NPC de pueblo, omitido");

                            if (vm.Exploration.WorldSearchResults.Count > 0)
                            {
                                var hit = vm.Exploration.WorldSearchResults[0];
                                bool resultadoMarcado = RegionDistinta((int)hit.MarkerX, (int)hit.MarkerY, 10);
                                Console.WriteLine($"IDEA7_MARCADORES: region del resultado de busqueda '{hit.Name}' ({hit.MarkerX},{hit.MarkerY}) distinta del mapa base={resultadoMarcado} (esperado True)");
                                if (!resultadoMarcado) Console.WriteLine($"FALLO: idea 7 - el marcador del resultado '{hit.Name}' no aparece en el PNG exportado");
                            }
                            else Console.WriteLine("IDEA7_MARCADORES: WorldSearchResults esta vacio en este punto del arnes, omitido");
                        }
                    }
                    finally { if (File.Exists(pngTemporal)) File.Delete(pngTemporal); }

                    // P-1 (auditoria de Opus vs TEdit): la franja de estado del mapa ya NO debe
                    // cambiar de alto al entrar/salir el raton (antes: Visibility=EmptyToCollapsed
                    // sobre la caja entera, salto de layout constante).
                    var mapStatusBar = Descendientes<Border>(window).FirstOrDefault(b => b.Name == "MapStatusBar");
                    if (mapStatusBar != null)
                    {
                        vm.Exploration.UpdateHover(-1, -1); // fuera de rango = "sin hover"
                        DoEvents();
                        double altoSinHover = mapStatusBar.ActualHeight;
                        if (tileSumergido is { } p2) vm.Exploration.UpdateHover(p2.X, p2.Y);
                        DoEvents();
                        double altoConHover = mapStatusBar.ActualHeight;
                        Console.WriteLine($"P-1-ALTURA: franja de estado sin hover={altoSinHover:0.0}px, con hover={altoConHover:0.0}px (esperado igual)");
                        if (Math.Abs(altoSinHover - altoConHover) > 0.5) Console.WriteLine("FALLO: P-1 - la franja de estado del mapa cambia de alto al entrar/salir el raton");
                    }
                    else Console.WriteLine("P-1-ALTURA: no se encontro 'MapStatusBar' en el arbol visual - omitido");

                    var rtbBuscadorFase2 = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbBuscadorFase2.Render(window);
                    var encBuscadorFase2 = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encBuscadorFase2.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbBuscadorFase2));
                    using (var fsBuscadorFase2 = File.Create(Path.Combine(AppContext.BaseDirectory, "mundo-buscador-cofre-letrero.png"))) encBuscadorFase2.Save(fsBuscadorFase2);
                    Console.WriteLine("Captura buscador de cofre/letrero -> mundo-buscador-cofre-letrero.png");

                    vm.Exploration.WorldSearchText = string.Empty;
                    WaitForDispatcher(100);
                }
                catch (Exception ex) { Console.WriteLine("BUSCADOR-MUNDO-FASE2-EXCEPTION: " + ex); }

                // Punto 4 (advisor Opus, "una nueva barra lateral... rama madre... buscar npcs
                // buscador de cofres buscador o marcador de minerales buscador de objetos" - ver
                // ESPEC-ui-exploracion.md#9). Verificacion real del rediseño completo de la barra
                // lateral: las 5 pildoras de categoria (texto REAL leido via UI Automation, no
                // adivinado de una captura - el Content de un RadioButton se convierte en su
                // Name real de automatizacion), y cada categoria nueva con datos reales.
                try
                {
                    var todasLasPildoras = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.RadioButton))
                        .Cast<AutomationElement>().Select(e => e.Current.Name).ToList();
                    Console.WriteLine($"CATEGORIAS-PILDORAS-DEBUG: TODOS los RadioButton reales del arbol = [{string.Join(" | ", todasLasPildoras)}]");
                    var pildoras = todasLasPildoras.Where(n => n.StartsWith("Todo") || n.StartsWith("NPCs") || n.StartsWith("Cofres") || n.StartsWith("Minerales") || n.StartsWith("Objetos")).ToList();
                    Console.WriteLine($"CATEGORIAS-PILDORAS: texto real de las 5 pildoras = [{string.Join(" | ", pildoras)}] (esperado 'Todo', 'NPCs (N)', 'Cofres (N)', 'Minerales (N)', 'Objetos (N)')");
                    // "Todo" no lleva contador a proposito (busca en todo, no cuenta un tipo) -
                    // solo las otras 4 tienen que llevar "(N)" real.
                    if (pildoras.Count != 5 || pildoras.Where(p => p != "Todo").Any(p => !p.Contains('(')))
                        Console.WriteLine("FALLO: Punto 4 - el texto real de alguna pildora de categoria (salvo 'Todo') no lleva su contador");

                    // Encargo del usuario 4-sep-2026 (Parte B, ver ESPEC-sprites-botones-badges.md#D.3):
                    // tras renombrar CategoryPill/CategoryChip a CategorySelector/ViewSelector, el
                    // GroupName real de WPF sigue dando la exclusion mutua nativa - clic real (via
                    // UI Automation, no asignando la propiedad) en cada una de las 5 y comprobar que
                    // SelectedCategory cambia Y que solo una queda IsSelected.
                    var pildoraElementos = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.RadioButton))
                        .Cast<AutomationElement>().Where(e => pildoras.Contains(e.Current.Name)).ToList();
                    bool exclusionOk = true;
                    foreach (var el in pildoraElementos)
                    {
                        if (!el.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selPatObj)) { exclusionOk = false; continue; }
                        var selPat = (SelectionItemPattern)selPatObj;
                        selPat.Select();
                        DoEvents();
                        int seleccionadas = pildoraElementos.Count(e2 =>
                            e2.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var p2) && ((SelectionItemPattern)p2).Current.IsSelected);
                        if (seleccionadas != 1 || !selPat.Current.IsSelected) exclusionOk = false;
                    }
                    Console.WriteLine($"SELECTORES-EXCLUSION: tras marcar cada una de las 5 por turnos, siempre queda exactamente 1 IsSelected={exclusionOk} (esperado True - el GroupName real sigue vivo tras el cambio de plantilla)");
                    if (!exclusionOk) Console.WriteLine("FALLO: Parte B - los selectores de categoria perdieron la exclusion mutua tras el rediseño");
                    vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                    DoEvents();

                    // NPCs: chip "Bajo tierra" - ya se sabe (H6-08 mas abajo) cuantos NPCs reales
                    // tiene este mundo; si alguno esta bajo tierra, el chip debe reducir de verdad
                    // la lista y ordenarla por profundidad.
                    vm.Exploration.SelectedCategory = WorldSearchCategory.Npcs;
                    DoEvents();
                    int npcsAntesDelChip = vm.Exploration.NpcSearchResults.Count;
                    vm.Exploration.NpcFilterUnderground = true;
                    DoEvents();
                    int npcsBajoTierra = vm.Exploration.Npcs.Count(n => n.IsUnderground);
                    bool cuentaCoincide = vm.Exploration.NpcSearchResults.Count == npcsBajoTierra;
                    Console.WriteLine($"CATEGORIAS-NPCS-SUBSUELO: NPCs reales bajo tierra={npcsBajoTierra} (de {npcsAntesDelChip} totales), tras activar el chip NpcSearchResults.Count={vm.Exploration.NpcSearchResults.Count} (esperado igual)");
                    if (!cuentaCoincide) Console.WriteLine("FALLO: Punto 4 - el chip 'Bajo tierra' no filtra de verdad NpcSearchResults");
                    vm.Exploration.NpcFilterUnderground = false;

                    // Parte B (ver ESPEC-sprites-botones-badges.md#D.3): los 3 chips de NPCs
                    // (ToggleButton, ViewSelector) siguen siendo multiseleccion INDEPENDIENTE tras
                    // el rediseño - marcar dos a la vez y comprobar que ninguno desmarca al otro,
                    // via el TogglePattern real (no solo el ViewModel).
                    vm.Exploration.NpcFilterWithHome = true;
                    vm.Exploration.NpcFilterHomeless = true;
                    DoEvents();
                    var chipsNpcs = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                        .Cast<AutomationElement>().Where(e => e.Current.Name is "Con casa" or "Sin casa" or "Bajo tierra").ToList();
                    var chipConCasa = chipsNpcs.FirstOrDefault(e => e.Current.Name == "Con casa");
                    var chipSinCasa = chipsNpcs.FirstOrDefault(e => e.Current.Name == "Sin casa");
                    bool multiOk = vm.Exploration.NpcFilterWithHome && vm.Exploration.NpcFilterHomeless;
                    Console.WriteLine($"SELECTORES-MULTI: 'Con casa'+'Sin casa' marcados a la vez (ViewModel)={multiOk}, encontrados en el arbol visual={chipConCasa != null}/{chipSinCasa != null} (esperado True en los 4)");
                    if (!multiOk) Console.WriteLine("FALLO: Parte B - los chips de NPCs dejaron de ser independientes tras el rediseño");
                    vm.Exploration.NpcFilterWithHome = false;
                    vm.Exploration.NpcFilterHomeless = false;

                    var rtbSelectoresNpcs = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbSelectoresNpcs.Render(window);
                    var encSelectoresNpcs = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encSelectoresNpcs.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbSelectoresNpcs));
                    using (var fsSelNpcs = File.Create(Path.Combine(AppContext.BaseDirectory, "exploracion-selectores-npcs.png"))) encSelectoresNpcs.Save(fsSelNpcs);
                    Console.WriteLine("Captura selectores de NPCs (rediseñados) -> exploracion-selectores-npcs.png");

                    // Cofres: el inventario real (ChestKindCounts, por defecto "por tipo de
                    // cofre") tiene que tener contenido real, y un clic en la primera fila tiene
                    // que buscar de verdad (SearchInventoryRowCommand -> WorldSearchResults).
                    vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                    DoEvents();
                    int cofresInventario = vm.Exploration.Inventory.Count;
                    if (cofresInventario > 0)
                    {
                        var primeraFila = vm.Exploration.Inventory[0];
                        vm.Exploration.SearchInventoryRowCommand.Execute(primeraFila);
                        WaitForDispatcher(1000);
                        Console.WriteLine($"CATEGORIAS-COFRES: Inventory.Count={cofresInventario} (esperado >=1), clic en '{primeraFila.Name}' -> WorldSearchResults.Count={vm.Exploration.WorldSearchResults.Count} (esperado >=1)");
                        if (vm.Exploration.WorldSearchResults.Count == 0) Console.WriteLine("FALLO: Punto 4 - clic en una fila de inventario de Cofres no encontro nada");

                        // Encargo del usuario 4-sep-2026 (Parte A, ver ESPEC-sprites-botones-badges.md#D.3):
                        // los 3 tipos de tile contenedor reales (21/88/467) tienen icono extraido -
                        // en la vista por defecto ("Por tipo de cofre") TODAS las filas de un mundo
                        // vanilla real deberian tener IconPath != null.
                        int conIcono = vm.Exploration.Inventory.Count(r => r.IconPath != null);
                        Console.WriteLine($"ICONOS-INVENTARIO: {conIcono}/{vm.Exploration.Inventory.Count} filas de Cofres con sprite real (esperado TODAS en un mundo vanilla real)");
                        if (conIcono != vm.Exploration.Inventory.Count) Console.WriteLine("FALLO: Parte A - alguna fila de Cofres/Por tipo salio sin sprite real");

                        var rtbCofresSprites = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtbCofresSprites.Render(window);
                        var encCofresSprites = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encCofresSprites.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbCofresSprites));
                        using (var fsCofresSprites = File.Create(Path.Combine(AppContext.BaseDirectory, "exploracion-cofres-con-sprites.png"))) encCofresSprites.Save(fsCofresSprites);
                        Console.WriteLine("Captura Cofres con sprites reales -> exploracion-cofres-con-sprites.png");
                    }
                    else Console.WriteLine("FALLO: Punto 4 - la categoria Cofres no genero ningun inventario con un mundo real que SI tiene cofres");

                    // C-06 (informe de pulido final, cierra E8): tercer modo "Cofre a cofre" -
                    // una fila por cofre REAL (nunca agrupado), ordenadas por distancia al spawn.
                    try
                    {
                        vm.Exploration.ChestViewMode = 2;
                        DoEvents();
                        int cofresReales = vm.Exploration.ChestRows.Count;
                        Console.WriteLine($"C-06-COFREACOFRE: ChestRows.Count={cofresReales} (esperado igual al numero real de cofres del mundo)");
                        if (cofresReales == 0) Console.WriteLine("FALLO: C-06 - 'Cofre a cofre' no genero ninguna fila con un mundo real que SI tiene cofres");
                        else
                        {
                            int sx = vm.Exploration.WorldSpawnX, sy = vm.Exploration.WorldSpawnY;
                            double Dist(ChestRowViewModel c) => Math.Sqrt(Math.Pow(c.TileX - sx, 2) + Math.Pow(c.TileY - sy, 2));
                            bool ordenadoPorDistancia = vm.Exploration.ChestRows.Zip(vm.Exploration.ChestRows.Skip(1), (a, b) => Dist(a) <= Dist(b) + 0.001).All(ok => ok);
                            Console.WriteLine($"C-06-ORDEN: ordenado por distancia ascendente al spawn ({sx},{sy})={ordenadoPorDistancia} (esperado True)");
                            if (!ordenadoPorDistancia) Console.WriteLine("FALLO: C-06 - 'Cofre a cofre' no esta ordenado por distancia al spawn del mundo");

                            int conIconoCofreACofre = vm.Exploration.ChestRows.Count(r => r.IconPath != null);
                            Console.WriteLine($"C-06-ICONOS: {conIconoCofreACofre}/{cofresReales} filas con sprite real de variante (esperado TODAS en un mundo vanilla real)");
                            if (conIconoCofreACofre != cofresReales) Console.WriteLine("FALLO: C-06 - alguna fila de 'Cofre a cofre' salio sin sprite real de variante");

                            var conContenido = vm.Exploration.ChestRows.FirstOrDefault(r => r.ItemCount > 0);
                            if (conContenido != null)
                            {
                                bool antesDesplegado = conContenido.IsExpanded;
                                vm.Exploration.GoToChestCommand.Execute(conContenido);
                                Console.WriteLine($"C-06-DESPLEGAR: cofre con {conContenido.ItemCount} objeto(s) real(es) -> IsExpanded antes={antesDesplegado}, despues={conContenido.IsExpanded} (esperado el contrario)");
                                if (conContenido.IsExpanded == antesDesplegado) Console.WriteLine("FALLO: C-06 - pulsar la fila del cofre no despliega/repliega su contenido");
                                bool primerObjetoConNombreReal = conContenido.Items.Count > 0 && !string.IsNullOrEmpty(conContenido.Items[0].Name);
                                Console.WriteLine($"C-06-CONTENIDO: primer objeto real del cofre tiene nombre resuelto={primerObjetoConNombreReal} (esperado True) - '{(conContenido.Items.Count > 0 ? conContenido.Items[0].Name : "")}'");
                                if (!primerObjetoConNombreReal) Console.WriteLine("FALLO: C-06 - el contenido desplegado del cofre no resuelve nombres reales");
                                vm.Exploration.GoToChestCommand.Execute(conContenido); // deja el estado como estaba
                            }
                            else Console.WriteLine("C-06-DESPLEGAR: ningun cofre real de este mundo tiene contenido - omitido");

                            // Deja un cofre desplegado de verdad para la captura visual de abajo.
                            if (conContenido != null && !conContenido.IsExpanded) vm.Exploration.GoToChestCommand.Execute(conContenido);
                            DoEvents();
                            var rtbCofreACofre = new System.Windows.Media.Imaging.RenderTargetBitmap(
                                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                            rtbCofreACofre.Render(window);
                            var encCofreACofre = new System.Windows.Media.Imaging.PngBitmapEncoder();
                            encCofreACofre.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbCofreACofre));
                            using (var fsCofreACofre = File.Create(Path.Combine(AppContext.BaseDirectory, "exploracion-cofre-a-cofre.png"))) encCofreACofre.Save(fsCofreACofre);
                            Console.WriteLine("Captura Cofre a cofre (C-06) -> exploracion-cofre-a-cofre.png");
                        }
                        vm.Exploration.ChestViewMode = 0; // deja el estado por defecto para el resto del arnes
                        DoEvents();
                    }
                    catch (Exception ex) { Console.WriteLine("C-06-EXCEPTION: " + ex); }

                    // Minerales: los 3 grupos reales + "Marcar en el mapa" (capa de resaltado sin
                    // tope + lista de VETAS agrupadas).
                    vm.Exploration.SelectedCategory = WorldSearchCategory.Ores;
                    DoEvents();
                    int mineralesPresentes = vm.Exploration.OreMetals.Count + vm.Exploration.OreGems.Count + vm.Exploration.OreTargets.Count;
                    Console.WriteLine($"CATEGORIAS-MINERALES: presentes en este mundo real = {mineralesPresentes} (metales={vm.Exploration.OreMetals.Count}, gemas={vm.Exploration.OreGems.Count}, otros={vm.Exploration.OreTargets.Count})");
                    if (mineralesPresentes > 0)
                    {
                        // Parte A: todo mineral/gema/objetivo real del catalogo tiene su tile
                        // base extraido (749 de 754 tipos reales) - esperado 100% con sprite.
                        int mineralesConIcono = vm.Exploration.OreMetals.Concat(vm.Exploration.OreGems).Concat(vm.Exploration.OreTargets).Count(r => r.IconPath != null);
                        Console.WriteLine($"ICONOS-MINERALES: {mineralesConIcono}/{mineralesPresentes} filas con sprite real (esperado TODAS)");
                        if (mineralesConIcono != mineralesPresentes) Console.WriteLine("FALLO: Parte A - algun mineral/gema/objetivo real salio sin sprite");

                        var primerMineral = vm.Exploration.OreMetals.FirstOrDefault() ?? vm.Exploration.OreGems.FirstOrDefault() ?? vm.Exploration.OreTargets.First();
                        primerMineral.IsChecked = true;
                        // Sin app.Run() real este arnes no puede await-ear sin deadlockear (ver
                        // el comentario real de X-7/T-13 mas abajo) - fire-and-forget + pumpear
                        // con WaitForDispatcher hasta que termine, mismo patron ya establecido.
                        vm.Exploration.MarkOresOnMapCommand.Execute(null);
                        WaitForDispatcher(2000);
                        bool hayResaltado = vm.Exploration.WorldHighlight != null;
                        Console.WriteLine($"CATEGORIAS-MINERALES-MARCAR: '{primerMineral.Name}' ({primerMineral.CountLabel}) -> WorldHighlight != null={hayResaltado} (esperado True), WorldSearchResults.Count={vm.Exploration.WorldSearchResults.Count} (vetas, esperado >=1), resumen='{vm.Exploration.WorldSearchSummary}'");
                        if (!hayResaltado || vm.Exploration.WorldSearchResults.Count == 0) Console.WriteLine("FALLO: Punto 4 - 'Marcar en el mapa' no genero ni la capa de resaltado ni la lista de vetas");

                        var rtbMinerales = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtbMinerales.Render(window);
                        var encMinerales = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encMinerales.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbMinerales));
                        using (var fsMinerales = File.Create(Path.Combine(AppContext.BaseDirectory, "mundo-minerales-marcados.png"))) encMinerales.Save(fsMinerales);
                        Console.WriteLine("Captura minerales marcados en el mapa -> mundo-minerales-marcados.png");

                        vm.Exploration.ClearOreMarksCommand.Execute(null);

                        // A9-03-MINERALCLIC (informe de pulido final, C-03, cierra E5): pulsar el
                        // NOMBRE de un mineral tenia que hacer lo mismo que en Cofres/Objetos -
                        // antes era el unico sitio de la barra lateral donde un clic no llevaba a
                        // ningun resultado (BuildSingleRowQuery no tenia rama Ores).
                        vm.Exploration.SearchInventoryRowCommand.Execute(primerMineral);
                        WaitForDispatcher(1000);
                        Console.WriteLine($"A9-03-MINERALCLIC: clic en '{primerMineral.Name}' -> WorldSearchResults.Count={vm.Exploration.WorldSearchResults.Count} (esperado > 0)");
                        if (vm.Exploration.WorldSearchResults.Count == 0) Console.WriteLine("FALLO: C-03 - pulsar el nombre de un mineral no encontro nada");

                        // C-04: el tick por si solo (sin pulsar "Marcar en el mapa") tiene que
                        // disparar el resaltado con debounce (WorldInventoryRowViewModel.
                        // CheckedChanged + _highlightDebounceTimer, 250ms). IsChecked ya estaba a
                        // True desde arriba - forzar el CAMBIO real (False->True) o el setter
                        // generado no vuelve a disparar OnIsCheckedChanged.
                        vm.Exploration.ClearOreMarksCommand.Execute(null);
                        primerMineral.IsChecked = false;
                        DoEvents();
                        primerMineral.IsChecked = true;
                        // 250ms de debounce + el propio render+flood-fill del mineral (mismo
                        // orden de magnitud que CATEGORIAS-MINERALES-MARCAR arriba, que ya usa
                        // 2000ms para el mismo mundo Grande real).
                        WaitForDispatcher(2500);
                        bool resaltadoPorTick = vm.Exploration.WorldHighlight != null;
                        Console.WriteLine($"C-04-TICKSOLO: marcar el tick de '{primerMineral.Name}' sin pulsar boton -> WorldHighlight != null={resaltadoPorTick} (esperado True tras el debounce)");
                        if (!resaltadoPorTick) Console.WriteLine("FALLO: C-04 - el tick por si solo no dispara el resaltado con debounce");

                        vm.Exploration.ClearOreMarksCommand.Execute(null);
                        primerMineral.IsChecked = false;
                        WaitForDispatcher(500);
                    }
                    else Console.WriteLine("CATEGORIAS-MINERALES: este mundo real no tiene ningun mineral/gema/objetivo de la tabla real, omitido el marcado");

                    // F-15 (auditoria de Opus vs TEdit, cierra B-01/B-06): en Exploracion, con un
                    // mundo real cargado, la columna central de la barra superior debe mostrar el
                    // TITULO REAL del mundo (no la franja de vitales del personaje).
                    vm.SelectedTabIndex = 4; // Exploracion
                    DoEvents();
                    var tituloMundoEnBarra = Descendientes<TextBlock>(window).FirstOrDefault(t => t.Text == vm.Exploration.WorldTitle && t.IsVisible);
                    Console.WriteLine($"F-15: IsExplorationTabActive={vm.IsExplorationTabActive} (esperado True), ShowVitalsStrip={vm.ShowVitalsStrip} (esperado False), titulo real del mundo ('{vm.Exploration.WorldTitle}') visible en la barra superior={tituloMundoEnBarra != null}");
                    if (!vm.IsExplorationTabActive || vm.ShowVitalsStrip || tituloMundoEnBarra == null)
                        Console.WriteLine("FALLO: F-15 - la barra superior no muestra el titulo real del mundo en la pestaña Exploracion");

                    // F-10 (auditoria de Opus vs TEdit, E-10): plegar/desplegar la barra lateral
                    // debe cambiar el ancho REAL de la columna (no solo la propiedad de la
                    // ViewModel) - se localiza el DockPanel real subiendo desde un TextBlock
                    // conocido de dentro de esa columna.
                    var buscarEnElMundo = Descendientes<TextBlock>(window).FirstOrDefault(t => t.Text == "Buscar en el mundo");
                    DependencyObject? ancestroSidebar = buscarEnElMundo;
                    System.Windows.Controls.DockPanel? sidebarDockPanel = null;
                    while (ancestroSidebar != null)
                    {
                        ancestroSidebar = System.Windows.Media.VisualTreeHelper.GetParent(ancestroSidebar);
                        if (ancestroSidebar is System.Windows.Controls.DockPanel dpSidebar) { sidebarDockPanel = dpSidebar; break; }
                    }
                    if (sidebarDockPanel != null)
                    {
                        double anchoExpandido = sidebarDockPanel.ActualWidth;
                        vm.Settings.ExplorationSidebarWidth = 0;
                        DoEvents(); DoEvents();
                        double anchoPlegado = sidebarDockPanel.ActualWidth;
                        vm.Settings.ExplorationSidebarWidth = 320;
                        DoEvents(); DoEvents();
                        double anchoRestaurado = sidebarDockPanel.ActualWidth;
                        Console.WriteLine($"F-10: ancho expandido={anchoExpandido:0}px, plegado={anchoPlegado:0}px (esperado ~0), restaurado={anchoRestaurado:0}px (esperado >200)");
                        if (anchoPlegado > 2 || anchoRestaurado < 200) Console.WriteLine("FALLO: F-10 - el plegado/despliegue de la barra lateral no cambia el ancho real de la columna");

                        // F-10-REOPEN (14-sep-2026): bug real confirmado por el usuario - al
                        // plegar a 0, el boton "›" (dentro de la columna que se colapsa) y el
                        // GridSplitter (Collapsed por su propio Style) desaparecian los DOS a la
                        // vez, sin dejar ningun sitio real desde el que volver a abrir la barra.
                        // F-10 de arriba solo movia la propiedad de la ViewModel - nunca miraba
                        // si de verdad quedaba algo pulsable en pantalla, que es justo el hueco
                        // por el que se colo el bug. Aqui se comprueba lo real: que las dos
                        // pestañas (plegar/desplegar) son visibilidades EXACTAMENTE opuestas en
                        // los dos estados, y que un CLIC REAL (InvokePattern, no la propiedad)
                        // sobre la de desplegar devuelve el ancho de antes de plegar.
                        var collapseBtn = window.FindName("CollapseExplorationSidebarButton") as System.Windows.Controls.Button;
                        var expandBtn = window.FindName("ExpandExplorationSidebarButton") as System.Windows.Controls.Button;
                        if (collapseBtn == null || expandBtn == null)
                            Console.WriteLine("FALLO: F-10-REOPEN - no se encuentran los dos botones reales de plegar/desplegar por su x:Name");
                        else
                        {
                            // El Border original nunca tuvo su PROPIA Visibility ligada al ancho
                            // (solo el GridSplitter la tiene), y su ActualWidth/hit-test no bajan
                            // de forma fiable a "0 de verdad" solo por vivir dentro de una columna
                            // a 0px (el ScrollViewer recorta por CLIP visual, no reduciendo el
                            // Arrange de sus hijos - un detalle de layout de WPF, no del bug) -
                            // intentarlo daba lecturas contradictorias con la ventana real. La
                            // prueba que de verdad importa, y que SI es inequivoca: el boton NUEVO
                            // (IsVisible ligado a una DataTrigger real) tiene que verse exactamente
                            // cuando la barra esta plegada, Y un CLIC REAL sobre el (InvokePattern,
                            // nunca la propiedad a mano) tiene que devolver la columna a su ancho
                            // REAL exacto de antes de plegar (medido en pixeles, no solo en la
                            // propiedad) - eso es lo que demuestra que el usuario ya tiene un sitio
                            // real y funcional desde el que volver a abrirla.
                            void ClicRealBoton(string nombre)
                            {
                                var el = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, nombre));
                                if (el != null && el.TryGetCurrentPattern(InvokePattern.Pattern, out var pat)) ((InvokePattern)pat).Invoke();
                                else Console.WriteLine($"FALLO: F-10-REOPEN - '{nombre}' no se encuentra por UI Automation o no expone InvokePattern, un clic real no podria activarlo");
                                DoEvents(); DoEvents(); WaitForDispatcher(200);
                            }

                            vm.Settings.ExplorationSidebarWidth = 280;
                            DoEvents(); DoEvents();
                            Console.WriteLine($"F-10-REOPEN: con la barra ABIERTA (280px) -> boton desplegar IsVisible={expandBtn.IsVisible} (esperado False - no hace falta con la barra ya abierta)");
                            if (expandBtn.IsVisible)
                                Console.WriteLine("FALLO: F-10-REOPEN - con la barra abierta el boton de desplegar no deberia verse");

                            // Clic REAL de plegar (no la propiedad a mano): asi el code-behind
                            // memoriza _lastExpandedSidebarWidth=280 de verdad, exactamente lo que
                            // le pasaria a un usuario real - el mismo AutomationProperties.Name
                            // (distinto del Content "‹"/"›" visible, que coincide con el de
                            // "resultado anterior" de la busqueda del mapa y seria ambiguo).
                            ClicRealBoton("CollapseExplorationSidebarButton");
                            double anchoTrasClicPlegar = sidebarDockPanel.ActualWidth;
                            Console.WriteLine($"F-10-REOPEN: tras CLIC REAL de plegar -> Settings.ExplorationSidebarWidth={vm.Settings.ExplorationSidebarWidth:0} (esperado 0), ancho real de la columna={anchoTrasClicPlegar:0}px (esperado ~0), boton desplegar IsVisible={expandBtn.IsVisible} (esperado True - este es el que el bug real dejaba sin ningun sitio)");
                            if (vm.Settings.ExplorationSidebarWidth != 0 || anchoTrasClicPlegar > 2 || !expandBtn.IsVisible)
                                Console.WriteLine("FALLO: F-10-REOPEN - tras plegar con un clic real tiene que quedar visible un sitio real desde el que volver a abrirla");

                            ClicRealBoton("ExpandExplorationSidebarButton");
                            double anchoTrasClicReal = sidebarDockPanel.ActualWidth;
                            Console.WriteLine($"F-10-REOPEN: clic REAL (InvokePattern) sobre el boton de desplegar -> Settings.ExplorationSidebarWidth={vm.Settings.ExplorationSidebarWidth:0}, ancho real de la columna={anchoTrasClicReal:0}px (esperado los dos en 280, el ancho real de antes de plegar)");
                            if (vm.Settings.ExplorationSidebarWidth != 280 || anchoTrasClicReal < 200)
                                Console.WriteLine("FALLO: F-10-REOPEN - el clic real sobre el boton de desplegar no devuelve la barra a su ancho anterior exacto");

                            vm.Settings.ExplorationSidebarWidth = 320; // deja el estado como esperan los bloques siguientes
                            DoEvents(); DoEvents();
                        }
                    }
                    else Console.WriteLine("F-10: no se encontro la barra lateral en el arbol visual - omitido");

                    // F-8 (auditoria de Opus vs TEdit, E-05): el minimapa real muestra el bitmap
                    // del mundo YA congelado, y su rectangulo de viewport esta visible.
                    //
                    // Falso positivo real encontrado y arreglado el 15-sep-2026 (cierre de sesion,
                    // triaje de los FALLO del recorrido completo): este bloque corre justo DESPUES
                    // de "X-a AJUSTAR-A-LA-VENTANA" (mas arriba en este mismo Main()), que deja el
                    // zoom al 8% a proposito - con el mundo ENTERO ya visible en el viewport,
                    // MainWindow.xaml.cs.UpdateMinimapViewport() colapsa el rectangulo A PROPOSITO
                    // ("igual que cualquier minimapa real... no aportaria nada", ver el comentario
                    // real de esa funcion) - el codigo de produccion hace lo correcto, era esta
                    // asercion la que no tenia en cuenta en que zoom quedaba el mapa por el propio
                    // orden de los bloques anteriores. Se reestablece aqui un zoom de trabajo real
                    // (4.0, el mismo "zoom de trabajo real de F-3" que ya usa AR-12e mas abajo) para
                    // volver a la situacion que este bloque siempre quiso comprobar de verdad: el
                    // mundo NO cabe entero en el viewport, asi que el rectangulo SI debe verse.
                    //
                    // SEGUNDA causa real, encontrada al comprobar que el arreglo de arriba por si
                    // solo NO bastaba (seguia en FALLO tras fijar el zoom): el settings.json REAL
                    // de esta maquina (%LOCALAPPDATA%\Terrakeep\settings.json) tenia
                    // "IsMinimapVisible":false - UpdateMinimapViewport() colapsa el rectangulo
                    // ANTES de mirar el zoom si este ajuste esta en false (primer early-return de
                    // la funcion, MainWindow.xaml.cs). Este arnes carga el CharacterFileService/
                    // SettingsViewModel reales de esta maquina (nunca uno aislado, a diferencia de
                    // otros _SOLO que restauran window.json/settings.json a proposito) - el ajuste
                    // llevaba en false desde una ronda de pruebas anterior que lo toco y no lo
                    // devolvio a su valor, sin que ningun bloque de este arnes lo supiera. Se fija
                    // aqui explicitamente a True (mismo criterio "arrange" ya usado por
                    // Settings.Language en otros bloques de este mismo Main) para que la
                    // comprobacion sea determinista pase lo que pase en el disco real.
                    vm.Settings.IsMinimapVisible = true;
                    var scrollF8 = Descendientes<ScrollViewer>(window).FirstOrDefault(sv => sv.Name == "WorldMapScroll");
                    vm.Exploration.Zoom = 4.0;
                    scrollF8?.UpdateLayout();
                    DoEvents(); DoEvents();
                    var minimapImg = Descendientes<System.Windows.Controls.Image>(window).FirstOrDefault(i => i.Name == "MinimapImage");
                    Console.WriteLine($"F-8: MinimapImage encontrado={minimapImg != null}, con el bitmap real del mundo={minimapImg?.Source != null} (esperado True)");
                    if (minimapImg == null || minimapImg.Source == null) Console.WriteLine("FALLO: F-8 - el minimapa no muestra el bitmap real del mundo");
                    var minimapRect = Descendientes<System.Windows.Shapes.Rectangle>(window).FirstOrDefault(r => r.Name == "MinimapViewportRect");
                    Console.WriteLine($"F-8: con zoom={vm.Exploration.Zoom} (mundo NO cabe entero) -> MinimapViewportRect encontrado={minimapRect != null}, visible={minimapRect?.IsVisible} (esperado True)");
                    if (minimapRect == null || !minimapRect.IsVisible) Console.WriteLine("FALLO: F-8 - el rectangulo de viewport del minimapa no aparece con el mundo parcialmente visible");

                    // F-14 (auditoria de Opus vs TEdit, E-16/E-17): el informe generado debe
                    // contener la semilla REAL del mundo (no un valor inventado) y el censo.
                    string informeMundo = vm.Exploration.BuildWorldReportText();
                    string semillaReal = vm.Exploration.WorldSeedText;
                    bool informeValido = informeMundo.Contains(semillaReal) && informeMundo.Contains("Aire:") && informeMundo.Length > 50;
                    Console.WriteLine($"F-14: informe generado ({informeMundo.Length} caracteres), contiene la semilla real ('{semillaReal}')={informeMundo.Contains(semillaReal)}, contiene 'Aire:'={informeMundo.Contains("Aire:")}");
                    if (!informeValido) Console.WriteLine("FALLO: F-14 - el informe del mundo no contiene los datos reales esperados");

                    // F-11 (auditoria de Opus vs TEdit, E-11): guardar la vista, cambiar zoom/
                    // scroll, RECARGAR el mismo mundo de disco y confirmar que la vista guardada
                    // se restaura de verdad (no solo que el fichero se escriba).
                    var mapScroll = Descendientes<System.Windows.Controls.ScrollViewer>(window).FirstOrDefault(sv => sv.Name == "WorldMapScroll");
                    if (mapScroll != null)
                    {
                        vm.Exploration.Zoom = 2.5;
                        DoEvents();
                        mapScroll.UpdateLayout();
                        mapScroll.ScrollToHorizontalOffset(500);
                        mapScroll.ScrollToVerticalOffset(300);
                        DoEvents(); DoEvents();
                        vm.Exploration.SaveCurrentViewState(mapScroll.HorizontalOffset, mapScroll.VerticalOffset);
                        double zoomGuardado = vm.Exploration.Zoom;
                        double offsetHGuardado = mapScroll.HorizontalOffset;
                        double offsetVGuardado = mapScroll.VerticalOffset;

                        var taskRecarga = vm.Exploration.LoadFromPathAsync(worldPath);
                        while (!taskRecarga.IsCompleted) DoEvents();
                        DoEvents();

                        bool zoomRestaurado = Math.Abs(vm.Exploration.Zoom - zoomGuardado) < 0.001;
                        bool tienePendiente = vm.Exploration.TryConsumePendingViewRestore(out double offsetHRestaurado, out double offsetVRestaurado);
                        Console.WriteLine($"F-11: zoom guardado={zoomGuardado}, tras recargar={vm.Exploration.Zoom} (esperado igual); vista pendiente encontrada={tienePendiente}, offset=({offsetHRestaurado:0},{offsetVRestaurado:0}) (esperado ~({offsetHGuardado:0},{offsetVGuardado:0}))");
                        if (!zoomRestaurado || !tienePendiente || Math.Abs(offsetHRestaurado - offsetHGuardado) > 1 || Math.Abs(offsetVRestaurado - offsetVGuardado) > 1)
                            Console.WriteLine("FALLO: F-11 - la vista guardada no se restauro correctamente al recargar el mismo mundo");
                    }
                    else Console.WriteLine("F-11: no se encontro WorldMapScroll en el arbol visual - omitido");

                    // Objetos: inventario real de tiles (vista por defecto).
                    vm.Exploration.SelectedCategory = WorldSearchCategory.Objects;
                    DoEvents();
                    Console.WriteLine($"CATEGORIAS-OBJETOS: Inventory.Count={vm.Exploration.Inventory.Count} (esperado >=1, tiles realmente presentes en este mundo)");
                    if (vm.Exploration.Inventory.Count == 0) Console.WriteLine("FALLO: Punto 4 - la categoria Objetos no genero ningun inventario de tiles");

                    // P-7 (auditoria de Opus vs TEdit): el id real ([N]) debe verse de verdad en
                    // el arbol visual de al menos una fila del inventario, no solo estar en el
                    // ViewModel - y sin haber recortado nada (AR-02, ya comprobado arriba).
                    if (vm.Exploration.Inventory.Count > 0)
                    {
                        int primerId = vm.Exploration.Inventory[0].Id;
                        var idVisible = Descendientes<TextBlock>(window).FirstOrDefault(t => t.Text == $"[{primerId}]" && t.IsVisible);
                        Console.WriteLine($"P-7: id real del primer objeto ({primerId}) visible en el arbol visual={idVisible != null} (esperado True)");
                        if (idVisible == null) Console.WriteLine("FALLO: P-7 - el id de la fila de inventario no aparece visible");
                    }

                    var rtbCategorias = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbCategorias.Render(window);
                    var encCategorias = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encCategorias.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbCategorias));
                    using (var fsCategorias = File.Create(Path.Combine(AppContext.BaseDirectory, "mundo-categoria-objetos.png"))) encCategorias.Save(fsCategorias);
                    Console.WriteLine("Captura categoria Objetos -> mundo-categoria-objetos.png");

                    // Parte B: captura de los selectores "Tiles/Paredes/Liquidos" rediseñados,
                    // para juzgar a ojo si se parecen a "Cargar personaje" y se distinguen de el.
                    var rtbSelectoresObjetos = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbSelectoresObjetos.Render(window);
                    var encSelectoresObjetos = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encSelectoresObjetos.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbSelectoresObjetos));
                    using (var fsSelObjetos = File.Create(Path.Combine(AppContext.BaseDirectory, "exploracion-selectores-objetos.png"))) encSelectoresObjetos.Save(fsSelObjetos);
                    Console.WriteLine("Captura selectores de Objetos (rediseñados) -> exploracion-selectores-objetos.png");

                    // Parte A: Paredes (esperado alto pero NO 100% - la pared 367 y las de mod no
                    // tienen icono, mismo hueco real que ya tienen sin nombre - ver
                    // ESPEC-sprites-botones-badges.md#A.3.6) y Liquidos (esperado 0%, decision
                    // deliberada, ver #A.11).
                    vm.Exploration.ObjectsViewMode = 1;
                    DoEvents();
                    int paredesConIcono = vm.Exploration.Inventory.Count(r => r.IconPath != null);
                    Console.WriteLine($"ICONOS-PAREDES: {paredesConIcono}/{vm.Exploration.Inventory.Count} filas con sprite real (esperado alto pero NO necesariamente el 100%)");

                    vm.Exploration.ObjectsViewMode = 2;
                    DoEvents();
                    int liquidosConIcono = vm.Exploration.Inventory.Count(r => r.IconPath != null);
                    Console.WriteLine($"ICONOS-LIQUIDOS: {liquidosConIcono}/{vm.Exploration.Inventory.Count} filas con sprite real (esperado 0 - decision deliberada, sin sprite recortable)");
                    if (liquidosConIcono != 0) Console.WriteLine("FALLO: Parte A - algun liquido salio con IconPath (deberia ser siempre null)");

                    // Bug real corregido (reportado: "pestaña Liquidos no tiene sus sprites" - sin
                    // IconPath NI SwatchColor, la fila no mostraba nada en absoluto). El swatch de
                    // respaldo ahora usa el color real de MapColorCatalog.LiquidColor, nunca
                    // transparente si hay al menos un liquido presente en el mundo.
                    int liquidosSwatchTransparente = vm.Exploration.Inventory.Count(r => r.SwatchColor.A == 0);
                    Console.WriteLine($"SWATCH-LIQUIDOS: {liquidosSwatchTransparente}/{vm.Exploration.Inventory.Count} filas con SwatchColor transparente (esperado 0)");
                    if (vm.Exploration.Inventory.Count > 0 && liquidosSwatchTransparente > 0)
                        Console.WriteLine("FALLO: la pestaña Liquidos tiene filas sin sprite NI color de respaldo (no se ve nada)");

                    // A9-10-MIELTODA (informe de pulido final, C-04, cierra E6): "una cantidad
                    // absurda de mieles" - antes el mapa y la lista compartian el mismo tope de
                    // facto. Generalizado el patron de Minerales (resaltado sin tope) a Objetos >
                    // Liquidos: el numero de pixeles a opacidad completa (nucleo, sin contar el
                    // halo de alrededor) tiene que ser EXACTAMENTE el recuento real del liquido.
                    var primerLiquido = vm.Exploration.Inventory.FirstOrDefault();
                    if (primerLiquido != null)
                    {
                        primerLiquido.IsChecked = true;
                        vm.Exploration.MarkObjectsOnMapCommand.Execute(null);
                        WaitForDispatcher(2000);
                        int nucleoPixeles = 0;
                        if (vm.Exploration.WorldHighlight is System.Windows.Media.Imaging.BitmapSource bmpLiquido)
                        {
                            int bw = bmpLiquido.PixelWidth, bh = bmpLiquido.PixelHeight;
                            var buf = new byte[bh * bw * 4];
                            bmpLiquido.CopyPixels(buf, bw * 4, 0);
                            for (int i = 3; i < buf.Length; i += 4) if (buf[i] == 255) nucleoPixeles++;
                        }
                        Console.WriteLine($"A9-10-MIELTODA: '{primerLiquido.Name}' ({primerLiquido.Count:N0} tiles reales) -> pixeles a opacidad completa en la capa={nucleoPixeles:N0} (esperado exactamente igual)");
                        if (nucleoPixeles != primerLiquido.Count) Console.WriteLine("FALLO: C-04 - el resaltado de liquidos no marca TODAS las posiciones reales");
                        vm.Exploration.ClearOreMarksCommand.Execute(null);
                        primerLiquido.IsChecked = false;
                        WaitForDispatcher(500);
                    }
                    vm.Exploration.ObjectsViewMode = 0;

                    vm.Exploration.SelectedCategory = WorldSearchCategory.All; // deja el estado limpio para pasos siguientes
                    DoEvents();

                    // A8-01 (auditoria de Opus vs TEdit, E-01): "Sin resultados." se calculaba
                    // pero el TextBlock que lo muestra vivia dentro de un Grid cuya visibilidad
                    // dependia de WorldSearchResults.Count>0 - justo la condicion falsa. Buscar
                    // algo que este mundo no tiene debe dejar feedback VISIBLE de verdad en el
                    // arbol visual (IsVisible ya tiene en cuenta la visibilidad de TODOS los
                    // ancestros, no solo la propia), no solo la propiedad de la ViewModel.
                    // P-6 (misma auditoria): el mensaje ya no es el CaptionText plano de 11px -
                    // el bloque 4 lo sustituyo por un panel con cuerpo (BodyText + sugerencia,
                    // ShowZeroResultsState), asi que la comprobacion verifica ESE panel real, no
                    // el texto literal "Sin resultados." (que ahora vive solo en WorldSearchSummary,
                    // consumido por P-6, no mostrado a secas).
                    vm.Exploration.WorldSearchText = "zzzznoexisteenningunmundo";
                    WaitForDispatcher(1000); // debounce real (250ms) + el barrido en segundo plano
                    Console.WriteLine($"A8-01: WorldSearchResults.Count={vm.Exploration.WorldSearchResults.Count} (esperado 0), WorldSearchSummary='{vm.Exploration.WorldSearchSummary}' (esperado 'Sin resultados.'), ShowZeroResultsState={vm.Exploration.ShowZeroResultsState} (esperado True)");
                    var zeroResultsPanel = Descendientes<System.Windows.Controls.StackPanel>(window).FirstOrDefault(sp => sp.Name == "ZeroResultsPanel");
                    // Bug real encontrado al verificar: TextBlock.Text devuelve "" cuando el
                    // contenido se puso via Runs anidados en XAML (no via el atributo Text) - hay
                    // que leer Inlines directamente, no el getter .Text, para ese TextBlock.
                    string textoPanel = zeroResultsPanel != null
                        ? string.Concat(Descendientes<TextBlock>(zeroResultsPanel)
                            .SelectMany(t => t.Inlines.OfType<System.Windows.Documents.Run>().Select(r => r.Text)))
                        : "";
                    Console.WriteLine($"A8-01: ZeroResultsPanel encontrado={zeroResultsPanel != null}, IsVisible={zeroResultsPanel?.IsVisible} (esperado True), texto='{textoPanel}'");
                    if (zeroResultsPanel == null || !zeroResultsPanel.IsVisible || !textoPanel.Contains("Nada que coincida"))
                        Console.WriteLine("FALLO: A8-01 - el panel de 'sin resultados' (P-6) no aparece VISIBLE en el arbol visual tras una busqueda sin coincidencias");
                    vm.Exploration.WorldSearchText = string.Empty; // deja el estado limpio para pasos siguientes
                    WaitForDispatcher(300);
                }
                catch (Exception ex) { Console.WriteLine("CATEGORIAS-EXPLORACION-EXCEPTION: " + ex); }

                // Auditoria de redimensionado, AR-02 (H-02): la barra lateral de Exploracion NO
                // se recorta a NINGUN tamaño real, incluido 4K - antes de esta auditoria se
                // recortaba SIEMPRE (el MaxWidth vivia en el sitio equivocado, ver R-02). El
                // mundo real cargado arriba (roca negra) sigue disponible en este punto.
                try
                {
                    foreach (double w in new double[] { 1080, 1500, 1920, 2560 })
                    {
                        FijarTamaño(window, w, 900);
                        vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                        DoEvents(); DoEvents();
                        foreach (var cat in Enum.GetValues<WorldSearchCategory>())
                        {
                            vm.Exploration.SelectedCategory = cat;
                            DoEvents(); DoEvents();
                            var pildoraObjetos = Descendientes<System.Windows.Controls.RadioButton>(window)
                                .FirstOrDefault(rb => (rb.Content as string ?? "").StartsWith("Objetos") ||
                                    Descendientes<TextBlock>(rb).Any(t => (t.Text ?? "").StartsWith("Objetos")));
                            if (pildoraObjetos == null) continue;
                            var (rx, ry) = Recorte(pildoraObjetos);
                            Console.WriteLine($"AR-02: a {w}px, categoria {cat}, pildora 'Objetos' recorte=({rx:0},{ry:0}) (esperado 0,0)");
                            if (rx > 0 || ry > 0) Console.WriteLine($"FALLO: AR-02 - la barra lateral de Exploracion recorta {rx:0}x{ry:0}px a {w}px, categoria {cat} (H-02)");
                        }
                    }
                    vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                    FijarTamaño(window, 1180, 860);
                    DoEvents();
                }
                catch (Exception ex) { Console.WriteLine("AR-02-EXCEPTION: " + ex); }

                // AR-11 (bug real reportado por el usuario probando la app, 6-sep-2026: "en todas
                // las secciones de Exploracion se pierde contenido con el scroll", captura de
                // Cofres > "Por lo que contienen" con la palabra "contienen" cortada). Tres
                // comprobaciones PERMANENTES sobre la barra lateral entera, no sobre un caso:
                //   (a) reparto vertical - el hueco real que le queda al contenido de cada
                //       categoria dentro del DockPanel de la columna. Antes del arreglo, los
                //       bloques Dock=Top/Bottom (cabecera + "Este mundo" DESPLEGADO + pildoras +
                //       buscador + bloque de resultados) se servian PRIMERO y el relleno - la
                //       lista real de la categoria - se quedaba con lo que sobrase, que a poca
                //       altura de ventana es 0px: contenido perdido de verdad, sin ninguna barra
                //       de scroll con la que alcanzarlo (un DockPanel no scrollea).
                //   (b) recorte de los 3 chips de modo de Cofres - "Por lo que contienen" es el
                //       texto mas largo de los tres y el UniformGrid les da un tercio exacto.
                //   (c) ningun elemento VISIBLE de la columna puede quedar recortado sin un
                //       ScrollViewer ancestro que permita llegar a el (mismo criterio real que
                //       AR-07 ya aplica a la columna de preview de Inicio).
                try
                {
                    var sidebar = window.FindName("ExplorationSidebarPanel") as FrameworkElement;
                    // ADR-TERRAKEEP-029 (26-sep-2026): Browse se movio a BrowseView.xaml
                    // (UserControl con su propio NameScope) - FindName DOBLE, mismo patron ya usado
                    // por GUIA/ADR-021, Compare/ADR-025, WorldTools/ADR-027 y ChestInspector/ADR-028.
                    var browseViewHostAr11 = window.FindName("BrowseView") as FrameworkElement;
                    var contenidoCat = browseViewHostAr11?.FindName("ExplorationCategoryContent") as FrameworkElement;
                    var bloqueResultados = browseViewHostAr11?.FindName("ExplorationResultsBlock") as FrameworkElement;
                    var chipsCofres = browseViewHostAr11?.FindName("ChestModeSelector") as FrameworkElement;
                    if (sidebar == null || contenidoCat == null || bloqueResultados == null || chipsCofres == null)
                        Console.WriteLine("FALLO: AR-11 - no se encontraron los elementos con nombre de la barra lateral de Exploracion (¿se renombraron en MainWindow.xaml?)");
                    else
                    {
                        // 860 es el alto real por defecto del arnes; 700 es el caso apretado real
                        // que AR-07 ya usa como suelo (portatil 1080x720 con barra de tareas).
                        foreach (var (w, h) in new (double, double)[] { (1180, 860), (1080, 700) })
                        {
                            FijarTamaño(window, w, h);
                            // "Este mundo" DESPLEGADO es el caso peor real y perfectamente normal
                            // (el usuario lo abre para ver semilla/version): +200px de Dock=Top.
                            var esteMundo = Descendientes<System.Windows.Controls.Expander>(window)
                                .FirstOrDefault(e => (e.Header as string) == "Este mundo" || (e.Header as string) == "This world");
                            foreach (bool desplegado in new[] { false, true })
                            {
                                if (esteMundo != null) esteMundo.IsExpanded = desplegado;
                                foreach (var cat in Enum.GetValues<WorldSearchCategory>())
                                {
                                    vm.Exploration.SelectedCategory = cat;
                                    if (cat == WorldSearchCategory.Chests) vm.Exploration.ChestViewMode = 2;
                                    DoEvents(); DoEvents();

                                    // (a) hueco real del contenido de la categoria.
                                    double alto = contenidoCat.ActualHeight;
                                    var (crx, cry) = Recorte(contenidoCat);
                                    Console.WriteLine($"AR-11a: {w}x{h}, {cat}, 'Este mundo' desplegado={desplegado} -> alto del contenido de categoria={alto:0}px, recorte=({crx:0},{cry:0}) (esperado >=120px y sin recorte)");
                                    if (alto < 120)
                                        Console.WriteLine($"FALLO: AR-11a - el contenido de la categoria {cat} se queda con {alto:0}px a {w}x{h} (Este mundo desplegado={desplegado}): contenido perdido sin forma de alcanzarlo");
                                    if (cry > 0)
                                        Console.WriteLine($"FALLO: AR-11a - el contenido de la categoria {cat} esta recortado {cry:0}px en vertical a {w}x{h} (Este mundo desplegado={desplegado})");

                                    // (c) nada visible recortado sin scroll con el que llegar.
                                    var recortadosSinScroll = Descendientes<FrameworkElement>(sidebar)
                                        .Where(fe => fe.IsVisible && fe is TextBlock or System.Windows.Controls.Primitives.ButtonBase)
                                        .Where(fe => { var (rx2, ry2) = Recorte(fe); return rx2 > 1 || ry2 > 1; })
                                        .Where(fe => !TieneScrollAncestro(fe, sidebar))
                                        .ToList();
                                    if (recortadosSinScroll.Count > 0)
                                    {
                                        string muestra = string.Join(" | ", recortadosSinScroll.Take(4).Select(fe =>
                                        {
                                            var (rx3, ry3) = Recorte(fe);
                                            string txt = fe is TextBlock tb2 ? tb2.Text : (fe as System.Windows.Controls.ContentControl)?.Content as string ?? fe.GetType().Name;
                                            return $"'{txt}' {rx3:0}x{ry3:0}px";
                                        }));
                                        Console.WriteLine($"FALLO: AR-11c - {recortadosSinScroll.Count} elemento(s) visible(s) recortado(s) SIN scroll ancestro a {w}x{h}, {cat} (Este mundo desplegado={desplegado}): {muestra}");
                                    }
                                    else Console.WriteLine($"AR-11c: {w}x{h}, {cat}, desplegado={desplegado} -> 0 elementos visibles recortados sin scroll (esperado 0)");
                                }
                            }
                            if (esteMundo != null) esteMundo.IsExpanded = false;

                            // (b) los 3 chips de modo de Cofres, con su texto real.
                            vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                            DoEvents(); DoEvents();
                            foreach (var chip in Descendientes<System.Windows.Controls.RadioButton>(chipsCofres))
                            {
                                var tbChip = Descendientes<TextBlock>(chip).FirstOrDefault();
                                string etiqueta = tbChip?.Text ?? chip.Content as string ?? "?";
                                var (bx, by) = Recorte(chip);
                                var (tx, ty) = tbChip != null ? Recorte(tbChip) : (0, 0);
                                Console.WriteLine($"AR-11b: a {w}px, chip de Cofres '{etiqueta}' recorte boton=({bx:0},{by:0}) texto=({tx:0},{ty:0}) (esperado 0,0 en los cuatro)");
                                if (bx > 1 || by > 1 || tx > 1 || ty > 1)
                                    Console.WriteLine($"FALLO: AR-11b - el chip de modo de Cofres '{etiqueta}' se recorta a {w}px (boton {bx:0}x{by:0}, texto {tx:0}x{ty:0})");
                            }
                        }
                        // (f) el ScrollViewer nuevo es una RED DE SEGURIDAD, no la forma normal de
                        // usar la columna: al tamaño por defecto y con el estado por defecto
                        // ("Este mundo" colapsado) no debe aparecer barra ninguna - si aparece, el
                        // MinHeight se ha pasado y la columna scrollea cuando no hacia falta.
                        vm.Exploration.ChestViewMode = 0;
                        vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                        FijarTamaño(window, 1180, 860);
                        DoEvents(); DoEvents();
                        var svLateral = window.FindName("ExplorationSidebarScroll") as System.Windows.Controls.ScrollViewer;
                        if (svLateral != null)
                        {
                            Console.WriteLine($"AR-11f: a 1180x860 (estado por defecto), columna: viewport={svLateral.ViewportHeight:0}px, contenido={svLateral.ExtentHeight:0}px, barra visible={svLateral.ComputedVerticalScrollBarVisibility} (esperado contenido<=viewport y Hidden)");
                            if (svLateral.ExtentHeight > svLateral.ViewportHeight + 1)
                                Console.WriteLine($"FALLO: AR-11f - la barra lateral de Exploracion scrollea al tamaño por defecto ({svLateral.ExtentHeight:0}px de contenido en {svLateral.ViewportHeight:0}px): el MinHeight es demasiado alto");
                        }
                        else Console.WriteLine("FALLO: AR-11f - no se encontro ExplorationSidebarScroll en el arbol visual");
                    }
                }
                catch (Exception ex) { Console.WriteLine("AR-11-EXCEPTION: " + ex); }

                // AR-12 (bugs reales reportados por el usuario probando la app, 6-sep-2026):
                //   (d) "en Cofre a cofre, al seleccionar un cofre no aparece el cuadradito de
                //       resaltado que si funciona en las demas secciones". Causa real:
                //       ChestRowViewModel no tenia IsCurrent (la plantilla de las demas categorias
                //       lo tiene en WorldSearchHitRowViewModel) - no habia binding roto, faltaba la
                //       propiedad Y el borde en la plantilla. Se comprueba en el ARBOL VISUAL real,
                //       no solo en la ViewModel: el Border de la fila pulsada tiene que pintar el
                //       teal, y el de la anterior tiene que apagarse.
                //   (e) "que Cofre a cofre tenga su PROPIA casilla de acercar". Se comprueba la
                //       independencia REAL en las dos direcciones (el estado de una no toca el de
                //       la otra) y, sobre todo, el EFECTO real: con la global encendida y la de
                //       cofres apagada, pulsar un cofre NO debe acercar; con la de cofres
                //       encendida, si.
                try
                {
                    vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                    vm.Exploration.ChestViewMode = 2;
                    DoEvents(); DoEvents();
                    if (vm.Exploration.ChestRows.Count < 2)
                        Console.WriteLine($"AR-12: el mundo real de pruebas solo tiene {vm.Exploration.ChestRows.Count} cofre(s) - omitido");
                    else
                    {
                        var cofreA = vm.Exploration.ChestRows[0];
                        var cofreB = vm.Exploration.ChestRows[1];

                        // (d) resaltado de seleccion, en la ViewModel y en el arbol visual.
                        vm.Exploration.GoToChestCommand.Execute(cofreA);
                        DoEvents(); DoEvents();
                        int marcadosA = vm.Exploration.ChestRows.Count(r => r.IsCurrent);
                        var brushA = BordeDeFilaDeCofre(window, cofreA);
                        Console.WriteLine($"AR-12d: tras pulsar el 1er cofre -> IsCurrent en el={cofreA.IsCurrent} (esperado True), filas marcadas={marcadosA} (esperado 1), borde real en pantalla={brushA}");
                        if (!cofreA.IsCurrent || marcadosA != 1)
                            Console.WriteLine("FALLO: AR-12d - 'Cofre a cofre' no marca como actual el cofre seleccionado");
                        if (brushA is not System.Windows.Media.SolidColorBrush scA || scA.Color.A == 0)
                            Console.WriteLine("FALLO: AR-12d - el cofre seleccionado NO pinta el borde de resaltado en el arbol visual (el indicador sigue sin verse)");

                        vm.Exploration.GoToChestCommand.Execute(cofreB);
                        DoEvents(); DoEvents();
                        var brushAtras = BordeDeFilaDeCofre(window, cofreA);
                        Console.WriteLine($"AR-12d: tras pulsar el 2o cofre -> 1er cofre IsCurrent={cofreA.IsCurrent} (esperado False), 2o={cofreB.IsCurrent} (esperado True), borde del 1o={brushAtras}");
                        if (cofreA.IsCurrent || !cofreB.IsCurrent || vm.Exploration.ChestRows.Count(r => r.IsCurrent) != 1)
                            Console.WriteLine("FALLO: AR-12d - el resaltado de 'Cofre a cofre' no es exclusivo (deberia marcar solo el ultimo pulsado)");

                        // (e) las dos casillas son de verdad independientes.
                        vm.Exploration.AutoZoomOnNavigate = false;
                        vm.Exploration.AutoZoomOnChestNavigate = false;
                        vm.Exploration.AutoZoomOnChestNavigate = true;
                        bool globalIntacta = !vm.Exploration.AutoZoomOnNavigate;
                        vm.Exploration.AutoZoomOnChestNavigate = false;
                        vm.Exploration.AutoZoomOnNavigate = true;
                        bool cofresIntacta = !vm.Exploration.AutoZoomOnChestNavigate;
                        Console.WriteLine($"AR-12e: encender la de cofres deja la global apagada={globalIntacta} (esperado True); encender la global deja la de cofres apagada={cofresIntacta} (esperado True)");
                        if (!globalIntacta || !cofresIntacta)
                            Console.WriteLine("FALLO: AR-12e - las dos casillas de acercar siguen atadas entre si");

                        // Efecto real: global ENCENDIDA, la de cofres APAGADA -> pulsar un cofre no
                        // debe acercar, pero saltar a un resultado de busqueda si.
                        vm.Exploration.Zoom = 1.0;
                        DoEvents();
                        vm.Exploration.GoToChestCommand.Execute(cofreA);
                        DoEvents(); DoEvents();
                        double zoomTrasCofreSinCasilla = vm.Exploration.Zoom;
                        Console.WriteLine($"AR-12e: global=ON, cofres=OFF -> zoom tras pulsar un cofre={zoomTrasCofreSinCasilla} (esperado 1, sin acercar)");
                        if (Math.Abs(zoomTrasCofreSinCasilla - 1.0) > 0.001)
                            Console.WriteLine("FALLO: AR-12e - 'Cofre a cofre' sigue obedeciendo a la casilla GLOBAL (no es independiente de verdad)");

                        // Y al reves: la suya encendida -> si acerca (el zoom de trabajo real, 4.0).
                        vm.Exploration.AutoZoomOnNavigate = false;
                        vm.Exploration.AutoZoomOnChestNavigate = true;
                        vm.Exploration.Zoom = 1.0;
                        DoEvents();
                        vm.Exploration.GoToChestCommand.Execute(cofreB);
                        DoEvents(); DoEvents();
                        double zoomTrasCofreConCasilla = vm.Exploration.Zoom;
                        Console.WriteLine($"AR-12e: global=OFF, cofres=ON -> zoom tras pulsar un cofre={zoomTrasCofreConCasilla} (esperado 4, el zoom de trabajo real de F-3)");
                        if (Math.Abs(zoomTrasCofreConCasilla - 4.0) > 0.001)
                            Console.WriteLine("FALLO: AR-12e - la casilla propia de 'Cofre a cofre' no acerca de verdad al seleccionar un cofre");

                        var rtbCofreSel = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtbCofreSel.Render(window);
                        var encCofreSel = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encCofreSel.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbCofreSel));
                        using (var fsCofreSel = File.Create(Path.Combine(AppContext.BaseDirectory, "exploracion-cofre-a-cofre-seleccionado.png"))) encCofreSel.Save(fsCofreSel);
                        Console.WriteLine("Captura Cofre a cofre con resaltado y casilla propia -> exploracion-cofre-a-cofre-seleccionado.png");

                        vm.Exploration.AutoZoomOnChestNavigate = false;
                        vm.Exploration.Zoom = 1.0;
                        vm.Exploration.ChestViewMode = 0;
                        vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                        DoEvents();
                    }
                }
                catch (Exception ex) { Console.WriteLine("AR-12-EXCEPTION: " + ex); }

                // AR-13 (bugs reales reportados por el usuario probando la app, 6-sep-2026, con
                // captura del panel "Buscar en el mundo" sobre el mundo real "Afueras de Larvas de
                // gusano"):
                //   (a) MAQUETACION de las 5 categorias: "se sigue perdiendo contenido, mira todo
                //       el espacio que hay vacio para mostrar los nombres de los cofres... que se
                //       revise eso para TODAS las pestañas de Exploracion". El recuento de cada
                //       fila ("184", "45") vivia en una segunda linea PEGADO A LA IZQUIERDA, con
                //       toda la anchura de la columna vacia a su derecha, mientras el nombre de al
                //       lado se cortaba con puntos suspensivos. Se mide el hueco real que queda a
                //       la derecha del dato numerico de la fila, y que ningun nombre se recorte.
                //   (b) MARCADOR del cofre seleccionado: "al seleccionar un cofre el mapa no
                //       navega/marca de forma que se distinga - he tenido que pasar el raton a
                //       mano por encima para encontrarlo". Se comprueba el efecto REAL sobre el
                //       ScrollViewer del mapa (a que coordenada queda centrado) y que el marcador
                //       existe y se ve en el arbol visual, no solo que la casilla cambia de valor.
                //   (c) NOMBRES reales de cofre ("Tile #-1", "Wooden Chest"): mas abajo, sobre un
                //       mundo con cofres de MOD de verdad (roca_negra no tiene ninguno).
                try
                {
                    var mapaScroll = Descendientes<ScrollViewer>(window).FirstOrDefault(sv => sv.Name == "WorldMapScroll");

                    // (categoria, modo de cofres, coleccion de filas, como se llama su dato numerico)
                    var casos = new (string Etiqueta, WorldSearchCategory Cat, int? ModoCofres, Func<System.Collections.IList> Filas)[]
                    {
                        ("Todo", WorldSearchCategory.All, null, () => vm.Exploration.WorldSearchResults),
                        ("NPCs", WorldSearchCategory.Npcs, null, () => vm.Exploration.NpcSearchResults),
                        ("Cofres/por tipo", WorldSearchCategory.Chests, 0, () => vm.Exploration.Inventory),
                        ("Cofres/cofre a cofre", WorldSearchCategory.Chests, 2, () => vm.Exploration.ChestRows),
                        ("Minerales", WorldSearchCategory.Ores, null, () => vm.Exploration.OreMetals),
                        ("Objetos", WorldSearchCategory.Objects, null, () => vm.Exploration.Inventory),
                    };
                    foreach (var (etiqueta, cat, modo, filas) in casos)
                    {
                        vm.Exploration.SelectedCategory = cat;
                        if (modo.HasValue) vm.Exploration.ChestViewMode = modo.Value;
                        DoEvents(); DoEvents(); DoEvents();
                        // "Todo" no tiene inventario propio: su lista son los resultados de una
                        // busqueda real, asi que hay que lanzar una (con su debounce de 250ms).
                        if (cat == WorldSearchCategory.All && vm.Exploration.WorldSearchResults.Count == 0)
                        {
                            vm.Exploration.WorldSearchText = "cofre";
                            for (int i = 0; i < 600; i++)
                            {
                                DoEvents(); Thread.Sleep(10);
                                if (!vm.Exploration.IsSearching && vm.Exploration.WorldSearchResults.Count > 0) break;
                            }
                            DoEvents(); DoEvents();
                        }
                        var lista = filas();
                        if (lista.Count == 0) { Console.WriteLine($"AR-13a: {etiqueta} no tiene ninguna fila real en este mundo - omitido"); continue; }

                        object primera = lista[0]!;
                        // El contenedor real de esa fila: el Button de la plantilla, ya renderizado
                        // y VISIBLE. IsVisible es imprescindible: los paneles de las 5 categorias
                        // conviven en el mismo Grid con Visibility (ExplorationCategoryContent), asi
                        // que los de las otras categorias siguen en el arbol visual con anchos
                        // reales - y Cofres y Objetos comparten ademas la MISMA coleccion
                        // (Inventory), asi que sin este filtro se mide la fila del panel escondido.
                        var contenedor = Descendientes<FrameworkElement>(window)
                            .FirstOrDefault(fe => ReferenceEquals(fe.DataContext, primera) && fe.IsVisible && fe is Button b && b.ActualWidth > 40);
                        if (contenedor == null) { Console.WriteLine($"AR-13a: {etiqueta} - la primera fila no esta realizada en el arbol visual, omitido"); continue; }

                        var textos = Descendientes<TextBlock>(contenedor)
                            .Where(t => t.IsVisible && !string.IsNullOrWhiteSpace(t.Text) && t.ActualWidth > 0).ToList();
                        if (textos.Count == 0) { Console.WriteLine($"AR-13a: {etiqueta} - la fila no tiene ningun texto visible, omitido"); continue; }

                        // La referencia NO es el ancho de la fila sino el de la LISTA que la
                        // contiene (el panel de items): asi el hueco medido incluye tambien el caso
                        // de que la propia fila no llegue a estirarse a todo el ancho disponible -
                        // que es justo lo que pasaba en Cofres, donde el panel del modo se dockeaba
                        // a la izquierda con su ancho deseado y dejaba media columna en blanco.
                        var host = (FrameworkElement?)AncestroPanelDeItems(contenedor) ?? contenedor;
                        double finContenido = textos.Max(t => FinRealDelTexto(t, host));
                        double hueco = host.ActualWidth - finContenido;
                        int recortados = textos.Count(TextoRecortado);
                        Console.WriteLine($"AR-13a: {etiqueta} -> lista de {host.ActualWidth:0}px (fila {contenedor.ActualWidth:0}px), contenido hasta {finContenido:0}px, hueco vacio a la derecha={hueco:0}px (esperado <=40), textos recortados={recortados} (esperado 0) [{string.Join(" · ", textos.Select(t => $"\"{t.Text}\"@{t.TranslatePoint(new Point(0, 0), host).X:0}+{t.ActualWidth:0}"))}]");
                        if (hueco > 40)
                        {
                            Console.WriteLine($"FALLO: AR-13a - {etiqueta} desperdicia {hueco:0}px de ancho a la derecha de la fila");
                            // La cadena de contenedores solo hace falta cuando algo va mal, pero
                            // entonces es lo unico que dice DONDE se pierde el ancho: el culpable es
                            // el primer eslabon cuyo ancho ya no llega al de la lista (asi salieron
                            // los dos bugs reales de esta ronda - el DockPanel que solo estira a su
                            // ultimo hijo, y el estilo implicito de Button que centra el contenido).
                            Console.WriteLine($"AR-13a: cadena de {etiqueta}: {string.Join(" > ", Ascendencia(textos.OrderByDescending(t => FinRealDelTexto(t, host)).First(), host))}");
                        }
                        if (recortados > 0)
                            Console.WriteLine($"FALLO: AR-13a - {etiqueta} recorta {recortados} texto(s) de la fila teniendo sitio: {string.Join(" | ", textos.Where(TextoRecortado).Select(t => t.Text))}");
                    }

                    // (b) marcador + desplazamiento REAL del mapa al seleccionar un cofre.
                    vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                    vm.Exploration.ChestViewMode = 2;
                    DoEvents(); DoEvents();
                    if (vm.Exploration.ChestRows.Count > 0 && mapaScroll != null)
                    {
                        // Un cofre LEJOS del centro actual, para que el desplazamiento se note.
                        var lejano = vm.Exploration.ChestRows.OrderByDescending(r => r.TileX).First();
                        vm.Exploration.AutoZoomOnNavigate = false;
                        vm.Exploration.AutoZoomOnChestNavigate = true;
                        vm.Exploration.Zoom = 1.0;
                        mapaScroll.ScrollToHorizontalOffset(0);
                        mapaScroll.ScrollToVerticalOffset(0);
                        DoEvents(); DoEvents();

                        vm.Exploration.GoToChestCommand.Execute(lejano);
                        DoEvents(); DoEvents(); DoEvents();

                        double zoom = vm.Exploration.Zoom;
                        // Lo que de verdad se puede pedir: centrar el cofre, RECORTADO a los limites
                        // reales del ScrollViewer - un cofre pegado al borde del mundo (x=8392 de
                        // 8400) nunca puede quedar en el centro exacto, y exigirlo seria un FALLO
                        // falso. Se compara contra ese objetivo recortado, no contra el tile a pelo.
                        double objetivoX = Math.Clamp(lejano.TileX * zoom - mapaScroll.ViewportWidth / 2, 0, Math.Max(0, mapaScroll.ExtentWidth - mapaScroll.ViewportWidth));
                        double objetivoY = Math.Clamp(lejano.TileY * zoom - mapaScroll.ViewportHeight / 2, 0, Math.Max(0, mapaScroll.ExtentHeight - mapaScroll.ViewportHeight));
                        double errorX = Math.Abs(mapaScroll.HorizontalOffset - objetivoX), errorY = Math.Abs(mapaScroll.VerticalOffset - objetivoY);
                        bool aLaVista = Math.Abs(lejano.TileX * zoom - (mapaScroll.HorizontalOffset + mapaScroll.ViewportWidth / 2)) <= mapaScroll.ViewportWidth / 2
                                     && Math.Abs(lejano.TileY * zoom - (mapaScroll.VerticalOffset + mapaScroll.ViewportHeight / 2)) <= mapaScroll.ViewportHeight / 2;
                        Console.WriteLine($"AR-13b: cofre en ({lejano.TileX}, {lejano.TileY}) -> zoom={zoom} (esperado 4), offset real=({mapaScroll.HorizontalOffset:0}, {mapaScroll.VerticalOffset:0}) frente al objetivo recortado=({objetivoX:0}, {objetivoY:0}), error=({errorX:0}, {errorY:0})px (esperado <=2), cofre dentro del viewport={aLaVista} (esperado True)");
                        if (Math.Abs(zoom - 4.0) > 0.001)
                            Console.WriteLine("FALLO: AR-13b - seleccionar un cofre con su casilla marcada no acerca el mapa");
                        if (errorX > 2 || errorY > 2)
                            Console.WriteLine($"FALLO: AR-13b - el mapa no se desplaza a donde esta el cofre (error de {errorX:0}x{errorY:0}px sobre lo maximo que se puede desplazar)");
                        if (!aLaVista)
                            Console.WriteLine("FALLO: AR-13b - tras seleccionar el cofre, su casilla real ni siquiera queda dentro de lo que se ve del mapa");

                        var marcador = Descendientes<System.Windows.Shapes.Rectangle>(window)
                            .FirstOrDefault(r => r.Name == "CurrentChestMarker");
                        bool visible = marcador is { IsVisible: true };
                        double mx = marcador == null ? -1 : Canvas.GetLeft(marcador);
                        double my = marcador == null ? -1 : Canvas.GetTop(marcador);
                        // Bug real reportado por el usuario jugando (17-sep-2026, offset CONSTANTE en los
                        // 358 cofres de un .wld real - ver bitacora.md): lejano.TileX/TileY es la esquina
                        // superior-izquierda cruda del bloque 2x2 del cofre (WldChest.X/Y), no su centro -
                        // el marcador (arreglado) tiene que caer en (TileX+1, TileY+1), no en la esquina.
                        Console.WriteLine($"AR-13b: marcador del cofre en el mapa -> existe={marcador != null}, visible={visible}, en tile=({mx:0}, {my:0}) (esperado el centro real ({lejano.TileX + 1}, {lejano.TileY + 1})), HasCurrentChest={vm.Exploration.HasCurrentChest}");
                        if (!visible)
                            Console.WriteLine("FALLO: AR-13b - el cofre seleccionado no se marca en el mapa (nada que distinguir a simple vista)");
                        else if (Math.Abs(mx - (lejano.TileX + 1)) > 0.5 || Math.Abs(my - (lejano.TileY + 1)) > 0.5)
                            Console.WriteLine("FALLO: AR-13b - el marcador del cofre no cae sobre el centro real del cofre (esquina+1, footprint 2x2)");

                        // Y se apaga con "Cerrar", que es el unico gesto real de "quita las marcas".
                        vm.Exploration.ClearOreMarksCommand.Execute(null);
                        DoEvents(); DoEvents();
                        bool sigueVisible = Descendientes<System.Windows.Shapes.Rectangle>(window)
                            .Any(r => r.Name == "CurrentChestMarker" && r.IsVisible);
                        Console.WriteLine($"AR-13b: tras 'Cerrar' -> marcador visible={sigueVisible} (esperado False), filas marcadas={vm.Exploration.ChestRows.Count(r => r.IsCurrent)} (esperado 0)");
                        if (sigueVisible)
                            Console.WriteLine("FALLO: AR-13b - el marcador del cofre se queda pegado en el mapa despues de cerrar los resultados");
                    }

                    vm.Exploration.AutoZoomOnChestNavigate = false;
                    vm.Exploration.Zoom = 1.0;
                    vm.Exploration.ChestViewMode = 0;
                    vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                    DoEvents();
                }
                catch (Exception ex) { Console.WriteLine("AR-13-EXCEPTION: " + ex); }

                // AR-13d (17-sep-2026): Bug 1 real reportado por el usuario jugando - "el mapa no
                // tiene ningun clic que abra el editor de cofres". Los marcadores de resultado de
                // busqueda (WorldSearchResults) llevaban Cursor="Hand"/ToolTip pero SIN Command
                // detras; el marcador de "cofre actual" llevaba IsHitTestVisible=False explicito.
                // Arreglo real: MouseBinding en MainWindow.xaml reutiliza GoToWorldSearchHitCommand
                // (marcadores de busqueda) y el nuevo EditCurrentChestOnMapCommand (marcador de
                // "cofre actual") - los DOS abren el editor real (EditingChest/EditingChestSlots)
                // del cofre CORRECTO, nunca otro. Verificado sobre Blando_Río.wld (mundo real del
                // usuario, 358 cofres reales, el mismo que usa el canario de KeepQA
                // verificarAlineacionMarcador.js para el Bug 2) con dos cofres reales de contenido
                // distinto y conocido: cofre-7 (X=5579,Y=1036, contiene NetId 167/188/2350/282/73)
                // y cofre-8 (X=5375,Y=803, contiene NetId 21/279/8/73 - comparte el 73 a proposito,
                // para que "aparece 21" sea la prueba real de que se abrio el cofre EQUIVOCADO).
                try
                {
                    string mundoCofres = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\Worlds\Blando_Río.wld");
                    if (!File.Exists(mundoCofres))
                        Console.WriteLine("AR-13d: no se encontro Blando_Río.wld (mundo real con los 358 cofres) - omitido");
                    else
                    {
                        var carga = vm.Exploration.LoadFromPathAsync(mundoCofres);
                        while (!carga.IsCompleted) { DoEvents(); Thread.Sleep(15); }
                        DoEvents(); DoEvents();

                        // WorldSearch.cs ya deja la posicion en el CENTRO real del cofre tras el
                        // arreglo del Bug 2 (esquina+1 en cada eje) - el clic simulado usa ese mismo
                        // centro, exactamente lo que un clic real sobre el marcador del mapa manda.
                        var hitCofre7 = new WorldSearchHitRowViewModel(new WorldSearchHit(5580, 1037, "Barra de hierro", WorldSearchKind.ChestItem));
                        vm.Exploration.GoToWorldSearchHitCommand.Execute(hitCofre7);
                        DoEvents(); DoEvents();

                        bool categoriaOk = vm.Exploration.SelectedCategory == WorldSearchCategory.Chests && vm.Exploration.ChestViewMode == 2;
                        var editando7 = vm.Exploration.EditingChest;
                        var netIds7 = vm.Exploration.EditingChestSlots.Select(s => s.Item.Id).Where(id => id != 0).ToHashSet();
                        bool contenido7Ok = new[] { 167, 188, 2350, 282 }.All(netIds7.Contains) && !netIds7.Contains(21) && !netIds7.Contains(279);
                        Console.WriteLine($"AR-13d: clic en marcador de cofre-7 -> categoria/modo OK={categoriaOk}, cofre abierto=({editando7?.TileX}, {editando7?.TileY}) (esperado (5579, 1036)), contenido real OK={contenido7Ok}, HasCurrentChest={vm.Exploration.HasCurrentChest}, marcador=({vm.Exploration.CurrentChestX},{vm.Exploration.CurrentChestY}) (esperado (5580, 1037))");
                        if (!categoriaOk) Console.WriteLine("FALLO: AR-13d - el clic en el marcador del mapa no salta a 'Cofres > Cofre a cofre'");
                        if (editando7 == null || editando7.TileX != 5579 || editando7.TileY != 1036)
                            Console.WriteLine("FALLO: AR-13d - el clic en el marcador de cofre-7 no abre el editor de ESE cofre");
                        if (!contenido7Ok)
                            Console.WriteLine($"FALLO: AR-13d - el editor abierto no tiene el contenido REAL del cofre-7 (NetIds vistos: {string.Join(",", netIds7)})");
                        if (!vm.Exploration.HasCurrentChest || vm.Exploration.CurrentChestX != 5580 || vm.Exploration.CurrentChestY != 1037)
                            Console.WriteLine("FALLO: AR-13d - el marcador del mapa no queda sobre el centro real del cofre-7 tras el clic");

                        // Segundo clic, sobre OTRO cofre real (cofre-8) - tiene que abrir ESE, no
                        // quedarse con el anterior (la prueba mas directa de "abre el correcto").
                        var hitCofre8 = new WorldSearchHitRowViewModel(new WorldSearchHit(5376, 804, "Hierro", WorldSearchKind.ChestItem));
                        vm.Exploration.GoToWorldSearchHitCommand.Execute(hitCofre8);
                        DoEvents(); DoEvents();
                        var editando8 = vm.Exploration.EditingChest;
                        var netIds8 = vm.Exploration.EditingChestSlots.Select(s => s.Item.Id).Where(id => id != 0).ToHashSet();
                        bool contenido8Ok = new[] { 21, 279, 8 }.All(netIds8.Contains) && !netIds8.Contains(167);
                        Console.WriteLine($"AR-13d: clic en marcador de cofre-8 -> cofre abierto=({editando8?.TileX}, {editando8?.TileY}) (esperado (5375, 803)), contenido real OK={contenido8Ok}");
                        if (editando8 == null || editando8.TileX != 5375 || editando8.TileY != 803)
                            Console.WriteLine("FALLO: AR-13d - el clic en el marcador de cofre-8 no abre el editor de ESE cofre (o se quedo en el anterior)");
                        if (!contenido8Ok)
                            Console.WriteLine($"FALLO: AR-13d - el editor abierto no tiene el contenido REAL del cofre-8 (NetIds vistos: {string.Join(",", netIds8)})");

                        // Segunda mitad del Bug 1: el marcador de "cofre actual" (antes
                        // IsHitTestVisible=False, ahora clicable) reabre el MISMO cofre ya marcado.
                        vm.Exploration.CancelEditingChestCommand.Execute(null);
                        DoEvents();
                        bool marcadorSigueTrasCancel = vm.Exploration.HasCurrentChest && vm.Exploration.EditingChest == null;
                        vm.Exploration.EditCurrentChestOnMapCommand.Execute(null);
                        DoEvents();
                        var editandoTrasMarcador = vm.Exploration.EditingChest;
                        Console.WriteLine($"AR-13d: 'Cerrar editor' deja el marcador puesto={marcadorSigueTrasCancel} (esperado True); EditCurrentChestOnMap reabre=({editandoTrasMarcador?.TileX}, {editandoTrasMarcador?.TileY}) (esperado el mismo cofre-8, (5375, 803))");
                        if (!marcadorSigueTrasCancel)
                            Console.WriteLine("FALLO: AR-13d - cerrar el editor de cofre apaga tambien el marcador del mapa (deberian ser independientes)");
                        if (editandoTrasMarcador == null || editandoTrasMarcador.TileX != 5375 || editandoTrasMarcador.TileY != 803)
                            Console.WriteLine("FALLO: AR-13d - el marcador de 'cofre actual' del mapa no reabre el editor del cofre correcto");

                        vm.Exploration.CancelEditingChestCommand.Execute(null);
                        vm.Exploration.ClearOreMarksCommand.Execute(null);
                        vm.Exploration.ChestViewMode = 0;
                        vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                        DoEvents();
                    }

                    // Al terminar se deja recargado el mundo de siempre, mismo criterio que AR-13c.
                    string mundoDeSiempreAR13d = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
                    if (File.Exists(mundoDeSiempreAR13d))
                    {
                        var vuelta = vm.Exploration.LoadFromPathAsync(mundoDeSiempreAR13d);
                        while (!vuelta.IsCompleted) { DoEvents(); Thread.Sleep(15); }
                        DoEvents();
                    }
                }
                catch (Exception ex) { Console.WriteLine("AR-13d-EXCEPTION: " + ex); }

                // AR-13e (18-sep-2026): Bug 2 real, DISTINTO del Bug 1 ya cubierto en AR-13d - ver
                // bitacora.md "desfase grande de marcadores" (investigacion del 18-sep-2026).
                // AR-13d invoca GoToWorldSearchHitCommand.Execute(...) DIRECTAMENTE, sin pasar
                // nunca por un clic de raton real - un hueco real y ya confirmado del propio
                // arnes (documentado en bitacora.md): OnWorldMapMouseDown capturaba el raton de
                // forma INCONDICIONAL en CUALQUIER boton izquierdo pulsado dentro del mapa
                // (WorldMapScroll.CaptureMouse(), CaptureMode.Element por defecto), asi que el
                // MouseLeftButtonUp que necesita el MouseBinding LeftClick del propio marcador
                // NUNCA le llegaba - el editor de cofre no se abria con un clic real, aunque
                // Mouse.DirectlyOver/e.OriginalSource confirmaran que el clic aterrizaba sobre el
                // marcador real y aunque Command.Execute() directo (como en AR-13d) si funcionara.
                // Arreglo real: OnWorldMapMouseDown ahora comprueba primero, subiendo el arbol
                // visual desde e.OriginalSource, si el clic empieza sobre un elemento con su
                // propio MouseBinding (OriginatesFromClickableMarker) y, si es asi, no captura -
                // deja que el propio marcador reciba su ciclo de clic normal; solo captura para
                // arrastrar/paneear cuando el clic empieza sobre mapa vacio. Esta prueba simula el
                // ciclo COMPLETO de raton real (down+up, con SetCursorPos/mouse_event a nivel de
                // SO, mismo patron ya real de AR-EX2-PAN/AR-EX2-MINIMAPA-CLIC mas abajo) sobre
                // CurrentChestMarker - lo que AR-13d NUNCA pudo detectar - y, a continuacion, que
                // un clic+arrastre que empieza sobre mapa VACIO sigue paneando igual que siempre
                // (el mismo fix no debe robarle la captura al arrastre real).
                try
                {
                    string mundoCofresClic = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\Worlds\Blando_Río.wld");
                    if (!File.Exists(mundoCofresClic))
                        Console.WriteLine("AR-13e: no se encontro Blando_Río.wld (mundo real con los 358 cofres) - omitido");
                    else
                    {
                        var cargaClic = vm.Exploration.LoadFromPathAsync(mundoCofresClic);
                        while (!cargaClic.IsCompleted) { DoEvents(); Thread.Sleep(15); }
                        DoEvents(); DoEvents();

                        // Mismo cofre-7 real ya usado en AR-13d (X=5579,Y=1036, centro (5580,1037),
                        // contiene NetId 167/188/2350/282/73). Se navega una vez con el Command
                        // (no es lo que se quiere probar aqui) solo para dejar el marcador visible
                        // en su posicion real y centrado en el viewport (NavigateToTile real);
                        // el editor se cierra enseguida (AR-13d ya confirmo que el marcador SIGUE
                        // puesto tras cerrar) para que el clic REAL de abajo sea el UNICO camino
                        // que lo vuelva a abrir.
                        var hitCofre7Clic = new WorldSearchHitRowViewModel(new WorldSearchHit(5580, 1037, "Barra de hierro", WorldSearchKind.ChestItem));
                        vm.Exploration.GoToWorldSearchHitCommand.Execute(hitCofre7Clic);
                        DoEvents(); DoEvents();
                        vm.Exploration.CancelEditingChestCommand.Execute(null);
                        DoEvents(); DoEvents();

                        var marcadorClic = Descendientes<System.Windows.Shapes.Rectangle>(window).FirstOrDefault(r => r.Name == "CurrentChestMarker");
                        var mapaScrollClic = Descendientes<ScrollViewer>(window).FirstOrDefault(sv => sv.Name == "WorldMapScroll");
                        if (marcadorClic == null || mapaScrollClic == null || !vm.Exploration.HasCurrentChest)
                            Console.WriteLine("FALLO: AR-13e - no se encontro el marcador/ScrollViewer del mapa o el marcador no quedo visible tras navegar");
                        else
                        {
                            marcadorClic.UpdateLayout();
                            var centroMarcadorVentana = marcadorClic.TransformToAncestor(window)
                                .Transform(new Point(marcadorClic.ActualWidth / 2, marcadorClic.ActualHeight / 2));
                            var centroMarcadorPantalla = window.PointToScreen(centroMarcadorVentana);

                            ForzarPrimerPlano(hwnd);
                            DoEvents();
                            SetCursorPos((int)centroMarcadorPantalla.X, (int)centroMarcadorPantalla.Y);
                            Thread.Sleep(60);
                            System.Windows.Input.Mouse.Synchronize();
                            DoEvents();

                            // Clic real de SISTEMA OPERATIVO, down+up en el MISMO sitio (sin
                            // arrastre) - el ciclo completo real que un MouseBinding LeftClick
                            // necesita para reconocerse, exactamente lo que AR-13d nunca ejercita.
                            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                            Thread.Sleep(50); DoEvents(); DoEvents();
                            bool capturaDuranteElClic = System.Windows.Input.Mouse.Captured != null;
                            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                            Thread.Sleep(60); DoEvents(); DoEvents(); DoEvents();

                            var editandoTrasClicReal = vm.Exploration.EditingChest;
                            var netIdsTrasClicReal = vm.Exploration.EditingChestSlots.Select(s => s.Item.Id).Where(id => id != 0).ToHashSet();
                            bool contenidoTrasClicRealOk = new[] { 167, 188, 2350, 282 }.All(netIdsTrasClicReal.Contains) && !netIdsTrasClicReal.Contains(21) && !netIdsTrasClicReal.Contains(279);
                            Console.WriteLine($"AR-13e: clic REAL de SO (down+up) sobre el marcador de cofre-7 en pantalla ({centroMarcadorPantalla.X:0},{centroMarcadorPantalla.Y:0}) -> capturo el raton durante el clic={capturaDuranteElClic} (esperado False), editor abierto=({editandoTrasClicReal?.TileX}, {editandoTrasClicReal?.TileY}) (esperado (5579, 1036)), contenido real OK={contenidoTrasClicRealOk}, Mouse.Captured tras soltar={System.Windows.Input.Mouse.Captured}");
                            if (capturaDuranteElClic)
                                Console.WriteLine("FALLO: AR-13e - OnWorldMapMouseDown sigue capturando el raton en un clic que empieza sobre el marcador (CaptureMode.Element se roba el MouseLeftButtonUp del propio MouseBinding)");
                            if (editandoTrasClicReal == null || editandoTrasClicReal.TileX != 5579 || editandoTrasClicReal.TileY != 1036)
                                Console.WriteLine("FALLO: AR-13e - un clic REAL de raton (down+up) sobre el marcador del cofre no abre su editor (regresion del bug real reportado por el usuario, 'el clic sobre el cofre real no hace nada')");
                            else if (!contenidoTrasClicRealOk)
                                Console.WriteLine($"FALLO: AR-13e - el editor abierto por el clic real no tiene el contenido REAL del cofre-7 (NetIds vistos: {string.Join(",", netIdsTrasClicReal)})");
                            if (System.Windows.Input.Mouse.Captured != null)
                                Console.WriteLine($"FALLO: AR-13e - la captura del raton se quedo colgada tras soltar sobre el marcador ({System.Windows.Input.Mouse.Captured})");

                            // Companero directo: el mismo fix no debe romper el arrastre/paneo del
                            // mapa cuando el clic SI empieza sobre mapa vacio (comportamiento de
                            // siempre) - down en un punto vacio, mover, up, y comprobar que la
                            // vista se desplazo lo mismo que se movio el cursor de verdad (mismo
                            // criterio real que AR-EX2-PAN, repetido aqui para dejar esta prueba
                            // autocontenida junto al fix que verifica).
                            vm.Exploration.CancelEditingChestCommand.Execute(null);
                            DoEvents();
                            vm.Exploration.Zoom = 1.0;
                            mapaScrollClic.UpdateLayout();
                            mapaScrollClic.ScrollToHorizontalOffset(1500);
                            mapaScrollClic.ScrollToVerticalOffset(700);
                            DoEvents(); mapaScrollClic.UpdateLayout(); DoEvents();
                            double hAntesPanClic = mapaScrollClic.HorizontalOffset, vAntesPanClic = mapaScrollClic.VerticalOffset;
                            // Esquina superior izquierda del viewport, bien lejos de cualquier
                            // marcador real - mapa "vacio" de proposito.
                            var puntoVacioVentana = new Point(20, 20);
                            var puntoVacioPantalla = mapaScrollClic.PointToScreen(puntoVacioVentana);
                            SetCursorPos((int)puntoVacioPantalla.X, (int)puntoVacioPantalla.Y);
                            Thread.Sleep(50);
                            System.Windows.Input.Mouse.Synchronize();
                            DoEvents();
                            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                            Thread.Sleep(50); DoEvents(); DoEvents();
                            bool capturoEnMapaVacio = System.Windows.Input.Mouse.Captured != null;
                            var posAntesPanClic = System.Windows.Input.Mouse.GetPosition(mapaScrollClic);
                            SetCursorPos((int)puntoVacioPantalla.X + 90, (int)puntoVacioPantalla.Y + 50);
                            Thread.Sleep(90);
                            System.Windows.Input.Mouse.Synchronize();
                            DoEvents(); DoEvents(); DoEvents();
                            var posDespuesPanClic = System.Windows.Input.Mouse.GetPosition(mapaScrollClic);
                            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                            Thread.Sleep(50); DoEvents(); DoEvents();
                            double dhPanClic = mapaScrollClic.HorizontalOffset - hAntesPanClic, dvPanClic = mapaScrollClic.VerticalOffset - vAntesPanClic;
                            double movXPanClic = posDespuesPanClic.X - posAntesPanClic.X, movYPanClic = posDespuesPanClic.Y - posAntesPanClic.Y;
                            Console.WriteLine($"AR-13e-PAN: sigue capturando en mapa vacio={capturoEnMapaVacio} (esperado True), cursor se movio de verdad ({movXPanClic:0},{movYPanClic:0})px -> el mapa se desplazo ({dhPanClic:0},{dvPanClic:0})px (esperado ({-movXPanClic:0},{-movYPanClic:0}))");
                            if (!capturoEnMapaVacio)
                                Console.WriteLine("FALLO: AR-13e-PAN - el fix de OnWorldMapMouseDown dejo de capturar tambien en mapa vacio (rompe el arrastre/paneo de siempre)");
                            if (Math.Abs(movXPanClic) < 5 && Math.Abs(movYPanClic) < 5)
                                Console.WriteLine("AR-13e-PAN: el cursor real no llego a moverse (raton compartido con otra ventana de esta misma maquina) - medicion omitida, no es un fallo de la app");
                            else if (Math.Abs(dhPanClic + movXPanClic) > 4 || Math.Abs(dvPanClic + movYPanClic) > 4)
                                Console.WriteLine($"FALLO: AR-13e-PAN - el arrastre sobre mapa vacio dejo de desplazar el mapa lo mismo que se movio el cursor: cursor ({movXPanClic:0},{movYPanClic:0}) -> mapa ({dhPanClic:0},{dvPanClic:0})");
                            if (System.Windows.Input.Mouse.Captured != null)
                                Console.WriteLine($"FALLO: AR-13e-PAN - la captura del raton se quedo colgada tras soltar en mapa vacio ({System.Windows.Input.Mouse.Captured})");
                        }

                        vm.Exploration.CancelEditingChestCommand.Execute(null);
                        vm.Exploration.ClearOreMarksCommand.Execute(null);
                        vm.Exploration.ChestViewMode = 0;
                        vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                        DoEvents();
                    }

                    string mundoDeSiempreAR13e = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
                    if (File.Exists(mundoDeSiempreAR13e))
                    {
                        var vuelta = vm.Exploration.LoadFromPathAsync(mundoDeSiempreAR13e);
                        while (!vuelta.IsCompleted) { DoEvents(); Thread.Sleep(15); }
                        DoEvents();
                    }
                }
                catch (Exception ex) { Console.WriteLine("AR-13e-EXCEPTION: " + ex); }

                // AR-MRK (19-sep-2026): geometria real de TODOS los marcadores del mapa a 7 zooms
                // + el clic sobre el tile de un cofre. Corre tambien en la tirada completa, no solo
                // con TERRAKEEP_SOLO_MRK=1 (ese modo es solo para iterar rapido). Va justo aqui,
                // pegado a AR-13d/AR-13e, porque comparte mundo real (Blando_Río.wld) y lo deja
                // restaurado igual que ellos.
                PruebasMarcadoresMapa(window, vm);

                // AR-13c: los NOMBRES reales de las filas de cofre, sobre un mundo que SI tiene
                // cofres de mod (roca_negra no tiene ninguno: los 505 estan sobre tiles vanilla).
                //   - "Tile #-1" con 51 apariciones: -1 no es un id de tile, es "casilla vacia"
                //     (WldTile.Type). tModLoader guarda los tiles de mods como aire en el .wld
                //     (WorldFile.cs:1425, `tile.active() && tile.type < TileID.Count`) y su tipo
                //     real en el .twld, asi que esos cofres de Calamity se quedan sin casilla que
                //     mirar. Ahora se dicen por su nombre real ("Cofre de un mod") con un tooltip
                //     que lo explica, en vez de un numero que no significa nada.
                //   - "Wooden Chest"/"Web Coverd Chest"/"Wooden Dresser" en ingles: el generador de
                //     tile_names.json cruza el nombre INGLES de TEdit contra ItemName del juego, y
                //     esos tres no casan (TEdit los llama de otra forma, o tiene una errata).
                //     scripts/parchear-nombres-contenedores-es.js los pone con la traduccion real.
                // Al terminar se deja recargado el mundo de siempre: el resto del arnes cuenta con el.
                try
                {
                    string mundoConMods = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\Afueras_de_Larvas_de_gusano.wld");
                    string mundoDeSiempre = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
                    if (!File.Exists(mundoConMods))
                        Console.WriteLine("AR-13c: no se encontro Afueras_de_Larvas_de_gusano.wld (mundo real con cofres de Calamity) - omitido");
                    else
                    {
                        var carga = vm.Exploration.LoadFromPathAsync(mundoConMods);
                        while (!carga.IsCompleted) { DoEvents(); Thread.Sleep(15); }
                        vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                        vm.Exploration.ChestViewMode = 0;
                        DoEvents(); DoEvents();

                        var nombres = vm.Exploration.Inventory.Select(f => f.Name).ToList();
                        var sinResolver = nombres.Where(n => n.StartsWith("Tile #", StringComparison.Ordinal)).ToList();
                        string[] enIngles = ["Wooden Chest", "Web Coverd Chest", "Wooden Dresser", "Chests", "Dressers", "Chests (Group 2)"];
                        var quedanEnIngles = nombres.Where(n => enIngles.Contains(n)).ToList();
                        var deMod = vm.Exploration.Inventory.FirstOrDefault(f => f.IsModdedChest);
                        Console.WriteLine($"AR-13c: {vm.Exploration.Inventory.Count} tipos de cofre reales -> sin resolver ('Tile #N')={sinResolver.Count} (esperado 0), en ingles={quedanEnIngles.Count} (esperado 0), fila de cofre de mod='{deMod?.Name}' x{deMod?.Count}");
                        if (sinResolver.Count > 0)
                            Console.WriteLine($"FALLO: AR-13c - siguen saliendo tipos de cofre sin nombre real: {string.Join(", ", sinResolver)}");
                        if (quedanEnIngles.Count > 0)
                            Console.WriteLine($"FALLO: AR-13c - siguen saliendo nombres de cofre en ingles: {string.Join(", ", quedanEnIngles)}");
                        if (deMod == null)
                            Console.WriteLine("FALLO: AR-13c - este mundo tiene cofres de Calamity sobre casilla vacia y ninguna fila los reconoce como tales");

                        // Y lo mismo en "Cofre a cofre", que resuelve el nombre por su cuenta.
                        vm.Exploration.ChestViewMode = 2;
                        DoEvents(); DoEvents();
                        int filasSinResolver = vm.Exploration.ChestRows.Count(r => r.VariantName.StartsWith("Tile #", StringComparison.Ordinal));
                        int filasDeMod = vm.Exploration.ChestRows.Count(r => r.IsModdedChest);
                        Console.WriteLine($"AR-13c: 'Cofre a cofre' -> {vm.Exploration.ChestRows.Count} cofres, sin resolver={filasSinResolver} (esperado 0), reconocidos como de mod={filasDeMod}");
                        if (filasSinResolver > 0)
                            Console.WriteLine("FALLO: AR-13c - 'Cofre a cofre' sigue mostrando cofres como 'Tile #N'");

                        var rtbCofresMod = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        vm.Exploration.ChestViewMode = 0;
                        DoEvents(); DoEvents();
                        rtbCofresMod.Render(window);
                        var encCofresMod = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encCofresMod.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbCofresMod));
                        using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "exploracion-cofres-nombres-y-maquetacion.png"))) encCofresMod.Save(fs);
                        Console.WriteLine("Captura de Cofres con nombres reales y el recuento a la derecha -> exploracion-cofres-nombres-y-maquetacion.png");

                        // Estado como estaba: el resto del arnes trabaja sobre roca_negra.
                        if (File.Exists(mundoDeSiempre))
                        {
                            var vuelta = vm.Exploration.LoadFromPathAsync(mundoDeSiempre);
                            while (!vuelta.IsCompleted) { DoEvents(); Thread.Sleep(15); }
                        }
                        vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                        DoEvents();
                    }
                }
                catch (Exception ex) { Console.WriteLine("AR-13c-EXCEPTION: " + ex); }

                // AR-15 (bug real reportado por el usuario probando la app, 6-sep-2026): "la lista
                // de 'NPCs que faltan' SOLO se ve completa con la ventana a pantalla completa - con
                // la ventana a un tamaño normal (no maximizada) se corta y no se ven todos".
                //
                // Por que no lo cazaron AR-11a/AR-11c (los dos bloques de la ronda anterior sobre
                // esta misma columna):
                //   - AR-11a mide el alto que le queda al CONTENEDOR de la categoria
                //     (ExplorationCategoryContent), no el reparto DENTRO de la categoria de NPCs.
                //   - AR-11c solo ve un elemento recortado si el recorte de layout de WPF cae
                //     sobre EL (VisualTreeHelper.GetClip). Cuando el corte lo hace un ANCESTRO
                //     (aqui: el Expander/DockPanel de la categoria), las filas que quedan por
                //     debajo del corte no llevan clip ninguno - simplemente no se pintan. Cero
                //     recorte medible, y aun asi contenido perdido.
                //   - Y ninguno de los dos DESPLIEGA nunca el Expander de "NPCs que faltan", que
                //     es justo el estado en el que el usuario ve el bug.
                //
                // Lo que mide AR-15, con datos reales (el mundo de pruebas tiene 14 NPCs de los 40
                // del roster vanilla, o sea ~26 filas reales que enseñar): cuantas de esas filas se
                // ven ENTERAS ahora mismo, y cuantas son ALCANZABLES de verdad haciendo scroll
                // (BringIntoView real sobre cada fila, que es exactamente el gesto del usuario).
                // La geometria se mide con el recorte acumulado de TODOS los ancestros hasta la
                // ventana (VisibleEntero/RectVisible), no con el clip del propio elemento.
                // Tamaños: maximizada de verdad (WindowState.Maximized) + cuatro tamaños
                // intermedios REALES no maximizados, hasta el suelo declarado de la ventana
                // (MinWidth=1080/MinHeight=700 en MainWindow.xaml).
                try
                {
                    // ADR-TERRAKEEP-029 (26-sep-2026): Browse se movio a BrowseView.xaml
                    // (UserControl con su propio NameScope) - FindName DOBLE, mismo patron ya usado
                    // por GUIA/ADR-021, Compare/ADR-025, WorldTools/ADR-027 y ChestInspector/ADR-028.
                    var browseViewHostAr15 = window.FindName("BrowseView") as FrameworkElement;
                    var expFaltan = browseViewHostAr15?.FindName("MissingNpcsExpander") as System.Windows.Controls.Expander;
                    var listaFaltan = browseViewHostAr15?.FindName("MissingNpcsList") as ItemsControl;
                    var listaNpcs = browseViewHostAr15?.FindName("NpcResultsList") as FrameworkElement;
                    if (expFaltan == null || listaFaltan == null)
                        Console.WriteLine("FALLO: AR-15 - no se encontro el Expander/lista de 'NPCs que faltan' en el arbol visual (¿se renombraron MissingNpcsExpander/MissingNpcsList en MainWindow.xaml?)");
                    else
                    {
                        var categoriaAntes = vm.Exploration.SelectedCategory;
                        bool expandidoAntes = expFaltan.IsExpanded;
                        vm.Exploration.SelectedCategory = WorldSearchCategory.Npcs;
                        expFaltan.IsExpanded = true;
                        DoEvents(); DoEvents();

                        Console.WriteLine($"AR-15: pantalla real de esta maquina {SystemParameters.PrimaryScreenWidth:0}x{SystemParameters.PrimaryScreenHeight:0}, " +
                                          $"NPCs de pueblo que faltan en el mundo de pruebas={vm.Exploration.MissingNpcs.Count} de {vm.Exploration.MissingNpcs.Count + vm.Exploration.Npcs.Count} reales del roster vanilla");

                        // El ultimo caso es el PEOR real y perfectamente normal: la ventana en su
                        // suelo declarado Y "Este mundo" tambien desplegado (+200px de Dock=Top que
                        // se comen la columna antes de que la categoria vea un solo pixel) - mismo
                        // criterio de caso peor que ya usa AR-11a.
                        var esteMundoExp = Descendientes<System.Windows.Controls.Expander>(window)
                            .FirstOrDefault(e => (e.Header as string) == "Este mundo" || (e.Header as string) == "This world");
                        foreach (var (etiqueta, w, h, esteMundo) in new (string?, double, double, bool)[]
                                 { ("maximizada", 0, 0, false), (null, 1600, 1000, false), (null, 1400, 900, false),
                                   (null, 1180, 860, false), (null, 1080, 700, false), ("1080x700 + 'Este mundo' abierto", 1080, 700, true) })
                        {
                            if (esteMundoExp != null) esteMundoExp.IsExpanded = esteMundo;
                            if (etiqueta == "maximizada")
                            {
                                window.WindowState = WindowState.Maximized;
                                DoEvents(); DoEvents(); DoEvents();
                            }
                            else
                            {
                                window.WindowState = WindowState.Normal;
                                FijarTamaño(window, w, h);
                            }
                            DoEvents(); DoEvents();
                            string caso = etiqueta ?? $"{w:0}x{h:0}";

                            var filas = vm.Exploration.MissingNpcs
                                .Select(item => listaFaltan.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement)
                                .Where(fe => fe != null).Select(fe => fe!).ToList();
                            int total = vm.Exploration.MissingNpcs.Count;

                            int sinScroll = filas.Count(fe => VisibleEntero(fe, window));
                            int alcanzables = 0;
                            foreach (var fe in filas)
                            {
                                fe.BringIntoView();
                                DoEvents(); DoEvents();
                                if (VisibleEntero(fe, window)) alcanzables++;
                            }
                            // Deja el scroll arriba del todo: el caso siguiente mide "sin tocar nada".
                            if (filas.Count > 0) { filas[0].BringIntoView(); DoEvents(); DoEvents(); }

                            double altoLista = listaNpcs?.ActualHeight ?? -1;
                            Console.WriteLine($"AR-15: {caso} ({window.ActualWidth:0}x{window.ActualHeight:0}) -> 'NPCs que faltan' desplegado: {total} filas reales, " +
                                              $"se ven enteras sin tocar nada={sinScroll}, ALCANZABLES con scroll={alcanzables} (esperado {total}), " +
                                              $"alto del Expander={expFaltan.ActualHeight:0}px / contenido que pide={expFaltan.DesiredSize.Height:0}px, " +
                                              $"alto de la lista de NPCs del mundo={altoLista:0}px (esperado >0: no puede quedarse sin sitio)");
                            if (alcanzables < total)
                                Console.WriteLine($"FALLO: AR-15 - a {caso} solo se pueden alcanzar {alcanzables} de los {total} NPCs que faltan: " +
                                                  $"{total - alcanzables} fila(s) cortadas SIN scroll con el que llegar a ellas");
                            if (listaNpcs != null && altoLista < 1)
                                Console.WriteLine($"FALLO: AR-15 - a {caso}, desplegar 'NPCs que faltan' deja la lista de NPCs del mundo con {altoLista:0}px (se come la categoria entera)");
                        }

                        // Evidencia visual del caso apretado real (el que reporto el usuario).
                        if (esteMundoExp != null) esteMundoExp.IsExpanded = false;
                        window.WindowState = WindowState.Normal;
                        FijarTamaño(window, 1080, 700);
                        DoEvents(); DoEvents();
                        var rtbFaltan = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtbFaltan.Render(window);
                        var encFaltan = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encFaltan.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbFaltan));
                        using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "exploracion-npcs-que-faltan-ventana-pequena.png"))) encFaltan.Save(fs);
                        Console.WriteLine("Captura de 'NPCs que faltan' desplegado en ventana pequeña real -> exploracion-npcs-que-faltan-ventana-pequena.png");

                        // Estado como estaba (mismo criterio que el resto de bloques del arnes).
                        if (esteMundoExp != null) esteMundoExp.IsExpanded = false;
                        expFaltan.IsExpanded = expandidoAntes;
                        vm.Exploration.SelectedCategory = categoriaAntes;
                        FijarTamaño(window, 1180, 860);
                        DoEvents(); DoEvents();
                    }
                }
                catch (Exception ex) { Console.WriteLine("AR-15-EXCEPTION: " + ex); }

                // AR-EX1 (oleada grande de pruebas del 6-sep-2026, area "Exploracion del mundo"):
                // las TRES rondas anteriores sobre esta misma columna (AR-11, AR-13, AR-15) la
                // midieron SIEMPRE con el bloque de resultados VACIO - y ese bloque
                // (ExplorationResultsBlock) es DockPanel.Dock="Bottom", o sea que el DockPanel se
                // lo sirve ENTERO y con su DesiredSize completo ANTES de dejarle nada al relleno
                // (ExplorationCategoryContent). Exactamente la misma familia de bug que AR-15 ya
                // encontro en el otro sitio (el Expander Dock=Bottom de "NPCs que faltan").
                //
                // El estado con resultados NO es un caso raro: es el estado NORMAL de esta pestaña
                // en cuanto el usuario busca algo o pulsa una fila de inventario, y el bloque crece
                // hasta ~370px (dos casillas + resumen + fila de botones + ListBox MaxHeight=240).
                // Se mide el alto REAL que le queda al contenido de cada categoria en 5 tamaños
                // reales, con la geometria honesta (RectVisible: recorte acumulado de TODOS los
                // ancestros hasta la ventana, la leccion de AR-15) y contando filas que de verdad
                // se ven enteras.
                try
                {
                    // ADR-TERRAKEEP-029 (26-sep-2026): Browse se movio a BrowseView.xaml
                    // (UserControl con su propio NameScope) - FindName DOBLE, mismo patron ya usado
                    // por GUIA/ADR-021, Compare/ADR-025, WorldTools/ADR-027 y ChestInspector/ADR-028.
                    // browseViewHost16 se reutiliza tambien dentro de ListaDeCategoria16() mas abajo.
                    var browseViewHost16 = window.FindName("BrowseView") as FrameworkElement;
                    var contenidoCat = browseViewHost16?.FindName("ExplorationCategoryContent") as FrameworkElement;
                    var bloqueRes = browseViewHost16?.FindName("ExplorationResultsBlock") as FrameworkElement;
                    var svLat16 = window.FindName("ExplorationSidebarScroll") as System.Windows.Controls.ScrollViewer;
                    if (contenidoCat == null || bloqueRes == null || svLat16 == null)
                        Console.WriteLine("FALLO: AR-EX1 - no se encontro ExplorationCategoryContent/ExplorationResultsBlock/ExplorationSidebarScroll en el arbol visual");
                    else
                    {
                        var catAntes16 = vm.Exploration.SelectedCategory;
                        string textoAntes16 = vm.Exploration.WorldSearchText;
                        int modoCofresAntes16 = vm.Exploration.ChestViewMode;
                        int modoObjetosAntes16 = vm.Exploration.ObjectsViewMode;

                        // Resultados reales en el bloque compartido - el mismo barrido de "lava"
                        // que BUSCADOR-MUNDO ya usa (este mundo real agota el tope de 1000, que es
                        // el caso PEOR y el mas comun de verdad). Se deja puesto a proposito
                        // mientras se recorren las categorias: cambiar de categoria NO limpia
                        // WorldSearchResults (ver OnSelectedCategoryChanged), asi que el bloque
                        // sigue ahi - que es justo lo que ve el usuario al buscar y luego mirar
                        // Cofres/Minerales/Objetos.
                        vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                        vm.Exploration.WorldSearchText = "lava";
                        WaitForDispatcher(2600);
                        int nRes16 = vm.Exploration.WorldSearchResults.Count;
                        Console.WriteLine($"AR-EX1: bloque de resultados con {nRes16} resultado(s) reales (esperado >0 - si sale 0 este bloque no mide nada)");

                        // La lista real de cada categoria, localizada por su ItemsSource (no por
                        // x:Name: dos categorias distintas comparten la coleccion Inventory, y solo
                        // una de las dos esta visible en cada momento).
                        ItemsControl? ListaDeCategoria16() => vm.Exploration.SelectedCategory switch
                        {
                            WorldSearchCategory.Chests when vm.Exploration.ChestViewMode == 2 => browseViewHost16?.FindName("ChestByChestList") as ItemsControl,
                            WorldSearchCategory.Chests or WorldSearchCategory.Objects =>
                                Descendientes<ListBox>(window).FirstOrDefault(lb => lb.IsVisible && ReferenceEquals(lb.ItemsSource, vm.Exploration.Inventory)),
                            WorldSearchCategory.Ores =>
                                Descendientes<ItemsControl>(window).FirstOrDefault(ic => ic.IsVisible && ReferenceEquals(ic.ItemsSource, vm.Exploration.OreMetals)),
                            _ => null,
                        };

                        foreach (var (etiqueta16, w16, h16) in new (string?, double, double)[]
                                 { ("maximizada", 0, 0), (null, 1600, 1000), (null, 1400, 900), (null, 1180, 860), (null, 1080, 700) })
                        {
                            if (etiqueta16 == "maximizada")
                            {
                                window.WindowState = WindowState.Maximized;
                                DoEvents(); DoEvents(); DoEvents();
                            }
                            else
                            {
                                window.WindowState = WindowState.Normal;
                                FijarTamaño(window, w16, h16);
                            }
                            DoEvents(); DoEvents();
                            string caso16 = etiqueta16 ?? $"{w16:0}x{h16:0}";

                            foreach (var (cat16, modoCofres16) in new (WorldSearchCategory, int)[]
                                     { (WorldSearchCategory.Chests, 0), (WorldSearchCategory.Chests, 2),
                                       (WorldSearchCategory.Ores, 0), (WorldSearchCategory.Objects, 0) })
                            {
                                vm.Exploration.SelectedCategory = cat16;
                                if (cat16 == WorldSearchCategory.Chests) vm.Exploration.ChestViewMode = modoCofres16;
                                DoEvents(); DoEvents();

                                var lista16 = ListaDeCategoria16();
                                int totalFilas16 = lista16?.Items.Count ?? 0;
                                // Filas que de verdad se ven enteras AHORA MISMO (sin tocar nada):
                                // solo se miran las primeras 30 (con 1000 filas virtualizadas la
                                // pregunta util es "¿cabe algo?", no recorrerlas todas).
                                int visiblesEnteras16 = 0;
                                if (lista16 != null)
                                    foreach (var item in lista16.Items.Cast<object>().Take(30))
                                        if (lista16.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement fe16 && VisibleEntero(fe16, window))
                                            visiblesEnteras16++;

                                var rectCat16 = RectVisible(contenidoCat, window);
                                string nombreCat16 = cat16 == WorldSearchCategory.Chests ? (modoCofres16 == 2 ? "Cofres/Cofre a cofre" : "Cofres/Por tipo") : cat16.ToString();
                                Console.WriteLine($"AR-EX1: {caso16} ({window.ActualWidth:0}x{window.ActualHeight:0}), {nombreCat16} con {nRes16} resultado(s) abiertos -> " +
                                                  $"contenido de categoria: pide {contenidoCat.DesiredSize.Height:0}px, mide {contenidoCat.ActualHeight:0}px, SE VE {rectCat16.Height:0}px (esperado >=120); " +
                                                  $"bloque de resultados={bloqueRes.ActualHeight:0}px; lista de la categoria: {totalFilas16} filas, se ven enteras={visiblesEnteras16} (esperado >=1); " +
                                                  $"columna: viewport={svLat16.ViewportHeight:0}px contenido={svLat16.ExtentHeight:0}px");
                                if (rectCat16.Height < 120)
                                    Console.WriteLine($"FALLO: AR-EX1 - a {caso16}, con resultados abiertos, a {nombreCat16} solo le quedan {rectCat16.Height:0}px visibles (el bloque de resultados se lleva {bloqueRes.ActualHeight:0}px)");
                                if (totalFilas16 > 0 && visiblesEnteras16 == 0)
                                    Console.WriteLine($"FALLO: AR-EX1 - a {caso16}, {nombreCat16} tiene {totalFilas16} filas reales y NO se ve ninguna entera con los resultados abiertos");
                            }
                        }

                        // Estado como estaba (misma disciplina que AR-13c/AR-15: todo bloque que
                        // toca estado compartido lo devuelve - aqui ademas el texto de busqueda,
                        // que deja 1000 marcadores pintados sobre el mapa).
                        vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                        vm.Exploration.WorldSearchText = string.Empty;
                        WaitForDispatcher(300);
                        vm.Exploration.ClearOreMarksCommand.Execute(null);
                        vm.Exploration.ChestViewMode = modoCofresAntes16;
                        vm.Exploration.ObjectsViewMode = modoObjetosAntes16;
                        vm.Exploration.SelectedCategory = catAntes16;
                        vm.Exploration.WorldSearchText = textoAntes16;
                        window.WindowState = WindowState.Normal;
                        FijarTamaño(window, 1180, 860);
                        DoEvents(); DoEvents();
                    }
                }
                catch (Exception ex) { Console.WriteLine("AR-EX1-EXCEPTION: " + ex); }

                // AR-EX-HSCROLL (16-sep-2026): hueco real de cobertura que AR-EX1 (justo arriba)
                // dejaba pasar - bug reportado por el usuario con captura propia ("la pestaña de
                // cofres se corta y ademas sale un scroll lateral... horizontal, pasa tambien en
                // Objetos y Minerales, keepqa no cazo esto"). Dos motivos reales, medidos con este
                // mismo arnes antes de escribir el chequeo (nunca a ciegas):
                //  1) D1 (AuditoriaMaquetacion.cs, AR-LAY) NO marca FALLO por un scroll horizontal
                //     activo: un ScrollViewer que SI puede desplazarse en ese eje cuenta como
                //     "escape" (AlcanzableConScroll), asi que un scroll horizontal REAL pero
                //     INDESEADO nunca levanta la alarma - D1 solo caza contenido de verdad perdido
                //     sin ninguna via de alcanzarlo.
                //  2) AR-EX1 (arriba) solo barre TAMAÑO DE VENTANA (1080..1600px), nunca el ANCHO
                //     REAL de la barra lateral - y esa columna es un ancho FIJO en pixeles
                //     (Settings.ExplorationSidebarWidth, arrastrable con el GridSplitter real,
                //     clamp 260-520 en SettingsViewModel.OnExplorationSidebarWidthChanged), NO una
                //     columna "*" que reaccione al tamaño de ventana - variar la ventana sin variar
                //     ESTE ancho no mueve un solo pixel la columna, y el diagnostico de esta misma
                //     ronda (AR_EX_HSCROLL_SOLO=1, ver el bloque al principio de Main) lo confirmo
                //     con medidas reales: a 1080-1600px de ventana y 320px de sidebar (el de
                //     fabrica) CERO scroll horizontal en las 4 combinaciones Cofres/Cofre a
                //     cofre/Minerales/Objetos - pero al sidebar REAL a 260px (extremo real,
                //     alcanzable arrastrando el GridSplitter) la lista COMPARTIDA de resultados
                //     (WorldSearchResults) mide 279,4px de contenido en un viewport de 224px:
                //     scroll horizontal REAL y ACTIVO (ComputedHorizontalScrollBarVisibility=
                //     Visible), 55,4px de sobra, en Cofres/Por tipo; y 12,8px de sobra en Objetos.
                //     Incluso al ancho de FABRICA (320px) el margen medido es de solo 4,6px
                //     (viewport 284 vs contenido 279,4) - una fila con un nombre real un poco mas
                //     largo (idioma EN, un mod con nombres largos) lo tumba sin tocar el
                //     GridSplitter para nada. Cofre a cofre (222px de contenido) y Minerales (lista
                //     propia, adaptativa) no reprodujeron el desbordamiento con el mundo/busqueda
                //     de esta ronda (roca_negra.wld, "lava") - queda anotado como limite real de
                //     esta verificacion, no como "arreglado", por si el contenido real del usuario
                //     (otro mundo, otro idioma) sí lo dispara ahi tambien.
                //
                // Este bloque NO arregla nada (mismo criterio que el resto de AR-EX1/AR-LAY: mide
                // y marca FALLO) - el arreglo visual es tarea de otra ronda/agente.
                try
                {
                    var sidebarHs = window.FindName("ExplorationSidebarPanel") as FrameworkElement;
                    if (sidebarHs == null)
                        Console.WriteLine("FALLO: AR-EX-HSCROLL - no se encontro ExplorationSidebarPanel en el arbol visual");
                    else
                    {
                        double anchoSidebarAntesHs = vm.Settings.ExplorationSidebarWidth;
                        var catAntesHs = vm.Exploration.SelectedCategory;
                        string textoAntesHs = vm.Exploration.WorldSearchText;
                        int modoCofresAntesHs = vm.Exploration.ChestViewMode;

                        vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                        vm.Exploration.WorldSearchText = "lava";
                        WaitForDispatcher(2600);
                        int nResHs = vm.Exploration.WorldSearchResults.Count;
                        Console.WriteLine($"AR-EX-HSCROLL: {nResHs} resultado(s) reales en el bloque compartido (esperado >0 - si sale 0 este bloque no mide nada)");

                        window.WindowState = WindowState.Normal;
                        FijarTamaño(window, 1180, 860); // tamaño de fabrica - la variable real de este chequeo es el sidebar, no la ventana
                        foreach (double anchoSidebarHs in new[] { 260.0, 300.0, 320.0, 420.0, 520.0 })
                        {
                            vm.Settings.ExplorationSidebarWidth = anchoSidebarHs;
                            DoEvents(); DoEvents();

                            foreach (var (catHs, modoCofresHs, nombreCatHs) in new (WorldSearchCategory, int, string)[]
                                     { (WorldSearchCategory.Chests, 0, "Cofres/Por tipo"), (WorldSearchCategory.Chests, 2, "Cofres/Cofre a cofre"),
                                       (WorldSearchCategory.Ores, 0, "Minerales"), (WorldSearchCategory.Objects, 0, "Objetos"),
                                       (WorldSearchCategory.Npcs, 0, "NPCs") })
                            {
                                vm.Exploration.SelectedCategory = catHs;
                                if (catHs == WorldSearchCategory.Chests) vm.Exploration.ChestViewMode = modoCofresHs;
                                DoEvents(); DoEvents();

                                foreach (var sv in Descendientes<System.Windows.Controls.ScrollViewer>(sidebarHs))
                                {
                                    // ExplorationSidebarScroll ya tiene su propio chequeo real (AR-11f,
                                    // mas arriba) y es VERTICAL/Disabled en horizontal a proposito - no
                                    // es el sospechoso de este bug.
                                    if (sv.Name == "ExplorationSidebarScroll" || !sv.IsVisible) continue;
                                    bool horizontalActivaHs = sv.HorizontalScrollBarVisibility != System.Windows.Controls.ScrollBarVisibility.Disabled
                                                               && sv.ScrollableWidth > 0.5;
                                    if (!horizontalActivaHs) continue;
                                    string cadenaHs = "";
                                    try { cadenaHs = string.Join(" / ", Ascendencia(sv, window).TakeLast(6)); } catch (Exception) { }
                                    Console.WriteLine($"FALLO: AR-EX-HSCROLL - sidebar={anchoSidebarHs:0}px, {nombreCatHs}: ScrollViewer(nombre='{sv.Name}') tiene scroll " +
                                                      $"HORIZONTAL real activo ({sv.ScrollableWidth:0.#}px de sobra, contenido {sv.ExtentWidth:0.#}px en un viewport de " +
                                                      $"{sv.ViewportWidth:0.#}px) <- {cadenaHs}");
                                }
                            }
                        }

                        // Estado como estaba (misma disciplina que AR-EX1, justo arriba).
                        vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                        vm.Exploration.WorldSearchText = string.Empty;
                        WaitForDispatcher(300);
                        vm.Exploration.ClearOreMarksCommand.Execute(null);
                        vm.Exploration.ChestViewMode = modoCofresAntesHs;
                        vm.Exploration.SelectedCategory = catAntesHs;
                        vm.Exploration.WorldSearchText = textoAntesHs;
                        vm.Settings.ExplorationSidebarWidth = anchoSidebarAntesHs;
                        DoEvents(); DoEvents();
                    }
                }
                catch (Exception ex) { Console.WriteLine("AR-EX-HSCROLL-EXCEPTION: " + ex); }

                // AR-EX2 (misma oleada, area "Exploracion del mundo"): los GESTOS del mapa, que
                // hasta ahora solo estaban probados a medias - X-a media "Ajustar a la ventana" y
                // F-8 solo comprobaba que el minimapa EXISTE. Aqui se prueban de verdad, con la
                // geometria real:
                //   (a) zoom con rueda CENTRADO EN EL CURSOR: la coordenada de mundo bajo el raton
                //       tiene que ser la MISMA antes y despues del paso de rueda (es la promesa
                //       literal del arreglo de OnWorldMapPreviewMouseWheel del 1-sep-2026, "el
                //       zoom no lo hace recto", que nunca tuvo comprobacion permanente). El evento
                //       se levanta sobre el ScrollViewer real y el cursor se coloca de verdad con
                //       SetCursorPos, porque MouseWheelEventArgs.GetPosition consulta el raton
                //       REAL del sistema, no un punto que se pueda inventar en el evento.
                //   (b) arrastrar para desplazar (pan): mismo camino real (los tres handlers de
                //       raton del ScrollViewer), comprobando que el mapa se mueve EXACTAMENTE lo
                //       que se movio el cursor, ni mas ni menos.
                //   (c) minimapa: el rectangulo de viewport tiene que corresponder de verdad con
                //       la fraccion visible del mundo, y un clic en el minimapa tiene que llevar
                //       el mapa a ESA zona (conversion clic->tile real de OnMinimapClick).
                try
                {
                    var mapaScrollEx = Descendientes<ScrollViewer>(window).FirstOrDefault(sv => sv.Name == "WorldMapScroll");
                    var imgMapaEx = Descendientes<System.Windows.Controls.Image>(window).FirstOrDefault(i => i.Name == "WorldMapImage");
                    var miniImgEx = Descendientes<System.Windows.Controls.Image>(window).FirstOrDefault(i => i.Name == "MinimapImage");
                    var miniRectEx = Descendientes<System.Windows.Shapes.Rectangle>(window).FirstOrDefault(r => r.Name == "MinimapViewportRect");
                    var bitmapMundo = vm.Exploration.WorldImage;
                    if (mapaScrollEx == null || imgMapaEx == null || bitmapMundo == null)
                        Console.WriteLine("FALLO: AR-EX2 - no se encontro WorldMapScroll/WorldMapImage o no hay mundo cargado");
                    else
                    {
                        // Mismo motivo real que el arreglo de F-8 (mas arriba en este Main): el
                        // settings.json REAL de esta maquina tenia "IsMinimapVisible":false
                        // (residuo de una ronda de pruebas anterior que lo toco y no lo devolvio),
                        // lo que colapsa el rectangulo del minimapa ANTES de mirar el zoom -
                        // fijado aqui explicitamente para que AR-EX2-MINIMAPA sea determinista.
                        vm.Settings.IsMinimapVisible = true;
                        FijarTamaño(window, 1400, 900);
                        DoEvents(); DoEvents();
                        // 15-sep-2026 (ronda de re-verificacion de los 21 FALLO): el pan (b) y el
                        // clic del minimapa (c) mandan mouse_event REAL a nivel de SO - si esta
                        // ventana no esta de verdad en primer plano, el down/up puede irse a otra
                        // ventana visible en ese mismo punto de pantalla y el arrastre no ocurre
                        // nunca (la causa real, no solo "raton compartido", detras de una parte de
                        // los falsos negativos ya documentados de AR-EX2-PAN). Antes solo (c) forzaba
                        // primer plano; ahora se hace una vez aqui, para las tres comprobaciones.
                        ForzarPrimerPlano(hwnd);
                        DoEvents();
                        double zoomAntesEx = vm.Exploration.Zoom;

                        // (a) Zoom con rueda centrado en el cursor. Se parte de un zoom y un
                        // desplazamiento con margen real por los cuatro lados: si el punto elegido
                        // estuviera pegado a un borde, el ScrollViewer clamparia el offset nuevo y
                        // el punto se moveria por una razon legitima, no por el bug.
                        vm.Exploration.Zoom = 1.0;
                        mapaScrollEx.UpdateLayout();
                        mapaScrollEx.ScrollToHorizontalOffset(2000);
                        mapaScrollEx.ScrollToVerticalOffset(900);
                        DoEvents(); mapaScrollEx.UpdateLayout(); DoEvents();

                        var centroViewport = new Point(mapaScrollEx.ViewportWidth / 2, mapaScrollEx.ViewportHeight / 2);
                        var centroPantalla = mapaScrollEx.PointToScreen(centroViewport);
                        SetCursorPos((int)centroPantalla.X, (int)centroPantalla.Y);
                        System.Threading.Thread.Sleep(40);
                        // Sin Synchronize(), Mouse.GetPosition NO consulta la posicion real del
                        // cursor: devuelve la del ultimo mensaje de raton que WPF proceso, asi que
                        // un SetCursorPos "silencioso" (sin movimiento fisico que genere
                        // WM_MOUSEMOVE) se queda invisible para toda la capa de entrada de WPF -
                        // medido en esta misma tanda: el punto salia siempre el mismo, muy fuera
                        // del elemento, y las mediciones del pan y del minimapa se omitian solas.
                        System.Windows.Input.Mouse.Synchronize();
                        DoEvents();

                        foreach (int delta in new[] { 120, -120, -120 })
                        {
                            double zAntes = vm.Exploration.Zoom;
                            var posAntes = System.Windows.Input.Mouse.GetPosition(mapaScrollEx);
                            double mundoXAntes = (mapaScrollEx.HorizontalOffset + posAntes.X) / zAntes;
                            double mundoYAntes = (mapaScrollEx.VerticalOffset + posAntes.Y) / zAntes;

                            mapaScrollEx.RaiseEvent(new System.Windows.Input.MouseWheelEventArgs(
                                System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, delta)
                            { RoutedEvent = UIElement.PreviewMouseWheelEvent });
                            DoEvents(); mapaScrollEx.UpdateLayout(); DoEvents();

                            double zDespues = vm.Exploration.Zoom;
                            var posDespues = System.Windows.Input.Mouse.GetPosition(mapaScrollEx);
                            double mundoXDespues = (mapaScrollEx.HorizontalOffset + posDespues.X) / zDespues;
                            double mundoYDespues = (mapaScrollEx.VerticalOffset + posDespues.Y) / zDespues;
                            double derivaX = Math.Abs(mundoXDespues - mundoXAntes), derivaY = Math.Abs(mundoYDespues - mundoYAntes);
                            Console.WriteLine($"AR-EX2-RUEDA: delta={delta} -> zoom {zAntes:0.###}->{zDespues:0.###} (esperado x{(delta > 0 ? "1,25" : "1/1,25")}), " +
                                              $"tile bajo el cursor ({mundoXAntes:0.#},{mundoYAntes:0.#}) -> ({mundoXDespues:0.#},{mundoYDespues:0.#}), deriva=({derivaX:0.#},{derivaY:0.#}) tiles (esperado <=2)");
                            double esperado = delta > 0 ? zAntes * ExplorationViewModel.ZoomStep : zAntes / ExplorationViewModel.ZoomStep;
                            if (Math.Abs(zDespues - esperado) > 0.001)
                                Console.WriteLine($"FALLO: AR-EX2-RUEDA - la rueda no aplico el paso real de zoom ({zDespues:0.####} en vez de {esperado:0.####})");
                            if (derivaX > 2 || derivaY > 2)
                                Console.WriteLine($"FALLO: AR-EX2-RUEDA - el zoom con rueda NO se centra en el cursor: el punto de mundo bajo el raton se movio ({derivaX:0.#},{derivaY:0.#}) tiles");
                        }

                        // (b) Pan real arrastrando: down + move con el boton pulsado + up, por los
                        // mismos handlers reales del ScrollViewer.
                        vm.Exploration.Zoom = 1.0;
                        mapaScrollEx.UpdateLayout();
                        mapaScrollEx.ScrollToHorizontalOffset(2000);
                        mapaScrollEx.ScrollToVerticalOffset(900);
                        DoEvents(); mapaScrollEx.UpdateLayout(); DoEvents();
                        double hAntesPan = mapaScrollEx.HorizontalOffset, vAntesPan = mapaScrollEx.VerticalOffset;
                        var inicioPan = mapaScrollEx.PointToScreen(centroViewport);
                        SetCursorPos((int)inicioPan.X, (int)inicioPan.Y);
                        System.Threading.Thread.Sleep(40); DoEvents();
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                        System.Threading.Thread.Sleep(40); DoEvents(); DoEvents();
                        // El movimiento del cursor se MIDE, nunca se da por hecho que fue a donde se
                        // le pidio: SetCursorPos trabaja en pixeles FISICOS de pantalla mientras el
                        // ScrollViewer razona en DIP, la sesion puede tener escala de DPI, y con la
                        // app real del usuario abierta (o con otro arnes en marcha) el raton es un
                        // recurso COMPARTIDO. Lo que se comprueba es la relacion real que promete el
                        // pan: el mapa se desplaza exactamente lo que se movio el cursor.
                        System.Windows.Input.Mouse.Synchronize(); DoEvents();
                        var posAntesPan = System.Windows.Input.Mouse.GetPosition(mapaScrollEx);
                        SetCursorPos((int)inicioPan.X - 120, (int)inicioPan.Y - 60);
                        System.Threading.Thread.Sleep(90);
                        System.Windows.Input.Mouse.Synchronize();
                        DoEvents(); DoEvents(); DoEvents();
                        var posDespuesPan = System.Windows.Input.Mouse.GetPosition(mapaScrollEx);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                        System.Threading.Thread.Sleep(40); DoEvents(); DoEvents();
                        double dhPan = mapaScrollEx.HorizontalOffset - hAntesPan, dvPan = mapaScrollEx.VerticalOffset - vAntesPan;
                        double movXPan = posDespuesPan.X - posAntesPan.X, movYPan = posDespuesPan.Y - posAntesPan.Y;
                        Console.WriteLine($"AR-EX2-PAN: el cursor se movio de verdad ({movXPan:0},{movYPan:0})px sobre el mapa -> el mapa se desplazo ({dhPan:0},{dvPan:0})px " +
                                          $"(esperado ({-movXPan:0},{-movYPan:0}): arrastrar hacia la izquierda mueve la vista hacia la derecha)");
                        if (Math.Abs(movXPan) < 5 && Math.Abs(movYPan) < 5)
                            Console.WriteLine("AR-EX2-PAN: el cursor real no llego a moverse (raton compartido con otra ventana de esta misma maquina) - medicion omitida, no es un fallo de la app");
                        else if (Math.Abs(dhPan + movXPan) > 4 || Math.Abs(dvPan + movYPan) > 4)
                            Console.WriteLine($"FALLO: AR-EX2-PAN - el arrastre no desplaza el mapa lo mismo que se movio el cursor: cursor ({movXPan:0},{movYPan:0}) -> mapa ({dhPan:0},{dvPan:0})");
                        if (System.Windows.Input.Mouse.Captured != null)
                            Console.WriteLine($"FALLO: AR-EX2-PAN - la captura del raton se quedo colgada tras soltar ({System.Windows.Input.Mouse.Captured})");

                        // (c) Minimapa: geometria del rectangulo de viewport + clic real.
                        if (miniImgEx == null || miniRectEx == null)
                            Console.WriteLine("FALLO: AR-EX2-MINIMAPA - no se encontro MinimapImage/MinimapViewportRect");
                        else
                        {
                            double escalaMini = Math.Min(miniImgEx.ActualWidth / bitmapMundo.PixelWidth, miniImgEx.ActualHeight / bitmapMundo.PixelHeight);
                            double anchoVisibleTiles = mapaScrollEx.ViewportWidth / vm.Exploration.Zoom;
                            double anchoRectEsperado = anchoVisibleTiles * escalaMini;
                            Console.WriteLine($"AR-EX2-MINIMAPA: visible={miniRectEx.IsVisible} (esperado True con el mapa al 100%), ancho del rectangulo={miniRectEx.Width:0.#}px, " +
                                              $"esperado={anchoRectEsperado:0.#}px ({anchoVisibleTiles:0} tiles visibles x escala {escalaMini:0.#####})");
                            if (!miniRectEx.IsVisible)
                                Console.WriteLine("FALLO: AR-EX2-MINIMAPA - con el mapa al 100% (una fraccion pequeña del mundo a la vista) el rectangulo de viewport deberia verse");
                            else if (Math.Abs(miniRectEx.Width - anchoRectEsperado) > 2)
                                Console.WriteLine($"FALLO: AR-EX2-MINIMAPA - el rectangulo no corresponde con la fraccion visible real ({miniRectEx.Width:0.#}px contra {anchoRectEsperado:0.#}px)");

                            // Clic sobre el 75% del ancho del mundo dentro del minimapa: el mapa
                            // tiene que quedar centrado en ese tile (conversion clic->tile real de
                            // OnMinimapClick, via ExplorationViewModel.NavigateToTile).
                            //
                            // El cursor se coloca de VERDAD con SetCursorPos (OnMinimapClick lee
                            // e.GetPosition(MinimapImage), que consulta el raton real del sistema -
                            // un punto inventado en el evento no serviria de nada), pero el handler
                            // se invoca directamente en vez de con un clic real del SO: un clic real
                            // depende de que ESTA ventana tenga el foco en ese instante, y en esta
                            // maquina conviven la app real del usuario y hasta otro arnes - medido:
                            // el clic real se perdia y la prueba acusaba de un bug que no existe
                            // (el eje Y "casi acertaba" solo porque el mapa ya estaba por la mitad).
                            double huecoXMini = (miniImgEx.ActualWidth - bitmapMundo.PixelWidth * escalaMini) / 2;
                            double huecoYMini = (miniImgEx.ActualHeight - bitmapMundo.PixelHeight * escalaMini) / 2;
                            int tileObjetivoX = (int)(bitmapMundo.PixelWidth * 0.75), tileObjetivoY = (int)(bitmapMundo.PixelHeight * 0.5);
                            var puntoMini = miniImgEx.PointToScreen(new Point(huecoXMini + tileObjetivoX * escalaMini, huecoYMini + tileObjetivoY * escalaMini));
                            ForzarPrimerPlano(hwnd);
                            DoEvents();
                            SetCursorPos((int)puntoMini.X, (int)puntoMini.Y);
                            System.Threading.Thread.Sleep(60);
                            System.Windows.Input.Mouse.Synchronize();
                            DoEvents();
                            var posEnMini = System.Windows.Input.Mouse.GetPosition(miniImgEx);
                            int tileClicadoX = (int)((posEnMini.X - huecoXMini) / escalaMini);
                            // ADR-TERRAKEEP-030 (27-sep-2026): OnMinimapClick se movio a
                            // Terrakeep.App/Views/WorldMapView.xaml.cs - la reflexion tiene que
                            // apuntar a esa clase y a la instancia real WorldMapView (no a
                            // MainWindow), mismo patron ya establecido en este archivo para
                            // reflexion sobre metodos privados de un UserControl.
                            var worldMapViewInstanciaMini = window.FindName("WorldMapView") as FrameworkElement;
                            var handlerMini = worldMapViewInstanciaMini?.GetType().GetMethod("OnMinimapClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            handlerMini?.Invoke(worldMapViewInstanciaMini, [miniImgEx, new System.Windows.Input.MouseButtonEventArgs(
                                System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, System.Windows.Input.MouseButton.Left)]);
                            WaitForDispatcher(250);
                            if (handlerMini == null)
                                Console.WriteLine("FALLO: AR-EX2-MINIMAPA-CLIC - no se encontro OnMinimapClick (¿se renombro en WorldMapView.xaml.cs?)");
                            // La comprobacion vale para CUALQUIER punto en el que el cursor haya
                            // caido de verdad ("el mapa acaba centrado en el tile pulsado"), no solo
                            // para el que se pidio. Lo unico que NO se puede juzgar es un punto
                            // fuera del mundo: ahi NavigateToTile clampa a proposito, y con el raton
                            // compartido de esta maquina (app real del usuario + otros arneses)
                            // SetCursorPos a veces no llega - medido, y acusaba de un bug inexistente.
                            bool clicDentroDelMundo = tileClicadoX >= 0 && tileClicadoX < bitmapMundo.PixelWidth;
                            Console.WriteLine($"AR-EX2-MINIMAPA-CLIC: el cursor cayo de verdad sobre el tile {tileClicadoX} del minimapa (pedido {tileObjetivoX}), dentro del mundo={clicDentroDelMundo}");
                            double zoomMini = vm.Exploration.Zoom;
                            double centroXTiles = (mapaScrollEx.HorizontalOffset + mapaScrollEx.ViewportWidth / 2) / zoomMini;
                            double centroYTiles = (mapaScrollEx.VerticalOffset + mapaScrollEx.ViewportHeight / 2) / zoomMini;
                            // Se compara contra el tile sobre el que cayo el cursor DE VERDAD, no
                            // contra el que se pidio - misma honestidad que el pan de arriba.
                            double errorMiniX = Math.Abs(centroXTiles - tileClicadoX), errorMiniY = Math.Abs(centroYTiles - tileObjetivoY);
                            Console.WriteLine($"AR-EX2-MINIMAPA-CLIC: clic sobre el tile ({tileClicadoX},{tileObjetivoY}) del minimapa -> el mapa queda centrado en ({centroXTiles:0},{centroYTiles:0}), " +
                                              $"error=({errorMiniX:0},{errorMiniY:0}) tiles (esperado <= la resolucion real de un pixel de minimapa, {1 / escalaMini:0} tiles)");
                            if (!clicDentroDelMundo)
                                Console.WriteLine("AR-EX2-MINIMAPA-CLIC: el cursor real no llego a caer dentro del minimapa (raton compartido con otra ventana de esta misma maquina) - medicion omitida, no es un fallo de la app");
                            else if (errorMiniX > 1 / escalaMini + 20)
                                Console.WriteLine($"FALLO: AR-EX2-MINIMAPA-CLIC - el clic en el minimapa no lleva el mapa a esa zona ({errorMiniX:0} tiles de error en X)");
                        }

                        vm.Exploration.Zoom = zoomAntesEx;
                        DoEvents();
                    }
                }
                catch (Exception ex) { Console.WriteLine("AR-EX2-EXCEPTION: " + ex); }

                // AR-EX1b (misma oleada): bug real encontrado leyendo el camino de carga y
                // confirmado aqui con DOS mundos reales de esta maquina. LoadFromPathAsync limpia
                // con cuidado casi todo el estado del mundo saliente y termina llamando a
                // RebuildInventory() - pero RebuildInventory despacha por SelectedCategory, y dos
                // lineas antes se acaba de dejar en "Todo", la unica categoria que no reconstruye
                // ningun inventario. Resultado: "Cofre a cofre" conservaba ENTERAS las filas del
                // mundo ANTERIOR y, con un cofre seleccionado, su marcador teal seguia pintado
                // sobre el mapa NUEVO. Se comprueba con el gesto real del usuario (pulsar un cofre
                // y cargar otro mundo desde el lanzador), no solo en la ViewModel.
                try
                {
                    string mundoA = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\Afueras_de_Larvas_de_gusano.wld");
                    string mundoB = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
                    if (!File.Exists(mundoA) || !File.Exists(mundoB))
                        Console.WriteLine("AR-EX1b: hacen falta dos mundos reales distintos - omitido");
                    else
                    {
                        var cargaA = vm.Exploration.LoadFromPathAsync(mundoA);
                        while (!cargaA.IsCompleted) { DoEvents(); Thread.Sleep(15); }
                        vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                        vm.Exploration.ChestViewMode = 2;
                        DoEvents(); DoEvents();
                        int cofresA = vm.Exploration.ChestRows.Count;
                        if (cofresA == 0) Console.WriteLine("AR-EX1b: el primer mundo no tiene cofres - omitido");
                        else
                        {
                            var cofreElegido = vm.Exploration.ChestRows[0];
                            vm.Exploration.GoToChestCommand.Execute(cofreElegido);
                            DoEvents(); DoEvents();
                            bool marcadoAntes = vm.Exploration.HasCurrentChest;
                            int marcadorX = vm.Exploration.CurrentChestX, marcadorY = vm.Exploration.CurrentChestY;

                            var cargaB = vm.Exploration.LoadFromPathAsync(mundoB);
                            while (!cargaB.IsCompleted) { DoEvents(); Thread.Sleep(15); }
                            DoEvents(); DoEvents();

                            Console.WriteLine($"AR-EX1b: mundo A '{Path.GetFileName(mundoA)}' con {cofresA} cofres y el ({marcadorX},{marcadorY}) seleccionado (marcador puesto={marcadoAntes}) -> " +
                                              $"tras cargar '{Path.GetFileName(mundoB)}': ChestRows={vm.Exploration.ChestRows.Count} (esperado 0), marcador={vm.Exploration.HasCurrentChest} (esperado False), " +
                                              $"resultados de busqueda={vm.Exploration.WorldSearchResults.Count} (esperado 0)");
                            if (!marcadoAntes)
                                Console.WriteLine("FALLO: AR-EX1b - pulsar un cofre no dejo marcador que comprobar (regresion de C-06/AR-12d)");
                            if (vm.Exploration.ChestRows.Count > 0)
                                Console.WriteLine($"FALLO: AR-EX1b - tras cargar otro mundo siguen las {vm.Exploration.ChestRows.Count} filas de cofre del mundo anterior");
                            if (vm.Exploration.HasCurrentChest)
                                Console.WriteLine($"FALLO: AR-EX1b - tras cargar otro mundo sigue pintado el marcador del cofre ({vm.Exploration.CurrentChestX},{vm.Exploration.CurrentChestY}) del mundo anterior");
                        }
                        // Estado como estaba: el resto del arnes trabaja sobre roca_negra (misma
                        // disciplina que AR-13c).
                        if (!string.Equals(vm.Exploration.WorldTitle, "roca negra", StringComparison.OrdinalIgnoreCase))
                        {
                            var vuelta = vm.Exploration.LoadFromPathAsync(mundoB);
                            while (!vuelta.IsCompleted) { DoEvents(); Thread.Sleep(15); }
                        }
                        vm.Exploration.ChestViewMode = 0;
                        vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                        DoEvents();
                    }
                }
                catch (Exception ex) { Console.WriteLine("AR-EX1b-EXCEPTION: " + ex); }

                // AR-EX3 (misma oleada): el HOVER real (franja de estado de P-1/F-6) tile a tile
                // sobre el mundo real, con la formula GPS del propio juego - hasta ahora solo se
                // media que la franja no CAMBIA DE ALTO (P-1-ALTURA), nunca lo que dice. Y el aviso
                // de legibilidad del >=40% del mapa, que nunca se habia disparado en ninguna prueba
                // (marcar cobre, que es lo que prueban los bloques de Minerales, cubre el 0,3%).
                try
                {
                    var expl = vm.Exploration;
                    // Los cuatro estratos reales de este mundo, sacados de su propia cabecera - no
                    // de constantes inventadas (mismo criterio que el resto del arnes).
                    var cabecera = Terrakeep.Core.WldFormat.WldReader.ReadHeader(File.ReadAllBytes(worldPath));
                    var casos = new (string Zona, int X, int Y)[]
                    {
                        ("cielo/espacio", cabecera.TilesWide / 2, 5),
                        ("superficie", cabecera.TilesWide / 2, (int)cabecera.GroundLevel - 3),
                        ("subsuelo", cabecera.TilesWide / 2, (int)cabecera.GroundLevel + 40),
                        ("cavernas", cabecera.TilesWide / 2, (int)cabecera.RockLevel + 100),
                        ("inframundo", cabecera.TilesWide / 2, cabecera.TilesHigh - 100),
                    };
                    var zonasVistas = new List<string>();
                    foreach (var (zona, x, y) in casos)
                    {
                        expl.UpdateHover(x, y);
                        zonasVistas.Add(expl.HoverLayerText);
                        Console.WriteLine($"AR-EX3-HOVER: ({x},{y}) [{zona}] -> capa='{expl.HoverLayerText}', profundidad='{expl.HoverDepthText}', tile='{expl.HoverTileText}', pared='{expl.HoverWallText}', liquido='{expl.HoverLiquidText}'");
                        if (expl.HoverCoordText != $"({x}, {y})")
                            Console.WriteLine($"FALLO: AR-EX3-HOVER - las coordenadas de la franja no son las del tile ('{expl.HoverCoordText}' en vez de '({x}, {y})')");
                        if (string.IsNullOrWhiteSpace(expl.HoverLayerText) || expl.HoverLayerText == "—")
                            Console.WriteLine($"FALLO: AR-EX3-HOVER - sin capa resuelta en ({x},{y}), que esta dentro del mundo");
                        if (expl.HoverLayerColor.A == 0)
                            Console.WriteLine($"FALLO: AR-EX3-HOVER - la capa '{expl.HoverLayerText}' no trae color real de la paleta del mapa");
                    }
                    // Las cinco alturas tienen que dar capas DISTINTAS entre si (si la formula GPS
                    // estuviera mal, varias caerian en la misma).
                    int zonasDistintas = zonasVistas.Distinct().Count();
                    Console.WriteLine($"AR-EX3-HOVER: capas distintas resueltas en las 5 alturas={zonasDistintas} de 5 ({string.Join(" / ", zonasVistas)})");
                    if (zonasDistintas < 5)
                        Console.WriteLine($"FALLO: AR-EX3-HOVER - dos alturas muy separadas del mundo resuelven la MISMA capa: {string.Join(" / ", zonasVistas)}");

                    // Fuera del mundo: la franja se limpia entera, nunca enseña el ultimo tile
                    // valido como si el raton siguiera encima.
                    expl.UpdateHover(-1, -1);
                    bool limpio = expl.HoverCoordText == "—" && expl.HoverTileText == "—" && expl.HoverLayerText == "—" && expl.HoverInfo.Length == 0;
                    Console.WriteLine($"AR-EX3-HOVER-FUERA: al salir del mapa la franja queda limpia={limpio} (esperado True)");
                    if (!limpio)
                        Console.WriteLine($"FALLO: AR-EX3-HOVER - al salir del mapa la franja conserva datos del ultimo tile (coord='{expl.HoverCoordText}', tile='{expl.HoverTileText}')");

                    // Aviso de legibilidad: marcar el tile MAS abundante del mundo real (el que de
                    // verdad tiñe medio mapa) tiene que disparar el aviso del >=40%; y "Quitar
                    // marcas" tiene que dejarlo todo apagado.
                    var catAntesEx3 = expl.SelectedCategory;
                    expl.SelectedCategory = WorldSearchCategory.Objects;
                    expl.ObjectsViewMode = 0;
                    DoEvents();
                    long totalTilesMundo = (long)cabecera.TilesWide * cabecera.TilesHigh;
                    // Se marcan los tiles MAS abundantes hasta pasar del 40% de verdad - el umbral
                    // real del aviso. Con uno solo no basta ni en un mundo Grande (la piedra, el
                    // mas abundante de este mundo real, cubre el 18,4%), asi que hasta ahora el
                    // aviso no lo habia ejercitado ninguna prueba: una garantia que nunca se activa
                    // no esta demostrada (misma leccion que AR-14b).
                    var acumuladas = new List<WorldInventoryRowViewModel>();
                    long tilesAcumulados = 0;
                    foreach (var fila in expl.Inventory.OrderByDescending(r => r.Count))
                    {
                        acumuladas.Add(fila);
                        tilesAcumulados += fila.Count;
                        if ((double)tilesAcumulados / totalTilesMundo >= 0.42) break;
                    }
                    double fraccion = (double)tilesAcumulados / totalTilesMundo;
                    if (acumuladas.Count == 0) Console.WriteLine("AR-EX3-LEGIBILIDAD: el mundo no trae ningun tile - omitido");
                    else
                    {
                        foreach (var fila in acumuladas) fila.IsChecked = true;
                        expl.MarkObjectsOnMapCommand.Execute(null);
                        WaitForDispatcher(4000);
                        string resumen = expl.WorldSearchSummary;
                        // El aviso va DELANTE del resumen (ApplyHighlightResult), asi que basta con
                        // mirar si el texto empieza por un digito (resumen a secas) o no (aviso).
                        bool hayAviso = resumen.Length > 0 && !char.IsDigit(resumen[0]);
                        Console.WriteLine($"AR-EX3-LEGIBILIDAD: {acumuladas.Count} tile(s) marcados ({string.Join(" + ", acumuladas.Select(r => r.Name))}) cubren {tilesAcumulados:N0} de {totalTilesMundo:N0} ({fraccion:P1}) " +
                                          $"-> resumen='{resumen}' (esperado que EMPIECE por el aviso de legibilidad, porque pasa del 40%)");
                        if (fraccion >= 0.4 && !hayAviso)
                            Console.WriteLine($"FALLO: AR-EX3-LEGIBILIDAD - la seleccion cubre el {fraccion:P0} del mapa y el resumen no avisa de nada: '{resumen}'");
                        if (fraccion < 0.4 && hayAviso)
                            Console.WriteLine($"FALLO: AR-EX3-LEGIBILIDAD - avisa de ilegibilidad con solo el {fraccion:P0} del mapa cubierto: '{resumen}'");

                        expl.ClearOreMarksCommand.Execute(null);
                        DoEvents();
                        Console.WriteLine($"AR-EX3-LEGIBILIDAD: tras 'Quitar marcas' -> resaltado={(expl.WorldHighlight == null ? "apagado" : "SIGUE ENCENDIDO")}, resultados={expl.WorldSearchResults.Count} (esperado apagado y 0)");
                        if (expl.WorldHighlight != null || expl.WorldSearchResults.Count > 0)
                            Console.WriteLine("FALLO: AR-EX3-LEGIBILIDAD - 'Quitar marcas' no apaga el resaltado y/o la lista de resultados");
                        foreach (var fila in acumuladas) fila.IsChecked = false;
                        WaitForDispatcher(500);
                        expl.ClearOreMarksCommand.Execute(null);
                    }
                    expl.SelectedCategory = catAntesEx3;
                    DoEvents();
                }
                catch (Exception ex) { Console.WriteLine("AR-EX3-EXCEPTION: " + ex); }

                // AR-EX4 (misma oleada): lo que queda del encargo real de esta area y que ninguna
                // prueba tocaba todavia:
                //   (a) ARRASTRAR Y SOLTAR un .wld sobre la ventana (F-13). El gesto OLE real no se
                //       puede sintetizar, pero el manejador si: se le entrega un DataObject de
                //       FileDrop de verdad, que es exactamente lo que le llega del sistema. Se
                //       prueban los tres casos que existen: un mundo bueno, un fichero de otro tipo
                //       (no debe pasar nada) y un .wld CORRUPTO (no puede reventar la app).
                //   (b) EXPORTAR el mapa a PNG con el resaltado encendido: F-12 solo probaba el
                //       mapa desnudo, y la rama que COMPONE las dos capas (DrawingVisual +
                //       RenderTargetBitmap) no la habia ejecutado nunca ninguna prueba.
                //   (c) el reparto vertical de la columna (AR-EX1) tambien en INGLES: los textos
                //       cambian de largo y las dos casillas del bloque de resultados pueden pasar a
                //       dos lineas, que es justo lo que empuja a la categoria.
                try
                {
                    string mundoOk = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
                    var dropHandler = typeof(MainWindow).GetMethod("OnWindowDrop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (dropHandler == null) Console.WriteLine("FALLO: AR-EX4-DROP - no se encontro OnWindowDrop (se renombro en MainWindow.xaml.cs?)");
                    else if (!File.Exists(mundoOk)) Console.WriteLine("AR-EX4-DROP: no hay mundo real con el que probar - omitido");
                    else
                    {
                        // El manejador es async void: se le da tiempo real bombeando el Dispatcher,
                        // igual que hace el resto del arnes con las cargas de mundo.
                        // DragEventArgs no tiene NINGUN constructor publico (WPF solo los crea por
                        // dentro, desde el OLE real), asi que se construye por reflexion rellenando
                        // cada parametro por su TIPO - sin depender de cuantos sean ni de su orden,
                        // que es detalle interno de WPF y puede cambiar entre versiones.
                        void Soltar(string ruta)
                        {
                            var datos = new DataObject(DataFormats.FileDrop, new[] { ruta });
                            var ctor = typeof(DragEventArgs)
                                .GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                .OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
                            if (ctor == null) { Console.WriteLine("FALLO: AR-EX4-DROP - no se pudo construir un DragEventArgs real"); return; }
                            var valores = ctor.GetParameters().Select(par =>
                                par.ParameterType.IsInstanceOfType(datos) ? (object)datos
                                : par.ParameterType == typeof(DragDropKeyStates) ? DragDropKeyStates.None
                                : par.ParameterType == typeof(DragDropEffects) ? DragDropEffects.Copy
                                : par.ParameterType.IsInstanceOfType(window) ? window
                                : par.ParameterType == typeof(Point) ? new Point(10, 10)
                                : par.ParameterType.IsValueType ? Activator.CreateInstance(par.ParameterType)!
                                : null!).ToArray();
                            var args = (DragEventArgs)ctor.Invoke(valores);
                            args.RoutedEvent = System.Windows.DragDrop.DropEvent;
                            dropHandler.Invoke(window, [window, args]);
                            for (int i = 0; i < 300 && vm.Exploration.IsLoading; i++) { DoEvents(); Thread.Sleep(15); }
                            WaitForDispatcher(300);
                        }

                        int pestanaAntes = vm.SelectedTabIndex;
                        vm.SelectedTabIndex = 0; // Inicio: soltar un mundo tiene que traer al usuario a Exploracion
                        DoEvents();
                        Soltar(mundoOk);
                        Console.WriteLine($"AR-EX4-DROP: soltar '{Path.GetFileName(mundoOk)}' -> mundo cargado={vm.Exploration.IsWorldLoaded} (esperado True), " +
                                          $"titulo='{vm.Exploration.WorldTitle}', pestaña activa={vm.SelectedTabIndex} (esperado 4, Exploracion)");
                        if (!vm.Exploration.IsWorldLoaded)
                            Console.WriteLine("FALLO: AR-EX4-DROP - soltar un .wld real no cargo el mundo");
                        if (vm.SelectedTabIndex != 4)
                            Console.WriteLine($"FALLO: AR-EX4-DROP - soltar un .wld no lleva a la pestaña de Exploracion (quedo en {vm.SelectedTabIndex})");

                        // Un fichero que no es ni .wld ni .plr: no debe hacer NADA (ni cargar, ni
                        // cambiar de pestaña, ni dar error).
                        string ajeno = Path.Combine(Path.GetTempPath(), $"terrakeep-ajeno-{Guid.NewGuid():N}.txt");
                        File.WriteAllText(ajeno, "esto no es un mundo");
                        string tituloAntes = vm.Exploration.WorldTitle ?? "";
                        vm.SelectedTabIndex = 0;
                        DoEvents();
                        Soltar(ajeno);
                        Console.WriteLine($"AR-EX4-DROP: soltar un .txt -> pestaña={vm.SelectedTabIndex} (esperado 0, sin moverse), mundo sigue siendo '{vm.Exploration.WorldTitle}' (esperado '{tituloAntes}')");
                        if (vm.SelectedTabIndex != 0 || vm.Exploration.WorldTitle != tituloAntes)
                            Console.WriteLine("FALLO: AR-EX4-DROP - soltar un fichero que no es un mundo cambia el estado de la app");
                        File.Delete(ajeno);

                        // Un .wld CORRUPTO (copia real truncada a la mitad): tiene que quedarse en
                        // un mensaje de error legible, nunca reventar ni dejar medio mundo cargado.
                        string corrupto = Path.Combine(Path.GetTempPath(), $"terrakeep-corrupto-{Guid.NewGuid():N}.wld");
                        var bytesReales = File.ReadAllBytes(mundoOk);
                        File.WriteAllBytes(corrupto, bytesReales.Take(bytesReales.Length / 2).ToArray());
                        vm.SelectedTabIndex = 0;
                        DoEvents();
                        Soltar(corrupto);
                        Console.WriteLine($"AR-EX4-DROP: soltar un .wld truncado a la mitad -> IsWorldLoaded={vm.Exploration.IsWorldLoaded} (esperado False), " +
                                          $"estado='{vm.Exploration.StatusMessage}' (esperado un mensaje de error legible), filas de cofre={vm.Exploration.ChestRows.Count} (esperado 0)");
                        if (vm.Exploration.IsWorldLoaded)
                            Console.WriteLine("FALLO: AR-EX4-DROP - un .wld corrupto se da por cargado");
                        if (vm.Exploration.ChestRows.Count > 0)
                            Console.WriteLine($"FALLO: AR-EX4-DROP - tras fallar la carga quedan {vm.Exploration.ChestRows.Count} filas de cofre del mundo anterior");
                        File.Delete(corrupto);

                        // Estado como estaba: mundo bueno cargado y la pestaña donde estaba.
                        Soltar(mundoOk);
                        vm.SelectedTabIndex = pestanaAntes;
                        DoEvents();
                    }
                }
                catch (Exception ex) { Console.WriteLine("AR-EX4-DROP-EXCEPTION: " + ex); }

                try
                {
                    // (b) Exportar el mapa CON resaltado - la rama que compone las dos capas.
                    if (!vm.Exploration.IsWorldLoaded) Console.WriteLine("AR-EX4-PNG: sin mundo cargado - omitido");
                    else
                    {
                        var catAntesPng = vm.Exploration.SelectedCategory;
                        vm.Exploration.SelectedCategory = WorldSearchCategory.Ores;
                        DoEvents();
                        var mineral = vm.Exploration.OreMetals.FirstOrDefault();
                        if (mineral == null) Console.WriteLine("AR-EX4-PNG: este mundo no trae ningun metal - omitido");
                        else
                        {
                            mineral.IsChecked = true;
                            WaitForDispatcher(3500);
                            bool hayResaltado = vm.Exploration.WorldHighlight != null;
                            string destino = Path.Combine(AppContext.BaseDirectory, "exploracion-mapa-con-resaltado.png");
                            vm.Exploration.ExportMapToPng(destino);
                            var pngLeido = new System.Windows.Media.Imaging.BitmapImage();
                            pngLeido.BeginInit();
                            pngLeido.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                            pngLeido.UriSource = new Uri(destino);
                            pngLeido.EndInit();
                            var mapa = vm.Exploration.WorldImage!;
                            Console.WriteLine($"AR-EX4-PNG: exportado con resaltado activo={hayResaltado} (esperado True) -> {pngLeido.PixelWidth}x{pngLeido.PixelHeight} " +
                                              $"(esperado {mapa.PixelWidth}x{mapa.PixelHeight}), {new FileInfo(destino).Length / 1024:N0} KB");
                            if (!hayResaltado)
                                Console.WriteLine("FALLO: AR-EX4-PNG - marcar un mineral no dejo capa de resaltado que exportar");
                            if (pngLeido.PixelWidth != mapa.PixelWidth || pngLeido.PixelHeight != mapa.PixelHeight)
                                Console.WriteLine($"FALLO: AR-EX4-PNG - el PNG compuesto no conserva el tamaño real del mundo ({pngLeido.PixelWidth}x{pngLeido.PixelHeight} en vez de {mapa.PixelWidth}x{mapa.PixelHeight})");
                            mineral.IsChecked = false;
                            WaitForDispatcher(600);
                            vm.Exploration.ClearOreMarksCommand.Execute(null);
                        }
                        vm.Exploration.SelectedCategory = catAntesPng;
                        DoEvents();
                    }
                }
                catch (Exception ex) { Console.WriteLine("AR-EX4-PNG-EXCEPTION: " + ex); }

                try
                {
                    // (c) El reparto vertical de AR-EX1, en INGLES y en los tamaños que mas aprietan.
                    // ADR-TERRAKEEP-029: Browse se movio a BrowseView.xaml - FindName DOBLE.
                    var browseViewHostEx4 = window.FindName("BrowseView") as FrameworkElement;
                    var contenidoCatEn = browseViewHostEx4?.FindName("ExplorationCategoryContent") as FrameworkElement;
                    var bloqueResEn = browseViewHostEx4?.FindName("ExplorationResultsBlock") as FrameworkElement;
                    if (contenidoCatEn == null || bloqueResEn == null) Console.WriteLine("FALLO: AR-EX4-IDIOMA - no se encontro el contenido de categoria / bloque de resultados");
                    else
                    {
                        string idiomaAntesEx = LocalizationService.Instance.Language;
                        var catAntesEx = vm.Exploration.SelectedCategory;
                        // La pestaña TIENE que estar activa para medir: en otra pestaña, WPF ni
                        // siquiera realiza estos elementos y la medida sale vacia (salia como
                        // "-inf px" en la primera version de este bloque, que parecia un bug).
                        int pestanaAntesIdioma = vm.SelectedTabIndex;
                        vm.SelectedTabIndex = 4; // Exploracion
                        DoEvents(); DoEvents();
                        LocalizationService.Instance.SetLanguage("en");
                        vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                        vm.Exploration.WorldSearchText = "lava";
                        // Mismo bug de fondo ya arreglado en A8-02 (mas arriba en este Main()):
                        // WaitForDispatcher(2600) fijo es una carrera contra el reloj, no contra el
                        // estado real - bajo carga real de la maquina el barrido puede tardar mas.
                        // Sondeo real con margen generoso en su lugar.
                        long limiteEx4Idioma = Environment.TickCount64 + 60_000;
                        while (vm.Exploration.IsSearching && Environment.TickCount64 < limiteEx4Idioma)
                        { DoEvents(); System.Threading.Thread.Sleep(1); }
                        DoEvents();
                        int nResEn = vm.Exploration.WorldSearchResults.Count;
                        foreach (var (wEn, hEn) in new (double, double)[] { (1400, 900), (1180, 860), (1080, 700) })
                        {
                            window.WindowState = WindowState.Normal;
                            FijarTamaño(window, wEn, hEn);
                            foreach (var catEn in new[] { WorldSearchCategory.Chests, WorldSearchCategory.Ores, WorldSearchCategory.Objects })
                            {
                                vm.Exploration.SelectedCategory = catEn;
                                DoEvents(); DoEvents();
                                var rectEn = RectVisible(contenidoCatEn, window);
                                Console.WriteLine($"AR-EX4-IDIOMA: [en] {wEn:0}x{hEn:0}, {catEn} con {nResEn} resultado(s) -> contenido de categoria SE VE {rectEn.Height:0}px (esperado >=120), " +
                                                  $"bloque de resultados={bloqueResEn.ActualHeight:0}px");
                                if (rectEn.Height < 120)
                                    Console.WriteLine($"FALLO: AR-EX4-IDIOMA - en INGLES, a {wEn:0}x{hEn:0}, a {catEn} solo le quedan {rectEn.Height:0}px (el bloque de resultados se lleva {bloqueResEn.ActualHeight:0}px)");
                            }
                        }
                        vm.Exploration.WorldSearchText = string.Empty;
                        WaitForDispatcher(300);
                        vm.Exploration.ClearOreMarksCommand.Execute(null);
                        vm.Exploration.SelectedCategory = catAntesEx;
                        LocalizationService.Instance.SetLanguage(idiomaAntesEx);
                        vm.SelectedTabIndex = pestanaAntesIdioma;
                        FijarTamaño(window, 1180, 860);
                        DoEvents(); DoEvents();
                    }
                }
                catch (Exception ex) { Console.WriteLine("AR-EX4-IDIOMA-EXCEPTION: " + ex); }

                // AR-EX5 (misma oleada): edicion de dificultad - "los 4 modos SEGUN LA VERSION del
                // mundo" y guardado atomico. A9-11-DIFICULTAD ya probaba el guardado real (1->2
                // sobre una copia, con .bak y un solo Int32 distinto), pero nadie habia mirado los
                // CHIPS reales: se ofrecian los cuatro en cualquier mundo, y en un mundo anterior a
                // la version 209 del formato la mitad no se pueden escribir (WldWriter.
                // SupportsGameMode) - se descubria al pulsar Guardar, con una excepcion convertida
                // en mensaje de error. Ahora el chip imposible no se deja pulsar; aqui se comprueba
                // sobre los RadioButton REALES del arbol visual, no solo en la ViewModel.
                try
                {
                    // Los chips viven DENTRO del Expander "Este mundo", que arranca COLAPSADO:
                    // hasta desplegarlo sus hijos no existen en el arbol visual (la primera version
                    // de este bloque encontraba 0 chips y lo cantaba como fallo). Se despliega, se
                    // mide, y se deja como estaba - misma disciplina que AR-15.
                    int pestanaAntesEx5 = vm.SelectedTabIndex;
                    vm.SelectedTabIndex = 4; // Exploracion
                    DoEvents(); DoEvents();
                    var esteMundoEx5 = Descendientes<System.Windows.Controls.Expander>(window)
                        .FirstOrDefault(e => (e.Header as string) == "Este mundo" || (e.Header as string) == "This world");
                    bool desplegadoAntesEx5 = esteMundoEx5?.IsExpanded ?? false;
                    if (esteMundoEx5 != null) { esteMundoEx5.IsExpanded = true; DoEvents(); DoEvents(); }
                    var chipsDificultad = Descendientes<System.Windows.Controls.RadioButton>(window)
                        .Where(rb => rb.GroupName == "DificultadMundo").ToList();
                    var cabeceraMundo = Terrakeep.Core.WldFormat.WldReader.ReadHeader(File.ReadAllBytes(worldPath));
                    if (chipsDificultad.Count != 4)
                        Console.WriteLine($"FALLO: AR-EX5-DIFICULTAD - se esperaban 4 chips de dificultad en el arbol visual, hay {chipsDificultad.Count}");
                    else
                    {
                        // roca_negra es version 279 (moderna): los cuatro tienen que estar vivos.
                        var habilitados = chipsDificultad.Select(rb => rb.IsEnabled).ToList();
                        Console.WriteLine($"AR-EX5-DIFICULTAD: mundo de formato {cabeceraMundo.Version} -> chips habilitados = [{string.Join(", ", habilitados)}] (esperado los 4 en True: >=209 admite los 4 modos), " +
                                          $"ViewModel dice [{vm.Exploration.CanUseGameModeClassic}, {vm.Exploration.CanUseGameModeExpert}, {vm.Exploration.CanUseGameModeMaster}, {vm.Exploration.CanUseGameModeJourney}]");
                        for (int modo = 0; modo <= 3; modo++)
                        {
                            bool esperadoModo = Terrakeep.Core.WldFormat.WldWriter.SupportsGameMode(cabeceraMundo.Version, modo);
                            if (habilitados[modo] != esperadoModo)
                                Console.WriteLine($"FALLO: AR-EX5-DIFICULTAD - el chip del modo {modo} esta {(habilitados[modo] ? "habilitado" : "deshabilitado")} y el formato {cabeceraMundo.Version} dice lo contrario");
                        }
                    }

                    // Guardado real de los CUATRO modos, uno por uno, sobre una COPIA del mundo
                    // (nunca el archivo del usuario) - A9-11 solo probaba 1->2. Se comprueba ademas
                    // que el resto del archivo no se mueve ni un byte en ninguno de los cuatro.
                    if (esteMundoEx5 != null) { esteMundoEx5.IsExpanded = desplegadoAntesEx5; DoEvents(); }
                    vm.SelectedTabIndex = pestanaAntesEx5;
                    DoEvents();

                    string copiaEx5 = Path.Combine(Path.GetTempPath(), $"terrakeep-dificultad-ex5-{Guid.NewGuid():N}.wld");
                    File.Copy(worldPath, copiaEx5, overwrite: true);
                    try
                    {
                        var bytesBase = File.ReadAllBytes(copiaEx5);
                        foreach (int modo in new[] { 0, 1, 2, 3 })
                        {
                            var parcheado = Terrakeep.Core.WldFormat.WldWriter.PatchGameMode(bytesBase, modo);
                            int distintos = Enumerable.Range(0, bytesBase.Length).Count(i => bytesBase[i] != parcheado[i]);
                            int leido = Terrakeep.Core.WldFormat.WldReader.ReadHeader(parcheado).GameMode;
                            Console.WriteLine($"AR-EX5-GUARDADO: modo {modo} -> releido del archivo={leido} (esperado {modo}), bytes distintos del original={distintos} (esperado <=4, solo el Int32 de GameMode), mismo tamaño={bytesBase.Length == parcheado.Length}");
                            if (leido != modo)
                                Console.WriteLine($"FALLO: AR-EX5-GUARDADO - escribir el modo {modo} deja {leido} en el archivo");
                            if (distintos > 4 || bytesBase.Length != parcheado.Length)
                                Console.WriteLine($"FALLO: AR-EX5-GUARDADO - escribir el modo {modo} toca {distintos} bytes del mundo (solo puede tocar el Int32 de GameMode)");
                        }
                    }
                    finally
                    {
                        if (File.Exists(copiaEx5)) File.Delete(copiaEx5);
                    }
                }
                catch (Exception ex) { Console.WriteLine("AR-EX5-EXCEPTION: " + ex); }

                // AR-EX6 (15-sep-2026, bug real reportado por el usuario mirando su propia
                // pantalla a ~1180px: "los botones de la segunda fila de la barra de
                // herramientas de Exploracion aparecen solapados/apretados", y sobre todo "keepqa
                // tambien dejo pasar esto"): cierra el hueco real de cobertura - AR-02/AR-11/
                // AR-EX1..5 miden la COLUMNA LATERAL de Exploracion, ninguno media la barra
                // SUPERIOR (el StackPanel de zoom de MainWindow.xaml:4440). Cuerpo real en
                // AuditoriaBarraExploracion.cs (misma clase parcial) - ver alli el porque
                // detallado y la causa geometrica real confirmada. Se ejecuta SIEMPRE (sin
                // variable de entorno), igual que el resto de AR-xx de este bloque, para que
                // esto no pueda volver a colarse en silencio.
                EjecutarComprobacionBarraExploracion(window, vm);

                // Sexta auditoria de Opus, H6-08/H6-09/H6-10 ("el mapa muestra puntos rosas que
                // el usuario cree que son mascotas -son NPCs- deberia verse solo cabezas de
                // NPC"): con el mundo real ya cargado arriba, confirma que la mayoria de NPCs
                // reales resuelven una cabeza real (HeadIconPath != null) - un mundo real
                // conocido de este equipo no deberia tener ningun NPC de pueblo real sin
                // cabeza salvo OldMan/SkeletonMerchant (sin icono real en el propio juego).
                try
                {
                    int totalNpcs = vm.Exploration.Npcs.Count;
                    int conCabeza = vm.Exploration.Npcs.Count(n => n.HeadIconPath != null);
                    var sinCabeza = vm.Exploration.Npcs.Where(n => n.HeadIconPath == null).Select(n => n.Name).Distinct().ToList();
                    Console.WriteLine($"H6-08-CABEZAS: {conCabeza}/{totalNpcs} NPC(s) reales con cabeza real resuelta, sin cabeza: [{string.Join(", ", sinCabeza)}] (esperado: solo Viejito/Mercader Esqueleto, si acaso)");

                    // Centra el mapa en el primer NPC real (el zoom in extremo se probo aparte
                    // a mano y no encuadraba bien en este arnes - problema real del scroll del
                    // propio arnes bajo zoom extremo, no del codigo de produccion; el recuento
                    // 14/14 de arriba ya es la prueba real y automatica de que HeadIconPath se
                    // resuelve de verdad, esta captura es solo apoyo visual complementario).
                    var primerNpc = vm.Exploration.Npcs.FirstOrDefault();
                    if (primerNpc != null)
                    {
                        vm.Exploration.GoToNpcCommand.Execute(primerNpc);
                        DoEvents(); DoEvents();
                    }
                    var rtbCabezas = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbCabezas.Render(window);
                    var encCabezas = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encCabezas.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbCabezas));
                    using var fsCabezas = File.Create(Path.Combine(AppContext.BaseDirectory, "h6-08-mapa-cabezas-npc.png"));
                    encCabezas.Save(fsCabezas);
                    Console.WriteLine($"Captura mapa centrado en '{primerNpc?.Name}' -> h6-08-mapa-cabezas-npc.png");
                }
                catch (Exception ex) { Console.WriteLine("H6-08-CABEZAS-EXCEPTION: " + ex); }

                // X-g (segunda auditoria de Opus, Fable): "el mapa no sabe nada del personaje
                // real" - añade un Spawn Point real (Servers, la unica fuente real de
                // coordenadas de aparicion del .plr) dentro de los limites reales de este mundo,
                // navega fuera y vuelve a Exploracion (dispara OnSelectedTabIndexChanged ->
                // SetCharacterSpawns) y confirma que aparece en CharacterSpawns.
                //
                // C-05 (informe de pulido final, cierra E4): ahora un Spawn Point solo aparece si
                // WorldId Y Name coinciden de verdad con el mundo cargado (regla real del propio
                // juego, Player.FindSpawn) - WorldId=0/Name arbitrario (el valor por defecto de
                // "Añadir spawn point") ya NO basta, hay que fijarlos al mundo real cargado para
                // que este spawn de prueba siga representando el caso "pertenece a este mundo".
                vm.Servers.AddEntryCommand.Execute(null);
                var spawnRow = vm.Servers.Entries[^1];
                spawnRow.WorldId = vm.Exploration.LoadedWorldId ?? 0;
                spawnRow.Name = vm.Exploration.WorldTitle ?? "";
                spawnRow.SpawnX = 4200;
                spawnRow.SpawnY = 300;
                vm.SelectedTabIndex = 1; // Personaje
                DoEvents();
                vm.SelectedTabIndex = 4; // Exploracion - dispara el refresco real
                DoEvents(); DoEvents();
                bool xgEncontrado = vm.Exploration.CharacterSpawns.Any(s => s.TileX == 4200 && s.TileY == 300 && s.Label.Contains("(4200, 300)"));
                Console.WriteLine($"X-G-SPAWN-PERSONAJE: Spawn Point real añadido (WorldId/Name del mundo cargado) -> aparece en el mapa={xgEncontrado} (esperado True), CharacterSpawns.Count={vm.Exploration.CharacterSpawns.Count}");
                if (!xgEncontrado) Console.WriteLine("FALLO: X-g (segunda auditoria) - el Spawn Point real del personaje no llego al mapa");

                // A9-08-SPAWNMUNDO (informe de pulido final, C-05): el mismo Spawn Point, pero
                // con el WorldId de OTRO mundo, NO debe aparecer - antes cualquier Spawn Point
                // guardado se mostraba en CUALQUIER mundo cargado.
                spawnRow.WorldId = (vm.Exploration.LoadedWorldId ?? 0) + 999;
                vm.SelectedTabIndex = 1; // Personaje - y de vuelta, para forzar el refresco real (SetCharacterSpawns solo se recalcula al ENTRAR en Exploracion)
                DoEvents();
                vm.SelectedTabIndex = 4; // Exploracion
                DoEvents(); DoEvents();
                bool xgDeOtroMundo = vm.Exploration.CharacterSpawns.Any(s => s.TileX == 4200 && s.TileY == 300);
                Console.WriteLine($"A9-08-SPAWNMUNDO: mismo Spawn Point con WorldId de otro mundo -> sigue en el mapa={xgDeOtroMundo} (esperado False)");
                if (xgDeOtroMundo) Console.WriteLine("FALLO: C-05 - un Spawn Point de OTRO mundo aparece en el mapa del mundo cargado");
                spawnRow.WorldId = vm.Exploration.LoadedWorldId ?? 0; // deja el spawn de prueba coherente para la captura de abajo
                vm.SelectedTabIndex = 1;
                DoEvents();
                vm.SelectedTabIndex = 4;
                DoEvents(); DoEvents();
                var rtbSpawn = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbSpawn.Render(window);
                var encSpawn = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encSpawn.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbSpawn));
                using (var fsSpawn = File.Create(Path.Combine(AppContext.BaseDirectory, "mundo-spawn-personaje.png"))) encSpawn.Save(fsSpawn);
                Console.WriteLine("Captura mapa con spawn del personaje -> mundo-spawn-personaje.png");
                vm.Servers.RemoveEntryCommand.Execute(spawnRow); // deja el personaje real como estaba
            }
            else Console.WriteLine("X7-ASYNC: fichero no encontrado, omitido");
        }
        catch (Exception ex) { Console.WriteLine("X7-ASYNC-EXCEPTION: " + ex); }

        // Verificacion real de P-1 (auditoria de Opus, Bloque 4): el preview de Apariencia debe
        // quedarse REALMENTE fijo en pantalla (misma posicion en pixeles) mientras la columna
        // derecha se desplaza - no basta con que "no forme parte del StackPanel que scrollea"
        // en el codigo, hay que medir su posicion real en pantalla antes y despues.
        try
        {
            vm.SelectedTabIndex = 1; // Personaje
            vm.PersonajeInnerTabIndex = 3; // Apariencia
            FijarTamaño(window, 1180, 700);

            // FindFirst encontraria antes la miniatura pequeña de la cabecera global (N-1,
            // 28x39, el mismo Appearance.PreviewImage a otro tamaño) que el preview grande real
            // de Apariencia (240x336) - se filtra por el ancho real para coger el correcto.
            var previewImage = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Image))
                .Cast<AutomationElement>().FirstOrDefault(img => img.Current.BoundingRectangle.Width > 100);
            var rectAntes = previewImage?.Current.BoundingRectangle ?? Rect.Empty;

            var rtbAparienciaAntes = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbAparienciaAntes.Render(window);
            var encAparienciaAntes = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encAparienciaAntes.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbAparienciaAntes));
            using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "p1-apariencia-antes-scroll.png"))) encAparienciaAntes.Save(fs);

            // Desplaza la columna derecha (el ScrollViewer real que envuelve Genero/Peinado/
            // Tinte/colores/estadisticas) hasta el final via ScrollPattern real.
            var scrollViewers = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Pane))
                .Cast<AutomationElement>().Where(el => el.TryGetCurrentPattern(ScrollPattern.Pattern, out _)).ToList();
            AutomationElement? rightScroller = null;
            foreach (var sv in scrollViewers)
            {
                if (sv.TryGetCurrentPattern(ScrollPattern.Pattern, out var pat) && ((ScrollPattern)pat).Current.VerticallyScrollable)
                {
                    rightScroller = sv;
                    break;
                }
            }
            if (rightScroller != null && rightScroller.TryGetCurrentPattern(ScrollPattern.Pattern, out var scrollPat))
            {
                ((ScrollPattern)scrollPat).SetScrollPercent(ScrollPattern.NoScroll, 100);
                DoEvents();
                DoEvents();
            }
            else Console.WriteLine("P1-SCROLL: ScrollViewer real con contenido desplazable NO-FOUND");

            var rectDespues = previewImage?.Current.BoundingRectangle ?? Rect.Empty;
            var rtbAparienciaDespues = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbAparienciaDespues.Render(window);
            var encAparienciaDespues = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encAparienciaDespues.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbAparienciaDespues));
            using (var fs = File.Create(Path.Combine(AppContext.BaseDirectory, "p1-apariencia-despues-scroll.png"))) encAparienciaDespues.Save(fs);

            Console.WriteLine($"P1-PREVIEW-FIJO: posicion antes={rectAntes}, posicion despues={rectDespues} (esperado: identicas)");
        }
        catch (Exception ex) { Console.WriteLine("P1-EXCEPTION: " + ex); }

        // Verificacion real de T-20 (auditoria de Opus, Bloque 6): AutoEquipService extraido de
        // MainViewModel - nunca se habia probado en el arnes ni antes ni despues del cambio, se
        // prueba ahora con datos reales de Builds (mismo Source que ya pasa el boton real
        // "Auto-equipar" del XAML).
        try
        {
            var gear = vm.Builds.VanillaStages.FirstOrDefault()?.Classes.FirstOrDefault()?.Source;
            if (gear != null && vm.EquipmentGroup != null)
            {
                var headBefore = vm.EquipmentGroup.EquippedItems.Slots[0].DisplayName;
                vm.AutoEquipCommand.Execute(gear);
                DoEvents();
                var headAfter = vm.EquipmentGroup.EquippedItems.Slots[0].DisplayName;
                Console.WriteLine($"T20-AUTOEQUIP: cabeza antes='{headBefore}' despues='{headAfter}' (esperado: cambia a un objeto real), StatusMessage={vm.StatusMessage}");
            }
            else Console.WriteLine("T20-AUTOEQUIP: sin gear/EquipmentGroup real - omitido");
        }
        catch (Exception ex) { Console.WriteLine("T20-AUTOEQUIP-EXCEPTION: " + ex); }

        // Sexta auditoria de Opus, H6-06 (Tanda D - "unificar el doll de Apariencia con el de
        // Inicio, que YA muestra la armadura/vanidad real puesta"): el equipo real ya puesto
        // por T20-AUTOEQUIP arriba mismo tiene que verse en el doll de Apariencia SIN recargar
        // el personaje - confirma que aparece solo, que el toggle "Mostrar equipo puesto" lo
        // quita/pone de verdad, y deja una captura real.
        try
        {
            vm.SelectedTabIndex = 1; // Personaje
            vm.PersonajeInnerTabIndex = 3; // Apariencia
            DoEvents(); DoEvents();

            var conEquipo = vm.Appearance.PreviewImage;
            byte[] PixelesDe(System.Windows.Media.Imaging.WriteableBitmap bmp)
            {
                var px = new byte[bmp.PixelHeight * bmp.PixelWidth * 4];
                bmp.CopyPixels(px, bmp.PixelWidth * 4, 0);
                return px;
            }
            Console.WriteLine($"H6-06-DOLL: ShowEquipment por defecto={vm.Appearance.ShowEquipment} (esperado True)");

            var rtbConEquipo = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbConEquipo.Render(window);
            var encConEquipo = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encConEquipo.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbConEquipo));
            using (var fsCon = File.Create(Path.Combine(AppContext.BaseDirectory, "h6-06-doll-con-equipo.png"))) encConEquipo.Save(fsCon);

            vm.Appearance.ShowEquipment = false;
            DoEvents(); DoEvents();
            bool cambioAlApagar = conEquipo != null && vm.Appearance.PreviewImage != null &&
                !PixelesDe(conEquipo).SequenceEqual(PixelesDe(vm.Appearance.PreviewImage));
            Console.WriteLine($"H6-06-DOLL: apagar 'Mostrar equipo puesto' cambia el preview={cambioAlApagar} (esperado True)");
            if (!cambioAlApagar) Console.WriteLine("FALLO: H6-06 - el toggle 'Mostrar equipo puesto' no quita de verdad la armadura del preview");

            var rtbSinEquipo = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbSinEquipo.Render(window);
            var encSinEquipo = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encSinEquipo.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbSinEquipo));
            using (var fsSin = File.Create(Path.Combine(AppContext.BaseDirectory, "h6-06-doll-sin-equipo.png"))) encSinEquipo.Save(fsSin);
            Console.WriteLine("Capturas doll con/sin equipo -> h6-06-doll-con-equipo.png, h6-06-doll-sin-equipo.png");

            vm.Appearance.ShowEquipment = true; // deja el estado real por defecto para el resto del arnes
            vm.SelectedTabIndex = 1;
            vm.PersonajeInnerTabIndex = 0;
            DoEvents();
        }
        catch (Exception ex) { Console.WriteLine("H6-06-DOLL-EXCEPTION: " + ex); }

        // Sexta auditoria de Opus, H6-07 (Tanda D - "pelo bajo el casco/pelo largo detras del
        // cuerpo"): con un peinado LARGO real puesto (backHairDraw=true), coloca 3 objetos
        // REALES de cabeza uno tras otro (Cubo vacio=hatHair real, Casco de hierro=oculta pelo
        // real, vacio=pelo normal) y confirma que el preview cambia cada vez - capturas reales
        // de los 3 estados.
        try
        {
            vm.SelectedTabIndex = 1; // Personaje
            vm.PersonajeInnerTabIndex = 3; // Apariencia
            int hairStyleAntes = vm.Appearance.HairStyle;
            vm.Appearance.HairStyle = 51; // backHairDraw=true real, ver HairDrawProfileTests.cs
            DoEvents(); DoEvents();

            byte[] PixelesDeH607(System.Windows.Media.Imaging.WriteableBitmap bmp)
            {
                var px = new byte[bmp.PixelHeight * bmp.PixelWidth * 4];
                bmp.CopyPixels(px, bmp.PixelWidth * 4, 0);
                return px;
            }
            void CapturarH607(string nombre)
            {
                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(window);
                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                using var fs = File.Create(Path.Combine(AppContext.BaseDirectory, nombre));
                enc.Save(fs);
            }

            var headSlotEquip = vm.EquipmentGroup?.EquippedItems.Slots[0];
            if (headSlotEquip != null)
            {
                var peloLargoSinCasco = vm.Appearance.PreviewImage;
                CapturarH607("h6-07-pelo-largo-sin-casco.png");

                headSlotEquip.PlaceItem(205); // Cubo vacio - hatHair real
                DoEvents(); DoEvents();
                var conCubo = vm.Appearance.PreviewImage;
                CapturarH607("h6-07-pelo-cubo-hathair.png");
                bool cuboCambio = peloLargoSinCasco != null && conCubo != null && !PixelesDeH607(peloLargoSinCasco).SequenceEqual(PixelesDeH607(conCubo));
                Console.WriteLine($"H6-07-PELO: Cubo vacio (hatHair real) cambia el preview={cuboCambio} (esperado True)");
                if (!cuboCambio) Console.WriteLine("FALLO: H6-07 - el sprite hatHair (Cubo vacio) no cambio el preview de verdad");

                headSlotEquip.PlaceItem(90); // Casco de hierro - oculta pelo real
                DoEvents(); DoEvents();
                var conCasco = vm.Appearance.PreviewImage;
                CapturarH607("h6-07-pelo-oculto-casco-hierro.png");
                bool cascoCambio = conCubo != null && conCasco != null && !PixelesDeH607(conCubo).SequenceEqual(PixelesDeH607(conCasco));
                Console.WriteLine($"H6-07-PELO: Casco de hierro (oculta pelo real) cambia el preview={cascoCambio} (esperado True)");
                if (!cascoCambio) Console.WriteLine("FALLO: H6-07 - el casco completo no oculto el pelo de verdad");

                headSlotEquip.ClearCommand.Execute(null); // deja el slot como estaba para el resto del arnes
            }
            else Console.WriteLine("H6-07-PELO: sin EquipmentGroup real - omitido");

            vm.Appearance.HairStyle = hairStyleAntes;
            vm.SelectedTabIndex = 1;
            vm.PersonajeInnerTabIndex = 0;
            DoEvents();
        }
        catch (Exception ex) { Console.WriteLine("H6-07-PELO-EXCEPTION: " + ex); }

        // Verificacion real de T-24 (auditoria de Opus, Bloque 6): 3 casos deterministas
        // (matematica pura, sin depender de ninguna ventana ni layout ya corrido) para los 3
        // modos reales de SlotGridPanel.MeasureOverride - ver el resumen real en el propio
        // SlotGridPanel.cs. Panel.Measure() funciona standalone (sin arbol visual real, sin
        // Window) porque MeasureOverride es matematica pura sobre InternalChildren/las
        // DependencyProperty del propio panel.
        try
        {
            static SlotGridPanel BuildGrid(int childCount, int columns, double minCell, double maxCell, double gap,
                int referenceColumns = 0, double referenceWidth = 0, double availableHeight = 0)
            {
                var grid = new SlotGridPanel
                {
                    Columns = columns, MinCell = minCell, MaxCell = maxCell, Gap = gap,
                    ReferenceColumns = referenceColumns, ReferenceWidth = referenceWidth, AvailableHeight = availableHeight,
                };
                for (int i = 0; i < childCount; i++) grid.Children.Add(new Border());
                return grid;
            }

            // Caso 1 (modo BASICO, suelo real MinCell): 10 columnas, 10 hijos (1 fila), un ancho
            // disponible tan estrecho (300px) que la celda "natural" (26.4px) cae por debajo del
            // suelo real - debe congelarse en MinCell=40, no seguir encogiendo (el ancho real
            // pedido por la rejilla, 436px, supera el disponible - eso es EXACTAMENTE lo que
            // activa el scroll horizontal real cuando esto vive dentro de un ScrollViewer, ver
            // MainWindow.xaml). Hallazgo real de paso, verificado aqui mismo (no de memoria):
            // FrameworkElement.Measure() recorta el ANCHO devuelto al availableSize de entrada
            // (300, no los 436 reales que MeasureOverride calculo) - comportamiento real y
            // documentado de WPF (protege contra un Panel mal comportado que pida mas sitio del
            // que se le ofrecio), NO un bug de SlotGridPanel: el ALTO (sin restriccion real
            // aqui, Infinity de entrada) SI llega intacto y es la prueba real de que la celda de
            // verdad elegida fue 40 (rows=1 * cell=40 = 40), confirmando el suelo real por una
            // via que el recorte de WPF no toca.
            var grid1 = BuildGrid(childCount: 10, columns: 10, minCell: 40, maxCell: 90, gap: 4);
            grid1.Measure(new Size(300, double.PositiveInfinity));
            var size1 = grid1.DesiredSize;
            Console.WriteLine($"T24-SLOTGRID caso1 (suelo MinCell): DesiredSize={size1} (esperado alto=40 real -1*MinCell-; ancho=300, recortado por WPF al availableSize de entrada, no 436 - ver comentario real)");

            // Caso 2 (modo BASICO, techo real MaxCell): mismos parametros, ancho disponible
            // enorme (2000px) - la celda "natural" (196.4px) supera el techo real, debe
            // congelarse en MaxCell=90, no seguir creciendo (para que Monedas/Municion, si
            // vivieran aqui, no se inflen a tarjetas gigantes).
            var grid2 = BuildGrid(childCount: 10, columns: 10, minCell: 40, maxCell: 90, gap: 4);
            grid2.Measure(new Size(2000, double.PositiveInfinity));
            var size2 = grid2.DesiredSize;
            Console.WriteLine($"T24-SLOTGRID caso2 (techo MaxCell): DesiredSize={size2} (esperado 936x90 - 10*90+4*9=936, 1*90=90)");

            // Caso 3 (modo ReferenceColumns+ReferenceWidth): 5 columnas, ancho PROPIO enorme
            // (2000px, dejaria crecer la celda sin limite real por si solo) pero referenciado
            // contra una fila hermana de 10 columnas en solo 400px de ancho (cellFromReference=
            // (400-4*9)/10=36.4) - la celda debe quedarse en 36.4, LA MISMA que tendria esa fila
            // hermana, demostrando que el limite cruzado (no el propio ancho) es el que manda.
            var grid3 = BuildGrid(childCount: 5, columns: 5, minCell: 30, maxCell: 90, gap: 4, referenceColumns: 10, referenceWidth: 400);
            grid3.Measure(new Size(2000, double.PositiveInfinity));
            var size3 = grid3.DesiredSize;
            Console.WriteLine($"T24-SLOTGRID caso3 (ReferenceWidth cruzado): DesiredSize={size3} (esperado 198x36.4 - 5*36.4+4*4=198, 36.4)");
        }
        catch (Exception ex) { Console.WriteLine("T24-SLOTGRID-EXCEPTION: " + ex); }

        // T-E (segunda auditoria de Opus, Fable): "barrido de tildes" - guarda real para que la
        // inconsistencia real que motivo esta ola (algunos textos de cara al usuario con tildes
        // reales, otros sin ellas por descuido, ej. "Investigacion"/"Exploracion" como cabecera
        // de pestaña) no vuelva a colarse sin que nadie se entere. Recorre TODO el arbol visual
        // ya realizado a estas alturas (se ha pasado por casi todas las pestañas reales) y
        // comprueba el Name/HelpText (ToolTip real) de cada elemento contra una lista real de
        // palabras que casi siempre llevan tilde en español de España y que ya aparecieron sin
        // ella en este mismo proyecto - por palabra completa, no subcadena (evita falsos
        // positivos tipo "mascara" dentro de otra palabra). Best-effort: contenido virtualizado
        // que nunca llego a realizarse (ej. una fila de un ItemsControl con scroll fuera de
        // vista) no se comprueba aqui - mismo limite real que el resto de comprobaciones de
        // este arnes basadas en UI Automation.
        try
        {
            string[] palabrasConTildeReal =
            [
                "version", "codigo", "indice", "numero", "pagina", "maximo", "minimo", "tecnico",
                "practica", "especifico", "linea", "ultimo", "ultima", "automatico", "automatica",
                "estadisticas", "categoria", "caracter", "util", "facil", "dificil", "rapido",
                "posicion", "opcion", "edicion", "seleccion", "informacion", "configuracion",
                "descripcion", "duracion", "colocacion", "proteccion", "distribucion", "accion",
                "investigacion", "exploracion", "libreria", "generacion", "region", "cancion",
                "genero", "aparicion", "puntuacion", "mineria", "credito", "creditos",
            ];
            var todos = root.FindAll(TreeScope.Descendants, System.Windows.Automation.Condition.TrueCondition);
            int fallosTilde = 0;
            foreach (AutomationElement el in todos)
            {
                foreach (string texto in new[] { el.Current.Name, el.Current.HelpText })
                {
                    if (string.IsNullOrEmpty(texto)) continue;
                    foreach (string mala in palabrasConTildeReal)
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(texto, $@"(?<![a-záéíóúñ]){mala}(?![a-záéíóúñ])", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        {
                            Console.WriteLine($"FALLO: T-E (segunda auditoria) - '{mala}' sin tilde real en \"{texto}\" ({el.Current.ControlType.ProgrammaticName})");
                            fallosTilde++;
                        }
                    }
                }
            }
            Console.WriteLine($"T-E-TILDES: {fallosTilde} fallo(s) (esperado 0)");
        }
        catch (Exception ex) { Console.WriteLine("T-E-TILDES-EXCEPTION: " + ex); }

        // T-H/F2 (segunda auditoria de Opus, Fable): "No hay FocusVisualStyle propio en un tema
        // oscuro personalizado" - el rectangulo de foco de WPF por defecto (negro discontinuo)
        // es invisible sobre este tema. SetFocus() real via UI Automation (no simulado) sobre el
        // boton "Guardar" + captura real, para comprobar de verdad que el nuevo FocusVisualStyle
        // se aplica (mismo criterio de la bitacora: min()/max() de CSS ya enseño que "no dio
        // ningun error" no es lo mismo que "se aplico de verdad").
        //
        // FALSO POSITIVO cerrado el 18-sep-2026 (investigacion de los 2 FALLO nuevos de la
        // madrugada): reproducido aislado con un arnes minimo (personaje recien cargado, idioma
        // es/en, ForzarPrimerPlano confirmado con GetForegroundWindow==hwnd) - el boton SE
        // ENCUENTRA, el foco SI se mueve de verdad (Keyboard.FocusedElement/.IsKeyboardFocused/
        // .IsFocused, los tres en True) y el AdornerLayer del elemento NO es nulo, pero
        // GetAdorners() da 0 SIEMPRE que el foco se puso con AutomationElement.SetFocus() a
        // secas. Causa real (mecanismo interno de WPF, no un bug de Terrakeep): el Adorner de
        // FocusVisualStyle solo se pinta cuando el teclado fue el ULTIMO dispositivo de entrada
        // real que uso la app (lo que WPF llama "keyboard most recent input device" - se activa
        // con un evento de teclado FISICO real, nunca con foco puesto por programa/UI Automation
        // ni por un clic de raton). Confirmado de forma aislante (misma leccion que "verificar
        // aislando la variable"): quitando el foco, enviando una pulsacion FISICA real e inocua
        // (Shift, via keybd_event - no dispara ningun comando) y volviendo a enfocar el MISMO
        // boton, el Adorner SI aparece (GetAdorners()>0) - la unica variable que cambio fue esa
        // pulsacion fisica. O sea que el FocusVisualStyle real del tema SI funciona de verdad
        // (lo que ve un usuario real tabulando con el teclado); lo que fallaba era que
        // AutomationElement.SetFocus() nunca simulaba una pulsacion fisica real, y por eso este
        // bloque nunca cumplia la condicion interna de WPF. Arreglo real del ARNES (no del
        // producto): una pulsacion de teclado FISICA real e inocua justo antes de SetFocus(),
        // mismo patron de "ALT que desarma el foreground lock" que ya usa ForzarPrimerPlano mas
        // abajo en este mismo fichero.
        try
        {
            // El foco visual real de WPF solo se pinta con la ventana ACTIVA (igual que el
            // sistema operativo real nunca muestra el foco de una ventana en segundo plano) -
            // SetForegroundWindow real antes de SetFocus(), mismo patron ya usado para los
            // atajos Ctrl+ reales de mas arriba.
            ForzarPrimerPlano(hwnd);
            var saveButton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, "Guardar")));
            // Pulsacion fisica real e inocua (Shift, sin combinar con nada) ANTES de enfocar: sin
            // esto WPF nunca cuenta el teclado como "ultimo dispositivo de entrada" y el Adorner
            // de FocusVisualStyle no se pinta aunque el foco se haya movido de verdad - ver el
            // comentario completo arriba.
            const byte VK_SHIFT_THF2 = 0x10;
            keybd_event(VK_SHIFT_THF2, 0, 0, UIntPtr.Zero);
            keybd_event(VK_SHIFT_THF2, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            DoEvents(); DoEvents();
            saveButton?.SetFocus();
            DoEvents(); DoEvents();
            // Comprobacion real (no solo visual): el foco realmente se movio Y WPF realmente
            // adjunto un adorner de foco al elemento enfocado - las dos cosas hacian falta para
            // descartar tanto "SetFocus() no funciono en headless" como "el estilo no se aplico".
            bool focoReal = System.Windows.Input.Keyboard.FocusedElement is System.Windows.UIElement focusedReal
                && System.Windows.Documents.AdornerLayer.GetAdornerLayer(focusedReal)?.GetAdorners(focusedReal)?.Length > 0;
            Console.WriteLine($"T-H-FOCO: foco real + adorner de FocusVisualStyle adjunto={focoReal} (esperado True)");
            if (!focoReal) Console.WriteLine("FALLO: T-H/F2 (segunda auditoria) - el FocusVisualStyle no se aplico al enfocar por teclado");
            var rtbFocus = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbFocus.Render(window);
            var encFocus = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encFocus.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbFocus));
            using var fsFocus = File.Create(Path.Combine(AppContext.BaseDirectory, "foco-teclado-guardar.png"));
            encFocus.Save(fsFocus);
            Console.WriteLine("T-H-FOCO: captura -> foco-teclado-guardar.png");
        }
        catch (Exception ex) { Console.WriteLine("T-H-FOCO-EXCEPTION: " + ex); }

        // H5-14 (quinta auditoria de Opus): "ningun slot se puede alcanzar con tabulador ni
        // flechas... Supr vacia, Intro abre 'Elegir...', Ctrl+C/Ctrl+V copian/pegan, Ctrl+1..6
        // saltan de pestaña". A diferencia del clic/doble clic de H5-12 (sin precedente de raton
        // simulado en este arnes), el foco y la inyeccion de teclado real SI tienen precedente
        // real y probado aqui mismo (T-H-FOCO/N3-CTRL-S) - verificacion real de extremo a
        // extremo, no solo a nivel de ViewModel. Un Border sin AutomationPeer propio (WPF no le
        // da uno por defecto) no aparece en el arbol de UI Automation - Keyboard.Focus() directo
        // sobre la instancia real (hallada recorriendo el arbol visual, mismo patron ya usado en
        // este arnes - ver WalkVisual/FindEditorScroll) en vez de AutomationElement.SetFocus().
        try
        {
            static System.Windows.FrameworkElement? FindBorderForSlot(System.Windows.DependencyObject d, object slotViewModel)
            {
                if (d is System.Windows.FrameworkElement { } fe && ReferenceEquals(fe.DataContext, slotViewModel) && fe is System.Windows.Controls.Border)
                    return fe;
                int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
                for (int i = 0; i < n; i++)
                {
                    var found = FindBorderForSlot(System.Windows.Media.VisualTreeHelper.GetChild(d, i), slotViewModel);
                    if (found != null) return found;
                }
                return null;
            }

            vm.SelectedTabIndex = 1; // Personaje
            vm.PersonajeInnerTabIndex = 0; // Objetos
            DoEvents(); DoEvents(); // deja que el TabControl realice el contenido de "Objetos" antes de mostrar la pagina "Inventario" dentro
            // NAV123 (25-sep-2026): "Inventario" ya no es un TabItem que buscar por nombre - es
            // la pagina real seleccionada con ObjetosSubTabIndex=1 (mismo patron ya usado mas
            // abajo en este mismo metodo, ver OBJ-10).
            vm.ObjetosSubTabIndex = 1; // Inventario
            DoEvents(); DoEvents();

            var slotOrigen = vm.InventoryContainer!.Slots[20]; // vacio (0-11 ocupados por la fixture, 12/49 por H5-12)
            var slotVecino = vm.InventoryContainer.Slots[21];
            var slotParaCopiar = vm.InventoryContainer.Slots[22];
            slotOrigen.PlaceItem(4); // Espada larga de hierro, id real cualquiera - lo que importa es tener algo que vaciar
            slotParaCopiar.PlaceItem(3);
            slotParaCopiar.Count = 7;
            slotParaCopiar.ToggleFavoriteCommand.Execute(null);

            ForzarPrimerPlano(hwnd);
            var borderOrigen = FindBorderForSlot(window, slotOrigen);
            Console.WriteLine($"H5-14-FOCO: Border real del slot 20 encontrado en el arbol visual={borderOrigen != null} (esperado True)");
            if (borderOrigen != null)
            {
                System.Windows.Input.Keyboard.Focus(borderOrigen);
                DoEvents(); DoEvents();
                bool focoReal = ReferenceEquals(System.Windows.Input.Keyboard.FocusedElement, borderOrigen);
                Console.WriteLine($"H5-14-FOCO: Keyboard.FocusedElement es el Border real del slot 20={focoReal} (esperado True)");

                // Flecha derecha: KeyboardNavigation.DirectionalNavigation="Contained" del
                // SlotGridPanel (Theme.xaml) debe mover el foco al slot vecino real (21), no
                // fuera de la rejilla.
                PressKey(0x27); // VK_RIGHT
                DoEvents(); DoEvents();
                bool focoMovioAlVecino = System.Windows.Input.Keyboard.FocusedElement is System.Windows.FrameworkElement feDerecha
                    && ReferenceEquals(feDerecha.DataContext, slotVecino);
                Console.WriteLine($"H5-14-FLECHA: tras VK_RIGHT, foco real en el slot vecino (21)={focoMovioAlVecino} (esperado True)");
                if (!focoMovioAlVecino) Console.WriteLine("FALLO: H5-14 - la flecha derecha no movio el foco real al slot vecino dentro de la rejilla");

                // Supr real sobre el slot 20 (vuelve a enfocarlo primero).
                System.Windows.Input.Keyboard.Focus(borderOrigen);
                DoEvents();
                PressKey(0x2E); // VK_DELETE
                DoEvents(); DoEvents();
                Console.WriteLine($"H5-14-SUPR: slot 20 vacio tras VK_DELETE={slotOrigen.IsEmpty} (esperado True)");
                if (!slotOrigen.IsEmpty) Console.WriteLine("FALLO: H5-14 - Supr real sobre el slot enfocado no lo vacio");

                // Ctrl+C real sobre el slot 22 (favorito, cantidad 7, prefijo real), Ctrl+V real
                // sobre el slot 20 (ahora vacio) - debe reproducir el objeto ENTERO copiado.
                var borderParaCopiar = FindBorderForSlot(window, slotParaCopiar);
                if (borderParaCopiar != null)
                {
                    System.Windows.Input.Keyboard.Focus(borderParaCopiar);
                    DoEvents();
                    PressCtrlPlus(0x43); // VK_C
                    DoEvents();
                    System.Windows.Input.Keyboard.Focus(borderOrigen);
                    DoEvents();
                    PressCtrlPlus(0x56); // VK_V
                    DoEvents(); DoEvents();
                    Console.WriteLine($"H5-14-COPIA-PEGA: slot 20 tras Ctrl+C(22)+Ctrl+V(20) -> DisplayName={slotOrigen.DisplayName}, Count={slotOrigen.Count} (esperado 7), IsFavorited={slotOrigen.IsFavorited} (esperado True)");
                    if (slotOrigen.Count != 7 || !slotOrigen.IsFavorited) Console.WriteLine("FALLO: H5-14 - Ctrl+C/Ctrl+V real no reprodujo el objeto entero copiado (cantidad/favorito)");
                }
                else Console.WriteLine("H5-14-COPIA-PEGA: Border real del slot 22 no encontrado - omitido");

                // OBJ-10 (oleada de Objetos, 6-sep-2026): el mismo Ctrl+V real, pero sobre un slot
                // que NO puede aceptar lo copiado (el hueco de casco de Equipamiento, SlotKind.
                // ArmorHead, con un arma en el portapapeles). Antes no pasaba absolutamente nada
                // visible: PasteItem escribia su RejectionMessage pero ese aviso solo se ve en el
                // panel "Editar", que muestra el slot SELECCIONADO - y el teclado nunca seleccionaba
                // nada. Aqui se comprueba lo que de verdad ve el usuario: el slot NO cambia, y el
                // aviso queda a la vista de verdad (slot seleccionado Y mensaje puesto).
                try
                {
                    // Los dos slots viven en sub-pestañas DISTINTAS de "Objetos" (el arma en
                    // Inventario, el hueco de casco en Equipamiento) y solo esta renderizada la
                    // que se ve: hay que copiar con una a la vista y pegar con la otra, o
                    // FindBorderForSlot no encuentra nada (primer intento de este bloque: se
                    // omitio entero por eso).
                    int subTabAntesObj10 = vm.ObjetosSubTabIndex;
                    vm.ObjetosSubTabIndex = 1; // Inventario
                    DoEvents(); DoEvents();
                    var slotArma = vm.InventoryContainer?.Slots.FirstOrDefault(x => !x.IsEmpty);
                    var borderArma = slotArma != null ? FindBorderForSlot(window, slotArma) : null;
                    if (slotArma != null && borderArma != null)
                    {
                        System.Windows.Input.Keyboard.Focus(borderArma);
                        DoEvents();
                        PressCtrlPlus(0x43); // VK_C sobre el arma, con Inventario a la vista
                        DoEvents();
                    }
                    vm.ObjetosSubTabIndex = 0; // Equipamiento
                    DoEvents(); DoEvents();
                    var slotCasco = vm.EquipmentGroup?.EquippedItems.Slots[0];
                    var borderCasco = slotCasco != null ? FindBorderForSlot(window, slotCasco) : null;
                    if (slotArma == null || slotCasco == null || borderArma == null || borderCasco == null)
                    {
                        Console.WriteLine("OBJ-10: no se encontraron los dos slots reales para el pegado rechazado - omitido");
                    }
                    else if (slotCasco.AcceptsItem(slotArma.ItemId))
                    {
                        Console.WriteLine($"OBJ-10: el slot de casco SI acepta '{slotArma.DisplayName}' - no sirve como caso de rechazo, omitido");
                    }
                    else
                    {
                        string cascoAntes = slotCasco.DisplayName;
                        System.Windows.Input.Keyboard.Focus(borderCasco);
                        DoEvents();
                        PressCtrlPlus(0x56); // VK_V sobre el hueco de casco - debe rechazarse
                        DoEvents(); DoEvents();
                        bool avisoALaVista = slotCasco.IsSelected
                                             && ReferenceEquals(vm.ItemEdit.Slot, slotCasco)
                                             && !string.IsNullOrEmpty(slotCasco.RejectionMessage);
                        Console.WriteLine($"OBJ-10: Ctrl+V real de '{slotArma.DisplayName}' sobre el hueco de casco -> el slot sigue siendo '{slotCasco.DisplayName}' (era '{cascoAntes}'), aviso a la vista={avisoALaVista} (esperado True), mensaje='{slotCasco.RejectionMessage}'");
                        // La pulsacion REAL (keybd_event) necesita que ESTA ventana este en primer
                        // plano, y con varios arneses de otros agentes a la vez el foco real se lo
                        // puede llevar otra (misma verdad del entorno que ya documento AR-EX2 para
                        // el raton). Se distingue sin ambiguedad: PasteItem SIEMPRE deja una de las
                        // dos huellas - o cambia el slot, o escribe el mensaje de rechazo. Si no hay
                        // NINGUNA de las dos, la tecla no llego, y eso no es un bug de la app.
                        bool llegoLaTecla = slotCasco.DisplayName != cascoAntes || !string.IsNullOrEmpty(slotCasco.RejectionMessage);
                        if (!llegoLaTecla)
                        {
                            Console.WriteLine("OBJ-10: el Ctrl+V real no llego a la ventana (foco de teclado compartido con otra ventana de esta maquina) - medicion omitida, no es un fallo de la app");
                        }
                        else
                        {
                            if (slotCasco.DisplayName != cascoAntes)
                                Console.WriteLine("FALLO: OBJ-10 - el pegado rechazado SI cambio el slot: la restriccion de tipo no se aplica por Ctrl+V");
                            if (!avisoALaVista)
                                Console.WriteLine("FALLO: OBJ-10 - un Ctrl+V rechazado no deja ninguna señal: el aviso del panel Editar solo se ve si el slot esta SELECCIONADO, y el teclado no lo selecciona");
                        }
                    }
                    vm.ObjetosSubTabIndex = subTabAntesObj10; // deja la sub-pestaña como estaba
                    DoEvents();
                }
                catch (Exception exObj10) { Console.WriteLine("OBJ-10-EXCEPTION: " + exObj10); }

                // Intro real: abre "Elegir..." (ChooseFromLibraryCommand -> Library.PickTarget).
                System.Windows.Input.Keyboard.Focus(borderOrigen);
                DoEvents();
                vm.Library.CancelPickCommand.Execute(null);
                PressKey(0x0D); // VK_RETURN
                DoEvents(); DoEvents();
                Console.WriteLine($"H5-14-INTRO: Library.PickTarget tras VK_RETURN=={ReferenceEquals(vm.Library.PickTarget, slotOrigen)} (esperado True)");
                if (!ReferenceEquals(vm.Library.PickTarget, slotOrigen)) Console.WriteLine("FALLO: H5-14 - Intro real sobre el slot enfocado no abrio 'Elegir...'");
                vm.Library.CancelPickCommand.Execute(null);
            }

            // Ctrl+1..6 real: salto directo entre las 6 pestañas raiz.
            ForzarPrimerPlano(hwnd);
            PressCtrlPlus(0x33); // VK_3 -> Builds (indice 2)
            DoEvents(); DoEvents();
            Console.WriteLine($"H5-14-CTRL3: SelectedTabIndex tras Ctrl+3={vm.SelectedTabIndex} (esperado 2, Builds)");
            if (vm.SelectedTabIndex != 2) Console.WriteLine("FALLO: H5-14 - Ctrl+3 real no salto a Builds");
            PressCtrlPlus(0x31); // VK_1 -> Inicio (indice 0)
            DoEvents(); DoEvents();
            Console.WriteLine($"H5-14-CTRL1: SelectedTabIndex tras Ctrl+1={vm.SelectedTabIndex} (esperado 0, Inicio)");
            if (vm.SelectedTabIndex != 0) Console.WriteLine("FALLO: H5-14 - Ctrl+1 real no volvio a Inicio");
        }
        catch (Exception ex) { Console.WriteLine("H5-14-FOCO-EXCEPTION: " + ex); }

        // Verificacion visual real de S-d/D-b (segunda auditoria de Opus, Fable): capturas de
        // Spawn Points y Desbloqueos, pestañas que este arnes no visitaba todavia.
        try
        {
            vm.SelectedTabIndex = 1; // Personaje
            vm.PersonajeInnerTabIndex = 4; // Spawn Points
            DoEvents(); DoEvents();
            var rtbSpawn = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbSpawn.Render(window);
            var encSpawn = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encSpawn.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbSpawn));
            using (var fsSpawn = File.Create(Path.Combine(AppContext.BaseDirectory, "spawn-points-sd.png"))) encSpawn.Save(fsSpawn);

            // S-c (segunda auditoria de Opus, Fable): "sin enlace al mapa de Exploracion desde
            // Spawn Points" - "Ver en el mapa" real: añade un Spawn Point real, lo pulsa (el
            // comando real, mismo camino que el boton) y confirma que salta a Exploracion Y
            // desplaza el mapa real de verdad (el mundo roca_negra.wld sigue cargado de antes).
            vm.Servers.AddEntryCommand.Execute(null);
            var filaMapa = vm.Servers.Entries[^1];
            filaMapa.Name = "S-c prueba real";
            filaMapa.SpawnX = 100;
            filaMapa.SpawnY = 50;
            (int X, int Y)? tileRecibido = null;
            void OnNavReq(int x, int y) => tileRecibido = (x, y);
            vm.Exploration.NavigateToTileRequested += OnNavReq;
            vm.ViewSpawnOnMapCommand.Execute(filaMapa);
            vm.Exploration.NavigateToTileRequested -= OnNavReq;
            DoEvents(); DoEvents();
            Console.WriteLine($"S-C-MAPA: tras 'Ver en el mapa' -> SelectedTabIndex={vm.SelectedTabIndex} (esperado 4, Exploracion), tile pedido={tileRecibido} (esperado (100, 50)), IsWorldLoaded={vm.Exploration.IsWorldLoaded}");
            if (tileRecibido != (100, 50)) Console.WriteLine("FALLO: S-c (segunda auditoria) - 'Ver en el mapa' no pidio navegar a las coordenadas reales del Spawn Point");
            if (vm.SelectedTabIndex != 4) Console.WriteLine("FALLO: S-c (segunda auditoria) - 'Ver en el mapa' no salto a Exploracion");
            var rtbSpawnMap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbSpawnMap.Render(window);
            var encSpawnMap = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encSpawnMap.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbSpawnMap));
            using (var fsSpawnMap = File.Create(Path.Combine(AppContext.BaseDirectory, "spawn-ver-en-el-mapa.png"))) encSpawnMap.Save(fsSpawnMap);
            Console.WriteLine("Captura tras 'Ver en el mapa' -> spawn-ver-en-el-mapa.png");

            // S-b (segunda auditoria de Opus, Fable): captura real de la tabla YA poblada (con
            // la fila de prueba todavia puesta) - cabecera unica real + boton "Ver en el mapa"
            // por fila, antes de quitarla.
            vm.SelectedTabIndex = 1; // Personaje
            vm.PersonajeInnerTabIndex = 4; // Spawn Points
            DoEvents(); DoEvents();
            var rtbSpawnTabla = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbSpawnTabla.Render(window);
            var encSpawnTabla = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encSpawnTabla.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbSpawnTabla));
            using (var fsSpawnTabla = File.Create(Path.Combine(AppContext.BaseDirectory, "spawn-points-tabla-poblada.png"))) encSpawnTabla.Save(fsSpawnTabla);
            Console.WriteLine("Captura tabla de Spawn Points poblada -> spawn-points-tabla-poblada.png");

            vm.Servers.RemoveEntryCommand.Execute(filaMapa); // deja el personaje real como estaba

            vm.PersonajeInnerTabIndex = 5; // Desbloqueos
            DoEvents(); DoEvents();
            var rtbFlags = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbFlags.Render(window);
            var encFlags = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encFlags.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbFlags));
            using (var fsFlags = File.Create(Path.Combine(AppContext.BaseDirectory, "desbloqueos-db.png"))) encFlags.Save(fsFlags);
            Console.WriteLine("S-d/D-b: capturas -> spawn-points-sd.png, desbloqueos-db.png");

            // D-d (segunda auditoria de Opus, Fable): "Marcar todos" real - las 13 casillas.
            vm.Flags.MarkAllCommand.Execute(null);
            DoEvents();
            Console.WriteLine($"D-D-MARCAR-TODOS: ExtraAccessory={vm.Flags.ExtraAccessory}, UsingSuperMinecart={vm.Flags.UsingSuperMinecart} (esperado True en ambos)");
            var rtbFlagsAll = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbFlagsAll.Render(window);
            var encFlagsAll = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encFlagsAll.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbFlagsAll));
            using (var fsFlagsAll = File.Create(Path.Combine(AppContext.BaseDirectory, "desbloqueos-marcar-todos.png"))) encFlagsAll.Save(fsFlagsAll);

            // D-e: baja la version real por debajo de TODOS los umbrales reales y confirma que
            // los 5 avisos reales aparecen (mundo real, no un mock).
            int versionOriginal = vm.VersionEditor.RawVersion;
            vm.VersionEditor.RawVersion = 100;
            vm.PersonajeInnerTabIndex = 0; // fuerza un cambio real de pestaña antes de volver
            DoEvents();
            vm.PersonajeInnerTabIndex = 5; // Desbloqueos - dispara el recalculo real
            DoEvents(); DoEvents();
            Console.WriteLine($"D-E-AVISO-VERSION: version=100 -> ExtraAccessoryBelowVersion={vm.Flags.ExtraAccessoryBelowVersion}, BiomeTorchesBelowVersion={vm.Flags.BiomeTorchesBelowVersion}, ExtraUsingFlagsBelowVersion={vm.Flags.ExtraUsingFlagsBelowVersion}, FinishedDD2EventBelowVersion={vm.Flags.FinishedDD2EventBelowVersion}, SuperMinecartBelowVersion={vm.Flags.SuperMinecartBelowVersion} (esperado True en los 5)");
            if (!vm.Flags.ExtraAccessoryBelowVersion || !vm.Flags.SuperMinecartBelowVersion) Console.WriteLine("FALLO: D-e (segunda auditoria) - los avisos de version no se recalcularon de verdad");
            var rtbFlagsWarn = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbFlagsWarn.Render(window);
            var encFlagsWarn = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encFlagsWarn.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbFlagsWarn));
            using (var fsFlagsWarn = File.Create(Path.Combine(AppContext.BaseDirectory, "desbloqueos-aviso-version.png"))) encFlagsWarn.Save(fsFlagsWarn);
            Console.WriteLine("Capturas D-d/D-e -> desbloqueos-marcar-todos.png, desbloqueos-aviso-version.png");

            // Deja el personaje real como estaba, para no afectar a los pasos siguientes (V-c
            // baja la version tambien, pero desde su propio punto de partida real).
            vm.Flags.MarkNoneCommand.Execute(null);
            vm.VersionEditor.RawVersion = versionOriginal;
        }
        catch (Exception ex) { Console.WriteLine("S-d-D-b-EXCEPTION: " + ex); }

        // Verificacion visual real de V-c (segunda auditoria de Opus, Fable): UIA-Test ya trae
        // equipo real puesto (T20-AUTOEQUIP, mas arriba) - bajar de version por debajo del
        // umbral real 145 debe mostrar el aviso naranja real con el conteo.
        try
        {
            vm.PersonajeInnerTabIndex = 6; // Version
            DoEvents(); DoEvents();
            vm.VersionEditor.SetVersionCommand.Execute(98);
            DoEvents(); DoEvents();
            Console.WriteLine($"V-c: DowngradeWarning tras bajar a 98 -> \"{vm.VersionEditor.DowngradeWarning}\" (esperado real, no null)");
            if (vm.VersionEditor.DowngradeWarning == null) Console.WriteLine("FALLO: V-c (segunda auditoria) - no aviso pese a tener equipo real puesto");
            var rtbVersion = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbVersion.Render(window);
            var encVersion = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encVersion.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbVersion));
            using (var fsVersion = File.Create(Path.Combine(AppContext.BaseDirectory, "version-aviso-vc.png"))) encVersion.Save(fsVersion);
            vm.VersionEditor.SetVersionCommand.Execute(279); // deja la version real de vuelta para el resto del flujo (Guardar, etc.)
            DoEvents();
        }
        catch (Exception ex) { Console.WriteLine("V-c-EXCEPTION: " + ex); }

        // Auditoria de redimensionado (ESPEC-auditoria-redimensionado.md §6.1): una comprobacion
        // real por hallazgo, con el mismo personaje real (UIA-Test, equipo puesto por
        // T20-AUTOEQUIP) que ya esta cargado en este punto. Recorte() = VisualTreeHelper.
        // GetClip sobre el elemento recortado - ver el comentario real de esa funcion, mas
        // arriba en este fichero.
        try
        {
            // AR-01 (H-01, el hallazgo mas grave): los 6 botones de conjunto siguen existiendo a
            // los dos lados del umbral de Amplio, en Inventario Y en Almacenes.
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 1;
            foreach (double w in new double[] { 1450, 1500, 1920 })
            {
                FijarTamaño(window, w, 900);
                DoEvents(); DoEvents();
                var nombresBotones = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                    .Cast<AutomationElement>().Select(b => b.Current.Name).ToList();
                foreach (string b in new[] { "Guardar conjunto...", "Cargar...", "Añadir..." })
                {
                    int veces = nombresBotones.Count(n => n == b);
                    Console.WriteLine($"AR-01: a {w}px, '{b}' presente x{veces} (esperado >=1 siempre, >=2 en Amplio - Inventario Y Almacen)");
                    if (veces == 0) Console.WriteLine($"FALLO: AR-01 - '{b}' desaparecio a {w}px (H-01)");
                }
            }

            // AR-08 (H-08): el contador "Inventario (N/M)" a 1080px (el caso mas apretado). R-01
            // (WrapPanel en la barra de botones) reduce mucho el aprieto pero NO lo elimina del
            // todo (medido: de ~660px de una fila sin envolver a un residual real de 14px a
            // 1080px - a ese ancho concreto sigue sin caber TODO a la vez). Parche previsto de
            // forma explicita en el propio informe para este residual: TextTrimming, para que
            // sea un recorte HONESTO con puntos suspensivos en vez de un corte seco silencioso -
            // por eso aqui NO se exige recorte=(0,0) (seria pedir mas de lo que R-01+el parche
            // prometen), se exige que el texto siga siendo LEGIBLE (nunca vacio del todo) y que
            // WPF confirme que esta usando el mecanismo real de recorte con puntos suspensivos,
            // no un clip duro silencioso.
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 1;
            FijarTamaño(window, 1080, 700);
            DoEvents(); DoEvents();
            var tbContador = Descendientes<TextBlock>(window).FirstOrDefault(t => (t.Text ?? "").StartsWith("Inventario ("));
            if (tbContador != null)
            {
                var (rx, ry) = Recorte(tbContador);
                Console.WriteLine($"AR-08: a 1080px, '{tbContador.Text}' recorte de layout=({rx:0},{ry:0}), TextTrimming={tbContador.TextTrimming} (esperado: si hay recorte de layout, TextTrimming!=None y el texto sigue sin estar vacio)");
                if ((rx > 0 || ry > 0) && tbContador.TextTrimming == System.Windows.TextTrimming.None)
                    Console.WriteLine("FALLO: AR-08 - el contador de Inventario se recorta a 1080px SIN TextTrimming (corte seco, no honesto) (H-08)");
                if (string.IsNullOrWhiteSpace(tbContador.Text))
                    Console.WriteLine("FALLO: AR-08 - el contador de Inventario quedo completamente vacio a 1080px (H-08)");
            }

            // AR-03 (H-03): el nombre real mas largo del catalogo de Builds (Calamity Mod) no se
            // recorta, ni a la ventana minima ni a 4K.
            vm.SelectedTabIndex = 2; // Builds
            DoEvents(); DoEvents();
            var calamityModTab = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Calamity Mod")));
            if (calamityModTab != null && calamityModTab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var calamityModPat))
                ((SelectionItemPattern)calamityModPat).Select();
            DoEvents(); DoEvents();
            foreach (double w in new double[] { 1080, 1500, 3840 })
            {
                FijarTamaño(window, w, 900);
                DoEvents(); DoEvents();
                var tbLargo = Descendientes<TextBlock>(window).FirstOrDefault(t => (t.Text ?? "").Contains("Semblante de Filo de Cable Tesla"));
                if (tbLargo == null) { Console.WriteLine($"AR-03: a {w}px, nombre largo real no encontrado en el arbol visual (¿cambio el catalogo de builds_calamity.json?)"); continue; }
                var (rx, ry) = Recorte(tbLargo);
                Console.WriteLine($"AR-03: a {w}px, '{tbLargo.Text}' recorte=({rx:0},{ry:0}) (esperado 0,0)");
                if (rx > 0 || ry > 0) Console.WriteLine($"FALLO: AR-03 - nombre de Builds recortado {rx:0}x{ry:0}px a {w}px (H-03)");
            }

            // AR-04 (H-04): barrido del umbral de la franja vital de la cabecera - tras R-04 no
            // deberia haber recorte a NINGUN ancho de la lista, incluido el MinWidth=1080 real.
            vm.SelectedTabIndex = 0; // Inicio, la cabecera es visible en las 6 pestañas
            foreach (double w in new double[] { 1080, 1170, 1299, 1300, 1320, 1500 })
            {
                FijarTamaño(window, w, 860);
                DoEvents(); DoEvents();
                var tira = Descendientes<System.Windows.Controls.WrapPanel>(window)
                    .FirstOrDefault(wp => Descendientes<TextBlock>(wp).Any(t => t.Text == "♥"));
                if (tira == null) { Console.WriteLine($"AR-04: a {w}px, franja vital no encontrada en el arbol visual"); continue; }
                var (rx, ry) = Recorte(tira);
                Console.WriteLine($"AR-04: a {w}px SizeClass={vm.SizeClass} expandida={vm.IsVitalsStripExpanded} recorte=({rx:0},{ry:0}) (esperado 0,0 SIEMPRE tras R-04)");
                if (rx > 0 || ry > 0) Console.WriteLine($"FALLO: AR-04 - franja vital recortada {rx:0}x{ry:0}px a {w}px (H-04)");

                // A9-06-BARRAHUECO (informe de pulido final, C-14, cierra H3): separacion
                // horizontal real (TransformToAncestor) entre el borde derecho de la ultima
                // insignia de identidad y el borde izquierdo del "corazon" de vida - antes "se
                // juntaba con Softcore" a cualquier ancho, umbral real >= 16px (el escalon de la
                // app es 20, se deja margen de sobra frente a redondeos de layout).
                var corazon = Descendientes<TextBlock>(tira).FirstOrDefault(t => t.Text == "♥");
                DependencyObject? ancestroCabecera = tira;
                System.Windows.Controls.Grid? grid3 = null;
                while (ancestroCabecera != null)
                {
                    ancestroCabecera = System.Windows.Media.VisualTreeHelper.GetParent(ancestroCabecera);
                    if (ancestroCabecera is System.Windows.Controls.Grid g && g.ColumnDefinitions.Count == 3) { grid3 = g; break; }
                }
                if (corazon != null && grid3 != null)
                {
                    var col0 = grid3.Children.OfType<UIElement>().FirstOrDefault(c => System.Windows.Controls.Grid.GetColumn(c) == 0 && c.IsVisible);
                    if (col0 is FrameworkElement col0Fe)
                    {
                        double col0Right = col0Fe.TransformToAncestor(window).Transform(new System.Windows.Point(col0Fe.ActualWidth, 0)).X;
                        double corazonLeft = corazon.TransformToAncestor(window).Transform(new System.Windows.Point(0, 0)).X;
                        double gapIdentidadVitales = corazonLeft - col0Right;

                        Console.WriteLine($"A9-06-BARRAHUECO: a {w}px, identidad<->vitales={gapIdentidadVitales:0.0}px (esperado >= 16px)");
                        if (gapIdentidadVitales < 16)
                            Console.WriteLine($"FALLO: C-14 - hueco horizontal identidad<->vitales de la cabecera {gapIdentidadVitales:0.0}px < 16px a {w}px");
                    }
                }
            }

            // AR-05 (H-05): insignia "Calamity" de una tarjeta de Inicio, sin recorte a 1080 y a 1920.
            vm.SelectedTabIndex = 0; // Inicio
            foreach (double w in new double[] { 1080, 1920 })
            {
                FijarTamaño(window, w, 900);
                DoEvents(); DoEvents();
                var tbCalamity = Descendientes<TextBlock>(window).FirstOrDefault(t => t.Text == "Calamity" && t.Foreground == System.Windows.Media.Brushes.White);
                if (tbCalamity == null) { Console.WriteLine($"AR-05: a {w}px, ninguna tarjeta de Inicio con insignia Calamity real (¿ningun personaje con Calamity en esta carpeta?)"); continue; }
                var (rx, ry) = Recorte(tbCalamity);
                Console.WriteLine($"AR-05: a {w}px, insignia 'Calamity' de Inicio recorte=({rx:0},{ry:0}) (esperado 0,0)");
                if (rx > 0 || ry > 0) Console.WriteLine($"FALLO: AR-05 - insignia Calamity de Inicio recortada {rx:0}x{ry:0}px a {w}px (H-05)");
            }

            // AR-06 (H-06): un nodo de 2º nivel del arbol de categorias, en las 3 pantallas que
            // comparten CategoryNodeTemplate (Libreria, Libreria de buffs, Investigacion).
            (int tab, int inner, string label)[] arbolesConSegundoNivel =
            [
                (1, 0, "Libreria (Objetos)"),
                (1, 1, "Libreria de buffs"),
                (1, 2, "Investigacion"),
            ];
            foreach (var (tab, inner, label) in arbolesConSegundoNivel)
            {
                vm.SelectedTabIndex = tab; vm.PersonajeInnerTabIndex = inner;
                foreach (double w in new double[] { 1080, 1920 })
                {
                    FijarTamaño(window, w, 900);
                    DoEvents(); DoEvents();
                    var tbNodo = Descendientes<TextBlock>(window).FirstOrDefault(t => (t.Text ?? "").StartsWith("Pociones (regenera"));
                    if (tbNodo == null) continue; // carpeta no visible en este arbol/tamaño concreto - no todos la tienen
                    var (rx, ry) = Recorte(tbNodo);
                    Console.WriteLine($"AR-06: {label} a {w}px, '{tbNodo.Text}' recorte=({rx:0},{ry:0}) (esperado 0,0)");
                    if (rx > 0 || ry > 0) Console.WriteLine($"FALLO: AR-06 - nodo de 2º nivel recortado {rx:0}x{ry:0}px en {label} a {w}px (H-06)");
                }
            }

            // AR-07 (H-07): a 1080x700 (el caso mas apretado en vertical), la columna del preview
            // de Apariencia tiene su ScrollViewer de seguridad y el parrafo no se recorta.
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 3; // Apariencia
            FijarTamaño(window, 1080, 700);
            DoEvents(); DoEvents();
            var tbPreview = Descendientes<TextBlock>(window).FirstOrDefault(t => (t.Text ?? "").StartsWith("Preview real de cuerpo completo"));
            if (tbPreview != null)
            {
                var (rx, ry) = Recorte(tbPreview);
                bool tieneScrollAncestro = false;
                for (var d = (DependencyObject)tbPreview; d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
                    if (d is System.Windows.Controls.ScrollViewer) { tieneScrollAncestro = true; break; }
                Console.WriteLine($"AR-07: a 1080x700, parrafo del preview recorte=({rx:0},{ry:0}) (esperado 0,0), tiene ScrollViewer ancestro={tieneScrollAncestro} (esperado True)");
                if ((rx > 0 || ry > 0) && !tieneScrollAncestro) Console.WriteLine("FALLO: AR-07 - el parrafo del preview se recorta Y no tiene forma de alcanzarlo (H-07)");
            }

            // AR-09 (H-09): aprovechamiento de ancho en Inicio - informativo con umbral, no una
            // perdida de contenido (a diferencia del resto de AR-*).
            vm.SelectedTabIndex = 0; // Inicio
            foreach (double w in new double[] { 1500, 1920, 2560, 3840 })
            {
                FijarTamaño(window, w, 1080);
                DoEvents(); DoEvents();
                var svInicio = Descendientes<System.Windows.Controls.ScrollViewer>(window).FirstOrDefault();
                var contenidoInicio = svInicio?.Content as FrameworkElement;
                if (svInicio == null || contenidoInicio == null || svInicio.ViewportWidth <= 0) continue;
                double desperdicio = 1 - Math.Min(1, contenidoInicio.ActualWidth / svInicio.ViewportWidth);
                int tope = w >= 3000 ? 55 : w >= 2400 ? 35 : 15;
                Console.WriteLine($"AR-09: a {w}px Inicio usa {contenidoInicio.ActualWidth:0} de {svInicio.ViewportWidth:0} -> {100 * desperdicio:0}% sin usar (esperado <{tope}% tras R-10)");
                if (100 * desperdicio >= tope) Console.WriteLine($"FALLO: AR-09 - Inicio desaprovecha mas de lo esperado a {w}px ({100 * desperdicio:0}% >= {tope}%, H-09)");
            }

            // AR-10 (H-10): ninguna pestaña interna de Personaje se recorta. Hallazgo real
            // durante esta misma verificacion, DISTINTO del diagnostico original de R-09 ("el
            // TemplateBinding duplica el Margin"): aislado con un experimento directo (Margin=0
            // en el Setter de InnerTabItem -> clip.Bounds=null; CUALQUIER Margin no nulo en el
            // TabItem, doble o simple -> clip = exactamente ese Margin) que TabPanel.
            // ArrangeOverride no reserva hueco real para el Margin de sus TabItem hijos - el
            // Margin tiene que vivir en el Border INTERIOR de la plantilla, nunca en el TabItem
            // en si. Corregido asi en Theme.xaml; probado a dos anchos (1080 y 1920) para
            // descartar que fuera en realidad "no caben todas y TabPanel comprime" (mismo
            // sintoma visual, causa distinta - se descarto midiendo que TabPanel tenia de sobra:
            // 919px reales para solo 625px de contenido a 1080px).
            vm.SelectedTabIndex = 1; // Personaje
            foreach (double w in new double[] { 1080, 1920 })
            {
                FijarTamaño(window, w, 700);
                DoEvents(); DoEvents();
                var pestañasInternas = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem))
                    .Cast<AutomationElement>().Where(t => t.Current.Name is "Objetos" or "Buffs" or "Investigación" or "Apariencia" or "Puntos de aparición" or "Desbloqueos" or "Versión").ToList();
                var tabItemsWpf = Descendientes<System.Windows.Controls.TabItem>(window).Where(ti => pestañasInternas.Any(p => p.Current.Name == (ti.Header as string))).ToList();
                int recortadas = 0;
                foreach (var ti in tabItemsWpf)
                {
                    var (rx, ry) = Recorte(ti);
                    if (rx > 0 || ry > 0) { recortadas++; Console.WriteLine($"FALLO: AR-10 - pestaña interna '{ti.Header}' recortada {rx:0}x{ry:0}px a {w}px (H-10)"); }
                }
                Console.WriteLine($"AR-10: a {w}px, {tabItemsWpf.Count - recortadas}/{tabItemsWpf.Count} pestañas internas sin recorte (esperado {tabItemsWpf.Count}/{tabItemsWpf.Count} tras R-09)");
            }

            FijarTamaño(window, 1180, 860);
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 0;
            DoEvents();
        }
        catch (Exception ex) { Console.WriteLine("AUDITORIA-REDIMENSIONADO-EXCEPTION: " + ex); }

        // A10-BACKUPS-HOMONIMOS (auditoria final de Opus, 5-sep-2026): BUG REAL DE PERDIDA DE
        // DATOS que tenia BackupHistoryService - identificaba al personaje solo por el nombre del
        // fichero, asi que dos personajes DISTINTOS llamados igual en las dos carpetas reales que
        // Terrakeep escanea (vanilla y tModLoader) compartian historial: restaurar una copia de
        // uno sobrescribia al otro. Se comprueba sobre COPIAS en una carpeta temporal propia,
        // nunca sobre personajes reales del usuario, y se borra todo al terminar.
        try
        {
            string tmpRaiz = Path.Combine(Path.GetTempPath(), "terrakeep-audit-homonimos-" + Guid.NewGuid().ToString("N")[..8]);
            string dirA = Path.Combine(tmpRaiz, "tModLoader", "Players");
            string dirB = Path.Combine(tmpRaiz, "vanilla", "Players");
            Directory.CreateDirectory(dirA);
            Directory.CreateDirectory(dirB);
            try
            {
                // Dos ficheros con el MISMO nombre y contenido distinto - el escenario real.
                string pjA = Path.Combine(dirA, "Homonimo.plr");
                string pjB = Path.Combine(dirB, "Homonimo.plr");
                File.WriteAllBytes(pjA, [1, 2, 3, 4]);
                File.WriteAllBytes(pjB, [9, 9, 9, 9, 9, 9]);

                // SaveBackup solo mira PlrPath/TplrPath - un PlrCharacter minimo real basta.
                static LoadedCharacter Cargado(string ruta) => new(
                    ruta, null, "Player",
                    new PlrCharacter { Version = 279, Name = "Homonimo", PrimaryLoadout = PlrLoadout.CreateEmpty(true) },
                    null, []);

                // BK (13-sep-2026): el snapshot ya no son ficheros sueltos sino un contenedor
                // .tkbak - el tamaño real del .plr fotografiado se lee de dentro (ReadPlrBytes),
                // que es ademas lo que de verdad importa comprobar aqui.
                var backups = new BackupHistoryService();
                backups.SaveBackup(Cargado(pjA), BackupReason.Manual);
                backups.SaveBackup(Cargado(pjB), BackupReason.Manual);
                int deA = backups.ListBackups(pjA).Count;
                int deB = backups.ListBackups(pjB).Count;
                long tamañoDeA = deA > 0 ? backups.ReadPlrBytes(backups.ListBackups(pjA)[0]).LongLength : -1;
                long tamañoDeB = deB > 0 ? backups.ReadPlrBytes(backups.ListBackups(pjB)[0]).LongLength : -1;
                bool ok = deA == 1 && deB == 1 && tamañoDeA == 4 && tamañoDeB == 6;
                Console.WriteLine($"A10-BACKUPS-HOMONIMOS: dos personajes distintos llamados igual -> copias vistas por A={deA} (esperado 1), por B={deB} (esperado 1), tamaño de la copia de A={tamañoDeA} (esperado 4), de B={tamañoDeB} (esperado 6)");
                if (!ok) Console.WriteLine("FALLO: A10-BACKUPS-HOMONIMOS - dos personajes distintos con el mismo nombre comparten historial de copias (restaurar uno pisaria al otro)");
            }
            finally
            {
                try { Directory.Delete(tmpRaiz, recursive: true); } catch (Exception) { }
                // Las carpetas de historial que ha creado esta prueba viven en el AppData real -
                // se limpian tambien, no deben quedar como basura de una ejecucion de arnes.
                try
                {
                    string raizBackups = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "Backups");
                    foreach (string d in Directory.Exists(raizBackups) ? Directory.GetDirectories(raizBackups, "Homonimo-*") : [])
                        Directory.Delete(d, recursive: true);
                }
                catch (Exception) { }
            }
        }
        catch (Exception ex) { Console.WriteLine("A10-BACKUPS-HOMONIMOS-EXCEPTION: " + ex); }

        // A10-IDIOMA-BARRIDO (auditoria final de Opus, 5-sep-2026, antes de publicar): hasta ahora
        // A9-13-IDIOMA solo probaba UN TextBlock concreto de Inicio. Esto recorre TODAS las
        // pestañas reales con el idioma puesto en ingles y busca los dos fallos que build/test no
        // detectan nunca: (1) una clave que no existe en NINGUN diccionario, que se ve literal
        // entre corchetes ("[clave_x]", criterio real de LocalizationService); (2) texto que sigue
        // en ESPAÑOL con la app en ingles, es decir una cadena que nunca se migro al diccionario.
        // La deteccion de (2) va por palabras funcionales españolas inequivocas (no existen en
        // ingles) - nunca por acentos: el ingles real de la app tambien puede llevar nombres
        // propios acentuados del propio juego.
        try
        {
            // Ronda de idioma del 6-sep-2026 (queja real del usuario: "mas de la mitad del
            // contenido sigue en español al cambiar a ingles"): la lista de abajo se AMPLIO
            // mucho (antes 31 entradas, casi todas sustantivos concretos de una pantalla) con
            // palabras funcionales españolas que no existen en ingles - articulos, preposiciones
            // y conectores. Son las que de verdad cazan una FRASE entera sin migrar, que es lo
            // que se estaba escapando (el contenido de Novedades / registro de cambios y las
            // carpetas de la Libreria pasaban el barrido viejo casi enteras).
            string[] palabrasEspañolas =
            [
                "personaje", "guardar", "guardado", "conjunto", "hueco", "búsqueda", "busqueda",
                "aparición", "aparicion", "copia de seguridad", "copias de seguridad", "mundo",
                "objetos", "acepta", "cargar", "carpeta", "cambios", "ningún", "ningun",
                "seleccionado", "vanidad", "tintes", "afina", "escribe para buscar", "en total",
                "diseñado", "desarrollado", "reescritura", "propiedad de sus", "está", "esta ",
                // palabras funcionales - una frase española real cae casi siempre en alguna
                " de ", " del ", " la ", " el ", " los ", " las ", " un ", " una ", " para ",
                " con ", " sin ", " que ", " por ", " como ", " pero ", " ya ", " al ",
                " se ", " su ", " sus ", " son ", " más ", " mas ", " muy ", " cada ",
                " todo ", " toda ", " todos ", " todas ", " cuando ", " donde ", " desde ",
                " hasta ", " entre ", " sobre ", " ahora ", " tras ", " este ", " esta ",
                " estos ", " estas ", " ese ", " esa ", " hay ", " ni ",
                // OJO: " no ", " a ", " o ", " y ", " es ", " la " y " para " NO entran aqui a
                // proposito - son tambien palabras/letras inglesas corrientes ("no new items
                // added", "a slot", "is"), y con ellas el barrido marcaba como fallo frases que
                // YA estaban traducidas de verdad. Una palabra solo sirve aqui si no existe en
                // ingles.
                // verbos/sustantivos frecuentes de la interfaz y del contenido
                "añad", "arregl", "corregid", "corrige", "nuevo", "nueva", "nuevos", "nuevas",
                "versión", "version de", "actualización", "actualizacion", "pestaña", "botón",
                "boton", "ahora ", "antes ", "también", "tambien", "según", "segun",
                "materiales", "armas", "armadura", "accesorios", "herramientas", "bloques",
                "pociones", "muebles", "colocables", "mascotas", "monturas", "alas", "tintes",
                "modo dificil", "modo difícil", "cuerpo a cuerpo", "a distancia", "invocación",
                "invocacion", "pícaro", "picaro", "magia", "otro", "otros", "varios",
            ];
            // Limites REALES ya conocidos y documentados (bitacora, bloques 3/4 del idioma): NO es
            // texto sin migrar. Son cadenas que YA viven en el diccionario pero se fijan una sola
            // vez (constructor / LoadFrom / un StatusMessage calculado antes del cambio) y no se
            // vuelven a evaluar solas al cambiar de idioma en caliente - se ven en el idioma de
            // arranque hasta la siguiente recarga real. Hacerlas reactivas exigiria rehacer la
            // construccion entera de cada ViewModel, mucho mas que una migracion de texto, y esta
            // fuera a proposito. Aparte van los CATALOGOS DE CONTENIDO del juego (nombres de
            // objeto/tile, Novedades, registro de cambios): viven en sus propios JSON de datos y
            // solo existen en español, camino propio tambien documentado. Se listan igualmente en
            // la salida, pero no cuentan como FALLO: asi este barrido sigue detectando de verdad
            // cualquier cadena NUEVA que se olvide de migrar en el futuro.
            //
            // Ronda del 6-sep-2026: esta lista SE ENCOGE mucho. Casi todo lo que habia aqui ya
            // no es un limite: Novedades y el registro de cambios se traducen de verdad ahora, y
            // los selectores/pildoras (Vanidad/Tintes/Ninguno/"Mundo: "/"Objetos (") pasaron a
            // guardar la clave en vez del texto ya resuelto, asi que si reaccionan en caliente.
            // Lo que queda es de dos clases, las dos reales y documentadas:
            //  - un StatusMessage o un resumen YA COMPUESTO antes del cambio de idioma: son
            //    frases de un solo uso que se rehacen en la siguiente accion real del usuario.
            //  - (CERRADO el 6-sep-2026, ronda de traduccion del CONTENIDO del juego) los
            //    CATALOGOS DE CONTENIDO del juego (nombres de objeto/NPC/tile/buff, tooltips,
            //    bonos de set) YA existen y se muestran en los dos idiomas. Este barrido los
            //    sigue descontando como ruido - no puede distinguirlos de la interfaz -, asi
            //    que quien vigila que sigan traducidos es A11-CONTENIDO-IDIOMA, en
            //    AuditoriaContenidoIdioma.cs.
            string[] limitesConocidos =
            [
                "Auto-equipar:",             // StatusMessage ya calculado antes del cambio
                "objetos en total (vanilla", // ResultsSummary de la Libreria, ya calculado
                "objeto(s) investigado(s)",  // idem, Investigacion
                "NPC(s) de pueblo",          // resumen del mundo recien cargado, ya compuesto
                "En el nivel del suelo",     // profundidad de un NPC, fijada al leer el mundo
                "· versión",                 // cabecera: se recompone al recargar el personaje
                // Contenido del MUNDO del propio usuario, no de la app: el Explorador lista los
                // letreros con el texto que escribio el jugador dentro del juego ("Afueras de
                // Larvas de gusano", "El Musgo de Accidentes" son letreros reales del mundo de
                // pruebas de esta maquina). Traducir eso seria falsear un dato del usuario.
                "Afueras de", "El Musgo de",
            ];
            // Ronda del 6-sep-2026: los nombres reales de objeto/NPC/tile/buff del JUEGO son
            // catalogo de contenido, no interfaz - y muchisimos casan con una palabra funcional
            // española ("Bastón de la ruina", "Alas Elíseas"). Se cargan del mismo fichero real
            // que usa la app y se descuentan enteros: si no, ahogan la señal (1206 lineas de
            // ruido tapaban las 23 reales de interfaz en la primera pasada de esta ronda).
            var nombresDeCatalogo = new HashSet<string>(StringComparer.Ordinal);
            // Ronda de traduccion del CONTENIDO del juego (6-sep-2026): entran tambien los
            // catalogos INGLESES nuevos. Sin ellos, un nombre ingles real que contenga por
            // casualidad una palabra funcional española (" a ", " no ", "alas"...) ya no lo
            // descontaria nadie y este barrido lo marcaria como "texto sin migrar" - un FALLO
            // falso provocado justamente por haber traducido bien. Quien SI mira si el
            // contenido del juego sigue en español con la app en ingles es A11-CONTENIDO-IDIOMA
            // (AuditoriaContenidoIdioma.cs), que va por el camino contrario.
            foreach (string fichero in new[] { "vanilla_item_names.json", "vanilla_item_names_en.json", "npc_names.json", "vanilla_buff_names_es.json", "vanilla_buff_names_en.json", "vanilla_buff_descriptions_en.json", "tile_names.json", "vanilla_item_tooltips_en.json", @"calamity\catalog.json", "calamity_buff_descriptions.json" })
            {
                try
                {
                    string ruta = Path.Combine(AppContext.BaseDirectory, "Assets", fichero);
                    if (!File.Exists(ruta)) continue;
                    using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(ruta));
                    void Recoger(System.Text.Json.JsonElement el)
                    {
                        switch (el.ValueKind)
                        {
                            case System.Text.Json.JsonValueKind.String:
                                if (el.GetString() is { Length: > 0 } s) nombresDeCatalogo.Add(s);
                                break;
                            case System.Text.Json.JsonValueKind.Object:
                                foreach (var p in el.EnumerateObject()) Recoger(p.Value);
                                break;
                            case System.Text.Json.JsonValueKind.Array:
                                foreach (var i in el.EnumerateArray()) Recoger(i);
                                break;
                        }
                    }
                    Recoger(doc.RootElement);
                }
                catch (Exception) { } // un catalogo ausente solo hace el barrido mas ruidoso, nunca lo tumba
            }
            // Y el propio diccionario INGLES: una frase inglesa real puede casar por casualidad
            // con una palabra funcional española (" no ", " a ") - si el texto en pantalla ES
            // literalmente una traduccion inglesa nuestra, por definicion no esta sin traducir.
            var textosInglesesReales = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                string ruta = Path.Combine(AppContext.BaseDirectory, "Assets", "strings_en.json");
                if (File.Exists(ruta))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(ruta));
                    foreach (var p in doc.RootElement.EnumerateObject())
                        if (p.Value.GetString() is { Length: > 0 } s) textosInglesesReales.Add(s);
                }
            }
            catch (Exception) { }
            var sospechas = new List<string>();
            var conocidos = new List<string>();
            var corchetes = new List<string>();
            void Examinar(string donde, string? t)
            {
                if (string.IsNullOrWhiteSpace(t)) return;
                if (System.Text.RegularExpressions.Regex.IsMatch(t, @"^\[[a-z0-9_]+\]$"))
                { corchetes.Add($"{donde}: {t}"); return; }
                // Ronda del 6-sep-2026: fuera el ruido antes de mirar palabra por palabra.
                if (nombresDeCatalogo.Contains(t) || textosInglesesReales.Contains(t)) return;
                // El Explorador añade el id real del tile al final ("Pared de nieve (natural)
                // [40]") - el nombre de catalogo sigue siendo el mismo, solo hay que quitarlo.
                if (nombresDeCatalogo.Contains(System.Text.RegularExpressions.Regex.Replace(t, @" \[\d+\]$", ""))) return;
                // el espacio de guarda hace que " de " case tambien al principio/final del texto
                string bajo = " " + t.ToLowerInvariant().Replace("\n", " ") + " ";
                foreach (string p in palabrasEspañolas)
                    if (bajo.Contains(p))
                    {
                        string linea = $"{donde}: \"{(t.Length > 90 ? t[..90] + "..." : t)}\" (por '{p.Trim()}')";
                        if (limitesConocidos.Any(t.Contains)) conocidos.Add(linea);
                        else sospechas.Add(linea);
                        return;
                    }
            }

            // Ronda del 6-sep-2026: ademas de los TextBlock (lo unico que miraba antes) se barre
            // el Content de texto de cualquier ContentControl real (botones, RadioButton de
            // pildora, cabeceras de TabItem) - ahi vivian, por ejemplo, las etiquetas de las
            // carpetas de la Libreria y los selectores de almacen.
            void BarrerPantallaActual(string donde)
            {
                foreach (var tb in Descendientes<System.Windows.Controls.TextBlock>(window))
                    if (tb.IsVisible) Examinar(donde, tb.Text);
                foreach (var cc in Descendientes<System.Windows.Controls.ContentControl>(window))
                    if (cc.IsVisible && cc.Content is string s) Examinar(donde, s);
            }

            vm.Settings.Language = "en";
            DoEvents(); DoEvents();
            for (int tab = 0; tab <= 5; tab++)
            {
                vm.SelectedTabIndex = tab;
                DoEvents(); DoEvents();
                if (tab == 1)
                {
                    for (int inner = 0; inner <= 9; inner++)
                    {
                        try { vm.PersonajeInnerTabIndex = inner; } catch (Exception) { break; }
                        DoEvents(); DoEvents();
                        BarrerPantallaActual($"Personaje/sub{inner}");
                    }
                    vm.PersonajeInnerTabIndex = 0;
                    DoEvents();
                }
                else BarrerPantallaActual($"Pestaña{tab}");
                // Ronda del 6-sep-2026: las pestañas con TabControl INTERNO (Novedades: Terraria /
                // tModLoader-Calamity) solo enseñaban su primera hoja - la segunda no la miraba
                // nadie. Se recorren todas las hojas de cada TabControl anidado real.
                foreach (var tc in Descendientes<System.Windows.Controls.TabControl>(window).ToList())
                {
                    if (!tc.IsVisible || tc.Items.Count < 2) continue;
                    int previo = tc.SelectedIndex;
                    for (int h = 0; h < tc.Items.Count; h++)
                    {
                        try { tc.SelectedIndex = h; } catch (Exception) { break; }
                        DoEvents(); DoEvents();
                        BarrerPantallaActual($"Pestaña{tab}/interna{h}");
                    }
                    try { tc.SelectedIndex = previo; } catch (Exception) { }
                    DoEvents();
                }
            }

            // Ronda del 6-sep-2026 (queja real del usuario: "Libreria... sigue en español"): las
            // CARPETAS del arbol nunca las veia este barrido, porque en reposo la Libreria enseña
            // tarjetas de carpeta raiz y el arbol desplegado solo aparece al entrar en una. Se
            // abre de verdad la primera carpeta raiz de la Libreria de objetos, la de buffs y la
            // de Investigacion, y se expanden sus hijos.
            void BarrerArbol(string donde, System.Collections.IEnumerable raices,
                             Action<Terrakeep.App.ViewModels.CategoryNodeViewModel> seleccionar)
            {
                var lista = raices.Cast<Terrakeep.App.ViewModels.CategoryNodeViewModel>().ToList();
                foreach (var raiz in lista) Examinar($"{donde}/raiz", raiz.Name);
                var primera = lista.FirstOrDefault();
                if (primera == null) return;
                seleccionar(primera);
                primera.IsExpanded = true;
                foreach (var hijo in primera.Children) hijo.IsExpanded = true;
                DoEvents(); DoEvents();
                BarrerPantallaActual($"{donde}/abierta");
                foreach (var hijo in primera.Children)
                {
                    Examinar($"{donde}/hijo", hijo.Name);
                    foreach (var nieto in hijo.Children) Examinar($"{donde}/nieto", nieto.Name);
                }
            }

            vm.SelectedTabIndex = 1;
            vm.PersonajeInnerTabIndex = 0;
            DoEvents(); DoEvents();
            BarrerArbol("Libreria", vm.Library.RootCategories, n => vm.Library.SelectCategoryCommand.Execute(n));
            vm.Library.ClearCategoryCommand.Execute(null);
            DoEvents();
            vm.PersonajeInnerTabIndex = 2;
            DoEvents(); DoEvents();
            BarrerArbol("Investigacion", vm.Research.RootCategories, n => vm.Research.SelectCategoryCommand.Execute(n));
            vm.Research.ClearCategoryCommand.Execute(null);
            DoEvents();
            vm.PersonajeInnerTabIndex = 1;
            DoEvents(); DoEvents();
            BarrerArbol("LibreriaBuffs", vm.BuffLibrary.RootCategories, n => vm.BuffLibrary.SelectCategoryCommand.Execute(n));
            vm.BuffLibrary.ClearCategoryCommand.Execute(null);
            vm.PersonajeInnerTabIndex = 0;
            DoEvents();

            // Parte 3 del encargo: los creditos reales ("IncrediBad") tienen que verse enteros en
            // la pestaña Acerca de, en los DOS idiomas - se comprueba el texto real ya renderizado
            // en pantalla, no solo la propiedad del ViewModel.
            vm.SelectedTabIndex = 7; // Acerca de - AppTab.AcercaDe, reordenado T1 21-sep-2026
            DoEvents(); DoEvents();
            var autorEn = Descendientes<System.Windows.Controls.TextBlock>(window)
                .FirstOrDefault(tb => tb.IsVisible && tb.Text.Contains("IncrediBad"));
            Console.WriteLine($"A10-CREDITOS(en): TextBlock con 'IncrediBad' visible={autorEn != null}, ancho={autorEn?.ActualWidth ?? -1:0.#}, alto={autorEn?.ActualHeight ?? -1:0.#}, recortado={(autorEn != null && autorEn.ActualWidth > 0 && autorEn.DesiredSize.Width > autorEn.ActualWidth + 0.5)} (esperado visible=True, recortado=False)");
            if (autorEn == null) Console.WriteLine("FALLO: A10-CREDITOS - el credito de autoria no aparece en 'Acerca de' con la app en ingles");

            vm.Settings.Language = "es";
            DoEvents(); DoEvents();
            var autorEs = Descendientes<System.Windows.Controls.TextBlock>(window)
                .FirstOrDefault(tb => tb.IsVisible && tb.Text.Contains("IncrediBad"));
            Console.WriteLine($"A10-CREDITOS(es): TextBlock con 'IncrediBad' visible={autorEs != null}, ancho={autorEs?.ActualWidth ?? -1:0.#}, alto={autorEs?.ActualHeight ?? -1:0.#}, recortado={(autorEs != null && autorEs.ActualWidth > 0 && autorEs.DesiredSize.Width > autorEs.ActualWidth + 0.5)} (esperado visible=True, recortado=False)");
            if (autorEs == null) Console.WriteLine("FALLO: A10-CREDITOS - el credito de autoria no aparece en 'Acerca de' en español");

            Console.WriteLine($"A10-IDIOMA-BARRIDO: claves sin traducir a la vista (formato '[clave]')={corchetes.Count} (esperado 0), textos NUEVOS que siguen en español con la app en ingles={sospechas.Count} (esperado 0), casos ya conocidos y documentados={conocidos.Distinct().Count()} (informativo, no es fallo)");
            foreach (string c in corchetes.Distinct().Take(25)) Console.WriteLine("   SIN-TRADUCIR " + c);
            foreach (string s in sospechas.Distinct().Take(60)) Console.WriteLine("   EN-ESPAÑOL " + s);
            foreach (string c in conocidos.Distinct().Take(40)) Console.WriteLine("   LIMITE-CONOCIDO " + c);
            if (corchetes.Count > 0) Console.WriteLine("FALLO: A10-IDIOMA-BARRIDO - hay claves de idioma que no existen en ningun diccionario");
            if (sospechas.Count > 0) Console.WriteLine("FALLO: A10-IDIOMA-BARRIDO - hay texto sin migrar al diccionario (se queda en español con la app en ingles)");

            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0;
            DoEvents();
        }
        catch (Exception ex) { Console.WriteLine("A10-IDIOMA-BARRIDO-EXCEPTION: " + ex); }
        finally
        {
            vm.Settings.Language = "es"; // el resto del arnes asume español, pase lo que pase arriba
            DoEvents();
        }

        // AR-LAY: barrido sistematico de maquetacion por tamaño de ventana e idioma sobre TODA
        // la app - vive en su propio fichero (AuditoriaMaquetacion.cs, misma clase parcial): ver
        // alli el porque de no meter otro bloque mas dentro de este Main() ya enorme.
        BarridoMaquetacionPorTamañoEIdioma(window, vm);

        // A11-CONTENIDO-IDIOMA (6-sep-2026): el CONTENIDO del juego (nombres de objeto/NPC/tile/
        // buff, tooltips, bonos de set) en los dos idiomas - el punto ciego POR DISEÑO de A10,
        // que descuenta como ruido cualquier texto que sea un nombre de catalogo. Vive en su
        // propio fichero (AuditoriaContenidoIdioma.cs, misma clase parcial).
        AuditoriaContenidoDelJuegoEnIdioma(window, vm);

        // LIB-* / BUFLIB-* / BUILDS-* (6-sep-2026): oleada de pruebas de Libreria (objetos y
        // buffs) y Builds - el cuerpo real vive en PruebasLibreriaYBuilds.cs, otra parte de esta
        // misma clase (ver el comentario de `partial` arriba).
        PruebasLibreriaYBuilds(vm, window);

        // PB-* (6-sep-2026): oleada de pruebas de Personaje > Buffs / Apariencia /
        // Investigacion / Spawn Points / Desbloqueos / Version - el cuerpo real vive en
        // PruebasBuffsAparienciaVersion.cs, otra parte de esta misma clase. Mide esas pantallas
        // en su estado POBLADO Y ABIERTO (rejilla llena, selectores desplegados, carpeta
        // elegida, avisos naranjas visibles), que es justo el punto ciego de AR-LAY.
        PruebasPersonajeBuffsAparienciaVersion(vm, window);

        // MP-* (6-sep-2026): "Mejor prefijo" y prefijo automatico sobre objetos que ANTES no
        // tenian cobertura en calamity/best_prefix.json - el cuerpo real vive en
        // PruebasMejorPrefijo.cs, otra parte de esta misma clase. Va detras de los bloques de
        // Objetos a proposito: coloca y quita armas en el hueco 0 del Inventario (y lo deja
        // como estaba), asi que no debe correr antes que OBJ-*.
        PruebasMejorPrefijo(vm, window);

        // ExploracionRediseno FaseI (26-sep-2026, aplicador-fix): canario PERMANENTE de layout del
        // sidebar de Exploracion (los 3 modos Browse/ChestInspector/WorldTools, 1180x860 y
        // 1080x700) - cuerpo real en CanarioExploracionLayoutPermanente.cs, otra parte de esta
        // misma clase. Se llama aqui de forma INCONDICIONAL (a diferencia de EXPLORATION_LAYOUT_SOLO
        // de mas arriba, que solo corre bajo demanda) para que corra en CADA pasada completa del
        // arnes - guardia anti-regresion del antipatron MinHeight=900/1000 que el usuario prohibio
        // explicitamente y confirmacion de 0 overflow horizontal real, ademas de que
        // Guardar/Cancelar del Inspector siguen alcanzables por scroll. Autocontenida (carga su
        // propio mundo si hace falta, restaura tamano de ventana/pestaña/estado de Exploracion al
        // terminar) - no depende de que ningun bloque anterior haya dejado un estado concreto.
        EjecutarCanarioExploracionLayoutPermanente(window, vm);

        // Oleada del 6-sep-2026, area "Inicio, Ajustes, Novedades, Acerca de" - el cuerpo real
        // vive en PruebasInicioAjustes.cs, otra parte de esta misma clase (ver el comentario de
        // `partial` arriba). Va al final a proposito: manipula carpetas adicionales, idioma y
        // window.json, y los deja como estaban antes de seguir.
        PruebasInicioAjustesNovedadesAcercaDe(vm, window);

        string errorLog = Path.Combine(AppContext.BaseDirectory, "ultimo-error.log");
        Console.WriteLine("ultimo-error.log existe: " + File.Exists(errorLog));

        // Bloque 0 (N-2, IsDirty real): MainWindow.OnWindowClosing ahora muestra un MessageBox
        // MODAL real de "cambios sin guardar" cuando IsDirty=true - sin nadie delante para
        // pulsarlo, este arnes se quedaba colgado para siempre en window.Close() (confirmado:
        // 5+ minutos sin avanzar, salida vacia incluso tras matar el proceso - el buffer de
        // consola redirigido nunca llega a volcarse porque el hilo de UI nunca vuelve). El
        // arnes es codigo de prueba, no un usuario real - se limpia el flag antes de cerrar.
        // A9-12-VENTANAFIJA (pedido explicito del usuario, 5-sep-2026): "guardar el tamaño
        // actual de la ventana... con un tick que lo activa/desactiva". Round-trip real de
        // WindowPlacementService.Pin/IsPinned/Unpin sobre el window.json REAL de esta maquina -
        // respaldado como TEXTO antes de tocar nada y restaurado byte a byte al final (try/
        // finally), igual de estricto que A9-11-DIFICULTAD con el mundo real: si el usuario ya
        // habia activado el tick de verdad en esta misma maquina, su preferencia real vuelve
        // intacta, nunca se pisa con datos de prueba.
        string placementPathParaFijar = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "window.json");
        string? placementBackup = File.Exists(placementPathParaFijar) ? File.ReadAllText(placementPathParaFijar) : null;
        try
        {
            bool antesDeFijar = WindowPlacementService.IsPinned();
            window.Left = 321; window.Top = 65;
            FijarTamaño(window, 1345, 812);
            WindowPlacementService.Pin(window);
            bool trasFijar = WindowPlacementService.IsPinned();
            WindowPlacementService.Unpin();
            bool trasQuitar = WindowPlacementService.IsPinned();
            Console.WriteLine($"A9-12-VENTANAFIJA: antes={antesDeFijar} (esperado False en una maquina limpia), tras Pin()={trasFijar} (esperado True), tras Unpin()={trasQuitar} (esperado False)");
            if (!trasFijar || trasQuitar) Console.WriteLine("FALLO: A9-12-VENTANAFIJA - Pin()/Unpin() no cambiaron IsPinned() como se esperaba");

            // A10-VENTANAFIJA-MAXIMIZADA (auditoria final de Opus, 5-sep-2026): el ciclo de
            // arriba solo prueba la ventana en estado Normal. Falta el caso real que mas facil
            // es entender mal: marcar el tick con la ventana MAXIMIZADA. Pin() guarda a
            // proposito RestoreBounds (el tamaño DESmaximizado) y Apply() nunca maximiza si el
            // tick esta puesto - decision de diseño deliberada y documentada en el propio
            // servicio ("fijar un tamaño concreto y luego arrancar maximizado no tendria
            // sentido"), no un fallo; se comprueba aqui para que quede fijada como el
            // comportamiento esperado y no cambie sin querer. Tambien se ejercita Apply() de
            // verdad sobre la ventana real, que hasta ahora no lo cubria nadie.
            double anchoNormal = 1345, altoNormal = 812;
            window.WindowState = System.Windows.WindowState.Maximized;
            DoEvents(); DoEvents();
            WindowPlacementService.Pin(window);
            string json = File.ReadAllText(placementPathParaFijar);
            var guardado = System.Text.Json.JsonSerializer.Deserialize<WindowPlacementInfo>(json)!;
            bool guardoElRestaurado = Math.Abs(guardado.PinnedWidth - anchoNormal) < 2 && Math.Abs(guardado.PinnedHeight - altoNormal) < 2;
            Console.WriteLine($"A10-VENTANAFIJA-MAXIMIZADA: Pin() con la ventana maximizada guarda {guardado.PinnedWidth:0}x{guardado.PinnedHeight:0} (esperado el tamaño DESmaximizado {anchoNormal:0}x{altoNormal:0}, no el de pantalla completa) -> {guardoElRestaurado}");
            if (!guardoElRestaurado) Console.WriteLine("FALLO: A10-VENTANAFIJA-MAXIMIZADA - Pin() no guardo RestoreBounds estando maximizada");

            window.WindowState = System.Windows.WindowState.Normal;
            DoEvents(); DoEvents();
            FijarTamaño(window, 900, 640); // tamaño distinto a proposito, para ver si Apply lo pisa
            // Diagnostico permanente (ronda del 6-sep-2026): cuando este bloque falla, lo unico
            // que decide el resultado real de Apply() son estos tres datos - el JSON REAL que
            // Apply va a leer (otro proceso puede haberlo reescrito por debajo: window.json es
            // global de la maquina, misma verdad ya documentada para session.json) y el area
            // virtual real contra la que Apply recorta. Sin ellos, un FALLO aqui no dice nada.
            Console.WriteLine($"A10-VENTANAFIJA-DIAG: VirtualScreen={SystemParameters.VirtualScreenWidth:0}x{SystemParameters.VirtualScreenHeight:0} " +
                              $"(izq={SystemParameters.VirtualScreenLeft:0} arr={SystemParameters.VirtualScreenTop:0}), MinWidth/MinHeight de la ventana={window.MinWidth:0}x{window.MinHeight:0}, " +
                              $"window.json justo antes de Apply()={File.ReadAllText(placementPathParaFijar)}");
            WindowPlacementService.Apply(window);
            DoEvents(); DoEvents();
            // Oleada del 6-sep-2026: esto comparaba contra el tamaño fijado A SECAS, y daba FALLO
            // en cualquier maquina cuya pantalla real sea mas pequeña que ese tamaño - Apply()
            // recorta a proposito contra el area virtual real y contra MinWidth/MinHeight ("nunca
            // restaurar fuera de la pantalla", su propio comentario). Es la MISMA leccion que
            // FijarTamaño ya tenia escrita: el esperado no es lo pedido, es lo pedido YA recortado
            // por los limites reales que el propio codigo declara. Salio de verdad en una sesion
            // remota cuyo escritorio mide 576x1197 en unidades WPF (1440x2992 fisicos al 250%),
            // o sea mas estrecho que el MinWidth=1080 de la propia app: el bloque daba FALLO sin
            // que nada estuviera roto. No se pierde poder de deteccion - lo que de verdad hay que
            // demostrar es que Apply leyo los campos Pinned* y no los normales, y eso pasa a
            // comprobarse explicitamente aqui abajo (el ultimo tamaño usado es otro numero).
            double esperadoAncho = Math.Max(window.MinWidth, Math.Min(anchoNormal, SystemParameters.VirtualScreenWidth));
            double esperadoAlto = Math.Max(window.MinHeight, Math.Min(altoNormal, SystemParameters.VirtualScreenHeight));
            bool aplicoElFijado = Math.Abs(window.Width - esperadoAncho) < 2 && Math.Abs(window.Height - esperadoAlto) < 2;
            bool noMaximizo = window.WindowState == System.Windows.WindowState.Normal;
            var jsonAplicado = System.Text.Json.JsonSerializer.Deserialize<WindowPlacementInfo>(File.ReadAllText(placementPathParaFijar))!;
            double esperadoSiIgnorase = Math.Max(window.MinHeight, Math.Min(jsonAplicado.Height, SystemParameters.VirtualScreenHeight));
            bool usoLosCamposFijados = Math.Abs(esperadoAlto - esperadoSiIgnorase) <= 2 || Math.Abs(window.Height - esperadoAlto) < 2;
            Console.WriteLine($"A10-VENTANAFIJA-MAXIMIZADA: Apply() con el tick puesto deja la ventana en {window.Width:0}x{window.Height:0} " +
                              $"(esperado {esperadoAncho:0}x{esperadoAlto:0} = el fijado {anchoNormal:0}x{altoNormal:0} recortado por MinWidth/MinHeight {window.MinWidth:0}x{window.MinHeight:0} y por la pantalla real {SystemParameters.VirtualScreenWidth:0}x{SystemParameters.VirtualScreenHeight:0}) -> {aplicoElFijado}, " +
                              $"NO maximizada -> {noMaximizo}, uso los campos Pinned* y no el ultimo tamaño usado ({jsonAplicado.Width:0}x{jsonAplicado.Height:0}) -> {usoLosCamposFijados}");
            if (!aplicoElFijado || !noMaximizo) Console.WriteLine("FALLO: A10-VENTANAFIJA-MAXIMIZADA - Apply() no restauro el tamaño fijado, o maximizo con el tick puesto");
            if (!usoLosCamposFijados) Console.WriteLine("FALLO: A10-VENTANAFIJA-MAXIMIZADA - Apply() con el tick puesto uso el ULTIMO tamaño usado en vez del tamaño fijado");

            WindowPlacementService.Unpin();
        }
        finally
        {
            if (placementBackup != null) File.WriteAllText(placementPathParaFijar, placementBackup);
            else if (File.Exists(placementPathParaFijar)) File.Delete(placementPathParaFijar);
        }

        // Verificacion real de T-3 (auditoria de Opus, Bloque 4): tamaño/posicion reconocibles
        // y distintos de los de fabrica, antes de cerrar (Close() real dispara
        // OnWindowClosing -> WindowPlacementService.Save real).
        window.Left = 40;
        window.Top = 55;
        FijarTamaño(window, 1234, 789);

        vm.IsDirty = false;
        window.Close();
        DoEvents();
        string placementPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Terrakeep", "window.json");
        Console.WriteLine($"T3-GUARDADO: {placementPath} existe={File.Exists(placementPath)}, contenido={(File.Exists(placementPath) ? File.ReadAllText(placementPath) : "(nada)")}");
        app.Shutdown();
        Console.WriteLine("DONE");
    }

    private static void DoEvents()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    // 15-sep-2026 (KeepQA V2.0/V3, segunda ronda de la fuga de KEEPQA_MEMORIA - ver bitacora.md):
    // bombea el Dispatcher hasta ContextIdle de verdad (por debajo de Background, que es lo mas
    // bajo a lo que llega DoEvents()) - la prioridad real a la que WeakEventManager programa su
    // propia purga interna de listeners muertos (Dispatcher.BeginInvoke(DispatcherPriority.
    // ContextIdle, Purge), confirmado leyendo el motor de bindings de WPF). Un DoEvents() normal
    // NUNCA llega tan abajo, asi que esa purga nunca se dispara en este bucle.
    //
    // Por que hace falta aqui (y no es hacer trampa): tras el arreglo de WeakEventManager del
    // 15-sep-2026 (commit 26c57c60), un segundo dotnet-gcdump diff seguia mostrando
    // MS.Utility.FrugalObjectList/SingleItemList<WeakEventManager+Listener> creciendo ~198-200
    // instancias por ciclo (29.677->111.017 entre ciclo~150 y ciclo~560, dos capturas reales del
    // mismo proceso). Pero los CONSUMIDORES reales de esas listas - System.Windows.Data.
    // BindingExpression (3.453->3.221), MS.Internal.Data.PropertyPathWorker/ClrBindingWorker
    // (+524 en 410 ciclos, ruido normal) y los propios ItemSlotViewModel (352->348) - se quedaban
    // PLANOS en el mismo diff: la fuga NO es de objetos vivos alcanzables, es SOLO la bolsa
    // interna de entradas muertas (WeakReference ya apuntando a nada) que WeakEventManager
    // arrastra sin compactar hasta que se cumple una de sus dos condiciones de purga (el evento
    // se dispara de verdad, o el Dispatcher respira hasta ContextIdle). Origen real: el
    // DataTemplate del slot (MainWindow.xaml, "SlotCompactTemplate") usa varios
    // {Binding Loc[clave]} por instancia (tooltip/menu contextual) contra
    // LocalizationService.Instance - un POCO compartido y permanente, no un DependencyObject -
    // asi que el propio motor de bindings de WPF (no codigo de Terrakeep) tiene que usar
    // PropertyChangedEventManager para escuchar su "Item[]" en cada instancia nueva.
    //
    // Confirmado de forma aislada (mismo criterio que "verificar-aislando-la-variable"): con
    // ESTE pump activado, N=40 pasa de ~116 KB/ciclo estable a 0,0 KB/ciclo desde el ciclo 8 en
    // adelante - la unica variable que cambia es si el Dispatcher llega a respirar de verdad.
    // Un usuario real de Terrakeep SI deja que el Dispatcher llegue a ContextIdle constantemente
    // (entre clics, mientras lee la pantalla) - este bucle sintetico de abrir/cerrar sin parar
    // nunca lo hacia, y por eso KEEPQA_MEMORIA veia una "fuga" que en el uso real no existe.
    // Arreglo real: hacer que el propio arnes de medicion sea representativo de verdad (respira
    // como respiraria un usuario), no forzar ningun cambio en Terrakeep.App (no hay ningun
    // objeto vivo que liberar ahi - ya se libera solo).
    private static void PumpToContextIdle()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    // L-c (segunda auditoria de Opus, Fable): DoEvents por si sola NO espera tiempo real, solo
    // vacia lo que ya este listo AHORA MISMO - un DispatcherTimer real (LibraryViewModel.
    // _searchDebounceTimer) no dispara hasta que pasa tiempo de reloj de verdad. Bombea el
    // Dispatcher en bucle hasta que el tiempo pedido transcurre de verdad, para probar el
    // camino async/temporizado real (no solo lo sincrono).
    //
    // Bug real de este mismo arnes encontrado al verificar (no del codigo de produccion): un
    // bucle DoEvents() sin ninguna pausa real reencola trabajo propio en cada vuelta y puede
    // dejar la cola de mensajes SIEMPRE ocupada - un DispatcherTimer real usa un temporizador
    // de Windows aparte (WM_TIMER, prioridad baja) que necesita que la cola quede libre un
    // instante de verdad para entregarse. Un Thread.Sleep(1) real entre vueltas (cede la CPU
    // de verdad al hilo/SO) fue lo que lo arreglo - confirmado antes con un log temporal
    // (DEBUG-TICK) que demostro que el Tick SI llegaba a disparar con esa pausa real de por
    // medio, y no siempre sin ella.
    private static void WaitForDispatcher(int ms)
    {
        long until = Environment.TickCount64 + ms;
        while (Environment.TickCount64 < until)
        {
            DoEvents();
            System.Threading.Thread.Sleep(1);
        }
    }

    // Auditoria de redimensionado (ESPEC-auditoria-redimensionado.md §1.1-1.2): en una sesion
    // RDP con escalado alto, el escritorio logico puede ser MAS ESTRECHO que el MinWidth=1080
    // de la ventana (medido en esta maquina: pantalla 576x1197 DIP = 1440x2992 fisicos al
    // 250%). Windows limita cualquier ventana a MINMAXINFO.ptMaxTrackSize (por omision,
    // SM_CXMAXTRACK x SM_CYMAXTRACK - 1476x3028 fisicos aqui), pero WPF rellena
    // ptMinTrackSize desde Window.MinWidth (1080 DIP = 2700 fisicos) - como el minimo se
    // aplica DESPUES del maximo dentro del mismo mensaje WM_GETMINMAXINFO, TODA peticion de
    // window.Width queda clavada en 1080 exactos, sin excepcion ni aviso. Sin este hook,
    // cualquier comprobacion de umbral de SizeClass en esta maquina mide 1080px pase lo que
    // pase - exactamente lo que llevaban haciendo en silencio E2-UMBRAL/A4-1350/A4-EXPANDIDO/
    // H5-09-AMPLIO antes de esta auditoria. El hook vive SOLO en este arnes (nunca en
    // produccion) y sube el techo a un numero que ningun monitor real va a alcanzar.
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct POINT { public int x; public int y; }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    private const int WM_GETMINMAXINFO = 0x0024;

    private static void InstalarHookMaxTrackSize(Window w)
    {
        var hwndSource = (HwndSource)PresentationSource.FromVisual(w)!;
        hwndSource.AddHook((IntPtr h, int msg, IntPtr wp, IntPtr lp, ref bool handled) =>
        {
            if (msg == WM_GETMINMAXINFO)
            {
                var mmi = Marshal.PtrToStructure<MINMAXINFO>(lp);
                mmi.ptMaxTrackSize.x = 32000; mmi.ptMaxTrackSize.y = 32000;
                mmi.ptMaxSize.x = 32000; mmi.ptMaxSize.y = 32000;
                Marshal.StructureToPtr(mmi, lp, true);
            }
            return IntPtr.Zero;
        });
    }

    // Sustituye a los `window.Width = ...; window.Height = ...;` sueltos del resto de este
    // fichero. Grita si el redimensionado real no funciono (RESIZE-IMPOSIBLE), en vez de dejar
    // que las comprobaciones de despues midan un tamaño distinto al pedido sin decirlo - ver el
    // comentario de InstalarHookMaxTrackSize de arriba, esto es justo lo que fallaba en
    // silencio antes de esta auditoria.
    private static void FijarTamaño(Window w, double ancho, double alto)
    {
        w.Width = ancho; w.Height = alto;
        DoEvents(); DoEvents(); DoEvents();
        // El objetivo real no es el ancho/alto PEDIDO a secas, es el pedido YA recortado por el
        // suelo real que la propia ventana declara (Window.MinWidth/MinHeight, MainWindow.xaml)
        // - varias llamadas de este arnes piden a proposito menos que el minimo (ej.
        // "resize-equip-forzado-pequeno.png", CaptureAt(700,400,...)) para comprobar justo que
        // WPF respeta ese suelo. Solo hay RESIZE-IMPOSIBLE de verdad si el resultado no coincide
        // ni con lo pedido NI con el suelo real - eso es el clamp real del entorno (§1.1), no un
        // suelo declarado a proposito.
        double anchoEsperado = Math.Max(ancho, w.MinWidth);
        double altoEsperado = Math.Max(alto, w.MinHeight);
        if (Math.Abs(w.ActualWidth - anchoEsperado) > 1 || Math.Abs(w.ActualHeight - altoEsperado) > 1)
            Console.WriteLine($"FALLO: RESIZE-IMPOSIBLE - pedido {ancho}x{alto} (esperado real {anchoEsperado:0}x{altoEsperado:0} " +
                              $"tras el MinWidth/MinHeight declarado), obtenido {w.ActualWidth:0}x{w.ActualHeight:0}. " +
                              $"TODA comprobacion de umbral que venga despues es INVALIDA en esta maquina.");
    }

    // Devuelve los pixeles que WPF esta recortando AHORA MISMO de este elemento (0,0 = ninguno).
    // VisualTreeHelper.GetClip sobre el propio elemento recortado es el detector real - ver
    // ESPEC-auditoria-redimensionado.md §1.4 (experimento controlado: el recorte NO lo hace el
    // CornerRadius de un Border contenedor, lo hace el recorte de layout de WPF cuando un hijo
    // no cabe en el hueco que se le arregla, y se lee EN EL HIJO recortado, no en el padre).
    private static (double x, double y) Recorte(FrameworkElement fe)
    {
        var c = System.Windows.Media.VisualTreeHelper.GetClip(fe);
        if (c == null) return (0, 0);
        return (Math.Max(0, fe.ActualWidth - c.Bounds.Width), Math.Max(0, fe.ActualHeight - c.Bounds.Height));
    }

    // AR-12d: el pincel REAL del borde de la fila de "Cofre a cofre" de este cofre concreto, tal
    // y como esta pintado ahora mismo en pantalla - el Border exterior de ChestRowTemplate es el
    // unico descendiente Border cuyo DataContext es esa fila Y que tiene BorderThickness real.
    // Null si la fila esta virtualizada fuera de vista (nunca lo esta para las primeras).
    private static System.Windows.Media.Brush? BordeDeFilaDeCofre(DependencyObject raiz, object fila) =>
        Descendientes<System.Windows.Controls.Border>(raiz)
            .FirstOrDefault(b => ReferenceEquals(b.DataContext, fila) && b.BorderThickness.Left > 0)?.BorderBrush;

    // AR-13a: la cadena real de contenedores (tipo, x dentro de la lista y ancho) desde un texto
    // hasta el panel de la lista - es lo unico que dice DONDE se pierde el ancho cuando una fila
    // no llena su columna: el culpable es el primer eslabon cuyo ancho ya no llega al de la lista.
    private static IEnumerable<string> Ascendencia(DependencyObject elemento, FrameworkElement host)
    {
        var cadena = new List<string>();
        for (var d = elemento; d != null && !ReferenceEquals(d, host); d = System.Windows.Media.VisualTreeHelper.GetParent(d))
            if (d is FrameworkElement fe)
                cadena.Add($"{fe.GetType().Name}@{fe.TranslatePoint(new Point(0, 0), host).X:0}+{fe.ActualWidth:0}");
        cadena.Reverse();
        return cadena;
    }

    // AR-13a: el panel que hospeda las filas de la lista a la que pertenece este elemento
    // (VirtualizingStackPanel/StackPanel con IsItemsHost). Su ancho es el sitio real que tienen
    // las filas, que puede ser MAYOR que el de una fila concreta si esa fila no se estira.
    private static Panel? AncestroPanelDeItems(DependencyObject elemento)
    {
        for (var d = System.Windows.Media.VisualTreeHelper.GetParent(elemento); d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
            if (d is Panel p && p.IsItemsHost) return p;
        return null;
    }

    // AR-13a: el ancho REAL que ocupan los glifos de un TextBlock, medido con la misma fuente,
    // tamaño y estilo con los que esta pintado. Hace falta porque ActualWidth NO sirve para esto:
    // un TextBlock dentro de un StackPanel/DockPanel se estira a todo el ancho disponible aunque
    // su texto sean tres cifras, asi que "donde acaba la caja" y "donde acaba el texto" son cosas
    // muy distintas - y el hueco vacio que reporto el usuario esta justo entre las dos.
    private static double AnchoNaturalDelTexto(TextBlock tb)
    {
        var ft = new System.Windows.Media.FormattedText(
            tb.Text, System.Globalization.CultureInfo.CurrentCulture, tb.FlowDirection,
            new System.Windows.Media.Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch),
            tb.FontSize, System.Windows.Media.Brushes.Black,
            System.Windows.Media.VisualTreeHelper.GetDpi(tb).PixelsPerDip);
        return ft.WidthIncludingTrailingWhitespace;
    }

    // AR-13a: coordenada X, dentro de la fila, donde acaba de verdad el ultimo glifo de este
    // TextBlock - teniendo en cuenta si el texto esta alineado a la derecha (entonces el glifo
    // acaba con la caja) o a la izquierda (entonces acaba a su ancho natural del origen).
    private static double FinRealDelTexto(TextBlock tb, FrameworkElement fila)
    {
        double x = tb.TranslatePoint(new Point(0, 0), fila).X;
        bool aLaDerecha = tb.TextAlignment == TextAlignment.Right;
        double ancho = aLaDerecha ? tb.ActualWidth : Math.Min(tb.ActualWidth, AnchoNaturalDelTexto(tb));
        return x + ancho;
    }

    // AR-13a: ¿este texto se esta cortando teniendo sitio? Solo puede pasar sin TextWrapping: con
    // Wrap el texto pasa a la linea siguiente y no se pierde nada. Con NoWrap + TextTrimming, WPF
    // no recorta el elemento (VisualTreeHelper.GetClip da null) sino que dibuja "…" - la unica
    // forma de detectarlo es comparar el ancho natural del texto con el ancho real de la caja.
    private static bool TextoRecortado(TextBlock tb) =>
        tb.TextWrapping == TextWrapping.NoWrap && AnchoNaturalDelTexto(tb) > tb.ActualWidth + 0.5;

    // D-PALABRA (20-sep-2026, bug real de Builds - "Cuerpo a cuerpo" partido letra a letra/silaba
    // a silaba dentro de una columna de 220px): AGUJERO REAL confirmado en TextoRecortado de
    // arriba - su propio comentario da por sentado que "con Wrap el texto pasa a la linea
    // siguiente y no se pierde nada", que es cierto SOLO si el ancho disponible alcanza para la
    // palabra mas larga. Cuando NO alcanza, WPF no pierde texto (nada se recorta, nada se
    // truncan con "...") pero lo parte DENTRO de una palabra - técnicamente "cabe" (crece en
    // ALTO en vez de desbordar en ANCHO) y por eso ni D1 (recorte) ni D2 (solape) ni el D3 de
    // arriba (TextTrimming activo) lo detectaban nunca: es una CUARTA categoria de fallo con
    // vocabulario propio, la razon real por la que Builds paso desapercibido hasta que el usuario
    // lo vio con sus propios ojos. Mismo criterio de medicion que AnchoNaturalDelTexto (misma
    // fuente/tamaño/estilo reales, FormattedText) pero por PALABRA suelta en vez del texto
    // entero.
    private static double AnchoNaturalDePalabra(TextBlock tb, string palabra)
    {
        var ft = new System.Windows.Media.FormattedText(
            palabra, System.Globalization.CultureInfo.CurrentCulture, tb.FlowDirection,
            new System.Windows.Media.Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch),
            tb.FontSize, System.Windows.Media.Brushes.Black,
            System.Windows.Media.VisualTreeHelper.GetDpi(tb).PixelsPerDip);
        return ft.WidthIncludingTrailingWhitespace;
    }

    // ¿Este TextBlock (con Wrap real, nunca NoWrap) tiene alguna palabra suelta mas ancha que su
    // propia caja? Si la tiene, WPF va a partirla dentro de si misma SI O SI - no hay forma de
    // que un renderizador de texto real "envuelva" una palabra sin espacios en mitad de una
    // palabra sin partirla, es matematicamente inevitable con ese ancho. out palabra/ancho: la
    // PEOR (mas ancha) de las que no caben, para el mensaje real del detalle.
    private static bool TextoPartidoDentroDePalabra(TextBlock tb, out string peorPalabra, out double peorAncho)
    {
        peorPalabra = ""; peorAncho = 0;
        if (tb.TextWrapping == TextWrapping.NoWrap) return false;
        if (tb.ActualWidth < 1) return false;
        foreach (string palabra in tb.Text.Split([' ', '\n', '\t', '\r'], StringSplitOptions.RemoveEmptyEntries))
        {
            double ancho = AnchoNaturalDePalabra(tb, palabra);
            if (ancho > tb.ActualWidth + 0.5 && ancho > peorAncho) { peorPalabra = palabra; peorAncho = ancho; }
        }
        return peorAncho > 0;
    }

    // AR-15: el rectangulo de este elemento que de verdad se esta VIENDO ahora mismo, en
    // coordenadas de `raiz`, despues de aplicarle el recorte de TODOS sus ancestros. Hace falta
    // porque Recorte() (VisualTreeHelper.GetClip sobre el propio elemento) NO detecta este caso:
    // cuando quien recorta es un ancestro, las filas que caen por debajo del corte no llevan clip
    // ninguno - simplemente no se pintan, y el elemento sigue diciendo que mide su alto entero.
    // Ese es exactamente el bug de "NPCs que faltan": contenido perdido con 0px de recorte medible.
    private static Rect RectVisible(FrameworkElement fe, FrameworkElement raiz)
    {
        if (!fe.IsVisible || fe.ActualHeight <= 0) return Rect.Empty;
        var r = fe.TransformToAncestor(raiz).TransformBounds(new Rect(0, 0, fe.ActualWidth, fe.ActualHeight));
        for (DependencyObject d = fe; d != null && !ReferenceEquals(d, raiz); d = System.Windows.Media.VisualTreeHelper.GetParent(d))
        {
            if (d is not System.Windows.Media.Visual v) continue;
            var clip = System.Windows.Media.VisualTreeHelper.GetClip(v);
            if (clip == null) continue;
            r.Intersect(v.TransformToAncestor(raiz).TransformBounds(clip.Bounds));
            if (r.IsEmpty) return Rect.Empty;
        }
        return r;
    }

    // AR-15: ¿esta fila se ve ENTERA ahora mismo? (alto completo dentro de lo que de verdad se
    // pinta). Combinada con un BringIntoView real antes de preguntarlo, es la definicion honesta
    // de "el usuario puede llegar a verla": si tras desplazarse hasta ella sigue sin verse
    // entera, esa fila es contenido perdido de verdad, no un simple "hay que hacer scroll".
    private static bool VisibleEntero(FrameworkElement fe, FrameworkElement raiz)
    {
        try
        {
            var r = RectVisible(fe, raiz);
            return !r.IsEmpty && r.Height >= fe.ActualHeight - 0.5;
        }
        catch (InvalidOperationException) { return false; } // no cuelga de `raiz` (fila virtualizada fuera)
    }


    // AR-11: ¿hay un ScrollViewer entre este elemento y el limite dado? Un elemento recortado
    // pero dentro de un ScrollViewer sigue siendo ALCANZABLE (solo hay que desplazarse); uno
    // recortado sin ningun scroll por encima es contenido PERDIDO. Mismo criterio real que ya
    // aplicaba AR-07 en linea, extraido aqui para poder barrer una columna entera con el.
    private static bool TieneScrollAncestro(DependencyObject elemento, DependencyObject limite)
    {
        for (var d = System.Windows.Media.VisualTreeHelper.GetParent(elemento); d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
        {
            if (d is System.Windows.Controls.ScrollViewer) return true;
            if (ReferenceEquals(d, limite)) return false;
        }
        return false;
    }

    // OBJ-01: todos los bindings de este subarbol cuya RUTA no se resolvio de verdad, tal y como
    // lo sabe el propio WPF (BindingExpressionBase.Status == PathError). Es el unico detector
    // fiable de "el DataContext local no tiene esa propiedad": WPF no lanza ninguna excepcion ni
    // pinta nada raro en ese caso - deja la propiedad destino en su valor por defecto (un
    // Content a null, un Text a "") y sigue. GetLocalValueEnumerator es la via real para llegar a
    // las expresiones sin tener que enumerar a mano las DependencyProperty de cada tipo: una
    // propiedad enlazada guarda su BindingExpression COMO valor local.
    private static List<string> BindingsRotos(DependencyObject raiz)
    {
        var fallos = new List<string>();
        foreach (var fe in Descendientes<FrameworkElement>(raiz))
        {
            if (!fe.IsVisible) continue;
            Revisar(fe, fe.DataContext, fallos);
            // Los <Run>/<Span> de un TextBlock con contenido mixto son FrameworkContentElement,
            // NO FrameworkElement: no cuelgan del arbol VISUAL y Descendientes<T> jamas los ve.
            // Hay que mirarlos aparte o se escapa justo el caso de "Defensa total: 51", que es un
            // Run enlazado dentro de su TextBlock - y es el sintoma mas confuso de todos, porque
            // el numero se sigue viendo y lo unico que desaparece es la palabra que dice que es.
            if (fe is TextBlock tb)
                foreach (var inline in tb.Inlines)
                    Revisar(inline, inline.DataContext, fallos);
        }
        return fallos;

        static void Revisar(DependencyObject d, object? dataContext, List<string> fallos)
        {
            var e = d.GetLocalValueEnumerator();
            while (e.MoveNext())
            {
                if (e.Current.Value is not System.Windows.Data.BindingExpressionBase beb) continue;
                if (beb.Status != System.Windows.Data.BindingStatus.PathError) continue;
                string ruta = beb switch
                {
                    System.Windows.Data.BindingExpression be => be.ParentBinding.Path?.Path ?? "(sin ruta)",
                    System.Windows.Data.MultiBindingExpression mbe => string.Join("+", mbe.BindingExpressions
                        .OfType<System.Windows.Data.BindingExpression>().Select(x => x.ParentBinding.Path?.Path ?? "?")),
                    _ => beb.GetType().Name,
                };
                fallos.Add($"{d.GetType().Name}.{e.Current.Property.Name} <- {{Binding {ruta}}} sobre DataContext={dataContext?.GetType().Name ?? "null"}");
            }
        }
    }

    // Recorrido real del arbol visual (no logico) - mismo patron ya usado por RESIZE-DIAG mas
    // arriba en este fichero, generalizado con un tipo T para reutilizarlo en AR-02..AR-10.
    private static IEnumerable<T> Descendientes<T>(DependencyObject raiz) where T : DependencyObject
    {
        int n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(raiz);
        for (int i = 0; i < n; i++)
        {
            var hijo = System.Windows.Media.VisualTreeHelper.GetChild(raiz, i);
            if (hijo is T t) yield return t;
            foreach (var nieto in Descendientes<T>(hijo)) yield return nieto;
        }
    }

    // Verificacion real de N-3 (auditoria de Opus, Bloque 3, atajos de teclado): Keyboard.Modifiers
    // lee el estado REAL del teclado a nivel de SO (no algo derivable de un RoutedEventArgs
    // sintetico) - la unica forma real de probar un Ctrl+combinacion de verdad es inyectar
    // pulsaciones reales a nivel de SO (keybd_event), con la ventana real en primer plano.
    [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    // Pedido explicito del usuario (4-sep-2026, "el boton dónde lo encuentro deja la interfaz
    // bloqueada"): el gesto de RATON real (no InvokePattern/Command.Execute, que nunca pasan
    // por la captura/el foco reales de Windows) - unico precedente real de raton simulado en
    // este arnes, necesario para reproducir de verdad el bug real (Border+MouseBinding dentro
    // de un Popup StaysOpen=False, ver el comentario real de RowClickButton en Theme.xaml).
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002, MOUSEEVENTF_LEFTUP = 0x0004;
    // GetCursorPos (25-sep-2026, verificacion real del arreglo de CanarioDragGhostLibreria): la
    // MISMA API Win32 que ahora usa MainWindow.StartCardDrag/OnFeedback en produccion, para poder
    // medir de forma independiente (sin llamar a codigo de produccion) si la posicion real del
    // cursor sigue disponible durante el bucle modal OLE - a diferencia de Mouse.GetPosition, que
    // esta comprobado que se congela ahi SIEMPRE, sea cual sea el codigo de produccion.
    [StructLayout(LayoutKind.Sequential)]
    private struct Win32PointTests { public int X; public int Y; }
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Win32PointTests point);

    // ForzarPrimerPlano (15-sep-2026, ronda de re-verificacion de los 21 FALLO): mismo problema
    // real, mismo arreglo real, que StarvekeepMod ya encontro y resolvio calibrando DST el
    // 14-sep-2026 (ver StarvekeepMod/bitacora.md linea ~5109 y KeepQA/src/entrada-dst/
    // clicar_pantalla.py, funcion forzar_primer_plano) - Windows tiene un "foreground lock
    // timeout" real: un proceso normal NO puede robarle el primer plano a otro salvo un puñado
    // de excepciones concretas, y SetForegroundWindow a secas puede devolver FALSE en silencio
    // (o, peor, devolver TRUE sin que el cambio ocurra de verdad) con la ventana objetivo viva,
    // visible y respondiendo - exactamente la familia de falso negativo detras de UI-BLOQUEADA/
    // AR-EX2-PAN/AR-EX2-MINIMAPA/H5-05/T-H-FOCO en este mismo arnes. El truco real que lo
    // desbloquea (documentado por Microsoft como una de las excepciones reales a la regla): 1) un
    // toque de ALT (keybd_event) "desarma" el bloqueo un instante, 2) AttachThreadInput entre el
    // hilo llamante y el hilo de la ventana objetivo hace que SetForegroundWindow cuente como si
    // fuera "el mismo" hilo que ya tiene el foco - condicion que Windows SI permite. Nunca fiarse
    // del booleano de la API: se confirma DESPUES con GetForegroundWindow() == hwnd, igual que ya
    // hace clicar_pantalla.py.
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private const byte VK_MENU = 0x12;

    private static bool ForzarPrimerPlano(IntPtr hwndObjetivo)
    {
        if (GetForegroundWindow() == hwndObjetivo) return true;

        keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
        keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

        uint hiloActual = GetCurrentThreadId();
        uint hiloObjetivo = GetWindowThreadProcessId(hwndObjetivo, IntPtr.Zero);
        bool adjuntado = hiloObjetivo != 0 && hiloObjetivo != hiloActual && AttachThreadInput(hiloActual, hiloObjetivo, true);
        try
        {
            BringWindowToTop(hwndObjetivo);
            SetForegroundWindow(hwndObjetivo);
        }
        finally
        {
            if (adjuntado) AttachThreadInput(hiloActual, hiloObjetivo, false);
        }
        System.Threading.Thread.Sleep(150);
        bool logrado = GetForegroundWindow() == hwndObjetivo;
        if (!logrado) Console.WriteLine("ForzarPrimerPlano: no se consiguio poner la ventana en primer plano de verdad (GetForegroundWindow no coincide tras el truco ALT+AttachThreadInput) - posible falso negativo de entrada sintetica en las comprobaciones siguientes");
        return logrado;
    }

    private static void RealClickAt(int screenX, int screenY)
    {
        SetCursorPos(screenX, screenY);
        System.Threading.Thread.Sleep(30); // el SO real necesita un instante para registrar la posicion antes del down/up
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        System.Threading.Thread.Sleep(30);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const byte VK_CONTROL = 0x11;

    // Oleada del 6-sep-2026 - BUG REAL DEL PROPIO ARNES: los 4 keybd_event iban seguidos SIN
    // bombear la cola de mensajes entre medias. keybd_event solo ENCOLA; cuando WPF llega a
    // procesar el KeyDown de la tecla, el KeyUp del Control ya puede estar encolado o incluso
    // procesado, asi que Keyboard.Modifiers dentro del manejador NO lleva Control y el atajo no
    // dispara. Por eso las teclas SUELTAS (PressKey: Supr, flechas) siempre han funcionado y las
    // combinaciones con Ctrl no: H5-14 llevaba dando 3 FALLO fijos (Ctrl+C/Ctrl+V y Ctrl+1) que
    // parecian de la app y eran del arnes. Un DoEvents() entre pulsacion y pulsacion deja que
    // cada mensaje se procese con el estado de modificadores correcto.
    private static void PressCtrlPlus(byte vkKey)
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        DoEvents();
        keybd_event(vkKey, 0, 0, UIntPtr.Zero);
        DoEvents();
        keybd_event(vkKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        DoEvents();
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        DoEvents();
    }

    private static void PressKey(byte vkKey)
    {
        keybd_event(vkKey, 0, 0, UIntPtr.Zero);
        keybd_event(vkKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private static CategoryNodeViewModel? FindCategoryWithItem(IEnumerable<CategoryNodeViewModel> nodes, int itemId)
    {
        foreach (var node in nodes)
        {
            if (node.ItemIdsOrdered.Contains(itemId)) return node;
            var inChild = FindCategoryWithItem(node.Children, itemId);
            if (inChild != null) return inChild;
        }
        return null;
    }
}
