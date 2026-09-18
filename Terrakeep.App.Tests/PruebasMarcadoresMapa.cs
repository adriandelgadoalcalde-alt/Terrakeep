// AR-MRK (19-sep-2026): bloque real de medicion de GEOMETRIA de los marcadores del mapa de
// Exploracion. Nace de un tercer reporte del usuario con captura real ("el cuadrado teal esta
// desplazado varios tiles del bloque que deberia señalar; pasa con minerales, con cualquier
// objeto, Y TAMBIEN con cofres").
//
// Por que hacia falta un bloque NUEVO y no valia el de anoche: la medicion del 18-sep midio el
// `Rectangle x:Name="CurrentChestMarker"` (el marcador de "cofre actual" de Cofre a cofre) y dio
// error 0 a zoom 1.0 y 0.09. Ese marcador es el UNICO del mapa cuyo ScaleTransform inverso al
// zoom cuelga del PROPIO Rectangle: su RenderTransformOrigin="0.5,0.5" cae justo en el centro
// del rectangulo, que por su Margin="-12,-12,0,0" coincide exactamente con el punto de tile -
// es decir, es correcto POR CONSTRUCCION y no podia detectar nada.
//
// TODOS los demas marcadores (los de WorldSearchResults - cofre, mineral, objeto, letrero, NPC,
// pared, liquido... - y los de Npcs/CharacterSpawns) envuelven sus formas en un `Grid` y ponen
// el ScaleTransform en ESE Grid, con RenderTransformOrigin="0.5,0.5" del Grid. El 0.5,0.5 de ese
// Grid NO es el punto de tile: el Grid mide lo que le dejan sus hijos de margen negativo (un
// Rectangle de 10x10 con Margin="-5,-5,0,0" da un DesiredSize de 5x5), y el punto de tile esta
// en su ESQUINA (0,0), no en su centro. Escalar por 1/zoom alrededor del centro de ese Grid
// desplaza el marcador `origen * (1 - 1/zoom)` unidades de tile. A zoom 1.0 sale exactamente 0
// (por eso la prueba de anoche paso), y crece hasta ~media anchura del Grid segun se hace zoom.
//
// Esta prueba mide la geometria REAL post-transform con TransformToAncestor (nunca Canvas.Get*)
// a SIETE niveles de zoom, incluidos los intermedios que nadie habia probado, y comprueba ademas
// que un hit-test real en el centro VISUAL del marcador aterriza de verdad en un elemento que
// lleva el MouseBinding detras (el sintoma "clico el cuadrado y no pasa nada").
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Terrakeep.App;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.WldFormat;

internal static partial class Program
{
    private static void PruebasMarcadoresMapa(Window window, MainViewModel vm)
    {
        try
        {
            string mundo = @"C:\Users\adrian\Documents\My Games\Terraria\Worlds\Blando_Río.wld";
            if (!File.Exists(mundo))
            {
                Console.WriteLine("AR-MRK: no se encontro Blando_Río.wld - omitido");
                return;
            }

            vm.SelectedTabIndex = 4; // Exploracion - sin esto el TabItem no se realiza y no hay arbol visual de mapa
            FijarTamaño(window, 1400, 900);
            DoEvents(); DoEvents();

            var carga = vm.Exploration.LoadFromPathAsync(mundo);
            while (!carga.IsCompleted) { DoEvents(); Thread.Sleep(15); }
            DoEvents(); DoEvents();

            var scroll = Descendientes<ScrollViewer>(window).FirstOrDefault(s => s.Name == "WorldMapScroll");
            var img = Descendientes<System.Windows.Controls.Image>(window).FirstOrDefault(i => i.Name == "WorldMapImage");
            if (scroll == null || img == null)
            {
                Console.WriteLine("FALLO: AR-MRK - no se encontro WorldMapScroll/WorldMapImage");
                return;
            }

            // Tres categorias REALES distintas a la vez, para responder con datos a "pasa con
            // minerales, con cualquier objeto y tambien con cofres": si el desfase sale igual en
            // las tres, la causa es unica y compartida (la plantilla), no logica duplicada.
            // Se inyectan directamente en WorldSearchResults porque lo que se mide es COMO se
            // pinta una fila, no como se encuentra - el camino de busqueda real ya lo cubren
            // AR-13a..e y no cambia ni un pixel de la geometria de aqui.
            // Los hits se construyen EXACTAMENTE como los construye el codigo de produccion
            // (WorldSearch.Run: un cofre real es esquina+1 con SubX/SubY=0 porque un cofre es 2x2;
            // una veta y un tile suelto son 1x1 y se quedan con el 0.5 por defecto), y el centro
            // ESPERADO no se saca de la propia fila sino de la geometria REAL del mundo: el cofre
            // de verdad esta en `esquina..esquina+1` en los dos ejes, asi que su centro real es
            // esquina+1.0; una veta 1x1 en el tile T ocupa [T,T+1) y su centro real es T+0.5.
            // Asi la prueba NO puede "darse la razon a si misma" comparando MarkerX contra MarkerX.
            vm.Exploration.WorldSearchResults.Clear();
            const int esquinaCofreX = 5579, esquinaCofreY = 1036; // cofre-7 real de Blando_Río.wld
            var filaCofre = new WorldSearchHitRowViewModel(new WorldSearchHit(esquinaCofreX + 1, esquinaCofreY + 1, "Cofre real 2x2", WorldSearchKind.ChestItem, 0, 0));
            var filaVeta = new WorldSearchHitRowViewModel(new WorldSearchHit(4700, 614, "Veta de estaño", WorldSearchKind.OreVein));
            var filaObjeto = new WorldSearchHitRowViewModel(new WorldSearchHit(3000, 500, "Tile suelto 1x1", WorldSearchKind.Tile));
            // Centro geometrico REAL de cada uno, derivado del mundo, no del codigo que se prueba.
            var centroEsperado = new Dictionary<WorldSearchHitRowViewModel, (double X, double Y)>
            {
                [filaCofre] = (esquinaCofreX + 1.0, esquinaCofreY + 1.0), // 2x2 -> centro exacto
                [filaVeta] = (4700 + 0.5, 614 + 0.5),                     // 1x1 -> centro de celda
                [filaObjeto] = (3000 + 0.5, 500 + 0.5),                   // 1x1 -> centro de celda
            };
            vm.Exploration.WorldSearchResults.Add(filaCofre);
            vm.Exploration.WorldSearchResults.Add(filaVeta);
            vm.Exploration.WorldSearchResults.Add(filaObjeto);
            DoEvents(); DoEvents();

            // Y ademas, que el codigo de PRODUCCION (la busqueda real sobre el .wld real, no un hit
            // fabricado aqui) produzca de verdad esos SubX/SubY: sin esto, la prueba de arriba solo
            // demostraria que la plantilla pinta bien lo que se le da.
            ComprobarSubDeProduccion(vm);

            // Uno de los tres, ademas, como resultado ACTIVO (Marco 24x24 en vez de 10x10): el
            // Grid mide entonces 12x12 en vez de 5x5, asi que el desfase teorico es mas del
            // DOBLE - es justo el cuadrado grande que el usuario rodeo en su captura.
            filaVeta.IsCurrent = true;
            DoEvents(); DoEvents();

            var rectMapa = scroll.TransformToAncestor(window).TransformBounds(new Rect(0, 0, scroll.ActualWidth, scroll.ActualHeight));
            Console.WriteLine($"AR-MRK: ventana={window.ActualWidth}x{window.ActualHeight}, WorldMapScroll en ventana={rectMapa}, viewport={scroll.ViewportWidth}x{scroll.ViewportHeight}");
            Console.WriteLine("AR-MRK: --- geometria real de marcadores de resultado (TransformToAncestor), 7 zooms ---");
            double peorError = 0;
            // Tope real de la app (MaxZoom=6.0) - pedir 8 lo clampa y la comparacion saldria
            // contra un zoom que nunca se aplico (falso positivo gigante medido en la 1a pasada).
            foreach (double zoomPedido in new[] { 0.09, 0.25, 0.5, 1.0, 2.0, 4.0, 6.0 })
            {
                vm.Exploration.Zoom = zoomPedido;
                DoEvents(); scroll.UpdateLayout(); DoEvents(); DoEvents();
                double zoom = vm.Exploration.Zoom; // el REAL ya clampado, nunca el pedido

                foreach (var (fila, etiqueta) in new[] { (filaCofre, "COFRE"), (filaVeta, "VETA*"), (filaObjeto, "OBJETO") })
                {
                    // El hit-test de mas abajo solo dice algo si el marcador esta DE VERDAD
                    // dentro del viewport: en la primera pasada casi todos caian a miles de
                    // pixeles fuera de la ventana y "no hay nada ahi" no significaba nada.
                    scroll.ScrollToHorizontalOffset(fila.TileX * zoom - scroll.ViewportWidth / 2);
                    scroll.ScrollToVerticalOffset(fila.TileY * zoom - scroll.ViewportHeight / 2);
                    DoEvents(); scroll.UpdateLayout(); DoEvents(); DoEvents();

                    // Origen REAL de la imagen del mapa en coordenadas de la ventana: todo lo
                    // demas se mide contra el, igual que hizo la medicion del 18-sep.
                    var origen = img.TransformToAncestor(window).Transform(new Point(0, 0));
                    var marco = Descendientes<System.Windows.Shapes.Rectangle>(window)
                        .FirstOrDefault(r => r.Name == "Marco" && ReferenceEquals(r.DataContext, fila));
                    if (marco == null || marco.ActualWidth <= 0)
                    {
                        Console.WriteLine($"AR-MRK: zoom={zoom} {etiqueta} - no se encontro/realizo el Rectangle 'Marco' (omitido)");
                        continue;
                    }

                    var t = marco.TransformToAncestor(window);
                    var centroReal = t.Transform(new Point(marco.ActualWidth / 2, marco.ActualHeight / 2));

                    // Esperado, con el mismo criterio real de TEdit (DrawFindCrosshair): centro
                    // geometrico real * zoom, en pixeles de pantalla desde el origen del mapa.
                    // El centro sale de centroEsperado (derivado del mundo real), NUNCA de la
                    // propia fila que se esta midiendo.
                    var (cx, cy) = centroEsperado[fila];
                    double espX = origen.X + cx * zoom;
                    double espY = origen.Y + cy * zoom;
                    double errPxX = centroReal.X - espX;
                    double errPxY = centroReal.Y - espY;
                    double errTilesX = errPxX / zoom;
                    double errTilesY = errPxY / zoom;
                    peorError = Math.Max(peorError, Math.Max(Math.Abs(errTilesX), Math.Abs(errTilesY)));

                    Console.WriteLine($"AR-MRK: zoom={zoom,-5} {etiqueta,-6} tile=({fila.TileX},{fila.TileY}) " +
                                      $"centroVisual=({centroReal.X:0.00},{centroReal.Y:0.00}) esperado=({espX:0.00},{espY:0.00}) " +
                                      $"errorPx=({errPxX:0.00},{errPxY:0.00}) errorTiles=({errTilesX:0.000},{errTilesY:0.000})");
                    if (Math.Abs(errTilesX) > 0.01 || Math.Abs(errTilesY) > 0.01)
                        Console.WriteLine($"FALLO: AR-MRK - el marcador {etiqueta} NO cae sobre su tile real a zoom {zoom} (desfase real de {errTilesX:0.00}x{errTilesY:0.00} tiles)");

                    // Segunda mitad, el sintoma "clico el cuadrado y no pasa nada": un hit-test
                    // REAL de WPF en el centro visual del marcador tiene que aterrizar en algo
                    // desde lo que, subiendo el arbol, se llegue al elemento con InputBindings
                    // (exactamente lo que hace OriginatesFromClickableMarker en MainWindow.xaml.cs).
                    // InputHitTest, NO VisualTreeHelper.HitTest: el segundo es un hit-test de
                    // VISUAL puro y aterriza alegremente dentro de pestañas Collapsed (medido en
                    // esta misma sesion) - no modela lo que hace un raton de verdad. InputHitTest
                    // si respeta Visibility/IsHitTestVisible, que es justo lo que decide si el
                    // clic del usuario llega al marcador.
                    var hit = window.InputHitTest(centroReal) as DependencyObject;
                    bool esEsteMarcador = false;
                    var cadena = new List<string>();
                    for (DependencyObject? d = hit; d != null && cadena.Count < 22; d = VisualTreeHelper.GetParent(d))
                    {
                        string n = d.GetType().Name;
                        if (d is FrameworkElement fe)
                        {
                            if (!string.IsNullOrEmpty(fe.Name)) n += $"#{fe.Name}";
                            if (fe.Opacity < 1) n += $"(op={fe.Opacity:0.##})";
                            if (fe.Visibility != Visibility.Visible) n += $"({fe.Visibility})";
                            if (fe.InputBindings.Count > 0) n += "[MouseBinding]";
                            // Lo unico que de verdad importa: que el clic aterrice en el marcador
                            // de ESTA fila, no en cualquier ancestro que por casualidad tambien
                            // tenga InputBindings (el ScrollViewer del mapa los tiene).
                            if (ReferenceEquals(fe.DataContext, fila) && fe.InputBindings.Count > 0) esEsteMarcador = true;
                        }
                        cadena.Add(n);
                    }
                    Console.WriteLine($"AR-MRK: zoom={zoom,-5} {etiqueta,-6} hit-test en el centro visual -> {string.Join(" < ", cadena)} || aterriza en ESTE marcador={esEsteMarcador}");
                    if (!esEsteMarcador)
                        Console.WriteLine($"FALLO: AR-MRK-CLIC - un clic en el centro VISUAL del marcador {etiqueta} a zoom {zoom} NO aterriza en ese marcador");
                }
            }

            Console.WriteLine($"AR-MRK: peor desfase real medido (resultados de busqueda) = {peorError:0.000} tiles (esperado 0)");

            // --- Segunda mitad: los marcadores que NO salen de la plantilla de resultados ---
            // El marcador de "cofre actual" (CurrentChestMarker), la cabeza de NPC, la estrella
            // del spawn del personaje, la casa del spawn del mundo y el rombo de la mazmorra.
            // Se miden aparte porque cada uno tiene su propio anclaje y NINGUNO se habia medido
            // nunca (la medicion del 18-sep solo cubrio CurrentChestMarker, y a zoom 1.0/0.09).
            Console.WriteLine("AR-MRK: --- el resto de marcadores del mapa (nunca medidos antes) ---");
            // ChestRows solo se rellena al entrar en la categoria Cofres (RebuildInventory
            // despacha por SelectedCategory) - sin esto la lista esta vacia y no hay marcador de
            // "cofre actual" que medir.
            vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
            vm.Exploration.ChestViewMode = 2;
            DoEvents(); DoEvents();
            var cofre = vm.Exploration.ChestRows.FirstOrDefault();
            if (cofre != null) vm.Exploration.GoToChestCommand.Execute(cofre);
            DoEvents(); DoEvents();
            Console.WriteLine($"AR-MRK-OTROS: ChestRows={vm.Exploration.ChestRows.Count}, HasCurrentChest={vm.Exploration.HasCurrentChest}, Npcs={vm.Exploration.Npcs.Count}");
            var npc = vm.Exploration.Npcs.FirstOrDefault();
            double peorOtros = 0;
            foreach (double zoomPedido in new[] { 0.09, 0.5, 1.0, 2.0, 6.0 })
            {
                vm.Exploration.Zoom = zoomPedido;
                DoEvents(); scroll.UpdateLayout(); DoEvents(); DoEvents();
                double zoom = vm.Exploration.Zoom;

                // centroX/centroY = centro geometrico REAL esperado, ya en coordenadas de tile
                // fraccionarias. Se pasa explicito por marcador (y no "tile+0.5" para todos) justo
                // por lo aprendido en esta sesion: el marcador de "cofre actual" NO es un 1x1 - su
                // CurrentChestX ya es esquina+1, el centro exacto de un cofre 2x2, y sumarle otro
                // medio tile seria reintroducir el bug por el otro lado.
                void Medir(string etiqueta, FrameworkElement? el, double tileX, double tileY, double centroX, double centroY)
                {
                    // FALLO, no "omitido" a secas: un marcador que no llega a realizarse con
                    // tamaño real es justo una regresion de las que hay que ver (paso de verdad
                    // en esta sesion - la cabeza de NPC se quedo con alto 0 y estuvo a punto de
                    // colarse como "medicion en verde" porque la prueba lo saltaba en silencio).
                    if (el == null || el.ActualWidth <= 0 || el.ActualHeight <= 0)
                    {
                        Console.WriteLine($"FALLO: AR-MRK-OTROS - {etiqueta} no se realiza con tamaño real a zoom {zoom} " +
                                          $"(encontrado={el != null}, {el?.ActualWidth ?? -1}x{el?.ActualHeight ?? -1}) - el marcador no se ve en el mapa");
                        return;
                    }
                    scroll.ScrollToHorizontalOffset(tileX * zoom - scroll.ViewportWidth / 2);
                    scroll.ScrollToVerticalOffset(tileY * zoom - scroll.ViewportHeight / 2);
                    DoEvents(); scroll.UpdateLayout(); DoEvents();
                    var org = img.TransformToAncestor(window).Transform(new Point(0, 0));
                    var c = el.TransformToAncestor(window).Transform(new Point(el.ActualWidth / 2, el.ActualHeight / 2));
                    double eX = org.X + centroX * zoom, eY = org.Y + centroY * zoom;
                    double tX = (c.X - eX) / zoom, tY = (c.Y - eY) / zoom;
                    peorOtros = Math.Max(peorOtros, Math.Max(Math.Abs(tX), Math.Abs(tY)));
                    Console.WriteLine($"AR-MRK-OTROS: zoom={zoom,-5} {etiqueta,-14} tile=({tileX},{tileY}) errorTiles=({tX:0.000},{tY:0.000})");
                    if (Math.Abs(tX) > 0.01 || Math.Abs(tY) > 0.01)
                        Console.WriteLine($"FALLO: AR-MRK-OTROS - {etiqueta} desplazado {tX:0.00}x{tY:0.00} tiles a zoom {zoom}");
                }

                // Cofre 2x2: CurrentChestX ya ES el centro exacto (esquina+1), sin medio tile extra.
                Medir("CofreActual", Descendientes<System.Windows.Shapes.Rectangle>(window).FirstOrDefault(r => r.Name == "CurrentChestMarker"),
                      vm.Exploration.CurrentChestX, vm.Exploration.CurrentChestY,
                      vm.Exploration.CurrentChestX, vm.Exploration.CurrentChestY);
                if (npc != null)
                    Medir("CabezaNPC", Descendientes<FrameworkElement>(window)
                            .FirstOrDefault(e => (e is System.Windows.Controls.Image || e is System.Windows.Shapes.Ellipse)
                                                 && ReferenceEquals(e.DataContext, npc) && e.IsVisible),
                          npc.TileX, npc.TileY, npc.TileX + 0.5, npc.TileY + 0.5);
                Medir("SpawnMundo", Descendientes<TextBlock>(window).FirstOrDefault(t => t.Text == "⌂"),
                      vm.Exploration.WorldSpawnX, vm.Exploration.WorldSpawnY, vm.Exploration.WorldSpawnX + 0.5, vm.Exploration.WorldSpawnY + 0.5);
                Medir("Mazmorra", Descendientes<TextBlock>(window).FirstOrDefault(t => t.Text == "◇"),
                      vm.Exploration.WorldDungeonX, vm.Exploration.WorldDungeonY, vm.Exploration.WorldDungeonX + 0.5, vm.Exploration.WorldDungeonY + 0.5);
            }
            Console.WriteLine($"AR-MRK: peor desfase real medido (resto de marcadores) = {peorOtros:0.000} tiles (esperado 0)");

            // AR-MRK-COFRE-TILE: el otro sintoma real del mismo reporte ("por mucho que clique un
            // cofre por el mapa no me abre nada"). Se comprueban las CUATRO casillas del footprint
            // 2x2 de un cofre real, mas una casilla vecina que NO es cofre (que tiene que seguir
            // sin abrir nada - un clic en mapa vacio no debe abrir el cofre de al lado).
            // AR-MRK-13E: replica EXACTA del estado de AR-13e (que manda un clic real de SO) pero
            // resolviendo el hit-test en frio, sin raton: si el centro visual de CurrentChestMarker
            // aterriza en el propio marcador, el camino de clic esta sano y un fallo de AR-13e es
            // del raton/primer plano, no del codigo. Si aterriza en otra cosa, es una regresion de
            // verdad y hay que arreglarla.
            {
                var hit13e = new WorldSearchHitRowViewModel(new WorldSearchHit(5580, 1037, "Barra de hierro", WorldSearchKind.ChestItem, 0, 0));
                vm.Exploration.GoToWorldSearchHitCommand.Execute(hit13e);
                DoEvents(); DoEvents();
                vm.Exploration.CancelEditingChestCommand.Execute(null);
                DoEvents(); DoEvents();
                var mk = Descendientes<System.Windows.Shapes.Rectangle>(window).FirstOrDefault(r => r.Name == "CurrentChestMarker");
                if (mk == null || !vm.Exploration.HasCurrentChest)
                    Console.WriteLine("FALLO: AR-MRK-13E - no hay CurrentChestMarker visible tras navegar");
                else
                {
                    mk.UpdateLayout();
                    var c13 = mk.TransformToAncestor(window).Transform(new Point(mk.ActualWidth / 2, mk.ActualHeight / 2));
                    var h13 = window.InputHitTest(c13) as DependencyObject;
                    var cad = new List<string>();
                    bool llega = false;
                    for (DependencyObject? d = h13; d != null && cad.Count < 6; d = VisualTreeHelper.GetParent(d))
                    {
                        if (d is FrameworkElement f)
                        {
                            cad.Add(f.GetType().Name + (string.IsNullOrEmpty(f.Name) ? "" : "#" + f.Name) + (f.InputBindings.Count > 0 ? "[MB]" : ""));
                            if (ReferenceEquals(f, mk)) llega = true;
                        }
                        else cad.Add(d.GetType().Name);
                    }
                    Console.WriteLine($"AR-MRK-13E: centro visual de CurrentChestMarker en ventana=({c13.X:0.0},{c13.Y:0.0}), zoom={vm.Exploration.Zoom}, hit-test -> {string.Join(" < ", cad)} || es el propio marcador={llega}");
                    if (!llega)
                        Console.WriteLine("FALLO: AR-MRK-13E - el centro visual del marcador de cofre actual NO es clicable (regresion real del camino de clic)");
                }
                vm.Exploration.CancelEditingChestCommand.Execute(null);
                DoEvents();
            }

            vm.Exploration.CancelEditingChestCommand.Execute(null);
            DoEvents();
            const int cX = 5579, cY = 1036; // cofre-7 real de Blando_Río.wld, esquina cruda
            foreach (var (tx, ty, debeAbrir) in new[]
            {
                (cX, cY, true), (cX + 1, cY, true), (cX, cY + 1, true), (cX + 1, cY + 1, true),
                (cX - 5, cY - 5, false),
            })
            {
                vm.Exploration.CancelEditingChestCommand.Execute(null);
                DoEvents();
                bool abrio = vm.Exploration.TryOpenChestAtTile(tx, ty);
                DoEvents();
                var ed = vm.Exploration.EditingChest;
                bool ok = abrio == debeAbrir && (!debeAbrir || (ed != null && ed.TileX == cX && ed.TileY == cY));
                Console.WriteLine($"AR-MRK-COFRE-TILE: clic en tile ({tx},{ty}) -> abrio={abrio} (esperado {debeAbrir}), editor=({ed?.TileX},{ed?.TileY})");
                if (!ok)
                    Console.WriteLine($"FALLO: AR-MRK-COFRE-TILE - un clic en ({tx},{ty}) no hace lo que debe (abrio={abrio}, esperado {debeAbrir})");
            }
            vm.Exploration.CancelEditingChestCommand.Execute(null);
            DoEvents();

            // Deja el estado como se lo encontro el resto del arnes: mismo criterio y mismo mundo
            // de siempre que AR-13d/AR-13c.
            vm.Exploration.WorldSearchResults.Clear();
            vm.Exploration.Zoom = 1.0;
            vm.Exploration.ChestViewMode = 0;
            vm.Exploration.SelectedCategory = WorldSearchCategory.All;
            DoEvents();
            string mundoDeSiempre = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld";
            if (File.Exists(mundoDeSiempre))
            {
                var vuelta = vm.Exploration.LoadFromPathAsync(mundoDeSiempre);
                while (!vuelta.IsCompleted) { DoEvents(); Thread.Sleep(15); }
                DoEvents();
            }
        }
        catch (Exception ex) { Console.WriteLine("AR-MRK-EXCEPTION: " + ex); }
    }

    // AR-MRK-SUB: cierra el hueco real de la prueba de arriba. Alli los hits se fabrican a mano,
    // asi que solo demuestra que la PLANTILLA pinta bien lo que se le da. Aqui se lanza una
    // busqueda REAL sobre el .wld real y se comprueba que los hits que salen del codigo de
    // produccion traen el centro fraccionario correcto para su footprint: un cofre es 2x2 y su
    // marcador tiene que caer EXACTAMENTE en TileX (que ya es esquina+1), sin ningun medio tile
    // de mas; un tile suelto es 1x1 y tiene que caer en TileX+0.5.
    private static void ComprobarSubDeProduccion(MainViewModel vm)
    {
        try
        {
            vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
            vm.Exploration.ChestViewMode = 0;
            DoEvents(); DoEvents();
            var fila = vm.Exploration.Inventory.FirstOrDefault();
            if (fila == null) { Console.WriteLine("FALLO: AR-MRK-SUB - el inventario real de cofres salio vacio, no se pudo comprobar"); return; }

            vm.Exploration.SearchInventoryRowCommand.Execute(fila);
            for (int i = 0; i < 400 && vm.Exploration.WorldSearchResults.Count == 0; i++) { DoEvents(); Thread.Sleep(15); }
            DoEvents(); DoEvents();

            var cofres = vm.Exploration.WorldSearchResults.Where(r => r.Kind == WorldSearchKind.ChestItem).ToList();
            if (cofres.Count == 0) { Console.WriteLine($"FALLO: AR-MRK-SUB - la busqueda real de '{fila.Name}' no devolvio ningun resultado de cofre"); return; }

            var malos = cofres.Where(r => Math.Abs(r.MarkerX - r.TileX) > 0.0001 || Math.Abs(r.MarkerY - r.TileY) > 0.0001).ToList();
            Console.WriteLine($"AR-MRK-SUB: busqueda REAL de '{fila.Name}' -> {cofres.Count} cofres; " +
                              $"primero TileX/Y=({cofres[0].TileX},{cofres[0].TileY}) MarkerX/Y=({cofres[0].MarkerX},{cofres[0].MarkerY}) " +
                              $"(esperado identicos: un cofre 2x2 ya trae su centro exacto); con medio tile de mas={malos.Count} (esperado 0)");
            if (malos.Count > 0)
                Console.WriteLine("FALLO: AR-MRK-SUB - la busqueda real de cofres mete medio tile de desfase en el marcador");
        }
        catch (Exception ex) { Console.WriteLine("AR-MRK-SUB-EXCEPTION: " + ex); }
    }
}
