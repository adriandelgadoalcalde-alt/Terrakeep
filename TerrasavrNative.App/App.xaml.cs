using System.IO;
using System.Windows;
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
