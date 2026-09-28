// CANARIO REAL - FASE C del responsive global (PDF "Arreglo familia keep", bloque 2 "TERRAKEEP -
// RESPONSIVE GLOBAL, PAGINACION Y SCROLL COMO ULTIMO RECURSO", secciones 3, 8, 13-17, 19, 22-26,
// 28-29 y 34). 28-sep-2026, aplicador-fix-responsive-faseC.
//
// INVALM_RESPONSIVE_SOLO=1 mide "Personaje > Objetos > Inventario" y "> Almacenes" (Banco, Caja fuerte,
// Forja del Defensor y Camara del vacio) con geometria REAL (RectCompleto vs ZonaVisible/RectVisible,
// el mismo par de AR-LAY/NAV123/EQUIP_RESPONSIVE) con las colecciones LLENAS (s23: 50/50 y 40/40), en
// varios tamaños, ES/EN, modo compacto, con el banner "Deshacer" visible y con redimensionado en
// caliente, y falla (lineas "FALLO: INVALM_RESPONSIVE_SOLO-<codigo>") si aparece:
//   - SCROLL-ANIDADO  (s26 A) un ScrollViewer vertical dentro del scroll owner de la pagina
//                         (ObjetosPaginaInventario / ObjetosPaginaAlmacenes).
//   - CLIP-H / CLIP-V (s26 E/F) contenido recortado sin escape, o solo alcanzable por un scroll anidado.
//   - CELDA           (s26 E) celdas por debajo de su MinCell.
//   - OVERLAP         rejilla / cabecera / panel Editar que se pisan.
//   - HSCROLL         (s26 G) Disabled con contenido mas ancho que el viewport, o barra horizontal.
//   - ESTRUCTURAL     (s3 A / s26 F) un boton, pildora de almacen o la navegacion de pagina no se ve
//                         ENTERO sin desplazar la pagina.
//   - NAV-ETIQUETA    la navegacion Equipamiento/Inventario/Almacenes sin etiqueta propia (solo "1/2/3",
//                         confundible con los 1/2/3 de Loadout - hallazgo del revisor de la FASE B).
//   - CLIC-SCROLL     pulsar un slot visible mueve el scroll de la pagina (el salto de ~37px visto por el
//                         canario de arrastre, 020fde86) o deja el foco de teclado fuera del slot.
//   (EDITAR-SCROLL se INFORMA como AVISO, no falla: el permiso s13 aa5f7395 del panel Editar esta
//    pendiente de decision del usuario y el panel es s7, fuera de esta fase.)
//   - ESTRELLA        el boton de favorito del panel Editar (N-02) no se ve entero con un slot de Inventario.
//   - RESIZE          (s24) perdida de estado al redimensionar grande->normal->minimo->grande.
//   - VIEJO           (s26/s28 negative acceptance) el scroll local de ContainerCompactTemplate sigue
//                         existiendo por debajo del modelo nuevo.
//
// Solo abre una COPIA del personaje (carpeta del arnes, AislamientoPartidasReales.cs), nunca guarda.
// Capturas en INVALM_RESPONSIVE_EVIDENCIA (si se define) o en <bin>/keepqa-evidencia/responsive-faseC.
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Terrakeep.App;
using Terrakeep.App.Controls;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RectMonitor { public int left, top, right, bottom; }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct InfoMonitor { public int cbSize; public RectMonitor rcMonitor, rcWork; public uint dwFlags; }
    private delegate bool EnumMonitorProc(IntPtr hMonitor, IntPtr hdc, IntPtr lprc, IntPtr data);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, EnumMonitorProc proc, IntPtr data);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref InfoMonitor info);

    // Zona de trabajo (coordenadas fisicas) del monitor mas ANCHO - solo lectura, no mueve nada. El
    // mas ancho y no el de mas area: en esta maquina el de mas area es un monitor vertical (1440x2872
    // fisicos, 1080 DIP de ancho), y el caso "maximizado" que importa para s17 es una ventana ancha.
    private static RectMonitor? MonitorMasGrande()
    {
        RectMonitor? mejor = null; long area = 0;
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (h, _, _, _) =>
        {
            var info = new InfoMonitor { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<InfoMonitor>() };
            if (GetMonitorInfo(h, ref info))
            {
                long a = info.rcWork.right - info.rcWork.left;
                if (a > area) { area = a; mejor = info.rcWork; }
            }
            return true;
        }, IntPtr.Zero);
        return mejor;
    }

    private static void EjecutarInvAlmResponsiveSolo(MainWindow window, MainViewModel vm)
    {
        int fallos = 0;
        void Fallo(string codigo, string msg) { fallos++; Console.WriteLine($"FALLO: INVALM_RESPONSIVE_SOLO-{codigo} - {msg}"); }

        try
        {
            // --- Aislamiento: copia propia del personaje, abierta por ruta (mismo patron que
            //     EQUIP_RESPONSIVE_SOLO, H-04). Nunca se guarda: IsDirty se limpia tras cada cambio.
            var origen = vm.Home.Characters.FirstOrDefault(c => EstaDentro(c.FilePath, RaizPersonajesAislada))?.FilePath;
            if (origen == null) { Fallo("AISLAMIENTO", "no hay ninguna copia aislada de personaje que abrir"); return; }
            string carpetaCanario = Path.Combine(RaizPersonajesAislada, "canario-invalm", "tModLoader", "Players");
            Directory.CreateDirectory(carpetaCanario);
            string rutaCanario = Path.Combine(carpetaCanario, Path.GetFileName(origen));
            File.Copy(origen, rutaCanario, overwrite: true);
            string tplrOrigen = Path.ChangeExtension(origen, ".tplr");
            if (File.Exists(tplrOrigen)) File.Copy(tplrOrigen, Path.ChangeExtension(rutaCanario, ".tplr"), overwrite: true);
            vm.IsDirty = false;
            vm.LoadFromPath(rutaCanario);
            DoEvents(); DoEvents();
            ComprobarPersonajeAislado(vm, "INVALM_RESPONSIVE_SOLO");
            if (!string.Equals(vm.LoadedFilePath, rutaCanario, StringComparison.OrdinalIgnoreCase))
            { Fallo("AISLAMIENTO", $"el personaje cargado ({vm.LoadedFilePath}) no es la copia del canario"); Environment.Exit(4); }
            Console.WriteLine($"INVALM AISLAMIENTO: abierto por ruta la COPIA '{vm.LoadedFilePath}' (origen: la copia aislada '{origen}')");
            if (vm.InventoryContainer == null || vm.StorageGroup == null) { Fallo("PREPARACION", "sin Inventario/Almacenes (¿personaje sin cargar?)"); return; }

            string outDir = Environment.GetEnvironmentVariable("INVALM_RESPONSIVE_EVIDENCIA")
                            ?? Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia", "responsive-faseC");
            Directory.CreateDirectory(outDir);
            string etiqueta = Environment.GetEnvironmentVariable("INVALM_RESPONSIVE_ETIQUETA") ?? "actual";

            var objetosView = window.FindName("ObjetosView") as FrameworkElement;
            var pagInv = objetosView?.FindName("ObjetosPaginaInventario") as ScrollViewer;
            var pagAlm = objetosView?.FindName("ObjetosPaginaAlmacenes") as ScrollViewer;
            var pagEquip = objetosView?.FindName("ObjetosPaginaEquipamiento") as ScrollViewer;
            if (objetosView == null || pagInv == null || pagAlm == null || pagEquip == null) { Fallo("PREPARACION", "no se encuentran las paginas de Objetos"); return; }

            string idiomaOriginal = vm.Settings.Language;
            bool libreriaOriginal = vm.IsLibraryCollapsed;
            bool compactoOriginal = vm.Settings.IsCompactMode;

            // --- s23: colecciones LLENAS (50/50 y 40/40, objetos variados) - el peor caso real de
            //     rejilla y de rotulos "(50/50)". Solo en memoria, sobre la copia; nunca se guarda.
            int idObjeto = 1;
            void Llenar(ContainerViewModel c)
            {
                foreach (var s in c.Slots.Where(s => s.IsEmpty)) { s.PlaceItem(idObjeto); idObjeto = idObjeto % 400 + 1; }
            }
            Llenar(vm.InventoryContainer);
            foreach (var o in vm.StorageGroup.Options) { vm.StorageGroup.SelectCommand.Execute(o); Llenar(vm.StorageGroup.Current); }
            vm.StorageGroup.SelectCommand.Execute(vm.StorageGroup.Options[0]);
            DoEvents(); DoEvents();
            vm.IsDirty = false;
            string llenado = $"Inventario {vm.InventoryContainer.Slots.Count(s => !s.IsEmpty)}/{vm.InventoryContainer.Slots.Count}; " +
                             string.Join("; ", vm.StorageGroup.Options.Select(o => o.DisplayLabel));
            Console.WriteLine($"INVALM COLECCIONES LLENAS (s23): {llenado}");

            void IrA(int pagina)
            {
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.RequestObjetosSection(pagina);
                DoEvents(); DoEvents();
            }
            void ElegirAlmacen(int i)
            {
                vm.StorageGroup!.SelectCommand.Execute(vm.StorageGroup.Options[i]);
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

            // Celdas reales (el Border enfocable de SlotCompactTemplate) de la rejilla de un contenedor.
            List<Border> Celdas(ScrollViewer pagina, ContainerViewModel c) => Descendientes<SlotGridPanel>(pagina)
                .Where(p => p.IsVisible && ReferenceEquals(p.DataContext, c))
                .SelectMany(p => p.Children.OfType<FrameworkElement>())
                .Select(cp => Descendientes<Border>(cp).FirstOrDefault(b => b.Focusable))
                .Where(b => b != null).Cast<Border>().ToList();

            bool EnteroEnAmbosEjes(FrameworkElement fe)
            {
                try
                {
                    var r = RectVisible(fe, window);
                    return !r.IsEmpty && r.Height >= fe.ActualHeight - 0.5 && r.Width >= fe.ActualWidth - 0.5;
                }
                catch (InvalidOperationException) { return false; }
            }

            // Por nombre (FASE D, D-01/D-05: el Style de Editar es ahora EditarTarjetaComposicion, BasedOn SidePanelCard).
            FrameworkElement? PanelEditar() => objetosView.FindName("EditarTarjeta") as FrameworkElement is { IsVisible: true } ed ? ed : null;

            var resumen = new List<string>();

            // ---------------------------------------------------------------------------------
            // Medicion de UN estado de una pagina (Inventario o Almacenes).
            // ---------------------------------------------------------------------------------
            void Medir(ScrollViewer pagina, string nombrePagina, ContainerViewModel contenedor, string id)
            {
                pagina.ScrollToVerticalOffset(0);
                DoEvents(); DoEvents();
                WaitForDispatcher(120);
                string cab = $"[{id}] {nombrePagina} {window.ActualWidth:0}x{window.ActualHeight:0} SizeClass={vm.SizeClass} idioma={vm.Settings.Language} libreriaPlegada={vm.IsLibraryCollapsed} compacto={vm.Settings.IsCompactMode} contenedor='{contenedor.DisplayName}'";

                // --- A: scroll anidado del mismo eje (cualquier ScrollViewer vertical visible) ---
                var anidados = Descendientes<ScrollViewer>(pagina).Where(sv => sv.IsVisible && sv.TemplatedParent is not TextBoxBase).ToList();
                var anidadosV = anidados.Where(sv => sv.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled).ToList();
                string detalle = anidadosV.Count == 0 ? "ninguno" : string.Join("; ", anidadosV.Select(sv =>
                    $"{Nombre(sv)}(vp={sv.ViewportHeight:0.#} ext={sv.ExtentHeight:0.#} scr={sv.ScrollableHeight:0.#} barra={sv.ComputedVerticalScrollBarVisibility})"));
                Console.WriteLine($"INVALM {cab} | pagina vp={pagina.ViewportHeight:0.#} ext={pagina.ExtentHeight:0.#} scr={pagina.ScrollableHeight:0.#} ancho={pagina.ViewportWidth:0.#} | scrollAnidadosV={anidadosV.Count}: {detalle}");
                if (anidadosV.Count > 0)
                    Fallo("SCROLL-ANIDADO", $"{cab}: {anidadosV.Count} ScrollViewer vertical(es) dentro del scroll owner de la pagina ({detalle})");

                // --- G: horizontal inesperado ---
                foreach (var sv in anidados.Prepend(pagina))
                {
                    if (sv.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled && sv.ExtentWidth > sv.ViewportWidth + 0.5)
                        Fallo("HSCROLL", $"{cab}: {Nombre(sv)} Disabled con contenido de {sv.ExtentWidth:0.#}px en {sv.ViewportWidth:0.#}px");
                    if (sv.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
                        Fallo("HSCROLL", $"{cab}: {Nombre(sv)} muestra barra horizontal (ScrollableWidth={sv.ScrollableWidth:0.#})");
                }

                // --- Rejilla: celdas, tamaño, cuantas se ven ENTERAS sin desplazar nada ---
                var celdas = Celdas(pagina, contenedor);
                int enteras = celdas.Count(c => EnteroEnAmbosEjes(c));
                double celda = celdas.Count > 0 ? celdas.Min(c => c.ActualWidth) : 0;
                var sgp = Descendientes<SlotGridPanel>(pagina).FirstOrDefault(p => p.IsVisible && ReferenceEquals(p.DataContext, contenedor));
                double anchoRejilla = 0;
                if (celdas.Count > 0)
                {
                    var rs = celdas.Select(c => RectCompleto(c, window)).ToList();
                    anchoRejilla = rs.Max(r => r.Right) - rs.Min(r => r.Left);
                }
                Console.WriteLine($"INVALM {cab} | rejilla {celdas.Count} celdas de {celda:0.#}px (MinCell={sgp?.MinCell:0.#} MaxCell={sgp?.MaxCell:0.#} cols={contenedor.Columns}) | enteras sin desplazar={enteras}/{celdas.Count} | ancho rejilla {anchoRejilla:0.#}px de {pagina.ViewportWidth:0.#}px ({(pagina.ViewportWidth > 0 ? anchoRejilla / pagina.ViewportWidth : 0):P0})");
                if (sgp != null && celdas.Count > 0 && celda < sgp.MinCell - 0.5)
                    Fallo("CELDA", $"{cab}: celdas de {celda:0.#}px, por debajo de MinCell={sgp.MinCell:0.#}");
                if (celdas.Count != contenedor.Slots.Count)
                    Fallo("CELDA", $"{cab}: la rejilla pinta {celdas.Count} celdas y el contenedor tiene {contenedor.Slots.Count}");
                // H-C1/M-C3 (revisor de la FASE C): contrato real de "de un plumazo" (s22) para estas
                // paginas FINITAS - sin el banner "Deshacer" (que crece la cabecera un momento y SI puede
                // empujar la ultima fila al scroll del owner) todas las casillas se ven ENTERAS y la pagina
                // no desplaza. Antes solo se imprimia.
                bool conBanner = contenedor.CanUndoClear;
                if (!conBanner && pagina.ScrollableHeight > 0.5)
                    Fallo("PAGINA-SCROLL", $"{cab}: la pagina desplaza {pagina.ScrollableHeight:0.#}px (ext {pagina.ExtentHeight:0.#} en vp {pagina.ViewportHeight:0.#}) sin banner - la rejilla no se ajusto al viewport");
                if (!conBanner && enteras < celdas.Count)
                    Fallo("ENTERAS", $"{cab}: solo {enteras}/{celdas.Count} casillas se ven enteras sin desplazar");

                // --- M-C1 (revisor, s17): banda muerta bajo el marco y marco cenido. Con la rejilla en
                //     MaxCell y la Libreria desplegada el alto que la pagina ya no usa pasa a la Libreria. ---
                var marco = sgp == null ? null : Descendientes<Border>(pagina).FirstOrDefault(b => b.IsAncestorOf(sgp) && b.CornerRadius.TopLeft == 10);
                var filaLib = objetosView!.FindName("FilaLibreria") as RowDefinition;
                if (marco != null)
                {
                    var rm = RectCompleto(marco, window); var rp = RectCompleto(pagina, window);
                    double banda = rp.Bottom - rm.Bottom;
                    bool enMaxCell = sgp != null && celda >= sgp.MaxCell - 0.5;
                    Console.WriteLine($"INVALM {cab} | marco {rm.Width:0.#}x{rm.Height:0.#} (rejilla {anchoRejilla:0.#} de ancho) | banda vacia bajo el marco dentro de la pagina={banda:0.#}px | fila Libreria={filaLib?.ActualHeight:0.#}px | celda en MaxCell={enMaxCell}");
                    if (enMaxCell && !vm.IsLibraryCollapsed && banda > 40)
                        Fallo("BANDA", $"{cab}: con la rejilla en MaxCell quedan {banda:0.#}px vacios bajo el marco que deberian pasar a la Libreria (s17)");
                }

                // --- Clipping de celdas, botones y textos ---
                var elementos = new List<FrameworkElement>(celdas);
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
                    { clipH++; peorH = Math.Max(peorH, faltaX); if (ejemplos.Count < 4) ejemplos.Add($"H {Describir(fe)} {faltaX:0.#}px"); }
                    if (faltaY > 1)
                    {
                        var dueño = PrimerScrollQueDesplaza(fe, false);
                        if (dueño == null) { clipVPerdido++; peorV = Math.Max(peorV, faltaY); if (ejemplos.Count < 4) ejemplos.Add($"V-perdido {Describir(fe)} {faltaY:0.#}px"); }
                        else if (!ReferenceEquals(dueño, pagina)) { clipVAnidado++; peorV = Math.Max(peorV, faltaY); if (ejemplos.Count < 4) ejemplos.Add($"V-anidado({Nombre(dueño)}) {Describir(fe)} {faltaY:0.#}px"); }
                        else bajoPliegue++;
                    }
                }
                Console.WriteLine($"INVALM {cab} | elementos={elementos.Count} clipH={clipH} (peor {peorH:0.#}px) clipV-perdido={clipVPerdido} clipV-soloScrollAnidado={clipVAnidado} (peor {peorV:0.#}px) bajoPliegueDelOwnerPrincipal={bajoPliegue} {(ejemplos.Count > 0 ? "ej: " + string.Join(" | ", ejemplos) : "")}");
                if (clipH > 0) Fallo("CLIP-H", $"{cab}: {clipH} elemento(s) recortados en horizontal sin escape (peor {peorH:0.#}px)");
                if (clipVPerdido > 0) Fallo("CLIP-V", $"{cab}: {clipVPerdido} elemento(s) recortados en vertical sin ningun scroll que los alcance (peor {peorV:0.#}px)");
                if (clipVAnidado > 0) Fallo("CLIP-V", $"{cab}: {clipVAnidado} elemento(s) solo alcanzables por un scroll ANIDADO");

                // --- s3 A / s26 F: estructura (botones de accion, pildoras de almacen, navegacion de
                //     pagina) ENTERA sin desplazar la pagina ---
                // Sin las piezas de la barra de scroll de la propia pagina (RepeatButton/Thumb): no son
                // estructura de la pantalla, son el scroll owner.
                var botones = Descendientes<ButtonBase>(pagina).Where(b => b.IsVisible && b.ActualWidth > 0 && !celdas.Any(c => c.IsAncestorOf(b))
                    && !Descendientes<ScrollBar>(pagina).Any(sb => sb.IsAncestorOf(b))).ToList();
                var navs = new[] { "ObjetosNavToggle1", "ObjetosNavToggle2", "ObjetosNavToggle3" }.Select(n => objetosView!.FindName(n) as FrameworkElement).Where(n => n != null).Cast<FrameworkElement>().ToList();
                int botonesEnteros = botones.Count(EnteroEnAmbosEjes);
                int navEnteros = navs.Count(EnteroEnAmbosEjes);
                Console.WriteLine($"INVALM {cab} | estructura: botones/pildoras {botonesEnteros}/{botones.Count} enteros sin desplazar ({string.Join(", ", botones.Select(b => (b as ContentControl)?.Content is string s ? s : Describir(b)))}) | nav de pagina {navEnteros}/3");
                if (botonesEnteros != botones.Count)
                    Fallo("ESTRUCTURAL", $"{cab}: {botones.Count - botonesEnteros} boton(es)/pildora(s) de la pagina no se ven enteros sin desplazar ({string.Join(", ", botones.Where(b => !EnteroEnAmbosEjes(b)).Select(Describir))})");
                if (navEnteros != 3)
                    Fallo("ESTRUCTURAL", $"{cab}: la navegacion Equipamiento/Inventario/Almacenes no se ve entera ({navEnteros}/3)");

                // --- Overlap: rejilla / cabecera / panel Editar ---
                var editar = PanelEditar();
                var regiones = new List<(string n, Rect r)>();
                if (sgp != null && celdas.Count > 0)
                {
                    var rs = celdas.Select(c => RectCompleto(c, window)).ToList();
                    regiones.Add(("Rejilla", new Rect(new Point(rs.Min(r => r.Left), rs.Min(r => r.Top)), new Point(rs.Max(r => r.Right), rs.Max(r => r.Bottom)))));
                }
                foreach (var b in botones) regiones.Add(($"Boton {Describir(b)}", RectCompleto(b, window)));
                foreach (var n in navs) regiones.Add(($"Nav {n.Name}", RectCompleto(n, window)));
                if (editar != null) regiones.Add(("Panel Editar", RectCompleto(editar, window)));
                for (int i = 0; i < regiones.Count; i++)
                    for (int j = i + 1; j < regiones.Count; j++)
                    {
                        var a = regiones[i].r; var b = regiones[j].r;
                        double ix = Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left);
                        double iy = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top);
                        if (ix > 0.5 && iy > 0.5)
                            Fallo("OVERLAP", $"{cab}: '{regiones[i].n}' y '{regiones[j].n}' se pisan {ix:0.#}x{iy:0.#}px");
                    }

                // --- Panel Editar: scroll residual (permiso aa5f7395, solo se informa y se vigila el
                //     limite) y estrella de favorito (N-02) ---
                string editarTxt = "Editar: no encontrado";
                if (editar != null)
                {
                    var svEditar = Descendientes<ScrollViewer>(editar).FirstOrDefault(sv => sv.IsVisible && sv.TemplatedParent is not TextBoxBase);
                    editarTxt = $"Editar: {editar.ActualWidth:0.#}x{editar.ActualHeight:0.#} scrollPropio vp={svEditar?.ViewportHeight:0.#} ext={svEditar?.ExtentHeight:0.#} scr={svEditar?.ScrollableHeight:0.#} slot={(vm.ItemEdit.Slot == null ? "ninguno" : $"{vm.ItemEdit.Slot.ContainerName}#{vm.ItemEdit.Slot.SlotIndex}")}";
                    // Solo se INFORMA (encargo de la FASE C: re-medir, no cerrar ni aceptar el permiso
                    // aa5f7395, que sigue pendiente de decision del usuario; el panel Editar es s7).
                    if (svEditar != null && svEditar.ScrollableHeight > 12.3 + 0.05)
                        Console.WriteLine($"INVALM AVISO EDITAR-SCROLL {cab}: el panel Editar desplaza {svEditar.ScrollableHeight:0.#}px, por encima del limite del permiso propuesto aa5f7395 (12,3px, medido con un slot de armadura) - fuera del alcance de la FASE C, se informa al coordinador");
                    var estrella = Descendientes<Button>(editar).FirstOrDefault(b => b.IsVisible && vm.ItemEdit.Slot != null && ReferenceEquals(b.Command, vm.ItemEdit.Slot.ToggleFavoriteCommand));
                    if (vm.ItemEdit.Slot?.SupportsFavorite == true)
                    {
                        if (estrella == null) Fallo("ESTRELLA", $"{cab}: el slot seleccionado admite favorito y no hay boton de estrella visible en Editar");
                        else
                        {
                            var re = RectCompleto(estrella, window);
                            bool entera = EnteroEnAmbosEjes(estrella);
                            editarTxt += $" | estrella favorito '{estrella.Content}' {re.Width:0.#}x{re.Height:0.#} en ({re.X:0.#},{re.Y:0.#}) entera={entera}";
                            if (!entera) Fallo("ESTRELLA", $"{cab}: la estrella de favorito de Editar no se ve entera");
                        }
                    }
                }
                Console.WriteLine($"INVALM {cab} | {editarTxt}");

                resumen.Add($"{id}: vp={pagina.ViewportHeight:0.#} ext={pagina.ExtentHeight:0.#} scr={pagina.ScrollableHeight:0.#} anidados={anidadosV.Count} celda={celda:0.#} enteras={enteras}/{celdas.Count} rejilla={anchoRejilla:0}/{pagina.ViewportWidth:0} | {editarTxt}");

                string shot = Path.Combine(outDir, $"invalm-{etiqueta}-{id}.png");
                File.WriteAllBytes(shot, CapturarPng(window, window.ActualWidth, window.ActualHeight));
                Console.WriteLine($"INVALM {cab} | captura -> {shot}");
            }

            // ---------------------------------------------------------------------------------
            // Clic en un slot visible: no puede mover el scroll de la pagina y el foco de teclado
            // debe quedar en el propio slot (H5-14: los slots son enfocables). Eventos de raton
            // reales del arbol de WPF (PreviewMouseDown/MouseDown/MouseUp enrutados, sin entrada
            // a nivel de SO): recorren los mismos manejadores de clase (ScrollViewer.
            // OnMouseLeftButtonDown -> Focus -> BringIntoView) que un clic de verdad.
            // ---------------------------------------------------------------------------------
            void PruebaClic(ScrollViewer pagina, string nombrePagina, ContainerViewModel contenedor, string id, bool desplazada)
            {
                pagina.ScrollToVerticalOffset(0);
                DoEvents(); DoEvents(); WaitForDispatcher(80);
                if (desplazada && pagina.ScrollableHeight > 1) { pagina.ScrollToVerticalOffset(Math.Min(15, pagina.ScrollableHeight)); DoEvents(); DoEvents(); }
                window.Activate(); DoEvents();
                var candidatas = Celdas(pagina, contenedor).Where(EnteroEnAmbosEjes).ToList();
                // Una celda de la ultima fila visible entera (el caso mas propenso a arrastrar el
                // scroll: BringIntoView de un contenedor mas alto que el viewport alinea arriba).
                var celda = candidatas.OrderByDescending(c => RectCompleto(c, window).Top).ThenBy(c => RectCompleto(c, window).Left).FirstOrDefault();
                string cab = $"[{id}] {nombrePagina} {window.ActualWidth:0}x{window.ActualHeight:0} libreriaPlegada={vm.IsLibraryCollapsed} desplazada={desplazada}";
                if (celda == null) { Console.WriteLine($"INVALM CLIC {cab}: INCONCLUSIVE - ninguna celda entera visible"); return; }
                var todos = Descendientes<ScrollViewer>(window).Where(sv => sv.IsVisible && sv.TemplatedParent is not TextBoxBase).ToList();
                var antes = todos.ToDictionary(sv => sv, sv => sv.VerticalOffset);
                var rAntes = RectCompleto(celda, window);
                int t = Environment.TickCount;
                celda.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, t, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseDownEvent, Source = celda });
                celda.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, t, MouseButton.Left) { RoutedEvent = UIElement.MouseDownEvent, Source = celda });
                DoEvents(); DoEvents();
                celda.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, t + 60, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseUpEvent, Source = celda });
                celda.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, t + 60, MouseButton.Left) { RoutedEvent = UIElement.MouseUpEvent, Source = celda });
                DoEvents(); DoEvents(); WaitForDispatcher(80);
                var movidos = todos.Where(sv => Math.Abs(sv.VerticalOffset - antes[sv]) > 0.5).Select(sv => $"{Nombre(sv)} {antes[sv]:0.#}->{sv.VerticalOffset:0.#}").ToList();
                var rDespues = RectCompleto(celda, window);
                var foco = Keyboard.FocusedElement as DependencyObject;
                bool focoEnSlot = foco != null && (ReferenceEquals(foco, celda) || celda.IsAncestorOf(foco));
                bool seleccionado = celda.DataContext is ItemSlotViewModel s && ReferenceEquals(vm.ItemEdit.Slot, s);
                Console.WriteLine($"INVALM CLIC {cab}: celda en y={rAntes.Top:0.#} -> {rDespues.Top:0.#} (delta {rDespues.Top - rAntes.Top:0.#}px) | scrolls movidos: {(movidos.Count == 0 ? "ninguno" : string.Join("; ", movidos))} | foco de teclado en {(foco == null ? "null" : Nombre(foco))} (en el slot={focoEnSlot}) | slot seleccionado en Editar={seleccionado} | ventana activa={window.IsActive}");
                if (movidos.Count > 0 || Math.Abs(rDespues.Top - rAntes.Top) > 0.5)
                    Fallo("CLIC-SCROLL", $"{cab}: pulsar un slot visible desplaza {rDespues.Top - rAntes.Top:0.#}px ({string.Join("; ", movidos)})");
                if (window.IsActive && !focoEnSlot)
                    Fallo("CLIC-SCROLL", $"{cab}: tras pulsar el slot el foco de teclado queda en {(foco == null ? "null" : Nombre(foco))}, no en el slot");
                if (!seleccionado)
                    Fallo("CLIC-SCROLL", $"{cab}: el clic no selecciono el slot en Editar");
                pagina.ScrollToVerticalOffset(0); DoEvents();
            }

            // Slot de Inventario con objeto seleccionado: Editar enseña su contenido completo
            // (estado mas alto) y la estrella de favorito (Inventario SI guarda favorito, OBJ-05).
            void SeleccionarSlotInventario()
            {
                var s = vm.InventoryContainer!.Slots.FirstOrDefault(x => !x.IsEmpty);
                if (s != null) { vm.SelectSlot(s); DoEvents(); }
            }
            void SeleccionarSlotAlmacen()
            {
                var s = vm.StorageGroup!.Current.Slots.FirstOrDefault(x => !x.IsEmpty);
                if (s != null) { vm.SelectSlot(s); DoEvents(); }
            }

            // ---------------------------------------------------------------------------------
            // 1) Tamaños (ES), Libreria plegada y desplegada, Inventario y Almacenes (Banco).
            // ---------------------------------------------------------------------------------
            vm.Settings.Language = "es"; vm.Settings.IsCompactMode = false; DoEvents();
            var casos = new (string id, double w, double h, bool plegada)[]
            {
                ("min-1080x700-libplegada", 1080, 700, true),
                ("min-1080x700-libdesplegada", 1080, 700, false),
                ("medio-1366x768-libplegada", 1366, 768, true),
                ("medio-1366x768-libdesplegada", 1366, 768, false),
                ("amplio-1520x860-libdesplegada", 1520, 860, false),
                ("grande-1920x1080-libdesplegada", 1920, 1080, false),
                // s2/s17: tamaño Extra fijo, independiente del monitor que haya conectado (el
                // maximizado real depende de el - ver MAXIMIZADO mas abajo).
                ("extra-2560x1440-libdesplegada", 2560, 1440, false),
            };
            // H-C1 (revisor): la PRIMERA entrada en cada pagina, en el minimo con la Libreria DESPLEGADA y
            // sin ninguna medida previa de esa pagina en este proceso (ViewModel recien creado y personaje
            // recien cargado), es el caso que dejaba ext=316 en vp=295,9 - se mide ANTES que la plegada.
            window.WindowState = WindowState.Normal; FijarTamaño(window, 1080, 700); vm.IsLibraryCollapsed = false;
            IrA(1); SeleccionarSlotInventario(); Medir(pagInv, "Inventario", vm.InventoryContainer, "inv-primera-entrada-1080x700-libdesplegada");
            IrA(2); ElegirAlmacen(0); SeleccionarSlotAlmacen(); Medir(pagAlm, "Almacenes", vm.StorageGroup.Current, "alm-primera-entrada-1080x700-libdesplegada");
            foreach (var (id, w, h, plegada) in casos)
            {
                window.WindowState = WindowState.Normal; DoEvents();
                FijarTamaño(window, w, h);
                vm.IsLibraryCollapsed = plegada;
                IrA(1); SeleccionarSlotInventario();
                Medir(pagInv, "Inventario", vm.InventoryContainer, "inv-" + id);
                IrA(2); ElegirAlmacen(0); SeleccionarSlotAlmacen();
                Medir(pagAlm, "Almacenes", vm.StorageGroup.Current, "alm-" + id);
            }
            vm.IsLibraryCollapsed = false;
            // En una maquina con varios monitores el maximizado depende del monitor donde quede la
            // ventana (medido: una ejecucion maximizo a 1080x1162 en un monitor vertical). Se lleva
            // antes al origen del monitor PRINCIPAL para medir siempre el mismo maximizado.
            FijarTamaño(window, 1180, 860);
            var monitor = MonitorMasGrande();
            if (monitor is { } m)
            {
                // Coordenadas fisicas -> DIP de WPF (la ventana vive en DIP).
                var src = PresentationSource.FromVisual(window);
                double kx = src?.CompositionTarget?.TransformFromDevice.M11 ?? 1, ky = src?.CompositionTarget?.TransformFromDevice.M22 ?? 1;
                window.Left = (m.left + 40) * kx; window.Top = (m.top + 40) * ky;
                Console.WriteLine($"INVALM MAXIMIZADO: monitor mas ancho del equipo {m.right - m.left}x{m.bottom - m.top} (zona de trabajo) en ({m.left},{m.top})");
            }
            DoEvents();
            window.WindowState = WindowState.Maximized; DoEvents(); DoEvents(); DoEvents(); WaitForDispatcher(150);
            Console.WriteLine($"INVALM MAXIMIZADO: ventana {window.ActualWidth:0}x{window.ActualHeight:0} SizeClass={vm.SizeClass}"
                + (vm.SizeClass == Terrakeep.App.ViewModels.WindowSizeClass.Extra ? "" : " -> INCONCLUSIVE como caso 'ventana grande': el unico monitor conectado no da un maximizado Extra (el tamaño grande lo cubre extra-2560x1440)"));
            IrA(1); SeleccionarSlotInventario(); Medir(pagInv, "Inventario", vm.InventoryContainer, "inv-maximizado-libdesplegada");
            IrA(2); ElegirAlmacen(0); SeleccionarSlotAlmacen(); Medir(pagAlm, "Almacenes", vm.StorageGroup.Current, "alm-maximizado-libdesplegada");
            window.WindowState = WindowState.Normal; DoEvents();

            // Los 4 almacenes en el minimo (s16: la navegacion entre almacenes se ve y funciona en
            // el tamaño mas pequeño; cada uno tiene 40 casillas).
            FijarTamaño(window, 1080, 700); vm.IsLibraryCollapsed = false; IrA(2);
            string[] claves = ["banco", "cajafuerte", "forja", "vacio"];
            for (int i = 1; i < 4; i++) { ElegirAlmacen(i); SeleccionarSlotAlmacen(); Medir(pagAlm, "Almacenes", vm.StorageGroup.Current, $"alm-min-1080x700-libdesplegada-{claves[i]}"); }
            ElegirAlmacen(0);

            // ---------------------------------------------------------------------------------
            // 2) EN (s25), modo compacto (s15: preferencia del usuario, compatible con el
            //    responsive) y banner "Deshacer" visible (H4-05, crece la cabecera) en el minimo.
            // ---------------------------------------------------------------------------------
            vm.Settings.Language = "en"; DoEvents(); DoEvents();
            IrA(1); SeleccionarSlotInventario(); Medir(pagInv, "Inventario", vm.InventoryContainer, "inv-min-1080x700-EN-libdesplegada");
            IrA(2); SeleccionarSlotAlmacen(); Medir(pagAlm, "Almacenes", vm.StorageGroup.Current, "alm-min-1080x700-EN-libdesplegada");
            vm.Settings.Language = "es"; DoEvents(); DoEvents();
            vm.Settings.IsCompactMode = true; DoEvents(); DoEvents();
            IrA(1); SeleccionarSlotInventario(); Medir(pagInv, "Inventario", vm.InventoryContainer, "inv-min-1080x700-compacto-libdesplegada");
            IrA(2); SeleccionarSlotAlmacen(); Medir(pagAlm, "Almacenes", vm.StorageGroup.Current, "alm-min-1080x700-compacto-libdesplegada");
            vm.Settings.IsCompactMode = false; DoEvents(); DoEvents();
            {
                IrA(1);
                vm.InventoryContainer.ClearAllCommand.Execute(null); DoEvents(); DoEvents();
                Console.WriteLine($"INVALM BANNER: tras 'Vaciar contenedor' CanUndoClear={vm.InventoryContainer.CanUndoClear}");
                Medir(pagInv, "Inventario", vm.InventoryContainer, "inv-min-1080x700-banner-deshacer");
                if (vm.InventoryContainer.CanUndoClear) vm.InventoryContainer.UndoClearCommand.Execute(null);
                DoEvents(); DoEvents(); vm.IsDirty = false;
                Console.WriteLine($"INVALM BANNER: tras 'Deshacer' Inventario {vm.InventoryContainer.Slots.Count(s => !s.IsEmpty)}/50");
                Medir(pagInv, "Inventario", vm.InventoryContainer, "inv-min-1080x700-tras-deshacer");
            }

            // ---------------------------------------------------------------------------------
            // 3) Clic en un slot (bug del salto de ~37px, 020fde86): en el minimo con la Libreria
            //    desplegada (la pagina SI desplaza ahi) y con la pagina ya desplazada.
            // ---------------------------------------------------------------------------------
            FijarTamaño(window, 1080, 700); vm.IsLibraryCollapsed = false;
            IrA(1); PruebaClic(pagInv, "Inventario", vm.InventoryContainer, "clic-inv", false); PruebaClic(pagInv, "Inventario", vm.InventoryContainer, "clic-inv", true);
            IrA(2); PruebaClic(pagAlm, "Almacenes", vm.StorageGroup.Current, "clic-alm", false); PruebaClic(pagAlm, "Almacenes", vm.StorageGroup.Current, "clic-alm", true);
            IrA(0);
            if (vm.EquipmentGroup != null) { PruebaClic(pagEquip, "Equipamiento", vm.EquipmentGroup.Current, "clic-equip", false); PruebaClic(pagEquip, "Equipamiento", vm.EquipmentGroup.Current, "clic-equip", true); }

            // ---------------------------------------------------------------------------------
            // 4) Navegacion de pagina con etiqueta propia (no "1/2/3" a secas).
            // ---------------------------------------------------------------------------------
            {
                var loc = Terrakeep.App.Services.LocalizationService.Instance;
                string[] esperados = [loc["char_tab_equipment"], loc["char_tab_inventory"], loc["char_tab_storage"]];
                for (int i = 0; i < 3; i++)
                {
                    var rb = objetosView.FindName($"ObjetosNavToggle{i + 1}") as RadioButton;
                    string texto = rb == null ? "" : string.Join(" ", Descendientes<TextBlock>(rb).Where(t => t.IsVisible).Select(t => t.Text).Where(x => !string.IsNullOrWhiteSpace(x)));
                    if (rb?.Content is string sc) texto = sc;
                    bool conEtiqueta = texto.Contains(esperados[i], StringComparison.OrdinalIgnoreCase);
                    Console.WriteLine($"INVALM NAV: ObjetosNavToggle{i + 1} texto visible='{texto}' tooltip='{rb?.ToolTip}' (esperada etiqueta '{esperados[i]}') -> {conEtiqueta}");
                    if (!conEtiqueta) Fallo("NAV-ETIQUETA", $"el boton de pagina {i + 1} no lleva la etiqueta '{esperados[i]}' (solo '{texto}', confundible con los 1/2/3 de Loadout)");
                }
            }

            // ---------------------------------------------------------------------------------
            // 5) Redimensionado en caliente (s24) en Almacenes con la Forja elegida: grande ->
            //    normal -> minimo -> grande sin perder pagina, almacen, slot de Editar, libreria,
            //    busqueda ni ViewModels.
            // ---------------------------------------------------------------------------------
            {
                FijarTamaño(window, 1920, 1080); vm.IsLibraryCollapsed = false; IrA(2); ElegirAlmacen(2);
                var grupoAntes = vm.StorageGroup; var invAntes = vm.InventoryContainer; var forja = vm.StorageGroup.Current;
                var slotElegido = forja.Slots[7]; vm.SelectSlot(slotElegido); DoEvents();
                vm.Library.SearchText = "sword"; DoEvents(); DoEvents();
                foreach (var (paso, w, h) in new[] { ("grande", 1920.0, 1080.0), ("normal", 1366.0, 768.0), ("minimo", 1080.0, 700.0), ("grande-vuelta", 1920.0, 1080.0) })
                {
                    FijarTamaño(window, w, h); DoEvents(); DoEvents();
                    bool ok = ReferenceEquals(vm.StorageGroup, grupoAntes) && ReferenceEquals(vm.InventoryContainer, invAntes)
                              && vm.ObjetosSubTabIndex == 2 && vm.StorageGroup!.SelectedIndex == 2 && ReferenceEquals(vm.StorageGroup.Current, forja)
                              && ReferenceEquals(vm.ItemEdit.Slot, slotElegido) && !vm.IsLibraryCollapsed && vm.Library.SearchText == "sword";
                    Console.WriteLine($"INVALM RESIZE {paso} {w:0}x{h:0}: mismosVM={ReferenceEquals(vm.StorageGroup, grupoAntes) && ReferenceEquals(vm.InventoryContainer, invAntes)} pagina={vm.ObjetosSubTabIndex} almacen={vm.StorageGroup!.SelectedIndex} slotEditar={(ReferenceEquals(vm.ItemEdit.Slot, slotElegido) ? "igual" : "PERDIDO")} libreriaPlegada={vm.IsLibraryCollapsed} busqueda='{vm.Library.SearchText}'");
                    if (!ok) Fallo("RESIZE", $"en el paso '{paso}' ({w:0}x{h:0}) se perdio estado");
                    Medir(pagAlm, "Almacenes", vm.StorageGroup.Current, $"alm-resize-{paso}");
                }
                vm.Library.SearchText = string.Empty; ElegirAlmacen(0);
            }

            // ---------------------------------------------------------------------------------
            // 6) Negative acceptance del mecanismo VIEJO (s26/s28): el ScrollViewer propio de
            //    ContainerCompactTemplate no puede seguir existiendo por debajo del modelo nuevo.
            // ---------------------------------------------------------------------------------
            {
                FijarTamaño(window, 1366, 768); IrA(1); DoEvents();
                var svInv = Descendientes<ScrollViewer>(pagInv).Where(sv => sv.TemplatedParent is not TextBoxBase).ToList();
                IrA(2); DoEvents();
                var svAlm = Descendientes<ScrollViewer>(pagAlm).Where(sv => sv.TemplatedParent is not TextBoxBase).ToList();
                Console.WriteLine($"INVALM VIEJO: ScrollViewer dentro de ObjetosPaginaInventario (visibles o no) = {svInv.Count}, dentro de ObjetosPaginaAlmacenes = {svAlm.Count} (esperado 0 y 0; antes 1 y 1, el de ContainerCompactTemplate)");
                if (svInv.Count + svAlm.Count > 0)
                    Fallo("VIEJO", $"quedan {svInv.Count + svAlm.Count} ScrollViewer dentro de las paginas de Inventario/Almacenes - el scroll local viejo sigue por debajo del owner de la pagina");
                bool plantillaVieja = objetosView.TryFindResource("ContainerCompactTemplate") != null;
                Console.WriteLine($"INVALM VIEJO: recurso ContainerCompactTemplate existe = {plantillaVieja} (esperado False)");
                if (plantillaVieja) Fallo("VIEJO", "la plantilla ContainerCompactTemplate (rejilla con ScrollViewer propio) sigue existiendo");
            }

            Console.WriteLine("INVALM RESUMEN:");
            foreach (var r in resumen) Console.WriteLine("  " + r);

            vm.Settings.Language = idiomaOriginal;
            vm.Settings.IsCompactMode = compactoOriginal;
            vm.IsLibraryCollapsed = libreriaOriginal;
            vm.IsDirty = false;
            IrA(0);
            FijarTamaño(window, 1180, 860);
            DoEvents();
        }
        catch (Exception ex) { fallos++; Console.WriteLine("INVALM-RESPONSIVE-EXCEPTION: " + ex); }
        Console.WriteLine($"INVALM_RESPONSIVE_SOLO: {fallos} fallo(s)");
    }
}
