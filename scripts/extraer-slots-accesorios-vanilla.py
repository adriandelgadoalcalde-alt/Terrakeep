"""
Extrae, por id real de objeto vanilla, el VALOR literal de waistSlot/neckSlot/handOnSlot/
handOffSlot/backSlot/shieldSlot/faceSlot/shoeSlot/balloonSlot/wingSlot - el indice real de
sprite de ACCESORIO (Acc_Waist_N.xnb / Acc_Neck_N.xnb / Acc_HandsOn_N.xnb / Acc_HandsOff_N.xnb /
Acc_Back_N.xnb / Acc_Shield_N.xnb / Acc_Face_N.xnb / Acc_Shoes_N.xnb / Acc_Balloon_N.xnb /
Wings_N.xnb) que Terraria usa para dibujar ese accesorio puesto sobre el personaje. Wings
Encargo1 (25-sep-2026): ampliado a wingSlot (12º campo real, item.wingSlot en Item.cs - Player.
UpdateVisibleAccessory tiene el mismo patron "if (item.wingSlot > 0) wings = item.wingSlot;"
que los 11 anteriores) - mismo escaner generico, con 1 excepcion real documentada mas abajo
(WING_MANUAL, fallthrough de 4 casos con expresion no literal, mismo problema ya conocido de
BEARD_MANUAL/FRONT_MANUAL). PortSeleccion Encargo1
(25-sep-2026): extiende a los 7 slots de accesorio la misma regla ya aplicada a
headSlot/bodySlot/legSlot en extraer-slots-armadura-vanilla.py - NO reinventar el escaner de
bloques (split_by_case), calco exacto del de ese script hermano. GapAnalysis Encargo D
(25-sep-2026): ampliado a shoeSlot (8º campo real) - mismo patron exacto, sin logica nueva.
GapAnalysis Encargo C (25-sep-2026): ampliado ademas a balloonSlot (9º campo real) - mismo
patron exacto, sin logica nueva. GapAnalysis Encargo G (25-sep-2026): ampliado ademas a
beardSlot (10º campo real) - a diferencia de los 9 anteriores NO usa el escaner generico
(BEARD_MANUAL mas abajo, con la justificacion completa de por que). GapAnalysis Encargo E
(25-sep-2026): ampliado ademas a frontSlot (11º campo real, Player.cs:37185-37188 real - "if
(item.frontSlot > 0) front = item.frontSlot;"). 7 de los 11 items reales SI caen en el escaner
generico (frontSlot literal dentro de un "case N:" de primer nivel); los otros 4 (rango
2284-2287, "if (type >= 2284 && type <= 2287) { ... frontSlot = (sbyte)(1 + type - 2284); ... }")
tienen el MISMO problema real ya documentado para BEARD_MANUAL (bloque fuera de un "case N:" de
primer nivel - aqui un "if" de rango, ni siquiera un switch anidado - Y valor no literal), asi
que se transcriben a mano en FRONT_MANUAL, mismo criterio exacto.

Confirmado en el decompilado real (Terraria.Player.cs, UpdateVisibleAccessory(int itemSlot,
Item item, ...), linea ~36237-36309 / balloonSlot en 37232-37241): "if (item.waistSlot > 0)
waist = item.waistSlot;" y analogo para neck/handon/handoff/back/shield/face/balloon -
EXACTAMENTE el mismo patron ya usado para headSlot/bodySlot/legSlot en
Item.SetDefaultsN(int type), solo que el campo de destino en Player ya no es por INDICE de
slot (a diferencia de armadura, los slots de accesorio de PlrLoadout.Items[3..9]/Social[3..9]
son genericos - cualquiera puede llevar cualquier TIPO de accesorio) - de ahi que el resolver
(ver EquipmentAppearanceResolver.cs) tenga que escanear los huecos buscando cual de estos
campos tiene cada item, no asumir una posicion fija.

Fuente real: Downloads\\Keep\\tModLoader-Decompiled\\tModLoader\\Terraria\\Item.cs (ruta
corregida 25-sep-2026 - la vieja Downloads\\tModLoader-Decompiled\\ sin Keep\\ ya no existe,
confirmado con Test-Path antes de escribir este script).

Salida: Terrakeep.App/Assets/vanilla_accessory_slots.json -
{"id": {"w":N,"n":N,"ho":N,"hf":N,"bk":N,"s":N,"fc":N,"sh":N,"bl":N,"bd":N,"fr":N,"wg":N}} - solo
las claves realmente asignadas (un objeto real normalmente solo asigna UNA de las 12, pero se
guardan todas las que aparezcan, igual que el script hermano de armadura).
"""
import json
import re

ITEM_SRC = r"C:\Users\adrian\Downloads\Keep\tModLoader-Decompiled\tModLoader\Terraria\Item.cs"
OUT = r"C:\Users\adrian\Downloads\Keep\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets\vanilla_accessory_slots.json"

BACKSLASH = chr(92)
DQUOTE = chr(34)
SQUOTE = chr(39)


def split_by_case(body_text: str, switch_re: re.Pattern, case_re: re.Pattern) -> list[tuple[int, int]]:
    """Calco exacto de extraer-slots-armadura-vanilla.py: recorre TODOS los bloques switch
    del metodo, solo cuenta como limite de item un "case N:" a profundidad 1 relativa a ESE
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


with open(ITEM_SRC, encoding="utf-8") as f:
    item_text = f.read()

method_starts = [m.start() for m in re.finditer(r"public void SetDefaults\d\(int type\)", item_text)]
method_starts.append(len(item_text))
bodies = [item_text[method_starts[i]:method_starts[i + 1]] for i in range(len(method_starts) - 1)]

SWITCH_TYPE_RE = re.compile(r"switch\s*\(\s*type\s*\)\s*\{")
CASE_RE = re.compile(r"case\s+(\d+)\s*:")

FIELD_RES = {
    "w": re.compile(r"\bwaistSlot\s*=\s*(\d+)\s*;"),
    "n": re.compile(r"\bneckSlot\s*=\s*(\d+)\s*;"),
    "ho": re.compile(r"\bhandOnSlot\s*=\s*(\d+)\s*;"),
    "hf": re.compile(r"\bhandOffSlot\s*=\s*(\d+)\s*;"),
    "bk": re.compile(r"\bbackSlot\s*=\s*(\d+)\s*;"),
    "s": re.compile(r"\bshieldSlot\s*=\s*(\d+)\s*;"),
    "fc": re.compile(r"\bfaceSlot\s*=\s*(\d+)\s*;"),
    "sh": re.compile(r"\bshoeSlot\s*=\s*(\d+)\s*;"),
    "bl": re.compile(r"\bballoonSlot\s*=\s*(\d+)\s*;"),
    "fr": re.compile(r"\bfrontSlot\s*=\s*(\d+)\s*;"),
    "wg": re.compile(r"\bwingSlot\s*=\s*(\d+)\s*;"),
    # "bd" (beardSlot) NO entra aqui a proposito - ver BEARD_MANUAL mas abajo, el escaner
    # generico da un resultado REAL pero MAL ATRIBUIDO para este campo concreto.
}

slots: dict[int, dict[str, int]] = {}

for body in bodies:
    entries = split_by_case(body, SWITCH_TYPE_RE, CASE_RE)
    for idx, (item_id, block_start) in enumerate(entries):
        if item_id <= 0:
            continue
        block_end = entries[idx + 1][1] if idx + 1 < len(entries) else len(body)
        block = body[block_start:block_end]

        entry: dict[str, int] = {}
        for key, rx in FIELD_RES.items():
            m = rx.search(block)
            if m:
                entry[key] = int(m.group(1))

        if entry:
            slots.setdefault(item_id, {}).update(entry)

# GapAnalysis Encargo G (25-sep-2026): beardSlot (10º campo real, Item.cs, "if (item.beardSlot >
# 0) beard = item.beardSlot;" en Player.cs:37243-37246) - EXACTAMENTE 4 objetos vanilla reales lo
# declaran en TODO el juego (GingerBeard/WilsonBeardShort/WilsonBeardLong/WilsonBeardMagnificent,
# ArmorIDs.Beard.Count=5 incluyendo el 0="sin barba"), confirmados a mano en el decompilado real:
#   id 2501 (GingerBeard)              -> beardSlot=1   (Item.cs:27930)
#   id 5104 (WilsonBeardShort)         -> beardSlot=2   (Item.cs:45527-45534, ver mas abajo)
#   id 5105 (WilsonBeardLong)          -> beardSlot=3   (idem)
#   id 5106 (WilsonBeardMagnificent)   -> beardSlot=4   (idem)
# NO se anadio "beardSlot" a FIELD_RES/split_by_case por 2 motivos reales, verificados a mano
# ejecutando el escaner con el regex añadido antes de descartarlo (no es una suposicion):
#   1. El bloque real de 2501 vive DENTRO de un "default: switch (type) { ... }" ANIDADO
#      (Item.cs:25833-45608 aprox, el mismo nivel que headSlot=157 del id 2199) - split_by_case
#      solo reconoce "case N:" a profundidad 1 RELATIVA AL PRIMER switch, así que el id 2501 (y
#      todo lo que hay dentro del switch anidado) nunca genera su propia entrada en "entries";
#      en su lugar, el bloque completo del switch anidado queda "colgando" del ULTIMO case de
#      profundidad 1 anterior (id 2191, un objeto totalmente distinto) - confirmado empiricamente:
#      con el regex activo, el escaner atribuia "beardSlot=1" a id 2191, NUNCA a 2501. Bug real
#      y preexistente del escaner para switches anidados (afecta en teoria a cualquier campo que
#      caiga dentro de un switch anidado, no solo beardSlot - fuera del alcance de este encargo
#      arreglar split_by_case en general, ver bitacora.md).
#   2. Los ids 5104/5105/5106 comparten UN SOLO bloque de codigo por fallthrough ("case 5104:
#      case 5105: case 5106: ... beardSlot = (sbyte)(2 + (type - 5104)); ..." Item.cs:45527-
#      45538) - split_by_case solo asocia el bloque real al ULTIMO case de la cadena (5106), los
#      2 anteriores (5104/5105) quedan con un bloque vacio (solo el texto del siguiente "case
#      N:"). Y aunque se asociara bien, el valor NO es un literal (\d+) sino una expresion
#      ((sbyte)(2 + (type - 5104))) que el regex \d+ nunca captura.
# Con solo 4 objetos reales en total y el valor confirmado a mano contra el .cs decompilado
# (mismo criterio que "lo que no se encuentra no se inventa" del resto del proyecto - aqui SI se
# encontro, solo que el escaner generico no lo puede atribuir bien), se transcribe literal en vez
# de forzar un escaner generico a un caso que no encaja con su forma.
BEARD_MANUAL = {2501: 1, 5104: 2, 5105: 3, 5106: 4}
for item_id, beard_id in BEARD_MANUAL.items():
    slots.setdefault(item_id, {})["bd"] = beard_id

# GapAnalysis Encargo E (25-sep-2026): frontSlot (11º campo real) - 4 items reales
# (CrimsonCloak/MysteriousCape/RedCape/WinterCape, ItemID.cs:6114-6120) comparten un UNICO
# bloque real via un "if (type >= 2284 && type <= 2287)" (Item.cs decompilado real) que NO es
# un "case N:" de primer nivel - split_by_case nunca genera una entrada propia para estos 4 ids
# (mismo problema real ya documentado para BEARD_MANUAL, aqui ademas ni siquiera es un switch
# anidado con case, es un rango por "if"), y el valor tampoco es literal
# ("frontSlot = (sbyte)(1 + type - 2284)", "backSlot = (sbyte)(3 + type - 2284)"). Se transcribe
# a mano, backSlot incluido (el mismo bloque real tambien asigna backSlot, y split_by_case
# tampoco lo capturaba para estos 4 ids - confirmado con un volcado real ANTES de este parche:
# los 4 salian ausentes del json, ni "bk" ni "fr").
FRONT_MANUAL = {2284: (1, 3), 2285: (2, 4), 2286: (3, 5), 2287: (4, 6)}  # id -> (frontSlot, backSlot)
for item_id, (front_id, back_id) in FRONT_MANUAL.items():
    entry = slots.setdefault(item_id, {})
    entry["fr"] = front_id
    entry["bk"] = back_id

# GapAnalysis Encargo E: otros 3 items reales (ChippysWings/LunasCloak/DruidicSerpentCloak,
# ItemID.cs:12800/13828/13918) con frontSlot/backSlot LITERALES (no un "if" de rango como los 4
# de arriba) que aun asi el escaner generico no captura - verificado empiricamente (volcado real
# ANTES de este parche: los 3 salian ausentes del json) - mismo motivo real ya documentado para
# BEARD_MANUAL, un "case N:" real que vive dentro de un switch ANIDADO de mas alto nivel (estos 3
# ids son de la era Journey's End, 1.4.0.1+, anadidos dentro del bloque "default" del switch
# principal de SetDefaults - profundidad 1 relativa al switch exterior, no al interior real).
# Valores confirmados a mano contra el .cs decompilado real:
#   id 5627 (ChippysWings)          -> frontSlot=15, backSlot=38 (wingSlot=48, fuera de alcance)
#   id 6141 (LunasCloak)            -> frontSlot=16, backSlot=39
#   id 6186 (DruidicSerpentCloak)   -> frontSlot=17, backSlot=41
FRONT_MANUAL_NESTED = {5627: (15, 38), 6141: (16, 39), 6186: (17, 41)}
for item_id, (front_id, back_id) in FRONT_MANUAL_NESTED.items():
    entry = slots.setdefault(item_id, {})
    entry["fr"] = front_id
    entry["bk"] = back_id

# Wings Encargo1 (25-sep-2026): wingSlot (12º campo real) - 7 items reales que el escaner
# generico pierde, por 2 motivos reales distintos (confirmado con un volcado ANTES de este
# parche: los 7 salian ausentes del json):
#   1. WingsSolar/WingsVortex/WingsNebula/WingsStardust (ItemID.cs: 3468/3469/3470/3471,
#      wingSlot 29/30/31/32) comparten un UNICO bloque via fallthrough ("case 3468: case 3469:
#      case 3470: case 3471: ... wingSlot = (sbyte)(29 + type - 3468); ...", Item.cs:35219-35229
#      real) - split_by_case solo atribuye el bloque al ULTIMO case de la cadena (3471), y ademas
#      el valor no es literal (\d+ no captura "(sbyte)(29 + type - 3468)"), mismo problema real
#      ya documentado para BEARD_MANUAL/FRONT_MANUAL.
#   2. FinWings/FishronWings/MothronWings (ItemID.cs: 2494/2609/2770, wingSlot 25/26/27) SI
#      tienen un valor literal ("wingSlot = 25;"/"= 26;"/"= 27;") en su propio "case N:", pero ese
#      case vive dentro de un switch ANIDADO (misma indentacion extra que el caso real ya
#      documentado para BEARD_MANUAL, id 2501) - profundidad 2 relativa al switch exterior, fuera
#      del alcance de split_by_case (solo depth==1). Confirmado a mano contra el .cs real.
# Se transcriben los 7 a mano.
WING_MANUAL = {3468: 29, 3469: 30, 3470: 31, 3471: 32, 2494: 25, 2609: 26, 2770: 27}
for item_id, wing_id in WING_MANUAL.items():
    slots.setdefault(item_id, {})["wg"] = wing_id

print(f"{len(slots)} objetos con algun slot de accesorio real")
for key, label in (("w", "waist"), ("n", "neck"), ("ho", "handOn"), ("hf", "handOff"),
                    ("bk", "back"), ("s", "shield"), ("fc", "face"), ("sh", "shoe"),
                    ("bl", "balloon"), ("bd", "beard"), ("fr", "front"), ("wg", "wing")):
    n = sum(1 for e in slots.values() if key in e)
    print(f"  {label}: {n}")

# Spot-check real conocido, valores literales vistos a mano en el propio Item.cs antes de
# ejecutar el escaner: id 15 -> waistSlot=2 (Item.cs:3787), id 156 -> shieldSlot=1
# (Item.cs:5534, Cobalt Shield). GapAnalysis Encargo D: id 54 -> shoeSlot=6 (Item.cs:4267,
# Hermes Boots - sin entrada en MaleToFemaleID), id 5077 -> shoeSlot=25 (Item.cs:45325, Glass
# Slipper - SI tiene entrada real: ArmorIDs.Shoe.Sets.MaleToFemaleID[25]=26). GapAnalysis
# Encargo C: id 159 -> balloonSlot=8 (Item.cs:5576, Shiny Red Balloon), id 5076 ->
# balloonSlot=18 (Item.cs:45316, Royal Scepter - el UNICO balloonSlot real con
# DrawInFrontOfBackArmLayer=true, ver ArmorIDs.cs:2252). Wings Encargo1: id 492 -> wingSlot=1
# (Item.cs:9707, Demon Wings), id 749 -> wingSlot=5 (Item.cs:12918, Butterfly Wings), id 1866 ->
# wingSlot=22 (Item.cs:23847, Hoverboard), id 3469 -> wingSlot=30 (WingsVortex, via WING_MANUAL),
# id 2494 -> wingSlot=25 (FinWings, via WING_MANUAL, switch anidado).
for known_id, esperado in ((15, {"w": 2}), (156, {"s": 1}), (54, {"sh": 6}), (5077, {"sh": 25}),
                            (159, {"bl": 8}), (5076, {"bl": 18}), (2501, {"bd": 1}),
                            (5104, {"bd": 2}), (5105, {"bd": 3}), (5106, {"bd": 4}),
                            # GapAnalysis Encargo E: 4744 HunterCloak espera fr=8/bk=24
                            # (Item.cs real, "case 4744: ... backSlot = 24; frontSlot = 8;").
                            (4744, {"bk": 24, "fr": 8}), (2284, {"fr": 1, "bk": 3}),
                            (5627, {"fr": 15, "bk": 38}), (6186, {"fr": 17, "bk": 41}),
                            (492, {"wg": 1}), (749, {"wg": 5}), (1866, {"wg": 22}),
                            (3469, {"wg": 30}), (2494, {"wg": 25})):
    obtenido = slots.get(known_id)
    print(f"  spot-check id {known_id}: {obtenido} (esperado {esperado})")
    if obtenido != esperado:
        print("    AVISO: no coincide - revisar el regex/indice.")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump({str(k): v for k, v in sorted(slots.items())}, f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
