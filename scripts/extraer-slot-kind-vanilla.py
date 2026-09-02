"""
Extrae, por id real de objeto vanilla, un bitmask de "que tipos de slot restringido acepta"
(SlotKind) - pedido explicito 2-sep-2026 ("los slots de tintes, gancho, vagoneta, montura y
mascota solo deberian poderse equipar sus respectivos items" + investigacion previa de
monedas/municion). Consulta a Opus (sexta pasada): un solo enum de flags, calculado UNA vez
aqui, en vez de 9 catalogos o reglas sueltas en C#.

Reutiliza el escaner de bloques SetDefaults ya depurado en extraer-categorias-vanilla.py
(split_by_case) - mismo criterio, no reinventarlo.

Fuentes reales (Downloads\\tModLoader-Decompiled\\tModLoader\\, consulta a Opus con cita
archivo:linea, verificado aqui de nuevo antes de escribir el escaner):
- Municion:  Item.FitsAmmoSlot() real (Item.cs:3544) = ammo>0 || bait>0 || PaintOrCoating ||
  type==353 (Frasco de Miel especial). Aqui se aproxima como ammo>0 || bait>0 || type==353 -
  PaintOrCoating (pintura/revestimiento) queda fuera a proposito (campo booleano separado,
  cobertura marginal, documentado como hueco conocido en vez de forzarlo).
- Moneda:    Item.IsACoin (Item.cs:999) = id en {71,72,73,74}, literal.
- Tinte:     ItemSlot.DyeSwap() (ItemSlot.cs:3101) = dye>0 (excluyendo hairDye, que es un
  slot de Apariencia totalmente distinto, no equipo).
- Gancho:    ItemSlot.cs case 16 = Main.projHook[item.shoot] - proyectiles reales con
  aiStyle=7 en Projectile.cs (patron real "else if (type == N [|| type == M]) { ... aiStyle
  = 7; }", escaneado aqui con una maquina de estados simple sobre esa condicion).
- Montura/Vagoneta: ItemSlot.cs case 17/18 = mountType!=-1, distinguidas solo por
  MountID.Sets.Cart (MountID.cs:17, lista literal real de 27 ids). Ambas via
  DefaultToMount(N) (Item.cs:48449, mountType=N) ademas del campo directo.
- Mascota/Mascota de luz: ItemSlot.cs case 19/20 = buffType>0 && Main.vanityPet[buffType]
  (Main.cs:9378-9445, ~68 asignaciones "vanityPet[N] = true;") / Main.lightPet[buffType]
  (Main.cs:9447-9458, 12 asignaciones). Ademas via DefaultToVanitypet(projId, buffID)
  (Item.cs:47961, buffType=buffID) - 27 usos que el escaneo textual directo del campo
  "buffType = N" NO detecta porque la asignacion vive dentro del metodo ayudante, no inline
  en el bloque del objeto - se detectan aparte por su propia llamada literal.

- Armadura/Accesorio (ampliacion 2-sep-2026, misma sexta pasada - "las armaduras y los
  accesorios, si los quiero [restringidos] arriba"): campos directos reales headSlot/
  bodySlot/legSlot/accessory de Item.cs, MISMO patron ya validado en
  extraer-categorias-vanilla.py (has_head/has_body/has_leg/has_accessory) - vale igual para
  objetos funcionales y de vanidad (vanity=true no cambia el equip type real).

Salida: TerrasavrNative.App/Assets/vanilla_slot_kind.json - {"id": bitmask}, solo ids con
bitmask != 0 (mismo criterio de "lo que no aplica no aparece" que vanilla_categories.json).
Bits (deben coincidir 1:1 con TerrasavrNative.Core.Model.SlotKind):
  1=Ammo 2=Coin 4=Dye 8=Hook 16=Mount 32=Cart 64=VanityPet 128=LightPet
  256=ArmorHead 512=ArmorBody 1024=ArmorLegs 2048=Accessory
"""
import json
import re

ITEM_SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Item.cs"
PROJ_SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Projectile.cs"
MAIN_SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Main.cs"
DYE_SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Initializers\DyeInitializer.cs"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\vanilla_slot_kind.json"

BIT_AMMO, BIT_COIN, BIT_DYE, BIT_HOOK, BIT_MOUNT, BIT_CART, BIT_VANITYPET, BIT_LIGHTPET = (1, 2, 4, 8, 16, 32, 64, 128)
BIT_ARMORHEAD, BIT_ARMORBODY, BIT_ARMORLEGS, BIT_ACCESSORY = (256, 512, 1024, 2048)

# Lista real literal, MountID.cs:17 - los mountType que son vagoneta, no montura.
CART_MOUNT_TYPES = {6, 11, 13, 15, 16, 18, 19, 20, 21, 22, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 38, 39, 51, 53}

BACKSLASH = chr(92)
DQUOTE = chr(34)
SQUOTE = chr(39)


def split_by_case(body_text: str, switch_re: re.Pattern, case_re: re.Pattern) -> list[tuple[int, int]]:
    """Calco exacto de extraer-categorias-vanilla.py: recorre TODOS los bloques switch del
    metodo, solo cuenta como limite de item un "case N:" a profundidad 1 relativa a ESE
    switch, ignorando cadenas/chars/comentarios."""
    entries: list[tuple[int, int]] = []
    i, n = 0, len(body_text)
    in_switch = False
    depth = 0
    while i < n:
        if not in_switch:
            m = switch_re.match(body_text, i)
            if m:
                in_switch, depth, i = True, 1, m.end()
                continue
            i += 1
            continue
        c = body_text[i]
        if c == "/" and i + 1 < n and body_text[i + 1] == "/":
            j = body_text.find("\n", i)
            i = j + 1 if j != -1 else n
            continue
        if c == "/" and i + 1 < n and body_text[i + 1] == "*":
            j = body_text.find("*/", i + 2)
            i = j + 2 if j != -1 else n
            continue
        if c == DQUOTE:
            i += 1
            while i < n and body_text[i] != DQUOTE:
                i += 2 if body_text[i] == BACKSLASH else 1
            i += 1
            continue
        if c == SQUOTE:
            i += 1
            while i < n and body_text[i] != SQUOTE:
                i += 2 if body_text[i] == BACKSLASH else 1
            i += 1
            continue
        if c == "{":
            depth += 1
            i += 1
            continue
        if c == "}":
            depth -= 1
            i += 1
            if depth == 0:
                in_switch = False
            continue
        if depth == 1:
            m2 = case_re.match(body_text, i)
            if m2:
                entries.append((int(m2.group(1)), m2.end()))
                i = m2.end()
                continue
        i += 1
    return entries


# --- Paso 1: proyectiles gancho reales (aiStyle=7) desde Projectile.cs ---------------------

with open(PROJ_SRC, encoding="utf-8") as f:
    proj_text = f.read()

hook_projectile_ids: set[int] = set()
COND_BLOCK_RE = re.compile(r"else if\s*\(([^)]+)\)\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*?)\}", re.DOTALL)
EQ_RE = re.compile(r"\btype\s*==\s*(\d+)")
RANGE_RE = re.compile(r"\btype\s*>=\s*(\d+)\s*&&\s*type\s*<=\s*(\d+)")

for m in COND_BLOCK_RE.finditer(proj_text):
    cond, block = m.group(1), m.group(2)
    if "aiStyle = 7;" not in block:
        continue
    ids = set(int(x) for x in EQ_RE.findall(cond))
    rm = RANGE_RE.search(cond)
    if rm:
        lo, hi = int(rm.group(1)), int(rm.group(2))
        ids |= set(range(lo, hi + 1))
    hook_projectile_ids |= ids

print(f"proyectiles gancho reales (aiStyle=7): {len(hook_projectile_ids)} ids -> {sorted(hook_projectile_ids)[:15]}...")

# --- Paso 2: mascota vanity / mascota de luz reales (por buffType) desde Main.cs -----------

with open(MAIN_SRC, encoding="utf-8") as f:
    main_text = f.read()

vanity_pet_buffs = set(int(x) for x in re.findall(r"\bvanityPet\[(\d+)\]\s*=\s*true;", main_text))
light_pet_buffs = set(int(x) for x in re.findall(r"\blightPet\[(\d+)\]\s*=\s*true;", main_text))
print(f"vanityPet[] reales: {len(vanity_pet_buffs)} buffTypes; lightPet[] reales: {len(light_pet_buffs)} buffTypes")

# --- Paso 2b: tintes reales (por id de objeto) desde DyeInitializer.cs --------------------
#
# dye = GameShaders.Armor.GetShaderIdFromItemId(type) se asigna GLOBAL en Item.cs (linea
# 48970, fuera de cualquier SetDefaults por objeto) - no es detectable por objeto ahi. El id
# real de cada tinte esta en donde se REGISTRA su shader: DyeInitializer.cs, llamadas
# GameShaders.Armor.BindShader(id, ...) (72 veces) directas, mas el helper
# LoadBasicColorDye(base, r,g,b,...) (16 usos, expande a base/base+12/base+31/base+44 - la
# variante corta) o LoadBasicColorDye(base,black,bright,silver, r,g,b,...) (la variante con
# los 4 ids explicitos). GameShaders.Hair.BindShader (tinte de pelo, Apariencia, fuera de
# alcance aqui) usa un metodo distinto, asi que no hace falta excluirlo aparte.

with open(DYE_SRC, encoding="utf-8") as f:
    dye_text = f.read()

dye_item_ids: set[int] = set()
for m in re.finditer(r"Armor\.BindShader\(\s*(\d+)\s*,", dye_text):
    dye_item_ids.add(int(m.group(1)))

# Variante con los 4 ids explicitos: 2do..4to argumento tambien enteros (sin sufijo "f").
for m in re.finditer(r"LoadBasicColorDye\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,", dye_text):
    dye_item_ids.update(int(g) for g in m.groups())

# Variante corta: 2do argumento es un float (lleva "f"), expande a base/+12/+31/+44 (formula
# real de la sobrecarga corta, DyeInitializer.cs:24).
for m in re.finditer(r"LoadBasicColorDye\(\s*(\d+)\s*,\s*[\d.]+f\s*,", dye_text):
    base = int(m.group(1))
    dye_item_ids.update({base, base + 12, base + 31, base + 44})

print(f"tintes de equipo reales (DyeInitializer.cs): {len(dye_item_ids)} ids")

# --- Paso 3: escanear Item.cs por objeto -----------------------------------------------

with open(ITEM_SRC, encoding="utf-8") as f:
    item_text = f.read()

method_starts = [m.start() for m in re.finditer(r"public void SetDefaults\d\(int type\)", item_text)]
method_starts.append(len(item_text))
bodies = [item_text[method_starts[i]:method_starts[i + 1]] for i in range(len(method_starts) - 1)]

SWITCH_TYPE_RE = re.compile(r"switch\s*\(\s*type\s*\)\s*\{")
CASE_RE = re.compile(r"case\s+(\d+)\s*:")

AMMO_RE = re.compile(r"\bammo\s*=\s*(?!0\b|AmmoID\.None\b)")
BAIT_RE = re.compile(r"\bbait\s*=\s*(?!0\b)")
SHOOT_RE = re.compile(r"\bshoot\s*=\s*(\d+)\s*;")
HEAD_RE = re.compile(r"\bheadSlot\s*=\s*(?!-1\b)")
BODY_RE = re.compile(r"\bbodySlot\s*=\s*(?!-1\b)")
LEG_RE = re.compile(r"\blegSlot\s*=\s*(?!-1\b)")
ACCESSORY_RE = re.compile(r"\baccessory\s*=\s*true")
MOUNTTYPE_RE = re.compile(r"\bmountType\s*=\s*(\d+)\s*;")
DEFAULTTOMOUNT_RE = re.compile(r"\bDefaultToMount\(\s*(\d+)\s*\)")
BUFFTYPE_RE = re.compile(r"\bbuffType\s*=\s*(\d+)\s*;")
DEFAULTTOVANITYPET_RE = re.compile(r"\bDefaultToVanitypet\(\s*\d+\s*,\s*(\d+)\s*\)")

kinds: dict[int, int] = {}

for body in bodies:
    for item_id, block_start in split_by_case(body, SWITCH_TYPE_RE, CASE_RE):
        if item_id <= 0:
            continue
        # mismo criterio de recorte que extraer-categorias-vanilla.py: hasta el siguiente
        # "case M:" real de este mismo switch (aproximado buscando el siguiente en la lista
        # ya extraida no es trivial aqui sin repetir todo el recorrido, asi que se re-usa la
        # lista completa de esta pasada).
        pass
    # recalcular con acceso a la lista completa para poder cortar bloques
    entries = split_by_case(body, SWITCH_TYPE_RE, CASE_RE)
    for idx, (item_id, block_start) in enumerate(entries):
        if item_id <= 0:
            continue
        block_end = entries[idx + 1][1] if idx + 1 < len(entries) else len(body)
        block = body[block_start:block_end]

        mask = 0
        if AMMO_RE.search(block) or BAIT_RE.search(block) or item_id == 353:
            mask |= BIT_AMMO
        if item_id in (71, 72, 73, 74):
            mask |= BIT_COIN
        if item_id in dye_item_ids:
            mask |= BIT_DYE
        if HEAD_RE.search(block):
            mask |= BIT_ARMORHEAD
        if BODY_RE.search(block):
            mask |= BIT_ARMORBODY
        if LEG_RE.search(block):
            mask |= BIT_ARMORLEGS
        if ACCESSORY_RE.search(block):
            mask |= BIT_ACCESSORY

        shoot_m = SHOOT_RE.search(block)
        if shoot_m and int(shoot_m.group(1)) in hook_projectile_ids:
            mask |= BIT_HOOK

        mount_val = None
        mt_m = MOUNTTYPE_RE.search(block)
        if mt_m:
            mount_val = int(mt_m.group(1))
        else:
            dtm_m = DEFAULTTOMOUNT_RE.search(block)
            if dtm_m:
                mount_val = int(dtm_m.group(1))
        if mount_val is not None:
            mask |= BIT_CART if mount_val in CART_MOUNT_TYPES else BIT_MOUNT

        buff_val = None
        bt_m = BUFFTYPE_RE.search(block)
        if bt_m:
            buff_val = int(bt_m.group(1))
        else:
            dvp_m = DEFAULTTOVANITYPET_RE.search(block)
            if dvp_m:
                buff_val = int(dvp_m.group(1))
        if buff_val is not None:
            if buff_val in light_pet_buffs:
                mask |= BIT_LIGHTPET
            elif buff_val in vanity_pet_buffs:
                mask |= BIT_VANITYPET

        if mask != 0:
            kinds[item_id] = kinds.get(item_id, 0) | mask

print(f"{len(kinds)} objetos con algun SlotKind real")
from collections import Counter
bit_names = {BIT_AMMO: "Ammo", BIT_COIN: "Coin", BIT_DYE: "Dye", BIT_HOOK: "Hook",
             BIT_MOUNT: "Mount", BIT_CART: "Cart", BIT_VANITYPET: "VanityPet", BIT_LIGHTPET: "LightPet",
             BIT_ARMORHEAD: "ArmorHead", BIT_ARMORBODY: "ArmorBody", BIT_ARMORLEGS: "ArmorLegs", BIT_ACCESSORY: "Accessory"}
counts = Counter()
for mask in kinds.values():
    for bit, name in bit_names.items():
        if mask & bit:
            counts[name] += 1
for name, n in sorted(counts.items(), key=lambda kv: -kv[1]):
    print(f"  {name}: {n}")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump({str(k): v for k, v in sorted(kinds.items())}, f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")

# Nota de cobertura real (no ocultarla): Item.cs tiene 27 "mountType = " + 25
# "DefaultToMount(" = 52 asignaciones de montura/vagoneta reales en total, pero split_by_case
# solo encuentra el objeto dueño de 37 de ellas (Mount=11 + Cart=26 arriba) - mismo techo de
# cobertura ya documentado en extraer-categorias-vanilla.py (~84% de los ids reales caen
# dentro de un "case N:" a profundidad 1 de un switch(type) real; el resto usa una forma de
# despacho que este escaner no cubre). Efecto real: un ~30% de las monturas/vagonetas
# vanilla se quedaran SIN restringir (aceptaran cualquier objeto en su slot, igual que hoy) -
# no es un falso positivo (nunca RECHAZA un objeto que si deberia aceptar), es under-coverage
# conocido. Ampliar el escaner a ese resto queda fuera de esta pasada.
