// Guia v2 (F0, 02-oct-2026): genera Terrakeep.Core/Guia/V2/Datos/guia_v2_referencias.json, la
// tabla de TODO lo que citan las guias v2 (objetos, NPC, estaciones, grupos de receta) con:
//   - id real (ItemID/NPCID de tModLoader 1.4.4.9) o nombre interno verificado (Calamity 2.2.4);
//   - nombre en español oficial (Terraria es-ES; CalamityKeep-Traduccion-ES para Calamity) y en ingles;
//     sin traduccion -> se queda el ingles, con fuenteEs = "sin traduccion" (nunca inventado);
//   - como conseguir cada objeto (recetas, botin, bolsas, tiendas) del codigo decompilado real
//     (extraer-obtencion.js), con archivo:linea, para los objetos citados y sus ingredientes
//     directos (profundidad 2: la UI puede seguir la cadena un paso mas).
//
// Uso:  node generar-referencias.js [--obtencion <obtencion.json>]
//       (sin --obtencion ejecuta extraer-obtencion.js a un temporal)
'use strict';
const fs = require('fs');
const os = require('os');
const path = require('path');
const { execFileSync } = require('child_process');
const F = require('./fuentes.js');

const args = process.argv.slice(2);
const arg = (n, d) => { const i = args.indexOf(n); return i >= 0 ? args[i + 1] : d; };
const DATOS = path.join(__dirname, '..', '..', 'Terrakeep.Core', 'Guia', 'V2', 'Datos');
const SALIDA = path.join(DATOS, 'guia_v2_referencias.json');
let OBT = arg('--obtencion', null);
if (!OBT) {
  OBT = path.join(os.tmpdir(), 'guia-v2-obtencion.json');
  execFileSync(process.execPath, [path.join(__dirname, 'extraer-obtencion.js'), '--salida', OBT], { stdio: 'inherit' });
}
const obtencion = JSON.parse(fs.readFileSync(OBT, 'utf8'));

// ---------------------------------------------------------------- 1. recoger referencias
const objetos = new Set(), npcs = new Set();
const OBJ_COND = new Set(['objeto', 'objeto_cualquiera', 'objeto_poseido', 'equipado']);
function recorrerCond(c) {
  if (!c) return;
  if (c.condiciones) c.condiciones.forEach(recorrerCond);
  const destino = OBJ_COND.has(c.tipo) ? objetos : (c.tipo === 'npc' || c.tipo === 'npc_activo') ? npcs : null;
  if (!destino) return;
  if (c.ref) destino.add(c.ref);
  (c.refs || []).forEach(r => destino.add(r));
}
function recorrer(x, clave) {
  if (typeof x === 'string') {
    for (const m of x.matchAll(/\{([on]):([^}|]+)/g)) (m[1] === 'o' ? objetos : npcs).add(m[2]);
    return;
  }
  if (Array.isArray(x)) { x.forEach(v => recorrer(v, clave)); return; }
  if (!x || typeof x !== 'object') return;
  if ('tipo' in x && ('bandera' in x || 'ref' in x || 'refs' in x || 'condiciones' in x || 'clave' in x || 'valor' in x) && clave !== 'ubicaciones') recorrerCond(x);
  for (const [k, v] of Object.entries(x)) {
    if (k === 'jefes') v.forEach(r => npcs.add(r));
    else if (k === 'conservaObjetos') v.forEach(r => objetos.add(r));
    else if (k === 'ubicaciones') v.forEach(u => { if (u.tipo === 'npc' || u.tipo === 'jefe') npcs.add(u.id); });
    else if (k === 'invocacion' && v && v.objeto) { objetos.add(v.objeto); recorrer(v, k); }
    else if ((k === 'necesitas' || k === 'armas' || k === 'armadura' || k === 'accesorios' || k === 'otros') && Array.isArray(v)) { v.forEach(o => objetos.add(o.ref)); recorrer(v, k); }
    else recorrer(v, k);
  }
}
const guias = fs.readdirSync(DATOS).filter(f => /^guia_v2_.+\.json$/.test(f) && f !== 'guia_v2_referencias.json');
for (const g of guias) recorrer(JSON.parse(fs.readFileSync(path.join(DATOS, g), 'utf8')));
const citadosDirectamente = new Set(objetos);

// ---------------------------------------------------------------- 2. nombres e ids
const ids = F.ids();
const cal = F.nombresCalamity();
const clases = F.clasesCalamity();
const problemas = [];

function refObjeto(ref) {
  const [mod, interno] = ref.split('/');
  if (mod === 'Terraria') {
    const id = ids.item.porNombre[interno];
    if (id == null) { problemas.push('objeto vanilla inexistente en ItemID 1.4.4.9: ' + ref); return null; }
    const es = F.nombreObjetoVanilla(interno, 'es_ES'), en = F.nombreObjetoVanilla(interno, 'en_US');
    return { id, es: es || en || interno, en: en || interno, fuenteEs: es ? 'Terraria 1.4.4.9 es-ES oficial (tModLoader)' : 'sin traduccion' };
  }
  if (mod === 'CalamityMod') {
    if (!clases.Items[interno]) { problemas.push('objeto de Calamity sin clase en el decompilado 2.2.4: ' + ref); return null; }
    const en = cal.en.Items[interno], es = cal.es.Items[interno];
    return { es: es || en || interno, en: en || interno, fuenteEs: es ? `CalamityKeep-Traduccion-ES ${cal.versionEsp} (es-ES)` : 'sin traduccion' };
  }
  problemas.push('mod desconocido: ' + ref);
  return null;
}
function refNpc(ref) {
  const [mod, interno] = ref.split('/');
  if (mod === 'Terraria') {
    const id = ids.npc.porNombre[interno];
    if (id == null) { problemas.push('NPC vanilla inexistente en NPCID 1.4.4.9: ' + ref); return null; }
    const es = F.nombreNpcVanilla(interno, 'es_ES'), en = F.nombreNpcVanilla(interno, 'en_US');
    return { id, es: es || en || interno, en: en || interno, fuenteEs: es ? 'Terraria 1.4.4.9 es-ES oficial (tModLoader)' : 'sin traduccion' };
  }
  if (mod === 'CalamityMod') {
    if (!clases.NPCs[interno]) { problemas.push('NPC de Calamity sin clase en el decompilado 2.2.4: ' + ref); return null; }
    const en = cal.en.NPCs[interno], es = cal.es.NPCs[interno];
    return { es: es || en || interno, en: en || interno, fuenteEs: es ? `CalamityKeep-Traduccion-ES ${cal.versionEsp} (es-ES)` : 'sin traduccion' };
  }
  problemas.push('mod desconocido: ' + ref);
  return null;
}

// ---------------------------------------------------------------- 3. estaciones y grupos
const MAP_OBJ = { es: F.locTerraria('es_ES', 'Game').MapObject, en: F.locTerraria('en_US', 'Game').MapObject };
// TileID -> objeto que lo coloca (vanilla) o MapObject oficial. Los que admiten varios objetos
// (yunque de mithril u oricalco) se nombran con los dos, separados por " / ".
const TILE_VANILLA = {
  WorkBenches: ['WorkBench'], Anvils: ['map:Anvil'], MythrilAnvil: ['MythrilAnvil', 'OrichalcumAnvil'], Sawmill: ['Sawmill'],
  LunarCraftingStation: ['LunarCraftingStation'], DyeVat: ['DyeVat'], TinkerersWorkbench: ['TinkerersWorkshop'], HeavyWorkBench: ['HeavyWorkBench'],
  Furnaces: ['Furnace'], Bookcases: ['Bookcase'], Loom: ['Loom'], CookingPots: ['CookingPot'], Bottles: ['Bottle'], Solidifier: ['Solidifier'],
  LivingLoom: ['LivingLoom'], DemonAltar: ['map:DemonAltar', 'map:CrimsonAltar'], GlassKiln: ['GlassKiln'], CrystalBall: ['CrystalBall'], Kegs: ['Keg'],
  SkyMill: ['SkyMill'], BoneWelder: ['BoneWelder'], AlchemyTable: ['AlchemyTable'], HoneyDispenser: ['HoneyDispenser'], FleshCloningVat: ['FleshCloningVaat'],
  IceMachine: ['IceMachine'], LihzahrdFurnace: ['LihzahrdFurnace'], SteampunkBoiler: ['SteampunkBoiler'], AdamantiteForge: ['AdamantiteForge', 'TitaniumForge'],
  Hellforge: ['Hellforge'], ImbuingStation: ['ImbuingStation'], Chairs: ['map:Chair'], Tables: ['map:Table'], Autohammer: ['Autohammer'],
  MeatGrinder: ['MeatGrinder'], Blendomatic: ['BlendOMatic'], TeaKettle: ['TeaKettle'], LesionStation: ['LesionStation'], Tombstones: ['Tombstone'],
  Campfire: ['Campfire'], Hive: ['Hive'], PlanteraBulb: ['map:PlanterasBulb'], Extractinator: ['Extractinator'],
};
function nombreEstacion(clave) {
  let m = /^Terraria\/Tile\/(\w+)$/.exec(clave);
  if (m) {
    const lista = TILE_VANILLA[m[1]];
    if (!lista) { problemas.push('estacion vanilla sin nombre: ' + clave); return { es: m[1], en: m[1], fuenteEs: 'sin traduccion' }; }
    const partes = lista.map(x => x.startsWith('map:')
      ? { es: MAP_OBJ.es[x.slice(4)], en: MAP_OBJ.en[x.slice(4)] }
      : { es: F.nombreObjetoVanilla(x, 'es_ES'), en: F.nombreObjetoVanilla(x, 'en_US') });
    if (partes.some(p => !p.es)) problemas.push('estacion vanilla con nombre incompleto: ' + clave + ' ' + JSON.stringify(lista));
    return { es: partes.map(p => p.es || p.en).join(' / '), en: partes.map(p => p.en).join(' / '), fuenteEs: 'Terraria 1.4.4.9 es-ES oficial (objeto/MapObject que coloca el tile)' };
  }
  m = /^CalamityMod\/Tile\/(\w+)$/.exec(clave);
  if (m) {
    for (const cand of [m[1], m[1] + 'Item', m[1].replace(/Tile$/, ''), m[1].replace(/Tile$/, 'Item')]) {
      if (clases.Items[cand] && (cal.es.Items[cand] || cal.en.Items[cand]))
        return { es: cal.es.Items[cand] || cal.en.Items[cand], en: cal.en.Items[cand] || cand, fuenteEs: cal.es.Items[cand] ? `CalamityKeep-Traduccion-ES ${cal.versionEsp} (objeto ${cand})` : 'sin traduccion' };
    }
    const especiales = { SCalAltar: 'AltarOfTheAccursedItem', AshenAltar: null };
    if (especiales[m[1]]) { const c = especiales[m[1]]; return { es: cal.es.Items[c] || cal.en.Items[c], en: cal.en.Items[c], fuenteEs: `CalamityKeep-Traduccion-ES ${cal.versionEsp} (objeto ${c})` }; }
    problemas.push('estacion de Calamity sin objeto: ' + clave);
    return { es: m[1], en: m[1], fuenteEs: 'sin traduccion' };
  }
  problemas.push('estacion desconocida: ' + clave);
  return { es: clave, en: clave, fuenteEs: 'sin traduccion' };
}

// Grupos de receta: se evalua la expresion REAL de RecipeGroup (Recipe.cs vanilla y
// RecipeSystem.cs de Calamity) con la localizacion de cada idioma.
function evaluador(expr) {
  return idioma => {
    const loc = idioma === 'es' ? 'es_ES' : 'en_US';
    const leg = F.locTerraria(loc, 'Legacy');
    const game = F.locTerraria(loc, 'Game');
    const planoCal = idioma === 'es' ? cal.planoEs : cal.planoEn;
    const partes = [];
    for (const t of expr.matchAll(/Lang\.misc\[(\d+)\]\.Value|Lang\.GetItemNameValue\((\d+)\)|Lang\.GetNPCNameValue\((\d+)\)|GetTextValue\("([^"]+)"\)/g)) {
      if (t[1]) partes.push(leg.LegacyMisc[t[1]]);
      else if (t[2]) partes.push(F.nombreObjetoVanilla(ids.item.porId[t[2]], loc));
      else if (t[3]) partes.push(F.nombreNpcVanilla(ids.npc.porId[t[3]], loc));
      else {
        const k = t[4];
        const [sec, ...resto] = k.split('.');
        const sub = resto.join('.');
        let v = planoCal['Mods.CalamityMod.' + k] || planoCal[k] || leg[sec]?.[sub] || game[sec]?.[sub];
        if (v) v = v.replace(/\{\$LegacyMisc\.(\d+)\}/g, (a, n) => leg.LegacyMisc[n]);
        partes.push(v);
      }
    }
    return partes.length && partes.every(Boolean) ? partes.join(' ') : null;
  };
}
function gruposReales() {
  const r = {};
  const fuentes = [
    path.join(F.TML, 'Terraria', 'Recipe.cs'),
    path.join(F.CAL, 'CalamityMod.Systems', 'RecipeSystem.cs'),
  ];
  for (const f of fuentes) {
    // Linea a linea: "Func<string> getName = () => EXPR;", "rec = new RecipeGroup(() => EXPR, ids)"
    // o "new RecipeGroup(getName, obj)", y despues "RegisterGroup("Nombre", rec)".
    const funcs = {};
    let actual = null;
    for (const linea of fs.readFileSync(f, 'utf8').split(/\r?\n/)) {
      let m = /Func<string> (\w+) = \(\) => (.*);\s*$/.exec(linea);
      if (m) { funcs[m[1]] = m[2]; continue; }
      m = /new RecipeGroup\((?:\(\) => (.*?), (?:\d|ModContent|\w+\))|(\w+), \w+\))/.exec(linea);
      if (m) actual = m[1] || funcs[m[2]] || null;
      m = /RegisterGroup\("([^"]+)"/.exec(linea);
      if (m && actual && !r[m[1]]) r[m[1]] = evaluador(actual);
    }
  }
  return r;
}
const GRUPOS = gruposReales();
function nombreGrupo(g) {
  const k = g.replace(/^RecipeSystem\./, '');
  const f = GRUPOS[k];
  const es = f && f('es'), en = f && f('en');
  if (!es) { problemas.push('grupo de receta sin nombre: ' + g); return { es: en || g, en: en || g, fuenteEs: 'sin traduccion' }; }
  return { es, en, fuenteEs: k in GRUPOS ? 'nombre real de RecipeGroup (Lang/Language) en es-ES' : 'sin traduccion' };
}

// ---------------------------------------------------------------- 4. obtencion (profundidad 2)
const tabla = { esquema: 1, fuentes: {}, objetos: {}, npcs: {}, estaciones: {}, grupos: {} };
const estaciones = new Set(), grupos = new Set();
function convertir(o) {
  const salida = {
    tipo: o.tipo,
    ...(o.cantidadResultado && o.cantidadResultado !== 1 ? { cantidadResultado: o.cantidadResultado } : {}),
    ...(o.ingredientes ? { ingredientes: o.ingredientes.map(i => (i.grupo ? { grupo: i.grupo, cantidad: i.cantidad } : { ref: i.ref, cantidad: i.cantidad })) } : {}),
    ...(o.estaciones && o.estaciones.length ? { estaciones: o.estaciones } : {}),
    ...(o.condiciones && o.condiciones.length ? { condiciones: o.condiciones } : {}),
    ...((o.de || o.vendedor) && /^\w+\//.test(o.de || o.vendedor) ? { de: o.de || o.vendedor } : {}),
    ...((o.de || o.vendedor) && !/^\w+\//.test(o.de || o.vendedor) ? { condicion: [o.de || o.vendedor, o.condicion].filter(Boolean).join(' | ') } : {}),
    ...(o.probabilidad ? { probabilidad: o.probabilidad } : {}),
    ...(o.condicion ? { condicion: o.condicion } : {}),
    fuente: o.fuente,
  };
  for (const i of salida.ingredientes || []) if (i.grupo) grupos.add(i.grupo); else objetos.add(i.ref);
  for (const e of salida.estaciones || []) estaciones.add(e);
  if (salida.de) (o.tipo === 'bolsa' ? objetos : npcs).add(salida.de);
  return salida;
}
const conObtencion = new Set([...objetos]);
for (const r of [...conObtencion]) for (const o of obtencion.objetos[r] || []) for (const i of o.ingredientes || []) if (i.ref) conObtencion.add(i.ref);

let fase = 0;
for (const r of [...conObtencion].sort()) {
  const datos = refObjeto(r);
  if (!datos) continue;
  tabla.objetos[r] = { ...datos, obtencion: (obtencion.objetos[r] || []).map(convertir) };
}
// Lo que solo aparece como ingrediente de profundidad 2, bolsa o similar: solo nombre.
for (const r of [...objetos].sort()) if (!tabla.objetos[r]) { const d = refObjeto(r); if (d) tabla.objetos[r] = { ...d, obtencion: [] }; }
for (const r of [...npcs].sort()) { const d = refNpc(r); if (d) tabla.npcs[r] = { ...d, jefe: /Head$|Boss|Core$/.test(r) || undefined }; }
for (const e of [...estaciones].sort()) tabla.estaciones[e] = nombreEstacion(e);
for (const g of [...grupos].sort()) tabla.grupos[g] = nombreGrupo(g);
fase++;

tabla.fuentes = {
  ids: 'ItemID.cs / NPCID.cs / TileID.cs del tModLoader 1.4.4.9 decompilado',
  nombresVanilla: 'Terraria.Localization.Content.es-ES/en-US (incrustados en tModLoader 1.4.4.9)',
  nombresCalamity: `Calamity ${cal.versionCalamity} Localization/en-US + CalamityKeep-Traduccion-ES ${cal.versionEsp} Localization/es-ES (traduccion propia de la familia Keep; Calamity no trae es-ES)`,
  obtencion: `extraer-obtencion.js sobre el decompilado de Calamity ${cal.versionCalamity} y tModLoader 1.4.4.9 (${obtencion.estadisticas?.porcentajeNoReconocido ?? '?'} % de patrones sin reconocer, listados en su salida)`,
  generado: new Date().toISOString().slice(0, 10),
};

fs.writeFileSync(SALIDA, JSON.stringify(tabla) + '\n');
const sinEs = Object.values(tabla.objetos).filter(o => o.fuenteEs === 'sin traduccion').length;
const sinObt = [...citadosDirectamente].filter(r => tabla.objetos[r] && tabla.objetos[r].obtencion.length === 0);
console.log('Escrito', SALIDA, (fs.statSync(SALIDA).size / 1024).toFixed(0) + ' KB');
console.log('objetos', Object.keys(tabla.objetos).length, '(citados', citadosDirectamente.size + ')', 'npcs', Object.keys(tabla.npcs).length,
  'estaciones', Object.keys(tabla.estaciones).length, 'grupos', Object.keys(tabla.grupos).length, 'sin traduccion', sinEs);
console.log('citados sin ninguna obtencion en el codigo (cofres/pesca/mundo):', sinObt.length, sinObt.slice(0, 40).join(' '));
if (problemas.length) console.log('PROBLEMAS (' + problemas.length + '):\n  ' + [...new Set(problemas)].join('\n  '));
