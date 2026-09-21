// Hover en Inicio, correccion real EN VIVO (usuario comparando contra Terraria vanilla,
// 21-sep-2026): "la mascota sale como sprite estatico, en vanilla se anima de verdad". Extrae
// las hojas de sprite REALES de los proyectiles de mascota (Projectile_{shoot}.xnb) que
// PetPreviewRenderer necesita para animar en vivo durante el hover, con el MISMO criterio ya
// establecido para legskin/pants/shoes (extraer-sprites-jugador.js, idea 10): se guarda la tira
// vertical ENTERA (PNG, sin recortar a un solo fotograma), y es el renderer quien recorta la
// fila real en tiempo de ejecucion.
//
// Fuente REAL de que proyectiles hacen falta y cuantos fotogramas tiene cada hoja
// (Terrakeep.App/Assets/pet_animations.json, generado desde el decompilado real - ver
// bitacora.md 21-sep-2026 para la cita completa: Terraria/ID/ProjectileID.cs
// CharacterPreviewAnimations + Terraria/Main.cs projFrames[id]).
//
// Uso: node scripts/extraer-sprites-mascotas.js
'use strict';
const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const OUT_DIR = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'pets');
const CATALOG_PATH = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'pet_animations.json');

const catalog = JSON.parse(fs.readFileSync(CATALOG_PATH, 'utf8'));
const shootsNeeded = new Map(); // shoot -> totalFrames real (de Main.projFrames)
for (const entry of Object.values(catalog)) {
  shootsNeeded.set(entry.shoot, entry.totalFrames);
}

fs.mkdirSync(OUT_DIR, { recursive: true });

let ok = 0, faltan = 0, inconsistentes = 0;
for (const [shoot, totalFrames] of shootsNeeded) {
  const xnbPath = path.join(STEAM_IMAGES, `Projectile_${shoot}.xnb`);
  if (!fs.existsSync(xnbPath)) {
    console.log(`  falta: Projectile_${shoot}.xnb`);
    faltan++;
    continue;
  }
  const { png, width, height } = xnbToPng(xnbPath);
  if (height % totalFrames !== 0) {
    console.log(`  Projectile_${shoot}.xnb: alto real ${height} NO es multiplo exacto de totalFrames=${totalFrames} (Main.projFrames real) - se guarda igual, el renderer usara Math.Floor`);
    inconsistentes++;
  }
  fs.writeFileSync(path.join(OUT_DIR, `${shoot}.png`), PNG.sync.write(png));
  console.log(`${shoot} -> ${width}x${height} (${totalFrames} fotogramas reales, ${(height / totalFrames).toFixed(1)}px/fotograma) OK`);
  ok++;
}
console.log(`\nTotal: ${ok} hojas extraidas, ${faltan} ausentes, ${inconsistentes} con alto no exacto (revisar a mano si aparece alguno).`);
