// Ronda de Libreria/Builds del 6-sep-2026 - BUG REAL medido: 36 carpetas de la Libreria se ven
// COMPLETAMENTE VACIAS y otras 15 enseñan menos objetos de los que su propio nombre declara.
//
// Causa real (no una suposicion - medida id a id): el ARBOL de carpetas de la Libreria es un
// puerto literal del Terrasavr real (Assets/vanilla_library_tree.json, ver
// extraer-arbol-libreria-vanilla.js) y ese Terrasavr va con un Terraria MAS NUEVO: referencia
// ids de objeto hasta el 6145. El catalogo de NOMBRES (Assets/vanilla_item_names.json) se genero
// en su dia contra un ItemID.cs que solo llega al 5455. LibraryViewModel.ApplyFilter descarta EN
// SILENCIO cualquier id que el catalogo no conozca (arreglo real de 2-sep-2026, para no tumbar la
// app con un KeyNotFoundException) - util para no reventar, pero el efecto visible es que una
// pagina entera de una carpeta puede quedarse sin una sola tarjeta sin decir por que.
//
// Numeros reales antes de este script:
//   - 691 ids del arbol sin nombre (690 por encima de 5455, mas el 0, que es relleno real del
//     propio Terrasavr y NO es ningun objeto).
//   - 402 hojas del arbol vanilla: 51 con perdida, 36 completamente vacias (ej.
//     "Categories/Placeable (3219)/Pages 61+/Page 68" -> 0 de 40).
//   - 14.998 objetos declarados por las hojas -> solo 13.608 renderizables.
//
// Los ICONOS ya estaban al dia (Assets/vanilla/icons llega hasta 6195 - extraer-iconos-vanilla.js
// los saco de la instalacion real de Steam, no del atlas viejo): lo unico que faltaba eran los
// nombres.
//
// Fuentes REALES (nada inventado, mismo criterio de siempre - lo que no se encuentra se queda
// fuera, no se rellena a ojo):
//   - Terraria 1.4.5.8 decompilado: Terraria/ID/ItemID.cs (ItemID.Count = 6196 real).
//   - Traduccion oficial del propio juego: Terraria.Localization.Content.es-ES.Items.json,
//     seccion "ItemName" - la MISMA fuente con la que se genero el catalogo original.
//
// IDEMPOTENTE a proposito: solo AÑADE ids/claves que no existan ya, nunca pisa una entrada
// existente. Asi se puede volver a pasar tal cual, y ademas ninguna traduccion ya revisada
// cambia por debajo (mismo criterio que parchear-nombres-contenedores-es.js).
//
// Uso: node scripts/completar-nombres-objetos-145.js
'use strict';
const fs = require('fs');
const path = require('path');

const ASSETS = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets');
const VANILLA = 'C:\\Users\\adrian\\Downloads\\tModLoader-Decompiled\\TerrariaVanilla';
const ITEM_ID_CS = path.join(VANILLA, 'Terraria', 'ID', 'ItemID.cs');
const LANG_ES = path.join(VANILLA, 'Terraria.Localization.Content.es-ES.Items.json');

function leerJson(p) { return JSON.parse(fs.readFileSync(p, 'utf8')); }

const names = leerJson(path.join(ASSETS, 'vanilla_item_names.json'));
const namesByKey = leerJson(path.join(ASSETS, 'vanilla_item_names_by_key.json'));
const idsByKey = leerJson(path.join(ASSETS, 'vanilla_item_ids_by_key.json'));
const itemName = leerJson(LANG_ES).ItemName || {};

// Constantes reales de ItemID.cs. Los ids NEGATIVOS son reales (las Phasesaber "Old" y demas
// alias historicos) pero no son objetos que se puedan colocar ni aparecen en el arbol - se
// dejan fuera, igual que hacia el catalogo original.
const src = fs.readFileSync(ITEM_ID_CS, 'utf8');
const re = /public const short ([A-Za-z0-9_]+) = (-?\d+);/g;
const constantes = [];
let m;
while ((m = re.exec(src)) !== null) {
    const id = Number(m[2]);
    if (id > 0) constantes.push({ key: m[1], id });
}

let añadidosPorId = 0, añadidosPorClave = 0, sinTraduccion = [];
for (const { key, id } of constantes) {
    const nombre = itemName[key];
    if (nombre === undefined || String(nombre).trim() === '') {
        if (names[String(id)] === undefined) sinTraduccion.push(`${key} (${id})`);
        continue;
    }
    if (names[String(id)] === undefined) { names[String(id)] = nombre; añadidosPorId++; }
    if (namesByKey[key] === undefined) { namesByKey[key] = nombre; añadidosPorClave++; }
    if (idsByKey[key] === undefined) idsByKey[key] = id;
}

// Mismo orden REAL que ya tenian los tres ficheros: por clave de texto en names (asi estaba
// generado: "1","10","100","1000"...), alfabetico en names_by_key, por id ascendente en
// ids_by_key. Se respeta para que el diff sea legible y no una reordenacion entera.
function escribirOrdenado(nombre, obj, comparador) {
    const salida = {};
    for (const k of Object.keys(obj).sort(comparador)) salida[k] = obj[k];
    fs.writeFileSync(path.join(ASSETS, nombre), JSON.stringify(salida), 'utf8');
}
escribirOrdenado('vanilla_item_names.json', names, (a, b) => a.localeCompare(b, 'en'));
escribirOrdenado('vanilla_item_names_by_key.json', namesByKey, (a, b) => a.localeCompare(b, 'en'));
escribirOrdenado('vanilla_item_ids_by_key.json', idsByKey, (a, b) => idsByKey[a] - idsByKey[b]);

console.log(`ItemID.cs real: ${constantes.length} constantes con id>0`);
console.log(`vanilla_item_names.json: +${añadidosPorId} ids nuevos -> ${Object.keys(names).length} en total`);
console.log(`vanilla_item_names_by_key.json: +${añadidosPorClave} claves -> ${Object.keys(namesByKey).length}`);
console.log(`vanilla_item_ids_by_key.json: ${Object.keys(idsByKey).length} claves`);
console.log(`sin traduccion es-ES real (se quedan fuera a proposito): ${sinTraduccion.length}${sinTraduccion.length ? ' -> ' + sinTraduccion.join(', ') : ''}`);
