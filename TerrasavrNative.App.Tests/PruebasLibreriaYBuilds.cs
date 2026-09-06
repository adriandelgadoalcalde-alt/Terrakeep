using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TerrasavrNative.App;
using TerrasavrNative.App.Services;
using TerrasavrNative.App.ViewModels;

// ========================================================================================
// LIB-* / BUFLIB-* / BUILDS-* (6-sep-2026): oleada de pruebas de LIBRERIA y BUILDS
// ========================================================================================
// Otra parte de la MISMA clase Program del arnes (ver el comentario de `partial` en
// Program.cs): usa tal cual sus helpers reales (DoEvents, WaitForDispatcher, FijarTamaño,
// RectVisible/VisibleEntero, Descendientes) sin duplicar ni uno. Vive en su propio fichero
// solo porque Program.cs pasa de 5.700 lineas y varias rondas trabajan sobre el a la vez.
//
// Zona cubierta: Libreria de objetos (arbol vanilla curado + carpeta madre "Calamity (mod)",
// buscador con la gramatica REAL de Terrasavr, restriccion de slot, tope de resultados,
// paginacion de carpetas >40), Libreria de buffs (mismo buscador, su propio tope de 300) y
// Builds (posesion real, filtro de clase, auto-equipar).
//
// Lo que estos bloques miden y NINGUN bloque anterior medía:
//  - La GRAMATICA de busqueda (coma=OR, espacio=AND, "#id", "#a-b", ".tooltip", plegado de
//    acentos) solo estaba probada como funcion pura o de refilon; aqui se ejercita sobre el
//    ViewModel real con el catalogo real completo, que es donde vive el bug de verdad.
//  - La coherencia del ARBOL construido: que ninguna hoja de Calamity se pase del tope de
//    paginacion, que ninguna carpeta salga vacia, que la lista de una carpeta madre sea de
//    verdad la union ordenada de sus hijos, y que ningun id del arbol sea un FANTASMA
//    (LibraryViewModel descarta en silencio un id que el catalogo no conozca - util para no
//    reventar, pero significa que una carpeta puede enseñar menos objetos de los que dice).
//  - El arbol RENDERIZADO en su columna estrecha y FIJA (210px) con los nombres largos reales
//    de Calamity, a varios tamaños de ventana: cuantas filas son alcanzables de verdad,
//    cuantas se cortan a lo ancho y cuanto ancho util le queda al nombre mas hundido.
//  - El IDIOMA de la Libreria haciendo el gesto real del usuario (cambiar de idioma y NO tocar
//    nada mas). El barrido A10-IDIOMA-BARRIDO no puede ver esto: navega PULSANDO carpetas, y
//    cada pulsacion vuelve a llamar a ApplyFilter, que regenera el texto en el idioma activo.
internal static partial class Program
{
    private static void PruebasLibreriaYBuilds(MainViewModel vm, MainWindow window)
    {
        try
        {
            int tabPrevioLib = vm.SelectedTabIndex, innerPrevioLib = vm.PersonajeInnerTabIndex;
            string idiomaPrevioLib = vm.Settings.Language;
            double anchoPrevioLib = window.ActualWidth, altoPrevioLib = window.ActualHeight;
            bool libPlegadaPrevio = vm.IsLibraryCollapsed, bufPlegadaPrevio = vm.IsBuffLibraryCollapsed;
            try
            {
                vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0;
                vm.IsLibraryCollapsed = false;
                DoEvents(); DoEvents();

                // La busqueda de las 3 superficies pasa por un DispatcherTimer real de 180ms
                // (CatalogBrowserViewModel) - un DoEvents() suelto no espera tiempo real ninguno,
                // misma leccion ya aprendida por H5-02 en Program.cs.
                List<LibraryItemViewModel> Buscar(string q)
                {
                    vm.Library.SearchText = q;
                    WaitForDispatcher(300);
                    return vm.Library.Results.ToList();
                }

                // ---------- LIB-01: gramatica real de busqueda sobre el catalogo real ----------
                vm.Library.ClearCategoryCommand.Execute(null);
                DoEvents();

                var porId = Buscar("#4"); // Iron Broadsword, id real ya usado por otros bloques
                bool idOk = porId.Count == 1 && porId[0].Id == 4;
                Console.WriteLine($"LIB-01-ID: '#4' -> {porId.Count} resultado(s) (esperado 1), id={(porId.Count > 0 ? porId[0].Id : -1)}, nombre='{(porId.Count > 0 ? porId[0].DisplayName : "-")}'");
                if (!idOk) Console.WriteLine("FALLO: LIB-01-ID - la busqueda por '#id' exacto no devuelve exactamente ese objeto");

                var porDosIds = Buscar("#4,#5");
                bool orOk = porDosIds.Count == 2 && porDosIds.Any(r => r.Id == 4) && porDosIds.Any(r => r.Id == 5);
                Console.WriteLine($"LIB-01-OR: '#4,#5' -> {porDosIds.Count} resultado(s) (esperado 2: la coma es OR), ids=[{string.Join(", ", porDosIds.Select(r => r.Id))}]");
                if (!orOk) Console.WriteLine("FALLO: LIB-01-OR - la coma dejo de comportarse como OR entre terminos");

                var rango = Buscar("#1-40");
                var rangoCorto = Buscar("#1-20");
                bool dentroDelRango = rango.All(r => r.Id >= 1 && r.Id <= 40);
                bool esSubconjunto = rangoCorto.All(r => rango.Any(x => x.Id == r.Id));
                bool rangoOk = rango.Count > 0 && dentroDelRango && esSubconjunto && rangoCorto.Count <= rango.Count;
                Console.WriteLine($"LIB-01-RANGO: '#1-40' -> {rango.Count} resultado(s), todos dentro del rango={dentroDelRango}; '#1-20' -> {rangoCorto.Count}, subconjunto del anterior={esSubconjunto} (esperado True en los dos)");
                if (!rangoOk) Console.WriteLine("FALLO: LIB-01-RANGO - la busqueda por '#a-b' no acota de verdad al rango de ids");

                // AND real: dos palabras del nombre REAL del objeto 4, sacadas del propio
                // catalogo (nada hardcodeado que se quede viejo si cambia una traduccion).
                string nombre4 = porId.Count == 1 ? porId[0].DisplayName : string.Empty;
                var palabras4 = LibrarySearchGrammar.Fold(nombre4)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(p => p.Length >= 3).Distinct().ToList();
                if (palabras4.Count >= 2)
                {
                    string p1 = palabras4[0], p2 = palabras4[^1];
                    var soloP1 = Buscar(p1);
                    var conAmbas = Buscar($"{p1} {p2}");
                    bool todasContienen = conAmbas.All(r => r.NameFolded.Contains(p1) && r.NameFolded.Contains(p2));
                    // Solo se puede exigir que el objeto 4 este entre los resultados si el
                    // conjunto NO esta topado (con el tope puesto puede haberse quedado fuera por
                    // orden, no por el filtro) - misma cautela que ya obligo a documentar L-c.
                    bool topado = conAmbas.Count >= 100;
                    bool contieneEl4 = topado || conAmbas.Any(r => r.Id == 4);
                    bool andOk = conAmbas.Count > 0 && todasContienen && contieneEl4 && conAmbas.Count <= soloP1.Count;
                    Console.WriteLine($"LIB-01-AND: '{p1} {p2}' -> {conAmbas.Count} resultado(s) (solo '{p1}' da {soloP1.Count}), todos contienen LAS DOS palabras={todasContienen}, incluye el objeto 4={contieneEl4}{(topado ? " (conjunto topado, no exigible)" : "")}");
                    if (!andOk) Console.WriteLine("FALLO: LIB-01-AND - el espacio dejo de comportarse como AND dentro de un termino");
                }
                else Console.WriteLine($"LIB-01-AND: el nombre real del objeto 4 ('{nombre4}') no da dos palabras de 3+ letras - omitido");

                string dano = LibrarySearchGrammar.Fold("daño");
                var porTooltip = Buscar(".daño");
                bool tooltipTodos = porTooltip.Count > 0 && porTooltip.All(r => (r.TooltipFolded ?? string.Empty).Contains(dano));
                bool tooltipNoEsNombre = porTooltip.Any(r => !r.NameFolded.Contains(dano));
                Console.WriteLine($"LIB-01-TOOLTIP: '.daño' -> {porTooltip.Count} resultado(s), todos con esa palabra en el TOOLTIP={tooltipTodos}, y alguno que NO la tiene en el nombre={tooltipNoEsNombre} (esperado True: si no, estaria buscando por nombre)");
                if (!tooltipTodos || !tooltipNoEsNombre) Console.WriteLine("FALLO: LIB-01-TOOLTIP - el prefijo '.' ya no busca en el tooltip real");

                var conTilde = Buscar("máscara");
                var sinTilde = Buscar("mascara");
                bool acentosOk = conTilde.Count > 0
                    && conTilde.Select(r => r.Id).OrderBy(i => i).SequenceEqual(sinTilde.Select(r => r.Id).OrderBy(i => i));
                Console.WriteLine($"LIB-01-ACENTOS: 'máscara' -> {conTilde.Count} resultado(s), 'mascara' -> {sinTilde.Count} (esperado los MISMOS ids: el plegado de acentos va en los dos sentidos)");
                if (!acentosOk) Console.WriteLine("FALLO: LIB-01-ACENTOS - buscar sin tilde ya no encuentra lo mismo que con tilde");

                var unaLetra = Buscar("e");
                Console.WriteLine($"LIB-01-1CHAR: 'e' (un solo caracter) -> {unaLetra.Count} resultado(s) (esperado 0: quirk REAL de Terrasavr, un termino de <2 caracteres se ignora entero)");
                if (unaLetra.Count != 0) Console.WriteLine("FALLO: LIB-01-1CHAR - un termino de un solo caracter dejo de ignorarse (deja de calcar la gramatica real)");

                // ---------- LIB-02: tope real de 100 resultados y su resumen ----------
                var amplia = Buscar("ar");
                string resumenAmplio = vm.Library.ResultsSummary;
                bool topeOk = amplia.Count == 100 && resumenAmplio.Contains("100");
                Console.WriteLine($"LIB-02-TOPE: busqueda amplia 'ar' -> Results={amplia.Count} (esperado 100, el tope real medido en L-c), resumen='{resumenAmplio}'");
                if (!topeOk) Console.WriteLine("FALLO: LIB-02-TOPE - el tope real de 100 resultados, o el resumen que lo anuncia, dejo de aplicarse");

                // ---------- LIB-03: restriccion de slot al abrir desde un slot ----------
                vm.Library.SearchText = string.Empty;
                WaitForDispatcher(300);
                vm.Library.ClearCategoryCommand.Execute(null);
                DoEvents();
                var slotTinte = vm.DyesContainer?.Slots.FirstOrDefault();
                if (slotTinte != null)
                {
                    slotTinte.ChooseFromLibraryCommand.Execute(null);
                    DoEvents();
                    var resTinte = vm.Library.Results.ToList();
                    bool todosValidos = resTinte.Count > 0 && resTinte.All(r => slotTinte.AcceptsItem(r.Id));
                    bool sinEspada = !resTinte.Any(r => r.Id == 4); // un arma NUNCA vale en un slot de tinte
                    bool sinTarjetasRaiz = !vm.Library.ShowRootCategoryCards;
                    bool conPildora = vm.Library.SlotRestrictionLabel != null;
                    Console.WriteLine($"LIB-03-SLOT: slot '{slotTinte.SlotRoleLabel}' -> {resTinte.Count} resultado(s), TODOS validos para el slot={todosValidos}, sin el arma id=4={sinEspada}, tarjetas de carpeta raiz ocultas={sinTarjetasRaiz}, pildora='{vm.Library.SlotRestrictionLabel}'");
                    if (!todosValidos || !sinEspada) Console.WriteLine("FALLO: LIB-03-SLOT - la Libreria ofrece objetos que el slot restringido no acepta");
                    if (!sinTarjetasRaiz || !conPildora) Console.WriteLine("FALLO: LIB-03-SLOT - abrir desde un slot restringido no reduce la vista (tarjetas raiz/pildora)");

                    // La restriccion tiene que seguir en pie CON una busqueda por texto encima -
                    // es el camino real del usuario (abre desde el slot y ademas escribe).
                    var resTinteBuscando = Buscar("ar");
                    bool sigueValido = resTinteBuscando.All(r => slotTinte.AcceptsItem(r.Id));
                    Console.WriteLine($"LIB-03-SLOT+BUSQUEDA: 'ar' con el slot de tinte esperando -> {resTinteBuscando.Count} resultado(s), todos validos={sigueValido}");
                    if (!sigueValido) Console.WriteLine("FALLO: LIB-03-SLOT+BUSQUEDA - buscar por texto se salta la restriccion del slot");

                    vm.Library.SearchText = string.Empty;
                    WaitForDispatcher(300);
                    vm.Library.CancelPickCommand.Execute(null);
                    DoEvents();
                }
                else Console.WriteLine("LIB-03-SLOT: sin contenedor de Tintes real cargado - omitido");

                // ---------- LIB-04: coherencia real del arbol construido ----------
                var serviceFieldLib = typeof(MainViewModel).GetField("_service", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var svcLib = (CharacterFileService)serviceFieldLib!.GetValue(vm)!;
                var idsRealesDelCatalogo = new HashSet<int>(svcLib.VanillaCatalog.AllEntries().Select(e => e.Id));
                foreach (var e in svcLib.CalamityCatalog.Entries) idsRealesDelCatalogo.Add(e.SyntheticId);

                int nodos = 0, hojas = 0, hojasPasadas = 0, nodosVacios = 0, unionesRotas = 0, profundidadMax = 0, fantasmas = 0;
                int hojasQueSeVenVacias = 0, objetosNoEnseñables = 0;
                string peorNombre = string.Empty, peorUnion = string.Empty, peorFantasma = string.Empty, peorHojaVacia = string.Empty;
                var fantasmaIds = new HashSet<int>();
                void RecorrerArbol(CategoryNodeViewModel n, int prof, bool esCalamity)
                {
                    nodos++;
                    profundidadMax = Math.Max(profundidadMax, prof);
                    if (n.Name.Length > peorNombre.Length) peorNombre = n.Name;
                    // El id 0 es RELLENO real del propio arbol de Terrasavr (rellena la rejilla
                    // hasta cuadrar la fila), no un objeto - no cuenta como fantasma.
                    foreach (int id in n.ItemIdsOrdered)
                        if (id > 0 && !idsRealesDelCatalogo.Contains(id) && fantasmaIds.Add(id))
                        {
                            fantasmas++;
                            if (peorFantasma.Length == 0) peorFantasma = $"id={id} en '{n.FullPath}'";
                        }
                    if (n.ItemIdsOrdered.Count == 0) nodosVacios++;
                    if (n.Children.Count == 0)
                    {
                        hojas++;
                        // El sintoma que de verdad ve el usuario de un id fantasma: una carpeta
                        // que anuncia N objetos y no pinta NI UNA tarjeta (LibraryViewModel
                        // descarta en silencio lo que el catalogo no conoce, ver su ApplyFilter).
                        int declara = n.ItemIdsOrdered.Count(id => id > 0);
                        int resolubles = n.ItemIdsOrdered.Count(id => id > 0 && idsRealesDelCatalogo.Contains(id));
                        objetosNoEnseñables += declara - resolubles;
                        if (declara > 0 && resolubles == 0)
                        {
                            hojasQueSeVenVacias++;
                            if (peorHojaVacia.Length == 0) peorHojaVacia = $"'{n.FullPath}' anuncia {declara} objetos y pintaria 0";
                        }
                        // El tope de paginacion (40) solo lo aplica el algoritmo agrupado real
                        // (Calamity, LibraryTreeBuilder.BuildGroupedRoot); el arbol vanilla trae
                        // sus hojas ya paginadas por el propio Terrasavr y no tiene por que
                        // respetar ese mismo numero.
                        if (esCalamity && n.ItemIdsOrdered.Count > 40) hojasPasadas++;
                    }
                    else
                    {
                        var union = new List<int>();
                        var vistos = new HashSet<int>();
                        foreach (var h in n.Children)
                            foreach (int id in h.ItemIdsOrdered)
                                if (vistos.Add(id)) union.Add(id);
                        if (!union.SequenceEqual(n.ItemIdsOrdered))
                        {
                            unionesRotas++;
                            if (peorUnion.Length == 0) peorUnion = $"'{n.FullPath}': el padre declara {n.ItemIdsOrdered.Count} ids y la union real de sus hijos son {union.Count}";
                        }
                        foreach (var h in n.Children) RecorrerArbol(h, prof + 1, esCalamity);
                    }
                }
                var raizCalamity = vm.Library.RootCategories.FirstOrDefault(c => c.FullPath == "Calamity");
                foreach (var raiz in vm.Library.RootCategories) RecorrerArbol(raiz, 0, ReferenceEquals(raiz, raizCalamity));
                Console.WriteLine($"LIB-04-ARBOL: {vm.Library.RootCategories.Count} carpetas raiz, {nodos} nodos, {hojas} hojas, profundidad maxima={profundidadMax}, " +
                                  $"objetos bajo 'Calamity (mod)'={raizCalamity?.ItemIdsOrdered.Count ?? -1}");
                Console.WriteLine($"LIB-04-ARBOL: hojas de Calamity por encima del tope de 40={hojasPasadas} (esperado 0), carpetas SIN un solo objeto={nodosVacios} (esperado 0), " +
                                  $"uniones padre!=hijos={unionesRotas} (esperado 0){(peorUnion.Length > 0 ? " - " + peorUnion : "")}, " +
                                  $"ids FANTASMA (el arbol los referencia y el catalogo no los conoce)={fantasmas}{(peorFantasma.Length > 0 ? " - primero: " + peorFantasma : "")}");
                Console.WriteLine($"LIB-04-ARBOL: carpetas hoja que anuncian objetos y pintarian CERO tarjetas={hojasQueSeVenVacias} (esperado 0){(peorHojaVacia.Length > 0 ? " - " + peorHojaVacia : "")}, " +
                                  $"objetos que el arbol declara y la Libreria no puede enseñar={objetosNoEnseñables}");
                Console.WriteLine($"LIB-04-ARBOL: nombre de carpeta mas largo del arbol entero = '{peorNombre}' ({peorNombre.Length} caracteres)");
                // Bug real de esta ronda: el arbol es un puerto literal de un Terrasavr con un
                // Terraria MAS NUEVO (ids hasta 6145) y el catalogo de nombres se quedo en 5455 -
                // 36 carpetas se veian completamente vacias. Ver
                // scripts/completar-nombres-objetos-145.js.
                if (hojasQueSeVenVacias > 0) Console.WriteLine("FALLO: LIB-04-ARBOL - hay carpetas de la Libreria que anuncian objetos y no pintan ni una sola tarjeta (ids que el catalogo de nombres no conoce)");
                if (hojasPasadas > 0) Console.WriteLine("FALLO: LIB-04-ARBOL - alguna hoja de Calamity supera el tope de paginacion de 40 (la paginacion dejo de aplicarse)");
                if (nodosVacios > 0) Console.WriteLine("FALLO: LIB-04-ARBOL - hay carpetas sin un solo objeto dentro (paginacion o agrupado roto)");
                if (unionesRotas > 0) Console.WriteLine("FALLO: LIB-04-ARBOL - la lista de una carpeta madre ya no es la union ordenada real de sus hijos");

                // Y que la paginacion se vea de verdad: la ULTIMA pagina de la categoria con mas
                // paginas tiene que renderizar EXACTAMENTE sus objetos (ni menos por ids que el
                // catalogo no resuelva, ni de mas).
                CategoryNodeViewModel? conMasPaginas = null;
                if (raizCalamity != null)
                {
                    void BuscarPaginada(CategoryNodeViewModel n)
                    {
                        if (n.Children.Count > 0 && n.Children.All(h => h.Children.Count == 0)
                            && n.Children.Count > (conMasPaginas?.Children.Count ?? 1)) conMasPaginas = n;
                        foreach (var h in n.Children) BuscarPaginada(h);
                    }
                    BuscarPaginada(raizCalamity);
                    if (conMasPaginas != null)
                    {
                        var ultima = conMasPaginas.Children[^1];
                        vm.Library.SearchText = string.Empty;
                        WaitForDispatcher(300);
                        vm.Library.SelectCategoryCommand.Execute(ultima);
                        DoEvents();
                        int esperados = Math.Min(ultima.ItemIdsOrdered.Count, 100);
                        bool paginaOk = vm.Library.Results.Count == esperados
                                        && vm.Library.Results.All(r => ultima.ItemIdSet.Contains(r.Id));
                        Console.WriteLine($"LIB-04-PAGINA: '{conMasPaginas.Name}' tiene {conMasPaginas.Children.Count} paginas; la ultima ('{ultima.Name}') declara {ultima.ItemIdsOrdered.Count} objetos y renderiza {vm.Library.Results.Count} (esperado {esperados}, y todos suyos)");
                        if (!paginaOk) Console.WriteLine("FALLO: LIB-04-PAGINA - una pagina de carpeta no muestra exactamente los objetos que declara");
                        vm.Library.ClearCategoryCommand.Execute(null);
                        DoEvents();
                    }
                    else Console.WriteLine("LIB-04-PAGINA: ninguna categoria de Calamity salio paginada - omitido");
                }

                // ---------- LIB-05: el arbol RENDERIZADO en columna estrecha ----------
                // La columna del arbol es de ancho FIJO (210px) a cualquier tamaño de ventana, y
                // cada nivel de profundidad se come 16px de sangria - con los nombres largos
                // reales de Calamity ("Colocables - Muebles de Navystone - Antiguos (N)") es el
                // sitio natural donde esperar contenido cortado. Se mide con el recorte real
                // acumulado de TODOS los ancestros (RectVisible), no a ojo.
                var plantillaArbol = window.TryFindResource("CategoryNodeTemplate") as DataTemplate;
                var arbolIC = Descendientes<ItemsControl>(window)
                    .FirstOrDefault(ic => ReferenceEquals(ic.ItemsSource, vm.Library.RootCategories)
                                          && ReferenceEquals(ic.ItemTemplate, plantillaArbol));
                if (arbolIC == null) Console.WriteLine("FALLO: LIB-05-ARBOL - no se encontro el ItemsControl real del arbol de la Libreria (¿cambio CategoryNodeTemplate?)");
                else if (raizCalamity != null)
                {
                    // Despliega la rama de Calamity (raiz + primer nivel) - el peor caso real de
                    // sangria + nombre largo que el usuario provoca con dos clics.
                    var expandidosAntes = new List<CategoryNodeViewModel>();
                    void Desplegar(CategoryNodeViewModel n, int prof, int maxProf)
                    {
                        if (prof > maxProf || n.Children.Count == 0) return;
                        if (!n.IsExpanded) { n.IsExpanded = true; expandidosAntes.Add(n); }
                        foreach (var h in n.Children) Desplegar(h, prof + 1, maxProf);
                    }
                    Desplegar(raizCalamity, 0, 1);
                    // Y ademas la rama concreta del nombre mas largo, hasta el fondo.
                    CategoryNodeViewModel? masLargo = null;
                    void BuscarLargo(CategoryNodeViewModel n)
                    {
                        if (masLargo == null || n.Name.Length > masLargo.Name.Length) masLargo = n;
                        foreach (var h in n.Children) BuscarLargo(h);
                    }
                    BuscarLargo(raizCalamity);
                    if (masLargo != null && masLargo.Children.Count > 0 && !masLargo.IsExpanded) { masLargo.IsExpanded = true; expandidosAntes.Add(masLargo); }
                    DoEvents(); DoEvents();

                    foreach (var (etiqueta, w, h) in new (string?, double, double)[]
                             { ("maximizada", 0, 0), (null, 1600, 1000), (null, 1400, 900), (null, 1180, 860), (null, 1080, 700) })
                    {
                        if (etiqueta == "maximizada") { window.WindowState = WindowState.Maximized; DoEvents(); DoEvents(); DoEvents(); }
                        else { window.WindowState = WindowState.Normal; FijarTamaño(window, w, h); }
                        DoEvents(); DoEvents();
                        string caso = etiqueta ?? $"{w:0}x{h:0}";

                        var filasArbol = Descendientes<Button>(arbolIC)
                            .Where(b => b.DataContext is CategoryNodeViewModel && b.IsVisible && b.ActualHeight > 0).ToList();
                        int enterasSinTocar = filasArbol.Count(f => VisibleEntero(f, window));

                        // El ancho SOLO se puede medir sobre una fila que este de verdad a la
                        // vista: una fila que ahora mismo cae fuera del ScrollViewer no tiene
                        // rectangulo visible ninguno, y contarla como "cortada a lo ancho" seria
                        // un falso positivo (le paso a la primera version de este bloque: daba
                        // 191 de 203 "cortadas" en TODOS los tamaños, incluida la maximizada).
                        // Por eso primero se hace el gesto real del usuario - BringIntoView - y
                        // solo despues se pregunta cuanto de la fila se ve.
                        int alcanzables = 0, cortadasAncho = 0, textosCortados = 0;
                        double peorCorteAncho = 0; string peorFilaAncho = string.Empty;
                        double anchoUtilDelMasLargo = -1;
                        foreach (var f in filasArbol)
                        {
                            f.BringIntoView();
                            DoEvents();
                            var visible = RectVisible(f, window);
                            if (VisibleEntero(f, window)) alcanzables++;
                            double faltaAncho = visible.IsEmpty ? f.ActualWidth : f.ActualWidth - visible.Width;
                            if (faltaAncho > 1)
                            {
                                cortadasAncho++;
                                if (faltaAncho > peorCorteAncho) { peorCorteAncho = faltaAncho; peorFilaAncho = ((CategoryNodeViewModel)f.DataContext).Name; }
                            }
                            var tb = Descendientes<TextBlock>(f).FirstOrDefault(t => t.IsVisible && t.ActualHeight > 0);
                            if (tb != null)
                            {
                                // Con TextWrapping="Wrap" un nombre largo NO se corta: crece a
                                // varias lineas. Lo que si seria un fallo real es que el
                                // rectangulo del propio texto no se vea entero (lo estaria
                                // tapando un ancestro) - eso si es contenido perdido.
                                var vTb = RectVisible(tb, window);
                                if (vTb.IsEmpty || vTb.Width < tb.ActualWidth - 1 || vTb.Height < tb.ActualHeight - 1) textosCortados++;
                                if (masLargo != null && ReferenceEquals(f.DataContext, masLargo)) anchoUtilDelMasLargo = tb.ActualWidth;
                            }
                        }
                        if (filasArbol.Count > 0) { filasArbol[0].BringIntoView(); DoEvents(); }

                        Console.WriteLine($"LIB-05-ARBOL: {caso} ({window.ActualWidth:0}x{window.ActualHeight:0}) -> {filasArbol.Count} filas de arbol renderizadas, " +
                                          $"enteras sin tocar nada={enterasSinTocar}, ALCANZABLES con scroll={alcanzables} (esperado {filasArbol.Count}), " +
                                          $"filas cortadas A LO ANCHO tras desplazarse hasta ellas={cortadasAncho} (esperado 0){(cortadasAncho > 0 ? $", peor: '{peorFilaAncho}' pierde {peorCorteAncho:0.#}px" : "")}, " +
                                          $"textos tapados por un ancestro={textosCortados} (esperado 0), " +
                                          $"ancho util del nombre mas largo ('{masLargo?.Name}')={anchoUtilDelMasLargo:0.#}px");
                        if (alcanzables < filasArbol.Count)
                            Console.WriteLine($"FALLO: LIB-05-ARBOL - a {caso}, {filasArbol.Count - alcanzables} fila(s) del arbol no se pueden alcanzar ni haciendo scroll");
                        if (cortadasAncho > 0)
                            Console.WriteLine($"FALLO: LIB-05-ARBOL - a {caso}, {cortadasAncho} fila(s) del arbol se cortan a lo ancho (la columna es fija y no hay scroll horizontal)");
                        if (textosCortados > 0)
                            Console.WriteLine($"FALLO: LIB-05-ARBOL - a {caso}, {textosCortados} nombre(s) de carpeta se ven a medias (los tapa un ancestro)");
                    }

                    // Captura real del peor caso (ventana en su suelo + rama de Calamity abierta).
                    window.WindowState = WindowState.Normal;
                    FijarTamaño(window, 1080, 700);
                    DoEvents(); DoEvents();
                    var rtbArbol = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbArbol.Render(window);
                    var encArbol = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encArbol.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbArbol));
                    using (var fsArbol = File.Create(Path.Combine(AppContext.BaseDirectory, "libreria-arbol-calamity-ventana-minima.png"))) encArbol.Save(fsArbol);
                    Console.WriteLine("Captura del arbol de Calamity desplegado en ventana minima -> libreria-arbol-calamity-ventana-minima.png");

                    foreach (var n in expandidosAntes) n.IsExpanded = false; // estado como estaba
                    DoEvents();
                }

                // ---------- LIB-06: el idioma de la Libreria, con el panel a la vista ----------
                FijarTamaño(window, 1180, 860);
                vm.Library.ClearCategoryCommand.Execute(null);
                vm.Library.SearchText = string.Empty;
                WaitForDispatcher(300);
                string resumenEs = vm.Library.ResultsSummary;
                vm.Settings.Language = "en";
                DoEvents(); DoEvents();
                string resumenEn = vm.Library.ResultsSummary;
                bool resumenCambia = resumenEs != resumenEn && resumenEn.Contains("items in total");
                Console.WriteLine($"LIB-06-IDIOMA: resumen (es)='{resumenEs}' -> (en)='{resumenEn}' (esperado que cambie de idioma SIN volver a buscar ni pulsar carpeta)");
                if (!resumenCambia) Console.WriteLine("FALLO: LIB-06-IDIOMA - la linea de resumen de la Libreria se queda congelada en el idioma anterior");

                var rotuloPlegar = Descendientes<TextBlock>(window)
                    .FirstOrDefault(tb => tb.IsVisible && (tb.Text.Contains("Plegar") || tb.Text.Contains("Desplegar") || tb.Text.Contains("Collapse") || tb.Text.Contains("Expand")));
                bool rotuloEnIngles = rotuloPlegar != null && (rotuloPlegar.Text.Contains("Collapse") || rotuloPlegar.Text.Contains("Expand"));
                Console.WriteLine($"LIB-06-IDIOMA: rotulo de plegar/desplegar de la cabecera de Libreria con la app en ingles = '{rotuloPlegar?.Text ?? "(no encontrado)"}' (esperado en INGLES)");
                if (!rotuloEnIngles) Console.WriteLine("FALLO: LIB-06-IDIOMA - el rotulo Plegar/Desplegar de la Libreria sigue en español con la app en ingles");
                vm.Settings.Language = "es";
                DoEvents();

                // ---------- BUFLIB-01/02: Libreria de buffs ----------
                vm.PersonajeInnerTabIndex = 1; // Buffs
                vm.IsBuffLibraryCollapsed = false;
                DoEvents(); DoEvents();
                vm.BuffLibrary.ClearCategoryCommand.Execute(null);
                vm.BuffLibrary.SearchText = string.Empty;
                WaitForDispatcher(300);

                var raicesBuff = vm.BuffLibrary.RootCategories.ToList();
                var indice = raicesBuff.FirstOrDefault(c => c.FullPath == "Indice");
                bool indiceConTilde = indice != null && indice.Children.Count > 0
                                      && indice.Children.All(p => p.Name.StartsWith("Índice", StringComparison.Ordinal));
                Console.WriteLine($"BUFLIB-01-ARBOL: {raicesBuff.Count} carpetas raiz [{string.Join(", ", raicesBuff.Select(c => c.Name))}]");
                Console.WriteLine($"BUFLIB-01-INDICE: {indice?.Children.Count ?? -1} paginas, todas escritas igual que su carpeta madre ('Índice', con tilde)={indiceConTilde} (esperado True)");
                if (!indiceConTilde) Console.WriteLine("FALLO: BUFLIB-01-INDICE - las paginas del indice de buffs se escriben distinto que su propia carpeta madre");

                var raizBuffCalamity = raicesBuff.FirstOrDefault(c => c.FullPath == "Calamity");
                if (raizBuffCalamity != null)
                {
                    vm.BuffLibrary.SelectCategoryCommand.Execute(raizBuffCalamity);
                    DoEvents();
                    int declara = raizBuffCalamity.ItemIdsOrdered.Count;
                    int esperadoBuff = Math.Min(declara, 300);
                    Console.WriteLine($"BUFLIB-01-TOPE: carpeta 'Calamity (mod)' declara {declara} buffs y renderiza {vm.BuffLibrary.Results.Count} (esperado {esperadoBuff}, el tope real de 300), resumen='{vm.BuffLibrary.ResultsSummary}'");
                    if (vm.BuffLibrary.Results.Count != esperadoBuff) Console.WriteLine("FALLO: BUFLIB-01-TOPE - el tope real de 300 resultados de la Libreria de buffs dejo de aplicarse");
                    vm.BuffLibrary.ClearCategoryCommand.Execute(null);
                    DoEvents();
                }

                // Misma gramatica real, sobre el universo de buffs.
                vm.BuffLibrary.SearchText = "#1";
                WaitForDispatcher(300);
                var buffPorId = vm.BuffLibrary.Results.ToList();
                bool buffIdOk = buffPorId.Count == 1 && buffPorId[0].Id == 1;
                Console.WriteLine($"BUFLIB-01-GRAMATICA: '#1' -> {buffPorId.Count} resultado(s) (esperado 1: '{(buffPorId.Count > 0 ? buffPorId[0].DisplayName : "-")}')");
                if (!buffIdOk) Console.WriteLine("FALLO: BUFLIB-01-GRAMATICA - la Libreria de buffs no comparte la gramatica real de busqueda por id");
                vm.BuffLibrary.SearchText = string.Empty;
                WaitForDispatcher(300);

                // Colocar un buff que YA esta puesto: PlaceBuff tiene que rechazarlo y el picker
                // quedarse abierto (Bu-b) - "avisar, no fingir que se coloco algo".
                if (vm.Buffs.Container != null && vm.Buffs.Container.Slots.Count > 1)
                {
                    var slot0 = vm.Buffs.Container.Slots[0];
                    var slotLibre = vm.Buffs.Container.Slots.FirstOrDefault(s => s.IsEmpty && !ReferenceEquals(s, slot0));
                    int buffPrevio = slot0.Buff.Id;
                    slot0.PlaceBuff(1);
                    DoEvents();
                    if (slotLibre != null)
                    {
                        slotLibre.ChooseFromLibraryCommand.Execute(null);
                        DoEvents();
                        vm.BuffLibrary.SearchText = "#1";
                        WaitForDispatcher(300);
                        var entrada1 = vm.BuffLibrary.Results.FirstOrDefault(r => r.Id == 1);
                        if (entrada1 != null)
                        {
                            vm.BuffLibrary.PlaceInTargetCommand.Execute(entrada1);
                            DoEvents();
                            bool rechazado = slotLibre.IsEmpty && vm.BuffLibrary.IsPicking && slotLibre.RejectionMessage != null;
                            Console.WriteLine($"BUFLIB-02-DUPLICADO: colocar el buff 1 en un segundo slot -> el slot sigue vacio={slotLibre.IsEmpty}, el picker sigue abierto={vm.BuffLibrary.IsPicking}, aviso real='{slotLibre.RejectionMessage}'");
                            if (!rechazado) Console.WriteLine("FALLO: BUFLIB-02-DUPLICADO - se pudo colocar dos veces el mismo buff, o el picker se cerro fingiendo que se coloco");
                            vm.BuffLibrary.CancelPickCommand.Execute(null);
                        }
                        else Console.WriteLine("BUFLIB-02-DUPLICADO: el buff 1 no aparece en el catalogo real - omitido");
                    }
                    vm.BuffLibrary.SearchText = string.Empty;
                    WaitForDispatcher(300);
                    if (buffPrevio == 0 && slot0.Buff.Id == 1) slot0.ClearCommand.Execute(null); // estado como estaba
                    DoEvents();
                }

                // ---------- BUILDS ----------
                vm.PersonajeInnerTabIndex = 0;
                vm.SelectedTabIndex = 2; // Builds
                DoEvents(); DoEvents();

                // "Ya lo tienes" tiene que mirar TODOS los contenedores reales, no solo el
                // Inventario (que es lo unico que BD-D-POSEIDO ejercita) - aqui se prueba en un
                // ALMACEN, y ademas que el dato se RECALCULE al quitarlo (no se quede pegado).
                var claseVanilla = vm.Builds.VanillaStages[0].Classes[0];
                var filaLibre = claseVanilla.AllRows.FirstOrDefault(r => r.ItemId != 0 && !r.IsOwned);
                var almacen = vm.StorageGroup?.Current;
                if (filaLibre != null && almacen != null && almacen.Slots.Any(s => s.IsEmpty))
                {
                    var hueco = almacen.Slots.First(s => s.IsEmpty);
                    hueco.PlaceItem(filaLibre.ItemId);
                    vm.SelectedTabIndex = 1; DoEvents();
                    vm.SelectedTabIndex = 2; DoEvents();
                    Console.WriteLine($"BUILDS-01-POSESION: '{filaLibre.DisplayName}' colocado en el almacen '{almacen.DisplayName}' -> IsOwned={filaLibre.IsOwned} (esperado True: 'ya lo tienes' recorre inventario + almacenes + equipo)");
                    if (!filaLibre.IsOwned) Console.WriteLine("FALLO: BUILDS-01-POSESION - un objeto guardado en un almacen no cuenta como 'ya lo tienes'");
                    hueco.ClearCommand.Execute(null);
                    vm.SelectedTabIndex = 1; DoEvents();
                    vm.SelectedTabIndex = 2; DoEvents();
                    Console.WriteLine($"BUILDS-01-POSESION: tras vaciar ese hueco del almacen -> IsOwned={filaLibre.IsOwned} (esperado False: el dato se recalcula de verdad, no se queda pegado)");
                    if (filaLibre.IsOwned) Console.WriteLine("FALLO: BUILDS-01-POSESION - 'ya lo tienes' se queda pegado tras quitar el objeto");
                }
                else Console.WriteLine("BUILDS-01-POSESION: sin fila/almacen real disponible - omitido");

                // Filtro de clase: "rogue" SOLO existe en Calamity (vanilla no tiene esa clase) -
                // filtrar por ella tiene que dejar Vanilla entero invisible, no vacio a medias.
                var opcionRogue = vm.Builds.ClassFilterOptions.FirstOrDefault(o => o.Key == "rogue");
                if (opcionRogue != null)
                {
                    vm.Builds.SelectClassFilterCommand.Execute(opcionRogue);
                    DoEvents();
                    bool vanillaOculta = vm.Builds.VanillaStages.All(s => !s.IsVisible);
                    bool calamityVisible = vm.Builds.CalamityStages.Any(s => s.IsVisible);
                    bool soloRogue = vm.Builds.CalamityStages.SelectMany(s => s.Classes).Where(c => c.IsVisible).All(c => c.ClassName == "rogue");
                    Console.WriteLine($"BUILDS-02-FILTRO: 'Pícaro' -> etapas vanilla todas ocultas={vanillaOculta} (esperado True: esa clase no existe en vanilla), alguna etapa de Calamity visible={calamityVisible}, y solo clases 'rogue' visibles={soloRogue}");
                    if (!vanillaOculta || !calamityVisible || !soloRogue) Console.WriteLine("FALLO: BUILDS-02-FILTRO - el filtro por clase no deja exactamente las clases de esa clase");
                    var todas = vm.Builds.ClassFilterOptions.First(o => o.Key == null);
                    vm.Builds.SelectClassFilterCommand.Execute(todas);
                    DoEvents();
                    bool vuelveTodo = vm.Builds.VanillaStages.All(s => s.IsVisible) && vm.Builds.CalamityStages.All(s => s.IsVisible);
                    Console.WriteLine($"BUILDS-02-FILTRO: volver a 'Todas' deja TODAS las etapas visibles={vuelveTodo} (esperado True)");
                    if (!vuelveTodo) Console.WriteLine("FALLO: BUILDS-02-FILTRO - volver al filtro 'Todas' no restaura todas las etapas");
                }
                else Console.WriteLine("BUILDS-02-FILTRO: no hay clase 'rogue' en el catalogo real - omitido");

                // Auto-equipar: UNA sola entrada de Deshacer que cubra Equipamiento E Inventario,
                // y que al deshacerla devuelva TODOS los slots tocados a como estaban.
                if (vm.EquipmentGroup != null && vm.InventoryContainer != null)
                {
                    var claseParaEquipar = vm.Builds.VanillaStages[^1].Classes[0];
                    var contenedoresTocados = vm.EquipmentGroup.AllContainers.Append(vm.InventoryContainer).ToList();
                    var antes = contenedoresTocados.SelectMany(c => c.Slots).Select(s => (Slot: s, Item: s.Item.Clone())).ToList();
                    int entradasAntes = vm.UndoStack.Entries.Count;
                    vm.AutoEquipCommand.Execute(claseParaEquipar.Source);
                    DoEvents();
                    int nuevasEntradas = vm.UndoStack.Entries.Count - entradasAntes;
                    int cambiados = antes.Count(p => !p.Item.ContentEquals(p.Slot.Item));
                    Console.WriteLine($"BUILDS-03-AUTOEQUIP: clase '{claseParaEquipar.ClassName}' de la ultima etapa vanilla -> {cambiados} slot(s) realmente cambiados, entradas de Deshacer nuevas={nuevasEntradas} (esperado exactamente 1), rotulo='{vm.UndoStack.NextUndoLabel}', mensaje='{vm.StatusMessage}'");
                    if (nuevasEntradas != 1) Console.WriteLine("FALLO: BUILDS-03-AUTOEQUIP - auto-equipar no deja EXACTAMENTE una entrada de Deshacer");
                    if (cambiados == 0) Console.WriteLine("FALLO: BUILDS-03-AUTOEQUIP - auto-equipar no cambio ni un solo slot real");

                    vm.UndoEditCommand.Execute(null);
                    DoEvents();
                    int siguenDistintos = antes.Count(p => !p.Item.ContentEquals(p.Slot.Item));
                    Console.WriteLine($"BUILDS-03-AUTOEQUIP: tras UN solo Deshacer quedan {siguenDistintos} slot(s) distintos de como estaban (esperado 0)");
                    if (siguenDistintos != 0) Console.WriteLine("FALLO: BUILDS-03-AUTOEQUIP - un unico Deshacer no revierte todo lo que auto-equipar toco");

                    // Bd-f: un build de Calamity sobre un personaje SIN datos de Calamity avisa.
                    var claseCalamity = vm.Builds.CalamityStages
                        .SelectMany(s => s.Classes)
                        .FirstOrDefault(c => c.Source.Armor.Concat(c.Source.Weapons).Concat(c.Source.Accessories).Any(i => i.Pid?.Contains('/') == true));
                    if (claseCalamity != null)
                    {
                        var antesCal = contenedoresTocados.SelectMany(c => c.Slots).Select(s => (Slot: s, Item: s.Item.Clone())).ToList();
                        vm.AutoEquipCommand.Execute(claseCalamity.Source);
                        DoEvents();
                        string aviso = LocalizationService.Instance["warning_no_calamity_data"];
                        bool avisaSiToca = vm.HasCalamityData || vm.StatusMessage.StartsWith(aviso, StringComparison.Ordinal);
                        Console.WriteLine($"BUILDS-04-AVISO-CALAMITY: HasCalamityData={vm.HasCalamityData}, mensaje='{vm.StatusMessage}' (esperado que EMPIECE por el aviso real solo si el personaje no tiene datos de Calamity)");
                        if (!avisaSiToca) Console.WriteLine("FALLO: BUILDS-04-AVISO-CALAMITY - colocar un build de Calamity en un personaje sin .tplr no avisa de nada");
                        vm.UndoEditCommand.Execute(null);
                        DoEvents();
                        int restanCal = antesCal.Count(p => !p.Item.ContentEquals(p.Slot.Item));
                        if (restanCal != 0) Console.WriteLine($"FALLO: BUILDS-04-AVISO-CALAMITY - deshacer el auto-equipar de Calamity dejo {restanCal} slot(s) sin revertir");
                    }
                }
            }
            finally
            {
                // Todo bloque que cambia estado PERSISTIDO (session.json: pestaña activa, plegado
                // de las dos Librerias) tiene que dejarlo como estaba - misma leccion ya escrita
                // por la ronda de las pildoras de Equipamiento y por AR-13c.
                vm.Library.SearchText = string.Empty;
                vm.Library.ClearCategoryCommand.Execute(null);
                vm.Library.CancelPickCommand.Execute(null);
                vm.BuffLibrary.SearchText = string.Empty;
                vm.BuffLibrary.ClearCategoryCommand.Execute(null);
                vm.BuffLibrary.CancelPickCommand.Execute(null);
                vm.Settings.Language = idiomaPrevioLib;
                vm.IsLibraryCollapsed = libPlegadaPrevio;
                vm.IsBuffLibraryCollapsed = bufPlegadaPrevio;
                vm.SelectedTabIndex = tabPrevioLib;
                vm.PersonajeInnerTabIndex = innerPrevioLib;
                window.WindowState = WindowState.Normal;
                FijarTamaño(window, anchoPrevioLib, altoPrevioLib);
                WaitForDispatcher(300);
            }
        }
        catch (Exception ex) { Console.WriteLine("LIB-BUILDS-EXCEPTION: " + ex); }
    }
}
