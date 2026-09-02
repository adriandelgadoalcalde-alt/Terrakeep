"""
Extrae la tabla REAL de bonificacion de set completo de armadura (head/body/legs -> texto),
pregunta a Opus sobre el diseño 2-sep-2026, cuarta pasada ("si tienes el set completo siempre
hay una bonificacion... no se muestra al pasar el raton"). Dos fuentes reales:

1. headSlot/bodySlot/legSlot por item id (Item.cs, mismo split_by_case que
   extraer-estadisticas-vanilla.py) - el juego real identifica un set por INDICE DE TEXTURA de
   equipo, no por item id (verificado: el Casco de cobre es el item 89, pero headSlot=1).
2. Las condiciones reales de Player.UpdateArmorSets (~66 asignaciones de setBonus) - cada una
   compara head/body/legs (los headSlot/bodySlot/legSlot puestos, no ids) contra literales
   fijos o rangos. Se evalua cada condicion contra el producto cartesiano de los valores
   candidatos que aparecen en ESA condicion (dominio siempre pequeño, unos pocos valores por
   variable) - nunca se inventa ninguna combinacion.

Caso especial real (unico en todo el metodo): el bonus "Hallowed" vs "HallowedSummoner" es un
if/else anidado - el "else" (sin condicion propia) cubre los head reales de la condicion
EXTERIOR que no entran en el "if" interior (254/258). Documentado y resuelto a mano abajo con
los valores reales leidos del propio codigo (ver comentario en el diccionario NESTED_ELSE) en
vez de escribir un parser generico de else para un unico caso real en todo el archivo.
"""
import itertools
import json
import re

ITEM_SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Item.cs"
PLAYER_SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Player.cs"
GAME_JSON = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\TerrariaVanilla\Terraria.Localization.Content.es-ES.Game.json"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\vanilla_armor_sets.json"

# --- 1. headSlot/bodySlot/legSlot por item id (mismo split_by_case real ya usado) ---
with open(ITEM_SRC, encoding="utf-8") as f:
    item_text = f.read()

SWITCH_TYPE_RE = re.compile(r"switch\s*\(\s*type\s*\)\s*\{")
CASE_RE = re.compile(r"case\s+(\d+)\s*:")
BACKSLASH, DQUOTE, SQUOTE = chr(92), chr(34), chr(39)


def split_by_case(body_text: str) -> list[tuple[int, int]]:
    entries: list[tuple[int, int]] = []
    i, n = 0, len(body_text)
    in_switch, depth = False, 0
    while i < n:
        if not in_switch:
            m = SWITCH_TYPE_RE.match(body_text, i)
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
            m2 = CASE_RE.match(body_text, i)
            if m2:
                entries.append((int(m2.group(1)), m2.end()))
                i = m2.end()
                continue
        i += 1
    return entries


method_starts = [m.start() for m in re.finditer(r"public void SetDefaults\d\(int type\)", item_text)]
method_starts.append(len(item_text))
bodies = [item_text[method_starts[i]:method_starts[i + 1]] for i in range(len(method_starts) - 1)]

SLOT_RE = {
    "head": re.compile(r"\bheadSlot\s*=\s*(-?\d+)\s*;"),
    "body": re.compile(r"\bbodySlot\s*=\s*(-?\d+)\s*;"),
    "legs": re.compile(r"\blegSlot\s*=\s*(-?\d+)\s*;"),
}
# slot_by_kind["head"][slotIndex] = itemId (inversion real, 1:1 verificada por Opus)
slot_by_kind: dict[str, dict[int, int]] = {"head": {}, "body": {}, "legs": {}}

for body in bodies:
    for item_id, block_start in split_by_case(body):
        if item_id <= 0:
            continue
        block_end = body.find("case ", block_start)
        block = body[block_start: block_end if block_end != -1 else len(body)]
        for kind, rx in SLOT_RE.items():
            m = rx.search(block)
            if m:
                slot_by_kind[kind][int(m.group(1))] = item_id

print("slots reales:", {k: len(v) for k, v in slot_by_kind.items()})

# --- 2. Condiciones reales de UpdateArmorSets ---
with open(PLAYER_SRC, encoding="utf-8") as f:
    player_text = f.read()

mstart = player_text.index("public void UpdateArmorSets(int i)")
open_brace = player_text.index("{", mstart)
depth = 0
i = open_brace
while True:
    if player_text[i] == "{":
        depth += 1
    elif player_text[i] == "}":
        depth -= 1
        if depth == 0:
            method_end = i + 1
            break
    i += 1
method_body = player_text[open_brace:method_end]

# Bloques "if (COND) { ... setBonus = Language.GetTextValue("ArmorSetBonus.KEY"); ... }"
# a CUALQUIER nivel de anidamiento - cada uno con su propia condicion explicita ya basta
# (ver docstring: el unico "else" sin condicion propia de todo el archivo se resuelve aparte).
IF_RE = re.compile(r"if\s*\(")
KEY_RE = re.compile(r'ArmorSetBonus\.(\w+)')

results: dict[str, set[tuple[int, int, int]]] = {}  # key -> set of (head, body, legs) slot triples


def find_matching_paren(text: str, open_idx: int) -> int:
    depth = 0
    i = open_idx
    while True:
        if text[i] == "(":
            depth += 1
        elif text[i] == ")":
            depth -= 1
            if depth == 0:
                return i
        i += 1


def find_matching_brace(text: str, open_idx: int) -> int:
    depth = 0
    i = open_idx
    while True:
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
            if depth == 0:
                return i
        i += 1


def candidates_for(cond: str) -> dict[str, list[int]]:
    cand: dict[str, set[int]] = {"head": set(), "body": set(), "legs": set()}
    for var in cand:
        for m in re.finditer(rf"\b{var}\s*(==|>=|<=|>|<)\s*(\d+)", cond):
            cand[var].add(int(m.group(2)))
        # Rango real (unico caso: Shroomite, head >= 103 && head <= 105) - expandir inclusive.
        ge = re.search(rf"\b{var}\s*>=\s*(\d+)", cond)
        le = re.search(rf"\b{var}\s*<=\s*(\d+)", cond)
        if ge and le:
            for v in range(int(ge.group(1)), int(le.group(1)) + 1):
                cand[var].add(v)
    return {k: sorted(v) if v else [-999] for k, v in cand.items()}


def to_python(cond: str) -> str:
    return cond.replace("&&", " and ").replace("||", " or ")


i = 0
n = len(method_body)
while i < n:
    m = IF_RE.search(method_body, i)
    if not m:
        break
    paren_open = m.end() - 1
    paren_close = find_matching_paren(method_body, paren_open)
    cond = method_body[paren_open + 1:paren_close]

    j = paren_close + 1
    while method_body[j] in " \t\r\n":
        j += 1
    if method_body[j] != "{":
        i = m.end()
        continue
    brace_close = find_matching_brace(method_body, j)
    block = method_body[j + 1:brace_close]

    key_m = KEY_RE.search(block)
    if key_m and "head" in cond + block or "body" in cond:
        pass
    if key_m and re.search(r"\bhead\b|\bbody\b|\blegs\b", cond):
        cand = candidates_for(cond)
        py_cond = to_python(cond)
        matches = set()
        for h, b, l in itertools.product(cand["head"], cand["body"], cand["legs"]):
            try:
                if eval(py_cond, {}, {"head": h, "body": b, "legs": l}):
                    matches.add((h, b, l))
            except Exception:
                pass
        if matches:
            results.setdefault(key_m.group(1), set()).update(matches)
    i = brace_close + 1

# Caso especial real (unico "else" sin condicion propia de todo el metodo, ver docstring):
# if ((body==24||body==229) && (legs==23||legs==212) &&
#     (head==42||head==41||head==43||head==254||head==257||head==256||head==255||head==258))
# { if (head==254||head==258) => HallowedSummoner; else => Hallowed; }
hallowed_heads = {42, 41, 43, 257, 256, 255}  # los 8 del exterior MENOS {254, 258}
for h, b, l in itertools.product(hallowed_heads, [24, 229], [23, 212]):
    results.setdefault("Hallowed", set()).add((h, b, l))

print(f"{len(results)} claves reales de ArmorSetBonus resueltas con al menos una combinacion")

# --- 3. Texto real en español ---
with open(GAME_JSON, encoding="utf-8") as f:
    game_json = json.load(f)
texts: dict[str, str] = game_json["ArmorSetBonus"]

# --- 4. Componer salida: itemId (de cualquier pieza) -> {key, text, pieces:[head,body,legs]} ---
out: dict[str, dict] = {}
unmapped_keys = []
for key, triples in results.items():
    text = texts.get(key)
    if text is None:
        unmapped_keys.append(key)
        continue
    for h, b, l in triples:
        head_id = slot_by_kind["head"].get(h)
        body_id = slot_by_kind["body"].get(b)
        legs_id = slot_by_kind["legs"].get(l)
        if head_id is None or body_id is None or legs_id is None:
            continue
        piece = {"key": key, "text": text, "pieces": [head_id, body_id, legs_id]}
        out[str(head_id)] = piece
        out[str(body_id)] = piece
        out[str(legs_id)] = piece

print(f"{len(out)} item ids reales con bonificacion de set mapeada")
if unmapped_keys:
    print("claves sin texto real en Game.json (revisar):", unmapped_keys)

with open(OUT, "w", encoding="utf-8") as f:
    json.dump(dict(sorted(out.items(), key=lambda kv: int(kv[0]))), f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
