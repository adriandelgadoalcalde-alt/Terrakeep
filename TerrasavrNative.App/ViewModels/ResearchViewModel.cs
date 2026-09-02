using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.App.ViewModels;

// Investigacion (Modo Viaje) - antes una unica lista plana de hasta miles de objetos
// (WrapPanel sin categorizar, "es larguisimo" - pedido explicito 2-sep-2026). Reutiliza el
// mismo arbol real de carpetas que la Libreria (LibraryCategoryTreeBuilder - mismo pedido:
// "quiero que calques exactamente la estructura de carpetas... para esta librera Y
// investigacion") para navegar por carpetas en vez de un unico listado - mismos nombres y
// sprites de siempre (ResearchRowViewModel no cambia), solo cambia COMO se navega.
public sealed partial class ResearchViewModel : ObservableObject
{
    private readonly CharacterFileService _service;
    private Dictionary<int, int> _researchedCounts = new();

    [ObservableProperty] private CategoryNodeViewModel? _selectedCategory;
    [ObservableProperty] private string _resultsSummary = "Sin personaje cargado.";

    public ObservableCollection<CategoryNodeViewModel> RootCategories { get; } = [];
    public ObservableCollection<ResearchRowViewModel> Results { get; } = [];

    public ResearchViewModel(CharacterFileService service)
    {
        _service = service;
        foreach (var node in LibraryCategoryTreeBuilder.Build(service))
            RootCategories.Add(node);
    }

    public void LoadFrom(PlrCharacter character)
    {
        Reset();
        _researchedCounts = ResolveResearchedCounts(character);
        ApplyFilter();
    }

    // Sin personaje cargado (o al recargar uno nuevo) - misma limpieza de seleccion que
    // MainViewModel.RebuildContainers ya hacia con Containers/EquipmentGroup.
    public void Reset()
    {
        if (SelectedCategory != null) { SelectedCategory.IsSelected = false; SelectedCategory = null; }
        _researchedCounts = new Dictionary<int, int>();
        Results.Clear();
        ResultsSummary = "Sin personaje cargado.";
    }

    // Misma resolucion de Pid real que ya usaba MainViewModel.RebuildResearch: un Pid con "/"
    // es de Calamity (mod/internal), si no es el nombre interno vanilla real (nunca lleva "/").
    private Dictionary<int, int> ResolveResearchedCounts(PlrCharacter character)
    {
        var counts = new Dictionary<int, int>();
        foreach (var entry in character.Research)
        {
            int? id = entry.Pid.Contains('/')
                ? ResolveCalamityId(entry.Pid)
                : _service.VanillaCatalog.GetIdByKey(entry.Pid);
            if (id.HasValue) counts[id.Value] = entry.Count;
        }
        return counts;
    }

    private int? ResolveCalamityId(string pid)
    {
        int slash = pid.IndexOf('/');
        string mod = pid[..slash], internalName = pid[(slash + 1)..];
        return _service.CalamityCatalog.ByModAndInternal(mod, internalName)?.SyntheticId;
    }

    [RelayCommand]
    private void SelectCategory(CategoryNodeViewModel node)
    {
        // Mismo bug real corregido en LibraryViewModel.SelectCategory - ver ahi el porque.
        node.IsExpanded = !node.IsExpanded;

        if (SelectedCategory != null) SelectedCategory.IsSelected = false;
        if (SelectedCategory == node)
        {
            SelectedCategory = null;
        }
        else
        {
            SelectedCategory = node;
            node.IsSelected = true;
        }
        ApplyFilter();
    }

    [RelayCommand]
    private void ClearCategory()
    {
        if (SelectedCategory != null) SelectedCategory.IsSelected = false;
        SelectedCategory = null;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Results.Clear();

        if (SelectedCategory == null)
        {
            ResultsSummary = $"{_researchedCounts.Count} objeto(s) investigado(s) en total - elige una carpeta para verlos.";
            return;
        }

        var matches = SelectedCategory.ItemIdSet
            .Where(_researchedCounts.ContainsKey)
            .OrderBy(id => id)
            .ToList();

        foreach (int id in matches)
        {
            bool isCalamity = _service.CalamityCatalog.BySyntheticId(id) != null;
            string displayName;
            string? iconPath;
            if (isCalamity)
            {
                var calEntry = _service.CalamityCatalog.BySyntheticId(id);
                displayName = calEntry?.DisplayName ?? $"Calamity #{id}";
                iconPath = calEntry?.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + calEntry.Icon : null;
            }
            else
            {
                displayName = _service.VanillaCatalog.GetName(id);
                iconPath = VanillaIconResolver.GetIconPath(id);
            }
            Results.Add(new ResearchRowViewModel(displayName, _researchedCounts[id], isCalamity, iconPath));
        }

        ResultsSummary = $"{matches.Count} objeto(s) investigado(s) en \"{SelectedCategory.Name}\".";
    }
}
