r"""
Genera las DOS tablas de "mejor prefijo" del proyecto:

  `Assets/calamity/best_prefix.json`      -> app de escritorio, contra Terraria **1.4.5.8**
  `Assets/calamity/best_prefix_tml.json`  -> TerrakeepMod,      contra tModLoader **1.4.4.9**
                                             (se copia tal cual a TerrakeepMod/Assets/best_prefix.json)

Las usan el boton "Mejor prefijo" (la estrella), el prefijo automatico al colocar un objeto y,
en el mod, la linea "Mejor prefijo posible: X" del tooltip real del juego.

POR QUE DOS TABLAS Y NO UNA (hallazgo del 8-sep-2026, a raiz de que NINGUN baculo de invocacion
enseñaba su etiqueta en el mod): el mod corre sobre tModLoader 1.4.4.9, que a estos efectos es
otro juego distinto de Terraria 1.4.5.8, en tres cosas que cambian el resultado de verdad:

  1. Los prefijos de INVOCACION (85 Fabled..97 Scraggling) no existen en 1.4.4.9: alli
     `PrefixID.Count` es 85 y hay un unico `PrefixesForMagicAndSummons` (tope 83 Mythical).
     La wiki oficial lo confirma en la historia de `Modifiers`: "Added 13 new modifiers:
     Ballistic, Eager, Fabled, ... All of them are exclusively obtainable by summon weapons,
     **which previously shared modifiers with magic weapons**" (Desktop 1.4.5.0).
  2. Varias decenas de objetos tienen ESTADISTICAS distintas entre las dos versiones, y eso
     cambia que prefijos son aplicables. El caso gordo son los propios baculos de invocacion:
     en 1.4.4.9 gastan mana y en 1.4.5.8 no ("Desktop 1.4.5.0: Removed mana cost (cost 10 mana
     previously)", historial del Baculo optico en la wiki), asi que en 1.4.4.9 los prefijos que
     tocan el mana SI son aplicables y Mythical gana; en 1.4.5.8 quedarian descartados.
  3. Algun objeto cambia de POOL: `Gladius` (4463) esta en `SwordsHammersAxesPicks` en 1.4.4.9
     (mejor real 81 Legendary) y en `SpearsMacesChainsawsDrillsPunchCannon` en 1.4.5.8 (59
     Godly) - "1.4.5.0: Can now only have spear-type modifiers", wiki oficial.

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

En 1.4.4.9 no existe `GetRollablePrefixes()`: el equivalente real es
`Item.GetPrefixCategories()` + `Item.GetVanillaPrefixes(PrefixCategory)`, con los mismos sets
salvo que `Magic` y `Summon` son un unico `MagicAndSummon` -> `PrefixesForMagicAndSummons`
(mejor real: 83 Mythical, no 85 Fabled). Ahi tambien caen las armas de invocacion de MOD:
`SummonDamageClass.GetPrefixInheritance(dc) => dc == DamageClass.Magic`, o sea
`ModItem.MagicPrefix()` es true para ellas.

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
adivinarlo seria inventar. Al generar la tabla de 1.4.4.9 si se rehacen las entradas cuyo
prefijo no existe en ese arbol (las 103 de invocacion), con el pool real de alli.

DOS VIAS REALES por las que Item.cs define las estadisticas de un objeto - las dos hacen falta,
y saltarse cualquiera de ellas deja huecos o mete falsos positivos:
  a) `case <id>:` dentro de `switch (type)`, con o sin helpers (`SetWeaponValues`,
     `DefaultToStaff`...). Si el case esta dentro de un switch ANIDADO, hereda del bloque padre
     solo lo del NIVEL SUPERIOR (`strip_nested_blocks`).
  b) `if (type >= A && type <= B) { ... return; }` ANTES del switch, sin ningun case para esos
     ids (`extract_if_type_stats`). Es la unica definicion de, por ejemplo, 2214..2217 (Paleta,
     Agarre extendido, Spray de pintura, Hormigonera portatil) y 3309..3314 (los 6 contrapesos
     de yoyo), ocho accesorios reales que si admiten prefijo. Antes del 8-sep-2026 ese bloque se
     heredaba ENTERO como `outer` del switch siguiente, con dos efectos a la vez: esos ids se
     quedaban fuera de la tabla, y su `accessory = true;` se le pegaba a 56 objetos del switch
     que en el juego real no admiten prefijo ninguno (muebles dinasticos, bolsas del tesoro,
     bloques de arenisca, ropa de vanidad de obsidiana...).
"""
import io
import json
import os
import re

VANILLA = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\TerrariaVanilla\Terraria"
TMODLOADER = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria"
ITEM_SRC = os.path.join(VANILLA, "Item.cs")
LEGACY_SRC = os.path.join(VANILLA, "GameContent", "Prefixes", "PrefixLegacy.cs")
ITEMID_SRC = os.path.join(VANILLA, "ID", "PrefixID.cs")
ITEMIDS_SRC = os.path.join(VANILLA, "ID", "ItemID.cs")
CALAMITY_SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\CalamityMod\CalamityMod\Items"
ASSETS = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets"
CATALOG = os.path.join(ASSETS, "calamity", "catalog.json")
OUT = os.path.join(ASSETS, "calamity", "best_prefix.json")
OUT_TML = os.path.join(ASSETS, "calamity", "best_prefix_tml.json")

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


def strip_nested_blocks(texto):
    """Deja solo las sentencias del NIVEL SUPERIOR de `texto`, vaciando el cuerpo de cualquier
    bloque `{...}` anidado.

    Hace falta para heredar bien el `outer` (ver `find_case_blocks`): lo que hay dentro de un
    `if (type >= 2214 && type <= 2217) { ... accessory = true; ... return; }` pertenece a ESOS
    ids, no al `switch (type)` que viene detras. Sin esto, ese `accessory = true` se pegaba a
    todos los `case` del switch siguiente y metia en la tabla decenas de objetos que en el juego
    real NO admiten prefijo ninguno (muebles dinasticos, bolsas del tesoro, bloques de
    arenisca...). Los ids de esos `if` se recogen aparte, en `extract_if_type_stats`."""
    out, i, n, depth = "", 0, len(texto), 0
    while i < n:
        ch = texto[i]
        if ch == "/" and i + 1 < n and texto[i + 1] == "/":
            j = texto.find("\n", i)
            i = (j + 1) if j != -1 else n
            continue
        if ch == "/" and i + 1 < n and texto[i + 1] == "*":
            j = texto.find("*/", i + 2)
            i = (j + 2) if j != -1 else n
            continue
        if ch in (DQUOTE, SQUOTE):
            q, j = ch, i + 1
            while j < n and texto[j] != q:
                j += 2 if texto[j] == BACKSLASH else 1
            if depth == 0:
                out += texto[i:j + 1]
            i = j + 1
            continue
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
        elif depth == 0:
            out += ch
        i += 1
    return out


def find_case_blocks(body, outer=""):
    """`outer` = texto del bloque PADRE que precede a este switch anidado. Sus asignaciones
    valen tambien para los ids del switch de dentro: es un patron real de Item.cs (los yoyos
    3278..3292 ponen `useAnimation = 25;` en el bloque comun y solo damage/knockBack en el
    switch(type) anidado). Sin heredarlo, 3315..3317 salen sin useAnimation.

    Solo se hereda el NIVEL SUPERIOR de ese texto (`strip_nested_blocks`): los bloques `{...}`
    que hay por el camino son `if (type ...)` de otros ids."""
    out, i, n = [], 0, len(body)
    while i < n:
        m = SWITCH_TYPE_RE.search(body, i)
        if not m:
            break
        o = m.end() - 1
        c = find_matching_brace(body, o)
        out.extend(extract_immediate_cases(body, o + 1, c, outer + strip_nested_blocks(body[i:m.start()])))
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
    # La firma cambia entre arboles (`public bool ...` con `out value` en 1.4.5.8, `private bool
    # ...` sin el en 1.4.4.9) y el cuerpo termina en un sitio distinto: en 1.4.5.8 justo antes de
    # calcular `value`, en 1.4.4.9 en el primero de los cuatro filtros de "el cambio no se
    # notaria". El `switch` de casos que hay en medio es el mismo en las dos.
    m = re.search(r"(?:public|private) bool TryGetPrefixStatMultipliersForItem", item_text)
    start = m.start()
    fin = min(p for p in (item_text.find("value = 1f * dmg", start),
                          item_text.find("if (dmg != 1f", start)) if p != -1)
    body = item_text[start:fin]
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
    # `DefaultToGolfBall(proj)` tambien pone `accessory = true;` (y `maxStack = 1`). Son las 16
    # pelotas de golf reales (3989, 4242..4255), accesorios que el motor SI deja prefijar
    # (`Item.CanHavePrefixes()` devuelve true para ellas en el juego real, comprobado).
    "DefaultToGolfBall": {"accessory": True},
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


def apply_block(entry, block):
    """Aplica a `entry` las asignaciones reales de un bloque de codigo, EN ORDEN TEXTUAL: es
    habitual que un helper ponga un valor y una linea posterior lo pise (o al reves) -
    `case 4058: DefaultToBow(17, 11f); SetWeaponValues(8, 5f);`."""
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
    return entry


SETDEFAULTS_RE = re.compile(r"(?:public|private) void (SetDefaults\d|SetFoodDefaults)\(int type\)")
IF_TYPE_RE = re.compile(r"if\s*\(\s*(type\s*[=<>!][^)]{0,200})\)\s*\{")
IF_RANGE_RE = re.compile(r"^type\s*>=\s*(\d+)\s*&&\s*type\s*<=\s*(\d+)$")
IF_EQ_LIST_RE = re.compile(r"^type\s*==\s*\d+(?:\s*\|\|\s*type\s*==\s*\d+)*$")


def ids_de_condicion(cond):
    """Los ids concretos que cumplen un `if (type ...)`, o None si la condicion no nombra un
    conjunto CERRADO y pequeño de ids. Solo se aceptan las tres formas reales de Item.cs que
    definen un objeto entero: `type == N`, `type == A || type == B ...` y `type >= A && type <= B`.
    Todo lo demas (`type <= N`, `type < A || type > B`, condiciones con otras variables) se ignora
    a proposito: son guardas de flujo, no la definicion de unos objetos concretos."""
    cond = " ".join(cond.split())
    m = IF_RANGE_RE.match(cond)
    if m:
        a, b = int(m.group(1)), int(m.group(2))
        return list(range(a, b + 1)) if 0 < b - a < 64 else None
    if IF_EQ_LIST_RE.match(cond):
        return [int(x) for x in re.findall(r"\d+", cond)]
    return None


def extract_if_type_stats(item_text):
    """Segunda via real por la que Item.cs define un objeto entero: `if (type >= A && type <= B)
    { ... return; }` ANTES del `switch (type)`, sin ningun `case` para esos ids.

    Sin esta pasada quedaban fuera de la tabla accesorios reales que si admiten prefijo -
    2214..2217 (Paleta, Agarre extendido, Spray de pintura, Hormigonera portatil) y 3309..3314
    (los seis contrapesos de yoyo) -, porque su unica definicion en todo el fichero es un bloque
    de estos."""
    stats = {}
    for m in SETDEFAULTS_RE.finditer(item_text):
        o = item_text.index("{", m.start())
        fin = find_matching_brace(item_text, o)
        pos = o
        while True:
            mi = IF_TYPE_RE.search(item_text, pos, fin)
            if not mi:
                break
            abre = mi.end() - 1
            cierra = find_matching_brace(item_text, abre)
            ids = ids_de_condicion(mi.group(1))
            if ids:
                bloque = strip_variant_blocks(item_text[abre:cierra + 1])
                for iid in ids:
                    apply_block(stats.setdefault(iid, {}), bloque)
            pos = mi.end()
    return {k: v for k, v in stats.items() if k > 0 and v}


def extract_vanilla_stats(item_text):
    redirects = {}
    stats = {}
    # Capa base: los `if (type ...)` que definen objetos enteros. Si un id tiene ademas un `case`
    # propio, el `case` manda (se aplica encima).
    for iid, entry in extract_if_type_stats(item_text).items():
        stats[iid] = dict(entry)
    for m in SETDEFAULTS_RE.finditer(item_text):
        pos = m.start()
        o = item_text.index("{", pos)
        body = item_text[pos:find_matching_brace(item_text, o) + 1]
        for iid, raw_block in find_case_blocks(body):
            if iid <= 0:
                continue
            block = strip_variant_blocks(raw_block)
            entry = stats.setdefault(iid, {})
            apply_block(entry, block)
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


# ------------------------------------------------------------------- 3. pools y sets reales
def load_pools_and_sets(raiz=None):
    """`raiz` = carpeta `Terraria\\` del arbol decompilado a leer. Por defecto la de Terraria
    1.4.5.8 (`VANILLA`, la de la app de escritorio); con `TMODLOADER` da los pools y sets REALES
    de tModLoader 1.4.4.9, que es otro juego a estos efectos:

      - 1.4.5.8 tiene `PrefixesForMagic` y `PrefixesForSummons` SEPARADOS (invocacion usa
        85 Fabled..97, prefijos que en 1.4.4.9 no existen: alli `PrefixID.Count` es 85).
      - 1.4.4.9 tiene un unico `PrefixesForMagicAndSummons` (mismos 36 ids que
        `PrefixesForMagic` de 1.4.5.8, tope 83 Mythical) y un unico bool set `MagicAndSummon`.
        Que las armas de invocacion de MOD caen en ese mismo pool esta confirmado en el codigo
        real: `SummonDamageClass.GetPrefixInheritance(dc) => dc == DamageClass.Magic`, o sea
        `ModItem.MagicPrefix()` es true para ellas y `Item.GetPrefixCategories()` las manda a
        `PrefixCategory.Magic`."""
    raiz = raiz or VANILLA
    legacy = io.open(os.path.join(raiz, "GameContent", "Prefixes", "PrefixLegacy.cs"), encoding="utf-8").read()
    unificado = "PrefixesForMagicAndSummons" in legacy

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
        "magic": arr("PrefixesForMagicAndSummons" if unificado else "PrefixesForMagic"),
        "boomerangsChakrams": arr("PrefixesForBoomeransAndChakrums"),
        "terrarianYoyo": arr("PrefixesForBoomeransAndChakrums_TerrarianYoyo"),
        "accessories": arr("PrefixesForAccessories"),
    }
    sets = {
        "swords": bset("SwordsHammersAxesPicks"),
        "spears": bset("SpearsMacesChainsawsDrillsPunchCannon"),
        "gunsBows": bset("GunsBows"),
        "magic": bset("MagicAndSummon" if unificado else "Magic"),
        "boomerangsChakrams": bset("BoomerangsChakrams"),
        "terrarianYoyo": bset("ItemsThatCanHaveLegendary2"),
    }
    if unificado:
        # En 1.4.4.9 invocacion NO es un pool aparte: comparte el de magia. Se deja la clave
        # para que el resto del script no tenga que saber en que version esta.
        pools["summons"] = pools["magic"]
        sets["summons"] = set()
    else:
        pools["summons"] = arr("PrefixesForSummons")
        sets["summons"] = bset("Summon")
    itemid = io.open(os.path.join(raiz, "ID", "ItemID.cs"), encoding="utf-8").read()
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
    skipped_vanity = skipped_blacklist = skipped_sin_daño = 0
    for iid in sorted(candidates):
        if iid <= 0:
            continue
        pool_key = next((k for k in POOL_ORDER if iid in sets[k]), None)
        if pool_key is not None and not (stats.get(iid, {}).get("damage") or 0) > 0 \
                and not stats.get(iid, {}).get("accessory"):
            # `Item.CanHavePrefixes()` real: `if (damage <= 0) return IsAPrefixableAccessory();`.
            # Estar en un set de arma NO basta. Unico caso vanilla en las dos versiones: la
            # Pistola de monedas (905, `damage = 0;` literal - su daño sale de la moneda que
            # dispara). El motor devuelve false para ella de verdad, comprobado en el juego.
            skipped_sin_daño += 1
            continue
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
          f"({skipped_vanity} accesorios de vanidad, {skipped_blacklist} de la lista negra "
          f"CanGetPrefixes y {skipped_sin_daño} sin daño base descartados: en el juego real "
          f"no admiten prefijo)")
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


def build_calamity_missing(mult, pools, sets, vanilla_stats, catalog, existing, forzar=()):
    """Aplica el MISMO criterio a los objetos de Calamity que aun no tenian entrada.

    `forzar` = nombres que hay que resolver aunque su categoria del catalogo no sea de arma ni
    de accesorio. Hace falta al rehacer una tabla para otro arbol: hay armas reales catalogadas
    por su SET, no por su tipo (`WulfrumFusionCannon`, categoria `Armor/Wulfrum`, pero
    `damageType = DamageClass.Summon`), y sin esto se perderian por el camino."""
    sources = index_calamity_sources()
    added, unresolved = {}, []
    for item in catalog:
        name = item["internal"]
        if name in existing:
            continue
        category = item.get("category") or ""
        if not (category.startswith("Weapons") or category.startswith("Accessories")) and name not in forzar:
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
def generar(raiz, salida, previous, catalog, etiqueta):
    """Una tabla completa contra UN arbol decompilado concreto. `previous` es la tabla anterior:
    de ahi salen `prefixNames` y las entradas de Calamity que se conservan tal cual."""
    print(f"\n=== {etiqueta}  ({raiz})")
    item_text = io.open(os.path.join(raiz, "Item.cs"), encoding="utf-8").read()
    mult = load_prefix_multipliers(item_text)
    print(f"  {len(mult)} prefijos con multiplicadores reales (Item.TryGetPrefixStatMultipliersForItem)")
    stats = extract_vanilla_stats(item_text)
    print(f"  {len(stats)} objetos vanilla con alguna estadistica real (SetDefaults1..5)")
    pools, sets, cannot = load_pools_and_sets(raiz)

    vanilla = build_vanilla(mult, stats, pools, sets, cannot)

    # Calamity: se conservan las entradas previas, MENOS las que apuntan a un prefijo que no
    # existe en este arbol (`>= PrefixID.Count`). Esas se recalculan con el pool real de aqui,
    # que es exactamente para lo que sirve `build_calamity_missing`: darle como "ya existentes"
    # solo las validas hace que rehaga justo las otras, sin tocar ni una del resto.
    tope = max(max(p) for p in pools.values())     # el PrefixID mas alto que existe en este arbol
    conservadas = {k: v for k, v in previous["calamity"].items() if v <= tope}
    descartadas = len(previous["calamity"]) - len(conservadas)
    if descartadas:
        print(f"  calamity: {descartadas} entradas apuntaban a un prefijo inexistente en este arbol, se recalculan")
    rehacer = set(previous["calamity"]) - set(conservadas)
    calamity = dict(conservadas)
    calamity.update(build_calamity_missing(mult, pools, sets, stats, catalog, conservadas, rehacer))
    perdidas = sorted(rehacer - set(calamity))
    if perdidas:
        print(f"  calamity: {len(perdidas)} sin resolver en este arbol, se quedan FUERA (no se inventa valor): {perdidas}")

    out = {
        "prefixNames": previous["prefixNames"],
        "vanilla": {str(k): v for k, v in sorted(vanilla.items())},
        "calamity": {k: calamity[k] for k in sorted(calamity)},
    }
    with io.open(salida, "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, separators=(",", ":"))
    print(f"  escrito {salida}")
    print(f"    {len(out['vanilla'])} vanilla + {len(out['calamity'])} Calamity")
    return out


def main():
    previous = json.load(io.open(OUT, encoding="utf-8"))
    catalog = json.load(io.open(CATALOG, encoding="utf-8"))
    print(f"tabla anterior: {len(previous['vanilla'])} vanilla + {len(previous['calamity'])} Calamity")

    # 1) La de la app de escritorio, contra Terraria 1.4.5.8 (sin cambios de criterio).
    generar(VANILLA, OUT, previous, catalog, "Terraria 1.4.5.8 -> best_prefix.json (app de escritorio)")

    # 2) La del MOD, contra tModLoader 1.4.4.9 - que es el juego real donde corre TerrakeepMod.
    # No es la misma tabla: alli Magia e Invocacion comparten pool (tope 83 Mythical, no existen
    # 85..97), y ademas varias decenas de objetos tienen estadisticas distintas (los baculos de
    # invocacion, por ejemplo, SI gastan mana en 1.4.4.9 y no en 1.4.5.8, y eso cambia que
    # prefijos son aplicables). Se copia tal cual a TerrakeepMod/Assets/best_prefix.json.
    generar(TMODLOADER, OUT_TML, previous, catalog, "tModLoader 1.4.4.9 -> best_prefix_tml.json (mod)")


if __name__ == "__main__":
    main()
