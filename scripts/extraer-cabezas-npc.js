// Sexta auditoria de Opus, H6-08/H6-09/H6-10 ("el mapa del mundo muestra 4 puntos rosas que el
// usuario cree que son mascotas -en realidad son NPCs sin icono real-, deberia verse solo
// cabezas de NPC, no el cuerpo entero"). Extrae de la instalacion real de Steam los 81 iconos
// reales de cabeza de NPC de pueblo (Images/NPC_Head_{0..80}.xnb, confirmado real y completo
// con `ls`) - el mismo asset que usa el propio mapa del juego real (TextureAssets.NpcHead[],
// AssetInitializer.cs: "Images/NPC_Head_" + indice). El indice REAL por NPC (no 1:1 con el
// tipo de NPC) sale de Terraria.GameContent.TownNPCProfiles.cs decompilado real - ver
// Terrakeep.Core/Data/NpcHeadProfile.cs, que porta esa tabla tal cual.
//
// Uso: node scripts/extraer-cabezas-npc.js
// (necesita NODE_PATH=...Terrasavr-Calamity-Beta\resources\app\node_modules para pngjs)
// Salida: Terrakeep.App/Assets/npc_heads/{0..80}.png
'use strict';
const fs = require('fs');
const path = require('path');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');
const { PNG } = require('pngjs');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const OUT_DIR = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'npc_heads');

fs.mkdirSync(OUT_DIR, { recursive: true });

let ok = 0, faltan = 0;
for (let i = 0; i <= 80; i++) {
    const xnbPath = path.join(STEAM_IMAGES, `NPC_Head_${i}.xnb`);
    if (!fs.existsSync(xnbPath)) { console.log(`falta: NPC_Head_${i}.xnb`); faltan++; continue; }
    const { png } = xnbToPng(xnbPath);
    const buffer = PNG.sync.write(png);
    fs.writeFileSync(path.join(OUT_DIR, `${i}.png`), buffer);
    ok++;
}

console.log(`Cabezas de NPC: ${ok} extraidas, ${faltan} ausentes (esperado 81/0).`);
