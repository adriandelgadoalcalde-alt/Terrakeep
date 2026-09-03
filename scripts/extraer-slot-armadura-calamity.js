// H3-11 (tercera auditoria de Opus, Fable): "una pieza de armadura de Calamity se acepta en
// CUALQUIERA de los 3 slots de armadura (cabeza/cuerpo/piernas), da igual la pieza real" -
// causa raiz: catalog.json solo trae "category" a nivel de familia ("Armor/Aerospec"), nunca
// QUE PARTE del cuerpo es cada pieza - ItemSlotViewModel.AcceptsItem ya lo sabe para vanilla
// (VanillaSlotKinds) pero para Calamity solo comprobaba "es armadura", nunca "de que parte".
//
// Fuente real: tModLoader moderno (1.4.4+, la version que compila este CalamityMod
// decompilado) ya NO usa los campos numericos antiguos headSlot/bodySlot/legSlot de Item.cs -
// usa el atributo real `[AutoloadEquip(new EquipType[] { EquipType.X })]` sobre la clase
// (confirmado a mano: AerospecBreastplate.cs, EmpyreanMask.cs, EmpyreanCuisses.cs...). Mismo
// criterio ya usado en extraer-defensa-calamity.js: indexar por nombre de clase real, leer el
// atributo tal cual esta escrito en el .cs decompilado, nunca inventar la relacion.
//
// Uso: node scripts/extraer-slot-armadura-calamity.js
'use strict';
const fs = require('fs');
const path = require('path');

const CALAMITY_SRC = 'C:\\Users\\adrian\\Downloads\\tModLoader-Decompiled\\CalamityMod';
const CATALOG_PATH = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'calamity', 'catalog.json');

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
const armaduras = catalog.filter(e => e.category && e.category.startsWith('Armor'));
console.log(`Catalogo real: ${catalog.length} objetos, ${armaduras.length} de armadura.`);

// [AutoloadEquip(new EquipType[] { EquipType.Head })] - un unico EquipType real por pieza de
// armadura (confirmado: 0 casos con mas de uno dentro de Items/Armor). Head/Body/Legs son los
// 3 reales que ocupan los slots de armadura - el resto de EquipType (Wings/Balloon/Shoes/...)
// son accesorios funcionales, categoria distinta en el catalogo, no armadura.
const RE_EQUIP = /AutoloadEquip\(new EquipType\[\]\s*\{\s*EquipType\.(\w+)/;

let conSlot = 0, sinFichero = 0, sinAtributo = 0, otroTipo = 0;
const otroTipoDetalle = [];

for (const entry of armaduras) {
    const candidatos = indice.get(entry.internal);
    if (!candidatos) { sinFichero++; continue; }

    let slot = null;
    for (const filePath of candidatos) {
        const src = fs.readFileSync(filePath, 'utf8');
        const m = src.match(RE_EQUIP);
        if (m) { slot = m[1]; break; }
    }

    if (slot === null) { sinAtributo++; continue; }
    if (slot !== 'Head' && slot !== 'Body' && slot !== 'Legs') {
        otroTipo++;
        otroTipoDetalle.push({ internal: entry.internal, slot });
        continue; // Wings/Balloon/etc - no es una pieza de armadura de cuerpo real
    }

    entry.equipSlot = slot;
    conSlot++;
}

console.log(`Resultado real: ${conSlot} piezas con slot real encontrado (Head/Body/Legs), ${sinAtributo} sin atributo AutoloadEquip, ${sinFichero} sin fichero fuente por nombre de clase, ${otroTipo} con otro EquipType (Wings/Balloon/etc, no es una pieza de cuerpo).`);
if (otroTipoDetalle.length > 0) {
    console.log('Detalle "otro tipo" (primeras 10):');
    for (const d of otroTipoDetalle.slice(0, 10)) console.log(`  ${d.internal}: EquipType.${d.slot}`);
}

fs.writeFileSync(CATALOG_PATH, JSON.stringify(catalog, null, 2) + '\n', 'utf8');
console.log(`catalog.json actualizado -> ${CATALOG_PATH}`);

// Spot-check real contra los 3 casos ya verificados a mano en esta misma sesion.
for (const [internal, esperado] of [['AerospecBreastplate', 'Body'], ['EmpyreanMask', 'Head'], ['EmpyreanCuisses', 'Legs']]) {
    const check = catalog.find(e => e.internal === internal);
    console.log(`Spot-check ${internal}.equipSlot = ${check?.equipSlot} (esperado ${esperado})`);
    if (check?.equipSlot !== esperado) console.log('  AVISO: no coincide - revisar el regex/indice.');
}
