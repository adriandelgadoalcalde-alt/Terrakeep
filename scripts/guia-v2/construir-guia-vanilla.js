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
const { rotuloConjunto } = require('./conjuntos.js');

const args = process.argv.slice(2);
const arg = (n, d) => { const i = args.indexOf(n); return i >= 0 ? args[i + 1] : d; };
const SALIDA = arg('--salida', path.join(__dirname, '..', '..', 'Terrakeep.Core', 'Guia', 'V2', 'Datos', 'guia_v2_vanilla.json'));

const errores = [];
// Concordancia de numero: el token pinta el nombre oficial en singular; tras un determinante
// plural ("las [[Soul of Might]]") se usa el plural del MISMO nombre oficial como texto propio
// ({o:Terraria/SoulofMight|Almas de poder}): el sprite y el enlace no cambian (diseño §3).
const PLURALES = {
  'Medalla del Defensor': 'Medallas del Defensor', 'Duende etéreo': 'Duendes etéreos', 'Cristal de vida': 'Cristales de vida',
  'Escama de las sombras': 'Escamas de las sombras', 'Regalo': 'Regalos', 'Duende arquero': 'Duendes arqueros',
  'Bola con pinchos': 'Bolas con pinchos', 'Duende hechicero': 'Duendes hechiceros', 'Caparazón de escarabajo': 'Caparazones de escarabajo',
  'Demonio vudú': 'Demonios vudú', 'Gel': 'Geles', 'Aguijón': 'Aguijones', 'Avispón del musgo': 'Avispones del musgo',
  'Lingote sagrado': 'Lingotes sagrados', 'Fruta de la vida': 'Frutas de la vida', 'Alma de poder': 'Almas de poder',
  'Alma de visión': 'Almas de visión', 'Alma de terror': 'Almas de terror', 'Alma de luz': 'Almas de luz', 'Alma de noche': 'Almas de noche',
  'Alma de vuelo': 'Almas de vuelo', 'Cabeza meteorito': 'Cabezas meteorito', 'Bala de mosquete': 'Balas de mosquete',
  'Estrella fugaz': 'Estrellas fugaces', 'Cuchillo arrojadizo': 'Cuchillos arrojadizos', 'Fragmento de tablilla solar': 'Fragmentos de tablilla solar',
  'Serpiente voladora': 'Serpientes voladoras', 'Hilo blanco': 'Hilos blancos', 'Cofre de las sombras': 'Cofres de las sombras',
  'Flecha de madera': 'Flechas de madera', 'Llave dorada': 'Llaves doradas', 'Mineral endemoniado': 'Minerales endemoniados',
};
const REFS_ES = (() => { return { o: n => F.nombreObjetoVanilla(n, 'es_ES'), n: n => F.nombreNpcVanilla(n, 'es_ES') }; })();
const concordar = t => typeof t !== 'string' ? t : t.replace(/\b(los|las|sus|unos|unas|varios|varias|muchos|muchas|tus|estos|estas|Los|Las|Sus|Tus)(\s+)\{([on]):Terraria\/(\w+)\}/g, (all, det, sp, k, interno) => {
  const es = REFS_ES[k](interno);
  return PLURALES[es] ? `${det}${sp}{${k}:Terraria/${interno}|${PLURALES[es]}}` : all;
});
const concordarProfundo = x => typeof x === 'string' ? concordar(x) : Array.isArray(x) ? x.map(concordarProfundo)
  : x && typeof x === 'object' ? Object.fromEntries(Object.entries(x).map(([k, v]) => [k, concordarProfundo(v)])) : x;
const T = (x, donde) => concordarProfundo(R.resolverProfundo(x, errores, donde));
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
  // El oceano del lado de la Mazmorra es el mismo bioma: comparte la fila de «Océano».
  const oceano = zonas.find(z => z.id === 'oceano'), lado = zonas.find(z => z.id === 'oceano_mazmorra');
  if (oceano && lado && !lado.resumen) lado.resumen = oceano.resumen;
}

// ---- 4. escaleras (wiki oficial + verificacion contra ItemID 1.4.4.9)
const ESC = JSON.parse(fs.readFileSync(path.join(__dirname, 'datos', 'escaleras_vanilla_wiki.json'), 'utf8'));
// Conjunto (nombre ingles de la wiki) -> rotulo en español comun a las dos guias: scripts/guia-v2/conjuntos.js.
// Las notas de la wiki describen ya 1.4.5: toda frase (o parentesis) que cite un objeto que NO
// existe en 1.4.4.9 (lista noVerificados del propio archivo) se quita, para no recomendar nada
// que el jugador no pueda conseguir. Las etiquetas de seccion de la wiki pasan a negrita.
const SOLO_145 = [...new Set(ESC.noVerificados.filter(x => /^no existe en ItemID 1\.4\.4\.9|^ningún casco/.test(x.motivo))
  .map(x => x.nombreIngles.replace(/ \(casco de la clase\)$/, '')).filter(n => n.length > 3))];
const ETIQUETAS = { minions: 'Esbirros', armadura: 'Armadura', accesorios: 'Accesorios', 'pociones y mejoras': 'Pociones y mejoras',
  'accesorios ofensivos': 'Accesorios ofensivos', 'látigos': 'Látigos', centinelas: 'Centinelas' };
// «(casco Mask)» de la wiki = que variante de casco del conjunto; el sustantivo sale de los nombres
// oficiales es-ES de esas piezas (TitaniumMask «Máscara de titanio», HallowedHeadgear «Tocado sagrado»,
// BeetleShell «Coraza de escarabajo»...).
const PIEZA_CASCO = { Mask: 'la máscara', Helmet: 'el casco', Headgear: 'el tocado', Hood: 'la capucha', Visor: 'el visor', Shell: 'la coraza' };
const NOMBRES_EN_NOTAS = ['Spinal Tap', 'Cool Whip', 'Firecracker', 'Titanium Mask'];
let frasesQuitadas = 0;
function limpiarNota(t) {
  if (!t) return '';
  const cita145 = x => SOLO_145.some(n => x.includes(n));
  const lineas = t.split('\n').map(l => {
    l = l.replace(/\(([^()]*)\)/g, (all, dentro) => (cita145(dentro) ? (frasesQuitadas++, '') : all));
    return l.split(/(?<=[.!?])\s+/).filter(f => (cita145(f) ? (frasesQuitadas++, false) : true)).join(' ');
  });
  return lineas.join('\n').replace(/\[([a-záéíóúñ ]+)\]\s*/g, (all, e) => (ETIQUETAS[e] ? `**${ETIQUETAS[e]}.** ` : all))
    .replace(/ \((?:Pre-[^)]*|Gearing Up|Endgame)\)/g, '')
    // Nombres de conjunto no oficiales de la traduccion de la wiki -> los de las piezas es-ES.
    .replace(/armadura de necro\b/g, 'armadura de los muertos').replace(/(armadura|cascos?) de shroomita/g, '$1 de piñonita')
    .replace(/armadura Red Riding/g, 'armadura de Caperuza roja')
    // Nombres de objeto que la traduccion de la wiki dejo en ingles pese a tener nombre oficial
    // es-ES (F2b): se enlazan con el marcado de autor para que salga el nombre oficial y su ficha.
    .replace(/los Antlion Egg\b/g, 'los [[Antlion Eggs]]').replace(/\(casco (Mask|Helmet|Headgear|Hood|Visor|Shell)\)/g, (all, p) => '(con ' + PIEZA_CASCO[p] + ')')
    .replace(new RegExp('(?<![\\[\\w|:])(' + NOMBRES_EN_NOTAS.join('|') + ')(?![\\w\\]])', 'g'), '[[$1]]')
    .replace(/[ \t]+\n/g, '\n').replace(/\n{3,}/g, '\n\n').replace(/ {2,}/g, ' ').trim();
}
const opcion = o => {
  const marcas = (o.marcas || '').trim().split(/\s+/).filter(Boolean);
  for (const m of marcas) if (!ESC.leyendaMarcas[m]) throw new Error('Marca sin leyenda: ' + m);
  if (!/^Terraria\/\w+$/.test(o.ref) || !(o.ref.split('/')[1] in ids.item.porNombre)) throw new Error('Ref de escalera inexistente: ' + o.ref);
  return {
    ref: o.ref, origen: '', nota: T(limpiarNota(o.nota || ''), o.ref), rol: o.tipo || '',
    conjunto: o.conjunto ? rotuloConjunto(o.conjunto) : '', marcas,
  };
};
const escaleras = Object.entries(ESC.clases).map(([clase, etapas]) => ({
  clase,
  etapas: etapas.map(et => ({
    id: et.id, desde: et.desde, momento: T(et.momento, et.id),
    armas: (et.armas || []).map(opcion), armadura: (et.armadura || []).map(opcion),
    accesorios: (et.accesorios || []).map(opcion), otros: (et.otros || []).map(opcion),
    nota: T(limpiarNota(et.nota || ''), et.id), fuentes: (et.fuentes || []).map(fuenteWiki),
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
  'etapas', escaleras.reduce((a, e) => a + e.etapas.length, 0), 'frases 1.4.5 quitadas de las notas', frasesQuitadas, 'zonas sin resumen', zonas.filter(z => !z.resumen).map(z => z.id).join(','));
