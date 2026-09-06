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
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.Nbt;
using TerrasavrNative.Core.PlrFormat;
using TerrasavrNative.Core.WldFormat;

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
        app.Resources["InverseBoolToVis"] = new InverseBooleanToVisibilityConverter();
        app.Resources["BoolToGridLength"] = new BoolToGridLengthConverter();
        app.Resources["BoolToDouble"] = new BoolToDoubleConverter();
        // Punto 4 (advisor Opus, selector de categoria de Exploracion - ver
        // ESPEC-ui-exploracion.md#9.1): mismo motivo real que el resto de converters de arriba -
        // se olvido la primera vez que se probo esta tanda, mismo bug real ya documentado.
        app.Resources["EnumEquals"] = new EnumEqualsConverter();
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

            string worldPathShot = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld";
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

            vmShot.SelectedTabIndex = 5; // Acerca de (incluye Ajustes)
            vmShot.Settings.Language = "en";
            DoEvents();
            Shot("04-about-settings-en");
            vmShot.Settings.Language = "es";
            DoEvents();

            Console.WriteLine($"SCREENSHOTS-LISTAS: {shotDir}");
            Environment.Exit(0);
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

        // Verificacion real de I-1 (auditoria de Opus, Bloque 2): HomeViewModel escanea SOLO
        // al construirse (constructor de MainViewModel, antes de este punto) la carpeta REAL de
        // tModLoader de esta maquina - sin sintetizar nada, se comprueban los .plr reales que
        // ya existen ahi.
        Console.WriteLine($"HOME-SCAN: {vm.Home.Characters.Count} personaje(s) encontrado(s) en la carpeta real");
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
            FijarTamaño(window, w, h);
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
                    // Paso el debounce (250ms) primero, para que el barrido en segundo plano ya
                    // este en marcha de verdad antes de mirar.
                    WaitForDispatcher(280);
                    bool buscandoAMitad = vm.Exploration.IsSearching;
                    WaitForDispatcher(1720); // resto hasta completar (mismo total de 2000ms ya medido)
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
                        var cabeceraReal = TerrasavrNative.Core.WldFormat.WldReader.ReadHeader(File.ReadAllBytes(worldPath));
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
                    var worldIndependiente = TerrasavrNative.Core.WldFormat.WldReader.Read(File.ReadAllBytes(worldPath));

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
                        bool encontrado = vm.Exploration.WorldSearchResults.Any(h => h.TileX == chestConObjeto.X && h.TileY == chestConObjeto.Y);
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
                        bool encontrado = vm.Exploration.WorldSearchResults.Any(h => h.TileX == letreroReal.X && h.TileY == letreroReal.Y);
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
                        if (existe)
                        {
                            var bytesPng = File.ReadAllBytes(pngTemporal);
                            using var msPng = new MemoryStream(bytesPng);
                            var decoder = new System.Windows.Media.Imaging.PngBitmapDecoder(msPng, System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                            anchoPng = decoder.Frames[0].PixelWidth;
                            altoPng = decoder.Frames[0].PixelHeight;
                        }
                        Console.WriteLine($"F-12: PNG exportado existe={existe}, {anchoPng}x{altoPng} (esperado {hdrReal.TilesWide}x{hdrReal.TilesHigh})");
                        if (!existe || anchoPng != hdrReal.TilesWide || altoPng != hdrReal.TilesHigh)
                            Console.WriteLine("FALLO: F-12 - el PNG exportado no existe o no tiene las dimensiones reales del mundo");
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
                    }
                    else Console.WriteLine("F-10: no se encontro la barra lateral en el arbol visual - omitido");

                    // F-8 (auditoria de Opus vs TEdit, E-05): el minimapa real muestra el bitmap
                    // del mundo YA congelado, y su rectangulo de viewport esta visible.
                    var minimapImg = Descendientes<System.Windows.Controls.Image>(window).FirstOrDefault(i => i.Name == "MinimapImage");
                    Console.WriteLine($"F-8: MinimapImage encontrado={minimapImg != null}, con el bitmap real del mundo={minimapImg?.Source != null} (esperado True)");
                    if (minimapImg == null || minimapImg.Source == null) Console.WriteLine("FALLO: F-8 - el minimapa no muestra el bitmap real del mundo");
                    var minimapRect = Descendientes<System.Windows.Shapes.Rectangle>(window).FirstOrDefault(r => r.Name == "MinimapViewportRect");
                    Console.WriteLine($"F-8: MinimapViewportRect encontrado={minimapRect != null}, visible={minimapRect?.IsVisible} (esperado True)");
                    if (minimapRect == null || !minimapRect.IsVisible) Console.WriteLine("FALLO: F-8 - el rectangulo de viewport del minimapa no aparece");

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
                    var contenidoCat = window.FindName("ExplorationCategoryContent") as FrameworkElement;
                    var bloqueResultados = window.FindName("ExplorationResultsBlock") as FrameworkElement;
                    var chipsCofres = window.FindName("ChestModeSelector") as FrameworkElement;
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
                    .Cast<AutomationElement>().Where(t => t.Current.Name is "Objetos" or "Buffs" or "Investigación" or "Apariencia" or "Spawn Points" or "Desbloqueos" or "Versión").ToList();
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

                var backups = new BackupHistoryService();
                backups.SaveBackup(Cargado(pjA));
                backups.SaveBackup(Cargado(pjB));
                int deA = backups.ListBackups(pjA).Count;
                int deB = backups.ListBackups(pjB).Count;
                long tamañoDeA = deA > 0 ? new FileInfo(backups.ListBackups(pjA)[0].PlrPath).Length : -1;
                long tamañoDeB = deB > 0 ? new FileInfo(backups.ListBackups(pjB)[0].PlrPath).Length : -1;
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
            //  - los CATALOGOS DE CONTENIDO del juego (nombres de objeto/NPC/tile/buff): viven
            //    en sus propios JSON de datos y solo existen en español. Traducirlos es una
            //    decision aparte, pendiente del usuario (ver bitacora.md, 6-sep-2026).
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
            foreach (string fichero in new[] { "vanilla_item_names.json", "npc_names.json", "vanilla_buff_names_es.json", "tile_names.json", @"calamity\catalog.json", "calamity_buff_descriptions.json" })
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
                             Action<TerrasavrNative.App.ViewModels.CategoryNodeViewModel> seleccionar)
            {
                var lista = raices.Cast<TerrasavrNative.App.ViewModels.CategoryNodeViewModel>().ToList();
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
            vm.SelectedTabIndex = 5;
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
            WindowPlacementService.Apply(window);
            DoEvents(); DoEvents();
            bool aplicoElFijado = Math.Abs(window.Width - anchoNormal) < 2 && Math.Abs(window.Height - altoNormal) < 2;
            bool noMaximizo = window.WindowState == System.Windows.WindowState.Normal;
            Console.WriteLine($"A10-VENTANAFIJA-MAXIMIZADA: Apply() con el tick puesto deja la ventana en {window.Width:0}x{window.Height:0} (esperado {anchoNormal:0}x{altoNormal:0}) -> {aplicoElFijado}, y NO maximizada -> {noMaximizo}");
            if (!aplicoElFijado || !noMaximizo) Console.WriteLine("FALLO: A10-VENTANAFIJA-MAXIMIZADA - Apply() no restauro el tamaño fijado, o maximizo con el tick puesto");

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
