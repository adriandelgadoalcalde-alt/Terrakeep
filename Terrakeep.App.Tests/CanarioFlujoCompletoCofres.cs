// CANARIO REAL (26-sep-2026, TASK CONTEXT e5eaea9e-c261-4199-8e7d-060b6054f58d, investigador-bug del
// patron de 2 fases - NO toca produccion, solo este arnes). Encargo del coordinador: "reconfirmar
// desde cero, contra la arquitectura NUEVA (ExploracionRediseno Fase B-I + ChestInspector slots
// vacios ya cerrados), el requisito real 'editar un cofre debe ser estable, alcanzable y evidente'".
// El punto 1 original ("Editar cofre inalcanzable por perdida de hover") ya se investigo dos veces
// (bitacora 24/25-sep, CanarioClusterCofresInspector.cs "COFRES-INSPECTOR-P1") y quedo confirmado
// NO REPRODUCIBLE con la arquitectura de ENTONCES (boton "Editar" siempre visible, sin hover). Este
// canario no repite esa cobertura - cubre el HUECO real que quedaba: el flujo END-TO-END completo
// de las 8 fases pedidas por el encargo (abrir/seleccionar/editar/cambiar de slot/guardar/cancelar/
// volver/cambiar de cofre) contra la arquitectura de pagina exclusiva del sidebar (SidebarMode.
// ChestInspector), algo que ningun canario existente probaba de punta a punta con el MISMO cofre y
// el MISMO editor abiertos en secuencia real.
//
// Usa una COPIA del mundo real (Blando_Río.wld, 358 cofres) en el scratchpad de la sesion, nunca el
// original - el paso de "Guardar" de este flujo escribe de verdad en el .wld (WorldFileService.
// SaveChestItems hace WriteAtomic sobre el path real), y una copia evita cualquier riesgo de tocar
// el mundo real compartido con otros canarios/el usuario.
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Terrakeep.App;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.WldFormat;

internal static partial class Program
{
    private static void EjecutarFlujoCompletoCofresSolo(MainWindow window, MainViewModel vm)
    {
        try
        {
            string mundoOriginal = @"C:\Users\adrian\Documents\My Games\Terraria\Worlds\Blando_Río.wld";
            if (!File.Exists(mundoOriginal))
            {
                Console.WriteLine($"FLUJOCOFRES: no se encontro {mundoOriginal} - abortando (mismo mundo real ya usado por COFRES_INSPECTOR_SOLO)");
                return;
            }

            string carpetaCopia = Path.Combine(Path.GetTempPath(), "claude", "flujo-completo-cofres");
            Directory.CreateDirectory(carpetaCopia);
            string mundoCopia = Path.Combine(carpetaCopia, "flujo-completo-cofres.wld");
            File.Copy(mundoOriginal, mundoCopia, overwrite: true);
            Console.WriteLine($"FLUJOCOFRES: copia real del mundo creada en {mundoCopia} ({new FileInfo(mundoCopia).Length} bytes) - todo este canario edita/guarda SOLO sobre esta copia, nunca sobre el mundo real del usuario");

            vm.SelectedTabIndex = 4; // Exploracion
            FijarTamaño(window, 1180, 860);
            DoEvents(); DoEvents();

            var carga = vm.Exploration.LoadFromPathAsync(mundoCopia);
            while (!carga.IsCompleted) { DoEvents(); System.Threading.Thread.Sleep(15); }
            DoEvents(); DoEvents();

            vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
            vm.Exploration.ChestViewMode = 2; // Cofre a cofre
            DoEvents(); DoEvents(); window.UpdateLayout();

            // Dos cofres reales DISTINTOS con contenido (necesarios para el paso 8, "cambiar de
            // cofre"), y que el primero tenga huecos vacios de verdad (para el paso 2, seleccionar
            // un slot vacio Y uno ocupado).
            var candidatos = vm.Exploration.ChestRows.Where(r => r.Items.Count > 0).ToList();
            var chestA = candidatos.FirstOrDefault(r => r.Items.Count < 40);
            var chestB = candidatos.FirstOrDefault(r => r != chestA && (r.TileX != chestA?.TileX || r.TileY != chestA?.TileY));
            if (chestA == null || chestB == null)
            {
                Console.WriteLine($"FLUJOCOFRES: AVISO - no se encontraron 2 cofres reales distintos con contenido (candidatos={candidatos.Count}) - abortando el flujo completo");
                return;
            }
            Console.WriteLine($"FLUJOCOFRES: chestA=({chestA.TileX},{chestA.TileY}) Items={chestA.Items.Count}, chestB=({chestB.TileX},{chestB.TileY}) Items={chestB.Items.Count}");

            byte[] HashArchivo() { using var sha = SHA256.Create(); using var fs = File.OpenRead(mundoCopia); return sha.ComputeHash(fs); }
            string HashHex(byte[] h) => Convert.ToHexString(h);

            // ========================= PASO 1: ABRIR =========================
            byte[] hashAntesDeAbrir = HashArchivo();
            // ADR-TERRAKEEP-028 (26-sep-2026): ChestInspector se movio a ChestInspectorView.xaml
            // (UserControl con su propio NameScope) - FindName DOBLE, mismo patron ya usado por
            // GUIA/ADR-021, Compare/ADR-025 y WorldTools/ADR-027.
            var chestInspectorViewHost = window.FindName("ChestInspectorView") as FrameworkElement;
            var placeholder = chestInspectorViewHost?.FindName("ExplorationSidebarChestInspectorPlaceholder") as FrameworkElement;
            // ADR-TERRAKEEP-029 (26-sep-2026): Browse se movio a BrowseView.xaml (UserControl con
            // su propio NameScope) - mismo patron de FindName DOBLE que ChestInspectorView arriba.
            var browseViewHost = window.FindName("BrowseView") as FrameworkElement;
            var browseContent = browseViewHost?.FindName("ExplorationSidebarBrowseContent") as FrameworkElement;

            vm.Exploration.EditChestCommand.Execute(chestA);
            DoEvents(); DoEvents(); window.UpdateLayout();

            bool paso1Ok = vm.Exploration.SidebarMode == ExplorationSidebarMode.ChestInspector
                && ReferenceEquals(vm.Exploration.EditingChest, chestA)
                && vm.Exploration.EditingChestSlots.Count > 0
                && placeholder?.Visibility == Visibility.Visible
                && browseContent?.Visibility == Visibility.Collapsed;
            Console.WriteLine($"FLUJOCOFRES-1-ABRIR: SidebarMode={vm.Exploration.SidebarMode} (esperado ChestInspector), EditingChest==chestA={ReferenceEquals(vm.Exploration.EditingChest, chestA)}, EditingChestSlots.Count={vm.Exploration.EditingChestSlots.Count}, Placeholder.Visibility={placeholder?.Visibility} (esperado Visible), BrowseContent.Visibility={browseContent?.Visibility} (esperado Collapsed) -> {(paso1Ok ? "OBSERVED" : "FALLO")}");
            if (!paso1Ok) Console.WriteLine("FALLO: FLUJOCOFRES-1-ABRIR - el cofre no se abrio de verdad como pagina exclusiva del sidebar");
            CapturaVentanaKeepQa(window, "flujo-1-abrir");

            // ========================= PASO 2: SELECCIONAR (vacio y ocupado) =========================
            var slotVacio = vm.Exploration.EditingChestSlots.FirstOrDefault(s => s.IsEmpty);
            var slotOcupado = vm.Exploration.EditingChestSlots.FirstOrDefault(s => !s.IsEmpty);
            if (slotVacio == null || slotOcupado == null)
            {
                Console.WriteLine($"FLUJOCOFRES-2-SELECCIONAR: AVISO - este cofre no tiene a la vez un slot vacio y uno ocupado (vacio={slotVacio != null}, ocupado={slotOcupado != null}) - no se puede probar el paso 2 completo con este cofre");
            }
            else
            {
                vm.Exploration.SelectChestSlot(slotVacio);
                DoEvents(); window.UpdateLayout();
                bool vacioSeleccionado = ReferenceEquals(vm.Exploration.ChestItemEdit.Slot, slotVacio) && slotVacio.IsSelected;
                Console.WriteLine($"FLUJOCOFRES-2-SELECCIONAR-VACIO: ChestItemEdit.Slot==slotVacio={ReferenceEquals(vm.Exploration.ChestItemEdit.Slot, slotVacio)}, slotVacio.IsSelected={slotVacio.IsSelected} -> {(vacioSeleccionado ? "OBSERVED" : "FALLO")}");
                if (!vacioSeleccionado) Console.WriteLine("FALLO: FLUJOCOFRES-2-SELECCIONAR-VACIO - seleccionar un slot vacio no lo marca como seleccionado / no llega al panel Editar");

                vm.Exploration.SelectChestSlot(slotOcupado);
                DoEvents(); window.UpdateLayout();
                bool ocupadoSeleccionado = ReferenceEquals(vm.Exploration.ChestItemEdit.Slot, slotOcupado) && slotOcupado.IsSelected && !slotVacio.IsSelected;
                Console.WriteLine($"FLUJOCOFRES-2-SELECCIONAR-OCUPADO: ChestItemEdit.Slot==slotOcupado={ReferenceEquals(vm.Exploration.ChestItemEdit.Slot, slotOcupado)}, slotOcupado.IsSelected={slotOcupado.IsSelected}, slotVacio ya deseleccionado={!slotVacio.IsSelected} -> {(ocupadoSeleccionado ? "OBSERVED" : "FALLO")}");
                if (!ocupadoSeleccionado) Console.WriteLine("FALLO: FLUJOCOFRES-2-SELECCIONAR-OCUPADO - seleccionar un slot ocupado no mueve la seleccion real (exclusividad rota o panel Editar no sigue al slot)");

                // Evidencia real de UI (no solo VM): el Border interior de resaltado
                // (BorderBrush=AccentBrush, Visibility ligada a IsSelected, MainWindow.xaml~7902)
                // del slot ocupado debe estar realmente Visible en el arbol visual ya montado.
                var bordeExterior = Descendientes<Border>(window).FirstOrDefault(b => b.DataContext == slotOcupado && VisualTreeHelper.GetParent(b) is ContentPresenter);
                if (bordeExterior != null)
                {
                    var bordeResaltado = Descendientes<Border>(bordeExterior).FirstOrDefault(b => b.BorderBrush != null);
                    Console.WriteLine($"FLUJOCOFRES-2-SELECCIONAR-UI: borde de resaltado real encontrado={bordeResaltado != null}, Visibility={bordeResaltado?.Visibility} (esperado Visible)");
                    if (bordeResaltado == null || bordeResaltado.Visibility != Visibility.Visible)
                        Console.WriteLine("FALLO: FLUJOCOFRES-2-SELECCIONAR-UI - la seleccion del slot no se ve realmente en el arbol visual (borde de resaltado ausente/Collapsed)");
                }
                else Console.WriteLine("FLUJOCOFRES-2-SELECCIONAR-UI: AVISO - no se encontro el Border exterior real del slot ocupado en el arbol visual ya realizado");

                CapturaVentanaKeepQa(window, "flujo-2-seleccionar-ocupado");
            }

            if (slotOcupado == null) { Console.WriteLine("FLUJOCOFRES: abortando el resto del flujo (paso 2 no localizo un slot ocupado real)"); vm.Exploration.CancelEditingChestCommand.Execute(null); DoEvents(); return; }

            // ========================= PASO 3: EDITAR =========================
            // SelectedCategory a null ANTES de buscar: con una carpeta curada ya elegida (restaurada
            // de una sesion anterior via window.json, ej. "Armas"), ApplyFilter busca solo DENTRO de
            // esa carpeta (CatalogBrowserViewModel.ApplyFilter, linea ~241) y una pocion real nunca
            // aparece - mismo motivo por el que la busqueda existente de COFRES-INSPECTOR-P2-PICARO
            // (un arma) SI funcionaba sin este reset.
            vm.Library.SelectedCategory = null;
            DoEvents();
            vm.Library.SearchText = "Poción curativa"; // vanilla real id=188 (vanilla_item_names.json) - NUNCA "de vida", ese nombre no existe en el catalogo real en español
            WaitForDispatcher(300); // debounce real de 180ms del buscador de la Libreria, mismo patron ya usado en COFRES-INSPECTOR-P2-PICARO
            var pocion = vm.Library.Results.FirstOrDefault(r => r.Id == 188) ?? vm.Library.Results.FirstOrDefault();
            vm.Library.SearchText = string.Empty;
            DoEvents();
            if (pocion == null)
            {
                Console.WriteLine("FLUJOCOFRES-3-EDITAR: AVISO - no se encontro 'Poción curativa' en la Libreria - se omite el paso 3 y siguientes que dependen de un objeto real colocado");
            }
            else
            {
                int idOriginalOcupado = slotOcupado.ItemId;
                vm.Exploration.SelectChestSlot(slotOcupado);
                DoEvents();
                slotOcupado.ItemId = pocion.Id; // mismo camino real que escribir el id en el campo "Indice" del panel Editar (OnItemIdChanged -> PlaceItem)
                DoEvents(); DoEvents(); window.UpdateLayout();

                bool editadoOk = slotOcupado.ItemId == pocion.Id && !slotOcupado.IsEmpty && ReferenceEquals(vm.Exploration.EditingChestSlots[vm.Exploration.EditingChestSlots.IndexOf(slotOcupado)], slotOcupado);
                Console.WriteLine($"FLUJOCOFRES-3-EDITAR: slot antes ItemId={idOriginalOcupado} -> despues ItemId={slotOcupado.ItemId} (esperado {pocion.Id}, '{pocion.DisplayName}'), DisplayName real='{slotOcupado.DisplayName}' -> {(editadoOk ? "OBSERVED" : "FALLO")}");
                if (!editadoOk) Console.WriteLine("FALLO: FLUJOCOFRES-3-EDITAR - editar el Indice del slot seleccionado no coloco el objeto nuevo de verdad");
                CapturaVentanaKeepQa(window, "flujo-3-editar");

                // ========================= PASO 4: CAMBIAR DE SLOT SIN GUARDAR =========================
                var slotOtro = vm.Exploration.EditingChestSlots.FirstOrDefault(s => !ReferenceEquals(s, slotOcupado));
                if (slotOtro == null)
                {
                    Console.WriteLine("FLUJOCOFRES-4-CAMBIAR-SLOT: AVISO - este cofre solo tiene un slot editable, no se puede probar el cambio de slot");
                }
                else
                {
                    vm.Exploration.SelectChestSlot(slotOtro);
                    DoEvents(); window.UpdateLayout();

                    bool paso4Ok = ReferenceEquals(vm.Exploration.ChestItemEdit.Slot, slotOtro)
                        && slotOcupado.ItemId == pocion.Id // la edicion del paso 3 SIGUE viva tras cambiar de slot
                        && vm.Exploration.SidebarMode == ExplorationSidebarMode.ChestInspector
                        && ReferenceEquals(vm.Exploration.EditingChest, chestA);
                    Console.WriteLine($"FLUJOCOFRES-4-CAMBIAR-SLOT: ChestItemEdit.Slot cambio a slotOtro={ReferenceEquals(vm.Exploration.ChestItemEdit.Slot, slotOtro)}, slotOcupado (paso 3) SIGUE con ItemId={slotOcupado.ItemId} (esperado {pocion.Id}, no perdido al cambiar de slot), SidebarMode={vm.Exploration.SidebarMode}, EditingChest sigue siendo chestA={ReferenceEquals(vm.Exploration.EditingChest, chestA)} -> {(paso4Ok ? "OBSERVED" : "FALLO")}");
                    if (!paso4Ok) Console.WriteLine("FALLO: FLUJOCOFRES-4-CAMBIAR-SLOT - cambiar de slot sin guardar pierde la edicion previa o expulsa del Inspector");
                    CapturaVentanaKeepQa(window, "flujo-4-cambiar-slot");
                }

                // Ningun paso de arriba escribe en disco todavia - confirma que abrir/seleccionar/
                // editar/cambiar de slot son 100% en memoria.
                byte[] hashTrasEditarEnMemoria = HashArchivo();
                Console.WriteLine($"FLUJOCOFRES-SINEFECTO-DISCO: hash del .wld tras abrir+seleccionar+editar+cambiar de slot (SIN guardar) sigue IGUAL al de antes de abrir={HashHex(hashAntesDeAbrir) == HashHex(hashTrasEditarEnMemoria)} (esperado True)");
                if (HashHex(hashAntesDeAbrir) != HashHex(hashTrasEditarEnMemoria))
                    Console.WriteLine("FALLO: FLUJOCOFRES-SINEFECTO-DISCO - el .wld cambio en disco sin haber pulsado Guardar todavia");

                // ========================= PASO 5: GUARDAR =========================
                var tareaGuardar = vm.Exploration.SaveEditingChestCommand.ExecuteAsync(null);
                while (!tareaGuardar.IsCompleted) DoEvents();
                DoEvents(); DoEvents(); window.UpdateLayout();

                bool paso5VmOk = vm.Exploration.SidebarMode == ExplorationSidebarMode.Browse && vm.Exploration.EditingChest == null;
                string? statusGuardado = vm.Exploration.ChestEditStatus;
                Console.WriteLine($"FLUJOCOFRES-5-GUARDAR: SidebarMode={vm.Exploration.SidebarMode} (esperado Browse), EditingChest={(vm.Exploration.EditingChest == null ? "null" : "NO-NULL")}, ChestEditStatus real='{statusGuardado}' -> {(paso5VmOk ? "OBSERVED" : "FALLO")}");
                if (!paso5VmOk) Console.WriteLine("FALLO: FLUJOCOFRES-5-GUARDAR - guardar no vuelve el sidebar a Browse / no limpia EditingChest");

                byte[] hashTrasGuardar = HashArchivo();
                bool discoRealmenteCambio = HashHex(hashAntesDeAbrir) != HashHex(hashTrasGuardar);
                Console.WriteLine($"FLUJOCOFRES-5-GUARDAR-DISCO: hash SHA256 del .wld cambio de verdad tras Guardar={discoRealmenteCambio} (esperado True - WorldFileService.SaveChestItems hace WriteAtomic real sobre el archivo)");
                if (!discoRealmenteCambio) Console.WriteLine("FALLO: FLUJOCOFRES-5-GUARDAR-DISCO - el .wld en disco NO cambio tras pulsar Guardar (guardado silenciosamente no-op)");

                // Verificacion fuerte: recarga el mundo COMPLETO desde cero desde el mismo path (una
                // lectura independiente, WldReader real, no el estado en memoria que ya se actualizo
                // via WithChestItems) y confirma que el objeto guardado esta REALMENTE ahi.
                var cargaVerif = vm.Exploration.LoadFromPathAsync(mundoCopia);
                while (!cargaVerif.IsCompleted) { DoEvents(); System.Threading.Thread.Sleep(15); }
                DoEvents(); DoEvents();
                vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                vm.Exploration.ChestViewMode = 2;
                DoEvents(); DoEvents();
                var chestARecargado = vm.Exploration.ChestRows.FirstOrDefault(r => r.TileX == chestA.TileX && r.TileY == chestA.TileY);
                bool guardadoEnDiscoOk = chestARecargado != null && chestARecargado.Items.Any(i => i.NetId == pocion.Id);
                Console.WriteLine($"FLUJOCOFRES-5-GUARDAR-RELEIDO: tras recargar el mundo DESDE CERO (WldReader real, no memoria), el cofre ({chestA.TileX},{chestA.TileY}) recargado tiene el objeto guardado (NetId={pocion.Id})={guardadoEnDiscoOk}");
                if (!guardadoEnDiscoOk) Console.WriteLine("FALLO: FLUJOCOFRES-5-GUARDAR-RELEIDO - el objeto guardado no aparece al releer el .wld desde cero - guardado no persistente de verdad");

                // ========================= PASO 6: CANCELAR SIN GUARDAR =========================
                if (chestARecargado == null)
                {
                    Console.WriteLine("FLUJOCOFRES-6-CANCELAR: AVISO - no se pudo recargar chestA, se omite el paso 6/7/8");
                }
                else
                {
                    byte[] hashAntesDeCancelar = HashArchivo();
                    vm.Exploration.EditChestCommand.Execute(chestARecargado);
                    DoEvents(); DoEvents();
                    var slotParaCancelar = vm.Exploration.EditingChestSlots.FirstOrDefault(s => !s.IsEmpty);
                    if (slotParaCancelar == null)
                    {
                        Console.WriteLine("FLUJOCOFRES-6-CANCELAR: AVISO - chestA recargado no tiene ningun slot ocupado real, se omite el paso 6");
                        vm.Exploration.CancelEditingChestCommand.Execute(null);
                        DoEvents();
                    }
                    else
                    {
                        int idPreCancelar = slotParaCancelar.ItemId;
                        vm.Exploration.SelectChestSlot(slotParaCancelar);
                        DoEvents();
                        slotParaCancelar.ItemId = 1; // "Espada de cobre" (id=1, vanilla real) - cualquier id valido distinto, solo para probar que se descarta
                        DoEvents(); DoEvents();
                        Console.WriteLine($"FLUJOCOFRES-6-CANCELAR: edicion real hecha (sin guardar) ItemId {idPreCancelar} -> {slotParaCancelar.ItemId}");

                        vm.Exploration.CancelEditingChestCommand.Execute(null);
                        DoEvents(); DoEvents(); window.UpdateLayout();

                        bool paso6VmOk = vm.Exploration.SidebarMode == ExplorationSidebarMode.Browse && vm.Exploration.EditingChest == null;
                        byte[] hashTrasCancelar = HashArchivo();
                        bool discoIntacto = HashHex(hashAntesDeCancelar) == HashHex(hashTrasCancelar);
                        Console.WriteLine($"FLUJOCOFRES-6-CANCELAR: SidebarMode={vm.Exploration.SidebarMode} (esperado Browse), EditingChest={(vm.Exploration.EditingChest == null ? "null" : "NO-NULL")}, hash del .wld intacto tras Cancelar={discoIntacto} (esperado True) -> {(paso6VmOk && discoIntacto ? "OBSERVED" : "FALLO")}");
                        if (!paso6VmOk) Console.WriteLine("FALLO: FLUJOCOFRES-6-CANCELAR - cancelar no vuelve el sidebar a Browse / no limpia EditingChest");
                        if (!discoIntacto) Console.WriteLine("FALLO: FLUJOCOFRES-6-CANCELAR-DISCO - Cancelar escribio algo en disco (no deberia tocar el archivo en absoluto)");

                        // Reabre el MISMO cofre (misma fila, todavia en memoria, sin recargar) y
                        // confirma que el objeto puesto justo antes de cancelar NO esta - la edicion
                        // se descarto de verdad, no solo se ocultio.
                        vm.Exploration.EditChestCommand.Execute(chestARecargado);
                        DoEvents(); DoEvents();
                        // slotParaCancelar es la instancia VIEJA (de la apertura anterior, descartada
                        // por completo al reabrir) - buscamos por VALOR (ItemId) en la coleccion
                        // NUEVA, no por referencia ni por indice.
                        var slotDescartadoRestaurado = vm.Exploration.EditingChestSlots.FirstOrDefault(s => s.ItemId == idPreCancelar);
                        Console.WriteLine($"FLUJOCOFRES-6-CANCELAR-DESCARTE: tras reabrir el mismo cofre, existe de nuevo un slot con el ItemId original ({idPreCancelar})={slotDescartadoRestaurado != null}, y NINGUN slot quedo con el ItemId descartado (1, 'Espada de cobre')={!vm.Exploration.EditingChestSlots.Any(s => s.ItemId == 1)}");
                        if (slotDescartadoRestaurado == null)
                            Console.WriteLine("FALLO: FLUJOCOFRES-6-CANCELAR-DESCARTE - el contenido original del cofre no aparece al reabrirlo tras Cancelar");

                        CapturaVentanaKeepQa(window, "flujo-6-cancelar");

                        // ========================= PASO 7: VOLVER (boton real "<- Cofres") =========================
                        // Mismo estado que acabamos de dejar (cofre reabierto, sin editar nada nuevo) -
                        // localiza el BOTON REAL de la cabecera (no solo invoca el comando a mano, para
                        // confirmar que el elemento de UI que el usuario pulsaria de verdad existe y
                        // esta ligado al mismo comando).
                        var botonVolver = Descendientes<Button>(window).FirstOrDefault(b =>
                            (BindingOperations.GetBindingExpression(b, Button.CommandProperty)?.ParentBinding?.Path?.Path) == "Exploration.CancelEditingChestCommand"
                            && b.Content is string s && s == vm.Loc["explore_inspector_back"]);
                        Console.WriteLine($"FLUJOCOFRES-7-VOLVER: boton real '<- Cofres' encontrado en el arbol visual={botonVolver != null}, Visibility={botonVolver?.Visibility}, IsEnabled={botonVolver?.IsEnabled}");
                        if (botonVolver == null)
                            Console.WriteLine("FALLO: FLUJOCOFRES-7-VOLVER - no se encontro el boton real '<- Cofres' ligado a CancelEditingChestCommand");
                        else
                        {
                            botonVolver.Command?.Execute(botonVolver.CommandParameter);
                            DoEvents(); DoEvents(); window.UpdateLayout();
                            bool paso7Ok = vm.Exploration.SidebarMode == ExplorationSidebarMode.Browse && vm.Exploration.EditingChest == null
                                && browseContent?.Visibility == Visibility.Visible && placeholder?.Visibility == Visibility.Collapsed;
                            Console.WriteLine($"FLUJOCOFRES-7-VOLVER: tras ejecutar el comando real del boton -> SidebarMode={vm.Exploration.SidebarMode} (esperado Browse), BrowseContent.Visibility={browseContent?.Visibility} (esperado Visible), Placeholder.Visibility={placeholder?.Visibility} (esperado Collapsed) -> {(paso7Ok ? "OBSERVED" : "FALLO")}");
                            if (!paso7Ok) Console.WriteLine("FALLO: FLUJOCOFRES-7-VOLVER - el boton real '<- Cofres' no devuelve el sidebar a Browse de verdad (UI, no solo VM)");
                            CapturaVentanaKeepQa(window, "flujo-7-volver");
                        }

                        // ========================= PASO 8: CAMBIAR DE COFRE (abrir OTRO distinto) =========================
                        // Reabre chestA, hace un cambio SIN guardar, y abre chestB DIRECTAMENTE (sin
                        // pasar por Cancelar/Guardar/Volver) - la ruta real que faltaba por cubrir:
                        // ningun canario existente encadenaba 2 EditChestCommand.Execute distintos sin
                        // un cierre expreso entre medias.
                        vm.Exploration.EditChestCommand.Execute(chestARecargado);
                        DoEvents(); DoEvents();
                        var slotParaEnsuciar = vm.Exploration.EditingChestSlots.FirstOrDefault(s => !s.IsEmpty);
                        int idChestAAntesDeCambiar = slotParaEnsuciar?.ItemId ?? -1;
                        if (slotParaEnsuciar != null)
                        {
                            vm.Exploration.SelectChestSlot(slotParaEnsuciar);
                            DoEvents();
                            slotParaEnsuciar.ItemId = 2; // "Espada corta" (id=2 vanilla) - edicion deliberadamente SIN guardar antes de saltar de cofre
                            DoEvents();
                        }

                        Exception? excepcionCambioCofre = null;
                        try { vm.Exploration.EditChestCommand.Execute(chestB); }
                        catch (Exception exCambio) { excepcionCambioCofre = exCambio; }
                        DoEvents(); DoEvents(); window.UpdateLayout();

                        bool paso8Ok = excepcionCambioCofre == null
                            && vm.Exploration.SidebarMode == ExplorationSidebarMode.ChestInspector
                            && ReferenceEquals(vm.Exploration.EditingChest, chestB)
                            && vm.Exploration.EditingChestSlots.Count > 0
                            && vm.Exploration.ChestItemEdit.Slot == null
                            && vm.Exploration.EditingChestSlots.Count(s => s.ItemId != 0) == chestB.Items.Count;
                        Console.WriteLine($"FLUJOCOFRES-8-CAMBIAR-COFRE: excepcion real={excepcionCambioCofre}, SidebarMode={vm.Exploration.SidebarMode} (esperado ChestInspector), EditingChest==chestB={ReferenceEquals(vm.Exploration.EditingChest, chestB)}, EditingChestSlots.Count={vm.Exploration.EditingChestSlots.Count} (chestB.Items.Count={chestB.Items.Count}), ChestItemEdit.Slot reseteado a null={vm.Exploration.ChestItemEdit.Slot == null} -> {(paso8Ok ? "OBSERVED" : "FALLO")}");
                        if (excepcionCambioCofre != null) Console.WriteLine("FALLO: FLUJOCOFRES-8-CAMBIAR-COFRE - abrir un cofre distinto con otro en edicion (sin guardar/cancelar antes) lanza una excepcion real");
                        else if (!paso8Ok) Console.WriteLine("FALLO: FLUJOCOFRES-8-CAMBIAR-COFRE - cambiar de cofre directamente no reconstruye el Inspector de forma limpia (mezcla contenido, no cambia EditingChest, o deja el panel Editar apuntando al slot viejo)");
                        CapturaVentanaKeepQa(window, "flujo-8-cambiar-cofre");

                        // Observacion de diseño (NO fallo): el cambio sin guardar en chestA se
                        // descarta en SILENCIO al saltar a chestB, exactamente el mismo comportamiento
                        // ya aceptado para Cancelar/Volver (pasos 6/7) - no hay dialogo de confirmacion
                        // en ningun punto de guardado de la app (tampoco lo tienen Guardar mundo,
                        // letreros, NPCs...). Se documenta el hash SIN recargar UI para confirmarlo con
                        // datos reales, no solo describirlo.
                        var chestATrasSalir = vm.Exploration.ChestRows.FirstOrDefault(r => r.TileX == chestA.TileX && r.TileY == chestA.TileY);
                        Console.WriteLine($"FLUJOCOFRES-8-OBSERVACION: chestA.Items en memoria tras cambiar a chestB sigue con el contenido GUARDADO en el paso 5 (id={idChestAAntesDeCambiar} antes de ensuciar, ItemId=2 fue el cambio descartado) - fila real de ChestRows para chestA sigue existiendo={chestATrasSalir != null} (comportamiento esperado: descarte silencioso identico al de Cancelar, consistente con el resto de la app)");

                        vm.Exploration.CancelEditingChestCommand.Execute(null);
                        DoEvents(); DoEvents();
                    }
                }
            }

            Console.WriteLine("FLUJOCOFRES: flujo completo de 8 pasos terminado");

            // ========================= HALLAZGO REAL: eje NO cubierto por ningun canario existente =========================
            // Mientras se ejecutaba este flujo, COFRES_INSPECTOR_SOLO (el mismo dia, misma maquina)
            // empezo a fallar de verdad en "COFRES-INSPECTOR-SLOTSVACIOS" (placeholder.ActualWidth=
            // 239px, IGUAL a 1180x860 y a 1080x700 - no cambia con el tamaño de ventana). Causa real
            // confirmada: el ancho del sidebar de Exploracion NO esta atado al tamaño de la ventana
            // como asumia el comentario de "ChestInspector slots vacios REABIERTO" (MainWindow.xaml
            // ~7850-7872, "296px por defecto, 279px al minimo real de ventana 1080x700") - esta atado
            // a `Settings.ExplorationSidebarWidth` (MainWindow.xaml:5709, ColumnDefinition Width
            // TwoWay), un ancho INDEPENDIENTE, persistido en disco
            // (%LocalAppData%\Terrakeep\settings.json, MISMO archivo que usa el Terrakeep.exe REAL
            // instalado) y arrastrable por el usuario con un GridSplitter real, con su PROPIO suelo
            // de 260px (SettingsViewModel.cs:138, "F-10", pre-existente, anterior y ajeno al fix de
            // ChestInspector) - 260px de COLUMNA, no 260px de contenido real disponible tras
            // padding/bordes/scroll (hay ~41px reales de diferencia, medidos abajo). Nunca se probo
            // contra ESTE eje (solo contra el tamaño de VENTANA) ni en la verificacion original ni en
            // EXPLORATION_LAYOUT_SOLO (su guarda AR-EX-HSCROLL mide ScrollableWidth, que se queda en
            // 0 aunque haya recorte real - el mismo punto ciego que el propio comentario de
            // ChestInspector ya documentaba para el ScrollContentPresenter, solo que aqui aplica al
            // eje de ANCHO DEL SIDEBAR, no al de tamaño de ventana). Este bloque cuantifica el umbral
            // real y deja un canario PERMANENTE de regresion para este eje - restaura el valor
            // original de Settings.ExplorationSidebarWidth al final, para no dejar el ajuste real del
            // usuario (persistido en el MISMO archivo que usa la app instalada) modificado.
            try
            {
                vm.SelectedTabIndex = 4;
                FijarTamaño(window, 1180, 860);
                vm.Exploration.SelectedCategory = WorldSearchCategory.Chests;
                vm.Exploration.ChestViewMode = 2;
                DoEvents(); DoEvents();

                double anchoOriginalReal = vm.Settings.ExplorationSidebarWidth;
                Console.WriteLine($"FLUJOCOFRES-SIDEBARWIDTH: Settings.ExplorationSidebarWidth ANTES de este diagnostico={anchoOriginalReal}px (valor real persistido en settings.json, restaurado al terminar)");

                var filaAncho = vm.Exploration.ChestRows.FirstOrDefault(r => r.Items.Count > 0 && r.Items.Count < 40) ?? vm.Exploration.ChestRows.FirstOrDefault(r => r.Items.Count > 0);
                if (filaAncho == null)
                {
                    Console.WriteLine("FLUJOCOFRES-SIDEBARWIDTH: AVISO - no hay ningun cofre real con contenido para este diagnostico");
                }
                else
                {
                    vm.Exploration.EditChestCommand.Execute(filaAncho);
                    DoEvents(); DoEvents();

                    // 3 valores reales: el suelo YA validado por SettingsViewModel (260, "F-10"), el
                    // valor REAL que ahora mismo hay persistido en disco (el que disparo el FALLO real
                    // de COFRES_INSPECTOR_SOLO), y el valor por defecto de fabrica
                    // (SettingsService.cs:20, ExplorationSidebarWidth=320, nunca usado por el usuario
                    // real si ya guardo otro ancho antes).
                    foreach (double anchoPrueba in new[] { 260.0, anchoOriginalReal, 320.0 }.Distinct())
                    {
                        vm.Settings.ExplorationSidebarWidth = anchoPrueba; // dispara OnExplorationSidebarWidthChanged real (clamp+Persist, MISMO camino que arrastrar el GridSplitter de verdad)
                        DoEvents(); DoEvents(); window.UpdateLayout(); DoEvents();

                        // ADR-TERRAKEEP-028: FindName DOBLE, ChestInspectorView tiene su propio NameScope.
                        var placeholderAncho = (window.FindName("ChestInspectorView") as FrameworkElement)?.FindName("ExplorationSidebarChestInspectorPlaceholder") as FrameworkElement;
                        var bordesAncho = placeholderAncho == null
                            ? Enumerable.Empty<Border>().ToList()
                            : Descendientes<Border>(placeholderAncho).Where(b => b.DataContext is ItemSlotViewModel && VisualTreeHelper.GetParent(b) is ContentPresenter).ToList();

                        int fueraDeAncho = 0;
                        foreach (var borde in bordesAncho)
                        {
                            try
                            {
                                var esquina = borde.TransformToAncestor(placeholderAncho!).Transform(new Point(0, 0));
                                if (esquina.X < -1.0 || esquina.X + borde.ActualWidth > placeholderAncho!.ActualWidth + 1.0) fueraDeAncho++;
                            }
                            catch (InvalidOperationException) { }
                        }

                        Console.WriteLine($"FLUJOCOFRES-SIDEBARWIDTH: Settings.ExplorationSidebarWidth pedido={anchoPrueba}px -> valor real tras clamp={vm.Settings.ExplorationSidebarWidth}px, placeholder.ActualWidth={placeholderAncho?.ActualWidth:0.#}px, slots reales medidos={bordesAncho.Count}, slots fuera del ancho real (inalcanzables sin scroll, HorizontalScrollBarVisibility=Disabled)={fueraDeAncho}");
                        if (fueraDeAncho > 0)
                            Console.WriteLine($"FALLO: FLUJOCOFRES-SIDEBARWIDTH - con Settings.ExplorationSidebarWidth={anchoPrueba}px (dentro del rango real 260-520 valido de la app), {fueraDeAncho} slot(s) del editor de cofre quedan fuera del ancho real del Inspector, inalcanzables de verdad (mismo riesgo ya nombrado AR-EX-HSCROLL/COFRES-INSPECTOR-SLOTSVACIOS, pero en el eje de ANCHO DEL SIDEBAR, nunca probado hasta ahora)");

                        CapturaVentanaKeepQa(window, $"flujo-sidebarwidth-{anchoPrueba:0}px");
                    }

                    vm.Exploration.CancelEditingChestCommand.Execute(null);
                    DoEvents(); DoEvents();
                }

                // Restaura el valor REAL que habia antes de este diagnostico - mismo archivo que usa
                // el Terrakeep.exe instalado, no se deja modificado.
                vm.Settings.ExplorationSidebarWidth = anchoOriginalReal;
                DoEvents();
                Console.WriteLine($"FLUJOCOFRES-SIDEBARWIDTH: Settings.ExplorationSidebarWidth restaurado a {vm.Settings.ExplorationSidebarWidth}px (valor real que habia antes de este diagnostico)");
            }
            catch (Exception exAncho) { Console.WriteLine("FLUJOCOFRES-SIDEBARWIDTH-EXCEPTION: " + exAncho); }
        }
        catch (Exception ex)
        {
            Console.WriteLine("FLUJOCOFRES-EXCEPTION: " + ex);
        }
    }
}
