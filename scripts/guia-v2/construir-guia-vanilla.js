// Guia v2 (F1, 02-oct-2026): construye Terrakeep.Core/Guia/V2/Datos/guia_v2_vanilla.json.
//
// Entradas (todas en este repo; no hay guia HTML de partida para vanilla):
//   1. vanilla/paradas-1.js y paradas-2.js - la ruta: 49 paradas con tareas y condiciones.
//   2. vanilla/manual-a.js, manual-b.js y fichas.js - capitulos de manual, «Estoy perdido» y «He
//      encontrado algo raro».
//   3. anotaciones-vanilla.js - capitulos de ruta, zonas, avisos por modo y objetos necesarios.
//   4. datos/escaleras_vanilla_wiki.json - escaleras de equipo de las 4 clases (terraria.wiki.gg
//      Guide:Class setups, cada objeto verificado contra ItemID de tModLoader 1.4.4.9).
//   5. Nombres oficiales: Terraria es-ES de tModLoader 1.4.4.9 (fuentes.js). El marcado de autor
//      [[Nombre ingles exacto]] se resuelve a {o:..}/{n:..} por nombre EXACTO; uno inexistente o
//      ambiguo para el build (vanilla/resolver-nombres.js). Nada se adivina.
//
// Uso:  node construir-guia-vanilla.js [--salida <json>]
// Despues: node generar-referencias.js (tabla de nombres/ids/obtencion de todo lo citado).
'use strict';
const fs = require('fs');
const path = require('path');
const F = require('./fuentes.js');
const R = require('./vanilla/resolver-nombres.js');
const AY = require('./vanilla/ayudas.js');
const A = require('./anotaciones-vanilla.js');

const args = process.argv.slice(2);
const arg = (n, d) => { const i = args.indexOf(n); return i >= 0 ? args[i + 1] : d; };
const SALIDA = arg('--salida', path.join(__dirname, '..', '..', 'Terrakeep.Core', 'Guia', 'V2', 'Datos', 'guia_v2_vanilla.json'));

const errores = [];
const T = (x, donde) => R.resolverProfundo(x, errores, donde);
const unicos = a => [...new Set(a)];
const refsEn = (texto, tipo) => [...String(texto).matchAll(new RegExp(`\\{${tipo}:([^}|]+)`, 'g'))].map(m => m[1]);
const refDe = (nombre, tipo) => {
  const r = R.resolverUno(/^[on]:/.test(nombre) ? nombre : `${tipo}:${nombre}`);
  if (r.error) { errores.push(r.error); return null; }
  return r.ref;
};
const fuenteWiki = f => (typeof f === 'string' ? fuenteDeUrl(f) : f);
function fuenteDeUrl(u) {
  const m = /^https:\/\/terraria\.wiki\.gg\/wiki\/([^#?]+)(#.*)?$/.exec(u);
  return m ? { wiki: 'terraria', pagina: decodeURIComponent(m[1]).replace(/_/g, ' ') } : { url: u };
}

// ---- 1. paradas
const CLASES = ['cuerpo_a_cuerpo', 'distancia', 'magia', 'invocacion'];
const crudas = [...require('./vanilla/paradas-1.js').paradas, ...require('./vanilla/paradas-2.js').paradas];
const capitulosIds = new Set(A.capitulos.map(c => c.id));
const paradas = crudas.map(p => {
  if (!capitulosIds.has(p.capitulo)) throw new Error(`Parada ${p.id}: capitulo desconocido ${p.capitulo}`);
  for (const k of Object.keys(p.porClase || {})) if (!CLASES.includes(k)) throw new Error(`Parada ${p.id}: clase ${k}`);
  const invocacion = p.invocacion
    ? { objeto: p.invocacion.objeto ? refDe(p.invocacion.objeto, 'o') : null, donde: T(p.invocacion.donde, p.id), notas: T(p.invocacion.notas, p.id) }
    : null;
  const necesitas = [];
  if (invocacion?.objeto) necesitas.push({ ref: invocacion.objeto, cantidad: 1, motivo: 'Invocador.', clases: [] });
  for (const n of A.necesitas[p.id] || []) {
    const [nombre, cantidad, motivo, clases] = Array.isArray(n) ? [n[0], n[1], n[2], []] : [n.n, n.c || 1, n.motivo, n.clases || []];
    const ref = refDe(nombre, 'o');
    if (ref && !necesitas.some(x => x.ref === ref)) necesitas.push({ ref, cantidad, motivo: T(motivo || '', p.id), clases });
  }
  const conserva = T(p.conserva || '', p.id);
  return {
    id: p.id,
    capitulo: p.capitulo,
    titulo: T(p.titulo, p.id),
    tipo: p.tipo,
    opcional: !!p.opcional,
    etiqueta: 'Terraria',
    jefes: (p.jefes || []).map(j => refDe(j, 'n')).filter(Boolean),
    vidaObjetivo: { min: p.vida.min, max: p.vida.max, trasMejora: !!p.vida.trasMejora, texto: p.vida.texto },
    donde: T(p.donde, p.id),
    ubicaciones: p.ubicaciones,
    invocacion,
    preparate: T(p.preparate, p.id),
    preparatePorClase: T(p.porClase || {}, p.id),
    necesitas,
    tareas: p.tareas.map((t, i) => ({
      id: `${p.id}.${i + 1}`,
      texto: T(t.texto, p.id),
      ...(t.condicion === AY.M ? { manual: true } : { condicion: t.condicion }),
      ...(t.opcional ? { opcional: true } : {}),
      ...(t.clases ? { clases: t.clases } : {}),
    })),
    combate: T(p.combate, p.id),
    desbloquea: T(p.desbloquea, p.id),
    listoCuando: T(p.listoCuando, p.id),
    completadaCuando: p.completadaCuando || null,
    conserva,
    conservaObjetos: unicos(refsEn(conserva, 'o')),
    avisos: (p.avisos || []).map((v, i) => ({ id: `${p.id}.aviso${i + 1}`, modos: v.modos, texto: T(v.texto, p.id) })),
    fuentes: p.fuentes.map(fuenteWiki),
  };
});

// ---- 2. manual y fichas
const ORDEN_ARTICULOS = ['inicio-guia', 'mapa', 'mazmorra', 'templo', 'inframundo', 'jungla', 'maldad', 'eventos', 'vecinos', 'vida', 'pesca', 'materiales', 'eter', 'estructuras', 'problemas', 'fuentes'];
const MA = require('./vanilla/manual-a.js'), MB = require('./vanilla/manual-b.js'), FI = require('./vanilla/fichas.js');
const todosArticulos = [...MA.articulos, ...MB.articulos, ...FI.articulos];
const articulos = ORDEN_ARTICULOS.map(id => {
  const a = todosArticulos.find(x => x.id === id);
  if (!a) throw new Error('Falta el articulo ' + id);
  return T({ id: a.id, titulo: a.titulo, subtitulo: a.subtitulo || '', icono: a.icono || '', bloques: a.bloques }, a.id);
});
for (const a of todosArticulos) if (!ORDEN_ARTICULOS.includes(a.id)) throw new Error('Articulo no previsto: ' + a.id);
const ficha = f => T({ id: f.id, titulo: f.titulo, bloques: f.bloques, paradas: f.paradas || [], zonas: f.zonas || [], fuentes: (f.fuentes || []).map(fuenteWiki) }, f.id);
const problemas = FI.problemas.map(ficha);
const hallazgos = FI.hallazgos.map(ficha);

// ---- 3. zonas
const ids = F.ids();
const bioEs = F.locTerraria('es_ES', 'Game').Bestiary_Biomes;
const mapEs = F.locTerraria('es_ES', 'Game').MapObject;
function nombreZona(n) {
  if (n.terraria) {
    let es = bioEs[n.terraria];
    if (!es) throw new Error('Bioma sin nombre oficial: ' + n.terraria);
    let fuente = 'Terraria es-ES oficial (Bestiary_Biomes.' + n.terraria + ')';
    if (n.sufijo) {
      const s2 = bioEs[n.sufijo.terraria];
      es += ' (' + n.sufijo.prefijo + ' ' + s2.replace(/^La /, 'la ') + ')';
      fuente += ' + Bestiary_Biomes.' + n.sufijo.terraria;
    }
    return [es, fuente];
  }
  if (n.objeto) return [F.nombreObjetoVanilla(n.objeto, 'es_ES'), 'Terraria es-ES oficial (nombre del objeto ' + n.objeto + ')'];
  if (n.mapa) return [n.mapa.map(k => mapEs[k]).join(' / '), 'Terraria es-ES oficial (MapObject.' + n.mapa.join(', MapObject.') + ')'];
  if (n.texto) return [n.texto, n.fuente];
  throw new Error('Zona sin nombre: ' + JSON.stringify(n));
}
const zonas = A.zonas.map(z => {
  const [nombre, fuenteNombre] = nombreZona(z.nombre);
  if (!nombre) throw new Error('Zona sin nombre oficial: ' + z.id);
  const tiles = (z.tiles || []).map(t => {
    if (!(t in ids.tile.porNombre)) throw new Error('TileID inexistente: ' + t);
    return ids.tile.porNombre[t];
  });
  return {
    id: z.id, nombre, fuenteNombre, ambito: 'vanilla', capa: z.capa || 'cualquiera',
    firma: { tiles, tilesMod: [], minimo: z.minimo ?? 40 },
    ...(z.punto ? { punto: z.punto } : {}),
    resumen: '', bloques: [], fuentes: [],
  };
});
// Resumen de cada zona (tooltip del marcador): de las tablas del capitulo «Mapa» (primera columna
// {z:id...}): «qué hay» + «cuándo ir».
{
  const mapa = articulos.find(a => a.id === 'mapa');
  const filas = [];
  const rec = bs => { for (const b of bs) { if (b.tipo === 'tabla') filas.push(...b.filas); if (b.bloques) rec(b.bloques); } };
  rec(mapa.bloques);
  for (const z of zonas) {
    const f = filas.find(r => new RegExp(`^\\{z:${z.id}[|}]`).test(r[0]));
    if (f) z.resumen = [f[1], f[2]].filter(Boolean).join(' ');
  }
}

// ---- 4. escaleras (wiki oficial + verificacion contra ItemID 1.4.4.9)
const ESC = JSON.parse(fs.readFileSync(path.join(__dirname, 'datos', 'escaleras_vanilla_wiki.json'), 'utf8'));
// Conjunto (nombre ingles de la wiki) -> nombre en español sacado de los nombres OFICIALES es-ES de
// sus piezas (p. ej. MoltenHelmet «Casco fundido» -> «armadura fundida»; JungleHat «Casco para la
// selva» -> «armadura para la selva»).
const CONJUNTOS = {
  'Cactus armor': 'de cactus', 'Wooden armor': 'de madera', 'Platinum armor': 'de platino', 'Gladiator armor': 'de gladiador',
  'Gold armor': 'de oro', 'Pumpkin armor': 'de calabaza', 'Molten armor': 'fundida', 'Shadow armor': 'de las sombras',
  'Crimson armor': 'carmesí', 'Bee armor': 'de abeja', 'Adamantite armor': 'de adamantita', 'Titanium armor': 'de titanio',
  'Palladium armor': 'de paladio', 'Crystal Assassin armor': 'de asesino de cristal', 'Hallowed armor': 'sagrada',
  'Turtle armor': 'de tortuga', 'Chlorophyte armor': 'de clorofita', 'Beetle armor': 'de escarabajo',
  'Solar Flare armor': 'de fulguración solar', 'Fossil armor': 'de fósil', 'Necro armor': 'de los muertos', 'Frost armor': 'helada',
  'Shroomite armor': 'de piñonita', 'Vortex armor': 'del vórtice', 'Jungle armor': 'para la selva', 'Meteor armor': 'de meteorito',
  'Spectre armor': 'espectral', 'Dark Artist armor': 'de Artista Oscuro', 'Nebula armor': 'de nebulosa', 'Obsidian armor': 'de obsidiana',
  'Spider armor': 'de araña', 'Squire armor': 'de escudero', 'Tiki armor': 'tiki', 'Spooky armor': 'tétrica',
  'Valhalla Knight armor': 'de Caballero del Valhalla', 'Stardust armor': 'de polvo estelar',
  'Cobalt armor': 'de cobalto', 'Mythril armor': 'de mithril', 'Orichalcum armor': 'de oricalco', 'Silver armor': 'de plata',
  'Tungsten armor': 'de tungsteno', 'Iron armor': 'de hierro', 'Lead armor': 'de plomo', 'Copper armor': 'de cobre', 'Tin armor': 'de estaño',
  'Ninja armor': 'ninja', 'Monk armor': 'de monje', 'Huntress armor': 'de cazadora', 'Apprentice armor': 'de aprendiz',
  'Shinobi Infiltrator armor': 'de infiltrado shinobi', 'Red Riding armor': 'de Caperucita', 'Forbidden armor': 'prohibida',
};
const opcion = o => {
  const marcas = (o.marcas || '').trim().split(/\s+/).filter(Boolean);
  for (const m of marcas) if (!ESC.leyendaMarcas[m]) throw new Error('Marca sin leyenda: ' + m);
  if (o.conjunto && !CONJUNTOS[o.conjunto]) throw new Error('Conjunto sin nombre en español: ' + o.conjunto);
  if (!/^Terraria\/\w+$/.test(o.ref) || !(o.ref.split('/')[1] in ids.item.porNombre)) throw new Error('Ref de escalera inexistente: ' + o.ref);
  return {
    ref: o.ref, origen: '', nota: T(o.nota || '', o.ref), rol: o.tipo || '',
    conjunto: o.conjunto ? 'armadura ' + CONJUNTOS[o.conjunto] : '', marcas,
  };
};
const escaleras = Object.entries(ESC.clases).map(([clase, etapas]) => ({
  clase,
  etapas: etapas.map(et => ({
    id: et.id, desde: et.desde, momento: T(et.momento, et.id),
    armas: (et.armas || []).map(opcion), armadura: (et.armadura || []).map(opcion),
    accesorios: (et.accesorios || []).map(opcion), otros: (et.otros || []).map(opcion),
    nota: T(et.nota || '', et.id), fuentes: (et.fuentes || []).map(fuenteWiki),
  })),
}));
if (JSON.stringify(escaleras.map(e => e.clase)) !== JSON.stringify(CLASES)) throw new Error('Clases de escalera inesperadas: ' + escaleras.map(e => e.clase));

// ---- 5. documento
const doc = {
  esquema: 2,
  id: 'vanilla',
  ambito: 'vanilla',
  titulo: 'De tu primera noche al Señor de la Luna',
  subtitulo: 'Guía de progresión de Terraria, para todas las clases y modos',
  referencia: {
    terraria: '1.4.4.9 (tModLoader)', calamity: '', fechaInvestigacion: '2026-10-02',
    nota: 'Contenido escrito para Terrakeep (F1, 02-oct-2026) sobre Terraria 1.4.4.9: mecánicas comprobadas en el código decompilado de tModLoader 1.4.4.9 y en terraria.wiki.gg. Escaleras por clase: terraria.wiki.gg Guide:Class setups (consultada el 2-oct-2026; la wiki ya describe 1.4.5 y cada objeto se verificó contra ItemID 1.4.4.9). Recetas, botín y tiendas: código decompilado.',
  },
  creditos: 'Guía vanilla de Terrakeep, con el mismo formato que la guía «Terraria + Calamity · Manual de campo» y conectada a tu partida real.',
  clases: CLASES,
  capitulos: A.capitulos.map(c => ({ ...c, resumen: T(c.resumen, c.id) })),
  paradas,
  articulos,
  escaleras,
  leyendaEscaleras: ESC.leyendaMarcas,
  zonas,
  avisosModo: A.avisosModo.map(a => ({ ...a, texto: T(a.texto, a.id) })),
  problemas,
  hallazgos,
};

// ---- 6. comprobaciones de coherencia (antes de escribir)
const idsParada = new Set(paradas.map(p => p.id)), idsZona = new Set(zonas.map(z => z.id)), idsArt = new Set(articulos.map(a => a.id));
const recorrerTokens = (x, donde) => {
  if (typeof x === 'string') {
    for (const m of x.matchAll(/\{([zpa]):([^}|]+)/g)) {
      const ok = m[1] === 'z' ? idsZona.has(m[2]) : m[1] === 'p' ? idsParada.has(m[2]) : idsArt.has(m[2]);
      if (!ok) errores.push(`${donde}: enlace roto {${m[1]}:${m[2]}}`);
    }
    if (/\[\[/.test(x)) errores.push(`${donde}: marcado [[...]] sin resolver`);
    return;
  }
  if (Array.isArray(x)) x.forEach(v => recorrerTokens(v, donde));
  else if (x && typeof x === 'object') for (const [k, v] of Object.entries(x)) recorrerTokens(v, x.id || donde);
};
recorrerTokens(doc, 'doc');
for (const p of paradas) for (const u of p.ubicaciones) if (u.tipo === 'zona' && !idsZona.has(u.id)) errores.push(`${p.id}: ubicacion con zona desconocida ${u.id}`);
for (const f of [...problemas, ...hallazgos]) {
  for (const p of f.paradas) if (!idsParada.has(p)) errores.push(`${f.id}: parada desconocida ${p}`);
  for (const z of f.zonas) if (!idsZona.has(z)) errores.push(`${f.id}: zona desconocida ${z}`);
}
for (const e of escaleras) for (const et of e.etapas) if (!idsParada.has(et.desde)) errores.push(`${et.id}: desde desconocido ${et.desde}`);
const todos = unicos([...errores, ...AY.errores]);
if (todos.length) { console.error(todos.length + ' ERRORES:\n  ' + todos.join('\n  ')); process.exit(1); }

fs.mkdirSync(path.dirname(SALIDA), { recursive: true });
fs.writeFileSync(SALIDA, JSON.stringify(doc, null, 1) + '\n');

// ---- 7. informe
const tareas = paradas.flatMap(p => p.tareas);
const evaluables = tareas.filter(t => t.condicion).length;
console.log('Escrito', SALIDA);
console.log('capitulos', doc.capitulos.length, 'paradas', paradas.length, `(opcionales ${paradas.filter(p => p.opcional).length}, con completadaCuando ${paradas.filter(p => p.completadaCuando).length})`,
  'tareas', tareas.length, `(evaluables ${evaluables} = ${(100 * evaluables / tareas.length).toFixed(1)} %)`,
  'articulos', articulos.length, 'problemas', problemas.length, 'hallazgos', hallazgos.length, 'zonas', zonas.length,
  'etapas', escaleras.reduce((a, e) => a + e.etapas.length, 0), 'zonas sin resumen', zonas.filter(z => !z.resumen).map(z => z.id).join(','));
