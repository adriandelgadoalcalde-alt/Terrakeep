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

    // Portado de CALAMITY_CATEGORY_LABELS (overrides.js real) y ampliado a mano (2-sep-2026,
    // pedido explicito del usuario tras revisar el esquema: varias subcarpetas se quedaban a
    // medio traducir) hasta cubrir las 121 categorias reales de calamity/catalog.json - ver
    // scripts que generan el catalogo. Los nombres de SET/material propios de Calamity
    // (Aerospec, Astral, Basalt de Armor/Placeables por igual, Cosmilite, Marnite, Navystone,
    // Silva, Statigel, Stratus, Wulfrum, Acidwood...) se dejan sin traducir a proposito, mismo
    // criterio ya establecido para los ~33 sets de armadura ("no vale la pena inventar una
    // traduccion de un nombre propio del mod, y esta instalacion de Calamity no trae ningun
    // es-ES real de donde sacarla") - solo se traduce la palabra estructural que los envuelve
    // (ej. "FurnitureCosmilite" -> "Muebles de Cosmilite", el material se queda igual). Un par
    // de terminos SI son vanilla real y verificados contra vanilla_item_names_by_key.json
    // (Pylon -> real "Torre", ej. TeleportationPylonVictory -> "Torre universal"; Fountain ->
    // real "Fuente"; Banner -> real "Estandarte") en vez de inventados a ciegas.
    private static readonly Dictionary<string, string> CalamityCategoryLabelsEs = new()
    {
        ["Weapons/Melee"] = "Armas - Cuerpo a cuerpo",
        ["Weapons/Ranged"] = "Armas - A distancia",
        ["Weapons/Magic"] = "Armas - Magia",
        ["Weapons/Rogue"] = "Armas - Pícaro",
        ["Weapons/Summon"] = "Armas - Invocación",
        ["Weapons/DraedonsArsenal"] = "Armas - Arsenal de Draedon",
        ["Weapons/Typeless"] = "Armas - Sin tipo",
        ["Accessories"] = "Accesorios",
        ["Accessories/Vanity"] = "Accesorios - Vanidad",
        ["Accessories/Wings"] = "Accesorios - Alas",
        ["Armor/Vanity"] = "Armadura - Vanidad",
        ["Materials"] = "Materiales",
        ["Potions"] = "Pociones",
        ["Potions/Alcohol"] = "Pociones - Alcohol",
        ["Potions/Food"] = "Pociones - Comida",
        ["Tools"] = "Herramientas",
        ["Tools/ClimateChange"] = "Herramientas - Cambio climático",
        ["Tools/SpawnBlocker"] = "Herramientas - Bloqueador de aparición",
        ["Ammo"] = "Munición",
        ["Dyes"] = "Tintes",
        ["Dyes/HairDye"] = "Tintes - Tinte de pelo",
        ["Pets"] = "Mascotas",
        ["Mounts"] = "Monturas",
        ["Mounts/Minecarts"] = "Monturas - Vagonetas",
        ["LoreItems"] = "Objetos de historia",
        ["SummonItems"] = "Objetos de invocación (jefes)",
        ["SummonItems/Invasion"] = "Objetos de invocación (jefes) - Invasión",
        ["SummonItems/TownPets"] = "Objetos de invocación (jefes) - Mascotas de pueblo",
        ["TreasureBags"] = "Bolsas del tesoro",
        ["TreasureBags/MiscGrabBags"] = "Bolsas del tesoro - Bolsas variadas",
        ["DraedonItems"] = "Objetos de Draedon",
        ["DraedonMisc"] = "Varios de Draedon",
        ["Fishing"] = "Pesca",
        ["Fishing/AstralCatches"] = "Pesca - Capturas Astrales",
        ["Fishing/BrimstoneCragCatches"] = "Pesca - Capturas del Risco de Brimstone",
        ["Fishing/FishingRods"] = "Pesca - Cañas de pescar",
        ["Fishing/SulphurCatches"] = "Pesca - Capturas de Azufre",
        ["Fishing/SunkenSeaCatches"] = "Pesca - Capturas del Mar Hundido",
        ["Misc"] = "Varios",
        ["Critters"] = "Criaturas",
        ["LabFinders"] = "Localizadores de laboratorio",
        ["PermanentBoosters"] = "Potenciadores permanentes",
        ["Placeables/Furniture"] = "Colocables - Muebles",
        ["Placeables/Furniture/Trophies"] = "Colocables - Trofeos",
        ["Placeables/Furniture/BossRelics"] = "Colocables - Reliquias de jefes",
        ["Placeables/Furniture/CraftingStations"] = "Colocables - Muebles - Estaciones de crafteo",
        ["Placeables/Furniture/Fountains"] = "Colocables - Muebles - Fuentes",
        ["Placeables/Furniture/Monoliths"] = "Colocables - Muebles - Monolitos",
        ["Placeables/Furniture/Paintings"] = "Colocables - Muebles - Cuadros",
        ["Placeables/FurnitureAbyss"] = "Colocables - Muebles del Abismo",
        ["Placeables/FurnitureAcidwood"] = "Colocables - Muebles de Acidwood",
        ["Placeables/FurnitureAncient"] = "Colocables - Muebles Antiguos",
        ["Placeables/FurnitureAshen"] = "Colocables - Muebles Cenicientos",
        ["Placeables/FurnitureAuric"] = "Colocables - Muebles Auric",
        ["Placeables/FurnitureBasalt"] = "Colocables - Muebles de Basalto",
        ["Placeables/FurnitureBotanic"] = "Colocables - Muebles Botánicos",
        ["Placeables/FurnitureCosmilite"] = "Colocables - Muebles de Cosmilite",
        ["Placeables/FurnitureDriftwood"] = "Colocables - Muebles de Madera de Deriva",
        ["Placeables/FurnitureExo"] = "Colocables - Muebles Exo",
        ["Placeables/FurnitureMarnite"] = "Colocables - Muebles de Marnite",
        ["Placeables/FurnitureMonolith"] = "Colocables - Muebles Monolito",
        ["Placeables/FurnitureNavystone"] = "Colocables - Muebles de Navystone",
        ["Placeables/FurnitureNavystone/FurnitureAncientNavystone"] = "Colocables - Muebles de Navystone - Antiguos",
        ["Placeables/FurnitureOtherworldly"] = "Colocables - Muebles de Otro Mundo",
        ["Placeables/FurniturePlagued"] = "Colocables - Muebles de la Plaga",
        ["Placeables/FurnitureProfaned"] = "Colocables - Muebles Profanados",
        ["Placeables/FurnitureRunestone"] = "Colocables - Muebles Rúnicos",
        ["Placeables/FurnitureSacrilegious"] = "Colocables - Muebles Sacrílegos",
        ["Placeables/FurnitureShellstone"] = "Colocables - Muebles de Piedra Concha",
        ["Placeables/FurnitureSilva"] = "Colocables - Muebles de Silva",
        ["Placeables/FurnitureStatigel"] = "Colocables - Muebles de Statigel",
        ["Placeables/FurnitureStratus"] = "Colocables - Muebles de Stratus",
        ["Placeables/FurnitureVoid"] = "Colocables - Muebles del Vacío",
        ["Placeables/FurnitureWulfrum"] = "Colocables - Muebles Wulfrum",
        ["Placeables/FurnitureWulfrum/FurnitureAnodizedWulfrum"] = "Colocables - Muebles Wulfrum - Anodizados",
        ["Placeables/Banners"] = "Colocables - Estandartes",
        ["Placeables/Walls"] = "Colocables - Paredes",
        ["Placeables/Walls/DraedonStructures"] = "Colocables - Paredes - Estructuras de Draedon",
        ["Placeables/DraedonStructures"] = "Colocables - Estructuras de Draedon",
        ["Placeables/DraedonStructures/CagedLights"] = "Colocables - Estructuras de Draedon - Luces enjauladas",
        ["Placeables/Abyss"] = "Colocables - Abismo",
        ["Placeables/Astral"] = "Colocables - Astral",
        ["Placeables/Crags"] = "Colocables - Riscos",
        ["Placeables/LivingFire"] = "Colocables - Fuego Viviente",
        ["Placeables/MusicBoxes"] = "Colocables - Cajas de Música",
        ["Placeables/Ores"] = "Colocables - Minerales",
        ["Placeables/PlaceableTurrets"] = "Colocables - Torretas",
        ["Placeables/Plates"] = "Colocables - Placas",
        ["Placeables/Pylons"] = "Colocables - Torres",
        ["Placeables/SunkenSea"] = "Colocables - Mar Hundido",
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
