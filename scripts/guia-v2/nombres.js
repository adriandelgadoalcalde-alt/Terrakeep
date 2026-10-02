// Guia v2 (F0): sustitucion de nombres ingleses por referencias {o:..}/{n:..}/{z:..} en los textos
// portados de la guia del usuario, para que cada UI pinte el nombre OFICIAL en español + sprite.
//
// Orden de preferencia (el primero que case gana, y siempre la coincidencia mas larga):
//   1. Lista curada del usuario (Guia-Calamity/inventario-sprites.json: 534 fichas con su id
//      verificado contra Terrakeep y Calamity 2.2.4) - la misma que usa su HTML para los sprites.
//   2. Glosario de zonas/eventos/terminos (abajo), con nombre oficial (Terraria es-ES /
//      CalamityModEsp) o token de zona.
//   3. Diccionario completo de nombres ingleses (todos los objetos y NPC de Terraria 1.4.4.9 y
//      Calamity 2.2.4), solo para nombres de 2+ palabras o de 6+ letras que no esten en la
//      lista negra (palabras que en la guia significan otra cosa: "Calamity" es el mod, no el
//      accesorio homonimo).
// Lo que no case se queda tal cual (y el informe de leftovers lo lista para revisar a mano).
'use strict';
const fs = require('fs');
const F = require('./fuentes.js');

const LISTA_NEGRA = new Set([
  'calamity', 'terraria', 'hardmode', 'expert', 'master', 'death', 'revengeance', 'rage', 'adrenaline',
  'abyss', 'dungeon', 'desert', 'jungle', 'ocean', 'hallow', 'corruption', 'crimson', 'underworld', 'space',
  'guide', 'normal', 'classic', 'journey', 'shimmer', 'aether', 'events', 'fishing', 'difficulty', 'boss rush',
  'acid rain', 'sandstorm', 'blood moon', 'solar eclipse', 'slime rain', 'stat meter', 'world generation',
  'configuration', 'turrets', 'shrines', 'planetoid', 'bloodworm', 'adamantite', 'titanium', 'mythril',
  'orichalcum', 'cobalt', 'palladium', 'hallowed', 'luminite', 'auric', 'cosmilite', 'uelibloom',
]);

// Clases renombradas en Calamity 2.2.4 respecto a la ficha del usuario (comprobado en el decompilado:
// NPCs.Bumblebirb/Dragonfolly.cs, NPCs.ExoMechs.Thanatos/ThanatosHead.cs, Items...CosmicAnvilItem.cs).
const RENOMBRES_CALAMITY = {
  'CalamityMod/Bumblebirb': 'CalamityMod/Dragonfolly',
  'CalamityMod/Thanatos': 'CalamityMod/ThanatosHead',
  'CalamityMod/CosmicAnvil': 'CalamityMod/CosmicAnvilItem',
};
const NO_RESUELTOS = [];

function cargarCurada(rutaInventario) {
  const ids = F.ids();
  const inv = JSON.parse(fs.readFileSync(rutaInventario, 'utf8'));
  const alias = new Map(); // alias(minusculas) -> {tipo:'o'|'n', ref}
  // Mismo orden que su sprites-ui.js: las fichas de "Jefes" se aplican las ultimas y ganan.
  const entradas = [...inv.entries].sort((a, b) => Number(a.category === 'Jefes') - Number(b.category === 'Jefes'));
  for (const e of entradas) {
    let ref = null, tipo = e.identType === 'ID de NPC' ? 'n' : 'o';
    let m = /^Terraria\/(Item|NPC)\/(\d+)$/.exec(e.identifier);
    if (m) {
      const nombre = (m[1] === 'Item' ? ids.item : ids.npc).porId[Number(m[2])];
      if (nombre) ref = 'Terraria/' + nombre;
    } else if (/^CalamityMod\/\w+/.test(e.identifier)) {
      ref = e.identifier.split(' + ')[0].trim();
      // Su ficha usa algun nombre de clase anterior a 2.2.4: se corrige al real del decompilado.
      ref = RENOMBRES_CALAMITY[ref] || ref;
      const interno = ref.split('/')[1];
      const c = F.clasesCalamity();
      if (!(tipo === 'n' ? c.NPCs[interno] : c.Items[interno])) { NO_RESUELTOS.push(e.identifier); continue; }
    }
    if (!ref) continue;
    const variantes = new Set(e.aliases.concat([e.name]));
    if (['Materiales', 'Pociones y mejoras'].includes(e.category)) variantes.add(e.name + 's');
    if (e.name.startsWith('Soul of ')) variantes.add(e.name.replace('Soul of ', 'Souls of '));
    for (const a of variantes) {
      if (!a) continue;
      // Una sola palabra: los nombres de conjunto de armadura ("Victide", "Molten") NO son la
      // pieza concreta a la que su ficha apunta; se traducen por el glosario de conjuntos.
      if (!/\s/.test(a) && e.category === 'Armaduras') continue;
      // Una sola palabra en español ("Mecánica", "Gato") o generica en ingles ("Chest"): solo
      // casa con la misma mayuscula inicial (ver crearSustituidor), para no convertir la palabra
      // corriente "mecánica" en el NPC.
      alias.set(a.toLowerCase().replace(/’/g, "'"), { tipo, ref, exacto: !/\s/.test(a) ? a.replace(/’/g, "'") : null });
    }
  }
  return alias;
}

function cargarCompleta() {
  const ids = F.ids();
  const cal = F.nombresCalamity();
  const clases = F.clasesCalamity();
  const alias = new Map();
  const poner = (nombre, valor) => {
    if (!nombre) return;
    const k = nombre.toLowerCase().replace(/’/g, "'");
    const palabras = k.split(/\s+/).length;
    if (LISTA_NEGRA.has(k)) return;
    if (palabras < 2 && k.length < 6) return;
    if (!alias.has(k)) alias.set(k, valor);
    // Plurales ingleses de la guia ("Pearl Shards", "Power Cell Factories", "Stormlions").
    const plural = /y$/.test(k) && !/[aeiou]y$/.test(k) ? k.slice(0, -1) + 'ies' : k + 's';
    if (!alias.has(plural) && !LISTA_NEGRA.has(plural)) alias.set(plural, valor);
  };
  for (const [interno, en] of Object.entries(cal.en.NPCs)) if (clases.NPCs[interno]) poner(en, { tipo: 'n', ref: 'CalamityMod/' + interno });
  for (const [interno, en] of Object.entries(cal.en.Items)) if (clases.Items[interno]) poner(en, { tipo: 'o', ref: 'CalamityMod/' + interno });
  const itemsEn = F.locTerraria('en_US', 'Items').ItemName || {};
  for (const [interno, en] of Object.entries(itemsEn)) if (interno in ids.item.porNombre) poner(en, { tipo: 'o', ref: 'Terraria/' + interno });
  const npcsEn = F.locTerraria('en_US', 'NPCs').NPCName || {};
  for (const [interno, en] of Object.entries(npcsEn)) if (interno in ids.npc.porNombre) poner(en, { tipo: 'n', ref: 'Terraria/' + interno });
  return alias;
}

const escapar = s => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&').replace(/'/g, "['’]");

function patron(claves) {
  const orden = [...claves].sort((a, b) => b.length - a.length).map(escapar);
  return new RegExp('(?<![\\p{L}\\p{N}_{])('+ orden.join('|') + ')(?![\\p{L}\\p{N}_}])', 'giu');
}

// Sustituye en un texto, sin tocar lo que ya esta dentro de un token {...}.
function crearSustituidor({ curada, glosario, completa, glosarioFinal }) {
  const capas = [
    { mapa: curada, re: patron(curada.keys()) },
    { mapa: glosario, re: patron(glosario.keys()) },
    { mapa: completa, re: patron(completa.keys()) },
  ];
  // Ultima capa: nombres sueltos de conjunto ("Tarragon") que, puestos antes, romperian nombres
  // completos de objeto ("Mollusk Husk").
  if (glosarioFinal && glosarioFinal.size) capas.push({ mapa: glosarioFinal, re: patron(glosarioFinal.keys()) });
  const usados = new Map();
  function aplicarCapa(texto, capa, nivel) {
    // Trocea por tokens existentes para no reescribir dentro de ellos.
    return texto.split(/(\{[^}]*\})/).map(trozo => {
      if (trozo.startsWith('{')) return trozo;
      return trozo.replace(capa.re, (m) => {
        const v = capa.mapa.get(m.toLowerCase().replace(/’/g, "'"));
        if (!v) return m;
        if (v.exacto && v.exacto[0] !== m[0]) return m; // palabra suelta: misma mayuscula inicial
        const clave = nivel + '|' + m;
        usados.set(clave, (usados.get(clave) || 0) + 1);
        if (v.tipo === 'texto') return v.texto;
        return `{${v.tipo}:${v.ref}}`;
      });
    }).join('');
  }
  function sustituir(texto) {
    if (!texto) return texto;
    let t = texto;
    capas.forEach((c, i) => { t = aplicarCapa(t, c, i); });
    return t;
  }
  return { sustituir, usados };
}

module.exports = { NO_RESUELTOS, cargarCurada, cargarCompleta, crearSustituidor, patron, LISTA_NEGRA };
