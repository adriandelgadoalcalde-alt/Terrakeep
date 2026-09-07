"""
Gemelo INGLES de los catalogos de nombres de objeto vanilla (ronda de traduccion del CONTENIDO
del juego, 6-sep-2026 - el limite que arrastraban `vanilla_item_names.json` /
`vanilla_item_names_by_key.json`: solo tenian español, asi que la app enseñaba "Pico de hierro"
tambien con la interfaz en ingles).

Fuente real, la MISMA que uso el catalogo español pero contra `en-US`:
`Terraria.Localization.Content.en-US.Items.json`, seccion `ItemName` (6184 claves reales),
cruzada por el MISMO nombre interno PascalCase que ya usa `vanilla_item_ids_by_key.json` - o
sea, ningun mapeo nuevo que inventar, exactamente el mismo mecanismo que
`completar-nombres-objetos-145.js`.

Salida (ficheros paralelos, no se toca ni un byte del español ya verificado):
  - `vanilla_item_names_en.json`        (id numerico -> nombre en ingles)
  - `vanilla_item_names_by_key_en.json` (nombre interno -> nombre en ingles)

Criterio de siempre: lo que NO tenga nombre real en `en-US` se queda FUERA del fichero ingles -
nunca se inventa ni se traduce a mano. `LocalizedContent.Pick` cae entonces a la entrada
española, que es el idioma de referencia y esta completo.

Uso: python scripts/extraer-nombres-objetos-en.py
"""
import json
import os

import lang_vanilla

ASSETS = os.path.join(os.path.dirname(__file__), "..", "Terrakeep.App", "Assets")


def leer(nombre: str):
    with open(os.path.join(ASSETS, nombre), encoding="utf-8") as f:
        return json.load(f)


def escribir(nombre: str, obj: dict, comparador):
    ruta = os.path.join(ASSETS, nombre)
    ordenado = {k: obj[k] for k in sorted(obj.keys(), key=comparador)}
    with open(ruta, "w", encoding="utf-8") as f:
        json.dump(ordenado, f, ensure_ascii=False, separators=(",", ":"))
    print(f"escrito {nombre}: {len(ordenado)} entradas")


nombres_es = leer("vanilla_item_names.json")
nombres_por_clave_es = leer("vanilla_item_names_by_key.json")
ids_por_clave = leer("vanilla_item_ids_by_key.json")

item_name_en: dict[str, str] = lang_vanilla.load("en-US", "Items").get("ItemName", {})

por_id: dict[str, str] = {}
por_clave: dict[str, str] = {}
sin_en: list[str] = []

for clave, item_id in ids_por_clave.items():
    en = item_name_en.get(clave)
    if en is None or str(en).strip() == "":
        # Solo cuenta como excepcion real si el objeto SI existe en el catalogo español (es
        # decir, se le enseña un nombre al usuario y ese nombre se quedara en español).
        if str(item_id) in nombres_es or clave in nombres_por_clave_es:
            sin_en.append(f"{clave} ({item_id})")
        continue
    por_clave[clave] = en
    # Un id puede tener varias claves reales (alias historicos); gana la primera, igual que
    # hace el catalogo español - el nombre mostrado es el mismo objeto real de todas formas.
    por_id.setdefault(str(item_id), en)

print(f"en-US ItemName real: {len(item_name_en)} claves")
print(f"catalogo español: {len(nombres_es)} ids / {len(nombres_por_clave_es)} claves")
escribir("vanilla_item_names_en.json", por_id, lambda k: k)
escribir("vanilla_item_names_by_key_en.json", por_clave, lambda k: k)
print(f"sin nombre real en en-US (se quedan en español a proposito): {len(sin_en)}")
if sin_en:
    print("  " + ", ".join(sin_en))
