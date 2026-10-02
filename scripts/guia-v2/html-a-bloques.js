// Guia v2 (F0): convierte el HTML de los capitulos de la guia del usuario (Guia-Calamity) en
// Bloques del modelo v2 (titulo/parrafo/lista/tabla/aviso/cajas/fuentes). El HTML es generado
// por sus propias plantillas (capitulos.js), bien formado y con un vocabulario cerrado de
// etiquetas, asi que basta un parser minimo. Lo que no se reconozca se avisa, nunca se pierde
// en silencio.
'use strict';

const ENTIDADES = { amp: '&', lt: '<', gt: '>', quot: '"', '#39': "'", nbsp: ' ', rarr: '→' };
const decodificar = s => s.replace(/&(#?\w+);/g, (a, e) => ENTIDADES[e] ?? (e[0] === '#' ? String.fromCodePoint(Number(e.slice(1))) : a));

// ---- parser: arbol de {tag, attrs, hijos} y cadenas de texto
function parsear(html) {
  const raiz = { tag: '#raiz', attrs: {}, hijos: [] };
  const pila = [raiz];
  const re = /<!--[\s\S]*?-->|<\/(\w+)\s*>|<(\w+)((?:\s+[\w-]+(?:="[^"]*"|='[^']*')?)*)\s*(\/?)>|([^<]+)/g;
  const vacios = new Set(['br', 'img', 'input', 'hr']);
  let m;
  while ((m = re.exec(html))) {
    const actual = pila[pila.length - 1];
    if (m[0].startsWith('<!--')) continue;
    if (m[1]) {
      // cierre: desapila hasta el tag correspondiente
      for (let i = pila.length - 1; i > 0; i--) if (pila[i].tag === m[1]) { pila.length = i; break; }
    } else if (m[2]) {
      const attrs = {};
      for (const a of m[3].matchAll(/([\w-]+)(?:="([^"]*)"|='([^']*)')?/g)) attrs[a[1]] = decodificar(a[2] ?? a[3] ?? '');
      const nodo = { tag: m[2].toLowerCase(), attrs, hijos: [] };
      actual.hijos.push(nodo);
      if (!vacios.has(nodo.tag) && !m[4]) pila.push(nodo);
    } else if (m[5]) {
      actual.hijos.push(decodificar(m[5]));
    }
  }
  return raiz;
}

const clases = n => (n.attrs?.class || '').split(/\s+/).filter(Boolean);

// Pagina de wiki a partir de un href de wiki.gg; null si no es de wiki.
function fuenteDeEnlace(href, texto) {
  let m = /^https:\/\/calamitymod\.wiki\.gg\/wiki\/(.+)$/.exec(href);
  if (m) return { wiki: 'calamity', pagina: decodeURIComponent(m[1]).replace(/_/g, ' '), texto: texto && texto !== decodeURIComponent(m[1]).replace(/_/g, ' ') ? texto : '' };
  m = /^https:\/\/terraria\.wiki\.gg\/wiki\/(.+)$/.exec(href);
  if (m) return { wiki: 'terraria', pagina: decodeURIComponent(m[1]).replace(/_/g, ' '), texto: '' };
  return { url: href, texto };
}

// Texto en linea: <b>/<strong> -> **..**, enlaces -> su texto (y la fuente se recoge aparte),
// botones de ruta -> {p:id}/{a:id}.
function enLinea(nodos, fuentes, avisos) {
  let s = '';
  for (const n of nodos) {
    if (typeof n === 'string') { s += n; continue; }
    switch (n.tag) {
      case 'b': case 'strong': { const t = enLinea(n.hijos, fuentes, avisos).trim(); s += t ? '**' + t + '**' : ''; break; }
      case 'a': {
        const t = enLinea(n.hijos, fuentes, avisos);
        if (n.attrs.href) fuentes.push(fuenteDeEnlace(n.attrs.href, t.trim()));
        s += t;
        break;
      }
      case 'button':
        if (n.attrs['data-stage']) s += ` {p:${n.attrs['data-stage']}}`;
        else if (n.attrs['data-view']) s += ` {a:${n.attrs['data-view']}}`;
        else s += enLinea(n.hijos, fuentes, avisos);
        break;
      case 'br': s += ' '; break;
      case 'span': case 'em': case 'i': case 'small': case 'code': case 'label': s += enLinea(n.hijos, fuentes, avisos); break;
      case 'img': break;
      default:
        avisos.push('etiqueta en linea no prevista: <' + n.tag + '>');
        s += enLinea(n.hijos, fuentes, avisos);
    }
  }
  return s.replace(/\s+/g, ' ');
}

function bloques(nodos, avisos) {
  const out = [];
  let fuentesSueltas = [];
  const parrafo = (texto, extra = {}) => { const t = texto.trim(); if (t) out.push({ tipo: 'parrafo', texto: t, ...extra }); };

  for (const n of nodos) {
    if (typeof n === 'string') { parrafo(n); continue; }
    const c = clases(n);
    const fuentes = [];
    switch (n.tag) {
      case 'h2': case 'h3': case 'h4':
        out.push({ tipo: 'titulo', texto: enLinea(n.hijos, fuentes, avisos).trim() });
        break;
      case 'p':
        if (c.includes('refs')) {
          enLinea(n.hijos, fuentes, avisos);
          out.push({ tipo: 'fuentes', fuentes });
        } else if (c.includes('notice')) {
          out.push({ tipo: 'aviso', estilo: 'nota', texto: enLinea(n.hijos, fuentes, avisos).trim(), ...(fuentes.length ? { fuentes } : {}) });
        } else {
          const t = enLinea(n.hijos, fuentes, avisos).trim();
          if (t) out.push({ tipo: 'parrafo', texto: t, ...(fuentes.length ? { fuentes } : {}) });
        }
        break;
      case 'ul': case 'ol': {
        const items = n.hijos.filter(h => typeof h !== 'string' && h.tag === 'li').map(li => enLinea(li.hijos, fuentes, avisos).trim());
        out.push({ tipo: 'lista', numerada: n.tag === 'ol', items, ...(fuentes.length ? { fuentes } : {}) });
        break;
      }
      case 'table': out.push(tabla(n, avisos)); break;
      case 'div':
        if (c.includes('tablewrap')) out.push(...bloques(n.hijos, avisos));
        else if (c.includes('callout')) out.push({ tipo: 'aviso', estilo: c.includes('soft') ? 'suave' : 'destacado', texto: enLinea(n.hijos, fuentes, avisos).trim(), ...(fuentes.length ? { fuentes } : {}) });
        else if (c.includes('grid2') || c.includes('grid3')) out.push({ tipo: 'cajas', bloques: n.hijos.filter(h => typeof h !== 'string').map(h => caja(h, avisos)) });
        else if (c.includes('worldmap')) out.push({ tipo: 'aviso', estilo: 'nota', texto: '__MAPA_ESQUEMATICO__' });
        else if (c.includes('flow') || c.includes('healthroad')) {
          // Diagrama de pasos: cada <span> es un paso (las flechas <b>→</b> se descartan).
          const items = n.hijos.filter(h => typeof h !== 'string' && h.tag === 'span').map(sp => enLinea(sp.hijos.map(x => (typeof x !== 'string' && x.tag === 'small') ? { tag: 'span', attrs: {}, hijos: [' (', ...x.hijos, ')'] } : x), fuentes, avisos).trim());
          out.push({ tipo: 'flujo', items });
        } else if (c.includes('arena')) {
          // Esquema de arena de practica: cada linea es un nivel del dibujo.
          const items = n.hijos.filter(h => typeof h !== 'string').map(d => enLinea(d.hijos, fuentes, avisos).replace(/─+/g, ' ').trim());
          out.push({ tipo: 'esquema', titulo: n.attrs['aria-label'] || '', items });
        }
        else if (c.includes('summon') || c.includes('stop') || c.includes('stageblock')) out.push(...bloques(n.hijos, avisos));
        else { avisos.push('div no previsto: ' + (n.attrs.class || '')); out.push(...bloques(n.hijos, avisos)); }
        break;
      case 'article': out.push(caja(n, avisos)); break;
      case 'button':
        parrafo(enLinea([n], fuentes, avisos));
        break;
      default:
        avisos.push('bloque no previsto: <' + n.tag + '>');
        parrafo(enLinea(n.hijos, fuentes, avisos));
    }
  }
  return out;
}

function tabla(n, avisos) {
  const filas = [];
  let cabeceras = [];
  const fuentes = [];
  const recorrer = nodo => {
    for (const h of nodo.hijos) {
      if (typeof h === 'string') continue;
      if (h.tag === 'tr') {
        const celdas = h.hijos.filter(x => typeof x !== 'string' && (x.tag === 'td' || x.tag === 'th'));
        const textos = celdas.map(x => enLinea(x.hijos, fuentes, avisos).trim());
        if (celdas.length && celdas.every(x => x.tag === 'th')) cabeceras = textos; else filas.push(textos);
      } else recorrer(h);
    }
  };
  recorrer(n);
  return { tipo: 'tabla', cabeceras, filas, ...(fuentes.length ? { fuentes } : {}) };
}

// <article class="box [tipo]"><h3>titulo</h3> ...contenido... </article>
function caja(n, avisos) {
  const h3 = n.hijos.find(h => typeof h !== 'string' && h.tag === 'h3');
  const resto = n.hijos.filter(h => h !== h3);
  const c = clases(n).filter(x => x !== 'box');
  const fuentes = [];
  const titulo = h3 ? enLinea(h3.hijos, fuentes, avisos).trim() : '';
  return { tipo: 'caja', titulo, estilo: c[0] || '', bloques: bloques(resto, avisos) };
}

function htmlABloques(html, avisos = []) {
  return bloques(parsear(html).hijos, avisos);
}

module.exports = { htmlABloques, parsear, enLinea, fuenteDeEnlace };
