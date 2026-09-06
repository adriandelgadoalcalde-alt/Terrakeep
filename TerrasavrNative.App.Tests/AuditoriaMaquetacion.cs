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
using TerrasavrNative.App;
using TerrasavrNative.App.Controls;
using TerrasavrNative.App.ViewModels;

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
        string idiomaPrevio = vm.Settings.Language;
        int tabPrevio = vm.SelectedTabIndex, innerPrevio = vm.PersonajeInnerTabIndex, subPrevio = vm.ObjetosSubTabIndex;
        double anchoPrevio = window.ActualWidth, altoPrevio = window.ActualHeight;
    
        // El TabControl anidado de Builds y el de Novedades son los unicos con exactamente 2
        // hojas - no hace falta ponerles x:Name en el XAML de produccion solo para el arnes.
        static void HojaAnidada(Window w, int indice)
        {
            var tc = Descendientes<System.Windows.Controls.TabControl>(w).FirstOrDefault(t => t.IsVisible && t.Items.Count == 2);
            if (tc != null && indice < tc.Items.Count) tc.SelectedIndex = indice;
        }
    
        var pantallas = new List<(string nombre, Action ir)>
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
                for (DependencyObject? d = System.Windows.Media.VisualTreeHelper.GetParent(fe); d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
                    if (d is System.Windows.Controls.ScrollViewer sv0)
                    {
                        cadena += $" [1er ScrollViewer: vertical={sv0.VerticalScrollBarVisibility} viewport={sv0.ViewportHeight:0.#} extent={sv0.ExtentHeight:0.#} scrollable={sv0.ScrollableHeight:0.#}; " +
                                  $"horizontal={sv0.HorizontalScrollBarVisibility} viewport={sv0.ViewportWidth:0.#} extent={sv0.ExtentWidth:0.#} scrollable={sv0.ScrollableWidth:0.#}]";
                        break;
                    }
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
    
        Console.WriteLine($"AR-LAY: {layoutsMedidos} combinaciones pantalla x tamaño x idioma medidas ({elementosMedidos} elementos), {swArLay.ElapsedMilliseconds}ms");
        Console.WriteLine($"AR-LAY: contenido PERDIDO (recortado y sin scroll que lo alcance)={perdidos.Count} firmas (esperado 0), " +
                          $"SOLAPES entre celdas disjuntas de un Grid={solapes.Count} firmas (esperado 0), " +
                          $"textos truncados con '...'={truncados.Count} firmas (informativo)");
        foreach (var kv in perdidos.OrderByDescending(k => k.Value.peorFalta).Take(30))
            Console.WriteLine($"   AR-LAY-PERDIDO x{kv.Value.veces} {kv.Value.peor}");
        foreach (var kv in solapes.OrderByDescending(k => k.Value.peorArea).Take(30))
            Console.WriteLine($"   AR-LAY-SOLAPE x{kv.Value.veces} {kv.Value.peor}");
        foreach (var kv in truncados.OrderByDescending(k => k.Value.veces).Take(25))
            Console.WriteLine($"   AR-LAY-TRUNCADO x{kv.Value.veces} {kv.Value.peor}");
        if (perdidos.Count > 0) Console.WriteLine($"FALLO: AR-LAY - {perdidos.Count} elementos quedan recortados sin ninguna forma de alcanzarlos en algun tamaño de ventana");
        if (solapes.Count > 0) Console.WriteLine($"FALLO: AR-LAY - {solapes.Count} pares de elementos de celdas disjuntas se solapan en algun tamaño de ventana");
    
        vm.Settings.Language = idiomaPrevio;
        FijarTamaño(window, anchoPrevio, altoPrevio);
        vm.SelectedTabIndex = tabPrevio; vm.PersonajeInnerTabIndex = innerPrevio; vm.ObjetosSubTabIndex = subPrevio;
        DoEvents();
    }
    catch (Exception ex) { Console.WriteLine("AR-LAY-EXCEPTION: " + ex); }
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

    // AR-LAY: el rectangulo COMPLETO del elemento (lo que ocuparia si nadie lo recortara), en
    // coordenadas de `raiz`. Su pareja es RectVisible (lo que de verdad se pinta): la diferencia
    // entre los dos es, exactamente, el contenido que el usuario no llega a ver.
    private static Rect RectCompleto(FrameworkElement fe, FrameworkElement raiz) =>
        fe.TransformToAncestor(raiz).TransformBounds(new Rect(0, 0, fe.ActualWidth, fe.ActualHeight));
    
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
