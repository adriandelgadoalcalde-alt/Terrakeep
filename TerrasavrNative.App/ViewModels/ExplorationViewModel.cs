using System.Collections.ObjectModel;
using System.IO;
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

    public void LoadFromPath(string wldPath)
    {
        try
        {
            StatusMessage = "Leyendo mundo...";
            var world = WldReader.Read(File.ReadAllBytes(wldPath));
            _world = world;

            StatusMessage = "Pintando mapa...";
            WorldImage = WorldRenderer.Render(world, _mapColors);

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
    }

    partial void OnNpcSearchTextChanged(string value) => ApplyNpcFilter();

    private const double MinZoom = 0.1, MaxZoom = 6.0;

    partial void OnZoomChanged(double value)
    {
        double clamped = Math.Clamp(value, MinZoom, MaxZoom);
        if (clamped != value) Zoom = clamped; // reentra, se estabiliza al segundo paso
    }

    [RelayCommand] private void ZoomIn() => Zoom *= 1.25;
    [RelayCommand] private void ZoomOut() => Zoom /= 1.25;
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
