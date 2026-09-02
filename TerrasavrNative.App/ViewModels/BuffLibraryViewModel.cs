using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TerrasavrNative.App.Services;

namespace TerrasavrNative.App.ViewModels;

// Libreria de buffs (Fase 2 del rework de Buffs, pregunta a Opus sobre el diseño 2-sep-2026,
// cuarta pasada) - calco de LibraryViewModel (objetos): buscar por texto sobre el catalogo
// COMPLETO (vanilla + Calamity), o navegar el arbol REAL de categorias de Terrasavr
// (BuffLibraryTreeBuilder, calcado de app.BuffSide real). Tambien hace de selector: cuando un
// BuffSlotViewModel pide "elegir buff" (ChooseFromLibraryCommand), MainViewModel pone ese slot
// en PickTarget y cambia a esta pestaña - al pulsar una tarjeta aqui con PickTarget puesto, el
// buff se coloca en ese slot.
public partial class BuffLibraryViewModel : ObservableObject
{
    private readonly List<BuffCatalogEntryViewModel> _all;
    private readonly Dictionary<int, BuffCatalogEntryViewModel> _byId;
    // Mismo criterio que LibraryViewModel (objetos): _filtered guarda TODO lo que casa, sin
    // paginar - Results es solo la pagina actual, lo que de verdad se manda a la rejilla.
    private List<BuffCatalogEntryViewModel> _filtered = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultsSummary = string.Empty;
    [ObservableProperty] private BuffSlotViewModel? _pickTarget;
    [ObservableProperty] private CategoryNodeViewModel? _selectedCategory;

    // Paginacion real, mismo patron que LibraryViewModel (consulta a Opus, octava pasada -
    // Bestiario de Terraria decompilado: celda casi fija, columnas/filas variables, desborde
    // resuelto con paginas, nunca con scroll). PageCapacity lo escribe LibraryGridPanel
    // (Mode=OneWayToSource, ver MainWindow.xaml).
    [ObservableProperty] private int _pageCapacity = 40;
    [ObservableProperty] private int _pageIndex;

    public int PageCount => PageCapacity > 0 ? Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)PageCapacity)) : 1;

    public string RangeText
    {
        get
        {
            if (_filtered.Count == 0) return "0 resultados";
            int from = PageIndex * PageCapacity + 1;
            int to = Math.Min(_filtered.Count, from + PageCapacity - 1);
            return PageCount > 1 ? $"{from}-{to} de {_filtered.Count}" : $"{_filtered.Count} resultado(s)";
        }
    }

    public bool IsPicking => PickTarget != null;

    public event Action? BuffPlaced;

    public ObservableCollection<BuffCatalogEntryViewModel> Results { get; } = [];
    public ObservableCollection<CategoryNodeViewModel> RootCategories { get; } = [];

    public BuffLibraryViewModel(CharacterFileService service)
    {
        _all = [];
        _byId = new Dictionary<int, BuffCatalogEntryViewModel>();

        foreach (var (id, name) in service.VanillaBuffs.AllEntries())
        {
            var item = new BuffCatalogEntryViewModel(service.VanillaBuffs.GetDisplayName(id), id, false,
                VanillaBuffIconResolver.GetIconPath(id), service.VanillaBuffs.GetDescription(id));
            _all.Add(item);
            _byId[id] = item;
        }
        foreach (var entry in service.CalamityBuffCatalog.Entries)
        {
            string? iconPath = entry.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/buff_icons/" + entry.Icon : null;
            var item = new BuffCatalogEntryViewModel(entry.DisplayName, entry.SyntheticId, true, iconPath, entry.Description);
            _all.Add(item);
            _byId[entry.SyntheticId] = item;
        }

        foreach (var node in BuffLibraryTreeBuilder.Build(service))
            RootCategories.Add(node);
        RefreshVisibleFolders();

        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    // Fase 3 (octava pasada) - mismo navegador de un solo nivel + migas de pan que
    // LibraryViewModel, ver ahi el porque completo.
    private readonly List<CategoryNodeViewModel> _navStack = [];
    [ObservableProperty] private CategoryNodeViewModel? _currentFolder;

    public ObservableCollection<CategoryNodeViewModel> VisibleFolders { get; } = [];
    public ObservableCollection<CategoryNodeViewModel> Breadcrumb { get; } = [];

    private void RefreshVisibleFolders()
    {
        VisibleFolders.Clear();
        foreach (var n in CurrentFolder?.Children ?? RootCategories) VisibleFolders.Add(n);
    }

    private void RefreshBreadcrumb()
    {
        Breadcrumb.Clear();
        foreach (var n in _navStack) Breadcrumb.Add(n);
        if (CurrentFolder != null) Breadcrumb.Add(CurrentFolder);
    }

    private void SelectCategoryInternal(CategoryNodeViewModel node)
    {
        if (SelectedCategory != null) SelectedCategory.IsSelected = false;
        SelectedCategory = node;
        node.IsSelected = true;
        ApplyFilter();
    }

    [RelayCommand]
    private void Navigate(CategoryNodeViewModel node)
    {
        SelectCategoryInternal(node);
        if (node.Children.Count == 0) return;

        if (CurrentFolder != null) _navStack.Add(CurrentFolder);
        CurrentFolder = node;
        RefreshVisibleFolders();
        RefreshBreadcrumb();
    }

    [RelayCommand]
    private void GoToCrumb(CategoryNodeViewModel node)
    {
        if (node == CurrentFolder) return;
        int idx = _navStack.IndexOf(node);
        if (idx >= 0) _navStack.RemoveRange(idx, _navStack.Count - idx);
        CurrentFolder = node;
        RefreshVisibleFolders();
        RefreshBreadcrumb();
        SelectCategoryInternal(node);
    }

    [RelayCommand]
    private void ClearCategory()
    {
        if (SelectedCategory != null) SelectedCategory.IsSelected = false;
        SelectedCategory = null;
        _navStack.Clear();
        CurrentFolder = null;
        RefreshVisibleFolders();
        RefreshBreadcrumb();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        bool hasSearch = !string.IsNullOrWhiteSpace(SearchText);
        // Mismo arreglo real que LibraryViewModel.ApplyFilter (KeyNotFoundException real
        // reportada 2-sep-2026) - un id que el arbol conoce pero el catalogo no se descarta
        // en silencio en vez de tumbar la app entera.
        IEnumerable<BuffCatalogEntryViewModel> matches = SelectedCategory != null
            ? SelectedCategory.ItemIdsOrdered.Select(id => _byId.GetValueOrDefault(id)).OfType<BuffCatalogEntryViewModel>()
            : _all;

        if (hasSearch)
            matches = matches.Where(i => i.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        if (!hasSearch && SelectedCategory == null)
        {
            _filtered = [];
            Results.Clear();
            PageIndex = 0;
            ResultsSummary = $"{_all.Count} buffs en total (vanilla + Calamity) - escribe para buscar o elige una carpeta.";
            OnPropertyChanged(nameof(PageCount));
            OnPropertyChanged(nameof(RangeText));
            return;
        }

        _filtered = matches.ToList();
        PageIndex = 0;
        UpdatePage();

        string categoryLabel = SelectedCategory != null ? $" en \"{SelectedCategory.Name}\"" : string.Empty;
        ResultsSummary = categoryLabel.Length > 0 ? categoryLabel.Trim() : "Resultados de la búsqueda";
    }

    private void UpdatePage()
    {
        PageIndex = Math.Clamp(PageIndex, 0, PageCount - 1);
        Results.Clear();
        foreach (var item in _filtered.Skip(PageIndex * PageCapacity).Take(Math.Max(1, PageCapacity)))
            Results.Add(item);
        OnPropertyChanged(nameof(PageCount));
        OnPropertyChanged(nameof(RangeText));
        NextPageCommand.NotifyCanExecuteChanged();
        PrevPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnPageCapacityChanged(int value) => UpdatePage();
    partial void OnPageIndexChanged(int value)
    {
        Results.Clear();
        foreach (var item in _filtered.Skip(PageIndex * PageCapacity).Take(Math.Max(1, PageCapacity)))
            Results.Add(item);
        OnPropertyChanged(nameof(RangeText));
        NextPageCommand.NotifyCanExecuteChanged();
        PrevPageCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanGoNextPage))]
    private void NextPage() => PageIndex++;
    private bool CanGoNextPage() => PageIndex < PageCount - 1;

    [RelayCommand(CanExecute = nameof(CanGoPrevPage))]
    private void PrevPage() => PageIndex--;
    private bool CanGoPrevPage() => PageIndex > 0;

    partial void OnPickTargetChanged(BuffSlotViewModel? value) => OnPropertyChanged(nameof(IsPicking));

    [RelayCommand]
    private void PlaceInTarget(BuffCatalogEntryViewModel entry)
    {
        if (PickTarget == null) return;
        PickTarget.PlaceBuff(entry.Id);
        PickTarget = null;
        BuffPlaced?.Invoke();
    }

    [RelayCommand]
    private void CancelPick() => PickTarget = null;
}
