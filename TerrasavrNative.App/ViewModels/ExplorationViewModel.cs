using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TerrasavrNative.Core.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.WldFormat;

namespace TerrasavrNative.App.ViewModels;

// Punto 4 del feedback del usuario ("el mundo... podria tener un buscador de todo tipo de
// objetos, no es un editor pero si un buscador... que Opus haga ingenieria inversa a tedit en
// su buscador"). Fase 1 de ESPEC-buscador-mundo-tedit.md (advisor Opus): una fila de resultado,
// hermana real de WhereIsItResultViewModel (que hace lo mismo para el inventario del
// personaje) - icono deliberadamente ausente en esta primera pasada (tiles/paredes no tienen
// un catalogo de sprites propio como los objetos, a diferencia de WhereIsIt).
// Fase 3 (ESPEC-buscador-mundo-tedit.md#5.3): ObservableObject para poder resaltar el
// resultado "actual" (navegacion anterior/siguiente circular, mismo mecanismo real que
// GoToCurrentResult/ShowCrosshair de TEdit) y mostrar la distancia al spawn bajo demanda
// (CalculateDistance real de TEdit, casilla apagada por defecto) sin tener que rehacer la
// busqueda entera cada vez.
public sealed partial class WorldSearchHitRowViewModel(WorldSearchHit hit) : ObservableObject
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    public int TileX { get; } = hit.X;
    public int TileY { get; } = hit.Y;
    public string Name { get; } = hit.Name;
    public string Position { get; } = $"({hit.X}, {hit.Y})";
    public string KindLabel { get; } = hit.Kind switch
    {
        WorldSearchKind.Tile => "Tile",
        WorldSearchKind.Wall => LocalizationService.Instance["kind_wall"],
        WorldSearchKind.Liquid => LocalizationService.Instance["kind_liquid"],
        WorldSearchKind.Npc => "NPC",
        // Fase 2 (ESPEC-buscador-mundo-tedit.md#5.2): la coordenada de un objeto de cofre es la
        // del COFRE, no la del objeto - mismo criterio real que TEdit (SearchContainers).
        WorldSearchKind.ChestItem => LocalizationService.Instance["kind_in_chest"],
        // Fase 2b: marco de objeto/perchero/maniqui/bandeja/frasco/ancla - mismo criterio de
        // coordenada que ChestItem (la del contenedor, no la del objeto).
        WorldSearchKind.TileEntityItem => LocalizationService.Instance["kind_in_object"],
        WorldSearchKind.Sign => LocalizationService.Instance["kind_sign"],
        WorldSearchKind.OreVein => LocalizationService.Instance["unit_vein_capitalized"],
        _ => "",
    };

    // P-8 (auditoria de Opus vs TEdit): "la pildora KindLabel de cada resultado es siempre
    // TealBrush - un color por familia permite escanear la lista sin leer". Sin inventar
    // colores nuevos: los 7 solidos ya reales de Theme.xaml, reutilizando el mismo criterio que
    // ya usa el resto de la app donde aplica (PinkBrush para NPC, igual que su marcador magenta
    // del mapa; MasterGoldBrush para lo que sale de un contenedor, igual que el tesoro).
    public Brush KindColor { get; } = (Brush)System.Windows.Application.Current.Resources[hit.Kind switch
    {
        WorldSearchKind.Wall => "OrangeBrush",
        WorldSearchKind.Liquid => "AccentBrush",
        WorldSearchKind.Npc => "PinkBrush",
        WorldSearchKind.ChestItem or WorldSearchKind.TileEntityItem => "MasterGoldBrush",
        WorldSearchKind.Sign => "EquippedGreenBrush",
        WorldSearchKind.OreVein => "CalamityBrush",
        _ => "TealBrush", // Tile y cualquier valor de reserva
    }];

    // Fase 3: null = "distancia al spawn" apagada (no se muestra) - ExplorationViewModel.
    // ApplyWorldSearchOrder es quien la calcula/limpia, nunca este constructor (el spawn real
    // solo se conoce con el mundo cargado, no al crear la fila).
    [ObservableProperty] private string? _distanceLabel;

    // Fase 3: el resultado activo de la navegacion anterior/siguiente - resalta la fila en la
    // lista Y el marcador en el mapa (mismo objeto, dos plantillas distintas).
    [ObservableProperty] private bool _isCurrent;
}

// Punto 4 (advisor Opus, "una nueva barra lateral... buscar npcs buscador de cofres buscador o
// marcador de minerales buscador de objetos" - ver ESPEC-ui-exploracion.md#9.1). La categoria
// "manda": cada una muestra de entrada un inventario real de lo que el mundo cargado tiene, no
// solo filtra un catalogo generico. "Todo" es el buscador de texto libre ya existente,
// sin cambios de comportamiento.
public enum WorldSearchCategory { All, Npcs, Chests, Ores, Objects }

// Fila de inventario generica - reutilizada por Cofres (las dos vistas), Minerales y Objetos
// (las tres vistas). NPCs sigue con su propio WorldNpcRowViewModel (ya existente, con icono real
// y estado de mapa) - un inventario generico no le aporta nada que no tenga ya.
//
// Encargo del usuario 4-sep-2026 ("faltan todos los sprites... solo salen cuadrados de colores"):
// IconPath es el sprite REAL de lo que representa la fila, resuelto por QUIEN construye la fila
// (cada vista sabe que significa su Id/U/V - ver ESPEC-sprites-botones-badges.md#A.1; meter esa
// decision aqui dentro obligaria a esta clase a saber de que vista viene, que es justo lo que la
// hace reutilizable). SwatchColor NO desaparece: es el respaldo real de la plantilla cuando
// IconPath es null (tiles de mods, liquidos, NetId sin icono extraido).
public sealed partial class WorldInventoryRowViewModel(int id, short u, short v, string name, int count, int? veinCount, string? iconPath, Color swatchColor) : ObservableObject
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    public int Id { get; } = id;
    public short U { get; } = u;
    public short V { get; } = v;
    public string Name { get; } = name;
    public int Count { get; } = count;
    public int? VeinCount { get; } = veinCount;
    public string? IconPath { get; } = iconPath;
    public Color SwatchColor { get; } = swatchColor;
    // ESPEC-ui-exploracion.md#9.3-D: "86.200 tiles · 6.738 vetas" para Minerales; para el resto
    // (Cofres/Objetos), solo el recuento a secas.
    public string CountLabel { get; } = veinCount.HasValue
        ? $"{count:N0} tiles · {veinCount.Value:N0} veta{(veinCount.Value == 1 ? "" : "s")}"
        : $"{count:N0}";
    // C-04 (informe de pulido final, cierra E6/E7): el tick pasa a significar "muestralo en el
    // mapa" - quien construye la fila (Minerales/Objetos) se suscribe para relanzar el marcado
    // con debounce (ExplorationViewModel._highlightDebounceTimer), sin que esta fila generica
    // necesite saber nada de mapas ni de resaltado.
    public event Action? CheckedChanged;
    [ObservableProperty] private bool _isChecked;
    partial void OnIsCheckedChanged(bool value) => CheckedChanged?.Invoke();
    // Filtro por nombre O id (mismo criterio que TileWallPickerViewModel.FilterItem de TEdit,
    // ESPEC-ui-exploracion.md#1.3) - atenua/oculta en vez de quitar de la coleccion, mismo
    // patron ya establecido por WorldNpcRowViewModel.IsMatch.
    [ObservableProperty] private bool _isMatch = true;
}

public sealed partial class WorldNpcRowViewModel(int id, string name, int x, int y, bool homeless, int? headIndex, bool isUnderground, int depthTiles) : ObservableObject
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    public int Id { get; } = id;
    public string Name { get; } = name;
    public int TileX { get; } = x;
    public int TileY { get; } = y;
    public bool Homeless { get; } = homeless;
    public string Position { get; } = homeless ? $"({x}, {y}) - sin casa" : $"({x}, {y})";
    public string? IconPath { get; } = NpcIconResolver.GetIconPath(id);
    // Punto 4 (advisor Opus, "npcs escondidos en el subsuelo que puedas encontrarlos facilmente" -
    // ver ESPEC-ui-exploracion.md#12): TileY > GroundLevel real del mundo (WldHeader.GroundLevel,
    // ya leido y ya usado para el fondo por zona, ZoneFor). DepthTiles solo tiene sentido si
    // IsUnderground - la profundidad EN TILES bajo el nivel del suelo, mas legible que una
    // coordenada Y suelta.
    public bool IsUnderground { get; } = isUnderground;
    public int DepthTiles { get; } = depthTiles;
    public string? DepthLabel { get; } = isUnderground ? LocalizationService.Instance.Format("npc_depth_label", depthTiles) : null;
    // H6-08/H6-09/H6-10 (sexta auditoria de Opus, "el mapa debe mostrar solo cabezas de NPC,
    // no puntos rosas ni el cuerpo entero") - icono real de cabeza (NpcHeadProfile ya resolvio
    // el indice real: normal/shimmer/variacion segun toque), usado por el marcador del MAPA
    // (MainWindow.xaml); IconPath de arriba se queda para la lista lateral, sin cambios.
    public string? HeadIconPath { get; } = headIndex.HasValue ? NpcHeadIconResolver.GetIconPath(headIndex.Value) : null;

    // X-c (segunda auditoria de Opus, Fable): "el buscador de NPCs oculta marcadores del mapa" -
    // antes filtrar el buscador VACIABA la unica coleccion Npcs, que es la MISMA que dibuja los
    // marcadores del mapa (ver el ItemsControl del mapa en MainWindow.xaml) - buscar "Enfermera"
    // hacia desaparecer del mapa entero a todos los demas NPCs, no solo de la lista lateral.
    // Npcs se queda siempre completa (ver ExplorationViewModel.ApplyNpcFilter); esto marca cada
    // NPC como coincide/no-coincide para RESALTAR en el mapa en vez de ocultar - true por
    // defecto (sin busqueda activa, todos coinciden).
    [ObservableProperty] private bool _isMatch = true;
}

// X-g (segunda auditoria de Opus, Fable): "el mapa no sabe nada del personaje real" - antes
// Exploracion era un visor totalmente independiente del personaje cargado (cualquier .wld,
// sin relacion con nada de Personaje). Un punto de aparicion REAL del personaje ya cargado,
// de los guardados en la pestaña Spawn Points (PlrServerEntry.SpawnX/Y - el .plr no guarda
// ninguna "aparicion principal" propia aparte de estos, ver el comentario en MainViewModel.
// BuildCharacterSpawns) - coordenadas de tile real, mismo espacio que TileX/TileY de los NPCs.
public sealed class CharacterSpawnRowViewModel(string label, int x, int y)
{
    public string Label { get; } = label;
    public int TileX { get; } = x;
    public int TileY { get; } = y;
}

// Un NPC del roster que el jugador todavia no tiene en este mundo - solo nombre+icono, sin
// posicion (no esta en el mundo).
public sealed class MissingNpcRowViewModel(int id, string name)
{
    public string Name { get; } = name;
    public string? IconPath { get; } = NpcIconResolver.GetIconPath(id);
}

// C-06 (informe de pulido final, ESPEC-pulido-final-libreria-inicio-apariencia.md#9.6, cierra
// E8): un objeto real DENTRO de un cofre concreto (Cofre a cofre, tercer modo de ChestViewMode) -
// gemelo de "Por lo que contienen" (WorldInventoryRowViewModel) pero SIN el recuento agregado de
// todo el mundo, con cantidad y prefijo REALES de ESTA pieza suelta.
public sealed class ChestContentItemViewModel(int netId, string name, int stack, string? prefixName, string? iconPath)
{
    public int NetId { get; } = netId;
    public string Name { get; } = name;
    public int Stack { get; } = stack;
    public string? PrefixName { get; } = prefixName;
    public string? IconPath { get; } = iconPath;
}

// C-06: una fila real de la vista "Cofre a cofre" - _world.Chests, uno por cofre real del mundo
// (no agrupado por variante ni por contenido, al contrario que los otros dos modos de
// ChestViewMode). IsExpanded controla si Items se ve o no (desplegable real al pulsar la fila).
public sealed partial class ChestRowViewModel(string variantName, string? chestName, int x, int y, string? iconPath, IReadOnlyList<ChestContentItemViewModel> items) : ObservableObject
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    public string VariantName { get; } = variantName;
    // WldChest.Name - el nombre propio que el jugador le puso al cofre (renombrar un cofre es
    // una accion real del juego) - antes no se mostraba en NINGUN sitio de la app (C-19).
    public string? ChestName { get; } = string.IsNullOrWhiteSpace(chestName) ? null : chestName;
    public int TileX { get; } = x;
    public int TileY { get; } = y;
    public string? IconPath { get; } = iconPath;
    public IReadOnlyList<ChestContentItemViewModel> Items { get; } = items;
    public int ItemCount { get; } = items.Count;
    [ObservableProperty] private bool _isExpanded;
    // Mismo filtro por nombre/id ya establecido (ApplyInventoryFilter) - por variante, nombre
    // propio del cofre o cualquier objeto real de dentro.
    [ObservableProperty] private bool _isMatch = true;
}

// Pestaña "Exploracion" - cargar un .wld real, pintarlo entero (WorldRenderer), listar sus
// NPCs de pueblo con su posicion (con buscador por nombre) y marcar cuales NPCs de pueblo
// reales (VanillaTownNpcRoster) todavia no tiene el jugador en este mundo. Solo lectura, no
// coloca/quita tiles (misma restriccion que el visor JS de referencia).
public partial class ExplorationViewModel : ObservableObject
{
    private readonly NpcNameCatalog _npcNames;
    private readonly MapColorCatalog _mapColors;
    private readonly TileNameCatalog _tileNames;
    // Fase 2 (ESPEC-buscador-mundo-tedit.md#5.2): nombres reales de objeto para resolver que
    // hay dentro de un cofre encontrado - un NetId de Calamity real (que tModLoader asigna en
    // tiempo de carga del mod, no coincide con el synthetic id que usa el resto de este puerto
    // para .plr) cae en su propio "Item #N" de fallback, nunca se inventa.
    private readonly VanillaItemCatalog _itemNames;
    // C-06: nombre real del prefijo de cada objeto suelto dentro de un cofre ("Cofre a cofre").
    private readonly VanillaPrefixCatalog _prefixNames;
    private List<WorldNpcRowViewModel> _allNpcs = [];
    private WldWorld? _world;
    // Punto 4 (advisor Opus, "que solo puedan salir los objetos que tiene ese mundo" - ver
    // ESPEC-ui-exploracion.md#10.3): censo real del mundo cargado, calculado una vez en el mismo
    // Task.Run que ya lee+pinta (LoadFromPathAsync). Null sin mundo cargado, igual que _world.
    private WorldPresenceIndex? _presence;

    [ObservableProperty] private BitmapSource? _worldImage;
    // Punto 4 (Minerales - ESPEC-ui-exploracion.md#11.3): capa de resaltado de mineral, una
    // segunda Image dentro del mismo Grid escalado que WorldMapImage - null = sin marcar nada.
    [ObservableProperty] private BitmapSource? _worldHighlight;
    [ObservableProperty] private string _statusMessage = LocalizationService.Instance["status_no_world_loaded_dot"];
    [ObservableProperty] private string? _worldTitle;
    // C-05 (informe de pulido final, cierra E4): WorldId real del mundo cargado (WldHeader.
    // WorldId) - MainViewModel.BuildCharacterSpawns lo cruza con PlrServerEntry.WorldId para
    // saber si un Spawn Point guardado pertenece de verdad a ESTE mundo (regla real del propio
    // juego, Player.FindSpawn: id Y nombre tienen que coincidir). Null sin mundo cargado.
    public int? LoadedWorldId => _world?.Header.WorldId;
    // F-15 (auditoria de Opus vs TEdit): tamaño real del mundo, para la franja de la barra
    // superior consciente de la pestaña - dato que ya se calculaba (StatusMessage) pero no
    // vivia en una propiedad propia reutilizable.
    [ObservableProperty] private string _worldSizeText = "—";
    // F-14 (auditoria de Opus vs TEdit, E-16/E-17): panel "Este mundo" - coste 0 (WldHeader ya
    // los leia y los descartaba, ver el comentario real de WldReader.cs). Alcance acotado a
    // proposito a lo que el propio informe recomienda para una primera pasada (semilla+modo,
    // sin banderas de jefes/modo dificil - eso exige avanzar mucho mas el lector, mas riesgo).
    [ObservableProperty] private string _worldSeedText = "—";
    [ObservableProperty] private string _worldGameModeText = "—";
    [ObservableProperty] private string _worldVersionText = "—";
    // Pedido explicito del usuario (5-sep-2026): poder cambiar la dificultad del mundo, las 4
    // posibilidades reales de Terraria - unica excepcion real de todo el visor a "Solo lectura"
    // (ver WldWriter.PatchGameMode/WorldFileService.SaveGameMode). WorldGameMode es la seleccion
    // EN MEMORIA (el chip que se ve marcado, cambia libremente sin tocar el disco);
    // _savedWorldGameMode es el ultimo valor confirmado en el ARCHIVO real - solo cuando
    // difieren tiene sentido "Guardar" (CanSaveWorldGameMode).
    [ObservableProperty] private int _worldGameMode;
    private int _savedWorldGameMode;
    [ObservableProperty] private string? _worldGameModeSaveStatus;

    partial void OnWorldGameModeChanged(int value) => SaveWorldGameModeCommand.NotifyCanExecuteChanged();

    private static string GameModeLabel(int gameMode) => gameMode switch
    {
        1 => LocalizationService.Instance["explore_gamemode_expert"],
        2 => LocalizationService.Instance["explore_gamemode_master"],
        3 => LocalizationService.Instance["explore_gamemode_journey"],
        _ => LocalizationService.Instance["explore_gamemode_classic"],
    };

    private bool CanSaveWorldGameMode() => IsWorldLoaded && _currentWorldPath != null && WorldGameMode != _savedWorldGameMode;

    [RelayCommand(CanExecute = nameof(CanSaveWorldGameMode))]
    private async Task SaveWorldGameModeAsync()
    {
        if (_world == null || _currentWorldPath == null) return;
        int nuevoModo = WorldGameMode;
        var mundoActual = _world;
        string ruta = _currentWorldPath;
        WorldGameModeSaveStatus = LocalizationService.Instance["status_saving"];
        try
        {
            var mundoActualizado = await Task.Run(() => WorldFileService.SaveGameMode(mundoActual, ruta, nuevoModo));
            _world = mundoActualizado;
            _savedWorldGameMode = nuevoModo;
            WorldGameModeText = GameModeLabel(nuevoModo);
            WorldGameModeSaveStatus = LocalizationService.Instance.Format("status_saved_backup", Path.GetFileName(ruta));
        }
        catch (Exception ex)
        {
            // Nunca silencioso - un fallo aqui toca el archivo de mundo real del usuario, tiene
            // que verse con toda claridad, no solo en StatusMessage (que otra accion cualquiera
            // puede pisar en el instante siguiente).
            WorldGameModeSaveStatus = LocalizationService.Instance.Format("status_save_failed", ex.Message);
        }
        finally
        {
            SaveWorldGameModeCommand.NotifyCanExecuteChanged();
        }
    }
    // F-7 (auditoria de Opus vs TEdit, E-06): "el punto de aparicion del mundo... su unico uso
    // en toda la aplicacion es calcular la distancia. No hay ningun marcador de spawn en el
    // mapa" - y la mazmorra "ni siquiera se leen" (ya corregido en WldReader/WldHeader, ver sus
    // comentarios). Coordenadas de tile real, mismo espacio que TileX/TileY de NPCs/Spawns.
    [ObservableProperty] private int _worldSpawnX;
    [ObservableProperty] private int _worldSpawnY;
    [ObservableProperty] private int _worldDungeonX;
    [ObservableProperty] private int _worldDungeonY;
    [ObservableProperty] private bool _isWorldLoaded;
    [ObservableProperty] private string _npcSearchText = string.Empty;
    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private string _hoverInfo = string.Empty;

    // F-6 (auditoria de Opus vs TEdit, E-07): campos separados para la franja de estado fija de
    // P-1 (MainWindow.xaml) - HoverInfo (arriba) NO se toca, sigue siendo la cadena unica del
    // tooltip flotante que sigue al cursor (pedido explicito del usuario, 1-sep-2026). "—" por
    // defecto (nunca vacio) para que la franja no cambie de alto al entrar/salir del mapa -
    // mismo motivo real que P-1 documenta.
    [ObservableProperty] private string _hoverCoordText = "—";
    [ObservableProperty] private string _hoverLayerText = "—";
    [ObservableProperty] private Color _hoverLayerColor = Colors.Transparent;
    [ObservableProperty] private string _hoverDepthText = "—";
    [ObservableProperty] private string _hoverTileText = "—";
    [ObservableProperty] private string _hoverWallText = "—";
    [ObservableProperty] private string _hoverLiquidText = "—";
    // Auditoria de Opus, Bloque 3 (X-7/T-13): medido de verdad antes de tocar nada (no de
    // memoria) - un mundo .wld real y grande de esta maquina (11MB, 8400x2400 tiles) tarda
    // ~1.4s en leerse+pintarse, congelando el hilo de UI entero sin ningun aviso mientras tanto
    // (ni spinner, ni "cargando...", la app parece colgada). Cargar un personaje (~8ms) o
    // "Investigar todo" (~3ms, en memoria) NO mostraron ningun freeze real medible - por eso
    // solo el mundo se hace async aqui, no los otros dos (no resolver un problema que no existe
    // de verdad).
    [ObservableProperty] private bool _isLoading;
    public bool IsNotLoading => !IsLoading;
    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotLoading));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsLoadingOverExistingWorld));
    }

    // P-8 (auditoria de Opus vs TEdit): "el overlay de carga tapa el mapa con #B0000000 opaco;
    // con lienzo vacio no aporta nada". IsWorldLoaded NO se resetea al empezar una carga (solo
    // al terminar, ver LoadFromPathAsync) - asi que durante la RECARGA de un mundo distinto
    // sigue valiendo True (hay un mapa anterior real que oscurecer), y durante la PRIMERA carga
    // vale False (el lienzo ya esta vacio, oscurecerlo no tiene efecto util).
    public bool IsLoadingOverExistingWorld => IsLoading && IsWorldLoaded;

    // H4-08 (cuarta auditoria de Opus, Fable): "el estado vacio de Exploracion es un lienzo
    // negro sin guia" - el unico aviso real ("Sin mundo cargado.") vivia en StatusMessage, en
    // el tamaño de letra mas pequeño de la app y en la esquina inferior izquierda. Real,
    // centrado en el propio lienzo (mismo patron ya usado por el overlay de IsLoading, ver
    // MainWindow.xaml) - nunca durante la carga, para no parpadear entre los dos avisos.
    public bool IsEmpty => !IsWorldLoaded && !IsLoading;
    partial void OnIsWorldLoadedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsLoadingOverExistingWorld));
    }

    // Siempre TODOS los NPCs del mundo - lo que dibuja los marcadores del mapa (nunca se
    // filtra, ver X-c arriba).
    public ObservableCollection<WorldNpcRowViewModel> Npcs { get; } = [];
    // X-c: solo los que coinciden con el buscador - lo que muestra la lista lateral de texto.
    public ObservableCollection<WorldNpcRowViewModel> NpcSearchResults { get; } = [];
    public ObservableCollection<MissingNpcRowViewModel> MissingNpcs { get; } = [];
    // X-g: puntos de aparicion reales del personaje cargado (principal + Spawn Points
    // guardados) - independiente de si hay mundo cargado o no (se rellena/limpia igual, los
    // marcadores solo se ven cuando ADEMAS hay un mundo real a la vista).
    public ObservableCollection<CharacterSpawnRowViewModel> CharacterSpawns { get; } = [];

    // Punto 4: buscador real de "todo tipo de objetos del mundo" - tiles/paredes/liquidos/NPCs
    // (Fase 1) + objetos dentro de cofres y texto de letreros (Fase 2, ESPEC-buscador-mundo-
    // tedit.md - ahora que WldReader lee esas dos secciones del .wld). Tile entities
    // (maniquies/marcos de item/percheros) siguen fuera a proposito, ver el comentario de
    // WldReader.Read. Reutiliza LibrarySearchGrammar (comas=OR, espacios=AND, "#123"/
    // "#100-200" por id) - la misma gramatica real que ya usa la Libreria de objetos/buffs,
    // para que el usuario no tenga que aprender una sintaxis nueva.
    [ObservableProperty] private string _worldSearchText = string.Empty;
    [ObservableProperty] private string _worldSearchSummary = string.Empty;
    partial void OnWorldSearchSummaryChanged(string value)
    {
        OnPropertyChanged(nameof(ShowZeroResultsState));
        OnPropertyChanged(nameof(ShowCompactSummary));
    }

    // P-6 (auditoria de Opus vs TEdit, cierra E-01 estetico): estado "sin resultados" con
    // cuerpo, no solo el texto de 11px de WorldSearchSummary. Acotado a "Todo" (la unica
    // categoria con busqueda de verdad asincrona - las otras 4 filtran en memoria via IsMatch,
    // sin equivalente a WorldSearchSummary; extenderlo alli exigiria plumbing nuevo que no
    // aporta lo mismo, fuera de alcance de esta pasada).
    public bool ShowZeroResultsState => SelectedCategory == WorldSearchCategory.All && WorldSearchSummary == LocalizationService.Instance["status_no_results"];
    // El resumen compacto de siempre (F-1) se sigue mostrando para CUALQUIER resultado no vacio
    // (incluida la limitacion de 1000) - solo se sustituye por el panel de P-6 en el caso
    // concreto de 0 resultados.
    public bool ShowCompactSummary => !string.IsNullOrEmpty(WorldSearchSummary) && !ShowZeroResultsState;
    public ObservableCollection<WorldSearchHitRowViewModel> WorldSearchResults { get; } = [];

    // Fase 3 (ESPEC-buscador-mundo-tedit.md#5.3 puntos 4/5): navegacion circular anterior/
    // siguiente (misma logica real que NavigateNext/NavigatePrevious de TEdit, con modulo) y
    // distancia opcional al spawn (CalculateDistance real, apagada por defecto -
    // "Default false" es el comentario literal de TEdit). _lastWorldSearchRows guarda el orden
    // REAL del barrido (WorldSearch.Run) para poder reordenar por distancia sin repetir la
    // busqueda - ApplyWorldSearchOrder es el unico sitio que toca WorldSearchResults.
    [ObservableProperty] private bool _showSpawnDistance;
    partial void OnShowSpawnDistanceChanged(bool value) => ApplyWorldSearchOrder();
    private List<WorldSearchHitRowViewModel> _lastWorldSearchRows = [];

    // F-3 (auditoria de Opus vs TEdit, E-12): "ir a un resultado nunca ajusta el zoom, y no hay
    // opcion de que lo haga". El valor por defecto False (solo desplazar, sin zoom) ya era
    // correcto - citaba el mismo "Default false - just pan, don't zoom" de TEdit,
    // FindSidebarViewModel.cs:61 - lo que faltaba era la CASILLA para que el usuario decida.
    // El code-behind (OnNavigateToTile, MainWindow.xaml.cs) es quien lee esta propiedad.
    [ObservableProperty] private bool _autoZoomOnNavigate;
    private int _worldSearchCurrentIndex = -1;

    private readonly DispatcherTimer _worldSearchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private CancellationTokenSource? _worldSearchCts;
    private int _worldSearchGeneration;

    // Punto 4 (advisor Opus, "una nueva barra lateral... que ya no solo salgan los npc, eso se
    // traslada dentro de una rama madre" - ver ESPEC-ui-exploracion.md#9). La categoria activa;
    // NPCs sigue usando Npcs/NpcSearchResults (ya existente); Cofres/Minerales/Objetos usan
    // Inventory (u OreMetals/OreGems/OreTargets para Minerales, que necesita 3 grupos).
    [ObservableProperty] private WorldSearchCategory _selectedCategory = WorldSearchCategory.All;
    partial void OnSelectedCategoryChanged(WorldSearchCategory value)
    {
        // Cambiar de categoria es empezar de cero - mismo criterio que cargar otro mundo ya
        // limpia el buscador (LoadFromPathAsync). WorldSearchText/NpcSearchText disparan su
        // propio OnXxxChanged si de verdad cambian (CommunityToolkit no reemite si el valor es
        // el mismo, ej. las dos ya estaban vacias).
        WorldSearchText = string.Empty;
        NpcSearchText = string.Empty;
        RebuildInventory();
        OnPropertyChanged(nameof(ShowZeroResultsState));
        OnPropertyChanged(nameof(ShowCompactSummary));
    }

    public ObservableCollection<WorldInventoryRowViewModel> Inventory { get; } = [];
    // C-06 (informe de pulido final, cierra E8): tercer modo real de ChestViewMode ("Cofre a
    // cofre") - una fila por cofre real de _world.Chests, coleccion PROPIA (no reutiliza
    // Inventory, estructuralmente distinta: desplegable, con su propia lista de objetos dentro).
    public ObservableCollection<ChestRowViewModel> ChestRows { get; } = [];
    // Minerales necesita 3 grupos con cabecera (Minerales/Gemas/Otros objetivos,
    // ESPEC-ui-exploracion.md#11.1) - 3 colecciones separadas en vez de un mecanismo de
    // agrupado WPF generico, mismo criterio ya usado en el proyecto (Npcs/NpcSearchResults/
    // MissingNpcs son 3 colecciones separadas, no una con un flag de tipo).
    public ObservableCollection<WorldInventoryRowViewModel> OreMetals { get; } = [];
    public ObservableCollection<WorldInventoryRowViewModel> OreGems { get; } = [];
    public ObservableCollection<WorldInventoryRowViewModel> OreTargets { get; } = [];

    // ESPEC-ui-exploracion.md#9.3-C: "Por tipo de cofre" (0, por defecto) / "Por lo que
    // contienen" (1). #9.3-E: "Tiles" (0, por defecto) / "Paredes" (1) / "Liquidos" (2).
    [ObservableProperty] private int _chestViewMode;
    partial void OnChestViewModeChanged(int value) => RebuildChestInventory();
    [ObservableProperty] private int _objectsViewMode;
    partial void OnObjectsViewModeChanged(int value) => RebuildObjectsInventory();

    // Contadores reales para las pildoras de categoria (ESPEC-ui-exploracion.md#9.1: "NPCs (18)",
    // "Cofres (560)"...) - Minerales/Objetos cuentan TIPOS distintos presentes, no instancias
    // (asi es como el propio espec los midio y los describe: "260 tipos de tile", "16 minerales
    // presentes"). NpcsPillCount no hace falta un campo propio, Npcs.Count ya es el real.
    public int ChestsPillCount => _world?.Chests.Count ?? 0;
    public int OresPillCount => _presence == null ? 0 : OreTileCatalog.All.Count(_presence.HasTile);
    public int ObjectsPillCount => _presence?.TileCounts.Count ?? 0;

    // F-14 (auditoria de Opus vs TEdit, E-17): "el censo del mundo existe por dentro
    // (WorldPresenceIndex) pero no hay ningun sitio donde verlo de un vistazo" - panel "Este
    // mundo", todo ya calculado, cero recorridos nuevos.
    public int WallTypesPresentCount => _presence?.WallCounts.Count ?? 0;
    public int SignCount => _presence?.SignCount ?? 0;
    public string WorldAirPercentText
    {
        get
        {
            if (_presence == null || _world == null) return "—";
            long total = (long)_world.Header.TilesWide * _world.Header.TilesHigh;
            if (total == 0) return "—";
            long activos = _presence.TileCounts.Values.Sum(v => (long)v);
            double airePct = 1.0 - (double)activos / total;
            return airePct.ToString("P1");
        }
    }

    // F-14: informe de texto plano (mismo espiritu que AnalyzeWorldSaveCommand de TEdit) - el
    // dialogo real de guardado vive en el code-behind (mismo criterio que ExportMapToPng).
    public string BuildWorldReportText()
    {
        if (_world == null || _presence == null) return string.Empty;
        var loc = LocalizationService.Instance;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{WorldTitle}");
        sb.AppendLine(loc.Format("report_seed", WorldSeedText));
        sb.AppendLine(loc.Format("report_game_mode", WorldGameModeText));
        sb.AppendLine(loc.Format("report_size", WorldSizeText));
        sb.AppendLine(loc.Format("report_format_version", WorldVersionText));
        sb.AppendLine();
        sb.AppendLine(loc["report_census_header"]);
        sb.AppendLine(loc.Format("report_air", WorldAirPercentText));
        sb.AppendLine(loc.Format("report_tile_types", ObjectsPillCount));
        sb.AppendLine(loc.Format("report_wall_types", WallTypesPresentCount));
        sb.AppendLine(loc.Format("report_chests", ChestsPillCount));
        sb.AppendLine(loc.Format("report_signs", _presence.SignCount));
        sb.AppendLine(loc.Format("report_town_npcs", _allNpcs.Count));
        sb.AppendLine();
        sb.AppendLine(loc["report_top10_header"]);
        long totalTiles = (long)_world.Header.TilesWide * _world.Header.TilesHigh;
        foreach (var (type, count) in _presence.TileCounts.OrderByDescending(kv => kv.Value).Take(10))
            sb.AppendLine($"{_tileNames.TileName(type)} [{type}]: {count:N0} ({(double)count / totalTiles:P2})");
        return sb.ToString();
    }

    private static Color ToWpfColor(RgbaColor c) => Color.FromArgb(c.A, c.R, c.G, c.B);

    // Unico punto de entrada real - despacha segun SelectedCategory. "Todo"/"NPCs" no usan
    // Inventory (el buscador de texto libre y la lista de NPCs ya existentes, respectivamente).
    private void RebuildInventory()
    {
        Inventory.Clear();
        OreMetals.Clear();
        OreGems.Clear();
        OreTargets.Clear();
        if (_world == null || _presence == null) return;

        switch (SelectedCategory)
        {
            case WorldSearchCategory.Chests: RebuildChestInventory(); break;
            case WorldSearchCategory.Ores: RebuildOreInventory(); break;
            case WorldSearchCategory.Objects: RebuildObjectsInventory(); break;
        }
    }

    // ESPEC-ui-exploracion.md#9.3-C: por defecto, las variantes de cofre REALMENTE presentes
    // (ChestKindCounts, la casilla real en chest.X/Y - ya calculado por WorldPresenceIndex, no
    // hace falta volver a recorrer nada); en el otro modo, los objetos REALMENTE encontrados
    // dentro de algun cofre (ChestItemCounts).
    private void RebuildChestInventory()
    {
        Inventory.Clear();
        ChestRows.Clear();
        if (_world == null || _presence == null) return;
        if (ChestViewMode == 0)
        {
            foreach (var ((type, u, v), count) in _presence.ChestKindCounts.OrderByDescending(kv => kv.Value))
                Inventory.Add(new WorldInventoryRowViewModel(type, u, v, _tileNames.TileVariantName(type, u, v), count, null,
                    TileIconResolver.GetIconPath(type, u, v), ToWpfColor(_mapColors.TileColor(type))));
        }
        else if (ChestViewMode == 2)
        {
            RebuildChestByChest();
            return; // ApplyInventoryFilter de mas abajo es solo para Inventory - ChestRows tiene su propio filtro
        }
        else
        {
            // Fase 2b: "Por lo que contienen" ya no es solo cofres - une el contenido real de
            // cofres CON el de tile entities (marco de objeto/perchero/maniqui/bandeja/frasco/
            // ancla), mismo NetId cuenta las dos fuentes juntas. La busqueda por fila
            // (BuildSingleRowQuery, ya usa ChestItemIds) casa contra ambas sin cambios, ver el
            // comentario real de WorldSearch.Run.
            var combinados = new Dictionary<int, int>(_presence.ChestItemCounts);
            foreach (var (netId, count) in _presence.TileEntityItemCounts)
                combinados[netId] = combinados.GetValueOrDefault(netId) + count;

            foreach (var (netId, count) in combinados.OrderByDescending(kv => kv.Value))
                // El Id es un NetId REAL de objeto del .wld, NO un id sintetico de esta app -
                // GameItem.IsCalamity (Id >= 20.000.000) nunca es cierto aqui, asi que NO hay
                // rama de Calamity que valga: un objeto modeado dentro de un cofre trae el id de
                // runtime que tModLoader le asigno, que no se puede traducir (mismo motivo por el
                // que _itemNames.GetName ya cae en "Item #N", ver el comentario de arriba).
                // VanillaIconResolver devuelve null para todos ellos -> cuadradito de color,
                // igual que hoy. Ver ESPEC-sprites-botones-badges.md#A.2.
                Inventory.Add(new WorldInventoryRowViewModel(netId, 0, 0, _itemNames.GetName(netId), count, null,
                    VanillaIconResolver.GetIconPath(netId), Colors.Transparent));
        }
        ApplyInventoryFilter();
    }

    // ESPEC-ui-exploracion.md#11: tres grupos (Minerales/Gemas/Otros objetivos), solo los
    // presentes (_presence.HasTile). El recuento de VETAS usa CountVeinsByType con TODOS los
    // presentes JUNTOS en una unica llamada (un unico barrido de la rejilla) - llamarlo una vez
    // por mineral es correcto pero MUY caro (hallazgo real medido esta sesion: ~5s en un mundo
    // Grande real llamando a Find uno a uno, 255ms con la llamada combinada).
    private void RebuildOreInventory()
    {
        OreMetals.Clear();
        OreGems.Clear();
        OreTargets.Clear();
        if (_world == null || _presence == null) return;

        var presentes = OreTileCatalog.All.Where(_presence.HasTile).ToHashSet();
        var vetasPorTipo = presentes.Count > 0
            ? OreVeinFinder.CountVeinsByType(_world, presentes)
            : new Dictionary<int, int>();

        void Fill(ObservableCollection<WorldInventoryRowViewModel> target, IReadOnlyList<int> ids)
        {
            foreach (int id in ids)
            {
                if (!_presence.HasTile(id)) continue;
                int count = _presence.TileCounts[id];
                int veinCount = vetasPorTipo.GetValueOrDefault(id);
                var row = new WorldInventoryRowViewModel(id, 0, 0, _tileNames.TileName(id), count, veinCount,
                    TileIconResolver.GetIconPath(id), ToWpfColor(_mapColors.TileColor(id)));
                // C-04: el tick de cualquier mineral relanza el marcado en el mapa con debounce.
                row.CheckedChanged += OnHighlightCheckToggled;
                target.Add(row);
            }
        }
        Fill(OreMetals, OreTileCatalog.Metals);
        Fill(OreGems, OreTileCatalog.Gems);
        Fill(OreTargets, OreTileCatalog.Targets);
        ApplyInventoryFilter();
    }

    // ESPEC-ui-exploracion.md#9.3-E: Tiles/Paredes/Liquidos, ordenados por recuento descendente
    // (igual que WorldAnalysis.cs de TEdit). ALCANCE DELIBERADO, no un descuido: no se porta el
    // arbol de dos niveles (tile + sus variantes de UV) que propone el espec - Cofres/"Por tipo
    // de cofre" ya cubre el caso real donde mas importa distinguir variantes (que cofre es cada
    // uno), y un arbol expandible generico para las demas 130 familias de sprite es una pieza de
    // UI de WPF bastante mas arriesgada sin poder iterarla visualmente varias veces primero.
    private void RebuildObjectsInventory()
    {
        Inventory.Clear();
        if (_world == null || _presence == null) return;
        // C-04: el tick de cualquier fila de Objetos (Tiles/Paredes/Liquidos) relanza el
        // marcado en el mapa con debounce - mismo mecanismo que Minerales, generalizado aqui.
        switch (ObjectsViewMode)
        {
            case 0:
                foreach (var (id, count) in _presence.TileCounts.OrderByDescending(kv => kv.Value))
                {
                    var row = new WorldInventoryRowViewModel(id, 0, 0, _tileNames.TileName(id), count, null,
                        TileIconResolver.GetIconPath(id), ToWpfColor(_mapColors.TileColor(id)));
                    row.CheckedChanged += OnHighlightCheckToggled;
                    Inventory.Add(row);
                }
                break;
            case 1:
                foreach (var (id, count) in _presence.WallCounts.OrderByDescending(kv => kv.Value))
                {
                    var row = new WorldInventoryRowViewModel(id, 0, 0, _tileNames.WallName(id), count, null,
                        WallIconResolver.GetIconPath(id), ToWpfColor(_mapColors.WallColor(id)));
                    row.CheckedChanged += OnHighlightCheckToggled;
                    Inventory.Add(row);
                }
                break;
            case 2:
                foreach (var (code, count) in _presence.LiquidCounts.OrderByDescending(kv => kv.Value))
                {
                    // Sin icono a proposito: el juego dibuja los liquidos con un shader sobre una
                    // mascara (LiquidMask.fxc), no hay sprite recortable - el color de la paleta
                    // real del mapa es mejor que un icono inventado. Ver
                    // ESPEC-sprites-botones-badges.md#A.11.
                    //
                    // Bug real corregido (reportado: "pestaña Liquidos no tiene sus sprites"): el
                    // swatch de respaldo (SwatchColor, ver el comentario de la clase mas abajo)
                    // se pasaba en Colors.Transparent en vez del color real - la fila no mostraba
                    // NADA, ni sprite ni color. Ahora usa MapColorCatalog.LiquidColor(code), el
                    // mismo color real que ya pinta el mapa.
                    var row = new WorldInventoryRowViewModel(code, 0, 0, WorldSearch.LiquidName(code), count, null,
                        null, ToWpfColor(_mapColors.LiquidColor(code)));
                    row.CheckedChanged += OnHighlightCheckToggled;
                    Inventory.Add(row);
                }
                break;
        }
        ApplyInventoryFilter();
    }

    // Filtro por nombre O id (gemelo exacto de TileWallPickerViewModel.FilterItem de TEdit,
    // ESPEC-ui-exploracion.md#1.3) - inmediato, sin debounce ni Task.Run: el inventario de una
    // categoria es corto de verdad (260 filas como mucho, medido), no hace falta lo mismo que
    // el barrido real de "Todo".
    private void ApplyInventoryFilter()
    {
        bool sinBusqueda = string.IsNullOrWhiteSpace(WorldSearchText);
        // C-09 (informe de pulido final, cierra L2): el "undecimo sitio" que el informe señala
        // explicitamente - no pasa por LibrarySearchGrammar (es un filtro simple, no la gramatica
        // completa de comas/espacios), pero tenia el mismo hueco real: "mascara" no encontraba
        // "máscara" aqui tampoco. Fold en vez de OrdinalIgnoreCase - plegado UNA vez por
        // WorldSearchText (constante para toda la pasada), no por fila.
        string queryFolded = sinBusqueda ? string.Empty : LibrarySearchGrammar.Fold(WorldSearchText);
        void Filtrar(IEnumerable<WorldInventoryRowViewModel> filas)
        {
            foreach (var row in filas)
                row.IsMatch = sinBusqueda || LibrarySearchGrammar.Fold(row.Name).Contains(queryFolded, StringComparison.Ordinal)
                    || row.Id.ToString().Contains(WorldSearchText, StringComparison.Ordinal);
        }
        Filtrar(Inventory);
        Filtrar(OreMetals);
        Filtrar(OreGems);
        Filtrar(OreTargets);

        // C-06: "Cofre a cofre" - por variante, nombre propio del cofre o cualquier objeto real
        // de dentro (mismo criterio de Fold ya establecido, reutiliza queryFolded).
        foreach (var chest in ChestRows)
            chest.IsMatch = sinBusqueda
                || LibrarySearchGrammar.Fold(chest.VariantName).Contains(queryFolded, StringComparison.Ordinal)
                || (chest.ChestName != null && LibrarySearchGrammar.Fold(chest.ChestName).Contains(queryFolded, StringComparison.Ordinal))
                || chest.Items.Any(it => LibrarySearchGrammar.Fold(it.Name).Contains(queryFolded, StringComparison.Ordinal))
                || $"{chest.TileX},{chest.TileY}".Contains(WorldSearchText, StringComparison.Ordinal);
    }

    // C-06 (informe de pulido final, cierra E8): una fila por cofre REAL (nunca agrupado, al
    // contrario que los otros dos modos de ChestViewMode), ordenados por distancia al spawn del
    // mundo - mismo calculo ya establecido para ShowSpawnDistance (ApplyWorldSearchOrder), "que
    // cofre tengo mas cerca" es el orden util de verdad y no cuesta nada nuevo.
    private void RebuildChestByChest()
    {
        if (_world == null) return;
        int sx = _world.Header.SpawnX, sy = _world.Header.SpawnY;
        double Dist(WldChest c) => Math.Sqrt(Math.Pow(c.X - sx, 2) + Math.Pow(c.Y - sy, 2));
        foreach (var chest in _world.Chests.OrderBy(Dist))
        {
            var tile = _world.Tiles[chest.X, chest.Y];
            string variantName = _tileNames.TileVariantName(tile.Type, tile.U, tile.V);
            string? iconPath = TileIconResolver.GetIconPath(tile.Type, tile.U, tile.V);
            // Casillas vacias reales de un cofre parcialmente lleno tienen NetId=0 - se
            // descartan, mismo criterio que "Por lo que contienen" (ChestItemCounts solo cuenta
            // objetos reales).
            var items = chest.Items.Where(it => it.NetId != 0).Select(it =>
            {
                string? prefixName = it.Prefix != 0 ? _prefixNames.ById(it.Prefix)?.Es ?? _prefixNames.ById(it.Prefix)?.En : null;
                return new ChestContentItemViewModel(it.NetId, _itemNames.GetName(it.NetId), it.Stack, prefixName, VanillaIconResolver.GetIconPath(it.NetId));
            }).ToList();
            ChestRows.Add(new ChestRowViewModel(variantName, chest.Name, chest.X, chest.Y, iconPath, items));
        }
    }

    // C-06: pulsar la fila del cofre navega a su posicion (mismo NavigateToTile de siempre) Y
    // despliega/repliega su contenido - un unico gesto hace las dos cosas, coherente con "ya
    // estas mirando este cofre" en las dos mitades del resultado.
    [RelayCommand]
    private void GoToChest(ChestRowViewModel chest)
    {
        chest.IsExpanded = !chest.IsExpanded;
        NavigateToTile(chest.TileX, chest.TileY);
    }

    // Clic simple sobre una fila de inventario - busca SOLO esa (el caso comun no debe costar
    // dos gestos, ESPEC-ui-exploracion.md#9.3-C). Cofres/Objetos usan TileTypes a secas salvo
    // cuando la fila representa una VARIANTE real (U/V != 0 o el propio Type no es generico -
    // en la practica, cualquier fila de "Cofres/Por tipo" con U/V reales usa SpriteVariants
    // para no traer TODOS los cofres del mismo Type).
    // Hallazgo real (feedback directo del usuario, la misma captura que ya mostraba "Mineral de
    // hierro (1, 844)/(1, 845)/(1, 846)..." en fila): pulsar el NOMBRE de una fila de Minerales/
    // Objetos (Tiles/Paredes/Liquidos) sin marcar ningun tick seguia yendo por
    // RunWorldSearchAsyncWithQuery -> WorldSearch.Run, el mismo barrido x->y con tope de 1000
    // (WorldSearchQuery.DisplayLimit) que el propio informe ya diagnostico como causa real de E6
    // ("cubre solo el borde izquierdo del mundo, no es ningun filtro de sprite") - C-04 solo lo
    // corrigio para "Marcar en el mapa" (ApplyTileHighlightAsync), nunca para el clic simple. Un
    // mineral comun agota los 1000 resultados dentro de las primeras columnas del mundo, asi que
    // la lista entera (y cualquier fila que se pulse despues para navegar) cae siempre cerca de
    // x=0 - el "monton de cuadraditos al final del mundo a la izquierda" que describe el clic en
    // la primera veta. Los tres helpers de abajo ya agrupan por veta con OreVeinFinder (limite
    // real de posiciones, no de tiles sueltos) para "Marcar en el mapa" - reutilizados aqui SIN
    // pintar el resaltado (paint:false, no hace falta regenerar el bitmap de 80,6MB para consultar
    // una sola fila) resuelve el sesgo en la RAIZ para cualquier consumidor, no solo el boton.
    [RelayCommand]
    private void SearchInventoryRow(WorldInventoryRowViewModel row)
    {
        switch (SelectedCategory)
        {
            case WorldSearchCategory.Ores:
                _ = ApplyTileHighlightAsync(new HashSet<int> { row.Id }, LocalizationService.Instance["unit_vein"], paint: false);
                break;
            case WorldSearchCategory.Objects when ObjectsViewMode == 0:
                _ = ApplyTileHighlightAsync(new HashSet<int> { row.Id }, LocalizationService.Instance["unit_group"], paint: false);
                break;
            case WorldSearchCategory.Objects when ObjectsViewMode == 1:
                _ = ApplyWallHighlightAsync(new HashSet<int> { row.Id }, paint: false);
                break;
            case WorldSearchCategory.Objects:
                _ = ApplyLiquidHighlightAsync(new HashSet<byte> { (byte)row.Id }, paint: false);
                break;
            // Cofres: SpriteVariants/ChestItemIds no son un "tipo de tile" que OreVeinFinder sepa
            // agrupar (una variante de sprite concreta, o "que hay dentro" - ni siquiera es un
            // barrido de tiles en el segundo caso) - mismo camino de siempre, sin el sesgo real de
            // arriba: un mundo real tiene cientos de cofres, muy por debajo del tope de 1000.
            default:
                _ = RunWorldSearchAsyncWithQuery(BuildSingleRowQuery(row));
                break;
        }
    }

    // C-03 (informe de pulido final, cierra E5): la fuente de filas incluye tambien los 3 grupos
    // de Minerales (union de las 4 colecciones), y el switch gana la rama Ores - antes Minerales
    // era la unica categoria de la barra lateral donde marcar+pulsar no hacia nada de verdad.
    [RelayCommand]
    private void SearchCheckedInventory()
    {
        var marcadas = Inventory.Concat(OreMetals).Concat(OreGems).Concat(OreTargets).Where(r => r.IsChecked).ToList();
        if (marcadas.Count == 0) return;
        switch (SelectedCategory)
        {
            case WorldSearchCategory.Chests when ChestViewMode == 0:
                _ = RunWorldSearchAsyncWithQuery(new WorldSearchQuery { SpriteVariants = marcadas.Select(r => (r.Id, r.U, r.V)).ToHashSet() });
                break;
            case WorldSearchCategory.Chests:
                _ = RunWorldSearchAsyncWithQuery(new WorldSearchQuery { ChestItemIds = marcadas.Select(r => r.Id).ToHashSet() });
                break;
            // Mismo motivo que SearchInventoryRow de arriba: agrupado por veta/grupo real, sin
            // pintar el resaltado (paint:false) - "Buscar seleccionados" es una lista, no un mapa.
            case WorldSearchCategory.Ores:
                _ = ApplyTileHighlightAsync(marcadas.Select(r => r.Id).ToHashSet(), LocalizationService.Instance["unit_vein"], paint: false);
                break;
            case WorldSearchCategory.Objects when ObjectsViewMode == 0:
                _ = ApplyTileHighlightAsync(marcadas.Select(r => r.Id).ToHashSet(), LocalizationService.Instance["unit_group"], paint: false);
                break;
            case WorldSearchCategory.Objects when ObjectsViewMode == 1:
                _ = ApplyWallHighlightAsync(marcadas.Select(r => r.Id).ToHashSet(), paint: false);
                break;
            case WorldSearchCategory.Objects:
                _ = ApplyLiquidHighlightAsync(marcadas.Select(r => (byte)r.Id).ToHashSet(), paint: false);
                break;
        }
    }

    private WorldSearchQuery BuildSingleRowQuery(WorldInventoryRowViewModel row) => SelectedCategory switch
    {
        WorldSearchCategory.Chests when ChestViewMode == 0 => new WorldSearchQuery { SpriteVariants = new HashSet<(int, short, short)> { (row.Id, row.U, row.V) } },
        WorldSearchCategory.Chests => new WorldSearchQuery { ChestItemIds = new HashSet<int> { row.Id } },
        _ => new WorldSearchQuery(),
    };

    // Punto 4 (Minerales - ESPEC-ui-exploracion.md#11.3/11.4): "Marcar en el mapa" activa la
    // capa de resaltado (sin tope, TODAS las posiciones - mismo reparto real que hace TEdit,
    // resaltado sin tope + lista topada) y rellena WorldSearchResults con las VETAS (no los
    // tiles sueltos, serian decenas de miles). El tope de la LISTA sigue siendo 1000
    // (WorldSearchQuery.DisplayLimit); el resumen dice la verdad completa (ver
    // RunWorldSearchAsyncWithQuery).
    [RelayCommand]
    private async Task MarkOresOnMap() =>
        await ApplyTileHighlightAsync(OreMetals.Concat(OreGems).Concat(OreTargets).Where(r => r.IsChecked).Select(r => r.Id).ToHashSet(), LocalizationService.Instance["unit_vein"]);

    // C-04 (informe de pulido final, cierra E6/E7): generalizacion del boton de arriba a
    // "Objetos" - misma arquitectura (resaltado sin tope + lista agrupada topada), segun cual de
    // las 3 vistas (Tiles/Paredes/Liquidos) este activa. "grupo" en vez de "veta" en el resumen
    // porque aqui no siempre es mineral (una veta de agua no es una expresion natural).
    [RelayCommand]
    private async Task MarkObjectsOnMap()
    {
        var marcadas = Inventory.Where(r => r.IsChecked).ToList();
        switch (ObjectsViewMode)
        {
            case 0: await ApplyTileHighlightAsync(marcadas.Select(r => r.Id).ToHashSet(), LocalizationService.Instance["unit_group"]); break;
            case 1: await ApplyWallHighlightAsync(marcadas.Select(r => r.Id).ToHashSet()); break;
            default: await ApplyLiquidHighlightAsync(marcadas.Select(r => (byte)r.Id).ToHashSet()); break;
        }
    }

    [RelayCommand]
    private void ClearOreMarks()
    {
        WorldHighlight = null;
        WorldSearchResults.Clear();
        _lastWorldSearchRows = [];
        _worldSearchCurrentIndex = -1;
        WorldSearchSummary = string.Empty;
    }

    // C-04: el tick de CUALQUIER fila de Minerales/Objetos pasa a significar "muestralo en el
    // mapa" y actua solo, con debounce (mismo umbral de 250ms que _worldSearchDebounceTimer) -
    // los botones "Marcar en el mapa"/"Quitar marcas" se quedan como "aplicar ya"/"desmarcar
    // todo". Regenerar la capa de resaltado en CADA tick individual seria carisimo (80,6 MB por
    // repintado en un mundo Grande, WorldHighlightRenderer) si el usuario marca varias filas
    // seguidas - el debounce agrupa la rafaga en un unico repintado.
    private readonly DispatcherTimer _highlightDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private void OnHighlightCheckToggled()
    {
        _highlightDebounceTimer.Stop();
        _highlightDebounceTimer.Start();
    }

    // Hallazgo real (feedback directo del usuario): "paint" separa las dos mitades que antes
    // siempre iban juntas - el agrupado por veta (OreVeinFinder, barato) de la capa de resaltado
    // (WorldHighlightRenderer, 80,6MB por repintado en un mundo Grande). SearchInventoryRow/
    // SearchCheckedInventory de arriba solo quieren la lista agrupada (paint:false, nunca tocan
    // WorldHighlight ni cuentan como "Marcar en el mapa" - ni pintan ni avisan de legibilidad,
    // eso solo tiene sentido cuando de verdad se va a pintar el mapa entero).
    private async Task ApplyTileHighlightAsync(IReadOnlySet<int> tileIds, string unitLabel, bool paint = true)
    {
        if (_world == null) return;
        if (tileIds.Count == 0) { ClearOreMarks(); return; }
        var world = _world;
        var cts = new CancellationTokenSource();
        _worldSearchCts?.Cancel();
        _worldSearchCts = cts;
        int myGeneration = ++_worldSearchGeneration;
        try
        {
            var (highlight, vetas, total) = await Task.Run(() =>
            {
                var img = paint ? WorldHighlightRenderer.Render(world, tileIds, Colors.Orange, cts.Token) : null;
                var v = OreVeinFinder.Find(world, tileIds, limit: 1000, out int t, cts.Token);
                return (img, v, t);
            }, cts.Token);
            if (myGeneration != _worldSearchGeneration) return;
            var rows = vetas.Select(vein => new WorldSearchHitRowViewModel(
                new WorldSearchHit(vein.CenterX, vein.CenterY, $"{_tileNames.TileName(vein.Type)} ({vein.TileCount:N0} tiles)", WorldSearchKind.OreVein))).ToList();
            if (paint)
            {
                long cubiertos = tileIds.Sum(id => (long)(_presence?.TileCounts.GetValueOrDefault(id) ?? 0));
                ApplyHighlightResult(highlight!, rows, vetas.Count, total, unitLabel, LegibilityWarning(cubiertos));
            }
            else ApplyGroupedSearchResult(rows, vetas.Count, total, unitLabel);
        }
        catch (OperationCanceledException) { }
    }

    // C-04: gemela de ApplyTileHighlightAsync, agrupando por Wall (WorldHighlightRenderer.
    // RenderWalls + OreVeinFinder.FindWalls) - generalizacion a "Objetos > Paredes".
    private async Task ApplyWallHighlightAsync(IReadOnlySet<int> wallIds, bool paint = true)
    {
        if (_world == null) return;
        if (wallIds.Count == 0) { ClearOreMarks(); return; }
        var world = _world;
        var cts = new CancellationTokenSource();
        _worldSearchCts?.Cancel();
        _worldSearchCts = cts;
        int myGeneration = ++_worldSearchGeneration;
        try
        {
            var (highlight, grupos, total) = await Task.Run(() =>
            {
                var img = paint ? WorldHighlightRenderer.RenderWalls(world, wallIds, Colors.Orange, cts.Token) : null;
                var v = OreVeinFinder.FindWalls(world, wallIds, limit: 1000, out int t, cts.Token);
                return (img, v, t);
            }, cts.Token);
            if (myGeneration != _worldSearchGeneration) return;
            var rows = grupos.Select(g => new WorldSearchHitRowViewModel(
                new WorldSearchHit(g.CenterX, g.CenterY, $"{_tileNames.WallName(g.Type)} ({g.TileCount:N0} tiles)", WorldSearchKind.OreVein))).ToList();
            if (paint)
            {
                long cubiertos = wallIds.Sum(id => (long)(_presence?.WallCounts.GetValueOrDefault(id) ?? 0));
                ApplyHighlightResult(highlight!, rows, grupos.Count, total, LocalizationService.Instance["unit_group"], LegibilityWarning(cubiertos));
            }
            else ApplyGroupedSearchResult(rows, grupos.Count, total, LocalizationService.Instance["unit_group"]);
        }
        catch (OperationCanceledException) { }
    }

    // C-04: gemela de ApplyTileHighlightAsync, agrupando por LiquidType (WorldHighlightRenderer.
    // RenderLiquids + OreVeinFinder.FindLiquids) - generalizacion a "Objetos > Liquidos".
    private async Task ApplyLiquidHighlightAsync(IReadOnlySet<byte> liquidTypes, bool paint = true)
    {
        if (_world == null) return;
        if (liquidTypes.Count == 0) { ClearOreMarks(); return; }
        var world = _world;
        var cts = new CancellationTokenSource();
        _worldSearchCts?.Cancel();
        _worldSearchCts = cts;
        int myGeneration = ++_worldSearchGeneration;
        try
        {
            var (highlight, grupos, total) = await Task.Run(() =>
            {
                var img = paint ? WorldHighlightRenderer.RenderLiquids(world, liquidTypes, Colors.Orange, cts.Token) : null;
                var v = OreVeinFinder.FindLiquids(world, liquidTypes, limit: 1000, out int t, cts.Token);
                return (img, v, t);
            }, cts.Token);
            if (myGeneration != _worldSearchGeneration) return;
            var rows = grupos.Select(g => new WorldSearchHitRowViewModel(
                new WorldSearchHit(g.CenterX, g.CenterY, $"{WorldSearch.LiquidName((byte)g.Type)} ({g.TileCount:N0} tiles)", WorldSearchKind.OreVein))).ToList();
            if (paint)
            {
                long cubiertos = liquidTypes.Sum(id => (long)(_presence?.LiquidCounts.GetValueOrDefault(id) ?? 0));
                ApplyHighlightResult(highlight!, rows, grupos.Count, total, LocalizationService.Instance["unit_group"], LegibilityWarning(cubiertos));
            }
            else ApplyGroupedSearchResult(rows, grupos.Count, total, LocalizationService.Instance["unit_group"]);
        }
        catch (OperationCanceledException) { }
    }

    private void ApplyGroupedSearchResult(List<WorldSearchHitRowViewModel> rows, int shown, int total, string unitLabel)
    {
        _lastWorldSearchRows = rows;
        _worldSearchCurrentIndex = -1;
        ApplyWorldSearchOrder();
        WorldSearchSummary = total > shown
            ? LocalizationService.Instance.Format("summary_of_total_capped", shown, total, unitLabel)
            : LocalizationService.Instance.Format("summary_total_unit", total, unitLabel);
    }

    private void ApplyHighlightResult(WriteableBitmap highlight, List<WorldSearchHitRowViewModel> rows, int shown, int total, string unitLabel, string? warning)
    {
        WorldHighlight = highlight;
        ApplyGroupedSearchResult(rows, shown, total, unitLabel);
        string resumen = total > shown
            ? LocalizationService.Instance.Format("summary_of_total_capped_map_all", shown, total, unitLabel)
            : LocalizationService.Instance.Format("summary_total_unit", total, unitLabel);
        WorldSearchSummary = warning is null ? resumen : $"{warning} {resumen}";
    }

    // C-04: "marcar Piedra o Tierra tiñe el 60% del mapa y no informa de nada" (evaluacion del
    // usuario sobre E7) - avisar (no impedir, el propio informe pide "avisar") cuando la
    // seleccion cubre una fraccion grande del mundo. 40% es un umbral razonado, no medido: por
    // debajo, incluso un mineral comun como el cobre (mundo Grande real, ~68.700 de 20.160.000
    // tiles = 0.3%) queda muy lejos de disparar el aviso.
    private string? LegibilityWarning(long tilesCubiertos)
    {
        if (_world == null) return null;
        long totalTiles = (long)_world.Header.TilesWide * _world.Header.TilesHigh;
        if (totalTiles <= 0) return null;
        double frac = (double)tilesCubiertos / totalTiles;
        return frac >= 0.4 ? LocalizationService.Instance.Format("warning_covers_map_fraction", frac.ToString("P0")) : null;
    }

    // F-12 (auditoria de Opus vs TEdit, E-13): "Terrakeep genera un WriteableBitmap completo
    // del mundo y lo congela - un PngBitmapEncoder sobre el son ~8 lineas. No existe." El
    // dialogo real vive en el code-behind (mismo criterio ya establecido, SaveItemSetDialog);
    // aqui solo la composicion+codificacion. Si hay resaltado de mineral activo (WorldHighlight,
    // "Marcar en el mapa") se mezcla ENCIMA del mapa base - exportar justo lo que se esta viendo,
    // no solo el mapa desnudo.
    public void ExportMapToPng(string path)
    {
        var mapa = WorldImage;
        if (mapa == null) return;
        BitmapSource final = mapa;
        if (WorldHighlight is { } resaltado)
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawImage(mapa, new System.Windows.Rect(0, 0, mapa.PixelWidth, mapa.PixelHeight));
                dc.DrawImage(resaltado, new System.Windows.Rect(0, 0, mapa.PixelWidth, mapa.PixelHeight));
            }
            var compuesto = new RenderTargetBitmap(mapa.PixelWidth, mapa.PixelHeight, mapa.DpiX, mapa.DpiY, PixelFormats.Pbgra32);
            compuesto.Render(visual);
            final = compuesto;
        }
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(final));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    [RelayCommand]
    private void GoToWorldSearchHit(WorldSearchHitRowViewModel hit)
    {
        _worldSearchCurrentIndex = WorldSearchResults.IndexOf(hit);
        UpdateCurrentWorldSearchHighlight();
        NavigateToTile(hit.TileX, hit.TileY);
    }

    [RelayCommand]
    private void NextWorldSearchResult() => MoveWorldSearchResult(+1);
    [RelayCommand]
    private void PreviousWorldSearchResult() => MoveWorldSearchResult(-1);

    // Circular con modulo (mismo criterio real que NavigateNext/NavigatePrevious de TEdit) -
    // desde "sin nada seleccionado" (-1), Siguiente va al primero y Anterior al ultimo.
    private void MoveWorldSearchResult(int delta)
    {
        if (WorldSearchResults.Count == 0) return;
        _worldSearchCurrentIndex = ((_worldSearchCurrentIndex + delta) % WorldSearchResults.Count + WorldSearchResults.Count) % WorldSearchResults.Count;
        UpdateCurrentWorldSearchHighlight();
        var row = WorldSearchResults[_worldSearchCurrentIndex];
        // TEdit: "Default false - just pan, don't zoom" - NavigateToTile ya solo desplaza el
        // ScrollViewer (ver MainWindow.xaml.cs, OnNavigateToTile), nunca toca Zoom, asi que el
        // comportamiento por defecto real ya coincide sin necesidad de ningun flag extra.
        NavigateToTile(row.TileX, row.TileY);
    }

    private void UpdateCurrentWorldSearchHighlight()
    {
        for (int i = 0; i < WorldSearchResults.Count; i++) WorldSearchResults[i].IsCurrent = i == _worldSearchCurrentIndex;
    }

    // Reordena (o no) WorldSearchResults a partir de _lastWorldSearchRows segun
    // ShowSpawnDistance - nunca vuelve a recorrer el mundo. Conserva el resultado "actual" a
    // traves del reordenado (si estaba resaltado antes de activar la distancia, lo sigue
    // estando despues, aunque haya cambiado de indice).
    private void ApplyWorldSearchOrder()
    {
        var currentRow = _worldSearchCurrentIndex >= 0 && _worldSearchCurrentIndex < WorldSearchResults.Count
            ? WorldSearchResults[_worldSearchCurrentIndex] : null;

        IEnumerable<WorldSearchHitRowViewModel> ordered = _lastWorldSearchRows;
        if (ShowSpawnDistance && _world != null)
        {
            int sx = _world.Header.SpawnX, sy = _world.Header.SpawnY;
            double Dist(WorldSearchHitRowViewModel r) => Math.Sqrt(Math.Pow(r.TileX - sx, 2) + Math.Pow(r.TileY - sy, 2));
            foreach (var row in _lastWorldSearchRows) row.DistanceLabel = $"{Math.Round(Dist(row))} tiles del spawn";
            ordered = _lastWorldSearchRows.OrderBy(Dist);
        }
        else
        {
            foreach (var row in _lastWorldSearchRows) row.DistanceLabel = null;
        }

        WorldSearchResults.Clear();
        foreach (var row in ordered) WorldSearchResults.Add(row);
        _worldSearchCurrentIndex = currentRow != null ? WorldSearchResults.IndexOf(currentRow) : -1;
        UpdateCurrentWorldSearchHighlight();
    }

    // Llamado por MainViewModel al cargar personaje (tras Servers.LoadFrom, que es quien de
    // verdad rellena los Spawn Points reales) y al entrar en esta pestaña (mismo criterio ya
    // establecido en Bd-d/BuildsViewModel.RefreshOwnership: foto fija recalculada cuando de
    // verdad hace falta, no en cada tecla de una edicion en Spawn Points). Limitacion real
    // conocida: estas coordenadas no se validan contra NINGUN mundo en concreto (un spawn
    // guardado para un mundo distinto al cargado aqui se vera en un sitio sin sentido) - mismo
    // criterio ya aceptado para "Spawn Points" en si, que tampoco sabe a que mundo pertenece
    // cada uno.
    public void SetCharacterSpawns(IEnumerable<(string Label, int X, int Y)> spawns)
    {
        CharacterSpawns.Clear();
        foreach (var (label, x, y) in spawns) CharacterSpawns.Add(new CharacterSpawnRowViewModel(label, x, y));
    }

    // El code-behind (unico sitio que conoce el ScrollViewer real del mapa) se suscribe a esto
    // para centrar la vista - la ViewModel no puede tocar controles de UI directamente.
    public event Action<int, int>? NavigateToTileRequested;

    [RelayCommand]
    private void GoToNpc(WorldNpcRowViewModel npc) => NavigateToTile(npc.TileX, npc.TileY);

    // S-c (segunda auditoria de Opus, Fable): "sin enlace al mapa de Exploracion desde Spawn
    // Points" - punto de entrada publico real para que MainViewModel (el unico sitio que
    // conoce ambas pestañas a la vez) pueda centrar el mapa en un Spawn Point real sin
    // depender de un comando pensado solo para NPCs.
    public void NavigateToTile(int x, int y) => NavigateToTileRequested?.Invoke(x, y);

    // H4-08 (cuarta auditoria de Opus, Fable): "la version buena - un lanzador de mundos
    // calcado del de personajes de Inicio". Mismo patron real que HomeViewModel/I-1: escanea
    // UNA VEZ al arrancar (y bajo demanda con "Actualizar") la carpeta real de mundos de
    // tModLoader, con solo la lectura BARATA de cabecera (WldReader.ReadHeader) - nunca decodifica
    // tiles/NPCs, eso solo pasa al elegir uno de verdad (LoadFromPathAsync). Un mundo ajeno/
    // corrupto no debe tumbar el listado de los demas, mismo criterio ya establecido en
    // HomeViewModel.ScanCharacters.
    public ObservableCollection<WorldListEntryViewModel> Worlds { get; } = [];
    [ObservableProperty] private bool _isScanningWorlds;
    [ObservableProperty] private string? _scanMessage;

    // H5-11 (quinta auditoria de Opus): "el lanzador de mundos desaparece para siempre en
    // cuanto cargas uno... su gemelo de Inicio no hace esto, sigue ahi siempre, con el cargado
    // resaltado". Mismo campo/patron real que HomeViewModel._currentPath.
    private string? _currentWorldPath;

    // F-11 (auditoria de Opus vs TEdit, E-11): offset de scroll pendiente de restaurar tras
    // cargar un mundo - null = sin vista guardada (el code-behind hace "Ajustar a la ventana"
    // en su lugar). El ScrollViewer no vive aqui, asi que el code-behind consume esto con
    // TryConsumePendingViewRestore justo despues de LoadFromPathAsync.
    private double? _pendingRestoreOffsetH;
    private double? _pendingRestoreOffsetV;

    public bool TryConsumePendingViewRestore(out double offsetH, out double offsetV)
    {
        if (_pendingRestoreOffsetH is double h && _pendingRestoreOffsetV is double v)
        {
            offsetH = h; offsetV = v;
            _pendingRestoreOffsetH = null;
            _pendingRestoreOffsetV = null;
            return true;
        }
        offsetH = offsetV = 0;
        return false;
    }

    // F-11: llamado desde el code-behind (unico sitio que conoce los offsets reales del
    // ScrollViewer) antes de cargar OTRO mundo y al cerrar la ventana.
    public void SaveCurrentViewState(double offsetH, double offsetV)
    {
        if (_currentWorldPath == null) return;
        var estados = WorldViewStateService.Load();
        estados[_currentWorldPath] = new WorldViewState(Zoom, offsetH, offsetV);
        WorldViewStateService.Save(estados);
    }

    public ExplorationViewModel(CharacterFileService service)
    {
        _npcNames = service.NpcNames;
        _mapColors = service.MapColors;
        _tileNames = service.TileNames;
        _itemNames = service.VanillaCatalog;
        _prefixNames = service.VanillaPrefixCatalog;
        // Fire-and-forget deliberado, mismo criterio real que HomeViewModel - el constructor no
        // puede ser async, y no hay nada que esperar aqui (Worlds se rellena un instante
        // despues, IsScanningWorlds refleja el hueco mientras tanto).
        _ = RefreshWorldsAsync();

        // Mismo patron real ya establecido en el proyecto (AppearanceViewModel.
        // _hairOptionsDebounceTimer): un mundo grande son millones de tiles - no se relanza el
        // barrido en cada tecla, se espera a que el usuario pare de escribir 250ms.
        _worldSearchDebounceTimer.Tick += (_, _) =>
        {
            _worldSearchDebounceTimer.Stop();
            _ = RunWorldSearchAsync();
        };

        // C-04: mismo patron, para el resaltado en el mapa que dispara el tick de Minerales/
        // Objetos (ver WorldInventoryRowViewModel.CheckedChanged).
        _highlightDebounceTimer.Tick += (_, _) =>
        {
            _highlightDebounceTimer.Stop();
            _ = SelectedCategory == WorldSearchCategory.Ores ? MarkOresOnMap() : MarkObjectsOnMap();
        };
    }

    // H5-11: gemelo real de HomeViewModel.UpdateCurrentPath - se llama tras cargar un mundo con
    // exito (o fallar, IsWorldLoaded queda False y ninguna pildora se marca) y tras cada
    // Refresh() (la lista se reconstruye entera, IsCurrent no sobrevive).
    private void UpdateCurrentWorldPath(string? path)
    {
        _currentWorldPath = path;
        foreach (var entry in Worlds)
            entry.IsCurrent = string.Equals(entry.FilePath, path, StringComparison.OrdinalIgnoreCase);
    }

    // H5-07 (quinta auditoria de Opus): mismo bug real y mismo arreglo que
    // HomeViewModel._scanGeneration - MainWindow.xaml.cs relanza este escaneo tras aplicar las
    // carpetas adicionales de Ajustes, encima del escaneo automatico que ya dispara este mismo
    // constructor.
    private int _scanGeneration;

    [RelayCommand]
    private async Task RefreshWorldsAsync()
    {
        int myGeneration = ++_scanGeneration;
        Worlds.Clear();
        IsScanningWorlds = true;
        try
        {
            // Pedido explicito del usuario: mundos VANILLA (carpeta real "Documents\My Games\
            // Terraria\Worlds" sin el segmento "tModLoader") tambien cuentan, mismo criterio
            // real que HomeViewModel.RefreshAsync con los personajes.
            var dirs = CharacterFileService.GetAllWorldsDirectories();
            var scanned = await Task.Run(() => ScanWorlds(dirs));
            if (myGeneration != _scanGeneration) return; // una vuelta MAS NUEVA ya esta en marcha - esta es obsoleta
            foreach (var entry in scanned) Worlds.Add(entry);
            ScanMessage = Worlds.Count == 0
                ? dirs.Count == 0
                    ? LocalizationService.Instance["scan_no_worlds_folder"]
                    : LocalizationService.Instance.Format("scan_no_worlds_in", string.Join(LocalizationService.Instance["scan_nor_in"], dirs))
                : null;
            UpdateCurrentWorldPath(_currentWorldPath); // la lista es nueva de cero, IsCurrent hay que recalcularlo
        }
        finally
        {
            if (myGeneration == _scanGeneration) IsScanningWorlds = false;
        }
    }

    // H5-11: "ya que la lista es permanente, la tarjeta de mundo gana el menu contextual que su
    // gemela de personaje ya tiene ('Abrir carpeta'), hoy ausente por alcance" - mismo gesto
    // real que HomeViewModel.OpenFolder.
    [RelayCommand]
    private void OpenFolder(WorldListEntryViewModel entry)
    {
        try
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{entry.FilePath}\"");
        }
        catch (Exception ex)
        {
            StatusMessage = LocalizationService.Instance.Format("error_open_folder", ex.Message);
        }
    }

    // Todo el trabajo real de disco (enumerar + leer la cabecera de cada .wld) - se ejecuta en
    // un hilo de fondo via Task.Run (RefreshWorldsAsync de arriba), nunca toca Worlds
    // directamente (seria una modificacion desde fuera del hilo de UI).
    private static List<WorldListEntryViewModel> ScanWorlds(IEnumerable<string> dirs)
    {
        var result = new List<WorldListEntryViewModel>();
        var wldFiles = dirs.SelectMany(dir => Directory.GetFiles(dir, "*.wld"));
        foreach (string path in wldFiles.OrderByDescending(File.GetLastWriteTimeUtc))
        {
            try
            {
                var header = WldReader.ReadHeader(File.ReadAllBytes(path));
                result.Add(new WorldListEntryViewModel(path, header.Title, header.TilesWide, header.TilesHigh, File.GetLastWriteTimeUtc(path)));
            }
            catch (Exception)
            {
                // Un .wld ajeno/corrupto no debe tumbar el listado de los demas - se omite en
                // silencio, igual que ya hace HomeViewModel.ScanCharacters con los .plr.
            }
        }
        return result;
    }

    // Llamado desde el code-behind con la posicion del raton YA en espacio de tile (pixel
    // nativo del bitmap = 1 tile, ver WorldRenderer) - actualiza el texto informativo de
    // "tooltip" (tile/pared/coordenadas) bajo el mapa. Fuera de rango o sin mundo cargado
    // limpia el texto en vez de mostrar basura.
    public void UpdateHover(int tileX, int tileY)
    {
        if (_world == null || tileX < 0 || tileY < 0 || tileX >= _world.Header.TilesWide || tileY >= _world.Header.TilesHigh)
        {
            HoverInfo = string.Empty;
            HoverCoordText = HoverLayerText = HoverDepthText = HoverTileText = HoverWallText = HoverLiquidText = "—";
            HoverLayerColor = Colors.Transparent;
            return;
        }

        var tile = _world.Tiles[tileX, tileY];
        // Bug real corregido (2-sep-2026, reportado: "pone que esta vacio" sobre agua/lava
        // real): un tile de liquido puro (charco/lago/lava) tiene IsActive=false (no hay
        // bloque solido) pero LiquidAmount>0 - antes esto caia siempre en "(vacio)" sin mirar
        // el liquido. Los codigos reales (1=Agua/2=Lava/3=Miel/4=Shimmer, este ultimo
        // sintetico - ver WldReader.cs) son los mismos que ya corrige WorldRenderer.
        // LiquidColor, confirmados contra TEdit real.
        string tileText = tile.IsActive
            ? _tileNames.TileVariantName(tile.Type, tile.U, tile.V)
            : tile.LiquidAmount > 0
                ? LiquidName(tile.LiquidType)
                : "(vacio)";
        string wallText = _tileNames.WallName(tile.Wall);
        HoverInfo = string.IsNullOrEmpty(wallText)
            ? $"({tileX}, {tileY}) - {tileText}"
            : $"({tileX}, {tileY}) - {tileText} / pared: {wallText}";

        // F-6 (auditoria de Opus vs TEdit, E-07/E-08): campos reales para la franja de estado
        // fija de P-1 - id entre corchetes (como TEdit) y liquido SIEMPRE nombrado con su
        // cantidad (antes de este arreglo, E-08: un bloque bajo el agua/lava/miel no lo
        // mencionaba porque IsActive==true saltaba directo a la rama de tile, el mismo bug ya
        // corregido en su forma simetrica el 2-sep-2026 - ver el comentario justo arriba).
        HoverCoordText = $"({tileX}, {tileY})";
        HoverTileText = tile.IsActive ? $"{_tileNames.TileVariantName(tile.Type, tile.U, tile.V)} [{tile.Type}]" : "—";
        HoverWallText = string.IsNullOrEmpty(wallText) ? "—" : $"{wallText} [{tile.Wall}]";
        HoverLiquidText = tile.LiquidAmount > 0 ? $"{LiquidName(tile.LiquidType)} ({tile.LiquidAmount}/255)" : "—";

        // Formula GPS real del propio juego (TEdit UI/MouseTile.cs:115-153, comentario literal
        // "Updates depth display text using Terraria's in-game GPS formulas") - DELIBERADAMENTE
        // NO se reutiliza WldHeader.ZoneFor (usa umbrales distintos, pensados para el fondo del
        // mapa, no para el HUD de profundidad que el jugador ve en el juego real).
        double groundLevel = _world.Header.GroundLevel;
        double pies = tileY * 2 - groundLevel * 2;
        double spaceCheck = (tileY - (65 + 10 * Math.Pow(_world.Header.TilesWide / 4200.0, 2))) / (groundLevel / 5.0);
        string zonaColor;
        if (tileY > _world.Header.TilesHigh - 204) { HoverLayerText = LocalizationService.Instance["zone_underworld"]; zonaColor = "Hell"; }
        else if (tileY > _world.Header.RockLevel) { HoverLayerText = LocalizationService.Instance["zone_caverns"]; zonaColor = "Rock"; }
        else if (pies > 0) { HoverLayerText = LocalizationService.Instance["zone_underground"]; zonaColor = "Earth"; }
        else if (spaceCheck < 1.0) { HoverLayerText = LocalizationService.Instance["zone_space"]; zonaColor = "Space"; }
        else { HoverLayerText = LocalizationService.Instance["zone_surface"]; zonaColor = "Sky"; }
        var c = _mapColors.Global(zonaColor);
        HoverLayerColor = Color.FromArgb(c.A, c.R, c.G, c.B);

        int tilesRespectoSuelo = (int)Math.Round(tileY - groundLevel);
        HoverDepthText = tilesRespectoSuelo switch
        {
            > 0 => LocalizationService.Instance.Format("depth_below_ground", tilesRespectoSuelo.ToString("N0")),
            < 0 => LocalizationService.Instance.Format("depth_above_ground", (-tilesRespectoSuelo).ToString("N0")),
            _ => LocalizationService.Instance["depth_ground_level"],
        };
    }

    // H3-10 (tercera auditoria de Opus, Fable): miel y Shimmer compartian el codigo 3 (mostraba
    // siempre "Miel" aunque fuera Shimmer de verdad) - "Centelleo" es el nombre real que usa la
    // propia localizacion es-ES oficial del juego para el LIQUIDO (Terraria.Localization.
    // Content.es-ES.Items.json, ShimmerCloak: "...Mantén Abajo para entrar en fase mientras
    // estás sumergido en el centelleo" - la traduccion oficial de "Shimmer" varia por objeto
    // (eter/fulgor/centelleo), pero esta es la unica que se refiere de verdad al liquido en el
    // que uno se sumerge, no a un objeto solido).
    private static string LiquidName(byte liquidType) => liquidType switch
    {
        2 => "Lava",
        3 => LocalizationService.Instance["liquid_honey"],
        4 => LocalizationService.Instance["liquid_shimmer"],
        _ => LocalizationService.Instance["liquid_water"],
    };

    // Auditoria de Opus, X-7/T-13: leer (RLE de hasta miles de tiles de ancho/alto) y pintar
    // (WriteableBitmap 1 pixel por tile) el mundo son los dos pasos reales caros (~1.4s medidos
    // en un mundo real de 11MB) - se mandan juntos a un hilo de fondo via Task.Run. Seguro
    // crear+pintar+Freeze() un WriteableBitmap fuera del hilo de UI (patron real de WPF, el
    // Freeze() final lo hace inmutable y compartible entre hilos) - el resto (listas de NPCs,
    // ObservableCollection) es barato de verdad (no midio nada perceptible) y se queda en el
    // hilo de UI de siempre, sin necesidad de marshalling manual.
    public async Task LoadFromPathAsync(string wldPath)
    {
        IsLoading = true;
        try
        {
            StatusMessage = LocalizationService.Instance["status_reading_painting_map"];
            // Punto 4 (advisor Opus, "que solo puedan salir los objetos que tiene ese mundo" -
            // ver ESPEC-ui-exploracion.md#10.3): el censo real del mundo se calcula AQUI, dentro
            // del mismo Task.Run que ya lee+pinta - el mundo ya esta caliente en cache justo en
            // este punto, el overlay de "Leyendo y pintando..." ya esta en pantalla (sin hueco
            // nuevo que tapar), y el sobrecoste medido es de decenas de milisegundos frente a los
            // ~1.4s que ya cuesta este paso completo.
            var (world, image, presence) = await Task.Run(() =>
            {
                var w = WldReader.Read(File.ReadAllBytes(wldPath));
                var img = WorldRenderer.Render(w, _mapColors);
                var idx = WorldPresenceIndex.Build(w);
                return (w, img, idx);
            });
            _world = world;
            _presence = presence;
            WorldImage = image;
            WorldHighlight = null; // un mundo nuevo invalida cualquier resaltado de mineral anterior

            _allNpcs = world.Npcs
                .OrderBy(n => _npcNames.GetName(n.Id))
                .Select(n =>
                {
                    // Punto 4 (advisor Opus): "bajo tierra" = TileY > GroundLevel real de este
                    // mundo (WldHeader.GroundLevel, ver ESPEC-ui-exploracion.md#12).
                    bool underground = n.TileY > world.Header.GroundLevel;
                    int depth = underground ? n.TileY - (int)world.Header.GroundLevel : 0;
                    return new WorldNpcRowViewModel(n.Id, _npcNames.GetName(n.Id), n.TileX, n.TileY, n.Homeless,
                        NpcHeadProfile.GetHeadIndex(n.Id, n.VariationIndex, world.ShimmeredNpcTypes.Contains(n.Id)),
                        underground, depth);
                })
                .ToList();
            Npcs.Clear();
            foreach (var npc in _allNpcs) Npcs.Add(npc);
            NpcSearchText = string.Empty;
            NpcFilterWithHome = false;
            NpcFilterHomeless = false;
            NpcFilterUnderground = false;
            // F-11 (auditoria de Opus vs TEdit, E-11): si hay una vista guardada de ESTE mundo
            // (misma ruta), se restaura Zoom aqui (el offset de scroll lo aplica el code-behind,
            // via TryConsumePendingViewRestore, porque el ScrollViewer no vive en la ViewModel).
            // Sin vista guardada: Zoom=1.0 de siempre, y el code-behind hace "Ajustar a la
            // ventana" en su lugar - la regla de sentido comun que el propio TEdit NO tiene: el
            // 100% en un mundo Grande enseña solo el 12% del ancho.
            if (WorldViewStateService.Load().TryGetValue(wldPath, out var vistaGuardada))
            {
                Zoom = vistaGuardada.Zoom;
                _pendingRestoreOffsetH = vistaGuardada.OffsetH;
                _pendingRestoreOffsetV = vistaGuardada.OffsetV;
            }
            else
            {
                Zoom = 1.0;
                _pendingRestoreOffsetH = null;
                _pendingRestoreOffsetV = null;
            }
            HoverInfo = string.Empty;
            ApplyNpcFilter();
            // Punto 4: un mundo nuevo invalida cualquier resultado de busqueda anterior (era de
            // OTRO mundo) - mismo criterio que el reinicio de NpcSearchText de arriba.
            WorldSearchText = string.Empty;
            WorldSearchResults.Clear();
            WorldSearchSummary = string.Empty;
            _lastWorldSearchRows = [];
            _worldSearchCurrentIndex = -1;
            SelectedCategory = WorldSearchCategory.All;
            ChestViewMode = 0;
            ObjectsViewMode = 0;
            RebuildInventory();

            var foundIds = world.Npcs.Select(n => n.Id).ToHashSet();
            MissingNpcs.Clear();
            foreach (int id in VanillaTownNpcRoster.Ids)
                if (!foundIds.Contains(id))
                    MissingNpcs.Add(new MissingNpcRowViewModel(id, _npcNames.GetName(id)));

            WorldTitle = world.Header.Title;
            WorldSizeText = $"{world.Header.TilesWide}×{world.Header.TilesHigh}";
            WorldSeedText = world.Header.Seed;
            WorldGameModeText = GameModeLabel(world.Header.GameMode);
            _savedWorldGameMode = world.Header.GameMode;
            WorldGameMode = world.Header.GameMode;
            WorldGameModeSaveStatus = null;
            WorldVersionText = world.Header.Version.ToString();
            WorldSpawnX = world.Header.SpawnX;
            WorldSpawnY = world.Header.SpawnY;
            WorldDungeonX = world.Header.DungeonX;
            WorldDungeonY = world.Header.DungeonY;
            IsWorldLoaded = true;
            StatusMessage = LocalizationService.Instance.Format("status_world_loaded_summary",
                world.Header.Title, world.Header.TilesWide, world.Header.TilesHigh, _allNpcs.Count, MissingNpcs.Count);
            OnPropertyChanged(nameof(ChestsPillCount));
            OnPropertyChanged(nameof(OresPillCount));
            OnPropertyChanged(nameof(ObjectsPillCount));
            OnPropertyChanged(nameof(WallTypesPresentCount));
            OnPropertyChanged(nameof(SignCount));
            OnPropertyChanged(nameof(WorldAirPercentText));
            UpdateCurrentWorldPath(wldPath);
            // IsWorldLoaded (de la que depende CanSaveWorldGameMode) cambia DESPUES de fijar
            // WorldGameMode arriba - reevaluar aqui explicitamente en vez de fiarse de que algun
            // evento de UI ambiental fuerce un requery de WPF.
            SaveWorldGameModeCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _world = null;
            _presence = null;
            WorldHighlight = null;
            WorldSizeText = "—";
            IsWorldLoaded = false;
            WorldGameModeSaveStatus = null;
            StatusMessage = LocalizationService.Instance.Format("error_reading_world", ex.Message);
            UpdateCurrentWorldPath(null); // un fallo real no debe dejar ninguna pildora marcada como "cargada"
            SaveWorldGameModeCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnNpcSearchTextChanged(string value) => ApplyNpcFilter();

    // Punto 4 (advisor Opus, "npcs escondidos en el subsuelo que puedas encontrarlos facilmente" -
    // ver ESPEC-ui-exploracion.md#9.3-B/#12): tres chips multiseleccion, combinados con OR entre
    // ellos y AND con el texto - "ningun chip pulsado" equivale a "todos", igual que el buscador
    // de texto vacio.
    [ObservableProperty] private bool _npcFilterWithHome;
    [ObservableProperty] private bool _npcFilterHomeless;
    [ObservableProperty] private bool _npcFilterUnderground;
    partial void OnNpcFilterWithHomeChanged(bool value) => ApplyNpcFilter();
    partial void OnNpcFilterHomelessChanged(bool value) => ApplyNpcFilter();
    partial void OnNpcFilterUndergroundChanged(bool value) => ApplyNpcFilter();

    // Bug real encontrado verificando X-a (segunda auditoria de Opus, Fable) con un mundo REAL
    // de 8400x2400 tiles ("Grande", el tamaño maximo real de Terraria): "Ajustar a la ventana"
    // calculaba el zoom real que hace falta para que quepa entero (ej. 9.4% con un viewport de
    // 787px), pero el suelo de 0.1 (10%) de antes lo recortaba hacia arriba, dejando el mundo
    // sin caber del todo pese a que el boton decia "ajustar". Confirmado con una captura real
    // (arnes) antes de tocar el numero. 0.02 sigue siendo suficiente para frenar un zoom-out
    // repetido con la rueda antes de llegar a una imagen imperceptible, y deja sitio real para
    // que un mundo Grande quepa incluso en una ventana bastante estrecha (viewport >= 168px).
    private const double MinZoom = 0.02, MaxZoom = 6.0;

    partial void OnZoomChanged(double value)
    {
        double clamped = Math.Clamp(value, MinZoom, MaxZoom);
        if (clamped != value) Zoom = clamped; // reentra, se estabiliza al segundo paso
    }

    // X-b (segunda auditoria de Opus, Fable): antes la rueda del raton daba pasos de x1.15 (ver
    // MainWindow.xaml.cs, OnWorldMapPreviewMouseWheel) mientras estos botones daban x1.25 - dos
    // velocidades de zoom distintas para la MISMA accion segun el metodo de entrada usado.
    // Constante real unica (ambos sitios la referencian ahora) para que no vuelvan a divergir.
    public const double ZoomStep = 1.25;

    [RelayCommand] private void ZoomIn() => Zoom *= ZoomStep;
    [RelayCommand] private void ZoomOut() => Zoom /= ZoomStep;
    [RelayCommand] private void ZoomReset() => Zoom = 1.0;

    // X-c: Npcs (el mapa) NUNCA se toca aqui - solo se marca IsMatch por NPC (resaltar, no
    // ocultar) y se reconstruye NpcSearchResults (la lista lateral) con solo los que coinciden.
    // Punto 4: + los tres chips de 9.3-B, y ordenacion por profundidad descendente (el mas
    // escondido primero) cuando el chip "Bajo tierra" esta activo - la busqueda literal de "se
    // me perdio un NPC bajo tierra" del encargo.
    private void ApplyNpcFilter()
    {
        bool sinBusqueda = string.IsNullOrWhiteSpace(NpcSearchText);
        bool sinChips = !NpcFilterWithHome && !NpcFilterHomeless && !NpcFilterUnderground;

        var coincidencias = new List<WorldNpcRowViewModel>();
        foreach (var npc in _allNpcs)
        {
            bool matchesChips = sinChips
                || (NpcFilterWithHome && !npc.Homeless)
                || (NpcFilterHomeless && npc.Homeless)
                || (NpcFilterUnderground && npc.IsUnderground);
            bool matchesText = sinBusqueda || npc.Name.Contains(NpcSearchText, StringComparison.OrdinalIgnoreCase);
            npc.IsMatch = matchesChips && matchesText;
            if (npc.IsMatch) coincidencias.Add(npc);
        }
        if (NpcFilterUnderground) coincidencias = coincidencias.OrderByDescending(n => n.DepthTiles).ToList();

        NpcSearchResults.Clear();
        foreach (var npc in coincidencias) NpcSearchResults.Add(npc);
    }

    // Punto 4 (advisor Opus, ESPEC-ui-exploracion.md#9.2/#14.3 punto 8): el MISMO cuadro de
    // texto cambia de comportamiento segun la categoria activa. En "NPCs" se limita a
    // sincronizar NpcSearchText (que ya dispara ApplyNpcFilter via su propio OnXxxChanged - el
    // mecanismo de NPCs no cambia, solo gana un cuadro de texto compartido). En Cofres/
    // Minerales/Objetos filtra el inventario YA EN MEMORIA, inmediato, sin debounce ni Task.Run
    // (es una lista de 260 filas como mucho). Solo en "Todo" se conserva el debounce de 250ms +
    // el barrido real de RunWorldSearchAsync (comportamiento identico al que ya habia).
    partial void OnWorldSearchTextChanged(string value)
    {
        if (SelectedCategory == WorldSearchCategory.Npcs)
        {
            NpcSearchText = value;
            return;
        }
        if (SelectedCategory != WorldSearchCategory.All)
        {
            ApplyInventoryFilter();
            return;
        }

        _worldSearchDebounceTimer.Stop();
        if (string.IsNullOrWhiteSpace(value))
        {
            // Vaciar el cuadro limpia al instante, sin esperar el debounce - mismo criterio que
            // vaciar cualquier otro buscador de la app.
            _worldSearchCts?.Cancel();
            _lastWorldSearchRows = [];
            _worldSearchCurrentIndex = -1;
            WorldSearchResults.Clear();
            WorldSearchSummary = string.Empty;
            return;
        }
        _worldSearchDebounceTimer.Start();
    }

    // Resuelve el texto libre a un WorldSearchQuery real usando LibrarySearchGrammar (la misma
    // gramatica ya usada por la Libreria de objetos/buffs - comas=OR, espacios=AND, "#123"/
    // "#100-200" por id) contra los tres catalogos de nombres que ya tiene esta ViewModel, mas
    // los 4 liquidos reales del juego (sin catalogo propio, tabla fija).
    // Propiedad (no "static readonly") a proposito: un campo estatico se evaluaria UNA vez, en
    // el primer uso real del tipo (con toda probabilidad en español, el idioma de arranque) y se
    // quedaria congelado en ese idioma para siempre - esto se llama en vivo desde una busqueda
    // interactiva, tiene que reflejar el idioma ACTUAL en cada pulsacion, no el de arranque.
    private static (int Id, string Name)[] LiquidCandidates =>
        [(1, LocalizationService.Instance["liquid_water"]), (2, "Lava"), (3, LocalizationService.Instance["liquid_honey"]), (4, LocalizationService.Instance["liquid_shimmer"])];

    private WorldSearchQuery BuildWorldSearchQuery(string text)
    {
        // Punto 4 (advisor Opus, "que entre todas las opciones solo puedan salir los objetos que
        // tiene ese mundo" - ver ESPEC-ui-exploracion.md#10.4): un tile/pared/NPC/objeto de cofre
        // que este mundo NUNCA genero no es candidato, aunque el catalogo completo del juego lo
        // conozca. Medido en mundos reales (ESPEC-ui-exploracion.md#6): entre el 65% y el 96% de
        // lo que se ofrecia antes de este cambio no existia en el mundo cargado. Sin mundo
        // cargado (_presence == null, no deberia pasar de verdad porque el cuadro esta
        // deshabilitado, pero por si acaso) se cae al catalogo completo en vez de no ofrecer nada.
        var tileTypes = new HashSet<int>();
        foreach (var (id, name) in _tileNames.AllTiles)
        {
            if (_presence != null && !_presence.HasTile(id)) continue;
            if (LibrarySearchGrammar.Matches(text, id, LibrarySearchGrammar.Fold(name), null)) tileTypes.Add(id);
        }

        var wallIds = new HashSet<int>();
        foreach (var (id, name) in _tileNames.AllWalls)
        {
            if (_presence != null && !_presence.HasWall(id)) continue;
            if (LibrarySearchGrammar.Matches(text, id, LibrarySearchGrammar.Fold(name), null)) wallIds.Add(id);
        }

        var npcIds = new HashSet<int>();
        foreach (var (id, name) in _npcNames.All)
        {
            if (_presence != null && !_presence.HasNpc(id)) continue;
            if (LibrarySearchGrammar.Matches(text, id, LibrarySearchGrammar.Fold(name), null)) npcIds.Add(id);
        }

        var liquidTypes = new HashSet<byte>();
        foreach (var (id, name) in LiquidCandidates)
        {
            if (_presence != null && !_presence.HasLiquid((byte)id)) continue;
            if (LibrarySearchGrammar.Matches(text, id, LibrarySearchGrammar.Fold(name), null)) liquidTypes.Add((byte)id);
        }

        // Fase 2 (ESPEC-buscador-mundo-tedit.md#5.2): objetos reales dentro de cofres, mismo
        // criterio de candidatos que tiles/paredes/NPCs (recorrer el catalogo entero, casar con
        // la gramatica real, ahora filtrado a lo realmente presente). Los NetId de Calamity
        // reales que un .wld pueda guardar no estan en VanillaItemCatalog - simplemente no se
        // ofrecen como candidato por NOMBRE (pero SI aparecen en el inventario real de la
        // categoria Cofres, que sale de _presence, no del catalogo - ver RebuildInventory),
        // nunca se inventa una coincidencia.
        var chestItemIds = new HashSet<int>();
        foreach (var (id, name) in _itemNames.AllEntries())
        {
            if (_presence != null && !_presence.HasChestItem(id)) continue;
            if (LibrarySearchGrammar.Matches(text, id, LibrarySearchGrammar.Fold(name), null)) chestItemIds.Add(id);
        }

        // Fase 2: los letreros son texto libre, sin catalogo de ids - se reutiliza la MISMA
        // gramatica (comas=OR, espacios=AND) tratando el texto de cada letrero como si fuera el
        // nombre de una unica entrada (id=0, sin sentido para un letrero, se ignora).
        bool SignPredicate(string signText) => LibrarySearchGrammar.Matches(text, 0, LibrarySearchGrammar.Fold(signText), null);

        return new WorldSearchQuery
        {
            TileTypes = tileTypes,
            WallIds = wallIds,
            NpcIds = npcIds,
            LiquidTypes = liquidTypes,
            ChestItemIds = chestItemIds,
            SignTextPredicate = SignPredicate,
        };
    }

    // El barrido real (WorldSearch.Run) puede recorrer millones de tiles en un mundo Grande -
    // se manda a un hilo de fondo (mismo criterio ya establecido por LoadFromPathAsync) y es
    // cancelable: si el usuario teclea otra vez antes de que termine, la vuelta vieja se
    // descarta en vez de pisar un resultado mas nuevo con uno obsoleto que llega tarde.
    private async Task RunWorldSearchAsync()
    {
        string text = WorldSearchText;
        if (_world == null || string.IsNullOrWhiteSpace(text)) return;
        await RunWorldSearchAsyncWithQuery(BuildWorldSearchQuery(text));
    }

    // Punto 4 (advisor Opus): nucleo compartido real entre el buscador de texto libre ("Todo")
    // y el clic sobre una fila de inventario (Cofres/Objetos, SearchInventoryRow/
    // SearchCheckedInventory) - las dos rutas terminan aqui, con la MISMA cancelacion por
    // generacion y el mismo formato de resumen, para que un resultado nunca dependa de por
    // donde se pidio.
    private async Task RunWorldSearchAsyncWithQuery(WorldSearchQuery query)
    {
        _worldSearchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _worldSearchCts = cts;
        int myGeneration = ++_worldSearchGeneration;

        var world = _world;
        if (world == null) return;

        if (query.IsEmpty)
        {
            if (myGeneration == _worldSearchGeneration)
            {
                _lastWorldSearchRows = [];
                _worldSearchCurrentIndex = -1;
                WorldSearchResults.Clear();
                WorldSearchSummary = LocalizationService.Instance["status_no_results"];
            }
            return;
        }

        // F-4 (auditoria de Opus vs TEdit, E-02): el barrido puede recorrer millones de tiles
        // (mundo Grande real, 8400x2400 = 20,2M) sin ningun aviso de que esta en marcha. IsSearching
        // solo se activa AQUI (no si query.IsEmpty, que ya ha vuelto arriba sin ningun hueco
        // asincrono real que avisar) y se apaga en el finally CON la guarda de generacion, igual
        // que ya hace RefreshWorldsAsync - una vuelta obsoleta que termine tarde no debe apagar
        // el indicador de la vuelta viva.
        IsSearching = true;
        try
        {
            var result = await Task.Run(() => WorldSearch.Run(world, query, _tileNames, _npcNames, _itemNames, cts.Token), cts.Token);
            if (myGeneration != _worldSearchGeneration) return; // una busqueda MAS NUEVA ya esta en marcha - esta es obsoleta

            _lastWorldSearchRows = result.Hits.Select(h => new WorldSearchHitRowViewModel(h)).ToList();
            _worldSearchCurrentIndex = -1;
            ApplyWorldSearchOrder(); // aplica el orden real (por distancia si ShowSpawnDistance esta activo) y vuelca WorldSearchResults
            WorldSearchSummary = result.TotalCount == 0
                ? LocalizationService.Instance["status_no_results"]
                : result.TotalCount > result.Hits.Count
                    ? LocalizationService.Instance.Format("summary_results_capped", result.Hits.Count, result.TotalCount, query.DisplayLimit)
                    : LocalizationService.Instance.Format("summary_results_total", result.TotalCount);
        }
        catch (OperationCanceledException)
        {
            // Cancelada por una busqueda mas nueva (ver arriba) - no es un error real, no toca
            // WorldSearchResults/Summary (los deja a los de la vuelta que SI vaya a terminar).
        }
        finally
        {
            if (myGeneration == _worldSearchGeneration) IsSearching = false;
        }
    }

    [ObservableProperty] private bool _isSearching;

    // F-4: boton Cancelar, visible solo con IsSearching. La infraestructura de cancelacion ya
    // existia entera (_worldSearchCts) desde el buscador original - solo faltaba exponerla.
    [RelayCommand]
    private void CancelWorldSearch() => _worldSearchCts?.Cancel();
}
