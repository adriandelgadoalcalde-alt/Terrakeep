// AR-14 / AR-14b: LA FILA FUSIONADA DE EQUIPAMIENTO (Mascota-Montura+Tinte | Armadura/Accesorios |
// Monedas/Municion) MEDIDA EN COORDENADAS REALES, TAMAÑO A TAMAÑO.
//
// Vive en su propio fichero, y ya no dentro del Main() de Program.cs, por el mismo motivo real y
// medido que AR-LAY (ver la cabecera de AuditoriaMaquetacion.cs): Program.cs pasa de 6.800 lineas
// y varias sesiones lo tocan a la vez. Como clase parcial de Program sigue viendo TODOS sus
// helpers (Descendientes, FijarTamaño, DoEvents...) sin duplicar una linea, y de paso permite el
// modo de foco AR14_SOLO=1, que corre SOLO esta auditoria sobre el personaje ya cargado y sale -
// imprescindible para iterar sobre el reparto de esta fila sin pagar los ~10 minutos y las
// decenas de RenderTargetBitmap del recorrido completo (mismo motivo real que PB_SOLO).
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TerrasavrNative.App.Controls;
using TerrasavrNative.App.ViewModels;

internal static partial class Program
{
    private static void AuditoriaFilaFusionadaEquipamiento(Window window, MainViewModel vm)
    {
        // AR-14 (6-sep-2026, queja real del usuario con captura: "los slots de accesorios se
        // vuelven a solapar con las monedas/municion a pantalla mas pequeña"). La septima pasada
        // ya arreglo un solape de esta misma fila fusionada (2-sep-2026, columnas "2*/5*/4*" ->
        // Auto+MinWidth/*) pero NUNCA se dejo una comprobacion permanente que lo midiera: se
        // verifico mirando capturas. Esto es esa comprobacion que faltaba.
        //
        // Se mide en COORDENADAS REALES, celda a celda: TranslatePoint ignora el recorte, asi que
        // un slot que invade el lateral de Monedas/Municion se detecta igual aunque el
        // ClipToBounds del SlotRowHost lo este tapando (que es exactamente lo que el usuario ve:
        // el ultimo accesorio CORTADO justo donde empieza la caja de "Monedas"). Barrido de
        // anchos entre el MinWidth real declarado y una ventana grande, incluidos los dos lados
        // de cada umbral real de SizeClass (1320 Normal, 1500 Amplio, 1920 Extra).
        try
        {
            static Rect RectEn(FrameworkElement fe, FrameworkElement host)
            {
                var p = fe.TranslatePoint(new Point(0, 0), host);
                return new Rect(p.X, p.Y, fe.ActualWidth, fe.ActualHeight);
            }

            int kindOriginal = (int)(vm.EquipmentGroup?.SelectedKind ?? EquipmentKind.Items);
            var tamaños = new List<(double, double)>();
            if (Environment.GetEnvironmentVariable("AR14_BARRIDO_FINO") == "1")
                for (double a = 1080; a <= 1920; a += 20) tamaños.Add((a, 760.0));
            // "2" = biseccion real de 2 en 2px alrededor del umbral de SizeClass.Amplio - es como
            // se midio el valor real de AmplioMinWidth (ver MainViewModel): el primer ancho en que
            // las 3 vistas de Equipamiento caben de verdad sin recortarse entre ellas.
            else if (Environment.GetEnvironmentVariable("AR14_BARRIDO_FINO") == "2")
                for (double a = 1480; a <= 1600; a += 2) tamaños.Add((a, 860.0));
            // "3" (AR-14c, 6-sep-2026) = barrido en DOS DIMENSIONES. El corte de Monedas/Municion
            // no es de ancho sino de ALTO, asi que un barrido de anchos a un alto fijo (los dos de
            // arriba) no puede encontrar su umbral real: el mismo ancho falla o no segun lo alta
            // que sea la ventana. Rejilla completa ancho x alto, acotable con AR14_DESDE/HASTA/
            // PASO y AR14_ALTO_DESDE/HASTA/PASO.
            else if (Environment.GetEnvironmentVariable("AR14_BARRIDO_FINO") == "3")
            {
                static double Env(string n, double porDefecto) =>
                    double.TryParse(Environment.GetEnvironmentVariable(n), out double v) ? v : porDefecto;
                for (double a = Env("AR14_DESDE", 1080); a <= Env("AR14_HASTA", 1600); a += Env("AR14_PASO", 20))
                    for (double al = Env("AR14_ALTO_DESDE", 700); al <= Env("AR14_ALTO_HASTA", 1000); al += Env("AR14_ALTO_PASO", 20))
                        tamaños.Add((a, al));
            }
            else
                tamaños.AddRange(new[]
                {
                    // Los dos lados de cada umbral real de SizeClass (1320 Normal, 1520 Amplio,
                    // 1920 Extra) - cruzar uno reorganiza esta fila entera, y es justo donde el
                    // reparto puede quedarse corto.
                    (1080.0, 700.0), (1120.0, 760.0), (1180.0, 860.0), (1240.0, 800.0), (1319.0, 860.0),
                    (1320.0, 860.0), (1400.0, 860.0), (1500.0, 860.0), (1519.0, 860.0), (1520.0, 860.0),
                    (1560.0, 860.0), (1600.0, 900.0), (1700.0, 900.0), (1919.0, 1000.0), (1920.0, 1000.0),
                });
            bool libreriaOriginal = vm.IsLibraryCollapsed;
            vm.IsLibraryCollapsed = Environment.GetEnvironmentVariable("AR14_LIBRERIA_PLEGADA") == "1" || libreriaOriginal;

            // Dos barridos, ascendente y DESCENDENTE (el orden importa de verdad: las dos columnas
            // laterales son "Auto" y su contenido -SlotGridPanel- se mide contra ReferenceWidth =
            // ActualWidth del propio SlotRowHost, o sea contra el resultado del layout ANTERIOR;
            // encoger desde una ventana grande no tiene por que dar el mismo reparto que crecer
            // hasta el mismo ancho, y el usuario reporta el bug ENCOGIENDO desde maximizada).
            foreach (var (w, h) in tamaños.Concat(Enumerable.Reverse(tamaños)))
            {
                // Gesto REAL del usuario: la app se usa maximizada y se restaura a un tamaño
                // intermedio. Es la unica forma de que el layout llegue a cada ancho DESDE una
                // ventana grande, que es lo que hace que las columnas "Auto" laterales lleguen
                // con un DesiredSize calculado contra un ReferenceWidth mucho mayor.
                if (Environment.GetEnvironmentVariable("AR14_MAXIMIZAR") == "1")
                {
                    window.WindowState = System.Windows.WindowState.Maximized;
                    DoEvents(); DoEvents();
                    window.WindowState = System.Windows.WindowState.Normal;
                    DoEvents();
                }
                FijarTamaño(window, w, h);
                vm.SelectedTabIndex = 1;
                vm.PersonajeInnerTabIndex = 0;
                vm.ObjetosSubTabIndex = 0; // Equipamiento
                DoEvents();
                // Mismo estado exacto de la captura del usuario: conjunto activo + vista "Armadura"
                // (la unica de las 3 que lleva los 7 accesorios reales).
                var armadura = vm.EquipmentGroup?.KindOptions.FirstOrDefault(o => o.Value == (int)EquipmentKind.Items);
                if (armadura != null) vm.EquipmentGroup!.SelectKindCommand.Execute(armadura);
                DoEvents(); DoEvents();

                var host = Descendientes<SlotRowHost>(window).FirstOrDefault();
                if (host == null) { Console.WriteLine($"FALLO: AR-14 - a {w:0}x{h:0} no hay SlotRowHost en el arbol visual (la fila fusionada de Equipamiento no se esta renderizando)"); continue; }

                // Los dos bloques laterales POR SU NOMBRE de XAML, no por su celda del Grid
                // (AR-14c, 6-sep-2026): desde que Monedas/Municion se apila DEBAJO del centro en
                // las ventanas estrechas, su celda ya no es fija - buscarlo por "columna 2" lo
                // habria dado por ausente justo en el caso que hay que vigilar, y el bloque se
                // habria saltado la medicion entera sin decir nada.
                FrameworkElement? cajaMonedas = null, cajaMascotas = null;
                var celdasCentro = new List<(FrameworkElement Fe, Rect R, ScrollViewer? Sv)>();
                foreach (var hijo in host.Children.OfType<FrameworkElement>())
                {
                    int col = Grid.GetColumn(hijo);
                    if (!hijo.IsVisible) continue;
                    if (hijo.Name == "CajaMonedasMunicion") cajaMonedas = hijo;
                    else if (hijo.Name == "CajaMascotasTintes") cajaMascotas = hijo;
                    else if (col == 1 && Grid.GetRow(hijo) == 1)
                    {
                        foreach (var sgp in Descendientes<SlotGridPanel>(hijo))
                        {
                            // El ScrollViewer que de verdad RECORTA esta rejilla (el de
                            // ContainerCompactTemplate) - es el que decide si un slot que se sale
                            // se ve cortado o no, y con HorizontalScrollBarVisibility="Disabled"
                            // lo que se sale por la derecha no se puede alcanzar de ninguna forma.
                            ScrollViewer? svPropio = null;
                            for (var d = (DependencyObject)sgp; d != null && !ReferenceEquals(d, host); d = System.Windows.Media.VisualTreeHelper.GetParent(d))
                                if (d is ScrollViewer s) { svPropio = s; break; }
                            foreach (var celda in sgp.Children.OfType<FrameworkElement>())
                                celdasCentro.Add((celda, RectEn(celda, host), svPropio));
                        }
                    }
                }

                if (cajaMonedas == null || celdasCentro.Count == 0)
                {
                    Console.WriteLine($"FALLO: AR-14 - a {w:0}x{h:0} no se pudo medir (Monedas={cajaMonedas != null}, celdas centro={celdasCentro.Count})");
                    continue;
                }

                var rMonedas = RectEn(cajaMonedas, host);
                var rMascotas = cajaMascotas != null ? RectEn(cajaMascotas, host) : Rect.Empty;
                double derechaCentro = celdasCentro.Max(c => c.R.Right);
                double izquierdaCentro = celdasCentro.Min(c => c.R.Left);
                // Solape REAL por eje (no Rect.Intersect a secas - leccion ya documentada): dos
                // bloques de la misma fila solo se solapan de verdad si se pisan en X Y en Y.
                double invadeDerecha = derechaCentro - rMonedas.Left;
                double invadeIzquierda = rMascotas.IsEmpty ? double.NegativeInfinity : rMascotas.Right - izquierdaCentro;
                bool compartenFranja = celdasCentro.Any(c => c.R.Bottom > rMonedas.Top && c.R.Top < rMonedas.Bottom);
                int celdasQueInvaden = celdasCentro.Count(c => c.R.Right > rMonedas.Left + 0.5 && c.R.Bottom > rMonedas.Top && c.R.Top < rMonedas.Bottom);

                var sgpCentro = Descendientes<SlotGridPanel>(host).FirstOrDefault(p => celdasCentro.Any(c => ReferenceEquals(c.Fe, p.Children.Count > 0 ? p.Children[0] : null)));
                double cell = sgpCentro != null && sgpCentro.Children.Count > 0 ? ((FrameworkElement)sgpCentro.Children[0]).ActualWidth : -1;
                double anchoColCentro = host.ColumnDefinitions.Count > 1 ? host.ColumnDefinitions[1].ActualWidth : -1;
                string columnas = string.Join("/", host.ColumnDefinitions.Select(c => $"{c.ActualWidth:0.#}"));

                // Lo que el usuario ve DE VERDAD no es "un slot pintado encima de la caja de
                // Monedas": el ScrollViewer de la rejilla recorta antes de llegar ahi, asi que un
                // slot que se sale se ve CORTADO justo donde acaba su columna (que esta a solo 8px
                // -el Margin del Border- del borde de la caja de Monedas, de ahi que se lea como
                // "solapado con las monedas"). Este es el criterio real: cuanto se sale cada celda
                // del viewport que la recorta, y cuantas celdas quedan cortadas o directamente
                // fuera - con HorizontalScrollBarVisibility="Disabled" eso es contenido PERDIDO,
                // no meramente desplazado.
                double cortePeor = 0;
                int celdasCortadas = 0, celdasFuera = 0;
                foreach (var (celda, r, sv) in celdasCentro)
                {
                    if (sv == null || sv.ViewportWidth <= 0) continue;
                    double bordeVisible = RectEn(sv, host).Left + sv.ViewportWidth;
                    double sale = r.Right - bordeVisible;
                    if (sale > 0.5)
                    {
                        celdasCortadas++;
                        if (r.Left >= bordeVisible - 0.5) celdasFuera++;
                        cortePeor = Math.Max(cortePeor, sale);
                    }
                }

                // AR-14d (6-sep-2026): el MISMO criterio, pero en el eje VERTICAL. AR-14 nacio de
                // un corte horizontal y solo miraba la X, y eso deja un punto ciego real: una
                // reorganizacion de esta fila que le quite ALTO al centro no se ve por ninguna
                // parte. Medido de verdad al probar la variante "Monedas/Municion apilada DEBAJO
                // del centro" que se propuso para arreglar AR-14c: a 1080x700 la fila del centro
                // cae de 115,7 a 38,1px para una rejilla que necesita 84 (2 filas de MinCell 40 +
                // Gap), o sea que se ve UNA fila de accesorios de las dos. Aqui SI hay escape
                // (el ScrollViewer de ContainerCompactTemplate lleva el eje vertical en "Auto",
                // al reves que el horizontal), asi que D1 de AR-LAY lo daria por "alcanzable" y
                // callaria - pero media rejilla de accesorios escondida detras de una barra que
                // antes no existia es exactamente la regresion que esta comprobacion vigila.
                double corteVertPeor = 0;
                int celdasCortadasVert = 0;
                foreach (var (celda, r, sv) in celdasCentro)
                {
                    if (sv == null || sv.ViewportHeight <= 0) continue;
                    double bordeVisibleY = RectEn(sv, host).Top + sv.ViewportHeight;
                    double saleY = r.Bottom - bordeVisibleY;
                    if (saleY > 0.5) { celdasCortadasVert++; corteVertPeor = Math.Max(corteVertPeor, saleY); }
                }

                Console.WriteLine($"AR-14 {w:0}x{h:0} SizeClass={vm.SizeClass} Amplio={vm.IsEquipmentExpanded} | host={host.ActualWidth:0.#} cols={columnas} celda={cell:0.#} " +
                                  $"centro=[{izquierdaCentro:0.#}..{derechaCentro:0.#}] monedas.Left={rMonedas.Left:0.#} | invadeDerecha={invadeDerecha:0.#}px invadeIzquierda={invadeIzquierda:0.#}px " +
                                  $"corteMax={cortePeor:0.#}px celdasCortadas={celdasCortadas} celdasFuera={celdasFuera} mismaFranja={compartenFranja} " +
                                  $"| corteVert={corteVertPeor:0.#}px celdasCortadasVert={celdasCortadasVert}");

                if (invadeDerecha > 0.5 && compartenFranja)
                    Console.WriteLine($"FALLO: AR-14 - a {w:0}x{h:0} los slots de Armadura/Accesorios invaden {invadeDerecha:0.#}px el bloque de Monedas/Municion ({celdasQueInvaden} celdas reales pisadas)");
                if (invadeIzquierda > 0.5)
                    Console.WriteLine($"FALLO: AR-14 - a {w:0}x{h:0} los slots de Armadura/Accesorios invaden {invadeIzquierda:0.#}px el bloque de Mascota/Montura/Tinte");
                if (celdasCortadas > 0)
                    Console.WriteLine($"FALLO: AR-14 - a {w:0}x{h:0} {celdasCortadas} slot(s) de Armadura/Accesorios se salen hasta {cortePeor:0.#}px de su columna y quedan CORTADOS contra el bloque de Monedas/Municion ({celdasFuera} invisibles del todo, sin scroll horizontal con el que alcanzarlos)");

                if (celdasCortadasVert > 0)
                    Console.WriteLine($"FALLO: AR-14d - a {w:0}x{h:0} {celdasCortadasVert} slot(s) de Armadura/Accesorios se salen {corteVertPeor:0.#}px POR ABAJO de su propia caja: la rejilla de 2 filas ya no cabe entera y hay que hacer scroll para ver la segunda");

                // ---- AR-14c: el OTRO lado de la misma fila, Monedas/Municion ----
                // AR-LAY lo dejo escrito como "limite conocido" el 6-sep-2026 y el usuario pidio
                // explicitamente no dejarlo asi. Medido aqui: a 1080x700 el bloque pedia 125,3px
                // de ALTO y su Border solo tenia 103,7 utiles, mientras que en ANCHO le sobraba a
                // todo el mundo (columnas 140/241/200: al centro le sobran 25px de los 216 que
                // pide, y entre el ultimo accesorio y esta caja habia 8px reales de separacion).
                // O sea que el corte de esta caja NO es de ancho -que es lo unico que AR-14 sabia
                // mirar- sino de ALTO, y de ahi que los tres arreglos que se probaron sobre el
                // reparto HORIZONTAL (ScrollViewer propio, MaxWidth en la columna, apilarla debajo
                // del centro) tuvieran todos un efecto colateral peor: ninguno tocaba la dimension
                // que de verdad se quedaba corta. Ver el comentario real del Border en
                // MainWindow.xaml para el arreglo y sus numeros.
                //
                // Se mide en los DOS ejes y con el mismo criterio que AR-14 y que D1 de AR-LAY:
                // cuanto del contenido real queda fuera de la zona que de verdad se pinta, y si
                // hay o no un ScrollViewer que pueda alcanzarlo EN ESE EJE. El ScrollViewer de
                // cada ContainerCompactTemplate no sirve de escape aqui: vive DENTRO de la pila,
                // asi que cuando quien recorta es el Border/la fila, su ScrollableHeight es 0 y
                // no hay nada que desplazar (justo lo que hacia que AR-LAY lo contara como
                // contenido perdido de verdad).
                var pila = Descendientes<FrameworkElement>(cajaMonedas).FirstOrDefault(f => f.Name == "PilaMonedasMunicion");
                if (pila == null)
                    Console.WriteLine($"FALLO: AR-14c - a {w:0}x{h:0} no se encuentra la pila de Monedas/Municion (PilaMonedasMunicion) dentro de su caja");
                else
                {
                    var completoPila = RectCompleto(pila, window);
                    var zonaPila = ZonaVisible(pila, window);
                    double faltaXPila = zonaPila.IsEmpty ? completoPila.Width
                        : Math.Min(completoPila.Width, Math.Max(0, zonaPila.Left - completoPila.Left) + Math.Max(0, completoPila.Right - zonaPila.Right));
                    double faltaYPila = zonaPila.IsEmpty ? completoPila.Height
                        : Math.Min(completoPila.Height, Math.Max(0, zonaPila.Top - completoPila.Top) + Math.Max(0, completoPila.Bottom - zonaPila.Bottom));
                    bool escapaXPila = faltaXPila <= 1 || AlcanzableConScroll(pila, window, horizontal: true);
                    bool escapaYPila = faltaYPila <= 1 || AlcanzableConScroll(pila, window, horizontal: false);

                    // Y ademas slot a slot, que es lo que el usuario ve: media fila de munición
                    // cortada por abajo se lee como "los slots estan partidos", no como "la caja
                    // mide 24px menos".
                    int slotsCortadosMon = 0, slotsFueraMon = 0;
                    double corteMon = 0;
                    foreach (var sgpMon in Descendientes<SlotGridPanel>(cajaMonedas))
                        foreach (var celdaMon in sgpMon.Children.OfType<FrameworkElement>())
                        {
                            var rc = RectCompleto(celdaMon, window);
                            var zc = ZonaVisible(celdaMon, window);
                            double faltaX = zc.IsEmpty ? rc.Width : Math.Min(rc.Width, Math.Max(0, zc.Left - rc.Left) + Math.Max(0, rc.Right - zc.Right));
                            double faltaY = zc.IsEmpty ? rc.Height : Math.Min(rc.Height, Math.Max(0, zc.Top - rc.Top) + Math.Max(0, rc.Bottom - zc.Bottom));
                            if (faltaX > 1 && AlcanzableConScroll(celdaMon, window, true)) faltaX = 0;
                            if (faltaY > 1 && AlcanzableConScroll(celdaMon, window, false)) faltaY = 0;
                            double peor = Math.Max(faltaX, faltaY);
                            if (peor <= 1) continue;
                            slotsCortadosMon++;
                            if (peor >= Math.Min(rc.Width, rc.Height) - 1) slotsFueraMon++;
                            corteMon = Math.Max(corteMon, peor);
                        }

                    // El reparto VERTICAL real de la fila fusionada y quien lo limita: sin esto,
                    // un "pierde 21,6px de alto" no dice de donde habria que sacarlos.
                    string filas = string.Join("/", host.RowDefinitions.Select(r => $"{r.ActualHeight:0.#}"));
                    string svArriba = "(ninguno)";
                    for (DependencyObject? d = System.Windows.Media.VisualTreeHelper.GetParent(host); d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
                        if (d is ScrollViewer svH)
                        {
                            svArriba = $"{svH.GetType().Name} V:{svH.VerticalScrollBarVisibility} vp={svH.ViewportHeight:0.#} ext={svH.ExtentHeight:0.#} scr={svH.ScrollableHeight:0.#}";
                            break;
                        }
                    Console.WriteLine($"AR-14c-ALTO {w:0}x{h:0} host={host.ActualWidth:0.#}x{host.ActualHeight:0.#} filas={filas} scrollArriba={svArriba}");

                    Console.WriteLine($"AR-14c {w:0}x{h:0} monedas/municion: caja={rMonedas.Width:0.#}x{rMonedas.Height:0.#} en celda c{Grid.GetColumn(cajaMonedas)}/f{Grid.GetRow(cajaMonedas)} " +
                                      $"pila pide {completoPila.Width:0.#}x{completoPila.Height:0.#} y se pinta {(zonaPila.IsEmpty ? "nada" : $"{Math.Min(completoPila.Width, Math.Max(0, Math.Min(completoPila.Right, zonaPila.Right) - Math.Max(completoPila.Left, zonaPila.Left))):0.#}x{Math.Min(completoPila.Height, Math.Max(0, Math.Min(completoPila.Bottom, zonaPila.Bottom) - Math.Max(completoPila.Top, zonaPila.Top))):0.#}")} " +
                                      $"| pierde {(escapaXPila ? 0 : faltaXPila):0.#}x{(escapaYPila ? 0 : faltaYPila):0.#}px sin scroll | slotsCortados={slotsCortadosMon} (peor {corteMon:0.#}px, {slotsFueraMon} invisibles del todo)");

                    if (!escapaXPila && faltaXPila > 1)
                        Console.WriteLine($"FALLO: AR-14c - a {w:0}x{h:0} el bloque de Monedas/Municion pierde {faltaXPila:0.#}px de ANCHO sin ningun scroll con el que alcanzarlos");
                    if (!escapaYPila && faltaYPila > 1)
                        Console.WriteLine($"FALLO: AR-14c - a {w:0}x{h:0} el bloque de Monedas/Municion pierde {faltaYPila:0.#}px de ALTO sin ningun scroll con el que alcanzarlos");
                    if (slotsCortadosMon > 0)
                        Console.WriteLine($"FALLO: AR-14c - a {w:0}x{h:0} {slotsCortadosMon} slot(s) de Monedas/Municion quedan CORTADOS hasta {corteMon:0.#}px ({slotsFueraMon} invisibles del todo), sin scroll con el que alcanzarlos");
                }

                if (Environment.GetEnvironmentVariable("AR14_CAPTURAS") == "1")
                {
                    var rtbEq = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbEq.Render(window);
                    var encEq = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encEq.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbEq));
                    using var fsEq = File.Create(Path.Combine(AppContext.BaseDirectory, $"ar14-armadura-{w:0}x{h:0}.png"));
                    encEq.Save(fsEq);
                }
            }

            // AR-14b: el MinWidth=216 nuevo de la columna central es una GARANTIA, y una garantia
            // que nunca se activa no esta demostrada. Hoy no salta jamas porque las dos columnas
            // laterales se quedan clavadas en su propio MinWidth (140/200), asi que hay que
            // PROVOCARLO: se le pide a la lateral de Monedas/Municion mucho mas sitio del que le
            // toca y se comprueba que quien cede es ELLA, no el centro. Sin esto seria una
            // suposicion sobre como reparte WPF (MinWidth de una columna estrella frente a una
            // Auto exigente), justo lo que este proyecto no da por bueno sin medir. Se restaura el
            // MinWidth real al terminar.
            FijarTamaño(window, 1180, 860);
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 0;
            DoEvents(); DoEvents();
            var hostGarantia = Descendientes<SlotRowHost>(window).FirstOrDefault();
            if (hostGarantia == null || hostGarantia.ColumnDefinitions.Count != 3)
                Console.WriteLine("FALLO: AR-14b - SlotRowHost o sus 3 columnas NO-FOUND, la garantia de ancho minimo no se ha podido comprobar");
            else
            {
                var colCentral = hostGarantia.ColumnDefinitions[1];
                var colMonedas = hostGarantia.ColumnDefinitions[2];
                double minMonedasReal = colMonedas.MinWidth;
                double centroAntes = colCentral.ActualWidth;
                colMonedas.MinWidth = 400; // el doble de lo suyo: alguien "Auto" pidiendo de mas
                DoEvents(); DoEvents();
                double centroApretado = colCentral.ActualWidth, monedasApretado = colMonedas.ActualWidth;
                // Contrafactual real (leccion "verificar aislando la variable"): con el mismo
                // lateral exigente pero SIN el MinWidth nuevo, el centro tiene que caer por debajo
                // de 216 - si no cayera, es que el MinWidth no estaba arreglando nada y el "OK" de
                // arriba seria un falso positivo de otro limite cualquiera.
                double minCentralReal = colCentral.MinWidth;
                colMonedas.MinWidth = 400;
                colCentral.MinWidth = 0;
                DoEvents(); DoEvents();
                double centroSinGarantia = colCentral.ActualWidth;
                colCentral.MinWidth = minCentralReal;
                colMonedas.MinWidth = minMonedasReal;
                DoEvents(); DoEvents();
                double centroVuelta = colCentral.ActualWidth;
                Console.WriteLine($"AR-14b GARANTIA: centro {centroAntes:0.#} -> con Monedas pidiendo 400px: centro={centroApretado:0.#} monedas={monedasApretado:0.#} -> restaurado: centro={centroVuelta:0.#} (esperado: centro nunca por debajo de 216, y vuelta al valor de partida)");
                Console.WriteLine($"AR-14b CONTRAFACTUAL: el MISMO lateral exigente sin el MinWidth de la columna central deja el centro en {centroSinGarantia:0.#}px, o sea {216 - centroSinGarantia:0.#}px menos de los que la rejilla necesita - eso es el solape real que se reporto");
                if (centroSinGarantia >= 215.5)
                    Console.WriteLine($"FALLO: AR-14b - el contrafactual no reproduce nada (centro={centroSinGarantia:0.#} sin MinWidth): el 'OK' de la garantia lo estaria dando otro limite, no este arreglo");
                if (centroApretado < 215.5)
                    Console.WriteLine($"FALLO: AR-14b - el MinWidth de la columna central NO es una garantia real: con la lateral de Monedas pidiendo 400px el centro cayo a {centroApretado:0.#}px, por debajo de los 216 que necesita la rejilla a MinCell");
                if (Math.Abs(centroVuelta - centroAntes) > 1)
                    Console.WriteLine($"FALLO: AR-14b - el propio bloque no restauro el reparto ({centroAntes:0.#} -> {centroVuelta:0.#}), contamina lo que venga despues");
            }

            // Deja el estado como estaba (misma leccion que LOADOUT-PILDORAS/AR-13c): vista de
            // Equipamiento original y tamaño base del arnes.
            var kindVuelta = vm.EquipmentGroup?.KindOptions.FirstOrDefault(o => o.Value == kindOriginal);
            if (kindVuelta != null) vm.EquipmentGroup!.SelectKindCommand.Execute(kindVuelta);
            vm.IsLibraryCollapsed = libreriaOriginal;
            FijarTamaño(window, 1180, 860);
            DoEvents();
        }
        catch (Exception ex)
        {
            Console.WriteLine("AR-14-EXCEPTION: " + ex);
        }
    }
}
