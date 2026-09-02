// Extrae la defensa REAL de los objetos de Calamity (armaduras, principalmente) directamente
// de SetDefaults() en el codigo decompilado real de CalamityMod - mismo criterio ya usado para
// vanilla (extraer-estadisticas-vanilla.py, bloques SetDefaults1..5 de Item.cs). Pedido real
// del usuario: "las armaduras de calamity no dicen especificaciones cuando pasas el raton" -
// causa raiz: catalog.json nunca tuvo "defense" en su "stats" para NINGUN objeto de Calamity
// (comprobado antes de escribir esto - 186 armaduras reales, 0 con defense), asi que
// ItemStatsFormatter no tenia nada real que mostrar (pasaba defense:null a proposito, no era
// un bug de formato, era un hueco de datos real).
//
// Uso: node scripts/extraer-defensa-calamity.js
'use strict';
const fs = require('fs');
const path = require('path');

const CALAMITY_SRC = 'C:\\Users\\adrian\\Downloads\\tModLoader-Decompiled\\CalamityMod';
const CATALOG_PATH = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'calamity', 'catalog.json');

// Indice real: nombre de clase (= nombre de fichero) -> ruta completa. Un solo recorrido del
// arbol de 8101 ficheros reales en vez de buscar de uno en uno (mas rapido y mas facil de
// depurar si hay colisiones).
function indexarFuentes(dir, indice) {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            indexarFuentes(full, indice);
        } else if (entry.isFile() && entry.name.endsWith('.cs')) {
            const nombre = entry.name.slice(0, -3);
            if (!indice.has(nombre)) indice.set(nombre, []);
            indice.get(nombre).push(full);
        }
    }
}

console.log('Indexando codigo fuente real de Calamity...');
const indice = new Map();
indexarFuentes(CALAMITY_SRC, indice);
console.log(`  ${indice.size} nombres de clase reales indexados.`);

const catalog = JSON.parse(fs.readFileSync(CATALOG_PATH, 'utf8'));
console.log(`Catalogo real: ${catalog.length} objetos.`);

// Item.defense = N / base.Item.defense = N / this.Item.defense = N - las 3 formas reales que
// aparecen en el codigo decompilado real (confirmado con AerospecBreastplate.cs:
// "base.Item.defense = 7;").
const RE_DEFENSE = /(?:base\.|this\.)?Item\.defense\s*=\s*(-?\d+)\s*;/;

let conColision = 0, sinFichero = 0, sinDefense = 0, conDefense = 0;
const colisiones = [];

for (const entry of catalog) {
    const candidatos = indice.get(entry.internal);
    if (!candidatos) { sinFichero++; continue; }

    // Colision real (mismo nombre de clase en mas de un fichero, ej. clases base/abstractas
    // reutilizadas) - se prueban TODOS los candidatos y se toma el primero con un defense real,
    // documentando la colision para revision manual si hiciera falta.
    if (candidatos.length > 1) { conColision++; colisiones.push({ internal: entry.internal, candidatos }); }

    let defense = null;
    for (const filePath of candidatos) {
        const src = fs.readFileSync(filePath, 'utf8');
        const m = src.match(RE_DEFENSE);
        if (m) { defense = parseInt(m[1], 10); break; }
    }

    if (defense === null) { sinDefense++; continue; }
    conDefense++;
    entry.stats = entry.stats || {};
    entry.stats.defense = defense;
}

console.log(`Resultado real: ${conDefense} objetos con defense real encontrada, ${sinDefense} sin ninguna asignacion de defense en su SetDefaults, ${sinFichero} sin fichero fuente encontrado por nombre de clase.`);
console.log(`Colisiones de nombre de clase (mas de un fichero): ${conColision}`);
if (colisiones.length > 0) {
    console.log('Detalle de colisiones (primeras 10):');
    for (const c of colisiones.slice(0, 10)) console.log(`  ${c.internal}: ${c.candidatos.join(' | ')}`);
}

fs.writeFileSync(CATALOG_PATH, JSON.stringify(catalog, null, 2) + '\n', 'utf8');
console.log(`catalog.json actualizado -> ${CATALOG_PATH}`);

// Spot-check real contra un caso ya verificado a mano en esta misma sesion.
const check = catalog.find(e => e.internal === 'AerospecBreastplate');
console.log('Spot-check AerospecBreastplate.stats:', JSON.stringify(check?.stats));
if (check?.stats?.defense !== 7) console.log('  AVISO: se esperaba defense=7 (verificado a mano en el .cs real) - revisar el regex/indice.');
