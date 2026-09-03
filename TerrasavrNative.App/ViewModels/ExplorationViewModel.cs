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

public sealed class WorldNpcRowViewModel(int id, string name, int x, int y, bool homeless)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public int TileX { get; } = x;
    public int TileY { get; } = y;
    public string Position { get; } = homeless ? $"({x}, {y}) - sin casa" : $"({x}, {y})";
    public string? IconPath { get; } = NpcIconResolver.GetIconPath(id);
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
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsNotLoading));

    public ObservableCollection<WorldNpcRowViewModel> Npcs { get; } = [];
    public ObservableCollection<MissingNpcRowViewModel> MissingNpcs { get; } = [];

    // El code-behind (unico sitio que conoce el ScrollViewer real del mapa) se suscribe a esto
    // para centrar la vista - la ViewModel no puede tocar controles de UI directamente.
    public event Action<int, int>? NavigateToTileRequested;

    [RelayCommand]
    private void GoToNpc(WorldNpcRowViewModel npc) => NavigateToTileRequested?.Invoke(npc.TileX, npc.TileY);

    public ExplorationViewModel(CharacterFileService service)
    {
        _npcNames = service.NpcNames;
        _mapColors = service.MapColors;
        _tileNames = service.TileNames;
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
        // el liquido. Los codigos reales (1=Agua/2=Lava/3=Miel-o-Shimmer) son los mismos que
        // ya corrige WorldRenderer.LiquidColor, confirmados contra TEdit real.
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

    private static string LiquidName(byte liquidType) => liquidType switch
    {
        2 => "Lava",
        3 => "Miel",
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

    private const double MinZoom = 0.1, MaxZoom = 6.0;

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

    private void ApplyNpcFilter()
    {
        Npcs.Clear();
        var matches = string.IsNullOrWhiteSpace(NpcSearchText)
            ? _allNpcs
            : _allNpcs.Where(n => n.Name.Contains(NpcSearchText, StringComparison.OrdinalIgnoreCase));
        foreach (var npc in matches) Npcs.Add(npc);
    }
}
