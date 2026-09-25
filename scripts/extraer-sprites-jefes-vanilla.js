// Guia Encargo4 (25-sep-2026): "el banner objetivo actual y el arbol de la Guia deben mostrar el
// sprite real del jefe cuando el hito lo requiera" - Encargo3 dejo `GuidePasoViewModel.IconPath`
// con la cadena de fallback jefe->objeto->npc lista, pero el caso "jefe" no resolvia nada porque
// `NpcIconResolver` solo cubre los 27+13 NPCs de pueblo (`VanillaTownNpcRoster`), nunca jefes de
// verdad. Este script arranca del mismo algoritmo real de extraer-sprites-npcs-mascotas.js (primer
// bloque de filas con contenido, cortando en la primera fila totalmente transparente) pero AMPLIADO
// tras comprobar en la propia extraccion real que ese algoritmo original solo (con su heuristica
// "minScan = ancho*0.5") se equivocaba en 2 de los 23 jefes reales (Cutris/Reina Slime=657 y
// Deerclops=668): ambos usan una hoja en REJILLA (varios frames por fila ademas de por columna,
// no solo una tira vertical), y "minScan" saltaba de largo la fila vacia real que separa el primer
// frame del resto, capturando 2-3 frames pegados en vez de uno. Version real aqui: SIN heuristica
// de salto (se busca la primera fila vacia real desde el principio, sin asumir una altura minima)
// y, ademas, se recorta tambien por COLUMNA dentro de ese bloque de filas (primer tramo de columnas
// con contenido) - asi un jefe en rejilla (varios frames por fila Y por columna) tambien recorta un
// unico frame, no una fila entera de la rejilla. Verificado visualmente contra los 23 sprites reales
// (Read de cada PNG resultante) antes de dar el algoritmo por bueno - los jefes con tira de
// animacion simple (Ojo de Cthulhu, Rey Slime...) tampoco tienen una geometria de celda fija, ver
// aviso ya documentado en VanillaIconResolver.cs:9-21 para el caso analogo de objetos animados.
//
// Los 23 types reales de jefe/segmento final usados en guia_progresion.json (campo "jefe" de
// cada paso + "jefeFinal" de cada tramo, JSON real, no una lista de memoria - recalcular con un
// vistazo rapido al JSON si cambia: python -c "import json; d=json.load(open('Terrakeep.App/
// Assets/guia/guia_progresion.json',encoding='utf-8')); print(sorted({p.get('jefe',0) for t in
// d['tramos'] for p in t['pasos']}|{t.get('jefeFinal',0) for t in d['tramos']})-{0})"
//
// Uso: node scripts/extraer-sprites-jefes-vanilla.js
// (necesita NODE_PATH=...Terrasavr-Calamity-Beta\resources\app\node_modules para pngjs)
// Salida: Terrakeep.App/Assets/boss_icons/{type}.png
'use strict';
const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const OUT_DIR = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'boss_icons');

// Los 23 NPC types reales que aparecen como paso.jefe/tramo.jefeFinal en guia_progresion.json
// (extraidos del propio JSON, no de memoria - ver comentario de cabecera).
const IDS_JEFES = [
    4,   // EyeofCthulhu
    13,  // EaterofWorldsHead
    35,  // SkeletronHead
    50,  // KingSlime
    113, // WallofFlesh
    134, // TheDestroyer
    222, // QueenBee
    245, // Golem
    262, // Plantera
    325, // MourningWood
    327, // Pumpking
    344, // Everscream
    345, // IceQueen
    346, // SantaNK1
    370, // DukeFishron
    398, // MoonLordCore
    439, // CultistBoss
    493, // LunarTowerStardust
    551, // DD2Betsy
    618, // BloodNautilus (Dreadnautilus en el juego)
    636, // HallowBoss (Emperatriz de la Luz)
    657, // QueenSlimeBoss
    668, // Deerclops
];

function filaVacia(png, y) {
    const { width, data } = png;
    for (let x = 0; x < width; x++) {
        if (data[(y * width + x) * 4 + 3] !== 0) return false;
    }
    return true;
}

function columnaVaciaEnRango(png, x, y0, y1) {
    const { width, data } = png;
    for (let y = y0; y < y1; y++) {
        if (data[(y * width + x) * 4 + 3] !== 0) return false;
    }
    return true;
}

// Primer frame real de la hoja: primer bloque de filas con contenido (sin heuristica de salto,
// ver comentario de cabecera) y, dentro de ese bloque, primer bloque de columnas con contenido -
// cubre tanto una tira vertical simple (x0=0, w=ancho completo) como una rejilla con varios
// frames por fila (Reina Slime/Deerclops).
function primerFrame(png) {
    const { width, height } = png;
    let y0 = 0;
    while (y0 < height && filaVacia(png, y0)) y0++;
    let y1 = y0;
    while (y1 < height && !filaVacia(png, y1)) y1++;

    let x0 = 0;
    while (x0 < width && columnaVaciaEnRango(png, x0, y0, y1)) x0++;
    let x1 = x0;
    while (x1 < width && !columnaVaciaEnRango(png, x1, y0, y1)) x1++;

    return { x0, y0, w: x1 - x0, h: y1 - y0 };
}

fs.mkdirSync(OUT_DIR, { recursive: true });

let ok = 0;
const fallos = [];
for (const id of IDS_JEFES) {
    const xnbPath = path.join(STEAM_IMAGES, `NPC_${id}.xnb`);
    if (!fs.existsSync(xnbPath)) { console.log(`${id} FALTA: no existe ${xnbPath}`); fallos.push(id); continue; }
    try {
        const { png } = xnbToPng(xnbPath);
        const f = primerFrame(png);
        const recortado = new PNG({ width: f.w, height: f.h });
        PNG.bitblt(png, recortado, f.x0, f.y0, f.w, f.h, 0, 0);
        fs.writeFileSync(path.join(OUT_DIR, `${id}.png`), PNG.sync.write(recortado));
        console.log(`${id} -> ${f.w}x${f.h} (recorte x0=${f.x0},y0=${f.y0}) OK`);
        ok++;
    } catch (e) {
        console.log(`${id} FALLO: ${e.message}`);
        fallos.push(id);
    }
}
console.log(`\nTotal: ${ok}/${IDS_JEFES.length} extraidos. Fallos: ${fallos.join(',') || 'ninguno'}`);
