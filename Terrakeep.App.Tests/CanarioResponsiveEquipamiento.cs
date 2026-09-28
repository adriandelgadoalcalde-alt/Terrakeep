// CANARIO REAL - FASE B del responsive global (PDF "Arreglo familia keep", bloque 2 "TERRAKEEP -
// RESPONSIVE GLOBAL, PAGINACION Y SCROLL COMO ULTIMO RECURSO", secciones 4-7, 13-14, 21, 24-26, 34).
// 28-sep-2026, aplicador-fix-responsive-faseB.
//
// EQUIP_RESPONSIVE_SOLO=1 mide la pagina "Personaje > Objetos > Equipamiento" con geometria REAL
// (RectCompleto vs ZonaVisible/RectVisible, mismo par de herramientas que AR-LAY/NAV123/LIBCARD) en
// varios tamaños, ES/EN y con redimensionado en caliente, y falla (lineas "FALLO: ...") si aparece:
//   - SCROLL-ANIDADO   (A) un ScrollViewer vertical dentro del scroll owner vertical de la pagina
//                          (ObjetosPaginaEquipamiento) - el modelo VIEJO tenia 5 niveles.
//   - SELECTOR         (B/F) Armadura/Vanidad/Tintes sin navegacion visible y ENTERA sin desplazar,
//                          o el selector Loadout / la navegacion 1/2/3 fuera del viewport.
//   - CLIP-H / CLIP-V  (E) contenido recortado sin escape, o solo alcanzable por un scroll ANIDADO
//                          (lo unico legitimo es el scroll owner principal de la pagina).
//   - OVERLAP          regiones o celdas que se pisan (Mascotas / cabecera / rejilla / Monedas /
//                          panel Editar).
//   - HSCROLL          (G) HorizontalScrollBarVisibility=Disabled con contenido mas ancho que el
//                          viewport, o una barra horizontal visible inesperada.
//   - TEXTO-LOCAL      (s34) texto estructural (Defensa/bono de set) con un ScrollViewer propio.
//   - RESIZE           (s24) perdida de estado al redimensionar grande->normal->minimo->grande.
//   - VIEJO            (s26/s28 negative acceptance) el mecanismo retirado sigue existiendo por
//                          debajo del nuevo: ScrollViewer con MaxHeight en la cabecera, la rejilla
//                          de 3 columnas "Amplio" (IsEquipmentExpanded) o la propiedad en el VM.
//
// No toca produccion. Capturas en EQUIP_RESPONSIVE_EVIDENCIA (si se define) o en
// <bin>/keepqa-evidencia/responsive-faseB.
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Terrakeep.App;
using Terrakeep.App.Controls;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static void EjecutarEquipResponsiveSolo(MainWindow window, MainViewModel vm)
    {
        int fallos = 0;
        void Fallo(string codigo, string msg) { fallos++; Console.WriteLine($"FALLO: EQUIP_RESPONSIVE_SOLO-{codigo} - {msg}"); }

        try
        {
            var personajeReal = vm.Home.Characters.FirstOrDefault();
            if (personajeReal != null) { vm.Home.OpenCommand.Execute(personajeReal); DoEvents(); DoEvents(); }
            if (vm.EquipmentGroup == null) { Fallo("PREPARACION", "no hay EquipmentGroup (¿personaje sin cargar?)"); return; }

            string outDir = Environment.GetEnvironmentVariable("EQUIP_RESPONSIVE_EVIDENCIA")
                            ?? Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia", "responsive-faseB");
            Directory.CreateDirectory(outDir);
            string etiqueta = Environment.GetEnvironmentVariable("EQUIP_RESPONSIVE_ETIQUETA") ?? "actual";

            var objetosView = window.FindName("ObjetosView") as FrameworkElement;
            var pagina = objetosView?.FindName("ObjetosPaginaEquipamiento") as ScrollViewer;
            if (objetosView == null || pagina == null) { Fallo("PREPARACION", "no se encuentra ObjetosView/ObjetosPaginaEquipamiento"); return; }

            string idiomaOriginal = vm.Settings.Language;
            bool libreriaOriginal = vm.IsLibraryCollapsed;

            void IrAEquipamiento()
            {
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.RequestObjetosSection(0);
                DoEvents(); DoEvents();
            }

            void SeleccionarVista(EquipmentKind k)
            {
                var op = vm.EquipmentGroup!.KindOptions.FirstOrDefault(o => o.Value == (int)k);
                if (op != null) vm.EquipmentGroup.SelectKindCommand.Execute(op);
                DoEvents(); DoEvents();
            }

            static string Nombre(DependencyObject d) => d is FrameworkElement f
                ? $"{f.GetType().Name}{(string.IsNullOrEmpty(f.Name) ? "" : "#" + f.Name)}"
                : d.GetType().Name;

            ScrollViewer? PrimerScrollQueDesplaza(DependencyObject e, bool horizontal)
            {
                for (var d = VisualTreeHelper.GetParent(e); d != null; d = VisualTreeHelper.GetParent(d))
                {
                    if (d is ScrollViewer sv)
                    {
                        if (horizontal && sv.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled && sv.ScrollableWidth > 0.5) return sv;
                        if (!horizontal && sv.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled && sv.ScrollableHeight > 0.5) return sv;
                    }
                    if (ReferenceEquals(d, window)) break;
                }
                return null;
            }

            // ScrollViewers "de verdad" dentro de la pagina: excluye los PART_ContentHost de un
            // TextBox (su plantilla SIEMPRE trae uno, no es composicion de la pantalla).
            List<ScrollViewer> ScrollsAnidados() => Descendientes<ScrollViewer>(pagina)
                .Where(sv => sv.IsVisible && sv.TemplatedParent is not TextBoxBase)
                .ToList();

            // ---------------------------------------------------------------------------------
            // Medicion de UN estado (tamaño + idioma + libreria) - devuelve un resumen legible.
            // ---------------------------------------------------------------------------------
            void Medir(string id)
            {
                pagina.ScrollToVerticalOffset(0);
                DoEvents(); DoEvents();
                WaitForDispatcher(120);

                var grupo = vm.EquipmentGroup!;
                string cab = $"[{id}] {window.ActualWidth:0}x{window.ActualHeight:0} SizeClass={vm.SizeClass} idioma={vm.Settings.Language} libreriaPlegada={vm.IsLibraryCollapsed} vista={grupo.SelectedKind}";

                // --- A: scroll anidado del mismo eje ---
                var anidados = ScrollsAnidados();
                var anidadosV = anidados.Where(sv => sv.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled).ToList();
                string detalleAnidados = anidadosV.Count == 0 ? "ninguno" : string.Join("; ", anidadosV.Select(sv =>
                    $"{Nombre(sv)}(MaxHeight={(double.IsInfinity(sv.MaxHeight) ? "inf" : sv.MaxHeight.ToString("0"))} vp={sv.ViewportHeight:0.#} ext={sv.ExtentHeight:0.#} scr={sv.ScrollableHeight:0.#} barra={sv.ComputedVerticalScrollBarVisibility})"));
                int anidadosConBarra = anidadosV.Count(sv => sv.ComputedVerticalScrollBarVisibility == Visibility.Visible);
                Console.WriteLine($"EQUIP-RESP {cab} | pagina vp={pagina.ViewportHeight:0.#} ext={pagina.ExtentHeight:0.#} scr={pagina.ScrollableHeight:0.#} ancho={pagina.ViewportWidth:0.#} | scrollAnidadosV={anidadosV.Count} (con barra visible={anidadosConBarra}): {detalleAnidados}");
                if (anidadosV.Count > 0)
                    Fallo("SCROLL-ANIDADO", $"{cab}: {anidadosV.Count} ScrollViewer vertical(es) dentro del scroll owner de la pagina ({detalleAnidados})");

                // --- G: horizontal inesperado ---
                foreach (var sv in anidados.Prepend(pagina))
                {
                    if (sv.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled && sv.ExtentWidth > sv.ViewportWidth + 0.5)
                        Fallo("HSCROLL", $"{cab}: {Nombre(sv)} tiene HorizontalScrollBarVisibility=Disabled y su contenido mide {sv.ExtentWidth:0.#}px en un viewport de {sv.ViewportWidth:0.#}px (contenido cortado sin escape)");
                    if (sv.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
                        Fallo("HSCROLL", $"{cab}: {Nombre(sv)} muestra una barra de scroll horizontal inesperada (ScrollableWidth={sv.ScrollableWidth:0.#})");
                }

                // --- Clipping: celdas, botones y textos de la pagina ---
                var elementos = new List<FrameworkElement>();
                foreach (var sgp in Descendientes<SlotGridPanel>(pagina).Where(p => p.IsVisible))
                    elementos.AddRange(sgp.Children.OfType<FrameworkElement>());
                elementos.AddRange(Descendientes<ButtonBase>(pagina).Where(b => b.IsVisible && b.ActualWidth > 0));
                elementos.AddRange(Descendientes<TextBlock>(pagina).Where(t => t.IsVisible && t.ActualWidth > 0 && !string.IsNullOrWhiteSpace(t.Text)));
                int clipH = 0, clipVAnidado = 0, clipVPerdido = 0, bajoPliegue = 0;
                double peorH = 0, peorV = 0;
                var ejemplos = new List<string>();
                foreach (var fe in elementos.Distinct())
                {
                    var rc = RectCompleto(fe, window);
                    var z = ZonaVisible(fe, window);
                    double faltaX = z.IsEmpty ? rc.Width : Math.Min(rc.Width, Math.Max(0, z.Left - rc.Left) + Math.Max(0, rc.Right - z.Right));
                    double faltaY = z.IsEmpty ? rc.Height : Math.Min(rc.Height, Math.Max(0, z.Top - rc.Top) + Math.Max(0, rc.Bottom - z.Bottom));
                    if (faltaX > 1 && PrimerScrollQueDesplaza(fe, true) == null)
                    {
                        clipH++; peorH = Math.Max(peorH, faltaX);
                        if (ejemplos.Count < 4) ejemplos.Add($"H {Describir(fe)} {faltaX:0.#}px");
                    }
                    if (faltaY > 1)
                    {
                        var dueño = PrimerScrollQueDesplaza(fe, false);
                        if (dueño == null) { clipVPerdido++; peorV = Math.Max(peorV, faltaY); if (ejemplos.Count < 4) ejemplos.Add($"V-perdido {Describir(fe)} {faltaY:0.#}px"); }
                        else if (!ReferenceEquals(dueño, pagina)) { clipVAnidado++; peorV = Math.Max(peorV, faltaY); if (ejemplos.Count < 4) ejemplos.Add($"V-anidado({Nombre(dueño)}) {Describir(fe)} {faltaY:0.#}px"); }
                        else bajoPliegue++;
                    }
                }
                Console.WriteLine($"EQUIP-RESP {cab} | elementos={elementos.Count} clipH={clipH} (peor {peorH:0.#}px) clipV-perdido={clipVPerdido} clipV-soloScrollAnidado={clipVAnidado} (peor {peorV:0.#}px) bajoPliegueDelOwnerPrincipal={bajoPliegue} {(ejemplos.Count > 0 ? "ej: " + string.Join(" | ", ejemplos) : "")}");
                if (clipH > 0) Fallo("CLIP-H", $"{cab}: {clipH} elemento(s) recortados en horizontal sin escape (peor {peorH:0.#}px)");
                if (clipVPerdido > 0) Fallo("CLIP-V", $"{cab}: {clipVPerdido} elemento(s) recortados en vertical sin ningun scroll que los alcance (peor {peorV:0.#}px)");
                if (clipVAnidado > 0) Fallo("CLIP-V", $"{cab}: {clipVAnidado} elemento(s) solo alcanzables por un scroll ANIDADO, no por el owner principal de la pagina");

                // --- Celdas por debajo de MinCell (E) ---
                foreach (var sgp in Descendientes<SlotGridPanel>(pagina).Where(p => p.IsVisible))
                {
                    var celdas = sgp.Children.OfType<FrameworkElement>().ToList();
                    if (celdas.Count == 0) continue;
                    double min = celdas.Min(c => c.ActualWidth);
                    if (min < sgp.MinCell - 0.5)
                        Fallo("CELDA", $"{cab}: rejilla de '{(sgp.DataContext as ContainerViewModel)?.DisplayName ?? "?"}' con celdas de {min:0.#}px, por debajo de su MinCell={sgp.MinCell:0.#}");
                }

                // --- Overlap entre regiones y entre celdas de rejillas distintas ---
                var regiones = new List<(string n, Rect r)>();
                void Region(string n, FrameworkElement? fe) { if (fe != null && fe.IsVisible && fe.ActualWidth > 0) regiones.Add((n, RectCompleto(fe, window))); }
                Region("Mascotas/Tintes", Descendientes<FrameworkElement>(pagina).FirstOrDefault(f => f.Name == "CajaMascotasTintes"));
                Region("Monedas/Municion", Descendientes<FrameworkElement>(pagina).FirstOrDefault(f => f.Name == "CajaMonedasMunicion"));
                var cabecera = Descendientes<StackPanel>(pagina).FirstOrDefault(s => s.IsVisible && s.DataContext is EquipmentGroupViewModel && Descendientes<ButtonBase>(s).Any());
                Region("Cabecera Loadout/Vista/Defensa", cabecera);
                var rejillasEquip = Descendientes<SlotGridPanel>(pagina).Where(p => p.IsVisible && grupo.AllContainers.Contains(p.DataContext as ContainerViewModel)).ToList();
                foreach (var rj in rejillasEquip) Region($"Rejilla {(rj.DataContext as ContainerViewModel)?.DisplayName}", rj);
                var editar = Descendientes<Border>(objetosView).FirstOrDefault(b => b.IsVisible && b.Style == (Style)window.FindResource("SidePanelCard"));
                if (editar != null) regiones.Add(("Panel Editar", RectCompleto(editar, window)));
                regiones.Add(("Pagina Equipamiento", RectCompleto(pagina, window)));
                for (int i = 0; i < regiones.Count; i++)
                    for (int j = i + 1; j < regiones.Count; j++)
                    {
                        // La pagina contiene a todas las demas salvo Editar: solo se compara contra Editar.
                        if ((regiones[i].n == "Pagina Equipamiento" || regiones[j].n == "Pagina Equipamiento")
                            && regiones[i].n != "Panel Editar" && regiones[j].n != "Panel Editar") continue;
                        var a = regiones[i].r; var b = regiones[j].r;
                        double ix = Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left);
                        double iy = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top);
                        if (ix > 0.5 && iy > 0.5)
                            Fallo("OVERLAP", $"{cab}: '{regiones[i].n}' y '{regiones[j].n}' se pisan {ix:0.#}x{iy:0.#}px");
                    }
                var celdasTodas = Descendientes<SlotGridPanel>(pagina).Where(p => p.IsVisible)
                    .SelectMany(p => p.Children.OfType<FrameworkElement>().Select(c => (p, r: RectCompleto(c, window)))).ToList();
                int solapesCeldas = 0;
                for (int i = 0; i < celdasTodas.Count; i++)
                    for (int j = i + 1; j < celdasTodas.Count; j++)
                    {
                        if (ReferenceEquals(celdasTodas[i].p, celdasTodas[j].p)) continue;
                        var a = celdasTodas[i].r; var b = celdasTodas[j].r;
                        if (Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left) > 0.5 && Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top) > 0.5) solapesCeldas++;
                    }
                if (solapesCeldas > 0) Fallo("OVERLAP", $"{cab}: {solapesCeldas} pareja(s) de celdas de rejillas distintas se pisan");

                // --- B/F: navegacion estructural visible ENTERA sin desplazar la pagina ---
                var botonesVista = Descendientes<ButtonBase>(pagina)
                    .Where(b => b.IsVisible && b.DataContext is EquipmentOptionViewModel o && grupo.KindOptions.Contains(o)).ToList();
                var botonesLoadout = Descendientes<ButtonBase>(pagina)
                    .Where(b => b.IsVisible && b.DataContext is EquipmentOptionViewModel o && grupo.LoadoutOptions.Contains(o)).ToList();
                int vistaEnteros = botonesVista.Count(b => VisibleEntero(b, window));
                int loadoutEnteros = botonesLoadout.Count(b => VisibleEntero(b, window));
                var navs = new[] { "ObjetosNavToggle1", "ObjetosNavToggle2", "ObjetosNavToggle3" }.Select(n => objetosView.FindName(n) as FrameworkElement).ToList();
                int navEnteros = navs.Count(n => n != null && VisibleEntero(n, window));
                Console.WriteLine($"EQUIP-RESP {cab} | selector Vista: {botonesVista.Count}/3 presentes, {vistaEnteros} enteros sin desplazar ({string.Join("/", botonesVista.Select(b => (b.DataContext as EquipmentOptionViewModel)?.Label))}) | Loadout: {loadoutEnteros}/{grupo.LoadoutOptions.Count} | nav 1/2/3: {navEnteros}/3");
                if (botonesVista.Count != 3)
                    Fallo("SELECTOR", $"{cab}: el selector Armadura/Vanidad/Tintes solo tiene {botonesVista.Count}/3 botones visibles (Vanidad/Tintes sin navegacion - negative acceptance B)");
                else if (vistaEnteros != 3)
                    Fallo("SELECTOR", $"{cab}: el selector Armadura/Vanidad/Tintes no se ve entero sin desplazar la pagina ({vistaEnteros}/3)");
                if (loadoutEnteros != grupo.LoadoutOptions.Count)
                    Fallo("SELECTOR", $"{cab}: el selector de Loadout no se ve entero sin desplazar ({loadoutEnteros}/{grupo.LoadoutOptions.Count})");
                if (navEnteros != 3)
                    Fallo("SELECTOR", $"{cab}: la navegacion 1/2/3 de Objetos no se ve entera ({navEnteros}/3)");

                // --- s34: texto estructural sin scroll local ---
                if (cabecera != null)
                {
                    var conScrollLocal = new List<string>();
                    foreach (var tb in Descendientes<TextBlock>(cabecera).Where(t => t.IsVisible && !string.IsNullOrWhiteSpace(t.Text)))
                    {
                        for (var d = VisualTreeHelper.GetParent(tb); d != null && !ReferenceEquals(d, pagina); d = VisualTreeHelper.GetParent(d))
                            if (d is ScrollViewer svT)
                            {
                                conScrollLocal.Add($"{Describir(tb)} (en {Nombre(svT)} MaxHeight={svT.MaxHeight})");
                                break;
                            }
                        if (tb.TextTrimming != TextTrimming.None)
                            Fallo("TEXTO-LOCAL", $"{cab}: texto estructural '{Describir(tb)}' usa TextTrimming={tb.TextTrimming}");
                    }
                    if (conScrollLocal.Count > 0)
                        Fallo("TEXTO-LOCAL", $"{cab}: {conScrollLocal.Count} texto(s) estructurales de la cabecera viven dentro de un ScrollViewer propio en vez de crecer y dejar el scroll al owner de la pagina (ej. {conScrollLocal[0]})");
                }

                // Panel Editar (s7): hermano de la pagina, no anidado - se informa su scroll propio
                // (hoy ItemEditTemplate lleva un ScrollViewer de seguridad, L-d) para que la
                // decision de s7 se tome con numeros reales.
                if (editar != null)
                {
                    var svEditar = Descendientes<ScrollViewer>(editar).FirstOrDefault(sv => sv.IsVisible && sv.TemplatedParent is not TextBoxBase);
                    Console.WriteLine($"EQUIP-RESP {cab} | Editar: {editar.ActualWidth:0.#}x{editar.ActualHeight:0.#} scrollPropio vp={svEditar?.ViewportHeight:0.#} ext={svEditar?.ExtentHeight:0.#} scr={svEditar?.ScrollableHeight:0.#} slotSeleccionado={(vm.ItemEdit.Slot != null)}");
                }

                // --- Resumen de composicion ---
                var host = Descendientes<SlotRowHost>(pagina).FirstOrDefault(h => h.IsVisible);
                string cols = host == null ? "(sin SlotRowHost)" : string.Join("/", host.ColumnDefinitions.Select(c => $"{c.ActualWidth:0.#}"));
                string rejillas = string.Join(" ", Descendientes<SlotGridPanel>(pagina).Where(p => p.IsVisible && p.Children.Count > 0)
                    .Select(p => $"{(p.DataContext as ContainerViewModel)?.Key ?? "?"}={((FrameworkElement)p.Children[0]).ActualWidth:0.#}px"));
                Console.WriteLine($"EQUIP-RESP {cab} | columnas fila={cols} | celdas: {rejillas} | Editar={(editar == null ? "?" : $"{editar.ActualWidth:0.#}x{editar.ActualHeight:0.#}")}");

                string shot = Path.Combine(outDir, $"equip-{etiqueta}-{id}.png");
                File.WriteAllBytes(shot, CapturarPng(window, window.ActualWidth, window.ActualHeight));
                Console.WriteLine($"EQUIP-RESP {cab} | captura -> {shot}");
            }

            // ---------------------------------------------------------------------------------
            // 1) Tamaños: minimo declarado, intermedio, grande y maximizado real, ES, Libreria
            //    plegada y desplegada (compite de verdad por el alto de la fila de Objetos).
            // ---------------------------------------------------------------------------------
            vm.Settings.Language = "es"; DoEvents();
            // Un slot con objeto seleccionado: el panel Editar muestra su contenido real (el
            // estado mas alto de ese panel), no el aviso "selecciona un hueco".
            IrAEquipamiento(); SeleccionarVista(EquipmentKind.Items);
            var slotConObjeto = vm.EquipmentGroup.Current.Slots.FirstOrDefault(s => !s.IsEmpty) ?? vm.EquipmentGroup.Current.Slots.FirstOrDefault();
            if (slotConObjeto != null) { vm.SelectSlot(slotConObjeto); DoEvents(); }
            var casos = new (string id, double w, double h, bool plegada)[]
            {
                ("min-1080x700-libplegada", 1080, 700, true),
                ("min-1080x700-libdesplegada", 1080, 700, false),
                ("medio-1366x768-libplegada", 1366, 768, true),
                ("medio-1366x768-libdesplegada", 1366, 768, false),
                ("grande-1920x1080-libdesplegada", 1920, 1080, false),
            };
            foreach (var (id, w, h, plegada) in casos)
            {
                window.WindowState = WindowState.Normal; DoEvents();
                FijarTamaño(window, w, h);
                vm.IsLibraryCollapsed = plegada;
                IrAEquipamiento();
                SeleccionarVista(EquipmentKind.Items);
                Medir(id);
            }
            // Maximizado real (monitor de esta maquina).
            vm.IsLibraryCollapsed = false;
            window.WindowState = WindowState.Maximized; DoEvents(); DoEvents(); DoEvents();
            IrAEquipamiento(); SeleccionarVista(EquipmentKind.Items);
            Medir("maximizado-libdesplegada");
            window.WindowState = WindowState.Normal; DoEvents();

            // Las 3 subvistas en el minimo (s22: "ah, tambien existe Vanidad" no debe pasar).
            FijarTamaño(window, 1080, 700); vm.IsLibraryCollapsed = true; IrAEquipamiento();
            SeleccionarVista(EquipmentKind.Social); Medir("min-1080x700-vanidad");
            SeleccionarVista(EquipmentKind.Dyes); Medir("min-1080x700-tintes");

            // ---------------------------------------------------------------------------------
            // 2) ES / EN en el minimo (s25: los textos en español son mas largos).
            // ---------------------------------------------------------------------------------
            vm.Settings.Language = "en"; DoEvents(); DoEvents();
            SeleccionarVista(EquipmentKind.Items);
            vm.IsLibraryCollapsed = false; IrAEquipamiento();
            Medir("min-1080x700-EN-libdesplegada");
            vm.Settings.Language = "es"; DoEvents(); DoEvents();

            // ---------------------------------------------------------------------------------
            // 3) Redimensionado en caliente (s24): grande -> normal -> minimo -> grande sin perder
            //    subpagina, vista, loadout, slot seleccionado (Editar), libreria ni ViewModel.
            // ---------------------------------------------------------------------------------
            FijarTamaño(window, 1920, 1080); IrAEquipamiento();
            vm.IsLibraryCollapsed = false;
            SeleccionarVista(EquipmentKind.Social);
            var grupoAntes = vm.EquipmentGroup;
            var loadoutElegido = vm.EquipmentGroup.LoadoutOptions.Count > 1 ? vm.EquipmentGroup.LoadoutOptions[1] : vm.EquipmentGroup.LoadoutOptions.FirstOrDefault();
            if (loadoutElegido != null) { vm.EquipmentGroup.SelectLoadoutCommand.Execute(loadoutElegido); DoEvents(); }
            var slotElegido = vm.EquipmentGroup.Current.Slots.FirstOrDefault();
            if (slotElegido != null) vm.SelectSlot(slotElegido);
            vm.Library.SearchText = "sword"; DoEvents(); DoEvents();
            var kindAntes = vm.EquipmentGroup.SelectedKind; int loadoutAntes = vm.EquipmentGroup.SelectedLoadout;
            foreach (var (paso, w, h) in new[] { ("grande", 1920.0, 1080.0), ("normal", 1366.0, 768.0), ("minimo", 1080.0, 700.0), ("grande-vuelta", 1920.0, 1080.0) })
            {
                FijarTamaño(window, w, h);
                DoEvents(); DoEvents();
                bool ok = ReferenceEquals(vm.EquipmentGroup, grupoAntes)
                          && vm.ObjetosSubTabIndex == 0
                          && vm.EquipmentGroup!.SelectedKind == kindAntes
                          && vm.EquipmentGroup.SelectedLoadout == loadoutAntes
                          && ReferenceEquals(vm.ItemEdit.Slot, slotElegido)
                          && !vm.IsLibraryCollapsed
                          && vm.Library.SearchText == "sword";
                Console.WriteLine($"EQUIP-RESP RESIZE {paso} {w:0}x{h:0}: mismoVM={ReferenceEquals(vm.EquipmentGroup, grupoAntes)} subpagina={vm.ObjetosSubTabIndex} vista={vm.EquipmentGroup!.SelectedKind} loadout={vm.EquipmentGroup.SelectedLoadout} slotEditar={(ReferenceEquals(vm.ItemEdit.Slot, slotElegido) ? "igual" : "PERDIDO")} libreriaPlegada={vm.IsLibraryCollapsed} busqueda='{vm.Library.SearchText}'");
                if (!ok) Fallo("RESIZE", $"en el paso '{paso}' ({w:0}x{h:0}) se perdio estado (subpagina/vista/loadout/slot Editar/libreria/busqueda o se recreo el ViewModel)");
                Medir($"resize-{paso}");
            }
            vm.Library.SearchText = string.Empty;
            if (vm.EquipmentGroup.LoadoutOptions.Count > 0) vm.EquipmentGroup.SelectLoadoutCommand.Execute(vm.EquipmentGroup.LoadoutOptions[0]);
            SeleccionarVista(EquipmentKind.Items);

            // ---------------------------------------------------------------------------------
            // 4) Negative acceptance del mecanismo VIEJO (s26/s28): no basta con que lo nuevo
            //    funcione - lo viejo no puede seguir activo por debajo.
            // ---------------------------------------------------------------------------------
            FijarTamaño(window, 1920, 1080); IrAEquipamiento(); DoEvents();
            var svConMaxHeight = Descendientes<ScrollViewer>(pagina).Where(sv => !double.IsInfinity(sv.MaxHeight)).ToList();
            Console.WriteLine($"EQUIP-RESP VIEJO: ScrollViewer con MaxHeight dentro de Equipamiento = {svConMaxHeight.Count} (esperado 0, antes 1 con MaxHeight=112)");
            if (svConMaxHeight.Count > 0) Fallo("VIEJO", $"sigue existiendo un ScrollViewer con MaxHeight dentro de Equipamiento ({string.Join(", ", svConMaxHeight.Select(s => s.MaxHeight.ToString("0")))}) - la caja de scroll local de la cabecera Loadout/Vista/Defensa/bono");
            var todosSv = Descendientes<ScrollViewer>(pagina).Where(sv => sv.TemplatedParent is not TextBoxBase).ToList();
            Console.WriteLine($"EQUIP-RESP VIEJO: ScrollViewer en el arbol de Equipamiento (visibles o no) = {todosSv.Count} (esperado 0, antes 5 niveles: fila, lateral, cabecera, centro y el de ContainerCompactTemplate)");
            if (todosSv.Count > 0) Fallo("VIEJO", $"quedan {todosSv.Count} ScrollViewer en el arbol de Equipamiento aunque esten ocultos - el mecanismo viejo sigue presente por debajo del nuevo");
            // La rejilla "Amplio" de 3 columnas (Items/Social/Dyes simultaneos, Visibility ligada a
            // IsEquipmentExpanded): en 1920 era la que se mostraba. Si existe un contenedor que
            // enseñe CurrentItems y CurrentSocial a la vez, el modelo viejo sigue vivo.
            var contenedoresMostrados = Descendientes<SlotGridPanel>(pagina).Select(p => p.DataContext).OfType<ContainerViewModel>().ToList();
            bool tresALaVez = contenedoresMostrados.Contains(vm.EquipmentGroup.CurrentItems) && contenedoresMostrados.Contains(vm.EquipmentGroup.CurrentSocial);
            Console.WriteLine($"EQUIP-RESP VIEJO: a 1920x1080 Armadura y Vanidad renderizadas a la vez = {tresALaVez} (esperado False - una sola subvista, misma navegacion en todos los tamaños)");
            if (tresALaVez) Fallo("VIEJO", "a 1920x1080 sigue existiendo la rejilla 'Amplio' de 3 subvistas simultaneas (modelo que cambia la arquitectura con el tamaño)");
            bool propiedadVieja = typeof(MainViewModel).GetProperty("IsEquipmentExpanded") != null;
            Console.WriteLine($"EQUIP-RESP VIEJO: MainViewModel.IsEquipmentExpanded existe = {propiedadVieja} (esperado False)");
            if (propiedadVieja) Fallo("VIEJO", "MainViewModel.IsEquipmentExpanded sigue existiendo (modelo visual que cambia con el tamaño)");

            vm.Settings.Language = idiomaOriginal;
            vm.IsLibraryCollapsed = libreriaOriginal;
            FijarTamaño(window, 1180, 860);
            DoEvents();
        }
        catch (Exception ex) { fallos++; Console.WriteLine("EQUIP-RESPONSIVE-EXCEPTION: " + ex); }
        Console.WriteLine($"EQUIP_RESPONSIVE_SOLO: {fallos} fallo(s)");
    }
}
