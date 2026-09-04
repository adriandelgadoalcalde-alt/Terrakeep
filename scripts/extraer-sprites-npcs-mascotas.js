// Pedido explicito del usuario (4-sep-2026, tras H6-08): "extrae las imagenes de las mascotas
// de slime, el perro, el gato, el mercader ambulante, el recaudador de impuestos etc para
// poner los sprites en Exploracion" - los 13 NPCs reales que el roster gano en H6-08 (ver
// VanillaTownNpcRoster.cs) nunca tuvieron sprite de CUERPO real para la lista lateral de
// Exploracion (NpcIconResolver, Assets/npc_icons/{id}.png) - solo se les extrajo la cabeza
// (Assets/npc_heads/, para el mapa). Mismo algoritmo real ya usado con exito para los 27
// originales (Terrasavr-Calamity-Beta/resources/app/extraer-sprites-npcs.js, 2-sep-2026): el
// primer frame de un NPC no tiene una altura fija (sombreros, vestidos largos...), asi que se
// recorta dinamicamente hasta la primera fila totalmente transparente por debajo de la
// cabecera del sprite (las hojas de animacion real de Terraria separan cada frame con filas
// vacias).
'use strict';
const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const OUT_DIR = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'npc_icons');

// Los 13 ids reales nuevos del roster (H6-08) - TravelingMerchant/TaxCollector mas las 11
// "mascotas de pueblo" 1.4.4 (Gato/Perro/Conejo/Slimes x8).
const IDS_NUEVOS = [368, 441, 637, 638, 656, 670, 678, 679, 680, 681, 682, 683, 684];

function alturaPrimerFrame(png) {
    const { width, height, data } = png;
    const minScan = Math.max(8, Math.floor(width * 0.5));
    for (let y = minScan; y < height; y++) {
        let filaVacia = true;
        for (let x = 0; x < width; x++) {
            const alpha = data[(y * width + x) * 4 + 3];
            if (alpha !== 0) { filaVacia = false; break; }
        }
        if (filaVacia) return y;
    }
    return Math.min(height, width * 2);
}

fs.mkdirSync(OUT_DIR, { recursive: true });

let ok = 0;
const fallos = [];
for (const id of IDS_NUEVOS) {
    const xnbPath = path.join(STEAM_IMAGES, `NPC_${id}.xnb`);
    try {
        const { png, width } = xnbToPng(xnbPath);
        const frameHeight = alturaPrimerFrame(png);
        const recortado = new PNG({ width, height: frameHeight });
        PNG.bitblt(png, recortado, 0, 0, width, frameHeight, 0, 0);
        fs.writeFileSync(path.join(OUT_DIR, `${id}.png`), PNG.sync.write(recortado));
        console.log(`${id} -> ${width}x${frameHeight} OK`);
        ok++;
    } catch (e) {
        console.log(`${id} FALLO: ${e.message}`);
        fallos.push(id);
    }
}
console.log(`\nTotal: ${ok}/${IDS_NUEVOS.length} extraidos. Fallos: ${fallos.join(',') || 'ninguno'}`);
