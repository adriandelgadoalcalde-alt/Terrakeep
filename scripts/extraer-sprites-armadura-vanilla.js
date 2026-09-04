// Extrae los sprites REALES de armadura (Head/Legs/Body compuesto) del jugador vanilla,
// para el doll de cuerpo completo de "Inicio" - pedido explicito del usuario (3-sep-2026):
// "los personajes de inicio no se visualizan como realmente son en el juego... que muestre
// el personaje con la vanidad que tiene cada uno pero fiel al guardado".
//
// Mismo criterio ya usado para el cuerpo/pelo base (ver bitacora.md, "Preview de personaje
// de CUERPO COMPLETO") y para los iconos fantasma de slot (extraer-iconos-fantasma-slot.js):
// instalacion vanilla REAL de Steam (Content/Images/Armor_Head_N.xnb, Armor_Legs_N.xnb,
// Armor/Armor_N.xnb - el compuesto torso+brazo), extraida con xnb-to-png.js/lzx-decoder.js
// ya escritos y probados en el proyecto hermano.
//
// Confirmado por muestra real antes de escribir este script (ver bitacora.md): las tres
// hojas usan la MISMA convencion ya establecida para el cuerpo base - Head/Legs son tiras
// verticales de 40 de ancho (frame0 = celda(0,0), sin cambios), Body es una rejilla 9x4 de
// 360x224 (exactamente el mismo tamano que TorsoSkin).
//
// H6-01 (Opus, sexta pasada - "faltan los brazos"): Body YA NO se recorta a la celda (0,0) -
// esa celda es solo el TORSO, el brazo/hombro real vive en otras celdas de la misma hoja
// (ver PlayerPreviewRenderer.cs, LoadArmorCell) - se guarda la hoja 360x224 ENTERA, mismo
// criterio ya aplicado al cuerpo base (extraer-sprites-jugador.js).
//
// Uso: node scripts/extraer-sprites-armadura-vanilla.js
// Salida: TerrasavrNative.App/Assets/player/armor_{head,legs}/{id}.png (40x56, sin cambios) y
//         TerrasavrNative.App/Assets/player/armor_body/{id}.png (360x224, hoja entera)

'use strict';
const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const SLOTS_JSON = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'vanilla_armor_slots.json');
const OUT_ROOT = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'player');

const W = 40, H = 56;

function cropFrame0(xnbPath) {
  const { png, width, height } = xnbToPng(xnbPath);
  // Un puñado de hojas reales son mas pequeñas que el lienzo 40x56 estandar (accesorios de
  // cabeza minimos, algun sprite legado sin piernas/torso completo) - se descartan en vez de
  // rellenar con un recorte a medias que saldria descolocado (misma politica "lo que no se
  // encuentra/no encaja no se inventa" del resto del proyecto).
  if (width < W || height < H) return null;
  const out = new PNG({ width: W, height: H });
  PNG.bitblt(png, out, 0, 0, W, H, 0, 0);
  return out;
}

function fullSheet(xnbPath) {
  const { png } = xnbToPng(xnbPath);
  return png;
}

function extraerGrupo(nombre, ids, xnbPathFn, outDir, modo) {
  fs.mkdirSync(outDir, { recursive: true });
  let ok = 0, faltan = 0, pequenos = 0;
  const faltantes = [];
  for (const id of ids) {
    const xnbPath = xnbPathFn(id);
    if (!fs.existsSync(xnbPath)) { faltan++; faltantes.push(id); continue; }
    const png = modo === 'hoja' ? fullSheet(xnbPath) : cropFrame0(xnbPath);
    if (!png) { pequenos++; continue; }
    fs.writeFileSync(path.join(outDir, id + '.png'), PNG.sync.write(png));
    ok++;
  }
  console.log(`${nombre}: ${ok} extraidos, ${faltan} sin fichero real, ${pequenos} descartados por lienzo mas pequeño que 40x56`);
  if (faltantes.length > 0) console.log(`  faltan ids: ${faltantes.slice(0, 20).join(', ')}${faltantes.length > 20 ? '...' : ''}`);
}

const slots = JSON.parse(fs.readFileSync(SLOTS_JSON, 'utf8'));

const headIds = new Set(), bodyIds = new Set(), legIds = new Set();
for (const entry of Object.values(slots)) {
  if (entry.h !== undefined) headIds.add(entry.h);
  if (entry.b !== undefined) bodyIds.add(entry.b);
  if (entry.l !== undefined) legIds.add(entry.l);
}

console.log(`ids unicos referenciados: head=${headIds.size} body=${bodyIds.size} legs=${legIds.size}`);

extraerGrupo('Head', headIds, (id) => path.join(STEAM_IMAGES, `Armor_Head_${id}.xnb`), path.join(OUT_ROOT, 'armor_head'), 'frame0');
extraerGrupo('Legs', legIds, (id) => path.join(STEAM_IMAGES, `Armor_Legs_${id}.xnb`), path.join(OUT_ROOT, 'armor_legs'), 'frame0');
extraerGrupo('Body', bodyIds, (id) => path.join(STEAM_IMAGES, 'Armor', `Armor_${id}.xnb`), path.join(OUT_ROOT, 'armor_body'), 'hoja');
