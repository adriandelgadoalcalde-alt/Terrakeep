// CANARIO REAL (24-sep-2026, revision-correccion-integral-familia-Keep, bloque "Personaje >
// Objetos"): dos huecos de cobertura reales cerrados por el mismo encargo, documentados en la
// bitacora bajo "imagen2" - investigado por el agente investigador-bug del patron de 2 fases (NO
// toca produccion, solo este arnes). LIBCARD_CLIP_SOLO sigue siendo CANARIO ROJO A PROPOSITO.
//
// NAV123_SOLO (bug A) REESCRITO de arriba a abajo (25-sep-2026, aplicador-fix, TASK CONTEXT
// e5eaea9e-c261-4199-8e7d-060b6054f58d): el diseño original de este canario reproducia el bug del
// scroll continuo (ObjetosBoardScroll, ya retirado) midiendo offsets. Con el arreglo real
// aplicado (NAV123: ObjetosPageHost, 3 ScrollViewer superpuestos, Visibility de cada uno atada en
// EXCLUSIVA a MainViewModel.ObjetosSubTabIndex via EnumEqualsToVis) ya no existe ningun offset que
// reproducir - este canario verifica ahora el COMPORTAMIENTO REAL del arreglo, con codigo, no
// visualmente: (A) exclusividad de Visibility por pagina (exactamente 1 de 3 Visible para
// ObjetosSubTabIndex=0/1/2), (B) ausencia del sticky bar duplicado (ObjetosStickyBar retirado),
// (C) funciona con pocos objetos (Inventario casi vacio) y muchos (Almacenes lleno 40/40, el caso
// real que antes clampaba el offset), (D) ciclo de clics 1->2->3->1 sin drift geometrico, (E)
// RequestObjetosSection(0/1/2) llamado directamente en el ViewModel (sin pasar por un clic de UI)
// selecciona la pagina correcta.
//
// LIBCARD_CLIP_SOLO (bug B): "las cards de categoria de la Libreria (y de la Libreria de Buffs)
// tienen el hover/borde superior cortado porque estan demasiado pegadas al limite superior de su
// contenedor" (mismo reporte, mismo screenshot - 'Materiales' con el borde celeste pegado al
// borde de su fila). Causa real: NavCardButton (Terrakeep.App\Styles\Theme.xaml linea 1231) sube
// el Border "Bd" 3px en hover via TranslateTransform "BdLift" (Trigger IsMouseOver, linea 1255) -
// las 3 rejillas que reutilizan este estilo para sus "tarjetas de carpeta raiz" (Libreria de
// objetos MainWindow.xaml linea ~3690, Libreria de Buffs linea ~3906, Investigacion linea ~4067)
// meten el ItemsControl/WrapPanel dentro de un ScrollViewer SIN ningun margen/padding superior de
// reserva - la fila 1, con VerticalOffset=0, ya esta pegada al borde exacto del viewport que
// recorta, asi que los -3px del lift caen fuera del area que el ScrollViewer pinta y se cortan.
// Medido aqui con RectCompleto (el rectangulo que el Border ocuparia sin recorte, AuditoriaMaquetacion.cs)
// contra RectVisible (lo que de verdad se pinta tras el recorte de todos los ancestros, ya usado
// por el barrido AR-15 real de mas abajo en este mismo arnes) - la MISMA pareja de herramientas
// que ya ha encontrado otros recortes reales en este proyecto, aplicada aqui por primera vez a
// una tarjeta en estado hover.
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Terrakeep.App;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    // ====================================================================================
    // Bug A: selector 1/2/3 ausente + estado intermedio roto reproducible en el scroll libre
    // ====================================================================================
    private static void EjecutarNav123Solo(MainWindow window, MainViewModel vm)
    {
        try
        {
            var personajeReal = vm.Home.Characters.FirstOrDefault();
            if (personajeReal != null) { vm.Home.OpenCommand.Execute(personajeReal); DoEvents(); DoEvents(); }
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; // Personaje > Objetos
            // 1080x700: el minimo real documentado de la ventana (H3-18, MainWindow.xaml) - el
            // caso mas exigente para este tablero, pedido explicitamente para la verificacion
            // visual de las 3 paginas (TASK CONTEXT e5eaea9e-c261-4199-8e7d-060b6054f58d).
            FijarTamaño(window, 1080, 700);
            DoEvents(); DoEvents(); DoEvents();

            // Las 3 paginas reales son los ScrollViewer con la Visibility ligada (ObjetosPaginaX) -
            // NO el Border interior (ObjetosSeccionX, sin Visibility propia): comprobar la
            // Visibility del Border daria siempre "Visible" (su valor local nunca cambia, solo el
            // de su ScrollViewer ancestro se colapsa), un falso negativo de medicion, no un bug de
            // produccion - confirmado comparando ambos durante el desarrollo de este canario.
            var pagEquip = window.FindName("ObjetosPaginaEquipamiento") as FrameworkElement;
            var pagInv = window.FindName("ObjetosPaginaInventario") as FrameworkElement;
            var pagAlm = window.FindName("ObjetosPaginaAlmacenes") as FrameworkElement;
            var t1 = window.FindName("ObjetosNavToggle1") as RadioButton;
            var t2 = window.FindName("ObjetosNavToggle2") as RadioButton;
            var t3 = window.FindName("ObjetosNavToggle3") as RadioButton;
            if (pagEquip == null || pagInv == null || pagAlm == null || t1 == null || t2 == null || t3 == null)
            {
                Console.WriteLine("FALLO: NAV123_SOLO - no se encuentran las 3 paginas/RadioButton reales del tablero de Objetos (ObjetosPagina*/ObjetosNavToggle1-3)");
                return;
            }

            // --- B: la barra pegajosa (T3 PASO 2, duplicaba el titulo de la seccion activa - el
            // bug real que el usuario reporto) tiene que haber desaparecido del arbol visual, no
            // solo quedar oculta. ---
            var stickyBar = window.FindName("ObjetosStickyBar");
            Console.WriteLine($"NAV123: barra pegajosa retirada del arbol -> encontrada={stickyBar != null} (esperado null, no solo Collapsed)");
            if (stickyBar != null)
                Console.WriteLine("FALLO: NAV123_SOLO-STICKY - ObjetosStickyBar sigue existiendo en el arbol visual; con paginas exclusivas duplicaria el titulo que ya trae cada seccion.");

            // --- C: casos reales de pocos/muchos objetos con el mismo personaje. Inventario casi
            // vacio (deja solo 1 slot ocupado). ---
            int inventarioAntes = vm.InventoryContainer?.Slots.Count(s => !s.IsEmpty) ?? -1;
            if (vm.InventoryContainer != null)
            {
                vm.InventoryContainer.ClearAllCommand.Execute(null);
                vm.InventoryContainer.Slots.FirstOrDefault()?.PlaceItem(1);
            }
            // Almacenes lleno (el caso real que antes clampaba ScrollViewer - "toggle 3
            // desincronizado" con Almacenes casi vacio esperaba menos scroll del que Equipamiento/
            // Inventario ya habian consumido; con Almacenes LLENO, si algo del mecanismo antiguo
            // hubiera sobrevivido a medias, seria aqui donde se notaria).
            if (vm.StorageGroup != null)
                foreach (var slot in vm.StorageGroup.Current.Slots) slot.PlaceItem(1);
            DoEvents(); DoEvents();
            int inventarioDespues = vm.InventoryContainer?.Slots.Count(s => !s.IsEmpty) ?? -1;
            int almacenesOcupados = vm.StorageGroup?.Current.Slots.Count(s => !s.IsEmpty) ?? -1;
            int almacenesTotal = vm.StorageGroup?.Current.Slots.Count ?? -1;
            Console.WriteLine($"NAV123: casos reales preparados -> Inventario ocupado antes={inventarioAntes}, despues={inventarioDespues} (esperado 1, casi vacio); Almacenes ocupado={almacenesOcupados}/{almacenesTotal} (esperado lleno, ocupado==total)");
            if (inventarioDespues != 1)
                Console.WriteLine("NAV123: AVISO - no se pudo dejar el Inventario casi vacio con este personaje/fixture real (¿ClearAllCommand o PlaceItem cambiaron de firma?)");
            if (almacenesTotal <= 0 || almacenesOcupados != almacenesTotal)
                Console.WriteLine("NAV123: AVISO - no se pudo dejar Almacenes lleno con este personaje/fixture real");

            // --- A: exclusividad de Visibility por pagina, para los 3 valores reales de
            // ObjetosSubTabIndex - exactamente 1 de 3 Visible, las otras 2 Collapsed. ---
            (bool ok, Visibility[] estados) VerificaExclusividad(int indice, string nombreEsperado)
            {
                vm.RequestObjetosSection(indice);
                DoEvents(); DoEvents();
                Visibility[] vis = [pagEquip.Visibility, pagInv.Visibility, pagAlm.Visibility];
                string[] nombres = ["Equipamiento", "Inventario", "Almacenes"];
                int idxEsperado = Array.IndexOf(nombres, nombreEsperado);
                int visibles = vis.Count(v => v == Visibility.Visible);
                bool ok = visibles == 1 && vis[idxEsperado] == Visibility.Visible;
                Console.WriteLine($"NAV123: ObjetosSubTabIndex={indice} -> Visibility real = Equipamiento:{vis[0]}, Inventario:{vis[1]}, Almacenes:{vis[2]} (esperado SOLO '{nombreEsperado}' Visible)");
                if (!ok)
                    Console.WriteLine($"FALLO: NAV123_SOLO-EXCLUSIVIDAD - con ObjetosSubTabIndex={indice} se esperaba exactamente 1 pagina Visible ('{nombreEsperado}') y las otras 2 Collapsed; visibles reales={visibles}");
                return (ok, vis);
            }

            VerificaExclusividad(0, "Equipamiento");
            string shotEquip = Path.Combine(AppContext.BaseDirectory, "nav123-pagina-equipamiento.png");
            File.WriteAllBytes(shotEquip, CapturarPng(window, window.ActualWidth, window.ActualHeight));

            VerificaExclusividad(1, "Inventario");
            string shotInv = Path.Combine(AppContext.BaseDirectory, "nav123-pagina-inventario.png");
            File.WriteAllBytes(shotInv, CapturarPng(window, window.ActualWidth, window.ActualHeight));

            VerificaExclusividad(2, "Almacenes");
            string shotAlm = Path.Combine(AppContext.BaseDirectory, "nav123-pagina-almacenes.png");
            File.WriteAllBytes(shotAlm, CapturarPng(window, window.ActualWidth, window.ActualHeight));
            Console.WriteLine($"NAV123: capturas reales de las 3 paginas exclusivas -> {shotEquip}, {shotInv}, {shotAlm}");

            // --- D: ciclo de clic real 1->2->3->1 - IsChecked correcto + pagina Visible correcta +
            // sin drift geometrico (tamaño de ventana estable, ninguna medicion de offset de por
            // medio que pueda desplazar nada). ---
            double anchoAntes = window.ActualWidth, altoAntes = window.ActualHeight;
            void ClicYVerifica(RadioButton objetivo, string nombreSeccion, string nombreEsperado)
            {
                objetivo.IsChecked = true; // como el clic real: primero cambia el estado del control...
                objetivo.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); // ...y luego avisa (OnObjetosNavToggleClick)
                DoEvents(); DoEvents();
                Visibility[] vis = [pagEquip.Visibility, pagInv.Visibility, pagAlm.Visibility];
                string[] nombres = ["Equipamiento", "Inventario", "Almacenes"];
                int idxEsperado = Array.IndexOf(nombres, nombreEsperado);
                bool marcaCorrecta = objetivo.IsChecked == true
                    && (objetivo == t1 || t1.IsChecked != true)
                    && (objetivo == t2 || t2.IsChecked != true)
                    && (objetivo == t3 || t3.IsChecked != true);
                bool paginaCorrecta = vis.Count(v => v == Visibility.Visible) == 1 && vis[idxEsperado] == Visibility.Visible;
                bool sinDrift = window.ActualWidth == anchoAntes && window.ActualHeight == altoAntes;
                Console.WriteLine($"NAV123-CICLO: clic en '{nombreSeccion}' -> IsChecked(1/2/3)=({t1.IsChecked},{t2.IsChecked},{t3.IsChecked}), Visibility(Equip/Inv/Alm)=({vis[0]},{vis[1]},{vis[2]}), tamaño ventana sin cambios={sinDrift}");
                if (!marcaCorrecta)
                    Console.WriteLine($"FALLO: NAV123_SOLO-CICLO - tras un clic real en '{nombreSeccion}' el indicador 1/2/3 no queda marcado en esa seccion (IsChecked real: 1={t1.IsChecked}, 2={t2.IsChecked}, 3={t3.IsChecked})");
                if (!paginaCorrecta)
                    Console.WriteLine($"FALLO: NAV123_SOLO-CICLO - tras el clic en '{nombreSeccion}' la pagina Visible real no coincide con la esperada");
                if (!sinDrift)
                    Console.WriteLine($"FALLO: NAV123_SOLO-CICLO - drift geometrico real: el tamaño de ventana cambio tras el clic en '{nombreSeccion}' (antes {anchoAntes}x{altoAntes}, ahora {window.ActualWidth}x{window.ActualHeight})");
            }

            ClicYVerifica(t1, "1 (Equipamiento)", "Equipamiento");
            ClicYVerifica(t2, "2 (Inventario)", "Inventario");
            ClicYVerifica(t3, "3 (Almacenes)", "Almacenes");
            ClicYVerifica(t1, "1 (Equipamiento, vuelta)", "Equipamiento");

            // --- E: RequestObjetosSection(0/1/2) llamado DIRECTAMENTE en el ViewModel (sin pasar
            // por ningun clic de UI) tiene que seleccionar la pagina correcta - confirma que la
            // Visibility de cada pagina reacciona sola al binding (ObjetosSubTabIndex via
            // EnumEqualsToVis), no a un efecto secundario del manejador de clic. IsChecked del
            // RadioButton 3 tambien se espera correcto: el binding TwoWay (EnumEquals) lo sincroniza
            // solo, sin que el Click intervenga. ---
            vm.RequestObjetosSection(2);
            DoEvents(); DoEvents();
            bool almacenesVisibleSinClic = pagAlm.Visibility == Visibility.Visible && pagEquip.Visibility == Visibility.Collapsed && pagInv.Visibility == Visibility.Collapsed;
            Console.WriteLine($"NAV123: RequestObjetosSection(2) llamado directo en el ViewModel (sin clic de UI) -> Visibility(Equip/Inv/Alm)=({pagEquip.Visibility},{pagInv.Visibility},{pagAlm.Visibility}), toggle3.IsChecked={t3.IsChecked} (esperado solo Almacenes Visible y toggle3 marcado)");
            if (!almacenesVisibleSinClic)
                Console.WriteLine("FALLO: NAV123_SOLO-DIRECTO - RequestObjetosSection(2) llamado directamente en el ViewModel no selecciono en exclusiva la pagina de Almacenes");
            if (t3.IsChecked != true)
                Console.WriteLine("FALLO: NAV123_SOLO-DIRECTO - RequestObjetosSection(2) selecciono la pagina correcta pero el RadioButton 3 no quedo marcado (binding TwoWay EnumEquals roto)");

            // Deja el tablero como lo encontro.
            vm.RequestObjetosSection(0);
            DoEvents();
        }
        catch (Exception ex) { Console.WriteLine("NAV123-EXCEPTION: " + ex); }
    }

    // ====================================================================================
    // Bug B: hover/borde superior de las cards de "carpeta raiz" cortado en la fila 1
    // ====================================================================================
    private static void EjecutarLibCardClipSolo(MainWindow window, MainViewModel vm)
    {
        try
        {
            var personajeReal = vm.Home.Characters.FirstOrDefault();
            if (personajeReal != null) { vm.Home.OpenCommand.Execute(personajeReal); DoEvents(); DoEvents(); }
            vm.SelectedTabIndex = 1; // Personaje

            // Mide UNA superficie de tarjetas de carpeta raiz ya visible en pantalla: agrupa los
            // NavCardButton reales por fila (misma Y redondeada tras TranslatePoint a `window`),
            // simula el estado final real de hover (mismo valor que la animacion XAML, Theme.xaml
            // linea 1255: DoubleAnimation To="-3" sobre BdLift) tocando SOLO los nombres de parte
            // de plantilla ya expuestos por el propio Style (Bd/BdLift, Theme.xaml linea
            // 1238-1244) y compara RectCompleto (AuditoriaMaquetacion.cs) contra RectVisible
            // (Program.cs, AR-15) - la diferencia en el eje Y de arriba es, literalmente, lo que
            // el usuario deja de ver.
            void MedirSuperficie(string idSuperficie)
            {
                DoEvents(); DoEvents();
                WaitForDispatcher(200);

                var tarjetas = Descendientes<Button>(window)
                    .Where(b => b.Style == (Style)window.FindResource("NavCardButton") && b.IsVisible && b.ActualWidth > 0
                        && b.CommandParameter is CategoryNodeViewModel)
                    .ToList();
                if (tarjetas.Count == 0)
                {
                    Console.WriteLine($"LIBCARD-CLIP [{idSuperficie}]: AVISO - no hay tarjetas de carpeta raiz visibles (¿ShowRootCategoryCards=false o superficie vacia?) - omitido");
                    return;
                }

                var filas = tarjetas
                    .Select(b => (boton: b, y: Math.Round(b.TranslatePoint(new Point(0, 0), window).Y)))
                    .GroupBy(t => t.y)
                    .OrderBy(g => g.Key)
                    .ToList();
                Console.WriteLine($"LIBCARD-CLIP [{idSuperficie}]: {tarjetas.Count} tarjeta(s) real(es) en {filas.Count} fila(s) (Y real en pantalla: {string.Join(", ", filas.Select(f => f.Key))})");

                (bool ok, double clipSuperiorPx, Rect completo, Rect visible) Medir(Button boton)
                {
                    boton.ApplyTemplate();
                    var bd = boton.Template.FindName("Bd", boton) as Border;
                    var lift = boton.Template.FindName("BdLift", boton) as TranslateTransform;
                    if (bd == null || lift == null) return (false, 0, Rect.Empty, Rect.Empty);

                    double yOriginal = lift.Y;
                    lift.Y = -3; // mismo valor final real que Theme.xaml linea 1255 (hover completo)
                    boton.UpdateLayout();
                    DoEvents(); DoEvents();

                    Rect completo = RectCompleto(bd, window);
                    Rect visible = RectVisible(bd, window);
                    double clipSuperior = visible.IsEmpty ? completo.Height : Math.Max(0, visible.Top - completo.Top);

                    lift.Y = yOriginal; // deja el boton como estaba
                    boton.UpdateLayout();
                    DoEvents();
                    return (true, clipSuperior, completo, visible);
                }

                var primeraFila = filas.First();
                var (b1, y1) = primeraFila.First();
                var m1 = Medir(b1);
                string nombre1 = (b1.DataContext as CategoryNodeViewModel)?.Name ?? "?";
                Console.WriteLine($"LIBCARD-CLIP [{idSuperficie}]: fila 1 ('{nombre1}') en hover simulado -> RectCompleto.Top={m1.completo.Top:0.##}, RectVisible.Top={m1.visible.Top:0.##}, recorte superior real={m1.clipSuperiorPx:0.##}px (esperado 0)");
                if (m1.ok && m1.clipSuperiorPx > 0.5)
                    Console.WriteLine($"FALLO: LIBCARD_CLIP_SOLO [{idSuperficie}] - la tarjeta '{nombre1}' (fila 1) pierde {m1.clipSuperiorPx:0.##}px de su borde/hover superior contra el ScrollViewer que la contiene (sin margen/padding superior de reserva). Causa real: NavCardButton sube el Border 'Bd' -3px en hover (Theme.xaml:1231-1269, Trigger IsMouseOver) dentro de un ScrollViewer con la fila 1 pegada a Y=0 (MainWindow.xaml linea ~3690/3906/4067, sin Padding/Margin superior). Canario ROJO a proposito.");
                else if (!m1.ok)
                    Console.WriteLine($"LIBCARD-CLIP [{idSuperficie}]: AVISO - no se pudieron localizar las partes de plantilla 'Bd'/'BdLift' en la tarjeta de fila 1 (¿cambio el Style NavCardButton?)");

                // Control: la MISMA simulacion sobre una tarjeta de una fila POSTERIOR (si existe)
                // - confirma que el recorte es posicional (solo la fila pegada al borde superior
                // del ScrollViewer), no un problema generico de todas las cards.
                if (filas.Count > 1)
                {
                    var (b2, y2) = filas[1].First();
                    var m2 = Medir(b2);
                    string nombre2 = (b2.DataContext as CategoryNodeViewModel)?.Name ?? "?";
                    Console.WriteLine($"LIBCARD-CLIP [{idSuperficie}]: fila 2 ('{nombre2}', control) en hover simulado -> recorte superior real={m2.clipSuperiorPx:0.##}px (esperado 0, y asi confirma que el problema es SOLO posicional de la fila 1)");
                    if (m2.ok && m2.clipSuperiorPx > 0.5)
                        Console.WriteLine($"LIBCARD-CLIP [{idSuperficie}]: AVISO - la fila 2 tambien pierde {m2.clipSuperiorPx:0.##}px; revisar si el margen entre filas (Margin=\"0,0,12,12\") tambien es insuficiente en esta superficie concreta.");
                }
                else
                {
                    Console.WriteLine($"LIBCARD-CLIP [{idSuperficie}]: AVISO - solo hay 1 fila visible a este ancho de ventana, no se pudo comparar contra una fila 2 de control.");
                }

                string shot = Path.Combine(AppContext.BaseDirectory, $"libcard-clip-{idSuperficie}.png");
                File.WriteAllBytes(shot, CapturarPng(window, window.ActualWidth, window.ActualHeight));
                Console.WriteLine($"LIBCARD-CLIP [{idSuperficie}]: captura real (estado normal, sin hover) -> {shot}");
            }

            // 1080x900: mismo ancho minimo real de la ventana (bastantes filas para tener control
            // real de fila 2) que ya documenta MainWindow.xaml para este tablero.
            FijarTamaño(window, 1080, 900);

            // --- Superficie 1: Libreria de objetos (Personaje > Objetos) ---
            vm.PersonajeInnerTabIndex = 0;
            vm.IsLibraryCollapsed = false;
            vm.Library.ClearCategoryCommand.Execute(null);
            vm.Library.SearchText = string.Empty;
            MedirSuperficie("libreria-objetos");

            // --- Superficie 2: Libreria de Buffs (Personaje > Buffs) - mismo reporte explicito
            // del usuario ("comprobar tambien la Libreria de Buffs") ---
            vm.PersonajeInnerTabIndex = 1;
            vm.IsBuffLibraryCollapsed = false;
            vm.BuffLibrary.ClearCategoryCommand.Execute(null);
            vm.BuffLibrary.SearchText = string.Empty;
            MedirSuperficie("libreria-buffs");

            // --- Superficie 3: Investigacion (Personaje > Investigacion) - mismo NavCardButton,
            // mismo patron de ScrollViewer sin margen superior (MainWindow.xaml linea ~4067):
            // hallazgo adicional pedido explicitamente por el encargo ("comprobar tambien otros
            // catalogos"). ---
            vm.PersonajeInnerTabIndex = 2;
            vm.Research.ClearCategoryCommand.Execute(null);
            vm.Research.SearchText = string.Empty;
            MedirSuperficie("investigacion");
        }
        catch (Exception ex) { Console.WriteLine("LIBCARD-CLIP-EXCEPTION: " + ex); }
    }
}
