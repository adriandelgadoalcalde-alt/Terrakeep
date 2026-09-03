using System.Collections.ObjectModel;
using System.Windows.Threading;
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
    // H3-06 (tercera auditoria de Opus, Fable): "el tope+debounce medido de verdad en L-c
    // (LibraryViewModel) solo se aplico a esa unica superficie - Investigacion no tenia NINGUN
    // tope (tras 'Investigar todo', elegir una carpeta grande pintaba miles de filas de golpe)
    // NI debounce (reflowaba en cada tecla)". Mismo numero y mismo intervalo YA medidos en
    // produccion (100 resultados, 180ms) - no se remide aqui porque es el MISMO WrapPanel sin
    // virtualizar con el MISMO coste real por tarjeta, no una superficie distinta.
    private const int MaxResults = 100;
    private readonly DispatcherTimer _searchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(180) };

    private readonly CharacterFileService _service;
    private Dictionary<int, int> _researchedCounts = new();

    // R-f (segunda auditoria de Opus, Fable): "sin progreso global - el denominador (5.402
    // objetos reales) ya se conoce y no se muestra". Real y dinamico (mismo universo que
    // ResearchAllService.Apply ya recorre - vanilla + Calamity), no un numero fijo que pueda
    // desincronizarse si algun catalogo cambia de tamaño.
    private readonly int _totalKnownObjects;

    [ObservableProperty] private CategoryNodeViewModel? _selectedCategory;
    [ObservableProperty] private string _resultsSummary = "Sin personaje cargado.";

    // R-e (segunda auditoria de Opus, Fable): "sin buscador - Libreria y Libreria de buffs si lo
    // tienen, con la misma estructura de arbol. Asimetria pura". Misma gramatica real
    // (LibrarySearchGrammar, L-a) que las otras dos.
    [ObservableProperty] private string _searchText = string.Empty;
    // H3-06: mismo debounce real ya en produccion en LibraryViewModel - solo la busqueda por
    // TEXTO se difiere (elegir/quitar carpeta sigue aplicando al instante, un clic discreto).
    partial void OnSearchTextChanged(string value)
    {
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    // R-g (segunda auditoria de Opus, Fable): "ninguna advertencia si el personaje no es Modo
    // Viaje - el dato (Appearance.Difficulty) ya esta a mano". La Investigacion (desbloquear
    // recetas) solo tiene efecto real en el juego en Modo Viaje - investigar sin estarlo no
    // sirve de nada aunque la pestaña deje hacerlo igual (no es su trabajo impedirlo, solo
    // avisar).
    [ObservableProperty] private bool _isJourneyMode;

    public ObservableCollection<CategoryNodeViewModel> RootCategories { get; } = [];
    public ObservableCollection<ResearchRowViewModel> Results { get; } = [];

    public ResearchViewModel(CharacterFileService service)
    {
        _service = service;
        foreach (var node in LibraryCategoryTreeBuilder.Build(service))
            RootCategories.Add(node);
        // Auditoria de Opus, T-18: cada nodo lleva su propio comando real - ver el comentario
        // real en CategoryNodeViewModel.SelectCommand.
        CategoryNodeViewModel.AssignSelectCommand(RootCategories, SelectCategoryCommand);
        _totalKnownObjects = service.VanillaCatalog.AllInternalNames().Count() + service.CalamityCatalog.Entries.Count;
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            ApplyFilter();
        };
    }

    public void LoadFrom(PlrCharacter character)
    {
        Reset();
        _researchedCounts = ResolveResearchedCounts(character);
        IsJourneyMode = character.Difficulty == 3;
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
        IsJourneyMode = false;
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

    private (string DisplayName, string? IconPath, bool IsCalamity, int? RequiredCount) ResolveDisplay(int id)
    {
        var calEntry = _service.CalamityCatalog.BySyntheticId(id);
        if (calEntry != null)
        {
            string? iconPath = calEntry.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + calEntry.Icon : null;
            return (calEntry.DisplayName ?? $"Calamity #{id}", iconPath, true, null);
        }
        return (_service.VanillaCatalog.GetName(id), VanillaIconResolver.GetIconPath(id), false, _service.VanillaResearchCounts.Get(id));
    }

    private void ApplyFilter()
    {
        Results.Clear();
        bool hasSearch = !string.IsNullOrWhiteSpace(SearchText);

        // R-e: sin carpeta elegida pero CON busqueda, se busca en TODO lo ya investigado (mismo
        // criterio real que Libreria: la busqueda no exige elegir carpeta primero). Sin carpeta
        // Y sin busqueda, no hay ningun conjunto real que mostrar todavia - Fase 1 no cambia eso
        // (mostrar tambien lo NO investigado es R-a, Fase 2, layout distinto).
        if (SelectedCategory == null && !hasSearch)
        {
            ResultsSummary = $"{_researchedCounts.Count}/{_totalKnownObjects} objeto(s) investigado(s) en total - elige una carpeta o escribe para buscar.";
            return;
        }

        // Mismo bug real corregido en LibraryViewModel.ApplyFilter - ver ahi el porque:
        // ItemIdsOrdered respeta el orden curado real de Terrasavr, un HashSet (ItemIdSet) o
        // un OrderBy(id) numerico no. Sin carpeta (busqueda global), el orden real de Terrasavr
        // no aplica - se recorre en el orden en que ya estan investigados.
        IEnumerable<int> candidates = SelectedCategory != null
            ? SelectedCategory.ItemIdsOrdered.Where(_researchedCounts.ContainsKey)
            : _researchedCounts.Keys;

        var matches = new List<ResearchRowViewModel>();
        foreach (int id in candidates)
        {
            var (displayName, iconPath, isCalamity, requiredCount) = ResolveDisplay(id);
            if (hasSearch && !LibrarySearchGrammar.Matches(SearchText, id, displayName.ToLowerInvariant(), null)) continue;
            matches.Add(new ResearchRowViewModel(displayName, _researchedCounts[id], requiredCount, isCalamity, iconPath));
        }
        // H3-06: mismo tope real ya medido en LibraryViewModel (100 - WrapPanel sin
        // virtualizar, 300 tarjetas = 802ms de congelacion real) - una carpeta grande (ej.
        // "Calamity (mod)", miles de objetos) ya no pinta todo de golpe.
        foreach (var row in matches.Take(MaxResults)) Results.Add(row);

        string categoryLabel = SelectedCategory != null ? $" en \"{SelectedCategory.Name}\"" : string.Empty;
        ResultsSummary = matches.Count > MaxResults
            ? $"Mostrando {MaxResults} de {matches.Count} objeto(s) investigado(s){categoryLabel} - afina la busqueda."
            : $"{matches.Count} objeto(s) investigado(s){categoryLabel}.";
    }
}
