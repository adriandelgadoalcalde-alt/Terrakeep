// CANARIOS REALES - "una sola guia en toda la app" (F2b, 02-oct-2026).
//
// Hasta F2, el chip "Guía: <capa>" y las bandas de profundidad del mapa de Exploracion (y la tarjeta
// "Te toca" de Inicio) leian la guia v1 mientras la pestaña Guia y su marcador ya usaban la v2: en
// una captura real salia "Guía: Subterráneo" junto al marcador del Rey slime. Estos dos canarios
// sustituyen a los cuerpos v1 de IDEA7_SOLO y GUIACHIP_SOLO y comprueban LO MISMO que comprobaban
// (bandas con los limites reales del .wld, una sola banda visible y solo cuando hay objetivo, chip
// visible con el color semantico de su capa, banda pintada de verdad), ahora contra la guia v2, y
// ademas que alfiler, banda y chip cuentan la MISMA parada en el MISMO sitio.
//
// Mundo: roca_negra.wld real, siempre a traves de MundoAislado (copia temporal, nunca el archivo del
// usuario). Personaje: sintetico limpio escrito con PlrFile.Write en una carpeta temporal.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Terrakeep.App;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.PlrFormat;

internal static partial class Program
{
    // Capa (GuiaV2ViewModel.MarcadorCapa) -> color real de su banda/borde en Theme.xaml
    // (MasterGoldBrush / EquippedGreenBrush / DebuffBrush / CalamityBrush).
    private static readonly Dictionary<string, string> ColorPorCapaGuia = new()
    {
        ["superficie"] = "#FFFFD24A",
        ["subterraneo"] = "#FF3DDC6E",
        ["cavernas"] = "#FF9B59B6",
        ["infierno"] = "#FFC0392B",
    };

    /// <summary>Tras cargar personaje/mundo: fuerza la evaluacion de la guia v2 y espera al marcador.</summary>
    private static void EsperarGuiaV2(MainViewModel vm)
    {
        DoEvents(); WaitForDispatcher(200); DoEvents();
        vm.GuiaV2.Refresh();
        long limite = Environment.TickCount64 + 20_000;
        while (!vm.GuiaV2.MarcadorTarea.IsCompleted && Environment.TickCount64 < limite) DoEvents();
        DoEvents(); DoEvents();
    }

    private static string CrearPersonajeLimpio(string carpeta, string nombre)
    {
        Directory.CreateDirectory(carpeta);
        string ruta = System.IO.Path.Combine(carpeta, nombre + ".plr");
        var p = new PlrCharacter
        {
            Name = nombre,
            Version = 279,
            PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
            Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
        };
        File.WriteAllBytes(ruta, PlrFile.Write(p));
        return ruta;
    }

    /// <summary>Las 4 bandas de profundidad del mapa: los unicos Rectangle no hit-testables con
    /// opacidad 0.30 y uno de los 4 pinceles de capa.</summary>
    private static List<Rectangle> BandasGuia(Window window) =>
        Descendientes<Rectangle>(window)
            .Where(r => !r.IsHitTestVisible && Math.Abs(r.Opacity - 0.30) < 0.01
                        && r.Fill is SolidColorBrush b && ColorPorCapaGuia.ContainsValue(b.Color.ToString()))
            .ToList();

    private static Canvas? AlfilerGuia(Window window) =>
        Descendientes<Canvas>(window).FirstOrDefault(c => c.Name == "GuiaMarcadorSiguiente");

    private static TextBlock? TextoChipGuia(Window window) =>
        Descendientes<TextBlock>(window).FirstOrDefault(t => t.Name == "GuiaChipMapaTexto");

    private static double? TopBandaEsperada(MainViewModel vm, string capa) => capa switch
    {
        "superficie" => vm.Exploration.GuideBandSuperficieTop,
        "subterraneo" => vm.Exploration.GuideBandSubterraneoTop,
        "cavernas" => vm.Exploration.GuideBandCavernasTop,
        "infierno" => vm.Exploration.GuideBandInfiernoTop,
        _ => null,
    };

    private static void GuardarCaptura(Window window, string nombre, string etiqueta)
    {
        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(window);
        var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
        enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
        string shot = System.IO.Path.Combine(AppContext.BaseDirectory, nombre);
        using (var fs = File.Create(shot)) enc.Save(fs);
        Console.WriteLine($"{etiqueta}: captura real -> {shot}");
    }

    // IDEA7_SOLO (capa guia sobre el mapa), sobre la guia v2.
    private static void EjecutarIdea7Solo(MainWindow window, MainViewModel vm)
    {
        string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"terrakeep-idea7-{Guid.NewGuid():N}");
        try
        {
            string mundoPath = MundoAislado(RutasEntornoReal.Documentos(@"tModLoader\Worlds\roca_negra.wld"));
            if (!File.Exists(mundoPath)) { Console.WriteLine("IDEA7_SOLO: AVISO - falta el mundo real de prueba, se omite"); return; }

            vm.LoadFromPath(CrearPersonajeLimpio(dir, "PersonajeIdea7"));
            DoEvents(); DoEvents();
            vm.SelectedTabIndex = 4; // Exploracion
            DoEvents();
            var taskCarga = vm.Exploration.LoadFromPathAsync(mundoPath);
            while (!taskCarga.IsCompleted) DoEvents();
            EsperarGuiaV2(vm);

            // --- 1. Verdad de referencia de las 4 bandas, WldReader propio (igual que antes) ---
            var mundoVerdad = Terrakeep.Core.WldFormat.WldReader.Read(File.ReadAllBytes(mundoPath));
            double groundLevel = mundoVerdad.Header.GroundLevel, rockLevel = mundoVerdad.Header.RockLevel;
            double infiernoTop = mundoVerdad.Header.TilesHigh - 192;
            Console.WriteLine($"IDEA7_SOLO: verdad de referencia -> GroundLevel={groundLevel}, RockLevel={rockLevel}, TilesHigh={mundoVerdad.Header.TilesHigh}, InfiernoTop={infiernoTop}");
            bool bandasOk = vm.Exploration.GuideBandSuperficieTop == 0.0 && vm.Exploration.GuideBandSuperficieHeight == groundLevel
                && vm.Exploration.GuideBandSubterraneoTop == groundLevel && vm.Exploration.GuideBandSubterraneoHeight == rockLevel - groundLevel
                && vm.Exploration.GuideBandCavernasTop == rockLevel && vm.Exploration.GuideBandCavernasHeight == infiernoTop - rockLevel
                && vm.Exploration.GuideBandInfiernoTop == infiernoTop && vm.Exploration.GuideBandInfiernoHeight == 192.0;
            if (!bandasOk) Console.WriteLine("FALLO: IDEA7_SOLO - las bandas de profundidad calculadas por el ViewModel no coinciden con la verdad de referencia real");
            else Console.WriteLine("IDEA7_SOLO: las 4 bandas coinciden EXACTAMENTE con la verdad de referencia real");

            // --- 2. Objetivo = siguiente parada de la guia v2: alfiler, UNA banda y chip, los tres de la misma parada ---
            var g = vm.GuiaV2;
            Console.WriteLine($"IDEA7_SOLO: guia={g.GuiaId} siguiente={g.Siguiente?.Id} '{g.Siguiente?.TituloPlano}' marcador visible={g.MarcadorVisible} ({g.MarcadorX},{g.MarcadorY}) capa={g.MarcadorCapa} aprox={g.MarcadorAproximado}");
            if (g.Siguiente == null) Console.WriteLine("FALLO: IDEA7_SOLO - un personaje limpio tiene que tener siguiente parada");
            else if (!g.MarcadorVisible) Console.WriteLine($"FALLO: IDEA7_SOLO - la siguiente parada '{g.Siguiente.Id}' no tiene marcador en el mapa del mundo real cargado");
            else
            {
                ComprobarCapaGuiaEnMapa(window, vm, "IDEA7_SOLO", "siguiente parada");

                // Otro objetivo en OTRA capa (una zona del manual de biomas situada en el mundo real):
                // la banda y el chip tienen que seguir al marcador, no quedarse en la capa anterior.
                var tz = g.ResolverZonasAsync();
                while (!tz.IsCompleted) DoEvents();
                DoEvents();
                string capaAntes = g.MarcadorCapa!;
                var otraZona = g.Zonas.FirstOrDefault(z => z.Ubicacion != null
                    && Terrakeep.App.ViewModels.GuiaV2ViewModel.CapaDe(z.Ubicacion.Y, mundoVerdad.Header) != capaAntes);
                if (otraZona == null) Console.WriteLine("IDEA7_SOLO: AVISO - ninguna zona situada en otra capa en este mundo, se omite el cambio de capa");
                else
                {
                    g.VerZonaEnMapa(otraZona); DoEvents(); DoEvents();
                    vm.SelectedTabIndex = 4; DoEvents(); DoEvents();
                    Console.WriteLine($"IDEA7_SOLO: marcador movido a la zona '{otraZona.Nombre}' -> capa {g.MarcadorCapa} (antes {capaAntes})");
                    if (g.MarcadorCapa == capaAntes) Console.WriteLine("FALLO: IDEA7_SOLO - la capa del marcador no cambio al señalar una zona de otra capa");
                    ComprobarCapaGuiaEnMapa(window, vm, "IDEA7_SOLO", "zona " + otraZona.Id);
                    if (!g.ChipMapaTexto.Contains(otraZona.Nombre, StringComparison.Ordinal))
                        Console.WriteLine($"FALLO: IDEA7_SOLO - el chip ('{g.ChipMapaTexto}') no nombra la zona señalada ('{otraZona.Nombre}')");
                }

                // --- 3. Sin objetivo: ni banda, ni chip, ni alfiler (nada de falsos positivos permanentes) ---
                g.MarcadorVisible = false;
                DoEvents(); DoEvents();
                int bandasDespues = BandasGuia(window).Count(r => r.IsVisible);
                bool chipDespues = TextoChipGuia(window)?.IsVisible == true;
                bool alfilerDespues = AlfilerGuia(window)?.IsVisible == true;
                Console.WriteLine($"IDEA7_SOLO: sin marcador -> bandas visibles={bandasDespues} chip visible={chipDespues} alfiler visible={alfilerDespues} (esperado 0/False/False)");
                if (bandasDespues != 0 || chipDespues || alfilerDespues)
                    Console.WriteLine("FALLO: IDEA7_SOLO - algo de la capa guia sigue visible sin marcador de la guia");

                EsperarGuiaV2(vm); // vuelve a la siguiente parada
                vm.SelectedTabIndex = 4; DoEvents(); DoEvents(); DoEvents();
                GuardarCaptura(window, "idea7-capa-guia-mapa.png", "IDEA7_SOLO");

                // Capturas del mapa centrado en el marcador (lo que hace "Ver en el mapa") a los dos
                // tamaños de referencia, para revisarlas a ojo: chip, alfiler y banda juntos.
                foreach (var (ancho, alto) in new[] { (1080, 700), (2560, 1440) })
                {
                    FijarTamaño(window, ancho, alto);
                    DoEvents(); DoEvents();
                    var ver = g.VerParadaEnMapaCommand.ExecuteAsync(g.Siguiente);
                    long lim = Environment.TickCount64 + 10_000;
                    while (!ver.IsCompleted && Environment.TickCount64 < lim) DoEvents();
                    DoEvents(); WaitForDispatcher(300); DoEvents();
                    ComprobarCapaGuiaEnMapa(window, vm, "IDEA7_SOLO", $"ver en el mapa {ancho}x{alto}");
                    GuardarCaptura(window, $"f2b-mapa-guia-{ancho}x{alto}.png", "IDEA7_SOLO");
                }
            }
        }
        catch (Exception ex) { Console.WriteLine("IDEA7_SOLO-EXCEPTION: " + ex); }
        finally { try { Directory.Delete(dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    /// <summary>Alfiler, banda y chip del mapa: los tres tienen que contar el MISMO objetivo (el
    /// marcador de la guia v2) en la MISMA capa real del mundo.</summary>
    private static void ComprobarCapaGuiaEnMapa(Window window, MainViewModel vm, string etiqueta, string que)
    {
        var g = vm.GuiaV2;
        DoEvents(); DoEvents();
        string capa = g.MarcadorCapa ?? "(null)";
        string capaVerdad = vm.Exploration.CurrentWorld is { } w ? Terrakeep.App.ViewModels.GuiaV2ViewModel.CapaDe(g.MarcadorY, w.Header) : "(sin mundo)";
        if (capa != capaVerdad) Console.WriteLine($"FALLO: {etiqueta} - capa del marcador '{capa}' distinta de la capa real de su casilla Y={g.MarcadorY} ('{capaVerdad}')");

        var visibles = BandasGuia(window).Where(r => r.IsVisible).ToList();
        Console.WriteLine($"{etiqueta}: [{que}] bandas visibles={visibles.Count} (esperado 1, la de '{capa}')");
        if (visibles.Count != 1) Console.WriteLine($"FALLO: {etiqueta} - se esperaba exactamente 1 banda visible para la capa '{capa}', hay {visibles.Count}");
        else
        {
            string colorBanda = ((SolidColorBrush)visibles[0].Fill).Color.ToString();
            double top = Canvas.GetTop(visibles[0]);
            double? topEsperado = TopBandaEsperada(vm, capa);
            Console.WriteLine($"{etiqueta}: [{que}] banda visible color={colorBanda} Canvas.Top={top} (esperado {ColorPorCapaGuia.GetValueOrDefault(capa)} / {topEsperado})");
            if (colorBanda != ColorPorCapaGuia.GetValueOrDefault(capa) || top != topEsperado)
                Console.WriteLine($"FALLO: {etiqueta} - la banda visible no es la de la capa del marcador ('{capa}')");
        }

        var alfiler = AlfilerGuia(window);
        if (alfiler == null || !alfiler.IsVisible) Console.WriteLine($"FALLO: {etiqueta} - el alfiler de la guia no esta visible con marcador");
        else
        {
            double x = Canvas.GetLeft(alfiler), y = Canvas.GetTop(alfiler);
            bool enSuCasilla = Math.Abs(x - (g.MarcadorX + 0.5)) < 0.01 && Math.Abs(y - (g.MarcadorY + 0.5)) < 0.01;
            Console.WriteLine($"{etiqueta}: [{que}] alfiler en ({x},{y}) para la casilla ({g.MarcadorX},{g.MarcadorY}) -> {enSuCasilla}; nombre UIA='{System.Windows.Automation.AutomationProperties.GetName(alfiler)}'");
            if (!enSuCasilla) Console.WriteLine($"FALLO: {etiqueta} - el alfiler no esta en la casilla del marcador");
            if (System.Windows.Automation.AutomationProperties.GetName(alfiler) != g.MarcadorTitulo)
                Console.WriteLine($"FALLO: {etiqueta} - el alfiler no se llama como el marcador ('{g.MarcadorTitulo}')");
        }

        var chip = TextoChipGuia(window);
        string textoEsperado = g.ChipMapaTexto;
        Console.WriteLine($"{etiqueta}: [{que}] chip visible={chip?.IsVisible} texto='{chip?.Text}'");
        if (chip == null || !chip.IsVisible) Console.WriteLine($"FALLO: {etiqueta} - el chip de la guia no esta visible con marcador");
        else
        {
            if (chip.Text != textoEsperado || !chip.Text.Contains(g.MarcadorTitulo, StringComparison.Ordinal)
                || !chip.Text.Contains(vm.Loc["guia2_capa_" + capa], StringComparison.Ordinal))
                Console.WriteLine($"FALLO: {etiqueta} - el chip ('{chip.Text}') no cuenta el marcador ('{g.MarcadorTitulo}') y su capa ('{vm.Loc["guia2_capa_" + capa]}')");
            // Borde con el color de la capa, fondo neutro (familia SemanticStateChip).
            var borde = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(chip)) as Border;
            string bordeReal = (borde?.BorderBrush as SolidColorBrush)?.Color.ToString() ?? "(sin borde)";
            Console.WriteLine($"{etiqueta}: [{que}] borde del chip={bordeReal} (esperado {ColorPorCapaGuia.GetValueOrDefault(capa)})");
            if (bordeReal != ColorPorCapaGuia.GetValueOrDefault(capa)) Console.WriteLine($"FALLO: {etiqueta} - el borde del chip no es el color de la capa '{capa}'");
        }
    }

    // GUIACHIP_SOLO, sobre la guia v2.
    private static void EjecutarGuiaChipSolo(MainWindow window, MainViewModel vm)
    {
        string dirChip = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"terrakeep-guiachip-{Guid.NewGuid():N}");
        try
        {
            string wldPath = MundoAislado(RutasEntornoReal.Documentos(@"tModLoader\Worlds\roca_negra.wld"));
            vm.LoadFromPath(CrearPersonajeLimpio(dirChip, "PersonajeLimpio"));
            DoEvents(); DoEvents();
            vm.SelectedTabIndex = 4; // Exploracion
            DoEvents(); DoEvents();
            var tareaChip = vm.Exploration.LoadFromPathAsync(wldPath);
            while (!tareaChip.IsCompleted) DoEvents();
            EsperarGuiaV2(vm);
            System.Threading.Thread.Sleep(300); DoEvents(); DoEvents();

            var g = vm.GuiaV2;
            string capa = g.MarcadorCapa ?? "(null)";
            Console.WriteLine($"GUIACHIP_SOLO: personaje limpio + mundo real 'roca_negra.wld' -> guia {g.GuiaId}, siguiente parada '{g.Siguiente?.TituloPlano}', marcador visible={g.MarcadorVisible}, capa real del marcador='{capa}'");
            if (!g.MarcadorVisible || !ColorPorCapaGuia.ContainsKey(capa))
            {
                Console.WriteLine($"FALLO: GUIACHIP_SOLO - la siguiente parada de un personaje limpio ('{g.Siguiente?.Id}') tiene que tener marcador y capa en este mundo (visible={g.MarcadorVisible}, capa={capa})");
                return;
            }

            // Chip: visible, con el texto de la parada y su capa, borde del color de la capa y fondo neutro.
            var chip = TextoChipGuia(window);
            Console.WriteLine($"GUIACHIP_SOLO: chip real '{chip?.Text}' visible en el arbol visual={chip?.IsVisible}");
            if (chip == null || !chip.IsVisible) Console.WriteLine($"FALLO: GUIACHIP_SOLO - el chip real no esta visible en pantalla pese a haber marcador en la capa {capa}");
            else
            {
                if (g.Siguiente == null || !chip.Text.Contains(g.Siguiente.TituloPlano, StringComparison.Ordinal))
                    Console.WriteLine($"FALLO: GUIACHIP_SOLO - el chip ('{chip.Text}') no nombra la siguiente parada ('{g.Siguiente?.TituloPlano}')");
                if (!chip.Text.Contains(vm.Loc["guia2_capa_" + capa], StringComparison.Ordinal))
                    Console.WriteLine($"FALLO: GUIACHIP_SOLO - el chip ('{chip.Text}') no dice la capa real del marcador ('{vm.Loc["guia2_capa_" + capa]}')");
                var borde = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(chip)) as Border;
                string bordeReal = (borde?.BorderBrush as SolidColorBrush)?.Color.ToString() ?? "(sin Border padre real)";
                string fondoReal = (borde?.Background as SolidColorBrush)?.Color.ToString() ?? "(sin fondo)";
                Console.WriteLine($"GUIACHIP_SOLO: borde real del chip = {bordeReal} (esperado {ColorPorCapaGuia[capa]}), fondo real = {fondoReal} (esperado neutro #FF1E2233)");
                if (bordeReal != ColorPorCapaGuia[capa]) Console.WriteLine($"FALLO: GUIACHIP_SOLO - el color real del chip no coincide con el esperado para {capa} ({bordeReal} vs {ColorPorCapaGuia[capa]})");
                if (fondoReal != "#FF1E2233") Console.WriteLine($"FALLO: GUIACHIP_SOLO - el fondo del chip no es el neutro de la familia SemanticStateChip ({fondoReal})");
            }

            // Banda: el Rectangle real de la capa, visible, opacidad 0.30 y pintado de verdad (render aislado).
            string brushEsperado = ColorPorCapaGuia[capa];
            var bandaReal = BandasGuia(window).FirstOrDefault(r => ((SolidColorBrush)r.Fill).Color.ToString() == brushEsperado);
            if (bandaReal == null) Console.WriteLine($"FALLO: GUIACHIP_SOLO-BANDA - no se encontro ningun Rectangle real con Fill={brushEsperado} para la capa {capa}");
            else
            {
                Console.WriteLine($"GUIACHIP_SOLO-BANDA: Rectangle real encontrado para {capa}, IsVisible={bandaReal.IsVisible}, Opacity real={bandaReal.Opacity}, Height real={bandaReal.ActualHeight:F1}, Width real={bandaReal.ActualWidth:F1}");
                if (!bandaReal.IsVisible) Console.WriteLine($"FALLO: GUIACHIP_SOLO-BANDA - la banda real de {capa} no esta visible");
                int otrasVisibles = BandasGuia(window).Count(r => r.IsVisible && r != bandaReal);
                if (otrasVisibles != 0) Console.WriteLine($"FALLO: GUIACHIP_SOLO-BANDA - hay {otrasVisibles} bandas de OTRAS capas visibles a la vez");

                // Render AISLADO del propio Rectangle (RenderTargetBitmap.Render(window) da falso
                // negativo con elementos de miles de unidades de ancho, ya documentado).
                var rtbBanda = new System.Windows.Media.Imaging.RenderTargetBitmap(200, 200, 96, 96, PixelFormats.Pbgra32);
                var visualBanda = new DrawingVisual();
                using (var dcBanda = visualBanda.RenderOpen())
                {
                    var vb = new VisualBrush(bandaReal) { Stretch = Stretch.None, ViewboxUnits = BrushMappingMode.Absolute, Viewbox = new Rect(0, 0, 200, 200) };
                    dcBanda.DrawRectangle(vb, null, new Rect(0, 0, 200, 200));
                }
                rtbBanda.Render(visualBanda);
                var px = new byte[200 * 200 * 4];
                rtbBanda.CopyPixels(px, 200 * 4, 0);
                byte pb = px[0], pg = px[1], pr = px[2], pa = px[3];
                Console.WriteLine($"GUIACHIP_SOLO-BANDA: render aislado real -> pixel premultiplicado (B={pb},G={pg},R={pr},A={pa})");
                if (pa == 0) Console.WriteLine($"FALLO: GUIACHIP_SOLO-BANDA - el Rectangle real de {capa} no pinta ningun pixel no-transparente en aislamiento");
                else
                {
                    double alpha = pa / 255.0;
                    int rr = (int)Math.Round(pr / alpha), gg = (int)Math.Round(pg / alpha), bb = (int)Math.Round(pb / alpha);
                    var esperado = (Color)ColorConverter.ConvertFromString(brushEsperado);
                    Console.WriteLine($"GUIACHIP_SOLO-BANDA: color real despremultiplicado=({rr},{gg},{bb}) alpha real={alpha:F2} vs color de marca ({esperado.R},{esperado.G},{esperado.B}) opacidad esperada 0.30");
                    if (Math.Abs(rr - esperado.R) > 6 || Math.Abs(gg - esperado.G) > 6 || Math.Abs(bb - esperado.B) > 6)
                        Console.WriteLine($"FALLO: GUIACHIP_SOLO-BANDA - color real de la banda de {capa} no coincide con el color de marca esperado");
                    if (Math.Abs(alpha - 0.30) > 0.03) Console.WriteLine($"FALLO: GUIACHIP_SOLO-BANDA - opacidad real pintada {alpha:F2}, esperada 0.30");
                }
            }

            ComprobarCapaGuiaEnMapa(window, vm, "GUIACHIP_SOLO", "siguiente parada");
            GuardarCaptura(window, "guiachip-capa.png", "GUIACHIP_SOLO");
        }
        catch (Exception ex) { Console.WriteLine("GUIACHIP_SOLO-EXCEPTION: " + ex); }
        finally { try { Directory.Delete(dirChip, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
}
