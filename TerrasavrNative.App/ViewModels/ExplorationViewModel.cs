using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.WldFormat;

namespace TerrasavrNative.App.ViewModels;

public sealed partial class WorldNpcRowViewModel(int id, string name, int x, int y, bool homeless) : ObservableObject
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public int TileX { get; } = x;
    public int TileY { get; } = y;
    public string Position { get; } = homeless ? $"({x}, {y}) - sin casa" : $"({x}, {y})";
    public string? IconPath { get; } = NpcIconResolver.GetIconPath(id);

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

    public ExplorationViewModel(CharacterFileService service)
    {
        _npcNames = service.NpcNames;
        _mapColors = service.MapColors;
        _tileNames = service.TileNames;
        // Fire-and-forget deliberado, mismo criterio real que HomeViewModel - el constructor no
        // puede ser async, y no hay nada que esperar aqui (Worlds se rellena un instante
        // despues, IsScanningWorlds refleja el hueco mientras tanto).
        _ = RefreshWorldsAsync();
    }

    [RelayCommand]
    private async Task RefreshWorldsAsync()
    {
        Worlds.Clear();
        IsScanningWorlds = true;
        try
        {
            string dir = CharacterFileService.GetDefaultWorldsDirectory();
            var scanned = await Task.Run(() => ScanWorlds(dir));
            foreach (var entry in scanned) Worlds.Add(entry);
            ScanMessage = Worlds.Count == 0 ? $"Ningun mundo encontrado en {dir}" : null;
        }
        finally
        {
            IsScanningWorlds = false;
        }
    }

    // Todo el trabajo real de disco (enumerar + leer la cabecera de cada .wld) - se ejecuta en
    // un hilo de fondo via Task.Run (RefreshWorldsAsync de arriba), nunca toca Worlds
    // directamente (seria una modificacion desde fuera del hilo de UI).
    private static List<WorldListEntryViewModel> ScanWorlds(string dir)
    {
        var result = new List<WorldListEntryViewModel>();
        var wldFiles = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.wld") : [];
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
                .Select(n => new WorldNpcRowViewModel(n.Id, _npcNames.GetName(n.Id), n.TileX, n.TileY, n.Homeless))
                .ToList();
            Npcs.Clear();
            foreach (var npc in _allNpcs) Npcs.Add(npc);
            NpcSearchText = string.Empty;
            Zoom = 1.0;
            HoverInfo = string.Empty;
            ApplyNpcFilter();

            var foundIds = world.Npcs.Select(n => n.Id).ToHashSet();
            MissingNpcs.Clear();
            foreach (int id in VanillaTownNpcRoster.Ids)
                if (!foundIds.Contains(id))
                    MissingNpcs.Add(new MissingNpcRowViewModel(id, _npcNames.GetName(id)));

            WorldTitle = world.Header.Title;
            IsWorldLoaded = true;
            StatusMessage = $"'{world.Header.Title}' - {world.Header.TilesWide}x{world.Header.TilesHigh} tiles, " +
                $"{_allNpcs.Count} NPC(s) de pueblo, {MissingNpcs.Count} todavia sin conseguir.";
        }
        catch (Exception ex)
        {
            _world = null;
            IsWorldLoaded = false;
            StatusMessage = $"Error al leer el mundo: {ex.Message}";
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
}
