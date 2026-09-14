// KEEPQA_SOLO=1 (14-sep-2026): barrido FRESCO pedido por el coordinador para aplicar el nuevo
// arsenal de KeepQA (geometria/alineacion/contraste/contenido adversarial) contra Terrakeep -
// hasta hoy el UNICO consumidor de la familia que no usaba explicitamente el verificador
// compartido (ver KeepQA/PATRONES.md: "Solo Terrakeep sigue sin usar el verificador compartido
// de KeepQA de forma explicita... pendiente, no decidido, si migrarlo"). Vive en su propio
// fichero, mismo motivo real que AuditoriaMaquetacion.cs (Program.cs ya tiene miles de lineas y
// varias sesiones lo tocan a la vez) - la unica huella en Program.cs es la llamada bajo su propio
// _SOLO.
//
// Dos piezas, las dos NUEVAS (nunca ejecutadas antes en este proyecto):
//
//  1. CONTENIDO ADVERSARIAL (Parte 21 de ESPEC-VISUAL-QA.md): inyecta los 11 casos reales de
//     KeepQA/src/adversarial/catalogo.json (nunca textos inventados sueltos - mismo criterio que
//     ya impone src/resoluciones/) en TRES campos de texto libre reales de la app:
//       - MainViewModel.CharacterName (aparece en la cabecera de Personaje y en el titulo de
//         ventana - WindowTitle).
//       - LibraryViewModel.SearchText (buscador de la Libreria de objetos).
//       - MainViewModel.WhereIsItSearchText (buscador "Donde lo tengo?").
//     Tras cada caso, se comprueba con los MISMOS dos detectores de AR-LAY (D1 contenido perdido
//     sin escape por scroll, D2 solape entre celdas disjuntas de un Grid) si el texto extremo
//     rompe el layout - reutiliza los helpers YA escritos y probados de AuditoriaMaquetacion.cs
//     (misma clase parcial: RectCompleto/ZonaVisible/AlcanzableConScroll/QuienRecorta/Describir/
//     Descendientes), nunca reinventa esa logica.
//
//  2. VOLCADO DE GEOMETRIA para `node src/alineacion/verificarAlineacion.js` (Parte 13,
//     Alignment - la pieza que NINGUN detector propio de Terrakeep cubre hoy: AR-LAY mide
//     contencion/solape/truncado, nunca si un grupo de pestañas esta alineado entre si). Exporta,
//     en el contrato EXACTO {id,tipo,padre_id,x,y,ancho,alto,grupo} que documenta la cabecera de
//     KeepQA/src/geometria/verificarGeometria.js, las cajas reales de TODOS los TabControl
//     visibles (pestañas raiz, internas de Personaje, subpestañas de Objetos, hojas anidadas de
//     Builds/Novedades) en las 6 pantallas raiz - el primer volcado real de Terrakeep para esta
//     pieza compartida de la familia.
//
// Capturas PNG (solo de los casos mas extremos, para no generar cientos de PNG por una pasada) y
// el volcado JSON se dejan en AppContext.BaseDirectory\keepqa-evidencia - node procesa esos
// ficheros aparte, fuera de dotnet, con los scripts reales de KeepQA.
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static void EjecutarKeepQaAdversarialYGeometria(Window window, MainViewModel vm)
    {
        try
        {
            string catalogoPath = @"C:\Users\adrian\Downloads\KeepQA\src\adversarial\catalogo.json";
            if (!File.Exists(catalogoPath))
            {
                Console.WriteLine("KEEPQA_SOLO: catalogo.json no encontrado en " + catalogoPath + " - omitido");
                return;
            }
            using var doc = JsonDocument.Parse(File.ReadAllText(catalogoPath));
            var casos = doc.RootElement.GetProperty("casos")
                .EnumerateArray()
                .Select(c => (
                    id: c.GetProperty("id").GetString()!,
                    categoria: c.GetProperty("categoria").GetString()!,
                    texto: c.GetProperty("texto").GetString()!))
                .ToList();
            Console.WriteLine($"KEEPQA_SOLO: {casos.Count} casos adversariales cargados de {catalogoPath}");

            string outDir = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
            Directory.CreateDirectory(outDir);

            void Captura(string nombre)
            {
                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(window);
                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                using var fs = File.Create(Path.Combine(outDir, nombre + ".png"));
                enc.Save(fs);
            }

            // Mismos dos detectores que AR-LAY (D1 contenido perdido, D2 solape en celdas
            // disjuntas de Grid, ver AuditoriaMaquetacion.cs) - reescritos aqui en pequeño porque
            // Auditar() de alli es una funcion local privada de ese bloque concreto, pero TODA la
            // logica de medicion real (RectCompleto/ZonaVisible/AlcanzableConScroll/QuienRecorta/
            // Describir) es la MISMA, reutilizada tal cual sin reescribir un solo calculo.
            // `raiz` es DELIBERADAMENTE un parametro, no siempre `window`: un Popup de WPF
            // (WhereIsItPopup, StaysOpen=False - ver MainWindow.xaml) NO es hijo visual de la
            // ventana en el sentido de VisualTreeHelper (tiene su propia raiz de presentacion),
            // asi que barrer desde `window` jamas encuentra su contenido y ni RenderTargetBitmap
            // (window) lo captura - confirmado en esta misma ronda: la primera version de este
            // arnes barria/capturaba SIEMPRE desde `window` para los 3 campos, y los 3 PNG de
            // "Donde lo tengo?" salieron BYTE A BYTE IDENTICOS pase lo que pase en
            // WhereIsItSearchText (111232/113471/112358 para CharacterName, todos DISTINTOS,
            // pero 145005/145005/145005 para el popup, los 3 iguales) - la prueba de que nunca se
            // estaba mirando de verdad el popup. Arreglado pasando `popup.Child` como raiz de
            // escaneo y de captura para ese campo en concreto (ver mas abajo).
            List<string> Escanear(FrameworkElement raiz, string contexto)
            {
                var hallazgos = new List<string>();
                foreach (var fe in Descendientes<FrameworkElement>(raiz))
                {
                    if (fe is not (TextBlock or System.Windows.Controls.Primitives.ButtonBase or System.Windows.Controls.TextBox)) continue;
                    if (!fe.IsVisible || fe.ActualWidth < 1 || fe.ActualHeight < 1) continue;
                    if (fe is TextBlock tbv && string.IsNullOrWhiteSpace(tbv.Text)) continue;
                    Rect completo, zona;
                    try { completo = RectCompleto(fe, raiz); zona = ZonaVisible(fe, raiz); }
                    catch (InvalidOperationException) { continue; }
                    double faltaX, faltaY;
                    if (zona.IsEmpty)
                    {
                        if (AlcanzableConScroll(fe, raiz, true) || AlcanzableConScroll(fe, raiz, false)) continue;
                        faltaX = completo.Width; faltaY = completo.Height;
                    }
                    else
                    {
                        faltaX = Math.Min(completo.Width, Math.Max(0, zona.Left - completo.Left) + Math.Max(0, completo.Right - zona.Right));
                        faltaY = Math.Min(completo.Height, Math.Max(0, zona.Top - completo.Top) + Math.Max(0, completo.Bottom - zona.Bottom));
                    }
                    if (faltaX <= 1 && faltaY <= 1) continue;
                    bool escapaX = faltaX <= 1 || AlcanzableConScroll(fe, raiz, horizontal: true);
                    bool escapaY = faltaY <= 1 || AlcanzableConScroll(fe, raiz, horizontal: false);
                    if (escapaX && escapaY) continue;
                    hallazgos.Add($"{contexto}: {Describir(fe)} pierde {(escapaX ? 0 : faltaX):0.#}x{(escapaY ? 0 : faltaY):0.#}px [recorta: {QuienRecorta(fe, raiz)}]");
                }
                foreach (var g in Descendientes<System.Windows.Controls.Grid>(raiz))
                {
                    if (!g.IsVisible || g.Children.Count < 2 || g.Children.Count > 30) continue;
                    if (g.ColumnDefinitions.Count < 2 && g.RowDefinitions.Count < 2) continue;
                    var hijos = g.Children.OfType<FrameworkElement>().Where(c => c.IsVisible && c.ActualWidth > 1 && c.ActualHeight > 1).ToList();
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
                            if (!colsDisjuntas && !filasDisjuntas) continue;
                            Rect ra2, rb2;
                            try { ra2 = RectCompleto(a, raiz); rb2 = RectCompleto(b, raiz); }
                            catch (InvalidOperationException) { continue; }
                            var inter = Rect.Intersect(ra2, rb2);
                            if (inter.IsEmpty || inter.Width <= 1 || inter.Height <= 1) continue;
                            hallazgos.Add($"{contexto}: {Describir(a)} y {Describir(b)} se pisan {inter.Width:0.#}x{inter.Height:0.#}px");
                        }
                }
                return hallazgos;
            }

            void CapturaVisual(System.Windows.Media.Visual visual, int ancho, int alto, string nombre)
            {
                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    Math.Max(1, ancho), Math.Max(1, alto), 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(visual);
                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                using var fs = File.Create(Path.Combine(outDir, nombre + ".png"));
                enc.Save(fs);
            }

            int hallazgosTotal = 0;
            string nombreOriginal = vm.CharacterName ?? "";

            // ---- Campo 1: CharacterName ----
            vm.SelectedTabIndex = 1; // Personaje - la cabecera con el nombre siempre visible
            DoEvents(); DoEvents();
            foreach (var caso in casos)
            {
                vm.CharacterName = caso.texto;
                DoEvents(); DoEvents();
                var h = Escanear(window, $"CharacterName/{caso.id}({caso.categoria})");
                hallazgosTotal += h.Count;
                foreach (var linea in h) Console.WriteLine("   KEEPQA-ADVERSARIAL " + linea);
                if (caso.id is "muy_largo" or "japones" or "arabe") Captura($"adversarial-charactername-{caso.id}");
            }
            vm.CharacterName = nombreOriginal;
            DoEvents();

            // ---- Campo 2: LibraryViewModel.SearchText (buscador de la Libreria de objetos) ----
            vm.SelectedTabIndex = 1; vm.PersonajeInnerTabIndex = 0; vm.ObjetosSubTabIndex = 0;
            DoEvents(); DoEvents();
            foreach (var caso in casos)
            {
                vm.Library.SearchText = caso.texto;
                DoEvents(); DoEvents();
                var h = Escanear(window, $"Libreria.SearchText/{caso.id}({caso.categoria})");
                hallazgosTotal += h.Count;
                foreach (var linea in h) Console.WriteLine("   KEEPQA-ADVERSARIAL " + linea);
                if (caso.id is "muy_largo" or "japones" or "arabe") Captura($"adversarial-librosearch-{caso.id}");
            }
            vm.Library.SearchText = "";
            DoEvents();

            // ---- Campo 3: MainViewModel.WhereIsItSearchText ("Donde lo tengo?") ----
            // Vive dentro de un Popup (x:Name="WhereIsItPopup", IsOpen={Binding IsWhereIsItOpen})
            // - DOS problemas reales encontrados y arreglados en esta misma ronda, los dos
            // confirmados con evidencia real, no supuestos:
            //   1. Sin abrir el popup primero (IsWhereIsItOpen=true) el TextBox nunca se
            //      renderiza - ya arreglado arriba.
            //   2. Un Popup de WPF NO es hijo visual de la ventana (VisualTreeHelper no lo
            //      alcanza desde `window`, tiene su propia raiz de presentacion) - Escanear(window,
            //      ...) y RenderTargetBitmap.Render(window) NUNCA ven su contenido pase lo que
            //      pase dentro. Prueba real: la primera version de este arnes (ya con el popup
            //      abierto) seguia dando los 3 PNG "dondelotengo" BYTE A BYTE IDENTICOS
            //      (145005 bytes los 3), mientras que CharacterName SI daba 3 tamaños distintos
            //      con el mismo cambio de texto - la firma inequivoca de que el popup nunca se
            //      estaba mirando de verdad. Arreglo: usar `popup.Child` (el FrameworkElement
            //      real del contenido del Popup, propiedad publica de System.Windows.Controls.
            //      Primitives.Popup) como RAIZ propia de escaneo y de captura para este campo,
            //      nunca `window`.
            var whereIsItPopup = window.FindName("WhereIsItPopup") as System.Windows.Controls.Primitives.Popup;
            if (whereIsItPopup == null)
                Console.WriteLine("KEEPQA_SOLO: FALLO - no se encuentra WhereIsItPopup por su x:Name, campo 3 omitido");
            else
            {
                vm.IsWhereIsItOpen = true;
                DoEvents(); DoEvents(); DoEvents();
                var raizPopup = whereIsItPopup.Child as FrameworkElement;
                if (raizPopup == null)
                    Console.WriteLine("KEEPQA_SOLO: FALLO - WhereIsItPopup.Child es null incluso con IsOpen=true, campo 3 omitido");
                else
                {
                    foreach (var caso in casos)
                    {
                        vm.WhereIsItSearchText = caso.texto;
                        DoEvents(); DoEvents(); DoEvents();
                        var h = Escanear(raizPopup, $"DondeLoTengo/{caso.id}({caso.categoria})");
                        hallazgosTotal += h.Count;
                        foreach (var linea in h) Console.WriteLine("   KEEPQA-ADVERSARIAL " + linea);
                        if (caso.id is "muy_largo" or "japones" or "arabe")
                            CapturaVisual(raizPopup, (int)Math.Ceiling(raizPopup.ActualWidth), (int)Math.Ceiling(raizPopup.ActualHeight), $"adversarial-dondelotengo-{caso.id}");
                    }
                }
                vm.WhereIsItSearchText = "";
                vm.IsWhereIsItOpen = false;
                DoEvents();
            }

            Console.WriteLine($"KEEPQA_SOLO-ADVERSARIAL: {casos.Count} casos x 3 campos = {casos.Count * 3} combinaciones, {hallazgosTotal} hallazgos de contenido perdido/solape (esperado 0)");
            if (hallazgosTotal > 0) Console.WriteLine($"FALLO: KEEPQA_SOLO-ADVERSARIAL - {hallazgosTotal} hallazgos reales de contenido adversarial rompiendo el layout");

            // ---- Volcado de geometria real para verificarAlineacion.js (Parte 13) ----
            // OJO real, encontrado y arreglado en esta misma ronda: el primer intento daba
            // padre_id=null a TODOS los TabControl de TODAS las pantallas, y el verificador
            // compara como "hermanos" a cualquier par que comparta el mismo padre_id (null
            // incluido, es un valor de agrupacion valido como otro cualquiera) - como cada
            // pantalla se visita EN SECUENCIA (nunca dos a la vez de verdad), los TabControl de
            // Inicio/Personaje/Builds/... quedaban comparados entre si como si coexistieran en
            // pantalla, y el verificador reporto 45 "solapes" falsos (G-SOLAPE-01..45) que en
            // realidad eran solo "estas dos pantallas ocupan la misma zona de la ventana", nunca
            // un bug real - exactamente el tipo de falso positivo por padre_id compartido que
            // PATRONES.md ya documenta para otros consumidores. Arreglo: una raiz SINTETICA
            // distinta por pantalla (id "raiz_<pantalla>", su propia entrada en el volcado con
            // las dimensiones reales de la ventana) para que el padre_id de cada TabControl sea
            // unico por pantalla - los TabControl de pantallas distintas dejan de compararse
            // entre si sin falsear ninguna medida real dentro de cada pantalla.
            var elementos = new List<object>();
            int contador = 0;
            void VolcarTabControl(TabControl tc, string idTc, string padreId, string grupoPrefijo)
            {
                Rect rTc;
                try { rTc = RectCompleto(tc, window); } catch (InvalidOperationException) { return; }
                elementos.Add(new { id = idTc, tipo = "panel_pestanas", padre_id = (string?)padreId, x = rTc.X, y = rTc.Y, ancho = rTc.Width, alto = rTc.Height, grupo = (string?)null });
                for (int i = 0; i < tc.Items.Count; i++)
                {
                    if (tc.ItemContainerGenerator.ContainerFromIndex(i) is not TabItem ti || !ti.IsVisible) continue;
                    Rect rTi;
                    try { rTi = RectCompleto(ti, window); } catch (InvalidOperationException) { continue; }
                    if (rTi.Width < 1 || rTi.Height < 1) continue;
                    elementos.Add(new { id = $"{idTc}_item{i}", tipo = "pestana", padre_id = (string?)idTc, x = rTi.X, y = rTi.Y, ancho = rTi.Width, alto = rTi.Height, grupo = (string?)$"{grupoPrefijo}_{tc.Items.Count}" });
                }
            }

            foreach (string pantalla in new[] { "Inicio", "Personaje", "Builds", "Novedades", "Exploracion", "AcercaDe" })
            {
                vm.SelectedTabIndex = pantalla switch { "Inicio" => 0, "Personaje" => 1, "Builds" => 2, "Novedades" => 3, "Exploracion" => 4, _ => 5 };
                DoEvents(); DoEvents();

                // Capturas reales de pantalla completa para node src/contraste/comprobarContraste.js
                // (Parte 16 - nada en Terrakeep comprueba contraste texto/fondo hoy, es cobertura
                // 100% NUEVA) y para la revision manual de las 8 pasadas del protocolo: tamaño
                // MINIMO real (1080x700, MinWidth x MinHeight de la ventana) en los DOS idiomas -
                // el tamaño con mas probabilidad real de mostrar un problema, ver AR-LAY - mas
                // una captura a tamaño NORMAL en español como referencia de linea base (Parte 24).
                foreach (string idiomaCap in new[] { "es", "en" })
                {
                    vm.Settings.Language = idiomaCap;
                    DoEvents(); DoEvents();
                    FijarTamaño(window, 1080, 700);
                    DoEvents(); DoEvents();
                    CapturaVisual(window, (int)window.ActualWidth, (int)window.ActualHeight, $"pantalla-{pantalla}-min1080x700-{idiomaCap}");
                }
                vm.Settings.Language = "es";
                FijarTamaño(window, 1180, 860);
                DoEvents(); DoEvents();
                CapturaVisual(window, (int)window.ActualWidth, (int)window.ActualHeight, $"pantalla-{pantalla}-normal1180x860-es");

                // padre_id de la raiz sintetica: apunta a un id que NO existe en el volcado a
                // proposito (la ventana en si no tiene contenedor real que comprobar) - si se
                // dejara en null, las 6 raices (una por pantalla) compartirian ese mismo valor
                // "null" como padre_id y el verificador las trataria como HERMANAS entre si,
                // reportando que "Inicio" y "Personaje" se "solapan" (obvio: ocupan la misma
                // ventana, nunca coexisten de verdad) - el mismo tipo de falso positivo que ya
                // obligo antes a dar padre_id distinto a los TabControl de cada pantalla. Un
                // padre_id inexistente por pantalla sale como "padresNoEncontrados" (aviso de
                // extraccion, no un hallazgo de la Parte 35 - ver la cabecera de
                // verificarGeometria.js) en vez de un solape falso.
                string idRaizPantalla = $"raiz_{pantalla}";
                elementos.Add(new { id = idRaizPantalla, tipo = "raiz_pantalla", padre_id = (string?)$"(ventana_sin_padre_{pantalla})", x = 0.0, y = 0.0, ancho = window.ActualWidth, alto = window.ActualHeight, grupo = (string?)null });

                // Dos pasadas: (1) asignar un id estable a cada TabControl visible de esta
                // pantalla, (2) volcarlo con el padre_id REAL - el TabControl ANCESTRO mas
                // cercano de ESTE MISMO conjunto si existe (nido real: Personaje > Objetos >
                // Equipamiento/Inventario/Almacenes son 2 niveles de TabControl anidados de
                // verdad), o la raiz sintetica de la pantalla si no hay ninguno por encima.
                // Sin esto, dos TabControl anidados de verdad (uno fisicamente DENTRO del otro)
                // quedarian como "hermanos" bajo la misma raiz sintetica y el verificador
                // reportaria un solape que es en realidad contencion normal - mismo tipo de
                // falso positivo que ya obligo a introducir la raiz por pantalla mas arriba.
                var tcsVisibles = Descendientes<TabControl>(window).Where(t => t.IsVisible).ToList();
                var idPorTc = new Dictionary<TabControl, string>();
                foreach (var tc in tcsVisibles) idPorTc[tc] = $"tabcontrol_{contador++}";
                foreach (var tc in tcsVisibles)
                {
                    string padreId = idRaizPantalla;
                    for (DependencyObject? d = System.Windows.Media.VisualTreeHelper.GetParent(tc); d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
                    {
                        if (d is TabControl tcAncestro && idPorTc.TryGetValue(tcAncestro, out var idAnc)) { padreId = idAnc; break; }
                    }
                    VolcarTabControl(tc, idPorTc[tc], padreId, $"tabs_{pantalla}");
                }
            }

            string volcadoPath = Path.Combine(outDir, "volcado-geometria-tabs.json");
            File.WriteAllText(volcadoPath, JsonSerializer.Serialize(elementos, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"KEEPQA_SOLO-GEOMETRIA: {elementos.Count} elementos (TabControls + TabItems) volcados a {volcadoPath}");
            Console.WriteLine($"KEEPQA_SOLO: evidencia (capturas + volcado) en {outDir}");

            vm.SelectedTabIndex = 1;
            DoEvents();
        }
        catch (Exception ex) { Console.WriteLine("KEEPQA_SOLO-EXCEPTION: " + ex); }
    }
}
