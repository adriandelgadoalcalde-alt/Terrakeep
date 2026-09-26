// GapAnalysis Encargo J (25-sep-2026) + ParidadPersonaje Fase5 (26-sep-2026): extrae los 3
// sprites reales de "TextureAssets.Extra[N]" que hacen falta para los estados especiales de bajo
// impacto de Player.cs (ver bitacora.md) - hermano puntual de extraer-sprites-armadura-vanilla.js/
// extraer-sprites-accesorios-vanilla.js, pero para la carpeta Images/Extra_N.xnb (sin agrupar por
// tipo de accesorio real, solo ids sueltos que no encajan en ningun catalogo existente -
// "hasUnicornHorn"/"yoraiz0rDarkness"/"hasFloatingTube" son flags de item.type EXACTO, no un slot
// de equipo con su propia lista de ids).
//
// - Unicorn Horn (item.type==4563, Player.cs:37267-37270): TextureAssets.Extra[143],
//   PlayerDrawLayers.cs:2860-2870 (DrawPlayer_22_FaceAcc, justo despues de FaceFlower) - dibujado
//   con `drawinfo.drawPlayer.bodyFrame` como rectangulo de origen sobre su PROPIA textura, misma
//   convencion "tira 40x(56*N) alineada al lienzo" que el resto de accesorios de tipo Face
//   (ver acc_face/) - se guarda la hoja ENTERA, sin recortar (modo 'hoja').
// - Yoraiz0r Darkness (item.type==3581, Player.cs:37255-37258): TextureAssets.Extra[67],
//   PlayerDrawLayers.cs:2626-2632 (DrawPlayer_21_Head_TheFace, rama de la piel base SIN faceHead
//   puesto) - dibujado con `drawinfo.colorHead`/`skinDyePacked`, MISMA posicion/convencion que
//   la piel base de la cabeza (Players[skinVar,0], ya extraida como frame0 unico en este
//   proyecto - "cabeza... se queda siempre en su frame de reposo", ver el comentario de clase de
//   PlayerPreviewRenderer.cs) - se recorta a un unico frame 40x56 (modo 'frame0'), mismo criterio
//   que HeadBackFile/BeardFile.
//
// - Floating Tube (item.type==4404, Player.cs:36345-36347): TextureAssets.Extra[105], dibujado
//   DOS VECES en capas reales distintas (PlayerDrawLayers.cs:2294-2300/3352-3358) con la MISMA
//   posicion "torso" pero DOS FRAMES DISTINTOS de la misma tira 40x112 (2 filas) - split espacial
//   fijo (frame0 detras/frame1 delante), NO animacion en el tiempo. Investigacion previa lo habia
//   marcado INCONCLUSIVE ("depende de estar mojado") - CORREGIDO: 0 dependencia real de wet/
//   lavaWet/honeyWet en todo el pipeline de dibujado (ParidadPersonaje Fase5, ver el comentario
//   real completo en EquipmentAppearanceResolver.cs). Se guarda la hoja ENTERA de 2 filas (modo
//   'hoja'), el renderer elige la fila real en cada punto de dibujado (LoadStripFrameAbsolute).
//
// Yoraiz0r Eye (item.type==3580) queda FUERA de este script a proposito: no dibuja ningun sprite
// estatico real (Player.cs:12616-12664, solo particulas de luz/polvo en tiempo real ligadas a la
// velocidad del jugador) - LIMITE REAL documentado en EquipmentAppearanceResolver.cs, sin sprite
// que extraer.
//
// Uso: node scripts/extraer-sprites-estados-especiales.js
// Salida: Terrakeep.App/Assets/player/extra/143.png (hoja 40x(56*N)),
//         Terrakeep.App/Assets/player/extra/67.png (frame0 40x56),
//         Terrakeep.App/Assets/player/extra/105.png (hoja 40x112, 2 filas)

'use strict';
const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const OUT_DIR = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'player', 'extra');

const W = 40, H = 56;

function fullSheet(xnbPath) {
  const { png } = xnbToPng(xnbPath);
  return png;
}

function cropFrame0(xnbPath) {
  const { png, width, height } = xnbToPng(xnbPath);
  if (width < W || height < H) return null;
  const out = new PNG({ width: W, height: H });
  PNG.bitblt(png, out, 0, 0, W, H, 0, 0);
  return out;
}

fs.mkdirSync(OUT_DIR, { recursive: true });

const ITEMS = [
  { id: 143, nombre: 'UnicornHorn', modo: 'hoja' },
  { id: 67, nombre: 'Yoraiz0rDarkness', modo: 'frame0' },
  { id: 105, nombre: 'FloatingTube', modo: 'hoja' },
];

for (const { id, nombre, modo } of ITEMS) {
  const xnbPath = path.join(STEAM_IMAGES, `Extra_${id}.xnb`);
  if (!fs.existsSync(xnbPath)) {
    console.log(`${nombre} (Extra_${id}): SIN fichero real en la instalacion`);
    continue;
  }
  const png = modo === 'hoja' ? fullSheet(xnbPath) : cropFrame0(xnbPath);
  if (!png) {
    console.log(`${nombre} (Extra_${id}): descartado, lienzo mas pequeño que 40x56`);
    continue;
  }
  fs.writeFileSync(path.join(OUT_DIR, id + '.png'), PNG.sync.write(png));
  console.log(`${nombre} (Extra_${id}): extraido -> ${path.join(OUT_DIR, id + '.png')}`);
}
