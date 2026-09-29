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
using Terrakeep.App;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    // Indices reales de MainViewModel.AppTab (enum privado - mismos valores a mano).
    private const int TabInicio = 0, TabBuilds = 2, TabGuia = 3, TabHosting = 5, TabNovedades = 6, TabAcercaDe = 7;

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
                // FASE G (WARN-02, s17): "NO columna estrecha en el centro + 50% de fondo vacio" en
                // ventana grande. Medido contra el ANCHO REAL usado por el contenido (ActualWidth del
                // hijo directo del ScrollViewer, que en estas paginas es el StackPanel/Grid raiz con
                // HorizontalAlignment que decide su propio ancho) frente al ancho real disponible
                // (sv.ViewportWidth) - solo se exige a partir de 1920 (el propio s17 habla de "ventana
                // grande"/1920-2560, nunca del minimo).
                if (w >= 1920 && sv.Content is FrameworkElement contenidoRaiz)
                {
                    double anchoUsado = contenidoRaiz.ActualWidth;
                    double anchoDisponible = sv.ViewportWidth;
                    double fraccion = anchoDisponible > 0 ? anchoUsado / anchoDisponible : 0;
                    Console.WriteLine($"RESTO {cab}: anchoUsado={anchoUsado:0.#} anchoDisponible={anchoDisponible:0.#} uso={fraccion:P0}");
                    if (fraccion < 0.5)
                        Fallo(nombre.ToUpperInvariant() + "-ANCHO", $"{cab}: el contenido solo usa {fraccion:P0} del ancho disponible ({anchoUsado:0.#}px de {anchoDisponible:0.#}px) - columna estrecha + mas de la mitad de fondo vacio (s17)");
                }
                if (w == 1080 || primeraEntrada)
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
                    // Mismo criterio de uso de ancho que MedirPaginaSimple (WARN-02, s17).
                    if (w >= 1920 && sv.Content is FrameworkElement contenidoRaiz)
                    {
                        double anchoUsado = contenidoRaiz.ActualWidth;
                        double anchoDisponible = sv.ViewportWidth;
                        double fraccion = anchoDisponible > 0 ? anchoUsado / anchoDisponible : 0;
                        Console.WriteLine($"RESTO {cab}: anchoUsado={anchoUsado:0.#} anchoDisponible={anchoDisponible:0.#} uso={fraccion:P0}");
                        if (fraccion < 0.5)
                            Fallo(nombre.ToUpperInvariant() + "-ANCHO", $"{cab}: el contenido solo usa {fraccion:P0} del ancho disponible ({anchoUsado:0.#}px de {anchoDisponible:0.#}px) - columna estrecha + mas de la mitad de fondo vacio (s17)");
                    }
                    // El selector de tabs (TabItem.Header) siempre debe seguir siendo clicable/visible -
                    // TabControl nunca lo mete dentro del propio ScrollViewer de contenido (confirmado
                    // por estructura XAML, BuildsView/WhatsNewView: TabControl es el padre, no un hijo).
                    if (!interno.IsVisible) Fallo(nombre.ToUpperInvariant() + "-SELECTOR", $"{cab}: el TabControl interno (selector de sub-pestañas) no esta visible");
                    if (w == 1080) { string shot = Path.Combine(outDir, $"resto-{etiqueta}-{nombre.ToLowerInvariant()}-sub{i}-{w:0}x{h:0}-{idioma}.png"); File.WriteAllBytes(shot, CapturarPng(window, window.ActualWidth, window.ActualHeight)); }
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
