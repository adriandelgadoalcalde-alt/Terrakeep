// AR-LAY: BARRIDO SISTEMATICO DE MAQUETACION POR TAMAÑO DE VENTANA E IDIOMA.
//
// Vive en su propio fichero, y no dentro del Main() de Program.cs, por una razon real y medida:
// Program.cs es un fichero de 5700 lineas que TODAS las sesiones que trabajan sobre este repo
// tocan a la vez - el 6-sep-2026, dos rondas en paralelo se pisaron ahi mismo (dos bloques
// distintos llamados "AR-16", y un renombrado por numero que quedo desalineado y dejo el
// proyecto sin compilar para todos). Como clase parcial de Program, este barrido sigue viendo
// TODOS sus helpers (RectVisible, TextoRecortado, Descendientes, FijarTamaño, DoEvents...) sin
// duplicar ni una linea, y la unica huella que deja en Program.cs es la llamada.
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Terrakeep.App;
using Terrakeep.App.Controls;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    // ====================================================================================
    // AR-LAY (6-sep-2026): BARRIDO SISTEMATICO DE MAQUETACION POR TAMAÑO DE VENTANA E IDIOMA
    // ====================================================================================
    // Por que existe: esta semana han aparecido TRES bugs de maquetacion reales (Exploracion/
    // Cofres, "NPCs que faltan", Equipamiento/accesorios) con exactamente la misma forma - no
    // se ven ni al tamaño MINIMO de la ventana ni MAXIMIZADA, solo en una franja de tamaños
    // INTERMEDIOS (en el caso de los accesorios, una franja de 14px de ancho). Los tres se
    // encontraron por casualidad o porque el usuario los reporto jugando, y los tres se
    // arreglaron con una comprobacion hecha a mano SOLO para su zona (AR-13a, AR-15, AR-14).
    //
    // Esto es la generalizacion de esas tres: en vez de ir por area funcional, va por TAMAÑO
    // e IDIOMA, y recorre la superficie ENTERA de la app (las 6 pestañas raiz, las 7 internas
    // de Personaje, las 3 de Objetos y las hojas de los TabControl anidados de Builds y
    // Novedades) en los dos idiomas. No mira nada "a ojo": mide con coordenadas reales.
    //
    // Los tres detectores, cada uno modelado sobre un bug REAL ya encontrado:
    //
    //  D1 - CONTENIDO PERDIDO. Para cada texto/boton visible compara su rectangulo COMPLETO
    //       con el que de verdad se esta pintando (RectVisible: interseccion del clip de
    //       TODOS los ancestros, el helper que ya hizo falta para "NPCs que faltan" - un
    //       elemento recortado por un ANCESTRO no lleva clip propio, asi que
    //       VisualTreeHelper.GetClip sobre el da null y miente). Si falta trozo Y no hay
    //       ScrollViewer ancestro que pueda desplazarse en ESE eje, es contenido perdido de
    //       verdad, no "hay que hacer scroll". Un ScrollViewer con el eje en Disabled NO
    //       cuenta como escape: es justo el mecanismo que cortaba los accesorios (AR-14).
    //  D2 - SOLAPE ENTRE CELDAS DISJUNTAS DE UN Grid. Dos hijos directos de un mismo Grid
    //       cuyos rangos de columna (o de fila) NO se tocan no pueden pisarse nunca: si sus
    //       rectangulos reales se cruzan, uno se ha desbordado de su celda. Es exactamente
    //       la forma del bug de "los accesorios se solapan con las monedas". Los hijos que
    //       SI comparten celda a proposito (overlays, capas superpuestas con RowSpan) se
    //       descartan solos por definicion, sin lista de excepciones que mantener.
    //  D3 - TEXTO TRUNCADO (informativo). TextTrimming real medido comparando el ancho de
    //       los glifos con el de la caja (helper TextoRecortado, de AR-13a). No es FALLO:
    //       un TextTrimming puede ser una decision honesta. Se cuenta y se lista el peor
    //       caso por pantalla para poder revisarlo.
    //
    // Capas: la lista corta de tamaños corre SIEMPRE (cada `dotnet run`). El barrido fino de
    // anchos, mucho mas caro, vive detras de AR_LAY_FINO=<paso en px> - es el que caza franjas
    // estrechas como la de 14px de AR-14. AR_LAY_SOLO=<subcadena> limita las pantallas.
    //
    // Se llama desde el Main() del arnes, al final del recorrido: con personaje y mundo reales ya
    // cargados, porque sin datos de verdad media app se queda vacia y no habria nada que medir.
    private static void BarridoMaquetacionPorTamañoEIdioma(Window window, MainViewModel vm)
    {
    try
    {
        // CANARIO (16-sep-2026, KeepQA/v4/I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md, hallazgo H6): antes
        // de fiarse de ninguna de las combinaciones reales de abajo, se comprueba que D1/D2 SEPAN
        // detectar. Ver ComprobarCanarioArLay para el porqué completo - en corto: este bloque
        // entero esta envuelto en el catch(Exception) de la linea de cierre, y el gate real de
        // todo este arnes es "grep FALLO: en consola" (Environment.Exit(0) siempre, Program.cs) -
        // si algo revienta A MEDIA auditoria, ni la linea de resumen ni ningun FALLO: se imprimen
        // NUNCA, indistinguible de una pasada limpia para quien solo mira si aparecio "FALLO:".
        if (!ComprobarCanarioArLay())
        {
            Console.WriteLine("FALLO: AR-LAY - el canario del propio detector no paso (ver AR-LAY-CANARIO arriba); abortando el barrido real, no seria de fiar.");
            return;
        }

        string idiomaPrevio = vm.Settings.Language;
        int tabPrevio = vm.SelectedTabIndex, innerPrevio = vm.PersonajeInnerTabIndex, subPrevio = vm.ObjetosSubTabIndex;
        double anchoPrevio = window.ActualWidth, altoPrevio = window.ActualHeight;
    
        var pantallas = ConstruirPantallasMaquetacion(window, vm);
        string? soloPantalla = Environment.GetEnvironmentVariable("AR_LAY_SOLO");
        if (!string.IsNullOrWhiteSpace(soloPantalla))
            pantallas = pantallas.Where(p => p.nombre.Contains(soloPantalla, StringComparison.OrdinalIgnoreCase)).ToList();
    
        // Tamaños reales, elegidos para caer a los DOS lados de cada umbral conocido de
        // WindowSizeClass (1320 Normal, 1520 Amplio) y en los altos que la app se encuentra de
        // verdad: 700 es el suelo real (MinHeight), 860 el de arranque, 1440 una pantalla 2K.
        (double w, double h)[] tamaños =
        [
            (1080, 700),   // el suelo real declarado: MinWidth x MinHeight
            (1080, 1440),  // minimo de ancho con alto de sobra (aisla los bugs de ANCHO)
            (1180, 860),   // el tamaño con el que arranca la app
            (1280, 720),   // HD, muy comun en portatil
            (1319, 800),   // 1px por debajo del umbral de Normal
            (1320, 800),   // justo en el umbral de Normal
            (1366, 768),   // resolucion de portatil mas extendida
            (1440, 900),
            (1519, 864),   // 1px por debajo del umbral de Amplio (AR-14 vivia aqui)
            (1520, 864),   // justo en el umbral de Amplio
            (1600, 900),
            (1920, 1080),
            (2560, 1440),
        ];
    
        // Firma -> (veces, peor caso). Agrupar es lo que hace legible la salida: el mismo bug
        // aparece en decenas de combinaciones tamaño x idioma, y lo util es "en cuantas y cual
        // es la peor", no 300 lineas iguales.
        var perdidos = new Dictionary<string, (int veces, string peor, double peorFalta)>();
        var solapes = new Dictionary<string, (int veces, string peor, double peorArea)>();
        var truncados = new Dictionary<string, (int veces, string peor)>();
        int layoutsMedidos = 0, elementosMedidos = 0;

        // Limites CONOCIDOS y medidos: se siguen listando en la salida, pero no cuentan como
        // FALLO. Mismo criterio (y mismo motivo) que la lista de A10-IDIOMA-BARRIDO: un barrido
        // que se queda en rojo por un caso ya diagnosticado y con dueño deja de servir para
        // detectar lo SIGUIENTE, que es para lo que existe.
        //
        // ESTA LISTA ESTA VACIA A PROPOSITO desde el 6-sep-2026. Su unica entrada era
        // "Personaje/Equipamiento | SlotGridPanel" (Monedas/Municion cortada a 1080x700), y ya
        // NO es un limite: esta arreglada de verdad (ver AR-14c en AuditoriaEquipamiento.cs y la
        // entrada de bitacora.md "Monedas/Municion ya no se corta"). Se deja el mecanismo, no el
        // caso: si algun dia vuelve a hacer falta, ojo al formato - la FIRMA con la que se compara
        // es "<pantalla> | <elemento>" (el tamaño va solo en el detalle), asi que aqui se nombra
        // el ELEMENTO, nunca el tamaño.
        string[] limitesConocidos = [];
    
        void Auditar(string contexto)
        {
            layoutsMedidos++;
            // ---- D1: contenido perdido (recortado y sin escape por scroll en ese eje) ----
            foreach (var fe in Descendientes<FrameworkElement>(window))
            {
                if (fe is not (TextBlock or System.Windows.Controls.Primitives.ButtonBase or SlotGridPanel)) continue;
                if (!fe.IsVisible || fe.ActualWidth < 1 || fe.ActualHeight < 1) continue;
                if (fe is TextBlock tbVacio && string.IsNullOrWhiteSpace(tbVacio.Text)) continue;
                elementosMedidos++;
                Rect completo, zona;
                try { completo = RectCompleto(fe, window); zona = ZonaVisible(fe, window); }
                catch (InvalidOperationException) { continue; } // desconectado del arbol a media medicion
                // La falta se mide EJE A EJE contra la zona que de verdad se pinta, nunca restando
                // anchos: un elemento que ha quedado ENTERO por debajo del viewport tiene una
                // interseccion vacia, y restar anchos le atribuiria tambien una falta horizontal
                // que no existe. Primera version de este bloque: 1560 "perdidos", todos falsos por
                // esto mismo (filas de Novedades/Builds/Ajustes que solo necesitaban scroll
                // VERTICAL, el que si tenian). Medido por eje: la falta horizontal sale 0 y el
                // caso desaparece solo, sin ninguna lista de excepciones.
                double faltaX, faltaY;
                if (zona.IsEmpty)
                {
                    // Nada de este elemento se esta pintando y no hay zona con la que comparar, asi
                    // que no se puede saber en QUE eje se perdio. Es el caso normal de una fila que
                    // ha quedado fuera del viewport de una lista larga (Builds, Novedades), y ahi
                    // cualquier scroll por encima basta para llegar a ella: solo cuenta como
                    // perdida si no hay NINGUNA via de scroll, ni horizontal ni vertical.
                    if (AlcanzableConScroll(fe, window, true) || AlcanzableConScroll(fe, window, false)) continue;
                    faltaX = completo.Width; faltaY = completo.Height;
                }
                else
                {
                    faltaX = Math.Min(completo.Width, Math.Max(0, zona.Left - completo.Left) + Math.Max(0, completo.Right - zona.Right));
                    faltaY = Math.Min(completo.Height, Math.Max(0, zona.Top - completo.Top) + Math.Max(0, completo.Bottom - zona.Bottom));
                }
                if (faltaX <= 1 && faltaY <= 1) continue;
                bool escapaX = faltaX <= 1 || AlcanzableConScroll(fe, window, horizontal: true);
                bool escapaY = faltaY <= 1 || AlcanzableConScroll(fe, window, horizontal: false);
                if (escapaX && escapaY) continue; // recortado pero ALCANZABLE: no es perdida
                string que = Describir(fe);
                string firma = $"{contexto.Split(' ')[0]} | {que}";
                double falta = Math.Max(escapaX ? 0 : faltaX, escapaY ? 0 : faltaY);
                // La cadena de contenedores es lo unico que dice DONDE se pierde el ancho (mismo
                // helper que ya uso AR-13a): el culpable es el primer eslabon que ya mide mas que
                // la zona que de verdad se pinta.
                string cadena = "";
                try { cadena = " <- " + string.Join(" / ", Ascendencia(fe, window).TakeLast(8)); } catch (Exception) { }
                cadena += " [recorta: " + QuienRecorta(fe, window) + "]";
                // Y el estado real del primer ScrollViewer por encima: dice si el escape existia
                // pero estaba desactivado (eje Disabled) o si simplemente no tenia a donde
                // desplazarse (Scrollable=0, o sea que el ScrollViewer tampoco sabe que su
                // contenido no cabe) - son dos causas distintas y se arreglan distinto.
                var svs = new List<string>();
                for (DependencyObject? d = System.Windows.Media.VisualTreeHelper.GetParent(fe); d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
                    if (d is System.Windows.Controls.ScrollViewer sv0)
                        svs.Add($"V:{sv0.VerticalScrollBarVisibility} vp={sv0.ViewportHeight:0.#} ext={sv0.ExtentHeight:0.#} scr={sv0.ScrollableHeight:0.#} / " +
                                $"H:{sv0.HorizontalScrollBarVisibility} vp={sv0.ViewportWidth:0.#} ext={sv0.ExtentWidth:0.#} scr={sv0.ScrollableWidth:0.#}");
                if (svs.Count > 0) cadena += " [ScrollViewers de dentro a fuera: " + string.Join(" || ", svs) + "]";
                string detalle = $"{contexto}: {que} pierde {(escapaX ? 0 : faltaX):0.#}x{(escapaY ? 0 : faltaY):0.#}px " +
                                 $"(caja {completo.Width:0.#}x{completo.Height:0.#} en x={completo.Left:0.#} y={completo.Top:0.#}, " +
                                 $"zona pintada {(zona.IsEmpty ? "vacia" : $"{zona.Left:0.#}..{zona.Right:0.#} x {zona.Top:0.#}..{zona.Bottom:0.#}")}) sin scroll que lo alcance{cadena}";
                if (!perdidos.TryGetValue(firma, out var p) || falta > p.peorFalta) perdidos[firma] = ((perdidos.TryGetValue(firma, out var q) ? q.veces : 0) + 1, detalle, falta);
                else perdidos[firma] = (p.veces + 1, p.peor, p.peorFalta);
            }
    
            // ---- D2: solape entre hijos de un Grid que ocupan celdas disjuntas ----
            foreach (var g in Descendientes<System.Windows.Controls.Grid>(window))
            {
                if (!g.IsVisible || g.Children.Count < 2 || g.Children.Count > 30) continue;
                if (g.ColumnDefinitions.Count < 2 && g.RowDefinitions.Count < 2) continue;
                var hijos = g.Children.OfType<FrameworkElement>()
                    .Where(c => c.IsVisible && c.ActualWidth > 1 && c.ActualHeight > 1).ToList();
                for (int i = 0; i < hijos.Count; i++)
                    for (int j = i + 1; j < hijos.Count; j++)
                    {
                        var a = hijos[i]; var b = hijos[j];
                        int ca = System.Windows.Controls.Grid.GetColumn(a), csa = Math.Max(1, System.Windows.Controls.Grid.GetColumnSpan(a));
                        int cb = System.Windows.Controls.Grid.GetColumn(b), csb = Math.Max(1, System.Windows.Controls.Grid.GetColumnSpan(b));
                        int ra = System.Windows.Controls.Grid.GetRow(a), rsa = Math.Max(1, System.Windows.Controls.Grid.GetRowSpan(a));
                        int rb = System.Windows.Controls.Grid.GetRow(b), rsb = Math.Max(1, System.Windows.Controls.Grid.GetRowSpan(b));
                        bool colsDisjuntas = ca + csa <= cb || cb + csb <= ca;
                        bool filasDisjuntas = ra + rsa <= rb || rb + rsb <= ra;
                        if (!colsDisjuntas && !filasDisjuntas) continue; // comparten celda a proposito
                        Rect ra2, rb2;
                        try { ra2 = RectVisible(a, window); rb2 = RectVisible(b, window); }
                        catch (InvalidOperationException) { continue; }
                        if (ra2.IsEmpty || rb2.IsEmpty) continue;
                        var inter = Rect.Intersect(ra2, rb2);
                        if (inter.IsEmpty || inter.Width <= 1 || inter.Height <= 1) continue;
                        double area = inter.Width * inter.Height;
                        string firma = $"{contexto.Split(' ')[0]} | {Describir(a)} <-> {Describir(b)}";
                        string detalle = $"{contexto}: {Describir(a)} y {Describir(b)} se pisan {inter.Width:0.#}x{inter.Height:0.#}px " +
                                         $"(celdas c{ca}+{csa}/f{ra}+{rsa} y c{cb}+{csb}/f{rb}+{rsb} - disjuntas, nunca deberian tocarse)";
                        if (!solapes.TryGetValue(firma, out var s) || area > s.peorArea) solapes[firma] = ((solapes.TryGetValue(firma, out var t) ? t.veces : 0) + 1, detalle, area);
                        else solapes[firma] = (s.veces + 1, s.peor, s.peorArea);
                    }
            }
    
            // ---- D3: texto truncado con puntos suspensivos (informativo) ----
            foreach (var tb in Descendientes<TextBlock>(window))
            {
                if (!tb.IsVisible || tb.ActualWidth < 1 || string.IsNullOrWhiteSpace(tb.Text)) continue;
                if (!TextoRecortado(tb)) continue;
                string firma = $"{contexto.Split(' ')[0]} | {(tb.Text.Length > 40 ? tb.Text[..40] : tb.Text)}";
                string detalle = $"{contexto}: \"{(tb.Text.Length > 60 ? tb.Text[..60] + "..." : tb.Text)}\" necesita {AnchoNaturalDelTexto(tb):0}px y tiene {tb.ActualWidth:0}px";
                if (!truncados.ContainsKey(firma)) truncados[firma] = (1, detalle);
                else truncados[firma] = (truncados[firma].veces + 1, truncados[firma].peor);
            }
        }
    
        // Traza con AutoFlush a fichero propio: si el proceso se cae a media auditoria (paso
        // real de esta ronda), lo escrito por Console se pierde ENTERO en el buffer y no queda
        // ni una pista de DONDE murio. Ruta absoluta y fija dentro de la propia salida de
        // compilacion, nunca Path.GetTempPath() (ver CLAUDE.md, "verdades del entorno"), y con
        // el PID en el nombre: dos ejecuciones del arnes A LA VEZ (pasa de verdad cuando hay
        // varias sesiones trabajando sobre el mismo repo) se pisaban el fichero, y la
        // IOException resultante se llevaba por delante el bloque entero sin medir nada.
        StreamWriter? traza = null;
        try { traza = new StreamWriter(Path.Combine(AppContext.BaseDirectory, $"ar-lay-traza-{Environment.ProcessId}.log"), append: false) { AutoFlush = true }; }
        catch (IOException) { } // sin traza se mide igual: es apoyo de diagnostico, no la medida
    
        var swArLay = System.Diagnostics.Stopwatch.StartNew();
        foreach (string idioma in new[] { "es", "en" })
        {
            traza?.WriteLine($"{swArLay.ElapsedMilliseconds}ms >>> idioma := {idioma}");
            vm.Settings.Language = idioma;
            DoEvents();
            traza?.WriteLine($"{swArLay.ElapsedMilliseconds}ms >>> idioma {idioma} aplicado");
            foreach (var (w, h) in tamaños)
            {
                traza?.WriteLine($"{swArLay.ElapsedMilliseconds}ms >>> tamaño := {w:0}x{h:0}");
                FijarTamaño(window, w, h);
                foreach (var (nombre, ir) in pantallas)
                {
                    traza?.WriteLine($"{swArLay.ElapsedMilliseconds}ms {nombre} {w:0}x{h:0} [{idioma}] ir()");
                    ir();
                    DoEvents(); DoEvents();
                    traza?.WriteLine($"{swArLay.ElapsedMilliseconds}ms {nombre} {w:0}x{h:0} [{idioma}] auditar()");
                    Auditar($"{nombre} {w:0}x{h:0} [{idioma}]");
                }
            }
        }
    
        // Barrido FINO de anchos: es el unico que caza una franja de pocos px como la de
        // AR-14 (14px de ancho). Caro (cada paso reevalua la app entera), por eso solo con
        // AR_LAY_FINO=<paso>. Ej: AR_LAY_FINO=4 AR_LAY_SOLO=Equipamiento.
        string? fino = Environment.GetEnvironmentVariable("AR_LAY_FINO");
        if (int.TryParse(fino, out int paso) && paso > 0)
        {
            double desde = double.TryParse(Environment.GetEnvironmentVariable("AR_LAY_DESDE"), out double d) ? d : 1080;
            double hasta = double.TryParse(Environment.GetEnvironmentVariable("AR_LAY_HASTA"), out double hh) ? hh : 2560;
            vm.Settings.Language = "es";
            DoEvents();
            for (double w = desde; w <= hasta; w += paso)
            {
                FijarTamaño(window, w, 860);
                foreach (var (nombre, ir) in pantallas)
                {
                    ir();
                    DoEvents(); DoEvents();
                    Auditar($"{nombre} {w:0}x860 [es,fino]");
                }
            }
            Console.WriteLine($"AR-LAY-FINO: barrido de {desde:0} a {hasta:0} de {paso} en {paso}px completado");
        }
        swArLay.Stop();
        traza?.Dispose();
    
        var perdidosNuevos = perdidos.Where(k => !limitesConocidos.Any(l => k.Key.Contains(l))).ToList();
        var perdidosConocidos = perdidos.Count - perdidosNuevos.Count;
        Console.WriteLine($"AR-LAY: {layoutsMedidos} combinaciones pantalla x tamaño x idioma medidas ({elementosMedidos} elementos), {swArLay.ElapsedMilliseconds}ms");
        Console.WriteLine($"AR-LAY: contenido PERDIDO (recortado y sin scroll que lo alcance)={perdidosNuevos.Count} firmas (esperado 0), " +
                          $"SOLAPES entre celdas disjuntas de un Grid={solapes.Count} firmas (esperado 0), " +
                          $"textos truncados con '...'={truncados.Count} firmas (informativo), " +
                          $"limites ya conocidos y documentados={perdidosConocidos} (informativo, no es fallo)");
        foreach (var kv in perdidosNuevos.OrderByDescending(k => k.Value.peorFalta).Take(30))
            Console.WriteLine($"   AR-LAY-PERDIDO x{kv.Value.veces} {kv.Value.peor}");
        foreach (var kv in perdidos.Where(k => limitesConocidos.Any(l => k.Key.Contains(l))).Take(10))
            Console.WriteLine($"   AR-LAY-LIMITE-CONOCIDO x{kv.Value.veces} {kv.Value.peor}");
        foreach (var kv in solapes.OrderByDescending(k => k.Value.peorArea).Take(30))
            Console.WriteLine($"   AR-LAY-SOLAPE x{kv.Value.veces} {kv.Value.peor}");
        foreach (var kv in truncados.OrderByDescending(k => k.Value.veces).Take(25))
            Console.WriteLine($"   AR-LAY-TRUNCADO x{kv.Value.veces} {kv.Value.peor}");
        if (perdidosNuevos.Count > 0) Console.WriteLine($"FALLO: AR-LAY - {perdidosNuevos.Count} elementos quedan recortados sin ninguna forma de alcanzarlos en algun tamaño de ventana");
        if (solapes.Count > 0) Console.WriteLine($"FALLO: AR-LAY - {solapes.Count} pares de elementos de celdas disjuntas se solapan en algun tamaño de ventana");
    
        vm.Settings.Language = idiomaPrevio;
        FijarTamaño(window, anchoPrevio, altoPrevio);
        vm.SelectedTabIndex = tabPrevio; vm.PersonajeInnerTabIndex = innerPrevio; vm.ObjetosSubTabIndex = subPrevio;
        DoEvents();
    }
    catch (Exception ex) { Console.WriteLine("AR-LAY-EXCEPTION: " + ex); }
    }

    // ====================================================================================
    // CANARIO DE AR-LAY (16-sep-2026, KeepQA/v4/I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md, hallazgo H6)
    // ====================================================================================
    // H6 documenta tres incidentes reales de la familia donde el arnes automatico dio verde sin
    // haber comprobado nada de verdad (un regex que nunca casaba en ServidorKeep, un informe de
    // Starvekeep que llego a decir "OK" con un bug puesto a proposito, y los tres proyectos WPF -
    // Terrakeep incluido - pagando el mismo tipo de riesgo con el contraste) y solo StarvekeepMod
    // tenia ya un canario real que lo evita (scripts/starvekeep/auditoria.lua,
    // Auditoria.RevisarCanario: "si el detector se rompiera, todo lo demas saldria en verde sin
    // haber comprobado nada"). AR-LAY es el detector mas critico de todo este arnes -corre en
    // CADA `dotnet run`, sobre cientos de combinaciones pantalla x tamaño x idioma, nunca detras
    // de un flag opcional como CAPAS_SOLO- y esta envuelto en un catch(Exception) que solo
    // imprime "AR-LAY-EXCEPTION" y sigue: si algo revienta A MEDIA auditoria (antes de llegar a
    // la linea "AR-LAY: N combinaciones medidas"), ni esa linea de resumen ni ningun
    // "FALLO: AR-LAY" se imprimen NUNCA. Como el gate real de todo este arnes es "grep FALLO: en
    // la consola" (cada modo de Program.cs hace Environment.Exit(0) pase lo que pase, confirmado
    // grepeando el propio fichero), un AR-LAY que nunca llega a imprimir nada es indistinguible
    // de un AR-LAY que corrio limpio para quien solo mira si aparecio "FALLO:" - exactamente la
    // misma forma que el regex roto de ServidorKeep (una condicion que deja de evaluarse nunca
    // avisa de que dejo de evaluarse).
    //
    // Este canario ejercita el motor real de deteccion (RectCompleto/ZonaVisible/
    // AlcanzableConScroll, los mismos tres que usa D1; RectVisible, el que usa D2) contra una
    // ventana real, minima, fuera de pantalla - mismo patron ya probado en este mismo repo
    // (AuditoriaKeepQA.cs, EjecutarCapasSinteticoSolo: sin PresentationSource, RectCompleto/
    // TransformToAncestor no reflejan un layout real) - con un caso IMPOSIBLE de no detectar en
    // cada detector y uno trivial que no tiene que detectarse, exactamente el mismo criterio que
    // Auditoria.RevisarCanario de StarvekeepMod. Se llama ANTES del barrido real; si falla,
    // imprime "FALLO: AR-LAY-CANARIO" (mismo prefijo "FALLO:" que ya vigila el resto del arnes,
    // sin inventar un segundo mecanismo de gate) y BarridoMaquetacionPorTamañoEIdioma no continua
    // con el barrido real.
    private static bool ComprobarCanarioArLay()
    {
        // ---- D1: un texto recortado sin escape de scroll TIENE que detectarse, uno sin recortar NO ----
        var textoLargo = new TextBlock { Text = new string('M', 200), FontSize = 20, TextWrapping = TextWrapping.NoWrap };
        var cajaRecortada = new Border { Width = 40, Height = 30, ClipToBounds = true, Child = textoLargo };
        var textoTrivial = new TextBlock { Text = "Vida", FontSize = 13 };

        // ---- D2: dos elementos en celdas DISJUNTAS de un Grid que se pisan de verdad TIENEN que
        // detectarse; dos que respetan su celda NO ----
        var gridD2 = new Grid();
        gridD2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        gridD2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        // cajaSolapa se sale a proposito de su columna con un margen izquierdo NEGATIVO (no con
        // un Width mayor que la columna: comprobado de verdad en esta misma ronda, con trazas
        // reales, que un Grid en este WPF/.NET SÍ limita el Arrange de un hijo al ancho de su
        // ColumnDefinition aunque su Width propio pida más -contra lo que la documentación
        // "clásica" de WPF sugiere-, así que un Width mayor no reproduce un solape real aquí: el
        // hijo queda clampado a 100px igual y el canario no detectaría nada porque no habría nada
        // que detectar. Un margen negativo, en cambio, desplaza el rectángulo de Arrange ENTERO
        // fuera de la columna sin que ese clamp lo evite - la misma clase de solape real que
        // motivó AR-LAY (un elemento colocado a mano con Margin que invade la celda vecina).
        // Border en vez de Button a propósito: la plantilla/chrome de Button añade su propio
        // recorte interno (confirmado con trazas: con Button, RectVisible ya salía clampado a la
        // columna incluso con margen negativo), y eso habría probado la plantilla de Button, no
        // el detector D2 en sí.
        var cajaSolapa = new Border { Background = Brushes.Red, Width = 90, Height = 30, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(-70, 0, 0, 0) };
        System.Windows.Controls.Grid.SetColumn(cajaSolapa, 1);
        var cajaVecina = new Border { Background = Brushes.Blue, Width = 90, Height = 30, HorizontalAlignment = HorizontalAlignment.Left };
        System.Windows.Controls.Grid.SetColumn(cajaVecina, 0);
        gridD2.Children.Add(cajaSolapa);
        gridD2.Children.Add(cajaVecina);

        var gridD2Trivial = new Grid();
        gridD2Trivial.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        gridD2Trivial.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        var cajaPropia = new Border { Background = Brushes.Red, Width = 90, Height = 30, HorizontalAlignment = HorizontalAlignment.Left };
        System.Windows.Controls.Grid.SetColumn(cajaPropia, 0);
        var cajaVecinaOk = new Border { Background = Brushes.Blue, Width = 90, Height = 30, HorizontalAlignment = HorizontalAlignment.Left };
        System.Windows.Controls.Grid.SetColumn(cajaVecinaOk, 1);
        gridD2Trivial.Children.Add(cajaPropia);
        gridD2Trivial.Children.Add(cajaVecinaOk);

        var raiz = new StackPanel();
        raiz.Children.Add(cajaRecortada);
        raiz.Children.Add(textoTrivial);
        raiz.Children.Add(gridD2);
        raiz.Children.Add(gridD2Trivial);

        var ventana = new Window
        {
            Content = raiz,
            Width = 400,
            Height = 300,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -5000,
            Top = -5000,
            ShowInTaskbar = false,
            Title = "Terrakeep-canario-AR-LAY",
        };
        ventana.Show();
        DoEvents(); DoEvents(); DoEvents();

        bool ok = true;

        // --- D1, caso imposible ---
        Rect completoLargo, zonaLargo;
        try { completoLargo = RectCompleto(textoLargo, ventana); zonaLargo = ZonaVisible(textoLargo, ventana); }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine("FALLO: AR-LAY-CANARIO - excepcion midiendo el caso D1 imposible: " + ex.Message);
            ventana.Close();
            return false;
        }
        double faltaXLargo, faltaYLargo;
        if (zonaLargo.IsEmpty) { faltaXLargo = completoLargo.Width; faltaYLargo = completoLargo.Height; }
        else
        {
            faltaXLargo = Math.Min(completoLargo.Width, Math.Max(0, zonaLargo.Left - completoLargo.Left) + Math.Max(0, completoLargo.Right - zonaLargo.Right));
            faltaYLargo = Math.Min(completoLargo.Height, Math.Max(0, zonaLargo.Top - completoLargo.Top) + Math.Max(0, completoLargo.Bottom - zonaLargo.Bottom));
        }
        bool escapaXLargo = faltaXLargo <= 1 || AlcanzableConScroll(textoLargo, ventana, horizontal: true);
        bool escapaYLargo = faltaYLargo <= 1 || AlcanzableConScroll(textoLargo, ventana, horizontal: false);
        if (escapaXLargo && escapaYLargo)
        {
            Console.WriteLine($"FALLO: AR-LAY-CANARIO - un texto de 200 caracteres recortado por un Border de 40x30 sin ScrollViewer NO se detecta como contenido perdido (faltaX={faltaXLargo:0.#} faltaY={faltaYLargo:0.#}). D1 no esta comprobando de verdad.");
            ok = false;
        }

        // --- D1, caso trivial ---
        Rect completoTrivial, zonaTrivial;
        try { completoTrivial = RectCompleto(textoTrivial, ventana); zonaTrivial = ZonaVisible(textoTrivial, ventana); }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine("FALLO: AR-LAY-CANARIO - excepcion midiendo el caso D1 trivial: " + ex.Message);
            ventana.Close();
            return false;
        }
        double faltaXTrivial = zonaTrivial.IsEmpty ? completoTrivial.Width : Math.Min(completoTrivial.Width, Math.Max(0, zonaTrivial.Left - completoTrivial.Left) + Math.Max(0, completoTrivial.Right - zonaTrivial.Right));
        double faltaYTrivial = zonaTrivial.IsEmpty ? completoTrivial.Height : Math.Min(completoTrivial.Height, Math.Max(0, zonaTrivial.Top - completoTrivial.Top) + Math.Max(0, completoTrivial.Bottom - zonaTrivial.Bottom));
        if (faltaXTrivial > 1 || faltaYTrivial > 1)
        {
            Console.WriteLine($"FALLO: AR-LAY-CANARIO - un texto trivial SIN recortar se detecta como perdido (faltaX={faltaXTrivial:0.#} faltaY={faltaYTrivial:0.#}). D1 esta dando falsos positivos.");
            ok = false;
        }

        // --- D2, caso imposible (solape real entre celdas disjuntas) ---
        Rect ra2, rb2;
        try { ra2 = RectVisible(cajaSolapa, ventana); rb2 = RectVisible(cajaVecina, ventana); }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine("FALLO: AR-LAY-CANARIO - excepcion midiendo el caso D2 imposible: " + ex.Message);
            ventana.Close();
            return false;
        }
        var interD2 = Rect.Intersect(ra2, rb2);
        if (interD2.IsEmpty || interD2.Width <= 1 || interD2.Height <= 1)
        {
            Console.WriteLine($"FALLO: AR-LAY-CANARIO - dos elementos en columnas DISJUNTAS de un Grid (uno de ellos con un margen negativo que lo mete 60px dentro de la columna vecina) NO se detectan solapados. D2 no esta comprobando de verdad.");
            ok = false;
        }

        // --- D2, caso trivial (cada uno respeta su celda) ---
        Rect rc2, rd2;
        try { rc2 = RectVisible(cajaPropia, ventana); rd2 = RectVisible(cajaVecinaOk, ventana); }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine("FALLO: AR-LAY-CANARIO - excepcion midiendo el caso D2 trivial: " + ex.Message);
            ventana.Close();
            return false;
        }
        var interD2Trivial = Rect.Intersect(rc2, rd2);
        if (!interD2Trivial.IsEmpty && interD2Trivial.Width > 1 && interD2Trivial.Height > 1)
        {
            Console.WriteLine($"FALLO: AR-LAY-CANARIO - dos botones que respetan su propia columna se detectan solapados ({interD2Trivial.Width:0.#}x{interD2Trivial.Height:0.#}px). D2 esta dando falsos positivos.");
            ok = false;
        }

        ventana.Close();
        DoEvents();

        if (ok) Console.WriteLine("AR-LAY-CANARIO: OK - D1 distingue un recorte imposible de un caso trivial, y D2 distingue un solape real de dos celdas respetadas.");
        return ok;
    }

    // AR-LAY: la ZONA de la ventana que de verdad se esta pintando para este elemento - la
    // interseccion del clip de todos sus ancestros, SIN cruzarla con el rectangulo del propio
    // elemento. Es la pieza que le falta a RectVisible para poder decir en QUE EJE se pierde
    // contenido: RectVisible devuelve Rect.Empty en cuanto el elemento cae entero fuera del
    // viewport, y de un rectangulo vacio no se puede deducir si lo que falto fue alto o ancho.
    private static Rect ZonaVisible(FrameworkElement fe, FrameworkElement raiz)
    {
        var zona = new Rect(0, 0, raiz.ActualWidth, raiz.ActualHeight);
        for (DependencyObject? d = fe; d != null && !ReferenceEquals(d, raiz); d = System.Windows.Media.VisualTreeHelper.GetParent(d))
        {
            if (d is not System.Windows.Media.Visual v) continue;
            var clip = System.Windows.Media.VisualTreeHelper.GetClip(v);
            if (clip == null) continue;
            zona.Intersect(v.TransformToAncestor(raiz).TransformBounds(clip.Bounds));
            if (zona.IsEmpty) return Rect.Empty;
        }
        return zona;
    }

    // AR-LAY: QUIEN esta recortando de verdad a este elemento - el ancestro cuyo clip fija el
    // limite mas estrecho. Sin esto, un "se pierde contenido" solo dice que algo va mal; con
    // esto dice DONDE tocar, que es la mitad del trabajo (el clip nunca vive en el elemento
    // recortado: esa es justo la razon por la que Recorte()/GetClip no encontraba estos casos).
    private static string QuienRecorta(FrameworkElement fe, FrameworkElement raiz)
    {
        // TODOS los ancestros con clip, de dentro hacia fuera. A proposito no se elige "el mas
        // pequeño": el que limita el ANCHO y el que limita el ALTO suelen ser dos distintos, y
        // quedarse con uno solo (por area) esconde justo al culpable del eje que falla.
        var conClip = new List<string>();
        for (DependencyObject? d = fe; d != null && !ReferenceEquals(d, raiz); d = System.Windows.Media.VisualTreeHelper.GetParent(d))
        {
            if (d is not System.Windows.Media.Visual v) continue;
            var clip = System.Windows.Media.VisualTreeHelper.GetClip(v);
            if (clip == null) continue;
            string nombre = d is FrameworkElement f
                ? $"{f.GetType().Name}{(string.IsNullOrEmpty(f.Name) ? "" : "#" + f.Name)}"
                : d.GetType().Name;
            conClip.Add($"{nombre} {clip.Bounds.Width:0.#}x{clip.Bounds.Height:0.#}");
        }
        return conClip.Count == 0 ? "(nadie: cabe entero)" : string.Join(" < ", conClip);
    }

    // AR-LAY: el rectangulo COMPLETO del elemento (lo que ocuparia si nadie lo recortara), en
    // coordenadas de `raiz`. Su pareja es RectVisible (lo que de verdad se pinta): la diferencia
    // entre los dos es, exactamente, el contenido que el usuario no llega a ver.
    private static Rect RectCompleto(FrameworkElement fe, FrameworkElement raiz) =>
        fe.TransformToAncestor(raiz).TransformBounds(new Rect(0, 0, fe.ActualWidth, fe.ActualHeight));

    // Lista COMPARTIDA de pantallas/sub-pestañas reales (16: Inicio, 9 estados de Personaje -
    // Equipamiento/Inventario/Almacenes/Buffs/Investigacion/Apariencia/SpawnPoints/Desbloqueos/
    // Version -, 2 hojas de Builds, 2 de Novedades, Exploracion, AcercaDe+Ajustes) - extraida de
    // BarridoMaquetacionPorTamañoEIdioma (14-sep-2026, encargo KeepQA "informe real tras la ronda
    // de arreglos") para que el volcado de geometria de AuditoriaKeepQA.cs (KEEPQA_SOLO) recorra
    // EXACTAMENTE las mismas pantallas que AR-LAY ya audita cada `dotnet run`, en vez de mantener
    // una segunda lista de mano que se desincronizaria de esta con el tiempo - un solo sitio real
    // donde vive "cuales son las pantallas de Terrakeep", nunca dos copias.
    private static List<(string nombre, Action ir)> ConstruirPantallasMaquetacion(Window window, MainViewModel vm)
    {
        // El TabControl anidado de Builds y el de Novedades son los unicos con exactamente 2
        // hojas - no hace falta ponerles x:Name en el XAML de produccion solo para el arnes.
        static void HojaAnidada(Window w, int indice)
        {
            var tc = Descendientes<System.Windows.Controls.TabControl>(w).FirstOrDefault(t => t.IsVisible && t.Items.Count == 2);
            if (tc != null && indice < tc.Items.Count) tc.SelectedIndex = indice;
        }

        return new List<(string nombre, Action ir)>
        {
            ("Inicio", () => { vm.SelectedTabIndex = 0; }),
            ("Personaje/Equipamiento", () => { vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 0; }),
            ("Personaje/Inventario", () => { vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 1; }),
            ("Personaje/Almacenes", () => { vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 2; }),
            ("Personaje/Buffs", () => { vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 1; }),
            ("Personaje/Investigacion", () => { vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 2; }),
            ("Personaje/Apariencia", () => { vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 3; }),
            ("Personaje/SpawnPoints", () => { vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 4; }),
            ("Personaje/Desbloqueos", () => { vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 5; }),
            ("Personaje/Version", () => { vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 6; }),
            ("Builds/Vanilla", () => { vm.SelectedTabIndex = 2; DoEvents(); HojaAnidada(window, 0); }),
            ("Builds/Calamity", () => { vm.SelectedTabIndex = 2; DoEvents(); HojaAnidada(window, 1); }),
            ("Novedades/Terraria", () => { vm.SelectedTabIndex = 3; DoEvents(); HojaAnidada(window, 0); }),
            ("Novedades/tModLoader", () => { vm.SelectedTabIndex = 3; DoEvents(); HojaAnidada(window, 1); }),
            ("Exploracion", () => { vm.SelectedTabIndex = 4; }),
            ("AcercaDe+Ajustes", () => { vm.SelectedTabIndex = 5; }),
        };
    }

    // Extractor REAL de orden de dibujado para WPF (14-sep-2026, encargo del coordinador: "el
    // equivalente real existe de verdad... construye el extractor real"), pieza gemela de
    // `self.ordenProot` de StarvekeepMod (DST/Lua, instrumentado a mano porque ese motor no
    // expone ningun indice de capa) pero mas directa aqui porque WPF SI expone un orden de
    // dibujado real y consultable, sin instrumentar nada: `Panel.GetZIndex()` (explicito, si se
    // ha fijado) y, dentro del mismo valor de ZIndex, el ORDEN DE POSICION dentro de los hijos
    // reales de su mismo padre visual (`VisualTreeHelper.GetChild`) - los elementos declarados/
    // añadidos despues se pintan encima de los anteriores dentro del mismo padre, hecho real y
    // documentado del propio motor de composicion de WPF, no una suposicion.
    //
    // Honestidad real sobre "explicito o no": la propiedad adjunta `Panel.ZIndex` NO expone
    // ninguna API publica para distinguir "nunca se fijo" de "se fijo a 0 a proposito" (el valor
    // por defecto de `Panel.GetZIndex()` es 0 en los dos casos, comprobado leyendo la propia
    // implementacion de `Panel.ZIndexProperty` - es una DependencyProperty con valor por defecto
    // 0, sin ningun `ReadLocalValue`/`HasLocalValue` publico que XAML use por convenio). Por eso
    // este extractor no intenta fingir esa distincion: combina SIEMPRE los dos factores reales
    // que WPF usa de verdad para decidir el orden de dibujado (ZIndex primero, orden de hijos
    // como desempate dentro del mismo ZIndex - el algoritmo real de `Panel`, ver su codigo fuente
    // de referencia) en un unico numero comparable, multiplicando el ZIndex por un factor mayor
    // que cualquier recuento de hijos real posible en esta app (100000, ningun panel de Terrakeep
    // se acerca a esa cifra) y sumandole el indice de posicion real. Sigue siendo la fuente de
    // verdad honesta: si dos elementos comparten padre visual, `OrdenZ(a) > OrdenZ(b)` predice
    // EXACTAMENTE cual pinta WPF por delante, sea por ZIndex explicito o por orden de insercion.
    private static int VisualChildIndex(DependencyObject hijo)
    {
        var padre = System.Windows.Media.VisualTreeHelper.GetParent(hijo);
        if (padre == null) return 0;
        int total = System.Windows.Media.VisualTreeHelper.GetChildrenCount(padre);
        for (int i = 0; i < total; i++)
            if (ReferenceEquals(System.Windows.Media.VisualTreeHelper.GetChild(padre, i), hijo)) return i;
        return 0;
    }

    private static double OrdenZ(FrameworkElement fe) =>
        System.Windows.Controls.Panel.GetZIndex(fe) * 100000.0 + VisualChildIndex(fe);
    
    // AR-LAY: ¿este elemento, recortado en ESTE eje, se puede alcanzar desplazandose? Version
    // afinada de TieneScrollAncestro (AR-11): no basta con que HAYA un ScrollViewer por encima -
    // tiene que poder desplazarse EN ESE EJE. Un ScrollViewer con el eje en Disabled recorta y
    // punto (es el mecanismo real que cortaba los accesorios en AR-14), y uno con ScrollableWidth/
    // Height a 0 no tiene a donde moverse. Sin esta distincion, D1 daria por "alcanzable" justo
    // el caso peor.
    private static bool AlcanzableConScroll(DependencyObject elemento, DependencyObject raiz, bool horizontal)
    {
        for (var d = System.Windows.Media.VisualTreeHelper.GetParent(elemento); d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
        {
            if (d is System.Windows.Controls.ScrollViewer sv)
            {
                if (horizontal && sv.HorizontalScrollBarVisibility != System.Windows.Controls.ScrollBarVisibility.Disabled && sv.ScrollableWidth > 0.5) return true;
                if (!horizontal && sv.VerticalScrollBarVisibility != System.Windows.Controls.ScrollBarVisibility.Disabled && sv.ScrollableHeight > 0.5) return true;
            }
            if (ReferenceEquals(d, raiz)) break;
        }
        return false;
    }
    
    // AR-LAY: nombre corto y ESTABLE de un elemento para agrupar sus incidencias entre tamaños -
    // el texto real si lo tiene (es lo que el usuario reconoce en una captura), y si no el tipo
    // mas su Name de XAML.
    private static string Describir(FrameworkElement fe) => fe switch
    {
        TextBlock tb when !string.IsNullOrWhiteSpace(tb.Text) => $"texto \"{(tb.Text.Length > 34 ? tb.Text[..34] + "..." : tb.Text)}\"",
        System.Windows.Controls.Primitives.ButtonBase b when b.Content is string s && s.Length > 0 => $"boton \"{(s.Length > 30 ? s[..30] + "..." : s)}\"",
        System.Windows.Controls.Primitives.ButtonBase b when Descendientes<TextBlock>(b).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Text)) is { } t2 => $"boton \"{(t2.Text.Length > 30 ? t2.Text[..30] + "..." : t2.Text)}\"",
        _ => $"{fe.GetType().Name}{(string.IsNullOrEmpty(fe.Name) ? "" : "#" + fe.Name)}",
    };
}
