using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Calamity;
using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App.ViewModels;

// Panel "Editar" compartido - equivalente real de app.TabEdit de Terrasavr: acompaña al
// slot SELECCIONADO (no uno fijo) en cualquier contenedor (Inventario/Banco/Caja fuerte/
// Fragua/Boveda/Mascotas/Loadouts...), pedido explicito 1-sep-2026 tras comparar con
// capturas reales de Terrasavr ("esa ventana naranja acompaña desde equipamiento hasta
// forja del vacio"). Deliberadamente delgado: Nombre/Indice/Contar/Prefijo se enlazan
// DIRECTAMENTE a Slot.* en el XAML (ItemSlotViewModel ya los tiene editables) - esta clase
// solo añade el selector de prefijo categorizado, que no tenia sitio en ItemSlotViewModel.
public partial class ItemEditViewModel : ObservableObject
{
    // Bloque de idioma (pedido explicito del usuario, 5-sep-2026): esta clase se usa como DataContext dentro de una plantilla/menu/tooltip (ContextMenu y ToolTip son popups, no alcanzables con RelativeSource AncestorType=Window) - exponer Loc aqui directamente, igual que MainViewModel, evita esa complicacion: {Binding Loc[clave]} se resuelve contra ESTE objeto sin ningun truco de RelativeSource/PlacementTarget.
    public Services.LocalizationService Loc => Services.LocalizationService.Instance;

    private readonly CharacterFileService _service;
    private PrefixCategory _currentCategories = PrefixCategory.None;

    [ObservableProperty] private ItemSlotViewModel? _slot;
    [ObservableProperty] private PrefixMetaButtonViewModel? _selectedMeta;
    [ObservableProperty] private PrefixGroupButtonViewModel? _selectedGroup;
    [ObservableProperty] private bool _canHavePrefix;
    [ObservableProperty] private string _noPrefixMessage = LocalizationService.Instance["select_slot_to_edit"];
    [ObservableProperty] private string _categoriesLabel = string.Empty;
    // Slot seleccionado y con un objeto real dentro - controla si se muestran los campos
    // Indice/Contar/Prefijo (no tiene sentido editarlos sobre un slot vacio o sin seleccion).
    [ObservableProperty] private bool _hasSelection;

    public ObservableCollection<PrefixMetaButtonViewModel> Metas { get; }
    public ObservableCollection<PrefixGroupButtonViewModel> Groups { get; } = [];
    public ObservableCollection<PrefixCatalogEntryViewModel> Prefixes { get; } = [];

    public ItemEditViewModel(CharacterFileService service)
    {
        _service = service;
        Metas = new ObservableCollection<PrefixMetaButtonViewModel>(
            PrefixGroupCatalog.Metas.Select(m => new PrefixMetaButtonViewModel(m)));
        // Ronda de idioma del 6-sep-2026: los textos de este panel (NoPrefixMessage,
        // CategoriesLabel) se fijaban al construir y solo se recalculaban al CAMBIAR de slot -
        // "Selecciona un slot para editarlo." se quedaba en español con la app en ingles hasta
        // que el usuario tocaba algo. Refresh() ya sabe recomponerlos todos: basta con volver a
        // llamarlo al cambiar de idioma (evento debil, mismo motivo que en el resto de la ronda).
        System.ComponentModel.PropertyChangedEventManager.AddHandler(
            LocalizationService.Instance, OnIdiomaCambiado, "Item[]");
        Refresh();
    }

    private void OnIdiomaCambiado(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => Refresh();

    partial void OnSlotChanged(ItemSlotViewModel? oldValue, ItemSlotViewModel? newValue)
    {
        if (oldValue != null) oldValue.PropertyChanged -= OnSlotPropertyChanged;
        if (newValue != null) newValue.PropertyChanged += OnSlotPropertyChanged;
        Refresh();
    }

    // Cualquier cambio del slot (objeto colocado/cambiado por Indice o Libreria, prefijo
    // cambiado a mano, vaciado...) puede volver a cambiar que categorias/grupos aplican -
    // reconstruir es barato (como mucho 19 prefijos), asi que no hace falta filtrar por
    // que propiedad exacta cambio.
    //
    // T-I (segunda auditoria de Opus, Fable): salvo `IsSelected`/`JustEdited` - ninguno de los
    // dos cambia que prefijos aplican (son solo visuales, seleccion en la rejilla y el flash
    // real de "acabo de editarse"), y reconstruir Groups/Prefixes en cada uno hacia parpadear
    // la rejilla de prefijos sin motivo real cada vez que el propio slot editado terminaba su
    // flash (~600ms despues) - mismo bug real ya cerrado en EquipmentGroupViewModel (B-6) y ya
    // evitado desde el principio en BuffEditViewModel.
    // H3-03 (tercera auditoria, Fable): RejectionMessage tambien excluido, por coherencia -
    // una colocacion rechazada no cambia el objeto del slot, reconstruir la rejilla de
    // prefijos por ese motivo seria el mismo parpadeo sin sentido que T-I ya cerro arriba.
    private void OnSlotPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ItemSlotViewModel.IsSelected) or nameof(ItemSlotViewModel.JustEdited) or nameof(ItemSlotViewModel.RejectionMessage)) return;
        Refresh();
    }

    private void Refresh()
    {
        var slot = Slot;
        HasSelection = slot != null && !slot.IsEmpty;
        if (slot == null || slot.IsEmpty)
        {
            CanHavePrefix = false;
            NoPrefixMessage = slot == null ? LocalizationService.Instance["select_slot_to_edit"] : LocalizationService.Instance["slot_empty"];
            CategoriesLabel = string.Empty;
            _currentCategories = PrefixCategory.None;
            Groups.Clear();
            Prefixes.Clear();
            return;
        }

        _currentCategories = PrefixEligibility.For(slot.Item, _service.PrefixRules, _service.CalamityCatalog);
        CanHavePrefix = _currentCategories != PrefixCategory.None;
        NoPrefixMessage = CanHavePrefix ? string.Empty : LocalizationService.Instance["item_no_prefix_allowed"];
        CategoriesLabel = DescribeCategories(_currentCategories);

        if (!CanHavePrefix)
        {
            Groups.Clear();
            Prefixes.Clear();
            return;
        }

        // Si la meta seleccionada ya no aplica a este objeto, salta a la primera que si -
        // equivalente real de btMeta[0].click() al cambiar de item en app.TabEdit.
        var applicableMeta = SelectedMeta != null && HasApplicableGroups(SelectedMeta)
            ? SelectedMeta
            : Metas.FirstOrDefault(HasApplicableGroups);

        if (ReferenceEquals(applicableMeta, SelectedMeta)) RebuildGroups();
        else SelectedMeta = applicableMeta; // dispara OnSelectedMetaChanged -> RebuildGroups
    }

    private bool HasApplicableGroups(PrefixMetaButtonViewModel meta)
    {
        var slot = Slot!;
        return PrefixGroupCatalog.GroupsFor(meta.Meta, _currentCategories, slot.IsCalamity, slot.Item.Id, _service.PrefixRules).Any();
    }

    [RelayCommand]
    private void SelectMeta(PrefixMetaButtonViewModel? meta)
    {
        if (meta == null || ReferenceEquals(meta, SelectedMeta)) return;
        SelectedMeta = meta;
    }

    partial void OnSelectedMetaChanged(PrefixMetaButtonViewModel? oldValue, PrefixMetaButtonViewModel? newValue)
    {
        if (oldValue != null) oldValue.IsSelected = false;
        if (newValue != null) newValue.IsSelected = true;
        RebuildGroups();
    }

    [RelayCommand]
    private void SelectGroup(PrefixGroupButtonViewModel? group)
    {
        if (group == null || ReferenceEquals(group, SelectedGroup)) return;
        SelectedGroup = group;
    }

    partial void OnSelectedGroupChanged(PrefixGroupButtonViewModel? oldValue, PrefixGroupButtonViewModel? newValue)
    {
        if (oldValue != null) oldValue.IsSelected = false;
        if (newValue != null) newValue.IsSelected = true;
        RebuildPrefixes();
    }

    // H3-05 (tercera auditoria de Opus, Fable): descubierto escribiendo la prueba real de
    // aplicar un prefijo Picaro - `Groups` se reconstruye ENTERO (Clear + new
    // PrefixGroupButtonViewModel) en cada `Refresh()` (dispara con cualquier cambio del slot,
    // incluido aplicar un prefijo desde el propio picker), y comparaba el grupo "que ya estaba
    // seleccionado" por REFERENCIA del wrapper - un wrapper siempre NUEVO nunca es el mismo
    // objeto que el anterior, asi que cualquier Aplicar (o cualquier otro cambio del slot)
    // devolvia el picker en silencio al primer grupo de la meta ("Accesorio"), perdiendo la
    // seleccion real del usuario (ej. elegir "Pícaro", aplicar "Vicioso" - el picker saltaba a
    // "Accesorio" antes de que RebuildPrefixes terminara, el prefijo recien aplicado ni
    // siquiera se veia marcado como actual). `PrefixGroup` (el dato real, no el wrapper) SI es
    // la misma instancia siempre (viene sin copiar de `PrefixGroupCatalog.Metas`) - comparar
    // por ahi conserva la seleccion real entre reconstrucciones.
    private void RebuildGroups()
    {
        var previousGroup = SelectedGroup?.Group;
        Groups.Clear();
        var slot = Slot;
        if (slot == null || SelectedMeta == null) { Prefixes.Clear(); return; }

        foreach (var g in PrefixGroupCatalog.GroupsFor(SelectedMeta.Meta, _currentCategories, slot.IsCalamity, slot.Item.Id, _service.PrefixRules))
            Groups.Add(new PrefixGroupButtonViewModel(g));

        var keptGroup = previousGroup != null ? Groups.FirstOrDefault(g => ReferenceEquals(g.Group, previousGroup)) : null;
        var targetGroup = keptGroup ?? Groups.FirstOrDefault();
        if (ReferenceEquals(targetGroup, SelectedGroup)) RebuildPrefixes();
        else SelectedGroup = targetGroup;
    }

    private void RebuildPrefixes()
    {
        Prefixes.Clear();
        var slot = Slot;
        var group = SelectedGroup;
        if (slot == null || group == null) return;

        foreach (int id in PrefixGroupCatalog.PrefixIdsFor(group.Group, slot.IsCalamity, slot.Item.Id, _service.PrefixRules))
        {
            // H3-05 (tercera auditoria de Opus, Fable): id >= PrefixIdBase (10000) es un
            // prefijo REAL sintetico de Calamity (RoguePrefixCatalog, 21 ModPrefix reales -
            // ver el grupo "Pícaro" y los 4 añadidos a "Accesorio") - se resuelve/representa
            // distinto de un PrefixID vanilla plano (byte, campo `prefix` del NBT).
            if (id >= CalamityIds.PrefixIdBase)
            {
                var rogueEntry = _service.RoguePrefixCatalog.ById(id);
                string rogueName = rogueEntry?.Es ?? rogueEntry?.En ?? $"Prefijo #{id}";
                bool rogueIsCurrent = slot.Item.Prefix.IsCalamity && slot.Item.Prefix.SyntheticId == id;
                // Accesorio (10017-10020): texto real ya formateado en el propio catalogo.
                // Arma (10000-10016): numeros reales de verdad (DescribeWeaponEffect), mismo
                // criterio D-6/H3-08 de nunca dejar un nombre opaco sin lo que hace.
                string? rogueEffect = rogueEntry == null ? null
                    : rogueEntry.Effect ?? RoguePrefixCatalog.DescribeWeaponEffect(rogueEntry);
                Prefixes.Add(new PrefixCatalogEntryViewModel(rogueName, ItemPrefix.CalamitySynthetic(id), isCalamity: true, rogueIsCurrent, rogueEffect));
                continue;
            }

            var entry = _service.VanillaPrefixCatalog.ById(id);
            string name = entry?.Es ?? entry?.En ?? $"Prefijo #{id}";
            // PrefixID.Count real == 85 (ver Fase 2/bitacora): 85-97 no son vanilla, son los
            // de invocacion que añade Calamity sobre bytes libres del mismo campo prefix.
            bool isCalamityPrefix = id >= 85;
            bool isCurrent = !slot.Item.Prefix.IsCalamity && slot.Item.Prefix.VanillaId == id;
            // Auditoria de Opus, D-6: efecto real del prefijo (numeros reales, no un nombre
            // opaco) - null para los de Calamity, ver PrefixCatalogEntryViewModel.
            string? effect = isCalamityPrefix ? null : _service.PrefixEffects.Describe(id);
            Prefixes.Add(new PrefixCatalogEntryViewModel(name, ItemPrefix.Vanilla((byte)id), isCalamityPrefix, isCurrent, effect));
        }
    }

    [RelayCommand]
    private void ApplyPrefix(PrefixCatalogEntryViewModel? entry)
    {
        if (Slot == null || entry == null) return;
        Slot.SetPrefix(entry.Prefix);
        RebuildPrefixes();
    }

    [RelayCommand]
    private void ClearPrefix()
    {
        if (Slot == null) return;
        Slot.SetPrefix(ItemPrefix.None);
        RebuildPrefixes();
    }

    [RelayCommand]
    private void ApplyBestPrefix()
    {
        if (Slot == null || !Slot.ApplyBestPrefixCommand.CanExecute(null)) return;
        Slot.ApplyBestPrefixCommand.Execute(null);
        RebuildPrefixes();
    }

    // Ronda de idioma del 6-sep-2026: las 5 categorias iban a pelo en español, aunque 4 de las 5
    // claves ("class_melee"/"class_ranged"/"class_mage"/"class_summoner") YA existian en los dos
    // diccionarios - este texto se ve en el tooltip real de cada prefijo del panel Editar.
    // "prefix_cat_accessory" es la unica clave nueva: aqui "Accesorio" es un TIPO de objeto, no
    // una clase de juego, y no tiene por que traducirse igual en otro idioma.
    private static string DescribeCategories(PrefixCategory cats)
    {
        var loc = Services.LocalizationService.Instance;
        var parts = new List<string>();
        if (cats.HasFlag(PrefixCategory.Melee)) parts.Add(loc["class_melee"]);
        if (cats.HasFlag(PrefixCategory.Ranged)) parts.Add(loc["class_ranged"]);
        if (cats.HasFlag(PrefixCategory.Magic)) parts.Add(loc["class_mage"]);
        if (cats.HasFlag(PrefixCategory.Summon)) parts.Add(loc["class_summoner"]);
        if (cats.HasFlag(PrefixCategory.Accessory)) parts.Add(loc["prefix_cat_accessory"]);
        return string.Join(" · ", parts);
    }
}
