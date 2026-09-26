using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Terrakeep.App;

public partial class App : Application
{
    // GetSystemMetrics(SM_REMOTESESSION) - señal real y documentada de Win32 para "esta
    // sesion es una sesion remota de Terminal Services/Escritorio Remoto" (no un simple
    // registro/entorno adivinado). https://learn.microsoft.com/windows/win32/termserv/
    // detecting-the-terminal-services-environment
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
    private const int SM_REMOTESESSION = 0x1000;

    // Red de seguridad global: sin esto, cualquier excepcion no capturada en el hilo de UI
    // (p.ej. un fallo de activacion de binding XAML, como el crash real de la pestaña
    // Novedades del 1-sep-2026 - Run.Text es TwoWay por defecto y DisplayText es de solo
    // lectura) cierra el programa entero sin explicacion visible para quien lo esta usando.
    // Ahora se muestra el error real y se deja seguir usando la app en vez de cerrarla, y se
    // deja tambien un volcado en disco para poder mandarlo/leerlo despues.
    protected override void OnStartup(StartupEventArgs e)
    {
        // Bug real encontrado 2-sep-2026 ("terrakeep sale en blanco en remoto"): WPF usa
        // renderizado por hardware/DirectX de serie, que puede salir en blanco/negro cuando
        // la ventana se ve a traves de Escritorio Remoto/Chrome Remote Desktop (la API de
        // captura de pantalla remota no siempre captura bien la superficie compuesta por
        // DirectX de una app WPF, aunque el resto del escritorio se vea normal). Forzar
        // software SIEMPRE arreglaba ese caso, pero penalizaba el caso normal (uso local, la
        // inmensa mayoria del tiempo) con un renderizado mas lento sin ninguna necesidad real
        // - contra P5 de la auditoria de Opus (T-12: "reactivo de verdad", no de sobra donde
        // no hace falta).
        //
        // Auditoria de Opus, Bloque 3 (T-12): se detecta de verdad una sesion de Escritorio
        // Remoto (RDP/Terminal Services) via GetSystemMetrics(SM_REMOTESESSION) - señal real
        // de Windows, no adivinada - y SOLO ahi se fuerza software, automatico, sin tocar
        // nada. LIMITE HONESTO (documentado, no escondido): Chrome Remote Desktop y
        // herramientas similares (AnyDesk, compartir pantalla de Zoom/Discord/OBS) NO son una
        // sesion RDP real - capturan el escritorio local por otra via (captura de pantalla,
        // no el pipe de Terminal Services), y no existe ninguna API universal de Windows para
        // detectar "mi ventana esta siendo capturada ahora por una herramienta externa
        // cualquiera" - cualquier deteccion para ese caso seria adivinar, no algo real. Como
        // escape real para ese caso (el que de hecho disparo el bug original), variable de
        // entorno documentada: TERRAKEEP_FORCE_SOFTWARE_RENDER=1 fuerza software sin
        // necesidad de recompilar, para cuando SI haga falta.
        if (ShouldForceSoftwareRendering())
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

        // Bug2 (TASK CONTEXT e5eaea9e-c261-4199-8e7d-060b6054f58d, investigador-bug a6b00cdb):
        // contrato real acordado con ServidorKeep (ver INTEGRACIONES-KEEP.json, id
        // "servidorkeep-terrakeep-abrir-editor") - "Abrir en Terrakeep" desde una instancia de
        // servidor lanza este .exe con "--abrir-mundo <ruta.wld>". Se parsea AQUI, antes de que
        // MainWindow exista, para que quede listo cuando su constructor lo consuma (mismo camino
        // real que ya usan OnLoadWorldClick/OnWindowDrop, ver MainWindow.xaml.cs). Nunca lanza:
        // sin argumento, con el flag mal formado o con una ruta que no existe de verdad, se queda
        // en null y Terrakeep arranca NORMAL, sin mundo cargado - igual que si se hubiera lanzado
        // a secas.
        PendingWorldPath = ParsePendingWorldPath(e.Args);

        base.OnStartup(e);
    }

    /// <summary>Ruta real de un <c>.wld</c> ya existente que <see cref="MainWindow"/> debe cargar
    /// automaticamente al arrancar (Bug2 - ver el comentario real de <see cref="OnStartup"/>), o
    /// <c>null</c> si no se paso ningun argumento valido. Publica y con setter privado para poder
    /// leerla desde MainWindow sin exponer un setter externo.</summary>
    public static string? PendingWorldPath { get; private set; }

    // Extraido como metodo propio (mismo criterio que ShouldForceSoftwareRendering, unas lineas
    // mas abajo) para poder verificarlo de verdad desde el arnes sin pasar por OnStartup real.
    // Publico por el mismo motivo (Terrakeep.App.Tests, sin InternalsVisibleTo configurado).
    public static string? ParsePendingWorldPath(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] != "--abrir-mundo") continue;
            string ruta = args[i + 1];
            try
            {
                if (string.Equals(Path.GetExtension(ruta), ".wld", StringComparison.OrdinalIgnoreCase) && File.Exists(ruta))
                    return ruta;
            }
            catch { /* ruta con caracteres invalidos u otro fallo real de E/S - arranca normal */ }
            return null;
        }
        return null;
    }

    // Extraido como metodo propio (en vez de dejarlo inline en OnStartup) para poder
    // verificarlo de verdad desde el arnes de pruebas - OnStartup nunca se ejecuta ahi (el
    // arnes crea un System.Windows.Application a pelo, sin pasar por App.xaml.cs). Publico
    // porque no hay InternalsVisibleTo configurado hacia el arnes (Terrakeep.App.Tests -
    // H3-16, tercera auditoria de Opus, Fable: proyecto real y permanente del propio repo desde
    // T-21, ya no vive en el scratchpad de la sesion).
    public static bool ShouldForceSoftwareRendering() =>
        GetSystemMetrics(SM_REMOTESESSION) != 0 ||
        Environment.GetEnvironmentVariable("TERRAKEEP_FORCE_SOFTWARE_RENDER") == "1";

    // Deuda real detectada el 17-sep-2026 (KeepQA, snapshot visual de Starvekeep): una llamada de
    // RED real (comprobar version nueva contra GitHub) puede resolver en un instante no
    // determinista respecto al temporizador de una captura, colando la tarjeta "Hay una version
    // nueva..." en capturas que no la estaban probando a proposito - visto de verdad en
    // Starvekeep (3/3 capturas contaminadas), mismo mecanismo exacto aqui porque
    // MainWindow.xaml.cs dispara la misma llamada real (ver IniciarComprobacionDeActualizacion).
    //
    // Mismo patron real que Starvekeep.App.App._modoDiagnostico, adaptado: alli se deriva de
    // "--captura" en OnStartup porque el arnes de Starvekeep reutiliza el propio OnStartup de
    // produccion. Aqui NO se puede derivar asi - Terrakeep.App.Tests nunca pasa por OnStartup
    // (construye su propio System.Windows.Application a pelo, ver el comentario real de
    // Program.cs) - asi que es una propiedad publica que el arnes fija a mano, a proposito, antes
    // de construir la MainWindow real (Program.cs, inicio de Main()): TODA ejecucion de
    // Terrakeep.App.Tests es diagnostico/prueba, nunca produccion real, no hace falta un flag mas
    // fino que ese. En produccion real (Terrakeep.exe con StartupUri) esto se queda en su valor
    // por defecto, false, y la comprobacion de actualizacion sigue disparandose como siempre.
    public static bool ModoDiagnostico { get; set; }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogAndShow(e.Exception);
        e.Handled = true; // sigue viva - solo se rompio una pantalla concreta, no todo el programa
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex) LogAndShow(ex);
    }

    private static void LogAndShow(Exception ex)
    {
        string logPath = Path.Combine(AppContext.BaseDirectory, "ultimo-error.log");
        try { File.WriteAllText(logPath, $"{DateTime.Now}\n{ex}"); } catch { /* no bloquear el aviso por un disco no escribible */ }

        MessageBox.Show(
            Services.LocalizationService.Instance.Format("dlg_unexpected_error_body", ex.GetType().Name, ex.Message, logPath),
            Services.LocalizationService.Instance["dlg_unexpected_error_title"],
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}
