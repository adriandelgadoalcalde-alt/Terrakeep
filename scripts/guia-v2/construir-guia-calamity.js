// Guia v2 (F0, 02-oct-2026): construye Terrakeep.Core/Guia/V2/Datos/guia_v2_calamity.json.
//
// Entradas:
//   1. La guia HTML del usuario (Downloads\Guia-Calamity: datos.js + capitulos.js +
//      complementos.js), SOLO LECTURA - el contenido base (47 paradas, 186 tareas, 10 capitulos).
//   2. anotaciones-calamity.js - lo que añade Terrakeep: titulos en español, condiciones
//      evaluables, ubicaciones en el mundo real, textos para todas las clases, avisos por modo.
//   3. datos/escaleras_calamity_wiki.json - escaleras de equipo de las 5 clases (wiki oficial de
//      Calamity, verificadas contra el decompilado 2.2.4).
//   4. Nombres oficiales via nombres.js/fuentes.js (Terraria es-ES de tModLoader 1.4.4.9,
//      CalamityModEsp para Calamity).
//
// Uso:  node construir-guia-calamity.js [--guia <carpeta Guia-Calamity>] [--salida <json>]
// Despues: node generar-referencias.js (tabla de nombres/ids/obtencion de todo lo citado).
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const { htmlABloques } = require('./html-a-bloques.js');
const N = require('./nombres.js');
const F = require('./fuentes.js');
const A = require('./anotaciones-calamity.js');

const args = process.argv.slice(2);
const arg = (n, d) => { const i = args.indexOf(n); return i >= 0 ? args[i + 1] : d; };
const GUIA = arg('--guia', 'C:\\Users\\adrian\\Downloads\\Guia-Calamity');
const SALIDA = arg('--salida', path.join(__dirname, '..', '..', 'Terrakeep.Core', 'Guia', 'V2', 'Datos', 'guia_v2_calamity.json'));

// ---- 1. contenido del usuario (mismo orden de carga que su Guia-Calamity.html)
const ctx = vm.createContext({});
vm.runInContext(['datos.js', 'capitulos.js', 'complementos.js'].map(f => fs.readFileSync(path.join(GUIA, f), 'utf8')).join('\n;\n') + '\n;this.__r={stages,chapters};', ctx);
const { stages, chapters } = ctx.__r;

// ---- 2. sustitucion de nombres
const mapaGlosario = g => new Map(Object.entries(g).map(([k, v]) => {
  const m = /^\{([oz]):([^}]+)\}$/.exec(v);
  return [k.toLowerCase(), m ? { tipo: m[1], ref: m[2] } : { tipo: 'texto', texto: v }];
}));
const sust = N.crearSustituidor({
  curada: N.cargarCurada(path.join(GUIA, 'inventario-sprites.json')),
  glosario: mapaGlosario(A.glosario),
  completa: N.cargarCompleta(),
  glosarioFinal: mapaGlosario(A.glosarioFinal),
});
const S = t => (t == null ? '' : sust.sustituir(String(t).replace(/<[^>]+>/g, '').replace(/\s+/g, ' ').trim()));

// Reescrituras puntuales (texto pensado solo para cuerpo a cuerpo, o que ya no aplica a una guia
// que lee tu partida real). [donde, de, a]: "donde" = "articulo:<id>" o "parada:<id>.<campo>".
const REESCRITURAS = [
  ['articulo:inicio-guia', 'Para esta guía déjalo desactivado.', 'La ruta funciona con o sin Revengeance: si lo activas, la guía lo detecta en tu mundo y añade los avisos de ese modo en cada parada.'],
  ['articulo:inicio-guia', 'Qué quiere decir «melee» aquí', 'Si juegas cuerpo a cuerpo: qué quiere decir «melee»'],
  ['articulo:inicio-guia', 'Esta guía no lee tu instalación: en Mod Browser puedes comprobar tu número real.', 'Esta guía sí lee tu partida (personaje y mundo) para marcar sola lo que ya has hecho, pero no tu lista de mods: en el navegador de mods puedes comprobar tu versión real de Calamity.'],
  ['articulo:inicio-guia', 'Los nombres ingleses se conservan para encontrarlos en tu juego. Las traducciones de títulos son explicativas, no prometen coincidir con una traducción instalada.', 'Los nombres de objetos, jefes y biomas salen de la traducción oficial al español de Terraria y de la traducción española de Calamity (CalamityModEsp); lo que no tiene traducción se queda con su nombre original.'],
  ['parada:acid1.desbloquea', 'no tu siguiente armadura melee obligatoria', 'no una armadura obligatoria para las demás clases'],
];
function reescribir(donde, texto) {
  let t = texto;
  for (const [d, de, a] of REESCRITURAS) if (d === donde && t.includes(de)) t = t.split(de).join(a);
  return t;
}

const MAPA_REAL = 'El mapa de esta guía es el de **tu propio mundo**: en Terrakeep lo ves en Exploración y en TerrakeepMod en el mapa del juego, con la siguiente parada marcada. El lado de la {z:mazmorra} (y con él el {z:mar_sulfuroso} y el {z:abismo}) puede ser el izquierdo o el derecho según tu mundo.';

function arreglarBloques(bloques, donde) {
  for (const b of bloques) {
    if (b.texto === '__MAPA_ESQUEMATICO__') b.texto = MAPA_REAL;
    else if (b.texto) b.texto = reescribir(donde, S(b.texto));
    if (b.titulo) b.titulo = reescribir(donde, S(b.titulo));
    if (b.items) b.items = b.items.map(i => reescribir(donde, S(i)));
    if (b.cabeceras) b.cabeceras = b.cabeceras.map(S);
    if (b.filas) b.filas = b.filas.map(f => f.map(c => reescribir(donde, S(c))));
    if (b.bloques) arreglarBloques(b.bloques, donde);
  }
  return bloques;
}

const fuenteWiki = p => ({ wiki: 'calamity', pagina: p });
const refsEn = (texto, tipo) => [...String(texto).matchAll(new RegExp(`\\{${tipo}:([^}|]+)`, 'g'))].map(m => m[1]);
const unicos = a => [...new Set(a)];

// Objeto invocador: nombre ingles de la guia -> ref (mismo sustituidor; si no casa, null + aviso).
const INVOCADOR_MANUAL = { 'The Codebreaker': 'CalamityMod/CodebreakerBase', 'Blessed Phoenix Egg': 'CalamityMod/YharonEgg', 'Bloodworm': 'CalamityMod/BloodwormItem', 'Chaos State': null };
const avisos = [];
function refInvocador(nombre) {
  if (nombre in INVOCADOR_MANUAL) return INVOCADOR_MANUAL[nombre];
  const t = S(nombre);
  const m = /^\{o:([^}|]+)\}$/.exec(t);
  if (!m) { avisos.push('invocador sin ref: ' + nombre + ' -> ' + t); return null; }
  return m[1];
}

// ---- 3. paradas
const capitulosIds = A.capitulos.map(c => c.id);
const paradas = stages.map(s => {
  const a = A.paradas[s.id];
  if (!a) throw new Error('Parada sin anotaciones: ' + s.id);
  const tareas = s.todo.map((t, i) => {
    const an = a.tareas[i + 1];
    if (!an) throw new Error(`Tarea sin anotacion: ${s.id}.${i + 1}`);
    const manual = an.condicion === 'manual';
    return {
      id: `${s.id}.${i + 1}`,
      texto: an.texto ? S(an.texto) : S(t),
      ...(manual ? { manual: true } : { condicion: an.condicion }),
      ...(an.clases ? { clases: an.clases } : {}),
    };
  });
  const invocacion = s.summon ? { objeto: refInvocador(s.summon[0]), donde: S(s.summon[1]), notas: S(s.summon[2]) } : null;
  const conserva = S(s.keep);
  const necesitas = [...(a.necesitas || [])];
  if (invocacion?.objeto && !necesitas.some(n => n.ref === invocacion.objeto)) necesitas.unshift({ ref: invocacion.objeto, cantidad: 1, motivo: 'invocador' });
  return {
    id: s.id,
    capitulo: capitulosIds[s.era],
    titulo: S(a.titulo),
    tipo: a.tipo,
    opcional: !!a.opcional,
    etiqueta: S(s.boss),
    jefes: a.jefes || [],
    vidaObjetivo: { min: a.vida.min, max: a.vida.max, trasMejora: !!a.vida.trasMejora, texto: S(s.hp) },
    donde: S(s.where),
    ubicaciones: a.ubicaciones,
    invocacion,
    preparate: S(a.preparate),
    preparatePorClase: { cuerpo_a_cuerpo: S(s.gear) },
    necesitas: necesitas.map(n => ({ ref: n.ref, cantidad: n.cantidad || 1, motivo: n.motivo || '' })),
    tareas,
    combate: reescribir(`parada:${s.id}.combate`, S(s.fight)),
    desbloquea: reescribir(`parada:${s.id}.desbloquea`, S(s.unlock)),
    listoCuando: reescribir(`parada:${s.id}.listoCuando`, S(s.stop)),
    completadaCuando: a.completadaCuando || null,
    conserva,
    conservaObjetos: unicos(refsEn(conserva, 'o')),
    avisos: (a.avisos || []).map((v, i) => ({ id: `${s.id}.aviso${i + 1}`, modos: v.modos, texto: S(v.texto) })),
    fuentes: s.sources.map(fuenteWiki),
  };
});

// ---- 4. capitulos del manual -> articulos + fichas estructuradas
const ICONOS = { 'inicio-guia': 'inicio', mapa: 'mapa', equipo: 'equipo', vida: 'vida', draedon: 'laboratorio', materiales: 'materiales', estructuras: 'hallazgo', secretos: 'secreto', problemas: 'perdido', fuentes: 'fuentes' };
const articulos = [];
const problemas = [];
const hallazgos = [];
const avisosHtml = [];
for (const c of chapters) {
  let bloques = arreglarBloques(htmlABloques(c.html, avisosHtml), 'articulo:' + c.id);
  if (c.id === 'estructuras') {
    // Cada caja = una ficha "He encontrado algo raro".
    const resto = [];
    for (const b of bloques) {
      if (b.tipo !== 'cajas') { resto.push(b); continue; }
      for (const caja of b.bloques) {
        const texto = JSON.stringify(caja.bloques);
        const fuentes = caja.bloques.filter(x => x.tipo === 'fuentes').flatMap(x => x.fuentes);
        hallazgos.push({
          id: 'hallazgo' + (hallazgos.length + 1),
          titulo: caja.titulo,
          bloques: caja.bloques.filter(x => x.tipo !== 'fuentes'),
          paradas: unicos(refsEn(texto, 'p')),
          zonas: unicos(refsEn(texto, 'z')),
          fuentes,
        });
      }
    }
    bloques = resto;
  }
  if (c.id === 'problemas') {
    // Cada fila de las tablas "Lo que pasa | Comprueba esto primero" = una ficha "Estoy perdido".
    const resto = [];
    for (const b of bloques) {
      if (b.tipo === 'tabla' && b.cabeceras.length === 2) {
        for (const f of b.filas) {
          problemas.push({
            id: 'problema' + (problemas.length + 1),
            titulo: f[0],
            bloques: [{ tipo: 'parrafo', texto: f[1] }],
            paradas: unicos(refsEn(f[1], 'p')),
            zonas: unicos(refsEn(f.join(' '), 'z')),
            fuentes: [],
          });
        }
      } else resto.push(b);
    }
    bloques = resto;
  }
  let titulo = S(c.title), subtitulo = S(c.subtitle);
  if (c.id === 'inicio-guia') subtitulo = 'Tu partida: clase, dificultad y modos de Calamity';
  if (c.id === 'equipo') {
    titulo = 'Equipo cuerpo a cuerpo';
    bloques.unshift({ tipo: 'aviso', estilo: 'suave', texto: 'Este capítulo es el manual de equipo **cuerpo a cuerpo**. La escalera de equipo de **tu clase** (cuerpo a cuerpo, distancia, magia, invocación o pícaro), etapa por etapa y con la forma de conseguir cada objeto, está en cada parada y en «Mi equipo».' });
  }
  articulos.push({ id: c.id, titulo, subtitulo, icono: ICONOS[c.id] || '', bloques });
}

// ---- 5. zonas
const ids = F.ids();
const calNombres = F.nombresCalamity();
const bioEs = F.locTerraria('es_ES', 'Game').Bestiary_Biomes;
function nombreZona(n) {
  const parte = x => x.terraria ? [bioEs[x.terraria], 'Terraria es-ES oficial (Bestiary_Biomes.' + x.terraria + ')']
    : x.calamity ? [calNombres.es.Biomes[x.calamity] || null, 'CalamityModEsp (Biomes.' + x.calamity + ')']
      : x.objeto ? [null, 'objeto:' + x.objeto] : [x.texto, 'texto'];
  let [n1, f1] = parte(n);
  if (n.objeto) {
    const [mod, interno] = n.objeto.split('/');
    n1 = mod === 'Terraria' ? F.nombreObjetoVanilla(interno, 'es_ES') : calNombres.es.Items[interno];
    f1 = (mod === 'Terraria' ? 'Terraria es-ES oficial' : 'CalamityModEsp') + ' (nombre del objeto)';
  }
  if (!n1) throw new Error('Zona sin nombre oficial: ' + JSON.stringify(n));
  if (n.sufijo) {
    const [n2, f2] = parte(n.sufijo);
    return [n1.replace(/^Laboratorios/, 'Laboratorio') + ' (' + n2.toLowerCase() + ')', f1 + ' + ' + f2];
  }
  return [n1, f1];
}
const zonas = A.zonas.map(z => {
  const [nombre, fuenteNombre] = nombreZona(z.nombre);
  const tiles = (z.tiles || []).map(t => {
    if (!(t in ids.tile.porNombre)) throw new Error('TileID inexistente: ' + t);
    return ids.tile.porNombre[t];
  });
  const clases = F.clasesCalamity();
  const tilesMod = (z.tilesMod || []).map(t => {
    if (!clases.Tiles[t]) throw new Error('Tile de Calamity inexistente en 2.2.4: ' + t);
    return 'CalamityMod/' + t;
  });
  return {
    id: z.id, nombre, fuenteNombre, ambito: z.ambito || 'vanilla', capa: z.capa || 'cualquiera',
    firma: { tiles, tilesMod, minimo: z.minimo ?? 40 },
    ...(z.punto ? { punto: z.punto } : {}),
    resumen: '', bloques: [], fuentes: [],
  };
});

// ---- 6. escaleras por clase (wiki oficial + verificacion contra el decompilado)
const ESC = JSON.parse(fs.readFileSync(path.join(__dirname, 'datos', 'escaleras_calamity_wiki.json'), 'utf8'));
const fuenteDeUrl = u => {
  const m = /^https:\/\/calamitymod\.wiki\.gg\/wiki\/([^#?]+)(#.*)?$/.exec(u);
  return m ? { wiki: 'calamity', pagina: decodeURIComponent(m[1]).replace(/_/g, ' ') } : { url: u };
};
// En la wiki, el campo de marcas de un objeto a veces lleva una nota en ingles en vez de simbolos
// (Tarragon Wings: "This has more flight time than the other wings."). Se traduce aqui, a mano y
// literal, y pasa a la nota; las marcas solo conservan simbolos de la leyenda.
const NOTAS_WIKI = { 'This has more flight time than the other wings.': 'Tiene más tiempo de vuelo que las otras alas.' };
const opcion = o => {
  const crudo = (o.marcas || '').trim();
  const esNota = crudo && !crudo.split(/\s+/).every(m => ESC.leyendaMarcas[m[0]]);
  if (esNota && !NOTAS_WIKI[crudo]) throw new Error('Marca de la wiki sin traducir: ' + crudo);
  return {
    ref: o.ref, origen: '', nota: S([o.nota, esNota ? NOTAS_WIKI[crudo] : ''].filter(Boolean).join(' ')),
    rol: o.tipo || '', conjunto: o.conjunto ? S(o.conjunto) : '',
    marcas: esNota ? [] : crudo.split(/\s+/).filter(Boolean),
  };
};
const escaleras = Object.entries(ESC.clases).map(([clase, etapas]) => ({
  clase,
  etapas: etapas.map(et => ({
    id: et.id, desde: et.desde, momento: S(et.momento),
    armas: (et.armas || []).map(opcion), armadura: (et.armadura || []).map(opcion),
    accesorios: (et.accesorios || []).map(opcion), otros: (et.otros || []).map(opcion),
    nota: S(et.nota || ''), fuentes: (et.fuentes || []).map(fuenteDeUrl),
  })),
}));

// ---- 7. documento
const doc = {
  esquema: 2,
  id: 'calamity',
  ambito: 'calamity',
  titulo: 'De tu primera espada a los dos finales',
  subtitulo: 'Guía de progresión de Terraria con Calamity, para todas las clases y modos',
  referencia: {
    terraria: '1.4.4.9 (tModLoader)', calamity: '2.2.4', fechaInvestigacion: '2026-10-01',
    nota: 'Contenido base: guía del usuario (investigación cerrada el 1-oct-2026, referencia Calamity 2.2.4). Escaleras por clase: calamitymod.wiki.gg Guide:Class setups (consultada el 2-oct-2026, la wiki declara 2.1.2; cada objeto verificado en el decompilado 2.2.4). Recetas, botín y tiendas: código decompilado de Calamity 2.2.4 y tModLoader 1.4.4.9.',
  },
  creditos: 'Basada en la guía «Terraria + Calamity · Manual de campo» de Adrián, ampliada para todas las clases y modos y conectada a tu partida real.',
  clases: ['cuerpo_a_cuerpo', 'distancia', 'magia', 'invocacion', 'picaro'],
  capitulos: A.capitulos.map(c => ({ ...c, resumen: S(c.resumen) })),
  paradas,
  articulos,
  escaleras,
  leyendaEscaleras: ESC.leyendaMarcas,
  zonas,
  avisosModo: A.avisosModo.map(a => ({ ...a, texto: S(a.texto) })),
  problemas,
  hallazgos,
};

fs.mkdirSync(path.dirname(SALIDA), { recursive: true });
fs.writeFileSync(SALIDA, JSON.stringify(doc, null, 1) + '\n');

// ---- 8. informe
const restoIngles = new Map();
const PALABRAS_EN = /\b([A-Z][a-z]+(?:[ '’-][A-Z][a-z]+)+)\b/g;
const recorrer = x => {
  if (typeof x === 'string') { for (const m of x.replace(/\{[^}]*\}/g, '').matchAll(PALABRAS_EN)) restoIngles.set(m[1], (restoIngles.get(m[1]) || 0) + 1); return; }
  if (Array.isArray(x)) x.forEach(recorrer); else if (x && typeof x === 'object') for (const [k, v] of Object.entries(x)) if (!['fuentes', 'ref', 'refs', 'jefes', 'id'].includes(k)) recorrer(v);
};
recorrer({ paradas, articulos, problemas, hallazgos });
console.log('Escrito', SALIDA);
console.log('paradas', paradas.length, 'tareas', paradas.reduce((a, p) => a + p.tareas.length, 0), 'articulos', articulos.length, 'problemas', problemas.length, 'hallazgos', hallazgos.length, 'zonas', zonas.length, 'etapas', escaleras.reduce((a, e) => a + e.etapas.length, 0));
if (avisos.length) console.log('AVISOS', avisos);
if (avisosHtml.length) console.log('AVISOS HTML', unicos(avisosHtml));
const top = [...restoIngles.entries()].sort((a, b) => b[1] - a[1]);
fs.writeFileSync(path.join(require('os').tmpdir(), 'guia-v2-informe-ingles.txt'), top.map(([k, v]) => v + '\t' + k).join('\n'));
console.log('Nombres en ingles que quedan (multipalabra):', top.length, top.slice(0, 40).map(([k, v]) => `${k}(${v})`).join(', '));
