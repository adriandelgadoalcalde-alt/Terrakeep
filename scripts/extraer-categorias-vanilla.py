"""
Extrae una categoria real por id de objeto vanilla directamente de los bloques
SetDefaults1..5 de Item.cs (decompilado de tModLoader real, no adivinado) - mismo
criterio que ya se uso en el proyecto hermano para tile_names.json/map_colors.json
(mirar el codigo/datos reales en vez de inventar una taxonomia).

Los case son numericos ("case 1:", "case 2:"...) - coinciden 1:1 con GameItem.Id.
Para cada bloque, se decide una categoria (con jerarquia "/" al estilo del catalog.json
real de Calamity) mirando que campos se asignan de verdad dentro de ese bloque concreto.

Bug real encontrado y arreglado 2-sep-2026 (verificando el pedido explicito del usuario de
comprobar las estadisticas de armas contra la descompilacion real), en DOS pasadas:

1. La primera version partia el texto por CUALQUIER "case N:" en todo el archivo
   concatenado, sin respetar el anidamiento de llaves - un item con logica interna propia
   (ej. una animacion de color de particulas con su propio "switch(frame) { case 1: ... }"
   anidado DENTRO de su bloque) generaba "case N:" FALSOS que se confundian con limites de
   item real, sobrescribiendo la clasificacion correcta de items con id bajo (el caso mas
   visible: la Espada corta de hierro, id 1, con "melee = true" real, salia "Materiales"
   porque un "case 1:" anidado en la animacion de OTRO item mucho mas adelante ganaba por
   ser el ultimo en sobrescribir el diccionario).
2. Al arreglar el punto 1 contando profundidad de llaves relativa a un UNICO
   "switch (type) { ... }" por metodo, la cobertura se desplomo (2226 de ~5455) y objetos
   de sobra conocidos (Excalibur id 368, Terrarian id 3389) desaparecieron por completo.
   Motivo real: cada metodo `SetDefaults#` NO tiene un unico switch - tiene VARIOS bloques
   "switch (type) { ... }" seguidos uno detras de otro dentro del mismo metodo (confirmado
   leyendo el propio archivo: tras el "}" que cierra el primer switch de SetDefaults1,
   viene literalmente "switch (type) { case 122: ..." de nuevo). Buscar solo el PRIMER
   "switch(type){" con `re.search` (sin bucle) hacia que el escaneo se detuviera en el
   primer bloque y perdiera todos los que venian despues.

Arreglado de verdad recorriendo el metodo entero como un escaner de estados: fuera de un
switch, busca el siguiente "switch (type) {"; dentro, cuenta profundidad de llaves real
(ignorando cadenas/chars/comentarios, que si no tambien pueden desincronizar el conteo) y
solo trata un "case N:" a profundidad 1 relativa a ESE switch como limite de item; al cerrar
ese switch (profundidad vuelve a 0), sigue buscando el siguiente. Verificado: cobertura
1..500 completa (0 huecos), Excalibur (368) y Terrarian (3389) presentes, ~4575 ids unicos
reales en total (mucho mas cercano al ~5455 real que las dos versiones anteriores, que o
inventaban categorias por contaminacion o perdian la mayoria de los items).
"""
import json
import re

SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Item.cs"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets\vanilla_categories.json"

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
    """Devuelve [(item_id, posicion_justo_tras_el_':'), ...] recorriendo TODOS los bloques
    "switch (type) { ... }" del metodo (puede haber varios seguidos) - dentro de cada uno,
    solo cuenta como limite de item un "case N:" a profundidad 1 relativa a ESE switch,
    ignorando cadenas/chars/comentarios para no desincronizar el conteo de llaves."""
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


for body in bodies:
    case_entries = split_by_case(body)
    for idx, (item_id, block_start) in enumerate(case_entries):
        if item_id <= 0:
            continue
        # El bloque de este item va desde el final de su propio "case N:" hasta el final
        # del SIGUIENTE "case M:" (o hasta el final de este metodo si es el ultimo) -
        # incluye de mas los pocos caracteres literales "case M:" al final, pero eso es
        # inofensivo para classify(), que solo busca patrones tipo "melee = true" dentro
        # del texto, no depende de un recorte exacto del bloque.
        block_end = case_entries[idx + 1][1] if idx + 1 < len(case_entries) else len(body)
        block = body[block_start:block_end]
        categories[item_id] = classify(block)

print(f"{len(categories)} objetos clasificados (con case real a profundidad 1, sin contaminacion de switches anidados)")
from collections import Counter
counts = Counter(categories.values())
for cat, n in sorted(counts.items(), key=lambda kv: -kv[1]):
    print(f"  {cat}: {n}")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump({str(k): v for k, v in sorted(categories.items())}, f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
