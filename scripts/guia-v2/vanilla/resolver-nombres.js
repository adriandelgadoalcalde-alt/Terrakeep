// Guia v2 (F1, 02-oct-2026): resuelve el marcado de autor de la guia vanilla a tokens del modelo.
//   [[English Name]]        -> {o:Terraria/X} o {n:Terraria/X} por nombre ingles EXACTO (en_US 1.4.4.9)
//   [[o:English Name]]      -> fuerza objeto;  [[n:English Name]] -> fuerza NPC
//   [[English Name|texto]]  -> {o:Terraria/X|texto}
// Nombre inexistente o ambiguo (objeto y NPC a la vez sin prefijo) = error: nunca se adivina.
'use strict';
const F = require('../fuentes.js');

let _idx;
const norm = s => String(s).toLowerCase().replace(/’/g, "'").replace(/\s+/g, ' ').trim();
function indice() {
  if (_idx) return _idx;
  const ids = F.ids();
  const objetos = new Map(), npcs = new Map();
  for (const [interno, en] of Object.entries(F.locTerraria('en_US', 'Items').ItemName || {}))
    if (interno in ids.item.porNombre && ids.item.porNombre[interno] > 0) { const k = norm(F.nombreObjetoVanilla(interno, 'en_US') || en); if (!objetos.has(k)) objetos.set(k, interno); }
  for (const [interno, en] of Object.entries(F.locTerraria('en_US', 'NPCs').NPCName || {}))
    if (interno in ids.npc.porNombre) { const k = norm(F.nombreNpcVanilla(interno, 'en_US') || en); if (!npcs.has(k)) npcs.set(k, interno); }
  _idx = { objetos, npcs };
  return _idx;
}

// Nombres ingleses repetidos en varias constantes de NPC (partes o variantes de un jefe, NPC
// atados): se fija la constante canonica (la que lleva el retrato y la bandera del jefe).
const NPC_CANONICO = {
  'eater of worlds': 'EaterofWorldsHead', 'the destroyer': 'TheDestroyer', 'skeletron': 'SkeletronHead',
  'skeletron prime': 'SkeletronPrime', 'wall of flesh': 'WallofFlesh', 'moon lord': 'MoonLordCore',
  'golem': 'Golem', 'lunatic cultist': 'CultistBoss', 'retinazer': 'Retinazer', 'spazmatism': 'Spazmatism',
  'martian saucer': 'MartianSaucerCore', 'betsy': 'DD2Betsy', 'flying dutchman': 'PirateShip',
  'solar pillar': 'LunarTowerSolar', 'vortex pillar': 'LunarTowerVortex', 'nebula pillar': 'LunarTowerNebula',
  'stardust pillar': 'LunarTowerStardust', 'tavernkeep': 'DD2Bartender', 'queen slime': 'QueenSlimeBoss',
  'empress of light': 'HallowBoss', 'traveling merchant': 'TravellingMerchant', 'zoologist': 'BestiaryGirl',
  'dark mage': 'DD2DarkMageT1', 'ogre': 'DD2OgreT2',
};

function resolverUno(contenido) {
  const { objetos, npcs } = indice();
  let tipo = null, nombre = contenido, texto = null;
  const bar = nombre.indexOf('|');
  if (bar >= 0) { texto = nombre.slice(bar + 1); nombre = nombre.slice(0, bar); }
  const m = /^([on]):(.*)$/.exec(nombre);
  if (m) { tipo = m[1]; nombre = m[2]; }
  const k = norm(nombre);
  const o = objetos.get(k), n = NPC_CANONICO[k] || npcs.get(k);
  if (tipo === 'o' && !o) return { error: `objeto inexistente en en_US 1.4.4.9: "${nombre}"` };
  if (tipo === 'n' && !n) return { error: `NPC inexistente en en_US 1.4.4.9: "${nombre}"` };
  if (!tipo) {
    if (o && n) return { error: `nombre ambiguo (objeto y NPC): "${nombre}" -> usa [[o:..]] o [[n:..]]` };
    if (!o && !n) return { error: `nombre inexistente en en_US 1.4.4.9: "${nombre}"` };
    tipo = o ? 'o' : 'n';
  }
  const ref = 'Terraria/' + (tipo === 'o' ? o : n);
  return { token: `{${tipo}:${ref}${texto ? '|' + texto : ''}}`, ref, tipo };
}

// Sustituye en un texto; acumula errores en la lista dada.
function resolverTexto(t, errores, donde) {
  if (typeof t !== 'string') return t;
  return t.replace(/\[\[([^\]]+)\]\]/g, (all, c) => {
    const r = resolverUno(c);
    if (r.error) { errores.push((donde ? donde + ': ' : '') + r.error); return all; }
    return r.token;
  });
}

// Recorre un valor cualquiera resolviendo todas las cadenas.
function resolverProfundo(x, errores, donde = '') {
  if (typeof x === 'string') return resolverTexto(x, errores, donde);
  if (Array.isArray(x)) return x.map(v => resolverProfundo(v, errores, donde));
  if (x && typeof x === 'object') {
    const r = {};
    const aqui = x.id ? String(x.id) : donde;
    for (const [k, v] of Object.entries(x)) r[k] = resolverProfundo(v, errores, aqui);
    return r;
  }
  return x;
}

function comprobarTodo(mod) { const e = []; resolverProfundo(mod, e); return [...new Set(e)]; }

module.exports = { resolverUno, resolverTexto, resolverProfundo, comprobarTodo, NPC_CANONICO };
