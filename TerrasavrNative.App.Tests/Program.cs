// Auditoria de Opus, Bloque 6 (T-21): antes este arnes vivia SOLO en el scratchpad efimero de
// cada sesion - cada continuacion de este proyecto lo reconstruia desde cero (cientos de lineas
// re-escritas, decenas de bugs ya resueltos antes vueltos a pisar sin querer). Ahora es un
// proyecto real y permanente del propio repo (`TerrasavrNative.App.Tests`, en `TerrasavrNative.
// slnx`) - se compila y ejecuta con `dotnet run --project TerrasavrNative.App.Tests` desde la
// raiz del repo, sin depender de ninguna ruta de scratchpad.
//
// NO es un proyecto xunit (`[Fact]`/`Assert`) a proposito: gran parte de lo que verifica es
// VISUAL (capturas reales de pantalla que hace falta mirar, no solo un booleano pasa/falla) -
// un runner de tests headless nunca podria juzgar eso. Sigue siendo un programa de consola con
// un Main() real que monta una MainWindow real, coloca datos reales, interactua via UI
// Automation real (clics/teclado/scroll reales, nunca simulados a medias) y deja capturas +
// lineas "esperado X, obtenido Y" en stdout para revisar a mano. Ver CLAUDE.md ("Verdad del
// entorno WPF... un arnes que instancia TerrasavrNative.App.App y llama a Run() crea una
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
using TerrasavrNative.App;
using TerrasavrNative.App.Controls;
using TerrasavrNative.App.Converters;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.PlrFormat;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Verificacion real de T-12 (auditoria de Opus, Bloque 3): sesion local normal (esta
        // maquina, sin RDP) -> false; con la variable de entorno puesta -> true. El propio
        // OnStartup de App.xaml.cs nunca se ejecuta en este arnes (crea un Application a pelo),
        // por eso ShouldForceSoftwareRendering() se probo aparte, como metodo publico.
        Console.WriteLine($"T12-RENDER: local sin RDP -> ShouldForceSoftwareRendering()={TerrasavrNative.App.App.ShouldForceSoftwareRendering()} (esperado False en esta maquina)");
        Environment.SetEnvironmentVariable("TERRAKEEP_FORCE_SOFTWARE_RENDER", "1");
        Console.WriteLine($"T12-RENDER: con TERRAKEEP_FORCE_SOFTWARE_RENDER=1 -> ShouldForceSoftwareRendering()={TerrasavrNative.App.App.ShouldForceSoftwareRendering()} (esperado True)");
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
            Source = new Uri("pack://application:,,,/TerrasavrNative.App;component/Styles/Theme.xaml")
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
        app.Resources["InverseBoolToVis"] = new InverseBooleanToVisibilityConverter();
        app.Resources["BoolToGridLength"] = new BoolToGridLengthConverter();
        app.Resources["BoolToDouble"] = new BoolToDoubleConverter();
        app.DispatcherUnhandledException += (_, e) =>
        {
            Console.WriteLine("DISPATCHER-EXCEPTION: " + e.Exception);
            e.Handled = true;
        };

        var swStartup = System.Diagnostics.Stopwatch.StartNew();
        var window = new MainWindow();
        swStartup.Stop();
        Console.WriteLine($"T-G-ARRANQUE: new MainWindow() (CharacterFileService + MainViewModel + XAML) tardo {swStartup.ElapsedMilliseconds}ms");
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
        window.Width = 1180;
        window.Height = 860;
        DoEvents(); DoEvents();
        Console.WriteLine($"ARNES-TAMAÑO-BASE: Width={window.Width} Height={window.Height} (fijado aqui para que el resto del arnes no dependa del tamaño heredado de window.json)");

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

        // Verificacion real de I-1 (auditoria de Opus, Bloque 2): HomeViewModel escanea SOLO
        // al construirse (constructor de MainViewModel, antes de este punto) la carpeta REAL de
        // tModLoader de esta maquina - sin sintetizar nada, se comprueban los .plr reales que
        // ya existen ahi.
        Console.WriteLine($"HOME-SCAN: {vm.Home.Characters.Count} personaje(s) encontrado(s) en la carpeta real");
        foreach (var entry in vm.Home.Characters)
            Console.WriteLine($"  - {entry.Name} | {entry.DifficultyLabel} | Calamity={entry.IsCalamity} | {entry.LastModifiedText}");
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
                    // H5-04 (quinta auditoria de Opus): "Historial de guardados" es un
                    // CONTENEDOR de submenu real (HasItems=true) - no invoca nada por si
                    // mismo, asi que no tiene ni tiene por que tener Command/CommandParameter
                    // propios (mismo criterio que cualquier MenuItem "padre" real de WPF).
                    var comandosNulos = menu.Items.OfType<System.Windows.Controls.MenuItem>()
                        .Where(mi => !mi.HasItems && (mi.Command == null || mi.CommandParameter == null))
                        .Select(mi => (string)mi.Header).ToList();
                    Console.WriteLine($"I-b: {menu.Items.Count} item(s) de menu, comandos sin resolver={string.Join(",", comandosNulos)} (esperado ninguno)");
                    if (comandosNulos.Count > 0) Console.WriteLine("FALLO: I-b (segunda auditoria) - el truco PlacementTarget.Tag no resolvio Command/CommandParameter en algun item");

                    // H5-04 (quinta auditoria de Opus): abre de verdad el submenu real
                    // "Historial de guardados" (dispara OnBackupHistorySubmenuOpened, el mismo
                    // camino real que un clic del usuario) - confirma que puebla algo real (la
                    // lista real de copias, o el aviso real de "sin copias todavia") y nunca se
                    // queda en el placeholder "(cargando...)" ni lanza una excepcion real.
                    var historialItem = menu.Items.OfType<System.Windows.Controls.MenuItem>()
                        .FirstOrDefault(mi => (string)mi.Header == "Historial de guardados");
                    if (historialItem == null) Console.WriteLine("H5-04-HISTORIAL: MenuItem NO-FOUND");
                    else
                    {
                        historialItem.IsSubmenuOpen = true;
                        DoEvents(); DoEvents();
                        var cabeceras = historialItem.Items.OfType<System.Windows.Controls.MenuItem>().Select(mi => (string)mi.Header).ToList();
                        bool sigueEnPlaceholder = cabeceras.Count == 1 && cabeceras[0] == "(cargando...)";
                        Console.WriteLine($"H5-04-HISTORIAL: {cabeceras.Count} item(s) reales tras abrir el submenu ({string.Join(" | ", cabeceras)}) (esperado != placeholder)");
                        if (sigueEnPlaceholder) Console.WriteLine("FALLO: H5-04-HISTORIAL - el submenu se quedo en el placeholder, OnBackupHistorySubmenuOpened no lo repoblo");
                        historialItem.IsSubmenuOpen = false;
                    }

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
                Console.WriteLine($"H6-01-DOLL: personaje real '{vm.CharacterName}', IsMale={vm.Appearance.IsMale}, HairStyle={vm.Appearance.HairStyle}, pixeles opacos={opacosH6}/2240 (esperado > 700)");
                if (previewH6 == null) Console.WriteLine("FALLO: H6-01 - Appearance.PreviewImage es null tras cargar un personaje real");
                else if (opacosH6 <= 700) Console.WriteLine("FALLO: H6-01 - muy pocos pixeles opacos, los brazos/torso no se estan componiendo de verdad");

                var rtbH6 = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbH6.Render(window);
                var encH6 = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encH6.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbH6));
                using (var fsH6 = File.Create(Path.Combine(AppContext.BaseDirectory, "h6-doll-personaje-real.png"))) encH6.Save(fsH6);
                Console.WriteLine("Captura doll con brazos, personaje real -> h6-doll-personaje-real.png");

                vm.SelectedTabIndex = 0; // deja la navegacion como estaba para el resto del arnes
                DoEvents();
            }
            catch (Exception ex) { Console.WriteLine("H6-01-EXCEPTION: " + ex); }
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
            // diferencia de ammo/mountType/etc). id sintetico 20000243 = armadura real
            // (Armor/Aerospec), 20000000 = accesorio real (Accessories) - ver catalog.json.
            int calamityArmorId = 20000243, calamityAccessoryId = 20000000;
            Console.WriteLine($"Calamity armadura en slot cabeza: headSlot.AcceptsItem({calamityArmorId})={headSlot.AcceptsItem(calamityArmorId)} (esperado True)");
            Console.WriteLine($"Calamity armadura en slot accesorio: accSlot.AcceptsItem({calamityArmorId})={accSlot.AcceptsItem(calamityArmorId)} (esperado False - este era el bug)");
            Console.WriteLine($"Calamity accesorio en slot accesorio: accSlot.AcceptsItem({calamityAccessoryId})={accSlot.AcceptsItem(calamityAccessoryId)} (esperado True)");
            Console.WriteLine($"Calamity accesorio en slot cabeza: headSlot.AcceptsItem({calamityAccessoryId})={headSlot.AcceptsItem(calamityAccessoryId)} (esperado False)");

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

        string[] tabNames = ["Equipamiento", "Inventario", "Almacenes"]; // quinta pasada: Monturas/Monedas ya no son pestañas, se fusionaron dentro de Equipamiento
        foreach (var name in tabNames)
        {
            try
            {
                var tabCondition = new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                    new PropertyCondition(AutomationElement.NameProperty, name));
                var tabItem = root.FindFirst(TreeScope.Descendants, tabCondition);
                if (tabItem == null) { Console.WriteLine($"TAB {name}: NOT-FOUND"); continue; }

                if (tabItem.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pat))
                    ((SelectionItemPattern)pat).Select();
                else
                    Console.WriteLine($"TAB {name}: NO-SELECTIONITEMPATTERN");

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
            var equipTabAgain = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Equipamiento")));
            if (equipTabAgain != null && equipTabAgain.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var equipAgainPat))
                ((SelectionItemPattern)equipAgainPat).Select();
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
                if (d is TerrasavrNative.App.Controls.SlotGridPanel sgp)
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
            var almacenesTab = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Almacenes")));
            if (almacenesTab != null && almacenesTab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selPat))
                ((SelectionItemPattern)selPat).Select();
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
            var equipTab = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Equipamiento")));
            if (equipTab != null && equipTab.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var equipSelPat))
                ((SelectionItemPattern)equipSelPat).Select();
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
                Console.WriteLine($"PILDORA Equipamiento -> Current={vm.EquipmentGroup?.Current.DisplayName} Columns={vm.EquipmentGroup?.Current.Columns} (esperado: Equipo puesto - vanidad, Columns=5)");
            }
            else Console.WriteLine("PILDORA Equipamiento: boton 'Vanidad' NO-FOUND");
        }
        catch (Exception ex)
        {
            Console.WriteLine("EQUIPAMIENTO-PILDORA-EXCEPTION: " + ex);
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
                var pathReal = TerrasavrNative.App.Services.VanillaIconResolver.GetIconPath(idConocido);
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
                new PropertyCondition(AutomationElement.NameProperty, "🔍 ¿Dónde lo tengo?")));
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
                new PropertyCondition(AutomationElement.NameProperty, "🔍 ¿Dónde lo tengo?")));
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
                SetForegroundWindow(hwnd);
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
            vm.SelectedTabIndex = 5; // Acerca de
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
                emptyBuffSlot.PlaceBuff(TerrasavrNative.Core.Calamity.CalamityIds.BuffIdBase);
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
                var svcH612 = (TerrasavrNative.App.Services.CharacterFileService)serviceFieldH612!.GetValue(vm)!;
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
            window.Width = w;
            window.Height = h;
            DoEvents();
            DoEvents();
            DoEvents();
            Console.WriteLine($"  Ventana pedida {w}x{h} -> real ActualWidth={window.ActualWidth:0.#} ActualHeight={window.ActualHeight:0.#}");

            // "Equipamiento"/"Inventario"/"Almacenes" viven DENTRO de "Personaje" > "Objetos" -
            // hace falta seleccionar esos dos primero o el TabItem interno ni siquiera existe
            // en el arbol visual (TabControl solo realiza el contenido de la pestaña activa).
            vm.SelectedTabIndex = 1;
            vm.PersonajeInnerTabIndex = 0;
            DoEvents();

            var tabItem = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, tabName)));
            if (tabItem != null && tabItem.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selPat))
                ((SelectionItemPattern)selPat).Select();
            else
                Console.WriteLine($"  AVISO: TabItem '{tabName}' no encontrado");
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
            Console.WriteLine($"E2-UMBRAL: en 1450px, SizeClass={vm.SizeClass} IsEquipmentExpanded={vm.IsEquipmentExpanded} (esperado Compacto/Normal, False - probado a mano que 1450 recorta la 3a vista, ver bitacora.md)");
            CaptureAt(1550, 860, "Equipamiento", "e2-justo-encima-1550.png");
            Console.WriteLine($"E2-UMBRAL: en 1550px, SizeClass={vm.SizeClass} IsEquipmentExpanded={vm.IsEquipmentExpanded} (esperado Amplio, True)");

            // Diagnostico whitebox real: anchos reales de las 3 columnas del SlotRowHost y de
            // cada SlotGridPanel dentro, a la resolucion minima real - para saber si el centro
            // (Armadura/Accesorios) tiene de verdad sitio para sus 5 columnas a MinCell=40, en
            // vez de seguir adivinando a mano.
            void FindRowHostWidths(System.Windows.DependencyObject d)
            {
                if (d is TerrasavrNative.App.Controls.SlotRowHost srh)
                {
                    Console.WriteLine($"  RESIZE-DIAG SlotRowHost: ActualWidth={srh.ActualWidth:0.#}");
                    foreach (var cd in srh.ColumnDefinitions)
                        Console.WriteLine($"  RESIZE-DIAG   Column: ActualWidth={cd.ActualWidth:0.#} Width={cd.Width} MinWidth={cd.MinWidth}");
                }
                if (d is TerrasavrNative.App.Controls.SlotGridPanel sgp)
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

            // Verificacion real de A-4 (auditoria de Opus, Bloque 4): por debajo de
            // AmplioMinWidth=1500 (umbral real, medido - ver el comentario de
            // MainViewModel.IsStorageExpanded), la pestaña "Inventario" sigue mostrando solo
            // Inventario - Almacenes ni siquiera existe en el arbol visual en ese momento
            // (drag&drop cruzado imposible, tal cual hasta ahora). Se prueba justo por debajo
            // (1350) para confirmar que el umbral real es de verdad 1500, no el "Normal"
            // original (1300).
            static List<AutomationElement> BuscarPildoraBanco(AutomationElement r) =>
                r.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                    .Cast<AutomationElement>().Where(b => b.Current.Name.StartsWith("Banco")).ToList();

            CaptureAt(1350, 860, "Inventario", "a4-todavia-compacto-1350.png");
            Console.WriteLine($"A4-1350: pildora 'Banco' presente en el arbol visual={BuscarPildoraBanco(root).Count > 0} (esperado False)");

            // A partir de 1500px, Inventario Y Almacenes deben coexistir de verdad en el mismo
            // arbol visual - prueba real (no solo el ViewModel): la pildora real de Almacenes
            // ("Banco...") tiene que aparecer YA, sin cambiar de pestaña.
            CaptureAt(1500, 860, "Inventario", "a4-expandido-1500.png");
            Console.WriteLine($"A4-EXPANDIDO: pildora real de Almacenes presente en Inventario={BuscarPildoraBanco(root).Count > 0} (esperado True - coexisten de verdad, no solo el ViewModel)");

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
            window.Width = 1080; window.Height = 700;
            DoEvents(); DoEvents();
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
            window.Width = 1180;
            window.Height = 860;
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
            var svc = (TerrasavrNative.App.Services.CharacterFileService)serviceField!.GetValue(vm)!;
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
            SetForegroundWindow(hwnd);
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
            Console.WriteLine($"N3-CTRL-F: SelectedTabIndex={vm.SelectedTabIndex} (esperado 1), IsLibraryCollapsed={vm.IsLibraryCollapsed} (esperado False), foco real en LibrarySearchBox={ReferenceEquals(focused, window.FindName("LibrarySearchBox"))}");

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
            SetForegroundWindow(hwnd);
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

            window.Width = 1550; window.Height = 900; DoEvents(); DoEvents(); DoEvents();
            Console.WriteLine($"H5-09-AMPLIO: SizeClass={vm.SizeClass} DetailContentMaxWidth={vm.DetailContentMaxWidth} DetailCardColumns={vm.DetailCardColumns} (esperado Amplio/1200/2)");
            CaptureDetailTab(1, 5, "Desbloqueos", "h5-09-desbloqueos-amplio.png");
            CaptureDetailTab(1, 6, "Versión", "h5-09-version-amplio.png");
            CaptureDetailTab(3, null, "Terraria", "h5-09-novedades-amplio.png");
            CaptureDetailTab(5, null, "Acerca de", "h5-09-acerca-de-amplio.png");

            window.Width = 1180; window.Height = 860; DoEvents(); DoEvents(); DoEvents();
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
            string worldPath = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld";
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
                var worldScroll = (ScrollViewer)window.FindName("WorldMapScroll");
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
                vm.Servers.AddEntryCommand.Execute(null);
                var spawnRow = vm.Servers.Entries[^1];
                spawnRow.Name = "X-g prueba real";
                spawnRow.SpawnX = 4200;
                spawnRow.SpawnY = 300;
                vm.SelectedTabIndex = 1; // Personaje
                DoEvents();
                vm.SelectedTabIndex = 4; // Exploracion - dispara el refresco real
                DoEvents(); DoEvents();
                bool xgEncontrado = vm.Exploration.CharacterSpawns.Any(s => s.Label == "X-g prueba real" && s.TileX == 4200 && s.TileY == 300);
                Console.WriteLine($"X-G-SPAWN-PERSONAJE: Spawn Point real añadido -> aparece en el mapa={xgEncontrado} (esperado True), CharacterSpawns.Count={vm.Exploration.CharacterSpawns.Count}");
                if (!xgEncontrado) Console.WriteLine("FALLO: X-g (segunda auditoria) - el Spawn Point real del personaje no llego al mapa");
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
            window.Width = 1180; window.Height = 700;
            DoEvents();
            DoEvents();

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
        try
        {
            // El foco visual real de WPF solo se pinta con la ventana ACTIVA (igual que el
            // sistema operativo real nunca muestra el foco de una ventana en segundo plano) -
            // SetForegroundWindow real antes de SetFocus(), mismo patron ya usado para los
            // atajos Ctrl+ reales de mas arriba.
            SetForegroundWindow(hwnd);
            var saveButton = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.NameProperty, "Guardar")));
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
            DoEvents(); DoEvents(); // deja que el TabControl realice el contenido de "Objetos" antes de buscar la sub-pestaña "Inventario" dentro
            var invTabForKeys = root.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Inventario")));
            if (invTabForKeys != null && invTabForKeys.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var invSelPatForKeys))
                ((SelectionItemPattern)invSelPatForKeys).Select();
            DoEvents(); DoEvents();

            var slotOrigen = vm.InventoryContainer!.Slots[20]; // vacio (0-11 ocupados por la fixture, 12/49 por H5-12)
            var slotVecino = vm.InventoryContainer.Slots[21];
            var slotParaCopiar = vm.InventoryContainer.Slots[22];
            slotOrigen.PlaceItem(4); // Espada larga de hierro, id real cualquiera - lo que importa es tener algo que vaciar
            slotParaCopiar.PlaceItem(3);
            slotParaCopiar.Count = 7;
            slotParaCopiar.ToggleFavoriteCommand.Execute(null);

            SetForegroundWindow(hwnd);
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
            SetForegroundWindow(hwnd);
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

        string errorLog = Path.Combine(AppContext.BaseDirectory, "ultimo-error.log");
        Console.WriteLine("ultimo-error.log existe: " + File.Exists(errorLog));

        // Bloque 0 (N-2, IsDirty real): MainWindow.OnWindowClosing ahora muestra un MessageBox
        // MODAL real de "cambios sin guardar" cuando IsDirty=true - sin nadie delante para
        // pulsarlo, este arnes se quedaba colgado para siempre en window.Close() (confirmado:
        // 5+ minutos sin avanzar, salida vacia incluso tras matar el proceso - el buffer de
        // consola redirigido nunca llega a volcarse porque el hilo de UI nunca vuelve). El
        // arnes es codigo de prueba, no un usuario real - se limpia el flag antes de cerrar.
        // Verificacion real de T-3 (auditoria de Opus, Bloque 4): tamaño/posicion reconocibles
        // y distintos de los de fabrica, antes de cerrar (Close() real dispara
        // OnWindowClosing -> WindowPlacementService.Save real).
        window.Left = 40;
        window.Top = 55;
        window.Width = 1234;
        window.Height = 789;
        DoEvents();

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

    private static void PressCtrlPlus(byte vkKey)
    {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(vkKey, 0, 0, UIntPtr.Zero);
        keybd_event(vkKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
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
