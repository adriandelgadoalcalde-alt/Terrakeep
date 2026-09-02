"""
Extrae el efecto REAL de cada prefijo (multiplicadores de arma / bonos planos de accesorio),
para poder componer una linea de tooltip real tipo "+15% de daño, +5% de critico" en vez de
solo el nombre del prefijo - pregunta a Opus sobre el diseño 2-sep-2026, cuarta pasada ("los
accesorios... no te pone el porcentaje si es +1% de daño +2% de ataque crítico etc").

Dos fuentes reales, ambas parseadas letra a letra del propio codigo decompilado (nunca
adivinadas):
1. Item.TryGetPrefixStatMultipliersForItem (armas: prefijos 1-58 aprox, switch real
   "case N: campo = X.Yf; ... break;") - multiplicadores relativos a 1.0.
2. Player.GrantPrefixBenefits (accesorios: prefijos 62-80, "if (item.prefix == N) { campo
   += X; }" reales, planos) - los accesorios tambien llaman a
   PrefixLoader.ApplyAccessoryEffects para prefijos de Calamity/mods, fuera de alcance aqui
   (vanilla-only, igual que el resto de este catalogo).
"""
import json
import re

ITEM_SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Item.cs"
PLAYER_SRC = r"C:\Users\adrian\Downloads\tModLoader-Decompiled\tModLoader\Terraria\Player.cs"
OUT = r"C:\Users\adrian\Downloads\Terrasavr-Win\Terrasavr-Native\TerrasavrNative.App\Assets\vanilla_prefix_effects.json"

effects: dict[int, dict[str, float]] = {}

# --- 1. Multiplicadores de arma (Item.cs) ---
with open(ITEM_SRC, encoding="utf-8") as f:
    item_text = f.read()

start = item_text.index("private bool TryGetPrefixStatMultipliersForItem")
end = item_text.index("if (dmg != 1f && Math.Round", start)
switch_body = item_text[start:end]

CASE_BLOCK_RE = re.compile(r"case\s+(\d+)\s*:(.*?)break;", re.S)
FIELD_RE = re.compile(r"\b(dmg|kb|spd|size|shtspd|mcst|crt)\s*=\s*(-?\d+(?:\.\d+)?)f?\s*;")

for m in CASE_BLOCK_RE.finditer(switch_body):
    prefix_id = int(m.group(1))
    block = m.group(2)
    entry = {}
    for fm in FIELD_RE.finditer(block):
        field, value = fm.group(1), float(fm.group(2))
        entry[field] = value
    if entry:
        effects[prefix_id] = entry

print(f"{len(effects)} prefijos de arma reales (Item.cs)")

# --- 2. Bonos planos de accesorio (Player.cs) ---
with open(PLAYER_SRC, encoding="utf-8") as f:
    player_text = f.read()

gstart = player_text.index("public void GrantPrefixBenefits")
gend = player_text.index("PrefixLoader.ApplyAccessoryEffects", gstart)
accessory_body = player_text[gstart:gend]

IF_BLOCK_RE = re.compile(r"if\s*\(item\.prefix\s*==\s*(\d+)\)\s*\{(.*?)\}", re.S)
INC_RE = re.compile(r"\b(statDefense|statManaMax2|allCrit|allDamage|moveSpeed|meleeSpeed)\s*\+=\s*(-?\d+(?:\.\d+)?)f?\s*;")
PLUSPLUS_RE = re.compile(r"\+\+\s*(statDefense|statManaMax2|allCrit|allDamage|moveSpeed|meleeSpeed)\s*;")

for m in IF_BLOCK_RE.finditer(accessory_body):
    prefix_id = int(m.group(1))
    block = m.group(2)
    entry = {}
    for fm in INC_RE.finditer(block):
        field, value = fm.group(1), float(fm.group(2))
        entry[field] = value
    for fm in PLUSPLUS_RE.finditer(block):
        entry[fm.group(1)] = 1.0
    if entry:
        effects[prefix_id] = entry

print(f"{len(effects)} prefijos reales en total (arma + accesorio)")

with open(OUT, "w", encoding="utf-8") as f:
    json.dump({str(k): v for k, v in sorted(effects.items())}, f, ensure_ascii=False, separators=(",", ":"))
print(f"escrito {OUT}")
