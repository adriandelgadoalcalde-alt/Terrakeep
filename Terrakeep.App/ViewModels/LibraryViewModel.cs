using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Terrakeep.App.Services;
using Terrakeep.Core.Data;
using Terrakeep.Core.Model;

namespace Terrakeep.App.ViewModels;

// Libreria/Buscador (Fase 4, ampliada 1-sep-2026 con arbol de carpetas real - pedido explicito
// de inspirarse en la Libreria real de Terrasavr). Dos formas de encontrar un objeto, que se
// pueden combinar: buscar por texto sobre el catalogo COMPLETO (vanilla + Calamity, ~8200
// objetos), o navegar el arbol de categorias reales (Category de CalamityCatalogEntry para
// Calamity, VanillaCategoryCatalog - extraido de Item.cs decompilado real, ver
// scripts/extraer-categorias-vanilla.py - para vanilla) y elegir una carpeta. Solo se
// renderizan los primeros N resultados a la vez (rendimiento con WrapPanel sin virtualizar).
//
// Tambien hace de selector de objetos: cuando un ItemSlotViewModel pide "elegir objeto"
// (boton en un slot vacio o "cambiar objeto" en uno lleno), MainViewModel pone ese slot en
// PickTarget y cambia la pestaña activa a esta - al pulsar una tarjeta aqui con PickTarget
// puesto, el objeto se coloca en ese slot y se dispara ItemPlaced para volver a Personaje.
//
// H5-15 (quinta auditoria de Opus): arbol de categorias, busqueda con debounce y el par
// SelectCategory/ClearCategory ya no viven aqui - ver CatalogBrowserViewModel, base real
// compartida con BuffLibraryViewModel/ResearchViewModel.
public partial class LibraryViewModel : CatalogBrowserViewModel<LibraryItemViewModel>
{
    // L-c (segunda auditoria de Opus, Fable): "el tope de 300 no tiene ninguna medicion real
    // detras, solo el motivo generico de que WrapPanel no virtualiza". Medido de verdad con el
    // arnes UIA (busqueda amplia real, "ar", sobre el catalogo completo ~8469 objetos, Debug
    // primera pasada): 100 objetos -> 158ms, 150 -> 271ms, 300 -> 802ms real - NO escala lineal
    // (WrapPanel sin virtualizar empeora peor que proporcional al crecer), 300 era un freeze
    // real y perceptible tecleando. 100 es el punto real donde el reflow deja de notarse de
    // verdad manteniendo un numero de resultados util antes de pedir afinar la busqueda.
    private const int MaxResults = 100;

    private readonly List<LibraryItemViewModel> _all;
    private readonly Dictionary<int, LibraryItemViewModel> _byId;

    // Filtros combinables (encargo del usuario, 13-sep-2026: "amplia la busqueda con filtros
    // combinables - tipo de objeto, rareza, si es equipable en que slot"). Tres grupos, cada uno
    // OR entre sus propias pastillas seleccionadas (elegir "Azul" y "Verde" enseña las dos
    // rarezas) y AND entre grupos distintos (elegir una rareza Y una ranura de equipo exige
    // ambas a la vez) - mismo lenguaje real de filtro facetado que cualquier tienda, y se
    // combinan ademas con el texto de busqueda y la carpeta elegida, no los sustituyen.
    // Dominio FIJO de cada grupo (no "lo que aparezca en el catalogo"): son los tramos reales
    // del propio juego (11 rarezas + blanca, 5 tipos de daño, 12 ranuras de SlotKind), no algo
    // que pueda variar entre pasadas - ver los comentarios de cada array mas abajo.
    private static readonly (int Key, string LocKey)[] RarityDomain =
    [
        (-1, "library_filter_rarity_gray"),
        (0, "library_filter_rarity_white"),
        (1, "library_filter_rarity_blue"),
        (2, "library_filter_rarity_green"),
        (3, "library_filter_rarity_orange"),
        (4, "library_filter_rarity_red"),
        (5, "library_filter_rarity_pink"),
        (6, "library_filter_rarity_purple"),
        (7, "library_filter_rarity_lime"),
        (8, "library_filter_rarity_yellow"),
        (9, "library_filter_rarity_cyan"),
        (-11, "library_filter_rarity_amber"),
    ];

    private static readonly (ItemDamageKind Kind, string LocKey)[] DamageKindDomain =
    [
        (ItemDamageKind.Melee, "library_filter_damage_melee"),
        (ItemDamageKind.Ranged, "library_filter_damage_ranged"),
        (ItemDamageKind.Magic, "library_filter_damage_magic"),
        (ItemDamageKind.Summon, "library_filter_damage_summon"),
        (ItemDamageKind.Rogue, "library_filter_damage_rogue"),
    ];

    // Rotulos: donde ya existe un rotulo real de una sola palabra (ItemSlotViewModel.
    // SlotRoleLabel/char_dye_label) se reutiliza tal cual - solo "Accesorio" (sin numero, a
    // diferencia de "Accesorio {0}" que ya usa ese slot en el personaje) es una clave nueva.
    private static readonly (SlotKind Kind, string LocKey)[] EquipSlotDomain =
    [
        (SlotKind.ArmorHead, "slot_role_head"),
        (SlotKind.ArmorBody, "slot_role_body"),
        (SlotKind.ArmorLegs, "slot_role_legs"),
        (SlotKind.Accessory, "library_filter_accessory"),
        (SlotKind.Dye, "char_dye_label"),
        (SlotKind.Ammo, "slot_role_ammo"),
        (SlotKind.Coin, "slot_role_coin"),
        (SlotKind.Hook, "slot_role_hook"),
        (SlotKind.Mount, "slot_role_mount"),
        (SlotKind.Cart, "slot_role_cart"),
        (SlotKind.VanityPet, "slot_role_vanity_pet"),
        (SlotKind.LightPet, "slot_role_light_pet"),
    ];

    public ObservableCollection<LibraryFilterChipViewModel<int>> RarityChips { get; } = [];
    public ObservableCollection<LibraryFilterChipViewModel<ItemDamageKind>> DamageKindChips { get; } = [];
    public ObservableCollection<LibraryFilterChipViewModel<SlotKind>> EquipSlotChips { get; } = [];

    // Popup anclado al boton "Filtros" (mismo mecanismo real que "¿Donde lo tengo?" -
    // WhereIsItPopup en MainWindow.xaml - StaysOpen=False, se cierra solo con un clic fuera).
    [ObservableProperty] private bool _isFiltersOpen;

    public int ActiveFilterCount =>
        RarityChips.Count(c => c.IsSelected) + DamageKindChips.Count(c => c.IsSelected) + EquipSlotChips.Count(c => c.IsSelected);
    public bool HasActiveFilters => ActiveFilterCount > 0;

    [ObservableProperty] private ItemSlotViewModel? _pickTarget;

    // L-b (segunda auditoria de Opus, Fable): "el aviso de 'solo validos para el slot
    // seleccionado' es un texto mas dentro de ResultsSummary, facil de pasar por alto - y ni
    // siquiera dice CUAL slot". PickTarget (el mismo ItemSlotViewModel que abrio el selector,
    // ver RequestPickForSlot en MainViewModel) ya conoce su propio rol real (SlotRoleLabel -
    // "Cabeza"/"Accesorio 3"/"Tinte"...) - una pildora real y separada, no un sufijo de frase.
    [ObservableProperty] private string? _slotRestrictionLabel;

    public bool IsPicking => PickTarget != null;

    // H5-13 (quinta auditoria de Opus): igual que la base, salvo que un slot restringido como
    // destino (PickTarget con AcceptedKind real, ej. "Tinte"/"Gancho") ya reduce el catalogo a
    // un conjunto pequeño y util de ver de inmediato (ver hasSlotRestriction en ApplyFilter) -
    // mostrar las carpetas raiz genericas ahi seria un paso atras, no un atajo.
    // Ampliado 13-sep-2026: con cualquier filtro de pastilla activo (Rareza/Tipo de daño/Ranura)
    // el catalogo YA esta reducido a algo util de ver de inmediato, igual que con una restriccion
    // de slot - mostrar las carpetas raiz genericas en su lugar escondería el propio filtro que
    // el usuario acaba de marcar (bug real que se hubiera visto en la primera prueba: marcar
    // "Azul" sin escribir nada ni elegir carpeta no cambiaba la pantalla).
    public override bool ShowRootCategoryCards => base.ShowRootCategoryCards &&
        !(PickTarget != null && PickTarget.AcceptedKind != Terrakeep.Core.Model.SlotKind.None) &&
        !HasActiveFilters;

    public event Action? ItemPlaced;

    public LibraryViewModel(CharacterFileService service)
    {
        _all = [];
        _byId = new Dictionary<int, LibraryItemViewModel>();

        // Los DOS nombres reales por entrada (ronda de traduccion del CONTENIDO del juego,
        // 6-sep-2026): la tarjeta elige al leer segun el idioma activo. Se piden por idioma
        // EXPLICITO, no por el activo, precisamente porque este catalogo se construye una sola
        // vez al arrancar y el idioma puede cambiar mil veces despues.
        foreach (var (id, name) in service.VanillaCatalog.AllEntries(LocalizedContent.Spanish))
        {
            string nameEn = service.VanillaCatalog.GetName(id, LocalizedContent.English);
            var stats = ItemStatsFormatter.Describe(false, id, service.TooltipCatalogs);
            var rarityColor = VanillaRarityColorCatalog.Get(service.VanillaStats.Get(id)?.Rare);
            var equipSlotKind = ItemEquipSlotClassifier.Classify(id, false, service);
            var item = new LibraryItemViewModel(name, false, VanillaIconResolver.GetIconPath(id), id, service.VanillaCategories.GetCategory(id), stats, rarityColor, nameEn, equipSlotKind);
            _all.Add(item);
            _byId[id] = item;
        }

        foreach (var entry in service.CalamityCatalog.Entries)
        {
            string? iconPath = entry.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + entry.Icon : null;
            var stats = ItemStatsFormatter.Describe(true, entry.SyntheticId, service.TooltipCatalogs);
            var equipSlotKind = ItemEquipSlotClassifier.Classify(entry.SyntheticId, true, service);
            var item = new LibraryItemViewModel(entry.DisplayNameFor(LocalizedContent.Spanish), true, iconPath, entry.SyntheticId, entry.Category, stats,
                displayNameEn: entry.DisplayNameFor(LocalizedContent.English), equipSlotKind: equipSlotKind);
            _all.Add(item);
            _byId[entry.SyntheticId] = item;
        }

        // Pastillas de filtro: dominio fijo (ver los arrays de arriba), color real solo en
        // Rareza (VanillaRarityColorCatalog - null para lo que de verdad no tiene un color fijo,
        // igual que RarityBrush de cada tarjeta).
        foreach (var (key, locKey) in RarityDomain)
        {
            var rgb = VanillaRarityColorCatalog.Get(key);
            Brush? swatch = rgb is { } c ? new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B)) : null;
            RarityChips.Add(new LibraryFilterChipViewModel<int>(key, locKey, swatch));
        }
        foreach (var (kind, locKey) in DamageKindDomain)
            DamageKindChips.Add(new LibraryFilterChipViewModel<ItemDamageKind>(kind, locKey));
        foreach (var (kind, locKey) in EquipSlotDomain)
            EquipSlotChips.Add(new LibraryFilterChipViewModel<SlotKind>(kind, locKey));

        // Arbol real de Terrasavr (vanilla + carpeta madre "Calamity (mod)") - compartido con
        // la pestaña Investigacion, ver LibraryCategoryTreeBuilder (mismo pedido explicito
        // 2-sep-2026 para ambas: "quiero que calques exactamente la estructura de carpetas
        // orden y organizacion de terrasav para esta librera Y investigacion").
        foreach (var node in LibraryCategoryTreeBuilder.Build(service))
            RootCategories.Add(node);
        // Auditoria de Opus, T-18: cada nodo lleva su propio comando real - ver el comentario
        // real en CategoryNodeViewModel.SelectCommand.
        CategoryNodeViewModel.AssignSelectCommand(RootCategories, SelectCategoryCommand);

        // Ronda de idioma del 6-sep-2026: el tooltip de estadisticas de cada tarjeta se redacta
        // al leerlo, asi que al cambiar de idioma basta con avisar. UNA sola suscripcion aqui que
        // reparte a las 8821 entradas (LibraryItemViewModel.RefrescarIdioma) - una suscripcion
        // por entrada seria un derroche. Evento DEBIL, mismo motivo real que
        // LocalizedContentViewModel: LocalizationService.Instance es un singleton que vive lo que
        // la aplicacion y este ViewModel no.
        System.ComponentModel.PropertyChangedEventManager.AddHandler(
            LocalizationService.Instance, OnIdiomaCambiadoRefrescarTarjetas, "Item[]");

        ApplyFilter();
    }

    private void OnIdiomaCambiadoRefrescarTarjetas(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        foreach (var item in _all) item.RefrescarIdioma();
        foreach (var chip in RarityChips) chip.RefrescarIdioma();
        foreach (var chip in DamageKindChips) chip.RefrescarIdioma();
        foreach (var chip in EquipSlotChips) chip.RefrescarIdioma();
    }

    protected override void ApplyFilter()
    {
        Results.Clear();

        // Bug real encontrado y corregido 2-sep-2026 (pedido explicito: "reordenar todos los
        // ítems... para que coincidan 100 por 100 de como lo tenemos en terrasav"): filtrar
        // _all por ItemIdSet.Contains (un HashSet, sin orden garantizado) daba el orden
        // arbitrario del catalogo completo, no el orden curado real de Terrasavr. Con una
        // carpeta elegida se recorre ItemIdsOrdered (que SI respeta ese orden real) y se
        // resuelve cada id por diccionario - sin carpeta elegida (busqueda libre sobre todo
        // el catalogo) no hay ningun orden real de Terrasavr al que igualar, se deja _all.
        bool hasSearch = !string.IsNullOrWhiteSpace(SearchText);
        // Restriccion de slot (consulta a Opus, sexta pasada): con un slot restringido como
        // destino, el catalogo se reduce a lo valido ANTES de aplicar busqueda/carpeta -
        // conjuntos tipicamente pequeños (4 monedas, ~20 ganchos, ~100 tintes), seguro
        // mostrarlos de inmediato sin exigir una busqueda primero, a diferencia del catalogo
        // completo sin restriccion (~8200 objetos).
        var target = PickTarget;
        bool hasSlotRestriction = target != null && target.AcceptedKind != Terrakeep.Core.Model.SlotKind.None;
        // L-b: rol real del slot ("Cabeza"/"Accesorio 3"/"Tinte"...) para la pildora - null
        // cuando no hay restriccion, oculta la pildora en el XAML (NullToCollapsed).
        // Ronda de idioma del 6-sep-2026: el respaldo "este slot" iba a pelo (se ve en la pildora
        // real "Solo objetos validos para: ..." cuando el slot no tiene un rol con nombre propio).
        SlotRestrictionLabel = hasSlotRestriction
            ? target!.SlotRoleLabel ?? Services.LocalizationService.Instance["library_this_slot"]
            : null;

        // Bug real reportado 2-sep-2026 (KeyNotFoundException, id 5462, al elegir "Armas"):
        // el arbol real de la Libreria (extraido de Terrasavr) referencia algun id que no
        // esta en el catalogo de nombres vanilla (VanillaItemCatalog, cobertura no al 100% -
        // ver su propio comentario "los ~13 objetos sin traduccion real"). Un id que el arbol
        // conoce pero el catalogo no se descarta en silencio en vez de tumbar la app entera -
        // mismo criterio de "lo que no se encuentra no se inventa", aplicado aqui a
        // "lo que no se encuentra no rompe nada".
        IEnumerable<LibraryItemViewModel> matches = SelectedCategory != null
            ? SelectedCategory.ItemIdsOrdered.Select(id => _byId.GetValueOrDefault(id)).OfType<LibraryItemViewModel>()
            : _all;

        if (hasSlotRestriction)
            matches = matches.Where(i => target!.AcceptsItem(i.Id));

        // L-a (segunda auditoria de Opus, Fable): gramatica de busqueda real de Terrasavr,
        // recuperada de a0f5027 (revertida sin querer junto al rediseño de navegacion rechazado
        // en c38c960 - esta parte no tocaba navegacion, era segura de recuperar). Coma=OR,
        // espacio=AND, "#id"/"#a-b" por id, ".texto" en el tooltip real (StatsTooltip) en vez
        // del nombre. Ver LibrarySearchGrammar.
        if (hasSearch)
        {
            string query = SearchText;
            // C-09 (informe de pulido final, cierra L2): NameFolded/TooltipFolded ya vienen
            // plegados de fabrica (LibraryItemViewModel) - ni ToLowerInvariant ni Fold aqui, en
            // cada pulsacion, sobre las 8821 entradas reales del catalogo.
            matches = matches.Where(i => LibrarySearchGrammar.Matches(
                query, i.Id, i.NameFolded, i.TooltipFolded));
        }

        // Filtros de pastilla (13-sep-2026). Rareza es SOLO vanilla a proposito: Calamity nunca
        // tiene un Rarity real (ver el comentario de LibraryItemViewModel.Rarity) - un objeto de
        // Calamity con la pastilla "Blanca" marcada seria fingir un dato que no existe, asi que
        // se excluye del todo en vez de caer en ese cajon por defecto.
        var selectedRarities = RarityChips.Where(c => c.IsSelected).Select(c => c.Value).ToHashSet();
        if (selectedRarities.Count > 0)
            matches = matches.Where(i => !i.IsCalamity && selectedRarities.Contains(i.Rarity ?? 0));

        var selectedDamageKinds = DamageKindChips.Where(c => c.IsSelected).Select(c => c.Value).ToHashSet();
        if (selectedDamageKinds.Count > 0)
            matches = matches.Where(i => selectedDamageKinds.Contains(i.DamageKind));

        var selectedEquipMask = EquipSlotChips.Where(c => c.IsSelected)
            .Aggregate(Terrakeep.Core.Model.SlotKind.None, (acc, c) => acc | c.Value);
        if (selectedEquipMask != Terrakeep.Core.Model.SlotKind.None)
            matches = matches.Where(i => (i.EquipSlotKind & selectedEquipMask) != 0);

        if (ShowRootCategoryCards)
        {
            ResultsSummary = LocalizationService.Instance.Format("library_summary_all", _all.Count);
            return;
        }

        var list = matches.ToList();
        foreach (var item in list.Take(MaxResults)) Results.Add(item);

        // L-b: la restriccion de slot ya la dice la pildora aparte (mas visible, con el rol
        // real del slot) - ResultsSummary ya no repite un "válidos para este slot" generico.
        string categoryLabel = SelectedCategory != null ? LocalizationService.Instance.Format("library_summary_in_category", SelectedCategory.Name) : string.Empty;
        ResultsSummary = list.Count > MaxResults
            ? LocalizationService.Instance.Format("library_summary_showing", MaxResults, list.Count, categoryLabel)
            : LocalizationService.Instance.Format("library_summary_count", list.Count, categoryLabel);
    }

    partial void OnPickTargetChanged(ItemSlotViewModel? value)
    {
        OnPropertyChanged(nameof(IsPicking));
        OnPropertyChanged(nameof(ShowRootCategoryCards)); // H5-13: la restriccion de slot (o su ausencia) cambia esta condicion
        // Capa principal de prevencion de la restriccion de slot (consulta a Opus, sexta
        // pasada: "cero chrome nuevo, cero popups... el problema deja de existir en el 90% de
        // los casos, el usuario nunca ve un objeto invalido que poder elegir") - al abrir el
        // selector para un slot restringido, refiltra de inmediato.
        ApplyFilter();
    }

    [RelayCommand]
    private void ToggleFiltersOpen() => IsFiltersOpen = !IsFiltersOpen;

    // Tres comandos en vez de uno generico con cast (CommandParameter llega ya tipado desde el
    // DataTemplate real de cada grupo en MainWindow.xaml - ver LibraryFilterChipViewModel<T>).
    [RelayCommand]
    private void ToggleRarityChip(LibraryFilterChipViewModel<int> chip) => ToggleChip(chip);

    [RelayCommand]
    private void ToggleDamageKindChip(LibraryFilterChipViewModel<ItemDamageKind> chip) => ToggleChip(chip);

    [RelayCommand]
    private void ToggleEquipSlotChip(LibraryFilterChipViewModel<SlotKind> chip) => ToggleChip(chip);

    private void ToggleChip<T>(LibraryFilterChipViewModel<T> chip) where T : notnull
    {
        chip.IsSelected = !chip.IsSelected;
        RaiseFilterCountChanged();
        ApplyFilter();
    }

    [RelayCommand]
    private void ClearFilters()
    {
        bool changed = false;
        foreach (var c in RarityChips) { if (c.IsSelected) { c.IsSelected = false; changed = true; } }
        foreach (var c in DamageKindChips) { if (c.IsSelected) { c.IsSelected = false; changed = true; } }
        foreach (var c in EquipSlotChips) { if (c.IsSelected) { c.IsSelected = false; changed = true; } }
        if (!changed) return;
        RaiseFilterCountChanged();
        ApplyFilter();
    }

    private void RaiseFilterCountChanged()
    {
        OnPropertyChanged(nameof(ActiveFilterCount));
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(ShowRootCategoryCards));
    }

    [RelayCommand]
    private void PlaceInTarget(LibraryItemViewModel entry)
    {
        if (PickTarget == null) return;
        PickTarget.PlaceItem(entry.Id);
        PickTarget = null;
        ItemPlaced?.Invoke();
    }

    [RelayCommand]
    private void CancelPick() => PickTarget = null;
}
