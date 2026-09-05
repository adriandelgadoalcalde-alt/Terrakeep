"""
C-16 (auditoria de pulido final, ESPEC-pulido-final-libreria-inicio-apariencia.md#9.16, cierra
media N1): "Objetos nuevos" de la pestaña Novedades (1.4.5.7) salian sin sprite - el catalogo
real de nombre->id (vanilla_item_ids_by_key.json) para para en 5455, y los 40 objetos nuevos de
esta version (6147-6195, con huecos) llegan mas alla.

Decision de diseño real del informe (criterio de Opus, no mio): NO ampliar
vanilla_item_ids_by_key.json - ese fichero alimenta la Libreria, la Investigacion y el buscador
del personaje, y Terrakeep edita guardados de tModLoader 1.4.4.9, donde los ids 5456+ NO
existen de verdad. Colocar uno de esos objetos en un slot produciria un objeto invalido en el
.plr del usuario. En su lugar, un catalogo SEPARADO y de solo lectura, consumido UNICAMENTE por
WhatsNewItemViewModel.ForVanilla (nunca por Libreria/Investigacion/buscador).

Fuente real: TerrariaVanilla/Terraria/ID/ItemID.cs, "public const short Nombre = Id;" (formato
real confirmado - NO "const int", los ids de item son short). Extrae TODOS los que superan el
maximo ya conocido (dinamico, leido del propio vanilla_item_ids_by_key.json - no un "5455"
fijo a mano, para que una futura ampliacion de ese catalogo no deje ids duplicados aqui) -
verificado: 740 objetos reales por encima de 5455, con sprite YA en disco
(Assets/vanilla/icons/{id}.png, VanillaIconResolver ya resuelve por id crudo, no por nombre) -
esta pasada solo cierra el eslabon que faltaba, el nombre.
"""
import json
import re

ITEM_SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\TerrariaVanilla\Terraria\ID\ItemID.cs"
EXISTING_IDS = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\vanilla_item_ids_by_key.json"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\whats_new_item_ids.json"

with open(EXISTING_IDS, encoding="utf-8") as f:
    existing = json.load(f)
max_existing_id = max(existing.values())
print(f"Maximo id ya conocido (vanilla_item_ids_by_key.json): {max_existing_id}")

with open(ITEM_SRC, encoding="utf-8") as f:
    text = f.read()

CONST_RE = re.compile(r"public const short (\w+) = (-?\d+);")
out = {}
for m in CONST_RE.finditer(text):
    name, value = m.group(1), int(m.group(2))
    if value > max_existing_id:
        out[name] = value

print(f"{len(out)} objetos reales por encima de {max_existing_id} (esperado 740 en esta version)")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump(dict(sorted(out.items(), key=lambda kv: kv[1])), f, ensure_ascii=False, indent=2)
print(f"escrito {OUT}")
