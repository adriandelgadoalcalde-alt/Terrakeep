using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
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

    // Indice de la pestaña externa (Personaje=0, Libreria=1, ...) - se usa para saltar
    // automaticamente a la Libreria al pulsar "Elegir objeto" en un slot, y volver a
    // Personaje en cuanto se coloca el objeto elegido.
    private const int PersonajeTabIndex = 0;
    private const int LibreriaTabIndex = 1;

    [ObservableProperty] private string _statusMessage = "Sin personaje cargado.";
    [ObservableProperty] private string? _characterName;
    [ObservableProperty] private bool _isCharacterLoaded;
    [ObservableProperty] private bool _hasCalamityData;
    [ObservableProperty] private int _selectedTabIndex;

    public ObservableCollection<ContainerViewModel> Containers { get; } = [];
    public ObservableCollection<BuffRowViewModel> Buffs { get; } = [];
    public ObservableCollection<ResearchRowViewModel> Research { get; } = [];
    public BuildsViewModel Builds { get; }
    public WhatsNewViewModel WhatsNew { get; }
    public AboutViewModel About { get; } = new();
    public ExplorationViewModel Exploration { get; }
    public LibraryViewModel Library { get; }

    public MainViewModel()
    {
        Builds = new BuildsViewModel(_service.VanillaBuilds, _service.CalamityBuilds);
        WhatsNew = new WhatsNewViewModel(_service.WhatsNew);
        Exploration = new ExplorationViewModel(_service);
        Library = new LibraryViewModel(_service);
        Library.ItemPlaced += () => SelectedTabIndex = PersonajeTabIndex;
    }

    private void RequestPickForSlot(ItemSlotViewModel slot)
    {
        Library.PickTarget = slot;
        SelectedTabIndex = LibreriaTabIndex;
    }

    public void LoadFromPath(string plrPath)
    {
        try
        {
            Library.PickTarget = null;
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
        Buffs.Clear();
        Research.Clear();
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

        // Buffs vanilla solamente por ahora - los de Calamity viven en "modBuffs" dentro del
        // .tplr, sin fusionar todavia (ver bitacora.md).
        foreach (var buff in _loaded.Character.Buffs)
        {
            if (buff.Id == 0) continue;
            Buffs.Add(BuffRowViewModel.From(buff, _service.VanillaBuffs));
        }

        RebuildResearch();
    }

    private void RebuildResearch()
    {
        Research.Clear();
        if (_loaded == null) return;
        foreach (var entry in _loaded.Character.Research.OrderBy(e => e.Pid))
        {
            bool isCalamity = entry.Pid.Contains('/');
            string displayName = isCalamity
                ? ResolveCalamityPidName(entry.Pid)
                : _service.VanillaCatalog.GetNameByKey(entry.Pid);
            Research.Add(new ResearchRowViewModel(displayName, entry.Count, isCalamity));
        }
    }

    private string ResolveCalamityPidName(string pid)
    {
        int slash = pid.IndexOf('/');
        if (slash < 0) return pid;
        string mod = pid[..slash], internalName = pid[(slash + 1)..];
        return _service.CalamityCatalog.ByModAndInternal(mod, internalName)?.DisplayName ?? pid;
    }

    // "Investigar todo": rellena PlrCharacter.Research con una entrada por cada objeto conocido
    // (vanilla + Calamity) que todavia no estuviera investigado, con un conteo alto fijo
    // (mismo criterio que la version JS: no se conoce la tabla real de "cuantos hacen falta"
    // por objeto, un valor alto de sobra garantiza el desbloqueo completo igualmente).
    [RelayCommand(CanExecute = nameof(IsCharacterLoaded))]
    private void ResearchAll()
    {
        if (_loaded == null) return;
        const int placeholderCount = 9999;
        var existingPids = new HashSet<string>(_loaded.Character.Research.Select(e => e.Pid));

        foreach (var pid in _service.VanillaCatalog.AllInternalNames())
        {
            if (existingPids.Add(pid))
                _loaded.Character.Research.Add(new PlrResearchEntry { Pid = pid, Count = placeholderCount });
        }
        foreach (var entry in _service.CalamityCatalog.Entries)
        {
            string pid = $"{entry.Mod}/{entry.Internal}";
            if (existingPids.Add(pid))
                _loaded.Character.Research.Add(new PlrResearchEntry { Pid = pid, Count = placeholderCount });
        }

        RebuildResearch();
        StatusMessage = $"Investigacion completa aplicada ({Research.Count} objetos) - pulsa Guardar para conservarlo.";
    }

    private void AddContainer(string key, string displayName, GameItem[] items)
    {
        var slots = new ObservableCollection<ItemSlotViewModel>();
        for (int i = 0; i < items.Length; i++)
            slots.Add(new ItemSlotViewModel(_service, i, items[i], RequestPickForSlot));
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
