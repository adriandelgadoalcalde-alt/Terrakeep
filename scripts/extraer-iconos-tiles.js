// Encargo del usuario 4-sep-2026 ("faltan todos los sprites en exploracion de que es cada cosa
// como lo que es un cofre dorado de agua etc solo salen cuadrados de colores") - ver
// ESPEC-sprites-botones-badges.md#A.
//
// Extrae un icono real por TIPO de tile (frame base, 0,0) y ademas uno por VARIANTE de cofre/
// comoda (los 3 unicos tipos contenedores reales, TileID.Sets.BasicChest={21,467} y
// BasicDresser={88} - Terraria/ID/TileID.cs:359,367), que es la unica vista que usa el (u,v)
// real del .wld.
//
// GEOMETRIA REAL, no un rectangulo: el hueco de 2px entre celdas de la hoja de sprites esta
// TRANSPARENTE del todo (comprobado a nivel de pixel en Tiles_21.xnb: x=16,17 con alpha 0), asi
// que un recorte rectangular meteria una costura por el centro del objeto. El juego dibuja
// CELDA A CELDA (Terraria/GameContent/Drawing/TileDrawing.cs:1951, Rectangle(frameX,frameY,16,16)
// por cada tile de un objeto multi-casilla, con frameX avanzando textureGrid+frameGap) - esto
// compone lo mismo: bitblt de cada celda de textureGrid px, pegadas sin hueco.
//
// Uso: node scripts/extraer-iconos-tiles.js
// (necesita NODE_PATH=...Terrasavr-Calamity-Beta\resources\app\node_modules para pngjs, y que
//  xnb-lzx-tool-refs/tiles.json exista - esa carpeta esta gitignorada en el otro repo, si falta
//  hay que rebajar src/TEdit.Terraria/Data/tiles.json de github.com/TEdit/Terraria-Map-Editor)
// Salida: TerrasavrNative.App/Assets/vanilla/tile_icons/{id}.png  y  {id}_{u}_{v}.png
'use strict';
const fs = require('fs');
const path = require('path');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');
const { PNG } = require('pngjs');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const TILES_JSON = path.join(__dirname, '..', '..', 'Terrasavr-Calamity-Beta', 'resources', 'app',
    'xnb-lzx-tool-refs', 'tiles.json');
const OUT_DIR = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'vanilla', 'tile_icons');

// Los 3 unicos tipos de tile que pueden ser el contenedor de un chest real del .wld
// (TileID.Sets.BasicChest = {21,467}, TileID.Sets.BasicDresser = {88}) - son los unicos para los
// que la UI pide un icono POR VARIANTE (u,v). Ampliar esta lista es la unica linea que hay que
// tocar si algun dia hiciera falta el arbol de variantes de Objetos->Tiles.
const TIPOS_CON_VARIANTE = [21, 88, 467];

const tiles = JSON.parse(fs.readFileSync(TILES_JSON, 'utf8'));
fs.mkdirSync(OUT_DIR, { recursive: true });

// Compone un icono limpio celda a celda. Devuelve null si el recorte no cabe en la hoja real.
function componer(src, u, v, celdasX, celdasY, textureGrid, frameGap) {
    const anchoNecesario = u + (celdasX - 1) * (textureGrid[0] + frameGap[0]) + textureGrid[0];
    const altoNecesario = v + (celdasY - 1) * (textureGrid[1] + frameGap[1]) + textureGrid[1];
    if (anchoNecesario > src.width || altoNecesario > src.height) return null;
    const out = new PNG({ width: celdasX * textureGrid[0], height: celdasY * textureGrid[1] });
    for (let cy = 0; cy < celdasY; cy++) {
        for (let cx = 0; cx < celdasX; cx++) {
            PNG.bitblt(src.png, out,
                u + cx * (textureGrid[0] + frameGap[0]),
                v + cy * (textureGrid[1] + frameGap[1]),
                textureGrid[0], textureGrid[1],
                cx * textureGrid[0], cy * textureGrid[1]);
        }
    }
    return out;
}

// Respaldo real para el UNICO desajuste de metadatos de las 754 entradas: id=171 Christmas Tree
// dice frameGap [2,2] pero su Tiles_171.xnb real mide 64x128 = 4*16 x 8*16 exactos, sin relleno.
function icono(tile, src, u, v, tamCeldas) {
    const [cx, cy] = tamCeldas;
    return componer(src, u, v, cx, cy, tile.textureGrid, tile.frameGap)
        || componer(src, u, v, cx, cy, tile.textureGrid, [0, 0]);
}

const totalmenteTransparente = (png) => {
    for (let i = 3; i < png.data.length; i += 4) if (png.data[i] > 0) return false;
    return true;
};

let base = 0, variantes = 0, sinXnb = 0, sinRecorte = 0, transparentes = [];
for (const tile of tiles) {
    const xnbPath = path.join(STEAM_IMAGES, `Tiles_${tile.id}.xnb`);
    if (!fs.existsSync(xnbPath)) { sinXnb++; continue; }
    let src;
    try { src = xnbToPng(xnbPath); } catch (e) { console.log(`ERROR xnb id=${tile.id}: ${e.message}`); sinXnb++; continue; }

    // 1) icono base del TIPO: frame (0,0) con frameSize[0]. Deliberado, no el (u,v) real de una
    //    instancia del mundo: para un bloque no enmarcado ese (u,v) es un recorte de blending con
    //    los vecinos, no un icono limpio - y las vistas que usan este icono (Minerales,
    //    Objetos->Tiles) agrupan por Type a secas, sin variante.
    const iconoBase = icono(tile, src, 0, 0, tile.frameSize[0]);
    if (!iconoBase) { sinRecorte++; }
    else if (totalmenteTransparente(iconoBase)) { transparentes.push(`${tile.id}:${tile.name}`); }
    else { fs.writeFileSync(path.join(OUT_DIR, `${tile.id}.png`), PNG.sync.write(iconoBase)); base++; }

    // 2) variantes con nombre, solo para los tipos contenedores
    if (!TIPOS_CON_VARIANTE.includes(tile.id)) continue;
    for (const frame of (tile.frames || [])) {
        // 178 de los 9546 frames reales no traen 'uv' (frame unico, uv implicito en 0,0);
        // 317 traen su propio 'size' en celdas, que manda sobre frameSize del tile - eso resuelve
        // por si solo los 3 tiles con mas de un frameSize (165/185/233), sin adivinar nada.
        const uv = frame.uv || [0, 0];
        const tam = frame.size || tile.frameSize[0];
        const png = icono(tile, src, uv[0], uv[1], tam);
        if (!png || totalmenteTransparente(png)) continue;
        fs.writeFileSync(path.join(OUT_DIR, `${tile.id}_${uv[0]}_${uv[1]}.png`), PNG.sync.write(png));
        variantes++;
    }
}

console.log(`\nIconos de tile: ${base} base + ${variantes} variantes de cofre = ${base + variantes}`);
console.log(`  ${sinXnb} sin Tiles_{id}.xnb real, ${sinRecorte} sin recorte posible`);
console.log(`  ${transparentes.length} descartados por salir 100% transparentes: ${transparentes.join(', ')}`);
