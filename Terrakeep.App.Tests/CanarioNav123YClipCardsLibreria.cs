// CANARIO REAL (24-sep-2026, revision-correccion-integral-familia-Keep, bloque "Personaje >
// Objetos"): dos huecos de cobertura reales cerrados por el mismo encargo, documentados en la
// bitacora bajo "imagen2" - investigado por el agente investigador-bug del patron de 2 fases (NO
// toca produccion, solo este arnes). Ambos modos son CANARIOS ROJOS A PROPOSITO hoy (reproducen
// el bug real con evidencia medida/renderizada, no "deberia fallar") - se espera que pasen a OK
// cuando `aplicador-fix` aplique el arreglo real, sin tocar este fichero.
//
// NAV123_SOLO (bug A): "la navegacion Equipamiento/Inventario/Almacenes es un scroll continuo
// (MainWindow.xaml linea ~2876, ScrollViewer x:Name=ObjetosBoardScroll) sin ningun selector
// directo - el usuario puede quedar a medio camino, con una seccion colapsada a una franja de
// pocos pixeles mientras la siguiente ya ocupa el centro de la pantalla" (captura real del
// coordinador, imagen2.png). Reproduce ese estado exacto con offsets reales del propio
// ScrollViewer y confirma la AUSENCIA del selector 1/2/3 pedido.
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
            // 1180x700: mismo tamaño real que T3_SOLO ya usa para este mismo tablero (caso mas
            // exigente documentado en MainWindow.xaml, "el minimo real de la ventana").
            FijarTamaño(window, 1180, 700);
            DoEvents(); DoEvents(); DoEvents();

            var scroll = window.FindName("ObjetosBoardScroll") as ScrollViewer;
            var secEquip = window.FindName("ObjetosSeccionEquipamiento") as FrameworkElement;
            var secInv = window.FindName("ObjetosSeccionInventario") as FrameworkElement;
            var secAlm = window.FindName("ObjetosSeccionAlmacenes") as FrameworkElement;
            if (scroll == null || secEquip == null || secInv == null || secAlm == null)
            {
                Console.WriteLine("NAV123: FALLO - no se encuentran el ScrollViewer/las 3 secciones reales del tablero de Objetos (ver T3_SOLO, mismo arbol)");
                return;
            }

            // Posiciones reales de cada seccion dentro del scroll, con VerticalOffset=0 (mismo
            // calculo/convencion exacta que T3_SOLO y que ScrollToObjetosSection en
            // MainWindow.xaml.cs linea ~708 - los 3 numeros son validos para pasarselos tal
            // cual a ScrollToVerticalOffset).
            double yEquip = secEquip.TranslatePoint(new Point(0, 0), scroll).Y;
            double yInv = secInv.TranslatePoint(new Point(0, 0), scroll).Y;
            double yAlm = secAlm.TranslatePoint(new Point(0, 0), scroll).Y;
            double alturaEquip = secEquip.ActualHeight;
            Console.WriteLine($"NAV123: posiciones reales del tablero -> Equipamiento y={yEquip:0.#} (alto={alturaEquip:0.#}), Inventario y={yInv:0.#}, Almacenes y={yAlm:0.#}, ScrollableHeight={scroll.ScrollableHeight:0.#}, ViewportHeight={scroll.ViewportHeight:0.#}");

            // --- REPRODUCCION del estado exacto de la captura del coordinador: un offset
            // intermedio elegido para dejar solo un resto de pocos pixeles de Equipamiento
            // visible mientras Inventario ya domina el viewport. ---
            // yEquip+alturaEquip = fin real del contenido de Equipamiento (no yInv - hay un hueco
            // real de margen, Margin="0,14,0,0" en ObjetosSeccionInventario, MainWindow.xaml linea
            // 3278, entre el fin de una seccion y el comienzo visual de la siguiente).
            double offsetRoto = Math.Max(0, yEquip + alturaEquip - 8);
            scroll.ScrollToVerticalOffset(offsetRoto);
            scroll.UpdateLayout();
            DoEvents(); DoEvents();
            double pxEquipVisibles = Math.Max(0, (yEquip + alturaEquip) - scroll.VerticalOffset);
            double pxInventarioEnViewport = Math.Max(0, scroll.ViewportHeight - Math.Max(0, yInv - scroll.VerticalOffset));
            Console.WriteLine($"NAV123: estado intermedio reproducido -> VerticalOffset real={scroll.VerticalOffset:0.#} (pedido {offsetRoto:0.#}), Equipamiento visible={pxEquipVisibles:0.#}px de {alturaEquip:0.#}px totales, Inventario ya ocupa {pxInventarioEnViewport:0.#}px de los {scroll.ViewportHeight:0.#}px del viewport");
            bool reproducidoElCasoDeLaCaptura = pxEquipVisibles > 0 && pxEquipVisibles < 40 && pxInventarioEnViewport > scroll.ViewportHeight * 0.4;
            if (!reproducidoElCasoDeLaCaptura)
                Console.WriteLine("NAV123: AVISO - con este ancho/alto concretos el offset elegido no deja a Equipamiento en una franja tan estrecha como la captura; el mecanismo (ver abajo) sigue siendo el mismo, ajustar el offset si el layout real cambio de altura.");

            string shotRoto = Path.Combine(AppContext.BaseDirectory, "nav123-estado-intermedio-roto.png");
            File.WriteAllBytes(shotRoto, CapturarPng(window, window.ActualWidth, window.ActualHeight));
            Console.WriteLine($"NAV123: captura real del estado intermedio -> {shotRoto}");

            // --- La UNICA pieza de navegacion no textual hoy es la barra pegajosa, y es
            // deliberadamente NO interactiva (MainWindow.xaml linea 3437, IsHitTestVisible="False"
            // - solo es un rotulo que dice en que seccion esta el usuario, no lo lleva a ningun
            // sitio). ---
            var stickyBar = window.FindName("ObjetosStickyBar") as Border;
            Console.WriteLine($"NAV123: barra pegajosa -> encontrada={stickyBar != null}, IsHitTestVisible={stickyBar?.IsHitTestVisible} (False = solo rotulo, no es una forma real de navegar)");

            // --- EL HUECO REAL: no existe ningun selector 1/2/3 (RadioButton/ToggleButton con 3
            // opciones reales) que lleve EXACTAMENTE a yEquip/yInv/yAlm sin poder quedar a medio
            // camino. Se busca cualquier candidato ya en el arbol visual. ---
            var candidatos = Descendientes<ToggleButton>(window)
                .Where(t => t.IsVisible && t.Content != null && (t.Content.ToString() == "1" || t.Content.ToString() == "2" || t.Content.ToString() == "3"))
                .ToList();
            Console.WriteLine($"NAV123: controles reales de navegacion directa (ToggleButton/RadioButton con contenido '1'/'2'/'3') encontrados junto al tablero = {candidatos.Count} (esperado 3)");
            if (candidatos.Count < 3)
                Console.WriteLine("FALLO: NAV123_SOLO - no existe el selector Toggle 1/2/3 pedido por el usuario para Equipamiento/Inventario/Almacenes. Hoy la UNICA forma real de moverse entre secciones es el scroll libre de 'ObjetosBoardScroll' (MainWindow.xaml linea ~2876-2878), que permite (reproducido arriba con evidencia real) quedar detenido en cualquier offset intermedio visualmente roto. Canario ROJO a proposito: debe pasar a OK cuando el selector real exista y sus 3 destinos aterricen en offsets exactos (yEquip/yInv/yAlm medidos arriba), sin estados intermedios alcanzables por ese camino.");

            // Deja el tablero como lo encontro.
            scroll.ScrollToVerticalOffset(0);
            scroll.UpdateLayout();
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
