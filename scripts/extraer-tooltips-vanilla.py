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

AMPLIACIÓN 6-sep-2026 (ronda de traducción del CONTENIDO del juego): el mismo mecanismo, exacto,
corriendo también contra `en-US` -> `vanilla_item_tooltips_en.json`. Fichero paralelo: el español
ya verificado no se toca. El marcador de tecla sale como "[key]" en inglés, mismo criterio
(no se inventa un binding). Los `en-US.*` traen comas finales reales que `json.load` rechaza,
por eso se leen con `lang_vanilla.load` - ver el docstring de ese módulo.
"""
import json
import re

import lang_vanilla

IDS = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets\vanilla_item_ids_by_key.json"
OUT_DIR = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\Terrakeep.App\Assets"

# (idioma real del juego, fichero de salida, marcador de tecla en ese idioma)
IDIOMAS = [
    ("es-ES", "vanilla_item_tooltips.json", "[tecla]"),
    ("en-US", "vanilla_item_tooltips_en.json", "[key]"),
]

with open(IDS, encoding="utf-8") as f:
    ids_by_key: dict[str, int] = json.load(f)

REF_RE = re.compile(r"\{\$(CommonItemTooltip|PaintingArtist)\.([A-Za-z0-9_]+)\}")
INPUT_RE = re.compile(r"\{InputTrigger_[A-Za-z0-9_]+\}")


def make_resolver(dicts: dict[str, dict]):
    def resolve(text: str, depth: int = 0) -> str:
        if depth > 6:
            return text

        def repl(m: re.Match) -> str:
            section, key = m.group(1), m.group(2)
            val = dicts.get(section, {}).get(key)
            return resolve(val, depth + 1) if val is not None else m.group(0)

        new_text = REF_RE.sub(repl, text)
        return resolve(new_text, depth + 1) if new_text != text else new_text

    return resolve


for language, out_name, key_marker in IDIOMAS:
    data = lang_vanilla.load(language, "Items")
    tooltip: dict[str, str] = data["ItemTooltip"]
    resolve = make_resolver({
        "CommonItemTooltip": data.get("CommonItemTooltip", {}),
        "PaintingArtist": data.get("PaintingArtist", {}),
    })

    out: dict[str, str] = {}
    missing_id = 0
    for key, raw in tooltip.items():
        resolved = INPUT_RE.sub(key_marker, resolve(raw))
        item_id = ids_by_key.get(key)
        if item_id is None:
            missing_id += 1
            continue
        out[str(item_id)] = resolved

    print(f"[{language}] {len(out)} tooltips reales resueltos ({missing_id} sin id real - nombre interno no encontrado en vanilla_item_ids_by_key.json)")

    path = f"{OUT_DIR}\\{out_name}"
    with open(path, "w", encoding="utf-8") as f:
        json.dump({k: v for k, v in sorted(out.items(), key=lambda kv: int(kv[0]))}, f, ensure_ascii=False, separators=(",", ":"))
    print(f"escrito {path}")
