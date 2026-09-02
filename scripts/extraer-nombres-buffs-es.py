"""
Extrae el nombre REAL en español de cada buff vanilla - cierra un TODO real documentado en
VanillaBuffCatalog.cs ("SIN traduccion real al español todavia... hasta que se investigue de
donde saca el juego real el nombre de buff traducido"). Encontrado por un agente en paralelo
(ver bitacora.md): Terraria.Localization.Content.es-ES.Game.json SI tiene una clave BuffName
(352 entradas), justo al lado de BuffDescription (que ya se lee en
extraer-descripciones-buffs.py) - mismo nombre interno PascalCase.
"""
import json

SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\TerrariaVanilla\Terraria.Localization.Content.es-ES.Game.json"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\vanilla_buff_names_es.json"

with open(SRC, encoding="utf-8") as f:
    game = json.load(f)

names = game.get("BuffName", {})
print(f"{len(names)} nombres reales de buff vanilla en español")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump(names, f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
