using TerrasavrNative.App.ViewModels;
using TerrasavrNative.Core.Data;

namespace TerrasavrNative.App.Services;

// Arbol de carpetas REAL de Terrasavr para la Libreria de buffs (Fase 2 del rework de Buffs,
// pregunta a Opus sobre el diseño 2-sep-2026, cuarta pasada: "extrae de terrasav la jerarquia
// de ramas para hacerlo exactamente igual"). Confirmado real por Opus: la jerarquia vive en
// app.BuffSide (no app.TabBuffs, que solo pinta iconos - error real de la investigacion
// previa), metodo initLibs() real (reference/terrasavr-real/script.beautified.js, ~lineas
// 2996-3090) - 6 categorias curadas + un indice paginado que cubre TODOS los buffs 1..N.
// Listas literales reales (mismo criterio que PrefixGroupCatalog/CALAMITY_CATEGORY_LABELS,
// puertos literales de Terrasavr real, no inventados) - pertenencia multiple real confirmada
// (3 en Utilidad y Special; 26 en Defensivo y Special; 86 en Offensivo y Negativo), igual que
// ya soporta CategoryNodeViewModel para el arbol de objetos.
public static class BuffLibraryTreeBuilder
{
    private const int LeafPageSize = 40;
    private const int IndexPageSize = 33;

    // Etiquetas reales de lib.buffs (Terrasavr.es-ES.json) - los nombres "Offensivo"/"Special"
    // son typos/anglicismos REALES de Terrasavr, no se corrigen (mismo criterio que
    // "Extremandamente" en ItemStatsFormatter).
    private static readonly int[] Utility = [1, 4, 8, 9, 10, 11, 12, 15, 18, 19, 27, 34, 57, 3, 63, 101, 102];
    private static readonly int[] Offensive = [7, 13, 86, 16, 25, 17, 71, 73, 74, 75, 76, 77, 78, 79, 93, 98, 99, 100];
    private static readonly int[] Defensive = [5, 14, 26, 43, 48, 58, 59, 62, 87, 89, 95, 96, 97];
    private static readonly int[] Special = [3, 6, 26, 28, 29, 60, 64, 49, 83, 90];
    private static readonly int[] Pets = [40, 41, 42, 45, 50, 51, 52, 53, 54, 55, 56, 61, 65, 66, 81, 82, 84, 85, 91, 92];
    private static readonly int[] Negative = [21, 20, 22, 23, 24, 30, 31, 32, 33, 35, 36, 37, 38, 44, 46, 47, 67, 68, 69, 70, 72, 80, 86, 88, 94, 103];

    public static List<CategoryNodeViewModel> Build(CharacterFileService service)
    {
        var vanilla = service.VanillaBuffs;
        return
        [
            BuildNamed("Utilidad", Utility, vanilla),
            BuildNamed("Offensivo", Offensive, vanilla),
            BuildNamed("Defensivo", Defensive, vanilla),
            BuildNamed("Special", Special, vanilla),
            BuildNamed("Mascota", Pets, vanilla),
            BuildNamed("Negativo", Negative, vanilla),
            BuildIndex(vanilla),
            BuildCalamityRoot(service),
        ];
    }

    private static CategoryNodeViewModel BuildNamed(string name, int[] ids, VanillaBuffCatalog vanilla)
    {
        var node = new CategoryNodeViewModel($"{name} ({ids.Length})", name)
        {
            IconPath = VanillaBuffIconResolver.GetIconPath(ids[0]),
            ItemIdsOrdered = ids.ToList(),
            ItemIdSet = new HashSet<int>(ids),
        };
        node.ItemCount = node.ItemIdSet.Count;
        return node;
    }

    // "Indice" real: paginas de 33 en 33 sobre TODOS los buffs vanilla conocidos (1..N), para
    // encontrar cualquier buff que no caiga en ninguna de las 6 carpetas curadas de arriba -
    // mismo espiritu que "Items by ID" en el arbol real de objetos.
    private static CategoryNodeViewModel BuildIndex(VanillaBuffCatalog vanilla)
    {
        var allIds = vanilla.AllEntries().Select(e => e.Id).OrderBy(id => id).ToList();
        // C-08 (auditoria de pulido final, cierra L1): unico nodo raiz del arbol de buffs sin
        // IconPath - las 6 carpetas curadas y las paginas de Calamity si lo tienen, con el mismo
        // criterio ("el primer objeto real de la categoria"). De paso, tilde y recuento como el
        // resto de nodos raiz (unico sin ninguno de los dos).
        var root = new CategoryNodeViewModel($"Índice ({allIds.Count})", "Indice")
        {
            IconPath = allIds.Count > 0 ? VanillaBuffIconResolver.GetIconPath(allIds[0]) : null,
        };
        int max = allIds.Count == 0 ? 0 : allIds[^1];
        for (int start = 1; start <= max; start += IndexPageSize)
        {
            int end = Math.Min(start + IndexPageSize - 1, max);
            var pageIds = allIds.Where(id => id >= start && id <= end).ToList();
            if (pageIds.Count == 0) continue;
            var page = new CategoryNodeViewModel($"Indice ({start}-{end})", $"Indice/{start}-{end}")
            {
                IconPath = VanillaBuffIconResolver.GetIconPath(pageIds[0]),
                ItemIdsOrdered = pageIds,
                ItemIdSet = new HashSet<int>(pageIds),
            };
            page.ItemCount = page.ItemIdSet.Count;
            root.Children.Add(page);
        }
        ApplyOrderedUnion(root);
        return root;
    }

    // Puerto del mismo patron real que ya usa LibraryCategoryTreeBuilder.BuildCalamityRoot para
    // objetos (agrupar por categoria real, paginar hojas >40 en "Página N") - aqui la categoria
    // real viene de calamity/buffs.json campo "category" (Summon/StatBuffs/DamageOverTime/
    // Pets/Alcohol/StatDebuffs/Potions/Mounts/Placeables, 305 buffs reales, 2 sin categoria
    // real -> "Otros").
    private static CategoryNodeViewModel BuildCalamityRoot(CharacterFileService service)
    {
        var catalog = service.CalamityBuffCatalog;
        var byCategory = catalog.Entries
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Category) ? "Otros" : e.Category!)
            .ToDictionary(g => g.Key, g => g.Select(e => e.SyntheticId).ToList());
        var cats = byCategory.Keys.OrderBy(c => c, StringComparer.Ordinal).ToList();

        string? IconOf(int syntheticId) =>
            catalog.BySyntheticId(syntheticId)?.Icon is { } icon
                ? "pack://siteoforigin:,,,/Assets/calamity/buff_icons/" + icon
                : null;

        CategoryNodeViewModel BuildCategoryNode(string cat)
        {
            var ids = byCategory[cat];
            string label = $"{CalamityBuffCategoryLabel(cat)} ({ids.Count})";
            var node = new CategoryNodeViewModel(label, "Calamity/" + cat) { IconPath = IconOf(ids[0]) };

            if (ids.Count <= LeafPageSize)
            {
                node.ItemIdsOrdered = ids.ToList();
                node.ItemIdSet = new HashSet<int>(ids);
                node.ItemCount = node.ItemIdSet.Count;
                return node;
            }

            for (int i = 0; i < ids.Count; i += LeafPageSize)
            {
                var chunk = ids.Skip(i).Take(LeafPageSize).ToList();
                var page = new CategoryNodeViewModel($"Página {i / LeafPageSize + 1}", $"{node.FullPath}/Page{i / LeafPageSize + 1}")
                {
                    IconPath = IconOf(chunk[0]),
                    ItemIdsOrdered = chunk,
                    ItemIdSet = new HashSet<int>(chunk),
                };
                page.ItemCount = page.ItemIdSet.Count;
                node.Children.Add(page);
            }
            ApplyOrderedUnion(node);
            return node;
        }

        var root = new CategoryNodeViewModel("Calamity (mod)", "Calamity");
        foreach (var cat in cats)
            root.Children.Add(BuildCategoryNode(cat));
        ApplyOrderedUnion(root);
        return root;
    }

    private static void ApplyOrderedUnion(CategoryNodeViewModel node)
    {
        var seen = new HashSet<int>();
        var ordered = new List<int>();
        foreach (var child in node.Children)
            foreach (int id in child.ItemIdsOrdered)
                if (seen.Add(id)) ordered.Add(id);
        node.ItemIdsOrdered = ordered;
        node.ItemIdSet = seen;
        node.ItemCount = seen.Count;
    }

    // Sin precedente real en español que copiar aqui (a diferencia de CALAMITY_CATEGORY_LABELS
    // de objetos, que sí viene de overrides.js real) - traduccion literal razonada de las 9
    // categorias reales de calamity/buffs.json.
    private static string CalamityBuffCategoryLabel(string category) => category switch
    {
        "Summon" => "Invocación",
        "StatBuffs" => "Bonificaciones",
        "DamageOverTime" => "Daño continuo",
        "Pets" => "Mascotas",
        "Alcohol" => "Alcohol",
        "StatDebuffs" => "Penalizaciones",
        "Potions" => "Pociones",
        "Mounts" => "Monturas",
        "Placeables" => "Colocables",
        "Otros" => "Otros",
        _ => category,
    };
}
