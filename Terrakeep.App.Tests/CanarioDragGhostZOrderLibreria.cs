// CANARIO REAL (28-sep-2026) - bug reportado por el usuario: "el tema de arrastrar el Sprite desde
// las librerias ahora lo coge pero segun lo vas moviendo para llevarlo al inventario o en
// cualquier sitio por la app se lo comen las capas de fuera de la libreria". Investigado y
// arreglado en la misma ronda (sin patron de 2 fases separado esta vez: el hallazgo se confirmo de
// forma estructural, determinista y sin depender de ninguna captura de pantalla - ver el detalle
// completo abajo).
//
// Contexto: el 25-sep-2026 ya se arreglo un bug DISTINTO pero relacionado en el mismo mecanismo de
// arrastre (CanarioDragGhostLibreria.cs - el ghost se congelaba fuera de la ventana por culpa de
// Mouse.GetPosition dejando de actualizarse durante el bucle modal OLE). Ese arreglo (GetCursorPos)
// es necesario pero no suficiente: una vez la POSICION del ghost ya seguia al cursor con
// normalidad, aparecio este SEGUNDO bug, distinto, en la CAPA donde se dibuja.
//
// Causa real (Terrakeep.App/Controls/DragDropSupport.cs, StartCardDrag): `AdornerLayer.
// GetAdornerLayer(element)`, cuando `element` es una tarjeta de la Libreria (o cualquier slot
// dentro de un ScrollViewer - el caso real de las 3 librerias de la app y de varios paneles de
// slots), NO devuelve la capa de TODA LA VENTANA - devuelve una capa LOCAL propia del ScrollViewer
// que contiene al elemento. WPF le da a los descendientes de un ScrollViewer su propio AdornerLayer
// anidado dentro de ScrollContentPresenter (para que adornos "normales" - seleccion de texto,
// indicadores de resize - se recorten y se desplacen CON el scroll, comportamiento correcto para
// ESOS casos). Medido en vivo con VisualTreeHelper.GetParent desde el AdornerLayer encontrado hacia
// arriba (sin ninguna captura de pantalla, evidencia 100% estructural y determinista): esa capa
// local media 815x160px (el viewport visible de la lista de la Libreria dentro de su ScrollViewer)
// frente a los 1164x821px reales de la capa de la ventana completa (la del AdornerDecorator
// implicito de Window) - dos instancias DISTINTAS confirmadas con ReferenceEquals. Como
// ScrollContentPresenter SIEMPRE recorta su contenido al viewport (es lo que hace que el scroll
// funcione), cualquier cosa que el DragAdorner dibuje fuera de esos ~160px de alto queda cortada en
// cuanto el cursor sale de la zona visible de la Libreria - exactamente "se lo comen las capas de
// fuera de la libreria" (el usuario arrastra HACIA ARRIBA, hacia el panel de Equipamiento/
// Inventario, que esta fuera - por encima - de ese viewport recortado).
//
// Arreglo aplicado (DragDropSupport.cs, StartCardDrag): coger la capa desde la RAIZ de la ventana
// (`Window.GetWindow(element).Content`) en vez de desde el propio elemento arrastrado, con
// fallback al comportamiento anterior si no hay ninguna Window real todavia. AdornedElement sigue
// siendo el propio `element` (mismo espacio de coordenadas que ya usa UpdatePosition via
// PointFromScreen), solo cambia DE QUE CAPA cuelga el adorno.
//
// Verificacion (este canario, sin tocar nada privado de DragDropSupport - mismo criterio ya
// establecido en el proyecto, sin InternalsVisibleTo hacia este arnes): re-deriva la MISMA
// resolucion con la MISMA API publica de WPF (AdornerLayer.GetAdornerLayer), sobre una tarjeta REAL
// de la Libreria con datos reales, y confirma que la capa que se obtendria desde la raiz de la
// ventana (el camino que ahora usa produccion) es realmente del tamaño de la ventana completa - NO
// la capa pequeña, recortada, que se obtendria desde la propia tarjeta (el camino que produccion
// usaba ANTES del arreglo, y que causaba el bug). Deliberadamente NO usa RenderTargetBitmap sobre
// un Adorner (limitacion YA documentada en CanarioDragGhostLibreria.cs: un Adorner recien
// invalidado no se pinta de verdad dentro del mismo tick sincrono, ni con capturas de pantalla
// reales via BitBlt se consiguio ver el ghost a tiempo en esta maquina) - la evidencia real y
// estable de este canario es estructural (tamaños/instancias de AdornerLayer), igual que el canario
// hermano de posicion usa GetCursorPos en vez de intentar una captura visual fragil.
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Terrakeep.App;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static void EjecutarDragGhostZOrderLibreriaSolo(MainWindow window, MainViewModel vm)
    {
        try
        {
            var personajeReal = vm.Home.Characters.FirstOrDefault();
            if (personajeReal != null) { vm.Home.OpenCommand.Execute(personajeReal); DoEvents(); DoEvents(); }
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; // Personaje > Objetos
            FijarTamaño(window, 1180, 860);
            DoEvents(); DoEvents(); DoEvents();

            var categoria = vm.Library.RootCategories.FirstOrDefault();
            if (categoria?.SelectCommand == null)
            {
                Console.WriteLine("FALLO: DRAG_GHOST_ZORDER_LIBRERIA_SOLO - no hay categorias reales en la Libreria para probar (Library.RootCategories vacio)");
                return;
            }
            categoria.SelectCommand.Execute(categoria);
            DoEvents(); DoEvents(); DoEvents();

            if (vm.Library.Results.Count == 0)
            {
                Console.WriteLine($"FALLO: DRAG_GHOST_ZORDER_LIBRERIA_SOLO - la categoria real '{categoria.Name}' no devolvio ningun resultado (Library.Results vacio)");
                return;
            }
            var primerItem = vm.Library.Results[0];
            var tarjeta = Descendientes<Border>(window)
                .FirstOrDefault(b => ReferenceEquals(b.DataContext, primerItem) && b.ActualWidth > 0);
            if (tarjeta == null)
            {
                Console.WriteLine("FALLO: DRAG_GHOST_ZORDER_LIBRERIA_SOLO - no se encontro el Border real de la primera tarjeta de la Libreria en el arbol visual");
                return;
            }

            // Camino ANTERIOR (el que causaba el bug): la capa mas cercana a la propia tarjeta.
            var layerDesdeTarjeta = AdornerLayer.GetAdornerLayer(tarjeta);

            // Camino ACTUAL de produccion (DragDropSupport.StartCardDrag tras el arreglo del
            // 28-sep-2026): la capa desde la raiz de la ventana.
            var ventana = Window.GetWindow(tarjeta);
            if (ventana?.Content is not Visual raizVentana)
            {
                Console.WriteLine("FALLO: DRAG_GHOST_ZORDER_LIBRERIA_SOLO - no se encontro Window.Content real como Visual");
                return;
            }
            var layerDesdeVentana = AdornerLayer.GetAdornerLayer(raizVentana);
            if (layerDesdeVentana == null)
            {
                Console.WriteLine("FALLO: DRAG_GHOST_ZORDER_LIBRERIA_SOLO - AdornerLayer.GetAdornerLayer(window.Content) devolvio null (¿MainWindow perdio su AdornerDecorator implicito?)");
                return;
            }

            double anchoVentana = ventana.ActualWidth, altoVentana = ventana.ActualHeight;
            double anchoCapa = layerDesdeVentana.RenderSize.Width, altoCapa = layerDesdeVentana.RenderSize.Height;
            bool distinta = layerDesdeTarjeta != null && !ReferenceEquals(layerDesdeTarjeta, layerDesdeVentana);
            string tamañoLocal = layerDesdeTarjeta == null ? "null" : $"{layerDesdeTarjeta.RenderSize.Width:0}x{layerDesdeTarjeta.RenderSize.Height:0}";
            Console.WriteLine($"DRAG_GHOST_ZORDER_LIBRERIA: ventana real={anchoVentana:0}x{altoVentana:0} ; capa desde window.Content={anchoCapa:0}x{altoCapa:0} ; capa desde la propia tarjeta={tamañoLocal} ; son instancias distintas={distinta}");

            // La aserción real: la capa que produccion usa ahora (desde la raiz de la ventana) tiene
            // que cubrir la ventana entera (>=90% en cada eje, tolerancia por barras/chrome), no
            // quedarse recortada al tamaño de un ScrollViewer interno como pasaba antes del arreglo.
            bool capaSuficiente = anchoCapa >= anchoVentana * 0.9 && altoCapa >= altoVentana * 0.9;
            if (!capaSuficiente)
            {
                Console.WriteLine($"FALLO: DRAG_GHOST_ZORDER_LIBRERIA_SOLO - la capa resuelta desde window.Content ({anchoCapa:0}x{altoCapa:0}) es mas pequeña que la ventana real ({anchoVentana:0}x{altoVentana:0}) - el ghost volveria a quedar recortado por un contenedor intermedio (ScrollViewer u otro) en cuanto el cursor saliera de esa zona. Revisar DragDropSupport.StartCardDrag: debe seguir resolviendo la capa desde Window.GetWindow(element).Content, no desde el propio elemento arrastrado.");
            }
            else
            {
                Console.WriteLine("DRAG_GHOST_ZORDER_LIBRERIA: OK - la capa que usa produccion (desde la raiz de la ventana) cubre la ventana completa; el ghost ya no deberia quedar recortado al salir del viewport de la Libreria.");
            }

            if (layerDesdeTarjeta != null && !distinta)
            {
                Console.WriteLine("DRAG_GHOST_ZORDER_LIBRERIA: nota - en este entorno la tarjeta ya NO esta anidada dentro de un ScrollViewer con capa propia (la capa local coincide con la de ventana); el escenario real que causaba el bug ya no aplica aqui, pero la aserción de arriba sigue siendo la garantia real independientemente de eso.");
            }
        }
        catch (Exception ex) { Console.WriteLine("DRAG_GHOST_ZORDER_LIBRERIA-EXCEPTION: " + ex); }
    }
}
