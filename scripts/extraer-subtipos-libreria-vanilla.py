r"""
Deriva el SUBTIPO REAL de cada objeto vanilla (arma, herramienta, equipo, colocable, pared)
a partir del codigo decompilado real de Terraria 1.4.5.8, y lo escribe en
`Terrakeep.App/Assets/vanilla_library_subtypes.json`.

PARA QUE (pedido del usuario, 8-sep-2026, con captura): dentro de la rama raiz "Categorias" de
la Libreria, cada carpeta hoja ("Daño a Distancia", "Colocable"...) se partia en "Pagina 1",
"Pagina 2"... de 40 en 40 **por orden de id**, sin ningun criterio de tipo. Cada pagina enseña
como icono el PRIMER objeto de su lista, asi que parecia que la pagina representaba un tipo de
arma concreto (un arco, un pico) cuando dentro habia de todo: *"da igual que pagina clickes,
despues no esta ordenado; dentro de picos te encuentras espadas"*. Es el comportamiento fiel del
Terrasavr original (`Hc.deploy`, tope real de 40 por hoja), no una regresion - lo que se pide es
ir mas alla: que cada carpeta agrupe objetos del MISMO tipo real y se llame por ese tipo.

EL CRITERIO SALE SIEMPRE DE UN DATO REAL DEL JUEGO, NUNCA DE UNA LISTA A MANO. Las fuentes son:

  * `Item.cs` (SetDefaults1..5): `useStyle`, `useAmmo`, `ammo`, `shoot`, `pick`/`axe`/`hammer`,
    `createTile`/`createWall`, `headSlot`/`bodySlot`/`legSlot`, los doce slots de accesorio
    (`wingSlot`, `shoeSlot`, `balloonSlot`...), `dye`/`hairDye`, `vanity`, `accessory`...
  * `Projectile.cs`: el `aiStyle` REAL del proyectil que dispara el arma. Es lo que separa de
    verdad una lanza de un mayal o de un bumeran (`ProjAIStyleID`: 3 Boomerang, 15 Flail,
    19 Spear, 20 Drill, 99 Yoyo, 161 ShortSword, 141 PoleSmash...).
  * `ItemID.Sets`: `Yoyo`, `IsDrill`, `IsChainsaw`, `Torches`, `Campfires`, `Glowsticks`.
  * `GameContent/Prefixes/PrefixLegacy.cs` (`ItemSets`): `SwordsHammersAxesPicks` y `GunsBows`,
    la propia clasificacion del juego por tipo de arma para repartir prefijos.
  * `TileID.Sets`: `Paintings`, `Platforms`, `BasicChest`, `BasicChestFake`, `BasicDresser`.
  * `Main.cs`: `tileFrameImportant` (un tile "con marco" es un OBJETO/mueble; uno sin marco es
    un bloque) y `tileSolid`.
  * `Terrakeep.App/Assets/tile_names.json` para el nombre real del tile que coloca un objeto
    (mismo dato del Explorador del Mundo, sacado de TEdit).

Lo que no tiene un campo o un set real y univoco NO se fuerza: cae en un grupo residual con
nombre honesto ("Other melee weapons", "Other placeables"...). Preferimos un "Otros" pequeño a
una clasificacion inventada.

Reutiliza el escaner de `Item.cs` ya verificado en `generar-mejor-prefijo.py` (bloques `case N:`
con herencia del nivel superior, switches anidados a cualquier profundidad, bloques
`if (type >= A && type <= B)` que definen objetos enteros, y la redireccion real
`SetDefaults3(2772); type = 3462;`). Se importa de alli en vez de copiarlo: es el mismo escaner,
ya auditado, y duplicarlo seria pedir que las dos copias se separen con el tiempo.

Uso: python scripts/extraer-subtipos-libreria-vanilla.py
Salida: Terrakeep.App/Assets/vanilla_library_subtypes.json  (lo consume
        scripts/extraer-arbol-libreria-vanilla.js al reconstruir el arbol)
"""
import importlib.util
import io
import json
import os
import re
import sys
from collections import Counter, defaultdict

RAIZ = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\TerrariaVanilla\Terraria"
SCRIPTS = os.path.dirname(os.path.abspath(__file__))
ASSETS = os.path.join(os.path.dirname(SCRIPTS), "Terrakeep.App", "Assets")
OUT = os.path.join(ASSETS, "vanilla_library_subtypes.json")

if sys.stdout.encoding and sys.stdout.encoding.lower() != "utf-8":
    sys.stdout.reconfigure(encoding="utf-8")


def _cargar_escaner():
    """Importa `generar-mejor-prefijo.py` como modulo (el guion del nombre impide un `import`
    normal). Solo se usan sus utilidades de parseo; su `main()` esta bajo `if __name__`."""
    ruta = os.path.join(SCRIPTS, "generar-mejor-prefijo.py")
    spec = importlib.util.spec_from_file_location("mejor_prefijo", ruta)
    modulo = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(modulo)
    return modulo


mp = _cargar_escaner()


def leer(*partes):
    with io.open(os.path.join(RAIZ, *partes), encoding="utf-8") as f:
        return f.read()


ITEM_TXT = leer("Item.cs")
PROJ_TXT = leer("Projectile.cs")
MAIN_TXT = leer("Main.cs")
ITEMID_TXT = leer("ID", "ItemID.cs")
TILEID_TXT = leer("ID", "TileID.cs")
AMMOID_TXT = leer("ID", "AmmoID.cs")
PREFIX_TXT = leer("GameContent", "Prefixes", "PrefixLegacy.cs")


# ------------------------------------------------------------------ constantes reales por id
def constantes(texto, patron):
    return {m.group(1): int(m.group(2)) for m in re.finditer(patron, texto)}


AMMO = constantes(AMMOID_TXT, r"public static int (\w+) = (\d+);")
TILE = constantes(TILEID_TXT, r"public const (?:ushort|short|int) (\w+) = (\d+);")
CONST_TABLAS = {"AmmoID": AMMO, "TileID": TILE}


def bool_set(texto, nombre):
    """Ids de un `Factory.CreateBoolSet(...)` real. El primer argumento puede ser el valor por
    defecto (`true`/`false`) y no un id."""
    m = re.search(r"public static bool\[\] " + nombre + r" = Factory\.CreateBoolSet\(([^;]*)\);", texto)
    if not m:
        raise SystemExit(f"No se encontro el set real '{nombre}' - ¿cambio el decompilado?")
    args = [a.strip() for a in m.group(1).split(",")]
    if args and args[0] in ("true", "false"):
        args = args[1:]
    return {int(a) for a in args if a.isdigit()}


SET_YOYO = bool_set(ITEMID_TXT, "Yoyo")
SET_DRILL = bool_set(ITEMID_TXT, "IsDrill")
SET_CHAINSAW = bool_set(ITEMID_TXT, "IsChainsaw")
SET_TORCHES = bool_set(ITEMID_TXT, "Torches")
SET_CAMPFIRES = bool_set(ITEMID_TXT, "Campfires")
SET_GLOWSTICKS = bool_set(ITEMID_TXT, "Glowsticks")
SET_ESPADAS = bool_set(PREFIX_TXT, "SwordsHammersAxesPicks")
SET_ARMAS_FUEGO_ARCOS = bool_set(PREFIX_TXT, "GunsBows")
TILES_CUADROS = bool_set(TILEID_TXT, "Paintings")
TILES_PLATAFORMAS = bool_set(TILEID_TXT, "Platforms")
TILES_COFRES = (bool_set(TILEID_TXT, "BasicChest") | bool_set(TILEID_TXT, "BasicChestFake")
                | bool_set(TILEID_TXT, "BasicDresser"))


# --------------------------------------------------- Main.tileFrameImportant / Main.tileSolid
FOR_RANGO_RE = re.compile(r"for \((?:ushort|int|short) (\w+) = (\d+); \1 <=? (\d+); \1\+\+\)\s*\n\s*\{")


def arrays_de_tile(nombres):
    """Valor final real de arrays como `Main.tileFrameImportant[]`/`Main.tileSolid[]`, que
    `Main.Initialize` rellena con asignaciones sueltas y con unos pocos bucles `for` de rango
    literal. Se recorre en orden textual: la ultima asignacion de un tile manda."""
    salida = {n: {} for n in nombres}
    # 1) bucles con rango literal: lo que asignen dentro vale para todo el rango
    for m in FOR_RANGO_RE.finditer(MAIN_TXT):
        var, desde, hasta = m.group(1), int(m.group(2)), int(m.group(3))
        abre = MAIN_TXT.index("{", m.end() - 1)
        cuerpo = MAIN_TXT[abre:mp.find_matching_brace(MAIN_TXT, abre)]
        for nombre in nombres:
            mm = re.search(rf"\b{nombre}\[{var}\] = (true|false);", cuerpo)
            if mm and hasta - desde < 64:
                for t in range(desde, hasta + 1):
                    salida[nombre][(m.start(), t)] = mm.group(1) == "true"
    # 2) asignaciones literales
    for nombre in nombres:
        for m in re.finditer(rf"\b{nombre}\[(\d+)\] = (true|false);", MAIN_TXT):
            salida[nombre][(m.start(), int(m.group(1)))] = m.group(2) == "true"
    # `AddEchoFurnitureTile(N)` pone tileFrameImportant[N] = true (helper real de Main.cs)
    if "tileFrameImportant" in nombres:
        for m in re.finditer(r"AddEchoFurnitureTile\((\d+)\);", MAIN_TXT):
            salida["tileFrameImportant"][(m.start(), int(m.group(1)))] = True
    final = {}
    for nombre, eventos in salida.items():
        acc = {}
        for (_, tile), valor in sorted(eventos.items()):
            acc[tile] = valor
        final[nombre] = {t for t, v in acc.items() if v}
    return final


_arrays = arrays_de_tile(["tileFrameImportant", "tileSolid"])
TILES_CON_MARCO = _arrays["tileFrameImportant"]
TILES_SOLIDOS = _arrays["tileSolid"]


# ------------------------------------------------------------------- aiStyle real por proyectil
def ai_style_por_proyectil():
    """`Projectile.SetDefaults` es una cadena real de `if (type == N) { ... }` (no un switch), y
    varios proyectiles fijan su aiStyle llamando a un helper (`DefaultToFlail()`,
    `DefaultToYoyo()`, `DefaultToShortsword()`...). Sin resolver los helpers se pierden justo los
    tipos que mas importan aqui: sin ellos los yoyos salen con aiStyle desconocido."""
    helpers = {}
    for m in re.finditer(r"public void (DefaultTo\w+)\(\)\s*\n\s*\{", PROJ_TXT):
        abre = PROJ_TXT.index("{", m.end() - 1)
        cuerpo = PROJ_TXT[abre:mp.find_matching_brace(PROJ_TXT, abre)]
        mm = re.search(r"\baiStyle = (\d+);", cuerpo)
        if mm:
            helpers[m.group(1)] = int(mm.group(1))

    inicio = PROJ_TXT.index("public void SetDefaults(int Type)")
    abre = PROJ_TXT.index("{", inicio)
    cuerpo = PROJ_TXT[abre:mp.find_matching_brace(PROJ_TXT, abre) + 1]

    estilos = {}
    for m in re.finditer(r"(?:else )?if \(([^)]*type == \d+[^)]*)\)\s*\n\s*\{", cuerpo):
        ids = [int(x) for x in re.findall(r"type == (\d+)", m.group(1))]
        o = cuerpo.index("{", m.end() - 1)
        bloque = cuerpo[o:mp.find_matching_brace(cuerpo, o)]
        mm = re.search(r"\baiStyle = (\d+);", bloque)
        valor = int(mm.group(1)) if mm else None
        if valor is None:
            for h, ai in helpers.items():
                if re.search(rf"\b{h}\(\);", bloque):
                    valor = ai
                    break
        if valor is not None:
            for pid in ids:
                estilos.setdefault(pid, valor)
    return estilos


AISTYLE = ai_style_por_proyectil()

# ProjAIStyleID real (ver Terraria/ID/ProjAIStyleID.cs del arbol de tModLoader, mismos numeros).
AI_BOOMERANG, AI_FLAIL, AI_HARPOON, AI_SPEAR, AI_DRILL = 3, 15, 13, 19, 20
AI_YOYO, AI_SHORTSWORD, AI_POLESMASH, AI_FLAIRON = 99, 161, 141, 69


# ------------------------------------------------------------------------ campos reales por id
CAMPOS_INT = [
    "useStyle", "shoot", "pick", "axe", "hammer", "damage", "mana", "defense",
    "createTile", "createWall", "placeStyle", "headSlot", "bodySlot", "legSlot",
    "wingSlot", "shoeSlot", "handOnSlot", "handOffSlot", "backSlot", "frontSlot",
    "neckSlot", "faceSlot", "balloonSlot", "waistSlot", "shieldSlot", "beardSlot",
    "dye", "hairDye", "fishingPole", "bait", "buffType", "mountType", "tileWand",
]
CAMPOS_CONST = ["useAmmo", "ammo"]
CAMPOS_BOOL = ["melee", "ranged", "magic", "summon", "consumable", "noMelee", "channel",
               "accessory", "vanity", "material", "notAmmo"]

NUM = r"(-?\d+)"
RE_INT = {c: re.compile(rf"\b{c} = {NUM}\s*;") for c in CAMPOS_INT}
RE_CONST = {c: re.compile(rf"\b{c} = (?:(\w+)\.(\w+)|{NUM})\s*;") for c in CAMPOS_CONST}
RE_BOOL = {c: re.compile(rf"\b{c} = (true|false)\s*;") for c in CAMPOS_BOOL}
# `createTile = TileID.Statue;` y `createTile = (ushort)91;` son formas reales del fichero.
RE_TILE_CONST = re.compile(r"\b(createTile|createWall) = (?:\(ushort\)\s*)?(?:TileID|WallID)\.(\w+)\s*;")
RE_CAST_NUM = re.compile(r"\b(createTile|createWall) = \(ushort\)\s*(\d+)\s*;")


def helpers_de_item():
    """Tabla automatica `DefaultToX(args) -> campos que fija`, leida del CUERPO REAL de cada
    helper de `Item.cs` (no escrita a mano: son 30 y varios se llaman entre ellos, por ejemplo
    `DefaultToBanner` -> `DefaultToPlaceableTile(91, estilo)` -> `createTile = 91`).

    Cada campo queda como ('lit', valor) o ('arg', indice, valor_por_defecto)."""
    firmas = {}
    for m in re.finditer(r"(?:public|private|internal) void (DefaultTo\w+)\(([^)]*)\)\s*\n\s*\{", ITEM_TXT):
        nombre, params = m.group(1), m.group(2)
        abre = ITEM_TXT.index("{", m.end() - 1)
        cuerpo = ITEM_TXT[abre:mp.find_matching_brace(ITEM_TXT, abre)]
        nombres, defectos = [], []
        for p in mp.split_args(params):
            if not p:
                continue
            trozos = p.split("=")
            nombres.append(trozos[0].split()[-1].strip())
            defectos.append(mp.numeric(trozos[1]) if len(trozos) > 1 else None)
        # Una sobrecarga que solo reenvia a otra (DefaultToPlaceableTile(int) -> (ushort)) se
        # queda con la de mas cuerpo: nos vale la primera que fije campos de verdad.
        firmas.setdefault(nombre, []).append((nombres, defectos, cuerpo))

    def resolver(nombre, visitados):
        if nombre in visitados:
            return []
        visitados = visitados | {nombre}
        mejor = []
        for nombres, defectos, cuerpo in firmas.get(nombre, []):
            efectos = []
            for campo, rx in list(RE_INT.items()) + [(c, RE_BOOL[c]) for c in CAMPOS_BOOL]:
                for mm in rx.finditer(cuerpo):
                    valor = mm.group(1)
                    efectos.append((mm.start(), campo, ("lit", valor == "true" if valor in ("true", "false") else int(valor))))
            for mm in RE_TILE_CONST.finditer(cuerpo):
                if mm.group(2) in TILE:
                    efectos.append((mm.start(), mm.group(1), ("lit", TILE[mm.group(2)])))
            for mm in RE_CAST_NUM.finditer(cuerpo):
                efectos.append((mm.start(), mm.group(1), ("lit", int(mm.group(2)))))
            for campo, rx in RE_CONST.items():
                for mm in rx.finditer(cuerpo):
                    if mm.group(1):
                        tabla = CONST_TABLAS.get(mm.group(1))
                        if tabla and mm.group(2) in tabla:
                            efectos.append((mm.start(), campo, ("lit", tabla[mm.group(2)])))
                    else:
                        efectos.append((mm.start(), campo, ("lit", int(mm.group(3)))))
            # asignaciones campo = parametro
            for i, p in enumerate(nombres):
                for campo in CAMPOS_INT + CAMPOS_CONST:
                    for mm in re.finditer(rf"\b{campo} = (?:\(ushort\)\s*)?{p}\s*;", cuerpo):
                        efectos.append((mm.start(), campo, ("arg", i, defectos[i])))
            # llamadas a otros helpers, con sus argumentos reenviados
            for mm in re.finditer(r"\b(DefaultTo\w+)\(([^;]*)\);", cuerpo):
                args = mp.split_args(mm.group(2))
                for campo, efecto in resolver(mm.group(1), visitados):
                    if efecto[0] == "lit":
                        efectos.append((mm.start(), campo, efecto))
                        continue
                    idx, defecto = efecto[1], efecto[2]
                    if idx >= len(args):
                        if defecto is not None:
                            efectos.append((mm.start(), campo, ("lit", int(defecto))))
                        continue
                    tok = args[idx]
                    v = mp.numeric(tok.replace("(ushort)", "").strip())
                    if v is not None:
                        efectos.append((mm.start(), campo, ("lit", int(v))))
                        continue
                    mc = re.fullmatch(r"(\w+)\.(\w+)", tok.strip())
                    if mc and mc.group(1) in CONST_TABLAS and mc.group(2) in CONST_TABLAS[mc.group(1)]:
                        efectos.append((mm.start(), campo, ("lit", CONST_TABLAS[mc.group(1)][mc.group(2)])))
                        continue
                    if tok.strip() in nombres:
                        efectos.append((mm.start(), campo, ("arg", nombres.index(tok.strip()), None)))
            efectos.sort()
            resumen = [(campo, efecto) for _, campo, efecto in efectos]
            if len(resumen) > len(mejor):
                mejor = resumen
        return mejor

    return {nombre: resolver(nombre, frozenset()) for nombre in firmas}


HELPERS = helpers_de_item()
RE_LLAMADA_HELPER = re.compile(r"\b(DefaultTo\w+)\(([^;]*)\)\s*;")


_CAMPOS_LINEALES = "|".join(CAMPOS_INT)
# Dos formas reales de la misma cuenta: `createTile = 262 + type - 1970;` y
# `headSlot = type + 146 - 2104;` (las mascaras de jefe). Un grupo de ids consecutivos que
# ocupa valores consecutivos del campo.
RE_LINEAL_A = re.compile(rf"\b({_CAMPOS_LINEALES}) = (\d+) \+ \(?type - (\d+)\)?\s*;")
RE_LINEAL_B = re.compile(rf"\b({_CAMPOS_LINEALES}) = type \+ (\d+) - (\d+)\s*;")


def aplicar(entrada, bloque, iid=None):
    """Aplica a `entrada` las asignaciones reales del bloque EN ORDEN TEXTUAL (un helper puede
    poner un valor y la linea siguiente pisarlo, y al reves)."""
    eventos = []
    for campo, rx in RE_INT.items():
        for m in rx.finditer(bloque):
            eventos.append((m.start(), campo, int(m.group(1))))
    # Sin resolver estas dos formas se quedaban sin clasificar 120 colocables reales (bloques
    # Gemspark, jaulas de bicho, bloques de fragmento, de equipo...) y las 12 mascaras de jefe.
    if iid is not None:
        for m in RE_LINEAL_A.finditer(bloque):
            eventos.append((m.start(), m.group(1), int(m.group(2)) + iid - int(m.group(3))))
        for m in RE_LINEAL_B.finditer(bloque):
            eventos.append((m.start(), m.group(1), iid + int(m.group(2)) - int(m.group(3))))
    for m in RE_TILE_CONST.finditer(bloque):
        if m.group(2) in TILE:
            eventos.append((m.start(), m.group(1), TILE[m.group(2)]))
    for m in RE_CAST_NUM.finditer(bloque):
        eventos.append((m.start(), m.group(1), int(m.group(2))))
    for campo, rx in RE_CONST.items():
        for m in rx.finditer(bloque):
            if m.group(1):
                tabla = CONST_TABLAS.get(m.group(1))
                if tabla and m.group(2) in tabla:
                    eventos.append((m.start(), campo, tabla[m.group(2)]))
            else:
                eventos.append((m.start(), campo, int(m.group(3))))
    for campo, rx in RE_BOOL.items():
        for m in rx.finditer(bloque):
            eventos.append((m.start(), campo, m.group(1) == "true"))
    for m in RE_LLAMADA_HELPER.finditer(bloque):
        args = mp.split_args(m.group(2))
        if m.group(1) == "DefaultToInfoAccessory":
            # Los accesorios INFORMATIVOS (brujula, GPS, medidor de profundidad, los trozos del
            # movil...) tienen su propio helper real en Item.cs; es la unica forma de separarlos
            # de los demas accesorios sin ranura visual.
            eventos.append((m.start(), "infoAccessory", True))
        for campo, efecto in HELPERS.get(m.group(1), []):
            if efecto[0] == "lit":
                eventos.append((m.start(), campo, efecto[1]))
                continue
            idx, defecto = efecto[1], efecto[2]
            if idx >= len(args):
                if defecto is not None:
                    eventos.append((m.start(), campo, int(defecto)))
                continue
            tok = args[idx].replace("(ushort)", "").strip()
            v = mp.numeric(tok)
            if v is not None:
                eventos.append((m.start(), campo, int(v)))
                continue
            mc = re.fullmatch(r"(\w+)\.(\w+)", tok)
            if mc and mc.group(1) in CONST_TABLAS and mc.group(2) in CONST_TABLAS[mc.group(1)]:
                eventos.append((m.start(), campo, CONST_TABLAS[mc.group(1)][mc.group(2)]))
    eventos.sort(key=lambda e: e[0])
    for _, campo, valor in eventos:
        entrada[campo] = valor
    return entrada


def ids_de_condicion_ancha(cond):
    """Como `ids_de_condicion` de `generar-mejor-prefijo.py`, pero admitiendo rangos mas largos.
    Alli el tope de 64 ids es una precaucion porque ese bloque se HEREDA como contexto de un
    switch posterior; aqui solo se leen los campos del bloque para esos ids concretos, y hay
    rangos reales mas largos que si definen objetos enteros (ej. `if (type >= 1970 && type <=
    2000)`)."""
    cond = " ".join(cond.split())
    m = mp.IF_RANGE_RE.match(cond)
    if m:
        a, b = int(m.group(1)), int(m.group(2))
        return list(range(a, b + 1)) if 0 < b - a < 600 else None
    if mp.IF_EQ_LIST_RE.match(cond):
        return [int(x) for x in re.findall(r"\d+", cond)]
    return None


def campos_reales():
    campos = {}
    # capa 1: `if (type >= A && type <= B) { ... }` - unica definicion de algunos objetos
    for m in mp.SETDEFAULTS_RE.finditer(ITEM_TXT):
        abre = ITEM_TXT.index("{", m.start())
        fin = mp.find_matching_brace(ITEM_TXT, abre)
        pos = abre
        while True:
            mi = mp.IF_TYPE_RE.search(ITEM_TXT, pos, fin)
            if not mi:
                break
            o = mi.end() - 1
            c = mp.find_matching_brace(ITEM_TXT, o)
            ids = ids_de_condicion_ancha(mi.group(1))
            if ids:
                bloque = mp.strip_variant_blocks(ITEM_TXT[o:c + 1])
                for iid in ids:
                    aplicar(campos.setdefault(iid, {}), bloque, iid)
            pos = mi.end()
    # capa 2: los `case` (mandan sobre lo anterior), con su herencia de nivel superior
    redirecciones = {}
    for m in mp.SETDEFAULTS_RE.finditer(ITEM_TXT):
        abre = ITEM_TXT.index("{", m.start())
        cuerpo = ITEM_TXT[m.start():mp.find_matching_brace(ITEM_TXT, abre) + 1]
        for iid, crudo in mp.find_case_blocks(cuerpo):
            if iid <= 0:
                continue
            bloque = mp.strip_variant_blocks(crudo)
            aplicar(campos.setdefault(iid, {}), bloque, iid)
            rm = mp.REDIRECT_RE.search(bloque)
            if rm and int(rm.group(1)) != iid:
                redirecciones[iid] = int(rm.group(1))
    # `case 3462: SetDefaults3(2772); type = 3462;` - copia entera de otro objeto
    for _ in range(4):
        for iid, origen in redirecciones.items():
            if origen in campos:
                fusion = dict(campos[origen])
                fusion.update(campos.get(iid, {}))
                campos[iid] = fusion
    return campos


CAMPOS = campos_reales()


def c(iid, campo, defecto=0):
    return CAMPOS.get(iid, {}).get(campo, defecto)


def ai(iid):
    return AISTYLE.get(c(iid, "shoot"), -1)


# ================================================================== clasificacion por dimension
# Cada funcion devuelve la CLAVE INGLESA del subgrupo (que es tambien el nombre de la carpeta en
# el arbol y la clave de traduccion), o None si ese objeto no pertenece a esa dimension.

def subtipo_herramienta(iid):
    """Picos/hachas/martillos y sus variantes reales. `ItemID.Sets.IsDrill`/`IsChainsaw` son sets
    REALES del juego; el resto sale de la combinacion de `pick`/`axe`/`hammer`, que es
    exactamente lo que define a un pico, un hacha o un hachamartillo."""
    p, a, h = c(iid, "pick"), c(iid, "axe"), c(iid, "hammer")
    if p <= 0 and a <= 0 and h <= 0:
        return None
    if iid in SET_DRILL:
        return "Drills"
    if iid in SET_CHAINSAW:
        return "Chainsaws"
    if p > 0 and a > 0:
        return "Pickaxe axes"
    if a > 0 and h > 0:
        return "Hamaxes"
    if p > 0:
        return "Pickaxes"
    if a > 0:
        return "Axes"
    return "Hammers"


def subtipo_arma(iid):
    """Subtipo real de un arma (o de la municion que la alimenta: la carpeta "Daño a Distancia"
    del arbol real de Terrasavr mete las dos cosas juntas). El orden importa:

    1. Una herramienta que ademas hace daño (un pico, una motosierra) es antes herramienta que
       "espada" - es lo que el usuario espera ver y lo que dice su campo `pick`/`axe`/`hammer`.
    2. Si el objeto ES municion (`Item.ammo`), va con la municion de su tipo.
    3. Si es un arma a distancia, manda la MUNICION QUE CONSUME (`Item.useAmmo`): es el dato con
       el que el propio juego decide que puede disparar, y separa arcos, armas de fuego,
       lanzadores y cerbatanas sin ninguna ambiguedad.
    4. Solo entonces se mira el `aiStyle` REAL del proyectil, que es lo que distingue de verdad
       una lanza de un mayal, de un bumeran o de un yoyo. Se hace despues a proposito: el Arpon
       (160) es un arma a DISTANCIA cuyo proyectil usa el aiStyle 13 (el mismo que las armas de
       cadena cuerpo a cuerpo), y mirando solo el aiStyle acabaria entre los mayales.
    """
    herramienta = subtipo_herramienta(iid)
    if herramienta:
        return herramienta
    # Las armas de INVOCACION tienen sus propias hojas en el arbol real ("Daño de invocación",
    # "Látigos de invocador", "Daño de centinela") y no se reparten: sin este corte, los latigos
    # saldrian como "espadas" solo porque estan en el set de prefijos SwordsHammersAxesPicks.
    # El "y no es melee ni a distancia" no sobra: las armas del Ejercito Antiguo (Lanzacervezas,
    # Marca del Infierno, Dragon Volador) viven dentro del mismo bloque que las varitas de
    # centinela en Item.cs y heredan de el su `summon = true`, pero son melee de verdad.
    if c(iid, "summon", False) and not c(iid, "melee", False) and not c(iid, "ranged", False):
        return None

    municion = c(iid, "ammo")
    if municion and not c(iid, "notAmmo", False):
        return {AMMO["Arrow"]: "Arrows", AMMO["Bullet"]: "Bullets", AMMO["Rocket"]: "Rockets",
                AMMO["Dart"]: "Darts", AMMO["Flare"]: "Flares"}.get(municion, "Other ammunition")

    # El set de prefijos GunsBows tiene tambien algun arma CUERPO A CUERPO que gasta munición
    # (el Lanzacervezas, id 3821, gasta jarras de cerveza), asi que no basta con estar en el set:
    # si el objeto es melee de verdad, se clasifica como melee.
    a_distancia = c(iid, "ranged", False) or (iid in SET_ARMAS_FUEGO_ARCOS and not c(iid, "melee", False))
    if a_distancia:
        usa = c(iid, "useAmmo")
        if usa:
            return {AMMO["Arrow"]: "Bows", AMMO["Bullet"]: "Guns", AMMO["Rocket"]: "Launchers",
                    AMMO["Dart"]: "Dart weapons"}.get(usa, "Other ammo weapons")
        if c(iid, "consumable", False):
            return "Thrown weapons"
        return "Ammo-free weapons"

    estilo = ai(iid)
    if iid in SET_YOYO or estilo == AI_YOYO:
        return "Yoyos"
    if estilo == AI_BOOMERANG:
        return "Boomerangs"
    if estilo in (AI_FLAIL, AI_HARPOON, AI_FLAIRON):
        return "Flails"
    if estilo in (AI_SPEAR, AI_POLESMASH):
        return "Spears"
    if estilo == AI_SHORTSWORD or iid in SET_ESPADAS:
        return "Swords"
    if c(iid, "melee", False):
        return "Other melee weapons"
    return None


def subtipo_ranura(iid):
    """Que ranura de equipo ocupa: la separacion real de una armadura (casco/peto/grebas) y de
    la vanidad. `headSlot`/`bodySlot`/`legSlot` valen -1 cuando no aplican."""
    if c(iid, "headSlot", -1) >= 0:
        return "Head"
    if c(iid, "bodySlot", -1) >= 0:
        return "Body"
    if c(iid, "legSlot", -1) >= 0:
        return "Legs"
    if c(iid, "accessory", False):
        return "Accessory"
    return None


def subtipo_clase_equipo(iid):
    """Dentro de una ranura concreta: armadura de verdad (da defensa y no es vanidad), vanidad,
    o accesorio. Todo campo real (`vanity`, `accessory`, `defense`)."""
    if c(iid, "vanity", False):
        return "Vanity"
    if c(iid, "accessory", False):
        return "Accessories"
    if c(iid, "defense") > 0:
        return "Armor"
    return "Other equipment"


# Los doce slots visuales reales de un accesorio en Item.cs, en el orden en que los declara el
# propio juego. Un accesorio sin ninguno de ellos no se ve en el muñeco (anillos, emblemas...).
SLOTS_ACCESORIO = [
    ("wingSlot", "Wings"),
    ("shoeSlot", "Boots"),
    ("balloonSlot", "Balloons"),
    ("shieldSlot", "Shields"),
    ("neckSlot", "Necklaces"),
    ("faceSlot", "Face accessories"),
    ("handOnSlot", "Gloves"),
    ("handOffSlot", "Bracelets"),
    ("backSlot", "Back accessories"),
    ("frontSlot", "Front accessories"),
    ("waistSlot", "Belts"),
    ("beardSlot", "Beards"),
]


def subtipo_accesorio(iid):
    if not c(iid, "accessory", False):
        return None
    for campo, clave in SLOTS_ACCESORIO:
        if c(iid, campo, -1) > 0:
            return clave
    if c(iid, "infoAccessory", False):
        return "Informational accessories"
    return "Other accessories"


def tintes_de_pelo():
    """Los 12 tintes de PELO reales. No se pueden sacar de `Item.cs`: alli `hairDye` se rellena
    en tiempo de ejecucion (`hairDye = GameShaders.Hair.GetShaderIdFromItemId(type)`). La fuente
    real es `DyeInitializer`, ya extraida por scripts/extraer-tintes-pelo.py a hair_dyes.json."""
    with io.open(os.path.join(ASSETS, "hair_dyes.json"), encoding="utf-8") as f:
        return {int(e["itemId"]) for e in json.load(f)}


TINTES_PELO = tintes_de_pelo()


def nombres_en():
    with io.open(os.path.join(ASSETS, "vanilla_item_names_en.json"), encoding="utf-8") as f:
        return {int(k): v for k, v in json.load(f).items()}


NOMBRES_EN = nombres_en()


def subtipo_tinte(iid):
    """Un tinte de pelo real tiene `hairDye` (por defecto -1); uno de ropa, `dye` (el id del
    sombreador). Lo que no tiene ninguno de los dos no es un tinte y se queda sin clave: la
    carpeta "Tintes" del arbol real de Terrasavr se llena por NOMBRE (todo lo que acaba en
    "Dye"), que es el mismo predicado que se usa aqui para no marcar medio juego."""
    if not NOMBRES_EN.get(iid, "").endswith("Dye"):
        return None
    if iid in TINTES_PELO or c(iid, "hairDye", -1) >= 0:
        return "Hair dyes"
    return "Dyes"


def nombres_de_tile():
    with io.open(os.path.join(ASSETS, "tile_names.json"), encoding="utf-8") as f:
        datos = json.load(f)["tiles"]
    return {int(k): v.get("name") for k, v in datos.items() if v.get("name")}


TILE_NOMBRES = nombres_de_tile()
# Cuantos objetos tiene que colocar un mismo tile para merecer carpeta propia. Por debajo de eso
# la carpeta seria un cajon de un par de objetos y ensucia mas que ayuda: van a "Other
# placeables". Es una decision de PRESENTACION (cuantas carpetas caben en pantalla), no de
# clasificacion: ningun objeto se asigna a mano.
MINIMO_POR_TILE = 10


RE_PARENTESIS = re.compile(r"\s*\([^)]*\)\s*$")


def nombre_de_grupo_de_tile(tile):
    """Nombre real del tile, quitando el parentesis aclaratorio con el que TEdit desambigua dos
    tiles del mismo tipo ("Doors (Closed)" -> "Doors", "Tables (Group 2)" -> "Tables"). El efecto
    querido es que esos dos tiles caigan en la MISMA carpeta, que es lo que son: mesas y puertas.
    """
    nombre = TILE_NOMBRES.get(tile)
    return RE_PARENTESIS.sub("", nombre).strip() if nombre else None


def subtipo_colocable(iid, cuenta_por_tile):
    tile = c(iid, "createTile", -1)
    if tile < 0:
        return None
    if tile in TILES_CUADROS:
        return "Paintings"
    if tile in TILES_PLATAFORMAS:
        return "Platforms"
    if iid in SET_TORCHES:
        return "Torches"
    if iid in SET_CAMPFIRES:
        return "Campfires"
    if iid in SET_GLOWSTICKS:
        return "Glowsticks"
    if tile in TILES_COFRES:
        return "Chests"
    if tile not in TILES_CON_MARCO:
        return "Blocks"
    nombre = nombre_de_grupo_de_tile(tile)
    if nombre and cuenta_por_tile.get(nombre, 0) >= MINIMO_POR_TILE:
        return nombre
    return "Other placeables"


# ======================================================================= nombres de cada grupo
# La CLASIFICACION sale entera de los datos reales de arriba; esto es solo como se LLAMA cada
# grupo en pantalla, en los dos idiomas - el mismo criterio que ya usa
# `LibraryTreeBuilder.CalamityCategoryLabelsEs` para las categorias de Calamity. El español se
# ha comprobado contra los nombres REALES de objetos del juego en
# `vanilla_item_names.json` (Pico de cobre, Taladro de cobalto, Motosierra de cobalto,
# Hacha-martillo de meteorito, Pico hacha, Yoyó de madera, Bumerán de madera, Arco de madera,
# Bala de mosquete, Cohete I, Dardo venenoso, Bengala, Estandarte de conejito, Caja de música,
# Lápida, Lingote de cobre, Retrete, Bañera, Candelabro, Lámpara araña de cobre, Cama, Piano,
# Banco de trabajo, Mesa de madera, Silla de madera, Puerta de madera, Plataforma de madera,
# Estatua de ángel, Cedro de bosque en maceta...). Dos decisiones anotadas a proposito:
#   * "Mayales" para los flails: el juego en español no tiene un termino unico (usa "Flagelo"
#     en dos nombres y "Maza" en otros), y "mayal" es el que usa la wiki española para la clase.
#   * "Estanterias" y no "Librerias" (que es el nombre real del objeto Bookcase): dentro del
#     propio panel "Librería" una carpeta llamada "Librerías" se lee fatal.
ETIQUETAS = {
    # -- armas cuerpo a cuerpo
    "Swords": ("Swords", "Espadas"),
    "Spears": ("Spears", "Lanzas"),
    "Flails": ("Flails", "Mayales"),
    "Yoyos": ("Yoyos", "Yoyós"),
    "Boomerangs": ("Boomerangs", "Bumeranes"),
    "Other melee weapons": ("Other melee weapons", "Otras armas cuerpo a cuerpo"),
    # -- armas a distancia y municion
    "Bows": ("Bows", "Arcos"),
    "Guns": ("Guns", "Armas de fuego"),
    "Launchers": ("Launchers", "Lanzadores"),
    "Dart weapons": ("Dart weapons", "Armas de dardos"),
    "Other ammo weapons": ("Other ammo weapons", "Otras armas con munición"),
    "Ammo-free weapons": ("Ammo-free weapons", "Armas sin munición"),
    "Thrown weapons": ("Thrown weapons", "Armas arrojadizas"),
    "Arrows": ("Arrows", "Flechas"),
    "Bullets": ("Bullets", "Balas"),
    "Rockets": ("Rockets", "Cohetes"),
    "Darts": ("Darts", "Dardos"),
    "Flares": ("Flares", "Bengalas"),
    "Other ammunition": ("Other ammunition", "Otra munición"),
    # -- herramientas
    "Pickaxes": ("Pickaxes", "Picos"),
    "Drills": ("Drills", "Taladros"),
    "Pickaxe axes": ("Pickaxe axes", "Picos hacha"),
    "Axes": ("Axes", "Hachas"),
    "Chainsaws": ("Chainsaws", "Motosierras"),
    "Hamaxes": ("Hamaxes", "Hachas-martillo"),
    "Hammers": ("Hammers", "Martillos"),
    # -- equipo
    "Head": ("Head", "Cabeza"),
    "Body": ("Body", "Cuerpo"),
    "Legs": ("Legs", "Piernas"),
    "Accessory": ("Accessory", "Accesorio"),
    "Armor": ("Armor", "Armadura"),
    "Vanity": ("Vanity", "Vanidad"),
    "Accessories": ("Accessories", "Accesorios"),
    "Other equipment": ("Other equipment", "Otro equipo"),
    "Wings": ("Wings", "Alas"),
    "Boots": ("Boots", "Botas"),
    "Balloons": ("Balloons", "Globos"),
    "Shields": ("Shields", "Escudos"),
    "Necklaces": ("Necklaces", "Collares"),
    "Face accessories": ("Face accessories", "Accesorios de cara"),
    "Gloves": ("Gloves", "Guantes"),
    "Bracelets": ("Bracelets", "Brazaletes"),
    "Back accessories": ("Back accessories", "Accesorios de espalda"),
    "Front accessories": ("Front accessories", "Accesorios frontales"),
    "Belts": ("Belts", "Cinturones"),
    "Beards": ("Beards", "Barbas"),
    "Informational accessories": ("Informational accessories", "Accesorios informativos"),
    "Other accessories": ("Other accessories", "Otros accesorios"),
    "Dyes": ("Dyes", "Tintes"),
    "Hair dyes": ("Hair dyes", "Tintes de pelo"),
    # -- colocables (los que no son un nombre de tile directo)
    "Blocks": ("Blocks", "Bloques"),
    "Paintings": ("Paintings", "Cuadros"),
    "Platforms": ("Platforms", "Plataformas"),
    "Torches": ("Torches", "Antorchas"),
    "Campfires": ("Campfires", "Hogueras"),
    "Glowsticks": ("Glowsticks", "Barras luminosas"),
    "Chests": ("Chests", "Cofres"),
    "Other placeables": ("Other placeables", "Otros colocables"),
    # -- colocables agrupados por el tile real que colocan (la clave es el nombre real del tile)
    "Banners": ("Banners", "Estandartes"),
    "Music Boxes": ("Music boxes", "Cajas de música"),
    "Critter Anchor": ("Critter cages", "Jaulas de bicho"),
    "Statue": ("Statues", "Estatuas"),
    "Text Statue": ("Alphabet statues", "Estatuas de letras"),
    "Chandeliers": ("Chandeliers", "Lámparas de araña"),
    "Lanterns": ("Lanterns", "Faroles"),
    "Benches": ("Benches", "Bancos"),
    "Doors": ("Doors", "Puertas"),
    "Chairs": ("Chairs", "Sillas"),
    "Tables": ("Tables", "Mesas"),
    "Clocks": ("Clocks", "Relojes"),
    "Sinks": ("Sinks", "Fregaderos"),
    "Work Benches": ("Work benches", "Bancos de trabajo"),
    "Beds": ("Beds", "Camas"),
    "Pianos": ("Pianos", "Pianos"),
    "Bathtubs": ("Bathtubs", "Bañeras"),
    "Lamps": ("Lamps", "Lámparas"),
    "Candelabras": ("Candelabras", "Candelabros"),
    "Bookcases": ("Bookcases", "Estanterías"),
    "Toilets": ("Toilets", "Retretes"),
    "Candles": ("Candles", "Velas"),
    "Relic Base": ("Relics", "Reliquias"),
    "Fishing Crates": ("Fishing crates", "Cajas de pesca"),
    "Kite Anchor": ("Kites", "Cometas"),
    "Ore Bars": ("Bars", "Lingotes"),
    "Potted Trees": ("Potted plants", "Plantas en maceta"),
    "Tombstones": ("Tombstones", "Lápidas"),
    "Dye Plants": ("Dye plants", "Plantas de tinte"),
    "Pylons": ("Pylons", "Torres"),
    "Water Fountains": ("Fountains", "Fuentes"),
}

# Orden en el que salen los subgrupos dentro de su carpeta. Uno solo por dimension: cada hoja
# usa el subconjunto que le toque y el resto se ignora.
ORDENES = {
    "w": ["Swords", "Spears", "Flails", "Yoyos", "Boomerangs", "Other melee weapons",
          "Bows", "Guns", "Launchers", "Dart weapons", "Other ammo weapons",
          "Ammo-free weapons", "Thrown weapons",
          "Pickaxes", "Drills", "Pickaxe axes", "Axes", "Chainsaws", "Hamaxes", "Hammers",
          "Arrows", "Bullets", "Rockets", "Darts", "Flares", "Other ammunition"],
    "t": ["Pickaxes", "Drills", "Pickaxe axes", "Axes", "Chainsaws", "Hamaxes", "Hammers"],
    "s": ["Head", "Body", "Legs", "Accessory"],
    "k": ["Armor", "Vanity", "Accessories", "Other equipment"],
    "a": ["Wings", "Boots", "Balloons", "Shields", "Necklaces", "Face accessories", "Gloves",
          "Bracelets", "Back accessories", "Front accessories", "Belts", "Beards",
          "Informational accessories", "Other accessories"],
    "d": ["Dyes", "Hair dyes"],
}


def main():
    ids = sorted(CAMPOS)
    print(f"Item.cs: {len(ids)} ids con campos reales; {len(AISTYLE)} proyectiles con aiStyle; "
          f"{len(TILES_CON_MARCO)} tiles con marco, {len(TILES_SOLIDOS)} solidos; "
          f"{len(HELPERS)} helpers DefaultTo* resueltos solos")

    # los tiles "con marco" que de verdad merecen carpeta propia se deciden con el recuento real
    cuenta_por_tile = Counter()
    for iid in ids:
        tile = c(iid, "createTile", -1)
        if tile >= 0 and tile in TILES_CON_MARCO:
            nombre = nombre_de_grupo_de_tile(tile)
            if nombre:
                cuenta_por_tile[nombre] += 1

    salida = {}
    for iid in ids:
        ranura = subtipo_ranura(iid)
        entrada = {}
        for clave, valor in (("w", subtipo_arma(iid)),
                             ("t", subtipo_herramienta(iid)),
                             ("s", ranura),
                             ("k", subtipo_clase_equipo(iid) if ranura else None),
                             ("a", subtipo_accesorio(iid)),
                             ("d", subtipo_tinte(iid)),
                             ("p", subtipo_colocable(iid, cuenta_por_tile))):
            if valor:
                entrada[clave] = valor
        if entrada:
            salida[str(iid)] = entrada

    # Orden de los colocables: los bloques primero (es la carpeta gorda y la que se busca), luego
    # cada tipo de mueble/objeto de mas a menos poblado, y el cajon honesto al final.
    cuenta_p = Counter(v["p"] for v in salida.values() if "p" in v)
    ordenes = dict(ORDENES)
    ordenes["p"] = (["Blocks"]
                    + sorted((k for k in cuenta_p if k not in ("Blocks", "Other placeables")),
                             key=lambda k: (-cuenta_p[k], k))
                    + ["Other placeables"])

    claves = {v for entrada in salida.values() for v in entrada.values()}
    sin_etiqueta = sorted(k for k in claves if k not in ETIQUETAS)
    if sin_etiqueta:
        # No es un error fatal (el arbol se queda con la clave en ingles), pero hay que verlo.
        print(f"AVISO: {len(sin_etiqueta)} subtipos sin etiqueta: {sin_etiqueta}")
    etiquetas = {k: {"en": ETIQUETAS[k][0], "es": ETIQUETAS[k][1]} for k in sorted(claves) if k in ETIQUETAS}

    with io.open(OUT, "w", encoding="utf-8") as f:
        json.dump({"items": salida, "orders": ordenes, "labels": etiquetas},
                  f, ensure_ascii=False, separators=(",", ":"), sort_keys=True)
    print(f"escrito {OUT} ({len(salida)} objetos, {len(etiquetas)} subtipos con etiqueta)")

    for clave, titulo in (("w", "armas"), ("t", "herramientas"), ("s", "ranura"),
                          ("k", "clase de equipo"), ("a", "accesorios"), ("d", "tintes"),
                          ("p", "colocables")):
        cuenta = Counter(v[clave] for v in salida.values() if clave in v)
        print(f"\n-- {titulo}: {sum(cuenta.values())} objetos en {len(cuenta)} subtipos")
        for k, n in cuenta.most_common():
            print(f"     {ETIQUETAS.get(k, (k, k))[0]:34s} {n}")


if __name__ == "__main__":
    main()
