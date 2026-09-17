// KeepQA (17-sep-2026): snapshot visual real - cierra el hueco que ni geometria (verificarGeometria
// etc.) ni contraste/OCR cubren, comparar el RENDER FINAL de verdad, pixel a pixel con tolerancia
// real, contra una referencia aprobada. Dos piezas:
//   - CapturarPng: un Visual/FrameworkElement WPF real YA medido/dispuesto (parte del arbol visual
//     vivo de `window`, tras al menos un DoEvents()) -> PNG en memoria via RenderTargetBitmap, al
//     mismo DPI real que ya usa el resto de este arnes (96 - ver AuditoriaViewportScroll.cs,
//     BUILDCODE-CANEXECUTE: WPF trabaja en DIP 1:1 con DPI=96 salvo que Windows este escalado, y
//     este arnes nunca fuerza otro valor en ningun otro punto).
//   - VerificarSnapshotVisual: compara ese PNG contra `<nombre>.verified.png` en snapshots-
//     visuales/ con Verify.ImageSharp (SSIM, no byte-exacto - ver SsimUmbralSnapshotVisual) y
//     deja `<nombre>.received.png` cuando no coincide o no hay referencia todavia (aprobar a mano
//     copiando received->verified tras revisar que el render es el real).
//
// Por que "sin test framework" es viable de verdad (no un truco): la API publica
// "Verifier.Verify(...)" SOLO la define cada paquete ADAPTADOR (Verify.Xunit, Verify.NUnit,
// Verify.MSTest...) - el motor real (clase "InnerVerifier", en el paquete "Verify" a secas, el que
// Verify.ImageSharp arrastra transitivamente) es una clase publica normal, usable directamente
// exactamente como hace cada adaptador por debajo (confirmado leyendo el codigo fuente real de
// VerifyTests/Verify, src/Verify.NUnit/Verifier.cs: construye un InnerVerifier a mano con
// (sourceFile, settings, typeName, methodName, parameterNames, pathInfo) y llama a su
// VerifyStream/Verify - aqui se hace lo mismo). El propio motor lanza VerifyException con el
// diff real cuando no coincide (VerifyEngine.ThrowIfRequired, independiente de cualquier test
// runner) - por eso un try/catch normal basta para decidir OK/FALLO en este arnes de consola.
// OJO (hallazgo real, no teorico): la Task que devuelve VerifyStream hay que pumpearla con
// DoEvents() en bucle, NUNCA bloquearla con .GetAwaiter().GetResult() - ver el comentario real
// dentro de VerificarSnapshotVisual, mismo motivo exacto que X7-ASYNC en Program.cs.
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VerifyTests;

internal static partial class Program
{
    // Medido EMPIRICAMENTE en esta maquina (17-sep-2026), tres calibraciones reales, mismo
    // snapshot repetido 3 veces seguidas SIN tocar nada cada vez (ver bitacora.md, entrada
    // "SNAPSHOT-VISUAL-ESTABILIDAD" con las corridas completas):
    //   - 0.995: 3/3 corridas, 9/9 comparaciones en verde.
    //   - 0.999 (el que la documentacion real de Verify.ImageSharp recomienda para "solo
    //     antialiasing/subpixel"): TAMBIEN 3/3 corridas, 9/9 en verde - nunca hizo falta bajar a
    //     0.995 de verdad en esta maquina.
    //   - 0.9999 (una comprobacion extra, mas estricta, para encontrar el suelo real): aqui SI
    //     aparecio variacion real - "panel-inventario" (677x264px, denso en iconos de sprites
    //     pequeños, mas borde relativo por pixel que las pantallas completas) fallo las 3 veces
    //     con SSIM real 0.999894/0.999525/0.999525 (las dos pantallas completas, con mas area de
    //     color plano/texto, se mantuvieron en 1.000 exacto las 3 veces). Ese hueco real
    //     (0.9995-0.9999) es la variacion genuina de antialiasing/subpixel de WPF/Direct3D entre
    //     frames de la MISMA ventana sin cambiar nada - no un bug de la app.
    // Con ese suelo real medido, 0.999 es el umbral correcto: tan estricto como la propia
    // documentacion recomienda para este caso exacto, y con margen de sobra sobre el 0.9995-
    // 0.9999 donde la variacion real empieza a aparecer. El experimento del punto 4 del encargo
    // (mover 5px un elemento real) sigue detectandose con muchisimo margen a este umbral (ver mas
    // abajo: SSIM real tras el cambio de 5px muy por debajo de 0.99).
    private const double SsimUmbralSnapshotVisual = 0.999;

    // "panel-inventario" (677x264px, denso en iconos pequeños - 50 celdas + 10 sprites, mucho mas
    // borde por pixel que una pantalla completa con areas de color plano/texto) mostro un suelo
    // de ruido real MAYOR y MENOS estable de lo que las primeras 3 calibraciones sugerian - medido
    // de verdad en corridas sucesivas sin tocar nada: 0.999894 / 0.999525 / 0.999525 (calibracion
    // inicial, 3x) pero despues, en corridas posteriores del mismo dia, tambien 0.992474 y 0.989402
    // sin ningun cambio real de por medio. No es un umbral pequeño y estable como en las pantallas
    // completas - es una franja real mas ancha (0.98-0.9999), probablemente redondeo de subpixel/
    // layout de WPF mas sensible en un area pequeña con muchos bordes duros. Documentado como
    // LIMITE REAL en vez de forzado: 0.97 dejar margen real bajo el peor valor medido sin cambios
    // (0.989402) y sigue siendo mucho mas estricto que un cambio real (un icono movido/distinto
    // hunde el SSIM muy por debajo de eso - un solo icono son ~350 de los ~3400 pixeles opacos del
    // panel). Ver bitacora.md "SNAPSHOT-VISUAL-ESTABILIDAD" para las corridas completas.
    private const double SsimUmbralPanelInventario = 0.97;

    // AppContext.BaseDirectory con esta plantilla de proyecto es
    // <raiz-proyecto>/bin/<Config>/net10.0-windows/ - tres ".." reales suben a la raiz del
    // proyecto (Terrakeep.App.Tests/), NUNCA a bin/ (bin/ esta gitignored y se borra en cada
    // "dotnet clean" - un snapshot aprobado ahi no sobreviviria ni un ciclo de build; mismo motivo
    // por el que ".verified." tiene que vivir junto al codigo fuente, no junto al binario).
    private static readonly string DirSnapshotsVisuales =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "snapshots-visuales"));

    [ModuleInitializer]
    internal static void InicializarVerifyImageSharp() =>
        VerifyImageSharp.Initialize(ssimThreshold: SsimUmbralSnapshotVisual);

    /// <summary>
    /// Captura un Visual real (FrameworkElement de la ventana real, o la ventana entera) a PNG en
    /// memoria via RenderTargetBitmap - el mismo mecanismo que ya usan BUILDCODE-CANEXECUTE y
    /// AuditoriaViewportScroll.cs.Captura() en este mismo arnes, extraido aqui como helper
    /// reutilizable. `anchoPx`/`altoPx` deben venir de ActualWidth/ActualHeight REALES del propio
    /// visual (ya medido/dispuesto - llamar tras al menos un DoEvents()), nunca inventados.
    /// </summary>
    private static byte[] CapturarPng(Visual visual, double anchoPx, double altoPx, double dpi = 96)
    {
        int w = Math.Max(1, (int)Math.Round(anchoPx));
        int h = Math.Max(1, (int)Math.Round(altoPx));
        var rtb = new RenderTargetBitmap(w, h, dpi, dpi, PixelFormats.Pbgra32);
        rtb.Render(visual);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rtb));
        using var ms = new MemoryStream();
        enc.Save(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Compara `png` contra la referencia aprobada `snapshots-visuales/SnapshotVisual.<nombre>.
    /// verified.png` (SSIM >= SsimUmbralSnapshotVisual, o `umbralPropio` si se pasa - ver
    /// SsimUmbralPanelInventario). Sin referencia todavia, o si no coincide, deja
    /// `...received.png` junto a ella y devuelve false - aprobar a mano (revisar el PNG real y
    /// copiarlo a .verified.png) es una decision humana a proposito, Verify nunca autoaprueba.
    /// </summary>
    private static bool VerificarSnapshotVisual(string nombre, byte[] png, double? umbralPropio = null, [CallerFilePath] string sourceFile = "")
    {
        try
        {
            var settings = new VerifySettings();
            settings.DisableDiff(); // headless: nunca lanzar un visor de diffs externo en este arnes
            if (umbralPropio is double u) settings.SsimThreshold(u); // override real via VerifierSettings.Context, ver VerifyImageSharp.cs
            var pathInfo = new PathInfo(directory: DirSnapshotsVisuales, typeName: "SnapshotVisual", methodName: nombre);
            using var verifier = new InnerVerifier(sourceFile, settings, "SnapshotVisual", nombre, null, pathInfo);

            // NUNCA .GetAwaiter().GetResult() aqui - deadlockearia de verdad (medido: se colgo
            // literalmente MAS DE DOS HORAS las dos primeras veces que se probo esto, hasta
            // encontrar el motivo real). Este arnes instala a mano un DispatcherSynchronizationContext
            // (mismo comentario real en Program.cs, junto a X7-ASYNC/vm.Exploration.LoadFromPathAsync
            // unas lineas mas arriba en el propio Main): bloquear el hilo de UI con .GetResult()
            // mientras el propio motor de Verify hace su I/O real (comprobar/leer/escribir los
            // .verified./.received.) impide que ESE MISMO hilo bombee el mensaje que reanudaria la
            // continuacion - interbloqueo clasico de sync-over-async con contexto de sincronizacion
            // capturado. Arreglo real (el mismo patron ya establecido en este arnes): pumpear el
            // Dispatcher con DoEvents() en bucle hasta que la Task termine, nunca bloquear.
            var task = verifier.VerifyStream(png, "png", (object?)null);
            while (!task.IsCompleted) DoEvents();
            if (task.IsFaulted) throw task.Exception!.GetBaseException();

            Console.WriteLine($"SNAPSHOT-VISUAL: {nombre} -> OK (SSIM >= {(umbralPropio ?? SsimUmbralSnapshotVisual):F4} contra la referencia aprobada)");
            return true;
        }
        catch (Exception ex)
        {
            // "VerifyException" (la que de verdad lanza el motor cuando no coincide o no hay
            // referencia todavia) es INTERNAL al paquete Verify - no accesible desde aqui (
            // comprobado compilando: CS0122). Capturar Exception a secas es correcto igualmente:
            // este catch es el ULTIMO paso de esta funcion, cualquier excepcion real aqui es un
            // snapshot que no paso (o la excepcion no tiene nada que ver con Verify y hace falta
            // verla igual - el mensaje completo de ex se imprime siempre, nunca se traga nada).
            Console.WriteLine($"SNAPSHOT-VISUAL: {nombre} -> FALLO ({ex.GetType().Name})\n{ex.Message}");
            return false;
        }
    }
}
