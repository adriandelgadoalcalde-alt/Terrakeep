using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace TerrasavrNative.App;

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
        base.OnStartup(e);
    }

    // Extraido como metodo propio (en vez de dejarlo inline en OnStartup) para poder
    // verificarlo de verdad desde el arnes de pruebas - OnStartup nunca se ejecuta ahi (el
    // arnes crea un System.Windows.Application a pelo, sin pasar por App.xaml.cs). Publico
    // porque no hay InternalsVisibleTo configurado hacia el arnes (TerrasavrNative.App.Tests -
    // H3-16, tercera auditoria de Opus, Fable: proyecto real y permanente del propio repo desde
    // T-21, ya no vive en el scratchpad de la sesion).
    public static bool ShouldForceSoftwareRendering() =>
        GetSystemMetrics(SM_REMOTESESSION) != 0 ||
        Environment.GetEnvironmentVariable("TERRAKEEP_FORCE_SOFTWARE_RENDER") == "1";

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
