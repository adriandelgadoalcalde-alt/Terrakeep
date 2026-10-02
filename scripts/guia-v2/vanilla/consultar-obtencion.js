// Guia v2 (F1): consulta rapida de la obtencion extraida del codigo (para redactar sin inventar).
// Uso: node consultar-obtencion.js <obtencion.json> "English Name" ...
'use strict';
const fs = require('fs');
const R = require('./resolver-nombres.js');
const obt = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
for (const n of process.argv.slice(3)) {
  const r = R.resolverUno(n);
  if (r.error) { console.log(n, '->', r.error); continue; }
  const lista = (obt.objetos[r.ref] || []).map(o => {
    const ing = (o.ingredientes || []).map(i => `${i.cantidad}x${(i.ref || i.grupo).replace('Terraria/', '')}`).join(' + ');
    return `${o.fuente && o.fuente.startsWith('CalamityMod') ? '[CAL] ' : ''}${o.tipo}${ing ? ' ' + ing : ''}${o.estaciones?.length ? ' @' + o.estaciones.map(e => e.replace('Terraria/Tile/', '')).join('/') : ''}${o.de || o.vendedor ? ' de ' + (o.de || o.vendedor) : ''}${o.probabilidad ? ' p=' + o.probabilidad : ''}${o.condicion ? ' [' + o.condicion + ']' : ''}${o.condiciones?.length ? ' {' + o.condiciones.join(',') + '}' : ''}`;
  });
  console.log(n, '(' + r.ref + '):', lista.length ? '\n   ' + lista.join('\n   ') : 'SIN OBTENCION EN CODIGO (mundo/cofres/pesca)');
}
