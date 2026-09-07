// Gemelo de extraer-iconos-tiles.js para las paredes - ver ESPEC-sprites-botones-badges.md#A.4.
//
// walls.json de TEdit NO trae geometria (solo id/name/key/color/blendType/largeFrameType,
// comprobado sobre sus 367 entradas), asi que sale entera del codigo real del juego:
//   - Framing.cs:119   -> wallFrameSize = Point16(36, 36)  (celdas de 36x36 en la hoja)
//   - WallDrawing.cs:213 -> Rectangle(0,0,32,32)           (de cada celda se pinta 32x32)
//   - Framing.cs:400-409 + :135 -> con los 4 vecinos encajando, style=15, y
//     AddWallFrameLookup(15, 1,1, 2,1, 3,1, 2,5) pone sus variantes en las celdas (1,1)/(2,1)/
//     (3,1)/(2,5). Se coge la PRIMERA, celda (1,1) = pixel (36,36): es literalmente el fotograma
//     que el juego usa en el interior macizo de una zona de pared, el icono representativo obvio.
//
// Uso: node scripts/extraer-iconos-paredes.js
// Salida: Terrakeep.App/Assets/vanilla/wall_icons/{id}.png (32x32 cada uno)
'use strict';
const fs = require('fs');
const path = require('path');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');
const { PNG } = require('pngjs');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const WALLS_JSON = path.join(__dirname, '..', '..', 'Terrasavr-Calamity-Beta', 'resources', 'app',
    'xnb-lzx-tool-refs', 'walls.json');
const OUT_DIR = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'vanilla', 'wall_icons');

const FRAME = 36;   // wallFrameSize real
const DIBUJO = 32;  // lo que el juego pinta de cada celda
const CELDA_X = 1, CELDA_Y = 1; // estilo 15, primera variante

const walls = JSON.parse(fs.readFileSync(WALLS_JSON, 'utf8'));
fs.mkdirSync(OUT_DIR, { recursive: true });

let ok = 0, sinXnb = [], fuera = [], vacias = [];
for (const wall of walls) {
    // id 0 = "Sky" = NO hay pared: no existe Wall_0.xnb y WorldPresenceIndex ya salta wall==0.
    const xnbPath = path.join(STEAM_IMAGES, `Wall_${wall.id}.xnb`);
    if (!fs.existsSync(xnbPath)) { sinXnb.push(`${wall.id}:${wall.name}`); continue; }
    let src;
    try { src = xnbToPng(xnbPath); } catch (e) { sinXnb.push(`${wall.id}:ERROR ${e.message}`); continue; }

    const x = CELDA_X * FRAME, y = CELDA_Y * FRAME;
    if (x + DIBUJO > src.width || y + DIBUJO > src.height) { fuera.push(`${wall.id}:${src.width}x${src.height}`); continue; }

    const out = new PNG({ width: DIBUJO, height: DIBUJO });
    PNG.bitblt(src.png, out, x, y, DIBUJO, DIBUJO, 0, 0);
    let opaco = false;
    for (let i = 3; i < out.data.length; i += 4) if (out.data[i] > 0) { opaco = true; break; }
    if (!opaco) { vacias.push(`${wall.id}:${wall.name}`); continue; }

    fs.writeFileSync(path.join(OUT_DIR, `${wall.id}.png`), PNG.sync.write(out));
    ok++;
}
console.log(`\nIconos de pared: ${ok} de ${walls.length}`);
console.log(`  sin Wall_{id}.xnb: ${sinXnb.join(', ') || '(ninguno)'}`);
console.log(`  fuera de rango: ${fuera.join(', ') || '(ninguno)'} | vacias: ${vacias.join(', ') || '(ninguna)'}`);
