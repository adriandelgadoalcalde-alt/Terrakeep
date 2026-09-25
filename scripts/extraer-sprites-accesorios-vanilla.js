// Extrae los sprites REALES de los 7 tipos de accesorio funcional/vanidad (Waist/Neck/
// HandOn/HandOff/Back/Shield/Face) para el doll de cuerpo completo - PortSeleccion Encargo1
// (25-sep-2026), extension del mismo criterio ya usado para armadura (ver
// extraer-sprites-armadura-vanilla.js, hermano directo de este script - NO reinventar
// xnbToPng/la deteccion de "no cabe en el lienzo esperado").
//
// Confirmado en el decompilado real (Terraria.DataStructures.PlayerDrawLayers.cs) que
// nombre de fichero real y textura usa cada capa:
//   DrawPlayer_19_WaistAcc  -> TextureAssets.AccWaist[player.waist]   -> Images/Acc_Waist_N.xnb
//   DrawPlayer_20_NeckAcc   -> TextureAssets.AccNeck[player.neck]     -> Images/Acc_Neck_N.xnb
//   DrawPlayer_29_OnhandAcc -> TextureAssets.AccHandsOn[player.handon]   -> Images/Acc_HandsOn_N.xnb
//   DrawPlayer_18_OffhandAcc-> TextureAssets.AccHandsOff[player.handoff] -> Images/Acc_HandsOff_N.xnb
//   DrawPlayer_10_BackAcc   -> TextureAssets.AccBack[player.back]     -> Images/Acc_Back_N.xnb
//   (Shield, capa "Shield")-> TextureAssets.AccShield[player.shield] -> Images/Acc_Shield_N.xnb
//   DrawPlayer_22_FaceAcc   -> TextureAssets.AccFace[player.face/.faceFlower] -> Images/Acc_Face_N.xnb
// (rutas de fichero confirmadas independientemente en Terraria.Initializers.AssetInitializer.cs,
// bucles de LoadAsset<Texture2D>("Images/Acc_X_" + n) - mismo patron que Armor_Head_N/
// Armor_Legs_N ya usado por el script hermano).
//
// A diferencia de Head/Body/Legs (donde el indice de PlrLoadout.Items/Social YA ES el tipo:
// 0=cabeza/1=cuerpo/2=piernas), los 7 accesorios funcionales/vanidad de Terraria son slots
// GENERICOS (Player.cs, UpdateVisibleAccessory: "if (item.waistSlot > 0) waist = ...", igual
// para los otros 6 campos) - cualquiera de los 7 items en armor[3..9] puede llevar CUALQUIER
// tipo de accesorio. Por eso este script extrae por TIPO (todo lo referenciado en
// vanilla_accessory_slots.json bajo cada clave), y el resolver (EquipmentAppearanceResolver.cs)
// es quien escanea los 7 huecos del loadout buscando a que tipo pertenece cada item real.
//
// Se guarda la hoja REAL tal cual sale del XNB, sin recortar a un frame concreto - mismo
// criterio "hoja completa" ya usado para armor_legs/armor_body (extraer-sprites-armadura-
// vanilla.js, modo 'hoja'): la mayoria de estos accesorios se dibujan recortando bodyFrame/
// legFrame de su propia hoja (animan con el personaje, ver PlayerDrawLayers.cs), y Shield en
// concreto NO sigue la rejilla estandar (su propio ancho real sustituye a bodyFrame.Width,
// PlayerDrawLayers.cs:4958-4961) - guardar la hoja entera sin recortar es lo unico correcto
// para los 7 tipos a la vez sin inventar un tamano de recorte que no aplica a todos.
//
// Uso: node scripts/extraer-sprites-accesorios-vanilla.js
// Salida: Terrakeep.App/Assets/player/acc_waist/{id}.png, acc_neck/, acc_handon/, acc_handoff/,
//         acc_back/, acc_shield/, acc_face/ - hoja XNB->PNG real, sin recorte.

'use strict';
const fs = require('fs');
const path = require('path');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');
const { PNG } = require('pngjs');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const SLOTS_JSON = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'vanilla_accessory_slots.json');
const OUT_ROOT = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'player');

// clave del json -> [nombre legible, prefijo real del fichero XNB, carpeta de salida]
const TIPOS = {
  w: ['Waist', 'Acc_Waist_', 'acc_waist'],
  n: ['Neck', 'Acc_Neck_', 'acc_neck'],
  ho: ['HandOn', 'Acc_HandsOn_', 'acc_handon'],
  hf: ['HandOff', 'Acc_HandsOff_', 'acc_handoff'],
  bk: ['Back', 'Acc_Back_', 'acc_back'],
  s: ['Shield', 'Acc_Shield_', 'acc_shield'],
  fc: ['Face', 'Acc_Face_', 'acc_face'],
};

function fullSheet(xnbPath) {
  const { png } = xnbToPng(xnbPath);
  return png;
}

function extraerTipo(clave, ids) {
  const [nombre, prefijo, carpeta] = TIPOS[clave];
  const outDir = path.join(OUT_ROOT, carpeta);
  fs.mkdirSync(outDir, { recursive: true });
  let ok = 0, faltan = 0;
  const faltantes = [];
  for (const id of ids) {
    const xnbPath = path.join(STEAM_IMAGES, `${prefijo}${id}.xnb`);
    if (!fs.existsSync(xnbPath)) { faltan++; faltantes.push(id); continue; }
    const png = fullSheet(xnbPath);
    fs.writeFileSync(path.join(outDir, id + '.png'), PNG.sync.write(png));
    ok++;
  }
  console.log(`${nombre}: ${ok} extraidos, ${faltan} sin fichero real en la instalacion`);
  if (faltantes.length > 0) console.log(`  faltan ids: ${faltantes.slice(0, 20).join(', ')}${faltantes.length > 20 ? '...' : ''}`);
}

const slots = JSON.parse(fs.readFileSync(SLOTS_JSON, 'utf8'));

const idsPorTipo = { w: new Set(), n: new Set(), ho: new Set(), hf: new Set(), bk: new Set(), s: new Set(), fc: new Set() };
for (const entry of Object.values(slots)) {
  for (const clave of Object.keys(idsPorTipo)) {
    if (entry[clave] !== undefined) idsPorTipo[clave].add(entry[clave]);
  }
}

console.log(`ids unicos referenciados por tipo: ${Object.entries(idsPorTipo).map(([k, v]) => `${TIPOS[k][0]}=${v.size}`).join(' ')}`);

for (const clave of Object.keys(TIPOS)) {
  extraerTipo(clave, idsPorTipo[clave]);
}
