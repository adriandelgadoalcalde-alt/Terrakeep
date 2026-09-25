// Wings Encargo1 (25-sep-2026): extrae los 51 sprites REALES de alas vanilla (Wings_1.xnb..
// Wings_51.xnb, TextureAssets.Wings[N] en el juego real) para la capa base estatica del doll de
// cuerpo completo - hermano directo de extraer-sprites-accesorios-vanilla.js, mismo criterio
// "hoja completa sin recortar" (el recorte real de la fila/fotograma 0 lo hace el renderer,
// PlayerPreviewRenderer.LoadWingFrame, no este script - ver WingDrawTable para la tabla real de
// anchor/divisor por id).
//
// A diferencia de los 11 tipos de acc_X ya extraidos (prefijo real "Acc_X_"), el prefijo de
// fichero real de Wings NO lleva "Acc_" - confirmado en Terraria.Initializers.
// AssetInitializer.cs decompilado real: "TextureAssets.Wings[num] = LoadAsset<Texture2D>(
// "Images/Wings_" + num, AssetRequestMode.ImmediateLoad);" (bucle 1..ArmorIDs.Wing.Count-1,
// distinto del bucle "Images/Acc_X_" + num que usan los otros 11 tipos).
//
// El rango es FIJO (1-51, ArmorIDs.Wing.Count real de la version instalada) - a diferencia de
// los 11 tipos de acc_X (generic slots resueltos desde vanilla_accessory_slots.json, que solo
// trae los ids REALMENTE referenciados por algun item), aqui no hace falta escanear el catalogo
// de items: los 51 sprites existen en la instalacion real independientemente de si algun item de
// la version de Item.cs que usa este proyecto (tModLoader 1.4.4.9) los asigna de verdad - 46 de
// los 51 SI tienen un item real que los asigna (ver vanilla_accessory_slots.json, campo "wg",
// scripts/extraer-slots-accesorios-vanilla.py); wingSlot 47-51 no estan asignados a ningun item
// en esa version concreta (limite real documentado, ver el comentario de
// VanillaAccessorySlotEntry.Wing) - se extraen igual los 51 sprites reales, por fidelidad
// completa y por si el catalogo de items se amplia en el futuro.
//
// Uso: node scripts/extraer-sprites-alas-vanilla.js
// Salida: Terrakeep.App/Assets/player/acc_wing/{id}.png (1..51) - hoja XNB->PNG real, sin recorte.

'use strict';
const fs = require('fs');
const path = require('path');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');
const { PNG } = require('pngjs');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const OUT_DIR = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'player', 'acc_wing');

const WING_COUNT = 51;

fs.mkdirSync(OUT_DIR, { recursive: true });

let ok = 0, faltan = 0;
const faltantes = [];
for (let id = 1; id <= WING_COUNT; id++) {
  const xnbPath = path.join(STEAM_IMAGES, `Wings_${id}.xnb`);
  if (!fs.existsSync(xnbPath)) { faltan++; faltantes.push(id); continue; }
  const { png } = xnbToPng(xnbPath);
  fs.writeFileSync(path.join(OUT_DIR, id + '.png'), PNG.sync.write(png));
  ok++;
}

console.log(`Wings: ${ok} extraidos, ${faltan} sin fichero real en la instalacion`);
if (faltantes.length > 0) console.log(`  faltan ids: ${faltantes.join(', ')}`);
