// CANARIO REAL - correcciones del revisor visual de la FASE D del responsive global (28-sep-2026,
// aplicador-fix-responsive-faseD). Parte de LIBRARY_RESPONSIVE_SOLO (ver CanarioResponsiveLibrerias.cs).
// Solo FindName/reflexion/tipos que existen tambien en fae1461a: el mismo fichero compila y MIDE contra el
// codigo anterior a la correccion (prueba en rojo) y contra el nuevo (verde).
//
// Falla ("FALLO: LIBRARY_RESPONSIVE_SOLO-<codigo>") si:
//   - RESULTADOS-2FILAS  (D-01) a 1080x700 con la Libreria desplegada, en una ruta de 2.o/3.er nivel, en la mas
//                            profunda o en la carpeta con mas hijos (ES y EN), el viewport de resultados no enseña
//                            al menos 2 filas ENTERAS de celdas.
//   - RUTA-UNA-LINEA     (D-01) la linea de ruta ocupa mas de una linea.
//   - TARJETAS-RAIZ      (D-02) en "Ver todo" no se ve entera la primera fila de tarjetas de carpeta raiz.
//   - POPUP-SUBCAT       (D-03) el desplegable de subcategorias mas alto que la ventana o fuera del area de trabajo.
//   - POPUP-PREFIJO-REPETIDO (D-03) una subcategoria del desplegable repite el nombre de su madre delante.
//   - ORDEN              (D-04) las 3 superficies no siguen cabecera con buscador -> pastillas -> ruta/resumen.
//   - EDITAR-COMPOSICION (D-05) Editar objeto y Editar buff con composiciones distintas en el mismo tamaño.
//   - PASTILLAS-ALTO     (D-06) pastillas de categoria de distinto alto o por debajo de 23,5px.
//   - EDITAR-VACIO-ALTO  (D-07) Editar sin seleccion a 2560x1440 mas alto que su contenido (> 400px).
//   - PREFIJO-DESPLEGABLE  el selector de prefijo en desplegable (Compacto) con scroll o fuera del area de trabajo.
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Terrakeep.App;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static void MedirCorreccionesRevisorFaseD(MainWindow window, MainViewModel vm, FrameworkElement objetosView,
        Action<string, string> Fallo, string outDir, string etiqueta, Action<int, int> IrA, Func<FrameworkElement, bool> EnteroEnAmbosEjes)
    {
        void Shot(string nombre) => File.WriteAllBytes(Path.Combine(outDir, $"lib-{etiqueta}-{nombre}.png"), CapturarPng(window, window.ActualWidth, window.ActualHeight));
        double Escala() => PresentationSource.FromVisual(window)?.CompositionTarget?.TransformToDevice.M11 ?? 1;
        Rect RectPantalla(FrameworkElement fe)
        {
            var p = fe.PointToScreen(new Point(0, 0)); double e = Escala();
            return new Rect(p.X / e, p.Y / e, fe.ActualWidth, fe.ActualHeight);
        }
        string? ModoComposicion() => typeof(MainViewModel).GetProperty("IsEditarBarraCompleta")?.GetValue(vm) is bool b ? (b ? "barra-completa" : "fila-contenido") : null;
        var estilosPastilla = new[] { "PastillaCategoria", "PastillaCategoriaBase", "PastillaVerTodo" }.Select(k => window.TryFindResource(k) as Style).Where(s => s != null).ToList();
        bool EsPastilla(ButtonBase b) => b.Style != null && estilosPastilla.Contains(b.Style);

        var nav = objetosView.FindName("LibreriaNavegadorCategorias") as FrameworkElement;
        var resultados = objetosView.FindName("LibreriaResultados") as ScrollViewer;
        if (nav == null || resultados == null) { Fallo("PREPARACION", "revisor FASE D: no se encuentran LibreriaNavegadorCategorias/LibreriaResultados"); return; }
        FrameworkElement? EnNav(string n) => nav.FindName(n) as FrameworkElement;

        // --- Arbol real: nodo de 2.o nivel, de 3.er nivel, el mas profundo y el de mas hijos ---
        CategoryNodeViewModel? masHijos = null, profundo = null, nivel2 = null, nivel3 = null; int maxProf = 0;
        void Rec(CategoryNodeViewModel n, int prof)
        {
            if (masHijos == null || n.Children.Count > masHijos.Children.Count) masHijos = n;
            if (prof > maxProf) { maxProf = prof; profundo = n; }
            if (prof == 2 && nivel2 == null && n.Children.Count > 0 && n.FullPath.StartsWith("Categories")) nivel2 = n;
            if (prof == 3 && nivel3 == null && n.FullPath.StartsWith("Categories")) nivel3 = n;
            foreach (var c in n.Children) Rec(c, prof + 1);
        }
        foreach (var r in vm.Library.RootCategories) Rec(r, 1);
        Console.WriteLine($"LIB REVISOR ARBOL: nivel2='{nivel2?.FullPath}' nivel3='{nivel3?.FullPath}' mas profundo (nivel {maxProf})='{profundo?.FullPath}' mas hijos='{masHijos?.FullPath}' ({masHijos?.Children.Count})");

        // --- D-01: 2 filas enteras de resultados + ruta en una linea (1080x700, ES/EN) ---
        foreach (var idioma in new[] { "es", "en" })
        {
            vm.Settings.Language = idioma; DoEvents(); DoEvents();
            window.WindowState = WindowState.Normal; FijarTamaño(window, 1080, 700); vm.IsLibraryCollapsed = false;
            IrA(0, 1); vm.Library.SearchText = ""; WaitForDispatcher(250);
            foreach (var (etq, nodo) in new[] { ("nivel2", nivel2), ("nivel3", nivel3), ("profundo", profundo), ("mashijos", masHijos) })
            {
                if (nodo == null) continue;
                vm.Library.ClearCategoryCommand.Execute(null); DoEvents();
                vm.Library.SelectCategoryCommand.Execute(nodo); DoEvents(); DoEvents(); WaitForDispatcher(200);
                var ruta = EnNav("RutaSubcategorias");
                double altoRuta = ruta?.ActualHeight ?? -1;
                var ic = Descendientes<ItemsControl>(resultados).FirstOrDefault(i => i.IsVisible && ReferenceEquals(i.ItemsSource, vm.Library.Results));
                var celdas = ic == null ? [] : Enumerable.Range(0, ic.Items.Count).Select(i => ic.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement)
                    .Where(c => c != null && c.IsVisible && c.ActualWidth > 0).Cast<FrameworkElement>().ToList();
                var enteras = celdas.Where(EnteroEnAmbosEjes).ToList();
                int filasEnteras = enteras.Select(c => Math.Round(RectCompleto(c, window).Top)).Distinct().Count();
                int plegadas = ruta?.GetType().GetProperty("MigasPlegadas")?.GetValue(ruta) is int p ? p : -1;
                string cab = $"[ruta-{etq}-1080x700-{idioma}] '{nodo.FullPath}' migas={vm.Library.MigasCategoria.Count}";
                Console.WriteLine($"LIB REVISOR D-01 {cab}: linea de ruta alto={altoRuta:0.#}px (migas plegadas en '...'={plegadas}) | resultados vp={resultados.ViewportHeight:0.#}px, celdas={celdas.Count}, enteras={enteras.Count} en {filasEnteras} fila(s) | navegador alto={nav.ActualHeight:0.#}px | composicion Editar={ModoComposicion() ?? "(sin la propiedad: codigo anterior)"}");
                if (altoRuta > 32) Fallo("RUTA-UNA-LINEA", $"{cab}: la linea de ruta mide {altoRuta:0.#}px (mas de una linea de 24px)");
                if (celdas.Count > 0 && filasEnteras < Math.Min(2, (int)Math.Ceiling(celdas.Count / 5.0)))
                    Fallo("RESULTADOS-2FILAS", $"{cab}: el viewport de resultados ({resultados.ViewportHeight:0.#}px) enseña {filasEnteras} fila(s) entera(s) - se exigen 2");
                Shot($"revisor-ruta-{etq}-1080x700-{idioma.ToUpperInvariant()}");

                if (ReferenceEquals(nodo, masHijos))
                {
                    // --- D-03: desplegable de subcategorias ---
                    var boton = EnNav("BotonSubcategorias") as ButtonBase;
                    var pop = EnNav("PopupSubcategorias") as Popup;
                    if (boton == null || pop == null) { Fallo("POPUP-SUBCAT", $"{cab}: sin boton/desplegable de subcategorias"); continue; }
                    pop.IsOpen = false; DoEvents();
                    boton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); DoEvents(); DoEvents(); WaitForDispatcher(300);
                    if (!pop.IsOpen || pop.Child is not FrameworkElement b) { Fallo("POPUP-SUBCAT", $"{cab}: el desplegable no se abrio"); continue; }
                    b.UpdateLayout();
                    var rp = RectPantalla(b); var rw = RectPantalla(window); var wa = SystemParameters.WorkArea;
                    bool dentroTrabajo = rp.Top >= wa.Top - 0.5 && rp.Bottom <= wa.Bottom + 0.5 && rp.Left >= wa.Left - 0.5 && rp.Right <= wa.Right + 0.5;
                    bool cabeVentana = rp.Height <= window.ActualHeight + 0.5;
                    var svPop = Descendientes<ScrollViewer>(b).FirstOrDefault(s => s.IsVisible);
                    var pills = Descendientes<ButtonBase>(b).Where(x => x.IsVisible && x.ActualHeight > 0).ToList();
                    string baseMadre = CategoryBase(masHijos!.Name);
                    var repetidas = pills.Select(x => Descendientes<TextBlock>(x).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Text))?.Text ?? "")
                        .Where(t => t.StartsWith(baseMadre + " - ", StringComparison.Ordinal)).ToList();
                    Console.WriteLine($"LIB REVISOR D-03 {cab}: desplegable {rp.Width:0.#}x{rp.Height:0.#} DIP en pantalla ({rp.Left:0},{rp.Top:0}) | ventana {rw.Width:0}x{rw.Height:0} en ({rw.Left:0},{rw.Top:0}) | area de trabajo {wa} | dentro del area={dentroTrabajo} cabe en el alto de la ventana={cabeVentana} | scroll local={(svPop == null ? "no" : $"vp={svPop.ViewportHeight:0.#} ext={svPop.ExtentHeight:0.#}")} | pastillas={pills.Count} alto min/max={(pills.Count == 0 ? 0 : pills.Min(x => x.ActualHeight)):0.#}/{(pills.Count == 0 ? 0 : pills.Max(x => x.ActualHeight)):0.#} | etiquetas con el prefijo '{baseMadre} - ' repetido={repetidas.Count}{(repetidas.Count > 0 ? " (p.ej. '" + repetidas[0] + "')" : "")}");
                    if (!dentroTrabajo || !cabeVentana) Fallo("POPUP-SUBCAT", $"{cab}: el desplegable mide {rp.Width:0.#}x{rp.Height:0.#} DIP (ventana {window.ActualHeight:0}px de alto, dentro del area de trabajo={dentroTrabajo})");
                    if (repetidas.Count > 0) Fallo("POPUP-PREFIJO-REPETIDO", $"{cab}: {repetidas.Count} subcategoria(s) repiten '{baseMadre} - ' delante (p.ej. '{repetidas[0]}')");
                    File.WriteAllBytes(Path.Combine(outDir, $"lib-{etiqueta}-revisor-popup-mashijos-1080x700-{idioma.ToUpperInvariant()}.png"), CapturarPng(b, b.ActualWidth, b.ActualHeight));
                    pop.IsOpen = false; DoEvents();
                }
            }
            vm.Library.ClearCategoryCommand.Execute(null); DoEvents();
        }
        vm.Settings.Language = "es"; DoEvents(); DoEvents();

        // --- D-02: primera fila de tarjetas raiz ENTERA en "Ver todo" (las 3 superficies) ---
        foreach (var (tid, w, h, idioma) in new[] { ("1080x700", 1080.0, 700.0, "es"), ("1080x700", 1080.0, 700.0, "en"), ("1366x768", 1366.0, 768.0, "es"), ("1520x860", 1520.0, 860.0, "es") })
        {
            vm.Settings.Language = idioma; DoEvents();
            window.WindowState = WindowState.Normal; FijarTamaño(window, w, h); vm.IsLibraryCollapsed = false; vm.IsBuffLibraryCollapsed = false;
            foreach (var (sup, pest, cat) in new (string, int, dynamic)[] { ("Libreria", 0, vm.Library), ("LibreriaBuffs", 1, vm.BuffLibrary), ("Investigacion", 2, vm.Research) })
            {
                IrA(pest, 1);
                cat.ClearCategoryCommand.Execute(null); cat.SearchText = ""; WaitForDispatcher(250); DoEvents(); DoEvents();
                var tarjetas = Descendientes<FrameworkElement>(window).FirstOrDefault(f => f.GetType().Name == "TarjetasCategoriasRaiz" && f.IsVisible);
                var icT = tarjetas == null ? null : Descendientes<ItemsControl>(tarjetas).FirstOrDefault();
                if (icT == null) { Console.WriteLine($"LIB REVISOR D-02 [{sup}-vertodo-{tid}-{idioma}]: sin tarjetas raiz visibles"); continue; }
                var cards = Enumerable.Range(0, icT.Items.Count).Select(i => icT.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter)
                    .Where(c => c != null && c.ActualHeight > 0 && VisualTreeHelper.GetChildrenCount(c) > 0)
                    .Select(c => (FrameworkElement)VisualTreeHelper.GetChild(c!, 0)).ToList();
                if (cards.Count == 0) continue;
                double top0 = cards.Min(c => RectCompleto(c, window).Top);
                var fila = cards.Where(c => RectCompleto(c, window).Top - top0 < 1).ToList();
                int enteras = fila.Count(EnteroEnAmbosEjes);
                var owner = FindAncestro<ScrollViewer>(icT);
                bool subpastillas = cards.Any(c => Descendientes<ItemsControl>(c).Any(x => x.IsVisible && x.Items.Count > 0));
                string cab = $"[{sup}-vertodo-{tid}-{idioma}]";
                Console.WriteLine($"LIB REVISOR D-02 {cab}: primera fila {fila.Count} tarjeta(s), enteras sin desplazar={enteras}, alto tarjeta max={fila.Max(c => c.ActualHeight):0.#}px, viewport={owner?.ViewportHeight:0.#}px, subpastillas visibles={subpastillas}");
                if (enteras < fila.Count) Fallo("TARJETAS-RAIZ", $"{cab}: solo {enteras}/{fila.Count} tarjetas de la primera fila se ven enteras (alto {fila.Max(c => c.ActualHeight):0.#}px en un viewport de {owner?.ViewportHeight:0.#}px)");
                if (sup == "Libreria") Shot($"revisor-vertodo-{tid}-{idioma.ToUpperInvariant()}");

                // --- D-06: alto de las pastillas (principales, "Ver todo" y subpastillas) ---
                var region = FindAncestro<Border>(icT);
                var pastillas = Descendientes<ButtonBase>(window).Where(b => b.IsVisible && b.ActualHeight > 0 && EsPastilla(b) && region != null && FindRegion(b, pest, vm) ).ToList();
                if (pastillas.Count > 0)
                {
                    double min = pastillas.Min(b => b.ActualHeight), max = pastillas.Max(b => b.ActualHeight);
                    // Un nombre que envuelve a 2 lineas puede crecer (s34) - se descartan las que llevan texto envuelto.
                    var deUnaLinea = pastillas.Where(b => Descendientes<TextBlock>(b).All(t => t.ActualHeight < 20)).ToList();
                    double min1 = deUnaLinea.Count == 0 ? 0 : deUnaLinea.Min(b => b.ActualHeight), max1 = deUnaLinea.Count == 0 ? 0 : deUnaLinea.Max(b => b.ActualHeight);
                    Console.WriteLine($"LIB REVISOR D-06 {cab}: {pastillas.Count} pastillas, alto min/max={min:0.#}/{max:0.#}px (de una linea: {min1:0.#}/{max1:0.#})");
                    if (deUnaLinea.Count > 0 && (min1 < 23.5 || max1 - min1 > 1)) Fallo("PASTILLAS-ALTO", $"{cab}: pastillas de una linea de {min1:0.#} a {max1:0.#}px (se piden ~24px iguales)");
                }
            }
        }
        vm.Settings.Language = "es"; DoEvents();

        // --- D-04: mismo orden en las 3 superficies (1366x768) ---
        FijarTamaño(window, 1366, 768); vm.IsLibraryCollapsed = false; vm.IsBuffLibraryCollapsed = false;
        foreach (var (sup, pest, cat) in new (string, int, dynamic)[] { ("Libreria", 0, vm.Library), ("LibreriaBuffs", 1, vm.BuffLibrary), ("Investigacion", 2, vm.Research) })
        {
            IrA(pest, 1);
            var raiz0 = ((System.Collections.IEnumerable)cat.RootCategories).Cast<CategoryNodeViewModel>().First(r => r.Children.Count > 0);
            cat.ClearCategoryCommand.Execute(null); cat.SelectCategoryCommand.Execute(raiz0); DoEvents(); DoEvents(); WaitForDispatcher(150);
            string ruta = sup == "Libreria" ? "Library" : sup == "LibreriaBuffs" ? "BuffLibrary" : "Research";
            var buscador = Descendientes<TextBox>(window).FirstOrDefault(t => t.IsVisible && System.Windows.Data.BindingOperations.GetBindingExpression(t, TextBox.TextProperty)?.ParentBinding.Path.Path == ruta + ".SearchText");
            var raices = Descendientes<ButtonBase>(window).Where(b => b.IsVisible && b.ActualHeight > 0 && b.DataContext is CategoryNodeViewModel n && ((System.Collections.IEnumerable)cat.RootCategories).Cast<object>().Contains(n) && EsPastilla(b)).ToList();
            var resumen = Descendientes<TextBlock>(window).FirstOrDefault(t => t.IsVisible && ReferenceEquals(t.DataContext, (object)cat) && System.Windows.Data.BindingOperations.GetBindingExpression(t, TextBlock.TextProperty)?.ParentBinding.Path.Path == "ResultsSummary");
            if (buscador == null || raices.Count == 0 || resumen == null) { Fallo("ORDEN", $"[{sup}] no se encuentran buscador ({buscador != null}), pastillas raiz ({raices.Count}) o resumen ({resumen != null})"); continue; }
            // La cabecera se pinta de verdad con los datos de la app: el buscador con el MainViewModel como
            // DataContext y el boton de plegar (Libreria/Libreria de buffs) con su titulo. Hallazgo de la primera
            // pasada de esta correccion: tras mover la cabecera al flujo de las categorias, un DataContext mal
            // resuelto dejaba los botones SIN TEXTO y el resto de este canario seguia en verde.
            bool dcOk = buscador.DataContext is MainViewModel;
            var plegar = Descendientes<ButtonBase>(window).FirstOrDefault(x => x.IsVisible && System.Windows.Data.BindingOperations.GetBindingExpression(x, ButtonBase.CommandProperty)?.ParentBinding.Path.Path is "ToggleLibraryCollapsedCommand" or "ToggleBuffLibraryCollapsedCommand"
                && (sup == "Libreria") == (System.Windows.Data.BindingOperations.GetBindingExpression(x, ButtonBase.CommandProperty)?.ParentBinding.Path.Path == "ToggleLibraryCollapsedCommand"));
            var textosPlegar = plegar == null ? [] : Descendientes<TextBlock>(plegar).Where(t => t.IsVisible).Select(t => t.Text).ToList();
            Console.WriteLine($"LIB REVISOR CABECERA [{sup}] buscador con MainViewModel={dcOk} | boton de plegar={(plegar == null ? "(no aplica)" : "'" + string.Join("' '", textosPlegar) + "'")}");
            if (!dcOk) Fallo("CABECERA-TEXTO", $"[{sup}] el buscador de la cabecera no tiene el MainViewModel como DataContext ({buscador.DataContext?.GetType().Name ?? "null"})");
            if (sup != "Investigacion" && (plegar == null || textosPlegar.Count < 2 || textosPlegar.Any(string.IsNullOrWhiteSpace)))
                Fallo("CABECERA-TEXTO", $"[{sup}] el boton de plegar la libreria no enseña su titulo ('{string.Join("' '", textosPlegar)}')");
            var rb = RectCompleto(buscador, window); var rr = RectCompleto(resumen, window);
            double raicesTop = raices.Min(b => RectCompleto(b, window).Top), raicesBottom = raices.Max(b => RectCompleto(b, window).Bottom);
            bool ok = rb.Top <= raicesTop + 1 && raicesBottom <= rr.Top + 1 && rb.Bottom <= rr.Top + 1;
            Console.WriteLine($"LIB REVISOR D-04 [{sup}] buscador y={rb.Top:0.#}-{rb.Bottom:0.#} | pastillas raiz y={raicesTop:0.#}-{raicesBottom:0.#} | ruta/resumen y={rr.Top:0.#} -> orden cabecera->pastillas->ruta={ok}");
            if (!ok) Fallo("ORDEN", $"[{sup}] el orden no es cabecera con buscador -> pastillas -> ruta/resumen (buscador y={rb.Top:0.#}, pastillas y={raicesTop:0.#}-{raicesBottom:0.#}, resumen y={rr.Top:0.#})");
            cat.ClearCategoryCommand.Execute(null); DoEvents();
        }

        // --- D-05: Editar objeto y Editar buff con la MISMA composicion ---
        foreach (var (tid, w, h) in new[] { ("1080x700", 1080.0, 700.0), ("1366x768", 1366.0, 768.0), ("1920x1080", 1920.0, 1080.0) })
        {
            FijarTamaño(window, w, h); vm.IsLibraryCollapsed = false; vm.IsBuffLibraryCollapsed = false;
            IrA(0, 1);
            var editarObj = objetosView.FindName("EditarTarjeta") as FrameworkElement;
            var libObj = objetosView.FindName("LibreriaObjetosPanel") as FrameworkElement;
            (int span, double anchoLib, double anchoGrid) Comp(FrameworkElement? ed, FrameworkElement? lib)
                => ed == null || lib == null ? (-1, 0, 0) : (Grid.GetRowSpan(ed), lib.ActualWidth, (VisualTreeHelper.GetParent(lib) as FrameworkElement)?.ActualWidth ?? 0);
            var co = Comp(editarObj, libObj);
            IrA(1, -1);
            var editarBuff = Descendientes<Border>(window).FirstOrDefault(b => b.IsVisible && Descendientes<ContentControl>(b).Any(c => ReferenceEquals(c.Content, vm.BuffEdit)) && b.Child is ContentControl);
            var libBuff = window.FindName("LibreriaBuffsPanel") as FrameworkElement;
            var cb = Comp(editarBuff, libBuff);
            // "Misma composicion": el mismo RowSpan dentro de su rejilla y la Libreria con el mismo ancho relativo.
            bool mismaComp = co.span == cb.span && Math.Abs(co.anchoLib / Math.Max(1, co.anchoGrid) - cb.anchoLib / Math.Max(1, cb.anchoGrid)) < 0.02;
            Console.WriteLine($"LIB REVISOR D-05 [{tid}] Editar objeto RowSpan={co.span} Libreria {co.anchoLib:0}/{co.anchoGrid:0}px | Editar buff RowSpan={cb.span} Libreria de buffs {cb.anchoLib:0}/{cb.anchoGrid:0}px | composicion={ModoComposicion() ?? "?"} -> misma={mismaComp}");
            if (!mismaComp) Fallo("EDITAR-COMPOSICION", $"[{tid}] Editar objeto (RowSpan {co.span}, Libreria {co.anchoLib:0}/{co.anchoGrid:0}) y Editar buff (RowSpan {cb.span}, Libreria {cb.anchoLib:0}/{cb.anchoGrid:0}) no siguen la misma composicion");
        }

        // --- D-07: Editar sin seleccion a 2560x1440 ajustado a su contenido ---
        {
            FijarTamaño(window, 2560, 1440); vm.IsLibraryCollapsed = false; IrA(0, 1);
            if (vm.ItemEdit.Slot != null) { vm.ItemEdit.Slot.IsSelected = false; vm.ItemEdit.Slot = null; }
            DoEvents(); DoEvents(); WaitForDispatcher(80);
            var editar = objetosView.FindName("EditarTarjeta") as FrameworkElement;
            double alto = editar?.ActualHeight ?? -1;
            Console.WriteLine($"LIB REVISOR D-07 [2560x1440] Editar sin seleccion {editar?.ActualWidth:0}x{alto:0.#}px (slot={(vm.ItemEdit.Slot == null ? "ninguno" : "?")})");
            if (alto > 400) Fallo("EDITAR-VACIO-ALTO", $"[2560x1440] Editar sin seleccion mide {alto:0.#}px de alto con solo la leyenda");
            Shot("revisor-editar-vacio-2560x1440");
        }

        // --- Editar como barra lateral (Normal+) con el PEOR objeto de prefijos (granadas Picaro, 17 prefijos) en
        //     la combinacion mas larga: los 17 botones enteros y la tarjeta dentro de su rejilla, ES/EN. Directo, sin
        //     depender de la heuristica de "peor holgura" de la seccion 3 (con Editar alto segun su contenido la
        //     holgura interna ya no discrimina). ---
        foreach (var (tid, w, h) in new[] { ("1320x700", 1320.0, 700.0), ("1366x768", 1366.0, 768.0), ("1920x1080", 1920.0, 1080.0) })
            foreach (var idioma in new[] { "es", "en" })
            {
                vm.Settings.Language = idioma; DoEvents(); DoEvents();
                FijarTamaño(window, w, h); vm.IsLibraryCollapsed = false; IrA(0, 1);
                var slot = vm.InventoryContainer!.Slots[9]; int idOrig = slot.ItemId;
                slot.PlaceItem(20001905); vm.SelectSlot(slot); DoEvents(); DoEvents();
                var ie = vm.ItemEdit;
                string? mejor = null; int maxPref = -1;
                foreach (var m in ie.Metas.ToList())
                {
                    ie.SelectMetaCommand.Execute(m);
                    foreach (var g in ie.Groups.ToList())
                    {
                        ie.SelectGroupCommand.Execute(g);
                        if (ie.Prefixes.Count > maxPref) { maxPref = ie.Prefixes.Count; mejor = $"{m.Label}/{g.Label}"; }
                    }
                }
                // Volver a la combinacion con mas prefijos.
                foreach (var m in ie.Metas.ToList())
                {
                    ie.SelectMetaCommand.Execute(m);
                    var g = ie.Groups.FirstOrDefault(x => $"{m.Label}/{x.Label}" == mejor);
                    if (g != null) { ie.SelectGroupCommand.Execute(g); break; }
                }
                DoEvents(); DoEvents(); WaitForDispatcher(80); window.UpdateLayout();
                var editar = objetosView.FindName("EditarTarjeta") as FrameworkElement;
                if (editar == null) { Fallo("EDITAR-CLIP", $"[editar-barra-peor-{tid}-{idioma}] sin panel Editar"); continue; }
                var botones = Descendientes<ButtonBase>(editar).Where(b => b.IsVisible && b.DataContext is PrefixCatalogEntryViewModel).ToList();
                int enteros = botones.Count(EnteroEnAmbosEjes);
                var re = RectCompleto(editar, window);
                var rejilla = VisualTreeHelper.GetParent(editar) as FrameworkElement;
                double margen = rejilla == null ? double.NaN : RectCompleto(rejilla, window).Bottom - re.Bottom;
                string cab = $"[editar-barra-peor-{tid}-{idioma}] '{slot.DisplayName}' {mejor} prefijos={ie.Prefixes.Count}";
                Console.WriteLine($"LIB REVISOR EDITAR-BARRA {cab}: composicion={ModoComposicion() ?? "?"} | Editar {re.Width:0.#}x{re.Height:0.#} | botones de prefijo enteros={enteros}/{ie.Prefixes.Count} (visibles {botones.Count}) | margen hasta el fondo de su rejilla={margen:0.#}px | ScrollViewer dentro={Descendientes<ScrollViewer>(editar).Count(s => s.TemplatedParent is not System.Windows.Controls.Primitives.TextBoxBase)}");
                if (ModoComposicion() == "barra-completa" && (enteros < ie.Prefixes.Count || margen < -0.5))
                    Fallo("EDITAR-CLIP", $"{cab}: {enteros}/{ie.Prefixes.Count} prefijos enteros, margen hasta el fondo {margen:0.#}px");
                if (tid == "1320x700") Shot($"revisor-editar-barra-peor-1320x700-{idioma.ToUpperInvariant()}");
                slot.PlaceItem(idOrig); vm.IsDirty = false;
            }
        vm.Settings.Language = "es"; DoEvents();

        // --- Selector de prefijo en desplegable (Compacto, 1080x700): sin scroll y dentro del area de trabajo ---
        {
            FijarTamaño(window, 1080, 700); vm.IsLibraryCollapsed = false; IrA(0, 1);
            var slot = vm.InventoryContainer!.Slots[9]; int idOrig = slot.ItemId;
            foreach (var idioma in new[] { "es", "en" })
            {
                vm.Settings.Language = idioma; DoEvents(); DoEvents();
                slot.PlaceItem(20001905); vm.SelectSlot(slot); DoEvents(); DoEvents(); WaitForDispatcher(80);
                var ie = vm.ItemEdit;
                var meta = ie.Metas.FirstOrDefault(m => m.Label.StartsWith("Posit") || m.Label.StartsWith("Posi")) ?? ie.Metas.FirstOrDefault();
                if (meta != null) ie.SelectMetaCommand.Execute(meta);
                var grupo = ie.Groups.OrderByDescending(g => g.Label.Length).FirstOrDefault(g => g.Label.Contains("caro") || g.Label.Contains("Rogue")) ?? ie.Groups.FirstOrDefault();
                if (grupo != null) ie.SelectGroupCommand.Execute(grupo);
                DoEvents(); DoEvents();
                var editar = objetosView.FindName("EditarTarjeta") as FrameworkElement;
                var popup = editar == null ? null : Descendientes<Popup>(editar).FirstOrDefault(p => p.Name == "PopupPrefijo");
                var boton = editar == null ? null : Descendientes<ButtonBase>(editar).FirstOrDefault(b => b.Name == "BotonPrefijoDesplegable");
                string cab = $"[prefijo-desplegable-1080x700-{idioma}] prefijos={ie.Prefixes.Count}";
                if (popup == null || boton == null || !boton.IsVisible)
                {
                    Console.WriteLine($"LIB REVISOR PREFIJO {cab}: sin desplegable de prefijo visible (composicion={ModoComposicion() ?? "codigo anterior"}) - selector en linea");
                    continue;
                }
                boton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); DoEvents(); DoEvents(); WaitForDispatcher(250);
                if (!popup.IsOpen || popup.Child is not FrameworkElement b) { Fallo("PREFIJO-DESPLEGABLE", $"{cab}: el desplegable no se abrio"); continue; }
                b.UpdateLayout();
                var rp = RectPantalla(b); var wa = SystemParameters.WorkArea;
                bool dentro = rp.Top >= wa.Top - 0.5 && rp.Bottom <= wa.Bottom + 0.5;
                int svs = Descendientes<ScrollViewer>(b).Count(s => s.TemplatedParent is not System.Windows.Controls.Primitives.TextBoxBase);
                var prefBtns = Descendientes<ButtonBase>(b).Where(x => x.IsVisible && x.DataContext is PrefixCatalogEntryViewModel).ToList();
                Console.WriteLine($"LIB REVISOR PREFIJO {cab}: desplegable {rp.Width:0.#}x{rp.Height:0.#} DIP en ({rp.Left:0},{rp.Top:0}) | area de trabajo {wa} dentro={dentro} | ScrollViewer dentro={svs} | botones de prefijo visibles={prefBtns.Count}/{ie.Prefixes.Count}");
                if (!dentro || svs > 0 || prefBtns.Count < ie.Prefixes.Count) Fallo("PREFIJO-DESPLEGABLE", $"{cab}: desplegable {rp.Width:0.#}x{rp.Height:0.#}, dentro del area={dentro}, ScrollViewer={svs}, prefijos visibles {prefBtns.Count}/{ie.Prefixes.Count}");
                File.WriteAllBytes(Path.Combine(outDir, $"lib-{etiqueta}-revisor-prefijo-desplegable-1080x700-{idioma.ToUpperInvariant()}.png"), CapturarPng(b, b.ActualWidth, b.ActualHeight));
                Shot($"revisor-editar-compacto-1080x700-{idioma.ToUpperInvariant()}");
                popup.IsOpen = false; DoEvents();
            }
            vm.Settings.Language = "es"; DoEvents();
            slot.PlaceItem(idOrig); vm.IsDirty = false;
        }
    }

    private static string CategoryBase(string nombre)
    {
        int i = nombre.LastIndexOf(" (", StringComparison.Ordinal);
        return i > 0 && nombre.EndsWith(')') ? nombre[..i] : nombre;
    }

    private static T? FindAncestro<T>(DependencyObject d) where T : DependencyObject
    {
        for (var p = VisualTreeHelper.GetParent(d); p != null; p = VisualTreeHelper.GetParent(p))
            if (p is T t) return t;
        return null;
    }

    // Pastilla perteneciente a la superficie de la pestaña interna 'pest' (0 Objetos, 1 Buffs, 2 Investigacion):
    // su DataContext es un nodo del arbol de ESA superficie, o es "Ver todo" de ese catalogo.
    private static bool FindRegion(ButtonBase b, int pest, MainViewModel vm)
    {
        System.Collections.IEnumerable raices = pest switch { 0 => vm.Library.RootCategories, 1 => vm.BuffLibrary.RootCategories, _ => vm.Research.RootCategories };
        object cat = pest switch { 0 => vm.Library, 1 => vm.BuffLibrary, _ => vm.Research };
        if (ReferenceEquals(b.DataContext, cat)) return true;
        if (b.DataContext is not CategoryNodeViewModel n) return false;
        bool Contiene(System.Collections.IEnumerable nodos) => nodos.Cast<CategoryNodeViewModel>().Any(x => ReferenceEquals(x, n) || Contiene(x.Children));
        return Contiene(raices);
    }
}
