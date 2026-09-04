// H6-01/H6-03/H6-04 (Opus, sexta pasada - "los personajes de Inicio no reflejan fielmente la
// apariencia real, y les faltan los brazos"). Reescribe DE RAIZ la extraccion de sprites del
// jugador: la version anterior recortaba SIEMPRE la celda (0,0) de 40x56 de cada hoja - correcto
// para las 7 piezas que son una TIRA VERTICAL (Head/EyeWhites/Eyes/LegSkin/Pants/Shoes + pelo),
// pero cero-relleno para las 8 piezas que son una REJILLA COMPUESTA 9x4 de 360x224
// (TorsoSkin/Undershirt/Hands/Shirt/ArmSkin/ArmUndershirt/ArmHand/ArmShirt) - el brazo/mano/
// camiseta interior real NO viven en la celda (0,0) de esas hojas, viven en las celdas que la
// propia PlayerDrawSet.cs real usa para el frame de reposo (confirmado leyendo
// Terraria/DataStructures/PlayerDrawSet.cs + PlayerDrawLayers.cs decompilados reales -
// CreateCompositeFrameRect, UpdateCompositeArm, DrawPlayer_12_Skin_Composite/
// SkinComposite_BackArmShirt/17_TorsoComposite/28_ArmOverItemComposite).
//
// Las piezas compuestas ya NO se recortan aqui - se guarda la hoja 360x224 ENTERA (PNG
// indexado, unos pocos KB cada una) y es PlayerPreviewRenderer quien recorta la celda real en
// tiempo de render (LoadBodyCell) - exactamente el mismo criterio que ya se va a aplicar a la
// armadura compuesta (extraer-sprites-armadura-vanilla.js).
//
// H6-03: ademas de la variante de piel "0" (StarterMale), se extraen las otras 3 variantes de
// CUERPO real que tiene la instalacion (1 MaleSticker, 2 MaleGangster, 3 MaleCoat - ropa base
// distinta) y la "4" (FemaleStarter, torso/hombros en fila 2 en vez de 0, confirmado con
// PlayerDrawLayers.cs: "if (!drawPlayer.Male) pt.Y += 2"). Las variantes 5/6/7/9 (Female
// Sticker/Gangster/Coat/Dress) y 8 (MaleDress) heredan la mayoria de piezas de la 0/4 segun
// Terraria.Initializers.PlayerDataInitializer.cs real - por simplicidad y porque no cambian la
// silueta del doll a este tamaño, esta pasada solo cubre 0/1/2/3/4 (bastan para IsMale real +
// las 3 variantes de ropa base mas comunes); el resto cae a la 0/4 mas cercana en
// PlayerPreviewRenderer (documentado ahi, no aqui).
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
// Salida: TerrasavrNative.App/Assets/player/{variant}/{pieceId}.png (compuestas, 360x224) y
//         TerrasavrNative.App/Assets/player/{variant}/{pieceId}.png (tiras, 40x56, sin cambios
//         de formato) + TerrasavrNative.App/Assets/player/hair/{0..227}.png

'use strict';
const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const OUT_ROOT = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'player');

const W = 40, H = 56;

// Y real de cada pieza (PlayerTextureID.cs), y si es tira vertical (frame0, 40x56) o rejilla
// compuesta (hoja entera, 360x224).
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
  { y: 10, name: 'legskin', composite: false },
  { y: 11, name: 'pants', composite: false },
  { y: 12, name: 'shoes', composite: false },
  { y: 13, name: 'armshirt', composite: true },
];

// H6-03: variantes de CUERPO reales - 0 (MaleStarter) y 4 (FemaleStarter), las 2 UNICAS que
// tienen SU PROPIA hoja para las 13 piezas (verificado con el listado real de la instalacion:
// Player_1/2/3_{0,1,2}.xnb NO EXISTEN - las variantes 1/2/3/5-11, "ropa base" alternativa,
// SOLO reemplazan un subconjunto de piezas y heredan el resto de 0/4 segun
// Terraria.Initializers.PlayerDataInitializer.cs real - no se cubren en esta pasada, alcance
// deliberado: el doll de Terrakeep distingue Chico/Chica, no las variantes de vestuario base
// del creador de personajes, que no cambian la silueta de forma perceptible a este tamaño).
// head/eyewhites/eyes son iguales en TODAS las variantes (confirmado: Player_4_{0,1,2}.xnb NO
// EXISTEN en la instalacion real) - se cargan siempre de la variante 0, ver
// PlayerPreviewRenderer.
const VARIANTS = [0, 4];

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

// head/eyewhites/eyes son compartidas por TODAS las variantes (solo existe Player_0_{0,1,2}.xnb
// en la instalacion real) - se extraen una unica vez, de la variante 0.
const SHARED_PIECES = new Set(['head', 'eyewhites', 'eyes']);

let ok = 0, faltan = 0;
for (const variant of VARIANTS) {
  const outDir = path.join(OUT_ROOT, 'body' + variant);
  fs.mkdirSync(outDir, { recursive: true });
  for (const piece of PIECES) {
    if (variant !== 0 && SHARED_PIECES.has(piece.name)) continue; // ya se extrajeron con la variante 0
    const xnbPath = path.join(STEAM_IMAGES, `Player_${variant}_${piece.y}.xnb`);
    if (!fs.existsSync(xnbPath)) { faltan++; console.log(`  falta: Player_${variant}_${piece.y}.xnb (${piece.name})`); continue; }
    const png = piece.composite ? fullSheet(xnbPath) : cropFrame0(xnbPath);
    if (!png) { faltan++; console.log(`  hoja mas pequeña que el lienzo esperado: Player_${variant}_${piece.y}.xnb`); continue; }
    fs.writeFileSync(path.join(outDir, piece.name + '.png'), PNG.sync.write(png));
    ok++;
  }
}
console.log(`Cuerpo: ${ok} piezas extraidas (${VARIANTS.length} variantes x ${PIECES.length} piezas), ${faltan} ausentes/pequeñas`);

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
