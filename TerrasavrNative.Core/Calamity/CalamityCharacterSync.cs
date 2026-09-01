using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.Nbt;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.Core.Calamity;

// Orquestador de fusion/enmascarado entre un PlrCharacter (formato de cable del .plr, sin
// nocion de Calamity) y la raiz NBT de un .tplr - mismo mecanismo que CALAMITY_LIST_MAP/
// calamityMergeIntoPlayer/calamityMaskForVanillaSave/calamitySyncFromPlayer en overrides.js
// real (nombres de clave NBT confirmados leyendo el codigo fuente directamente, no adivinados).
//
// ALCANCE DE ESTA PRIMERA VERSION: cubre los 7 contenedores "planos" que SI se sincronizan de
// verdad con el .tplr (inventory/bank/bank2/bank3/bank4/miscEquips/miscDyes) - el caso mas
// basico y mas probado de la app actual (objetos sueltos/equipo simple). Los 3 contenedores
// adicionales que la version JS solo enmascara por seguridad SIN sincronizar
// (coins/ammo/tempItems - CALAMITY_FLAT_SLOT_FIELDS los incluye pero CALAMITY_LIST_MAP no) y el
// merge/mask/sync de armadura+tinte POR LOADOUT (calamityMergeArmorDyeIntoPlayer/
// calamitySyncArmorDyeFromPlayer, con la logica de "loadout activo" de calamityActiveLoadout
// que usa player.loadouts[currentLoadout] SIN el +1 que si usa el guardado vanilla nativo -
// confirmado leyendo el codigo real, no es un bug de este port sino un comportamiento real y
// replicable de la app actual) quedan FUERA de esta version, pendientes de una siguiente ronda.
public sealed class CalamityCharacterSync(CalamityItemCodec itemCodec)
{
    // Clave NBT del .tplr -> selector/asignador del contenedor correspondiente en PlrCharacter.
    // Nombres de clave confirmados literalmente contra CALAMITY_LIST_MAP en overrides.js real.
    private static readonly (string TplrKey, Func<PlrCharacter, PlrItemSlot[]> Get, Action<PlrCharacter, PlrItemSlot[]> Set)[] FlatContainers =
    [
        ("inventory", c => c.Inventory, (c, v) => c.Inventory = v),
        ("bank", c => c.BankItems, (c, v) => c.BankItems = v),
        ("bank2", c => c.SafeItems, (c, v) => c.SafeItems = v),
        ("bank3", c => c.ForgeItems, (c, v) => c.ForgeItems = v),
        ("bank4", c => c.VoidItems, (c, v) => c.VoidItems = v),
        ("miscEquips", c => c.EquipmentItems, (c, v) => c.EquipmentItems = v),
        ("miscDyes", c => c.EquipmentDyes, (c, v) => c.EquipmentDyes = v),
    ];

    // --- Carga: fusiona los objetos de Calamity del .tplr en un PlrCharacter ya leido. ---
    // Devuelve una vista GameItem[] por contenedor (misma clave que TplrKey) para que la capa
    // de UI pueda mostrar los objetos de Calamity mezclados con los vanilla. El PlrCharacter en
    // si NO se modifica (sus PlrItemSlot[] siguen siendo el reflejo fiel del .plr crudo).
    public Dictionary<string, GameItem[]> MergeAll(PlrCharacter character, NbtCompound? tplrRoot)
    {
        var result = new Dictionary<string, GameItem[]>();
        foreach (var (tplrKey, getSlots, _) in FlatContainers)
        {
            var merged = getSlots(character).ToGameItems();
            if (tplrRoot != null && tplrRoot.Get(tplrKey) is NbtList list)
                MergeListInto(list, merged);
            result[tplrKey] = merged;
        }
        return result;
    }

    private void MergeListInto(NbtList list, GameItem[] target)
    {
        foreach (var itemTag in list.Items)
        {
            if (itemTag is not NbtCompound entry) continue;
            var decoded = itemCodec.Decode(entry); // null = shadow vanilla (mod=Terraria) o item desconocido
            if (decoded == null) continue;
            var (slot, item) = decoded.Value;
            if (slot < 0 || slot >= target.Length) continue;
            target[slot] = item;
        }
    }

    // --- Guardado: dado el estado fusionado (con objetos de Calamity ya colocados por la UI),
    // produce el PlrCharacter enmascarado (seguro para el escritor vanilla) y actualiza la raiz
    // NBT del .tplr in-place. El .tplr de entrada puede ser null (personaje que aun no tenia
    // .tplr, p.ej. se le acaba de añadir el primer objeto de Calamity) - se crea uno nuevo. ---
    public NbtCompound MaskAndSyncAll(PlrCharacter character, Dictionary<string, GameItem[]> merged, NbtCompound? existingTplrRoot)
    {
        var tplrRoot = existingTplrRoot ?? new NbtCompound();

        foreach (var (tplrKey, _, setSlots) in FlatContainers)
        {
            var items = merged[tplrKey];
            setSlots(character, items.ToPlrItemSlots()); // Calamity -> Empty, vanilla intacto
            SyncListFrom(tplrRoot, tplrKey, items);
        }

        return tplrRoot;
    }

    private void SyncListFrom(NbtCompound tplrRoot, string tplrKey, GameItem[] items)
    {
        var existingList = tplrRoot.Get(tplrKey) as NbtList;

        // Las entradas "sombra" de items vanilla (mod=Terraria) que ya hubiera en el .tplr se
        // conservan tal cual - no es responsabilidad de esta capa recrearlas, y el motor
        // vanilla nunca las escribe el mismo (solo relevante si algo mas las puso ahi).
        var keptVanillaShadow = new List<NbtTag>();
        if (existingList != null)
        {
            foreach (var tag in existingList.Items)
            {
                if (tag is NbtCompound entry && (entry.Get("mod") as NbtString)?.Value == "Terraria")
                    keptVanillaShadow.Add(entry);
            }
        }

        // Preservacion de globalData: NO hace falta cruzar contra oldEntryBySlot aqui (a
        // diferencia de overrides.js, que enmascara borrando el item del slot y solo conserva
        // id/count/prefix hasta el momento de sincronizar) - en este diseño GameItem.GlobalData
        // viaja pegado al objeto desde que se fusiona en MergeListInto() y solo se pierde si
        // quien construye/edita el GameItem lo descarta explicitamente. Responsabilidad de la
        // capa de UI (Fase 2): al editar un item de Calamity YA existente en un slot (cambiar
        // prefijo/cantidad), reutilizar el mismo GameItem.GlobalData, no crear uno vacio nuevo.
        var newItems = new List<NbtTag>(keptVanillaShadow);
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (!item.IsCalamity) continue;
            newItems.Add(itemCodec.Encode((short)i, item));
        }

        var newList = new NbtList(NbtTagType.Compound, newItems);
        tplrRoot.Set(tplrKey, newList);
    }
}
