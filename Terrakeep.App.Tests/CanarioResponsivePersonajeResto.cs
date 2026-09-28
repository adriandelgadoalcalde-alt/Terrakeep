// CANARIO REAL - FASE E del responsive global (PDF "Arreglo familia keep", bloque 2 "TERRAKEEP -
// RESPONSIVE GLOBAL, PAGINACION Y SCROLL COMO ULTIMO RECURSO", secciones 3, 8, 13-17, 19, 22-26,
// 28-29 y 34). 28-sep-2026, aplicador-fix-responsive-faseE.
//
// PERSONAJE_RESPONSIVE_SOLO=1 mide el "resto de Personaje" (todo lo que no es Objetos/Library/
// BuffLibrary/Research, ya cerrado en las FASES B/C/D): Buffs (su rejilla, NO la Libreria de abajo,
// ya migrada en la FASE D), Apariencia, Puntos de aparicion, Desbloqueos, Version y Comparar.
//
// BUFFS (arreglo real de esta fase, HARD FAIL - la rejilla de 44/22/10 slots era la UNICA region que
// quedaba con el patron viejo "ContainerCompactTemplate" (ScrollViewer PROPIO anidado dentro del
// scroll owner de la pagina) que la FASE C ya retiro para Equipamiento/Inventario/Almacenes - ver el
// comentario historico en Views/ObjetosView.xaml. Mismo arreglo aplicado aqui: ScrollViewer
// "BuffsPaginaContenedor" + AjusteAlViewport como UNICO scroll owner, BuffContainerCompactTemplate
// sin ScrollViewer propio. Codigos:
//   - SCROLL-ANIDADO (s26 A) un ScrollViewer vertical dentro de BuffsPaginaContenedor.
//   - PAGINA-SCROLL / ENTERAS (s22) sin el banner "Deshacer", la pagina no debe desplazar y las 44
//                          celdas deben verse ENTERAS sin desplazar nada (11x4, cabe de sobra).
//   - CLIP-H / CLIP-V / OVERLAP / HSCROLL - mismos codigos que EQUIP/INVALM_RESPONSIVE_SOLO.
//   - VIEJO (s26/s28 negative acceptance) el ScrollViewer propio de BuffContainerCompactTemplate ya
//                          no puede existir por debajo de BuffsPaginaContenedor.
//
// APARIENCIA / SPAWN POINTS / DESBLOQUEOS / VERSION / COMPARAR: estas 5 pestañas ya usan UN solo
// ScrollViewer por pagina con estructura (cabecera/selectores/botones) FUERA de el (SpawnpointsView,
// UnlocksView, VersionView, CompareView) o dos ScrollViewer HERMANOS con proposito real distinto
// (AppearanceView: preview fijo a la izquierda para que no se pierda de vista al editar, P-1) - no se
// ha tocado codigo de produccion en ellas esta ronda (sin medicion en tiempo real con el arnes: la
// RESTRICCION DE EJECUCION del encargo impidio abrir ventanas hasta 'via libre'). Aqui solo se
// INFORMA con geometria real (AVISO, nunca FALLO) para que la siguiente ronda, con el arnes libre,
// decida si hace falta tocar algo mas - ver bitacora.md, entrada de la FASE E, para el razonamiento
// completo por que Apariencia es la candidata mas probable (su propio comentario historico llama al
// ScrollViewer de la columna del preview "ScrollViewer de seguridad").
//
// Solo abre una COPIA del personaje (carpeta del arnes, AislamientoPartidasReales.cs), nunca guarda.
// Capturas en PERSONAJE_RESPONSIVE_EVIDENCIA (si se define) o en <bin>/keepqa-evidencia/responsive-faseE.
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
    // Indices reales de MainViewModel.PersonajeInnerTab (enum privado - mismos valores a mano).
    private const int TabObjetos = 0, TabBuffs = 1, TabInvestigacion = 2, TabApariencia = 3,
                       TabSpawnPoints = 4, TabDesbloqueos = 5, TabVersion = 6, TabComparar = 7;

    private static void EjecutarPersonajeRestoResponsiveSolo(MainWindow window, MainViewModel vm)
    {
        int fallos = 0;
        void Fallo(string codigo, string msg) { fallos++; Console.WriteLine($"FALLO: PERSONAJE_RESPONSIVE_SOLO-{codigo} - {msg}"); }

        try
        {
            // --- Aislamiento: copia propia del personaje (mismo patron que EQUIP/INVALM_RESPONSIVE_SOLO). ---
            var origen = vm.Home.Characters.FirstOrDefault(c => EstaDentro(c.FilePath, RaizPersonajesAislada))?.FilePath;
            if (origen == null) { Fallo("AISLAMIENTO", "no hay ninguna copia aislada de personaje que abrir"); return; }
            string carpetaCanario = Path.Combine(RaizPersonajesAislada, "canario-personaje", "tModLoader", "Players");
            Directory.CreateDirectory(carpetaCanario);
            string rutaCanario = Path.Combine(carpetaCanario, Path.GetFileName(origen));
            File.Copy(origen, rutaCanario, overwrite: true);
            string tplrOrigen = Path.ChangeExtension(origen, ".tplr");
            if (File.Exists(tplrOrigen)) File.Copy(tplrOrigen, Path.ChangeExtension(rutaCanario, ".tplr"), overwrite: true);
            vm.IsDirty = false;
            vm.LoadFromPath(rutaCanario);
            DoEvents(); DoEvents();
            ComprobarPersonajeAislado(vm, "PERSONAJE_RESPONSIVE_SOLO");
            if (!string.Equals(vm.LoadedFilePath, rutaCanario, StringComparison.OrdinalIgnoreCase))
            { Fallo("AISLAMIENTO", $"el personaje cargado ({vm.LoadedFilePath}) no es la copia del canario"); Environment.Exit(4); }
            Console.WriteLine($"PERSONAJE AISLAMIENTO: abierto por ruta la COPIA '{vm.LoadedFilePath}' (origen: la copia aislada '{origen}')");
            if (vm.Buffs.Container == null) { Fallo("PREPARACION", "sin Buffs.Container (¿personaje sin cargar?)"); return; }

            string outDir = Environment.GetEnvironmentVariable("PERSONAJE_RESPONSIVE_EVIDENCIA")
                            ?? Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia", "responsive-faseE");
            Directory.CreateDirectory(outDir);
            string etiqueta = Environment.GetEnvironmentVariable("PERSONAJE_RESPONSIVE_ETIQUETA") ?? "actual";

            var pagBuffs = window.FindName("BuffsPaginaContenedor") as ScrollViewer;
            if (pagBuffs == null) { Fallo("PREPARACION", "no se encuentra BuffsPaginaContenedor"); return; }

            string idiomaOriginal = vm.Settings.Language;
            bool libreriaBuffsOriginal = vm.IsBuffLibraryCollapsed;

            static string Nombre(DependencyObject d) => d is FrameworkElement f
                ? $"{f.GetType().Name}{(string.IsNullOrEmpty(f.Name) ? "" : "#" + f.Name)}"
                : d.GetType().Name;

            bool EnteroEnAmbosEjes(FrameworkElement fe)
            {
                try
                {
                    var r = RectVisible(fe, window);
                    return !r.IsEmpty && r.Height >= fe.ActualHeight - 0.5 && r.Width >= fe.ActualWidth - 0.5;
                }
                catch (InvalidOperationException) { return false; }
            }

            List<Border> CeldasBuffs(ScrollViewer pagina) => Descendientes<SlotGridPanel>(pagina)
                .Where(p => p.IsVisible)
                .SelectMany(p => p.Children.OfType<FrameworkElement>())
                .Select(cp => Descendientes<Border>(cp).FirstOrDefault(b => b.Focusable))
                .Where(b => b != null).Cast<Border>().ToList();

            void IrABuffs()
            {
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = TabBuffs;
                DoEvents(); DoEvents();
            }

            var resumen = new List<string>();

            // ---------------------------------------------------------------------------------
            // Medicion de la rejilla de Buffs (arreglo real de esta fase).
            // ---------------------------------------------------------------------------------
            void MedirBuffs(string id)
            {
                pagBuffs.ScrollToVerticalOffset(0);
                DoEvents(); DoEvents();
                WaitForDispatcher(120);
                string cab = $"[{id}] Buffs {window.ActualWidth:0}x{window.ActualHeight:0} SizeClass={vm.SizeClass} idioma={vm.Settings.Language} libreriaBuffsPlegada={vm.IsBuffLibraryCollapsed}";

                // --- A: scroll anidado del mismo eje dentro del owner de la pagina ---
                var anidados = Descendientes<ScrollViewer>(pagBuffs).Where(sv => !ReferenceEquals(sv, pagBuffs) && sv.IsVisible && sv.TemplatedParent is not TextBoxBase).ToList();
                var anidadosV = anidados.Where(sv => sv.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled).ToList();
                string detalle = anidadosV.Count == 0 ? "ninguno" : string.Join("; ", anidadosV.Select(sv =>
                    $"{Nombre(sv)}(vp={sv.ViewportHeight:0.#} ext={sv.ExtentHeight:0.#} scr={sv.ScrollableHeight:0.#})"));
                Console.WriteLine($"PERSONAJE {cab} | pagina vp={pagBuffs.ViewportHeight:0.#} ext={pagBuffs.ExtentHeight:0.#} scr={pagBuffs.ScrollableHeight:0.#} | scrollAnidadosV={anidadosV.Count}: {detalle}");
                if (anidadosV.Count > 0)
                    Fallo("SCROLL-ANIDADO", $"{cab}: {anidadosV.Count} ScrollViewer vertical(es) dentro de BuffsPaginaContenedor ({detalle})");

                foreach (var sv in anidados.Prepend(pagBuffs))
                {
                    if (sv.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled && sv.ExtentWidth > sv.ViewportWidth + 0.5)
                        Fallo("HSCROLL", $"{cab}: {Nombre(sv)} Disabled con contenido de {sv.ExtentWidth:0.#}px en {sv.ViewportWidth:0.#}px");
                    if (sv.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
                        Fallo("HSCROLL", $"{cab}: {Nombre(sv)} muestra barra horizontal (ScrollableWidth={sv.ScrollableWidth:0.#})");
                }

                var celdas = CeldasBuffs(pagBuffs);
                int enteras = celdas.Count(c => EnteroEnAmbosEjes(c));
                double celda = celdas.Count > 0 ? celdas.Min(c => c.ActualWidth) : 0;
                var sgp = Descendientes<SlotGridPanel>(pagBuffs).FirstOrDefault(p => p.IsVisible);
                Console.WriteLine($"PERSONAJE {cab} | rejilla {celdas.Count} celdas de {celda:0.#}px (MinCell={sgp?.MinCell:0.#} MaxCell={sgp?.MaxCell:0.#} cols={vm.Buffs.Container!.Columns}) | enteras sin desplazar={enteras}/{celdas.Count}");
                if (celdas.Count != vm.Buffs.Container.Slots.Count)
                    Fallo("CELDA", $"{cab}: la rejilla pinta {celdas.Count} celdas y el contenedor tiene {vm.Buffs.Container.Slots.Count}");
                if (sgp != null && celdas.Count > 0 && celda < sgp.MinCell - 0.5)
                    Fallo("CELDA", $"{cab}: celdas de {celda:0.#}px, por debajo de MinCell={sgp.MinCell:0.#}");

                bool conBanner = vm.Buffs.Container.CanUndoClear;
                if (!conBanner && pagBuffs.ScrollableHeight > 0.5)
                    Fallo("PAGINA-SCROLL", $"{cab}: la pagina desplaza {pagBuffs.ScrollableHeight:0.#}px (ext {pagBuffs.ExtentHeight:0.#} en vp {pagBuffs.ViewportHeight:0.#}) sin banner - 44/22/10 slots en 11 columnas (<=4 filas) deberian caber siempre");
                if (!conBanner && enteras < celdas.Count)
                    Fallo("ENTERAS", $"{cab}: solo {enteras}/{celdas.Count} casillas de Buffs se ven enteras sin desplazar");

                // --- Clipping de celdas/botones/textos dentro de la pagina de Buffs ---
                var elementos = new List<FrameworkElement>(celdas);
                elementos.AddRange(Descendientes<ButtonBase>(pagBuffs).Where(b => b.IsVisible && b.ActualWidth > 0));
                int clipH = 0, clipV = 0;
                foreach (var fe in elementos.Distinct())
                {
                    var rc = RectCompleto(fe, window);
                    var z = ZonaVisible(fe, window);
                    double faltaX = z.IsEmpty ? rc.Width : Math.Min(rc.Width, Math.Max(0, z.Left - rc.Left) + Math.Max(0, rc.Right - z.Right));
                    double faltaY = z.IsEmpty ? rc.Height : Math.Min(rc.Height, Math.Max(0, z.Top - rc.Top) + Math.Max(0, rc.Bottom - z.Bottom));
                    if (faltaX > 1) clipH++;
                    if (faltaY > 1) clipV++;
                }
                Console.WriteLine($"PERSONAJE {cab} | elementos={elementos.Count} clipH={clipH} clipV={clipV}");
                if (clipH > 0) Fallo("CLIP-H", $"{cab}: {clipH} elemento(s) de Buffs recortados en horizontal");
                if (clipV > 0 && !conBanner) Fallo("CLIP-V", $"{cab}: {clipV} elemento(s) de Buffs recortados en vertical (sin banner que lo justifique)");

                resumen.Add($"{id}: vp={pagBuffs.ViewportHeight:0.#} ext={pagBuffs.ExtentHeight:0.#} scr={pagBuffs.ScrollableHeight:0.#} anidados={anidadosV.Count} celda={celda:0.#} enteras={enteras}/{celdas.Count}");

                string shot = Path.Combine(outDir, $"personaje-{etiqueta}-{id}.png");
                File.WriteAllBytes(shot, CapturarPng(window, window.ActualWidth, window.ActualHeight));
                Console.WriteLine($"PERSONAJE {cab} | captura -> {shot}");
            }

            // ---------------------------------------------------------------------------------
            // Rellenar TODOS los slots de Buffs (peor caso, 44 a version>=269) - Bu.PlaceBuff,
            // mismo patron real que PruebasBuffsAparienciaVersion.cs.
            // ---------------------------------------------------------------------------------
            var slotsBuff = vm.Buffs.Container.Slots;
            for (int i = 0; i < slotsBuff.Count; i++) slotsBuff[i].PlaceBuff(i % 400 + 1);
            DoEvents(); DoEvents();
            vm.IsDirty = false;
            Console.WriteLine($"PERSONAJE BUFFS LLENOS: {slotsBuff.Count(s => s.IsNotEmpty)}/{slotsBuff.Count}");

            // ---------------------------------------------------------------------------------
            // Tamaños del encargo (§18/§2, ES), primera entrada en el minimo con el VM recien
            // cargado, Libreria de buffs plegada Y desplegada.
            // ---------------------------------------------------------------------------------
            vm.Settings.Language = "es"; DoEvents();
            var casos = new (string id, double w, double h)[]
            {
                ("min-1080x700", 1080, 700),
                ("medio-1366x768", 1366, 768),
                ("amplio-1520x860", 1520, 860),
                ("grande-1920x1080", 1920, 1080),
                ("extra-2560x1440", 2560, 1440),
            };
            window.WindowState = WindowState.Normal; FijarTamaño(window, 1080, 700);
            vm.IsBuffLibraryCollapsed = true; IrABuffs(); MedirBuffs("primera-entrada-1080x700-libplegada");
            vm.IsBuffLibraryCollapsed = false; DoEvents(); DoEvents(); MedirBuffs("min-1080x700-libdesplegada");
            foreach (var (id, w, h) in casos)
            {
                window.WindowState = WindowState.Normal; DoEvents();
                FijarTamaño(window, w, h);
                vm.IsBuffLibraryCollapsed = true; DoEvents(); DoEvents();
                MedirBuffs(id + "-libplegada");
                vm.IsBuffLibraryCollapsed = false; DoEvents(); DoEvents();
                MedirBuffs(id + "-libdesplegada");
            }

            // --- EN (s25) en el minimo ---
            vm.Settings.Language = "en"; DoEvents(); DoEvents();
            FijarTamaño(window, 1080, 700); vm.IsBuffLibraryCollapsed = false;
            MedirBuffs("min-1080x700-EN-libdesplegada");
            vm.Settings.Language = "es"; DoEvents(); DoEvents();

            // --- Banner "Deshacer" visible (crece la cabecera, se INFORMA aparte, no cuenta como FALLO) ---
            {
                vm.Buffs.Container.ClearAllCommand.Execute(null); DoEvents(); DoEvents();
                Console.WriteLine($"PERSONAJE BANNER: tras 'Vaciar' CanUndoClear={vm.Buffs.Container.CanUndoClear}");
                MedirBuffs("min-1080x700-banner-deshacer");
                if (vm.Buffs.Container.CanUndoClear) vm.Buffs.Container.UndoClearCommand.Execute(null);
                DoEvents(); DoEvents(); vm.IsDirty = false;
                for (int i = 0; i < slotsBuff.Count; i++) if (slotsBuff[i].IsEmpty) slotsBuff[i].PlaceBuff(i % 400 + 1);
                DoEvents(); DoEvents(); vm.IsDirty = false;
            }

            // ---------------------------------------------------------------------------------
            // Resize en caliente (s24): grande -> normal -> minimo -> grande sin perder pestaña,
            // idioma ni el estado de la Libreria de buffs.
            // ---------------------------------------------------------------------------------
            {
                FijarTamaño(window, 1920, 1080); vm.IsBuffLibraryCollapsed = false; IrABuffs();
                var slotElegido = slotsBuff.FirstOrDefault(s => s.IsNotEmpty);
                if (slotElegido != null) { vm.SelectBuffSlot(slotElegido); DoEvents(); }
                foreach (var (paso, w, h) in new[] { ("grande", 1920.0, 1080.0), ("normal", 1366.0, 768.0), ("minimo", 1080.0, 700.0), ("grande-vuelta", 1920.0, 1080.0) })
                {
                    FijarTamaño(window, w, h); DoEvents(); DoEvents();
                    bool ok = vm.PersonajeInnerTabIndex == TabBuffs && !vm.IsBuffLibraryCollapsed
                              && (slotElegido == null || ReferenceEquals(vm.BuffEdit.Slot, slotElegido));
                    Console.WriteLine($"PERSONAJE RESIZE {paso} {w:0}x{h:0}: pestaña={vm.PersonajeInnerTabIndex} libreriaBuffsPlegada={vm.IsBuffLibraryCollapsed} slotEditar={(slotElegido == null ? "(ninguno)" : ReferenceEquals(vm.BuffEdit.Slot, slotElegido) ? "igual" : "PERDIDO")}");
                    if (!ok) Fallo("RESIZE", $"en el paso '{paso}' ({w:0}x{h:0}) se perdio estado de Buffs");
                    MedirBuffs($"resize-{paso}");
                }
            }

            // ---------------------------------------------------------------------------------
            // Negative acceptance (s26/s28): el ScrollViewer propio de BuffContainerCompactTemplate
            // ya no puede existir por debajo del owner de la pagina.
            // ---------------------------------------------------------------------------------
            {
                FijarTamaño(window, 1366, 768); IrABuffs(); DoEvents();
                var svBuffs = Descendientes<ScrollViewer>(pagBuffs).Where(sv => !ReferenceEquals(sv, pagBuffs) && sv.TemplatedParent is not TextBoxBase).ToList();
                Console.WriteLine($"PERSONAJE VIEJO: ScrollViewer dentro de BuffsPaginaContenedor (visibles o no, sin contarse a si mismo) = {svBuffs.Count} (esperado 0; antes 1, el de BuffContainerCompactTemplate)");
                if (svBuffs.Count > 0)
                    Fallo("VIEJO", $"quedan {svBuffs.Count} ScrollViewer dentro de la pagina de Buffs - el scroll local viejo sigue por debajo del owner de la pagina");
            }

            Console.WriteLine("PERSONAJE RESUMEN Buffs:");
            foreach (var r in resumen) Console.WriteLine("  " + r);

            // ===================================================================================
            // Resto de Personaje (Apariencia/SpawnPoints/Desbloqueos/Version/Comparar) - medido de
            // verdad con el arnes (vía libre, 28-sep-2026). SpawnPoints/Desbloqueos/Version/Comparar:
            // UN solo ScrollViewer por pagina, con la cabecera/selectores/botones estructurales
            // fuera de el - Fallo() real si aparece scroll (contenido finito, s3/s22). Apariencia:
            // DOS ScrollViewer hermanos con proposito real distinto (P-1, preview fijo) - tras el
            // arreglo de esta ronda (Padding/margenes compactados, s14 paso 7) el residuo a 1080x700
            // es de 60,9px (columna preview) y 284,7px (columna editable, 7 selectores de color +
            // genero/peinado/tinte/estadisticas - contenido real, no espaciado sobrante) - se vigila
            // con un LIMITE (AVISO si crece, Fallo() si se DUPLICA) en vez de exigir 0, igual que el
            // permiso aa5f7395 de la FASE C/D.
            // ===================================================================================
            void MedirPaginaSimple(int tab, string nombre, double w, double h, string idioma)
            {
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = tab; DoEvents(); DoEvents(); WaitForDispatcher(80);
                var sv = Descendientes<ScrollViewer>(window).Where(s => s.IsVisible && s.TemplatedParent is not System.Windows.Controls.Primitives.TextBoxBase).FirstOrDefault();
                string cab = $"{nombre} {w:0}x{h:0} idioma={idioma}";
                Console.WriteLine($"PERSONAJE {cab}: vp={sv?.ViewportHeight:0.#} ext={sv?.ExtentHeight:0.#} scr={sv?.ScrollableHeight:0.#}");
                if (sv != null && sv.ScrollableHeight > 0.5)
                    Fallo(nombre.ToUpperInvariant(), $"{cab}: la pagina desplaza {sv.ScrollableHeight:0.#}px (ext {sv.ExtentHeight:0.#} en vp {sv.ViewportHeight:0.#}) - contenido finito, no deberia necesitar scroll (s3/s22)");
                if (w == 1080)
                {
                    string shot = Path.Combine(outDir, $"personaje-{etiqueta}-{nombre.ToLowerInvariant()}-{w:0}x{h:0}-{idioma}.png");
                    File.WriteAllBytes(shot, CapturarPng(window, window.ActualWidth, window.ActualHeight));
                }
            }

            // Comparar necesita dos personajes reales elegidos para tener resultados que medir.
            var dosPersonajes = vm.Compare.AvailableCharacters.Take(2).ToList();
            if (dosPersonajes.Count == 2) { vm.Compare.SelectedA = dosPersonajes[0]; vm.Compare.SelectedB = dosPersonajes[1]; DoEvents(); DoEvents(); }
            else Console.WriteLine($"PERSONAJE COMPARAR: INCONCLUSIVE - solo {dosPersonajes.Count} personaje(s) disponible(s) en Compare.AvailableCharacters, hacen falta 2");

            void MedirComparar(double w, double h, string idioma)
            {
                var compareView = window.FindName("CompareView") as FrameworkElement;
                var svCompare = compareView?.FindName("CompareResultsScrollViewer") as ScrollViewer;
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = TabComparar; DoEvents(); DoEvents(); WaitForDispatcher(80);
                string cab = $"Comparar {w:0}x{h:0} idioma={idioma}";
                Console.WriteLine($"PERSONAJE {cab}: ShowResults={vm.Compare.ShowResults} vp={svCompare?.ViewportHeight:0.#} ext={svCompare?.ExtentHeight:0.#} scr={svCompare?.ScrollableHeight:0.#}");
                if (vm.Compare.ShowResults && svCompare == null)
                    Fallo("COMPARAR", $"{cab}: ShowResults=True y no se encuentra CompareResultsScrollViewer");
                // Comparar es la familia UNBOUNDED_COLLECTION (resultados de la comparacion, s23) - su
                // propio scroll de RESULTADOS es legitimo y esperado, solo se informa (nunca Fallo()).
                if (w == 1080)
                {
                    string shot = Path.Combine(outDir, $"personaje-{etiqueta}-comparar-{w:0}x{h:0}-{idioma}.png");
                    File.WriteAllBytes(shot, CapturarPng(window, window.ActualWidth, window.ActualHeight));
                }
            }

            void MedirApariencia(double w, double h, string idioma)
            {
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = TabApariencia; DoEvents(); DoEvents(); WaitForDispatcher(80);
                var svs = Descendientes<ScrollViewer>(window).Where(s => s.IsVisible && s.TemplatedParent is not System.Windows.Controls.Primitives.TextBoxBase).ToList();
                for (int i = 0; i < svs.Count; i++)
                {
                    var s = svs[i];
                    string cab = $"Apariencia[{i}] {w:0}x{h:0} idioma={idioma}";
                    Console.WriteLine($"PERSONAJE {cab}: {Nombre(s)} vp={s.ViewportHeight:0.#} ext={s.ExtentHeight:0.#} scr={s.ScrollableHeight:0.#}");
                    // Limite del permiso (medido tras el arreglo de esta ronda, worst case 1080x700 ES):
                    // columna 0 (preview) 60,9px, columna 1 (editable) 284,7px. Fallo() si se DUPLICA -
                    // señal de una regresion real, no de una variacion de idioma/DPI menor.
                    double limite = i == 0 ? 130 : 580;
                    if (s.ScrollableHeight > limite)
                        Fallo("APARIENCIA", $"{cab}: {Nombre(s)} desplaza {s.ScrollableHeight:0.#}px, por encima del limite del permiso ({limite}px) - ver known-diff de Apariencia en el requirement");
                }
                if (w == 1080)
                {
                    string shot = Path.Combine(outDir, $"personaje-{etiqueta}-apariencia-{w:0}x{h:0}-{idioma}.png");
                    File.WriteAllBytes(shot, CapturarPng(window, window.ActualWidth, window.ActualHeight));
                }
            }

            foreach (var (w, h) in new[] { (1080.0, 700.0), (1366.0, 768.0), (1520.0, 860.0), (1920.0, 1080.0), (2560.0, 1440.0) })
            {
                FijarTamaño(window, w, h); DoEvents(); DoEvents();
                MedirApariencia(w, h, "es");
                MedirPaginaSimple(TabSpawnPoints, "SpawnPoints", w, h, "es");
                MedirPaginaSimple(TabDesbloqueos, "Desbloqueos", w, h, "es");
                MedirPaginaSimple(TabVersion, "Version", w, h, "es");
                MedirComparar(w, h, "es");
            }

            // --- EN (s25) en el minimo, con resize en caliente grande->minimo->grande (s24) ---
            vm.Settings.Language = "en"; DoEvents(); DoEvents();
            FijarTamaño(window, 1080, 700); DoEvents(); DoEvents();
            MedirApariencia(1080, 700, "en");
            MedirPaginaSimple(TabSpawnPoints, "SpawnPoints", 1080, 700, "en");
            MedirPaginaSimple(TabDesbloqueos, "Desbloqueos", 1080, 700, "en");
            MedirPaginaSimple(TabVersion, "Version", 1080, 700, "en");
            MedirComparar(1080, 700, "en");
            vm.Settings.Language = "es"; DoEvents(); DoEvents();

            {
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = TabComparar; DoEvents(); DoEvents();
                var selAAntes = vm.Compare.SelectedA; var selBAntes = vm.Compare.SelectedB;
                foreach (var (paso, w, h) in new[] { ("grande", 1920.0, 1080.0), ("minimo", 1080.0, 700.0), ("grande-vuelta", 1920.0, 1080.0) })
                {
                    FijarTamaño(window, w, h); DoEvents(); DoEvents();
                    bool ok = vm.PersonajeInnerTabIndex == TabComparar && ReferenceEquals(vm.Compare.SelectedA, selAAntes) && ReferenceEquals(vm.Compare.SelectedB, selBAntes);
                    Console.WriteLine($"PERSONAJE RESIZE-COMPARAR {paso} {w:0}x{h:0}: pestaña={vm.PersonajeInnerTabIndex} personajesIguales={ok}");
                    if (!ok) Fallo("RESIZE", $"Comparar en el paso '{paso}' ({w:0}x{h:0}) perdio la seleccion de personajes");
                }
            }

            vm.Settings.Language = idiomaOriginal;
            vm.IsBuffLibraryCollapsed = libreriaBuffsOriginal;
            vm.IsDirty = false;
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = TabObjetos;
            FijarTamaño(window, 1180, 860);
            DoEvents();
        }
        catch (Exception ex) { fallos++; Console.WriteLine("PERSONAJE-RESPONSIVE-EXCEPTION: " + ex); }
        Console.WriteLine($"PERSONAJE_RESPONSIVE_SOLO: {fallos} fallo(s)");
    }
}
