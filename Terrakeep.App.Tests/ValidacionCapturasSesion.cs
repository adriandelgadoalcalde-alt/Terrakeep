// CAPTURAS CON LA SESION DE WINDOWS DESCONECTADA (R2-L2 del revisor visual de la FASE C, 28-sep-2026).
//
// Obstaculo real: con la sesion interactiva del usuario DESCONECTADA (qwinsta: sesion 1 "Desc", consola
// fisica en LogonUI), TODO RenderTargetBitmap del proceso devolvia pixeles 0 - incluso un DrawingVisual
// con un rectangulo rojo, sin ventana de por medio. La geometria (Measure/Arrange) seguia bien, asi que
// los canarios daban "0 FALLO" con capturas de evidencia completamente vacias.
//
// Causa (PresentationCore 10.0.12 decompilado con ilspycmd, MediaContext..cctor + CreateChannels):
//   ShouldRenderEvenWhenNoDisplayDevicesAreAvailable = !Environment.UserInteractive
//       ? !CoreAppContextSwitches.ShouldNotRenderInNonInteractiveWindowStation
//       : CoreAppContextSwitches.ShouldRenderEvenWhenNoDisplayDevicesAreAvailable;
//   DUCE.NotifyPolicyChangeForNonInteractiveMode(ese valor, Channel);
// Un proceso de escritorio (UserInteractive=true) sin dispositivos de pantalla validos (sesion
// desconectada) NO renderiza nada salvo que se active el AppContext switch documentado
// "Switch.System.Windows.Media.ShouldRenderEvenWhenNoDisplayDevicesAreAvailable".
//
// Medicion real en esta misma sesion desconectada (exp.exe desechable en el scratchpad,
// SM_REMOTESESSION=1): sin el switch, DrawingVisual/Border sin ventana/VisualBrush/Window real ->
// 0 pixeles no cero en todos; RenderOptions.ProcessRenderMode=SoftwareOnly -> 0 (no ayuda); hilo STA
// aparte -> 0 (no ayuda); CON el switch -> 10000/10000, 16000/16000, 16000/16000, 47520/60000
// (la ventana entera menos el marco no cliente, que RenderTargetBitmap nunca pinta).
//
// Solucion (solo en este arnes, nunca en Terrakeep.App): el switch se fija en
// Terrakeep.App.Tests.csproj (RuntimeHostConfigurationOption -> runtimeconfig.json del arnes), asi
// se aplica antes de la primera linea de Main y afecta a TODAS las capturas de todos los modos.
// TERRAKEEP_ARNES_SIN_RENDER_FORZADO=1 lo desactiva a proposito (negative acceptance: permite
// comprobar que la validacion de abajo detecta de verdad las capturas en blanco).
//
// Blindaje (a): aunque el render vuelva a fallar por cualquier otro motivo, ninguna captura en blanco
// se da por buena. Al arrancar se anota el estado real de la sesion (WTSQuerySessionInformation/
// WTSConnectState, SM_REMOTESESSION) y una sonda de render; al salir (ProcessExit, excepcion no
// capturada) se revisan TODOS los .png escritos durante esta ejecucion bajo la carpeta del arnes y
// bajo cualquier carpeta indicada en una variable de entorno *_EVIDENCIA: si una imagen es de un solo
// color (incluido todo 0) se imprime "INCONCLUSIVE: captura en blanco (<motivo>) <ruta>".
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static partial class Program
{
    internal const string SwitchRenderSinPantallas = "Switch.System.Windows.Media.ShouldRenderEvenWhenNoDisplayDevicesAreAvailable";

    private static DateTime _inicioEjecucionUtc;
    private static string _estadoSesion = "desconocido";
    private static string? _motivoRenderEnBlanco;
    private static int _capturasValidadas; // 0/1, Interlocked: la revision solo corre una vez

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQuerySessionInformationW(IntPtr hServer, int sessionId, int infoClass, out IntPtr buffer, out int bytesReturned);

    [DllImport("wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr memory);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    private static readonly string[] NombresWtsConnectState =
        ["WTSActive", "WTSConnected", "WTSConnectQuery", "WTSShadow", "WTSDisconnected", "WTSIdle", "WTSListen", "WTSReset", "WTSDown", "WTSInit"];

    /// <summary>Lo primero de Main: estado real de la sesion + sonda de render. Tiene que ir antes de
    /// cualquier uso de WPF para que el SetSwitch(false) del modo negativo llegue antes que el
    /// constructor estatico de MediaContext.</summary>
    private static void PrepararValidacionCapturas()
    {
        _inicioEjecucionUtc = DateTime.UtcNow.AddSeconds(-2);
        if (Environment.GetEnvironmentVariable("TERRAKEEP_ARNES_SIN_RENDER_FORZADO") == "1")
            AppContext.SetSwitch(SwitchRenderSinPantallas, false);
        AppContext.TryGetSwitch(SwitchRenderSinPantallas, out bool renderForzado);

        string connect = "?";
        const int WTSConnectState = 8;
        if (WTSQuerySessionInformationW(IntPtr.Zero, -1, WTSConnectState, out IntPtr buf, out int bytes) && buf != IntPtr.Zero)
        {
            try
            {
                int v = bytes >= 4 ? Marshal.ReadInt32(buf) : -1;
                connect = v >= 0 && v < NombresWtsConnectState.Length ? NombresWtsConnectState[v] : v.ToString();
            }
            finally { WTSFreeMemory(buf); }
        }
        int remota = GetSystemMetrics(0x1000); // SM_REMOTESESSION
        int monitores = GetSystemMetrics(80);  // SM_CMONITORS
        _estadoSesion = $"sesion {System.Diagnostics.Process.GetCurrentProcess().SessionId} WTSConnectState={connect} SM_REMOTESESSION={remota} SM_CMONITORS={monitores} UserInteractive={Environment.UserInteractive}";
        Console.WriteLine($"SESION-ESTADO: {_estadoSesion}; {SwitchRenderSinPantallas}={renderForzado}");

        // Sonda: un rectangulo opaco de 8x8. Si sale todo 0, cualquier captura de esta ejecucion
        // saldra igual de vacia - se sabe ANTES de gastar minutos en el canario.
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen()) dc.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 8, 8));
        var rtb = new RenderTargetBitmap(8, 8, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);
        var px = new byte[8 * 8 * 4];
        rtb.CopyPixels(px, 8 * 4, 0);
        int noCero = 0;
        for (int i = 0; i < 64; i++) if (px[i * 4] != 0 || px[i * 4 + 1] != 0 || px[i * 4 + 2] != 0 || px[i * 4 + 3] != 0) noCero++;
        if (noCero == 0)
        {
            _motivoRenderEnBlanco = $"WPF no renderiza en este proceso: {_estadoSesion}, {SwitchRenderSinPantallas}={renderForzado}";
            Console.WriteLine($"RENDER-SONDA: INCONCLUSIVE - 0/64 pixeles pintados ({_motivoRenderEnBlanco}); toda captura de esta ejecucion saldra en blanco");
        }
        else Console.WriteLine($"RENDER-SONDA: OK - {noCero}/64 pixeles pintados");

        AppDomain.CurrentDomain.ProcessExit += (_, _) => ValidarCapturasDeEstaEjecucion();
    }

    /// <summary>Revisa una sola vez todos los .png escritos durante esta ejecucion. Llamado desde
    /// ProcessExit y desde el manejador de excepcion no capturada.</summary>
    internal static void ValidarCapturasDeEstaEjecucion()
    {
        if (Interlocked.Exchange(ref _capturasValidadas, 1) == 1) return;
        try
        {
            var carpetas = new List<string> { AppContext.BaseDirectory };
            foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
                if (e.Key is string k && k.EndsWith("_EVIDENCIA", StringComparison.OrdinalIgnoreCase) && e.Value is string d && Directory.Exists(d))
                    carpetas.Add(d);
            var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int revisadas = 0, blancas = 0;
            foreach (string carpeta in carpetas)
                foreach (string png in Directory.EnumerateFiles(carpeta, "*.png", SearchOption.AllDirectories))
                {
                    if (!vistas.Add(Path.GetFullPath(png))) continue;
                    if (File.GetLastWriteTimeUtc(png) < _inicioEjecucionUtc) continue;
                    revisadas++;
                    string? motivo = MotivoCapturaEnBlanco(png);
                    if (motivo == null) continue;
                    blancas++;
                    Console.WriteLine($"INCONCLUSIVE: captura en blanco ({motivo}) {png}");
                }
            Console.WriteLine($"CAPTURAS-VALIDACION: {revisadas} captura(s) de esta ejecucion revisadas, {revisadas - blancas} con contenido real, {blancas} INCONCLUSIVE (en blanco)");
        }
        catch (Exception ex) { Console.WriteLine("CAPTURAS-VALIDACION: INCONCLUSIVE - no se pudieron revisar las capturas: " + ex.Message); }
    }

    /// <summary>null si la imagen tiene contenido real; si es de un solo color (todo 0 incluido),
    /// el motivo a imprimir.</summary>
    internal static string? MotivoCapturaEnBlanco(string rutaPng)
    {
        BitmapSource frame;
        using (var fs = File.OpenRead(rutaPng))
            frame = new PngBitmapDecoder(fs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
        var bgra = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        int w = bgra.PixelWidth, h = bgra.PixelHeight;
        var px = new byte[w * h * 4];
        bgra.CopyPixels(px, w * 4, 0);
        uint primero = BitConverter.ToUInt32(px, 0);
        for (int i = 4; i < px.Length; i += 4)
            if (BitConverter.ToUInt32(px, i) != primero) return null;
        string color = primero == 0 ? "todos los pixeles a 0" : $"un solo color 0x{primero:X8}";
        return _motivoRenderEnBlanco != null ? $"{color}; {_motivoRenderEnBlanco}" : $"{color}; {_estadoSesion}";
    }
}
