using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace TerrasavrNative.App;

public partial class App : Application
{
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
        // captura de pantalla remota - DXGI Desktop Duplication - no siempre captura bien la
        // superficie compuesta por DirectX de una app WPF, aunque el resto del escritorio se
        // vea normal). Forzar renderizado por software (mismo pixel final, solo cambia como
        // se dibuja) es el arreglo real y documentado para este caso - hay que ponerlo ANTES
        // de que se cree cualquier ventana, por eso va lo primero de OnStartup.
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        base.OnStartup(e);
    }

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
            $"Terrakeep encontro un error inesperado y esta pantalla puede no funcionar bien.\n\n" +
            $"{ex.GetType().Name}: {ex.Message}\n\n" +
            $"Detalle guardado en:\n{logPath}",
            "Error inesperado - Terrakeep",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}
