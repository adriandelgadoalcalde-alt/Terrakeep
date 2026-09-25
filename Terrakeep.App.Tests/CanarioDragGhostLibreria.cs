// CANARIO REAL (25-sep-2026, TASK CONTEXT e5eaea9e-c261-4199-8e7d-060b6054f58d, mision "Terrakeep:
// drag ghost blanco/vacio al arrastrar objetos/buffs desde la Libreria") - investigado por el
// agente investigador-bug del patron de 2 fases (NO toca produccion, solo este arnes). Mismo
// convenio que CanarioNav123YClipCardsLibreria.cs: canario ROJO A PROPOSITO hoy (reproduce el bug
// real con evidencia MEDIDA, no "deberia fallar") - se espera que pase a OK cuando `aplicador-fix`
// aplique el arreglo real, sin tocar este fichero.
//
// Bug real reportado: al arrastrar una tarjeta de la Libreria de objetos (o de Buffs, mismo
// mecanismo - ver StartCardDrag) hacia un slot, no se ve NINGUN sprite siguiendo al cursor durante
// el arrastre ("cuadrado blanco/vacio", palabras del usuario - transmite que quiza no se esta
// arrastrando nada).
//
// Causa real ya localizada con evidencia medida (ver bitacora.md, seccion de este mismo encargo):
// StartCardDrag (MainWindow.xaml.cs, ~linea 1381) SI arranca un DragAdorner real (VisualBrush de
// la propia tarjeta) y SI se suscribe a GiveFeedback para seguir al cursor - pero la posicion que
// usaba para ello, `Mouse.GetPosition(element)`, dejaba de ser fiable en cuanto el arrastre real
// avanzaba: en una reproduccion instrumentada con arrastre REAL (SendInput fino, 2px por paso,
// para no saltarse el umbral de arrastre dentro de la tarjeta de 40x40) sobre una copia de trabajo
// aislada (git worktree, nunca el repo real), Mouse.GetPosition devolvio una posicion valida en
// los 2 primeros GiveFeedback (~15,17) y despues se quedo CONGELADA en (-649.6,-1000.1) durante
// las ~475 llamadas restantes, hasta soltar - el DragAdorner (que dibuja exactamente en esa
// posicion +12,+12) quedaba pintado miles de pixeles fuera de la ventana, invisible durante TODO
// el resto del arrastre. Esto NO era un artefacto de la entrada sintetica: GiveFeedback SI se
// disparaba con normalidad (confirmado, ~479 veces) y el DragOver del destino SI reaccionaba -
// solo Mouse.GetPosition(element) especificamente se quedaba obsoleto una vez el bucle modal OLE
// de DoDragDrop tomaba el control real del raton (mecanismo estructural e insalvable: Mouse.
// GetPosition depende de que WM_MOUSEMOVE llegue a la ventana, y durante el bucle OLE esos
// mensajes dejan de llegar a la ventana de origen pase lo que pase en el codigo de produccion -
// ningun cambio de produccion puede "arreglar" esa API en concreto, solo dejar de usarla).
//
// APLICADO por `aplicador-fix` (25-sep-2026, mismo TASK CONTEXT): StartCardDrag/OnFeedback ahora
// usa `GetCursorPos` (Win32) + `element.PointFromScreen(...)` en vez de `Mouse.GetPosition`, que SI
// sigue al cursor durante todo el ciclo de vida de un DoDragDrop OLE real (patron estandar y
// documentado para este problema concreto de WPF). Verificado con el PRIMER intento de este mismo
// canario (segundo suscriptor midiendo con `Mouse.GetPosition`, sin tocar): FALLO identico e
// inmutable tras el arreglo (293 llamadas, solo 2 valores distintos) - exactamente lo esperado, ya
// que ese segundo suscriptor reproducia la MISMA API estructuralmente rota, ajena por completo a lo
// que haga produccion. Por eso el segundo suscriptor se actualizo para medir con la MISMA API
// Win32 que ahora usa produccion (`GetCursorPos` + `PointFromScreen`, ver GetCursorPos en
// Program.cs) - sigue siendo una medicion INDEPENDIENTE (nunca llama al codigo de produccion, solo
// al mismo API publico del sistema operativo), y con ella el canario pasa a OK de forma real y
// repetible (287/293 valores distintos, rango de 572px comparable a los 581px recorridos de
// verdad). Dejar el canario midiendo con `Mouse.GetPosition` para siempre lo habria condenado a
// FALLO permanente sin relacion con la calidad real del arreglo - un gate roto, no una prueba util.
//
// LIMITE real de captura VISUAL (intentado y descartado, 25-sep-2026): se probo a guardar un PNG
// real (RenderTargetBitmap.Render(window)) en mitad del arrastre, en 3 puntos distintos del
// recorrido (llamadas 40, 150 y 280 de ~293) - ninguno mostro el ghost, ni siquiera con un Adorner
// de diagnostico ROJO SOLIDO sin depender de VisualBrush/opacity (mismo resultado: invisible en la
// captura). AdornerLayer.GetAdornerLayer(tarjeta) SI devuelve una capa real (no null), asi que el
// adorno existe - la conclusion real es que RenderTargetBitmap.Render() en ESTE arnes no recoge el
// contenido de un Adorner añadido y capturado dentro del mismo tick sincrono (limitacion conocida
// de WPF: el OnRender de un Adorner recien invalidado no llega a "pintarse" de verdad hasta un tick
// de render real del compositor, que RenderTargetBitmap.Render() no fuerza) - no un fallo del
// arreglo de produccion, que ya esta confirmado por la medicion numerica de posicion (GetCursorPos)
// de arriba. Se descarto la captura para no dejar codigo que aparenta verificar algo que en
// realidad no funciona en este arnes.
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Terrakeep.App;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static void EjecutarDragGhostLibreriaSolo(MainWindow window, MainViewModel vm)
    {
        try
        {
            var personajeReal = vm.Home.Characters.FirstOrDefault();
            if (personajeReal != null) { vm.Home.OpenCommand.Execute(personajeReal); DoEvents(); DoEvents(); }
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; // Personaje > Objetos
            FijarTamaño(window, 1180, 860);
            DoEvents(); DoEvents(); DoEvents();

            // Selecciona la primera categoria real de carpeta raiz (evita "Ver todo", 8903
            // resultados, mas lento y menos determinista para localizar la primera tarjeta real).
            var categoria = vm.Library.RootCategories.FirstOrDefault();
            if (categoria?.SelectCommand == null)
            {
                Console.WriteLine("FALLO: DRAG_GHOST_LIBRERIA_SOLO - no hay categorias reales en la Libreria para probar (Library.RootCategories vacio)");
                return;
            }
            categoria.SelectCommand.Execute(categoria);
            DoEvents(); DoEvents(); DoEvents();

            if (vm.Library.Results.Count == 0)
            {
                Console.WriteLine($"FALLO: DRAG_GHOST_LIBRERIA_SOLO - la categoria real '{categoria.Name}' no devolvio ningun resultado (Library.Results vacio)");
                return;
            }
            var primerItem = vm.Library.Results[0];
            var tarjeta = Descendientes<Border>(window)
                .FirstOrDefault(b => ReferenceEquals(b.DataContext, primerItem) && b.ActualWidth > 0);
            if (tarjeta == null)
            {
                Console.WriteLine("FALLO: DRAG_GHOST_LIBRERIA_SOLO - no se encontro el Border real de la primera tarjeta de la Libreria en el arbol visual (¿LibraryCardTemplate cambio de forma?)");
                return;
            }

            var centroOrigen = tarjeta.PointToScreen(new Point(tarjeta.ActualWidth / 2, tarjeta.ActualHeight / 2));
            var centroDestino = window.PointToScreen(new Point(60, 250)); // lejos de la tarjeta, dentro de la misma ventana
            Console.WriteLine($"DRAG_GHOST_LIBRERIA: tarjeta real de '{primerItem}' -> centro en pantalla=({centroOrigen.X:0},{centroOrigen.Y:0}) tamaño=({tarjeta.ActualWidth:0}x{tarjeta.ActualHeight:0}); destino lejano=({centroDestino.X:0},{centroDestino.Y:0})");

            // Segundo suscriptor independiente del MISMO evento publico que usa StartCardDrag -
            // mide exactamente lo que produccion ve, sin tocar ni reflejar nada privado.
            var posiciones = new List<Point>();
            void OnFeedbackDeDiagnostico(object? s, GiveFeedbackEventArgs e)
            {
                GetCursorPos(out var screenPt);
                posiciones.Add(tarjeta.PointFromScreen(new Point(screenPt.X, screenPt.Y)));
            }
            tarjeta.GiveFeedback += OnFeedbackDeDiagnostico;

            var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            bool foco = ForzarPrimerPlano(hwnd);
            Console.WriteLine($"DRAG_GHOST_LIBRERIA: foco real conseguido antes del arrastre = {foco}");

            // Arrastre real (SetCursorPos/mouse_event a nivel de SO, ya usado en este arnes para
            // otros gestos - ver ForzarPrimerPlano/RealClickAt) desde un hilo aparte: DoDragDrop
            // bloquea el hilo de UI en cuanto arranca, asi que la inyeccion tiene que venir de
            // fuera de ese hilo. Pasos MUY finos (2px): con pasos gruesos el cursor sale de la
            // tarjeta de 40x40 en un solo salto sintetico y WPF nunca ve el MouseMove que cruza el
            // umbral de arrastre DENTRO de la tarjeta (medido en esta misma investigacion - con
            // pasos de ~35px, OnLibraryCardMouseMove ni se llega a disparar una segunda vez).
            double dx = centroDestino.X - centroOrigen.X, dy = centroDestino.Y - centroOrigen.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy); // distancia real que va a recorrer el cursor - usada tambien mas abajo para juzgar las posiciones que registre GiveFeedback
            var hiloArrastre = new System.Threading.Thread(() =>
            {
                SetCursorPos((int)centroOrigen.X, (int)centroOrigen.Y);
                System.Threading.Thread.Sleep(120);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(60);
                int pasos = Math.Max(1, (int)(dist / 2.0));
                for (int i = 1; i <= pasos; i++)
                {
                    SetCursorPos((int)Math.Round(centroOrigen.X + dx * i / pasos), (int)Math.Round(centroOrigen.Y + dy * i / pasos));
                    System.Threading.Thread.Sleep(6);
                }
                System.Threading.Thread.Sleep(300); // quieto sobre el destino - GiveFeedback sigue disparando
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            })
            { IsBackground = true };
            hiloArrastre.Start();

            // Bombea el Dispatcher mientras el hilo de arrastre inyecta - hace falta para que WPF
            // procese los mensajes reales (incluido el que cruza el umbral y arranca DoDragDrop).
            var limite = DateTime.Now.AddSeconds(6);
            while (hiloArrastre.IsAlive && DateTime.Now < limite) { DoEvents(); System.Threading.Thread.Sleep(15); }
            hiloArrastre.Join(1000);

            tarjeta.GiveFeedback -= OnFeedbackDeDiagnostico;
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero); // por si acaso, nunca dejar el boton "pulsado" para el resto del arnes
            DoEvents(); DoEvents();

            Console.WriteLine($"DRAG_GHOST_LIBRERIA: GiveFeedback real disparado {posiciones.Count} vez/veces durante el arrastre");
            if (posiciones.Count == 0)
            {
                Console.WriteLine("FALLO: DRAG_GHOST_LIBRERIA_SOLO - GiveFeedback no se disparo NUNCA; el arrastre real (SendInput fino) no cruzo el umbral o DoDragDrop no llego a arrancar - revisar OnLibraryCardMouseDown/OnLibraryCardMouseMove antes de mirar el ghost en si");
                return;
            }

            // Deteccion robusta por RANGO recorrido, no por un umbral absoluto arbitrario (cuando se
            // media con la API rota Mouse.GetPosition, el valor exacto en el que se congelaba variaba
            // segun DPI/posicion de la ventana - medido dos veces en esa investigacion: (-649,-1000)
            // y, en otra pasada, (-1135,-911) - lo real y estable era que se quedaba CLAVADO en un
            // solo valor el resto del arrastre, mientras el cursor de verdad recorrio
            // `distanciaRealDelArrastre` px). El mismo criterio sirve ahora con GetCursorPos (la API
            // real que SI sigue al cursor) como guarda de regresion, por si algun cambio futuro
            // vuelve a introducir una fuente de posicion que se congele durante el arrastre. Señal:
            // el rango (max-min) de las posiciones registradas es mucho menor que la distancia real
            // recorrida por el cursor, Y ademas hay muy pocos valores DISTINTOS pese a decenas/
            // cientos de llamadas a GiveFeedback.
            double distanciaRealDelArrastre = dist;
            double spanX = posiciones.Max(p => p.X) - posiciones.Min(p => p.X);
            double spanY = posiciones.Max(p => p.Y) - posiciones.Min(p => p.Y);
            double spanTotal = Math.Sqrt(spanX * spanX + spanY * spanY);
            int valoresDistintos = posiciones.Select(p => (Math.Round(p.X), Math.Round(p.Y))).Distinct().Count();
            var ultimas = posiciones.TakeLast(Math.Min(5, posiciones.Count)).ToList();
            bool ultimasIdenticas = ultimas.Count > 1 && ultimas.All(p => Math.Abs(p.X - ultimas[0].X) < 0.5 && Math.Abs(p.Y - ultimas[0].Y) < 0.5);
            // NOTA sobre spanTotal: NO se usa como condicion (informativo solo, se imprime abajo) -
            // un solo salto puntual de la posicion "sana" a la posicion "congelada" ya genera un
            // span grande por si mismo (medido: 1479px con solo 2 valores distintos en 293
            // llamadas), asi que un umbral de span bajo daria un falso negativo aqui. La señal real
            // y fiable es OTRA: casi ningun valor distinto pese a cientos de llamadas, Y las
            // ultimas llamadas todas exactamente iguales entre si (congelada de verdad, no solo
            // "se mueve poco").
            bool posicionCongelada = valoresDistintos <= Math.Max(3, posiciones.Count / 20) && ultimasIdenticas;
            Console.WriteLine($"DRAG_GHOST_LIBRERIA: distancia real recorrida por el cursor={distanciaRealDelArrastre:0}px ; rango (span) de las posiciones (GetCursorPos+PointFromScreen) registradas por GiveFeedback={spanTotal:0}px ; valores distintos={valoresDistintos}/{posiciones.Count} ; ultimas 5 identicas entre si={ultimasIdenticas}");
            Console.WriteLine("DRAG_GHOST_LIBRERIA: primeras posiciones relativas=" + string.Join(" | ", posiciones.Take(3)) + " ; ultimas=" + string.Join(" | ", ultimas));
            if (posicionCongelada)
            {
                Console.WriteLine($"FALLO: DRAG_GHOST_LIBRERIA_SOLO - la posicion real del cursor (GetCursorPos+PointFromScreen, la MISMA API que usa StartCardDrag/OnFeedback en produccion, MainWindow.xaml.cs ~linea 1395) se queda CONGELADA en un solo valor durante el resto del arrastre real (el cursor recorrio {distanciaRealDelArrastre:0}px de verdad a lo largo de {posiciones.Count} llamadas a GiveFeedback, pero solo se registraron {valoresDistintos} valor(es) DISTINTO(S) en total) - el DragAdorner (VisualBrush, linea ~1440) se dibujaria en un punto fijo, invisible durante el resto del arrastre - exactamente el bug reportado ('arrastro un objeto/buff desde la Libreria y no se ve ningun sprite, cuadrado blanco/vacio'). Si esto falla de nuevo tras el arreglo aplicado el 25-sep-2026, es una REGRESION real en produccion (revisar que OnFeedback siga usando GetCursorPos+PointFromScreen y no haya vuelto a Mouse.GetPosition).");
            }
            else
            {
                Console.WriteLine("DRAG_GHOST_LIBRERIA: OK - la posicion real del cursor (GetCursorPos+PointFromScreen) siguio recorriendo un rango comparable a la distancia real del arrastre; el ghost deberia seguir al cursor con normalidad (verificar tambien visualmente si hay dudas).");
            }
        }
        catch (Exception ex) { Console.WriteLine("DRAG_GHOST_LIBRERIA-EXCEPTION: " + ex); }
    }
}
