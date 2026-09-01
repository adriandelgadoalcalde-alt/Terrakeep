"""
Extrae una categoria real por id de objeto vanilla directamente de los bloques
SetDefaults1..5 de Item.cs (decompilado de tModLoader real, no adivinado) - mismo
criterio que ya se uso en el proyecto hermano para tile_names.json/map_colors.json
(mirar el codigo/datos reales en vez de inventar una taxonomia).

Los case son numericos ("case 1:", "case 2:"...) - coinciden 1:1 con GameItem.Id.
Para cada bloque, se decide una categoria (con jerarquia "/" al estilo del catalog.json
real de Calamity) mirando que campos se asignan de verdad dentro de ese bloque concreto.
"""
import json
import re

SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Item.cs"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\vanilla_categories.json"

with open(SRC, encoding="utf-8") as f:
    text = f.read()

# Concatena los cuerpos de los 5 metodos SetDefaults#, en orden.
method_starts = [m.start() for m in re.finditer(r"public void SetDefaults\d\(int type\)", text)]
method_starts.append(len(text))
bodies = []
for i in range(len(method_starts) - 1):
    bodies.append(text[method_starts[i]:method_starts[i + 1]])
full = "\n".join(bodies)

# Divide en bloques por "case <numero>:" - el bloque abarca hasta el siguiente case/default.
case_re = re.compile(r"case (\d+):")
matches = list(case_re.finditer(full))

categories: dict[int, str] = {}

def classify(block: str) -> str:
    has = lambda pat: re.search(pat, block) is not None

    is_vanity = has(r"\bvanity\s*=\s*true")
    has_head = has(r"\bheadSlot\s*=\s*(?!-1\b)")
    has_body = has(r"\bbodySlot\s*=\s*(?!-1\b)")
    has_leg = has(r"\blegSlot\s*=\s*(?!-1\b)")
    has_wing = has(r"\bwingSlot\s*=\s*(?!-1\b)")
    has_accessory = has(r"\baccessory\s*=\s*true")
    has_hairdye = has(r"\bhairDye\s*=\s*(?!-1\b)")
    has_dye = has(r"(?<!hair)\bdye\s*=\s*(?!-1\b)") and not has_hairdye
    has_ammo = has(r"\bammo\s*=\s*(?!0\b|AmmoID\.None\b)")
    has_mount = has(r"\bmountType\s*=\s*(?!-1\b)")
    has_melee = has(r"\bmelee\s*=\s*true")
    has_ranged = has(r"\branged\s*=\s*true")
    has_magic = has(r"\bmagic\s*=\s*true")
    has_summon = has(r"\bsummon\s*=\s*true")
    has_sentry = has(r"\bsentry\s*=\s*true")
    has_createtile = has(r"\bcreateTile\s*=\s*(?!-1\b)")
    has_createwall = has(r"\bcreateWall\s*=\s*(?!-1\b)")
    has_fishingpole = has(r"\bfishingPole\s*=\s*(?!0\b)")
    has_bait = has(r"\bbait\s*=\s*(?!0\b)")
    has_pick = has(r"\bpick\s*=\s*(?!0\b)")
    has_axe = has(r"\baxe\s*=\s*(?!0\b)")
    has_hammer = has(r"\bhammer\s*=\s*(?!0\b)")
    has_potion = has(r"\bpotion\s*=\s*true")
    has_food = has(r"\bfoodType\s*=\s*(?!-1\b)") or has(r"\bbuffType\s*=\s*BuffID\.WellFed")
    has_paint = has(r"\bpaint\s*=\s*(?!0\b)")

    if has_hairdye:
        return "Tintes/Pelo"
    if has_dye:
        return "Tintes"
    if has_head or has_body or has_leg:
        return "Armadura/Vanidad" if is_vanity else "Armadura"
    if has_wing:
        return "Accesorios/Alas"
    if has_accessory:
        return "Accesorios/Vanidad" if is_vanity else "Accesorios"
    if has_melee:
        return "Armas/Cuerpo a cuerpo"
    if has_ranged:
        return "Armas/A distancia"
    if has_magic:
        return "Armas/Magia"
    if has_summon or has_sentry:
        return "Armas/Invocacion"
    if has_ammo:
        return "Municion"
    if has_mount:
        return "Monturas"
    if has_fishingpole:
        return "Pesca/Cañas"
    if has_bait:
        return "Pesca/Cebos"
    if has_potion:
        return "Pociones"
    if has_food:
        return "Pociones/Comida"
    if has_paint:
        return "Pintura"
    if has_pick or has_axe or has_hammer:
        return "Herramientas"
    if has_createtile or has_createwall:
        return "Colocables"
    return "Materiales"


for i, m in enumerate(matches):
    item_id = int(m.group(1))
    if item_id <= 0:
        continue
    block_start = m.end()
    block_end = matches[i + 1].start() if i + 1 < len(matches) else len(full)
    block = full[block_start:block_end]
    categories[item_id] = classify(block)

print(f"{len(categories)} objetos clasificados")
from collections import Counter
counts = Counter(categories.values())
for cat, n in sorted(counts.items(), key=lambda kv: -kv[1]):
    print(f"  {cat}: {n}")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump({str(k): v for k, v in sorted(categories.items())}, f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
