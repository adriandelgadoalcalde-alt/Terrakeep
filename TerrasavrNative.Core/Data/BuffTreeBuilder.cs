namespace TerrasavrNative.Core.Data;

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
//
// WS2 de TerrakeepMod (6-sep-2026): antes vivia en TerrasavrNative.App/Services/
// BuffLibraryTreeBuilder.cs y construia CategoryNodeViewModel (WPF/MVVM) directamente. Ahora
// produce CategoryTreeNodeData puro, igual que LibraryTreeBuilder, para que tanto la app de
// escritorio como el mod de tModLoader puedan consumirlo. Las 6 listas literales de ids son un
// puerto real de initLibs() y NO se han tocado en ese movimiento.
public static class BuffTreeBuilder
{
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

    // buffIconResolver resuelve el icono de un buff por id (vanilla real, o id sintetico de
    // Calamity); itemIconResolver resuelve el de un OBJETO - hace falta solo para el icono de
    // la raiz "Calamity (mod)", que por criterio ya establecido es el objeto real
    // CalamityMod/Calamity (ningun buff puede representar "el mod entero"). calamityBuffs y
    // calamityItems pueden ser null: entonces no se añade esa carpeta madre.
    public static List<CategoryTreeNodeData> BuildBuffTree(
        VanillaBuffCatalog vanillaBuffs,
        CalamityBuffCatalog? calamityBuffs,
        CalamityCatalog? calamityItems,
        Func<int, string?> buffIconResolver,
        Func<int, string?> itemIconResolver)
    {
        var roots = new List<CategoryTreeNodeData>
        {
            // Ronda de idioma del 6-sep-2026: el segundo nombre es el ingles REAL de la misma
            // carpeta en Terrasavr (lib.buffs), no una traduccion inventada - "Special" y
            // "Offensivo" son el mismo caso ya documentado arriba (el español de Terrasavr trae
            // ese anglicismo tal cual, el ingles original es "Special"/"Offensive").
            BuildNamed("Utilidad", "Utility", Utility, buffIconResolver),
            BuildNamed("Offensivo", "Offensive", Offensive, buffIconResolver),
            BuildNamed("Defensivo", "Defensive", Defensive, buffIconResolver),
            BuildNamed("Special", "Special", Special, buffIconResolver),
            BuildNamed("Mascota", "Pet", Pets, buffIconResolver),
            BuildNamed("Negativo", "Negative", Negative, buffIconResolver),
            BuildIndex(vanillaBuffs, buffIconResolver),
        };
        if (calamityBuffs != null)
            roots.Add(BuildCalamityRoot(calamityBuffs, calamityItems, buffIconResolver, itemIconResolver));
        return roots;
    }

    private static CategoryTreeNodeData BuildNamed(string name, string nameEn, int[] ids, Func<int, string?> buffIconResolver)
    {
        var ordered = ids.ToList();
        // FullPath sigue siendo el nombre ESPAÑOL (clave estable ya persistida en preferencias y
        // usada por los tests) - solo cambia lo que se muestra.
        return new CategoryTreeNodeData(
            $"{name} ({ids.Length})", name, buffIconResolver(ids[0]),
            ordered, new HashSet<int>(ordered), [], $"{nameEn} ({ids.Length})");
    }

    // "Indice" real: paginas de 33 en 33 sobre TODOS los buffs vanilla conocidos (1..N), para
    // encontrar cualquier buff que no caiga en ninguna de las 6 carpetas curadas de arriba -
    // mismo espiritu que "Items by ID" en el arbol real de objetos.
    private static CategoryTreeNodeData BuildIndex(VanillaBuffCatalog vanilla, Func<int, string?> buffIconResolver)
    {
        var allIds = vanilla.AllEntries().Select(e => e.Id).OrderBy(id => id).ToList();
        // C-08 (auditoria de pulido final, cierra L1): unico nodo raiz del arbol de buffs sin
        // IconPath - las 6 carpetas curadas y las paginas de Calamity si lo tienen, con el mismo
        // criterio ("el primer objeto real de la categoria"). De paso, tilde y recuento como el
        // resto de nodos raiz (unico sin ninguno de los dos).
        string? rootIcon = allIds.Count > 0 ? buffIconResolver(allIds[0]) : null;

        var pages = new List<CategoryTreeNodeData>();
        int max = allIds.Count == 0 ? 0 : allIds[^1];
        for (int start = 1; start <= max; start += IndexPageSize)
        {
            int end = Math.Min(start + IndexPageSize - 1, max);
            var pageIds = allIds.Where(id => id >= start && id <= end).ToList();
            if (pageIds.Count == 0) continue;
            pages.Add(new CategoryTreeNodeData(
                $"Indice ({start}-{end})", $"Indice/{start}-{end}", buffIconResolver(pageIds[0]),
                pageIds, new HashSet<int>(pageIds), [], $"Index ({start}-{end})"));
        }
        var (ordered, set) = LibraryTreeBuilder.OrderedUnion(pages);
        return new CategoryTreeNodeData($"Índice ({allIds.Count})", "Indice", rootIcon, ordered, set, pages, $"Index ({allIds.Count})");
    }

    // Mismo patron real que ya usa LibraryTreeBuilder para los objetos de Calamity (agrupar por
    // categoria real, paginar hojas >40) - reutilizado literalmente via BuildGroupedRoot. Aqui
    // la categoria real viene de calamity/buffs.json campo "category" (Summon/StatBuffs/
    // DamageOverTime/Pets/Alcohol/StatDebuffs/Potions/Mounts/Placeables, 305 buffs reales, 2 sin
    // categoria real -> "Otros"). Ninguna de esas categorias lleva barra, asi que salen todas
    // colgando directas de la raiz (BuildGroupedRoot no crea carpeta intermedia para un grupo de
    // un solo miembro), igual que antes de compartir el algoritmo.
    private static CategoryTreeNodeData BuildCalamityRoot(
        CalamityBuffCatalog catalog, CalamityCatalog? calamityItems,
        Func<int, string?> buffIconResolver, Func<int, string?> itemIconResolver)
    {
        var byCategory = catalog.Entries
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Category) ? "Otros" : e.Category!)
            .ToDictionary(g => g.Key, g => g.Select(e => e.SyntheticId).ToList());

        // Hallazgo real (feedback directo tras dar por cerrado C-08): la bitacora del bloque 1
        // afirmaba que "Calamity (mod)" ya llevaba IconPath "mismo criterio que el resto" pero
        // el codigo real nunca se lo asigno - solo "Indice" lo recibio de verdad. Mismo criterio
        // YA establecido en el arbol de OBJETOS: ningun buff puede representar "el mod entero",
        // se usa el icono del objeto real CalamityMod/Calamity (el trofeo del mod).
        var rootIconEntry = calamityItems?.ByModAndInternal("CalamityMod", "Calamity");
        string? rootIcon = rootIconEntry != null ? itemIconResolver(rootIconEntry.SyntheticId) : null;

        return LibraryTreeBuilder.BuildGroupedRoot(
            "Calamity (mod)", "Calamity", rootIcon, byCategory,
            CalamityBuffCategoryLabel, buffIconResolver,
            page => $"Página {page}",
            rootNameEn: "Calamity (mod)",
            categoryLabelEn: CalamityBuffCategoryLabelEn,
            pageLabelEn: page => $"Page {page}");
    }

    // Sin precedente real en español que copiar aqui (a diferencia de CALAMITY_CATEGORY_LABELS
    // de objetos, que sí viene de overrides.js real) - traduccion literal razonada de las 9
    // categorias reales de calamity/buffs.json.
    public static string CalamityBuffCategoryLabel(string category) => category switch
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

    // Ronda de idioma del 6-sep-2026: version inglesa. La categoria real de calamity/buffs.json
    // YA viene en ingles, solo hay que separar el CamelCase - nada que traducir a mano. Unica
    // excepcion real: "Otros" es una etiqueta NUESTRA (los 2 buffs reales sin categoria en el
    // catalogo), no un dato del mod, y por eso si tiene traduccion propia.
    public static string CalamityBuffCategoryLabelEn(string category) => category switch
    {
        "Otros" => "Other",
        _ => LibraryTreeBuilder.SepararCamelCase(category),
    };
}
