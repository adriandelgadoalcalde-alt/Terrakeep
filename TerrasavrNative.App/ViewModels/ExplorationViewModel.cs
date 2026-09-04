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
    public int TileX { get; } = hit.X;
    public int TileY { get; } = hit.Y;
    public string Name { get; } = hit.Name;
    public string Position { get; } = $"({hit.X}, {hit.Y})";
    public string KindLabel { get; } = hit.Kind switch
    {
        WorldSearchKind.Tile => "Tile",
        WorldSearchKind.Wall => "Pared",
        WorldSearchKind.Liquid => "Liquido",
        WorldSearchKind.Npc => "NPC",
        // Fase 2 (ESPEC-buscador-mundo-tedit.md#5.2): la coordenada de un objeto de cofre es la
        // del COFRE, no la del objeto - mismo criterio real que TEdit (SearchContainers).
        WorldSearchKind.ChestItem => "En cofre",
        // Fase 2b: marco de objeto/perchero/maniqui/bandeja/frasco/ancla - mismo criterio de
        // coordenada que ChestItem (la del contenedor, no la del objeto).
        WorldSearchKind.TileEntityItem => "En objeto",
        WorldSearchKind.Sign => "Letrero",
        WorldSearchKind.OreVein => "Veta",
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
    [ObservableProperty] private bool _isChecked;
    // Filtro por nombre O id (mismo criterio que TileWallPickerViewModel.FilterItem de TEdit,
    // ESPEC-ui-exploracion.md#1.3) - atenua/oculta en vez de quitar de la coleccion, mismo
    // patron ya establecido por WorldNpcRowViewModel.IsMatch.
    [ObservableProperty] private bool _isMatch = true;
}

public sealed partial class WorldNpcRowViewModel(int id, string name, int x, int y, bool homeless, int? headIndex, bool isUnderground, int depthTiles) : ObservableObject
{
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
    public string? DepthLabel { get; } = isUnderground ? $"Bajo tierra (profundidad {depthTiles})" : null;
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
    [ObservableProperty] private string _statusMessage = "Sin mundo cargado.";
    [ObservableProperty] private string? _worldTitle;
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
    public bool ShowZeroResultsState => SelectedCategory == WorldSearchCategory.All && WorldSearchSummary == "Sin resultados.";
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
        if (_world == null || _presence == null) return;
        if (ChestViewMode == 0)
        {
            foreach (var ((type, u, v), count) in _presence.ChestKindCounts.OrderByDescending(kv => kv.Value))
                Inventory.Add(new WorldInventoryRowViewModel(type, u, v, _tileNames.TileVariantName(type, u, v), count, null,
                    TileIconResolver.GetIconPath(type, u, v), ToWpfColor(_mapColors.TileColor(type))));
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
                target.Add(new WorldInventoryRowViewModel(id, 0, 0, _tileNames.TileName(id), count, veinCount,
                    TileIconResolver.GetIconPath(id), ToWpfColor(_mapColors.TileColor(id))));
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
        switch (ObjectsViewMode)
        {
            case 0:
                foreach (var (id, count) in _presence.TileCounts.OrderByDescending(kv => kv.Value))
                    Inventory.Add(new WorldInventoryRowViewModel(id, 0, 0, _tileNames.TileName(id), count, null,
                        TileIconResolver.GetIconPath(id), ToWpfColor(_mapColors.TileColor(id))));
                break;
            case 1:
                foreach (var (id, count) in _presence.WallCounts.OrderByDescending(kv => kv.Value))
                    Inventory.Add(new WorldInventoryRowViewModel(id, 0, 0, _tileNames.WallName(id), count, null,
                        WallIconResolver.GetIconPath(id), ToWpfColor(_mapColors.WallColor(id))));
                break;
            case 2:
                foreach (var (code, count) in _presence.LiquidCounts.OrderByDescending(kv => kv.Value))
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
                    Inventory.Add(new WorldInventoryRowViewModel(code, 0, 0, WorldSearch.LiquidName(code), count, null,
                        null, ToWpfColor(_mapColors.LiquidColor(code))));
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
        void Filtrar(IEnumerable<WorldInventoryRowViewModel> filas)
        {
            foreach (var row in filas)
                row.IsMatch = sinBusqueda || row.Name.Contains(WorldSearchText, StringComparison.OrdinalIgnoreCase)
                    || row.Id.ToString().Contains(WorldSearchText, StringComparison.Ordinal);
        }
        Filtrar(Inventory);
        Filtrar(OreMetals);
        Filtrar(OreGems);
        Filtrar(OreTargets);
    }

    // Clic simple sobre una fila de inventario - busca SOLO esa (el caso comun no debe costar
    // dos gestos, ESPEC-ui-exploracion.md#9.3-C). Cofres/Objetos usan TileTypes a secas salvo
    // cuando la fila representa una VARIANTE real (U/V != 0 o el propio Type no es generico -
    // en la practica, cualquier fila de "Cofres/Por tipo" con U/V reales usa SpriteVariants
    // para no traer TODOS los cofres del mismo Type).
    [RelayCommand]
    private void SearchInventoryRow(WorldInventoryRowViewModel row) => _ = RunWorldSearchAsyncWithQuery(BuildSingleRowQuery(row));

    [RelayCommand]
    private void SearchCheckedInventory()
    {
        var marcadas = Inventory.Where(r => r.IsChecked).ToList();
        if (marcadas.Count == 0) return;
        WorldSearchQuery query = SelectedCategory switch
        {
            WorldSearchCategory.Chests when ChestViewMode == 0 =>
                new WorldSearchQuery { SpriteVariants = marcadas.Select(r => (r.Id, r.U, r.V)).ToHashSet() },
            WorldSearchCategory.Chests => new WorldSearchQuery { ChestItemIds = marcadas.Select(r => r.Id).ToHashSet() },
            WorldSearchCategory.Objects when ObjectsViewMode == 0 => new WorldSearchQuery { TileTypes = marcadas.Select(r => r.Id).ToHashSet() },
            WorldSearchCategory.Objects when ObjectsViewMode == 1 => new WorldSearchQuery { WallIds = marcadas.Select(r => r.Id).ToHashSet() },
            WorldSearchCategory.Objects => new WorldSearchQuery { LiquidTypes = marcadas.Select(r => (byte)r.Id).ToHashSet() },
            _ => new WorldSearchQuery(),
        };
        _ = RunWorldSearchAsyncWithQuery(query);
    }

    private WorldSearchQuery BuildSingleRowQuery(WorldInventoryRowViewModel row) => SelectedCategory switch
    {
        WorldSearchCategory.Chests when ChestViewMode == 0 => new WorldSearchQuery { SpriteVariants = new HashSet<(int, short, short)> { (row.Id, row.U, row.V) } },
        WorldSearchCategory.Chests => new WorldSearchQuery { ChestItemIds = new HashSet<int> { row.Id } },
        WorldSearchCategory.Objects when ObjectsViewMode == 0 => new WorldSearchQuery { TileTypes = new HashSet<int> { row.Id } },
        WorldSearchCategory.Objects when ObjectsViewMode == 1 => new WorldSearchQuery { WallIds = new HashSet<int> { row.Id } },
        WorldSearchCategory.Objects => new WorldSearchQuery { LiquidTypes = new HashSet<byte> { (byte)row.Id } },
        _ => new WorldSearchQuery(),
    };

    // Punto 4 (Minerales - ESPEC-ui-exploracion.md#11.3/11.4): "Marcar en el mapa" activa la
    // capa de resaltado (sin tope, TODAS las posiciones - mismo reparto real que hace TEdit,
    // resaltado sin tope + lista topada) y rellena WorldSearchResults con las VETAS (no los
    // tiles sueltos, serian decenas de miles). El tope de la LISTA sigue siendo 1000
    // (WorldSearchQuery.DisplayLimit); el resumen dice la verdad completa (ver
    // RunWorldSearchAsyncWithQuery).
    [RelayCommand]
    private async Task MarkOresOnMap()
    {
        if (_world == null) return;
        var marcados = OreMetals.Concat(OreGems).Concat(OreTargets).Where(r => r.IsChecked).Select(r => r.Id).ToHashSet();
        if (marcados.Count == 0) return;

        var world = _world;
        var cts = new CancellationTokenSource();
        _worldSearchCts?.Cancel();
        _worldSearchCts = cts;
        int myGeneration = ++_worldSearchGeneration;
        try
        {
            var (highlight, vetas, totalVetas) = await Task.Run(() =>
            {
                var img = WorldHighlightRenderer.Render(world, marcados, Colors.Orange, cts.Token);
                var v = OreVeinFinder.Find(world, marcados, limit: 1000, out int total, cts.Token);
                return (img, v, total);
            }, cts.Token);
            if (myGeneration != _worldSearchGeneration) return;

            WorldHighlight = highlight;
            _lastWorldSearchRows = vetas.Select(vein => new WorldSearchHitRowViewModel(
                new WorldSearchHit(vein.CenterX, vein.CenterY, $"{_tileNames.TileName(vein.Type)} ({vein.TileCount:N0} tiles)", WorldSearchKind.OreVein))).ToList();
            _worldSearchCurrentIndex = -1;
            ApplyWorldSearchOrder();
            WorldSearchSummary = totalVetas > vetas.Count
                ? $"{vetas.Count:N0} de {totalVetas:N0} veta(s) (limitado a 1000 en la lista - el mapa las marca TODAS)"
                : $"{totalVetas:N0} veta(s)";
        }
        catch (OperationCanceledException) { }
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

    public ExplorationViewModel(CharacterFileService service)
    {
        _npcNames = service.NpcNames;
        _mapColors = service.MapColors;
        _tileNames = service.TileNames;
        _itemNames = service.VanillaCatalog;
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
                    ? "No se encontro ninguna carpeta real de mundos de Terraria (vanilla ni tModLoader)."
                    : $"Ningun mundo encontrado en {string.Join(" ni en ", dirs)}"
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
            StatusMessage = $"Error al abrir la carpeta: {ex.Message}";
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
        if (tileY > _world.Header.TilesHigh - 204) { HoverLayerText = "Infierno"; zonaColor = "Hell"; }
        else if (tileY > _world.Header.RockLevel) { HoverLayerText = "Cavernas"; zonaColor = "Rock"; }
        else if (pies > 0) { HoverLayerText = "Subterráneo"; zonaColor = "Earth"; }
        else if (spaceCheck < 1.0) { HoverLayerText = "Espacio"; zonaColor = "Space"; }
        else { HoverLayerText = "Superficie"; zonaColor = "Sky"; }
        var c = _mapColors.Global(zonaColor);
        HoverLayerColor = Color.FromArgb(c.A, c.R, c.G, c.B);

        int tilesRespectoSuelo = (int)Math.Round(tileY - groundLevel);
        HoverDepthText = tilesRespectoSuelo switch
        {
            > 0 => $"{tilesRespectoSuelo:N0} tiles bajo el suelo",
            < 0 => $"{-tilesRespectoSuelo:N0} tiles sobre el suelo",
            _ => "En el nivel del suelo",
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
        3 => "Miel",
        4 => "Centelleo",
        _ => "Agua",
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
            StatusMessage = "Leyendo y pintando el mapa...";
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
            Zoom = 1.0;
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
            IsWorldLoaded = true;
            StatusMessage = $"'{world.Header.Title}' - {world.Header.TilesWide}x{world.Header.TilesHigh} tiles, " +
                $"{_allNpcs.Count} NPC(s) de pueblo, {MissingNpcs.Count} todavia sin conseguir.";
            OnPropertyChanged(nameof(ChestsPillCount));
            OnPropertyChanged(nameof(OresPillCount));
            OnPropertyChanged(nameof(ObjectsPillCount));
            UpdateCurrentWorldPath(wldPath);
        }
        catch (Exception ex)
        {
            _world = null;
            _presence = null;
            WorldHighlight = null;
            IsWorldLoaded = false;
            StatusMessage = $"Error al leer el mundo: {ex.Message}";
            UpdateCurrentWorldPath(null); // un fallo real no debe dejar ninguna pildora marcada como "cargada"
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
    private static readonly (int Id, string Name)[] LiquidCandidates =
        [(1, "Agua"), (2, "Lava"), (3, "Miel"), (4, "Centelleo")];

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
            if (LibrarySearchGrammar.Matches(text, id, name.ToLowerInvariant(), null)) tileTypes.Add(id);
        }

        var wallIds = new HashSet<int>();
        foreach (var (id, name) in _tileNames.AllWalls)
        {
            if (_presence != null && !_presence.HasWall(id)) continue;
            if (LibrarySearchGrammar.Matches(text, id, name.ToLowerInvariant(), null)) wallIds.Add(id);
        }

        var npcIds = new HashSet<int>();
        foreach (var (id, name) in _npcNames.All)
        {
            if (_presence != null && !_presence.HasNpc(id)) continue;
            if (LibrarySearchGrammar.Matches(text, id, name.ToLowerInvariant(), null)) npcIds.Add(id);
        }

        var liquidTypes = new HashSet<byte>();
        foreach (var (id, name) in LiquidCandidates)
        {
            if (_presence != null && !_presence.HasLiquid((byte)id)) continue;
            if (LibrarySearchGrammar.Matches(text, id, name.ToLowerInvariant(), null)) liquidTypes.Add((byte)id);
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
            if (LibrarySearchGrammar.Matches(text, id, name.ToLowerInvariant(), null)) chestItemIds.Add(id);
        }

        // Fase 2: los letreros son texto libre, sin catalogo de ids - se reutiliza la MISMA
        // gramatica (comas=OR, espacios=AND) tratando el texto de cada letrero como si fuera el
        // nombre de una unica entrada (id=0, sin sentido para un letrero, se ignora).
        bool SignPredicate(string signText) => LibrarySearchGrammar.Matches(text, 0, signText.ToLowerInvariant(), null);

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
                WorldSearchSummary = "Sin resultados.";
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
                ? "Sin resultados."
                : result.TotalCount > result.Hits.Count
                    ? $"{result.Hits.Count} de {result.TotalCount} resultado(s) (limitado a {query.DisplayLimit})"
                    : $"{result.TotalCount} resultado(s)";
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
