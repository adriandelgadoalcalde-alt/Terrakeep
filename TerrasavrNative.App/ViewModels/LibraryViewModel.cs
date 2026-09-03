using System.Collections.ObjectModel;
using System.Windows.Threading;
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
    // L-c (segunda auditoria de Opus, Fable): "el tope de 300 no tiene ninguna medicion real
    // detras, solo el motivo generico de que WrapPanel no virtualiza". Medido de verdad con el
    // arnes UIA (busqueda amplia real, "ar", sobre el catalogo completo ~8469 objetos, Debug
    // primera pasada): 100 objetos -> 158ms, 150 -> 271ms, 300 -> 802ms real - NO escala lineal
    // (WrapPanel sin virtualizar empeora peor que proporcional al crecer), 300 era un freeze
    // real y perceptible tecleando. 100 es el punto real donde el reflow deja de notarse de
    // verdad manteniendo un numero de resultados util antes de pedir afinar la busqueda.
    private const int MaxResults = 100;

    // L-c: la busqueda ya reflowaba en CADA pulsacion de tecla (UpdateSourceTrigger=
    // PropertyChanged) - con un termino amplio de varios caracteres, cada pulsacion
    // intermedia pagaba el coste real de reflow entero, no solo la ultima. Mismo patron ya
    // establecido en el proyecto (MainViewModel._saveConfirmationTimer, Stop()+Start() en
    // cada disparo) - solo la busqueda por TEXTO se debounça (elegir una carpeta o cambiar
    // PickTarget siguen aplicando el filtro al instante, son un unico clic discreto, no
    // tecleo continuo).
    private readonly DispatcherTimer _searchDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(180) };

    private readonly List<LibraryItemViewModel> _all;
    private readonly Dictionary<int, LibraryItemViewModel> _byId;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _resultsSummary = string.Empty;
    [ObservableProperty] private ItemSlotViewModel? _pickTarget;
    [ObservableProperty] private CategoryNodeViewModel? _selectedCategory;

    // L-b (segunda auditoria de Opus, Fable): "el aviso de 'solo validos para el slot
    // seleccionado' es un texto mas dentro de ResultsSummary, facil de pasar por alto - y ni
    // siquiera dice CUAL slot". PickTarget (el mismo ItemSlotViewModel que abrio el selector,
    // ver RequestPickForSlot en MainViewModel) ya conoce su propio rol real (SlotRoleLabel -
    // "Cabeza"/"Accesorio 3"/"Tinte"...) - una pildora real y separada, no un sufijo de frase.
    [ObservableProperty] private string? _slotRestrictionLabel;

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
            var rarityColor = VanillaRarityColorCatalog.Get(service.VanillaStats.Get(id)?.Rare);
            var item = new LibraryItemViewModel(name, false, VanillaIconResolver.GetIconPath(id), id, service.VanillaCategories.GetCategory(id), stats, rarityColor);
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
        // Auditoria de Opus, T-18: cada nodo lleva su propio comando real - ver el comentario
        // real en CategoryNodeViewModel.SelectCommand.
        CategoryNodeViewModel.AssignSelectCommand(RootCategories, SelectCategoryCommand);

        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            ApplyFilter();
        };

        ApplyFilter();
    }

    // L-c: reinicia el temporizador en cada pulsacion en vez de filtrar al instante - solo la
    // ULTIMA pulsacion de una racha de tecleo paga el coste real de reflow, 180ms despues de
    // que el usuario se detiene (imperceptible como demora, pero evita repetir el reflow entero
    // en cada caracter mientras todavia esta escribiendo).
    partial void OnSearchTextChanged(string value)
    {
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

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
        // L-b: rol real del slot ("Cabeza"/"Accesorio 3"/"Tinte"...) para la pildora - null
        // cuando no hay restriccion, oculta la pildora en el XAML (NullToCollapsed).
        SlotRestrictionLabel = hasSlotRestriction ? target!.SlotRoleLabel ?? "este slot" : null;

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
            matches = matches.Where(i => LibrarySearchGrammar.Matches(
                query, i.Id, i.DisplayName.ToLowerInvariant(), i.StatsTooltip?.ToLowerInvariant()));
        }

        if (!hasSearch && SelectedCategory == null && !hasSlotRestriction)
        {
            ResultsSummary = $"{_all.Count} objetos en total (vanilla + Calamity) - escribe para buscar o elige una carpeta.";
            return;
        }

        var list = matches.ToList();
        foreach (var item in list.Take(MaxResults)) Results.Add(item);

        // L-b: la restriccion de slot ya la dice la pildora aparte (mas visible, con el rol
        // real del slot) - ResultsSummary ya no repite un "válidos para este slot" generico.
        string categoryLabel = SelectedCategory != null ? $" en \"{SelectedCategory.Name}\"" : string.Empty;
        ResultsSummary = list.Count > MaxResults
            ? $"Mostrando {MaxResults} de {list.Count} resultados{categoryLabel} - afina la busqueda."
            : $"{list.Count} resultado(s){categoryLabel}.";
    }

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
