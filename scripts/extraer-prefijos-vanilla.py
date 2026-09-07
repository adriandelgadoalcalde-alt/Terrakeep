"""
Extrae la elegibilidad REAL de prefijo por objeto vanilla, directamente del codigo
decompilado de tModLoader (no inventado, no adivinado desde fuera) - pedido explicito
1-sep-2026 tras el bug reportado "me esta permitiendo poner prefijo a objetos que no
deberia".

Fuentes reales:
- Terraria/GameContent/Prefixes/PrefixLegacy.cs: los 7 arrays "PrefixesFor*" (el POOL de
  ids de prefijo real por tipo de objeto) y los 6 bool-set "ItemSets.*" (que objetos
  pertenecen a cada tipo).
- Terraria/ID/ItemID.cs, ItemID.Sets.CanGetPrefixes: lista NEGRA (el primer argumento de
  CreateBoolSet es el valor por defecto = true, los ids listados son la excepcion = false).
- Item.cs, Item.GetRollablePrefixes() (linea ~1844): la logica REAL de que pool de
  prefijos usa cada objeto, replicada aqui 1:1 en el mismo orden de prioridad (Swords >
  Spears > GunsBows > MagicAndSummon > BoomerangsChakrams > TerrarianYoyo > Accessory).
  Item.cs, Item.GetPrefixCategories() (linea ~52221): las categorias reales para el panel
  agrupado tipo Terrasavr real (Melee/Ranged/Magic/AnyWeapon/Accessory - Terraria vanilla
  NO tiene categoria "Summon" propia, los objetos de invocacion vanilla caen dentro de
  MagicAndSummon -> Magic; el "Invocacion" que se ve en Terrasavr/Calamity es cosa del
  propio Calamity Mod, no de esta tabla).
- Item.cs, Item.IsAPrefixableAccessory()/CanHavePrefixes() (linea ~1300/~1935): accesorio
  elegible = accessory=true && vanity=false && no esta en la lista negra CanGetPrefixes.
  Para accessory/vanity se REUTILIZA vanilla_categories.json (ya extraido en esta misma
  sesion con el mismo metodo de bloques SetDefaults#) en vez de volver a escanear
  Item.cs - las categorias "Accesorios"/"Accesorios/Vanidad" ya codifican exactamente esa
  distincion.

Simplificacion documentada: CanHavePrefixes() real tambien exige maxStack==1 (salvo el
flag por-objeto AllowReforgeForStackableItem, un hook de modding disperso que no es
extraible por regex de forma fiable) y ammo==0 - en la practica todo objeto que pertenece a
alguno de los sets de armas/accesorios de PrefixLegacy ya es maxStack=1 de por si (no se
puede apilar una espada), asi que esta comprobacion extra no cambia el resultado para
ningun objeto vanilla real - no se replica aparte.
"""
import json
import re
from collections import OrderedDict

PREFIX_LEGACY = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\GameContent\Prefixes\PrefixLegacy.cs"
ITEM_ID = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\ID\ItemID.cs"
CATEGORIES = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets\vanilla_categories.json"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets\vanilla_prefix_rules.json"


def parse_int_array(text: str, name: str) -> list[int]:
    m = re.search(rf"public static int\[\] {name} = new int\[\d+\]\s*\{{(.*?)\}};", text, re.S)
    if not m:
        raise ValueError(f"no encontrado: {name}")
    return [int(x) for x in re.findall(r"-?\d+", m.group(1))]


def parse_bool_set(text: str, name: str) -> set[int]:
    m = re.search(rf"public static bool\[\] {name} = Factory\.CreateBoolSet\((.*?)\);", text, re.S)
    if not m:
        raise ValueError(f"no encontrado: {name}")
    ids = [int(x) for x in re.findall(r"-?\d+", m.group(1))]
    return set(ids)


with open(PREFIX_LEGACY, encoding="utf-8") as f:
    legacy = f.read()

pools = {
    "swords": parse_int_array(legacy, "PrefixesForSwords"),
    "spears": parse_int_array(legacy, "PrefixesForSpears"),
    "gunsBows": parse_int_array(legacy, "PrefixesForGunsBows"),
    "magicAndSummons": parse_int_array(legacy, "PrefixesForMagicAndSummons"),
    "boomerangsChakrams": parse_int_array(legacy, "PrefixesForBoomeransAndChakrums"),
    "terrarianYoyo": parse_int_array(legacy, "PrefixesForBoomeransAndChakrums_TerrarianYoyo"),
    "accessories": parse_int_array(legacy, "PrefixesForAccessories"),
}

sets = {
    "swordsHammersAxesPicks": parse_bool_set(legacy, "SwordsHammersAxesPicks"),
    "spearsMacesChainsawsDrillsPunchCannon": parse_bool_set(legacy, "SpearsMacesChainsawsDrillsPunchCannon"),
    "gunsBows": parse_bool_set(legacy, "GunsBows"),
    "magicAndSummon": parse_bool_set(legacy, "MagicAndSummon"),
    "boomerangsChakrams": parse_bool_set(legacy, "BoomerangsChakrams"),
    "itemsThatCanHaveLegendary2": parse_bool_set(legacy, "ItemsThatCanHaveLegendary2"),
}

with open(ITEM_ID, encoding="utf-8") as f:
    item_id_text = f.read()

m = re.search(r"CanGetPrefixes = Factory\.CreateBoolSet\((.*?)\);", item_id_text, re.S)
if not m:
    raise ValueError("no encontrado: CanGetPrefixes")
cangetprefixes_args = [int(x) for x in re.findall(r"-?\d+", m.group(1))]
cangetprefixes_default = bool(cangetprefixes_args[0])  # True: la lista es negra (excepciones)
cangetprefixes_blacklist = set(cangetprefixes_args[1:])
assert cangetprefixes_default is True, "se esperaba que CanGetPrefixes sea una lista NEGRA (default true)"

with open(CATEGORIES, encoding="utf-8") as f:
    vanilla_categories: dict[str, str] = json.load(f)

all_item_ids = sorted(set(vanilla_categories.keys()), key=int)


def can_get_prefixes(item_id: int) -> bool:
    return item_id not in cangetprefixes_blacklist


def is_prefixable_accessory(item_id: int, category: str) -> bool:
    is_accessory = category.startswith("Accesorios") and "Vanidad" not in category
    return is_accessory and can_get_prefixes(item_id)


def get_rollable_pool(item_id: int, category: str) -> tuple[str, list[int]] | None:
    """Replica Item.GetRollablePrefixes() 1:1, mismo orden de prioridad real. El primer
    elemento es la CLAVE REAL de "pools" (no una etiqueta de categoria) - asi itemPool en el
    JSON de salida se puede usar directamente como indice a prefixesByCategory en C#."""
    if item_id in sets["swordsHammersAxesPicks"]:
        return "swords", pools["swords"]
    if item_id in sets["spearsMacesChainsawsDrillsPunchCannon"]:
        return "spears", pools["spears"]
    if item_id in sets["gunsBows"]:
        return "gunsBows", pools["gunsBows"]
    if item_id in sets["magicAndSummon"]:
        return "magicAndSummons", pools["magicAndSummons"]
    if item_id in sets["boomerangsChakrams"]:
        return "boomerangsChakrams", pools["boomerangsChakrams"]
    if item_id in sets["itemsThatCanHaveLegendary2"]:
        return "terrarianYoyo", pools["terrarianYoyo"]
    if is_prefixable_accessory(item_id, category):
        return "accessories", pools["accessories"]
    return None


def get_categories(item_id: int) -> list[str]:
    """Replica Item.GetPrefixCategories() - puede llevar varias etiquetas a la vez
    (p.ej. Melee + AnyWeapon), a diferencia de get_rollable_pool que es un unico pool."""
    cats = []
    if item_id in sets["swordsHammersAxesPicks"]:
        cats.append("melee")
    if item_id in sets["gunsBows"]:
        cats.append("ranged")
    if item_id in sets["magicAndSummon"]:
        cats.append("magic")
    if (item_id in sets["spearsMacesChainsawsDrillsPunchCannon"]
            or item_id in sets["boomerangsChakrams"]
            or item_id in sets["itemsThatCanHaveLegendary2"]
            or len(cats) != 0):
        cats.append("anyWeapon")
    category = vanilla_categories.get(str(item_id), "")
    if is_prefixable_accessory(item_id, category):
        cats.append("accessory")
    return cats


item_categories: "OrderedDict[str, list[str]]" = OrderedDict()
item_pool: "OrderedDict[str, str]" = OrderedDict()
pool_usage_counts: dict[str, int] = {}

for id_str in all_item_ids:
    item_id = int(id_str)
    if item_id <= 0:
        continue
    category = vanilla_categories[id_str]
    cats = get_categories(item_id)
    if not cats:
        continue
    item_categories[id_str] = cats
    pool_key, _ = get_rollable_pool(item_id, category) or (None, None)
    if pool_key:
        item_pool[id_str] = pool_key
        pool_usage_counts[pool_key] = pool_usage_counts.get(pool_key, 0) + 1

out = {
    "_fuente": (
        "PrefixLegacy.cs (PrefixesFor*/ItemSets.*) + ItemID.cs (Sets.CanGetPrefixes) + "
        "Item.cs (GetRollablePrefixes/GetPrefixCategories/IsAPrefixableAccessory), "
        "tModLoader-Decompiled real. accessory/vanity reutiliza vanilla_categories.json "
        "(mismo metodo SetDefaults# ya usado en este proyecto)."
    ),
    "prefixesByCategory": {k: sorted(v) for k, v in pools.items()},
    # id de objeto -> lista de categorias reales (para agrupar el picker por tipo, puede
    # llevar varias a la vez, ej. Espada Terra: ["melee","anyWeapon"]).
    "itemCategories": item_categories,
    # id de objeto -> UNA sola clave de prefixesByCategory: la lista REAL y completa de
    # prefijos legales para ESE objeto exacto (replica literal de la prioridad real de
    # Item.GetRollablePrefixes - a diferencia de itemCategories, que es solo para agrupar
    # visualmente, esto es lo que decide si un prefijo concreto es legal o no).
    "itemPool": item_pool,
}

print(f"{len(item_categories)} objetos con al menos una categoria de prefijo")
print("uso de pool (GetRollablePrefixes):", pool_usage_counts)

with open(OUT, "w", encoding="utf-8") as f:
    json.dump(out, f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
