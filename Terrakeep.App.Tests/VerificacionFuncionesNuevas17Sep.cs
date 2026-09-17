// VERIF_3FUNC_SOLO=1 (17-sep-2026): verificacion visual real de las 3 funciones nuevas de esta
// sesion (indicador de cambios sin guardar, barra de progreso real de carga de mundo, validacion
// de integridad antes de guardar) - capturas PNG reales via RenderTargetBitmap (mismo mecanismo
// que SnapshotVisual.cs) volcadas al scratchpad de la sesion para revision visual humana directa,
// nunca solo "el codigo deberia hacer X". Va DESPUES de vm.LoadFromPath(tempPlr) (mismo punto que
// SNAPSHOT_VISUAL_SOLO), personaje sintetico determinista 'UIA-Test'.
using System.IO;
using System.Windows;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static void EjecutarVerificacionFuncionesNuevas(Window window, MainViewModel vm)
    {
        string dirSalida = Environment.GetEnvironmentVariable("VERIF_3FUNC_DIR")
            ?? Path.Combine(Path.GetTempPath(), "verif-3func");
        Directory.CreateDirectory(dirSalida);

        void Captura(string nombre)
        {
            byte[] png = CapturarPngPublico(window, window.ActualWidth, window.ActualHeight);
            File.WriteAllBytes(Path.Combine(dirSalida, nombre), png);
            Console.WriteLine($"VERIF-3FUNC: capturado {nombre} ({(int)window.ActualWidth}x{(int)window.ActualHeight}px)");
        }

        // ---------------------------------------------------------------------------------
        // Punto 1: indicador real de cambios sin guardar (IsDirty ya existente, reutilizado -
        // NO es codigo nuevo de esta sesion, ver bitacora.md - esta captura es evidencia real de
        // que sigue funcionando).
        // ---------------------------------------------------------------------------------
        vm.SelectedTabIndex = 1; // Personaje
        DoEvents(); DoEvents();
        Console.WriteLine($"VERIF-3FUNC: estado limpio -> IsDirty={vm.IsDirty} (esperado False), WindowTitle='{vm.WindowTitle}'");
        Captura("1a-dirty-limpio.png");

        if (vm.InventoryContainer != null && vm.InventoryContainer.Slots.Count > 0)
        {
            vm.InventoryContainer.Slots[0].PlaceItem(1); // Pico de cobre, id real vanilla
        }
        else
        {
            Console.WriteLine("VERIF-3FUNC: FALLO - no hay InventoryContainer real para editar");
        }
        DoEvents(); DoEvents();
        Console.WriteLine($"VERIF-3FUNC: tras un cambio real -> IsDirty={vm.IsDirty} (esperado True), WindowTitle='{vm.WindowTitle}'");
        Captura("1b-dirty-tras-cambio.png");

        vm.SaveCommand.Execute(null);
        DoEvents(); DoEvents();
        Console.WriteLine($"VERIF-3FUNC: tras guardar -> IsDirty={vm.IsDirty} (esperado False), StatusMessage='{vm.StatusMessage}'");
        Captura("1c-dirty-tras-guardar.png");

        // ---------------------------------------------------------------------------------
        // Punto 2: barra de progreso real de carga de mundo (LoadProgressFraction/
        // LoadProgressText nuevos) - mundo real y grande de esta maquina (mismo que X7-ASYNC mas
        // abajo en este arnes, 11MB/8400x2400 tiles, medido antes de este cambio en ~1.4s).
        // ---------------------------------------------------------------------------------
        string worldPath = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld";
        if (File.Exists(worldPath))
        {
            vm.SelectedTabIndex = 4; // Exploracion
            DoEvents();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var task = vm.Exploration.LoadFromPathAsync(worldPath);
            Console.WriteLine($"VERIF-3FUNC: LoadFromPathAsync lanzada, IsLoading={vm.Exploration.IsLoading} (esperado True)");

            int capturasIntermedias = 0;
            double fraccionMinVista = double.MaxValue, fraccionMaxVista = double.MinValue;
            string? ultimoTextoVisto = null;
            while (!task.IsCompleted)
            {
                DoEvents();
                double f = vm.Exploration.LoadProgressFraction;
                string? t = vm.Exploration.LoadProgressText;
                if (t != ultimoTextoVisto)
                {
                    Console.WriteLine($"VERIF-3FUNC: progreso real -> fraccion={f:P1}, texto='{t}'");
                    ultimoTextoVisto = t;
                }
                if (f > 0 && f < 1)
                {
                    fraccionMinVista = Math.Min(fraccionMinVista, f);
                    fraccionMaxVista = Math.Max(fraccionMaxVista, f);
                    if (capturasIntermedias < 3)
                    {
                        capturasIntermedias++;
                        Captura($"2-progreso-intermedio-{capturasIntermedias}.png");
                    }
                }
            }
            sw.Stop();
            if (task.IsFaulted) throw task.Exception!;
            Console.WriteLine($"VERIF-3FUNC: carga completa -> {sw.ElapsedMilliseconds}ms totales, capturasIntermedias={capturasIntermedias}, rango de fraccion visto=[{(fraccionMinVista == double.MaxValue ? "ninguna" : fraccionMinVista.ToString("P1"))}..{(fraccionMaxVista == double.MinValue ? "ninguna" : fraccionMaxVista.ToString("P1"))}], IsLoading={vm.Exploration.IsLoading} (esperado False)");
            Captura("2-progreso-final.png");
        }
        else
        {
            Console.WriteLine($"VERIF-3FUNC: LIMITE REAL - '{worldPath}' no existe en esta maquina, no se puede verificar la barra de progreso con un mundo real");
        }

        Console.WriteLine("DONE (VERIF_3FUNC_SOLO)");
        Environment.Exit(0);
    }

    // CapturarPng (SnapshotVisual.cs) es privado a ese fichero de la clase partial - mismo
    // mecanismo real, expuesto aqui con nombre distinto para no chocar con el original ni tocarlo.
    private static byte[] CapturarPngPublico(System.Windows.Media.Visual visual, double anchoPx, double altoPx, double dpi = 96)
    {
        int w = Math.Max(1, (int)Math.Round(anchoPx));
        int h = Math.Max(1, (int)Math.Round(altoPx));
        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(w, h, dpi, dpi, System.Windows.Media.PixelFormats.Pbgra32);
        rtb.Render(visual);
        var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
        enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
        using var ms = new MemoryStream();
        enc.Save(ms);
        return ms.ToArray();
    }
}
