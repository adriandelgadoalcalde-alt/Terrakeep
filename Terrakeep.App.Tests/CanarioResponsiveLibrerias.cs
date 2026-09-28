// CANARIO REAL - FASE D del responsive global (PDF "Arreglo familia keep", bloque 2 "TERRAKEEP -
// RESPONSIVE GLOBAL, PAGINACION Y SCROLL COMO ULTIMO RECURSO", secciones 7-17, 19, 22-26, 28-29, 31 y
// 34). 28-sep-2026, aplicador-fix-responsive-faseD.
//
// LIBRARY_RESPONSIVE_SOLO=1 mide la FAMILIA "catalogo con categorias" - Personaje > Objetos > Libreria,
// Personaje > Buffs > Libreria de buffs y Personaje > Investigacion - mas el panel Editar de Objetos
// (s7), con geometria REAL (RectCompleto vs RectVisible/ZonaVisible, el mismo par de AR-LAY/NAV123/
// EQUIP/INVALM) en varios tamaños, ES/EN, colecciones grandes (s23), primera entrada con un ViewModel
// recien creado y redimensionado en caliente (s24). Falla (lineas "FALLO: LIBRARY_RESPONSIVE_SOLO-<codigo>")
// si aparece:
//   - SCROLL-ANIDADO  (s12/s13/s26 A/H) mas de UN scroll owner vertical en la superficie (p.ej. arbol de
//                         categorias + resultados + tarjetas), o uno dentro de otro, o la superficie entera
//                         dentro de un scroll exterior que desplaza.
//   - CATEGORIAS      (s9/s12/s26 C) alguna categoria PRINCIPAL (raiz) o "Ver todo" no se ve ENTERA sin
//                         desplazar nada.
//   - ESTRUCTURAL     (s23/s26 F) buscador, boton de filtros o resumen no se ven enteros.
//   - RESULTADOS      el viewport de resultados no enseña ni una fila entera de celdas.
//   - COLUMNAS        (s8/s17) con muchos resultados la rejilla usa menos del 85% del ancho (columnas fijas
//                         que dejan media franja vacia).
//   - CLIP-H / CLIP-V / HSCROLL / OVERLAP  (s26 D/E/F/G) como en los canarios hermanos.
//   - CONTRASTE       la categoria SELECCIONADA por debajo de 4,5:1 (texto sobre su fondo real).
//   - EDITAR-SCROLL / EDITAR-CLIP  (s7, permiso aa5f7395 y known-diff 570a3c9d) el panel Editar desplaza o
//                         recorta algo, con Cenit y con el objeto de prefijos mas largo que se encuentre.
//   - BANDA-EQUIP     franja vacia > 40px entre Equipamiento y la Libreria en ventana grande (s17).
//   - ALM-EJE         (R2-L1) una linea de pildoras/acciones de Almacenes descentrada respecto a la rejilla.
//   - RESIZE          (s24) perdida de estado al redimensionar.
//   - VIEJO           (s26/s28 negative acceptance) el mecanismo viejo sigue existiendo: arbol de categorias
//                         con ScrollViewer propio (columna fija de 210px), tope LibraryRowMaxHeight, ScrollViewer
//                         dentro del panel Editar.
//
// Solo abre una COPIA del personaje (carpeta del arnes, AislamientoPartidasReales.cs), nunca guarda.
// Capturas en LIBRARY_RESPONSIVE_EVIDENCIA (si se define) o en <bin>/keepqa-evidencia/responsive-faseD.
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Terrakeep.App;
using Terrakeep.App.Controls;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static void EjecutarLibreriasResponsiveSolo(MainWindow window, MainViewModel vm)
    {
        int fallos = 0;
        void Fallo(string codigo, string msg) { fallos++; Console.WriteLine($"FALLO: LIBRARY_RESPONSIVE_SOLO-{codigo} - {msg}"); }

        try
        {
            // --- Aislamiento: copia propia del personaje, abierta por ruta (mismo patron que INVALM/EQUIP).
            var origen = vm.Home.Characters.FirstOrDefault(c => EstaDentro(c.FilePath, RaizPersonajesAislada))?.FilePath;
            if (origen == null) { Fallo("AISLAMIENTO", "no hay ninguna copia aislada de personaje que abrir"); return; }
            string carpetaCanario = Path.Combine(RaizPersonajesAislada, "canario-librerias", "tModLoader", "Players");
            Directory.CreateDirectory(carpetaCanario);
            string rutaCanario = Path.Combine(carpetaCanario, Path.GetFileName(origen));
            File.Copy(origen, rutaCanario, overwrite: true);
            string tplrOrigen = Path.ChangeExtension(origen, ".tplr");
            if (File.Exists(tplrOrigen)) File.Copy(tplrOrigen, Path.ChangeExtension(rutaCanario, ".tplr"), overwrite: true);
            vm.IsDirty = false;
            vm.LoadFromPath(rutaCanario);
            DoEvents(); DoEvents();
            ComprobarPersonajeAislado(vm, "LIBRARY_RESPONSIVE_SOLO");
            if (!string.Equals(vm.LoadedFilePath, rutaCanario, StringComparison.OrdinalIgnoreCase))
            { Fallo("AISLAMIENTO", $"el personaje cargado ({vm.LoadedFilePath}) no es la copia del canario"); Environment.Exit(4); }
            Console.WriteLine($"LIB AISLAMIENTO: abierto por ruta la COPIA '{vm.LoadedFilePath}'");
            if (vm.InventoryContainer == null || vm.StorageGroup == null || vm.EquipmentGroup == null) { Fallo("PREPARACION", "personaje sin contenedores"); return; }

            string outDir = Environment.GetEnvironmentVariable("LIBRARY_RESPONSIVE_EVIDENCIA")
                            ?? Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia", "responsive-faseD");
            Directory.CreateDirectory(outDir);
            string etiqueta = Environment.GetEnvironmentVariable("LIBRARY_RESPONSIVE_ETIQUETA") ?? "actual";

            var objetosView = window.FindName("ObjetosView") as FrameworkElement;
            if (objetosView == null) { Fallo("PREPARACION", "no se encuentra ObjetosView"); return; }
            var libSearch = objetosView.FindName("LibrarySearchBox") as TextBox;
            var buffSearch = window.FindName("BuffLibrarySearchBox") as TextBox;
            if (libSearch == null || buffSearch == null) { Fallo("PREPARACION", "no se encuentran los buscadores de las librerias"); return; }

            string idiomaOriginal = vm.Settings.Language;
            bool libreriaOriginal = vm.IsLibraryCollapsed, buffLibOriginal = vm.IsBuffLibraryCollapsed, compactoOriginal = vm.Settings.IsCompactMode;
            vm.Settings.Language = "es"; vm.Settings.IsCompactMode = false; DoEvents();

            // Inventario con objetos (lo que el usuario tiene al trabajar) - solo en memoria, sobre la copia.
            {
                int id = 1;
                foreach (var s in vm.InventoryContainer.Slots.Where(s => s.IsEmpty)) { s.PlaceItem(id); id = id % 400 + 1; }
                vm.IsDirty = false;
            }

            static string Nombre(DependencyObject d) => d is FrameworkElement f
                ? $"{f.GetType().Name}{(string.IsNullOrEmpty(f.Name) ? "" : "#" + f.Name)}"
                : d.GetType().Name;

            bool EnteroEnAmbosEjes(FrameworkElement fe)
            {
                try
                {
                    if (!fe.IsVisible || fe.ActualWidth <= 0 || fe.ActualHeight <= 0) return false;
                    var r = RectVisible(fe, window);
                    return !r.IsEmpty && r.Height >= fe.ActualHeight - 0.5 && r.Width >= fe.ActualWidth - 0.5;
                }
                catch (InvalidOperationException) { return false; }
            }

            ScrollViewer? PrimerScrollQueDesplaza(DependencyObject e, bool horizontal, DependencyObject? hasta = null)
            {
                for (var d = VisualTreeHelper.GetParent(e); d != null; d = VisualTreeHelper.GetParent(d))
                {
                    if (d is ScrollViewer sv && sv.TemplatedParent is not TextBoxBase)
                    {
                        if (horizontal && sv.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled && sv.ScrollableWidth > 0.5) return sv;
                        if (!horizontal && sv.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled && sv.ScrollableHeight > 0.5) return sv;
                    }
                    if (ReferenceEquals(d, window) || (hasta != null && ReferenceEquals(d, hasta))) break;
                }
                return null;
            }

            // Region de una libreria (Objetos/Buffs): el elemento de la fila 1 de su Grid de 2 filas que
            // contiene su buscador. Igual en el codigo viejo y en el nuevo (la Libreria vive en la fila 1).
            FrameworkElement? RegionFila1(FrameworkElement desde)
            {
                for (DependencyObject? d = desde; d != null; d = VisualTreeHelper.GetParent(d))
                {
                    if (d is FrameworkElement fe && VisualTreeHelper.GetParent(fe) is Grid g && g.RowDefinitions.Count == 2 && Grid.GetRow(fe) == 1)
                        return fe;
                    if (ReferenceEquals(d, window)) break;
                }
                return null;
            }
            // Region de Investigacion: la raiz del contenido de su pestaña (hija del ContentPresenter de la
            // TabControl interna de Personaje).
            FrameworkElement? RaizPestaña(FrameworkElement desde)
            {
                for (DependencyObject? d = desde; d != null; d = VisualTreeHelper.GetParent(d))
                {
                    if (d is FrameworkElement fe && VisualTreeHelper.GetParent(fe) is ContentPresenter cp && cp.Name == "PART_SelectedContentHost")
                        return fe;
                    if (ReferenceEquals(d, window)) break;
                }
                return null;
            }
            TextBox? BuscadorInvestigacion() => Descendientes<TextBox>(window).FirstOrDefault(t => t.IsVisible &&
                BindingOperations.GetBindingExpression(t, TextBox.TextProperty)?.ParentBinding.Path.Path == "Research.SearchText");

            void IrA(int pestañaInterna, int paginaObjetos = -1)
            {
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = pestañaInterna;
                if (pestañaInterna == 0 && paginaObjetos >= 0) vm.RequestObjetosSection(paginaObjetos);
                DoEvents(); DoEvents(); WaitForDispatcher(60);
            }

            var resumen = new List<string>();

            // Brushes -> color(es) reales para medir contraste.
            static IEnumerable<Color> Colores(Brush? b) => b switch
            {
                SolidColorBrush s => [s.Color],
                GradientBrush g => g.GradientStops.Select(x => x.Color),
                _ => [],
            };
            static double Luminancia(Color c)
            {
                static double Canal(byte v) { double s = v / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
                return 0.2126 * Canal(c.R) + 0.7152 * Canal(c.G) + 0.0722 * Canal(c.B);
            }
            static double Contraste(Color a, Color b)
            {
                double la = Luminancia(a), lb = Luminancia(b);
                return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
            }
            // Contraste REAL en pixeles: se renderiza el boton, el fondo es el color mas frecuente, el texto
            // el pixel de mayor contraste contra ese fondo fuera de las imagenes (iconos) del boton.
            (double ratio, Color fondo, Color texto) ContrastePixeles(FrameworkElement boton)
            {
                int w = Math.Max(1, (int)Math.Round(boton.ActualWidth)), h = Math.Max(1, (int)Math.Round(boton.ActualHeight));
                var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(boton);
                var px = new byte[w * h * 4];
                rtb.CopyPixels(px, w * 4, 0);
                var excluir = Descendientes<Image>(boton).Where(i => i.IsVisible && i.ActualWidth > 0)
                    .Select(i => i.TransformToAncestor(boton).TransformBounds(new Rect(0, 0, i.ActualWidth, i.ActualHeight))).ToList();
                var cuenta = new Dictionary<uint, int>();
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int i = (y * w + x) * 4;
                        if (px[i + 3] < 250) continue; // bordes redondeados / fuera del fondo
                        uint k = (uint)(px[i] | px[i + 1] << 8 | px[i + 2] << 16);
                        cuenta[k] = cuenta.GetValueOrDefault(k) + 1;
                    }
                if (cuenta.Count == 0) return (0, Colors.Transparent, Colors.Transparent);
                uint kf = cuenta.OrderByDescending(kv => kv.Value).First().Key;
                var fondo = Color.FromRgb((byte)(kf >> 16), (byte)(kf >> 8), (byte)kf);
                double mejor = 1; Color texto = fondo;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        if (excluir.Any(r => r.Contains(new Point(x + 0.5, y + 0.5)))) continue;
                        int i = (y * w + x) * 4;
                        if (px[i + 3] < 250) continue;
                        var c = Color.FromRgb(px[i + 2], px[i + 1], px[i]);
                        double cr = Contraste(c, fondo);
                        if (cr > mejor) { mejor = cr; texto = c; }
                    }
                return (mejor, fondo, texto);
            }

            // ---------------------------------------------------------------------------------
            // Medicion de UNA superficie del catalogo (Libreria / Libreria de buffs / Investigacion).
            // ---------------------------------------------------------------------------------
            void MedirCatalogo(string superficie, string id, System.Collections.IList resultados, System.Collections.IList raices, System.Windows.Input.ICommand limpiarCategoria)
            {
                DoEvents(); DoEvents(); WaitForDispatcher(120);
                FrameworkElement? region = superficie switch
                {
                    "Libreria" => RegionFila1(libSearch),
                    "LibreriaBuffs" => RegionFila1(buffSearch),
                    _ => BuscadorInvestigacion() is { } tb ? RaizPestaña(tb) : null,
                };
                string cab = $"[{id}] {superficie} {window.ActualWidth:0}x{window.ActualHeight:0} SizeClass={vm.SizeClass} idioma={vm.Settings.Language} compacto={vm.Settings.IsCompactMode}";
                if (region == null || !region.IsVisible) { Fallo("PREPARACION", $"{cab}: no se encuentra la region visible de la superficie"); return; }
                var rr = RectCompleto(region, window);

                // --- s13/s26 A/H: scroll owners verticales visibles dentro de la superficie ---
                var svs = Descendientes<ScrollViewer>(region).Where(sv => sv.IsVisible && sv.TemplatedParent is not TextBoxBase && sv.ActualHeight > 0).ToList();
                var svV = svs.Where(sv => sv.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled).ToList();
                string det = string.Join("; ", svV.Select(sv => $"{Nombre(sv)}(vp={sv.ViewportHeight:0.#} ext={sv.ExtentHeight:0.#} scr={sv.ScrollableHeight:0.#} {RectCompleto(sv, window).Width:0}x{RectCompleto(sv, window).Height:0})"));
                var exterior = PrimerScrollQueDesplaza(region, false);
                Console.WriteLine($"LIB {cab} | region {rr.Width:0.#}x{rr.Height:0.#} | scroll owners V={svV.Count}: {det} | scroll exterior que desplaza={(exterior == null ? "ninguno" : Nombre(exterior))}");
                if (svV.Count > 1) Fallo("SCROLL-ANIDADO", $"{cab}: {svV.Count} scroll owners verticales en la misma superficie ({det}) - s13 pide UNO (resultados)");
                foreach (var sv in svV)
                    if (svV.Any(o => !ReferenceEquals(o, sv) && o.IsAncestorOf(sv)))
                        Fallo("SCROLL-ANIDADO", $"{cab}: {Nombre(sv)} vive dentro de otro ScrollViewer vertical");
                if (exterior != null) Fallo("SCROLL-ANIDADO", $"{cab}: la superficie entera vive dentro de un scroll exterior que desplaza ({Nombre(exterior)})");

                // --- s9/s12/s26 C: categorias principales + "Ver todo" enteras sin desplazar nada ---
                var navCard = window.TryFindResource("NavCardButton") as Style;
                var botonesCat = Descendientes<ButtonBase>(region).Where(b => b.IsVisible && b.DataContext is CategoryNodeViewModel && b.Style != navCard).ToList();
                int raicesEnteras = 0; var faltan = new List<string>();
                foreach (var raiz in raices.Cast<CategoryNodeViewModel>())
                {
                    var b = botonesCat.FirstOrDefault(x => ReferenceEquals(x.DataContext, raiz));
                    if (b != null && EnteroEnAmbosEjes(b)) raicesEnteras++; else faltan.Add(raiz.Name);
                }
                var verTodo = Descendientes<ButtonBase>(region).FirstOrDefault(b => b.IsVisible && ReferenceEquals(b.Command, limpiarCategoria));
                bool verTodoOk = verTodo != null && EnteroEnAmbosEjes(verTodo);
                var rCats = botonesCat.Where(b => b.IsVisible).Select(b => RectCompleto(b, window)).ToList();
                string zonaCat = rCats.Count == 0 ? "-" : $"{rCats.Min(r => r.Left) - rr.Left:0},{rCats.Min(r => r.Top) - rr.Top:0} {rCats.Max(r => r.Right) - rCats.Min(r => r.Left):0}x{rCats.Max(r => r.Bottom) - rCats.Min(r => r.Top):0}";
                Console.WriteLine($"LIB {cab} | categorias principales enteras sin desplazar={raicesEnteras}/{raices.Count} 'Ver todo' entero={verTodoOk} | zona de navegacion de categorias {zonaCat}{(faltan.Count > 0 ? " | NO visibles: " + string.Join(", ", faltan) : "")}");
                if (raicesEnteras < raices.Count) Fallo("CATEGORIAS", $"{cab}: solo {raicesEnteras}/{raices.Count} categorias principales se ven enteras sin desplazar (faltan: {string.Join(", ", faltan)})");
                if (!verTodoOk) Fallo("CATEGORIAS", $"{cab}: 'Ver todo' no se ve entero");

                // --- s23: buscador / filtros / resumen enteros ---
                var buscador = Descendientes<TextBox>(region).FirstOrDefault(t => t.IsVisible);
                var estructurales = new List<FrameworkElement>();
                if (buscador != null) estructurales.Add(buscador); else Fallo("ESTRUCTURAL", $"{cab}: sin buscador visible");
                if (superficie == "Libreria" && objetosView.FindName("LibraryFiltersButton") is FrameworkElement fb) estructurales.Add(fb);
                foreach (var e in estructurales.Where(e => !EnteroEnAmbosEjes(e)))
                    Fallo("ESTRUCTURAL", $"{cab}: {Describir(e)} no se ve entero");

                // --- Resultados: el owner, celdas, columnas y uso del ancho ---
                var icResultados = Descendientes<ItemsControl>(region).FirstOrDefault(ic => ic.IsVisible && ReferenceEquals(ic.ItemsSource, resultados));
                var icTarjetas = Descendientes<ItemsControl>(region).FirstOrDefault(ic => ic.IsVisible && ReferenceEquals(ic.ItemsSource, raices) && Descendientes<ButtonBase>(ic).Any(b => b.Style == navCard));
                var contenido = (FrameworkElement?)icResultados ?? icTarjetas;
                string resTxt = "sin resultados visibles";
                if (contenido != null)
                {
                    var owner = svV.FirstOrDefault(sv => sv.IsAncestorOf(contenido));
                    var celdas = Enumerable.Range(0, (contenido as ItemsControl)!.Items.Count)
                        .Select(i => (contenido as ItemsControl)!.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement)
                        .Where(c => c != null && c.IsVisible && c.ActualWidth > 0).Cast<FrameworkElement>().ToList();
                    int enteras = celdas.Count(EnteroEnAmbosEjes);
                    var rs = celdas.Select(c => RectCompleto(c, window)).ToList();
                    int cols = rs.Count == 0 ? 0 : rs.Select(r => Math.Round(r.Left)).Distinct().Count();
                    int filas = rs.Count == 0 ? 0 : rs.Select(r => Math.Round(r.Top)).Distinct().Count();
                    double anchoUsado = rs.Count == 0 ? 0 : rs.Max(r => r.Right) - rs.Min(r => r.Left);
                    double anchoDisp = owner?.ViewportWidth ?? contenido.ActualWidth;
                    double celda = rs.Count == 0 ? 0 : rs.Min(r => r.Width);
                    double vpAlto = owner?.ViewportHeight ?? 0;
                    var sgp = Descendientes<SlotGridPanel>(contenido).FirstOrDefault();
                    resTxt = $"{(ReferenceEquals(contenido, icTarjetas) ? "tarjetas de carpeta raiz" : "resultados")} n={celdas.Count} enteras sin desplazar={enteras} celda={celda:0.#}px cols={cols} filas={filas} ancho usado {anchoUsado:0}/{anchoDisp:0}px ({(anchoDisp > 0 ? anchoUsado / anchoDisp : 0):P0}) | owner={(owner == null ? "NINGUNO" : $"{Nombre(owner)} vp={owner.ViewportHeight:0.#} ext={owner.ExtentHeight:0.#} scr={owner.ScrollableHeight:0.#}")}{(sgp != null ? $" | SlotGridPanel Columns={sgp.Columns} Adaptive={sgp.AdaptiveColumns} MinCell={sgp.MinCell:0.#} MaxCell={sgp.MaxCell:0.#}" : "")}";
                    // Rejilla de resultados: al menos una celda ENTERA sin desplazar. Tarjetas de carpeta raiz (estado
                    // "Ver todo", un mapa navegable dentro del mismo owner): viewport util de al menos una fila de
                    // MinCell (40px) - la fila de categorias principales ya esta siempre visible encima.
                    bool modoTarjetas = ReferenceEquals(contenido, icTarjetas);
                    if (!modoTarjetas && celdas.Count > 0 && enteras == 0) Fallo("RESULTADOS", $"{cab}: el viewport de resultados (alto {vpAlto:0.#}px) no enseña ni una celda entera");
                    if (modoTarjetas && vpAlto < 44) Fallo("RESULTADOS", $"{cab}: el viewport de las tarjetas raiz mide {vpAlto:0.#}px (< una fila de 40px)");
                    // s8/s17: colecciones grandes -> la rejilla llena el ancho (columnas adaptativas).
                    if (ReferenceEquals(contenido, icResultados) && sgp != null && celdas.Count >= 60 && anchoDisp > 0 && anchoUsado / anchoDisp < 0.85)
                        Fallo("COLUMNAS", $"{cab}: con {celdas.Count} resultados la rejilla usa solo {anchoUsado:0}/{anchoDisp:0}px ({anchoUsado / anchoDisp:P0}) - {cols} columnas fijas de {celda:0.#}px");
                    if (ReferenceEquals(contenido, icTarjetas) && owner != null)
                    {
                        double ocupado = rs.Count == 0 ? 0 : rs.Max(r => r.Bottom) - RectCompleto(owner, window).Top;
                        Console.WriteLine($"LIB {cab} | tarjetas raiz: alto ocupado {ocupado:0}px de {owner.ViewportHeight:0}px del viewport ({(owner.ViewportHeight > 0 ? ocupado / owner.ViewportHeight : 0):P0})");
                    }
                }
                Console.WriteLine($"LIB {cab} | {resTxt}");

                // --- G: horizontal inesperado ---
                foreach (var sv in svs)
                {
                    if (sv.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled && sv.ExtentWidth > sv.ViewportWidth + 0.5)
                        Fallo("HSCROLL", $"{cab}: {Nombre(sv)} Disabled con contenido de {sv.ExtentWidth:0.#}px en {sv.ViewportWidth:0.#}px");
                    if (sv.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
                        Fallo("HSCROLL", $"{cab}: {Nombre(sv)} muestra barra horizontal");
                }

                // --- Clipping de botones/textos fuera de los resultados (estructura) ---
                var elementos = Descendientes<ButtonBase>(region).Where(b => b.IsVisible && b.ActualWidth > 0).Cast<FrameworkElement>()
                    .Concat(Descendientes<TextBlock>(region).Where(t => t.IsVisible && t.ActualWidth > 0 && !string.IsNullOrWhiteSpace(t.Text)))
                    .Where(e => contenido == null || !contenido.IsAncestorOf(e))
                    .Where(e => !Descendientes<ScrollBar>(region).Any(sb => sb.IsAncestorOf(e)))
                    .Distinct().ToList();
                int clipH = 0, clipV = 0, clipVAnidado = 0; var ej = new List<string>();
                foreach (var fe in elementos)
                {
                    var rc = RectCompleto(fe, window); var z = ZonaVisible(fe, window);
                    double fx = z.IsEmpty ? rc.Width : Math.Min(rc.Width, Math.Max(0, z.Left - rc.Left) + Math.Max(0, rc.Right - z.Right));
                    double fy = z.IsEmpty ? rc.Height : Math.Min(rc.Height, Math.Max(0, z.Top - rc.Top) + Math.Max(0, rc.Bottom - z.Bottom));
                    if (fx > 1 && PrimerScrollQueDesplaza(fe, true) == null) { clipH++; if (ej.Count < 4) ej.Add($"H {Describir(fe)} {fx:0.#}px"); }
                    if (fy > 1)
                    {
                        var d = PrimerScrollQueDesplaza(fe, false);
                        if (d == null) { clipV++; if (ej.Count < 4) ej.Add($"V {Describir(fe)} {fy:0.#}px"); }
                        else { clipVAnidado++; if (ej.Count < 4) ej.Add($"V-solo-con-scroll({Nombre(d)}) {Describir(fe)} {fy:0.#}px"); }
                    }
                }
                Console.WriteLine($"LIB {cab} | estructura: elementos={elementos.Count} clipH={clipH} clipV-perdido={clipV} clipV-tras-scroll-local={clipVAnidado} {(ej.Count > 0 ? "ej: " + string.Join(" | ", ej) : "")}");
                if (clipH > 0) Fallo("CLIP-H", $"{cab}: {clipH} elemento(s) estructurales recortados en horizontal");
                if (clipV > 0) Fallo("CLIP-V", $"{cab}: {clipV} elemento(s) estructurales recortados en vertical sin escape");
                if (clipVAnidado > 0) Fallo("CATEGORIAS", $"{cab}: {clipVAnidado} elemento(s) de navegacion solo alcanzables desplazando un scroll local (categorias detras de un scroll)");

                // --- Overlap: navegacion de categorias / buscador / resultados ---
                var regiones = new List<(string n, Rect r)>();
                if (rCats.Count > 0 && !(contenido != null && botonesCat.Any(b => contenido.IsAncestorOf(b))))
                    regiones.Add(("Categorias", new Rect(new Point(rCats.Min(r => r.Left), rCats.Min(r => r.Top)), new Point(rCats.Max(r => r.Right), rCats.Max(r => r.Bottom)))));
                if (buscador != null) regiones.Add(("Buscador", RectCompleto(buscador, window)));
                var ownerRes = contenido == null ? null : svV.FirstOrDefault(sv => sv.IsAncestorOf(contenido));
                if (ownerRes != null) regiones.Add(("Resultados", RectVisible(ownerRes, window)));
                // Correccion D-01/D-04 (28-sep-2026): la cabecera (buscador) comparte ahora la PRIMERA linea del
                // flujo de las categorias principales, asi que el rectangulo que ENVUELVE a todas las pastillas
                // contiene al buscador sin que nada se pise. Buscador frente a categorias se mide pastilla a
                // pastilla (solape real); el resto de pares sigue con las zonas completas.
                if (buscador != null)
                {
                    var rBus = RectCompleto(buscador, window);
                    foreach (var rc in rCats)
                    {
                        double ix = Math.Min(rc.Right, rBus.Right) - Math.Max(rc.Left, rBus.Left), iy = Math.Min(rc.Bottom, rBus.Bottom) - Math.Max(rc.Top, rBus.Top);
                        if (ix > 0.5 && iy > 0.5) { Fallo("OVERLAP", $"{cab}: una pastilla de categoria y el buscador se pisan {ix:0.#}x{iy:0.#}px"); break; }
                    }
                }
                for (int i = 0; i < regiones.Count; i++)
                    for (int j = i + 1; j < regiones.Count; j++)
                    {
                        if ((regiones[i].n, regiones[j].n) is ("Categorias", "Buscador") or ("Buscador", "Categorias")) continue;
                        var a = regiones[i].r; var b = regiones[j].r;
                        if (a.IsEmpty || b.IsEmpty) continue;
                        double ix = Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left), iy = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top);
                        if (ix > 0.5 && iy > 0.5) Fallo("OVERLAP", $"{cab}: '{regiones[i].n}' y '{regiones[j].n}' se pisan {ix:0.#}x{iy:0.#}px");
                    }

                resumen.Add($"{id}: owners V={svV.Count} raices={raicesEnteras}/{raices.Count} | {resTxt}");
                string shot = Path.Combine(outDir, $"lib-{etiqueta}-{id}.png");
                File.WriteAllBytes(shot, CapturarPng(window, window.ActualWidth, window.ActualHeight));
                Console.WriteLine($"LIB {cab} | captura -> {shot}");
            }

            void Contraste_(string superficie, string id, System.Collections.IList raices, System.Windows.Input.ICommand seleccionar, int indiceRaiz)
            {
                FrameworkElement? region = superficie switch
                {
                    "Libreria" => RegionFila1(libSearch),
                    "LibreriaBuffs" => RegionFila1(buffSearch),
                    _ => BuscadorInvestigacion() is { } tb ? RaizPestaña(tb) : null,
                };
                if (region == null) return;
                var raiz = raices.Cast<CategoryNodeViewModel>().ElementAt(indiceRaiz);
                if (!raiz.IsSelected) { seleccionar.Execute(raiz); DoEvents(); DoEvents(); WaitForDispatcher(80); }
                var navCard = window.TryFindResource("NavCardButton") as Style;
                var boton = Descendientes<ButtonBase>(region).FirstOrDefault(b => b.IsVisible && ReferenceEquals(b.DataContext, raiz) && b.Style != navCard);
                string cab = $"[{id}] {superficie} idioma={vm.Settings.Language} categoria seleccionada '{raiz.Name}'";
                if (boton == null) { Fallo("CONTRASTE", $"{cab}: no se encuentra su boton de navegacion visible"); return; }
                // Brush real: Foreground del boton contra el fondo pintado mas cercano (el Border de su plantilla).
                var fondoBorde = Descendientes<Border>(boton).FirstOrDefault(b => b.Background != null && Colores(b.Background).Any(c => c.A > 200));
                var fg = Colores(boton.Foreground).FirstOrDefault();
                var textoReal = Descendientes<TextBlock>(boton).FirstOrDefault(t => t.IsVisible && !string.IsNullOrWhiteSpace(t.Text));
                if (textoReal != null) fg = Colores(textoReal.Foreground).FirstOrDefault();
                double peorBrush = fondoBorde == null ? 0 : Colores(fondoBorde.Background).Min(c => Contraste(fg, c));
                var (ratio, fondo, texto) = ContrastePixeles(boton);
                Console.WriteLine($"LIB CONTRASTE {cab}: pinceles texto={fg} fondo={(fondoBorde == null ? "?" : string.Join("/", Colores(fondoBorde.Background)))} -> peor {peorBrush:0.00}:1 | pixeles reales fondo={fondo} texto={texto} -> {ratio:0.00}:1");
                if (Math.Min(peorBrush <= 0 ? ratio : peorBrush, ratio) < 4.5)
                    Fallo("CONTRASTE", $"{cab}: contraste {Math.Min(peorBrush <= 0 ? ratio : peorBrush, ratio):0.00}:1 < 4,5:1");
                string shot = Path.Combine(outDir, $"lib-{etiqueta}-{id}-contraste.png");
                File.WriteAllBytes(shot, CapturarPng(boton, boton.ActualWidth, boton.ActualHeight));
                seleccionar.Execute(raiz); DoEvents(); // deseleccionar (mismo comando alterna)
            }

            // ---------------------------------------------------------------------------------
            // Panel Editar (s7): sin scroll propio ni recortes.
            // ---------------------------------------------------------------------------------
            // Por NOMBRE (x:Name="EditarTarjeta", el mismo en el codigo viejo y en el nuevo): desde la correccion
            // D-01/D-05 su Style es EditarTarjetaComposicion (BasedOn SidePanelCard) y la busqueda por Style
            // devolvia null en silencio ("Editar no encontrado" sin FALLO).
            FrameworkElement? PanelEditar() => objetosView.FindName("EditarTarjeta") as FrameworkElement is { IsVisible: true } fe ? fe : null;
            (double scr, double clip, string txt, double holgura) MedirEditar(string id, bool silencioso = false)
            {
                DoEvents(); DoEvents(); WaitForDispatcher(60);
                var editar = PanelEditar();
                if (editar == null) { Fallo("EDITAR-CLIP", $"[{id}] no se encuentra el panel Editar visible (x:Name EditarTarjeta)"); return (0, 0, "Editar no encontrado", 0); }
                var svs = Descendientes<ScrollViewer>(editar).Where(sv => sv.TemplatedParent is not TextBoxBase).ToList();
                double scr = svs.Where(sv => sv.IsVisible).Select(sv => sv.ScrollableHeight).DefaultIfEmpty(0).Max();
                var re = RectCompleto(editar, window);
                double peorClip = 0; string peorQue = "";
                foreach (var fe in Descendientes<FrameworkElement>(editar).Where(f => f.IsVisible && f.ActualHeight > 0 && (f is TextBlock or ButtonBase or TextBox)))
                {
                    var rc = RectCompleto(fe, window); var rv = RectVisible(fe, window);
                    double falta = rv.IsEmpty ? rc.Height : rc.Height - rv.Height;
                    if (falta > peorClip) { peorClip = falta; peorQue = Describir(fe); }
                }
                // Holgura: del borde inferior util del panel (sin su Padding/Border) al fondo del ultimo elemento
                // visible - negativa = el contenido no cabe (lo recortaria el layout).
                double fondo = Descendientes<FrameworkElement>(editar).Where(f => f.IsVisible && f.ActualHeight > 0 && f is TextBlock or ButtonBase or TextBox or Image)
                    .Select(f => RectCompleto(f, window).Bottom).DefaultIfEmpty(re.Top).Max();
                double holgura = re.Bottom - ((editar as Border)?.Padding.Bottom ?? 0) - ((editar as Border)?.BorderThickness.Bottom ?? 0) - fondo;
                // Correccion D-07: con Editar alto SEGUN SU CONTENIDO (barra lateral, VerticalAlignment=Top) la holgura
                // interna es siempre la misma (su Padding); lo que puede agotarse es el sitio hasta el fondo de su rejilla
                // (filas de contenido + Libreria), y esa es la holgura que cuenta: el peor objeto/combinacion es el Editar
                // mas alto (negativa = se sale de su rejilla).
                double margenFondo = double.NaN;
                if (editar.VerticalAlignment == VerticalAlignment.Top && VisualTreeHelper.GetParent(editar) is FrameworkElement rejillaEditar)
                {
                    margenFondo = RectCompleto(rejillaEditar, window).Bottom - re.Bottom;
                    holgura = margenFondo;
                }
                string txt = $"Editar {re.Width:0.#}x{re.Height:0.#} en y={re.Top:0.#} | holgura inferior={holgura:0.#}px{(double.IsNaN(margenFondo) ? "" : $" (margen hasta el fondo de su rejilla {margenFondo:0.#}px)")} | ScrollViewer dentro={svs.Count} (visibles {svs.Count(s => s.IsVisible)}) scr max={scr:0.#}px | recorte max={peorClip:0.#}px{(peorClip > 0.5 ? $" ({peorQue})" : "")} | slot={(vm.ItemEdit.Slot == null ? "ninguno" : $"{vm.ItemEdit.Slot.ContainerName}#{vm.ItemEdit.Slot.SlotIndex} '{vm.ItemEdit.Slot.DisplayName}'")} metas={vm.ItemEdit.Metas.Count} grupos={vm.ItemEdit.Groups.Count} prefijos={vm.ItemEdit.Prefixes.Count}";
                if (!silencioso) Console.WriteLine($"LIB EDITAR [{id}] {window.ActualWidth:0}x{window.ActualHeight:0} libPlegada={vm.IsLibraryCollapsed} idioma={vm.Settings.Language} | {txt}");
                if (holgura < -0.5 && peorClip <= 0.5) peorClip = -holgura;
                return (scr, peorClip, txt, holgura);
            }
            void ExigirEditar(string id, (double scr, double clip, string txt, double holgura) m)
            {
                if (m.scr > 0.5) Fallo("EDITAR-SCROLL", $"[{id}] {window.ActualWidth:0}x{window.ActualHeight:0} libPlegada={vm.IsLibraryCollapsed} idioma={vm.Settings.Language}: el panel Editar desplaza {m.scr:0.#}px ({m.txt})");
                if (m.clip > 0.5) Fallo("EDITAR-CLIP", $"[{id}] {window.ActualWidth:0}x{window.ActualHeight:0} libPlegada={vm.IsLibraryCollapsed} idioma={vm.Settings.Language}: el panel Editar recorta {m.clip:0.#}px de contenido ({m.txt})");
            }

            // ---------------------------------------------------------------------------------
            // 0) PRIMERA entrada con el ViewModel recien creado (s24/H-C1): Libreria de objetos a 1080x700
            //    desplegada, antes de cualquier otra medida de esta superficie en este proceso.
            // ---------------------------------------------------------------------------------
            window.WindowState = WindowState.Normal; FijarTamaño(window, 1080, 700);
            vm.IsLibraryCollapsed = false; vm.IsBuffLibraryCollapsed = false;
            IrA(0, 1);
            MedirCatalogo("Libreria", "lib-primera-entrada-1080x700", vm.Library.Results, vm.Library.RootCategories, vm.Library.ClearCategoryCommand);
            IrA(1);
            MedirCatalogo("LibreriaBuffs", "buff-primera-entrada-1080x700", vm.BuffLibrary.Results, vm.BuffLibrary.RootCategories, vm.BuffLibrary.ClearCategoryCommand);
            IrA(2);
            MedirCatalogo("Investigacion", "inv-primera-entrada-1080x700", vm.Research.Results, vm.Research.RootCategories, vm.Research.ClearCategoryCommand);

            // ---------------------------------------------------------------------------------
            // 1) Tamaños x idioma x estado (sin categoria = tarjetas raiz; categoria grande; busqueda amplia).
            // ---------------------------------------------------------------------------------
            var tamaños = new (string id, double w, double h)[]
            {
                ("1080x700", 1080, 700), ("1366x768", 1366, 768), ("1520x860", 1520, 860), ("1920x1080", 1920, 1080), ("2560x1440", 2560, 1440),
            };
            CategoryNodeViewModel? RaizMasGrande(System.Collections.IList raices) => raices.Cast<CategoryNodeViewModel>().OrderByDescending(r => r.ItemCount).FirstOrDefault();
            void Estados(string idioma, IEnumerable<(string id, double w, double h)> lista)
            {
                vm.Settings.Language = idioma; DoEvents(); DoEvents();
                foreach (var (tid, w, h) in lista)
                {
                    window.WindowState = WindowState.Normal; FijarTamaño(window, w, h);
                    vm.IsLibraryCollapsed = false; vm.IsBuffLibraryCollapsed = false;
                    string suf = $"{tid}-{idioma.ToUpperInvariant()}";

                    IrA(0, 1);
                    vm.Library.ClearCategoryCommand.Execute(null); vm.Library.SearchText = ""; WaitForDispatcher(250);
                    MedirCatalogo("Libreria", $"lib-vertodo-{suf}", vm.Library.Results, vm.Library.RootCategories, vm.Library.ClearCategoryCommand);
                    var grande = RaizMasGrande(vm.Library.RootCategories);
                    if (grande != null) { vm.Library.SelectCategoryCommand.Execute(grande); DoEvents(); }
                    MedirCatalogo("Libreria", $"lib-categoria-grande-{suf}", vm.Library.Results, vm.Library.RootCategories, vm.Library.ClearCategoryCommand);
                    vm.Library.ClearCategoryCommand.Execute(null);
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    vm.Library.SearchText = "#1-99999999"; WaitForDispatcher(300); DoEvents(); DoEvents();
                    sw.Stop();
                    Console.WriteLine($"LIB BUSQUEDA-AMPLIA Libreria '#1-99999999' {suf}: {vm.Library.Results.Count} resultados en pantalla ({vm.Library.ResultsSummary}) en {sw.ElapsedMilliseconds}ms (incluye el debounce de 180ms)");
                    MedirCatalogo("Libreria", $"lib-busqueda-amplia-{suf}", vm.Library.Results, vm.Library.RootCategories, vm.Library.ClearCategoryCommand);
                    vm.Library.SearchText = ""; WaitForDispatcher(250);

                    IrA(1);
                    vm.BuffLibrary.ClearCategoryCommand.Execute(null); vm.BuffLibrary.SearchText = ""; WaitForDispatcher(250);
                    MedirCatalogo("LibreriaBuffs", $"buff-vertodo-{suf}", vm.BuffLibrary.Results, vm.BuffLibrary.RootCategories, vm.BuffLibrary.ClearCategoryCommand);
                    vm.BuffLibrary.SearchText = "#1-99999999"; WaitForDispatcher(300); DoEvents();
                    MedirCatalogo("LibreriaBuffs", $"buff-busqueda-amplia-{suf}", vm.BuffLibrary.Results, vm.BuffLibrary.RootCategories, vm.BuffLibrary.ClearCategoryCommand);
                    vm.BuffLibrary.SearchText = ""; WaitForDispatcher(250);

                    IrA(2);
                    vm.Research.ClearCategoryCommand.Execute(null); vm.Research.SearchText = ""; WaitForDispatcher(250);
                    MedirCatalogo("Investigacion", $"inv-vertodo-{suf}", vm.Research.Results, vm.Research.RootCategories, vm.Research.ClearCategoryCommand);
                    var grandeR = RaizMasGrande(vm.Research.RootCategories);
                    if (grandeR != null) { vm.Research.SelectCategoryCommand.Execute(grandeR); DoEvents(); }
                    MedirCatalogo("Investigacion", $"inv-categoria-grande-{suf}", vm.Research.Results, vm.Research.RootCategories, vm.Research.ClearCategoryCommand);
                    vm.Research.ClearCategoryCommand.Execute(null);
                }
            }
            // LIBRARY_RESPONSIVE_RAPIDO=1: iteracion rapida durante una correccion (sin el barrido de tamaños x
            // idioma ni el de Editar, ~8 de sus ~10 minutos). El cierre de cualquier ronda se hace SIN esta variable.
            bool rapido = Environment.GetEnvironmentVariable("LIBRARY_RESPONSIVE_RAPIDO") == "1";
            if (rapido) Console.WriteLine("LIB MODO RAPIDO: se omiten el barrido de tamaños x idioma y el del panel Editar (no vale para cerrar una ronda)");
            if (!rapido) Estados("es", tamaños);
            if (!rapido) Estados("en", tamaños.Where(t => t.id is "1080x700" or "1366x768" or "1920x1080"));
            vm.Settings.Language = "es"; DoEvents(); DoEvents();

            // Modo compacto (s15: preferencia del usuario, compatible con el responsive) en el minimo.
            vm.Settings.IsCompactMode = true; DoEvents(); DoEvents();
            FijarTamaño(window, 1080, 700); vm.IsLibraryCollapsed = false; IrA(0, 1);
            vm.Library.SearchText = "#1-99999999"; WaitForDispatcher(300);
            MedirCatalogo("Libreria", "lib-busqueda-amplia-1080x700-compacto", vm.Library.Results, vm.Library.RootCategories, vm.Library.ClearCategoryCommand);
            vm.Library.SearchText = ""; WaitForDispatcher(250);
            vm.Settings.IsCompactMode = false; DoEvents(); DoEvents();

            // ---------------------------------------------------------------------------------
            // 1b) Correcciones del revisor visual de la FASE D (D-01..D-07, 28-sep-2026). Solo nombres
            //     (FindName) y reflexion: el mismo fichero tiene que compilar y medir contra fae1461a.
            // ---------------------------------------------------------------------------------
            MedirCorreccionesRevisorFaseD(window, vm, objetosView, Fallo, outDir, etiqueta, IrA, EnteroEnAmbosEjes);

            // ---------------------------------------------------------------------------------
            // 2) Contraste de la categoria seleccionada (hallazgo Low del revisor de la FASE C, visto con
            //    "Decorative" en EN) en las 3 superficies.
            // ---------------------------------------------------------------------------------
            FijarTamaño(window, 1366, 768); vm.IsLibraryCollapsed = false; vm.IsBuffLibraryCollapsed = false;
            foreach (var idioma in new[] { "en", "es" })
            {
                vm.Settings.Language = idioma; DoEvents(); DoEvents();
                IrA(0, 1); vm.Library.ClearCategoryCommand.Execute(null);
                int iDeco = vm.Library.RootCategories.Select((r, i) => (r, i)).FirstOrDefault(t => t.r.FullPath.Contains("Decorative", StringComparison.OrdinalIgnoreCase)).i;
                Contraste_("Libreria", $"contraste-lib-{idioma}", vm.Library.RootCategories, vm.Library.SelectCategoryCommand, iDeco);
                IrA(1); vm.BuffLibrary.ClearCategoryCommand.Execute(null);
                Contraste_("LibreriaBuffs", $"contraste-buff-{idioma}", vm.BuffLibrary.RootCategories, vm.BuffLibrary.SelectCategoryCommand, 0);
                IrA(2); vm.Research.ClearCategoryCommand.Execute(null);
                Contraste_("Investigacion", $"contraste-inv-{idioma}", vm.Research.RootCategories, vm.Research.SelectCategoryCommand, iDeco);
            }
            vm.Settings.Language = "es"; DoEvents(); DoEvents();

            // ---------------------------------------------------------------------------------
            // 3) Panel Editar (s7, permiso aa5f7395 + known-diff 570a3c9d): Cenit, el objeto de prefijos
            //    mas largo encontrado (barrido del catalogo entero por el propio ItemEditViewModel) y un
            //    slot de armadura.
            // ---------------------------------------------------------------------------------
            var slotPrueba = vm.InventoryContainer.Slots[9];
            int idOriginal = slotPrueba.ItemId;
            const int Cenit = 4956;
            // Barrido: con el panel Editar FUERA del arbol visual (pestaña Buffs) para que las 3 colecciones
            // no regeneren botones en cada paso. Estimacion del alto con FormattedText real (misma fuente y
            // tamaños que ItemEditTemplate) sobre el ancho real del panel.
            IrA(1);
            var tf = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            double AnchoTexto(string s, double tam) => new FormattedText(s ?? "", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, tf, tam, Brushes.White, 1.0).WidthIncludingTrailingWhitespace;
            double AltoWrap(IEnumerable<string> etiquetas, double tam, double padH, double alto, double ancho)
            {
                double x = 0; int filas = 0;
                foreach (var e in etiquetas)
                {
                    double w = AnchoTexto(e, tam) + 2 * padH + 5;
                    if (filas == 0 || x + w > ancho) { filas++; x = 0; }
                    x += w;
                }
                return filas * alto;
            }
            var todos = rapido ? new List<LibraryItemViewModel>() : (typeof(LibraryViewModel).GetField("_all", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(vm.Library) as System.Collections.IEnumerable)?.Cast<LibraryItemViewModel>().ToList() ?? [];
            var sw2 = System.Diagnostics.Stopwatch.StartNew();
            var puntuaciones = new List<(int id, string nombre, int meta, int grupo, double alto)>();
            foreach (var item in todos)
            {
                slotPrueba.PlaceItem(item.Id); vm.SelectSlot(slotPrueba);
                var ie = vm.ItemEdit;
                if (!ie.CanHavePrefix) continue;
                double mejor = 0; int mMejor = -1, gMejor = -1;
                var metas = ie.Metas.ToList();
                for (int m = 0; m < metas.Count; m++)
                {
                    ie.SelectMetaCommand.Execute(metas[m]);
                    var grupos = ie.Groups.ToList();
                    for (int g = 0; g < grupos.Count; g++)
                    {
                        ie.SelectGroupCommand.Execute(grupos[g]);
                        double a = AltoWrap(ie.Metas.Select(x => x.Label), 11, 9, 27, 264)
                                 + AltoWrap(ie.Groups.Select(x => x.Label), 10.5, 9, 26, 264)
                                 + AltoWrap(ie.Prefixes.Select(x => x.DisplayName), 11, 7, 27, 264)
                                 + Math.Ceiling(AnchoTexto(item.DisplayName, 12.5) / 220) * 17;
                        if (a > mejor) { mejor = a; mMejor = m; gMejor = g; }
                    }
                }
                puntuaciones.Add((item.Id, item.DisplayName, mMejor, gMejor, mejor));
            }
            sw2.Stop();
            var peores = puntuaciones.OrderByDescending(p => p.alto).Take(3).ToList();
            Console.WriteLine($"LIB EDITAR BARRIDO: {todos.Count} objetos del catalogo, {puntuaciones.Count} admiten prefijo, {sw2.ElapsedMilliseconds}ms. Peores (alto estimado de metas+grupos+prefijos+nombre): " +
                              string.Join(" | ", peores.Select(p => $"#{p.id} '{p.nombre}' meta={p.meta} grupo={p.grupo} ~{p.alto:0}px")));
            var cenitP = puntuaciones.FirstOrDefault(p => p.id == Cenit);
            Console.WriteLine($"LIB EDITAR BARRIDO: Cenit #{Cenit} '{cenitP.nombre}' peor combinacion meta={cenitP.meta} grupo={cenitP.grupo} ~{cenitP.alto:0}px");

            // Combinaciones reales (todas las metas x grupos) de un objeto, en el panel real.
            (double scr, double clip, string txt, double holgura) PeorEditar(int idObjeto, string id)
            {
                slotPrueba.PlaceItem(idObjeto); vm.SelectSlot(slotPrueba); DoEvents();
                var ie = vm.ItemEdit;
                (double scr, double clip, string txt, double holgura) peor = MedirEditar(id + "-inicial", silencioso: true);
                string combo = "inicial";
                var metas = ie.Metas.ToList();
                for (int m = 0; m < metas.Count; m++)
                {
                    ie.SelectMetaCommand.Execute(metas[m]); DoEvents();
                    var grupos = ie.Groups.ToList();
                    for (int g = 0; g < grupos.Count; g++)
                    {
                        ie.SelectGroupCommand.Execute(grupos[g]); DoEvents();
                        var r = MedirEditar(id, silencioso: true);
                        if (r.scr + r.clip > peor.scr + peor.clip || (r.scr + r.clip <= 0.5 && peor.scr + peor.clip <= 0.5 && r.holgura < peor.holgura))
                        { peor = r; combo = $"meta {m} '{metas[m].Label}' grupo {g} '{grupos[g].Label}'"; }
                    }
                }
                Console.WriteLine($"LIB EDITAR [{id}] {window.ActualWidth:0}x{window.ActualHeight:0} libPlegada={vm.IsLibraryCollapsed} idioma={vm.Settings.Language} | PEOR combinacion ({combo}): {peor.txt}");
                return peor;
            }

            IrA(0, 1);
            var casosEditar = rapido ? [] : new (string id, double w, double h, bool plegada)[]
            {
                ("1080x700-libplegada", 1080, 700, true), ("1080x700-libdesplegada", 1080, 700, false),
                ("1366x768-libplegada", 1366, 768, true), ("1366x768-libdesplegada", 1366, 768, false),
                // Normal en su alto minimo (Editar en la fila de contenido con el desplegable de prefijo, como Compacto).
                ("1320x700-libdesplegada", 1320, 700, false),
                ("1520x860", 1520, 860, false), ("1920x1080", 1920, 1080, false), ("2560x1440", 2560, 1440, false),
            };
            foreach (var idioma in new[] { "es", "en" })
            {
                vm.Settings.Language = idioma; DoEvents(); DoEvents();
                foreach (var (cid, w, h, plegada) in casosEditar)
                {
                    if (idioma == "en" && !cid.StartsWith("1080") && !cid.StartsWith("1366") && !cid.StartsWith("1320")) continue;
                    window.WindowState = WindowState.Normal; FijarTamaño(window, w, h); vm.IsLibraryCollapsed = plegada; IrA(0, 1);
                    var rc = PeorEditar(Cenit, $"cenit-{cid}-{idioma}"); ExigirEditar($"cenit-{cid}-{idioma}", rc);
                    foreach (var p in peores)
                    {
                        var rp = PeorEditar(p.id, $"prefijos#{p.id}-{cid}-{idioma}"); ExigirEditar($"prefijos#{p.id}-{cid}-{idioma}", rp);
                    }
                    if (cid == "1080x700-libdesplegada")
                    {
                        slotPrueba.PlaceItem(peores.Count > 0 ? peores[0].id : Cenit); vm.SelectSlot(slotPrueba); DoEvents(); WaitForDispatcher(80);
                        File.WriteAllBytes(Path.Combine(outDir, $"lib-{etiqueta}-editar-prefijos-{cid}-{idioma}.png"), CapturarPng(window, window.ActualWidth, window.ActualHeight));
                        slotPrueba.PlaceItem(Cenit); vm.SelectSlot(slotPrueba); DoEvents(); WaitForDispatcher(80);
                        File.WriteAllBytes(Path.Combine(outDir, $"lib-{etiqueta}-editar-cenit-{cid}-{idioma}.png"), CapturarPng(window, window.ActualWidth, window.ActualHeight));
                    }
                    // Permiso aa5f7395: slot de armadura (Cabeza) de Equipamiento.
                    IrA(0, 0);
                    var cabeza = vm.EquipmentGroup!.Current.Slots.FirstOrDefault(s => !s.IsEmpty) ?? vm.EquipmentGroup.Current.Slots[0];
                    vm.SelectSlot(cabeza); DoEvents();
                    var ra = MedirEditar($"armadura-{cid}-{idioma}"); ExigirEditar($"armadura-{cid}-{idioma}", ra);
                    IrA(0, 1);
                }
            }
            vm.Settings.Language = "es"; DoEvents(); DoEvents();
            slotPrueba.PlaceItem(idOriginal); vm.IsDirty = false;

            // ---------------------------------------------------------------------------------
            // 4) Banda vacia entre Equipamiento y la Libreria en ventana grande (anotada en la FASE B, s17).
            // ---------------------------------------------------------------------------------
            {
                var pagEquip = objetosView.FindName("ObjetosPaginaEquipamiento") as ScrollViewer;
                var filaLib = objetosView.FindName("FilaLibreria") as RowDefinition;
                foreach (var (bid, w, h) in new[] { ("1366x768", 1366.0, 768.0), ("1920x1080", 1920.0, 1080.0), ("2560x1440", 2560.0, 1440.0) })
                {
                    FijarTamaño(window, w, h); vm.IsLibraryCollapsed = false; IrA(0, 0); WaitForDispatcher(100);
                    if (pagEquip == null) break;
                    double banda = pagEquip.ScrollableHeight > 0.5 ? 0 : pagEquip.ViewportHeight - pagEquip.ExtentHeight;
                    Console.WriteLine($"LIB BANDA-EQUIP {bid}: pagina vp={pagEquip.ViewportHeight:0.#} ext={pagEquip.ExtentHeight:0.#} scr={pagEquip.ScrollableHeight:0.#} -> banda vacia bajo el contenido {banda:0.#}px | fila Libreria {filaLib?.ActualHeight:0.#}px (MaxHeight {filaLib?.MaxHeight:0.#})");
                    if (banda > 40) Fallo("BANDA-EQUIP", $"{bid}: {banda:0.#}px vacios entre el contenido de Equipamiento y la Libreria (s17: ese alto deberia pasar a la Libreria)");
                    File.WriteAllBytes(Path.Combine(outDir, $"lib-{etiqueta}-banda-equip-{bid}.png"), CapturarPng(window, window.ActualWidth, window.ActualHeight));
                }
            }

            // ---------------------------------------------------------------------------------
            // 5) R2-L1 (Low de la FASE C): cada LINEA de pildoras/acciones de Almacenes centrada en el eje de
            //    la rejilla.
            // ---------------------------------------------------------------------------------
            {
                var pagAlm = objetosView.FindName("ObjetosPaginaAlmacenes") as ScrollViewer;
                foreach (var (aid, w, h) in new[] { ("1080x700", 1080.0, 700.0), ("1366x768", 1366.0, 768.0), ("1920x1080", 1920.0, 1080.0) })
                {
                    FijarTamaño(window, w, h); vm.IsLibraryCollapsed = false; IrA(0, 2); WaitForDispatcher(80);
                    if (pagAlm == null) break;
                    var rejilla = Descendientes<SlotGridPanel>(pagAlm).FirstOrDefault(p => p.IsVisible);
                    var celdasAlm = rejilla == null ? [] : rejilla.Children.OfType<FrameworkElement>().Select(c => RectCompleto(c, window)).ToList();
                    if (celdasAlm.Count == 0) continue;
                    double ejeRejilla = (celdasAlm.Min(r => r.Left) + celdasAlm.Max(r => r.Right)) / 2;
                    var botones = Descendientes<ButtonBase>(pagAlm).Where(b => b.IsVisible && b.ActualWidth > 0 && !rejilla!.IsAncestorOf(b)
                        && !Descendientes<ScrollBar>(pagAlm).Any(sb => sb.IsAncestorOf(b))).Select(b => RectCompleto(b, window)).ToList();
                    var lineas = botones.GroupBy(r => Math.Round(r.Top / 4)).Select(g => (top: g.Min(r => r.Top), izq: g.Min(r => r.Left), der: g.Max(r => r.Right), n: g.Count())).OrderBy(l => l.top).ToList();
                    foreach (var l in lineas)
                    {
                        double desv = (l.izq + l.der) / 2 - ejeRejilla;
                        Console.WriteLine($"LIB ALM-EJE {aid}: linea y={l.top:0.#} con {l.n} boton(es) de {l.izq:0.#} a {l.der:0.#} -> centro desviado {desv:0.#}px del eje de la rejilla ({ejeRejilla:0.#})");
                        if (Math.Abs(desv) > 3) Fallo("ALM-EJE", $"{aid}: una linea de pildoras/acciones de Almacenes queda {desv:0.#}px fuera del eje de la rejilla");
                    }
                    File.WriteAllBytes(Path.Combine(outDir, $"lib-{etiqueta}-alm-eje-{aid}.png"), CapturarPng(window, window.ActualWidth, window.ActualHeight));
                }
            }

            // ---------------------------------------------------------------------------------
            // 6) Redimensionado en caliente (s24): grande -> normal -> minimo -> grande, sin perder
            //    categoria (tambien de segundo nivel), busqueda, slot de Editar, pagina ni ViewModels.
            // ---------------------------------------------------------------------------------
            {
                FijarTamaño(window, 1920, 1080); vm.IsLibraryCollapsed = false; vm.IsBuffLibraryCollapsed = false; IrA(0, 1);
                var libAntes = vm.Library; var buffAntes = vm.BuffLibrary; var resAntes = vm.Research;
                var raizL = vm.Library.RootCategories.First(r => r.Children.Count > 0);
                vm.Library.SelectCategoryCommand.Execute(raizL); var hijaL = raizL.Children[0]; vm.Library.SelectCategoryCommand.Execute(hijaL);
                var raizB = vm.BuffLibrary.RootCategories[0]; vm.BuffLibrary.SelectCategoryCommand.Execute(raizB);
                var raizR = vm.Research.RootCategories.Last(); vm.Research.SelectCategoryCommand.Execute(raizR);
                vm.Research.SearchText = "de"; WaitForDispatcher(250);
                slotPrueba.PlaceItem(Cenit); vm.SelectSlot(slotPrueba); DoEvents();
                foreach (var (paso, w, h) in new[] { ("grande", 1920.0, 1080.0), ("normal", 1366.0, 768.0), ("minimo", 1080.0, 700.0), ("grande-vuelta", 1920.0, 1080.0) })
                {
                    FijarTamaño(window, w, h); IrA(0, 1);
                    bool ok = ReferenceEquals(vm.Library, libAntes) && ReferenceEquals(vm.BuffLibrary, buffAntes) && ReferenceEquals(vm.Research, resAntes)
                              && ReferenceEquals(vm.Library.SelectedCategory, hijaL) && ReferenceEquals(vm.BuffLibrary.SelectedCategory, raizB)
                              && ReferenceEquals(vm.Research.SelectedCategory, raizR) && vm.Research.SearchText == "de"
                              && ReferenceEquals(vm.ItemEdit.Slot, slotPrueba) && vm.ObjetosSubTabIndex == 1 && !vm.IsLibraryCollapsed;
                    Console.WriteLine($"LIB RESIZE {paso} {w:0}x{h:0}: mismosVM={ReferenceEquals(vm.Library, libAntes) && ReferenceEquals(vm.BuffLibrary, buffAntes) && ReferenceEquals(vm.Research, resAntes)} categoriaLib='{vm.Library.SelectedCategory?.FullPath}' categoriaBuff='{vm.BuffLibrary.SelectedCategory?.FullPath}' categoriaInv='{vm.Research.SelectedCategory?.FullPath}' busquedaInv='{vm.Research.SearchText}' slotEditar={(ReferenceEquals(vm.ItemEdit.Slot, slotPrueba) ? "igual" : "PERDIDO")} pagina={vm.ObjetosSubTabIndex}");
                    if (!ok) Fallo("RESIZE", $"en el paso '{paso}' ({w:0}x{h:0}) se perdio estado");
                    MedirCatalogo("Libreria", $"lib-resize-{paso}", vm.Library.Results, vm.Library.RootCategories, vm.Library.ClearCategoryCommand);
                }
                vm.Library.ClearCategoryCommand.Execute(null); vm.BuffLibrary.ClearCategoryCommand.Execute(null); vm.Research.ClearCategoryCommand.Execute(null);
                vm.Research.SearchText = ""; WaitForDispatcher(250);
                slotPrueba.PlaceItem(idOriginal); vm.IsDirty = false;
            }

            // ---------------------------------------------------------------------------------
            // 7) Negative acceptance del mecanismo VIEJO (s26/s28).
            // ---------------------------------------------------------------------------------
            {
                FijarTamaño(window, 1366, 768); vm.IsLibraryCollapsed = false; vm.IsBuffLibraryCollapsed = false;
                bool propTope = typeof(MainViewModel).GetProperty("LibraryRowMaxHeight") != null;
                Console.WriteLine($"LIB VIEJO: MainViewModel.LibraryRowMaxHeight existe = {propTope} (esperado False)");
                if (propTope) Fallo("VIEJO", "el tope LibraryRowMaxHeight (MaxHeight 460/640 de la fila de la Libreria) sigue existiendo");
                var filaLib = objetosView.FindName("FilaLibreria") as RowDefinition;
                bool bindTope = filaLib != null && BindingOperations.GetBindingExpression(filaLib, RowDefinition.MaxHeightProperty) != null;
                Console.WriteLine($"LIB VIEJO: FilaLibreria con MaxHeight enlazado = {bindTope} (esperado False)");
                if (bindTope) Fallo("VIEJO", "la fila de la Libreria de objetos sigue con MaxHeight enlazado");
                var plantillasArbol = new[] { "CategoryNodeTemplate", "ObjetosCategoryNodeTemplate" }.Where(k => window.TryFindResource(k) != null || objetosView.TryFindResource(k) != null).ToList();
                Console.WriteLine($"LIB VIEJO: plantillas del arbol lateral de categorias que siguen existiendo = [{string.Join(", ", plantillasArbol)}] (esperado ninguna)");
                if (plantillasArbol.Count > 0) Fallo("VIEJO", $"siguen existiendo las plantillas del arbol lateral con scroll propio: {string.Join(", ", plantillasArbol)}");
                foreach (var (sup, pest, desde) in new (string, int, Func<FrameworkElement?>)[]
                         { ("Libreria", 0, () => RegionFila1(libSearch)), ("LibreriaBuffs", 1, () => RegionFila1(buffSearch)), ("Investigacion", 2, () => BuscadorInvestigacion() is { } t ? RaizPestaña(t) : null) })
                {
                    IrA(pest, 1);
                    var reg = desde();
                    if (reg == null) continue;
                    int col210 = Descendientes<Grid>(reg).Sum(g => g.ColumnDefinitions.Count(c => c.Width.IsAbsolute && Math.Abs(c.Width.Value - 210) < 0.5));
                    int svTotal = Descendientes<ScrollViewer>(reg).Count(sv => sv.TemplatedParent is not TextBoxBase);
                    Console.WriteLine($"LIB VIEJO: {sup}: columnas fijas de 210px = {col210} (esperado 0); ScrollViewer en la superficie (visibles o no) = {svTotal} (esperado 1, el de resultados)");
                    if (col210 > 0) Fallo("VIEJO", $"{sup}: sigue la columna fija de categorias de 210px");
                    if (svTotal > 1) Fallo("VIEJO", $"{sup}: {svTotal} ScrollViewer en la superficie (arbol/tarjetas/resultados con scroll propio cada uno)");
                }
                IrA(0, 1);
                slotPrueba.PlaceItem(Cenit); vm.SelectSlot(slotPrueba); DoEvents();
                var editar = PanelEditar();
                int svEditar = editar == null ? -1 : Descendientes<ScrollViewer>(editar).Count(sv => sv.TemplatedParent is not TextBoxBase);
                Console.WriteLine($"LIB VIEJO: ScrollViewer dentro del panel Editar = {svEditar} (esperado 0; antes 2: el de seguridad L-d y el de la lista de prefijos)");
                if (svEditar > 0) Fallo("VIEJO", $"el panel Editar sigue con {svEditar} ScrollViewer propio(s)");
                slotPrueba.PlaceItem(idOriginal); vm.IsDirty = false;
            }

            Console.WriteLine("LIB RESUMEN:");
            foreach (var r in resumen) Console.WriteLine("  " + r);

            vm.Settings.Language = idiomaOriginal;
            vm.Settings.IsCompactMode = compactoOriginal;
            vm.IsLibraryCollapsed = libreriaOriginal; vm.IsBuffLibraryCollapsed = buffLibOriginal;
            vm.IsDirty = false;
            IrA(0, 0);
            FijarTamaño(window, 1180, 860);
            DoEvents();
        }
        catch (Exception ex) { fallos++; Console.WriteLine("LIB-RESPONSIVE-EXCEPTION: " + ex); }
        Console.WriteLine($"LIBRARY_RESPONSIVE_SOLO: {fallos} fallo(s)");
    }
}
