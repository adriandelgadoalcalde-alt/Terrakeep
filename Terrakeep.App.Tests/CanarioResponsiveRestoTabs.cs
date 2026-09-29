// CANARIO REAL - FASE F del responsive global (PDF "Arreglo familia keep", bloque 2 "TERRAKEEP -
// RESPONSIVE GLOBAL, PAGINACION Y SCROLL COMO ULTIMO RECURSO", secciones 3, 13-19, 22-26, 28-29 y
// 34). 29-sep-2026, aplicador-fix-responsive-faseF.
//
// RESTO_RESPONSIVE_SOLO=1 mide las 6 pestañas de primer nivel que NO son Personaje ni Exploracion
// (esas dos tienen su propia familia de canarios ya real: PERSONAJE_RESPONSIVE_SOLO y
// COFRES_INSPECTOR_SOLO/EXPLORATION_LAYOUT_SOLO/CanarioFlujoCompletoCofres.cs - este archivo no las
// repite, serian mecanismos paralelos, s28/s29): Inicio, Builds, Guia, Hosting, Novedades, Acerca de.
//
// Por lectura de codigo (bitacora.md, entrada FASE F) las 6 ya cumplian el contrato ANTES de esta
// ronda (un solo ScrollViewer por pagina/subvista, cabecera-selectores-botones estructurales FUERA
// de el) - este canario existe para MEDIR de verdad con el arnes (no solo lectura, s27: "no basta
// que no crashee, capturar geometria real") y para que una regresion futura no pueda volver sin que
// el arnes la cace. Todas las medidas son HARD FAIL si aparece scroll de pagina (contenido finito,
// s3 STRUCTURAL_FINITE/s22) salvo donde el propio encargo permite scroll de resultados
// (Builds/Novedades: tarjetas dentro de un TabItem, familia FINITE_PAGEABLE con posible scroll
// real de contenido largo - se informa la geometria, solo Fallo() si hay overlap/clipping/scroll
// horizontal o si el selector/pestañas quedan inalcanzables).
//
// Sin aislamiento de personaje (estas 6 pestañas no tocan ningun .plr/.tplr/.wld real).
// Capturas en RESTO_RESPONSIVE_EVIDENCIA (si se define) o en <bin>/keepqa-evidencia/responsive-faseF.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Shape = System.Windows.Shapes.Shape;
using Terrakeep.App;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    // Indices reales de MainViewModel.AppTab (enum privado - mismos valores a mano).
    private const int TabInicio = 0, TabBuilds = 2, TabGuia = 3, TabHosting = 5, TabNovedades = 6, TabAcercaDe = 7;

    // FASE G del responsive global (correccion del coordinador, 29-sep-2026, tras el checkpoint
    // parcial): medir ActualWidth del hijo directo del ScrollViewer NO basta - un StackPanel
    // vertical se auto-dimensiona por el hijo MAS ANCHO (aunque ese hijo este mas abajo, fuera del
    // viewport actual sin hacer scroll: WPF sigue midiendolo, no hay virtualizacion en un
    // ItemsControl normal). AcercaDe lo demostro: el ActualWidth del StackPanel llegaba a 1699,8px
    // (99% de uso) porque el UniformGrid del Changelog (mas abajo del todo) fuerza ese ancho, pero a
    // 1920x1080 el Changelog esta FUERA del viewport visible (confirmado con las capturas reales del
    // revisor, resto-actual-acercade-1920x1080-es.png: la pagina corta justo antes del Changelog) -
    // el usuario ve solo el texto corrido de ~680px con >900px de fondo vacio a la derecha, pese a
    // que la metrica anterior decia "99% de uso". Metrica correcta: la UNION de los bounds (en
    // coordenadas de VENTANA) de los descendientes "hoja" visibles del ScrollViewer (TextBlock/
    // Border/Image/TextBox/Slider/Shape/ButtonBase - contenido real, nunca paneles de layout como
    // Grid/StackPanel/WrapPanel/UniformGrid/ItemsControl, que solo medirian el contenedor otra vez),
    // INTERSECADA con el rectangulo real del viewport del ScrollViewer (asi que un elemento mas abajo
    // sin hacer scroll, aunque exista en el arbol visual, no cuenta - no esta VISIBLE de verdad).
    private static bool EsContenidoVisibleHoja(FrameworkElement el) =>
        el is TextBlock || el is Border || el is Image || el is TextBox || el is Slider || el is Shape || el is ButtonBase;

    private static double MedirAnchoContenidoVisible(MainWindow window, ScrollViewer sv)
    {
        Rect viewportEnVentana;
        // OJO real (hallazgo propio, corregido antes de sacar ninguna conclusion): sv.ActualWidth
        // INCLUYE el ancho de la barra de scroll vertical cuando esta visible (~17px), pero
        // sv.ViewportWidth (con la que se compara la fraccion mas abajo) NO la incluye - usar
        // ActualWidth aqui daba un rectangulo de viewport mas ancho que el real, y la interseccion
        // con el podia superar el 100% de uso (medido: Guia/AcercaDe daban 101% en vez de un numero
        // real). sv.ViewportWidth/ViewportHeight son el tamaño real del area de contenido visible.
        try { viewportEnVentana = sv.TransformToAncestor(window).TransformBounds(new Rect(0, 0, sv.ViewportWidth, sv.ViewportHeight)); }
        catch (InvalidOperationException) { return 0; }

        double minX = double.PositiveInfinity, maxX = double.NegativeInfinity;
        foreach (var el in Descendientes<FrameworkElement>(sv))
        {
            if (!EsContenidoVisibleHoja(el) || !el.IsVisible || el.ActualWidth <= 0 || el.ActualHeight <= 0) continue;
            Rect rectoEnVentana;
            try { rectoEnVentana = el.TransformToAncestor(window).TransformBounds(new Rect(0, 0, el.ActualWidth, el.ActualHeight)); }
            catch (InvalidOperationException) { continue; }
            var interseccion = Rect.Intersect(rectoEnVentana, viewportEnVentana);
            if (interseccion.IsEmpty || interseccion.Width <= 0.5) continue;
            if (interseccion.Left < minX) minX = interseccion.Left;
            if (interseccion.Right > maxX) maxX = interseccion.Right;
        }
        return double.IsInfinity(minX) ? 0 : maxX - minX;
    }

    // REGRESION REAL (verificador QA final, 29-sep-2026, tras el cierre en verde de WARN-02):
    // GUIA/ACERCADE (y en teoria HOSTING/NOVEDADES por el mismo mecanismo) mostraban la columna
    // derecha CORTADA por el borde derecho a 1080x700, sin scroll para alcanzarla - RESTO_RESPONSIVE_
    // SOLO no lo cazaba porque solo media SCROLL VERTICAL (s3/s22) y USO DE ANCHO (s17, solo >=1920),
    // nunca RECORTE HORIZONTAL propiamente dicho. Causa real del bug de produccion: Grid.Row/
    // Grid.Column/Margin puestos como atributos LOCALES ademas de en un Style con DataTrigger -en
    // WPF un valor local siempre gana sobre cualquier Setter de Style- dejaba la columna derecha FIJA
    // en Grid.Column=1 sin importar IsDetailSideBySide (ver el comentario real de GuideView.xaml).
    // Esta funcion reutiliza el oraculo YA REAL de la familia (RectCompleto/ZonaVisible/
    // AlcanzableConScroll/QuienRecorta/Describir, D1 de AuditoriaMaquetacion.cs - MISMA clase
    // parcial Program, sin duplicar logica ni tocar ese archivo) pero acotado al ScrollViewer de
    // CADA pagina (mas rapido que el barrido de toda la ventana de AR14_SOLO, y corre SIEMPRE junto
    // al resto de RESTO_RESPONSIVE_SOLO, en TODOS los tamaños - el recorte de este bug se dio
    // justo en el minimo, 1080x700, no en los tamaños grandes que ya cubria ANCHO).
    private static void MedirClipHorizontal(MainWindow window, ScrollViewer sv, string nombre, string cab, Action<string, string> fallo)
    {
        foreach (var fe in Descendientes<FrameworkElement>(sv))
        {
            if (fe is not (TextBlock or Border or ButtonBase)) continue;
            if (!fe.IsVisible || fe.ActualWidth < 1 || fe.ActualHeight < 1) continue;
            if (fe is TextBlock tbVacio && string.IsNullOrWhiteSpace(tbVacio.Text)) continue;
            Rect completo, zona;
            try { completo = RectCompleto(fe, window); zona = ZonaVisible(fe, window); }
            catch (InvalidOperationException) { continue; }
            double faltaX;
            if (zona.IsEmpty)
            {
                if (AlcanzableConScroll(fe, window, true) || AlcanzableConScroll(fe, window, false)) continue;
                faltaX = completo.Width;
            }
            else
            {
                faltaX = Math.Min(completo.Width, Math.Max(0, zona.Left - completo.Left) + Math.Max(0, completo.Right - zona.Right));
            }
            if (faltaX <= 1) continue;
            if (AlcanzableConScroll(fe, window, horizontal: true)) continue;
            fallo(nombre.ToUpperInvariant() + "-CLIP", $"{cab}: {Describir(fe)} pierde {faltaX:0.#}px por recorte horizontal sin scroll para alcanzarlo [recorta: {QuienRecorta(fe, window)}]");
        }
    }

    private static void EjecutarRestoResponsiveSolo(MainWindow window, MainViewModel vm)
    {
        int fallos = 0;
        void Fallo(string codigo, string msg) { fallos++; Console.WriteLine($"FALLO: RESTO_RESPONSIVE_SOLO-{codigo} - {msg}"); }

        try
        {
            string outDir = Environment.GetEnvironmentVariable("RESTO_RESPONSIVE_EVIDENCIA")
                            ?? Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia", "responsive-faseF");
            Directory.CreateDirectory(outDir);
            string etiqueta = Environment.GetEnvironmentVariable("RESTO_RESPONSIVE_ETIQUETA") ?? "actual";

            var tamaños = new (string id, double w, double h)[]
            {
                ("min-1080x700", 1080, 700),
                // Intermedios de §2 que el verificador QA final (29-sep-2026) señalo sin medir en
                // toda la app - añadidos aqui a peticion explicita ("Añade esos tamaños a RESTO y
                // PERSONAJE"). Son justo la zona donde vivia la regresion del recorte horizontal
                // (1080-1320px de ancho, Compacto/Normal - por debajo de AmplioMinWidth=1520, donde
                // IsDetailSideBySide debe seguir en False).
                ("fino-1100x720", 1100, 720),
                ("fino-1120x740", 1120, 740),
                ("fino-1180x760", 1180, 760),
                ("fino-1180x800", 1180, 800),
                ("fino-1200x800", 1200, 800),
                ("fino-1280x800", 1280, 800),
                ("fino-1280x900", 1280, 900),
                // Hallazgo propio (29-sep-2026, investigando la regresion de arriba con AR-LAY):
                // mismo ancho que el minimo (1080, Compacto), pero mucho mas alto - expone una
                // regresion REAL que ninguno de los tamaños anteriores cazaba (todos comparten
                // altura <=900): a 1080x1440 el Changelog de AcercaDe (mucho mas contenido
                // realizado que a 700 de alto) media hasta 3276px de ancho sin su propio MaxWidth
                // (ver MainViewModel.ChangelogMaxWidth y el comentario real de AboutView.xaml) -
                // 43 casos reales en AR-LAY antes de este arreglo, 0 despues. Se queda en el
                // barrido para que no pueda volver sin que el arnes lo cace.
                ("altura-1080x1440", 1080, 1440),
                ("medio-1366x768", 1366, 768),
                ("amplio-1520x860", 1520, 860),
                ("grande-1920x1080", 1920, 1080),
                ("extra-2560x1440", 2560, 1440),
            };

            string idiomaOriginal = vm.Settings.Language;
            int tabOriginal = vm.SelectedTabIndex;

            // ---------------------------------------------------------------------------------
            // Paginas de UN solo ScrollViewer. HARD FAIL si desplazan para Hosting (contenido
            // puramente finito, s3/s22 - cabecera/estado/acciones nunca dentro de un scroll de
            // descubrimiento). Para Inicio/Guia/AcercaDe el mismo ScrollViewer aloja TAMBIEN
            // contenido real legitimamente largo (Inicio: Home.Characters, lista real que crece
            // con los .plr del usuario, comparte pagina con las tarjetas de navegacion - mismo
            // criterio ya aceptado para Comparar en PERSONAJE_RESPONSIVE_SOLO, "resultados de
            // Comparar" s23; Guia: el objetivo actual completo, instructivo/documental por
            // naturaleza; AcercaDe: Changelog.Entries, changelog completo de toda la app - EXPLICITO
            // en s34 como excepcion real de scroll permitido, "changelogs extensos, documentos") -
            // medido con contenidoDocumentalLegitimo=true SOLO informa (nunca Fallo() por el total
            // de la pagina), pero sigue vigilando clipping/overlap/scroll horizontal/ScrollViewer
            // competidores de verdad.
            // ---------------------------------------------------------------------------------
            void MedirPaginaSimple(int tab, string nombre, double w, double h, string idioma, bool primeraEntrada = false, bool contenidoDocumentalLegitimo = false)
            {
                vm.SelectedTabIndex = tab; DoEvents(); DoEvents(); WaitForDispatcher(80);
                var svs = Descendientes<ScrollViewer>(window).Where(s => s.IsVisible && s.TemplatedParent is not TextBoxBase).ToList();
                string cab = $"{nombre} {w:0}x{h:0} idioma={idioma}{(primeraEntrada ? " primeraEntrada" : "")}";
                if (svs.Count != 1)
                {
                    Console.WriteLine($"RESTO {cab}: {svs.Count} ScrollViewer visibles (esperado 1 - el owner de la pagina)");
                    if (svs.Count == 0) { Fallo(nombre.ToUpperInvariant(), $"{cab}: no se encuentra ningun ScrollViewer - ¿la pagina cambio de estructura?"); return; }
                    if (svs.Count > 1) Fallo(nombre.ToUpperInvariant() + "-ANIDADO", $"{cab}: {svs.Count} ScrollViewer compitiendo a la vez (s13/s26-H, se esperaba 1 unico owner)");
                }
                var sv = svs[0];
                Console.WriteLine($"RESTO {cab}: vp={sv.ViewportHeight:0.#} ext={sv.ExtentHeight:0.#} scr={sv.ScrollableHeight:0.#}{(contenidoDocumentalLegitimo ? " (contenido documental/lista real, solo se informa)" : "")}");
                if (sv.ScrollableHeight > 0.5 && !contenidoDocumentalLegitimo)
                    Fallo(nombre.ToUpperInvariant(), $"{cab}: la pagina desplaza {sv.ScrollableHeight:0.#}px (ext {sv.ExtentHeight:0.#} en vp {sv.ViewportHeight:0.#}) - contenido finito, no deberia necesitar scroll (s3/s22)");
                if (sv.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled && sv.ExtentWidth > sv.ViewportWidth + 0.5)
                    Fallo(nombre.ToUpperInvariant() + "-HSCROLL", $"{cab}: overflow horizontal real ({sv.ExtentWidth:0.#}px en {sv.ViewportWidth:0.#}px) con la barra deshabilitada (s26 G)");
                // Regresion real (verificador QA final): recorte horizontal SIN escape de scroll -
                // en TODOS los tamaños, no solo >=1920 (ver el comentario real de MedirClipHorizontal).
                MedirClipHorizontal(window, sv, nombre, cab, Fallo);
                // FASE G (WARN-02, s17): "NO columna estrecha en el centro + 50% de fondo vacio" en
                // ventana grande. Medido contra el ANCHO REAL VISIBLE (union de bounds de los
                // descendientes hoja visibles DENTRO del viewport actual del ScrollViewer, ver
                // MedirAnchoContenidoVisible mas arriba - NO el ActualWidth del contenedor, que se
                // auto-dimensiona por el hijo mas ancho aunque este fuera del viewport sin hacer
                // scroll, falso positivo real medido en AcercaDe) frente al ancho real disponible
                // (sv.ViewportWidth) - solo se exige a partir de 1920 (el propio s17 habla de "ventana
                // grande"/1920-2560, nunca del minimo).
                // HALLAZGO PROPIO (segunda vuelta, corregido antes de fiarse del numero): el barrido
                // de tamaños NUNCA vuelve a poner el ScrollViewer en offset=0 entre una medida y la
                // siguiente - a un tamaño mas pequeño (1366/1520) el usuario real tuvo que bajar para
                // ver el Changelog, y ese offset queda "pegado" cuando la ventana crece a 1920/2560,
                // colando el Changelog en el viewport aunque una visita FRESCA a ese tamaño (offset=0,
                // justo lo que muestran las capturas reales del revisor) no lo veria. ScrollToTop()
                // fuerza la misma foto que ve el usuario real la primera vez que abre la pagina a este
                // tamaño - s22, "de un plumazo".
                if (w >= 1920)
                {
                    sv.ScrollToVerticalOffset(0); DoEvents(); DoEvents(); WaitForDispatcher(30);
                    double anchoUsado = MedirAnchoContenidoVisible(window, sv);
                    double anchoDisponible = sv.ViewportWidth;
                    double fraccion = anchoDisponible > 0 ? anchoUsado / anchoDisponible : 0;
                    Console.WriteLine($"RESTO {cab}: anchoContenidoVisible={anchoUsado:0.#} anchoDisponible={anchoDisponible:0.#} uso={fraccion:P0}");
                    if (fraccion < 0.5)
                        Fallo(nombre.ToUpperInvariant() + "-ANCHO", $"{cab}: el contenido VISIBLE solo usa {fraccion:P0} del ancho disponible ({anchoUsado:0.#}px de {anchoDisponible:0.#}px) - columna estrecha + mas de la mitad de fondo vacio (s17)");
                }
                // FASE G (s32/docs evidencia): tambien 1920x1080 y 2560x1440, no solo el minimo -
                // criterio explicito del cierre "capturas antes y despues a 1080x700/1920x1080/
                // 2560x1440" y para poder JUZGAR CON LOS OJOS (no solo con el numero de anchoUsado)
                // el resultado real de WARN-02 en las 6 vistas.
                if (w == 1080 || w == 1920 || w == 2560 || primeraEntrada)
                {
                    string shot = Path.Combine(outDir, $"resto-{etiqueta}-{nombre.ToLowerInvariant()}-{w:0}x{h:0}-{idioma}{(primeraEntrada ? "-primeraentrada" : "")}.png");
                    File.WriteAllBytes(shot, CapturarPng(window, window.ActualWidth, window.ActualHeight));
                }
            }

            // ---------------------------------------------------------------------------------
            // Builds/Novedades: TabControl interno con 2 TabItem, cada uno con su propio
            // ScrollViewer de tarjetas (FINITE_PAGEABLE, subpagina real - no es scroll anidado
            // porque WPF solo mantiene en el arbol visual el contenido del TabItem SELECCIONADO).
            // Se recorren los 2 sub-tabs explicitamente; el selector/pestañas de arriba deben
            // seguir visibles y alcanzables sin scroll en cualquier tamaño.
            // ---------------------------------------------------------------------------------
            void MedirTabControlInterno(int tab, string nombre, double w, double h, string idioma)
            {
                vm.SelectedTabIndex = tab; DoEvents(); DoEvents(); WaitForDispatcher(80);
                var interno = Descendientes<TabControl>(window).FirstOrDefault(t => t.Name != "RootTabControl");
                if (interno == null) { Fallo(nombre.ToUpperInvariant(), $"{nombre} {w:0}x{h:0}: no se encuentra el TabControl interno"); return; }
                int subTabsOriginal = interno.SelectedIndex;
                for (int i = 0; i < interno.Items.Count; i++)
                {
                    interno.SelectedIndex = i; DoEvents(); DoEvents(); WaitForDispatcher(80);
                    var svs = Descendientes<ScrollViewer>(window).Where(s => s.IsVisible && s.TemplatedParent is not TextBoxBase).ToList();
                    string cab = $"{nombre}[{i}] {w:0}x{h:0} idioma={idioma}";
                    if (svs.Count != 1)
                    {
                        Console.WriteLine($"RESTO {cab}: {svs.Count} ScrollViewer visibles (esperado 1)");
                        if (svs.Count > 1) Fallo(nombre.ToUpperInvariant() + "-ANIDADO", $"{cab}: {svs.Count} ScrollViewer compitiendo a la vez (s13/s26-H)");
                        if (svs.Count == 0) continue;
                    }
                    var sv = svs[0];
                    Console.WriteLine($"RESTO {cab}: vp={sv.ViewportHeight:0.#} ext={sv.ExtentHeight:0.#} scr={sv.ScrollableHeight:0.#} (scroll de resultados FINITE_PAGEABLE, solo se informa)");
                    if (sv.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled && sv.ExtentWidth > sv.ViewportWidth + 0.5)
                        Fallo(nombre.ToUpperInvariant() + "-HSCROLL", $"{cab}: overflow horizontal real ({sv.ExtentWidth:0.#}px en {sv.ViewportWidth:0.#}px)");
                    // Regresion real (verificador QA final): mismo chequeo de recorte horizontal que
                    // MedirPaginaSimple, en TODOS los tamaños.
                    MedirClipHorizontal(window, sv, nombre, cab, Fallo);
                    // Mismo criterio de uso de ancho VISIBLE que MedirPaginaSimple (WARN-02, s17),
                    // incluido el ScrollToTop() (ver su comentario real arriba).
                    if (w >= 1920)
                    {
                        sv.ScrollToVerticalOffset(0); DoEvents(); DoEvents(); WaitForDispatcher(30);
                        double anchoUsado = MedirAnchoContenidoVisible(window, sv);
                        double anchoDisponible = sv.ViewportWidth;
                        double fraccion = anchoDisponible > 0 ? anchoUsado / anchoDisponible : 0;
                        Console.WriteLine($"RESTO {cab}: anchoContenidoVisible={anchoUsado:0.#} anchoDisponible={anchoDisponible:0.#} uso={fraccion:P0}");
                        if (fraccion < 0.5)
                            Fallo(nombre.ToUpperInvariant() + "-ANCHO", $"{cab}: el contenido VISIBLE solo usa {fraccion:P0} del ancho disponible ({anchoUsado:0.#}px de {anchoDisponible:0.#}px) - columna estrecha + mas de la mitad de fondo vacio (s17)");
                    }
                    // El selector de tabs (TabItem.Header) siempre debe seguir siendo clicable/visible -
                    // TabControl nunca lo mete dentro del propio ScrollViewer de contenido (confirmado
                    // por estructura XAML, BuildsView/WhatsNewView: TabControl es el padre, no un hijo).
                    if (!interno.IsVisible) Fallo(nombre.ToUpperInvariant() + "-SELECTOR", $"{cab}: el TabControl interno (selector de sub-pestañas) no esta visible");
                    if (w == 1080 || w == 1920 || w == 2560) { string shot = Path.Combine(outDir, $"resto-{etiqueta}-{nombre.ToLowerInvariant()}-sub{i}-{w:0}x{h:0}-{idioma}.png"); File.WriteAllBytes(shot, CapturarPng(window, window.ActualWidth, window.ActualHeight)); }
                }
                interno.SelectedIndex = subTabsOriginal; DoEvents(); DoEvents();
            }

            // ---------------------------------------------------------------------------------
            // Inicio: ademas del scroll de pagina, las tarjetas de navegacion (WrapPanel) deben
            // seguir siendo alcanzables/legibles sin overlap ni clipping en cualquier tamaño (s16:
            // "ninguna funcion debe aparecer solo porque la ventana sea mas grande").
            // ---------------------------------------------------------------------------------
            void MedirInicio(double w, double h, string idioma, bool primeraEntrada = false)
            {
                MedirPaginaSimple(TabInicio, "Inicio", w, h, idioma, primeraEntrada, contenidoDocumentalLegitimo: true);
                // Style x:Key no da un nombre legible en tiempo de ejecucion (WPF compilado) - se
                // cuentan las tarjetas reales por su geometria propia (Width=270/MinHeight=108,
                // unicas en toda la ventana con esa combinacion, HomeView.xaml:534-579).
                var tarjetasReales = Descendientes<ButtonBase>(window).Where(b => b.IsVisible && b.Width == 270 && b.MinHeight == 108).ToList();
                Console.WriteLine($"RESTO Inicio {w:0}x{h:0} idioma={idioma}: tarjetas de navegacion visibles={tarjetasReales.Count} (esperado >=3, Libreria/Builds/Exploracion siempre presentes)");
                if (tarjetasReales.Count < 3)
                    Fallo("INICIO-TARJETAS", $"Inicio {w:0}x{h:0} idioma={idioma}: solo {tarjetasReales.Count} tarjetas de navegacion visibles, se esperaban al menos 3 (Libreria/Builds/Exploracion siempre presentes)");
            }

            // ===================================================================================
            // Primera entrada (VM recien arrancado, sin haber visitado la pestaña antes) en el
            // minimo real, ES - s22 "de un plumazo".
            // ===================================================================================
            vm.Settings.Language = "es"; DoEvents();
            FijarTamaño(window, 1080, 700); DoEvents(); DoEvents();
            MedirInicio(1080, 700, "es", primeraEntrada: true);
            MedirPaginaSimple(TabGuia, "Guia", 1080, 700, "es", primeraEntrada: true, contenidoDocumentalLegitimo: true);
            MedirPaginaSimple(TabHosting, "Hosting", 1080, 700, "es", primeraEntrada: true);
            MedirPaginaSimple(TabAcercaDe, "AcercaDe", 1080, 700, "es", primeraEntrada: true, contenidoDocumentalLegitimo: true);
            MedirTabControlInterno(TabBuilds, "Builds", 1080, 700, "es");
            MedirTabControlInterno(TabNovedades, "Novedades", 1080, 700, "es");

            // ===================================================================================
            // Tamaños del encargo (§18/§2, ES) - barrido completo.
            // ===================================================================================
            foreach (var (id, w, h) in tamaños)
            {
                FijarTamaño(window, w, h); DoEvents(); DoEvents();
                MedirInicio(w, h, "es");
                MedirPaginaSimple(TabGuia, "Guia", w, h, "es", contenidoDocumentalLegitimo: true);
                MedirPaginaSimple(TabHosting, "Hosting", w, h, "es");
                MedirPaginaSimple(TabAcercaDe, "AcercaDe", w, h, "es", contenidoDocumentalLegitimo: true);
                MedirTabControlInterno(TabBuilds, "Builds", w, h, "es");
                MedirTabControlInterno(TabNovedades, "Novedades", w, h, "es");
            }

            // --- EN (s25) en el minimo ---
            vm.Settings.Language = "en"; DoEvents(); DoEvents();
            FijarTamaño(window, 1080, 700); DoEvents(); DoEvents();
            MedirInicio(1080, 700, "en");
            MedirPaginaSimple(TabGuia, "Guia", 1080, 700, "en", contenidoDocumentalLegitimo: true);
            MedirPaginaSimple(TabHosting, "Hosting", 1080, 700, "en");
            MedirPaginaSimple(TabAcercaDe, "AcercaDe", 1080, 700, "en", contenidoDocumentalLegitimo: true);
            MedirTabControlInterno(TabBuilds, "Builds", 1080, 700, "en");
            MedirTabControlInterno(TabNovedades, "Novedades", 1080, 700, "en");
            vm.Settings.Language = "es"; DoEvents(); DoEvents();

            // ---------------------------------------------------------------------------------
            // Resize en caliente (s24): grande -> normal -> minimo -> grande sin perder pestaña ni
            // idioma. Se prueba con Inicio (representa a las 6, mismo mecanismo de ScrollViewer
            // unico en todas).
            // ---------------------------------------------------------------------------------
            {
                FijarTamaño(window, 1920, 1080); vm.SelectedTabIndex = TabInicio; DoEvents(); DoEvents();
                foreach (var (paso, w, h) in new[] { ("grande", 1920.0, 1080.0), ("normal", 1366.0, 768.0), ("minimo", 1080.0, 700.0), ("grande-vuelta", 1920.0, 1080.0) })
                {
                    FijarTamaño(window, w, h); DoEvents(); DoEvents();
                    bool ok = vm.SelectedTabIndex == TabInicio && vm.Settings.Language == "es";
                    Console.WriteLine($"RESTO RESIZE {paso} {w:0}x{h:0}: pestaña={vm.SelectedTabIndex} idioma={vm.Settings.Language}");
                    if (!ok) Fallo("RESIZE", $"en el paso '{paso}' ({w:0}x{h:0}) se perdio la pestaña/idioma");
                    MedirInicio(w, h, "es");
                }
            }

            vm.Settings.Language = idiomaOriginal;
            vm.SelectedTabIndex = tabOriginal;
            FijarTamaño(window, 1180, 860);
            DoEvents();
        }
        catch (Exception ex) { fallos++; Console.WriteLine("RESTO-RESPONSIVE-EXCEPTION: " + ex); }
        Console.WriteLine($"RESTO_RESPONSIVE_SOLO: {fallos} fallo(s)");
    }
}
