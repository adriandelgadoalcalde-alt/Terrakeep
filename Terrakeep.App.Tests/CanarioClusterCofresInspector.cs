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
            string mundo = @"C:\Users\adrian\Documents\My Games\Terraria\Worlds\Blando_Río.wld";
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

            var bloqueResultados = window.FindName("ExplorationResultsBlock") as FrameworkElement;
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
                var bloqueResultadosPorContenido = window.FindName("ExplorationResultsBlock") as FrameworkElement;
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

            // Deja recargado el mundo de siempre del resto del arnes, mismo criterio que AR-13d/AR-13e.
            vm.Exploration.ClearOreMarksCommand.Execute(null);
            vm.Exploration.ChestViewMode = 0;
            vm.Exploration.SelectedCategory = WorldSearchCategory.All;
            string mundoDeSiempre = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld";
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
