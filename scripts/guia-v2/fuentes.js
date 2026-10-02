// Guia v2 (F0, 02-oct-2026): cargadores de las FUENTES REALES que usan los scripts de la guia v2.
// Ningun nombre ni id se escribe a mano: todo sale de aqui.
//   - tModLoader 1.4.4.9 decompilado (la version que corre TerrakeepMod): ItemID/NPCID/TileID y
//     su localizacion oficial incrustada (Terraria.Localization.Content.<idioma>.*.json).
//   - Calamity 2.2.4: clases reales (decompilado con ilspycmd del CalamityMod.dll del .tmod
//     instalado) y su Localization/en-US del .tmod.
//   - Nombres de Calamity en español: la traduccion PROPIA de la familia Keep,
//     CalamityKeep-Traduccion-ES (repo hermano Downloads\Keep\CalamityKeep-Traduccion-ES, carpeta
//     CalamityKeepTraduccionES/Localization/es-ES, GENERADA y verificada 1 a 1 contra Calamity 2.2.4:
//     9 793/9 793 claves). Hasta el 2-oct-2026 se leia CalamityModEsp (workshop 2829795471), que dejaba
//     186 objetos y 21 NPC sin traducir y no usa nuestro glosario (T6 de CalamityKeep-Traduccion-ES).
// Rutas locales de este PC: los scripts son de mantenimiento, no forman parte de la build.
'use strict';
const fs = require('fs');
const path = require('path');

// Rutas relativas a la carpeta del usuario (o por variable de entorno), nunca escritas a mano.
const os = require('os');
const KEEP = process.env.KEEP_DIR || path.join(os.homedir(), 'Downloads', 'Keep');
const DECOMP = process.env.TK_DECOMPILADO || path.join(KEEP, 'tModLoader-Decompiled');
const TML = path.join(DECOMP, 'tModLoader');
const CAL = path.join(DECOMP, 'CalamityMod-2.2.4');
const TMOD_EXTRACT = path.join(KEEP, 'Terrasavr-Win', 'Terrasavr-Calamity-Beta', 'resources', 'app', 'tmod-extract.js');
const WORKSHOP = process.env.TK_WORKSHOP || path.join('C:\\', 'Program Files (x86)', 'Steam', 'steamapps', 'workshop', 'content', '1281930');
const TMOD_CALAMITY = path.join(WORKSHOP, '2824688072', '2026.6', 'CalamityMod.tmod');
const TRADUCCION_CALAMITY = process.env.TK_TRADUCCION_CALAMITY || path.join(KEEP, 'CalamityKeep-Traduccion-ES', 'CalamityKeepTraduccionES');

function leerConstantes(archivo) {
  const txt = fs.readFileSync(archivo, 'utf8');
  const porNombre = {}, porId = {};
  const re = /public const (?:short|int|ushort) (\w+) = (-?\d+);/g;
  let m;
  while ((m = re.exec(txt))) {
    const id = Number(m[2]);
    if (m[1] === 'Count') continue;
    if (!(m[1] in porNombre)) porNombre[m[1]] = id;
    if (!(id in porId)) porId[id] = m[1];
  }
  const cuenta = /public static readonly (?:short|int|ushort) Count = (\d+);/.exec(txt);
  return { porNombre, porId, cuenta: cuenta ? Number(cuenta[1]) : null };
}

let _ids;
function ids() {
  if (!_ids) _ids = {
    item: leerConstantes(path.join(TML, 'Terraria', 'ID', 'ItemID.cs')),
    npc: leerConstantes(path.join(TML, 'Terraria', 'ID', 'NPCID.cs')),
    tile: leerConstantes(path.join(TML, 'Terraria', 'ID', 'TileID.cs')),
  };
  return _ids;
}

// JSON de localizacion de Terraria: admite comas finales y comentarios.
function jsonTolerante(txt) {
  txt = txt.replace(/^\uFEFF/, '').replace(/^\s*\/\/.*$/gm, '').replace(/,(\s*[}\]])/g, '$1');
  return JSON.parse(txt);
}

const _loc = {};
function locTerraria(idioma, archivo) {
  const k = idioma + archivo;
  if (!_loc[k]) _loc[k] = jsonTolerante(fs.readFileSync(path.join(TML, `Terraria.Localization.Content.${idioma}.${archivo}.json`), 'utf8'));
  return _loc[k];
}

// {$ItemName.X} y similares dentro de un nombre (pocos casos).
function resolverCopias(texto, idioma) {
  return String(texto).replace(/\{\$(\w+)\.(\w+)\}/g, (all, sec, clave) => {
    for (const f of ['Items', 'NPCs', 'Game', 'Legacy', 'Main']) {
      const d = locTerraria(idioma, f);
      if (d[sec] && d[sec][clave]) return d[sec][clave];
    }
    return all;
  });
}

function nombreObjetoVanilla(interno, idioma) {
  const v = locTerraria(idioma, 'Items').ItemName?.[interno];
  return v ? resolverCopias(v, idioma) : null;
}
function nombreNpcVanilla(interno, idioma) {
  const v = locTerraria(idioma, 'NPCs').NPCName?.[interno];
  return v ? resolverCopias(v, idioma) : null;
}

// ---- hjson (parser minimo, misma forma que scripts/extraer-bonos-set-calamity.js)
function parseHjson(text) {
  let i = 0; const n = text.length;
  function skipWs() {
    while (i < n) {
      const c = text[i];
      if (c === ' ' || c === '\t' || c === '\r' || c === '\n') { i++; continue; }
      if ((c === '/' && text[i + 1] === '/') || c === '#') { while (i < n && text[i] !== '\n') i++; continue; }
      break;
    }
  }
  function readKey() { skipWs(); const s = i; while (i < n && !/[\s:{}]/.test(text[i])) i++; return text.substring(s, i); }
  function readValue() {
    skipWs();
    const triple = "'''";
    if (text.startsWith(triple, i)) {
      i += 3; const e = text.indexOf(triple, i); const b = text.substring(i, e); i = e + 3;
      return b.split('\n').map(l => l.trim()).filter(l => l !== '').join('\n');
    }
    if (text[i] === '"') { i++; const s = i; while (i < n && text[i] !== '"') { if (text[i] === '\\') i++; i++; } const v = text.substring(s, i); i++; return v; }
    if (text[i] === '{') return readObject();
    const s = i; while (i < n && text[i] !== '\n') i++; return text.substring(s, i).trim();
  }
  function readObject() {
    const o = {}; skipWs(); if (text[i] === '{') i++;
    for (;;) { skipWs(); if (i >= n) break; if (text[i] === '}') { i++; break; } const k = readKey(); skipWs(); if (text[i] === ':') i++; o[k] = readValue(); }
    return o;
  }
  return readObject();
}
function aplanar(obj, prefijo, out) {
  for (const [k, v] of Object.entries(obj)) {
    const key = prefijo ? prefijo + '.' + k : k;
    if (typeof v === 'string') out[key] = v; else aplanar(v, key, out);
  }
  return out;
}

let _calLoc;
function locCalamity() {
  if (_calLoc) return _calLoc;
  const { readTmod } = require(TMOD_EXTRACT);
  const cargar = (tmod, carpeta) => {
    const m = readTmod(tmod), plano = {};
    for (const [nombre, buf] of m.files) {
      if (!nombre.startsWith(carpeta) || !nombre.endsWith('.hjson')) continue;
      const base = path.basename(nombre, '.hjson');
      aplanar(parseHjson(buf.toString('utf8').replace(/^\uFEFF/, '')), base, plano);
    }
    return { plano, version: m.version };
  };
  const en = cargar(TMOD_CALAMITY, 'Localization/en-US/');
  const es = cargarTraduccionKeep();
  _calLoc = { en: en.plano, es: es.plano, versionCalamity: en.version, versionEsp: es.version };
  return _calLoc;
}

// CalamityKeep-Traduccion-ES: sus .hjson GENERADOS son JSON estricto (con comentarios // de
// cabecera); la clave es el nombre del archivo + la ruta, igual que en tModLoader. Las referencias
// {$Clave} se resuelven como tModLoader: primero con el prefijo de la clave que la contiene (de mas
// largo a mas corto) y despues tal cual. Sin la carpeta, error claro: nunca se cae a otra fuente.
function cargarTraduccionKeep() {
  const dir = path.join(TRADUCCION_CALAMITY, 'Localization', 'es-ES');
  if (!fs.existsSync(dir)) throw new Error('No existe la traduccion de Calamity de la familia Keep: ' + dir + ' (repo CalamityKeep-Traduccion-ES)');
  const plano = {};
  for (const f of fs.readdirSync(dir).filter(x => x.endsWith('.hjson')).sort()) {
    const txt = fs.readFileSync(path.join(dir, f), 'utf8').replace(/^\uFEFF/, '').replace(/^\s*\/\/.*$/gm, '');
    aplanar(JSON.parse(txt), path.basename(f, '.hjson'), plano);
  }
  const resolver = (clave, valor, prof) => (prof > 8 ? valor : valor.replace(/\{\$([\w.]+)\}/g, (todo, ref) => {
    const partes = clave.split('.').slice(0, -1);
    for (let i = partes.length; i >= 0; i--) {
      const c = (i ? partes.slice(0, i).join('.') + '.' : '') + ref;
      if (c in plano) return resolver(c, plano[c], prof + 1);
    }
    return todo;
  }));
  for (const k of Object.keys(plano)) if (plano[k].includes('{$')) plano[k] = resolver(k, plano[k], 0);
  const build = fs.readFileSync(path.join(TRADUCCION_CALAMITY, 'build.txt'), 'utf8');
  const version = (/^version\s*=\s*(\S+)/m.exec(build) || [])[1] || '?';
  return { plano, version };
}

function indiceCalamity(plano) {
  const idx = { Items: {}, NPCs: {}, Biomes: {} };
  for (const [k, v] of Object.entries(plano)) {
    let m = /^Mods\.CalamityMod\.(Items|NPCs)\.(?:[\w.]+\.)?(\w+)\.DisplayName$/.exec(k);
    if (m) { idx[m[1]][m[2]] = v; continue; }
    m = /^Mods\.CalamityMod\.Biomes\.(\w+)(?:\.DisplayName)?$/.exec(k);
    if (m) idx.Biomes[m[1]] = v;
  }
  return idx;
}
let _idxCal;
function nombresCalamity() {
  if (_idxCal) return _idxCal;
  const l = locCalamity();
  _idxCal = { en: indiceCalamity(l.en), es: indiceCalamity(l.es), versionCalamity: l.versionCalamity, versionEsp: l.versionEsp, planoEn: l.en, planoEs: l.es };
  // Clases que NO tienen DisplayName propio en la localizacion porque lo toman de otra en el codigo
  // (p.ej. AstralachneaWall.cs:27 "DisplayName => CalamityUtils.GetText("NPCs.AstralachneaGround.DisplayName")",
  // PhantomSpiritL/M/S, PlagueChargerLarge): mismo nombre que la clave a la que apuntan.
  const clases = clasesCalamity();
  for (const tipo of ['Items', 'NPCs']) {
    for (const [interno, rel] of Object.entries(clases[tipo])) {
      if (_idxCal.en[tipo][interno]) continue;
      const m = /DisplayName\s*=>\s*CalamityUtils\.GetText\("(Items|NPCs)\.(?:[\w.]+\.)?(\w+)\.DisplayName"\)/.exec(fs.readFileSync(path.join(CAL, rel), 'utf8'));
      if (!m) continue;
      for (const idioma of ['en', 'es']) {
        const v = _idxCal[idioma][m[1]][m[2]];
        if (v) _idxCal[idioma][tipo][interno] = v;
      }
    }
  }
  return _idxCal;
}

// Clases reales del decompilado 2.2.4: interno -> ruta relativa, separadas por Items/NPCs/Tiles.
let _clases;
function clasesCalamity() {
  if (_clases) return _clases;
  const r = { Items: {}, NPCs: {}, Tiles: {} };
  for (const dir of fs.readdirSync(CAL)) {
    const m = /^CalamityMod\.(Items|NPCs|Tiles)(\.|$)/.exec(dir);
    if (!m) continue;
    for (const f of fs.readdirSync(path.join(CAL, dir))) {
      if (!f.endsWith('.cs')) continue;
      const interno = f.slice(0, -3);
      if (!(interno in r[m[1]])) r[m[1]][interno] = dir + '/' + f;
    }
  }
  return (_clases = r);
}

module.exports = {
  DECOMP, TML, CAL, TMOD_CALAMITY, TRADUCCION_CALAMITY,
  ids, locTerraria, nombreObjetoVanilla, nombreNpcVanilla, nombresCalamity, clasesCalamity, parseHjson, jsonTolerante,
};
