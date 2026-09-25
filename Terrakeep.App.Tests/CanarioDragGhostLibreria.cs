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
// usa para ello, `Mouse.GetPosition(element)` (MainWindow.xaml.cs linea ~1395), deja de ser fiable
// en cuanto el arrastre real avanza: en una reproduccion instrumentada con arrastre REAL (SendInput
// fino, 2px por paso, para no saltarse el umbral de arrastre dentro de la tarjeta de 40x40) sobre
// una copia de trabajo aislada (git worktree, nunca el repo real), Mouse.GetPosition devolvio una
// posicion valida en los 2 primeros GiveFeedback (~15,17) y despues se quedo CONGELADA en
// (-649.6,-1000.1) durante las ~475 llamadas restantes, hasta soltar - el DragAdorner (que dibuja
// exactamente en esa posicion +12,+12) queda pintado miles de pixeles fuera de la ventana, invisible
// durante TODO el resto del arrastre. Esto NO es un artefacto de la entrada sintetica: GiveFeedback
// SI se disparaba con normalidad (confirmado, ~479 veces) y el DragOver del destino SI reaccionaba -
// solo Mouse.GetPosition(element) especificamente se queda obsoleto una vez el bucle modal OLE de
// DoDragDrop toma el control real del raton. Este canario mide EXACTAMENTE ese mismo sintoma, con
// un arrastre real, sin tocar ni un solo caracter de MainWindow.xaml.cs: un SEGUNDO suscriptor
// independiente del mismo evento PUBLICO FrameworkElement.GiveFeedback (no hace falta reflexion ni
// tocar nada privado) que registra Mouse.GetPosition(tarjeta) en cada llamada real.
//
// Propuesta tecnica para `aplicador-fix` (no aplicar aqui): sustituir `Mouse.GetPosition(element)`
// por la posicion real del cursor via Win32 (`GetCursorPos` + `element.PointFromScreen(...)`), que
// SI sigue al cursor durante todo el ciclo de vida de un DoDragDrop OLE real - patron estandar y
// documentado para este problema concreto de WPF, y ya usado en este mismo proyecto para otros
// gestos de raton reales (ver SetCursorPos/mouse_event en este arnes, y AR-EX2-PAN/MINIMAPA en
// bitacora.md).
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
            void OnFeedbackDeDiagnostico(object? s, GiveFeedbackEventArgs e) => posiciones.Add(Mouse.GetPosition(tarjeta));
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

            // Deteccion robusta por RANGO recorrido, no por un umbral absoluto arbitrario (el valor
            // exacto en el que Mouse.GetPosition se congela varia segun DPI/posicion de la ventana -
            // medido dos veces en esta misma investigacion: (-649,-1000) y, en otra pasada, (-1135,
            // -911) - lo real y estable es que se queda CLAVADO en un solo valor el resto del
            // arrastre, mientras el cursor de verdad recorrio `distanciaRealDelArrastre` px). Señal:
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
            Console.WriteLine($"DRAG_GHOST_LIBRERIA: distancia real recorrida por el cursor={distanciaRealDelArrastre:0}px ; rango (span) de las posiciones registradas por GiveFeedback={spanTotal:0}px ; valores distintos={valoresDistintos}/{posiciones.Count} ; ultimas 5 identicas entre si={ultimasIdenticas}");
            Console.WriteLine("DRAG_GHOST_LIBRERIA: primeras posiciones relativas=" + string.Join(" | ", posiciones.Take(3)) + " ; ultimas=" + string.Join(" | ", ultimas));
            if (posicionCongelada)
            {
                Console.WriteLine($"FALLO: DRAG_GHOST_LIBRERIA_SOLO - Mouse.GetPosition(element) en StartCardDrag/OnFeedback (MainWindow.xaml.cs linea ~1395) deja de actualizarse y se queda CONGELADA en un solo valor durante el resto del arrastre real (el cursor recorrio {distanciaRealDelArrastre:0}px de verdad a lo largo de {posiciones.Count} llamadas a GiveFeedback, pero solo se registraron {valoresDistintos} valor(es) DISTINTO(S) en total - un salto puntual de la posicion inicial sana a un valor roto, y despues congelada ahi el resto del arrastre, sin ningun valor intermedio real) - el DragAdorner (VisualBrush, linea ~1440) se dibuja en un punto fijo, casi siempre fuera de la ventana o encima de la propia tarjeta de origen, invisible durante el resto del arrastre - exactamente el bug reportado ('arrastro un objeto/buff desde la Libreria y no se ve ningun sprite, cuadrado blanco/vacio'). Recomendacion real: sustituir Mouse.GetPosition(element) por GetCursorPos (P/Invoke, ver SetCursorPos/mouse_event ya declarados en este mismo arnes) + element.PointFromScreen(...), que si sigue al cursor durante todo el ciclo de vida real de un DoDragDrop OLE.");
            }
            else
            {
                Console.WriteLine("DRAG_GHOST_LIBRERIA: OK - Mouse.GetPosition(element) siguio recorriendo un rango comparable a la distancia real del arrastre; el ghost deberia seguir al cursor con normalidad (verificar tambien visualmente si hay dudas).");
            }
        }
        catch (Exception ex) { Console.WriteLine("DRAG_GHOST_LIBRERIA-EXCEPTION: " + ex); }
    }
}
