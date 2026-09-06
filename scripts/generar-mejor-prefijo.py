r"""
Genera `Assets/calamity/best_prefix.json` - la tabla de "mejor prefijo" que usan el boton
"Mejor prefijo" (la estrella) y el prefijo automatico al colocar un objeto.

=====================================================================================
DE DONDE SALE EL CRITERIO (no es una tabla curada a mano ni copiada de la wiki)
=====================================================================================
Es la FORMULA REAL del juego, aplicada offline a las estadisticas reales de cada objeto.
Terraria puntua cada prefijo con un unico numero (`value`) y llama "mejor prefijo" al de
`value` maximo entre los que ese objeto concreto puede llevar. Las dos piezas reales
estan en `TerrariaVanilla\Terraria\Item.cs` (Terraria 1.4.5.8 decompilado real):

  Item.TryGetPrefixStatMultipliersForItem(prefijo, out dmg, kb, spd, size, shtspd, mcst,
                                          crt, tagdmg, arpen, out value)
      value = 1 * dmg * (2 - spd) * (2 - mcst) * size * kb * shtspd
                * (1 + crt * 0.02) * (1 + arpen * 0.015) * (1 + tagdmg * 0.03)
      * 1.05 si el prefijo es 62/69/73/77   (grupo de accesorio "flojo")
      * 1.10 si es 63/70/74/78/67
      * 1.15 si es 64/71/75/79/66
      * 1.20 si es 65/72/76/80/68          (grupo de accesorio "fuerte")
      y devuelve FALSE (prefijo NO aplicable a ESE objeto) si el cambio no se notaria:
        - dmg  != 1 y round(damage       * dmg ) == damage
        - spd  != 1 y round(useAnimation * spd ) == useAnimation
        - mcst != 1 y round(mana         * mcst) == mana
        - kb   != 1 y knockBack == 0            <- el arma no tiene retroceso que subir

  Item.BestPrefixValue()  -> max(value) sobre Item.GetRollablePrefixes()

`GetRollablePrefixes()` (mismo Item.cs) da la lista REAL de prefijos legales de cada
objeto, en este orden exacto de prioridad, contra los sets de
`GameContent\Prefixes\PrefixLegacy.cs`:
    SwordsHammersAxesPicks -> PrefixesForSwords          (mejor real: 81 Legendary)
    SpearsMacesChainsawsDrillsPunchCannon -> ForSpears   (mejor real: 59 Godly)
    GunsBows               -> PrefixesForGunsBows        (mejor real: 82 Unreal)
    Magic                  -> PrefixesForMagic           (mejor real: 83 Mythical)
    Summon                 -> PrefixesForSummons         (mejor real: 85 Fabled)
    BoomerangsChakrams     -> ForBoomeransAndChakrums    (mejor real: 59 Godly)
    ItemsThatCanHaveLegendary2 -> ..._TerrarianYoyo      (mejor real: 84 Legendary2)
    IsAPrefixableAccessory() -> PrefixesForAccessories   (empate real, ver desempate)
`IsAPrefixableAccessory()` = accessory && !vanity && ItemID.Sets.CanGetPrefixes[type].

IMPORTANTE - la fuente vanilla de ESTE fichero es `TerrariaVanilla\` (Terraria **1.4.5.8**),
no `tModLoader\` (1.4.4.9), a diferencia de casi todos los demas scripts de esta carpeta.
Es a proposito y se nota en el resultado: 1.4.5.8 separo `PrefixesForMagic` de
`PrefixesForSummons` y añadio los prefijos 85..97 (Fabled, Loyal, Worthy...), que son los
unicos que existen para armas de invocacion. La tabla que ya habia en el repo antes de
este script tambien los usaba (18 objetos vanilla con 85), asi que esa eleccion ya estaba
tomada; aqui solo queda escrita.

DESEMPATE (el juego no lo define: solo se queda con el maximo numerico, y varios prefijos
pueden dar exactamente el mismo `value`). Criterio de este generador, en orden:
  1. accesorios: si 72 (Menacing) esta entre los empatados, gana - es el criterio ya
     establecido en el proyecto (`ACCESSORY_PREFIX = 'Menacing'` en el `overrides.js` del
     proyecto hermano) y el que ya tenian las 273 entradas de accesorio previas. Los cinco
     del grupo 1.20 (65 Warding, 68 Lucky, 72 Menacing, 76 Quick, 80 Violent) empatan
     SIEMPRE: sus bonos son planos y viven en `Player.GrantPrefixBenefits`, no en la
     formula de `value`.
  2. mayor multiplicador de daño (dmg), 3. mayor critico (crt),
  4. mayor arpen+tagdmg,        5. menor spd (mas rapido),  6. id de prefijo mas bajo.
Ejemplo real verificado a mano: Minishark (id 98, knockBack 0 real) -> empatan a 1.265
Rapid (17), Hasty (18) y Demonic (60); gana Demonic por el paso 2. La wiki oficial dice
exactamente eso: "Its best modifier is Demonic, as it does not have any knockback and thus
cannot get modifiers that affect it" (terraria.wiki.gg/wiki/Minishark).

CALAMITY: las entradas ya existentes NO se recalculan (se copian tal cual del fichero
anterior). Salieron de este mismo criterio en una sesion previa, en la que el pool de cada
arma se decidio con los hooks reales de tModLoader (`ModItem.MeleePrefix()` =
`DamageType.GetsPrefixesFor(Melee) && !noUseGraphic`, `RangedPrefix`, `MagicPrefix`, y
`RogueWeapon.WeaponPrefix()` de Calamity, que manda las armas Picaro al pool generico
tipo lanza). Este script solo AÑADE las que faltaban, resolviendo su clase real en el
codigo decompilado de Calamity (incluido `Item.CloneDefaults(<id vanilla>)`, que copia
entero un objeto vanilla, y las alas, que derivan de `BaseWings`). Las armas de tipo de
daño hibrido/sin clase (AverageDamageClass, AllClassDamageClass, MeleeRangedHybrid,
Typeless) se dejan fuera a proposito, como ya estaban: no tienen un pool real univoco y
adivinarlo seria inventar.
"""
import io
import json
import os
import re

VANILLA = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\TerrariaVanilla\Terraria"
ITEM_SRC = os.path.join(VANILLA, "Item.cs")
LEGACY_SRC = os.path.join(VANILLA, "GameContent", "Prefixes", "PrefixLegacy.cs")
ITEMID_SRC = os.path.join(VANILLA, "ID", "PrefixID.cs")
ITEMIDS_SRC = os.path.join(VANILLA, "ID", "ItemID.cs")
CALAMITY_SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\CalamityMod\CalamityMod\Items"
ASSETS = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets"
CATALOG = os.path.join(ASSETS, "calamity", "catalog.json")
OUT = os.path.join(ASSETS, "calamity", "best_prefix.json")

BACKSLASH, DQUOTE, SQUOTE = chr(92), chr(34), chr(39)


# ---------------------------------------------------------------- utilidades de parseo
def find_matching_brace(t, i):
    d = 0
    while True:
        if t[i] == "{":
            d += 1
        elif t[i] == "}":
            d -= 1
            if d == 0:
                return i
        i += 1


SWITCH_TYPE_RE = re.compile(r"switch\s*\(\s*type\s*\)\s*\{")
CASE_RE = re.compile(r"case\s+(\d+)\s*:")
DEFAULT_RE = re.compile(r"default\s*:")


def find_case_blocks(body, outer=""):
    """`outer` = texto del bloque PADRE que precede a este switch anidado. Sus asignaciones
    valen tambien para los ids del switch de dentro: es un patron real de Item.cs (los yoyos
    3278..3292 ponen `useAnimation = 25;` en el bloque comun y solo damage/knockBack en el
    switch(type) anidado). Sin heredarlo, 3315..3317 salen sin useAnimation."""
    out, i, n = [], 0, len(body)
    while i < n:
        m = SWITCH_TYPE_RE.search(body, i)
        if not m:
            break
        o = m.end() - 1
        c = find_matching_brace(body, o)
        out.extend(extract_immediate_cases(body, o + 1, c, outer + body[i:m.start()]))
        i = c + 1
    return out


def extract_immediate_cases(body, start, end, outer=""):
    """Etiquetas case/default del nivel inmediato de UN switch, agrupando las consecutivas
    sin codigo entre medias (fall-through real), y bajando de forma recursiva a cualquier
    switch(type){...} anidado. Mismo escaner ya verificado en extraer-sets-armadura.py /
    extraer-estadisticas-vanilla.py."""
    labels = []
    i, depth = start, 0
    while i < end:
        ch = body[i]
        if ch == "/" and i + 1 < end and body[i + 1] == "/":
            j = body.find("\n", i, end)
            i = (j + 1) if j != -1 else end
            continue
        if ch == "/" and i + 1 < end and body[i + 1] == "*":
            j = body.find("*/", i + 2, end)
            i = (j + 2) if j != -1 else end
            continue
        if ch in (DQUOTE, SQUOTE):
            q = ch
            i += 1
            while i < end and body[i] != q:
                i += 2 if body[i] == BACKSLASH else 1
            i += 1
            continue
        if ch == "{":
            depth += 1
            i += 1
            continue
        if ch == "}":
            depth -= 1
            i += 1
            continue
        if depth == 0:
            m2 = CASE_RE.match(body, i)
            if m2:
                labels.append((int(m2.group(1)), m2.end()))
                i = m2.end()
                continue
            m3 = DEFAULT_RE.match(body, i)
            if m3:
                labels.append((None, m3.end()))
                i = m3.end()
                continue
        i += 1

    def label_text(idx):
        iid, _ = labels[idx]
        return f"case {iid}" if iid is not None else "default"

    res, k = [], 0
    while k < len(labels):
        ids, j = [], k
        while True:
            iid, lend = labels[j]
            if iid is not None:
                ids.append(iid)
            last = j + 1 >= len(labels)
            nxt = None if last else body.rfind(label_text(j + 1), lend, labels[j + 1][1])
            gap = "" if last else body[lend:nxt]
            if not last and gap.strip() == "":
                j += 1
                continue
            block = body[lend:(end if last else nxt)]
            break
        for iid in ids:
            res.append((iid, outer + block))
        res.extend(find_case_blocks(block, outer))
        k = j + 1
    return res


# ------------------------------------------------- 1. multiplicadores reales por prefijo
def load_prefix_multipliers(item_text):
    start = item_text.index("public bool TryGetPrefixStatMultipliersForItem")
    body = item_text[start:item_text.index("value = 1f * dmg", start)]
    field_re = re.compile(r"\b(dmg|kb|spd|size|shtspd|mcst|crt|tagdmg|arpen)\s*=\s*(-?\d+(?:\.\d+)?)f?\s*;")
    mult = {}
    for m in re.finditer(r"case\s+(\d+)\s*:(.*?)break;", body, re.S):
        entry = {}
        for fm in field_re.finditer(m.group(2)):
            entry[fm.group(1)] = float(fm.group(2))
        if entry:
            mult[int(m.group(1))] = entry
    return mult


def prefix_value(mult, pid, damage, use_animation, mana, knock_back):
    """Replica literal de Item.TryGetPrefixStatMultipliersForItem: devuelve el `value` real,
    o None si ese prefijo NO es aplicable a ese objeto (la funcion real devuelve false)."""
    e = mult.get(pid, {})
    dmg = e.get("dmg", 1.0)
    kb = e.get("kb", 1.0)
    spd = e.get("spd", 1.0)
    size = e.get("size", 1.0)
    shtspd = e.get("shtspd", 1.0)
    mcst = e.get("mcst", 1.0)
    crt = e.get("crt", 0.0)
    tagdmg = e.get("tagdmg", 0.0)
    arpen = e.get("arpen", 0.0)
    value = (1.0 * dmg * (2.0 - spd) * (2.0 - mcst) * size * kb * shtspd
             * (1.0 + crt * 0.02) * (1.0 + arpen * 0.015) * (1.0 + tagdmg * 0.03))
    if pid in (62, 69, 73, 77):
        value *= 1.05
    if pid in (63, 70, 74, 78, 67):
        value *= 1.1
    if pid in (64, 71, 75, 79, 66):
        value *= 1.15
    if pid in (65, 72, 76, 80, 68):
        value *= 1.2
    if dmg != 1.0 and round(damage * dmg) == damage:
        return None
    if spd != 1.0 and round(use_animation * spd) == use_animation:
        return None
    if mcst != 1.0 and round(mana * mcst) == mana:
        return None
    if kb != 1.0 and knock_back == 0:
        return None
    return value


def pick_best(mult, pool_key, pool, damage, use_animation, mana, knock_back):
    """Mejor prefijo real del pool + desempate documentado en la cabecera."""
    scored = []
    for pid in pool:
        v = prefix_value(mult, pid, damage, use_animation, mana, knock_back)
        if v is not None:
            scored.append((v, pid))
    if not scored:
        return None
    top = max(v for v, _ in scored)
    tied = [pid for v, pid in scored if abs(v - top) <= 1e-9]
    if len(tied) == 1:
        return tied[0]
    if pool_key == "accessories" and 72 in tied:
        return 72
    def key(pid):
        e = mult.get(pid, {})
        return (e.get("dmg", 1.0), e.get("crt", 0.0),
                e.get("arpen", 0.0) + e.get("tagdmg", 0.0), -e.get("spd", 1.0), -pid)
    return max(tied, key=key)


# --------------------------------------------------- 2. estadisticas reales por id vanilla
NUM = r"(-?\d+(?:\.\d+)?)f?"
SIMPLE = {
    "damage": rf"\bdamage\s*=\s*{NUM}\s*;",
    "mana": rf"\bmana\s*=\s*{NUM}\s*;",
    "knockBack": rf"\bknockBack\s*=\s*{NUM}\s*;",
    "useAnimation": rf"\buseAnimation\s*=\s*{NUM}\s*;",
}
CHAIN_RE = re.compile(rf"\buseAnimation\s*=\s*\(\s*useTime\s*=\s*{NUM}\s*\)\s*;")
CHAIN2_RE = re.compile(rf"\buseTime\s*=\s*\(\s*useAnimation\s*=\s*{NUM}\s*\)\s*;")
ACCESSORY_RE = re.compile(r"\baccessory\s*=\s*(true|false)\s*;")
VANITY_RE = re.compile(r"\bvanity\s*=\s*(true|false)\s*;")
VARIANT_IF_RE = re.compile(r"if\s*\(\s*Variant\s*==")
REDIRECT_RE = re.compile(r"SetDefaults\d?\s*\(\s*(\d+)\s*[,)]")

# Helpers reales de Item.cs 1.4.5.8 que fijan alguno de los campos que importan aqui.
# El valor es la POSICION del argumento (0-based); (pos, defecto) si el argumento es opcional.
# `SetWeaponValues` es el que faltaba en la extraccion anterior y explica por si solo la mayor
# parte del hueco de cobertura: cientos de armas vanilla ponen su daño y retroceso SOLO por ahi.
HELPERS = {
    "DefaultToWhip": {"damage": 1, "knockBack": 2, "useAnimation": (4, 30)},
    "DefaultToStaff": {"useAnimation": 2, "mana": 3},
    "DefaultToSpear": {"useAnimation": 2},
    "DefaultToBow": {"useAnimation": 0},
    "DefaultToMagicWeapon": {"useAnimation": 1},
    "DefaultToRangedWeapon": {"useAnimation": 2},
    "DefaultToThrownWeapon": {"useAnimation": 1},
    "DefaultToAccessory": {"accessory": True},
    "DefaultToInfoAccessory": {"accessory": True},
    "DefaultToVoiceOverrideAccessory": {"accessory": True},
    "SetWeaponValues": {"damage": 0, "knockBack": 1},
}
HELPER_CALL_RE = re.compile(r"\b(DefaultTo[A-Za-z]+|SetWeaponValues)\s*\(([^;]*)\)\s*;")


def split_args(s):
    out, depth, cur = [], 0, ""
    for ch in s:
        if ch in "([<":
            depth += 1
        elif ch in ")]>":
            depth -= 1
        if ch == "," and depth == 0:
            out.append(cur.strip())
            cur = ""
        else:
            cur += ch
    if cur.strip():
        out.append(cur.strip())
    return out


def numeric(tok):
    m = re.fullmatch(r"(-?\d+(?:\.\d+)?)f?", tok.strip())
    return float(m.group(1)) if m else None


def strip_variant_blocks(block):
    """Quita el cuerpo de `if (Variant == ItemVariants.X) { ... }` (y su else): son las stats
    de una VARIANTE opcional, no las base. Caso real: id 5147, cuyo `damage = 42;` de la
    StrongerVariant pisaba el `damage = 15;` de verdad."""
    out, i = "", 0
    while True:
        m = VARIANT_IF_RE.search(block, i)
        if not m:
            return out + block[i:]
        out += block[i:m.start()]
        o = block.find("{", m.end())
        if o == -1:
            return out + block[m.start():]
        i = find_matching_brace(block, o) + 1
        if block[i:].lstrip().startswith("else"):
            j = block.index("else", i)
            o2 = block.find("{", j)
            i = find_matching_brace(block, o2) + 1 if o2 != -1 else j + 4


def extract_vanilla_stats(item_text):
    redirects = {}
    stats = {}
    starts = [m.start() for m in re.finditer(r"private void (SetDefaults\d|SetFoodDefaults)\(int type\)", item_text)]
    for pos in starts:
        o = item_text.index("{", pos)
        body = item_text[pos:find_matching_brace(item_text, o) + 1]
        for iid, raw_block in find_case_blocks(body):
            if iid <= 0:
                continue
            block = strip_variant_blocks(raw_block)
            entry = stats.setdefault(iid, {})
            # En orden TEXTUAL: es habitual que un helper ponga un valor y una linea posterior
            # lo pise (o al reves) - `case 4058: DefaultToBow(17, 11f); SetWeaponValues(8, 5f);`.
            events = []
            for hm in HELPER_CALL_RE.finditer(block):
                spec = HELPERS.get(hm.group(1))
                if spec:
                    events.append((hm.start(), "helper", spec, split_args(hm.group(2))))
            for field, pat in SIMPLE.items():
                for sm in re.finditer(pat, block):
                    events.append((sm.start(), "set", field, float(sm.group(1))))
            for rx in (CHAIN_RE, CHAIN2_RE):
                for sm in rx.finditer(block):
                    events.append((sm.start(), "set", "useAnimation", float(sm.group(1))))
            for rx, field in ((ACCESSORY_RE, "accessory"), (VANITY_RE, "vanity")):
                for sm in rx.finditer(block):
                    events.append((sm.start(), "set", field, sm.group(1) == "true"))
            events.sort(key=lambda x: x[0])
            for ev in events:
                if ev[1] == "set":
                    entry[ev[2]] = ev[3]
                    continue
                spec, args = ev[2], ev[3]
                for field, idx in spec.items():
                    if idx is True:
                        entry[field] = True
                        continue
                    default = None
                    if isinstance(idx, tuple):
                        idx, default = idx
                    if idx < len(args):
                        v = numeric(args[idx])
                        if v is not None:
                            entry[field] = v
                    elif default is not None:
                        entry[field] = float(default)
            # Redireccion real: `SetDefaults3(2772); type = 3462;` - el objeto COPIA entero el
            # bloque de otro id y solo cambia lo cosmetico. Sin resolverlo, 2777..2786 y
            # 3462..3466 (variantes de armas reales) salen sin ninguna estadistica.
            rm = REDIRECT_RE.search(block)
            if rm and int(rm.group(1)) != iid:
                redirects[iid] = int(rm.group(1))
            if not entry:
                stats.pop(iid, None)
    for _ in range(4):  # cadenas de redireccion (A copia B, B copia C)
        for iid, src in redirects.items():
            base = stats.get(src)
            if base:
                merged = dict(base)
                merged.update(stats.get(iid, {}))
                stats[iid] = merged
    return stats


# --------------------------------------------------------- 3. pools y sets reales (1.4.5.8)
def load_pools_and_sets():
    legacy = io.open(LEGACY_SRC, encoding="utf-8").read()

    def arr(name):
        m = re.search(rf"public static int\[\] {name} = new int\[\d+\]\s*\{{(.*?)\}};", legacy, re.S)
        if not m:
            raise ValueError(f"no encontrado: {name}")
        return [int(x) for x in re.findall(r"-?\d+", m.group(1))]

    def bset(name):
        m = re.search(rf"public static bool\[\] {name} = Factory\.CreateBoolSet\((.*?)\);", legacy, re.S)
        if not m:
            raise ValueError(f"no encontrado: {name}")
        return set(int(x) for x in re.findall(r"-?\d+", m.group(1)))

    pools = {
        "swords": arr("PrefixesForSwords"),
        "spears": arr("PrefixesForSpears"),
        "gunsBows": arr("PrefixesForGunsBows"),
        "magic": arr("PrefixesForMagic"),
        "summons": arr("PrefixesForSummons"),
        "boomerangsChakrams": arr("PrefixesForBoomeransAndChakrums"),
        "terrarianYoyo": arr("PrefixesForBoomeransAndChakrums_TerrarianYoyo"),
        "accessories": arr("PrefixesForAccessories"),
    }
    sets = {
        "swords": bset("SwordsHammersAxesPicks"),
        "spears": bset("SpearsMacesChainsawsDrillsPunchCannon"),
        "gunsBows": bset("GunsBows"),
        "magic": bset("Magic"),
        "summons": bset("Summon"),
        "boomerangsChakrams": bset("BoomerangsChakrams"),
        "terrarianYoyo": bset("ItemsThatCanHaveLegendary2"),
    }
    itemid = io.open(ITEMIDS_SRC, encoding="utf-8").read()
    m = re.search(r"CanGetPrefixes = Factory\.CreateBoolSet\((.*?)\);", itemid, re.S)
    raw = m.group(1)
    assert raw.strip().startswith("true,"), "se esperaba que CanGetPrefixes sea lista NEGRA"
    cannot = set(int(x) for x in re.findall(r"-?\d+", raw))
    return pools, sets, cannot


# Orden REAL de prioridad de Item.GetRollablePrefixes(), replicado 1:1.
POOL_ORDER = ["swords", "spears", "gunsBows", "magic", "summons", "boomerangsChakrams", "terrarianYoyo"]


def build_vanilla(mult, stats, pools, sets, cannot):
    out = {}
    candidates = set()
    for s in sets.values():
        candidates |= s
    candidates |= {i for i, e in stats.items() if e.get("accessory")}
    skipped_vanity = skipped_blacklist = 0
    for iid in sorted(candidates):
        if iid <= 0:
            continue
        pool_key = next((k for k in POOL_ORDER if iid in sets[k]), None)
        if pool_key is None:
            e = stats.get(iid, {})
            if not e.get("accessory") or e.get("vanity"):
                if e.get("accessory") and e.get("vanity"):
                    skipped_vanity += 1
                continue
            if iid in cannot:
                skipped_blacklist += 1
                continue
            pool_key = "accessories"
        e = stats.get(iid, {})
        best = pick_best(mult, pool_key, pools[pool_key],
                         e.get("damage", 0.0), e.get("useAnimation", 0.0),
                         e.get("mana", 0.0), e.get("knockBack", 0.0))
        if best is not None:
            out[iid] = best
    print(f"  vanilla: {len(out)} objetos con mejor prefijo real "
          f"({skipped_vanity} accesorios de vanidad y {skipped_blacklist} de la lista negra "
          f"CanGetPrefixes descartados: en el juego real no admiten prefijo)")
    return out


# ------------------------------------------------------- 4. Calamity: solo lo que falta
CAL_DAMAGE_TYPE_RE = re.compile(r"DamageType\s*=\s*([A-Za-z.]+)\s*;")
CAL_CLONE_RE = re.compile(r"CloneDefaults\s*\(\s*(\d+)\s*\)")
CAL_BASE_RE = re.compile(r"class\s+\w+\s*:\s*([A-Za-z_]\w*)")
CAL_FIELD_RE = {
    "damage": re.compile(rf"\bdamage\s*=\s*{NUM}\s*;"),
    "knockBack": re.compile(rf"\bknockBack\s*=\s*{NUM}\s*;"),
    "mana": re.compile(rf"\bmana\s*=\s*{NUM}\s*;"),
    "useAnimation": re.compile(rf"\buseAnimation\s*=\s*{NUM}\s*;"),
}
CAL_CHAIN_RE = re.compile(rf"\buseAnimation\s*=\s*\(\s*base\.Item\.useTime\s*=\s*{NUM}\s*\)\s*;")
CAL_NOUSEGRAPHIC_RE = re.compile(r"noUseGraphic\s*=\s*true\s*;")


def index_calamity_sources():
    by_name = {}
    for root, _, files in os.walk(CALAMITY_SRC):
        for f in files:
            if f.endswith(".cs"):
                by_name.setdefault(f[:-3], os.path.join(root, f))
    return by_name


def calamity_setdefaults_body(path):
    """Solo el cuerpo de `SetDefaults()`: `noUseGraphic = true` tambien aparece en HoldItem /
    Shoot de varias armas (BrimstoneSword, BrinyBaron, DeathsAscension) y ahi NO decide el
    pool de prefijos - leer el fichero entero mandaria esas armas al pool equivocado."""
    src = io.open(path, encoding="utf-8", errors="replace").read()
    m = re.search(r"public override void SetDefaults\(\)", src)
    if not m:
        return "", src
    o = src.index("{", m.end())
    return src[o:find_matching_brace(src, o) + 1], src


def build_calamity_missing(mult, pools, sets, vanilla_stats, catalog, existing):
    """Aplica el MISMO criterio a los objetos de Calamity que aun no tenian entrada."""
    sources = index_calamity_sources()
    added, unresolved = {}, []
    for item in catalog:
        name = item["internal"]
        if name in existing:
            continue
        category = item.get("category") or ""
        if not (category.startswith("Weapons") or category.startswith("Accessories")):
            continue
        if "/Vanity" in category:
            continue
        stats = item.get("stats") or {}
        damage_type = stats.get("damageType") or ""
        path = sources.get(name)
        body, whole = calamity_setdefaults_body(path) if path else ("", "")

        pool_key = None
        if category.startswith("Accessories"):
            pool_key = "accessories"
        else:
            clone = CAL_CLONE_RE.search(body)
            if not damage_type and clone:
                # `Item.CloneDefaults(921)` copia entero un objeto vanilla: el pool es el
                # de ESE id vanilla, tal cual lo decide GetRollablePrefixes().
                cloned = int(clone.group(1))
                pool_key = next((k for k in POOL_ORDER if cloned in sets[k]), None)
                base = vanilla_stats.get(cloned, {})
                stats = dict(stats)
                stats.setdefault("knockBack", base.get("knockBack"))
            elif "Rogue" in damage_type:
                pool_key = "spears"          # RogueWeapon.WeaponPrefix() -> pool generico
            elif "Summon" in damage_type:
                pool_key = "summons"
            elif "Magic" in damage_type:
                pool_key = "magic"
            elif "Ranged" in damage_type and "Hybrid" not in damage_type:
                pool_key = "gunsBows"
            elif "Melee" in damage_type and "Hybrid" not in damage_type:
                # ModItem.MeleePrefix() real: DamageType.GetsPrefixesFor(Melee) && !noUseGraphic
                pool_key = "spears" if CAL_NOUSEGRAPHIC_RE.search(body) else "swords"
        if pool_key is None:
            unresolved.append((name, category, damage_type or "sin damageType"))
            continue

        damage = stats.get("damage") or 0
        knock_back = stats.get("knockBack") or 0
        mana = stats.get("mana") or 0
        use_animation = 0
        if body:
            chain = CAL_CHAIN_RE.findall(body)
            if chain:
                use_animation = float(chain[-1])
            else:
                plain = CAL_FIELD_RE["useAnimation"].findall(body)
                if plain:
                    use_animation = float(plain[-1])
        if not use_animation:
            use_animation = stats.get("useTime") or 0
        if pool_key == "accessories":
            damage = use_animation = mana = knock_back = 0
        best = pick_best(mult, pool_key, pools[pool_key], damage, use_animation, mana, knock_back)
        if best is not None:
            added[name] = best
    print(f"  calamity: {len(existing)} entradas conservadas + {len(added)} nuevas")
    if unresolved:
        print(f"  calamity: {len(unresolved)} sin pool univoco, se dejan fuera a proposito:")
        for name, cat, dt in unresolved:
            print(f"    - {name} ({cat}, {dt})")
    return added


# --------------------------------------------------------------------------------- main
def main():
    item_text = io.open(ITEM_SRC, encoding="utf-8").read()
    mult = load_prefix_multipliers(item_text)
    print(f"{len(mult)} prefijos con multiplicadores reales (Item.TryGetPrefixStatMultipliersForItem)")
    stats = extract_vanilla_stats(item_text)
    print(f"{len(stats)} objetos vanilla con alguna estadistica real (SetDefaults1..5)")
    pools, sets, cannot = load_pools_and_sets()

    previous = json.load(io.open(OUT, encoding="utf-8"))
    vanilla = build_vanilla(mult, stats, pools, sets, cannot)
    catalog = json.load(io.open(CATALOG, encoding="utf-8"))
    calamity = dict(previous["calamity"])
    calamity.update(build_calamity_missing(mult, pools, sets, stats, catalog, previous["calamity"]))

    out = {
        "prefixNames": previous["prefixNames"],
        "vanilla": {str(k): v for k, v in sorted(vanilla.items())},
        "calamity": {k: calamity[k] for k in sorted(calamity)},
    }
    with io.open(OUT, "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, separators=(",", ":"))
    print(f"escrito {OUT}")
    print(f"  antes: {len(previous['vanilla'])} vanilla + {len(previous['calamity'])} Calamity")
    print(f"  ahora: {len(out['vanilla'])} vanilla + {len(out['calamity'])} Calamity")


if __name__ == "__main__":
    main()
