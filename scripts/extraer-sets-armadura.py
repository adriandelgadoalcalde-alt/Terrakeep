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

Auditoria de pulido final (C-10a, ESPEC-pulido-final-libreria-inicio-apariencia.md#9.10): la
primera version de este script solo resolvia 43 de las 63 claves reales de ArmorSetBonus.* -
tres bugs reales, cada uno con su arreglo aqui:

  1. "if anidado saltado": tras procesar un bloque "if (COND) { ... }", el bucle saltaba a
     brace_close+1 SIN bajar a los if/else-if anidados dentro (el patron real de Cobalto/
     Mithril/Adamantita: un if "guardia" sin bono propio - if(body==17 && legs==16) - con un
     if/else-if POR CASCO dentro, cada uno con su propia clave). find_armor_set_conditions es
     ahora recursiva: si el primer ArmorSetBonus.* de un bloque aparece DESPUES de un if
     anidado (o no aparece en absoluto), baja al bloque componiendo la condicion del padre
     (AND) con la de cada if/else-if hijo por separado. Cierra las 10 claves de Cobalto/
     Mithril/Adamantita/Titanio.
  2. "inverso incompleto": split_by_case (ahora find_case_blocks) solo veia case labels al
     PRIMER nivel de un switch(type){...} - Item.cs tiene un patron real
     "default: switch (type) { case 2199: ... }" dentro de una de las 5 SetDefaultsN (un
     switch ANIDADO dentro del default del switch principal), invisible a un parser de un solo
     nivel. find_case_blocks ahora es recursiva: cualquier switch(type){...} anidado a
     cualquier profundidad se descubre y se procesa igual. Cierra 8 claves de casco alto
     (157-171 y superiores).
  3. "sets de 2 piezas": Wizard/MagicHat solo comparan head+body en Player.cs (nunca
     mencionan legs) - candidates_for ahora distingue "variable no referenciada" (None, la
     pieza es irrelevante) de "sin candidatos encontrados" (raro, lista vacia), y la salida
     compone pieces:[head,body] sin legs cuando corresponde. Cierra Wizard y MagicHat.

Criterio de aceptacion duro (ver A9-05-SETVANILLA, VanillaArmorSetKeysTests): el conjunto de
`key` distintas de la salida tiene que ser EXACTAMENTE el conjunto de `ArmorSetBonus.*` reales
de Player.cs (63 = 63, diferencia vacia en los dos sentidos) - no una impresion, un diff real.
"""
import itertools
import json
import re

import lang_vanilla

ITEM_SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Item.cs"
PLAYER_SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Player.cs"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\vanilla_armor_sets.json"

BACKSLASH, DQUOTE, SQUOTE = chr(92), chr(34), chr(39)


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


# --- 1. headSlot/bodySlot/legSlot por item id ---
with open(ITEM_SRC, encoding="utf-8") as f:
    item_text = f.read()

SWITCH_TYPE_RE = re.compile(r"switch\s*\(\s*type\s*\)\s*\{")
CASE_RE = re.compile(r"case\s+(\d+)\s*:")
DEFAULT_RE = re.compile(r"default\s*:")


def find_case_blocks(text: str) -> list[tuple[int, str]]:
    """(item_id, block_text) para cada 'case N:' de CUALQUIER switch(type){...} anidado a
    cualquier profundidad dentro de `text` - ver el modo 2 del docstring de arriba."""
    results: list[tuple[int, str]] = []
    i, n = 0, len(text)
    while i < n:
        m = SWITCH_TYPE_RE.search(text, i)
        if not m:
            break
        switch_open = m.end() - 1
        switch_close = find_matching_brace(text, switch_open)
        results.extend(extract_immediate_cases(text, switch_open + 1, switch_close))
        i = switch_close + 1
    return results


def extract_immediate_cases(text: str, body_start: int, body_end: int) -> list[tuple[int, str]]:
    """Etiquetas case/default al nivel INMEDIATO (profundidad 0 relativa a body_start) de un
    unico switch, agrupando labels consecutivas sin codigo entre medias (fall-through real,
    "case 2190:\ncase 2191:\n  codigo") bajo el MISMO bloque - y buscando de forma recursiva
    cualquier switch(type){...} anidado dentro de cada bloque resuelto."""
    labels: list[tuple[int | None, int]] = []  # (item_id_o_None, posicion_tras_la_etiqueta)
    i, depth = body_start, 0
    while i < body_end:
        c = text[i]
        if c == "/" and i + 1 < body_end and text[i + 1] == "/":
            j = text.find("\n", i, body_end)
            i = (j + 1) if j != -1 else body_end
            continue
        if c == "/" and i + 1 < body_end and text[i + 1] == "*":
            j = text.find("*/", i + 2, body_end)
            i = (j + 2) if j != -1 else body_end
            continue
        if c == DQUOTE:
            i += 1
            while i < body_end and text[i] != DQUOTE:
                i += 2 if text[i] == BACKSLASH else 1
            i += 1
            continue
        if c == SQUOTE:
            i += 1
            while i < body_end and text[i] != SQUOTE:
                i += 2 if text[i] == BACKSLASH else 1
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
            m2 = CASE_RE.match(text, i)
            if m2:
                labels.append((int(m2.group(1)), m2.end()))
                i = m2.end()
                continue
            m3 = DEFAULT_RE.match(text, i)
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
            # Hueco real entre esta etiqueta y la siguiente (localizando donde EMPIEZA la
            # siguiente, no donde termina) - vacio de verdad (solo espacios/saltos de linea) =
            # fall-through real, este grupo sigue creciendo con la siguiente etiqueta.
            next_label_start = None if is_last else text.rfind(label_text_of(j + 1), label_end, labels[j + 1][1])
            gap = "" if is_last else text[label_end:next_label_start]
            if not is_last and gap.strip() == "":
                j += 1
                continue
            block_end = body_end if is_last else next_label_start
            block = text[label_end:block_end]
            break
        for item_id in group_ids:
            results.append((item_id, block))
        results.extend(find_case_blocks(block))
        k = j + 1
    return results


SLOT_RE = {
    "head": re.compile(r"\bheadSlot\s*=\s*(-?\d+)\s*;"),
    "body": re.compile(r"\bbodySlot\s*=\s*(-?\d+)\s*;"),
    "legs": re.compile(r"\blegSlot\s*=\s*(-?\d+)\s*;"),
}
# slot_by_kind["head"][slotIndex] = itemId (inversion real, 1:1 verificada por Opus)
slot_by_kind: dict[str, dict[int, int]] = {"head": {}, "body": {}, "legs": {}}

# Hallazgo real (feedback directo del usuario, verificado al corregir extraer-estadisticas-
# vanilla.py el mismo dia): delimitar "hasta la siguiente coincidencia" (o EOF para la ultima)
# es incorrecto - SetDefaults5 (la ultima) arrastraba ~12.500 lineas de metodos NO
# relacionados hasta el final real del fichero, cualquiera con un switch sobre una variable
# local tambien llamada "type" contaminaba resultados. Delimitar cada metodo por su propia
# llave de cierre (find_matching_brace, ya definida arriba) lo elimina de raiz.
method_starts = [m.start() for m in re.finditer(r"public void SetDefaults\d\(int type\)", item_text)]
bodies = []
for pos in method_starts:
    brace_open = item_text.index("{", pos)
    brace_close = find_matching_brace(item_text, brace_open)
    bodies.append(item_text[pos:brace_close + 1])

for body in bodies:
    for item_id, block in find_case_blocks(body):
        if item_id <= 0:
            continue
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
method_end = find_matching_brace(player_text, open_brace) + 1
method_body = player_text[open_brace:method_end]

IF_RE = re.compile(r"if\s*\(")
KEY_RE = re.compile(r'ArmorSetBonus\.(\w+)')

results: dict[str, set[tuple[int | None, int | None, int | None]]] = {}  # key -> set of (head, body, legs) slot triples, None = pieza no exigida por esta clave


def candidates_for(cond: str) -> dict[str, list[int] | None]:
    """None = la variable NO aparece en absoluto en cond (pieza irrelevante para este bono,
    modo 3 - sets de 2 piezas); lista = valores candidatos reales encontrados en cond."""
    cand: dict[str, set[int]] = {"head": set(), "body": set(), "legs": set()}
    referenced: dict[str, bool] = {"head": False, "body": False, "legs": False}
    for var in cand:
        if re.search(rf"\b{var}\b", cond):
            referenced[var] = True
        for m in re.finditer(rf"\b{var}\s*(==|>=|<=|>|<)\s*(\d+)", cond):
            cand[var].add(int(m.group(2)))
        ge = re.search(rf"\b{var}\s*>=\s*(\d+)", cond)
        le = re.search(rf"\b{var}\s*<=\s*(\d+)", cond)
        if ge and le:
            for v in range(int(ge.group(1)), int(le.group(1)) + 1):
                cand[var].add(v)
    return {k: (sorted(v) if v else [-999]) if referenced[k] else None for k, v in cand.items()}


def to_python(cond: str) -> str:
    return cond.replace("&&", " and ").replace("||", " or ")


def find_key_at_depth0(block: str) -> re.Match | None:
    """Primer 'ArmorSetBonus.KEY' que aparece al nivel INMEDIATO de `block` (profundidad 0,
    fuera de cualquier llave anidada) - distingue el patron real "Nebula" (una asignacion suelta
    DESPUES de un if anidado sin relacion, `if (nebulaCD > 0) { nebulaCD--; } setBonus = ...`)
    del patron real "Cobalto/Mithril/Adamantita/Titanio" (el bono SOLO existe dentro de un
    if/else-if anidado, nunca suelto en el propio bloque) - comparar solo POSICIONES textuales
    (key antes/despues del primer if) no basta, Nebula tiene un if anidado ANTES de su bono
    directo y a la vez ningun bono dentro de ese if."""
    i, n, depth = 0, len(block), 0
    while i < n:
        c = block[i]
        if c == "/" and i + 1 < n and block[i + 1] == "/":
            j = block.find("\n", i)
            i = (j + 1) if j != -1 else n
            continue
        if c == "/" and i + 1 < n and block[i + 1] == "*":
            j = block.find("*/", i + 2)
            i = (j + 2) if j != -1 else n
            continue
        if c == DQUOTE:
            # "ArmorSetBonus.KEY" SIEMPRE vive dentro de un literal de cadena real
            # (Language.GetTextValue("ArmorSetBonus.KEY")) - comprobar el contenido nada mas
            # abrir la cadena (a depth 0), antes de saltarsela entera, o KEY_RE nunca la ve.
            if depth == 0:
                km = KEY_RE.match(block, i + 1)
                if km:
                    return km
            i += 1
            while i < n and block[i] != DQUOTE:
                i += 2 if block[i] == BACKSLASH else 1
            i += 1
            continue
        if c == SQUOTE:
            i += 1
            while i < n and block[i] != SQUOTE:
                i += 2 if block[i] == BACKSLASH else 1
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
        i += 1
    return None


def find_armor_set_conditions(text: str, parent_cond: str | None) -> None:
    """Recorre `text` buscando bloques if(...) { ... } - modo 1 del docstring de arriba: si el
    bloque tiene un ArmorSetBonus.* al nivel INMEDIATO (find_key_at_depth0, no dentro de un if
    anidado - vale igual si ese if anidado viene antes, el caso real "Nebula"), resuelve las
    candidatas cruzando su condicion con la de todos los padres (AND); si no tiene bono propio
    a ese nivel pero SI contiene mas ifs/else-if anidados (el patron real "guardia sin bono" de
    Cobalto/Mithril/Adamantita/Titanio), baja recursivamente componiendo la condicion."""
    i, n = 0, len(text)
    while i < n:
        m = IF_RE.search(text, i)
        if not m:
            break
        paren_open = m.end() - 1
        paren_close = find_matching_paren(text, paren_open)
        cond = text[paren_open + 1:paren_close]

        j = paren_close + 1
        while text[j] in " \t\r\n":
            j += 1
        if text[j] != "{":
            i = m.end()
            continue
        brace_close = find_matching_brace(text, j)
        block = text[j + 1:brace_close]

        key_m = find_key_at_depth0(block)
        combined_cond = cond if parent_cond is None else f"({parent_cond}) and ({cond})"

        if key_m is not None:
            if re.search(r"\bhead\b|\bbody\b|\blegs\b", combined_cond):
                cand = candidates_for(combined_cond)
                py_cond = to_python(combined_cond)
                head_vals = cand["head"] or [None]
                body_vals = cand["body"] or [None]
                legs_vals = cand["legs"] or [None]
                matches: set[tuple[int | None, int | None, int | None]] = set()
                for h, b, l in itertools.product(head_vals, body_vals, legs_vals):
                    env = {}
                    if h is not None:
                        env["head"] = h
                    if b is not None:
                        env["body"] = b
                    if l is not None:
                        env["legs"] = l
                    try:
                        if eval(py_cond, {}, env):
                            matches.add((h, b, l))
                    except Exception:
                        pass
                if matches:
                    results.setdefault(key_m.group(1), set()).update(matches)
        elif IF_RE.search(block) is not None:
            find_armor_set_conditions(block, combined_cond)
        i = brace_close + 1


find_armor_set_conditions(method_body, None)

# Caso especial real (unico "else" sin condicion propia de todo el metodo, ver docstring):
# if ((body==24||body==229) && (legs==23||legs==212) &&
#     (head==42||head==41||head==43||head==254||head==257||head==256||head==255||head==258))
# { if (head==254||head==258) => HallowedSummoner; else => Hallowed; }
hallowed_heads = {42, 41, 43, 257, 256, 255}  # los 8 del exterior MENOS {254, 258}
for h, b, l in itertools.product(hallowed_heads, [24, 229], [23, 212]):
    results.setdefault("Hallowed", set()).add((h, b, l))

print(f"{len(results)} claves reales de ArmorSetBonus resueltas con al menos una combinacion")

# --- 3. Texto real en español Y en ingles ---
# Ampliacion 6-sep-2026 (ronda de traduccion del CONTENIDO del juego): el bono de set se veia en
# español tambien con la interfaz en ingles. Misma clave real (ArmorSetBonus.X), mismo fichero,
# solo cambia el idioma - `text_en` sale de `en-US.Game.json`. Se lee con lang_vanilla porque los
# `en-US.*` traen comas finales reales que json.load rechaza (ver ese modulo).
texts: dict[str, str] = lang_vanilla.load("es-ES", "Game")["ArmorSetBonus"]
texts_en: dict[str, str] = lang_vanilla.load("en-US", "Game")["ArmorSetBonus"]

# --- 4. Componer salida: itemId (de cualquier pieza) -> {key, text, text_en, pieces} ---
# pieces solo incluye las piezas que la clave REALMENTE exige (modo 3: 2 piezas si legs es
# None, es decir, la condicion real de Player.cs nunca menciona legs para esta clave).
out: dict[str, dict] = {}
unmapped_keys = []
sin_ingles = []
for key, triples in results.items():
    text = texts.get(key)
    if text is None:
        unmapped_keys.append(key)
        continue
    text_en = texts_en.get(key)
    if text_en is None or str(text_en).strip() == "":
        # Sin texto ingles real -> no se inventa: la entrada se queda sin `text_en` y el
        # catalogo cae al español (idioma de referencia, ver LocalizedContent.Pick).
        sin_ingles.append(key)
        text_en = None
    for h, b, l in triples:
        head_id = slot_by_kind["head"].get(h) if h is not None else None
        body_id = slot_by_kind["body"].get(b) if b is not None else None
        legs_id = slot_by_kind["legs"].get(l) if l is not None else None
        if head_id is None or body_id is None:
            continue
        pieces = [head_id, body_id] if legs_id is None else [head_id, body_id, legs_id]
        piece = {"key": key, "text": text, "pieces": pieces}
        if text_en is not None:
            piece["text_en"] = text_en
        out[str(head_id)] = piece
        out[str(body_id)] = piece
        if legs_id is not None:
            out[str(legs_id)] = piece

print(f"{len(out)} item ids reales con bonificacion de set mapeada")
print(f"{len(results)} claves distintas resueltas (esperado 63, ArmorSetBonus.* reales de Player.cs)")
if unmapped_keys:
    print("claves sin texto real en Game.json (revisar):", unmapped_keys)
print(f"claves sin texto INGLES real (se quedan en español a proposito): {len(sin_ingles)}{' -> ' + ', '.join(sorted(sin_ingles)) if sin_ingles else ''}")
print(f"entradas con text_en real: {sum(1 for v in out.values() if 'text_en' in v)} de {len(out)}")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump(dict(sorted(out.items(), key=lambda kv: int(kv[0]))), f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
