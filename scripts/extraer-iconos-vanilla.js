// Sexta auditoria de Opus, H6-11 ("objetos vanilla animados -Alma de vuelo/Alma de luz, etc.-
// salen como una tira de fotogramas entera, no un unico icono; auditar TODOS los objetos
// vanilla y quedarse con un unico fotograma consistente").
//
// Los iconos vanilla actuales (Assets/vanilla/icons/{id}.png, 5454 ficheros) salian de un
// atlas "items.png" (32 columnas, celdas de 40x40) heredado de Terrasavr-Calamity-Beta - para
// los ~102 objetos REALMENTE animados esa celda de 40x40 no basta (el sprite real es mas alto
// que ancho, varias veces) y el recorte capturaba la tira entera en vez de un unico fotograma.
// Ademas el atlas se quedaba corto de verdad: solo 5454 de los 6134 Item_{id}.xnb reales que
// tiene la instalacion de Steam (680 objetos sin icono real en absoluto, no solo mal recortados
// - PalladiumDrill/1189 era solo UNO de los huecos reales, no el unico).
//
// Esta pasada re-extrae TODOS los iconos vanilla de la fuente real e individual por objeto
// (Images/Item_{id}.xnb, mismo criterio ya usado con exito para jugador/armadura/NPCs esta
// misma ronda) - sin depender de ningun atlas ni de ninguna geometria de celda fija.
//
// La lista de animados es REAL, portada de Terraria.Main.InitializeItemAnimations() decompilado
// (Terraria/Main.cs) - no una lista a ojo:
//   - 15 ids con RegisterItemAnimation(id, new DrawAnimationVertical(ticks, frameCount)) explicito.
//   - TODOS los ids con ItemID.Sets.IsFood[id]==true (86 reales, Terraria/ID/ItemID.cs) reciben
//     DrawAnimationVertical(int.MaxValue, 3) - un bucle real, no una lista escrita a mano.
//   - id 5644 (Scrying Orb) usa DrawAnimationScryingOrb con FrameCount=9 (clase de animacion
//     distinta, pero el mismo recorte real: texture.Frame(1, FrameCount, 0, frameY) - una
//     tira VERTICAL de FrameCount fotogramas iguales, frame 0 = el de arriba del todo).
// Todos son tiras VERTICALES de FrameCount fotogramas del MISMO alto (confirmado con
// Texture2D.Frame(1, FrameCount, 0, Y) real - horizontalFrames=1 siempre) - se recorta la franja
// superior (frame 0), alto_total/FrameCount, sin excepcion.
//
// Uso: node scripts/extraer-iconos-vanilla.js
// (necesita NODE_PATH=...Terrasavr-Calamity-Beta\resources\app\node_modules para pngjs)
// Salida: Terrakeep.App/Assets/vanilla/icons/{id}.png
'use strict';
const fs = require('fs');
const path = require('path');
const { xnbToPng } = require('../../Terrasavr-Calamity-Beta/resources/app/xnb-to-png.js');
const { PNG } = require('pngjs');

const STEAM_IMAGES = 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terraria\\Content\\Images';
const OUT_DIR = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'vanilla', 'icons');

// Los 15 ids explicitos reales (Main.cs, InitializeItemAnimations) - id -> frameCount.
const EXPLICIT_ANIMATED = {
    3581: 4, 3580: 4, 75: 8, 575: 4, 547: 4, 520: 4, 548: 4, 521: 4, 549: 4,
    3453: 4, 3454: 4, 3455: 4, 4068: 4, 4069: 4, 4070: 4,
};

// ItemID.Sets.IsFood real (Terraria/ID/ItemID.cs) - 86 ids reales, todos con
// DrawAnimationVertical(int.MaxValue, 3) real (3 fotogramas).
const FOOD_IDS = [
    353, 357, 1787, 1911, 1912, 1919, 1920, 2266, 2267, 2268, 2425, 2426, 2427, 3195, 3532,
    4009, 4010, 4011, 4012, 4013, 4014, 4015, 4016, 4017, 4018, 4019, 4020, 4021, 4022, 4023,
    4024, 4025, 4026, 4027, 4028, 4029, 4030, 4031, 4032, 4033, 4034, 4035, 4036, 4037, 967,
    969, 4282, 4283, 4284, 4285, 4286, 4287, 4288, 4289, 4290, 4291, 4292, 4293, 4294, 4295,
    4296, 4297, 4403, 4411, 4614, 4615, 4616, 4617, 4618, 4619, 4620, 4621, 4622, 4623, 4624,
    4625, 5009, 5042, 5041, 5092, 5093, 5275, 5277, 5278, 5537, 5645,
];

const ANIMATED = { ...EXPLICIT_ANIMATED };
for (const id of FOOD_IDS) ANIMATED[id] = 3;
ANIMATED[5644] = 9; // Scrying Orb (DrawAnimationScryingOrb), mismo recorte real

const MAX_ID = 6195;

let ok = 0, faltan = 0, animadosRecortados = 0, alturaImpar = 0;
for (let id = 0; id <= MAX_ID; id++) {
    const xnbPath = path.join(STEAM_IMAGES, `Item_${id}.xnb`);
    if (!fs.existsSync(xnbPath)) { faltan++; continue; }

    const { png, width, height } = xnbToPng(xnbPath);
    const frameCount = ANIMATED[id];
    let out = png, outHeight = height;

    if (frameCount) {
        if (height % frameCount !== 0) {
            console.log(`ALTURA IMPAR real: id=${id} height=${height} frameCount=${frameCount} (se redondea hacia abajo)`);
            alturaImpar++;
        }
        outHeight = Math.floor(height / frameCount);
        out = new PNG({ width, height: outHeight });
        PNG.bitblt(png, out, 0, 0, width, outHeight, 0, 0);
        animadosRecortados++;
    }

    fs.writeFileSync(path.join(OUT_DIR, `${id}.png`), PNG.sync.write(out));
    ok++;
}

console.log(`\nIconos vanilla: ${ok} extraidos (${animadosRecortados} animados recortados a 1 fotograma real), ${faltan} sin Item_{id}.xnb real, ${alturaImpar} con altura no divisible exacta.`);
