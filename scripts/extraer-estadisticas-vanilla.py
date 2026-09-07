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

Hallazgo real (feedback directo del usuario, 5-sep-2026): "en la Solar [Flare] no se
muestra la defensa" - la Solar Flare Helmet (id 2763, headSlot=171) SI tiene
"defense = 24;" real en Item.cs, pero la version anterior de este script nunca la veia.
Causa raiz identica al bug #2 ya encontrado y arreglado en extraer-sets-armadura.py
(C-10a, ESPEC-pulido-final-libreria-inicio-apariencia.md#9.10): Item.cs tiene un patron
real "default: switch (type) { case 2763: ... } " dentro de una de las 5 SetDefaultsN -
un switch(type){...} ANIDADO dentro del default del switch principal, invisible a un
escaner de un solo nivel (cualquier headSlot alto, 157-171 y superiores, cae ahi). La
propia cabecera de este fichero YA afirmaba tener el arreglo "con el mismo escaner de
estados" - afirmacion equivocada, el escaner de dos niveles (in_switch/depth) de la
version anterior NO baja a un switch anidado DENTRO de un caso que ya esta procesando
(depth>1 en ese punto, CASE_RE solo mira depth==1). Portado aqui find_case_blocks/
extract_immediate_cases de extraer-sets-armadura.py (recursion real a cualquier
profundidad, ya verificada alli con el criterio de aceptacion duro de A9-05-SETVANILLA).
"""
import json
import re

SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Item.cs"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets\vanilla_stats.json"

with open(SRC, encoding="utf-8") as f:
    text = f.read()

BACKSLASH = chr(92)
DQUOTE = chr(34)
SQUOTE = chr(39)
SWITCH_TYPE_RE = re.compile(r"switch\s*\(\s*type\s*\)\s*\{")
CASE_RE = re.compile(r"case\s+(\d+)\s*:")
DEFAULT_RE = re.compile(r"default\s*:")


def find_matching_brace(body_text: str, open_idx: int) -> int:
    depth = 0
    i = open_idx
    while True:
        if body_text[i] == "{":
            depth += 1
        elif body_text[i] == "}":
            depth -= 1
            if depth == 0:
                return i
        i += 1


def find_case_blocks(body_text: str) -> list[tuple[int, str]]:
    """(item_id, block_text) para cada 'case N:' de CUALQUIER switch(type){...} anidado a
    cualquier profundidad dentro de `body_text` - mismo criterio real ya verificado en
    extraer-sets-armadura.py (find_case_blocks), portado aqui sin cambios de fondo."""
    results: list[tuple[int, str]] = []
    i, n = 0, len(body_text)
    while i < n:
        m = SWITCH_TYPE_RE.search(body_text, i)
        if not m:
            break
        switch_open = m.end() - 1
        switch_close = find_matching_brace(body_text, switch_open)
        results.extend(extract_immediate_cases(body_text, switch_open + 1, switch_close))
        i = switch_close + 1
    return results


def extract_immediate_cases(body_text: str, block_start: int, block_end: int) -> list[tuple[int, str]]:
    """Etiquetas case/default al nivel INMEDIATO (profundidad 0 relativa a block_start) de un
    unico switch, agrupando labels consecutivas sin codigo entre medias (fall-through real)
    bajo el MISMO bloque - y bajando de forma recursiva a cualquier switch(type){...} anidado
    dentro de cada bloque resuelto."""
    labels: list[tuple[int | None, int]] = []  # (item_id_o_None, posicion_tras_la_etiqueta)
    i, depth = block_start, 0
    while i < block_end:
        c = body_text[i]
        if c == "/" and i + 1 < block_end and body_text[i + 1] == "/":
            j = body_text.find("\n", i, block_end)
            i = (j + 1) if j != -1 else block_end
            continue
        if c == "/" and i + 1 < block_end and body_text[i + 1] == "*":
            j = body_text.find("*/", i + 2, block_end)
            i = (j + 2) if j != -1 else block_end
            continue
        if c == DQUOTE:
            i += 1
            while i < block_end and body_text[i] != DQUOTE:
                i += 2 if body_text[i] == BACKSLASH else 1
            i += 1
            continue
        if c == SQUOTE:
            i += 1
            while i < block_end and body_text[i] != SQUOTE:
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
            continue
        if depth == 0:
            m2 = CASE_RE.match(body_text, i)
            if m2:
                labels.append((int(m2.group(1)), m2.end()))
                i = m2.end()
                continue
            m3 = DEFAULT_RE.match(body_text, i)
            if m3:
                labels.append((None, m3.end()))
                i = m3.end()
                continue
        i += 1

    def label_text_of(idx: int) -> str:
        item_id, _ = labels[idx]
        return f"case {item_id}" if item_id is not None else "default"

    results: list[tuple[int, str]] = []
    k = 0
    while k < len(labels):
        group_ids: list[int] = []
        j = k
        while True:
            item_id, label_end = labels[j]
            if item_id is not None:
                group_ids.append(item_id)
            is_last = j + 1 >= len(labels)
            next_label_start = None if is_last else body_text.rfind(label_text_of(j + 1), label_end, labels[j + 1][1])
            gap = "" if is_last else body_text[label_end:next_label_start]
            if not is_last and gap.strip() == "":
                j += 1
                continue
            case_block_end = block_end if is_last else next_label_start
            block = body_text[label_end:case_block_end]
            break
        for item_id in group_ids:
            results.append((item_id, block))
        results.extend(find_case_blocks(block))
        k = j + 1
    return results


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

# Hallazgo real (mismo repaso, encontrado al verificar este arreglo contra la version
# anterior): delimitar cada SetDefaultsN "hasta la siguiente coincidencia" (o hasta EOF para
# la ultima) es incorrecto - el METODO real termina en su propia llave de cierre, bastante
# antes que el siguiente "public void SetDefaultsN" o el final real del fichero (SetDefaults5,
# la ultima, arrastraba ~12.500 lineas de metodos NO relacionados hasta EOF). Cualquier otro
# metodo con un switch sobre una variable local tambien llamada "type" en ese tramo de mas
# contaminaba resultados de ids reales (ej. Eggnog/1912, MouseCage/2191, Valor/3317 - todos con
# valores incorrectos hasta este arreglo, verificado comparando contra el bloque real de cada
# uno en Item.cs). Delimitar por llave de cierre real de cada metodo lo elimina de raiz.
method_starts = [m.start() for m in re.finditer(r"public void SetDefaults\d\(int type\)", text)]
bodies = []
for pos in method_starts:
    brace_open = text.index("{", pos)
    brace_close = find_matching_brace(text, brace_open)
    bodies.append(text[pos:brace_close + 1])

for body in bodies:
    for item_id, block in find_case_blocks(body):
        if item_id <= 0:
            continue
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
