using System.IO;
using System.Linq;
using Terrakeep.App;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;
using ServidorKeep.Core.Instancias;

// Cierre de sesion (15-sep-2026, encargo del coordinador): las capturas de docs/screenshots/ que
// usa README.md llevaban desde el 5-sep-2026 (version 2.1.0). Aquella ronda uso el personaje/
// mundo REALES de esta maquina ('adrian'/'roca_negra.wld') como dato "presentable". Saneado de
// privacidad (29-sep-2026, antes de publicar la 3.3.0 en el repo PUBLICO): el propio criterio de
// la familia Keep para capturas de README es "SOLO personajes y mundos de PRUEBA, nunca partidas
// reales del usuario" (ver REGLAS-PUBLICACION-FAMILIA.md) - un personaje llamado igual que el
// usuario de Windows es justo el caso que esa regla prohibe. Ahora se construye un personaje
// sintetico PRESENTABLE de verdad (armadura Hallowed completa + Terra Blade + monedas, nunca
// "UIA-Test" vacio) con Terrakeep.Core.PlrFormat directamente (mismo mecanismo ya usado por
// AuditoriaBadgesEstado.cs para sus personajes sinteticos), y se aisla el escaneo de Inicio a
// SOLO esa carpeta sintetica via CharacterFileService.CarpetasPersonajesDePrueba/
// CarpetasMundosDePrueba (SOLO PRUEBAS, exige App.ModoDiagnostico=true) - así ninguna captura
// puede mostrar ya el nombre de una partida real, ni siquiera de las que YA estaban aisladas de
// escritura por AislamientoPartidasReales.cs (esa proteccion es contra ESCRITURA, no evita que el
// NOMBRE real se vea en pantalla). El mundo sigue siendo 'roca_negra.wld' real (su contenido -
// terreno/cofres - no identifica al usuario, a diferencia de un nombre de personaje), SIEMPRE
// sobre la copia YA aislada por AislamientoPartidasReales.cs, nunca el original de Documentos.
internal static partial class Program
{
    private static void CapturarPantallasReadme(MainWindow window, MainViewModel vm)
    {
        string dirDocs = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "screenshots");
        dirDocs = Path.GetFullPath(dirDocs);
        if (!Directory.Exists(dirDocs))
        {
            Console.WriteLine($"README_SHOTS: carpeta docs/screenshots no encontrada en '{dirDocs}' - omitido.");
            return;
        }

        void Capturar(string nombreArchivo)
        {
            DoEvents(); DoEvents();
            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(window);
            var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            string destino = Path.Combine(dirDocs, nombreArchivo);
            using (var fs = File.Create(destino)) enc.Save(fs);
            Console.WriteLine($"README_SHOTS: {nombreArchivo} <- {window.ActualWidth:0}x{window.ActualHeight:0}, {new FileInfo(destino).Length} bytes");
        }

        vm.Settings.Language = "es";
        FijarTamaño(window, 1920, 1080);

        // Personaje sintetico presentable ("Aventurero" - nunca el nombre del usuario real):
        // Hallowed completo (551/552/553) + Angel Wings (493) equipados, Terra Blade (757) e
        // Iron Pickaxe/Axe (1/10) en el inventario, monedas reales. IDs vanilla confirmados contra
        // Terrakeep.App/Assets/vanilla_item_names_en.json (catalogo real del propio repo, nunca
        // inventados). Version=279 (misma linea base que el resto del arnes).
        var personaje = new PlrCharacter
        {
            Version = 279,
            Name = "Aventurero",
            Difficulty = 0,
            PlayTimeLow = 5_400_000, // ~1h30 a 60 ticks/s, solo para que no salga "0h" en la tarjeta
            HealthMax = 400,
            ManaMax = 200,
            HairColor = [90, 60, 35],
            SkinColor = [255, 200, 165],
            EyeColor = [105, 90, 75],
            ShirtColor = [175, 165, 140],
            UnderColor = [85, 85, 180],
            PantsColor = [170, 140, 90],
            ShoesColor = [130, 90, 60],
            EquipmentItems =
            [
                new PlrItemSlot(553, 1, 0, false), // Hallowed Helmet
                new PlrItemSlot(551, 1, 0, false), // Hallowed Plate Mail
                new PlrItemSlot(552, 1, 0, false), // Hallowed Greaves
                new PlrItemSlot(493, 1, 0, false), // Angel Wings
                PlrItemSlot.Empty,
            ],
            EquipmentDyes = new PlrItemSlot[5],
            Inventory = CrearInventarioPresentable(),
            Coins =
            [
                new PlrItemSlot(74, 2, 0, false),  // Platinum Coin
                new PlrItemSlot(73, 35, 0, false),  // Gold Coin
                new PlrItemSlot(72, 60, 0, false),  // Silver Coin
                new PlrItemSlot(71, 90, 0, false),  // Copper Coin
            ],
            PrimaryLoadout = CrearLoadoutPrincipal(),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };

        string origenWld = MundoAislado(RutasEntornoReal.Documentos(@"tModLoader\Worlds\roca_negra.wld"));

        // Carpeta PROPIA del temp, con la estructura Players/Worlds que espera
        // CharacterFileService (SUSTITUYE por completo las carpetas reales mientras dura esta
        // captura - restaurado al terminar en el finally).
        string tempDir = Path.Combine(Path.GetTempPath(), "terrakeep-readme-shots");
        string dirPersonajes = Path.Combine(tempDir, "Players");
        string dirMundos = Path.Combine(tempDir, "Worlds");
        Directory.CreateDirectory(dirPersonajes);
        Directory.CreateDirectory(dirMundos);
        string copiaPlr = Path.Combine(dirPersonajes, "Aventurero.plr");
        File.WriteAllBytes(copiaPlr, PlrFile.Write(personaje));

        var carpetasPersonajesAnteriores = CharacterFileService.CarpetasPersonajesDePrueba;
        var carpetasMundosAnteriores = CharacterFileService.CarpetasMundosDePrueba;
        CharacterFileService.CarpetasPersonajesDePrueba = [dirPersonajes];
        CharacterFileService.CarpetasMundosDePrueba = [dirMundos];

        string? copiaWld = null;
        try
        {
            // Se carga el personaje sintetico ANTES de capturar Inicio a proposito (bug real de
            // esta MISMA captura encontrado revisando el PNG generado en la ronda original): sin
            // esto, la tarjeta destacada de Inicio ("Continuar con...") sale con el ultimo
            // personaje de una ronda de pruebas anterior. Cargando 'Aventurero' primero, el
            // evento CharacterLoaded real (MainWindow.xaml.cs, H5-07) deja la sesion guardada con
            // un personaje presentable antes de que Inicio se capture.
            // Home.Characters ya se escaneo UNA VEZ contra las carpetas reales antes de que este
            // metodo se ejecutara (T-G, escaneo en segundo plano del propio constructor de
            // MainWindow) - el cambio de CarpetasPersonajesDePrueba de mas arriba no lo reescanea
            // solo. Sin este refresco forzado, "Tus personajes" seguiria mostrando la lista real
            // (Eldelgas/Zenith/Terrariano/adrian) aunque la tarjeta activa ya diga "Aventurero" -
            // bug real encontrado revisando el PNG generado en esta misma ronda de saneado.
            var refrescoHome = vm.Home.RefreshCommand.ExecuteAsync(null);
            while (!refrescoHome.IsCompleted) DoEvents();
            DoEvents();
            Console.WriteLine($"README_SHOTS: Home.Characters tras el refresco aislado = {vm.Home.Characters.Count} (esperado 1, solo 'Aventurero')");

            vm.LoadFromPath(copiaPlr);
            DoEvents();
            // La tarjeta hero "Continuar con X" (LastSessionCharacterEntry) NO se actualiza sola
            // al cargar un personaje por codigo (solo lo hace MainViewModel.SaveSession, ligado al
            // cierre real de la ventana) - se quedaba con el ultimo _lastSessionPath SEMBRADO desde
            // la copia real de session.json (el personaje real de la sesion anterior del usuario).
            // Bug real encontrado revisando el PNG generado en esta misma ronda de saneado.
            vm.Home.SetLastSession(new Terrakeep.App.Services.TerrakeepSession { LastCharacterPath = copiaPlr, LastCharacterName = personaje.Name });
            DoEvents();
            Console.WriteLine($"README_SHOTS: personaje sintetico cargado -> HasCalamityData={vm.HasCalamityData} (esperado False, es vanilla puro)");

            // Copia el mundo a la carpeta aislada ANTES del refresco, para que
            // RefreshWorldsCommand lo detecte y "Tus mundos" muestre 'roca negra' en vez de
            // quedar vacio (dirMundos aun no tenia ningun .wld dentro en el primer intento de
            // este mismo saneado).
            if (File.Exists(origenWld))
            {
                copiaWld = Path.Combine(dirMundos, "roca_negra.wld");
                File.Copy(origenWld, copiaWld, overwrite: true);
                string origenTwld = Path.ChangeExtension(origenWld, ".twld");
                if (File.Exists(origenTwld)) File.Copy(origenTwld, Path.ChangeExtension(copiaWld, ".twld"), overwrite: true);
            }

            // Mismo bug que Home.Characters (ver mas arriba): Exploration.Worlds ("Tus mundos")
            // tambien se escaneo UNA VEZ contra las carpetas reales antes de este metodo - sin
            // refrescarla mostraba los 6 mundos reales de esta maquina (Blando Río, adriandres,
            // El Musgo de Accidentes...) en la barra de "Tus mundos", aunque el mapa cargado ya
            // fuera la copia aislada. Bug real encontrado revisando el PNG generado en esta misma
            // ronda de saneado.
            var refrescoMundos = vm.Exploration.RefreshWorldsCommand.ExecuteAsync(null);
            while (!refrescoMundos.IsCompleted) DoEvents();
            DoEvents();
            Console.WriteLine($"README_SHOTS: Exploration.Worlds tras el refresco aislado = {vm.Exploration.Worlds.Count} (esperado 1, solo 'roca_negra')");

            if (copiaWld != null)
            {
                var cargaTemprana = vm.Exploration.LoadFromPathAsync(copiaWld);
                while (!cargaTemprana.IsCompleted) DoEvents();
                DoEvents();
            }

            // 01-inicio.png: Inicio con la sesion sintetica ya al dia (tarjeta "Continuar con
            // Aventurero", tarjeta del personaje marcada como actual - unico personaje real
            // visible gracias al aislamiento de carpetas de mas arriba). Esperar tambien a que
            // termine el escaneo de disco en segundo plano (Home.IsScanning, fire-and-forget
            // desde el propio constructor, T-G).
            vm.SelectedTabIndex = 0;
            long limiteScan = Environment.TickCount64 + 10_000;
            while (vm.Home.IsScanning && Environment.TickCount64 < limiteScan) DoEvents();
            DoEvents();
            Capturar("01-inicio.png");

            // 02-personaje.png: pestaña Personaje, sub-pestaña Objetos (la que trae la Libreria
            // debajo del inventario desde el 1-sep-2026) - equipo/inventario sinteticos de
            // 'Aventurero'. PersonajeInnerTabIndex se fija a mano (0=Objetos): session.json puede traer
            // persistida la sub-pestaña de una ronda de pruebas anterior (measurado: se quedaba en
            // Apariencia, 3, una captura bastante menos representativa del uso real de la app).
            vm.SelectedTabIndex = 1;
            vm.PersonajeInnerTabIndex = 0;
            // Panel en blanco real (dos rondas seguidas, 40290 bytes deterministas) con solo dos
            // DoEvents(): la vista de Objetos/Libreria tarda mas en montar su arbol la PRIMERA vez
            // que se visita esta pestaña en la sesion (8903 objetos) - WaitForDispatcher(300) da
            // tiempo real al layout, mismo patron ya usado en otros bloques de este arnes.
            WaitForDispatcher(300);
            Capturar("02-personaje.png");

            Console.WriteLine($"README_SHOTS: mundo cargado -> IsWorldLoaded={vm.Exploration.IsWorldLoaded}");
            if (vm.Exploration.IsWorldLoaded)
            {
                // 03-exploracion.png: pestaña Exploracion con una busqueda real abierta
                // (Minerales) - mismo mecanismo por ViewModel que ya usa el resto del arnes
                // (SelectedCategory), sin depender de un clic real de raton (T-H/F2: el raton/
                // teclado sintetico no llega siempre de verdad a la ventana en esta sesion).
                vm.SelectedTabIndex = 4;
                DoEvents();
                vm.Exploration.SelectedCategory = Terrakeep.App.ViewModels.WorldSearchCategory.Ores;
                DoEvents(); DoEvents();
                Capturar("03-exploracion.png");

                // 05-guia.png (NUEVA - README no mostraba esta pestaña, integrada esta misma
                // noche): objetivo actual + arbol de tramos evaluados de verdad contra el
                // personaje y el mundo ya cargados (personaje vanilla puro - MostrarAvisoCalamity
                // esperado False, no hay ningun aviso de Calamity que mostrar).
                vm.SelectedTabIndex = 3; // Guia - AppTab.Guia, reordenado T1 21-sep-2026
                DoEvents();
                vm.Guide.Refresh();
                DoEvents(); DoEvents();
                Console.WriteLine($"README_SHOTS: Guia -> Tramos.Count={vm.Guide.Tramos.Count}, MostrarAvisoCalamity={vm.Guide.MostrarAvisoCalamity}");
                Capturar("05-guia.png");
            }

            // 04-about-settings-en.png: pestaña "Acerca de" en ingles (mismo criterio que la
            // captura ya existente - la version/changelog/autoria reales, en el segundo idioma).
            vm.Settings.Language = "en";
            vm.SelectedTabIndex = 7; // Acerca de - AppTab.AcercaDe, reordenado T1 21-sep-2026
            DoEvents(); DoEvents();
            Capturar("04-about-settings-en.png");
            vm.Settings.Language = "es";
            DoEvents();

            // 06-servidor.png (NUEVA): pestaña Servidor - si Terraria esta instalado de verdad en
            // esta maquina (ya confirmado por HOSTING_SOLO en la Fase B de esta misma noche),
            // lanza una instancia REAL corta por el mismo camino que pulsaria un usuario
            // (Hosting.IniciarCommand) y espera a EnEscucha para que la captura muestre el estado
            // "en escucha" real, no solo el formulario vacio - mas presentable y mas honesto.
            vm.SelectedTabIndex = 5; // Hosting/Servidor - AppTab.Hosting, reordenado T1 21-sep-2026
            DoEvents();
            Console.WriteLine($"README_SHOTS: Hosting -> TerrariaDetectado={vm.Hosting.TerrariaDetectado}");
            if (vm.Hosting.TerrariaDetectado)
            {
                // Nombres presentables (no un nombre de arnes/prueba) - el encargo pedia
                // explicitamente evitar datos de prueba feos en las capturas del README.
                vm.Hosting.NombreInstancia = "Mi servidor";
                vm.Hosting.NombreMundo = "Mundo compartido";
                vm.Hosting.Puerto = 27978; // distinto del usado por HOSTING_SOLO (27977) y del 7777 por defecto
                vm.Hosting.IniciarCommand.Execute(null);
                DoEvents();

                if (string.IsNullOrEmpty(vm.Hosting.ErrorMessage) && vm.Hosting.Instancias.Count > 0)
                {
                    var instancia = vm.Hosting.Instancias[^1];
                    long limiteEscucha = Environment.TickCount64 + 90_000;
                    while (instancia.Nucleo.Estado == EstadoInstancia.Arrancando && Environment.TickCount64 < limiteEscucha)
                    {
                        DoEvents();
                        System.Threading.Thread.Sleep(200);
                    }
                    DoEvents();
                    Console.WriteLine($"README_SHOTS: Hosting -> estado real tras esperar={instancia.Nucleo.Estado}");
                    Capturar("06-servidor.png");

                    int pidReal = instancia.Nucleo.Proceso.Id;
                    var detener = instancia.DetenerCommand.ExecuteAsync(null);
                    long limiteParada = Environment.TickCount64 + 15_000;
                    while (!detener.IsCompleted && Environment.TickCount64 < limiteParada) DoEvents();
                    DoEvents();
                    bool sigueVivo = System.Diagnostics.Process.GetProcesses().Any(p => p.Id == pidReal);
                    Console.WriteLine($"README_SHOTS: Hosting -> tras Detener, proceso sigue vivo={sigueVivo} (esperado False)");
                    try { Directory.Delete(instancia.CarpetaInstancia, recursive: true); } catch { }
                }
                else
                {
                    Console.WriteLine("README_SHOTS: Hosting.IniciarCommand no lanzo ninguna instancia - se captura el formulario tal cual.");
                    Capturar("06-servidor.png");
                }
            }
            else
            {
                Console.WriteLine("README_SHOTS: Terraria no detectado en esta maquina - se captura el formulario/estado tal cual, sin servidor activo.");
                Capturar("06-servidor.png");
            }
        }
        finally
        {
            CharacterFileService.CarpetasPersonajesDePrueba = carpetasPersonajesAnteriores;
            CharacterFileService.CarpetasMundosDePrueba = carpetasMundosAnteriores;
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    // Inventario sintetico presentable para "Aventurero" (README_SHOTS): un pico/hacha de hierro
    // en los dos primeros slots y una Terra Blade bien visible - IDs vanilla confirmados contra
    // Terrakeep.App/Assets/vanilla_item_names_en.json (catalogo real del propio repo).
    private static PlrItemSlot[] CrearInventarioPresentable()
    {
        var inventario = new PlrItemSlot[50];
        Array.Fill(inventario, PlrItemSlot.Empty);
        inventario[0] = new PlrItemSlot(757, 1, 0, false); // Terra Blade
        inventario[1] = new PlrItemSlot(1, 1, 0, false);   // Iron Pickaxe
        inventario[2] = new PlrItemSlot(10, 1, 0, false);  // Iron Axe
        return inventario;
    }

    // Loadout principal sintetico para "Aventurero": la Terra Blade tambien en el primer slot de
    // Items del loadout activo (loadouts[0], el "mirror" - PlrLoadout.CreateEmpty(isPrimary: true)
    // ya deja Hide=null como corresponde a ese loadout).
    private static PlrLoadout CrearLoadoutPrincipal()
    {
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[0] = new PlrItemSlot(757, 1, 0, false); // Terra Blade
        return loadout;
    }
}
