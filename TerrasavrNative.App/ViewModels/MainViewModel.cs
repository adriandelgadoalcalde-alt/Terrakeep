using System.Collections.ObjectModel;
using System.IO;
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

    // Indice de la pestaña externa (Inicio=0, Personaje=1, ...) - el resto solo lo usa la
    // pagina de Inicio para sus tarjetas de navegacion (GoToTabCommand).
    private const int InicioTabIndex = 0;
    private const int PersonajeTabIndex = 1;
    private const int BuildsTabIndex = 2;
    private const int NovedadesTabIndex = 3;
    private const int ExploracionTabIndex = 4;
    private const int AcercaDeTabIndex = 5;

    // Indice de la pestaña INTERNA dentro de Personaje (Objetos=0, ...) - la Libreria vive
    // ahora DENTRO de la propia pestaña Objetos, siempre visible debajo del inventario
    // (pedido explicito 1-sep-2026, "la libreria deberia estar tambien dentro de personaje...
    // como es terrasav" - y ademas necesario para que arrastrar una tarjeta hasta un slot sea
    // posible: si Libreria fuera una pestaña aparte, nunca se verian los dos a la vez).
    private const int ObjetosInnerTabIndex = 0;

    [ObservableProperty] private string _statusMessage = "Sin personaje cargado.";
    [ObservableProperty] private string? _characterName;
    [ObservableProperty] private bool _isCharacterLoaded;
    [ObservableProperty] private bool _hasCalamityData;
    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private int _personajeInnerTabIndex;

    public ObservableCollection<ContainerViewModel> Containers { get; } = [];
    [ObservableProperty] private EquipmentGroupViewModel? _equipmentGroup;
    public ObservableCollection<ResearchRowViewModel> Research { get; } = [];
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
    public ItemEditViewModel ItemEdit { get; }

    public MainViewModel()
    {
        Builds = new BuildsViewModel(_service.VanillaBuilds, _service.CalamityBuilds, _service);
        WhatsNew = new WhatsNewViewModel(_service.WhatsNew);
        Changelog = new ChangelogViewModel(_service.Changelog);
        Exploration = new ExplorationViewModel(_service);
        Library = new LibraryViewModel(_service);
        Appearance = new AppearanceViewModel(_service);
        Library.ItemPlaced += () =>
        {
            SelectedTabIndex = PersonajeTabIndex;
            PersonajeInnerTabIndex = ObjetosInnerTabIndex;
        };
        Buffs = new BuffsViewModel(_service);
        ItemEdit = new ItemEditViewModel(_service);
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

    // Usado por las tarjetas de la pagina de Inicio para saltar directamente a una seccion.
    // "Libreria" ya no es una pestaña propia (ver el comentario de ObjetosInnerTabIndex) - vive
    // dentro de Objetos, asi que la tarjeta de Inicio salta ahi igual que "Personaje".
    [RelayCommand]
    private void GoToTab(string tab)
    {
        if (tab is "Personaje" or "Libreria")
        {
            SelectedTabIndex = PersonajeTabIndex;
            PersonajeInnerTabIndex = ObjetosInnerTabIndex;
            return;
        }
        SelectedTabIndex = tab switch
        {
            "Builds" => BuildsTabIndex,
            "Novedades" => NovedadesTabIndex,
            "Exploracion" => ExploracionTabIndex,
            "AcercaDe" => AcercaDeTabIndex,
            _ => InicioTabIndex,
        };
    }

    private void RequestPickForSlot(ItemSlotViewModel slot)
    {
        Library.PickTarget = slot;
        SelectSlot(slot); // el panel Editar sigue al slot que se esta rellenando desde la Libreria
        SelectedTabIndex = PersonajeTabIndex;
        PersonajeInnerTabIndex = ObjetosInnerTabIndex;
    }

    public void LoadFromPath(string plrPath)
    {
        try
        {
            Library.PickTarget = null;
            ItemEdit.Slot = null;
            _loaded = _service.Load(plrPath);
            RebuildContainers();
            Appearance.LoadFrom(_loaded.Character);
            Servers.LoadFrom(_loaded.Character);
            Flags.LoadFrom(_loaded.Character);
            VersionEditor.LoadFrom(_loaded.Character);
            Buffs.LoadFrom(_loaded.Character);
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
        Research.Clear();
        EquipmentGroup = null;
        if (_loaded == null) return;

        // Contenedores con fusion real de Calamity (mismos 7 que CalamityCharacterSync cubre).
        AddContainer("inventory", "Inventario", _loaded.MergedContainers["inventory"]);
        AddContainer("bank", "Banco", _loaded.MergedContainers["bank"]);
        AddContainer("bank2", "Caja fuerte", _loaded.MergedContainers["bank2"]);
        AddContainer("bank3", "Fragua del Defensor", _loaded.MergedContainers["bank3"]);
        AddContainer("bank4", "Boveda del Vacio", _loaded.MergedContainers["bank4"]);
        AddContainer("miscEquips", "Mascota / Montura / Gancho", _loaded.MergedContainers["miscEquips"]);
        AddContainer("miscDyes", "Tintes (mascota/montura/gancho)", _loaded.MergedContainers["miscDyes"]);

        // Monedas/municion: vanilla-only (ver CalamityCharacterSync.cs para el alcance
        // documentado - la app JS tampoco los sincroniza con Calamity, solo los protege).
        AddContainer("coins", "Monedas", _loaded.Character.Coins.ToGameItems());
        AddContainer("ammo", "Municion", _loaded.Character.Ammo.ToGameItems());

        // Equipo puesto + los 3 loadouts reales seleccionables (Version>=269, si no
        // Loadouts.Length==0) - consolidados en una unica pantalla "Equipamiento" con
        // selector, ver EquipmentGroupViewModel (antes eran 12 pestañas planas mas aqui
        // mismo, 21 pestañas en total - pedido explicito 2-sep-2026 tras el amontonamiento
        // real al reducir la ventana: "¿es necesario que haya tantos botones?").
        EquipmentGroup = new EquipmentGroupViewModel(_service, RequestPickForSlot, _loaded.MergedContainers, _loaded.Character.Loadouts.Length);

        RebuildResearch();
    }

    private void RebuildResearch()
    {
        Research.Clear();
        if (_loaded == null) return;
        foreach (var entry in _loaded.Character.Research.OrderBy(e => e.Pid))
        {
            bool isCalamity = entry.Pid.Contains('/');
            string displayName;
            string? iconPath;
            if (isCalamity)
            {
                int slash = entry.Pid.IndexOf('/');
                string mod = entry.Pid[..slash], internalName = entry.Pid[(slash + 1)..];
                var calEntry = _service.CalamityCatalog.ByModAndInternal(mod, internalName);
                displayName = calEntry?.DisplayName ?? entry.Pid;
                iconPath = calEntry?.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + calEntry.Icon : null;
            }
            else
            {
                displayName = _service.VanillaCatalog.GetNameByKey(entry.Pid);
                int? vanillaId = _service.VanillaCatalog.GetIdByKey(entry.Pid);
                iconPath = vanillaId.HasValue ? VanillaIconResolver.GetIconPath(vanillaId.Value) : null;
            }
            Research.Add(new ResearchRowViewModel(displayName, entry.Count, isCalamity, iconPath));
        }
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

    // Auto-equipar desde el panel Builds: arma+armadura+accesorios de una clase/etapa
    // concreta se colocan directamente en el personaje cargado. Armadura -> los 3 primeros
    // slots del equipo puesto (cabeza/cuerpo/piernas), accesorios -> los 5 siguientes
    // (Items[3..7] del loadout, ver PROYECTO-TERRASAVR.md); las armas no tienen slot fijo en
    // Terraria, se colocan en el primer hueco libre del inventario. Objetos que no se
    // consigan resolver (pid no encontrado en el catalogo) o para los que no quede hueco se
    // cuentan aparte y se avisa en el mensaje de estado - nunca se sobrescribe un objeto ya
    // puesto salvo en los 3+5 slots fijos de armadura/accesorios, que es justo lo que este
    // botón promete reemplazar.
    [RelayCommand(CanExecute = nameof(IsCharacterLoaded))]
    private void AutoEquip(BuildClassGear? gear)
    {
        if (_loaded == null || gear == null || EquipmentGroup == null) return;

        var armorSlots = EquipmentGroup.EquippedItems.Slots;
        var inventorySlots = Containers.First(c => c.Key == "inventory").Slots;
        int placed = 0, skipped = 0;

        void PlaceInSlot(ItemSlotViewModel slot, BuildItemRef itemRef)
        {
            var resolved = BuildItemResolver.Resolve(itemRef, _service.VanillaCatalog, _service.CalamityCatalog, _service.VanillaPrefixCatalog);
            if (resolved == null) { skipped++; return; }
            slot.UpdateFrom(resolved);
            placed++;
        }

        for (int i = 0; i < gear.Armor.Count && i < 3; i++)
            PlaceInSlot(armorSlots[i], gear.Armor[i]);

        for (int i = 0; i < gear.Accessories.Count && i < 5; i++)
            PlaceInSlot(armorSlots[3 + i], gear.Accessories[i]);

        foreach (var weapon in gear.Weapons)
        {
            var emptySlot = inventorySlots.FirstOrDefault(s => s.IsEmpty);
            if (emptySlot == null) { skipped++; continue; }
            PlaceInSlot(emptySlot, weapon);
        }

        StatusMessage = skipped > 0
            ? $"Auto-equipar: {placed} objeto(s) colocado(s), {skipped} sin resolver o sin hueco libre - pulsa Guardar para conservarlo."
            : $"Auto-equipar: {placed} objeto(s) colocado(s) - pulsa Guardar para conservarlo.";
        SelectedTabIndex = PersonajeTabIndex;
    }

    private void AddContainer(string key, string displayName, GameItem[] items)
    {
        var slots = new ObservableCollection<ItemSlotViewModel>();
        for (int i = 0; i < items.Length; i++)
            slots.Add(new ItemSlotViewModel(_service, i, displayName, items[i], RequestPickForSlot));
        Containers.Add(new ContainerViewModel(key, displayName, slots));
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
    }
}
