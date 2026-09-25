// Extrae los sprites REALES de los 11 tipos de accesorio funcional/vanidad (Waist/Neck/
// HandOn/HandOff/Back/Shield/Face/Shoe/Balloon/Beard/Front) para el doll de cuerpo completo -
// PortSeleccion Encargo1 (25-sep-2026), extension del mismo criterio ya usado para armadura (ver
// extraer-sprites-armadura-vanilla.js, hermano directo de este script - NO reinventar
// xnbToPng/la deteccion de "no cabe en el lienzo esperado"). GapAnalysis Encargo D
// (25-sep-2026): ampliado con el 8º tipo, Shoe (shoeSlot) - mismo patron exacto. GapAnalysis
// Encargo C (25-sep-2026): ampliado ademas con el 9º tipo, Balloon (balloonSlot) - mismo
// patron exacto. GapAnalysis Encargo G (25-sep-2026): ampliado ademas con el 10º tipo, Beard
// (beardSlot) - solo 4 ids reales en total (1-4, ArmorIDs.Beard.Count=5 con el 0="sin barba"),
// mismo patron de extraccion exacto (hoja completa sin recortar). GapAnalysis Encargo E
// (25-sep-2026): ampliado ademas con el 11º tipo, Front (frontSlot) - 11 ids reales
// (1,2,3,4,5,8,11,12,15,16,17 - ver vanilla_accessory_slots.json/scripts/extraer-slots-
// accesorios-vanilla.py), misma tira 40x(56*N) alineada al lienzo (TextureAssets.AccFront usa
// drawPlayer.bodyFrame directamente, PlayerDrawLayers.cs:3897-3904 - NO el ancho variable de
// Shield ni el offset propio de Balloon), mismo patron de extraccion exacto.
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
//   DrawPlayer_14_Shoes     -> TextureAssets.AccShoes[player.shoe]   -> Images/Acc_Shoes_N.xnb
//   (Balloon, DrawPlayer_11_Balloons/_12_1_BalloonFronts)-> TextureAssets.AccBalloon[player.
//     balloon/.balloonFront] -> Images/Acc_Balloon_N.xnb
//   (Beard, dentro de la capa Head, DESPUES de casco/pelo)-> TextureAssets.AccBeard[player.
//     beard] -> Images/Acc_Beard_N.xnb
//   DrawPlayer_32_FrontAcc(_FrontPart/_BackPart) -> TextureAssets.AccFront[player.front] ->
//     Images/Acc_Front_N.xnb
// (rutas de fichero confirmadas independientemente en Terraria.Initializers.AssetInitializer.cs,
// bucles de LoadAsset<Texture2D>("Images/Acc_X_" + n) - mismo patron que Armor_Head_N/
// Armor_Legs_N ya usado por el script hermano; AccShoes concretamente en AssetInitializer.cs:
// "TextureAssets.AccShoes[num18] = LoadAsset<Texture2D>("Images/Acc_Shoes_" + num18, ...)";
// AccBalloon analogo, "TextureAssets.AccBalloon[num23] = LoadAsset<Texture2D>("Images/
// Acc_Balloon_" + num23, ...)"; AccBeard analogo, AssetInitializer.cs:538,
// "TextureAssets.AccBeard[num24] = LoadAsset<Texture2D>("Images/Acc_Beard_" + num24, ...)");
// AccFront analogo, AssetInitializer.cs:508-510, "TextureAssets.AccFront[num17] =
// LoadAsset<Texture2D>("Images/Acc_Front_" + num17, ...)").
//
// A diferencia de Head/Body/Legs (donde el indice de PlrLoadout.Items/Social YA ES el tipo:
// 0=cabeza/1=cuerpo/2=piernas), los 9 accesorios funcionales/vanidad de Terraria son slots
// GENERICOS (Player.cs, UpdateVisibleAccessory: "if (item.waistSlot > 0) waist = ...", igual
// para los otros 8 campos, balloonSlot incluido) - cualquiera de los 7 items en armor[3..9]
// puede llevar CUALQUIER tipo de accesorio. Por eso este script extrae por TIPO (todo lo
// referenciado en vanilla_accessory_slots.json bajo cada clave), y el resolver
// (EquipmentAppearanceResolver.cs) es quien escanea los 7 huecos del loadout buscando a que
// tipo pertenece cada item real.
//
// Se guarda la hoja REAL tal cual sale del XNB, sin recortar a un frame concreto - mismo
// criterio "hoja completa" ya usado para armor_legs/armor_body (extraer-sprites-armadura-
// vanilla.js, modo 'hoja'): la mayoria de estos accesorios se dibujan recortando bodyFrame/
// legFrame de su propia hoja (animan con el personaje, ver PlayerDrawLayers.cs; Shoes en
// concreto usa legFrame, PlayerDrawLayers.cs:1758-1777 - misma fila que las perneras, NO
// bodyFrame), y Shield en concreto NO sigue la rejilla estandar (su propio ancho real
// sustituye a bodyFrame.Width, PlayerDrawLayers.cs:4958-4961) - guardar la hoja entera sin
// recortar es lo unico correcto para los 9 tipos a la vez sin inventar un tamano de recorte
// que no aplica a todos. Balloon en particular NO sigue la convencion bodyFrame/legFrame de
// ninguno de los otros 8 tipos (posicion propia via Main.OffsetsPlayerOffhand + 4 fotogramas
// reales de animacion propia, Height/4 - ver el comentario real de
// PlayerPreviewRenderer.LoadBalloonFrame para la cita completa) - se guarda igual la hoja
// entera sin recortar, el recorte/posicionado real lo hace el renderer, no este script.
//
// GapAnalysis Encargo J (25-sep-2026): ACC_FACE_SINTETICO_ANGEL_HALO - mismo patron exacto que
// SHOE_SINTETICOS_MALE_TO_FEMALE (mas abajo). Angel Halo (item.type==1987, Player.cs:37271-37274,
// "if (item.type == 1987) hasAngelHalo = true;") NUNCA declara item.faceSlot=7 - el juego real
// fuerza directamente `Main.instance.LoadAccFace(7); TextureAssets.AccFace[7]`
// (PlayerDrawLayers.cs:2871-2884) sin pasar por ningun faceSlot real, asi que el id 7 no sale
// nunca de vanilla_accessory_slots.json (confirmado: 0 entradas reales con fc=7 en ese fichero).
// Confirmado que Acc_Face_7.xnb SI existe en la instalacion real de Steam.
const ACC_FACE_SINTETICO_ANGEL_HALO = [7];
//
// Uso: node scripts/extraer-sprites-accesorios-vanilla.js
// Salida: Terrakeep.App/Assets/player/acc_waist/{id}.png, acc_neck/, acc_handon/, acc_handoff/,
//         acc_back/, acc_shield/, acc_face/, acc_shoes/, acc_balloon/, acc_beard/, acc_front/ -
//         hoja XNB->PNG real, sin recorte.

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
  sh: ['Shoe', 'Acc_Shoes_', 'acc_shoes'],
  bl: ['Balloon', 'Acc_Balloon_', 'acc_balloon'],
  bd: ['Beard', 'Acc_Beard_', 'acc_beard'],
  fr: ['Front', 'Acc_Front_', 'acc_front'],
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

const idsPorTipo = { w: new Set(), n: new Set(), ho: new Set(), hf: new Set(), bk: new Set(), s: new Set(), fc: new Set(), sh: new Set(), bl: new Set(), bd: new Set(), fr: new Set() };
for (const entry of Object.values(slots)) {
  for (const clave of Object.keys(idsPorTipo)) {
    if (entry[clave] !== undefined) idsPorTipo[clave].add(entry[clave]);
  }
}

// GapAnalysis Encargo D (25-sep-2026): mismo patron exacto que HEAD_SINTETICOS_FRONT_TO_BACK
// (extraer-sprites-armadura-vanilla.js) - shoeSlot 26 (GlassSlipperFemale) es la variante
// femenina REAL de shoeSlot 25 (GlassSlipperMale, ArmorIDs.cs:1869,
// "MaleToFemaleID = Factory.CreateIntSet(-1, 25, 26)"), aplicada en runtime por
// PlayerPreviewRenderer.Render (PlayerBodyDrawTables.ShoeMaleToFemaleID) - ningun item real
// declara shoeSlot=26 directamente (solo el 25, id 5077 "Glass Slipper"), asi que sin este
// id sintetico nunca saldria de vanilla_accessory_slots.json y el personaje femenino se
// quedaria sin sprite al llevar puesto ese accesorio. Confirmado que Acc_Shoes_26.xnb SI
// existe en la instalacion real de Steam.
const SHOE_SINTETICOS_MALE_TO_FEMALE = [26];
for (const id of SHOE_SINTETICOS_MALE_TO_FEMALE) idsPorTipo.sh.add(id);
for (const id of ACC_FACE_SINTETICO_ANGEL_HALO) idsPorTipo.fc.add(id);

console.log(`ids unicos referenciados por tipo: ${Object.entries(idsPorTipo).map(([k, v]) => `${TIPOS[k][0]}=${v.size}`).join(' ')}`);

for (const clave of Object.keys(TIPOS)) {
  extraerTipo(clave, idsPorTipo[clave]);
}
