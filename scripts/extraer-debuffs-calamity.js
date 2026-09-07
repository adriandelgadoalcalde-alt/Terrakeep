// Sexta auditoria de Opus, H6-12 ("los buffs de Calamity no distinguen buff de debuff"):
// extrae de verdad, del propio codigo fuente C# de Calamity Mod (no una lista a mano), que
// buffs reales son debuffs. Fuente real: cada ModBuff que es un debuff pone
// `Main.debuff[base.Type] = true;` dentro de su propio SetStaticDefaults() (confirmado
// leyendo varios reales - CalamityMod/Buffs/StatDebuffs/Malnourished.cs,
// CalamityMod/Buffs/DamageOverTime/Bane.cs - y varios buffs POSITIVOS que lo ponen
// explicitamente a `false` - CalamityMod/Buffs/Potions/Zen.cs). Los buffs que heredan de una
// base compartida sin SetStaticDefaults propio (ej. BaseSummonBuff, todos los buffs de
// invocacion) nunca tocan Main.debuff - por defecto en Terraria real ese array es
// TODO-false, asi que "sin mencion" tambien es "no es debuff" de verdad, no una omision.
//
// El nombre de clase (nombre del fichero, sin .cs) coincide 1:1 con el campo "internal" real
// de calamity/buffs.json - mismo esquema que usa el resto del proyecto para casar el catalogo
// contra el codigo fuente real.
const fs = require('fs');
const path = require('path');

const BUFFS_DIR = 'C:\\Users\\adrian\\Downloads\\tModLoader-Decompiled\\CalamityMod\\CalamityMod\\Buffs';
const OUT_PATH = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'calamity', 'buff_debuffs.json');

function listCsFilesRecursive(dir) {
    let out = [];
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) out = out.concat(listCsFilesRecursive(full));
        else if (entry.name.endsWith('.cs')) out.push(full);
    }
    return out;
}

const files = listCsFilesRecursive(BUFFS_DIR);
const debuffs = {};
let trueCount = 0, falseCount = 0, noMention = 0;

for (const file of files) {
    const className = path.basename(file, '.cs');
    const content = fs.readFileSync(file, 'utf8');
    const match = content.match(/Main\.debuff\[base\.Type\]\s*=\s*(true|false);/);
    if (!match) { noMention++; continue; }
    if (match[1] === 'true') { debuffs[className] = true; trueCount++; }
    else falseCount++;
}

fs.writeFileSync(OUT_PATH, JSON.stringify(debuffs, Object.keys(debuffs).sort(), 2), 'utf8');

console.log(`Ficheros de buff reales escaneados: ${files.length}`);
console.log(`Main.debuff=true (debuffs reales): ${trueCount}`);
console.log(`Main.debuff=false (explicito, buffs reales): ${falseCount}`);
console.log(`Sin mencion (heredan de una base compartida, nunca debuff): ${noMention}`);
console.log(`Escrito -> ${OUT_PATH}`);

// Verificacion cruzada real contra el catalogo ya usado por la app: cuantas entradas de
// buffs.json tienen de verdad un fichero de clase real con ese nombre.
const catalogPath = path.join(__dirname, '..', 'Terrakeep.App', 'Assets', 'calamity', 'buffs.json');
const catalog = JSON.parse(fs.readFileSync(catalogPath, 'utf8'));
const knownClasses = new Set(files.map(f => path.basename(f, '.cs')));
const sinFichero = catalog.filter(e => !knownClasses.has(e.internal));
console.log(`\nCatalogo real (buffs.json): ${catalog.length} entradas, ${catalog.length - sinFichero.length} con fichero de clase real encontrado, ${sinFichero.length} SIN fichero (deberia ser 0 o casi 0):`);
if (sinFichero.length > 0) console.log(sinFichero.map(e => e.internal).join(', '));

const catalogDebuffCount = catalog.filter(e => debuffs[e.internal] === true).length;
console.log(`\nDe las ${catalog.length} entradas del catalogo, ${catalogDebuffCount} son debuffs reales (Main.debuff[base.Type]=true en su propio fichero).`);
