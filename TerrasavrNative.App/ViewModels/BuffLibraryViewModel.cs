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
//
// H5-15 (quinta auditoria de Opus): arbol de categorias, busqueda con debounce y el par
// SelectCategory/ClearCategory ya no viven aqui - ver CatalogBrowserViewModel.
public partial class BuffLibraryViewModel : CatalogBrowserViewModel<BuffCatalogEntryViewModel>
{
    // H3-06 (tercera auditoria de Opus, Fable): "el tope+debounce medido de verdad en L-c
    // (LibraryViewModel) solo se aplico a esa unica superficie - Investigacion (ya arreglado
    // arriba) y esta Libreria de buffs no tenian NINGUN debounce (reflowaba en cada tecla)".
    // El tope de 300 se deja igual que siempre (universo de buffs mucho mas pequeño que el de
    // objetos, no hay medicion propia que justifique bajarlo a 100) - solo faltaba el mismo
    // intervalo ya medido (180ms, ver CatalogBrowserViewModel).
    private const int MaxResults = 300;

    private readonly List<BuffCatalogEntryViewModel> _all;
    private readonly Dictionary<int, BuffCatalogEntryViewModel> _byId;

    [ObservableProperty] private BuffSlotViewModel? _pickTarget;

    public bool IsPicking => PickTarget != null;

    public event Action? BuffPlaced;

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
            var item = new BuffCatalogEntryViewModel(entry.DisplayName, entry.SyntheticId, true, iconPath, entry.Description, entry.IsDebuff);
            _all.Add(item);
            _byId[entry.SyntheticId] = item;
        }

        foreach (var node in BuffLibraryTreeBuilder.Build(service))
            RootCategories.Add(node);
        // Auditoria de Opus, T-18: cada nodo lleva su propio comando real - ver el comentario
        // real en CategoryNodeViewModel.SelectCommand.
        CategoryNodeViewModel.AssignSelectCommand(RootCategories, SelectCategoryCommand);

        ApplyFilter();
    }

    protected override void ApplyFilter()
    {
        Results.Clear();

        bool hasSearch = !string.IsNullOrWhiteSpace(SearchText);
        // Mismo arreglo real que LibraryViewModel.ApplyFilter (KeyNotFoundException real
        // reportada 2-sep-2026) - un id que el arbol conoce pero el catalogo no se descarta
        // en silencio en vez de tumbar la app entera.
        IEnumerable<BuffCatalogEntryViewModel> matches = SelectedCategory != null
            ? SelectedCategory.ItemIdsOrdered.Select(id => _byId.GetValueOrDefault(id)).OfType<BuffCatalogEntryViewModel>()
            : _all;

        // L-a (segunda auditoria de Opus, Fable): misma gramatica real de busqueda de
        // Terrasavr que LibraryViewModel, recuperada de a0f5027 - ver LibrarySearchGrammar.
        if (hasSearch)
        {
            string query = SearchText;
            // C-09 (informe de pulido final, cierra L2): NameFolded/DescriptionFolded ya vienen
            // plegados de fabrica (BuffCatalogEntryViewModel).
            matches = matches.Where(i => LibrarySearchGrammar.Matches(
                query, i.Id, i.NameFolded, i.DescriptionFolded));
        }

        if (ShowRootCategoryCards)
        {
            ResultsSummary = $"{_all.Count} buffs en total (vanilla + Calamity) - escribe para buscar o elige una carpeta.";
            return;
        }

        var list = matches.ToList();
        foreach (var item in list.Take(MaxResults)) Results.Add(item);

        string categoryLabel = SelectedCategory != null ? $" en \"{SelectedCategory.Name}\"" : string.Empty;
        ResultsSummary = list.Count > MaxResults
            ? $"Mostrando {MaxResults} de {list.Count} resultados{categoryLabel} - afina la busqueda."
            : $"{list.Count} resultado(s){categoryLabel}.";
    }

    partial void OnPickTargetChanged(BuffSlotViewModel? value) => OnPropertyChanged(nameof(IsPicking));

    [RelayCommand]
    private void PlaceInTarget(BuffCatalogEntryViewModel entry)
    {
        if (PickTarget == null) return;
        // Bu-b (segunda auditoria de Opus, Fable): si PlaceBuff rechaza (buff duplicado real),
        // el picker se queda abierto - el aviso real (Slot.RejectionMessage) ya se ve en el
        // panel Editar, cerrar el picker aqui ademas seria fingir que se coloco algo.
        if (!PickTarget.PlaceBuff(entry.Id)) return;
        PickTarget = null;
        BuffPlaced?.Invoke();
    }

    [RelayCommand]
    private void CancelPick() => PickTarget = null;
}
