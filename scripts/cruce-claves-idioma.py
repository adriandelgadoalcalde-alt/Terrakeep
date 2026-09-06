"""Cruce de claves de idioma de Terrakeep, en los dos sentidos.

Uso (desde la raiz del repo):
    python scripts\\cruce-claves-idioma.py

Que comprueba, todo contra el codigo REAL (TerrasavrNative.App, .cs y .xaml):
  1. Toda clave usada existe en strings_es.json Y en strings_en.json.
  2. Toda clave del diccionario se usa de verdad en alguna parte (huerfanas).
  3. Los dos diccionarios tienen exactamente las mismas claves.
  4. Ninguna pareja es/en difiere en sus marcadores {0}/{1} - una plantilla inglesa
     con mas marcadores que argumentos revienta con FormatException EN EJECUCION,
     que ningun test de compilacion detecta.
  5. Informativo: claves con el mismo texto en los dos idiomas (a veces es correcto -
     "Buffs", "Loadout:" - y a veces delata una traduccion que se olvido).

Ronda de idioma del 6-sep-2026: antes esto se rehacia a mano en el scratchpad de cada
sesion. Se guarda aqui para que la proxima ronda no empiece de cero. Detalle que costo
encontrar la primera vez: una clave puede llegar al servicio de idioma de CUATRO formas
distintas (Loc[clave] en XAML, Loc["clave"], .Format("clave", ...) y como variable o
rama de un ternario, p.ej. Format(plural ? "status_moved_plural" : "...")), asi que
buscar solo las tres primeras da decenas de "huerfanas" que en realidad si se usan.
La forma fiable es buscar cada clave del diccionario COMO TEXTO en todo el codigo.
"""
import json
import os
import re
import sys
import io

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
APP = os.path.join(RAIZ, "TerrasavrNative.App")
ASSETS = os.path.join(APP, "Assets")

es = json.load(open(os.path.join(ASSETS, "strings_es.json"), encoding="utf-8"))
en = json.load(open(os.path.join(ASSETS, "strings_en.json"), encoding="utf-8"))

def sin_comentarios(texto, es_xaml):
    """Quita comentarios antes de buscar claves.

    Hace falta de verdad: varios comentarios del proyecto documentan el patron escribiendo
    literalmente "Loc[clave]", y sin esto el cruce reporta una clave inexistente llamada
    "clave" en cada pasada - ruido que tapa un fallo de verdad.
    """
    if es_xaml:
        return re.sub(r"<!--.*?-->", "", texto, flags=re.S)
    texto = re.sub(r"/\*.*?\*/", "", texto, flags=re.S)
    # "//" solo cuenta como comentario si no va dentro de una cadena
    limpias = [
        re.sub(r'("(?:[^"\\]|\\.)*")|//.*$', lambda m: m.group(1) or "", linea)
        for linea in texto.split("\n")
    ]
    return "\n".join(limpias)


fuentes = {}
for base, dirs, files in os.walk(APP):
    dirs[:] = [d for d in dirs if d not in ("bin", "obj", "Assets")]
    for f in files:
        if f.endswith((".cs", ".xaml")):
            ruta = os.path.join(base, f)
            fuentes[os.path.relpath(ruta, RAIZ)] = sin_comentarios(
                open(ruta, encoding="utf-8").read(), f.endswith(".xaml"))

# --- claves USADAS de forma directa (las tres formas reconocibles con regex) ---
patrones = [
    re.compile(r"Loc\[([A-Za-z0-9_]+)\]"),                        # XAML
    re.compile(r"Loc\[\"([^\"]+)\"\]"),                            # C#
    re.compile(r"Loc\.Format\(\"([^\"]+)\""),
    re.compile(r"LocalizationService\.Instance\[\"([^\"]+)\"\]"),
    re.compile(r"LocalizationService\.Instance\.Format\(\"([^\"]+)\""),
]
usadas = set()
for texto in fuentes.values():
    for rx in patrones:
        usadas.update(m.group(1) for m in rx.finditer(texto))

# --- y las que llegan de forma indirecta: la clave aparece como literal en el codigo ---
todo = "\n".join(fuentes.values())
def aparece(clave):
    return f'"{clave}"' in todo or f"[{clave}]" in todo

fallos = 0

falta_es = sorted(k for k in usadas if k not in es)
falta_en = sorted(k for k in usadas if k not in en)
solo_en = sorted(k for k in en if k not in es)
solo_es = sorted(k for k in es if k not in en)
huerfanas = sorted(k for k in es if not aparece(k))
desajuste = [k for k in es if k in en
             and sorted(re.findall(r"\{(\d+)", es[k])) != sorted(re.findall(r"\{(\d+)", en[k]))]

print(f"claves en es={len(es)}  en={len(en)}  usadas (deteccion directa)={len(usadas)}")
print(f"ficheros de codigo revisados: {len(fuentes)}")

for titulo, lista in [
    ("usadas y SIN entrada en strings_es.json", falta_es),
    ("usadas y SIN entrada en strings_en.json", falta_en),
    ("en strings_en.json pero NO en strings_es.json", solo_en),
    ("en strings_es.json pero NO en strings_en.json", solo_es),
    ("en el diccionario y nunca usadas en el codigo", huerfanas),
    ("con marcadores {0}/{1} distintos entre es y en", sorted(desajuste)),
]:
    print(f"\n{titulo}: {len(lista)}")
    for k in lista:
        print(f"   {k}")
    fallos += len(lista)

iguales = [k for k in es if k in en and es[k] == en[k] and re.search(r"[A-Za-zÁÉÍÓÚáéíóúñ]", es[k])]
print(f"\n(informativo, no cuenta como fallo) mismo texto en los dos idiomas: {len(iguales)}")
for k in sorted(iguales):
    print(f"   {k} = {es[k]!r}")

print(f"\n=== {'OK, 0 discrepancias' if fallos == 0 else str(fallos) + ' DISCREPANCIAS'} ===")
sys.exit(1 if fallos else 0)
