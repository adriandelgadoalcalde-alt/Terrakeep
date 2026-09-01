"""
Extrae estadisticas reales por id de objeto vanilla directamente de los bloques
SetDefaults1..5 de Item.cs (decompilado real de tModLoader) - mismo criterio y mismo
alcance de bloque que extraer-categorias-vanilla.py (ver ese script para el detalle real
de como se localizan los bloques, incluido el bug de contaminacion por switches anidados
Y por metodos con VARIOS bloques "switch (type) { ... }" seguidos, encontrado y arreglado
el 2-sep-2026 - este script usaba el mismo split plano y por tanto tenia el mismo bug,
arreglado aqui con el mismo escaner de estados).

Solo se guardan los campos que de verdad aparecen asignados dentro del bloque de ESE id
concreto (no se rellena con 0 por defecto - un arma real con damage=0 explicito y un
objeto que ni siquiera es un arma son cosas distintas, y la UI debe poder distinguirlas
sin mostrar una estadistica falsa).
"""
import json
import re

SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Item.cs"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\vanilla_stats.json"

with open(SRC, encoding="utf-8") as f:
    text = f.read()

method_starts = [m.start() for m in re.finditer(r"public void SetDefaults\d\(int type\)", text)]
method_starts.append(len(text))
bodies = [text[method_starts[i]:method_starts[i + 1]] for i in range(len(method_starts) - 1)]

BACKSLASH = chr(92)
DQUOTE = chr(34)
SQUOTE = chr(39)
SWITCH_TYPE_RE = re.compile(r"switch\s*\(\s*type\s*\)\s*\{")
CASE_RE = re.compile(r"case\s+(\d+)\s*:")


def split_by_case(body_text: str) -> list[tuple[int, int]]:
    """Ver extraer-categorias-vanilla.py para el detalle completo - recorre TODOS los
    bloques "switch (type) { ... }" del metodo (puede haber varios seguidos), y dentro de
    cada uno solo cuenta como limite de item un "case N:" a profundidad 1 relativa a ESE
    switch, ignorando cadenas/chars/comentarios."""
    entries: list[tuple[int, int]] = []
    i, n = 0, len(body_text)
    in_switch = False
    depth = 0
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


NUM = r"(-?\d+(?:\.\d+)?)f?"

FIELDS = {
    "damage": rf"\bdamage\s*=\s*{NUM}\s*;",
    "defense": rf"\bdefense\s*=\s*{NUM}\s*;",
    "knockBack": rf"\bknockBack\s*=\s*{NUM}\s*;",
    "useTime": rf"\buseTime\s*=\s*{NUM}\s*;",
    "crit": rf"\bcrit\s*=\s*{NUM}\s*;",
    "mana": rf"\bmana\s*=\s*{NUM}\s*;",
    "healLife": rf"\bhealLife\s*=\s*{NUM}\s*;",
    "healMana": rf"\bhealMana\s*=\s*{NUM}\s*;",
    "rare": rf"\brare\s*=\s*{NUM}\s*;",
}

stats: dict[int, dict[str, float]] = {}

for body in bodies:
    case_entries = split_by_case(body)
    for idx, (item_id, block_start) in enumerate(case_entries):
        if item_id <= 0:
            continue
        block_end = case_entries[idx + 1][1] if idx + 1 < len(case_entries) else len(body)
        block = body[block_start:block_end]

        entry = {}
        for field, pattern in FIELDS.items():
            found = re.search(pattern, block)
            if found:
                value = float(found.group(1))
                entry[field] = int(value) if value.is_integer() else value
        if entry:
            stats[item_id] = entry

print(f"{len(stats)} objetos con al menos una estadistica real")
from collections import Counter
field_counts = Counter()
for entry in stats.values():
    field_counts.update(entry.keys())
for field, n in field_counts.most_common():
    print(f"  {field}: {n}")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump({str(k): v for k, v in sorted(stats.items())}, f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
