// AR-EX6 (15-sep-2026): cierre de un hueco real de KeepQA, reportado por el usuario mirando su
// propia pantalla con la ventana reducida (~1180px): "los botones de la segunda fila de la barra
// de herramientas de Exploracion (−/+/Restablecer/Ajustar a la ventana/Exportar a PNG...)
// aparecen solapados/apretados". Cita literal, muy en serio: "keepqa tambien dejo pasar esto...
// eso es un problema critico para mi en keepqa que deje pasar cosas tan obvias".
//
// LA CAUSA REAL, confirmada aqui con geometria real (no a ojo - volcado completo de la fila,
// ver el Console.WriteLine "fila completa del WrapPanel exterior" que este chequeo imprime a
// cada ancho) - NO es la que sugeria a primera vista el codigo (un StackPanel horizontal sin
// wrap empujado fuera de la ventana): a NINGUNO de los 6 anchos reales probados el StackPanel de
// zoom pide mas ancho del que cabe (fuera-de-ventana=0 siempre) - el StackPanel de 410px de
// ancho SIEMPRE cabe de sobra, incluso en 1080px. El bug real es otro, MEDIDO con un barrido
// fino adicional (1080/1150/1160/1163/1164/1170/1180, no forma parte del array permanente de
// abajo): el WrapPanel exterior (MainWindow.xaml:4412) SI envuelve correctamente el StackPanel
// de zoom (MainWindow.xaml:4440-4472, Margin="20,0,0,0") a una segunda linea en cuanto la fila
// entera (2 botones + titulo + insignia + zoom) no cabe en una sola - eso pasa a CUALQUIER ancho
// de 1080 a 1170px con el mundo real usado aqui (roca_negra.wld, titulo corto "roca negra"; un
// titulo mas largo empeoraria el umbral, no lo arreglaria), y deja de pasar justo a partir de
// 1180px (medido: a 1170px SI envuelve, a 1180px NO - un margen de apenas ~10-19px, exactamente
// el tipo de umbral fragil que R-11 ya advertia "el margen es pequeño"). El problema NO es que
// el wrap ocurra (eso es el comportamiento querido) sino que, CUANDO ocurre, el StackPanel de
// zoom no lleva NINGUN margen SUPERIOR (solo "20,0,0,0", izquierda) - asi que su borde de arriba
// queda a 0px EXACTOS del borde de abajo de la linea 1 (medido: linea 1 termina en y=195,63px,
// el StackPanel de zoom empieza en y=196,00px - un hueco de 0,37px, indistinguible de cero y por
// debajo de cualquier tolerancia visual real), las dos lineas se tocan borde a borde y SE LEEN
// como "apretados/solapados" exactamente igual que reporto el usuario. Es LA MISMA categoria de
// bug, con el mismo numero (0px de salto de linea), que el caso YA CATALOGADO en
// KeepQA/src/regresion/casos/terrakeep-cabecera-botones-sin-espacio-salto-linea.json (la barra
// de Personaje, arreglada dandole 8px de margen superior a cada hijo directo del WrapPanel) -
// solo que en una fila DISTINTA (Exploracion) que nadie habia medido nunca con el arnes.
//
// POR QUE NINGUNA PIEZA DE KeepQA LO CAZO ANTES (el hueco real que este chequeo cierra):
//   - verificarEspaciado.js (Spacing Intelligence, 14-sep-2026, "salto_de_linea") SI sabe medir
//     EXACTAMENTE este tipo de bug - es la misma pieza que ya cazo el caso gemelo de la barra de
//     Personaje - pero nunca recibio un volcado de ESTA fila (la de Exploracion). No es un hueco
//     de logica, es un hueco de COBERTURA: nadie habia barrido este WrapPanel en concreto a un
//     ancho donde de verdad envuelve.
//   - verificarGeometria.js (CONTENCION/SOLAPE) tampoco recibio nunca un volcado de esta fila -
//     y aunque lo hubiera recibido, un hueco de 0px entre dos hermanos que NO se solapan (solo
//     se tocan) esta en el limite exacto de lo que CONTENCION detecta (¿un hijo se sale de su
//     padre?, no aplica aqui: ninguno se sale) frente a lo que SOLAPE detecta (dos hermanos que
//     se pisan de verdad, con area de interseccion) - un hueco de 0px SIN pisarse cae en la
//     categoria Spacing Intelligence, no en las dos comprobaciones originales de verificarGeometria.js.
//   - AR-02/AR-11/AR-EX1..5 (los chequeos ya existentes de Exploracion en este mismo arnes)
//     cubren la COLUMNA LATERAL (categorias/resultados/chips de Cofres) - ninguno mide la barra
//     SUPERIOR. El propio comentario de MainWindow.xaml:4406-4411 ("R-11... lo hacen imposible
//     en vez de solo improbable") documenta la INTENCION del arreglo de R-11 (evitar que el
//     titulo largo empuje el bloque de zoom fuera de la ventana) pero nunca midio el caso limite
//     REAL con el arnes ni el efecto secundario de que el propio wrap, al ocurrir, deja 0px de
//     aire - una afirmacion sin verificar, exactamente el patron que este chequeo cierra.
//   - Este chequeo (AR-EX6) es la pieza que faltaba: vive en el barrido PERMANENTE (sin
//     variable de entorno, se ejecuta en cualquier `dotnet run` completo del arnes, igual que
//     AR-01..15/AR-EX1..5) para que una regresion futura de este mismo tipo no pueda volver a
//     colarse en silencio. Tambien expone EXPTOOLBAR_SOLO=1 (ver Program.cs) para diagnostico
//     rapido aislado, mismo patron que VITALS_SOLO/SIDEBAR_SOLO/EX3_SOLO. Comprueba ADEMAS (por
//     si un arreglo futuro cambiase el mecanismo) el caso de desbordamiento horizontal puro
//     (boton mas alla del borde derecho de la ventana) y el solape directo entre botones - las
//     otras dos formas honestas en que esta misma fila podria romperse.
//
// Vive en su propio fichero (no en el Main() de 7000+ lineas de Program.cs) por la MISMA razon
// real ya documentada en AuditoriaMaquetacion.cs: varias sesiones tocan Program.cs a la vez y ya
// colisionaron una vez ahi mismo (dos bloques "AR-16" a la vez). Como clase parcial de Program,
// este chequeo ve TODOS los helpers ya existentes (Descendientes, Recorte, RectVisible,
// FijarTamaño, DoEvents...) sin duplicar nada.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    // Las mismas 6 resoluciones ya usadas esta noche para la franja de vitales (VITALS_SOLO) y
    // documentadas en KeepQA/src/resoluciones/catalogo.json: MINIMO, ARRANQUE (tamaño por
    // defecto real de la ventana - el caso que reporto el usuario), dos paradas mas cruzando los
    // breakpoints Normal/Amplio de Terrakeep (NormalMinWidth=1300, AmplioMinWidth=1500/1520), y
    // FULLHD como techo comun real - MAS una septima parada propia de este chequeo, necesaria
    // por un motivo real medido con el barrido fino de diagnostico (ver la cabecera del
    // fichero): el umbral exacto donde el WrapPanel exterior deja de envolver la fila de zoom
    // esta en 1170px->envuelve / 1180px->no envuelve. Las 6 paradas "de catalogo" por si solas
    // SOLO cazan el bug en el extremo MINIMO (1080px) y dejan sin cubrir TODO el hueco real de
    // 1081 a 1179px - justo donde cayo el caso real que reporto el usuario ("parece ~1180px",
    // una estimacion visual, no un numero exacto). Sin esta septima parada, este mismo chequeo
    // habria repetido el error que se le pide cerrar: "pasa a 1080 y a 1180, luego no hay bug".
    private static readonly (double ancho, double alto)[] ResolucionesBarraExploracion =
    {
        (1080, 700),  // minimo-terrakeep: MinWidth/MinHeight real de la ventana.
        (1170, 860),  // ZONA DE PELIGRO REAL (propia de este chequeo): el ultimo ancho medido
                       // donde el WrapPanel exterior SIGUE envolviendo la fila de zoom - a
                       // 1180px (la parada siguiente) ya no envuelve. Cubre de verdad el hueco
                       // 1081-1179px que ninguna de las 6 paradas de catalogo tocaba.
        (1180, 860),  // arranque-terrakeep: tamaño inicial real con el que abre Terrakeep.
        (1320, 860),  // justo por encima de NormalMinWidth=1300.
        (1500, 860),  // amplio: justo en AmplioMinWidth.
        (1520, 860),  // AmplioMinWidth real medido por AR-14 (el umbral que de verdad usa el ViewModel).
        (1920, 1080), // fullhd: el tamaño de pantalla completa mas comun.
    };

    private static void EjecutarComprobacionBarraExploracion(Window window, MainViewModel vm)
    {
        try
        {
            if (!vm.Exploration.IsWorldLoaded)
            {
                string worldPathEx6 = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld";
                if (!File.Exists(worldPathEx6))
                {
                    Console.WriteLine("AR-EX6: roca_negra.wld no esta en esta maquina y no hay ningun mundo ya cargado - omitido (la fila de zoom solo existe con IsWorldLoaded=true)");
                    return;
                }
                vm.SelectedTabIndex = 4; // Exploracion
                DoEvents();
                var cargaEx6 = vm.Exploration.LoadFromPathAsync(worldPathEx6);
                while (!cargaEx6.IsCompleted) DoEvents();
                DoEvents(); DoEvents();
            }
            vm.SelectedTabIndex = 4; // Exploracion
            DoEvents(); DoEvents();

            // Los 5 botones de la fila de zoom, localizados por su CONTENIDO REAL ya resuelto
            // (dos son literales fijos en el XAML - "-"/"+" -, los otros tres vienen de
            // vm.Loc[...] tal cual el binding los resuelve en el idioma activo - nunca un
            // literal en español a secas, para que el chequeo siga siendo valido en ingles).
            var etiquetasEsperadas = new[]
            {
                "\u2212",                               // "−" (Content="&#8722;" en el XAML)
                "+",
                vm.Loc["action_reset"],
                vm.Loc["explore_fit_window"],
                vm.Loc["explore_export_png"],
            };

            foreach (var (w, h) in ResolucionesBarraExploracion)
            {
                FijarTamaño(window, w, h);
                DoEvents(); DoEvents();

                var botonesZoom = new List<Button>();
                foreach (var etiqueta in etiquetasEsperadas)
                {
                    var boton = Descendientes<Button>(window)
                        .FirstOrDefault(b => (b.Content as string) == etiqueta && b.IsVisible);
                    if (boton != null) botonesZoom.Add(boton);
                }
                if (botonesZoom.Count != etiquetasEsperadas.Length)
                {
                    Console.WriteLine($"AR-EX6: a {w}x{h}, solo se encontraron {botonesZoom.Count}/{etiquetasEsperadas.Length} botones de la fila de zoom (¿IsWorldLoaded=false o cambio el XAML?) - omitido a este tamaño");
                    continue;
                }

                // El StackPanel Horizontal que los agrupa a todos (MainWindow.xaml:4440) - subir
                // desde el primer boton hasta el primer StackPanel ancestro.
                DependencyObject? asc = botonesZoom[0];
                StackPanel? stackZoom = null;
                while (asc != null)
                {
                    asc = System.Windows.Media.VisualTreeHelper.GetParent(asc);
                    if (asc is StackPanel sp) { stackZoom = sp; break; }
                }

                var rectVentana = new Rect(0, 0, window.ActualWidth, window.ActualHeight);
                var rectsBotones = botonesZoom.Select(b =>
                    (etiqueta: (b.Content as string) ?? "?",
                     rect: b.TransformToAncestor(window).TransformBounds(new Rect(0, 0, b.ActualWidth, b.ActualHeight)))).ToList();

                // (a) DESBORDAMIENTO: ¿algun boton de la fila de zoom pide un borde derecho mas
                // alla del ancho real de la ventana? Esto es lo que WrapPanel/StackPanel NO
                // pueden evitar por si solos (ver el porque en la cabecera de este fichero) -
                // el sintoma real que reporto el usuario ("apretados/solapados" cerca del borde).
                double margenTolerancia = 1.0;
                var fueraDeVentana = rectsBotones.Where(rb => rb.rect.Right > rectVentana.Right + margenTolerancia).ToList();

                // (b) SOLAPE ENTRE HERMANOS: dos botones de la MISMA fila de zoom que se pisen
                // entre si (no deberia pasar nunca dentro de un StackPanel bien medido, pero se
                // comprueba con numeros reales, no se asume).
                var solapesEntreBotones = new List<string>();
                for (int i = 0; i < rectsBotones.Count; i++)
                    for (int j = i + 1; j < rectsBotones.Count; j++)
                        if (rectsBotones[i].rect.IntersectsWith(rectsBotones[j].rect))
                            solapesEntreBotones.Add($"'{rectsBotones[i].etiqueta}'x'{rectsBotones[j].etiqueta}'");

                // (c) el StackPanel de zoom colisionando con el resto de la fila (titulo del
                // mundo/insignia "Solo lectura"/botones de cargar mundo) - el otro sintoma
                // posible de "apretados": no fuera de ventana, sino invadiendo el hueco de un
                // vecino en la MISMA linea del WrapPanel si el reparto de MeasureOverride se
                // equivoca con un titulo de mundo largo.
                var solapesConVecinos = new List<string>();
                if (stackZoom != null)
                {
                    var rectStackZoom = stackZoom.TransformToAncestor(window).TransformBounds(new Rect(0, 0, stackZoom.ActualWidth, stackZoom.ActualHeight));
                    DependencyObject? ascWrap = stackZoom;
                    WrapPanel? wrapExterior = null;
                    while (ascWrap != null)
                    {
                        ascWrap = System.Windows.Media.VisualTreeHelper.GetParent(ascWrap);
                        if (ascWrap is WrapPanel wp) { wrapExterior = wp; break; }
                    }
                    if (wrapExterior != null)
                    {
                        // Volcado COMPLETO de la fila (siempre, no solo cuando falla) - la unica
                        // forma honesta de distinguir "no cabe y desborda" de "cabe de sobra pero
                        // el WrapPanel decide mal el reparto" es ver las cinco cajas reales a la
                        // vez, no solo la del bloque de zoom.
                        var volcadoFila = wrapExterior.Children.OfType<FrameworkElement>()
                            .Where(c => c.IsVisible)
                            .Select(c =>
                            {
                                var r = c.TransformToAncestor(window).TransformBounds(new Rect(0, 0, c.ActualWidth, c.ActualHeight));
                                string contenido = (c as ContentControl)?.Content as string ?? (c is TextBlock tbc ? tbc.Text : null) ?? "";
                                return $"{c.GetType().Name}('{contenido}')@x={r.X:0},y={r.Y:0},w={r.Width:0},h={r.Height:0}";
                            });
                        Console.WriteLine($"AR-EX6: a {w}x{h}, fila completa del WrapPanel exterior ({wrapExterior.Children.OfType<FrameworkElement>().Count(c => c.IsVisible)} hijo(s) visible(s)): {string.Join(" | ", volcadoFila)}");

                        foreach (var hermano in wrapExterior.Children.OfType<FrameworkElement>())
                        {
                            if (ReferenceEquals(hermano, stackZoom) || !hermano.IsVisible) continue;
                            var rectHermano = hermano.TransformToAncestor(window).TransformBounds(new Rect(0, 0, hermano.ActualWidth, hermano.ActualHeight));
                            if (rectStackZoom.IntersectsWith(rectHermano))
                                solapesConVecinos.Add($"{hermano.GetType().Name}@{rectHermano}");
                        }
                    }

                    var (rx, ry) = Recorte(stackZoom);
                    string anchoStack = $"{stackZoom.ActualWidth:0}px, borde derecho real={rectStackZoom.Right:0}px (ventana={rectVentana.Right:0}px)";
                    Console.WriteLine($"AR-EX6: a {w}x{h}, StackPanel de zoom -> ancho={anchoStack}, recorte-clip=({rx:0},{ry:0}), fuera-de-ventana={fueraDeVentana.Count} boton(es), solapes-entre-botones={solapesEntreBotones.Count}, solapes-con-vecinos-de-fila={solapesConVecinos.Count}");
                }
                else
                {
                    Console.WriteLine($"AR-EX6: a {w}x{h}, no se encontro el StackPanel ancestro de los botones de zoom (¿cambio la estructura de MainWindow.xaml?)");
                }

                foreach (var rb in fueraDeVentana)
                    Console.WriteLine($"FALLO: AR-EX6 - el boton '{rb.etiqueta}' de la fila de zoom de Exploracion queda {rb.rect.Right - rectVentana.Right:0}px mas alla del borde derecho real de la ventana a {w}x{h} (borde del boton={rb.rect.Right:0}px, ventana={rectVentana.Right:0}px)");
                if (solapesEntreBotones.Count > 0)
                    Console.WriteLine($"FALLO: AR-EX6 - {solapesEntreBotones.Count} par(es) de botones de la fila de zoom se solapan entre si a {w}x{h}: {string.Join(", ", solapesEntreBotones)}");
                if (solapesConVecinos.Count > 0)
                    Console.WriteLine($"FALLO: AR-EX6 - el bloque de zoom se solapa con {solapesConVecinos.Count} elemento(s) vecino(s) de la misma fila a {w}x{h}: {string.Join(", ", solapesConVecinos)}");
            }

            FijarTamaño(window, 1180, 860);
            DoEvents(); DoEvents();
        }
        catch (Exception ex) { Console.WriteLine("AR-EX6-EXCEPTION: " + ex); }
    }
}
