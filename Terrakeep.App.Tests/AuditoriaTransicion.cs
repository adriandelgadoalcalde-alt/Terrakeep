// KEEPQA_TRANSICION_SOLO=1 (16-sep-2026): cierre del sesgo S2 de
// Downloads\KeepQA\AUDITORIA-SESGOS-16SEP.md ("solo se audita la posicion inicial") para
// Terrakeep. Hasta hoy TODOS los volcados de geometria de este arnes (KEEPQA_SOLO,
// KEEPQA_VIEWPORT_SOLO, KEEPQA_VITALS_*) fotografiaban UN estado; el unico que tocaba una
// transicion (VIEWPORT_SOLO, ScrollToEnd) solo volcaba el estado FINAL, sin nada con que
// compararlo - asi que "el scroll no se ejecuto de verdad" (el falso verde real del "--desplazar"
// no-op de Starvekeep, bitacora del 15-sep) habria pasado en silencio aqui tambien.
//
// Que hace: para cuatro transiciones REALES de la app, vuelca la geometria del arbol visual
// COMPLETO de la ventana ANTES y DESPUES, y escribe cada par en el formato que consume la pieza
// compartida nueva Downloads\KeepQA\src\transicion\verificarTransicion.js ({transicion,
// objetivo, antes, despues}):
//   1. hover    - tooltip REAL de un slot de Inventario con objeto (el <ToolTip> de MainWindow.xaml
//                 ~L405, abierto con IsOpen=true sobre su PlacementTarget real). El tooltip vive en
//                 un Popup (arbol visual APARTE de la ventana, mismo hecho ya documentado en
//                 AuditoriaKeepQA.cs para WhereIsItPopup), asi que se mide por PointToScreen y se
//                 añade al volcado "despues" como hijo de la raiz. Lo que caza: tooltip fuera de la
//                 ventana, tooltip vacio, tooltip que tapa el slot, y cualquier cosa de la ventana
//                 que se mueva al pasar el raton (no deberia moverse nada).
//   2. scroll   - selector de peinado (Appearance.HairOptions, 228 miniaturas, ScrollViewer
//                 MaxHeight=360): ScrollToHome -> antes, ScrollToEnd -> despues. Lo que caza:
//                 que el scroll SE EJECUTO (sin_efecto si no), que el ScrollViewer no cambio de
//                 caja y que nada fuera de el se movio.
//   3. tamano   - Personaje/Inventario a 1180x860 (diseño) -> MinWidth x MinHeight reales de la
//                 ventana (1080x700). Lo que caza: elementos que DESAPARECEN al reducir (la franja
//                 de vitales real del 15-sep, caso terrakeep-franja-vital-desaparecia-ventana-
//                 reducida - un bug que encontro el usuario a ojo) y contenciones/solapes que solo
//                 existen a tamaño minimo.
//   4. idioma   - misma pestaña, es -> en (LocalizationService.SetLanguage). Lo que caza: textos que
//                 en ingles desbordan o pisan a su vecino, y elementos que solo existen en un
//                 idioma.
// Cada par deja ademas una captura real del estado "despues" (RenderTargetBitmap de la ventana -
// OJO: NO incluye el Popup del tooltip, por el mismo motivo de arriba; el dato del tooltip esta en
// el JSON, no en el PNG).
//
// Identificadores ESTABLES entre los dos volcados: ruta en el arbol visual (id del padre + "/" +
// x:Name si lo tiene, o Tipo#indice entre hermanos del mismo tipo) - mismo esquema exacto que
// Keep.Wpf\GeometriaWpf.cs (Starvekeep/ServidorKeep) y GeometriaControles.cs (TerrakeepTrainer),
// para que verificarTransicion.js pueda emparejar "el mismo elemento" en los dos estados sin
// ningun metadato extra. No reimplementa RectCompleto/OrdenZ/Descendientes/DoEvents: reutiliza los
// de AuditoriaMaquetacion.cs/Program.cs.
//
// Como se ejecuta y se verifica (desde la raiz del repo, con el Terrakeep.exe del usuario abierto
// no se puede compilar a bin\ - usar BaseOutputPath aparte, ver bitacora):
//   dotnet build Terrakeep.App.Tests -c Debug -p:BaseOutputPath=bin_keepqaDebug/
//   KEEPQA_TRANSICION_SOLO=1 Terrakeep.App.Tests\bin_keepqaDebug\Debug\net10.0-windows\Terrakeep.App.Tests.exe
//   node Downloads\KeepQA\src\transicion\verificarTransicion.js <keepqa-evidencia>\transicion-hover.par.json --formato-espec
//   (idem scroll/tamano/idioma)
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Terrakeep.App;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    /// <summary>Carpeta de evidencia de todo el arnes (AppContext.BaseDirectory\keepqa-evidencia).</summary>
    private static string CarpetaEvidenciaKeepQa()
    {
        string outDir = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
        Directory.CreateDirectory(outDir);
        return outDir;
    }

    /// <summary>Captura real de la ventana (RenderTargetBitmap) a keepqa-evidencia\nombre.png -
    /// misma tecnica que Captura() de AuditoriaKeepQA.cs/AuditoriaViewportScroll.cs, sacada a metodo
    /// para poder reutilizarla desde cualquier modo _SOLO (Hosting incluido).</summary>
    private static string CapturaVentanaKeepQa(Window window, string nombre)
    {
        string outDir = CarpetaEvidenciaKeepQa();
        var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
            Math.Max(1, (int)window.ActualWidth), Math.Max(1, (int)window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        rtb.Render(window);
        var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
        enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
        string ruta = Path.Combine(outDir, nombre + ".png");
        using (var fs = File.Create(ruta)) enc.Save(fs);
        return ruta;
    }

    /// <summary>
    /// Volcado GENERICO del arbol visual completo de `raiz` al contrato de verificarGeometria.js
    /// (id, tipo, padre_id, x, y, ancho, alto, orden_z, capa?, viewportAlto?, viewportAncho?,
    /// extentAncho?), coordenadas relativas a `raiz`. `idsPorElemento` (opcional) devuelve el id
    /// asignado a cada FrameworkElement, para poder señalar un objetivo (el slot bajo el raton, el
    /// ScrollViewer desplazado) por su id real del volcado.
    /// </summary>
    private static List<Dictionary<string, object?>> VolcarArbolVisual(FrameworkElement raiz, string idRaiz, Dictionary<FrameworkElement, string>? idsPorElemento = null)
    {
        var lista = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["id"] = idRaiz, ["tipo"] = raiz.GetType().Name, ["padre_id"] = null,
                ["x"] = 0.0, ["y"] = 0.0, ["ancho"] = Math.Round(raiz.ActualWidth, 2), ["alto"] = Math.Round(raiz.ActualHeight, 2),
                ["orden_z"] = 0.0, ["capa"] = "fondo",
            },
        };
        RecorrerArbolVisual(raiz, raiz, idRaiz, lista, new Dictionary<string, int>(), idsPorElemento);
        // La PANTALLA (area de trabajo real, en coordenadas de la ventana) como segunda raiz: en una
        // app de escritorio un ToolTip/Popup puede salir legitimamente de la VENTANA (es una ventana
        // propia del SO), pero nunca de la pantalla - verificarTransicion.js juzga "aparecido_fuera"
        // contra la raiz de mayor area, asi que con esto el limite pasa a ser el correcto. Presente
        // en los dos volcados (existe siempre), asi nunca cuenta como "aparecido". Si la ventana no
        // esta presentada (sin PresentationSource) no se puede situar y se omite.
        if (raiz is Window w && PresentationSource.FromVisual(w) is { CompositionTarget: not null } ps)
        {
            var aDip = ps.CompositionTarget.TransformFromDevice;
            Point origenVentana = aDip.Transform(w.PointToScreen(new Point(0, 0)));
            Rect area = SystemParameters.WorkArea; // ya en DIPs
            lista.Add(new Dictionary<string, object?>
            {
                ["id"] = "pantalla", ["tipo"] = "AreaDeTrabajo", ["padre_id"] = null,
                ["x"] = Math.Round(area.X - origenVentana.X, 2), ["y"] = Math.Round(area.Y - origenVentana.Y, 2),
                ["ancho"] = Math.Round(area.Width, 2), ["alto"] = Math.Round(area.Height, 2),
                ["orden_z"] = -1.0, ["capa"] = "fondo",
            });
        }
        return lista;
    }

    private static void RecorrerArbolVisual(FrameworkElement raizAbsoluta, DependencyObject nodo, string idPadre,
        List<Dictionary<string, object?>> lista, Dictionary<string, int> contador, Dictionary<FrameworkElement, string>? ids)
    {
        string idParaHijos = idPadre;
        if (nodo is FrameworkElement fe && !ReferenceEquals(nodo, raizAbsoluta))
        {
            // Visibility (no IsVisible) y corte del subarbol: mismo criterio que GeometriaWpf.Recorrer.
            if (fe.Visibility != Visibility.Visible) return;
            string tipo = fe.GetType().Name;
            // AdornerLayer: capa interna de WPF, siempre del tamaño de su host, nunca informativa
            // (21 de 28 solapes falsos en el primer volcado real de Starvekeep, 14-sep-2026).
            if (tipo != "AdornerLayer" && fe.ActualWidth >= 1 && fe.ActualHeight >= 1)
            {
                Rect r;
                try { r = RectCompleto(fe, raizAbsoluta); }
                catch (InvalidOperationException) { return; } // no es descendiente visual real (Popup)
                string clave = idPadre + " " + tipo;
                contador.TryGetValue(clave, out int n);
                contador[clave] = n + 1;
                string id = idPadre + "/" + (string.IsNullOrEmpty(fe.Name) ? tipo + "#" + n : fe.Name);
                var d = new Dictionary<string, object?>
                {
                    ["id"] = id, ["tipo"] = tipo, ["padre_id"] = idPadre,
                    ["x"] = Math.Round(r.X, 2), ["y"] = Math.Round(r.Y, 2), ["ancho"] = Math.Round(r.Width, 2), ["alto"] = Math.Round(r.Height, 2),
                    ["orden_z"] = OrdenZ(fe),
                };
                // Contenedor de scroll: contrato de verificarBordeViewport.js/
                // verificarDesbordamientoHorizontal.js, con los dos gotchas reales ya documentados
                // en Keep.Wpf\GeometriaWpf.ViewportDe (PART_ContentHost de TextBox/PasswordBox no es
                // un contenedor de diseño; con ScrollUnit=Item ViewportHeight esta en ITEMS, no px).
                if (fe is ScrollViewer sv && sv.TemplatedParent is not (System.Windows.Controls.Primitives.TextBoxBase or PasswordBox))
                {
                    bool porItem = sv.CanContentScroll && Descendientes<VirtualizingPanel>(sv).Take(1).Any(p => VirtualizingPanel.GetScrollUnit(p) == ScrollUnit.Item);
                    if (sv.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled) d["viewportAlto"] = Math.Round(porItem ? sv.ActualHeight : sv.ViewportHeight, 2);
                    if (sv.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
                    {
                        d["viewportAncho"] = Math.Round(porItem ? sv.ActualWidth : sv.ViewportWidth, 2);
                        if (!porItem) d["extentAncho"] = Math.Round(sv.ExtentWidth, 2);
                    }
                }
                lista.Add(d);
                if (ids != null) ids[fe] = id;
                idParaHijos = id;
                // Una ScrollBar es un control de PLANTILLA del framework: sus partes internas
                // (PART_Track, Thumb, RepeatButton, el Border de 2px de la pista) no son diseño de
                // la app y se pisan entre si por construccion de WPF (cazado como falso "solape
                // inducido" de 17x2px en el primer volcado real a tamaño minimo). Se vuelca la
                // ScrollBar entera como una caja y no se desciende - mismo criterio que
                // AdornerLayer.
                if (fe is System.Windows.Controls.Primitives.ScrollBar) return;
            }
        }
        int hijos = VisualTreeHelper.GetChildrenCount(nodo);
        for (int i = 0; i < hijos; i++) RecorrerArbolVisual(raizAbsoluta, VisualTreeHelper.GetChild(nodo, i), idParaHijos, lista, contador, ids);
    }

    private static void EscribirParTransicion(string outDir, string transicion, string? objetivo, bool esperaCambio,
        List<Dictionary<string, object?>> antes, List<Dictionary<string, object?>> despues)
    {
        var par = new Dictionary<string, object?>
        {
            ["transicion"] = transicion, ["objetivo"] = objetivo, ["esperaCambio"] = esperaCambio,
            ["antes"] = antes, ["despues"] = despues,
        };
        string ruta = Path.Combine(outDir, $"transicion-{transicion}.par.json");
        File.WriteAllText(ruta, JsonSerializer.Serialize(par, new JsonSerializerOptions { WriteIndented = false, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        Console.WriteLine($"TRANSICION-{transicion}: antes={antes.Count} despues={despues.Count} objetivo={objetivo ?? "(ninguno)"} -> {ruta}");
    }

    private static void EjecutarKeepQaTransicionSolo(Window window, MainViewModel vm)
    {
        string outDir = CarpetaEvidenciaKeepQa();
        const string idRaiz = "ventana";

        static ScrollViewer? ScrollViewerAncestroDe(DependencyObject? d)
        {
            while (d != null)
            {
                if (d is ScrollViewer sv) return sv;
                d = VisualTreeHelper.GetParent(d);
            }
            return null;
        }

        // ------------------------------------------------------------------------------------
        // 1. HOVER: tooltip real de un slot de Inventario con objeto.
        // ------------------------------------------------------------------------------------
        try
        {
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 1; // Personaje/Objetos/Inventario
            DoEvents(); DoEvents();

            // El slot real: un Border con <Border.ToolTip><ToolTip .../> y ToolTipService.IsEnabled
            // (ligado a ShowTooltip, true solo con objeto dentro - MainWindow.xaml ~L398-405).
            var slot = Descendientes<Border>(window).FirstOrDefault(b =>
                b.IsVisible && b.ActualWidth > 1 && b.ToolTip is ToolTip && ToolTipService.GetIsEnabled(b)
                && b.DataContext?.GetType().GetProperty("IsEmpty")?.GetValue(b.DataContext) is false);
            if (slot == null)
            {
                Console.WriteLine("TRANSICION-hover: ningun slot visible con objeto y ToolTip real - omitido (¿personaje sin objetos en Inventario?)");
            }
            else
            {
                var ids = new Dictionary<FrameworkElement, string>();
                var antes = VolcarArbolVisual(window, idRaiz, ids);
                string objetivo = ids[slot];

                var tt = (ToolTip)slot.ToolTip;
                var placementAntes = tt.Placement;
                tt.PlacementTarget = slot;
                // Sin Placement explicito, un ToolTip abierto por codigo usa PlacementMode.Mouse:
                // la posicion REAL del cursor del usuario en ese instante (en la primera pasada
                // real salio en y=839 de una ventana de 860 - el raton estaba fuera, no el tooltip
                // mal colocado). Bottom sobre el slot reproduce lo que ve un usuario con el raton
                // encima del slot, de forma determinista.
                tt.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                tt.IsOpen = true;
                WaitForDispatcher(150);
                DoEvents(); DoEvents();

                var despues = VolcarArbolVisual(window, idRaiz);
                // El ToolTip vive en un Popup: se mide por pantalla y se traduce al sistema de la
                // ventana (PointToScreen devuelve pixeles de DISPOSITIVO - hay que deshacer el DPI
                // con TransformFromDevice, si no a 125%/150% la caja saldria desplazada y agrandada).
                var origenPantalla = PresentationSource.FromVisual(window)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
                Point pVentana = origenPantalla.Transform(window.PointToScreen(new Point(0, 0)));
                Rect cajaTooltip = Rect.Empty;
                if (tt.IsOpen && tt.ActualWidth > 0 && PresentationSource.FromVisual(tt) != null)
                {
                    Point pTooltip = origenPantalla.Transform(tt.PointToScreen(new Point(0, 0)));
                    cajaTooltip = new Rect(pTooltip.X - pVentana.X, pTooltip.Y - pVentana.Y, tt.ActualWidth, tt.ActualHeight);
                }
                despues.Add(new Dictionary<string, object?>
                {
                    ["id"] = "tooltip_slot", ["tipo"] = "ToolTip", ["padre_id"] = idRaiz,
                    ["x"] = Math.Round(cajaTooltip.IsEmpty ? 0 : cajaTooltip.X, 2), ["y"] = Math.Round(cajaTooltip.IsEmpty ? 0 : cajaTooltip.Y, 2),
                    ["ancho"] = Math.Round(cajaTooltip.IsEmpty ? 0 : cajaTooltip.Width, 2), ["alto"] = Math.Round(cajaTooltip.IsEmpty ? 0 : cajaTooltip.Height, 2),
                    ["orden_z"] = 1e9, ["capa"] = "overlay",
                });
                Console.WriteLine($"TRANSICION-hover: slot={objetivo} tooltip IsOpen={tt.IsOpen} caja={(cajaTooltip.IsEmpty ? "(no medible - IsOpen o PresentationSource nulos)" : cajaTooltip.ToString())} ventana={window.ActualWidth}x{window.ActualHeight}");
                CapturaVentanaKeepQa(window, "transicion-hover-despues");
                EscribirParTransicion(outDir, "hover", objetivo, esperaCambio: true, antes, despues);

                tt.IsOpen = false;
                tt.Placement = placementAntes;
                DoEvents();
            }
        }
        catch (Exception ex) { Console.WriteLine("TRANSICION-hover-EXCEPTION: " + ex); }

        // ------------------------------------------------------------------------------------
        // 2. SCROLL: selector de peinado, inicio -> final.
        // ------------------------------------------------------------------------------------
        try
        {
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 3; // Apariencia
            DoEvents();
            vm.Appearance.OpenHairPickerCommand.Execute(null);
            DoEvents(); DoEvents();
            var icHair = Descendientes<ItemsControl>(window).FirstOrDefault(x => ReferenceEquals(x.ItemsSource, vm.Appearance.HairOptions));
            var svHair = ScrollViewerAncestroDe(icHair);
            if (svHair == null)
            {
                Console.WriteLine("TRANSICION-scroll: ScrollViewer real del selector de peinado no encontrado - omitido");
            }
            else
            {
                svHair.ScrollToHome();
                DoEvents(); DoEvents();
                var ids = new Dictionary<FrameworkElement, string>();
                var antes = VolcarArbolVisual(window, idRaiz, ids);
                string objetivo = ids.TryGetValue(svHair, out var idSv) ? idSv : "(ScrollViewer sin id)";

                svHair.ScrollToEnd();
                DoEvents(); DoEvents();
                var despues = VolcarArbolVisual(window, idRaiz);
                Console.WriteLine($"TRANSICION-scroll: {objetivo} ScrollableHeight={svHair.ScrollableHeight:0.#} VerticalOffset={svHair.VerticalOffset:0.#} (esperado ~= ScrollableHeight)");
                CapturaVentanaKeepQa(window, "transicion-scroll-despues");
                EscribirParTransicion(outDir, "scroll", objetivo, esperaCambio: true, antes, despues);
            }
            vm.Appearance.CloseHairPickerCommand.Execute(null);
            DoEvents();
        }
        catch (Exception ex) { Console.WriteLine("TRANSICION-scroll-EXCEPTION: " + ex); }

        // ------------------------------------------------------------------------------------
        // 3. TAMAÑO: Inventario a tamaño de diseño -> minimo real de la ventana.
        // ------------------------------------------------------------------------------------
        double anchoAntes = window.Width, altoAntes = window.Height;
        try
        {
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 1;
            window.Width = 1180; window.Height = 860;
            DoEvents(); DoEvents();
            var antes = VolcarArbolVisual(window, idRaiz);
            window.Width = window.MinWidth; window.Height = window.MinHeight;
            DoEvents(); DoEvents(); WaitForDispatcher(100); DoEvents();
            var despues = VolcarArbolVisual(window, idRaiz);
            Console.WriteLine($"TRANSICION-tamano: {antes[0]["ancho"]}x{antes[0]["alto"]} -> {despues[0]["ancho"]}x{despues[0]["alto"]} (MinWidth={window.MinWidth}, MinHeight={window.MinHeight})");
            CapturaVentanaKeepQa(window, "transicion-tamano-despues");
            EscribirParTransicion(outDir, "tamano", null, esperaCambio: true, antes, despues);
        }
        catch (Exception ex) { Console.WriteLine("TRANSICION-tamano-EXCEPTION: " + ex); }
        finally { window.Width = anchoAntes; window.Height = altoAntes; DoEvents(); DoEvents(); }

        // ------------------------------------------------------------------------------------
        // 4. IDIOMA: es -> en, misma pestaña.
        // ------------------------------------------------------------------------------------
        string idiomaAntes = LocalizationService.Instance.Language;
        try
        {
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 1;
            LocalizationService.Instance.SetLanguage(LocalizationService.Spanish);
            DoEvents(); DoEvents();
            var antes = VolcarArbolVisual(window, idRaiz);
            LocalizationService.Instance.SetLanguage(LocalizationService.English);
            DoEvents(); DoEvents(); WaitForDispatcher(100); DoEvents();
            var despues = VolcarArbolVisual(window, idRaiz);
            Console.WriteLine($"TRANSICION-idioma: es -> en, Language ahora='{LocalizationService.Instance.Language}'");
            CapturaVentanaKeepQa(window, "transicion-idioma-despues");
            EscribirParTransicion(outDir, "idioma", null, esperaCambio: true, antes, despues);
        }
        catch (Exception ex) { Console.WriteLine("TRANSICION-idioma-EXCEPTION: " + ex); }
        finally { LocalizationService.Instance.SetLanguage(idiomaAntes); DoEvents(); }

        Console.WriteLine($"TRANSICION: evidencia (pares .par.json + capturas) en {outDir}");
    }
}
