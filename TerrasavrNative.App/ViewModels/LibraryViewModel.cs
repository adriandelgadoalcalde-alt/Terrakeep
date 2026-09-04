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
//
// H5-15 (quinta auditoria de Opus): arbol de categorias, busqueda con debounce y el par
// SelectCategory/ClearCategory ya no viven aqui - ver CatalogBrowserViewModel, base real
// compartida con BuffLibraryViewModel/ResearchViewModel.
public partial class LibraryViewModel : CatalogBrowserViewModel<LibraryItemViewModel>
{
    // L-c (segunda auditoria de Opus, Fable): "el tope de 300 no tiene ninguna medicion real
    // detras, solo el motivo generico de que WrapPanel no virtualiza". Medido de verdad con el
    // arnes UIA (busqueda amplia real, "ar", sobre el catalogo completo ~8469 objetos, Debug
    // primera pasada): 100 objetos -> 158ms, 150 -> 271ms, 300 -> 802ms real - NO escala lineal
    // (WrapPanel sin virtualizar empeora peor que proporcional al crecer), 300 era un freeze
    // real y perceptible tecleando. 100 es el punto real donde el reflow deja de notarse de
    // verdad manteniendo un numero de resultados util antes de pedir afinar la busqueda.
    private const int MaxResults = 100;

    private readonly List<LibraryItemViewModel> _all;
    private readonly Dictionary<int, LibraryItemViewModel> _byId;

    [ObservableProperty] private ItemSlotViewModel? _pickTarget;

    // L-b (segunda auditoria de Opus, Fable): "el aviso de 'solo validos para el slot
    // seleccionado' es un texto mas dentro de ResultsSummary, facil de pasar por alto - y ni
    // siquiera dice CUAL slot". PickTarget (el mismo ItemSlotViewModel que abrio el selector,
    // ver RequestPickForSlot en MainViewModel) ya conoce su propio rol real (SlotRoleLabel -
    // "Cabeza"/"Accesorio 3"/"Tinte"...) - una pildora real y separada, no un sufijo de frase.
    [ObservableProperty] private string? _slotRestrictionLabel;

    public bool IsPicking => PickTarget != null;

    // H5-13 (quinta auditoria de Opus): igual que la base, salvo que un slot restringido como
    // destino (PickTarget con AcceptedKind real, ej. "Tinte"/"Gancho") ya reduce el catalogo a
    // un conjunto pequeño y util de ver de inmediato (ver hasSlotRestriction en ApplyFilter) -
    // mostrar las carpetas raiz genericas ahi seria un paso atras, no un atajo.
    public override bool ShowRootCategoryCards => base.ShowRootCategoryCards &&
        !(PickTarget != null && PickTarget.AcceptedKind != TerrasavrNative.Core.Model.SlotKind.None);

    public event Action? ItemPlaced;

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

        ApplyFilter();
    }

    protected override void ApplyFilter()
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

        if (ShowRootCategoryCards)
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
        OnPropertyChanged(nameof(ShowRootCategoryCards)); // H5-13: la restriccion de slot (o su ausencia) cambia esta condicion
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
