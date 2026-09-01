"""
Extrae las descripciones reales de buff vanilla (las que se ven al pasar el raton por
encima en Terrasavr real) de la localizacion real de Terraria - pedido explicito 2-sep-2026
("faltan descripciones de buff al pasar el raton, como en terrasav").

Fuente real: Terraria.Localization.Content.es-ES.Game.json (NO hay un "Buffs.json" aparte -
las descripciones de buff viven dentro de Game.json, clave "BuffDescription", diccionario
nombre-interno -> texto). Mismo nombre interno que ya usa vanilla_buff_names.json/
VanillaBuffCatalog.
"""
import json

SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\TerrariaVanilla\Terraria.Localization.Content.es-ES.Game.json"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\vanilla_buff_descriptions.json"

with open(SRC, encoding="utf-8") as f:
    game = json.load(f)

descriptions = game.get("BuffDescription", {})
print(f"{len(descriptions)} descripciones reales de buff vanilla")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump(descriptions, f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
