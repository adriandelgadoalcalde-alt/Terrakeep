// Guia v2 (parche 3.4.2 / TerrakeepMod 0.8.2, 3-oct-2026): ULTIMA PASADA de terminos en español sobre el
// texto visible de las dos guias. Nace del hallazgo «busca islas flotantes y Planetoids» (la escalera de
// Calamity enseñaba ingles dentro de un texto en español). Los textos de la guia vienen de cuatro sitios
// (la guia HTML del usuario, las anotaciones, las escaleras de la wiki y la guia vanilla escrita a mano) y
// todos arrastraban jerga o nombres ingleses sueltos: melee, dash, buff, build, Planetoid, Aerialite...
//
// Criterio (el mismo que CalamityKeep-Traduccion-ES/datos/glosario.json, que manda):
//   - Terraria es-ES oficial primero: Icor, Ácido ponzoñoso, Extremo (hardcore), Tinte, Éter, fulgor, Ayuda,
//     Defensivo/Amenazante (prefijos), cofre celestial...
//   - Calamity: la traduccion de la familia (embestida, parada, provocación, súbdito, sigilo, pícaro,
//     Muerte, Maestro, Irradiación, Paso Vernal, Poderío celestial, Réplica de la podredumbre, Evocar...).
//   - Se QUEDAN en su forma original, a proposito y por glosario: Boss Rush y Revengeance (no se traducen), los
//     nombres de otros mods (Boss Checklist, Recipe Browser, Magic Storage), y los nombres propios inventados
//     (uelibloom, shadowspec, victide, statigel, tarragon, aerospec...).
//   - Un termino entre «comillas angulares» es una MENCION deliberada del nombre antiguo o ingles («melee»,
//     «Dragon Egg», «Best»): no se toca. Lo que va dentro de un token {..} tampoco.
//
// Uso: node construir-guia-*.js lo llama sobre el documento final (traducirDocumento). La prueba
// GuiaV2TextoVisibleTests (Core.Tests) comprueba el resultado con una lista de palabras inglesas.
'use strict';

const B = '(?<![\\p{L}\\p{N}_])';
const E = '(?![\\p{L}\\p{N}_])';
const rx = src => new RegExp(B + src + E, 'giu');
const rxs = src => new RegExp(B + src + E, 'gu'); // distingue mayusculas

// [expresion, sustitucion, 'comun'|'propio']. 'comun': si lo encontrado empieza en mayuscula y es inicio de
// frase, la sustitucion empieza en mayuscula tambien. 'propio': se escribe tal cual.
// El ORDEN importa: lo mas largo y lo mas especifico primero.
const REGLAS = [
  // ---- mecanicas y clases (glosario: clases, mecanicas) ----
  [rx('True melee'), 'cuerpo a cuerpo directo', 'comun'],
  [rx('melee speed'), 'velocidad del cuerpo a cuerpo', 'comun'],
  [rxs('Como melee'), 'Si juegas cuerpo a cuerpo,', 'propio'],
  [rxs('como melee'), 'si juegas cuerpo a cuerpo', 'propio'],
  [rx('armadura rogue'), 'armadura de pícaro', 'propio'],
  [rx('opción rogue'), 'opción de pícaro', 'propio'],
  [rx('orientada a rogue'), 'orientada al pícaro', 'propio'],
  [rx('de summon'), 'de invocación', 'propio'],
  [rx('Ranged usa munición, magic usa maná, summon usa esbirros y rogue usa stealth de su equipo'),
    'La clase a distancia usa munición, la de magia usa maná, la de invocación usa súbditos y la de pícaro usa el sigilo de su equipo', 'propio'],
  [rx('Summon item'), 'Objeto de invocación', 'propio'],
  [rx('otra melee'), 'otra arma cuerpo a cuerpo', 'comun'],
  [rx('de clase melee'), 'de la clase cuerpo a cuerpo', 'comun'],
  [rx('ataques a distancia melee'), 'ataques a distancia de armas cuerpo a cuerpo', 'comun'],
  [rx('opción melee Shadowspec'), 'opción cuerpo a cuerpo: Shadowspec', 'comun'],
  [rx('melee'), 'cuerpo a cuerpo', 'comun'],
  [rx('ranged'), 'a distancia', 'comun'],
  [rx('magic'), 'magia', 'comun'],
  [rx('summon'), 'invocación', 'comun'],
  [rx('rogue'), 'pícaro', 'comun'],
  [rx('stealth'), 'sigilo', 'comun'],
  [rx('minion-mixing'), 'mezcla de súbditos', 'comun'],
  [rx('minions'), 'súbditos', 'comun'],
  [rx('minion'), 'súbdito', 'comun'],
  [rx('arma spam'), 'arma de disparo continuo', 'comun'],
  [rx('spam'), 'disparo continuo', 'comun'],
  [rx('el dash'), 'la embestida', 'comun'],
  [rx('un dash temprano'), 'una embestida temprana', 'comun'],
  [rx('un dash equivalente'), 'una embestida equivalente', 'comun'],
  [rx('otro dash fiable'), 'otra embestida fiable', 'comun'],
  [rx('dash mejorado'), 'embestida mejorada', 'comun'],
  [rx('un dash fiable'), 'una embestida fiable', 'comun'],
  [rx('un dash'), 'una embestida', 'comun'],
  [rx('otro dash'), 'otra embestida', 'comun'],
  [rx('un dash más rápido'), 'una embestida más rápida', 'comun'],
  [rx('dash más rápido'), 'embestida más rápida', 'comun'],
  [rx('dashes'), 'embestidas', 'comun'],
  [rx('dash'), 'embestida', 'comun'],
  [rx('el parry'), 'la parada', 'comun'],
  [rx('parry'), 'parada', 'comun'],
  [rx('cooldown'), 'recarga', 'comun'],
  [rx('aggro'), 'provocación', 'comun'],
  [rx('buffs'), 'potenciadores', 'comun'],
  [rx('buff'), 'potenciador', 'comun'],
  [rx('builds'), 'configuraciones', 'comun'],
  [rx('build'), 'configuración', 'comun'],
  [rx('boomerangs'), 'bumeranes', 'comun'],
  [rx('ticks'), 'ciclos', 'comun'],
  [rx('tier'), 'nivel', 'comun'],
  [rx('Probes'), 'las sondas', 'propio'],
  [rx('pets'), 'mascotas', 'comun'],
  [rx('dyes'), 'tintes', 'comun'],
  [rx('mounts'), 'monturas', 'comun'],
  [rx('lore'), 'trasfondo', 'comun'],
  [rx('hardcore'), 'extremo', 'comun'],
  [rx('Bullet hell'), 'lluvia de balas', 'comun'],
  [rx('Non-consumable'), 'No consumible', 'propio'],
  [rx('Crafting station'), 'Estación de fabricación', 'propio'],
  [rx('Schematic / decrypt'), 'Esquema / descifrar', 'propio'],
  [rx('DR · damage reduction'), 'RD · reducción de daño', 'propio'],
  [rx('Enrage'), 'Enfurecimiento', 'propio'],

  // ---- modos y estados (UI.Death «Muerte», UI.Master, Buffs.Irradiated «Irradiación») ----
  [rx('Master/Revengeance'), 'Maestro/Revengeance', 'propio'],
  [rx('Master'), 'Maestro', 'propio'],
  [rx('Death'), 'Muerte', 'propio'],
  [rx('Irradiated'), 'Irradiación', 'propio'],

  // ---- bioma, estructuras y materiales de Calamity ----
  [rx('esquema Planetoid'), 'esquema del planetoide', 'propio'],
  [rx('Planetoids'), 'planetoides', 'comun'],
  [rx('Planetoid'), 'planetoide', 'comun'],
  [rx('esquema Ice'), 'esquema de hielo', 'propio'],
  [rx('esquema Jungle'), 'esquema de la jungla', 'propio'],
  [rx('\\*\\*Vernal Pass'), '**Paso Vernal', 'propio'],
  [rx('en Vernal Pass'), 'en el Paso Vernal', 'propio'],
  [rx('Vernal Pass'), 'el Paso Vernal', 'comun'],
  [rx('\\*\\*Forsaken Archive'), '**Archivo abandonado', 'propio'],
  [rx('el Forsaken Archive'), 'el Archivo abandonado', 'propio'],
  [rx('Forsaken Archive'), 'el Archivo abandonado', 'comun'],
  [rx('\\*\\*Evil Island'), '**Isla del mal', 'propio'],
  [rx('\\*\\*Shrines'), '**Santuarios', 'propio'],
  [rx('Aerialite'), 'aerialita', 'comun'],
  [rx('Cosmilite'), 'cosmilita', 'comun'],
  [rx('casco Auric'), 'casco áurico', 'propio'],
  [rx('armas Auric'), 'armas áuricas', 'propio'],
  [rx('requiere Auric'), 'requiere la Tesla áurica', 'propio'],
  [rx('Auric'), 'Tesla áurica', 'propio'],
  [rxs('Para Tesla áurica'), 'Para la Tesla áurica', 'propio'],
  [rx('Seraph con'), 'Estelas seráficas con', 'propio'],
  [rx('Celestial Tracers'), 'Estelas celestiales', 'propio'],
  [rx('Elysian Tracers'), 'Estelas elíseas', 'propio'],
  [rx('Supreme Ultramage, Permafrost'), 'Ultramago supremo, Permafrost', 'propio'],
  [rx('Flarefrost'), 'la hoja de fuego y escarcha', 'propio'],
  [rx('Heaven\'s Might'), 'Poderío celestial', 'propio'],
  [rx('Decay\'s Retort'), 'Réplica de la podredumbre', 'propio'],
  [rx('Lunar Fragments'), 'fragmentos lunares', 'propio'],
  [rx('Amidias’ Blessing'), 'la Bendición de Amidias', 'propio'],
  [rx('Aether \\/ Shimmer'), 'Éter / fulgor', 'propio'],
  [rx('Aether/Shimmer'), 'Éter/fulgor', 'propio'],
  [rx('En Aether, Shimmer'), 'En el Éter, el fulgor', 'propio'],
  [rx('santuario Shimmer'), 'santuario del fulgor', 'propio'],
  [rx('Aether'), 'Éter', 'propio'],
  [rx('Shimmer'), 'fulgor', 'comun'],
  [rx('Weaver'), 'Tejetormentas', 'propio'],
  [rx('Void'), 'Vacío', 'propio'],
  [rx('su Cyst'), 'su quiste', 'propio'],
  [rx('Souls de'), 'Almas de', 'propio'],
  [rx('la Mark'), 'la Marca de Providencia', 'propio'],
  [rx('recibe Rock'), 'recibe la Roca', 'propio'],
  [rx('Rock,'), 'Roca,', 'propio'],
  [rx('Piggy'), 'Cerdito', 'propio'],
  [rx('Vile \\/'), 'Polvo vil /', 'propio'],
  [rx('Warding'), 'Defensivo', 'propio'],
  [rx('Menacing'), 'Amenazante', 'propio'],
  [rx('Enchanting'), 'Encantar', 'propio'],
  [rx('botón Evoke'), 'botón Evocar', 'propio'],
  [rx('Help concede'), 'Su opción Ayuda concede', 'propio'],
  [rx('Help'), 'Ayuda', 'propio'],
  [rx('Goblin Summoner'), 'duende hechicero', 'comun'],
  [rx('Flinx'), 'copitos', 'comun'],
  [rx('Acid Venom'), 'Ácido ponzoñoso', 'propio'],
  [rx('el Jungle\'s Fury'), 'la Furia de la jungla', 'propio'],
  [rx('Jungle\'s Fury'), 'Furia de la jungla', 'propio'],
  [rx('Biome Key'), 'llaves de bioma', 'propio'],
  [rx('Solar Eclipse'), 'Eclipse', 'propio'],
  [rx('pre-Plantera'), 'anterior a Plantera', 'propio'],
  [rx('post-Plantera'), 'posterior a Plantera', 'propio'],
  [rx('taller del Goblin'), 'taller del Duende chapucero', 'propio'],
  [rx('Descifra Ice'), 'Descifra el esquema de hielo', 'propio'],
  [rx('Counterweights'), 'contrapesos', 'comun'],
  [rx('instead of'), 'en lugar de', 'propio'],
  [rx('Bloodworms'), 'gusanos de sangre', 'comun'],
  [rx('campfire'), 'hoguera', 'comun'],
  [rx('Exodium'), 'exodio', 'comun'],
  [rx('defense damage'), 'daño a la defensa', 'comun'],
  [rx('Yoyo'), 'Yoyó', 'propio'],
  [rx('y Rock como'), 'y la Roca como', 'propio'],
  [rx('vanilla'), 'del juego base', 'comun'],
  [rx('Dragonfruit'), '«Dragonfruit»', 'propio'],
  [rx('casco Scale Mail'), 'con la cota de malla de escarabajo', 'propio'],
  [rx('Skyware'), 'celestiales', 'propio'],
  [rx('ichor'), 'icor', 'comun'],
  [rx('Shadow\\/Crimson'), 'de las sombras/carmesí', 'comun'],
  [rx('Los Ark'), 'Las Arcas', 'propio'],
  [rx('Dragon Egg'), '«Dragon Egg»', 'propio'],
  [rx('Rune of Kos'), '«Rune of Kos»', 'propio'],
  [rx('Greatsword of Blah'), '«Greatsword of Blah»', 'propio'],
];

// Mencion deliberada: lo que va entre «..» o dentro de un token {..} no se toca.
function aplicarATrozo(t, esInicio) {
  let r = t;
  for (const [re, nuevo, tipo] of REGLAS) {
    re.lastIndex = 0;
    r = r.replace(re, (m, ...resto) => {
      const offset = resto[resto.length - 2];
      const todo = resto[resto.length - 1];
      let s = nuevo;
      if (tipo === 'comun' && /^\p{Lu}/u.test(m) && /^\p{Ll}/u.test(s)) {
        const antes = todo.slice(0, offset);
        const inicioFrase = (offset === 0 && esInicio) || /(^|[.!?:;·|]\s*|\*\*|\(\s*|¿|¡)$/u.test(antes) && !/[;]\s*$/.test(antes);
        if (inicioFrase) s = s[0].toUpperCase() + s.slice(1);
      }
      return s;
    });
  }
  return r;
}

function aEspanol(texto) {
  if (typeof texto !== 'string' || !/[A-Za-z]/.test(texto)) return texto;
  // El abreviado «** ’s Abandoned Shed.**» abarca un token: se trata antes de partir.
  let t = texto.replace(/(\{n:[^}]+\})[’']s Abandoned Shed/g, 'Cobertizo abandonado de $1');
  t = t.replace(/Holy \{o:Terraria\/InfernoPotion\}/g, 'Infierno sagrado');
  t = t.replace(/pescando con Bloodworm/g, 'pescando con {o:CalamityMod/BloodwormItem}');
  t = t.replace(/Supreme Witch (\{n:CalamityMod\/SupremeCalamitas\})/g, '$1');
  t = t.replace(/(\{o:Terraria\/Furnace\}) Anvil/g, '$1 {o:Terraria/IronAnvil}');
  t = t.replace(/(\{o:CalamityMod\/EssenceofEleum\}) \/ Havoc \/ Sunlight/g, '$1 / {o:CalamityMod/EssenceofHavoc} / {o:CalamityMod/EssenceofSunlight}');
  t = t.replace(/da buff infinito/g, 'da el potenciador infinito');
  t = t.replace(/maza con cadena \(flail\)/g, 'maza con cadena');
  t = t.replace(/Armored (\{n:Terraria\/DiggerHead\})/g, 'Excavador blindado');
  const partes = t.split(/(\{[^}]*\}|«[^»]*»|Magic Storage|Boss Rush|Boss Checklist|Recipe Browser|Get fixed boi|Guide:Class setups)/);
  return partes.map((p, i) => (i % 2 === 1 ? p : aplicarATrozo(p, i === 0))).join('');
}

// Claves del JSON cuyo contenido ve el jugador (mismo criterio que GuiaV2TextoVisibleTests.Visibles). El resto
// (ids, enlaces, banderas, estilos, el nombre de pagina de la wiki...) es interno y NO se traduce.
const VISIBLES = new Set(['titulo', 'subtitulo', 'resumen', 'texto', 'donde', 'preparate', 'combate', 'desbloquea', 'listoCuando',
  'conserva', 'etiqueta', 'notas', 'nota', 'motivo', 'rol', 'conjunto', 'marcas', 'momento', 'nombre', 'items', 'cabeceras',
  'filas', 'origen', 'creditos', 'preparatePorClase', 'leyendaEscaleras', 'cuerpo_a_cuerpo', 'distancia', 'magia', 'invocacion', 'picaro', 'necesitas']);
const INTERNAS = new Set(['pagina', 'ref', 'refs', 'id', 'ambito', 'bandera', 'desde', 'estilo', 'tipo', 'url', 'fuentes', 'fuenteNombre',
  'clave', 'calamity', 'terraria', 'objeto', 'npc', 'punto', 'capa', 'tiles', 'tilesMod', 'zona', 'completadaCuando', 'condicion', 'condiciones']);

function traducirDocumento(x, visible = false) {
  if (typeof x === 'string') return visible ? aEspanol(x) : x;
  if (Array.isArray(x)) return x.map(v => traducirDocumento(v, visible));
  if (x && typeof x === 'object') {
    const r = {};
    for (const [k, v] of Object.entries(x)) {
      if (INTERNAS.has(k)) r[k] = v;
      else r[k] = traducirDocumento(v, visible || VISIBLES.has(k));
    }
    return r;
  }
  return x;
}

module.exports = { aEspanol, traducirDocumento, REGLAS };
