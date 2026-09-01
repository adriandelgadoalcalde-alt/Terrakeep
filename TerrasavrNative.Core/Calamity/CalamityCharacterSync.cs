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
// (modBuffs, ver MergeBuffs/SyncBuffs) MAS armadura/tinte POR LOADOUT (ver
// MergeLoadoutArmorDye/SyncLoadoutArmorDye). El unico hueco que queda: los 3 contenedores
// que la version JS solo enmascara por seguridad SIN sincronizar (coins/ammo/tempItems -
// CALAMITY_FLAT_SLOT_FIELDS los incluye pero CALAMITY_LIST_MAP no).
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
        MergeLoadoutArmorDye(character, tplrRoot, result);
        return result;
    }

    // Todos los loadouts en el mismo orden que usa la version JS (player.loadouts[0..3]):
    // indice 0 = PrimaryLoadout (mirror de lo puesto), 1..3 = Loadouts[0..2] (los 3 loadouts
    // reales). Si Version<269 (Loadouts vacio) solo existe el mirror - se degrada solo.
    private static IReadOnlyList<PlrLoadout> AllLoadouts(PlrCharacter character) =>
        character.Loadouts.Length == 0 ? [character.PrimaryLoadout] : [character.PrimaryLoadout, .. character.Loadouts];

    // Armadura/tinte de Calamity, por loadout. A diferencia de los 7 contenedores planos, cada
    // loadout tiene una vista combinada de 20 slots para armadura+vanidad (0-9 armadura/
    // accesorios, 10-19 vanidad - mismo esquema que calamityArmorSlot real) mas 10 de tinte,
    // fusionadas en `result` con las claves "loadout{i}Items"/"loadout{i}Social"/
    // "loadout{i}Dyes".
    //
    // Dos rutas de fusion, igual que calamityMergeArmorDyeIntoPlayer real: (1) las claves
    // PLANAS "armor"/"dye" del .tplr, que van al "loadout activo" - calamityActiveLoadout(player)
    // = player.loadouts[currentLoadout] SIN el +1 que si usa el guardado vanilla nativo
    // (loadouts[1+currentLoadout]) - confirmado leyendo overrides.js real, no un bug de este
    // port. Con CurrentLoadout==0 cae en el mirror (lo que la UI ya muestra como "equipo
    // puesto"); con CurrentLoadout>=1 cae en Loadouts[CurrentLoadout-1] en vez de en el mirror -
    // una discrepancia real de la app original entre lo que esta ruta fusiona y lo que el
    // juego/la UI consideran "puesto de verdad", no algo que corregir aqui. (2) las claves POR
    // LOADOUT "loadout{i}Armor"/"loadout{i}Dye" (una por cada uno de los 4), que van
    // directamente al loadout i - sin ambigüedad, se ejecuta DESPUES de la ruta plana (mismo
    // orden que el original) por si ambas rutas describen el mismo loadout.
    private void MergeLoadoutArmorDye(PlrCharacter character, NbtCompound? tplrRoot, Dictionary<string, GameItem[]> result)
    {
        var loadouts = AllLoadouts(character);
        for (int i = 0; i < loadouts.Count; i++)
        {
            result[$"loadout{i}Items"] = loadouts[i].Items.ToGameItems();
            result[$"loadout{i}Social"] = loadouts[i].Social.ToGameItems();
            result[$"loadout{i}Dyes"] = loadouts[i].Dyes.ToGameItems();
        }
        if (tplrRoot == null) return;

        int activeIdx = character.CurrentLoadout;
        if (activeIdx >= 0 && activeIdx < loadouts.Count)
            MergeArmorDyeForLoadout(tplrRoot, "armor", "dye", activeIdx, result);

        if (tplrRoot.Get("loadouts") is NbtCompound loadoutsEntries)
        {
            for (int i = 0; i < loadouts.Count; i++)
                MergeArmorDyeForLoadout(loadoutsEntries, $"loadout{i}Armor", $"loadout{i}Dye", i, result);
        }
    }

    private void MergeArmorDyeForLoadout(NbtCompound container, string armorKey, string dyeKey, int loadoutIdx, Dictionary<string, GameItem[]> result)
    {
        var combinedArmor = result[$"loadout{loadoutIdx}Items"].Concat(result[$"loadout{loadoutIdx}Social"]).ToArray();
        if (container.Get(armorKey) is NbtList armorList) MergeListInto(armorList, combinedArmor);
        for (int s = 0; s < 10; s++) result[$"loadout{loadoutIdx}Items"][s] = combinedArmor[s];
        for (int s = 0; s < 10; s++) result[$"loadout{loadoutIdx}Social"][s] = combinedArmor[10 + s];

        if (container.Get(dyeKey) is NbtList dyeList) MergeListInto(dyeList, result[$"loadout{loadoutIdx}Dyes"]);
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
        SyncLoadoutArmorDye(tplrRoot, character, merged);
        return tplrRoot;
    }

    // Contraparte de guardado de MergeLoadoutArmorDye - reescribe las claves planas "armor"/
    // "dye" (loadout activo) y "loadout{i}Armor"/"loadout{i}Dye" (los 4 loadouts) del .tplr, y
    // vuelca el resultado enmascarado a los PlrItemSlot[] reales de cada PlrLoadout (Calamity
    // -> Empty, vanilla intacto). PlrLoadout.Items/Social/Dyes son propiedades init-only (no se
    // puede reasignar el array despues de construido) - se mutan sus elementos uno a uno en vez
    // de reasignar la referencia, igual de valido y sin tocar el modelo de datos.
    //
    // Las claves "loadout{i}Items/Social/Dyes" del `merged` que entra aqui normalmente vienen
    // de MergeAll (siempre las rellena, ver MergeLoadoutArmorDye) - pero si un caller construye
    // `merged` a mano sin ellas (algunos tests unitarios de este mismo archivo, deliberadamente
    // centrados solo en los 7 contenedores planos), se cae al estado actual del propio
    // PlrLoadout en vez de lanzar una excepcion - un `merged` parcial no debe romper el resto
    // del guardado.
    private void SyncLoadoutArmorDye(NbtCompound tplrRoot, PlrCharacter character, Dictionary<string, GameItem[]> merged)
    {
        var loadouts = AllLoadouts(character);
        GameItem[] Items(int i) => merged.GetValueOrDefault($"loadout{i}Items") ?? loadouts[i].Items.ToGameItems();
        GameItem[] Social(int i) => merged.GetValueOrDefault($"loadout{i}Social") ?? loadouts[i].Social.ToGameItems();
        GameItem[] Dyes(int i) => merged.GetValueOrDefault($"loadout{i}Dyes") ?? loadouts[i].Dyes.ToGameItems();

        int activeIdx = character.CurrentLoadout;
        if (activeIdx >= 0 && activeIdx < loadouts.Count)
        {
            var combinedArmor = Items(activeIdx).Concat(Social(activeIdx)).ToArray();
            SyncListFrom(tplrRoot, "armor", combinedArmor);
            SyncListFrom(tplrRoot, "dye", Dyes(activeIdx));
        }

        var loadoutsCompound = tplrRoot.Get("loadouts") as NbtCompound ?? new NbtCompound();
        for (int i = 0; i < loadouts.Count; i++)
        {
            var combinedArmor = Items(i).Concat(Social(i)).ToArray();
            SyncListFrom(loadoutsCompound, $"loadout{i}Armor", combinedArmor);
            SyncListFrom(loadoutsCompound, $"loadout{i}Dye", Dyes(i));
        }
        tplrRoot.Set("loadouts", loadoutsCompound);

        for (int i = 0; i < loadouts.Count; i++)
        {
            CopyInto(loadouts[i].Items, Items(i).ToPlrItemSlots());
            CopyInto(loadouts[i].Social, Social(i).ToPlrItemSlots());
            CopyInto(loadouts[i].Dyes, Dyes(i).ToPlrItemSlots());
        }
    }

    private static void CopyInto(PlrItemSlot[] target, PlrItemSlot[] source)
    {
        for (int i = 0; i < target.Length && i < source.Length; i++) target[i] = source[i];
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
