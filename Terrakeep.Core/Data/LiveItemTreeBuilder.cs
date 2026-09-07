namespace Terrakeep.Core.Data;

// Lo MINIMO que hace falta saber de un objeto ya cargado por el juego para poder construir con
// el un arbol navegable de Librería. Deliberadamente neutral: ni un solo tipo de Terraria/
// tModLoader aparece aqui, porque Terrakeep.Core no depende de ellos (y no debe empezar a
// hacerlo - dejaria de compilar para net10.0, que es el destino que consume la app de
// escritorio).
//
// WS2 de TerrakeepMod (6-sep-2026), decision del plan: dentro del juego NO se porta el
// calamity/catalog.json estatico, se descubre el contenido EN VIVO recorriendo
// ContentSamples.ItemsByType (la instancia real de cada Item que el juego ya tiene cargada) -
// asi el arbol cubre automaticamente CUALQUIER mod instalado, no solo Calamity.
//
// >>> ESA EXTRACCION VIVE EN EL LADO DEL MOD, NO AQUI <<<
// El mod (WS3) recorrera ContentSamples.ItemsByType y rellenara un LiveItemInfo por objeto
// (Id = item.type, Name = item.Name / Lang.GetItemNameValue(type), ModName = el nombre del mod
// dueño via ItemLoader.GetItem(type)?.Mod?.Name -> "Terraria" para vanilla, Category = la ruta
// de carpeta que decida el propio mod a partir del Item real - DamageType/accessory/headSlot/
// createTile...). Lo de aqui es solo la parte PURA y testeable: convertir esa lista ya extraida
// en el mismo CategoryTreeNodeData que usa el arbol de la app de escritorio.
public readonly record struct LiveItemInfo(int Id, string Name, string ModName, string Category)
{
    // Extras opcionales - no hacen falta para agrupar, pero el extractor los tiene a mano y al
    // consumidor (busqueda, tooltips, orden) le sirven. Nunca se inventan: quien no los sepa,
    // los deja en su valor por defecto.
    public string? EquipSlot { get; init; }
    public int Rarity { get; init; }
}

// Convierte una lista de objetos descubiertos en vivo en el mismo arbol de carpetas
// (CategoryTreeNodeData) que produce LibraryTreeBuilder para la app de escritorio - reutilizando
// literalmente su algoritmo real de agrupado/paginado (BuildGroupedRoot), no una copia.
public static class LiveItemTreeBuilder
{
    // Una carpeta raiz por MOD (el mod vanilla del juego primero si esta presente, el resto en
    // orden ordinal - mismo criterio estable que el resto del arbol), y dentro de cada una, sus
    // categorias agrupadas/paginadas igual que "Calamity (mod)" en la app de escritorio.
    //
    // categoryLabel traduce el nombre de una categoria a lo que se muestra (la app de escritorio
    // pasaria LibraryTreeBuilder.CalamityCategoryLabel; el mod, lo que saque de su propia
    // localizacion) - por defecto, la categoria tal cual. pageLabel etiqueta las paginas de una
    // hoja de mas de 40 objetos. iconResolver resuelve el icono de una carpeta a partir del id
    // real de su primer objeto (en el mod, la textura ya cargada; null si no hay).
    public static List<CategoryTreeNodeData> BuildTree(
        IEnumerable<LiveItemInfo> items,
        Func<int, string?> iconResolver,
        Func<string, string>? categoryLabel = null,
        Func<int, string>? pageLabel = null,
        string vanillaModName = "Terraria")
    {
        categoryLabel ??= c => c;
        pageLabel ??= n => $"Página {n}";

        var byMod = new Dictionary<string, Dictionary<string, List<int>>>();
        var modOrder = new List<string>();
        var seenIds = new HashSet<int>();
        foreach (var item in items)
        {
            // Un mismo id no puede aparecer dos veces (ContentSamples.ItemsByType es un
            // diccionario por tipo, pero el extractor podria concatenar fuentes) - se queda la
            // primera aparicion, igual que hace OrderedUnion con la pertenencia multiple.
            if (!seenIds.Add(item.Id)) continue;
            string mod = string.IsNullOrWhiteSpace(item.ModName) ? vanillaModName : item.ModName;
            string cat = string.IsNullOrWhiteSpace(item.Category) ? "Otros" : item.Category;
            if (!byMod.TryGetValue(mod, out var cats)) { cats = []; byMod[mod] = cats; modOrder.Add(mod); }
            if (!cats.TryGetValue(cat, out var ids)) { ids = []; cats[cat] = ids; }
            ids.Add(item.Id);
        }

        modOrder.Sort((a, b) =>
            a == vanillaModName ? (b == vanillaModName ? 0 : -1)
            : b == vanillaModName ? 1
            : string.CompareOrdinal(a, b));

        var roots = new List<CategoryTreeNodeData>();
        foreach (var mod in modOrder)
        {
            var cats = byMod[mod];
            int total = cats.Values.Sum(ids => ids.Count);
            roots.Add(LibraryTreeBuilder.BuildGroupedRoot(
                $"{mod} ({total})", mod, RootIcon(cats, iconResolver), cats,
                categoryLabel, iconResolver, pageLabel));
        }
        return roots;
    }

    // Icono de la carpeta del mod: el del primer objeto real de su primera categoria en orden
    // ordinal - mismo criterio que ya usa el resto del arbol ("el primer objeto real de la
    // categoria"), nunca un icono generico inventado.
    private static string? RootIcon(Dictionary<string, List<int>> cats, Func<int, string?> iconResolver)
    {
        foreach (var cat in cats.Keys.OrderBy(c => c, StringComparer.Ordinal))
            if (cats[cat].Count > 0) return iconResolver(cats[cat][0]);
        return null;
    }
}
