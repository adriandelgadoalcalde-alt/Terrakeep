"""
Lector real de los ficheros de localizacion de Terraria
(`Terraria.Localization.Content.{idioma}.{categoria}.json` del decompilado).

Por que existe (obstaculo REAL medido el 6-sep-2026, ronda de traduccion del contenido del
juego): los ficheros `es-ES.*` son JSON estricto y `json.load` los lee sin problema - que es
justo por lo que ningun script anterior necesito nada de esto -, pero los `en-US.*` traen
**comas finales** reales antes de `}`/`]` (ej. `en-US.Items.json` linea 80, `en-US.Game.json`
linea 7, `en-US.NPCs.json` linea 29). El propio juego los lee con Newtonsoft.Json, que las
tolera; `json.load` de Python y `JSON.parse` de Node NO, y revientan con
"Illegal trailing comma". No es un fichero corrupto ni un error de extraccion: es el formato
real tal cual lo distribuye Terraria.

Se limpian con un recorrido que respeta cadenas y escapes (no un regex a pelo sobre el texto:
un `",\n}"` DENTRO de una cadena de texto real del juego se cargaria el fichero en silencio).
Sin dependencias externas a proposito - el resto de scripts del proyecto solo usan la libreria
estandar.
"""
import json
import os

VANILLA_DIR = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\TerrariaVanilla"


def strip_trailing_commas(text: str) -> str:
    """Quita las comas finales antes de `}`/`]` respetando cadenas y escapes."""
    out = []
    i, n = 0, len(text)
    in_string = False
    while i < n:
        c = text[i]
        if in_string:
            out.append(c)
            if c == "\\" and i + 1 < n:
                out.append(text[i + 1])
                i += 2
                continue
            if c == '"':
                in_string = False
            i += 1
            continue
        if c == '"':
            in_string = True
            out.append(c)
            i += 1
            continue
        if c == ",":
            # Mirar hacia delante saltando espacios: si lo siguiente cierra, la coma sobra.
            j = i + 1
            while j < n and text[j] in " \t\r\n":
                j += 1
            if j < n and text[j] in "}]":
                i += 1  # se descarta la coma
                continue
        out.append(c)
        i += 1
    return "".join(out)


def load(language: str, category: str) -> dict:
    """`load("en-US", "Items")` -> dict real ya parseado."""
    path = os.path.join(VANILLA_DIR, f"Terraria.Localization.Content.{language}.{category}.json")
    with open(path, encoding="utf-8") as f:
        raw = f.read()
    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        return json.loads(strip_trailing_commas(raw))
