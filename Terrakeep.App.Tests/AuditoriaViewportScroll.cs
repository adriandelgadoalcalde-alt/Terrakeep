// KEEPQA_VIEWPORT_SOLO=1 (15-sep-2026): pieza NUEVA y reutilizable del arnes, encargo del
// coordinador (barrido de calidad visual con el arsenal completo de KeepQA, incluida la pieza
// nueva src/viewport-scroll/verificarBordeViewport.js construida esa misma noche por otro
// equipo). Objetivo: cubrir el hueco real que verificarBordeViewport.js documenta en su propia
// cabecera - "el volcado de geometria de cada panel se hizo en su estado INICIAL, nadie desplazo
// la lista hasta el final antes de volcar" (el bug real que motivo esa pieza fue justo eso, en
// StarvekeepMod). Aqui, para Terrakeep: localizar cada ScrollViewer real con contenido largo,
// desplazarlo DE VERDAD hasta el final (ScrollToEnd()), y volcar la geometria real de su ultima
// pagina de hijos visibles con el campo viewportAlto (ViewportHeight real del ScrollViewer) -
// contrato exacto que consume verificarBordeViewport.js. Misma clase parcial, mismo patron que
// AuditoriaKeepQA.cs/AuditoriaMaquetacion.cs (Program.cs ya tiene miles de lineas).
//
// Identificacion de cada ScrollViewer: por REFERENCIA REAL del ItemsSource/Items contra la
// coleccion real del ViewModel (ReferenceEquals), nunca por x:Name inventado ni por texto fragil -
// ninguno de estos ScrollViewer/ItemsControl de MainWindow.xaml tiene x:Name propio hoy, y anadir
// uno solo para el arnes seria tocar produccion sin necesidad (la referencia real de la coleccion
// ya es un identificador inequivoco, mismo criterio que CAPAS_SOLO usa VisualChildIndex real en
// vez de inventar metadatos).
//
// Cinco paneles reales cubiertos (los cinco con contenido genuinamente largo/con scroll real de
// la app, elegidos tras grep real de "ScrollViewer" en MainWindow.xaml - no una lista arbitraria):
//   1. Panel "Editar" de un objeto con muchos prefijos legales (mismo escenario real que L-d en
//      Program.cs: meta "Positivos" + grupo "Cuerpo a cuerpo +" sobre un arma real, ventana a
//      700px de alto) - reconstruido aqui de forma AUTONOMA (no depende de que L-d haya corrido
//      antes) para que este modo _SOLO sea independiente y rapido.
//   2. Selector de tinte de pelo (Appearance.HairDyeOptions, ScrollViewer MaxHeight=240).
//   3. Selector de peinado (Appearance.HairOptions, ScrollViewer MaxHeight=360, 228 miniaturas).
//   4. Rejilla de resultados de la Biblioteca de objetos (Library.Results, SlotGridPanel, hasta
//      100 resultados con SearchText amplio).
//   5. Lista de resultados de busqueda de Exploracion (Exploration.WorldSearchResults, ListBox
//      virtualizado con scroll interno propio, mundo real roca_negra.wld, busqueda "lava" - mismo
//      mundo/busqueda que ya usa el resto del arnes en X-7/BUSCADOR-MUNDO).
//
// Pieza declarada explicitamente REUTILIZABLE (no un experimento a medias): queda en el arbol
// permanentemente, mismo criterio que KEEPQA_SOLO/CAPAS_SOLO/FALLO3_SOLO - un _SOLO mas del
// catalogo ya establecido en Program.cs, documentado aqui y enganchado alli en una sola linea.
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Terrakeep.App;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static void EjecutarKeepQaViewportScrollSolo(Window window, MainViewModel vm)
    {
        string outDir = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
        Directory.CreateDirectory(outDir);
        var elementos = new List<object>();

        void Captura(string nombre)
        {
            var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtb.Render(window);
            var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
            using var fs = File.Create(Path.Combine(outDir, nombre + ".png"));
            enc.Save(fs);
            Console.WriteLine($"VIEWPORT: captura -> {nombre}.png");
        }

        static ScrollViewer? BuscarScrollViewerAncestro(DependencyObject? d)
        {
            while (d != null)
            {
                if (d is ScrollViewer sv) return sv;
                d = System.Windows.Media.VisualTreeHelper.GetParent(d);
            }
            return null;
        }

        // Vuelca UN ScrollViewer real ya localizado + su ultima pagina de hijos REALIZADOS
        // (ItemContainerGenerator.ContainerFromIndex - null para los no realizados en un panel
        // virtualizado, exactamente lo que queremos: solo lo que hay de verdad en pantalla tras
        // el ScrollToEnd real). No reimplementa nada de RectCompleto/OrdenZ, reutiliza los
        // helpers ya existentes de AuditoriaMaquetacion.cs/Program.cs.
        void VolcarViewport(string idViewport, string tipoViewport, ScrollViewer? sv, ItemsControl? ic, string capaHijos)
        {
            if (sv == null || ic == null)
            {
                Console.WriteLine($"VIEWPORT-{idViewport}: ScrollViewer/ItemsControl real no encontrado - omitido");
                return;
            }
            sv.ScrollToEnd();
            DoEvents(); DoEvents();

            Rect rSv;
            try { rSv = RectCompleto(sv, window); }
            catch (InvalidOperationException ex) { Console.WriteLine($"VIEWPORT-{idViewport}: RectCompleto del ScrollViewer fallo - {ex.Message}"); return; }

            elementos.Add(new
            {
                id = idViewport,
                tipo = tipoViewport,
                padre_id = (string?)null,
                x = rSv.X,
                y = rSv.Y,
                ancho = rSv.Width,
                alto = rSv.Height,
                grupo = (string?)null,
                orden_z = OrdenZ(sv),
                capa = "panel",
                viewportAlto = sv.ViewportHeight,
                // 16-sep-2026 (hallazgo real de Fable, KeepQA/src/desbordamiento-horizontal): este
                // extractor SOLO volcaba el eje vertical desde que se escribio (15-sep-2026) -
                // sesgo de eje, no una omision puntual: verificarBordeViewport.js nacio de un bug
                // de "ultima fila pegada al final del scroll" (vertical) y el eje X de un
                // ScrollViewer nunca se midio en ningun extractor de la familia. Contrato real que
                // consume verificarDesbordamientoHorizontal.js (nuevo): viewportAncho=ViewportWidth,
                // extentAncho=ExtentWidth - con los dos presentes, esa pieza decide con la propia
                // aritmetica del motor (extentAncho>viewportAncho => scroll lateral real), sin
                // depender de que los hijos volcados abajo esten completos.
                viewportAncho = sv.ViewportWidth,
                extentAncho = sv.ExtentWidth,
            });

            int n = 0;
            for (int i = 0; i < ic.Items.Count; i++)
            {
                if (ic.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement cont) continue;
                if (!cont.IsVisible || cont.ActualWidth < 1 || cont.ActualHeight < 1) continue;
                Rect r;
                try { r = RectCompleto(cont, window); }
                catch (InvalidOperationException) { continue; }
                elementos.Add(new
                {
                    id = $"{idViewport}_item{i}",
                    tipo = "item",
                    padre_id = (string?)idViewport,
                    x = r.X,
                    y = r.Y,
                    ancho = r.Width,
                    alto = r.Height,
                    grupo = (string?)$"{idViewport}_items",
                    orden_z = OrdenZ(cont),
                    capa = capaHijos,
                });
                n++;
            }
            Console.WriteLine($"VIEWPORT-{idViewport}: ScrollViewer real, ViewportHeight={sv.ViewportHeight:0.#}px ScrollableHeight={sv.ScrollableHeight:0.#}px VerticalOffset={sv.VerticalOffset:0.#}px (esperado ~=ScrollableHeight tras ScrollToEnd), {n}/{ic.Items.Count} hijos REALIZADOS volcados como ultima pagina visible");
        }

        // ---------------------------------------------------------------------------------
        // 1. Panel "Editar" con muchos prefijos legales (mismo peor caso real que L-d).
        // ---------------------------------------------------------------------------------
        try
        {
            window.Height = 700;
            vm.SelectedTabIndex = 1; // Personaje
            vm.PersonajeInnerTabIndex = 0; // Objetos
            vm.ObjetosSubTabIndex = 1; // Inventario
            DoEvents();
            var slotParaPrefijos = vm.InventoryContainer?.Slots.FirstOrDefault(s => s.IsEmpty);
            if (slotParaPrefijos == null)
            {
                Console.WriteLine("VIEWPORT-editor_prefijos: sin slot de Inventario vacio real - omitido");
            }
            else
            {
                slotParaPrefijos.PlaceItem(4); // Iron Broadsword, arma real (Melee)
                vm.SelectSlot(slotParaPrefijos);
                DoEvents();
                var positivos = vm.ItemEdit.Metas.FirstOrDefault(m => m.Label.Contains("Positivos", StringComparison.OrdinalIgnoreCase));
                if (positivos == null)
                {
                    Console.WriteLine("VIEWPORT-editor_prefijos: meta 'Positivos' no encontrada - omitido");
                }
                else
                {
                    vm.ItemEdit.SelectMetaCommand.Execute(positivos);
                    DoEvents();
                    var meleePlus = vm.ItemEdit.Groups.FirstOrDefault(g => g.Label.Contains("Cuerpo a cuerpo", StringComparison.OrdinalIgnoreCase));
                    if (meleePlus != null) vm.ItemEdit.SelectGroupCommand.Execute(meleePlus);
                    DoEvents(); DoEvents();

                    // OJO real, encontrado en ESTA misma ronda antes de fiarse del primer intento
                    // (por texto "Editar"): el panel Editar tiene DOS ScrollViewer anidados
                    // (MainWindow.xaml ~L33, el "ScrollViewer de seguridad" de L-d que envuelve el
                    // panel Editar ENTERO, y ~L200, uno interior que envuelve SOLO la rejilla de
                    // chips de prefijo, con Margin="0,0,0,-16" - un margen NEGATIVO real,
                    // sospechoso de por si). Buscar por "contiene el texto Editar" siempre
                    // encuentra el EXTERIOR (el interior tambien esta bajo el exterior, pero la
                    // busqueda desciende y para en el primero que cumple la condicion) - eso mide
                    // el hueco contra un StackPanel-contenedor grande (metas+grupos+prefijos como
                    // un unico bloque), no contra los chips de prefijo reales, dato inutil para lo
                    // que este arnes quiere cazar. Localizar en su lugar el ScrollViewer INTERIOR
                    // real por REFERENCIA de coleccion (vm.ItemEdit.Prefixes, mismo criterio
                    // ReferenceEquals que el resto de esta pieza) - ese es el que de verdad
                    // recorta la lista de chips.
                    var icPrefijos = Descendientes<ItemsControl>(window).FirstOrDefault(x => ReferenceEquals(x.ItemsSource, vm.ItemEdit.Prefixes));
                    var scrollPrefijos = BuscarScrollViewerAncestro(icPrefijos);
                    if (scrollPrefijos == null || icPrefijos == null)
                    {
                        Console.WriteLine("VIEWPORT-editor_prefijos: ScrollViewer/ItemsControl real de la rejilla de prefijos no encontrado - omitido");
                    }
                    else
                    {
                        // El ScrollViewer de SEGURIDAD exterior (L33) envuelve el panel Editar
                        // ENTERO - si no se desplaza tambien EL, con solo 10 prefijos reales la
                        // rejilla de chips (el interior) puede quedar fuera de la parte visible
                        // del panel (por debajo del pliegue), y tanto la captura como cualquier
                        // "no hubo que hacer scroll interno" serian enganosos: el propio L-d
                        // (Program.cs) es precisamente la prueba de que ES el exterior el que
                        // desborda a 700px, no necesariamente el interior. Desplazar el exterior
                        // primero (ancestro-de-un-ancestro de scrollPrefijos, nunca el mismo) para
                        // que la captura y el volcado reflejen el estado real "desplazado hasta el
                        // final" de verdad.
                        var scrollSeguridad = BuscarScrollViewerAncestro(System.Windows.Media.VisualTreeHelper.GetParent(scrollPrefijos));
                        if (scrollSeguridad != null)
                        {
                            scrollSeguridad.ScrollToEnd();
                            DoEvents(); DoEvents();
                            Rect rSeg;
                            try { rSeg = RectCompleto(scrollSeguridad, window); } catch (InvalidOperationException) { rSeg = Rect.Empty; }
                            elementos.Add(new { id = "editor_seguridad_exterior", tipo = "scrollviewer_editar_completo", padre_id = (string?)null, x = rSeg.X, y = rSeg.Y, ancho = rSeg.Width, alto = rSeg.Height, grupo = (string?)null, orden_z = OrdenZ(scrollSeguridad), capa = "panel", viewportAlto = scrollSeguridad.ViewportHeight, viewportAncho = scrollSeguridad.ViewportWidth, extentAncho = scrollSeguridad.ExtentWidth });
                            Console.WriteLine($"VIEWPORT-editor_prefijos: ScrollViewer de seguridad (exterior) real, caja={rSeg}, ViewportHeight={scrollSeguridad.ViewportHeight:0.#}px ScrollableHeight={scrollSeguridad.ScrollableHeight:0.#}px VerticalOffset={scrollSeguridad.VerticalOffset:0.#}px tras ScrollToEnd (L-d: confirma si el panel Editar ENTERO desborda a 700px)");
                        }
                        else
                        {
                            Console.WriteLine("VIEWPORT-editor_prefijos: ScrollViewer de seguridad exterior no encontrado - solo se desplaza/vuelca el interior");
                        }
                        VolcarViewport("editor_prefijos", "scrollviewer_prefijos", scrollPrefijos, icPrefijos, "contenido");
                        Captura("viewport-editor-prefijos-700px");
                    }
                }
            }
        }
        catch (Exception ex) { Console.WriteLine("VIEWPORT-editor_prefijos-EXCEPTION: " + ex); }
        finally { window.Height = 860; DoEvents(); }

        // ---------------------------------------------------------------------------------
        // 2 y 3. Selectores de tinte de pelo / peinado (Apariencia).
        // ---------------------------------------------------------------------------------
        try
        {
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 3; // Apariencia
            DoEvents();

            vm.Appearance.OpenHairDyePickerCommand.Execute(null);
            DoEvents(); DoEvents();
            var icDye = Descendientes<ItemsControl>(window).FirstOrDefault(x => ReferenceEquals(x.ItemsSource, vm.Appearance.HairDyeOptions));
            var svDye = BuscarScrollViewerAncestro(icDye);
            VolcarViewport("selector_tinte_pelo", "scrollviewer_wrap", svDye, icDye, "contenido");
            Captura("viewport-selector-tinte-pelo");
            vm.Appearance.CloseHairDyePickerCommand.Execute(null);
            DoEvents();

            vm.Appearance.OpenHairPickerCommand.Execute(null);
            DoEvents(); DoEvents();
            var icHair = Descendientes<ItemsControl>(window).FirstOrDefault(x => ReferenceEquals(x.ItemsSource, vm.Appearance.HairOptions));
            var svHair = BuscarScrollViewerAncestro(icHair);
            VolcarViewport("selector_peinado", "scrollviewer_wrap", svHair, icHair, "contenido");
            Captura("viewport-selector-peinado");
            vm.Appearance.CloseHairPickerCommand.Execute(null);
            DoEvents();
        }
        catch (Exception ex) { Console.WriteLine("VIEWPORT-apariencia-EXCEPTION: " + ex); }

        // ---------------------------------------------------------------------------------
        // 4. Rejilla de resultados de la Biblioteca de objetos.
        // ---------------------------------------------------------------------------------
        try
        {
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 1; // Inventario
            DoEvents();
            var slotLib = vm.InventoryContainer?.Slots.FirstOrDefault(s => s.IsEmpty);
            if (slotLib == null)
            {
                Console.WriteLine("VIEWPORT-biblioteca_resultados: sin slot vacio real para abrir la Biblioteca - omitido");
            }
            else
            {
                slotLib.ChooseFromLibraryCommand.Execute(null);
                DoEvents();
                // "o" solo (1 caracter) se ignora por la gramatica real de busqueda
                // (LibrarySearchGrammar.Matches: "un termino de menos de 2 caracteres se ignora
                // por completo", quirk real heredado de Terrasavr) - "es" (2+, muy comun en
                // nombres reales en espanol) fuerza ShowRootCategoryCards=false y llena Results.
                vm.Library.SearchText = "es";
                // La busqueda de la Biblioteca tambien tiene debounce real (180ms,
                // CatalogBrowserViewModel.SearchDebounceTimer, DispatcherTimer real - necesita
                // tiempo de reloj real, no solo bombeo de mensajes) - mismo motivo que el fallo
                // ya corregido arriba en Exploracion. WaitForDispatcher (no DoEvents) dispara el
                // Tick de verdad.
                WaitForDispatcher(250);
                Console.WriteLine($"VIEWPORT-biblioteca_resultados: SearchText='{vm.Library.SearchText}' SlotRestrictionLabel='{vm.Library.SlotRestrictionLabel}' ResultsSummary='{vm.Library.ResultsSummary}' -> Library.Results.Count={vm.Library.Results.Count} (esperado hasta 100), ShowRootCategoryCards={vm.Library.ShowRootCategoryCards} (esperado False)");
                var icLib = Descendientes<ItemsControl>(window).FirstOrDefault(x => ReferenceEquals(x.ItemsSource, vm.Library.Results));
                var svLib = BuscarScrollViewerAncestro(icLib);
                VolcarViewport("biblioteca_resultados", "scrollviewer_rejilla", svLib, icLib, "contenido");
                Captura("viewport-biblioteca-resultados");
                vm.Library.SearchText = "";
                vm.Library.CancelPickCommand.Execute(null);
                DoEvents();
            }
        }
        catch (Exception ex) { Console.WriteLine("VIEWPORT-biblioteca_resultados-EXCEPTION: " + ex); }

        // ---------------------------------------------------------------------------------
        // 5. Lista de resultados de busqueda de Exploracion (mundo real roca_negra.wld).
        // ---------------------------------------------------------------------------------
        try
        {
            string worldPath = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld";
            if (!File.Exists(worldPath))
            {
                Console.WriteLine("VIEWPORT-exploracion_resultados: mundo real roca_negra.wld no encontrado - omitido");
            }
            else
            {
                vm.SelectedTabIndex = 4; // Exploracion
                DoEvents();
                var task = vm.Exploration.LoadFromPathAsync(worldPath);
                while (!task.IsCompleted) DoEvents();
                if (task.IsFaulted) throw task.Exception!;
                DoEvents();

                // La busqueda real tiene debounce (250ms) antes de que IsSearching pase a True -
                // un "while(IsSearching) DoEvents()" inmediatamente despues de fijar el texto
                // sale al primer tick, sin resultados. Mismo patron real ya validado en
                // BUSCADOR-MUNDO (X-7): WaitForDispatcher(280) para pasar el debounce, luego el
                // resto hasta completar el barrido real (~2000ms totales medidos alli).
                vm.Exploration.WorldSearchText = "lava";
                WaitForDispatcher(280);
                WaitForDispatcher(1720);
                DoEvents(); DoEvents();
                Console.WriteLine($"VIEWPORT-exploracion_resultados: busqueda 'lava' -> WorldSearchResults.Count={vm.Exploration.WorldSearchResults.Count} (esperado >=1)");

                // 16-sep-2026 (mismo hallazgo real de Fable): la categoria por defecto ("Todo") NO
                // reproduce el bug - el contenido real que desborda es el de "Cofres/Por tipo"
                // (confirmado con AR-EX-HSCROLL, Program.cs: 55,4px de sobra a sidebar=260px en
                // esa categoria exacta, 0px en "Todo"). Sin fijar la categoria aqui, este volcado
                // mediria un ancho de contenido que nunca reproduce lo que el usuario vio.
                vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                vm.Exploration.ChestViewMode = 0;
                DoEvents(); DoEvents();

                var listBoxEx = Descendientes<ListBox>(window).FirstOrDefault(lb => ReferenceEquals(lb.ItemsSource, vm.Exploration.WorldSearchResults));
                if (listBoxEx == null)
                {
                    Console.WriteLine("VIEWPORT-exploracion_resultados: ListBox real no encontrado - omitido");
                }
                else
                {
                    var svEx = Descendientes<ScrollViewer>(listBoxEx).FirstOrDefault();
                    VolcarViewport("exploracion_resultados", "scrollviewer_listbox", svEx, listBoxEx, "contenido");
                    Captura("viewport-exploracion-resultados");

                    // 16-sep-2026 (mismo hallazgo real de Fable): el ancho de fabrica del sidebar
                    // (320px, Settings.ExplorationSidebarWidth) SI cabe casi exacto (margen medido
                    // de solo 4,6px - ver AR-EX-HSCROLL, Program.cs) - un volcado a ese ancho no
                    // reproduce el bug real que vio el usuario. Segunda entrada, al ancho REAL
                    // minimo del GridSplitter (260px, clamp real en SettingsViewModel), que SI lo
                    // reproduce de verdad (confirmado con AR-EX-HSCROLL: 55,4px de scroll horizontal
                    // real en Cofres/Por tipo a este mismo ancho). A PROPOSITO sin ScrollToEnd aqui
                    // (a diferencia de VolcarViewport): un ListBox virtualizado solo mide el ANCHO
                    // de las filas REALIZADAS, y cuales estan realizadas depende de la posicion de
                    // scroll - AR-EX-HSCROLL midio el desbordamiento en la posicion de REPOSO (recien
                    // cambiada de categoria, sin tocar el scroll), no al final; forzar ScrollToEnd
                    // aqui median otras filas y dejaba de reproducir el mismo numero. Guarda y
                    // restaura el ajuste real del usuario - este arnes nunca deja el .json de
                    // settings tocado al salir.
                    double anchoSidebarAntesVp = vm.Settings.ExplorationSidebarWidth;
                    try
                    {
                        vm.Settings.ExplorationSidebarWidth = 260;
                        DoEvents(); DoEvents();
                        var svEx260 = Descendientes<ScrollViewer>(listBoxEx).FirstOrDefault();
                        if (svEx260 == null)
                        {
                            Console.WriteLine("VIEWPORT-exploracion_resultados_sidebar260: ScrollViewer real no encontrado - omitido");
                        }
                        else
                        {
                            // La captura "exploracion_resultados" de arriba ya dejo este MISMO
                            // ScrollViewer al final (ScrollToEnd, dentro de VolcarViewport) - sin
                            // esto, la posicion de reposo real que se quiere medir aqui heredaria
                            // ese desplazamiento y mediria las filas equivocadas.
                            svEx260.ScrollToHome();
                            DoEvents(); DoEvents();
                            Rect rSvEx260;
                            try { rSvEx260 = RectCompleto(svEx260, window); }
                            catch (InvalidOperationException ex) { rSvEx260 = Rect.Empty; Console.WriteLine($"VIEWPORT-exploracion_resultados_sidebar260: RectCompleto fallo - {ex.Message}"); }
                            elementos.Add(new
                            {
                                id = "exploracion_resultados_sidebar260",
                                tipo = "scrollviewer_listbox",
                                padre_id = (string?)null,
                                x = rSvEx260.X,
                                y = rSvEx260.Y,
                                ancho = rSvEx260.Width,
                                alto = rSvEx260.Height,
                                grupo = (string?)null,
                                orden_z = OrdenZ(svEx260),
                                capa = "panel",
                                viewportAlto = svEx260.ViewportHeight,
                                viewportAncho = svEx260.ViewportWidth,
                                extentAncho = svEx260.ExtentWidth,
                            });
                            Console.WriteLine($"VIEWPORT-exploracion_resultados_sidebar260: ScrollViewer real (SIN ScrollToEnd, posicion de reposo real - mismo criterio que AR-EX-HSCROLL), ViewportWidth={svEx260.ViewportWidth:0.#}px ExtentWidth={svEx260.ExtentWidth:0.#}px ScrollableWidth={svEx260.ScrollableWidth:0.#}px HorizontalScrollBarVisibility={svEx260.HorizontalScrollBarVisibility} computado={svEx260.ComputedHorizontalScrollBarVisibility}");
                            Captura("viewport-exploracion-resultados-sidebar260");
                        }
                    }
                    finally
                    {
                        vm.Settings.ExplorationSidebarWidth = anchoSidebarAntesVp;
                        DoEvents(); DoEvents();
                    }
                }
                vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                vm.Exploration.WorldSearchText = string.Empty;
                DoEvents();
            }
        }
        catch (Exception ex) { Console.WriteLine("VIEWPORT-exploracion_resultados-EXCEPTION: " + ex); }

        string volcadoPath = Path.Combine(outDir, "volcado-viewport-scroll.json");
        File.WriteAllText(volcadoPath, JsonSerializer.Serialize(elementos, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"VIEWPORT: {elementos.Count} elementos volcados a {volcadoPath}");
        Console.WriteLine($"VIEWPORT: evidencia (capturas + volcado) en {outDir}");
    }
}
