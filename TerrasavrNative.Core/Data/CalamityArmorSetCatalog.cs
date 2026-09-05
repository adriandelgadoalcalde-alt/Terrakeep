namespace TerrasavrNative.Core.Data;

// C-10b (auditoria de pulido final, ESPEC-pulido-final-libreria-inicio-apariencia.md#9.10,
// cierra la mitad Calamity de L3): catalogo de SET completo, no de pieza suelta - antes
// CalamityCatalogEntryData.SetBonus guardaba el bono SOLO en la pieza de cabeza (0/131 cuerpos/
// piernas de Calamity lo tienen, es una limitacion real del modelo de datos, no un hueco de
// extraccion), lo que dejaba "ActiveCalamitySetBonusText" comparando tres SetBonus que NUNCA
// podian coincidir (cuerpo/piernas siempre null) - "codigo verificado una vez y nunca
// funciono". Se deriva ENTERO de datos que ya existen en catalog.json (Category real tipo
// "Armor/Aerospec", EquipSlot real Head/Body/Legs, los 69 SetBonus ya extraidos) - no hace
// falta ningun fichero nuevo.
//
// Con 1 a 5 cascos reales por categoria (cada clase de daño real tiene el suyo, con su propio
// texto), un cuerpo y unas piernas (excepto Armor/MarniteArchitect, un set real de solo 2
// piezas, igual que Wizard/MagicHat en vanilla - LegsSyntheticId queda null ahi). Categorias sin
// NINGUN casco con SetBonus real (ej. "Armor/Vanity", piezas cosmeticas sueltas de distintos
// jefes, jamas forman un set con bono) se descartan por completo al construir el catalogo.
public sealed class CalamitySetInfo
{
    public required string Category { get; init; }
    public required int BodySyntheticId { get; init; }
    // null = set real de 2 piezas (cabeza+cuerpo, sin piernas - ej. MarniteArchitect).
    public int? LegsSyntheticId { get; init; }
    public required IReadOnlyList<(int HeadSyntheticId, string BonusText)> Heads { get; init; }
}

public sealed class CalamityArmorSetCatalog
{
    private readonly List<CalamitySetInfo> _sets;
    // Cabeza, cuerpo Y piernas de un set real apuntan aqui al MISMO CalamitySetInfo - es lo que
    // permite a ItemStatsFormatter (C-10c) mostrar el bono completo mirando CUALQUIER pieza.
    private readonly Dictionary<int, CalamitySetInfo> _byItemId;

    private CalamityArmorSetCatalog(List<CalamitySetInfo> sets, Dictionary<int, CalamitySetInfo> byItemId)
    {
        _sets = sets;
        _byItemId = byItemId;
    }

    public IReadOnlyList<CalamitySetInfo> Sets => _sets;

    public CalamitySetInfo? FindSetContaining(int itemId) => _byItemId.GetValueOrDefault(itemId);

    // Chequeo en vivo real (gemelo de VanillaArmorSetCatalog.BonusForEquipped): las piezas
    // puestas coinciden con un set real conocido - devuelve el texto de la VARIANTE de casco
    // puesta en concreto (cada casco de un mismo set tiene su propio bono real, no comparten).
    public string? BonusForEquipped(int headItemId, int bodyItemId, int legsItemId)
    {
        if (!_byItemId.TryGetValue(headItemId, out var set) || set.BodySyntheticId != bodyItemId) return null;
        if (set.LegsSyntheticId is int requiredLegs && requiredLegs != legsItemId) return null;
        foreach (var (headId, text) in set.Heads)
            if (headId == headItemId) return text;
        return null;
    }

    public static CalamityArmorSetCatalog Build(CalamityCatalog catalog)
    {
        var sets = new List<CalamitySetInfo>();
        var byItemId = new Dictionary<int, CalamitySetInfo>();

        var byCategory = catalog.Entries
            .Where(e => e.Category.StartsWith("Armor/", StringComparison.Ordinal))
            .GroupBy(e => e.Category, StringComparer.Ordinal);

        foreach (var group in byCategory)
        {
            var body = group.FirstOrDefault(e => e.EquipSlot == "Body");
            if (body is null) continue; // "Armor/Vanity" y similares: piezas sueltas, sin cuerpo real de set.
            var legs = group.FirstOrDefault(e => e.EquipSlot == "Legs");
            var heads = group
                .Where(e => e.EquipSlot == "Head" && !string.IsNullOrEmpty(e.SetBonus))
                .Select(e => (e.SyntheticId, e.SetBonus!))
                .ToList();
            if (heads.Count == 0) continue; // sin ningun casco con bono real -> no es un set real con bonificacion.

            var info = new CalamitySetInfo
            {
                Category = group.Key,
                BodySyntheticId = body.SyntheticId,
                LegsSyntheticId = legs?.SyntheticId,
                Heads = heads,
            };
            sets.Add(info);
            byItemId[body.SyntheticId] = info;
            if (legs != null) byItemId[legs.SyntheticId] = info;
            foreach (var (headId, _) in heads) byItemId[headId] = info;
        }

        return new CalamityArmorSetCatalog(sets, byItemId);
    }
}
