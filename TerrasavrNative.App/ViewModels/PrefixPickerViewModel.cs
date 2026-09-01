using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Model;

namespace TerrasavrNative.App.ViewModels;

// Selector manual de CUALQUIER prefijo (97 vanilla + 21 reales de Calamity/Picaro), no solo
// el "mejor" auto-sugerido - cierra el hueco #2 de la auditoria Terrasavr JS vs puerto (ver
// bitacora.md). Mismo patron "picker inline con PickTarget" que LibraryViewModel/
// BuffsViewModel, pero sin buscador obligatorio: con solo 118 entradas en total (frente a los
// ~8200 objetos o ~660 buffs) se muestran TODAS de entrada, la busqueda solo filtra.
//
// Deliberadamente NO agrupado por categoria de arma/armadura/accesorio: Terraria no expone
// esa categoria como una propiedad simple del prefijo (depende de la logica de elegibilidad
// por tipo de objeto en Item.Prefix(), no hay una tabla estatica prefijo->categoria fiable sin
// decompilar y verificar esa logica entera) - se prefirio un selector plano y verificable a
// inventar categorias sin confirmar.
public partial class PrefixPickerViewModel : ObservableObject
{
    private readonly List<PrefixCatalogEntryViewModel> _all;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultsSummary = string.Empty;
    [ObservableProperty] private ItemSlotViewModel? _pickTarget;

    public bool IsPicking => PickTarget != null;

    public event Action? PrefixApplied;

    public ObservableCollection<PrefixCatalogEntryViewModel> Results { get; } = [];

    public PrefixPickerViewModel(CharacterFileService service)
    {
        _all = [new PrefixCatalogEntryViewModel("(Ninguno)", ItemPrefix.None, false)];
        foreach (var entry in service.VanillaPrefixCatalog.AllEntries())
            _all.Add(new PrefixCatalogEntryViewModel(entry.Es ?? entry.En ?? entry.Internal, ItemPrefix.Vanilla((byte)entry.Id), false));
        foreach (var entry in service.RoguePrefixCatalog.Weapon.Concat(service.RoguePrefixCatalog.Accessory))
            _all.Add(new PrefixCatalogEntryViewModel(entry.Es ?? entry.En ?? entry.Internal, ItemPrefix.CalamitySynthetic(entry.Id), true));

        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnPickTargetChanged(ItemSlotViewModel? value)
    {
        OnPropertyChanged(nameof(IsPicking));
        SearchText = string.Empty;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Results.Clear();
        var matches = string.IsNullOrWhiteSpace(SearchText)
            ? _all
            : _all.Where(p => p.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var m in matches) Results.Add(m);
        ResultsSummary = string.IsNullOrWhiteSpace(SearchText)
            ? $"{_all.Count} prefijos en total (vanilla + Calamity)."
            : $"{matches.Count} resultado(s).";
    }

    [RelayCommand]
    private void PickPrefix(PrefixCatalogEntryViewModel? entry)
    {
        if (PickTarget == null || entry == null) return;
        PickTarget.SetPrefix(entry.Prefix);
        PickTarget = null;
        PrefixApplied?.Invoke();
    }

    [RelayCommand]
    private void CancelPick() => PickTarget = null;
}
