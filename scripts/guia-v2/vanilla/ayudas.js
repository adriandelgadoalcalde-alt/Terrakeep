// Guia v2 (F1, 02-oct-2026): ayudas para escribir condiciones de la guia vanilla con NOMBRES
// INGLESES EXACTOS (en_US de Terraria 1.4.4.9) en vez de constantes: se resuelven aqui mismo a
// "Terraria/<ItemID|NPCID>" y un nombre inexistente rompe el build (nunca se adivina).
// Vocabulario de condiciones: el cerrado del evaluador de Core (docs/guia-v2-diseno.md §4).
'use strict';
const R = require('./resolver-nombres.js');

// Los errores se acumulan (no se lanza al primero) para verlos todos de una vez; el build y
// comprobar-nombres.js fallan si la lista no esta vacia.
const errores = [];
function ref(nombre, tipo) {
  const r = R.resolverUno(/^[on]:/.test(nombre) ? nombre : `${tipo}:${nombre}`);
  if (r.error) { errores.push('condicion: ' + r.error); return 'Terraria/__ERROR__'; }
  return r.ref;
}
const O = n => ref(n, 'o');
const N = n => ref(n, 'n');

module.exports = {
  errores, O, N,
  B: bandera => ({ tipo: 'bandera', bandera }),
  P: (nombre, cantidad = 1) => ({ tipo: 'objeto_poseido', ref: O(nombre), cantidad }),
  PA: (...nombres) => ({ tipo: 'objeto_poseido', refs: nombres.map(O) }),
  PAC: (cantidad, ...nombres) => ({ tipo: 'objeto_poseido', refs: nombres.map(O), cantidad }),
  EQ: (...nombres) => (nombres.length === 1 ? { tipo: 'equipado', ref: O(nombres[0]) } : { tipo: 'equipado', refs: nombres.map(O) }),
  NPC: nombre => ({ tipo: 'npc', ref: N(nombre) }),
  VIDA: valor => ({ tipo: 'vida_maxima', valor }),
  CRISTALES: valor => ({ tipo: 'cristales_vida', valor }),
  FRUTAS: valor => ({ tipo: 'frutas_vida', valor }),
  MANA: valor => ({ tipo: 'mana_maxima', valor }),
  DEF: valor => ({ tipo: 'defensa', valor }),
  DANO: valor => ({ tipo: 'dano_arma', valor }),
  GANCHO: () => ({ tipo: 'gancho' }),
  PUEBLO: valor => ({ tipo: 'npcs_pueblo', valor }),
  MEJ: clave => ({ tipo: 'mejora_permanente', clave }),
  EST: clave => ({ tipo: 'estado_mundo', clave }),
  TODAS: (...condiciones) => ({ tipo: 'todas', condiciones }),
  ALGUNA: (...condiciones) => ({ tipo: 'alguna', condiciones }),
  M: 'manual',
  Z: (id, siMundo) => (siMundo ? { tipo: 'zona', id, siMundo } : { tipo: 'zona', id }),
  PT: id => ({ tipo: 'punto', id }),
  W: pagina => ({ wiki: 'terraria', pagina }),
};
