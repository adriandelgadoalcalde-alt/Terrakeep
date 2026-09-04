using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App;

public partial class MainWindow : Window
{
    // H5-12 (quinta auditoria de Opus): tiempo real de doble clic del propio sistema operativo
    // - SystemParameters (WPF) no expone este valor (solo existe en WinForms,
    // System.Windows.Forms.SystemInformation, una dependencia que no tiene sentido arrastrar
    // aqui solo por un numero). GetDoubleClickTime (user32.dll) es la API Win32 real y
    // documentada que usa el propio Explorador para decidir si dos clics cuentan como uno doble.
    [DllImport("user32.dll")]
    private static extern uint GetDoubleClickTime();

    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        // H5-07 (quinta auditoria de Opus): "carpetas adicionales" y "ultimo personaje/pestaña"
        // real de la sesion anterior - deliberadamente NO dentro de MainViewModel() (ver el
        // comentario real de RestoreSession/SettingsViewModel.LoadFromDisk: un fichero real en
        // disco leido en el constructor contaminaria las decenas de tests que construyen un
        // MainViewModel headless). Se llama aqui, desde la View, igual que
        // WindowPlacementService.Apply() de mas abajo - ANTES de que el usuario vea nada.
        // Home/Exploration ya escanearon una vez en su propio constructor (dentro de
        // MainViewModel(), arriba) sin las carpetas adicionales todavia aplicadas - se
        // reescanean aqui, ahora que si lo estan, para que la primera pantalla real ya las
        // incluya.
        _viewModel.Settings.LoadFromDisk();
        _viewModel.Home.RefreshCommand.Execute(null);
        _viewModel.Exploration.RefreshWorldsCommand.Execute(null);
        _viewModel.RestoreSession();
        // H5-07: unico suscriptor real de CharacterLoaded - ver el comentario real del evento
        // en MainViewModel.cs (por que NO es una llamada directa dentro de LoadFromPath).
        _viewModel.CharacterLoaded += _viewModel.SaveSession;
        _viewModel.Exploration.NavigateToTileRequested += OnNavigateToTile;
        // Auditoria de Opus, T-B (segunda auditoria, Fable): mismo dialogo real de
        // "cambios sin guardar" que OnWindowClosing, ahora tambien antes de cargar OTRO
        // personaje por encima desde Inicio.
        _viewModel.ConfirmDiscardChanges = () => ConfirmDiscardChanges("cargar otro personaje");
        // Auditoria de Opus, Bloque 4 (T-3): restaura el tamaño/posicion real de la ultima
        // sesion - antes de Show(), Width/Height/Left/Top ya se pueden fijar sin parpadeo.
        Services.WindowPlacementService.Apply(this);
        // Auditoria de Opus, Bloque 4 (T-2): valor inicial real (Width ya refleja lo que
        // Apply() acaba de restaurar, fiable incluso antes de que el layout corra) - despues,
        // SizeChanged mantiene SizeClass vivo con el ancho real ya descontado el chrome.
        // H5-08 (quinta auditoria de Opus): tambien la altura real, ver WindowHeightClass.cs.
        _viewModel.UpdateSizeClass(Width, Height);
        SizeChanged += (_, e) => _viewModel.UpdateSizeClass(e.NewSize.Width, e.NewSize.Height);
    }

    // Auditoria de Opus, N-2: "se pueden editar 40 slots, cambiar de pestaña, cerrar la app y
    // perderlo todo sin un solo aviso". Confirmacion real solo cuando de verdad hay algo que
    // perder (IsDirty) - Si/No/Cancelar, igual que cualquier app de escritorio real.
    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Auditoria de Opus, Bloque 4 (T-3): se guarda SIEMPRE, incluso si despues se cancela
        // el cierre por cambios sin guardar (Cancelar) - el tamaño de ventana no es un dato del
        // personaje, no hay nada que perder al recordarlo de todos modos.
        Services.WindowPlacementService.Save(this);
        // H5-07 (quinta auditoria de Opus): mismo criterio - la posicion de navegacion (pestaña/
        // sub-pestaña/loadout/almacen) tampoco es un dato del personaje, se recuerda SIEMPRE al
        // cerrar (independientemente de si el cierre se cancela despues por cambios sin guardar).
        _viewModel.SaveSession();
        if (!ConfirmDiscardChanges("cerrar")) e.Cancel = true;
    }

    // Auditoria de Opus, N-2 (extraido para T-B, segunda auditoria de Fable): dialogo real
    // Si/No/Cancelar, reutilizado ahora por OnWindowClosing Y por cualquier accion que vaya a
    // DESCARTAR el personaje actual (cargar otro por dialogo o desde Inicio) - antes solo
    // cerrar la ventana estaba protegido, cargar otro encima perdia cambios en silencio.
    // Devuelve true si es seguro continuar (no habia nada que perder, se guardo, o el usuario
    // elige descartar a proposito con "No") - false solo si el usuario cancela la accion.
    private bool ConfirmDiscardChanges(string action)
    {
        if (!_viewModel.IsDirty) return true;
        var result = MessageBox.Show(
            $"'{_viewModel.CharacterName}' tiene cambios sin guardar.\n\n¿Guardar antes de {action}?",
            "Cambios sin guardar", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
        switch (result)
        {
            case MessageBoxResult.Yes:
                _viewModel.SaveCommand.Execute(null);
                return !_viewModel.IsDirty; // el guardado fallo de verdad - no continuar en silencio
            case MessageBoxResult.Cancel:
                return false;
            default: // No: descartar a proposito
                return true;
        }
    }

    // Auditoria de Opus, Bloque 3 (N-3): atajos de teclado reales de cualquier editor de
    // escritorio - Ctrl+S guardar, Ctrl+O cargar, Ctrl+F saltar a la Libreria y enfocar la
    // busqueda, Esc cancela una seleccion de objeto/buff en curso. Nivel Window (no requieren
    // que el foco este en ningun control concreto) - Ctrl+combinacion nunca choca con escribir
    // texto normal en un TextBox.
    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        if (ctrl && shift && e.Key == Key.F)
        {
            // F-16 (auditoria de Opus vs TEdit, B-04): antes la unica forma de abrir "¿Donde lo
            // tengo?" era el boton. OnWhereIsItPopupOpened ya hace foco+seleccion al abrirse -
            // basta con ejecutar el comando. Va ANTES de la rama ctrl+F (Libreria) porque los dos
            // comparten Key.F y el primer if que hace match es el que gana.
            if (_viewModel.ToggleWhereIsItCommand.CanExecute(null)) _viewModel.ToggleWhereIsItCommand.Execute(null);
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.S)
        {
            if (_viewModel.SaveCommand.CanExecute(null)) _viewModel.SaveCommand.Execute(null);
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.O)
        {
            OnLoadClick(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.Z)
        {
            // H5-01 (quinta auditoria de Opus): Ctrl+Z real para el historial de deshacer de
            // objetos - se cede el paso al deshacer NATIVO de un TextBox si el foco esta dentro
            // de uno (Cantidad/Índice/Prefijo a mano/nombre del personaje...), mismo criterio
            // que cualquier editor de escritorio real: el usuario esta deshaciendo SU tecleo,
            // no una edicion de slot.
            if (Keyboard.FocusedElement is TextBox) return;
            if (_viewModel.UndoEditCommand.CanExecute(null)) _viewModel.UndoEditCommand.Execute(null);
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.Y)
        {
            if (Keyboard.FocusedElement is TextBox) return;
            if (_viewModel.RedoEditCommand.CanExecute(null)) _viewModel.RedoEditCommand.Execute(null);
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.F)
        {
            // H4-03/H4-13 (cuarta auditoria de Opus, Fable): el desplegado real ya lo hace
            // GoToContextualLibraryCommand por si mismo (evita que este atajo y la tarjeta
            // "Librería" de Inicio puedan divergir de nuevo) - y ahora es CONTEXTUAL: si la
            // pestaña interna activa ya es Buffs, salta a SU Libreria, no a la de objetos.
            _viewModel.GoToContextualLibraryCommand.Execute(null);
            bool buffs = _viewModel.IsBuffsInnerTabActive;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var box = buffs ? BuffLibrarySearchBox : LibrarySearchBox;
                box.Focus();
                box.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Background);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // F-17 (auditoria de Opus vs TEdit, B-05): comprobado que StaysOpen="False" por si
            // solo NO cierra un Popup de WPF con Escape (hace falta codigo propio) - antes de
            // esto no habia ningun manejador que cerrara WhereIsItPopup con esta tecla.
            if (_viewModel.IsWhereIsItOpen) _viewModel.IsWhereIsItOpen = false;
            else if (_viewModel.Library.IsPicking) _viewModel.Library.CancelPickCommand.Execute(null);
            else if (_viewModel.BuffLibrary.IsPicking) _viewModel.BuffLibrary.CancelPickCommand.Execute(null);
            else return; // nada real que cancelar - no consumir la tecla (ej. cerrar un ComboBox abierto)
            e.Handled = true;
        }
        // H5-14 (quinta auditoria de Opus): "Ctrl+1...6 para las 6 pestañas raiz, numero
        // anunciado en el propio rotulo" (mismo criterio T-H/F1) - salto directo sin pasar por
        // el raton, ningun conflicto real con un TextBox (Ctrl+numero no es un gesto de tecleo
        // normal, a diferencia de Ctrl+Z/Y que SI colisionan con el deshacer nativo de un campo).
        else if (ctrl && e.Key is >= Key.D1 and <= Key.D6)
        {
            _viewModel.SelectedTabIndex = e.Key - Key.D1;
            e.Handled = true;
        }
    }

    // Auditoria de Opus, T-17: campos que ejecutan una accion real al cambiar (Indice/Prefijo,
    // ver MainWindow.xaml) ya no usan UpdateSourceTrigger=PropertyChanged - confirman al perder
    // el foco (comportamiento real por defecto de WPF). Este manejador fuerza el mismo commit
    // real al pulsar Intro, sin obligar a hacer clic fuera del campo primero.
    private void OnCommitTextOnEnter(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox textBox) return;
        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    // H5-05 (quinta auditoria de Opus): mismo gesto real ya usado por Ctrl+F (LibrarySearchBox) -
    // abrir el panel de "¿Dónde lo tengo?" deja el cursor listo para teclear de inmediato, sin
    // exigir un clic extra en el propio cuadro.
    private void OnWhereIsItPopupOpened(object sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            WhereIsItSearchBox.Focus();
            WhereIsItSearchBox.SelectAll();
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    // H5-07 (quinta auditoria de Opus): dialogo real de "elegir carpeta" - vive aqui (View),
    // no en SettingsViewModel (mismo criterio real ya establecido en H5-03 con SaveItemSet/
    // LoadItemSet - MainViewModel/sus sub-ViewModels se quedan headless de verdad).
    private void OnAddCharacterFolderClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Elige una carpeta adicional con personajes (.plr)" };
        if (dialog.ShowDialog(this) == true) _viewModel.Settings.AddCharacterFolder(dialog.FolderName);
    }

    private void OnAddWorldFolderClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Elige una carpeta adicional con mundos (.wld)" };
        if (dialog.ShowDialog(this) == true) _viewModel.Settings.AddWorldFolder(dialog.FolderName);
    }

    private void OnLoadClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Cargar personaje de Terraria",
            Filter = "Personaje de Terraria (*.plr)|*.plr|Todos los archivos (*.*)|*.*",
            InitialDirectory = Services.CharacterFileService.GetDefaultPlayersDirectory(),
        };

        if (dialog.ShowDialog(this) == true)
        {
            if (!ConfirmDiscardChanges("cargar otro personaje")) return;
            _viewModel.LoadFromPath(dialog.FileName);
        }
    }

    // Auditoria de Opus, X-7/T-13: leer+pintar un mundo real (~1.4s medidos en uno de 11MB de
    // esta maquina) congelaba el hilo de UI entero sin ningun aviso - async void es el patron
    // real de WPF para un manejador de evento async (no se puede await desde un evento).
    private async void OnLoadWorldClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Cargar mundo de Terraria",
            Filter = "Mundo de Terraria (*.wld)|*.wld|Todos los archivos (*.*)|*.*",
            InitialDirectory = Services.CharacterFileService.GetDefaultWorldsDirectory(),
        };

        if (dialog.ShowDialog(this) == true)
        {
            await _viewModel.Exploration.LoadFromPathAsync(dialog.FileName);
            // X-a (segunda auditoria de Opus, Fable): se ajusta solo la primera vez que se ve el
            // mundo, sin que el usuario tenga que ir a buscar el boton. DispatcherPriority.Loaded
            // (no Background) para que el ScrollViewer ya haya completado un layout real con el
            // nuevo WorldImage/extent antes de leer su ViewportWidth/Height - justo la misma
            // necesidad real que UpdateLayout() ya resuelve en el zoom de la rueda, de abajo.
            _ = Dispatcher.BeginInvoke(new Action(FitWorldMapToWindow), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    // H4-08 (cuarta auditoria de Opus, Fable): gemelo real de OnLoadWorldClick - una tarjeta del
    // lanzador de mundos ya trae su ruta real (DataContext), no hace falta el dialogo del
    // Explorador de archivos. Mismo ajuste de zoom real al terminar.
    private async void OnWorldCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: WorldListEntryViewModel entry }) return;
        await _viewModel.Exploration.LoadFromPathAsync(entry.FilePath);
        _ = Dispatcher.BeginInvoke(new Action(FitWorldMapToWindow), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    // H5-04 (quinta auditoria de Opus): el submenu "Historial de guardados" de la tarjeta de
    // Inicio se puebla AQUI, bajo demanda al abrirse (nunca al escanear Inicio - seria I/O de
    // sobra para personajes que el usuario nunca llega a mirar). El ContextMenu es un popup
    // aparte del arbol visual de la ventana (mismo motivo real que el resto de MenuItem de esta
    // tarjeta ya viajan HomeViewModel/la entrada via PlacementTarget.Tag/.DataContext en vez de
    // heredar el DataContext normal) - aqui el "submenu" en si (el MenuItem padre) SI es
    // logicamente hijo directo del propio ContextMenu, asi que Parent llega derecho a el.
    private void OnBackupHistorySubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem submenu) return;
        if (submenu.Parent is not ContextMenu contextMenu) return;
        if (contextMenu.PlacementTarget is not FrameworkElement placementTarget) return;
        if (placementTarget.DataContext is not CharacterListEntryViewModel entry) return;
        if (placementTarget.Tag is not HomeViewModel home) return;

        submenu.Items.Clear();
        var backups = home.ListBackupPoints(entry);
        if (backups.Count == 0)
        {
            submenu.Items.Add(new MenuItem { Header = "Sin copias de seguridad todavía", IsEnabled = false });
            return;
        }
        foreach (var backup in backups)
        {
            var item = new MenuItem { Header = $"{backup.TimestampLocal:dd/MM/yyyy HH:mm:ss} · {FormatBackupSize(backup.SizeBytes)}" };
            item.Click += (_, _) => home.RestoreBackupPointCommand.Execute((entry, backup));
            submenu.Items.Add(item);
        }
    }

    private static string FormatBackupSize(long bytes) =>
        bytes >= 1024 * 1024 ? $"{bytes / (1024.0 * 1024.0):0.0} MB" : $"{bytes / 1024.0:0.0} KB";

    // H5-03 (quinta auditoria de Opus): "guardar/cargar conjuntos de objetos" - el dialogo real
    // de fichero vive aqui (MainViewModel es headless de verdad, mismo criterio ya establecido
    // en OnLoadClick/OnLoadWorldClick). Alcance de esta pasada: Inventario y Almacenes (el
    // almacen SELECCIONADO ahora mismo, Current) - Equipamiento/loadouts quedan fuera,
    // documentado en bitacora.md.
    private void OnSaveInventorySetClick(object sender, RoutedEventArgs e) => SaveItemSetDialog(_viewModel.InventoryContainer);
    private void OnLoadInventorySetClick(object sender, RoutedEventArgs e) => LoadItemSetDialog(_viewModel.InventoryContainer, append: false);
    private void OnAppendInventorySetClick(object sender, RoutedEventArgs e) => LoadItemSetDialog(_viewModel.InventoryContainer, append: true);

    private void OnSaveStorageSetClick(object sender, RoutedEventArgs e) => SaveItemSetDialog(_viewModel.StorageGroup?.Current);
    private void OnLoadStorageSetClick(object sender, RoutedEventArgs e) => LoadItemSetDialog(_viewModel.StorageGroup?.Current, append: false);
    private void OnAppendStorageSetClick(object sender, RoutedEventArgs e) => LoadItemSetDialog(_viewModel.StorageGroup?.Current, append: true);

    private void SaveItemSetDialog(ContainerViewModel? container)
    {
        if (container == null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Guardar conjunto de objetos",
            Filter = "Conjunto de objetos de Terrakeep (*.json)|*.json",
            FileName = container.Key + ".json",
        };
        if (dialog.ShowDialog(this) == true) _viewModel.SaveItemSet(container, dialog.FileName);
    }

    private void LoadItemSetDialog(ContainerViewModel? container, bool append)
    {
        if (container == null) return;
        var dialog = new OpenFileDialog
        {
            Title = append ? "Añadir conjunto de objetos" : "Cargar conjunto de objetos (reemplaza)",
            Filter = "Conjunto de objetos de Terrakeep (*.json)|*.json|Todos los archivos (*.*)|*.*",
        };
        if (dialog.ShowDialog(this) == true) _viewModel.LoadItemSet(container, dialog.FileName, append);
    }

    // Pedido explicito del usuario (4-sep-2026): "la pestaña de buff no tiene nada de guardar
    // json ni tampoco cargar para guardar combinaciones de buff" - gemelo real de
    // OnSave/LoadInventorySetClick de arriba, mismo criterio (dialogo real en la View).
    private void OnSaveBuffSetClick(object sender, RoutedEventArgs e) => SaveBuffSetDialog();
    private void OnLoadBuffSetClick(object sender, RoutedEventArgs e) => LoadBuffSetDialog(append: false);
    private void OnAppendBuffSetClick(object sender, RoutedEventArgs e) => LoadBuffSetDialog(append: true);

    private void SaveBuffSetDialog()
    {
        var container = _viewModel.Buffs.Container;
        if (container == null) return;
        var dialog = new SaveFileDialog
        {
            Title = "Guardar conjunto de buffs",
            Filter = "Conjunto de buffs de Terrakeep (*.json)|*.json",
            FileName = "buffs.json",
        };
        if (dialog.ShowDialog(this) == true) _viewModel.SaveBuffSet(container, dialog.FileName);
    }

    private void LoadBuffSetDialog(bool append)
    {
        var container = _viewModel.Buffs.Container;
        if (container == null) return;
        var dialog = new OpenFileDialog
        {
            Title = append ? "Añadir conjunto de buffs" : "Cargar conjunto de buffs (reemplaza)",
            Filter = "Conjunto de buffs de Terrakeep (*.json)|*.json|Todos los archivos (*.*)|*.*",
        };
        if (dialog.ShowDialog(this) == true) _viewModel.LoadBuffSet(container, dialog.FileName, append);
    }

    // X-a: boton real "Ajustar a la ventana" - antes solo existia "Restablecer" (vuelve al
    // 100%, que para un mundo grande deja ver una fraccion minima del ancho). El calculo
    // necesita el tamaño real del viewport del ScrollViewer, que la ViewModel no conoce - vive
    // aqui, igual que el resto de gestos del mapa (pan/zoom con rueda).
    private void OnFitToWindowClick(object sender, RoutedEventArgs e)
    {
        WorldMapScroll.UpdateLayout();
        FitWorldMapToWindow();
    }

    private void FitWorldMapToWindow()
    {
        var image = _viewModel.Exploration.WorldImage;
        if (image == null || WorldMapScroll.ViewportWidth <= 0 || WorldMapScroll.ViewportHeight <= 0) return;
        _viewModel.Exploration.Zoom = Math.Min(
            WorldMapScroll.ViewportWidth / image.PixelWidth,
            WorldMapScroll.ViewportHeight / image.PixelHeight);
    }

    // Rueda del raton = zoom directamente (sin necesitar Ctrl, pedido explicito - el arrastre ya
    // cubre el desplazamiento normal, asi que la rueda no hace falta para nada mas aqui).
    //
    // Bug real corregido (1-sep-2026, reportado: "el zoom no lo hace recto"): cambiar solo
    // Zoom sin tocar los offsets del ScrollViewer hace zoom desde la esquina superior
    // izquierda del mapa (offset 0,0), no desde donde esta el cursor - la vista "salta" en vez
    // de hacer zoom centrado en el punto que se esta mirando. Se calcula la coordenada de
    // mundo bajo el cursor ANTES de cambiar el zoom, y se recoloca el offset para que ese
    // mismo punto de mundo siga bajo el cursor DESPUES. UpdateLayout() fuerza a que el
    // ScrollViewer ya conozca el nuevo tamaño de contenido (post-LayoutTransform) antes de
    // pedirle el nuevo offset - sin esto, ScrollToHorizontalOffset calcularia contra el
    // extent viejo todavia y el resultado seguiria sin cuadrar.
    private void OnWorldMapPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        double oldZoom = _viewModel.Exploration.Zoom;
        var mousePos = e.GetPosition(WorldMapScroll);
        double worldX = (WorldMapScroll.HorizontalOffset + mousePos.X) / oldZoom;
        double worldY = (WorldMapScroll.VerticalOffset + mousePos.Y) / oldZoom;

        // X-b (segunda auditoria de Opus, Fable): mismo paso real que los botones ZoomIn/ZoomOut
        // (ExplorationViewModel.ZoomStep) - antes la rueda usaba x1.15, un paso distinto solo por
        // costumbre, no por ningun motivo real.
        _viewModel.Exploration.Zoom = oldZoom * (e.Delta > 0 ? ExplorationViewModel.ZoomStep : 1 / ExplorationViewModel.ZoomStep);
        double newZoom = _viewModel.Exploration.Zoom;

        WorldMapScroll.UpdateLayout();
        WorldMapScroll.ScrollToHorizontalOffset(worldX * newZoom - mousePos.X);
        WorldMapScroll.ScrollToVerticalOffset(worldY * newZoom - mousePos.Y);
        e.Handled = true;
    }

    // Arrastrar con el boton izquierdo para desplazar el mapa (pan) - captura el raton al
    // pulsar y mueve los offsets del ScrollViewer segun el desplazamiento real del cursor en
    // pantalla, sin depender del zoom actual (los offsets del ScrollViewer ya estan en el
    // espacio POST-transformacion porque el ScaleTransform esta en LayoutTransform, no
    // RenderTransform).
    private Point? _mapDragStart;
    private double _mapDragStartH, _mapDragStartV;

    private void OnWorldMapMouseDown(object sender, MouseButtonEventArgs e)
    {
        _mapDragStart = e.GetPosition(WorldMapScroll);
        _mapDragStartH = WorldMapScroll.HorizontalOffset;
        _mapDragStartV = WorldMapScroll.VerticalOffset;
        WorldMapScroll.CaptureMouse();
    }

    private void OnWorldMapMouseUp(object sender, MouseButtonEventArgs e)
    {
        _mapDragStart = null;
        WorldMapScroll.ReleaseMouseCapture();
    }

    // GetPosition(WorldMapImage) ya devuelve la posicion en el espacio de pixel NATIVO de la
    // imagen (WPF deshace el LayoutTransform/zoom automaticamente para el elemento sobre el
    // que se pide la posicion) - y WorldRenderer pinta a 1 pixel por tile, asi que el pixel es
    // directamente la coordenada de tile. Este handler vive en el ScrollViewer (no en la
    // Image) para que siga disparandose durante el arrastre, cuando el raton tiene captura.
    private void OnWorldMapMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(WorldMapImage);
        _viewModel.Exploration.UpdateHover((int)pos.X, (int)pos.Y);

        if (_mapDragStart is { } start && e.LeftButton == MouseButtonState.Pressed)
        {
            var current = e.GetPosition(WorldMapScroll);
            WorldMapScroll.ScrollToHorizontalOffset(_mapDragStartH - (current.X - start.X));
            WorldMapScroll.ScrollToVerticalOffset(_mapDragStartV - (current.Y - start.Y));
            MapTooltipBorder.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Collapsed); // no molesta mientras se arrastra
        }
        else
        {
            PositionMapTooltip(e.GetPosition(MapTooltipCanvas));
        }
    }

    // Tooltip flotante estilo TEdit: se coloca con un pequeño margen respecto al cursor y se
    // voltea al otro lado si no cabe por el borde derecho/inferior del propio Canvas (que
    // ocupa exactamente el area visible del mapa, ver MainWindow.xaml) - sin esto el texto se
    // saldria cortado fuera del visor en los bordes.
    private void PositionMapTooltip(Point cursorPos)
    {
        const double offset = 16, marginY = 18;
        MapTooltipBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = MapTooltipBorder.DesiredSize;

        double left = cursorPos.X + offset;
        if (left + size.Width > MapTooltipCanvas.ActualWidth) left = cursorPos.X - offset - size.Width;

        double top = cursorPos.Y + marginY;
        if (top + size.Height > MapTooltipCanvas.ActualHeight) top = cursorPos.Y - marginY - size.Height;

        Canvas.SetLeft(MapTooltipBorder, Math.Max(0, left));
        Canvas.SetTop(MapTooltipBorder, Math.Max(0, top));
        // SetCurrentValue, no el setter directo: Visibility ya tiene un Binding real en el XAML
        // (a Exploration.HoverInfo via EmptyToCollapsed) - asignar la propiedad a secas
        // reemplazaria ese binding para siempre; SetCurrentValue solo empuja un valor puntual
        // sin desengancharlo.
        MapTooltipBorder.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Visible);
    }

    private void OnWorldMapMouseLeave(object sender, MouseEventArgs e)
    {
        _viewModel.Exploration.UpdateHover(-1, -1);
        MapTooltipBorder.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Collapsed);
    }

    // Centra el mapa sobre la posicion de un NPC (pedido desde ExplorationViewModel via
    // NavigateToTileRequested al pulsar un NPC en la lista) - los offsets del ScrollViewer ya
    // estan en espacio post-zoom, igual que en el arrastre.
    private void OnNavigateToTile(int tileX, int tileY)
    {
        // F-3 (auditoria de Opus vs TEdit, E-12): con la casilla "Acercar al ir a un resultado"
        // marcada, fija un zoom de trabajo ANTES de centrar - mismo patron real que el zoom con
        // rueda (UpdateLayout() antes de pedir offsets nuevos, para que el ScrollViewer conozca
        // el extent post-LayoutTransform). TEdit fija _zoom=8 en su escala
        // (WorldRenderXna.xaml.cs:8178); el equivalente razonable aqui, dentro del MaxZoom=6.0
        // ya existente, es 4.0.
        if (_viewModel.Exploration.AutoZoomOnNavigate)
        {
            _viewModel.Exploration.Zoom = 4.0;
            WorldMapScroll.UpdateLayout();
        }
        double zoom = _viewModel.Exploration.Zoom;
        WorldMapScroll.ScrollToHorizontalOffset(tileX * zoom - WorldMapScroll.ViewportWidth / 2);
        WorldMapScroll.ScrollToVerticalOffset(tileY * zoom - WorldMapScroll.ViewportHeight / 2);
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

        if (sender is FrameworkElement { DataContext: LibraryItemViewModel item } element)
            DragDrop.DoDragDrop(element, new DataObject(typeof(LibraryItemViewModel), item), DragDropEffects.Copy);
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
            _viewModel.PlaceInFirstFreeInventorySlot(item.Id);
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
        var target = _viewModel.ItemEdit.Slot;
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
            _viewModel.SelectSlot(slot);
    }

    private void OnItemSlotMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragStartSlot is not { } start) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _dragStartSlot = null;

        if (sender is FrameworkElement { DataContext: ItemSlotViewModel { IsEmpty: false } slot } element)
            DragDrop.DoDragDrop(element, new DataObject(typeof(ItemSlotViewModel), slot), DragDropEffects.Move);
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
            if (_itemClipboard is { } clip) slot.PasteItem(clip);
            e.Handled = true;
        }
    }

    // Gemelos de los 3 de arriba, para la rejilla de Buffs (pregunta a Opus sobre el diseño
    // 2-sep-2026, cuarta pasada) - mismo patron exacto, solo cambia el tipo (BuffSlotViewModel
    // en vez de ItemSlotViewModel). Arrastrar un buff sobre otro los intercambia entero
    // (id+duracion).
    private Point? _dragStartBuffSlot;

    private void OnBuffSlotMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartBuffSlot = e.GetPosition(null);
        if (sender is FrameworkElement { DataContext: BuffSlotViewModel slot })
            _viewModel.SelectBuffSlot(slot);
    }

    private void OnBuffSlotMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragStartBuffSlot is not { } start) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _dragStartBuffSlot = null;

        if (sender is FrameworkElement { DataContext: BuffSlotViewModel { IsEmpty: false } slot } element)
            DragDrop.DoDragDrop(element, new DataObject(typeof(BuffSlotViewModel), slot), DragDropEffects.Move);
    }

    // Bug real reportado 2-sep-2026 ("los buff no se pueden arrastrar hasta la rejilla de
    // buff"): OnBuffSlotDrop solo aceptaba OTRO slot arrastrado (intercambio), nunca una
    // tarjeta arrastrada desde la Libreria de buffs - a diferencia de OnItemSlotDrop, que ya
    // acepta LibraryItemViewModel ademas de ItemSlotViewModel. Arreglado igual: la tarjeta de
    // la Libreria de buffs (BuffCatalogEntryViewModel) tambien inicia arrastre.
    private Point? _dragStartBuffLibrary;

    private void OnBuffLibraryCardMouseDown(object sender, MouseButtonEventArgs e) => _dragStartBuffLibrary = e.GetPosition(null);

    private void OnBuffLibraryCardMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragStartBuffLibrary is not { } start) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - start.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - start.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _dragStartBuffLibrary = null;

        if (sender is FrameworkElement { DataContext: BuffCatalogEntryViewModel entry } element)
            DragDrop.DoDragDrop(element, new DataObject(typeof(BuffCatalogEntryViewModel), entry), DragDropEffects.Copy);
    }

    // H4-04 (cuarta auditoria de Opus, Fable): gemelo real de OnItemSlotDragOver - solo hace
    // falta comprobar el origen "Libreria de buffs" (un buff duplicado real, ver
    // WouldRejectPlacingBuff), nunca el intercambio entre dos slots (SwapWith nunca puede crear
    // un duplicado - cada buff se queda en un unico slot antes y despues).
    private void OnBuffSlotDragOver(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: BuffSlotViewModel targetSlot }) return;

        bool accepted = !(e.Data.GetDataPresent(typeof(BuffCatalogEntryViewModel))
            && e.Data.GetData(typeof(BuffCatalogEntryViewModel)) is BuffCatalogEntryViewModel libraryEntry
            && targetSlot.WouldRejectPlacingBuff(libraryEntry.Id));

        e.Effects = accepted ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnBuffSlotDrop(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: BuffSlotViewModel targetSlot }) return;

        if (e.Data.GetDataPresent(typeof(BuffCatalogEntryViewModel)) && e.Data.GetData(typeof(BuffCatalogEntryViewModel)) is BuffCatalogEntryViewModel libraryEntry)
        {
            // H4-04: si se rechaza (duplicado real), seleccionar el slot destino - el aviso
            // real (RejectionMessage, ya puesto por PlaceBuff) queda a la vista en el panel
            // "Editar buff seleccionado" en vez de perderse sin que se note nada.
            if (!targetSlot.PlaceBuff(libraryEntry.Id)) _viewModel.SelectBuffSlot(targetSlot);
        }
        else if (e.Data.GetDataPresent(typeof(BuffSlotViewModel)) && e.Data.GetData(typeof(BuffSlotViewModel)) is BuffSlotViewModel sourceSlot
                 && !ReferenceEquals(sourceSlot, targetSlot))
        {
            sourceSlot.SwapWith(targetSlot);
        }
    }

    // H5-14: gemelo real de OnItemSlotKeyDown - sin "F" de favorito (un buff no tiene ese
    // concepto) ni Supr condicionado a IsNotEmpty (Clear() de un buff ya vacio es un no-op
    // barato, no hace falta guardarlo).
    private (int id, int time)? _buffClipboard;

    private void OnBuffSlotKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: BuffSlotViewModel slot }) return;
        bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

        if (e.Key == Key.Delete)
        {
            slot.ClearCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            slot.ChooseFromLibraryCommand.Execute(null);
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.C)
        {
            _buffClipboard = (slot.Buff.Id, slot.Buff.Time);
            e.Handled = true;
        }
        else if (ctrl && e.Key == Key.V)
        {
            if (_buffClipboard is { } clip) slot.PasteBuff(clip.id, clip.time);
            e.Handled = true;
        }
    }
}
