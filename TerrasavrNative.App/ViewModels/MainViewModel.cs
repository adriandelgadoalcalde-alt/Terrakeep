using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App.ViewModels;

// Nucleo basico de la Fase 2: cargar un personaje real, verlo (inventario/banco/caja/fragua/
// boveda/mascota-montura-gancho con objetos de Calamity ya mezclados, mas equipo puesto/monedas/
// municion en su forma vanilla), guardar. Sin edicion todavia - eso es el siguiente paso.
public partial class MainViewModel : ObservableObject
{
    private readonly CharacterFileService _service = new();
    private LoadedCharacter? _loaded;

    [ObservableProperty] private string _statusMessage = "Sin personaje cargado.";
    [ObservableProperty] private string? _characterName;
    [ObservableProperty] private bool _isCharacterLoaded;
    [ObservableProperty] private bool _hasCalamityData;

    public ObservableCollection<ContainerViewModel> Containers { get; } = [];

    public void LoadFromPath(string plrPath)
    {
        try
        {
            _loaded = _service.Load(plrPath);
            RebuildContainers();
            CharacterName = _loaded.Character.Name;
            HasCalamityData = _loaded.TplrPath != null;
            IsCharacterLoaded = true;
            int calamityCount = _loaded.MergedContainers.Values.Sum(items => items.Count(i => i.IsCalamity));
            StatusMessage = HasCalamityData
                ? $"Cargado '{_loaded.Character.Name}' - {calamityCount} objeto(s) de Calamity detectado(s)."
                : $"Cargado '{_loaded.Character.Name}' - personaje 100% vanilla (sin .tplr).";
        }
        catch (Exception ex)
        {
            IsCharacterLoaded = false;
            StatusMessage = $"Error al cargar: {ex.Message}";
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
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al guardar: {ex.Message}";
        }
    }

    private void RebuildContainers()
    {
        Containers.Clear();
        if (_loaded == null) return;

        // Contenedores con fusion real de Calamity (mismos 7 que CalamityCharacterSync cubre).
        AddContainer("inventory", "Inventario", _loaded.MergedContainers["inventory"]);
        AddContainer("bank", "Banco", _loaded.MergedContainers["bank"]);
        AddContainer("bank2", "Caja fuerte", _loaded.MergedContainers["bank2"]);
        AddContainer("bank3", "Fragua del Defensor", _loaded.MergedContainers["bank3"]);
        AddContainer("bank4", "Boveda del Vacio", _loaded.MergedContainers["bank4"]);
        AddContainer("miscEquips", "Mascota / Montura / Gancho", _loaded.MergedContainers["miscEquips"]);
        AddContainer("miscDyes", "Tintes (mascota/montura/gancho)", _loaded.MergedContainers["miscDyes"]);

        // Vanilla-only por ahora (ver CalamityCharacterSync.cs para el alcance documentado):
        AddContainer("coins", "Monedas", _loaded.Character.Coins.ToGameItems());
        AddContainer("ammo", "Municion", _loaded.Character.Ammo.ToGameItems());
        AddContainer("loadoutArmor", "Equipo puesto - armadura/accesorios (vanilla; Calamity pendiente)", _loaded.Character.PrimaryLoadout.Items.ToGameItems());
        AddContainer("loadoutSocial", "Equipo puesto - vanidad (vanilla; Calamity pendiente)", _loaded.Character.PrimaryLoadout.Social.ToGameItems());
        AddContainer("loadoutDyes", "Equipo puesto - tintes (vanilla; Calamity pendiente)", _loaded.Character.PrimaryLoadout.Dyes.ToGameItems());
    }

    private void AddContainer(string key, string displayName, GameItem[] items)
    {
        var slots = new ObservableCollection<ItemSlotViewModel>();
        for (int i = 0; i < items.Length; i++)
            slots.Add(new ItemSlotViewModel(_service, i, items[i]));
        Containers.Add(new ContainerViewModel(key, displayName, slots));
    }

    // Vuelca lo que haya en las colecciones enlazadas a la UI de vuelta a MergedContainers
    // antes de guardar - todavia no hay edicion real (Fase 2 basica), pero esto deja el
    // camino listo para cuando la UI empiece a modificar ItemSlotViewModel.Item de verdad.
    private void SyncEditsBackToMerged()
    {
        if (_loaded == null) return;
        foreach (var container in Containers)
        {
            if (!_loaded.MergedContainers.TryGetValue(container.Key, out var arr)) continue;
            for (int i = 0; i < container.Slots.Count && i < arr.Length; i++)
                arr[i] = container.Slots[i].Item;
        }
    }

    partial void OnIsCharacterLoadedChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();
}
