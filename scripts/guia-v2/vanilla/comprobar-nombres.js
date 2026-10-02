// Guia v2 (F1): comprueba que cada [[Nombre ingles]] / [[o:..]] / [[n:..]] de un modulo de
// contenido vanilla existe EXACTO en la localizacion en_US de Terraria 1.4.4.9 (y no es ambiguo).
// Uso: node comprobar-nombres.js <modulo.js>     (o:  node comprobar-nombres.js --nombre "Life Crystal")
'use strict';
const path = require('path');
const R = require('./resolver-nombres.js');
const a = process.argv.slice(2);
if (a[0] === '--nombre') {
  for (const n of a.slice(1)) console.log(n, '->', JSON.stringify(R.resolverUno(n)));
  process.exit(0);
}
const errores = R.comprobarTodo(require(path.resolve(a[0]))).concat(require('./ayudas.js').errores);
if (errores.length) { console.log(errores.length + ' ERRORES:\n  ' + errores.join('\n  ')); process.exit(1); }
console.log('OK: todos los nombres resuelven.');
