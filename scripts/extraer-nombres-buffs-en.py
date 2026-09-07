"""
Gemelo INGLES del nombre y la descripcion real de los buffs vanilla (ronda de traduccion del
CONTENIDO del juego, 6-sep-2026).

Situacion de partida real:
  - `vanilla_buff_names.json` NO es el nombre ingles del juego: es el nombre INTERNO de
    `BuffID.cs` ya "humanizado" con espacios (ej. "Beetle Might3", "Minecart Left") - sirve como
    clave y como ultimo recurso, pero no es lo que el juego real enseña en ingles.
  - `vanilla_buff_names_es.json` / `vanilla_buff_descriptions.json` si son texto real del juego,
    pero solo en español.

Fuente real (la misma que uso el español, contra `en-US`):
`Terraria.Localization.Content.en-US.Game.json`, claves `BuffName` (352 reales) y
`BuffDescription` (353 reales), indexadas por el mismo nombre interno PascalCase.

Salida: `vanilla_buff_names_en.json` y `vanilla_buff_descriptions_en.json` - ficheros paralelos,
el español no se toca. Lo que no tenga texto real en `en-US` se queda fuera (nunca inventado).

Uso: python scripts/extraer-nombres-buffs-en.py
"""
import json
import os

import lang_vanilla

ASSETS = os.path.join(os.path.dirname(__file__), "..", "Terrakeep.App", "Assets")


def leer(nombre: str):
    with open(os.path.join(ASSETS, nombre), encoding="utf-8") as f:
        return json.load(f)


def escribir(nombre: str, obj: dict):
    with open(os.path.join(ASSETS, nombre), "w", encoding="utf-8") as f:
        json.dump({k: obj[k] for k in sorted(obj)}, f, ensure_ascii=False, separators=(",", ":"))
    print(f"escrito {nombre}: {len(obj)} entradas")


game_en = lang_vanilla.load("en-US", "Game")
nombres_en: dict[str, str] = game_en.get("BuffName", {})
descripciones_en: dict[str, str] = game_en.get("BuffDescription", {})

# El universo real de buffs con los que trabaja la app: los ids de `vanilla_buff_names.json`
# (BuffID.cs), con su nombre interno reconstruido igual que hace VanillaBuffCatalog (quitar los
# espacios del nombre humanizado).
ids_por_interno = {v.replace(" ", ""): k for k, v in leer("vanilla_buff_names.json").items()}
nombres_es = leer("vanilla_buff_names_es.json")
descripciones_es = leer("vanilla_buff_descriptions.json")

salida_nombres = {k: v for k, v in nombres_en.items() if str(v).strip() != ""}
salida_desc = {k: v for k, v in descripciones_en.items() if str(v).strip() != ""}

sin_nombre = sorted(k for k in nombres_es if k not in salida_nombres)
sin_desc = sorted(k for k in descripciones_es if k not in salida_desc)

print(f"buffs reales de BuffID.cs: {len(ids_por_interno)}")
print(f"nombres es/en: {len(nombres_es)}/{len(salida_nombres)}  descripciones es/en: {len(descripciones_es)}/{len(salida_desc)}")
escribir("vanilla_buff_names_en.json", salida_nombres)
escribir("vanilla_buff_descriptions_en.json", salida_desc)
print(f"con nombre en español y sin ingles real: {len(sin_nombre)}{' -> ' + ', '.join(sin_nombre) if sin_nombre else ''}")
print(f"con descripcion en español y sin ingles real: {len(sin_desc)}{' -> ' + ', '.join(sin_desc) if sin_desc else ''}")
