"""
Extrae estadisticas reales por id de objeto vanilla directamente de los bloques
SetDefaults1..5 de Item.cs (decompilado real de tModLoader) - mismo criterio y mismo
alcance de bloque que extraer-categorias-vanilla.py (ver ese script para el detalle de
como se localizan los bloques "case <id>: ... break;").

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
full = "\n".join(bodies)

case_re = re.compile(r"case (\d+):")
matches = list(case_re.finditer(full))

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

for i, m in enumerate(matches):
    item_id = int(m.group(1))
    if item_id <= 0:
        continue
    block_start = m.end()
    block_end = matches[i + 1].start() if i + 1 < len(matches) else len(full)
    block = full[block_start:block_end]

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
