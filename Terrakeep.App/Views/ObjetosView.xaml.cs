using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Terrakeep.App.Controls;
using Terrakeep.App.Services;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Model;

namespace Terrakeep.App.Views;

// ADR-TERRAKEEP-016/031 (27-sep-2026): DECIMOQUINTA y ULTIMA extraccion real de MainWindow.xaml a
// un UserControl - cierra el plan completo de ADR-TERRAKEEP-016. Ver el comentario real de
// ObjetosView.xaml para el analisis completo de dependencias cruzadas. Code-behind exclusivo de
// Objetos movido tal cual (mismo mecanismo ya usado por las 14 rondas anteriores: "_viewModel" de
// MainWindow -> "ViewModel" ((MainViewModel)DataContext), "this" (Window) -> Window.GetWindow(this)
// donde hace falta un propietario real de dialogo). StartCardDrag (usado por
// OnLibraryCardMouseMove/OnItemSlotMouseMove) NO se duplico aqui - lo necesitan por igual los
// gemelos reales de Buffs (que se quedan en MainWindow.xaml.cs), asi que se extrajo a un helper
// compartido nuevo: Terrakeep.App/Controls/DragDropSupport.cs (internal static, mismo ensamblado).
// OnCommitTextOnEnter SI se duplica aqui (handler trivial sin estado, MainWindow.xaml.cs conserva
// su propia copia porque el TextBox de "nombre del personaje" de la cabecera global tambien la usa
// - mismo mecanismo ya usado por ChestInspectorView/ADR-028). GetDoubleClickTime (P/Invoke de una
// sola linea) tambien se duplica por el mismo motivo - MainWindow.xaml.cs la sigue necesitando para
// el gemelo real de Buffs (OnBuffLibraryCardClick).
public partial class ObjetosView : UserControl
{
    private static LocalizationService Loc => LocalizationService.Instance;

    // H5-12 (quinta auditoria de Opus): tiempo real de doble clic del propio sistema operativo -
    // SystemParameters (WPF) no expone este valor (solo existe en WinForms). Misma API Win32 real
    // ya usada por MainWindow.xaml.cs (duplicada aqui, ver el comentario de cabecera de esta clase).
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetDoubleClickTime();

    public ObjetosView()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    // Ctrl+F (OnWindowKeyDown, MainWindow.xaml.cs) enfoca LibrarySearchBox cuando la pestaña
    // interna activa es Objetos - gancho publico minimo, mismo patron que
    // BrowseView.FocusWorldSearchBox()/ADR-029 (BuffLibrarySearchBox, de Buffs sin extraer, sigue
    // resolviendose directo en MainWindow.xaml.cs).
    public void FocusLibrarySearchBox()
    {
        LibrarySearchBox.Focus();
        LibrarySearchBox.SelectAll();
    }

    // Duplicado local NECESARIO (mismo motivo que ChestInspectorItemEditTemplate/
    // ChestInspectorBoolToVis en el XAML, ver ADR-TERRAKEEP-028): OnCommitTextOnEnter real de
    // MainWindow.xaml.cs (T-17), usado por los TextBox de ItemEditTemplate (Indice/Prefijo) que
    // ahora viven aqui. MainWindow.xaml.cs conserva su propia copia porque el TextBox de "nombre
    // del personaje" de la cabecera global tambien la usa - handler trivial y sin estado, sin
    // dependencia de ViewModel, cero riesgo real de divergencia entre las dos copias.
    private void OnCommitTextOnEnter(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox textBox) return;
        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    // NAV123 (25-sep-2026, aplicador-fix, TASK CONTEXT e5eaea9e-c261-4199-8e7d-060b6054f58d):
    // retirados OnObjetosBoardScrollChanged (cabecera pegajosa + sincronizacion inversa por
    // offset) y ScrollToObjetosSection (scroll medido con TranslatePoint, clampable por
    // ScrollViewer - la causa real del bug "toggle 3 desincronizado"). Las 3 paginas de
    // ObjetosPageHost (MainWindow.xaml) son ahora exclusivas por Visibility ligada directamente a
    // MainViewModel.ObjetosSubTabIndex (EnumEqualsToVis) - el clic solo tiene que escribir ese
    // indice, sin medir ni mover nada.
    private void OnObjetosNavToggleClick(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag && int.TryParse(tag, out int indice))
            ViewModel.RequestObjetosSection(indice);
    }

    // H5-03 (quinta auditoria de Opus): "guardar/cargar conjuntos de objetos" - el dialogo real
    // de fichero vive aqui (MainViewModel es headless de verdad, mismo criterio ya establecido
    // en OnLoadClick/OnLoadWorldClick). Alcance de esta pasada: Inventario y Almacenes (el
    // almacen SELECCIONADO ahora mismo, Current) - Equipamiento/loadouts quedan fuera,
    // documentado en bitacora.md.
    private void OnSaveInventorySetClick(object sender, RoutedEventArgs e) => SaveItemSetDialog(ViewModel.InventoryContainer);
    private void OnLoadInventorySetClick(object sender, RoutedEventArgs e) => LoadItemSetDialog(ViewModel.InventoryContainer, append: false);
    private void OnAppendInventorySetClick(object sender, RoutedEventArgs e) => LoadItemSetDialog(ViewModel.InventoryContainer, append: true);

    private void OnSaveStorageSetClick(object sender, RoutedEventArgs e) => SaveItemSetDialog(ViewModel.StorageGroup?.Current);
    private void OnLoadStorageSetClick(object sender, RoutedEventArgs e) => LoadItemSetDialog(ViewModel.StorageGroup?.Current, append: false);
    private void OnAppendStorageSetClick(object sender, RoutedEventArgs e) => LoadItemSetDialog(ViewModel.StorageGroup?.Current, append: true);

    private void SaveItemSetDialog(ContainerViewModel? container)
    {
        if (container == null) return;
        var dialog = new SaveFileDialog
        {
            // Ronda de idioma del 6-sep-2026: los dos literales estaban a pelo aqui aunque sus
            // claves ("dlg_save_item_set_title" y "dlg_filter_item_set") YA existian en los dos
            // diccionarios desde la ronda anterior - se crearon y nunca se llegaron a enchufar.
            Title = Loc["dlg_save_item_set_title"],
            Filter = Loc["dlg_filter_item_set"],
            FileName = container.Key + ".json",
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) ViewModel.SaveItemSet(container, dialog.FileName);
    }

    private void LoadItemSetDialog(ContainerViewModel? container, bool append)
    {
        if (container == null) return;
        var dialog = new OpenFileDialog
        {
            Title = Loc[append ? "dlg_add_item_set" : "dlg_load_item_set"],
            Filter = Loc["dlg_filter_item_set"],
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true) ViewModel.LoadItemSet(container, dialog.FileName, append);
    }

    // Arrastrar y soltar (pedido explicito 1-sep-2026: "se puede arrastar para poder ir
    // poniendo en el inventario o en accesorios pero todo se visualiza en sprites"). Deteccion
    // de arrastre estandar de WPF: se guarda la posicion en el boton-abajo, y solo se arranca
    // DoDragDrop de verdad si el raton se mueve mas alla del umbral del sistema con el boton
    // aun pulsado - asi un simple clic (sin mover el raton) sigue llegando normal a los
    // botones de dentro de la tarjeta (Cambiar/★/✎/✕), que ya funcionaban por clic.
    private Point? _dragStartLibrary;
    private Point? _dragStartSlot;

    private void OnLibraryCardMouseDown(object sender, MouseButtonEventArgs e) => _dragStartLibrary = e.GetPosition(null);

    private void OnLibraryCardMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragStartLibrary is not { } start) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _dragStartLibrary = null;

        // C-11 (informe de pulido final, cierra L4): OnItemSlotDragOver/Drop devuelven Move para
        // el destino aceptado (ver OnItemSlotDragOver, mas abajo) - si aqui solo se permite Copy, ese Move queda FUERA
        // del conjunto de efectos permitidos por el ORIGEN del arrastre, lo que en algunos temas/
        // configuraciones de Windows hace que el cursor muestre "prohibido" durante todo el
        // arrastre pese a que Drop SI funciona. Copy|Move cubre los dos (arrastrar SIEMPRE coloca
        // una copia real del catalogo, nunca "mueve" nada de la Libreria - Move es solo lo que el
        // slot destino declara aceptar).
        if (sender is FrameworkElement { DataContext: LibraryItemViewModel item } element)
            DragDropSupport.StartCardDrag(element, new DataObject(typeof(LibraryItemViewModel), item));
    }

    // H5-12 (quinta auditoria de Opus): "un clic en una tarjeta de la Libreria no hace
    // absolutamente nada... la unica via real es arrastrar, gesto mas caro que nada anuncia".
    // Clic simple: coloca en el slot seleccionado ahora mismo en el panel Editar (ItemEdit.Slot)
    // - si lo rechaza, PlaceItem ya deja su propio RejectionMessage real, visible en ese mismo
    // panel. Doble clic: al primer hueco libre del Inventario, sin necesitar ninguna seleccion
    // previa. _dragStartLibrary es null aqui cuando el gesto YA se resolvio como un arrastre real
    // (OnLibraryCardMouseMove lo vacia justo antes de DoDragDrop) - sin esta guarda, soltar tras
    // arrastrar colocaria el objeto DOS veces (una via el Drop real, otra via este clic).
    private void OnLibraryCardClick(object sender, MouseButtonEventArgs e)
    {
        if (_dragStartLibrary is null) return; // ya se resolvio como un arrastre real, no un clic
        _dragStartLibrary = null;
        if (sender is not FrameworkElement { DataContext: LibraryItemViewModel item }) return;

        if (e.ClickCount >= 2)
        {
            _libraryClickTimer?.Stop();
            _pendingLibraryClickItem = null;
            ViewModel.PlaceInFirstFreeInventorySlot(item.Id);
            return;
        }

        // El primer clic de un futuro doble clic YA llega aqui con ClickCount=1 (WPF no junta
        // los dos hasta el segundo) - sin esperar el tiempo real del sistema, colocaria en el
        // slot seleccionado Y, un instante despues, el doble clic colocaria TAMBIEN en el primer
        // hueco libre: dos colocaciones reales por un solo gesto. DispatcherTimer con el tiempo
        // real de doble clic del propio sistema operativo (SystemParameters.DoubleClickTime,
        // nunca un numero inventado) - se cancela si un segundo clic llega a tiempo.
        _pendingLibraryClickItem = item;
        _libraryClickTimer ??= new System.Windows.Threading.DispatcherTimer();
        _libraryClickTimer.Stop();
        _libraryClickTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(GetDoubleClickTime(), 1));
        _libraryClickTimer.Tick -= OnLibraryClickTimerTick;
        _libraryClickTimer.Tick += OnLibraryClickTimerTick;
        _libraryClickTimer.Start();
    }

    private System.Windows.Threading.DispatcherTimer? _libraryClickTimer;
    private LibraryItemViewModel? _pendingLibraryClickItem;

    private void OnLibraryClickTimerTick(object? sender, EventArgs e)
    {
        _libraryClickTimer!.Stop();
        if (_pendingLibraryClickItem is not { } item) return;
        _pendingLibraryClickItem = null;
        var target = ViewModel.ItemEdit.Slot;
        if (target == null) return; // el tooltip de la tarjeta ya avisa de que hace falta elegir un hueco antes
        target.PlaceItem(item.Id);
    }

    // Un clic en cualquier slot (incluidos los botones ★/✕/Cambiar de dentro, ya que este
    // manejador es PreviewMouseLeftButtonDown en el Border completo) lo selecciona para el
    // panel "Editar" compartido - pedido explicito 1-sep-2026.
    private void OnItemSlotMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartSlot = e.GetPosition(null);
        if (sender is FrameworkElement { DataContext: ItemSlotViewModel slot })
            ViewModel.SelectSlot(slot);
    }

    private void OnItemSlotMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragStartSlot is not { } start) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _dragStartSlot = null;

        if (sender is FrameworkElement { DataContext: ItemSlotViewModel { IsEmpty: false } slot } element)
            DragDropSupport.StartCardDrag(element, new DataObject(typeof(ItemSlotViewModel), slot), DragDropEffects.Move);
    }

    // Retroalimentación de la restricción de slot MIENTRAS se arrastra, antes de soltar
    // (consulta a Opus, sexta pasada: "el propio cursor del sistema se convierte en el
    // símbolo de prohibido... es el idioma del SO, no el tuyo, y llega ANTES de soltar" - la
    // capa mas barata que existe, cero chrome nuevo). Cubre los dos origenes reales de
    // arrastre (tarjeta de la Libreria y otro slot).
    private void OnItemSlotDragOver(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ItemSlotViewModel targetSlot }) return;

        bool accepted;
        if (e.Data.GetDataPresent(typeof(LibraryItemViewModel)) && e.Data.GetData(typeof(LibraryItemViewModel)) is LibraryItemViewModel libraryItem)
        {
            accepted = targetSlot.AcceptsItem(libraryItem.Id);
        }
        else if (e.Data.GetDataPresent(typeof(ItemSlotViewModel)) && e.Data.GetData(typeof(ItemSlotViewModel)) is ItemSlotViewModel sourceSlot)
        {
            // El intercambio mueve el objeto en AMBAS direcciones - hay que validar que cada
            // slot acepta lo que le va a llegar, no solo el destino.
            accepted = targetSlot.AcceptsItem(sourceSlot.Item.Id) && sourceSlot.AcceptsItem(targetSlot.Item.Id);
        }
        else
        {
            accepted = true;
        }

        e.Effects = accepted ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    // Soltar una tarjeta de la Libreria coloca ese objeto (igual que "Cambiar objeto"); soltar
    // otro slot arrastrado los intercambia entero (prefijo/cantidad/favorito incluidos).
    private void OnItemSlotDrop(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ItemSlotViewModel targetSlot }) return;

        if (e.Data.GetDataPresent(typeof(LibraryItemViewModel)) && e.Data.GetData(typeof(LibraryItemViewModel)) is LibraryItemViewModel libraryItem)
        {
            targetSlot.PlaceItem(libraryItem.Id);
        }
        else if (e.Data.GetDataPresent(typeof(ItemSlotViewModel)) && e.Data.GetData(typeof(ItemSlotViewModel)) is ItemSlotViewModel sourceSlot
                 && !ReferenceEquals(sourceSlot, targetSlot)
                 && targetSlot.AcceptsItem(sourceSlot.Item.Id) && sourceSlot.AcceptsItem(targetSlot.Item.Id))
        {
            sourceSlot.SwapWith(targetSlot);
        }
    }

    // H5-14 (quinta auditoria de Opus): "los ~350 slots no son alcanzables sin raton... Supr
    // vacia, Intro abre 'Elegir...', F alterna favorito (H5-06), Ctrl+C/Ctrl+V copian/pegan un
    // objeto entero". Un unico "portapapeles" real de sesion (nunca persistido, se pierde al
    // cerrar la app - no tiene sentido real que sobreviva, a diferencia del portapapeles real
    // del SO) - GameItem.Clone() (H5-01) evita que copiar y luego seguir editando el slot
    // origen mute tambien lo ya copiado.
    private GameItem? _itemClipboard;

    private void OnItemSlotKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ItemSlotViewModel slot }) return;
        bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

        if (e.Key == Key.Delete)
        {
            if (slot.IsNotEmpty) slot.ClearCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            slot.ChooseFromLibraryCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.F && !ctrl)
        {
            if (slot.IsNotEmpty) slot.ToggleFavoriteCommand.Execute(null);
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.C)
        {
            _itemClipboard = slot.Item.Clone();
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.V)
        {
            if (_itemClipboard is { } clip)
            {
                // OBJ-10 (oleada de Objetos, 6-sep-2026): un Ctrl+V RECHAZADO por la restriccion
                // de slot (pegar un arma copiada sobre el hueco del casco, por ejemplo) no hacia
                // absolutamente NADA visible. PasteItem si escribe su RejectionMessage, pero ese
                // aviso vive en el panel "Editar", que solo muestra el slot SELECCIONADO - y
                // navegar/pegar por teclado nunca selecciona nada (el foco de WPF y la seleccion
                // del panel son cosas distintas). Era el cuarto camino con el mismo silencio que
                // ya se cerro para el campo "Indice" (sexta pasada) y para los buffs (H4-04).
                //
                // Se selecciona ANTES de pegar a proposito: ItemSlotViewModel.OnIsSelectedChanged
                // limpia RejectionMessage al cambiar de seleccion (L-e), asi que seleccionar
                // DESPUES borraria justo el aviso que se acaba de escribir - el mismo error de
                // orden que la ronda de Buffs de hoy tuvo que corregir. Y solo cuando de verdad va
                // a rechazarse: un pegado que funciona no tiene por que mover la seleccion del
                // usuario a otro sitio.
                if (!slot.AcceptsItem(clip.Id)) ViewModel.SelectSlot(slot);
                slot.PasteItem(clip);
            }
            e.Handled = true;
        }
    }
}
