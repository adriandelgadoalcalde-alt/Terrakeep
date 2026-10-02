// Guia v2 (F1, 02-oct-2026): contenido y anotaciones de la guia VANILLA (Terraria 1.4.4.9 sin
// mods). A diferencia de la Calamity, no hay HTML del usuario de partida: el contenido se escribe
// aqui y en scripts/guia-v2/vanilla/ con el mismo modelo (docs/guia-v2-diseno.md §2):
//   - vanilla/paradas-1.js, paradas-2.js: la ruta (49 paradas) con sus tareas y condiciones;
//   - vanilla/manual-a.js, manual-b.js: capitulos de manual; vanilla/fichas.js: «Estoy perdido» y
//     «He encontrado algo raro»;
//   - este archivo: capitulos de ruta, zonas (con firma en el mundo real), avisos por modo y los
//     objetos que necesita cada parada con su forma de conseguirlos.
// Marcado de autor: [[Nombre ingles exacto]] -> {o:..}/{n:..} (vanilla/resolver-nombres.js); las
// condiciones usan vanilla/ayudas.js. Toda ref se verifica al generar contra ItemID/NPCID 1.4.4.9.
'use strict';

const capitulos = [
  { id: 'primeros-pasos', titulo: 'Antes del primer jefe', resumen: 'Tu base, tus primeros vecinos, la minería y el [[Eye of Cthulhu]].' },
  { id: 'prehardmode', titulo: 'Antes del modo difícil', resumen: 'Del jefe del mal de tu mundo al [[Wall of Flesh]].' },
  { id: 'hardmode', titulo: 'Modo difícil', resumen: 'Minerales nuevos, los tres jefes mecánicos y [[Plantera]].' },
  { id: 'postplantera', titulo: 'Tras Plantera', resumen: 'El Templo, [[Golem]], las lunas de eventos y los jefes opcionales.' },
  { id: 'finjuego', titulo: 'El Señor de la Luna y después', resumen: 'El [[Lunatic Cultist]], las torres celestiales, el [[Moon Lord]] y los retos finales.' },
];

// Zonas del mundo vanilla. nombre: clave de Bestiary_Biomes (Terraria es-ES oficial), objeto que
// da nombre, MapObject oficial o texto con su fuente. Firma: TileID 1.4.4.9 por nombre interno.
const zonas = [
  { id: 'superficie', nombre: { terraria: 'Surface' }, capa: 'superficie' },
  { id: 'subsuelo', nombre: { terraria: 'Underground' }, capa: 'subterraneo' },
  { id: 'cavernas', nombre: { terraria: 'Caverns' }, capa: 'cavernas' },
  { id: 'cielo', nombre: { terraria: 'Sky' }, tiles: ['Sunplate', 'Cloud', 'RainCloud'], minimo: 20, capa: 'espacio' },
  { id: 'nieve', nombre: { terraria: 'Snow' }, tiles: ['SnowBlock', 'IceBlock'], capa: 'superficie' },
  { id: 'desierto', nombre: { terraria: 'Desert' }, tiles: ['Sand'], capa: 'superficie' },
  { id: 'desierto_subterraneo', nombre: { terraria: 'UndergroundDesert' }, tiles: ['HardenedSand', 'Sandstone'], capa: 'subterraneo' },
  { id: 'jungla', nombre: { terraria: 'Jungle' }, tiles: ['JungleGrass'], capa: 'superficie' },
  { id: 'jungla_subterranea', nombre: { terraria: 'UndergroundJungle' }, tiles: ['Mud', 'JungleGrass'], capa: 'cavernas' },
  { id: 'setas', nombre: { terraria: 'UndergroundMushroom' }, tiles: ['MushroomGrass'], capa: 'subterraneo' },
  { id: 'corrupcion', nombre: { terraria: 'TheCorruption' }, tiles: ['Ebonstone', 'CorruptGrass'], capa: 'superficie' },
  { id: 'carmesi', nombre: { terraria: 'Crimson' }, tiles: ['Crimstone', 'CrimsonGrass'], capa: 'superficie' },
  { id: 'colmena', nombre: { objeto: 'Hive' }, tiles: ['Hive'], capa: 'cavernas' },
  { id: 'mazmorra', nombre: { terraria: 'TheDungeon' }, tiles: ['BlueDungeonBrick', 'GreenDungeonBrick', 'PinkDungeonBrick'], punto: 'mazmorra' },
  { id: 'inframundo', nombre: { terraria: 'TheUnderworld' }, tiles: ['Ash', 'Hellstone'], capa: 'infierno' },
  { id: 'sagrado', nombre: { terraria: 'TheHallow' }, tiles: ['Pearlstone', 'HallowedGrass'], capa: 'superficie' },
  { id: 'templo', nombre: { terraria: 'TheTemple' }, tiles: ['LihzahrdBrick'], capa: 'cavernas' },
  { id: 'oceano', nombre: { terraria: 'Ocean' }, punto: 'oceano_lado_opuesto' },
  { id: 'oceano_mazmorra', nombre: { terraria: 'Ocean', sufijo: { terraria: 'TheDungeon', prefijo: 'lado de' } }, punto: 'oceano_lado_mazmorra' },
  { id: 'granito', nombre: { terraria: 'Granite' }, tiles: ['Granite'], capa: 'cavernas' },
  { id: 'marmol', nombre: { terraria: 'Marble' }, tiles: ['Marble'], capa: 'cavernas' },
  { id: 'meteorito', nombre: { terraria: 'Meteor' }, tiles: ['Meteorite'], minimo: 16, capa: 'superficie' },
  // El Eter no tiene firma de casillas (es piedra + liquido de fulgor): punto aproximado calculado
  // del paso "Shimmer" de WorldGen.cs (GuiaV2Ubicaciones, "eter"). Nombre: el que usa la propia
  // localizacion es-ES («Caja de música (Éter)», «Monolito de éter», «En un bioma de éter»).
  { id: 'eter', nombre: { texto: 'Éter', fuente: 'Terraria es-ES oficial (ItemName.MusicBoxShimmer «Caja de música (Éter)»; tModLoader InAether «En un bioma de éter»)' }, punto: 'eter', capa: 'cavernas' },
  { id: 'altares', nombre: { mapa: ['DemonAltar', 'CrimsonAltar'] }, tiles: ['DemonAltar'], minimo: 1, capa: 'cavernas' },
];

// Avisos generales segun el modo real de la partida.
const avisosModo = [
  { id: 'clasico-bolsas', modos: ['clasico', 'viaje'], texto: 'Tu mundo está en Clásico: los jefes no dan bolsa del tesoro, así que los accesorios exclusivos de Experto (como el [[Shield of Cthulhu]] o el [[Demon Heart]]) no aparecerán. Las tareas que los piden se pueden marcar a mano o dejar sin hacer.' },
  { id: 'experto', modos: ['experto'], texto: 'Experto: los enemigos tienen el doble de vida y hacen el doble de daño que en Clásico, y los jefes dan bolsa del tesoro con un accesorio exclusivo. Las cifras de vida de esta guía están pensadas para este modo.' },
  { id: 'maestro', modos: ['maestro'], texto: 'Maestro: los enemigos tienen el triple de vida y de daño que en Clásico. Tienes un hueco de accesorio más que en Experto y los jefes sueltan reliquias y mascotas propias. Ve con margen de vida y defensa sobre las cifras de la guía.' },
  { id: 'viaje', modos: ['viaje'], texto: 'Viaje: puedes investigar objetos para duplicarlos y ajustar la dificultad de los enemigos. La ruta sigue siendo el orden recomendado; las tareas de «consigue X» se dan por hechas igual si el objeto está en tu inventario.' },
];

// Objetos que necesita cada parada (ademas del invocador, que se añade solo). [nombre, cantidad,
// motivo]. Cuando la forma de conseguirlo NO sale de recetas/botin/tiendas del codigo (cofres del
// mundo, mineria, pesca), el motivo lo dice: la ficha de obtencion de la UI no tiene otra fuente.
const necesitas = {
  inicio: [['Work Bench', 1, 'Primera estación: se fabrica a mano con 10 de madera.'], ['Furnace', 1, 'Para fundir mineral.'], ['Iron Anvil', 1, 'Yunque para armaduras y herramientas (o el [[Lead Anvil]]).']],
  subsuelo: [['Magic Mirror', 1, 'Cofres del subsuelo y las cavernas; también se fabrica en el horno.'], ['Lesser Healing Potion', 5, 'Curación de emergencia.']],
  movilidad: [['Grappling Hook', 1, 'Movilidad básica.'], ['Hermes Boots', 1, 'Cofres del subsuelo y las cavernas.'], ['Cloud in a Bottle', 1, 'Cofres del subsuelo y las cavernas.']],
  slime: [['Gel', 20, 'Ingrediente de la corona; lo sueltan todos los slimes.'], ['Gold Crown', 1, 'Ingrediente de la corona (o la [[Platinum Crown]]).']],
  eye: [['Lens', 6, 'Ingrediente del invocador; la sueltan los ojos demoníacos de noche.']],
  evil: [['Worm Food', 1, 'Invocador si tu mundo es de Corrupción.'], ['Bloody Spine', 1, 'Invocador si tu mundo es de Carmesí.']],
  goblins: [['Goblin Battle Standard', 1, 'Opcional: provoca la invasión.']],
  dd2t1: [['Eternia Crystal Stand', 1, 'Se lo compras al [[n:Tavernkeep]].'], ['o:Eternia Crystal', 1, 'Se lo compras al [[n:Tavernkeep]].']],
  jungla: [['Jungle Spores', 15, 'Plantas brillantes de la jungla subterránea.'], ['Stinger', 12, 'Lo sueltan los avispones.'], ['Vine', 3, 'Lo sueltan las enredaderas de la jungla.']],
  bee: [['Stinger', 1, 'Ingrediente del invocador.'], ['Bottled Honey', 1, 'Ingrediente del invocador.']],
  deerclops: [['Flinx Fur', 3, 'Ingrediente del invocador: la sueltan los Flinx de la nieve subterránea.'], ['Lens', 1, 'Ingrediente del invocador.']],
  pesca: [['Fiberglass Fishing Pole', 1, 'Una caña mejor (cajas de pesca de la jungla).'], ['Fishing Potion', 1, 'Más poder de pesca.']],
  skeletron: [['Ironskin Potion', 1, 'Más defensa durante la pelea.'], ['Regeneration Potion', 1, 'Regeneración durante la pelea.']],
  mazmorra: [['Golden Key', 3, 'La sueltan los enemigos de la Mazmorra; cada cofre dorado gasta una.'], ['Shine Potion', 1, 'Luz para los pasillos oscuros.']],
  antorchas: [['Torch', 101, 'Más de 100 antorchas colocadas cerca de ti bajo tierra.']],
  inframundo: [['Obsidian Skin Potion', 2, 'Inmunidad a la lava mientras picas.'], ['Obsidian', 20, 'Se forma al juntar agua y lava; cada barra infernal lleva una.']],
  armapre: [
    { n: 'Light\'s Bane', motivo: 'Pieza del [[Night\'s Edge]] (Corrupción).', clases: ['cuerpo_a_cuerpo'] },
    { n: 'Blood Butcherer', motivo: 'Pieza del [[Night\'s Edge]] (Carmesí).', clases: ['cuerpo_a_cuerpo'] },
    { n: 'Muramasa', motivo: 'Pieza del [[Night\'s Edge]]: cofres dorados de la Mazmorra.', clases: ['cuerpo_a_cuerpo'] },
    { n: 'Blade of Grass', motivo: 'Pieza del [[Night\'s Edge]].', clases: ['cuerpo_a_cuerpo'] },
    { n: 'Volcano', motivo: 'Pieza del [[Night\'s Edge]]: barras de piedra infernal.', clases: ['cuerpo_a_cuerpo'] },
    { n: 'Handgun', motivo: 'Base del [[Phoenix Blaster]]: cofres dorados de la Mazmorra.', clases: ['distancia'] },
    { n: 'Mana Crystal', c: 9, motivo: 'De 20 a 200 de maná: cada uno se fabrica con 5 [[Fallen Star]].', clases: ['magia'] },
    { n: 'Hellstone Bar', c: 17, motivo: 'Para el [[Imp Staff]].', clases: ['invocacion'] },
    { n: 'Cobalt Shield', motivo: 'Base del [[Obsidian Shield]]: cofres dorados de la Mazmorra.' },
    { n: 'Obsidian Skull', motivo: 'Base del [[Obsidian Shield]].' },
  ],
  wall: [['Ironskin Potion', 1, 'Defensa extra.'], ['Regeneration Potion', 1, 'Regeneración.'], ['Swiftness Potion', 1, 'Velocidad para huir del Muro.']],
  hardstart: [['Pwnhammer', 1, 'Rompe los altares (lo suelta el Muro carnoso).'], ['Soul of Flight', 20, 'Para las alas: la sueltan los guivernos del Cielo en modo difícil.'], ['Feather', 10, 'Para las alas: la sueltan las arpías.']],
  sagrado: [['Purification Powder', 10, 'Limpia pequeñas zonas de corrupción, carmesí o sagrado.']],
  queenslime: [],
  pirates: [],
  frostlegion: [],
  mech1: [['Iron Bar', 5, 'Ingrediente del invocador (o barras de plomo según tu mundo).'], ['Soul of Night', 6, 'Ingrediente del invocador.']],
  mech2: [['Lens', 3, 'Ingrediente del invocador.'], ['Soul of Light', 6, 'Ingrediente del invocador.']],
  mech3: [['Bone', 30, 'Ingrediente del invocador: Mazmorra.'], ['Soul of Light', 3, 'Ingrediente del invocador.'], ['Soul of Night', 3, 'Ingrediente del invocador.']],
  frutas: [['Life Fruit', 20, 'Bulbos amarillos de la jungla subterránea, tras el primer mecánico.'], ['Hallowed Bar', 18, 'Para el [[Pickaxe Axe]] o el [[Drax]].']],
  plantera: [{ n: 'Chlorophyte Bullet', c: 500, motivo: 'Munición que persigue al blanco.', clases: ['distancia'] }],
  mazmorra2: [['Jungle Key', 1, 'Abre el cofre de la jungla de la Mazmorra: la suelta cualquier enemigo de la jungla en modo difícil (1/2500).']],
  templo: [['Temple Key', 1, 'La suelta Plantera.']],
  golem: [],
  fishron: [['Bug Net', 1, 'Para cazar el gusano trufa.']],
  pumpkin: [['Pumpkin', 30, 'Se recogen de las plantas de calabaza, que crecen con la [[Pumpkin Seed]] de la [[n:Dryad]].'], ['Ectoplasm', 5, 'Mazmorra tras Plantera.'], ['Hallowed Bar', 10, 'Jefes mecánicos.']],
  frostmoon: [['Silk', 20, 'Se fabrica con [[Cobweb]] en el [[Loom]].'], ['Ectoplasm', 5, 'Mazmorra tras Plantera.'], ['Soul of Fright', 5, 'Esqueletrón mayor.']],
  moonlord: [['Heart Lantern', 1, 'Regeneración en la arena.'], ['Campfire', 1, 'Regeneración en la arena.']],
  postmoon: [['Luminite Bar', 20, 'Se funde con [[Luminite]] del Señor de la Luna.']],
  mejoras: [['Life Crystal', 1, 'Al fulgor: [[Vital Crystal]].'], ['Life Fruit', 1, 'Al fulgor: [[Aegis Fruit]].'], ['Mana Crystal', 1, 'Al fulgor: [[Arcane Crystal]].'], ['Pink Pearl', 1, 'Al fulgor: [[Galaxy Pearl]]. Sale al abrir [[Oyster]] (rara).'], ['o:Gold Worm', 1, 'Al fulgor: [[Gummy Worm]]. Bicho raro que se caza con red; también sale de una [[Can Of Worms]].'], ['Spell Tome', 1, 'Al fulgor: [[Advanced Combat Techniques: Volume Two]].'], ['Peddler\'s Hat', 1, 'Al fulgor: [[Peddler\'s Satchel]]. Lo suelta el [[Traveling Merchant]] al morir.']],
  zenith: [['Terra Blade', 1, ''], ['Meowmere', 1, ''], ['Star Wrath', 1, ''], ['Influx Waver', 1, ''], ['The Horseman\'s Blade', 1, ''], ['Seedler', 1, ''], ['Starfury', 1, 'Cofres de las islas del Cielo.'], ['Bee Keeper', 1, ''], ['o:Enchanted Sword', 1, 'Santuarios de espada encantada en el subsuelo.'], ['Copper Shortsword', 1, 'La espada inicial.']],
};

module.exports = { capitulos, zonas, avisosModo, necesitas };
