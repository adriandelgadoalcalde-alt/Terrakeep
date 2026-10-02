// Nombres en ESPAÑOL de los objetos y buffs de Calamity del catálogo de escritorio
// (Terrakeep.App/Assets/calamity/catalog.json y buffs.json, campo `displayName_es`) desde la
// traducción PROPIA de la familia Keep, CalamityKeep-Traduccion-ES (T6, 2-oct-2026).
//
// Hasta hoy `displayName_es` venía de CalamityModEsp (traducción comunitaria, sin licencia, con
// objetos sin traducir y mayúsculas de título: «Equipo de Buceo Abisal»). La Guía v2 ya lee la
// traducción de la familia (scripts/guia-v2/fuentes.js); este script deja el catálogo con los
// MISMOS nombres que la Guía y que el juego con el mod de traducción activado.
//
// Fuente: scripts/guia-v2/fuentes.js -> nombresCalamity() (los .hjson GENERADOS del repo hermano
// CalamityKeep-Traduccion-ES, con las {$referencias} resueltas como tModLoader y los nombres que
// una clase toma de otra en el código). Solo se tocan entradas `mod === "CalamityMod"`; lo que no
// tenga nombre en la traducción se queda como estaba (nunca se inventa) y se lista.
// El ORDEN del array no se toca nunca: determina el id sintético de cada objeto/buff.
//
// Uso: node scripts/sincronizar-nombres-calamity-es.js [--comprobar]
//   --comprobar: no escribe; sale con código 1 si algún nombre difiere de la traducción.
'use strict';
const fs = require('fs');
const path = require('path');
const F = require('./guia-v2/fuentes.js');

const ASSETS = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'calamity');
const comprobar = process.argv.includes('--comprobar');
const cal = F.nombresCalamity();

function nombreBuff(interno) {
  for (const [k, v] of Object.entries(cal.planoEs)) {
    if (k.endsWith('.' + interno + '.DisplayName') && /^Mods\.CalamityMod\.Buffs\./.test(k)) return v;
  }
  return null;
}

let distintos = 0;
for (const [archivo, buscar] of [['catalog.json', i => cal.es.Items[i]], ['buffs.json', nombreBuff]]) {
  const ruta = path.join(ASSETS, archivo);
  const datos = JSON.parse(fs.readFileSync(ruta, 'utf8'));
  let cambiados = 0, iguales = 0;
  const sinNombre = [], ejemplos = [];
  for (const e of datos) {
    if (e.mod !== 'CalamityMod') continue;
    const es = buscar(e.internal);
    if (!es) { sinNombre.push(e.internal); continue; }
    if (e.displayName_es === es) { iguales++; continue; }
    if (ejemplos.length < 8) ejemplos.push(`${e.internal}: «${e.displayName_es}» -> «${es}»`);
    e.displayName_es = es;
    cambiados++;
  }
  distintos += cambiados;
  console.log(`${archivo}: ${cambiados} cambiados, ${iguales} ya iguales, ${sinNombre.length} sin nombre en la traducción${sinNombre.length ? ' (' + sinNombre.join(', ') + ')' : ''}`);
  for (const x of ejemplos) console.log('   ' + x);
  if (!comprobar && cambiados) fs.writeFileSync(ruta, JSON.stringify(datos, null, 2) + '\n');
}
console.log('Fuente: CalamityKeep-Traduccion-ES ' + cal.versionEsp + ' (Calamity ' + cal.versionCalamity + ')');
if (comprobar && distintos) process.exit(1);
