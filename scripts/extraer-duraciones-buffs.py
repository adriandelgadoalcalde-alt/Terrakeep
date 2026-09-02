"""
Extrae la duracion REAL minima de cada buff (el buffTime real de su pocion/objeto base) -
pregunta a Opus sobre el diseño 2-sep-2026, cuarta pasada, rework de Buffs: "3 botones que
pongan duracion maxima media y minima... los minimos son los que dan por defecto las
pociones". Fuente real: pares buffType/buffTime dentro del MISMO bloque case de Item.cs
decompilado (mismo split_by_case ya usado por extraer-estadisticas-vanilla.py/
extraer-sets-armadura.py).

Investigado a fondo (agente en paralelo, ver bitacora.md): de 111 asignaciones buffType=
reales, solo el buff 257 (Suerte) lo dan VARIOS items con duraciones distintas (Pocion de la
suerte menor/normal/mayor = 18000/36000/54000 ticks, ratio real exacto 1:2:3) - el resto de
buffs con dato real tienen un UNICO buffTime. No hay ningun tope/maximo real de duracion en el
juego (Player.cs no tiene ninguna constante de ese tipo) - la duracion maxima real usada por
Terrasavr es S.getMaxTime() (ver reference/terrasavr-real/script.beautified.js:9166), NO un
dato de Item.cs, y se aplica en BuffDurationPresets.cs (Core), no aqui.
"""
import json
import re

SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Item.cs"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\vanilla_buff_durations.json"

with open(SRC, encoding="utf-8") as f:
    text = f.read()

method_starts = [m.start() for m in re.finditer(r"public void SetDefaults\d\(int type\)", text)]
method_starts.append(len(text))
bodies = [text[method_starts[i]:method_starts[i + 1]] for i in range(len(method_starts) - 1)]

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


BUFFTYPE_RE = re.compile(r"\bbuffType\s*=\s*(\d+)\s*;")
BUFFTIME_RE = re.compile(r"\bbuffTime\s*=\s*(\d+)\s*;")

# buffId -> lista de (itemId, buffTime) reales - varios si el mismo buff lo dan varios objetos
# (el unico caso real: Suerte).
by_buff: dict[int, list[tuple[int, int]]] = {}

for body in bodies:
    for item_id, block_start in split_by_case(body):
        if item_id <= 0:
            continue
        block_end = body.find("case ", block_start)
        block = body[block_start: block_end if block_end != -1 else len(body)]
        bt = BUFFTYPE_RE.search(block)
        btime = BUFFTIME_RE.search(block)
        if bt and btime:
            buff_id = int(bt.group(1))
            time = int(btime.group(1))
            by_buff.setdefault(buff_id, []).append((item_id, time))

print(f"{len(by_buff)} buffs reales con al menos un buffTime real (Item.cs)")

out = {}
for buff_id, sources in by_buff.items():
    times = sorted({t for _, t in sources})
    min_time = min(times)
    entry = {"min": min_time, "src": "item", "srcItem": next(i for i, t in sources if t == min_time)}
    if len(times) > 1:
        entry["tiers"] = times  # caso real de Suerte: [18000, 36000, 54000]
    out[str(buff_id)] = entry

multi = {k: v for k, v in out.items() if "tiers" in v}
print(f"buffs con varias duraciones reales (escalera): {multi}")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump(dict(sorted(out.items(), key=lambda kv: int(kv[0]))), f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
