using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.WldFormat;

namespace TerrasavrNative.App.ViewModels;

public sealed class WorldNpcRowViewModel(string name, int x, int y)
{
    public string Name { get; } = name;
    public string Position { get; } = $"({x}, {y})";
}

// Pestaña "Exploracion" - cargar un .wld real, pintarlo entero (WorldRenderer) y listar sus
// NPCs de pueblo con su posicion. Solo lectura, no coloca/quita tiles (misma restriccion que
// el visor JS de referencia).
public partial class ExplorationViewModel : ObservableObject
{
    private readonly NpcNameCatalog _npcNames;
    private readonly MapColorCatalog _mapColors;

    [ObservableProperty] private BitmapSource? _worldImage;
    [ObservableProperty] private string _statusMessage = "Sin mundo cargado.";
    [ObservableProperty] private string? _worldTitle;
    [ObservableProperty] private bool _isWorldLoaded;

    public ObservableCollection<WorldNpcRowViewModel> Npcs { get; } = [];

    public ExplorationViewModel(CharacterFileService service)
    {
        _npcNames = service.NpcNames;
        _mapColors = service.MapColors;
    }

    public void LoadFromPath(string wldPath)
    {
        try
        {
            StatusMessage = "Leyendo mundo...";
            var world = WldReader.Read(File.ReadAllBytes(wldPath));

            StatusMessage = "Pintando mapa...";
            WorldImage = WorldRenderer.Render(world, _mapColors);

            Npcs.Clear();
            foreach (var npc in world.Npcs.OrderBy(n => _npcNames.GetName(n.Id)))
                Npcs.Add(new WorldNpcRowViewModel(_npcNames.GetName(npc.Id), npc.TileX, npc.TileY));

            WorldTitle = world.Header.Title;
            IsWorldLoaded = true;
            StatusMessage = $"'{world.Header.Title}' - {world.Header.TilesWide}x{world.Header.TilesHigh} tiles, {Npcs.Count} NPC(s) de pueblo.";
        }
        catch (Exception ex)
        {
            IsWorldLoaded = false;
            StatusMessage = $"Error al leer el mundo: {ex.Message}";
        }
    }
}
