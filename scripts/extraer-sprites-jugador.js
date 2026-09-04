// H6-01/H6-03/H6-04 (Opus, sexta pasada - "los personajes de Inicio no reflejan fielmente la
// apariencia real, y les faltan los brazos"). Reescribe DE RAIZ la extraccion de sprites del
// jugador: la version anterior recortaba SIEMPRE la celda (0,0) de 40x56 de cada hoja - correcto
// para las tiras VERTICALES (Head/EyeWhites/Eyes/LegSkin/Pants/Shoes + pelo), pero cero-relleno
// para las piezas que son una REJILLA COMPUESTA 9x4 de 360x224 (TorsoSkin/Undershirt/Hands/
// Shirt/ArmSkin/ArmUndershirt/ArmHand/ArmShirt) - el brazo/mano/camiseta interior real NO viven
// en la celda (0,0) de esas hojas, viven en las celdas que la propia PlayerDrawSet.cs real usa
// para el frame de reposo (confirmado leyendo Terraria/DataStructures/PlayerDrawSet.cs +
// PlayerDrawLayers.cs decompilados reales - CreateCompositeFrameRect, UpdateCompositeArm,
// DrawPlayer_12_Skin_Composite/SkinComposite_BackArmShirt/17_TorsoComposite/
// 28_ArmOverItemComposite).
//
// Las piezas compuestas ya NO se recortan aqui - se guarda la hoja 360x224 ENTERA (PNG
// indexado, unos pocos KB cada una) y es PlayerPreviewRenderer quien recorta la celda real en
// tiempo de render (LoadBodyCell) - exactamente el mismo criterio que ya usa la armadura
// compuesta (extraer-sprites-armadura-vanilla.js).
//
// H6-01-b (advisor Opus, "la vanidad no se dibuja bien en el cuerpo delgado" - ver
// ESPEC-dibujado-sprites.md seccion 4.1): la version anterior de este script SOLO cubria las
// variantes 0 (MaleStarter) y 4 (FemaleStarter), documentando (por error, confirmado ahora
// contra Terraria.Initializers.PlayerDataInitializer.cs decompilado real) que las otras 10
// variantes "no cambian la silueta a este tamaño". Es falso: las variantes de vestido/abrigo
// (3 MaleCoat/7 FemaleCoat/8 MaleDress/9 FemaleDress) SI cambian la ropa base entera, y las
// 10 variantes SI tienen sus propias hojas reales en disco para un subconjunto real de piezas
// (comprobado listando Content\Images\ real, tabla exacta en ESPEC-dibujado-sprites.md#4.1):
//
//   Variante 0 (MaleStarter):    propias TODAS (0..13) - la base real de todo lo masculino.
//   Variante 4 (FemaleStarter):  propias 3..13 (torso..armshirt) - 0/1/2 vienen SIEMPRE de la 0.
//   Variantes 1/2/3/8 (varon):   propias 4,6,8,11,12,13 (+14 en la 3 y la 8) - heredan el resto de la 0.
//   Variantes 5/6/7/9 (mujer):   propias 4,6,8,11,12,13 (+14 en la 7) - heredan el resto de la 4.
//
// Aqui se resuelve la herencia EN LA EXTRACCION (se copia la pieza heredada al PNG final de
// cada variante) para que PlayerPreviewRenderer no tenga que saber nada de herencia en tiempo
// de render - cada carpeta bodyN/ queda con el juego COMPLETO real de piezas listo para usar.
//
// H6-04: el pelo real es 0-based en el juego (Player.hair, 0..227) pero el .xnb en disco es
// Player_Hair_{hair+1}.xnb (confirmado: AssetInitializer.cs "Images/Player_Hair_" + (num4+1),
// UICharacterCreation.cs "switch (player.hair + 1)"). Se extrae hair/{id}.png para id=0..227
// desde Player_Hair_{id+1}.xnb - PlayerPreviewRenderer ya no necesita compensar ningun
// desfase, HairStyle del .plr se usa tal cual como indice de fichero.
//
// Uso: node scripts/extraer-sprites-jugador.js
// (necesita NODE_PATH=...Terrasavr-Calamity-Beta\resources\app\node_modules para pngjs, igual
// que extraer-sprites-armadura-vanilla.js)
// Salida: TerrasavrNative.App/Assets/player/body{0..9}/{pieza}.png (compuestas 360x224 o tiras
//         40x56 segun la pieza) + TerrasavrNative.App/Assets/player/hair/{0..227}.png

'use strict';
const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const OUT_ROOT = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'player');

const W = 40, H = 56;
const SheetWidth = 360, SheetHeight = 224;

// Y real de cada pieza (PlayerTextureID.cs, 16 constantes reales) y si es tira vertical
// (frame0, 40x56) o rejilla compuesta (hoja entera, 360x224). "extra" (14) es el faldon del
// vestido/abrigo (DrawPlayer_15_SkinLongCoat real) - solo lo tienen las variantes 3/7/8.
const PIECES = [
  { y: 0, name: 'head', composite: false },
  { y: 1, name: 'eyewhites', composite: false },
  { y: 2, name: 'eyes', composite: false },
  { y: 3, name: 'torsoskin', composite: true },
  { y: 4, name: 'undershirt', composite: true },
  { y: 5, name: 'hands', composite: true },
  { y: 6, name: 'shirt', composite: true },
  { y: 7, name: 'armskin', composite: true },
  { y: 8, name: 'armundershirt', composite: true },
  { y: 9, name: 'armhand', composite: true },
  { y: 10, name: 'legskin', composite: false },
  { y: 11, name: 'pants', composite: false },
  { y: 12, name: 'shoes', composite: false },
  { y: 13, name: 'armshirt', composite: true },
  { y: 14, name: 'extra', composite: false },
];
const PIECE_BY_NAME = Object.fromEntries(PIECES.map(p => [p.name, p]));

// head/eyewhites/eyes son compartidas por TODAS las variantes (solo existe Player_0_{0,1,2}.xnb
// en la instalacion real) - se extraen una unica vez, de la variante 0, y el renderer las carga
// siempre de body0 (sin cambios en esta pasada).
const SHARED_PIECES = new Set(['head', 'eyewhites', 'eyes']);

// Tabla real de herencia (ESPEC-dibujado-sprites.md#4.1, PlayerDataInitializer.cs real): que
// piezas tiene CADA variante en su propia hoja .xnb real, y de que variante hereda el resto
// (nunca aplica a head/eyewhites/eyes, siempre compartidas via SHARED_PIECES de arriba).
const VARIANT_OWN_PIECES = {
  0: ['torsoskin', 'undershirt', 'hands', 'shirt', 'armskin', 'armundershirt', 'armhand', 'legskin', 'pants', 'shoes', 'armshirt'],
  4: ['torsoskin', 'undershirt', 'hands', 'shirt', 'armskin', 'armundershirt', 'armhand', 'legskin', 'pants', 'shoes', 'armshirt'],
  1: ['undershirt', 'shirt', 'armundershirt', 'pants', 'shoes', 'armshirt'],
  2: ['undershirt', 'shirt', 'armundershirt', 'pants', 'shoes', 'armshirt'],
  3: ['undershirt', 'shirt', 'armundershirt', 'pants', 'shoes', 'armshirt', 'extra'],
  5: ['undershirt', 'shirt', 'armundershirt', 'pants', 'shoes', 'armshirt'],
  6: ['undershirt', 'shirt', 'armundershirt', 'pants', 'shoes', 'armshirt'],
  7: ['undershirt', 'shirt', 'armundershirt', 'pants', 'shoes', 'armshirt', 'extra'],
  8: ['undershirt', 'shirt', 'armundershirt', 'pants', 'shoes', 'armshirt', 'extra'],
  9: ['undershirt', 'shirt', 'armundershirt', 'pants', 'shoes', 'armshirt'],
};
const VARIANT_FALLBACK = { 1: 0, 2: 0, 3: 0, 8: 0, 5: 4, 6: 4, 7: 4, 9: 4 };
const VARIANTS = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
// Todas las piezas reales que necesita el renderer, salvo head/eyewhites/eyes (siempre de body0).
const ALL_BODY_PIECES = PIECES.filter(p => !SHARED_PIECES.has(p.name)).map(p => p.name);

function cropFrame0(xnbPath) {
  const { png, width, height } = xnbToPng(xnbPath);
  if (width < W || height < H) return null;
  const out = new PNG({ width: W, height: H });
  PNG.bitblt(png, out, 0, 0, W, H, 0, 0);
  return out;
}

function fullSheet(xnbPath) {
  const { png } = xnbToPng(xnbPath);
  return png; // PNG.js ya expone .width/.height reales - se escribe tal cual
}

let ok = 0, heredadas = 0, faltan = 0;
for (const variant of VARIANTS) {
  const outDir = path.join(OUT_ROOT, 'body' + variant);
  fs.mkdirSync(outDir, { recursive: true });

  // head/eyewhites/eyes: solo la variante 0 las extrae de verdad.
  if (variant === 0) {
    for (const name of SHARED_PIECES) {
      const piece = PIECE_BY_NAME[name];
      const xnbPath = path.join(STEAM_IMAGES, `Player_0_${piece.y}.xnb`);
      if (!fs.existsSync(xnbPath)) { faltan++; console.log(`  falta: Player_0_${piece.y}.xnb (${name})`); continue; }
      const png = cropFrame0(xnbPath);
      if (!png) { faltan++; continue; }
      fs.writeFileSync(path.join(outDir, name + '.png'), PNG.sync.write(png));
      ok++;
    }
  }

  const propias = new Set(VARIANT_OWN_PIECES[variant] || []);
  const fallback = VARIANT_FALLBACK[variant]; // undefined para 0/4 (no heredan nada)
  for (const name of ALL_BODY_PIECES) {
    if (name === 'extra' && !propias.has('extra')) continue; // solo 3/7/8 tienen faldon real
    const piece = PIECE_BY_NAME[name];
    if (propias.has(name)) {
      const xnbPath = path.join(STEAM_IMAGES, `Player_${variant}_${piece.y}.xnb`);
      if (!fs.existsSync(xnbPath)) { faltan++; console.log(`  falta: Player_${variant}_${piece.y}.xnb (${name})`); continue; }
      const png = piece.composite ? fullSheet(xnbPath) : cropFrame0(xnbPath);
      // Hallazgo real (comprobado a mano con xnb-to-png.js, no documentado en
      // PlayerDataInitializer.cs ni en la comprobacion previa de "que .xnb existen"):
      // Player_8_8.xnb (ArmUndershirt de MaleDress) es una tira 40x1120, NO la rejilla
      // 360x224 que tienen TODAS las demas variantes para esa misma pieza (Player_0/1/4_8.xnb
      // si son 360x224) - un dato real inconsistente con el resto del set, no un error de
      // extraccion. Si la hoja "propia" no tiene la forma de rejilla esperada, se descarta y
      // se hereda de la base (0/4) en su lugar - mismo criterio "lo que no encaja no se usa a
      // ciegas" que ya aplica extraer-sprites-armadura-vanilla.js a las hojas mas pequeñas.
      const formaValida = png && (piece.composite ? (png.width === SheetWidth && png.height === SheetHeight) : true);
      if (!formaValida) {
        console.log(`  Player_${variant}_${piece.y}.xnb (${name}) no tiene la forma esperada (${png ? png.width + 'x' + png.height : 'hoja mas pequeña'}) - se hereda de body${fallback} en su lugar`);
      } else {
        fs.writeFileSync(path.join(outDir, name + '.png'), PNG.sync.write(png));
        ok++;
        continue;
      }
      if (fallback === undefined) { faltan++; continue; }
      const origenAlt = path.join(OUT_ROOT, 'body' + fallback, name + '.png');
      if (!fs.existsSync(origenAlt)) { faltan++; console.log(`  falta el origen a heredar: body${fallback}/${name}.png (para body${variant})`); continue; }
      fs.copyFileSync(origenAlt, path.join(outDir, name + '.png'));
      heredadas++;
    } else {
      // Heredada de verdad de la variante base (0 varon / 4 mujer) - copia real de fichero,
      // no un enlace ni una referencia: cada carpeta bodyN/ queda autocontenida.
      const origen = path.join(OUT_ROOT, 'body' + fallback, name + '.png');
      if (!fs.existsSync(origen)) { faltan++; console.log(`  falta el origen a heredar: body${fallback}/${name}.png (para body${variant})`); continue; }
      fs.copyFileSync(origen, path.join(outDir, name + '.png'));
      heredadas++;
    }
  }
}
console.log(`Cuerpo: ${ok} piezas propias extraidas, ${heredadas} heredadas de la variante base (0/4), ${faltan} ausentes/pequeñas (${VARIANTS.length} variantes)`);

// Pelo: 228 estilos reales, 0-based (H6-04) - fichero {id}.png viene de Player_Hair_{id+1}.xnb.
const hairDir = path.join(OUT_ROOT, 'hair');
fs.mkdirSync(hairDir, { recursive: true });
let hairOk = 0, hairFaltan = 0;
for (let id = 0; id < 228; id++) {
  const xnbPath = path.join(STEAM_IMAGES, `Player_Hair_${id + 1}.xnb`);
  if (!fs.existsSync(xnbPath)) { hairFaltan++; continue; }
  const png = cropFrame0(xnbPath);
  if (!png) { hairFaltan++; continue; }
  fs.writeFileSync(path.join(hairDir, id + '.png'), PNG.sync.write(png));
  hairOk++;
}
console.log(`Pelo: ${hairOk} estilos extraidos (id 0-based, Player_Hair_{id+1}.xnb), ${hairFaltan} ausentes`);

// H6-07 (sexta auditoria de Opus): "pelo bajo el casco" - Player_HairAlt_{id+1}.xnb real
// (AssetInitializer.cs: "Images/Player_HairAlt_" + (id+1)), el sprite que el juego real
// dibuja en vez del normal cuando el casco puesto esta en la lista real "hatHair" (ver
// TerrasavrNative.Core/Model/HairDrawProfile.cs, portado de Player.GetHairSettings real).
// Mismo esquema 0-based +1 en el nombre de fichero que el pelo normal.
const hairAltDir = path.join(OUT_ROOT, 'hairalt');
fs.mkdirSync(hairAltDir, { recursive: true });
let hairAltOk = 0, hairAltFaltan = 0;
for (let id = 0; id < 228; id++) {
  const xnbPath = path.join(STEAM_IMAGES, `Player_HairAlt_${id + 1}.xnb`);
  if (!fs.existsSync(xnbPath)) { hairAltFaltan++; continue; }
  const png = cropFrame0(xnbPath);
  if (!png) { hairAltFaltan++; continue; }
  fs.writeFileSync(path.join(hairAltDir, id + '.png'), PNG.sync.write(png));
  hairAltOk++;
}
console.log(`Pelo (hatHair/casco): ${hairAltOk} estilos extraidos (id 0-based, Player_HairAlt_{id+1}.xnb), ${hairAltFaltan} ausentes`);
