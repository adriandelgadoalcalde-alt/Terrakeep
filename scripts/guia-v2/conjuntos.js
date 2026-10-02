// Guia v2 (F2b, 02-oct-2026): nombres en español de los CONJUNTOS de armadura de las escaleras de
// equipo, comunes a la guia vanilla y a la de Calamity.
//
// Terraria y Calamity no tienen una cadena oficial "nombre del conjunto": el nombre oficial esta en
// cada PIEZA (es-ES de Terraria 1.4.4.9 y CalamityModEsp). Aqui va el complemento que comparten las
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
  // Calamity (nombres de CalamityModEsp; mayusculas tal como las escriben sus piezas)
  Wulfrum: 'de wulfrum', Aerospec: 'de Aerospec', Victide: 'de victida', 'Snow Ruffian': 'de rufián de la nieve',
  'Desert Prowler': 'de merodeador del desierto', Sulphurous: 'sulfúrica', Statigel: 'de estatigel',
  Daedalus: 'de Dédalo', Reaver: 'de saqueador', 'Fathom Swarmer': 'de enjambrador de las profundidades',
  Hydrothermic: 'hidrotérmica', Astral: 'astral', 'Titan Heart': 'de corazón de titán', 'Plague Reaper': 'de segador de la plaga',
  Plaguebringer: 'de portadora de la plaga', 'Lunic Corps': 'del cuerpo lúnico', Brimflame: 'de llama de azufre',
  Umbraphile: 'umbrófila', Fearmonger: 'del Intimidador', Tarragon: 'de Estragón', 'Omega Blue': 'Azul Omega',
  Bloodflare: 'de Llamarada de Sangre', 'God Slayer': 'de Asesino de Dioses', Silva: 'de Silva', 'Gem Tech': 'Tecnogema',
  'Auric Tesla': 'de Tesla Áurica', Demonshade: 'de Sombra Demoníaca', Prismatic: 'Prismática', Empyrean: 'Empírea',
};

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

module.exports = { CONJUNTOS_ARMADURA, rotuloConjunto, armaduraEnTexto };
