// BADGES_ESTADO_SOLO=1 (24-sep-2026, investigador-bug, TASK CONTEXT e5eaea9e-c261-4199-8e7d-
// 060b6054f58d - "Terrakeep: inventario de badges/status-pills... para unificacion visual -
// imagen4"): hueco de cobertura REAL cerrado por este modo. Antes de hoy ningun modo de este
// arnes comprobaba los badges/status-pills NO INTERACTIVOS de la app JUNTOS ni comparaba su
// geometria real entre si - cada uno se probaba aislado (COMPARE_SOLO solo hace capturas de
// pantalla del comparador sin medir el pill de "N diferencia(s)"; GUIACHIP_SOLO mide el chip
// "Guia: <Zona>" pero nunca lo compara con "Solo lectura", que vive a su lado en la misma barra
// de Exploracion; SNAPSHOT_VISUAL_SOLO nunca visita ni Comparar ni Exploracion con un mundo
// cargado). Por eso el "67 diferencia(s)" que reporto el usuario (imagen4.png, captura real en
// "Downloads\Keep\Arreglos familia keep\imagen4.png") pudo llevar un Padding/Background/
// CornerRadius distintos de "Solo lectura" y "Guia: Superficie" sin que ningun canario lo
// marcara nunca - los tres badges viven bien, cada uno consistente CONSIGO MISMO, pero nunca se
// habian medido unos CONTRA otros.
//
// Este modo mide con datos REALES (nunca "deberia ser distinto") la geometria real de cada
// badge/status-pill que el inventario de esta sesion encontro en MainWindow.xaml (ver
// bitacora.md para el archivo:linea exacto de cada uno) y HOY FALLA A PROPOSITO
// (BADGES_ESTADO_SOLO-CONSISTENCIA) porque el lenguaje visual real todavia NO esta unificado -
// es el mismo canario que revisor-visual/aplicador-fix deben dejar en verde tras unificarlos bajo
// un unico Style/recurso compartido. Datos deterministas y sinteticos en las dos rutas (dos
// personajes "BadgeTestA"/"BadgeTestB" con stats reales distintos para forzar Compare.
// DifferenceCount > 0, y el mismo personaje limpio sintetico que ya usa GUIACHIP_SOLO sobre el
// mundo real roca_negra.wld) - nunca personajes reales de este equipo, para que el resultado sea
// reproducible en cualquier maquina/ejecucion.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

internal static partial class Program
{
    private sealed record BadgeGeometriaReal(
        string Nombre, string TextoReal, CornerRadius Radio, Thickness Relleno,
        string FondoReal, string TextoColorReal, double FontSizeReal, FontWeight PesoReal);

    // Busca el TextBlock VISIBLE con el texto real exacto (nunca el clave de localizacion en
    // crudo, para medir de verdad lo que el usuario ve en pantalla) y sube `nivelesHastaBorder`
    // padres reales en el arbol visual hasta el Border que pinta el fondo/CornerRadius del badge
    // (1 nivel cuando el TextBlock es hijo DIRECTO del Border - "N diferencia(s)"/"Solo lectura";
    // 2 cuando hay un StackPanel horizontal de por medio con icono+texto - "Guia: <Zona>").
    private static BadgeGeometriaReal? MedirBadgePorTextoReal(Window window, string nombre, string textoEsperado, int nivelesHastaBorder)
    {
        var tb = Descendientes<TextBlock>(window).FirstOrDefault(t => t.IsVisible && t.Text == textoEsperado);
        if (tb == null) return null;

        DependencyObject actual = tb;
        for (int i = 0; i < nivelesHastaBorder; i++)
        {
            var padre = VisualTreeHelper.GetParent(actual);
            if (padre == null) return null;
            actual = padre;
        }
        if (actual is not Border bd) return null;

        string fondoReal = bd.Background is SolidColorBrush scbFondo ? scbFondo.Color.ToString() : (bd.Background?.GetType().Name ?? "(sin fondo)");
        string colorTextoReal = tb.Foreground is SolidColorBrush scbTexto ? scbTexto.Color.ToString() : (tb.Foreground?.GetType().Name ?? "(heredado)");
        return new BadgeGeometriaReal(nombre, textoEsperado, bd.CornerRadius, bd.Padding, fondoReal, colorTextoReal, tb.FontSize, tb.FontWeight);
    }

    private static void CapturarPngADisco(Window window, string nombreArchivo)
    {
        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
        rtb.Render(window);
        var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
        enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
        string ruta = Path.Combine(AppContext.BaseDirectory, nombreArchivo);
        using (var fs = File.Create(ruta)) enc.Save(fs);
        Console.WriteLine($"BADGES_ESTADO_SOLO: captura real -> {ruta}");
    }

    private static void EjecutarBadgesEstadoSolo(Window window, MainViewModel vm)
    {
        var medidos = new List<BadgeGeometriaReal>();
        string dirBadges = Path.Combine(Path.GetTempPath(), $"terrakeep-badges-estado-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dirBadges);
        try
        {
            // 1) "N diferencia(s)" (Personaje > Comparar, MainWindow.xaml:4946-4967) - dos
            // personajes SINTETICOS con Dificultad/Vida/Mana reales distintos, nunca personajes
            // reales de este equipo (mismo criterio ya usado por INSIGNIAS-TMOD-SIN-CALAMITY mas
            // arriba en este mismo arnes).
            var serviceBadges = new CharacterFileService();
            CharacterListEntryViewModel CrearEntradaSintetica(string nombre, int vidaMax, int manaMax, byte dificultad)
            {
                string plrPath = Path.Combine(dirBadges, nombre + ".plr");
                var personaje = new PlrCharacter
                {
                    Name = nombre,
                    Version = 279,
                    Difficulty = dificultad,
                    HealthMax = vidaMax,
                    ManaMax = manaMax,
                    PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
                    Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
                };
                File.WriteAllBytes(plrPath, PlrFile.Write(personaje));
                return new CharacterListEntryViewModel(plrPath, personaje, isTModLoader: false, tplr: null, DateTime.UtcNow, serviceBadges.EquipmentAppearance);
            }

            var entradaA = CrearEntradaSintetica("BadgeTestA", vidaMax: 400, manaMax: 200, dificultad: 0);
            var entradaB = CrearEntradaSintetica("BadgeTestB", vidaMax: 500, manaMax: 180, dificultad: 3);
            vm.Home.Characters.Add(entradaA);
            vm.Home.Characters.Add(entradaB);

            vm.SelectedTabIndex = 0; // Inicio
            DoEvents(); DoEvents();
            vm.OpenCompareCommand.Execute(null);
            vm.Compare.SelectedA = entradaA;
            vm.Compare.SelectedB = entradaB;
            WaitForDispatcher(150);
            DoEvents(); DoEvents(); DoEvents();

            Console.WriteLine($"BADGES_ESTADO_SOLO: Compare.DifferenceCount real = {vm.Compare.DifferenceCount} (esperado > 0 con estos dos personajes sinteticos)");
            if (vm.Compare.DifferenceCount <= 0)
            {
                Console.WriteLine("FALLO: BADGES_ESTADO_SOLO - los dos personajes sinteticos deberian dar al menos 1 diferencia real; no se puede medir el pill de recuento asi");
            }
            else
            {
                string textoPillReal = string.Format(vm.Loc["compare_difference_count"], vm.Compare.DifferenceCount);
                var mComparar = MedirBadgePorTextoReal(window, "Comparar: pill \"N diferencia(s)\" (MainWindow.xaml:4946)", textoPillReal, nivelesHastaBorder: 1);
                if (mComparar == null) Console.WriteLine($"FALLO: BADGES_ESTADO_SOLO - no se encontro en pantalla el badge con texto real '{textoPillReal}'");
                else medidos.Add(mComparar);
            }
            CapturarPngADisco(window, "badges-comparar.png");

            // 2) "Solo lectura" (MainWindow.xaml:5322) + "Guia: <Zona>" (MainWindow.xaml:6072) -
            // MISMO montaje real y determinista que ya usa GUIACHIP_SOLO mas arriba en este mismo
            // fichero (personaje limpio sintetico + roca_negra.wld real).
            string plrLimpioPath = Path.Combine(dirBadges, "PersonajeLimpioBadges.plr");
            var personajeLimpio = new PlrCharacter
            {
                Name = "PersonajeLimpioBadges",
                Version = 279,
                PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
                Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
            };
            File.WriteAllBytes(plrLimpioPath, PlrFile.Write(personajeLimpio));

            string wldPath = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld";
            if (!File.Exists(wldPath))
            {
                Console.WriteLine("BADGES_ESTADO_SOLO: AVISO - falta el mundo real de prueba (roca_negra.wld), se omiten 'Solo lectura' y 'Guia: <Zona>' (LIMITE REAL, no simulado)");
            }
            else
            {
                vm.LoadFromPath(plrLimpioPath);
                DoEvents(); DoEvents();
                vm.SelectedTabIndex = 4; // Exploracion
                DoEvents(); DoEvents();
                var tareaExplore = vm.Exploration.LoadFromPathAsync(wldPath);
                while (!tareaExplore.IsCompleted) DoEvents();
                DoEvents(); DoEvents(); DoEvents();
                System.Threading.Thread.Sleep(300); DoEvents(); DoEvents();

                Console.WriteLine($"BADGES_ESTADO_SOLO: Exploration.IsWorldLoaded={vm.Exploration.IsWorldLoaded}");
                var mSoloLectura = MedirBadgePorTextoReal(window, "Exploracion: badge \"Solo lectura\" (MainWindow.xaml:5322)", vm.Loc["explore_readonly_badge"], nivelesHastaBorder: 1);
                if (mSoloLectura == null) Console.WriteLine("FALLO: BADGES_ESTADO_SOLO - no se encontro en pantalla el badge 'Solo lectura' con el mundo cargado");
                else medidos.Add(mSoloLectura);

                string zonaReal = vm.Guide.ObjetivoPaso?.Zona ?? "(null)";
                var claveChipPorZona = new Dictionary<string, string>
                {
                    ["Superficie"] = "guide_zone_chip_superficie",
                    ["Subterraneo"] = "guide_zone_chip_subterraneo",
                    ["Cavernas"] = "guide_zone_chip_cavernas",
                    ["Infierno"] = "guide_zone_chip_infierno",
                };
                if (claveChipPorZona.TryGetValue(zonaReal, out var claveChip))
                {
                    var mGuiaZona = MedirBadgePorTextoReal(window, $"Exploracion: chip \"Guia: {zonaReal}\" (MainWindow.xaml:6072)", vm.Loc[claveChip], nivelesHastaBorder: 2);
                    if (mGuiaZona == null) Console.WriteLine($"FALLO: BADGES_ESTADO_SOLO - no se encontro en pantalla el chip 'Guia: {zonaReal}' pese a Zona={zonaReal}");
                    else medidos.Add(mGuiaZona);
                }
                else
                {
                    Console.WriteLine($"BADGES_ESTADO_SOLO: Guide.ObjetivoPaso.Zona real ('{zonaReal}') no es una de las 4 bandas de profundidad en este mundo/progreso - se omite el chip de Guia (mismo caso real ya documentado en GUIACHIP_SOLO)");
                }

                CapturarPngADisco(window, "badges-exploracion.png");
            }

            // Evidencia real: geometria medida de cada badge encontrado en pantalla, y la
            // comprobacion de CONSISTENCIA que revisor-visual/aplicador-fix deben dejar en verde
            // tras unificar (ver bitacora.md para el inventario completo archivo:linea).
            Console.WriteLine("BADGES_ESTADO_SOLO: geometria real medida de cada badge/status-pill encontrado en pantalla:");
            foreach (var b in medidos)
                Console.WriteLine($"  - {b.Nombre}: texto='{b.TextoReal}' CornerRadius={b.Radio} Padding={b.Relleno} Fondo={b.FondoReal} TextoColor={b.TextoColorReal} FontSize={b.FontSizeReal} FontWeight={b.PesoReal}");

            if (medidos.Count >= 2)
            {
                var referencia = medidos[0];
                bool inconsistente = medidos.Skip(1).Any(b => !b.Radio.Equals(referencia.Radio) || !b.Relleno.Equals(referencia.Relleno));
                if (inconsistente)
                    Console.WriteLine("FALLO: BADGES_ESTADO_SOLO-CONSISTENCIA - los badges/status-pills medidos NO comparten CornerRadius/Padding reales entre si (ver evidencia arriba). Hueco visual real reportado por el usuario el 24-sep-2026 (imagen4.png, \"67 diferencia(s)\"). Pendiente para revisor-visual/aplicador-fix: unificar bajo un unico Style/recurso compartido - este canario debe quedar en VERDE tras el arreglo.");
                else
                    Console.WriteLine("BADGES_ESTADO_SOLO-CONSISTENCIA: los badges/status-pills medidos SI comparten CornerRadius/Padding reales entre si.");
            }
            else
            {
                Console.WriteLine("BADGES_ESTADO_SOLO-CONSISTENCIA: menos de 2 badges medidos con exito, no se puede comparar consistencia real esta corrida (ver FALLOs arriba).");
            }
        }
        catch (Exception ex) { Console.WriteLine("BADGES_ESTADO_SOLO-EXCEPTION: " + ex); }
        finally
        {
            try { Directory.Delete(dirBadges, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        Console.WriteLine("DONE (BADGES_ESTADO_SOLO)");
        Environment.Exit(0);
    }
}
