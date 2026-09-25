// GuiaCalamity Encargo B (25-sep-2026, revision-correccion-integral-familia-Keep, handoff
// e5eaea9e-c261-4199-8e7d-060b6054f58d): hermano DIRECTO de extraer-sprites-jefes-vanilla.js
// (Encargo4) para los jefes de CalamityMod - pero mas simple: CalamityMod.tmod trae, para casi
// todos los jefes, un icono de cabeza YA RECORTADO por el propio mod (formato .rawimg, el mismo
// que usa la barra de vida de jefe del juego), asi que NO hace falta ningun algoritmo de recorte
// de hoja de animacion como en el script vanilla - se decodifica el .rawimg tal cual (mismo
// formato ya usado en produccion por reemplazar-iconos.js: int32 version(=1) + int32 ancho +
// int32 alto + ancho*alto*4 bytes RGBA crudo, reencodeado a PNG real con pngjs). Confirmado real
// con 5 ejemplos decodificados a mano antes de escribir este script (Providence 82x30 no es una
// tira - Providence no llama a AddBossHeadTexture en su Load() real, usa el BossHeadTexture por
// defecto de ModNPC, la imagen YA es su unico icono de cabeza, con forma ancha real de dragon).
//
// Convencion real (confirmada listando las 60 entradas *_Head_Boss.rawimg reales del .tmod
// instalado v2.2.2 con tmod-extract.js): "NPCs/<Carpeta>/<InternalName>_Head_Boss.rawimg" - pero
// <Carpeta> NO siempre coincide con <InternalName> (ej. AresBody vive en NPCs/ExoMechs/Ares/,
// CalamitasClone en NPCs/CalClone/, RavagerBody en NPCs/Ravager/...), asi que en vez de adivinar
// la carpeta, este script busca directamente por SUFIJO del nombre de archivo
// ("/<InternalName>_Head_Boss.rawimg") sobre la lista real de archivos del .tmod - mas robusto
// que reconstruir la ruta completa a mano, y cubre los 26/29 pids que siguen la convencion
// directa sin necesidad de mantener 26 rutas literales.
//
// Los 3 casos reales que NO siguen la convencion (confirmados contra el codigo decompilado real
// de CalamityMod.dll v2.2.2, ver mapeo OVERRIDES_MANUALES abajo con la cita exacta de cada uno):
// Cryogen (su Texture override real es "Cryogen_Phase1", asi que su _Head_Boss tambien lleva ese
// sufijo de fase), Dragonfolly (vive en la carpeta/clase interna "Bumblebirb", override real
// BossHeadTexture="CalamityMod/NPCs/Bumblebirb/Birb_Head_Boss") y SupremeCalamitas (no usa la
// convencion _Head_Boss en absoluto - registra sus 3 iconos de cabeza a mano via
// AddBossHeadTexture con rutas explicitas, el primero/por defecto es "HoodedHeadIcon").
//
// Los 29 pids reales usados por guia_progresion.json (campo "jefeMod" de cada paso + "jefeFinalMod"/
// "jefeFinalModCarmesi" de cada tramo, recalculado iterando el JSON real - no de memoria):
// node -e "const fs=require('fs');const d=JSON.parse(fs.readFileSync('Terrakeep.App/Assets/guia/guia_progresion.json','utf8'));const s=new Set();for(const t of d.tramos){if(t.jefeFinalMod)s.add(t.jefeFinalMod);if(t.jefeFinalModCarmesi)s.add(t.jefeFinalModCarmesi);for(const p of t.pasos||[])if(p.jefeMod)s.add(p.jefeMod);}console.log(s.size,[...s].sort())"
// 27 de esos 29 pids aparecen a nivel de PASO (jefeMod) y quedan conectados de verdad en
// GuideViewModel.ResolverIconoDelHito; los otros 2 (HiveMind y PerforatorHive, ambos SOLO como
// jefeFinalMod/jefeFinalModCarmesi del tramo "HiveMindOPerforator", sin jefeMod en ningun paso
// propio) se extraen igualmente para el futuro pero hoy ningun paso los consume - MISMO patron
// exacto ya documentado en Encargo4 para jefeFinal=13 (Eater of Worlds) del lado vanilla.
//
// Uso: node scripts/extraer-sprites-jefes-calamity.js
// (necesita NODE_PATH=...Terrasavr-Calamity-Beta\resources\app\node_modules para pngjs)
// Salida: Terrakeep.App/Assets/calamity_boss_icons/{InternalName}.png
'use strict';
const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const { readTmod } = require('../../Terrasavr-Calamity-Beta/resources/app/tmod-extract.js');

const TMOD_PATH = 'C:\\Users\\adrian\\Documents\\My Games\\Terraria\\tModLoader\\Mods\\2026.6CalamityMod.tmod';
const OUT_DIR = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'calamity_boss_icons');

// Los 29 InternalName reales (pid = "CalamityMod/" + InternalName) que usa guia_progresion.json -
// ver comentario de cabecera para como se obtuvo esta lista del JSON real.
const INTERNAL_NAMES = [
    'AquaticScourgeHead',
    'AresBody',
    'AstrumAureus',
    'AstrumDeusHead',
    'BrimstoneElemental',
    'CalamitasClone',
    'CeaselessVoid',
    'Crabulon',
    'CragmawMire',
    'Cryogen',                    // OVERRIDE: Cryogen_Phase1_Head_Boss
    'DesertScourgeHead',
    'DevourerofGodsHead',
    'Dragonfolly',                // OVERRIDE: Bumblebirb/Birb_Head_Boss
    'GiantClam',
    'GreatSandShark',
    'HiveMind',                   // solo jefeFinalMod (tramo HiveMindOPerforator), sin paso propio
    'OldDuke',
    'PerforatorHive',             // solo jefeFinalModCarmesi (tramo HiveMindOPerforator, mundo carmesi)
    'PlaguebringerGoliath',
    'Polterghast',
    'PrimordialWyrmHead',
    'ProfanedGuardianCommander',
    'Providence',
    'RavagerBody',
    'Signus',
    'SlimeGodCore',
    'StormWeaverHead',
    'SupremeCalamitas',           // OVERRIDE: SupremeCalamitas/HoodedHeadIcon (sin sufijo _Head_Boss)
    'Yharon',
];

// Los 3 casos reales confirmados contra CalamityMod.dll v2.2.2 decompilado (ilspycmd) - citas
// exactas en el comentario de cabecera de este archivo.
const OVERRIDES_MANUALES = {
    Cryogen: 'NPCs/Cryogen/Cryogen_Phase1_Head_Boss.rawimg',
    Dragonfolly: 'NPCs/Bumblebirb/Birb_Head_Boss.rawimg',
    SupremeCalamitas: 'NPCs/SupremeCalamitas/HoodedHeadIcon.rawimg',
};

function rutaRawimgDe(internalName, nombresReales) {
    if (OVERRIDES_MANUALES[internalName]) return OVERRIDES_MANUALES[internalName];
    const sufijo = '/' + internalName + '_Head_Boss.rawimg';
    return nombresReales.find((n) => n.endsWith(sufijo)) || null;
}

// Mismo formato real que reemplazar-iconos.js (Terraria.ModLoader.IO.ImageIO.cs): int32
// version(=1) + int32 ancho + int32 alto + ancho*alto*4 bytes RGBA crudo. Los iconos de cabeza de
// jefe NUNCA son una tira de animacion (confirmado arriba), asi que aqui no hay frameCount que
// dividir - se decodifica tal cual.
function rawImgToPngBuffer(raw) {
    const version = raw.readInt32LE(0);
    if (version !== 1) throw new Error('Version de rawimg desconocida: ' + version);
    const width = raw.readInt32LE(4);
    const height = raw.readInt32LE(8);
    const esperado = 12 + width * height * 4;
    if (raw.length !== esperado) throw new Error('Tamano inesperado: ' + raw.length + ' != ' + esperado);
    const png = new PNG({ width, height });
    raw.copy(png.data, 0, 12, 12 + width * height * 4);
    return PNG.sync.write(png);
}

if (!fs.existsSync(TMOD_PATH)) {
    console.log('FALLO: no existe el .tmod real en', TMOD_PATH);
    process.exit(1);
}

console.log('Leyendo CalamityMod.tmod real (puede tardar, ~120-190 MB)...');
const mod = readTmod(TMOD_PATH);
console.log(mod.files.size, 'archivos,', mod.name, mod.version);
const nombresReales = [...mod.files.keys()];

fs.mkdirSync(OUT_DIR, { recursive: true });

let ok = 0;
const fallos = [];
for (const internalName of INTERNAL_NAMES) {
    const rawPath = rutaRawimgDe(internalName, nombresReales);
    if (!rawPath) {
        console.log(`${internalName} FALLO: no se encontro ningun _Head_Boss.rawimg real ni override manual`);
        fallos.push(internalName);
        continue;
    }
    const raw = mod.files.get(rawPath);
    if (!raw) {
        console.log(`${internalName} FALLO: '${rawPath}' no existe de verdad en el .tmod`);
        fallos.push(internalName);
        continue;
    }
    try {
        const pngBuf = rawImgToPngBuffer(raw);
        fs.writeFileSync(path.join(OUT_DIR, `${internalName}.png`), pngBuf);
        console.log(`${internalName} <- ${rawPath} OK`);
        ok++;
    } catch (e) {
        console.log(`${internalName} FALLO decodificando '${rawPath}': ${e.message}`);
        fallos.push(internalName);
    }
}
console.log(`\nTotal: ${ok}/${INTERNAL_NAMES.length} extraidos. Fallos: ${fallos.join(',') || 'ninguno'}`);
process.exit(fallos.length > 0 ? 1 : 0);
