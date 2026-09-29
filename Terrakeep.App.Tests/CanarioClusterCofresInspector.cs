// CANARIO REAL (24-sep-2026, revision-correccion-integral-familia-Keep, cluster "imagen5+imagen6"
// - Exploracion: cofres/inspector lateral) - investigado por el agente investigador-bug del patron
// de 2 fases (NO toca produccion, solo este arnes). Encargo del usuario, 4 puntos:
//   1. "Editar cofre" solo alcanzable con hover, desaparece al mover el raton hacia el boton.
//   2. Tabs del selector de prefijo (Biblioteca/Positivos/Negativos, Mejor/Daño/Critico...) se
//      cortan dentro del editor de cofre.
//   3. Rectangulo vacio grande en la parte inferior del panel lateral, en los 2 modos de busqueda
//      de cofres (por tipo / cofre a cofre).
//   4. Probar ventana alta/baja, DPI, texto largo, listas largas, scroll, seleccion, todos los
//      modos de busqueda de cofres.
//
// RESULTADO REAL DE LA INVESTIGACION (ver bitacora.md, misma fecha, para el detalle completo):
//   - Punto 1: NO REPRODUCIBLE en el codigo actual. `ChestRowTemplate` (MainWindow.xaml:1814-1829,
//     boton "Editar") NO tiene NINGUN trigger de IsMouseOver que condicione su Visibility - el
//     propio comentario del XAML (MainWindow.xaml:1809-1813) documenta que se dejo "SIEMPRE
//     visible" a proposito. `CurrentChestMarker` (MainWindow.xaml:5896-5909) es clicable
//     DIRECTAMENTE (MouseBinding LeftClick -> EditCurrentChestOnMapCommand), sin hover previo. Los
//     dos ya tienen regresion real cubierta por AR-13d (Program.cs, invocacion directa de comando)
//     y AR-13e (Program.cs, clic REAL de sistema operativo down+up sobre el marcador, sin
//     arrastre) - ambos arreglados el 17/18-sep-2026 (ver bitacora.md, "editor de cofres no se
//     encuentra"/"el clic sobre un cofre del mapa que nunca existio"). Este fichero NO repite esa
//     cobertura (ya existe); solo añade una guarda de regresion ESTATICA (sin foco/raton real,
//     segura de correr con el usuario delante) para que si alguien reintroduce una Visibility
//     condicionada a IsMouseOver en cualquiera de los dos, quede detectado sin depender de un
//     clic real de SO. LIMITE REAL: no se pudo reproducir en VIVO con el raton del usuario en el
//     momento de esta investigacion (la guardia de entrada grafica de KeepQA bloqueo la
//     automatizacion - el usuario estaba usando el equipo activamente). Recomendado a
//     coordinador/aplicador-fix: pedir confirmacion en vivo o revisar si el reporte del usuario es
//     anterior a los arreglos del 17/18-sep (la build de bin\Debug es del 24-sep 11:53, la
//     instalada en %LocalAppData%\Programs\Terrakeep del 21-sep - las dos posteriores al arreglo).
//   - Punto 2 y 3: causa real CONFIRMADA por lectura de codigo + aritmetica de las constantes
//     reales del propio XAML (no una suposicion "deberia fallar") - EJECUTAR ESTE BLOQUE
//     (COFRES_INSPECTOR_SOLO=1) DA LOS NUMEROS REALES RENDERIZADOS, pendiente porque el momento de
//     la investigacion no permitia abrir una ventana real (ver arriba).
//     - Punto 3: `ExplorationResultsBlock` (Border, MainWindow.xaml:6946) solo se colapsa para
//       SelectedCategory=="Npcs" (MainWindow.xaml:6950-6952) - NUNCA para Cofres. Su Grid interior
//       (MainWindow.xaml:6963-6972) tiene 7 filas; las 6 primeras se ocultan de verdad segun
//       WorldSearchResults.Count/estado, pero la ULTIMA (el ListBox real de resultados,
//       MainWindow.xaml:6971) es `Height="*" MinHeight="70"` - un MinHeight INCONDICIONAL que
//       reserva ~70px pase lo que pase, sea o no Collapsed el ListBox que vive dentro. Sumado al
//       Padding="10" del Border (20px) y su Margin="0,8,0,0" (8px), el suelo REAL medible desde
//       las constantes del propio XAML es de ~98px. `RebuildChestInventory`
//       (ExplorationViewModel.cs:1078-1097) NUNCA escribe en WorldSearchResults para
//       ChestViewMode==2 ("Cofre a cofre") - ese modo deja el bloque muerto el 100% del tiempo.
//       Para los modos 0/1 ("Por tipo"/"Por lo que contienen") solo se llena tras pulsar "Buscar
//       seleccionados" (SearchCheckedInventoryCommand) - antes de eso, tambien vacio. Ademas,
//       `ShowZeroResultsState` (ExplorationViewModel.cs:891) exige
//       `SelectedCategory == WorldSearchCategory.All` - la categoria Cofres queda EXCLUIDA a
//       proposito del mensaje amistoso de "sin resultados", asi que el hueco ademas no explica
//       nada al usuario.
//     - Punto 2: `ContentControl` que aloja `ItemEditTemplate` dentro del editor de cofre
//       (MainWindow.xaml:1881-1882) lleva `MaxHeight="320"` - la MISMA plantilla usada en
//       Personaje/Inventario (MainWindow.xaml:3449) NO tiene ningun tope de altura. Dentro de
//       `ItemEditTemplate` (MainWindow.xaml:25-236), el selector de prefijo de 3 niveles
//       (Metas="Biblioteca/Positivos/Negativos" linea 174-185, Groups="Mejor/Daño/Critico..."
//       linea 187-198, Prefixes linea 200-232) va DESPUES del bloque
//       Icono+Nombre+Indice+Cantidad+Prefijo(id) (linea 62-168), que el solo ya ocupa varios
//       cientos de px reales - dentro de un techo de 320px eso deja poco margen real para Metas/
//       Groups antes de necesitar scroll interno.
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Terrakeep.App;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.WldFormat;

internal static partial class Program
{
    private static void EjecutarClusterCofresInspectorSolo(MainWindow window, MainViewModel vm)
    {
        try
        {
            // ================= Guarda estatica de regresion del punto 1 (sin raton/foco real) =================
            // No reproduce nada nuevo (AR-13d/AR-13e ya cubren el clic real) - solo confirma, leyendo
            // el arbol visual YA MONTADO, que ninguna de las dos vias de "Editar cofre" depende de
            // IsMouseOver para su Visibility. Si algun cambio futuro le añade un trigger de hover a
            // cualquiera de los dos, esto debe pasar a FALLO sin necesidad de un clic real de SO.
            vm.SelectedTabIndex = 4; // Exploracion
            DoEvents(); DoEvents();

            var marcadorCofreActual = Descendientes<System.Windows.Shapes.Rectangle>(window).FirstOrDefault(r => r.Name == "CurrentChestMarker");
            if (marcadorCofreActual == null)
                Console.WriteLine("COFRES-INSPECTOR-P1: AVISO - no se encontro CurrentChestMarker en el arbol visual (x:Name, MainWindow.xaml ~5896)");
            else
            {
                var canvasPadre = VisualTreeHelper.GetParent(marcadorCofreActual) as UIElement;
                bool hitTestable = marcadorCofreActual.IsHitTestVisible && (canvasPadre == null || canvasPadre.IsHitTestVisible);
                bool tieneMouseBindingDirecto = marcadorCofreActual.InputBindings.OfType<System.Windows.Input.MouseBinding>()
                    .Any(mb => mb.MouseAction == System.Windows.Input.MouseAction.LeftClick);
                Console.WriteLine($"COFRES-INSPECTOR-P1: CurrentChestMarker IsHitTestVisible={hitTestable}, tiene MouseBinding LeftClick directo={tieneMouseBindingDirecto} (esperado True/True - clicable SIN hover previo)");
                if (!hitTestable || !tieneMouseBindingDirecto)
                    Console.WriteLine("FALLO: COFRES-INSPECTOR-P1 - CurrentChestMarker ya no es clicable directamente (o perdio su MouseBinding) - regresion real del arreglo del 17-sep-2026");
            }

            // ================= Puntos 2 y 3: geometria real medida sobre un mundo real =================
            string mundo = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\Worlds\Blando_Río.wld");
            if (!File.Exists(mundo))
            {
                Console.WriteLine($"COFRES-INSPECTOR: no se encontro {mundo} (mundo real con 358 cofres) - abortando la parte de geometria (puntos 2/3)");
                return;
            }

            FijarTamaño(window, 1180, 860); // tamaño de arranque real de la app
            var carga = vm.Exploration.LoadFromPathAsync(mundo);
            while (!carga.IsCompleted) { DoEvents(); System.Threading.Thread.Sleep(15); }
            DoEvents(); DoEvents();

            // --------- Punto 3: bloque de resultados compartido, vacio en Cofre a cofre ---------
            vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
            vm.Exploration.ChestViewMode = 2; // Cofre a cofre
            DoEvents(); DoEvents(); window.UpdateLayout();

            // Guarda estatica de regresion del punto 1 (sin raton/foco real): con filas REALES ya
            // realizadas en el arbol visual (mundo cargado, modo 'Cofre a cofre'), confirma que el
            // boton 'Editar' de al menos una fila esta Visible SIN ningun IsMouseOver simulado - no
            // reproduce nada nuevo (AR-13d/AR-13e ya cubren el clic real sobre el marcador del mapa
            // con SO), solo blinda que nadie reintroduzca una Visibility condicionada a hover aqui.
            var botonEditarCofre = Descendientes<Button>(window).FirstOrDefault(b =>
                (BindingOperations.GetBindingExpression(b, Button.CommandProperty)?.ParentBinding?.Path?.Path)
                == "DataContext.Exploration.EditChestCommand");
            if (botonEditarCofre == null)
                Console.WriteLine("COFRES-INSPECTOR-P1: AVISO - no se encontro ningun Button ligado a Exploration.EditChestCommand en el arbol visual ya realizado (¿cambio el binding, o la virtualizacion no realizo ninguna fila? revisar ChestRowTemplate, MainWindow.xaml ~1814)");
            else
            {
                bool visibleSinHover = botonEditarCofre.Visibility == Visibility.Visible;
                Console.WriteLine($"COFRES-INSPECTOR-P1: boton 'Editar' (ChestRowTemplate, fila real ya realizada) Visibility={botonEditarCofre.Visibility} sin ningun IsMouseOver simulado (esperado Visible - MainWindow.xaml:1809-1813 documenta 'SIEMPRE visible')");
                if (!visibleSinHover)
                    Console.WriteLine("FALLO: COFRES-INSPECTOR-P1 - el boton 'Editar' de la fila de cofre NO esta Visible por defecto (sin hover) - regresion real del punto 1 del encargo (hover-dependencia reintroducida)");

                // Confirma tambien, por Style.Triggers, que no hay ningun trigger de IsMouseOver
                // que gobierne Visibility/Opacity en el propio Style del boton (no solo el estado
                // puntual de ahora mismo).
                bool triggerHoverSobreVisibilidad = false;
                var style = botonEditarCofre.Style;
                while (style != null)
                {
                    foreach (var trig in style.Triggers)
                    {
                        if (trig is System.Windows.Trigger t && t.Property == UIElement.IsMouseOverProperty
                            && t.Setters.OfType<Setter>().Any(s => s.Property == UIElement.VisibilityProperty || s.Property == UIElement.OpacityProperty))
                            triggerHoverSobreVisibilidad = true;
                    }
                    style = style.BasedOn;
                }
                Console.WriteLine($"COFRES-INSPECTOR-P1: Style del boton 'Editar' con trigger IsMouseOver sobre Visibility/Opacity={triggerHoverSobreVisibilidad} (esperado False)");
                if (triggerHoverSobreVisibilidad)
                    Console.WriteLine("FALLO: COFRES-INSPECTOR-P1 - el Style del boton 'Editar' SI condiciona Visibility/Opacity a IsMouseOver - esto reproduciria el bug de hover del punto 1");
            }

            // ADR-TERRAKEEP-029 (26-sep-2026): Browse se movio a BrowseView.xaml (UserControl con
            // su propio NameScope) - FindName DOBLE, mismo patron ya usado por GUIA/ADR-021,
            // Compare/ADR-025, WorldTools/ADR-027 y ChestInspector/ADR-028. browseViewHost se
            // reutiliza en el resto de este mismo metodo (un unico window.FindName("BrowseView")
            // real, BrowseView es un hijo SIEMPRE presente del Grid host, su Visibility no afecta
            // a FindName).
            var browseViewHost = window.FindName("BrowseView") as FrameworkElement;
            var bloqueResultados = browseViewHost?.FindName("ExplorationResultsBlock") as FrameworkElement;
            int countResultados = vm.Exploration.WorldSearchResults.Count;
            int countCofres = vm.Exploration.ChestRows.Count;
            if (bloqueResultados == null)
                Console.WriteLine("COFRES-INSPECTOR-P3: AVISO - no se encontro ExplorationResultsBlock por FindName (¿x:Name cambio? MainWindow.xaml ~6946)");
            else
            {
                double altoBloque = bloqueResultados.ActualHeight;
                Console.WriteLine($"COFRES-INSPECTOR-P3: modo 'Cofre a cofre' -> WorldSearchResults.Count={countResultados} (esperado 0), ChestRows.Count={countCofres} (esperado >0, hay contenido real que SI se ve), ExplorationResultsBlock.Visibility={bloqueResultados.Visibility}, ActualHeight real medido={altoBloque:0.#}px");
                if (countResultados != 0)
                    Console.WriteLine("COFRES-INSPECTOR-P3: AVISO - WorldSearchResults ya no esta vacio en 'Cofre a cofre' en este mundo/estado - repetir con el mundo/modo tal cual describe el encargo antes de confiar en el resto de esta medicion");
                else if (bloqueResultados.Visibility == Visibility.Visible && altoBloque > 40)
                    Console.WriteLine($"FALLO: COFRES-INSPECTOR-P3 - ExplorationResultsBlock ocupa {altoBloque:0.#}px reales VACIO (0 resultados) en modo 'Cofre a cofre', sin ningun mensaje explicativo (ShowZeroResultsState excluye Chests, ExplorationViewModel.cs:891) - exactamente el rectangulo vacio senalado por el usuario en imagen5/imagen6. Causa: MinHeight=70 incondicional en MainWindow.xaml:6971 + Padding/Margin del Border MainWindow.xaml:6946.");
                else if (bloqueResultados.Visibility != Visibility.Visible)
                    Console.WriteLine("COFRES-INSPECTOR-P3: ExplorationResultsBlock esta Collapsed en este estado - el hueco vacio NO se reprodujo con estos datos, revisar si el mundo/estado real del usuario difiere");
            }

            // Repite la misma medicion en 'Por tipo de cofre' (modo 0, imagen6) SIN pulsar "Buscar
            // seleccionados" - el estado exacto de la captura del usuario.
            vm.Exploration.ChestViewMode = 0;
            DoEvents(); DoEvents(); window.UpdateLayout();
            if (bloqueResultados != null)
            {
                Console.WriteLine($"COFRES-INSPECTOR-P3: modo 'Por tipo de cofre' (sin buscar aun) -> WorldSearchResults.Count={vm.Exploration.WorldSearchResults.Count} (esperado 0), ActualHeight real medido={bloqueResultados.ActualHeight:0.#}px");
                if (vm.Exploration.WorldSearchResults.Count == 0 && bloqueResultados.Visibility == Visibility.Visible && bloqueResultados.ActualHeight > 40)
                    Console.WriteLine($"FALLO: COFRES-INSPECTOR-P3 - mismo hueco vacio real en 'Por tipo de cofre' antes de buscar ({bloqueResultados.ActualHeight:0.#}px) - coincide con imagen6.png");
            }

            // --------- Punto 2: tabs Metas/Groups del editor de cofre, dentro del techo de 320px ---------
            vm.Exploration.ChestViewMode = 2;
            DoEvents(); DoEvents();
            var filaConContenido = vm.Exploration.ChestRows.FirstOrDefault(r => r.Items.Count > 0);
            if (filaConContenido == null)
            {
                Console.WriteLine("COFRES-INSPECTOR-P2: AVISO - ningun cofre real de este mundo tiene contenido (Items.Count>0) - no se puede abrir el editor para medir");
            }
            else
            {
                vm.Exploration.EditChestCommand.Execute(filaConContenido);
                DoEvents(); DoEvents();

                // Selecciona el primer slot NO vacio cuyo objeto SI admita prefijo (CanHavePrefix),
                // para que Metas/Groups/Prefixes se rendericen de verdad (si no, esas 3 listas
                // quedan Collapsed y no habria nada que medir).
                ItemSlotViewModel? slotConPrefijo = null;
                foreach (var slot in vm.Exploration.EditingChestSlots)
                {
                    if (slot.Item.Id == 0) continue;
                    vm.Exploration.SelectChestSlot(slot);
                    DoEvents();
                    if (vm.Exploration.ChestItemEdit.CanHavePrefix) { slotConPrefijo = slot; break; }
                }

                if (slotConPrefijo == null)
                    Console.WriteLine("COFRES-INSPECTOR-P2: AVISO - ningun objeto real de este cofre admite prefijo (CanHavePrefix) - no se puede medir el recorte de Metas/Groups con este cofre concreto");
                else
                {
                    DoEvents(); window.UpdateLayout();

                    var contentControlEditor = Descendientes<ContentControl>(window).FirstOrDefault(cc =>
                        ReferenceEquals(cc.Content, vm.Exploration.ChestItemEdit) && cc.ContentTemplate != null);
                    var metasList = Descendientes<ItemsControl>(window).FirstOrDefault(ic =>
                        ReferenceEquals(ic.ItemsSource, vm.Exploration.ChestItemEdit.Metas));
                    var groupsList = Descendientes<ItemsControl>(window).FirstOrDefault(ic =>
                        ReferenceEquals(ic.ItemsSource, vm.Exploration.ChestItemEdit.Groups));

                    Console.WriteLine($"COFRES-INSPECTOR-P2: cofre real con objeto prefijable localizado (Items.Count={filaConContenido.Items.Count}), CanHavePrefix=True, ContentControl del editor encontrado={contentControlEditor != null}, lista Metas encontrada={metasList != null}, lista Groups encontrada={groupsList != null}");

                    if (contentControlEditor != null)
                        Console.WriteLine($"COFRES-INSPECTOR-P2: ContentControl (host de ItemEditTemplate dentro del cofre) ActualHeight real={contentControlEditor.ActualHeight:0.#}px (MaxHeight XAML=320, MainWindow.xaml:1882)");

                    if (metasList != null && contentControlEditor != null)
                    {
                        bool metasVisible = metasList.IsVisible;
                        Rect? rectMetas = null;
                        try
                        {
                            var esquina = metasList.TransformToAncestor(contentControlEditor).Transform(new Point(0, 0));
                            rectMetas = new Rect(esquina, new Size(metasList.ActualWidth, metasList.ActualHeight));
                        }
                        catch (InvalidOperationException) { /* no conectado al mismo arbol visual todavia */ }

                        Console.WriteLine($"COFRES-INSPECTOR-P2: WrapPanel 'Metas' (Biblioteca/Positivos/Negativos) IsVisible={metasVisible}, posicion real relativa al ContentControl={rectMetas}, ContentControl.ActualHeight={contentControlEditor.ActualHeight:0.#}px");
                        if (rectMetas.HasValue && rectMetas.Value.Bottom > contentControlEditor.ActualHeight + 1)
                            Console.WriteLine($"FALLO: COFRES-INSPECTOR-P2 - el WrapPanel 'Metas' (pestañas Biblioteca/Positivos/Negativos) termina en Y={rectMetas.Value.Bottom:0.#}px, MAS ALLA de los {contentControlEditor.ActualHeight:0.#}px reales del ContentControl (MaxHeight=320, MainWindow.xaml:1882) - coincide con el corte de 'Bibliot.../Positivos' señalado en imagen5.png. Alcanzable con scroll interno (ScrollViewer de ItemEditTemplate, MainWindow.xaml:33) pero no visible sin desplazarse.");
                    }
                    if (groupsList != null && contentControlEditor != null)
                    {
                        Rect? rectGroups = null;
                        try
                        {
                            var esquina = groupsList.TransformToAncestor(contentControlEditor).Transform(new Point(0, 0));
                            rectGroups = new Rect(esquina, new Size(groupsList.ActualWidth, groupsList.ActualHeight));
                        }
                        catch (InvalidOperationException) { }
                        Console.WriteLine($"COFRES-INSPECTOR-P2: WrapPanel 'Groups' (Mejor/Daño/Critico, etc.) posicion real relativa al ContentControl={rectGroups}");
                        if (rectGroups.HasValue && rectGroups.Value.Bottom > contentControlEditor.ActualHeight + 1)
                            Console.WriteLine($"FALLO: COFRES-INSPECTOR-P2 - el WrapPanel 'Groups' termina en Y={rectGroups.Value.Bottom:0.#}px, MAS ALLA de los {contentControlEditor.ActualHeight:0.#}px del ContentControl (MaxHeight=320)");
                    }
                }

                vm.Exploration.CancelEditingChestCommand.Execute(null);
                DoEvents();
            }

            // ================= Evidencia visual real (aplicador-fix, 25-sep-2026) =================
            // Capturas reales (RenderTargetBitmap, mismo patron que AuditoriaTransicion.cs) de los
            // 3 modos de busqueda de cofres visibles en las capturas del usuario (imagen5="Cofre a
            // cofre", imagen6="Por tipo de cofre"; "Por lo que contienen" comparte la MISMA causa de
            // codigo, confirmado aqui con el mismo dato real) + del editor con el caso mas exigente
            // real: un arma Picaro de Calamity con sus 17 prefijos legales reales
            // (Assets/calamity/rogue_prefixes.json, "weapon": 17) - confirma que Groups/Prefixes
            // siguen siendo descubribles a simple vista tras quitar el MaxHeight=320, sin depender
            // del ScrollViewer interno oculto (ItemEditTemplate linea ~33).
            try
            {
                vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                vm.Exploration.ChestViewMode = 2;
                DoEvents(); DoEvents(); window.UpdateLayout();
                CapturaVentanaKeepQa(window, "cofres-p3-cofre-a-cofre");

                vm.Exploration.ChestViewMode = 0;
                DoEvents(); DoEvents(); window.UpdateLayout();
                CapturaVentanaKeepQa(window, "cofres-p3-por-tipo");

                vm.Exploration.ChestViewMode = 1;
                DoEvents(); DoEvents(); window.UpdateLayout();
                // ADR-TERRAKEEP-029: reusa browseViewHost (FindName doble) ya obtenido arriba.
                var bloqueResultadosPorContenido = browseViewHost?.FindName("ExplorationResultsBlock") as FrameworkElement;
                Console.WriteLine($"COFRES-INSPECTOR-P3: modo 'Por lo que contienen' (misma causa, sin buscar aun) -> WorldSearchResults.Count={vm.Exploration.WorldSearchResults.Count} (esperado 0), ExplorationResultsBlock.Visibility={bloqueResultadosPorContenido?.Visibility}, ActualHeight real medido={bloqueResultadosPorContenido?.ActualHeight:0.#}px");
                if (vm.Exploration.WorldSearchResults.Count == 0 && bloqueResultadosPorContenido?.Visibility == Visibility.Visible && bloqueResultadosPorContenido.ActualHeight > 40)
                    Console.WriteLine($"FALLO: COFRES-INSPECTOR-P3 - mismo hueco vacio real en 'Por lo que contienen' antes de buscar ({bloqueResultadosPorContenido.ActualHeight:0.#}px)");
                CapturaVentanaKeepQa(window, "cofres-p3-por-lo-que-contienen");

                vm.Exploration.ChestViewMode = 2;
                DoEvents(); DoEvents();

                // Arma Picaro real de Calamity (RogueDamageClass.Instance en catalog.json real,
                // 149 armas comparten el mismo catalogo de 17 prefijos legales) - localizada por la
                // Libreria (mismo camino real que usaria el usuario), no un id inventado.
                vm.Library.SearchText = "Hacha Arrojadiza de Adamantita";
                WaitForDispatcher(300); // L-c: debounce real de 180ms antes de que Results se rellene (Program.cs)
                var picaro = vm.Library.Results.FirstOrDefault();
                vm.Library.SearchText = string.Empty;
                DoEvents();

                if (picaro == null)
                    Console.WriteLine("COFRES-INSPECTOR-P2-PICARO: AVISO - no se encontro el arma Picaro de prueba en la Libreria (¿cambio el nombre real?) - se omite la captura del caso de 17 prefijos");
                else
                {
                    var filaParaPicaro = vm.Exploration.ChestRows.FirstOrDefault(r => r.Items.Count > 0);
                    if (filaParaPicaro == null)
                        Console.WriteLine("COFRES-INSPECTOR-P2-PICARO: AVISO - ningun cofre real tiene contenido para alojar el Picaro de prueba");
                    else
                    {
                        vm.Exploration.EditChestCommand.Execute(filaParaPicaro);
                        DoEvents(); DoEvents();
                        var slotDestino = vm.Exploration.EditingChestSlots.FirstOrDefault();
                        if (slotDestino == null)
                            Console.WriteLine("COFRES-INSPECTOR-P2-PICARO: AVISO - el cofre elegido no tiene ningun slot real editable");
                        else
                        {
                            vm.Exploration.SelectChestSlot(slotDestino);
                            DoEvents();
                            slotDestino.ItemId = picaro.Id; // mismo camino real que escribir el id a mano en el TextBox (OnItemIdChanged -> PlaceItem)
                            DoEvents(); DoEvents(); window.UpdateLayout();

                            Console.WriteLine($"COFRES-INSPECTOR-P2-PICARO: objeto real colocado='{picaro.DisplayName}' (Id={picaro.Id}), CanHavePrefix={vm.Exploration.ChestItemEdit.CanHavePrefix}, Groups.Count={vm.Exploration.ChestItemEdit.Groups.Count}, Prefixes.Count={vm.Exploration.ChestItemEdit.Prefixes.Count} (esperado 17 prefijos legales reales, rogue_prefixes.json 'weapon':17)");

                            var contentControlPicaro = Descendientes<ContentControl>(window).FirstOrDefault(cc =>
                                ReferenceEquals(cc.Content, vm.Exploration.ChestItemEdit) && cc.ContentTemplate != null);
                            var metasPicaro = Descendientes<ItemsControl>(window).FirstOrDefault(ic =>
                                ReferenceEquals(ic.ItemsSource, vm.Exploration.ChestItemEdit.Metas));
                            var groupsPicaro = Descendientes<ItemsControl>(window).FirstOrDefault(ic =>
                                ReferenceEquals(ic.ItemsSource, vm.Exploration.ChestItemEdit.Groups));
                            if (contentControlPicaro != null && metasPicaro != null && groupsPicaro != null)
                            {
                                var rectMetasPicaro = new Rect(metasPicaro.TransformToAncestor(contentControlPicaro).Transform(new Point(0, 0)), new Size(metasPicaro.ActualWidth, metasPicaro.ActualHeight));
                                var rectGroupsPicaro = new Rect(groupsPicaro.TransformToAncestor(contentControlPicaro).Transform(new Point(0, 0)), new Size(groupsPicaro.ActualWidth, groupsPicaro.ActualHeight));
                                Console.WriteLine($"COFRES-INSPECTOR-P2-PICARO: ContentControl.ActualHeight={contentControlPicaro.ActualHeight:0.#}px, Metas.Bottom={rectMetasPicaro.Bottom:0.#}px, Groups.Bottom={rectGroupsPicaro.Bottom:0.#}px (esperado: los dos DENTRO o el ContentControl ya crecio para alojarlos - sin corte a media altura)");
                                if (rectMetasPicaro.Bottom > contentControlPicaro.ActualHeight + 1 || rectGroupsPicaro.Bottom > contentControlPicaro.ActualHeight + 1)
                                    Console.WriteLine("FALLO: COFRES-INSPECTOR-P2-PICARO - con el caso real mas exigente (17 prefijos), Metas/Groups siguen cortandose");

                                // Desplaza el ScrollViewer real de la barra lateral (3 niveles
                                // anidados, ver comentario de arriba) hasta que Groups quede dentro
                                // del viewport, para que la captura de pantalla muestre de verdad el
                                // caso sin cortes - no solo lo confirme la geometria numerica.
                                groupsPicaro.BringIntoView();
                                DoEvents(); DoEvents(); window.UpdateLayout();
                            }

                            CapturaVentanaKeepQa(window, "cofres-p2-picaro-17-prefijos");
                        }
                        vm.Exploration.CancelEditingChestCommand.Execute(null);
                        DoEvents();
                    }
                }
            }
            catch (Exception exVisual) { Console.WriteLine("COFRES-INSPECTOR-VISUAL-EXCEPTION: " + exVisual); }

            // ================= ExploracionRediseno Fase B: ExplorationSidebarMode (25-sep-2026) =================
            // Canario del aplicador-fix (patron de 2 fases) - modelo de estado del sidebar,
            // fundacional para el rediseno estructural posterior (ver bitacora.md, gap analysis
            // "ExploracionRediseno FaseA"). SidebarMode se mantiene SOLO desde
            // OnEditingChestChanged (ExplorationViewModel.cs) - confirma las 3 rutas REALES de
            // apertura (boton "Editar" de la fila, marcador del mapa via TryOpenChestAtTile,
            // resultado de busqueda via GoToWorldSearchHitCommand->OpenChestEditorIfApplicable,
            // exactamente los mismos 3 caminos ya verificados por AR-13d/AR-13e mas arriba en
            // Program.cs) y las 2 de cierre (Guardar, Cancelar). Fase B es SOLO ViewModel: no hay
            // ningun binding en XAML que consuma SidebarMode todavia, por eso no hace falta medir
            // nada visual aqui.
            try
            {
                vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                vm.Exploration.ChestViewMode = 2; // Cofre a cofre - mismo estado que el resto del cluster
                DoEvents(); DoEvents();

                Console.WriteLine($"COFRES-INSPECTOR-FASEB: SidebarMode antes de abrir ningun cofre={vm.Exploration.SidebarMode} (esperado Browse)");
                if (vm.Exploration.SidebarMode != ExplorationSidebarMode.Browse)
                    Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEB - SidebarMode no arranca en Browse");

                // --- Ruta 1: boton "Editar" de la fila (EditChestCommand), cierre por Cancelar ---
                var filaFaseB = vm.Exploration.ChestRows.FirstOrDefault();
                if (filaFaseB == null)
                    Console.WriteLine("COFRES-INSPECTOR-FASEB-R1: AVISO - no hay ninguna fila de cofre real para probar la ruta 1");
                else
                {
                    vm.Exploration.EditChestCommand.Execute(filaFaseB);
                    DoEvents(); DoEvents();
                    Console.WriteLine($"COFRES-INSPECTOR-FASEB-R1: tras EditChestCommand (fila 'Editar') -> SidebarMode={vm.Exploration.SidebarMode} (esperado ChestInspector)");
                    if (vm.Exploration.SidebarMode != ExplorationSidebarMode.ChestInspector)
                        Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEB-R1 - abrir un cofre desde la fila 'Editar' no pone SidebarMode=ChestInspector");

                    vm.Exploration.CancelEditingChestCommand.Execute(null);
                    DoEvents(); DoEvents();
                    Console.WriteLine($"COFRES-INSPECTOR-FASEB-R1: tras CancelEditingChestCommand -> SidebarMode={vm.Exploration.SidebarMode} (esperado Browse)");
                    if (vm.Exploration.SidebarMode != ExplorationSidebarMode.Browse)
                        Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEB-R1 - CancelEditingChest no vuelve SidebarMode a Browse");
                }

                // --- Ruta 2: marcador del mapa (TryOpenChestAtTile), mismo cofre-7 real que AR-13d/AR-13e
                // (X=5579,Y=1036, Blando_Río.wld) - cierre por Guardar (SaveEditingChestAsync) esta vez,
                // para cubrir tambien esa ruta de salida (Ruta 1 ya cubrio Cancelar).
                bool abrioCofre7 = vm.Exploration.TryOpenChestAtTile(5579, 1036);
                DoEvents(); DoEvents();
                Console.WriteLine($"COFRES-INSPECTOR-FASEB-R2: TryOpenChestAtTile(5579,1036) -> encontro cofre={abrioCofre7}, SidebarMode={vm.Exploration.SidebarMode} (esperado True/ChestInspector)");
                if (!abrioCofre7 || vm.Exploration.SidebarMode != ExplorationSidebarMode.ChestInspector)
                    Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEB-R2 - TryOpenChestAtTile no pone SidebarMode=ChestInspector");

                var tareaGuardarFaseB = vm.Exploration.SaveEditingChestCommand.ExecuteAsync(null);
                while (!tareaGuardarFaseB.IsCompleted) DoEvents();
                DoEvents(); DoEvents();
                Console.WriteLine($"COFRES-INSPECTOR-FASEB-R2: tras SaveEditingChestAsync -> SidebarMode={vm.Exploration.SidebarMode} (esperado Browse), EditingChest={(vm.Exploration.EditingChest == null ? "null" : "NO-NULL")}");
                if (vm.Exploration.SidebarMode != ExplorationSidebarMode.Browse)
                    Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEB-R2 - SaveEditingChestAsync no vuelve SidebarMode a Browse");

                // --- Ruta 3: resultado de busqueda (GoToWorldSearchHitCommand -> OpenChestEditorIfApplicable),
                // mismo hit real ya usado por AR-13e (centro del cofre-7, (5580,1037)) - cierre por Cancelar.
                var hitFaseB = new WorldSearchHitRowViewModel(new WorldSearchHit(5580, 1037, "Barra de hierro", WorldSearchKind.ChestItem));
                vm.Exploration.GoToWorldSearchHitCommand.Execute(hitFaseB);
                DoEvents(); DoEvents();
                Console.WriteLine($"COFRES-INSPECTOR-FASEB-R3: tras GoToWorldSearchHitCommand (resultado ChestItem) -> SidebarMode={vm.Exploration.SidebarMode} (esperado ChestInspector), EditingChest=({vm.Exploration.EditingChest?.TileX},{vm.Exploration.EditingChest?.TileY}) (esperado (5579, 1036))");
                if (vm.Exploration.SidebarMode != ExplorationSidebarMode.ChestInspector)
                    Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEB-R3 - un resultado de busqueda ChestItem no pone SidebarMode=ChestInspector");

                vm.Exploration.CancelEditingChestCommand.Execute(null);
                DoEvents(); DoEvents();
                Console.WriteLine($"COFRES-INSPECTOR-FASEB-R3: tras CancelEditingChestCommand -> SidebarMode={vm.Exploration.SidebarMode} (esperado Browse)");
                if (vm.Exploration.SidebarMode != ExplorationSidebarMode.Browse)
                    Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEB-R3 - CancelEditingChest no vuelve SidebarMode a Browse tras la ruta de busqueda");
            }
            catch (Exception exFaseB) { Console.WriteLine("COFRES-INSPECTOR-FASEB-EXCEPTION: " + exFaseB); }

            // ================= ExploracionRediseno Fase C: separar Browse/Inspector en XAML (25-sep-2026) =================
            // Canario del aplicador-fix - confirma que el Grid nuevo (ExplorationSidebarBrowseInspectorHost,
            // MainWindow.xaml) con sus 2 hijos superpuestos (DockPanel Browse / Grid placeholder
            // ChestInspector) responde de verdad a SidebarMode (Fase B, ya verificado arriba) via el
            // converter EnumEqualsToVis, y que el contenido de Browse (WrapPanel de categorias,
            // buscador, ExplorationCategoryContent, ExplorationResultsBlock) sigue viendose IGUAL que
            // antes de este cambio - el envoltorio nuevo (Grid+DockPanel, sin margen/padding propios)
            // es puramente estructural, nunca deberia cambiar ni un pixel del contenido de Browse.
            // Capturas reales (RenderTargetBitmap) de las 4 categorias reales pedidas por el encargo
            // (Cofres/Minerales/Objetos/NPCs) + la de abrir un cofre real (debe verse el placeholder
            // temporal, no el editor incrustado de ChestRowTemplate - comportamiento ESPERADO de esta
            // fase, Fase D lo sustituye).
            try
            {
                string outDirFaseC = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
                Directory.CreateDirectory(outDirFaseC);

                var hostFaseC = window.FindName("ExplorationSidebarBrowseInspectorHost") as FrameworkElement;
                // ADR-TERRAKEEP-029: reusa browseViewHost (FindName doble) ya obtenido arriba.
                var browseFaseC = browseViewHost?.FindName("ExplorationSidebarBrowseContent") as FrameworkElement;
                // ADR-TERRAKEEP-028 (26-sep-2026): ChestInspector se movio a ChestInspectorView.xaml
                // (UserControl con su propio NameScope) - FindName DOBLE, mismo patron ya usado por
                // GUIA/ADR-021, Compare/ADR-025 y WorldTools/ADR-027.
                var placeholderFaseC = (window.FindName("ChestInspectorView") as FrameworkElement)?.FindName("ExplorationSidebarChestInspectorPlaceholder") as FrameworkElement;
                var wrapCategoriasFaseC = Descendientes<WrapPanel>(window).FirstOrDefault();
                var contenidoCatFaseC = browseViewHost?.FindName("ExplorationCategoryContent") as FrameworkElement;
                var bloqueResFaseC = browseViewHost?.FindName("ExplorationResultsBlock") as FrameworkElement;
                if (hostFaseC == null || browseFaseC == null || placeholderFaseC == null)
                    Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEC - no se encuentra ExplorationSidebarBrowseInspectorHost/ExplorationSidebarBrowseContent/ExplorationSidebarChestInspectorPlaceholder en el arbol visual (MainWindow.xaml)");
                else
                {
                    vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                    vm.Exploration.ChestViewMode = 0; // "Por tipo" - Browse completo (WrapPanel+buscador+categorias) visible en la captura
                    DoEvents(); DoEvents();

                    Console.WriteLine($"COFRES-INSPECTOR-FASEC: SidebarMode={vm.Exploration.SidebarMode} (esperado Browse) -> BrowseContent.Visibility={browseFaseC.Visibility} (esperado Visible), Placeholder.Visibility={placeholderFaseC.Visibility} (esperado Collapsed)");
                    if (vm.Exploration.SidebarMode != ExplorationSidebarMode.Browse || browseFaseC.Visibility != Visibility.Visible || placeholderFaseC.Visibility != Visibility.Collapsed)
                        Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEC - en modo Browse, BrowseContent deberia ser Visible y el placeholder Collapsed");
                    if (wrapCategoriasFaseC == null || contenidoCatFaseC == null || bloqueResFaseC == null || !wrapCategoriasFaseC.IsVisible || !contenidoCatFaseC.IsVisible)
                        Console.WriteLine($"FALLO: COFRES-INSPECTOR-FASEC - el contenido real de Browse (WrapPanel categorias/ExplorationCategoryContent) no esta visible dentro del envoltorio nuevo (wrapCategorias={wrapCategoriasFaseC != null}, IsVisible={wrapCategoriasFaseC?.IsVisible}, contenidoCat.IsVisible={contenidoCatFaseC?.IsVisible})");

                    // Capturas reales de las 4 categorias del encargo - pixel-identicas a antes de este
                    // cambio porque el envoltorio nuevo no añade margen/padding/tamaño propio (mismo
                    // Grid de reparto, mismo DockPanel.Dock="Top", solo un nivel mas anidado).
                    foreach (var (cat, chestMode, etiqueta) in new (WorldSearchCategory, int?, string)[]
                             { (WorldSearchCategory.Chests, 0, "cofres"), (WorldSearchCategory.Ores, null, "minerales"), (WorldSearchCategory.Objects, null, "objetos"), (WorldSearchCategory.Npcs, null, "npcs") })
                    {
                        vm.Exploration.SelectedCategory = cat;
                        if (chestMode.HasValue) vm.Exploration.ChestViewMode = chestMode.Value;
                        DoEvents(); DoEvents();
                        var rtbFaseC = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtbFaseC.Render(window);
                        var encFaseC = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encFaseC.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbFaseC));
                        string rutaFaseC = Path.Combine(outDirFaseC, $"fasec-browse-{etiqueta}.png");
                        using (var fsFaseC = File.Create(rutaFaseC)) encFaseC.Save(fsFaseC);
                        Console.WriteLine($"COFRES-INSPECTOR-FASEC: captura real Browse/{etiqueta} -> {rutaFaseC}");
                    }

                    // Abrir un cofre real (ruta 1, boton "Editar") - ahora DEBE mostrar el placeholder,
                    // NO el editor incrustado (comportamiento ESPERADO de esta fase, ver comentario del
                    // XAML). Vuelve a dejar ChestViewMode=2 para que ChestRows tenga filas reales.
                    vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                    vm.Exploration.ChestViewMode = 2;
                    DoEvents(); DoEvents();
                    var filaFaseC = vm.Exploration.ChestRows.FirstOrDefault();
                    if (filaFaseC == null)
                        Console.WriteLine("COFRES-INSPECTOR-FASEC: AVISO - no hay fila de cofre real para probar la captura del placeholder");
                    else
                    {
                        vm.Exploration.EditChestCommand.Execute(filaFaseC);
                        DoEvents(); DoEvents();

                        Console.WriteLine($"COFRES-INSPECTOR-FASEC: cofre abierto -> SidebarMode={vm.Exploration.SidebarMode} (esperado ChestInspector), BrowseContent.Visibility={browseFaseC.Visibility} (esperado Collapsed), Placeholder.Visibility={placeholderFaseC.Visibility} (esperado Visible)");
                        if (vm.Exploration.SidebarMode != ExplorationSidebarMode.ChestInspector || browseFaseC.Visibility != Visibility.Collapsed || placeholderFaseC.Visibility != Visibility.Visible)
                            Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEC - al abrir un cofre, BrowseContent deberia colapsarse y el placeholder hacerse Visible");

                        var textoPlaceholder = Descendientes<TextBlock>(placeholderFaseC).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Text));
                        Console.WriteLine($"COFRES-INSPECTOR-FASEC: texto real del placeholder='{textoPlaceholder?.Text}' (esperado no vacio, Loc[explore_inspector_placeholder])");
                        if (textoPlaceholder == null)
                            Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEC - el placeholder no tiene ningun texto real visible");

                        var rtbInspFaseC = new System.Windows.Media.Imaging.RenderTargetBitmap(
                            (int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtbInspFaseC.Render(window);
                        var encInspFaseC = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encInspFaseC.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbInspFaseC));
                        string rutaInspFaseC = Path.Combine(outDirFaseC, "fasec-inspector-placeholder.png");
                        using (var fsInspFaseC = File.Create(rutaInspFaseC)) encInspFaseC.Save(fsInspFaseC);
                        Console.WriteLine($"COFRES-INSPECTOR-FASEC: captura real del placeholder abierto -> {rutaInspFaseC}");

                        vm.Exploration.CancelEditingChestCommand.Execute(null);
                        DoEvents(); DoEvents();
                        Console.WriteLine($"COFRES-INSPECTOR-FASEC: tras Cancelar -> SidebarMode={vm.Exploration.SidebarMode} (esperado Browse), BrowseContent.Visibility={browseFaseC.Visibility} (esperado Visible), Placeholder.Visibility={placeholderFaseC.Visibility} (esperado Collapsed)");
                        if (vm.Exploration.SidebarMode != ExplorationSidebarMode.Browse || browseFaseC.Visibility != Visibility.Visible || placeholderFaseC.Visibility != Visibility.Collapsed)
                            Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEC - tras cerrar el cofre, BrowseContent deberia volver a Visible y el placeholder a Collapsed");
                    }
                }
            }
            catch (Exception exFaseC) { Console.WriteLine("COFRES-INSPECTOR-FASEC-EXCEPTION: " + exFaseC); }

            // ================= ExploracionRediseno Fase D: editor real movido al Inspector (25-sep-2026) =================
            // Canario del aplicador-fix - Fase D saca el editor real de ChestRowTemplate y rellena el
            // placeholder de Fase C. Reubica exactamente la misma medicion de Metas/Groups del punto 2
            // (mas arriba, "COFRES-INSPECTOR-P2") pero ahora sobre el ANCHO COMPLETO del sidebar (antes
            // vivia indentado 33px dentro de una fila de ListBox), con el mismo objeto de 17 prefijos
            // reales ("Hacha Arrojadiza de Adamantita", RogueDamageClass, rogue_prefixes.json
            // "weapon":17) ya usado en la ronda anterior. Confirma tambien que ChestRowTemplate YA NO
            // tiene ningun Border condicionado a IsEditing (el bloque se movio, no se duplico) y que
            // ItemEditTemplate/ChestItemEdit siguen siendo la MISMA instancia de recurso/propiedad que
            // usa Personaje (nunca duplicados).
            try
            {
                // ADR-TERRAKEEP-028: FindName DOBLE, ChestInspectorView tiene su propio NameScope.
                var placeholderFaseD = (window.FindName("ChestInspectorView") as FrameworkElement)?.FindName("ExplorationSidebarChestInspectorPlaceholder") as FrameworkElement;
                // ADR-TERRAKEEP-029: reusa browseViewHost (FindName doble) ya obtenido arriba.
                var browseFaseD = browseViewHost?.FindName("ExplorationSidebarBrowseContent") as FrameworkElement;
                if (placeholderFaseD == null || browseFaseD == null)
                    Console.WriteLine("FALLO: COFRES-INSPECTOR-FASED - no se encuentra ExplorationSidebarChestInspectorPlaceholder/ExplorationSidebarBrowseContent en el arbol visual");
                else
                {
                    vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                    vm.Exploration.ChestViewMode = 2; // Cofre a cofre
                    DoEvents(); DoEvents();

                    // ---- No-duplicacion: ChestRowTemplate ya no tiene ningun Border IsEditing ----
                    // Antes de abrir nada, confirma leyendo el arbol de la propia lista de filas que
                    // ningun descendiente de ChestByChestList tiene un binding Content a ChestItemEdit
                    // (si lo tuviera, seria el editor DUPLICADO reintroducido por error).
                    var listaFilasFaseD = browseViewHost?.FindName("ChestByChestList") as ItemsControl;
                    bool editorDuplicadoEnFila = listaFilasFaseD != null && Descendientes<ContentControl>(listaFilasFaseD).Any(cc =>
                        (BindingOperations.GetBindingExpression(cc, ContentControl.ContentProperty)?.ParentBinding?.Path?.Path)?.Contains("ChestItemEdit") == true);
                    Console.WriteLine($"COFRES-INSPECTOR-FASED: ChestByChestList encontrada={listaFilasFaseD != null}, ContentControl->ChestItemEdit DUPLICADO dentro de la fila={editorDuplicadoEnFila} (esperado False)");
                    if (editorDuplicadoEnFila)
                        Console.WriteLine("FALLO: COFRES-INSPECTOR-FASED - ChestRowTemplate todavia tiene un ContentControl propio hacia ChestItemEdit - el editor se duplico en vez de moverse");

                    // ---- Ruta 1: boton "Editar" de la fila -> Inspector real relleno ----
                    var filaFaseD = vm.Exploration.ChestRows.FirstOrDefault(r => r.Items.Count > 0) ?? vm.Exploration.ChestRows.FirstOrDefault();
                    if (filaFaseD == null)
                        Console.WriteLine("COFRES-INSPECTOR-FASED: AVISO - no hay fila de cofre real para abrir el Inspector");
                    else
                    {
                        vm.Exploration.EditChestCommand.Execute(filaFaseD);
                        DoEvents(); DoEvents(); window.UpdateLayout();

                        Console.WriteLine($"COFRES-INSPECTOR-FASED-R1: SidebarMode={vm.Exploration.SidebarMode} (esperado ChestInspector), Placeholder.Visibility={placeholderFaseD.Visibility} (esperado Visible)");
                        if (vm.Exploration.SidebarMode != ExplorationSidebarMode.ChestInspector || placeholderFaseD.Visibility != Visibility.Visible)
                            Console.WriteLine("FALLO: COFRES-INSPECTOR-FASED-R1 - abrir un cofre desde la fila 'Editar' no deja el Inspector real visible");

                        // Cabecera real: boton "<- Cofres" + titulo dinamico (VariantName/coords).
                        var botonVolverFaseD = Descendientes<Button>(placeholderFaseD).FirstOrDefault(b =>
                            (BindingOperations.GetBindingExpression(b, Button.CommandProperty)?.ParentBinding?.Path?.Path) == "Exploration.CancelEditingChestCommand");
                        var contentControlFaseD = Descendientes<ContentControl>(placeholderFaseD).FirstOrDefault(cc =>
                            ReferenceEquals(cc.Content, vm.Exploration.ChestItemEdit) && cc.ContentTemplate != null);
                        var slotsFaseD = Descendientes<ItemsControl>(placeholderFaseD).FirstOrDefault(ic =>
                            ReferenceEquals(ic.ItemsSource, vm.Exploration.EditingChestSlots));
                        Console.WriteLine($"COFRES-INSPECTOR-FASED-R1: boton '<- Cofres' encontrado={botonVolverFaseD != null}, IsVisible={botonVolverFaseD?.IsVisible}, ContentControl(ItemEditTemplate) encontrado={contentControlFaseD != null}, rejilla de slots encontrada={slotsFaseD != null}, ancho real del ContentControl={contentControlFaseD?.ActualWidth:0.#}px, ancho real del Placeholder={placeholderFaseD.ActualWidth:0.#}px");
                        if (botonVolverFaseD == null || !botonVolverFaseD.IsVisible)
                            Console.WriteLine("FALLO: COFRES-INSPECTOR-FASED-R1 - no se encuentra el boton real '<- Cofres' (CancelEditingChestCommand) en la cabecera del Inspector");
                        if (contentControlFaseD == null || slotsFaseD == null)
                            Console.WriteLine("FALLO: COFRES-INSPECTOR-FASED-R1 - el Inspector no tiene ya el ContentControl real de ItemEditTemplate y/o la rejilla real de slots (EditingChestSlots) - el bloque no se movio de verdad");

                        var tituloFaseD = Descendientes<TextBlock>(placeholderFaseD).FirstOrDefault(t => t.Text == filaFaseD.VariantName);
                        Console.WriteLine($"COFRES-INSPECTOR-FASED-R1: titulo dinamico encontrado (Text==VariantName '{filaFaseD.VariantName}')={tituloFaseD != null}");
                        if (tituloFaseD == null)
                            Console.WriteLine("FALLO: COFRES-INSPECTOR-FASED-R1 - la cabecera del Inspector no muestra el VariantName real del cofre abierto");

                        // Ancho completo: el Placeholder debe ocupar el MISMO ancho real de contenido
                        // que ya usa Browse en este sidebar (documentado en bitacora Fase C: "230-245px
                        // de contenido de categoria" a tamaño de ventana por defecto, 320px de columna
                        // menos el Padding=10 de la tarjeta flotante y la barra de scroll vertical real)
                        // - NUNCA el ~185-196px que dejaba vivir el editor indentado 33px + Padding=8
                        // dentro de una fila de ListBox (Margin="33,0,..." de ChestRowTemplate, ya
                        // eliminado). Umbral real: >=220px (por debajo de eso volveria a ser el ancho
                        // estrecho de antes, no "ancho completo").
                        if (placeholderFaseD.ActualWidth < 220)
                            Console.WriteLine($"FALLO: COFRES-INSPECTOR-FASED-R1 - el Inspector mide solo {placeholderFaseD.ActualWidth:0.#}px de ancho real, por debajo del ancho real de contenido de Browse en este mismo sidebar (230-245px) - no parece estar aprovechando el ancho completo");

                        vm.Exploration.CancelEditingChestCommand.Execute(null);
                        DoEvents(); DoEvents();
                        Console.WriteLine($"COFRES-INSPECTOR-FASED-R1: tras boton '<- Cofres' (CancelEditingChestCommand) -> SidebarMode={vm.Exploration.SidebarMode} (esperado Browse)");
                        if (vm.Exploration.SidebarMode != ExplorationSidebarMode.Browse)
                            Console.WriteLine("FALLO: COFRES-INSPECTOR-FASED-R1 - el boton '<- Cofres' no vuelve SidebarMode a Browse");
                    }

                    // ---- Reubica la medicion de Metas/Groups (punto 2) al Inspector con el objeto de 17 prefijos ----
                    vm.Library.SearchText = "Hacha Arrojadiza de Adamantita";
                    WaitForDispatcher(300);
                    var picaroFaseD = vm.Library.Results.FirstOrDefault();
                    vm.Library.SearchText = string.Empty;
                    DoEvents();

                    if (picaroFaseD == null)
                        Console.WriteLine("COFRES-INSPECTOR-FASED-PICARO: AVISO - no se encontro el arma Picaro de prueba en la Libreria - se omite la remedicion");
                    else
                    {
                        var filaParaPicaroFaseD = vm.Exploration.ChestRows.FirstOrDefault(r => r.Items.Count > 0);
                        if (filaParaPicaroFaseD == null)
                            Console.WriteLine("COFRES-INSPECTOR-FASED-PICARO: AVISO - ningun cofre real tiene contenido para alojar el Picaro de prueba");
                        else
                        {
                            vm.Exploration.EditChestCommand.Execute(filaParaPicaroFaseD);
                            DoEvents(); DoEvents();
                            var slotDestinoFaseD = vm.Exploration.EditingChestSlots.FirstOrDefault();
                            if (slotDestinoFaseD == null)
                                Console.WriteLine("COFRES-INSPECTOR-FASED-PICARO: AVISO - el cofre elegido no tiene ningun slot real editable");
                            else
                            {
                                vm.Exploration.SelectChestSlot(slotDestinoFaseD);
                                DoEvents();
                                slotDestinoFaseD.ItemId = picaroFaseD.Id;
                                DoEvents(); DoEvents(); window.UpdateLayout();

                                Console.WriteLine($"COFRES-INSPECTOR-FASED-PICARO: objeto real colocado='{picaroFaseD.DisplayName}' (Id={picaroFaseD.Id}), CanHavePrefix={vm.Exploration.ChestItemEdit.CanHavePrefix}, Groups.Count={vm.Exploration.ChestItemEdit.Groups.Count} (esperado 17 prefijos legales reales)");

                                // Captura de la parte de ARRIBA del Inspector (cabecera "<- Cofres" +
                                // titulo dinamico + rejilla de slots). El bloque COFRES-INSPECTOR-P2-
                                // PICARO de mas arriba (investigacion original, antes de Fase C/D) ya
                                // hizo su propio BringIntoView sobre Groups sobre esta MISMA instancia
                                // real del editor (ahora vive en el Inspector) - el ScrollViewer
                                // exterior (ExplorationSidebarScroll) queda con ese scroll aplicado, asi
                                // que hace falta volver arriba de verdad antes de esta captura o se
                                // repetiria el mismo recorte que motivo esta fase.
                                if (window.FindName("ExplorationSidebarScroll") is ScrollViewer scrollInspectorFaseD)
                                {
                                    scrollInspectorFaseD.ScrollToTop();
                                    DoEvents(); DoEvents(); window.UpdateLayout();
                                }
                                string rutaFaseDArriba = CapturaVentanaKeepQa(window, "fased-inspector-cabecera-y-rejilla");
                                Console.WriteLine($"COFRES-INSPECTOR-FASED: captura real de la cabecera+rejilla del Inspector (sin scroll forzado) -> {rutaFaseDArriba}");

                                var contentControlPicaroFaseD = Descendientes<ContentControl>(window).FirstOrDefault(cc =>
                                    ReferenceEquals(cc.Content, vm.Exploration.ChestItemEdit) && cc.ContentTemplate != null);
                                var metasPicaroFaseD = Descendientes<ItemsControl>(window).FirstOrDefault(ic =>
                                    ReferenceEquals(ic.ItemsSource, vm.Exploration.ChestItemEdit.Metas));
                                var groupsPicaroFaseD = Descendientes<ItemsControl>(window).FirstOrDefault(ic =>
                                    ReferenceEquals(ic.ItemsSource, vm.Exploration.ChestItemEdit.Groups));
                                if (contentControlPicaroFaseD != null && metasPicaroFaseD != null && groupsPicaroFaseD != null)
                                {
                                    var rectMetasFaseD = new Rect(metasPicaroFaseD.TransformToAncestor(contentControlPicaroFaseD).Transform(new Point(0, 0)), new Size(metasPicaroFaseD.ActualWidth, metasPicaroFaseD.ActualHeight));
                                    var rectGroupsFaseD = new Rect(groupsPicaroFaseD.TransformToAncestor(contentControlPicaroFaseD).Transform(new Point(0, 0)), new Size(groupsPicaroFaseD.ActualWidth, groupsPicaroFaseD.ActualHeight));
                                    Console.WriteLine($"COFRES-INSPECTOR-FASED-PICARO: ContentControl.ActualWidth={contentControlPicaroFaseD.ActualWidth:0.#}px (ancho completo del Inspector, antes ~250px indentado en la fila), ContentControl.ActualHeight={contentControlPicaroFaseD.ActualHeight:0.#}px, Metas.Bottom={rectMetasFaseD.Bottom:0.#}px, Groups.Bottom={rectGroupsFaseD.Bottom:0.#}px (esperado: los dos DENTRO del ContentControl, sin corte)");
                                    if (rectMetasFaseD.Bottom > contentControlPicaroFaseD.ActualHeight + 1 || rectGroupsFaseD.Bottom > contentControlPicaroFaseD.ActualHeight + 1)
                                        Console.WriteLine("FALLO: COFRES-INSPECTOR-FASED-PICARO - con el caso real mas exigente (17 prefijos) YA en el Inspector de ancho completo, Metas/Groups siguen cortandose");

                                    groupsPicaroFaseD.BringIntoView();
                                    DoEvents(); DoEvents(); window.UpdateLayout();
                                }

                                string rutaFaseD = CapturaVentanaKeepQa(window, "fased-inspector-completo-picaro");
                                Console.WriteLine($"COFRES-INSPECTOR-FASED: captura real del Inspector con Metas/Groups (17 prefijos, objeto real) -> {rutaFaseD}");

                                // Tercera captura: baja del todo el ScrollViewer exterior para dejar
                                // constancia real de la lista de Prefixes (los 17 prefijos legales
                                // individuales) y de los botones Guardar/Cancelar, ambos por debajo del
                                // viewport en las 2 capturas anteriores.
                                if (window.FindName("ExplorationSidebarScroll") is ScrollViewer scrollInspectorFaseD2)
                                {
                                    Console.WriteLine($"COFRES-INSPECTOR-FASED-SCROLLDEBUG: antes de ScrollToBottom -> VerticalOffset={scrollInspectorFaseD2.VerticalOffset:0.#}, ScrollableHeight={scrollInspectorFaseD2.ScrollableHeight:0.#}, ExtentHeight={scrollInspectorFaseD2.ExtentHeight:0.#}, ViewportHeight={scrollInspectorFaseD2.ViewportHeight:0.#}");
                                    scrollInspectorFaseD2.ScrollToBottom();
                                    DoEvents(); DoEvents(); window.UpdateLayout(); DoEvents(); DoEvents();
                                    Console.WriteLine($"COFRES-INSPECTOR-FASED-SCROLLDEBUG: despues de ScrollToBottom -> VerticalOffset={scrollInspectorFaseD2.VerticalOffset:0.#}, ScrollableHeight={scrollInspectorFaseD2.ScrollableHeight:0.#}, ExtentHeight={scrollInspectorFaseD2.ExtentHeight:0.#}, ViewportHeight={scrollInspectorFaseD2.ViewportHeight:0.#}");
                                    string rutaFaseDAbajo = CapturaVentanaKeepQa(window, "fased-inspector-prefijos-guardar-cancelar");
                                    Console.WriteLine($"COFRES-INSPECTOR-FASED: captura real del final del Inspector (Prefixes de los 17 reales + Guardar/Cancelar) -> {rutaFaseDAbajo}");

                                    // Guarda de regresion real (no solo debug): las filas Guardar/Cancelar
                                    // deben quedar DENTRO del DockPanel.ActualHeight real (MinHeight=1000,
                                    // ExploracionRediseno Fase D - subido de 800 tras medir aqui mismo que
                                    // un cofre real de 40 slots con un objeto de 17 prefijos las dejaba en
                                    // Y=848,5-873,1px, mas alla de los 800px de entonces, inalcanzables con
                                    // scroll). Si ActualHeight de cada hijo NUNCA se encoge por el recorte
                                    // del padre (solo se deja de pintar mas alla de el), comparar Bottom
                                    // contra panelFaseD.ActualHeight detecta el mismo recorte real si
                                    // volviera a reproducirse con contenido aun mas alto.
                                    if (window.FindName("ExplorationSidebarPanel") is FrameworkElement panelFaseD)
                                    {
                                        var botonesGuardarCancelarFaseD = Descendientes<System.Windows.Controls.Primitives.UniformGrid>(placeholderFaseD).FirstOrDefault();
                                        if (botonesGuardarCancelarFaseD != null)
                                        {
                                            var esquinaBotones = botonesGuardarCancelarFaseD.TransformToAncestor(panelFaseD).Transform(new Point(0, 0));
                                            double bottomBotonesFaseD = esquinaBotones.Y + botonesGuardarCancelarFaseD.ActualHeight;
                                            Console.WriteLine($"COFRES-INSPECTOR-FASED: Guardar/Cancelar real (natural, sin recortar) -> Y={esquinaBotones.Y:0.#}px, Bottom={bottomBotonesFaseD:0.#}px, DockPanel.ActualHeight={panelFaseD.ActualHeight:0.#}px (MinHeight XAML=1000)");
                                            if (bottomBotonesFaseD > panelFaseD.ActualHeight + 1)
                                                Console.WriteLine($"FALLO: COFRES-INSPECTOR-FASED - Guardar/Cancelar quedan en Bottom={bottomBotonesFaseD:0.#}px, MAS ALLA del DockPanel.ActualHeight={panelFaseD.ActualHeight:0.#}px real - inalcanzables con scroll, regresion del ajuste de MinHeight");
                                        }
                                    }

                                    // FASE F del responsive global (29-sep-2026, s13/s26-A/s26-H): negative acceptance real
                                    // de que ChestInspectorItemEditTemplate ya NO tiene ningun ScrollViewer propio -
                                    // el duplicado local de ADR-028 (26-sep-2026) se habia quedado con el patron
                                    // OLD MODEL (ScrollViewer "de seguridad" envolviendo el DockPanel entero + otro
                                    // anidado alrededor de la lista de Prefixes) que la FASE D (28-sep-2026) ya
                                    // habia retirado del ItemEditTemplate original de Objetos (ObjetosView.xaml,
                                    // "RETIRADOS los dos ScrollViewer propios de este panel") sin que el duplicado
                                    // se migrara. Cuenta los ScrollViewer DESCENDIENTES del propio ContentControl
                                    // (nunca ExplorationSidebarScroll, que es un ANCESTRO, no un descendiente) con
                                    // el caso real mas exigente ya colocado (Picaro, 17 prefijos).
                                    if (contentControlPicaroFaseD != null)
                                    {
                                        // Excluye el ScrollViewer interno PART_ContentHost que trae el ControlTemplate por
                                        // defecto de CUALQUIER TextBox (Slot.ItemId/Count/PrefixId, 3 en esta plantilla) -
                                        // gotcha real ya documentado en otros canarios de la familia (AjusteAlViewportTests/
                                        // CanarioResponsivePersonajeResto.cs), nunca un contenedor de scroll de diseño.
                                        int scrollViewersAnidadosFaseF = Descendientes<ScrollViewer>(contentControlPicaroFaseD)
                                            .Count(sv => sv.TemplatedParent is not System.Windows.Controls.Primitives.TextBoxBase);
                                        Console.WriteLine($"COFRES-INSPECTOR-FASEF-SINSCROLLANIDADO: ScrollViewer descendientes de ChestInspectorItemEditTemplate (Picaro, 17 prefijos)={scrollViewersAnidadosFaseF} (esperado 0 - unico scroll owner real es ExplorationSidebarScroll exterior)");
                                        if (scrollViewersAnidadosFaseF > 0)
                                            Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEF-SINSCROLLANIDADO - ChestInspectorItemEditTemplate volvio a tener un ScrollViewer propio, scroll anidado del mismo eje dentro de ExplorationSidebarScroll (s13/s26-A/s26-H)");
                                    }

                                    // ExploracionRediseno Fase H (26-sep-2026, aplicador-fix): el punto 5
                                    // del encargo pide repetir esta MISMA comprobacion a la ventana MINIMA
                                    // real (1080x700, Window.MinWidth/MinHeight de MainWindow.xaml:12) -
                                    // antes de Fase H el Inspector compartia el MinHeight=1000 de
                                    // ExplorationSidebarPanel (protegia el caso 1180x860 nada mas); ahora
                                    // ExplorationSidebarChestInspectorPlaceholder vive en Auto/natural
                                    // (sin Height/MinHeight propios) dentro del MISMO ScrollViewer
                                    // exterior (ExplorationSidebarScroll) - nada debe quedar inalcanzable
                                    // tampoco al tamaño mas pequeño real que la ventana permite.
                                    if (window.FindName("ExplorationSidebarScroll") is ScrollViewer scrollInspectorFaseHMin)
                                    {
                                        FijarTamaño(window, 1080, 700);
                                        DoEvents(); DoEvents(); window.UpdateLayout(); DoEvents(); DoEvents();
                                        scrollInspectorFaseHMin.ScrollToBottom();
                                        DoEvents(); DoEvents(); window.UpdateLayout(); DoEvents(); DoEvents();
                                        Console.WriteLine($"COFRES-INSPECTOR-FASEH-MIN1080x700: tras ScrollToBottom -> VerticalOffset={scrollInspectorFaseHMin.VerticalOffset:0.#}px, ScrollableHeight={scrollInspectorFaseHMin.ScrollableHeight:0.#}px, ExtentHeight={scrollInspectorFaseHMin.ExtentHeight:0.#}px, ViewportHeight={scrollInspectorFaseHMin.ViewportHeight:0.#}px (esperado VerticalOffset≈ScrollableHeight)");
                                        if (Math.Abs(scrollInspectorFaseHMin.VerticalOffset - scrollInspectorFaseHMin.ScrollableHeight) > 1)
                                            Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEH-MIN1080x700 - ScrollToBottom no deja VerticalOffset≈ScrollableHeight en ExplorationSidebarScroll a 1080x700");

                                        var botonesGuardarCancelarFaseHMin = Descendientes<System.Windows.Controls.Primitives.UniformGrid>(placeholderFaseD).FirstOrDefault();
                                        if (botonesGuardarCancelarFaseHMin == null)
                                            Console.WriteLine("FALLO: COFRES-INSPECTOR-FASEH-MIN1080x700 - no se encuentra el UniformGrid real de Guardar/Cancelar a 1080x700");
                                        else
                                        {
                                            var rectBotonesFaseHMin = botonesGuardarCancelarFaseHMin.TransformToAncestor(window)
                                                .TransformBounds(new Rect(0, 0, botonesGuardarCancelarFaseHMin.ActualWidth, botonesGuardarCancelarFaseHMin.ActualHeight));
                                            bool alcanzableEnPantalla = rectBotonesFaseHMin.Top >= -1 && rectBotonesFaseHMin.Bottom <= window.ActualHeight + 1;
                                            Console.WriteLine($"COFRES-INSPECTOR-FASEH-MIN1080x700: Guardar/Cancelar rect real tras ScrollToBottom={rectBotonesFaseHMin}, ventana={window.ActualWidth:0}x{window.ActualHeight:0} -> alcanzableEnPantalla={alcanzableEnPantalla} (esperado True)");
                                            if (!alcanzableEnPantalla)
                                                Console.WriteLine($"FALLO: COFRES-INSPECTOR-FASEH-MIN1080x700 - Guardar/Cancelar quedan fuera de la ventana tras ScrollToBottom a 1080x700: {rectBotonesFaseHMin}");
                                            else
                                            {
                                                string rutaFaseHMin = CapturaVentanaKeepQa(window, "faseh-inspector-1080x700-guardar-cancelar");
                                                Console.WriteLine($"COFRES-INSPECTOR-FASEH-MIN1080x700: captura real a 1080x700 -> {rutaFaseHMin}");
                                            }
                                        }

                                        FijarTamaño(window, 1180, 860);
                                        DoEvents(); DoEvents(); window.UpdateLayout(); DoEvents(); DoEvents();
                                    }
                                }
                            }

                            // ---- Ruta de cierre 2: Guardar (SaveEditingChestAsync) ----
                            var tareaGuardarFaseD = vm.Exploration.SaveEditingChestCommand.ExecuteAsync(null);
                            while (!tareaGuardarFaseD.IsCompleted) DoEvents();
                            DoEvents(); DoEvents();
                            Console.WriteLine($"COFRES-INSPECTOR-FASED-R-GUARDAR: tras Guardar -> SidebarMode={vm.Exploration.SidebarMode} (esperado Browse), EditingChest={(vm.Exploration.EditingChest == null ? "null" : "NO-NULL")}");
                            if (vm.Exploration.SidebarMode != ExplorationSidebarMode.Browse)
                                Console.WriteLine("FALLO: COFRES-INSPECTOR-FASED-R-GUARDAR - Guardar no vuelve SidebarMode a Browse desde el Inspector real");
                        }
                    }

                    // ---- Ruta 2 (marcador del mapa) y Ruta 3 (resultado de busqueda) - regresion AR-13d/AR-13e,
                    // deben abrir ahora el Inspector REAL (con su cabecera/rejilla real), no el placeholder de Fase C.
                    bool abrioCofre7FaseD = vm.Exploration.TryOpenChestAtTile(5579, 1036);
                    DoEvents(); DoEvents(); window.UpdateLayout();
                    var cabeceraTrasMapaFaseD = Descendientes<Button>(placeholderFaseD).FirstOrDefault(b =>
                        (BindingOperations.GetBindingExpression(b, Button.CommandProperty)?.ParentBinding?.Path?.Path) == "Exploration.CancelEditingChestCommand");
                    Console.WriteLine($"COFRES-INSPECTOR-FASED-R2: TryOpenChestAtTile(5579,1036) -> encontro={abrioCofre7FaseD}, SidebarMode={vm.Exploration.SidebarMode} (esperado ChestInspector), cabecera real '<- Cofres' presente={cabeceraTrasMapaFaseD != null}");
                    if (!abrioCofre7FaseD || vm.Exploration.SidebarMode != ExplorationSidebarMode.ChestInspector || cabeceraTrasMapaFaseD == null)
                        Console.WriteLine("FALLO: COFRES-INSPECTOR-FASED-R2 - abrir el cofre real desde el marcador del mapa no deja el Inspector REAL (con cabecera) visible - regresion de AR-13d/AR-13e");
                    vm.Exploration.CancelEditingChestCommand.Execute(null);
                    DoEvents(); DoEvents();

                    var hitFaseD = new WorldSearchHitRowViewModel(new WorldSearchHit(5580, 1037, "Barra de hierro", WorldSearchKind.ChestItem));
                    vm.Exploration.GoToWorldSearchHitCommand.Execute(hitFaseD);
                    DoEvents(); DoEvents(); window.UpdateLayout();
                    var cabeceraTrasBusquedaFaseD = Descendientes<Button>(placeholderFaseD).FirstOrDefault(b =>
                        (BindingOperations.GetBindingExpression(b, Button.CommandProperty)?.ParentBinding?.Path?.Path) == "Exploration.CancelEditingChestCommand");
                    Console.WriteLine($"COFRES-INSPECTOR-FASED-R3: GoToWorldSearchHitCommand (ChestItem) -> SidebarMode={vm.Exploration.SidebarMode} (esperado ChestInspector), cabecera real '<- Cofres' presente={cabeceraTrasBusquedaFaseD != null}");
                    if (vm.Exploration.SidebarMode != ExplorationSidebarMode.ChestInspector || cabeceraTrasBusquedaFaseD == null)
                        Console.WriteLine("FALLO: COFRES-INSPECTOR-FASED-R3 - un resultado de busqueda ChestItem no deja el Inspector REAL visible - regresion de AR-13e");
                    vm.Exploration.CancelEditingChestCommand.Execute(null);
                    DoEvents(); DoEvents();
                }
            }
            catch (Exception exFaseD) { Console.WriteLine("COFRES-INSPECTOR-FASED-EXCEPTION: " + exFaseD); }

            // ================= ExploracionRediseno Fase F: WorldTools separado de Buscar (25-sep-2026) =================
            // Canario del aplicador-fix - los 3 Expanders de "Mundo" (Este mundo/Editar mundo/Bestiario)
            // se movieron TAL CUAL desde su Dock="Top" permanente de la cabecera a un DockPanel nuevo
            // (ExplorationSidebarWorldToolsContent) condicionado a SidebarMode==WorldTools, 3er hijo
            // superpuesto de ExplorationSidebarBrowseInspectorHost (junto a BrowseContent de Fase C y el
            // Inspector real de Fase D). Selector real "Buscar"/"Mundo" nuevo en la cabecera fija
            // (ShowSidebarBrowseCommand/ShowSidebarWorldToolsCommand, ExplorationViewModel). Confirma:
            // (1) el Grid/selector real existe y sus RadioButton reflejan SidebarMode via EnumEquals
            // OneWay; (2) WorldTools oculta Browse por completo (nunca comparten viewport); (3) los 3
            // Expanders arrancan desplegados en su propio contexto; (4) ChestInspector->Mundo cancela el
            // cofre en edicion antes de saltar (decision real documentada en el XAML); (5) los 3 botones
            // de guardado del editor de mundo (Spawn/Tiempo-Luna/Banderas) siguen escribiendo el .wld
            // real - ida y vuelta reversible sobre el mismo mundo de prueba ya cargado (Blando_Río.wld,
            // nunca el mundo real del usuario).
            try
            {
                string outDirFaseF = Path.Combine(AppContext.BaseDirectory, "keepqa-evidencia");
                Directory.CreateDirectory(outDirFaseF);

                var selectorBuscar = Descendientes<RadioButton>(window).FirstOrDefault(r =>
                    (BindingOperations.GetBindingExpression(r, RadioButton.CommandProperty)?.ParentBinding?.Path?.Path) == "Exploration.ShowSidebarBrowseCommand");
                var selectorMundo = Descendientes<RadioButton>(window).FirstOrDefault(r =>
                    (BindingOperations.GetBindingExpression(r, RadioButton.CommandProperty)?.ParentBinding?.Path?.Path) == "Exploration.ShowSidebarWorldToolsCommand");
                // ADR-TERRAKEEP-029: reusa browseViewHost (FindName doble) ya obtenido arriba.
                var browseFaseF = browseViewHost?.FindName("ExplorationSidebarBrowseContent") as FrameworkElement;
                // ADR-TERRAKEEP-027: WorldTools ahora vive en su propio UserControl
                // (WorldToolsView) - FindName DOBLE (mismo patron ya usado por GUIA/ADR-021 y
                // Compare/ADR-025).
                var worldToolsViewHostFaseF = window.FindName("WorldToolsView") as FrameworkElement;
                var worldToolsFaseF = worldToolsViewHostFaseF?.FindName("ExplorationSidebarWorldToolsContent") as FrameworkElement;
                Console.WriteLine($"EXPLORACION-FASEF: selector 'Buscar' encontrado={selectorBuscar != null}, selector 'Mundo' encontrado={selectorMundo != null}, ExplorationSidebarWorldToolsContent encontrado={worldToolsFaseF != null}, ExplorationSidebarBrowseContent encontrado={browseFaseF != null}");
                if (selectorBuscar == null || selectorMundo == null || worldToolsFaseF == null || browseFaseF == null)
                    Console.WriteLine("FALLO: EXPLORACION-FASEF - falta el selector real 'Buscar'/'Mundo' o alguno de los 2 contenedores de modo en el arbol visual (MainWindow.xaml)");
                else
                {
                    // Asegura estado de partida real: Browse, mundo Blando_Río.wld ya cargado por Fase D.
                    if (vm.Exploration.EditingChest != null) vm.Exploration.CancelEditingChestCommand.Execute(null);
                    vm.Exploration.SidebarMode = ExplorationSidebarMode.Browse;
                    vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                    vm.Exploration.ChestViewMode = 0;
                    DoEvents(); DoEvents(); window.UpdateLayout();

                    Console.WriteLine($"EXPLORACION-FASEF: estado inicial -> SidebarMode={vm.Exploration.SidebarMode} (esperado Browse), selector Buscar.IsChecked={selectorBuscar.IsChecked} (esperado True), selector Mundo.IsChecked={selectorMundo.IsChecked} (esperado False), Browse.Visibility={browseFaseF.Visibility} (esperado Visible), WorldTools.Visibility={worldToolsFaseF.Visibility} (esperado Collapsed)");
                    if (selectorBuscar.IsChecked != true || selectorMundo.IsChecked == true || browseFaseF.Visibility != Visibility.Visible || worldToolsFaseF.Visibility != Visibility.Collapsed)
                        Console.WriteLine("FALLO: EXPLORACION-FASEF - el estado inicial (Browse) no deja el selector/los 2 contenedores en el estado esperado");

                    // Vuelve arriba del todo antes de capturar - FaseD (mas arriba en este mismo canario)
                    // deja el ScrollViewer exterior desplazado al fondo (ScrollToBottom real), y esa
                    // posicion sobrevive al cambio de modo (el ScrollViewer es exterior a los 3 Grids
                    // de SidebarMode) - sin esto la captura no mostraria la cabecera real con el
                    // selector Buscar/Mundo.
                    if (window.FindName("ExplorationSidebarScroll") is ScrollViewer scrollFaseFBrowse)
                    { scrollFaseFBrowse.ScrollToTop(); DoEvents(); DoEvents(); window.UpdateLayout(); }

                    var rtbBrowseFaseF = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    rtbBrowseFaseF.Render(window);
                    var encBrowseFaseF = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encBrowseFaseF.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbBrowseFaseF));
                    string rutaBrowseFaseF = Path.Combine(outDirFaseF, "fasef-browse-sin-expanders.png");
                    using (var fsBrowseFaseF = File.Create(rutaBrowseFaseF)) encBrowseFaseF.Save(fsBrowseFaseF);
                    Console.WriteLine($"EXPLORACION-FASEF: captura real de Browse (sin los 3 Expanders de Mundo compartiendo viewport) -> {rutaBrowseFaseF}");

                    // ---- Cambio real de modo via el Command (mismo camino real que pulsar el RadioButton) ----
                    vm.Exploration.ShowSidebarWorldToolsCommand.Execute(null);
                    DoEvents(); DoEvents(); window.UpdateLayout();

                    Console.WriteLine($"EXPLORACION-FASEF: tras ShowSidebarWorldToolsCommand -> SidebarMode={vm.Exploration.SidebarMode} (esperado WorldTools), selector Buscar.IsChecked={selectorBuscar.IsChecked} (esperado False), selector Mundo.IsChecked={selectorMundo.IsChecked} (esperado True), Browse.Visibility={browseFaseF.Visibility} (esperado Collapsed), WorldTools.Visibility={worldToolsFaseF.Visibility} (esperado Visible)");
                    if (vm.Exploration.SidebarMode != ExplorationSidebarMode.WorldTools || selectorBuscar.IsChecked == true || selectorMundo.IsChecked != true || browseFaseF.Visibility != Visibility.Collapsed || worldToolsFaseF.Visibility != Visibility.Visible)
                        Console.WriteLine("FALLO: EXPLORACION-FASEF - ShowSidebarWorldToolsCommand no deja el selector/los 2 contenedores en el estado esperado (WorldTools)");

                    // ExploracionRediseno (rediseño de Exploracion>Mundo, 26-sep-2026): ya no hay 3
                    // Expander simultaneos dentro de WorldTools - 3 pastillas (CategorySelector,
                    // GroupName="ExploracionMundoSubvista") navegan 3 subvistas EXCLUSIVAS
                    // (WorldToolsSection, ExplorationViewModel), una visible cada vez.
                    var pillOverviewFaseF = Descendientes<RadioButton>(worldToolsFaseF).FirstOrDefault(r =>
                        (BindingOperations.GetBindingExpression(r, RadioButton.IsCheckedProperty)?.ParentBinding?.ConverterParameter as string) == "Overview");
                    var pillEditFaseF = Descendientes<RadioButton>(worldToolsFaseF).FirstOrDefault(r =>
                        (BindingOperations.GetBindingExpression(r, RadioButton.IsCheckedProperty)?.ParentBinding?.ConverterParameter as string) == "Edit");
                    var pillBestiaryFaseF = Descendientes<RadioButton>(worldToolsFaseF).FirstOrDefault(r =>
                        (BindingOperations.GetBindingExpression(r, RadioButton.IsCheckedProperty)?.ParentBinding?.ConverterParameter as string) == "Bestiary");
                    var subviewOverviewFaseF = worldToolsViewHostFaseF?.FindName("ExplorationSidebarWorldToolsOverview") as FrameworkElement;
                    var subviewEditFaseF = worldToolsViewHostFaseF?.FindName("ExplorationSidebarWorldToolsEdit") as FrameworkElement;
                    var subviewBestiaryFaseF = worldToolsViewHostFaseF?.FindName("ExplorationSidebarWorldToolsBestiary") as FrameworkElement;
                    Console.WriteLine($"EXPLORACION-FASEF: pastillas/subvistas de Mundo -> 'Este mundo' pastilla={pillOverviewFaseF != null}/subvista={subviewOverviewFaseF != null}; 'Editar mundo' pastilla={pillEditFaseF != null}/subvista={subviewEditFaseF != null}; 'Bestiario' pastilla={pillBestiaryFaseF != null}/subvista={subviewBestiaryFaseF != null} (HasBestiary={vm.Exploration.HasBestiary}); WorldToolsSection inicial={vm.Exploration.WorldToolsSection} (esperado Overview)");
                    if (pillOverviewFaseF == null || pillEditFaseF == null || subviewOverviewFaseF == null || subviewEditFaseF == null
                        || vm.Exploration.WorldToolsSection != WorldToolsSection.Overview || subviewOverviewFaseF.IsVisible != true)
                        Console.WriteLine("FALLO: EXPLORACION-FASEF - falta alguna pastilla/subvista real de Mundo (Overview/Edit), o WorldTools no arranca en Overview visible por defecto");

                    // Ningun elemento real de Browse (WrapPanel de categorias, buscador, resultados) debe
                    // seguir ocupando espacio del viewport mientras WorldTools esta activo - confirma que
                    // ya NO comparten viewport (motivo real de esta fase).
                    var wrapCategoriasFaseF = browseFaseF as DockPanel;
                    Console.WriteLine($"EXPLORACION-FASEF: BrowseContent.IsVisible={browseFaseF.IsVisible} (esperado False, colapsado de verdad, no solo Visibility local)");
                    if (browseFaseF.IsVisible)
                        Console.WriteLine("FALLO: EXPLORACION-FASEF - BrowseContent sigue IsVisible=True con WorldTools activo, seguiria compartiendo viewport con Cofres/Minerales/Objetos");

                    // Una captura real por subvista (ya no una unica captura "las 3 expandidas a la
                    // vez" - las 3 subvistas son EXCLUSIVAS, nunca coexisten en pantalla).
                    foreach (var seccionFaseF in new[] { WorldToolsSection.Overview, WorldToolsSection.Edit, WorldToolsSection.Bestiary })
                    {
                        if (seccionFaseF == WorldToolsSection.Bestiary && !vm.Exploration.HasBestiary) continue;
                        vm.Exploration.WorldToolsSection = seccionFaseF;
                        DoEvents(); DoEvents(); window.UpdateLayout(); DoEvents(); DoEvents();

                        if (window.FindName("ExplorationSidebarScroll") is ScrollViewer scrollFaseFWorldTools)
                        { scrollFaseFWorldTools.ScrollToTop(); DoEvents(); DoEvents(); window.UpdateLayout(); }

                        var rtbWorldToolsFaseF = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                        rtbWorldToolsFaseF.Render(window);
                        var encWorldToolsFaseF = new System.Windows.Media.Imaging.PngBitmapEncoder();
                        encWorldToolsFaseF.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbWorldToolsFaseF));
                        string rutaWorldToolsFaseF = Path.Combine(outDirFaseF, $"fasef-worldtools-{seccionFaseF.ToString().ToLowerInvariant()}.png");
                        using (var fsWorldToolsFaseF = File.Create(rutaWorldToolsFaseF)) encWorldToolsFaseF.Save(fsWorldToolsFaseF);
                        Console.WriteLine($"EXPLORACION-FASEF: captura real de WorldTools/{seccionFaseF} (subvista exclusiva, Browse oculto) -> {rutaWorldToolsFaseF}");
                    }
                    vm.Exploration.WorldToolsSection = WorldToolsSection.Overview;
                    DoEvents(); DoEvents();

                    // ExploracionRediseno Fase H (26-sep-2026, aplicador-fix; adaptado 26-sep-2026 al
                    // rediseno de subvistas exclusivas): el punto 6 del encargo pide confirmar
                    // VISUALMENTE que WorldTools no pierde contenido inalcanzable a la ventana MINIMA
                    // real (1080x700) - WorldToolsContent vive en Auto/natural (sin Height/MinHeight
                    // propios, mismo motivo real que ExplorationSidebarChestInspectorPlaceholder mas
                    // arriba) dentro del MISMO ScrollViewer exterior, asi que el fondo real debe seguir
                    // cayendo DENTRO de la ventana tras bajar el scroll del todo - repetido para las 3
                    // subvistas EXCLUSIVAS por separado (ya no "las 3 expandidas a la vez", que ya no
                    // existe con este diseno).
                    if (window.FindName("ExplorationSidebarScroll") is ScrollViewer scrollFaseHWorldToolsMin)
                    {
                        FijarTamaño(window, 1080, 700);
                        DoEvents(); DoEvents(); window.UpdateLayout(); DoEvents(); DoEvents();

                        foreach (var seccionFaseH in new[] { WorldToolsSection.Overview, WorldToolsSection.Edit, WorldToolsSection.Bestiary })
                        {
                            if (seccionFaseH == WorldToolsSection.Bestiary && !vm.Exploration.HasBestiary) continue;
                            vm.Exploration.WorldToolsSection = seccionFaseH;
                            DoEvents(); DoEvents(); window.UpdateLayout(); DoEvents(); DoEvents();

                            scrollFaseHWorldToolsMin.ScrollToBottom();
                            DoEvents(); DoEvents(); window.UpdateLayout(); DoEvents(); DoEvents();
                            Console.WriteLine($"EXPLORACION-FASEH-WORLDTOOLS-MIN1080x700: {seccionFaseH} tras ScrollToBottom -> VerticalOffset={scrollFaseHWorldToolsMin.VerticalOffset:0.#}px, ScrollableHeight={scrollFaseHWorldToolsMin.ScrollableHeight:0.#}px, ExtentHeight={scrollFaseHWorldToolsMin.ExtentHeight:0.#}px, ViewportHeight={scrollFaseHWorldToolsMin.ViewportHeight:0.#}px (esperado VerticalOffset≈ScrollableHeight)");
                            if (Math.Abs(scrollFaseHWorldToolsMin.VerticalOffset - scrollFaseHWorldToolsMin.ScrollableHeight) > 1)
                                Console.WriteLine($"FALLO: EXPLORACION-FASEH-WORLDTOOLS-MIN1080x700 - ScrollToBottom no deja VerticalOffset≈ScrollableHeight en ExplorationSidebarScroll a 1080x700 ({seccionFaseH})");

                            var rectWorldToolsFaseHMin = worldToolsFaseF.TransformToAncestor(window)
                                .TransformBounds(new Rect(0, 0, worldToolsFaseF.ActualWidth, worldToolsFaseF.ActualHeight));
                            bool worldToolsAlcanzable = rectWorldToolsFaseHMin.Bottom <= window.ActualHeight + 1;
                            Console.WriteLine($"EXPLORACION-FASEH-WORLDTOOLS-MIN1080x700: {seccionFaseH} WorldToolsContent rect real tras ScrollToBottom={rectWorldToolsFaseHMin}, ventana={window.ActualWidth:0}x{window.ActualHeight:0} -> alcanzableEnPantalla={worldToolsAlcanzable} (esperado True)");
                            if (!worldToolsAlcanzable)
                                Console.WriteLine($"FALLO: EXPLORACION-FASEH-WORLDTOOLS-MIN1080x700 - el fondo real de WorldToolsContent (Bottom={rectWorldToolsFaseHMin.Bottom:0.#}px) queda fuera de la ventana (alto={window.ActualHeight:0}px) tras ScrollToBottom a 1080x700 - contenido inalcanzable ({seccionFaseH})");

                            var rtbWorldToolsFaseHMin = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                            rtbWorldToolsFaseHMin.Render(window);
                            var encWorldToolsFaseHMin = new System.Windows.Media.Imaging.PngBitmapEncoder();
                            encWorldToolsFaseHMin.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtbWorldToolsFaseHMin));
                            string rutaWorldToolsFaseHMin = Path.Combine(outDirFaseF, $"faseh-worldtools-{seccionFaseH.ToString().ToLowerInvariant()}-1080x700-fondo.png");
                            using (var fsWorldToolsFaseHMin = File.Create(rutaWorldToolsFaseHMin)) encWorldToolsFaseHMin.Save(fsWorldToolsFaseHMin);
                            Console.WriteLine($"EXPLORACION-FASEH-WORLDTOOLS-MIN1080x700: captura real a 1080x700 ({seccionFaseH}, fondo del scroll) -> {rutaWorldToolsFaseHMin}");

                            scrollFaseHWorldToolsMin.ScrollToTop();
                            DoEvents(); DoEvents();
                        }

                        FijarTamaño(window, 1180, 860);
                        DoEvents(); DoEvents(); window.UpdateLayout(); DoEvents(); DoEvents();
                        vm.Exploration.WorldToolsSection = WorldToolsSection.Overview;
                        scrollFaseHWorldToolsMin.ScrollToTop();
                        DoEvents(); DoEvents(); window.UpdateLayout();
                    }

                    // ---- Interaccion WorldTools <-> ChestInspector: Mundo debe cancelar un cofre en edicion ----
                    // ChestRows SOLO se rellena en ChestViewMode==2 ("Cofre a cofre",
                    // RebuildChestByChest) - lo deja fijado aqui, no en 0 (el usado arriba solo para
                    // la captura de Browse), o esta lista real estaria vacia y la interaccion no se
                    // probaria de verdad.
                    vm.Exploration.ShowSidebarBrowseCommand.Execute(null);
                    vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                    vm.Exploration.ChestViewMode = 2;
                    DoEvents(); DoEvents();
                    var filaFaseF = vm.Exploration.ChestRows.FirstOrDefault();
                    if (filaFaseF == null)
                        Console.WriteLine("EXPLORACION-FASEF-CHESTINSPECTOR: AVISO - no hay fila de cofre real para probar la interaccion WorldTools<->ChestInspector");
                    else
                    {
                        vm.Exploration.EditChestCommand.Execute(filaFaseF);
                        DoEvents(); DoEvents();
                        Console.WriteLine($"EXPLORACION-FASEF-CHESTINSPECTOR: cofre abierto -> SidebarMode={vm.Exploration.SidebarMode} (esperado ChestInspector), EditingChest={(vm.Exploration.EditingChest == null ? "null" : "NO-NULL")} (esperado NO-NULL)");

                        vm.Exploration.ShowSidebarWorldToolsCommand.Execute(null);
                        DoEvents(); DoEvents(); window.UpdateLayout();
                        Console.WriteLine($"EXPLORACION-FASEF-CHESTINSPECTOR: tras pulsar 'Mundo' con un cofre en edicion -> SidebarMode={vm.Exploration.SidebarMode} (esperado WorldTools), EditingChest={(vm.Exploration.EditingChest == null ? "null" : "NO-NULL")} (esperado null, cancelado primero)");
                        if (vm.Exploration.SidebarMode != ExplorationSidebarMode.WorldTools || vm.Exploration.EditingChest != null)
                            Console.WriteLine("FALLO: EXPLORACION-FASEF-CHESTINSPECTOR - 'Mundo' no cancelo el cofre en edicion antes de saltar a WorldTools (EditingChest quedo colgado)");
                    }

                    // ================= Guardado real: Spawn/Tiempo-Luna/Banderas siguen escribiendo el .wld =================
                    // Ida y vuelta reversible sobre Blando_Río.wld (mundo de prueba, nunca el real del
                    // usuario) - cada bloque cambia un valor, guarda de verdad (misma llamada real a
                    // WorldFileService.SaveXxx que usa el usuario), confirma el mensaje de exito real y
                    // el estado en memoria actualizado, y restaura el valor original con una segunda
                    // escritura real - el archivo queda exactamente como estaba.
                    try
                    {
                        int spawnXOriginal = vm.Exploration.WorldSpawnX, spawnYOriginal = vm.Exploration.WorldSpawnY;
                        vm.Exploration.EditSpawnX = spawnXOriginal + 1;
                        vm.Exploration.EditSpawnY = spawnYOriginal;
                        var guardarSpawnIda = vm.Exploration.SaveSpawnPointCommand.ExecuteAsync(null);
                        while (!guardarSpawnIda.IsCompleted) DoEvents();
                        DoEvents(); DoEvents();
                        bool spawnIdaOk = vm.Exploration.WorldSpawnX == spawnXOriginal + 1 && (vm.Exploration.SpawnSaveStatus ?? "").Length > 0;
                        Console.WriteLine($"EXPLORACION-FASEF-GUARDADO-SPAWN: ida -> WorldSpawnX={vm.Exploration.WorldSpawnX} (esperado {spawnXOriginal + 1}), SpawnSaveStatus='{vm.Exploration.SpawnSaveStatus}'");
                        if (!spawnIdaOk) Console.WriteLine("FALLO: EXPLORACION-FASEF-GUARDADO-SPAWN - SaveSpawnPointCommand no escribio el nuevo valor real (WorldTools, ida)");

                        vm.Exploration.EditSpawnX = spawnXOriginal;
                        vm.Exploration.EditSpawnY = spawnYOriginal;
                        var guardarSpawnVuelta = vm.Exploration.SaveSpawnPointCommand.ExecuteAsync(null);
                        while (!guardarSpawnVuelta.IsCompleted) DoEvents();
                        DoEvents(); DoEvents();
                        bool spawnVueltaOk = vm.Exploration.WorldSpawnX == spawnXOriginal && vm.Exploration.WorldSpawnY == spawnYOriginal;
                        Console.WriteLine($"EXPLORACION-FASEF-GUARDADO-SPAWN: vuelta -> WorldSpawnX={vm.Exploration.WorldSpawnX}, WorldSpawnY={vm.Exploration.WorldSpawnY} (esperado {spawnXOriginal},{spawnYOriginal} - mundo de prueba restaurado)");
                        if (!spawnVueltaOk) Console.WriteLine("FALLO: EXPLORACION-FASEF-GUARDADO-SPAWN - no se pudo restaurar el spawn original del mundo de prueba tras la escritura real");
                    }
                    catch (Exception exSpawnFaseF) { Console.WriteLine("EXPLORACION-FASEF-GUARDADO-SPAWN-EXCEPTION: " + exSpawnFaseF); }

                    try
                    {
                        int faseOriginal = vm.Exploration.EditMoonPhase;
                        int faseNueva = (faseOriginal + 1) % 8;
                        vm.Exploration.EditMoonPhase = faseNueva;
                        var guardarTiempoIda = vm.Exploration.SaveTimeAndMoonCommand.ExecuteAsync(null);
                        while (!guardarTiempoIda.IsCompleted) DoEvents();
                        DoEvents(); DoEvents();
                        Console.WriteLine($"EXPLORACION-FASEF-GUARDADO-TIEMPO: ida -> EditMoonPhase={vm.Exploration.EditMoonPhase} (esperado {faseNueva}), TimeSaveStatus='{vm.Exploration.TimeSaveStatus}'");
                        if (vm.Exploration.EditMoonPhase != faseNueva || string.IsNullOrEmpty(vm.Exploration.TimeSaveStatus))
                            Console.WriteLine("FALLO: EXPLORACION-FASEF-GUARDADO-TIEMPO - SaveTimeAndMoonCommand no escribio la fase lunar nueva (WorldTools, ida)");

                        vm.Exploration.EditMoonPhase = faseOriginal;
                        var guardarTiempoVuelta = vm.Exploration.SaveTimeAndMoonCommand.ExecuteAsync(null);
                        while (!guardarTiempoVuelta.IsCompleted) DoEvents();
                        DoEvents(); DoEvents();
                        Console.WriteLine($"EXPLORACION-FASEF-GUARDADO-TIEMPO: vuelta -> EditMoonPhase={vm.Exploration.EditMoonPhase} (esperado {faseOriginal} - mundo de prueba restaurado)");
                        if (vm.Exploration.EditMoonPhase != faseOriginal)
                            Console.WriteLine("FALLO: EXPLORACION-FASEF-GUARDADO-TIEMPO - no se pudo restaurar la fase lunar original del mundo de prueba tras la escritura real");
                    }
                    catch (Exception exTiempoFaseF) { Console.WriteLine("EXPLORACION-FASEF-GUARDADO-TIEMPO-EXCEPTION: " + exTiempoFaseF); }

                    try
                    {
                        bool goblinOriginal = vm.Exploration.EditDownedGoblinArmy;
                        vm.Exploration.EditDownedGoblinArmy = !goblinOriginal;
                        var guardarFlagsIda = vm.Exploration.SaveBossFlagsCommand.ExecuteAsync(null);
                        while (!guardarFlagsIda.IsCompleted) DoEvents();
                        DoEvents(); DoEvents();
                        Console.WriteLine($"EXPLORACION-FASEF-GUARDADO-BANDERAS: ida -> EditDownedGoblinArmy={vm.Exploration.EditDownedGoblinArmy} (esperado {!goblinOriginal}), FlagsSaveStatus='{vm.Exploration.FlagsSaveStatus}'");
                        if (vm.Exploration.EditDownedGoblinArmy != !goblinOriginal || string.IsNullOrEmpty(vm.Exploration.FlagsSaveStatus))
                            Console.WriteLine("FALLO: EXPLORACION-FASEF-GUARDADO-BANDERAS - SaveBossFlagsCommand no escribio la bandera nueva (WorldTools, ida)");

                        vm.Exploration.EditDownedGoblinArmy = goblinOriginal;
                        var guardarFlagsVuelta = vm.Exploration.SaveBossFlagsCommand.ExecuteAsync(null);
                        while (!guardarFlagsVuelta.IsCompleted) DoEvents();
                        DoEvents(); DoEvents();
                        Console.WriteLine($"EXPLORACION-FASEF-GUARDADO-BANDERAS: vuelta -> EditDownedGoblinArmy={vm.Exploration.EditDownedGoblinArmy} (esperado {goblinOriginal} - mundo de prueba restaurado)");
                        if (vm.Exploration.EditDownedGoblinArmy != goblinOriginal)
                            Console.WriteLine("FALLO: EXPLORACION-FASEF-GUARDADO-BANDERAS - no se pudo restaurar la bandera original del mundo de prueba tras la escritura real");
                    }
                    catch (Exception exFlagsFaseF) { Console.WriteLine("EXPLORACION-FASEF-GUARDADO-BANDERAS-EXCEPTION: " + exFlagsFaseF); }

                    // Vuelve a Browse para dejar el estado limpio de cara al resto del arnes.
                    vm.Exploration.ShowSidebarBrowseCommand.Execute(null);
                    DoEvents(); DoEvents();
                }
            }
            catch (Exception exFaseF) { Console.WriteLine("EXPLORACION-FASEF-EXCEPTION: " + exFaseF); }

            // ================= ChestInspector slots vacios REABIERTO (26-sep-2026, aplicador-fix) =================
            // Causa real confirmada por el investigador: WrapPanel (MainWindow.xaml, ItemsPanelTemplate de
            // Exploration.EditingChestSlots) sin ItemWidth fijo dejaba que WrapPanel.ArrangeOverride
            // estirara cada hijo a la ALTURA maxima de su fila (heredada de vecinos con icono) pero al
            // ANCHO propio del hijo (casi 0 para un slot vacio, sin IconPath/Count visibles que le den
            // ancho natural) -> linea vertical estrecha en vez de celda cuadrada. Arreglo real aplicado:
            // WrapPanel sustituido por controls:SlotGridPanel (mismo patron literal que la rejilla de la
            // Libreria, MainWindow.xaml ~3890 - MinCell/MaxCell via CompactCellSizeConverter single-binding,
            // SIN AvailableHeight porque este Inspector comparte ExplorationSidebarScroll con todo el
            // sidebar y nunca tiene ScrollViewer propio). Este canario mide ActualWidth vs ActualHeight de
            // CADA Border real (DataContext is ItemSlotViewModel) dentro de la rejilla de slots del
            // Inspector, separando explicitamente vacios/ocupados, a los 2 tamanos reales de ventana
            // (1180x860 por defecto y 1080x700 minimo real, MainWindow.xaml:12).
            try
            {
                vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                vm.Exploration.ChestViewMode = 2; // Cofre a cofre
                DoEvents(); DoEvents();

                var filaSlotsVacios = vm.Exploration.ChestRows.FirstOrDefault(r => r.Items.Count > 0 && r.Items.Count < 40);
                if (filaSlotsVacios == null)
                    Console.WriteLine("COFRES-INSPECTOR-SLOTSVACIOS: AVISO - no se encontro ningun cofre real con objetos Y huecos vacios tras el ultimo objeto (Items.Count>0 y <40) para medir el caso real del bug");
                else
                {
                    vm.Exploration.EditChestCommand.Execute(filaSlotsVacios);
                    DoEvents(); DoEvents(); window.UpdateLayout();

                    void MedirSlotsInspector(string etiquetaTamaño)
                    {
                        // Filtro real necesario: el DataTemplate de cada slot (MainWindow.xaml ~7780-7784)
                        // tiene un Border EXTERIOR real (Style=ItemSlotCardCompact, hijo directo del
                        // ContentPresenter que el ItemsControl genera para cada item) Y un Border INTERIOR
                        // de resaltado de seleccion (BorderBrush=AccentBrush, dentro del Grid) que hereda el
                        // MISMO DataContext por herencia normal de WPF - sin distinguir por el padre visual
                        // se cuentan 2 Border por slot (confirmado: 80 encontrados para un cofre de 40
                        // slots antes de este filtro). Solo el Border exterior (parent=ContentPresenter) es
                        // el que realmente recibe el tamaño de celda de SlotGridPanel.Arrange.
                        // ADR-TERRAKEEP-028: FindName DOBLE, ChestInspectorView tiene su propio NameScope.
                        var placeholderSlotsVacios = (window.FindName("ChestInspectorView") as FrameworkElement)?.FindName("ExplorationSidebarChestInspectorPlaceholder") as FrameworkElement;
                        var bordesSlots = placeholderSlotsVacios == null
                            ? Enumerable.Empty<Border>().ToList()
                            : Descendientes<Border>(placeholderSlotsVacios)
                                .Where(b => b.DataContext is ItemSlotViewModel && VisualTreeHelper.GetParent(b) is ContentPresenter)
                                .ToList();
                        Console.WriteLine($"COFRES-INSPECTOR-SLOTSVACIOS-{etiquetaTamaño}: {bordesSlots.Count} slots reales encontrados en el arbol visual del Inspector (esperado >0)");
                        if (bordesSlots.Count == 0)
                        {
                            Console.WriteLine($"FALLO: COFRES-INSPECTOR-SLOTSVACIOS-{etiquetaTamaño} - no se encontro ningun Border de slot en el arbol visual del Inspector");
                            return;
                        }

                        int vacios = 0, ocupados = 0;
                        double minCellVacios = double.MaxValue, minCellOcupados = double.MaxValue;
                        bool huboSlotDebajoDeMinCell = false;
                        bool huboSlotFueraDeAncho = false;
                        const double minCellEsperado = 40.0; // ContainerViewModel.cs:57, suelo universal real
                        // El propio Border (MainWindow.xaml ~7783) lleva Margin="0,0,4,4" ya existente
                        // ANTES de este arreglo (no se toco el DataTemplate, pedido explicito del encargo)
                        // - WPF resta ese margen del Arrange real (cell x cell que da SlotGridPanel), asi
                        // que el ActualWidth/Height renderizado real de MinCell=40 es 40-4=36, IGUAL en
                        // ambas dimensiones (por eso sigue siendo cuadrado). El suelo real a comprobar es
                        // MinCell-margen, no MinCell en crudo.
                        const double margenPropioDelBorde = 4.0;
                        double minCellRealEsperado = minCellEsperado - margenPropioDelBorde;
                        // Guarda real de alcance horizontal (no solo tamaño/forma): con Columns literal
                        // fijo y MinCell=40 como suelo, si el ancho real disponible del sidebar fuera
                        // menor que Columns*MinCell+Gap*(Columns-1), SlotGridPanel.MeasureOverride
                        // devolveria un ancho deseado MAYOR que el real (clamp de MinCell nunca reduce
                        // por debajo, solo sube) - los slots de las ultimas columnas quedarian fuera del
                        // ancho real del Inspector, invisibles/inalcanzables SIN ningun overflow visible
                        // en pantalla (ExplorationSidebarScroll tiene HorizontalScrollBarVisibility=
                        // Disabled, MainWindow.xaml:6766 - el mismo riesgo real ya documentado por
                        // AR-EX-HSCROLL en ExploracionRediseno FaseI). Verificado aqui por posicion REAL
                        // (TransformToAncestor) contra el ancho REAL del propio placeholder, no solo por
                        // aritmetica teorica.
                        foreach (var borde in bordesSlots)
                        {
                            var slot = (ItemSlotViewModel)borde.DataContext;
                            double w = borde.ActualWidth, h = borde.ActualHeight;
                            bool esCuadrado = Math.Abs(w - h) <= 1.0;

                            Point esquinaSlot;
                            try { esquinaSlot = borde.TransformToAncestor(placeholderSlotsVacios!).Transform(new Point(0, 0)); }
                            catch (InvalidOperationException) { esquinaSlot = new Point(double.NaN, double.NaN); }
                            if (!double.IsNaN(esquinaSlot.X))
                            {
                                double right = esquinaSlot.X + w;
                                if (esquinaSlot.X < -1.0 || right > placeholderSlotsVacios!.ActualWidth + 1.0)
                                    huboSlotFueraDeAncho = true;
                            }

                            if (slot.IsEmpty)
                            {
                                vacios++;
                                minCellVacios = Math.Min(minCellVacios, Math.Min(w, h));
                                Console.WriteLine($"COFRES-INSPECTOR-SLOTSVACIOS-{etiquetaTamaño}: slot VACIO ActualWidth={w:0.##}px ActualHeight={h:0.##}px cuadrado={esCuadrado} (tolerancia 1px)");
                                if (!esCuadrado)
                                    Console.WriteLine($"FALLO: COFRES-INSPECTOR-SLOTSVACIOS-{etiquetaTamaño} - slot vacio NO es cuadrado (Width={w:0.##}px, Height={h:0.##}px) - linea vertical estrecha reproducida");
                            }
                            else
                            {
                                ocupados++;
                                minCellOcupados = Math.Min(minCellOcupados, Math.Min(w, h));
                                if (!esCuadrado)
                                    Console.WriteLine($"FALLO: COFRES-INSPECTOR-SLOTSVACIOS-{etiquetaTamaño} - slot OCUPADO NO es cuadrado (Width={w:0.##}px, Height={h:0.##}px)");
                            }
                            if (Math.Min(w, h) < minCellRealEsperado - 1.0)
                                huboSlotDebajoDeMinCell = true;
                        }
                        Console.WriteLine($"COFRES-INSPECTOR-SLOTSVACIOS-{etiquetaTamaño}: vacios={vacios} (minCell real={(vacios > 0 ? minCellVacios : 0):0.##}px), ocupados={ocupados} (minCell real={(ocupados > 0 ? minCellOcupados : 0):0.##}px), MinCell esperado (tras margen propio del Border)>={minCellRealEsperado}px");
                        if (vacios == 0)
                            Console.WriteLine($"COFRES-INSPECTOR-SLOTSVACIOS-{etiquetaTamaño}: AVISO - el cofre elegido no dejo ningun slot IsEmpty=true en el arbol visual realizado - repetir con otro cofre para medir el caso vacio");
                        if (huboSlotDebajoDeMinCell)
                            Console.WriteLine($"FALLO: COFRES-INSPECTOR-SLOTSVACIOS-{etiquetaTamaño} - al menos un slot (vacio u ocupado) cayo por debajo del MinCell esperado ({minCellRealEsperado}px)");
                        if (window.FindName("ExplorationSidebarScroll") is ScrollViewer scrollDiag)
                            Console.WriteLine($"COFRES-INSPECTOR-SLOTSVACIOS-{etiquetaTamaño}-DIAG: ExplorationSidebarScroll ViewportWidth={scrollDiag.ViewportWidth:0.##}px ExtentWidth={scrollDiag.ExtentWidth:0.##}px ScrollableWidth={scrollDiag.ScrollableWidth:0.##}px");
                        Console.WriteLine($"COFRES-INSPECTOR-SLOTSVACIOS-{etiquetaTamaño}: placeholder.ActualWidth={placeholderSlotsVacios?.ActualWidth:0.##}px, algun slot fuera del ancho real del placeholder={huboSlotFueraDeAncho} (esperado False)");
                        if (huboSlotFueraDeAncho)
                            Console.WriteLine($"FALLO: COFRES-INSPECTOR-SLOTSVACIOS-{etiquetaTamaño} - al menos un slot queda fuera del ancho real del Inspector (Left<0 o Right>ActualWidth) - con HorizontalScrollBarVisibility=Disabled (MainWindow.xaml:6766) quedaria invisible/inalcanzable sin ningun aviso visual, mismo riesgo real que AR-EX-HSCROLL");
                    }

                    MedirSlotsInspector("1180x860");
                    CapturaVentanaKeepQa(window, "cofres-slotsvacios-1180x860");

                    FijarTamaño(window, 1080, 700);
                    DoEvents(); DoEvents(); window.UpdateLayout(); DoEvents(); DoEvents();
                    MedirSlotsInspector("1080x700");
                    CapturaVentanaKeepQa(window, "cofres-slotsvacios-1080x700");

                    FijarTamaño(window, 1180, 860);
                    DoEvents(); DoEvents(); window.UpdateLayout();

                    vm.Exploration.CancelEditingChestCommand.Execute(null);
                    DoEvents(); DoEvents();
                }
            }
            catch (Exception exSlotsVacios) { Console.WriteLine("COFRES-INSPECTOR-SLOTSVACIOS-EXCEPTION: " + exSlotsVacios); }

            // Deja recargado el mundo de siempre del resto del arnes, mismo criterio que AR-13d/AR-13e.
            vm.Exploration.ClearOreMarksCommand.Execute(null);
            vm.Exploration.ChestViewMode = 0;
            vm.Exploration.SelectedCategory = WorldSearchCategory.All;
            string mundoDeSiempre = MundoAislado(@"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld");
            if (File.Exists(mundoDeSiempre))
            {
                var vuelta = vm.Exploration.LoadFromPathAsync(mundoDeSiempre);
                while (!vuelta.IsCompleted) { DoEvents(); System.Threading.Thread.Sleep(15); }
                DoEvents();
            }
        }
        catch (Exception ex) { Console.WriteLine("COFRES-INSPECTOR-EXCEPTION: " + ex); }
    }
}
