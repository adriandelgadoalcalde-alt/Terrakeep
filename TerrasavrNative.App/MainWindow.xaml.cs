using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App;

public partial class MainWindow : Window
{
    // Auditoria final de Opus (5-sep-2026): atajo local al diccionario de idioma. Todo el texto
    // que nace en este code-behind (titulos y filtros de los dialogos reales de fichero, los
    // MessageBox modales, el submenu de copias de seguridad) se quedaba SIEMPRE en español,
    // tambien con la app en ingles - los bloques 1-4 del idioma migraron el XAML y los
    // ViewModels, pero esta capa se quedo fuera y no la alcanza ningun binding.
    private static Services.LocalizationService Loc => Services.LocalizationService.Instance;

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
        // F-8 (auditoria de Opus vs TEdit, E-05): el rectangulo de viewport del minimapa
        // necesita recalcularse cada vez que el mapa se desplaza (ScrollChanged) O cambia de
        // zoom/mundo (Zoom/WorldImage - no pasan por ScrollChanged por si solos).
        _viewModel.Exploration.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ExplorationViewModel.Zoom) or nameof(ExplorationViewModel.WorldImage))
                UpdateMinimapViewport();
        };
        // Auditoria de Opus, T-B (segunda auditoria, Fable): mismo dialogo real de
        // "cambios sin guardar" que OnWindowClosing, ahora tambien antes de cargar OTRO
        // personaje por encima desde Inicio.
        _viewModel.ConfirmDiscardChanges = () => ConfirmDiscardChanges(Loc["dlg_action_load_other"]);
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
        // F-11 (auditoria de Opus vs TEdit, E-11): igual que arriba, se recuerda SIEMPRE al
        // cerrar - la vista del mapa no es un dato del personaje/mundo en si, no hay nada que
        // perder al guardarla de todos modos.
        if (_viewModel.Exploration.IsWorldLoaded)
            _viewModel.Exploration.SaveCurrentViewState(WorldMapScroll.HorizontalOffset, WorldMapScroll.VerticalOffset);
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
            Loc.Format("dlg_unsaved_body", _viewModel.CharacterName, action),
            Loc["dlg_unsaved_title"], MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
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
        bool alt = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
        // F-9 (auditoria de Opus vs TEdit, E-09): "Ctrl+Shift+F -> foco en el cuadro de busqueda
        // del mundo" del informe original CHOCA de verdad con F-16 (Ctrl+Shift+F ya es "Buscar en
        // el personaje", implementado en el bloque 1 de este mismo plan) - encontrado al
        // implementar, no al leer el informe. Ctrl+Alt+F en su lugar, unico libre de los ya
        // usados (Ctrl+F=Libreria, Ctrl+Shift+F=personaje).
        if (ctrl && alt && e.Key == Key.F)
        {
            _viewModel.SelectedTabIndex = 4; // AppTab.Exploracion
            Dispatcher.BeginInvoke(new Action(() => { WorldSearchBox.Focus(); WorldSearchBox.SelectAll(); }),
                System.Windows.Threading.DispatcherPriority.Background);
            e.Handled = true;
        }
        else if (ctrl && shift && e.Key == Key.F)
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
        // F-9 (auditoria de Opus vs TEdit, E-09): atajos del mapa - SOLO con Exploracion activa
        // (AppTab.Exploracion=4) y el foco FUERA de un TextBox (el propio cuadro de busqueda del
        // mundo vive en esta pestaña; sin esta guarda, teclear "10" ahi tambien haria zoom).
        else if (!ctrl && _viewModel.SelectedTabIndex == 4 && Keyboard.FocusedElement is not TextBox)
        {
            var exploracion = _viewModel.Exploration;
            if (e.Key is Key.OemPlus or Key.Add)
            {
                if (exploracion.ZoomInCommand.CanExecute(null)) exploracion.ZoomInCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key is Key.OemMinus or Key.Subtract)
            {
                if (exploracion.ZoomOutCommand.CanExecute(null)) exploracion.ZoomOutCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.D0)
            {
                OnFitToWindowClick(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.D1)
            {
                if (exploracion.ZoomResetCommand.CanExecute(null)) exploracion.ZoomResetCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.F3)
            {
                var comandoResultado = shift ? exploracion.PreviousWorldSearchResultCommand : exploracion.NextWorldSearchResultCommand;
                if (comandoResultado.CanExecute(null)) comandoResultado.Execute(null);
                e.Handled = true;
            }
            else if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
            {
                const double paso = 60;
                switch (e.Key)
                {
                    case Key.Left: WorldMapScroll.ScrollToHorizontalOffset(WorldMapScroll.HorizontalOffset - paso); break;
                    case Key.Right: WorldMapScroll.ScrollToHorizontalOffset(WorldMapScroll.HorizontalOffset + paso); break;
                    case Key.Up: WorldMapScroll.ScrollToVerticalOffset(WorldMapScroll.VerticalOffset - paso); break;
                    case Key.Down: WorldMapScroll.ScrollToVerticalOffset(WorldMapScroll.VerticalOffset + paso); break;
                }
                e.Handled = true;
            }
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
        var dialog = new OpenFolderDialog { Title = Loc["dlg_pick_players_folder"] };
        if (dialog.ShowDialog(this) == true) _viewModel.Settings.AddCharacterFolder(dialog.FolderName);
    }

    private void OnAddWorldFolderClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = Loc["dlg_pick_worlds_folder"] };
        if (dialog.ShowDialog(this) == true) _viewModel.Settings.AddWorldFolder(dialog.FolderName);
    }

    // Pedido explicito del usuario (5-sep-2026): "guardar el tamaño de ventana actual... con un
    // tick que lo activa/desactiva". Solo la View conoce el Window real (Left/Top/Width/Height/
    // RestoreBounds) - SettingsViewModel.IsWindowSizePinned es solo el espejo que el CheckBox
    // muestra, actualizado aqui explicitamente tras Pin()/Unpin() en vez de via el binding
    // normal (evita que una futura OnIsWindowSizePinnedChanged en la ViewModel intente
    // persistir algo que no puede calcular sin el Window).
    private void OnPinWindowSizeChecked(object sender, RoutedEventArgs e)
    {
        Services.WindowPlacementService.Pin(this);
        _viewModel.Settings.IsWindowSizePinned = true;
    }

    private void OnPinWindowSizeUnchecked(object sender, RoutedEventArgs e)
    {
        Services.WindowPlacementService.Unpin();
        _viewModel.Settings.IsWindowSizePinned = false;
    }

    // F-13 (auditoria de Opus vs TEdit, E-14): "AllowDrop aparece exactamente dos veces... las
    // dos son slots de objeto y de buff. La ventana no acepta ficheros." Filtrar por
    // DataFormats.FileDrop basta para no interferir con los dos AllowDrop internos (usan un
    // formato de datos propio para mover objetos entre slots, nunca FileDrop) - no hace falta
    // marcar e.Handled en ellos ni aqui.
    private async void OnWindowDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } files) return;
        string path = files[0];
        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".wld")
        {
            await LoadWorldAndRestoreView(path);
            _viewModel.SelectedTabIndex = 4; // AppTab.Exploracion, privado - mismo criterio ya usado en el arnes
        }
        else if (ext == ".plr")
        {
            if (!ConfirmDiscardChanges(Loc["dlg_action_load_other"])) return;
            _viewModel.LoadFromPath(path);
        }
    }

    private void OnLoadClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc["dlg_load_character"],
            Filter = Loc["dlg_filter_character"],
            InitialDirectory = Services.CharacterFileService.GetDefaultPlayersDirectory(),
        };

        if (dialog.ShowDialog(this) == true)
        {
            if (!ConfirmDiscardChanges(Loc["dlg_action_load_other"])) return;
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
            Title = Loc["dlg_load_world"],
            Filter = Loc["dlg_filter_world"],
            InitialDirectory = Services.CharacterFileService.GetDefaultWorldsDirectory(),
        };

        if (dialog.ShowDialog(this) == true) await LoadWorldAndRestoreView(dialog.FileName);
    }

    // F-10 (auditoria de Opus vs TEdit, E-10): alterna entre el ancho guardado y 0 - el ancho
    // "de antes de plegar" se recuerda aqui en memoria (no persistido aparte, no hace falta:
    // solo importa dentro de la MISMA sesion, entre un plegado y el siguiente despliegue).
    private double _lastExpandedSidebarWidth = 320;
    private void OnToggleExplorationSidebarClick(object sender, RoutedEventArgs e)
    {
        var settings = _viewModel.Settings;
        if (settings.ExplorationSidebarWidth > 0)
        {
            _lastExpandedSidebarWidth = settings.ExplorationSidebarWidth;
            settings.ExplorationSidebarWidth = 0;
        }
        else
        {
            settings.ExplorationSidebarWidth = _lastExpandedSidebarWidth;
        }
    }

    // F-14 (auditoria de Opus vs TEdit, E-16/E-17): dialogo real en la View (mismo criterio que
    // SaveItemSetDialog/ExportMapToPng) - BuildWorldReportText solo compone el texto.
    private void OnSaveWorldReportClick(object sender, RoutedEventArgs e)
    {
        string texto = _viewModel.Exploration.BuildWorldReportText();
        if (string.IsNullOrEmpty(texto)) return;
        var dialog = new SaveFileDialog
        {
            Title = Loc["dlg_save_world_report"],
            Filter = Loc["dlg_filter_text"],
            FileName = $"{_viewModel.Exploration.WorldTitle}-informe.txt",
        };
        if (dialog.ShowDialog(this) == true) File.WriteAllText(dialog.FileName, texto);
    }

    // F-12 (auditoria de Opus vs TEdit, E-13): dialogo real en la View (mismo criterio que
    // SaveItemSetDialog) - la composicion+codificacion vive en ExportMapToPng.
    private void OnExportMapClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Exploration.WorldImage == null) return;
        var dialog = new SaveFileDialog
        {
            Title = Loc["dlg_export_map"],
            Filter = Loc["dlg_filter_png"],
            FileName = $"{_viewModel.Exploration.WorldTitle}-mapa.png",
        };
        if (dialog.ShowDialog(this) == true) _viewModel.Exploration.ExportMapToPng(dialog.FileName);
    }

    // H4-08 (cuarta auditoria de Opus, Fable): gemelo real de OnLoadWorldClick - una tarjeta del
    // lanzador de mundos ya trae su ruta real (DataContext), no hace falta el dialogo del
    // Explorador de archivos. Mismo ajuste de zoom real al terminar.
    private async void OnWorldCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: WorldListEntryViewModel entry }) return;
        await LoadWorldAndRestoreView(entry.FilePath);
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
            submenu.Items.Add(new MenuItem { Header = Loc["dlg_no_backups_yet"], IsEnabled = false });
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
            // Ronda de idioma del 6-sep-2026: los dos literales estaban a pelo aqui aunque sus
            // claves ("dlg_save_item_set_title" y "dlg_filter_item_set") YA existian en los dos
            // diccionarios desde la ronda anterior - se crearon y nunca se llegaron a enchufar.
            Title = Loc["dlg_save_item_set_title"],
            Filter = Loc["dlg_filter_item_set"],
            FileName = container.Key + ".json",
        };
        if (dialog.ShowDialog(this) == true) _viewModel.SaveItemSet(container, dialog.FileName);
    }

    private void LoadItemSetDialog(ContainerViewModel? container, bool append)
    {
        if (container == null) return;
        var dialog = new OpenFileDialog
        {
            Title = Loc[append ? "dlg_add_item_set" : "dlg_load_item_set"],
            Filter = Loc["dlg_filter_item_set"],
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
            Title = Loc["dlg_save_buff_set"],
            Filter = Loc["dlg_filter_buff_set"],
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
            Title = Loc[append ? "dlg_add_buff_set" : "dlg_load_buff_set"],
            Filter = Loc["dlg_filter_buff_set"],
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

    // F-11 (auditoria de Opus vs TEdit, E-11): unico punto real de carga de un mundo (los 3
    // sitios que antes hacian await LoadFromPathAsync + FitWorldMapToWindow por su cuenta -
    // OnLoadWorldClick/OnWorldCardClick/OnWindowDrop - pasan a llamar aqui) para no triplicar la
    // logica de guardar-la-vista-anterior/restaurar-o-ajustar. Guarda la vista del mundo SALIENTE
    // (si habia uno) antes de cargar el nuevo, y tras cargar: si el mundo entrante tiene una vista
    // guardada la restaura (Zoom ya lo puso LoadFromPathAsync; aqui solo el offset del
    // ScrollViewer, que la ViewModel no puede tocar), si no, "Ajustar a la ventana" de siempre.
    private async Task LoadWorldAndRestoreView(string path)
    {
        if (_viewModel.Exploration.IsWorldLoaded)
            _viewModel.Exploration.SaveCurrentViewState(WorldMapScroll.HorizontalOffset, WorldMapScroll.VerticalOffset);

        await _viewModel.Exploration.LoadFromPathAsync(path);

        _ = Dispatcher.BeginInvoke(new Action(() =>
        {
            // DispatcherPriority.Loaded (no Background): el ScrollViewer necesita haber
            // completado un layout real con el WorldImage/extent nuevo (post-Zoom, ya restaurado
            // por LoadFromPathAsync si habia vista guardada) antes de poder pedirle su
            // ViewportWidth/Height o fijar un offset real - misma necesidad ya resuelta por
            // UpdateLayout() en el zoom de la rueda, mas abajo.
            if (_viewModel.Exploration.TryConsumePendingViewRestore(out double offsetH, out double offsetV))
            {
                WorldMapScroll.UpdateLayout();
                WorldMapScroll.ScrollToHorizontalOffset(offsetH);
                WorldMapScroll.ScrollToVerticalOffset(offsetV);
            }
            else
            {
                FitWorldMapToWindow();
            }
        }), System.Windows.Threading.DispatcherPriority.Loaded);
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
        // Punto 4 del encargo (6-sep-2026): ya no se lee la casilla global directamente - cada
        // origen de navegacion decide con la SUYA ("Cofre a cofre" tiene la propia) y deja el
        // resultado resuelto en NavigationWantsAutoZoom. Todo camino real hasta aqui pasa por
        // ExplorationViewModel.NavigateToTile (incluido el clic en el minimapa), asi que este
        // valor siempre corresponde a la navegacion que se esta atendiendo.
        if (_viewModel.Exploration.NavigationWantsAutoZoom)
        {
            _viewModel.Exploration.Zoom = 4.0;
            WorldMapScroll.UpdateLayout();
        }
        double zoom = _viewModel.Exploration.Zoom;
        WorldMapScroll.ScrollToHorizontalOffset(tileX * zoom - WorldMapScroll.ViewportWidth / 2);
        WorldMapScroll.ScrollToVerticalOffset(tileY * zoom - WorldMapScroll.ViewportHeight / 2);
        UpdateMinimapViewport();
    }

    // F-8 (auditoria de Opus vs TEdit, E-05): minimapa real - reutiliza el bitmap del mundo YA
    // congelado (WorldRenderer.cs), sin pintar nada de nuevo.
    private void OnWorldMapScrollChanged(object sender, ScrollChangedEventArgs e) => UpdateMinimapViewport();
    private void OnMinimapSizeChanged(object sender, SizeChangedEventArgs e) => UpdateMinimapViewport();

    private void OnToggleMinimapClick(object sender, RoutedEventArgs e) =>
        _viewModel.Settings.IsMinimapVisible = !_viewModel.Settings.IsMinimapVisible;

    // Con Stretch="Uniform", la imagen real dentro de MinimapImage no ocupa toda su caja
    // (220x63) salvo que el mundo tenga exactamente esa proporcion - hay que calcular la escala
    // real Y el hueco (letterbox) para que el rectangulo de viewport caiga donde de verdad esta
    // pintado el mundo, no donde estaria si Stretch="Fill".
    private void UpdateMinimapViewport()
    {
        var img = _viewModel.Exploration.WorldImage;
        if (img == null || MinimapImage.ActualWidth <= 0 || MinimapImage.ActualHeight <= 0
            || !_viewModel.Settings.IsMinimapVisible)
        {
            MinimapViewportRect.Visibility = Visibility.Collapsed;
            return;
        }

        double escala = Math.Min(MinimapImage.ActualWidth / img.PixelWidth, MinimapImage.ActualHeight / img.PixelHeight);
        double huecoX = (MinimapImage.ActualWidth - img.PixelWidth * escala) / 2;
        double huecoY = (MinimapImage.ActualHeight - img.PixelHeight * escala) / 2;

        double zoom = _viewModel.Exploration.Zoom;
        if (zoom <= 0 || WorldMapScroll.ViewportWidth <= 0)
        {
            MinimapViewportRect.Visibility = Visibility.Collapsed;
            return;
        }
        // C-02 (auditoria de pulido final, cierra E2): a zoom muy alejado (MinZoom=0.02) el
        // viewport real en tiles de mundo (ViewportWidth/zoom) puede ser MUCHO mas grande que el
        // propio mundo (medido: 50.000 tiles de viewport contra 8.400 de ancho real en un mundo
        // Grande) - sin recortar, el rectangulo salia 6 veces mas ancho que la caja del minimapa
        // y, sin ClipToBounds en ningun contenedor, se pintaba encima de toda la ventana.
        double vpX = Math.Clamp(WorldMapScroll.HorizontalOffset / zoom, 0, img.PixelWidth);
        double vpY = Math.Clamp(WorldMapScroll.VerticalOffset / zoom, 0, img.PixelHeight);
        double vpW = Math.Min(WorldMapScroll.ViewportWidth / zoom, img.PixelWidth - vpX);
        double vpH = Math.Min(WorldMapScroll.ViewportHeight / zoom, img.PixelHeight - vpY);

        // Con el mundo entero ya visible (p.ej. "Ajustar a la ventana" o mas alejado), el
        // rectangulo coincidiria con el borde exacto del minimapa y no aportaria nada - igual que
        // cualquier minimapa real, se oculta en ese caso.
        if (vpW >= img.PixelWidth && vpH >= img.PixelHeight)
        {
            MinimapViewportRect.Visibility = Visibility.Collapsed;
            return;
        }

        Canvas.SetLeft(MinimapViewportRect, huecoX + vpX * escala);
        Canvas.SetTop(MinimapViewportRect, huecoY + vpY * escala);
        MinimapViewportRect.Width = Math.Max(1, vpW * escala);
        MinimapViewportRect.Height = Math.Max(1, vpH * escala);
        MinimapViewportRect.Visibility = Visibility.Visible;
    }

    // Clic en el minimapa -> navega, misma conversion clic->tile que TEdit
    // (MainWindow.xaml.cs:946-957: posicion del clic / Resolution -> coordenada de mundo), aqui
    // con la escala real ya calculada arriba en vez de un "Resolution" fijo por muestreo.
    private void OnMinimapClick(object sender, MouseButtonEventArgs e)
    {
        var img = _viewModel.Exploration.WorldImage;
        if (img == null || MinimapImage.ActualWidth <= 0) return;
        double escala = Math.Min(MinimapImage.ActualWidth / img.PixelWidth, MinimapImage.ActualHeight / img.PixelHeight);
        double huecoX = (MinimapImage.ActualWidth - img.PixelWidth * escala) / 2;
        double huecoY = (MinimapImage.ActualHeight - img.PixelHeight * escala) / 2;
        var clic = e.GetPosition(MinimapImage);
        int tileX = (int)((clic.X - huecoX) / escala);
        int tileY = (int)((clic.Y - huecoY) / escala);
        // Via la ViewModel (no OnNavigateToTile a pelo) para que esta navegacion resuelva su
        // NavigationWantsAutoZoom con la casilla GLOBAL - el clic en el minimapa no viene de
        // "Cofre a cofre" ni de ninguna otra seccion con casilla propia, y asi conserva
        // exactamente el comportamiento que tenia antes del punto 4.
        _viewModel.Exploration.NavigateToTile(tileX, tileY);
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
        // el destino aceptado (linea 880) - si aqui solo se permite Copy, ese Move queda FUERA
        // del conjunto de efectos permitidos por el ORIGEN del arrastre, lo que en algunos temas/
        // configuraciones de Windows hace que el cursor muestre "prohibido" durante todo el
        // arrastre pese a que Drop SI funciona. Copy|Move cubre los dos (arrastrar SIEMPRE coloca
        // una copia real del catalogo, nunca "mueve" nada de la Libreria - Move es solo lo que el
        // slot destino declara aceptar).
        if (sender is FrameworkElement { DataContext: LibraryItemViewModel item } element)
            StartCardDrag(element, new DataObject(typeof(LibraryItemViewModel), item));
    }

    // C-11 (informe de pulido final, cierra L4): "WPF arrastra sin ninguna vista previa, asi
    // que el usuario no ve que este llevando nada" - adorno real (VisualBrush de la propia
    // tarjeta, semitransparente) que sigue al cursor durante el arrastre, en vez de solo confiar
    // en el cursor del sistema (que ya anuncia aceptado/rechazado via OnItemSlotDragOver, pero
    // nunca QUE se esta arrastrando). Compartido por las dos tarjetas reales que inician
    // arrastre (Libreria de objetos y Libreria de buffs) - los slots ya muestran su propio
    // contenido real en pantalla, la confusion original era especifica de las tarjetas.
    private void StartCardDrag(FrameworkElement element, DataObject data)
    {
        // AdornerLayer.GetAdornerLayer/DragAdorner anclados al PROPIO elemento arrastrado (no a
        // la ventana) - es el patron real de WPF: el layer que encuentra ya cubre toda la
        // ventana (el AdornerDecorator implicito del template por defecto de Window), y usar el
        // mismo elemento como AdornedElement mantiene Mouse.GetPosition en el MISMO espacio de
        // coordenadas que UpdatePosition, sin tener que reproyectar nada a mano.
        var layer = AdornerLayer.GetAdornerLayer(element);
        if (layer == null) { DragDrop.DoDragDrop(element, data, DragDropEffects.Copy | DragDropEffects.Move); return; }

        var adorner = new DragAdorner(element, element);
        layer.Add(adorner);
        void OnFeedback(object? s, GiveFeedbackEventArgs e)
        {
            var pos = Mouse.GetPosition(element);
            adorner.UpdatePosition(pos.X + 12, pos.Y + 12);
        }
        element.GiveFeedback += OnFeedback;
        try
        {
            DragDrop.DoDragDrop(element, data, DragDropEffects.Copy | DragDropEffects.Move);
        }
        finally
        {
            element.GiveFeedback -= OnFeedback;
            layer.Remove(adorner);
        }
    }

    // VisualBrush de la tarjeta original, dibujado en la posicion real del cursor - IsHitTestVisible
    // en False para no interferir con el propio Drop (el adorno vive en una capa aparte, por
    // encima de todo el arbol visual de la ventana, pero nunca debe recibir eventos de raton).
    private sealed class DragAdorner : Adorner
    {
        private readonly VisualBrush _brush;
        private readonly double _width, _height;
        private double _left, _top;

        public DragAdorner(UIElement adornedElement, FrameworkElement dragged) : base(adornedElement)
        {
            _brush = new VisualBrush(dragged) { Opacity = 0.75, Stretch = Stretch.Uniform };
            _width = dragged.ActualWidth;
            _height = dragged.ActualHeight;
            IsHitTestVisible = false;
        }

        public void UpdatePosition(double left, double top)
        {
            _left = left;
            _top = top;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext) =>
            drawingContext.DrawRectangle(_brush, null, new Rect(_left, _top, _width, _height));
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

    // C-11 (informe de pulido final, cierra L4): gemelo real de OnLibraryCardClick/
    // OnLibraryClickTimerTick para la Libreria de buffs - "pasa lo mismo con la pestaña buff"
    // (un clic ahi no hacia nada, unica via real era arrastrar). _dragStartBuffLibrary es null
    // aqui cuando el gesto YA se resolvio como un arrastre real (mismo criterio de guarda que
    // OnLibraryCardClick).
    private void OnBuffLibraryCardClick(object sender, MouseButtonEventArgs e)
    {
        if (_dragStartBuffLibrary is null) return;
        _dragStartBuffLibrary = null;
        if (sender is not FrameworkElement { DataContext: BuffCatalogEntryViewModel entry }) return;

        if (e.ClickCount >= 2)
        {
            _buffLibraryClickTimer?.Stop();
            _pendingBuffLibraryClickItem = null;
            _viewModel.PlaceInFirstFreeBuffSlot(entry.Id);
            return;
        }

        _pendingBuffLibraryClickItem = entry;
        _buffLibraryClickTimer ??= new System.Windows.Threading.DispatcherTimer();
        _buffLibraryClickTimer.Stop();
        _buffLibraryClickTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(GetDoubleClickTime(), 1));
        _buffLibraryClickTimer.Tick -= OnBuffLibraryClickTimerTick;
        _buffLibraryClickTimer.Tick += OnBuffLibraryClickTimerTick;
        _buffLibraryClickTimer.Start();
    }

    private System.Windows.Threading.DispatcherTimer? _buffLibraryClickTimer;
    private BuffCatalogEntryViewModel? _pendingBuffLibraryClickItem;

    private void OnBuffLibraryClickTimerTick(object? sender, EventArgs e)
    {
        _buffLibraryClickTimer!.Stop();
        if (_pendingBuffLibraryClickItem is not { } entry) return;
        _pendingBuffLibraryClickItem = null;
        var target = _viewModel.BuffEdit.Slot;
        if (target == null) return; // el tooltip de la tarjeta ya avisa de que hace falta elegir un hueco antes
        target.PlaceBuff(entry.Id);
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

        // C-11: mismo criterio y mismo adorno real que OnLibraryCardMouseMove.
        if (sender is FrameworkElement { DataContext: BuffCatalogEntryViewModel entry } element)
            StartCardDrag(element, new DataObject(typeof(BuffCatalogEntryViewModel), entry));
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
