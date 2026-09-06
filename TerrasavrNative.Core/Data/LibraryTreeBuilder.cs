namespace TerrasavrNative.Core.Data;

// Nodo de datos puro del arbol de carpetas de la Libreria (sin ObservableObject, sin comandos,
// sin estado de seleccion) - lo que de verdad es caro de calcular (agrupar/paginar/ordenar el
// catalogo completo). La app de escritorio lo envuelve en CategoryNodeViewModel (una instancia
// fresca por arbol, compartiendo por REFERENCIA estas listas ya calculadas); el mod de
// tModLoader lo consume tal cual, sin nada de WPF/MVVM por medio.
//
// WS2 de TerrakeepMod (6-sep-2026): vivia en TerrasavrNative.App/Services/
// LibraryCategoryTreeBuilder.cs junto con todo el algoritmo. Se mueve entero a Core (que no
// depende de WPF ni de nada externo y compila tambien para net8.0) para que el mod pueda
// reutilizarlo; en App solo se queda la capa fina de envoltorio a ViewModel.
//
// Ronda de idioma del 6-sep-2026 (queja real del usuario: la Libreria "sigue en español"): Name
// era el nombre YA traducido al español y el UNICO que existia, asi que las carpetas del arbol
// (vanilla y Calamity, objetos y buffs) se veian en español pasara lo que pasara con el idioma de
// la app. NameEn se añade al final CON VALOR POR DEFECTO a proposito: asi ninguna llamada
// existente se rompe, y un nodo que no lo rellene cae al nombre español - mismo criterio de
// idioma de referencia de todo el proyecto. La eleccion la hace la capa de presentacion
// (CategoryNodeViewModel en la app de escritorio), que ademas puede reaccionar a un cambio de
// idioma en caliente sin reconstruir el arbol entero: los dos nombres ya viajan en el nodo.
public sealed record CategoryTreeNodeData(
    string Name, string FullPath, string? IconPath,
    IReadOnlyList<int> ItemIdsOrdered, IReadOnlySet<int> ItemIdSet,
    IReadOnlyList<CategoryTreeNodeData> Children,
    string? NameEn = null);

// Construccion PURA del arbol de carpetas de la Libreria de OBJETOS: arbol vanilla real de
// Terrasavr (VanillaLibraryTreeCatalog, extraido y ejecutado de verdad desde el script.js real)
// mas la carpeta madre "Calamity (mod)" agrupada/paginada con el mismo algoritmo real de
// calamityBuildLibraryNode (overrides.js, ya en produccion en el Electron original).
//
// Nada aqui conoce WPF ni rutas de disco: la resolucion del icono de una carpeta entra como un
// Func<int,string?> inyectado (la app de escritorio pasa una ruta "pack://siteoforigin:,,,/..."
// real; el mod pasara lo que necesite para sus propias texturas). Tampoco conoce
// CharacterFileService (que es de App): recibe los catalogos de Core directamente.
public static class LibraryTreeBuilder
{
    // Mismo tope real que usa el propio Terrasavr (b()/c() en Hc.deploy real) para paginar una
    // carpeta hoja en "Page N" - se replica igual para las hojas de Calamity (que no vienen ya
    // paginadas del extractor, a diferencia de las vanilla).
    internal const int LeafPageSize = 40;

    // calamity puede ser null: entonces no se añade la carpeta madre "Calamity (mod)" (util
    // para el mod, donde el contenido de cualquier mod instalado se descubre en vivo - ver
    // LiveItemTreeBuilder - y no hay ningun catalogo estatico de Calamity que injertar).
    public static List<CategoryTreeNodeData> BuildItemTree(
        VanillaLibraryTreeCatalog vanillaTree,
        LibraryLabelCatalog labels,
        CalamityCatalog? calamity,
        Func<int, string?> iconResolver)
    {
        var roots = new List<CategoryTreeNodeData>();
        foreach (var root in vanillaTree.RootNodes)
            roots.Add(BuildVanillaNode(root, string.Empty, labels, iconResolver));
        if (calamity != null)
            roots.Add(BuildCalamityRoot(calamity, labels, iconResolver));
        return roots;
    }

    private static CategoryTreeNodeData BuildVanillaNode(VanillaLibraryNode node, string parentPath, LibraryLabelCatalog labels, Func<int, string?> iconResolver)
    {
        // FullPath se construye siempre a partir del nombre INGLES real (clave estable) -
        // Name (lo que se muestra) usa la traduccion real de Terrasavr.
        string fullPath = parentPath.Length == 0 ? node.Name : $"{parentPath}/{node.Name}";
        string? iconPath = node.Icon != 0 ? iconResolver(node.Icon) : null;

        if (node.IsLeaf)
        {
            var ids = node.ItemIds.ToList();
            return new CategoryTreeNodeData(labels.Translate(node.Name), fullPath, iconPath, ids, new HashSet<int>(ids), [], node.Name);
        }

        var children = node.Children.Select(child => BuildVanillaNode(child, fullPath, labels, iconResolver)).ToList();
        var (ordered, set) = OrderedUnion(children);
        // Ronda de idioma del 6-sep-2026: el nombre INGLES es literalmente node.Name - es la
        // clave real con la que LibraryLabelCatalog busca la traduccion, y viene tal cual del
        // arbol real de Terrasavr. No hay nada que traducir para el ingles: hay que NO traducir.
        return new CategoryTreeNodeData(labels.Translate(node.Name), fullPath, iconPath, ordered, set, children, node.Name);
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
    private static CategoryTreeNodeData BuildCalamityRoot(CalamityCatalog calamity, LibraryLabelCatalog labels, Func<int, string?> iconResolver)
    {
        var byCategory = calamity.Entries
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Select(e => e.SyntheticId).ToList());

        var rootIconEntry = calamity.ByModAndInternal("CalamityMod", "Calamity");
        string? rootIcon = rootIconEntry != null ? iconResolver(rootIconEntry.SyntheticId) : null;

        return BuildGroupedRoot(
            "Calamity (mod)", "Calamity", rootIcon, byCategory,
            CalamityCategoryLabel, iconResolver,
            page => labels.Translate($"Page {page}"),
            // Ronda de idioma del 6-sep-2026: en ingles la plantilla real de Terrasavr NO se
            // traduce - "Page N" ya es el original, es lo que LibraryLabelCatalog usa de clave.
            rootNameEn: "Calamity (mod)",
            categoryLabelEn: CalamityCategoryLabelEn,
            pageLabelEn: page => $"Page {page}");
    }

    // Raiz agrupada generica - el mismo algoritmo real que usa "Calamity (mod)" en el arbol de
    // objetos, compartido tambien por la Libreria de buffs (BuffTreeBuilder) y por el catalogo
    // en vivo del mod (LiveItemTreeBuilder), en vez de tres copias del mismo bucle:
    // 1. Las categorias se ordenan de forma ordinal y se agrupan por su primer segmento (lo que
    //    va antes de la primera barra). Un grupo con un solo miembro NO crea carpeta intermedia
    //    (se cuelga la categoria directa de la raiz) - por eso un conjunto de categorias sin
    //    ninguna barra (buffs) sale exactamente como una lista plana bajo la raiz.
    // 2. Cualquier hoja de mas de LeafPageSize entradas se parte en paginas ("Page N"/"Página N"
    //    segun lo que devuelva pageLabel - la Libreria de objetos traduce la plantilla real de
    //    Terrasavr, la de buffs usa la etiqueta literal en español).
    // 3. Cada carpeta lleva el icono de su primera entrada real; la raiz, el que se le pase.
//
    // Ronda de idioma del 6-sep-2026: los tres parametros "...En" son opcionales - quien no los
    // pase deja NameEn a null y ese nodo cae al nombre español, mismo criterio de referencia de
    // siempre. El recuento "(N)" va igual en los dos idiomas: solo cambia la etiqueta.
    internal static CategoryTreeNodeData BuildGroupedRoot(
        string rootName,
        string rootPath,
        string? rootIcon,
        IReadOnlyDictionary<string, List<int>> byCategory,
        Func<string, string> categoryLabel,
        Func<int, string?> iconResolver,
        Func<int, string> pageLabel,
        string? rootNameEn = null,
        Func<string, string>? categoryLabelEn = null,
        Func<int, string>? pageLabelEn = null)
    {
        var cats = byCategory.Keys.OrderBy(c => c, StringComparer.Ordinal).ToList();

        string? EtiquetaEn(string cat, int count) =>
            categoryLabelEn == null ? null : $"{categoryLabelEn(cat)} ({count})";

        CategoryTreeNodeData BuildCategoryNode(string cat)
        {
            var ids = byCategory[cat];
            string label = $"{categoryLabel(cat)} ({ids.Count})";
            string? labelEn = EtiquetaEn(cat, ids.Count);
            string path = $"{rootPath}/{cat}";

            if (ids.Count <= LeafPageSize)
                return new CategoryTreeNodeData(label, path, iconResolver(ids[0]), ids, new HashSet<int>(ids), [], labelEn);

            var pages = new List<CategoryTreeNodeData>();
            for (int i = 0; i < ids.Count; i += LeafPageSize)
            {
                var chunk = ids.Skip(i).Take(LeafPageSize).ToList();
                int numero = i / LeafPageSize + 1;
                pages.Add(new CategoryTreeNodeData(
                    pageLabel(numero), $"{path}/Page{numero}",
                    iconResolver(chunk[0]), chunk, new HashSet<int>(chunk), [], pageLabelEn?.Invoke(numero)));
            }
            var (pagedOrdered, pagedSet) = OrderedUnion(pages);
            return new CategoryTreeNodeData(label, path, iconResolver(ids[0]), pagedOrdered, pagedSet, pages, labelEn);
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
                topNodes.Add(new CategoryTreeNodeData($"{categoryLabel(top)} ({totalIds})", $"{rootPath}/{top}", childNodes[0].IconPath, ordered, set, childNodes, EtiquetaEn(top, totalIds)));
            }
        }
        var (rootOrdered, rootSet) = OrderedUnion(topNodes);
        return new CategoryTreeNodeData(rootName, rootPath, rootIcon, rootOrdered, rootSet, topNodes, rootNameEn);
    }

    // Union ordenada real de los hijos YA construidos, concatenados en su propio orden real,
    // sin duplicar un id que caiga en mas de un hijo a la vez (pertenencia multiple real - ver
    // CategoryNodeViewModel de la app).
    internal static (List<int> Ordered, HashSet<int> Set) OrderedUnion(IReadOnlyList<CategoryTreeNodeData> children)
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

    public static string CalamityCategoryLabel(string category)
    {
        if (CalamityCategoryLabelsEs.TryGetValue(category, out var direct)) return direct;
        var segments = category.Split('/');
        if (CalamityCategoryLabelsEs.TryGetValue(segments[0], out var topLabel)) segments[0] = topLabel;
        return string.Join(" - ", segments);
    }

    // Ronda de idioma del 6-sep-2026. Version INGLESA de la de arriba. Aqui no hace falta ninguna
    // tabla: la categoria real de calamity/catalog.json YA viene en ingles ("Weapons/Melee",
    // "Placeables/SunkenSea"), solo hay que presentarla legible - misma forma "A - B" que la
    // española, separando ademas el CamelCase real ("DraedonsArsenal" -> "Draedons Arsenal",
    // "PlaceableTurrets" -> "Placeable Turrets"). Nada inventado ni traducido a mano: es el
    // propio dato del mod.
    public static string CalamityCategoryLabelEn(string category)
        => string.Join(" - ", category.Split('/').Select(SepararCamelCase));

    // "SunkenSea" -> "Sunken Sea". Respeta las siglas seguidas y no toca lo que ya lleva espacio.
    public static string SepararCamelCase(string texto)
    {
        if (texto.Length < 2 || texto.Contains(' ')) return texto;
        var sb = new System.Text.StringBuilder(texto.Length + 4);
        for (int i = 0; i < texto.Length; i++)
        {
            char c = texto[i];
            bool cortar = i > 0 && char.IsUpper(c)
                && (!char.IsUpper(texto[i - 1]) || (i + 1 < texto.Length && char.IsLower(texto[i + 1])));
            if (cortar) sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
