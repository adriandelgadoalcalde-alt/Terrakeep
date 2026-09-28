using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using Terrakeep.App.Controls;
using Terrakeep.App.ViewModels;
using Terrakeep.Core.Model;

namespace Terrakeep.App;

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

    // Actualizacion en un clic (17-sep-2026): ver el comentario real de
    // MainViewModel.CerrarAppParaActualizar - evita que OnWindowClosing vuelva a preguntar por
    // cambios sin guardar cuando el cierre lo dispara el propio flujo de actualizacion (que ya
    // pregunto lo mismo un momento antes, antes de lanzar el instalador).
    private bool _cerrandoParaActualizar;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        // NAV123 (25-sep-2026): T3 PASO 3 suscribia aqui la Vista al evento ObjetosSectionRequested
        // para medir/mover un ScrollViewer real - ya no hace falta, RequestObjetosSection solo
        // escribe ObjetosSubTabIndex y el binding de Visibility de cada pagina (MainWindow.xaml,
        // ObjetosPageHost) reacciona solo, sin ninguna intervencion de la Vista.
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
        // T4 (catalogo de rediseño visual, 20-sep-2026): mismo motivo real que el comentario de
        // arriba - marcar "ya visto" toca Settings.Persist(), que escribe a disco de verdad, asi
        // que vive aqui (la View) y no en el constructor de MainViewModel, por el mismo riesgo
        // real de contaminar los tests headless.
        // BUG REAL encontrado en esta misma ronda (investigado antes de escribir el commit, no
        // en produccion): marcarlo aqui EN LINEA, antes de que la ventana real se pinte una sola
        // vez, dejaba HasSeenHomeIntro ya en true para cuando WPF evalua el binding de
        // Visibility del parrafo por primera vez - el parrafo NUNCA llegaria a verse ni la
        // primerisima vez. Dispatcher.BeginInvoke con prioridad Loaded difiere el marcado hasta
        // DESPUES de que este frame real ya se haya pintado con el valor viejo (false).
        Dispatcher.BeginInvoke(_viewModel.Settings.MarkHomeIntroSeen, System.Windows.Threading.DispatcherPriority.Loaded);
        // BK-4 (13-sep-2026): el cupo de copias es POR personaje, asi que una carpeta de
        // historial de un personaje que ya no existe no la retiraba nadie nunca - medido en esta
        // maquina antes de arreglarlo: 2.765 carpetas y 28 MB, casi todo de rutas temporales de
        // arneses de prueba ya desaparecidas. Se limpia al arrancar, pero SOLO lo que lleva mas
        // de 90 dias huerfano (ver BackupHistoryService.OrphanGracePeriod: un historial recien
        // quedado huerfano puede ser lo unico que queda de un personaje borrado sin querer).
        // Va DESPUES de LoadFromDisk a proposito - las carpetas adicionales de Ajustes tienen que
        // estar aplicadas antes de decidir que personaje "ya no existe" - y en segundo plano:
        // recorre disco y no debe retrasar ni un milisegundo la aparicion de la ventana.
        var historial = _viewModel.BackupHistory.Service;
        Task.Run(() => { try { historial.PurgeOrphanHistories(); } catch (Exception) { /* nunca un fallo visible por una limpieza de fondo */ } });
        _viewModel.Home.RefreshCommand.Execute(null);
        _viewModel.Exploration.RefreshWorldsCommand.Execute(null);
        _viewModel.RestoreSession();
        // X1 (I+D-PROXIMOS-PASOS-FAMILIA-KEEP.md, 16-sep-2026): mismo criterio real que
        // RestoreSession() de arriba - una llamada de RED real jamas puede vivir en el
        // constructor de MainViewModel (contaminaria las decenas de tests headless del
        // proyecto). Se dispara aqui, una sola vez, en segundo plano (async void real dentro -
        // ver MainViewModel.Actualizaciones.cs), sin retrasar ni un milisegundo esta ventana.
        //
        // NUNCA en modo diagnostico (Terrakeep.App.Tests, App.ModoDiagnostico=true): es una
        // llamada real de RED (GitHub) cuyo resultado llega en un instante no determinista
        // respecto al temporizador de una captura - mismo bug real ya encontrado en Starvekeep
        // (17-sep-2026, KeepQA/snapshot visual: la tarjeta "Hay una version nueva..." colada en
        // 3/3 capturas sin ningun cambio real de la app de por medio). Ver el comentario completo
        // en App.ModoDiagnostico. PruebasActualizacion.cs sigue verificando la tarjeta a proposito
        // inyectando el estado a mano (HayActualizacionDisponible/MensajeActualizacion), nunca vía
        // esta llamada de red real.
        if (!App.ModoDiagnostico) _viewModel.IniciarComprobacionDeActualizacion();
        // H5-07: unico suscriptor real de CharacterLoaded - ver el comentario real del evento
        // en MainViewModel.cs (por que NO es una llamada directa dentro de LoadFromPath).
        _viewModel.CharacterLoaded += _viewModel.SaveSession;
        // ADR-TERRAKEEP-016/030 (27-sep-2026): Mapa+minimapa vive ahora en Views/WorldMapView.xaml
        // - NavigateToTile(int,int) tiene la MISMA firma que el delegate Action<int,int> del
        // evento, asi que se suscribe DIRECTAMENTE (mismo timing exacto que antes, sin ningun
        // forwarder intermedio en esta clase).
        _viewModel.Exploration.NavigateToTileRequested += WorldMapView.NavigateToTile;
        // F-8 (auditoria de Opus vs TEdit, E-05): el rectangulo de viewport del minimapa
        // necesita recalcularse cada vez que el mapa se desplaza (ScrollChanged, dentro de
        // WorldMapView) O cambia de zoom/mundo (Zoom/WorldImage - no pasan por ScrollChanged por
        // si solos).
        _viewModel.Exploration.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ExplorationViewModel.Zoom) or nameof(ExplorationViewModel.WorldImage))
                WorldMapView.UpdateMinimapViewport();
        };
        // Auditoria de Opus, T-B (segunda auditoria, Fable): mismo dialogo real de
        // "cambios sin guardar" que OnWindowClosing, ahora tambien antes de cargar OTRO
        // personaje por encima desde Inicio.
        _viewModel.ConfirmDiscardChanges = () => ConfirmDiscardChanges(Loc["dlg_action_load_other"]);
        // Actualizacion en un clic (17-sep-2026): mismo dialogo real Si/No/Cancelar, con
        // "actualizar Terrakeep" como la accion - ver MainViewModel.Actualizaciones.cs.
        _viewModel.ConfirmDiscardChangesForUpdate = () => ConfirmDiscardChanges(Loc["dlg_action_update"]);
        // El instalador ya esta lanzado y esperando de verdad a que este proceso termine (ver el
        // comentario real de CerrarAppParaActualizar) - Close() normal (no Environment.Exit) para
        // que WindowPlacementService.Save/SaveSession sigan corriendo como en cualquier cierre,
        // pero _cerrandoParaActualizar evita que OnWindowClosing vuelva a preguntar por cambios
        // sin guardar: ActualizarAhora() ya lo pregunto una vez (ConfirmDiscardChangesForUpdate)
        // antes de lanzar el instalador - un segundo aviso aqui podria dejar al instalador
        // esperando en vano a que este proceso muera (Wait-Process tiene un timeout real, ver
        // LanzarInstaladorYRelanzar) si el usuario cancelara este segundo dialogo redundante.
        _viewModel.CerrarAppParaActualizar = () => { _cerrandoParaActualizar = true; Close(); };
        // Auditoria de Opus, Bloque 4 (T-3): restaura el tamaño/posicion real de la ultima
        // sesion - antes de Show(), Width/Height/Left/Top ya se pueden fijar sin parpadeo.
        Services.WindowPlacementService.Apply(this);
        // Auditoria de Opus, Bloque 4 (T-2): valor inicial real (Width ya refleja lo que
        // Apply() acaba de restaurar, fiable incluso antes de que el layout corra) - despues,
        // SizeChanged mantiene SizeClass vivo con el ancho real ya descontado el chrome.
        // H5-08 (quinta auditoria de Opus): tambien la altura real, ver WindowHeightClass.cs.
        _viewModel.UpdateSizeClass(Width, Height);
        SizeChanged += (_, e) => _viewModel.UpdateSizeClass(e.NewSize.Width, e.NewSize.Height);

        // Bug2 (TASK CONTEXT e5eaea9e-c261-4199-8e7d-060b6054f58d): si Terrakeep se lanzo con
        // "--abrir-mundo <ruta.wld>" real (ver App.ParsePendingWorldPath/App.OnStartup), se carga
        // aqui, ANTES de que WPF muestre esta ventana (StartupUri="MainWindow.xaml" en App.xaml
        // llama a Show() justo despues de que este constructor termine) - reutilizando el MISMO
        // camino real que ya usan OnLoadWorldClick/OnWindowDrop (LoadWorldAndRestoreView), nunca
        // uno paralelo. Fire-and-forget porque un constructor no puede ser async - seguro: la
        // carga real (ExplorationViewModel.LoadFromPathAsync) ya atrapa sus propias excepciones
        // en su try/catch/finally, nunca deja una excepcion sin capturar escapar de aqui.
        if (App.PendingWorldPath is string rutaMundoInicial)
            _ = AbrirMundoInicialAsync(rutaMundoInicial);

        // Bug real arreglado 28-sep-2026 (reporte del usuario, ver comentario completo en
        // OnPersonajeMenuClick/OnPersonajeMenuOutsideClick mas abajo): se registra UNA sola vez,
        // aqui, para toda la vida de la ventana - el mismo ContextMenu (PersonajeMenuButton.
        // ContextMenu, instancia unica declarada en XAML) se abre/cierra muchas veces, pero el
        // registro de este manejador no depende de si esta abierto o cerrado en cada momento.
        Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(PersonajeMenuButton.ContextMenu, OnPersonajeMenuOutsideClick);
    }

    private async Task AbrirMundoInicialAsync(string path)
    {
        await WorldMapView.LoadWorldAndRestoreView(path);
        _viewModel.SelectedTabIndex = 4; // AppTab.Exploracion, privado - mismo criterio que OnWindowDrop
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
        // perder al guardarla de todos modos. ADR-TERRAKEEP-016/030: gancho publico de
        // WorldMapView (mismo que usa LoadWorldAndRestoreView), el WorldMapScroll real ya vive
        // dentro de ese UserControl.
        WorldMapView.SaveCurrentViewState();
        if (!_cerrandoParaActualizar && !ConfirmDiscardChanges("cerrar")) e.Cancel = true;
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
            // ADR-TERRAKEEP-016/029 (26-sep-2026): WorldSearchBox vive ahora dentro de
            // Views/BrowseView.xaml (NameScope propio) - gancho publico minimo en vez de un campo
            // directo, mismo mecanismo real ya usado por HomeView/ADR-026 para OnLoadClick.
            Dispatcher.BeginInvoke(new Action(() => BrowseView.FocusWorldSearchBox()),
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
        // BK (13-sep-2026): Ctrl+H abre el historial de versiones del personaje cargado. H de
        // "historial"/"history" en los dos idiomas, y estaba libre (ver la lista real de atajos
        // ya usados en este mismo metodo).
        else if (ctrl && e.Key == Key.H)
        {
            if (_viewModel.OpenBackupHistoryCommand.CanExecute(null)) _viewModel.OpenBackupHistoryCommand.Execute(null);
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
                // ADR-TERRAKEEP-016/031 (27-sep-2026): LibrarySearchBox vive ahora dentro de
                // ObjetosView (NameScope propio) - gancho publico minimo
                // ObjetosView.FocusLibrarySearchBox(), mismo mecanismo ya usado por
                // BrowseView.FocusWorldSearchBox()/ADR-029. BuffLibrarySearchBox (Buffs, sin
                // extraer todavia) sigue resolviendose directo aqui.
                if (buffs) { BuffLibrarySearchBox.Focus(); BuffLibrarySearchBox.SelectAll(); }
                else ObjetosView.FocusLibrarySearchBox();
            }), System.Windows.Threading.DispatcherPriority.Background);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // F-17 (auditoria de Opus vs TEdit, B-05): comprobado que StaysOpen="False" por si
            // solo NO cierra un Popup de WPF con Escape (hace falta codigo propio) - antes de
            // esto no habia ningun manejador que cerrara WhereIsItPopup con esta tecla.
            // BK (13-sep-2026): el panel de historial es lo mas "modal" que hay en esta ventana
            // (velo opaco que bloquea el editor de detras) - si esta abierto, Escape es suyo
            // antes que de nada, que es lo que espera cualquiera.
            if (_viewModel.BackupHistory.IsOpen) _viewModel.BackupHistory.CloseCommand.Execute(null);
            // Comparador: ya NO es un overlay (catalogo de rediseño visual T9, 20-sep-2026 - es
            // una pestaña real de Personaje), Escape no tiene nada que cerrar ahi, igual que en
            // cualquier otra pestaña de la app.
            // Codigos de build (13-sep-2026): mismo overlay, mismo criterio.
            else if (_viewModel.IsBuildCodeOpen) _viewModel.CloseBuildCodeCommand.Execute(null);
            // Vista previa de generacion de mundo (14-sep-2026): mismo overlay, mismo criterio.
            else if (_viewModel.WorldPreview.IsOpen) _viewModel.WorldPreview.CloseCommand.Execute(null);
            else if (_viewModel.IsWhereIsItOpen) _viewModel.IsWhereIsItOpen = false;
            else if (_viewModel.Library.IsPicking) _viewModel.Library.CancelPickCommand.Execute(null);
            else if (_viewModel.BuffLibrary.IsPicking) _viewModel.BuffLibrary.CancelPickCommand.Execute(null);
            else return; // nada real que cancelar - no consumir la tecla (ej. cerrar un ComboBox abierto)
            e.Handled = true;
        }
        // H5-14 (quinta auditoria de Opus): "Ctrl+1...6 para las 6 pestañas raiz, numero
        // anunciado en el propio rotulo" (mismo criterio T-H/F1) - salto directo sin pasar por
        // el raton, ningun conflicto real con un TextBox (Ctrl+numero no es un gesto de tecleo
        // normal, a diferencia de Ctrl+Z/Y que SI colisionan con el deshacer nativo de un campo).
        // Fase B (15-sep-2026): ampliado de D6 a D8 - Guia (Ctrl+7) y el hosting de ServidorKeep
        // (Ctrl+8), las dos pestañas nuevas al final del TabControl.
        else if (ctrl && e.Key is >= Key.D1 and <= Key.D8)
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
                // ADR-TERRAKEEP-016/030: PanBy(dx, dy) es el gancho publico de WorldMapView -
                // mismo paso (60) y mismo sentido exactos que antes, solo movido de sitio.
                const double paso = 60;
                switch (e.Key)
                {
                    case Key.Left: WorldMapView.PanBy(-paso, 0); break;
                    case Key.Right: WorldMapView.PanBy(paso, 0); break;
                    case Key.Up: WorldMapView.PanBy(0, -paso); break;
                    case Key.Down: WorldMapView.PanBy(0, paso); break;
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

    // Codigos de build (13-sep-2026): el TextBox de "tu codigo" es de solo lectura - un clic
    // dentro selecciona el texto entero de una vez, listo para Ctrl+C real, en vez de obligar a
    // arrastrar el raton sobre ~160 caracteres en fuente monoespaciada.
    private void OnBuildCodeTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox) textBox.SelectAll();
    }

    // ADR-TERRAKEEP-016/020 (26-sep-2026): los 3 manejadores de Click de la pestaña ACERCA DE
    // (elegir carpeta de personajes/mundos, fijar tamaño de ventana) se movieron a
    // Views/AboutView.xaml.cs junto con el XAML que los usa - ver el comentario real de ese
    // archivo para el razonamiento completo (this->Window.GetWindow(this),
    // _viewModel->(MainViewModel)DataContext) y el historico de bugs reales de AJU-01.

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
            await WorldMapView.LoadWorldAndRestoreView(path);
            _viewModel.SelectedTabIndex = 4; // AppTab.Exploracion, privado - mismo criterio ya usado en el arnes
        }
        else if (ext == ".plr")
        {
            if (!ConfirmDiscardChanges(Loc["dlg_action_load_other"])) return;
            _viewModel.LoadFromPath(path);
        }
    }

    // T2 (catalogo de rediseño visual, 20-sep-2026): un ContextMenu real de WPF solo se abre con
    // clic DERECHO por omision - este es el truco real ya conocido para que un Button lo abra con
    // clic IZQUIERDO normal, igual que cualquier boton de menu desplegable (Ctrl+O/el resto de la
    // cabecera siguen intactos, este es solo el disparador del menu "⋯ Personaje").
    //
    // Bug real arreglado 28-sep-2026 (reporte del usuario: "un clic lo despliega, otro clic no
    // acaba de volver a plegar bien"). Causa raiz confirmada con un arnes real de clic de SISTEMA
    // OPERATIVO (down+up de mouse_event, no un RoutedEventArgs sintetico) contra
    // PersonajeMenuButton con el menu ya abierto: mientras el ContextMenu tiene la captura del
    // raton (CaptureMode.SubTree), un segundo mouse-down FISICO sobre el boton cuenta como "fuera"
    // del ContextMenu (el boton no es descendiente suyo) - WPF lo cierra por su cuenta ANTES de
    // que el propio down-event empiece siquiera a recorrer el arbol visual con los eventos
    // normales (Preview/Tunnel): medido con el arnes, ContextMenu.IsOpen ya vale False incluso en
    // el PreviewMouseDown mas temprano posible (a nivel de Window), y el evento Closed del
    // ContextMenu NUNCA llega a dispararse para este camino de cierre en absoluto (confirmado
    // suscribiendo un handler externo directo: jamas se invoca, aunque IsOpen si cambia) - asi que
    // cualquier intento de detectar el cierre desde Closed (probado con IsMouseOver y tambien con
    // Mouse.GetPosition, los dos descartados con evidencia real) llega sistematicamente tarde o
    // nunca, y el Click que sigue en el MouseUp de ese mismo clic (OnPersonajeMenuClick, sin
    // condicion ninguna) volvia a abrirlo de inmediato - un "cierra y reabre" instantaneo que el
    // usuario percibe como que el toggle nunca cierra.
    //
    // La señal fiable es Mouse.AddPreviewMouseDownOutsideCapturedElementHandler: la MISMA API
    // publica de WPF que usa su propio mecanismo interno de cierre para detectar "mouse-down fuera
    // del elemento con captura" - registrando NUESTRO PROPIO handler para ese mismo evento (una
    // vez, en el constructor) se nos avisa TAMBIEN, de forma sincrona, durante el procesado de ese
    // mismo down-event (antes de que el Click del boton llegue en el MouseUp) - a diferencia de
    // Closed, este SI se dispara siempre, confirmado con el arnes real. Los argumentos del evento
    // traen la posicion real del clic (MouseButtonEventArgs.GetPosition, una transformacion
    // geometrica de la posicion fisica del cursor, NO basada en hit-testing con ruteo de eventos -
    // por eso no le afecta el mismo sesgo que rompia IsMouseOver/DirectlyOver bajo captura) -
    // comprobar si esa posicion cae dentro del rectangulo real del boton (0..ActualWidth,
    // 0..ActualHeight) SI detecta correctamente "el cierre lo causo un clic sobre este mismo
    // boton", confirmado con el mismo arnes real tras el cambio (los 3 clics reales consecutivos
    // abren/cierran/reabren correctamente, ver PERSONAJEMENU_TOGGLE_SOLO en
    // Terrakeep.App.Tests/Program.cs). Un cierre por Escape/clic en otra parte de la
    // ventana/eleccion de un item deja el cursor fuera del rectangulo del boton (o no dispara este
    // evento en absoluto, al no tratarse de un "clic fuera con captura") y no activa esta supresion.
    //
    // Efecto secundario real, tambien medido con el arnes y corregido aqui de paso: como Closed
    // NUNCA se dispara para este camino de cierre (ver arriba), IsPersonajeMenuOpen (el booleano
    // que pinta la flecha ▾/▲ y el Tag="Open" de Theme.xaml) se quedaba en True tras este cierre -
    // el ContextMenu ya estaba cerrado de verdad pero el boton seguia con el aspecto "abierto"
    // hasta la siguiente apertura real. Este handler SI sabe con certeza que el menu se acaba de
    // cerrar (es el unico motivo por el que se le ha llamado), asi que actualiza el ViewModel el
    // mismo, sin esperar a un Closed que en este camino no va a llegar.
    private bool _suprimirProximaAperturaPersonajeMenu;

    private void OnPersonajeMenuOutsideClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ContextMenu { PlacementTarget: FrameworkElement boton }) return;

        _viewModel.IsPersonajeMenuOpen = false;

        Point posRelativa = e.GetPosition(boton);
        bool sobreBoton = posRelativa.X >= 0 && posRelativa.X <= boton.ActualWidth
            && posRelativa.Y >= 0 && posRelativa.Y <= boton.ActualHeight;
        if (sobreBoton) _suprimirProximaAperturaPersonajeMenu = true;
    }

    private void OnPersonajeMenuClick(object sender, RoutedEventArgs e)
    {
        if (_suprimirProximaAperturaPersonajeMenu)
        {
            _suprimirProximaAperturaPersonajeMenu = false;
            return;
        }

        if (sender is FrameworkElement fe && fe.ContextMenu != null)
        {
            fe.ContextMenu.PlacementTarget = fe;
            fe.ContextMenu.IsOpen = true;
        }
    }

    // Bug real arreglado 25-sep-2026 (handoff e5eaea9e-c261-4199-8e7d-060b6054f58d): un ContextMenu,
    // a diferencia de un Popup normal, no expone IsOpen como binding de doble via util aqui (se abre
    // a mano en OnPersonajeMenuClick) - estos dos eventos son el unico punto real donde WPF avisa de
    // verdad de que se abrio/cerro, y son los que mantienen IsPersonajeMenuOpen en sincronia para que
    // el boton (Tag="Open" + flecha ▾/▲, ver MainWindow.xaml) refleje el estado real.
    private void OnPersonajeMenuOpened(object sender, RoutedEventArgs e) => _viewModel.IsPersonajeMenuOpen = true;

    private void OnPersonajeMenuClosed(object sender, RoutedEventArgs e) => _viewModel.IsPersonajeMenuOpen = false;

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

        if (dialog.ShowDialog(this) == true) await WorldMapView.LoadWorldAndRestoreView(dialog.FileName);
    }

    // Idea 8 (catalogo de funciones, "Informe y comparador de mundos" - bitacora.md
    // 20-sep-2026): mismo dialogo/filtro que OnLoadWorldClick de arriba, pero contra el
    // comparador independiente (WorldCompareViewModel) - nunca toca _viewModel.Exploration ni el
    // mundo que pueda estar abierto ahi mismo.
    private async void OnLoadWorldCompareAClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc["dlg_load_world"],
            Filter = Loc["dlg_filter_world"],
            InitialDirectory = Services.CharacterFileService.GetDefaultWorldsDirectory(),
        };
        if (dialog.ShowDialog(this) == true) await _viewModel.WorldCompare.LoadAAsync(dialog.FileName);
    }

    private async void OnLoadWorldCompareBClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc["dlg_load_world"],
            Filter = Loc["dlg_filter_world"],
            InitialDirectory = Services.CharacterFileService.GetDefaultWorldsDirectory(),
        };
        if (dialog.ShowDialog(this) == true) await _viewModel.WorldCompare.LoadBAsync(dialog.FileName);
    }

    // ExploracionRediseno Fase G (25-sep-2026): "Volver al mapa" de la vista amplia del
    // comparador - vuelve a All (mismo reset real que ya usa otras rutas de la Exploracion,
    // ej. LoadFromPathAsync) en vez de a un WorldSearchCategory.None inexistente. Cambiar
    // SelectedCategory dispara OnSelectedCategoryChanged (ExplorationViewModel), que ya pone
    // IsShowingWorldCompare=false y ademas desmarca la pildora "Comparar" del selector (misma
    // fuente real, sin estado duplicado). WorldCompare es independiente del mundo cargado en
    // Exploracion (WorldCompareViewModel.cs:31-39, _worldA/_worldB propios) - no hay nada que
    // recargar ni destruir aqui, solo un cambio de categoria.
    private void OnCloseWorldCompareClick(object sender, RoutedEventArgs e)
        => _viewModel.Exploration.SelectedCategory = WorldSearchCategory.All;

    // Idea 6 (catalogo de funciones, "Laboratorio de personajes" - bitacora.md 20-sep-2026,
    // segunda pasada 20-sep-2026 reconsiderada a peticion explicita del coordinador):
    // investigado a fondo - no existe NINGUNA fabrica real de personaje "en blanco" en todo el
    // proyecto (ni Core ni App): builds.json describe equipo, nunca un PlrCharacter valido desde
    // cero (GUID, stats de partida, inventario inicial real...) - inventar ese formato entero es
    // una pieza nueva y grande, LIMITE real de esta ronda, documentado aqui con evidencia (grep
    // real a "NewCharacter"/"CreateCharacter"/"CrearPersonaje" en todo el repo: 0 resultados).
    // Camino real SI viable, sin inventar ese formato: el personaje YA cargado sirve de
    // PLANTILLA 100% valida (garantiza un .plr correcto, el mismo que ya pasa por Load/Save
    // reales) - duplicarlo a un fichero nuevo (mismo mecanismo real que HomeViewModel.Duplicate),
    // vaciar equipo+inventario y aplicar AutoEquipService.Apply (la MISMA regla de negocio que ya
    // usa el boton "Auto-equipar" de Builds, reutilizada tal cual via AutoEquipCommand) entrega
    // "equipo recomendado... con su mejor prefijo".
    //
    // "investigacion coherente" (segunda pasada): SI hay dato real y sin ambiguedad - los mismos
    // objetos que se acaban de equipar, marcados como investigados (ResearchViewModel.
    // MarkResearchedByPid, reutiliza los "pid" YA reales de BuildItemRef, mismo formato exacto
    // que PlrResearchEntry.Pid). Un personaje "listo para X" con el equipo puesto pero sin
    // investigarlo seria una incoherencia real en Modo Viaje.
    //
    // "vida/mana minimos del tramo": investigado a fondo, NO se implementa - motivo real
    // distinto para cada uno, ninguno de los dos es pereza:
    //   - Mana: TipoRequisitoGuia (Terrakeep.Core/Guia/GuideModel.cs) no tiene NINGUN tipo de
    //     requisito de mana, y guia_progresion.json (46 tramos reales) no menciona mana en
    //     ningun sitio - Terraria real no gatea la progresion por mana (mecanica opcional, solo
    //     relevante para magos), asi que no hay ningun dato que "vida/mana" pudiera estar
    //     escondiendo - ausencia real, no un hueco de extraccion.
    //   - Vida: SI existe un dato real (TipoRequisitoGuia.CristalesVida/VidaMaxima), pero
    //     SOLO en 6 de los 46 tramos reales (grep exacto a guia_progresion.json: "cristales_vida"
    //     x5, "vida_maxima" x1 - Terraria real no gatea el 90% de los jefes por vida explicita).
    //     Ademas, builds.json agrupa en 3 ETAPAS AMPLIAS ("prehardmode"/"hardmode"/"postml") sin
    //     ningun campo que las relacione con un tramo CONCRETO de los 46 (un boss por tramo) -
    //     inventar esa correspondencia (que tramo de vida usar para "listo para hardmode" a
    //     secas, cuando hardmode tiene mas de 20 tramos reales) seria adivinar, no leer un dato
    //     real. LIMITE real, distinto del de mana: aqui el dato existe pero es demasiado disperso/
    //     de grano mas fino que builds.json para mapearlo sin inventar la correspondencia.
    private void OnGenerateCharacterForBuildClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: BuildClassGearViewModel classGear }) return;
        string? sourcePath = _viewModel.Home.Characters.FirstOrDefault(c => c.IsCurrent)?.FilePath;
        if (sourcePath == null) return; // IsCharacterLoaded ya lo garantiza via IsEnabled - doble comprobacion real

        var dialog = new SaveFileDialog
        {
            Title = Loc["dlg_generate_character"],
            Filter = Loc["dlg_filter_character"],
            InitialDirectory = Services.CharacterFileService.GetDefaultPlayersDirectory(),
            FileName = $"{_viewModel.CharacterName} - {classGear.ClassLabel}.plr",
        };
        if (dialog.ShowDialog(this) != true) return;
        if (!ConfirmDiscardChanges(Loc["dlg_action_load_other"])) return;

        try
        {
            File.Copy(sourcePath, dialog.FileName, overwrite: true);
            string tplrSrc = Path.ChangeExtension(sourcePath, ".tplr");
            if (File.Exists(tplrSrc)) File.Copy(tplrSrc, Path.ChangeExtension(dialog.FileName, ".tplr"), overwrite: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, Loc.Format("error_duplicating", ex.Message));
            return;
        }

        _viewModel.LoadFromPath(dialog.FileName);

        // Vacia equipo puesto (3 armadura + 5 accesorios del loadout activo) + inventario entero
        // - mismo criterio real documentado arriba: "listo para X" no debe arrastrar objetos
        // sueltos de la plantilla que no tengan que ver con el build elegido, y deja hueco libre
        // real para que AutoEquipService.Apply coloque las armas (usa "el primer hueco vacio").
        if (_viewModel.EquipmentGroup != null)
            foreach (var slot in _viewModel.EquipmentGroup.CurrentItems.Slots)
                slot.UpdateFrom(GameItem.Empty);
        var inventario = _viewModel.Containers.FirstOrDefault(c => c.Key == "inventory");
        if (inventario != null)
            foreach (var slot in inventario.Slots)
                slot.UpdateFrom(GameItem.Empty);

        _viewModel.AutoEquipCommand.Execute(classGear.Source);

        // "investigacion coherente" (idea 6, segunda pasada) - ver el comentario real de
        // cabecera de este metodo. Los mismos Pid reales que builds.json/builds_calamity.json ya
        // traen para el equipo recien puesto, marcados como investigados.
        var source = classGear.Source;
        var pidsDelEquipo = source.Armor.Concat(source.Weapons).Concat(source.Accessories)
            .Select(item => item.Pid).Where(pid => !string.IsNullOrEmpty(pid))!;
        _viewModel.Research.MarkResearchedByPid(pidsDelEquipo!);
    }

    // Idea 5 (catalogo de funciones, "¿Donde esta? global, multi-mundo y multi-personaje" -
    // bitacora.md 20-sep-2026): un resultado de mundo NO selecciona el slot exacto en el mapa
    // (eso tocaria la zona de marcadores/AR-MRK, fuera de alcance de esta idea) - abre el mundo
    // real y reutiliza el buscador YA existente de Exploracion (categoria Cofres, "Por lo que
    // contienen", mismo texto) para que el usuario llegue al objeto con la misma navegacion/
    // marcado que ya conoce, sin reimplementar nada de eso aqui.
    private async void OnGlobalWorldHitClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ViewModels.GlobalWorldHitViewModel hit }) return;
        _viewModel.IsWhereIsItOpen = false;
        await WorldMapView.LoadWorldAndRestoreView(hit.FilePath);
        _viewModel.SelectedTabIndex = 4; // Exploracion
        _viewModel.Exploration.SelectedCategory = ViewModels.WorldSearchCategory.Chests;
        _viewModel.Exploration.ChestViewMode = 1; // "Por lo que contienen"
        _viewModel.Exploration.WorldSearchText = hit.ItemName;
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

    // Cierre del punto 1 del checklist de cierre (14-sep-2026): a la ventana MINIMA real
    // (1080x700) el viewport de esta columna no llega a cubrir su propia cabecera fija (titulo+
    // 3 Expanders colapsados+pildoras+buscador) antes de necesitar scroll, y nada en pantalla
    // avisaba de que habia que bajar - ver el comentario largo junto a MinHeight="800" en
    // MainWindow.xaml. En vez de recortar la cabecera (arriesga los MinHeight ya calibrados,
    // AR-11f/AR-15/AR-EX1/FALLO-3) o quitar contenido real, un indicador propio
    // (ExplorationScrollHint) que solo se enseña mientras de verdad queda recorrido por debajo.
    // ScrollChanged dispara con cualquier cambio real de Extent/Viewport/Offset - tamaño de
    // ventana, ancho de sidebar (GridSplitter) o contenido nuevo (mundo cargado, categoria
    // distinta) lo recalculan todos por igual, no hace falta escuchar cada uno por separado.
    private void OnExplorationSidebarScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (ExplorationScrollHint == null) return;
        // Margen de 2px: evita parpadeo por redondeo de layout cuando el contenido mide justo
        // igual que el viewport (mismo umbral ya usado por AR-11f para el caso simetrico).
        bool quedaScrollPendiente = e.ExtentHeight - e.ViewportHeight > 2
                                     && e.VerticalOffset < e.ExtentHeight - e.ViewportHeight - 2;
        ExplorationScrollHint.Visibility = quedaScrollPendiente ? Visibility.Visible : Visibility.Collapsed;
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

    // Catalogo de ideas Keep, idea 10 (20-sep-2026): mismo patron real que OnExportMapClick de
    // arriba - dialogo real en la View (MainViewModel headless), export en si en el ViewModel.
    private void OnExportPreviewClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Appearance.PreviewImage == null) return;
        var dialog = new SaveFileDialog
        {
            Title = Loc["dlg_export_preview"],
            Filter = Loc["dlg_filter_png"],
            FileName = $"{_viewModel.CharacterName}-vista-previa.png",
        };
        if (dialog.ShowDialog(this) == true) _viewModel.Appearance.ExportPreviewToPng(dialog.FileName);
    }

    // Catalogo de ideas Keep, idea 10 ("vista previa animada del personaje, exportable") - gemelo
    // real de OnExportPreviewClick de arriba, para el ciclo de andar completo en GIF
    // (AppearanceViewModel.ExportPreviewToGif).
    private void OnExportPreviewGifClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = Loc["dlg_export_preview_gif"],
            Filter = Loc["dlg_filter_gif"],
            FileName = $"{_viewModel.CharacterName}-ciclo-de-andar.gif",
        };
        if (dialog.ShowDialog(this) == true) _viewModel.Appearance.ExportPreviewToGif(dialog.FileName);
    }

    // Catalogo de ideas Keep, idea 8 ("Informe y comparador de mundos", "salida como tarjeta
    // compartible (PNG/HTML)") - mismo patron real de OnExportPreviewClick/OnExportPreviewGifClick.
    private void OnExportWorldCompareCardHtmlClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = Loc["dlg_export_card_html"],
            Filter = Loc["dlg_filter_html"],
            FileName = $"{_viewModel.WorldCompare.NameA}-vs-{_viewModel.WorldCompare.NameB}.html",
        };
        if (dialog.ShowDialog(this) == true) _viewModel.WorldCompare.ExportCardToHtml(dialog.FileName);
    }

    private void OnExportWorldCompareCardPngClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = Loc["dlg_export_card_png"],
            Filter = Loc["dlg_filter_png"],
            FileName = $"{_viewModel.WorldCompare.NameA}-vs-{_viewModel.WorldCompare.NameB}.png",
        };
        if (dialog.ShowDialog(this) == true) _viewModel.WorldCompare.ExportCardToPng(dialog.FileName);
    }

    // H4-08 (cuarta auditoria de Opus, Fable): gemelo real de OnLoadWorldClick - una tarjeta del
    // lanzador de mundos ya trae su ruta real (DataContext), no hace falta el dialogo del
    // Explorador de archivos. Mismo ajuste de zoom real al terminar.
    private async void OnWorldCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: WorldListEntryViewModel entry }) return;
        await WorldMapView.LoadWorldAndRestoreView(entry.FilePath);
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
    // en WorldMapView (ADR-TERRAKEEP-016/030), este handler se queda aqui solo porque el boton
    // que lo dispara vive en la barra de herramientas de MainWindow.xaml, fuera de ese bloque.
    private void OnFitToWindowClick(object sender, RoutedEventArgs e) => WorldMapView.FitToWindow();

    // ADR-TERRAKEEP-016/030 (27-sep-2026): LoadWorldAndRestoreView (unico punto real de carga de
    // un mundo, los sitios que antes hacian await LoadFromPathAsync + FitWorldMapToWindow por su
    // cuenta pasan a llamar aqui) se MOVIO ENTERO a WorldMapView.LoadWorldAndRestoreView(path) -
    // hallazgo real no anticipado por el grep inicial de este bloque: el boton "Cargar mundo" del
    // estado vacio ("Sin mundo cargado", dentro del propio Border del mapa) usa Click=
    // "OnLoadWorldClick", el MISMO manejador que el boton de la barra de herramientas (fuera del
    // bloque) - caso DUAL, mismo patron ya resuelto por OnLoadClick/HomeView/ADR-026: WorldMapView
    // se queda con su PROPIA copia de OnLoadWorldClick (Window.GetWindow(this) en vez de "this")
    // y, ya que el propio metodo de carga real vive intimamente ligado al mapa (guarda/restaura
    // SU vista), se movio entero con el en vez de reenviar cada llamada por separado.

    // ADR-TERRAKEEP-016/030 (27-sep-2026): OnWorldMapPreviewMouseWheel/OnWorldMapMouseDown/
    // OnWorldMapMouseUp/OnWorldMapMouseMove/OnWorldMapMouseLeave/OriginatesFromClickableMarker/
    // PositionMapTooltip (+ los campos _mapDragStart*/MapClickSlopPx) se MOVIERON a
    // Terrakeep.App/Views/WorldMapView.xaml.cs junto con el XAML que los usa - ninguno se
    // referenciaba fuera de ese bloque (grep confirmado antes de mover nada), ver el comentario
    // real completo en WorldMapView.xaml.

    // ADR-TERRAKEEP-016/026 (26-sep-2026): los manejadores de hover de Inicio
    // (OnCharacterCardMouseEnter/Leave, OnCharacterCardUnloaded, OnHomeBannerMouseEnter/Leave)
    // se MOVIERON a Terrakeep.App/Views/HomeView.xaml.cs junto con la DECIMA extraccion real de
    // MainWindow.xaml (seccion INICIO) - ninguno se usaba fuera de esa seccion (grep confirmado
    // antes de mover nada), ver el comentario real completo en HomeView.xaml.cs.

    // ADR-TERRAKEEP-016/030 (27-sep-2026): OnNavigateToTile se convirtio en el gancho publico
    // NavigateToTile(int,int) de WorldMapView (firma identica al delegate Action<int,int> del
    // evento Exploration.NavigateToTileRequested, suscrito directamente desde el constructor).
    // OnWorldMapScrollChanged/OnMinimapSizeChanged/OnToggleMinimapClick/OnMinimapClick se
    // MOVIERON tal cual a WorldMapView.xaml.cs (solo se usaban dentro de ese bloque).
    // UpdateMinimapViewport paso de privado a PUBLICO en WorldMapView (lo sigue necesitando el
    // PropertyChanged de Zoom/WorldImage suscrito en el constructor de esta clase). Ver el
    // comentario real completo en WorldMapView.xaml.cs.

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
            DragDropSupport.StartCardDrag(element, new DataObject(typeof(BuffSlotViewModel), slot), DragDropEffects.Move);
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

        // C-11: mismo criterio y mismo adorno real que OnLibraryCardMouseMove (ObjetosView.xaml.cs).
        if (sender is FrameworkElement { DataContext: BuffCatalogEntryViewModel entry } element)
            DragDropSupport.StartCardDrag(element, new DataObject(typeof(BuffCatalogEntryViewModel), entry));
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
            // H4-04: si se rechaza (duplicado real), el aviso real (RejectionMessage, que pone
            // PlaceBuff) tiene que quedar a la vista en el panel "Editar buff seleccionado", "en
            // vez de perderse sin que se note nada".
            //
            // Oleada del 6-sep-2026 - BUG REAL: el ORDEN anulaba ese arreglo. SelectBuffSlot
            // pone IsSelected=true y BuffSlotViewModel.OnIsSelectedChanged limpia
            // RejectionMessage a proposito (L-e: un aviso de rechazo no debe sobrevivir a un
            // cambio de seleccion), asi que seleccionar DESPUES de colocar borraba justo el
            // aviso recien puesto - salvo por casualidad cuando el slot destino YA era el
            // seleccionado, unico caso en que IsSelected no cambia de valor y el manejador no
            // corre. Se selecciona ANTES: el aviso del rechazo sobrevive, y en caso de exito el
            // panel Editar queda mirando el buff recien soltado, igual que ya hace "Elegir...".
            _viewModel.SelectBuffSlot(targetSlot);
            targetSlot.PlaceBuff(libraryEntry.Id);
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
            // Oleada del 6-sep-2026: mismo hueco real que el arrastre (ver OnBuffSlotDrop).
            // PasteBuff SI respeta la regla de "sin dos instancias del mismo buff" y deja su
            // RejectionMessage, pero por teclado ese aviso no se veia en ningun sitio: el panel
            // Editar sigue al slot SELECCIONADO, y el foco de teclado no lo es. Se selecciona
            // ANTES de pegar (nunca despues, que borraria el aviso, L-e).
            if (_buffClipboard is { } clip)
            {
                _viewModel.SelectBuffSlot(slot);
                slot.PasteBuff(clip.id, clip.time);
            }
            e.Handled = true;
        }
    }

    // Encargo de pulido visual (17-sep-2026): "el cambio de pestaña hoy es brusco, sin ninguna
    // animacion" - confirmado investigando primero (NavTabControl en Theme.xaml solo pone
    // TabStripPlacement/Background/BorderThickness/ItemContainerStyle, ningun ControlTemplate
    // propio - usa el ContentPresenter de fabrica de WPF, que cambia el contenido de golpe).
    // En vez de reescribir el ControlTemplate entero de TabControl (chrome real de
    // TabStripPlacement="Left" + TabPanel, riesgo de romper algo que ya funciona - ver "Verdades
    // del entorno WPF" en CLAUDE.md sobre Setter.TargetName/Storyboard.TargetName dentro de
    // plantillas), la transicion se aplica en code-behind directamente sobre el
    // ContentPresenter real que YA trae la plantilla de fabrica con el nombre fijo
    // "PART_SelectedContentHost" (documentado, mismo nombre en Aero2/Fluent) - un
    // BeginAnimation directo sobre ese elemento no necesita ningun TargetName de Storyboard.
    //
    // SelectionChanged es un RoutedEvent con estrategia Bubble: las TabControl INTERNAS
    // (Personaje/Novedades, InnerTabControl) burbujean su propio cambio hasta este handler
    // tambien - de ahi el guard de e.OriginalSource contra la instancia real de RootTabControl
    // (nunca animar por un cambio de pestaña interna, el encargo es solo para las principales).
    private ContentPresenter? _rootTabContentHost;

    private void OnRootTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, RootTabControl)) return;
        // "Reducir movimiento" real de Windows (Accesibilidad > Efectos visuales > Animaciones,
        // Configuracion > Accesibilidad > Efectos visuales en Windows 11) - SystemParameters.
        // ClientAreaAnimation es el mapeo WPF real de SPI_GETCLIENTAREAANIMATION, la misma señal
        // que ya respetan los ComboBox/menus nativos de Windows. Sin animacion: el contenido
        // nuevo aparece de golpe (comportamiento identico al que ya habia antes de este cambio),
        // nunca se deja a medio hacer (Opacity/RenderTransform quedan en su valor final).
        var host = FindSelectedContentHost();
        if (host == null) return;
        // BUG REAL encontrado verificando con keepqa-snapshot-visual (17-sep-2026, no en teoria):
        // dejar un TranslateTransform "en reposo" (Y=0, tras FillBehavior.HoldEnd por omision)
        // puesto como RenderTransform del ContentPresenter reventaba el snapshot aprobado de
        // "panel-inventario" (SSIM 0,887 contra el umbral 0,970 - los otros dos, ventana entera,
        // seguian pasando de sobra, la diferencia se diluye con mas pixeles) pese a que Y=0 es
        // visualmente identico a "sin transform". Motivo real: WPF solo activa su camino rapido
        // de alineado a pixel (ClearType nitido, bordes sin desenfocar) cuando RenderTransform es
        // null/Transform.Identity - un TranslateTransform con X=Y=0 sigue contando como "hay
        // transform" para ese camino rapido y desalinea el subarbol entero del grid de subpixel,
        // emborronando iconos/texto pequeños (justo lo que un panel denso como Inventario nota
        // mas que la ventana completa). Arreglo real: RenderTransform vuelve a null en cuanto la
        // animacion llega a su fin (FillBehavior.Stop + Completed pone el valor final a mano) -
        // nunca se queda un TranslateTransform "identidad" puesto de adorno.
        void Reposar()
        {
            host.BeginAnimation(UIElement.OpacityProperty, null);
            host.Opacity = 1;
            if (host.RenderTransform is TranslateTransform t) t.BeginAnimation(TranslateTransform.YProperty, null);
            host.RenderTransform = null;
        }
        if (!SystemParameters.ClientAreaAnimation) { Reposar(); return; }

        if (host.RenderTransform is not TranslateTransform slideTransform)
        {
            slideTransform = new TranslateTransform();
            host.RenderTransform = slideTransform;
        }
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(180);
        var fade = new DoubleAnimation(0, 1, duration) { EasingFunction = ease, FillBehavior = FillBehavior.Stop };
        var slide = new DoubleAnimation(6, 0, duration) { EasingFunction = ease, FillBehavior = FillBehavior.Stop };
        // FillBehavior.Stop (en vez del HoldEnd de fabrica): el Completed de abajo deja el valor
        // final a mano Y limpia el RenderTransform (ver el comentario real de arriba) - con
        // HoldEnd la animacion se queda "viva" reteniendo el valor y nunca se puede volver a null
        // sin perder el 1/0 final en el mismo instante.
        slide.Completed += (_, _) => Reposar();
        // BeginAnimation(null) antes de relanzar: un segundo cambio de pestaña durante la
        // animacion del primero (usuario pulsando Ctrl+1..8 rapido) no debe dejar dos animaciones
        // compitiendo por el mismo valor - la nueva sustituye a la anterior desde su valor actual.
        host.BeginAnimation(UIElement.OpacityProperty, null);
        slideTransform.BeginAnimation(TranslateTransform.YProperty, null);
        host.BeginAnimation(UIElement.OpacityProperty, fade);
        slideTransform.BeginAnimation(TranslateTransform.YProperty, slide);
    }

    // El nombre "PART_SelectedContentHost" es el mismo ContentPresenter en cualquier tema real de
    // WPF (Aero2/Fluent) para TabControl - se cachea tras la primera busqueda real porque es
    // SIEMPRE la misma instancia mientras la ventana viva (el TabControl no se reconstruye al
    // cambiar de pestaña, solo cambia que Content muestra su ContentPresenter).
    private ContentPresenter? FindSelectedContentHost()
    {
        if (_rootTabContentHost != null) return _rootTabContentHost;
        _rootTabContentHost = FindDescendantByName<ContentPresenter>(RootTabControl, "PART_SelectedContentHost");
        return _rootTabContentHost;
    }

    private static T? FindDescendantByName<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match && match.Name == name) return match;
            var found = FindDescendantByName<T>(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
