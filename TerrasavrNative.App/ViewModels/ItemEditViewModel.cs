using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
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
    private readonly CharacterFileService _service;
    private PrefixCategory _currentCategories = PrefixCategory.None;

    [ObservableProperty] private ItemSlotViewModel? _slot;
    [ObservableProperty] private PrefixMetaButtonViewModel? _selectedMeta;
    [ObservableProperty] private PrefixGroupButtonViewModel? _selectedGroup;
    [ObservableProperty] private bool _canHavePrefix;
    [ObservableProperty] private string _noPrefixMessage = "Selecciona un slot para editarlo.";
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
        Refresh();
    }

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
    private void OnSlotPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ItemSlotViewModel.IsSelected) or nameof(ItemSlotViewModel.JustEdited)) return;
        Refresh();
    }

    private void Refresh()
    {
        var slot = Slot;
        HasSelection = slot != null && !slot.IsEmpty;
        if (slot == null || slot.IsEmpty)
        {
            CanHavePrefix = false;
            NoPrefixMessage = slot == null ? "Selecciona un slot para editarlo." : "Este slot está vacío.";
            CategoriesLabel = string.Empty;
            _currentCategories = PrefixCategory.None;
            Groups.Clear();
            Prefixes.Clear();
            return;
        }

        _currentCategories = PrefixEligibility.For(slot.Item, _service.PrefixRules, _service.CalamityCatalog);
        CanHavePrefix = _currentCategories != PrefixCategory.None;
        NoPrefixMessage = CanHavePrefix ? string.Empty : "Este objeto no admite ningún prefijo.";
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

    private void RebuildGroups()
    {
        Groups.Clear();
        var slot = Slot;
        if (slot == null || SelectedMeta == null) { Prefixes.Clear(); return; }

        foreach (var g in PrefixGroupCatalog.GroupsFor(SelectedMeta.Meta, _currentCategories, slot.IsCalamity, slot.Item.Id, _service.PrefixRules))
            Groups.Add(new PrefixGroupButtonViewModel(g));

        var firstGroup = Groups.FirstOrDefault();
        if (ReferenceEquals(firstGroup, SelectedGroup)) RebuildPrefixes();
        else SelectedGroup = firstGroup;
    }

    private void RebuildPrefixes()
    {
        Prefixes.Clear();
        var slot = Slot;
        var group = SelectedGroup;
        if (slot == null || group == null) return;

        foreach (int id in PrefixGroupCatalog.PrefixIdsFor(group.Group, slot.IsCalamity, slot.Item.Id, _service.PrefixRules))
        {
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

    private static string DescribeCategories(PrefixCategory cats)
    {
        var parts = new List<string>();
        if (cats.HasFlag(PrefixCategory.Melee)) parts.Add("Cuerpo a cuerpo");
        if (cats.HasFlag(PrefixCategory.Ranged)) parts.Add("A distancia");
        if (cats.HasFlag(PrefixCategory.Magic)) parts.Add("Magia");
        if (cats.HasFlag(PrefixCategory.Summon)) parts.Add("Invocación");
        if (cats.HasFlag(PrefixCategory.Accessory)) parts.Add("Accesorio");
        return string.Join(" · ", parts);
    }
}
