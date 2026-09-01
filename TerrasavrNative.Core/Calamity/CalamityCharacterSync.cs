using TerrasavrNative.Core.Data;
using TerrasavrNative.Core.Model;
using TerrasavrNative.Core.Nbt;
using TerrasavrNative.Core.PlrFormat;

namespace TerrasavrNative.Core.Calamity;

// Orquestador de fusion/enmascarado entre un PlrCharacter (formato de cable del .plr, sin
// nocion de Calamity) y la raiz NBT de un .tplr - mismo mecanismo que CALAMITY_LIST_MAP/
// calamityMergeIntoPlayer/calamityMaskForVanillaSave/calamitySyncFromPlayer en overrides.js
// real (nombres de clave NBT confirmados leyendo el codigo fuente directamente, no adivinados).
//
// ALCANCE DE ESTA VERSION: cubre los 7 contenedores "planos" que SI se sincronizan de
// verdad con el .tplr (inventory/bank/bank2/bank3/bank4/miscEquips/miscDyes) - el caso mas
// basico y mas probado de la app actual (objetos sueltos/equipo simple) - MAS los buffs
// (modBuffs, ver MergeBuffs/SyncBuffs mas abajo). Los 3 contenedores adicionales que la
// version JS solo enmascara por seguridad SIN sincronizar (coins/ammo/tempItems -
// CALAMITY_FLAT_SLOT_FIELDS los incluye pero CALAMITY_LIST_MAP no) y el merge/mask/sync de
// armadura+tinte POR LOADOUT (calamityMergeArmorDyeIntoPlayer/calamitySyncArmorDyeFromPlayer,
// con la logica de "loadout activo" de calamityActiveLoadout que usa
// player.loadouts[currentLoadout] SIN el +1 que si usa el guardado vanilla nativo -
// confirmado leyendo el codigo real, no es un bug de este port sino un comportamiento real y
// replicable de la app actual) quedan FUERA de esta version, pendientes de una siguiente ronda.
public sealed class CalamityCharacterSync(CalamityItemCodec itemCodec, CalamityBuffCatalog buffCatalog)
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
    // de UI pueda mostrar los objetos de Calamity mezclados con los vanilla - para ESTOS 7
    // contenedores el PlrCharacter en si no se modifica (sus PlrItemSlot[] siguen siendo el
    // reflejo fiel del .plr crudo). Los buffs son distintos: MergeBuffs (llamado aqui mismo)
    // SI modifica character.Buffs directamente, porque no hay una "vista fusionada" aparte
    // para buffs - la UI ya lee character.Buffs tal cual (ver MainViewModel), igual que hace
    // la version JS con player.buffs.
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
        MergeBuffs(character, tplrRoot);
        return result;
    }

    // modBuffs (a diferencia de los contenedores de items) NO esta filtrado a "mod != Terraria"
    // - tModLoader desactiva el guardado del array de buffs vanilla nativo en cuanto un mod
    // añade sus propios slots de buff (confirmado leyendo el comentario de PlayerIO.cs), asi
    // que para un personaje modeado modBuffs es la UNICA fuente completa, incluidos los buffs
    // vanilla. Por eso esto no fusiona "por encima" de character.Buffs sino que rellena los
    // huecos vacios en orden (misma logica que calamityMergeBuffsIntoPlayer real: si el .plr
    // ya trajo algun buff vanilla en su propio array, se respeta y se salta; el resto de
    // huecos vacios se rellenan con lo que traiga modBuffs). Limitado al tamaño fijo de
    // character.Buffs (44/22/10 segun version) - si Calamity tiene mas buffs activos a la vez
    // de los que caben, el sobrante simplemente no se ve aqui pero SIGUE en el .tplr sin
    // tocar (SyncBuffs solo reescribe lo que character.Buffs contiene, nunca borra el resto).
    private void MergeBuffs(PlrCharacter character, NbtCompound? tplrRoot)
    {
        if (tplrRoot?.Get("modBuffs") is not NbtList buffList) return;
        var slots = character.Buffs;
        int bi = 0;
        foreach (var tag in buffList.Items)
        {
            if (tag is not NbtCompound entry) continue;
            string? mod = (entry.Get("mod") as NbtString)?.Value;
            int time = (entry.Get("time") as NbtInt)?.Value ?? 0;
            int? buffId = mod == "Terraria"
                ? (entry.Get("id") as NbtInt)?.Value
                : mod != null && (entry.Get("name") as NbtString)?.Value is string name
                    ? buffCatalog.ByModAndInternal(mod, name)?.SyntheticId
                    : null;
            if (buffId == null) continue;

            while (bi < slots.Count && slots[bi].Id != 0) bi++;
            if (bi >= slots.Count) continue;
            slots[bi].Id = buffId.Value;
            slots[bi].Time = time;
            bi++;
        }
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

        SyncBuffs(tplrRoot, character);
        return tplrRoot;
    }

    // A diferencia de los 7 contenedores de items, los buffs NO se enmascaran antes de
    // escribir el .plr vanilla - character.Buffs puede seguir conteniendo ids sinteticos de
    // Calamity (>= CalamityIds.BuffIdBase) en el array que se escribe tal cual. Esto replica
    // fielmente el comportamiento real de la version JS (calamityMaskForVanillaSave NUNCA
    // toca player.buffs, solo los contenedores de items via CALAMITY_FLAT_SLOT_FIELDS+
    // loadouts) - no es un descuido de este port, es que un id de buff fuera de rango no
    // corrompe la carga como si lo hace un id de OBJETO fuera de rango (el escritor nativo de
    // PlrBodySerializer no aplica ningun clamp tipo "id > maxId => 0" al array de buffs, a
    // diferencia del de items), y ademas tModLoader deja de leer el array de buffs nativo del
    // .plr en cuanto un personaje tiene mods instalados (el .tplr/modBuffs pasa a ser la unica
    // fuente real), asi que el valor que quede ahi es irrelevante en la practica.
    private void SyncBuffs(NbtCompound tplrRoot, PlrCharacter character)
    {
        var newItems = new List<NbtTag>();
        foreach (var buff in character.Buffs)
        {
            if (buff.Id == 0) continue;
            if (buff.Id < CalamityIds.BuffIdBase)
            {
                newItems.Add(NbtCompound.Of(("mod", new NbtString("Terraria")), ("id", new NbtInt(buff.Id)), ("time", new NbtInt(buff.Time))));
            }
            else
            {
                var entry = buffCatalog.BySyntheticId(buff.Id);
                if (entry == null) continue; // id sintetico desconocido (no deberia pasar) - se descarta, no se inventa
                newItems.Add(NbtCompound.Of(("mod", new NbtString(entry.Mod)), ("name", new NbtString(entry.Internal)), ("time", new NbtInt(buff.Time))));
            }
        }
        tplrRoot.Set("modBuffs", new NbtList(NbtTagType.Compound, newItems));
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
