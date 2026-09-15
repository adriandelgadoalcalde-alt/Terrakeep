using System.IO;
using System.Windows;
using Terrakeep.App;
using Terrakeep.App.ViewModels;

// X1 (I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md, 16-sep-2026): verificacion VISUAL real del aviso
// discreto de version nueva - ver el gancho ACTUALIZACION_SOLO=1 en Program.cs. El mecanismo de
// red (ComprobadorDeActualizaciones, en ServidorKeep.Core) ya se probo aparte con llamadas reales
// contra la API de GitHub (version antigua simulada, version real actual, y sin conexion via un
// bloqueo real de DNS de api.github.com revertido despues) - esto solo comprueba que el estado
// resultante se VE bien en la ventana real, con el tema real de la app.
internal static partial class Program
{
    private static void EjecutarActualizacionReal(MainWindow window, MainViewModel vm)
    {
        DoEvents();

        // Caso 1: version antigua simulada - la tarjeta tiene que aparecer, con su mensaje y sus
        // dos botones (Ocultar / Ver la versión nueva).
        vm.MensajeActualizacion = "Hay una versión nueva de Terrakeep: 99.0.0 (tienes instalada la 3.1.0).";
        vm.HayActualizacionDisponible = true;
        DoEvents();
        WaitForDispatcher(150);
        CapturaVentanaActualizacion(window, "actualizacion-disponible.png");
        Console.WriteLine("ACTUALIZACION: tarjeta visible con HayActualizacionDisponible=true");

        // Caso 2: estado normal (el real de esta version, ya al día) - nada debe verse.
        vm.HayActualizacionDisponible = false;
        DoEvents();
        WaitForDispatcher(150);
        CapturaVentanaActualizacion(window, "actualizacion-sin-aviso.png");
        Console.WriteLine("ACTUALIZACION: sin tarjeta con HayActualizacionDisponible=false (estado normal)");
    }

    private static void CapturaVentanaActualizacion(Window window, string nombre)
    {
        try
        {
            DoEvents();
            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(window);
            var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            string destino = Path.Combine(AppContext.BaseDirectory, nombre);
            using var fs = File.Create(destino);
            enc.Save(fs);
            Console.WriteLine("CAPTURA: " + destino);
        }
        catch (Exception ex)
        {
            Console.WriteLine("CAPTURA-EXCEPTION: " + ex.Message);
        }
    }
}
