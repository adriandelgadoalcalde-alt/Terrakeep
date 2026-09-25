"""
Extrae el maxStack REAL por id de objeto vanilla directamente de los bloques SetDefaults1..5
de Item.cs (decompilado real de tModLoader) - mismo escaner de bloques (con recursion a
switch(type){...} anidados) ya verificado en extraer-estadisticas-vanilla.py.

Contexto real (encargo Keep 25-sep-2026, "+10/+100/MAX en el editor de objeto"): el limite de
"Cantidad" por maxStack real llevaba aparcado desde el 2-sep-2026 (ver el comentario de L-f en
ContainerViewModel.cs, "Ordenar") por falta de esta extraccion - nunca se llego a hacer. Este
script cierra ese hueco de datos real.

`Terraria.Item.maxStack` se resetea a 1 en cada ResetStats(Type) (Item.cs, "Defaults to 1" en el
propio XML doc del campo) y solo se reasigna en el bloque SetDefaultsN del id concreto cuando el
objeto de verdad apila mas de 1 unidad - por eso SOLO se guarda aqui lo que aparece asignado de
verdad (igual que vanilla_stats.json): ausencia de entrada = 1, el default real del motor, nunca
un numero inventado.

Medido en esta version real de Item.cs (tModLoader 1.4.4.9 decompilado, confirmado por grep antes
de escribir esto): solo existen 3 valores reales de "maxStack = N;" en todo el fichero -
CommonMaxStack (9999, la inmensa mayoria de materiales/municion/pociones desde la reforma de
apilado de Journey's End), 100 (las 3 monedas convertibles: Plata/Oro/Platino, antes de subir de
denominacion) y 1 (armas/armaduras/equipables, reasignado explicitamente en 20 casos aunque ya
es el default de ResetStats). CommonMaxStack es una CONSTANTE real (Item.cs:89,
"public static int CommonMaxStack = 9999;"), no un valor fijo inventado por este script.

Uso: python scripts/extraer-max-stack-vanilla.py
"""
import json
import re

SRC = r"C:\Users\adrian\Downloads\Keep\tModLoader-Decompiled\tModLoader\Terraria\Item.cs"
OUT = r"C:\Users\adrian\Downloads\Keep\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets\vanilla_max_stack.json"

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
    extraer-sets-armadura.py/extraer-estadisticas-vanilla.py, portado aqui sin cambios de fondo."""
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
    labels: list[tuple[int | None, int]] = []
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


COMMON_MAX_STACK = 9999  # Item.cs:89, "public static int CommonMaxStack = 9999;"
MAX_STACK_RE = re.compile(r"\bmaxStack\s*=\s*(CommonMaxStack|\d+)\s*;")

# Hallazgo real (verificado a mano en Item.cs antes de escribir esto, no supuesto): un id que
# llama a uno de estos 8 metodos auxiliares reales de Item.cs (DefaultToPlaceableTile,
# DefaultToPlaceableWall, DefaultToThrownWeapon, DefaultToFood, DefaultToHealingPotion,
# DefaultToSeaShell, DefaultToCapturedCritter, DefaultToSolution - los 8 confirmados con
# "maxStack = CommonMaxStack;" dentro de su propio cuerpo, de los ~24 DefaultTo* reales que
# existen) recibe maxStack=CommonMaxStack SIN que su propio bloque case lleve nunca la linea
# "maxStack = ..." explicita - un escaner que solo busca esa linea literal (como la primera
# version de este script) se queda ciego a los ~580 ids reales que usan este atajo, la inmensa
# mayoria bloques colocables añadidos en actualizaciones posteriores a 1.3 (ladrillos, variantes
# de pared...). Otros 3 auxiliares reales (DefaultToQuestFish/DefaultToGolfBall/DefaultTokite)
# tambien fijan maxStack, pero a 1 - el mismo default que ya aplica sin hacer nada, no hace falta
# listarlos aqui.
HELPER_9999_RE = re.compile(
    r"\b(?:DefaultToPlaceableTile|DefaultToPlaceableWall|DefaultToThrownWeapon|DefaultToFood"
    r"|DefaultToHealingPotion|DefaultToSeaShell|DefaultToCapturedCritter|DefaultToSolution)\s*\("
)

method_starts = [m.start() for m in re.finditer(r"public void SetDefaults\d\(int type\)", text)]
bodies = []
for pos in method_starts:
    brace_open = text.index("{", pos)
    brace_close = find_matching_brace(text, brace_open)
    bodies.append(text[pos:brace_close + 1])

max_stacks: dict[int, int] = {}
via_helper = 0
for body in bodies:
    for item_id, block in find_case_blocks(body):
        if item_id <= 0:
            continue
        found = MAX_STACK_RE.search(block)
        if found:
            raw = found.group(1)
            max_stacks[item_id] = COMMON_MAX_STACK if raw == "CommonMaxStack" else int(raw)
        elif HELPER_9999_RE.search(block):
            max_stacks[item_id] = COMMON_MAX_STACK
            via_helper += 1

print(f"{len(max_stacks)} objetos con maxStack explicito real ({via_helper} via metodo auxiliar sin linea propia)")
from collections import Counter
value_counts = Counter(max_stacks.values())
for value, n in value_counts.most_common():
    print(f"  maxStack={value}: {n}")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump({str(k): v for k, v in sorted(max_stacks.items())}, f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")

# Spot-check real: Musket Ball (id 14, verificado a mano arriba - "maxStack = CommonMaxStack;"
# dentro de case 14 de SetDefaults1) y Silver Coin (id 71, "maxStack = 100;").
print("Spot-check id 14 (Musket Ball):", max_stacks.get(14))
if max_stacks.get(14) != 9999:
    print("  AVISO: se esperaba 9999 (CommonMaxStack) - revisar el regex/indice.")
print("Spot-check id 71 (Silver Coin):", max_stacks.get(71))
if max_stacks.get(71) != 100:
    print("  AVISO: se esperaba 100 - revisar el regex/indice.")
# Spot-check real del camino nuevo (via helper): id 4041 (bloque de Nube, verificado a mano -
# "case 4041: ... DefaultToPlaceableTile((ushort)3, 0); ..." SIN linea "maxStack = " propia).
print("Spot-check id 4041 (via DefaultToPlaceableTile):", max_stacks.get(4041))
if max_stacks.get(4041) != 9999:
    print("  AVISO: se esperaba 9999 via helper - revisar HELPER_9999_RE.")
