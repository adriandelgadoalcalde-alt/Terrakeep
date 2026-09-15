using System.IO;
using System.Linq;
using Terrakeep.App;
using Terrakeep.App.ViewModels;
using ServidorKeep.Core.Instancias;

// Cierre de sesion (15-sep-2026, encargo del coordinador): las capturas de docs/screenshots/ que
// usa README.md llevaban desde el 5-sep-2026 (version 2.1.0) - de antes de idioma completo, de
// la Libreria dentro de Personaje rediseñada, del Editor de mundos real y muy de antes de la
// Guia/Servidor de esta noche. Pedido explicito: sustituirlas por capturas REALES (nunca datos
// sinteticos "UIA-Test") con un personaje/mundo real presentable de esta maquina, resolucion
// grande (1920x1080, SizeClass=Extra - una sola linea en la franja de vitales, sin envolver), y
// AÑADIR capturas nuevas de las dos pestañas nuevas de esta noche (Guia/Servidor) que README
// todavia no mostraba en absoluto.
//
// Mismo criterio que GUIA_SOLO/HOSTING_SOLO (PruebasGuiaYServidor.cs): personaje real 'adrian'
// (Calamity real, .tplr real) y mundo real 'roca_negra.wld', SIEMPRE sobre una COPIA en el temp
// del sistema - los originales de Documentos nunca se abren en modo escritura ni se tocan.
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

        string origenPlr = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Players\adrian.plr";
        string origenTplr = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Players\adrian.tplr";
        string origenWld = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld";

        if (!File.Exists(origenPlr))
        {
            Console.WriteLine("README_SHOTS: adrian.plr no esta en esta maquina - se omiten el resto de capturas con datos reales.");
            return;
        }

        // Copia en una carpeta PROPIA del temp (no suelta en la raiz del temp) para poder llamar
        // a los ficheros "adrian.plr"/"adrian.tplr" tal cual (mismo nombre que el original) sin
        // arriesgar colision con otro fichero real - la cabecera de la app muestra el nombre de
        // fichero tal cual ("{0} · versión {1}", header_file_version_line), y un prefijo de arnes
        // ahi ("readme-harness-adrian.plr") se veria feo en una captura pensada para el README.
        string tempDir = Path.Combine(Path.GetTempPath(), "terrakeep-readme-shots");
        Directory.CreateDirectory(tempDir);
        string copiaPlr = Path.Combine(tempDir, "adrian.plr");
        string copiaTplr = Path.Combine(tempDir, "adrian.tplr");
        File.Copy(origenPlr, copiaPlr, overwrite: true);
        if (File.Exists(origenTplr)) File.Copy(origenTplr, copiaTplr, overwrite: true);

        string? copiaWld = null;
        try
        {
            // Se carga el personaje real ANTES de capturar Inicio a proposito (bug real de esta
            // MISMA captura encontrado revisando el PNG generado): session.json (%LOCALAPPDATA%\
            // Terrakeep\, restaurado por MainWindow() ya en el constructor) traia el ULTIMO
            // personaje de una ronda de pruebas anterior de este mismo arnes - la tarjeta
            // destacada de Inicio ("Continuar con...") salia con el nombre sintetico "UIA-Test",
            // justo el tipo de dato de prueba feo que el encargo pedia evitar. Cargando 'adrian'
            // primero, el evento CharacterLoaded real (MainWindow.xaml.cs, H5-07) deja la sesion
            // guardada con un personaje real y presentable antes de que Inicio se capture.
            vm.LoadFromPath(copiaPlr);
            DoEvents();
            Console.WriteLine($"README_SHOTS: personaje real cargado -> HasCalamityData={vm.HasCalamityData} (esperado True)");

            if (File.Exists(origenWld))
            {
                copiaWld = Path.Combine(tempDir, "roca_negra.wld");
                File.Copy(origenWld, copiaWld, overwrite: true);
                var cargaTemprana = vm.Exploration.LoadFromPathAsync(copiaWld);
                while (!cargaTemprana.IsCompleted) DoEvents();
                DoEvents();
            }

            // 01-inicio.png: Inicio con la sesion real ya al dia (tarjeta "Continuar con adrian",
            // tarjeta del personaje marcada como actual, lista real de los 5 personajes de esta
            // maquina). Esperar tambien a que termine el escaneo de disco en segundo plano
            // (Home.IsScanning, fire-and-forget desde el propio constructor, T-G).
            vm.SelectedTabIndex = 0;
            long limiteScan = Environment.TickCount64 + 10_000;
            while (vm.Home.IsScanning && Environment.TickCount64 < limiteScan) DoEvents();
            DoEvents();
            Capturar("01-inicio.png");

            // 02-personaje.png: pestaña Personaje, sub-pestaña Objetos (la que trae la Libreria
            // debajo del inventario desde el 1-sep-2026) - equipo/inventario reales de 'adrian'.
            // PersonajeInnerTabIndex se fija a mano (0=Objetos): session.json puede traer
            // persistida la sub-pestaña de una ronda de pruebas anterior (measurado: se quedaba en
            // Apariencia, 3, una captura bastante menos representativa del uso real de la app).
            vm.SelectedTabIndex = 1;
            vm.PersonajeInnerTabIndex = 0;
            DoEvents(); DoEvents();
            Capturar("02-personaje.png");

            Console.WriteLine($"README_SHOTS: mundo real cargado -> IsWorldLoaded={vm.Exploration.IsWorldLoaded}");
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
                // personaje Calamity y el mundo reales ya cargados.
                vm.SelectedTabIndex = 6;
                DoEvents();
                vm.Guide.Refresh();
                DoEvents(); DoEvents();
                Console.WriteLine($"README_SHOTS: Guia -> Tramos.Count={vm.Guide.Tramos.Count}, MostrarAvisoCalamity={vm.Guide.MostrarAvisoCalamity}");
                Capturar("05-guia.png");
            }

            // 04-about-settings-en.png: pestaña "Acerca de" en ingles (mismo criterio que la
            // captura ya existente - la version/changelog/autoria reales, en el segundo idioma).
            vm.Settings.Language = "en";
            vm.SelectedTabIndex = 5;
            DoEvents(); DoEvents();
            Capturar("04-about-settings-en.png");
            vm.Settings.Language = "es";
            DoEvents();

            // 06-servidor.png (NUEVA): pestaña Servidor - si Terraria esta instalado de verdad en
            // esta maquina (ya confirmado por HOSTING_SOLO en la Fase B de esta misma noche),
            // lanza una instancia REAL corta por el mismo camino que pulsaria un usuario
            // (Hosting.IniciarCommand) y espera a EnEscucha para que la captura muestre el estado
            // "en escucha" real, no solo el formulario vacio - mas presentable y mas honesto.
            vm.SelectedTabIndex = 7;
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
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
