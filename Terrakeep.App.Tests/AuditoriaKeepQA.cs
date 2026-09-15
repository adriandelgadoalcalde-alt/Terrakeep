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

            // orden_z/capa (14-sep-2026, encargo del coordinador: "construye la pieza real de
            // capas/z-order para WPF tambien... con el mismo rigor que se hizo hoy para DST") -
            // ver `OrdenZ`/`VisualChildIndex` en AuditoriaMaquetacion.cs (extractor compartido,
            // ZIndex real + orden de hijos como desempate, el algoritmo real de dibujado de WPF)
            // y `RANGO_CAPA` en KeepQA/src/capas/verificarCapas.js para el vocabulario cerrado de
            // `capa`. Todo elemento que este volcado ya sabia describir gana estos dos campos
            // nuevos para que `verificarCapas.js` (Parte 11) pueda comprobar el ORDEN DE DIBUJADO
            // entre hermanos que se solapan, algo que `verificarGeometria.js` nunca pudo juzgar
            // (sabe que dos cajas se solapan, no sabe cual deberia estar encima).
            void VolcarTabControl(TabControl tc, string idTc, string padreId, string grupoPrefijo)
            {
                Rect rTc;
                try { rTc = RectCompleto(tc, window); } catch (InvalidOperationException) { return; }
                elementos.Add(new { id = idTc, tipo = "panel_pestanas", padre_id = (string?)padreId, x = rTc.X, y = rTc.Y, ancho = rTc.Width, alto = rTc.Height, grupo = (string?)null, orden_z = OrdenZ(tc), capa = "panel" });
                for (int i = 0; i < tc.Items.Count; i++)
                {
                    if (tc.ItemContainerGenerator.ContainerFromIndex(i) is not TabItem ti || !ti.IsVisible) continue;
                    Rect rTi;
                    try { rTi = RectCompleto(ti, window); } catch (InvalidOperationException) { continue; }
                    if (rTi.Width < 1 || rTi.Height < 1) continue;
                    elementos.Add(new { id = $"{idTc}_item{i}", tipo = "pestana", padre_id = (string?)idTc, x = rTi.X, y = rTi.Y, ancho = rTi.Width, alto = rTi.Height, grupo = (string?)$"{grupoPrefijo}_{tc.Items.Count}", orden_z = OrdenZ(ti), capa = "navegacion" });
                }
            }

            // Cabecera de vitales (FALLO-1) y barra de botones superior (FALLO-2): IGUALES en
            // TODAS las sub-pestañas de Personaje (mismo WrapPanel real de MainWindow.xaml,
            // Grid.Column="1"/"2" - no depende de que sub-pestaña este activa) - se vuelcan UNA
            // vez por TAMAÑO (min/normal, español, suficiente para verificarAlineacion.js/
            // verificarEspaciado.js/verificarCapas.js) en vez de una vez por cada una de las 9
            // combinaciones de Personaje x 2 idiomas: multiplicar x18 el mismo dato no aporta
            // nada nuevo, solo hincha el volcado.
            void VolcarCabecera(string idRaizCombo, string etiquetaTam)
            {
                var tiraVitals = Descendientes<WrapPanel>(window)
                    .FirstOrDefault(wp => Descendientes<TextBlock>(wp).Any(t => t.Text == "♥"));
                if (tiraVitals == null)
                {
                    Console.WriteLine($"KEEPQA_SOLO: FALLO - no se encuentra la franja de vitales (WrapPanel con icono corazon) a {etiquetaTam}");
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
                        elementos.Add(new { id = $"cabecera_vitals_{etiquetaTam}_{idxVitals}", tipo = descrHijo, padre_id = (string?)idRaizCombo, x = rHijo.X, y = rHijo.Y, ancho = rHijo.Width, alto = rHijo.Height, grupo = (string?)$"cabecera_franja_vitales_{etiquetaTam}", orden_z = OrdenZ(hijo), capa = "contenido" });
                        idxVitals++;
                    }
                    Console.WriteLine($"KEEPQA_SOLO-GEOMETRIA[{etiquetaTam}]: {idxVitals} elementos de la franja de vitales de cabecera volcados (grupo cabecera_franja_vitales_{etiquetaTam})");
                }

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
                    Console.WriteLine($"KEEPQA_SOLO: FALLO - no se encuentra la barra superior de botones (ancestro WrapPanel de BuildCodeButton) a {etiquetaTam}");
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
                        string capaHijo = hijo is Button ? "controles" : "decoracion";
                        elementos.Add(new { id = $"toolbar_{etiquetaTam}_{idxBarra}", tipo = tipoHijo, padre_id = (string?)idRaizCombo, x = rHijo.X, y = rHijo.Y, ancho = rHijo.Width, alto = rHijo.Height, grupo = (string?)$"cabecera_barra_botones_{etiquetaTam}", orden_z = OrdenZ(hijo), capa = capaHijo });
                        idxBarra++;
                    }
                    Console.WriteLine($"KEEPQA_SOLO-GEOMETRIA[{etiquetaTam}]: {idxBarra} elementos de la barra superior de botones volcados (grupo cabecera_barra_botones_{etiquetaTam})");
                }
            }

            // ---- Barrido REAL de las 16 pantallas/sub-pestañas (Inicio, las 9 combinaciones
            // reales de Personaje - Equipamiento/Inventario/Almacenes/Buffs/Investigacion/
            // Apariencia/SpawnPoints/Desbloqueos/Version -, las 2 hojas de Builds, las 2 de
            // Novedades, Exploracion, AcercaDe+Ajustes - MISMA lista que ya audita AR-LAY cada
            // `dotnet run`, `ConstruirPantallasMaquetacion`, para no mantener una segunda copia
            // que se desincronice) a los 2 tamaños reales (minimo 1080x700 = MinWidth x
            // MinHeight, normal 1180x860 = tamaño de arranque) en los DOS idiomas: captura de
            // pantalla completa (para comprobarContraste.js/comprobar-texto.js y la revision
            // manual de las 8 pasadas) + volcado de geometria (TabControls/TabItems, con
            // orden_z/capa) para verificarGeometria.js/verificarAlineacion.js/
            // verificarEspaciado.js/verificarCapas.js. Amplia el barrido anterior de esta misma
            // ronda (solo 6 pantallas raiz, sin las 9 sub-pestañas de Personaje, sin normal-EN) -
            // pedido explicito del coordinador: "TODAS las pantallas principales... Personaje
            // completo (todas sus sub-pestañas)... en tamaño minimo y normal, español e ingles".
            var pantallas = ConstruirPantallasMaquetacion(window, vm);
            (double w, double h, string etiqueta)[] tamañosCaptura = [(1080, 700, "min1080x700"), (1180, 860, "normal1180x860")];
            bool cabeceraVolcadaMin = false, cabeceraVolcadaNormal = false;
            foreach (string idiomaCap in new[] { "es", "en" })
            {
                vm.Settings.Language = idiomaCap;
                DoEvents(); DoEvents();
                foreach (var (w, h, etiquetaTam) in tamañosCaptura)
                {
                    FijarTamaño(window, w, h);
                    DoEvents(); DoEvents();
                    foreach (var (nombrePantalla, ir) in pantallas)
                    {
                        ir();
                        DoEvents(); DoEvents();
                        string nombreArchivo = nombrePantalla.Replace('/', '-');
                        CapturaVisual(window, (int)window.ActualWidth, (int)window.ActualHeight, $"pantalla-{nombreArchivo}-{etiquetaTam}-{idiomaCap}");

                        // padre_id de la raiz sintetica: apunta a un id que NO existe en el
                        // volcado a proposito (la ventana en si no tiene contenedor real que
                        // comprobar) - si se dejara en null, las 64 raices (16 pantallas x 2
                        // tamaños x 2 idiomas) compartirian ese mismo valor "null" como padre_id
                        // y el verificador las trataria como HERMANAS entre si, reportando que
                        // "Inicio" y "Personaje/Buffs" se "solapan" (obvio: ocupan la misma
                        // ventana en momentos distintos, nunca coexisten de verdad). Un padre_id
                        // inexistente por combinacion sale como `padresNoEncontrados` (aviso de
                        // extraccion, no un hallazgo de la Parte 35).
                        string idRaizCombo = $"raiz_{nombrePantalla}_{etiquetaTam}_{idiomaCap}".Replace('/', '_');
                        elementos.Add(new { id = idRaizCombo, tipo = "raiz_pantalla", padre_id = (string?)$"(ventana_sin_padre_{idRaizCombo})", x = 0.0, y = 0.0, ancho = window.ActualWidth, alto = window.ActualHeight, grupo = (string?)null, orden_z = 0.0, capa = "fondo" });

                        // Dos pasadas: (1) asignar un id estable a cada TabControl visible de
                        // esta combinacion, (2) volcarlo con el padre_id REAL - el TabControl
                        // ANCESTRO mas cercano de ESTE MISMO conjunto si existe (nido real:
                        // Personaje > Objetos > Equipamiento/Inventario/Almacenes son 2 niveles de
                        // TabControl anidados de verdad), o la raiz sintetica si no hay ninguno
                        // por encima. Sin esto, dos TabControl anidados de verdad quedarian como
                        // "hermanos" bajo la misma raiz sintetica y el verificador reportaria un
                        // solape que es en realidad contencion normal.
                        var tcsVisibles = Descendientes<TabControl>(window).Where(t => t.IsVisible).ToList();
                        var idPorTc = new Dictionary<TabControl, string>();
                        foreach (var tc in tcsVisibles) idPorTc[tc] = $"tabcontrol_{contador++}";
                        foreach (var tc in tcsVisibles)
                        {
                            string padreId = idRaizCombo;
                            for (DependencyObject? d = System.Windows.Media.VisualTreeHelper.GetParent(tc); d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
                            {
                                if (d is TabControl tcAncestro && idPorTc.TryGetValue(tcAncestro, out var idAnc)) { padreId = idAnc; break; }
                            }
                            VolcarTabControl(tc, idPorTc[tc], padreId, $"tabs_{nombreArchivo}_{etiquetaTam}_{idiomaCap}");
                        }

                        if (nombrePantalla.StartsWith("Personaje/", StringComparison.Ordinal) && idiomaCap == "es")
                        {
                            if (etiquetaTam == "min1080x700" && !cabeceraVolcadaMin) { VolcarCabecera(idRaizCombo, etiquetaTam); cabeceraVolcadaMin = true; }
                            if (etiquetaTam == "normal1180x860" && !cabeceraVolcadaNormal) { VolcarCabecera(idRaizCombo, etiquetaTam); cabeceraVolcadaNormal = true; }
                        }
                    }
                }
            }
            vm.Settings.Language = "es";

            string volcadoPath = Path.Combine(outDir, "volcado-geometria-tabs.json");
            File.WriteAllText(volcadoPath, JsonSerializer.Serialize(elementos, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"KEEPQA_SOLO-GEOMETRIA: {elementos.Count} elementos (TabControls + TabItems + cabecera, con orden_z/capa) volcados a {volcadoPath} ({pantallas.Count} pantallas x {tamañosCaptura.Length} tamaños x 2 idiomas = {pantallas.Count * tamañosCaptura.Length * 2} combinaciones, {pantallas.Count * tamañosCaptura.Length * 2} capturas PNG)");
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

                // Cierre del punto 1 del checklist de cierre (14-sep-2026): el propio FALLO-3 dejo
                // anotado, sin arreglar, que a la ventana MINIMA real (1080x700) el viewport no
                // llega a cubrir la cabecera fija de la columna antes de necesitar scroll. El
                // arreglo real es el indicador "ExplorationScrollHint" (ver MainWindow.xaml/
                // MainWindow.xaml.cs) - se verifica aqui con el mismo escenario ya montado
                // (resultados de Piedra Infernal desplegados, el caso mas exigente), ANTES de que
                // el resto de esta funcion baje el scroll a proposito para su propia captura.
                svLat.ScrollToTop();
                DoEvents(); DoEvents();
                var hintScroll = window.FindName("ExplorationScrollHint") as FrameworkElement;
                bool quedaScrollReal = svLat.ExtentHeight - svLat.ViewportHeight > 2;
                bool hintVisibleArriba = hintScroll?.Visibility == Visibility.Visible;
                Console.WriteLine($"FALLO3_SOLO[{etiqueta}] indicador de scroll (arriba del todo): ExtentHeight={svLat.ExtentHeight:0}px ViewportHeight={svLat.ViewportHeight:0}px quedaScrollReal={quedaScrollReal} -> ExplorationScrollHint.Visibility={hintScroll?.Visibility}");
                if (quedaScrollReal && !hintVisibleArriba)
                    Console.WriteLine($"FALLO: FALLO3_SOLO - {etiqueta} tiene {svLat.ExtentHeight - svLat.ViewportHeight:0}px de scroll real pendiente y el indicador NO esta visible");
                if (!quedaScrollReal && hintVisibleArriba)
                    Console.WriteLine($"FALLO: FALLO3_SOLO - {etiqueta} no tiene scroll real pendiente pero el indicador SI esta visible (falso positivo)");
                if (quedaScrollReal)
                {
                    string outDirHint = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
                    Directory.CreateDirectory(outDirHint);
                    var rtbHint = new System.Windows.Media.Imaging.RenderTargetBitmap(
                        (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbHint.Render(window);
                    var encHint = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encHint.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbHint));
                    string nombreCapturaHint = Path.Combine(outDirHint, $"scrollhint-{etiqueta}-arriba.png");
                    using (var fsHint = File.Create(nombreCapturaHint)) encHint.Save(fsHint);
                    Console.WriteLine($"FALLO3_SOLO[{etiqueta}]: captura real del indicador -> {nombreCapturaHint}");
                }

                // Captura real de evidencia visual (antes/despues del arreglo, misma escena: mundo
                // real, categoria Minerales, resultados de Piedra Infernal desplegados) - para no
                // depender solo de numeros de consola. La columna scrollea (ExtentHeight > Viewport
                // a estos tamaños, ver el comentario real de MinHeight en MainWindow.xaml) y el
                // bloque de resultados es la fila DE ABAJO del Grid de dos filas - hace falta bajar
                // el scroll antes de capturar o la captura solo enseñaria la lista de tipos de
                // mineral de arriba, nunca el bloque que este fallo investiga.
                svLat.ScrollToBottom();
                DoEvents(); DoEvents();
                bool hintVisibleAbajo = hintScroll?.Visibility == Visibility.Visible;
                Console.WriteLine($"FALLO3_SOLO[{etiqueta}] indicador de scroll (al fondo, VerticalOffset={svLat.VerticalOffset:0}px de ScrollableHeight={svLat.ScrollableHeight:0}px): ExplorationScrollHint.Visibility={hintScroll?.Visibility}");
                if (hintVisibleAbajo)
                    Console.WriteLine($"FALLO: FALLO3_SOLO - {etiqueta} ya esta al fondo del scroll y el indicador sigue visible (deberia ocultarse)");
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

    // CAPAS_SOLO=1 (14-sep-2026, encargo REFORZADO del coordinador: "no te quedes en investigar
    // si hace falta - construye la pieza real de capas/z-order para WPF tambien, con el mismo
    // rigor que se hizo hoy para DST"). El extractor real vive en AuditoriaMaquetacion.cs
    // (`OrdenZ`/`VisualChildIndex`, reutilizado tambien por `VolcarTabControl`/`VolcarCabecera`
    // de mas arriba en este mismo fichero) - a diferencia del motor de widgets de DST/Lua de
    // StarvekeepMod (sin ningun GetZOrder()/GetDrawIndex(), instrumentado a mano en
    // starvekeepscreen.lua), WPF SI expone un orden de dibujado real y consultable sin
    // instrumentar nada: `Panel.GetZIndex()` (explicito) + el indice de posicion real dentro de
    // `VisualTreeHelper.GetChild(padre, i)` como desempate - el algoritmo real que el propio
    // compositor de WPF usa para decidir que pinta encima de que.
    //
    // VALIDACION REAL, no solo "deberia funcionar": revisados `bitacora.md` y `PATRONES.md` de
    // Terrakeep enteros (grep de "ZIndex"/"z-index"/"detras del"/"encima de") ANTES de escribir
    // esto - a diferencia de StarvekeepMod (que SI tuvo un bug real de capas, ver KeepQA/
    // PATRONES.md seccion F, la barra de pestañas pintandose detras del marco decorativo), no
    // aparece NINGUN caso real conocido de orden de capas incorrecto en Terrakeep hasta hoy. Esto
    // es distinto de "la pieza no aplica" (SI aplica: WPF tiene orden de dibujado real y este
    // extractor lo lee de verdad) - simplemente no hay ningun bug real de esta clase que
    // encontrar todavia en este proyecto. Para demostrar con datos REALES de WPF (no solo el
    // volcado sintetico de StarvekeepMod que ya valido PATRONES.md, ni un JSON escrito a mano)
    // que el extractor produce numeros que `verificarCapas.js` sabe interpretar, este modo
    // construye un caso SINTETICO A PROPOSITO: una ventana WPF real, minima, con un `Grid` real
    // (parte autentica del arbol visual, medido con `TransformToAncestor` como todo lo demas de
    // este fichero) que contiene dos hijos que se solapan de verdad - una "decoracion" (`Border`)
    // y una "navegacion" (`Button`) - con el `ZIndex` puesto DELIBERADAMENTE al reves de lo que
    // `RANGO_CAPA` exige (decoracion=5, navegacion=1, cuando navegacion deberia ganar por tener
    // rango funcional mayor) - reproduce exactamente el bug LITERAL de la Parte 11 de
    // ESPEC-VISUAL-QA.md ("navigation tabs appear visually underneath the panel background that
    // should be visually behind them"), el mismo que motivo la creacion de `verificarCapas.js`.
    private static void EjecutarCapasSinteticoSolo(Window window, MainViewModel vm)
    {
        try
        {
            string outDir = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
            Directory.CreateDirectory(outDir);

            var grid = new Grid { Width = 300, Height = 200, Background = System.Windows.Media.Brushes.DimGray };

            // "decoracion": ocupa casi todo el panel de prueba. Por `RANGO_CAPA` (verificarCapas.js)
            // deberia ir DETRAS de la navegacion (rango 1 contra rango 5) - se le pone a proposito
            // un ZIndex MAYOR (5) para simular el bug real que esta pieza tiene que cazar.
            var decoracion = new Border
            {
                Width = 260, Height = 160, Background = System.Windows.Media.Brushes.SaddleBrown,
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10, 10, 0, 0),
            };
            Panel.SetZIndex(decoracion, 5);

            // "navegacion": una pestaña/boton real que SOLAPA con la decoracion - por su capa
            // (rango 5) deberia dibujarse por delante, pero su ZIndex (1) es MENOR que el de la
            // decoracion: el bug deliberado que este caso sintetico existe para demostrar.
            var navegacion = new Button
            {
                Content = "Pestaña", Width = 120, Height = 40,
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(30, 20, 0, 0),
            };
            Panel.SetZIndex(navegacion, 1);

            grid.Children.Add(decoracion);
            grid.Children.Add(navegacion);

            // Ventana real aparte (no se toca el arbol de MainWindow): posicionada fuera de
            // pantalla (Left/Top muy negativos) para no interferir visualmente con la ventana
            // principal que sigue abierta en esta misma sesion del arnes, pero SIGUE necesitando
            // un `Show()` real - sin PresentationSource, `ActualWidth`/`TransformToAncestor` no
            // reflejan un layout real, solo 0 o valores sin medir.
            var ventanaPrueba = new Window
            {
                Content = grid, Width = 340, Height = 260,
                WindowStartupLocation = WindowStartupLocation.Manual, Left = -5000, Top = -5000,
                ShowInTaskbar = false, Title = "KeepQA-CAPAS-prueba-sintetica",
            };
            ventanaPrueba.Show();
            DoEvents(); DoEvents(); DoEvents();

            Rect rGrid = new(0, 0, grid.ActualWidth, grid.ActualHeight);
            Rect rDecoracion = decoracion.TransformToAncestor(grid).TransformBounds(new Rect(0, 0, decoracion.ActualWidth, decoracion.ActualHeight));
            Rect rNavegacion = navegacion.TransformToAncestor(grid).TransformBounds(new Rect(0, 0, navegacion.ActualWidth, navegacion.ActualHeight));

            var elementos = new List<object>
            {
                new { id = "grid_prueba", tipo = "panel_prueba", padre_id = (string?)null, x = rGrid.X, y = rGrid.Y, ancho = rGrid.Width, alto = rGrid.Height, grupo = (string?)null, orden_z = 0.0, capa = "fondo" },
                new { id = "decoracion_prueba", tipo = "decoracion(Border, ZIndex=5, MAL puesto a proposito)", padre_id = (string?)"grid_prueba", x = rDecoracion.X, y = rDecoracion.Y, ancho = rDecoracion.Width, alto = rDecoracion.Height, grupo = (string?)null, orden_z = OrdenZ(decoracion), capa = "decoracion" },
                new { id = "navegacion_prueba", tipo = "navegacion(Button, ZIndex=1)", padre_id = (string?)"grid_prueba", x = rNavegacion.X, y = rNavegacion.Y, ancho = rNavegacion.Width, alto = rNavegacion.Height, grupo = (string?)null, orden_z = OrdenZ(navegacion), capa = "navegacion" },
            };

            string volcadoPath = Path.Combine(outDir, "volcado-capas-sintetico-bug-zorder.json");
            File.WriteAllText(volcadoPath, JsonSerializer.Serialize(elementos, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"CAPAS_SOLO: caso sintetico (WPF real, ventana propia) volcado a {volcadoPath}");
            Console.WriteLine($"CAPAS_SOLO: decoracion (capa 'decoracion', rango 1) orden_z real={OrdenZ(decoracion):0} | navegacion (capa 'navegacion', rango 5) orden_z real={OrdenZ(navegacion):0} - navegacion DEBERIA tener mayor orden_z por su capa y NO lo tiene: bug deliberado, verificarCapas.js deberia marcarlo");
            Console.WriteLine($"CAPAS_SOLO: cajas reales - decoracion={rDecoracion}, navegacion={rNavegacion}, interseccion real={Rect.Intersect(rDecoracion, rNavegacion)}");

            var rtbPrueba = new System.Windows.Media.Imaging.RenderTargetBitmap(
                Math.Max(1, (int)grid.ActualWidth), Math.Max(1, (int)grid.ActualHeight), 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            rtbPrueba.Render(grid);
            var encPrueba = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encPrueba.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbPrueba));
            using (var fs = File.Create(Path.Combine(outDir, "capas-sintetico-bug-zorder.png"))) encPrueba.Save(fs);
            Console.WriteLine($"CAPAS_SOLO: captura real -> {Path.Combine(outDir, "capas-sintetico-bug-zorder.png")}");

            ventanaPrueba.Close();
            DoEvents();
        }
        catch (Exception ex) { Console.WriteLine("CAPAS_SOLO-EXCEPTION: " + ex); }
    }

    // KEEPQA_FRESCO_SOLO=1 (15-sep-2026): barrido del repaso integral nocturno pedido por el
    // coordinador - el arsenal ya cubrio 16 pantallas x 2 tamaños (minimo 1080x700, umbral
    // 1180x860) x 2 idiomas = 64 combinaciones con 0 hallazgos adversariales (ver
    // KEEPQA_SOLO/EjecutarKeepQaAdversarialYGeometria de arriba, entrada de bitacora.md
    // "14-sep-2026 (ronda siguiente)") - repetir esas dos resoluciones seria duplicar trabajo ya
    // cerrado. Este modo aporta la unica cobertura REALMENTE nueva pedida: una TERCERA resolucion
    // de la categoria "comun" del catalogo compartido (KeepQA/src/resoluciones/catalogo.json,
    // id "fullhd" 1920x1080 - "el tamaño de pantalla completa mas comun hoy", nunca ejercitado
    // mecanicamente contra Terrakeep hasta ahora), sobre 4 de las 6 pantallas raiz (Inicio,
    // Personaje/Inventario, Exploracion, AcercaDe+Ajustes - las dos primeras coinciden a proposito
    // con los 3 nombres que ya tiene baseline real en KeepQA/baselines/terrakeep/, Exploracion por
    // ser la pantalla con mas historial real de bugs de espacio/scroll de todo el proyecto), en
    // los 2 idiomas. Fichero de volcado PROPIO (nunca sobrescribe volcado-geometria-tabs.json, la
    // evidencia ya cerrada del 14-sep) y capturas con nombre compatible con
    // gestorBaseline.js (mismo patron "pantalla-<Nombre>-<tamaño>-<idioma>" que ya usa
    // EjecutarKeepQaAdversarialYGeometria) para poder comparar contra el baseline real donde
    // exista, ademas de las capturas en fullhd para revision manual/OCR/contraste.
    private static void EjecutarKeepQaFrescoTerceraResolucion(Window window, MainViewModel vm)
    {
        try
        {
            string outDir = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
            Directory.CreateDirectory(outDir);

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

            var todasPantallas = ConstruirPantallasMaquetacion(window, vm);
            var nombresElegidos = new[] { "Inicio", "Personaje/Inventario", "Exploracion", "AcercaDe+Ajustes" };
            var pantallas = todasPantallas.Where(p => nombresElegidos.Contains(p.nombre)).ToList();
            if (pantallas.Count != nombresElegidos.Length)
                Console.WriteLine($"KEEPQA_FRESCO_SOLO: AVISO - se esperaban {nombresElegidos.Length} pantallas, se encontraron {pantallas.Count}");

            (double w, double h, string etiqueta)[] tamaños =
            [
                (1080, 700, "min1080x700"),
                (1180, 860, "normal1180x860"),
                (1920, 1080, "fullhd1920x1080"),
            ];

            var elementos = new List<object>();
            int contador = 0;

            void VolcarTabControl(TabControl tc, string idTc, string padreId, string grupoPrefijo)
            {
                Rect rTc;
                try { rTc = RectCompleto(tc, window); } catch (InvalidOperationException) { return; }
                elementos.Add(new { id = idTc, tipo = "panel_pestanas", padre_id = (string?)padreId, x = rTc.X, y = rTc.Y, ancho = rTc.Width, alto = rTc.Height, grupo = (string?)null, orden_z = OrdenZ(tc), capa = "panel" });
                for (int i = 0; i < tc.Items.Count; i++)
                {
                    if (tc.ItemContainerGenerator.ContainerFromIndex(i) is not TabItem ti || !ti.IsVisible) continue;
                    Rect rTi;
                    try { rTi = RectCompleto(ti, window); } catch (InvalidOperationException) { continue; }
                    if (rTi.Width < 1 || rTi.Height < 1) continue;
                    elementos.Add(new { id = $"{idTc}_item{i}", tipo = "pestana", padre_id = (string?)idTc, x = rTi.X, y = rTi.Y, ancho = rTi.Width, alto = rTi.Height, grupo = (string?)$"{grupoPrefijo}_{tc.Items.Count}", orden_z = OrdenZ(ti), capa = "navegacion" });
                }
            }

            bool cabeceraVolcada = false;
            void VolcarCabecera(string idRaizCombo, string etiquetaTam)
            {
                var tiraVitals = Descendientes<WrapPanel>(window)
                    .FirstOrDefault(wp => Descendientes<TextBlock>(wp).Any(t => t.Text == "♥"));
                if (tiraVitals != null)
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
                        elementos.Add(new { id = $"fresco_cabecera_vitals_{etiquetaTam}_{idxVitals}", tipo = descrHijo, padre_id = (string?)idRaizCombo, x = rHijo.X, y = rHijo.Y, ancho = rHijo.Width, alto = rHijo.Height, grupo = (string?)$"fresco_cabecera_franja_vitales_{etiquetaTam}", orden_z = OrdenZ(hijo), capa = "contenido" });
                        idxVitals++;
                    }
                    Console.WriteLine($"KEEPQA_FRESCO_SOLO-GEOMETRIA[{etiquetaTam}]: {idxVitals} elementos de la franja de vitales de cabecera volcados");
                }
            }

            int capturas = 0;
            foreach (string idiomaCap in new[] { "es", "en" })
            {
                vm.Settings.Language = idiomaCap;
                DoEvents(); DoEvents();
                foreach (var (w, h, etiquetaTam) in tamaños)
                {
                    FijarTamaño(window, w, h);
                    DoEvents(); DoEvents();
                    foreach (var (nombrePantalla, ir) in pantallas)
                    {
                        ir();
                        DoEvents(); DoEvents();
                        string nombreArchivo = nombrePantalla.Replace('/', '-');
                        CapturaVisual(window, (int)window.ActualWidth, (int)window.ActualHeight, $"fresco15sep-pantalla-{nombreArchivo}-{etiquetaTam}-{idiomaCap}");
                        capturas++;

                        string idRaizCombo = $"fresco_raiz_{nombrePantalla}_{etiquetaTam}_{idiomaCap}".Replace('/', '_');
                        elementos.Add(new { id = idRaizCombo, tipo = "raiz_pantalla", padre_id = (string?)$"(ventana_sin_padre_{idRaizCombo})", x = 0.0, y = 0.0, ancho = window.ActualWidth, alto = window.ActualHeight, grupo = (string?)null, orden_z = 0.0, capa = "fondo" });

                        var tcsVisibles = Descendientes<TabControl>(window).Where(t => t.IsVisible).ToList();
                        var idPorTc = new Dictionary<TabControl, string>();
                        foreach (var tc in tcsVisibles) idPorTc[tc] = $"fresco_tabcontrol_{contador++}";
                        foreach (var tc in tcsVisibles)
                        {
                            string padreId = idRaizCombo;
                            for (DependencyObject? d = System.Windows.Media.VisualTreeHelper.GetParent(tc); d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
                            {
                                if (d is TabControl tcAncestro && idPorTc.TryGetValue(tcAncestro, out var idAnc)) { padreId = idAnc; break; }
                            }
                            VolcarTabControl(tc, idPorTc[tc], padreId, $"fresco_tabs_{nombreArchivo}_{etiquetaTam}_{idiomaCap}");
                        }

                        if (nombrePantalla == "Personaje/Inventario" && idiomaCap == "es" && !cabeceraVolcada)
                        {
                            VolcarCabecera(idRaizCombo, etiquetaTam);
                            cabeceraVolcada = true;
                        }
                    }
                }
            }
            vm.Settings.Language = "es";

            string volcadoPath = Path.Combine(outDir, "volcado-geometria-fresco-15sep.json");
            File.WriteAllText(volcadoPath, JsonSerializer.Serialize(elementos, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"KEEPQA_FRESCO_SOLO-GEOMETRIA: {elementos.Count} elementos volcados a {volcadoPath} ({pantallas.Count} pantallas x {tamaños.Length} tamaños x 2 idiomas = {pantallas.Count * tamaños.Length * 2} combinaciones, {capturas} capturas PNG)");
            Console.WriteLine($"KEEPQA_FRESCO_SOLO: evidencia en {outDir}");
        }
        catch (Exception ex) { Console.WriteLine("KEEPQA_FRESCO_SOLO-EXCEPTION: " + ex); }
    }

    // KEEPQA_VITALS_REAL=1 (15-sep-2026): cierra el hueco real que dejaba pasar el desfase de
    // "Defensa" que el usuario encontro A MANO cargando un personaje de verdad - investigado a
    // fondo antes de tocar nada (ver bitacora.md, entrada de esta misma fecha): NINGUN volcado de
    // "cabecera_franja_vitales" (ni el de KEEPQA_SOLO ni el de KEEPQA_FRESCO_SOLO) se hace de
    // verdad con un personaje real. Los dos llaman a Program.cs mucho despues de la linea
    // `vm.LoadFromPath(tempPlr)` (personaje sintetico "UIA-Test", PlrLoadout.CreateEmpty x4, CERO
    // equipo/monedas/horas jugadas) que reemplaza SIN excepcion al personaje real que si se habia
    // abierto un poco antes ("HOME-OPEN: click en 'Eldelgas'...") - todo volcado de la franja de
    // vitales hasta hoy viajaba con Vida=0/0, Mana=0/0, Defensa=0, Dinero="0c", Horas=0h SIEMPRE,
    // el mismo patron ya documentado esta noche ("no era un hueco de la pieza, era un hueco de
    // datos"). Con datos degenerados de un solo digito en las cinco cajas, un desfase real que
    // solo se nota con anchuras VARIABLES (Defensa a 2-3 digitos, Dinero con varias monedas,
    // Horas con 2-3 digitos) no tiene ninguna oportunidad de manifestarse: el campo `grupo` SI
    // esta bien puesto en los 5 elementos (comprobado leyendo VolcarCabecera de arriba), el hueco
    // real es de DATOS, no de la pieza que los etiqueta.
    //
    // Arreglo: un volcado PROPIO, con el PRIMER personaje real de esta maquina (mismo `first` que
    // ya usa HOME-OPEN, "Eldelgas" - Calamity/tModLoader real, equipo puesto de verdad, el mismo
    // tipo de personaje de las capturas originales del usuario), en TODAS las clases de tamaño
    // reales (Compacto/Normal/Amplio/Extra - NormalMinWidth=1320, AmplioMinWidth=1520,
    // ExtraMinWidth=1920, MainViewModel.cs) - hasta hoy el volcado real solo cubria min1080x700 y
    // normal1180x860, las dos por debajo de NormalMinWidth (SizeClass=Compacto siempre). Se llama
    // MUY PRONTO en Program.cs (justo tras abrir `first`, ANTES de `vm.LoadFromPath(tempPlr)`)
    // para que el personaje sintetico nunca llegue a pisarlo.
    private static void EjecutarKeepQaVitalesReal(Window window, MainViewModel vm)
    {
        try
        {
            string outDir = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
            Directory.CreateDirectory(outDir);

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

            if (!vm.IsCharacterLoaded)
            {
                Console.WriteLine("KEEPQA_VITALS_REAL: FALLO - no hay ningun personaje real cargado (llamar ANTES de vm.LoadFromPath(tempPlr))");
                return;
            }
            Console.WriteLine($"KEEPQA_VITALS_REAL: personaje real '{vm.CharacterName}' - Defensa={vm.EquipmentGroup?.TotalDefense}, Dinero='{vm.MoneyText}', Horas={vm.Appearance.PlayHours:0}");

            vm.SelectedTabIndex = 1; // Personaje - IsExplorationTabActive=false, ShowVitalsStrip=true
            DoEvents(); DoEvents();

            (double w, double h, string etiqueta)[] tamaños =
            [
                (1080, 700, "compacto1080x700"),
                (1180, 860, "compacto1180x860"),
                (1320, 860, "normal1320x860"),
                (1500, 860, "normal1500x860"),
                (1520, 860, "amplio1520x860"),
                (1920, 1080, "extra1920x1080"),
            ];

            var elementos = new List<object>();
            foreach (var (w, h, etiquetaTam) in tamaños)
            {
                FijarTamaño(window, w, h);
                DoEvents(); DoEvents();

                var tiraVitals = Descendientes<WrapPanel>(window)
                    .FirstOrDefault(wp => Descendientes<TextBlock>(wp).Any(t => t.Text == "♥"));
                if (tiraVitals == null)
                {
                    Console.WriteLine($"KEEPQA_VITALS_REAL: FALLO - no se encuentra la franja de vitales a {etiquetaTam}");
                    continue;
                }

                string idRaiz = $"vitals_real_raiz_{etiquetaTam}";
                elementos.Add(new { id = idRaiz, tipo = "raiz_pantalla", padre_id = (string?)$"(ventana_sin_padre_{idRaiz})", x = 0.0, y = 0.0, ancho = window.ActualWidth, alto = window.ActualHeight, grupo = (string?)null, orden_z = 0.0, capa = "fondo" });

                int idx = 0;
                var centros = new List<(string id, double centroY, double h)>();
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
                    string idElem = $"vitals_real_{etiquetaTam}_{idx}";
                    elementos.Add(new { id = idElem, tipo = descrHijo, padre_id = (string?)idRaiz, x = rHijo.X, y = rHijo.Y, ancho = rHijo.Width, alto = rHijo.Height, grupo = (string?)$"cabecera_franja_vitales_real_{etiquetaTam}", orden_z = OrdenZ(hijo), capa = "contenido" });
                    centros.Add((idElem, rHijo.Y + rHijo.Height / 2, rHijo.Height));
                    idx++;
                }
                Console.WriteLine($"KEEPQA_VITALS_REAL[{etiquetaTam}]: SizeClass={vm.SizeClass} MaxWidth={vm.VitalsStripMaxWidth:0}, {idx} elementos reales volcados (grupo cabecera_franja_vitales_real_{etiquetaTam})");

                // Verificacion propia, sin esperar a node: agrupa los elementos por LINEA real
                // (clustering por centro Y) ANTES de comparar alineacion dentro de cada linea - en
                // vez de comparar los 5 elementos contra UNA SOLA mediana global. Arreglo del
                // FALSO POSITIVO real de esta misma pieza, encontrado por el coordinador el
                // 15-sep-2026 y confirmado a ojo contra 'vitals-real-compacto1180x860.png': con el
                // diseño real e intencional de 2 lineas en SizeClass=Compacto (Vida+Mana arriba,
                // Defensa+Dinero+Horas abajo, ver bitacora.md "ARREGLO RECOMENDADO (aplicado):
                // subir VitalsStripMaxWidth de 200 a 210"), la mediana global cae ENTRE las dos
                // lineas, asi que los 5 elementos quedaban "desviados" de esa mediana aunque cada
                // linea estuviera perfectamente alineada consigo misma (0px de desvio real dentro
                // de cada una, verificado a mano contra el volcado). Mismo criterio de fondo que
                // `detectarOrientacion` de KeepQA/src/alineacion/verificarAlineacion.js: agrupar
                // ANTES de comparar, nunca mezclar lineas/ejes distintos en una sola mediana - la
                // cabecera de ese fichero documenta que el clustering por fila/columna real dentro
                // de un grupo no estaba construido todavia en ninguna pieza compartida; este es ese
                // clustering, construido aqui porque el caso real que lo necesita (la franja de
                // vitales en Compacto) vive en este arnes.
                if (centros.Count > 1)
                {
                    var ordenados = centros.OrderBy(c => c.centroY).ToList();
                    double altoMedianoGrupo = ordenados.Select(c => c.h).OrderBy(v => v).ElementAt(ordenados.Count / 2);
                    // Umbral de corte entre lineas: un salto de centro Y mayor que "media linea de
                    // alto" entre dos elementos consecutivos (ordenados por centro Y) es una linea
                    // NUEVA, no la misma linea con ruido - medido contra el caso real: ~0px de salto
                    // DENTRO de una linea (Vida/Mana o Defensa/Dinero/Horas, perfectamente
                    // alineados) frente a ~29px de salto ENTRE lineas a 1080x700/1180x860 (bitacora.
                    // md, 15-sep-2026) - la mitad del alto mediano separa ambos casos con margen de
                    // sobra en los dos sentidos. Suelo de 4px para que un grupo de una sola linea con
                    // elementos muy finos (barras de pocos px de alto) no fragmente en clusters de 1
                    // solo por ruido de subpixel.
                    double umbralLinea = Math.Max(4.0, altoMedianoGrupo / 2.0);

                    var lineas = new List<List<(string id, double centroY, double h)>>();
                    foreach (var c in ordenados)
                    {
                        if (lineas.Count > 0 && c.centroY - lineas[^1][^1].centroY <= umbralLinea)
                            lineas[^1].Add(c);
                        else
                            lineas.Add(new List<(string id, double centroY, double h)> { c });
                    }

                    Console.WriteLine($"KEEPQA_VITALS_REAL[{etiquetaTam}]: {lineas.Count} linea(s) real(es) detectada(s) por centro Y (umbral {umbralLinea:0.00}px) - {string.Join(" | ", lineas.Select(l => $"[{string.Join(",", l.Select(m => m.id))}]"))}");

                    foreach (var linea in lineas)
                    {
                        if (linea.Count < 2) continue; // nada con quien comparar dentro de esta linea real
                        double medianaLinea = linea.OrderBy(c => c.centroY).ElementAt(linea.Count / 2).centroY;
                        foreach (var c in linea)
                        {
                            double desvio = Math.Abs(c.centroY - medianaLinea);
                            if (desvio > 1.0)
                                Console.WriteLine($"KEEPQA_VITALS_REAL[{etiquetaTam}]: AVISO - '{c.id}' (alto={c.h:0.00}) tiene centro Y={c.centroY:0.00}, desviado {desvio:0.00}px de la mediana de SU LINEA real ({medianaLinea:0.00}) - posible desfase real dentro de la misma linea");
                        }
                    }
                }

                CapturaVisual(window, (int)window.ActualWidth, (int)window.ActualHeight, $"vitals-real-{etiquetaTam}");
            }

            string volcadoPath = Path.Combine(outDir, "volcado-geometria-vitals-real.json");
            File.WriteAllText(volcadoPath, JsonSerializer.Serialize(elementos, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"KEEPQA_VITALS_REAL: {elementos.Count} elementos (personaje real '{vm.CharacterName}') volcados a {volcadoPath}, {tamaños.Length} capturas PNG en {outDir}");
        }
        catch (Exception ex) { Console.WriteLine("KEEPQA_VITALS_REAL-EXCEPTION: " + ex); }
    }

    // KEEPQA_VITALS_DINERO_LARGO=1 (15-sep-2026): Parte 2 del encargo del coordinador - confirmar
    // si el arreglo de anoche (VitalsStripMaxWidth 200->210, calibrado SOLO contra el dinero corto
    // de 'Eldelgas', "59p 43o 83s") sigue aguantando con un dinero MUCHO mas largo (5 cifras de
    // platino, "20013p 9o" - el mismo tipo de valor que el usuario enseño en vivo con su personaje
    // 100% vanilla 'Terrariano'). Usa el personaje real 'Terrariano.plr' (Documents\My Games\
    // Terraria\Players, vanilla puro - NUNCA \tModLoader\Players\Terrariano.plr, un personaje
    // DISTINTO con el mismo nombre) como base real, pero SOLO sobre una COPIA en el temp del
    // sistema - el fichero real del usuario en Documentos NUNCA se toca ni se abre siquiera
    // (File.Copy primero, cualquier lectura/escritura posterior va contra la copia).
    //
    // Los 4 slots de Coins (cobre id71/plata id72/oro id73/platino id74, mismo mapa que
    // CoinValueInCopper de MainViewModel.cs) se sobreescriben a mano con PlrFile real
    // (Terrakeep.Core.PlrFormat, via CharacterFileService.Load/Save - nunca un byte inventado)
    // para forzar Platino=20013 y Oro=9, el "20013p 9o" real que enseño el usuario, sea cual sea
    // el dinero real de partida de la copia. Esto bypassa a proposito el clamp de 9999 que
    // ItemSlotViewModel.OnCountChanged aplica a una EDICION manual desde la UI (Math.Clamp(value,
    // 1, 9999)) - ese clamp NUNCA se aplica al CARGAR un .plr real (ItemSlotViewModel.UpdateFrom
    // pone _suppressCountWriteback=true antes de fijar Count), asi que es fiel a como la app trata
    // de verdad un .plr con una pila de monedas de mas de 9999 unidades.
    private static void EjecutarKeepQaVitalesRealDineroLargo(Window window, MainViewModel vm)
    {
        try
        {
            // Cifras de las 4 monedas configurables por variable de entorno (por defecto el caso
            // real exacto que enseño el usuario, "20013p 9o") - permite explorar el margen real
            // hasta romper sin recompilar por cada cifra distinta, MISMO mecanismo real
            // (CharacterFileService.Load/Save, PlrFormat real) para cualquier valor.
            int platino = int.TryParse(Environment.GetEnvironmentVariable("KEEPQA_DINERO_PLATINO"), out int vp) ? vp : 20013;
            int oro = int.TryParse(Environment.GetEnvironmentVariable("KEEPQA_DINERO_ORO"), out int vo) ? vo : 9;
            int plata = int.TryParse(Environment.GetEnvironmentVariable("KEEPQA_DINERO_PLATA"), out int vs) ? vs : 0;
            int cobre = int.TryParse(Environment.GetEnvironmentVariable("KEEPQA_DINERO_COBRE"), out int vc) ? vc : 0;

            string outDir = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
            Directory.CreateDirectory(outDir);

            string documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string origenReal = Path.Combine(documentos, "My Games", "Terraria", "Players", "Terrariano.plr");
            if (!File.Exists(origenReal))
            {
                Console.WriteLine($"KEEPQA_VITALS_DINERO_LARGO: FALLO - no se encuentra el personaje real '{origenReal}'");
                return;
            }

            string copiaTemp = Path.Combine(Path.GetTempPath(), "keepqa-dinero-largo-terrariano.plr");
            File.Copy(origenReal, copiaTemp, overwrite: true);
            Console.WriteLine($"KEEPQA_VITALS_DINERO_LARGO: copia real de '{origenReal}' -> '{copiaTemp}' (el original NUNCA se toca)");

            var service = new Terrakeep.App.Services.CharacterFileService();
            var loaded = service.Load(copiaTemp);
            static string DescribirCoins(Terrakeep.Core.PlrFormat.PlrItemSlot[] c) =>
                string.Join(", ", c.Select(s => $"id={s.Id} count={s.Count}"));
            Console.WriteLine($"KEEPQA_VITALS_DINERO_LARGO: Coins REALES de 'Terrariano' antes de tocar nada: [{DescribirCoins(loaded.Character.Coins)}] (mapa id71=cobre/72=plata/73=oro/74=platino)");

            loaded.Character.Coins =
            [
                new Terrakeep.Core.PlrFormat.PlrItemSlot(cobre > 0 ? 71 : 0, cobre > 0 ? cobre : 0, 0, false),
                new Terrakeep.Core.PlrFormat.PlrItemSlot(plata > 0 ? 72 : 0, plata > 0 ? plata : 0, 0, false),
                new Terrakeep.Core.PlrFormat.PlrItemSlot(oro > 0 ? 73 : 0, oro > 0 ? oro : 0, 0, false),
                new Terrakeep.Core.PlrFormat.PlrItemSlot(platino > 0 ? 74 : 0, platino > 0 ? platino : 0, 0, false),
            ];
            service.Save(loaded);
            Console.WriteLine($"KEEPQA_VITALS_DINERO_LARGO: Coins FORZADOS en la copia -> [{DescribirCoins(loaded.Character.Coins)}], guardado real en '{copiaTemp}' via CharacterFileService.Save (PlrFile.Write real)");

            vm.LoadFromPath(copiaTemp);
            DoEvents(); DoEvents();
            if (!vm.IsCharacterLoaded)
            {
                Console.WriteLine("KEEPQA_VITALS_DINERO_LARGO: FALLO - la copia modificada no cargo (IsCharacterLoaded=false)");
                return;
            }
            Console.WriteLine($"KEEPQA_VITALS_DINERO_LARGO: personaje real cargado '{vm.CharacterName}' - Defensa={vm.EquipmentGroup?.TotalDefense}, Dinero='{vm.MoneyText}', Horas={vm.Appearance.PlayHours:0}");

            vm.SelectedTabIndex = 1; // Personaje
            DoEvents(); DoEvents();

            (double w, double h, string etiqueta)[] tamaños =
            [
                (1080, 700, "compacto1080x700"),
                (1180, 860, "compacto1180x860"),
                (1320, 860, "normal1320x860"),
                (1500, 860, "normal1500x860"),
                (1520, 860, "amplio1520x860"),
                (1920, 1080, "extra1920x1080"),
            ];

            var elementos = new List<object>();
            bool huecoConfirmado = false;
            foreach (var (w, h, etiquetaTam) in tamaños)
            {
                FijarTamaño(window, w, h);
                DoEvents(); DoEvents();

                var tiraVitals = Descendientes<WrapPanel>(window)
                    .FirstOrDefault(wp => Descendientes<TextBlock>(wp).Any(t => t.Text == "♥"));
                if (tiraVitals == null)
                {
                    Console.WriteLine($"KEEPQA_VITALS_DINERO_LARGO: FALLO - no se encuentra la franja de vitales a {etiquetaTam}");
                    continue;
                }

                string idRaiz = $"vitals_dinero_largo_raiz_{etiquetaTam}";
                elementos.Add(new { id = idRaiz, tipo = "raiz_pantalla", padre_id = (string?)$"(ventana_sin_padre_{idRaiz})", x = 0.0, y = 0.0, ancho = window.ActualWidth, alto = window.ActualHeight, grupo = (string?)null, orden_z = 0.0, capa = "fondo" });

                int idx = 0;
                var centros = new List<(string id, double centroY, double h, double x, double ancho)>();
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
                    string idElem = $"vitals_dinero_largo_{etiquetaTam}_{idx}";
                    elementos.Add(new { id = idElem, tipo = descrHijo, padre_id = (string?)idRaiz, x = rHijo.X, y = rHijo.Y, ancho = rHijo.Width, alto = rHijo.Height, grupo = (string?)$"cabecera_franja_vitales_dinero_largo_{etiquetaTam}", orden_z = OrdenZ(hijo), capa = "contenido" });
                    centros.Add((idElem, rHijo.Y + rHijo.Height / 2, rHijo.Height, rHijo.X, rHijo.Width));
                    idx++;
                }
                Console.WriteLine($"KEEPQA_VITALS_DINERO_LARGO[{etiquetaTam}]: SizeClass={vm.SizeClass} MaxWidth={vm.VitalsStripMaxWidth:0}, {idx} elementos reales volcados");

                // Mismo clustering por linea real (centro Y) que ya arreglo el falso positivo de
                // KEEPQA_VITALS_REAL (Parte 1 de este mismo encargo) - aqui para saber cuantas
                // LINEAS reales forma el WrapPanel con este dinero largo: 2 lineas es el diseño
                // correcto (Vida+Mana arriba, Defensa+Dinero+Horas abajo), 3+ lineas es el bug
                // real reapareciendo.
                if (centros.Count > 1)
                {
                    var ordenados = centros.OrderBy(c => c.centroY).ToList();
                    double altoMedianoGrupo = ordenados.Select(c => c.h).OrderBy(v => v).ElementAt(ordenados.Count / 2);
                    double umbralLinea = Math.Max(4.0, altoMedianoGrupo / 2.0);
                    var lineas = new List<List<(string id, double centroY, double h, double x, double ancho)>>();
                    foreach (var c in ordenados)
                    {
                        if (lineas.Count > 0 && c.centroY - lineas[^1][^1].centroY <= umbralLinea)
                            lineas[^1].Add(c);
                        else
                            lineas.Add(new List<(string id, double centroY, double h, double x, double ancho)> { c });
                    }
                    Console.WriteLine($"KEEPQA_VITALS_DINERO_LARGO[{etiquetaTam}]: {lineas.Count} linea(s) real(es) detectada(s) - {string.Join(" | ", lineas.Select(l => $"[{string.Join(",", l.Select(m => m.id))}]"))}");
                    if (lineas.Count > 2)
                    {
                        huecoConfirmado = true;
                        Console.WriteLine($"FALLO: KEEPQA_VITALS_DINERO_LARGO - {etiquetaTam} parte la franja en {lineas.Count} lineas (se esperaban 2) - el bug real de Defensa/Dinero/Horas huerfanas reaparece con dinero largo");
                    }
                    foreach (var linea in lineas)
                    {
                        if (linea.Count < 2) continue;
                        double medianaLinea = linea.OrderBy(c => c.centroY).ElementAt(linea.Count / 2).centroY;
                        foreach (var c in linea)
                        {
                            double desvio = Math.Abs(c.centroY - medianaLinea);
                            if (desvio > 1.0)
                                Console.WriteLine($"KEEPQA_VITALS_DINERO_LARGO[{etiquetaTam}]: AVISO - '{c.id}' (alto={c.h:0.00}) tiene centro Y={c.centroY:0.00}, desviado {desvio:0.00}px de la mediana de SU LINEA real ({medianaLinea:0.00})");
                        }
                    }

                    // Medida real EXACTA del ancho de la primera linea (Vida+Mana, o Vida+Mana+lo
                    // que le quepa detras si el bug reaparte la franja) frente a VitalsStripMaxWidth
                    // - la causa real exacta que pide documentar la Parte 2 del encargo.
                    var linea1 = lineas[0];
                    double anchoLinea1 = linea1.Max(c => c.x + c.ancho) - linea1.Min(c => c.x);
                    Console.WriteLine($"KEEPQA_VITALS_DINERO_LARGO[{etiquetaTam}]: ancho real ocupado por la linea 1 ({linea1.Count} elemento(s): {string.Join(",", linea1.Select(m => m.id))}) = {anchoLinea1:0.00}px frente a VitalsStripMaxWidth={vm.VitalsStripMaxWidth:0}px");
                }

                var rtb = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(window);
                var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
                enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));
                using var fs = File.Create(Path.Combine(outDir, $"vitals-dinero-largo-{etiquetaTam}.png"));
                enc.Save(fs);
            }

            string volcadoPath = Path.Combine(outDir, "volcado-geometria-vitals-dinero-largo.json");
            File.WriteAllText(volcadoPath, JsonSerializer.Serialize(elementos, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"KEEPQA_VITALS_DINERO_LARGO: {elementos.Count} elementos volcados a {volcadoPath}, {tamaños.Length} capturas PNG en {outDir}");
            Console.WriteLine(huecoConfirmado
                ? "KEEPQA_VITALS_DINERO_LARGO: RESULTADO: bug real CONFIRMADO con dinero largo (3+ lineas en al menos un ancho)"
                : "KEEPQA_VITALS_DINERO_LARGO: RESULTADO: sin bug real detectado (maximo 2 lineas en todos los anchos)");

            File.Delete(copiaTemp);
            string tplrCopiaTemp = Path.ChangeExtension(copiaTemp, ".tplr");
            if (File.Exists(tplrCopiaTemp)) File.Delete(tplrCopiaTemp);
            if (File.Exists(copiaTemp + ".bak")) File.Delete(copiaTemp + ".bak");
        }
        catch (Exception ex) { Console.WriteLine("KEEPQA_VITALS_DINERO_LARGO-EXCEPTION: " + ex); }
    }
}
