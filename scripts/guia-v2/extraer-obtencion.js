#!/usr/bin/env node
/*
 * extraer-obtencion.js  -  Guía v2 de Terrakeep, Fase F0
 * ============================================================================
 * QUÉ HACE
 *   Extrae, de código decompilado REAL, cómo se consigue cada objeto:
 *     - recetas   (tipo "receta")  : Calamity (AddRecipes / Recipe.Create) y vanilla (Recipe.cs)
 *     - botín     (tipo "botin")   : ModifyNPCLoot de los ModNPC de Calamity + ItemDropDatabase vanilla
 *     - bolsas    (tipo "bolsa")   : ModifyItemLoot (bolsas de tesoro, cajas...) de Calamity y vanilla
 *     - tiendas   (tipo "tienda")  : AddShops/ModifyShop de Calamity + NPCShopDatabase vanilla
 *   y lo vuelca en un JSON indexado por objeto (ver formato en la salida).
 *
 * DE DÓNDE LEE (solo lectura)
 *   Calamity : C:\Users\adrian\Downloads\Keep\tModLoader-Decompiled\CalamityMod-2.2.4\
 *   Vanilla  : C:\Users\adrian\Downloads\Keep\tModLoader-Decompiled\tModLoader\
 *              (Terraria\ID\ItemID.cs, NPCID.cs, TileID.cs, Recipe.cs, ModLoader\NPCShopDatabase.cs,
 *               GameContent\ItemDropRules\ItemDropDatabase.cs)
 *
 * CÓMO EJECUTARLO
 *   node extraer-obtencion.js [--salida <ruta.json>] [--calamity <dir>] [--vanilla <dir>] [--verbose]
 *   Sin dependencias npm (CommonJS puro). Por defecto escribe en el scratchpad de la sesión F0.
 *
 * CÓMO FUNCIONA (resumen)
 *   No hay un parser C# completo: se enmascaran comentarios y literales, se parten los métodos en
 *   "sentencias" (respetando bloques if/for) y se interpretan las cadenas de llamadas
 *   (CreateRecipe().AddIngredient(...).AddTile(...), npcLoot.Add(...), new NPCShop(..).Add(..)).
 *   Los números vanilla (constantes inlined) se traducen con ItemID/NPCID/TileID.
 *   Lo que no se entiende NUNCA se inventa ni se descarta en silencio: va a "noReconocidos".
 *
 * LIMITACIONES CONOCIDAS
 *   - Las condiciones (AddCondition, ByCondition, DefineConditionalDropSet, lambdas) se guardan como
 *     TEXTO CRUDO resumido, no se evalúan.
 *   - Recetas vanilla generadas con variables/bucles en Recipe.cs (muebles de eventos, plataformas
 *     inversas...) no se resuelven: van a noReconocidos.
 *   - Las modificaciones que Calamity hace a recetas vanilla ya existentes (RecipeSystem.EditRecipes:
 *     quitar/cambiar ingredientes) NO se reflejan sobre la receta vanilla; solo las recetas NUEVAS.
 *   - Botín en GlobalNPC (CalamityGlobalNPC) y reglas globales: se extrae lo literal; el NPC destino se
 *     deduce solo de patrones simples (npc.type == X); el resto, a noReconocidos.
 *   - Efectos de la suerte, "per player", modo Master/Rev se reflejan solo como texto en "condicion".
 *   - El orden de las tiendas / posiciones (InsertAfter) no se conserva.
 */
'use strict';
const fs = require('fs');
const path = require('path');

// ---------------------------------------------------------------------------
// Argumentos
// ---------------------------------------------------------------------------
const ARGS = process.argv.slice(2);
function arg(nombre, def) { const i = ARGS.indexOf('--' + nombre); return i >= 0 && ARGS[i + 1] ? ARGS[i + 1] : def; }
const VERBOSE = ARGS.includes('--verbose');
const RAIZ_CAL = arg('calamity', 'C:\\Users\\adrian\\Downloads\\Keep\\tModLoader-Decompiled\\CalamityMod-2.2.4');
const RAIZ_VAN = arg('vanilla', 'C:\\Users\\adrian\\Downloads\\Keep\\tModLoader-Decompiled\\tModLoader');
const SALIDA = arg('salida', 'C:\\Users\\adrian\\AppData\\Local\\Temp\\claude\\C--Users-adrian-Downloads-Keep-Terrasavr-Win-Terrasavr-Calamity-Beta-resources-app\\d38ffe35-118f-4719-b326-0ca425888fe7\\scratchpad\\f0\\obtencion.json');

// ---------------------------------------------------------------------------
// Resultado global
// ---------------------------------------------------------------------------
const objetos = {};          // clave -> [entradas]
const vistos = new Set();    // dedupe por JSON
const noReconocidos = [];
const cont = { recetas: 0, botin: 0, tiendas: 0 };

function addEntrada(clave, entrada) {
  const k = clave + '|' + JSON.stringify(entrada);
  if (vistos.has(k)) return false;
  vistos.add(k);
  (objetos[clave] = objetos[clave] || []).push(entrada);
  if (entrada.tipo === 'receta') cont.recetas++;
  else if (entrada.tipo === 'tienda') cont.tiendas++;
  else cont.botin++;
  return true;
}
function noRec(archivo, linea, texto) {
  texto = String(texto).replace(/\s+/g, ' ').trim();
  if (texto.length > 220) texto = texto.slice(0, 217) + '...';
  const k = archivo + ':' + linea + ':' + texto;
  if (noRec._v.has(k)) return;
  noRec._v.add(k);
  noReconocidos.push({ archivo, linea, texto });
}
noRec._v = new Set();

// ---------------------------------------------------------------------------
// Utilidades de texto C#
// ---------------------------------------------------------------------------
function leer(p) { return fs.readFileSync(p, 'utf8').replace(/\r\n/g, '\n'); }

/** Enmascara comentarios y literales. cl: sin comentarios; sk: además contenido de strings -> '_'. */
function enmascarar(src) {
  const n = src.length; const cl = new Array(n); const sk = new Array(n);
  let i = 0;
  while (i < n) {
    const c = src[i], d = src[i + 1];
    if (c === '/' && d === '/') {
      while (i < n && src[i] !== '\n') { cl[i] = ' '; sk[i] = ' '; i++; }
    } else if (c === '/' && d === '*') {
      while (i < n && !(src[i] === '*' && src[i + 1] === '/')) { const ch = src[i] === '\n' ? '\n' : ' '; cl[i] = ch; sk[i] = ch; i++; }
      if (i < n) { cl[i] = ' '; sk[i] = ' '; cl[i + 1] = ' '; sk[i + 1] = ' '; i += 2; }
    } else if (c === '"' || (c === '@' && d === '"') || (c === '$' && d === '"') || (c === '$' && d === '@' && src[i + 2] === '"') || (c === '@' && d === '$' && src[i + 2] === '"')) {
      let verbatim = false;
      while (src[i] !== '"') { cl[i] = src[i]; sk[i] = src[i]; if (src[i] === '@') verbatim = true; i++; }
      cl[i] = '"'; sk[i] = '"'; i++;
      while (i < n) {
        if (!verbatim && src[i] === '\\') { cl[i] = src[i]; sk[i] = '_'; i++; if (i < n) { cl[i] = src[i]; sk[i] = '_'; i++; } continue; }
        if (src[i] === '"') {
          if (verbatim && src[i + 1] === '"') { cl[i] = '"'; sk[i] = '_'; cl[i + 1] = '"'; sk[i + 1] = '_'; i += 2; continue; }
          break;
        }
        cl[i] = src[i]; sk[i] = src[i] === '\n' ? '\n' : '_'; i++;
      }
      if (i < n) { cl[i] = '"'; sk[i] = '"'; i++; }
    } else if (c === '\'') {
      cl[i] = c; sk[i] = c; i++;
      while (i < n && src[i] !== '\'') {
        if (src[i] === '\\') { cl[i] = src[i]; sk[i] = '_'; i++; }
        if (i < n) { cl[i] = src[i]; sk[i] = '_'; i++; }
      }
      if (i < n) { cl[i] = '\''; sk[i] = '\''; i++; }
    } else { cl[i] = c; sk[i] = c; i++; }
  }
  return { cl: cl.join(''), sk: sk.join('') };
}

function indiceLineas(src) { const a = [0]; for (let i = 0; i < src.length; i++) if (src[i] === '\n') a.push(i + 1); return a; }
function lineaDe(idx, off) { let lo = 0, hi = idx.length - 1; while (lo < hi) { const m = (lo + hi + 1) >> 1; if (idx[m] <= off) lo = m; else hi = m - 1; } return lo + 1; }

const ABRE = '([{', CIERRA = ')]}';
function cierre(sk, i) {
  let d = 0;
  for (let j = i; j < sk.length; j++) {
    const c = sk[j];
    if (ABRE.includes(c)) d++;
    else if (CIERRA.includes(c)) { d--; if (d === 0) return j; }
  }
  return -1;
}
/** Parte sk[a,b) por sep a profundidad 0 -> [{s,e}] */
function partir(sk, a, b, sep) {
  const out = []; let d = 0, ini = a;
  for (let j = a; j < b; j++) {
    const c = sk[j];
    if (ABRE.includes(c)) d++; else if (CIERRA.includes(c)) d--;
    else if (c === sep && d === 0) { out.push({ s: ini, e: j }); ini = j + 1; }
  }
  if (ini < b || out.length) out.push({ s: ini, e: b });
  return out.filter((r, k, arr) => !(k === arr.length - 1 && sk.slice(r.s, r.e).trim() === ''));
}

/** Contexto de un archivo ya enmascarado */
function crearArchivo(rel, src) {
  const m = enmascarar(src);
  return { rel, src, cl: m.cl, sk: m.sk, idx: indiceLineas(src), clases: null };
}
function clasesDe(f) {
  if (!f.clases) {
    f.clases = []; const re = /\b(?:class|struct)\s+([A-Za-z_]\w*)/g; let m;
    while ((m = re.exec(f.sk))) f.clases.push({ off: m.index, nombre: m[1] });
  }
  return f.clases;
}
function claseEn(f, off) { let r = null; for (const c of clasesDe(f)) { if (c.off <= off) r = c.nombre; else break; } return r; }
function recortar(s) { return s.replace(/\s+/g, ' ').trim(); }

// Argumento: {sk, cl, s, e} (offsets de archivo)
function args(f, open, close) {
  const rs = partir(f.sk, open + 1, close, ',');
  const pos = [], nom = {};
  for (const r of rs) {
    const sk = f.sk.slice(r.s, r.e), cl = f.cl.slice(r.s, r.e);
    const m = /^\s*([A-Za-z_]\w*)\s*:(?!:)\s*/.exec(sk);
    if (m && !/^\s*(case|default)\b/.test(sk)) {
      nom[m[1]] = { sk: sk.slice(m[0].length), cl: cl.slice(m[0].length), s: r.s + m[0].length, e: r.e };
    } else pos.push({ sk, cl, s: r.s, e: r.e });
  }
  pos.named = nom;
  return pos;
}
function txt(a) { return a ? recortar(a.cl) : ''; }

// ---------------------------------------------------------------------------
// Tablas ID <-> nombre
// ---------------------------------------------------------------------------
function cargarTabla(rutaCs, tipoRe) {
  const src = leer(rutaCs); const porId = {}; const porNombre = {};
  const re = new RegExp('public const ' + tipoRe + '\\s+([A-Za-z_]\\w*)\\s*=\\s*(-?\\d+)\\s*;', 'g'); let m;
  while ((m = re.exec(src))) {
    const id = +m[2];
    if (!(id in porId)) porId[id] = m[1];
    porNombre[m[1]] = id;
  }
  return { porId, porNombre };
}
let T_ITEM, T_NPC, T_TILE;
const GRUPOS_ID = {};
function cargarTablas() {
  const base = path.join(RAIZ_VAN, 'Terraria', 'ID');
  T_ITEM = cargarTabla(path.join(base, 'ItemID.cs'), 'short');
  T_NPC = cargarTabla(path.join(base, 'NPCID.cs'), 'short');
  T_TILE = cargarTabla(path.join(base, 'TileID.cs'), 'ushort');
  const g = leer(path.join(base, 'RecipeGroupID.cs')); const re = /public static int (\w+)\s*=\s*(\d+)/g; let m;
  while ((m = re.exec(g))) GRUPOS_ID[m[2]] = m[1];
}

// ---------------------------------------------------------------------------
// Resolución de referencias
// ---------------------------------------------------------------------------
function ultimoSeg(s) { return s.split(/[.:]+/).pop().trim(); }
const AUTO = Symbol('self');
/** Devuelve "Terraria/X" | "CalamityMod/X" | null. entorno: {vals:{ident:texto}, self:'CalamityMod/Clase'} */
function resolverItem(texto, entorno) {
  let t = recortar(texto).replace(/global::/g, '');
  for (let g = 0; g < 4; g++) { const m = /^\((.*)\)$/.exec(t); if (m && cierreBalanceado(t)) t = m[1].trim(); else break; }
  t = t.replace(/^\((?:short|int|ushort)\)\s*/, '');
  let m;
  if ((m = /^ModContent\s*\.\s*ItemType\s*<\s*([\w\.]+)\s*>\s*\(\s*\)$/.exec(t))) return 'CalamityMod/' + ultimoSeg(m[1]);
  if ((m = /^ItemID\s*\.\s*(\w+)$/.exec(t))) return m[1] in T_ITEM.porNombre ? 'Terraria/' + m[1] : null;
  if ((m = /^-?\d+$/.exec(t))) { const nm = +t === 0 ? null : T_ITEM.porId[+t]; return nm ? 'Terraria/' + nm : null; }
  if (entorno && entorno.self && /^(base\.Item\.type|Item\.type|base\.Type|Type|this\.Type|base\.Item\.type)$/.test(t)) return entorno.self;
  if (entorno && entorno.vals && /^\w+$/.test(t) && entorno.vals[t] !== undefined && entorno.vals[t] !== t) return resolverItem(entorno.vals[t], Object.assign({}, entorno, { vals: Object.assign({}, entorno.vals, { [t]: undefined }) }));
  return null;
}
function cierreBalanceado(t) { return cierre(t, 0) === t.length - 1; }
function resolverTile(texto, entorno) {
  const t = recortar(texto); let m;
  if ((m = /^ModContent\s*\.\s*TileType\s*<\s*([\w\.]+)\s*>\s*\(\s*\)$/.exec(t))) return 'CalamityMod/Tile/' + ultimoSeg(m[1]);
  if ((m = /^TileID\s*\.\s*(\w+)$/.exec(t))) return m[1] in T_TILE.porNombre ? 'Terraria/Tile/' + m[1] : null;
  if (/^-?\d+$/.test(t)) { const nm = T_TILE.porId[+t]; return nm ? 'Terraria/Tile/' + nm : null; }
  if (entorno && entorno.vals && /^\w+$/.test(t) && entorno.vals[t]) return resolverTile(entorno.vals[t], null);
  return null;
}
function resolverNpc(texto, entorno) {
  const t = recortar(texto); let m;
  if ((m = /^ModContent\s*\.\s*NPCType\s*<\s*([\w\.]+)\s*>\s*\(\s*\)$/.exec(t))) return 'CalamityMod/' + ultimoSeg(m[1]);
  if ((m = /^NPCID\s*\.\s*(\w+)$/.exec(t))) return m[1] in T_NPC.porNombre ? 'Terraria/' + m[1] : null;
  if (/^-?\d+$/.test(t)) { const nm = T_NPC.porId[+t]; return nm ? 'Terraria/' + nm : null; }
  if (entorno && entorno.vals && /^\w+$/.test(t) && entorno.vals[t]) return resolverNpc(entorno.vals[t], null);
  return null;
}
function entero(texto, entorno) {
  let t = recortar(texto).replace(/^\((?:short|int)\)\s*/, '');
  if (/^-?\d+$/.test(t)) return +t;
  if (entorno && entorno.vals && /^\w+$/.test(t) && entorno.vals[t] !== undefined && /^-?\d+$/.test(recortar(entorno.vals[t]))) return +recortar(entorno.vals[t]);
  return null;
}

// ---------------------------------------------------------------------------
// Cadenas de llamadas
// ---------------------------------------------------------------------------
const RE_IDENT = /[A-Za-z_]\w*/y;
const RE_CAST = /\(\s*\(\s*[\w\.]+\s*\)\s*([A-Za-z_]\w*)\s*\)/y;
const RE_GENERIC = /<[A-Za-z_][\w\.,\s<>\[\]\?:]*>/y;
function saltarWs(sk, i) { while (i < sk.length && /\s/.test(sk[i])) i++; return i; }

/**
 * Parsea una cadena a partir de pos: devuelve {pasos:[{prop}|{llamada,generic,args,nombreIni,open,close}|{nuevo,...}], fin}
 */
function parsearCadena(f, pos) {
  const sk = f.sk; const pasos = []; let i = saltarWs(sk, pos);
  // new Tipo(...) / new Tipo[..]{..} / new[] {..}
  const mNew = /^new\b\s*/.exec(sk.slice(i, i + 12));
  if (mNew) {
    let j = i + mNew[0].length;
    RE_IDENT.lastIndex = j; const mi = RE_IDENT.exec(sk);
    let tipo = '';
    if (mi && mi.index === j) { tipo = mi[0]; j += tipo.length; while (sk[j] === '.') { RE_IDENT.lastIndex = j + 1; const m2 = RE_IDENT.exec(sk); if (!m2 || m2.index !== j + 1) break; tipo += '.' + m2[0]; j += 1 + m2[0].length; } }
    RE_GENERIC.lastIndex = j; const mg = RE_GENERIC.exec(sk); if (mg && mg.index === j) j += mg[0].length;
    j = saltarWs(sk, j);
    const paso = { nuevo: tipo, nombreIni: i };
    if (sk[j] === '(') { const c = cierre(sk, j); if (c < 0) return { pasos, fin: pos }; paso.open = j; paso.close = c; paso.args = args(f, j, c); j = c + 1; }
    else if (sk[j] === '[') { const c = cierre(sk, j); if (c < 0) return { pasos, fin: pos }; paso.array = true; j = c + 1; }
    j = saltarWs(sk, j);
    if (sk[j] === '{') { const c = cierre(sk, j); if (c < 0) return { pasos, fin: pos }; paso.init = args(f, j, c); j = c + 1; }
    pasos.push(paso); i = j;
  } else {
    // Ident (. Ident | <G>(...) | (...))*
    for (;;) {
      i = saltarWs(sk, i);
      if (!pasos.length) { RE_CAST.lastIndex = i; const mc = RE_CAST.exec(sk); if (mc && mc.index === i) { pasos.push({ prop: mc[1], nombreIni: i }); i += mc[0].length; const q0 = saltarWs(sk, i); if (sk[q0] === '.') { i = q0 + 1; continue; } break; } }
      RE_IDENT.lastIndex = i; const m = RE_IDENT.exec(sk);
      if (!m || m.index !== i) break;
      const nombre = m[0]; let j = i + nombre.length;
      let k = saltarWs(sk, j); let generic = null;
      RE_GENERIC.lastIndex = k; const mg = RE_GENERIC.exec(sk);
      if (mg && mg.index === k) { const k2 = saltarWs(sk, k + mg[0].length); if (sk[k2] === '(') { generic = mg[0].slice(1, -1).trim(); k = k2; } }
      if (sk[k] === '(') {
        const c = cierre(sk, k); if (c < 0) break;
        pasos.push({ llamada: nombre, generic, args: args(f, k, c), nombreIni: i, open: k, close: c });
        i = c + 1;
      } else { pasos.push({ prop: nombre, nombreIni: i }); i = j; }
      // indexador: lo absorbemos
      let q = saltarWs(sk, i);
      if (sk[q] === '[') { const c = cierre(sk, q); if (c > 0) { pasos[pasos.length - 1].indexador = true; i = c + 1; q = saltarWs(sk, i); } }
      if (sk[q] === '.') { i = q + 1; continue; }
      break;
    }
    return { pasos, fin: i };
  }
  // tras new ...: cadena .a(...).b(...)
  for (;;) {
    let q = saltarWs(sk, i);
    if (sk[q] !== '.') break;
    i = q + 1; i = saltarWs(sk, i);
    RE_IDENT.lastIndex = i; const m = RE_IDENT.exec(sk);
    if (!m || m.index !== i) break;
    const nombre = m[0]; let j = i + nombre.length; let k = saltarWs(sk, j); let generic = null;
    RE_GENERIC.lastIndex = k; const mg = RE_GENERIC.exec(sk);
    if (mg && mg.index === k) { const k2 = saltarWs(sk, k + mg[0].length); if (sk[k2] === '(') { generic = mg[0].slice(1, -1).trim(); k = k2; } }
    if (sk[k] === '(') { const c = cierre(sk, k); if (c < 0) break; pasos.push({ llamada: nombre, generic, args: args(f, k, c), nombreIni: i, open: k, close: c }); i = c + 1; }
    else { pasos.push({ prop: nombre, nombreIni: i }); i = j; }
  }
  return { pasos, fin: i };
}

// ---------------------------------------------------------------------------
// Sentencias: trocear un cuerpo en chunks respetando bloques
// ---------------------------------------------------------------------------
function trocear(f, a, b) {
  const sk = f.sk; const chunks = []; const pila = [];
  let ini = a, d = 0;
  const empujar = (fin) => {
    const t = sk.slice(ini, fin);
    if (t.trim()) { let s0 = ini; while (s0 < fin && /\s/.test(sk[s0])) s0++; chunks.push({ s: s0, e: fin, bloques: pila.slice() }); }
  };
  for (let i = a; i < b; i++) {
    const c = sk[i];
    if (c === '(' || c === '[') d++;
    else if (c === ')' || c === ']') d--;
    else if (d === 0) {
      if (c === ';') { empujar(i); ini = i + 1; }
      else if (c === '{') {
        const cur = sk.slice(ini, i);
        if (/\bnew\b[^;{}]*$/.test(cur) || /=\s*$/.test(cur) || /=>\s*$/.test(cur) || /\bdelegate\s*(\([^)]*\))?\s*$/.test(cur) || /\]\s*$/.test(cur) && /\bnew\b/.test(cur)) {
          const cc = cierre(sk, i); if (cc < 0) return chunks; i = cc;
        } else {
          const hdr = recortar(f.cl.slice(ini, i));
          pila.push(hdr); ini = i + 1;
        }
      } else if (c === '}') { if (sk.slice(ini, i).trim()) empujar(i); pila.pop(); ini = i + 1; }
    }
  }
  if (sk.slice(ini, b).trim()) empujar(b);
  return chunks;
}
/** Cuerpo de métodos con nombre dado: devuelve [{a,b,off}] (a,b: dentro de las llaves) */
function metodos(f, nombre) {
  const out = []; const re = new RegExp('\\b' + nombre + '\\s*\\(', 'g'); let m;
  while ((m = re.exec(f.sk))) {
    // debe ser declaración: precedida por tipo de retorno
    const antes = f.sk.slice(Math.max(0, m.index - 80), m.index);
    if (!/\b(void|bool|int|IEnumerable<[\w\.]+>)\s+$/.test(antes) && !/\boverride\s+\w+\s+$/.test(antes)) continue;
    const open = m.index + m[0].length - 1; const c = cierre(f.sk, open); if (c < 0) continue;
    let j = saltarWs(f.sk, c + 1);
    if (f.sk[j] !== '{') continue;
    const k = cierre(f.sk, j); if (k < 0) continue;
    out.push({ a: j + 1, b: k, off: m.index, paramsOpen: open, paramsClose: c });
  }
  return out;
}
function dividirAsignacion(sk) {
  // localiza '=' de asignación a profundidad 0 (no ==, =>, <=, >=, !=)
  let d = 0;
  for (let i = 0; i < sk.length; i++) {
    const c = sk[i];
    if (ABRE.includes(c)) d++; else if (CIERRA.includes(c)) d--;
    else if (c === '=' && d === 0) {
      const p = sk[i - 1], n = sk[i + 1];
      if (n === '=' || n === '>' || '=!<>+-*/|&?%^'.includes(p || ' ')) { if (n === '=') i++; continue; }
      return i;
    }
  }
  return -1;
}
function ultimoIdent(sk) { const m = /([A-Za-z_]\w*)\s*$/.exec(sk); return m ? m[1] : null; }

// ---------------------------------------------------------------------------
// Listado de archivos
// ---------------------------------------------------------------------------
function listarCs(dir) {
  const out = [];
  (function rec(d) {
    for (const e of fs.readdirSync(d, { withFileTypes: true })) {
      const p = path.join(d, e.name);
      if (e.isDirectory()) rec(p); else if (e.name.endsWith('.cs')) out.push(p);
    }
  })(dir);
  return out;
}
function relDe(raiz, p, prefijo) { return (prefijo || '') + path.relative(raiz, p).replace(/\\/g, '/'); }

// ===========================================================================
// RECETAS (Calamity y cualquier mod con API moderna)
// ===========================================================================
const IGNORAR_RECETA = new Set(['Register', 'DisableDecraft', 'SortAfter', 'SortBefore', 'SortAfterFirstRecipesOf', 'SortBeforeFirstRecipesOf',
  'AddConsumeIngredientCallback', 'AddOnCraftCallback', 'AddCustomShimmerResult', 'AddCustomShimmerResults', 'ChangeResult', 'SetShimmerResult', 'Clone', 'AddConsumeItemCallback', 'DisableRecipe']);
function textoCondicion(a) {
  if (!a) return '';
  let t = txt(a);
  // ConstructRecipeCondition(2, out var condition), condition  -> solo el primero
  return t;
}
function procesarCadenaReceta(f, pasos, resultado, entorno, lineaIni, destinoDirty) {
  const ing = [], est = [], cond = [], gruposAceptados = [];
  let cantRes = 1, sucio = null;
  for (const p of pasos) {
    if (!p.llamada) continue;
    const n = p.llamada; const A = p.args || [];
    if (n === 'AddIngredient') {
      let ref, cant;
      if (p.generic) { ref = 'CalamityMod/' + ultimoSeg(p.generic); cant = A.length ? entero(A[0].cl, entorno) : 1; if (A.length && cant === null) cant = txt(A[0]); }
      else { ref = A[0] ? resolverItem(A[0].cl, entorno) : null; cant = A.length > 1 ? entero(A[1].cl, entorno) : 1; if (A.length > 1 && cant === null) cant = txt(A[1]); }
      if (!ref || cant === null || typeof cant === 'string') { sucio = 'AddIngredient(' + A.map(txt).join(', ') + ')'; continue; }
      ing.push({ ref, cantidad: cant });
    } else if (n === 'AddRecipeGroup') {
      const g = A[0] ? txt(A[0]) : ''; let nombre = g.replace(/^"|"$/g, '');
      const mg = /^RecipeGroupID\s*\.\s*(\w+)$/.exec(g); if (mg) nombre = mg[1];
      const cant = A.length > 1 ? entero(A[1].cl, entorno) : 1;
      if (cant === null) { sucio = 'AddRecipeGroup(' + A.map(txt).join(', ') + ')'; continue; }
      ing.push({ grupo: nombre, cantidad: cant });
    } else if (n === 'AddTile') {
      let ref = p.generic ? 'CalamityMod/Tile/' + ultimoSeg(p.generic) : (A[0] ? resolverTile(A[0].cl, entorno) : null);
      if (!ref) { sucio = 'AddTile(' + A.map(txt).join(', ') + ')'; continue; }
      est.push(ref);
    } else if (n === 'AddCondition') {
      cond.push(textoCondicion(A[0]) || '(sin texto)');
    } else if (n === 'AddDecraftCondition') {
      cond.push('decraft: ' + textoCondicion(A[0]));
    } else if (n === 'CreateRecipe' || n === 'Create') {
      // ya tratado por el llamador
    } else if (!IGNORAR_RECETA.has(n)) {
      sucio = sucio || ('método de receta no tratado: ' + n);
    }
  }
  return { ing, est, cond, sucio };
}

function extraerRecetasCalamity(archivos) {
  for (const f of archivos) {
    const re = /\b(?:Recipe\s*\.\s*Create|CreateRecipe)\s*\(/g; let m;
    while ((m = re.exec(f.sk))) {
      const iniNombre = m.index; const open = m.index + m[0].length - 1;
      // descartar declaraciones (public Recipe CreateRecipe(...) {)
      const antes = f.sk.slice(Math.max(0, iniNombre - 40), iniNombre);
      if (/\b(Recipe|void)\s+$/.test(antes) && !/=\s*$/.test(antes)) continue;
      if (/\.\s*$/.test(antes) && !/\bRecipe\s*\.\s*$/.test(antes) && /Recipe\s*\.\s*Create/.test(m[0]) === false) { /* mod.CreateRecipe etc.: aceptamos igualmente */ }
      const close = cierre(f.sk, open); if (close < 0) continue;
      const A = args(f, open, close);
      const esStatic = /Recipe\s*\.\s*Create/.test(m[0]);
      const clase = claseEn(f, iniNombre);
      const entorno = { vals: {}, self: clase ? 'CalamityMod/' + clase : null };
      let resultado, cantRes = 1;
      if (esStatic) {
        resultado = A[0] ? resolverItem(A[0].cl, entorno) : null;
        if (A[1]) { const q = entero(A[1].cl, entorno); if (q !== null) cantRes = q; }
        if (!resultado) { noRec(f.rel, lineaDe(f.idx, iniNombre), 'Recipe.Create con resultado no resuelto: ' + txt(A[0])); continue; }
      } else {
        resultado = entorno.self; if (A[0]) { const q = entero(A[0].cl, entorno); if (q !== null) cantRes = q; }
        if (!resultado) { noRec(f.rel, lineaDe(f.idx, iniNombre), 'CreateRecipe sin clase'); continue; }
      }
      // cadena posterior
      const cad = parsearCadena(f, close + 1 - 0);
      // parsearCadena espera un ident; aquí viene ".AddX(...)". Ajuste: partir desde el punto
      let pasos = [], fin = close + 1;
      for (;;) {
        let q = saltarWs(f.sk, fin);
        if (f.sk[q] !== '.') break;
        const sub = parsearCadena(f, q + 1);
        if (!sub.pasos.length || !sub.pasos[0].llamada) { break; }
        // parsearCadena sigue la cadena entera; tomamos todo
        pasos = pasos.concat(sub.pasos); fin = sub.fin; break;
      }
      // variable?
      const prev = f.sk.slice(Math.max(0, iniNombre - 80), iniNombre);
      let variable = null;
      const mv = /([A-Za-z_]\w*)\s*=\s*(?:Recipe\s*\.\s*Create|CreateRecipe)?\s*$/.exec(prev.replace(/Recipe\s*\.\s*$/, ''));
      const mv2 = /([A-Za-z_]\w*)\s*=\s*$/.exec(prev.replace(/\bRecipe\s*\.\s*$/, ''));
      if (mv2) variable = mv2[1];
      if (variable) {
        // llamadas posteriores sobre la variable hasta Register o reasignación
        const reV = new RegExp('\\b' + variable + '\\s*(=(?!=)|\\.)', 'g'); reV.lastIndex = fin; let mm;
        const finClase = f.sk.length; let limite = f.sk.length;
        // límite: fin del bloque que contiene la declaración
        let depth = 0; for (let j = iniNombre; j < f.sk.length; j++) { if (f.sk[j] === '{') depth++; else if (f.sk[j] === '}') { if (depth === 0) { limite = j; break; } depth--; } }
        let registrada = pasos.some(p => p.llamada === 'Register');
        while (!registrada && (mm = reV.exec(f.sk)) && mm.index < limite) {
          if (mm[1].startsWith('=')) break;
          const sub = parsearCadena(f, mm.index + variable.length + 1 + (mm[0].length - variable.length - 1 - 1) + 1 - 1);
          // simplificación: parsear desde el ident tras el punto
          const posPunto = f.sk.indexOf('.', mm.index + variable.length);
          const sub2 = parsearCadena(f, posPunto + 1);
          pasos = pasos.concat(sub2.pasos); reV.lastIndex = Math.max(reV.lastIndex, sub2.fin);
          if (sub2.pasos.some(p => p.llamada === 'Register')) registrada = true;
        }
      }
      const r = procesarCadenaReceta(f, pasos, resultado, entorno);
      const linea = lineaDe(f.idx, iniNombre);
      const fuente = f.rel + ':' + linea;
      if (r.sucio) { noRec(f.rel, linea, 'receta de ' + resultado + ' con elemento no resuelto -> ' + r.sucio); continue; }
      if (!r.ing.length && !pasos.some(p => p.llamada === 'Register')) { noRec(f.rel, linea, 'receta sin Register ni ingredientes reconocibles: ' + recortar(f.cl.slice(iniNombre, Math.min(fin + 10, iniNombre + 200)))); continue; }
      addEntrada(resultado, { tipo: 'receta', cantidadResultado: cantRes, ingredientes: r.ing, estaciones: r.est, condiciones: r.cond, fuente });
    }
  }
}

// ===========================================================================
// RECETAS VANILLA (Recipe.cs, estilo 1.4.4.9)
// ===========================================================================
function extraerRecetasVanilla() {
  const rel = 'Terraria/Recipe.cs';
  const f = crearArchivo(rel, leer(path.join(RAIZ_VAN, 'Terraria', 'Recipe.cs')));
  const ini = f.src.indexOf('public static void SetupRecipes()');
  const fin = f.src.indexOf('private static void AddRecipe()');
  const chunks = trocear(f, ini, fin);
  const FLAGS_GRUPO = { anyWood: 'Wood', anySand: 'Sand', anyIronBar: 'IronBar', anyFragment: 'Fragment', anyPressurePlate: 'PressurePlate' };
  const FLAGS_COND = { needGraveyardBiome: 'Condition.InGraveyard', needSnowBiome: 'Condition.InSnow', needWater: 'Condition.NearWater', needLava: 'Condition.NearLava', needHoney: 'Condition.NearHoney', needEverythingSeed: 'Condition.ZenithWorld' };
  const FLAGS_DECRAFT = { crimson: 'decraft: Condition.CrimsonWorld', corruption: 'decraft: Condition.CorruptWorld' };
  let R = null;
  const nuevo = () => ({ res: null, cantRes: 1, ing: [], tiles: [], grupos: [], aceptados: [], cond: [], sucio: null, linea: null });
  R = nuevo();
  let n = 0;
  for (const ch of chunks) {
    const t = recortar(f.cl.slice(ch.s, ch.e)); let m;
    const linea = lineaDe(f.idx, ch.s);
    if (!/currentRecipe|AddRecipe\(\)/.test(t)) continue;
    if (t === 'AddRecipe()') {
      if (R.res !== null || R.ing.length) {
        if (R.sucio || R.res === null) noRec(rel, R.linea || linea, 'receta vanilla con elementos no resueltos: ' + (R.sucio || 'sin resultado'));
        else {
          const ent = { tipo: 'receta', cantidadResultado: R.cantRes, ingredientes: R.ing.filter(Boolean), estaciones: R.tiles.filter(x => x !== null), condiciones: R.cond, fuente: rel + ':' + R.linea };
          if (R.aceptados.length) ent.gruposAceptados = R.aceptados;
          R.ing = R.ing.filter(Boolean);
          addEntrada(R.res, ent); n++;
        }
      }
      R = nuevo(); continue;
    }
    if ((m = /^currentRecipe\.createItem\.SetDefaults\((.+)\)$/.exec(t))) {
      const nm = /^\d+$/.test(m[1]) ? T_ITEM.porId[+m[1]] : null;
      if (!nm) R.sucio = 'createItem.SetDefaults(' + m[1] + ')'; else R.res = 'Terraria/' + nm;
      if (R.linea === null) R.linea = linea;
    } else if ((m = /^currentRecipe\.createItem\.stack = (\d+)$/.exec(t))) { R.cantRes = +m[1]; }
    else if ((m = /^currentRecipe\.requiredItem\[(\d+)\]\.SetDefaults\((.+)\)$/.exec(t))) {
      const nm = /^\d+$/.test(m[2]) ? T_ITEM.porId[+m[2]] : null;
      if (!nm) R.sucio = 'requiredItem.SetDefaults(' + m[2] + ')'; else R.ing[+m[1]] = { ref: 'Terraria/' + nm, cantidad: 1 };
      if (R.linea === null) R.linea = linea;
    } else if ((m = /^currentRecipe\.requiredItem\[(\d+)\]\.stack = (.+)$/.exec(t))) {
      if (/^\d+$/.test(m[2]) && R.ing[+m[1]]) R.ing[+m[1]].cantidad = +m[2]; else R.sucio = 'stack = ' + m[2];
    } else if ((m = /^currentRecipe\.requiredTile\[(\d+)\] = (.+)$/.exec(t))) {
      const nm = /^\d+$/.test(m[2]) ? T_TILE.porId[+m[2]] : null;
      if (!nm) R.sucio = 'requiredTile = ' + m[2]; else R.tiles[+m[1]] = 'Terraria/Tile/' + nm;
    } else if ((m = /^currentRecipe\.SetCraftingStation\((.*)\)$/.exec(t))) {
      const xs = m[1].split(',').map(s => s.trim());
      xs.forEach((x, i) => { const nm = /^\d+$/.test(x) ? T_TILE.porId[+x] : null; if (!nm) R.sucio = 'SetCraftingStation(' + m[1] + ')'; else R.tiles[i] = 'Terraria/Tile/' + nm; });
    } else if ((m = /^currentRecipe\.SetIngredients\((.*)\)$/.exec(t))) {
      let xs = m[1].split(',').map(s => s.trim());
      if (xs.length === 1) xs.push('1');
      if (xs.some(x => !/^\d+$/.test(x)) || xs.length % 2) R.sucio = 'SetIngredients(' + m[1] + ')';
      else for (let i = 0; i < xs.length; i += 2) { const nm = T_ITEM.porId[+xs[i]]; if (!nm) R.sucio = 'SetIngredients(' + m[1] + ')'; else R.ing[i / 2] = { ref: 'Terraria/' + nm, cantidad: +xs[i + 1] }; }
      if (R.linea === null) R.linea = linea;
    } else if ((m = /^currentRecipe\.(\w+) = true$/.exec(t))) {
      if (FLAGS_GRUPO[m[1]]) R.grupos.push(FLAGS_GRUPO[m[1]]);
      else if (FLAGS_COND[m[1]]) R.cond.push(FLAGS_COND[m[1]]);
      else if (FLAGS_DECRAFT[m[1]]) R.cond.push(FLAGS_DECRAFT[m[1]]);
      else if (m[1] === 'notDecraftable') { /* irrelevante para obtención */ }
      else R.sucio = 'flag ' + m[1];
      if (FLAGS_GRUPO[m[1]]) R.aceptados.push(FLAGS_GRUPO[m[1]]);
    } else if ((m = /^currentRecipe\.RequireGroup\((?:RecipeGroupID\.(\w+)|(\d+))\)$/.exec(t))) {
      const nm = m[1] || GRUPOS_ID[m[2]];
      if (!nm) R.sucio = 'RequireGroup(' + (m[2]) + ')'; else R.aceptados.push(nm);
    } else if (/^currentRecipe\.AddCustomShimmerResult\(/.test(t)) { /* sin interés */ }
    else { R.sucio = t; noRec(rel, linea, 'sentencia de receta vanilla no tratada: ' + t); }
  }
  if (VERBOSE) console.log('recetas vanilla literales:', n);
}

// ===========================================================================
// BOTÍN (común Calamity y vanilla)
// ===========================================================================
function textoDen(a, entorno) {
  if (!a) return null;
  const v = entero(a.cl, entorno); if (v !== null) return v;
  return txt(a);
}
function fmtProb(den, min, max, num) {
  let p;
  if (den === null || den === undefined) p = '1/1';
  else if (typeof den === 'number') p = (num && num !== 1 ? num : 1) + '/' + den;
  else p = den;
  const mn = typeof min === 'number' ? min : null, mx = typeof max === 'number' ? max : null;
  if (mn !== null || mx !== null) {
    const a = mn === null ? 1 : mn, b = mx === null ? a : mx;
    if (!(a === 1 && b === 1)) p += a === b ? ' (' + a + ')' : ' (' + a + '-' + b + ')';
  } else if (typeof min === 'string' || typeof max === 'string') p += ' [cantidad: ' + [min, max].filter(Boolean).join('-') + ']';
  return p;
}

const CONST_FRACCION = {};
function cargarFracciones() {
  const p = path.join(RAIZ_CAL, 'CalamityMod', 'DropHelper.cs'); if (!fs.existsSync(p)) return;
  const s = leer(p); const re = /public static readonly Fraction (\w+) = new Fraction\((\d+),\s*(\d+)\)/g; let m;
  while ((m = re.exec(s))) CONST_FRACCION[m[1]] = m[2] + '/' + m[3];
}
function condLegible(t) {
  t = recortar(t);
  t = t.replace(/\bnew (Conditions\.\w+)\(\)/g, '$1');
  t = t.replace(/\bDropHelper\.(\w+)\b/g, (x, n) => (CONST_FRACCION[n] ? 'DropHelper.' + n + ' (' + CONST_FRACCION[n] + ')' : x));
  return t.length > 160 ? t.slice(0, 157) + '...' : t;
}
function fracTexto(a, entorno) {
  const v = entero(a.cl, entorno); if (v !== null) return v;
  const t = txt(a); const m = /^new Fraction\((\d+),\s*(\d+)\)$/.exec(t); if (m) return m[1] + '/' + m[2];
  const m2 = /^DropHelper\s*\.\s*(\w+)$/.exec(t); if (m2 && CONST_FRACCION[m2[1]]) return CONST_FRACCION[m2[1]] + ' [' + m2[1] + ']';
  return t;
}

/**
 * Motor de reglas de botín. Cada "regla" evalúa a {drops:[{ref,prob,cond:[],nota:'bolsa'|...}], cont:{cond:[]}|null}.
 * ctx: {destinos:[{tipo:'botin'|'bolsa', de}], cond:[], f, entorno}
 */
class Motor {
  constructor(f, entorno, destinosBase) {
    this.f = f; this.entorno = entorno; this.env = { rules: {}, conts: {}, arrs: {} };
    this.base = { destinos: destinosBase, cond: [] };
    this.emitidos = 0;
  }
  lineaEn(off) { return lineaDe(this.f.idx, off); }
  fuente(off) { return this.f.rel + ':' + this.lineaEn(off); }

  emitir(drop, ctx, off) {
    for (const d of ctx.destinos) {
      const e = { tipo: d.tipo, de: d.de };
      if (drop.prob) e.probabilidad = drop.prob;
      const cond = ctx.cond.concat(drop.cond || []).filter(Boolean);
      if (cond.length) e.condicion = cond.join(' | ');
      e.fuente = this.fuente(drop.off !== undefined ? drop.off : off);
      if (addEntrada(drop.ref, e)) this.emitidos++;
    }
  }
  emitirRegla(r, ctx, off) {
    if (!r) return;
    for (const d of r.drops) this.emitir(d, ctx, off);
  }

  /** lista de referencias de item en una lista de argumentos (acepta arrays, params, WeightedItemStack) */
  listaItems(A) {
    const out = []; let ok = true;
    for (const a of A) {
      const t = txt(a);
      const r = resolverItem(a.cl, this.entorno);
      if (r) { out.push(r); continue; }
      if (/^\w+$/.test(t) && this.env.arrs[t]) { const arr = this.env.arrs[t]; out.push(...arr.filter(Boolean)); if (arr.some(x => !x)) ok = false; continue; }
      let m;
      if ((m = /^new\s+(?:int|short)?\s*\[[^\]]*\]\s*\{([\s\S]*)\}$/.exec(t))) {
        const sub = this.partirTexto(m[1]); for (const s of sub) { const rr = resolverItem(s, this.entorno); if (rr) out.push(rr); else ok = false; } continue;
      }
      if ((m = /^new\s+WeightedItemStack\s*\(([\s\S]*)\)$/.exec(t))) {
        const sub = this.partirTexto(m[1]); const rr = resolverItem(sub[0] || '', this.entorno); if (rr) out.push(rr); else ok = false; continue;
      }
      ok = false;
    }
    return { refs: out, ok };
  }
  partirTexto(s) { const m = enmascarar(s); return partir(m.sk, 0, s.length, ',').map(r => s.slice(r.s, r.e).trim()); }

  /** Evalúa una expresión como "regla" -> {drops, cont} | null */
  regla(a, off0) {
    const f = this.f; const t = txt(a);
    if (/^\w+$/.test(t) && this.env.rules[t]) { const r = this.env.rules[t]; return { drops: r.drops.slice(), cont: r.cont, variable: t }; }
    const { pasos } = parsearCadena(f, a.s);
    if (!pasos.length) { return null; }
    let base = null, idx = 0, drops = [], cont = null;
    const p0 = pasos[0];
    const A = (p) => p.args || [];
    const item = (x) => resolverItem(x.cl, this.entorno);
    let q = null;
    const add = (ref, prob, cond, extra) => { if (ref) drops.push(Object.assign({ ref, prob, cond: cond || [], off: p0.nombreIni }, extra || {})); };
    // cualificador + primera llamada
    let calif = [], k = 0;
    while (k < pasos.length && pasos[k].prop) { calif.push(pasos[k].prop); k++; }
    const call = pasos[k] && (pasos[k].llamada || pasos[k].nuevo !== undefined) ? pasos[k] : null;
    if (p0.nuevo !== undefined) {
      const tipo = p0.nuevo; const AA = A(p0);
      switch (tipo) {
        case 'LeadingConditionRule': cont = { cond: [condLegible(AA.map(txt).join(', '))] }; break;
        case 'CommonDrop': { const r = item(AA[0]); add(r, fmtProb(textoDen(AA[1], this.entorno), entero(AA[2] ? AA[2].cl : '1', this.entorno), entero(AA[3] ? AA[3].cl : '1', this.entorno))); break; }
        case 'OneFromRulesRule': case 'SequentialRulesRule': case 'SequentialRulesNotScalingWithLuckRule': {
          const den = textoDen(AA[0], this.entorno); const hijos = AA.slice(1);
          const nota = (tipo === 'OneFromRulesRule' ? 'una de ' + hijos.length + ' reglas, 1/' : 'secuencia de reglas, 1/') + den;
          for (const h of hijos) { const rr = this.regla(h); if (rr) for (const d of rr.drops) drops.push(Object.assign({}, d, { cond: [nota].concat(d.cond || []) })); }
          break; }
        case 'DropBasedOnExpertMode': {
          const r1 = this.regla(AA[0]), r2 = this.regla(AA[1]);
          if (r1) for (const d of r1.drops) drops.push(Object.assign({}, d, { cond: ['modo normal'].concat(d.cond || []) }));
          if (r2) for (const d of r2.drops) drops.push(Object.assign({}, d, { cond: ['modo experto'].concat(d.cond || []) }));
          break; }
        case 'DropBasedOnMasterAndExpertMode': case 'DropBasedOnMasterMode': {
          for (const h of AA) { const rr = this.regla(h); if (rr) for (const d of rr.drops) drops.push(Object.assign({}, d, { cond: ['segun modo (' + tipo + ')'].concat(d.cond || []) })); }
          break; }
        case 'ItemDropWithConditionRule': { const r = item(AA[0]); add(r, fmtProb(textoDen(AA[1], this.entorno), entero(AA[2] ? AA[2].cl : '1', this.entorno), entero(AA[3] ? AA[3].cl : '1', this.entorno)), [condLegible(txt(AA[5]) || txt(AA[4]) || '')]); break; }
        case 'OneFromOptionsDropRule': case 'OneFromOptionsNotScaledWithLuckDropRule': {
          const L = this.listaItems(AA.slice(2)); const nu = entero(AA[1] ? AA[1].cl : '1', this.entorno);
          for (const r of L.refs) add(r, (nu && nu !== 1 ? nu : 1) + '/' + textoDen(AA[0], this.entorno) + ' (una de ' + L.refs.length + ' opciones)');
          if (!L.ok) drops._noRec = 'new ' + tipo + ' con opciones no resueltas'; break; }
        case 'FewFromOptionsDropRule': case 'FewFromOptionsNotScaledWithLuckDropRule': {
          const L = this.listaItems(AA.slice(3));
          for (const r of L.refs) add(r, txt(AA[0]) + ' de ' + L.refs.length + ' opciones (' + (entero(AA[2] ? AA[2].cl : '1', this.entorno) || 1) + '/' + textoDen(AA[1], this.entorno) + ')');
          if (!L.ok) drops._noRec = 'new ' + tipo + ' con opciones no resueltas'; break; }
        case 'FromOptionsWithoutRepeatsDropRule': {
          const L = this.listaItems(AA.slice(1));
          for (const r of L.refs) add(r, txt(AA[0]) + ' de ' + L.refs.length + ' opciones sin repetir');
          if (!L.ok) drops._noRec = 'new ' + tipo + ' con opciones no resueltas'; break; }
        case 'CommonDropNotScalingWithLuck': {
          const r = item(AA[0]);
          if (AA.length >= 5) add(r, fmtProb(textoDen(AA[1], this.entorno), entero(AA[3].cl, this.entorno), entero(AA[4].cl, this.entorno), entero(AA[2].cl, this.entorno)));
          else add(r, fmtProb(textoDen(AA[1], this.entorno), entero(AA[2] ? AA[2].cl : '1', this.entorno), entero(AA[3] ? AA[3].cl : '1', this.entorno)));
          break; }
        case 'AlwaysAtleastOneSuccessDropRule': {
          for (const h of AA) { const rr = this.regla(h); if (rr) for (const d of rr.drops) drops.push(Object.assign({}, d, { cond: ['al menos una de las reglas'].concat(d.cond || []) })); }
          break; }
        case 'IItemDropRule': {
          for (const h of (p0.init || [])) { const rr = this.regla(h); if (rr) { for (const d of rr.drops) drops.push(d); } else drops._noRec = 'elemento de array de reglas no reconocido: ' + txt(h); }
          break; }
        default: { drops._noRec = 'new ' + tipo; }
      }
      idx = 1;
    } else if (calif.length && call && call.llamada) {
      const cual = calif[calif.length - 1]; const n = call.llamada; const AA = A(call);
      if (cual === 'ItemDropRule') {
        const num = (x) => x ? entero(x.cl, this.entorno) : null;
        switch (n) {
          case 'Common': case 'NotScalingWithLuck': case 'Food': case 'StatusImmunityItem': {
            add(item(AA[0]), fmtProb(AA[1] ? textoDen(AA[1], this.entorno) : null, AA[2] ? (num(AA[2]) ?? txt(AA[2])) : null, AA[3] ? (num(AA[3]) ?? txt(AA[3])) : null)); break; }
          case 'NotScalingWithLuckWithNumerator': {
            add(item(AA[0]), fmtProb(AA[1] ? textoDen(AA[1], this.entorno) : null, AA[3] ? (num(AA[3]) ?? txt(AA[3])) : null, AA[4] ? (num(AA[4]) ?? txt(AA[4])) : null, num(AA[2]))); break; }
          case 'BossBag': add(item(AA[0]), '1/1', ['bolsa de tesoro (BossBag)']); break;
          case 'BossBagByCondition': add(item(AA[1]), '1/1', ['bolsa de tesoro (BossBag) si ' + condLegible(txt(AA[0]))]); break;
          case 'MasterModeCommonDrop': add(item(AA[0]), '1/1', ['solo modo Maestro']); break;
          case 'MasterModeDropOnAllPlayers': add(item(AA[0]), AA[1] ? '1/' + (textoDen(AA[1], this.entorno)) : '1/1', ['solo modo Maestro (a todos los jugadores)']); break;
          case 'ExpertGetsRerolls': add(item(AA[0]), '1/' + textoDen(AA[1], this.entorno), ['modo experto: ' + txt(AA[2]) + ' rerolls']); break;
          case 'WithRerolls': add(item(AA[0]), fmtProb(AA[2] ? textoDen(AA[2], this.entorno) : null, AA[3] ? num(AA[3]) : null, AA[4] ? num(AA[4]) : null), ['rerolls: ' + txt(AA[1])]); break;
          case 'ByCondition': add(item(AA[1]), fmtProb(AA[2] ? textoDen(AA[2], this.entorno) : null, AA[3] ? (num(AA[3]) ?? txt(AA[3])) : null, AA[4] ? (num(AA[4]) ?? txt(AA[4])) : null, num(AA[5])), [condLegible(txt(AA[0]))]); break;
          case 'NormalvsExpert': case 'NormalvsExpertNotScalingWithLuck': add(item(AA[0]), '1/' + textoDen(AA[1], this.entorno) + ' (normal), 1/' + textoDen(AA[2], this.entorno) + ' (experto)'); break;
          case 'OneFromOptions': case 'OneFromOptionsNotScalingWithLuck': case 'OneFromOptionsWithNumerator': case 'OneFromOptionsNotScalingWithLuckWithX': {
            const nNum = (n.endsWith('WithNumerator') || n.endsWith('WithX')) ? 2 : 1;
            const L = this.listaItems(AA.slice(nNum)); const den = textoDen(AA[0], this.entorno);
            for (const r of L.refs) add(r, '1/' + den + ' (una de ' + L.refs.length + ' opciones)');
            if (!L.ok) drops._noRec = 'ItemDropRule.' + n + ' con opciones no resueltas'; break; }
          case 'NormalvsExpertOneFromOptions': case 'NormalvsExpertOneFromOptionsNotScalingWithLuck': {
            const L = this.listaItems(AA.slice(2));
            for (const r of L.refs) add(r, '1/' + textoDen(AA[0], this.entorno) + ' (normal), 1/' + textoDen(AA[1], this.entorno) + ' (experto) (una de ' + L.refs.length + ' opciones)');
            if (!L.ok) drops._noRec = 'ItemDropRule.' + n + ' con opciones no resueltas'; break; }
          case 'FewFromOptions': case 'FewFromOptionsNotScalingWithLuck': case 'FewFromOptionsWithNumerator': case 'FewFromOptionsNotScalingWithLuckWithX': {
            const nNum = n.endsWith('Numerator') || n.endsWith('WithX') ? 3 : 2;
            const L = this.listaItems(AA.slice(nNum));
            for (const r of L.refs) add(r, txt(AA[0]) + ' de ' + L.refs.length + ' opciones (1/' + textoDen(AA[1], this.entorno) + ')');
            if (!L.ok) drops._noRec = 'ItemDropRule.' + n + ' con opciones no resueltas'; break; }
          case 'SequentialRules': case 'SequentialRulesNotScalingWithLuck': case 'SequentialRulesNotScalingWithLuckWithNumerator': {
            const nNum = n.endsWith('Numerator') ? 2 : 1; const den = textoDen(AA[0], this.entorno);
            for (const h of AA.slice(nNum)) { const rr = this.regla(h); if (rr) for (const d of rr.drops) drops.push(Object.assign({}, d, { cond: ['secuencia de reglas, 1/' + den].concat(d.cond || []) })); }
            break; }
          case 'AlwaysAtleastOneSuccess': {
            for (const h of AA) { const rr = this.regla(h); if (rr) for (const d of rr.drops) drops.push(Object.assign({}, d, { cond: ['al menos una de las reglas'].concat(d.cond || []) })); }
            break; }
          case 'DropNothing': case 'Coins': case 'CoinsBasedOnNPCValue': break;
          default: drops._noRec = 'ItemDropRule.' + n;
        }
        idx = k + 1;
      } else if (cual === 'DropHelper') {
        const num = (x) => x ? entero(x.cl, this.entorno) : null;
        switch (n) {
          case 'PerPlayer': {
            const den = AA[1] ? fracTexto(AA[1], this.entorno) : null;
            add(item(AA[0]), fmtProb(den, AA[2] ? (num(AA[2]) ?? txt(AA[2])) : null, AA[3] ? (num(AA[3]) ?? txt(AA[3])) : null, AA[4] ? num(AA[4]) : null), ['por jugador (PerPlayer)']); break; }
          case 'CalamityStyle': {
            const frac = fracTexto(AA[0], this.entorno); let resto = AA.slice(1);
            if (resto.length && /^(true|false)$/.test(txt(resto[0]))) resto = resto.slice(1);
            const L = this.listaItems(resto);
            for (const r of L.refs) add(r, (typeof frac === 'number' ? '1/' + frac : frac) + ' por objeto (CalamityStyle, ' + L.refs.length + ' objetos)');
            if (!L.ok) drops._noRec = 'DropHelper.CalamityStyle con opciones no resueltas'; break; }
          case 'NormalVsExpertQuantity': {
            add(item(AA[0]), '1/' + textoDen(AA[1], this.entorno) + ' (normal ' + txt(AA[2]) + '-' + txt(AA[3]) + ', experto ' + txt(AA[4]) + '-' + txt(AA[5]) + ')'); break; }
          default: drops._noRec = 'DropHelper.' + n;
        }
        idx = k + 1;
      } else { return null; }
    } else return null;
    // cadena posterior: OnSuccess / OnFailedRoll / OnFailedConditions
    const hijos = [];
    for (let j = idx; j < pasos.length; j++) {
      const p = pasos[j];
      if (!p.llamada) continue;
      if (/^(OnSuccess|OnFailedRoll|OnFailedConditions)$/.test(p.llamada)) {
        const rr = this.regla(A(p)[0]);
        const nota = p.llamada === 'OnSuccess' ? null : (p.llamada === 'OnFailedRoll' ? 'si falla la tirada anterior' : 'si no se cumple la condición anterior');
        if (rr) { for (const d of rr.drops) drops.push(Object.assign({}, d, { cond: (nota ? [nota] : []).concat(cont ? cont.cond : []).concat(d.cond || []) })); }
        else if (A(p)[0]) drops._noRec = (drops._noRec || '') + ' hijo no resuelto: ' + txt(A(p)[0]);
      } else drops._noRec = (drops._noRec || '') + ' llamada ' + p.llamada;
    }
    const res = { drops, cont, variable: null };
    if (drops._noRec) res.noRec = drops._noRec;
    return res;
  }

  /** Aplica una regla (resultado de regla()) bajo ctx; devuelve el contenedor resultante */
  registrarRegla(a, ctx, off) {
    const t = txt(a); let r = this.regla(a);
    if (!r) { this.noReconocido(off, 'regla no reconocida: ' + t); return ctx; }
    if (r.noRec) this.noReconocido(off, 'elemento de regla no reconocido (' + r.noRec + ') en: ' + t);
    // contenedor propio de la regla
    let ctx2 = ctx;
    if (r.cont) ctx2 = { destinos: ctx.destinos, cond: ctx.cond.concat(r.cont.cond) };
    if (r.variable && this.env.rules[r.variable]) {
      const v = this.env.rules[r.variable];
      this.emitirRegla({ drops: v.drops }, ctx2, off);
      v.registrado = ctx2; v.drops = [];
    } else this.emitirRegla(r, ctx2, off);
    return ctx2;
  }
  noReconocido(off, texto) { noRec(this.f.rel, this.lineaEn(off), texto); }
}

const RAICES_LOOT = new Set(['npcLoot', 'itemLoot', 'loot', 'globalLoot']);
let INDICE_HELPERS = null; // nombre -> [{f, a, b, params:[{tipo,nombre}]}]
function construirIndiceHelpers(archivos) {
  INDICE_HELPERS = {};
  const re = /\b(?:static\s+)?(?:void|IItemDropRule|LeadingConditionRule)\s+([A-Za-z_]\w*)\s*\(([^()]*\b(?:ILoot|NPCLoot|ItemLoot|GlobalLoot)\b[^()]*)\)\s*\{/g;
  for (const f of archivos) {
    let m;
    while ((m = re.exec(f.sk))) {
      if (/^Modify(NPC|Item|Global)Loot$/.test(m[1])) continue;
      const open = m.index + m[0].length - 1; const c = cierre(f.sk, open); if (c < 0) continue;
      const params = partir(f.sk, m.index + m[0].indexOf('(') + 1, m.index + m[0].lastIndexOf(')'), ',').map(r => {
        const t = recortar(f.cl.slice(r.s, r.e)).replace(/=[\s\S]*$/, '').trim(); const mm = /^(?:this\s+)?(.*?)\s+(\w+)$/.exec(t);
        return mm ? { tipo: mm[1], nombre: mm[2] } : { tipo: t, nombre: t };
      });
      (INDICE_HELPERS[m[1]] = INDICE_HELPERS[m[1]] || []).push({ f, a: open + 1, b: c, params });
    }
  }
}
function esTipoLoot(t) { return /\b(ILoot|NPCLoot|ItemLoot|GlobalLoot)\b/.test(t); }
/** Expande un método auxiliar de botín. A: argumentos de la llamada; receptor: true si es extensión sobre la raíz */
function expandirHelper(motor, nombre, A, receptor, ctx, off) {
  const cands = INDICE_HELPERS && INDICE_HELPERS[nombre]; if (!cands || (motor.prof || 0) >= 4) return false;
  const h = cands.find(x => x.f === motor.f) || cands[0];
  let raiz = null;
  if (receptor) raiz = (h.params.find(p => esTipoLoot(p.tipo)) || {}).nombre;
  else { for (let i = 0; i < A.length; i++) { const t = txt(A[i]); if (h.params[i] && esTipoLoot(h.params[i].tipo) && (RAICES_LOOT.has(t) || (motor.raices && motor.raices.has(t)))) { raiz = h.params[i].nombre; break; } } }
  if (!raiz) return false;
  const m2 = new Motor(h.f, { vals: {}, self: motor.entorno.self }, ctx.destinos);
  m2.base.cond = ctx.cond.slice(); m2.raices = new Set([raiz]); m2.prof = (motor.prof || 0) + 1;
  correrChunks(m2, h.f, trocear(h.f, h.a, h.b), {});
  return true;
}
function destinosGlobal(ch, casos) {
  const out = [];
  for (const h of ch.bloques) {
    const re = /npc\s*\.\s*(?:type|netID)\s*==\s*(ModContent\s*\.\s*NPCType\s*<[\w\.]+>\s*\(\s*\)|NPCID\s*\.\s*\w+|-?\d+)/g; let m;
    while ((m = re.exec(h))) { const r = resolverNpc(m[1], null); if (r) out.push({ tipo: 'botin', de: r }); }
    const rc = /\bcase\s+([^:]+):/g;
    while ((m = rc.exec(h))) { const r = resolverNpc(m[1], null); if (r) out.push({ tipo: 'botin', de: r }); }
  }
  if (out.length) return out;
  return casos && casos.length ? casos.map(r => ({ tipo: 'botin', de: r })) : null;
}
/** Ejecuta una lista de chunks (sentencias) de un método de botín */
function correrChunks(motor, f, chunks, opts) {
  opts = opts || {}; const env = motor.env; let casos = null;
  for (const ch of chunks) {
    let ini = ch.s; const labels = [];
    for (;;) {
      const m = /^\s*(?:case\b[^:]*:|default\s*:)\s*/.exec(f.sk.slice(ini, ch.e)); if (!m) break;
      labels.push(f.cl.slice(ini, ini + m[0].length)); ini += m[0].length;
    }
    if (opts.global && labels.length) {
      casos = labels.map(l => { const mm = /case\s+([^:]+):/.exec(l); return mm ? resolverNpc(mm[1], null) : null; });
      if (casos.some(x => !x)) casos = null;
    }
    const t = recortar(f.cl.slice(ini, ch.e)); if (!t) continue;
    if (opts.global) {
      for (const h of ch.bloques) {
        const rc = /\bcase\s+([^:]+):/g; let mm; const lst = [];
        while ((mm = rc.exec(h))) lst.push(resolverNpc(mm[1], null));
        if (lst.length) casos = lst.every(Boolean) ? lst : null;
      }
    }
    if (/^(break|return|continue)\b/.test(t)) { casos = null; continue; }
    const condBloques = ch.bloques.filter(h => /^(if|else)\b/.test(h) && !(opts.global && /\bnpc\s*\.\s*(type|netID)\s*==/.test(h))).map(h => 'bloque: ' + condLegible(h));
    let destinos = motor.base.destinos;
    if (opts.global) destinos = destinosGlobal(ch, casos);
    const ctxBase = { destinos: destinos || [], cond: (motor.base.cond || []).concat(condBloques) };
    const eq = dividirAsignacion(f.sk.slice(ini, ch.e));
    let nombre = null, rhsIni = ini, decl = '';
    if (eq >= 0) { decl = f.sk.slice(ini, ini + eq); nombre = ultimoIdent(decl); rhsIni = ini + eq + 1; while (/\s/.test(f.sk[rhsIni])) rhsIni++; }
    const rhs = { sk: f.sk.slice(rhsIni, ch.e), cl: f.cl.slice(rhsIni, ch.e), s: rhsIni, e: ch.e };
    const rhsT = txt(rhs);
    // arrays de items
    if (nombre && /^new\b/.test(rhsT)) {
      const { pasos } = parsearCadena(f, rhsIni);
      const p0 = pasos[0];
      if (p0 && p0.nuevo !== undefined && (p0.array || p0.init) && !p0.args && p0.nuevo !== 'IItemDropRule') {
        env.arrs[nombre] = (p0.init || []).map(x => resolverItem(x.cl, motor.entorno)); continue;
      }
    }
    { const me = /^(\w+)\s*\[\s*(\d+)\s*\]\s*$/.exec(decl.trim()); if (me && eq >= 0 && env.arrs[me[1]]) { env.arrs[me[1]][+me[2]] = resolverItem(rhsT, motor.entorno); continue; } }
    if (nombre && eq >= 0 && /^(?:[\w<>\[\],\.\?]+\s+)?\w+\s*$/.test(decl.trim())) {
      if (resolverItem(rhsT, motor.entorno) || /^-?\d+$/.test(rhsT)) motor.entorno.vals[nombre] = rhsT;
    }
    const es = /\b(npcLoot|itemLoot|loot|globalLoot|ItemDropRule|DropHelper|Register\w+)\b/.test(rhsT)
      || (motor.raices && [...motor.raices].some(r => new RegExp('\\b' + r + '\\b').test(rhsT)))
      || (() => { const m = /^(\w+)\s*\./.exec(rhsT); return m && (env.rules[m[1]] || env.conts[m[1]]); })()
      || /\b(LeadingConditionRule|OneFromRulesRule|CommonDrop|DropBasedOn\w+|IItemDropRule)\b/.test(rhsT);
    if (!es) continue;
    if (opts.global && !destinos && /^(?:[\w<>]+\s+)?\w+\s*=\s*(?:new LeadingConditionRule|\w+\s*\.\s*Define\w*DropSet\()/.test(t)) { evalSentencia(motor, f, rhs, nombre, ctxBase); continue; }
    if (opts.global && !destinos) {
      if (!/^(?:[\w<>]+\s+)?\w+\s*=\s*(?:new LeadingConditionRule|\w+\s*\.\s*Define\w*DropSet\()/.test(t)) noRec(f.rel, lineaDe(f.idx, ini), 'botín en GlobalNPC sin NPC deducible: ' + t);
      continue;
    }
    evalSentencia(motor, f, rhs, nombre, ctxBase);
  }
}

/**
 * Evalúa rhs: cadena sobre contenedor (npcLoot..., var.), llamada Register* sin cualificar, o fábrica de regla (asignada a var).
 * Devuelve true si algo reconocido.
 */
function evalSentencia(motor, f, rhs, nombreVar, ctxBase) {
  const env = motor.env; const { pasos } = parsearCadena(f, rhs.s);
  if (!pasos.length) { noRec(f.rel, lineaDe(f.idx, rhs.s), 'sentencia de botín no parseable: ' + txt(rhs)); return false; }
  // 1) fábrica de regla (ItemDropRule.X / new X) -> regla pendiente o variable
  const p0 = pasos[0];
  const esFab = p0.nuevo !== undefined || (p0.prop && (p0.prop === 'ItemDropRule' || p0.prop === 'DropHelper') && pasos[1] && pasos[1].llamada && !/^(Define|Add|If)/.test(pasos[1].llamada) && pasos[1].llamada !== 'BlockDrops' && pasos[1].llamada !== 'BlockEverything');
  if (esFab && p0.nuevo !== 'List' && !/^(?:List|Dictionary|HashSet)/.test(p0.nuevo || '')) {
    const r = motor.regla(rhs);
    if (!r) { noRec(f.rel, lineaDe(f.idx, rhs.s), 'regla no reconocida: ' + txt(rhs)); return false; }
    if (r.noRec) noRec(f.rel, lineaDe(f.idx, rhs.s), 'elemento de regla no reconocido (' + r.noRec + ') en: ' + txt(rhs));
    if (nombreVar) {
      env.rules[nombreVar] = { drops: r.drops, cont: r.cont, registrado: null };
    } else if (!r.drops.length && !r.cont) { /* nada */ }
    else noRec(f.rel, lineaDe(f.idx, rhs.s), 'regla creada y no asignada: ' + txt(rhs));
    return true;
  }
  // 2) llamada sin cualificar Register*
  if (p0.llamada && /^Register/.test(p0.llamada)) return evalRegister(motor, f, pasos, rhs, nombreVar, ctxBase);
  if (p0.llamada && INDICE_HELPERS && INDICE_HELPERS[p0.llamada] && expandirHelper(motor, p0.llamada, p0.args, false, ctxBase, p0.nombreIni)) return true;
  if (p0.prop && pasos.length === 2 && pasos[1].llamada && INDICE_HELPERS && INDICE_HELPERS[pasos[1].llamada] && expandirHelper(motor, pasos[1].llamada, pasos[1].args, false, ctxBase, pasos[1].nombreIni)) return true;
  // 3) cadena sobre contenedor
  let k = 0, raiz = p0.prop;
  if (!raiz) { noRec(f.rel, lineaDe(f.idx, rhs.s), 'sentencia de botín no reconocida: ' + txt(rhs)); return false; }
  let ctx;
  if (RAICES_LOOT.has(raiz) || motor.raices && motor.raices.has(raiz)) ctx = { destinos: ctxBase.destinos, cond: ctxBase.cond.slice() };
  else if (env.conts[raiz]) ctx = { destinos: env.conts[raiz].destinos.length ? env.conts[raiz].destinos : ctxBase.destinos, cond: env.conts[raiz].cond.concat(ctxBase.cond) };
  else if (env.rules[raiz] && env.rules[raiz].registrado) ctx = { destinos: env.rules[raiz].registrado.destinos.length ? env.rules[raiz].registrado.destinos : ctxBase.destinos, cond: env.rules[raiz].registrado.cond.concat(ctxBase.cond) };
  else if (env.rules[raiz]) {
    // regla pendiente: los hijos se acumulan
    const v = env.rules[raiz];
    for (let j = 1; j < pasos.length; j++) {
      const p = pasos[j]; if (!p.llamada) continue;
      if (/^(OnSuccess|OnFailedRoll|OnFailedConditions)$/.test(p.llamada)) { const rr = motor.regla(p.args[0]); if (rr) for (const d of rr.drops) v.drops.push(Object.assign({}, d, { cond: (d.cond || []) })); else noRec(f.rel, lineaDe(f.idx, p.nombreIni), 'hijo de regla no reconocido: ' + txt(p.args[0])); }
      else if (p.llamada === 'Add' && p.args[0]) {
        const A0 = p.args; const refI = resolverItem(A0[0].cl, motor.entorno);
        const condR = [];
        if (refI) {
          const numz = (x) => x ? entero(x.cl, motor.entorno) : null;
          v.drops.push({ ref: refI, prob: fmtProb(A0[1] ? fracTexto(A0[1], motor.entorno) : null, A0[2] ? (numz(A0[2]) ?? txt(A0[2])) : null, A0[3] ? (numz(A0[3]) ?? txt(A0[3])) : null), cond: condR.slice(), off: p.nombreIni });
        } else {
          const rr = motor.regla(A0[0]);
          if (rr) { for (const d of rr.drops) v.drops.push(Object.assign({}, d, { cond: condR.concat(d.cond || []) })); }
          else noRec(f.rel, lineaDe(f.idx, p.nombreIni), 'Add sobre regla pendiente no reconocido: ' + A0.map(txt).join(', '));
        }
      }
      else noRec(f.rel, lineaDe(f.idx, p.nombreIni), 'llamada sobre regla pendiente no tratada: ' + p.llamada);
    }
    return true;
  } else { return false; }
  k = 1;
  let reconocido = false;
  for (; k < pasos.length; k++) {
    const p = pasos[k]; if (!p.llamada) continue;
    const n = p.llamada; const A = p.args; const off = p.nombreIni;
    const num = (x) => x ? entero(x.cl, motor.entorno) : null;
    const itm = (x) => x ? resolverItem(x.cl, motor.entorno) : null;
    if (n === 'DefineNormalOnlyDropSet') { ctx = { destinos: ctx.destinos, cond: ctx.cond.concat(['solo modo normal (DefineNormalOnlyDropSet)']) }; reconocido = true; }
    else if (n === 'DefineConditionalDropSet') { ctx = { destinos: ctx.destinos, cond: ctx.cond.concat([condLegible(txt(A[0]))]) }; reconocido = true; }
    else if (n === 'Add' || n === 'AddNormalOnly' || n === 'AddFail' || n === 'OnSuccess' || n === 'OnFailedRoll' || n === 'OnFailedConditions') {
      const extra = n === 'AddNormalOnly' ? ['solo modo normal'] : n === 'AddFail' ? ['si falla la condición del conjunto'] : n === 'OnFailedRoll' ? ['si falla la tirada anterior'] : n === 'OnFailedConditions' ? ['si no se cumple la condición anterior'] : [];
      const ref = itm(A[0]);
      if (ref) {
        const den = A[1] ? fracTexto(A[1], motor.entorno) : null;
        motor.emitir({ ref, prob: fmtProb(den, A[2] ? (num(A[2]) ?? txt(A[2])) : null, A[3] ? (num(A[3]) ?? txt(A[3])) : null), cond: extra, off }, ctx, off);
        reconocido = true;
      } else if (A[0]) {
        const r = motor.regla(A[0]);
        if (r) { if (r.noRec) noRec(f.rel, lineaDe(f.idx, off), 'elemento de regla no reconocido (' + r.noRec + ') en: ' + txt(A[0])); const rr = { drops: r.drops.map(d => Object.assign({}, d, { cond: extra.concat(d.cond || []) })) }; const ctx2 = r.cont ? { destinos: ctx.destinos, cond: ctx.cond.concat(r.cont.cond) } : ctx; motor.emitirRegla(rr, ctx2, off); if (r.variable && env.rules[r.variable]) { env.rules[r.variable].registrado = ctx2; env.rules[r.variable].drops = []; } if (r.cont && nombreVar) env.conts[nombreVar] = ctx2; reconocido = true; if (r.cont) { ctx = ctx2; } }
        else noRec(f.rel, lineaDe(f.idx, off), n + ' con argumento no reconocido: ' + txt(A[0]));
      }
    } else if (n === 'AddIf' || n === 'AddConditionalPerPlayer') {
      const ref = itm(A[1]);
      const cnd = condLegible(txt(A[0]));
      if (ref) {
        const den = n === 'AddIf' && A[2] ? fracTexto(A[2], motor.entorno) : null;
        motor.emitir({ ref, prob: fmtProb(den, n === 'AddIf' && A[3] ? (num(A[3]) ?? txt(A[3])) : null, n === 'AddIf' && A[4] ? (num(A[4]) ?? txt(A[4])) : null), cond: [cnd].concat(n === 'AddConditionalPerPlayer' ? ['por jugador'] : []), off }, ctx, off);
        reconocido = true;
      } else {
        const r = A[1] && motor.regla(A[1]);
        if (r) { motor.emitirRegla({ drops: r.drops.map(d => Object.assign({}, d, { cond: [cnd].concat(d.cond || []) })) }, ctx, off); reconocido = true; }
        else noRec(f.rel, lineaDe(f.idx, off), n + ' no reconocido: ' + A.map(txt).join(', '));
      }
      if (n === 'AddConditionalPerPlayer' && nombreVar) env.conts[nombreVar] = { destinos: ctx.destinos, cond: ctx.cond.concat([cnd]) };
    } else if (n === 'AddWithCondition') {
      // AddWithCondition(cond?, item,...) forma desconocida a priori: buscamos el primer argumento que sea item
      let ok = false;
      for (let i = 0; i < A.length; i++) { const ref = itm(A[i]); if (ref) { motor.emitir({ ref, prob: fmtProb(A[i + 1] ? fracTexto(A[i + 1], motor.entorno) : null, null, null), cond: A.slice(0, i).map(x => condLegible(txt(x))), off }, ctx, off); ok = true; break; } }
      if (!ok) noRec(f.rel, lineaDe(f.idx, off), 'AddWithCondition no reconocido: ' + A.map(txt).join(', ')); else reconocido = true;
    } else if (/^(ChangeDropRate|Remove|RemoveWhere|RemoveFromNPC|BlockDrops|BlockEverything)$/.test(n)) {
      noRec(f.rel, lineaDe(f.idx, off), 'modificación/eliminación de botín ya existente (no es una fuente nueva): ' + n + '(' + A.map(txt).join(', ') + ')');
    } else if (expandirHelper(motor, n, A, true, ctx, off)) {
      reconocido = true;
    } else {
      noRec(f.rel, lineaDe(f.idx, off), 'llamada de botín no tratada: ' + n + '(' + A.map(txt).join(', ') + ')');
    }
  }
  if (nombreVar && reconocido) env.conts[nombreVar] = ctx;
  return reconocido;
}

/** Register* vanilla: RegisterToNPC(type, rule) / RegisterToMultipleNPCs(rule, ids...) / RegisterToItem(type, rule) / RegisterToGlobal(rule) */
function evalRegister(motor, f, pasos, rhs, nombreVar, ctxBase) {
  const env = motor.env; const p = pasos[0]; const A = p.args; const n = p.llamada;
  const destinoDe = (tipo, a) => {
    if (tipo === 'item') { const r = resolverItem(a.cl, motor.entorno); return r ? { tipo: 'bolsa', de: r } : null; }
    const r = resolverNpc(a.cl, motor.entorno); return r ? { tipo: 'botin', de: r } : null;
  };
  let destinos = [], regla;
  if (n === 'RegisterToNPC') { const d = destinoDe('npc', A[0]); if (!d) { noRec(f.rel, lineaDe(f.idx, p.nombreIni), 'RegisterToNPC con NPC no resuelto: ' + txt(A[0])); return false; } destinos = [d]; regla = A[1]; }
  else if (n === 'RegisterToItem') { const d = destinoDe('item', A[0]); if (!d) { noRec(f.rel, lineaDe(f.idx, p.nombreIni), 'RegisterToItem con objeto no resuelto: ' + txt(A[0])); return false; } destinos = [d]; regla = A[1]; }
  else if (/^RegisterToMultipleNPCs/.test(n)) {
    regla = A[0]; const resto = A.slice(1);
    for (const a of resto) {
      const t = txt(a);
      if (/^\w+$/.test(t) && env.arrs[t] && env.arrs[t].npcs) { for (const x of env.arrs[t].npcs) destinos.push({ tipo: 'botin', de: x }); continue; }
      const d = destinoDe('npc', a); if (d) destinos.push(d); else { noRec(f.rel, lineaDe(f.idx, p.nombreIni), n + ' con NPC no resuelto: ' + t); }
    }
    if (/NotRemixSeed/.test(n)) ctxBase = { destinos: ctxBase.destinos, cond: ctxBase.cond.concat(['mundo no Remix']) };
    if (/RemixSeed$/.test(n)) ctxBase = { destinos: ctxBase.destinos, cond: ctxBase.cond.concat(['mundo Remix']) };
  } else if (n === 'RegisterToMultipleItems') {
    regla = A[0];
    for (const a of A.slice(1)) { const d = destinoDe('item', a); if (d) destinos.push(d); else noRec(f.rel, lineaDe(f.idx, p.nombreIni), n + ' con objeto no resuelto: ' + txt(a)); }
  } else if (n === 'RegisterToGlobal') { destinos = [{ tipo: 'botin', de: '(cualquier NPC)' }]; regla = A[0]; }
  else if (new RegExp('\\bvoid\\s+' + n + '\\s*\\(').test(f.sk)) { return true; /* llamada a otro método Register* del propio archivo: se procesa por separado */ }
  else { noRec(f.rel, lineaDe(f.idx, p.nombreIni), 'Register no tratado: ' + n); return false; }
  if (!destinos.length || !regla) return false;
  const ctx = { destinos, cond: ctxBase.cond.slice() };
  const ctx2 = motor.registrarRegla(regla, ctx, p.nombreIni);
  // encadenado: .OnSuccess(...)
  for (let j = 1; j < pasos.length; j++) {
    const q = pasos[j]; if (!q.llamada) continue;
    if (/^(OnSuccess|OnFailedRoll|OnFailedConditions)$/.test(q.llamada)) {
      const extra = q.llamada === 'OnFailedRoll' ? ['si falla la tirada anterior'] : q.llamada === 'OnFailedConditions' ? ['si no se cumple la condición anterior'] : [];
      motor.registrarRegla(q.args[0], { destinos: ctx2.destinos, cond: ctx2.cond.concat(extra) }, q.nombreIni);
    } else noRec(f.rel, lineaDe(f.idx, q.nombreIni), 'llamada encadenada tras Register no tratada: ' + q.llamada);
  }
  if (nombreVar) env.conts[nombreVar] = ctx2;
  return true;
}

// ---------------------------------------------------------------------------
// Botín Calamity: recorre todos los ModifyNPCLoot / ModifyItemLoot / ModifyGlobalLoot
// ---------------------------------------------------------------------------
function extraerBotinCalamity(archivos) {
  construirIndiceHelpers(archivos);
  for (const f of archivos) {
    for (const nombreMet of ['ModifyNPCLoot', 'ModifyItemLoot', 'ModifyGlobalLoot']) {
      for (const met of metodos(f, nombreMet)) {
        const clase = claseEn(f, met.off); const self = clase ? 'CalamityMod/' + clase : null;
        const par = recortar(f.cl.slice(met.paramsOpen + 1, met.paramsClose)); const mp = /(\w+)\s*$/.exec(par);
        const raiz = mp ? mp[1] : 'npcLoot';
        const esItem = nombreMet === 'ModifyItemLoot';
        const mCl = new RegExp('\\b(?:class|struct)\\s+' + clase + '\\b([^{]*)\\{').exec(f.sk);
        const global = !!(mCl && /Global(NPC|Item)/.test(mCl[1]));
        if (!self) continue;
        if (global && esItem) { noRec(f.rel, lineaDe(f.idx, met.off), 'ModifyItemLoot en GlobalItem sin expandir (' + clase + ')'); continue; }
        const entorno = { vals: {}, self };
        const motor = new Motor(f, entorno, global ? [] : [{ tipo: esItem ? 'bolsa' : 'botin', de: self }]);
        motor.raices = new Set([raiz]);
        correrChunks(motor, f, trocear(f, met.a, met.b), { global });
      }
    }
  }
}

// ---------------------------------------------------------------------------
// ---------------------------------------------------------------------------
// Botín vanilla
// ---------------------------------------------------------------------------
function extraerBotinVanilla() {
  const rel = 'Terraria/GameContent/ItemDropRules/ItemDropDatabase.cs';
  const f = crearArchivo(rel, leer(path.join(RAIZ_VAN, 'Terraria', 'GameContent', 'ItemDropRules', 'ItemDropDatabase.cs')));
  const reM = /\bprivate void (Register\w+)\s*\(\s*\)\s*\{/g; let m;
  while ((m = reM.exec(f.sk))) {
    const open = m.index + m[0].length - 1; const c = cierre(f.sk, open); if (c < 0) continue;
    const entorno = { vals: {}, self: null };
    const motor = new Motor(f, entorno, []);
    motor.entorno = entorno;
    const chunks = trocear(f, open + 1, c);
    for (const ch of chunks) {
      const ini = ch.s; const t = recortar(f.cl.slice(ini, ch.e)); if (!t) continue;
      const eq = dividirAsignacion(f.sk.slice(ini, ch.e));
      let nombre = null, rhsIni = ini, tipoDecl = '';
      if (eq >= 0) { nombre = ultimoIdent(f.sk.slice(ini, ini + eq)); tipoDecl = recortar(f.sk.slice(ini, ini + eq)); rhsIni = ini + eq + 1; while (/\s/.test(f.sk[rhsIni])) rhsIni++; }
      const rhs = { sk: f.sk.slice(rhsIni, ch.e), cl: f.cl.slice(rhsIni, ch.e), s: rhsIni, e: ch.e };
      const rhsT = txt(rhs);
      // constantes simples: short type = 50; type = 4782;
      if (nombre && /^-?\d+$/.test(rhsT)) { entorno.vals[nombre] = rhsT; continue; }
      // arrays de ids de NPC
      if (nombre && /^new\s+int\s*\[/.test(rhsT)) {
        const { pasos } = parsearCadena(f, rhsIni);
        if (pasos[0] && pasos[0].init) { const npcs = pasos[0].init.map(x => resolverNpc(x.cl, null)).filter(Boolean); const arr = []; arr.npcs = npcs; motor.env.arrs[nombre] = arr; continue; }
      }
      if (!/\b(Register\w+|ItemDropRule|LeadingConditionRule|OneFromRulesRule|DropBasedOn\w+|CommonDrop|OnSuccess|OnFailed\w+)\b/.test(rhsT) && !(nombre && motor.env.rules[nombre])) {
        if (/\b(\w+)\s*\.\s*(OnSuccess|OnFailed\w+)/.test(rhsT) === false) continue;
      }
      // la raíz de "x.OnSuccess" puede ser variable
      evalSentencia(motor, f, rhs, nombre, { destinos: [], cond: [] });
    }
  }
}

// ===========================================================================
// TIENDAS
// ===========================================================================
function condsDeArgs(motor, A, desde, entornoConds) {
  const out = [];
  for (const a of A.slice(desde)) {
    let t = txt(a); let m;
    if (/^Array\.Empty<Condition>\(\)$/.test(t)) continue;
    if ((m = /^new\s+Condition\s*\[[^\]]*\]\s*\{([\s\S]*)\}$/.exec(t))) { for (const s of motor.partirTexto(m[1])) out.push(resolverCondVar(s, entornoConds)); continue; }
    out.push(resolverCondVar(t, entornoConds));
  }
  return out.filter(Boolean);
}
function resolverCondVar(t, entornoConds) {
  t = recortar(t);
  if (/^\w+$/.test(t) && entornoConds && entornoConds[t]) return t + ' = ' + entornoConds[t];
  return condLegible(t);
}
function tiendasEnFuente(f, a, b, opciones) {
  // recorre sentencias buscando cadenas new NPCShop(...)... / shop.xxx(...)
  const chunks = trocear(f, a, b);
  const entorno = opciones.entorno; const condVars = {}; const tiendas = {};
  const motor = new Motor(f, entorno, []);
  for (const ch of chunks) {
    const ini = ch.s; const t = recortar(f.cl.slice(ini, ch.e)); if (!t) continue;
    const eq = dividirAsignacion(f.sk.slice(ini, ch.e));
    let nombre = null, rhsIni = ini, decl = '';
    if (eq >= 0) { decl = recortar(f.sk.slice(ini, ini + eq)); nombre = ultimoIdent(decl); rhsIni = ini + eq + 1; while (/\s/.test(f.sk[rhsIni])) rhsIni++; }
    const rhsT = recortar(f.cl.slice(rhsIni, ch.e));
    if (nombre && /^Condition\b/.test(decl) ) { condVars[nombre] = condLegible(rhsT); continue; }
    if (nombre && /^(?:int|short)\s/.test(decl) && /^shop\s*\.\s*NpcType$/.test(rhsT)) { tiendas.__npcTypeVar = nombre; continue; }
    if (nombre && /^-?\d+$/.test(rhsT)) { entorno.vals[nombre] = rhsT; continue; }
    if (!/\bNPCShop\b|\bshop\b|\b(\w+)\s*\.\s*(Add|InsertAfter|InsertBefore|AddWithCustomValue)\b/.test(rhsT)) continue;
    const { pasos } = parsearCadena(f, rhsIni);
    if (!pasos.length) continue;
    let vendedor = null, nombreTienda = null, k = 0;
    const p0 = pasos[0];
    if (p0.nuevo === 'NPCShop') {
      const r = resolverNpcOSelf(p0.args[0], opciones); vendedor = r;
      if (p0.args[1]) nombreTienda = txt(p0.args[1]).replace(/^"|"$/g, '');
      k = 1; if (nombre) tiendas[nombre] = { vendedor, nombreTienda };
    } else if (p0.prop && tiendas[p0.prop]) { vendedor = tiendas[p0.prop].vendedor; nombreTienda = tiendas[p0.prop].nombreTienda; k = 1; }
    else if (p0.prop === 'shop' && opciones.modificar) {
      // vendedor por cabecera if (npcType == N)
      for (const h of ch.bloques) {
        const mm = /(?:npcType|shop\s*\.\s*NpcType)\s*==\s*([^\)&|]+)/.exec(h);
        if (mm) { const r = resolverNpc(mm[1], entorno) || (/ModContent\.NPCType/.test(mm[1]) ? resolverNpc(mm[1], null) : null); if (r) vendedor = r; }
      }
      k = 1;
      if (!vendedor) { noRec(f.rel, lineaDe(f.idx, ini), 'ModifyShop: vendedor no deducible: ' + t); continue; }
    } else continue;
    if (!vendedor) { noRec(f.rel, lineaDe(f.idx, ini), 'tienda con vendedor no resuelto: ' + t); continue; }
    let reconocido = false;
    for (; k < pasos.length; k++) {
      const p = pasos[k]; if (!p.llamada) continue;
      const n = p.llamada; let A = p.args; const off = p.nombreIni;
      let ref = null, precio = null, desde = 1;
      if (n === 'Add' && !p.generic && A[0] && /^new\s+NPCShop\s*\.\s*Entry\b/.test(txt(A[0]))) { const pc = parsearCadena(f, A[0].s).pasos[0]; if (pc && pc.args) A = pc.args; }
      if (n === 'Add') {
        if (p.generic) { ref = 'CalamityMod/' + ultimoSeg(p.generic); desde = 0; }
        else { ref = resolverItem(A[0].cl, entorno); desde = 1; }
      } else if (n === 'AddWithCustomValue') {
        if (p.generic) { ref = 'CalamityMod/' + ultimoSeg(p.generic); precio = txt(A[0]); desde = 1; }
        else { ref = resolverItem(A[0].cl, entorno); precio = txt(A[1]); desde = 2; }
      } else if (n === 'InsertAfter' || n === 'InsertBefore') {
        if (p.generic) { ref = 'CalamityMod/' + ultimoSeg(p.generic); desde = 1; }
        else { ref = resolverItem(A[1] ? A[1].cl : '', entorno); desde = 2; }
      } else if (n === 'Register' || n === 'OrderLast' || n === 'AddInternal' || n === 'ReplaceItem' || n === 'Remove') { if (n !== 'Register') noRec(f.rel, lineaDe(f.idx, off), 'tienda: llamada no tratada ' + n + '(' + A.map(txt).join(', ') + ')'); continue; }
      else { noRec(f.rel, lineaDe(f.idx, off), 'tienda: llamada no tratada ' + n + '(' + A.map(txt).join(', ') + ')'); continue; }
      if (!ref) { noRec(f.rel, lineaDe(f.idx, off), 'tienda: objeto no resuelto en ' + n + '(' + A.map(txt).join(', ') + ')'); continue; }
      const cond = condsDeArgs(motor, A, desde, condVars).concat(ch.bloques.filter(h => /^(if|else)\b/.test(h) && !/npcType|NpcType/.test(h)).map(h => 'bloque: ' + condLegible(h)));
      const ent = { tipo: 'tienda', vendedor, condicion: cond.join('; '), fuente: f.rel + ':' + lineaDe(f.idx, off) };
      if (precio) ent.precio = precio;
      if (nombreTienda && nombreTienda !== 'Shop') ent.tienda = nombreTienda;
      addEntrada(ref, ent); reconocido = true;
    }
  }
}
function resolverNpcOSelf(a, opciones) {
  const t = txt(a);
  if (/^(base\.Type|Type|base\.NPC\.type|this\.Type)$/.test(t)) return opciones.self;
  return resolverNpc(a.cl, opciones.entorno);
}
function extraerTiendasCalamity(archivos) {
  for (const f of archivos) {
    for (const nm of ['AddShops', 'ModifyShop']) {
      for (const met of metodos(f, nm)) {
        const clase = claseEn(f, met.off);
        tiendasEnFuente(f, met.a, met.b, { entorno: { vals: {}, self: 'CalamityMod/' + clase }, self: 'CalamityMod/' + clase, modificar: nm === 'ModifyShop' });
      }
    }
  }
}
function extraerTiendasVanilla() {
  const rel = 'Terraria/ModLoader/NPCShopDatabase.cs';
  const f = crearArchivo(rel, leer(path.join(RAIZ_VAN, 'Terraria', 'ModLoader', 'NPCShopDatabase.cs')));
  const reM = /\bprivate static void (Register\w+)\s*\(\s*\)\s*\{/g; let m;
  while ((m = reM.exec(f.sk))) {
    if (m[1] === 'RegisterVanillaNPCShops') continue;
    const open = m.index + m[0].length - 1; const c = cierre(f.sk, open); if (c < 0) continue;
    tiendasEnFuente(f, open + 1, c, { entorno: { vals: {}, self: null }, self: null, modificar: false });
  }
}

// ===========================================================================
// MAIN
// ===========================================================================
function main() {
  const t0 = Date.now();
  cargarTablas(); cargarFracciones();
  const archCal = listarCs(RAIZ_CAL).map(p => crearArchivo(relDe(RAIZ_CAL, p), leer(p)));
  if (VERBOSE) console.log('archivos Calamity:', archCal.length);
  extraerRecetasCalamity(archCal); const rCal = cont.recetas;
  extraerRecetasVanilla(); const rVan = cont.recetas - rCal;
  extraerBotinCalamity(archCal); const bCal = cont.botin;
  extraerBotinVanilla(); const bVan = cont.botin - bCal;
  extraerTiendasCalamity(archCal); const tCal = cont.tiendas;
  extraerTiendasVanilla(); const tVan = cont.tiendas - tCal;
  const claves = Object.keys(objetos).sort();
  const ordenado = {}; for (const k of claves) ordenado[k] = objetos[k];
  const total = cont.recetas + cont.botin + cont.tiendas;
  const salida = {
    generado: new Date().toISOString(),
    fuentes: { calamity: 'CalamityMod 2.2.4 decompilado (' + RAIZ_CAL + ')', vanilla: 'tModLoader 1.4.4.9 decompilado (' + RAIZ_VAN + ')' },
    estadisticas: { recetas: cont.recetas, botin: cont.botin, tiendas: cont.tiendas, objetosConObtencion: claves.length, patronesNoReconocidos: noReconocidos.length,
      porcentajeNoReconocido: +(100 * noReconocidos.length / (total + noReconocidos.length)).toFixed(2),
      desglose: { recetasCalamity: rCal, recetasVanilla: rVan, botinCalamity: bCal, botinVanilla: bVan, tiendasCalamity: tCal, tiendasVanilla: tVan } },
    objetos: ordenado,
    noReconocidos
  };
  fs.mkdirSync(path.dirname(SALIDA), { recursive: true });
  fs.writeFileSync(SALIDA, JSON.stringify(salida, null, 1));
  console.log(JSON.stringify(salida.estadisticas, null, 1));
  console.log('Salida:', SALIDA, '(' + (Date.now() - t0) + ' ms)');
}
main();
