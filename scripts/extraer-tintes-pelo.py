"""
Extrae el indice REAL de cada tinte de pelo (el numero que se guarda en PlrCharacter.HairDye)
directamente del codigo decompilado de tModLoader - pedido explicito 1-sep-2026 ("Apariencia
sigue siendo por ID", refiriendose en concreto al tinte de pelo, ya que el estilo de peinado
ya tenia selector visual).

Fuente real: Terraria/Initializers/DyeInitializer.cs, Terraria/Graphics/Shaders/
HairShaderDataSet.cs. HairShaderDataSet.BindShader(itemId, ...) asigna
"_shaderLookupDictionary[itemId] = ++_shaderDataCount" - el indice real es 1-based, en el
orden EXACTO en que BindShader se llama en tiempo de ejecucion (NO el orden por numero de
linea del fichero fuente: DyeInitializer.LoadHairDyes() llama primero a
LoadLegacyHairdyes() -que a su vez enlaza los 11 ids "legacy", definidos DESPUES en el
fichero por orden de linea- y solo entonces enlaza su propio id 3259 el ultimo de los 12 -
confirmado leyendo Load() -> LoadHairDyes() -> [LoadLegacyHairdyes() primero, bind(3259)
despues] linea a linea, no asumido por orden de aparicion en el texto).

GameShaders.Hair.BindShader solo se llama desde este fichero en todo el codigo decompilado
(comprobado con una busqueda global) - no hay ninguna otra fuente que pueda desplazar estos
indices.
"""
import json
import re

SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Initializers\DyeInitializer.cs"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets\hair_dyes.json"

with open(SRC, encoding="utf-8") as f:
    text = f.read()


def method_body(name: str) -> str:
    m = re.search(rf"private static void {name}\(\)\s*\{{", text)
    if not m:
        raise ValueError(f"metodo no encontrado: {name}")
    start = m.end()
    depth = 1
    i = start
    while depth > 0:
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
        i += 1
    return text[start:i]


legacy_body = method_body("LoadLegacyHairdyes")
hair_body = method_body("LoadHairDyes")

legacy_ids = [int(x) for x in re.findall(r"GameShaders\.Hair\.BindShader\((\d+),", legacy_body)]
# En LoadHairDyes, excluir la propia llamada a LoadLegacyHairdyes() (ya contada aparte) - solo
# el bind propio de esa funcion, que en el codigo real va DESPUES de esa llamada.
own_ids = [int(x) for x in re.findall(r"GameShaders\.Hair\.BindShader\((\d+),", hair_body)]

# Orden de llamada real: LoadHairDyes() ejecuta LoadLegacyHairdyes() primero (11 ids), luego
# su propio bind(3259) el ultimo.
call_order = legacy_ids + own_ids
assert len(call_order) == 12, f"se esperaban 12 tintes de pelo reales, salieron {len(call_order)}: {call_order}"
assert call_order[0] == 1977 and call_order[-1] == 3259, f"orden real inesperado: {call_order}"

entries = [{"index": i + 1, "itemId": item_id} for i, item_id in enumerate(call_order)]

print(f"{len(entries)} tintes de pelo reales, orden de llamada real: {call_order}")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump(entries, f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
