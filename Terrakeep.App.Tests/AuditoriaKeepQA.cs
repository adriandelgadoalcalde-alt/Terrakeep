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
using Terrakeep.App;
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

                // FALLO-1 (reportado por el usuario, 14-sep-2026): "la barra de vida/mana y la
                // informacion de defensa/dinero/tiempo jugado estan desfasadas" en la cabecera.
                // Investigacion real: el volcado de geometria de mas abajo (para
                // verificarAlineacion.js) SOLO habia volcado TabControl/TabItem desde que existe
                // - la franja de vitales (iconos corazon/mana + sus barras + Defensa/Dinero/
                // Horas) JAMAS se habia volcado, para ninguna pantalla, en ninguna ronda anterior
                // (confirmado leyendo el propio codigo de este fichero antes de este cambio: el
                // unico volcado era `VolcarTabControl`). No es que verificarAlineacion.js la viera
                // y la descartara por "alineacion optica" (Parte 13) ni que la agrupara mal -
                // sencillamente nunca recibio ni un solo dato de esta zona. Se anade aqui, UNA vez
                // (pantalla Personaje, tamaño normal 1180x860 ya fijado arriba - la franja es
                // identica en las demas pantallas no-Exploracion, mismo WrapPanel/Grid.Column="1"
                // de MainWindow.xaml), como elementos hermanos bajo un grupo dedicado
                // "cabecera_franja_vitales" para que verificarAlineacion.js los compare de verdad
                // entre si por primera vez.
                if (pantalla == "Personaje")
                {
                    var tiraVitals = Descendientes<WrapPanel>(window)
                        .FirstOrDefault(wp => Descendientes<TextBlock>(wp).Any(t => t.Text == "♥"));
                    if (tiraVitals == null)
                    {
                        Console.WriteLine("KEEPQA_SOLO: FALLO - no se encuentra la franja de vitales (WrapPanel con icono corazon) para el volcado de cabecera");
                    }
                    else
                    {
                        int idxVitals = 0;
                        foreach (var hijo in tiraVitals.Children.OfType<FrameworkElement>())
                        {
                            if (!hijo.IsVisible || hijo.ActualWidth < 1 || hijo.ActualHeight < 1) continue;
                            Rect rHijo;
                            try { rHijo = RectCompleto(hijo, window); } catch (InvalidOperationException) { continue; }
                            string descrHijo = hijo switch
                            {
                                TextBlock tbHijo => $"icono_o_texto('{tbHijo.Text}')",
                                Grid => "barra_vida_o_mana",
                                StackPanel => "grupo_icono_valor",
                                _ => hijo.GetType().Name,
                            };
                            elementos.Add(new { id = $"cabecera_vitals_{idxVitals}", tipo = descrHijo, padre_id = (string?)$"raiz_{pantalla}", x = rHijo.X, y = rHijo.Y, ancho = rHijo.Width, alto = rHijo.Height, grupo = (string?)"cabecera_franja_vitales" });
                            idxVitals++;
                        }
                        Console.WriteLine($"KEEPQA_SOLO-GEOMETRIA: {idxVitals} elementos de la franja de vitales de cabecera volcados (grupo cabecera_franja_vitales) - primera vez que esta zona entra en el volcado");
                    }
                }

                // FALLO-2 (reportado por el usuario, 14-sep-2026): "los botones Cargar personaje
                // (.plr)..., Historial de versiones, Buscar en el personaje, Codigo de build
                // estan demasiado juntos" - espaciado (Parte 8, Spacing Intelligence), NO
                // contencion ni solape (eso ya lo mide AR-LAY/verificarGeometria.js todas las
                // rondas, y siempre da 0 aqui - los botones no se solapan, el problema es el HUECO
                // entre ellos, algo que ninguna pieza mecanica de la familia comprobaba hasta hoy).
                // Volcado real de los botones de la barra superior (WrapPanel ancestro de
                // BuildCodeButton, Grid.Column="2") para node src/espaciado/verificarEspaciado.js,
                // pieza nueva construida esta misma ronda para cerrar ese hueco.
                if (pantalla == "Personaje")
                {
                    static WrapPanel? EncontrarWrapPanelAncestro(DependencyObject? d)
                    {
                        while (d != null)
                        {
                            if (d is WrapPanel wp) return wp;
                            d = System.Windows.Media.VisualTreeHelper.GetParent(d);
                        }
                        return null;
                    }
                    var barraSuperior = window.FindName("BuildCodeButton") is Button bcbToolbar ? EncontrarWrapPanelAncestro(bcbToolbar) : null;
                    if (barraSuperior == null)
                    {
                        Console.WriteLine("KEEPQA_SOLO: FALLO - no se encuentra la barra superior de botones (ancestro WrapPanel de BuildCodeButton) para el volcado de espaciado");
                    }
                    else
                    {
                        int idxBarra = 0;
                        foreach (var hijo in barraSuperior.Children.OfType<FrameworkElement>())
                        {
                            if (!hijo.IsVisible || hijo.ActualWidth < 1 || hijo.ActualHeight < 1) continue;
                            Rect rHijo;
                            try { rHijo = RectCompleto(hijo, window); } catch (InvalidOperationException) { continue; }
                            string tipoHijo = hijo switch
                            {
                                Button btnHijo => $"boton('{btnHijo.Content}')",
                                Border => "separador",
                                _ => hijo.GetType().Name,
                            };
                            elementos.Add(new { id = $"toolbar_{idxBarra}", tipo = tipoHijo, padre_id = (string?)$"raiz_{pantalla}", x = rHijo.X, y = rHijo.Y, ancho = rHijo.Width, alto = rHijo.Height, grupo = (string?)"cabecera_barra_botones" });
                            idxBarra++;
                        }
                        Console.WriteLine($"KEEPQA_SOLO-GEOMETRIA: {idxBarra} elementos de la barra superior de botones volcados (grupo cabecera_barra_botones) - primera vez que esta zona entra en el volcado");
                    }
                }

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

    // FALLO3_SOLO=1 (14-sep-2026): investigacion del Fallo 3 reportado por el usuario - "en
    // Exploracion, con la ventana en su tamaño por defecto al abrir la app, la caja de resultados
    // de la barra lateral es muy pequeña y, al desplegar resultados de una categoria (ej. todo el
    // mineral de estaño por coordenadas), se come casi todo el espacio disponible, mientras queda
    // mucho espacio sin aprovechar en el resto de la pantalla".
    //
    // Primera hipotesis, CONFIRMADA PARCIAL: el `ListBox` de resultados llevaba un `MaxHeight="240"`
    // fijo sin cita de medicion real (a diferencia de casi cualquier otro numero del fichero) -
    // quitado (no era el cuello de botella real a tamaño por defecto, pero SI lo habria sido en
    // ventanas grandes/maximizada, donde la fila "*" que lo aloja habria calculado mas de 240px).
    // El cuello de botella real, encontrado con esta misma funcion: a 1180x860 el bloque de
    // resultados entero (checkboxes+resumen+botones+lista) solo recibia 205px, de los que la LISTA
    // que de verdad se desplaza se quedaba con 70px REALES - 1-2 filas de 1000 resultados reales.
    // Root cause real (no el `MaxHeight`): `ExplorationSidebarPanel` (el DockPanel que envuelve
    // toda la columna) fuerza un suelo de scroll fijo (`MinHeight`) sin importar el visor real del
    // `ScrollViewer` que lo contiene - ese suelo, calibrado en una ronda anterior (6/14-sep-2026)
    // contra CONTENIDO DISTINTO (Expanders de "Editar mundo"/"Bestiario", nunca el bloque de
    // resultados), se quedaba corto para este escenario. Ver el arreglo real y las cifras
    // medidas ANTES/DESPUES en el comentario de MainWindow.xaml junto a `MinHeight="800"` (subido
    // desde 652) y junto al `Grid` de dos filas "categoria/resultados" (documenta tambien un primer
    // intento descartado - subir el peso 1.2 de esa fila - que una re-verificacion real de la
    // garantia de AR-EX1 demostro que ROMPIA "Objetos" a tamaño por defecto).
    private static void EjecutarFallo3Exploracion(Window window, MainViewModel vm)
    {
        try
        {
            string worldPath = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld";
            if (!File.Exists(worldPath))
            {
                Console.WriteLine("FALLO3_SOLO: FALLO - no se encuentra roca_negra.wld, no se puede investigar con datos reales");
                return;
            }

            vm.SelectedTabIndex = 4; // Exploracion
            DoEvents();
            var carga = vm.Exploration.LoadFromPathAsync(worldPath);
            while (!carga.IsCompleted) DoEvents();
            DoEvents();

            var fitMethod = typeof(MainWindow).GetMethod("OnFitToWindowClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fitMethod?.Invoke(window, [window, new RoutedEventArgs()]);
            DoEvents();

            double anchoOriginal = window.Width, altoOriginal = window.Height;
            double sidebarOriginal = vm.Settings.ExplorationSidebarWidth;

            var contenidoCat = window.FindName("ExplorationCategoryContent") as FrameworkElement;
            var bloqueRes = window.FindName("ExplorationResultsBlock") as FrameworkElement;
            var svLat = window.FindName("ExplorationSidebarScroll") as ScrollViewer;
            if (contenidoCat == null || bloqueRes == null || svLat == null)
            {
                Console.WriteLine("FALLO3_SOLO: FALLO - no se encuentra ExplorationCategoryContent/ExplorationResultsBlock/ExplorationSidebarScroll en el arbol visual");
                return;
            }

            void MedirEn(string etiqueta, double w, double h, double anchoBarraLateral)
            {
                FijarTamaño(window, w, h);
                vm.Settings.ExplorationSidebarWidth = anchoBarraLateral;
                DoEvents(); DoEvents();

                vm.Exploration.SelectedCategory = WorldSearchCategory.Ores;
                DoEvents(); DoEvents();
                var filaEstaño = vm.Exploration.OreMetals.OrderByDescending(r => r.Count).FirstOrDefault();
                if (filaEstaño == null)
                {
                    Console.WriteLine($"FALLO3_SOLO[{etiqueta}]: FALLO - OreMetals vacio, no se puede investigar con datos reales");
                    return;
                }
                filaEstaño.IsChecked = true;
                WaitForDispatcher(1500);
                DoEvents(); DoEvents();

                // Captura real de evidencia visual (antes/despues del arreglo, misma escena: mundo
                // real, categoria Minerales, resultados de Piedra Infernal desplegados) - para no
                // depender solo de numeros de consola. La columna scrollea (ExtentHeight > Viewport
                // a estos tamaños, ver el comentario real de MinHeight en MainWindow.xaml) y el
                // bloque de resultados es la fila DE ABAJO del Grid de dos filas - hace falta bajar
                // el scroll antes de capturar o la captura solo enseñaria la lista de tipos de
                // mineral de arriba, nunca el bloque que este fallo investiga.
                svLat.ScrollToBottom();
                DoEvents(); DoEvents();
                string outDirFallo3 = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
                Directory.CreateDirectory(outDirFallo3);
                var rtbFallo3 = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtbFallo3.Render(window);
                var encFallo3 = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encFallo3.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbFallo3));
                string nombreCaptura3 = Path.Combine(outDirFallo3, $"fallo3-exploracion-{etiqueta}.png");
                using (var fsFallo3 = File.Create(nombreCaptura3)) encFallo3.Save(fsFallo3);
                Console.WriteLine($"FALLO3_SOLO[{etiqueta}]: captura real -> {nombreCaptura3}");

                // Re-verificacion directa del regla real que AR-EX1 vigila (6-sep-2026): con estos
                // MISMOS resultados abiertos (el escenario que este fallo describe), ¿las otras 3
                // categorias reales de la barra lateral siguen mostrando al menos UNA fila entera
                // de su propio contenido? El arreglo de este fallo (peso 1.2->1.8) le quita altura
                // real a la categoria (178->140px medido) - esto confirma que sigue sin romper la
                // garantia que AR-EX1 dejo puesta, no solo lo supone por estar por encima del
                // MinHeight=120.
                foreach (var (cat, modoCofres, nombreCat) in new (WorldSearchCategory, int, string)[]
                         { (WorldSearchCategory.Chests, 0, "Cofres/Por tipo"), (WorldSearchCategory.Chests, 2, "Cofres/Cofre a cofre"), (WorldSearchCategory.Objects, 0, "Objetos") })
                {
                    vm.Exploration.SelectedCategory = cat;
                    if (cat == WorldSearchCategory.Chests) vm.Exploration.ChestViewMode = modoCofres;
                    DoEvents(); DoEvents();
                    ItemsControl? lista = cat == WorldSearchCategory.Chests && modoCofres == 2
                        ? window.FindName("ChestByChestList") as ItemsControl
                        : Descendientes<ListBox>(window).FirstOrDefault(lb => lb.IsVisible && ReferenceEquals(lb.ItemsSource, vm.Exploration.Inventory));
                    int totalFilas = lista?.Items.Count ?? 0;
                    int visiblesEnteras = 0;
                    if (lista != null)
                        foreach (var item in lista.Items.Cast<object>().Take(30))
                            if (lista.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement feFila && VisibleEntero(feFila, window))
                                visiblesEnteras++;
                    double altoPrimeraFila = -1;
                    if (lista != null && lista.Items.Count > 0 && lista.ItemContainerGenerator.ContainerFromIndex(0) is FrameworkElement feAlto)
                        altoPrimeraFila = feAlto.ActualHeight;
                    Console.WriteLine($"FALLO3_SOLO[{etiqueta}] re-verificacion AR-EX1, {nombreCat}: {totalFilas} filas reales, visibles enteras={visiblesEnteras} (esperado >=1 si totalFilas>0), alto real de 1 fila={altoPrimeraFila:0}px");
                    if (totalFilas > 0 && visiblesEnteras == 0)
                        Console.WriteLine($"FALLO: FALLO3_SOLO - regresion real de AR-EX1 en {nombreCat} a {etiqueta} tras el arreglo del Fallo 3");
                }
                vm.Exploration.SelectedCategory = WorldSearchCategory.Ores;
                DoEvents(); DoEvents();

                var listaResultados = Descendientes<ListBox>(window)
                    .FirstOrDefault(lb => lb.IsVisible && ReferenceEquals(lb.ItemsSource, vm.Exploration.WorldSearchResults));
                double alturaListaReal = listaResultados?.ActualHeight ?? -1;
                double alturaFilaGrid = -1;
                if (listaResultados != null && System.Windows.Media.VisualTreeHelper.GetParent(listaResultados) is Grid gridPadre)
                {
                    int fila = System.Windows.Controls.Grid.GetRow(listaResultados);
                    if (fila < gridPadre.RowDefinitions.Count) alturaFilaGrid = gridPadre.RowDefinitions[fila].ActualHeight;
                }
                int nResultados = vm.Exploration.WorldSearchResults.Count;
                double anchoSidebarReal = svLat.ActualWidth;
                double anchoMapaReal = window.ActualWidth - anchoSidebarReal;
                double alturaCategoria = contenidoCat.ActualHeight;
                double alturaBloqueResultados = bloqueRes.ActualHeight;
                var panelSidebar = window.FindName("ExplorationSidebarPanel") as FrameworkElement;

                Console.WriteLine($"FALLO3_SOLO[{etiqueta}] ventana={window.ActualWidth:0}x{window.ActualHeight:0} sidebarPedido={anchoBarraLateral:0} -> " +
                                  $"sidebar REAL={anchoSidebarReal:0}px ({anchoSidebarReal / window.ActualWidth * 100:0.#}% del ancho), mapa REAL={anchoMapaReal:0}px ({anchoMapaReal / window.ActualWidth * 100:0.#}% del ancho); " +
                                  $"{nResultados} resultados reales de '{filaEstaño.Name}'; bloque de resultados={alturaBloqueResultados:0}px, contenido de categoria={alturaCategoria:0}px; " +
                                  $"fila del Grid para el ListBox={alturaFilaGrid:0}px, ListBox.ActualHeight={alturaListaReal:0}px (sin MaxHeight, quitado por el Fallo 3) -> " +
                                  $"HUECO SIN APROVECHAR bajo el ListBox = {(alturaFilaGrid >= 0 ? alturaFilaGrid - alturaListaReal : -1):0}px | " +
                                  $"ScrollViewer.ViewportHeight={svLat.ViewportHeight:0}px ExtentHeight={svLat.ExtentHeight:0}px | ExplorationSidebarPanel.ActualHeight={panelSidebar?.ActualHeight:0}px (MinHeight=800)");

                filaEstaño.IsChecked = false;
                WaitForDispatcher(500);
                DoEvents();
            }

            // Tamaño por defecto real de la app (Width/Height de MainWindow.xaml) con el ancho de
            // sidebar por defecto real (SettingsService.ExplorationSidebarWidth = 320).
            MedirEn("normal1180x860-sidebar320(defecto)", 1180, 860, 320);
            // Mismo tamaño de ventana, sidebar en su MAXIMO ya permitido hoy (520, arrastre manual
            // del GridSplitter) - para saber si ensanchar la columna por si solo ya resuelve algo
            // o si el cuello de botella real es el MaxHeight=240 fijo del ListBox.
            MedirEn("normal1180x860-sidebar520(maximo-ya-permitido)", 1180, 860, 520);
            // Tamaño minimo real de la ventana (MinWidth x MinHeight), sidebar en su minimo real.
            MedirEn("minimo1080x700-sidebar260(minimo)", 1080, 700, 260);

            FijarTamaño(window, anchoOriginal, altoOriginal);
            vm.Settings.ExplorationSidebarWidth = sidebarOriginal;
            vm.Exploration.SelectedCategory = WorldSearchCategory.All;
            DoEvents();
        }
        catch (Exception ex) { Console.WriteLine("FALLO3_SOLO-EXCEPTION: " + ex); }
    }
}
