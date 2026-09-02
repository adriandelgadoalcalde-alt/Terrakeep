"""
Extrae el tooltip descriptivo real de accesorios/armaduras/etc. (el texto con los
porcentajes YA rellenados, ej. "Aumenta un 15% el daño cuerpo a cuerpo") - pregunta a Opus
sobre el diseño 2-sep-2026, cuarta pasada ("los accesorios... no te pone el porcentaje...
y una pequeña descripción de lo que hace"). Fuente real: lang.zip decompilado ->
Terraria.Localization.Content.es-ES.Items.json, clave ItemTooltip (2790 entradas reales),
indexada por el MISMO nombre interno PascalCase que ya usa vanilla_item_ids_by_key.json
(ya en Assets, 5455 claves) - sin mapeo nuevo que inventar.

1377 de las 2790 entradas contienen referencias {$CommonItemTooltip.X}/{$PaintingArtist.X}
(texto compartido, ej. "Aumenta el maná máximo en X") que hay que resolver contra los
diccionarios del MISMO fichero antes de guardar - si no, el tooltip real saldría con la
plantilla sin rellenar. {InputTrigger_X} es una tecla que el propio juego sustituye en
caliente (no hay binding por defecto que copiar sin inventarlo) - se deja como "[tecla]"
literal, documentado aquí en vez de fingir un valor real.
"""
import json
import re

SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\TerrariaVanilla\Terraria.Localization.Content.es-ES.Items.json"
IDS = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\vanilla_item_ids_by_key.json"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\vanilla_item_tooltips.json"

with open(SRC, encoding="utf-8") as f:
    data = json.load(f)
with open(IDS, encoding="utf-8") as f:
    ids_by_key: dict[str, int] = json.load(f)

tooltip: dict[str, str] = data["ItemTooltip"]
dicts = {
    "CommonItemTooltip": data.get("CommonItemTooltip", {}),
    "PaintingArtist": data.get("PaintingArtist", {}),
}

REF_RE = re.compile(r"\{\$(CommonItemTooltip|PaintingArtist)\.([A-Za-z0-9_]+)\}")
INPUT_RE = re.compile(r"\{InputTrigger_[A-Za-z0-9_]+\}")


def resolve(text: str, depth: int = 0) -> str:
    if depth > 6:
        return text

    def repl(m: re.Match) -> str:
        section, key = m.group(1), m.group(2)
        val = dicts.get(section, {}).get(key)
        return resolve(val, depth + 1) if val is not None else m.group(0)

    new_text = REF_RE.sub(repl, text)
    return resolve(new_text, depth + 1) if new_text != text else new_text


out: dict[str, str] = {}
missing_id = 0
for key, raw in tooltip.items():
    resolved = INPUT_RE.sub("[tecla]", resolve(raw))
    item_id = ids_by_key.get(key)
    if item_id is None:
        missing_id += 1
        continue
    out[str(item_id)] = resolved

print(f"{len(out)} tooltips reales resueltos ({missing_id} sin id real - nombre interno no encontrado en vanilla_item_ids_by_key.json)")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump({k: v for k, v in sorted(out.items(), key=lambda kv: int(kv[0]))}, f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
