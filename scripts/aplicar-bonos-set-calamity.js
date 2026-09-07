// Paso 2 (manual, tras revisar set_bonus_en.json y traducir set_bonus_es.json a mano) - aplica
// el bono de set real a catalog.json.
//
// Bug real encontrado y corregido en el primer intento: al aplicar el mismo texto a TODOS los
// miembrosSet (cabeza + cuerpo + piernas), las piezas de cuerpo/piernas de Aerospec/Bloodflare/
// Tarragon/etc quedaban con el texto de la ULTIMA variante de clase procesada (Melee/Ranged/
// Magic/Rogue/Summon comparten la MISMA coraza y piernas, pero cada una tiene su propia pieza
// de cabeza con su propio UpdateArmorSet real) - "Coraza de Aerospec" acababa mostrando el bono
// de Invocacion aunque el jugador llevara puesto el yelmo de Cuerpo a cuerpo, un dato REAL pero
// incorrecto para esa combinacion. En el juego real no hay un unico bono "de la coraza sola" -
// depende de que cabeza concreta se lleve puesta, algo que este catalogo (sin personaje
// cargado) no puede saber. Se aplica el texto SOLO a la clase que de verdad lo define
// (set.clase, la unica pieza de cabeza real con su propio player.setBonus=...) - las piezas de
// cuerpo/piernas compartidas se quedan sin bono propio en su tooltip individual, igual que
// antes de este arreglo (nunca mostraron nada erroneo, solo nada).
'use strict';
const fs = require('fs');
const path = require('path');

const CATALOG_PATH = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'calamity', 'catalog.json');
const EN_PATH = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'calamity', 'set_bonus_en.json');
const ES_PATH = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'calamity', 'set_bonus_es.json');
// Ronda de traduccion del CONTENIDO del juego (6-sep-2026): texto 100% INGLES REAL del mod,
// generado con `node extraer-bonos-set-calamity.js en`. Se aplica a `setBonus_en`, campo `_en`
// paralelo - mismo patron que `text_en` de vanilla_armor_sets.json y que changelog.json.
const EN_REAL_PATH = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'calamity', 'set_bonus_en_real.json');

const catalog = JSON.parse(fs.readFileSync(CATALOG_PATH, 'utf8'));
const sets = JSON.parse(fs.readFileSync(EN_PATH, 'utf8'));
const textosEs = JSON.parse(fs.readFileSync(ES_PATH, 'utf8'));
const textosEnPorClase = new Map(
    fs.existsSync(EN_REAL_PATH)
        ? JSON.parse(fs.readFileSync(EN_REAL_PATH, 'utf8')).map(s => [s.clase, s.textoFinal])
        : []);

const porInternal = new Map(catalog.map(e => [e.internal, e]));

let aplicados = 0, sinTraduccion = 0, sinEntrada = 0, aplicadosEn = 0, sinIngles = [];
for (const set of sets) {
    const texto = textosEs[set.clase];
    if (!texto) { console.log(`AVISO: sin traduccion real para "${set.clase}", se omite.`); sinTraduccion++; continue; }
    const entry = porInternal.get(set.clase);
    if (!entry) { console.log(`AVISO: "${set.clase}" no esta en catalog.json.`); sinEntrada++; continue; }
    entry.setBonus = texto;
    aplicados++;
    const textoEn = textosEnPorClase.get(set.clase);
    // Sin texto ingles real no se inventa nada: la entrada se queda sin `setBonus_en` y el
    // catalogo cae al español (LocalizedContent.Pick).
    if (textoEn) { entry.setBonus_en = textoEn; aplicadosEn++; }
    else sinIngles.push(set.clase);
}

console.log(`Aplicado a ${aplicados} entradas reales de catalog.json (${sets.length} sets, sin traduccion: ${sinTraduccion}, sin entrada: ${sinEntrada}).`);
console.log(`setBonus_en (ingles real del mod) aplicado a ${aplicadosEn} entradas${sinIngles.length ? `; sin ingles real: ${sinIngles.join(', ')}` : '; sin ingles real: 0'}.`);
fs.writeFileSync(CATALOG_PATH, JSON.stringify(catalog, null, 2) + '\n', 'utf8');
console.log(`catalog.json actualizado -> ${CATALOG_PATH}`);
