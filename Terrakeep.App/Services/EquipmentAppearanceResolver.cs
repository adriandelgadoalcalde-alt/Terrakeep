using System.IO;
using Terrakeep.Core.Calamity;
using Terrakeep.Core.Data;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.Services;

// Resuelve que sprites reales de armadura/vanidad hay que dibujar sobre el doll de un
// personaje - pedido explicito del usuario (3-sep-2026): "los personajes de inicio no se
// visualizan como realmente son en el juego... que muestre el personaje con la vanidad que
// tiene cada uno pero fiel al guardado igual que lo que lleva puesto de vanidad".
//
// Regla real del propio juego (Player.cs decompilado, armor[0..9]=funcional/armor[10..19]=
// vanidad - aqui PlrLoadout.Items/Social, mismos 10 slots cada uno): si el slot de VANIDAD
// tiene un objeto puesto, ese es el que se VE; si esta vacio, se ve el objeto FUNCIONAL.
// Solo cubre los 3 slots de armadura real (indices 0=cabeza/1=cuerpo/2=piernas en ambos
// arrays) - los indices 3..9 son accesorios, sin capa visual propia sobre el doll (alcance
// ya deliberado de PlayerPreviewRenderer).
//
// loadouts[0] (PrimaryLoadout, el "mirror" de lo puesto de verdad) es el que hay que pasar
// aqui, NUNCA loadouts[1..3] - esos son los 3 loadouts guardados, no necesariamente el que
// esta activo.
//
// ALCANCE DELIBERADO, documentado y no oculto: no respeta los 3 bytes de "ocultar equipo"
// del panel de vanidad del juego real (HideVisual1/HideVisual2/HideMisc en PlrCharacter) -
// el bit exacto que le corresponde a cada slot dentro de esos bytes no se investigo a fondo
// (no compensaba el riesgo de esconder o mostrar la pieza equivocada por una lectura de bit
// erronea). Un personaje que use ese toggle poco frecuente vera su pieza dibujada aunque el
// juego real la esconda - hueco real, ya conocido, no un bug silencioso.
public sealed class EquipmentAppearanceResolver
{
    private readonly VanillaArmorSlotCatalog _vanillaSlots;
    private readonly CalamityCatalog _calamity;

    public EquipmentAppearanceResolver(VanillaArmorSlotCatalog vanillaSlots, CalamityCatalog calamity)
    {
        _vanillaSlots = vanillaSlots;
        _calamity = calamity;
    }

    public PlayerPreviewRenderer.EquippedArmor Resolve(PlrLoadout loadout)
    {
        var headSlot = Visible(loadout, 0);
        var bodySlot = Visible(loadout, 1);
        var legsSlot = Visible(loadout, 2);
        return new(
            ResolveHead(headSlot),
            ResolveBody(bodySlot),
            ResolveLegs(legsSlot),
            // H6-07: el indice REAL de headSlot (Terraria.Player.head, la misma tabla que
            // ArmorHead[]/armor_head/{slot}.png) - solo se conoce para objetos VANILLA (ver
            // ResolveVanillaPath); Calamity no comparte esta numeracion, null a proposito.
            ResolveHeadSlot(headSlot),
            // H6-01-b (advisor Opus): idem para bodySlot/legSlot - hacen falta como ID (no solo
            // como ruta) para poder aplicar SetMatch/hidesTopSkin/hidesBottomSkin/
            // GetMatchingBodyExtension en PlayerPreviewRenderer (ver PlayerBodyDrawTables).
            ResolveBodySlot(bodySlot),
            ResolveLegsSlot(legsSlot));
    }

    private static PlrItemSlot Visible(PlrLoadout loadout, int index) =>
        loadout.Social[index].IsEmpty ? loadout.Items[index] : loadout.Social[index];

    private string? ResolveHead(PlrItemSlot slot) => Resolve(slot, "Head", e => e.Head, "armor_head");
    private string? ResolveBody(PlrItemSlot slot) => Resolve(slot, "Body", e => e.Body, "armor_body");
    private string? ResolveLegs(PlrItemSlot slot) => Resolve(slot, "Legs", e => e.Legs, "armor_legs");

    private int? ResolveHeadSlot(PlrItemSlot slot)
    {
        if (slot.IsEmpty || slot.Id >= CalamityIds.ItemIdBase) return null;
        return _vanillaSlots.ById(slot.Id)?.Head;
    }

    // Calamity no comparte la numeracion de bodySlot/legSlot vanilla (registra sus propios
    // equip slots por mod) - null a proposito, ESPEC-dibujado-sprites.md#7.7 punto 8: el
    // camino fiel-por-defecto para una pieza de Calamity es "hasBody=true, sin SetMatch".
    private int? ResolveBodySlot(PlrItemSlot slot)
    {
        if (slot.IsEmpty || slot.Id >= CalamityIds.ItemIdBase) return null;
        return _vanillaSlots.ById(slot.Id)?.Body;
    }

    private int? ResolveLegsSlot(PlrItemSlot slot)
    {
        if (slot.IsEmpty || slot.Id >= CalamityIds.ItemIdBase) return null;
        return _vanillaSlots.ById(slot.Id)?.Legs;
    }

    private string? Resolve(PlrItemSlot slot, string calamitySuffix, Func<VanillaArmorSlotEntry, int?> vanillaPick, string vanillaDir)
    {
        if (slot.IsEmpty) return null;

        string? path = slot.Id >= CalamityIds.ItemIdBase
            ? ResolveCalamityPath(slot.Id, calamitySuffix)
            : ResolveVanillaPath(slot.Id, vanillaPick, vanillaDir);

        // Unos pocos ids reales (~1-2%) se quedaron sin sprite extraible de la instalacion
        // real (hoja mas pequeña que el lienzo estandar, ver extraer-sprites-armadura-
        // vanilla.js) - "lo que no se encuentra no se inventa", el doll se queda sin esa
        // capa en vez de intentar cargar un fichero que no existe.
        return path is not null && File.Exists(path) ? path : null;
    }

    private string? ResolveCalamityPath(int itemId, string calamitySuffix)
    {
        var entry = _calamity.BySyntheticId(itemId);
        if (entry is null || entry.EquipSlot != calamitySuffix) return null;
        return Path.Combine(AppContext.BaseDirectory, "Assets", "calamity", "icons", entry.Internal + "_" + calamitySuffix + ".png");
    }

    private string? ResolveVanillaPath(int itemId, Func<VanillaArmorSlotEntry, int?> pick, string vanillaDir)
    {
        var entry = _vanillaSlots.ById(itemId);
        int? spriteId = entry is null ? null : pick(entry);
        return spriteId is null ? null : Path.Combine(AppContext.BaseDirectory, "Assets", "player", vanillaDir, spriteId + ".png");
    }
}
