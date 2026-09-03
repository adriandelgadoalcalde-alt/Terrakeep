using System.Text.Json;

namespace TerrasavrNative.Core.Data;

// Categorias reales de prefijo vanilla (Item.GetPrefixCategories() real, ver
// scripts/extraer-prefijos-vanilla.py) - Terraria vanilla NO tiene una categoria "Summon"
// propia (los objetos de invocacion vanilla caen dentro de Magic, ver MagicAndSummon en
// PrefixLegacy.cs); Summon solo existe para objetos de Calamity (ver PrefixEligibility.cs).
[Flags]
public enum PrefixCategory
{
    None = 0,
    Melee = 1,
    Ranged = 2,
    Magic = 4,
    AnyWeapon = 8,
    Accessory = 16,
    Summon = 32, // solo Calamity, ver PrefixEligibility.For
    // H3-05 (tercera auditoria de Opus, Fable): solo Calamity (arma con damageType Rogue real,
    // RogueDamageClass) - los 17 ModPrefix reales de arma de RoguePrefixCatalog no tenian
    // NINGUN camino manual para elegirlos (solo "mejor prefijo" automatico, PrefixSuggester).
    Rogue = 64,
}

// Elegibilidad REAL de prefijo por objeto vanilla, extraida del codigo decompilado de
// tModLoader (PrefixLegacy.cs/ItemID.cs/Item.cs - ver el script de extraccion para el
// detalle exacto). Arregla el bug real reportado 1-sep-2026 ("me esta permitiendo poner
// prefijo a objetos que no deberia"): antes de esto no habia ninguna tabla real de que
// prefijos son legales para que objeto, solo la lista plana completa de 118 prefijos.
public sealed class PrefixRulesCatalog
{
    private readonly Dictionary<string, int[]> _pools;
    private readonly Dictionary<int, PrefixCategory> _categories;
    private readonly Dictionary<int, int[]> _legalPrefixes;

    private static readonly Dictionary<string, PrefixCategory> CategoryTagMap = new()
    {
        ["melee"] = PrefixCategory.Melee,
        ["ranged"] = PrefixCategory.Ranged,
        ["magic"] = PrefixCategory.Magic,
        ["anyWeapon"] = PrefixCategory.AnyWeapon,
        ["accessory"] = PrefixCategory.Accessory,
    };

    private PrefixRulesCatalog(Dictionary<string, int[]> pools, Dictionary<int, PrefixCategory> categories, Dictionary<int, int[]> legalPrefixes)
    {
        _pools = pools;
        _categories = categories;
        _legalPrefixes = legalPrefixes;
    }

    // Las categorias reales del objeto (para decidir que grupos del panel Editar mostrar) -
    // PrefixCategory.None si el objeto no admite ningun prefijo (material, bloque, etc).
    public PrefixCategory VanillaCategories(int itemId) =>
        _categories.TryGetValue(itemId, out var cats) ? cats : PrefixCategory.None;

    // La lista REAL y completa de ids de prefijo legales para ESE objeto exacto (un unico
    // pool, replica de Item.GetRollablePrefixes) - vacia si el objeto no admite prefijo.
    public IReadOnlyList<int> LegalPrefixes(int itemId) =>
        _legalPrefixes.TryGetValue(itemId, out var ids) ? ids : [];

    public bool IsLegal(int itemId, int prefixId) => LegalPrefixes(itemId).Contains(prefixId);

    public static PrefixRulesCatalog LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    public static PrefixRulesCatalog LoadFromStream(Stream stream)
    {
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;

        var pools = new Dictionary<string, int[]>();
        foreach (var prop in root.GetProperty("prefixesByCategory").EnumerateObject())
            pools[prop.Name] = prop.Value.EnumerateArray().Select(e => e.GetInt32()).ToArray();

        var categories = new Dictionary<int, PrefixCategory>();
        foreach (var prop in root.GetProperty("itemCategories").EnumerateObject())
        {
            var cat = PrefixCategory.None;
            foreach (var tag in prop.Value.EnumerateArray())
                if (CategoryTagMap.TryGetValue(tag.GetString() ?? "", out var flag))
                    cat |= flag;
            categories[int.Parse(prop.Name)] = cat;
        }

        var legalPrefixes = new Dictionary<int, int[]>();
        foreach (var prop in root.GetProperty("itemPool").EnumerateObject())
        {
            var poolKey = prop.Value.GetString() ?? "";
            if (pools.TryGetValue(poolKey, out var ids))
                legalPrefixes[int.Parse(prop.Name)] = ids;
        }

        return new PrefixRulesCatalog(pools, categories, legalPrefixes);
    }
}
