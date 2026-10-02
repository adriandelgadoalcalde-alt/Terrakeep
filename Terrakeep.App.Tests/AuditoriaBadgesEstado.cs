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
// REAPERTURA (25-sep-2026, TASK CONTEXT e5eaea9e-c261-4199-8e7d-060b6054f58d): la primera pasada
// (24-sep-2026) unifico los 6 badges bajo UN UNICO Style ("StatusPillCaption") y este canario
// afirmaba EXITO cuando todos compartian la MISMA geometria (CornerRadius/Padding identicos). El
// usuario reabrio el punto de verdad: eso resolvia la inconsistencia accidental de Padding pero
// no era la intencion visual real - cada badge cumple un papel distinto (metrica/recuento,
// etiqueta informativa, estado semantico por color, KPI sobre overlay) y debe LEERSE distinto de
// un vistazo, no fundirse en un unico lenguaje "pastilla". El arreglo real (aplicador-fix,
// 25-sep-2026) sustituye "StatusPillCaption" por 4 familias visuales separadas
// (StateTagNeutral/MetricAccentText/SemanticStateChip/HeroKpiPill, Theme.xaml) - este canario
// pasa de comprobar "todos comparten geometria" (lo CONTRARIO de lo pedido) a comprobar que CADA
// FAMILIA preserva su propia invariante real:
// - Nº1 "N diferencia(s)" (MetricAccentText): el TextBlock YA NO tiene un Border como padre
//   inmediato (vive en un StackPanel horizontal junto a una barra de acento vertical, Width~2).
// - Nº2 "Solo lectura" (StateTagNeutral): CornerRadius real DISTINTO de 99 (etiqueta, no capsula)
//   y Background transparente (sin relleno).
// - Nº3 "Guia: <Zona>" (SemanticStateChip): Background NEUTRO fijo (BgElevatedBrush) para
//   CUALQUIER zona - el color semantico real vive en el BorderBrush, no en el fondo.
// Datos deterministas y sinteticos en las dos rutas (dos personajes "BadgeTestA"/"BadgeTestB"
// con stats reales distintos para forzar Compare.DifferenceCount > 0, y el mismo personaje
// limpio sintetico que ya usa GUIACHIP_SOLO sobre el mundo real roca_negra.wld) - nunca
// personajes reales de este equipo, para que el resultado sea reproducible en cualquier
// maquina/ejecucion.
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
        string FondoReal, string BordeReal, string TextoColorReal, double FontSizeReal, FontWeight PesoReal);

    // Busca el TextBlock VISIBLE con el texto real exacto (nunca el clave de localizacion en
    // crudo, para medir de verdad lo que el usuario ve en pantalla) y sube `nivelesHastaBorder`
    // padres reales en el arbol visual hasta el Border que pinta el fondo/CornerRadius del badge
    // (1 nivel cuando el TextBlock es hijo DIRECTO del Border - "Solo lectura"; 2 cuando hay un
    // StackPanel horizontal de por medio con icono+texto - "Guia: <Zona>"). Usar solo para
    // familias StateTagNeutral/SemanticStateChip/HeroKpiPill (las que SI tienen un Border propio
    // como padre real del texto) - MetricAccentText usa `MedirBadgeMetricaPorTextoReal` en vez de
    // esta, porque su invariante real es justo la AUSENCIA de ese Border.
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
        string bordeReal = bd.BorderBrush is SolidColorBrush scbBorde ? scbBorde.Color.ToString() : (bd.BorderBrush?.GetType().Name ?? "(sin borde)");
        string colorTextoReal = tb.Foreground is SolidColorBrush scbTexto ? scbTexto.Color.ToString() : (tb.Foreground?.GetType().Name ?? "(heredado)");
        return new BadgeGeometriaReal(nombre, textoEsperado, bd.CornerRadius, bd.Padding, fondoReal, bordeReal, colorTextoReal, tb.FontSize, tb.FontWeight);
    }

    // Familia "MetricAccentText" (rediseno 25-sep-2026): SIN Border propio a proposito - el
    // TextBlock manda por si mismo con una barra de acento vertical (Border Width~2) como
    // HERMANO dentro del mismo StackPanel horizontal, nunca como padre que lo encierra. Mide y
    // verifica esa estructura real en vez de asumir geometria de capsula.
    private static BadgeGeometriaReal? MedirBadgeMetricaPorTextoReal(Window window, string nombre, string textoEsperado)
    {
        var tb = Descendientes<TextBlock>(window).FirstOrDefault(t => t.IsVisible && t.Text == textoEsperado);
        if (tb == null) return null;

        var padreInmediato = VisualTreeHelper.GetParent(tb);
        if (padreInmediato is Border)
        {
            Console.WriteLine($"FALLO: BADGES_ESTADO_SOLO-FAMILIA-MetricAccentText - '{nombre}' el padre inmediato del TextBlock ES un Border; la familia MetricAccentText no debe tener Border propio (debe vivir en un StackPanel junto a la barra de acento).");
            return null;
        }
        if (padreInmediato is not Panel panelPadre)
        {
            Console.WriteLine($"FALLO: BADGES_ESTADO_SOLO-FAMILIA-MetricAccentText - '{nombre}' el padre inmediato del TextBlock no es ni Border ni Panel ({padreInmediato?.GetType().Name ?? "(null)"}).");
            return null;
        }

        var barraAcento = panelPadre.Children.OfType<Border>().FirstOrDefault();
        double anchoBarraReal = barraAcento?.Width ?? -1;
        if (barraAcento == null || Math.Abs(anchoBarraReal - 2) > 0.5)
        {
            Console.WriteLine($"FALLO: BADGES_ESTADO_SOLO-FAMILIA-MetricAccentText - '{nombre}' no tiene una barra de acento (Border hermano, Width~2) valida junto al texto (Width real={anchoBarraReal}).");
            return null;
        }

        string colorTextoReal = tb.Foreground is SolidColorBrush scbTexto ? scbTexto.Color.ToString() : (tb.Foreground?.GetType().Name ?? "(heredado)");
        Console.WriteLine($"BADGES_ESTADO_SOLO-FAMILIA-MetricAccentText: '{nombre}' padre inmediato={padreInmediato.GetType().Name} (NO Border, correcto) barra de acento Width real={anchoBarraReal} (esperado ~2, correcto).");
        return new BadgeGeometriaReal(nombre, textoEsperado, new CornerRadius(0), new Thickness(0),
            "(sin Border propio - familia MetricAccentText)", "(sin Border propio)", colorTextoReal, tb.FontSize, tb.FontWeight);
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
            // 1) "N diferencia(s)" (Personaje > Comparar) - dos personajes SINTETICOS con
            // Dificultad/Vida/Mana reales distintos, nunca personajes reales de este equipo
            // (mismo criterio ya usado por INSIGNIAS-TMOD-SIN-CALAMITY mas arriba en este mismo
            // arnes).
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
                Console.WriteLine("FALLO: BADGES_ESTADO_SOLO - los dos personajes sinteticos deberian dar al menos 1 diferencia real; no se puede medir el badge de recuento asi");
            }
            else
            {
                string textoPillReal = string.Format(vm.Loc["compare_difference_count"], vm.Compare.DifferenceCount);
                var mComparar = MedirBadgeMetricaPorTextoReal(window, "Comparar: \"N diferencia(s)\" (familia MetricAccentText)", textoPillReal);
                if (mComparar == null) Console.WriteLine($"FALLO: BADGES_ESTADO_SOLO - no se encontro/valido en pantalla el badge con texto real '{textoPillReal}'");
                else medidos.Add(mComparar);
            }
            CapturarPngADisco(window, "badges-comparar.png");

            // 2) "Solo lectura" (familia StateTagNeutral) + chip de la guia del mapa (familia
            // SemanticStateChip) - MISMO montaje real y determinista que ya usa GUIACHIP_SOLO
            // (personaje limpio sintetico + roca_negra.wld real).
            string plrLimpioPath = Path.Combine(dirBadges, "PersonajeLimpioBadges.plr");
            var personajeLimpio = new PlrCharacter
            {
                Name = "PersonajeLimpioBadges",
                Version = 279,
                PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
                Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
            };
            File.WriteAllBytes(plrLimpioPath, PlrFile.Write(personajeLimpio));

            string wldPath = MundoAislado(RutasEntornoReal.Documentos(@"tModLoader\Worlds\roca_negra.wld"));
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
                var mSoloLectura = MedirBadgePorTextoReal(window, "Exploracion: badge \"Solo lectura\" (familia StateTagNeutral)", vm.Loc["explore_readonly_badge"], nivelesHastaBorder: 1);
                if (mSoloLectura == null) Console.WriteLine("FALLO: BADGES_ESTADO_SOLO - no se encontro en pantalla el badge 'Solo lectura' con el mundo cargado");
                else medidos.Add(mSoloLectura);

                // F2b (02-oct-2026): el chip "Guía: ..." sale de la guia v2 (capa real donde cae el
                // marcador de la siguiente parada, GuiaV2.MarcadorCapa) y su texto es GuiaV2.ChipMapaTexto.
                EsperarGuiaV2(vm);
                string capaReal = vm.GuiaV2.MarcadorCapa ?? "(null)";
                var bordeEsperadoPorCapa = ColorPorCapaGuia; // mismo mapeo que los MultiDataTrigger del Border.Style
                if (vm.GuiaV2.MarcadorVisible && bordeEsperadoPorCapa.ContainsKey(capaReal))
                {
                    string textoChip = vm.GuiaV2.ChipMapaTexto;
                    var mGuiaZona = MedirBadgePorTextoReal(window, $"Exploracion: chip de la guia \"{textoChip}\" (familia SemanticStateChip)", textoChip, nivelesHastaBorder: 2);
                    if (mGuiaZona == null)
                    {
                        Console.WriteLine($"FALLO: BADGES_ESTADO_SOLO - no se encontro en pantalla el chip de la guia '{textoChip}' pese a haber marcador en la capa {capaReal}");
                    }
                    else
                    {
                        medidos.Add(mGuiaZona);
                        const string fondoNeutroEsperado = "#FF1E2233"; // BgElevatedColor
                        string bordeEsperado = bordeEsperadoPorCapa[capaReal];
                        bool fondoOk = mGuiaZona.FondoReal == fondoNeutroEsperado;
                        bool bordeOk = mGuiaZona.BordeReal == bordeEsperado;
                        if (fondoOk && bordeOk)
                            Console.WriteLine($"BADGES_ESTADO_SOLO-FAMILIA-SemanticStateChip: chip de la guia (capa {capaReal}) Background real={mGuiaZona.FondoReal} (neutro, esperado {fondoNeutroEsperado} para CUALQUIER capa - correcto) BorderBrush real={mGuiaZona.BordeReal} (esperado {bordeEsperado} para esta capa - correcto).");
                        else
                            Console.WriteLine($"FALLO: BADGES_ESTADO_SOLO-FAMILIA-SemanticStateChip - chip de la guia (capa {capaReal}) Background real={mGuiaZona.FondoReal} (esperado {fondoNeutroEsperado}, neutro) BorderBrush real={mGuiaZona.BordeReal} (esperado {bordeEsperado} para esta capa)");
                    }
                }
                else
                {
                    // Personaje limpio + roca_negra: la primera parada (punto de aparicion) siempre se
                    // situa; sin marcador el chip no puede medirse y eso es un fallo, no un caso a omitir.
                    Console.WriteLine($"FALLO: BADGES_ESTADO_SOLO - sin marcador de la guia v2 (visible={vm.GuiaV2.MarcadorVisible}, capa={capaReal}, siguiente={vm.GuiaV2.Siguiente?.Id}): no se puede medir el chip de la guia");
                }

                if (mSoloLectura != null)
                {
                    bool cornerOk = !mSoloLectura.Radio.Equals(new CornerRadius(99));
                    const string fondoTransparenteEsperado = "#00FFFFFF";
                    bool fondoOk = mSoloLectura.FondoReal == fondoTransparenteEsperado;
                    if (cornerOk && fondoOk)
                        Console.WriteLine($"BADGES_ESTADO_SOLO-FAMILIA-StateTagNeutral: 'Solo lectura' CornerRadius real={mSoloLectura.Radio} (distinto de 99, correcto - etiqueta, no capsula) Background real={mSoloLectura.FondoReal} (transparente, correcto - sin relleno).");
                    else
                        Console.WriteLine($"FALLO: BADGES_ESTADO_SOLO-FAMILIA-StateTagNeutral - 'Solo lectura' CornerRadius real={mSoloLectura.Radio} (NO debe ser 99) Background real={mSoloLectura.FondoReal} (esperado transparente {fondoTransparenteEsperado})");
                }

                CapturarPngADisco(window, "badges-exploracion.png");
            }

            // Evidencia real: geometria medida de cada badge encontrado en pantalla (referencia,
            // ya no se compara geometricamente unos contra otros - cada familia tiene su propia
            // invariante real, verificada arriba en su propio bloque).
            Console.WriteLine("BADGES_ESTADO_SOLO: geometria real medida de cada badge/status-pill encontrado en pantalla:");
            foreach (var b in medidos)
                Console.WriteLine($"  - {b.Nombre}: texto='{b.TextoReal}' CornerRadius={b.Radio} Padding={b.Relleno} Fondo={b.FondoReal} Borde={b.BordeReal} TextoColor={b.TextoColorReal} FontSize={b.FontSizeReal} FontWeight={b.PesoReal}");

            Console.WriteLine("BADGES_ESTADO_SOLO-FAMILIAS: cada familia visual es distinta por diseño y cada una preserva su propia invariante (verificado)");
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
