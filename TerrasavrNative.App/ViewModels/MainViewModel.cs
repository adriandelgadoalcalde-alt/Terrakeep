using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Nucleo basico de la Fase 2: cargar un personaje real, verlo (inventario/banco/caja/fragua/
// boveda/mascota-montura-gancho con objetos de Calamity ya mezclados, mas equipo puesto/monedas/
// municion en su forma vanilla), guardar. Sin edicion todavia - eso es el siguiente paso.
public partial class MainViewModel : ObservableObject
{
    private readonly CharacterFileService _service = new();
    private LoadedCharacter? _loaded;

    // Auditoria de Opus, T-B (segunda auditoria, Fable): "elegir OTRO personaje en Inicio con
    // cambios sin guardar los tira sin avisar - mismo agujero real que OnWindowClosing ya
    // tapaba solo al CERRAR la ventana, no al cargar otro personaje por encima". Gancho de
    // dialogo real en vez de MessageBox aqui mismo - MainViewModel es headless de verdad (los
    // tests de TerrasavrNative.App.ViewModels.Tests lo instancian sin ninguna Window), asi que
    // el dialogo Si/No/Cancelar (mismo texto/logica real ya en MainWindow.OnWindowClosing) vive
    // en la View, que rellena este hueco en su constructor. Sin View enganchada (tests, arnes
    // de consola) se deja pasar siempre - comportamiento identico al de antes de este arreglo.
    public Func<bool>? ConfirmDiscardChanges { get; set; }

    // Confirmacion visual real de guardado (pedido explicito 2-sep-2026: "debe ser mas visual
    // que se allá confirmado el guardado no solamente un mensajito abajo a la izquierda") -
    // se activa un momento tras un Save() con exito y se apaga sola; StatusMessage se queda
    // para errores, que no deben ser tan efimeros. Ver el banner real en MainWindow.xaml.
    private readonly DispatcherTimer _saveConfirmationTimer = new() { Interval = TimeSpan.FromSeconds(1.5) };

    // H-3 (segunda auditoria de Opus, Fable): "Guardar ya funciona desde cualquier pestaña
    // (N-1) pero un error de guardado va a un TextBlock que 5 de 6 pestañas no ven" -
    // StatusMessage sigue viviendo SOLO dentro de Personaje (correcto para el detalle
    // informativo de la ultima operacion), pero un ERROR real (cargar/guardar/deshacer fallido)
    // ahora TAMBIEN sube a este canal global, visible en cualquier pestaña via un banner real en
    // MainWindow.xaml - mismo nivel que el banner de "Guardado" (N-1), nunca los exitos
    // efimeros, solo lo que de verdad requiere que el usuario se entere este donde este.
    [ObservableProperty] private string? _globalErrorMessage;

    [RelayCommand] private void DismissGlobalError() => GlobalErrorMessage = null;

    // Auditoria de Opus, Bloque 6 (N-5): antes 8 "const int...TabIndex" sueltos - ya tenian
    // nombre real (no eran literales sin explicar en medio del codigo), pero seguian siendo un
    // int cualquiera: nada impedia asignar `SelectedTabIndex = 99` sin que el compilador se
    // quejara, ni el IDE ofrecia autocompletado de "que valores son validos aqui". Un enum real
    // (mismo orden real que las pestañas del XAML, valor explicito para que reordenar el XAML
    // algun dia no desincronice esto en silencio) - el binding de WPF sigue siendo a un int
    // (`SelectedTabIndex`/`PersonajeInnerTabIndex`, TabControl.SelectedIndex no admite otra
    // cosa), el cast a `(int)AppTab.X` vive SOLO en el punto de asignacion.
    private enum AppTab { Inicio = 0, Personaje = 1, Builds = 2, Novedades = 3, Exploracion = 4, AcercaDe = 5 }

    // Indice de la pestaña INTERNA dentro de Personaje - la Libreria vive ahora DENTRO de la
    // propia pestaña Objetos, siempre visible debajo del inventario (pedido explicito
    // 1-sep-2026, "la libreria deberia estar tambien dentro de personaje... como es terrasav" -
    // y ademas necesario para que arrastrar una tarjeta hasta un slot sea posible: si Libreria
    // fuera una pestaña aparte, nunca se verian los dos a la vez). Buffs - usado por
    // RequestPickForBuffSlot para saltar a la pestaña correcta al "Elegir..." un buff.
    // D-e (segunda auditoria de Opus, Fable): se completan los 5 valores que faltaban (ya
    // usados como literal a secas en el arnes UIA) - Desbloqueos hace falta con nombre real
    // para OnPersonajeInnerTabIndexChanged, aqui abajo.
    private enum PersonajeInnerTab { Objetos = 0, Buffs = 1, Investigacion = 2, Apariencia = 3, SpawnPoints = 4, Desbloqueos = 5, Version = 6 }

    [ObservableProperty] private string _statusMessage = "Sin personaje cargado.";
    [ObservableProperty] private string? _characterName;
    [ObservableProperty] private bool _isCharacterLoaded;
    [ObservableProperty] private bool _hasCalamityData;
    // Bloque 0 de la auditoria de Opus (N-2): "se pueden editar 40 slots, cambiar de pestaña,
    // cerrar la app y perderlo todo sin un solo aviso". Se marca sola al detectar CUALQUIER
    // cambio real en un slot/Apariencia/Spawn Points/Desbloqueos/Version tras cargar - ver el
    // enganche real en el constructor, RebuildContainers y AddContainer. _suppressDirty evita
    // que el propio proceso de CARGAR (que tambien dispara PropertyChanged al rellenar campos)
    // se marque a si mismo como "cambio sin guardar".
    [ObservableProperty] private bool _isDirty;
    private bool _suppressDirty = true;

    private void MarkDirty()
    {
        if (!_suppressDirty) IsDirty = true;
    }

    // Auditoria de Opus, Bloque 3 (T-14): unico sitio real donde se decide "esto es una edicion
    // de verdad de un slot" - antes esta suscripcion vivia duplicada en RebuildContainers y
    // AddContainer solo para MarkDirty(); ahora tambien dispara el flash visual del propio
    // slot, y SOLO cuando de verdad es un usuario editando (no durante la carga silenciosa de
    // un personaje, mismo guardia _suppressDirty ya real de N-2 - JustEdited se excluye igual
    // que IsSelected, si no, el propio flash re-entraria el manejador sin fin).
    private void HookSlotEditing(ItemSlotViewModel slot)
    {
        slot.PropertyChanged += (_, e) =>
        {
            // H3-03 (tercera auditoria, Fable): RejectionMessage es puro estado de UI - se
            // escribe precisamente cuando NO se cambio ningun dato real (colocacion rechazada
            // por restriccion de slot, o al limpiar el aviso al cambiar de seleccion, L-e).
            // Sin excluirla aqui, un intento de colocar un objeto invalido marcaba el personaje
            // como "sin guardar" (sin nada real que guardar) Y disparaba el flash de "acabo de
            // editarme" - la señal contraria de lo que paso de verdad.
            if (e.PropertyName is nameof(ItemSlotViewModel.IsSelected) or nameof(ItemSlotViewModel.JustEdited) or nameof(ItemSlotViewModel.RejectionMessage)) return;
            MarkDirty();
            if (!_suppressDirty) slot.TriggerEditFlash();
        };
    }

    // Titulo real de ventana con el nombre del personaje y el punto "sin guardar" (N-2) - antes
    // era la constante fija "Terrakeep" siempre, sin importar que hubiera cargado ni si habia
    // cambios pendientes.
    public string WindowTitle => CharacterName == null
        ? "Terrakeep"
        : $"Terrakeep - {CharacterName}{(IsDirty ? " ●" : "")}";

    partial void OnCharacterNameChanged(string? value)
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(NameFileMismatch));
        // H-1 (segunda auditoria de Opus, Fable): "PlrCharacter.Name se lee y se escribe sin
        // problema... y muestra el nombre como texto muerto" - el TextBox real de la cabecera
        // (MainWindow.xaml) ahora escribe aqui de verdad. MarkDirty ya respeta _suppressDirty
        // durante la propia carga (mismo guardia real de N-2), asi que cargar un personaje no se
        // marca a si mismo como "cambio sin guardar" solo por rellenar este campo.
        if (_loaded != null && value != null) _loaded.Character.Name = value;
        MarkDirty();
    }

    // H-2 (segunda auditoria de Opus, Fable): "Sin rastro de la version ni del archivo abierto -
    // hay que ir a la pestaña Version (tercer nivel) para saberlo, y no hay forma de distinguir
    // dos personajes con el mismo nombre en carpetas distintas". Una linea real bajo el nombre,
    // en la cabecera global (visible en cualquier pestaña) con el archivo + la version resuelta
    // a su etiqueta real (ej. "1.4.4.0") cuando se conoce, o el numero crudo si no.
    public string? FileVersionLine => _loaded == null ? null
        : $"{Path.GetFileName(_loaded.PlrPath)} · versión {
            VersionEditor.Groups.SelectMany(g => g.Options).FirstOrDefault(o => o.Number == VersionEditor.RawVersion)?.Label
            ?? VersionEditor.RawVersion.ToString()}";

    // H-1/F2 (segunda auditoria de Opus, Fable): "Aviso discreto si el nombre del archivo no
    // coincide" - un personaje renombrado a mano en el juego, o un .plr copiado/renombrado por
    // fuera, puede tener un nombre real distinto del nombre del fichero (ej. "Personaje (2).plr"
    // con nombre real "Personaje") - confusion real al distinguir dos ventanas del Explorador.
    public bool NameFileMismatch => _loaded != null && CharacterName != null
        && !string.Equals(Path.GetFileNameWithoutExtension(_loaded.PlrPath), CharacterName, StringComparison.Ordinal);
    partial void OnIsDirtyChanged(bool value) => OnPropertyChanged(nameof(WindowTitle));
    [ObservableProperty] private int _selectedTabIndex;
    // Bd-d (segunda auditoria de Opus, Fable): recalcula "lo que ya se posee" al ENTRAR en la
    // pestaña Builds - momento real en que el dato importa, sin recalcular en cada tecla de una
    // edicion en Objetos (ver el comentario completo en BuildsViewModel.RefreshOwnership).
    partial void OnSelectedTabIndexChanged(int value)
    {
        if (value == (int)AppTab.Builds && EquipmentGroup != null)
            Builds.RefreshOwnership([.. Containers, .. EquipmentGroup.AllContainers]);
        // X-g (segunda auditoria de Opus, Fable): "el mapa no sabe nada del personaje real" -
        // mismo criterio que Bd-d de arriba, recalculado al ENTRAR en Exploracion (por si se
        // edito algo en Spawn Points desde la ultima vez).
        if (value == (int)AppTab.Exploracion)
            Exploration.SetCharacterSpawns(BuildCharacterSpawns());
    }

    // X-g: puntos de aparicion reales del personaje cargado - cada Spawn Point guardado
    // (Servers.Entries, PlrServerEntry.SpawnX/Y) que tenga coordenadas reales puestas (0,0 =
    // todavia sin fijar, no se muestra un marcador de adorno en la esquina para un spawn point
    // recien añadido y vacio). El .plr NO guarda ninguna "aparicion principal" propia - la
    // ultima cama real donde durmio el personaje es un dato del MUNDO (.wld), no del
    // personaje, confirmado en PlrCharacter.cs (sin ningun campo Spawn fuera de
    // PlrServerEntry) - Spawn Points es la unica fuente real de coordenadas de aparicion aqui.
    private IEnumerable<(string Label, int X, int Y)> BuildCharacterSpawns()
    {
        if (_loaded == null) yield break;
        foreach (var entry in Servers.Entries)
            if (entry.SpawnX != 0 || entry.SpawnY != 0)
                yield return (entry.Name, entry.SpawnX, entry.SpawnY);
    }

    // S-c (segunda auditoria de Opus, Fable): "sin enlace al mapa de Exploracion desde Spawn
    // Points" - "Ver en el mapa" real por fila, salta a Exploracion y centra el mapa en esas
    // coordenadas exactas (mismo mecanismo real ya usado por los NPCs, ver
    // ExplorationViewModel.NavigateToTile). Si todavia no hay ningun mundo cargado, avisa en
    // vez de saltar a un mapa en blanco sin explicar nada.
    [RelayCommand]
    private void ViewSpawnOnMap(ServerEntryRowViewModel? row)
    {
        if (row == null) return;
        SelectedTabIndex = (int)AppTab.Exploracion;
        if (!Exploration.IsWorldLoaded)
        {
            // Exploration.StatusMessage (no el StatusMessage global) - es el que se ve de
            // verdad en la pestaña a la que se acaba de saltar, ver el TextBlock real en
            // MainWindow.xaml bajo el mapa.
            Exploration.StatusMessage = "Carga un mundo (.wld) para ver este punto en el mapa.";
            return;
        }
        Exploration.NavigateToTile(row.SpawnX, row.SpawnY);
    }
    [ObservableProperty] private int _personajeInnerTabIndex;
    // D-e (segunda auditoria de Opus, Fable): recalcula el aviso real de version al ENTRAR en
    // Desbloqueos - mismo criterio ya establecido (Bd-d/X-g) para no recalcular en cada tecla
    // de una edicion en la pestaña Version.
    partial void OnPersonajeInnerTabIndexChanged(int value)
    {
        if (value == (int)PersonajeInnerTab.Desbloqueos) Flags.RefreshVersionWarning();
    }
    [ObservableProperty] private bool _saveConfirmationVisible;

    // Auditoria de Opus, Bloque 4 (T-2): breakpoint real y compartido, ver WindowSizeClass.cs.
    // AmplioMinWidth=1500 medido de verdad con el arnes de UI Automation, confirmado por DOS
    // consumidores reales independientes que necesitaron el mismo umbral: E-2 (las 3 vistas de
    // Equipamiento lado a lado - limpio a 1650px, recorta la 3ª columna a 1450px) y A-4
    // (Inventario+Almacen lado a lado, dos rejillas de 10 columnas - limpio a 1500px/1650px,
    // recorta la ultima columna a 1350px). NormalMinWidth=1300 se deja como umbral intermedio
    // real (Compacto/Amplio con margen entre medias) - sin consumidor propio todavia, pero
    // SizeClass en si ya es un valor correcto y usable en cuanto alguna pantalla nueva lo
    // necesite.
    [ObservableProperty] private WindowSizeClass _sizeClass = WindowSizeClass.Normal;
    private const double NormalMinWidth = 1300;
    private const double AmplioMinWidth = 1500;

    public void UpdateSizeClass(double actualWidth)
    {
        SizeClass = actualWidth >= AmplioMinWidth ? WindowSizeClass.Amplio
            : actualWidth >= NormalMinWidth ? WindowSizeClass.Normal
            : WindowSizeClass.Compacto;
    }

    // Auditoria de Opus, E-2: umbral real medido con el arnes (ver bitacora.md) - a partir de
    // aqui hay sitio de sobra para las 3 vistas de Equipamiento (Armadura/Vanidad/Tintes) a la
    // vez, sin apretar ninguna. Por debajo, se queda el selector de pildoras de siempre (un
    // panel a la vez).
    public bool IsEquipmentExpanded => SizeClass == WindowSizeClass.Amplio;

    // Auditoria de Opus, A-4: "Inventario y Almacenes viven en pestañas separadas - nunca se
    // pueden ver a la vez, y por eso arrastrar un objeto del uno al otro es literalmente
    // imposible" (el drop-target del otro contenedor ni siquiera existe en el arbol visual
    // mientras esa pestaña no esta activa). Con sitio real, la pestaña "Inventario" pasa a
    // mostrar Inventario + el Almacen seleccionado lado a lado - el intercambio entre slots YA
    // es generico de por si (ItemSlotViewModel.SwapWith, MainWindow.xaml.cs OnItemSlotDrop no
    // distingue de que contenedor viene cada slot), asi que arrastrar de verdad entre los dos
    // funciona en cuanto ambos coexisten en el arbol visual, sin tocar ese codigo.
    //
    // Umbral medido de verdad (no el intermedio "Normal" original, que resulto demasiado
    // agresivo para esto): dos rejillas REALES de 10 columnas cada una necesitan mas sitio del
    // que "Normal" (1300) da - a 1350px la columna 10 de cada rejilla queda recortada contra el
    // borde (confirmado con captura real). A 1500px (= AmplioMinWidth, el mismo umbral real que
    // ya mide E-2 para sus 3 vistas) ambas rejillas se ven limpias y completas - se reutiliza
    // el mismo umbral compartido en vez de inventar uno propio (ese es justo el punto de T-2:
    // un unico breakpoint real, no uno por pantalla).
    public bool IsStorageExpanded => SizeClass == WindowSizeClass.Amplio;

    // I-c (segunda auditoria de Opus, Fable): "anchos fijos en una pantalla que es puro
    // WrapPanel - en una ventana de 1920px, Inicio usa 880px y deja 1.000px negros". Mismo
    // SizeClass real compartido en vez de un umbral propio - en Amplio, sitio de sobra para que
    // el WrapPanel de tarjetas reparta una fila mas ancha en vez de quedarse angosto.
    public double InicioContentMaxWidth => SizeClass == WindowSizeClass.Amplio ? 1400 : 880;

    partial void OnSizeClassChanged(WindowSizeClass value)
    {
        OnPropertyChanged(nameof(IsEquipmentExpanded));
        OnPropertyChanged(nameof(IsStorageExpanded));
        OnPropertyChanged(nameof(InicioContentMaxWidth));
    }

    public ObservableCollection<ContainerViewModel> Containers { get; } = [];
    [ObservableProperty] private EquipmentGroupViewModel? _equipmentGroup;
    [ObservableProperty] private StorageGroupViewModel? _storageGroup;
    [ObservableProperty] private ContainerViewModel? _inventoryContainer;
    [ObservableProperty] private ContainerViewModel? _mountsContainer;
    [ObservableProperty] private ContainerViewModel? _dyesContainer;
    [ObservableProperty] private ContainerViewModel? _coinsContainer;
    [ObservableProperty] private ContainerViewModel? _ammoContainer;

    // Pregunta a Opus sobre el diseño (2-sep-2026): de los ~589px utiles de la pestaña
    // "Objetos", 270 vivian congelados en la fila de la Libreria (46% del alto) - mas robo de
    // espacio que las propias 9 pestañas. Plegada por defecto (con auto-despliegue al elegir
    // objeto, ver RequestPickForSlot) devuelve ese espacio a la cuadricula de items, que es la
    // prioridad explicita del usuario ("me gusta mucho que los objetos se vean directamente de
    // un plumazo").
    [ObservableProperty] private bool _isLibraryCollapsed = true;
    // Mismo criterio que IsLibraryCollapsed de arriba, para la Libreria de buffs (Fase 2 del
    // rework de Buffs, pregunta a Opus sobre el diseño 2-sep-2026, cuarta pasada) - plegada por
    // defecto, auto-despliegue al "Elegir..." un buff (RequestPickForBuffSlot).
    [ObservableProperty] private bool _isBuffLibraryCollapsed = true;

    // Segunda auditoria de Opus (Fable), B-2: BUG REAL - "Elegir objeto..."/"Elegir buff..."
    // ponian IsLibraryCollapsed=false DIRECTAMENTE y nada lo volvia a poner en true jamas (ni
    // PlaceInTarget/CancelPick, ni cambiar de personaje) - el PRIMER "Elegir..." de la sesion
    // dejaba la Libreria desplegada para siempre, sin que el usuario hubiera tocado el boton -
    // un pestillo de un solo sentido. IsLibraryCollapsed vuelve a ser SOLO la preferencia real
    // del usuario (la que cambia el boton, y solo el boton) - la visibilidad real es esta
    // propiedad derivada, que combina esa preferencia con un despliegue TEMPORAL mientras se
    // esta eligiendo, sin pisarla. Al terminar/cancelar el pick, vuelve sola a la preferencia
    // real - el boton nunca miente sobre lo que hay en pantalla. Recuperado de 83fd33c (ver
    // BoolToGridLengthConverter), que un revert por rango demasiado ancho se llevo por delante.
    public bool IsLibraryVisible => !IsLibraryCollapsed || Library.IsPicking;
    public bool IsBuffLibraryVisible => !IsBuffLibraryCollapsed || BuffLibrary.IsPicking;
    partial void OnIsLibraryCollapsedChanged(bool value) => OnPropertyChanged(nameof(IsLibraryVisible));
    partial void OnIsBuffLibraryCollapsedChanged(bool value) => OnPropertyChanged(nameof(IsBuffLibraryVisible));
    public ResearchViewModel Research { get; }
    public BuildsViewModel Builds { get; }
    public WhatsNewViewModel WhatsNew { get; }
    public ChangelogViewModel Changelog { get; }
    public AboutViewModel About { get; } = new();
    public ExplorationViewModel Exploration { get; }
    public LibraryViewModel Library { get; }
    public AppearanceViewModel Appearance { get; }
    public ServersViewModel Servers { get; } = new();
    public FlagsViewModel Flags { get; } = new();
    public VersionEditorViewModel VersionEditor { get; } = new();
    public BuffsViewModel Buffs { get; }
    public BuffLibraryViewModel BuffLibrary { get; }
    public BuffEditViewModel BuffEdit { get; }
    public ItemEditViewModel ItemEdit { get; }
    public HomeViewModel Home { get; } = new();

    public MainViewModel()
    {
        // Auditoria de Opus, I-1: elegir un personaje real en el lanzador de Inicio carga
        // exactamente igual que el dialogo de "Cargar personaje..." de siempre, y salta
        // directo a Personaje - de nada sirve un lanzador de un click si despues hay que ir a
        // buscar la pestaña a mano (P1).
        Home.CharacterChosen += path =>
        {
            if (IsDirty && ConfirmDiscardChanges?.Invoke() == false) return;
            LoadFromPath(path);
            SelectedTabIndex = (int)AppTab.Personaje;
            PersonajeInnerTabIndex = (int)PersonajeInnerTab.Objetos;
        };
        Builds = new BuildsViewModel(_service.VanillaBuilds, _service.CalamityBuilds, _service);
        WhatsNew = new WhatsNewViewModel(_service.WhatsNew, _service.VanillaCatalog);
        Changelog = new ChangelogViewModel(_service.Changelog);
        Exploration = new ExplorationViewModel(_service);
        Library = new LibraryViewModel(_service);
        Research = new ResearchViewModel(_service);
        Appearance = new AppearanceViewModel(_service);
        Library.ItemPlaced += () =>
        {
            SelectedTabIndex = (int)AppTab.Personaje;
            PersonajeInnerTabIndex = (int)PersonajeInnerTab.Objetos;
        };
        // IsLibraryVisible depende de Library.IsPicking (ver su comentario) - Library ya
        // notifica ese cambio (OnPickTargetChanged), asi que solo hace falta reenviarlo.
        Library.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LibraryViewModel.IsPicking)) OnPropertyChanged(nameof(IsLibraryVisible));
        };
        BuffEdit = new BuffEditViewModel(_service);
        BuffLibrary = new BuffLibraryViewModel(_service);
        BuffLibrary.BuffPlaced += () =>
        {
            SelectedTabIndex = (int)AppTab.Personaje;
            PersonajeInnerTabIndex = (int)PersonajeInnerTab.Buffs;
        };
        BuffLibrary.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BuffLibraryViewModel.IsPicking)) OnPropertyChanged(nameof(IsBuffLibraryVisible));
        };
        Buffs = new BuffsViewModel(_service, RequestPickForBuffSlot);
        // Buffs es una unica instancia persistente que reconstruye sus slots en cada
        // LoadFrom() real (a diferencia de los contenedores de objetos, que MainViewModel crea
        // el mismo directamente en AddContainer) - SlotChanged reenvia el cambio de cualquier
        // slot de buff, cargado el personaje que sea, sin tener que resuscribirse cada vez.
        Buffs.SlotChanged += MarkDirty;
        ItemEdit = new ItemEditViewModel(_service);
        // Apariencia/Spawn Points/Desbloqueos/Version son tambien instancias persistentes -
        // cualquier propiedad que cambien tras cargar un personaje es una edicion real.
        Appearance.PropertyChanged += (_, _) => MarkDirty();
        Servers.Changed += MarkDirty; // B-5 (segunda auditoria de Opus): evento real, ver ServersViewModel.Changed
        Flags.PropertyChanged += (_, _) => MarkDirty();
        VersionEditor.PropertyChanged += (_, _) =>
        {
            MarkDirty();
            OnPropertyChanged(nameof(FileVersionLine)); // H-2: cambiar la version en la pestaña Version debe reflejarse tambien en la cabecera
        };
        _saveConfirmationTimer.Tick += (_, _) =>
        {
            SaveConfirmationVisible = false;
            _saveConfirmationTimer.Stop();
        };
    }

    // Selecciona un slot para el panel "Editar" compartido (equivalente real de app.TabEdit)
    // - un unico slot seleccionado a la vez, sea cual sea el contenedor donde este (pedido
    // explicito 1-sep-2026: el panel debe "acompañar" a cualquier pestaña de objetos).
    public void SelectSlot(ItemSlotViewModel slot)
    {
        if (ItemEdit.Slot != null) ItemEdit.Slot.IsSelected = false;
        slot.IsSelected = true;
        ItemEdit.Slot = slot;
    }

    // Mismo patron que SelectSlot de arriba, para el panel "Editar buff seleccionado" (pregunta
    // a Opus sobre el diseño 2-sep-2026, cuarta pasada) - un unico buff seleccionado a la vez.
    public void SelectBuffSlot(BuffSlotViewModel slot)
    {
        if (BuffEdit.Slot != null) BuffEdit.Slot.IsSelected = false;
        slot.IsSelected = true;
        BuffEdit.Slot = slot;
    }

    // Usado por las tarjetas de la pagina de Inicio para saltar directamente a una seccion.
    // "Libreria" ya no es una pestaña propia (ver el comentario de PersonajeInnerTab) - vive
    // dentro de Objetos, asi que la tarjeta de Inicio salta ahi igual que "Personaje".
    [RelayCommand]
    private void GoToTab(string tab)
    {
        if (tab is "Personaje" or "Libreria")
        {
            SelectedTabIndex = (int)AppTab.Personaje;
            PersonajeInnerTabIndex = (int)PersonajeInnerTab.Objetos;
            return;
        }
        SelectedTabIndex = tab switch
        {
            "Builds" => (int)AppTab.Builds,
            "Novedades" => (int)AppTab.Novedades,
            "Exploracion" => (int)AppTab.Exploracion,
            "AcercaDe" => (int)AppTab.AcercaDe,
            _ => (int)AppTab.Inicio,
        };
    }

    private void RequestPickForSlot(ItemSlotViewModel slot)
    {
        Library.PickTarget = slot;
        SelectSlot(slot); // el panel Editar sigue al slot que se esta rellenando desde la Libreria
        SelectedTabIndex = (int)AppTab.Personaje;
        PersonajeInnerTabIndex = (int)PersonajeInnerTab.Objetos;
        // IsLibraryCollapsed YA NO se toca aqui (segunda auditoria, B-2) - Library.IsPicking
        // (puesto arriba al fijar PickTarget) ya revela la Libreria via IsLibraryVisible, sin
        // pisar la preferencia real del usuario.
    }

    [RelayCommand]
    private void ToggleLibraryCollapsed() => IsLibraryCollapsed = !IsLibraryCollapsed;

    // Gemelo de RequestPickForSlot de arriba, para buffs (Fase 2 del rework, pregunta a Opus
    // sobre el diseño 2-sep-2026, cuarta pasada).
    private void RequestPickForBuffSlot(BuffSlotViewModel slot)
    {
        BuffLibrary.PickTarget = slot;
        SelectBuffSlot(slot);
        SelectedTabIndex = (int)AppTab.Personaje;
        PersonajeInnerTabIndex = (int)PersonajeInnerTab.Buffs;
        // IsBuffLibraryCollapsed YA NO se toca aqui, mismo motivo que RequestPickForSlot.
    }

    [RelayCommand]
    private void ToggleBuffLibraryCollapsed() => IsBuffLibraryCollapsed = !IsBuffLibraryCollapsed;

    public void LoadFromPath(string plrPath)
    {
        // Auditoria de Opus, N-2: mientras se carga, cada LoadFrom() de abajo dispara sus
        // propios PropertyChanged reales (rellenar campos) - eso NO es una edicion del
        // usuario, asi que se suprime aqui y se reactiva solo cuando la carga entera termino
        // bien (o se corta del todo si fallo, ver el catch).
        _suppressDirty = true;
        try
        {
            Library.PickTarget = null;
            BuffLibrary.PickTarget = null;
            ItemEdit.Slot = null;
            BuffEdit.Slot = null;
            _loaded = _service.Load(plrPath);
            RebuildContainers();
            Appearance.LoadFrom(_loaded.Character);
            Servers.LoadFrom(_loaded.Character);
            Flags.LoadFrom(_loaded.Character);
            VersionEditor.LoadFrom(_loaded.Character);
            Buffs.LoadFrom(_loaded.Character);
            BuffEdit.SetCharacterVersion(_loaded.Character.Version);
            CharacterName = _loaded.Character.Name;
            HasCalamityData = _loaded.TplrPath != null;
            IsCharacterLoaded = true;
            int calamityCount = _loaded.MergedContainers.Values.Sum(items => items.Count(i => i.IsCalamity));
            StatusMessage = HasCalamityData
                ? $"Cargado '{_loaded.Character.Name}' - {calamityCount} objeto(s) de Calamity detectado(s)."
                : $"Cargado '{_loaded.Character.Name}' - personaje 100% vanilla (sin .tplr).";
            GlobalErrorMessage = null; // H-3: una carga con exito limpia cualquier error global anterior
        }
        catch (Exception ex)
        {
            // Auditoria de Opus, T-22: "LoadFromPath traga excepciones y sigue... queda en un
            // estado a medias". Si el fallo ocurrio DESPUES de _service.Load (ej. en
            // RebuildContainers o en algun LoadFrom), _loaded ya apunta al personaje nuevo
            // pero los ViewModels solo se rellenaron a medias - Guardar/AutoEquip/Investigar
            // todo actuarian sobre ese estado inconsistente. _loaded=null cierra el hueco real
            // (los 3 comandos ya comprueban _loaded==null antes de hacer nada).
            _loaded = null;
            IsCharacterLoaded = false;
            StatusMessage = $"Error al cargar: {ex.Message}";
            GlobalErrorMessage = StatusMessage; // H-3: visible en cualquier pestaña, no solo Personaje
        }
        finally
        {
            _suppressDirty = false;
            IsDirty = false;
            UndoLastSaveCommand.NotifyCanExecuteChanged(); // el personaje cargado (y su .bak) ha cambiado
            OnPropertyChanged(nameof(FileVersionLine)); // H-2: archivo/version cambian con cada carga (o desaparecen si fallo)
            Home.UpdateCurrentPath(_loaded?.PlrPath); // I-a: la tarjeta real de Inicio debe reflejar cual esta cargado ahora
            // X-g: cubre cargar un personaje distinto mientras Exploracion ya esta a la vista -
            // en el finally (no en RebuildContainers) porque Servers.LoadFrom (arriba) tiene que
            // haber corrido ya para leer sus Spawn Points reales, no los del personaje anterior.
            Exploration.SetCharacterSpawns(BuildCharacterSpawns());
        }
    }

    [RelayCommand(CanExecute = nameof(IsCharacterLoaded))]
    private void Save()
    {
        if (_loaded == null) return;
        try
        {
            SyncEditsBackToMerged();
            _service.Save(_loaded);
            StatusMessage = $"Guardado: {Path.GetFileName(_loaded.PlrPath)}" +
                (_loaded.TplrPath != null ? $" + {Path.GetFileName(_loaded.TplrPath)}" : "");
            IsDirty = false;
            SaveConfirmationVisible = true;
            _saveConfirmationTimer.Stop();
            _saveConfirmationTimer.Start();
            GlobalErrorMessage = null; // H-3: un guardado con exito limpia cualquier error global anterior
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al guardar: {ex.Message}";
            GlobalErrorMessage = StatusMessage; // H-3: visible en cualquier pestaña, no solo Personaje
        }
        finally
        {
            UndoLastSaveCommand.NotifyCanExecuteChanged(); // Save() acaba de crear/renovar el .bak real
        }
    }

    // T-C (segunda auditoria de Opus, Fable): "el .bak existe de verdad desde el Bloque 0 pero
    // no hay ninguna forma real de usarlo desde la UI - un usuario que se de cuenta tarde de que
    // ha guardado algo que no queria no tiene mas remedio que ir a buscarlo a mano en el
    // Explorador de archivos". Restaura el/los .bak reales que CharacterFileService.Save ya deja
    // (WriteAtomic) y recarga - mismo mecanismo real que "Cargar personaje...", no uno nuevo.
    // H3-01 (tercera auditoria, Fable): "Deshacer" tiraba ediciones sin guardar sin preguntar -
    // T-B cerro este mismo agujero en los otros 3 puntos de entrada reales (Inicio, "Cargar
    // personaje...", Ctrl+O), pero "Deshacer ultimo guardado" (un clic, siempre visible en la
    // cabecera global) se quedo fuera. Mismo gancho real que los otros 3 - ConfirmDiscardChanges
    // solo cuando de verdad hay algo que perder (IsDirty).
    [RelayCommand(CanExecute = nameof(CanUndoLastSave))]
    private void UndoLastSave()
    {
        if (_loaded == null) return;
        if (IsDirty && ConfirmDiscardChanges?.Invoke() == false) return;
        string plrPath = _loaded.PlrPath;
        string plrBak = plrPath + ".bak";
        if (!File.Exists(plrBak)) return;
        try
        {
            File.Copy(plrBak, plrPath, overwrite: true);
            string tplrPath = _loaded.TplrPath ?? Path.ChangeExtension(plrPath, ".tplr");
            string tplrBak = tplrPath + ".bak";
            if (File.Exists(tplrBak))
            {
                File.Copy(tplrBak, tplrPath, overwrite: true);
            }
            else if (File.Exists(tplrPath))
            {
                // H3-02 (tercera auditoria, Fable): WriteAtomic solo genera un .bak real cuando
                // el fichero YA EXISTIA (File.Replace) - si no hay .tplr.bak pero SI hay .tplr,
                // este .tplr nacio en el MISMO guardado que se esta deshaciendo (no habia
                // ninguno antes). Dejarlo intacto resucitaria su contenido de Calamity al
                // recargar (MergeAll lo fusiona igual), aunque el .plr ya se haya revertido -
                // "Deshecho" seria un mensaje falso. Se borra para volver de verdad al estado
                // real de antes de ese guardado (sin ningun .tplr).
                File.Delete(tplrPath);
            }
            string nombreAntesDeRecargar = CharacterName ?? plrPath;
            LoadFromPath(plrPath);
            // LoadFromPath NUNCA relanza (traga sus propias excepciones, T-22) - si la recarga
            // en si fallo, ya dejo su propio StatusMessage/GlobalErrorMessage de error reales;
            // pisarlo aqui con un mensaje de exito falso seria peor que no decir nada.
            if (IsCharacterLoaded) StatusMessage = $"Deshecho el último guardado de '{nombreAntesDeRecargar}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al deshacer el último guardado: {ex.Message}";
            GlobalErrorMessage = StatusMessage; // H-3: visible en cualquier pestaña, no solo Personaje
        }
    }

    private bool CanUndoLastSave() => _loaded != null && File.Exists(_loaded.PlrPath + ".bak");

    private void RebuildContainers()
    {
        Containers.Clear();
        EquipmentGroup = null;
        StorageGroup = null;
        InventoryContainer = null;
        MountsContainer = null;
        DyesContainer = null;
        CoinsContainer = null;
        AmmoContainer = null;
        Research.Reset();
        if (_loaded == null) { Builds.RefreshOwnership([]); return; }

        // Contenedores con fusion real de Calamity (mismos 7 que CalamityCharacterSync cubre).
        // Se siguen guardando TODOS en Containers (SyncEditsBackToMerged/AutoEquip los buscan
        // ahi por Key) - las referencias devueltas de mas abajo son solo para la navegacion de
        // 5 pestañas (pregunta a Opus sobre el diseño, 2-sep-2026), presentacion pura, ningun
        // dato nuevo ni fusion de colecciones.
        InventoryContainer = AddContainer("inventory", "Inventario", _loaded.MergedContainers["inventory"]);
        var bank = AddContainer("bank", "Banco", _loaded.MergedContainers["bank"]);
        var bank2 = AddContainer("bank2", "Caja fuerte", _loaded.MergedContainers["bank2"]);
        var bank3 = AddContainer("bank3", "Fragua del Defensor", _loaded.MergedContainers["bank3"]);
        var bank4 = AddContainer("bank4", "Boveda del Vacio", _loaded.MergedContainers["bank4"]);
        // columns: 1 (pregunta a Opus sobre el diseño, quinta pasada: "mascotas etc mejor en
        // vertical") - laterales de la Equipamiento fusionada, una sola columna de 5 filas.
        // Orden real de los 5 slots (Player.miscEquips, confirmado por Opus contra Player.cs
        // Y de forma independiente contra el propio script.js real de Terrasavr -
        // app.TabMiscEquips, slotLabels = ["Pet","Light pet","Minecart","Mount","Hook"], dos
        // fuentes reales coincidentes): 0=Mascota, 1=Mascota de luz, 2=Vagoneta, 3=Montura,
        // 4=Gancho. Restriccion de tipo (petición explícita 2-sep-2026: "solo deberian poderse
        // equipar sus respectivos items") + icono fantasma real por indice.
        SlotKind[] miscEquipKinds = [SlotKind.VanityPet, SlotKind.LightPet, SlotKind.Cart, SlotKind.Mount, SlotKind.Hook];
        string?[] miscEquipGhosts = ["pet", "pet_light", "minecart", "mount", "hook"];
        // minCell: 32 (pedido explicito 2-sep-2026: "los cuadrados... ya que ahora sale
        // scroll y no lo queremos") - 5+5 slots apilados en una columna estrecha no siempre
        // caben a 40px minimo en la resolucion real del usuario; celdas mas pequeñas en vez
        // de aceptar el scroll (el ScrollViewer de seguridad se queda de todos modos, para
        // ventanas aun mas pequeñas). maxCell: 56 (consulta a Opus, septima pasada - bug real
        // medido con el arnes: sin techo propio, esta columna UNICA crecia sin limite hacia
        // el techo universal (90) en cuanto sobraba alto, robandole sitio real a la columna
        // central "Auto"+"*" de Armadura/Accesorios - ver ContainerViewModel.MaxCell).
        MountsContainer = AddContainer("miscEquips", "Mascota / Montura / Gancho", _loaded.MergedContainers["miscEquips"], columns: 1,
            slotKinds: miscEquipKinds, ghostIcons: miscEquipGhosts, minCell: 32, maxCell: 56);
        // Los 5 tintes van emparejados 1:1 con los 5 slots de arriba, pero un tinte SIEMPRE
        // es solo un tinte (dye>0) sea cual sea el equipo al que este emparejado - mismo
        // SlotKind.Dye y mismo ghost "dye" en los 5, a diferencia del contenedor de arriba.
        DyesContainer = AddContainer("miscDyes", "Tintes (mascota/montura/gancho)", _loaded.MergedContainers["miscDyes"], columns: 1,
            slotKinds: [SlotKind.Dye, SlotKind.Dye, SlotKind.Dye, SlotKind.Dye, SlotKind.Dye],
            ghostIcons: ["dye", "dye", "dye", "dye", "dye"], minCell: 32, maxCell: 56);

        // Monedas/municion: vanilla-only (ver CalamityCharacterSync.cs para el alcance
        // documentado - la app JS tampoco los sincroniza con Calamity, solo los protege). Sin
        // ghost real (el propio juego tampoco dibuja uno en estos dos contextos, ver
        // ItemSlot.cs real - no se inventa ninguno).
        CoinsContainer = AddContainer("coins", "Monedas", _loaded.Character.Coins.ToGameItems(),
            slotKinds: [SlotKind.Coin, SlotKind.Coin, SlotKind.Coin, SlotKind.Coin]);
        AmmoContainer = AddContainer("ammo", "Municion", _loaded.Character.Ammo.ToGameItems(),
            slotKinds: [SlotKind.Ammo, SlotKind.Ammo, SlotKind.Ammo, SlotKind.Ammo]);

        StorageGroup = new StorageGroupViewModel(bank, bank2, bank3, bank4);

        // Equipo puesto + los 3 loadouts reales seleccionables (Version>=269, si no
        // Loadouts.Length==0) - consolidados en una unica pantalla "Equipamiento" con
        // selector, ver EquipmentGroupViewModel (antes eran 12 pestañas planas mas aqui
        // mismo, 21 pestañas en total - pedido explicito 2-sep-2026 tras el amontonamiento
        // real al reducir la ventana: "¿es necesario que haya tantos botones?").
        EquipmentGroup = new EquipmentGroupViewModel(_service, RequestPickForSlot, _loaded.MergedContainers, _loaded.Character.Loadouts.Length);
        // EquipmentGroup se RECREA entera cada carga (a diferencia de Buffs, que reutiliza la
        // misma instancia) - los slots de sus contenedores nunca pasan por AddContainer, asi
        // que se enganchan aqui, el unico sitio real donde MainViewModel ve la instancia nueva.
        foreach (var c in EquipmentGroup.AllContainers)
            foreach (var s in c.Slots)
                HookSlotEditing(s);

        Research.LoadFrom(_loaded.Character);
        // Bd-d: foto fija de "lo que ya se posee" para la pestaña Builds, recalculada tambien
        // al entrar en ella (ver OnSelectedTabIndexChanged) - aqui cubre el caso real de cargar
        // un personaje distinto mientras Builds ya esta a la vista.
        Builds.RefreshOwnership([.. Containers, .. EquipmentGroup.AllContainers]);
    }

    // "Investigar todo" - regla de negocio real extraida a ResearchAllService (auditoria de
    // Opus, Bloque 6, T-20) - aqui solo queda la orquestacion (recargar Research, avisar).
    [RelayCommand(CanExecute = nameof(IsCharacterLoaded))]
    private void ResearchAll()
    {
        if (_loaded == null) return;
        ResearchAllService.Apply(_loaded, _service);
        Research.LoadFrom(_loaded.Character);
        // Segunda auditoria de Opus (Fable), B-4 - BUG REAL: ResearchAllService.Apply muta
        // _loaded.Character.Research DIRECTAMENTE, sin pasar por ningun ViewModel observable -
        // IsDirty se quedaba en false pese a escribir miles de entradas reales. El usuario leia
        // "pulsa Guardar para conservarlo", cerraba la ventana sin guardar, y OnWindowClosing no
        // preguntaba nada (IsDirty==false) - perdida silenciosa de todo el trabajo. Mismo
        // escenario que N-2 (Bloque 0) existia para cerrar.
        MarkDirty();
        StatusMessage = $"Investigación completa aplicada ({_loaded.Character.Research.Count} objetos) - pulsa Guardar para conservarlo.";
    }

    // Auto-equipar desde el panel Builds - regla de negocio real extraida a
    // AutoEquipService (auditoria de Opus, Bloque 6, T-20) - aqui solo queda la orquestacion
    // (StatusMessage, saltar a Personaje).
    [RelayCommand(CanExecute = nameof(IsCharacterLoaded))]
    private void AutoEquip(BuildClassGear? gear)
    {
        if (_loaded == null || gear == null || EquipmentGroup == null) return;

        var result = AutoEquipService.Apply(gear, EquipmentGroup, Containers.First(c => c.Key == "inventory"), _service);
        // Bd-c (segunda auditoria de Opus, Fable): "sin resolver" y "sin hueco libre" son
        // causas reales distintas (una no tiene arreglo por parte del usuario, la otra si -
        // vaciar hueco en el Inventario) - se cuentan y se dicen aparte en vez de fundirse en
        // un unico "sin resolver o sin hueco libre".
        var partes = new List<string>();
        if (result.Unresolved > 0) partes.Add($"{result.Unresolved} sin resolver");
        if (result.NoSlot > 0) partes.Add($"{result.NoSlot} sin hueco libre en el Inventario");
        string mensaje = partes.Count > 0
            ? $"Auto-equipar: {result.Placed} objeto(s) colocado(s), {string.Join(", ", partes)} - pulsa Guardar para conservarlo."
            : $"Auto-equipar: {result.Placed} objeto(s) colocado(s) - pulsa Guardar para conservarlo.";
        // Bd-f (segunda auditoria de Opus, Fable): "avisar si el personaje no tiene .tplr" -
        // colocar equipo de Calamity Mod (pid con "/", ver BuildsViewModel.ResolveItem) en un
        // personaje sin datos de Calamity conocidos (HasCalamityData, real - TplrPath!=null) no
        // falla ni se pierde (CharacterFileService.Save crea el .tplr solo con guardar, ver
        // T-C), pero si el mod NO esta realmente instalado en el juego del usuario, esos
        // objetos no se reconoceran ahi - aviso informativo, no bloqueante (esta app no tiene
        // forma real de saber si el mod esta instalado, solo si este personaje ya lo uso antes).
        bool esBuildDeCalamity = gear.Armor.Concat(gear.Weapons).Concat(gear.Accessories).Any(i => i.Pid?.Contains('/') == true);
        if (esBuildDeCalamity && !HasCalamityData)
            mensaje = "Aviso: este personaje no tiene datos de Calamity conocidos (sin .tplr) - confirma que el mod esté instalado en el juego antes de usar este equipo. " + mensaje;
        StatusMessage = mensaje;
        SelectedTabIndex = (int)AppTab.Personaje;
    }

    // A-d (segunda auditoria de Opus, Fable): "mover todo al banco" - mueve el Inventario
    // entero al Almacen que este seleccionado ahora mismo (StorageGroup.Current, el mismo
    // selector de pildoras de Banco/Caja fuerte/Fragua/Boveda ya existente) en vez de fijarlo
    // solo a "Banco" - generaliza igual de bien a los 4 almacenes sin inventar un cuarto boton
    // por cada uno.
    [RelayCommand(CanExecute = nameof(IsCharacterLoaded))]
    private void MoveInventoryToStorage()
    {
        if (InventoryContainer == null || StorageGroup == null) return;
        int moved = InventoryContainer.MoveAllTo(StorageGroup.Current);
        if (moved == 0)
        {
            StatusMessage = "Nada que mover: el inventario está vacío o el almacén seleccionado no tiene hueco libre.";
            return;
        }
        MarkDirty();
        StatusMessage = $"Movido{(moved == 1 ? "" : "s")} {moved} objeto(s) al almacén seleccionado - pulsa Guardar para conservarlo.";
    }

    // Los primeros 10 slots reales de "inventory" son la barra rapida (Player.inventory[0..9]
    // en el propio Terraria - confirmado en Player.cs decompilado, "Hotbar1".."Hotbar0" son 10
    // triggers reales) - contorno verde de "equipado" tambien ahi, igual que en Equipamiento
    // (pedido explicito 2-sep-2026, confirmado con el usuario tras preguntar: el .plr no
    // guarda ningun campo real de "arma empuñada ahora mismo" fiable, asi que la barra rapida
    // entera es el criterio, no un unico slot).
    private const int HotbarSlotCount = 10;

    // columns (pregunta a Opus sobre el diseño, quinta pasada - fusion de Equipamiento con
    // Monturas/Monedas como laterales): default 10 para los contenedores de siempre; los
    // laterales que se quieren verticales (Mascota/Montura/Gancho, Tintes) pasan columns: 1.
    // slotKinds/ghostIcons (pedido explicito 2-sep-2026: "los slots de tintes, gancho,
    // vagoneta, montura y mascota solo deberian poderse equipar sus respectivos items" +
    // iconos fantasma reales) - null = sin restriccion/sin ghost en todos los slots (el caso
    // de siempre: Inventario/Banco/Caja fuerte/Fragua/Boveda), o un array del mismo tamaño
    // que items para fijarlo por indice (miscEquips/miscDyes/coins/ammo).
    private ContainerViewModel AddContainer(string key, string displayName, GameItem[] items, int columns = 10,
        SlotKind[]? slotKinds = null, string?[]? ghostIcons = null, double minCell = 40, double maxCell = 90)
    {
        var slots = new ObservableCollection<ItemSlotViewModel>();
        for (int i = 0; i < items.Length; i++)
        {
            bool isEquipped = key == "inventory" && i < HotbarSlotCount;
            var kind = slotKinds != null && i < slotKinds.Length ? slotKinds[i] : SlotKind.None;
            var ghost = ghostIcons != null && i < ghostIcons.Length ? ghostIcons[i] : null;
            var slot = new ItemSlotViewModel(_service, i, displayName, items[i], RequestPickForSlot, isEquipped, kind, ghost);
            HookSlotEditing(slot);
            slots.Add(slot);
        }
        var container = new ContainerViewModel(key, displayName, slots) { Columns = columns, MinCell = minCell, MaxCell = maxCell };
        Containers.Add(container);
        return container;
    }

    // Vuelca lo que haya en las colecciones enlazadas a la UI de vuelta a MergedContainers
    // antes de guardar.
    //
    // Bug real encontrado y arreglado (fase 4 del rework, 1-sep-2026): "coins"/"ammo" NUNCA
    // estan en MergedContainers (CalamityCharacterSync.MergeAll no los toca, son vanilla-only
    // a proposito - ver el comentario de RebuildContainers) - el "continue" de abajo los
    // saltaba en silencio, asi que CUALQUIER edicion de Monedas/Municion se perdia al
    // guardar sin ningun aviso. Se escriben aparte, directo a PlrCharacter.Coins/Ammo.
    private void SyncEditsBackToMerged()
    {
        if (_loaded == null) return;
        SyncContainersBackToMerged(Containers);
        if (EquipmentGroup != null) SyncContainersBackToMerged(EquipmentGroup.AllContainers);
    }

    private void SyncContainersBackToMerged(IEnumerable<ContainerViewModel> containers)
    {
        if (_loaded == null) return;
        foreach (var container in containers)
        {
            if (container.Key == "coins") { CopySlotsInto(_loaded.Character.Coins, container.Slots); continue; }
            if (container.Key == "ammo") { CopySlotsInto(_loaded.Character.Ammo, container.Slots); continue; }
            if (!_loaded.MergedContainers.TryGetValue(container.Key, out var arr)) continue;
            for (int i = 0; i < container.Slots.Count && i < arr.Length; i++)
                arr[i] = container.Slots[i].Item;
        }
    }

    private static void CopySlotsInto(PlrItemSlot[] target, IReadOnlyList<ItemSlotViewModel> source)
    {
        var items = source.Select(s => s.Item).ToArray().ToPlrItemSlots();
        for (int i = 0; i < target.Length && i < items.Length; i++) target[i] = items[i];
    }

    partial void OnIsCharacterLoadedChanged(bool value)
    {
        SaveCommand.NotifyCanExecuteChanged();
        ResearchAllCommand.NotifyCanExecuteChanged();
        AutoEquipCommand.NotifyCanExecuteChanged();
        // A-d: se olvido aqui al añadirlo - sin este aviso el boton "Mover todo al almacén" se
        // quedaba con aspecto deshabilitado hasta el primer requery automatico de WPF (foco/
        // raton), encontrado al verificar con captura real, no al escribir el codigo.
        MoveInventoryToStorageCommand.NotifyCanExecuteChanged();
    }
}
