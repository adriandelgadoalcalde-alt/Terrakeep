// Extrae las descripciones reales de buff de Calamity Mod - pedido explicito 2-sep-2026.
// Fuente real: el .tmod instalado (no hay decompilacion C# de esto, la localizacion es
// contenido crudo del mod, nunca se compila al ensamblado) - leido con el lector real de
// .tmod ya existente, tmod-extract.js.
//
// Confirmado en esta sesion: esta instalacion de Calamity SOLO trae localizacion en-US
// (no hay es-ES) - se guarda en ingles, sin traducir a mano (mismo criterio que el resto
// del proyecto: lo que no tiene traduccion real disponible se deja tal cual, nunca se
// inventa una).
//
// Formato hjson real de Localization/en-US/Mods.CalamityMod.Buffs.hjson (confirmado
// leyendolo): comentarios "//", bloques "NombreInterno: { DisplayName: ...\n Description:
// ... }" - parser tolerante por regex, suficiente para este formato concreto (no hace
// falta una libreria hjson completa).
const fs = require('fs');
const os = require('os');
const path = require('path');
const { readTmod } = require('../../Terrasavr-Calamity-Beta/resources/app/tmod-extract.js');

const tmodPath = path.join(os.homedir(), 'Documents', 'My Games', 'Terraria', 'tModLoader', 'Mods', '2026.6CalamityMod.tmod');
const OUT = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'calamity_buff_descriptions.json');

const mod = readTmod(tmodPath);
const buffsHjson = mod.files.get('Localization/en-US/Mods.CalamityMod.Buffs.hjson');
if (!buffsHjson) throw new Error('No se encontro Localization/en-US/Mods.CalamityMod.Buffs.hjson en el .tmod real');
const text = buffsHjson.toString('utf8');

// Cada bloque real: NombreInterno: {\n\tDisplayName: texto\n\tDescription: texto\n}
const blockRe = /^(\w+):\s*\{([^}]*)\}/gm;
const fieldRe = /(DisplayName|Description):\s*(.+)/;

const descriptions = {};
let m;
while ((m = blockRe.exec(text)) !== null) {
    const internalName = m[1];
    const body = m[2];
    let desc = null;
    for (const line of body.split('\n')) {
        const fm = fieldRe.exec(line.trim());
        if (fm && fm[1] === 'Description') desc = fm[2].trim();
    }
    if (desc) descriptions[internalName] = desc;
}

console.log(`${Object.keys(descriptions).length} descripciones reales de buff de Calamity (en-US, sin es-ES disponible en esta instalacion)`);
fs.writeFileSync(OUT, JSON.stringify(descriptions));
console.log('escrito', OUT);
