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
    private readonly List<LibraryItemViewModel> _all;
    private readonly Dictionary<int, LibraryItemViewModel> _byId;
    // Todo lo que casa con el filtro actual (categoria/restriccion/busqueda), SIN paginar -
    // Results (mas abajo) es solo la PAGINA actual, lo que de verdad se manda a la rejilla.
    private List<LibraryItemViewModel> _filtered = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultsSummary = string.Empty;
    [ObservableProperty] private ItemSlotViewModel? _pickTarget;
    [ObservableProperty] private CategoryNodeViewModel? _selectedCategory;

    // Paginacion real (consulta a Opus, octava pasada - patron real del Bestiario de Terraria
    // decompilado, UIBestiaryEntryGrid: celda casi fija, columnas/filas variables, desborde
    // resuelto con paginas, nunca con scroll). PageCapacity lo escribe LibraryGridPanel
    // (Mode=OneWayToSource, ver MainWindow.xaml) con cuantas tarjetas caben de VERDAD en el
    // espacio real disponible - 40 de partida (el mismo que ya usaba el arbol para paginar
    // carpetas hoja, LibraryCategoryTreeBuilder.LeafPageSize) hasta que el primer layout real
    // llegue.
    [ObservableProperty] private int _pageCapacity = 40;
    [ObservableProperty] private int _pageIndex;

    public int PageCount => PageCapacity > 0 ? Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)PageCapacity)) : 1;

    // Calco real de UIBestiaryEntryGrid.GetRangeText() ("{desde}-{hasta} ({total})") - consulta
    // a Opus, octava pasada: el mismo idioma que ya usa Terraria de verdad para "N-M de T".
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

    public event Action? ItemPlaced;

    public ObservableCollection<LibraryItemViewModel> Results { get; } = [];
    public ObservableCollection<CategoryNodeViewModel> RootCategories { get; } = [];

    public LibraryViewModel(CharacterFileService service)
    {
        _all = [];
        _byId = new Dictionary<int, LibraryItemViewModel>();

        foreach (var (id, name) in service.VanillaCatalog.AllEntries())
        {
            string? stats = ItemStatsFormatter.Format(false, id, service.TooltipCatalogs);
            var item = new LibraryItemViewModel(name, false, VanillaIconResolver.GetIconPath(id), id, service.VanillaCategories.GetCategory(id), stats);
            _all.Add(item);
            _byId[id] = item;
        }

        foreach (var entry in service.CalamityCatalog.Entries)
        {
            string? iconPath = entry.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + entry.Icon : null;
            string? stats = ItemStatsFormatter.Format(true, entry.SyntheticId, service.TooltipCatalogs);
            var item = new LibraryItemViewModel(entry.DisplayName, true, iconPath, entry.SyntheticId, entry.Category, stats);
            _all.Add(item);
            _byId[entry.SyntheticId] = item;
        }

        // Arbol real de Terrasavr (vanilla + carpeta madre "Calamity (mod)") - compartido con
        // la pestaña Investigacion, ver LibraryCategoryTreeBuilder (mismo pedido explicito
        // 2-sep-2026 para ambas: "quiero que calques exactamente la estructura de carpetas
        // orden y organizacion de terrasav para esta librera Y investigacion").
        foreach (var node in LibraryCategoryTreeBuilder.Build(service))
            RootCategories.Add(node);
        RefreshVisibleFolders();

        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    // Fase 3 (octava pasada, consulta a Opus - navegador de un solo nivel + migas de pan,
    // SOLO para la tira de la Libreria; Investigacion se queda con el arbol indentado clasico,
    // ver ResearchViewModel, decision explicita de Opus). El viejo arbol recursivo mostraba
    // los 4 niveles reales de Terrasavr indentados a la vez - "practico" de verdad, pedido
    // explicito del usuario, es mas parecido a explorar carpetas: se ve UN nivel (VisibleFolders,
    // hijos de CurrentFolder o la raiz si no hay ninguna abierta) con migas de pan (Breadcrumb)
    // para volver atras. _navStack guarda los antecesores del nivel actual (sin incluir
    // CurrentFolder, que ya se añade aparte al construir Breadcrumb).
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

    // Un clic hace las dos cosas reales a la vez: selecciona la carpeta como filtro (sus
    // ItemIdsOrdered, union real de sus descendientes si es una carpeta intermedia - por eso
    // tiene sentido mostrar resultados AUNQUE tenga subcarpetas) y, si tiene subcarpetas
    // (HasChildren), navega dentro para poder seguir explorando. Una hoja (sin hijos) solo
    // filtra - no hay a donde navegar.
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

    // Pulsar una miga de pan intermedia trunca la pila a partir de ahi - clasico "ir a esta
    // carpeta", igual que cualquier explorador de archivos real.
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
        bool hasSlotRestriction = target != null && target.AcceptedKind != TerrasavrNative.Core.Model.SlotKind.None;

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

        if (hasSearch)
            matches = matches.Where(i => i.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        if (!hasSearch && SelectedCategory == null && !hasSlotRestriction)
        {
            _filtered = [];
            Results.Clear();
            PageIndex = 0;
            ResultsSummary = $"{_all.Count} objetos en total (vanilla + Calamity) - escribe para buscar o elige una carpeta.";
            OnPropertyChanged(nameof(PageCount));
            OnPropertyChanged(nameof(RangeText));
            return;
        }

        // Paginacion real (consulta a Opus, octava pasada): _filtered guarda TODO lo que casa
        // (ya no hay tope de 300 - con paginas de verdad, el catalogo entero es navegable,
        // nunca se renderizan mas de PageCapacity tarjetas a la vez), UpdatePage() manda solo
        // la pagina actual a Results.
        _filtered = matches.ToList();
        PageIndex = 0;
        UpdatePage();

        string categoryLabel = SelectedCategory != null ? $" en \"{SelectedCategory.Name}\"" : string.Empty;
        string restrictionLabel = hasSlotRestriction ? " válidos para este slot" : string.Empty;
        ResultsSummary = $"{categoryLabel}{restrictionLabel}".Trim();
        if (ResultsSummary.Length == 0) ResultsSummary = "Resultados de la búsqueda";
    }

    // Manda solo la pagina actual a Results (lo que la rejilla real renderiza) - se llama al
    // cambiar de filtro, de pagina, o cuando LibraryGridPanel publica un PageCapacity real
    // nuevo (el espacio disponible cambio, ej. al redimensionar la ventana).
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

    partial void OnPickTargetChanged(ItemSlotViewModel? value)
    {
        OnPropertyChanged(nameof(IsPicking));
        // Capa principal de prevencion de la restriccion de slot (consulta a Opus, sexta
        // pasada: "cero chrome nuevo, cero popups... el problema deja de existir en el 90% de
        // los casos, el usuario nunca ve un objeto invalido que poder elegir") - al abrir el
        // selector para un slot restringido, refiltra de inmediato.
        ApplyFilter();
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
