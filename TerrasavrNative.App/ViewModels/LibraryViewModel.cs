using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.ViewModels;

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
public partial class LibraryViewModel : ObservableObject
{
    private const int MaxResults = 300;

    private readonly List<LibraryItemViewModel> _all;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultsSummary = string.Empty;
    [ObservableProperty] private ItemSlotViewModel? _pickTarget;
    [ObservableProperty] private CategoryNodeViewModel? _selectedCategory;

    public bool IsPicking => PickTarget != null;

    public event Action? ItemPlaced;

    public ObservableCollection<LibraryItemViewModel> Results { get; } = [];
    public ObservableCollection<CategoryNodeViewModel> RootCategories { get; } = [];

    public LibraryViewModel(CharacterFileService service)
    {
        _all = [];

        foreach (var (id, name) in service.VanillaCatalog.AllEntries())
        {
            string? stats = ItemStatsFormatter.Format(false, id, service.VanillaStats, service.CalamityCatalog, service.VanillaCategories);
            _all.Add(new LibraryItemViewModel(name, false, VanillaIconResolver.GetIconPath(id), id, service.VanillaCategories.GetCategory(id), stats));
        }

        foreach (var entry in service.CalamityCatalog.Entries)
        {
            string? iconPath = entry.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + entry.Icon : null;
            string? stats = ItemStatsFormatter.Format(true, entry.SyntheticId, service.VanillaStats, service.CalamityCatalog, service.VanillaCategories);
            _all.Add(new LibraryItemViewModel(entry.DisplayName, true, iconPath, entry.SyntheticId, entry.Category, stats));
        }

        // Arbol real de Terrasavr (vanilla + carpeta madre "Calamity (mod)") - compartido con
        // la pestaña Investigacion, ver LibraryCategoryTreeBuilder (mismo pedido explicito
        // 2-sep-2026 para ambas: "quiero que calques exactamente la estructura de carpetas
        // orden y organizacion de terrasav para esta librera Y investigacion").
        foreach (var node in LibraryCategoryTreeBuilder.Build(service))
            RootCategories.Add(node);

        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void SelectCategory(CategoryNodeViewModel node)
    {
        // Bug real encontrado y corregido 2-sep-2026: IsExpanded no se tocaba nunca aqui, asi
        // que ninguna carpeta por debajo de la raiz era alcanzable de verdad desde la UI (el
        // ItemsControl de Children solo se muestra cuando IsExpanded es true) - critico ahora
        // que el arbol vanilla real tiene hasta 4 niveles de profundidad. Pulsar una carpeta
        // la selecciona/deselecciona (para "ver todo lo de aqui") Y alterna su despliegue,
        // independientemente de si tiene hijos o no.
        node.IsExpanded = !node.IsExpanded;

        if (SelectedCategory != null) SelectedCategory.IsSelected = false;
        if (SelectedCategory == node)
        {
            SelectedCategory = null; // pulsar la misma carpeta otra vez la deselecciona
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

        IEnumerable<LibraryItemViewModel> matches = _all;
        if (SelectedCategory != null)
            matches = matches.Where(i => SelectedCategory.ItemIdSet.Contains(i.Id));

        bool hasSearch = !string.IsNullOrWhiteSpace(SearchText);
        if (hasSearch)
            matches = matches.Where(i => i.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        if (!hasSearch && SelectedCategory == null)
        {
            ResultsSummary = $"{_all.Count} objetos en total (vanilla + Calamity) - escribe para buscar o elige una carpeta.";
            return;
        }

        var list = matches.ToList();
        foreach (var item in list.Take(MaxResults)) Results.Add(item);

        string categoryLabel = SelectedCategory != null ? $" en \"{SelectedCategory.Name}\"" : string.Empty;
        ResultsSummary = list.Count > MaxResults
            ? $"Mostrando {MaxResults} de {list.Count} resultados{categoryLabel} - afina la busqueda."
            : $"{list.Count} resultado(s){categoryLabel}.";
    }

    partial void OnPickTargetChanged(ItemSlotViewModel? value) => OnPropertyChanged(nameof(IsPicking));

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
