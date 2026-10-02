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
// NOMBRE real se vea en pantalla). CORRECCION real (29-sep-2026, aviso del coordinador tras abrir
// 03-exploracion.png a mano): una version anterior de este comentario decia que seguir usando
// 'roca_negra.wld' real era aceptable porque "su contenido no identifica al usuario" - FALSO, el
// propio nombre del mundo ('roca negra') se veia literal en la barra superior/"Mundo: X"/"Tus
// mundos"/pie de mapa, y sigue siendo una partida real. Ahora el mundo tambien es de PRUEBA:
// TerrakeepPrueba.wld, el fixture real del propio arnes QA (Documents\My Games\Terraria\
// tModLoader-KeepQA\Worlds\), con Title interno tambien "TerrakeepPrueba" - SIEMPRE sobre la
// copia YA aislada por AislamientoPartidasReales.cs, nunca el original de Documentos.
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
            // Aviso real del coordinador (29-sep-2026, tras abrir 01-inicio.png/02-personaje.png a
            // mano): franjas blancas de ~40px abajo y ~16px a la derecha. Mismo bug real ya
            // diagnosticado y arreglado en Starvekeep (App.xaml.cs, RecortarAlContenidoReal): el
            // RenderTargetBitmap mide la VENTANA entera (ActualWidth/ActualHeight), pero el arbol
            // visual real (Grid.Margin, bordes con sombra, elementos que se miden a su propio
            // contenido) no siempre pinta hasta el borde exacto - la franja sin pintar queda con
            // alfa=0, y la mayoria de visores de imagen componen ese alfa 0 sobre blanco. Arreglo
            // real: recortar al rectangulo REALMENTE pintado (alfa>0), calculado pixel a pixel
            // sobre lo que se acaba de renderizar - nunca un margen fijo adivinado a mano.
            var recortada = RecortarAlContenidoReal(rtb);
            var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(recortada));
            string destino = Path.Combine(dirDocs, nombreArchivo);
            using (var fs = File.Create(destino)) enc.Save(fs);
            Console.WriteLine($"README_SHOTS: {nombreArchivo} <- {window.ActualWidth:0}x{window.ActualHeight:0} (lienzo) -> {recortada.PixelWidth}x{recortada.PixelHeight} (recortado al contenido real), {new FileInfo(destino).Length} bytes");
        }

        vm.Settings.Language = "es";
        FijarTamaño(window, 1920, 1080);

        // Personaje sintetico presentable ("Aventurero" - nunca el nombre del usuario real).
        // Correccion real (29-sep-2026, aviso del coordinador tras abrir 02-personaje.png a mano):
        // la primera version confundia dos arrays distintos de PlrCharacter. Investigado de verdad
        // contra el codigo real antes de tocar nada (EquipmentGroupViewModel.cs, MainViewModel.cs
        // RebuildContainers, DefenseCalculator.cs):
        //   - El panel "Armadura" (Cabeza/Cuerpo/Piernas + 7 accesorios) sale de
        //     PrimaryLoadout.Items[0..9] - Items[0]=Cabeza, [1]=Cuerpo, [2]=Piernas, [3..9]=accesorios
        //     (MainViewModel.cs, RebuildContainers, contenedor "loadout0Items"). Ahi es donde iba la
        //     Terra Blade en la version anterior (Items[0], el hueco de casco) - por eso aparecia
        //     ahi en vez de en el inventario, y la armadura Hallowed no aparecia en ningun sitio.
        //   - EquipmentItems (el array de 5 que se uso antes para "armadura") NO es armadura: es
        //     miscEquips - Mascota/Mascota de luz/Vagoneta/Montura/Gancho (MainViewModel.cs,
        //     comentario real de MountsContainer) - ahi fueron a parar las piezas Hallowed la vez
        //     anterior, como iconos irreconocibles en la columna "Mascota/Montura".
        //   - DefenseCalculator.Total() suma la Defense real de VanillaItemStatsCatalog sobre
        //     Items[0..2]+accesorios - por eso "Defensa total" salia 0 (esos slots no tenian
        //     armadura de verdad, solo la espada en el hueco de casco).
        // Ahora: Hallowed completo (551/552/553) en Items[0..2], 3 accesorios reales en Items[3..5]
        // (Angel Wings/Hermes Boots/Band of Regeneration), Terra Blade (757) SOLO en el inventario
        // (hotbar, slot 0) - nunca en un hueco de armadura. Vida/mana ACTUALES = maximo (antes
        // quedaban en 0/400 y 0/200 por no fijar HealthNow/ManaNow). IDs vanilla confirmados contra
        // Terrakeep.App/Assets/vanilla_item_names_en.json (catalogo real del propio repo, nunca
        // inventados). Version=279 (misma linea base que el resto del arnes).
        var personaje = new PlrCharacter
        {
            Version = 279,
            Name = "Aventurero",
            Difficulty = 0,
            PlayTimeLow = 5_400_000, // ~1h30 a 60 ticks/s, solo para que no salga "0h" en la tarjeta
            HealthMax = 400,
            HealthNow = 400,
            ManaMax = 200,
            ManaNow = 200,
            HairColor = [90, 60, 35],
            SkinColor = [255, 200, 165],
            EyeColor = [105, 90, 75],
            ShirtColor = [175, 165, 140],
            UnderColor = [85, 85, 180],
            PantsColor = [170, 140, 90],
            ShoesColor = [130, 90, 60],
            EquipmentItems = new PlrItemSlot[5], // miscEquips (mascota/montura/gancho) - vacio a proposito, no es armadura
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

        // Aviso real del coordinador (29-sep-2026, tras abrir 03-exploracion.png a mano): el
        // mundo real 'roca_negra' se veia en la barra superior, "Mundo: roca negra" y "Tus
        // mundos" - la linea 23 de un comentario anterior DECIA que el mundo "no identifica al
        // usuario", pero un mundo real SIGUE siendo una partida real y la regla de la familia
        // Keep para capturas de README es igual de tajante que con los personajes: "SOLO mundos
        // de PRUEBA". Opcion (a) del coordinador: TerrakeepPrueba.wld, el mundo real de fixture
        // del propio arnes QA (Documents\My Games\Terraria\tModLoader-KeepQA\Worlds\), con Title
        // interno tambien "TerrakeepPrueba" - nunca un nombre real del usuario, en NINGUN sitio
        // de la pantalla (barra superior/"Mundo: X"/"Tus mundos"/pie de mapa), porque el propio
        // campo del archivo ya dice eso.
        string origenWld = MundoAislado(RutasEntornoReal.Documentos(@"tModLoader-KeepQA\Worlds\TerrakeepPrueba.wld"));

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

            // Copia el mundo de PRUEBA a la carpeta aislada ANTES del refresco, para que
            // RefreshWorldsCommand lo detecte y "Tus mundos" muestre 'TerrakeepPrueba' en vez de
            // quedar vacio (dirMundos aun no tenia ningun .wld dentro en el primer intento de
            // este mismo saneado).
            if (File.Exists(origenWld))
            {
                copiaWld = Path.Combine(dirMundos, "TerrakeepPrueba.wld");
                File.Copy(origenWld, copiaWld, overwrite: true);
                string origenTwld = Path.ChangeExtension(origenWld, ".twld");
                if (File.Exists(origenTwld)) File.Copy(origenTwld, Path.ChangeExtension(copiaWld, ".twld"), overwrite: true);
            }

            // Mismo bug que Home.Characters (ver mas arriba): Exploration.Worlds ("Tus mundos")
            // tambien se escaneo UNA VEZ contra las carpetas reales antes de este metodo - sin
            // refrescarla mostraba los mundos reales de esta maquina (Blando Río, adriandres,
            // roca negra...) en la barra de "Tus mundos", aunque el mapa cargado ya fuera la
            // copia aislada del mundo de prueba. Bug real encontrado revisando el PNG generado en
            // esta misma ronda de saneado.
            var refrescoMundos = vm.Exploration.RefreshWorldsCommand.ExecuteAsync(null);
            while (!refrescoMundos.IsCompleted) DoEvents();
            DoEvents();
            Console.WriteLine($"README_SHOTS: Exploration.Worlds tras el refresco aislado = {vm.Exploration.Worlds.Count} (esperado 1, solo 'TerrakeepPrueba')");

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
                WaitForDispatcher(300); // mismo margen real que 02/04 - primera visita a la pestaña en la sesion
                Capturar("03-exploracion.png");

                // 05-guia.png (NUEVA - README no mostraba esta pestaña, integrada esta misma
                // noche): objetivo actual + arbol de tramos evaluados de verdad contra el
                // personaje y el mundo ya cargados (personaje vanilla puro - MostrarAvisoCalamity
                // esperado False, no hay ningun aviso de Calamity que mostrar).
                vm.SelectedTabIndex = 3; // Guia - AppTab.Guia, reordenado T1 21-sep-2026
                DoEvents();
                // Guia v2 (3.4.0, 02-oct-2026): la pestaña Guia es ya la guia grande nueva
                // (GuiaV2View). 05-guia.png = Ruta abierta en la siguiente parada de 'Aventurero'
                // (la que propone "Continuar"), evaluada contra el personaje y el mundo de prueba;
                // 07-guia-mi-guia.png = "Mi guía" (siguiente parada, progreso y avisos del modo).
                vm.GuiaV2.Refresh();
                vm.GuiaV2.ContinuarRutaCommand.Execute(null);
                WaitForDispatcher(400); // primera visita a la pestaña en la sesion + ubicacion en el mapa
                Console.WriteLine($"README_SHOTS: Guia v2 -> guia={vm.GuiaV2.GuiaId}, paradas={vm.GuiaV2.Paradas.Count}, seleccionada={vm.GuiaV2.ParadaSeleccionada?.Id}");
                Capturar("05-guia.png");
                vm.GuiaV2.Seccion = SeccionGuiaV2.MiGuia;
                WaitForDispatcher(300);
                Capturar("07-guia-mi-guia.png");
            }

            // 04-about-settings-en.png: pestaña "Acerca de" en ingles (mismo criterio que la
            // captura ya existente - la version/changelog/autoria reales, en el segundo idioma).
            // Panel en blanco real (aviso del coordinador, mundo de prueba nuevo - primera visita
            // a esta pestaña en la sesion, mismo patron ya visto y arreglado en 02-personaje.png):
            // WaitForDispatcher(300) da tiempo real al layout antes de capturar.
            vm.Settings.Language = "en";
            vm.SelectedTabIndex = 7; // Acerca de - AppTab.AcercaDe, reordenado T1 21-sep-2026
            WaitForDispatcher(300);
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

    // Recorta un RenderTargetBitmap al rectangulo real que tiene algo pintado (alfa > 0), quitando
    // cualquier franja sin pintar que haya quedado en los bordes - ver el comentario real de
    // Capturar() para el porque. Adaptado de Starvekeep.App/App.xaml.cs
    // (RecortarAlContenidoReal, mismo bug real ya diagnosticado y arreglado ahi el 29-sep-2026).
    // Nunca recorta un pixel con contenido de verdad: solo el margen exterior totalmente
    // transparente. Si no hay ningun margen asi, devuelve la misma imagen sin tocar.
    private static System.Windows.Media.Imaging.BitmapSource RecortarAlContenidoReal(System.Windows.Media.Imaging.RenderTargetBitmap mapa)
    {
        int ancho = mapa.PixelWidth;
        int alto = mapa.PixelHeight;
        int bytesPorFila = ancho * 4;
        var pixeles = new byte[bytesPorFila * alto];
        mapa.CopyPixels(pixeles, bytesPorFila, 0);

        int minX = ancho, minY = alto, maxX = -1, maxY = -1;
        for (int y = 0; y < alto; y++)
        {
            int filaBase = y * bytesPorFila;
            for (int x = 0; x < ancho; x++)
            {
                // Formato Pbgra32: byte 3 de cada pixel es el alfa (B,G,R,A).
                if (pixeles[filaBase + x * 4 + 3] == 0) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX || maxY < minY) return mapa; // nada pintado - red de seguridad, no deberia pasar

        int anchoReal = maxX - minX + 1;
        int altoReal = maxY - minY + 1;
        if (minX == 0 && minY == 0 && anchoReal == ancho && altoReal == alto) return mapa; // ya llena el lienzo entero

        return new System.Windows.Media.Imaging.CroppedBitmap(mapa, new System.Windows.Int32Rect(minX, minY, anchoReal, altoReal));
    }

    // Inventario sintetico presentable para "Aventurero" (README_SHOTS): Terra Blade en la hotbar
    // (slot 0, nunca en un hueco de armadura - ver el comentario real de mas arriba) mas
    // pico/hacha de hierro y unas cuantas pociones/materiales variados para que el inventario no
    // se vea vacio. IDs vanilla confirmados contra vanilla_item_names_en.json.
    private static PlrItemSlot[] CrearInventarioPresentable()
    {
        var inventario = new PlrItemSlot[50];
        Array.Fill(inventario, PlrItemSlot.Empty);
        inventario[0] = new PlrItemSlot(757, 1, 0, false);   // Terra Blade
        inventario[1] = new PlrItemSlot(1, 1, 0, false);     // Iron Pickaxe
        inventario[2] = new PlrItemSlot(10, 1, 0, false);    // Iron Axe
        inventario[3] = new PlrItemSlot(84, 1, 0, false);    // Grappling Hook
        inventario[4] = new PlrItemSlot(28, 20, 0, false);   // Lesser Healing Potion
        inventario[10] = new PlrItemSlot(8, 100, 0, false);  // Torch
        inventario[11] = new PlrItemSlot(9, 50, 0, false);   // Wood
        return inventario;
    }

    // Loadout principal sintetico para "Aventurero": armadura Hallowed real en los 3 huecos de
    // verdad (Items[0]=Cabeza, [1]=Cuerpo, [2]=Piernas - confirmado contra MainViewModel.
    // RebuildContainers/EquipmentGroupViewModel, nunca un hueco cualquiera a ciegas) + 3
    // accesorios reales en Items[3..5]. DefenseCalculator suma la Defense real del catalogo
    // vanilla sobre estos mismos slots, asi que "Defensa total" sale calculada de verdad, no a 0.
    // PlrLoadout.CreateEmpty(isPrimary: true) ya deja Hide=null, como corresponde al loadout
    // "mirror" activo (loadouts[0]).
    private static PlrLoadout CrearLoadoutPrincipal()
    {
        var loadout = PlrLoadout.CreateEmpty(isPrimary: true);
        loadout.Items[0] = new PlrItemSlot(553, 1, 0, false); // Hallowed Helmet (Cabeza)
        loadout.Items[1] = new PlrItemSlot(551, 1, 0, false); // Hallowed Plate Mail (Cuerpo)
        loadout.Items[2] = new PlrItemSlot(552, 1, 0, false); // Hallowed Greaves (Piernas)
        loadout.Items[3] = new PlrItemSlot(493, 1, 0, false); // Angel Wings (accesorio)
        loadout.Items[4] = new PlrItemSlot(54, 1, 0, false);  // Hermes Boots (accesorio)
        loadout.Items[5] = new PlrItemSlot(49, 1, 0, false);  // Band of Regeneration (accesorio)
        return loadout;
    }
}
