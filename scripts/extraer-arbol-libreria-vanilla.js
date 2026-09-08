// Extrae el arbol REAL de carpetas de la Libreria de Terrasavr (Hc.deploy en script.js real,
// clase app.Libraries) - pedido explicito 2-sep-2026: "quiero que calques exactamente la
// estructura de carpetas orden y organizacion de terrasav para esta libreria".
//
// El arbol real NO es "una categoria por objeto" - es un arbol curado a mano con listas de ids
// literales por carpeta hoja (un mismo objeto puede caer en varias carpetas a la vez, ej. una
// espada de hierro en "Materials/Iron & Lead" Y en "Categories/Weapons/Melee damage") mas
// carpetas calculadas por predicado sobre un campo compacto "metatype" por objeto, con
// paginacion automatica real ya integrada (mas de 40 objetos -> "Page N", mas de 480 -> "Pages
// N+" de 10 paginas). Todo esto viene de datos/codigo real embebidos en el propio script.js
// (clases terra.data.TdItem/terra.ItemParser/terra.Item, arrays za.$name/za.pid/za.meta
// separados por ';' en paralelo, y la propia funcion Hc.deploy con sus closures a()/b()/c()).
//
// Estrategia real (no una reimplementacion a ciegas del formato, mismo criterio que
// tmod-extract.js): se extraen los statements/funciones REALES tal cual del script.js real
// (respetando literales de cadena y profundidad de parentesis - un indexOf/regex ingenuo NO
// vale, el propio contenido puede llevar ';'/'"'), se ejecutan en un sandbox de Node (modulo
// "vm") con solo las dependencias externas minimas que Hc.deploy necesita para CONSTRUIR el
// arbol (nunca llama a metodos de sus nodos, solo los crea y enlaza en arrays "nodes") -
// ub/mb (clases Dir/Items reales, ver app.ShDir/app.ShItems), A.list (reconstruido con la
// misma logica real y simple de terra.ItemParser.run/loadMeta), y.cca/y.indexOf (HxOverrides
// real), D.endsWith/D.replace (helpers de string real). Verificado exacto contra datos
// independientes ya conocidos: el id=4 (Iron Broadsword) decodifica d=12|t=20|k=5.5, IGUAL que
// vanilla_stats.json (extraido de forma totalmente independiente del Item.cs decompilado en
// la ronda anterior) - y el arbol resultante cubre los 6146 ids reales sin ninguno huerfano.
//
// AÑADIDO 8-sep-2026 (pedido del usuario con captura, ver la cabecera de
// scripts/extraer-subtipos-libreria-vanilla.py): despues de reconstruir el arbol real tal cual,
// se REORGANIZA la rama raiz "Categories" - y solo esa. Ahi el Terrasavr original parte cada
// carpeta hoja en "Page 1", "Page 2"... de 40 en 40 POR ORDEN DE ID, y como el icono de cada
// pagina es el primer objeto de su lista, la pagina aparenta representar un tipo concreto
// (un arco, un pico) cuando dentro hay de todo. Aqui cada una de esas carpetas pasa a estar
// partida por el SUBTIPO REAL del objeto (espadas, lanzas, arcos, armas de fuego, picos,
// bloques, estandartes, cuadros...), derivado del codigo decompilado real del juego por
// scripts/extraer-subtipos-libreria-vanilla.py. El resto del arbol (Materials, Decorative,
// Items by ID...) se queda exactamente como estaba.
//
// Uso: node scripts/extraer-arbol-libreria-vanilla.js
//      (necesita vanilla_library_subtypes.json ya generado; si no esta, aborta)
// Salida: Terrakeep.App/Assets/vanilla_library_tree.json

const fs = require("fs");
const path = require("path");
const vm = require("vm");

const scriptPath = path.resolve(__dirname, "..", "..", "Terrasavr-Calamity-Beta", "resources", "app", "local-site", "script.js");
const assetsDir = path.resolve(__dirname, "..", "Terrakeep.App", "Assets");
const outPath = path.join(assetsDir, "vanilla_library_tree.json");
const subtypesPath = path.join(assetsDir, "vanilla_library_subtypes.json");

const content = fs.readFileSync(scriptPath, "utf8");

// Extrae un statement completo "marker...;" (o una funcion "marker...};") respetando
// literales de cadena y profundidad de parentesis/corchetes/llaves.
function extractStatement(src, marker) {
  const start = src.indexOf(marker);
  if (start === -1) throw new Error("marker no encontrado en script.js real: " + marker);
  let i = start, depth = 0;
  while (i < src.length) {
    const ch = src[i];
    if (ch === '"' || ch === "'") {
      const quote = ch; i++;
      while (i < src.length && src[i] !== quote) { if (src[i] === "\\") i++; i++; }
      i++; continue;
    }
    if (ch === "(" || ch === "[" || ch === "{") { depth++; i++; continue; }
    if (ch === ")" || ch === "]" || ch === "}") { depth--; i++; continue; }
    if (ch === ";" && depth === 0) return src.substring(start, i + 1);
    i++;
  }
  throw new Error("statement sin terminar para " + marker);
}

// --- Paso 1: datos crudos reales de items (za.$name/za.pid/za.meta, arrays paralelos
// separados por ';') - se EJECUTAN tal cual, no se reimplementa el formato a mano. ---
const zaCode = ["za.minId=", "za.maxId=", "za.count=", "za.$name=", "za.pid=", "za.meta="]
  .map(extractStatement.bind(null, content)).join("");
const zaSandbox = { za: {} };
vm.createContext(zaSandbox);
vm.runInContext(zaCode, zaSandbox);
const za = zaSandbox.za;
if (za.$name.length !== za.count || za.pid.length !== za.count || za.meta.length !== za.count) {
  throw new Error(`za.* desalineado: count=${za.count} pero $name=${za.$name.length} pid=${za.pid.length} meta=${za.meta.length}`);
}

// --- Paso 2: reconstruir A.list real (mismo bucle que terra.ItemParser.run/Item.loadMeta
// reales: pid con fallback = nombre sin espacios, metatype = meta hasta el primer '|', resto
// = pares clave=valor separados por '|', metadata = la cadena de meta completa tal cual la
// usan algunos predicados reales del propio arbol, ej. Picos/Hachas/Martillos). ---
function parseMeta(meta) {
  const bar = meta.indexOf("|");
  const metatype = bar >= 0 ? meta.substring(0, bar) : meta;
  const rest = bar >= 0 ? meta.substring(bar + 1) : "";
  const stats = {};
  for (const pair of rest ? rest.split("|") : []) {
    const eq = pair.indexOf("=");
    if (eq >= 0) stats[pair.substring(0, eq)] = pair.substring(eq + 1);
  }
  return { metatype, stats };
}

// Bug real encontrado y corregido 2-sep-2026 (reportado por el usuario: "Categories >
// Equipable > Wings" salia con 0 objetos): dos predicados reales del propio Hc.deploy
// (Wings, Mounts*) no miran metatype/metadata, miran "a.textLq" - el texto de tooltip ya
// formateado en minusculas, buscando frases literales en ingles ("allows flight",
// "rideable", "summons"). La clave real "1" dentro de meta (verificado a mano contra la
// cadena real: id 823 = Fledgling Wings -> "...|1=Allows flight|...", id del reno ->
// "...|1=Summons a rideable reindeer|...") es exactamente ese texto literal - no hace falta
// reimplementar el formateador Xa.parse/wa.pairDefs entero, con la clave "1" (cuando existe)
// basta para los dos unicos predicados reales que usan textLq (confirmado contando
// ".textLq" en el propio Hc.deploy: exactamente 2 apariciones, Wings y Mounts*).
function buildTextLq(stats) {
  return (stats["1"] ?? "").toLowerCase();
}

const itemList = [];
for (let c = 0, id = za.minId; c < za.count; c++, id++) {
  let name = za.$name[c];
  if (id !== 0 && name === "") name = "Unknown";
  let pid = za.pid[c];
  if (pid === "") pid = name.split(" ").join("");
  const { metatype, stats } = parseMeta(za.meta[c]);
  itemList.push({ id, pid, name, nameLq: name.toLowerCase(), metatype, metadata: za.meta[c], stats, textLq: buildTextLq(stats) });
}

// --- Paso 3: ejecutar el propio Hc.deploy real (extraido tal cual) con sus closures
// a()/b()/c() intactos, sustituyendo solo las dependencias externas minimas por
// equivalentes reales verificados (ver cabecera). No hace falta booteat el motor Haxe entero:
// Hc.deploy solo CONSTRUYE nodos y los enlaza, nunca llama a sus metodos. ---
const deployCode = extractStatement(content, "Hc.deploy=function()");

function Dir(icon, name, nodes) { this.icon = icon; this.type = 1; this.name = name; this.nodes = nodes; }
function Items(icon, name, nodes) { this.icon = icon; this.type = 2; this.name = name; this.nodes = nodes; }

const hcSandbox = {
  ub: Dir,
  mb: Items,
  A: { list: itemList, ITEMS: za.maxId + 1 },
  I: { ITEMS: za.maxId + 1 },
  y: {
    cca: (s, i) => { const c = s.charCodeAt(i); return c === c ? c : undefined; },
    indexOf: (arr, item, from) => arr.indexOf(item, from < 0 ? Math.max(arr.length + from, 0) : from),
  },
  z: Error,
  D: {
    endsWith: (a, b) => a.endsWith(b),
    replace: (a, b, c) => a.split(b).join(c),
  },
  Hc: {},
  window: {}, // sin __calamityBuildLibraryNode a proposito: Calamity se añade aparte, en C#
  console,
};
vm.createContext(hcSandbox);
vm.runInContext(deployCode, hcSandbox);
const tree = hcSandbox.Hc.deploy();

// --- Paso 4: reorganizar la rama "Categories" por SUBTIPO REAL (ver la cabecera). ---
if (!fs.existsSync(subtypesPath)) {
  throw new Error(`Falta ${subtypesPath}. Ejecuta antes: python scripts/extraer-subtipos-libreria-vanilla.py`);
}
const subtipos = JSON.parse(fs.readFileSync(subtypesPath, "utf8"));

// Que carpeta de "Categories" se reparte con que dimension del catalogo de subtipos:
//   w = subtipo de arma/municion, t = herramienta, s = ranura de equipo,
//   k = clase de equipo (armadura/vanidad/accesorio), a = ranura visual del accesorio,
//   d = tinte, p = colocable.
// Las que NO estan aqui es por un motivo real, no por olvido:
//   - "Magic damage": no hay ningun campo ni set real que separe los tipos de arma magica
//     (baculos, libros, pistolas magicas...) - se deja paginada antes que inventar un criterio.
//   - "Walls": las 292 son paredes y solo paredes; la pagina ya enseña una pared como icono,
//     asi que no hay ninguna falsa impresion que arreglar.
//   - "Summon damage", "Summoner whips", "Sentry damage", "Wings", "Fishing poles": son hojas de
//     menos de 40 objetos, ni siquiera llegan a paginarse.
const REORGANIZAR = [
  { ruta: ["Categories", "Weapons", "Melee damage"], dim: "w" },
  { ruta: ["Categories", "Weapons", "Ranged damage"], dim: "w" },
  { ruta: ["Categories", "Equipable", "Armor"], dim: "s" },
  { ruta: ["Categories", "Equipable", "Accessories"], dim: "a" },
  { ruta: ["Categories", "Equipable", "Vanity"], dim: "s" },
  { ruta: ["Categories", "Equipable", "Head slot"], dim: "k" },
  { ruta: ["Categories", "Equipable", "Body slot"], dim: "k" },
  { ruta: ["Categories", "Equipable", "Leg slot"], dim: "k" },
  { ruta: ["Categories", "Equipable", "Dyes"], dim: "d" },
  { ruta: ["Categories", "Tools", "Pickaxes"], dim: "t" },
  { ruta: ["Categories", "Tools", "Axes"], dim: "t" },
  { ruta: ["Categories", "Tools", "Hammers"], dim: "t" },
  { ruta: ["Categories", "Placeable"], dim: "p" },
];

// Donde cae lo que no tiene subtipo real en esa dimension. Nunca se fuerza dentro de un grupo
// con nombre concreto: o va al "Otros" que ya existe en esa dimension, o a uno generico.
const RESIDUOS = { p: "Other placeables", a: "Other accessories", s: "Other equipment", k: "Other equipment" };
const RESIDUO = "Other items";
const PAGINA = 40;             // el mismo tope real de Terrasavr (b()/c() de Hc.deploy)

// Nombre de una carpeta ignorando el "(N)" del recuento: "Melee damage (316)" -> "Melee damage".
function nombreBase(nombre) {
  const m = /^(.*?)\s*\(\d+\)$/.exec(nombre);
  return m ? m[1] : nombre;
}

function buscarNodo(nodos, ruta) {
  let actuales = nodos, nodo = null, padre = null, indice = -1;
  for (const paso of ruta) {
    indice = actuales.findIndex(n => nombreBase(n.name) === paso);
    if (indice === -1) return null;
    padre = actuales;
    nodo = actuales[indice];
    actuales = nodo.type === 1 ? nodo.nodes : [];
  }
  return { nodo, padre, indice };
}

// Todos los ids de una carpeta, en su orden real, sin repetir (una hoja son ids; una carpeta,
// hijos). Los 0 son huecos de la rejilla curada, no objetos.
function idsDe(nodo) {
  if (nodo.type === 2) return nodo.nodes.filter(id => id !== 0);
  return nodo.nodes.flatMap(idsDe);
}

// Una hoja de <=40 objetos; si hay mas, la misma paginacion real de Terrasavr por dentro
// ("Page N", y "Pages N+" de diez en diez a partir de 480), pero ya nunca mezclando tipos.
function carpetaDeGrupo(nombre, ids) {
  if (ids.length <= PAGINA) return { icon: ids[0], type: 2, name: nombre, nodes: ids };
  const paginas = [];
  for (let i = 0; i < ids.length; i += PAGINA) {
    const trozo = ids.slice(i, i + PAGINA);
    paginas.push({ icon: trozo[0], type: 2, name: `Page ${i / PAGINA + 1}`, nodes: trozo });
  }
  if (paginas.length <= 12) return { icon: ids[0], type: 1, name: nombre, nodes: paginas };
  const bloques = [];
  for (let i = 0; i < paginas.length; i += 10) {
    const trozo = paginas.slice(i, i + 10);
    bloques.push({ icon: trozo[0].icon, type: 1, name: `Pages ${i + 1}+`, nodes: trozo });
  }
  return { icon: ids[0], type: 1, name: nombre, nodes: bloques };
}

const informe = [];
for (const { ruta, dim } of REORGANIZAR) {
  const encontrado = buscarNodo(tree.nodes, ruta);
  if (!encontrado) throw new Error(`No existe la carpeta real "${ruta.join(" > ")}" en el arbol.`);
  const { nodo, padre, indice } = encontrado;

  const ids = idsDe(nodo);
  const grupos = new Map();
  for (const id of ids) {
    const clave = (subtipos.items[String(id)] || {})[dim] || RESIDUOS[dim] || RESIDUO;
    if (!grupos.has(clave)) grupos.set(clave, []);
    grupos.get(clave).push(id);
  }
  if (grupos.size <= 1) {
    informe.push(`  ${ruta.join(" > ")}: un solo subtipo real, se deja como estaba`);
    continue;
  }

  const orden = subtipos.orders[dim] || [];
  const claves = [...grupos.keys()].sort((a, b) => {
    const ia = orden.indexOf(a), ib = orden.indexOf(b);
    return (ia === -1 ? orden.length : ia) - (ib === -1 ? orden.length : ib) || a.localeCompare(b);
  });

  const hijos = claves.map(clave => {
    const etiqueta = (subtipos.labels[clave] || {}).en || clave;
    return carpetaDeGrupo(`${etiqueta} (${grupos.get(clave).length})`, grupos.get(clave));
  });
  const nuevo = { icon: nodo.icon, type: 1, name: nodo.name, nodes: hijos };
  padre[indice] = nuevo;

  const total = hijos.reduce((n, h) => n + idsDe(h).length, 0);
  if (total !== ids.length) throw new Error(`${ruta.join(" > ")}: ${ids.length} objetos antes y ${total} despues.`);
  informe.push(`  ${ruta.join(" > ")}: ${ids.length} objetos -> ${hijos.length} subcarpetas ` +
    `(${claves.map(c => `${(subtipos.labels[c] || {}).en || c} ${grupos.get(c).length}`).join(", ")})`);
}
console.log("Rama \"Categories\" reorganizada por subtipo real:");
informe.forEach(l => console.log(l));

// --- Verificacion antes de escribir: cobertura completa (todo id real debe aparecer al
// menos una vez, gracias a la carpeta "Items by ID") y spot-check de posiciones conocidas. ---
function findPaths(node, targetId, trail) {
  const here = trail.concat([node.name]);
  if (node.type === 2) return node.nodes.includes(targetId) ? [here.join(" > ")] : [];
  return node.nodes.flatMap(child => findPaths(child, targetId, here));
}
const uniqueIds = new Set();
(function walk(node) {
  if (node.type === 2) node.nodes.forEach(i => uniqueIds.add(i));
  else node.nodes.forEach(walk);
})(tree);

console.log(`Arbol real: ${tree.nodes.length} carpetas raiz, ${uniqueIds.size}/${za.count} ids reales cubiertos.`);
if (uniqueIds.size !== za.count) throw new Error(`Cobertura incompleta: faltan ${za.count - uniqueIds.size} ids reales en el arbol.`);

for (const [id, label] of [[4, "Iron Broadsword"], [368, "Excalibur"], [3389, "Terrarian"]]) {
  const paths = findPaths(tree, id, []);
  if (paths.length === 0) throw new Error(`Spot-check fallido: id=${id} (${label}) no aparece en ninguna carpeta real.`);
  console.log(`  id=${id} (${label}): ${paths.join(" | ")}`);
}

// Pedido explicito 2-sep-2026 tras encontrar "Wings" con 0 objetos: ninguna carpeta hoja real
// debe quedar vacia - si alguna sale con 0 es una señal real de que falta algo en la
// reconstruccion (como paso con textLq), no algo a ignorar en silencio.
const emptyLeaves = [];
(function findEmptyLeaves(node, trail) {
  const here = trail.concat([node.name]);
  if (node.type === 2) { if (node.nodes.length === 0) emptyLeaves.push(here.join(" > ")); return; }
  node.nodes.forEach(child => findEmptyLeaves(child, here));
})(tree, []);
if (emptyLeaves.length > 0) {
  throw new Error(`${emptyLeaves.length} carpeta(s) hoja real(es) sin ningun objeto:\n  ${emptyLeaves.join("\n  ")}`);
}
console.log("Ninguna carpeta hoja real quedo vacia.");

fs.writeFileSync(outPath, JSON.stringify(tree.nodes)); // solo los hijos reales, no el wrapper "" raiz
console.log(`Guardado: ${outPath}`);
