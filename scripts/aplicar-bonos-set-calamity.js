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

const CATALOG_PATH = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'calamity', 'catalog.json');
const EN_PATH = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'calamity', 'set_bonus_en.json');
const ES_PATH = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'calamity', 'set_bonus_es.json');

const catalog = JSON.parse(fs.readFileSync(CATALOG_PATH, 'utf8'));
const sets = JSON.parse(fs.readFileSync(EN_PATH, 'utf8'));
const textosEs = JSON.parse(fs.readFileSync(ES_PATH, 'utf8'));

const porInternal = new Map(catalog.map(e => [e.internal, e]));

let aplicados = 0, sinTraduccion = 0, sinEntrada = 0;
for (const set of sets) {
    const texto = textosEs[set.clase];
    if (!texto) { console.log(`AVISO: sin traduccion real para "${set.clase}", se omite.`); sinTraduccion++; continue; }
    const entry = porInternal.get(set.clase);
    if (!entry) { console.log(`AVISO: "${set.clase}" no esta en catalog.json.`); sinEntrada++; continue; }
    entry.setBonus = texto;
    aplicados++;
}

console.log(`Aplicado a ${aplicados} entradas reales de catalog.json (${sets.length} sets, sin traduccion: ${sinTraduccion}, sin entrada: ${sinEntrada}).`);
fs.writeFileSync(CATALOG_PATH, JSON.stringify(catalog, null, 2) + '\n', 'utf8');
console.log(`catalog.json actualizado -> ${CATALOG_PATH}`);
