using System.Text.Json;

namespace Terrakeep.Core.Data;

// Un nodo del arbol REAL de la Libreria de Terrasavr (vanilla_library_tree.json, extraido y
// ejecutado de verdad desde el propio script.js real - ver scripts/
// extraer-arbol-libreria-vanilla.js). A diferencia de VanillaCategoryCatalog (una categoria
// unica por objeto, pensada solo para el texto de daño de los tooltips), este arbol permite
// que un mismo objeto aparezca en MAS de una carpeta a la vez (ej. una espada de hierro cae
// en "Materials/Iron & Lead" Y en "Categories/Weapons/Melee damage") - asi es el real.
public sealed class VanillaLibraryNode
{
    public string Name { get; }
    // Id de objeto vanilla real cuyo icono representa esta carpeta (0 = sin icono real).
    public int Icon { get; }
    public bool IsLeaf { get; }
    // Solo si !IsLeaf.
    public IReadOnlyList<VanillaLibraryNode> Children { get; }
    // Solo si IsLeaf - ya sin los "0" de relleno de la rejilla curada a mano del propio
    // Terrasavr (huecos reales del propio arbol, no objetos).
    public IReadOnlyList<int> ItemIds { get; }

    public VanillaLibraryNode(string name, int icon, bool isLeaf, IReadOnlyList<VanillaLibraryNode> children, IReadOnlyList<int> itemIds)
    {
        Name = name;
        Icon = icon;
        IsLeaf = isLeaf;
        Children = children;
        ItemIds = itemIds;
    }
}

public sealed class VanillaLibraryTreeCatalog
{
    public IReadOnlyList<VanillaLibraryNode> RootNodes { get; }

    private VanillaLibraryTreeCatalog(IReadOnlyList<VanillaLibraryNode> rootNodes) => RootNodes = rootNodes;

    public static VanillaLibraryTreeCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static VanillaLibraryTreeCatalog LoadFromStream(Stream stream)
    {
        using var doc = JsonDocument.Parse(stream);
        var roots = doc.RootElement.EnumerateArray().Select(ParseNode).ToList();
        return new VanillaLibraryTreeCatalog(roots);
    }

    // "type" real: 1 = carpeta (app.ShDir, "nodes" es un array de nodos hijos), 2 = hoja de
    // objetos (app.ShItems, "nodes" es un array de ids planos) - mismo esquema que el propio
    // motor real, ver ub/mb en script.js.
    private static VanillaLibraryNode ParseNode(JsonElement el)
    {
        int icon = el.GetProperty("icon").GetInt32();
        int type = el.GetProperty("type").GetInt32();
        string name = el.GetProperty("name").GetString() ?? string.Empty;
        var nodesEl = el.GetProperty("nodes");

        if (type == 2)
        {
            var ids = nodesEl.EnumerateArray().Select(e => e.GetInt32()).Where(id => id != 0).ToList();
            return new VanillaLibraryNode(name, icon, isLeaf: true, children: [], itemIds: ids);
        }

        var children = nodesEl.EnumerateArray().Select(ParseNode).ToList();
        return new VanillaLibraryNode(name, icon, isLeaf: false, children: children, itemIds: []);
    }
}
