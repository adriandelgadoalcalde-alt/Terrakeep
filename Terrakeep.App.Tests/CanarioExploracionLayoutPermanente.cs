// ExploracionRediseno FaseI (26-sep-2026, aplicador-fix) - CANARIO PERMANENTE, segun diseno
// original de FaseA (arquitecto-keep). A diferencia de FALLO3_SOLO/COFRES_INSPECTOR_SOLO/
// NAV123_SOLO (misma clase parcial, mismo patron de fichero dedicado, pero SOLO corren bajo
// demanda y terminan el proceso con Environment.Exit(0) - ver los bloques
// `if (Environment.GetEnvironmentVariable("...") == "1")` de Program.cs), esta funcion se invoca
// DOS VECES: aqui abajo via EXPLORATION_LAYOUT_SOLO=1 para iteracion rapida aislada durante el
// desarrollo, Y de forma INCONDICIONAL dentro de la secuencia normal de Program.cs (justo despues
// de PruebasMejorPrefijo, antes de PruebasInicioAjustesNovedadesAcercaDe - ver el comentario real
// alli) para que corra en CADA pasada completa del arnes, no solo bajo demanda. Pedido explicito
// del encargo: "debe correr en CADA pasada completa del arnés (no solo bajo demanda con
// _SOLO=1)".
//
// Objetivo real (FaseH acaba de eliminar el MinHeight compartido de ExplorationSidebarPanel -
// ver bitacora.md, mismo dia - y este canario existe para que la causa real no pueda volver sin
// que el arnes lo cace, en los 3 modos mutuamente excluyentes del sidebar de Exploracion
// -Browse/ChestInspector/WorldTools- y en los 2 tamanos reales de ventana que importan
// -1180x860 por defecto, 1080x700 el minimo real de MainWindow.xaml:12-):
//   1. Cero overflow horizontal real (ScrollableWidth<=0.5px de margen de redondeo) en
//      ExplorationSidebarScroll y en los 3 ScrollViewer internos de WorldTools (Este mundo/
//      Editar mundo/Bestiario) - ScrollableWidth = Max(0, ExtentWidth-ViewportWidth), fiable
//      incluso con HorizontalScrollBarVisibility="Disabled" (AR-EX-HSCROLL ya lo demostro: mide
//      overflow real aunque la barra nunca se vea).
//   2. Guardar/Cancelar del Inspector de cofre alcanzables por scroll (nunca recortados) en el
//      peor caso real (cofre con un objeto Picaro de 17 prefijos legales, mismo patron ya usado
//      en CanarioClusterCofresInspector.cs/COFRES-INSPECTOR-FASEH-MIN1080x700).
//   3. Ningun MinHeight fijo mayor a 350px en ExplorationSidebarBrowseContent - guardia explicita
//      anti-regresion contra el antipatron MinHeight=900/1000 que el usuario prohibio
//      textualmente ("Prohibido explicitamente por el usuario volver a subir este numero" -
//      MainWindow.xaml, comentario real de FaseH junto a ExplorationSidebarBrowseContent).
//   4. El sidebar completo muestra contenido real (no vacio) en los 3 modos, a los 2 tamanos.
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Terrakeep.App;
using Terrakeep.App.ViewModels;

internal static partial class Program
{
    private static void EjecutarCanarioExploracionLayoutPermanente(MainWindow window, MainViewModel vm)
    {
        try
        {
            string mundo = @"C:\Users\adrian\Documents\My Games\Terraria\tModLoader\Worlds\roca_negra.wld";
            if (!File.Exists(mundo))
            {
                Console.WriteLine("EXPLORATION_LAYOUT: mundo real roca_negra.wld no encontrado - omitido");
                return;
            }

            // ---- Guarda estado real de partida para restaurarlo al terminar (mismo criterio que
            // el resto del arnes: este canario no debe dejar nada distinto para los bloques que
            // corran despues) ----
            double anchoOriginal = window.ActualWidth > 0 ? window.ActualWidth : 1180;
            double altoOriginal = window.ActualHeight > 0 ? window.ActualHeight : 860;
            int tabOriginal = vm.SelectedTabIndex;

            vm.SelectedTabIndex = 4; // Exploracion
            DoEvents(); DoEvents();
            if (!vm.Exploration.IsWorldLoaded)
            {
                var carga = vm.Exploration.LoadFromPathAsync(mundo);
                while (!carga.IsCompleted) DoEvents();
                if (carga.IsFaulted) throw carga.Exception!;
                DoEvents();
            }

            // ---- Punto 3: guardia estatica anti-regresion del MinHeight (independiente de tamano/modo) ----
            var browseContentGuardia = window.FindName("ExplorationSidebarBrowseContent") as FrameworkElement;
            if (browseContentGuardia == null)
            {
                Console.WriteLine("FALLO: EXPLORATION_LAYOUT-MINHEIGHT - ExplorationSidebarBrowseContent no encontrado en el arbol visual (x:Name cambio en MainWindow.xaml)");
            }
            else
            {
                double minHeightReal = browseContentGuardia.MinHeight;
                Console.WriteLine($"EXPLORATION_LAYOUT-MINHEIGHT: ExplorationSidebarBrowseContent.MinHeight real={minHeightReal:0.#}px (limite permitido<=350px)");
                if (minHeightReal > 350)
                    Console.WriteLine($"FALLO: EXPLORATION_LAYOUT-MINHEIGHT - ExplorationSidebarBrowseContent.MinHeight={minHeightReal:0.#}px supera el limite de 350px - regresion real del antipatron MinHeight=900/1000 que el usuario prohibio explicitamente volver a subir (ver ExploracionRediseno FaseH, MainWindow.xaml)");
            }

            void ComprobarScrollHorizontal(string id, ScrollViewer? sv)
            {
                if (sv == null) { Console.WriteLine($"EXPLORATION_LAYOUT-HSCROLL: {id} - ScrollViewer real no encontrado, omitido"); return; }
                double scrollable = sv.ScrollableWidth;
                Console.WriteLine($"EXPLORATION_LAYOUT-HSCROLL: {id} ViewportWidth={sv.ViewportWidth:0.#}px ExtentWidth={sv.ExtentWidth:0.#}px ScrollableWidth={scrollable:0.#}px (esperado<=0.5px, cero overflow horizontal real)");
                if (scrollable > 0.5)
                    Console.WriteLine($"FALLO: EXPLORATION_LAYOUT-HSCROLL - {id} tiene ScrollableWidth={scrollable:0.#}px (overflow horizontal real detectado, se esperaba 0)");
            }

            foreach (var (w, h, etiqueta) in new (double w, double h, string etiqueta)[]
            {
                (1180, 860, "1180x860-defecto"),
                (1080, 700, "1080x700-minimo"),
            })
            {
                FijarTamaño(window, w, h);
                DoEvents(); DoEvents(); window.UpdateLayout(); DoEvents(); DoEvents();

                var scrollLateral = window.FindName("ExplorationSidebarScroll") as ScrollViewer;
                if (scrollLateral == null)
                {
                    Console.WriteLine($"FALLO: EXPLORATION_LAYOUT - ExplorationSidebarScroll no encontrado a {etiqueta} - se omite el resto de este tamano");
                    continue;
                }

                // ================= Modo Browse =================
                try
                {
                    if (vm.Exploration.EditingChest != null) vm.Exploration.CancelEditingChestCommand.Execute(null);
                    vm.Exploration.ShowSidebarBrowseCommand.Execute(null);
                    vm.Exploration.SelectedCategory = WorldSearchCategory.All;
                    DoEvents(); DoEvents(); window.UpdateLayout();
                    scrollLateral.ScrollToTop();
                    DoEvents(); DoEvents();

                    var browseContent = window.FindName("ExplorationSidebarBrowseContent") as FrameworkElement;
                    bool browseConContenido = browseContent != null && browseContent.IsVisible
                        && Descendientes<RadioButton>(browseContent).Any(r => r.IsVisible);
                    Console.WriteLine($"EXPLORATION_LAYOUT-CONTENIDO: Browse a {etiqueta} -> encontrado={browseContent != null}, IsVisible={browseContent?.IsVisible}, con pildoras de categoria reales visibles={browseConContenido}");
                    if (!browseConContenido)
                        Console.WriteLine($"FALLO: EXPLORATION_LAYOUT-CONTENIDO - Browse a {etiqueta} no muestra contenido real (sidebar vacio)");

                    ComprobarScrollHorizontal($"ExplorationSidebarScroll_Browse_{etiqueta}", scrollLateral);
                    CapturaVentanaKeepQa(window, $"explorationlayout-browse-{etiqueta}");
                }
                catch (Exception exBrowse) { Console.WriteLine($"EXPLORATION_LAYOUT-BROWSE-EXCEPTION ({etiqueta}): " + exBrowse); }

                // ================= Modo ChestInspector (peor caso: objeto con 17 prefijos legales) =================
                try
                {
                    vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                    vm.Exploration.ChestViewMode = 2; // Cofre a cofre
                    DoEvents(); DoEvents();
                    var filaChest = vm.Exploration.ChestRows.FirstOrDefault(r => r.Items.Count > 0) ?? vm.Exploration.ChestRows.FirstOrDefault();
                    if (filaChest == null)
                    {
                        Console.WriteLine($"EXPLORATION_LAYOUT-CHESTINSPECTOR: AVISO - sin fila de cofre real en {mundo} a {etiqueta}, omitido");
                    }
                    else
                    {
                        vm.Exploration.EditChestCommand.Execute(filaChest);
                        DoEvents(); DoEvents();

                        // Arma Picaro real de Calamity, mismo peor caso real ya usado en
                        // COFRES_INSPECTOR_SOLO/AuditoriaViewportScroll.cs Fase H (17 prefijos
                        // legales, rogue_prefixes.json "weapon":17).
                        vm.Library.SearchText = "Hacha Arrojadiza de Adamantita";
                        WaitForDispatcher(300);
                        var picaro = vm.Library.Results.FirstOrDefault();
                        vm.Library.SearchText = string.Empty;
                        DoEvents();
                        var slotDestino = vm.Exploration.EditingChestSlots.FirstOrDefault();
                        if (picaro != null && slotDestino != null)
                        {
                            vm.Exploration.SelectChestSlot(slotDestino);
                            DoEvents();
                            slotDestino.ItemId = picaro.Id;
                            DoEvents(); DoEvents(); window.UpdateLayout();
                        }
                        else
                        {
                            Console.WriteLine($"EXPLORATION_LAYOUT-CHESTINSPECTOR: AVISO ({etiqueta}) - no se pudo colocar el Picaro de 17 prefijos, se mide el Inspector con el contenido real del cofre");
                        }

                        var placeholder = window.FindName("ExplorationSidebarChestInspectorPlaceholder") as FrameworkElement;
                        var botonVolver = placeholder == null ? null : Descendientes<Button>(placeholder).FirstOrDefault(b =>
                            (BindingOperations.GetBindingExpression(b, Button.CommandProperty)?.ParentBinding?.Path?.Path) == "Exploration.CancelEditingChestCommand");
                        bool inspectorConContenido = placeholder != null && placeholder.IsVisible && botonVolver != null && botonVolver.IsVisible;
                        Console.WriteLine($"EXPLORATION_LAYOUT-CONTENIDO: ChestInspector a {etiqueta} -> placeholder encontrado={placeholder != null}, IsVisible={placeholder?.IsVisible}, cabecera '<- Cofres' visible={botonVolver?.IsVisible}");
                        if (!inspectorConContenido)
                            Console.WriteLine($"FALLO: EXPLORATION_LAYOUT-CONTENIDO - ChestInspector a {etiqueta} no muestra contenido real (sidebar vacio)");

                        ComprobarScrollHorizontal($"ExplorationSidebarScroll_ChestInspector_{etiqueta}", scrollLateral);

                        // ---- Guardar/Cancelar alcanzables por scroll (nunca recortados) ----
                        scrollLateral.ScrollToBottom();
                        DoEvents(); DoEvents(); window.UpdateLayout(); DoEvents(); DoEvents();
                        Console.WriteLine($"EXPLORATION_LAYOUT-GUARDARCANCELAR: {etiqueta} tras ScrollToBottom -> VerticalOffset={scrollLateral.VerticalOffset:0.#}px, ScrollableHeight={scrollLateral.ScrollableHeight:0.#}px (esperado VerticalOffset≈ScrollableHeight)");
                        if (Math.Abs(scrollLateral.VerticalOffset - scrollLateral.ScrollableHeight) > 1)
                            Console.WriteLine($"FALLO: EXPLORATION_LAYOUT-GUARDARCANCELAR - ScrollToBottom no deja VerticalOffset≈ScrollableHeight en ExplorationSidebarScroll a {etiqueta}");

                        var botonesGuardarCancelar = placeholder == null ? null : Descendientes<System.Windows.Controls.Primitives.UniformGrid>(placeholder).FirstOrDefault();
                        if (botonesGuardarCancelar == null)
                            Console.WriteLine($"FALLO: EXPLORATION_LAYOUT-GUARDARCANCELAR - no se encuentra el UniformGrid real de Guardar/Cancelar a {etiqueta}");
                        else
                        {
                            var rectBotones = botonesGuardarCancelar.TransformToAncestor(window)
                                .TransformBounds(new Rect(0, 0, botonesGuardarCancelar.ActualWidth, botonesGuardarCancelar.ActualHeight));
                            bool alcanzable = rectBotones.Top >= -1 && rectBotones.Bottom <= window.ActualHeight + 1;
                            Console.WriteLine($"EXPLORATION_LAYOUT-GUARDARCANCELAR: {etiqueta} Guardar/Cancelar rect real tras ScrollToBottom={rectBotones}, ventana={window.ActualWidth:0}x{window.ActualHeight:0} -> alcanzable={alcanzable} (esperado True)");
                            if (!alcanzable)
                                Console.WriteLine($"FALLO: EXPLORATION_LAYOUT-GUARDARCANCELAR - Guardar/Cancelar quedan fuera de la ventana tras ScrollToBottom a {etiqueta}: {rectBotones}");
                            else
                                CapturaVentanaKeepQa(window, $"explorationlayout-chestinspector-{etiqueta}");
                        }

                        vm.Exploration.CancelEditingChestCommand.Execute(null);
                        DoEvents(); DoEvents();
                        scrollLateral.ScrollToTop();
                        DoEvents(); DoEvents();
                    }
                }
                catch (Exception exChest) { Console.WriteLine($"EXPLORATION_LAYOUT-CHESTINSPECTOR-EXCEPTION ({etiqueta}): " + exChest); }

                // ================= Modo WorldTools =================
                // ExploracionRediseno (rediseño de Exploracion>Mundo, 26-sep-2026, ver bitacora.md):
                // ya no hay 3 Expander simultaneos con IsExpanded forzado - las 3 subvistas
                // (WorldToolsSection, ExplorationViewModel) son EXCLUSIVAS, una visible cada vez, y
                // NINGUNA lleva scroll propio (contrato real: solo el ExplorationSidebarScroll
                // exterior). Se recorren las 3 explicitamente en vez de forzar un IsExpanded que ya
                // no existe, y el canario de "scroll interno" pasa de medir el ScrollViewer
                // MaxHeight de cada Expander a confirmar que YA NO QUEDA ningun ScrollViewer propio
                // dentro de ExplorationSidebarWorldToolsContent.
                try
                {
                    vm.Exploration.ShowSidebarWorldToolsCommand.Execute(null);
                    DoEvents(); DoEvents(); window.UpdateLayout();

                    // ADR-TERRAKEEP-027: WorldTools ahora vive en su propio UserControl
                    // (WorldToolsView) - FindName DOBLE (mismo patron ya usado por GUIA/ADR-021 y
                    // Compare/ADR-025): window.FindName("WorldToolsView") primero, despues
                    // worldToolsViewHost.FindName(...) sobre el NameScope propio del UserControl.
                    var worldToolsViewHost = window.FindName("WorldToolsView") as FrameworkElement;
                    var worldTools = worldToolsViewHost?.FindName("ExplorationSidebarWorldToolsContent") as FrameworkElement;
                    var subviewOverview = worldToolsViewHost?.FindName("ExplorationSidebarWorldToolsOverview") as FrameworkElement;
                    var subviewEdit = worldToolsViewHost?.FindName("ExplorationSidebarWorldToolsEdit") as FrameworkElement;
                    var subviewBestiary = worldToolsViewHost?.FindName("ExplorationSidebarWorldToolsBestiary") as FrameworkElement;

                    foreach (var seccion in new[] { WorldToolsSection.Overview, WorldToolsSection.Edit, WorldToolsSection.Bestiary })
                    {
                        if (seccion == WorldToolsSection.Bestiary && !vm.Exploration.HasBestiary)
                        {
                            Console.WriteLine($"EXPLORATION_LAYOUT-CONTENIDO: WorldTools/Bestiary a {etiqueta} - HasBestiary=false, omitido (mundo sin bestiario)");
                            continue;
                        }

                        vm.Exploration.WorldToolsSection = seccion;
                        DoEvents(); DoEvents(); window.UpdateLayout(); DoEvents(); DoEvents();

                        var subviewActiva = seccion switch
                        {
                            WorldToolsSection.Overview => subviewOverview,
                            WorldToolsSection.Edit => subviewEdit,
                            _ => subviewBestiary,
                        };
                        // Contenido real visible: Overview/Edit siempre llevan al menos un boton
                        // "Guardar" propio; Bestiario es de solo lectura (sin botones), su contenido
                        // real es el resumen de texto (BestiarySummaryText).
                        bool contenidoReal = subviewActiva != null && (seccion == WorldToolsSection.Bestiary
                            ? Descendientes<TextBlock>(subviewActiva).Any(t => t.IsVisible &&
                                (BindingOperations.GetBindingExpression(t, TextBlock.TextProperty)?.ParentBinding?.Path?.Path) == "Exploration.BestiarySummaryText")
                            : Descendientes<Button>(subviewActiva).Any(b => b.IsVisible));
                        bool worldToolsConContenido = worldTools != null && worldTools.IsVisible
                            && subviewActiva != null && subviewActiva.IsVisible && contenidoReal;
                        Console.WriteLine($"EXPLORATION_LAYOUT-CONTENIDO: WorldTools/{seccion} a {etiqueta} -> encontrado={worldTools != null}, IsVisible={worldTools?.IsVisible}, subvista encontrada={subviewActiva != null}, visible={subviewActiva?.IsVisible}, con contenido real={contenidoReal}");
                        if (!worldToolsConContenido)
                            Console.WriteLine($"FALLO: EXPLORATION_LAYOUT-CONTENIDO - WorldTools/{seccion} a {etiqueta} no muestra contenido real (sidebar vacio)");

                        ComprobarScrollHorizontal($"ExplorationSidebarScroll_WorldTools_{seccion}_{etiqueta}", scrollLateral);

                        // Contrato real del rediseno: ninguna subvista lleva scroll propio - las 3
                        // comparten el UNICO ExplorationSidebarScroll exterior. Canario equivalente
                        // al que antes media el ScrollViewer MaxHeight interno de cada Expander:
                        // cero ScrollViewer descendientes de ExplorationSidebarWorldToolsContent,
                        // salvo el PART_ContentHost interno de los TextBox de Spawn X/Y (mismo
                        // gotcha real ya documentado en AuditoriaTransicion.cs/Keep.Wpf.
                        // GeometriaWpf.ViewportDe - no es un contenedor de scroll de diseño).
                        int scrollsInternos = worldTools == null ? -1 : Descendientes<ScrollViewer>(worldTools)
                            .Count(sv => sv.TemplatedParent is not (System.Windows.Controls.Primitives.TextBoxBase or PasswordBox));
                        Console.WriteLine($"EXPLORATION_LAYOUT-SCROLLINTERNO: WorldTools/{seccion} a {etiqueta} -> ScrollViewer descendientes de ExplorationSidebarWorldToolsContent={scrollsInternos} (esperado 0 - maximo un scroll vertical real, el exterior)");
                        if (scrollsInternos != 0)
                            Console.WriteLine($"FALLO: EXPLORATION_LAYOUT-SCROLLINTERNO - WorldTools/{seccion} a {etiqueta} tiene {scrollsInternos} ScrollViewer propios (se esperaba 0 - contrato de un unico scroll vertical, el exterior)");

                        scrollLateral.ScrollToTop();
                        DoEvents(); DoEvents();
                        CapturaVentanaKeepQa(window, $"explorationlayout-worldtools-{seccion.ToString().ToLowerInvariant()}-{etiqueta}");
                    }

                    vm.Exploration.ShowSidebarBrowseCommand.Execute(null);
                    DoEvents(); DoEvents();
                }
                catch (Exception exWorld) { Console.WriteLine($"EXPLORATION_LAYOUT-WORLDTOOLS-EXCEPTION ({etiqueta}): " + exWorld); }
            }

            // ---- Restaura estado real para el resto del arnes ----
            if (vm.Exploration.EditingChest != null) vm.Exploration.CancelEditingChestCommand.Execute(null);
            vm.Exploration.SelectedCategory = WorldSearchCategory.All;
            vm.Exploration.ChestViewMode = 0;
            vm.Exploration.ShowSidebarBrowseCommand.Execute(null);
            FijarTamaño(window, anchoOriginal, altoOriginal);
            DoEvents(); DoEvents(); window.UpdateLayout();
            vm.SelectedTabIndex = tabOriginal;
            DoEvents();
        }
        catch (Exception ex) { Console.WriteLine("EXPLORATION_LAYOUT-EXCEPTION: " + ex); }
    }
}
