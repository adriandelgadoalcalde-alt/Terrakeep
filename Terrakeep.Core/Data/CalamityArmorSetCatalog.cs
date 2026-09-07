namespace Terrakeep.Core.Data;

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
// Un casco real del set, con su propio bono. Ronda de traduccion del CONTENIDO del juego
// (6-sep-2026): antes esto era una tupla `(int, string BonusText)` con el texto YA resuelto al
// construir el catalogo, lo que congelaba el bono en el idioma que hubiera al arrancar - un
// cambio de idioma en vivo no lo tocaba. Ahora se guarda la ENTRADA real y el texto se resuelve
// en cada lectura, igual que el resto de catalogos de contenido.
public sealed class CalamitySetHead(CalamityCatalogEntry entry)
{
    public int HeadSyntheticId => entry.SyntheticId;
    public string BonusText => entry.SetBonus ?? string.Empty;
    public string BonusTextFor(string language) => entry.SetBonusFor(language) ?? string.Empty;
    public string HeadName => entry.DisplayName;
    public string HeadNameFor(string language) => entry.DisplayNameFor(language);
}

public sealed class CalamitySetInfo
{
    public required string Category { get; init; }
    public required int BodySyntheticId { get; init; }
    // null = set real de 2 piezas (cabeza+cuerpo, sin piernas - ej. MarniteArchitect).
    public int? LegsSyntheticId { get; init; }
    public required IReadOnlyList<CalamitySetHead> Heads { get; init; }
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
        foreach (var head in set.Heads)
            if (head.HeadSyntheticId == headItemId) return head.BonusText;
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
            // SetBonusFor(Spanish), no SetBonus: "este casco tiene bono real" es una propiedad
            // del DATO, no del idioma activo - preguntarlo por el idioma vivo haria que la
            // FORMA del catalogo dependiera del idioma que hubiera al construirlo.
            var heads = group
                .Where(e => e.EquipSlot == "Head" && !string.IsNullOrEmpty(e.SetBonusFor(LocalizedContent.Spanish)))
                .Select(e => new CalamitySetHead(e))
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
            foreach (var head in heads) byItemId[head.HeadSyntheticId] = info;
        }

        return new CalamityArmorSetCatalog(sets, byItemId);
    }
}
