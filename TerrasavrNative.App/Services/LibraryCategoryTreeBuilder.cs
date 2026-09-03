using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.Services;

// T-G (segunda auditoria de Opus, Fable): "arranque sincrono - compartir una unica instancia
// del arbol de Libreria entre Libreria e Investigacion" - medido de verdad antes de tocar nada
// (mismo criterio que X-7): LibraryViewModel Y ResearchViewModel llamaban cada uno a Build()
// por separado, recorriendo/agrupando/paginando el mismo catalogo real de ~8469 objetos DOS
// VECES en cada arranque (~134ms medidos para el conjunto de sub-viewmodels de MainViewModel,
// con este doble trabajo real dentro). La parte cara (agrupar Calamity por categoria, paginar
// hojas >40, construir el arbol vanilla real) se calcula UNA SOLA VEZ aqui (cacheada en
// _cachedData - valido durante toda la vida del proceso, los catalogos de CharacterFileService
// son inmutables tras cargarse) como datos puros sin estado (CategoryTreeNodeData, sin
// ObservableObject ni comandos); Build() se queda con el mismo contrato de siempre (devuelve un
// arbol de CategoryNodeViewModel FRESCO e independiente en cada llamada - Libreria e
// Investigacion necesitan su propio IsSelected/SelectCommand por nodo, no pueden compartir las
// instancias de ViewModel en si) pero ahora solo hace el envoltorio barato (copiar referencias
// ya calculadas), no la reconstruccion entera.
public static class LibraryCategoryTreeBuilder
{
    // Mismo tope real que usa el propio Terrasavr (b()/c() en Hc.deploy real) para paginar una
    // carpeta hoja en "Page N" - se replica igual para las hojas de Calamity (que no vienen ya
    // paginadas del extractor, a diferencia de las vanilla).
    private const int LeafPageSize = 40;

    // Bug real de concurrencia evitado a proposito (mismo motivo real que la cache de
    // PlayerPreviewRenderer.Cache, ver bitacora.md "carrera de compilacion en paralelo"): xunit
    // corre clases de test en PARALELO por defecto, y muchas construyen su propio MainViewModel
    // (-> CharacterFileService -> Library/ResearchViewModel -> Build()) a la vez - un simple
    // `??=` sin lock podria arrancar BuildData() dos veces a la vez o publicar un _cachedData a
    // medio construir. El lock solo protege el check-y-set (barato); envolver TODO el metodo
    // desharia la ganancia real de compartir el trabajo.
    private static readonly object _cacheLock = new();
    private static List<CategoryTreeNodeData>? _cachedData;

    public static List<CategoryNodeViewModel> Build(CharacterFileService service)
    {
        List<CategoryTreeNodeData> data;
        lock (_cacheLock)
            data = _cachedData ??= BuildData(service);
        return data.Select(ToViewModel).ToList();
    }

    private static CategoryNodeViewModel ToViewModel(CategoryTreeNodeData data)
    {
        var vm = new CategoryNodeViewModel(data.Name, data.FullPath)
        {
            IconPath = data.IconPath,
            ItemIdsOrdered = data.ItemIdsOrdered, // misma lista inmutable de referencia, nunca se muta despues de construida
            ItemIdSet = data.ItemIdSet,
            ItemCount = data.ItemIdSet.Count,
        };
        foreach (var child in data.Children)
            vm.Children.Add(ToViewModel(child));
        return vm;
    }

    private static List<CategoryTreeNodeData> BuildData(CharacterFileService service)
    {
        var roots = new List<CategoryTreeNodeData>();
        foreach (var root in service.VanillaLibraryTree.RootNodes)
            roots.Add(BuildVanillaNode(root, string.Empty, service.LibraryLabels));
        roots.Add(BuildCalamityRoot(service));
        return roots;
    }

    private static CategoryTreeNodeData BuildVanillaNode(VanillaLibraryNode node, string parentPath, LibraryLabelCatalog labels)
    {
        // FullPath se construye siempre a partir del nombre INGLES real (clave estable) -
        // Name (lo que se muestra) usa la traduccion real de Terrasavr.
        string fullPath = parentPath.Length == 0 ? node.Name : $"{parentPath}/{node.Name}";
        string? iconPath = node.Icon != 0 ? VanillaIconResolver.GetIconPath(node.Icon) : null;

        if (node.IsLeaf)
        {
            var ids = node.ItemIds.ToList();
            return new CategoryTreeNodeData(labels.Translate(node.Name), fullPath, iconPath, ids, new HashSet<int>(ids), []);
        }

        var children = node.Children.Select(child => BuildVanillaNode(child, fullPath, labels)).ToList();
        var (ordered, set) = OrderedUnion(children);
        return new CategoryTreeNodeData(labels.Translate(node.Name), fullPath, iconPath, ordered, set, children);
    }

    // Puerto real de calamityBuildLibraryNode (Terrasavr-Calamity-Beta\resources\app\
    // local-site\overrides.js, ya en produccion en el Electron real) - agrupa las categorias
    // reales de calamity/catalog.json (con barra, ej. "Armor/Aerospec") por su segmento raiz,
    // pagina cualquier hoja de mas de 40 objetos en "Page N" (traducido con la misma etiqueta
    // real "Page $1" que usa el resto del arbol), etiquetas en español portadas de
    // CALAMITY_CATEGORY_LABELS. Deliberadamente SIN el limite de 19 carpetas por pantalla del
    // Electron original (LIBRARY_FOLDER_CAP) - era un parche a una limitacion real del motor
    // Haxe/OpenFL antiguo (lista de lineas fija sin scroll), que no existe en este arbol real
    // de WPF.
    private static CategoryTreeNodeData BuildCalamityRoot(CharacterFileService service)
    {
        var labels = service.LibraryLabels;
        var byCategory = service.CalamityCatalog.Entries
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Select(e => e.SyntheticId).ToList());
        var cats = byCategory.Keys.OrderBy(c => c, StringComparer.Ordinal).ToList();

        string? IconOf(int syntheticId) =>
            service.CalamityCatalog.BySyntheticId(syntheticId)?.Icon is { } icon
                ? "pack://siteoforigin:,,,/Assets/calamity/icons/" + icon
                : null;

        CategoryTreeNodeData BuildCategoryNode(string cat)
        {
            var ids = byCategory[cat];
            string label = $"{CalamityCategoryLabel(cat)} ({ids.Count})";

            if (ids.Count <= LeafPageSize)
                return new CategoryTreeNodeData(label, "Calamity/" + cat, IconOf(ids[0]), ids, new HashSet<int>(ids), []);

            var pages = new List<CategoryTreeNodeData>();
            for (int i = 0; i < ids.Count; i += LeafPageSize)
            {
                var chunk = ids.Skip(i).Take(LeafPageSize).ToList();
                pages.Add(new CategoryTreeNodeData(
                    labels.Translate($"Page {i / LeafPageSize + 1}"), $"Calamity/{cat}/Page{i / LeafPageSize + 1}",
                    IconOf(chunk[0]), chunk, new HashSet<int>(chunk), []));
            }
            var (ordered, set) = OrderedUnion(pages);
            return new CategoryTreeNodeData(label, "Calamity/" + cat, IconOf(ids[0]), ordered, set, pages);
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

        var rootIconEntry = service.CalamityCatalog.ByModAndInternal("CalamityMod", "Calamity");
        string? rootIcon = rootIconEntry != null ? IconOf(rootIconEntry.SyntheticId) : null;

        var topNodes = new List<CategoryTreeNodeData>();
        foreach (var top in groupOrder)
        {
            var members = groups[top];
            if (members.Count == 1)
            {
                topNodes.Add(BuildCategoryNode(members[0]));
            }
            else
            {
                var childNodes = members.Select(BuildCategoryNode).ToList();
                int totalIds = members.Sum(m => byCategory[m].Count);
                var (ordered, set) = OrderedUnion(childNodes);
                topNodes.Add(new CategoryTreeNodeData($"{CalamityCategoryLabel(top)} ({totalIds})", "Calamity/" + top, childNodes[0].IconPath, ordered, set, childNodes));
            }
        }
        var (rootOrdered, rootSet) = OrderedUnion(topNodes);
        return new CategoryTreeNodeData("Calamity (mod)", "Calamity", rootIcon, rootOrdered, rootSet, topNodes);
    }

    // Union ordenada real de los hijos YA construidos, concatenados en su propio orden real,
    // sin duplicar un id que caiga en mas de un hijo a la vez (pertenencia multiple real - ver
    // CategoryNodeViewModel).
    private static (List<int> Ordered, HashSet<int> Set) OrderedUnion(IReadOnlyList<CategoryTreeNodeData> children)
    {
        var seen = new HashSet<int>();
        var ordered = new List<int>();
        foreach (var child in children)
            foreach (int id in child.ItemIdsOrdered)
                if (seen.Add(id)) ordered.Add(id);
        return (ordered, seen);
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

// Nodo de datos puro (sin ObservableObject, sin comandos, sin estado de seleccion) - lo que de
// verdad es caro de calcular (agrupar/paginar/ordenar el catalogo completo), cacheado UNA vez
// y compartido entre todos los arboles de CategoryNodeViewModel que se piden despues.
public sealed record CategoryTreeNodeData(
    string Name, string FullPath, string? IconPath,
    IReadOnlyList<int> ItemIdsOrdered, IReadOnlySet<int> ItemIdSet,
    IReadOnlyList<CategoryTreeNodeData> Children);
