// Extrae el maxStack REAL de los objetos de Calamity directamente de SetDefaults() en el
// codigo decompilado real de CalamityMod - mismo criterio ya usado para vanilla
// (extraer-max-stack-vanilla.py, bloques SetDefaults1..5 de Item.cs) y para
// extraer-defensa-calamity.js (indice de clases por nombre de fichero, misma busqueda con
// "primer candidato con dato real" ante colisiones de nombre).
//
// Contexto real (encargo Keep 25-sep-2026, "+10/+100/MAX en el editor de objeto"): el limite de
// "Cantidad" por maxStack real llevaba aparcado desde el 2-sep-2026 (ver el comentario de L-f en
// ContainerViewModel.cs) - nunca se habia extraido ni para vanilla ni para Calamity. Este script
// cierra la mitad Calamity de ese hueco de datos.
//
// `Terraria.Item.maxStack` (heredado, Calamity nunca redefine el campo) se resetea a 1 en cada
// ResetStats(Type) real de tModLoader y solo se reasigna en SetDefaults() cuando el objeto apila
// mas de 1 unidad de verdad - por eso SOLO se guarda aqui lo que aparece asignado de verdad
// (ausencia = 1, igual que "defense" ya hace en este mismo catalog.json). Medido antes de
// escribir esto (Select-String real sobre las 8101 fuentes .cs de CalamityMod): solo existen 3
// formas reales de asignacion - "Item.maxStack = Item.CommonMaxStack;"/"= CommonMaxStack;" (9999,
// la constante real de tModLoader), "= 9999;" (literal, mismo valor) y "= 1;" (equipables,
// redundante con el default pero presente en 15 clases) - mas los 8 metodos auxiliares reales
// que fijan maxStack sin linea propia (ver RE_HELPER_9999 mas abajo).
//
// Limite real conocido, no ignorado: 23 clases de Calamity usan `Item.CloneDefaults(idVanilla)`
// para heredar TODOS los stats (incluido maxStack) de un objeto vanilla existente - ni este
// script ni extraer-defensa-calamity.js (mismo punto ciego real, ya existente) resuelven esa
// herencia dinamica. Esas 23 clases se quedan con maxStack ausente (default real 1) aunque el
// objeto vanilla clonado tenga otro valor - documentado aqui en vez de fingir cobertura total.
//
// Uso: node scripts/extraer-max-stack-calamity.js
'use strict';
const fs = require('fs');
const path = require('path');

const CALAMITY_SRC = 'C:\\Users\\adrian\\Downloads\\Keep\\tModLoader-Decompiled\\CalamityMod';
const CATALOG_PATH = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'calamity', 'catalog.json');

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

// (base.|this.)?Item.maxStack = (Item.)?(CommonMaxStack|N); - las 3 formas reales confirmadas
// arriba. "==" nunca hace match (exige el ";" final, las 3 comparaciones reales encontradas en
// ReforgeChange.cs/RogueWeaponPrefix.cs/ChargingStationUI.cs no llevan ";" justo despues del
// numero/identificador en esa posicion).
const RE_MAXSTACK = /(?:base\.|this\.)?Item\.maxStack\s*=\s*(?:Item\.)?(CommonMaxStack|\d+)\s*;/;
const COMMON_MAX_STACK = 9999;

// Hallazgo real (verificado a mano contra tModLoader/Terraria/Item.cs antes de escribir esto):
// 8 metodos auxiliares reales de Item.cs (DefaultToPlaceableTile/DefaultToPlaceableWall/
// DefaultToThrownWeapon/DefaultToFood/DefaultToHealingPotion/DefaultToSeaShell/
// DefaultToCapturedCritter/DefaultToSolution) fijan maxStack=CommonMaxStack DENTRO de su propio
// cuerpo - una clase de Calamity que solo llama a `Item.DefaultToPlaceableTile(...)` (ej.
// AstralBar.cs: "base.Item.DefaultToPlaceableTile(...)") nunca tiene la linea "maxStack = "
// propia, y se quedaba fuera del RE_MAXSTACK de arriba (measurable: ~947 llamadas reales a estos
// 8 helpers en las fuentes de CalamityMod, comprobado con Select-String antes de este cambio).
const RE_HELPER_9999 = /(?:base\.|this\.)?Item\.(?:DefaultToPlaceableTile|DefaultToPlaceableWall|DefaultToThrownWeapon|DefaultToFood|DefaultToHealingPotion|DefaultToSeaShell|DefaultToCapturedCritter|DefaultToSolution)\s*\(/;

let conColision = 0, sinFichero = 0, sinMaxStack = 0, conMaxStack = 0, viaHelper = 0;
const colisiones = [];

for (const entry of catalog) {
    const candidatos = indice.get(entry.internal);
    if (!candidatos) { sinFichero++; continue; }

    if (candidatos.length > 1) { conColision++; colisiones.push({ internal: entry.internal, candidatos }); }

    let maxStack = null;
    for (const filePath of candidatos) {
        const src = fs.readFileSync(filePath, 'utf8');
        const m = src.match(RE_MAXSTACK);
        if (m) { maxStack = m[1] === 'CommonMaxStack' ? COMMON_MAX_STACK : parseInt(m[1], 10); break; }
        if (RE_HELPER_9999.test(src)) { maxStack = COMMON_MAX_STACK; viaHelper++; break; }
    }

    if (maxStack === null) { sinMaxStack++; continue; }
    conMaxStack++;
    entry.stats = entry.stats || {};
    entry.stats.maxStack = maxStack;
}

console.log(`Resultado real: ${conMaxStack} objetos con maxStack real encontrado (${viaHelper} via metodo auxiliar sin linea propia), ${sinMaxStack} sin ninguna asignacion explicita (se quedan en el default real, 1), ${sinFichero} sin fichero fuente encontrado por nombre de clase.`);
console.log(`Colisiones de nombre de clase (mas de un fichero): ${conColision}`);
if (colisiones.length > 0) {
    console.log('Detalle de colisiones (primeras 10):');
    for (const c of colisiones.slice(0, 10)) console.log(`  ${c.internal}: ${c.candidatos.join(' | ')}`);
}

fs.writeFileSync(CATALOG_PATH, JSON.stringify(catalog, null, 2) + '\n', 'utf8');
console.log(`catalog.json actualizado -> ${CATALOG_PATH}`);

// Spot-check real contra dos casos ya verificados a mano arriba (Select-String directo).
const check1 = catalog.find(e => e.internal === 'BloodfireArrow');
console.log('Spot-check BloodfireArrow.stats.maxStack:', check1?.stats?.maxStack);
if (check1?.stats?.maxStack !== 9999) console.log('  AVISO: se esperaba 9999 - revisar el regex/indice.');
const check2 = catalog.find(e => e.internal === 'EncryptedSchematicHell');
console.log('Spot-check EncryptedSchematicHell.stats.maxStack:', check2?.stats?.maxStack);
if (check2?.stats?.maxStack !== 1) console.log('  AVISO: se esperaba 1 - revisar el regex/indice.');
// Spot-check real del camino nuevo (via helper): AstralBar - "base.Item.DefaultToPlaceableTile(
// ModContent.TileType<...AstralBar>());" en SetDefaults(), SIN linea "Item.maxStack = " propia.
const check3 = catalog.find(e => e.internal === 'AstralBar');
console.log('Spot-check AstralBar.stats.maxStack (via DefaultToPlaceableTile):', check3?.stats?.maxStack);
if (check3?.stats?.maxStack !== 9999) console.log('  AVISO: se esperaba 9999 via helper - revisar RE_HELPER_9999.');
