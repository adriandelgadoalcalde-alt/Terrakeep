"""
Extrae, por id real de objeto vanilla, el VALOR literal de waistSlot/neckSlot/handOnSlot/
handOffSlot/backSlot/shieldSlot/faceSlot/shoeSlot/balloonSlot - el indice real de sprite de
ACCESORIO (Acc_Waist_N.xnb / Acc_Neck_N.xnb / Acc_HandsOn_N.xnb / Acc_HandsOff_N.xnb /
Acc_Back_N.xnb / Acc_Shield_N.xnb / Acc_Face_N.xnb / Acc_Shoes_N.xnb / Acc_Balloon_N.xnb) que
Terraria usa para dibujar ese accesorio puesto sobre el personaje. PortSeleccion Encargo1
(25-sep-2026): extiende a los 7 slots de accesorio la misma regla ya aplicada a
headSlot/bodySlot/legSlot en extraer-slots-armadura-vanilla.py - NO reinventar el escaner de
bloques (split_by_case), calco exacto del de ese script hermano. GapAnalysis Encargo D
(25-sep-2026): ampliado a shoeSlot (8º campo real) - mismo patron exacto, sin logica nueva.
GapAnalysis Encargo C (25-sep-2026): ampliado ademas a balloonSlot (9º campo real) - mismo
patron exacto, sin logica nueva.

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
{"id": {"w":N,"n":N,"ho":N,"hf":N,"bk":N,"s":N,"fc":N,"sh":N,"bl":N}} - solo las claves
realmente asignadas (un objeto real normalmente solo asigna UNA de las 9, pero se guardan
todas las que aparezcan, igual que el script hermano de armadura).
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

print(f"{len(slots)} objetos con algun slot de accesorio real")
for key, label in (("w", "waist"), ("n", "neck"), ("ho", "handOn"), ("hf", "handOff"),
                    ("bk", "back"), ("s", "shield"), ("fc", "face"), ("sh", "shoe"),
                    ("bl", "balloon")):
    n = sum(1 for e in slots.values() if key in e)
    print(f"  {label}: {n}")

# Spot-check real conocido, valores literales vistos a mano en el propio Item.cs antes de
# ejecutar el escaner: id 15 -> waistSlot=2 (Item.cs:3787), id 156 -> shieldSlot=1
# (Item.cs:5534, Cobalt Shield). GapAnalysis Encargo D: id 54 -> shoeSlot=6 (Item.cs:4267,
# Hermes Boots - sin entrada en MaleToFemaleID), id 5077 -> shoeSlot=25 (Item.cs:45325, Glass
# Slipper - SI tiene entrada real: ArmorIDs.Shoe.Sets.MaleToFemaleID[25]=26). GapAnalysis
# Encargo C: id 159 -> balloonSlot=8 (Item.cs:5576, Shiny Red Balloon), id 5076 ->
# balloonSlot=18 (Item.cs:45316, Royal Scepter - el UNICO balloonSlot real con
# DrawInFrontOfBackArmLayer=true, ver ArmorIDs.cs:2252).
for known_id, esperado in ((15, {"w": 2}), (156, {"s": 1}), (54, {"sh": 6}), (5077, {"sh": 25}),
                            (159, {"bl": 8}), (5076, {"bl": 18})):
    obtenido = slots.get(known_id)
    print(f"  spot-check id {known_id}: {obtenido} (esperado {esperado})")
    if obtenido != esperado:
        print("    AVISO: no coincide - revisar el regex/indice.")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump({str(k): v for k, v in sorted(slots.items())}, f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
