// Extrae los iconos "fantasma"/watermark reales que Terraria dibuja en los slots de equipo
// vacios (mascota, mascota de luz, montura, vagoneta, gancho, tinte, armadura/vanidad de
// cabeza-cuerpo-piernas, accesorio, accesorio de vanidad) - pedido explicito 2-sep-2026
// ("extraer las imágenes del background de los slots de montura vagoneta mascota y gancho...
// con la armadura y accesorios podrias hacer igual").
//
// Investigacion real (consulta a Opus, sexta pasada, codigo decompilado de tModLoader
// 1.4.5.8): el mecanismo esta en Terraria.UI.ItemSlot.Draw() (ItemSlot.cs, ~lineas
// 2297-2384 del ensamblado decompilado), dibuja TextureAssets.Extra[54] (ExtrasID.cs:113,
// EquipIcons=54 -> Content/Images/Extra_54.xnb) solo cuando el slot esta vacio, a
// Color.White*0.35f. Atlas real: 3 columnas x 6 filas de celdas de 34x34, recortadas a 32x32
// (value.Width-=2; value.Height-=2 en el propio juego - 2px de relleno por celda). Verificado
// visualmente extrayendo el .xnb real: los 13 frames usados encajan exactamente con su
// significado (gancho = baston de caramelo, montura = herradura, mascota = huella, mascota de
// luz = estrella...).
//
// Tabla frame = row*3+col real, verbatim del switch de ItemSlot.cs (contextos 1=Monedas y
// 2=Municion NO tienen frame a proposito en el juego real - no se inventa ninguno aqui):
//   0=armor_head 1=dye 2=accessory_vanity 3=vanity_head 4=hook 6=armor_body 7=minecart
//   9=vanity_body 10=pet 11=accessory 12=armor_legs 13=mount 15=vanity_legs 17=pet_light
//
// A diferencia del juego real (blanco puro al 35% de alfa), aqui se retiñe cada silueta con
// TextSecondaryBrush (#8a8fa3, Theme.xaml) para que pertenezca a la paleta gris de Terrakeep
// en vez de ser un pixel crudo de Terraria - la opacidad al 35% se aplica luego en XAML
// (Opacity="0.35" sobre el Image), no aqui.
//
// Requiere la instalacion real de tModLoader en Steam (ver herramientas.json) y reutiliza
// xnb-to-png.js/lzx-decoder.js ya escritos y probados en el proyecto hermano (mismo criterio
// que extraer-descripciones-buffs-calamity.js con tmod-extract.js).
//
// Uso: node scripts/extraer-iconos-fantasma-slot.js
// Salida: Terrakeep.App/Assets/vanilla/slot_ghosts/{nombre}.png (13 archivos)

'use strict';
const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');

const XNB_PATH = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\tModLoader\\Content\\Images\\Extra_54.xnb';
const OUT_DIR = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'vanilla', 'slot_ghosts');

const CELL = 34, CROP = 32;
const TINT = { r: 0x8a, g: 0x8f, b: 0xa3 };

const FRAMES = {
  armor_head: 0, dye: 1, accessory_vanity: 2,
  vanity_head: 3, hook: 4,
  armor_body: 6, minecart: 7,
  vanity_body: 9, pet: 10, accessory: 11,
  armor_legs: 12, mount: 13,
  vanity_legs: 15,
  pet_light: 17,
};

function main() {
  const { png: atlas, width, height } = xnbToPng(XNB_PATH);
  if (width !== 102 || height !== 204) {
    throw new Error(`Atlas de tamaño inesperado: ${width}x${height} (esperado 102x204, 3x6 celdas de 34px) - la tabla de frames puede haber cambiado de version`);
  }

  fs.mkdirSync(OUT_DIR, { recursive: true });

  for (const [name, frame] of Object.entries(FRAMES)) {
    const col = frame % 3, row = Math.floor(frame / 3);
    const ox = col * CELL, oy = row * CELL;
    const out = new PNG({ width: CROP, height: CROP });
    for (let y = 0; y < CROP; y++) {
      for (let x = 0; x < CROP; x++) {
        const si = (atlas.width * (oy + y) + (ox + x)) << 2;
        const di = (CROP * y + x) << 2;
        const alpha = atlas.data[si + 3];
        out.data[di] = TINT.r;
        out.data[di + 1] = TINT.g;
        out.data[di + 2] = TINT.b;
        out.data[di + 3] = alpha;
      }
    }
    const outPath = path.join(OUT_DIR, name + '.png');
    fs.writeFileSync(outPath, PNG.sync.write(out));
    console.log('OK', name, `(frame ${frame})`, '->', outPath);
  }
}

main();
