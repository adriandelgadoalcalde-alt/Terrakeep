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
    // Mismo tope real que usa el propio Terrasavr (b()/c() en Hc.deploy real) para paginar una
    // carpeta hoja en "Page N" - se replica igual para las hojas de Calamity (que no vienen ya
    // paginadas del extractor, a diferencia de las vanilla).
    private const int LeafPageSize = 40;

    private readonly List<LibraryItemViewModel> _all;
    private readonly Dictionary<int, LibraryItemViewModel> _byId;

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
        _byId = new Dictionary<int, LibraryItemViewModel>();

        foreach (var (id, name) in service.VanillaCatalog.AllEntries())
        {
            string? stats = ItemStatsFormatter.Format(false, id, service.VanillaStats, service.CalamityCatalog, service.VanillaCategories);
            var item = new LibraryItemViewModel(name, false, VanillaIconResolver.GetIconPath(id), id, service.VanillaCategories.GetCategory(id), stats);
            _all.Add(item);
            _byId[id] = item;
        }

        foreach (var entry in service.CalamityCatalog.Entries)
        {
            string? iconPath = entry.Icon != null ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + entry.Icon : null;
            string? stats = ItemStatsFormatter.Format(true, entry.SyntheticId, service.VanillaStats, service.CalamityCatalog, service.VanillaCategories);
            var item = new LibraryItemViewModel(entry.DisplayName, true, iconPath, entry.SyntheticId, entry.Category, stats);
            _all.Add(item);
            _byId[entry.SyntheticId] = item;
        }

        BuildCategoryTree(service);
        ApplyFilter();
    }

    // Arbol real de Terrasavr para vanilla (pedido explicito 2-sep-2026: "quiero que calques
    // exactamente la estructura de carpetas orden y organizacion de terrasav") + una unica
    // carpeta madre "Calamity (mod)" con sus categorias reales agrupadas - antes, "Materiales"
    // y "Colocables" eran cajones de sastre sin ningun criterio real detras, y los segmentos
    // de Calamity salian sueltos mezclados con los de vanilla. Ver bitacora.md para la
    // investigacion real completa (Hc.deploy/calamityBuildLibraryNode).
    private void BuildCategoryTree(CharacterFileService service)
    {
        foreach (var root in service.VanillaLibraryTree.RootNodes)
            RootCategories.Add(BuildVanillaNode(root, string.Empty));

        RootCategories.Add(BuildCalamityRoot(service));
    }

    private CategoryNodeViewModel BuildVanillaNode(VanillaLibraryNode node, string parentPath)
    {
        string fullPath = parentPath.Length == 0 ? node.Name : $"{parentPath}/{node.Name}";
        var vm = new CategoryNodeViewModel(node.Name, fullPath)
        {
            IconPath = node.Icon != 0 ? VanillaIconResolver.GetIconPath(node.Icon) : null,
        };

        if (node.IsLeaf)
        {
            vm.ItemIdSet = new HashSet<int>(node.ItemIds);
            vm.ItemCount = vm.ItemIdSet.Count;
            return vm;
        }

        foreach (var child in node.Children)
        {
            var childVm = BuildVanillaNode(child, fullPath);
            vm.Children.Add(childVm);
            vm.ItemIdSet.UnionWith(childVm.ItemIdSet);
        }
        vm.ItemCount = vm.ItemIdSet.Count;
        return vm;
    }

    // Puerto real de calamityBuildLibraryNode (Terrasavr-Calamity-Beta\resources\app\
    // local-site\overrides.js, ya en produccion en el Electron real) - agrupa las categorias
    // reales de calamity/catalog.json (con barra, ej. "Armor/Aerospec") por su segmento raiz,
    // pagina cualquier hoja de mas de 40 objetos en "Page N", etiquetas en español portadas de
    // CALAMITY_CATEGORY_LABELS. Deliberadamente SIN el limite de 19 carpetas por pantalla del
    // Electron original (LIBRARY_FOLDER_CAP) - era un parche a una limitacion real del motor
    // Haxe/OpenFL antiguo (lista de lineas fija sin scroll), que no existe en este arbol real
    // de WPF.
    private CategoryNodeViewModel BuildCalamityRoot(CharacterFileService service)
    {
        var byCategory = service.CalamityCatalog.Entries
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Select(e => e.SyntheticId).ToList());
        var cats = byCategory.Keys.OrderBy(c => c, StringComparer.Ordinal).ToList();

        string? IconOf(int id) => _byId.TryGetValue(id, out var item) ? item.IconPath : null;

        CategoryNodeViewModel BuildCategoryNode(string cat)
        {
            var ids = byCategory[cat];
            string label = $"{CalamityCategoryLabel(cat)} ({ids.Count})";
            var node = new CategoryNodeViewModel(label, "Calamity/" + cat) { IconPath = IconOf(ids[0]) };

            if (ids.Count <= LeafPageSize)
            {
                node.ItemIdSet = new HashSet<int>(ids);
                node.ItemCount = node.ItemIdSet.Count;
                return node;
            }

            for (int i = 0; i < ids.Count; i += LeafPageSize)
            {
                var chunk = ids.Skip(i).Take(LeafPageSize).ToList();
                var page = new CategoryNodeViewModel($"Page {i / LeafPageSize + 1}", $"{node.FullPath}/Page{i / LeafPageSize + 1}")
                {
                    IconPath = IconOf(chunk[0]),
                    ItemIdSet = new HashSet<int>(chunk),
                };
                page.ItemCount = page.ItemIdSet.Count;
                node.Children.Add(page);
                node.ItemIdSet.UnionWith(chunk);
            }
            node.ItemCount = node.ItemIdSet.Count;
            return node;
        }

        var groupOrder = new List<string>();
        var groups = new Dictionary<string, List<string>>();
        foreach (var cat in cats)
        {
            string top = cat.Split('/')[0];
            if (!groups.TryGetValue(top, out var members)) { members = []; groups[top] = members; groupOrder.Add(top); }
            members.Add(cat);
        }
        groupOrder.Sort(StringComparer.Ordinal);

        var root = new CategoryNodeViewModel("Calamity (mod)", "Calamity");
        var rootIconEntry = service.CalamityCatalog.ByModAndInternal("CalamityMod", "Calamity");
        root.IconPath = rootIconEntry != null ? IconOf(rootIconEntry.SyntheticId) : null;

        foreach (var top in groupOrder)
        {
            var members = groups[top];
            CategoryNodeViewModel node;
            if (members.Count == 1)
            {
                node = BuildCategoryNode(members[0]);
            }
            else
            {
                var childNodes = members.Select(BuildCategoryNode).ToList();
                int totalIds = members.Sum(m => byCategory[m].Count);
                node = new CategoryNodeViewModel($"{CalamityCategoryLabel(top)} ({totalIds})", "Calamity/" + top) { IconPath = childNodes[0].IconPath };
                foreach (var child in childNodes)
                {
                    node.Children.Add(child);
                    node.ItemIdSet.UnionWith(child.ItemIdSet);
                }
                node.ItemCount = node.ItemIdSet.Count;
            }
            root.Children.Add(node);
            root.ItemIdSet.UnionWith(node.ItemIdSet);
        }
        root.ItemCount = root.ItemIdSet.Count;
        return root;
    }

    // Portado tal cual de CALAMITY_CATEGORY_LABELS (overrides.js real) - rotulos en español ya
    // en produccion en el Electron. Lo que no tiene entrada exacta cae al segmento raiz
    // traducido si existe, o se deja tal cual (mismo criterio real: "no vale la pena una
    // entrada por cada uno de los ~33 sets de armadura").
    private static readonly Dictionary<string, string> CalamityCategoryLabelsEs = new()
    {
        ["Weapons/Melee"] = "Armas - Cuerpo a cuerpo",
        ["Weapons/Ranged"] = "Armas - A distancia",
        ["Weapons/Magic"] = "Armas - Magia",
        ["Weapons/Rogue"] = "Armas - Pícaro",
        ["Weapons/Summon"] = "Armas - Invocación",
        ["Weapons/DraedonsArsenal"] = "Armas - Arsenal de Draedon",
        ["Accessories"] = "Accesorios",
        ["Accessories/Vanity"] = "Accesorios - Vanidad",
        ["Accessories/Wings"] = "Accesorios - Alas",
        ["Armor/PreHardmode"] = "Armadura - Pre-Hardmode",
        ["Armor/Hardmode"] = "Armadura - Hardmode",
        ["Armor/PostMoonLord"] = "Armadura - Postmoonlord",
        ["Armor/Vanity"] = "Armadura - Vanidad",
        ["Materials"] = "Materiales",
        ["Potions"] = "Pociones",
        ["Tools"] = "Herramientas",
        ["Ammo"] = "Munición",
        ["Dyes"] = "Tintes",
        ["Pets"] = "Mascotas",
        ["Mounts"] = "Monturas",
        ["LoreItems"] = "Objetos de historia",
        ["SummonItems"] = "Objetos de invocación (jefes)",
        ["TreasureBags"] = "Bolsas del tesoro",
        ["DraedonItems"] = "Objetos de Draedon",
        ["Fishing"] = "Pesca",
        ["Misc"] = "Varios",
        ["Placeables/Furniture"] = "Colocables - Muebles",
        ["Placeables/Banners"] = "Colocables - Estandartes",
        ["Placeables/Walls"] = "Colocables - Paredes",
        ["Placeables/Furniture/Trophies"] = "Colocables - Trofeos",
        ["Placeables/Furniture/BossRelics"] = "Colocables - Reliquias de jefes",
        ["Placeables/DraedonStructures"] = "Colocables - Estructuras de Draedon",
        ["Placeables/FurnitureWulfrum"] = "Colocables - Muebles Wulfrum",
        ["Placeables/FurnitureExo"] = "Colocables - Muebles Exo",
        ["Armor"] = "Armadura",
        ["Placeables"] = "Colocables",
        ["Weapons"] = "Armas",
    };

    private static string CalamityCategoryLabel(string category)
    {
        if (CalamityCategoryLabelsEs.TryGetValue(category, out var direct)) return direct;
        var segments = category.Split('/');
        if (CalamityCategoryLabelsEs.TryGetValue(segments[0], out var topLabel)) segments[0] = topLabel;
        return string.Join(" - ", segments);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void SelectCategory(CategoryNodeViewModel node)
    {
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
