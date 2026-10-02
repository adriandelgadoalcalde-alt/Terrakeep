// Guia v2 (F2b, 02-oct-2026): nombres en español de los CONJUNTOS de armadura de las escaleras de
// equipo, comunes a la guia vanilla y a la de Calamity.
//
// Terraria y Calamity no tienen una cadena oficial "nombre del conjunto": el nombre oficial esta en
// cada PIEZA (es-ES de Terraria 1.4.4.9 y CalamityKeep-Traduccion-ES). Aqui va el complemento que comparten las
// piezas oficiales de cada conjunto (SilverHelmet «Casco de plata», SilverChainmail «Cota de malla
// de plata» -> «de plata»; GemTechHeadgear «Casco Tecnogema» -> «Tecnogema»). La prueba de contenido
// (GuiaV2ContenidoTests) comprueba que cada rotulo sale de las piezas reales del grupo y que no queda
// ninguna palabra inglesa con traduccion oficial; nunca se inventa un nombre.
//
// Clave = nombre ingles del conjunto en la wiki SIN " armor".
'use strict';

const CONJUNTOS_ARMADURA = {
  // Terraria (vanilla, tambien usados por la guia de Calamity)
  Wooden: 'de madera', Cactus: 'de cactus', Pumpkin: 'de calabaza', Gold: 'de oro', Platinum: 'de platino', Silver: 'de plata',
  Gladiator: 'de gladiador', Fossil: 'de fósil', Obsidian: 'de obsidiana', Bee: 'de abeja', Jungle: 'para la selva',
  Meteor: 'de meteorito', Necro: 'de los muertos', Shadow: 'de las sombras', Crimson: 'carmesí', Molten: 'fundida',
  Spider: 'de araña', Cobalt: 'de cobalto', Palladium: 'de paladio', Mythril: 'de mithril', Orichalcum: 'de oricalco',
  Adamantite: 'de adamantita', Titanium: 'de titanio', 'Crystal Assassin': 'de asesino de cristal', Frost: 'helada',
  Hallowed: 'sagrada', Chlorophyte: 'de clorofita', Turtle: 'de tortuga', Tiki: 'tiki', Spooky: 'tétrica',
  Shroomite: 'de piñonita', Spectre: 'espectral', Beetle: 'de escarabajo', Apprentice: 'de aprendiz', Squire: 'de escudero',
  Monk: 'de monje', 'Dark Artist': 'de Artista Oscuro', 'Valhalla Knight': 'de Caballero del Valhalla',
  'Shinobi Infiltrator': 'de infiltrador shinobi', 'Solar Flare': 'de fulguración solar', Vortex: 'del vórtice',
  Nebula: 'de nebulosa', Stardust: 'de polvo estelar',
};

// Calamity: complemento comun de los nombres de sus piezas en CalamityKeep-Traduccion-ES (la
// traduccion propia de la familia Keep, T6 del 2-oct-2026; antes se tomaba de CalamityModEsp).
// verificarConjuntosCalamity() comprueba al construir la guia que cada uno sale de verdad de los
// nombres de las piezas reales (clase <Conjunto>*): si la traduccion cambia, la guia no se construye.
const CONJUNTOS_CALAMITY = {
  Wulfrum: 'de wulfrum', Aerospec: 'de aerospec', Victide: 'de victide', 'Snow Ruffian': 'del rufián de las nieves',
  'Desert Prowler': 'del acechador del desierto', Sulphurous: 'sulfurosa', Statigel: 'de statigel',
  Daedalus: 'de Dédalo', Reaver: 'de saqueador', 'Fathom Swarmer': 'del enjambre abisal',
  Hydrothermic: 'hidrotermal', Astral: 'astral', 'Titan Heart': 'de corazón de titán', 'Plague Reaper': 'del segador de la Plaga',
  Plaguebringer: 'portaplagas', 'Lunic Corps': 'del Cuerpo Lúnico', Brimflame: 'de llama de azufre',
  Umbraphile: 'umbrófila', Fearmonger: 'del sembrador de terror', Tarragon: 'de tarragon', 'Omega Blue': 'azul omega',
  Bloodflare: 'de sangrellama', 'God Slayer': 'matadioses', Silva: 'de silva', 'Gem Tech': 'tecnogema',
  'Auric Tesla': 'de Tesla áurica', Demonshade: 'de sombra demoníaca', Prismatic: 'prismática', Empyrean: 'empírea',
};

Object.assign(CONJUNTOS_ARMADURA, CONJUNTOS_CALAMITY);

const base = conjunto => String(conjunto).replace(/\s+armou?r$/i, '').trim();

/** Rotulo del grupo de armadura en la escalera ("Armadura de plata"). Error si falta: nunca se deja
 *  el nombre ingles de la wiki ni se inventa uno. */
function rotuloConjunto(conjuntoWiki) {
  const es = CONJUNTOS_ARMADURA[base(conjuntoWiki)];
  if (!es) throw new Error('Conjunto de armadura sin nombre en español (scripts/guia-v2/conjuntos.js): ' + conjuntoWiki);
  return 'Armadura ' + es;
}

/** Forma para el texto corrido ("armadura de plata"). */
function armaduraEnTexto(conjuntoWiki) {
  return rotuloConjunto(conjuntoWiki).replace(/^Armadura/, 'armadura');
}

/** Cada conjunto de Calamity tiene que leerse en el nombre de alguna de sus piezas reales
 *  (Mods.CalamityMod.Items.Armor.*.<Conjunto sin espacios>*.DisplayName de la traduccion Keep).
 *  Devuelve la lista de fallos (vacia = OK). */
function verificarConjuntosCalamity(planoEs) {
  const fallos = [];
  for (const [en, es] of Object.entries(CONJUNTOS_CALAMITY)) {
    const nucleo = es.replace(/^(?:de |del )/, '').toLowerCase();
    const re = new RegExp('^Mods\\.CalamityMod\\.Items\\.Armor\\.\\w+\\.' + en.replace(/ /g, '') + '\\w*\\.DisplayName$');
    const piezas = Object.entries(planoEs).filter(([k]) => re.test(k)).map(([, v]) => v);
    if (!piezas.length) fallos.push(en + ': sin piezas en la traduccion');
    else if (!piezas.some(v => v.toLowerCase().includes(nucleo))) fallos.push(en + ': «' + es + '» no aparece en ' + piezas.join(' / '));
  }
  return fallos;
}

module.exports = { CONJUNTOS_ARMADURA, CONJUNTOS_CALAMITY, rotuloConjunto, armaduraEnTexto, verificarConjuntosCalamity };
