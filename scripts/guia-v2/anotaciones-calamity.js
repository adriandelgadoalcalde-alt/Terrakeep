// Guia v2 (F0, 02-oct-2026): anotaciones de la guia Calamity sobre el contenido de la guia del
// usuario (Downloads\Guia-Calamity, solo lectura). Aqui vive lo que su HTML no tiene y la guia
// de Terrakeep/TerrakeepMod si necesita:
//   - titulo en español (los nombres de jefe salen de la localizacion por token);
//   - tipo/opcional, jefes (refs reales), ubicacion objetivo en el mundo real;
//   - condicion de "parada completada" y de cada tarea (o manual), con el vocabulario cerrado del
//     evaluador de Core (ver docs/guia-v2-diseno.md);
//   - "Preparate" valido para TODAS las clases (el texto melee original del usuario se conserva
//     como matiz de cuerpo a cuerpo) y reescrituras puntuales de tareas pensadas solo para melee;
//   - avisos segun el modo (Clasico/Experto/Maestro, Revengeance/Death) con datos del decompilado
//     2.2.4 (p.ej. las bolsas que dan mejoras de Rage/Adrenalina solo en Revengeance:
//     CrabulonBag.cs:80 "CalamityWorld.revenge && !rageBoostOne").
// Las refs ("Mod/NombreInterno") se verifican al generar (generar-referencias.js) contra el
// decompilado: una ref inexistente rompe el validador, nunca se cuela.
'use strict';

const { CONJUNTOS_ARMADURA, armaduraEnTexto } = require('./conjuntos.js');
const B = bandera => ({ tipo: 'bandera', bandera });
const P = (ref, cantidad = 1) => ({ tipo: 'objeto_poseido', ref, cantidad });
const PA = (...refs) => ({ tipo: 'objeto_poseido', refs });
const EQ = (...refs) => (refs.length === 1 ? { tipo: 'equipado', ref: refs[0] } : { tipo: 'equipado', refs });
const VIDA = valor => ({ tipo: 'vida_maxima', valor });
const MEJ = clave => ({ tipo: 'mejora_permanente', clave });
const EST = clave => ({ tipo: 'estado_mundo', clave });
const NPC = ref => ({ tipo: 'npc', ref });
const TODAS = (...condiciones) => ({ tipo: 'todas', condiciones });
const ALGUNA = (...condiciones) => ({ tipo: 'alguna', condiciones });
const M = 'manual';
const Z = id => ({ tipo: 'zona', id });
const PT = id => ({ tipo: 'punto', id });

// Zonas del mundo (nombre oficial: Terraria es-ES Bestiary_Biomes / CalamityKeep-Traduccion-ES Biomes; firma
// con TileID 1.4.4.9 por nombre interno y tiles de Calamity 2.2.4 por clase real).
const zonas = [
  { id: 'superficie', nombre: { terraria: 'Surface' }, capa: 'superficie' },
  { id: 'subsuelo', nombre: { terraria: 'Underground' }, capa: 'subterraneo' },
  { id: 'cavernas', nombre: { terraria: 'Caverns' }, capa: 'cavernas' },
  { id: 'cielo', nombre: { terraria: 'Sky' }, capa: 'espacio' },
  { id: 'nieve', nombre: { terraria: 'Snow' }, tiles: ['SnowBlock', 'IceBlock'], capa: 'superficie' },
  { id: 'desierto', nombre: { terraria: 'Desert' }, tiles: ['Sand'], capa: 'superficie' },
  { id: 'desierto_subterraneo', nombre: { terraria: 'UndergroundDesert' }, tiles: ['HardenedSand', 'Sandstone'], capa: 'subterraneo' },
  { id: 'jungla', nombre: { terraria: 'Jungle' }, tiles: ['JungleGrass'], capa: 'superficie' },
  { id: 'jungla_subterranea', nombre: { terraria: 'UndergroundJungle' }, tiles: ['Mud', 'JungleGrass'], capa: 'cavernas' },
  { id: 'setas', nombre: { terraria: 'UndergroundMushroom' }, tiles: ['MushroomGrass'], capa: 'subterraneo' },
  { id: 'corrupcion', nombre: { terraria: 'TheCorruption' }, tiles: ['Ebonstone', 'CorruptGrass'], capa: 'superficie' },
  { id: 'carmesi', nombre: { terraria: 'Crimson' }, tiles: ['Crimstone', 'CrimsonGrass'], capa: 'superficie' },
  { id: 'colmena', nombre: { objeto: 'Terraria/Hive' }, tiles: ['Hive'], capa: 'cavernas' },
  { id: 'mazmorra', nombre: { terraria: 'TheDungeon' }, tiles: ['BlueDungeonBrick', 'GreenDungeonBrick', 'PinkDungeonBrick'], punto: 'mazmorra' },
  { id: 'inframundo', nombre: { terraria: 'TheUnderworld' }, tiles: ['Ash', 'Hellstone'], capa: 'infierno' },
  { id: 'sagrado', nombre: { terraria: 'TheHallow' }, tiles: ['Pearlstone', 'HallowedGrass'], capa: 'superficie' },
  { id: 'templo', nombre: { terraria: 'TheTemple' }, tiles: ['LihzahrdBrick'], capa: 'cavernas' },
  { id: 'oceano', nombre: { terraria: 'Ocean' }, punto: 'oceano_lado_opuesto' },
  { id: 'mar_sulfuroso', nombre: { calamity: 'SulphurousSea' }, ambito: 'calamity', tilesMod: ['SulphurousSand', 'SulphurousSandstone', 'HardenedSulphurousSandstone'], punto: null, capa: 'superficie' },
  { id: 'abismo', nombre: { calamity: 'Abyss' }, ambito: 'calamity', tilesMod: ['AbyssGravel', 'Voidstone', 'PyreMantle'], capa: 'cavernas' },
  { id: 'abismo_capa4', nombre: { calamity: 'AbyssLayer4Biome' }, ambito: 'calamity', tilesMod: ['Voidstone'], capa: 'cavernas' },
  { id: 'mar_hundido', nombre: { calamity: 'SunkenSea' }, ambito: 'calamity', tilesMod: ['Navystone', 'EutrophicSand', 'HardenedEutrophicSand'], capa: 'subterraneo' },
  { id: 'penascos_azufre', nombre: { calamity: 'BrimstoneCrags' }, ambito: 'calamity', tilesMod: ['BrimstoneSlag', 'ScorchedRemains'], capa: 'infierno' },
  { id: 'infeccion_astral', nombre: { calamity: 'AstralInfection' }, ambito: 'calamity', tilesMod: ['AstralDirt', 'AstralStone', 'AstralGrass'], capa: 'superficie' },
  { id: 'faro_astral', nombre: { objeto: 'CalamityMod/AstralBeaconItem' }, ambito: 'calamity', tilesMod: ['AstralBeacon'], minimo: 1, capa: 'superficie' },
  { id: 'lab_mar_hundido', nombre: { calamity: 'ArsenalLabBiome', sufijo: { calamity: 'SunkenSea' } }, ambito: 'calamity', punto: 'SunkenSeaLabCenter' },
  { id: 'lab_planetoide', nombre: { calamity: 'ArsenalLabBiome', sufijo: { texto: 'planetoide' } }, ambito: 'calamity', punto: 'PlanetoidLabCenter' },
  { id: 'lab_jungla', nombre: { calamity: 'ArsenalLabBiome', sufijo: { terraria: 'Jungle' } }, ambito: 'calamity', punto: 'JungleLabCenter' },
  { id: 'lab_infierno', nombre: { calamity: 'ArsenalLabBiome', sufijo: { terraria: 'TheUnderworld' } }, ambito: 'calamity', punto: 'HellLabCenter' },
  { id: 'lab_hielo', nombre: { calamity: 'ArsenalLabBiome', sufijo: { terraria: 'Snow' } }, ambito: 'calamity', punto: 'IceLabCenter' },
  { id: 'lab_cavernas', nombre: { calamity: 'ArsenalLabBiome', sufijo: { terraria: 'Caverns' } }, ambito: 'calamity', punto: 'CavernLabCenter' },
];

// Glosario: termino ingles de la guia del usuario -> token de zona o texto oficial en español.
const glosario = {
  'Sunken Sea': '{z:mar_hundido}', 'Sulphurous Sea': '{z:mar_sulfuroso}', 'Abyss': '{z:abismo}',
  'Brimstone Crag': '{z:penascos_azufre}', 'Brimstone Crags': '{z:penascos_azufre}',
  'Astral Infection': '{z:infeccion_astral}', 'Astral Beacon': '{o:CalamityMod/AstralBeaconItem}',
  // Con texto propio sin articulo: el nombre oficial ya lo lleva («La Mazmorra», «Lo Sagrado») y
  // la frase del usuario pone el suyo ("delante de Dungeon", "en el Hallow subterráneo").
  'Dungeon': '{z:mazmorra|mazmorra}', 'Underworld': '{z:inframundo|Inframundo}', 'Hallow': '{z:sagrado|Sagrado}', 'Space': '{z:cielo|cielo}',
  // Opcion de configuracion: su etiqueta en CalamityKeep-Traduccion-ES (Configs...EarlyHardmodeProgressionRework.Label,
  // sin el icono [i:Pwnhammer]). La forma antigua de CalamityModEsp que pudiera traer el texto se reescribe igual.
  'Early Hardmode Progression Rework': '«Rediseño del inicio del modo Difícil»',
  'Rework de Progresión del Hardmode Temprano': 'Rediseño del inicio del modo Difícil',
  'Arsenal Lab': 'laboratorio del Arsenal', 'Arsenal Labs': 'laboratorios del Arsenal',
  'Acid Rain': 'Lluvia ácida', 'pre-Hardmode': 'prehardmode', 'Hardmode': 'modo difícil',
  'Expert': 'Experto', 'Treasure Bag': 'bolsa del tesoro', 'Treasure Bags': 'bolsas del tesoro',
  // Eventos: nombre oficial de Terraria es-ES (Game.json, Bestiary_Events / Bestiary_Invasions).
  'Solar Eclipse': 'Eclipse', 'Frost Moon': 'Luna Gélida', 'Pumpkin Moon': 'Luna calabaza',
  'Martian Madness': 'Locura marciana', "Old One's Army": 'Ejército del Antiguo', 'Slime Rain': 'Lluvia de slime',
  'Blood Moon': 'Luna de Sangre', 'Sandstorm': 'Tormenta de arena', 'Frost Legion': 'Legión de escarcha',
  'Pirate Invasion': 'Invasión pirata', 'Goblin Army': 'Invasión duende',
  // Tiles sin nombre oficial en nuestras fuentes (no son objetos): se protegen tal cual para que
  // no se conviertan en el objeto homonimo ("Heart" = el corazon de vida que se recoge).
  // F2b: el tile si tiene nombre oficial, el de su objeto homonimo (CrimsonHeart «Corazón carmesí»,
  // Terraria es-ES Items.json); se deja como texto para no enlazar a la mascota.
  'Crimson Heart': 'Corazón carmesí', 'Crimson Hearts': 'Corazones carmesí',
  // Nombre antiguo del invocador de Yharon (hoy YharonEgg): no es el Huevo de dragon de Terraria.
  'Dragon Egg': 'Dragon Egg',
  // "Solar Flare" en la guia es la armadura lunar, no el enemigo homonimo (NPCID.SolarFlare).
  'Solar Flare': 'armadura de fulguración solar',
  // Exo Mechs es el grupo, no Ares; Apollo es su propio NPC (la ficha del usuario los junta).
  // Nombre del grupo en CalamityKeep-Traduccion-ES (DraedonBag «Caja del tesoro (Exomecas)», BossChecklist «Exomecas»).
  'Exo Mechs': 'Exomecas', 'Apollo': '{n:CalamityMod/Apollo}', 'Artemis': '{n:CalamityMod/Artemis}',
  'The Codebreaker': 'el {o:CalamityMod/CodebreakerBase}',
  // Mecanicas de Revengeance: termino de CalamityKeep-Traduccion-ES (UI.Rage «Rabia», UI.Adrenaline
  // «Adrenalina»; glosario: meter = barra), no la Pocion de furia homonima.
  // F2b: nombres que la guia del usuario deja en ingles y SI tienen nombre oficial: estado de
  // Terraria es-ES (Game.json BuffName.ChaosState), minerales de CalamityKeep-Traduccion-ES (ScoriaOre
  // «Mineral de escoria», CryonicOre «Mineral criónico», PerennialOre «Mineral perenne»), la familia de
  // mecanismos de Draedon (CyanSeekingMechanism «Mecanismo rastreador cian»...), la llave
  // OnyxExcavatorKey «Llave del excavador de ónice» y piezas sueltas de su conjunto.
  'Chaos State': 'Estado de caos', 'Scoria': 'escoria', 'Cryonic': 'criónico', 'Perennial': 'perenne',
  'Seeking Mechanisms': 'mecanismos rastreadores', 'con el Onyx Excavator': 'con el excavador de ónice', 'Onyx Excavator': 'excavador de ónice',
  'Scale Mail': '{o:Terraria/BeetleScaleMail}', 'Ram Mask': '{o:CalamityMod/BloodflareHeadMelee}',
  'Horned Greathelm': '{o:CalamityMod/GodSlayerHeadMelee}', 'Royal Helm': '{o:CalamityMod/AuricTeslaHeadMelee}',
  'Rage': 'Rabia', 'Adrenaline': 'Adrenalina', 'Adrenaline Meter': 'barra de Adrenalina',
  // El texto español del usuario ya nombra estos biomas: se enlazan a su zona.
  'mar sulfuroso': '{z:mar_sulfuroso}', 'mar hundido': '{z:mar_hundido}',
};

// Conjuntos de armadura de Calamity: nombre del conjunto tal como aparece en los nombres de sus
// piezas en CalamityKeep-Traduccion-ES (p.ej. GodSlayerHeadMelee = «Gran yelmo cornudo matadioses»,
// AuricTeslaHeadMelee = «Yelmo real de Tesla áurica»). Forma suelta, en minuscula a mitad de frase.
const CONJUNTOS = {
  'God Slayer': 'matadioses', 'Auric Tesla': 'Tesla áurica', 'Tarragon': 'tarragon', 'Bloodflare': 'sangrellama',
  'Victide': 'victide', 'Statigel': 'statigel', 'Daedalus': 'Dédalo', 'Hydrothermic': 'hidrotermal', 'Demonshade': 'sombra demoníaca',
  'Sulphurous': 'sulfurosa', 'Reaver': 'saqueador', 'Mollusk': 'molusco', 'Astral': 'astral', 'Omega Blue': 'azul omega',
  'Prismatic': 'prismática', 'Empyrean': 'empírea', 'Umbraphile': 'umbrófila', 'Plague Reaper': 'segador de la Plaga',
  'Fathom Swarmer': 'enjambre abisal', 'Snow Ruffian': 'rufián de las nieves', 'Desert Prowler': 'acechador del desierto',
};
// Conjuntos vanilla: [con preposicion para "armadura ...", forma suelta], de los nombres
// oficiales de Terraria es-ES de sus piezas (MoltenHelmet «Casco fundido», BeetleHelmet «Casco de
// escarabajo», SolarFlareHelmet «Casco de fulguración solar»...).
const CONJUNTOS_VANILLA = {
  'Molten': ['fundida', 'fundida'], 'Beetle': ['de escarabajo', 'escarabajo'], 'Chlorophyte': ['de clorofita', 'clorofita'],
  'Hallowed': ['sagrada', 'sagrada'], 'Cobalt': ['de cobalto', 'cobalto'], 'Crimson': ['carmesí', 'carmesí'],
  'Shadow': ['de las sombras', 'de las sombras'], 'Palladium': ['de paladio', 'paladio'], 'Mythril': ['de mithril', 'mithril'],
  'Orichalcum': ['de oricalco', 'oricalco'], 'Adamantite': ['de adamantita', 'adamantita'], 'Titanium': ['de titanio', 'titanio'],
  'Gold': ['de oro', 'oro'], 'Platinum': ['de platino', 'platino'], 'Solar Flare': ['de fulguración solar', 'fulguración solar'],
  'Necro': ['de los muertos', 'de los muertos'], 'Jungle': ['para la selva', 'para la selva'], 'Meteor': ['de meteorito', 'meteorito'],
  'Frost': ['helada', 'helada'], 'Shroomite': ['de piñonita', 'piñonita'], 'Spectre': ['espectral', 'espectral'],
  'Turtle': ['de tortuga', 'tortuga'], 'Spooky': ['tétrica', 'tétrica'], 'Vortex': ['del vórtice', 'vórtice'],
  'Nebula': ['de nebulosa', 'nebulosa'], 'Stardust': ['de polvo estelar', 'polvo estelar'], 'Bee': ['de abeja', 'abeja'],
  'Spider': ['de araña', 'araña'], 'Fossil': ['de fósil', 'fósil'],
};
for (const [en, [conDe]] of Object.entries(CONJUNTOS_VANILLA)) {
  glosario[en + ' armor'] = 'armadura ' + conDe;
  glosario[en + ' armour'] = 'armadura ' + conDe;
}

const glosarioFinal = {};
for (const [en, [, suelta]] of Object.entries(CONJUNTOS_VANILLA)) {
  // Solo las que no son a la vez palabra corriente de otra cosa en la guia ("Jungle", "Frost",
  // "Spider", "Bee", "Crimson", "Gold", "Shadow" se quedan fuera de la forma suelta).
  if (['Jungle', 'Frost', 'Spider', 'Bee', 'Crimson', 'Gold', 'Shadow', 'Turtle', 'Meteor', 'Fossil', 'Spooky', 'Necro'].includes(en)) continue;
  glosarioFinal[en] = suelta;
}
for (const [en, es] of Object.entries(CONJUNTOS)) {
  glosario[en + ' armor'] = 'armadura ' + es;
  glosario[en + ' armour'] = 'armadura ' + es;
  glosarioFinal[en] = es;
}
// F2b: el nombre del conjunto en el texto corrido sale de la MISMA tabla que los rotulos de la
// escalera (scripts/guia-v2/conjuntos.js), derivada de los nombres oficiales de sus piezas.
for (const k of Object.keys(CONJUNTOS_ARMADURA)) {
  glosario[k + ' armor'] = armaduraEnTexto(k);
  glosario[k + ' armour'] = armaduraEnTexto(k);
}

// Clases: el texto melee original de "Preparate" se conserva como matiz de cuerpo a cuerpo.
// preparate = texto valido para TODAS las clases (la escalera de cada clase se enseña aparte).
const paradas = {
  inicio: {
    titulo: 'Tu primera base y tu primer equipo', tipo: 'preparacion',
    ubicaciones: [PT('spawn'), Z('superficie')],
    preparate: 'Armadura de oro o platino (o su equivalente de tu mundo) y un arma que ataque a distancia o con alcance: para cuerpo a cuerpo, espada larga o bumerán; para distancia, arco y flechas; para magia, la primera vara o un libro; para invocación, el primer bastón de esbirros. La escalera de tu clase, abajo, concreta las opciones.',
    vida: { min: 180, max: 200 },
    tareas: {
      1: { condicion: M },
      2: { condicion: ALGUNA(NPC('Terraria/Merchant'), NPC('Terraria/Nurse')), texto: 'Construye casas válidas con paredes de fondo, luz, mesa, silla y entrada, y coloca cofres por materiales, equipo y pociones. La guía lo da por hecho cuando el {n:Terraria/Merchant} o la {n:Terraria/Nurse} se instalan en tu pueblo.' },
      3: { condicion: TODAS(P('Terraria/WorkBench'), P('Terraria/Furnace'), PA('Terraria/IronAnvil', 'Terraria/LeadAnvil')) },
      4: { condicion: TODAS(VIDA(180), { tipo: 'gancho' }) },
      5: { condicion: M },
    },
    completadaCuando: TODAS(VIDA(180), { tipo: 'gancho' }),
  },
  slime: {
    titulo: '{n:Terraria/KingSlime}', tipo: 'jefe', opcional: true, jefes: ['Terraria/KingSlime'],
    ubicaciones: [Z('superficie')],
    preparate: 'Oro o platino y un arma con alcance o proyectil de tu clase: el alcance importa más que perseguirlo cuerpo a cuerpo.',
    vida: { min: 200, max: 240 },
    tareas: {
      1: { condicion: ALGUNA(P('Terraria/SlimeCrown'), B('downedSlimeKing')) },
      2: { condicion: M },
      3: { condicion: ALGUNA(P('Terraria/Solidifier'), B('downedSlimeKing')), texto: 'Derrota al jefe y, en Experto o Maestro, abre su bolsa del tesoro. Conserva el {o:Terraria/Solidifier}.' },
    },
    completadaCuando: B('downedSlimeKing'),
  },
  desert: {
    titulo: '{n:CalamityMod/DesertScourgeHead}', tipo: 'jefe', jefes: ['CalamityMod/DesertScourgeHead'],
    ubicaciones: [Z('desierto'), Z('desierto_subterraneo')],
    preparate: 'Un arma con alcance o proyectil de tu clase; oro o platino basta si controlas la movilidad.',
    vida: { min: 240, max: 280 },
    tareas: {
      1: { condicion: ALGUNA(P('CalamityMod/StormlionMandible', 2), P('CalamityMod/DesertMedallion'), B('downedDesertScourge')) },
      2: { condicion: M },
      3: { condicion: B('downedDesertScourge'), texto: 'Invoca con el {o:CalamityMod/DesertMedallion}. En Experto, atiende a los Desert Nuisances cuando aparezcan.' },
      4: { condicion: P('CalamityMod/PearlShard'), texto: 'Tras ganar, revisa los {o:CalamityMod/PearlShard} y la receta de la armadura de victide (tiene casco para cada clase).' },
    },
    completadaCuando: B('downedDesertScourge'),
    necesitas: [{ ref: 'CalamityMod/DesertMedallion', motivo: 'invocador' }, { ref: 'CalamityMod/StormlionMandible', cantidad: 2, motivo: 'receta del invocador' }],
  },
  sunken: {
    titulo: '{z:mar_hundido} y {n:CalamityMod/GiantClam}', tipo: 'exploracion', jefes: ['CalamityMod/GiantClam'],
    ubicaciones: [Z('mar_hundido'), Z('lab_mar_hundido')],
    preparate: 'Victide es una opción cómoda para el agua (tiene casco para cada clase); no es obligatorio reemplazar una armadura que te funciona.',
    vida: { min: 260, max: 300 },
    tareas: {
      1: { condicion: M },
      2: { condicion: B('downedCLAM') },
      3: { condicion: NPC('CalamityMod/SeaKing') },
      4: { condicion: ALGUNA(P('CalamityMod/EncryptedSchematicSunkenSea'), EST('HasFoundSunkenSeaSchematic')) },
    },
    completadaCuando: TODAS(B('downedCLAM'), EST('HasFoundSunkenSeaSchematic')),
  },
  eye: {
    titulo: '{n:Terraria/EyeofCthulhu}', tipo: 'jefe', jefes: ['Terraria/EyeofCthulhu'],
    ubicaciones: [Z('superficie')],
    preparate: 'Un arma con proyectil de tu clase; oro, platino o Victide. Lleva velocidad y salto adicional.',
    vida: { min: 280, max: 320 },
    tareas: {
      1: { condicion: ALGUNA(P('Terraria/SuspiciousLookingEye'), B('downedBoss1')) },
      2: { condicion: B('downedBoss1') },
      3: { condicion: EQ('Terraria/EoCShield') },
    },
    completadaCuando: B('downedBoss1'),
    avisos: [{ modos: ['clasico', 'viaje'], texto: 'En Clásico no hay bolsa del tesoro ni {o:Terraria/EoCShield}: busca otro dash (por ejemplo, la Capa de Tabi tras Plantera o los accesorios de dash de Calamity) y marca la tarea a mano.' }],
  },
  acid1: {
    titulo: 'Lluvia ácida (nivel 1)', tipo: 'evento', opcional: true,
    ubicaciones: [Z('mar_sulfuroso'), PT('oceano_lado_mazmorra')],
    preparate: 'Un arma con proyectil o esbirros que ataquen a distancia; combate desde plataformas sobre el agua, nunca nadando.',
    vida: { min: 300, max: 360 },
    tareas: {
      1: { condicion: M },
      2: { condicion: M },
      3: { condicion: ALGUNA(B('downedEoCAcidRain'), P('CalamityMod/SulphuricScale')) },
      4: { condicion: M },
    },
    completadaCuando: B('downedEoCAcidRain'),
  },
  crabulon: {
    titulo: '{n:CalamityMod/Crabulon}', tipo: 'jefe', jefes: ['CalamityMod/Crabulon'],
    ubicaciones: [Z('setas')],
    preparate: 'Tu mejor armadura disponible y un arma con alcance; botas, salto adicional y dash.',
    vida: { min: 320, max: 400 },
    tareas: {
      1: { condicion: M },
      2: { condicion: ALGUNA(P('CalamityMod/DecapoditaSprout'), B('downedCrabulon')) },
      3: { condicion: B('downedCrabulon'), texto: 'Deja suficiente anchura y varias alturas para pasar por encima de sus saltos, y derrótalo.' },
    },
    completadaCuando: B('downedCrabulon'),
    avisos: [{ modos: ['revengeance', 'death'], texto: 'En Revengeance su bolsa da {o:CalamityMod/MushroomPlasmaRoot}: consúmelo para ampliar la duración de la Rabia.' }],
  },
  evil: {
    titulo: '{n:Terraria/EaterofWorldsHead} o {n:Terraria/BrainofCthulhu}', tipo: 'jefe', jefes: ['Terraria/EaterofWorldsHead', 'Terraria/BrainofCthulhu'],
    ubicaciones: [{ tipo: 'zona', id: 'corrupcion', siMundo: 'corrupcion' }, { tipo: 'zona', id: 'carmesi', siMundo: 'carmesi' }],
    preparate: 'Un arma con alcance o que golpee a varios enemigos a la vez (perforante, de área o esbirros), y armadura completa.',
    vida: { min: 400, max: 400 },
    tareas: {
      1: { condicion: ALGUNA(P('Terraria/WormFood'), P('Terraria/BloodySpine'), B('downedBoss2')) },
      2: { condicion: M },
      3: { condicion: PA('Terraria/NightmarePickaxe', 'Terraria/DeathbringerPickaxe') },
      4: { condicion: PA('Terraria/WormScarf', 'Terraria/BrainOfConfusion') },
    },
    completadaCuando: B('downedBoss2'),
    avisos: [{ modos: ['clasico', 'viaje'], texto: 'En Clásico no hay bolsa del tesoro: la {o:Terraria/WormScarf} y el {o:Terraria/BrainOfConfusion} son de Experto. Marca esa tarea a mano.' }],
  },
  hive: {
    titulo: '{n:CalamityMod/HiveMind} o {n:CalamityMod/PerforatorHive}', tipo: 'jefe', jefes: ['CalamityMod/HiveMind', 'CalamityMod/PerforatorHive'],
    ubicaciones: [{ tipo: 'zona', id: 'corrupcion', siMundo: 'corrupcion' }, { tipo: 'zona', id: 'carmesi', siMundo: 'carmesi' }],
    preparate: 'La armadura de las sombras o carmesí (o fundida si ya la tienes) y un arma que te deje esquivar a distancia.',
    vida: { min: 400, max: 400 },
    tareas: {
      1: { condicion: ALGUNA(B('downedHiveMind'), B('downedPerforator')) },
      2: { condicion: M },
      3: { condicion: ALGUNA(P('CalamityMod/AerialiteOre'), P('CalamityMod/AerialiteBar')) },
      4: { condicion: M, texto: 'Fabrica la armadura Aerospec con el casco de tu clase si quieres su estilo de movilidad; la armadura fundida es otra opción.' },
    },
    completadaCuando: ALGUNA(B('downedHiveMind'), B('downedPerforator')),
  },
  bee: {
    titulo: '{n:Terraria/QueenBee} y {n:Terraria/Deerclops}', tipo: 'jefe', opcional: true, jefes: ['Terraria/QueenBee', 'Terraria/Deerclops'],
    ubicaciones: [Z('colmena'), Z('nieve')],
    preparate: 'Aerospec o fundida, arma de alcance de tu clase y dash.',
    vida: { min: 400, max: 400 },
    tareas: {
      1: { condicion: B('downedQueenBee') },
      2: { condicion: NPC('Terraria/WitchDoctor'), texto: 'Derrotarla desbloquea al {n:Terraria/WitchDoctor}: vende la {o:Terraria/ImbuingStation} (frascos para armas cuerpo a cuerpo y látigos) y equipo de invocación.' },
      3: { condicion: B('downedDeerclops') },
      4: { condicion: M },
    },
    completadaCuando: B('downedQueenBee'),
  },
  skeletron: {
    titulo: '{n:Terraria/SkeletronHead}', tipo: 'jefe', jefes: ['Terraria/SkeletronHead'],
    ubicaciones: [PT('mazmorra'), Z('mazmorra')],
    preparate: 'Armadura fundida o Aerospec y tu mejor arma de alcance: no dependas de golpear la cabeza de cerca.',
    vida: { min: 400, max: 400 },
    tareas: {
      1: { condicion: B('downedBoss3'), texto: 'Habla con el {n:Terraria/OldMan} por la noche, elige «Maldición» y derrota al jefe. Empieza al anochecer.' },
      2: { condicion: M },
      3: { condicion: NPC('Terraria/Mechanic') },
      4: { condicion: M },
    },
    completadaCuando: B('downedBoss3'),
  },
  abyss1: {
    titulo: 'Primera expedición al {z:abismo}', tipo: 'exploracion', opcional: true,
    ubicaciones: [Z('abismo'), Z('mar_sulfuroso')],
    preparate: 'Bendición de Amidias, movilidad acuática y accesorios de respiración; regreso rápido preparado.',
    vida: { min: 400, max: 400 },
    tareas: {
      1: { condicion: M },
      2: { condicion: M },
      3: { condicion: M },
      4: { condicion: ALGUNA(EST('HasFoundPlanetoidSchematic'), TODAS(P('CalamityMod/CodebreakerBase'), P('CalamityMod/DecryptionComputer'))) },
    },
    completadaCuando: EST('HasFoundPlanetoidSchematic'),
  },
  slimegod: {
    titulo: '{n:CalamityMod/SlimeGodCore}', tipo: 'jefe', jefes: ['CalamityMod/SlimeGodCore'],
    ubicaciones: [Z('superficie')],
    preparate: 'Fundida o Aerospec, un arma que cubra distancia (o esbirros que persigan), botas y dash.',
    vida: { min: 400, max: 400 },
    tareas: {
      1: { condicion: ALGUNA(P('CalamityMod/OverloadedSludge'), B('downedSlimeGod')) },
      2: { condicion: B('downedSlimeGod'), texto: 'Haz una arena más ancha que la de tus primeros jefes, con alturas separadas, y derrótalo.' },
      3: { condicion: M },
      4: { condicion: P('CalamityMod/FracturedArk'), clases: ['cuerpo_a_cuerpo'] },
    },
    completadaCuando: B('downedSlimeGod'),
    avisos: [{ modos: ['revengeance', 'death'], texto: 'En Revengeance su bolsa da {o:CalamityMod/ElectrolyteGelPack}: amplía tu Adrenalina.' }],
  },
  wall: {
    titulo: '{n:Terraria/WallofFlesh}', tipo: 'jefe', jefes: ['Terraria/WallofFlesh'],
    ubicaciones: [Z('inframundo')],
    preparate: 'Fundida o Statigel con el casco de tu clase y un arma que dispare mientras corres.',
    vida: { min: 400, max: 400 },
    tareas: {
      1: { condicion: M },
      2: { condicion: ALGUNA(B('hardMode'), NPC('Terraria/Guide')), texto: 'Comprueba que el {n:Terraria/Guide} está vivo y lanza el {o:Terraria/GuideVoodooDoll} a la lava del infierno para invocar.' },
      3: { condicion: MEJ('demonHeart') },
      4: { condicion: M },
    },
    completadaCuando: B('hardMode'),
    avisos: [{ modos: ['clasico', 'viaje'], texto: 'En Clásico no hay {o:Terraria/DemonHeart}: tendrás cinco huecos de accesorio hasta la {o:CalamityMod/CelestialOnion} de después de la Luna. Marca esa tarea a mano.' }],
  },
  hardstart: {
    titulo: 'Reequípate y sube a 500 de vida', tipo: 'preparacion',
    ubicaciones: [Z('jungla_subterranea'), Z('sagrado'), Z('infeccion_astral')],
    preparate: 'Paladio o cobalto con el casco de tu clase.',
    vida: { min: 500, max: 500 },
    tareas: {
      1: { condicion: M },
      2: { condicion: M, texto: 'Extrae cobalto o paladio con el {o:Terraria/MoltenPickaxe} y fabrica la armadura con el casco de tu clase.' },
      3: { condicion: ALGUNA(VIDA(500), { tipo: 'frutas_vida', valor: 20 }) },
      4: { condicion: M, texto: 'Consigue unas alas tempranas y, si tu clase lo aprovecha, combina garras y guante en el {o:Terraria/PowerGlove}.' },
      5: { condicion: M },
    },
    completadaCuando: VIDA(500),
  },
  queenslime: {
    titulo: '{n:Terraria/QueenSlimeBoss}', tipo: 'jefe', opcional: true, jefes: ['Terraria/QueenSlimeBoss'],
    ubicaciones: [Z('sagrado')],
    preparate: 'Paladio o cobalto, un arma a distancia de tu clase y alas.',
    vida: { min: 500, max: 500 },
    tareas: {
      1: { condicion: ALGUNA(P('Terraria/QueenSlimeCrystal'), B('downedQueenSlime')) },
      2: { condicion: M },
      3: { condicion: M },
    },
    completadaCuando: B('downedQueenSlime'),
  },
  cryogen: {
    titulo: '{n:CalamityMod/Cryogen}', tipo: 'jefe', jefes: ['CalamityMod/Cryogen'],
    ubicaciones: [Z('nieve')],
    preparate: 'Paladio o cobalto y tu mejor arma de alcance de Hardmode temprano.',
    vida: { min: 500, max: 500 },
    tareas: {
      1: { condicion: ALGUNA(P('CalamityMod/CryoKey'), B('downedCryogen')) },
      2: { condicion: M },
      3: { condicion: ALGUNA(NPC('CalamityMod/Archmage'), B('downedCryogen')) },
      4: { condicion: M },
    },
    completadaCuando: B('downedCryogen'),
  },
  mech1: {
    titulo: 'Primer mecánico: {n:Terraria/TheDestroyer}', tipo: 'jefe', jefes: ['Terraria/TheDestroyer'],
    ubicaciones: [Z('superficie')],
    preparate: 'Paladio o cobalto con el casco de tu clase; alcance y espacio vertical. No necesitas equipo del tercer mecánico para empezar.',
    vida: { min: 500, max: 500 },
    tareas: {
      1: { condicion: ALGUNA(P('Terraria/MechanicalWorm'), B('downedMechBossAny')) },
      2: { condicion: B('downedMechBossAny') },
      3: { condicion: M },
      4: { condicion: ALGUNA(EST('HasFoundJungleSchematic'), P('CalamityMod/LongRangedSensorArray')) },
    },
    completadaCuando: B('downedMechBossAny'),
    // Cifras de la propia opcion de Calamity (EarlyHardmodeProgressionRework.Tooltip; nombre de la opcion en CalamityKeep-Traduccion-ES).
    avisos: [
      { modos: ['clasico', 'viaje'], texto: 'Con el «Rediseño del inicio del modo Difícil» activado, el primer jefe mecánico que luches tiene un 20 % menos de vida y daño, y el segundo un 10 % menos.' },
      { modos: ['experto', 'maestro'], texto: 'Con el «Rediseño del inicio del modo Difícil» activado, el primer jefe mecánico que luches tiene un 10 % menos de vida y daño (en Clásico sería un 20 %), y el segundo un 5 % menos.' },
    ],
  },
  aquatic: {
    titulo: '{n:CalamityMod/AquaticScourgeHead} y Lluvia ácida (nivel 2)', tipo: 'jefe', jefes: ['CalamityMod/AquaticScourgeHead'],
    ubicaciones: [Z('mar_sulfuroso'), PT('oceano_lado_mazmorra')],
    preparate: 'Armadura de la etapa mithril/oricalco, un arma que cubra distancia y una plataforma sobre el mar.',
    vida: { min: 500, max: 500 },
    tareas: {
      1: { condicion: B('downedAquaticScourge'), texto: 'Fabrica el {o:CalamityMod/Seafood} e invoca en el {z:mar_sulfuroso}. Puede aparecer de forma natural, pero no necesitas esperarlo.' },
      2: { condicion: B('downedMechBossAny') },
      3: { condicion: B('downedAquaticScourgeAcidRain') },
    },
    completadaCuando: B('downedAquaticScourge'),
  },
  mech2: {
    titulo: 'Segundo mecánico: {n:Terraria/Retinazer} y {n:Terraria/Spazmatism}', tipo: 'jefe', jefes: ['Terraria/Retinazer', 'Terraria/Spazmatism'],
    ubicaciones: [Z('superficie')],
    preparate: 'Oricalco o mithril (o paladio si te funciona), un arma con proyectiles de tu clase y alas.',
    vida: { min: 500, max: 500 },
    tareas: {
      1: { condicion: ALGUNA(P('Terraria/MechanicalEye'), B('downedMechBoss2')) },
      2: { condicion: B('downedMechBoss2') },
      3: { condicion: P('CalamityMod/CryonicOre') },
      4: { condicion: M, texto: 'Valora la armadura Daedalus con el casco de tu clase y el arma de esta etapa de tu escalera (para cuerpo a cuerpo, la {o:CalamityMod/FlarefrostBlade} dispara proyectiles teledirigidos).' },
    },
    completadaCuando: B('downedMechBoss2'),
  },
  brimstone: {
    titulo: '{n:CalamityMod/BrimstoneElemental}', tipo: 'jefe', jefes: ['CalamityMod/BrimstoneElemental'],
    ubicaciones: [Z('penascos_azufre')],
    preparate: 'Adamantita, titanio o Daedalus con el casco de tu clase y un arma que mantenga el alcance.',
    vida: { min: 500, max: 500 },
    tareas: {
      1: { condicion: M },
      2: { condicion: M },
      3: { condicion: B('downedBrimstoneElemental') },
    },
    completadaCuando: B('downedBrimstoneElemental'),
  },
  mech3: {
    titulo: 'Tercer mecánico: {n:Terraria/SkeletronPrime}', tipo: 'jefe', jefes: ['Terraria/SkeletronPrime'],
    ubicaciones: [Z('superficie')],
    preparate: 'Adamantita, titanio o Daedalus y tu mejor arma de alcance.',
    vida: { min: 525, max: 525, trasMejora: true },
    tareas: {
      1: { condicion: ALGUNA(P('Terraria/MechanicalSkull'), B('downedMechBoss3')) },
      2: { condicion: B('downedMechBossAll') },
      3: { condicion: MEJ('bloodOrange') },
      4: { condicion: M, texto: 'Revisa la armadura sagrada con el casco de tu clase, el accesorio de daño de tu escalera y el equipo necesario para el {n:CalamityMod/CalamitasClone}.' },
    },
    completadaCuando: B('downedMechBoss3'),
  },
  clone: {
    titulo: '{n:CalamityMod/CalamitasClone}', tipo: 'jefe', jefes: ['CalamityMod/CalamitasClone'],
    ubicaciones: [Z('superficie')],
    preparate: 'Armadura sagrada o Daedalus con el casco de tu clase y el arma de alcance de tu escalera.',
    vida: { min: 525, max: 525 },
    tareas: {
      1: { condicion: ALGUNA(P('CalamityMod/EyeofDesolation'), B('downedCalamitasClone')) },
      2: { condicion: B('downedCalamitasClone'), texto: 'Prepara una arena ancha, aprende cuándo deja de recibir daño y derrótala.' },
      3: { condicion: P('CalamityMod/AshesofCalamity') },
    },
    completadaCuando: B('downedCalamitasClone'),
  },
  plantera: {
    titulo: '{n:Terraria/Plantera}', tipo: 'jefe', jefes: ['Terraria/Plantera'],
    ubicaciones: [Z('jungla_subterranea')],
    preparate: 'Armadura sagrada, Daedalus o de clorofita con el casco de tu clase y un arma de alcance. Conserva espacio para girar.',
    vida: { min: 525, max: 525 },
    tareas: {
      1: { condicion: M },
      2: { condicion: B('downedPlantBoss'), texto: 'Prepara una cámara antes de invocar, comprueba que conserva la jungla subterránea y derrótala.' },
      3: { condicion: P('Terraria/TempleKey') },
      4: { condicion: P('CalamityMod/PerennialOre') },
    },
    completadaCuando: B('downedPlantBoss'),
  },
  leviathan: {
    titulo: '{n:CalamityMod/Leviathan} y {n:CalamityMod/Anahita}', tipo: 'jefe', opcional: true, jefes: ['CalamityMod/Leviathan', 'CalamityMod/Anahita'],
    ubicaciones: [Z('oceano')],
    preparate: 'El arma post-Plantera de tu escalera y tu armadura actual.',
    vida: { min: 525, max: 525 },
    tareas: {
      1: { condicion: M },
      2: { condicion: B('downedLeviathan') },
      3: { condicion: ALGUNA(P('CalamityMod/Lumenyl'), P('CalamityMod/DepthCells')) },
      4: { condicion: M },
    },
    completadaCuando: B('downedLeviathan'),
  },
  aureus: {
    titulo: '{n:CalamityMod/AstrumAureus}', tipo: 'jefe', opcional: true, jefes: ['CalamityMod/AstrumAureus'],
    ubicaciones: [Z('infeccion_astral')],
    preparate: 'El arma post-Plantera de tu escalera y la armadura de esa etapa.',
    vida: { min: 525, max: 525 },
    tareas: {
      1: { condicion: ALGUNA(P('CalamityMod/AstralChunk'), B('downedAstrumAureus')) },
      2: { condicion: B('downedAstrumAureus'), texto: 'Mantén una arena amplia dentro del bioma, deja margen para sus saltos y derrótalo.' },
      3: { condicion: M },
      4: { condicion: M },
    },
    completadaCuando: B('downedAstrumAureus'),
    avisos: [{ modos: ['revengeance', 'death'], texto: 'En Revengeance su bolsa da {o:CalamityMod/StarlightFuelCell}: amplía tu Adrenalina.' }],
  },
  golem: {
    titulo: '{n:Terraria/Golem}', tipo: 'jefe', jefes: ['Terraria/Golem'],
    ubicaciones: [Z('templo')],
    preparate: 'Armadura post-Plantera con el casco de tu clase y el arma de esa etapa de tu escalera.',
    vida: { min: 550, max: 550, trasMejora: true },
    tareas: {
      1: { condicion: M },
      2: { condicion: B('downedGolemBoss') },
      3: { condicion: P('Terraria/Picksaw') },
      4: { condicion: ALGUNA(P('CalamityMod/ScoriaOre'), P('CalamityMod/LifeAlloy')) },
      5: { condicion: TODAS(MEJ('miracleFruit'), EST('HasFoundHellSchematic')) },
    },
    completadaCuando: B('downedGolemBoss'),
  },
  plague: {
    titulo: '{n:CalamityMod/PlaguebringerGoliath}', tipo: 'jefe', jefes: ['CalamityMod/PlaguebringerGoliath'],
    ubicaciones: [Z('jungla')],
    preparate: 'Escarabajo, Hydrothermic o la armadura de tu clase en esta etapa, y un arma de alcance equivalente.',
    vida: { min: 550, max: 550 },
    tareas: {
      1: { condicion: ALGUNA(P('CalamityMod/Abombination'), B('downedPlaguebringer')) },
      2: { condicion: M },
      3: { condicion: B('downedPlaguebringer') },
    },
    completadaCuando: B('downedPlaguebringer'),
  },
  ravager: {
    titulo: '{n:CalamityMod/RavagerBody}', tipo: 'jefe', opcional: true, jefes: ['CalamityMod/RavagerBody'],
    ubicaciones: [Z('superficie')],
    preparate: 'La armadura de tu clase de esta etapa y ataques a distancia para golpear sus partes sin acercarte demasiado.',
    vida: { min: 550, max: 550 },
    tareas: {
      1: { condicion: ALGUNA(P('CalamityMod/DeathWhistle'), B('downedRavager')) },
      2: { condicion: B('downedRavager') },
      3: { condicion: M },
      4: { condicion: M },
    },
    completadaCuando: B('downedRavager'),
    avisos: [{ modos: ['revengeance', 'death'], texto: 'En Revengeance su bolsa da {o:CalamityMod/InfernalBlood}: amplía la duración de la Rabia.' }],
  },
  hardextras: {
    titulo: '{n:Terraria/DukeFishron}, {n:Terraria/HallowBoss} y eventos', tipo: 'evento', opcional: true, jefes: ['Terraria/DukeFishron', 'Terraria/HallowBoss'],
    ubicaciones: [Z('oceano'), Z('sagrado')],
    preparate: 'La armadura de tu clase de esta etapa. Busca opciones que aporten alcance, vuelo o comodidad.',
    vida: { min: 550, max: 550 },
    tareas: {
      1: { condicion: B('downedFishron') },
      2: { condicion: B('downedEmpressOfLight') },
      3: { condicion: P('Terraria/BrokenHeroSword') },
      4: { condicion: ALGUNA(B('downedMartians'), B('downedDD2EventAnyDifficulty')) },
      5: { condicion: M, texto: 'Elige el arma de esta etapa de tu escalera; para cuerpo a cuerpo, el {o:CalamityMod/TrueArkoftheAncients}, la {o:Terraria/TerraBlade} y el {o:CalamityMod/FallenPaladinsHammer} son objetivos razonables. No necesitas todas.' },
    },
    completadaCuando: TODAS(B('downedFishron'), B('downedEmpressOfLight')),
  },
  cultist: {
    titulo: '{n:Terraria/CultistBoss} y las columnas celestiales', tipo: 'jefe', jefes: ['Terraria/CultistBoss'],
    ubicaciones: [PT('mazmorra')],
    preparate: 'La armadura de tu clase de esta etapa y un arma de alcance; después, el arma con fragmentos de tu clase.',
    vida: { min: 550, max: 550 },
    tareas: {
      1: { condicion: B('downedAncientCultist') },
      2: { condicion: ALGUNA(P('Terraria/LunarCraftingStation'), P('Terraria/FragmentSolar')) },
      3: { condicion: M, texto: 'Usa los fragmentos de tu clase (solar cuerpo a cuerpo, vórtice distancia, nebulosa magia, polvo estelar invocación) para mejorar tu arma y continúa con las otras columnas.' },
      4: { condicion: M },
      5: { condicion: B('downedAstrumDeus') },
    },
    completadaCuando: B('downedAncientCultist'),
  },
  deus: {
    titulo: '{n:CalamityMod/AstrumDeusHead}', tipo: 'jefe', jefes: ['CalamityMod/AstrumDeusHead'],
    ubicaciones: [Z('faro_astral'), Z('infeccion_astral')],
    preparate: 'La armadura de tu clase de esta etapa y tu arma con mejor rendimiento real.',
    vida: { min: 550, max: 550 },
    tareas: {
      1: { condicion: ALGUNA(P('CalamityMod/TitanHeart'), P('CalamityMod/Starcore'), B('downedAstrumDeus')) },
      2: { condicion: B('downedAstrumDeus') },
      3: { condicion: ALGUNA(P('CalamityMod/AstralOre'), P('CalamityMod/AstralBar')) },
    },
    completadaCuando: B('downedAstrumDeus'),
  },
  moon: {
    titulo: '{n:Terraria/MoonLordHead}', tipo: 'jefe', jefes: ['Terraria/MoonLordCore'],
    ubicaciones: [Z('superficie')],
    preparate: 'La armadura de tu clase de esta etapa, el arma con fragmentos y equipo de movilidad fiable.',
    vida: { min: 550, max: 550 },
    tareas: {
      1: { condicion: B('downedTowers') },
      2: { condicion: B('downedMoonlord'), texto: 'Vence al jefe y usa la luminita y los fragmentos para la armadura lunar de tu clase (solar, vórtice, nebulosa o polvo estelar).' },
      3: { condicion: MEJ('extraAccessoryML') },
      4: { condicion: M },
    },
    completadaCuando: B('downedMoonlord'),
    avisos: [{ modos: ['maestro'], texto: 'En Maestro la {o:CalamityMod/CelestialOnion} funciona como un {o:Terraria/DemonHeart} extra (CelestialOnion.cs), así que la guía no puede distinguirla: marca la tarea a mano.' }],
  },
  guardians: {
    titulo: '{n:CalamityMod/ProfanedGuardianCommander}', tipo: 'jefe', jefes: ['CalamityMod/ProfanedGuardianCommander'],
    ubicaciones: [Z('sagrado'), Z('inframundo')],
    preparate: 'La armadura lunar de tu clase y un arma post-lunar con alcance.',
    vida: { min: 550, max: 550 },
    tareas: {
      1: { condicion: P('CalamityMod/UnholyEssence') },
      2: { condicion: ALGUNA(P('CalamityMod/ProfanedShard'), B('downedGuardians')) },
      3: { condicion: B('downedGuardians') },
      4: { condicion: ALGUNA(P('CalamityMod/ProfanedCore'), B('downedProvidence')) },
    },
    completadaCuando: B('downedGuardians'),
  },
  dragonfolly: {
    titulo: '{n:CalamityMod/Dragonfolly}', tipo: 'jefe', opcional: true, jefes: ['CalamityMod/Dragonfolly'],
    ubicaciones: [Z('jungla')],
    preparate: 'La armadura lunar de tu clase y un arma post-lunar.',
    vida: { min: 550, max: 550 },
    tareas: {
      1: { condicion: ALGUNA(P('CalamityMod/ExoticPheromones'), B('downedDragonfolly')) },
      2: { condicion: P('CalamityMod/EffulgentFeather') },
      3: { condicion: M },
    },
    completadaCuando: B('downedDragonfolly'),
    avisos: [{ modos: ['revengeance', 'death'], texto: 'En Revengeance su bolsa da {o:CalamityMod/RedLightningContainer}: amplía la duración de la Rabia.' }],
  },
  providence: {
    titulo: '{n:CalamityMod/Providence}', tipo: 'jefe', jefes: ['CalamityMod/Providence'],
    ubicaciones: [Z('sagrado'), Z('inframundo')],
    preparate: 'La armadura lunar de tu clase, el arma post-lunar de tu escalera, comida y buffs completos.',
    vida: { min: 575, max: 575, trasMejora: true },
    tareas: {
      1: { condicion: B('downedProvidence'), texto: 'Usa el {o:CalamityMod/ProfanedCore} al comienzo del día en {z:sagrado} o {z:inframundo} y derrótala. Evita empezar cuando falta poco para la noche.' },
      2: { condicion: M },
      3: { condicion: P('CalamityMod/UelibloomOre'), texto: 'Tras ganar, busca uelibloom en el barro con un pico adecuado y fabrica la armadura Tarragon con el casco de tu clase.' },
      4: { condicion: TODAS(MEJ('elderBerry'), EST('HasFoundIceSchematic')) },
      5: { condicion: M },
    },
    completadaCuando: B('downedProvidence'),
  },
  markbosses: {
    titulo: 'Los tres jefes de la {o:CalamityMod/MarkofProvidence}', tipo: 'jefe', jefes: ['CalamityMod/StormWeaverHead', 'CalamityMod/CeaselessVoid', 'CalamityMod/Signus'],
    ubicaciones: [Z('cielo'), Z('mazmorra'), Z('inframundo')],
    preparate: 'Tarragon con el casco de tu clase, el arma post-Providence de tu escalera y movilidad.',
    vida: { min: 575, max: 575 },
    tareas: {
      1: { condicion: B('downedStormWeaver') },
      2: { condicion: B('downedCeaselessVoid') },
      3: { condicion: B('downedSignus') },
      4: { condicion: TODAS(P('CalamityMod/ArmoredShell'), P('CalamityMod/DarkPlasma'), P('CalamityMod/TwistingNether')) },
    },
    completadaCuando: TODAS(B('downedStormWeaver'), B('downedCeaselessVoid'), B('downedSignus')),
  },
  polter: {
    titulo: '{n:CalamityMod/Polterghast}', tipo: 'jefe', jefes: ['CalamityMod/Polterghast'],
    ubicaciones: [Z('mazmorra')],
    preparate: 'Tarragon con el casco de tu clase y el arma post-Providence de tu escalera.',
    vida: { min: 575, max: 575 },
    tareas: {
      1: { condicion: ALGUNA(P('CalamityMod/NecroplasmicBeacon'), B('downedPolterghast')) },
      2: { condicion: M },
      3: { condicion: B('downedPolterghast'), texto: 'Usa el Forsaken Archive del fondo de {z:mazmorra|la mazmorra} como base de arena, conserva el bioma y derrótalo.' },
      4: { condicion: M, texto: 'Tras vencer, fabrica la armadura Bloodflare con el casco de tu clase y explora las nuevas recompensas abisales con el equipo apropiado.' },
    },
    completadaCuando: B('downedPolterghast'),
    avisos: [{ modos: ['revengeance', 'death'], texto: 'En Revengeance su bolsa da {o:CalamityMod/Ectoheart}: amplía tu Adrenalina.' }],
  },
  oldduke: {
    titulo: 'Lluvia ácida (nivel 3) y {n:CalamityMod/OldDuke}', tipo: 'jefe', opcional: true, jefes: ['CalamityMod/OldDuke'],
    ubicaciones: [Z('mar_sulfuroso'), PT('oceano_lado_mazmorra')],
    preparate: 'Bloodflare con el casco de tu clase, el arma de la etapa de Polterghast, dash y buenas alas.',
    vida: { min: 575, max: 575 },
    tareas: {
      1: { condicion: ALGUNA(B('downedBoomerDuke'), B('downedMauler'), B('downedNuclearTerror')) },
      2: { condicion: B('downedBoomerDuke') },
      3: { condicion: M },
      4: { condicion: M },
    },
    completadaCuando: B('downedBoomerDuke'),
  },
  dog: {
    titulo: '{n:CalamityMod/DevourerofGodsHead}', tipo: 'jefe', jefes: ['CalamityMod/DevourerofGodsHead'],
    ubicaciones: [Z('superficie')],
    preparate: 'Bloodflare con el casco de tu clase, el arma de esta etapa de tu escalera y un dash fiable.',
    vida: { min: 575, max: 575 },
    tareas: {
      1: { condicion: ALGUNA(P('CalamityMod/CosmicWorm'), B('downedDoG')) },
      2: { condicion: M },
      3: { condicion: B('downedDoG'), texto: 'Practica el dash antes de usarlo para cruzar una carga (la ventana de invulnerabilidad no dura todo el movimiento) y derrótalo.' },
      4: { condicion: P('CalamityMod/CosmicAnvilItem') },
    },
    completadaCuando: B('downedDoG'),
  },
  eventsdog: {
    titulo: 'Repite los tres eventos con botín nuevo', tipo: 'evento',
    ubicaciones: [Z('superficie')],
    preparate: 'Bloodflare camino de God Slayer con el casco de tu clase.',
    vida: { min: 575, max: 575 },
    tareas: {
      1: { condicion: P('CalamityMod/NightmareFuel') },
      2: { condicion: P('CalamityMod/EndothermicEnergy') },
      3: { condicion: P('CalamityMod/DarksunFragment') },
      4: { condicion: P('CalamityMod/AscendantSpiritEssence') },
      5: { condicion: M, texto: 'Fabrica la armadura God Slayer con el casco de tu clase, el accesorio de daño de tu escalera, la {o:CalamityMod/AsgardianAegis} y la mejora de arma que te interese.' },
    },
    completadaCuando: P('CalamityMod/AscendantSpiritEssence'),
  },
  yharon: {
    titulo: '{n:CalamityMod/Yharon}', tipo: 'jefe', jefes: ['CalamityMod/Yharon'],
    ubicaciones: [Z('superficie')],
    preparate: 'God Slayer con el casco de tu clase y el arma post-DoG de tu escalera.',
    vida: { min: 600, max: 600, trasMejora: true },
    tareas: {
      1: { condicion: ALGUNA(P('CalamityMod/YharonEgg'), B('downedYharon')), texto: 'Fabrica el {o:CalamityMod/YharonEgg}. En vídeos antiguos lo verás como Dragon Egg.' },
      2: { condicion: B('downedYharon'), texto: 'Invoca desde un punto marcado (los pilares de fuego delimitan el combate respecto a ese lugar) y derrótalo.' },
      3: { condicion: P('CalamityMod/AuricOre') },
      4: { condicion: TODAS(MEJ('dragonFruit'), P('CalamityMod/AuricQuantumCoolingCell')), texto: 'Consume la {o:CalamityMod/SacredStrawberry}, fabrica la armadura Auric Tesla de tu clase y completa la {o:CalamityMod/AuricQuantumCoolingCell}.' },
      5: { condicion: M, texto: 'Para Auric Tesla conserva Tarragon, Bloodflare y God Slayer de tu clase: cada casco Auric exige los de su clase.' },
    },
    completadaCuando: B('downedYharon'),
  },
  exo: {
    titulo: 'Exomecas', tipo: 'jefe', jefes: ['CalamityMod/AresBody', 'CalamityMod/ThanatosHead', 'CalamityMod/Artemis', 'CalamityMod/Apollo'],
    ubicaciones: [Z('superficie')],
    preparate: 'Auric Tesla de tu clase, el arma post-Yharon de tu escalera, alas tardías y la {o:CalamityMod/AsgardianAegis}.',
    vida: { min: 600, max: 600 },
    tareas: {
      1: { condicion: ALGUNA(EST('TalkedToDraedon'), MEJ('HasTalkedAtCodebreaker')) },
      2: { condicion: M },
      3: { condicion: M },
      4: { condicion: P('CalamityMod/DraedonsForge') },
    },
    completadaCuando: B('downedExoMechs'),
  },
  calamitas: {
    titulo: '{n:CalamityMod/SupremeCalamitas}', tipo: 'jefe', jefes: ['CalamityMod/SupremeCalamitas'],
    ubicaciones: [Z('superficie')],
    preparate: 'Auric Tesla de tu clase y tu mejor arma final.',
    vida: { min: 600, max: 600 },
    tareas: {
      1: { condicion: P('CalamityMod/AltarOfTheAccursedItem') },
      2: { condicion: B('downedCalamitas') },
      3: { condicion: M },
      4: { condicion: ALGUNA(NPC('CalamityMod/BrimstoneWitch'), P('CalamityMod/CeremonialUrn')) },
    },
    completadaCuando: B('downedCalamitas'),
  },
  shadowspec: {
    titulo: 'Shadowspec: tu equipo de cierre', tipo: 'preparacion',
    ubicaciones: [PT('spawn')],
    preparate: 'Demonshade o Auric Tesla de tu clase y una o dos armas finales cómodas.',
    vida: { min: 600, max: 600 },
    tareas: {
      1: { condicion: P('CalamityMod/ShadowspecBar') },
      2: { condicion: M },
      3: { condicion: M },
      4: { condicion: M },
    },
    completadaCuando: P('CalamityMod/ShadowspecBar'),
  },
  rush: {
    titulo: '{o:CalamityMod/Terminus} y Boss Rush', tipo: 'desafio', opcional: true,
    ubicaciones: [Z('abismo_capa4'), Z('abismo')],
    preparate: 'Tu combinación final preferida (Shadowspec/Demonshade o Auric) y preparación completa.',
    vida: { min: 600, max: 600 },
    tareas: {
      1: { condicion: ALGUNA(P('CalamityMod/Terminus'), B('downedBossRush')) },
      2: { condicion: M },
      3: { condicion: B('downedBossRush') },
      4: { condicion: P('CalamityMod/Rock') },
    },
    completadaCuando: B('downedBossRush'),
  },
  wyrm: {
    titulo: '{n:CalamityMod/PrimordialWyrmHead}', tipo: 'desafio', opcional: true, jefes: ['CalamityMod/PrimordialWyrmHead'],
    ubicaciones: [Z('abismo_capa4'), Z('abismo')],
    preparate: 'Equipo final, preparación de respiración y movilidad abisal; no improvises con el equipo del prehardmode.',
    vida: { min: 600, max: 600 },
    tareas: {
      1: { condicion: M },
      2: { condicion: M },
      3: { condicion: B('downedPrimordialWyrm') },
      4: { condicion: P('CalamityMod/HalibutCannon') },
    },
    completadaCuando: B('downedPrimordialWyrm'),
  },
};

// Capitulos de la ruta (eras de la guia del usuario).
const capitulos = [
  { id: 'prehardmode', titulo: 'Antes del modo difícil', resumen: 'De tu primera espada al {n:Terraria/WallofFlesh}.' },
  { id: 'hardmode', titulo: 'Modo difícil', resumen: 'De los mecánicos al {n:Terraria/MoonLordCore}.' },
  { id: 'postluna', titulo: 'Después del Señor de la Luna', resumen: 'De los Guardianes profanados a {n:CalamityMod/Yharon}.' },
  { id: 'finales', titulo: 'Finales y desafíos', resumen: 'Mechas Exo, Calamitas, Boss Rush y secretos.' },
];

// Avisos generales por modo de la partida real (cifras del decompilado 2.2.4).
const avisosModo = [
  { id: 'clasico-bolsas', modos: ['clasico', 'viaje'], texto: 'Tu mundo está en Clásico: los jefes no dan bolsa del tesoro, así que los accesorios exclusivos de Experto (como el {o:Terraria/EoCShield} o el {o:Terraria/DemonHeart}) no aparecerán. Las tareas que los piden se pueden marcar a mano.' },
  { id: 'experto', modos: ['experto'], texto: 'Experto: la ruta de esta guía está pensada para esta dificultad. Revengeance es opcional y se activa con el selector de dificultad de Calamity, sin jefes activos.' },
  { id: 'maestro', modos: ['maestro'], texto: 'Maestro: los jefes pegan más y tienes un hueco de accesorio extra del {o:Terraria/DemonHeart}. Calamity activa Revengeance automáticamente en mundos de Maestro; revisa los avisos de Revengeance.' },
  { id: 'rev', modos: ['revengeance'], texto: 'Revengeance: aparecen las barras de Rabia y Adrenalina, y ciertas bolsas del tesoro dan mejoras permanentes de esas barras. Los avisos de cada parada te dicen cuáles.' },
  { id: 'death', modos: ['death'], texto: 'Death: los jefes y enemigos cambian todavía más. Ve con margen de vida y defensa sobre las cifras de la guía y practica cada pelea antes de gastar invocadores caros.' },
];

module.exports = { paradas, zonas, glosario, glosarioFinal, capitulos, avisosModo };
