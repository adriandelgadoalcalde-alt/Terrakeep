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
// CalamityAccesorios (25-sep-2026, gap analysis paridad UICharacter): ampliado para cubrir
// TAMBIEN los 9 canales de accesorio real (EquipType.Waist/Neck/HandsOn/HandsOff/Back/Shield/
// Face/Balloon/Shoes) - EquipmentAppearanceResolver (ResolveAccessorySprite/IsAccessoryType) ya
// consultaba `entry.EquipSlot == calamitySuffix` genericamente para estos 9 tipos desde
// PortSeleccion Encargo1, pero catalog.json nunca llego a rellenar ese dato para ellos. 3
// arreglos reales sobre la version anterior de este script:
//   1. CALAMITY_SRC apuntaba a la ruta vieja sin `Keep\` (ya no existe en disco, ver
//      centralizacion 16-sep-2026) - corregido.
//   2. El filtro previo `category.startsWith('Armor')` perdia items reales fuera de esa
//      categoria - ej. StygianShield (categoria real "Weapons/Melee", EquipType.Shield). El
//      atributo AutoloadEquip real es la fuente de verdad, la categoria del catalogo NO - se
//      quita el filtro, se escanea el catalogo COMPLETO (mismo criterio que
//      extraer-defensa-calamity.js: "indexar por nombre de clase real, leer el atributo tal
//      cual", nunca suponer por categoria).
//   3. El regex anterior solo capturaba el PRIMER EquipType del atributo - 6 items reales
//      (guantes: BloodstainedGlove, ElectriciansGlove, ElementalGauntlet, FilthyGlove,
//      GloveOfPrecision, GloveOfRecklessness) declaran EquipType.HandsOn Y EquipType.HandsOff a
//      la vez (confirmado leyendo los .cs reales) - ahora se capturan TODOS los tokens
//      EquipType.X dentro del array, no solo el primero.
//
// Suficos reales de fichero en Assets/calamity/icons/ = nombre CRUDO del enum EquipType (no un
// alias corto) - confirmado con `ls` real contra el volcado ya extraido del .tmod:
// `BloodstainedGlove_HandsOn.png`/`_HandsOff.png` (no "_HandOn"), `AngelTreads_Shoes.png` (no
// "_Shoe"), `DepthCharm_Waist.png`, `StygianShield_Shield.png`. El campo `equipSlot`/
// `equipSlotSecondary` que este script escribe usa por tanto el nombre crudo del enum tal cual
// (Waist/Neck/HandsOn/HandsOff/Back/Shield/Face/Balloon/Shoes), igual que ya hacia para
// Head/Body/Legs - EquipmentAppearanceResolver.cs usa el MISMO sufijo crudo para construir la
// ruta del icono y para comparar contra EquipSlot (ver su comentario real).
//
// Wings/Front/Beard quedan FUERA a proposito (documentado, no forzado): 0 items reales de
// Calamity los declaran (confirmado con el volcado "otroTipo" de abajo) y
// EquipmentAppearanceResolver no tiene ningun canal para ellos todavia.
//
// Uso: node scripts/extraer-slot-armadura-calamity.js
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
console.log(`Catalogo real: ${catalog.length} objetos (escaneando TODOS, sin filtro de categoria - ver cabecera).`);

// Captura TODOS los tokens EquipType.X dentro del array del atributo, en el orden real en que
// estan escritos - funciona igual con el array en una sola linea o repartido en varias (\s* ya
// cruza saltos de linea en JS sin flag 's').
const RE_EQUIP_BLOCK = /AutoloadEquip\(new EquipType\[\]\s*\{([\s\S]*?)\}/;
const RE_EQUIP_TOKEN = /EquipType\.(\w+)/g;

// Los 12 tipos reales con soporte real en el resolver (Head/Body/Legs = armadura de cuerpo,
// los otros 9 = accesorio, ver EquipmentAppearanceResolver.cs). El nombre es el CRUDO del enum,
// igual que el sufijo real de fichero en disco.
const TIPOS_SOPORTADOS = new Set([
    'Head', 'Body', 'Legs',
    'Waist', 'Neck', 'HandsOn', 'HandsOff', 'Back', 'Shield', 'Face', 'Balloon', 'Shoes',
]);

let conSlot = 0, conSlotSecundario = 0, sinFichero = 0, sinAtributo = 0, otroTipo = 0, masDeDos = 0;
const porTipo = {};
const otroTipoDetalle = [];

for (const entry of catalog) {
    const candidatos = indice.get(entry.internal);
    if (!candidatos) { sinFichero++; continue; }

    let tipos = null;
    for (const filePath of candidatos) {
        const src = fs.readFileSync(filePath, 'utf8');
        const bloque = src.match(RE_EQUIP_BLOCK);
        if (!bloque) continue;
        tipos = [...bloque[1].matchAll(RE_EQUIP_TOKEN)].map(m => m[1]);
        break;
    }

    if (tipos === null) { sinAtributo++; continue; }

    const soportados = tipos.filter(t => TIPOS_SOPORTADOS.has(t));
    const noSoportados = tipos.filter(t => !TIPOS_SOPORTADOS.has(t));

    if (soportados.length === 0) {
        otroTipo++;
        otroTipoDetalle.push({ internal: entry.internal, tipos });
        continue; // Wings/Front/Beard - sin canal real en el resolver todavia.
    }

    entry.equipSlot = soportados[0];
    porTipo[soportados[0]] = (porTipo[soportados[0]] || 0) + 1;
    conSlot++;

    if (soportados.length >= 2) {
        entry.equipSlotSecondary = soportados[1];
        porTipo[soportados[1]] = (porTipo[soportados[1]] || 0) + 1;
        conSlotSecundario++;
        if (soportados.length > 2) {
            masDeDos++;
            console.log(`  AVISO: ${entry.internal} declara ${soportados.length} tipos soportados (${soportados.join(', ')}) - solo se guardan los 2 primeros.`);
        }
    }
    if (noSoportados.length > 0) {
        console.log(`  Nota: ${entry.internal} tambien declara tipo(s) sin soporte real: ${noSoportados.join(', ')} (ignorado).`);
    }
}

console.log(`Resultado real: ${conSlot} entradas con equipSlot (de las cuales ${conSlotSecundario} tambien con equipSlotSecondary), ${sinAtributo} sin atributo AutoloadEquip, ${sinFichero} sin fichero fuente por nombre de clase, ${otroTipo} con SOLO tipos sin soporte real (Wings/Front/Beard).`);
console.log('Desglose real por tipo (una entrada con equipSlotSecondary cuenta en los 2 tipos):');
for (const [tipo, n] of Object.entries(porTipo).sort()) console.log(`  ${tipo}: ${n}`);
if (masDeDos > 0) console.log(`AVISO: ${masDeDos} entrada(s) con mas de 2 tipos soportados a la vez - revisar a mano (no se esperaba ningun caso real).`);
if (otroTipoDetalle.length > 0) {
    console.log('Detalle "solo tipos sin soporte" (primeras 10):');
    for (const d of otroTipoDetalle.slice(0, 10)) console.log(`  ${d.internal}: ${d.tipos.join(', ')}`);
}

fs.writeFileSync(CATALOG_PATH, JSON.stringify(catalog, null, 2) + '\n', 'utf8');
console.log(`catalog.json actualizado -> ${CATALOG_PATH}`);

// Spot-check real contra casos ya verificados a mano en esta sesion (armadura + los 9 canales
// de accesorio nuevos, incluido el caso doble real de guantes).
for (const [internal, esperado] of [
    ['AerospecBreastplate', 'Body'], ['EmpyreanMask', 'Head'], ['EmpyreanCuisses', 'Legs'],
    ['DepthCharm', 'Waist'], ['StygianShield', 'Shield'],
]) {
    const check = catalog.find(e => e.internal === internal);
    console.log(`Spot-check ${internal}.equipSlot = ${check?.equipSlot} (esperado ${esperado})`);
    if (check?.equipSlot !== esperado) console.log('  AVISO: no coincide - revisar el regex/indice.');
}
for (const internal of ['BloodstainedGlove', 'ElectriciansGlove', 'ElementalGauntlet', 'FilthyGlove', 'GloveOfPrecision', 'GloveOfRecklessness']) {
    const check = catalog.find(e => e.internal === internal);
    console.log(`Spot-check ${internal}.equipSlot/equipSlotSecondary = ${check?.equipSlot}/${check?.equipSlotSecondary} (esperado HandsOn/HandsOff)`);
    if (check?.equipSlot !== 'HandsOn' || check?.equipSlotSecondary !== 'HandsOff') console.log('  AVISO: no coincide - revisar el regex/indice.');
}
