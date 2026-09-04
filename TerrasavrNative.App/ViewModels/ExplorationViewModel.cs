using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
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
        WorldSearchKind.Sign => "Letrero",
        _ => "",
    };

    // Fase 3: null = "distancia al spawn" apagada (no se muestra) - ExplorationViewModel.
    // ApplyWorldSearchOrder es quien la calcula/limpia, nunca este constructor (el spawn real
    // solo se conoce con el mundo cargado, no al crear la fila).
    [ObservableProperty] private string? _distanceLabel;

    // Fase 3: el resultado activo de la navegacion anterior/siguiente - resalta la fila en la
    // lista Y el marcador en el mapa (mismo objeto, dos plantillas distintas).
    [ObservableProperty] private bool _isCurrent;
}

public sealed partial class WorldNpcRowViewModel(int id, string name, int x, int y, bool homeless, int? headIndex) : ObservableObject
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public int TileX { get; } = x;
    public int TileY { get; } = y;
    public string Position { get; } = homeless ? $"({x}, {y}) - sin casa" : $"({x}, {y})";
    public string? IconPath { get; } = NpcIconResolver.GetIconPath(id);
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

    [ObservableProperty] private BitmapSource? _worldImage;
    [ObservableProperty] private string _statusMessage = "Sin mundo cargado.";
    [ObservableProperty] private string? _worldTitle;
    [ObservableProperty] private bool _isWorldLoaded;
    [ObservableProperty] private string _npcSearchText = string.Empty;
    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private string _hoverInfo = string.Empty;
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
    }

    // H4-08 (cuarta auditoria de Opus, Fable): "el estado vacio de Exploracion es un lienzo
    // negro sin guia" - el unico aviso real ("Sin mundo cargado.") vivia en StatusMessage, en
    // el tamaño de letra mas pequeño de la app y en la esquina inferior izquierda. Real,
    // centrado en el propio lienzo (mismo patron ya usado por el overlay de IsLoading, ver
    // MainWindow.xaml) - nunca durante la carga, para no parpadear entre los dos avisos.
    public bool IsEmpty => !IsWorldLoaded && !IsLoading;
    partial void OnIsWorldLoadedChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));

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
    private int _worldSearchCurrentIndex = -1;

    private readonly DispatcherTimer _worldSearchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private CancellationTokenSource? _worldSearchCts;
    private int _worldSearchGeneration;

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
            var (world, image) = await Task.Run(() =>
            {
                var w = WldReader.Read(File.ReadAllBytes(wldPath));
                var img = WorldRenderer.Render(w, _mapColors);
                return (w, img);
            });
            _world = world;
            WorldImage = image;

            _allNpcs = world.Npcs
                .OrderBy(n => _npcNames.GetName(n.Id))
                .Select(n => new WorldNpcRowViewModel(n.Id, _npcNames.GetName(n.Id), n.TileX, n.TileY, n.Homeless,
                    NpcHeadProfile.GetHeadIndex(n.Id, n.VariationIndex, world.ShimmeredNpcTypes.Contains(n.Id))))
                .ToList();
            Npcs.Clear();
            foreach (var npc in _allNpcs) Npcs.Add(npc);
            NpcSearchText = string.Empty;
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

            var foundIds = world.Npcs.Select(n => n.Id).ToHashSet();
            MissingNpcs.Clear();
            foreach (int id in VanillaTownNpcRoster.Ids)
                if (!foundIds.Contains(id))
                    MissingNpcs.Add(new MissingNpcRowViewModel(id, _npcNames.GetName(id)));

            WorldTitle = world.Header.Title;
            IsWorldLoaded = true;
            StatusMessage = $"'{world.Header.Title}' - {world.Header.TilesWide}x{world.Header.TilesHigh} tiles, " +
                $"{_allNpcs.Count} NPC(s) de pueblo, {MissingNpcs.Count} todavia sin conseguir.";
            UpdateCurrentWorldPath(wldPath);
        }
        catch (Exception ex)
        {
            _world = null;
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
    private void ApplyNpcFilter()
    {
        bool sinBusqueda = string.IsNullOrWhiteSpace(NpcSearchText);
        NpcSearchResults.Clear();
        foreach (var npc in _allNpcs)
        {
            npc.IsMatch = sinBusqueda || npc.Name.Contains(NpcSearchText, StringComparison.OrdinalIgnoreCase);
            if (npc.IsMatch) NpcSearchResults.Add(npc);
        }
    }

    // Punto 4: reinicia el debounce en cada tecla - el barrido real (RunWorldSearchAsync) no se
    // lanza aqui directamente, solo cuando el usuario para de escribir (ver el Tick del
    // constructor).
    partial void OnWorldSearchTextChanged(string value)
    {
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
        var tileTypes = new HashSet<int>();
        foreach (var (id, name) in _tileNames.AllTiles)
            if (LibrarySearchGrammar.Matches(text, id, name.ToLowerInvariant(), null)) tileTypes.Add(id);

        var wallIds = new HashSet<int>();
        foreach (var (id, name) in _tileNames.AllWalls)
            if (LibrarySearchGrammar.Matches(text, id, name.ToLowerInvariant(), null)) wallIds.Add(id);

        var npcIds = new HashSet<int>();
        foreach (var (id, name) in _npcNames.All)
            if (LibrarySearchGrammar.Matches(text, id, name.ToLowerInvariant(), null)) npcIds.Add(id);

        var liquidTypes = new HashSet<byte>();
        foreach (var (id, name) in LiquidCandidates)
            if (LibrarySearchGrammar.Matches(text, id, name.ToLowerInvariant(), null)) liquidTypes.Add((byte)id);

        // Fase 2 (ESPEC-buscador-mundo-tedit.md#5.2): objetos reales dentro de cofres, mismo
        // criterio de candidatos que tiles/paredes/NPCs (recorrer el catalogo entero, casar con
        // la gramatica real). Los NetId de Calamity reales que un .wld pueda guardar no estan
        // en VanillaItemCatalog - simplemente no se ofrecen como candidato, el objeto seguira
        // apareciendo en la lista si se busca por su cofre de otra forma (id/#) o no aparecera,
        // nunca se inventa una coincidencia.
        var chestItemIds = new HashSet<int>();
        foreach (var (id, name) in _itemNames.AllEntries())
            if (LibrarySearchGrammar.Matches(text, id, name.ToLowerInvariant(), null)) chestItemIds.Add(id);

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
        _worldSearchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _worldSearchCts = cts;
        int myGeneration = ++_worldSearchGeneration;

        var world = _world;
        string text = WorldSearchText;
        if (world == null || string.IsNullOrWhiteSpace(text)) return;

        var query = BuildWorldSearchQuery(text);
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
    }
}
