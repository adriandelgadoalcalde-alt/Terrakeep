using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.Services;

// Arbol de carpetas REAL de Terrasavr (vanilla, Hc.deploy real - ver VanillaLibraryTreeCatalog/
// scripts/extraer-arbol-libreria-vanilla.js) + una unica carpeta madre "Calamity (mod)" (puerto
// fiel de calamityBuildLibraryNode, overrides.js real) - compartido entre la Libreria y la
// pestaña Investigacion (pedido explicito 2-sep-2026: "quiero que calques exactamente la
// estructura de carpetas orden y organizacion de terrasav para esta librera Y investigacion"),
// para no duplicar el algoritmo de agrupado de Calamity en dos sitios.
public static class LibraryCategoryTreeBuilder
{
    // Mismo tope real que usa el propio Terrasavr (b()/c() en Hc.deploy real) para paginar una
    // carpeta hoja en "Page N" - se replica igual para las hojas de Calamity (que no vienen ya
    // paginadas del extractor, a diferencia de las vanilla).
    private const int LeafPageSize = 40;

    public static List<CategoryNodeViewModel> Build(CharacterFileService service)
    {
        var roots = new List<CategoryNodeViewModel>();
        foreach (var root in service.VanillaLibraryTree.RootNodes)
            roots.Add(BuildVanillaNode(root, string.Empty));
        roots.Add(BuildCalamityRoot(service));
        return roots;
    }

    private static CategoryNodeViewModel BuildVanillaNode(VanillaLibraryNode node, string parentPath)
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
    private static CategoryNodeViewModel BuildCalamityRoot(CharacterFileService service)
    {
        var byCategory = service.CalamityCatalog.Entries
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Select(e => e.SyntheticId).ToList());
        var cats = byCategory.Keys.OrderBy(c => c, StringComparer.Ordinal).ToList();

        string? IconOf(int syntheticId) =>
            service.CalamityCatalog.BySyntheticId(syntheticId)?.Icon is { } icon
                ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + icon
                : null;

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
}
