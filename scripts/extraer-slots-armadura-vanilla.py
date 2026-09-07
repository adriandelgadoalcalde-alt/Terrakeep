"""
Extrae, por id real de objeto vanilla, el VALOR literal de headSlot/bodySlot/legSlot -
el indice real de sprite de armadura (Armor_Head_N.xnb / Armor_Legs_N.xnb / Images/Armor/
Armor_N.xnb) que Terraria usa para dibujar esa pieza puesta sobre el personaje. Pedido
explicito del usuario (3-sep-2026): "los personajes de inicio no se visualizan como
realmente son en el juego... que muestre el personaje con la vanidad que tiene cada uno
pero fiel al guardado".

Ya existia extraer-slot-kind-vanilla.py, que solo guarda SI un objeto tiene headSlot/
bodySlot/legSlot asignado (bitmask, para restringir que se puede equipar en cada slot) -
aqui hace falta el VALOR real asignado, no solo el bit. Reutiliza el mismo escaner de
bloques (split_by_case) ya depurado en extraer-categorias-vanilla.py/
extraer-slot-kind-vanilla.py - NO reinventarlo.

Fuente real: Downloads\\tModLoader-Decompiled\\tModLoader\\Terraria\\Item.cs. Version REAL
de esta carpeta: 1.4.4.9 (confirmado independientemente en la sexta auditoria de Opus,
Main.cs decompilado real: "assemblyVersionNumber = 1.4.4.9" - el comentario anterior aqui
decia 1.4.5.8 por error; esa es la version real de la OTRA carpeta decompilada,
TerrariaVanilla, no de esta). Confirmado a mano (Terraria.Initializers.
AssetInitializer.cs, decompilado real): TextureAssets.ArmorHead[n] se carga de
"Images/Armor_Head_" + n, TextureAssets.ArmorLeg[n] de "Images/Armor_Legs_" + n,
TextureAssets.ArmorBodyComposite[n] de "Images/Armor/Armor_" + n (el compuesto
torso+brazo ya renderizado, MISMA rejilla 9x4 de 40x56 que ya usa TorsoSkin - verificado
extrayendo una muestra real con xnb-to-png.js antes de escribir este script, ver
bitacora.md). No existen Armor_Body_N/Female_Body_N como ficheros sueltos en la
instalacion real de Steam (solo el compuesto) - esta app ya usa unicamente la variante de
piel StarterMale para ambos generos (decision ya tomada en el preview de cuerpo completo),
asi que el compuesto encaja sin mas huecos que tapar.

Salida: Terrakeep.App/Assets/vanilla_armor_slots.json -
{"id": {"h":N,"b":N,"l":N}} - solo las claves realmente asignadas (headSlot/bodySlot/
legSlot por separado, un objeto puede tener una, dos o las tres si fuera un caso raro,
aunque en la practica cada objeto de armadura real solo asigna UNA).
"""
import json
import re

ITEM_SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Item.cs"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets\vanilla_armor_slots.json"

BACKSLASH = chr(92)
DQUOTE = chr(34)
SQUOTE = chr(39)


def split_by_case(body_text: str, switch_re: re.Pattern, case_re: re.Pattern) -> list[tuple[int, int]]:
    """Calco exacto de extraer-categorias-vanilla.py/extraer-slot-kind-vanilla.py: recorre
    TODOS los bloques switch del metodo, solo cuenta como limite de item un "case N:" a
    profundidad 1 relativa a ESE switch, ignorando cadenas/chars/comentarios."""
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

HEAD_RE = re.compile(r"\bheadSlot\s*=\s*(\d+)\s*;")
BODY_RE = re.compile(r"\bbodySlot\s*=\s*(\d+)\s*;")
LEG_RE = re.compile(r"\blegSlot\s*=\s*(\d+)\s*;")

slots: dict[int, dict[str, int]] = {}

for body in bodies:
    entries = split_by_case(body, SWITCH_TYPE_RE, CASE_RE)
    for idx, (item_id, block_start) in enumerate(entries):
        if item_id <= 0:
            continue
        block_end = entries[idx + 1][1] if idx + 1 < len(entries) else len(body)
        block = body[block_start:block_end]

        entry: dict[str, int] = {}
        m = HEAD_RE.search(block)
        if m:
            entry["h"] = int(m.group(1))
        m = BODY_RE.search(block)
        if m:
            entry["b"] = int(m.group(1))
        m = LEG_RE.search(block)
        if m:
            entry["l"] = int(m.group(1))

        if entry:
            slots.setdefault(item_id, {}).update(entry)

print(f"{len(slots)} objetos con headSlot/bodySlot/legSlot real")
n_head = sum(1 for e in slots.values() if "h" in e)
n_body = sum(1 for e in slots.values() if "b" in e)
n_legs = sum(1 for e in slots.values() if "l" in e)
print(f"  head: {n_head}  body: {n_body}  legs: {n_legs}")

# Spot-check real conocido: Casco de cobre (id 79 en 1.4.5.8 - MetalTier1, ver
# vanilla_armor_sets.json ya existente: "pieces":[89,80,76]/[90,81,77]... confirmar contra
# uno de esos ids reales en vez de adivinar uno propio).
for known_id in (76, 77, 80, 81, 89, 90):
    print(f"  spot-check id {known_id}: {slots.get(known_id)}")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump({str(k): v for k, v in sorted(slots.items())}, f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
