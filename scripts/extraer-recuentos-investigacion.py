"""
Auditoria de Opus, Bloque 2 (R-1): extrae la tabla REAL de "cuantos hacen falta sacrificar
para investigar del todo" (Modo Viaje) de tModLoader - antes "Investigar todo" escribia un
conteo fijo inventado (9999) para TODO objeto, y la Libreria/Investigacion no podia mostrar
nunca un "x/N" real por no conocer N.

Fuente real: Terraria.GameContent.Creative.Content.Sacrifices.tsv (recurso embebido real,
extraido junto al resto del decompilado) - cada fila es "NombreInterno<TAB>Letra<TAB>...", la
letra es una categoria de rareza que fija N (tabla real, decodificada a mano del comentario de
cabecera del propio TSV Y confirmada contra el switch real de
Terraria/GameContent/Creative/CreativeItemSacrificesCatalog.cs: a=50 b=25 c=5 d=1 e=invalido
(el objeto no es investigable de verdad, ej. objetos viejos pre-1.4 tipo "...Old" - se omite
sin inventar nada) f=2 g=3 h=10 i=15 j=30 k=99 l=100 m=200 n=20 o=400.

El nombre interno se resuelve a id real via vanilla_item_ids_by_key.json (ya generado por
otro extractor, mismo mapa nombre->id que ya usa el resto del catalogo vanilla) - un nombre
del TSV que no aparezca ahi (mismo caso ya documentado en VanillaItemCatalog: cobertura no al
100%) se omite en silencio, no se inventa ningun id.

ALCANCE DELIBERADO: no se porta ContentSamples.CreativeResearchItemPersistentIdOverride (un
puñado de variantes que en el juego real comparten investigacion con otro objeto base, ej.
colores de un mismo objeto) - afecta a un numero pequeño de casos y no estaba investigado a
fondo dentro del tiempo de esta ronda; cada uno de esos ids sigue teniendo su PROPIO umbral
real del TSV en vez de heredar el del objeto que comparten, ligeramente menos preciso pero
nunca inventado.
"""
import json
import re

TSV = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria.GameContent.Creative.Content.Sacrifices.tsv"
IDS_BY_KEY = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\vanilla_item_ids_by_key.json"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\vanilla_research_counts.json"

THRESHOLDS = {
    "a": 50, "b": 25, "c": 5, "d": 1, "e": None,
    "f": 2, "g": 3, "h": 10, "i": 15, "j": 30,
    "k": 99, "l": 100, "m": 200, "n": 20, "o": 400,
}

with open(IDS_BY_KEY, encoding="utf-8") as f:
    ids_by_key: dict[str, int] = json.load(f)

with open(TSV, encoding="utf-8") as f:
    lines = re.split(r"\r\n|\r|\n", f.read())

counts: dict[int, int] = {}
skipped_invalid = 0
skipped_unknown = 0
for line in lines:
    if not line or line.startswith("//"):
        continue
    parts = line.split("\t")
    if len(parts) < 2:
        continue
    name, letter = parts[0], parts[1].lower()
    if letter not in THRESHOLDS:
        raise ValueError(f"Letra de categoria desconocida: {letter!r} (fila: {line!r})")
    threshold = THRESHOLDS[letter]
    if threshold is None:
        skipped_invalid += 1
        continue
    item_id = ids_by_key.get(name)
    if item_id is None:
        skipped_unknown += 1
        continue
    counts[item_id] = threshold

with open(OUT, "w", encoding="utf-8") as f:
    json.dump(counts, f, ensure_ascii=False, separators=(",", ":"))

print(f"{len(counts)} objeto(s) con umbral real de investigacion -> {OUT}")
print(f"  omitidos (categoria E, invalido/no investigable): {skipped_invalid}")
print(f"  omitidos (nombre interno sin id conocido): {skipped_unknown}")
