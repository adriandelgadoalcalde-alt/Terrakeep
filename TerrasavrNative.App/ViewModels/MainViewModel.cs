using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
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

    // Confirmacion visual real de guardado (pedido explicito 2-sep-2026: "debe ser mas visual
    // que se allá confirmado el guardado no solamente un mensajito abajo a la izquierda") -
    // se activa un momento tras un Save() con exito y se apaga sola; StatusMessage se queda
    // para errores, que no deben ser tan efimeros. Ver el banner real en MainWindow.xaml.
    private readonly DispatcherTimer _saveConfirmationTimer = new() { Interval = TimeSpan.FromSeconds(1.5) };

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
    // Buffs=1 en el mismo TabControl interno (Objetos/Buffs/Investigacion, ver MainWindow.xaml)
    // - usado por RequestPickForBuffSlot para saltar a la pestaña correcta al "Elegir..." un
    // buff, mismo criterio que ObjetosInnerTabIndex para objetos.
    private const int BuffsInnerTabIndex = 1;

    [ObservableProperty] private string _statusMessage = "Sin personaje cargado.";
    [ObservableProperty] private string? _characterName;
    [ObservableProperty] private bool _isCharacterLoaded;
    [ObservableProperty] private bool _hasCalamityData;
    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private int _personajeInnerTabIndex;
    [ObservableProperty] private bool _saveConfirmationVisible;

    public ObservableCollection<ContainerViewModel> Containers { get; } = [];
    [ObservableProperty] private EquipmentGroupViewModel? _equipmentGroup;
    [ObservableProperty] private StorageGroupViewModel? _storageGroup;
    [ObservableProperty] private ContainerViewModel? _inventoryContainer;
    [ObservableProperty] private ContainerViewModel? _mountsContainer;
    [ObservableProperty] private ContainerViewModel? _dyesContainer;
    [ObservableProperty] private ContainerViewModel? _coinsContainer;
    [ObservableProperty] private ContainerViewModel? _ammoContainer;

    // Pregunta a Opus sobre el diseño (2-sep-2026): de los ~589px utiles de la pestaña
    // "Objetos", 270 vivian congelados en la fila de la Libreria (46% del alto) - mas robo de
    // espacio que las propias 9 pestañas. Plegada por defecto (con auto-despliegue al elegir
    // objeto, ver RequestPickForSlot) devuelve ese espacio a la cuadricula de items, que es la
    // prioridad explicita del usuario ("me gusta mucho que los objetos se vean directamente de
    // un plumazo").
    [ObservableProperty] private bool _isLibraryCollapsed = true;
    // Mismo criterio que IsLibraryCollapsed de arriba, para la Libreria de buffs (Fase 2 del
    // rework de Buffs, pregunta a Opus sobre el diseño 2-sep-2026, cuarta pasada) - plegada por
    // defecto, auto-despliegue al "Elegir..." un buff (RequestPickForBuffSlot).
    [ObservableProperty] private bool _isBuffLibraryCollapsed = true;
    public ResearchViewModel Research { get; }
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
    public BuffLibraryViewModel BuffLibrary { get; }
    public BuffEditViewModel BuffEdit { get; }
    public ItemEditViewModel ItemEdit { get; }

    public MainViewModel()
    {
        Builds = new BuildsViewModel(_service.VanillaBuilds, _service.CalamityBuilds, _service);
        WhatsNew = new WhatsNewViewModel(_service.WhatsNew);
        Changelog = new ChangelogViewModel(_service.Changelog);
        Exploration = new ExplorationViewModel(_service);
        Library = new LibraryViewModel(_service);
        Research = new ResearchViewModel(_service);
        Appearance = new AppearanceViewModel(_service);
        Library.ItemPlaced += () =>
        {
            SelectedTabIndex = PersonajeTabIndex;
            PersonajeInnerTabIndex = ObjetosInnerTabIndex;
        };
        BuffEdit = new BuffEditViewModel(_service);
        BuffLibrary = new BuffLibraryViewModel(_service);
        BuffLibrary.BuffPlaced += () =>
        {
            SelectedTabIndex = PersonajeTabIndex;
            PersonajeInnerTabIndex = BuffsInnerTabIndex;
        };
        Buffs = new BuffsViewModel(_service, RequestPickForBuffSlot);
        ItemEdit = new ItemEditViewModel(_service);
        _saveConfirmationTimer.Tick += (_, _) =>
        {
            SaveConfirmationVisible = false;
            _saveConfirmationTimer.Stop();
        };
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

    // Mismo patron que SelectSlot de arriba, para el panel "Editar buff seleccionado" (pregunta
    // a Opus sobre el diseño 2-sep-2026, cuarta pasada) - un unico buff seleccionado a la vez.
    public void SelectBuffSlot(BuffSlotViewModel slot)
    {
        if (BuffEdit.Slot != null) BuffEdit.Slot.IsSelected = false;
        slot.IsSelected = true;
        BuffEdit.Slot = slot;
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
        IsLibraryCollapsed = false; // "Elegir..." siempre debe revelar la Libreria, este por defecto plegada o no
    }

    [RelayCommand]
    private void ToggleLibraryCollapsed() => IsLibraryCollapsed = !IsLibraryCollapsed;

    // Gemelo de RequestPickForSlot de arriba, para buffs (Fase 2 del rework, pregunta a Opus
    // sobre el diseño 2-sep-2026, cuarta pasada).
    private void RequestPickForBuffSlot(BuffSlotViewModel slot)
    {
        BuffLibrary.PickTarget = slot;
        SelectBuffSlot(slot);
        SelectedTabIndex = PersonajeTabIndex;
        PersonajeInnerTabIndex = BuffsInnerTabIndex;
        IsBuffLibraryCollapsed = false;
    }

    [RelayCommand]
    private void ToggleBuffLibraryCollapsed() => IsBuffLibraryCollapsed = !IsBuffLibraryCollapsed;

    public void LoadFromPath(string plrPath)
    {
        try
        {
            Library.PickTarget = null;
            BuffLibrary.PickTarget = null;
            ItemEdit.Slot = null;
            BuffEdit.Slot = null;
            _loaded = _service.Load(plrPath);
            RebuildContainers();
            Appearance.LoadFrom(_loaded.Character);
            Servers.LoadFrom(_loaded.Character);
            Flags.LoadFrom(_loaded.Character);
            VersionEditor.LoadFrom(_loaded.Character);
            Buffs.LoadFrom(_loaded.Character);
            BuffEdit.SetCharacterVersion(_loaded.Character.Version);
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
            SaveConfirmationVisible = true;
            _saveConfirmationTimer.Stop();
            _saveConfirmationTimer.Start();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al guardar: {ex.Message}";
        }
    }

    private void RebuildContainers()
    {
        Containers.Clear();
        EquipmentGroup = null;
        StorageGroup = null;
        InventoryContainer = null;
        MountsContainer = null;
        DyesContainer = null;
        CoinsContainer = null;
        AmmoContainer = null;
        Research.Reset();
        if (_loaded == null) return;

        // Contenedores con fusion real de Calamity (mismos 7 que CalamityCharacterSync cubre).
        // Se siguen guardando TODOS en Containers (SyncEditsBackToMerged/AutoEquip los buscan
        // ahi por Key) - las referencias devueltas de mas abajo son solo para la navegacion de
        // 5 pestañas (pregunta a Opus sobre el diseño, 2-sep-2026), presentacion pura, ningun
        // dato nuevo ni fusion de colecciones.
        InventoryContainer = AddContainer("inventory", "Inventario", _loaded.MergedContainers["inventory"]);
        var bank = AddContainer("bank", "Banco", _loaded.MergedContainers["bank"]);
        var bank2 = AddContainer("bank2", "Caja fuerte", _loaded.MergedContainers["bank2"]);
        var bank3 = AddContainer("bank3", "Fragua del Defensor", _loaded.MergedContainers["bank3"]);
        var bank4 = AddContainer("bank4", "Boveda del Vacio", _loaded.MergedContainers["bank4"]);
        // columns: 1 (pregunta a Opus sobre el diseño, quinta pasada: "mascotas etc mejor en
        // vertical") - laterales de la Equipamiento fusionada, una sola columna de 5 filas.
        MountsContainer = AddContainer("miscEquips", "Mascota / Montura / Gancho", _loaded.MergedContainers["miscEquips"], columns: 1);
        DyesContainer = AddContainer("miscDyes", "Tintes (mascota/montura/gancho)", _loaded.MergedContainers["miscDyes"], columns: 1);

        // Monedas/municion: vanilla-only (ver CalamityCharacterSync.cs para el alcance
        // documentado - la app JS tampoco los sincroniza con Calamity, solo los protege).
        CoinsContainer = AddContainer("coins", "Monedas", _loaded.Character.Coins.ToGameItems());
        AmmoContainer = AddContainer("ammo", "Municion", _loaded.Character.Ammo.ToGameItems());

        StorageGroup = new StorageGroupViewModel(bank, bank2, bank3, bank4);

        // Equipo puesto + los 3 loadouts reales seleccionables (Version>=269, si no
        // Loadouts.Length==0) - consolidados en una unica pantalla "Equipamiento" con
        // selector, ver EquipmentGroupViewModel (antes eran 12 pestañas planas mas aqui
        // mismo, 21 pestañas en total - pedido explicito 2-sep-2026 tras el amontonamiento
        // real al reducir la ventana: "¿es necesario que haya tantos botones?").
        EquipmentGroup = new EquipmentGroupViewModel(_service, RequestPickForSlot, _loaded.MergedContainers, _loaded.Character.Loadouts.Length);

        Research.LoadFrom(_loaded.Character);
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

        Research.LoadFrom(_loaded.Character);
        StatusMessage = $"Investigacion completa aplicada ({_loaded.Character.Research.Count} objetos) - pulsa Guardar para conservarlo.";
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

    // Los primeros 10 slots reales de "inventory" son la barra rapida (Player.inventory[0..9]
    // en el propio Terraria - confirmado en Player.cs decompilado, "Hotbar1".."Hotbar0" son 10
    // triggers reales) - contorno verde de "equipado" tambien ahi, igual que en Equipamiento
    // (pedido explicito 2-sep-2026, confirmado con el usuario tras preguntar: el .plr no
    // guarda ningun campo real de "arma empuñada ahora mismo" fiable, asi que la barra rapida
    // entera es el criterio, no un unico slot).
    private const int HotbarSlotCount = 10;

    // columns (pregunta a Opus sobre el diseño, quinta pasada - fusion de Equipamiento con
    // Monturas/Monedas como laterales): default 10 para los contenedores de siempre; los
    // laterales que se quieren verticales (Mascota/Montura/Gancho, Tintes) pasan columns: 1.
    private ContainerViewModel AddContainer(string key, string displayName, GameItem[] items, int columns = 10)
    {
        var slots = new ObservableCollection<ItemSlotViewModel>();
        for (int i = 0; i < items.Length; i++)
        {
            bool isEquipped = key == "inventory" && i < HotbarSlotCount;
            slots.Add(new ItemSlotViewModel(_service, i, displayName, items[i], RequestPickForSlot, isEquipped));
        }
        var container = new ContainerViewModel(key, displayName, slots) { Columns = columns };
        Containers.Add(container);
        return container;
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
