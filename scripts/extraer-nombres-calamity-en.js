// Nombre INGLES REAL de los objetos y buffs de Calamity, del `.tmod` real instalado.
//
// Por que hace falta (medido, no supuesto - ronda de traduccion del CONTENIDO del juego,
// 6-sep-2026): `catalog.json` y `buffs.json` traen un campo `displayName_fallback` que se venia
// usando como cara inglesa del catalogo, pero NO es el nombre real del mod en muchas entradas:
// es el nombre INTERNO de la clase "humanizado" separando por mayusculas. El caso que lo
// destapo, visto en el volcado del arnes A11: `AerospecHeadMelee` salia como
// "Aerospec Head Melee" cuando el `.hjson` real en-US dice `DisplayName: Aerospec Helm`.
//
// Fuente real: los `.hjson` de `Localization/en-US/` del `.tmod` instalado, bloques
// `NombreInterno: { DisplayName: ... }` - exactamente la misma fuente que ya usaban
// extraer-descripciones-buffs-calamity.js y extraer-bonos-set-calamity.js.
//
// Escribe `displayName_en` en las entradas de `calamity/catalog.json` y `calamity/buffs.json`
// (campo `_en` paralelo, mismo patron que `text_en`/`setBonus_en`). NO toca `displayName_es` ni
// `displayName_fallback`: el fallback humanizado se queda como ultimo recurso real para lo que
// de verdad no tenga `DisplayName` en el hjson. Lo que no se encuentra no se inventa.
//
// El ORDEN del array no se toca nunca: determina el id sintetico de cada objeto/buff.
//
// Uso: node scripts/extraer-nombres-calamity-en.js
'use strict';
const fs = require('fs');
const path = require('path');

const TMOD_PATH = 'C:\\Users\\adrian\\Documents\\My Games\\Terraria\\tModLoader\\Mods\\2026.6CalamityMod.tmod';
const TMOD_EXTRACT = 'C:\\Users\\adrian\\Downloads\\Terrasavr-Win\\Terrasavr-Calamity-Beta\\resources\\app\\tmod-extract.js';
const ASSETS = path.join(__dirname, '..', 'TerrasavrNative.App', 'Assets', 'calamity');

// Parser hjson minimo, gemelo del de extraer-bonos-set-calamity.js (solo lo que Calamity usa de
// verdad: claves sin comillas, objetos anidados, valores de una linea, bloques '''...''' y
// comentarios //).
function parseHjson(text) {
    let i = 0;
    const n = text.length;
    function skipWs() {
        while (i < n) {
            const c = text[i];
            if (c === ' ' || c === '\t' || c === '\r' || c === '\n') { i++; continue; }
            if (c === '/' && text[i + 1] === '/') { while (i < n && text[i] !== '\n') i++; continue; }
            break;
        }
    }
    function readKey() {
        skipWs();
        const start = i;
        while (i < n && !/[\s:{}]/.test(text[i])) i++;
        return text.substring(start, i);
    }
    function readValue() {
        skipWs();
        if (text.startsWith("'''", i)) {
            i += 3;
            const end = text.indexOf("'''", i);
            const block = text.substring(i, end);
            i = end + 3;
            return block.split('\n').map(l => l.trim())
                .filter((l, idx, arr) => !(idx === 0 && l === '') && !(idx === arr.length - 1 && l === ''))
                .join('\n');
        }
        if (text[i] === '"') {
            i++;
            const start = i;
            while (i < n && text[i] !== '"') i++;
            const v = text.substring(start, i);
            i++;
            return v;
        }
        if (text[i] === '{') return readObject();
        const start = i;
        while (i < n && text[i] !== '\n') i++;
        return text.substring(start, i).trim();
    }
    function readObject() {
        const obj = {};
        skipWs();
        if (text[i] === '{') i++;
        for (;;) {
            skipWs();
            if (i >= n) break;
            if (text[i] === '}') { i++; break; }
            const key = readKey();
            skipWs();
            if (text[i] === ':') i++;
            obj[key] = readValue();
        }
        return obj;
    }
    return readObject();
}

// internalName -> DisplayName real. Un mismo nombre interno solo aparece una vez en todo el
// arbol de localizacion real de Calamity (comprobado: 0 colisiones con DisplayName distinto).
function recogerDisplayNames(obj, out, colisiones) {
    for (const [k, v] of Object.entries(obj)) {
        if (typeof v !== 'object' || v === null) continue;
        if (typeof v.DisplayName === 'string' && v.DisplayName.trim() !== '') {
            const nuevo = v.DisplayName.trim();
            if (out.has(k) && out.get(k) !== nuevo) colisiones.push(`${k}: "${out.get(k)}" vs "${nuevo}"`);
            else out.set(k, nuevo);
        }
        recogerDisplayNames(v, out, colisiones);
    }
}

const { readTmod } = require(TMOD_EXTRACT);
const mod = readTmod(TMOD_PATH);
const displayNames = new Map();
const colisiones = [];
let hjsonLeidos = 0;
for (const [name, buf] of mod.files) {
    if (!name.startsWith('Localization/en-US/') || !name.endsWith('.hjson')) continue;
    hjsonLeidos++;
    recogerDisplayNames(parseHjson(buf.toString('utf8')), displayNames, colisiones);
}
console.log(`${hjsonLeidos} .hjson en-US reales leidos -> ${displayNames.size} DisplayName reales`);
if (colisiones.length) console.log(`AVISO: ${colisiones.length} nombres internos con DisplayName distinto en dos sitios: ${colisiones.slice(0, 5).join(' | ')}`);

function aplicar(fichero) {
    const ruta = path.join(ASSETS, fichero);
    const datos = JSON.parse(fs.readFileSync(ruta, 'utf8'));
    let conReal = 0, igualAlFallback = 0, sinReal = [];
    for (const e of datos) {
        const real = displayNames.get(e.internal);
        if (!real) { sinReal.push(e.internal); continue; }
        e.displayName_en = real;
        conReal++;
        if (real === e.displayName_fallback) igualAlFallback++;
    }
    fs.writeFileSync(ruta, JSON.stringify(datos, null, 2) + '\n', 'utf8');
    console.log(`${fichero}: ${conReal} de ${datos.length} con displayName_en REAL del mod ` +
        `(${conReal - igualAlFallback} de ellos DISTINTOS del fallback humanizado que se usaba antes); ` +
        `sin DisplayName real: ${sinReal.length}${sinReal.length ? ' -> ' + sinReal.slice(0, 10).join(', ') + (sinReal.length > 10 ? ', ...' : '') : ''}`);
}

aplicar('catalog.json');
aplicar('buffs.json');
